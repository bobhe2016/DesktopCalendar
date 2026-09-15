Imports System.Drawing
Imports System.Windows.Forms

Public Class FormFlyout
    Inherits Form

    ' 视图模式枚举：0=日视图, 1=月视图, 2=年代视图
    Private Enum CalendarViewMode
        MonthDays = 0
        YearMonths = 1
        DecadeYears = 2
    End Enum

    Private currentView As CalendarViewMode = CalendarViewMode.MonthDays

    Private displayYear As Integer
    Private displayMonth As Integer

    Protected WithEvents btnPrev As Button
    Protected WithEvents btnNext As Button
    Protected WithEvents lblTitle As Label
    Protected panelGrid As TableLayoutPanel
    Protected panelWeekHeader As Panel

    Public Sub New()
        MyBase.New()

        Me.Size = New Size(280, 320)
        Me.FormBorderStyle = FormBorderStyle.None

        Me.BackColor = Color.FromArgb(35, 35, 35)
        Me.Opacity = FormMain.CurrentOpacity
        Me.ShowInTaskbar = False
        Me.DoubleBuffered = True

        Dim now As DateTime = DateTime.Now
        displayYear = now.Year
        displayMonth = now.Month

        InitCustomLayout()
        SwitchView(CalendarViewMode.MonthDays)
    End Sub

    Private Sub InitCustomLayout()
        lblTitle = New Label()
        btnPrev = New Button()
        btnNext = New Button()
        panelGrid = New TableLayoutPanel()
        panelWeekHeader = New Panel()

        ' 年月标题（点击可切换视图）
        lblTitle.SetBounds(50, 8, 180, 25)
        lblTitle.Font = New Font("Microsoft YaHei", 10.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        lblTitle.BackColor = Color.Transparent
        lblTitle.Cursor = Cursors.Hand
        AddHandler lblTitle.Click, AddressOf OnTitleClick

        ' 上一页按钮 <
        btnPrev.SetBounds(10, 5, 35, 28)
        btnPrev.Text = "<"
        btnPrev.FlatStyle = FlatStyle.Flat
        btnPrev.FlatAppearance.BorderSize = 0
        btnPrev.ForeColor = Color.White
        btnPrev.BackColor = Color.FromArgb(50, 50, 50)
        btnPrev.Font = New Font("Microsoft YaHei", 10.0F, FontStyle.Bold)
        AddHandler btnPrev.Click, AddressOf OnPrevClick

        ' 下一页按钮 >
        btnNext.SetBounds(235, 5, 35, 28)
        btnNext.Text = ">"
        btnNext.FlatStyle = FlatStyle.Flat
        btnNext.FlatAppearance.BorderSize = 0
        btnNext.ForeColor = Color.White
        btnNext.BackColor = Color.FromArgb(50, 50, 50)
        btnNext.Font = New Font("Microsoft YaHei", 10.0F, FontStyle.Bold)
        AddHandler btnNext.Click, AddressOf OnNextClick

        Me.Controls.Add(btnPrev)
        Me.Controls.Add(lblTitle)
        Me.Controls.Add(btnNext)

        ' 星期表头容器（修改为周一开始，周末显示红色）
        panelWeekHeader.SetBounds(10, 38, 260, 20)
        panelWeekHeader.BackColor = Color.Transparent
        Dim weekDays As String() = New String() {"一", "二", "三", "四", "五", "六", "日"}
        For i As Integer = 0 To 6
            Dim lblW As New Label()
            lblW.SetBounds(i * 37, 0, 36, 20)
            lblW.Text = weekDays(i)
            lblW.TextAlign = ContentAlignment.MiddleCenter
            lblW.Font = New Font("Microsoft YaHei", 8.5F, FontStyle.Regular)
            lblW.BackColor = Color.Transparent
            ' 周六(索引5)和周日(索引6)突出显示为红褐色
            If i = 5 OrElse i = 6 Then
                lblW.ForeColor = Color.IndianRed
            Else
                lblW.ForeColor = Color.Gray
            End If
            panelWeekHeader.Controls.Add(lblW)
        Next
        Me.Controls.Add(panelWeekHeader)

        ' 日历主面板网格
        panelGrid.SetBounds(10, 60, 260, 250)
        Me.Controls.Add(panelGrid)
    End Sub

    ' 切换视图类型
    Private Sub SwitchView(ByVal newView As CalendarViewMode)
        currentView = newView
        Select Case currentView
            Case CalendarViewMode.MonthDays
                panelWeekHeader.Visible = True
                panelGrid.SetBounds(10, 60, 260, 250)
                RenderMonthDays()

            Case CalendarViewMode.YearMonths
                panelWeekHeader.Visible = False
                panelGrid.SetBounds(10, 40, 260, 270)
                RenderYearMonths()

            Case CalendarViewMode.DecadeYears
                panelWeekHeader.Visible = False
                panelGrid.SetBounds(10, 40, 260, 270)
                RenderDecadeYears()
        End Select
    End Sub

    ' 点击标题：切换视图 (日 -> 月 -> 年)
    Private Sub OnTitleClick(ByVal sender As Object, ByVal e As EventArgs)
        If currentView = CalendarViewMode.MonthDays Then
            SwitchView(CalendarViewMode.YearMonths)
        ElseIf currentView = CalendarViewMode.YearMonths Then
            SwitchView(CalendarViewMode.DecadeYears)
        End If
    End Sub

    Private Sub OnPrevClick(ByVal sender As Object, ByVal e As EventArgs)
        Select Case currentView
            Case CalendarViewMode.MonthDays
                displayMonth = displayMonth - 1
                If displayMonth < 1 Then
                    displayMonth = 12
                    displayYear = displayYear - 1
                End If
                RenderMonthDays()

            Case CalendarViewMode.YearMonths
                displayYear = displayYear - 1
                RenderYearMonths()

            Case CalendarViewMode.DecadeYears
                displayYear = displayYear - 10
                RenderDecadeYears()
        End Select
    End Sub

    Private Sub OnNextClick(ByVal sender As Object, ByVal e As EventArgs)
        Select Case currentView
            Case CalendarViewMode.MonthDays
                displayMonth = displayMonth + 1
                If displayMonth > 12 Then
                    displayMonth = 1
                    displayYear = displayYear + 1
                End If
                RenderMonthDays()

            Case CalendarViewMode.YearMonths
                displayYear = displayYear + 1
                RenderYearMonths()

            Case CalendarViewMode.DecadeYears
                displayYear = displayYear + 10
                RenderDecadeYears()
        End Select
    End Sub

    ' 1. 渲染【日视图】（改为周一开始）
    Private Sub RenderMonthDays()
        lblTitle.Text = displayYear.ToString() & "年 " & displayMonth.ToString("D2") & "月"
        panelGrid.Controls.Clear()
        panelGrid.ColumnStyles.Clear()
        panelGrid.RowStyles.Clear()

        panelGrid.ColumnCount = 7
        panelGrid.RowCount = 6
        For i As Integer = 0 To 6
            panelGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 37.0F))
        Next
        For i As Integer = 0 To 5
            panelGrid.RowStyles.Add(New RowStyle(SizeType.Absolute, 40.0F))
        Next

        Dim firstDay As New DateTime(displayYear, displayMonth, 1)

        ' 计算周一开始的起始列偏移（周一=0, 周二=1, ... 周日=6）
        Dim dayOfWeekVal As Integer = CInt(firstDay.DayOfWeek)
        Dim startWeek As Integer = (dayOfWeekVal + 6) Mod 7

        Dim totalDays As Integer = DateTime.DaysInMonth(displayYear, displayMonth)
        Dim today As DateTime = DateTime.Now

        Dim dayCounter As Integer = 1

        For r As Integer = 0 To 5
            For c As Integer = 0 To 6
                If (r = 0 AndAlso c < startWeek) OrElse dayCounter > totalDays Then
                    panelGrid.Controls.Add(New Label(), c, r)
                Else
                    Dim dtCurrent As New DateTime(displayYear, displayMonth, dayCounter)

                    Dim box As New Panel()
                    box.Margin = New Padding(1)
                    box.Dock = DockStyle.Fill

                    If dtCurrent.Date = today.Date Then
                        box.BackColor = Color.FromArgb(217, 83, 79)
                    Else
                        box.BackColor = Color.FromArgb(45, 45, 45)
                    End If

                    ' 公历数字
                    Dim lblSolar As New Label()
                    lblSolar.Text = dayCounter.ToString()
                    lblSolar.SetBounds(0, 2, 35, 18)
                    lblSolar.TextAlign = ContentAlignment.MiddleCenter
                    lblSolar.Font = New Font("Microsoft YaHei", 8.5F, FontStyle.Bold)
                    lblSolar.ForeColor = Color.White
                    lblSolar.BackColor = Color.Transparent

                    ' 农历文本（初一显示月份，非初一显示“廿六”等）
                    Dim lblLunar As New Label()
                    lblLunar.Text = FormMain.GetLunarShortString(dtCurrent)
                    lblLunar.SetBounds(0, 20, 35, 16)
                    lblLunar.TextAlign = ContentAlignment.MiddleCenter
                    lblLunar.Font = New Font("Microsoft YaHei", 7.0F, FontStyle.Regular)
                    lblLunar.ForeColor = Color.Gainsboro
                    lblLunar.BackColor = Color.Transparent

                    box.Controls.Add(lblSolar)
                    box.Controls.Add(lblLunar)
                    panelGrid.Controls.Add(box, c, r)

                    dayCounter = dayCounter + 1
                End If
            Next
            If dayCounter > totalDays Then Exit For
        Next
    End Sub

    ' 2. 渲染【月视图】
    Private Sub RenderYearMonths()
        lblTitle.Text = displayYear.ToString() & "年"
        panelGrid.Controls.Clear()
        panelGrid.ColumnStyles.Clear()
        panelGrid.RowStyles.Clear()

        panelGrid.ColumnCount = 4
        panelGrid.RowCount = 3
        For i As Integer = 0 To 3
            panelGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        Next
        For i As Integer = 0 To 2
            panelGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 33.33F))
        Next

        For m As Integer = 1 To 12
            Dim btnM As New Button()
            btnM.Text = m.ToString() & "月"
            btnM.Dock = DockStyle.Fill
            btnM.Margin = New Padding(3)
            btnM.FlatStyle = FlatStyle.Flat
            btnM.FlatAppearance.BorderSize = 0
            btnM.Font = New Font("Microsoft YaHei", 10.0F, FontStyle.Bold)
            btnM.ForeColor = Color.White
            btnM.Tag = m

            If displayYear = DateTime.Now.Year AndAlso m = DateTime.Now.Month Then
                btnM.BackColor = Color.FromArgb(217, 83, 79)
            Else
                btnM.BackColor = Color.FromArgb(50, 50, 50)
            End If

            AddHandler btnM.Click, AddressOf OnMonthSelectClick
            panelGrid.Controls.Add(btnM, (m - 1) Mod 4, (m - 1) \ 4)
        Next
    End Sub

    Private Sub OnMonthSelectClick(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As Button = TryCast(sender, Button)
        If btn IsNot Nothing Then
            displayMonth = CInt(btn.Tag)
            SwitchView(CalendarViewMode.MonthDays)
        End If
    End Sub

    ' 3. 渲染【年代视图】
    Private Sub RenderDecadeYears()
        Dim startYear As Integer = (displayYear \ 10) * 10
        Dim endYear As Integer = startYear + 9
        lblTitle.Text = startYear.ToString() & " - " & endYear.ToString()

        panelGrid.Controls.Clear()
        panelGrid.ColumnStyles.Clear()
        panelGrid.RowStyles.Clear()

        panelGrid.ColumnCount = 4
        panelGrid.RowCount = 3
        For i As Integer = 0 To 3
            panelGrid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        Next
        For i As Integer = 0 To 2
            panelGrid.RowStyles.Add(New RowStyle(SizeType.Percent, 33.33F))
        Next

        Dim yr As Integer = startYear - 1
        For idx As Integer = 0 To 11
            Dim btnY As New Button()
            btnY.Text = yr.ToString()
            btnY.Dock = DockStyle.Fill
            btnY.Margin = New Padding(3)
            btnY.FlatStyle = FlatStyle.Flat
            btnY.FlatAppearance.BorderSize = 0
            btnY.Font = New Font("Microsoft YaHei", 9.5F, FontStyle.Bold)
            btnY.Tag = yr

            If yr < startYear OrElse yr > endYear Then
                btnY.ForeColor = Color.Gray
                btnY.BackColor = Color.FromArgb(40, 40, 40)
            ElseIf yr = DateTime.Now.Year Then
                btnY.ForeColor = Color.White
                btnY.BackColor = Color.FromArgb(217, 83, 79)
            Else
                btnY.ForeColor = Color.White
                btnY.BackColor = Color.FromArgb(50, 50, 50)
            End If

            AddHandler btnY.Click, AddressOf OnYearSelectClick
            panelGrid.Controls.Add(btnY, idx Mod 4, idx \ 4)
            yr = yr + 1
        Next
    End Sub

    Private Sub OnYearSelectClick(ByVal sender As Object, ByVal e As EventArgs)
        Dim btn As Button = TryCast(sender, Button)
        If btn IsNot Nothing Then
            displayYear = CInt(btn.Tag)
            SwitchView(CalendarViewMode.YearMonths)
        End If
    End Sub

    Protected Overrides Sub OnDeactivate(ByVal e As EventArgs)
        MyBase.OnDeactivate(e)
        Me.Close()
    End Sub
End Class