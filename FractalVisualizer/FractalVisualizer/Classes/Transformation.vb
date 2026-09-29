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

        Dim NewPoint As Fpoint

        Dim TCoords As Double() =
        Expression.Evaluate(Point.X, Point.Y)

        NewPoint = New Fpoint(TCoords(0), TCoords(1), Index, Point.Iteration + 1)


        Return NewPoint

    End Function
End Class
