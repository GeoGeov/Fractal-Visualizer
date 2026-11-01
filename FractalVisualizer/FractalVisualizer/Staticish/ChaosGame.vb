Module ChaosGame

    Public Transformations As New List(Of Transformation)

    Private ReadOnly RNG As New Random()

    Private TotalProbability As Double

    Public Sub UpdateProbabilities()
        'may require error handling if the sum of probabilities is 0 or NaN or negative,
        'but that should be handled by future me

        TotalProbability = Transformations.Sum(Function(T) T.Probability)

    End Sub

    Public Function GeneratePoint(StartingPoint As Fpoint) As Fpoint

        Dim RandomValue As Double = RNG.NextDouble() * TotalProbability
        Dim CumulativeProbability As Double = 0

        For Each T As Transformation In Transformations
            CumulativeProbability += T.Probability

            If RandomValue < CumulativeProbability Then
                Return T.ApplyTransformation(StartingPoint)
            End If
        Next

        Return StartingPoint

    End Function


    Public Function GeneratePoints(StartingPoint As Fpoint, NumberOfIterations As Integer) As List(Of Fpoint)
        'leftover from old architecture
        'left incase needed

        Dim Points As New List(Of Fpoint)
        Dim CurrentPoint As Fpoint = StartingPoint

        For i As Integer = 1 To NumberOfIterations
            CurrentPoint = GeneratePoint(CurrentPoint)
            CurrentPoint.Iteration = i
            Points.Add(CurrentPoint)
        Next

        Return Points

    End Function
End Module