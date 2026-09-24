Public Class MathsExpression
    'Placeholder Class at moment

    Public StringExpression As String
    Public ParsedExpression As Object ' Placeholder for the parsed expression
    Sub New(Expression As String)
        StringExpression = Expression
    End Sub

    Function Evaluate(X As Double, Y As Double)
        'add code to evaluate the expression using X and Y
        Return {X, Y} 'placeholder return value}
    End Function
End Class
