#If ETRADE_CONNECTION Then
Imports XA_DATASETLib
#End If
Imports System.Data.SqlClient
Imports System.Runtime.CompilerServices
Imports System.Xml
Imports System.Diagnostics

Public Class Form1
#If NO_SHOW_TO_THE_FORM Then
    Public HitList As New List(Of ListViewItem)
#End If
    Public Delegate Sub DelegateSortDateTime()
    Public Delegate Sub DelegateResultAnalysis()
    Public Delegate Sub DelegateCopyResultAndDeleteContents()
#If ETRADE_CONNECTION Then
    Private WithEvents _T1305 As New XAQuery
    Private WithEvents _ChartIndex As New XAQuery
    Private WithEvents _T8412 As New XAQuery           '190131: 왜 이벤트가 안 걸리는지 모르겠다. 다른 TR을 돌렸을 때는 어떤가 확인해보자.
    Public HistoryThread As New Threading.Thread(AddressOf HistoryProcess)
#End If
    Public CurrentProcess As Process = Process.GetCurrentProcess()
    Private ResponseReceived As Boolean
    Private RxProcessCompleted As Boolean
    Private RequestResult As Integer
    Public FailedSymbolNumber As Integer
    Public NoResponseNumber As Integer
    Public ProgressText1, ProgressText2, ProgressText3 As String
    Public LastExceptionErrorMesssage As String
    Public LoggedButAccountNotRequested As Boolean = False
    'Public StockMst2Obj As New StockMst2
    'Public StockBidObj As StockBid
#If ETRADE_CONNECTION Then
    Public WithEvents xa_session As XA_SESSIONLib.XASession
#End If
    Public Shared SVR_IP_VIRTUAL As String = "demo.etrade.co.kr"
    Public Shared SVR_PORT_VIRTUAL As String = "20001"
    Public Shared USER_ID_VIRTUAL As String = "jeanion"
    Public Shared USER_PASSWORD_VIRTUAL As String = "sejin00"
    Public Shared USER_CERT_VIRTUAL As String = "mio7ne3ry"
    Public Shared SVR_IP_REAL As String = "hts.ebestsec.co.kr"
    Public Shared SVR_PORT_REAL As String = "20001"
    Public Shared USER_ID_REAL As String = "jeanion"
    Public Shared USER_PASSWORD_REAL As String = "qlfkdjet"
    Public Shared USER_CERT_REAL As String = "rhdtkaxkr3937*"
    Private Const _STOCK_HI_PRICE As Integer = 100000000
    Private Const _LO_VOLUME As Long = 100000
    Private _Counting As Integer = 0
#If MOVING_AVERAGE_DIFFERENCE Then
    Public DBSupporter As c042_ChartDBSupport
#Else
    Public DBSupporter As c041_DefaultDBSupport
#End If
    'Public RealTimeMode As Boolean
    Public IsThreadExecuting As Boolean
    Public InvokingDone As Boolean = False
    Public AccelValue As Integer = 50
    Public DBIntervalStored As Integer
    Private LogFileSaveTimeCount As Integer = 0
#If PCRENEW_LEARN Then
    Public Form_SCORE_THRESHOLD As Double = 0.042436
    Public Form_FALL_SCALE_LOWER_THRESHOLD As Double = 1.07
    Public Form_DEFAULT_HAVING_TIME1 As Integer = 13
    Public Form_DEFAULT_HAVING_TIME2 As Integer = 13
    Public Form_DEFAULT_HAVING_TIME3 As Integer = 13
    Public Form_DEFAULT_HAVING_TIME4 As Integer = 13
    Public Form_DEFAULT_HAVING_TIME5 As Integer = 13
    Public Form_DEFAULT_HAVING_TIME6 As Integer = 13
    Public Form_TH_ATTENUATION As Double = 0.117157287525381
    Public Form_VOLUME_ATTENUATION As Double = 0.45
#If UNWANTED_RISING_DETECTION Then
    Public Form_TIME_DIFF_FOR_RISING_DETECTION As UInt32 = 88 '50   
    Public Form_RISING_SLOPE_THRESHOLD As Double = 0.094198743936 '0.108
    Public Form_ENTERING_PROHIBIT_TIME As UInt32 = 64 '44
#End If

    'Public BestCoeffs() As Double = {0.064396 / 0.97, 1.072375, 27, 13, 9, 7, 15, 59} 'Please Start From Here
    Public BestCoeffs() As Double = {Form_SCORE_THRESHOLD, Form_FALL_SCALE_LOWER_THRESHOLD, Form_DEFAULT_HAVING_TIME1, Form_DEFAULT_HAVING_TIME2, Form_DEFAULT_HAVING_TIME3, Form_DEFAULT_HAVING_TIME4, Form_DEFAULT_HAVING_TIME5, Form_DEFAULT_HAVING_TIME6} 'Please Start From Here
#ElseIf DOUBLE_FALL Then
    Public Form_SCORE_THRESHOLD1 As Single = 0.0549836
    Public Form_FALL_SCALE_LOWER_THRESHOLD1 As Single = 1.030083
    Public Form_SCORE_THRESHOLD2 As Single = 0.07399661
    Public Form_FALL_SCALE_LOWER_THRESHOLD2 As Single = 1.066653
    Public Form_MAX_SECONDFALL_WAITING As Integer = 36
    Public Form_DEFAULT_HAVING_TIME As Integer = 32

    Public BestCoeffs() As Double = {Form_SCORE_THRESHOLD1, Form_FALL_SCALE_LOWER_THRESHOLD1, Form_SCORE_THRESHOLD2, Form_FALL_SCALE_LOWER_THRESHOLD2, Form_MAX_SECONDFALL_WAITING, Form_DEFAULT_HAVING_TIME}
#ElseIf SHARP_LINES Then
    Public BestCoeffs() As Double = {91.35, 2.46305418719212, 14.2011834319527, 8, 50.7692307692308, -0.776699029126214, -1.06796116504854}

    Public Form_VERTICAL_SCALE As Double = 91.35
    Public Form_HOT_HEIGHT As Double = 2.46305418719212
    Public Form_HOT_WIDTH As Double = 14.2011834319527
    Public Form_STANDARD_LENGTH As Double = 8
    Public Form_X_LIMIT As Double = 50.7692307692308
    Public Form_SLOPE1_LIMIT As Double = -0.776699029126214
    Public Form_SLOPE3_LIMIT As Double = -1.06796116504854
#Else
#End If
    Public SimulationResult As Double
    Public BestSimulationResult As Double = [Double].MinValue
    Public PCRenewWeeklyLearn As Boolean = False
    Private IdleTimeChecker As PerformanceCounter
    Public TARGET_CPU_LOAD As Single = 0.62
    Public CPU_LOAD_SENSITIVITY As Single = 0.01

    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        AccelValue = trb_Accel.Value
#If MOVING_AVERAGE_DIFFERENCE Then
        DBSupporter = New c042_ChartDBSupport()       'DB supporter 생성
#Else
        DBSupporter = New c041_DefaultDBSupport()       'DB supporter 생성
#End If
        'If Not DBSupporter.DBStatusOK Then
        'MsgBox("DB 설정에 문제가 있어서 프로그램을 종료합니다.")
        '프로그램 종료
        'Me.Close()
        'Exit Sub
        'End If
        'tm_1mClock.Start()          'start 1minute timer
        'StockMst2Obj.SetInputValue(0, tb_StockCode.Text)        '종목 코드 세팅
        GlobalVarInit(Me)                   '전역변수 초기화

        'load past price information
        Dim file_list = My.Computer.FileSystem.GetFiles(".")
        Dim file_extension As String
        Dim file_name As String
        'Dim symbol_name As String
        Dim file_contents As List(Of String)
        For index As Integer = 0 To file_list.Count - 1
            file_extension = IO.Path.GetExtension(file_list(index))
            If file_extension = ".txt" Then
                file_name = IO.Path.GetFileName(file_list(index))
                If file_name.Substring(0, 9) = "history1_" Then
                    If file_name(9) = "A" Then
                        'no_response => 상장폐지 종목
                    Else
                        '종목 나왔다.
                        file_contents = IO.File.ReadAllLines(file_list(index)).ToList()
                        For line_index As Integer = 0 To file_contents.Count - 1

                        Next
                    End If
                End If
            End If

        Next
        'If MsgBox("실시간 모드로 할래요? (No는 시뮬레이션 모드)", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
        '실시간 모드
        'RealTimeMode = True
        'SymbolCollection()                      '종목 알아내기
        'tm_15sClock_Tick(Nothing, Nothing)       'Get the very first data
        'tm_15sClock.Start()                     '15초 타이머 돌리기
        'TabControl1.SelectedTab = TabControl1.TabPages(1)
        'Else
        '시뮬레이션 모드
        'RealTimeMode = False
        TabControl1.SelectedTab = TabControl1.TabPages(2)
#If PCRENEW_LEARN Or DOUBLE_FALL Or SHARP_LINES Then
        '        If DecisionByPattern Then
        '패턴용 트렌드 모니터용 칼럼 생성
        Dim new_column As ColumnHeader
        If GangdoDB Then
#If 0 Then
                new_column = New ColumnHeader()
                new_column.Tag = "f"
                new_column.Text = "BuyAmountCenter"
                new_column.Width = 60
                lv_DoneDecisions.Columns.Add(new_column)

                new_column = New ColumnHeader()
                new_column.Tag = "f"
                new_column.Text = "SelAmountCenter"
                new_column.Width = 60
                lv_DoneDecisions.Columns.Add(new_column)
#End If
            new_column = New ColumnHeader()
            new_column.Tag = "f"
            new_column.Text = "PatternGangdo"
            new_column.Width = 100
            lv_DoneDecisions.Columns.Add(new_column)
        End If

        For index As Integer = 1 To MAX_HAVING_LENGTH
            new_column = New ColumnHeader()
            new_column.Tag = "f"
            new_column.Text = "Trend" & index.ToString("##")
            new_column.Width = 60
            lv_DoneDecisions.Columns.Add(new_column)
        Next
        '        End If
#End If
        'End If

        'Form update 타이머 기동
        tm_FormUpdate.Start()       '관리 timer 시작

        'allow multiple entering 변수 동기화
        'cb_AllowMultipleEntering.Checked = AllowMultipleEntering

        ' Create a PerformanceCounter instance to monitor CPU usage
        IdleTimeChecker = New PerformanceCounter("Processor", "% Idle Time", "_Total")

        ' Start the counter
        IdleTimeChecker.NextValue()
    End Sub

#If 0 Then
    Private Sub SymbolCollection()
        Dim cp_stock_code_obj As New CpStockCode
        Dim cp_code_mgr_obj As New CpCodeMgr
        Dim number_of_symbols As Integer = cp_stock_code_obj.GetCount
        Dim symbol_code As String
        Dim symbol_name As String
        Dim market_kind As Integer
        Dim control_kind As Integer
        Dim supervision_kind As Integer
        Dim status_kind As Integer
        Dim last_end_price As Long
        Dim list_view_item As ListViewItem
        Dim yester_price As Long
        Dim yester_amount As ULong
        Dim volume As Long
        Dim init_price As UInt32
        '        Dim amount As UInt64
        '        Dim gangdo As Double
        Dim symbol_obj As c03_Symbol = Nothing
        Dim bunch_obj As c02_Bunch = Nothing
        Dim selected_bunch_obj As c02_Bunch = Nothing

        'SymTree.StartSymbolListing()            '종목 모으기 시작
        ProgressText1 = "Symbol Collection step - "
        ProgressText2 = "0 / " & cp_stock_code_obj.GetCount
        ProgressText3 = ""
        For index As Integer = 0 To cp_stock_code_obj.GetCount - 1
            symbol_code = cp_stock_code_obj.GetData(0, index)
            symbol_name = cp_stock_code_obj.GetData(1, index)

            symbol_obj = New c03_Symbol(symbol_code, symbol_name)    '종목 개체 생성
            SymbolList.Add(symbol_obj)                                     '종목리스트에 추가
            market_kind = cp_code_mgr_obj.GetStockMarketKind(symbol_code)
            symbol_obj.MarketKind = market_kind         '마켓 종류 : 1: KOSPI, 2: KOSDAQ
            '130604: 대신증권 도움말에 종목별 증거금(100%,75%,50% 등) 알아내는 법을 알아보자. 근데 도움말 어떻게 읽어.
            symbol_obj.EvidanRate = cp_code_mgr_obj.GetStockMarginRate(symbol_code)     '증거금률 읽어오기
            control_kind = cp_code_mgr_obj.GetStockControlKind(symbol_code)
            supervision_kind = cp_code_mgr_obj.GetStockSupervisionKind(symbol_code)
            status_kind = cp_code_mgr_obj.GetStockStatusKind(symbol_code)
            last_end_price = cp_code_mgr_obj.GetStockYdClosePrice(symbol_code)

            If market_kind = 1 Or market_kind = 2 Then
                '소속부가 거래소 또는 코스닥이면
                If control_kind = 0 Or control_kind = 1 Then
                    '감리구분이 정상 또는 주의 이면
                    If supervision_kind = 0 Then
                        '관리구분이 일반종목이면
                        If status_kind = 0 Then
                            '주식상태가 거래정지나 거래중단이 아니면

                            If bunch_obj Is Nothing Then
                                '아직 번치가 생성되지 않았으면 하나 만든다
                                bunch_obj = New c02_Bunch(symbol_obj)
                            Else
                                '생성되어 있으면 종목을 덧붙인다.
                                bunch_obj.Add(symbol_obj)
                            End If

                            If bunch_obj.Count = _MAX_NUMBER_OF_REQUEST Then
                                '110개 다 모았으니 request 해야 된다.
                                bunch_obj.SymbolListFix()           '번치의 종목 리스트 마무리
                                bunch_obj.Mst2BlockRequest()        '리퀘스트...

                                For result_count As Integer = 0 To bunch_obj.Count - 1
                                    symbol_code = bunch_obj.GetSymbolCode(result_count)
                                    symbol_name = bunch_obj.GetSymbolName(result_count)
                                    yester_price = bunch_obj.GetYesterdayPrice(result_count)      '전일종가
                                    yester_amount = bunch_obj.GetYesterdayAmount(result_count)    '전일거래량
                                    volume = yester_price * yester_amount                                '전일거대래금

                                    If yester_price < _STOCK_HI_PRICE And volume >= _LO_VOLUME Then
                                        '너무 비싸지 않은 가격에 거래량이 충분히 있는 종목을 고른다.
                                        init_price = bunch_obj.GetNowPrice(result_count)
                                        'amount = bunch_obj.GetAmount(result_count)
                                        'gangdo = bunch_obj.GetGangdo(result_count)
                                        list_view_item = New ListViewItem(symbol_code)
                                        list_view_item.SubItems.Add(symbol_name)
                                        list_view_item.SubItems.Add(yester_price) '.ToString("n"))
                                        list_view_item.SubItems.Add(yester_amount)
                                        list_view_item.SubItems.Add(volume) '.ToString("n"))
                                        list_view_item.SubItems.Add(init_price)      '초기가
                                        list_view_item.SubItems.Add(0)         '현재가
                                        list_view_item.SubItems.Add(0)     '현재거래량
                                        'list_view_item.SubItems.Add(0)      '현재 체결강도
                                        list_view_item.SubItems.Add(0)          'data 갯수
                                        list_view_item.SubItems.Add(0)          '이동평균
                                        list_view_item.SubItems.Add(0)          '델타매수량
                                        list_view_item.SubItems.Add(0)          '델타매도량
                                        lv_Symbols.Items.Add(list_view_item)

                                        bunch_obj.Item(result_count).Initialize()           '해당 종목 주가 모니터링 시작
                                        bunch_obj.Item(result_count).LVItem = list_view_item    '리스트뷰아이템 설정
                                        '아래에서 첫 데이터를 넘겨준다.
                                        'bunch_obj.Item(result_count).SetNewData(now_price, amount, gangdo)
                                        'SymTree.AddSymbol(bunch_obj.Item(result_count))       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
                                    Else
                                        '선택되지 않은 종목은 초기 가격정보를 지운다
                                        bunch_obj.Item(result_count).Initialize()
                                    End If

                                Next

                                MessageLogging(SymTree.Count.ToString & " 번치 끝")
                                '다음 번치를 위한 준비과정
                                bunch_obj.Terminate()
                                bunch_obj = Nothing
                            End If
                        End If
                    End If
                End If

            End If
            ProgressText2 = index & " / " & cp_stock_code_obj.GetCount
        Next

        If bunch_obj IsNot Nothing AndAlso bunch_obj.Count > 0 Then
            '110개 안 모였어도 request 해야 된다.
            bunch_obj.SymbolListFix()           '번치의 종목 리스트 마무리
            bunch_obj.Mst2BlockRequest()        '리퀘스트...

            For result_count As Integer = 0 To bunch_obj.Count - 1
                symbol_code = bunch_obj.GetSymbolCode(result_count)
                symbol_name = bunch_obj.GetSymbolName(result_count)
                yester_price = bunch_obj.GetYesterdayPrice(result_count)      '전일종가
                yester_amount = bunch_obj.GetYesterdayAmount(result_count)    '전일거래량
                volume = yester_price * yester_amount                                '전일거대래금

                If yester_price < _STOCK_HI_PRICE And volume >= _LO_VOLUME Then
                    '너무 비싸지 않은 가격에 거래량이 충분히 있는 종목을 고른다.
                    init_price = bunch_obj.GetNowPrice(result_count)
                    'amount = bunch_obj.GetAmount(result_count)
                    'gangdo = bunch_obj.GetGangdo(result_count)
                    list_view_item = New ListViewItem(symbol_code)
                    list_view_item.SubItems.Add(symbol_name)
                    list_view_item.SubItems.Add(yester_price) '.ToString("n"))
                    list_view_item.SubItems.Add(yester_amount)
                    list_view_item.SubItems.Add(volume) '.ToString("n"))
                    list_view_item.SubItems.Add(init_price)      '초기가
                    list_view_item.SubItems.Add(0)         '현재가
                    list_view_item.SubItems.Add(0)     '현재거래량
                    'list_view_item.SubItems.Add(0)      '현재 체결강도
                    list_view_item.SubItems.Add(0)          'data 갯수
                    list_view_item.SubItems.Add(0)          '이동평균
                    list_view_item.SubItems.Add(0)          '델타매수량
                    list_view_item.SubItems.Add(0)          '델타매도량
                    lv_Symbols.Items.Add(list_view_item)

                    bunch_obj.Item(result_count).Initialize()           '해당 종목 주가 모니터링 시작
                    bunch_obj.Item(result_count).LVItem = list_view_item    '리스트뷰아이템 설정
                    '아래에서 첫 데이터를 넘겨준다.
                    'bunch_obj.Item(result_count).SetNewData(now_price, amount, gangdo)
                    'SymTree.AddSymbol(bunch_obj.Item(result_count))       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
                End If

            Next

            MessageLogging(SymTree.Count.ToString & " 마지막 번치 끝")
            '번치 클리어
            bunch_obj.Terminate()
            bunch_obj = Nothing
        Else
            '번치 클리어 할 필요 없다.
        End If

        'SymTree.FinishSymbolListing()           '종목 모으기 끝

        '종목리스트에 대해 DB 상태 정보 업데이트 =>나중에 저장할 때 한다.
        'DBSupporter.UpdateDBStatus()
        ProgressText1 = "Number of symbols : " & number_of_symbols_in_interest & " / " & number_of_symbols & ", Number of bunches: " & SymTree.Count
        'Text = lv_Symbols.Items.Count
    End Sub
#End If

    '131014 : 15초 timer를 5초 timer로 바꾸는 작업 개시.
    '5초마다 종목시세 업데이트
    'Private Sub tm_PriceClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stm_PriceClock.Elapsed
    'If IsMarketTime Then
    '장중이면
    'SymTree.ClockSupply()           '시세 업데이트 클락 공급
    'End If
    'End Sub

    '시뮬레이션 시작
    Private Sub bt_StartSimul_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bt_StartSimul.Click
        Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf SimulateThread)
        simulate_thread.IsBackground = True
        simulate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        'tm_FormUpdate.Start()       '관리 timer 시작
    End Sub

    '넘겨받은 숫자에서 set된 bit 의 갯수를 돌려준다..
    Private Function SetBitCount(ByVal the_number As Integer) As Integer
        Dim mask As Integer
        Dim bit_count As Integer = 0

        For index As Integer = 0 To NUMBER_OF_COEFFS - 1
            mask = 2 ^ index
            If mask And the_number Then
                bit_count += 1
            End If
        Next

        Return bit_count
    End Function

    '조합 (Combination) 계산
    Private Function Combination(ByVal a As Integer, ByVal b As Integer)
        Dim result As Integer = 1

        For index As Integer = 1 To b
            result = result * (a + 1 - index)
        Next

        For index As Integer = b To 2 Step -1
            result = result / index
        Next

        Return result
    End Function

    '시뮬레이션 스레드용
    Private Sub SimulateThread()
        SimulationStarted = True
        If MixedLearning Then
            '20230901: 처음에는 DEFAULT_HAVING_TIME 1부터 6까지 TestArray 로 돌려서 학습하고, 다음에 SmartLearn 을 돌려 미세하게 학습한다.

            Dim simulation_result_list As New List(Of Double)

#If PCRENEW_LEARN Then
            SmartLearning = False   '스마트 러닝 오프
            '초기 파라미터 세팅
            'Form_SCORE_THRESHOLD = 0.064338
            'Form_FALL_SCALE_LOWER_THRESHOLD = 1.072375

            'DEFAULT_HAVING_TIME 1~6 순서대로 돌리기
            '2023.11.25: 1~3은 걸리는 애들이 많은 구역으로 2개월 단위 학습, 4~6는 걸리는 애들 별로 없는 구역으로 5개월 단위 학습
            For index As Integer = 1 To 6
                '시뮬레이션 결과 초기화
                simulation_result_list.Clear()
                'TestArray 설정, 시뮬레이션 기간 설정
                If WeeklyLearning Then
                    'weekly learning 이면 1,5,6 은 길게 갈 수도 있어서 띄엄띄엄인 TestArray_a 로 가고 2,3,4 는 보통 짧으니까 TestArray_b 로 간다.
                    '시뮬레이션 기간 : 1,5,6 은 샘플수가 별로 없어서 4달은 해야 된다. 2,3,4 는 샘플수가 많아서 2달만 하면 된다.

                    Select Case index
                        Case 1
                            TestArray = TestArray_a
                            SimulStartDate = SimulEndDate - TimeSpan.FromDays(120)
                        Case 2
                            TestArray = TestArray_b
                            SimulStartDate = SimulEndDate - TimeSpan.FromDays(60)
                        Case 3
                            TestArray = TestArray_b
                            SimulStartDate = SimulEndDate - TimeSpan.FromDays(60)
                        Case 4
                            TestArray = TestArray_b
                            SimulStartDate = SimulEndDate - TimeSpan.FromDays(60)
                        Case 5
                            TestArray = TestArray_a
                            SimulStartDate = SimulEndDate - TimeSpan.FromDays(120)
                        Case 6
                            TestArray = TestArray_a
                            SimulStartDate = SimulEndDate - TimeSpan.FromDays(120)
                    End Select
                Else
                    'Weekly Learning 아니면 TestArray 는 하드코딩으로 설정한 대로 가고, 시뮬레이션 기간은 컨트롤에서 설정한대로 간다.
                End If
                For innerIndex As Integer = 0 To TestArray.Length - 1
                    Select Case index
                        Case 1
                            Form_DEFAULT_HAVING_TIME1 = TestArray(innerIndex)
                        Case 2
                            Form_DEFAULT_HAVING_TIME2 = TestArray(innerIndex)
                        Case 3
                            Form_DEFAULT_HAVING_TIME3 = TestArray(innerIndex)
                        Case 4
                            Form_DEFAULT_HAVING_TIME4 = TestArray(innerIndex)
                        Case 5
                            Form_DEFAULT_HAVING_TIME5 = TestArray(innerIndex)
                        Case 6
                            Form_DEFAULT_HAVING_TIME6 = TestArray(innerIndex)
                    End Select
                    TestIndex = innerIndex
                    SequentialTestBody()    '시뮬레이션 실행
                    simulation_result_list.Add(SimulationResult)    '시뮬레이션 결과 저장
                Next
                'Best 결과를 찾는다.
                Dim max_result As Double = Double.MinValue
                Dim max_index As Integer = -1
                For result_index As Integer = 0 To simulation_result_list.Count - 1
                    If max_result < simulation_result_list(result_index) Then
                        max_result = simulation_result_list(result_index)
                        max_index = result_index
                    End If
                Next
                Select Case index
                    Case 1
                        Form_DEFAULT_HAVING_TIME1 = TestArray(max_index)
                    Case 2
                        Form_DEFAULT_HAVING_TIME2 = TestArray(max_index)
                    Case 3
                        Form_DEFAULT_HAVING_TIME3 = TestArray(max_index)
                    Case 4
                        Form_DEFAULT_HAVING_TIME4 = TestArray(max_index)
                    Case 5
                        Form_DEFAULT_HAVING_TIME5 = TestArray(max_index)
                    Case 6
                        Form_DEFAULT_HAVING_TIME6 = TestArray(max_index)
                End Select
            Next

            '위에서 학습한 대로 BestCoeff 설정
            BestCoeffs(0) = Form_SCORE_THRESHOLD * 1.03         ' 스마트러닝에서 쓸 데 없이 올라갔다가 내려오는 거를 막기 위함
            BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
            BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1
            BestCoeffs(3) = Form_DEFAULT_HAVING_TIME2
            BestCoeffs(4) = Form_DEFAULT_HAVING_TIME3
            BestCoeffs(5) = Form_DEFAULT_HAVING_TIME4
            BestCoeffs(6) = Form_DEFAULT_HAVING_TIME5
            BestCoeffs(7) = Form_DEFAULT_HAVING_TIME6
