Public Class UnaryExpression
    Inherits Expression

    Public ReadOnly OperatorType As TokenType
    Public ReadOnly Operand As Expression

    Public Sub New(operatorType As TokenType, operand As Expression)
        Me.OperatorType = operatorType
        Me.Operand = operand
    End Sub

    'evaluates expressions such as -x or +y, whereas operatorexpression evaluates x + y
    Public Overrides Function Evaluate(x As Double, y As Double) As Double
        Dim value = Operand.Evaluate(x, y)

        Select Case OperatorType
            Case TokenType.Minus
                Return -value
            Case TokenType.Plus
                Return value
            Case Else
                Throw New Exception("Invalid unary operator.")
        End Select
    End Function

End Class
