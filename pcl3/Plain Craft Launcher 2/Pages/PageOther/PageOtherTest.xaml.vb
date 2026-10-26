Imports System.Runtime.InteropServices

Public Class PageOtherTest

    Private Shared IsMemoryOptimizeRunning As Boolean = False
    Private IsCaveEnabled As Boolean = False

    Public Sub New()
        InitializeComponent()
        AddHandler MyTextBox.ValidateChanged, Sub(sender As Object, e As EventArgs) StartButtonRefresh()
    End Sub

    Private Sub PageOtherTest_PageEnter() Handles Me.PageEnter
        RefreshDownloadFolder()
        TextDownloadFolder.Validate()
        SaveDownloadFolder()
        TextDownloadName.Validate()
        StartButtonRefresh()
        IsCaveEnabled = True
    End Sub

#Region "今日人品"

    Public Shared Sub Jrrp()
        Dim Num As Integer = CInt(Math.Round(Math.Abs(($"asdfgbn{Now.DayOfYear}12#3$45{Now.Year}IUY".GetStableHashCode() / 3.0 +
                                                        $"QWERTY{Identify}0*8&6{Now.Day}kjhg".GetStableHashCode() / 3.0) / 527.0) Mod 1001.0))
        Dim Score As Integer = If(Num < 970, CInt(Math.Round(Num / 969.0 * 99.0)), 100)
        Dim Text As String
        Select Case True
            Case Score = 100
                Text = "！100！100！！！！！" & If(ModSecret.ThemeUnlock(13, False), vbCrLf & "隐藏主题 欧皇彩 已解锁！", "")
            Case Score >= 98
                Text = "！差点就到 100 了呢……"
            Case Score >= 90
                Text = "！好评如潮！"
            Case Score >= 65
                Text = "！今天运气不错呢！"
            Case Score > 50
                Text = "！还行啦，还行啦。"
            Case Score = 50
                Text = "！五五开……"
            Case Score >= 40
                Text = "！勉强还行吧……？"
            Case Score >= 20
                Text = "！呜……"
            Case Score >= 11
                Text = "？！不会吧……"
            Case Score >= 1
                Text = "……（是百分制哦）"
            Case Else
                Text = "？！"
                If MyMsgBox("在查看结果前，请先同意以下附加使用条款：" & vbCrLf & vbCrLf &
                            "1. 我知晓并了解 PCL 的今日人品功能完全没有出 Bug。" & vbCrLf &
                            "2. PCL 不对使用本软件所间接造成的一切财产损失（如砸电脑等）等负责。",
                            "今日人品 - 附加使用条款", "同意并查看结果", "再见", "", IsWarn:=True) = 2 Then Return
        End Select
        MyMsgBox(If(FrmMain.IsSystemTimeChanged, "在这个时候，你的人品值会是：", "你今天的人品值是：") & Score & Text,
                 If(FrmMain.IsSystemTimeChanged, "今日人品? - ", "今日人品 - ") & Now.ToString("yyyy'/'M'/'d"),
                 "我知道了", "", "", IsWarn:=Score < 20)
    End Sub

    Private Sub BtnJrrp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnJrrp.Click
        Jrrp()
    End Sub

#End Region

