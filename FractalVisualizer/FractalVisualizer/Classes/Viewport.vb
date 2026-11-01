Public Structure ViewportBounds

    Public Property Left As Double
    Public Property Right As Double
    Public Property Top As Double
    Public Property Bottom As Double

    Public Sub New(leftValue As Double, rightValue As Double, topValue As Double, bottomValue As Double)
        Left = leftValue
        Right = rightValue
        Top = topValue
        Bottom = bottomValue
    End Sub

    Public Function Contains(point As Fpoint) As Boolean
        If point Is Nothing Then Return False

        'if in true
        Return point.X >= Left AndAlso
               point.X <= Right AndAlso
               point.Y >= Bottom AndAlso
               point.Y <= Top

    End Function

    Public Function Expand(margin As Double) As ViewportBounds
        'returns a new ViewportBounds that is expanded by the specified margin on all sides.

        Dim width As Double = Right - Left
        Dim height As Double = Top - Bottom

        Dim marginX As Double = width * margin
        Dim marginY As Double = height * margin

        Return New ViewportBounds(
            Left - marginX,
            Right + marginX,
            Top + marginY,
            Bottom - marginY
        )

    End Function

End Structure

