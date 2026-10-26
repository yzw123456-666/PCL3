'由于包含加解密等安全信息，本文件中的部分代码已被删除
'PCL3：已移除全部匿名数据上报（遥测）相关代码，启动器不会向任何服务器上传数据。

Friend Module ModSecret

    '标注 PCL 的不同分支，仅用于替换标记
    Public Const VersionBranchMain As String = "PCL3"
    '在开源版的注册表与常规版的注册表隔离，以防数据冲突
    Public Const RegFolder As String = "PCLDebug"
    '用于微软登录的 ClientId
    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "")
    'CurseForge API Key
    Public CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")

#Region "网络鉴权"

    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers（UA、Referer）。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")  '#4951
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"PCL3/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"PCL3/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl3.server/")
        If Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net") Then Req.Headers.Add("x-api-key", CurseForgeAPIKey)
    End Sub

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeDontClick As Integer = 0

    ''' <summary>
    ''' 是否为 PCL2 下架哀悼日（每年 10 月 20 日）。
    ''' 这一天启动器会在启动时弹出默哀提示，并（默认）切换为灰色色调。
    ''' 设置环境变量 PCL3_FORCE_MOURNING = 1 可在任意日期强制启用，仅用于调试。
    ''' </summary>
    Public ReadOnly IsMourningDay As Boolean =
        Environment.GetEnvironmentVariable("PCL3_FORCE_MOURNING") = "1" OrElse (Now.Month = 10 AndAlso Now.Day = 20)

    ''' <summary>
    ''' 哀悼日是否应用灰色色调。
    ''' 受 设置 → 其他 → 彩蛋 中的「哀悼日灰色色调」开关控制（默认开启）。
    ''' </summary>
    Public ReadOnly Property IsMourningGray As Boolean
        Get
            Return IsMourningDay AndAlso Settings.Get(Of Boolean)("SystemEasterMourningGray")
        End Get
    End Property

    ''' <summary>
    ''' 根据主题 ID 计算配色参数。
    ''' </summary>
    Private Sub ThemeApply(Id As Integer)
        Select Case Id
            Case 0 '龙猫蓝
                ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            Case 1 '甜柠青
                ColorHue = 172 : ColorSat = 70 : ColorLightAdjust = 2 : ColorHueTopbarDelta = 0
            Case 2 '小草绿
                ColorHue = 140 : ColorSat = 65 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            Case 3 '菠萝黄
                ColorHue = 45 : ColorSat = 95 : ColorLightAdjust = -2 : ColorHueTopbarDelta = 0
            Case 4 '橡木棕
                ColorHue = 28 : ColorSat = 45 : ColorLightAdjust = -8 : ColorHueTopbarDelta = 0
            Case 5 '玄素黑
                ColorHue = 210 : ColorSat = 4 : ColorLightAdjust = -22 : ColorHueTopbarDelta = 0
            Case 6 '铁杆粉
                ColorHue = 330 : ColorSat = 72 : ColorLightAdjust = 2 : ColorHueTopbarDelta = 0
            Case 7 '神秘紫
                ColorHue = 265 : ColorSat = 65 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            Case 8 '秋仪金
                ColorHue = 38 : ColorSat = 70 : ColorLightAdjust = -4 : ColorHueTopbarDelta = 0
            Case 9 '活跃橙
                ColorHue = 25 : ColorSat = 90 : ColorLightAdjust = 2 : ColorHueTopbarDelta = 0
            Case 10 '跳票红
                ColorHue = 355 : ColorSat = 75 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            Case 11 '极客蓝
                ColorHue = 195 : ColorSat = 95 : ColorLightAdjust = -6 : ColorHueTopbarDelta = -25
            Case 12 '滑稽彩（轮换速度可在 设置 → 其他 → 彩蛋 中调整）
                Dim HueStep As Integer = Math.Max(1, Settings.Get(Of Integer)("SystemEasterHueStep"))
                ColorHue = (ColorHue + HueStep) Mod 360 : ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            Case 13 '欧皇彩（随机色在选中时生成）
                ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
            Case 14 '自定义
                ColorHue = Settings.Get(Of Integer)("UiLauncherHue")
                ColorSat = Settings.Get(Of Integer)("UiLauncherSat")
                ColorHueTopbarDelta = Settings.Get(Of Integer)("UiLauncherDelta") - 90
                ColorLightAdjust = Settings.Get(Of Integer)("UiLauncherLight") - 20
            Case Else
                ColorHue = 210 : ColorSat = 85 : ColorLightAdjust = 0 : ColorHueTopbarDelta = 0
        End Select
    End Sub

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            If ThemeNow = NewTheme AndAlso NewTheme >= 0 AndAlso NewTheme <> 12 AndAlso NewTheme <> 13 Then Return
            If NewTheme >= 0 Then
                ThemeNow = NewTheme
                If NewTheme = 13 Then ColorHue = RandomInteger(0, 359)
            End If
            If ThemeNow >= 0 Then ThemeApply(ThemeNow)

            'PCL2 下架哀悼日：整个启动器切换为灰色色调（去饱和），可在 设置 → 其他 → 彩蛋 中关闭
            If IsMourningGray Then ColorSat = 0

            Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
            Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
            Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
            Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
            Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
            Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
            Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
            Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
            ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
            ColorBg1 = New MyColor(190, Color7)

            ColorSemiTransparent = New MyColor(1, Color8)
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(Color1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(Color2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(Color3)
            Application.Current.Resources("ColorBrush4") = New SolidColorBrush(Color4)
            Application.Current.Resources("ColorBrush5") = New SolidColorBrush(Color5)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(Color6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(Color7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(Color8)
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorObject1") = CType(Color1, Color)
            Application.Current.Resources("ColorObject2") = CType(Color2, Color)
            Application.Current.Resources("ColorObject3") = CType(Color3, Color)
            Application.Current.Resources("ColorObject4") = CType(Color4, Color)
            Application.Current.Resources("ColorObject5") = CType(Color5, Color)
            Application.Current.Resources("ColorObject6") = CType(Color6, Color)
            Application.Current.Resources("ColorObject7") = CType(Color7, Color)
            Application.Current.Resources("ColorObject8") = CType(Color8, Color)
            Application.Current.Resources("ColorObjectBg0") = CType(ColorBg0, Color)
            Application.Current.Resources("ColorObjectBg1") = CType(ColorBg1, Color)
            ThemeRefreshMain()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub
    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Return
            '顶部条背景
            Dim Brush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + Deltas(0), ColorSat, 48 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + Deltas(1), ColorSat, 54 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + Deltas(2), ColorSat, 48 + ColorLightAdjust)})
            FrmMain.PanTitle.Background = Brush
            FrmMain.PanTitle.Background.Freeze()
            '主页面背景
            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Brush = New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
                Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, ColorSat * 0.8, 91)})
                FrmMain.PanForm.Background = Brush
            Else
                FrmMain.PanForm.Background = New MyColor(245, 245, 245)
            End If
            FrmMain.PanForm.Background.Freeze()
        End Sub)
    End Sub
    ''' <summary>
    ''' 将图片转换为灰度（按亮度加权），用于哀悼日的背景图片。
    ''' </summary>
    Public Function GrayscaleImage(Source As System.Drawing.Bitmap) As System.Drawing.Bitmap
        If Source Is Nothing Then Return Nothing
        Dim Target As New System.Drawing.Bitmap(Source.Width, Source.Height)
        Using G As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(Target)
            Dim Matrix As New System.Drawing.Imaging.ColorMatrix(New Single()() {
                New Single() {0.299F, 0.299F, 0.299F, 0},
                New Single() {0.587F, 0.587F, 0.587F, 0},
                New Single() {0.114F, 0.114F, 0.114F, 0},
                New Single() {0, 0, 0, 1}})
            Using Attr As New System.Drawing.Imaging.ImageAttributes
                Attr.SetColorMatrix(Matrix)
                G.DrawImage(Source, New System.Drawing.Rectangle(0, 0, Source.Width, Source.Height),
                            0, 0, Source.Width, Source.Height, System.Drawing.GraphicsUnit.Pixel, Attr)
            End Using
        End Using
        Return Target
    End Function

    ''' <summary>
    ''' 检查主题解锁状态。
    ''' PCL3：所有主题默认解锁，不再需要赞助或任何条件。
    ''' </summary>
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    ''' <summary>
    ''' 检查单个主题是否可用。PCL3：恒为可用。
    ''' </summary>
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    ''' <summary>
    ''' 尝试解锁主题。PCL3：所有主题默认可用，因此返回 False（无需解锁）。
    ''' </summary>
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    Friend Sub UpdateCheckByButton()
        Hint("该版本中不包含更新功能……")
    End Sub

    Friend IsUpdateWaitingRestart As Boolean = False
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
    End Sub

    ''' <summary>
    ''' 确保 PathTemp/Latest.exe 是最新正式版的 PCL，它会被用于整合包打包。
    ''' 如果不是，则下载一个。
    ''' </summary>
    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)
        '注意：如果要自行实现这个功能，请换用另一个文件路径，以免与官方版本冲突
    End Sub

#End Region

#Region "联网配置"

    ''' <summary>
    ''' 联网获取的配置信息。
    ''' 若获取失败或仍在获取中，可能为 Nothing。
    ''' </summary>
    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", Sub() Logger.Info("该版本中不包含更新通知功能……"), Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60} '超时 1 小时

#End Region

#Region "赞助等级"

    'PCL3：已移除全部赞助等级与土豆码逻辑
    Public ReadOnly Property CurrentRank As DonationRank
        Get
            Return DonationRank.None
        End Get
    End Property

    ''' <summary>
    ''' 获取设备识别码。
    ''' </summary>
    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

#End Region

End Module
