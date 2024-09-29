Imports DSCBO1Lib
Imports CPUTILLib

Module m00_Global
    Public SymTree As New c01_Symtree
    Public SymbolList As New List(Of c03_Symbol)
    Public MainForm As Form1
    Public MarketStartTime As Integer
    Public MarketEndTime As Integer
    Public MA_Base As Integer = 4

    Public Const _MAX_NUMBER_OF_REQUEST As Integer = 110
    Public Const LVSUB_YESTER_PRiCE As Integer = 2
    Public Const LVSUB_YESTER_AMOUNT As Integer = 3
    Public Const LVSUB_VOLUME As Integer = 4
    Public Const LVSUB_INIT_PRICE As Integer = 5
    Public Const LVSUB_NOW_PRICE As Integer = 6
    Public Const LVSUB_AMOUNT As Integer = 7
    Public Const LVSUB_GANGDO As Integer = 8
    Public Const LVSUB_DATA_COUNT As Integer = 9
    Public Const LVSUB_MOVING_AVERAGE_PRICE As Integer = 10
    Public Const LVSUB_BUY_DELTA As Integer = 11
    Public Const LVSUB_SEL_DELTA As Integer = 12

    Public Sub GlobalVarInit(ByVal main_form As Form1)
        MainForm = main_form

        Dim cp_code_mgr As New CpCodeMgr()
        MarketStartTime = CType(cp_code_mgr.GetMarketStartTime(), Integer)
        MarketEndTime = CType(cp_code_mgr.GetMarketEndTime(), Integer)
    End Sub

    Public ReadOnly Property IsMarketTime() As Boolean
        Get
            Dim current_hour As Integer = Now.Hour

            If current_hour - MarketStartTime >= 0 AndAlso MarketEndTime - current_hour > 0 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property
End Module
