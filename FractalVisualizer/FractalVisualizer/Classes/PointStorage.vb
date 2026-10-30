Public Class PointStorage
    'Class to store points in a thread-safe manner, with a maximum limit on the number of points stored.

    Private ReadOnly Points As New List(Of Fpoint)
    Private ReadOnly PointsLock As New Object()
    Public TotalAdded As Long = 0

    'the maximum number of points to store, can be adjusted based on performance needs
    'should be adjustable from ui
    Public Property MaxPoints As Integer = 500000

    'return the number of points currently stored
    Public ReadOnly Property Count As Integer
        Get
            'synchronize access to the Points list to ensure thread safety
            SyncLock PointsLock
                Return Points.Count
            End SyncLock
        End Get
    End Property


    'methods

    Public Sub AddBatch(newPoints As IEnumerable(Of Fpoint))
        SyncLock PointsLock
            For Each p As Fpoint In newPoints
                'ensure that the point is valid before adding it to the list
                If p Is Nothing Then
                    Continue For
                End If
                If Double.IsNaN(p.X) OrElse Double.IsNaN(p.Y) OrElse
                    Double.IsInfinity(p.X) OrElse Double.IsInfinity(p.Y) Then
                    Continue For
                End If

                Points.Add(p)
                TotalAdded += 1
            Next

            'prolly shouldn't allow MaxPoints to be less than 1, but just in case
            If MaxPoints < 1 Then MaxPoints = 1

            'remove oldest points if we exceed the maximum allowed points
            'this is ideal as it allows for a rolling buffer of points, keeping the most recent ones
            If Points.Count > MaxPoints Then
                Dim excess As Integer = Points.Count - MaxPoints
                Points.RemoveRange(0, excess)
            End If
        End SyncLock
    End Sub

    Public Function GetSnapshot() As Fpoint()
        'return a snapshot of the current points in storage
        SyncLock PointsLock
            Return Points.ToArray()
        End SyncLock
    End Function

    Public Function GetChanges(ByRef lastSeenTotal As Long, ByRef needsRebuild As Boolean) As Fpoint()
        'return the changes in points since the last seen total, and indicate if a rebuild is needed
        SyncLock PointsLock
            Dim firstRetainedIndex As Long = TotalAdded - Points.Count

            If lastSeenTotal < firstRetainedIndex OrElse
               lastSeenTotal > TotalAdded Then
                needsRebuild = True
                lastSeenTotal = TotalAdded
                'if the last seen total is outside the range of retained points, we need to rebuild the view
                Return Points.ToArray()
            End If

            needsRebuild = False

            Dim startIndex As Integer = CInt(lastSeenTotal - firstRetainedIndex)
            Dim numberOfChanges As Integer = Points.Count - startIndex

            'if there are no new points since the last seen total, return an empty array
            If numberOfChanges <= 0 Then
                Return Array.Empty(Of Fpoint)()
            End If

            'create an array to hold the new points
            Dim result(numberOfChanges - 1) As Fpoint

            'copy the new points to the result array
            Points.CopyTo(startIndex, result, 0, numberOfChanges)

            lastSeenTotal = TotalAdded

            'return the new points since the last seen total
            Return result
        End SyncLock
    End Function

    Public Sub Clear()
        'clear all points from storage and reset the total added count
        'should be called when the fractal is regenerated or when a new pattern is requested
        SyncLock PointsLock
            Points.Clear()
            TotalAdded = 0
        End SyncLock
    End Sub
End Class

