Public Module TransformationPresets
    'was used previously
    'want to repurpose this for presets

    Public Sub Load()
        ChaosGame.Transformations.Clear()
        'Loads the Sierpinski triangle preset
        ChaosGame.Transformations.Add(New Transformation("0.5 * x + 0.5 $ 0.5 * y", 1, 0))
        ChaosGame.Transformations.Add(New Transformation("0.5 * x - 0.5 $ 0.5 * y", 1, 1))
        ChaosGame.Transformations.Add(New Transformation("0.5 * x $ 0.5 * y + 0.5", 1, 2))
        ChaosGame.UpdateProbabilities()
    End Sub

End Module