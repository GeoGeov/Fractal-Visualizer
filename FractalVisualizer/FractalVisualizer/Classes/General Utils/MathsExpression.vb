Public Class MathsExpression

    Private ReadOnly Root As Expression

    Public ReadOnly StringExpression As String

    Public Sub New(expression As String)

        StringExpression = expression

        Dim lexer As New Lexer(expression)
        Dim tokens = lexer.GetTokens()
        Dim parser As New PrattParser(tokens)

        'on creation, parse the expression and store the root node for evaluation
        Root = parser.Parse()

    End Sub

    Public Function Evaluate(x As Double, y As Double) As Double
        Return Root.Evaluate(x, y)

    End Function

End Class
