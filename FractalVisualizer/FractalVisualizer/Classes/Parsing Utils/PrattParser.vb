Imports System.Linq.Expressions

Public Class PrattParser
    Private ReadOnly Tokens As List(Of Token)
    Private Position As Integer

    Public Sub New(tokens As List(Of Token))
        Me.Tokens = tokens
        Position = 0
    End Sub

    Public Function Parse() As Expression
        Dim result As Expression = ParseExpression(0)

        'Ensure the entire expression was parsed
        If Current().Type <> TokenType.EndOfFile Then
            Throw New Exception(
            "Unexpected token: " & Current().Text)
        End If

        Return result

    End Function


    Private Function Current() As Token
        'if position is past the end, return an EOF token
        If Position < Tokens.Count Then
            Return Tokens(Position)
        End If
        Return New Token(TokenType.EndOfFile, String.Empty)
    End Function

    Private Function Advance() As Token
        Dim token = Current()
        If Position < Tokens.Count Then
            Position += 1
        End If
        Return token
    End Function

    Private Function ParseExpression(minimumBindingPower As Integer) As Expression

        'for prefix operators
        Dim left As Expression = ParsePrefix()

        While True
            'for infix operators
            Dim currentToken = Current()

            ' Stop at EOF to avoid consuming the EOF token as an operator
            If currentToken.Type = TokenType.EndOfFile Then
                Exit While
            End If

            Dim bindingPower = GetBindingPower(currentToken.Type)

            ' If the operator's binding power is not strong enough, stop parsing infix operators
            If bindingPower <= minimumBindingPower Then
                Exit While
            End If

            Advance()

            'recursively parse the right-hand side of the expression with a higher binding power
            Dim right = ParseExpression(bindingPower + 1)
            left = New BinaryOperatorExpression(left, currentToken.Type, right)
        End While

        Return left

    End Function

    Private Function GetBindingPower(type As TokenType) As Integer

        'tells the parser how tightly binding the operator is, which determines the order of operations
        Select Case type
            Case TokenType.Plus, TokenType.Minus
                Return 10
            Case TokenType.Multiply, TokenType.Divide
                Return 20
            Case TokenType.Power
                Return 30
            Case Else
                Return -1

        End Select

    End Function

    Private Function ParsePrefix() As Expression

        Dim token = Advance()

        Select Case token.Type

            Case TokenType.Number
                Return New NumberExpression(token.NumberValue)

            Case TokenType.Identifier
                If Current().Type = TokenType.LeftParen Then
                    'Consume '('
                    Advance()

                    Dim arguments As New List(Of Expression)

                    'Allow zero arguments, e.g. random()
                    If Current().Type <> TokenType.RightParen Then

                        arguments.Add(ParseExpression(0))

                        'Parse additional comma-separated arguments
                        While Current().Type = TokenType.Comma

                            Advance() 'Consume comma

                            arguments.Add(ParseExpression(0))

                        End While

                    End If

                    'Require closing ')'
                    If Current().Type <> TokenType.RightParen Then
                        Throw New Exception(
                "Expected ')' after function arguments.")
                    End If

                    Advance()

                    Return New FunctionExpression(
            token.Text,
            arguments)

                End If

                'If the identifier is not followed by a '(', treat it as a variable
                Return New VariableExpression(token.Text)

            Case TokenType.Plus, TokenType.Minus
                'parse the operand of the unary operator with a higher binding power
                'because unary operators have higher precedence than binary operators
                Dim operand = ParseExpression(25)
                Return New UnaryExpression(token.Type, operand)

            'recursively parse the expression inside the parentheses
            Case TokenType.LeftParen
                Dim expression = ParseExpression(0)
                If Current().Type <> TokenType.RightParen Then
                    Throw New Exception("Expected ')'.")
                End If

                Advance()
                Return expression
            Case TokenType.EndOfFile
                Throw New Exception("Unexpected end of expression.")

            Case Else
                Throw New Exception("Unexpected token: " & token.Text)
        End Select

    End Function

End Class