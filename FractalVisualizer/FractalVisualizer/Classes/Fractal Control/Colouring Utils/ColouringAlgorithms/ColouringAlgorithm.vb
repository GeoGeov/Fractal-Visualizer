Public MustInherit Class ColouringAlgorithm

    Public MustOverride ReadOnly Property Name As String
    Public MustOverride Function GenerateBitmap(context As ColouringContext) As Bitmap

End Class
