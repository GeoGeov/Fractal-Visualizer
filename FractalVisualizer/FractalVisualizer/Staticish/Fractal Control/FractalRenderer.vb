Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

Public Class FractalRenderer
    Implements IDisposable

#Region "Private Fields"
    Private LastSeenTotal As Long = -1
    Private NeedsFullRedraw As Boolean = True

    Private ReadOnly ViewCamera As Camera

    Private Const PointArgb As Integer = &HFF000000
#End Region

    'i want to be private but made public for ease
    Public Property Surface As Bitmap


    Public Sub New(camera As Camera)
        ViewCamera = camera
    End Sub

    Public Sub Invalidate()
        NeedsFullRedraw = True
    End Sub

    Public Function Update(
    store As PointStorage,
    width As Integer,
    height As Integer
) As Bitmap

        If width <= 0 OrElse height <= 0 Then Return Nothing

        Dim sizeChanged As Boolean =
        Surface Is Nothing OrElse
        Surface.Width <> width OrElse
        Surface.Height <> height

        If sizeChanged Then

            If Surface IsNot Nothing Then
                Surface.Dispose()
            End If

            Surface = New Bitmap(
            width,
            height,
            PixelFormat.Format32bppArgb
        )

            NeedsFullRedraw = True

        End If

        Dim rebuildRequired As Boolean = False
        Dim latestTotal As Long = 0

        Dim changedPoints As List(Of Fpoint) =
        store.GetChanges(
            LastSeenTotal,
            rebuildRequired,
            latestTotal
        )

        If NeedsFullRedraw OrElse rebuildRequired Then

            Dim snapshotTotal As Long

            Dim allPoints As List(Of Fpoint) =
            store.GetSnapshot(snapshotTotal)

            Using g As Graphics = Graphics.FromImage(Surface)
                g.Clear(Color.White)
            End Using

            PlotPoints(allPoints, width, height)

            LastSeenTotal = snapshotTotal
            NeedsFullRedraw = False

        Else

            If changedPoints IsNot Nothing AndAlso
           changedPoints.Count > 0 Then

                PlotPoints(changedPoints, width, height)

            End If

            LastSeenTotal = latestTotal

        End If

        Return Surface

    End Function

    Private Sub PlotPoints(
        points As List(Of Fpoint),
        width As Integer,
        height As Integer
    )

        If points Is Nothing OrElse points.Count = 0 Then Return

        Dim rect As New Rectangle(0, 0, width, height)

        Dim data As BitmapData =
            Surface.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            )

        Try
            Dim stride As Integer = data.Stride
            Dim baseAddress As IntPtr = data.Scan0

            Dim scale As Double =
                Math.Min(width, height) * ViewCamera.Zoom

            Dim centreX As Double = width / 2.0
            Dim centreY As Double = height / 2.0

            For Each p As Fpoint In points
                If p Is Nothing Then Continue For

                Dim sx As Double =
                    centreX + (p.X - ViewCamera.X) * scale

                Dim sy As Double =
                    centreY - (p.Y - ViewCamera.Y) * scale

                If Double.IsNaN(sx) OrElse Double.IsNaN(sy) OrElse
                   Double.IsInfinity(sx) OrElse Double.IsInfinity(sy) Then
                    Continue For
                End If

                If sx < 0 OrElse sx >= width OrElse
                   sy < 0 OrElse sy >= height Then
                    Continue For
                End If

                Dim px As Integer = CInt(Math.Floor(sx))
                Dim py As Integer = CInt(Math.Floor(sy))

                Dim rowAddress As IntPtr =
                    IntPtr.Add(baseAddress, py * stride)

                Marshal.WriteInt32(
                    rowAddress,
                    px * 4,
                    PointArgb
                )
            Next

        Finally
            Surface.UnlockBits(data)
        End Try
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If Surface IsNot Nothing Then
            Surface.Dispose()
            Surface = Nothing
        End If
    End Sub

End Class
