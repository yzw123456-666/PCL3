Imports System.Windows.Media.Media3D

''' <summary>
''' 用于渲染 3D 皮肤与披风的控件。支持拖动旋转与滚轮缩放。
''' </summary>
Public Class MySkinViewer

    '皮肤纹理
    Private SkinTexture As MyBitmap
    Private CapeTexture As MyBitmap
    Private SkinScale As Integer = 1
    Private IsSlimSkin As Boolean = False

    '视角
    Private AngleY As Double = 35
    Private AngleX As Double = 0
    Private Zoom As Double = 1
    Private ReadOnly RotateY As New AxisAngleRotation3D(New Vector3D(0, 1, 0), 35)
    Private ReadOnly RotateX As New AxisAngleRotation3D(New Vector3D(1, 0, 0), 0)
    Private ReadOnly Parts As New List(Of Model3D)
    '叠加层（半透明）最后绘制，避免其深度写入挡住较早绘制的基础部件
    Private ReadOnly PendingOverlays As New List(Of Model3D)

    Public Sub New()
        InitializeComponent()
        Dim Group As New Transform3DGroup()
        Group.Children.Add(New RotateTransform3D(RotateY))
        Group.Children.Add(New RotateTransform3D(RotateX))
        Model.Transform = Group
        AddHandler Me.SizeChanged, Sub(sender As Object, e As SizeChangedEventArgs) UpdateCamera()
        UpdateCamera()
    End Sub

    '载入与重建

    ''' <summary>
    ''' 载入并显示皮肤，路径无效时显示空白。
    ''' </summary>
    Public Sub SetSkin(FilePath As String, Optional IsSlim As Boolean = False)
        SkinTexture = Nothing
        If Not String.IsNullOrEmpty(FilePath) Then
            Try
                SkinTexture = New MyBitmap(FilePath)
                SkinScale = Math.Max(1, SkinTexture.Pic.Width \ 64)
                IsSlimSkin = IsSlim
            Catch ex As Exception
                Logger.Error(ex, $"更衣室加载皮肤纹理失败（{FilePath}）")
                SkinTexture = Nothing
            End Try
        End If
        Rebuild()
    End Sub

    ''' <summary>
    ''' 载入并显示披风，路径无效时隐藏披风。
    ''' </summary>
    Public Sub SetCape(FilePath As String)
        CapeTexture = Nothing
        If Not String.IsNullOrEmpty(FilePath) AndAlso FileUtils.Exists(FilePath) Then
            Try
                CapeTexture = New MyBitmap(FilePath)
            Catch ex As Exception
                Logger.Error(ex, $"更衣室加载披风纹理失败（{FilePath}）")
                CapeTexture = Nothing
            End Try
        End If
        Rebuild()
    End Sub

    ''' <summary>
    ''' 重置视角。
    ''' </summary>
    Public Sub ResetView()
        AngleY = 35
        AngleX = 0
        Zoom = 1
        UpdateRotate()
        UpdateCamera()
    End Sub

    Private Sub Rebuild()
        For Each Part As Model3D In Parts
            Models.Children.Remove(Part)
        Next
        Parts.Clear()
        PendingOverlays.Clear()
        If SkinTexture Is Nothing Then Return
        Try
            Dim Is64 As Boolean = SkinTexture.Pic.Height >= 64
            Dim Big As MyBitmap = Upscale(SkinTexture, 16)
            Dim S As Integer = SkinScale * 16
            Dim ArmWidth As Double = If(IsSlimSkin, 3.0, 4.0)
            '各部件之间留出极小间隙，避免共面导致深度冲突（表现为接缝处的白线）
            Const Eps As Double = 0.03

            '头部
            Dim Head() As SkinFace = {
                New SkinFace(8, 0, 8, 8), New SkinFace(16, 0, 8, 8), New SkinFace(0, 8, 8, 8),
                New SkinFace(8, 8, 8, 8), New SkinFace(16, 8, 8, 8), New SkinFace(24, 8, 8, 8)}
            AddPart(New Point3D(-4, 8 + Eps, -4), New Point3D(4, 16 + Eps, 4), Big, S, Head)
            If Is64 Then AddPart(New Point3D(-4, 8 + Eps, -4), New Point3D(4, 16 + Eps, 4), Big, S, OffsetFaces(Head, 32, 0), 0.5)

            '身体
            Dim Body() As SkinFace = {
                New SkinFace(20, 16, 8, 4), New SkinFace(28, 16, 8, 4), New SkinFace(16, 20, 4, 12),
                New SkinFace(20, 20, 8, 12), New SkinFace(28, 20, 4, 12), New SkinFace(32, 20, 8, 12)}
            AddPart(New Point3D(-4, -4, -2), New Point3D(4, 8, 2), Big, S, Body)
            If Is64 Then AddPart(New Point3D(-4, -4, -2), New Point3D(4, 8, 2), Big, S, OffsetFaces(Body, 0, 16), 0.5)

            '右腿
            Dim RightLeg() As SkinFace = {
                New SkinFace(4, 16, 4, 4), New SkinFace(8, 16, 4, 4), New SkinFace(0, 20, 4, 12),
                New SkinFace(4, 20, 4, 12), New SkinFace(8, 20, 4, 12), New SkinFace(12, 20, 4, 12)}
            AddPart(New Point3D(-4, -16 - Eps, -2), New Point3D(-0.01, -4 - Eps, 2), Big, S, RightLeg)
            If Is64 Then AddPart(New Point3D(-4, -16 - Eps, -2), New Point3D(-0.01, -4 - Eps, 2), Big, S, OffsetFaces(RightLeg, 0, 16), 0.5)

            '左腿
            If Is64 Then
                Dim LeftLeg() As SkinFace = {
                    New SkinFace(20, 48, 4, 4), New SkinFace(24, 48, 4, 4), New SkinFace(16, 52, 4, 12),
                    New SkinFace(20, 52, 4, 12), New SkinFace(24, 52, 4, 12), New SkinFace(28, 52, 4, 12)}
                AddPart(New Point3D(0.01, -16 - Eps, -2), New Point3D(4, -4 - Eps, 2), Big, S, LeftLeg)
                AddPart(New Point3D(0.01, -16 - Eps, -2), New Point3D(4, -4 - Eps, 2), Big, S, OffsetFaces(LeftLeg, -16, 0), 0.5)
            Else
                AddPart(New Point3D(0.01, -16 - Eps, -2), New Point3D(4, -4 - Eps, 2), Big, S, MirrorFaces(RightLeg))
            End If

            '右臂
            Dim RightArm() As SkinFace = GetArmFaces(IsSlimSkin, False)
            AddPart(New Point3D(-4 - ArmWidth - Eps, -4, -2), New Point3D(-4 - Eps, 8, 2), Big, S, RightArm)
            If Is64 Then AddPart(New Point3D(-4 - ArmWidth - Eps, -4, -2), New Point3D(-4 - Eps, 8, 2), Big, S, OffsetFaces(RightArm, 0, 16), 0.5)

            '左臂
            If Is64 Then
                Dim LeftArm() As SkinFace = GetArmFaces(IsSlimSkin, True)
                AddPart(New Point3D(4 + Eps, -4, -2), New Point3D(4 + ArmWidth + Eps, 8, 2), Big, S, LeftArm)
                AddPart(New Point3D(4 + Eps, -4, -2), New Point3D(4 + ArmWidth + Eps, 8, 2), Big, S, OffsetFaces(LeftArm, 16, 0), 0.5)
            Else
                AddPart(New Point3D(4 + Eps, -4, -2), New Point3D(4 + ArmWidth + Eps, 8, 2), Big, S, MirrorFaces(RightArm))
            End If

            '披风
            If CapeTexture IsNot Nothing Then
                Try
                    Dim CapeBig As MyBitmap = Upscale(CapeTexture, 16)
                    Dim CS As Integer = Math.Max(1, CapeTexture.Pic.Width \ 64) * 16
                    ''披风
                    Dim CapeOuter As New SkinFace(1, 1, 10, 16)
                    Dim CapeInner As New SkinFace(12, 1, 10, 16)
                    AddPart(New Point3D(-5, -8, -3.4), New Point3D(5, 8, -2.6), CapeBig, CS,
                            {CapeInner, CapeInner, CapeInner, CapeInner, CapeInner, CapeOuter})
                Catch ex As Exception
                    Logger.Error(ex, "更衣室渲染披风失败")
                End Try
            End If
        Catch ex As Exception
            Logger.Error(ex, "更衣室渲染皮肤失败")
        End Try
        For Each Overlay As Model3D In PendingOverlays
            Models.Children.Add(Overlay)
        Next
        PendingOverlays.Clear()
    End Sub

    ''' <summary>
    ''' 获取手臂六个面的纹理区域。LeftSide 为 True 时返回左臂。
    ''' </summary>
    Private Shared Function GetArmFaces(IsSlim As Boolean, LeftSide As Boolean) As SkinFace()
        If LeftSide Then
            If IsSlim Then
                Return {
                    New SkinFace(36, 48, 3, 4), New SkinFace(39, 48, 3, 4), New SkinFace(32, 52, 4, 12),
                    New SkinFace(36, 52, 3, 12), New SkinFace(39, 52, 4, 12), New SkinFace(43, 52, 3, 12)}
            Else
                Return {
                    New SkinFace(36, 48, 4, 4), New SkinFace(40, 48, 4, 4), New SkinFace(32, 52, 4, 12),
                    New SkinFace(36, 52, 4, 12), New SkinFace(40, 52, 4, 12), New SkinFace(44, 52, 4, 12)}
            End If
        Else
            If IsSlim Then
                Return {
                    New SkinFace(44, 16, 3, 4), New SkinFace(47, 16, 3, 4), New SkinFace(40, 20, 4, 12),
                    New SkinFace(44, 20, 3, 12), New SkinFace(47, 20, 4, 12), New SkinFace(51, 20, 3, 12)}
            Else
                Return {
                    New SkinFace(44, 16, 4, 4), New SkinFace(48, 16, 4, 4), New SkinFace(40, 20, 4, 12),
                    New SkinFace(44, 20, 4, 12), New SkinFace(48, 20, 4, 12), New SkinFace(52, 20, 4, 12)}
            End If
        End If
    End Function

    ''' <summary>
    ''' 添加一个立方体的六个面（顺序为 顶、底、右、前、左、后）。
    ''' </summary>
    Private Sub AddPart(Min As Point3D, Max As Point3D, Texture As MyBitmap, TextureScale As Integer,
                        Faces As SkinFace(), Optional Inflate As Double = 0)
        Dim X1 As Double = Min.X - Inflate, Y1 As Double = Min.Y - Inflate, Z1 As Double = Min.Z - Inflate
        Dim X2 As Double = Max.X + Inflate, Y2 As Double = Max.Y + Inflate, Z2 As Double = Max.Z + Inflate
        Dim IsOverlay As Boolean = Inflate > 0
        AddFace(New Point3D(X1, Y2, Z2), New Point3D(X2, Y2, Z2), New Point3D(X2, Y2, Z1), New Point3D(X1, Y2, Z1), Faces(0), Texture, TextureScale, IsOverlay:=IsOverlay)
        Dim BottomUV() As Point
        If Faces(1).Flip Then
            BottomUV = New Point() {New Point(1, 0), New Point(0, 0), New Point(0, 1), New Point(1, 1)}
        Else
            BottomUV = New Point() {New Point(0, 0), New Point(1, 0), New Point(1, 1), New Point(0, 1)}
        End If
        AddFace(New Point3D(X1, Y1, Z1), New Point3D(X2, Y1, Z1), New Point3D(X2, Y1, Z2), New Point3D(X1, Y1, Z2), Faces(1), Texture, TextureScale, BottomUV, IsOverlay)
        AddFace(New Point3D(X1, Y1, Z1), New Point3D(X1, Y1, Z2), New Point3D(X1, Y2, Z2), New Point3D(X1, Y2, Z1), Faces(2), Texture, TextureScale, IsOverlay:=IsOverlay)
        AddFace(New Point3D(X1, Y1, Z2), New Point3D(X2, Y1, Z2), New Point3D(X2, Y2, Z2), New Point3D(X1, Y2, Z2), Faces(3), Texture, TextureScale, IsOverlay:=IsOverlay)
        AddFace(New Point3D(X2, Y1, Z2), New Point3D(X2, Y1, Z1), New Point3D(X2, Y2, Z1), New Point3D(X2, Y2, Z2), Faces(4), Texture, TextureScale, IsOverlay:=IsOverlay)
        AddFace(New Point3D(X2, Y1, Z1), New Point3D(X1, Y1, Z1), New Point3D(X1, Y2, Z1), New Point3D(X2, Y2, Z1), Faces(5), Texture, TextureScale, IsOverlay:=IsOverlay)
    End Sub

    ''' <summary>
    ''' 添加一个面，四个顶点按 左下、右下、右上、左上（从外部观察）的顺序给出。
    ''' </summary>
    Private Sub AddFace(P1 As Point3D, P2 As Point3D, P3 As Point3D, P4 As Point3D, Face As SkinFace, Texture As MyBitmap, TextureScale As Integer, Optional UVs As Point() = Nothing, Optional IsOverlay As Boolean = False)
        Dim Piece As MyBitmap = Texture.Clip(Face.X * TextureScale, Face.Y * TextureScale, Face.W * TextureScale, Face.H * TextureScale)
        Dim Source As New FormatConvertedBitmap(CType(Piece, ImageSource), PixelFormats.Pbgra32, Nothing, 0)
        Source.Freeze()
        Dim Brush As New ImageBrush(Source) With {.Stretch = Stretch.Fill}
        Brush.Freeze()
        Dim Material As New DiffuseMaterial(Brush)
        Dim Mesh As New MeshGeometry3D()
        Mesh.Positions.Add(P1)
        Mesh.Positions.Add(P2)
        Mesh.Positions.Add(P3)
        Mesh.Positions.Add(P4)
        If UVs IsNot Nothing Then
            For Each UV As Point In UVs
                Mesh.TextureCoordinates.Add(UV)
            Next
        ElseIf Face.Flip Then
            Mesh.TextureCoordinates.Add(New Point(1, 1))
            Mesh.TextureCoordinates.Add(New Point(0, 1))
            Mesh.TextureCoordinates.Add(New Point(0, 0))
            Mesh.TextureCoordinates.Add(New Point(1, 0))
        Else
            Mesh.TextureCoordinates.Add(New Point(0, 1))
            Mesh.TextureCoordinates.Add(New Point(1, 1))
            Mesh.TextureCoordinates.Add(New Point(1, 0))
            Mesh.TextureCoordinates.Add(New Point(0, 0))
        End If
        Mesh.TriangleIndices.Add(0)
        Mesh.TriangleIndices.Add(1)
        Mesh.TriangleIndices.Add(2)
        Mesh.TriangleIndices.Add(0)
        Mesh.TriangleIndices.Add(2)
        Mesh.TriangleIndices.Add(3)
        Mesh.Freeze()
        Dim Model As New GeometryModel3D(Mesh, Material) With {.BackMaterial = Material}
        Model.Freeze()
        Parts.Add(Model)
        If IsOverlay Then PendingOverlays.Add(Model) Else Models.Children.Add(Model)
    End Sub

    ''' <summary>
    ''' 将皮肤纹理按最近邻放大，以获得清晰的像素边缘。
    ''' </summary>
    Private Shared Function Upscale(Source As MyBitmap, Factor As Integer) As MyBitmap
        Dim Width As Integer = Source.Pic.Width * Factor
        Dim Height As Integer = Source.Pic.Height * Factor
        Dim Result As New System.Drawing.Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        Using Graphics As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(Result)
            Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor
            Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half
            Graphics.DrawImage(Source.Pic, New System.Drawing.Rectangle(0, 0, Width, Height))
        End Using
        Return New MyBitmap(Result)
    End Function

    ''' <summary>
    ''' 偏移一组面的纹理坐标。
    ''' </summary>
    Private Shared Function OffsetFaces(Faces As SkinFace(), X As Integer, Y As Integer) As SkinFace()
        Dim Result(Faces.Length - 1) As SkinFace
        For i As Integer = 0 To Faces.Length - 1
            Result(i) = Faces(i)
            Result(i).X += X
            Result(i).Y += Y
        Next
        Return Result
    End Function

    ''' <summary>
    ''' 生成与一组面互为镜像的面（用于 64x32 旧版皮肤的左侧肢体），左右两面互换并水平翻转。
    ''' </summary>
    Private Shared Function MirrorFaces(Faces As SkinFace()) As SkinFace()
        Dim Result(Faces.Length - 1) As SkinFace
        For i As Integer = 0 To Faces.Length - 1
            Result(i) = Faces(i)
            Result(i).Flip = Not Faces(i).Flip
        Next
        Dim Temp As SkinFace = Result(2)
        Result(2) = Result(4)
        Result(4) = Temp
        Return Result
    End Function

    Public Structure SkinFace
        Public X As Integer
        Public Y As Integer
        Public W As Integer
        Public H As Integer
        Public Flip As Boolean
        Public Sub New(X As Integer, Y As Integer, W As Integer, H As Integer, Optional Flip As Boolean = False)
            Me.X = X
            Me.Y = Y
            Me.W = W
            Me.H = H
            Me.Flip = Flip
        End Sub
    End Structure

    '交互

    Private IsDragging As Boolean = False
    Private LastPoint As Point

    Private Sub PanBack_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        IsDragging = True
        LastPoint = e.GetPosition(Me)
        Me.CaptureMouse()
    End Sub
    Private Sub PanBack_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonUp
        IsDragging = False
        Me.ReleaseMouseCapture()
    End Sub
    Private Sub PanBack_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        If Not IsDragging Then Return
        Dim Current As Point = e.GetPosition(Me)
        AngleY += (Current.X - LastPoint.X) * 0.6
        AngleX = Math.Max(-60, Math.Min(60, AngleX + (Current.Y - LastPoint.Y) * 0.6))
        LastPoint = Current
        UpdateRotate()
    End Sub
    Private Sub PanBack_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles Me.MouseWheel
        Zoom = Math.Max(0.5, Math.Min(2.5, Zoom * Math.Pow(0.9, e.Delta / 120.0)))
        UpdateCamera()
        e.Handled = True
    End Sub
    Private Sub UpdateRotate()
        RotateY.Angle = AngleY
        RotateX.Angle = AngleX
    End Sub
    Private Sub UpdateCamera()
        Dim Width As Double = Math.Max(ActualWidth, 1), Height As Double = Math.Max(ActualHeight, 1)
        Camera.Width = 44 * Zoom * Width / Height
        Camera.Position = New Point3D(0, 0, 100)
    End Sub

End Class
