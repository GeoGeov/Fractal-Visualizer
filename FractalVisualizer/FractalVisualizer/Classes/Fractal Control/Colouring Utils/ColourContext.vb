Public Class ColouringContext

    Public ReadOnly Property Store As PointStorage
    Public ReadOnly Property Viewport As ViewportBounds
    Public ReadOnly Property Points As IReadOnlyList(Of Fpoint)
    Public Property MaxIterations As Integer
    Public Property Width As Integer
    Public Property Height As Integer

    Public Sub New(store As PointStorage, viewport As ViewportBounds, width As Integer, height As Integer, maxIterations As Integer)
        Me.Store = store
        Me.Viewport = viewport

        ' Take one snapshot for all colouring algorithms.
        Me.Points = store.GetSnapshot()

        Me.Width = width
        Me.Height = height
        Me.MaxIterations = maxIterations
    End Sub

End Class