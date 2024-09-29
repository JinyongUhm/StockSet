<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Me.tb_StockCode = New System.Windows.Forms.TextBox
        Me.tb_Display = New System.Windows.Forms.TextBox
        Me.tm_1mClock = New System.Windows.Forms.Timer(Me.components)
        Me.TabControl1 = New System.Windows.Forms.TabControl
        Me.TabPage1 = New System.Windows.Forms.TabPage
        Me.pb_Progress = New System.Windows.Forms.ProgressBar
        Me.TabPage2 = New System.Windows.Forms.TabPage
        Me.lv_Symbols = New StockSearcher.uc0_ListViewSortable
        Me.ch_Code = New System.Windows.Forms.ColumnHeader
        Me.ch_Name = New System.Windows.Forms.ColumnHeader
        Me.ch_YesterPrice = New System.Windows.Forms.ColumnHeader
        Me.ch_YesterAmount = New System.Windows.Forms.ColumnHeader
        Me.ch_Volume = New System.Windows.Forms.ColumnHeader
        Me.ch_InitPrice = New System.Windows.Forms.ColumnHeader
        Me.ch_NowPrice = New System.Windows.Forms.ColumnHeader
        Me.ch_Amount = New System.Windows.Forms.ColumnHeader
        Me.ch_Gangdo = New System.Windows.Forms.ColumnHeader
        Me.ch_DataCount = New System.Windows.Forms.ColumnHeader
        Me.ch_MAPrice = New System.Windows.Forms.ColumnHeader
        Me.ch_BuyDelta = New System.Windows.Forms.ColumnHeader
        Me.ch_SelDelta = New System.Windows.Forms.ColumnHeader
        Me.TabPage3 = New System.Windows.Forms.TabPage
        Me.bt_StartSimul = New System.Windows.Forms.Button
        Me.dtp_EndDate = New System.Windows.Forms.DateTimePicker
        Me.dtp_StartDate = New System.Windows.Forms.DateTimePicker
        Me.Label2 = New System.Windows.Forms.Label
        Me.Label1 = New System.Windows.Forms.Label
        Me.TabPage4 = New System.Windows.Forms.TabPage
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer
        Me.lv_DoneDecisions = New System.Windows.Forms.ListView
        Me.ch_StartTime = New System.Windows.Forms.ColumnHeader
        Me.ch_CodeDecided = New System.Windows.Forms.ColumnHeader
        Me.ch_NameDecided = New System.Windows.Forms.ColumnHeader
        Me.ch_EnterPrice = New System.Windows.Forms.ColumnHeader
        Me.cg_ExitPrice = New System.Windows.Forms.ColumnHeader
        Me.ch_Interest = New System.Windows.Forms.ColumnHeader
        Me.ch_TimeTaken = New System.Windows.Forms.ColumnHeader
        Me.gdp_Display = New ClassLibrary1.GraphDisplayPanel
        Me.tm_15sClock = New System.Windows.Forms.Timer(Me.components)
        Me.tm_FormUpdate = New System.Windows.Forms.Timer(Me.components)
        Me.Button1 = New System.Windows.Forms.Button
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.TabPage4.SuspendLayout()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.SuspendLayout()
        '
        'tb_StockCode
        '
        Me.tb_StockCode.Location = New System.Drawing.Point(6, 6)
        Me.tb_StockCode.Name = "tb_StockCode"
        Me.tb_StockCode.Size = New System.Drawing.Size(136, 21)
        Me.tb_StockCode.TabIndex = 0
        Me.tb_StockCode.Text = "A000660"
        '
        'tb_Display
        '
        Me.tb_Display.Location = New System.Drawing.Point(6, 33)
        Me.tb_Display.Multiline = True
        Me.tb_Display.Name = "tb_Display"
        Me.tb_Display.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.tb_Display.Size = New System.Drawing.Size(406, 240)
        Me.tb_Display.TabIndex = 1
        '
        'tm_1mClock
        '
        Me.tm_1mClock.Interval = 6000
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage3)
        Me.TabControl1.Controls.Add(Me.TabPage4)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(1004, 528)
        Me.TabControl1.TabIndex = 2
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.Button1)
        Me.TabPage1.Controls.Add(Me.pb_Progress)
        Me.TabPage1.Controls.Add(Me.tb_StockCode)
        Me.TabPage1.Controls.Add(Me.tb_Display)
        Me.TabPage1.Location = New System.Drawing.Point(4, 21)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(996, 503)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "TabPage1"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'pb_Progress
        '
        Me.pb_Progress.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.pb_Progress.Location = New System.Drawing.Point(8, 358)
        Me.pb_Progress.Name = "pb_Progress"
        Me.pb_Progress.Size = New System.Drawing.Size(903, 23)
        Me.pb_Progress.TabIndex = 3
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.lv_Symbols)
        Me.TabPage2.Location = New System.Drawing.Point(4, 21)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(996, 503)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "TabPage2"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'lv_Symbols
        '
        Me.lv_Symbols.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ch_Code, Me.ch_Name, Me.ch_YesterPrice, Me.ch_YesterAmount, Me.ch_Volume, Me.ch_InitPrice, Me.ch_NowPrice, Me.ch_Amount, Me.ch_Gangdo, Me.ch_DataCount, Me.ch_MAPrice, Me.ch_BuyDelta, Me.ch_SelDelta})
        Me.lv_Symbols.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lv_Symbols.FullRowSelect = True
        Me.lv_Symbols.Location = New System.Drawing.Point(3, 3)
        Me.lv_Symbols.Name = "lv_Symbols"
        Me.lv_Symbols.Size = New System.Drawing.Size(990, 497)
        Me.lv_Symbols.TabIndex = 0
        Me.lv_Symbols.UseCompatibleStateImageBehavior = False
        Me.lv_Symbols.View = System.Windows.Forms.View.Details
        '
        'ch_Code
        '
        Me.ch_Code.Text = "Code"
        '
        'ch_Name
        '
        Me.ch_Name.Text = "Name"
        Me.ch_Name.Width = 89
        '
        'ch_YesterPrice
        '
        Me.ch_YesterPrice.Tag = "i"
        Me.ch_YesterPrice.Text = "전일종가"
        Me.ch_YesterPrice.Width = 73
        '
        'ch_YesterAmount
        '
        Me.ch_YesterAmount.Tag = "i"
        Me.ch_YesterAmount.Text = "전일거래"
        Me.ch_YesterAmount.Width = 78
        '
        'ch_Volume
        '
        Me.ch_Volume.Tag = "i"
        Me.ch_Volume.Text = "전일볼륨"
        Me.ch_Volume.Width = 90
        '
        'ch_InitPrice
        '
        Me.ch_InitPrice.Text = "초기가"
        '
        'ch_NowPrice
        '
        Me.ch_NowPrice.Tag = "i"
        Me.ch_NowPrice.Text = "현재가"
        Me.ch_NowPrice.Width = 68
        '
        'ch_Amount
        '
        Me.ch_Amount.Tag = "i"
        Me.ch_Amount.Text = "거래량"
        Me.ch_Amount.Width = 84
        '
        'ch_Gangdo
        '
        Me.ch_Gangdo.Tag = "f"
        Me.ch_Gangdo.Text = "체결강도"
        Me.ch_Gangdo.Width = 69
        '
        'ch_DataCount
        '
        Me.ch_DataCount.Text = "Data갯수"
        Me.ch_DataCount.Width = 69
        '
        'ch_MAPrice
        '
        Me.ch_MAPrice.Text = "이동평균"
        Me.ch_MAPrice.Width = 93
        '
        'ch_BuyDelta
        '
        Me.ch_BuyDelta.Text = "델타매수량"
        Me.ch_BuyDelta.Width = 80
        '
        'ch_SelDelta
        '
        Me.ch_SelDelta.Text = "델타매도량"
        Me.ch_SelDelta.Width = 75
        '
        'TabPage3
        '
        Me.TabPage3.Controls.Add(Me.bt_StartSimul)
        Me.TabPage3.Controls.Add(Me.dtp_EndDate)
        Me.TabPage3.Controls.Add(Me.dtp_StartDate)
        Me.TabPage3.Controls.Add(Me.Label2)
        Me.TabPage3.Controls.Add(Me.Label1)
        Me.TabPage3.Location = New System.Drawing.Point(4, 21)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage3.Size = New System.Drawing.Size(996, 503)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "TabPage3"
        Me.TabPage3.UseVisualStyleBackColor = True
        '
        'bt_StartSimul
        '
        Me.bt_StartSimul.Location = New System.Drawing.Point(510, 43)
        Me.bt_StartSimul.Name = "bt_StartSimul"
        Me.bt_StartSimul.Size = New System.Drawing.Size(75, 23)
        Me.bt_StartSimul.TabIndex = 4
        Me.bt_StartSimul.Text = "실행"
        Me.bt_StartSimul.UseVisualStyleBackColor = True
        '
        'dtp_EndDate
        '
        Me.dtp_EndDate.Location = New System.Drawing.Point(272, 45)
        Me.dtp_EndDate.Name = "dtp_EndDate"
        Me.dtp_EndDate.Size = New System.Drawing.Size(200, 21)
        Me.dtp_EndDate.TabIndex = 3
        '
        'dtp_StartDate
        '
        Me.dtp_StartDate.Location = New System.Drawing.Point(23, 45)
        Me.dtp_StartDate.Name = "dtp_StartDate"
        Me.dtp_StartDate.Size = New System.Drawing.Size(200, 21)
        Me.dtp_StartDate.TabIndex = 2
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(242, 49)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(14, 12)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "~"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(21, 13)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(65, 12)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "시뮬레이션"
        '
        'TabPage4
        '
        Me.TabPage4.Controls.Add(Me.SplitContainer1)
        Me.TabPage4.Location = New System.Drawing.Point(4, 21)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage4.Size = New System.Drawing.Size(996, 503)
        Me.TabPage4.TabIndex = 3
        Me.TabPage4.Text = "TabPage4"
        Me.TabPage4.UseVisualStyleBackColor = True
        '
        'SplitContainer1
        '
        Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer1.Location = New System.Drawing.Point(3, 3)
        Me.SplitContainer1.Name = "SplitContainer1"
        Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.lv_DoneDecisions)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.gdp_Display)
        Me.SplitContainer1.Size = New System.Drawing.Size(990, 497)
        Me.SplitContainer1.SplitterDistance = 169
        Me.SplitContainer1.TabIndex = 0
        '
        'lv_DoneDecisions
        '
        Me.lv_DoneDecisions.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ch_StartTime, Me.ch_CodeDecided, Me.ch_NameDecided, Me.ch_EnterPrice, Me.cg_ExitPrice, Me.ch_Interest, Me.ch_TimeTaken})
        Me.lv_DoneDecisions.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lv_DoneDecisions.Location = New System.Drawing.Point(0, 0)
        Me.lv_DoneDecisions.Name = "lv_DoneDecisions"
        Me.lv_DoneDecisions.Size = New System.Drawing.Size(990, 169)
        Me.lv_DoneDecisions.TabIndex = 0
        Me.lv_DoneDecisions.UseCompatibleStateImageBehavior = False
        Me.lv_DoneDecisions.View = System.Windows.Forms.View.Details
        '
        'ch_StartTime
        '
        Me.ch_StartTime.Text = "시작시간"
        Me.ch_StartTime.Width = 89
        '
        'ch_CodeDecided
        '
        Me.ch_CodeDecided.Text = "코드"
        Me.ch_CodeDecided.Width = 81
        '
        'ch_NameDecided
        '
        Me.ch_NameDecided.Text = "종목명"
        Me.ch_NameDecided.Width = 108
        '
        'ch_EnterPrice
        '
        Me.ch_EnterPrice.Text = "진입가"
        Me.ch_EnterPrice.Width = 107
        '
        'cg_ExitPrice
        '
        Me.cg_ExitPrice.Text = "청산가"
        Me.cg_ExitPrice.Width = 97
        '
        'ch_Interest
        '
        Me.ch_Interest.Text = "수익률"
        Me.ch_Interest.Width = 89
        '
        'ch_TimeTaken
        '
        Me.ch_TimeTaken.Text = "소요시간"
        Me.ch_TimeTaken.Width = 76
        '
        'gdp_Display
        '
        Me.gdp_Display.DisplayMode = ClassLibrary1.DisplayModeType.MULTI_DISPLAY
        Me.gdp_Display.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gdp_Display.IsTimeBased = False
        Me.gdp_Display.Location = New System.Drawing.Point(0, 0)
        Me.gdp_Display.LogScaleOption = False
        Me.gdp_Display.Name = "gdp_Display"
        Me.gdp_Display.Size = New System.Drawing.Size(990, 324)
        Me.gdp_Display.SteppedGraphOption = False
        Me.gdp_Display.TabIndex = 0
        Me.gdp_Display.TimeDisplayMode = ClassLibrary1.TimeDisplayType.INTERVAL_SECONDS
        Me.gdp_Display.TimeOffset = 0
        '
        'tm_15sClock
        '
        Me.tm_15sClock.Interval = 15000
        '
        'tm_FormUpdate
        '
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(420, 168)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 4
        Me.Button1.Text = "Button1"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1004, 528)
        Me.Controls.Add(Me.TabControl1)
        Me.Name = "Form1"
        Me.Text = "Form1"
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.TabPage2.ResumeLayout(False)
        Me.TabPage3.ResumeLayout(False)
        Me.TabPage3.PerformLayout()
        Me.TabPage4.ResumeLayout(False)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tb_StockCode As System.Windows.Forms.TextBox
    Friend WithEvents tb_Display As System.Windows.Forms.TextBox
    Friend WithEvents tm_1mClock As System.Windows.Forms.Timer
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents TabPage2 As System.Windows.Forms.TabPage
    Friend WithEvents ch_Code As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Name As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_YesterPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_YesterAmount As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Volume As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_NowPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Amount As System.Windows.Forms.ColumnHeader
    Friend WithEvents lv_Symbols As StockSearcher.uc0_ListViewSortable
    Friend WithEvents tm_15sClock As System.Windows.Forms.Timer
    Friend WithEvents ch_Gangdo As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_DataCount As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_InitPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_MAPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_BuyDelta As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_SelDelta As System.Windows.Forms.ColumnHeader
    Friend WithEvents TabPage3 As System.Windows.Forms.TabPage
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents dtp_StartDate As System.Windows.Forms.DateTimePicker
    Friend WithEvents bt_StartSimul As System.Windows.Forms.Button
    Friend WithEvents dtp_EndDate As System.Windows.Forms.DateTimePicker
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents pb_Progress As System.Windows.Forms.ProgressBar
    Friend WithEvents tm_FormUpdate As System.Windows.Forms.Timer
    Friend WithEvents TabPage4 As System.Windows.Forms.TabPage
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents lv_DoneDecisions As System.Windows.Forms.ListView
    Friend WithEvents ch_StartTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_CodeDecided As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_NameDecided As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_EnterPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents cg_ExitPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Interest As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_TimeTaken As System.Windows.Forms.ColumnHeader
    Friend WithEvents gdp_Display As ClassLibrary1.GraphDisplayPanel
    Friend WithEvents Button1 As System.Windows.Forms.Button

End Class
