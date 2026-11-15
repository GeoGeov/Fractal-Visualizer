Public Class ColourLayer
    Implements IDisposable

    Public Property Algorithm As ColouringAlgorithm
    'normal by default
    Public Property BlendMode As BlendMode = BlendMode.Normal
    Public Property Enabled As Boolean = True

    Private cachedBitmap As Bitmap

    Public Sub New(algorithm As ColouringAlgorithm)
        Me.Algorithm = algorithm
    End Sub

    Public Function GetBitmap(context As ColouringContext) As Bitmap
        If cachedBitmap Is Nothing Then
            cachedBitmap = Algorithm.GenerateBitmap(context)
        End If

        Return cachedBitmap

    End Function

    Public Sub Invalidate()
        If cachedBitmap IsNot Nothing Then
            cachedBitmap.Dispose()
            cachedBitmap = Nothing
        End If

    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Invalidate()
    End Sub

End Class