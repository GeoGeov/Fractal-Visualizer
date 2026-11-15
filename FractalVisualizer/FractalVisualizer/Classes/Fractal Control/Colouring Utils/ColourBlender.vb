Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Public Class ColourBlender

    Public Shared Sub BlendBitmaps(backBitmap As Bitmap, frontBitmap As Bitmap, blendMode As BlendMode)

        If backBitmap Is Nothing Then
            Throw New ArgumentNullException(NameOf(backBitmap))
        End If

        If frontBitmap Is Nothing Then
            Throw New ArgumentNullException(NameOf(frontBitmap))
        End If

        If backBitmap.Width <> frontBitmap.Width OrElse
           backBitmap.Height <> frontBitmap.Height Then

            Throw New ArgumentException("Both bitmaps must have the same dimensions.")
        End If

        Dim rect As New Rectangle(0, 0, backBitmap.Width, backBitmap.Height)

        Dim backData As BitmapData = Nothing
        Dim frontData As BitmapData = Nothing

        Try
            backData = backBitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            )

            frontData = frontBitmap.LockBits(
                rect,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            )

            Dim bytesPerPixel As Integer = 4
            Dim width As Integer = backBitmap.Width
            Dim height As Integer = backBitmap.Height

            Dim backBytes(height * backData.Stride - 1) As Byte
            Dim frontBytes(height * frontData.Stride - 1) As Byte

            Marshal.Copy(backData.Scan0, backBytes, 0,
                backBytes.Length
            )

            Marshal.Copy(
                frontData.Scan0,
                frontBytes,
                0,
                frontBytes.Length
            )

            For y As Integer = 0 To height - 1

                For x As Integer = 0 To width - 1

                    Dim index As Integer =
                        y * backData.Stride + x * bytesPerPixel

                    ' Bitmap data is stored in BGRA order.
                    Dim backB As Double = backBytes(index)
                    Dim backG As Double = backBytes(index + 1)
                    Dim backR As Double = backBytes(index + 2)
                    Dim backA As Double = backBytes(index + 3)

                    Dim frontB As Double = frontBytes(index)
                    Dim frontG As Double = frontBytes(index + 1)
                    Dim frontR As Double = frontBytes(index + 2)
                    Dim frontA As Double = frontBytes(index + 3)

                    Dim result As Color = Blend(
                        Color.FromArgb(
                            CInt(backA),
                            CInt(backR),
                            CInt(backG),
                            CInt(backB)
                        ),
                        Color.FromArgb(
                            CInt(frontA),
                            CInt(frontR),
                            CInt(frontG),
                            CInt(frontB)
                        ),
                        blendMode
                    )

                    backBytes(index) = result.B
                    backBytes(index + 1) = result.G
                    backBytes(index + 2) = result.R
                    backBytes(index + 3) = result.A

                Next

            Next

            Marshal.Copy(
                backBytes,
                0,
                backData.Scan0,
                backBytes.Length
            )

        Finally

            If backData IsNot Nothing Then
                backBitmap.UnlockBits(backData)
            End If

            If frontData IsNot Nothing Then
                frontBitmap.UnlockBits(frontData)
            End If

        End Try

    End Sub

    Public Shared Function Blend(backColour As Color, frontColour As Color, blendMode As BlendMode) As Color

        Dim backA As Double = backColour.A / 255.0
        Dim frontA As Double = frontColour.A / 255.0

        ' Calculate the resulting alpha.
        Dim resultA As Double = frontA + backA * (1.0 - frontA)


        If resultA <= 0 Then
            Return Color.Transparent
        End If

        ' Blend each colour channel.
        Dim resultR As Integer = BlendChannel(backColour.R, frontColour.R, backA, frontA, resultA, blendMode)
        Dim resultG As Integer = BlendChannel(backColour.G, frontColour.G, backA, frontA, resultA, blendMode)
        Dim resultB As Integer = BlendChannel(backColour.B, frontColour.B, backA, frontA, resultA, blendMode)

        resultA = Clamp(CInt(Math.Round(resultA * 255)), 0, 255)

        Return Color.FromArgb(resultA, resultR, resultG, resultB)

    End Function

    Private Shared Function BlendChannel(backChannel As Integer, frontChannel As Integer, backAlpha As Double, frontAlpha As Double, resultAlpha As Double, blendMode As BlendMode) As Integer

        Dim back As Double = backChannel / 255.0
        Dim front As Double = frontChannel / 255.0

        Dim blended As Double

        Select Case blendMode
            Case BlendMode.Normal
                blended = front
            Case BlendMode.Add
                blended = Math.Min(1.0, back + front)
            Case BlendMode.Subtract
                blended = Math.Max(0.0, back - front)
            Case BlendMode.Multiply
                blended = back * front
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(blendMode))
        End Select

        ' Composite the blended colour over the background.
        Dim result As Double = (back * backAlpha * (1.0 - frontAlpha) + front * frontAlpha * (1.0 - backAlpha) + blended * backAlpha * frontAlpha) / resultAlpha

        Return Clamp(CInt(Math.Round(result * 255)), 0, 255)

    End Function

    Private Shared Function Clamp(value As Integer, minimum As Integer, maximum As Integer) As Integer

        Return Math.Max(minimum, Math.Min(maximum, value))

    End Function

End Class