#End If

            'SmartLearn 돌리기
            SmartLearning = True   '스마트 러닝 온
            If WeeklyLearning Then
                '주간학습일 경우 다시 2달간 학습으로 변경
                SimulStartDate = SimulEndDate - TimeSpan.FromDays(60)
                SimulationDateCollector.Clear()     '아래 SmartLearn 시작하기 전에 날짜 clear 해주어야 한다.
            End If


            SmartLearn()
        Else
            If SmartLearning Then
#If PCRENEW_LEARN Then
#If LEARN_UMEP Then
                BestCoeffs(0) = Form_SCORE_THRESHOLD * 1.03         ' 스마트러닝에서 쓸 데 없이 올라갔다가 내려오는 거를 막기 위함
                BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
                BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1
                BestCoeffs(3) = Form_TH_ATTENUATION
                BestCoeffs(4) = Form_VOLUME_ATTENUATION
#ElseIf LEARN_UNIHAV Then
                BestCoeffs(0) = Form_SCORE_THRESHOLD * 1.03         ' 스마트러닝에서 쓸 데 없이 올라갔다가 내려오는 거를 막기 위함
                BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
                BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1
#Else
                BestCoeffs(0) = Form_SCORE_THRESHOLD * 1.03         ' 스마트러닝에서 쓸 데 없이 올라갔다가 내려오는 거를 막기 위함
                BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
                BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1
                BestCoeffs(3) = Form_DEFAULT_HAVING_TIME2
                BestCoeffs(4) = Form_DEFAULT_HAVING_TIME3
                BestCoeffs(5) = Form_DEFAULT_HAVING_TIME4
                BestCoeffs(6) = Form_DEFAULT_HAVING_TIME5
                BestCoeffs(7) = Form_DEFAULT_HAVING_TIME6
#End If
#ElseIf DOUBLE_FALL Then
                BestCoeffs(0) = Form_SCORE_THRESHOLD1 * 1.03         ' 스마트러닝에서 쓸 데 없이 올라갔다가 내려오는 거를 막기 위함
#ElseIf SHARP_LINES Then
                ＇BestCoeffs(0) = Form_VERTICAL_SCALE * 1.03         ' 스마트러닝에서 쓸 데 없이 올라갔다가 내려오는 거를 막기 위함
#Else
#End If

#If PCRENEW_LEARN Then
#If LEARN_UMEP Then
                SmartLearn_UMEP()
#ElseIf LEARN_UNIHAV Then
                SmartLearn_UNIHAV()
#Else
                SmartLearn()
#End If
#Else
                SmartLearn()
#End If
            ElseIf LearnApplyRepeat Then
                LARTest()
            Else
                SequentialTest()
            End If
        End If
        SimulationStarted = False
    End Sub

    Public Sub SmartLearn()
        'Coefficient 초기값 세팅
#If PCRENEW_LEARN Or DOUBLE_FALL Then
        Dim ALTER_FACTOR0 As Double = 0.03
        Dim ALTER_FACTOR1 As Double = 0.03
        Dim ALTER_FACTOR2 As Double = 0.03
        Dim ALTER_FACTOR3 As Double = 0.03
        Dim ALTER_FACTOR4 As Double = 0.03
        Dim ALTER_FACTOR5 As Double = 0.1
        Dim ALTER_FACTOR6 As Double = 0.03
#ElseIf SHARP_LINES Then
        Dim ALTER_FACTOR0 As Double = 0.2
        Dim ALTER_FACTOR1 As Double = 0.2
        Dim ALTER_FACTOR2 As Double = 0.6
        Dim ALTER_FACTOR3 As Double = 1
        Dim ALTER_FACTOR4 As Double = 0.6
        Dim ALTER_FACTOR5 As Double = 0.2
        Dim ALTER_FACTOR6 As Double = 0.2

        'Dim ALTER_FACTOR0 As Double = 0.1
        'Dim ALTER_FACTOR1 As Double = 0.1
        'Dim ALTER_FACTOR2 As Double = 0.3
        'Dim ALTER_FACTOR3 As Double = 0.5
        'Dim ALTER_FACTOR4 As Double = 0.3
        'Dim ALTER_FACTOR5 As Double = 0.1
        'Dim ALTER_FACTOR6 As Double = 0.1

        ＇Dim ALTER_FACTOR0 As Double = 0.03
        ＇Dim ALTER_FACTOR1 As Double = 0.03
        ＇Dim ALTER_FACTOR2 As Double = 0.1
        ＇Dim ALTER_FACTOR3 As Double = 0.15
        ＇Dim ALTER_FACTOR4 As Double = 0.1
        ＇Dim ALTER_FACTOR5 As Double = 0.03
        ＇Dim ALTER_FACTOR6 As Double = 0.03
#End If
        'Dim ALTER_FACTOR7 As Double = 0.03
        Dim temp_coeffs() As Double
        Dim trial_indicator(2 ^ NUMBER_OF_COEFFS - 1) As Integer
#If PCRENEW_LEARN Then
        Dim number_of_coeffs_in_trial As Integer = 1
#ElseIf DOUBLE_FALL Then
        Dim number_of_coeffs_in_trial As Integer = 1
#ElseIf SHARP_LINES Then
        Dim number_of_coeffs_in_trial As Integer = 1
#End If
        Dim total_number_of_cards As Integer = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
        Dim used_number_of_cards As Integer = 0
        'Dim random_gen As New DetermineLikeRandom      '랜덤같은 determine
        Dim determine_gen As New SequenceDetermine(2 ^ NUMBER_OF_COEFFS - 1)    '차례대로 determine
        Dim the_card As Integer
        Dim coeff0_alter, coeff1_alter, coeff2_alter, coeff3_alter, coeff4_alter, coeff5_alter, coeff6_alter, coeff7_alter As New List(Of Double)
        Dim coeffs_alter_list As New List(Of Double())
        Dim simulation_result_list As New List(Of Double)

        trial_indicator(0) = 1 '카드 0번은 아무 변화를 안 주겠다는 뜻이니까 시도할 필요도 없다.
        While 1
            '각 coefficient 업데이트
            'random generation 을 통해 변화를 줄 coefficient를 고른다.
            'the_card = Math.Floor(random_gen.NextDouble() * (2 ^ NUMBER_OF_COEFFS))    '랜덤같은 deterministic
            the_card = determine_gen.NextNumber()  '차례대로 deterministic
            '뽑은 카드에 써진 coefficients 의 갯수가 number_of_coeffs_in_trial 과 일치하는지 본다. 일치하면 바로 밑으로 빠지고 아니면 다시 카드 뽑는다.
            While (SetBitCount(the_card) <> number_of_coeffs_in_trial) Or (trial_indicator(the_card) = 1)
                'the_card = Math.Floor(random_gen.NextDouble() * (2 ^ NUMBER_OF_COEFFS))    '랜덤같은 deterministic
                the_card = determine_gen.NextNumber()   '차례대로 deterministic
            End While
            trial_indicator(the_card) = 1
            used_number_of_cards += 1
            '2021.09.11: MAX HAVING TIME 5개 coefficient 는 서로 독립적이기 때문에 단독변이가 아니면 굳이 여러개 묶어서 변이를 줄 필요가 없기 때문이다. 이러면 학습시간을 아낄 수 있을 것이다.
            'If ((number_of_coeffs_in_trial <> 1) AndAlso ((the_card And &H3) = &H0)) Then
            'Continue While
            'End If
#If PCRENEW_LEARN Then
            '2021.09.25: MAX HAVING TIME 5개 coefficient 중 2개 이상이 set 되어 있으면 skip 한다.
            'SKIP조건
#If UNWANTED_RISING_DETECTION Then
                If (SetBitCount(the_card And &HC) >= 2) Then ' Or (SetBitCount(the_card And &HF) > 0) Then    '2022.01.18: 뒤에 and &HF 하는 부분은 unwanted rising detect 하는 부분 외에 다른 부분을 학습에서 제외시키고 싶을 때 enable함
#Else
            'If (SetBitCount(the_card And &H1) > 0) Or (SetBitCount(the_card And &H7C) > 1) Then    '첫째 아규먼트(score threshld) 학습 안 하기
            If ((SetBitCount(the_card And &H3) = 0) AndAlso (SetBitCount(the_card And &HFC) > 1)) _ 'Then        '다른 비트 없이 having time 관려 비트들만 두개 이상 셋되어 있다면 학습 안 함.
                 Or (the_card And &HC4) > 0 Then    '2023.11.26 : 걸린 애들이 충분하지 않은 범위에 있는 영역 1,5,6 은 학습하지 않는다.
#End If
#ElseIf DOUBLE_FALL Then
            If 0 Then
#ElseIf SHARP_LINES Then
            If 0 Then
#Else
#End If
                If total_number_of_cards = used_number_of_cards Then
                    '현재 하고 있는 변이 coefficients 갯수에서는 할 걸 다 해봤다. => 변이 coefficients 갯수를 증가시킨다.
                    If number_of_coeffs_in_trial = NUMBER_OF_COEFFS Then
                        '더 이상 해볼 게 없다. => Give up
                        Exit While
                    Else
                        number_of_coeffs_in_trial += 1
                        total_number_of_cards = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
                        used_number_of_cards = 0
                    End If
                Else
                    '현재 하고 있는 변이 coefficients 갯수에서 아직 할 게 남아 있다. => 다음 카드 뽑으러 간다.
                End If
                Continue While
            End If

            '뽑힌 카드로부터 시도할 coefficients list 를 도출해낼 것이다.
            '우선 각 coefficient 의 변이 list를 도출해낸다.
            coeff0_alter.Clear()
            If (the_card And 1) Then
                'If CType(BestCoeffs(0), UInt32) = CType(BestCoeffs(0) * (1 - ALTER_FACTOR0), UInt32) Then
                'If BestCoeffs(0) = 1 Then
                'coeff0_alter.Add(BestCoeffs(0))
                'Else
                'coeff0_alter.Add(BestCoeffs(0) - 1)
                'End If
                'coeff0_alter.Add(BestCoeffs(0) + 1)
                'Else
                coeff0_alter.Add(BestCoeffs(0) / (1 + ALTER_FACTOR0 / number_of_coeffs_in_trial))
                coeff0_alter.Add(BestCoeffs(0) * (1 + ALTER_FACTOR0 / number_of_coeffs_in_trial))
                'End If
            Else
                coeff0_alter.Add(BestCoeffs(0))
            End If
            coeff1_alter.Clear()
            If (the_card And 2) Then
                'If CType(BestCoeffs(1), UInt32) = CType(BestCoeffs(1) * (1 - ALTER_FACTOR1), UInt32) Then
                'coeff1_alter.Add(BestCoeffs(1) - 2)
                'coeff1_alter.Add(BestCoeffs(1) + 2)
                'Else
#If PCRENEW_LEARN Then
                coeff1_alter.Add((BestCoeffs(1) - 1) / (1 + ALTER_FACTOR1 / number_of_coeffs_in_trial) + 1)
                coeff1_alter.Add((BestCoeffs(1) - 1) * (1 + ALTER_FACTOR1 / number_of_coeffs_in_trial) + 1)
#ElseIf DOUBLE_FALL Then
                coeff1_alter.Add((BestCoeffs(1) - 1) / (1 + ALTER_FACTOR1 / number_of_coeffs_in_trial) + 1)
                coeff1_alter.Add((BestCoeffs(1) - 1) * (1 + ALTER_FACTOR1 / number_of_coeffs_in_trial) + 1)
#ElseIf SHARP_LINES Then
                coeff1_alter.Add(BestCoeffs(1) / (1 + ALTER_FACTOR1 / number_of_coeffs_in_trial))
                coeff1_alter.Add(BestCoeffs(1) * (1 + ALTER_FACTOR1 / number_of_coeffs_in_trial))
#End If
                'End If
            Else
                coeff1_alter.Add(BestCoeffs(1))
            End If
            coeff2_alter.Clear()
            If (the_card And 4) Then
                'If CType(BestCoeffs(2), UInt32) = CType(BestCoeffs(2) * (1 - ALTER_FACTOR2), UInt32) Then
#If PCRENEW_LEARN Then
                coeff2_alter.Add(BestCoeffs(2) - 2)
                coeff2_alter.Add(BestCoeffs(2) + 2)
#ElseIf DOUBLE_FALL Then
                coeff2_alter.Add(BestCoeffs(2) / (1 + ALTER_FACTOR2 / number_of_coeffs_in_trial))
                coeff2_alter.Add(BestCoeffs(2) * (1 + ALTER_FACTOR2 / number_of_coeffs_in_trial))
#ElseIf SHARP_LINES Then
                coeff2_alter.Add(BestCoeffs(2) / (1 + ALTER_FACTOR2 / number_of_coeffs_in_trial))
                coeff2_alter.Add(BestCoeffs(2) * (1 + ALTER_FACTOR2 / number_of_coeffs_in_trial))
#Else
#End If
                'Else
                'coeff2_alter.Add(BestCoeffs(2) * (1 - ALTER_FACTOR2))
                'coeff2_alter.Add(BestCoeffs(2) * (1 + ALTER_FACTOR2))
                'End If
            Else
                coeff2_alter.Add(BestCoeffs(2))
            End If
            coeff3_alter.Clear()
            If (the_card And 8) Then
                'If CType(BestCoeffs(3), UInt32) = CType(BestCoeffs(3) * (1 - ALTER_FACTOR3), UInt32) Then
#If PCRENEW_LEARN Then
                coeff3_alter.Add(BestCoeffs(3) - 2)
                coeff3_alter.Add(BestCoeffs(3) + 2)
#ElseIf DOUBLE_FALL Then
                coeff3_alter.Add((BestCoeffs(3) - 1) / (1 + ALTER_FACTOR3 / number_of_coeffs_in_trial) + 1)
                coeff3_alter.Add((BestCoeffs(3) - 1) * (1 + ALTER_FACTOR3 / number_of_coeffs_in_trial) + 1)
#ElseIf SHARP_LINES Then
                coeff3_alter.Add(BestCoeffs(3) / (1 + ALTER_FACTOR3 / number_of_coeffs_in_trial))
                coeff3_alter.Add(BestCoeffs(3) * (1 + ALTER_FACTOR3 / number_of_coeffs_in_trial))
#Else
#End If
                'Else
                'coeff3_alter.Add(BestCoeffs(3) * (1 - ALTER_FACTOR3))
                'coeff3_alter.Add(BestCoeffs(3) * (1 + ALTER_FACTOR3))
                'End If
            Else
                coeff3_alter.Add(BestCoeffs(3))
            End If
            coeff4_alter.Clear()
            If (the_card And 16) Then
                'If CType(BestCoeffs(4), UInt32) = CType(BestCoeffs(4) * (1 - ALTER_FACTOR4), UInt32) Then
#If PCRENEW_LEARN Or DOUBLE_FALL Then
                coeff4_alter.Add(BestCoeffs(4) - 2)
                coeff4_alter.Add(BestCoeffs(4) + 2)
#ElseIf SHARP_LINES Then
                coeff4_alter.Add(BestCoeffs(4) / (1 + ALTER_FACTOR4 / number_of_coeffs_in_trial))
                coeff4_alter.Add(BestCoeffs(4) * (1 + ALTER_FACTOR4 / number_of_coeffs_in_trial))
#End If
                'coeff4_alter.Add(Math.Max(BestCoeffs(4) - 8, 3))
                'coeff4_alter.Add(BestCoeffs(4) + 8)
                'Else
                'coeff4_alter.Add(BestCoeffs(4) * (1 - ALTER_FACTOR4))
                'coeff4_alter.Add(BestCoeffs(4) * (1 + ALTER_FACTOR4))
                'End If
            Else
                coeff4_alter.Add(BestCoeffs(4))
            End If
            coeff5_alter.Clear()
            If (the_card And 32) Then
                'If CType(BestCoeffs(5), UInt32) = CType(BestCoeffs(5) * (1 - ALTER_FACTOR5), UInt32) Then
#If PCRENEW_LEARN Or DOUBLE_FALL Then
                coeff5_alter.Add(BestCoeffs(5) - 2)
                coeff5_alter.Add(BestCoeffs(5) + 2)
#ElseIf SHARP_LINES Then
                coeff5_alter.Add(BestCoeffs(5) / (1 + ALTER_FACTOR5 / number_of_coeffs_in_trial))
                coeff5_alter.Add(BestCoeffs(5) * (1 + ALTER_FACTOR5 / number_of_coeffs_in_trial))
#End If
                'Else
                'coeff5_alter.Add(BestCoeffs(5) * (1 - ALTER_FACTOR5))
                'coeff5_alter.Add(BestCoeffs(5) * (1 + ALTER_FACTOR5))
                'End If
            Else
                coeff5_alter.Add(BestCoeffs(5))
            End If
#If PCRENEW_LEARN Or SHARP_LINES Then
            coeff6_alter.Clear()
            If (the_card And 64) Then
#If PCRENEW_LEARN Then
                coeff6_alter.Add(BestCoeffs(6) - 2)
                coeff6_alter.Add(BestCoeffs(6) + 2)
#ElseIf SHARP_LINES Then
                coeff6_alter.Add(BestCoeffs(6) / (1 + ALTER_FACTOR6 / number_of_coeffs_in_trial))
                coeff6_alter.Add(BestCoeffs(6) * (1 + ALTER_FACTOR6 / number_of_coeffs_in_trial))
#End If
                'coeff6_alter.Add(Math.Max(BestCoeffs(6) - 8, 3))
                'coeff6_alter.Add(BestCoeffs(6) + 8)
            Else
                coeff6_alter.Add(BestCoeffs(6))
            End If
#End If
#If PCRENEW_LEARN Then
            coeff7_alter.Clear()
            If (the_card And 128) Then
                coeff7_alter.Add(BestCoeffs(7) - 2)
                coeff7_alter.Add(BestCoeffs(7) + 2)
                'coeff7_alter.Add(Math.Max(BestCoeffs(7) - 8, 3))
                'coeff7_alter.Add(BestCoeffs(7) + 8)
            Else
                coeff7_alter.Add(BestCoeffs(7))
            End If
#End If
#If 0 Then
                coeff7_alter.Clear()
                If (the_card And 128) Then
                    coeff7_alter.Add(BestCoeffs(7) * (1 - ALTER_FACTOR7))
                    coeff7_alter.Add(BestCoeffs(7) * (1 + ALTER_FACTOR7))
                Else
                    coeff7_alter.Add(BestCoeffs(7))
                End If
#End If
            coeffs_alter_list.Clear()
            simulation_result_list.Clear()
            For index0 As Integer = 0 To coeff0_alter.Count - 1
                For index1 As Integer = 0 To coeff1_alter.Count - 1
                    For index2 As Integer = 0 To coeff2_alter.Count - 1
                        For index3 As Integer = 0 To coeff3_alter.Count - 1
                            For index4 As Integer = 0 To coeff4_alter.Count - 1
                                For index5 As Integer = 0 To coeff5_alter.Count - 1
#If PCRENEW_LEARN Then
                                    For index6 As Integer = 0 To coeff6_alter.Count - 1
                                        For index7 As Integer = 0 To coeff7_alter.Count - 1
                                            temp_coeffs = {coeff0_alter(index0), coeff1_alter(index1), coeff2_alter(index2), coeff3_alter(index3), coeff4_alter(index4), coeff5_alter(index5), coeff6_alter(index6), coeff7_alter(index7)}
                                            coeffs_alter_list.Add(temp_coeffs)
                                        Next
                                    Next
#ElseIf DOUBLE_FALL Then
                                    'For index6 As Integer = 0 To coeff6_alter.Count - 1
                                    'For index7 As Integer = 0 To coeff7_alter.Count - 1
                                    temp_coeffs = {coeff0_alter(index0), coeff1_alter(index1), coeff2_alter(index2), coeff3_alter(index3), coeff4_alter(index4), coeff5_alter(index5)} ', coeff6_alter(index6)}
                                    coeffs_alter_list.Add(temp_coeffs)
                                    'Next
                                    'Next
#ElseIf SHARP_LINES Then
                                    For index6 As Integer = 0 To coeff6_alter.Count - 1
                                        temp_coeffs = {coeff0_alter(index0), coeff1_alter(index1), coeff2_alter(index2), coeff3_alter(index3), coeff4_alter(index4), coeff5_alter(index5), coeff6_alter(index6)}
                                        coeffs_alter_list.Add(temp_coeffs)
                                    Next
#Else
#End If
                                Next
                            Next
                        Next
                    Next
                Next
            Next

            '만든 coefficients list를 가지고 돌려본다.
            TestIndex = 0
            CoefficientsAlterListLenth = coeffs_alter_list.Count
            NumberOfCoeffsInTrial = number_of_coeffs_in_trial
            Do
#If PCRENEW_LEARN Then
                Form_SCORE_THRESHOLD = coeffs_alter_list(TestIndex)(0)
                Form_FALL_SCALE_LOWER_THRESHOLD = coeffs_alter_list(TestIndex)(1)
                Form_DEFAULT_HAVING_TIME1 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME2 = coeffs_alter_list(TestIndex)(3)
                Form_DEFAULT_HAVING_TIME3 = coeffs_alter_list(TestIndex)(4)
                Form_DEFAULT_HAVING_TIME4 = coeffs_alter_list(TestIndex)(5)
                Form_DEFAULT_HAVING_TIME5 = coeffs_alter_list(TestIndex)(6)
                Form_DEFAULT_HAVING_TIME6 = coeffs_alter_list(TestIndex)(7)
#If UNWANTED_RISING_DETECTION Then
                    Form_TIME_DIFF_FOR_RISING_DETECTION = coeffs_alter_list(TestIndex)(4)
                    Form_RISING_SLOPE_THRESHOLD = coeffs_alter_list(TestIndex)(5)
                    Form_ENTERING_PROHIBIT_TIME = coeffs_alter_list(TestIndex)(6)
#End If
#ElseIf DOUBLE_FALL Then
                Form_SCORE_THRESHOLD1 = coeffs_alter_list(TestIndex)(0)
                    Form_FALL_SCALE_LOWER_THRESHOLD1 = coeffs_alter_list(TestIndex)(1)
                    Form_SCORE_THRESHOLD2 = coeffs_alter_list(TestIndex)(2)
                    Form_FALL_SCALE_LOWER_THRESHOLD2 = coeffs_alter_list(TestIndex)(3)
                    Form_MAX_SECONDFALL_WAITING = coeffs_alter_list(TestIndex)(4)
                Form_DEFAULT_HAVING_TIME = coeffs_alter_list(TestIndex)(5)
#ElseIf SHARP_LINES Then
                Form_VERTICAL_SCALE = coeffs_alter_list(TestIndex)(0)
                Form_HOT_HEIGHT = coeffs_alter_list(TestIndex)(1)
                Form_HOT_WIDTH = coeffs_alter_list(TestIndex)(2)
                Form_STANDARD_LENGTH = coeffs_alter_list(TestIndex)(3)
                Form_X_LIMIT = coeffs_alter_list(TestIndex)(4)
                Form_SLOPE1_LIMIT = coeffs_alter_list(TestIndex)(5)
                Form_SLOPE3_LIMIT = coeffs_alter_list(TestIndex)(6)
