Imports System.Drawing
Imports System.Drawing.Imaging


Module FractalRenderer
    'Turn Point co-ords onto on screen co-ords
    'Add zoom and pan functionality
    'Draw points to a bitmap and then display the bitmap on the form
    'Camera
    Public Static Class Camera
        'screen centere co-ords
        'default to 0,0
        Public X As Double = 0
        Public Y As Double = 0
        'default zoom to 1
        Public Zoom As Double = 1
    End Class

    Private Function WorldToScreen(Point As Fpoint, Width As Integer, Height As Integer) As Point

        Dim Scale As Double = Math.Min(Width, Height) / 2.0
        Scale *= Camera.Zoom
        'Convert  maths co-ords to screen co-ords
        'Screen Co-ords are integer
        Dim ScreenX As Integer = CInt(Width / 2.0 + (Point.X - Camera.X) * Scale)
        Dim ScreenY As Integer = CInt(Height / 2.0 - (Point.Y - Camera.Y) * Scale)

        Return New Point(ScreenX, ScreenY)

    End Function

    Public Function Render(Points As List(Of Fpoint), Width As Integer, Height As Integer) As Bitmap

        'initialize bitmap with 32bpp ARGB pixel format for better color depth 
        'colour depth for future colour function addition
        Dim Bitmap As New Bitmap(Width, Height, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(Bitmap)
            'White BG
            g.Clear(Color.white)
            For Each P As Fpoint In Points
                Dim ScreenPoint As Point =
                    WorldToScreen(P, Width, Height)

                'Ignore points outside the screen
                If ScreenPoint.X >= 0 AndAlso
                   ScreenPoint.X < Width AndAlso
                   ScreenPoint.Y >= 0 AndAlso
                   ScreenPoint.Y < Height Then

                    'Draw the point on the bitmap
                    'For now, just draw a black pixel for each point
                    Bitmap.SetPixel(ScreenPoint.X, ScreenPoint.Y, Color.Black)

                End If
            Next
        End Using
        Return Bitmap
    End Function
End Module
