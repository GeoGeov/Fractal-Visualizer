Public Class TransformationColouring
    Inherits ColouringAlgorithm

    Public Overrides ReadOnly Property Name As String
        Get
            Return "Transformation"
        End Get
    End Property

    'temp list of colours for each transformation
    'to be changeable by ui in future
    Public Property Colours As New List(Of Color) From {
        Color.Red,
        Color.Blue,
        Color.Green,
        Color.Yellow,
        Color.Purple
    }

    Public Overrides Function GenerateBitmap(context As ColouringContext) As Bitmap
        Dim bitmap As New Bitmap(context.Width, context.Height, Imaging.PixelFormat.Format32bppArgb)

        For Each point As Fpoint In context.Points

            If Not context.Viewport.Contains(point) Then Continue For

            'temp need add a shared world to screen point conversion function
            'may rework render later to use shared world to screen point conversion function
            Dim screenPoint = point

            If point.Transformation < 0 OrElse
               point.Transformation >= Colours.Count Then Continue For

            If screenPoint.X < 0 OrElse screenPoint.X >= context.Width OrElse
               screenPoint.Y < 0 OrElse screenPoint.Y >= context.Height Then Continue For

            bitmap.SetPixel(screenPoint.X, screenPoint.Y, Colours(point.Transformation))

        Next

        Return bitmap

    End Function

End Class
