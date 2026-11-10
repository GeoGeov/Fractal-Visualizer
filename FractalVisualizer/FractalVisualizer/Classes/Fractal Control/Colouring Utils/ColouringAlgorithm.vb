Public MustInherit Class ColouringAlgorithm
    'abstract class that will be inherited by all colouring algos
    Public MustOverride Function GetColour(point As Fpoint) As Color

    Public Overridable ReadOnly Property Name As String
        Get
            Return "Unknown"
        End Get
    End Property

End Class
