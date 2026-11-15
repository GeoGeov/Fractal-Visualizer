Public Class ColouringContext

    Public ReadOnly Property Store As PointStorage
    Public ReadOnly Property Viewport As ViewportBounds
    Public ReadOnly Property Points As IReadOnlyList(Of Fpoint)

    Public ReadOnly Property MaxIterations As Integer
    Public ReadOnly Property Width As Integer
    Public ReadOnly Property Height As Integer

    Public Sub New(store As PointStorage, viewport As ViewportBounds, width As Integer, height As Integer, maxIterations As Integer)

        If store Is Nothing Then
            Throw New ArgumentNullException(NameOf(store))
        End If

        If width <= 0 OrElse height <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(width), "Bitmap dimensions must be positive.")
        End If

        Me.Store = store
        Me.Viewport = viewport
        Me.Points = store.GetSnapshot()

        Me.Width = width
        Me.Height = height
        Me.MaxIterations = maxIterations

    End Sub

    ' Converts a fractal point into a pixel coordinate.
    'might be better to move this to a shared utility class
    'here for now to avoid passing around the context object
    Public Function TryWorldToScreen(point As Fpoint, ByRef screenX As Integer, ByRef screenY As Integer) As Boolean
        screenX = 0
        screenY = 0

        If point Is Nothing Then Return False

        If Not Viewport.Contains(point) Then
            Return False
        End If

        Dim worldWidth As Double = Viewport.Right - Viewport.Left
        Dim worldHeight As Double = Viewport.Top - Viewport.Bottom

        If worldWidth <= 0 OrElse worldHeight <= 0 Then
            Return False
        End If

        Dim normalisedX As Double = (point.X - Viewport.Left) / worldWidth

        Dim normalisedY As Double = (Viewport.Top - point.Y) / worldHeight

        screenX = CInt(Math.Floor(normalisedX * Width))
        screenY = CInt(Math.Floor(normalisedY * Height))

        ' Include points exactly on the right and bottom edges.
        screenX = Math.Min(screenX, Width - 1)
        screenY = Math.Min(screenY, Height - 1)

        Return screenX >= 0 AndAlso screenX < Width AndAlso screenY >= 0 AndAlso screenY < Height

    End Function

End Class