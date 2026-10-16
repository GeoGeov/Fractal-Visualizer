Public Class Transformation
    'Temp Vals
    Public TXExpression As MathsExpression
    Public TYExpression As MathsExpression
    Public Probability As Double
    Public Index As Integer

    Public Sub New(TempExpression As String, RelativeProbability As Double, i As Integer)
        Probability = RelativeProbability
        'Expect expression in form "<x expression>, <y expression>"
        Dim parts = TempExpression.Split({","c}, 2)
        If parts.Length = 2 Then
            TXExpression = New MathsExpression(parts(0).Trim())
            TYExpression = New MathsExpression(parts(1).Trim())
        Else
            'If only one expression provided, use it for both coordinates
            TXExpression = New MathsExpression(TempExpression)
            TYExpression = New MathsExpression(TempExpression)
        End If
        Index = i
    End Sub

    Public Function ApplyTransformation(Point As Fpoint) As Fpoint

        Dim NewPoint As Fpoint

        Dim x As Double = TXExpression.Evaluate(Point.X, Point.Y)
        Dim y As Double = TYExpression.Evaluate(Point.X, Point.Y)

        NewPoint = New Fpoint(x, y, Index, Point.Iteration + 1)


        Return NewPoint

    End Function
End Class
