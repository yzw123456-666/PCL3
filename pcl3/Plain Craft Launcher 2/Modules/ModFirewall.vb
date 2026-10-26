'PCL3：内置防火墙（Modules/ModFirewall.vb）
'三层防护：
'  1. 启动前静态扫描 mods 文件夹中的 Jar（已知恶意特征 + 危险 API 启发式，按文件大小/修改时间缓存结果）；
'  2. 启动门禁：发现恶意/可疑模组时按设置 提示 或 阻止启动，支持白名单；
'  3. 运行时监控：游戏运行期间监视 Java 进程的可疑子进程（脚本解释器、下载工具等，
'     fractureiser 类木马的典型行为），以及启动目录 / 注册表 Run 键的新增项（持久化检测）。
'说明：静态扫描为启发式，可能误报；所有阻断均提供白名单与继续选项；全部事件写入防火墙日志。

Imports System.IO.Compression
Imports System.Text

Friend Module ModFirewall

#Region "公共状态"

    ''' <summary>防火墙是否启用（总开关）。</summary>
    Public ReadOnly Property Enabled As Boolean
        Get
            Return Settings.Get(Of Boolean)("SystemSafeEnabled")
        End Get
    End Property

    ''' <summary>防火墙日志文件路径。</summary>
    Public ReadOnly Property FirewallLogFile As String
        Get
            Return PathTemp & "Firewall\Log.txt"
        End Get
    End Property

    ''' <summary>写防火墙日志（同时写入 PCL 主日志）。</summary>
    Private Sub WriteLog(Text As String)
        Try
            DirectoryUtils.Create(PathTemp & "Firewall\")
            IO.File.AppendAllText(FirewallLogFile, $"[{Date.Now:yyyy/MM/dd HH:mm:ss}] {Text}{vbCrLf}{vbCrLf}")
        Catch ex As Exception
            Logger.Warn(ex, "写入防火墙日志失败")
        End Try
        Logger.Info($"[防火墙] {Text}")
    End Sub

#End Region

#Region "模组静态扫描"

    ''' <summary>单个文件的扫描结果。</summary>
    Public Class SafeFileResult
        Public FileName As String
        Public Score As Integer
        ''' <summary>0 安全 / 3 注意 / 6 可疑 / 10 恶意（取最高权重与总分共同决定）。</summary>
        Public Level As Integer
        Public Findings As New List(Of String)
    End Class

    'P/Invoke：Toolhelp32 进程快照。每 2 秒枚举一次全部进程，耗时约 1~3ms，
    '比 WMI（SELECT * FROM Win32_Process，每次数十至数百 ms 的 COM 开销）轻两个数量级。
    Private Const TH32CS_SNAPPROCESS As Integer = &H2

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
    Private Structure PROCESSENTRY32W
        Public dwSize As UInteger
        Public cntUsage As UInteger
        Public th32ProcessID As UInteger
        Public th32DefaultHeapID As UIntPtr
        Public th32ModuleID As UInteger
        Public cntThreads As UInteger
        Public th32ParentProcessID As UInteger
        Public pcPriClassBase As Integer
        Public dwFlags As UInteger
        <System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst:=260)>
        Public szExeFile As String
    End Structure

    Private Declare Function CreateToolhelp32Snapshot Lib "kernel32" (dwFlags As Integer, th32ProcessID As Integer) As IntPtr
    Private Declare Function Process32FirstW Lib "kernel32" (hSnapshot As IntPtr, ByRef lpEntry As PROCESSENTRY32W) As Boolean
    Private Declare Function Process32NextW Lib "kernel32" (hSnapshot As IntPtr, ByRef lpEntry As PROCESSENTRY32W) As Boolean
    Private Declare Function CloseHandle Lib "kernel32" (hObject As IntPtr) As Boolean

    ''' <summary>
    ''' 枚举当前全部进程：PID -> (父 PID, 进程名)。
    ''' </summary>
    ''' '''
    Private Function ProcessSnapshot() As Dictionary(Of Integer, KeyValuePair(Of Integer, String))
        Dim Table As New Dictionary(Of Integer, KeyValuePair(Of Integer, String))
        Dim Handle As IntPtr = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0)
        If Handle = IntPtr.Zero OrElse Handle = New IntPtr(-1) Then Return Table
        Try
            Dim Entry As New PROCESSENTRY32W With {.dwSize = CUInt(System.Runtime.InteropServices.Marshal.SizeOf(GetType(PROCESSENTRY32W)))}
            If Process32FirstW(Handle, Entry) Then
                Do
                    Table(CInt(Entry.th32ProcessID)) = New KeyValuePair(Of Integer, String)(CInt(Entry.th32ParentProcessID), Entry.szExeFile)
                Loop While Process32NextW(Handle, Entry)
            End If
        Finally
            CloseHandle(Handle)
        End Try
        Return Table
    End Function

    '危险字符串特征（在 .class 字节码的常量池中以明文 UTF-8 存在，可直接搜索）
    Private ReadOnly MarkRuntimeClass As String = "java/lang/Runtime"
    Private ReadOnly MarkGetRuntime As String = "getRuntime"
    Private ReadOnly MarkProcessBuilder As String = "java/lang/ProcessBuilder"
    Private ReadOnly MarkScriptEngine As String = "javax/script/ScriptEngine"
    Private ReadOnly MarkDefineClass As String = "defineClass"
    Private ReadOnly MarkBase64 As String = "java/util/Base64"
    Private ReadOnly MarkUnsafe As String = "sun/misc/Unsafe"
    Private ReadOnly MarkLoadLibrary As String = "loadLibrary"
    '凭据与钱包文件路径（浏览器配置目录中的敏感文件名）
    Private ReadOnly MarkCredentials() As String = {"Login Data", "cookies.sqlite", "key4.db", "wallet.dat", "Local Extension Settings"}
    '系统持久化 / 脚本解释器特征
    Private ReadOnly MarkPersistence() As String = {"CurrentVersion\\Run", "Start Menu\Programs\Startup", "schtasks", "REG QUERY", "reg add", "Scheduled Tasks"}
    Private ReadOnly MarkShell() As String = {"powershell", "cmd.exe", "mshta", "wscript", "cscript", "certutil", "bitsadmin"}
    '已公开的恶意模组家族特征（可按官方 IOC 通报持续补充）
    Private ReadOnly MarkMalware() As String = {"fractureiser", ".reflexa", "dltlbm"}
    '模组元数据文件（合法模组至少应包含其一）
    Private ReadOnly ModMetadata() As String = {"mcmod.info", "fabric.mod.json", "quilt.mod.json", "mods.toml"}
    '嵌入即视为可疑的非 Java 载荷
    Private ReadOnly BadExtensions() As String = {".exe", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".hta", ".scr", ".jar"}
    '扫描限额（防止被超大恶意 Jar 拖死）
    Private Const MaxEntryBytes As Long = 16 * 1024 * 1024
    Private Const MaxTotalBytes As Long = 96 * 1024 * 1024

    ''' <summary>
    ''' 扫描单个 Jar 文件。异常安全：无法读取时返回“可疑（无法读取）”结果。
    ''' </summary>
    Public Function ScanJar(FilePath As String) As SafeFileResult
        Dim Result As New SafeFileResult With {.FileName = PathUtils.GetLastPart(FilePath)}
        Try
            Using Archive As ZipArchive = FileUtils.OpenZip(FilePath)
                Dim HasMetadata As Boolean = False
                Dim HasClass As Boolean = False
                Dim HasEmbeddedNative As Boolean = False
                Dim HasEmbeddedJar As Boolean = False
                Dim FlagRuntimeClass As Boolean = False
                Dim FlagGetRuntime As Boolean = False
                Dim FlagProcessBuilder As Boolean = False
                Dim FlagScript As Boolean = False
                Dim FlagDefineClass As Boolean = False
                Dim FlagBase64 As Boolean = False
                Dim FlagUnsafe As Boolean = False
                Dim FlagLoadLibrary As Boolean = False
                Dim FlagCredential As Boolean = False
                Dim FlagPersistence As Boolean = False
                Dim FlagShell As Boolean = False
                Dim FlagMalware As Boolean = False
                Dim TotalBytes As Long = 0
                For Each Entry As ZipArchiveEntry In Archive.Entries
                    Dim Name As String = Entry.FullName.ToLower()
                    '条目名检查
                    If ModMetadata.Any(Function(m) Name.Contains(m)) Then HasMetadata = True
                    If Name.EndsWithF(".class") Then HasClass = True
                    Dim Ext As String = PathUtils.GetExtension(Name)
                    If Ext = "dll" OrElse Ext = "so" OrElse Ext = "dylib" Then HasEmbeddedNative = True
                    If BadExtensions.Contains(Ext) AndAlso Not Name.EndsWithF(".class") Then HasEmbeddedJar = True
                    Dim EntryNameLower As String = Entry.FullName.ToLower()
                    If MarkMalware.Any(Function(m) EntryNameLower.Contains(m)) Then FlagMalware = True
                    '内容检查（仅 .class，限额）
                    If Not Name.EndsWithF(".class") Then Continue For
                    If Entry.Length > MaxEntryBytes OrElse TotalBytes > MaxTotalBytes Then Continue For
                    TotalBytes += Entry.Length
                    Dim Content As String
                    Using Stream = Entry.Open()
                        Using Reader As New StreamReader(Stream, Encoding.GetEncoding("ISO-8859-1"))
                            Content = Reader.ReadToEnd()
                        End Using
                    End Using
                    If Content.Contains(MarkRuntimeClass) Then FlagRuntimeClass = True
                    If Content.Contains(MarkGetRuntime) Then FlagGetRuntime = True
                    If Content.Contains(MarkProcessBuilder) Then FlagProcessBuilder = True
                    If Content.Contains(MarkScriptEngine) Then FlagScript = True
                    If Content.Contains(MarkDefineClass) Then FlagDefineClass = True
                    If Content.Contains(MarkBase64) Then FlagBase64 = True
                    If Content.Contains(MarkUnsafe) Then FlagUnsafe = True
                    If Content.Contains(MarkLoadLibrary) Then FlagLoadLibrary = True
                    If MarkCredentials.Any(Function(m) Content.Contains(m)) Then FlagCredential = True
                    If MarkPersistence.Any(Function(m) Content.Contains(m)) Then FlagPersistence = True
                    If MarkShell.Any(Function(m) Content.Contains(m)) Then FlagShell = True
                    If MarkMalware.Any(Function(m) Content.ToLower().Contains(m)) Then FlagMalware = True
                Next
                '规则评分
                Dim CanExec As Boolean = (FlagRuntimeClass AndAlso FlagGetRuntime) OrElse FlagProcessBuilder
                If FlagMalware Then AddFinding(Result, 10, "包含已知恶意模组家族特征（fractureiser 等）")
                If FlagCredential Then AddFinding(Result, 10, "访问浏览器 / 钱包凭据文件路径（窃密行为）")
                If CanExec AndAlso FlagPersistence Then AddFinding(Result, 10, "可执行系统命令，且包含系统自启动 / 计划任务持久化路径")
                If CanExec AndAlso FlagShell Then AddFinding(Result, 8, "可执行系统命令，且包含脚本解释器调用（PowerShell / cmd 等）")
                If FlagDefineClass AndAlso FlagBase64 Then AddFinding(Result, 6, "运行时解密（Base64）并动态加载隐藏代码，常见于加壳恶意载荷")
                If CanExec Then AddFinding(Result, 5, "可执行系统命令（Runtime.exec / ProcessBuilder），正常模组极少使用")
                If FlagScript Then AddFinding(Result, 4, "使用脚本引擎（javax.script）动态执行代码")
                If FlagLoadLibrary AndAlso HasEmbeddedNative Then AddFinding(Result, 3, "加载内嵌的原生库（dll / so），少数性能优化模组也会如此")
                If FlagUnsafe Then AddFinding(Result, 1, "使用 sun.misc.Unsafe 底层 API")
                If Not HasMetadata AndAlso HasClass Then AddFinding(Result, 2, "缺少模组元数据文件（mcmod.info / fabric.mod.json / mods.toml），不是标准模组结构")
                If HasEmbeddedJar AndAlso CanExec Then AddFinding(Result, 3, "内嵌可执行文件（jar / exe / 脚本）且具备命令执行能力")
                '判定等级
                If Result.Score >= 10 OrElse Result.Findings.Any(Function(f) f.StartsWithF("10|")) Then
                    Result.Level = 10
                ElseIf Result.Score >= 6 Then
                    Result.Level = 6
                ElseIf Result.Score >= 3 Then
                    Result.Level = 3
                Else
                    Result.Level = 0
                End If
            End Using
        Catch ex As Exception
            Result.Level = 6
            Result.Score = 6
            Result.Findings.Add("6|无法读取文件（可能损坏或非标准 Zip 结构）：" & ex.Message)
        End Try
        Return Result
    End Function

    Private Sub AddFinding(Result As SafeFileResult, Weight As Integer, Text As String)
        Result.Findings.Add(Weight & "|" & Text)
        Result.Score += Weight
    End Sub

    ''' <summary>
    ''' 扫描结果的本地缓存（按文件大小 + 修改时间失效），路径：PathTemp\Firewall\Cache。
    ''' </summary>
    Private ReadOnly Property CacheFolder As String
        Get
            Return PathTemp & "Firewall\Cache\"
        End Get
    End Property

    Private Function CacheKey(FilePath As String) As String
        Dim Info As New FileInfo(FilePath)
        Return ($"{ResultFileName(FilePath)}|{Info.Length}|{Info.LastWriteTimeUtc.Ticks}").GetStableHashCode().ToString("X8")
    End Function

    Private Function ResultFileName(FilePath As String) As String
        Return PathUtils.GetLastPart(FilePath).Replace(".", "_")
    End Function

    ''' <summary>读取缓存的扫描结果；没有或已失效返回 Nothing。</summary>
    Private Function LoadCache(FilePath As String) As SafeFileResult
        Try
            Dim CachePath As String = CacheFolder & CacheKey(FilePath) & ".txt"
            If Not FileUtils.Exists(CachePath) Then Return Nothing
            Dim Lines = FileUtils.ReadAsLines(CachePath)
            Dim Result As New SafeFileResult With {.FileName = Lines(0), .Level = Val(Lines(1)), .Score = Val(Lines(2))}
            For i = 3 To Lines.Length - 1
                If Lines(i) <> "" Then Result.Findings.Add(Lines(i))
            Next
            Return Result
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub SaveCache(FilePath As String, Result As SafeFileResult)
        Try
            DirectoryUtils.Create(CacheFolder)
            Dim Content As New StringBuilder
            Content.AppendLine(Result.FileName)
            Content.AppendLine(Result.Level)
            Content.AppendLine(Result.Score)
            For Each F In Result.Findings
                Content.AppendLine(F)
            Next
            FileUtils.Write(CacheFolder & CacheKey(FilePath) & ".txt", Content.ToString())
        Catch ex As Exception
            Logger.Warn(ex, "保存防火墙扫描缓存失败")
        End Try
    End Sub

    ''' <summary>
    ''' 扫描某版本 mods 文件夹的全部 Jar（带缓存）。返回扫描结果列表（包含全部文件）。
    ''' </summary>
    Public Function ScanMods(Instance As McInstance, Optional Progress As Action(Of Integer, Integer, String) = Nothing) As List(Of SafeFileResult)
        Dim Results As New List(Of SafeFileResult)
        Try
            If Instance Is Nothing Then Return Results
            Instance.Load()
            Dim ModsFolder As String = Instance.PathIndie & "mods\"
            If Not DirectoryUtils.Exists(ModsFolder) Then Return Results
            Dim Jars = DirectoryUtils.EnumerateFiles(ModsFolder, False, "*.jar").Where(Function(f) f.EndsWithF(".jar")).ToList()
            For i = 0 To Jars.Count - 1
                Progress?.Invoke(i + 1, Jars.Count, PathUtils.GetLastPart(Jars(i)))
                Dim Result = LoadCache(Jars(i))
                If Result Is Nothing Then
                    Result = ScanJar(Jars(i))
                    SaveCache(Jars(i), Result)
                End If
                Results.Add(Result)
            Next
        Catch ex As Exception
            Logger.Warn(ex, "防火墙扫描 mods 文件夹失败")
        End Try
        Return Results
    End Function

