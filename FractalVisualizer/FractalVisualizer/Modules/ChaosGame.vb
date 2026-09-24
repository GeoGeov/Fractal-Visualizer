Module ChaosGame
    Public Transformations As New List(Of Transformation)

    Public Function generatePoint(StartingPoint As Fpoint) As Fpoint
        'init
        Dim RandomValue As Double = Rnd()
        Dim CumulativeProbability As Double = 0
        Dim SelectedTransform As Transformation = Nothing
        Dim totalProbability As Double = Transformations.Sum(Function(t) t.Probability)

        'randomly select a transformation based on their probabilities
        'Turns relative probabilities into a val between 0 and 1
        'Using the cumulative probability method to select a transformations 
        For Each T As Transformation In Transformations
            CumulativeProbability += T.Probability / totalProbability
            If RandomValue <= CumulativeProbability Then
                SelectedTransform = T
                Exit For
            End If
        Next

        ' Apply the selected transformation to the starting point
        If SelectedTransform IsNot Nothing Then
            Return SelectedTransform.ApplyTransformation(StartingPoint)
        Else
            ' If no transformation was selected, return the starting point unchanged
            Return StartingPoint
        End If
    End Function
End Module
