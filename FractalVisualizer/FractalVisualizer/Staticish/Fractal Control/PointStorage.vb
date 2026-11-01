Public Class PointStorage
#Region "Private Fields"
    Private ReadOnly Points As New List(Of Fpoint)
    Private ReadOnly PointSequences As New List(Of Long)
    Private ReadOnly PointsLock As New Object

    Private CurrentViewport As ViewportBounds
    Private HasViewport As Boolean = False

    Private _maxPoints As Integer = 500000
    Private _totalAdded As Long = 0

    ' Indicates that the renderer must redraw the entire bitmap.
    Private _requiresFullRedraw As Boolean = True
#End Region
#Region "Public Properties"
    Public Property MaxPoints As Integer

        Get
            SyncLock PointsLock
                Return _maxPoints
            End SyncLock
        End Get

        Set(value As Integer)

            If value <= 0 Then
                Throw New ArgumentOutOfRangeException(
                    NameOf(value),
                    "Maximum points must be greater than zero."
                )
            End If

            SyncLock PointsLock
                _maxPoints = value
                TrimPoints()
            End SyncLock

        End Set

    End Property

    Public ReadOnly Property Count As Integer

        Get
            SyncLock PointsLock
                Return Points.Count
            End SyncLock
        End Get

    End Property

    Public ReadOnly Property TotalAdded As Long

        Get
            SyncLock PointsLock
                Return _totalAdded
            End SyncLock
        End Get

    End Property
#End Region

    Public Sub UpdateViewport(viewport As ViewportBounds)

        SyncLock PointsLock

            CurrentViewport = viewport
            HasViewport = True

        End SyncLock

    End Sub


#Region "Point Control"
    'called by generator to add a batch of points to the storage.
    Public Sub AddBatch(batch As List(Of Fpoint))

        If batch Is Nothing OrElse batch.Count = 0 Then Return

        SyncLock PointsLock

            For Each point As Fpoint In batch

                If Not IsValidPoint(point) Then Continue For

                If HasViewport AndAlso
                   Not CurrentViewport.Contains(point) Then

                    Continue For

                End If

                Points.Add(point)

                _totalAdded += 1
                PointSequences.Add(_totalAdded)

            Next

            TrimPoints()

        End SyncLock

    End Sub


    'private to remove old or unviewable points if the storage exceeds the maximum allowed.
    Private Sub TrimPoints()

        If Points.Count <= _maxPoints Then Return

        Dim excess As Integer = Points.Count - _maxPoints

        ' First pass: remove points outside the current viewport.
        If HasViewport Then

            Dim writeIndex As Integer = 0
            Dim removed As Integer = 0

            For readIndex As Integer = 0 To Points.Count - 1

                Dim point As Fpoint = Points(readIndex)

                If Not CurrentViewport.Contains(point) AndAlso
               removed < excess Then

                    removed += 1

                Else

                    ' Keep this point and its sequence number.
                    If writeIndex <> readIndex Then
                        Points(writeIndex) = Points(readIndex)
                        PointSequences(writeIndex) = PointSequences(readIndex)
                    End If

                    writeIndex += 1

                End If

            Next

            If removed > 0 Then

                Points.RemoveRange(writeIndex, removed)
                PointSequences.RemoveRange(writeIndex, removed)

            End If

            excess -= removed

        End If

        ' Second pass: if necessary, remove the oldest remaining points.
        If excess > 0 Then

            Points.RemoveRange(0, excess)
            PointSequences.RemoveRange(0, excess)

        End If

        ' The renderer must redraw because points were removed.
        _requiresFullRedraw = True

    End Sub

    Private Function IsValidPoint(point As Fpoint) As Boolean

        If point Is Nothing Then Return False

        If Double.IsNaN(point.X) OrElse
           Double.IsNaN(point.Y) Then Return False

        If Double.IsInfinity(point.X) OrElse
           Double.IsInfinity(point.Y) Then Return False

        Return True

    End Function

    'when needed
    Public Sub Clear()

        SyncLock PointsLock

            Points.Clear()
            PointSequences.Clear()

            _totalAdded = 0

            _requiresFullRedraw = True

        End SyncLock

    End Sub
#End Region

#Region "Snapshots"
    'allows the renderer to get a snapshot of the current points without locking the storage for an extended period.
    'one version returns the current total number of points, the other does not.
    Public Function GetSnapshot() As List(Of Fpoint)

        SyncLock PointsLock
            Return New List(Of Fpoint)(Points)
        End SyncLock

    End Function
    Public Function GetSnapshot(ByRef latestTotal As Long) As List(Of Fpoint)

        SyncLock PointsLock

            latestTotal = _totalAdded

            Return New List(Of Fpoint)(Points)

        End SyncLock

    End Function

    'provides a snapshot of what has changed since the last render, and whether a full redraw is required.
    Public Function GetChanges(lastSeenTotal As Long, ByRef rebuildRequired As Boolean, ByRef latestTotal As Long) As List(Of Fpoint)

        SyncLock PointsLock
            latestTotal = _totalAdded
            rebuildRequired = _requiresFullRedraw

            'if rebuild is required, we don't need to return any changes, since the renderer will redraw everything anyway.
            If rebuildRequired Then
                _requiresFullRedraw = False
                Return New List(Of Fpoint)
            End If

            Dim changes As New List(Of Fpoint)

            ' Binary search for the first point greater than lastSeenTotal in PointSequences.
#Region "Binary Search"

            Dim low As Integer = 0
            Dim high As Integer = PointSequences.Count - 1
            Dim firstNew As Integer = PointSequences.Count

            While low <= high

                Dim middle As Integer = low + (high - low) \ 2

                If PointSequences(middle) > lastSeenTotal Then
                    firstNew = middle
                    high = middle - 1
                Else
                    low = middle + 1
                End If

            End While
#End Region

            ' Only copy points added since the previous render.
            For i As Integer = firstNew To Points.Count - 1
                changes.Add(Points(i))
            Next

            Return changes

        End SyncLock

    End Function
#End Region

End Class
