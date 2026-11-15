Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Public Class BitmapWriter

    Public Shared Function CreateBitmap(pixels As Integer(), width As Integer, height As Integer) As Bitmap

        If pixels Is Nothing Then
            Throw New ArgumentNullException(NameOf(pixels))
        End If

        If width <= 0 OrElse height <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(width))
        End If

        If pixels.Length <> width * height Then
            Throw New ArgumentException("Pixel array size does not match bitmap dimensions.")
        End If

        Dim bitmap As New Bitmap(width, height, PixelFormat.Format32bppArgb)
        Dim rect As New Rectangle(0, 0, width, height)
        Dim bitmapData As BitmapData = Nothing

        Try

            bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)
            Dim rowPixels(width - 1) As Integer

            For y As Integer = 0 To height - 1

                Array.Copy(pixels, y * width, rowPixels, 0, width)

                Dim rowAddress As IntPtr = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride)
                Marshal.Copy(rowPixels, 0, rowAddress, width)
            Next

        Catch

            bitmap.Dispose()
            Throw

        Finally

            If bitmapData IsNot Nothing Then
                bitmap.UnlockBits(bitmapData)
            End If

        End Try

        Return bitmap

    End Function

End Class
