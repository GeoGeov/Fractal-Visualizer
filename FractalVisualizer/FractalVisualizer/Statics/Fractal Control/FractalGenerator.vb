
Imports System.Threading
Imports System.Threading.Tasks

Public Class FractalGenerator

    Private ReadOnly Store As PointStorage

    Private Cancellation As CancellationTokenSource
    Private GenerationTask As Task

    Private ReadOnly LifecycleLock As New Object()

    Private CurrentPoint As Fpoint
    Private HasCurrentPoint As Boolean = False

    Private GeneratedCount As Long = 0

    Public Property BatchSize As Integer = 1000
    Public Property BatchDelayMilliseconds As Integer = 1

    Public Sub New(pointStorage As PointStorage)

        If pointStorage Is Nothing Then
            Throw New ArgumentNullException(NameOf(pointStorage))
        End If

        Store = pointStorage

    End Sub

    Public ReadOnly Property IsRunning As Boolean
        Get
            SyncLock LifecycleLock

                Return GenerationTask IsNot Nothing AndAlso
                       Not GenerationTask.IsCompleted

            End SyncLock
        End Get
    End Property

    Public ReadOnly Property TotalGenerated As Long
        Get
            SyncLock LifecycleLock
                Return GeneratedCount
            End SyncLock
        End Get
    End Property

    ' =========================================================
    ' NORMAL GENERATION
    ' =========================================================

    Public Async Function GenerateAsync(
        numberOfPoints As Integer,
        Optional startX As Double = 0,
        Optional startY As Double = 0,
        Optional t As Double = 0
    ) As Task

        Await StopAsync()

        ValidateSettings()
        ValidateTransformations()

        If numberOfPoints <= 0 Then
            Throw New ArgumentOutOfRangeException(
                NameOf(numberOfPoints)
            )
        End If

        ChaosGame.UpdateProbabilities()

        Store.Clear()

        SyncLock LifecycleLock

            CurrentPoint = New Fpoint(startX, startY)
            HasCurrentPoint = True
            GeneratedCount = 0

        End SyncLock

        Dim source As CancellationTokenSource = Nothing

        Dim task As Task = StartWorker(
            Function(token)
                Return GenerateLoopAsync(
                    numberOfPoints,
                    Nothing,
                    0,
                    token
                )
            End Function,
            source
        )

        Try
            Await task
        Finally
            CleanupTask(task, source)
        End Try

    End Function

    ' =========================================================
    ' VIEWPORT GENERATION
    ' =========================================================

    Public Async Function GenerateViewportAsync(
        targetVisiblePoints As Integer,
        viewport As ViewportBounds
    ) As Task

        ValidateSettings()
        ValidateTransformations()
        ValidateViewport(viewport)

        If targetVisiblePoints <= 0 Then Return

        ' Stop the previous generation request.
        ' The last completed batch remains saved.
        Await StopAsync()

        ChaosGame.UpdateProbabilities()

        EnsureCurrentPoint()

        Dim visibleCount As Integer =
            CountPointsInViewport(viewport)

        If visibleCount >= targetVisiblePoints Then Return

        Dim source As CancellationTokenSource = Nothing

        Dim task As Task = StartWorker(
            Function(token)
                Return GenerateLoopAsync(
                    Long.MaxValue,
                    viewport,
                    targetVisiblePoints,
                    token
                )
            End Function,
            source
        )

        Try
            Await task
        Finally
            CleanupTask(task, source)
        End Try

    End Function

    Private Function CountPointsInViewport(
        viewport As ViewportBounds
    ) As Integer

        Dim points As Fpoint() = Store.GetSnapshot()

        Dim count As Integer = 0

        For Each point As Fpoint In points

            If viewport.Contains(point) Then
                count += 1
            End If

        Next

        Return count

    End Function

    Private Sub EnsureCurrentPoint()

        SyncLock LifecycleLock

            If Not HasCurrentPoint Then

                CurrentPoint = New Fpoint(0, 0)
                HasCurrentPoint = True
                GeneratedCount = 0

            End If

        End SyncLock

    End Sub

    ' =========================================================
    ' CONTINUOUS GENERATION
    ' =========================================================

    Public Async Function StartContinuousAsync(
        Optional startX As Double = 0,
        Optional startY As Double = 0,
        Optional t As Double = 0
    ) As Task

        Await StopAsync()

        ValidateSettings()
        ValidateTransformations()

        ChaosGame.UpdateProbabilities()

        EnsureCurrentPoint()

        Dim source As CancellationTokenSource = Nothing

        ' Deliberately do not Await this task.
        ' Continuous generation runs in the background.
        Dim runningTask As Task = StartWorker(
            Function(token)
                Return GenerateLoopAsync(
                    Long.MaxValue,
                    Nothing,
                    0,
                    token
                )
            End Function,
            source
        )

    End Function

    ' =========================================================
    ' GENERATION LOOP
    ' =========================================================

    Private Async Function GenerateLoopAsync(
        numberOfAttempts As Long,
        viewport As Nullable(Of ViewportBounds),
        targetVisiblePoints As Integer,
        token As CancellationToken
    ) As Task

        Dim attempts As Long = 0

        Dim visibleCount As Integer = 0

        If viewport.HasValue Then
            visibleCount =
                CountPointsInViewport(viewport.Value)
        End If

        Do While attempts < numberOfAttempts

            token.ThrowIfCancellationRequested()

            If viewport.HasValue AndAlso
               visibleCount >= targetVisiblePoints Then

                Exit Do

            End If

            Dim batch As New List(Of Fpoint)

            ' Work with a local copy. Only commit it after
            ' the batch has been successfully completed.
            Dim batchCurrent As Fpoint = CurrentPoint

            Dim batchAttempts As Integer =
                CInt(Math.Min(
                    CLng(BatchSize),
                    numberOfAttempts - attempts
                ))

            For i As Integer = 1 To batchAttempts

                token.ThrowIfCancellationRequested()

                If viewport.HasValue AndAlso
                   visibleCount >= targetVisiblePoints Then

                    Exit For

                End If

                Dim nextPoint As Fpoint =
                    ChaosGame.GeneratePoint(batchCurrent)

                If nextPoint Is Nothing Then

                    Throw New InvalidOperationException(
                        "ChaosGame generated a Nothing point."
                    )

                End If

                If Double.IsNaN(nextPoint.X) OrElse
                   Double.IsNaN(nextPoint.Y) OrElse
                   Double.IsInfinity(nextPoint.X) OrElse
                   Double.IsInfinity(nextPoint.Y) Then

                    Throw New InvalidOperationException(
                        "ChaosGame generated an invalid point."
                    )

                End If

                ' Advance the sequence regardless of whether
                ' the point will be stored.
                batchCurrent = nextPoint

                If Not viewport.HasValue OrElse
                   viewport.Value.Contains(nextPoint) Then

                    batch.Add(nextPoint)

                    If viewport.HasValue Then
                        visibleCount += 1
                    End If

                End If

            Next

            ' Commit the completed batch.
            CurrentPoint = batchCurrent

            attempts += batchAttempts

            SyncLock LifecycleLock
                GeneratedCount += batchAttempts
            End SyncLock

            If batch.Count > 0 Then
                Store.AddBatch(batch)
            End If

            If BatchDelayMilliseconds > 0 Then

                Await Task.Delay(
                    BatchDelayMilliseconds,
                    token
                )

            Else

                Await Task.Yield()

            End If

        Loop

    End Function

    ' =========================================================
    ' WORKER MANAGEMENT
    ' =========================================================

    Private Function StartWorker(
        worker As Func(Of CancellationToken, Task),
        ByRef source As CancellationTokenSource
    ) As Task

        source = New CancellationTokenSource()

        Dim localSource As CancellationTokenSource = source
        Dim token As CancellationToken = localSource.Token

        Dim task As Task = Task.Run(
            Function()
                Return worker(token)
            End Function,
            token
        )

        SyncLock LifecycleLock

            Cancellation = localSource
            GenerationTask = task

        End SyncLock

        ' Clean up when a background task finishes, including
        ' continuous generation which is not awaited by its caller.
        task.ContinueWith(
            Sub(completedTask)
                CleanupTask(completedTask, localSource)
            End Sub,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        )

        Return task

    End Function

    Public Async Function StopAsync() As Task

        Dim currentSource As CancellationTokenSource
        Dim currentTask As Task

        SyncLock LifecycleLock

            currentSource = Cancellation
            currentTask = GenerationTask

        End SyncLock

        If currentSource Is Nothing OrElse
           currentTask Is Nothing Then

            Return

        End If

        currentSource.Cancel()

        Try

            Await currentTask

        Catch ex As OperationCanceledException

            ' Expected when generation is stopped.

        Finally

            CleanupTask(currentTask, currentSource)

        End Try

    End Function

    Private Sub CleanupTask(
        task As Task,
        source As CancellationTokenSource
    )

        SyncLock LifecycleLock

            If ReferenceEquals(GenerationTask, task) Then

                GenerationTask = Nothing
                Cancellation = Nothing

                If source IsNot Nothing Then
                    source.Dispose()
                End If

            End If

        End SyncLock

    End Sub

    ' =========================================================
    ' VALIDATION
    ' =========================================================

    Private Sub ValidateSettings()

        If BatchSize <= 0 Then

            Throw New InvalidOperationException(
                "BatchSize must be greater than zero."
            )

        End If

        If BatchDelayMilliseconds < 0 Then

            Throw New InvalidOperationException(
                "BatchDelayMilliseconds cannot be negative."
            )

        End If

    End Sub

    Private Sub ValidateTransformations()

        If ChaosGame.Transformations Is Nothing OrElse
           ChaosGame.Transformations.Count = 0 Then

            Throw New InvalidOperationException(
                "At least one transformation is required."
            )

        End If

    End Sub

    Private Sub ValidateViewport(viewport As ViewportBounds)

        If Double.IsNaN(viewport.Left) OrElse
           Double.IsNaN(viewport.Right) OrElse
           Double.IsNaN(viewport.Top) OrElse
           Double.IsNaN(viewport.Bottom) OrElse
           Double.IsInfinity(viewport.Left) OrElse
           Double.IsInfinity(viewport.Right) OrElse
           Double.IsInfinity(viewport.Top) OrElse
           Double.IsInfinity(viewport.Bottom) Then

            Throw New ArgumentException(
                "Viewport bounds must be finite."
            )

        End If

        If viewport.Left >= viewport.Right OrElse
           viewport.Bottom >= viewport.Top Then

            Throw New ArgumentException(
                "Viewport bounds are invalid."
            )

        End If

    End Sub

End Class
