Public Class Transformation
    'Temp Vals
    Public MathsExpression As String
    Public Probability As Double
    Public Index As Integer

    Public Sub New(Expression As String, RelativeProbability As Double, i As Integer)
        Probability = RelativeProbability
        MathsExpression = Expression
        Index = i
    End Sub

    Public Function ApplyTransformation(Point As Fpoint) As Fpoint
        Dim NewPoint As New Fpoint
        Dim X As Double = Point.X
        Dim Y As Double = Point.Y

        'Using MathsEvaluator to evaluate the expression
        'Add code when evaluator created


        'Set Point Function to this functions index
        NewPoint.Transformation = Index
        NewPoint.Iteration = Point.Iteration + 1

        Return NewPoint
    End Function
End Class
