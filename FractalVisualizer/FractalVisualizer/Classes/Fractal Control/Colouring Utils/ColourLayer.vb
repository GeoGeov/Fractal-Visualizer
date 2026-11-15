Public Class ColourLayer
    Implements IDisposable

    Private tAlgorithm As ColouringAlgorithm
    Private CachedBitmap As Bitmap

    Public Property BlendMode As BlendMode =
        BlendMode.Normal

    Public Property Enabled As Boolean = True

    Public Property Algorithm As ColouringAlgorithm
        Get
            Return tAlgorithm
        End Get

        Set(value As ColouringAlgorithm)
            If value Is Nothing Then
                Throw New ArgumentNullException(NameOf(value))
            End If
            If tAlgorithm Is value Then Return

            tAlgorithm = value
            Invalidate()

        End Set

    End Property

    Public Sub New(algorithm As ColouringAlgorithm)
        If algorithm Is Nothing Then
            Throw New ArgumentNullException(NameOf(algorithm))
        End If

        tAlgorithm = algorithm

    End Sub

    Public Function GetBitmap(context As ColouringContext) As Bitmap
        If CachedBitmap Is Nothing Then
            CachedBitmap = Algorithm.GenerateBitmap(context)
        End If

        Return CachedBitmap

    End Function

    Public Sub Invalidate()
        If CachedBitmap IsNot Nothing Then
            CachedBitmap.Dispose()
            CachedBitmap = Nothing
        End If

    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Invalidate()

    End Sub

End Class