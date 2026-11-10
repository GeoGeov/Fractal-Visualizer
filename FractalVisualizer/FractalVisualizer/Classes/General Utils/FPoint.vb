Public Class Fpoint
    'Co-ords
    Public X As Double
    Public Y As Double

    'Utils
    Public Transformation As Integer
    Public Iteration As Integer

    'To instatiate a new point with all values
    Public Sub New(X As Double, Y As Double, Optional Transformation As Integer = -1, Optional Iteration As Integer = 0)

        Me.X = X
        Me.Y = Y
        Me.Transformation = Transformation
        Me.Iteration = Iteration

    End Sub
End Class