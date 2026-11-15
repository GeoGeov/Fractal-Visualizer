Public Class FractalDisplay
    Inherits UserControl

#Region "Private Fields"

    Private DisplayTimer As Windows.Forms.Timer
    Private DetailTimer As Windows.Forms.Timer

    Private CurrentController As FractalController

    Private CachedBitmap As Bitmap

    Private IsDragging As Boolean = False
    Private IsWaitingForDetail As Boolean = False

    Private LastMousePosition As Point

    Private StatisticsTickCounter As Integer = 0

#End Region

#Region "Public Properties"

    Public FractalPicture As PictureBox

    ' Extra space retained around the viewport.
    Public Property ViewportMargin As Double = 0.2

    Public Property TargetVisiblePoints As Integer = 100000

    Public Event CameraChanged As EventHandler

    Public Event StatisticsUpdated As EventHandler

#End Region

#Region "Constructor"

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

        ' Refreshes the display regularly.
        ' The renderer ignores requests if nothing has changed.
        DisplayTimer = New Windows.Forms.Timer With {
            .Interval = 33
        }

        AddHandler DisplayTimer.Tick,
            AddressOf DisplayTimer_Tick

        ' Waits until the user stops moving the camera
        ' before requesting a new full-resolution render.
        DetailTimer = New Windows.Forms.Timer With {
            .Interval = 80
        }

        AddHandler DetailTimer.Tick,
            AddressOf DetailTimer_Tick

        DisplayTimer.Start()

    End Sub

#End Region

#Region "Controller Attachment"

    Public Sub AttachController(controller As FractalController)

        ' Detach from the previous renderer.
        If CurrentController IsNot Nothing Then

            RemoveHandler CurrentController.Renderer.RenderCompleted,
                AddressOf Renderer_RenderCompleted

            RemoveHandler CurrentController.Renderer.RenderFailed,
                AddressOf Renderer_RenderFailed

        End If

        CurrentController = controller

        If CurrentController Is Nothing Then

            If CachedBitmap IsNot Nothing Then
                CachedBitmap.Dispose()
                CachedBitmap = Nothing
            End If

            FractalPicture.Invalidate()

            Return

        End If

        ' Listen for completed background renders.
        AddHandler CurrentController.Renderer.RenderCompleted,
            AddressOf Renderer_RenderCompleted

        AddHandler CurrentController.Renderer.RenderFailed,
            AddressOf Renderer_RenderFailed

        CurrentController.InvalidateRenderer()

        UpdateGenerationViewport()

        FractalPicture.Focus()

    End Sub

#End Region

