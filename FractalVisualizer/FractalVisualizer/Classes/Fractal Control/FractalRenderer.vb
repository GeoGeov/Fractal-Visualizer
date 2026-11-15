Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Threading

Public Class FractalRenderer
    Implements IDisposable

#Region "Events"

    Public Event RenderCompleted As EventHandler(
        Of RenderCompletedEventArgs
    )

    Public Event RenderFailed As EventHandler(
        Of RenderFailedEventArgs
    )

#End Region

#Region "Private Classes"

    Private Class RenderRequest

        Public Store As PointStorage

        Public Viewport As ViewportBounds

        Public Width As Integer
        Public Height As Integer

        Public MaxIterations As Integer

        Public CameraVersion As Long

    End Class

#End Region

#Region "Private Fields"

    Private ReadOnly ViewCamera As Camera

    Private ReadOnly Colouring As ColouringManager

    Private ReadOnly StateLock As New Object()

    Private ReadOnly WorkerFinished As New ManualResetEventSlim(True)

    Private ReadOnly UIContext As SynchronizationContext

    Private PendingRequest As RenderRequest

    Private WorkerRunning As Boolean = False

    Private IsDisposed As Boolean = False

    ' Determines whether the individual colouring bitmaps
    ' need regenerating.
    Private NeedsLayerRegeneration As Boolean = True

    ' Determines whether the final composition needs updating.
    Private NeedsRedraw As Boolean = True

    ' Used to identify outdated camera renders.
    Private CameraVersion As Long = 0

    ' Tracks the point storage state.
    Private LastSeenTotal As Long = -1

    Private LastWidth As Integer = -1
    Private LastHeight As Integer = -1

#End Region

#Region "Properties"

    Public Property MaxIterations As Integer

#End Region

#Region "Constructor"

    Public Sub New(
        camera As Camera,
        colouringManager As ColouringManager
    )

        If camera Is Nothing Then
            Throw New ArgumentNullException(NameOf(camera))
        End If

        If colouringManager Is Nothing Then
            Throw New ArgumentNullException(NameOf(colouringManager))
        End If

        ViewCamera = camera
        Colouring = colouringManager

        ' Capture the UI thread so completed bitmaps can
        ' safely be passed back to Windows Forms.
        UIContext = SynchronizationContext.Current

        If UIContext Is Nothing Then
            Throw New InvalidOperationException(
                "FractalRenderer must be created on the UI thread."
            )
        End If

    End Sub

#End Region

#Region "Request Rendering"

    Public Sub RequestRender(
        store As PointStorage,
        width As Integer,
        height As Integer
    )

        If store Is Nothing Then Return

        If width <= 0 OrElse height <= 0 Then Return

        SyncLock StateLock

            If IsDisposed Then Return

            ' Check whether the display size has changed.
            If width <> LastWidth OrElse height <> LastHeight Then

                CameraVersion += 1

                NeedsRedraw = True
                NeedsLayerRegeneration = True

                LastWidth = width
                LastHeight = height

            End If

            ' Capture the camera position now.
            ' The worker must not read mutable camera properties.
            Dim viewport As ViewportBounds =
                GetViewport(
                    width,
                    height,
                    ViewCamera.X,
                    ViewCamera.Y,
                    ViewCamera.Zoom
                )

            ' Replace the pending request.
            ' If the renderer is busy, it will process this
            ' newest request after its current render finishes.
            PendingRequest = New RenderRequest With {
                .Store = store,
                .Viewport = viewport,
                .Width = width,
                .Height = height,
                .MaxIterations = MaxIterations,
                .CameraVersion = CameraVersion
            }

            ' Start a worker if one isn't already running.
            If Not WorkerRunning Then

                WorkerRunning = True

                WorkerFinished.Reset()

                Task.Run(Sub() RenderWorker())

            End If

        End SyncLock

    End Sub

#End Region

#Region "Background Worker"

    Private Sub RenderWorker()

        Try

            While True

                Dim request As RenderRequest = Nothing

                SyncLock StateLock

                    If IsDisposed Then

                        PendingRequest = Nothing
                        WorkerRunning = False

                        WorkerFinished.Set()

                        Return

                    End If

                    request = PendingRequest

                    ' No more pending work.
                    If request Is Nothing Then

                        WorkerRunning = False

                        WorkerFinished.Set()

                        Return

                    End If

                    PendingRequest = Nothing

                End SyncLock

                Try

                    ProcessRequest(request)

                Catch ex As Exception

                    RaiseRenderError(request, ex)

                End Try

            End While

        Finally

            SyncLock StateLock

                WorkerRunning = False

                WorkerFinished.Set()

            End SyncLock

        End Try

    End Sub

#End Region

