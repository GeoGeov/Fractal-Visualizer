
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class FractalDisplay
    Inherits UserControl

    Public FractalPicture As PictureBox

    Private DisplayTimer As Windows.Forms.Timer
    Private DetailTimer As Windows.Forms.Timer

    Private CurrentController As FractalController

    Private CachedBitmap As Bitmap

    Private IsDragging As Boolean = False
    Private LastMousePosition As Point

    Private PreviewOffsetX As Single = 0
    Private PreviewOffsetY As Single = 0

    Private StatisticsTickCounter As Integer = 0

    Public Property TargetVisiblePoints As Integer = 100000

    Public Event CameraChanged As EventHandler
    Public Event StatisticsUpdated As EventHandler

    Public Sub New()

        Me.Dock = DockStyle.Fill
        Me.Margin = Padding.Empty
        Me.Padding = Padding.Empty

        FractalPicture = New PictureBox With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .SizeMode = PictureBoxSizeMode.Normal,
            .TabStop = True
        }

        Controls.Add(FractalPicture)

        AddHandler FractalPicture.Paint,
            AddressOf FractalPicture_Paint

        AddHandler FractalPicture.MouseDown,
            AddressOf FractalPicture_MouseDown

        AddHandler FractalPicture.MouseMove,
            AddressOf FractalPicture_MouseMove

        AddHandler FractalPicture.MouseUp,
            AddressOf FractalPicture_MouseUp

        AddHandler FractalPicture.MouseWheel,
            AddressOf FractalPicture_MouseWheel

        AddHandler FractalPicture.MouseEnter,
            AddressOf FractalPicture_MouseEnter

        DisplayTimer = New Windows.Forms.Timer With {
            .Interval = 16
        }

        AddHandler DisplayTimer.Tick,
            AddressOf DisplayTimer_Tick

        DetailTimer = New Windows.Forms.Timer With {
            .Interval = 300
        }

        AddHandler DetailTimer.Tick,
            AddressOf DetailTimer_Tick

        DisplayTimer.Start()

    End Sub

    ' =========================================================
    ' CONTROLLER
    ' =========================================================

    Public Sub AttachController(controller As FractalController)

        CurrentController = controller

        If CurrentController IsNot Nothing Then
            CurrentController.InvalidateRenderer()
        End If

        RefreshDisplay()

    End Sub

    ' =========================================================
    ' DISPLAY
    ' =========================================================

    Public Sub RefreshDisplay()

        If CurrentController Is Nothing Then Return

        Dim width As Integer =
            FractalPicture.ClientSize.Width

        Dim height As Integer =
            FractalPicture.ClientSize.Height

        If width <= 0 OrElse height <= 0 Then Return

        CachedBitmap =
            CurrentController.Render(width, height)

        FractalPicture.Invalidate()

    End Sub

    Private Sub FractalPicture_Paint(
        sender As Object,
        e As PaintEventArgs
    )

        e.Graphics.Clear(Color.White)

        If CachedBitmap Is Nothing Then Return

        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.Half

        Dim destination As New RectangleF(
            PreviewOffsetX,
            PreviewOffsetY,
            FractalPicture.ClientSize.Width,
            FractalPicture.ClientSize.Height
        )

        e.Graphics.DrawImage(
            CachedBitmap,
            destination
        )

    End Sub

    Private Sub DisplayTimer_Tick(
        sender As Object,
        e As EventArgs
    )

        If CurrentController Is Nothing Then Return

        If Not IsDragging Then
            RefreshDisplay()
        End If

        StatisticsTickCounter += 1

        If StatisticsTickCounter >= 6 Then

            StatisticsTickCounter = 0

            RaiseEvent StatisticsUpdated(
                Me,
                EventArgs.Empty
            )

        End If

    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)

        MyBase.OnResize(e)

        If CurrentController Is Nothing Then Return

        CurrentController.InvalidateRenderer()
        RefreshDisplay()

        RequestViewportDetail()

    End Sub

    ' =========================================================
    ' VIEWPORT CALCULATION
    ' =========================================================

    Private Function GetViewportBounds() As ViewportBounds

        If CurrentController Is Nothing Then
            Throw New InvalidOperationException(
                "No controller is attached."
            )
        End If

        Dim width As Double =
            FractalPicture.ClientSize.Width

        Dim height As Double =
            FractalPicture.ClientSize.Height

        If width <= 0 OrElse height <= 0 Then
            Throw New InvalidOperationException(
                "Display dimensions must be positive."
            )
        End If

        Dim camera = CurrentController.Camera

        Dim scale As Double =
            Math.Min(width, height) * camera.Zoom

        If scale <= 0 OrElse
           Double.IsNaN(scale) OrElse
           Double.IsInfinity(scale) Then

            Throw New InvalidOperationException(
                "Invalid camera scale."
            )

        End If

        Dim halfWidth As Double =
            width / (2.0 * scale)

        Dim halfHeight As Double =
            height / (2.0 * scale)

        Return New ViewportBounds(
            camera.X - halfWidth,
            camera.X + halfWidth,
            camera.Y + halfHeight,
            camera.Y - halfHeight
        )

    End Function

    ' =========================================================
    ' VIEWPORT DETAIL REQUESTS
    ' =========================================================
    Public Async Function GenerateViewportNowAsync() As Task

        If CurrentController Is Nothing Then Return

        DetailTimer.Stop()

        Dim viewport As ViewportBounds =
        GetViewportBounds()

        Await CurrentController.Generator.GenerateViewportAsync(
        TargetVisiblePoints,
        viewport
    )

        RefreshDisplay()

    End Function
    Private Sub RequestViewportDetail()

        If CurrentController Is Nothing Then Return

        DetailTimer.Stop()
        DetailTimer.Start()

    End Sub

    Public Sub GenerateCurrentViewportDetail()

        RequestViewportDetail()

    End Sub

    Private Async Sub DetailTimer_Tick(
        sender As Object,
        e As EventArgs
    )

        DetailTimer.Stop()

        If CurrentController Is Nothing Then Return

        Try

            Dim viewport As ViewportBounds =
                GetViewportBounds()

            Await CurrentController.Generator.GenerateViewportAsync(
                TargetVisiblePoints,
                viewport
            )

        Catch ex As OperationCanceledException

            ' Expected when a newer request replaces the current one.

        Catch ex As Exception

            Debug.WriteLine(
                "Viewport generation error: " & ex.Message
            )

        End Try

        RefreshDisplay()

    End Sub

    ' =========================================================
    ' MOUSE FOCUS
    ' =========================================================

    Private Sub FractalPicture_MouseEnter(
        sender As Object,
        e As EventArgs
    )

        FractalPicture.Focus()

    End Sub

    ' =========================================================
    ' PANNING
    ' =========================================================

    Private Sub FractalPicture_MouseDown(
        sender As Object,
        e As MouseEventArgs
    )

        If e.Button <> MouseButtons.Left Then Return

        IsDragging = True

        LastMousePosition = e.Location

        PreviewOffsetX = 0
        PreviewOffsetY = 0

        FractalPicture.Cursor = Cursors.Hand
        FractalPicture.Capture = True

    End Sub

    Private Sub FractalPicture_MouseMove(
        sender As Object,
        e As MouseEventArgs
    )

        If Not IsDragging OrElse
           CurrentController Is Nothing Then Return

        Dim dx As Integer =
            e.X - LastMousePosition.X

        Dim dy As Integer =
            e.Y - LastMousePosition.Y

        LastMousePosition = e.Location

        Dim scale As Double =
            Math.Min(
                FractalPicture.ClientSize.Width,
                FractalPicture.ClientSize.Height
            ) * CurrentController.Camera.Zoom

        If scale <= 0 OrElse
           Double.IsNaN(scale) OrElse
           Double.IsInfinity(scale) Then Return

        CurrentController.Camera.X -= dx / scale
        CurrentController.Camera.Y += dy / scale

        PreviewOffsetX += dx
        PreviewOffsetY += dy

        FractalPicture.Invalidate()

        RaiseEvent CameraChanged(
            Me,
            EventArgs.Empty
        )

    End Sub

    Private Sub FractalPicture_MouseUp(
        sender As Object,
        e As MouseEventArgs
    )

        If e.Button <> MouseButtons.Left Then Return

        IsDragging = False

        FractalPicture.Capture = False
        FractalPicture.Cursor = Cursors.Default

        PreviewOffsetX = 0
        PreviewOffsetY = 0

        If CurrentController IsNot Nothing Then

            CurrentController.InvalidateRenderer()
            RefreshDisplay()

            RequestViewportDetail()

        End If

    End Sub

    ' =========================================================
    ' ZOOMING
    ' =========================================================

    Private Sub FractalPicture_MouseWheel(
        sender As Object,
        e As MouseEventArgs
    )

        If CurrentController Is Nothing Then Return

        Dim width As Double =
            FractalPicture.ClientSize.Width

        Dim height As Double =
            FractalPicture.ClientSize.Height

        If width <= 0 OrElse height <= 0 Then Return

        Dim camera = CurrentController.Camera

        Dim oldScale As Double =
            Math.Min(width, height) * camera.Zoom

        If oldScale <= 0 OrElse
           Double.IsNaN(oldScale) OrElse
           Double.IsInfinity(oldScale) Then Return

        ' World coordinate underneath the mouse.
        Dim worldX As Double =
            camera.X + (e.X - width / 2.0) / oldScale

        Dim worldY As Double =
            camera.Y - (e.Y - height / 2.0) / oldScale

        Dim zoomFactor As Double =
            Math.Pow(1.2, e.Delta / 120.0)

        Dim newZoom As Double =
            Math.Max(
                0.001,
                Math.Min(
                    1000000.0,
                    camera.Zoom * zoomFactor
                )
            )

        Dim newScale As Double =
            Math.Min(width, height) * newZoom

        If newScale <= 0 Then Return

        ' Keep the world coordinate under the mouse stationary.
        camera.X =
            worldX - (e.X - width / 2.0) / newScale

        camera.Y =
            worldY + (e.Y - height / 2.0) / newScale

        camera.Zoom = newZoom

        PreviewOffsetX = 0
        PreviewOffsetY = 0

        CurrentController.InvalidateRenderer()

        RefreshDisplay()

        RaiseEvent CameraChanged(
            Me,
            EventArgs.Empty
        )

        RequestViewportDetail()

    End Sub

    ' =========================================================
    ' STATISTICS
    ' =========================================================

    Public Function GetVisiblePointCount() As Integer

        If CurrentController Is Nothing Then Return 0

        Dim viewport As ViewportBounds =
            GetViewportBounds()

        Dim points As Fpoint() =
            CurrentController.Store.GetSnapshot()

        Dim count As Integer = 0

        For Each point As Fpoint In points

            If viewport.Contains(point) Then
                count += 1
            End If

        Next

        Return count

    End Function

    ' =========================================================
    ' DISPOSAL
    ' =========================================================

    Protected Overrides Sub Dispose(disposing As Boolean)

        If disposing Then

            If DisplayTimer IsNot Nothing Then

                DisplayTimer.Stop()
                DisplayTimer.Dispose()
                DisplayTimer = Nothing

            End If

            If DetailTimer IsNot Nothing Then

                DetailTimer.Stop()
                DetailTimer.Dispose()
                DetailTimer = Nothing

            End If

        End If

        MyBase.Dispose(disposing)

    End Sub

End Class
