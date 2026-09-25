Public Class Fpoint
    'Co-ords
    Public X As Double
    Public Y As Double

    'Utils
    Public Transformation As Integer
    Public Iteration As Integer

    'To instatiate a new point with all values
    Public Sub New(X As Double, Y As Double, Transformation As Integer, Iteration As Integer)

        Me.X = X
        Me.Y = Y
        Me.Transformation = Transformation
        Me.Iteration = Iteration

    End Sub

    'Use for initial point generation
    Public Sub New(X As Double, Y As Double)
        Me.X = X
        Me.Y = Y
        Me.Transformation = -1
        Me.Iteration = 0
    End Sub
End Class