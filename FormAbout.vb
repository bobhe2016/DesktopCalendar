Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Public Class FormAbout
    Inherits Form

    Private picAbout As PictureBox

    Public Sub New()
        MyBase.New()

        ' 1. 设置窗体基本属性（固定大小为 471 * 344，无边框/对话框边框）
        Me.Text = "桌面万年历|如果你觉得本工具好用，可以打赏我喝杯咖啡吗？"
        Me.ClientSize = New Size(471, 344)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog ' 或 FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ShowInTaskbar = False

        ' 2. 初始化 PictureBox 控件
        picAbout = New PictureBox()
        picAbout.Dock = DockStyle.Fill
        picAbout.SizeMode = PictureBoxSizeMode.StretchImage ' 铺满拉伸或 MaintainAspectRatio

        ' --- 加载图片方式（二选一）： ---

        ' 【方式 A】：从程序同级目录加载外部图片 (如 about.png 或 about.jpg)
        Dim imgPath As String = Path.Combine(Application.StartupPath, "about.png")
        If File.Exists(imgPath) Then
            picAbout.Image = Image.FromFile(imgPath)
        End If

        ' 【方式 B】：如果图片已添加到项目资源 My.Resources 中（取消下面这行注释）
        picAbout.Image = My.Resources.qrcode

        Me.Controls.Add(picAbout)

        ' 3. 点击图片或窗口任意位置时自动关闭“关于”窗口
        AddHandler picAbout.Click, Sub(s, e) Me.Close()
        AddHandler Me.Click, Sub(s, e) Me.Close()
    End Sub

    Private Sub FormAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class