#Else
#End If

                '171022: 돌리고 나서 화면에 coeffs도 같이 표시해주게 하고, 결과는 simulation_result_list에 저장하자. 그리고 Do loop 빠져나오면
                '171022: 결과중 maximum인 놈을 골라서 개선여부를 확인하자. 개선되면 best_coeffs 를 업데이트하고 같은 방향으로 몇 번 더해본다.
                '171022: 개선 안 되면 밖의 while loop를 도는데 trial indicator 봐서 다 안 될 경우 number_of_coeffs_in_trial 을 증가시킨다.
                '171022: trail indicator가 다 꽉차도록 개선이 없으면 종료한다.

                InvokingDone = False
                MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
                '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While


                IsThreadExecuting = True           'thread 종료 flag reset
                If Not DateRepeat Then
                    DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
                Else
                    'smart learning 에서는 test array에 의한 simulation하지 않는다.
                    'DBSupporter.Simulate()
                End If
                IsThreadExecuting = False           'thread 종료 flag reset
                '150811 : 아래 문 while 걸어서 invoking 종료 확인때까지 대기하는 거 넣자. 그리고 결과 clipboard로 카피하는 거 넣자
                InvokingDone = False
                MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                '좀 쉬어서 소트 정보 확실히 반영되게 하자
                Threading.Thread.Sleep(1000)

                InvokingDone = False
                MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                'copy to clipboard
                InvokingDone = False
                MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
                '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                simulation_result_list.Add(SimulationResult)
                Threading.Thread.Sleep(1000)

                TestIndex += 1
            Loop While TestIndex < coeffs_alter_list.Count

            'Best 결과를 찾는다.
            Dim max_result As Double = Double.MinValue
            Dim max_index As Integer = -1
            For result_index As Integer = 0 To simulation_result_list.Count - 1
                If max_result < simulation_result_list(result_index) Then
                    max_result = simulation_result_list(result_index)
                    max_index = result_index
                End If
            Next

            If max_result > BestSimulationResult Then
                '개선되었다.
                '같은 방향으로 몇 번 더 변이해보기 위해 변이량을 저장해둔다.
                Dim delta(NUMBER_OF_COEFFS - 1) As Double
                For coeff_index As Integer = 0 To NUMBER_OF_COEFFS - 1
                    delta(coeff_index) = coeffs_alter_list(max_index)(coeff_index) - BestCoeffs(coeff_index)
                Next
                'best coefficients와 best simulation result 업데이트
                BestCoeffs = coeffs_alter_list(max_index)
                BestSimulationResult = simulation_result_list(max_index)
                TestIndex = 0
                CoefficientsAlterListLenth = 0
                NumberOfCoeffsInTrial = number_of_coeffs_in_trial
                Do
#If PCRENEW_LEARN Then
                    Form_SCORE_THRESHOLD = BestCoeffs(0) + delta(0)
                    Form_FALL_SCALE_LOWER_THRESHOLD = BestCoeffs(1) + delta(1)
                    Form_DEFAULT_HAVING_TIME1 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME2 = BestCoeffs(3) + delta(3)
                    Form_DEFAULT_HAVING_TIME3 = BestCoeffs(4) + delta(4)
                    Form_DEFAULT_HAVING_TIME4 = BestCoeffs(5) + delta(5)
                    Form_DEFAULT_HAVING_TIME5 = BestCoeffs(6) + delta(6)
                    Form_DEFAULT_HAVING_TIME6 = BestCoeffs(7) + delta(7)
#If UNWANTED_RISING_DETECTION Then
                        Form_TIME_DIFF_FOR_RISING_DETECTION = BestCoeffs(4) + delta(4)
                        Form_RISING_SLOPE_THRESHOLD = BestCoeffs(5) + delta(5)
                        Form_ENTERING_PROHIBIT_TIME = BestCoeffs(6) + delta(6)
#End If
#ElseIf DOUBLE_FALL Then
                    Form_SCORE_THRESHOLD1 = BestCoeffs(0) + delta(0)
                    Form_FALL_SCALE_LOWER_THRESHOLD1 = BestCoeffs(1) + delta(1)
                    Form_SCORE_THRESHOLD2 = BestCoeffs(2) + delta(2)
                    Form_FALL_SCALE_LOWER_THRESHOLD2 = BestCoeffs(3) + delta(3)
                    Form_MAX_SECONDFALL_WAITING = BestCoeffs(4) + delta(4)
                    Form_DEFAULT_HAVING_TIME = BestCoeffs(5) + delta(5)
#ElseIf SHARP_LINES Then
                    Form_VERTICAL_SCALE = BestCoeffs(0) + delta(0)
                    Form_HOT_HEIGHT = BestCoeffs(1) + delta(1)
                    Form_HOT_WIDTH = BestCoeffs(2) + delta(2)
                    Form_STANDARD_LENGTH = BestCoeffs(3) + delta(3)
                    Form_X_LIMIT = BestCoeffs(4) + delta(4)
                    Form_SLOPE1_LIMIT = BestCoeffs(5) + delta(5)
                    Form_SLOPE3_LIMIT = BestCoeffs(6) + delta(6)
#Else

#End If

                    'remove contents
                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    IsThreadExecuting = True           'thread 종료 flag reset
                    If Not DateRepeat Then
                        DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
                    Else
                        'smart learning 에서는 test array에 의한 simulation 하지 않는다.
                        'DBSupporter.Simulate()
                    End If
                    IsThreadExecuting = False           'thread 종료 flag reset

                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    '좀 쉬어서 소트 정보 확실히 반영되게 하자
                    Threading.Thread.Sleep(1000)

                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    'copy to clipboard
                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    simulation_result_list.Add(SimulationResult)
                    Threading.Thread.Sleep(1000)

                    If SimulationResult > BestSimulationResult Then
                        '또 개선되었다. => Do loop 계속 돌리자
#If PCRENEW_LEARN Then
                        BestCoeffs(0) = Form_SCORE_THRESHOLD
                        BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
                        BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1
                        BestCoeffs(3) = Form_DEFAULT_HAVING_TIME2
                        BestCoeffs(4) = Form_DEFAULT_HAVING_TIME3
                        BestCoeffs(5) = Form_DEFAULT_HAVING_TIME4
                        BestCoeffs(6) = Form_DEFAULT_HAVING_TIME5
                        BestCoeffs(7) = Form_DEFAULT_HAVING_TIME6
#If UNWANTED_RISING_DETECTION Then
                            BestCoeffs(4) = Form_TIME_DIFF_FOR_RISING_DETECTION
                            BestCoeffs(5) = Form_RISING_SLOPE_THRESHOLD
                            BestCoeffs(6) = Form_ENTERING_PROHIBIT_TIME
#End If
#ElseIf DOUBLE_FALL Then
                        BestCoeffs(0) = Form_SCORE_THRESHOLD1
                        BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD1
                        BestCoeffs(2) = Form_SCORE_THRESHOLD2
                        BestCoeffs(3) = Form_FALL_SCALE_LOWER_THRESHOLD2
                        BestCoeffs(4) = Form_MAX_SECONDFALL_WAITING
                        BestCoeffs(5) = Form_DEFAULT_HAVING_TIME
#ElseIf SHARP_LINES Then
                        BestCoeffs(0) = Form_VERTICAL_SCALE
                        BestCoeffs(1) = Form_HOT_HEIGHT
                        BestCoeffs(2) = Form_HOT_WIDTH
                        BestCoeffs(3) = Form_STANDARD_LENGTH
                        BestCoeffs(4) = Form_X_LIMIT
                        BestCoeffs(5) = Form_SLOPE1_LIMIT
                        BestCoeffs(6) = Form_SLOPE3_LIMIT
#Else

#End If

                        BestSimulationResult = SimulationResult
                    Else
                        '개선되지 않았다. => 그만 do loop를 빠져나오자.
                        Exit Do
                    End If
                Loop
                '이제 이 개선된 상태에서 다시 1 bit 짜리 변이를 일으켜 돌려보자.
#If PCRENEW_LEARN Then
                If number_of_coeffs_in_trial = 1 AndAlso SetBitCount(the_card And &HFC) = 0 Then
                    '현재 having time 1비트를 하고 있었다면, 굳이 dependency가 없는 다른 비트들까지 리셋할 필요는 없다.
                    Dim remain_number_of_bits As Integer = NUMBER_OF_COEFFS
                    For card_index As Integer = 1 To 2 ^ NUMBER_OF_COEFFS - 1
                        'trial indicator reset 한다. index 0 은 reset 하지 않음에 유의하라.

                        If SetBitCount(card_index) = 1 Then
                            '다른 having time 의 학습에는 영향이 없으므로, 다른 having time bit 1비트짜리는 리셋하지 않는다.
                            If SetBitCount(card_index And &HFC) = 1 AndAlso trial_indicator(card_index) = 1 Then
                                'having time 비트 중 해당 비트를 이미 돌렸다면 리셋하지 않고, 이미 돌린 걸로 표시하기 위해 remain_number_of_bits 를 1 감소시키낟.
                                remain_number_of_bits -= 1
                            Else
                                '그렇지 않으면 리셋한다.
                                trial_indicator(card_index) = 0
                            End If
                        Else
                            trial_indicator(card_index) = 0
                        End If
                    Next
                    number_of_coeffs_in_trial = 1
                    total_number_of_cards = remain_number_of_bits
                    used_number_of_cards = 0
                Else
                    For card_index As Integer = 1 To 2 ^ NUMBER_OF_COEFFS - 1
                        'trial indicator reset 한다. index 0 은 reset 하지 않음에 유의하라.
                        trial_indicator(card_index) = 0
                    Next
                    number_of_coeffs_in_trial = 1
                    total_number_of_cards = NUMBER_OF_COEFFS
                    used_number_of_cards = 0
                End If
#ElseIf DOUBLE_FALL Then
                For card_index As Integer = 1 To 2 ^ NUMBER_OF_COEFFS - 1
                    'trial indicator reset 한다. index 0 은 reset 하지 않음에 유의하라.
                    trial_indicator(card_index) = 0
                Next
                number_of_coeffs_in_trial = 1
                total_number_of_cards = NUMBER_OF_COEFFS
                used_number_of_cards = 0
#ElseIf SHARP_LINES Then
                For card_index As Integer = 1 To 2 ^ NUMBER_OF_COEFFS - 1
                    'trial indicator reset 한다. index 0 은 reset 하지 않음에 유의하라.
                    trial_indicator(card_index) = 0
                Next
                number_of_coeffs_in_trial = 1
                total_number_of_cards = NUMBER_OF_COEFFS
                used_number_of_cards = 0
#Else
#End If
            Else
                '개선 안 되었다.
                If total_number_of_cards = used_number_of_cards Then
                    '현재 하고 있는 변이 coefficients 갯수에서는 할 걸 다 해봤다. => 변이 coefficients 갯수를 증가시킨다.
                    If number_of_coeffs_in_trial = NUMBER_OF_COEFFS Then
                        '더 이상 해볼 게 없다. => Give up
                        Exit While
                    Else
                        number_of_coeffs_in_trial += 1
                        total_number_of_cards = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
                        used_number_of_cards = 0
                    End If
                Else
                    '현재 하고 있는 변이 coefficients 갯수에서 아직 할 게 남아 있다. => 다음 카드 뽑으러 간다.
                End If
            End If
        End While
    End Sub
#If PCRENEW_LEARN Then

    'having time 을 단일화하고 다중진입 관련 두개 파라미터를 함께 학습
    Public Sub SmartLearn_UMEP()
        'multiple entering enable 하고 th_attenuation 과 volume_attenuation 까지 학습해보자.
        NUMBER_OF_COEFFS = 5
        'Coefficient 초기값 세팅
        Dim ALTER_FACTOR0 As Double = 0.03
        Dim ALTER_FACTOR1 As Double = 0.03
        Dim ALTER_FACTOR2 As Double = 0.03
        Dim ALTER_FACTOR3 As Double = 0.2
        Dim ALTER_FACTOR4 As Double = 0.2

        Dim temp_coeffs() As Double
        Dim trial_indicator(2 ^ NUMBER_OF_COEFFS - 1) As Integer
        Dim number_of_coeffs_in_trial As Integer = 1
        Dim total_number_of_cards As Integer = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
        Dim used_number_of_cards As Integer = 0
        'Dim random_gen As New DetermineLikeRandom      '랜덤같은 determine
        Dim determine_gen As New SequenceDetermine(2 ^ NUMBER_OF_COEFFS - 1)    '차례대로 determine
        Dim the_card As Integer
        Dim coeff0_alter, coeff1_alter, coeff2_alter, coeff3_alter, coeff4_alter As New List(Of Double)
        Dim coeffs_alter_list As New List(Of Double())
        Dim simulation_result_list As New List(Of Double)

        trial_indicator(0) = 1 '카드 0번은 아무 변화를 안 주겠다는 뜻이니까 시도할 필요도 없다.
        While 1
            '각 coefficient 업데이트
            'random generation 을 통해 변화를 줄 coefficient를 고른다.
            'the_card = Math.Floor(random_gen.NextDouble() * (2 ^ NUMBER_OF_COEFFS))    '랜덤같은 deterministic
            the_card = determine_gen.NextNumber()  '차례대로 deterministic
            '뽑은 카드에 써진 coefficients 의 갯수가 number_of_coeffs_in_trial 과 일치하는지 본다. 일치하면 바로 밑으로 빠지고 아니면 다시 카드 뽑는다.
            While (SetBitCount(the_card) <> number_of_coeffs_in_trial) Or (trial_indicator(the_card) = 1)
                'the_card = Math.Floor(random_gen.NextDouble() * (2 ^ NUMBER_OF_COEFFS))    '랜덤같은 deterministic
                the_card = determine_gen.NextNumber()   '차례대로 deterministic
            End While
            trial_indicator(the_card) = 1
            used_number_of_cards += 1

#If 0 Then
            'SKIP조건
            If ((SetBitCount(the_card And &H3) = 0) AndAlso (SetBitCount(the_card And &HFC) > 1)) _ 'Then        '다른 비트 없이 having time 관려 비트들만 두개 이상 셋되어 있다면 학습 안 함.
                 Or (the_card And &HC4) > 0 Then    '2023.11.26 : 걸린 애들이 충분하지 않은 범위에 있는 영역 1,5,6 은 학습하지 않는다.
                If total_number_of_cards = used_number_of_cards Then
                    '현재 하고 있는 변이 coefficients 갯수에서는 할 걸 다 해봤다. => 변이 coefficients 갯수를 증가시킨다.
                    If number_of_coeffs_in_trial = NUMBER_OF_COEFFS Then
                        '더 이상 해볼 게 없다. => Give up
                        Exit While
                    Else
                        number_of_coeffs_in_trial += 1
                        total_number_of_cards = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
                        used_number_of_cards = 0
                    End If
                Else
                    '현재 하고 있는 변이 coefficients 갯수에서 아직 할 게 남아 있다. => 다음 카드 뽑으러 간다.
                End If
                Continue While
            End If
#End If

            '뽑힌 카드로부터 시도할 coefficients list 를 도출해낼 것이다.
            '우선 각 coefficient 의 변이 list를 도출해낸다.
            coeff0_alter.Clear()
            If (the_card And 1) Then
                coeff0_alter.Add(BestCoeffs(0) / (1 + ALTER_FACTOR0 / Math.Sqrt(number_of_coeffs_in_trial)))
                coeff0_alter.Add(BestCoeffs(0) * (1 + ALTER_FACTOR0 / Math.Sqrt(number_of_coeffs_in_trial)))
            Else
                coeff0_alter.Add(BestCoeffs(0))
            End If

            coeff1_alter.Clear()
            If (the_card And 2) Then
                coeff1_alter.Add((BestCoeffs(1) - 1) / (1 + ALTER_FACTOR1 / Math.Sqrt(number_of_coeffs_in_trial)) + 1)
                coeff1_alter.Add((BestCoeffs(1) - 1) * (1 + ALTER_FACTOR1 / Math.Sqrt(number_of_coeffs_in_trial)) + 1)
            Else
                coeff1_alter.Add(BestCoeffs(1))
            End If

            coeff2_alter.Clear()
            If (the_card And 4) Then
                coeff2_alter.Add(BestCoeffs(2) - 2)
                coeff2_alter.Add(BestCoeffs(2) + 2)
            Else
                coeff2_alter.Add(BestCoeffs(2))
            End If

            coeff3_alter.Clear()
            If (the_card And 8) Then
                coeff3_alter.Add(BestCoeffs(3) / (1 + ALTER_FACTOR3 / Math.Sqrt(number_of_coeffs_in_trial)))
                coeff3_alter.Add(BestCoeffs(3) * (1 + ALTER_FACTOR3 / Math.Sqrt(number_of_coeffs_in_trial)))
            Else
                coeff3_alter.Add(BestCoeffs(3))
            End If

            coeff4_alter.Clear()
            If (the_card And 16) Then
                coeff4_alter.Add(BestCoeffs(4) / (1 + ALTER_FACTOR4 / Math.Sqrt(number_of_coeffs_in_trial)))
                coeff4_alter.Add(BestCoeffs(4) * (1 + ALTER_FACTOR4 / Math.Sqrt(number_of_coeffs_in_trial)))
            Else
                coeff4_alter.Add(BestCoeffs(4))
            End If

            coeffs_alter_list.Clear()
            simulation_result_list.Clear()
            For index0 As Integer = 0 To coeff0_alter.Count - 1
                For index1 As Integer = 0 To coeff1_alter.Count - 1
                    For index2 As Integer = 0 To coeff2_alter.Count - 1
                        For index3 As Integer = 0 To coeff3_alter.Count - 1
                            For index4 As Integer = 0 To coeff4_alter.Count - 1
                                temp_coeffs = {coeff0_alter(index0), coeff1_alter(index1), coeff2_alter(index2), coeff3_alter(index3), coeff4_alter(index4)}
                                coeffs_alter_list.Add(temp_coeffs)
                            Next
                        Next
                    Next
                Next
            Next

            '만든 coefficients list를 가지고 돌려본다.
            TestIndex = 0
            CoefficientsAlterListLenth = coeffs_alter_list.Count
            NumberOfCoeffsInTrial = number_of_coeffs_in_trial
            Do
                Form_SCORE_THRESHOLD = coeffs_alter_list(TestIndex)(0)
                Form_FALL_SCALE_LOWER_THRESHOLD = coeffs_alter_list(TestIndex)(1)
                Form_DEFAULT_HAVING_TIME1 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME2 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME3 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME4 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME5 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME6 = coeffs_alter_list(TestIndex)(2)
                Form_TH_ATTENUATION = coeffs_alter_list(TestIndex)(3)
                Form_VOLUME_ATTENUATION = coeffs_alter_list(TestIndex)(4)

                '171022: 돌리고 나서 화면에 coeffs도 같이 표시해주게 하고, 결과는 simulation_result_list에 저장하자. 그리고 Do loop 빠져나오면
                '171022: 결과중 maximum인 놈을 골라서 개선여부를 확인하자. 개선되면 best_coeffs 를 업데이트하고 같은 방향으로 몇 번 더해본다.
                '171022: 개선 안 되면 밖의 while loop를 도는데 trial indicator 봐서 다 안 될 경우 number_of_coeffs_in_trial 을 증가시킨다.
                '171022: trail indicator가 다 꽉차도록 개선이 없으면 종료한다.

                InvokingDone = False
                MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
                '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While


                IsThreadExecuting = True           'thread 종료 flag reset
                If Not DateRepeat Then
                    DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
                Else
                    'smart learning 에서는 test array에 의한 simulation하지 않는다.
                    'DBSupporter.Simulate()
                End If
                IsThreadExecuting = False           'thread 종료 flag reset
                '150811 : 아래 문 while 걸어서 invoking 종료 확인때까지 대기하는 거 넣자. 그리고 결과 clipboard로 카피하는 거 넣자
                InvokingDone = False
                MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                '좀 쉬어서 소트 정보 확실히 반영되게 하자
                Threading.Thread.Sleep(1000)

                InvokingDone = False
                MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                'copy to clipboard
                InvokingDone = False
                MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
                '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                simulation_result_list.Add(SimulationResult)
                Threading.Thread.Sleep(1000)

                TestIndex += 1
            Loop While TestIndex < coeffs_alter_list.Count

            'Best 결과를 찾는다.
            Dim max_result As Double = Double.MinValue
            Dim max_index As Integer = -1
            For result_index As Integer = 0 To simulation_result_list.Count - 1
                If max_result < simulation_result_list(result_index) Then
                    max_result = simulation_result_list(result_index)
                    max_index = result_index
                End If
            Next

            If max_result > BestSimulationResult Then
                '개선되었다.
                '같은 방향으로 몇 번 더 변이해보기 위해 변이량을 저장해둔다.
                Dim delta(NUMBER_OF_COEFFS - 1) As Double
                For coeff_index As Integer = 0 To NUMBER_OF_COEFFS - 1
                    delta(coeff_index) = coeffs_alter_list(max_index)(coeff_index) - BestCoeffs(coeff_index)
                Next
                'best coefficients와 best simulation result 업데이트
                BestCoeffs = coeffs_alter_list(max_index)
                BestSimulationResult = simulation_result_list(max_index)
                TestIndex = 0
                CoefficientsAlterListLenth = 0
                NumberOfCoeffsInTrial = number_of_coeffs_in_trial
                Do
                    Form_SCORE_THRESHOLD = BestCoeffs(0) + delta(0)
                    Form_FALL_SCALE_LOWER_THRESHOLD = BestCoeffs(1) + delta(1)
                    Form_DEFAULT_HAVING_TIME1 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME2 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME3 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME4 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME5 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME6 = BestCoeffs(2) + delta(2)
                    Form_TH_ATTENUATION = BestCoeffs(3) + delta(3)
                    Form_VOLUME_ATTENUATION = BestCoeffs(4) + delta(4)

                    'remove contents
                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    IsThreadExecuting = True           'thread 종료 flag reset
                    If Not DateRepeat Then
                        DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
                    Else
                        'smart learning 에서는 test array에 의한 simulation 하지 않는다.
                        'DBSupporter.Simulate()
                    End If
                    IsThreadExecuting = False           'thread 종료 flag reset

                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    '좀 쉬어서 소트 정보 확실히 반영되게 하자
                    Threading.Thread.Sleep(1000)

                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    'copy to clipboard
                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    simulation_result_list.Add(SimulationResult)
                    Threading.Thread.Sleep(1000)

                    If SimulationResult > BestSimulationResult Then
                        '또 개선되었다. => Do loop 계속 돌리자
                        BestCoeffs(0) = Form_SCORE_THRESHOLD
                        BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
                        BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1
                        BestCoeffs(3) = Form_TH_ATTENUATION
                        BestCoeffs(4) = Form_VOLUME_ATTENUATION

                        BestSimulationResult = SimulationResult
                    Else
                        '개선되지 않았다. => 그만 do loop를 빠져나오자.
                        Exit Do
                    End If
                Loop
                '이제 이 개선된 상태에서 다시 1 bit 짜리 변이를 일으켜 돌려보자.
                For card_index As Integer = 1 To 2 ^ NUMBER_OF_COEFFS - 1
                    'trial indicator reset 한다. index 0 은 reset 하지 않음에 유의하라.
                    trial_indicator(card_index) = 0
                Next
                number_of_coeffs_in_trial = 1
                total_number_of_cards = NUMBER_OF_COEFFS
                used_number_of_cards = 0
            Else
                '개선 안 되었다.
                If total_number_of_cards = used_number_of_cards Then
                    '현재 하고 있는 변이 coefficients 갯수에서는 할 걸 다 해봤다. => 변이 coefficients 갯수를 증가시킨다.
                    If number_of_coeffs_in_trial = NUMBER_OF_COEFFS Then
                        '더 이상 해볼 게 없다. => Give up
                        Exit While
                    Else
                        number_of_coeffs_in_trial += 1
                        total_number_of_cards = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
                        used_number_of_cards = 0
                    End If
                Else
                    '현재 하고 있는 변이 coefficients 갯수에서 아직 할 게 남아 있다. => 다음 카드 뽑으러 간다.
                End If
            End If
        End While
    End Sub

    'having time 을 단일화한 학습
    Public Sub SmartLearn_UNIHAV()
        'multiple entering enable 하고 th_attenuation 과 volume_attenuation 까지 학습해보자.
        NUMBER_OF_COEFFS = 3
        'Coefficient 초기값 세팅
        Dim ALTER_FACTOR0 As Double = 0.03
        Dim ALTER_FACTOR1 As Double = 0.03
        Dim ALTER_FACTOR2 As Double = 0.03
        Dim ALTER_FACTOR3 As Double = 0.2
        Dim ALTER_FACTOR4 As Double = 0.2

        Dim temp_coeffs() As Double
        Dim trial_indicator(2 ^ NUMBER_OF_COEFFS - 1) As Integer
        Dim number_of_coeffs_in_trial As Integer = 1
        Dim total_number_of_cards As Integer = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
        Dim used_number_of_cards As Integer = 0
        'Dim random_gen As New DetermineLikeRandom      '랜덤같은 determine
        Dim determine_gen As New SequenceDetermine(2 ^ NUMBER_OF_COEFFS - 1)    '차례대로 determine
        Dim the_card As Integer
        Dim coeff0_alter, coeff1_alter, coeff2_alter, coeff3_alter, coeff4_alter As New List(Of Double)
        Dim coeffs_alter_list As New List(Of Double())
        Dim simulation_result_list As New List(Of Double)

        trial_indicator(0) = 1 '카드 0번은 아무 변화를 안 주겠다는 뜻이니까 시도할 필요도 없다.
        While 1
            '각 coefficient 업데이트
            'random generation 을 통해 변화를 줄 coefficient를 고른다.
            'the_card = Math.Floor(random_gen.NextDouble() * (2 ^ NUMBER_OF_COEFFS))    '랜덤같은 deterministic
            the_card = determine_gen.NextNumber()  '차례대로 deterministic
            '뽑은 카드에 써진 coefficients 의 갯수가 number_of_coeffs_in_trial 과 일치하는지 본다. 일치하면 바로 밑으로 빠지고 아니면 다시 카드 뽑는다.
            While (SetBitCount(the_card) <> number_of_coeffs_in_trial) Or (trial_indicator(the_card) = 1)
                'the_card = Math.Floor(random_gen.NextDouble() * (2 ^ NUMBER_OF_COEFFS))    '랜덤같은 deterministic
                the_card = determine_gen.NextNumber()   '차례대로 deterministic
            End While
            trial_indicator(the_card) = 1
            used_number_of_cards += 1

