Public Class FractalController
    'controls the generation and rendering of fractals, managing the point storage, camera, and renderer

    Implements IDisposable

    Public ReadOnly Property Store As PointStorage
    Public ReadOnly Property Generator As FractalGenerator
    Public ReadOnly Property Camera As Camera
    Public ReadOnly Property Renderer As FractalRenderer

    Public Sub New(Optional maxPoints As Integer = 500000)
        Store = New PointStorage With {.MaxPoints = maxPoints}
        Camera = New Camera()
        Generator = New FractalGenerator(Store)
        Renderer = New FractalRenderer(Camera)
    End Sub

    Public Async Function GenerateAsync(pointCount As Integer) As Task
        'wait for any existing generation task to complete before starting a new one
        'prompts renderer to redraw the fractal after generation is complete

        'forgot why this before and after prolly some bug i encountered
        'should only need the after but just in case
        Renderer.Invalidate()
        Await Generator.GenerateAsync(pointCount)
        Renderer.Invalidate()


    End Function

    Public Async Function ResetAsync() As Task
        Await Generator.StopAsync()
        Store.Clear()
        Camera.Reset()
        Renderer.Invalidate()
    End Function

    Public Function Render(width As Integer, height As Integer) As Bitmap
        Return Renderer.Update(Store, width, height)
    End Function

    Public Sub InvalidateRenderer()
        Renderer.Invalidate()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Renderer.Dispose()
    End Sub


    ' Start a fresh fractal and continue generating in the background.
    Public Async Function StartContinuousAsync() As Task

        Await Generator.StopAsync()

        Store.Clear()
        Renderer.Invalidate()

        Await Generator.StartContinuousAsync()

    End Function

    ' Pause generation without clearing the existing points.
    Public Async Function StopGenerationAsync() As Task

        Await Generator.StopAsync()

        Renderer.Invalidate()

    End Function


End Class
