Imports System.Globalization
Public Class TransformationEditor
    Inherits UserControl

    Private ReadOnly FunctionGrid As New DataGridView()
    Private ReadOnly AddButton As New Button()
    Private ReadOnly RemoveButton As New Button()
    Private ReadOnly PresetButton As New Button()
    Private ReadOnly HeadingLabel As New Label()

    Public Sub New()
        BuildInterface()
        LoadSierpinskiPreset()

    End Sub

    Private Sub BuildInterface()

        Dock = DockStyle.Fill
        BackColor = Color.White
        Padding = New Padding(10)

        HeadingLabel.Text = "Transformations"
        HeadingLabel.Font = New Font("Segoe UI", 12, FontStyle.Bold)
        HeadingLabel.Dock = DockStyle.Top
        HeadingLabel.Height = 35

        FunctionGrid.Dock = DockStyle.Fill
        FunctionGrid.AllowUserToAddRows = False
        FunctionGrid.AllowUserToDeleteRows = False
        FunctionGrid.AllowUserToResizeRows = False
        FunctionGrid.RowHeadersVisible = False
        FunctionGrid.MultiSelect = False
        FunctionGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect
        FunctionGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill
        FunctionGrid.BackgroundColor = Color.White
        FunctionGrid.BorderStyle = BorderStyle.FixedSingle

        FunctionGrid.Columns.Add(
            New DataGridViewTextBoxColumn With {
                .Name = "Index",
                .HeaderText = "#",
                .ReadOnly = True,
                .FillWeight = 15
            })

        FunctionGrid.Columns.Add(
            New DataGridViewTextBoxColumn With {
                .Name = "Expression",
                .HeaderText = "Function (X $ Y)",
                .FillWeight = 100
            })

        FunctionGrid.Columns.Add(
            New DataGridViewTextBoxColumn With {
                .Name = "Probability",
                .HeaderText = "Probability",
                .FillWeight = 45
            })

        AddButton.Text = "+ Add"
        RemoveButton.Text = "- Remove"
        PresetButton.Text = "Sierpiński preset"

        AddButton.Dock = DockStyle.Fill
        RemoveButton.Dock = DockStyle.Fill
        PresetButton.Dock = DockStyle.Fill

        AddHandler AddButton.Click, AddressOf AddRow
        AddHandler RemoveButton.Click, AddressOf RemoveRow
        AddHandler PresetButton.Click,
            AddressOf PresetButton_Click

        Dim buttons As New TableLayoutPanel With {
            .Dock = DockStyle.Bottom,
            .Height = 38,
            .ColumnCount = 3,
            .RowCount = 1,
            .Padding = New Padding(0, 5, 0, 0)
        }

        buttons.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 25))
        buttons.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 25))
        buttons.ColumnStyles.Add(
            New ColumnStyle(SizeType.Percent, 50))

        buttons.Controls.Add(AddButton, 0, 0)
        buttons.Controls.Add(RemoveButton, 1, 0)
        buttons.Controls.Add(PresetButton, 2, 0)

        Controls.Add(FunctionGrid)
        Controls.Add(buttons)
        Controls.Add(HeadingLabel)

    End Sub

    Private Sub AddRow(sender As Object, e As EventArgs)
        FunctionGrid.Rows.Add(FunctionGrid.Rows.Count, "x $ y", "1")
    End Sub

    Private Sub RemoveRow(sender As Object, e As EventArgs)
        If FunctionGrid.SelectedRows.Count = 0 Then
            MessageBox.Show(
                "Select a transformation row first.")
            Return
        End If

        FunctionGrid.Rows.Remove(FunctionGrid.SelectedRows(0))
        UpdateIndices()
    End Sub

    Private Sub UpdateIndices()
        For i As Integer = 0 To FunctionGrid.Rows.Count - 1
            FunctionGrid.Rows(i).Cells("Index").Value = i
        Next
    End Sub

    Private Sub PresetButton_Click(sender As Object, e As EventArgs)
        LoadSierpinskiPreset()
    End Sub

    Public Sub LoadSierpinskiPreset()
        FunctionGrid.Rows.Clear()
        FunctionGrid.Rows.Add(0, "0.5 * x + 0.5 $ 0.5 * y", "1")
        FunctionGrid.Rows.Add(1, "0.5 * x - 0.5 $ 0.5 * y", "1")
        FunctionGrid.Rows.Add(2, "0.5 * x $ 0.5 * y + 0.5", "1")
    End Sub

    Public Sub ApplyTransformations()

        If FunctionGrid.IsCurrentCellInEditMode Then
            FunctionGrid.EndEdit()
        End If

        If FunctionGrid.Rows.Count = 0 Then
            Throw New InvalidOperationException(
                "Add at least one transformation.")
        End If

        Dim newTransformations As New List(Of Transformation)

        For i As Integer = 0 To FunctionGrid.Rows.Count - 1

            Dim expression As String =
                Convert.ToString(
                    FunctionGrid.Rows(i).Cells("Expression").Value)

            Dim probabilityText As String =
                Convert.ToString(
                    FunctionGrid.Rows(i).Cells("Probability").Value)

            If String.IsNullOrWhiteSpace(expression) Then
                Throw New InvalidOperationException(
                    "Transformation " & i.ToString() &
                    " has an empty expression.")
            End If

            Dim probability As Double

            If Not Double.TryParse(
                probabilityText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                probability) Then

                If Not Double.TryParse(
                    probabilityText,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    probability) Then

                    Throw New InvalidOperationException(
                        "Invalid probability in row " & i.ToString() & ".")
                End If

            End If

            If Double.IsNaN(probability) OrElse
               Double.IsInfinity(probability) OrElse
               probability < 0 Then

                Throw New InvalidOperationException(
                    "Probabilities must be finite and non-negative.")
            End If

            newTransformations.Add(
                New Transformation(expression, probability, i))

        Next

        If Not newTransformations.Exists(
            Function(t) t.Probability > 0) Then

            Throw New InvalidOperationException(
                "At least one probability must be greater than zero.")
        End If

        ChaosGame.Transformations.Clear()
        ChaosGame.Transformations.AddRange(newTransformations)
        ChaosGame.UpdateProbabilities()

    End Sub

End Class
