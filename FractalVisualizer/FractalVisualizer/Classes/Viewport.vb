
Public Structure ViewportBounds

    Public Left As Double
    Public Right As Double
    Public Top As Double
    Public Bottom As Double

    Public Sub New(
        leftValue As Double,
        rightValue As Double,
        topValue As Double,
        bottomValue As Double
    )

        Left = leftValue
        Right = rightValue
        Top = topValue
        Bottom = bottomValue

    End Sub

    Public Function Contains(point As Fpoint) As Boolean

        If point Is Nothing Then Return False

        Return (point.X >= Left AndAlso
               point.X <= Right AndAlso
               point.Y >= Bottom AndAlso
               point.Y <= Top)

    End Function

End Structure
