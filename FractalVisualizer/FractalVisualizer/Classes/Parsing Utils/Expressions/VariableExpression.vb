Public Class VariableExpression
    Inherits Expression

    Public ReadOnly Name As String

    Public Sub New(name As String)
        Me.Name = name
    End Sub

    'Evaluate predefined variables
    'To be expanded to include user-defined variables in the future
    Public Overrides Function Evaluate(x As Double, y As Double) As Double

        Select Case Name.ToLower()

            Case "x"
                Return x

            Case "y"
                Return y

            Case "pi"
                Return Math.PI

            Case "e"
                Return Math.E

            Case Else

                Throw New Exception(
                    "Unknown variable: " & Name)

        End Select

    End Function

End Class