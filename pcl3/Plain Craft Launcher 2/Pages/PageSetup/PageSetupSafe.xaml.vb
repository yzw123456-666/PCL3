Public Class PageSetupSafe

    Private Sub PageSetupSafe_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        '重复加载部分
        PanBack.ScrollToHome()
        AniControlEnabled += 1
        SettingService.RefreshSettings(Me)
        RefreshLastScan()
        AniControlEnabled -= 1
    End Sub

    Public Sub Reset()
        Try
            SettingService.ResetSettings(Me)
            Logger.Info("已初始化安全页设置")
            Hint("已初始化安全页设置！", HintType.Green, False)
        Catch ex As Exception
            Logger.Error(ex, "初始化安全页设置失败", LogBehavior.Alert)
        End Try
        SettingService.RefreshSettings(Me)
        RefreshLastScan()
    End Sub

    ''' <summary>
    ''' 刷新「最近扫描」显示。
    ''' </summary>
    Public Sub RefreshLastScan()
        Dim Last As String = Settings.Get(Of String)("SystemSafeLastScan")
        If LabLastScan Is Nothing Then Return
        LabLastScan.Text = If(String.IsNullOrWhiteSpace(Last), "尚未进行过手动扫描。（防火墙开启时，每次启动游戏都会自动扫描）", "最近一次手动扫描：" & Last)
    End Sub

    '立即扫描
    Private Sub BtnSafeScan_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSafeScan.Click
        RunInNewThread(AddressOf ModFirewall.ManualScan, "Firewall Manual Scan")
    End Sub

    '白名单管理
    Private Sub BtnSafeWhitelist_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSafeWhitelist.Click
        Dim List = ModFirewall.Whitelist()
        If Not List.Any() Then
            MyMsgBox("白名单为空。" & vbCrLf & "在启动警告弹窗中选择「信任并启动」，即可将对应文件加入白名单。", "防火墙白名单")
            Return
        End If
        Select Case MyMsgBox("当前白名单（共 " & List.Count & " 项）：" & vbCrLf & vbCrLf &
                             "· " & List.Join(vbCrLf & "· ") & vbCrLf & vbCrLf &
                             "白名单内的文件不会再触发扫描警告。是否清空白名单？", "防火墙白名单", "保留", "清空")
            Case 2
                ModFirewall.WhitelistClear()
                Hint("白名单已清空。", HintType.Green)
        End Select
    End Sub

    '查看记录
    Private Sub BtnSafeLog_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSafeLog.Click
        If Not FileUtils.Exists(ModFirewall.FirewallLogFile) Then
            Hint("还没有防火墙记录。")
            Return
        End If
        OpenExplorer(ModFirewall.FirewallLogFile)
    End Sub

End Class