#If 0 Then
            'SKIP조건
            If ((SetBitCount(the_card And &H3) = 0) AndAlso (SetBitCount(the_card And &HFC) > 1)) _ 'Then        '다른 비트 없이 having time 관려 비트들만 두개 이상 셋되어 있다면 학습 안 함.
                 Or (the_card And &HC4) > 0 Then    '2023.11.26 : 걸린 애들이 충분하지 않은 범위에 있는 영역 1,5,6 은 학습하지 않는다.
                If total_number_of_cards = used_number_of_cards Then
                    '현재 하고 있는 변이 coefficients 갯수에서는 할 걸 다 해봤다. => 변이 coefficients 갯수를 증가시킨다.
                    If number_of_coeffs_in_trial = NUMBER_OF_COEFFS Then
                        '더 이상 해볼 게 없다. => Give up
                        Exit While
                    Else
                        number_of_coeffs_in_trial += 1
                        total_number_of_cards = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
                        used_number_of_cards = 0
                    End If
                Else
                    '현재 하고 있는 변이 coefficients 갯수에서 아직 할 게 남아 있다. => 다음 카드 뽑으러 간다.
                End If
                Continue While
            End If
#End If

            '뽑힌 카드로부터 시도할 coefficients list 를 도출해낼 것이다.
            '우선 각 coefficient 의 변이 list를 도출해낸다.
            coeff0_alter.Clear()
            If (the_card And 1) Then
                coeff0_alter.Add(BestCoeffs(0) / (1 + ALTER_FACTOR0 / Math.Sqrt(number_of_coeffs_in_trial)))
                coeff0_alter.Add(BestCoeffs(0) * (1 + ALTER_FACTOR0 / Math.Sqrt(number_of_coeffs_in_trial)))
            Else
                coeff0_alter.Add(BestCoeffs(0))
            End If

            coeff1_alter.Clear()
            If (the_card And 2) Then
                coeff1_alter.Add((BestCoeffs(1) - 1) / (1 + ALTER_FACTOR1 / Math.Sqrt(number_of_coeffs_in_trial)) + 1)
                coeff1_alter.Add((BestCoeffs(1) - 1) * (1 + ALTER_FACTOR1 / Math.Sqrt(number_of_coeffs_in_trial)) + 1)
            Else
                coeff1_alter.Add(BestCoeffs(1))
            End If

            coeff2_alter.Clear()
            If (the_card And 4) Then
                coeff2_alter.Add(BestCoeffs(2) - 2)
                coeff2_alter.Add(BestCoeffs(2) + 2)
            Else
                coeff2_alter.Add(BestCoeffs(2))
            End If

            coeffs_alter_list.Clear()
            simulation_result_list.Clear()
            For index0 As Integer = 0 To coeff0_alter.Count - 1
                For index1 As Integer = 0 To coeff1_alter.Count - 1
                    For index2 As Integer = 0 To coeff2_alter.Count - 1
                        temp_coeffs = {coeff0_alter(index0), coeff1_alter(index1), coeff2_alter(index2)}
                        coeffs_alter_list.Add(temp_coeffs)
                    Next
                Next
            Next

            '만든 coefficients list를 가지고 돌려본다.
            TestIndex = 0
            CoefficientsAlterListLenth = coeffs_alter_list.Count
            NumberOfCoeffsInTrial = number_of_coeffs_in_trial
            Do
                Form_SCORE_THRESHOLD = coeffs_alter_list(TestIndex)(0)
                Form_FALL_SCALE_LOWER_THRESHOLD = coeffs_alter_list(TestIndex)(1)
                Form_DEFAULT_HAVING_TIME1 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME2 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME3 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME4 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME5 = coeffs_alter_list(TestIndex)(2)
                Form_DEFAULT_HAVING_TIME6 = coeffs_alter_list(TestIndex)(2)

                '171022: 돌리고 나서 화면에 coeffs도 같이 표시해주게 하고, 결과는 simulation_result_list에 저장하자. 그리고 Do loop 빠져나오면
                '171022: 결과중 maximum인 놈을 골라서 개선여부를 확인하자. 개선되면 best_coeffs 를 업데이트하고 같은 방향으로 몇 번 더해본다.
                '171022: 개선 안 되면 밖의 while loop를 도는데 trial indicator 봐서 다 안 될 경우 number_of_coeffs_in_trial 을 증가시킨다.
                '171022: trail indicator가 다 꽉차도록 개선이 없으면 종료한다.

                InvokingDone = False
                MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
                '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While


                IsThreadExecuting = True           'thread 종료 flag reset
                If Not DateRepeat Then
                    DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
                Else
                    'smart learning 에서는 test array에 의한 simulation하지 않는다.
                    'DBSupporter.Simulate()
                End If
                IsThreadExecuting = False           'thread 종료 flag reset
                '150811 : 아래 문 while 걸어서 invoking 종료 확인때까지 대기하는 거 넣자. 그리고 결과 clipboard로 카피하는 거 넣자
                InvokingDone = False
                MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                '좀 쉬어서 소트 정보 확실히 반영되게 하자
                Threading.Thread.Sleep(1000)

                InvokingDone = False
                MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                'copy to clipboard
                InvokingDone = False
                MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
                '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
                While InvokingDone = False
                    'wait until the invoking function finishes
                    Threading.Thread.Sleep(50)
                End While

                simulation_result_list.Add(SimulationResult)
                Threading.Thread.Sleep(1000)

                TestIndex += 1
            Loop While TestIndex < coeffs_alter_list.Count

            'Best 결과를 찾는다.
            Dim max_result As Double = Double.MinValue
            Dim max_index As Integer = -1
            For result_index As Integer = 0 To simulation_result_list.Count - 1
                If max_result < simulation_result_list(result_index) Then
                    max_result = simulation_result_list(result_index)
                    max_index = result_index
                End If
            Next

            If max_result > BestSimulationResult Then
                '개선되었다.
                '같은 방향으로 몇 번 더 변이해보기 위해 변이량을 저장해둔다.
                Dim delta(NUMBER_OF_COEFFS - 1) As Double
                For coeff_index As Integer = 0 To NUMBER_OF_COEFFS - 1
                    delta(coeff_index) = coeffs_alter_list(max_index)(coeff_index) - BestCoeffs(coeff_index)
                Next
                'best coefficients와 best simulation result 업데이트
                BestCoeffs = coeffs_alter_list(max_index)
                BestSimulationResult = simulation_result_list(max_index)
                TestIndex = 0
                CoefficientsAlterListLenth = 0
                NumberOfCoeffsInTrial = number_of_coeffs_in_trial
                Do
                    Form_SCORE_THRESHOLD = BestCoeffs(0) + delta(0)
                    Form_FALL_SCALE_LOWER_THRESHOLD = BestCoeffs(1) + delta(1)
                    Form_DEFAULT_HAVING_TIME1 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME2 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME3 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME4 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME5 = BestCoeffs(2) + delta(2)
                    Form_DEFAULT_HAVING_TIME6 = BestCoeffs(2) + delta(2)

                    'remove contents
                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    IsThreadExecuting = True           'thread 종료 flag reset
                    If Not DateRepeat Then
                        DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
                    Else
                        'smart learning 에서는 test array에 의한 simulation 하지 않는다.
                        'DBSupporter.Simulate()
                    End If
                    IsThreadExecuting = False           'thread 종료 flag reset

                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    '좀 쉬어서 소트 정보 확실히 반영되게 하자
                    Threading.Thread.Sleep(1000)

                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    'copy to clipboard
                    InvokingDone = False
                    MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
                    While InvokingDone = False
                        'wait until the invoking function finishes
                        Threading.Thread.Sleep(50)
                    End While

                    simulation_result_list.Add(SimulationResult)
                    Threading.Thread.Sleep(1000)

                    If SimulationResult > BestSimulationResult Then
                        '또 개선되었다. => Do loop 계속 돌리자
                        BestCoeffs(0) = Form_SCORE_THRESHOLD
                        BestCoeffs(1) = Form_FALL_SCALE_LOWER_THRESHOLD
                        BestCoeffs(2) = Form_DEFAULT_HAVING_TIME1

                        BestSimulationResult = SimulationResult
                    Else
                        '개선되지 않았다. => 그만 do loop를 빠져나오자.
                        Exit Do
                    End If
                Loop
                '이제 이 개선된 상태에서 다시 1 bit 짜리 변이를 일으켜 돌려보자.
                For card_index As Integer = 1 To 2 ^ NUMBER_OF_COEFFS - 1
                    'trial indicator reset 한다. index 0 은 reset 하지 않음에 유의하라.
                    trial_indicator(card_index) = 0
                Next
                number_of_coeffs_in_trial = 1
                total_number_of_cards = NUMBER_OF_COEFFS
                used_number_of_cards = 0
            Else
                '개선 안 되었다.
                If total_number_of_cards = used_number_of_cards Then
                    '현재 하고 있는 변이 coefficients 갯수에서는 할 걸 다 해봤다. => 변이 coefficients 갯수를 증가시킨다.
                    If number_of_coeffs_in_trial = NUMBER_OF_COEFFS Then
                        '더 이상 해볼 게 없다. => Give up
                        Exit While
                    Else
                        number_of_coeffs_in_trial += 1
                        total_number_of_cards = Combination(NUMBER_OF_COEFFS, number_of_coeffs_in_trial)
                        used_number_of_cards = 0
                    End If
                Else
                    '현재 하고 있는 변이 coefficients 갯수에서 아직 할 게 남아 있다. => 다음 카드 뽑으러 간다.
                End If
            End If
        End While
    End Sub

#End If



    'TestArray 처음부터 끝까지 돌림
    Public Sub SequentialTest()
        If DateRepeat Then
            For TestDateIndex = 0 To TestStartDateArray.Count - 1
                For m00_Global.TestIndex = 0 To TestArray.Count - 1
                    SequentialTestBody()
                Next
            Next
        Else
            '150806 : 여기서 아래 DBSupporter.Simulate을 For 문으로 돌리자
            For m00_Global.TestIndex = 0 To TestArray.Count - 1
                SequentialTestBody()
            Next
        End If
    End Sub

    'Learn Apply Repeat 실행
    Public Sub LARTest()
        For TestDateIndex = 0 To TestStartDateArray.Count - 1
            SequentialDateTestBody()
        Next
    End Sub

    Public Sub SequentialTestBody()
        SimulationDateCollector.Clear()     'TestArray가 날짜별로 되어 있는 경우는 clear 안 해주면 날짜가 계속 누적되어 이상해진다.
        InvokingDone = False
        MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf RemoveContents))
        While InvokingDone = False
            'wait until the invoking function finishes
            Threading.Thread.Sleep(50)
        End While

        IsThreadExecuting = True           'thread 종료 flag reset
        If DateRepeat Then
            DBSupporter.Simulate(Convert.ToDateTime(TestStartDateArray(TestDateIndex)), Convert.ToDateTime(TestEndDateArray(TestDateIndex)))
        Else
            DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
        End If
        IsThreadExecuting = False           'thread 종료 flag reset

        'GlobalData 계산
        If TwoStepSearching Then
            'Enter time 순으로 다시 sort
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            '좀 쉬어서 소트 정보 확실히 반영되게 하자
            Threading.Thread.Sleep(10000)

            '결과 분석
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            'Sort하고 global data 계산
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortToMakeGlobalData))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            'copy to clipboard
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            'remove contents
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf RemoveContents))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            Threading.Thread.Sleep(1000)

            '2021.04.13: 여기서 flag 하나 세팅하고 DBSupporter.Simulate()를 한 번 더 돌린다. Decision 객체에서는 flag에 따라 다른 알고리즘을 적용하여, 예를 들면 현재 걸린애들이 없는 상태에서 최초 걸리면 바로 진입 안하고 조금 기다린 후 가격 떨어진 경우 진입하던가 하고, 현재 걸린 애들이 많은 상태에서 걸리면 바로 진입한다던가 하는 것이다.
            SecondStep = True

            IsThreadExecuting = True           'thread 종료 flag reset
            If DateRepeat Then
                DBSupporter.Simulate(Convert.ToDateTime(TestStartDateArray(TestDateIndex)), Convert.ToDateTime(TestEndDateArray(TestDateIndex)))
            Else
                DBSupporter.Simulate(SimulStartDate.Date, SimulEndDate.Date)
            End If
            IsThreadExecuting = False           'thread 종료 flag reset

        Else
            '150811 : 아래 문 while 걸어서 invoking 종료 확인때까지 대기하는 거 넣자. 그리고 결과 clipboard로 카피하는 거 넣자
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            '좀 쉬어서 소트 정보 확실히 반영되게 하자
            Threading.Thread.Sleep(1000)

            InvokingDone = False
            MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            'copy to result window
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
            '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While
            Threading.Thread.Sleep(1000)
        End If
    End Sub

    Public Sub SequentialDateTestBody()
        SimulationDateCollector.Clear()     'TestArray가 날짜별로 되어 있는 경우는 clear 안 해주면 날짜가 계속 누적되어 이상해진다.
        InvokingDone = False
        MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf RemoveContents))
        While InvokingDone = False
            'wait until the invoking function finishes
            Threading.Thread.Sleep(50)
        End While

        If LearnApplyRepeat Then
            '정해진 날짜에 대해 TestArray 길이만큼 반복해서 돌리는 것이다.
            Dim result_array(TestArray.Count) As Double     '여기에 일평균최대벌이 결과를 저장해둔다.
            For TestIndex = 0 To TestArray.Count - 1
                SequentialTestBody()
                Dim max_earned_string As String = lb_AveMaxEarned.Text.Split(":")(1)
                If max_earned_string = " NaN" Then
                    result_array(TestIndex) = 0
                Else
                    result_array(TestIndex) = Convert.ToDouble(max_earned_string.Replace(",", ""))
                End If
            Next
            '결과중 가장 잘 나온 놈을 찾는다.
            Dim max_result As Double = 0
            Dim max_index As Integer
            For index As Integer = 0 To result_array.Count - 1
                If result_array(index) > max_result Then
                    max_result = result_array(index)
                    max_index = index
                End If
            Next
            '그 가장 잘 나온 놈으로 다음 7일을 돌린다.
            '돌리기 전에 일단 초기화 해야 한다.
            SimulationDateCollector.Clear()     'TestArray가 날짜별로 되어 있는 경우는 clear 안 해주면 날짜가 계속 누적되어 이상해진다.
            InvokingDone = False
            MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf RemoveContents))
            While InvokingDone = False
                'wait until the invoking function finishes
                Threading.Thread.Sleep(50)
            End While

            TestIndex = max_index
            IsThreadExecuting = True           'thread 종료 flag reset
            DBSupporter.Simulate(Convert.ToDateTime(TestStartDateArray(TestDateIndex)).AddDays(14), Convert.ToDateTime(TestEndDateArray(TestDateIndex)).AddDays(7))
            IsThreadExecuting = False           'thread 종료 flag reset
        Else
            IsThreadExecuting = True           'thread 종료 flag reset
            DBSupporter.Simulate(Convert.ToDateTime(TestStartDateArray(TestDateIndex)), Convert.ToDateTime(TestEndDateArray(TestDateIndex)))
            IsThreadExecuting = False           'thread 종료 flag reset
        End If

        '150811 : 아래 문 while 걸어서 invoking 종료 확인때까지 대기하는 거 넣자. 그리고 결과 clipboard로 카피하는 거 넣자
        InvokingDone = False
        MainForm.BeginInvoke(New DelegateSortDateTime(AddressOf SortDateTime))
        While InvokingDone = False
            'wait until the invoking function finishes
            Threading.Thread.Sleep(50)
        End While

        '좀 쉬어서 소트 정보 확실히 반영되게 하자
        Threading.Thread.Sleep(1000)

        InvokingDone = False
        MainForm.BeginInvoke(New DelegateResultAnalysis(AddressOf ResultAnalysis))
        While InvokingDone = False
            'wait until the invoking function finishes
            Threading.Thread.Sleep(50)
        End While

        'copy to clipboard
        InvokingDone = False
        MainForm.BeginInvoke(New DelegateCopyResultAndDeleteContents(AddressOf CopyResult))
        '150812 : 위에 CopyResultAndRemoveContents 함수 만들자.
        While InvokingDone = False
            'wait until the invoking function finishes
            Threading.Thread.Sleep(50)
        End While
        Threading.Thread.Sleep(1000)
    End Sub

    Public Sub CopyResult()
        'Copy result
        SaveAnalysisResult()
        InvokingDone = True
    End Sub

    Public Sub RemoveContents()
        'Remove contents in the ListView
        If SmartLearning Then
            '171023: 밑에 꺼 때문에 한참 헤맸다. 다 지워버리도록 수정했다.
#If NO_SHOW_TO_THE_FORM Then
            HitList.Clear()
            For index As Integer = 0 To SymbolList.Count - 1
                SymbolList(index).Initialize()
            Next
#Else
            lv_DoneDecisions.Items.Clear()
#End If
        Else
#If NO_SHOW_TO_THE_FORM Then
            'If TestIndex <> TestArray.Count - 1 Then
            '20220613: TestArray 루프 안에서 RemoveContents의 위치가 시뮬레이션 돌리기 앞쪽으로 변경되면서 위의 If 조건이 필요없어졌다고 봐야할 것 같다.
            For index As Integer = 0 To HitList.Count - 1
                HitList(index).Tag = Nothing         '이렇게 하면 아웃오브메모리 좀 덜 날까
            Next
            HitList.Clear()
            'End If
#Else
            'If TestIndex <> TestArray.Count - 1 Then
            For index As Integer = 0 To lv_DoneDecisions.Items.Count - 1
                lv_DoneDecisions.Items(index).Tag = Nothing         '이렇게 하면 아웃오브메모리 좀 덜 날까
            Next
            lv_DoneDecisions.Items.Clear()
            'End If
#End If
        End If
        InvokingDone = True
#If CHECK_PRE_PATTERN_STRATEGY Then
        CountPostPatternFail = 0
#End If
    End Sub

    'Form update timer
    Private Sub tm_FormUpdate_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tm_FormUpdate.Tick
        CurrentProcess = Process.GetCurrentProcess
        lb_MemoryUsage.Text = "Memory Usage : " & CurrentProcess.PrivateMemorySize64.ToString("N0")
        If IsThreadExecuting Then
            If SimulationStarted = True Then
                '시뮬레이션
#If MOVING_AVERAGE_DIFFERENCE Then
                'Chart DB 쓰는 거
                If SmartLearning Then
                    ProgressText1 = DBSupporter.DBProgressValue & " / " & DBSupporter.DBProgressMax & " - BestSimulationResult: " & BestSimulationResult.ToString("##,#.###") & ". " & TestIndex.ToString() & " / " & CoefficientsAlterListLenth.ToString() & ". Bits: " & NumberOfCoeffsInTrial.ToString()
                Else
                    ProgressText1 = DBSupporter.DBProgressValue & " / " & DBSupporter.DBProgressMax & " - TestIndex :" & TestIndex.ToString & " / " & TestArray.Length.ToString
                End If
#Else
                'Chart DB 안 쓰는 거
                If SmartLearning Then
                    Me.Text = DBSupporter.ProgressValue & " / " & DBSupporter.ProgressMax & " in DB " & DBSupporter.DBProgressValue & " / " & DBSupporter.DBProgressMax & " - BestSimulationResult: " & BestSimulationResult.ToString("##,#.###") & ". " & TestIndex.ToString() & " / " & CoefficientsAlterListLenth.ToString() & ". Bits: " & NumberOfCoeffsInTrial.ToString()
                Else
                    If DateRepeat Then
                        Me.Text = DBSupporter.ProgressValue & " / " & DBSupporter.ProgressMax & " in DB " & DBSupporter.DBProgressValue & " / " & DBSupporter.DBProgressMax & " - TestDateIndex :" & TestDateIndex.ToString & " / " & TestStartDateArray.Length.ToString
                    Else
                        Me.Text = DBSupporter.ProgressValue & " / " & DBSupporter.ProgressMax & " in DB " & DBSupporter.DBProgressValue & " / " & DBSupporter.DBProgressMax & " - TestIndex :" & TestIndex.ToString & " / " & TestArray.Length.ToString
                    End If
                End If
                If DBSupporter.ProgressMax <> 0 Then
                    pb_Progress.Value = DBSupporter.ProgressValue * 100 / DBSupporter.ProgressMax
                End If
#End If

            ElseIf bt_ChartDataUpdate.Enabled = False Then
                '차트 데이터 업데이트 중
                Me.Text = ProgressText1 & " " & ProgressText2 & " " & ProgressText3
                'If TotalNumber <> 0 Then
                'Me.Text = "Getting ChartData : " & CurrentNumber & " / " & TotalNumber
                'End If
            End If
#If MOVING_AVERAGE_DIFFERENCE Then
            Me.Text = ProgressText1 & " " & ProgressText2 & " " & ProgressText3
#End If
        Else
            'thread 종료 되었음
            '150816 : 여러번 실행되게 하면서 아래 세 라인 주석처리됨
            'IsThreadExecuting = False
            'bt_StartSimul.Enabled = True
            'tm_FormUpdate.Stop()
        End If
        'log file save 하기
        'LogFileSaveTimeCount = (LogFileSaveTimeCount + 1) Mod 10
        'If LogFileSaveTimeCount = 0 Then
        Dim messages_in_one_line As String = ""
        '2초에 한 번씩 로그를 파일로 저장
        SafeEnterTrace(StoredMessagesKeyForDisplay, 70)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        For index As Integer = 0 To StoredMessagesForFileSave.Count - 1
            messages_in_one_line = messages_in_one_line & StoredMessagesForFileSave(index)
        Next
        tb_Display.AppendText(messages_in_one_line)
        Try
            'My.Computer.FileSystem.WriteAllText("log" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", messages_in_one_line, True)
            StoredMessagesForFileSave.Clear()
        Catch ex As Exception
            MsgBox("Log file save 중 에러!! - " & ex.Message)
        End Try
        SafeLeaveTrace(StoredMessagesKeyForDisplay, 71)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'End If

        'Allow multiple entering 컨트롤 업데이트
        cb_AllowMultipleEntering.Checked = AllowMultipleEntering
        'StartDate 컨트롤 업데이트
        dtp_StartDate.Value = SimulStartDate
        'EndDate 컨트롤 업데이트
        dtp_EndDate.Value = SimulEndDate
        '주간학습 컨트롤 업데이트
        cb_WeeklyLearning.Checked = WeeklyLearning
        'Mixed Learning 컨트롤 업데이트
        cb_MixedLearning.Checked = MixedLearning
        'Smart Learning 컨트롤 업데이트
        cb_SmarLearning.Checked = SmartLearning
        'Date Releat 컨트롤 업데이트
        cb_DateRepeat.Checked = DateRepeat
        'Learn Apply Repeat 컨트롤 업데이트
        cb_LearnApplyRepeat.Checked = LearnApplyRepeat
        'Simulation 시작 버튼 업데이트
        bt_StartSimul.Enabled = Not SimulationStarted