#Region "Display Refresh"

    ' Requests a render without blocking the UI thread.
    Public Sub RefreshDisplay()

        If CurrentController Is Nothing Then Return

        If IsDisposed Then Return

        Dim width As Integer =
            FractalPicture.ClientSize.Width

        Dim height As Integer =
            FractalPicture.ClientSize.Height

        If width <= 0 OrElse height <= 0 Then Return

        CurrentController.Renderer.RequestRender(
            CurrentController.Store,
            width,
            height
        )

    End Sub

    ' Draws the last completed bitmap at its native resolution.
    Private Sub FractalPicture_Paint(
        sender As Object,
        e As PaintEventArgs
    )

        e.Graphics.Clear(Color.White)

        If CachedBitmap Is Nothing Then Return

        ' No scaling or interpolation.
        ' This preserves the original pixel sharpness.
        e.Graphics.DrawImageUnscaled(
            CachedBitmap,
            0,
            0
        )

    End Sub

    ' Called when the asynchronous renderer finishes.
    Private Sub Renderer_RenderCompleted(
        sender As Object,
        e As RenderCompletedEventArgs
    )

        If IsDisposed OrElse
           FractalPicture Is Nothing OrElse
           FractalPicture.IsDisposed Then

            e.Bitmap.Dispose()
            Return

        End If

        ' Dispose the previous image.
        If CachedBitmap IsNot Nothing Then

            CachedBitmap.Dispose()

        End If

        ' Take ownership of the new bitmap.
        CachedBitmap = e.Bitmap

        FractalPicture.Invalidate()

    End Sub

    ' Displays rendering errors on the UI thread.
    Private Sub Renderer_RenderFailed(
        sender As Object,
        e As RenderFailedEventArgs
    )

        If IsDisposed Then Return

        MessageBox.Show(
            Me,
            e.Exception.Message,
            "Rendering error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        )

    End Sub

    Private Sub DisplayTimer_Tick(
        sender As Object,
        e As EventArgs
    )

        If CurrentController Is Nothing Then Return

        ' Avoid requesting renders while the camera is moving.
        If Not IsDragging AndAlso Not IsWaitingForDetail Then

            RefreshDisplay()

        End If

        ' Update statistics less frequently.
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

        ' The bitmap dimensions must match the new display size.
        CurrentController.Renderer.Invalidate()

        RequestViewportDetail()

    End Sub

#End Region

#Region "Viewport"

    ' Returns the actual visible viewport in fractal coordinates.
    Public Function GetViewportBounds() As ViewportBounds

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

    ' Returns the viewport with an additional margin.
    Private Function GetExpandedViewportBounds() As ViewportBounds

        Return GetViewportBounds().Expand(ViewportMargin)

    End Function

    ' Updates PointStorage without restarting generation.
    Public Sub UpdateGenerationViewport()

        If CurrentController Is Nothing Then Return

        Try

            Dim viewport As ViewportBounds =
                GetExpandedViewportBounds()

            CurrentController.UpdateViewport(viewport)

            ' Camera and viewport have changed.
            CurrentController.Renderer.Invalidate()

            RefreshDisplay()

        Catch ex As InvalidOperationException

            ' Can occur during resizing when dimensions are invalid.
            Return

        End Try

    End Sub

    ' Schedules a viewport update after camera movement.
    Private Sub RequestViewportDetail()

        If CurrentController Is Nothing Then Return

        IsWaitingForDetail = True

        DetailTimer.Stop()
        DetailTimer.Start()

    End Sub

    ' Retained for compatibility with Form1.
    Public Sub GenerateCurrentViewportDetail()

        RequestViewportDetail()

    End Sub

    ' Retained for compatibility with existing callers.
    Public Function GenerateViewportNowAsync() As Task

        If CurrentController Is Nothing Then
            Return Task.CompletedTask
        End If

        DetailTimer.Stop()

        IsWaitingForDetail = False

        UpdateGenerationViewport()

        Return Task.CompletedTask

    End Function

    Private Sub DetailTimer_Tick(
        sender As Object,
        e As EventArgs
    )

        DetailTimer.Stop()

        If CurrentController Is Nothing Then Return

        ' Don't update the viewport during a drag.
        If IsDragging Then Return

        IsWaitingForDetail = False

        ' Update the generation viewport and request
        ' a full-resolution render.
        UpdateGenerationViewport()

    End Sub

#End Region

#Region "Mouse Control"

    ' Focus the picture box when the mouse enters it.
    Private Sub FractalPicture_MouseEnter(
        sender As Object,
        e As EventArgs
    )

        FractalPicture.Focus()

    End Sub

    ' Begin panning.
    Private Sub FractalPicture_MouseDown(
        sender As Object,
        e As MouseEventArgs
    )

        If e.Button <> MouseButtons.Left Then Return

        If CurrentController Is Nothing Then Return

        IsDragging = True

        DetailTimer.Stop()

        IsWaitingForDetail = True

        LastMousePosition = e.Location

        FractalPicture.Cursor = Cursors.Hand

        FractalPicture.Capture = True

    End Sub

    ' Pan the camera.
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

        ' Convert screen movement into world movement.
        CurrentController.Camera.X -= dx / scale

        CurrentController.Camera.Y += dy / scale

        ' Invalidate old camera renders.
        CurrentController.Renderer.Invalidate()

        ' Keep the old sharp bitmap displayed until
        ' the new viewport has finished rendering.
        RaiseEvent CameraChanged(
            Me,
            EventArgs.Empty
        )

    End Sub

    ' Finish panning.
    Private Sub FractalPicture_MouseUp(
        sender As Object,
        e As MouseEventArgs
    )

        If e.Button <> MouseButtons.Left Then Return

        If Not IsDragging Then Return

        IsDragging = False

        FractalPicture.Capture = False

        FractalPicture.Cursor = Cursors.Default

        ' Schedule the full-resolution render.
        RequestViewportDetail()

    End Sub

    ' Zoom around the mouse position.
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

        ' Find the world coordinate underneath the mouse.
        Dim worldX As Double =
            camera.X + (e.X - width / 2.0) / oldScale

        Dim worldY As Double =
            camera.Y - (e.Y - height / 2.0) / oldScale

        ' Calculate the zoom factor.
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

        ' Adjust the camera so the world coordinate
        ' underneath the cursor remains stationary.
        camera.X =
            worldX - (e.X - width / 2.0) / newScale

        camera.Y =
            worldY + (e.Y - height / 2.0) / newScale

        camera.Zoom = newZoom

        ' Invalidate any renders using the previous camera.
        CurrentController.Renderer.Invalidate()

        RaiseEvent CameraChanged(
            Me,
            EventArgs.Empty
        )

        ' Wait until zooming stops before rendering.
        RequestViewportDetail()

    End Sub

#End Region

#Region "Statistics"

    Public Function GetVisiblePointCount() As Integer

        If CurrentController Is Nothing Then Return 0

        Dim viewport As ViewportBounds

        Try

            viewport = GetViewportBounds()

        Catch ex As InvalidOperationException

            Return 0

        End Try

        Dim points As List(Of Fpoint) =
            CurrentController.Store.GetSnapshot()

        Dim count As Integer = 0

        For Each point As Fpoint In points

            If viewport.Contains(point) Then

                count += 1

            End If

        Next

        Return count

    End Function

#End Region

End Class