#Region "Process Render Request"

    Private Sub ProcessRequest(request As RenderRequest)

        Dim rebuildRequired As Boolean = False

        Dim latestTotal As Long = 0

        ' Get changes since the previous render.
        ' This currently detects changes; incremental
        ' colouring can use the returned points later.
        Dim changedPoints As List(Of Fpoint) =
            request.Store.GetChanges(
                LastSeenTotal,
                rebuildRequired,
                latestTotal
            )

        Dim pointsChanged As Boolean =
            latestTotal <> LastSeenTotal OrElse rebuildRequired

        Dim redrawRequired As Boolean
        Dim regenerateLayers As Boolean

        SyncLock StateLock

            If IsDisposed Then Return

            redrawRequired = NeedsRedraw

            regenerateLayers = NeedsLayerRegeneration

            ' Consume the invalidation flags.
            NeedsRedraw = False

            NeedsLayerRegeneration = False

        End SyncLock

        ' New points or storage rebuilds require recolouring.
        If pointsChanged Then

            redrawRequired = True
            regenerateLayers = True

        End If

        ' Nothing has changed.
        If Not redrawRequired Then Return

        ' Invalidate cached layer bitmaps only when required.
        If regenerateLayers Then
            Colouring.InvalidateAll()
        End If

        ' Create a context using the requested viewport.
        ' The context takes its own point snapshot.
        Dim context As New ColouringContext(
            request.Store,
            request.Viewport,
            request.Width,
            request.Height,
            request.MaxIterations
        )

        Dim result As Bitmap = Nothing

        Try

            ' Generate all colouring layers and blend them.
            result = Colouring.GenerateBitmap(context)

            If result Is Nothing Then
                Throw New InvalidOperationException(
                    "The colouring manager returned no bitmap."
                )
            End If

            ' Record the point count used for this render.
            LastSeenTotal = latestTotal

            ' Transfer ownership of the bitmap to the UI.
            PublishBitmap(request, result)

            result = Nothing

        Finally

            ' Dispose if ownership wasn't transferred.
            If result IsNot Nothing Then
                result.Dispose()
            End If

        End Try

    End Sub

#End Region

#Region "Publish Bitmap"

    Private Sub PublishBitmap(
        request As RenderRequest,
        bitmap As Bitmap
    )

        UIContext.Post(
            Sub(state)

                Dim completedBitmap As Bitmap = bitmap

                Dim shouldDisplay As Boolean

                SyncLock StateLock

                    shouldDisplay =
                        Not IsDisposed AndAlso
                        request.CameraVersion = CameraVersion

                End SyncLock

                If Not shouldDisplay Then

                    completedBitmap.Dispose()

                    Return

                End If

                RaiseEvent RenderCompleted(
                    Me,
                    New RenderCompletedEventArgs(completedBitmap)
                )

            End Sub,
            Nothing
        )

    End Sub

#End Region

#Region "Error Handling"

    Private Sub RaiseRenderError(
        request As RenderRequest,
        exception As Exception
    )

        UIContext.Post(
            Sub(state)

                SyncLock StateLock

                    If IsDisposed Then Return

                    If request.CameraVersion <> CameraVersion Then
                        Return
                    End If

                End SyncLock

                RaiseEvent RenderFailed(
                    Me,
                    New RenderFailedEventArgs(exception)
                )

            End Sub,
            Nothing
        )

    End Sub

#End Region

#Region "Viewport"

    Private Function GetViewport(
        width As Integer,
        height As Integer,
        cameraX As Double,
        cameraY As Double,
        zoom As Double
    ) As ViewportBounds

        Dim scale As Double =
            Math.Min(width, height) * zoom

        If scale <= 0 OrElse
           Double.IsNaN(scale) OrElse
           Double.IsInfinity(scale) Then

            Throw New InvalidOperationException(
                "Camera zoom must be a valid positive value."
            )

        End If

        Dim halfWidth As Double =
            width / (2.0 * scale)

        Dim halfHeight As Double =
            height / (2.0 * scale)

        Return New ViewportBounds(
            cameraX - halfWidth,
            cameraX + halfWidth,
            cameraY + halfHeight,
            cameraY - halfHeight
        )

    End Function

#End Region

#Region "Invalidation"

    ' Call when the camera moves, zooms or the viewport changes.
    Public Sub Invalidate()

        SyncLock StateLock

            If IsDisposed Then Return

            CameraVersion += 1

            NeedsRedraw = True
            NeedsLayerRegeneration = True

        End SyncLock

    End Sub

    ' Call when the layer composition changes but the
    ' individual colouring bitmaps remain valid.
    Public Sub InvalidateComposition()

        SyncLock StateLock

            If IsDisposed Then Return

            NeedsRedraw = True

        End SyncLock

    End Sub

    ' Call when a layer's actual colouring settings change.
    Public Sub InvalidateLayers()

        SyncLock StateLock

            If IsDisposed Then Return

            NeedsRedraw = True
            NeedsLayerRegeneration = True

        End SyncLock

    End Sub

#End Region

#Region "Disposal"

    Public Sub Dispose() Implements IDisposable.Dispose

        SyncLock StateLock

            If IsDisposed Then Return

            IsDisposed = True

            PendingRequest = Nothing

        End SyncLock

        ' Wait until the background worker has stopped using
        ' the colouring manager before it is disposed.
        WorkerFinished.Wait()

        WorkerFinished.Dispose()

    End Sub

#End Region

End Class