#If PCRENEW_LEARN Then
        '학습변수 컨트롤 업데이트
        If Not tb_Form_SCORE_THRESHOLD.Focused Then
            tb_Form_SCORE_THRESHOLD.Text = MainForm.Form_SCORE_THRESHOLD.ToString
        End If
        If Not tb_Form_FALL_SCALE_LOWER_THRESHOLD.Focused Then
            tb_Form_FALL_SCALE_LOWER_THRESHOLD.Text = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD.ToString
        End If
        If Not tb_Form_DEFAULT_HAVING_TIME1.Focused Then
            tb_Form_DEFAULT_HAVING_TIME1.Text = MainForm.Form_DEFAULT_HAVING_TIME1.ToString
        End If
        If Not tb_Form_DEFAULT_HAVING_TIME2.Focused Then
            tb_Form_DEFAULT_HAVING_TIME2.Text = MainForm.Form_DEFAULT_HAVING_TIME2.ToString
        End If
        If Not tb_Form_DEFAULT_HAVING_TIME3.Focused Then
            tb_Form_DEFAULT_HAVING_TIME3.Text = MainForm.Form_DEFAULT_HAVING_TIME3.ToString
        End If
        If Not tb_Form_DEFAULT_HAVING_TIME4.Focused Then
            tb_Form_DEFAULT_HAVING_TIME4.Text = MainForm.Form_DEFAULT_HAVING_TIME4.ToString
        End If
        If Not tb_Form_DEFAULT_HAVING_TIME5.Focused Then
            tb_Form_DEFAULT_HAVING_TIME5.Text = MainForm.Form_DEFAULT_HAVING_TIME5.ToString
        End If
        If Not tb_Form_DEFAULT_HAVING_TIME6.Focused Then
            tb_Form_DEFAULT_HAVING_TIME6.Text = MainForm.Form_DEFAULT_HAVING_TIME6.ToString
        End If
#End If

        'CPU load 조절버튼 업데이트
        'If Not trb_Accel.Focused Then
        trb_Accel.Value = AccelValue
        DBSupporter.SetCpuLoadControl(AccelValue)
        'End If
        Static Dim aa As Integer = 0
        aa += 1
        If aa = 5 Then
            ' Retrieve the current CPU usage across all cores
            'Dim cpuUsage As Single = CPULoadChecker.NextValue()

            ' Update your UI or display the CPU usage
            'MainForm.Text = String.Format("CPU Load: {0}%", cpuUsage)

            aa = 0
        End If
#If 1 Then
        '2024.03.31 : idle time percentage 측정값을 OS 로부터 받아서 Accel값을 조절한다.
        Static Dim measured_cpu_load_list As New List(Of Double)
        '5번의 idle time percentage 측정값을 평균한다.
        measured_cpu_load_list.Add(1 - IdleTimeChecker.NextValue / 100)
        If measured_cpu_load_list.Count > 5 Then
            measured_cpu_load_list.RemoveAt(0)

            Static Dim setting_count As Integer = 0
            setting_count += 1
            If setting_count = 2 Then
                '세팅하는 빈도는 위의 숫자로 조절할 수 있다.
                setting_count = 0
                If cb_AutoCPULoad.Checked Then
                    Dim average_CPU_load = measured_cpu_load_list.Average
                    Dim adjustment As Double
                    '20240402~주식~StockSearcher 에서 CPU 로드 자동조정을 위한 arc tanh 함수만들기 를 참조하시라
                    If average_CPU_load > TARGET_CPU_LOAD Then
                        adjustment = Math.Min(-30 * Atanh((measured_cpu_load_list.Average ^ 8 - TARGET_CPU_LOAD ^ 8) / (1 - TARGET_CPU_LOAD ^ 8)), 100)
                        If Double.IsNaN(adjustment) Then
                            adjustment = 100
                        End If
                    Else
                        adjustment = Math.Max(5 * Atanh((TARGET_CPU_LOAD ^ 4 - measured_cpu_load_list.Average ^ 4) / TARGET_CPU_LOAD ^ 4), -100)
                        If Double.IsNaN(adjustment) Then
                            adjustment = -100
                        End If
                    End If
                    If AccelValue + adjustment > 100 Then
                        AccelValue = 100
                    ElseIf AccelValue + adjustment < 15 Then
                        AccelValue = 15
                    Else
                        AccelValue = AccelValue + adjustment
                    End If
                End If
            End If
        End If
#End If
    End Sub

    Private Function Atanh(ByVal x) As Double
        Return Math.Log((1 + x) / (1 - x)) / 2
    End Function

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
#If 0 Then
        If MsgBox("위험! DB 제거 혹은 추가!", MsgBoxStyle.OkCancel) Then
            Dim remove_chart_db_contain_nothing_thread As Threading.Thread = New Threading.Thread(AddressOf RemoveAllChartDB)

            tm_FormUpdate.Start()       '관리 timer 시작
            'stm_PriceClock.Stop()         '가격 타이머 돌아가고 있으면 멈춘다.

            remove_chart_db_contain_nothing_thread.IsBackground = True
            remove_chart_db_contain_nothing_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        End If
#End If

#If CHECK_PRE_PATTERN_STRATEGY Then
        MainForm.Text = CountPostPatternFail
#End If
#If 0 Then
        Dim cat As ADOX.Catalog = New ADOX.Catalog()

        cat.Create("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\\Finance\\Database\\testing.accdb;")
        Dim _DB_connection As OleDb.OleDbConnection = New OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\\Finance\\Database\\testing.accdb;")

        _DB_connection.Open()

        '테이블 이름 리스트를 만든다.
        Dim data_table As DataTable = _DB_connection.GetSchema("tables")
        Dim table_name_list = New List(Of String)
        For table_index As Integer = 0 To data_table.Rows.Count - 1
            table_name_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
        Next
#End If
#If 0 Then
        Dim my_conn As SqlConnection = New SqlConnection("Server=" & PCName & "; database=master;")
        my_conn.Open()
        Dim Str As String = "CREATE DATABASE MyDatabase ON PRIMARY " & _
              "(NAME = MyDatabase_Data, " & _
              " FILENAME = 'D:\MyFolder\MyDatabaseData.mdf', " & _
              " SIZE = 2MB, " & _
              " MAXSIZE = 10MB, " & _
              " FILEGROWTH = 10%) " & _
              " LOG ON " & _
              "(NAME = MyDatabase_Log, " & _
              " FILENAME = 'D:\MyFolder\MyDatabaseLog.ldf', " & _
              " SIZE = 1MB, " & _
              " MAXSIZE = 5MB, " & _
              " FILEGROWTH = 10%) "

        Dim myCommand As SqlCommand = New SqlCommand(Str, my_conn)
#End If
#If 0 Then
        Dim _DB_connection As OleDb.OleDbConnection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS;Integrated Security=SSPI")
        _DB_connection.Open()

        Dim Str As String = "CREATE DATABASE MyDatabase ON PRIMARY " & _
              "(NAME = MyDatabase_Data, " & _
              " FILENAME = 'D:\MyFolder\MyDatabaseData.mdf') " & _
              " LOG ON " & _
              "(NAME = MyDatabase_Log, " & _
              " FILENAME = 'D:\MyFolder\MyDatabaseLog.ldf', " & _
              " FILEGROWTH = 10%) "
        Dim cmd As New OleDb.OleDbCommand(Str, _DB_connection)
        cmd.ExecuteNonQuery()
        _DB_connection.Close()
#End If
#If ETRADE_CONNECTION Then
        HistoryThread.Start()
#End If

    End Sub

#If ETRADE_CONNECTION Then
    Private Sub HistoryProcess()
        'Get the symbol name list
#If 0 Then
         "PriceFineDB_201401", _
         "PriceFineDB_201402", _
         "PriceFineDB_201403", _
         "PriceFineDB_201404", _
         "PriceFineDB_201405", _
         "PriceFineDB_201406", _
         "PriceFineDB_201407", _
         "PriceFineDB_201408", _
         "PriceFineDB_201409", _
         "PriceFineDB_201410", _
         "PriceFineDB_201411", _
         "PriceFineDB_201412", _
         "PriceFineDB_201501", _
         "PriceFineDB_201502", _
         "PriceFineDB_201503", _
         "PriceFineDB_201504", _
         "PriceFineDB_201505", _
         "PriceFineDB_201506", _
         "PriceFineDB_201507", _
         "PriceFineDB_201508", _
         "PriceFineDB_201509", _
         "PriceFineDB_201510", _
         "PriceFineDB_201511", _
         "PriceFineDB_201512", 
#End If
        Dim _DBList() As String = { _
         "PriceFineDB_201607", _
         "PriceFineDB_201608", _
         "PriceFineDB_201609" _
        }

        Dim db_connection As OleDb.OleDbConnection
        Dim data_table As DataTable
        Dim table_name As String
        Dim table_name_list As New List(Of String)
        For db_index As Integer = 0 To _DBList.Count - 1
            db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS; Initial Catalog=" & _DBList(db_index) & "; Integrated Security=SSPI;")
            db_connection.Open()
            data_table = db_connection.GetSchema("tables")
            For table_index As Integer = 0 To data_table.Rows.Count - 1
                table_name = data_table.Rows(table_index).Item(2).ToString
                '테이블 이름이 종목코드와 같은 형식인지 검사(첫자가 A이고 길이가 7)
                If table_name(0) = "A" And table_name.Length = 7 Then
                    If Not table_name_list.Contains(table_name) Then
                        table_name_list.Add(table_name)
                    End If
                End If
            Next
            db_connection.Close()
        Next
        Dim sub_str As String
        For table_index As Integer = 0 To table_name_list.Count - 1
            _T1305.ResFileName = "Res\t1305.res"
            sub_str = table_name_list(table_index).Substring(1, table_name_list(table_index).Length - 1)
            _T1305.SetFieldData("t1305InBlock", "shcode", 0, sub_str) ' table_name_list(table_index).Substring(1, table_name_list(table_index).Length - 1)) ' table_name_list(table_index).Substring(2,table_name_list(table_index).Length-1)
            _T1305.SetFieldData("t1305InBlock", "dwmcode", 0, 1L)
            '_T1305.SetFieldData("t1305InBlock", "date", 0, "")
            '_T1305.SetFieldData("t1305InBlock", "idx", 0, 0L)
            _T1305.SetFieldData("t1305InBlock", "cnt", 0, 500L)
            ResponseReceived = False
            RequestResult = _T1305.Request(False)
            If RequestResult > 0 Then
                Dim a = 0
                Do
                    a = a + 1
                    Threading.Thread.Sleep(10)
                    If a = 500 Then
                        My.Computer.FileSystem.WriteAllText("history1_" & table_name_list(table_index) & "_noresponse.txt", "", False)
                        NoResponseNumber = NoResponseNumber + 1
                        _T1305 = New XAQuery
                        Exit Do
                    End If
                Loop Until ResponseReceived
            Else
                FailedSymbolNumber = FailedSymbolNumber + 1
                My.Computer.FileSystem.WriteAllText("history1_" & table_name_list(table_index) & "_failed" & RequestResult.ToString & ".txt", "", False)
            End If
            Threading.Thread.Sleep(5000)
            My.Computer.FileSystem.WriteAllText("status.txt", "Current index : " & table_index.ToString & vbCrLf & "Failed : " & FailedSymbolNumber.ToString & vbCrLf & "No response : " & NoResponseNumber.ToString, False)
        Next
    End Sub

    Private Sub LoginProcess()
        Dim svr_ip As String
        Dim svr_port As String
        Dim user_id As String
        Dim user_password As String
        Dim user_cert As String
        Dim virtual_account_login As Integer

        '실제 계좌 로그인
        svr_ip = SVR_IP_REAL
        svr_port = SVR_PORT_REAL
        user_id = USER_ID_REAL
        user_password = USER_PASSWORD_REAL
        user_cert = USER_CERT_REAL
        virtual_account_login = 0

        ' XASession 객체를 생성한다.
        xa_session = New XA_SESSIONLib.XASession

        ' 이미 접속이 되어 있으면 접속을 끊는다.
        xa_session.DisconnectServer()

        ' 서버에 연결한다.
        If xa_session.ConnectServer(svr_ip, svr_port) = False Then
            MsgBox(xa_session.GetErrorMessage(xa_session.GetLastError()))
            Exit Sub
        End If
        ' 로그인 한다.
        If xa_session.Login(user_id, user_password, user_cert, virtual_account_login, False) = False Then
            MsgBox("로그인 전송 실패")
        Else
            '로그인 성공
            'LoggedIn = True
        End If

    End Sub
#End If

    '디시전 아이템 선택시
    Private Sub lv_DoneDecisions_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lv_DoneDecisions.SelectedIndexChanged
        'GDP에 표시하자
        gdp_Display.ClearAllGraphs()        '이전 그래프 모두 삭제

        If lv_DoneDecisions.SelectedItems.Count > 0 Then
            Dim the_decision_object As c050_DecisionMaker = lv_DoneDecisions.SelectedItems(0).Tag
            For index As Integer = 0 To the_decision_object.GraphicCompositeDataList.Count - 1
                gdp_Display.AddCompositeData(the_decision_object.GraphicCompositeDataList(index))
                If the_decision_object.GraphicCompositeDataList(index).Name = "DeltaGangdo" Then
                    gdp_Display.Graphs.Item(index).YScaleManager.LogScaleOption = True
                End If
            Next
        End If
        'Pause Time하자
        gdp_Display.PauseTime()
    End Sub

    '결과 분석 버튼 누름
    Private Sub bt_ProfitAnalyze_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bt_ProfitAnalyze.Click
        ResultAnalysis()
    End Sub

    Public Sub ResultAnalysis()
#If NO_SHOW_TO_THE_FORM Then
        Dim hit_list = HitList
#Else
        Dim hit_list = lv_DoneDecisions.Items
#End If
        '150808 : 여기서 Form 소속 데이터 업데이트하는 거는 전부 이미지 변수를 만들어서 이미지변수와 해당 폼콘트롤을 주기함수에서 매주기마다 동기화하는 걸로 바꾸자.
        '150810 : 아니다 필요 때마다 invoke 시켜서 하는 게 낫겠다. ListView 등은 이미지 변수 같은 거 생각도 못한다.
        If DateRepeat Then
            '시작 날짜
            lb_BeginDate.Text = lb_BeginDate.Tag.ToString & TestStartDateArray(TestDateIndex)
            '종료 날짜
            lb_EndDate.Text = lb_EndDate.Tag.ToString & TestEndDateArray(TestDateIndex)
        Else
            '시작 날짜
            lb_BeginDate.Text = lb_BeginDate.Tag.ToString & SimulStartDate.ToString("yyyy.MM.dd")
            '종료 날짜
            lb_EndDate.Text = lb_EndDate.Tag.ToString & SimulEndDate.ToString("yyyy.MM.dd")
        End If
        '분석 날짜
        lb_AnalyzeDate.Text = lb_AnalyzeDate.Tag.ToString & Now.Date.ToString("yyyy.MM.dd")

        '날짜 수 count
        'Dim previous_date As String = ""
        Dim date_count As Integer = SimulationDateCollector.Count
        'Dim total_volume As Decimal = 0
        'For index As Integer = 0 To hit_list.Count - 1
        'If hit_list(index).Text <> previous_date Then
        'date_count = date_count + 1
        'previous_date = hit_list(index).Text
        'End If

        'total_volume += CType(hit_list(index).Tag, c050_DecisionMaker).FallVolume
        'Next
        '유효 일수
        lb_DateCount.Text = lb_DateCount.Tag.ToString & date_count.ToString
        '적용 전략명
        If hit_list.Count > 0 Then
            Dim decision_maker_sample As c050_DecisionMaker = hit_list(0).Tag
            lb_StrategyName.Text = lb_StrategyName.Tag.ToString & decision_maker_sample.GetType.ToString.Split(".")(1)
        Else
            lb_StrategyName.Text = lb_StrategyName.Tag.ToString & "걸린애들없다"
        End If

        '겹침제외 일평균 검색건수 및 겹침 제외 일평균 최대 벌이. 뿐만 아니라 검색된 건수, profit도 여기서 count하기로 한다.
        Dim hit_count As Integer = 0
        Dim no_overlap_hit_count As Integer = 0
#If MOVING_AVERAGE_DIFFERENCE Then
        Dim enter_datetime_of_the_current_hit As DateTime
        Dim enter_datetime_of_the_previous_hit As DateTime = [DateTime].MinValue
        Dim minutes_passed_of_the_current_hit As Integer
        Dim minutes_passed_of_the_previous_hit As Integer = [Integer].MinValue
        Dim end_datetime_of_running_hit_no_overlap As DateTime = [DateTime].MinValue
        Dim end_datetime_of_running_hit_no_overlap_old As DateTime = [DateTime].MinValue
#Else
        Dim date_of_the_current_hit As String
        Dim date_of_the_previous_hit As String = ""
        Dim enter_time_of_the_current_hit As String
        Dim enter_time_of_the_previous_hit As String = ""
        Dim end_time_of_running_hit_no_overlap As String = ""
        Dim end_time_of_running_hit_no_overlap_old As String = ""
#End If
        Dim symbol_name_of_the_current_hit As String
        Dim symbol_name_of_the_previous_hit As String = ""
        Dim duplicated As Boolean = False
        Dim no_overlap_max_earned As Double = 0
        Dim no_overlap_total_profit_plus1 As Double = 1
        Dim the_decision_obj As c050_DecisionMaker
        Dim profit_product As Double = 1
        Dim min_profit As Double = [Double].MaxValue
        Dim max_profit As Double = [Double].MinValue
        Dim total_volume As Decimal = 0
        Dim max_earned As Double = 0
        Dim ave_invest As Double
        Dim running_hit_list_indexing As New List(Of Integer)

        For index As Integer = 0 To hit_list.Count - 1
#If MOVING_AVERAGE_DIFFERENCE Then
            the_decision_obj = hit_list(index).Tag
            enter_datetime_of_the_current_hit = CType(the_decision_obj, c05G_MovingAverageDifference).EnterTime
            minutes_passed_of_the_current_hit = CType(the_decision_obj, c05G_MovingAverageDifference).MinutesPassed
            'end_datetime_of_running_hit_no_overlap = CType(the_decision_obj, c05G_MovingAverageDifference).ExitTime
            'date_of_the_current_hit = hit_list(index).SubItems(0).Text
            'enter_time_of_the_current_hit = hit_list(index).SubItems(2).Text
            symbol_name_of_the_current_hit = hit_list(index).SubItems(4).Text.Substring(3)

            'Check duplicated
            If enter_datetime_of_the_current_hit = enter_datetime_of_the_previous_hit AndAlso minutes_passed_of_the_current_hit = minutes_passed_of_the_previous_hit AndAlso symbol_name_of_the_current_hit = symbol_name_of_the_previous_hit Then
                duplicated = True
            Else
                duplicated = False
            End If

            '평균 운용금액 구하기
            For running_index As Integer = running_hit_list_indexing.Count - 1 To 0 Step -1
                If CType(hit_list(running_hit_list_indexing(running_index)).Tag, c05G_MovingAverageDifference).ExitTime < enter_datetime_of_the_current_hit Then
                    running_hit_list_indexing.RemoveAt(running_index)
                End If
            Next
            If Not duplicated Then running_hit_list_indexing.Add(index)
            For running_index As Integer = 0 To running_hit_list_indexing.Count - 1
                If 0 AndAlso SecondStep Then
                    ave_invest += Math.Min(CType(hit_list(running_hit_list_indexing(running_index)).Tag, c05G_MovingAverageDifference).FallVolumeLimited * SILENT_INVOLVING_AMOUNT_RATE, INVEST_LIMIT)
                Else
                    ave_invest += CType(hit_list(running_hit_list_indexing(running_index)).Tag, c05G_MovingAverageDifference).FallVolumeLimited * SILENT_INVOLVING_AMOUNT_RATE
                End If
            Next

            'Update end time of current running hit
            If Not duplicated AndAlso (enter_datetime_of_the_current_hit > end_datetime_of_running_hit_no_overlap) Then
                end_datetime_of_running_hit_no_overlap = CType(the_decision_obj, c05G_MovingAverageDifference).ExitTime
            End If

            '일평균 최대 벌이
            If Not duplicated Then
                hit_count = hit_count + 1
                If 0 AndAlso SecondStep Then
                    max_earned += Math.Min(the_decision_obj.FallVolumeLimited * the_decision_obj.Profit * SILENT_INVOLVING_AMOUNT_RATE, INVEST_LIMIT)       'MovingAverageDifference용
                Else
                    max_earned += the_decision_obj.FallVolumeLimited * the_decision_obj.Profit * SILENT_INVOLVING_AMOUNT_RATE       'MovingAverageDifference용
                End If
            End If

            '겹침 제외 일평균 최대 벌이
            If end_datetime_of_running_hit_no_overlap <> end_datetime_of_running_hit_no_overlap_old Then
                no_overlap_hit_count += 1
                If 0 AndAlso SecondStep Then
                    no_overlap_max_earned += Math.Min(the_decision_obj.FallVolumeLimited * the_decision_obj.Profit * SILENT_INVOLVING_AMOUNT_RATE, INVEST_LIMIT)       'MovingAverageDifference용
                Else
                    no_overlap_max_earned += the_decision_obj.FallVolumeLimited * the_decision_obj.Profit * SILENT_INVOLVING_AMOUNT_RATE       'MovingAverageDifference용
                End If
                no_overlap_total_profit_plus1 *= 1 + the_decision_obj.Profit
            End If

            If Not duplicated Then
                profit_product *= 1 + CType(hit_list(index).Tag, c050_DecisionMaker).Profit
                min_profit = Math.Min(min_profit, CType(hit_list(index).Tag, c050_DecisionMaker).Profit)
                max_profit = Math.Max(max_profit, CType(hit_list(index).Tag, c050_DecisionMaker).Profit)
                total_volume += CType(hit_list(index).Tag, c050_DecisionMaker).FallVolume
            End If

            enter_datetime_of_the_previous_hit = enter_datetime_of_the_current_hit
            symbol_name_of_the_previous_hit = symbol_name_of_the_current_hit
            end_datetime_of_running_hit_no_overlap_old = end_datetime_of_running_hit_no_overlap
            minutes_passed_of_the_previous_hit = minutes_passed_of_the_current_hit
#Else
            date_of_the_current_hit = hit_list(index).SubItems(0).Text
            enter_time_of_the_current_hit = hit_list(index).SubItems(2).Text
            symbol_name_of_the_current_hit = hit_list(index).SubItems(4).Text.Substring(3)
            the_decision_obj = hit_list(index).Tag

            'Check duplicated
            If date_of_the_current_hit = date_of_the_previous_hit AndAlso enter_time_of_the_current_hit = enter_time_of_the_previous_hit AndAlso symbol_name_of_the_current_hit = symbol_name_of_the_previous_hit Then
                duplicated = True
            Else
                duplicated = False
            End If

            'Update end time of current running hit
            If Not duplicated AndAlso (date_of_the_current_hit <> date_of_the_previous_hit OrElse enter_time_of_the_current_hit > end_time_of_running_hit_no_overlap) Then
                end_time_of_running_hit_no_overlap = hit_list(index).SubItems(3).Text
            End If

            Dim invest_limit_multiplied As Double = INVEST_LIMIT
            If the_decision_obj.NumberOfEntering > 1 Then
                For entering_index As Integer = 0 To the_decision_obj.NumberOfEntering - 2
#If FLEXIBLE_BLUE Then
                    invest_limit_multiplied += INVEST_LIMIT
#Else
                    invest_limit_multiplied += INVEST_LIMIT * the_decision_obj.VOLUME_ATTENUATION
#End If
                Next
            End If

            '일평균 최대 벌이
            If Not duplicated Then
                hit_count = hit_count + 1

#If MOVING_AVERAGE_DIFFERENCE Then
                    max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit * SILENT_INVOLVING_AMOUNT_RATE       'MovingAverageDifference용
#ElseIf PCRENEW_LEARN Or DOUBLE_FALL Then
                max_earned += Math.Min(the_decision_obj.FallVolume / the_decision_obj.PatternLength * SilentInvolvingAmountRate, invest_limit_multiplied) * the_decision_obj.Profit 'pattern
#ElseIf SHARP_LINES Then
                'max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / ((the_decision_obj.EnterTime - the_decision_obj.StartTime).TotalSeconds / 5) * SILENT_INVOLVING_AMOUNT_RATE
                'max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / CType(the_decision_obj, c05G_DeltaGangdo).DELTA_PERIOD * SILENT_INVOLVING_AMOUNT_RATE
                'max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / the_decision_obj.PatternLength * SILENT_INVOLVING_AMOUNT_RATE 'pattern
                max_earned += Math.Min(the_decision_obj.FallVolume * SILENT_INVOLVING_AMOUNT_RATE, invest_limit_multiplied) * the_decision_obj.Profit 'pattern
#End If
            End If

            '겹침 제외 일평균 최대 벌이
            If date_of_the_current_hit <> date_of_the_previous_hit OrElse end_time_of_running_hit_no_overlap <> end_time_of_running_hit_no_overlap_old Then
                no_overlap_hit_count += 1
#If MOVING_AVERAGE_DIFFERENCE Then
                   no_overlap_max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit * SILENT_INVOLVING_AMOUNT_RATE       'MovingAverageDifference용
#ElseIf PCRENEW_LEARN Or DOUBLE_FALL Then
                no_overlap_max_earned += Math.Min(the_decision_obj.FallVolume / the_decision_obj.PatternLength * SilentInvolvingAmountRate, invest_limit_multiplied) * the_decision_obj.Profit   'pattern
