Public Class Transformation
    'Temp Vals
    Public Expression As MathsExpression
    Public Probability As Double
    Public Index As Integer

    Public Sub New(TExpression As String, RelativeProbability As Double, i As Integer)
        Probability = RelativeProbability
        Expression = New MathsExpression(TExpression)
        Index = i
    End Sub

    Public Function ApplyTransformation(Point As Fpoint) As Fpoint

        Dim NewPoint As New Fpoint

        Dim TCoords As Double() =
        Expression.Evaluate(Point.X, Point.Y)

        NewPoint.X = TCoords(0)
        NewPoint.Y = TCoords(1)

        NewPoint.Transformation = Index

        Return NewPoint

    End Function
End Class
