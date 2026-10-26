''' <summary>
''' 更衣室相关的皮肤与披风操作。
''' </summary>
Public Module ModSkinRoom

    ''' <summary>
    ''' 本地皮肤库文件夹，以 \ 结尾。
    ''' </summary>
    Public ReadOnly Property SkinRoomFolder As String
        Get
            Return Paths.AppDataThenName & "Skins\"
        End Get
    End Property

    Public Class SkinRoomCape
        Public Id As String
        Public Name As String
        Public Url As String
        Public IsActive As Boolean
    End Class

    ''' <summary>
    ''' 当前是否已使用正版账号登录。
    ''' </summary>
    Public Function SkinRoomIsMsLogin() As Boolean
        Return Settings.Get(Of String)("CacheMsV2Access") <> ""
    End Function

    ''' <summary>
    ''' 从登录缓存中获取玩家当前拥有的所有披风。
    ''' </summary>
    Public Function SkinRoomGetCapes() As List(Of SkinRoomCape)
        Dim Result As New List(Of SkinRoomCape)
        Try
            Dim JsonText As String = Settings.Get(Of String)("CacheMsV2ProfileJson")
            If String.IsNullOrEmpty(JsonText) Then Return Result
            Dim Json As JObject = JsonText.DeserializeJson()
            Dim Capes As JToken = Json("capes")
            If Capes Is Nothing Then Return Result
            For Each Item As JToken In Capes
                Dim Cape As JObject = CType(Item, JObject)
                Dim Id As String = If(Cape("id")?.ToString, "")
                If Id = "" Then Continue For
                Dim CapeAlias As String = If(Cape("alias")?.ToString, "")
                Result.Add(New SkinRoomCape With {
                    .Id = Id,
                    .Name = SkinRoomCapeName(CapeAlias, Id),
                    .Url = If(Cape("url")?.ToString, ""),
                    .IsActive = Cape("state")?.ToString = "ACTIVE"
                })
            Next
        Catch ex As Exception
            Logger.Error(ex, "更衣室读取披风列表失败")
        End Try
        Return Result
    End Function

    Private Function SkinRoomCapeName(CapeAlias As String, Id As String) As String
        Select Case CapeAlias
            Case "Migrator" : Return "迁移者披风"
            Case "MapMaker" : Return "Realms 地图制作者披风"
            Case "Moderator" : Return "Mojira 管理员披风"
            Case "Translator-Chinese" : Return "Crowdin 中文翻译者披风"
            Case "Translator" : Return "Crowdin 翻译者披风"
            Case "Cobalt" : Return "Cobalt 披风"
            Case "Vanilla" : Return "原版披风"
            Case "Minecon2011" : Return "Minecon 2011 参与者披风"
            Case "Minecon2012" : Return "Minecon 2012 参与者披风"
            Case "Minecon2013" : Return "Minecon 2013 参与者披风"
            Case "Minecon2015" : Return "Minecon 2015 参与者披风"
            Case "Minecon2016" : Return "Minecon 2016 参与者披风"
            Case "Cherry Blossom" : Return "樱花披风"
            Case "15th Anniversary" : Return "15 周年纪念披风"
            Case "Purple Heart" : Return "紫色心形披风"
            Case "Follower's" : Return "追随者披风"
            Case "MCC 15th Year" : Return "MCC 15 周年披风"
            Case "Minecraft Experience" : Return "村民救援披风"
            Case "Mojang Office" : Return "Mojang 办公室披风"
            Case "Home" : Return "家园披风"
            Case "Menace" : Return "入侵披风"
            Case "Yearn" : Return "渴望披风"
            Case "Common" : Return "普通披风"
            Case "Pan" : Return "薄煎饼披风"
            Case "Founder's" : Return "创始人披风"
            Case "Copper" : Return "铜披风"
            Case "Zombie Horse" : Return "僵尸马披风"
            Case "Builder" : Return "建造者披风"
            Case "Crafter" : Return "工匠披风"
            Case "Aurora" : Return "极光披风"
            Case Else : Return If(String.IsNullOrEmpty(CapeAlias), Id, CapeAlias)
        End Select
    End Function

    ''' <summary>
    ''' 获取披风纹理的本地路径，必要时会进行下载。失败时返回空字符串。
    ''' </summary>
    Public Function SkinRoomGetCapeTexture(Cape As SkinRoomCape) As String
        If Cape Is Nothing OrElse String.IsNullOrEmpty(Cape.Url) Then Return Nothing
        Try
            Dim Folder As String = PathTemp & "Cache\Cape\"
            DirectoryUtils.Create(Folder)
            Dim File As String = Folder & Cape.Id & ".png"
            If Not FileUtils.Exists(File) Then
                Dim Downloading As String = File & ".PCLDownloading"
                If FileUtils.Exists(Downloading) Then FileUtils.Delete(Downloading)
                NetDownloadByClient(Cape.Url, Downloading)
                If FileUtils.Exists(File) Then FileUtils.Delete(File)
                FileUtils.Move(Downloading, File)
                Logger.Info($"披风纹理下载成功：{Cape.Name}")
            End If
            Return File
        Catch ex As Exception
            Logger.Error(ex, $"更衣室下载披风纹理失败（{Cape.Name}）")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' 更改正版账号当前使用的披风，CapeId 为空时取下披风。
    ''' </summary>
    Public Sub SkinRoomApplyCape(CapeId As String)
        If Not SkinRoomIsMsLogin() Then
            Hint("请先在启动页使用正版账号登录，才能更换披风！", HintType.Red)
            Return
        End If
        Hint("正在更改披风……")
        RunInNewThread(
        Sub()
            Try
                If McLoginMsLoader.State <> LoadState.Finished Then McLoginMsLoader.WaitForExit(PageLoginMsSkin.GetLoginData())
                If McLoginMsLoader.State <> LoadState.Finished Then
                    Hint("登录失败，无法更改披风！", HintType.Red)
                    Return
                End If
                Dim Result As String = NetRequestByClientRetry("https://api.minecraftservices.com/minecraft/profile/capes/active",
                    If(String.IsNullOrEmpty(CapeId), HttpMethod.Delete, HttpMethod.Put),
                    Content:=If(String.IsNullOrEmpty(CapeId), "", New JObject(New JProperty("capeId", CapeId)).ToString(0)),
                    ContentType:="application/json",
                    Headers:={{"Authorization", "Bearer " & McLoginMsLoader.Output.AccessToken}})
                If Result.Contains("""errorMessage""") Then
                    Hint("更改披风失败：" & Result.DeserializeJson()("errorMessage").ToString, HintType.Red)
                    Return
                End If
                '更新本地缓存，避免再次打开更衣室时显示旧的披风
                Dim JsonText As String = Settings.Get(Of String)("CacheMsV2ProfileJson")
                If Not String.IsNullOrEmpty(JsonText) Then
                    Dim Json As JObject = JsonText.DeserializeJson()
                    If Json("capes") IsNot Nothing Then
                        For Each Item As JToken In Json("capes")
                            Dim Cape As JObject = CType(Item, JObject)
                            Cape("state") = If(Cape("id")?.ToString = CapeId, "ACTIVE", "INACTIVE")
                        Next
                    End If
                    Settings.Set("CacheMsV2ProfileJson", Json.ToString())
                    If McLoginMsLoader.State = LoadState.Finished Then McLoginMsLoader.Output.ProfileJson = Json.ToString()
                End If
                Hint("更改披风成功！", HintType.Green)
            Catch ex As Exception
                Logger.Error(ex, "更改披风失败", LogBehavior.Toast)
            End Try
        End Sub, "Cape Change")
    End Sub

    ''' <summary>
    ''' 应用皮肤。已登录正版账号时上传到 Mojang，否则作为离线自定义皮肤使用。
    ''' </summary>
    Public Sub SkinRoomApplySkin(FilePath As String, IsSlim As Boolean)
        If String.IsNullOrEmpty(FilePath) OrElse Not FileUtils.Exists(FilePath) Then
            Hint("请先选择一个皮肤！", HintType.Red)
            Return
        End If
        If Not SkinRoomIsMsLogin() Then
            Try
                Dim Dest As String = Paths.AppDataThenName & "CustomSkin.png"
                If FileUtils.Exists(Dest) Then FileUtils.Delete(Dest)
                FileUtils.Copy(FilePath, Dest)
                Settings.Set("LaunchSkinType", 4)
                PageLaunchLeft.SkinLegacy.WaitForExit(IsForceRestart:=True)
                Hint("已将此皮肤应用为离线皮肤！", HintType.Green)
            Catch ex As Exception
                Logger.Error(ex, "应用离线皮肤失败", LogBehavior.Toast)
            End Try
            Return
        End If
        Hint("正在应用皮肤……")
        RunInNewThread(
        Sub()
            Try
                If McLoginMsLoader.State <> LoadState.Finished Then McLoginMsLoader.WaitForExit(PageLoginMsSkin.GetLoginData())
                If McLoginMsLoader.State <> LoadState.Finished Then
                    Hint("登录失败，无法更改皮肤！", HintType.Red)
                    Return
                End If
                Dim Result As String = NetRequestByClientRetry("https://api.minecraftservices.com/minecraft/profile/skins", HttpMethod.Post,
                    Content:=New Net.Http.MultipartFormDataContent From {
                        {New Net.Http.StringContent(If(IsSlim, "slim", "classic")), "variant"},
                        {New Net.Http.ByteArrayContent(FileUtils.ReadAsBytes(FilePath)), "file", PathUtils.GetLastPart(FilePath)}
                    },
                    Headers:={{"Authorization", "Bearer " & McLoginMsLoader.Output.AccessToken}, {"Accept", "*/*"}, {"User-Agent", "MojangSharp/0.1"}})
                Dim Json As JObject = Result.DeserializeJson()
                If Json.ContainsKey("errorMessage") Then Throw New Exception(Json("errorMessage").ToString)
                For Each Item As JToken In Json("skins")
                    If Item("state")?.ToString = "ACTIVE" Then
                        MySkin.ReloadCache(Item("url").ToString)
                        Return
                    End If
                Next
                Hint("更改皮肤成功！", HintType.Green)
            Catch ex As Exception
                Logger.Error(ex, "更改皮肤失败", LogBehavior.Toast)
            End Try
        End Sub, "Skin Upload")
    End Sub

    ''' <summary>
    ''' 判断皮肤是否使用 Alex 细手臂模型。
    ''' </summary>
    Public Function SkinRoomIsSlim(FilePath As String) As Boolean
        Try
            Dim Image As New MyBitmap(FilePath)
            If Image.Pic.Width < 64 OrElse Image.Pic.Height < 64 Then Return False
            '在 64x64 皮肤中，细手臂模型右臂只有 3 像素宽，第 54 列不会被使用
            Return Image.Pic.GetPixel(54, 20).A = 0 OrElse Image.Pic.GetPixel(55, 20).A = 0
        Catch ex As Exception
            Return False
        End Try
    End Function

End Module