#ElseIf SHARP_LINES Then
                no_overlap_max_earned += Math.Min(the_decision_obj.FallVolume / SILENT_INVOLVING_AMOUNT_RATE, invest_limit_multiplied) * the_decision_obj.Profit   'pattern
                'no_overlap_max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / the_decision_obj.PatternLength * SILENT_INVOLVING_AMOUNT_RATE  'pattern
                'no_overlap_max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / ((the_decision_obj.EnterTime - the_decision_obj.StartTime).TotalSeconds / 5) * SILENT_INVOLVING_AMOUNT_RATE
                'no_overlap_max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / CType(the_decision_obj, c05G_DeltaGangdo).DELTA_PERIOD * SILENT_INVOLVING_AMOUNT_RATE
#End If
                no_overlap_total_profit_plus1 *= 1 + the_decision_obj.Profit
            End If

            If Not duplicated Then
                profit_product *= 1 + CType(hit_list(index).Tag, c050_DecisionMaker).Profit
                min_profit = Math.Min(min_profit, CType(hit_list(index).Tag, c050_DecisionMaker).Profit)
                max_profit = Math.Max(max_profit, CType(hit_list(index).Tag, c050_DecisionMaker).Profit)
                total_volume += CType(hit_list(index).Tag, c050_DecisionMaker).FallVolume
            End If

            date_of_the_previous_hit = date_of_the_current_hit
            enter_time_of_the_previous_hit = enter_time_of_the_current_hit
            symbol_name_of_the_previous_hit = symbol_name_of_the_current_hit
            end_time_of_running_hit_no_overlap_old = end_time_of_running_hit_no_overlap
#End If
        Next
        lb_AveMaxEarned_NoOverlap.Text = lb_AveMaxEarned_NoOverlap.Tag.ToString & (no_overlap_max_earned / date_count).ToString("##,#")

        '검색된 건수
        lb_HitCount.Text = lb_HitCount.Tag.ToString & hit_count.ToString
        '일평균 검색건수
        lb_AveHitCount.Text = lb_AveHitCount.Tag.ToString & (hit_count / date_count).ToString

        lb_AveHitCountNoOverlap.Text = lb_AveHitCountNoOverlap.Tag.ToString & (no_overlap_hit_count.ToString / date_count).ToString
        '겹침 비율
        If hit_count > 0 Then
            lb_OverlapRate.Text = lb_OverlapRate.Tag.ToString & (1 - no_overlap_hit_count / hit_count).ToString("p")
        Else
            lb_OverlapRate.Text = "-"
        End If

        '평균 하강 볼륨
        If hit_count > 0 Then
            lb_AveFallVolume.Text = lb_AveFallVolume.Tag.ToString & (total_volume / hit_count).ToString("##,#")
        Else
            lb_AveFallVolume.Text = "-"
        End If
        '평균 실질 수익률
        'Dim profit_product As Double = 1
        'Dim min_profit As Double = [Double].MaxValue
        'Dim max_profit As Double = [Double].MinValue
        'For index As Integer = 0 To hit_list.Count - 1
        'profit_product *= 1 + CType(hit_list(index).Tag, c050_DecisionMaker).Profit
        'profit_product += CType(hit_list(index).Tag, c050_DecisionMaker).Profit
        'min_profit = Math.Min(min_profit, CType(hit_list(index).Tag, c050_DecisionMaker).Profit)
        'max_profit = Math.Max(max_profit, CType(hit_list(index).Tag, c050_DecisionMaker).Profit)
        'Next
        Dim ave_profit As Double
        If hit_count > 0 Then
            ave_profit = profit_product ^ (CType(1, Double) / hit_count) - 1
        Else
            ave_profit = 0
        End If
        'Dim ave_profit As Double = profit_product / hit_list.Count
        lb_AveProfit.Text = lb_AveProfit.Tag.ToString & (ave_profit).ToString("p")
        '최저 수익률
        lb_MinProfit.Text = lb_MinProfit.Tag.ToString & min_profit.ToString("p")
        '최고 수익률
        lb_MaxProfit.Text = lb_MaxProfit.Tag.ToString & max_profit.ToString("p")
        '겹침 제외 누적 수익률
        'lb_TotalProfitWithoutOverlap.Text = lb_TotalProfitWithoutOverlap.Tag.ToString & (((1 + ave_profit) ^ no_overlap_hit_count) - 1).ToString("p")
        lb_TotalProfitWithoutOverlap.Text = lb_TotalProfitWithoutOverlap.Tag.ToString & (no_overlap_total_profit_plus1 - 1).ToString("p")     '2021.03.14: 좀 더 현실적인 겹침 제외 누적수익률로 변경
        '겹침 제외 누적 수익률 (연환산)
        Dim day_ave_profit_no_overlap As Double = (no_overlap_total_profit_plus1) ^ (1 / date_count)        '일평균 겹침제외 수익률 + 1
        lb_AnualTotalprofitWithoutOverlap.Text = lb_AnualTotalprofitWithoutOverlap.Tag.ToString & (day_ave_profit_no_overlap ^ 240 - 1).ToString("p")       '겹침제외누적수익률(연환산) 240은 연평균 거래일수
        '평균 총 소요시간
        If hit_list.Count > 0 Then
            Dim total_time_sum As TimeSpan = TimeSpan.FromSeconds(0)
            Dim waiting_time_sum As TimeSpan = TimeSpan.FromSeconds(0)
            Dim having_time_sum As TimeSpan = TimeSpan.FromSeconds(0)
            For index As Integer = 0 To hit_list.Count - 1
                the_decision_obj = hit_list(index).Tag
                total_time_sum += the_decision_obj.TookTime
                waiting_time_sum += the_decision_obj.EnterTime - the_decision_obj.StartTime
#If MOVING_AVERAGE_DIFFERENCE Then
                having_time_sum += TimeSpan.FromMinutes(CType(the_decision_obj, c05G_MovingAverageDifference).MinutesPassed)
#Else
                having_time_sum += the_decision_obj.ExitTime - the_decision_obj.EnterTime
#End If
            Next
            lb_AveTookTime.Text = lb_AveTookTime.Tag.ToString & TimeSpan.FromSeconds(total_time_sum.TotalSeconds / hit_list.Count).ToString("g")
            '평균 반등기다림 시간
            lb_AveWaitRisingTime.Text = lb_AveWaitRisingTime.Tag.ToString & TimeSpan.FromSeconds(waiting_time_sum.TotalSeconds / hit_list.Count).ToString("g")
            '평균 보유 시간
            lb_AveHavingTime.Text = lb_AveHavingTime.Tag.ToString & TimeSpan.FromSeconds(having_time_sum.TotalSeconds / hit_list.Count).ToString("g")
            '평균 보유시간/반등기다림시간 비율
            lb_AveHavingTimeRate.Text = lb_AveHavingTimeRate.Tag.ToString & (having_time_sum.TotalSeconds / waiting_time_sum.TotalSeconds).ToString("p")
        Else
            lb_AveTookTime.Text = "-"
            lb_AveWaitRisingTime.Text = "-"
            lb_AveHavingTime.Text = "-"
            lb_AveHavingTimeRate.Text = "-"
        End If
        '일평균 최대 벌이
        'Dim max_earned As Double = 0
        'For index As Integer = 0 To hit_list.Count - 1
        'the_decision_obj = hit_list(index).Tag
        'If DecisionByPattern Then
        'max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / the_decision_obj.PatternLength * SILENT_INVOLVING_AMOUNT_RATE
        'Else
        'max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / ((the_decision_obj.EnterTime - the_decision_obj.StartTime).TotalSeconds / 5) * SILENT_INVOLVING_AMOUNT_RATE
        'max_earned += the_decision_obj.FallVolume * the_decision_obj.Profit / CType(the_decision_obj, c05G_DeltaGangdo).DELTA_PERIOD * SILENT_INVOLVING_AMOUNT_RATE
        'End If
        'Next
        lb_AveMaxEarned.Text = lb_AveMaxEarned.Tag.ToString & (max_earned / date_count).ToString("##,#")
        '평균 운용금액
        lb_AveInvestMoney.Text = lb_AveInvestMoney.Tag.ToString & (ave_invest / hit_count).ToString("##,#")
        '기타 comment
        'SimulationResult 업데이트
        'SimulationResult = (ave_profit - 0.008) * no_overlap_hit_count / date_count ' no_overlap_max_earned / date_count * (ave_profit * 100 + 1) 'Math.Exp(ave_profit * 50)
        'SimulationResult = max_earned / date_count
        'SimulationResult = day_ave_profit_no_overlap ^ 240 - 1
        'SimulationResult = max_earned / date_count - 1000 * (45 - hit_count / date_count) ^ 2
        'SimulationResult = 1 - (ave_profit - 0.012) ^ 2
        SimulationResult = (ave_profit - 0.008) * hit_count / date_count
        'SimulationResult = (ave_profit) * hit_count / date_count
        'SimulationResult = (ave_profit - 0.001) * hit_count / date_count

        InvokingDone = True
    End Sub

    Private Function SaveAnalysisResult() As String()
        Dim header As String = ""
        Dim content_line As String = ""

        If lb_StrategyName.Text.Split(":")(1) <> " 걸린애들없다" Then
            '분석 날짜
            header += lb_AnalyzeDate.Text.Split(":")(0) & vbTab
            content_line += lb_AnalyzeDate.Text.Split(":")(1) & vbTab
            '시작 날짜
            header += lb_BeginDate.Text.Split(":")(0) & vbTab
            content_line += lb_BeginDate.Text.Split(":")(1) & vbTab
            '종료 날짜
            header += lb_EndDate.Text.Split(":")(0) & vbTab
            content_line += lb_EndDate.Text.Split(":")(1) & vbTab
            '유효 일수
            header += lb_DateCount.Text.Split(":")(0) & vbTab
            content_line += lb_DateCount.Text.Split(":")(1) & vbTab
            '적용 전략명
            header += lb_StrategyName.Text.Split(":")(0) & vbTab
            content_line += lb_StrategyName.Text.Split(":")(1) & vbTab
            '검색된 건수
            header += lb_HitCount.Text.Split(":")(0) & vbTab
            content_line += lb_HitCount.Text.Split(":")(1) & vbTab
            '일평균 검색건수
            header += lb_AveHitCount.Text.Split(":")(0) & vbTab
            content_line += lb_AveHitCount.Text.Split(":")(1) & vbTab
            '겹침제외 일평균 검색건수
            header += lb_AveHitCountNoOverlap.Text.Split(":")(0) & vbTab
            content_line += lb_AveHitCountNoOverlap.Text.Split(":")(1) & vbTab
            '겹침 비율
            header += lb_OverlapRate.Text.Split(":")(0) & vbTab
            content_line += lb_OverlapRate.Text.Split(":")(1) & vbTab
            '평균 하강 볼륨
            header += lb_AveFallVolume.Text.Split(":")(0) & vbTab
            content_line += lb_AveFallVolume.Text.Split(":")(1) & vbTab
            '평균 수익률
            header += lb_AveProfit.Text.Split(":")(0) & vbTab
            content_line += lb_AveProfit.Text.Split(":")(1) & vbTab
            '최저 수익률
            header += lb_MinProfit.Text.Split(":")(0) & vbTab
            content_line += lb_MinProfit.Text.Split(":")(1) & vbTab
            '최고 수익률
            header += lb_MaxProfit.Text.Split(":")(0) & vbTab
            content_line += lb_MaxProfit.Text.Split(":")(1) & vbTab
            '겹침 제외 누적 수익률
            header += lb_TotalProfitWithoutOverlap.Text.Split(":")(0) & vbTab
            content_line += lb_TotalProfitWithoutOverlap.Text.Split(":")(1) & vbTab
            '겹침 제외 누적 수익률 (연환산)
            header += lb_AnualTotalprofitWithoutOverlap.Text.Split(":")(0) & vbTab
            content_line += lb_AnualTotalprofitWithoutOverlap.Text.Split(":")(1) & vbTab
            '평균 총 소요시간
            header += lb_AveTookTime.Text.Split(":")(0) & vbTab
            content_line += lb_AveTookTime.Text.Substring(lb_AveTookTime.Text.IndexOf(":") + 1, lb_AveTookTime.Text.Length - lb_AveTookTime.Text.IndexOf(":") - 1) & vbTab
            '평균 반등기다림 시간
            header += lb_AveWaitRisingTime.Text.Split(":")(0) & vbTab
            content_line += lb_AveWaitRisingTime.Text.Substring(lb_AveWaitRisingTime.Text.IndexOf(":") + 1, lb_AveWaitRisingTime.Text.Length - lb_AveWaitRisingTime.Text.IndexOf(":") - 1) & vbTab
            '평균 보유 시간
            header += lb_AveHavingTime.Text.Split(":")(0) & vbTab
            content_line += lb_AveHavingTime.Text.Substring(lb_AveHavingTime.Text.IndexOf(":") + 1, lb_AveHavingTime.Text.Length - lb_AveHavingTime.Text.IndexOf(":") - 1) & vbTab
            '평균 보유시간/반등기다림시간 비율
            header += lb_AveHavingTimeRate.Text.Split(":")(0) & vbTab
            content_line += lb_AveHavingTimeRate.Text.Split(":")(1) & vbTab
            '기타 comment
            header += "기타 comment"
            content_line += " "

            '일평균 최대 벌이
            header += lb_AveMaxEarned.Text.Split(":")(0) & vbTab
            content_line += lb_AveMaxEarned.Text.Split(":")(1) & vbTab

            '겹침제외 일평균 최대 벌이
            header += lb_AveMaxEarned_NoOverlap.Text.Split(":")(0) & vbTab
            content_line += lb_AveMaxEarned_NoOverlap.Text.Split(":")(1) & vbTab

            '평균 운용금액
            header += lb_AveInvestMoney.Text.Split(":")(0) & vbTab
            content_line += lb_AveInvestMoney.Text.Split(":")(1) & vbTab
        End If
        'Coefficients
        If SmartLearning Then
            content_line += SimulationResult.ToString() & vbTab
#If PCRENEW_LEARN Then
#If LEARN_UMEP Then
            content_line += Form_SCORE_THRESHOLD.ToString() & vbTab
            content_line += Form_FALL_SCALE_LOWER_THRESHOLD.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME1.ToString() & vbTab
            content_line += Form_TH_ATTENUATION.ToString() & vbTab
            content_line += Form_VOLUME_ATTENUATION.ToString() & vbTab
#ElseIf LEARN_UNIHAV Then
            content_line += Form_SCORE_THRESHOLD.ToString() & vbTab
            content_line += Form_FALL_SCALE_LOWER_THRESHOLD.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME1.ToString() & vbTab
#Else
            content_line += Form_SCORE_THRESHOLD.ToString() & vbTab
            content_line += Form_FALL_SCALE_LOWER_THRESHOLD.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME1.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME2.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME3.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME4.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME5.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME6.ToString() & vbTab
#End If
#If UNWANTED_RISING_DETECTION Then
            content_line += Form_TIME_DIFF_FOR_RISING_DETECTION.ToString() & vbTab
            content_line += Form_RISING_SLOPE_THRESHOLD.ToString() & vbTab
            content_line += Form_ENTERING_PROHIBIT_TIME.ToString() & vbTab
#End If
#ElseIf DOUBLE_FALL Then
            content_line += Form_SCORE_THRESHOLD1.ToString() & vbTab
            content_line += Form_FALL_SCALE_LOWER_THRESHOLD1.ToString() & vbTab
            content_line += Form_SCORE_THRESHOLD2.ToString() & vbTab
            content_line += Form_FALL_SCALE_LOWER_THRESHOLD2.ToString() & vbTab
            content_line += Form_MAX_SECONDFALL_WAITING.ToString() & vbTab
            content_line += Form_DEFAULT_HAVING_TIME.ToString() & vbTab
#ElseIf SHARP_LINES Then
            content_line += Form_VERTICAL_SCALE.ToString() & vbTab
            content_line += Form_HOT_HEIGHT.ToString() & vbTab
            content_line += Form_HOT_WIDTH.ToString() & vbTab
            content_line += Form_STANDARD_LENGTH.ToString() & vbTab
            content_line += Form_X_LIMIT.ToString() & vbTab
            content_line += Form_SLOPE1_LIMIT.ToString() & vbTab
            content_line += Form_SLOPE3_LIMIT.ToString() & vbTab
#Else
#End If
            'content_line += MADIFFSCA_FADE_FACTOR_MA4800.ToString() & vbTab
            'content_line += MADIFFSCA_DETECT_SCALE_MA0035.ToString() & vbTab
            'content_line += REDUCE_ASSIGN_RATE.ToString() & vbTab
            'content_line += MADIFFSCA_A.ToString() & vbTab
            'content_line += MADIFFSCA_B.ToString() & vbTab
            'content_line += Form_TIME_DIFF_FOR_RISING_DETECTION.ToString() & vbTab
            'content_line += Form_RISING_SLOPE_THRESHOLD.ToString() & vbTab
            'content_line += Form_ENTERING_PROHIBIT_TIME.ToString() & vbTab
            'content_line += Form_TEMP_SLOPE1.ToString() & vbTab
            'content_line += Form_TEMP_SLOPE2.ToString() & vbTab
            content_line += TestIndex.ToString() & vbTab
            content_line += CoefficientsAlterListLenth.ToString() & vbTab
            content_line += NumberOfCoeffsInTrial.ToString() & vbTab
            'content_line += Form_DEFAULT_HAVING_TIME.ToString() & vbTab
            'content_line += Form_SCORE_THRESHOLD.ToString() & vbTab
            'content_line += Form_FALL_SCALE_LOWER_THRESHOLD.ToString() & vbTab
            'content_line += Form_MAX_HAVING_LENGTH.ToString() & vbTab
            'content_line += TestIndex.ToString() & vbTab
            'content_line += CoefficientsAlterListLenth.ToString() & vbTab
            'content_line += NumberOfCoeffsInTrial.ToString() & vbTab
        Else
            content_line += SimulationResult.ToString() & vbTab
            content_line += TestIndex.ToString() & vbTab
        End If
#If CHECK_PRE_PATTERN_STRATEGY Then
        content_line += CountPostPatternFail.ToString() & vbTab
#End If
        '결과창에 표시
        'tb_Result.AppendText(header & vbCrLf)
        tb_Result.AppendText(content_line & vbCrLf)

        '결과파일 smart_learning.txt에 저장
        My.Computer.FileSystem.WriteAllText("smart_learning.txt", content_line & vbCrLf, True)

        Return {header, content_line}
    End Function

    '결과를 복사해 notepad 에 갖다붙인다
    Private Sub SaveAnalysis_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bt_SaveAnalysis.Click
        Dim str_array As String() = SaveAnalysisResult()
        Dim header = str_array(0)
        Dim content_line = str_array(1)

        '클립보드에 저장
        'Clipboard.Clear()
        ' 2023.07.28: Chrome Remote Desktop 쓰면서 에러나서 일단 빼버림
        ' 2023.08.04: exception handling 넣으면서 다시 살림
        ' 2023.08.04: Requested Clipboard operation did not succeed 이런류의 클립보드 익셉션 해결을 위해 try catch 구문을 포함하여 SetText 대신 SetDataObject 사용 등등
        ' 2023.08.04: 여러가지 방법을 써 봤지만 효과가 없었음. 그래서 clipboard 용 thread 분리 하는 방법도 해봤지만 이건 local clipboard로도 복사가 안 됨.
        ' 2023.08.04: 그래서 그나마 로컬 클립보드로는 복사가 되는 방법을 사용하기로 함
        ' 2023.08.04: 다시 해보니 Clipboard.SetDataObject(header & vbCrLf & content_line, False, 5, 200) 를 가지고 하면 로컬은 물론이고 원격접속하는 PC 에도 copy가 된다. 이걸로 쓰자.
        Try
            'Clipboard.SetText(header & vbCrLf & content_line)
            Clipboard.SetDataObject(header & vbCrLf & content_line, False, 5, 200)
            'Dim thread_for_clipboard As Threading.Thread = New Threading.Thread(AddressOf ClipboardThreadFunction)
            'thread_for_clipboard.SetApartmentState(Threading.ApartmentState.STA)
            'thread_for_clipboard.Start(header & vbCrLf & content_line)
        Catch ex As Exception
            ' 2023.08.04: exception 나지만 실제로 clipboard는 복사가 된다고 함. 그냥 아무것도 안 하고 그냥 진행시키면 됨
        End Try
    End Sub

    'save to clipboard
    Private Sub SaveToClipboardToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SaveToClipboardToolStripMenuItem.Click
        Dim line_str As String = ""
        'Dim number_of_columns As Integer = lv_DoneDecisions.Columns.Count
#If NO_SHOW_TO_THE_FORM Then
        For index As Integer = 0 To HitList.Count - 1
            For index_col As Integer = 0 To HitList(index).SubItems.Count - 1 ' lv_DoneDecisions.Items(index).SubItems.Count - 1
                line_str += HitList(index).SubItems(index_col).Text & vbTab
            Next
            line_str += vbCrLf
            If index Mod 1000 = 999 Then
                My.Computer.FileSystem.WriteAllText("걸린애들저장.txt", line_str, True)
                line_str = ""
            End If
        Next
        My.Computer.FileSystem.WriteAllText("걸린애들저장.txt", line_str, True)
#Else
        For index As Integer = 0 To lv_DoneDecisions.Items.Count - 1
            For index_col As Integer = 0 To 10 'lv_DoneDecisions.Items(index).SubItems.Count - 1
                line_str += lv_DoneDecisions.Items(index).SubItems(index_col).Text & vbTab
            Next
            line_str += vbCrLf
        Next
#End If
        Clipboard.SetText(line_str)
    End Sub

    Private Sub trb_Accel_Scroll(sender As Object, e As EventArgs) Handles trb_Accel.Scroll
        AccelValue = trb_Accel.Value
        DBSupporter.SetCpuLoadControl(AccelValue)
    End Sub

    Private Sub trb_DBInterval_Scroll(sender As Object, e As EventArgs) Handles trb_DBInterval.Scroll
        DBIntervalStored = trb_DBInterval.Value
    End Sub

