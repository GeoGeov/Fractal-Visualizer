Imports System.Drawing
Imports System.Windows.Forms

Public Class Form1

    '===========================================================
    ' DATA
    '===========================================================

    Private Points As List(Of Fpoint)

    '===========================================================
    ' CONTROLS
    '===========================================================

    Private WithEvents GenerateButton As New Button()
    Private WithEvents ResetButton As New Button()

    Private IterationsBox As New NumericUpDown()

    Private FractalPicture As New PictureBox()

    Private StatusLabel As New Label()

    '===========================================================
    ' FORM
    '===========================================================

    Public Sub New()

        InitializeComponent()

        SetupForm()
        SetupControls()

    End Sub

    '===========================================================
    ' FORM SETUP
    '===========================================================

    Private Sub SetupForm()

        Me.Text = "Chaos Game Fractal Visualizer"

        Me.Size = New Size(1000, 700)

        Me.MinimumSize = New Size(700, 500)

        Me.StartPosition =
            FormStartPosition.CenterScreen

        Me.BackColor = Color.FromArgb(30, 30, 30)

    End Sub

    '===========================================================
    ' CONTROL SETUP
    '===========================================================

    Private Sub SetupControls()

        '-------------------------------------------------------
        ' Generate button
        '-------------------------------------------------------

        GenerateButton.Text = "Generate"

        GenerateButton.Size =
            New Size(100, 35)

        GenerateButton.Location =
            New Point(20, 20)

        GenerateButton.BackColor =
            Color.FromArgb(60, 60, 60)

        GenerateButton.ForeColor =
            Color.White

        GenerateButton.FlatStyle =
            FlatStyle.Flat

        Me.Controls.Add(GenerateButton)


        '-------------------------------------------------------
        ' Reset button
        '-------------------------------------------------------

        ResetButton.Text = "Reset"

        ResetButton.Size =
            New Size(100, 35)

        ResetButton.Location =
            New Point(130, 20)

        ResetButton.BackColor =
            Color.FromArgb(60, 60, 60)

        ResetButton.ForeColor =
            Color.White

        ResetButton.FlatStyle =
            FlatStyle.Flat

        Me.Controls.Add(ResetButton)


        '-------------------------------------------------------
        ' Iterations
        '-------------------------------------------------------

        Dim IterationsLabel As New Label()

        IterationsLabel.Text = "Iterations:"

        IterationsLabel.ForeColor =
            Color.White

        IterationsLabel.AutoSize = True

        IterationsLabel.Location =
            New Point(260, 28)

        Me.Controls.Add(IterationsLabel)


        IterationsBox.Minimum = 1

        IterationsBox.Maximum = 10000000

        IterationsBox.Value = 100000

        IterationsBox.Increment = 10000

        IterationsBox.Size =
            New Size(120, 25)

        IterationsBox.Location =
            New Point(330, 24)

        Me.Controls.Add(IterationsBox)


        '-------------------------------------------------------
        ' Status
        '-------------------------------------------------------

        StatusLabel.Text =
            "Ready"

        StatusLabel.ForeColor =
            Color.LightGray

        StatusLabel.AutoSize = True

        StatusLabel.Location =
            New Point(470, 28)

        Me.Controls.Add(StatusLabel)


        '-------------------------------------------------------
        ' PictureBox
        '-------------------------------------------------------

        FractalPicture.BackColor =
            Color.Black

        FractalPicture.BorderStyle =
            BorderStyle.FixedSingle

        FractalPicture.SizeMode =
            PictureBoxSizeMode.Normal

        FractalPicture.Location =
            New Point(20, 70)

        FractalPicture.Anchor =
            AnchorStyles.Top Or
            AnchorStyles.Bottom Or
            AnchorStyles.Left Or
            AnchorStyles.Right

        Me.Controls.Add(FractalPicture)

        ResizePictureBox()



        'Temp Transformations added to definition
        ChaosGame.Transformations.Add(
    New Transformation(
        "0.5*x - 0.5, 0.5*y",
        1,
        0
    )
)

        ChaosGame.Transformations.Add(
    New Transformation(
        "0.5*x + 0.5, 0.5*y",
        1,
        1
    )
)

        ChaosGame.Transformations.Add(
    New Transformation(
        "0.5*x, 0.5*y + 0.5",
        1,
        2
    )
)

        ChaosGame.UpdateProbabilities()

    End Sub

    '===========================================================
    ' RESIZE PICTURE BOX
    '===========================================================

    Private Sub ResizePictureBox()

        FractalPicture.Size =
            New Size(
                Me.ClientSize.Width - 40,
                Me.ClientSize.Height - 90
            )

    End Sub

    Private Sub Form1_Resize(
        sender As Object,
        e As EventArgs
    ) Handles MyBase.Resize

        ResizePictureBox()

    End Sub

    '===========================================================
    ' GENERATE
    '===========================================================

    Private Sub GenerateButton_Click(
        sender As Object,
        e As EventArgs
    ) Handles GenerateButton.Click

        If ChaosGame.Transformations.Count = 0 Then

            MessageBox.Show(
                "No transformations have been defined.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            )

            Return

        End If

        StatusLabel.Text = "Generating..."

        Application.DoEvents()

        'Make sure probability total is updated
        ChaosGame.UpdateProbabilities()

        Dim NumberOfIterations As Integer =
            CInt(IterationsBox.Value)

        Dim StartPoint As New Fpoint(0, 0)

        Dim StartTime As DateTime =
            DateTime.Now

        Points =
            ChaosGame.GeneratePoints(
                StartPoint,
                NumberOfIterations
            )

        Dim GenerationTime As TimeSpan =
            DateTime.Now - StartTime

        StatusLabel.Text =
            "Generated " &
            Points.Count.ToString("N0") &
            " points in " &
            GenerationTime.TotalSeconds.ToString("0.00") &
            "s"

        RenderFractal()

    End Sub

    '===========================================================
    ' RENDER
    '===========================================================

    Private Sub RenderFractal()

        If Points Is Nothing Then
            Return
        End If

        If FractalPicture.Width <= 0 OrElse
           FractalPicture.Height <= 0 Then

            Return

        End If

        StatusLabel.Text &= " | Rendering..."

        Application.DoEvents()

        Dim StartTime As DateTime =
            DateTime.Now

        Dim NewBitmap As Bitmap =
            FractalRenderer.Render(
                Points,
                FractalPicture.Width,
                FractalPicture.Height
            )

        Dim OldBitmap As Image =
            FractalPicture.Image

        FractalPicture.Image =
            NewBitmap

        If OldBitmap IsNot Nothing Then
            OldBitmap.Dispose()
        End If

        Dim RenderTime As TimeSpan =
            DateTime.Now - StartTime

        StatusLabel.Text =
            StatusLabel.Text.Replace(
                " | Rendering...",
                ""
            ) &
            " | Rendered in " &
            RenderTime.TotalSeconds.ToString("0.00") &
            "s"

    End Sub

    '===========================================================
    ' RESET
    '===========================================================

    Private Sub ResetButton_Click(
        sender As Object,
        e As EventArgs
    ) Handles ResetButton.Click

        Points = Nothing

        FractalRenderer.Camera.X = 0
        FractalRenderer.Camera.Y = 0
        FractalRenderer.Camera.Zoom = 1

        Dim OldBitmap As Image =
            FractalPicture.Image

        FractalPicture.Image = Nothing

        If OldBitmap IsNot Nothing Then
            OldBitmap.Dispose()
        End If

        StatusLabel.Text = "Ready"

    End Sub

    '===========================================================
    ' FORM CLOSING
    '===========================================================

    Protected Overrides Sub OnFormClosed(
        e As FormClosedEventArgs
    )

        If FractalPicture.Image IsNot Nothing Then

            FractalPicture.Image.Dispose()

            FractalPicture.Image = Nothing

        End If

        MyBase.OnFormClosed(e)

    End Sub

End Class