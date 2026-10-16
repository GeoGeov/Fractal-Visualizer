Public Class BinaryOperatorExpression
    Inherits Expression

    Public ReadOnly Left As Expression
    Public ReadOnly OperatorType As TokenType
    Public ReadOnly Right As Expression

    Public Sub New(left As Expression, operatorType As TokenType, right As Expression)
        Me.Left = left
        Me.OperatorType = operatorType
        Me.Right = right
    End Sub

    Public Overrides Function Evaluate(x As Double, y As Double) As Double

        'makes expressions tightly binding 
        'evaluates expressions to left and right of the operator
        'how tightly binding the operator is to be determined by the parser
        Dim leftValue = Left.Evaluate(x, y)
        Dim rightValue = Right.Evaluate(x, y)

        'performs the operation based on the operator type
        Select Case OperatorType
            Case TokenType.Plus
                Return leftValue + rightValue
            Case TokenType.Minus
                Return leftValue - rightValue
            Case TokenType.Multiply
                Return leftValue * rightValue
            Case TokenType.Divide
                Return leftValue / rightValue
            Case TokenType.Power
                Return Math.Pow(leftValue, rightValue)
            Case Else
                Throw New Exception("Invalid binary operator.")
        End Select

    End Function

End Class