#If ETRADE_CONNECTION Then
    Private Sub _T1305_ReceiveData(szTrCode As String) Handles _T1305.ReceiveData
        Dim symbol_code As String
        Dim count As Integer
        Dim close As Integer
        Dim change As Integer
        Dim sign As Integer
        Dim yester_price As Integer
        Dim this_date As String 
        Dim price_list As String = ""

        symbol_code = _T1305.GetFieldData("t1305OutBlock1", "shcode", 0)
        count = _T1305.GetFieldData("t1305OutBlock", "cnt", 0)
        For index As Integer = 0 To count - 1
            close = Convert.ToUInt32(_T1305.GetFieldData("t1305OutBlock1", "close", index))  '종가
            change = Convert.ToUInt32(_T1305.GetFieldData("t1305OutBlock1", "change", index))  '전일대비
            sign = Convert.ToUInt32(_T1305.GetFieldData("t1305OutBlock1", "sign", index))  '증감부호
            If sign = 2 Then
                yester_price = close - change
            Else 'if sign = 5 then
                yester_price = close + change
            End If
            this_date = _T1305.GetFieldData("t1305OutBlock1", "date", index)
            price_list = price_list & this_date & ", " & yester_price.ToString & vbCrLf
        Next

        My.Computer.FileSystem.WriteAllText("history1_" & symbol_code & ".txt", price_list, True)
        RxProcessCompleted = True
    End Sub


    Private Sub _ChartIndex_ReceiveData(szTrCode As String) Handles _ChartIndex.ReceiveData
        'Dim symbol_code As String
        Dim count As Integer
        'Dim close As Integer
        'Dim change As Integer
        'Dim sign As Integer
        'Dim yester_price As Integer
        'Dim this_date As String
        'Dim price_list As String = ""
        Dim date_str, time_str, open_str, high_str, low_str, close_str As String
        Dim line_text As String = ""

        'symbol_code = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "shcode", 0)
        'count = _ChartIndex.GetFieldData("ChartIndexOutBlock", "cnt", 0)
        'g_indexId = XAQuery_ChartIndex.GetFieldData("ChartIndexOutBlock", "indexid", 0)

        ' 검색 종목수
        count = Convert.ToInt32(_ChartIndex.GetFieldData("ChartIndexOutBlock", "rec_cnt", 0))

        For index As Integer = 1 To count
            date_str = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "date", index)     ' 일자
            time_str = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "time", index)     ' 시간
            open_str = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "open", index)     ' 시가
            high_str = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "high", index)     ' 고가
            low_str = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "low", index)      ' 저가
            close_str = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "close", index)    ' 종가
            line_text = date_str & ", " & time_str & ", " & open_str & ", " & high_str & ", " & low_str & ", " & vbCrLf
            'Close = Convert.ToUInt32(_ChartIndex.GetFieldData("ChartIndexOutBlock1", "close", index))  '종가
            'change = Convert.ToUInt32(_ChartIndex.GetFieldData("ChartIndexOutBlock1", "change", index))  '전일대비
            'sign = Convert.ToUInt32(_ChartIndex.GetFieldData("ChartIndexOutBlock1", "sign", index))  '증감부호
            'If sign = 2 Then
            'yester_price = Close() - change
            'Else 'if sign = 5 then
            'yester_price = Close() + change
            'End If
            'this_date = _ChartIndex.GetFieldData("ChartIndexOutBlock1", "date", index)
            'price_list = price_list & this_date & ", " & yester_price.ToString & vbCrLf
        Next

        My.Computer.FileSystem.WriteAllText("ChartData\Minute\" & SymbolCodeOnRequest & ".txt", line_text, True)
        ResponseFlag = True
    End Sub
#End If

    Private Sub bt_Login_Click(sender As Object, e As EventArgs) Handles bt_Login.Click
#If ETRADE_CONNECTION Then
        LoginProcess()
#End If
    End Sub

    Private Sub bt_GlobalTrend_Click(sender As Object, e As EventArgs) Handles bt_GlobalTrend.Click
        '170821: DBSupport에 함수 하나 만들고(Simulate 참고해서) 그거 부르자
#If Not MOVING_AVERAGE_DIFFERENCE Then
        DBSupporter.GlobalTrending(dtp_StartDate.Value.Date, dtp_EndDate.Value.Date)
#End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        '검색된 종목을 날짜별로 sort
#If 0 Then
        MessageLogging("PartCount")
        For index As Integer = 0 To 9
            MessageLogging(PartCount(index))
        Next
        MessageLogging("PartProfit")
        For index As Integer = 0 To 9
            MessageLogging(PartMoney(index) / PartCount(index))
        Next
#End If
        SortDateTime()
    End Sub

    Private Sub SortDateTime()
#If NO_SHOW_TO_THE_FORM Then
        'TabControl1.SelectTab(3)
        '190917: no show로 했을 때 소트가 이상하니 한 번 봐라. 정 안 되면 별도 sort 펑션 만들어야 할 듯.
        'Dim i_comparer_time As New HitListComparer(2, SortOrder.Ascending, ListViewSortType.LV_SORT_STRING)
        'Dim i_comparer_date As New HitListComparer(0, SortOrder.Ascending, ListViewSortType.LV_SORT_STRING)

#If 1 Then
        MessageLogging("PartCount")
        For index As Integer = 0 To 9
            MessageLogging(PartCount(index))
        Next
        MessageLogging("PartProfit")
        For index As Integer = 0 To 9
            MessageLogging(PartMoney(index) / PartCount(index))
        Next
        'Part정보 clear
        For index As Integer = 0 To 9
            PartMoney(index) = 0
            PartCount(index) = 0
        Next
#End If
        HitList.Sort(AddressOf DateTimeComparison)
        InvokingDone = True
        'TabControl1.SelectTab(4)
#Else
#If 1 Then
        MessageLogging("PartCount")
        For index As Integer = 0 To PartCount.Length - 1
            MessageLogging(PartCount(index))
        Next
        MessageLogging("PartProfit")
        For index As Integer = 0 To PartCount.Length - 1
            MessageLogging(PartMoney(index) / PartCount(index))
        Next
        'Part정보 clear
        For index As Integer = 0 To PartCount.Length - 1
            PartMoney(index) = 0
            PartCount(index) = 0
        Next
#End If
        TabControl1.SelectTab(3)
        lv_DoneDecisions.ColumnSort(4)              '종목코드별 sorting, 소트가 어떤 때는 조금씩 다를 때도 있어서 이걸 추가함
        lv_DoneDecisions.ColumnSort(2)              '진입시간별 sorting
        lv_DoneDecisions.ColumnSort(0)              '날짜별 sorting
        InvokingDone = True
        TabControl1.SelectTab(4)
#End If
    End Sub

    Private Sub SortToMakeGlobalData()
        TabControl1.SelectTab(3)

        'EnterTime 에 맞추고 sort
        lv_DoneDecisions.ColumnSort(4)              '종목코드별 sorting, 소트가 어떤 때는 조금씩 다를 때도 있어서 이걸 추가함
        lv_DoneDecisions.ColumnSort(2)              '진입시간별 sorting
        lv_DoneDecisions.ColumnSort(0)              '날짜별 sorting

        'GlobalData 수집
        Dim the_decision_obj As c050_DecisionMaker
        For gullin_index As Integer = 0 To lv_DoneDecisions.Items.Count - 1
            the_decision_obj = lv_DoneDecisions.Items(gullin_index).Tag
            GlobalDataTime.Add(CType(the_decision_obj, c05G_MovingAverageDifference).EnterTime)
        Next

        '3번열을 having time 이 아니라 exit 시간으로 재설정하고 같은 방식으로 소트해보자.
        For gullin_index As Integer = 0 To lv_DoneDecisions.Items.Count - 1
            the_decision_obj = lv_DoneDecisions.Items(gullin_index).Tag
            lv_DoneDecisions.Items(gullin_index).Text = the_decision_obj.ExitTime.Date.ToString("d")
            lv_DoneDecisions.Items(gullin_index).SubItems(3).Text = the_decision_obj.ExitTime.TimeOfDay.ToString
        Next

        'ExitTime 에 맞추고 sort
        lv_DoneDecisions.ColumnSort(4)              '종목코드별 sorting, 소트가 어떤 때는 조금씩 다를 때도 있어서 이걸 추가함
        lv_DoneDecisions.ColumnSort(3)              '퇴장시간별 sorting
        lv_DoneDecisions.ColumnSort(0)              '날짜별 sorting

        Dim global_pointer As Integer = 0
        Dim current_gullin_count As Integer = 0
        For gullin_index As Integer = 0 To lv_DoneDecisions.Items.Count - 1
            the_decision_obj = lv_DoneDecisions.Items(gullin_index).Tag
            If global_pointer < GlobalDataTime.Count AndAlso GlobalDataTime(global_pointer) < the_decision_obj.ExitTime Then
                Do
                    current_gullin_count += 1
                    GlobalDataCount.Add(current_gullin_count)
                    global_pointer += 1
                Loop While global_pointer < GlobalDataTime.Count AndAlso GlobalDataTime(global_pointer) < the_decision_obj.ExitTime
            End If
            GlobalDataTime.Insert(global_pointer, the_decision_obj.ExitTime)
            global_pointer += 1
            current_gullin_count -= 1
            GlobalDataCount.Add(current_gullin_count)
        Next

        InvokingDone = True
        TabControl1.SelectTab(4)
    End Sub

    Private Function DateTimeComparison(ByVal x As ListViewItem, ByVal y As ListViewItem) As Integer
        Dim x_str As String = x.Text & x.SubItems(2).Text & x.SubItems(4).Text
        Dim y_str As String = y.Text & y.SubItems(2).Text & y.SubItems(4).Text

        Dim return_val = [String].Compare(x_str, y_str)
        Return return_val
    End Function
#If 0 Then
    Private Function TimeComparison(ByVal x As ListViewItem, ByVal y As ListViewItem) As Integer
        Dim return_val = [String].Compare(x.SubItems(2).Text, y.SubItems(2).Text)
        Return return_val
    End Function

    Private Function DateComparison(ByVal x As ListViewItem, ByVal y As ListViewItem) As Integer
        Dim return_val = [String].Compare(x.SubItems(0).Text, y.SubItems(0).Text)
        Return return_val
    End Function
#End If

#If ETRADE_CONNECTION Then
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles bt_ChartDataUpdate.Click
        Dim chart_data_update_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDataUpdateThread)
        chart_data_update_thread.IsBackground = True
        chart_data_update_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        tm_FormUpdate.Start()       '관리 timer 시작
        'IsThreadExecuting = True
        bt_ChartDataUpdate.Enabled = False
    End Sub
#End If

    Private _db_connection As OleDb.OleDbConnection
    'Public TotalNumber, CurrentNumber As Integer
    Private Shared _CANDLE_CHART_FOLDER As String = "D:\Finance\Database\CandleChart\"
#If ETRADE_CONNECTION Then
    Public SymbolCodeOnRequest As String
    Public CtsDate As String = ""
    Public CtsTime As String = ""
    Private _TableNameToCheck As String
    Private _CandleChartInsertErrorCount As Integer

    Private Sub ChartDataUpdateThread()
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim read_result As OleDb.OleDbDataReader

        '최근 Price data DB로부터 종목정보를 모은다.
        Dim _DBList() As String = { _
         "PriceGangdoDB_201810", _
         "PriceGangdoDB_201811", _
         "PriceGangdoDB_201812", _
         "PriceGangdoDB_201901" _
        }
        Dim data_table As DataTable
        Dim table_name As String
        Dim table_name_list As New List(Of String)
        For db_index As Integer = 0 To _DBList.Count - 1
            _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS; Initial Catalog=" & _DBList(db_index) & "; Integrated Security=SSPI;")
            _db_connection.Open()
            data_table = _db_connection.GetSchema("tables")
            For table_index As Integer = 0 To data_table.Rows.Count - 1
                table_name = data_table.Rows(table_index).Item(2).ToString
                '테이블 이름이 종목코드와 같은 형식인지 검사(첫자가 A이고 길이가 7)
                If table_name(0) = "A" And table_name.Length = 7 Then
                    If Not table_name_list.Contains(table_name) Then
                        table_name_list.Add(table_name)
                    End If
                End If
            Next
            _db_connection.Close()
        Next

        'ChartData를 가져온다.
        '_db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS; Initial Catalog=CandleChartDB; Integrated Security=SSPI;")
        '_db_connection.Open()

        'Candle Chart 테이블 이름 리스트를 만든다.
        'data_table = _db_connection.GetSchema("tables")
        'Dim candle_table_list As New List(Of String)
        'For table_index As Integer = 0 To data_table.Rows.Count - 1
        'candle_table_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
        'Next

        Dim db_exist As Boolean
        Dim db_name As String
        Dim candle_table_list As New List(Of String)
        TotalNumber = table_name_list.Count
        CurrentNumber = 0
        For table_index As Integer = 0 To table_name_list.Count - 1
            If table_name_list(table_index) <> "A000030" Then
                Continue For
            End If
            'If table_index > 2603 Then
            ' Continue For
            'End If
            '190201: 1분 chart만 할꺼니까 t8412로만 하자.
            If Now.Hour = 6 AndAlso Now.Minute > 30 Then
                '야간에만 한다
                While Not (Now.Hour = 15 AndAlso Now.Minute > 32)
                    Threading.Thread.Sleep(100)
                End While
                LoginProcess()
                Threading.Thread.Sleep(2000)
            End If

            '해당 종목 DB가 있는지 보고 없으면 만든다.
            db_name = "CandleChartDB_" & table_name_list(table_index)
            _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS;Integrated Security=SSPI;" & "Initial Catalog=" & db_name)
            Try
                _db_connection.Open()
                'DB 존재함
                db_exist = True
            Catch ex As Exception
                'DB 존재하지 않음
                db_exist = False
            End Try

            If Not db_exist Then
                'DB 생성
                _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS;Integrated Security=SSPI;")

                Try
                    'master DB 접속
                    _db_connection.Open()

                    command_text = "CREATE DATABASE " & db_name & " ON PRIMARY " & _
                          "(NAME = " & db_name & "_main, " & _
                          " FILENAME = '" & _CANDLE_CHART_FOLDER & db_name & "_main.mdf', " & _
                          " FILEGROWTH = 5MB) " & _
                          " LOG ON " & _
                          "(NAME = " & db_name & "_log, " & _
                          " FILENAME = '" & _CANDLE_CHART_FOLDER & db_name & "_log.ldf', " & _
                          " FILEGROWTH = 10%) "
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.ExecuteNonQuery()
                    cmd.Dispose()
                    Threading.Thread.Sleep(10000) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
                Catch ex As Exception
                    'DB 생성에 실패했습니다.
                    MsgBox("DB 생성에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                    _db_connection.Close()              'DB connection 닫기
                    Exit Sub
                End Try
            End If

            _db_connection.Close()              'DB connection 닫기

            'DB 다시 열기 시도
            _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS;Integrated Security=SSPI;" & "Initial Catalog=" & db_name)

            Try
                _db_connection.Open()

                'Candle Chart 테이블 이름 리스트를 만든다.
                data_table = _db_connection.GetSchema("tables")
                candle_table_list.Clear()
                For candle_table_index As Integer = 0 To data_table.Rows.Count - 1
                    candle_table_list.Add(data_table.Rows(candle_table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
                Next

                _TableNameToCheck = table_name_list(table_index)
                If Not candle_table_list.Exists(AddressOf TableExistCheck) Then
                    '테이블 존재하지 않음 => 테이블 생성
                    command_text = "CREATE TABLE " & table_name_list(table_index) & "(CandleTime DATETIME PRIMARY KEY, OpenPrice INT, ClosePrice INT, HighPrice INT, LowPrice INT, Amount BIGINT);"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.ExecuteNonQuery()             '명령 실행
                    cmd.Dispose()
                Else
                    '테이블이 존재하면 => 이미 다입력되었다고 가정하고 다음 table로 가자 (임시로 하는 것임)
#If 0 Then
                    command_text = "SELECT * from " & table_name_list(table_index) & " WHERE CandleTime BETWEEN '2018-11-01 00:00:00' AND '2019-01-31 23:59:59' ORDER BY CandleTime ASC"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    read_result = cmd.ExecuteReader()
                    If read_result.Read Then
                        '데이타가 있다.=>이미 읽어서 저장되었다.=>건너 뛴다.
                        cmd.Dispose()
                        'Continue For
                    Else
                        '데이타가 없다.=>아래로 내려가 Chart data를 읽어 저장하는 작업을 한다.
                        cmd.Dispose()
                    End If
#End If
                End If
            Catch ex As Exception
                '생성에 성공했지만 여는데 실패.. why?
                MsgBox("DB 생성에 재~실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                _db_connection.Close()              'DB connection 닫기
                Exit Sub
            End Try

            'Candle Chart data 받기 위한 작업
            _T8412.ResFileName = "res\t8412.res"
            SymbolCodeOnRequest = table_name_list(table_index).Substring(1, table_name_list(table_index).Length - 1)

            _T8412.SetFieldData("t8412InBlock", "shcode", 0, SymbolCodeOnRequest)      '단축코드
            _T8412.SetFieldData("t8412InBlock", "ncnt", 0, "1L")        '단위(n틱/n분)
            _T8412.SetFieldData("t8412InBlock", "comp_yn", 0, "Y")      '압축여부(Y:압축,N:비압축)
            _T8412.SetFieldData("t8412InBlock", "sdate", 0, "20180701")       '시작일자(일/주/월 해당)
            _T8412.SetFieldData("t8412InBlock", "edate", 0, "20181031")       '종료일자(일/주/월 해당)

            Do
                ResponseReceived = False
                RxProcessCompleted = False
                If CtsDate = "" Then
                    RequestResult = _T8412.Request(False)
                Else
                    _T8412.SetFieldData("t8412InBlock", "cts_date", 0, CtsDate)       '다음 연속일자 입력
                    _T8412.SetFieldData("t8412InBlock", "cts_time", 0, CtsTime)       '다음 연속시간 입력
                    RequestResult = _T8412.Request(True)
                End If
                If RequestResult > 0 Then
                    Dim a = 0
                    Do
                        a = a + 1
                        Threading.Thread.Sleep(10)
                        If a = 3000 Then
                            My.Computer.FileSystem.WriteAllText("ChartData\" & SymbolCodeOnRequest & "_noresponse.txt", "", False)
                            NoResponseNumber = NoResponseNumber + 1
                            _T8412 = New XAQuery
                            LoginProcess()      '로긴을 다시 하고
                            CtsDate = ""        '후속 data 받는 거 없이 다음으로
                            CtsTime = ""        '넘어가기 위한 조치 취하고 가자
                            RxProcessCompleted = True
                            Exit Do
                        End If
                    Loop Until ResponseReceived
                    'Reponse 를 받았으면 이리 내려온다.
                    While Not RxProcessCompleted
                        Threading.Thread.Sleep(10)
                    End While
                    'Data 처리가 다 끝났으면 이리 내려온다.
                    Threading.Thread.Sleep(3000)
                Else
                    '에러나면 빠져나온다..
                    FailedSymbolNumber = FailedSymbolNumber + 1
                    My.Computer.FileSystem.WriteAllText("ChartData\" & SymbolCodeOnRequest & "_failed" & RequestResult.ToString & ".txt", "", False)
                    Exit Do
                End If
            Loop While CtsDate <> "" AndAlso CtsTime <> ""
            CurrentNumber = table_index
            _db_connection.Close()
        Next

        bt_ChartDataUpdate.Enabled = True
        CurrentNumber = TotalNumber
        IsThreadExecuting = False
    End Sub

    'table list에 있는지 체크
    Private Function TableExistCheck(ByVal a_string As String) As Boolean
        If a_string = _TableNameToCheck Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub _T8412_ReceiveData(szTrCode As String) Handles _T8412.ReceiveData
        ResponseReceived = True

        Dim symbol_code As String
        Dim count As Integer
        Dim date_str, time_str, open_str, high_str, low_str, close_str, amount_str As String
        Dim year_str, month_str, day_str, hour_str, minute_str, second_str As String
        Dim line_text As String = ""
        Dim cmd As OleDb.OleDbCommand
        Dim insert_command As String

        symbol_code = _T8412.GetFieldData("t8412OutBlock", "shcode", 0)
        If _T8412.Decompress("t8412OutBlock1") > 0 Then
            count = _T8412.GetBlockCount("t8412OutBlock1")
        Else
            count = 0
        End If
        CtsDate = _T8412.GetFieldData("t8412OutBlock", "cts_date", 0)     ' 연속일자
        CtsTime = _T8412.GetFieldData("t8412OutBlock", "cts_time", 0)     ' 연속시간
        For index As Integer = count - 1 To 0 Step -1
            date_str = _T8412.GetFieldData("t8412OutBlock1", "date", index)     ' 날짜
            time_str = _T8412.GetFieldData("t8412OutBlock1", "time", index)     ' 시간
            year_str = date_str.Substring(0, 4)
            month_str = date_str.Substring(4, 2)
            day_str = date_str.Substring(6, 2)
            hour_str = time_str.Substring(0, 2)
            minute_str = time_str.Substring(2, 2)
            second_str = time_str.Substring(4, 2)

            open_str = _T8412.GetFieldData("t8412OutBlock1", "open", index)     ' 시가
            high_str = _T8412.GetFieldData("t8412OutBlock1", "high", index)     ' 고가
            low_str = _T8412.GetFieldData("t8412OutBlock1", "low", index)     ' 저가
            close_str = _T8412.GetFieldData("t8412OutBlock1", "close", index)     ' 종가
            amount_str = _T8412.GetFieldData("t8412OutBlock1", "jdiff_vol", index)     ' 거래량
            'line_text += date_str & ", " & time_str & ", " & open_str & ", " & high_str & ", " & low_str & ", " & last_str & ", " & amount_str & vbCrLf
            insert_command = "INSERT INTO A" & symbol_code & " (CandleTime, OpenPrice, ClosePrice, HighPrice, LowPrice, Amount) VALUES ('" & year_str & "-" & month_str & "-" & day_str & " " & hour_str & ":" & minute_str & ":" & second_str & "', " & open_str & ", " & close_str & ", " & high_str & ", " & low_str & ", " & amount_str & ");"
            cmd = New OleDb.OleDbCommand(insert_command, _db_connection)
            Try
                cmd.ExecuteNonQuery()              'insert 명령 실행
            Catch ex As Exception
                _CandleChartInsertErrorCount += 1
            End Try
            cmd.Dispose()
        Next

        RxProcessCompleted = True
    End Sub
#End If

    Private Sub Button3_Click_1(sender As Object, e As EventArgs) Handles bt_ChartDataValidate.Click
#If 0 Then
        '차트 데이타 밸리데이션
        Dim chart_data_validate_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDataValidateThread)
        chart_data_validate_thread.IsBackground = True
        chart_data_validate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        tm_FormUpdate.Start()       '관리 timer 시작

        bt_ChartDataValidate.Enabled = False
#Else
        '차트 DB attach
        '차트 데이타 밸리데이션
        Dim chart_data_validate_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDBAttachThread)
        chart_data_validate_thread.IsBackground = True
        chart_data_validate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        'tm_FormUpdate.Start()       '관리 timer 시작

        bt_ChartDataValidate.Enabled = False
#End If
    End Sub

    Private Sub ChartDataValidateThread()
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim read_result As OleDb.OleDbDataReader

        Dim file_list = My.Computer.FileSystem.GetFiles(_CANDLE_CHART_FOLDER)
        Dim file_extension As String
        Dim db_name As String = ""
        Dim table_name As String
        Dim start_date, end_date As DateTime
        Dim start_date_string, end_date_string As String
        Dim this_date As DateTime
        Dim current_date As DateTime
        Dim data_count As Integer
        Dim count_count As Integer
        Dim datA_count_list(9) As Integer
        Dim datE_count_list(9) As Integer
        'TotalNumber = file_list.Count
        'CurrentNumber = 0
        'If cb_AutoUpdate.Checked Then
        start_date = Now.Date - TimeSpan.FromDays(Math.Ceiling(c03_Symbol.MAX_NUMBER_OF_CANDLE / 381)) 'dtp_LastCandleDate.Value
        start_date_string = start_date.ToString("yyyy-MM-dd")
        'Else
        'start_date = dtp_LastCandleDate.Value
        'start_date_string = start_date.ToString("yyyy-MM-dd")
        'End If
        end_date = Now.Date
        end_date_string = end_date.ToString("yyyy-MM-dd")

        For index As Integer = 0 To file_list.Count - 1
            file_extension = IO.Path.GetExtension(file_list(index))
            If file_extension = ".mdf" Then
                Try
                    db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)
                    table_name = db_name.Substring(14, 7)
                    'DB open
                    _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS; Initial Catalog=" & db_name & "; Integrated Security=SSPI;")
                    _db_connection.Open()

                    command_text = "SELECT CandleTime from " & table_name & " WHERE CandleTime BETWEEN '" & start_date_string & " 00:00:00' AND '" & end_date_string & " 23:59:59' ORDER BY CandleTime ASC"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    read_result = cmd.ExecuteReader()

                    '루프 초기화
                    current_date = [DateTime].MinValue
                    data_count = 0
                    count_count = 0
                    For index_for_init As Integer = 0 To 9
                        datA_count_list(index_for_init) = 0
                        datE_count_list(index_for_init) = 0
                    Next
                    Dim count_count_index As Integer = 0
                    While 1
                        If read_result.Read Then
                            '데이타가 있다.
                            this_date = CType(read_result(0), DateTime).Date
                            If this_date <> current_date Then
                                '전까지 카운트했던 것을 기록한다.
                                If current_date = [DateTime].MinValue Then
                                    'initial 값이므로 패쓰
                                Else
                                    count_count_index = 0
                                    While count_count_index < count_count
                                        If datA_count_list(count_count_index) = data_count Then
                                            datE_count_list(count_count_index) += 1
                                            Exit While
                                        End If
                                        count_count_index += 1
                                    End While
                                    If count_count_index = count_count Then
                                        '해당 data_count인 날이 처음이니 새로 기록한다.
                                        datA_count_list(count_count) = data_count
                                        datE_count_list(count_count) = 1
                                        count_count += 1
                                    End If
                                    If data_count <> 381 Then
                                        datE_count_list(count_count - 1) = current_date.Year * 10000 + current_date.Month * 100 + current_date.Day
                                    End If
                                    If (this_date - current_date).Days > 10 Then
                                        '건너뛴 날짜가 너무 길어 뭔가 이상하니 기록한다.
                                        datA_count_list(count_count) = 30000 + (this_date - current_date).Days
                                        datE_count_list(count_count) = this_date.Year * 10000 + this_date.Month * 100 + this_date.Day
                                        count_count += 1
                                    End If
                                End If

                                '새 시작을 준비한다.
                                current_date = this_date
                                data_count = 1
                            Else
                                data_count += 1
                            End If
                        Else
                            '데이타가 없다.
                            cmd.Dispose()
                            Exit While
                        End If
                    End While
                    If current_date = [DateTime].MinValue Then
                        'initial 값이므로 패쓰
                    Else
                        '마지막 날짜 기록 (while 내 기록하는 부분 그대로 카피.. 별로 좋지 못한 디자인이지만 최적화하기 귀찮다)
                        count_count_index = 0
                        While count_count_index < count_count
                            If datA_count_list(count_count_index) = data_count Then
                                datE_count_list(count_count_index) += 1
                                Exit While
                            End If
                            count_count_index += 1
                        End While
                        If count_count_index = count_count Then
                            '해당 data_count인 날이 처음이니 새로 기록한다.
                            datA_count_list(count_count) = data_count
                            datE_count_list(count_count) = 1
                            count_count += 1
                        End If
                        If data_count <> 381 Then
                            datE_count_list(count_count - 1) = current_date.Year * 10000 + current_date.Month * 100 + current_date.Day
                        End If
                        If (this_date - current_date).Days > 10 Then
                            '건너뛴 날짜가 너무 길어 뭔가 이상하니 기록한다.
                            datA_count_list(count_count) = 30000 + (this_date - current_date).Days
                            datE_count_list(count_count) = this_date.Year * 10000 + this_date.Month * 100 + this_date.Day
                            count_count += 1
                        End If
                    End If
                Catch ex As Exception
                    Dim a = 1
                End Try

                '읽은 결과를 여기에서 쓴다.
                For result_index As Integer = 0 To count_count - 1
                    'My.Computer.FileSystem.WriteAllText("CharDataValidateResult.txt", db_name & ", " & result_index & " / " & count_count & ", " & datA_count_list(result_index) & ", " & datE_count_list(result_index) & vbCrLf, True)
                    MessageLogging("CharDataValidate : " & db_name & ", " & result_index & " / " & count_count & ", " & datA_count_list(result_index) & ", " & datE_count_list(result_index))
                Next

                ProgressText1 = index & "/" & file_list.Count
                _db_connection.Close()
            End If
            'CurrentNumber = index
        Next

        'CurrentNumber = TotalNumber
        IsThreadExecuting = False
    End Sub

    Private Sub bt_PCRenewWeeklyLearnConfigure_Click(sender As Object, e As EventArgs) Handles bt_PCRenewWeeklyLearnConfigure.Click
        SimulStartDate = SimulEndDate - TimeSpan.FromDays(120)      '시작날짜 configure
        WeeklyLearning = True                                       '주간학습 여부 configure
        AllowMultipleEntering = False                               'Multiple Entering 허용여부 configure
        MixedLearning = True                                        'Mixed Learning configure
        SmartLearning = False                                       'Smart Learning configure
    End Sub

    Private Sub cb_AllowMultipleEntering_CheckedChanged(sender As Object, e As EventArgs) Handles cb_AllowMultipleEntering.CheckedChanged
        AllowMultipleEntering = cb_AllowMultipleEntering.Checked
    End Sub

    Private Sub dtp_StartDate_ValueChanged(sender As Object, e As EventArgs) Handles dtp_StartDate.ValueChanged
        SimulStartDate = dtp_StartDate.Value
    End Sub

    Private Sub dtp_EndDate_ValueChanged(sender As Object, e As EventArgs) Handles dtp_EndDate.ValueChanged
        SimulEndDate = dtp_EndDate.Value
    End Sub

    Private Sub cb_WeeklyLearning_CheckedChanged(sender As Object, e As EventArgs) Handles cb_WeeklyLearning.CheckedChanged
        WeeklyLearning = cb_WeeklyLearning.Checked
    End Sub

    Private Sub cb_MixedLearning_CheckedChanged(sender As Object, e As EventArgs) Handles cb_MixedLearning.CheckedChanged
        MixedLearning = cb_MixedLearning.Checked
    End Sub

    Private Sub cb_SmarLearning_CheckedChanged(sender As Object, e As EventArgs) Handles cb_SmarLearning.CheckedChanged
        SmartLearning = cb_SmarLearning.Checked
    End Sub

    Private Sub cb_DateRepeat_CheckedChanged(sender As Object, e As EventArgs) Handles cb_DateRepeat.CheckedChanged
        DateRepeat = cb_DateRepeat.Checked
    End Sub

    Private Sub cb_LearnApplyRepeat_CheckedChanged(sender As Object, e As EventArgs) Handles cb_LearnApplyRepeat.CheckedChanged
        LearnApplyRepeat = cb_LearnApplyRepeat.Checked
    End Sub

#If PCRENEW_LEARN Then
    Private Sub tb_Form_SCORE_THRESHOLD_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_SCORE_THRESHOLD.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_SCORE_THRESHOLD.Text)
            MainForm.Form_SCORE_THRESHOLD = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_FALL_SCALE_LOWER_THRESHOLD_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_FALL_SCALE_LOWER_THRESHOLD.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_FALL_SCALE_LOWER_THRESHOLD.Text)
            MainForm.Form_FALL_SCALE_LOWER_THRESHOLD = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_DEFAULT_HAVING_TIME1_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_DEFAULT_HAVING_TIME1.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_DEFAULT_HAVING_TIME1.Text)
            MainForm.Form_DEFAULT_HAVING_TIME1 = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_DEFAULT_HAVING_TIME2_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_DEFAULT_HAVING_TIME2.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_DEFAULT_HAVING_TIME2.Text)
            MainForm.Form_DEFAULT_HAVING_TIME2 = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_DEFAULT_HAVING_TIME3_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_DEFAULT_HAVING_TIME3.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_DEFAULT_HAVING_TIME3.Text)
            MainForm.Form_DEFAULT_HAVING_TIME3 = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_DEFAULT_HAVING_TIME4_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_DEFAULT_HAVING_TIME4.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_DEFAULT_HAVING_TIME4.Text)
            MainForm.Form_DEFAULT_HAVING_TIME4 = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_DEFAULT_HAVING_TIME5_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_DEFAULT_HAVING_TIME5.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_DEFAULT_HAVING_TIME5.Text)
            MainForm.Form_DEFAULT_HAVING_TIME5 = converted_value
        Catch ex As Exception

        End Try
    End Sub

    Private Sub tb_Form_DEFAULT_HAVING_TIME6_TextChanged(sender As Object, e As EventArgs) Handles tb_Form_DEFAULT_HAVING_TIME6.TextChanged
        Dim converted_value As Double
        Try
            converted_value = Convert.ToDouble(tb_Form_DEFAULT_HAVING_TIME6.Text)
            MainForm.Form_DEFAULT_HAVING_TIME6 = converted_value
        Catch ex As Exception

        End Try
    End Sub

