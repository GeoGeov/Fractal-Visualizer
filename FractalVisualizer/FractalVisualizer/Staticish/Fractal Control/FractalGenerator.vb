Imports System.Threading
Public Class FractalGenerator
#Region "Private Fields"
    Private ReadOnly Store As PointStorage
    Private Cancellation As CancellationTokenSource
    Private GenerationTask As Task

    ' Prevents multiple start/stop operations from interfering.
    'semaphoreSlim is used instead of a lock to allow async/await.
    Private ReadOnly LifecycleLock As New SemaphoreSlim(1, 1)

    Public Property CurrentPoint As Fpoint
    Private _totalGenerated As Long = 0

#End Region

#Region "Public Properties"
    'public as may be edited by the user at runtime later on.
    Public Property BatchSize As Integer = 1000
    Public Property BatchDelayMilliseconds As Decimal = 1


    Public ReadOnly Property TotalGenerated As Long
        Get
            Return Interlocked.Read(_totalGenerated)
        End Get
    End Property

    'leftover? idr why i added this or why public
    'will leave for now
    Public ReadOnly Property IsGenerating As Boolean
        Get
            Return GenerationTask IsNot Nothing AndAlso
                   Not GenerationTask.IsCompleted
        End Get
    End Property

#End Region

    Public Sub New(pointStorage As PointStorage)
        If pointStorage Is Nothing Then
            Throw New ArgumentNullException(NameOf(pointStorage))
        End If

        Store = pointStorage
    End Sub

    Public Sub UpdateViewport(viewport As ViewportBounds)
        Store.UpdateViewport(viewport)
    End Sub


