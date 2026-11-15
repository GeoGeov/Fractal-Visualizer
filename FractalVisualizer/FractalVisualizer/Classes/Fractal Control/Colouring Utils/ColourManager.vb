Public Class ColouringManager
    Implements IDisposable

    Public ReadOnly Property Layers As New List(Of ColourLayer)

    Public Function AddLayer(
        algorithm As ColouringAlgorithm
    ) As ColourLayer

        Dim layer As New ColourLayer(algorithm)

        Layers.Add(layer)

        Return layer

    End Function

    Public Sub RemoveLayer(layer As ColourLayer)

        If layer Is Nothing Then Return

        If Layers.Remove(layer) Then
            layer.Dispose()
        End If

    End Sub

    Public Function GenerateBitmap(
        context As ColouringContext
    ) As Bitmap

        If context Is Nothing Then
            Throw New ArgumentNullException(NameOf(context))
        End If

        Dim result As New Bitmap(
            context.Width,
            context.Height,
            Imaging.PixelFormat.Format32bppArgb
        )

        Try

            Using g As Graphics = Graphics.FromImage(result)
                g.Clear(Color.Transparent)
            End Using

            For Each layer As ColourLayer In Layers

                If Not layer.Enabled Then Continue For

                Dim layerBitmap As Bitmap =
                    layer.GetBitmap(context)

                ColourBlender.BlendBitmaps(
                    result,
                    layerBitmap,
                    layer.BlendMode
                )

            Next

            Return result

        Catch

            result.Dispose()
            Throw

        End Try

    End Function

    Public Sub InvalidateAll()

        For Each layer As ColourLayer In Layers
            layer.Invalidate()
        Next

    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose

        For Each layer As ColourLayer In Layers
            layer.Dispose()
        Next

        Layers.Clear()

    End Sub

End Class