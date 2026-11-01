Public Class Form1

    Private Controller As FractalController

    Private MainSplit As SplitContainer
    Private SettingsLayout As TableLayoutPanel
    Private SettingsTop As Panel

    Private GenerateButton As Button
    Private StopButton As Button
    Private ResetButton As Button

    Private XBox As NumericUpDown
    Private YBox As NumericUpDown
    Private ZoomBox As NumericUpDown

    Private PointsGeneratedLabel As Label
    Private MaxPointsLabel As Label
    Private PointsOnScreenLabel As Label
    Private StatusLabel As Label

    Private TransformationEditorView As TransformationEditor
    Private FractalView As FractalDisplay

    Private StatisticsTimer As Windows.Forms.Timer
    Private UpdatingCameraControls As Boolean = False
    Private IsClosing As Boolean = False

    Public Sub New()

        InitializeComponent()

        AddHandler Me.Resize, AddressOf Form1_Resize

        ' The UI is built entirely in code.
        Me.Controls.Clear()

        Me.Text = "Chaos Game Fractal Visualizer"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.WindowState = FormWindowState.Maximized
        Me.MinimumSize = New Size(800, 500)
        Me.AutoScaleMode = AutoScaleMode.Font

        Controller = New FractalController()

        BuildInterface()

        FractalView.AttachController(Controller)

        FractalView.TargetVisiblePoints = 500000

        AddHandler FractalView.CameraChanged,
            AddressOf FractalView_CameraChanged

        AddHandler FractalView.StatisticsUpdated,
            AddressOf UpdatePointStatistics

        AddHandler XBox.ValueChanged, AddressOf Camera_Changed
        AddHandler YBox.ValueChanged, AddressOf Camera_Changed
        AddHandler ZoomBox.ValueChanged, AddressOf Camera_Changed

        AddHandler GenerateButton.Click, AddressOf GenerateButton_Click
        AddHandler StopButton.Click, AddressOf StopButton_Click
        AddHandler ResetButton.Click, AddressOf ResetButton_Click

        StatisticsTimer = New Windows.Forms.Timer With {
            .Interval = 500
        }

        AddHandler StatisticsTimer.Tick,
            AddressOf UpdatePointStatistics

        StatisticsTimer.Start()

        AddHandler Me.FormClosing,
            AddressOf Form1_FormClosing

        SyncCameraControls()
        UpdatePointStatistics(Nothing, EventArgs.Empty)

    End Sub

    ' =========================================================
    ' Main interface
    ' =========================================================

    Private Sub BuildInterface()

        MainSplit = New SplitContainer With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .FixedPanel = FixedPanel.Panel1,
            .SplitterWidth = 5,
            .Panel1MinSize = 250,
            .Panel2MinSize = 100,
            .Margin = Padding.Empty,
            .TabStop = False,
            .SplitterDistance = 300
        }

        Me.Controls.Add(MainSplit)
        MainSplit.BringToFront()

        ' Left panel layout.
        SettingsLayout = New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 2,
            .Padding = New Padding(8),
            .Margin = Padding.Empty
        }

        SettingsLayout.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100.0F)
        )

        SettingsLayout.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 270.0F)
        )

        SettingsLayout.RowStyles.Add(
            New RowStyle(SizeType.Percent, 100.0F)
        )

        MainSplit.Panel1.Controls.Add(SettingsLayout)

        ' Fixed-height settings at the top.
        SettingsTop = New Panel With {
            .Dock = DockStyle.Fill,
            .Margin = Padding.Empty
        }

        SettingsLayout.Controls.Add(SettingsTop, 0, 0)

        BuildTopControls()

        ' Transformation editor expands vertically.
        TransformationEditorView = New TransformationEditor With {
            .Dock = DockStyle.Fill,
            .Margin = Padding.Empty,
            .MinimumSize = New Size(0, 0)
        }

        SettingsLayout.Controls.Add(
            TransformationEditorView, 0, 1
        )

        ' Right panel: fractal display fills all available space.
        FractalView = New FractalDisplay With {
            .Dock = DockStyle.Fill,
            .Margin = Padding.Empty,
            .MinimumSize = New Size(0, 0)
        }

        MainSplit.Panel2.Controls.Add(FractalView)

    End Sub

    ' =========================================================
    ' Top controls
    ' =========================================================

    Private Sub BuildTopControls()

        Dim layout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4,
            .Margin = Padding.Empty,
            .Padding = Padding.Empty
        }

        layout.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100.0F)
        )

        layout.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 40.0F)
        )

        layout.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 120.0F)
        )

        layout.RowStyles.Add(
            New RowStyle(SizeType.Absolute, 90.0F)
        )

        layout.RowStyles.Add(
            New RowStyle(SizeType.Percent, 100.0F)
        )

        SettingsTop.Controls.Add(layout)

        ' Generation buttons.
        Dim buttonPanel As New FlowLayoutPanel With {
            .Dock = DockStyle.Fill,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Margin = Padding.Empty,
            .Padding = Padding.Empty
        }

        GenerateButton = New Button With {
            .Text = "Generate",
            .Size = New Size(85, 30),
            .Margin = New Padding(0, 2, 5, 2)
        }

        StopButton = New Button With {
            .Text = "Stop",
            .Size = New Size(55, 30),
            .Margin = New Padding(0, 2, 5, 2)
        }

        ResetButton = New Button With {
            .Text = "Reset",
            .Size = New Size(55, 30),
            .Margin = New Padding(0, 2, 0, 2)
        }

        buttonPanel.Controls.Add(GenerateButton)
        buttonPanel.Controls.Add(StopButton)
        buttonPanel.Controls.Add(ResetButton)

        layout.Controls.Add(buttonPanel, 0, 0)

        ' Camera controls.
        Dim cameraLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 3,
            .Margin = New Padding(0, 4, 0, 4)
        }

        cameraLayout.ColumnStyles.Add(
            New ColumnStyle(SizeType.Absolute, 65.0F)
        )

        cameraLayout.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100.0F)
        )

        For i As Integer = 0 To 2
            cameraLayout.RowStyles.Add(
                New RowStyle(SizeType.Percent, 33.3333F)
            )
        Next

        XBox = CreateNumberBox(
            -100000D, 100000D, 0D, 0.1D, 3
        )

        YBox = CreateNumberBox(
            -100000D, 100000D, 0D, 0.1D, 3
        )

        ZoomBox = CreateNumberBox(
            0.001D, 1000000D, 1D, 0.1D, 3
        )

        AddCameraRow(cameraLayout, "Camera X", XBox, 0)
        AddCameraRow(cameraLayout, "Camera Y", YBox, 1)
        AddCameraRow(cameraLayout, "Zoom", ZoomBox, 2)

        layout.Controls.Add(cameraLayout, 0, 1)

        ' Statistics.
        Dim stats As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .Margin = New Padding(0, 4, 0, 4)
        }

        stats.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 100.0F)
        )

        For i As Integer = 0 To 2
            stats.RowStyles.Add(
                New RowStyle(SizeType.Percent, 33.3333F)
            )
        Next

        PointsGeneratedLabel = CreateLabel("Points generated: 0")
        MaxPointsLabel = CreateLabel("Maximum stored: 0")
        PointsOnScreenLabel = CreateLabel("Points on screen: 0")

        stats.Controls.Add(PointsGeneratedLabel, 0, 0)
        stats.Controls.Add(MaxPointsLabel, 0, 1)
        stats.Controls.Add(PointsOnScreenLabel, 0, 2)

        layout.Controls.Add(stats, 0, 2)

        StatusLabel = CreateLabel("Ready")
        layout.Controls.Add(StatusLabel, 0, 3)

    End Sub

    Private Function CreateNumberBox(
        minimum As Decimal,
        maximum As Decimal,
        initialValue As Decimal,
        increment As Decimal,
        decimalPlaces As Integer
    ) As NumericUpDown

        Return New NumericUpDown With {
            .Minimum = minimum,
            .Maximum = maximum,
            .Value = initialValue,
            .Increment = increment,
            .DecimalPlaces = decimalPlaces,
            .Dock = DockStyle.Fill,
            .TextAlign = HorizontalAlignment.Right,
            .Margin = New Padding(3)
        }

    End Function

    Private Function CreateLabel(text As String) As Label

        Return New Label With {
            .Text = text,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .AutoEllipsis = True,
            .Margin = Padding.Empty
        }

    End Function

    Private Sub AddCameraRow(
        layout As TableLayoutPanel,
        caption As String,
        box As NumericUpDown,
        row As Integer
    )

        Dim lbl As New Label With {
            .Text = caption,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Margin = Padding.Empty
        }

        layout.Controls.Add(lbl, 0, row)
        layout.Controls.Add(box, 1, row)

    End Sub

    ' =========================================================
    ' Camera synchronisation
    ' =========================================================

    Private Sub Camera_Changed(
    sender As Object,
    e As EventArgs
)

        If UpdatingCameraControls OrElse
       Controller Is Nothing OrElse
       IsClosing Then Return

        Controller.Camera.X = CDbl(XBox.Value)
        Controller.Camera.Y = CDbl(YBox.Value)
        Controller.Camera.Zoom = CDbl(ZoomBox.Value)

        Controller.InvalidateRenderer()
        FractalView.RefreshDisplay()

        ' Request detail for the new camera position.
        FractalView.GenerateCurrentViewportDetail()

    End Sub

    Private Sub FractalView_CameraChanged(
        sender As Object,
        e As EventArgs
    )

        SyncCameraControls()

    End Sub

    Private Sub SyncCameraControls()

        If Controller Is Nothing Then Return

        UpdatingCameraControls = True

        Try
            XBox.Value = ClampDecimal(
                CDec(Controller.Camera.X),
                XBox.Minimum,
                XBox.Maximum
            )

            YBox.Value = ClampDecimal(
                CDec(Controller.Camera.Y),
                YBox.Minimum,
                YBox.Maximum
            )

            ZoomBox.Value = ClampDecimal(
                CDec(Controller.Camera.Zoom),
                ZoomBox.Minimum,
                ZoomBox.Maximum
            )

        Finally
            UpdatingCameraControls = False
        End Try

    End Sub

    Private Function ClampDecimal(
        value As Decimal,
        minimum As Decimal,
        maximum As Decimal
    ) As Decimal

        If value < minimum Then Return minimum
        If value > maximum Then Return maximum

        Return value

    End Function

    ' =========================================================
    ' Generation controls
    ' =========================================================

    Private Async Sub GenerateButton_Click(
    sender As Object,
    e As EventArgs
)

        TransformationEditorView.ApplyTransformations()

        FractalView.UpdateGenerationViewport()

        GenerateButton.Enabled = False
        StopButton.Enabled = True

        Try

            Await Controller.StartContinuousAsync()

            StatusLabel.Text = "Generating..."

        Catch ex As Exception

            StatusLabel.Text = "Generation failed: " & ex.Message

            GenerateButton.Enabled = True
            StopButton.Enabled = False

        End Try

    End Sub
    Private Async Sub StopButton_Click(
    sender As Object,
    e As EventArgs
)

        StopButton.Enabled = False
        StatusLabel.Text = "Stopping generation..."

        Try
            Await Controller.StopGenerationAsync()

        Catch ex As Exception
            MessageBox.Show(
            "Error stopping generation: " & ex.Message
        )

        Finally
            GenerateButton.Enabled = True
            StopButton.Enabled = False
            StatusLabel.Text = "Generation stopped"
        End Try

    End Sub
    Private Async Sub ResetButton_Click(
        sender As Object,
        e As EventArgs
    )

        If IsClosing Then Return

        Try
            Await Controller.ResetAsync()

            SyncCameraControls()
            FractalView.RefreshDisplay()

            StatusLabel.Text = "Reset complete"

        Catch ex As Exception
            MessageBox.Show(
                ex.Message,
                "Reset error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            )
        End Try

        UpdatePointStatistics(Nothing, EventArgs.Empty)

    End Sub

    ' =========================================================
    ' Statistics
    ' =========================================================

    Private Sub UpdatePointStatistics(
        sender As Object,
        e As EventArgs
    )

        If IsClosing OrElse Controller Is Nothing Then Return

        PointsGeneratedLabel.Text =
    "Points generated: " &
    Controller.Generator.TotalGenerated.ToString("N0")

        MaxPointsLabel.Text =
            "Maximum stored: " &
            Controller.Store.MaxPoints.ToString("N0")

        PointsOnScreenLabel.Text =
            "Points on screen: " &
            FractalView.GetVisiblePointCount().ToString("N0")

    End Sub

    ' =========================================================
    ' Cleanup
    ' =========================================================

    Private Async Sub Form1_FormClosing(
        sender As Object,
        e As FormClosingEventArgs
    )

        If IsClosing Then Return

        IsClosing = True

        If StatisticsTimer IsNot Nothing Then
            StatisticsTimer.Stop()
            StatisticsTimer.Dispose()
            StatisticsTimer = Nothing
        End If

        Try
            If Controller IsNot Nothing Then
                Await Controller.StopGenerationAsync()
                Controller.Dispose()
                Controller = Nothing
            End If
        Catch
            ' Allow the form to close if shutdown fails.
        End Try

    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs)

        If FractalView Is Nothing OrElse Controller Is Nothing Then Return

        Controller.InvalidateRenderer()
        FractalView.RefreshDisplay()

    End Sub
End Class
