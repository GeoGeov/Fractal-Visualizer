Public Class FunctionExpression
    Inherits Expression

    Public ReadOnly Name As String
    Public ReadOnly Argument As Expression

    Public Sub New(name As String, argument As Expression)
        Me.Name = name
        Me.Argument = argument
    End Sub

    Public Overrides Function Evaluate(x As Double, y As Double) As Double
        Dim value = Argument.Evaluate(x, y)

        Select Case Name.ToLower()
            Case "sin"
                Return Math.Sin(value)
            Case "cos"
                Return Math.Cos(value)
            Case "tan"
                Return Math.Tan(value)
            Case "sqrt"
                Return Math.Sqrt(value)
            Case "abs"
                Return Math.Abs(value)
            Case "exp"
                Return Math.Exp(value)
            Case "log"
                Return Math.Log(value)
            Case Else
                Throw New Exception("Unknown function: " & Name)

        End Select

    End Function

End Class
