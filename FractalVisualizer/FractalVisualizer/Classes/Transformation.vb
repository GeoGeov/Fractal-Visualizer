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

    Public Function ApplyTransformation(Point As FPoint) As FPoint
        Dim NewPoint As New FPoint
        Dim X As Double = Point.X
        Dim Y As Double = Point.Y

        'Using MathsEvaluator to evaluate the expression
        Dim TCoords As Double() = Expression.EvaluateExpression(X, Y)
        NewPoint.X = TCoords(0)
        NewPoint.Y = TCoords(1)

        'Set Point Function to this functions index
        NewPoint.Transformation = Index
        NewPoint.Iteration = Point.Iteration + 1

        Return NewPoint
    End Function
End Class
