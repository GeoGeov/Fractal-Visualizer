Public Class FractalController
    Implements IDisposable

    Public ReadOnly Property Store As PointStorage
    Public ReadOnly Property Generator As FractalGenerator
    Public ReadOnly Property Camera As Camera
    Public ReadOnly Property Renderer As FractalRenderer

    Private IsDisposed As Boolean = False

    Public Sub New(Optional maxPoints As Integer = 500000)

        Store = New PointStorage With {
            .MaxPoints = maxPoints
        }

        Camera = New Camera()
        Generator = New FractalGenerator(Store)
        Renderer = New FractalRenderer(Camera)

    End Sub

    'Updates which points should be retained.
    'Does not restart or interrupt generation.
    Public Sub UpdateViewport(viewport As ViewportBounds)

        If IsDisposed Then Return

        Generator.UpdateViewport(viewport)

        Renderer.Invalidate()

    End Sub

    'Starts a finite generation session.
    Public Async Function GenerateAsync(pointCount As Integer) As Task

        If IsDisposed Then Return

        Renderer.Invalidate()

        Await Generator.GenerateAsync(pointCount)

        Renderer.Invalidate()

    End Function

    'Starts continuous generation.
    Public Async Function StartContinuousAsync() As Task

        If IsDisposed Then Return

        Await Generator.StartContinuousAsync()

        Renderer.Invalidate()

    End Function

    'Stops generation without clearing stored points.
    Public Async Function StopGenerationAsync() As Task

        If IsDisposed Then Return

        Await Generator.StopAsync()

        Renderer.Invalidate()

    End Function

    'Clears the fractal and resets the camera.
    Public Async Function ResetAsync() As Task

        If IsDisposed Then Return

        Await Generator.StopAsync()

        Store.Clear()
        Camera.Reset()

        Renderer.Invalidate()

    End Function

    'Renders the current stored points.
    Public Function Render(
        width As Integer,
        height As Integer
    ) As Bitmap

        If IsDisposed Then Return Nothing

        Return Renderer.Update(Store, width, height)

    End Function

    Public Sub InvalidateRenderer()

        If IsDisposed Then Return

        Renderer.Invalidate()

    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

        If IsDisposed Then Return

        IsDisposed = True

        'Ensure the background generator has stopped before
        'disposing resources it might still be using.
        Generator.StopAsync().GetAwaiter().GetResult()

        Renderer.Dispose()

    End Sub

End Class
