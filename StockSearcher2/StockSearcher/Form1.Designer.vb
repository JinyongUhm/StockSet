<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        Me.tb_StockCode = New System.Windows.Forms.TextBox()
        Me.tb_Display = New System.Windows.Forms.TextBox()
        Me.tm_1mClock = New System.Windows.Forms.Timer(Me.components)
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.cb_AutoCPULoad = New System.Windows.Forms.CheckBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.trb_DBInterval = New System.Windows.Forms.TrackBar()
        Me.bt_ChartDataValidate = New System.Windows.Forms.Button()
        Me.bt_ChartDataUpdate = New System.Windows.Forms.Button()
        Me.bt_GlobalTrend = New System.Windows.Forms.Button()
        Me.bt_Login = New System.Windows.Forms.Button()
        Me.trb_Accel = New System.Windows.Forms.TrackBar()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.pb_Progress = New System.Windows.Forms.ProgressBar()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.lv_Symbols = New StockSearcher.uc0_ListViewSortable()
        Me.ch_Code = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_Name = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_YesterPrice = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_YesterAmount = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_Volume = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_InitPrice = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_NowPrice = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_Amount = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_Gangdo = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_DataCount = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_MAPrice = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_BuyDelta = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_SelDelta = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.TabPage3 = New System.Windows.Forms.TabPage()
        Me.cb_DateRepeat = New System.Windows.Forms.CheckBox()
        Me.cb_LearnApplyRepeat = New System.Windows.Forms.CheckBox()
        Me.cb_SmarLearning = New System.Windows.Forms.CheckBox()
        Me.cb_MixedLearning = New System.Windows.Forms.CheckBox()
        Me.cb_AllowMultipleEntering = New System.Windows.Forms.CheckBox()
        Me.gb_PCRenewWeeklyLearn = New System.Windows.Forms.GroupBox()
        Me.lb_Form_DEFAULT_HAVING_TIME6 = New System.Windows.Forms.Label()
        Me.tb_Form_DEFAULT_HAVING_TIME6 = New System.Windows.Forms.TextBox()
        Me.lb_Form_DEFAULT_HAVING_TIME5 = New System.Windows.Forms.Label()
        Me.tb_Form_DEFAULT_HAVING_TIME5 = New System.Windows.Forms.TextBox()
        Me.lb_Form_DEFAULT_HAVING_TIME4 = New System.Windows.Forms.Label()
        Me.tb_Form_DEFAULT_HAVING_TIME4 = New System.Windows.Forms.TextBox()
        Me.lb_Form_DEFAULT_HAVING_TIME3 = New System.Windows.Forms.Label()
        Me.tb_Form_DEFAULT_HAVING_TIME3 = New System.Windows.Forms.TextBox()
        Me.lb_Form_DEFAULT_HAVING_TIME2 = New System.Windows.Forms.Label()
        Me.tb_Form_DEFAULT_HAVING_TIME2 = New System.Windows.Forms.TextBox()
        Me.lb_Form_DEFAULT_HAVING_TIME1 = New System.Windows.Forms.Label()
        Me.tb_Form_DEFAULT_HAVING_TIME1 = New System.Windows.Forms.TextBox()
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD = New System.Windows.Forms.Label()
        Me.tb_Form_FALL_SCALE_LOWER_THRESHOLD = New System.Windows.Forms.TextBox()
        Me.lb_Form_SCORE_THRESHOLD = New System.Windows.Forms.Label()
        Me.tb_Form_SCORE_THRESHOLD = New System.Windows.Forms.TextBox()
        Me.tb_PCRenewWeeklyLearn = New System.Windows.Forms.TextBox()
        Me.cb_WeeklyLearning = New System.Windows.Forms.CheckBox()
        Me.bt_PCRenewWeeklyLearnConfigure = New System.Windows.Forms.Button()
        Me.bt_StartSimul = New System.Windows.Forms.Button()
        Me.dtp_EndDate = New System.Windows.Forms.DateTimePicker()
        Me.dtp_StartDate = New System.Windows.Forms.DateTimePicker()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TabPage4 = New System.Windows.Forms.TabPage()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.lv_DoneDecisions = New StockSearcher.uc0_ListViewSortable()
        Me.ch_Date = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_StartTime = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_EnterTime = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_ExitTime = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_CodeDecided = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_SymbolName = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_EnterPrice = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_ExitPrice = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_Profit = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_FallVolume = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_Score = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ch_RelativeFall = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.cms_MenuForList = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.SaveToClipboardToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.gdp_Display = New ClassLibrary1.GraphDisplayPanel()
        Me.TabPage5 = New System.Windows.Forms.TabPage()
        Me.lb_AveInvestMoney = New System.Windows.Forms.Label()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.lb_AveMaxEarned_NoOverlap = New System.Windows.Forms.Label()
        Me.tb_Result = New System.Windows.Forms.TextBox()
        Me.bt_SaveAnalysis = New System.Windows.Forms.Button()
        Me.lb_AveFallVolume = New System.Windows.Forms.Label()
        Me.lb_AveHitCountNoOverlap = New System.Windows.Forms.Label()
        Me.lb_AveHitCount = New System.Windows.Forms.Label()
        Me.lb_DateCount = New System.Windows.Forms.Label()
        Me.lb_AveMaxEarned = New System.Windows.Forms.Label()
        Me.lb_AveHavingTimeRate = New System.Windows.Forms.Label()
        Me.lb_AveHavingTime = New System.Windows.Forms.Label()
        Me.lb_AveWaitRisingTime = New System.Windows.Forms.Label()
        Me.lb_AveTookTime = New System.Windows.Forms.Label()
        Me.lb_AnualTotalprofitWithoutOverlap = New System.Windows.Forms.Label()
        Me.lb_TotalProfitWithoutOverlap = New System.Windows.Forms.Label()
        Me.lb_OverlapRate = New System.Windows.Forms.Label()
        Me.lb_MaxProfit = New System.Windows.Forms.Label()
        Me.lb_MinProfit = New System.Windows.Forms.Label()
        Me.lb_AveProfit = New System.Windows.Forms.Label()
        Me.lb_HitCount = New System.Windows.Forms.Label()
        Me.lb_StrategyName = New System.Windows.Forms.Label()
        Me.lb_EndDate = New System.Windows.Forms.Label()
        Me.lb_BeginDate = New System.Windows.Forms.Label()
        Me.lb_AnalyzeDate = New System.Windows.Forms.Label()
        Me.bt_ProfitAnalyze = New System.Windows.Forms.Button()
        Me.tm_15sClock = New System.Windows.Forms.Timer(Me.components)
        Me.tm_FormUpdate = New System.Windows.Forms.Timer(Me.components)
        Me.lb_MemoryUsage = New System.Windows.Forms.Label()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        CType(Me.trb_DBInterval, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.trb_Accel, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.TabPage2.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.gb_PCRenewWeeklyLearn.SuspendLayout()
        Me.TabPage4.SuspendLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.cms_MenuForList.SuspendLayout()
        Me.TabPage5.SuspendLayout()
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
        Me.TabControl1.Controls.Add(Me.TabPage5)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(1004, 528)
        Me.TabControl1.TabIndex = 2
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.lb_MemoryUsage)
        Me.TabPage1.Controls.Add(Me.cb_AutoCPULoad)
        Me.TabPage1.Controls.Add(Me.Label4)
        Me.TabPage1.Controls.Add(Me.Label3)
        Me.TabPage1.Controls.Add(Me.trb_DBInterval)
        Me.TabPage1.Controls.Add(Me.bt_ChartDataValidate)
        Me.TabPage1.Controls.Add(Me.bt_ChartDataUpdate)
        Me.TabPage1.Controls.Add(Me.bt_GlobalTrend)
        Me.TabPage1.Controls.Add(Me.bt_Login)
        Me.TabPage1.Controls.Add(Me.trb_Accel)
        Me.TabPage1.Controls.Add(Me.Button1)
        Me.TabPage1.Controls.Add(Me.pb_Progress)
        Me.TabPage1.Controls.Add(Me.tb_StockCode)
        Me.TabPage1.Controls.Add(Me.tb_Display)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(996, 502)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "TabPage1"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'cb_AutoCPULoad
        '
        Me.cb_AutoCPULoad.AutoSize = True
        Me.cb_AutoCPULoad.Checked = True
        Me.cb_AutoCPULoad.CheckState = System.Windows.Forms.CheckState.Checked
        Me.cb_AutoCPULoad.Location = New System.Drawing.Point(829, 18)
        Me.cb_AutoCPULoad.Name = "cb_AutoCPULoad"
        Me.cb_AutoCPULoad.Size = New System.Drawing.Size(154, 16)
        Me.cb_AutoCPULoad.TabIndex = 13
        Me.cb_AutoCPULoad.Text = "Auto CPU Load Control"
        Me.cb_AutoCPULoad.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(712, 127)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(111, 12)
        Me.Label4.TabIndex = 12
        Me.Label4.Text = "DB request interval"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(723, 53)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(100, 12)
        Me.Label3.TabIndex = 11
        Me.Label3.Text = "CPU load control"
        '
        'trb_DBInterval
        '
        Me.trb_DBInterval.Location = New System.Drawing.Point(829, 116)
        Me.trb_DBInterval.Maximum = 100
        Me.trb_DBInterval.Name = "trb_DBInterval"
        Me.trb_DBInterval.Size = New System.Drawing.Size(159, 45)
        Me.trb_DBInterval.TabIndex = 10
        '
        'bt_ChartDataValidate
        '
        Me.bt_ChartDataValidate.Location = New System.Drawing.Point(473, 165)
        Me.bt_ChartDataValidate.Name = "bt_ChartDataValidate"
        Me.bt_ChartDataValidate.Size = New System.Drawing.Size(163, 23)
        Me.bt_ChartDataValidate.TabIndex = 9
        Me.bt_ChartDataValidate.Text = "ChartData 무결성검증"
        Me.bt_ChartDataValidate.UseVisualStyleBackColor = True
        '
        'bt_ChartDataUpdate
        '
        Me.bt_ChartDataUpdate.Location = New System.Drawing.Point(471, 116)
        Me.bt_ChartDataUpdate.Name = "bt_ChartDataUpdate"
        Me.bt_ChartDataUpdate.Size = New System.Drawing.Size(165, 23)
        Me.bt_ChartDataUpdate.TabIndex = 8
        Me.bt_ChartDataUpdate.Text = "ChartData Update"
        Me.bt_ChartDataUpdate.UseVisualStyleBackColor = True
        '
        'bt_GlobalTrend
        '
        Me.bt_GlobalTrend.Location = New System.Drawing.Point(574, 62)
        Me.bt_GlobalTrend.Name = "bt_GlobalTrend"
        Me.bt_GlobalTrend.Size = New System.Drawing.Size(139, 23)
        Me.bt_GlobalTrend.TabIndex = 7
        Me.bt_GlobalTrend.Text = "Global Trend 계산"
        Me.bt_GlobalTrend.UseVisualStyleBackColor = True
        '
        'bt_Login
        '
        Me.bt_Login.Location = New System.Drawing.Point(471, 33)
        Me.bt_Login.Name = "bt_Login"
        Me.bt_Login.Size = New System.Drawing.Size(75, 23)
        Me.bt_Login.TabIndex = 6
        Me.bt_Login.Text = "Login"
        Me.bt_Login.UseVisualStyleBackColor = True
        '
        'trb_Accel
        '
        Me.trb_Accel.Location = New System.Drawing.Point(829, 40)
        Me.trb_Accel.Maximum = 100
        Me.trb_Accel.Minimum = 15
        Me.trb_Accel.Name = "trb_Accel"
        Me.trb_Accel.Size = New System.Drawing.Size(159, 45)
        Me.trb_Accel.TabIndex = 5
        Me.trb_Accel.Value = 100
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(418, 62)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(75, 23)
        Me.Button1.TabIndex = 4
        Me.Button1.Text = "Button1"
        Me.Button1.UseVisualStyleBackColor = True
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
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(996, 502)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "TabPage2"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'lv_Symbols
        '
        Me.lv_Symbols.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ch_Code, Me.ch_Name, Me.ch_YesterPrice, Me.ch_YesterAmount, Me.ch_Volume, Me.ch_InitPrice, Me.ch_NowPrice, Me.ch_Amount, Me.ch_Gangdo, Me.ch_DataCount, Me.ch_MAPrice, Me.ch_BuyDelta, Me.ch_SelDelta})
        Me.lv_Symbols.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lv_Symbols.FullRowSelect = True
        Me.lv_Symbols.HideSelection = False
        Me.lv_Symbols.Location = New System.Drawing.Point(3, 3)
        Me.lv_Symbols.Name = "lv_Symbols"
        Me.lv_Symbols.Size = New System.Drawing.Size(990, 496)
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
        Me.TabPage3.Controls.Add(Me.cb_DateRepeat)
        Me.TabPage3.Controls.Add(Me.cb_LearnApplyRepeat)
        Me.TabPage3.Controls.Add(Me.cb_SmarLearning)
        Me.TabPage3.Controls.Add(Me.cb_MixedLearning)
        Me.TabPage3.Controls.Add(Me.cb_AllowMultipleEntering)
        Me.TabPage3.Controls.Add(Me.gb_PCRenewWeeklyLearn)
        Me.TabPage3.Controls.Add(Me.bt_StartSimul)
        Me.TabPage3.Controls.Add(Me.dtp_EndDate)
        Me.TabPage3.Controls.Add(Me.dtp_StartDate)
        Me.TabPage3.Controls.Add(Me.Label2)
        Me.TabPage3.Controls.Add(Me.Label1)
        Me.TabPage3.Location = New System.Drawing.Point(4, 22)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage3.Size = New System.Drawing.Size(996, 502)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "TabPage3"
        Me.TabPage3.UseVisualStyleBackColor = True
        '
        'cb_DateRepeat
        '
        Me.cb_DateRepeat.AutoSize = True
        Me.cb_DateRepeat.Location = New System.Drawing.Point(497, 166)
        Me.cb_DateRepeat.Name = "cb_DateRepeat"
        Me.cb_DateRepeat.Size = New System.Drawing.Size(92, 16)
        Me.cb_DateRepeat.TabIndex = 10
        Me.cb_DateRepeat.Text = "Date Repeat"
        Me.cb_DateRepeat.UseVisualStyleBackColor = True
        '
        'cb_LearnApplyRepeat
        '
        Me.cb_LearnApplyRepeat.AutoSize = True
        Me.cb_LearnApplyRepeat.Location = New System.Drawing.Point(497, 188)
        Me.cb_LearnApplyRepeat.Name = "cb_LearnApplyRepeat"
        Me.cb_LearnApplyRepeat.Size = New System.Drawing.Size(135, 16)
        Me.cb_LearnApplyRepeat.TabIndex = 9
        Me.cb_LearnApplyRepeat.Text = "Learn Apply Repeat"
        Me.cb_LearnApplyRepeat.UseVisualStyleBackColor = True
        '
        'cb_SmarLearning
        '
        Me.cb_SmarLearning.AutoSize = True
        Me.cb_SmarLearning.Location = New System.Drawing.Point(497, 144)
        Me.cb_SmarLearning.Name = "cb_SmarLearning"
        Me.cb_SmarLearning.Size = New System.Drawing.Size(110, 16)
        Me.cb_SmarLearning.TabIndex = 8
        Me.cb_SmarLearning.Text = "Smart Learning"
        Me.cb_SmarLearning.UseVisualStyleBackColor = True
        '
        'cb_MixedLearning
        '
        Me.cb_MixedLearning.AutoSize = True
        Me.cb_MixedLearning.Location = New System.Drawing.Point(497, 122)
        Me.cb_MixedLearning.Name = "cb_MixedLearning"
        Me.cb_MixedLearning.Size = New System.Drawing.Size(112, 16)
        Me.cb_MixedLearning.TabIndex = 7
        Me.cb_MixedLearning.Text = "Mixed Learning"
        Me.cb_MixedLearning.UseVisualStyleBackColor = True
        '
        'cb_AllowMultipleEntering
        '
        Me.cb_AllowMultipleEntering.AutoSize = True
        Me.cb_AllowMultipleEntering.Location = New System.Drawing.Point(497, 100)
        Me.cb_AllowMultipleEntering.Name = "cb_AllowMultipleEntering"
        Me.cb_AllowMultipleEntering.Size = New System.Drawing.Size(152, 16)
        Me.cb_AllowMultipleEntering.TabIndex = 6
        Me.cb_AllowMultipleEntering.Text = "Allow multiple entering"
        Me.cb_AllowMultipleEntering.UseVisualStyleBackColor = True
        '
        'gb_PCRenewWeeklyLearn
        '
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_DEFAULT_HAVING_TIME6)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_DEFAULT_HAVING_TIME6)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_DEFAULT_HAVING_TIME5)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_DEFAULT_HAVING_TIME5)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_DEFAULT_HAVING_TIME4)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_DEFAULT_HAVING_TIME4)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_DEFAULT_HAVING_TIME3)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_DEFAULT_HAVING_TIME3)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_DEFAULT_HAVING_TIME2)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_DEFAULT_HAVING_TIME2)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_DEFAULT_HAVING_TIME1)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_DEFAULT_HAVING_TIME1)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_FALL_SCALE_LOWER_THRESHOLD)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.lb_Form_SCORE_THRESHOLD)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_Form_SCORE_THRESHOLD)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.tb_PCRenewWeeklyLearn)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.cb_WeeklyLearning)
        Me.gb_PCRenewWeeklyLearn.Controls.Add(Me.bt_PCRenewWeeklyLearnConfigure)
        Me.gb_PCRenewWeeklyLearn.Location = New System.Drawing.Point(34, 90)
        Me.gb_PCRenewWeeklyLearn.Name = "gb_PCRenewWeeklyLearn"
        Me.gb_PCRenewWeeklyLearn.Size = New System.Drawing.Size(373, 375)
        Me.gb_PCRenewWeeklyLearn.TabIndex = 5
        Me.gb_PCRenewWeeklyLearn.TabStop = False
        Me.gb_PCRenewWeeklyLearn.Text = "PCRenew 주간 학습"
        '
        'lb_Form_DEFAULT_HAVING_TIME6
        '
        Me.lb_Form_DEFAULT_HAVING_TIME6.Location = New System.Drawing.Point(6, 335)
        Me.lb_Form_DEFAULT_HAVING_TIME6.Name = "lb_Form_DEFAULT_HAVING_TIME6"
        Me.lb_Form_DEFAULT_HAVING_TIME6.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_DEFAULT_HAVING_TIME6.TabIndex = 18
        Me.lb_Form_DEFAULT_HAVING_TIME6.Text = "DEFAULT_HAVING_TIME6"
        Me.lb_Form_DEFAULT_HAVING_TIME6.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_DEFAULT_HAVING_TIME6
        '
        Me.tb_Form_DEFAULT_HAVING_TIME6.Location = New System.Drawing.Point(238, 335)
        Me.tb_Form_DEFAULT_HAVING_TIME6.Name = "tb_Form_DEFAULT_HAVING_TIME6"
        Me.tb_Form_DEFAULT_HAVING_TIME6.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_DEFAULT_HAVING_TIME6.TabIndex = 17
        '
        'lb_Form_DEFAULT_HAVING_TIME5
        '
        Me.lb_Form_DEFAULT_HAVING_TIME5.Location = New System.Drawing.Point(6, 308)
        Me.lb_Form_DEFAULT_HAVING_TIME5.Name = "lb_Form_DEFAULT_HAVING_TIME5"
        Me.lb_Form_DEFAULT_HAVING_TIME5.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_DEFAULT_HAVING_TIME5.TabIndex = 16
        Me.lb_Form_DEFAULT_HAVING_TIME5.Text = "DEFAULT_HAVING_TIME5"
        Me.lb_Form_DEFAULT_HAVING_TIME5.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_DEFAULT_HAVING_TIME5
        '
        Me.tb_Form_DEFAULT_HAVING_TIME5.Location = New System.Drawing.Point(238, 308)
        Me.tb_Form_DEFAULT_HAVING_TIME5.Name = "tb_Form_DEFAULT_HAVING_TIME5"
        Me.tb_Form_DEFAULT_HAVING_TIME5.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_DEFAULT_HAVING_TIME5.TabIndex = 15
        '
        'lb_Form_DEFAULT_HAVING_TIME4
        '
        Me.lb_Form_DEFAULT_HAVING_TIME4.Location = New System.Drawing.Point(6, 281)
        Me.lb_Form_DEFAULT_HAVING_TIME4.Name = "lb_Form_DEFAULT_HAVING_TIME4"
        Me.lb_Form_DEFAULT_HAVING_TIME4.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_DEFAULT_HAVING_TIME4.TabIndex = 14
        Me.lb_Form_DEFAULT_HAVING_TIME4.Text = "DEFAULT_HAVING_TIME4"
        Me.lb_Form_DEFAULT_HAVING_TIME4.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_DEFAULT_HAVING_TIME4
        '
        Me.tb_Form_DEFAULT_HAVING_TIME4.Location = New System.Drawing.Point(238, 281)
        Me.tb_Form_DEFAULT_HAVING_TIME4.Name = "tb_Form_DEFAULT_HAVING_TIME4"
        Me.tb_Form_DEFAULT_HAVING_TIME4.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_DEFAULT_HAVING_TIME4.TabIndex = 13
        '
        'lb_Form_DEFAULT_HAVING_TIME3
        '
        Me.lb_Form_DEFAULT_HAVING_TIME3.Location = New System.Drawing.Point(6, 254)
        Me.lb_Form_DEFAULT_HAVING_TIME3.Name = "lb_Form_DEFAULT_HAVING_TIME3"
        Me.lb_Form_DEFAULT_HAVING_TIME3.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_DEFAULT_HAVING_TIME3.TabIndex = 12
        Me.lb_Form_DEFAULT_HAVING_TIME3.Text = "DEFAULT_HAVING_TIME3"
        Me.lb_Form_DEFAULT_HAVING_TIME3.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_DEFAULT_HAVING_TIME3
        '
        Me.tb_Form_DEFAULT_HAVING_TIME3.Location = New System.Drawing.Point(238, 254)
        Me.tb_Form_DEFAULT_HAVING_TIME3.Name = "tb_Form_DEFAULT_HAVING_TIME3"
        Me.tb_Form_DEFAULT_HAVING_TIME3.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_DEFAULT_HAVING_TIME3.TabIndex = 11
        '
        'lb_Form_DEFAULT_HAVING_TIME2
        '
        Me.lb_Form_DEFAULT_HAVING_TIME2.Location = New System.Drawing.Point(6, 227)
        Me.lb_Form_DEFAULT_HAVING_TIME2.Name = "lb_Form_DEFAULT_HAVING_TIME2"
        Me.lb_Form_DEFAULT_HAVING_TIME2.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_DEFAULT_HAVING_TIME2.TabIndex = 10
        Me.lb_Form_DEFAULT_HAVING_TIME2.Text = "DEFAULT_HAVING_TIME2"
        Me.lb_Form_DEFAULT_HAVING_TIME2.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_DEFAULT_HAVING_TIME2
        '
        Me.tb_Form_DEFAULT_HAVING_TIME2.Location = New System.Drawing.Point(238, 227)
        Me.tb_Form_DEFAULT_HAVING_TIME2.Name = "tb_Form_DEFAULT_HAVING_TIME2"
        Me.tb_Form_DEFAULT_HAVING_TIME2.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_DEFAULT_HAVING_TIME2.TabIndex = 9
        '
        'lb_Form_DEFAULT_HAVING_TIME1
        '
        Me.lb_Form_DEFAULT_HAVING_TIME1.Location = New System.Drawing.Point(6, 200)
        Me.lb_Form_DEFAULT_HAVING_TIME1.Name = "lb_Form_DEFAULT_HAVING_TIME1"
        Me.lb_Form_DEFAULT_HAVING_TIME1.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_DEFAULT_HAVING_TIME1.TabIndex = 8
        Me.lb_Form_DEFAULT_HAVING_TIME1.Text = "DEFAULT_HAVING_TIME1"
        Me.lb_Form_DEFAULT_HAVING_TIME1.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_DEFAULT_HAVING_TIME1
        '
        Me.tb_Form_DEFAULT_HAVING_TIME1.Location = New System.Drawing.Point(238, 200)
        Me.tb_Form_DEFAULT_HAVING_TIME1.Name = "tb_Form_DEFAULT_HAVING_TIME1"
        Me.tb_Form_DEFAULT_HAVING_TIME1.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_DEFAULT_HAVING_TIME1.TabIndex = 7
        '
        'lb_Form_FALL_SCALE_LOWER_THRESHOLD
        '
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD.Location = New System.Drawing.Point(6, 173)
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD.Name = "lb_Form_FALL_SCALE_LOWER_THRESHOLD"
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD.TabIndex = 6
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD.Text = "FALL_SCALE_LOWER_THRESHOLD"
        Me.lb_Form_FALL_SCALE_LOWER_THRESHOLD.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_FALL_SCALE_LOWER_THRESHOLD
        '
        Me.tb_Form_FALL_SCALE_LOWER_THRESHOLD.Location = New System.Drawing.Point(238, 173)
        Me.tb_Form_FALL_SCALE_LOWER_THRESHOLD.Name = "tb_Form_FALL_SCALE_LOWER_THRESHOLD"
        Me.tb_Form_FALL_SCALE_LOWER_THRESHOLD.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_FALL_SCALE_LOWER_THRESHOLD.TabIndex = 5
        '
        'lb_Form_SCORE_THRESHOLD
        '
        Me.lb_Form_SCORE_THRESHOLD.Location = New System.Drawing.Point(6, 146)
        Me.lb_Form_SCORE_THRESHOLD.Name = "lb_Form_SCORE_THRESHOLD"
        Me.lb_Form_SCORE_THRESHOLD.Size = New System.Drawing.Size(216, 23)
        Me.lb_Form_SCORE_THRESHOLD.TabIndex = 4
        Me.lb_Form_SCORE_THRESHOLD.Text = "SCORE_THRESHOLD"
        Me.lb_Form_SCORE_THRESHOLD.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'tb_Form_SCORE_THRESHOLD
        '
        Me.tb_Form_SCORE_THRESHOLD.Location = New System.Drawing.Point(238, 146)
        Me.tb_Form_SCORE_THRESHOLD.Name = "tb_Form_SCORE_THRESHOLD"
        Me.tb_Form_SCORE_THRESHOLD.Size = New System.Drawing.Size(100, 21)
        Me.tb_Form_SCORE_THRESHOLD.TabIndex = 3
        '
        'tb_PCRenewWeeklyLearn
        '
        Me.tb_PCRenewWeeklyLearn.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.tb_PCRenewWeeklyLearn.Location = New System.Drawing.Point(6, 20)
        Me.tb_PCRenewWeeklyLearn.Multiline = True
        Me.tb_PCRenewWeeklyLearn.Name = "tb_PCRenewWeeklyLearn"
        Me.tb_PCRenewWeeklyLearn.Size = New System.Drawing.Size(361, 65)
        Me.tb_PCRenewWeeklyLearn.TabIndex = 2
        Me.tb_PCRenewWeeklyLearn.Text = "시뮬레이션 끝날을 맞게 입력하고 아래 버튼을 눌러주세요. 그러면 시뮬레이션 첫날, 주간학습여부, Allow multiple entering, Mi" &
    "xed Learning, Smart Learning 컨트롤이 맞게 업데이트됩니다. 그리고나서 실행을 눌러주시면 시뮬레이션이 시작됩니다." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "학습 " &
    "도중에 필요에 따라 설정이 바뀌기도 합니다." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10)
        '
        'cb_WeeklyLearning
        '
        Me.cb_WeeklyLearning.AutoSize = True
        Me.cb_WeeklyLearning.Location = New System.Drawing.Point(161, 101)
        Me.cb_WeeklyLearning.Name = "cb_WeeklyLearning"
        Me.cb_WeeklyLearning.Size = New System.Drawing.Size(100, 16)
        Me.cb_WeeklyLearning.TabIndex = 1
        Me.cb_WeeklyLearning.Text = "주간학습 여부"
        Me.cb_WeeklyLearning.UseVisualStyleBackColor = True
        '
        'bt_PCRenewWeeklyLearnConfigure
        '
        Me.bt_PCRenewWeeklyLearnConfigure.Location = New System.Drawing.Point(6, 91)
        Me.bt_PCRenewWeeklyLearnConfigure.Name = "bt_PCRenewWeeklyLearnConfigure"
        Me.bt_PCRenewWeeklyLearnConfigure.Size = New System.Drawing.Size(149, 34)
        Me.bt_PCRenewWeeklyLearnConfigure.TabIndex = 0
        Me.bt_PCRenewWeeklyLearnConfigure.Text = "PCRenew 주간학습설정"
        Me.bt_PCRenewWeeklyLearnConfigure.UseVisualStyleBackColor = True
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
        Me.dtp_EndDate.Value = New Date(2023, 12, 31, 0, 0, 0, 0)
        '
        'dtp_StartDate
        '
        Me.dtp_StartDate.Location = New System.Drawing.Point(23, 45)
        Me.dtp_StartDate.Name = "dtp_StartDate"
        Me.dtp_StartDate.Size = New System.Drawing.Size(200, 21)
        Me.dtp_StartDate.TabIndex = 2
        Me.dtp_StartDate.Value = New Date(2023, 10, 1, 0, 0, 0, 0)
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
        Me.TabPage4.Location = New System.Drawing.Point(4, 22)
        Me.TabPage4.Name = "TabPage4"
        Me.TabPage4.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage4.Size = New System.Drawing.Size(996, 502)
        Me.TabPage4.TabIndex = 3
        Me.TabPage4.Text = "걸린 애들"
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
        Me.SplitContainer1.Size = New System.Drawing.Size(990, 496)
        Me.SplitContainer1.SplitterDistance = 168
        Me.SplitContainer1.TabIndex = 0
        '
        'lv_DoneDecisions
        '
        Me.lv_DoneDecisions.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ch_Date, Me.ch_StartTime, Me.ch_EnterTime, Me.ch_ExitTime, Me.ch_CodeDecided, Me.ch_SymbolName, Me.ch_EnterPrice, Me.ch_ExitPrice, Me.ch_Profit, Me.ch_FallVolume, Me.ch_Score, Me.ch_RelativeFall})
        Me.lv_DoneDecisions.ContextMenuStrip = Me.cms_MenuForList
        Me.lv_DoneDecisions.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lv_DoneDecisions.FullRowSelect = True
        Me.lv_DoneDecisions.HideSelection = False
        Me.lv_DoneDecisions.Location = New System.Drawing.Point(0, 0)
        Me.lv_DoneDecisions.MultiSelect = False
        Me.lv_DoneDecisions.Name = "lv_DoneDecisions"
        Me.lv_DoneDecisions.Size = New System.Drawing.Size(990, 168)
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
        Me.ch_ExitTime.Tag = ""
        Me.ch_ExitTime.Text = "청산시간"
        Me.ch_ExitTime.Width = 90
        '
        'ch_CodeDecided
        '
        Me.ch_CodeDecided.Text = "코드"
        Me.ch_CodeDecided.Width = 81
        '
        'ch_SymbolName
        '
        Me.ch_SymbolName.Text = "종목명"
        '
        'ch_EnterPrice
        '
        Me.ch_EnterPrice.Tag = "i"
        Me.ch_EnterPrice.Text = "진입가"
        Me.ch_EnterPrice.Width = 107
        '
        'ch_ExitPrice
        '
        Me.ch_ExitPrice.Tag = "i"
        Me.ch_ExitPrice.Text = "청산가"
        Me.ch_ExitPrice.Width = 97
        '
        'ch_Profit
        '
        Me.ch_Profit.Tag = "f"
        Me.ch_Profit.Text = "수익률"
        '
        'ch_FallVolume
        '
        Me.ch_FallVolume.Tag = "i"
        Me.ch_FallVolume.Text = "하강볼륨"
        Me.ch_FallVolume.Width = 136
        '
        'ch_Score
        '
        Me.ch_Score.Tag = ""
        Me.ch_Score.Text = "RelativeFall"
        '
        'cms_MenuForList
        '
        Me.cms_MenuForList.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.SaveToClipboardToolStripMenuItem})
        Me.cms_MenuForList.Name = "cms_MenuForList"
        Me.cms_MenuForList.Size = New System.Drawing.Size(169, 26)
        '
        'SaveToClipboardToolStripMenuItem
        '
        Me.SaveToClipboardToolStripMenuItem.Name = "SaveToClipboardToolStripMenuItem"
        Me.SaveToClipboardToolStripMenuItem.Size = New System.Drawing.Size(168, 22)
        Me.SaveToClipboardToolStripMenuItem.Text = "Save to clipboard"
        '
        'gdp_Display
        '
        Me.gdp_Display.DisplayMode = ClassLibrary1.DisplayModeType.SINGLE_DISPLAY_ALTERNATIVE_AXIS
        Me.gdp_Display.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gdp_Display.IsTimeBased = True
        Me.gdp_Display.Location = New System.Drawing.Point(0, 0)
        Me.gdp_Display.LogScaleOption = False
        Me.gdp_Display.Name = "gdp_Display"
        Me.gdp_Display.Size = New System.Drawing.Size(990, 324)
        Me.gdp_Display.SteppedGraphOption = False
        Me.gdp_Display.TabIndex = 0
        Me.gdp_Display.TimeDisplayMode = ClassLibrary1.TimeDisplayType.INTERVAL_HHMMSS
        Me.gdp_Display.TimeOffset = 0R
        Me.gdp_Display.XAuthFit = False
        Me.gdp_Display.YAuthFit = True
        '
        'TabPage5
        '
        Me.TabPage5.Controls.Add(Me.lb_AveInvestMoney)
        Me.TabPage5.Controls.Add(Me.Button2)
        Me.TabPage5.Controls.Add(Me.lb_AveMaxEarned_NoOverlap)
        Me.TabPage5.Controls.Add(Me.tb_Result)
        Me.TabPage5.Controls.Add(Me.bt_SaveAnalysis)
        Me.TabPage5.Controls.Add(Me.lb_AveFallVolume)
        Me.TabPage5.Controls.Add(Me.lb_AveHitCountNoOverlap)
        Me.TabPage5.Controls.Add(Me.lb_AveHitCount)
        Me.TabPage5.Controls.Add(Me.lb_DateCount)
        Me.TabPage5.Controls.Add(Me.lb_AveMaxEarned)
        Me.TabPage5.Controls.Add(Me.lb_AveHavingTimeRate)
        Me.TabPage5.Controls.Add(Me.lb_AveHavingTime)
        Me.TabPage5.Controls.Add(Me.lb_AveWaitRisingTime)
        Me.TabPage5.Controls.Add(Me.lb_AveTookTime)
        Me.TabPage5.Controls.Add(Me.lb_AnualTotalprofitWithoutOverlap)
        Me.TabPage5.Controls.Add(Me.lb_TotalProfitWithoutOverlap)
        Me.TabPage5.Controls.Add(Me.lb_OverlapRate)
        Me.TabPage5.Controls.Add(Me.lb_MaxProfit)
        Me.TabPage5.Controls.Add(Me.lb_MinProfit)
        Me.TabPage5.Controls.Add(Me.lb_AveProfit)
        Me.TabPage5.Controls.Add(Me.lb_HitCount)
        Me.TabPage5.Controls.Add(Me.lb_StrategyName)
        Me.TabPage5.Controls.Add(Me.lb_EndDate)
        Me.TabPage5.Controls.Add(Me.lb_BeginDate)
        Me.TabPage5.Controls.Add(Me.lb_AnalyzeDate)
        Me.TabPage5.Controls.Add(Me.bt_ProfitAnalyze)
        Me.TabPage5.Location = New System.Drawing.Point(4, 22)
        Me.TabPage5.Name = "TabPage5"
        Me.TabPage5.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage5.Size = New System.Drawing.Size(996, 502)
        Me.TabPage5.TabIndex = 4
        Me.TabPage5.Text = "수익 분석"
        Me.TabPage5.UseVisualStyleBackColor = True
        '
        'lb_AveInvestMoney
        '
        Me.lb_AveInvestMoney.AutoSize = True
        Me.lb_AveInvestMoney.Location = New System.Drawing.Point(640, 382)
        Me.lb_AveInvestMoney.Name = "lb_AveInvestMoney"
        Me.lb_AveInvestMoney.Size = New System.Drawing.Size(89, 12)
        Me.lb_AveInvestMoney.TabIndex = 24
        Me.lb_AveInvestMoney.Tag = "평균 운용금액 : "
        Me.lb_AveInvestMoney.Text = "평균 운용금액 :"
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(263, 6)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(75, 23)
        Me.Button2.TabIndex = 23
        Me.Button2.Text = "소트"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'lb_AveMaxEarned_NoOverlap
        '
        Me.lb_AveMaxEarned_NoOverlap.AutoSize = True
        Me.lb_AveMaxEarned_NoOverlap.Location = New System.Drawing.Point(572, 352)
        Me.lb_AveMaxEarned_NoOverlap.Name = "lb_AveMaxEarned_NoOverlap"
        Me.lb_AveMaxEarned_NoOverlap.Size = New System.Drawing.Size(157, 12)
        Me.lb_AveMaxEarned_NoOverlap.TabIndex = 22
        Me.lb_AveMaxEarned_NoOverlap.Tag = "겹침제외 일평균 최대 벌이 : "
        Me.lb_AveMaxEarned_NoOverlap.Text = "겹침제외 일평균 최대 벌이 :"
        '
        'tb_Result
        '
        Me.tb_Result.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.tb_Result.Location = New System.Drawing.Point(3, 411)
        Me.tb_Result.Multiline = True
        Me.tb_Result.Name = "tb_Result"
        Me.tb_Result.Size = New System.Drawing.Size(990, 88)
        Me.tb_Result.TabIndex = 21
        '
        'bt_SaveAnalysis
        '
        Me.bt_SaveAnalysis.Location = New System.Drawing.Point(89, 6)
        Me.bt_SaveAnalysis.Name = "bt_SaveAnalysis"
        Me.bt_SaveAnalysis.Size = New System.Drawing.Size(110, 23)
        Me.bt_SaveAnalysis.TabIndex = 20
        Me.bt_SaveAnalysis.Text = "분석결과복사"
        Me.bt_SaveAnalysis.UseVisualStyleBackColor = True
        '
        'lb_AveFallVolume
        '
        Me.lb_AveFallVolume.AutoSize = True
        Me.lb_AveFallVolume.Location = New System.Drawing.Point(123, 341)
        Me.lb_AveFallVolume.Name = "lb_AveFallVolume"
        Me.lb_AveFallVolume.Size = New System.Drawing.Size(89, 12)
        Me.lb_AveFallVolume.TabIndex = 19
        Me.lb_AveFallVolume.Tag = "평균 하강볼륨 : "
        Me.lb_AveFallVolume.Text = "평균 하강볼륨 :"
        '
        'lb_AveHitCountNoOverlap
        '
        Me.lb_AveHitCountNoOverlap.AutoSize = True
        Me.lb_AveHitCountNoOverlap.Location = New System.Drawing.Point(59, 276)
        Me.lb_AveHitCountNoOverlap.Name = "lb_AveHitCountNoOverlap"
        Me.lb_AveHitCountNoOverlap.Size = New System.Drawing.Size(153, 12)
        Me.lb_AveHitCountNoOverlap.TabIndex = 18
        Me.lb_AveHitCountNoOverlap.Tag = "겹침제외 일평균 검색건수 : "
        Me.lb_AveHitCountNoOverlap.Text = "겹침제외 일평균 검색건수 :"
        '
        'lb_AveHitCount
        '
        Me.lb_AveHitCount.AutoSize = True
        Me.lb_AveHitCount.Location = New System.Drawing.Point(111, 244)
        Me.lb_AveHitCount.Name = "lb_AveHitCount"
        Me.lb_AveHitCount.Size = New System.Drawing.Size(101, 12)
        Me.lb_AveHitCount.TabIndex = 17
        Me.lb_AveHitCount.Tag = "일평균 검색건수 : "
        Me.lb_AveHitCount.Text = "일평균 검색건수 :"
        '
        'lb_DateCount
        '
        Me.lb_DateCount.AutoSize = True
        Me.lb_DateCount.Location = New System.Drawing.Point(151, 148)
        Me.lb_DateCount.Name = "lb_DateCount"
        Me.lb_DateCount.Size = New System.Drawing.Size(61, 12)
        Me.lb_DateCount.TabIndex = 16
        Me.lb_DateCount.Tag = "유효일수 : "
        Me.lb_DateCount.Text = "유효일수 :"
        '
        'lb_AveMaxEarned
        '
        Me.lb_AveMaxEarned.AutoSize = True
        Me.lb_AveMaxEarned.Location = New System.Drawing.Point(624, 322)
        Me.lb_AveMaxEarned.Name = "lb_AveMaxEarned"
        Me.lb_AveMaxEarned.Size = New System.Drawing.Size(105, 12)
        Me.lb_AveMaxEarned.TabIndex = 15
        Me.lb_AveMaxEarned.Tag = "일평균 최대 벌이 : "
        Me.lb_AveMaxEarned.Text = "일평균 최대 벌이 :"
        '
        'lb_AveHavingTimeRate
        '
        Me.lb_AveHavingTimeRate.AutoSize = True
        Me.lb_AveHavingTimeRate.Location = New System.Drawing.Point(522, 292)
        Me.lb_AveHavingTimeRate.Name = "lb_AveHavingTimeRate"
        Me.lb_AveHavingTimeRate.Size = New System.Drawing.Size(207, 12)
        Me.lb_AveHavingTimeRate.TabIndex = 15
        Me.lb_AveHavingTimeRate.Tag = "평균 보유시간/반등기다림시간 비율 : "
        Me.lb_AveHavingTimeRate.Text = "평균 보유시간/반등기다림시간 비율 :"
        '
        'lb_AveHavingTime
        '
        Me.lb_AveHavingTime.AutoSize = True
        Me.lb_AveHavingTime.Location = New System.Drawing.Point(640, 262)
        Me.lb_AveHavingTime.Name = "lb_AveHavingTime"
        Me.lb_AveHavingTime.Size = New System.Drawing.Size(89, 12)
        Me.lb_AveHavingTime.TabIndex = 14
        Me.lb_AveHavingTime.Tag = "평균 보유시간 : "
        Me.lb_AveHavingTime.Text = "평균 보유시간 :"
        '
        'lb_AveWaitRisingTime
        '
        Me.lb_AveWaitRisingTime.AutoSize = True
        Me.lb_AveWaitRisingTime.Location = New System.Drawing.Point(600, 232)
        Me.lb_AveWaitRisingTime.Name = "lb_AveWaitRisingTime"
        Me.lb_AveWaitRisingTime.Size = New System.Drawing.Size(129, 12)
        Me.lb_AveWaitRisingTime.TabIndex = 13
        Me.lb_AveWaitRisingTime.Tag = "평균 반등기다림 시간 : "
        Me.lb_AveWaitRisingTime.Text = "평균 반등기다림 시간 :"
        '
        'lb_AveTookTime
        '
        Me.lb_AveTookTime.AutoSize = True
        Me.lb_AveTookTime.Location = New System.Drawing.Point(628, 202)
        Me.lb_AveTookTime.Name = "lb_AveTookTime"
        Me.lb_AveTookTime.Size = New System.Drawing.Size(101, 12)
        Me.lb_AveTookTime.TabIndex = 12
        Me.lb_AveTookTime.Tag = "평균 총소요시간 : "
        Me.lb_AveTookTime.Text = "평균 총소요시간 :"
        '
        'lb_AnualTotalprofitWithoutOverlap
        '
        Me.lb_AnualTotalprofitWithoutOverlap.AutoSize = True
        Me.lb_AnualTotalprofitWithoutOverlap.Location = New System.Drawing.Point(560, 172)
        Me.lb_AnualTotalprofitWithoutOverlap.Name = "lb_AnualTotalprofitWithoutOverlap"
        Me.lb_AnualTotalprofitWithoutOverlap.Size = New System.Drawing.Size(169, 12)
        Me.lb_AnualTotalprofitWithoutOverlap.TabIndex = 11
        Me.lb_AnualTotalprofitWithoutOverlap.Tag = "연환산 겹침 제외 누적수익률 : "
        Me.lb_AnualTotalprofitWithoutOverlap.Text = "연환산 겹침 제외 누적수익률 :"
        '
        'lb_TotalProfitWithoutOverlap
        '
        Me.lb_TotalProfitWithoutOverlap.AutoSize = True
        Me.lb_TotalProfitWithoutOverlap.Location = New System.Drawing.Point(600, 142)
        Me.lb_TotalProfitWithoutOverlap.Name = "lb_TotalProfitWithoutOverlap"
        Me.lb_TotalProfitWithoutOverlap.Size = New System.Drawing.Size(129, 12)
        Me.lb_TotalProfitWithoutOverlap.TabIndex = 10
        Me.lb_TotalProfitWithoutOverlap.Tag = "겹침 제외 누적수익률 : "
        Me.lb_TotalProfitWithoutOverlap.Text = "겹침 제외 누적수익률 :"
        '
        'lb_OverlapRate
        '
        Me.lb_OverlapRate.AutoSize = True
        Me.lb_OverlapRate.Location = New System.Drawing.Point(151, 308)
        Me.lb_OverlapRate.Name = "lb_OverlapRate"
        Me.lb_OverlapRate.Size = New System.Drawing.Size(61, 12)
        Me.lb_OverlapRate.TabIndex = 9
        Me.lb_OverlapRate.Tag = "겹침비율 : "
        Me.lb_OverlapRate.Text = "겹침비율 :"
        '
        'lb_MaxProfit
        '
        Me.lb_MaxProfit.AutoSize = True
        Me.lb_MaxProfit.Location = New System.Drawing.Point(656, 112)
        Me.lb_MaxProfit.Name = "lb_MaxProfit"
        Me.lb_MaxProfit.Size = New System.Drawing.Size(73, 12)
        Me.lb_MaxProfit.TabIndex = 8
        Me.lb_MaxProfit.Tag = "최고수익률 : "
        Me.lb_MaxProfit.Text = "최고수익률 :"
        '
        'lb_MinProfit
        '
        Me.lb_MinProfit.AutoSize = True
        Me.lb_MinProfit.Location = New System.Drawing.Point(656, 82)
        Me.lb_MinProfit.Name = "lb_MinProfit"
        Me.lb_MinProfit.Size = New System.Drawing.Size(73, 12)
        Me.lb_MinProfit.TabIndex = 7
        Me.lb_MinProfit.Tag = "최저수익률 : "
        Me.lb_MinProfit.Text = "최저수익률 :"
        '
        'lb_AveProfit
        '
        Me.lb_AveProfit.AutoSize = True
        Me.lb_AveProfit.Location = New System.Drawing.Point(656, 52)
        Me.lb_AveProfit.Name = "lb_AveProfit"
        Me.lb_AveProfit.Size = New System.Drawing.Size(73, 12)
        Me.lb_AveProfit.TabIndex = 6
        Me.lb_AveProfit.Tag = "평균수익률 : "
        Me.lb_AveProfit.Text = "평균수익률 :"
        '
        'lb_HitCount
        '
        Me.lb_HitCount.AutoSize = True
        Me.lb_HitCount.Location = New System.Drawing.Point(135, 212)
        Me.lb_HitCount.Name = "lb_HitCount"
        Me.lb_HitCount.Size = New System.Drawing.Size(77, 12)
        Me.lb_HitCount.TabIndex = 5
        Me.lb_HitCount.Tag = "검색된 건수 : "
        Me.lb_HitCount.Text = "검색된 건수 :"
        '
        'lb_StrategyName
        '
        Me.lb_StrategyName.AutoSize = True
        Me.lb_StrategyName.Location = New System.Drawing.Point(139, 180)
        Me.lb_StrategyName.Name = "lb_StrategyName"
        Me.lb_StrategyName.Size = New System.Drawing.Size(73, 12)
        Me.lb_StrategyName.TabIndex = 4
        Me.lb_StrategyName.Tag = "적용전략명 : "
        Me.lb_StrategyName.Text = "적용전략명 :"
        '
        'lb_EndDate
        '
        Me.lb_EndDate.AutoSize = True
        Me.lb_EndDate.Location = New System.Drawing.Point(163, 116)
        Me.lb_EndDate.Name = "lb_EndDate"
        Me.lb_EndDate.Size = New System.Drawing.Size(49, 12)
        Me.lb_EndDate.TabIndex = 3
        Me.lb_EndDate.Tag = "종료일 : "
        Me.lb_EndDate.Text = "종료일 :"
        '
        'lb_BeginDate
        '
        Me.lb_BeginDate.AutoSize = True
        Me.lb_BeginDate.Location = New System.Drawing.Point(163, 84)
        Me.lb_BeginDate.Name = "lb_BeginDate"
        Me.lb_BeginDate.Size = New System.Drawing.Size(49, 12)
        Me.lb_BeginDate.TabIndex = 2
        Me.lb_BeginDate.Tag = "시작일 : "
        Me.lb_BeginDate.Text = "시작일 :"
        '
        'lb_AnalyzeDate
        '
        Me.lb_AnalyzeDate.AutoSize = True
        Me.lb_AnalyzeDate.Location = New System.Drawing.Point(151, 52)
        Me.lb_AnalyzeDate.Name = "lb_AnalyzeDate"
        Me.lb_AnalyzeDate.Size = New System.Drawing.Size(61, 12)
        Me.lb_AnalyzeDate.TabIndex = 1
        Me.lb_AnalyzeDate.Tag = "분석날짜 : "
        Me.lb_AnalyzeDate.Text = "분석날짜 :"
        '
        'bt_ProfitAnalyze
        '
        Me.bt_ProfitAnalyze.Location = New System.Drawing.Point(8, 6)
        Me.bt_ProfitAnalyze.Name = "bt_ProfitAnalyze"
        Me.bt_ProfitAnalyze.Size = New System.Drawing.Size(75, 23)
        Me.bt_ProfitAnalyze.TabIndex = 0
        Me.bt_ProfitAnalyze.Text = "분석시작"
        Me.bt_ProfitAnalyze.UseVisualStyleBackColor = True
        '
        'tm_15sClock
        '
        Me.tm_15sClock.Interval = 15000
        '
        'tm_FormUpdate
        '
        Me.tm_FormUpdate.Interval = 200
        '
        'lb_MemoryUsage
        '
        Me.lb_MemoryUsage.AutoSize = True
        Me.lb_MemoryUsage.Location = New System.Drawing.Point(471, 215)
        Me.lb_MemoryUsage.Name = "lb_MemoryUsage"
        Me.lb_MemoryUsage.Size = New System.Drawing.Size(42, 12)
        Me.lb_MemoryUsage.TabIndex = 14
        Me.lb_MemoryUsage.Text = "Label5"
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1004, 528)
        Me.Controls.Add(Me.TabControl1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Form1"
        Me.Text = "실시간모드 전용"
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        CType(Me.trb_DBInterval, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.trb_Accel, System.ComponentModel.ISupportInitialize).EndInit()
        Me.TabPage2.ResumeLayout(False)
        Me.TabPage3.ResumeLayout(False)
        Me.TabPage3.PerformLayout()
        Me.gb_PCRenewWeeklyLearn.ResumeLayout(False)
        Me.gb_PCRenewWeeklyLearn.PerformLayout()
        Me.TabPage4.ResumeLayout(False)
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.cms_MenuForList.ResumeLayout(False)
        Me.TabPage5.ResumeLayout(False)
        Me.TabPage5.PerformLayout()
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
    Friend WithEvents ch_Gangdo As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_DataCount As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_InitPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_MAPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_BuyDelta As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_SelDelta As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_StartTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_CodeDecided As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_EnterPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ExitPrice As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_FallVolume As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_EnterTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_ExitTime As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Score As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Profit As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_Date As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_SymbolName As System.Windows.Forms.ColumnHeader
    Friend WithEvents ch_RelativeFall As System.Windows.Forms.ColumnHeader
    Friend WithEvents lv_Symbols As StockSearcher.uc0_ListViewSortable
    Friend WithEvents tm_15sClock As System.Windows.Forms.Timer
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
    Friend WithEvents lv_DoneDecisions As StockSearcher.uc0_ListViewSortable
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents TabPage5 As System.Windows.Forms.TabPage
    Friend WithEvents bt_ProfitAnalyze As System.Windows.Forms.Button
    Friend WithEvents lb_AveProfit As System.Windows.Forms.Label
    Friend WithEvents lb_HitCount As System.Windows.Forms.Label
    Friend WithEvents lb_StrategyName As System.Windows.Forms.Label
    Friend WithEvents lb_EndDate As System.Windows.Forms.Label
    Friend WithEvents lb_BeginDate As System.Windows.Forms.Label
    Friend WithEvents lb_AnalyzeDate As System.Windows.Forms.Label
    Friend WithEvents lb_AveHavingTimeRate As System.Windows.Forms.Label
    Friend WithEvents lb_AveHavingTime As System.Windows.Forms.Label
    Friend WithEvents lb_AveWaitRisingTime As System.Windows.Forms.Label
    Friend WithEvents lb_AveTookTime As System.Windows.Forms.Label
    Friend WithEvents lb_AnualTotalprofitWithoutOverlap As System.Windows.Forms.Label
    Friend WithEvents lb_TotalProfitWithoutOverlap As System.Windows.Forms.Label
    Friend WithEvents lb_OverlapRate As System.Windows.Forms.Label
    Friend WithEvents lb_MaxProfit As System.Windows.Forms.Label
    Friend WithEvents lb_MinProfit As System.Windows.Forms.Label
    Friend WithEvents lb_DateCount As System.Windows.Forms.Label
    Friend WithEvents lb_AveHitCountNoOverlap As System.Windows.Forms.Label
    Friend WithEvents lb_AveHitCount As System.Windows.Forms.Label
    Friend WithEvents lb_AveFallVolume As System.Windows.Forms.Label
    Friend WithEvents bt_SaveAnalysis As System.Windows.Forms.Button
    Friend WithEvents tb_Result As System.Windows.Forms.TextBox
    Friend WithEvents cms_MenuForList As System.Windows.Forms.ContextMenuStrip
    Friend WithEvents SaveToClipboardToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents lb_AveMaxEarned As System.Windows.Forms.Label
    Friend WithEvents gdp_Display As ClassLibrary1.GraphDisplayPanel
    Friend WithEvents trb_Accel As System.Windows.Forms.TrackBar
    Friend WithEvents bt_Login As System.Windows.Forms.Button
    Friend WithEvents bt_GlobalTrend As System.Windows.Forms.Button
    Friend WithEvents lb_AveMaxEarned_NoOverlap As System.Windows.Forms.Label
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents bt_ChartDataUpdate As System.Windows.Forms.Button
    Friend WithEvents bt_ChartDataValidate As System.Windows.Forms.Button
    Friend WithEvents lb_AveInvestMoney As System.Windows.Forms.Label
    Friend WithEvents Label3 As Label
    Friend WithEvents trb_DBInterval As TrackBar
    Friend WithEvents Label4 As Label
    Friend WithEvents gb_PCRenewWeeklyLearn As GroupBox
    Friend WithEvents bt_PCRenewWeeklyLearnConfigure As Button
    Friend WithEvents cb_AllowMultipleEntering As CheckBox
    Friend WithEvents cb_WeeklyLearning As CheckBox
    Friend WithEvents cb_MixedLearning As CheckBox
    Friend WithEvents cb_SmarLearning As CheckBox
    Friend WithEvents tb_PCRenewWeeklyLearn As TextBox
    Friend WithEvents tb_Form_SCORE_THRESHOLD As TextBox
    Friend WithEvents lb_Form_SCORE_THRESHOLD As Label
    Friend WithEvents lb_Form_FALL_SCALE_LOWER_THRESHOLD As Label
    Friend WithEvents tb_Form_FALL_SCALE_LOWER_THRESHOLD As TextBox
    Friend WithEvents lb_Form_DEFAULT_HAVING_TIME1 As Label
    Friend WithEvents tb_Form_DEFAULT_HAVING_TIME1 As TextBox
    Friend WithEvents lb_Form_DEFAULT_HAVING_TIME2 As Label
    Friend WithEvents tb_Form_DEFAULT_HAVING_TIME2 As TextBox
    Friend WithEvents lb_Form_DEFAULT_HAVING_TIME6 As Label
    Friend WithEvents tb_Form_DEFAULT_HAVING_TIME6 As TextBox
    Friend WithEvents lb_Form_DEFAULT_HAVING_TIME5 As Label
    Friend WithEvents tb_Form_DEFAULT_HAVING_TIME5 As TextBox
    Friend WithEvents lb_Form_DEFAULT_HAVING_TIME4 As Label
    Friend WithEvents tb_Form_DEFAULT_HAVING_TIME4 As TextBox
    Friend WithEvents lb_Form_DEFAULT_HAVING_TIME3 As Label
    Friend WithEvents tb_Form_DEFAULT_HAVING_TIME3 As TextBox
    Friend WithEvents cb_AutoCPULoad As CheckBox
    Friend WithEvents cb_LearnApplyRepeat As CheckBox
    Friend WithEvents cb_DateRepeat As CheckBox
    Friend WithEvents lb_MemoryUsage As Label
End Class
