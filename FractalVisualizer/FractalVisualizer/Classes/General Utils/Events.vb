Public Class RenderCompletedEventArgs
    Inherits EventArgs

    Public ReadOnly Property Bitmap As Bitmap

    Public Sub New(bitmap As Bitmap)
        Me.Bitmap = bitmap
    End Sub

End Class

Public Class RenderFailedEventArgs
    Inherits EventArgs

    Public ReadOnly Property Exception As Exception

    Public Sub New(exception As Exception)
        Me.Exception = exception
    End Sub

End Class