Public Class TransformationColouring
    Inherits ColouringAlgorithm

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Transformation"
        End Get
    End Property

    Public Property Colours As New List(Of Color) From {
        Color.Red,
        Color.Blue,
        Color.Green,
        Color.Yellow,
        Color.Purple
    }

    Public Overrides Function GenerateBitmap(context As ColouringContext) As Bitmap

        Dim pixels(context.Width * context.Height - 1) As Integer

        For Each point As Fpoint In context.Points
            Dim screenX As Integer
            Dim screenY As Integer

            If Not context.TryWorldToScreen(point, screenX, screenY) Then
                Continue For
            End If

            If point.Transformation < 0 OrElse point.Transformation >= Colours.Count Then
                Continue For

            End If

            Dim pixelIndex As Integer = screenY * context.Width + screenX

            pixels(pixelIndex) = Colours(point.Transformation).ToArgb()

        Next

        Return BitmapWriter.CreateBitmap(pixels, context.Width, context.Height)

    End Function

End Class