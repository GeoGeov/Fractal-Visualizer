Public Class ColourLayer
    'acts as layers in other art programs
    'will allow for multiple colouring algorithms to be applied to the same fractal, and then blended together
    Public Property Algorithm As ColouringAlgorithm

    Property Strength As Double = 1.0
    Property BlendMode As BlendMode

    Property Enabled As Boolean = True
End Class
