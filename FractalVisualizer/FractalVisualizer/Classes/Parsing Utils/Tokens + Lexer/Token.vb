Public Class Token

    Public Property Type As TokenType
    Public Property Text As String
    Public Property NumberValue As Double

    Public Sub New(type As TokenType, text As String)
        Me.Type = type
        Me.Text = text
    End Sub

    Public Sub New(value As Double, text As String)
        Type = TokenType.Number
        Me.Text = text
        NumberValue = value
    End Sub

End Class