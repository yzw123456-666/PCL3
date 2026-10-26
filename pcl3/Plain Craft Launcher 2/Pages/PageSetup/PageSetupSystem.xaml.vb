Public Class PageSetupSystem


    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        '重复加载部分
        PanBack.ScrollToHome()

        'PCL3：已移除赞助解锁面板与土豆码功能

        '非重复加载部分
        Static IsLoaded As Boolean = False
        If IsLoaded Then Return
        IsLoaded = True

        AniControlEnabled += 1
        Reload()
        SliderLoad()
        AniControlEnabled -= 1

    End Sub
    Public Sub Reload()
        SettingService.RefreshSettings(Me)
        RefreshDeveloperInfo()
    End Sub
    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化启动器页设置")
            Hint("已初始化启动器页设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化启动器页设置失败", LogBehavior.Alert)
        End Try
        Reload()
    End Sub

    '滑动条
    Private Sub SliderLoad()
        SliderDownloadThread.GetHintText = Function(v) v + 1
        SliderDownloadSpeed.GetHintText =
        Function(v)
            Select Case v
                Case Is <= 14
                    Return (v + 1) * 0.1 & " M/s"
                Case Is <= 31
                    Return (v - 11) * 0.5 & " M/s"
                Case Is <= 41
                    Return (v - 21) & " M/s"
                Case Else
                    Return "无限制"
            End Select
        End Function
        SliderDebugAnim.GetHintText = Function(v) If(v > 29, "关闭", (v / 10 + 0.1) & "x")
        SliderEasterHue.GetHintText = Function(v) "+" & v & " 色相"
    End Sub
    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        If SliderDownloadThread.Value < 100 Then Return
        If Not Settings.Get(Of Boolean)("HintDownloadThread") Then
            Settings.Set("HintDownloadThread", True)
            MyMsgBox("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。" & vbCrLf &
                     "一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", IsWarn:=True)
        End If
    End Sub

    'PCL3：已移除识别码/土豆码入口

    '调试模式
    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        If AniControlEnabled = 0 Then Hint("部分调试信息将在刷新或启动器重启后切换显示！",, False)
    End Sub

    '自动更新
    Private Sub ComboSystemActivity_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemActivity.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboSystemActivity.SelectedIndex <> 2 Then Return
        If MyMsgBox("若选择此项，即使在将来出现严重问题时，你也无法获取相关通知。" & vbCrLf &
                    "例如，如果发现某个版本游戏存在严重 Bug，你可能就会因为无法得到通知而导致无法预知的后果。" & vbCrLf & vbCrLf &
                    "一般选择 仅在有重要通知时显示公告 就可以让你尽量不受打扰了。" & vbCrLf &
                    "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            If e.RemovedItems.Count > 0 Then ComboSystemActivity.SelectedItem = e.RemovedItems(0)
        End If
    End Sub
    Private Sub ComboSystemUpdate_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboSystemUpdate.SelectionChanged
        If AniControlEnabled <> 0 Then Return
        If ComboSystemUpdate.SelectedIndex <> 3 Then Return
        If MyMsgBox("若选择此项，即使在启动器将来出现严重问题时，你也无法获取更新并获得修复。" & vbCrLf &
                    "例如，如果官方修改了登录方式，从而导致现有启动器无法登录，你可能就会因为无法更新而无法开始游戏。" & vbCrLf & vbCrLf &
                    "一般选择 仅在有重大漏洞更新时显示提示 就可以让你尽量不受打扰了。" & vbCrLf &
                    "除非你在制作服务器整合包，或时常手动更新启动器，否则极度不推荐选择此项！", "警告", "我知道我在做什么", "取消", IsWarn:=True) = 2 Then
            If e.RemovedItems.Count > 0 Then ComboSystemUpdate.SelectedItem = e.RemovedItems(0)
        End If
    End Sub
    Private Sub BtnSystemUpdate_Click(sender As Object, e As EventArgs) Handles BtnSystemUpdate.Click
        UpdateCheckByButton()
    End Sub
    ''' <summary>
    ''' 启动器是否已经是最新版？
    ''' 若返回 Nothing，则代表无更新缓存文件或出错。
    ''' </summary>
    Public Shared Function IsLauncherNewest() As Boolean?
        Try
            '确认服务器公告是否正常
            Dim ServerContent As String = If(FileUtils.TryReadAsString(PathTemp & "Cache\Notice.cfg"), "")
            If ServerContent.Split("|").Count < 3 Then Return Nothing
            '确认是否为最新
            Return ServerContent.Split("|")(If(BuildType = BuildTypes.Release, 2, 1)) <= VersionCode
        Catch ex As Exception
            Logger.Error(ex, "确认启动器更新失败")
            Return Nothing
        End Try
    End Function

#Region "开发者模式"

    Private Sub RefreshDeveloperInfo()
        If LabDevVersion Is Nothing Then Return
        Try
            LabDevVersion.Text = $"PCL {VersionDisplay}（版本号 {VersionCode}）{vbCrLf}" &
                                 $"系统：{My.Computer.Info.OSFullName}（{Environment.OSVersion.VersionString}，64 位：{Environment.Is64BitOperatingSystem}）{vbCrLf}" &
                                 $"PCL 路径：{Paths.Base}{vbCrLf}" &
                                 $"当前版本：{If(McInstanceSelected IsNot Nothing, McInstanceSelected.Name, "未选择")}"
        Catch ex As Exception
            Logger.Error(ex, "获取开发者信息失败")
        End Try
    End Sub

    Private Sub BtnDevLog_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevLog.Click
        Dim LogPath As String = Paths.BaseThenName & "Log1.txt"
        If Not FileUtils.Exists(LogPath) Then
            Hint("还没有生成日志文件。", HintType.Red, False)
            Return
        End If
        OpenExplorer(LogPath)
    End Sub

    Private Sub BtnDevFolder_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevFolder.Click
        OpenExplorer(Paths.Base)
    End Sub

    Private Sub BtnDevExport_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevExport.Click
        Try
            Dim Content As New Text.StringBuilder
            Content.AppendLine("=== PCL 诊断信息 ===")
            Content.AppendLine($"生成时间：{Now:yyyy/MM/dd HH:mm:ss}")
            Content.AppendLine($"版本：{VersionDisplay}（版本号 {VersionCode}，构建类型 {BuildType}，调试模式 {ModeDebug}）")
            Content.AppendLine($"系统：{My.Computer.Info.OSFullName}（{Environment.OSVersion.VersionString}）")
            Content.AppendLine($"运行环境：.NET {Environment.Version}，64 位：{Environment.Is64BitOperatingSystem}，管理员：{WindowsUtils.HasAdminRole()}")
            Content.AppendLine($"内存：可用 {Int(My.Computer.Info.AvailablePhysicalMemory / 1024 / 1024)} M / 总计 {Int(My.Computer.Info.TotalPhysicalMemory / 1024 / 1024)} M")
            Content.AppendLine($"DPI：{DPI}（{Math.Round(DPI / 96, 2) * 100}%）")
            Content.AppendLine($"PCL 路径：{Paths.Base}")
            Content.AppendLine($"临时文件夹：{PathTemp}")
            Content.AppendLine($"当前版本：{If(McInstanceSelected?.Name, "未选择")}")
            Content.AppendLine("MC 文件夹：")
            For Each Folder In McFolderList
                Content.AppendLine($"  {Folder.Name}：{Folder.Location}")
            Next
            Content.AppendLine()
            Content.AppendLine("=== 最近的日志 ===")
            Dim LogPath As String = Paths.BaseThenName & "Log1.txt"
            If FileUtils.Exists(LogPath) Then
                Dim Lines = FileUtils.ReadAsLines(LogPath)
                Content.AppendLine(String.Join(vbCrLf, Lines.Skip(Math.Max(0, Lines.Length - 300))))
            Else
                Content.AppendLine("（没有找到日志文件）")
            End If
            Dim Target As String = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) & "\PCL诊断信息_" & Now.ToString("yyyyMMdd_HHmmss") & ".txt"
            FileUtils.Write(Target, Content.ToString())
            Hint("诊断信息已保存到桌面！", HintType.Green, False)
            OpenExplorer(Target)
        Catch ex As Exception
            Logger.Error(ex, "导出诊断信息失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnDevCrash_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevCrash.Click
        Dim FilePath As String = Dialogs.SelectFile("选择要分析的崩溃报告或日志文件", False,
                                                   filter:={({"txt", "log", "json", "zip"}, "崩溃报告或日志文件")}).FirstOrDefault()
        If FilePath Is Nothing Then Return
        RunInNewThread(
        Sub()
            Try
                Hint("正在分析崩溃报告……")
                Dim Analyzer As New CrashAnalyzer(McInstanceSelected)
                Analyzer.Import(FilePath)
                If Not Analyzer.Prepare() Then
                    Hint("无法识别该文件中的崩溃信息。", HintType.Red)
                    Return
                End If
                Analyzer.Analyze()
                Analyzer.Output(True)
            Catch ex As Exception
                Logger.Error(ex, "分析崩溃报告失败", LogBehavior.Alert)
            End Try
        End Sub, "Dev Crash Analyze")
    End Sub

    Private Sub BtnDevVersion_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevVersion.Click
        Try
            If McInstanceSelected Is Nothing Then
                Hint("请先在启动页选择一个游戏版本！", HintType.Red, False)
                Return
            End If
            McInstanceSelected.Load()
            OpenExplorer(McInstanceSelected.PathVersion)
        Catch ex As Exception
            Logger.Error(ex, "打开版本文件夹失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnDevMods_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevMods.Click
        Try
            If McInstanceSelected Is Nothing Then
                Hint("请先在启动页选择一个游戏版本！", HintType.Red, False)
                Return
            End If
            McInstanceSelected.Load()
            Dim ModsFolder As String = McInstanceSelected.PathIndie & "mods\"
            If Not DirectoryUtils.Exists(ModsFolder) Then DirectoryUtils.Create(ModsFolder)
            OpenExplorer(ModsFolder)
        Catch ex As Exception
            Logger.Error(ex, "打开 mods 文件夹失败", LogBehavior.Alert)
        End Try
    End Sub

    Private Sub BtnDevJson_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDevJson.Click
        Try
            If McInstanceSelected Is Nothing Then
                Hint("请先在启动页选择一个游戏版本！", HintType.Red, False)
                Return
            End If
            McInstanceSelected.Load()
            Dim JsonPath As String = McInstanceSelected.PathVersion & McInstanceSelected.Name & ".json"
            If Not FileUtils.Exists(JsonPath) Then
                Hint("未找到该版本的 JSON 文件！", HintType.Red, False)
                Return
            End If
            StartProcess(New ProcessStartInfo("notepad.exe", $"""{JsonPath}"""))
        Catch ex As Exception
            Logger.Error(ex, "打开版本 JSON 失败", LogBehavior.Alert)
        End Try
    End Sub

#End Region

#Region "彩蛋"

    ''' <summary>
    ''' 「哀悼日灰色色调」开关改变时立即刷新主题与背景图片。
    ''' </summary>
    Public Shared Sub EasterMourningChanged()
        ThemeRefresh()
        If FrmMain IsNot Nothing AndAlso FrmMain.IsLoaded Then PageSetupUI.BackgroundRefresh(False, True)
    End Sub

    Private Sub BtnEasterHue12_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnEasterHue12.Click
        Settings.Set("UiLauncherTheme", 12) 'OnChanged 会自动触发 ThemeRefresh(12)
        Hint("已切换到滑稽彩主题，颜色会随界面刷新缓慢轮换！", HintType.Blue)
    End Sub

    Private Sub BtnEasterHue13_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnEasterHue13.Click
        Settings.Set("UiLauncherTheme", 13)
        ThemeRefresh(13) '若原本就是欧皇彩，设置值不变不会触发 OnChanged，手动刷新以重新随机
        Hint("已切换到欧皇彩主题：本次随机色相 " & ColorHue & "°", HintType.Blue)
    End Sub

    Private Sub BtnEasterReal_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnEasterReal.Click
        If ThemeDontClick = 2 Then
            '停止真·滑稽彩，恢复所选主题
            ThemeDontClick = 0
            ThemeRefresh(Settings.Get(Of Integer)("UiLauncherTheme"))
            BtnEasterReal.Text = "真·滑稽彩"
            Hint("真·滑稽彩已停止，主题已恢复。")
        Else
            ThemeDontClick = 2
            ThemeRefresh()
            BtnEasterReal.Text = "停止真·滑稽彩"
            Hint("真·滑稽彩已启动，界面颜色将持续高速轮换！", HintType.Blue)
        End If
    End Sub

    'PLC 彩蛋：切换标题栏 Logo 为手绘 PLC，再次点击恢复
    Private Const PlcLogoData As String = "M26,29 v-25 h6 a7,7 180 0 1 0,14 h-6 M83,6.5 a10,11.5 180 1 0 0,18 M48,2.5 v24.5 h13.5"
    Private PlcLogoOriginal As String = Nothing
    Private Sub BtnEasterPlc_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnEasterPlc.Click
        Try
            If PlcLogoOriginal Is Nothing Then
                '首次点击：记录原始 Logo 并切换为 PLC
                PlcLogoOriginal = FrmMain.ShapeTitleLogo.Data.ToString()
                FrmMain.ShapeTitleLogo.Data = New GeometryConverter().ConvertFromString(PlcLogoData)
                BtnEasterPlc.Text = "恢复 Logo"
                Hint("PLC 彩蛋已触发，标题栏 Logo 已更换！", HintType.Blue)
            Else
                '再次点击：恢复原始 Logo
                FrmMain.ShapeTitleLogo.Data = New GeometryConverter().ConvertFromString(PlcLogoOriginal)
                PlcLogoOriginal = Nothing
                BtnEasterPlc.Text = "PLC 彩蛋"
                Hint("标题栏 Logo 已恢复。")
            End If
        Catch ex As Exception
            Logger.Error(ex, "切换 PLC 彩蛋失败", LogBehavior.Alert)
        End Try
    End Sub

#End Region

#Region "AI 助手"

    '确认已配置 API Key，未配置时提示
    Private Function CheckAiKey() As Boolean
        If ModAi.AiEnabled() Then Return True
        MyMsgBox("请先填写 API Key 再使用 AI 功能。" & vbCrLf &
                 "支持 OpenAI 兼容接口，例如 DeepSeek、Kimi、智谱 GLM 等，填写接口地址与模型名即可使用。", "AI 助手")
        Return False
    End Function

    Private Sub BtnAiTest_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAiTest.Click
        If Not CheckAiKey() Then Return
        RunInNewThread(AddressOf ModAi.AiTestConnection, "AI Test")
    End Sub

    Private Sub BtnAiLogAnalyze_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAiLogAnalyze.Click
        If Not CheckAiKey() Then Return
        RunInNewThread(AddressOf ModAi.AiAnalyzeError, "AI Analyze")
    End Sub

    Private Sub BtnAiMods_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAiMods.Click
        If Not CheckAiKey() Then Return
        RunInNewThread(AddressOf ModAi.AiSuggestMods, "AI Mods")
    End Sub

    Private Sub BtnAiGuide_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnAiGuide.Click
        If Not CheckAiKey() Then Return
        RunInNewThread(AddressOf ModAi.AiGuide, "AI Guide")
    End Sub

#End Region

#Region "导出 / 导入设置"

    Private Sub BtnSystemSettingExp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingExp.Click
        Hint("该功能尚在开发中！")
    End Sub
    Private Sub BtnSystemSettingImp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingImp.Click
        Hint("该功能尚在开发中！")
    End Sub

#End Region

End Class
