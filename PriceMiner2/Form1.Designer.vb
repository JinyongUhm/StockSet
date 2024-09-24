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
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.tb_StockCode = New System.Windows.Forms.TextBox()
        Me.tb_Display = New System.Windows.Forms.TextBox()
        Me.cms_MenuForTheList = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.tsmi_SaveClipboard = New System.Windows.Forms.ToolStripMenuItem()
        Me.tm_5sClock = New System.Windows.Forms.Timer(Me.components)
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.cb_ShowLog = New System.Windows.Forms.CheckBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.lb_LogLocked = New System.Windows.Forms.Label()
        Me.bt_Login = New System.Windows.Forms.Button()
        Me.pb_Progress = New System.Windows.Forms.ProgressBar()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.SplitContainer2 = New System.Windows.Forms.SplitContainer()
        Me.lv_DoneDecisionsChart = New PriceMiner.uc0_ListViewSortable()
        Me.ch_ChartDate = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartStart = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartEnter = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartHavingTime = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartCode = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartName = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartEnterPrice = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartExitPrice = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartProfit = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartFallVolume = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartNumberOfEntering = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartRealProfit = New System.Windows.Forms.ColumnHeader()
        Me.ch_ChartBuyRate = New System.Windows.Forms.ColumnHeader()
        Me.ColumnHeader1 = New System.Windows.Forms.ColumnHeader()
        Me.ColumnHeader2 = New System.Windows.Forms.ColumnHeader()
        Me.ColumnHeader3 = New System.Windows.Forms.ColumnHeader()
        Me.ColumnHeader4 = New System.Windows.Forms.ColumnHeader()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.tb_DBListFileName = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.dtp_LastCandleDate = New System.Windows.Forms.DateTimePicker()
        Me.cb_AutoUpdate = New System.Windows.Forms.CheckBox()
        Me.bt_ChartDataValidate = New System.Windows.Forms.Button()
        Me.bt_ChartDataUpdate = New System.Windows.Forms.Button()
        Me.gdp_DisplayChart = New ClassLibrary1.GraphDisplayPanel()
        Me.TabPage4 = New System.Windows.Forms.TabPage()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.ToolStripButton1 = New System.Windows.Forms.ToolStripButton()
        Me.lv_DoneDecisions = New PriceMiner.uc0_ListViewSortable()
        Me.ch_Date = New System.Windows.Forms.ColumnHeader()
        Me.ch_StartTime = New System.Windows.Forms.ColumnHeader()
        Me.ch_EnterTime = New System.Windows.Forms.ColumnHeader()
        Me.ch_ExitTime = New System.Windows.Forms.ColumnHeader()
        Me.ch_CodeDecided = New System.Windows.Forms.ColumnHeader()
        Me.ch_NameDecided = New System.Windows.Forms.ColumnHeader()
        Me.ch_EnterPrice = New System.Windows.Forms.ColumnHeader()
        Me.cg_ExitPrice = New System.Windows.Forms.ColumnHeader()
        Me.ch_Interest = New System.Windows.Forms.ColumnHeader()
        Me.ch_FallVolume = New System.Windows.Forms.ColumnHeader()
        Me.ch_NumberOfEntering = New System.Windows.Forms.ColumnHeader()
        Me.ch_RealProfit = New System.Windows.Forms.ColumnHeader()
        Me.ch_BuyRate = New System.Windows.Forms.ColumnHeader()
        Me.ch_Reserved1 = New System.Windows.Forms.ColumnHeader()
        Me.gdp_Display = New ClassLibrary1.GraphDisplayPanel()
        Me.tm_200msClock = New System.Windows.Forms.Timer(Me.components)
        Me.lb_MemoryUsage = New System.Windows.Forms.Label()
        Me.cms_MenuForTheList.SuspendLayout()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        CType(Me.SplitContainer2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer2.Panel1.SuspendLayout()
        Me.SplitContainer2.Panel2.SuspendLayout()
        Me.SplitContainer2.SuspendLayout()
        Me.TabPage4.SuspendLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.ToolStrip1.SuspendLayout()
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
        Me.tb_Display.MaxLength = 10000
        Me.tb_Display.Multiline = True
        Me.tb_Display.Name = "tb_Display"
        Me.tb_Display.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.tb_Display.Size = New System.Drawing.Size(810, 319)
        Me.tb_Display.TabIndex = 1
        '
        'cms_MenuForTheList
        '
        Me.cms_MenuForTheList.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.tsmi_SaveClipboard})
        Me.cms_MenuForTheList.Name = "cms_MenuForTheList"
        Me.cms_MenuForTheList.Size = New System.Drawing.Size(169, 26)
        '
        'tsmi_SaveClipboard
        '
        Me.tsmi_SaveClipboard.Name = "tsmi_SaveClipboard"
        Me.tsmi_SaveClipboard.Size = New System.Drawing.Size(168, 22)
        Me.tsmi_SaveClipboard.Text = "Save to clipboard"
        '
        'tm_5sClock
        '
        Me.tm_5sClock.Interval = 5000
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage4)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(1116, 528)
        Me.TabControl1.TabIndex = 2
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.lb_MemoryUsage)
        Me.TabPage1.Controls.Add(Me.cb_ShowLog)
        Me.TabPage1.Controls.Add(Me.Button2)
        Me.TabPage1.Controls.Add(Me.Button1)
        Me.TabPage1.Controls.Add(Me.lb_LogLocked)
        Me.TabPage1.Controls.Add(Me.bt_Login)
        Me.TabPage1.Controls.Add(Me.pb_Progress)
        Me.TabPage1.Controls.Add(Me.tb_StockCode)
        Me.TabPage1.Controls.Add(Me.tb_Display)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(1108, 502)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "TabPage1"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'cb_ShowLog
        '
        Me.cb_ShowLog.AutoSize = True
        Me.cb_ShowLog.Checked = True
        Me.cb_ShowLog.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cb_ShowLog.Location = New System.Drawing.Point(839, 74)
        Me.cb_ShowLog.Name = "cb_ShowLog"
        Me.cb_ShowLog.Size = New System.Drawing.Size(72, 16)
        Me.cb_ShowLog.TabIndex = 8
        Me.cb_ShowLog.Text = "로그표시"
        Me.cb_ShowLog.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(876, 266)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 7
        Me.Button2.Text = "Button2"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(839, 196)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(123, 23)
        Me.Button1.TabIndex = 6
        Me.Button1.Text = "MyOrderList 체크"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'lb_LogLocked
        '
        Me.lb_LogLocked.AutoSize = True
        Me.lb_LogLocked.Location = New System.Drawing.Point(839, 15)
        Me.lb_LogLocked.Name = "lb_LogLocked"
        Me.lb_LogLocked.Size = New System.Drawing.Size(90, 12)
        Me.lb_LogLocked.TabIndex = 5
        Me.lb_LogLocked.Text = "Log 정상출력중"
        '
        'bt_Login
        '
        Me.bt_Login.Location = New System.Drawing.Point(867, 124)
        Me.bt_Login.Name = "bt_Login"
        Me.bt_Login.Size = New System.Drawing.Size(75, 23)
        Me.bt_Login.TabIndex = 4
        Me.bt_Login.Text = "Login"
        Me.bt_Login.UseVisualStyleBackColor = True
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
        Me.TabPage2.Controls.Add(Me.SplitContainer2)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(1108, 502)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "TabPage2"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'SplitContainer2
        '
        Me.SplitContainer2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer2.Location = New System.Drawing.Point(3, 3)
        Me.SplitContainer2.Name = "SplitContainer2"
        Me.SplitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer2.Panel1
        '
        Me.SplitContainer2.Panel1.Controls.Add(Me.lv_DoneDecisionsChart)
        '
        'SplitContainer2.Panel2
        '
        Me.SplitContainer2.Panel2.Controls.Add(Me.Label3)
        Me.SplitContainer2.Panel2.Controls.Add(Me.tb_DBListFileName)
        Me.SplitContainer2.Panel2.Controls.Add(Me.Label2)
        Me.SplitContainer2.Panel2.Controls.Add(Me.dtp_LastCandleDate)
        Me.SplitContainer2.Panel2.Controls.Add(Me.cb_AutoUpdate)
        Me.SplitContainer2.Panel2.Controls.Add(Me.bt_ChartDataValidate)
        Me.SplitContainer2.Panel2.Controls.Add(Me.bt_ChartDataUpdate)
        Me.SplitContainer2.Panel2.Controls.Add(Me.gdp_DisplayChart)
        Me.SplitContainer2.Size = New System.Drawing.Size(1102, 496)
        Me.SplitContainer2.SplitterDistance = 230
        Me.SplitContainer2.TabIndex = 0
        '
        'lv_DoneDecisionsChart
        '
        Me.lv_DoneDecisionsChart.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ch_ChartDate, Me.ch_ChartStart, Me.ch_ChartEnter, Me.ch_ChartHavingTime, Me.ch_ChartCode, Me.ch_ChartName, Me.ch_ChartEnterPrice, Me.ch_ChartExitPrice, Me.ch_ChartProfit, Me.ch_ChartFallVolume, Me.ch_ChartNumberOfEntering, Me.ch_ChartRealProfit, Me.ch_ChartBuyRate, Me.ColumnHeader1, Me.ColumnHeader2, Me.ColumnHeader3, Me.ColumnHeader4})
        Me.lv_DoneDecisionsChart.ContextMenuStrip = Me.cms_MenuForTheList
        Me.lv_DoneDecisionsChart.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lv_DoneDecisionsChart.FullRowSelect = True
        Me.lv_DoneDecisionsChart.HideSelection = False
        Me.lv_DoneDecisionsChart.Location = New System.Drawing.Point(0, 0)
        Me.lv_DoneDecisionsChart.MultiSelect = False
        Me.lv_DoneDecisionsChart.Name = "lv_DoneDecisionsChart"
        Me.lv_DoneDecisionsChart.Size = New System.Drawing.Size(1102, 230)
        Me.lv_DoneDecisionsChart.TabIndex = 1
        Me.lv_DoneDecisionsChart.UseCompatibleStateImageBehavior = False
        Me.lv_DoneDecisionsChart.View = System.Windows.Forms.View.Details
        '
        'ch_ChartDate
        '
        Me.ch_ChartDate.Text = "날짜"
        Me.ch_ChartDate.Width = 82
        '
        'ch_ChartStart
        '
        Me.ch_ChartStart.Text = "시작시간"
        Me.ch_ChartStart.Width = 89
        '
        'ch_ChartEnter
        '
        Me.ch_ChartEnter.Text = "진입시간"
        Me.ch_ChartEnter.Width = 90
        '
        'ch_ChartHavingTime
        '
        Me.ch_ChartHavingTime.Text = "청산시간"
        Me.ch_ChartHavingTime.Width = 90
        '
        'ch_ChartCode
        '
        Me.ch_ChartCode.Tag = ""
        Me.ch_ChartCode.Text = "코드"
        Me.ch_ChartCode.Width = 81
        '
        'ch_ChartName
        '
        Me.ch_ChartName.Text = "종목명"
        Me.ch_ChartName.Width = 80
        '
        'ch_ChartEnterPrice
        '
        Me.ch_ChartEnterPrice.Tag = "i"
        Me.ch_ChartEnterPrice.Text = "진입가"
        Me.ch_ChartEnterPrice.Width = 107
        '
        'ch_ChartExitPrice
        '
        Me.ch_ChartExitPrice.Tag = "i"
        Me.ch_ChartExitPrice.Text = "청산가"
        Me.ch_ChartExitPrice.Width = 97
        '
        'ch_ChartProfit
        '
        Me.ch_ChartProfit.Tag = "i"
        Me.ch_ChartProfit.Text = "수익률"
        Me.ch_ChartProfit.Width = 66
        '
        'ch_ChartFallVolume
        '
        Me.ch_ChartFallVolume.Tag = "i"
        Me.ch_ChartFallVolume.Text = "하강볼륨"
        Me.ch_ChartFallVolume.Width = 110
        '
        'ch_ChartNumberOfEntering
        '
        Me.ch_ChartNumberOfEntering.Tag = "i"
        Me.ch_ChartNumberOfEntering.Text = "Entering수"
        '
        'ch_ChartRealProfit
        '
        Me.ch_ChartRealProfit.Tag = "f"
        Me.ch_ChartRealProfit.Text = "실제수익률"
        Me.ch_ChartRealProfit.Width = 66
        '
        'ch_ChartBuyRate
        '
        Me.ch_ChartBuyRate.Tag = "f"
        Me.ch_ChartBuyRate.Text = "매수율"
        Me.ch_ChartBuyRate.Width = 66
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(414, 30)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(112, 12)
        Me.Label3.TabIndex = 8
        Me.Label3.Text = "Filename or DB list"
        '
        'tb_DBListFileName
        '
        Me.tb_DBListFileName.Location = New System.Drawing.Point(532, 27)
        Me.tb_DBListFileName.Name = "tb_DBListFileName"
        Me.tb_DBListFileName.Size = New System.Drawing.Size(200, 21)
        Me.tb_DBListFileName.TabIndex = 7
        Me.tb_DBListFileName.Text = "DB_list_to_update.txt"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(427, 4)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(99, 12)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Last candle date"
        '
        'dtp_LastCandleDate
        '
        Me.dtp_LastCandleDate.Location = New System.Drawing.Point(532, 0)
        Me.dtp_LastCandleDate.Name = "dtp_LastCandleDate"
        Me.dtp_LastCandleDate.Size = New System.Drawing.Size(200, 21)
        Me.dtp_LastCandleDate.TabIndex = 5
        '
        'cb_AutoUpdate
        '
        Me.cb_AutoUpdate.AutoSize = True
        Me.cb_AutoUpdate.Checked = True
        Me.cb_AutoUpdate.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cb_AutoUpdate.Location = New System.Drawing.Point(738, 3)
        Me.cb_AutoUpdate.Name = "cb_AutoUpdate"
        Me.cb_AutoUpdate.Size = New System.Drawing.Size(88, 16)
        Me.cb_AutoUpdate.TabIndex = 4
        Me.cb_AutoUpdate.Text = "AutoUpdate"
        Me.cb_AutoUpdate.UseVisualStyleBackColor = True
        '
        'bt_ChartDataValidate
        '
        Me.bt_ChartDataValidate.Location = New System.Drawing.Point(869, 20)
        Me.bt_ChartDataValidate.Name = "bt_ChartDataValidate"
        Me.bt_ChartDataValidate.Size = New System.Drawing.Size(116, 23)
        Me.bt_ChartDataValidate.TabIndex = 3
        Me.bt_ChartDataValidate.Text = "ChartDataValidate"
        Me.bt_ChartDataValidate.UseVisualStyleBackColor = True
        '
        'bt_ChartDataUpdate
        '
        Me.bt_ChartDataUpdate.Location = New System.Drawing.Point(738, 20)
        Me.bt_ChartDataUpdate.Name = "bt_ChartDataUpdate"
        Me.bt_ChartDataUpdate.Size = New System.Drawing.Size(125, 23)
        Me.bt_ChartDataUpdate.TabIndex = 2
        Me.bt_ChartDataUpdate.Text = "ChartDataUpdate"
        Me.bt_ChartDataUpdate.UseVisualStyleBackColor = True
        '
        'gdp_DisplayChart
        '
        Me.gdp_DisplayChart.DisplayMode = ClassLibrary1.DisplayModeType.SINGLE_DISPLAY_ALTERNATIVE_AXIS
        Me.gdp_DisplayChart.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gdp_DisplayChart.IsTimeBased = True
        Me.gdp_DisplayChart.Location = New System.Drawing.Point(0, 0)
        Me.gdp_DisplayChart.LogScaleOption = False
        Me.gdp_DisplayChart.Name = "gdp_DisplayChart"
        Me.gdp_DisplayChart.Size = New System.Drawing.Size(1102, 262)
        Me.gdp_DisplayChart.SteppedGraphOption = False
        Me.gdp_DisplayChart.TabIndex = 1
        Me.gdp_DisplayChart.TimeDisplayMode = ClassLibrary1.TimeDisplayType.INTERVAL_HHMMSS
        Me.gdp_DisplayChart.TimeOffset = 0R
        Me.gdp_DisplayChart.XAuthFit = False
        Me.gdp_DisplayChart.YAuthFit = True
        '
        'TabPage4
        '
        Me.TabPage4.Controls.Add(Me.SplitContainer1)
        Me.TabPage4.Location = New System.Drawing.Point(4, 22)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage4.Size = New System.Drawing.Size(1108, 502)
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
        Me.SplitContainer1.Panel1.Controls.Add(Me.ToolStrip1)
        Me.SplitContainer1.Panel1.Controls.Add(Me.lv_DoneDecisions)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.gdp_Display)
        Me.SplitContainer1.Size = New System.Drawing.Size(1102, 496)
        Me.SplitContainer1.SplitterDistance = 168
        Me.SplitContainer1.TabIndex = 0
        '
        'ToolStrip1
        '
        Me.ToolStrip1.Dock = System.Windows.Forms.DockStyle.None
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripButton1})
        Me.ToolStrip1.Location = New System.Drawing.Point(24, 88)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Size = New System.Drawing.Size(99, 25)
        Me.ToolStrip1.TabIndex = 1
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'ToolStripButton1
        '
        Me.ToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), System.Drawing.Image)
        Me.ToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.ToolStripButton1.Name = "ToolStripButton1"
        Me.ToolStripButton1.Size = New System.Drawing.Size(87, 22)
        Me.ToolStripButton1.Text = "클립보드 저장"
        '
        'lv_DoneDecisions
        '
        Me.lv_DoneDecisions.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ch_Date, Me.ch_StartTime, Me.ch_EnterTime, Me.ch_ExitTime, Me.ch_CodeDecided, Me.ch_NameDecided, Me.ch_EnterPrice, Me.cg_ExitPrice, Me.ch_Interest, Me.ch_FallVolume, Me.ch_NumberOfEntering, Me.ch_RealProfit, Me.ch_BuyRate, Me.ch_Reserved1})
        Me.lv_DoneDecisions.ContextMenuStrip = Me.cms_MenuForTheList
        Me.lv_DoneDecisions.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lv_DoneDecisions.FullRowSelect = True
        Me.lv_DoneDecisions.HideSelection = False
        Me.lv_DoneDecisions.Location = New System.Drawing.Point(0, 0)
        Me.lv_DoneDecisions.MultiSelect = False
        Me.lv_DoneDecisions.Name = "lv_DoneDecisions"
        Me.lv_DoneDecisions.Size = New System.Drawing.Size(1102, 168)
        Me.lv_DoneDecisions.TabIndex = 0
        Me.lv_DoneDecisions.UseCompatibleStateImageBehavior = False
        Me.lv_DoneDecisions.View = System.Windows.Forms.View.Details
        '
        'ch_Date
        '
        Me.ch_Date.Tag = ""
        Me.ch_Date.Text = "날짜"
        Me.ch_Date.Width = 82
        '
        'ch_StartTime
        '
        Me.ch_StartTime.Text = "시작시간"
        Me.ch_StartTime.Width = 89
        '
        'ch_EnterTime
        '
        Me.ch_EnterTime.Text = "진입시간"
        Me.ch_EnterTime.Width = 90
        '
        'ch_ExitTime
        '
        Me.ch_ExitTime.Text = "청산시간"
        Me.ch_ExitTime.Width = 90
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
        Me.ch_EnterPrice.Tag = "i"
        Me.ch_EnterPrice.Text = "진입가"
        Me.ch_EnterPrice.Width = 107
        '
        'cg_ExitPrice
        '
        Me.cg_ExitPrice.Tag = "i"
        Me.cg_ExitPrice.Text = "청산가"
        Me.cg_ExitPrice.Width = 97
        '
        'ch_Interest
        '
        Me.ch_Interest.Tag = "f"
        Me.ch_Interest.Text = "수익률"
        Me.ch_Interest.Width = 89
        '
        'ch_FallVolume
        '
        Me.ch_FallVolume.Tag = "i"
        Me.ch_FallVolume.Text = "하강볼륨"
        Me.ch_FallVolume.Width = 136
        '
        'ch_NumberOfEntering
        '
        Me.ch_NumberOfEntering.Tag = "i"
        Me.ch_NumberOfEntering.Text = "Entering수"
        '
        'ch_RealProfit
        '
        Me.ch_RealProfit.Tag = "f"
        Me.ch_RealProfit.Text = "실제수익률"
        '
        'ch_BuyRate
        '
        Me.ch_BuyRate.Tag = "f"
        Me.ch_BuyRate.Text = "매수율"
        '
        'ch_Reserved1
        '
        Me.ch_Reserved1.Tag = "f"
        Me.ch_Reserved1.Text = "Reserved1"
        '
        'gdp_Display
        '
        Me.gdp_Display.DisplayMode = ClassLibrary1.DisplayModeType.SINGLE_DISPLAY_ALTERNATIVE_AXIS
        Me.gdp_Display.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gdp_Display.IsTimeBased = True
        Me.gdp_Display.Location = New System.Drawing.Point(0, 0)
        Me.gdp_Display.LogScaleOption = False
        Me.gdp_Display.Name = "gdp_Display"
        Me.gdp_Display.Size = New System.Drawing.Size(1102, 324)
        Me.gdp_Display.SteppedGraphOption = False
        Me.gdp_Display.TabIndex = 0
        Me.gdp_Display.TimeDisplayMode = ClassLibrary1.TimeDisplayType.INTERVAL_HHMMSS
        Me.gdp_Display.TimeOffset = 0R
        Me.gdp_Display.XAuthFit = False
        Me.gdp_Display.YAuthFit = True
        '
        'tm_200msClock
        '
        Me.tm_200msClock.Interval = 200
        '
        'lb_MemoryUsage
        '
        Me.lb_MemoryUsage.AutoSize = True
        Me.lb_MemoryUsage.Location = New System.Drawing.Point(839, 36)
        Me.lb_MemoryUsage.Name = "lb_MemoryUsage"
        Me.lb_MemoryUsage.Size = New System.Drawing.Size(104, 12)
        Me.lb_MemoryUsage.TabIndex = 9
        Me.lb_MemoryUsage.Text = "Memory Usage : "
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1116, 528)
        Me.Controls.Add(Me.TabControl1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Form1"
        Me.Text = "실시간모드 전용"
        Me.cms_MenuForTheList.ResumeLayout(False)
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.TabPage2.ResumeLayout(False)
        Me.SplitContainer2.Panel1.ResumeLayout(False)
        Me.SplitContainer2.Panel2.ResumeLayout(False)
        Me.SplitContainer2.Panel2.PerformLayout()
        CType(Me.SplitContainer2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer2.ResumeLayout(False)
        Me.TabPage4.ResumeLayout(False)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel1.PerformLayout()
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents tb_StockCode As System.Windows.Forms.TextBox
    Friend WithEvents tb_Display As System.Windows.Forms.TextBox
    Friend WithEvents tm_5sClock As System.Windows.Forms.Timer
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents TabPage2 As System.Windows.Forms.TabPage
    Friend WithEvents pb_Progress As System.Windows.Forms.ProgressBar
    Friend WithEvents tm_200msClock As System.Windows.Forms.Timer
    Friend WithEvents TabPage4 As System.Windows.Forms.TabPage
    Friend WithEvents SplitContainer1 As System.Windows.Forms.SplitContainer
    Friend WithEvents lv_DoneDecisions As PriceMiner.uc0_ListViewSortable
    Friend WithEvents ch_StartTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_CodeDecided As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_NameDecided As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_EnterPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents cg_ExitPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Interest As System.Windows.Forms.ColumnHeader
    Friend WithEvents gdp_Display As ClassLibrary1.GraphDisplayPanel
    Friend WithEvents ch_Date As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_FallVolume As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_EnterTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ExitTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents bt_Login As System.Windows.Forms.Button
    Friend WithEvents ch_RealProfit As System.Windows.Forms.ColumnHeader
    Friend WithEvents cms_MenuForTheList As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents tsmi_SaveClipboard As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents lb_LogLocked As System.Windows.Forms.Label
    Friend WithEvents ch_Reserved1 As System.Windows.Forms.ColumnHeader
    Friend WithEvents ToolStrip1 As System.Windows.Forms.ToolStrip
    Friend WithEvents ToolStripButton1 As System.Windows.Forms.ToolStripButton
    Friend WithEvents ch_BuyRate As System.Windows.Forms.ColumnHeader
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents SplitContainer2 As System.Windows.Forms.SplitContainer
    Friend WithEvents lv_DoneDecisionsChart As PriceMiner.uc0_ListViewSortable
    Friend WithEvents ch_ChartDate As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartStart As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartEnter As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartHavingTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartCode As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartEnterPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartExitPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartProfit As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartFallVolume As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartRealProfit As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ChartBuyRate As System.Windows.Forms.ColumnHeader
    Friend WithEvents gdp_DisplayChart As ClassLibrary1.GraphDisplayPanel
    Friend WithEvents bt_ChartDataUpdate As System.Windows.Forms.Button
    Friend WithEvents bt_ChartDataValidate As System.Windows.Forms.Button
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents tb_DBListFileName As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents dtp_LastCandleDate As System.Windows.Forms.DateTimePicker
    Friend WithEvents cb_AutoUpdate As System.Windows.Forms.CheckBox
    Friend WithEvents ch_ChartName As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeader1 As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeader2 As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeader3 As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColumnHeader4 As System.Windows.Forms.ColumnHeader
    Friend WithEvents cb_ShowLog As CheckBox
    Friend WithEvents ch_ChartNumberOfEntering As ColumnHeader
    Friend WithEvents ch_NumberOfEntering As ColumnHeader
    Friend WithEvents lb_MemoryUsage As Label
End Class