#End If

    Private Sub ChartDBAttachThread()
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String

        Dim file_list = My.Computer.FileSystem.GetFiles(_CANDLE_CHART_FOLDER)
        Dim file_extension As String
        Dim db_name As String
        'TotalNumber = file_list.Count
        'CurrentNumber = 0
        _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS;Integrated Security=SSPI;")
        'master DB 접속
        _db_connection.Open()

        For index As Integer = 0 To file_list.Count - 1
            file_extension = IO.Path.GetExtension(file_list(index))
            If file_extension = ".mdf" Then
                Try
                    db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)

                    command_text = "CREATE DATABASE " & db_name & " ON (FILENAME = '" & _CANDLE_CHART_FOLDER & "\" & db_name & "_main.mdf')," _
                          & " (FILENAME = '" & _CANDLE_CHART_FOLDER & "\" & db_name & "_log.ldf') FOR ATTACH;"

                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.ExecuteNonQuery()
                    cmd.Dispose()
                    Threading.Thread.Sleep(100)
                Catch ex As Exception
                    'DB 생성에 실패했습니다.
                    MsgBox("DB 생성에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                    _db_connection.Close()              'DB connection 닫기
                    Exit Sub
                End Try

            End If

            'CurrentNumber = index
            ProgressText1 = index.ToString & " / " & file_list.ToString
        Next

        _db_connection.Close()              'DB connection 닫기

        'CurrentNumber = TotalNumber
        ProgressText1 = file_list.ToString & " / " & file_list.ToString
        IsThreadExecuting = False
    End Sub

    '모든 DB 제거 혹은 모든 DB 등록
    Private Sub RemoveAllChartDB()
        '리스트에서 읽어와서 drop 하자
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim db_name, table_name As String

        Dim table_name_list As New List(Of String)
#If 0 Then
        'Candle DB
        Dim file_list = My.Computer.FileSystem.GetFiles(_CANDLE_CHART_FOLDER)
        For index As Integer = 0 To file_list.Count - 1
            If IO.Path.GetExtension(file_list(index)) = ".mdf" Then
                db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)
                table_name = db_name.Substring(14, 7)
                'If Not table_name_list.Contains(table_name) Then
                table_name_list.Add(table_name)
                'End If
            End If
        Next
#End If
        'Price DB
        Dim file_list = My.Computer.FileSystem.GetFiles("D:\Finance\Database")
        Dim extension, filename As String
        For index As Integer = 0 To file_list.Count - 1
            extension = IO.Path.GetExtension(file_list(index))
            filename = IO.Path.GetFileName(file_list(index))
            If extension = ".mdf" AndAlso filename.Substring(0, 13) = "PriceGangdoDB" Then
                'db_name = filename.Substring(0, 20)
                table_name = filename.Substring(0, 20)
                table_name_list.Add(table_name)
            End If
        Next

        _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS;Integrated Security=SSPI;")
        'master DB 접속
        _db_connection.Open()

        For index As Integer = 0 To table_name_list.Count - 1
            'If index >= 1842 Then
            'Continue For
            'End If
            'DB 삭제 혹은 붙이기
#If 0 Then
            'Chart DB
            db_name = "CandleChartDB_" & table_name_list(index)
#Else
            'Price DB
            db_name = table_name_list(index)
#End If
            Try
#If 0 Then
                command_text = "DROP DATABASE " & db_name
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.ExecuteNonQuery()
                cmd.Dispose()
                MessageLogging("index " & index.ToString & ", " & db_name & " DB 삭제했습니다.")
                Threading.Thread.Sleep(100) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 삭제한지 얼마 안 됐으니까
#Else
#If 0 Then
                'Chart DB
                command_text = "CREATE DATABASE " & db_name & " ON PRIMARY " &
                      "(NAME = " & db_name & "_main, " &
                      " FILENAME = '" & _CANDLE_CHART_FOLDER & db_name & "_main.mdf', " &
                      " FILEGROWTH = 10%) " &
                      " LOG ON " &
                      "(NAME = " & db_name & "_log, " &
                      " FILENAME = '" & _CANDLE_CHART_FOLDER & db_name & "_log.ldf', " &
                      " FILEGROWTH = 10%) FOR ATTACH"
#Else
                'Price DB
                command_text = "CREATE DATABASE " & db_name & " ON PRIMARY " &
                      "(NAME = " & db_name & "_main, " &
                      " FILENAME = '" & "D:\Finance\Database\" & db_name & "_main.mdf', " &
                      " FILEGROWTH = 10%) " &
                      " LOG ON " &
                      "(NAME = " & db_name & "_log, " &
                      " FILENAME = '" & "D:\Finance\Database\" & db_name & "_log.ldf', " &
                      " FILEGROWTH = 10%) FOR ATTACH"
#End If
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.ExecuteNonQuery()
                cmd.Dispose()
                MessageLogging("index " & index.ToString & ", " & db_name & " DB 붙였습니다.")
                Threading.Thread.Sleep(100) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
#End If
            Catch ex As Exception
                'DB 삭제에 실패했습니다.
                If LastExceptionErrorMesssage <> ex.Message Then
                    LastExceptionErrorMesssage = ex.Message
                    MessageLogging("Exception:" & "index " & index.ToString & ex.Message)
                End If
                'MsgBox("DB 삭제에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                _db_connection.Close()              'DB connection 닫기
                'Exit Sub
            End Try

            ProgressText1 = index & "/" & table_name_list.Count
        Next
        _db_connection.Close()

        IsThreadExecuting = False
    End Sub
End Class

Class DetermineLikeRandom
    Private _Index As Integer = 0
    Private _Cycle As Integer = 0
    Public RotationBase As Double = 128.0
    Private _DeterminedArray As Double() = {
        0.0424293424773955,
        0.646096610725201,
        0.284433386471425,
        0.786774698858304,
        0.842683716117376,
        0.405803342171759,
        0.973151080023625,
        0.637101535261989,
        0.284352230465076,
        0.360023265995649,
        0.269664901205948,
        0.543542631742495,
        0.535183267565458,
        0.349090357138697,
        0.122090690717499,
        0.663505311474984,
        0.422426877420931,
        0.152737101314148,
        0.315145126301906,
        0.664889540975627,
        0.612604738695526,
        0.661274501529577,
        0.950905438626008,
        0.0531653129331854,
        0.363556153153715,
        0.625172013786505,
        0.539248134879512,
        0.167028216253024,
        0.864740607339905,
        0.282854358234507,
        0.107708116270696,
        0.205545326486689,
        0.269220176132158,
        0.57022039124327,
        0.248842080885161,
        0.123641744160832,
        0.619594415450709,
        0.610934135776657,
        0.424350348227097,
        0.44302799020807,
        0.744794240134635,
        0.780848484718241,
        0.499899781790924,
        0.550090804504091,
        0.0318826042427175,
        0.381973099636574,
        0.865084662072828,
        0.295559008659333,
        0.905303142771857,
        0.733050812875899,
        0.284541885076771,
        0.458722131837717,
        0.402297967194061,
        0.649638922481046,
        0.794798040698015,
        0.877070107068603,
        0.822104002186364,
        0.660903908974166,
        0.815561834542719,
        0.865356089845838,
        0.422620241340922,
        0.435667813915346,
        0.930138894922307,
        0.540707119566933,
        0.463136225878395,
        0.289041405272924,
        0.0711168787072691,
        0.0115156816781854,
        0.8355530935002,
        0.108075932261964,
        0.0626069357213626,
        0.293747989689881,
        0.234594648993046,
        0.634138087966659,
        0.394746163591723,
        0.897752709542075,
        0.287878848022555,
        0.574094454702686,
        0.497698881139737,
        0.673769443907523,
        0.99041937048705,
        0.310566800633834,
        0.529036805057359,
        0.0161057658371856,
        0.56450483566822,
        0.932313136891389,
        0.014760824337926,
        0.128725279678937,
        0.783735681093625,
        0.165342874306979,
        0.890443075611111,
        0.590578383943987,
        0.234321821079405,
        0.4101382941906,
        0.0382207413740187,
        0.110759714949817,
        0.912758695646337,
        0.286569381968698,
        0.052098242238763,
        0.0689183726965981,
        0.265357114039757,
        0.111637682203385,
        0.828415885117959,
        0.492328322383381,
        0.534145765026941,
        0.755862409119601,
        0.900720207086277,
        0.665868189023562,
        0.271059862724318,
        0.413705214003084,
        0.191181419009963,
        0.232248125873994,
        0.930251606061008,
        0.151160631294247,
        0.0390655581396796,
        0.76792430491302,
        0.420724960167435,
        0.751309340140646,
        0.388875404141786,
        0.0822830677752585,
        0.13491792659499,
        0.17855311679498,
        0.094853787435823,
        0.010697654765311,
        0.188175896824445,
        0.888028164388155,
        0.15977086948196,
        0.765706674134592,
        0.80489273213753,
        0.0954040798548512,
        0.941591021844679,
        0.966859970717422,
        0.920653209582218,
        0.996063753470725,
        0.447326296169767,
        0.958074529140282,
        0.873121334200808,
        0.374805600940105,
        0.8058980832916,
        0.711461427134375,
        0.403972546864997,
        0.785850445750079,
        0.675649060115555,
        0.655508105197717,
        0.430656965732912,
        0.980503588938012,
        0.187663203917205,
        0.468875770020747,
        0.643097556076462,
        0.999551501369261,
        0.682414921321227,
        0.923610055786915,
        0.866301462716994,
        0.414656740238345,
        0.311345834883444,
        0.831405006443539,
        0.757386056295274,
        0.0215838315999238,
        0.449588938586817,
        0.103492879592225,
        0.831194519814435,
        0.93378722633751,
        0.215236899955231,
        0.903495631210756,
        0.418896224928339,
        0.252231452273086,
        0.813007861494653,
        0.640020947890691,
        0.92829803941973,
        0.74087640806342,
        0.922563588833405,
        0.00799422384611881,
        0.783721246119986,
        0.356341605663401,
        0.217395519374079,
        0.555712225511604,
        0.506697218753166,
        0.840499491113275,
        0.782916275765028,
        0.132043183222933,
        0.288549126269265,
        0.874312221548599,
        0.885105365918185,
        0.0948231713718595,
        0.676808382091322,
        0.459205208017352,
        0.128497612190486,
        0.34492401799211,
        0.0269923377511296,
        0.395933054502509,
        0.823879547988316,
        0.0486384674495345,
        0.993411885460927,
        0.0981819418973581,
        0.545224226150684,
        0.800790821273733,
        0.836806829899858,
        0.397698174882696,
        0.849072721259372,
        0.649708411399864,
        0.30500771178845,
        0.677630352309862,
        0.641681338963536,
        0.545449271132735,
        0.60633318732244,
        0.788006332538455,
        0.317155598787962,
        0.791599569318896,
        0.220249048608475,
        0.689285268466712,
        0.570903987091897,
        0.263651718613491,
        0.420585973446439,
        0.275104231118435,
        0.667970925612526,
        0.119388649503804,
        0.231000970034243,
        0.596866159230275,
        0.470677488793828,
        0.396288523990668,
        0.136227020375877,
        0.631244887574337,
        0.38639712770531,
        0.175672864242035,
        0.253797837620387,
        0.317640064351449,
        0.133333080465632,
        0.536323755903804,
        0.279491837663158,
        0.891608629219213,
        0.0837826534115339,
        0.902259435997525,
        0.756426508770651,
        0.162304405772287,
        0.213292571019021,
        0.0219046750763755,
        0.95523200582423,
        0.979641771101964,
        0.679609051691423,
        0.481294525209295,
        0.913702549441549,
        0.144613026311879,
        0.314968606036509,
        0.878584616153099,
        0.874601153242115,
        0.0610159047796452,
        0.40692269275209,
        0.913516980838602,
        0.274440137605088,
        0.857641530543158,
        0.984982740847011,
        0.24790996152397,
        0.7041978987347,
        0.644795674606203,
        0.111307351692192,
        0.21713719660944,
        0.951855903140605,
        0.882768566121319,
        0.349428401676448,
        0.190327893334878,
        0.0947584785540821,
        0.126535535005676,
        0.558340008004521,
        0.613735473359571,
        0.284528695665748,
        0.893038296946857,
        0.886941269432519,
        0.0349613311635627,
        0.322023467879279,
        0.288685564218783,
        0.111643403510106,
        0.48389974722032,
        0.33761422884534,
        0.869206665939857,
        0.162674184434666,
        0.190167176452363,
        0.653806863429405,
        0.861522377254359,
        0.669740729875111,
        0.0324982343761347,
        0.359118169047617,
        0.76364708896881,
        0.580666674641989,
        0.158698534510553,
        0.868048603933053,
        0.139971066505446,
        0.137807944352543,
        0.189185738982673,
        0.515574837805136,
        0.513776130939869,
        0.348261595736494,
        0.0702874578796349,
        0.0208411146479239,
        0.578101488478602,
        0.689952191286022,
        0.355185665998419,
        0.854242862741822,
        0.435324326916174,
        0.67699850644595,
        0.40828151494318,
        0.469348561615663,
        0.279850073245693,
        0.833929767698989,
        0.960905634560982,
        0.0207998708420866,
        0.223694722086216,
        0.338612791218239,
        0.00131337878019588,
        0.275093422941096,
        0.962720382609866,
        0.619616871598856,
        0.00188582367292844,
        0.50531777384817,
        0.693032064633107,
        0.0322965753848488,
        0.238875474515279,
        0.381322240298655,
        0.492910879767763,
        0.323058246970787,
        0.527337123848135,
        0.116483390007698,
        0.510010400099601,
        0.965006609871791,
        0.425017584151847,
        0.986502053798471,
        0.862131330375749,
        0.112309157163962,
        0.131027256885172,
        0.624394425345792,
        0.147240161565994,
        0.590574859160755,
        0.996950195194049,
        0.741794150341944,
        0.906133391512913,
        0.944882070582065,
        0.198566180885607,
        0.9377134795699,
        0.953501473261257,
        0.427891626802702,
        0.442149697229824,
        0.877681603950945,
        0.488426679925328,
        0.350799331735651,
        0.450683626071009,
        0.0109064454549036,
        0.195776724880046,
        0.509853122911556,
        0.741238648340497,
        0.47321173554401,
        0.900741327755565,
        0.955095242022551,
        0.0804130834930459,
        0.634847192786355,
        0.812268299143781,
        0.236446022978667,
        0.165419735607817,
        0.0989208220762969,
        0.538304133736263,
        0.399796675894992,
        0.694002506038322,
        0.713597859455397,
        0.134730491430608,
        0.244591867157864,
        0.282493199374559,
        0.603512777554916,
        0.700158849649752,
        0.157398105981258,
        0.423278498488464,
        0.140128610412976,
        0.109537499715763,
        0.916183748913772,
        0.61597079395518,
        0.923017465678332,
        0.752993483600843,
        0.1100386968142,
        0.649583206877247,
        0.611403483985421,
        0.990021341432788,
        0.396727245880827,
        0.183402839164755,
        0.90219432846059,
        0.767433183594339,
        0.949914807748057,
        0.136678573784401,
        0.913490745932163,
        0.155383469479067,
        0.804260155105878,
        0.353848064845228,
        0.0767034822986173,
        0.676739143373681,
        0.935428193353172,
        0.743441710003961,
        0.134538061244585,
        0.941032862223878,
        0.815158761886218,
        0.800093094269324,
        0.604570583923352,
        0.837070253737022,
        0.686200610872612,
        0.968696816751594,
        0.339743847270803,
        0.927681426405221,
        0.0900592168386192,
        0.92737307452701,
        0.592776876596188,
        0.748347956589147,
        0.0378185413088779,
        0.150955356545666,
        0.223928656237717,
        0.86178038538844,
        0.653687913336712,
        0.850160132352617,
        0.783342609245328,
        0.535306431297137,
        0.499717643384142,
        0.639626330100818,
        0.315962410345411,
        0.239463766756804,
        0.948395126486515,
        0.852976808066668,
        0.0379900481916252,
        0.130370140659483,
        0.473852357510242,
        0.0218767606381702,
        0.540563215883075,
        0.304477296315846,
        0.329640782903415,
        0.196292045541857,
        0.0694370936726351,
        0.290980543967636,
        0.462834901392079,
        0.755705012926498,
        0.73032046271851,
        0.556749073434199,
        0.856613719539225,
        0.717004223230438,
        0.615982892083462,
        0.469023489732996,
        0.275341692530733,
        0.879646801382485,
        0.899126986104417,
        0.650585431876119,
        0.860192142011707,
        0.270080991367009,
        0.208823506568364,
        0.362276298081175,
        0.41784969616702,
        0.0387929021228123,
        0.0167252089175252,
        0.577713261191226,
        0.297323581905729,
        0.242799064435389,
        0.990830970738705,
        0.457325748950343,
        0.793593980652779,
        0.0378163246429898,
        0.0380140774602052,
        0.835762473059521,
        0.996086161616909,
        0.864938846182413,
        0.533812499800185,
        0.14067098422948,
        0.537717098751827,
        0.0272271934659829,
        0.720481841428945,
        0.436140380382933,
        0.286750971149861,
        0.152135426154358,
        0.975114323959971,
        0.397993534273716,
        0.152776909189118,
        0.400232876635128,
        0.763630851223554,
        0.152113173804777,
        0.279916344112752,
        0.07139079614647,
        0.670485511511558,
        0.371511240672008,
        0.658995679193134,
        0.744765011748201,
        0.373105948257353,
        0.323087220843968,
        0.890001039998342,
        0.31662152709957,
        0.144349185559248,
        0.0580021416553838,
        0.461319237814733,
        0.847456544569179,
        0.403966774238152,
        0.740296068929464,
        0.692634865853617,
        0.392663982800389,
        0.740697779581615,
        0.275101742810301,
        0.777444950850619,
        0.966759539078931,
        0.561308292898561,
        0.322375422225842,
        0.0756474711404288,
        0.959311717912121
    }

    Public Sub New()

    End Sub

    Public Function NextDouble() As Double
        Dim next_double As Double = _DeterminedArray(_Index)
        _Index += 1
        If _Index = 500 Then
            _Index = 0
            _Cycle += 1
            If _Cycle = RotationBase Then
                _Cycle = 0
            End If
        End If

        Return (next_double + _Cycle / RotationBase) Mod 1
    End Function
End Class

Class SequenceDetermine
    Private _MaxNumber As Integer
    Private _CurrentNumber As Integer

    Public Sub New(ByVal max_number As Integer)
        _MaxNumber = max_number
        _CurrentNumber = 0 '&HB
    End Sub

    Public Function NextNumber() As Integer
        Dim return_number As Integer = _CurrentNumber
        _CurrentNumber += 1
        'If _CurrentNumber = _MaxNumber Then
        '2024.06.20 : 위와 같이 되어 있어서 그 동안 maxnumber 선택이 안 되었었다. 예를 들면 max number 8일 때 next number 는 0, 1, 2, 3, 4, 5, 6, 0, 1, 2, ... 이런 식으로 됐었다.
        If _CurrentNumber > _MaxNumber Then
            _CurrentNumber = 0
        End If

        Return return_number
    End Function
End Class
