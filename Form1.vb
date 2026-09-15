Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms


Public Class FormMain
    Inherits Form


    ' --- Win32 API 用于读写 INI 文件 ---
    <DllImport("kernel32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function GetPrivateProfileString(ByVal section As String, ByVal key As String, ByVal def As String, ByVal retVal As StringBuilder, ByVal size As Integer, ByVal filePath As String) As Integer
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function WritePrivateProfileString(ByVal section As String, ByVal key As String, ByVal val As String, ByVal filePath As String) As Long
    End Function

    ' --- Win32 API 用于获取托盘图标位置 ---
    <StructLayout(LayoutKind.Sequential)>
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure NOTIFYICONIDENTIFIER
        Public cbSize As Integer
        Public hWnd As IntPtr
        Public uID As Integer
        Public guidItem As Guid
    End Structure

    <DllImport("shell32.dll", SetLastError:=True)>
    Private Shared Function Shell_NotifyIconGetRect(ByRef identifier As NOTIFYICONIDENTIFIER, ByRef iconLocation As RECT) As Integer
    End Function

    Protected WithEvents lblHeader As Label
    Protected WithEvents lblDay As Label
    Protected WithEvents lblWeek As Label
    Protected WithEvents lblTime As Label
    Protected WithEvents lblLunar As Label
    Protected WithEvents timerUpdate As Timer

    ' 主工具菜单 & 托盘右键菜单
    Protected WithEvents contextMenuMain As ContextMenuStrip
    Protected WithEvents contextMenuTray As ContextMenuStrip
    Protected WithEvents chkTopMost As ToolStripMenuItem
    Protected WithEvents chkAutoStart As ToolStripMenuItem
    Protected WithEvents notifyIconMain As NotifyIcon

    Private Shared ReadOnly lunarCal As New ChineseLunisolarCalendar()
    Private Shared ReadOnly randomObj As New Random()

    ' INI 配置文件路径及任务名称
    Private ReadOnly iniFilePath As String = Path.Combine(Application.StartupPath, "config.ini")
    Private Const TASK_NAME As String = "MyDesktopCalendar_AutoStart"

    ' 跨日检测：保存上一次刷新的日期
    Private lastDate As DateTime = DateTime.MinValue

    ' 颜色控制
    Private currentFormBgColor As Color = Color.FromArgb(30, 30, 30)
    Private currentHeaderColor As Color
    Private currentTextColor As Color
    Private currentLunarBgColor As Color

    Private ReadOnly headerColors As Color() = New Color() {
        Color.FromArgb(229, 57, 53), Color.FromArgb(30, 136, 229), Color.FromArgb(67, 160, 71),
        Color.FromArgb(251, 140, 0), Color.FromArgb(142, 36, 170), Color.FromArgb(0, 172, 193)
    }

    Private ReadOnly textColorList As Color() = New Color() {
        Color.FromArgb(255, 255, 255), Color.FromArgb(0, 229, 255), Color.FromArgb(255, 215, 0),
        Color.FromArgb(105, 240, 174), Color.FromArgb(255, 110, 64), Color.FromArgb(238, 130, 238)
    }

    Private ReadOnly lunarBgColorList As Color() = New Color() {
        Color.FromArgb(20, 20, 20), Color.FromArgb(60, 20, 20), Color.FromArgb(20, 40, 60),
        Color.FromArgb(20, 50, 35), Color.FromArgb(50, 25, 60), Color.FromArgb(60, 40, 20)
    }

    Private flyoutInstance As FormFlyout = Nothing
    Public Shared CurrentOpacity As Double = 0.9

    Public Sub New()
        MyBase.New()

        Me.Size = New Size(130, 165)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.ShowInTaskbar = False
        Me.DoubleBuffered = True
        Me.BackColor = currentFormBgColor

        InitializeCustomControls()
        InitNotifyIcon()
        InitContextMenu()

        ' 初始化主题与设置
        ApplyRandomTheme(False)
        LoadSettingsFromIni()

        ' 首次强制更新日历
        UpdateCalendar(True)

        ' 定时器每秒检测与更新
        timerUpdate = New Timer()
        timerUpdate.Interval = 1000
        timerUpdate.Start()
    End Sub

    Private Sub InitializeCustomControls()
        lblHeader = New Label()
        lblDay = New Label()
        lblWeek = New Label()
        lblTime = New Label()
        lblLunar = New Label()

        ' 1. Header (高度 26)
        lblHeader.Dock = DockStyle.Top
        lblHeader.Height = 26
        lblHeader.Font = New Font("Microsoft YaHei", 9.0F, FontStyle.Bold)
        lblHeader.ForeColor = Color.White
        lblHeader.TextAlign = ContentAlignment.MiddleCenter

        ' 2. 日期数字 (Y: 26, 高度: 56)
        lblDay.Size = New Size(130, 56)
        lblDay.Location = New Point(0, 26)
        lblDay.Font = New Font("Microsoft YaHei", 38.0F, FontStyle.Bold)
        lblDay.TextAlign = ContentAlignment.MiddleCenter
        lblDay.BackColor = Color.Transparent

        ' 3. 星期 (Y: 80, 高度: 18)
        lblWeek.Size = New Size(130, 18)
        lblWeek.Location = New Point(0, 80)
        lblWeek.Font = New Font("Microsoft YaHei", 8.5F, FontStyle.Regular)
        lblWeek.TextAlign = ContentAlignment.MiddleCenter
        lblWeek.BackColor = Color.Transparent

        ' 4. 时间 (Y: 98, 高度: 30, 字体: 15.5pt)
        lblTime.Size = New Size(130, 30)
        lblTime.Location = New Point(0, 98)
        lblTime.Font = New Font("Consolas", 15.5F, FontStyle.Bold)
        lblTime.ForeColor = Color.White
        lblTime.TextAlign = ContentAlignment.MiddleCenter
        lblTime.BackColor = Color.Transparent

        ' 5. 农历 (高度 26)
        lblLunar.Dock = DockStyle.Bottom
        lblLunar.Height = 26
        lblLunar.Font = New Font("Microsoft YaHei", 8.5F, FontStyle.Bold)
        lblLunar.ForeColor = Color.White
        lblLunar.TextAlign = ContentAlignment.MiddleCenter

        Me.Controls.Add(lblDay)
        Me.Controls.Add(lblWeek)
        Me.Controls.Add(lblTime)
        Me.Controls.Add(lblHeader)
        Me.Controls.Add(lblLunar)

        ' 双击展开/隐藏长日历
        'AddHandler Me.DoubleClick, AddressOf OnGadgetDoubleClick
        'AddHandler lblHeader.DoubleClick, AddressOf OnGadgetDoubleClick
        'AddHandler lblDay.DoubleClick, AddressOf OnGadgetDoubleClick
        'AddHandler lblWeek.DoubleClick, AddressOf OnGadgetDoubleClick
        'AddHandler lblTime.DoubleClick, AddressOf OnGadgetDoubleClick
        'AddHandler lblLunar.DoubleClick, AddressOf OnGadgetDoubleClick
        ' 双击展开/隐藏长日历（中间区域、农历与空白处保持双击展开长日历）
        AddHandler Me.DoubleClick, AddressOf OnGadgetDoubleClick
        AddHandler lblDay.DoubleClick, AddressOf OnGadgetDoubleClick
        AddHandler lblWeek.DoubleClick, AddressOf OnGadgetDoubleClick
        AddHandler lblTime.DoubleClick, AddressOf OnGadgetDoubleClick
        AddHandler lblLunar.DoubleClick, AddressOf OnGadgetDoubleClick

        ' 【新增/修改】：双击顶部 Header 直接最小化到托盘
        AddHandler lblHeader.DoubleClick, AddressOf OnMinimizeToTrayClick
        ' 拖拽支持
        AddHandler Me.MouseDown, AddressOf OnDragMouseDown
        ' AddHandler lblHeader.MouseDown, AddressOf OnDragMouseDown
        ' 绑定 Header 的双击事件（使用 MouseDoubleClick 响应更敏捷）
        AddHandler lblHeader.MouseDoubleClick, AddressOf OnHeaderMouseDoubleClick


        ' 2. lblHeader 绑定 MouseDown，内部同时接管拖拽和双击判定
        AddHandler lblHeader.MouseDown, AddressOf OnHeaderMouseDown
    End Sub
    ' 专用于 Header 的双击事件，响应极速且稳定
    Private Sub OnHeaderMouseDoubleClick(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            OnMinimizeToTrayClick(sender, e)
        End If
    End Sub
    ' 专门处理 Header 的鼠标按下事件（完美兼顾单/双击与拖拽）
    Private Sub OnHeaderMouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ' 判读如果鼠标是双击（Clicks = 2），则触发缩小到托盘
            If e.Clicks = 2 Then
                OnMinimizeToTrayClick(sender, e)
            Else
                ' 如果是单击或按住拖拽，则触发 Win32 窗口拖拽
                ReleaseCapture()
                SendMessage(Me.Handle, &HA1, 2, 0)
                SaveSettingsToIni()
            End If
        End If
    End Sub
    ' 初始化系统托盘图标
    Private Sub InitNotifyIcon()
        notifyIconMain = New NotifyIcon()

        ' 从项目资源（My.Resources）中直接读取嵌入的图标
        notifyIconMain.Icon = My.Resources.Appred

        notifyIconMain.Text = "桌面万年历工具"
        notifyIconMain.Visible = True

        ' 绑定托盘图标的单击与双击事件
        AddHandler notifyIconMain.MouseClick, AddressOf OnNotifyIconMouseClick
        AddHandler notifyIconMain.DoubleClick, AddressOf OnNotifyIconDoubleClick
    End Sub

    Private Sub InitContextMenu()
        ' --- 主界面右键菜单 ---
        contextMenuMain = New ContextMenuStrip()

        Dim opacityMenu As New ToolStripMenuItem("透明度设置")
        Dim op100 As New ToolStripMenuItem("100% (不透明)", Nothing, AddressOf SetOpacityHandler) : op100.Tag = 1.0
        Dim op90 As New ToolStripMenuItem("90%", Nothing, AddressOf SetOpacityHandler) : op90.Tag = 0.9
        Dim op80 As New ToolStripMenuItem("80%", Nothing, AddressOf SetOpacityHandler) : op80.Tag = 0.8
        Dim op70 As New ToolStripMenuItem("70%", Nothing, AddressOf SetOpacityHandler) : op70.Tag = 0.7
        Dim op50 As New ToolStripMenuItem("50% (半透明)", Nothing, AddressOf SetOpacityHandler) : op50.Tag = 0.5
        opacityMenu.DropDownItems.AddRange(New ToolStripItem() {op100, op90, op80, op70, op50})

        chkTopMost = New ToolStripMenuItem("总在最前", Nothing, AddressOf OnTopMostToggleClick) With {.CheckOnClick = True}
        chkAutoStart = New ToolStripMenuItem("开机启动", Nothing, AddressOf OnAutoStartToggleClick) With {.CheckOnClick = True}

        ' 功能 1：最小化到托盘
        Dim minimizeToTrayMenu As New ToolStripMenuItem("最小化到托盘", Nothing, AddressOf OnMinimizeToTrayClick)
        Dim randomThemeMenu As New ToolStripMenuItem("随机色彩主题", Nothing, AddressOf OnRefreshThemeClick)
        Dim aboutMenu As New ToolStripMenuItem("关于", Nothing, AddressOf OnAboutClick)
        Dim exitMenu As New ToolStripMenuItem("退出", Nothing, AddressOf OnExitClick)

        contextMenuMain.Items.Add(opacityMenu)
        contextMenuMain.Items.Add(chkTopMost)
        contextMenuMain.Items.Add(chkAutoStart)
        contextMenuMain.Items.Add(minimizeToTrayMenu)
        contextMenuMain.Items.Add(randomThemeMenu)
        contextMenuMain.Items.Add(New ToolStripSeparator())
        contextMenuMain.Items.Add(aboutMenu)
        contextMenuMain.Items.Add(exitMenu)

        Me.ContextMenuStrip = contextMenuMain
        lblHeader.ContextMenuStrip = contextMenuMain
        lblDay.ContextMenuStrip = contextMenuMain
        lblWeek.ContextMenuStrip = contextMenuMain
        lblTime.ContextMenuStrip = contextMenuMain
        lblLunar.ContextMenuStrip = contextMenuMain

        ' --- 托盘图标右键菜单 ---
        contextMenuTray = New ContextMenuStrip()
        Dim showMainItem As New ToolStripMenuItem("显示主界面", Nothing, AddressOf OnShowMainFromTrayClick)
        Dim trayAboutItem As New ToolStripMenuItem("关于", Nothing, AddressOf OnAboutClick)
        Dim trayExitItem As New ToolStripMenuItem("退出", Nothing, AddressOf OnExitClick)

        contextMenuTray.Items.Add(showMainItem)
        contextMenuTray.Items.Add(trayAboutItem)
        contextMenuTray.Items.Add(New ToolStripSeparator())
        contextMenuTray.Items.Add(trayExitItem)

        notifyIconMain.ContextMenuStrip = contextMenuTray
    End Sub

    ' 最小化到托盘
    Private Sub OnMinimizeToTrayClick(ByVal sender As Object, ByVal e As EventArgs)
        Me.Hide()
        If flyoutInstance IsNot Nothing AndAlso Not flyoutInstance.IsDisposed Then
            flyoutInstance.Hide()
        End If
    End Sub

    ' 从托盘恢复显示主界面
    Private Sub OnShowMainFromTrayClick(ByVal sender As Object, ByVal e As EventArgs)
        RestoreAndShowMain()
    End Sub

    Private Sub RestoreAndShowMain()
        Me.Show()
        Me.WindowState = FormWindowState.Normal
        Me.Activate()
    End Sub

    ' 功能 2：单击托盘图标显示/隐藏长日历（含位置检测）
    Private Sub OnNotifyIconMouseClick(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ' 如果长日历已经打开且可见，则收起
            If flyoutInstance IsNot Nothing AndAlso Not flyoutInstance.IsDisposed AndAlso flyoutInstance.Visible Then
                flyoutInstance.Close()
                Return
            End If

            ShowFlyoutNearTray()
        End If
    End Sub

    ' 功能 3：双击托盘图标在桌面显示主工具
    Private Sub OnNotifyIconDoubleClick(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            RestoreAndShowMain()
        End If
    End Sub

    ' 智能计算托盘位置并弹出完整长日历
    Private Sub ShowFlyoutNearTray()
        If flyoutInstance Is Nothing OrElse flyoutInstance.IsDisposed Then
            flyoutInstance = New FormFlyout()
        End If

        Dim trayRect As RECT = GetTrayIconPosition()
        Dim screenArea As Rectangle = Screen.PrimaryScreen.WorkingArea

        ' 默认尝试放置在托盘图标上方
        Dim targetX As Integer = trayRect.Left - (flyoutInstance.Width / 2)
        Dim targetY As Integer = trayRect.Top - flyoutInstance.Height - 5

        ' 1. 横向边界校正：防止超出左边或右边
        If targetX < screenArea.Left Then targetX = screenArea.Left + 5
        If targetX + flyoutInstance.Width > screenArea.Right Then targetX = screenArea.Right - flyoutInstance.Width - 5

        ' 2. 纵向边界校正：如果上方空间不够显示完整日历，就弹到图标下方
        If targetY < screenArea.Top Then
            targetY = trayRect.Bottom + 5
        End If

        flyoutInstance.StartPosition = FormStartPosition.Manual
        flyoutInstance.Location = New Point(targetX, targetY)
        flyoutInstance.Opacity = CurrentOpacity
        flyoutInstance.TopMost = True
        flyoutInstance.Show()
        flyoutInstance.Activate()
    End Sub

    ' 获取托盘图标位置 API
    Private Function GetTrayIconPosition() As RECT
        Dim rect As New RECT()
        Try
            Dim nid As New NOTIFYICONIDENTIFIER()
            nid.cbSize = Marshal.SizeOf(nid)
            nid.hWnd = Me.Handle
            nid.uID = 1

            Dim res As Integer = Shell_NotifyIconGetRect(nid, rect)
            If res <> 0 OrElse rect.Right = 0 Then
                ' 获取失败时退回到系统任务栏右下角区域估算
                Dim wa As Rectangle = Screen.PrimaryScreen.WorkingArea
                rect.Left = wa.Right - 40
                rect.Top = wa.Bottom - 40
                rect.Right = wa.Right
                rect.Bottom = wa.Bottom
            End If
        Catch
            Dim wa As Rectangle = Screen.PrimaryScreen.WorkingArea
            rect.Left = wa.Right - 40
            rect.Top = wa.Bottom - 40
        End Try
        Return rect
    End Function

    Private Sub OnTopMostToggleClick(ByVal sender As Object, ByVal e As EventArgs)
        Me.TopMost = chkTopMost.Checked
        If flyoutInstance IsNot Nothing AndAlso Not flyoutInstance.IsDisposed Then
            flyoutInstance.TopMost = Me.TopMost
        End If
        SaveSettingsToIni()
    End Sub

    Private Sub OnAutoStartToggleClick(ByVal sender As Object, ByVal e As EventArgs)
        SetTaskSchedulerAutoStart(chkAutoStart.Checked)
        SaveSettingsToIni()
    End Sub

    Private Sub OnAboutClick(ByVal sender As Object, ByVal e As EventArgs)
        ' MessageBox.Show("程序设计：童星食品@资讯部-何祖溪 2026年8月", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Using frmAbout As New FormAbout()
            frmAbout.ShowDialog(Me) ' 以模态对话框形式弹出
        End Using
    End Sub

    Private Sub SetOpacityHandler(ByVal sender As Object, ByVal e As EventArgs)
        Dim item As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)
        If item IsNot Nothing AndAlso item.Tag IsNot Nothing Then
            CurrentOpacity = Convert.ToDouble(item.Tag)
            Me.Opacity = CurrentOpacity
            If flyoutInstance IsNot Nothing AndAlso Not flyoutInstance.IsDisposed Then
                flyoutInstance.Opacity = CurrentOpacity
            End If
            SaveSettingsToIni()
        End If
    End Sub

    Private Sub OnRefreshThemeClick(ByVal sender As Object, ByVal e As EventArgs)
        ApplyRandomTheme(True)
    End Sub

    Private Sub OnExitClick(ByVal sender As Object, ByVal e As EventArgs)
        SaveSettingsToIni()
        notifyIconMain.Visible = False
        Application.Exit()
    End Sub

    Private Sub ApplyRandomTheme(ByVal saveIni As Boolean)
        currentFormBgColor = Color.FromArgb(30, 30, 30)
        Me.BackColor = currentFormBgColor

        Dim idxHeader As Integer = randomObj.Next(0, headerColors.Length)
        currentHeaderColor = headerColors(idxHeader)
        lblHeader.BackColor = currentHeaderColor

        Dim idxText As Integer = randomObj.Next(0, textColorList.Length)
        currentTextColor = textColorList(idxText)
        lblDay.ForeColor = currentTextColor
        lblWeek.ForeColor = Color.FromArgb(200, currentTextColor.R, currentTextColor.G, currentTextColor.B)

        Dim idxLunarBg As Integer = randomObj.Next(0, lunarBgColorList.Length)
        currentLunarBgColor = lunarBgColorList(idxLunarBg)
        lblLunar.BackColor = currentLunarBgColor

        If saveIni Then SaveSettingsToIni()
    End Sub

    ' 功能 4：时钟更新与跨日无缝同步处理
    Private Sub UpdateCalendar(Optional ByVal forceAll As Boolean = False)
        Dim dt As DateTime = DateTime.Now

        ' 每秒固定刷新跳动的时间
        lblTime.Text = dt.ToString("HH:mm:ss")

        ' 跨日检测：当日期改变或首次运行 (forceAll) 时更新年月日与农历
        If forceAll OrElse dt.Date <> lastDate Then
            lastDate = dt.Date
            lblHeader.Text = dt.ToString("yyyy年MM月")
            lblDay.Text = dt.ToString("dd")
            lblWeek.Text = dt.ToString("dddd")
            lblLunar.Text = "农历 " & GetLunarFullString(dt)

            ' 如果长日历当前处于打开状态，同步刷新长日历内容
            If flyoutInstance IsNot Nothing AndAlso Not flyoutInstance.IsDisposed AndAlso flyoutInstance.Visible Then
                flyoutInstance.Invalidate()
            End If
        End If
    End Sub

    Public Shared Function GetLunarFullString(ByVal dt As DateTime) As String
        Dim months As String() = New String() {"", "正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "冬", "腊"}
        Dim days As String() = New String() {"", "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
                                           "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
                                           "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"}

        Dim year As Integer = lunarCal.GetYear(dt)
        Dim month As Integer = lunarCal.GetMonth(dt)
        Dim day As Integer = lunarCal.GetDayOfMonth(dt)
        Dim leapMonth As Integer = lunarCal.GetLeapMonth(year)

        Dim isLeap As Boolean = False
        If leapMonth > 0 Then
            If month = leapMonth Then
                isLeap = True
                month = month - 1
            ElseIf month > leapMonth Then
                month = month - 1
            End If
        End If

        Dim prefix As String = ""
        If isLeap Then prefix = "闰"

        Return prefix & months(month) & "月" & days(day)
    End Function

    Public Shared Function GetLunarShortString(ByVal dt As DateTime) As String
        Dim months As String() = New String() {"", "正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "冬", "腊"}
        Dim days As String() = New String() {"", "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
                                           "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
                                           "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"}

        Dim year As Integer = lunarCal.GetYear(dt)
        Dim month As Integer = lunarCal.GetMonth(dt)
        Dim day As Integer = lunarCal.GetDayOfMonth(dt)
        Dim leapMonth As Integer = lunarCal.GetLeapMonth(year)

        Dim isLeap As Boolean = False
        If leapMonth > 0 Then
            If month = leapMonth Then
                isLeap = True
                month = month - 1
            ElseIf month > leapMonth Then
                month = month - 1
            End If
        End If

        If day = 1 Then
            Dim prefix As String = ""
            If isLeap Then prefix = "闰"
            Return prefix & months(month) & "月"
        Else
            Return days(day)
        End If
    End Function

    Private Sub OnGadgetDoubleClick(ByVal sender As Object, ByVal e As EventArgs)
        Dim mea As MouseEventArgs = TryCast(e, MouseEventArgs)
        If mea IsNot Nothing AndAlso mea.Button <> MouseButtons.Left Then Return

        If flyoutInstance Is Nothing OrElse flyoutInstance.IsDisposed Then
            flyoutInstance = New FormFlyout()

            Dim targetX As Integer = Me.Left - flyoutInstance.Width
            Dim targetY As Integer = Me.Top

            If targetX < Screen.PrimaryScreen.WorkingArea.Left Then
                targetX = Me.Right
            End If

            flyoutInstance.StartPosition = FormStartPosition.Manual
            flyoutInstance.Location = New Point(targetX, targetY)
            flyoutInstance.Opacity = CurrentOpacity
            flyoutInstance.TopMost = Me.TopMost
            flyoutInstance.Show()
        Else
            flyoutInstance.Close()
        End If
    End Sub

    Private Declare Sub ReleaseCapture Lib "user32.dll" ()
    Private Declare Sub SendMessage Lib "user32.dll" Alias "SendMessageA" (ByVal hwnd As IntPtr, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As Integer)

    Private Sub OnDragMouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ReleaseCapture()
            SendMessage(Me.Handle, &HA1, 2, 0)
            SaveSettingsToIni()
        End If
    End Sub

    Private Sub timerUpdate_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles timerUpdate.Tick
        UpdateCalendar(False)
    End Sub

    ' 开机自启逻辑
    Private Sub SetTaskSchedulerAutoStart(ByVal enable As Boolean)
        Try
            Dim exePath As String = Application.ExecutablePath
            Dim psi As New ProcessStartInfo()
            psi.FileName = "schtasks.exe"
            psi.CreateNoWindow = True
            psi.UseShellExecute = False

            If enable Then
                psi.Arguments = String.Format("/Create /TN ""{0}"" /TR """"{1}"""" /SC ONLOGON /F", TASK_NAME, exePath)
            Else
                psi.Arguments = String.Format("/Delete /TN ""{0}"" /F", TASK_NAME)
            End If

            Dim p As Process = Process.Start(psi)
            p.WaitForExit()
        Catch ex As Exception
            MessageBox.Show("配置开机自启失败: " & ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' INI 读写
    Public Sub SaveSettingsToIni()
        Try
            WriteIniVal("Settings", "TopMost", If(Me.TopMost, "1", "0"))
            WriteIniVal("Settings", "AutoStart", If(chkAutoStart.Checked, "1", "0"))
            WriteIniVal("Settings", "LocationX", Me.Location.X.ToString())
            WriteIniVal("Settings", "LocationY", Me.Location.Y.ToString())
            WriteIniVal("Settings", "Opacity", CurrentOpacity.ToString(CultureInfo.InvariantCulture))

            WriteIniVal("Colors", "FormBgColor", String.Format("{0},{1},{2}", currentFormBgColor.R, currentFormBgColor.G, currentFormBgColor.B))
            WriteIniVal("Colors", "HeaderColor", String.Format("{0},{1},{2}", currentHeaderColor.R, currentHeaderColor.G, currentHeaderColor.B))
            WriteIniVal("Colors", "TextColor", String.Format("{0},{1},{2}", currentTextColor.R, currentTextColor.G, currentTextColor.B))
            WriteIniVal("Colors", "LunarBgColor", String.Format("{0},{1},{2}", currentLunarBgColor.R, currentLunarBgColor.G, currentLunarBgColor.B))
        Catch ex As Exception
        End Try
    End Sub

    Private Sub LoadSettingsFromIni()
        If Not File.Exists(iniFilePath) Then
            Me.Location = New Point(Screen.PrimaryScreen.WorkingArea.Width - 150, 100)
            Return
        End If

        Try
            Dim topStr As String = ReadIniVal("Settings", "TopMost", "0")
            Me.TopMost = (topStr = "1")
            chkTopMost.Checked = Me.TopMost

            Dim autoStartStr As String = ReadIniVal("Settings", "AutoStart", "0")
            chkAutoStart.Checked = (autoStartStr = "1")

            Dim defX As Integer = Screen.PrimaryScreen.WorkingArea.Width - 150
            Dim locX As Integer = Integer.Parse(ReadIniVal("Settings", "LocationX", defX.ToString()))
            Dim locY As Integer = Integer.Parse(ReadIniVal("Settings", "LocationY", "100"))
            Me.Location = New Point(locX, locY)

            Dim opStr As String = ReadIniVal("Settings", "Opacity", "0.9")
            Double.TryParse(opStr, NumberStyles.Any, CultureInfo.InvariantCulture, CurrentOpacity)
            Me.Opacity = CurrentOpacity

            Dim formBgRgb As String = ReadIniVal("Colors", "FormBgColor", "")
            If Not String.IsNullOrEmpty(formBgRgb) Then
                currentFormBgColor = ParseColorFromRgb(formBgRgb, currentFormBgColor)
                Me.BackColor = currentFormBgColor
            End If

            Dim headerRgb As String = ReadIniVal("Colors", "HeaderColor", "")
            If Not String.IsNullOrEmpty(headerRgb) Then
                currentHeaderColor = ParseColorFromRgb(headerRgb, currentHeaderColor)
                lblHeader.BackColor = currentHeaderColor
            End If

            Dim textRgb As String = ReadIniVal("Colors", "TextColor", "")
            If Not String.IsNullOrEmpty(textRgb) Then
                currentTextColor = ParseColorFromRgb(textRgb, currentTextColor)
                lblDay.ForeColor = currentTextColor
                lblWeek.ForeColor = Color.FromArgb(200, currentTextColor.R, currentTextColor.G, currentTextColor.B)
            End If

            Dim lunarBgRgb As String = ReadIniVal("Colors", "LunarBgColor", "")
            If Not String.IsNullOrEmpty(lunarBgRgb) Then
                currentLunarBgColor = ParseColorFromRgb(lunarBgRgb, currentLunarBgColor)
                lblLunar.BackColor = currentLunarBgColor
            End If

        Catch ex As Exception
        End Try
    End Sub

    Private Sub WriteIniVal(ByVal section As String, ByVal key As String, ByVal val As String)
        WritePrivateProfileString(section, key, val, iniFilePath)
    End Sub

    Private Function ReadIniVal(ByVal section As String, ByVal key As String, ByVal def As String) As String
        Dim sb As New StringBuilder(255)
        GetPrivateProfileString(section, key, def, sb, 255, iniFilePath)
        Return sb.ToString()
    End Function

    Private Function ParseColorFromRgb(ByVal rgbStr As String, ByVal defaultColor As Color) As Color
        Try
            Dim parts As String() = rgbStr.Split(","c)
            If parts.Length = 3 Then
                Return Color.FromArgb(CByte(parts(0)), CByte(parts(1)), CByte(parts(2)))
            End If
        Catch
        End Try
        Return defaultColor
    End Function

    Protected Overrides Sub OnFormClosing(ByVal e As FormClosingEventArgs)
        SaveSettingsToIni()
        notifyIconMain.Visible = False
        MyBase.OnFormClosing(e)
    End Sub
End Class