#Region "Generation Methods"
    'Finite methods
    'Single point generation
    Private Sub GenerateNextPoint(batch As List(Of Fpoint))

        ' Use the existing chaos-game algorithm to select and
        ' apply the next transformation.
        Dim newPoint As Fpoint =
            ChaosGame.GeneratePoint(CurrentPoint)

        If newPoint Is Nothing Then
            Throw New InvalidOperationException(
                "The chaos-game algorithm returned Nothing."
            )
        End If

        ' Always update the current point, even if it is
        ' outside the viewport or has invalid coordinates.
        CurrentPoint = newPoint

        Interlocked.Increment(_totalGenerated)

        ' Do not store invalid coordinates.
        If Double.IsNaN(newPoint.X) OrElse
           Double.IsNaN(newPoint.Y) OrElse
           Double.IsInfinity(newPoint.X) OrElse
           Double.IsInfinity(newPoint.Y) Then

            Return
        End If

        batch.Add(newPoint)

    End Sub
    'Batch generation
    Public Async Function GenerateAsync(
    numberOfPoints As Integer,
    Optional startX As Double = 0,
    Optional startY As Double = 0
) As Task

        If numberOfPoints <= 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(numberOfPoints))
        End If

        Dim taskToRun As Task = Nothing

        Await LifecycleLock.WaitAsync()

        Try
            Await StopCurrentGenerationLockedAsync()

            ValidateSettings()
            ChaosGame.UpdateProbabilities()

            Store.Clear()

            CurrentPoint = New Fpoint(startX, startY)
            Interlocked.Exchange(_totalGenerated, 0)

            Cancellation = New CancellationTokenSource()

            Dim token As CancellationToken = Cancellation.Token

            taskToRun = Task.Run(
            Function()
                Return GenerateFiniteWorkerAsync(numberOfPoints, token)
            End Function
        )

            GenerationTask = taskToRun

        Finally
            LifecycleLock.Release()
        End Try

        ' Record any generation error without awaiting in Catch.
        Dim generationError As Exception = Nothing

        Try
            Await taskToRun

        Catch ex As Exception
            generationError = ex
        End Try

        ' Cleanup happens after the Catch/Finally blocks.
        Await LifecycleLock.WaitAsync()

        Try
            If GenerationTask Is taskToRun Then
                CleanupGeneration()
            End If

        Finally
            LifecycleLock.Release()
        End Try

        ' Re-throw the original generation error if one occurred.
        If generationError IsNot Nothing Then
            Throw generationError
        End If

    End Function
    Private Async Function GenerateFiniteWorkerAsync(
        numberOfPoints As Integer,
        token As CancellationToken
    ) As Task

        Dim batch As New List(Of Fpoint)(BatchSize)

        For i As Integer = 1 To numberOfPoints

            token.ThrowIfCancellationRequested()

            GenerateNextPoint(batch)

            If batch.Count >= BatchSize Then
                Store.AddBatch(batch)
                batch.Clear()
            End If

        Next

        ' Store any remaining points.
        If batch.Count > 0 Then
            Store.AddBatch(batch)
        End If

        Await Task.CompletedTask

    End Function



    'Continuous methods
    Public Async Function StartContinuousAsync(
        Optional startX As Double = 0,
        Optional startY As Double = 0
    ) As Task

        Await LifecycleLock.WaitAsync()

        Try
            Await StopCurrentGenerationLockedAsync()

            ValidateSettings()

            ChaosGame.UpdateProbabilities()

            Store.Clear()

            CurrentPoint = New Fpoint(startX, startY)

            Interlocked.Exchange(_totalGenerated, 0)

            Cancellation = New CancellationTokenSource()

            Dim token As CancellationToken = Cancellation.Token

            GenerationTask = Task.Run(
                Function()
                    Return GenerateContinuousWorkerAsync(token)
                End Function
            )

        Finally
            LifecycleLock.Release()
        End Try

    End Function
    Private Async Function GenerateContinuousWorkerAsync(
        token As CancellationToken
    ) As Task

        Dim batch As New List(Of Fpoint)(BatchSize)

        Try

            While Not token.IsCancellationRequested

                batch.Clear()

                For i As Integer = 1 To BatchSize

                    token.ThrowIfCancellationRequested()

                    GenerateNextPoint(batch)

                Next

                ' PointStorage checks the latest viewport when
                ' the batch is inserted.
                Store.AddBatch(batch)

                If BatchDelayMilliseconds > 0 Then
                    Await Task.Delay(
                        BatchDelayMilliseconds,
                        token
                    )
                End If

            End While

        Catch ex As OperationCanceledException
            ' Expected when generation is stopped.

        Finally
            ' Preserve any points generated in the final batch.
            If batch.Count > 0 Then
                Store.AddBatch(batch)
            End If
        End Try

    End Function

#End Region

#Region "Stop Generation"
    Public Async Function StopAsync() As Task

        Await LifecycleLock.WaitAsync()

        Try
            Await StopCurrentGenerationLockedAsync()

        Finally
            LifecycleLock.Release()
        End Try

    End Function

    Private Async Function StopCurrentGenerationLockedAsync() As Task

        Dim oldTask As Task = GenerationTask
        Dim oldCancellation As CancellationTokenSource = Cancellation

        If oldCancellation Is Nothing Then
            Return
        End If

        ' Request cancellation.
        oldCancellation.Cancel()

        Try

            If oldTask IsNot Nothing Then
                Await oldTask
            End If

        Catch ex As OperationCanceledException
            ' Expected during cancellation.

        Finally

            If GenerationTask Is oldTask Then
                CleanupGeneration()
            End If

        End Try

    End Function

    Private Sub CleanupGeneration()

        If Cancellation IsNot Nothing Then
            Cancellation.Dispose()
            Cancellation = Nothing
        End If

        GenerationTask = Nothing

    End Sub
#End Region

    Private Sub ValidateSettings()

        If Store Is Nothing Then
            Throw New InvalidOperationException(
                "PointStorage has not been initialised."
            )
        End If

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

        If ChaosGame.Transformations Is Nothing OrElse
           ChaosGame.Transformations.Count = 0 Then

            Throw New InvalidOperationException(
                "At least one transformation must be defined."
            )
        End If

    End Sub

End Class