#Region "内存优化"

    Private Sub BtnMemory_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnMemory.Click
        RunInThread(Sub() MemoryOptimize(True))
    End Sub

    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        If IsMemoryOptimizeRunning Then
            If ShowHint Then Hint("内存优化尚未结束，请稍等！")
            Return
        End If
        IsMemoryOptimizeRunning = True
        If ShowHint Then Hint("正在进行内存优化……")
        Try
            Dim Changed As Long
            If WindowsUtils.HasAdminRole() Then
                Dim Before As Long = CLng(My.Computer.Info.AvailablePhysicalMemory)
                Try
                    MemoryOptimizeInternal(ShowHint)
                Catch ex As Exception
                    Logger.Warn(ex, "内存优化失败", If(ShowHint, LogBehavior.Alert, LogBehavior.Toast))
                    Return
                Finally
                    IsMemoryOptimizeRunning = False
                End Try
                Changed = CLng(My.Computer.Info.AvailablePhysicalMemory) - Before
            Else
                Logger.Info("没有管理员权限，将以命令行方式进行内存优化")
                Try
                    Changed = CLng(RunAsAdmin("--memory")) * 1024
                Catch ex As Exception
                    Logger.Warn(ex, "命令行形式内存优化失败", LogBehavior.Toast)
                    If ShowHint Then Hint("获取管理员权限失败，请尝试右键 PCL，选择「以管理员身份运行」！", HintType.Red)
                    Return
                Finally
                    IsMemoryOptimizeRunning = False
                End Try
                If Changed < 0 Then Return
            End If
            Dim Remain As String = StringUtils.FormatByteSize(CLng(My.Computer.Info.AvailablePhysicalMemory))
            Logger.Info($"内存优化完成，可用内存改变量：{StringUtils.FormatByteSize(Changed)}，大致剩余内存：{Remain}")
            If Changed > 0 Then
                If ShowHint Then Hint($"内存优化完成，可用内存增加了 {StringUtils.FormatByteSize(CLng(Math.Round(Changed * 0.8)))}，目前剩余内存 {Remain}！", HintType.Green)
            ElseIf ShowHint Then
                Hint($"内存优化完成，已经优化到了最佳状态，目前剩余内存 {Remain}！")
            End If
        Finally
            IsMemoryOptimizeRunning = False
        End Try
    End Sub

    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        If Not WindowsUtils.HasAdminRole() Then
            Throw New Exception("内存优化功能需要管理员权限！" & vbCrLf &
                                "如果需要自动以管理员身份启动 PCL，可以右键 PCL，打开 属性 → 兼容性 → 以管理员身份运行此程序。")
        End If
        Logger.Info("获取内存优化权限")
        Using Identity As Security.Principal.WindowsIdentity = Security.Principal.WindowsIdentity.GetCurrent(
                Security.Principal.TokenAccessLevels.Query Or Security.Principal.TokenAccessLevels.AdjustPrivileges)
            Dim NewState As PrivilegeToken
            NewState.PrivilegeCount = 1
            NewState.Luid = 0
            NewState.Attributes = 2
            Dim SystemName As String = Nothing
            Dim PrivilegeName As String = "SeProfileSingleProcessPrivilege"
            If Not LookupPrivilegeValue(SystemName, PrivilegeName, NewState.Luid) Then
                Throw New Exception($"获取内存优化权限失败（错误代码：{Marshal.GetLastWin32Error()}）")
            End If
            Dim PreviousState As IntPtr = IntPtr.Zero
            Dim ReturnLength As Integer = 0
            If Not AdjustTokenPrivileges(Identity.Token, False, NewState, Marshal.SizeOf(NewState), PreviousState, ReturnLength) OrElse
               Marshal.GetLastWin32Error() <> 0 Then
                Throw New Exception($"获取内存优化权限失败（错误代码：{Marshal.GetLastWin32Error()}）")
            End If
        End Using
        For CommandId As Integer = 2 To 4
            Logger.Info($"内存优化操作 {CommandId} 开始")
            Dim Handle As GCHandle = GCHandle.Alloc(CommandId, GCHandleType.Pinned)
            Dim Result As UInteger = NtSetSystemInformation(80, Handle.AddrOfPinnedObject(), Marshal.SizeOf(CommandId))
            Handle.Free()
            If Result <> 0 Then Throw New Exception($"内存优化操作 {CommandId} 失败（错误代码：{Result}）")
        Next
    End Sub

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure PrivilegeToken
        Public PrivilegeCount As Integer
        Public Luid As Long
        Public Attributes As Integer
    End Structure

    <DllImport("ntdll.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
    Private Shared Function NtSetSystemInformation(SystemInformationClass As Integer, SystemInformation As IntPtr,
                                                   SystemInformationLength As Integer) As UInteger
    End Function

    <DllImport("advapi32.dll", CharSet:=CharSet.Ansi, EntryPoint:="LookupPrivilegeValueA", SetLastError:=True)>
    Private Shared Function LookupPrivilegeValue(<MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpSystemName As String,
                                                 <MarshalAs(UnmanagedType.VBByRefStr)> ByRef lpName As String,
                                                 ByRef lpLuid As Long) As Boolean
    End Function

    <DllImport("advapi32.dll", CharSet:=CharSet.Ansi, SetLastError:=True)>
    Private Shared Function AdjustTokenPrivileges(TokenHandle As IntPtr, DisableAllPrivileges As Boolean,
                                                  ByRef NewState As PrivilegeToken, BufferLength As Integer,
                                                  ByRef PreviousState As IntPtr, ByRef ReturnLength As Integer) As Boolean
    End Function

#End Region

#Region "清理缓存"

    Private Sub BtnClear_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnClear.Click
        RubbishClear()
    End Sub

    Public Shared Sub RubbishClear()
        RunInUi(Sub()
                    If FrmOtherTest?.BtnClear IsNot Nothing AndAlso FrmOtherTest.BtnClear.IsLoaded Then FrmOtherTest.BtnClear.IsEnabled = False
                End Sub)
        RunInNewThread(
        Sub()
            Try
                If HasRunningMinecraft OrElse McLaunchLoader.State = LoadState.Loading Then
                    Hint("请先关闭所有运行中的游戏！")
                    Return
                End If
                If HasDownloadingTask() Then
                    Hint("请在所有下载任务完成后再来清理吧！")
                    Return
                End If
                If Not McFolderList.Any() Then McFolderListLoader.Start()
                If PathUtils.IsSame(PathTemp, Path.GetTempPath() & "PCL\") Then
                    If Settings.Get(Of Integer)("HintClearRubbish") <= 2 Then
                        If MyMsgBox("即将清理游戏日志、错误报告、缓存等文件。" & vbCrLf &
                                    "虽然应该没人往这些地方放重要文件，但还是问一下，是否确认继续？" & vbCrLf & vbCrLf &
                                    "在完成清理后，需要重启 PCL。", "清理确认", "确定", "取消") = 2 Then Return
                        Settings.Set("HintClearRubbish", Settings.Get(Of Integer)("HintClearRubbish") + 1)
                    End If
                ElseIf MyMsgBox("即将清理游戏日志、错误报告、缓存等文件。" & vbCrLf & vbCrLf &
                                "你已将缓存文件夹手动修改为：" & PathTemp & vbCrLf &
                                "清理过程中，将删除该文件夹中的所有内容，且无法恢复。请确认其中没有除了 PCL 缓存以外的重要文件！" & vbCrLf & vbCrLf &
                                "在完成清理后，需要重启 PCL。", "清理确认", "确定", "取消") = 2 Then
                    Return
                End If
                Dim Dirs As New List(Of String)
                If Not McFolderList.Any() Then McFolderListLoader.WaitForExit()
                For Each Folder In McFolderList
                    Dirs.Add(Folder.Location)
                    Dirs.AddRange(DirectoryUtils.EnumerateDirectories(Folder.Location & "versions", False, "*"))
                Next
                Dim CleanDir As Action(Of String) =
                    Sub(TargetDir)
                        Try
                            DirectoryUtils.Delete(TargetDir, False)
                        Catch ex As Exception
                            Logger.Warn(ex, $"清理文件夹失败（{TargetDir}）", LogBehavior.Toast)
                        End Try
                    End Sub
                For Each TargetDir In Dirs
                    CleanDir(Path.Combine(TargetDir, ".mixin.out"))
                    CleanDir(Path.Combine(TargetDir, "crash-reports"))
                    CleanDir(Path.Combine(TargetDir, "debug"))
                    CleanDir(Path.Combine(TargetDir, "logs"))
                    For Each FilePath In DirectoryUtils.EnumerateFiles(TargetDir, False, "*")
                        Dim LastPart = PathUtils.GetLastPart(FilePath)
                        If LastPart.StartsWithF("hs_err_pid", False) OrElse PathUtils.GetExtension(LastPart) = "log" OrElse LastPart = "WailaErrorOutput.txt" Then
                            FileUtils.Delete(FilePath, False)
                        End If
                    Next
                    For Each SubDir In DirectoryUtils.EnumerateDirectories(TargetDir, False, "*")
                        If PathUtils.GetLastPart(SubDir) = PathUtils.GetLastPart(TargetDir) & "-natives" OrElse PathUtils.GetLastPart(SubDir) = "natives-windows-x86_64" Then
                            CleanDir(SubDir)
                        End If
                    Next
                Next
                CleanDir(PathTemp)
                CleanDir(OsDrive & "ProgramData\PCL\")
                If MyMsgBox("垃圾已经清理好啦！", "清理完成", "关闭 PCL", "重启 PCL", "", IsWarn:=False, HighLight:=True, ForceWait:=True) = 2 Then
                    StartProcess(New ProcessStartInfo(PathExe, "--wait"))
                End If
                FormMain.EndProgramForce()
            Catch ex As Exception
                Logger.Error(ex, "清理垃圾失败", LogBehavior.Alert)
            Finally
                RunInUiWait(Sub()
                                If FrmOtherTest?.BtnClear IsNot Nothing AndAlso FrmOtherTest.BtnClear.IsLoaded Then FrmOtherTest.BtnClear.IsEnabled = True
                            End Sub)
            End Try
        End Sub, "Rubbish Clear")
    End Sub

#End Region

#Region "下载工具"

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Try
            If String.IsNullOrWhiteSpace(Folder) Then
                Dim Target As String = Dialogs.SaveFile("选择文件保存位置", FileName)
                If Target Is Nothing Then Return
                FileName = PathUtils.GetLastPart(Target)
                Folder = PathUtils.RemoveLastPart(Target)
            End If
            Folder = PathUtils.AddSlashSuffix(Folder.Replace("/", "\"))
            Try
                DirectoryUtils.Create(Folder)
                CheckPermissionWithException(Folder)
            Catch ex As Exception
                Logger.Error(ex, $"访问文件夹失败（{Folder}）", LogBehavior.Alert)
                Return
            End Try
            Logger.Info($"自定义下载文件名：{FileName}")
            Logger.Info($"自定义下载文件目标：{Folder}")
            Dim Uuid As Integer = GetUuid()
            Dim Download As New LoaderDownload($"自定义下载文件：{FileName}",
                                               {New NetFile({Url}, Folder & FileName, Nothing, True)})
            Dim Combo As New LoaderCombo(Of Integer)($"自定义下载 ({Uuid})", {Download})
            Combo.OnStateChanged =
                Sub(Loader)
                    Select Case Combo.State
                        Case LoadState.Finished
                            Hint($"文件已下载到 {Folder & FileName}！", HintType.Green, False)
                        Case LoadState.Failed
                            Hint("文件下载失败，请检查下载地址是否有效！", HintType.Red, False)
                    End Select
                End Sub
            Combo.Start()
            LoaderTaskbarAdd(Combo)
            FrmMain.BtnExtraDownload.ShowRefresh()
            FrmMain.BtnExtraDownload.Ribble()
        Catch ex As Exception
            Logger.Error(ex, "开始自定义下载失败", LogBehavior.AlertThenFeedback)
        End Try
    End Sub

    Private Sub BtnDownloadStart_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDownloadStart.Click
        If Not StartButtonValidate() Then Return
        StartCustomDownload(TextDownloadUrl.Text.Trim, TextDownloadName.Text.Trim, TextDownloadFolder.Text.Trim)
        TextDownloadUrl.Text = ""
        TextDownloadName.Text = ""
        TextDownloadUrl.Validate()
        TextDownloadName.Validate()
        StartButtonRefresh()
    End Sub

    Private Sub BtnDownloadSelect_Click(sender As Object, e As EventArgs) Handles BtnDownloadSelect.Click
        Dim Folder = Dialogs.SelectFolder("选择目标文件夹", False).FirstOrDefault()
        If Folder Is Nothing Then Return
        TextDownloadFolder.Text = Folder
        SaveDownloadFolder()
        StartButtonRefresh()
    End Sub

    Private Sub BtnDownloadOpen_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDownloadOpen.Click
        Try
            Dim Folder = TextDownloadFolder.Text.Trim
            If String.IsNullOrWhiteSpace(Folder) Then Folder = DefaultDownloadFolder()
            DirectoryUtils.Create(Folder)
            StartProcess(Folder)
        Catch ex As Exception
            Logger.Warn(ex, "打开下载文件夹失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub TextDownloadUrl_Validated() Handles TextDownloadUrl.ValidatedTextChanged
        '自动填入文件名
        If String.IsNullOrWhiteSpace(TextDownloadName.Text) AndAlso Not String.IsNullOrWhiteSpace(TextDownloadUrl.Text) Then
            Try
                Dim Name = PathUtils.GetLastPart(WebUtility.UrlDecode(TextDownloadUrl.Text.Trim))
                If Not String.IsNullOrWhiteSpace(Name) AndAlso Name.Length <= 100 Then TextDownloadName.Text = Name
            Catch
                '忽略无法解析的地址
            End Try
        End If
        StartButtonRefresh()
    End Sub

    Private Sub TextDownload_Validated() Handles TextDownloadName.ValidatedTextChanged, TextDownloadFolder.ValidatedTextChanged
        SaveDownloadFolder()
        StartButtonRefresh()
    End Sub

    Private Function DefaultDownloadFolder() As String
        Return Paths.Base & "PCL\MyDownload\"
    End Function

    Private Sub RefreshDownloadFolder()
        Dim Saved As String = Settings.Get(Of String)("CacheDownloadFolder")
        If String.IsNullOrWhiteSpace(TextDownloadFolder.Text) Then
            TextDownloadFolder.Text = If(String.IsNullOrWhiteSpace(Saved), DefaultDownloadFolder(), Saved)
        End If
    End Sub

    Private Sub SaveDownloadFolder()
        Dim Folder = TextDownloadFolder.Text.Trim
        If String.IsNullOrWhiteSpace(Folder) Then Return
        Settings.Set("CacheDownloadFolder", Folder)
        Dim Rule = TryCast(TextDownloadName.ValidateRules.FirstOrDefault(), ValidateFileName)
        If Rule IsNot Nothing Then
            Rule.ParentFolder = Folder
            TextDownloadName.Validate()
        End If
    End Sub

    Private Function StartButtonValidate() As Boolean
        If String.IsNullOrWhiteSpace(TextDownloadUrl.Text) Then
            Hint("请填写下载地址！", HintType.Red, False)
            Return False
        End If
        If String.IsNullOrWhiteSpace(TextDownloadName.Text) Then
            Hint("请填写文件名！", HintType.Red, False)
            Return False
        End If
        If Not TextDownloadUrl.IsValidated Then
            Hint($"下载地址无效：{TextDownloadUrl.ValidateResult}", HintType.Red, False)
            Return False
        End If
        If Not TextDownloadName.IsValidated Then
            Hint($"文件名无效：{TextDownloadName.ValidateResult}", HintType.Red, False)
            Return False
        End If
        If Not TextDownloadFolder.IsValidated Then
            Hint($"保存文件夹无效：{TextDownloadFolder.ValidateResult}", HintType.Red, False)
            Return False
        End If
        Return True
    End Function

    Private Sub StartButtonRefresh()
        If BtnDownloadStart Is Nothing Then Return
        BtnDownloadStart.IsEnabled = TextDownloadFolder.IsValidated AndAlso TextDownloadUrl.IsValidated AndAlso TextDownloadName.IsValidated
        BtnDownloadOpen.IsEnabled = TextDownloadFolder.IsValidated
    End Sub

    Private Sub TextDownloadUrl_KeyDown(sender As Object, e As KeyEventArgs) Handles TextDownloadUrl.KeyDown
        If e.Key = Key.Return AndAlso BtnDownloadStart.IsEnabled Then
            e.Handled = True
            BtnDownloadStart_Click(BtnDownloadStart, Nothing)
        End If
    End Sub

#End Region

#Region "回声洞"

    Private Sub CardCave_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles CardCave.MouseLeftButtonUp
        If Not IsCaveEnabled Then Return
        IsCaveEnabled = False
        Dim NewText = GetRandomCave()
        AniStart({
            AaOpacity(LabCave, 0, 80),
            AaCode(Sub() LabCave.Text = NewText, 80),
            AaOpacity(LabCave, 1, 150, 80),
            AaCode(Sub() IsCaveEnabled = True, 0, 230)
        }, "Cave")
    End Sub

    Private Sub BtnCave_Click(sender As Object, e As EventArgs) Handles BtnCave.Click
        OpenWebsite("https://jinshuju.net/f/esXHQF")
    End Sub

    Public Shared Function GetRandomCave() As String
        Return RandomOne(New String() {
            "来硬的！", "钻石！", "我们需要再深入些！", "结束了？", "君临天下！", "与火共舞！",
            "天空即为极限！", "甜蜜的梦！", "探索的时光！", "狙击手的对决！", "这是？工作台！", "永恒的伙伴！",
            "这交易不错！", "你的世界！", "Also try Create!", "这是刻意的游戏设计！", "This is a FEATURE! Not a BUG!",
            "你好中国！", "/give @a hugs 64", "Minecraft 2.0!", "Hello, Herobrine!", "苦力怕！不是爬行者！",
            "Bugjump 自古特性多，可与育碧争霸王！", "Can't keep up! Did the system time change or is the server overloaded?",
            "今天也努力当咸鱼了呢（瘫）", "MC 是我们的青春啊！", "我的世界，启动！", "正于此地，愿你找到想要的 Mod 和整合包！",
            "脑子放假去了！", "黄瓜鸡蛋粥，好吃又美味！", "刷到此条的，一切都会如愿，所想所念一定能成功！",
            "没有钻石块的可以用下界合金块代替！", "Priority Crash Launcher!", "自己都不相信自己，那怎么会成功？",
            "Minecraft saves the world!", "那就当做没有 Bug 好了！", "试试泰拉瑞亚！", "向骰子低头！",
            "破产了启动器！", "泡菜龙启动器！", "Keep your DETERMINATION.", "我东方永不过气！", "PPCCLL!",
            "希望这个游戏可以给你带来更多的快乐！", "Also try Celeste!", "要恰饭的嘛！", "人生没有标准答案，去做自己认为正确的事！",
            "For Super Earth!", "支持正版 MC，加油！", "失之东隅，收之桑榆。", "让我们拜请白日铸炉！",
            "请为坠落的龙猫取名……", "小友，你在期待什么？", "你的旅途……到此为止……", "Also try PCL CE!",
            "Also try HMCL!", "那么，我们开始吧？", "突然想起来有回声洞！", "我可以一个人很好，但有你会更好。",
            "咕咕嘎嘎！", "众所周知，我的世界的开发商是 BugJump。", "别杀怪物，你这个海豚！",
            "我永远都不相信我的今日人品会是 0！", "千万得点！", "相信我，一切都会变好的！", "看到这个评论的人请去喝一杯水！",
            "长风破浪会有时，直挂云帆济沧海！", "Missing No.", "管管孩子，救救游戏！", "今天好累啊！",
            "希望人有事.jpg", "修了的叫 Bug，没修的叫特性。", "多线程被卡住了不玩手机玩什么呀！（绝望）",
            "Ba+2Na=Banana", "这里可以写广告嘛？", "众所周知，1+1=王！", "世界和平！",
            "别点了！手不累吗？", "很好的 MOD 使我存档损坏", "另类本身就是一种出类拔萃！", "麻辣鸡翅真好吃！",
            "已深度思考（用时 0 秒）" & vbCrLf & vbCrLf & "服务器繁忙，请稍后再试。", "For the Emperor!",
            "Wake up.", "It's time to go to school.", "Made in Abyss!", "我的世界我做主！",
            "早上好，中午好，晚上好！", "MC，启动！", "挖矿免费玩！", "宁可没用，不能没有！",
            "基岩真好吃！", "弱小和无知不是生存的障碍，傲慢才是。——《三体》", "Also try Terraria.", "Ctrl + S",
            "↑↑↓↓←→←→BABA", "我的评价是：我不予评价。", "* It fills you with determination.",
            "温馨提示：你可点击红色按钮，剩余要靠你的智慧啦~", "使用左键以和铁傀儡友好交流！",
            "乌拉！！！！！", "不要尝试带着猫挑衅苦力怕！", "快来试试 PCL 下载器！", "新的 Bug 删除了，旧的 Bug 增加了。",
            "你知道吗？你不知道。", "回声~回声~回声~", "爷爷，你关注的龙猫终于更新啦！", "哇！金色传说！",
            "赶紧去给我开 EVA！", "奇怪的知识增加了！", "小朋友，你是否有很多问号？", "生鱼忧患，死鱼安乐。",
            "Ease my mind.", "你看这个光影多棒啊，开一下试试，反正电脑好，诶我主机怎么烧",
            "衬衫的价格是九磅十五便士。", "犹豫，就会白给！", "PCL YES!", "龙猫是龙还是猫？",
            "恭喜你，你被恭喜了！", "你这什么游戏啊？手机能玩吗？要不要钱啊？", "Press F to enter the tank!",
            "多行不义必自毙。", "在我没想到好的留言之前请不要把我放到回声洞中！", "3T3B", "没通关过。",
            "幻翼感受到了动能……", "/kill @e[type=creeper]", "/gamemode 1", "你炸出了 TNT 之神,我将赐予你 TNT！",
            "不残血不会玩.jpg", "众所周知，别人的世界和我的世界不是同一款游戏。", "好想要正版账号",
            "众所周知，地狱有水。", "游戏一小时，看云 59min。", "我们需要再深入些。抱歉，今天不行。", "TIPS: 大量的红键会使屏幕起火！",
            "喵呜~你好可爱~", "如果游戏没有声音可以尝试按下 F3+S！", "Mojang AB = Mojang And Bug",
            "Removed Herobrine!", "你一定要点上面那个千万别点！！！", "花开花落，再灿烂的星光也终将消失。",
            "欢迎使用史上最复杂的解密启动器！（doge）", "宇宙多么浩瀚，偏偏我们在此相遇！", "人品测试，每天一次！",
            "没错，是百分制√", "这是一条回声！", "今天你猫车了吗？", "我的世界一直都不只是一款马赛克游戏！",
            "这次一定！", "秒了，有什么好说的。", "塔塔开！塔塔开！", "芜湖，起飞！", "催更催更，我是急急国王！",
            "Why do you see this? Shouldn't you play your game?", "We're no strangers to love~",
            "还记得第一次玩我的世界的时候吗？那曾是很美好的回忆……", "千万别点千万别点！",
            "在时间的流逝中，没有什么事是一成不变的。",
            "熬夜对身体不好，所以我建议你们……玩通宵。", "Now you're thinking with portals.",
            "买不起正版的穷鬼举个爪。", "有——回——声——吗—— 有-回-声-吗- 有回声吗",
            "Notch is coming back!", "我不会告诉你，鼠标悬浮在隐藏主题上，会显示出什么不可告人的秘密！",
            "结束了？不，你还有很多事要做，现在还不能休息哦……", "握~着~我~的~抱~枕~",
            "人被逼急了啥都能做出来，除了数学题！", "关掉，关掉，一定要关掉！",
            "月色与雪色之间，你是第三种绝色。", "或许你并不是不想睡觉，而是周围有怪物在游荡！",
            "阿瓦达啃大瓜！", "少年没有乌托邦！", "生活就像打电话，不是你先挂就是我先挂！",
            "好好睡一觉， 就是人生的重启方式呀。", "为什么披萨会考糊？",
            "再点一下吧！", "到点了，Visual Studio 上号！", "死机蓝！",
            "再次点击查看下一位沙雕网友乱七八糟的留言！", "咕咕咕——" & vbCrLf & "翻译：鸽，下次也不一定。",
            "在 MC 中，沙子可以下落，说明 MC 还是很科学的！（确信", "唔咕，要饭大失败……（眼神死",
            "这是回声洞~是回声洞~回声洞~声洞~洞~", "往往结束才是开始！", "玻璃，放错，退游，一气呵成！",
            "看到这条留言，请为疫情中献身的人们默哀一分钟，并献出至高的敬意。",
            "忠告：请不要在喝水的时候看回声洞，否则所造成的一切后果与 PCL 无关。",
            "在这个时候，你的人品值会是：100！100！100！！！！！",
            "愿世界永葆和平！", "前进，然后变得更好！", "The cake is a lie.", "别戳了，这里没有你想找的东西！",
            "Make Minecraft Great Again!", "恭喜你，你的鼠标左键没坏！",
            "* 移除了 Herobrine" & vbCrLf & "* 修复了一个 Bug" & vbCrLf & "* 增加了一个 Bug",
            "众所周知，在服务器中按 Alt+F4 可以开启飞行！", "夹子，夹子，更多的夹子，夹子在蔓延……",
            "反馈 Bug 前……先想一想这是不是特性！", "这是回声洞还是回字洞？", "歪比巴卜？",
            "获得成就：别人的世界！", "你知道吗，当你看了这条信息会发现看了跟不看一个样！",
            "冷知识：这其实是一条热知识！", "回声洞里面没有米勒星球！",
            "今日大无语事件：外卖点了个黑椒牛排套餐，结果商家忘了放牛排……",
            "虽然上面那个按钮叫千万别点，但是还有好多人去点它！",
            "damedane, dameyo……", "你说得对，但是，后面忘了。", "记得使用 Steam 启动 PCL 哦！",
            "你不崩谁崩？", "不懂就问，我要问什么？", "年轻人不讲武德！",
            "You should try our sister game, Minceraft!", "0 errors, 0 warnings!", "这个好诶！",
            "我们都是阴沟里的虫子，但总还是得有人仰望星空。", "开学愉快！",
            "温馨提示：按 Alt+F4 有惊喜！", "我的人品必不可能是 0！系统有 Bug！",
            "如果今天是你的生日，那我祝你生日快乐，如果不是，那我祝你早上中午下午晚上好！",
            "我保留了千万别点，这样你才知道你用的是 PCL。", "鸡汤来咯~~~~", "今天是个看人品值的好日子啊~",
            "这里没有人~我们都是鬼~", "Long may the sunshine!", "If you can.", "Tell me your secret.",
            "《我 们 拥 有 最 真 实 的 物 理 引 擎》", "MC，启动！", "今日人品？",
            "你每天都会忘掉很多事，为什么不把这件事也忘掉呢？", "再次点击这里以查看更多的 PCL 作者和各位沙雕网友乱七八糟的留言！",
            "不要再打羊驼了啊！！！", "Bug 变 Feature，妙啊！", "你在狗叫什么？"
        })
    End Function

    Public Shared Function GetRandomHint() As String
        Try
            Dim HintFile As String = Paths.Base & "PCL\hints.txt"
            If FileUtils.Exists(HintFile) Then
                Dim Lines = FileUtils.ReadAsLines(HintFile, True).Where(Function(s) Not String.IsNullOrWhiteSpace(s)).ToList()
                If Lines.Any() Then Return RandomOne(Lines)
            End If
        Catch ex As Exception
            Logger.Error(ex, "获取自定义 你知道吗 提示失败")
        End Try
        Return GetRandomPresetHint()
    End Function

    Public Shared Function GetRandomPresetHint() As String
        Return RandomOne(New String() {
            "在版本选择页面，右键某个版本也能进入版本设置页面！",
            "你可以在版本设置中调整分类，以将特定版本隐藏！",
            "使用 --memory 参数启动 PCL 可以静默进行内存优化！",
            "在第一次启动游戏时，PCL 会自动将语言设置为中文！",
            "在版本设置中可以开启第三方登录验证，例如 Little Skin 登录！",
            "自动配置游戏内存时，PCL 将会根据剩余内存与 Mod 数量动态决定分配的内存！",
            "主页可以使用特定的预设，在设置中看看吧！",
            "在高级启动选项中，可以设置在游戏启动前运行特定程序！",
            "将鼠标悬浮在设置页的左边栏，可以找到重置设置按钮！",
            "版本设置只对当前版本生效，而设置页面的设置对所有版本生效！",
            "要将已有的 MC 文件夹加入 PCL，可以在版本选择页的左侧选择「添加已有文件夹」！",
            "如果同时安装了 OptiFine 与对应的原版，PCL 会展示 OptiFine 版本，折叠原版！",
            "版本选择的 常规版本 分类中，只会列出最新的一个快照或预发布版。",
            "在版本选择页面，右键游戏文件夹可以进行打开、重命名、删除等操作！",
            "如果你在其他地方修改了皮肤，需要手动选择「刷新皮肤」才能更新登录页面的头像……",
            "下载 Mod 时，PCL 会自动定位对应版本的 Mod 文件夹！",
            "如果缺少 Java，PCL 也能自动下载，不必自己安装啦！",
            "如果你打开了调试模式，启动页右侧就会显示启动日志！",
            "将鼠标悬浮在下载页的左边栏，可以找到刷新按钮！",
            "将鼠标指向下载页的 MC 版本，可以在右侧找到「更新日志」选项！",
            "直接把 Mod 或整合包拖进 PCL 窗口就能安装了！",
            "在 PCL 文件夹下新建 hints.txt，可以自定义「你知道吗」的内容！",
            "更衣室中可以为离线登录自定义皮肤，正版与第三方登录也能管理皮肤和披风！",
            "如果不想用某项功能，可以在个性化设置中把它隐藏掉！",
            "如果打开了「游戏更新提示」功能，当 MC 更新时 PCL 会弹窗进行提醒！",
            "你可以使用主页来自定义快捷方式！",
            "点击版本右侧的心形就能将该版本加入收藏夹，便于查找。",
            "PCL 的第一个内部版本制作于 2018 年 8 月 13 日！",
            "据调查，有 90.3% 的用户点击了百宝箱中的千万别点按钮！",
            "PCL 的绝大多数代码都在 GitHub 开源了！",
            "PCL 的开发者龙腾猫跃经常被简称为龙猫，但和那只龙猫没有任何关系！"
        })
    End Function

#End Region

#Region "千万别点"

    Private Sub BtnClick_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnClick.Click
        Try
            If MyMsgBox("PCL 作者不会受理由于点击千万别点造成的任何 Bug。" & vbCrLf & "这是最后的警告，是否继续操作？",
                        "警告", "确定", "取消", "", IsWarn:=True) = 2 Then Return
            If MyMsgBox("当暴露在点击确定后的场景时，有极小部分人群会引发癫痫。" & vbCrLf &
                        "如果您在稍后出现任何不适症状，请立即关闭 PCL 并咨询您的医生或医师。" & vbCrLf &
                        "这是最后的警告，是否继续操作？", "警告", "确定", "取消", "", IsWarn:=True) = 2 Then Return
            ModSecret.ThemeDontClick = 2
            ModSecret.ThemeRefresh()
            Hint(GetRandomCave(), HintType.Blue, False)
        Catch ex As Exception
            Logger.Error(ex, "千万别点发生错误", LogBehavior.Alert)
        End Try
    End Sub

#End Region

End Class
