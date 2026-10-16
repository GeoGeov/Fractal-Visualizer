Public Class NumberExpression
    Inherits Expression

    Public ReadOnly Value As Double

    Public Sub New(value As Double)
        Me.Value = value
    End Sub

    'when evaluating a number expression returns the value of the number expression
    Public Overrides Function Evaluate(x As Double, y As Double) As Double
        Return Value
    End Function

End Class