Public Class PageSetupSkin

    Private SelectedSkin As String = ""
    Private SelectedCape As ModSkinRoom.SkinRoomCape

    Private Sub PageSetupSkin_Enter() Handles Me.PageEnter
        RefreshAvailability()
        If Not IsOfflineLogin OrElse String.IsNullOrEmpty(SelectedSkin) OrElse SelectedSkin.StartsWithF(PathImage) Then SelectedSkin = GetCurrentSkinPath()
        LoadSkinList()
        LoadSkin(SelectedSkin)
        If ItemCape.Checked Then
            LoadCapeList()
        Else
            PreviewActiveCape()
        End If
    End Sub

    ''' <summary>
    ''' 当前是否为离线登录。只有离线登录才能更换皮肤与披风，其它登录方式仅能查看。
    ''' </summary>
    Private ReadOnly Property IsOfflineLogin As Boolean
        Get
            Return Settings.Get(Of McLoginType)("LoginType") = McLoginType.Legacy
        End Get
    End Property

    '布局

    Private Sub PanBack_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanBack.SizeChanged
        UpdateFillHeight()
    End Sub

    ''' <summary>
    ''' 让更衣室卡片填满页面下方的剩余空间。
    ''' </summary>
    Private Sub UpdateFillHeight()
        If PanMain Is Nothing OrElse CardSkin Is Nothing Then Return
        Dim Spacing As Double = PanMain.Margin.Top + PanMain.Margin.Bottom + CardSkin.Margin.Top + CardSkin.Margin.Bottom
        PanMain.MinHeight = Math.Max(0, PanBack.ViewportHeight - Spacing - 1)
    End Sub

    Private Sub RefreshAvailability()
        Dim Visible As Visibility = If(IsOfflineLogin, Visibility.Visible, Visibility.Collapsed)
        BtnSkinImport.Visibility = Visible
        BtnSkinApply.Visibility = Visible
        BtnCapeApply.Visibility = Visible
        BtnCapeRefresh.Visibility = Visible
    End Sub

    '皮肤页

    Private Sub ItemSkin_Check(sender As MyListItem, e As EventArgs) Handles ItemSkin.Check
        If PanSkin Is Nothing OrElse PanCape Is Nothing Then Return
        PanSkin.Visibility = Visibility.Visible
        PanCape.Visibility = Visibility.Collapsed
        PanSkinScroll.Visibility = Visibility.Visible
        PanCapeScroll.Visibility = Visibility.Collapsed
    End Sub
    Private Sub ItemCape_Check(sender As MyListItem, e As EventArgs) Handles ItemCape.Check
        If PanSkin Is Nothing OrElse PanCape Is Nothing Then Return
        PanSkin.Visibility = Visibility.Collapsed
        PanCape.Visibility = Visibility.Visible
        PanSkinScroll.Visibility = Visibility.Collapsed
        PanCapeScroll.Visibility = Visibility.Visible
        LoadCapeList()
    End Sub

    ''' <summary>
    ''' 获取当前账号实际使用的皮肤文件路径。
    ''' </summary>
    Private Function GetCurrentSkinPath() As String
        Try
            Dim Loader As LoaderTask(Of (String, String), String)
            Select Case Settings.Get(Of McLoginType)("LoginType")
                Case McLoginType.Legacy : Loader = PageLaunchLeft.SkinLegacy
                Case McLoginType.Ms : Loader = PageLaunchLeft.SkinMs
                Case McLoginType.Nide : Loader = PageLaunchLeft.SkinNide
                Case Else : Loader = PageLaunchLeft.SkinAuth
            End Select
            If Loader IsNot Nothing AndAlso Loader.State = LoadState.Finished AndAlso FileUtils.Exists(Loader.Output) Then Return Loader.Output
        Catch ex As Exception
            Logger.Warn(ex, "更衣室获取当前皮肤失败")
        End Try
        Dim Custom As String = Paths.AppDataThenName & "CustomSkin.png"
        If Settings.Get(Of Integer)("LaunchSkinType") = 4 AndAlso FileUtils.Exists(Custom) Then Return Custom
        Return PathImage & "Skins/Steve.png"
    End Function

    Private Sub LoadSkin(FilePath As String)
        If String.IsNullOrEmpty(FilePath) Then Return
        Try
            Dim Slim As Boolean = ModSkinRoom.SkinRoomIsSlim(FilePath)
            Viewer.SetSkin(FilePath, Slim)
            SelectedSkin = FilePath
            Dim Location As String = If(FilePath.StartsWithF(PathImage), "默认皮肤", PathUtils.GetLastPart(FilePath))
            LabSkinHint.Text = $"当前预览：{Location}（{If(Slim, "Alex 细手臂模型", "Steve 粗手臂模型")}）" &
                If(IsOfflineLogin, "", "　·　当前登录方式只能查看皮肤，无法切换")
            RefreshSkinSelection()
        Catch ex As Exception
            Logger.Error(ex, "更衣室预览皮肤失败")
        End Try
    End Sub

    Private Sub LoadSkinList()
        PanSkinList.Children.Clear()
        If Not IsOfflineLogin Then
            PanSkinList.Children.Add(GetSkinItem(GetCurrentSkinPath(), IsReadOnly:=True))
            Return
        End If
        Try
            DirectoryUtils.Create(ModSkinRoom.SkinRoomFolder)
            Dim Files As List(Of String) = DirectoryUtils.EnumerateFiles(ModSkinRoom.SkinRoomFolder, searchPattern:="*.png").ToList()
            If Files.Count = 0 Then
                PanSkinList.Children.Add(New TextBlock With {
                    .Text = "皮肤库还是空的，点击「导入皮肤」添加吧～",
                    .FontSize = 12,
                    .Margin = New Thickness(2, 8, 0, 0),
                    .Foreground = New SolidColorBrush(Color.FromRgb(128, 128, 128))
                })
                Return
            End If
            For Each File In Files
                PanSkinList.Children.Add(GetSkinItem(File))
            Next
        Catch ex As Exception
            Logger.Error(ex, "更衣室读取皮肤库失败")
        End Try
    End Sub

    Private Function GetSkinItem(FilePath As String, Optional IsReadOnly As Boolean = False) As FrameworkElement
        Dim Picture As New Image With {.Width = 36, .Height = 36, .Stretch = Stretch.Fill, .VerticalAlignment = VerticalAlignment.Center}
        RenderOptions.SetBitmapScalingMode(Picture, BitmapScalingMode.NearestNeighbor)
        Try
            Dim Image As New MyBitmap(FilePath)
            Dim Scale As Integer = Math.Max(1, Image.Pic.Width \ 64)
            Picture.Source = CType(Image.Clip(8 * Scale, 8 * Scale, 8 * Scale, 8 * Scale), ImageSource)
        Catch ex As Exception
            Logger.Warn(ex, $"更衣室读取皮肤缩略图失败（{FilePath}）")
        End Try
        Dim Texts As New StackPanel With {.Margin = New Thickness(8, 0, 0, 0), .VerticalAlignment = VerticalAlignment.Center}
        Texts.Children.Add(New TextBlock With {
            .Text = Path.GetFileNameWithoutExtension(FilePath),
            .FontSize = 12,
            .TextTrimming = TextTrimming.CharacterEllipsis
        })
        Texts.Children.Add(New TextBlock With {
            .Text = If(ModSkinRoom.SkinRoomIsSlim(FilePath), "Alex 细手臂", "Steve 粗手臂"),
            .FontSize = 10,
            .Margin = New Thickness(0, 2, 0, 0),
            .Foreground = New SolidColorBrush(Color.FromRgb(128, 128, 128))
        })
        Dim Content As New StackPanel With {.Orientation = Orientation.Horizontal}
        Content.Children.Add(Picture)
        Content.Children.Add(Texts)
        Dim Root As New Border With {
            .Child = Content,
            .Tag = FilePath,
            .Margin = New Thickness(0, 0, 0, 4),
            .Padding = New Thickness(6),
            .CornerRadius = New CornerRadius(6),
            .Background = Brushes.Transparent,
            .Cursor = If(IsReadOnly, Cursors.Arrow, Cursors.Hand),
            .ToolTip = If(IsReadOnly, "当前账号的皮肤只能查看，无法更换", "左键预览，右键删除")
        }
        If Not IsReadOnly Then
            AddHandler Root.MouseLeftButtonUp, Sub(sender As Object, e As MouseButtonEventArgs) LoadSkin(FilePath)
            AddHandler Root.MouseRightButtonUp, Sub(sender As Object, e As MouseButtonEventArgs) DeleteSkin(FilePath)
        End If
        Return Root
    End Function

    Private Sub RefreshSkinSelection()
        For Each Child As Object In PanSkinList.Children
            If TypeOf Child Is Border Then
                Dim Root As Border = CType(Child, Border)
                Root.Background = If(Equals(Root.Tag, SelectedSkin), New SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), Brushes.Transparent)
            End If
        Next
    End Sub

    Private Sub DeleteSkin(FilePath As String)
        If MyMsgBox($"确定要删除皮肤「{Path.GetFileNameWithoutExtension(FilePath)}」吗？该操作不可撤销。", "删除确认", "删除", "取消", IsWarn:=True) <> 1 Then Return
        Try
            FileUtils.Delete(FilePath)
            If Equals(SelectedSkin, FilePath) Then
                SelectedSkin = ""
                Viewer.SetSkin("")
                LabSkinHint.Text = "皮肤已删除，点击「导入皮肤」添加新的皮肤吧～"
            End If
            LoadSkinList()
            RefreshSkinSelection()
        Catch ex As Exception
            Logger.Error(ex, $"更衣室删除皮肤失败（{FilePath}）", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub BtnSkinImport_Click(sender As Object, e As MouseButtonEventArgs)
        If Not IsOfflineLogin Then
            Hint("当前登录方式只能查看皮肤，请使用离线登录后再更换皮肤！", HintType.Red)
            Return
        End If
        Dim Source As String = Dialogs.SelectFile("选择皮肤文件", False, filter:={({"png", "jpg", "jpeg", "webp"}, "皮肤图片文件")}).FirstOrDefault()
        If String.IsNullOrEmpty(Source) Then Return
        Try
            Dim Image As New MyBitmap(Source)
            If Image.Pic.Width <> 64 OrElse (Image.Pic.Height <> 32 AndAlso Image.Pic.Height <> 64) Then
                Hint("皮肤图片大小应为 64x32 像素或 64x64 像素！", HintType.Red)
                Return
            End If
            DirectoryUtils.Create(ModSkinRoom.SkinRoomFolder)
            Dim Name As String = Path.GetFileNameWithoutExtension(Source)
            Dim Dest As String = ModSkinRoom.SkinRoomFolder & Name & ".png"
            If FileUtils.Exists(Dest) Then Dest = ModSkinRoom.SkinRoomFolder & Name & "_" & RandomInteger(1000, 9999) & ".png"
            FileUtils.Copy(Source, Dest)
            LoadSkinList()
            LoadSkin(Dest)
            Hint("皮肤已添加到皮肤库！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, $"更衣室导入皮肤失败（{Source}）", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub BtnSkinApply_Click(sender As Object, e As MouseButtonEventArgs)
        If Not IsOfflineLogin Then
            Hint("当前登录方式只能查看皮肤，请使用离线登录后再更换皮肤！", HintType.Red)
            Return
        End If
        If String.IsNullOrEmpty(SelectedSkin) Then
            Hint("请先选择一个皮肤！", HintType.Red)
            Return
        End If
        Dim Target As String = SelectedSkin
        If Target.StartsWithF(PathImage) Then
            '内置皮肤不是实际文件，需要先导出
            Try
                Target = PathTemp & "SkinApply.png"
                If FileUtils.Exists(Target) Then FileUtils.Delete(Target)
                Dim Image As New MyBitmap(SelectedSkin)
                Image.Save(Target)
            Catch ex As Exception
                Logger.Error(ex, "更衣室导出内置皮肤失败", LogBehavior.Toast)
                Return
            End Try
        End If
        ModSkinRoom.SkinRoomApplySkin(Target, ModSkinRoom.SkinRoomIsSlim(SelectedSkin))
    End Sub

    Private Sub BtnSkinSave_Click(sender As Object, e As MouseButtonEventArgs)
        If String.IsNullOrEmpty(SelectedSkin) Then
            Hint("请先选择一个皮肤！", HintType.Red)
            Return
        End If
        Try
            Dim Dest As String = Dialogs.SaveFile("选取保存皮肤的位置", Path.GetFileName(SelectedSkin), filter:={("png", "皮肤图片文件")})
            If String.IsNullOrEmpty(Dest) Then Return
            If FileUtils.Exists(Dest) Then FileUtils.Delete(Dest)
            If SelectedSkin.StartsWithF(PathImage) Then
                Dim Image As New MyBitmap(SelectedSkin)
                Image.Save(Dest)
            Else
                FileUtils.Copy(SelectedSkin, Dest)
            End If
            Hint("皮肤保存成功！", HintType.Green)
        Catch ex As Exception
            Logger.Error(ex, "更衣室保存皮肤失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub BtnSkinReset_Click(sender As Object, e As MouseButtonEventArgs)
        Viewer.ResetView()
    End Sub

    '披风页

    Private Sub PreviewActiveCape()
        Try
            Dim ActiveCape As ModSkinRoom.SkinRoomCape = ModSkinRoom.SkinRoomGetCapes().FirstOrDefault(Function(Target) Target.IsActive)
            If ActiveCape Is Nothing Then
                Viewer.SetCape("")
                Return
            End If
            Dim Cape As ModSkinRoom.SkinRoomCape = ActiveCape
            RunInThread(
            Sub()
                Dim Texture As String = ModSkinRoom.SkinRoomGetCapeTexture(Cape)
                RunInUi(Sub() Viewer.SetCape(If(Texture, "")))
            End Sub)
        Catch ex As Exception
            Logger.Error(ex, "更衣室预览披风失败")
        End Try
    End Sub

    Private Sub LoadCapeList()
        PanCapeList.Children.Clear()
        SelectedCape = Nothing
        If Not ModSkinRoom.SkinRoomIsMsLogin() Then
            LabCapeHint.Text = If(IsOfflineLogin, "离线登录无法获取披风列表，请使用正版账号登录后再试。", "当前登录方式无法获取披风列表。")
            Return
        End If
        Try
            Dim Capes As List(Of ModSkinRoom.SkinRoomCape) = ModSkinRoom.SkinRoomGetCapes()
            LabCapeHint.Text = If(Capes.Count = 0, "这个账号目前还没有任何披风。",
                $"共 {Capes.Count} 个披风。" & If(IsOfflineLogin, "左键预览，选中后点击「应用所选披风」。", "当前登录方式只能查看披风，无法更换。"))
            Dim NoneItem As Border = GetCapeItem(Nothing, "无披风")
            PanCapeList.Children.Add(NoneItem)
            For Each Cape In Capes
                PanCapeList.Children.Add(GetCapeItem(Cape, If(Cape.IsActive, Cape.Name & "（使用中）", Cape.Name)))
            Next
            SelectedCape = Capes.FirstOrDefault(Function(Target) Target.IsActive)
            RefreshCapeSelection()
            PreviewActiveCape()
        Catch ex As Exception
            Logger.Error(ex, "更衣室读取披风列表失败")
        End Try
    End Sub

    Private Function GetCapeItem(Cape As ModSkinRoom.SkinRoomCape, Text As String) As FrameworkElement
        Dim Label As New TextBlock With {
            .Text = Text,
            .FontSize = 12,
            .TextWrapping = TextWrapping.Wrap,
            .VerticalAlignment = VerticalAlignment.Center
        }
        Dim Root As New Border With {
            .Child = Label,
            .Tag = Cape,
            .Margin = New Thickness(0, 0, 0, 4),
            .Padding = New Thickness(8, 7, 8, 7),
            .CornerRadius = New CornerRadius(6),
            .Background = Brushes.Transparent,
            .Cursor = Cursors.Hand
        }
        AddHandler Root.MouseLeftButtonUp,
            Sub(sender As Object, e As MouseButtonEventArgs)
                SelectedCape = Cape
                RefreshCapeSelection()
                If Cape Is Nothing Then
                    Viewer.SetCape("")
                Else
                    RunInThread(
                    Sub()
                        Dim Texture As String = ModSkinRoom.SkinRoomGetCapeTexture(Cape)
                        RunInUi(Sub() Viewer.SetCape(If(Texture, "")))
                    End Sub)
                End If
            End Sub
        Return Root
    End Function

    Private Sub RefreshCapeSelection()
        For Each Child As Object In PanCapeList.Children
            If TypeOf Child Is Border Then
                Dim Root As Border = CType(Child, Border)
                Root.Background = If(Root.Tag Is SelectedCape, New SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), Brushes.Transparent)
            End If
        Next
    End Sub

    Private Sub BtnCapeRefresh_Click(sender As Object, e As MouseButtonEventArgs)
        RunInThread(
        Sub()
            Try
                Hint("正在刷新披风列表……")
                If McLoginMsLoader.State = LoadState.Loading Then
                    McLoginMsLoader.WaitForExit()
                Else
                    McLoginMsLoader.WaitForExit(PageLoginMsSkin.GetLoginData(), IsForceRestart:=True)
                End If
                RunInUi(Sub()
                            LoadCapeList()
                            Hint("已刷新披风列表！", HintType.Green)
                        End Sub)
            Catch ex As Exception
                Logger.Error(ex, "刷新披风列表失败", LogBehavior.Toast)
            End Try
        End Sub)
    End Sub

    Private Sub BtnCapeApply_Click(sender As Object, e As MouseButtonEventArgs)
        If Not IsOfflineLogin Then
            Hint("当前登录方式只能查看披风，无法更换。", HintType.Red)
            Return
        End If
        ModSkinRoom.SkinRoomApplyCape(If(SelectedCape Is Nothing, "", SelectedCape.Id))
    End Sub

    Private Sub BtnCapeReset_Click(sender As Object, e As MouseButtonEventArgs)
        Viewer.ResetView()
    End Sub

End Class
