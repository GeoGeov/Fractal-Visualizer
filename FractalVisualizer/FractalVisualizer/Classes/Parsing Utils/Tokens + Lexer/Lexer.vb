Public Class Lexer
    Private ReadOnly Text As String
    Private Position As Integer
    Private ReadOnly Tokens As List(Of Token)

    Public Sub New(text As String)
        Me.Text = text
        Position = 0
        Tokens = New List(Of Token)()
        Tokenize()
    End Sub

    Public Function GetTokens() As List(Of Token)
        Return Tokens
    End Function

    Private Sub Tokenize()
        While Position < Text.Length
            Dim ch = Text(Position)

            If Char.IsWhiteSpace(ch) Then
                Position += 1
                Continue While
            End If

            If Char.IsDigit(ch) OrElse ch = "."c Then
                Dim start = Position
                While Position < Text.Length AndAlso (Char.IsDigit(Text(Position)) OrElse Text(Position) = "."c)
                    Position += 1
                End While
                Dim numberText = Text.Substring(start, Position - start)
                Dim value As Double
                If Double.TryParse(numberText, value) Then
                    Tokens.Add(New Token(value, numberText))
                Else
                    Tokens.Add(New Token(TokenType.Identifier, numberText))
                End If
                Continue While
            End If

            If Char.IsLetter(ch) Then
                Dim start = Position
                While Position < Text.Length AndAlso Char.IsLetterOrDigit(Text(Position))
                    Position += 1
                End While
                Dim ident = Text.Substring(start, Position - start)
                Tokens.Add(New Token(TokenType.Identifier, ident))
                Continue While
            End If

            Select Case ch
                'c specifies a character
                Case "+"c
                    Tokens.Add(New Token(TokenType.Plus, ch.ToString()))
                Case "-"c
                    Tokens.Add(New Token(TokenType.Minus, ch.ToString()))
                Case "*"c
                    Tokens.Add(New Token(TokenType.Multiply, ch.ToString()))
                Case "/"c
                    Tokens.Add(New Token(TokenType.Divide, ch.ToString()))
                Case "^"c
                    Tokens.Add(New Token(TokenType.Power, ch.ToString()))
                Case "("c
                    Tokens.Add(New Token(TokenType.LeftParen, ch.ToString()))
                Case ")"c
                    Tokens.Add(New Token(TokenType.RightParen, ch.ToString()))
                Case ","c
                    Tokens.Add(New Token(TokenType.Comma, ch.ToString()))
                Case Else
                    ' Unknown character, treat as identifier
                    Tokens.Add(New Token(TokenType.Identifier, ch.ToString()))
            End Select

            Position += 1
        End While

        ' Ensure EOF token is present
        Tokens.Add(New Token(TokenType.EndOfFile, String.Empty))
    End Sub
End Class