#End Region

#Region "启动门禁"

    ''' <summary>
    ''' 白名单（以 ¨ 分隔的文件名列表）。
    ''' </summary>
    Public Function Whitelist() As List(Of String)
        Dim Raw As String = Settings.Get(Of String)("SystemSafeWhitelist")
        If String.IsNullOrWhiteSpace(Raw) Then Return New List(Of String)
        Return Raw.Split("¨"c).Where(Function(s) s <> "").ToList()
    End Function

    Public Sub WhitelistAdd(Names As IEnumerable(Of String))
        Dim List = Whitelist()
        List.AddRange(Names.Where(Function(n) Not List.Contains(n)))
        Settings.Set("SystemSafeWhitelist", List.Join("¨"c))
    End Sub

    Public Sub WhitelistClear()
        Settings.Set("SystemSafeWhitelist", "")
    End Sub

    ''' <summary>
    ''' 启动门禁：在启动加载器线程中调用。返回 True 放行；False 表示取消启动。
    ''' 发现已缓存的可疑结果时也会重新弹窗提示（但不重扫，保证启动速度）。
    ''' </summary>
    Public Function GateLaunch(Instance As McInstance) As Boolean
        Try
            If Not Enabled() OrElse Not Settings.Get(Of Boolean)("SystemSafeScan") Then Return True
            Dim Results = ScanMods(Instance)
            Dim Blocked = Results.Where(Function(r) r.Level >= 6 AndAlso Not Whitelist().Contains(r.FileName)).ToList()
            Dim Noted = Results.Where(Function(r) r.Level = 3 AndAlso Not Whitelist().Contains(r.FileName)).ToList()
            '仅“注意”级：气泡提示，不拦截
            If Noted.Any() AndAlso Not Blocked.Any() Then
                Hint($"防火墙：{Noted.Count} 个模组存在轻度可疑特征（可在 设置 → 安全 查看详情）", HintType.Blue)
            End If
            If Not Blocked.Any() Then Return True
            '发现可疑 / 恶意：弹窗
            Dim Report As New StringBuilder
            Report.AppendLine($"防火墙在 {Instance.Name} 的模组中发现 {Blocked.Count} 个可疑文件：")
            Report.AppendLine()
            For Each B In Blocked.Take(5)
                Report.AppendLine($"· {B.FileName}（{(If(B.Level >= 10, "疑似恶意", "可疑"))}）")
                For Each F In B.Findings.Take(3)
                    Report.AppendLine($"    - {F.AfterFirst("|")}")
                Next
            Next
            If Blocked.Count > 5 Then Report.AppendLine($"…… 以及另外 {Blocked.Count - 5} 个文件")
            Report.AppendLine()
            Report.AppendLine("建议：先删除上述模组再启动。如果确信其安全，可以选择信任（加入白名单）。")
            Report.AppendLine("完整报告见 设置 → 安全 → 查看记录。")
            Dim BlockMode As Boolean = Settings.Get(Of Integer)("SystemSafeMode") = 1
            Dim Choice As Integer
            If BlockMode Then
                Choice = MyMsgBox(Report.ToString(), "防火墙 - 已阻止启动", "取消启动", "无视风险并启动", "信任并启动",
                                  IsWarn:=True, ForceWait:=True)
                '1 取消，2 无视，3 信任
            Else
                Choice = MyMsgBox(Report.ToString(), "防火墙 - 安全警告", "仍要启动", "取消启动", "信任并启动",
                                  IsWarn:=True, ForceWait:=True)
                '1 仍要，2 取消，3 信任
                If Choice = 1 Then Choice = 2 '统一为下方语义：2 = 继续启动
            End If
            Select Case Choice
                Case 2
                    WriteLog($"已放行 {Instance.Name}（用户选择继续，涉及 {Blocked.Count} 个可疑文件：{Blocked.Select(Function(b) b.FileName).Join("、")}）")
                    Return True
                Case 3
                    WhitelistAdd(Blocked.Select(Function(b) b.FileName))
                    WriteLog($"已信任并放行 {Instance.Name}（加入白名单：{Blocked.Select(Function(b) b.FileName).Join("、")}）")
                    Hint("已将上述文件加入防火墙白名单。", HintType.Green)
                    Return True
                Case Else
                    WriteLog($"已阻止 {Instance.Name} 启动（可疑文件：{Blocked.Select(Function(b) b.FileName).Join("、")}）")
                    Return False
            End Select
        Catch ex As Exception
            '防火墙自身故障不应阻止游戏：记录后放行
            Logger.Error(ex, "防火墙启动检查失败（已放行）")
            Return True
        End Try
    End Function

#End Region

#Region "运行时监控"

    'Java 进程不应启动的子进程（脚本解释器 / 下载执行工具）
    Private ReadOnly SuspiciousChildren() As String = {"powershell.exe", "pwsh.exe", "cmd.exe", "wscript.exe", "cscript.exe",
                                                       "mshta.exe", "regsvr32.exe", "rundll32.exe", "bitsadmin.exe", "certutil.exe"}

    ''' <summary>
    ''' 持久化位置快照：注册表 Run 键 + 启动文件夹。
    ''' </summary>
    Private Function PersistenceSnapshot() As Dictionary(Of String, String)
        Dim Snap As New Dictionary(Of String, String)
        Try
            Using Key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Run")
                If Key IsNot Nothing Then
                    For Each Name As String In Key.GetValueNames()
                        Snap("HKCU\Run\" & Name) = If(Key.GetValue(Name), "").ToString()
                    Next
                End If
            End Using
        Catch
        End Try
        Try
            Using Key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Run")
                If Key IsNot Nothing Then
                    For Each Name As String In Key.GetValueNames()
                        Snap("HKLM\Run\" & Name) = If(Key.GetValue(Name), "").ToString()
                    Next
                End If
            End Using
        Catch
        End Try
        For Each Folder As String In {Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                                      Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)}
            Try
                If DirectoryUtils.Exists(Folder) Then
                    For Each File In DirectoryUtils.EnumerateFiles(Folder, False, "*")
                        Snap("Startup\" & PathUtils.GetLastPart(File)) = File
                    Next
                End If
            Catch
            End Try
        Next
        Return Snap
    End Function

    ''' <summary>
    ''' 游戏启动后开启运行时监控线程（子进程 + 持久化位置）。
    ''' </summary>
    Public Sub WatchStart(GameProcess As Process)
        If Not Enabled() OrElse Not Settings.Get(Of Boolean)("SystemSafeWatch") Then Return
        If GameProcess Is Nothing Then Return
        Dim Pid As Integer = GameProcess.Id
        Dim Baseline = PersistenceSnapshot()
        RunInNewThread(
        Sub()
            Try
                Dim AlertedChildren As New HashSet(Of Integer)
                Dim LastPersistCheck As Date = Date.Now
                Do While True
                    Thread.Sleep(2000)
                    Try
                        If GameProcess.HasExited Then Exit Do
                    Catch
                        Exit Do
                    End Try
                    '1. 子进程监控：枚举全部进程并筛出以游戏为祖先的
                    Try
                        Dim Table = ProcessSnapshot() 'pid -> (父pid, 名称)
                        Dim Suspicious As New List(Of String)
                        Dim SuspiciousPids As New List(Of Integer)
                        Dim Frontier As New HashSet(Of Integer) From {Pid}
                        Dim NextFrontier As New HashSet(Of Integer)
                        Do While Frontier.Any()
                            NextFrontier.Clear()
                            For Each Kv In Table
                                If Frontier.Contains(Kv.Value.Key) AndAlso Kv.Key <> Pid Then
                                    Dim Name As String = Kv.Value.Value.ToLower()
                                    If SuspiciousChildren.Contains(Name) AndAlso Not AlertedChildren.Contains(Kv.Key) Then
                                        AlertedChildren.Add(Kv.Key)
                                        Suspicious.Add(Name & "（PID " & Kv.Key & "）")
                                        SuspiciousPids.Add(Kv.Key)
                                    End If
                                    NextFrontier.Add(Kv.Key)
                                End If
                            Next
                            Dim Tmp = Frontier
                            Frontier = NextFrontier
                            NextFrontier = Tmp
                        Loop
                        If Suspicious.Any() Then
                            WriteLog($"检测到游戏进程启动了可疑子进程：{Suspicious.Join("、")}")
                            RunInUi(Sub()
                                        If MyMsgBox($"Minecraft 进程启动了可疑的子进程：{vbCrLf}{vbCrLf}· {Suspicious.Join(vbCrLf & "· ")}{vbCrLf}{vbCrLf}" &
                                                    "这通常意味着某个模组正在执行恶意代码（例如 fractureiser 类木马）。" & vbCrLf &
                                                    "是否立即结束游戏与这些可疑进程？", "防火墙 - 运行时警报", "结束进程", "忽略", IsWarn:=True) = 1 Then
                                            For Each Spid In SuspiciousPids
                                                Try
                                                    Process.GetProcessById(Spid).Kill()
                                                Catch
                                                End Try
                                            Next
                                            Try
                                                GameProcess.Kill()
                                                Hint("已结束游戏与可疑进程。", HintType.Green)
                                            Catch ex2 As Exception
                                                Logger.Warn(ex2, "结束游戏进程失败")
                                            End Try
                                        End If
                                    End Sub)
                        End If
                    Catch ex As Exception
                        Logger.Warn(ex, "防火墙子进程监控轮询失败")
                    End Try
                    '2. 持久化位置监控（每 15 秒对比一次）
                    If (Date.Now - LastPersistCheck).TotalSeconds >= 15 Then
                        LastPersistCheck = Date.Now
                        Try
                            Dim Current = PersistenceSnapshot()
                            Dim NewItems = Current.Where(Function(kv) Not Baseline.ContainsKey(kv.Key) OrElse Baseline(kv.Key) <> kv.Value).ToList()
                            If NewItems.Any() Then
                                For Each kv In NewItems
                                    Baseline(kv.Key) = kv.Value
                                Next
                                WriteLog($"游戏运行期间出现新增的自启动项：{NewItems.Select(Function(kv) kv.Key).Join("、")}")
                                RunInUi(Sub()
                                            MyMsgBox($"游戏运行期间，系统的自启动位置出现了新增项：{vbCrLf}{vbCrLf}" &
                                                     String.Join(vbCrLf, NewItems.Select(Function(kv) $"· {kv.Key} → {kv.Value}")) & vbCrLf & vbCrLf &
                                                     "这可能是恶意模组在植入开机自启（持久化）。若非你本人操作，请立即删除该项并查杀病毒。", "防火墙 - 持久化警报", "知道了", IsWarn:=True)
                                        End Sub)
                            End If
                        Catch ex As Exception
                            Logger.Warn(ex, "防火墙持久化监控轮询失败")
                        End Try
                    End If
                Loop
            Catch ex As Exception
                Logger.Warn(ex, "防火墙运行时监控线程异常退出")
            End Try
        End Sub, "Firewall Watch", ThreadPriority.BelowNormal)
    End Sub

#End Region

#Region "扫描预热"

    '预热单飞状态：快速切换版本时不重复扫描
    Private FirewallPrefetchLock As New Object
    Private FirewallPrefetchTarget As McInstance

    ''' <summary>
    ''' 在后台低优先级线程预热指定版本的模组扫描缓存。版本切换时调用，令启动门禁直接命中缓存、不再阻塞启动。
    ''' </summary>
    ''' '''
    Public Sub Prefetch(Instance As McInstance)
        If Instance Is Nothing OrElse Not Enabled() OrElse Not Settings.Get(Of Boolean)("SystemSafeScan") Then Return
        SyncLock FirewallPrefetchLock
            FirewallPrefetchTarget = Instance
        End SyncLock
        RunInNewThread(
        Sub()
            Try
                Do
                    Dim Target As McInstance = Nothing
                    SyncLock FirewallPrefetchLock
                        Target = FirewallPrefetchTarget
                        FirewallPrefetchTarget = Nothing
                    End SyncLock
                    If Target Is Nothing Then Exit Do
                    Logger.Info($"[防火墙] 后台预热模组扫描缓存：{Target.Name}")
                    ScanMods(Target)
                Loop
            Catch ex As Exception
                Logger.Warn(ex, "防火墙扫描预热失败")
            End Try
        End Sub, "Firewall Prefetch", ThreadPriority.BelowNormal)
    End Sub

#End Region

#Region "手动扫描（供设置页调用）"

    ''' <summary>
    ''' 立即扫描当前版本并在弹窗中显示完整报告。在工作线程调用。
    ''' </summary>
    Public Sub ManualScan()
        If McInstanceSelected Is Nothing Then
            Hint("请先在启动页选择一个游戏版本！", HintType.Red)
            Return
        End If
        Dim Instance = McInstanceSelected
        Dim Results As List(Of SafeFileResult) = Nothing
        RunInUiWait(Sub()
                        Hint("防火墙正在扫描 mods 文件夹……", HintType.Blue)
                    End Sub)
        Results = ScanMods(Instance, Sub(Done, Total, Name)
                                              RunInUiWait(Sub() Hint($"正在扫描 {Done}/{Total}：{Name}", HintType.Blue, False))
                                          End Sub)
        '记录结果
        Dim Bad = Results.Where(Function(r) r.Level >= 3).ToList()
        Dim Summary As String = $"{Date.Now:yyyy/MM/dd HH:mm} · {Instance.Name} · 共 {Results.Count} 个模组" &
                                If(Bad.Any(), $"，发现 {Bad.Count} 个风险文件（疑似恶意 {Bad.Where(Function(b) b.Level >= 10).Count} / 可疑 {Bad.Where(Function(b) b.Level = 6).Count} / 注意 {Bad.Where(Function(b) b.Level = 3).Count}）", "，未发现风险")
        Settings.Set("SystemSafeLastScan", Summary)
        WriteLog($"手动扫描：{Summary}")
        '弹窗报告
        Dim Report As New StringBuilder
        Report.AppendLine($"版本：{Instance.Name}    模组总数：{Results.Count}")
        Report.AppendLine($"结果：{If(Not Bad.Any(), "✔ 未发现风险文件", $"⚠ 发现 {Bad.Count} 个风险文件")}")
        Report.AppendLine()
        If Bad.Any() Then
            For Each B In Bad
                Report.AppendLine($"【{(If(B.Level >= 10, "疑似恶意", If(B.Level = 6, "可疑", "注意")))}】{B.FileName}")
                For Each F In B.Findings
                    Report.AppendLine($"    - {F.AfterFirst("|")}")
                Next
            Next
        Else
            Report.AppendLine("所有模组均未命中危险特征。")
            Report.AppendLine("注意：启发式扫描无法覆盖全部威胁，仍应避免安装来路不明的模组。")
        End If
        RunInUi(Sub() MyMsgBox(Report.ToString(), "防火墙 - 扫描报告", "确定"))
        RunInUi(Sub() FrmSetupSafe?.RefreshLastScan())
    End Sub

#End Region

End Module
