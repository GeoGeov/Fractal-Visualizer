Public Class FunctionExpression
    Inherits Expression

    Public ReadOnly Name As String
    Public ReadOnly Arguments As List(Of Expression)

    Private Shared ReadOnly RandomGenerator As New Random()
    Private Shared ReadOnly RandomLock As New Object()

    Public Sub New(name As String,
                   arguments As List(Of Expression))

        Me.Name = name.ToLowerInvariant()
        Me.Arguments = arguments

    End Sub

    Public Overrides Function Evaluate(x As Double, y As Double) As Double
        'Evaluate each argument once
        Dim values As New List(Of Double)

        For Each argument As Expression In Arguments
            values.Add(argument.Evaluate(x, y))
        Next

        'long case to evaluate the function based on its name
        'prolly a better way to do this 
        Select Case Name

            Case "sin"
                RequireArgumentCount(1)
                Return Math.Sin(values(0))

            Case "cos"
                RequireArgumentCount(1)
                Return Math.Cos(values(0))

            Case "tan"
                RequireArgumentCount(1)
                Return Math.Tan(values(0))
            Case "arcsin", "asin"
                RequireArgumentCount(1)
                Return Math.Asin(values(0))

            Case "arccos", "acos"
                RequireArgumentCount(1)
                Return Math.Acos(values(0))

            Case "arctan", "atan"
                RequireArgumentCount(1)
                Return Math.Atan(values(0))

            Case "sqrt"
                RequireArgumentCount(1)
                Return Math.Sqrt(values(0))

            Case "abs"
                RequireArgumentCount(1)
                Return Math.Abs(values(0))

            Case "exp"
                RequireArgumentCount(1)
                Return Math.Exp(values(0))

            Case "log", "ln"
                RequireArgumentCount(1)
                Return Math.Log(values(0))

            Case "min"
                RequireArgumentCount(2)
                Return Math.Min(values(0), values(1))

            Case "max"
                RequireArgumentCount(2)
                Return Math.Max(values(0), values(1))

            Case "clamp"
                RequireArgumentCount(3)
                Return Math.Max(values(0), Math.Min(values(1), values(2)))

            Case "sign"
                RequireArgumentCount(1)
                Return Math.Sign(values(0))

            Case "floor", "flr"
                RequireArgumentCount(1)
                Return Math.Floor(values(0))

            Case "ceil", "ceiling"
                RequireArgumentCount(1)
                Return Math.Ceiling(values(0))

            Case "round"
                RequireArgumentCount(1)
                Return Math.Round(values(0))

            Case "rnd", "random", "rand"
#Region "random"
                If values.Count = 0 Then
                    Return NextRandom()
                End If

                RequireArgumentCount(2)

                Dim minimum As Double = values(0)
                Dim maximum As Double = values(1)

                If Double.IsNaN(minimum) OrElse
                   Double.IsNaN(maximum) OrElse
                   Double.IsInfinity(minimum) OrElse
                   Double.IsInfinity(maximum) OrElse
                   minimum > maximum Then

                    Throw New ArgumentException(
                        "random requires finite bounds " &
                        "where minimum <= maximum.")

                End If

                Return minimum +
                    (maximum - minimum) * NextRandom()
#End Region
            Case Else
                Throw New ArgumentException(
                    "Unknown function: " & Name)

        End Select

    End Function

    Private Sub RequireArgumentCount(expected As Integer)
        'checks args
        'if wrong cries about it
        If Arguments.Count <> expected Then
            Throw New ArgumentException(Name & " requires " & expected & " argument(s).")
        End If

    End Sub

    Private Shared Function NextRandom() As Double

        SyncLock RandomLock
            Return RandomGenerator.NextDouble()
        End SyncLock

    End Function

End Class