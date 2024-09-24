'Imports DSCBO1Lib
'Imports CPUTILLib
'Imports System.Data.SqlClient
'Imports XA_DATASETLib
Imports System.Xml
Imports System.ServiceModel

Public Class Form1
    Enum _EnumWhatToLoad
        LOAD_SYMBOLCOLLECTION_PASTCANDLE
        LOAD_SYMBOLCOLLECTION
        LOAD_NOTHING
    End Enum

    Enum _EnumCpRestartStep
        CP_RESTART_IDLE
        CP_RESTART_WAIT_CP_END
        CP_RESTART_CHECK_NETWORK
        CP_RESTART_CHECK_CP_STUCK_RESOLVED
    End Enum

    Public CurrentProcess As Process = Process.GetCurrentProcess()
    Public MemoryUsage As Long
    Public MemoryCautionIssued As Boolean = False
    'Private WithEvents _ChartIndex As New XAQuery
    'Public DaishinServerObj As New CpCybos          '어떻게 쓰는 건지 잘 모르겠다. IsConnect가 왜 원하는 대로 안 나오지?
    Private WithEvents _T8412 As New et31_XAQuery_Wrapper()           '190131: 왜 이벤트가 안 걸리는지 모르겠다. 다른 TR을 돌렸을 때는 어떤가 확인해보자.
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
    Public WithEvents xa_session As XA_SESSIONLib.XASession
    Public WithEvents XAReal_SC0 As New et32_XAReal_Wrapper      '주문확인
    Public WithEvents XAReal_SC1 As New et32_XAReal_Wrapper      '체결확인
    Public WithEvents XAReal_SC3 As New et32_XAReal_Wrapper      '취소확인
    Public WithEvents XAReal_SC4 As New et32_XAReal_Wrapper      '주문거부
    Public WithEvents XAReal_News As New et32_XAReal_Wrapper      '실시간 뉴스
    Public MainAccountManager As et1_AccountManager
    Public SubAccountManager As et1_AccountManager
    Public TestAccountManager As et1_AccountManager
    Public Shared SVR_IP_VIRTUAL As String = "demo.etrade.co.kr"
    Public Shared SVR_PORT_VIRTUAL As String = "20001"
    Public Shared USER_ID_VIRTUAL As String = "jeanion"
    Public Shared USER_PASSWORD_VIRTUAL As String = "sejin00"
    Public Shared USER_CERT_VIRTUAL As String = "rhdtkaxkr3937*"
    Public Shared SVR_IP_REAL As String = "hts.ebestsec.co.kr"
    Public Shared SVR_PORT_REAL As String = "20001"
    Public Shared USER_ID_REAL As String = "jeanion"
    Public Shared USER_PASSWORD_REAL As String = "qlfkdjet"
    Public Shared USER_CERT_REAL As String = "rhdtkaxkr3937*"
    Public Const MEMORY_CAUTION_LEVEL As Long = 3500 * 10 ^ 6
    Private Const _STOCK_HI_PRICE As Integer = 100000000
    Private Const _LO_VOLUME As Long = 100000
    Private _Counting As Integer = 0
    Public DBSupporter As c041_DefaultDBSupport
    Public ChartDBSupporter As c042_ChartDBSupport
    Public IsLoadingDone As Boolean
    Private _IsInitializationAfterLoadingDone As Boolean = False
    Private _WhatToLoad As _EnumWhatToLoad
    Public SymbolCollectionDone As Boolean = False
    Public CandleLoadNotFinishedYet As Boolean = True
    'Public RealTimeMode As Boolean
    Public IsThreadExecuting As Boolean
    Private LogFileSaveTimeCount As Integer = 0
    Private WarningFileSaveTimeCount As Integer = 0
    Public SavePhase As Integer = 0
    Public WithEvents stm_PriceClock As New System.Timers.Timer()
    'Public WithEvents stm_PrethresholderActivateTimer As New System.Timers.Timer()
    Public CpRestartStep As _EnumCpRestartStep = _EnumCpRestartStep.CP_RESTART_IDLE
    Public NeedToProcessMAAccount As Boolean = True

    'Public WithEvents stm_OrderTimer As New System.Timers.Timer()
    'Public WithEvents stm_AccountTimer As New System.Timers.Timer()

    '131204 : 5초 타이머로 pre-threshold 종목들 검색이 끝나면 1초후 account timer가 종목별 점수를 매기고 상위권 애들한테 매매 가능하도록 하는 로직 필요


    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        'If RealTimeMode Then
        If IsThreadExecuting Then
            '스레드 실행중엔 종료전에 물어봄
            If MsgBox("스레드 실행중입니다. 진짜로 종료할까요?", MsgBoxStyle.YesNo) = MsgBoxResult.No Then
                e.Cancel = True
                Exit Sub
            End If
        End If
        Select Case MsgBox("진짜 끝낼까요?", MsgBoxStyle.OkCancel)
            Case MsgBoxResult.Ok
                '진짜 끝낸다.
            Case Else
                '끝내는 거 취소한다.
                e.Cancel = True
                '        Case MsgBoxResult.Yes
                '        '저장한다.
                '        DBSupporter.SavePriceInformation()
                '        Case MsgBoxResult.No
                '        '저장 안 하고 버린다
                '        Case Else
                '        '닫는 거 취소
                '        e.Cancel = True
        End Select
        'End If
    End Sub

    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'StoredMessagesMMF 설정
        StoredMessagesMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew("StoredMessagesMMF", 100000)
        'CpStuckMMF 설정
        CpStuckMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew("CpStuckMMF", 1)
        'CpStuckAccesser 설정
        CpStuckAccessor = CpStuckMMF.CreateViewAccessor(0, 1)
        'CybosBasicDataMMF 설정
        CybosBasicDataMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew("CybosBasicDataMMF", SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * 5000)
        Dim cybos_data_accessor = CybosBasicDataMMF.CreateViewAccessor(24, 4)
        Dim initial_number_of_symbols As Integer = 0
        cybos_data_accessor.Write(0, initial_number_of_symbols)     '초기값으로 0을 써넣는다. 이것은 CybosLauncher가 재시작할 경우 베이직 데이터 존재여부 판단에 쓰인다.
        'CybosRealDataMMF 설정
        CybosRealDataMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew("CybosRealDataMMF", SYMBOL_REAL_DATA_SIZE * 5000)
        'CybosRealDataAccesser 설정
        SymTree.CybosRealDataAccesser = CybosRealDataMMF.CreateViewAccessor(0, SYMBOL_REAL_START_OFFSET + SYMBOL_REAL_DATA_SIZE * 5000)

        '대신증권 접속
        'ConnectDaishinServer(True)     'confirm restart 를 true로 하니까 이상하게 안 넘어간다.
#If 0 Then
        '시작되었다는 것을 watchlog에 알린다.
        Try
            My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "log" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", "시작함", True)
        Catch ex As Exception
            'one more try
            Threading.Thread.Sleep(100)
            My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "log" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", "시작함", True)
        End Try

        'Process.Start(LogFileFolder & "\" & "watchlog.exe")
        'confirm the process is really restarted

        WaitUntilDaishinReady()
#End If
        'initialize
        TheRequestFormat = RequestFormat.RF_MARKET_EYE
        If TheRequestFormat = RequestFormat.RF_STOCK_MST2 Then
            MaxNumberOfRequest = 110
        Else 'If TheRequestFormat = RequestFormat.RF_MARKET_EYE Then
            MaxNumberOfRequest = 200
        End If

        MainAccountManager = New et1_AccountManager(0)
        SubAccountManager = New et1_AccountManager(1)
        TestAccountManager = New et1_AccountManager(2)

        DBSupporter = New c041_DefaultDBSupport()       'DB supporter 생성
        If Not DBSupporter.DBStatusOK Then
            MsgBox("DB 설정에 문제가 있어서 프로그램을 종료합니다.")
            '프로그램 종료
            Me.Close()
            Exit Sub
        End If

        'tm_1mClock.Start()          'start 1minute timer
        'StockMst2Obj.SetInputValue(0, tb_StockCode.Text)        '종목 코드 세팅
        GlobalVarInit(Me)                   '전역변수 초기화

        'If MsgBox("실시간 모드로 할래요? (No는 시뮬레이션 모드)", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
        '실시간 모드
        'RealTimeMode = True
        '디폴트로 로긴한다.
        '180204: 증거금률 때문에 XAQuery 를 좀 더 일찍 써야 해서 앞쪽으로 옮겼다.
        LoginProcess()

        ChartDBSupporter = New c042_ChartDBSupport()       'DB supporter 생성

        Dim arguments As String() = Environment.GetCommandLineArgs()
        '아규먼트를 보고 1 이면 LOAD_SYMBOLCOLLECTION_PASTCANDLE 라고 가정하고 간다. <= 2023.01.31 에 해제
        If arguments.Count = 1 Then
#If 1 Then
            Dim message_box_result As MsgBoxResult = MsgBox("Load Past Candles 할 건지 선택하시오." & vbCrLf & "Yes : 한다." & vbCrLf & "No : 안 하고 종목 수집만 한다" & vbCrLf & "Cancel : 아무것도 안 한다.", MsgBoxStyle.YesNoCancel)
            If message_box_result = MsgBoxResult.Yes Then
                _WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION_PASTCANDLE
            ElseIf message_box_result = MsgBoxResult.No Then
                _WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION
            Else 'If message_box_result = MsgBoxResult.Cancel Then
                _WhatToLoad = _EnumWhatToLoad.LOAD_NOTHING
            End If
#End If
            '_WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION_PASTCANDLE
        ElseIf arguments(1) = "1" Then
            _WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION_PASTCANDLE
        Else
            _WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION_PASTCANDLE
        End If

        'SymbolCollection과 LoadPastCandles 함께 별도 thread로 돌림
        Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf LoadingThread)
        simulate_thread.IsBackground = True
        IsLoadingDone = False
        simulate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        IsThreadExecuting = True

        'stm_AccountTimer.Interval = 200            '200m초 타이머 interval 설정
        'stm_AccountTimer.Start()                    '200m초 타이머 돌리기
        'stm_OrderTimer.Interval = 1000                  '1초 설정 (order timer)
        'TabControl1.SelectedTab = TabControl1.TabPages(1)
        'Else
        '시뮬레이션 모드
        'RealTimeMode = False
        'TabControl1.SelectedTab = TabControl1.TabPages(2)
        'End If

        'Form update 200 ms 타이머 시작
        tm_200msClock.Start()

        '5초 타이머 시작
        tm_5sClock.Start()

        '디폴트로 로긴한다.
        '180204: 증거금률 때문에 XAQuery 를 좀 더 일찍 써야 해서 앞쪽으로 옮긴다.
        'LoginProcess()
    End Sub

    Public Sub ConnectDaishinServer(ByVal confirm_restart As Boolean)
        '재연결을 시도한다.
        '서비스 disconnect
        'DaishinServerObj.PlusDisconnect()
        'task killing
        For Each prog As Process In Process.GetProcesses()
            If prog.ProcessName = "CpStart" Then
                prog.Kill()
            End If
            If prog.ProcessName = "coStarter" Then
                prog.Kill()
            End If
            If prog.ProcessName = "DibServer" Then
                prog.Kill()
            End If
        Next

        'confirm the process is really killed
        Dim is_really_killed As Boolean = False
        While Not is_really_killed
            is_really_killed = True
            For Each prog As Process In Process.GetProcesses()
                If prog.ProcessName = "CpStart" Then
                    is_really_killed = False
                End If
                If prog.ProcessName = "coStarter" Then
                    is_really_killed = False
                End If
                If prog.ProcessName = "DibServer" Then
                    is_really_killed = False
                End If
            Next
            Threading.Thread.Sleep(10)
        End While

        '프로그램 재시작
        'Process.Start("C:\DAISHIN\STARTER\ncStarter.exe /prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")
        Process.Start("C:\DAISHIN\STARTER\ncStarter.exe", "/prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")

        'confirm the process is really restarted

        Dim cp_start_checked As Boolean = False
        Dim dib_server_checked As Boolean = False
        While confirm_restart AndAlso Not (cp_start_checked = True AndAlso dib_server_checked = True)
            For Each prog As Process In Process.GetProcesses()
                If prog.ProcessName = "CpStart" Then
                    cp_start_checked = True
                End If
                If prog.ProcessName = "DibServer" Then
                    dib_server_checked = True
                End If
            Next
            Threading.Thread.Sleep(10)
        End While

        'confirm the server is connected
        'DaishinServerObj 의 사용법을 잘 모르겠다. 그냥 에러났을 때 리커넥트 하는 방법으로 가야겠다.
        'While confirm_restart AndAlso Not DaishinServerObj.IsConnect()
#If 0 Then
            For Each prog As Process In Process.GetProcesses()
                If prog.ProcessName = "CpStart.exe" Then
                    Exit While
                End If
            Next
#End If
        'DaishinServerObj = New CpCybos

        'End While
    End Sub

    Public Sub WaitUntilDaishinReady()
        Dim cp_start_checked As Boolean = False
        Dim dib_server_checked As Boolean = False
        'confirm the process is really restarted

        While Not (cp_start_checked = True AndAlso dib_server_checked = True)
            For Each prog As Process In Process.GetProcesses()
                If prog.ProcessName = "CpStart" Then
                    cp_start_checked = True
                End If
                If prog.ProcessName = "DibServer" Then
                    dib_server_checked = True
                End If
            Next
            Threading.Thread.Sleep(10)
        End While
    End Sub

    Private Sub LoadingThread()
#If 0 Then
        '2022.08.31: FileStream 을 사용한 Candle 저장 test
        Dim candle_file_system As New c06a_CandleFileSystem(1000)

        Dim candle_service As New c06_CandleLink(candle_file_system)
        candle_service.Initialize(500)
        Dim first_candle As CandleServiceInterfacePrj.CandleStructure
        first_candle.Amount = 123
        first_candle.High = 1000
        first_candle.CandleTime = Now
        candle_service.AddCandle(first_candle)
        Dim second_candle = first_candle
        second_candle.Amount = 234
        second_candle.High = 2999
        candle_service.AddCandle(second_candle)
        second_candle.High = 3999
        candle_service.AddCandle(second_candle)
        second_candle.High = 4999
        candle_service.AddCandle(second_candle)
        second_candle.High = 5999
        candle_service.AddCandle(second_candle)
        second_candle.High = 6999
        candle_service.AddCandle(second_candle)
        candle_service.RemoveCandle()
        candle_service.RemoveCandle()
        candle_service.RemoveCandle()
        candle_service.RemoveCandle()
        candle_service.RemoveCandle()
        For index As Integer = 0 To 9601 - 5
            second_candle.High += 1
            candle_service.AddCandle(second_candle)
        Next
        Dim number_of_candle As Integer = candle_service.CandleCount
        number_of_candle = candle_service.CandleCount
        Dim read_candle = candle_service.Candle(0)
#End If

        If _WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION_PASTCANDLE Then
            'CybosLauncher를 실행한다.
            Process.Start("E:\Development\CybosLauncher\CybosLauncher\bin\Release\CybosLauncher.exe")
            'CybosLauncher가 기본 정보를 전송할 때까지 기다린다.
            While Not Threading.Mutex.TryOpenExisting("CybosBasicDataMutex", CybosBasicDataMutex)
                System.Threading.Thread.Sleep(100)
            End While

            '심볼리스트를 작성한다.
            SymbolCollection()

            SymbolCollectionDone = True
            MainAccountManager.ImmediateBuy = False
            MainAccountManager.ScoreByFailureCount = True
            MainAccountManager.SortByScore = True
            SubAccountManager.ImmediateBuy = False
            SubAccountManager.ScoreByFailureCount = True
            SubAccountManager.SortByScore = True
            TestAccountManager.SortByScore = True
            '저장된 진행중인 걸린애들을 불러온다.
            LoadSavedStocks()

            'Symbol Collection 끝나고 Load Past Candles 
            '190526: SymbolCollection 끝나고 종목별 clock 공급되도록 손써야 한다
            ChartDBSupporter.LoadPastCandles()
#If 1 Then
            '2021.07.08: 현재 걸린 애들이 돌고 있는 종목은 RequestPriceInfo 한다.
            '2021.07.08: 그럴까 했으나, 장초반 과부하 걸릴 것 같아 안 하기로 한다. 주문 들어가면 그 때서 하겠지
            '2021.07.08: 생각해보니 실시간에서 걸렸을 때도 걸리자마자 바로 호가요청하는데, 똑같이 하려면 여기서 바로 호가요청 하는 게 맞다고 보인다.

            Dim decider_list As List(Of c05G_MovingAverageDifference)
            Dim decider As c05G_MovingAverageDifference
            Dim need_to_request_price As Boolean
            For Each a_symbol In SymbolList
                need_to_request_price = False
#If 0 Then
                For main_decider_list_index As Integer = 0 To a_symbol.MainDecisionMakerCenter.Count - 1
                    decider_list = a_symbol.MainDecisionMakerCenter(main_decider_list_index)
                    For Each decider In decider_list
                        If decider.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                            need_to_request_price = True
                        End If
                    Next
                Next
#End If
                For sub_decider_list_index As Integer = 0 To a_symbol.SubDecisionMakerCenter.Count - 1
                    decider_list = a_symbol.SubDecisionMakerCenter(sub_decider_list_index)
                    For Each decider In decider_list
                        If decider.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                            need_to_request_price = True
                        End If
                    Next
                Next
                If need_to_request_price Then
                    a_symbol.RequestPriceinfo()
                End If
            Next
#End If
            StockRestoring()

            'cybos real data mutex를 open한다. 진작에 CybosLauncher쪽에서 생성했기 때문에 바로 넘어갈 것이다.
            While Not Threading.Mutex.TryOpenExisting("CybosRealDataMutex", CybosRealDataMutex)
                System.Threading.Thread.Sleep(100)
            End While

            CandleLoadNotFinishedYet = False
        ElseIf _WhatToLoad = _EnumWhatToLoad.LOAD_SYMBOLCOLLECTION Then
            'CybosLauncher를 실행한다.
            Process.Start("E:\Development\CybosLauncher\CybosLauncher\bin\Release\CybosLauncher.exe")
            'CybosLauncher가 기본 정보를 전송할 때까지 기다린다.
            While Not Threading.Mutex.TryOpenExisting("CybosBasicDataMutex", CybosBasicDataMutex)
                System.Threading.Thread.Sleep(100)
            End While

            '심볼리스트를 작성한다.
            SymbolCollection()

            'cybos real data mutex를 open한다. 진작에 CybosLauncher쪽에서 생성했기 때문에 바로 넘어갈 것이다.
            While Not Threading.Mutex.TryOpenExisting("CybosRealDataMutex", CybosRealDataMutex)
                System.Threading.Thread.Sleep(100)
            End While
        End If

        IsLoadingDone = True
        IsThreadExecuting = False
    End Sub

    '조회한 잔고와 현재 걸린애들을 매칭시킨다.
    Private Sub StockRestoring()
        For index As Integer = 0 To SymbolList.Count - 1
            SymbolList(index).LoadStock()
        Next
        '191029_TODO: 위에서 stock을 잘라갔는데도 잔고가 연결이 안 되면 warning 하게 함.

        If MainAccountManager.StoredStockList.Count > 0 Then
            ErrorLogging("Main 이상함. 연결이 안 된 잔고주식이 있음 (안 잘라갔음).")
            'WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & " Main 미연결 잔고:")
            For index As Integer = 0 To MainAccountManager.StoredStockList.Count - 1
                MessageLogging(MainAccountManager.StoredStockList(index).Code & " : " & MainAccountManager.StoredStockList(index).Quantity & " 개")
                WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & " Main 미연결 잔고:" & MainAccountManager.StoredStockList(index).Code & " " & MainAccountManager.StoredStockList(index).MA_Base & " " & MainAccountManager.StoredStockList(index).Quantity & "개")
            Next
            'WarningLogging(vbCrLf)
        End If
        If SubAccountManager.StoredStockList.Count > 0 Then
            ErrorLogging("Sub 이상함. 연결이 안 된 잔고주식이 있음 (안 잘라갔음).")
            'WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & " Sub 미연결 잔고:")
            For index As Integer = 0 To SubAccountManager.StoredStockList.Count - 1
                Select Case SubAccountManager.StoredStockList(index).Code
                    Case "A053590" '한국테크놀로지
                    Case "A078130" '국일제지
                    Case "A068940" '셀피글로벌
                    Case "A214870" '뉴지랩파마
                    Case "A217480" '에스디생명공학
                    Case "A101140" '인바이오젠
                    Case "A117670" '알파홀딩스
                    Case "A290380" '대유
                    Case "A044060" '조광ILI
                    Case "A089530" '에이티세미콘
                    Case "A096040" '이트론
                    Case "A001140" '국보
                    Case "A016790" '카나리아바이오
                    Case "A096610" '알에프세미
                    Case "A217620" '디딤이앤에프
                        'Nothing
                    Case Else
                        MessageLogging(SubAccountManager.StoredStockList(index).Code & " : " & SubAccountManager.StoredStockList(index).Quantity & " 개")
                        WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & " Sub 미연결 잔고:" & SubAccountManager.StoredStockList(index).Code & " " & SubAccountManager.StoredStockList(index).MA_Base & " " & SubAccountManager.StoredStockList(index).Quantity & "개")
                End Select
            Next
            'WarningLogging(vbCrLf)
        End If

    End Sub


#If 0 Then
    Private Sub tm_1mClock_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles tm_1mClock.Tick
#If 0 Then
        Dim ret As Short = StockMst2Obj.BlockRequest()      '데이터 요청

        If ret = 0 Then
            '정상
            Dim display_text As String = StockMst2Obj.GetDataValue(0, 0) & ", " & StockMst2Obj.GetDataValue(1, 0) & ", " & StockMst2Obj.GetDataValue(3, 0) & vbCrLf
            tb_Display.AppendText(Now.TimeOfDay.ToString & " " & display_text)
        Else
            '비정상
            MsgBox("비정상")
        End If
#End If
        _Counting += 1
        StockBidObj = New StockBid()
        StockBidObj.SetInputValue(0, tb_StockCode.Text)             '종목 코드 세팅
        StockBidObj.SetInputValue(1, 0)                             '??
        StockBidObj.SetInputValue(2, 80)                            '요청 갯수
        StockBidObj.SetInputValue(3, Asc("C"))                           '체결가 비교 방식
        If _Counting = 1 Then
            StockBidObj.SetInputValue(4, "1030")        '시간 검색 입력
        Else
            StockBidObj.SetInputValue(4, "1331")        '시간 검색 입력
        End If
        StockBidObj.BlockRequest()             '데이터 요청

        Dim symbol_code As String = StockBidObj.GetHeaderValue(0)           '종목코드
        Dim total_sel_amount As Long = StockBidObj.GetHeaderValue(3)            '누적 매도체결량
        Dim total_buy_amount As Long = StockBidObj.GetHeaderValue(4)            '누적 매수체결량


        Dim display_text As String = symbol_code & " : " & total_sel_amount.ToString & " , " & total_buy_amount.ToString & vbCrLf
        tb_Display.AppendText(Now.TimeOfDay.ToString & " " & display_text)

        'tb_Display.AppendText(StockBidObj.GetDataValue(0, 0))

        StockBidObj = Nothing
    End Sub
#End If

    Private Sub SymbolCollection()
        'data access 준비한다.
        Dim cybos_data_accessor = CybosBasicDataMMF.CreateViewAccessor(0, SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * 5000)
        Dim number_of_symbols As Integer
        Dim symbol As c03_Symbol
        Dim code_buff(11) As Byte
        Dim name_buff(19) As Byte
        Dim market_kind_int As Integer
        Dim caution As Integer
        Dim supervision As Integer
        Dim open_price As UInt32
        Dim low_limit_price As Double
        Dim yester_price As UInt32
        cybos_data_accessor.Read(0, MarketStartTime)
        cybos_data_accessor.Read(4, MarketEndTime)
        cybos_data_accessor.Read(8, MarketStartHour)
        cybos_data_accessor.Read(12, MarketStartMinute)
        cybos_data_accessor.Read(16, MarketEndHour)
        cybos_data_accessor.Read(20, MarketEndMinute)
        cybos_data_accessor.Read(24, number_of_symbols)

        '종목별 캔들리스트 작성
        'CandleServiceCenter = New WcfCandleManage.CandleService(number_of_symbols)
        'Dim myBinding As New NetNamedPipeBinding
        'Dim myEndpoint As New EndpointAddress("net.pipe://localhost/CandleService/service")
        'Dim myChannelFactory As New ChannelFactory(Of CandleServiceInterfacePrj.ICandleService)(myBinding, myEndpoint)
        Try
            'CybosCandleStoreMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("CybosCandleStoreMMF")
#If 0 Then
            Dim temp_candle_link As New c06_CandleLink
            temp_candle_link.Initialize(0)
            Dim a As CandleServiceInterfacePrj.CandleStructure
            a.Amount = 10
            a.High = 1000
            temp_candle_link.AddCandle(a)
            a.Amount = 20
            a.High = 900
            temp_candle_link.AddCandle(a)
            Dim b As Integer = temp_candle_link.CandleCount
            temp_candle_link.RemoveCandle()
            a.Amount += b
            temp_candle_link.AddCandle(a)
            Dim a1 As CandleServiceInterfacePrj.CandleStructure
            a1 = temp_candle_link.LastCandle
            Dim a2 = temp_candle_link.Candle(1)
#End If

        Catch ex As Exception

        End Try
#If 0 Then
        'File을 이용한 candle service 장치 초기화
        SymTree.CandleFileSystem = New c06a_CandleFileSystem(number_of_symbols)
#End If



        ' Create a channel.
        'CandleServiceCenter = myChannelFactory.CreateChannel()
        'CandleServiceCenter.Initialize(number_of_symbols)

        ProgressText1 = "Symbol Collection step - "
        ProgressText2 = "0 / " & number_of_symbols
        ProgressText3 = ""
        For index As Integer = 0 To number_of_symbols - 1
            cybos_data_accessor.ReadArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index, code_buff, 0, 12)        'length:12
            cybos_data_accessor.ReadArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 12, name_buff, 0, 20)       'length:20
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 32, market_kind_int)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 36, caution)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 40, supervision)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 44, open_price)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 48, low_limit_price)       'length:8
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 56, yester_price)       'length:4

            symbol = New c03_Symbol(System.Text.Encoding.UTF8.GetString(code_buff).Replace(vbNullChar, ""), System.Text.Encoding.UTF8.GetString(name_buff).Replace(vbNullChar, ""), index)
            symbol.MarketKind = CType(market_kind_int, MARKET_KIND)
            symbol.Caution = CType(caution, Boolean)
            symbol.Supervision = CType(supervision, Boolean)
            symbol.OpenPrice = open_price
            symbol.LowLimitPrice = low_limit_price
            symbol.YesterPrice = yester_price
            SymbolList.Add(symbol)
            ProgressText2 = index & " / " & number_of_symbols
        Next

        SymTree.FinishSymbolListing()           '종목 모으기 끝

        ProgressText1 = "Number of symbols : " & number_of_symbols
    End Sub

    '131014 : 15초 timer를 5초 timer로 바꾸는 작업 개시.
    '5초마다 종목시세 업데이트
    Private ccc As Integer = 0
    Private clock_div As Integer = 0
    Private CSPAQ22200_clock As Integer = 0
    Private Sub stm_PriceClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stm_PriceClock.Elapsed
        '180125: stm 타이머를 몇 개 쓰고 있는 것 같은데 정말 안 겹치게 쓰고 있나 확인이 필요하다.
        'SafeEnterTrace(DebugMsgKey, 6)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & " PriceClockStart" & vbCrLf, True)
        'SafeLeaveTrace(DebugMsgKey, 7)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        If LoggedButAccountNotRequested Then
            '계좌 manager Login process 실행 
            XAReal_SC0.ResFileName = "Res\SC0.res"
            XAReal_SC0.AdviseRealData()     '체결확인 실시간 시작
            XAReal_SC1.ResFileName = "Res\SC1.res"
            XAReal_SC1.AdviseRealData()     '체결확인 실시간 시작
            XAReal_SC3.ResFileName = "Res\SC3.res"
            XAReal_SC3.AdviseRealData()     '체결확인 실시간 시작
            XAReal_SC4.ResFileName = "Res\SC4.res"
            XAReal_SC4.AdviseRealData()     '체결확인 실시간 시작
            XAReal_News.ResFileName = "Res\NWS.res"
            XAReal_News.SetFieldData("InBlock", "nwcode", "NWS001")
            XAReal_News.AdviseRealData()     '뉴스 수신 시작
            LoggedButAccountNotRequested = False
        End If
        '2023.11.11 : 여기 있던 계좌 조회 로직은 200ms timer 쪽으로 옮겨갔다.

        If IsMarketTime AndAlso Not IsThreadExecuting Then
            '장중이고 로딩 thread 등 돌아가지 않을 때면
            SymTree.ClockSupply()           '시세 업데이트 클락 공급

            'stm_PrethresholderActivateTimer.Start()     '1초후 pre thresholder들에게 호가 요청하기 위한 타이머 시작
            '2023.11.11 : 5초마다 한 번씩 돌리는 것은 sub account 만 돌리기로 하면서부터 stm_PrethresholderActivateTimer 는 필요가 없어졌다. 아래 한 줄을 타이머 필요없이 바로 여기서 실행한다.
            If NeedToProcessMAAccount Then
                '2024.01.30 : 5초마다 하는 것은 너무 자주다. 1분마다 candle update 되었을 때마다 하면 되겠다.
                SymTree.AccountTaskToQueue(SubAccountManager)
                NeedToProcessMAAccount = False
            End If
        End If
        'SafeEnterTrace(DebugMsgKey, 8)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & " PriceClockEnd" & vbCrLf, True)
        'SafeLeaveTrace(DebugMsgKey, 9)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

#If 0 Then
    '시세 정보 받을 때 prethresholder들은 T1101 요청하고, 1초후 pre-threshold symbol searching하여 list만들고 주문까지 함
    Private Sub stm_PrethresholderActivateTimer_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles stm_PrethresholderActivateTimer.Elapsed
        If IsMarketTime Then
            'SafeEnterTrace(DebugMsgKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & " AccountStart" & vbCrLf, True)
            'SafeLeaveTrace(DebugMsgKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

            'SymTree.AccountTaskToIndicator()        'Thread 관리를 위해 통합 thread가 존재하는 Symtree에 account task를 의뢰한다.
            '2023.11.11 : 이제부터 MA 계좌(sub 계좌) 만 5초 마다 주기저으로 account 처리작업을 하고 나머지 계좌들은 event 가 있을 때에만 account 처리작업을 하기로 한다.
            SymTree.AccountTaskToQueue(MainAccountManager)
            'MessageLogging("Main Search")
            'MainAccountManager.SymbolSearching()        '매매할 종목 찾기
            'MessageLogging("Test Search")
            'TestAccountManager.SymbolSearching()        '매매할 종목 찾기

            '131213 : Prethreshold 리스트 윗부분부터 돈 되는대로 주문 건다.
            '131216 : operation 객체가 다시 decision 객체에 소속되는 것이 필요할 것이다. 왜냐면 매매 시점은 decision maker가
            '  결정하기 때문이다. account 객체가 하는 일은 각 매매단위에 돈을 분배하는 일이다. 1차로 분배하고나서도
            '  돈이 많이 남는다면 2차로 더 싼가격으로 매수 시도를 하는 방법을 고려하도록 한다. 현재 매수 올려놓은 operation들에 대한
            '  정보는 account 객체도 리스트를 갖고 있어야 할 듯 하다.
            '131217 : Prethreshold는 매수가 된 후 청산을 기다리고 있는 시점에서도 유지하는 것이 좋을 듯 하다.
            '  그래야 2차로 돈 넣는 것이 수월할 듯 하다.
            stm_PrethresholderActivateTimer.Stop()    '1초 타이머 종료

            'MessageLogging("Main Distribute")
            'MainAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
            'MessageLogging("Test Distribute")
            'TestAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
            'stm_OrderTimer.Start()               '또다른 1초 타이머인 주문 타이머 시작
            '140123 : 주문타이머 만료 루틴에서 순위를 매기고 주문을 한다.

            '140207 : Order timer 정리한 것 같은데 저장 안 됐나보다. 정리하고, SymbolSearching이랑 MoneyDistribute랑 타이밍 정하자.
            'SafeEnterTrace(DebugMsgKey, 12)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & " AccountEnd" & vbCrLf, True)
            'SafeLeaveTrace(DebugMsgKey, 13)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End If
    End Sub
#End If

    '주문 타이머 만료
    'Private Sub stm_OrderTimer_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles stm_OrderTimer.Elapsed
    'If IsMarketTime Then
    'AccountManager.MoneyDistribute()        '매수를 위한 돈 분배

    'End If
    'End Sub

    '200m초마다 pre-threshold 등록된 종목들에 대한 점수 산정 및 주문 조작
    'Private Sub stm_AccountTimer_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles stm_AccountTimer.Elapsed
    'If IsMarketTime Then
    'Prethresholder를 다시 검사한다.

    'End If
    'End Sub
    '190816:TODO-DONE restored stock 읽고나서 하나하나 출력하기
    '190816:TODO-DONE 매수율을 왜 infinity가 나왔을까 분석해라

    'Form update timer
    'Public TestSymbolRequest As Integer = 0
    Private Sub tm_200msClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tm_200msClock.Tick
        'If SymbolList.Count > 1 Then
        'SymbolList(TestSymbolRequest).RequestPriceinfo()
        'TestSymbolRequest += 1
        'If TestSymbolRequest = SymbolList.Count Then
        'TestSymbolRequest = 0
        'End If
        'End If
        CurrentProcess = Process.GetCurrentProcess
        MemoryUsage = CurrentProcess.PrivateMemorySize64
        lb_MemoryUsage.Text = "Memory Usage : " & MemoryUsage.ToString("N0")
        If MemoryUsage >= MEMORY_CAUTION_LEVEL AndAlso Not MemoryCautionIssued Then
            MemoryCautionIssued = True
            WarningLogging("MainForm Memory Usage : " & MemoryUsage.ToString("N0") & " , MA 전략 걸리는 조건 제약받을 것임.")
        End If

        If IsLoadingDone Then
            If CpRestartStep = _EnumCpRestartStep.CP_RESTART_IDLE Then
                Dim is_stuck As Byte
                CpStuckAccessor.Read(0, is_stuck)
                If is_stuck = 1 Then
                    'CybosLauncher가 없어지길 기다렸다가 없어지면 www.google.com 을 ping 해보고 연결이 확인되면 CybosLauncher를 다시 launching 한다.
                    MessageLogging("MainForm CybosLauncher STUCK됨")
                    WarningLogging("MainForm CybosLauncher STUCK됨. 재시작 시도함.")
                    CpRestartStep = _EnumCpRestartStep.CP_RESTART_WAIT_CP_END
                End If
            End If
            If CpRestartStep = _EnumCpRestartStep.CP_RESTART_WAIT_CP_END Then
                Dim IsCpAlive As Boolean = False
                For Each prog As Process In Process.GetProcesses()
                    If prog.ProcessName = "CybosLauncher" Then
                        IsCpAlive = True
                        Exit For
                    End If
                Next
                If Not IsCpAlive Then
                    MessageLogging("MainForm CybosLauncher 종료됨")
                    CpRestartStep = _EnumCpRestartStep.CP_RESTART_CHECK_NETWORK
                End If
            End If
            If CpRestartStep = _EnumCpRestartStep.CP_RESTART_CHECK_NETWORK Then
                '2021.10.25: 네트웍이 심각하게 안 되면 아래 ping 할 때 exception error 날 수 있다.
                Try
                    If My.Computer.Network.Ping("www.google.com") = True Then
                        CybosRealDataMutex = Nothing    '기존 real data mutex를 버린다.

                        Process.Start("E:\Development\CybosLauncher\CybosLauncher\bin\Release\CybosLauncher.exe")
                        MessageLogging("MainForm CybosLauncher 재시작됨")
                        CpRestartStep = _EnumCpRestartStep.CP_RESTART_CHECK_CP_STUCK_RESOLVED
                    End If
                Catch ex As Exception
                    'nothing
                End Try
            End If
            If CpRestartStep = _EnumCpRestartStep.CP_RESTART_CHECK_CP_STUCK_RESOLVED Then
                Dim is_stuck As Byte
                CpStuckAccessor.Read(0, is_stuck)
                Dim is_real_data_mutex_open As Boolean = Threading.Mutex.TryOpenExisting("CybosRealDataMutex", CybosRealDataMutex)
                If (is_stuck = 0) AndAlso is_real_data_mutex_open Then
                    MessageLogging("MainForm CybosLauncher 재가동완료")
                    '실시간 가격 모니터링 하는 종목 모니터링 재시작
                    For index As Integer = 0 To SymbolList.Count - 1
                        SymbolList(index).RestartPriceMonitoring()
                    Next
                    CpRestartStep = _EnumCpRestartStep.CP_RESTART_IDLE
                End If
            End If
        End If
        If IsLoadingDone AndAlso Not _IsInitializationAfterLoadingDone Then
            'LoginProcess()  '2022.10.16: 캔들로딩이 시간이 오래 걸리기 때문에 로긴이 풀렸을 경우 다시 로긴해주도록 한다.
            '가격 타이머 시작
            stm_PriceClock_Tick(Nothing, Nothing)       'Get the very first data
            stm_PriceClock.Interval = 5000           '5초 타이머 interval 설정 '20200329: 이상한 일이다. 5000으로 설정하고 그 동안 아무일 없이 써왔는데, 윈도우즈 10 으로 바꾸고 5000이 4.5~4.7초 정도로 나온다. 뭐야 이게... 
            stm_PriceClock.Start()                     '5초 타이머 돌리기
            'stm_PrethresholderActivateTimer.Interval = 1000     '1초 설정 (pre threshold activate하는 timer)
            _IsInitializationAfterLoadingDone = True
        End If

        If IsLoadingDone Then
#If 0 Then
            '2024.02.24 : SymbolSearchService testing
            Dim test_symbol_code As String
            Dim test_search_count As Integer
            Dim symbol As c03_Symbol

            Dim result_count As New List(Of Integer)
            Dim result_symbol As New List(Of c03_Symbol)
            For index As Integer = 2700 To 3000
                test_symbol_code = SymbolList(index).Code
                test_search_count = 0
                symbol = SymbolSearchService(test_symbol_code, test_search_count)
                result_count.Add(test_search_count)
                result_symbol.Add(symbol)
            Next
#End If


            If bt_ChartDataUpdate.Enabled = True Then
                'Chart Update 안 할 때 그리고 로그인 되었을 때만만 계좌조회한다. 
                clock_div += 1
                If clock_div = 26 Then
                    '20200330: 5초당 1회로 계좌 조회 횟수가 제한되어 있어서 그냥 10초에 1회씩 하는 걸로 했다. 3개 계좌 돌릴 경우 30초에 1회씩 돌아오지만, 에이 그러면 어떠냐...
                    '2023.11.11 : 계좌정보 업데이트 시간 간격을 최소로 줄이기 위하여 5.2 초당 1회 Sub 와 Test 계좌를 번갈아가며 조회하자. Main 계좌는 이제 안 한다.
                    clock_div = 0
                    Dim request_result As Integer
                    If ccc = 0 Then
                        request_result = SubAccountManager.RequestAccountInfo_CSPAQ12200() ' 그냥 여기서 계좌정보 조회한다. 1초 후면 받겠지
                        ccc = 1
                        If request_result = -1 Then
                            Try
                                LoginProcess()
                            Catch ex As Exception
                                ErrorLogging("MainForm 계좌정보 조회하다 실패해서 로그인시도하는데 익셉션")
                            End Try
                        End If
                    ElseIf ccc = 1 Then
                        request_result = TestAccountManager.RequestAccountInfo_CSPAQ12200() ' 그냥 여기서 계좌정보 조회한다. 1초 후면 받겠지
                        ccc = 0
                        If request_result = -1 Then
                            '2021.08.20: 아래로 들어가 System.Runtime.InteropServices.SEHException 발생.이렇게 하는 게 맞는지 재고 필요
                            '2021.08.20:System.Runtime.InteropServices.SEHException
                            '2021.08.20:HResult = 0x80004005
                            '2021.08.20:메시지 = 외부 구성 요소에서 예외를 Throw했습니다.
                            '2021.08.25: Try Catch로 handling 하기로 함.
                            Try
                                LoginProcess()
                            Catch ex As Exception
                                ErrorLogging("MainForm 계좌정보 조회하다 실패해서 로그인시도하는데 익셉션")
                            End Try
                        End If
                    End If
                End If

                '2024.01.18 : CSPAQ22200 계좌정보조회 start
                CSPAQ22200_clock += 1
                If CSPAQ22200_clock = 15 Then
                    TestAccountManager.RequestAccountInfo_CSPAQ22200()  '3초마다 한 번씩 돌려 증거금100주문가능금액을 업데이트한다.
                    CSPAQ22200_clock = 0
                End If
            End If
            'TestAccountManager.RequestAccountInfo(TestAccountString, TestAccountPW) ' Test 계좌도 여기서 조회하면 횟수 제한 땜에 에러난다. 딴데서 해야 된다. Clock supply 중간에 하는 것이 좋겠다.

        End If

        'MessageLogging("C_En")
        '130724: MessageDisplay를 이거 아래로 내려볼까? Save용 List 어떻게 되나 보게...
        If IsMarketTime AndAlso Not IsThreadExecuting Then
            '190526: 마켓타임이고 로딩 thread 등 안 돌아갈 때만 operator 객체에 200ms tick 공급
            Dim main_decision_maker_list As List(Of c05G_DoubleFall)
            Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)
            Dim test_decision_maker_list As List(Of c05F_FlexiblePCRenew)
            For index As Integer = 0 To SymbolList.Count - 1
                '각 stock operator에 200ms clock 공급
                SafeEnterTrace(SymbolList(index).OneKey, 145)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                'MAIN
                For index_decision_center As Integer = 0 To MAIN_NUMBER_OF_DECIDERS - 1
                    main_decision_maker_list = SymbolList(index).MainDecisionMakerCenter(index_decision_center)
                    For index2 As Integer = 0 To main_decision_maker_list.Count - 1
                        If main_decision_maker_list(index2).StockOperator IsNot Nothing Then '아웃오브 인덱스 에러난부분 20160322
                            main_decision_maker_list(index2).StockOperator.Tm200ms_Tick()
                        End If
                    Next
                Next
                If SubAccount Then
                    'SUB
                    For index_decision_center As Integer = 0 To SUB_NUMBER_OF_DECIDERS - 1
                        sub_decision_maker_list = SymbolList(index).SubDecisionMakerCenter(index_decision_center)
                        For index2 As Integer = 0 To sub_decision_maker_list.Count - 1
                            If sub_decision_maker_list(index2).StockOperator IsNot Nothing Then '아웃오브 인덱스 에러난부분 20160322
                                sub_decision_maker_list(index2).StockOperator.Tm200ms_Tick()
                            End If
                        Next
                    Next
                End If
                'TEST
                For index_decision_center As Integer = 0 To TEST_NUMBER_OF_DECIDERS - 1
                    test_decision_maker_list = SymbolList(index).TestDecisionMakerCenter(index_decision_center)
                    For index2 As Integer = 0 To test_decision_maker_list.Count - 1
                        If test_decision_maker_list(index2).StockOperator IsNot Nothing Then '아웃오브 인덱스 에러난부분 20160322
                            test_decision_maker_list(index2).StockOperator.Tm200ms_Tick()
                        End If
                    Next
                Next
                SafeLeaveTrace(SymbolList(index).OneKey, 144)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Next
        End If

        If IsThreadExecuting Then
            If Not IsLoadingDone Then
                If ChartDBSupporter.DBProgressMax = 0 Then
                    ProgressText3 = ""
                Else
                    ProgressText3 = "Load past candles: " & ChartDBSupporter.DBProgressValue & " / " & ChartDBSupporter.DBProgressMax
                End If
            End If

            'If bt_ChartDataUpdate.Enabled = False Then
            '차트 데이터 업데이트 중
            Me.Text = ProgressText1 & " " & ProgressText2 & " " & ProgressText3
            'If TotalNumber <> 0 Then
            'Me.Text = "Getting ChartData : " & CurrentNumber & " / " & TotalNumber
            'End If
            'End If
        Else
            'thread 종료 되었음
        End If

        MessageDisplay()
        'MessageLogging("C_Le")
        'log file save 하기
        LogFileSaveTimeCount = (LogFileSaveTimeCount + 1) Mod 10
        If LogFileSaveTimeCount = 0 Then 'AndAlso SymTree.PendingRequestCount < 6 Then    'Pending request가 해결이 안 되면 문제가 있다는 뜻이니까 Wdg error를 발생시키기 위해 로그 저장을 안 한다. 2021.04.23 부로 Wdg error 발생시키는 거 해제했다. Err 메시지 잘 warning 되니까 굳이 이렇게 할 필요 없다. log가 파일로 남는 게 디버깅에 좋다.
            Dim messages_in_one_line As String = ""
            '2초에 한 번씩 로그를 파일로 저장
            For index As Integer = 0 To StoredMessagesForFileSave.Count - 1
                messages_in_one_line = messages_in_one_line & StoredMessagesForFileSave(index)
            Next
            For retry_index As Integer = 0 To 20
                Try
                    My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "log" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", messages_in_one_line, True)
                    StoredMessagesForFileSave.Clear()
                    Exit For
                Catch ex As Exception
                    If LastExceptionErrorMesssage <> ex.Message Then
                        LastExceptionErrorMesssage = ex.Message
                        ErrorLogging("MainForm Exception: 저장실패2" & ex.Message)
                    End If
                    Threading.Thread.Sleep(200)
                End Try
            Next
        End If

        WarningFileSaveTimeCount = (WarningFileSaveTimeCount + 1) Mod 5
        If WarningFileSaveTimeCount = 0 Then
            Dim messages_in_one_line As String = ""
            '2초에 한 번씩 로그를 파일로 저장
            SafeEnterTrace(WarningMessageKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            For index As Integer = 0 To StoredMessagesForWarning.Count - 1
                messages_in_one_line = messages_in_one_line & StoredMessagesForWarning(index)
            Next
            For retry_index As Integer = 0 To 10
                Try
                    My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "warning.txt", messages_in_one_line, True)
                    StoredMessagesForWarning.Clear()
                    Exit For
                Catch ex As Exception
                    If LastExceptionErrorMesssage <> ex.Message Then
                        LastExceptionErrorMesssage = ex.Message
                        ErrorLogging("MainForm Exception: 저장실패3" & ex.Message)
                    End If
                    Threading.Thread.Sleep(200)
                End Try
            Next
#If 0 Then
            Try
                My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "warning.txt", messages_in_one_line, True)
                StoredMessagesForWarning.Clear()
            Catch ex1 As Exception
                'one more retry
                Threading.Thread.Sleep(100)
                Try
                    My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "warning.txt", messages_in_one_line, True)
                    StoredMessagesForWarning.Clear()
                Catch ex2 As Exception
                    ErrorLogging("Warning file save 중 에러!! - " & ex2.Message)
                End Try
            End Try
#End If
            SafeLeaveTrace(WarningMessageKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End If

        'log lock 검출
        If LogLocked Then
            lb_LogLocked.Text = "Log 에러가 나고 있음"
        Else
            lb_LogLocked.Text = ""
        End If
    End Sub

    '디시전 아이템 선택시
    Private Sub lv_DoneDecisions_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lv_DoneDecisions.SelectedIndexChanged
        'GDP에 표시하자
        gdp_Display.ClearAllGraphs()        '이전 그래프 모두 삭제

        If lv_DoneDecisions.SelectedItems.Count > 0 Then
            Dim the_decision_object As c050_DecisionMaker = lv_DoneDecisions.SelectedItems(0).Tag
            For index As Integer = 0 To the_decision_object.GraphicCompositeDataList.Count - 1
                gdp_Display.AddCompositeData(the_decision_object.GraphicCompositeDataList(index))
            Next
        End If

        'Pause Time하자
        gdp_Display.PauseTime()
    End Sub

    '디시전 아이템 선택시
    Private Sub lv_DoneDecisionsChart_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lv_DoneDecisionsChart.SelectedIndexChanged
        'GDP에 표시하자
        gdp_DisplayChart.ClearAllGraphs()        '이전 그래프 모두 삭제

        If lv_DoneDecisionsChart.SelectedItems.Count > 0 Then
            Dim the_decision_object As c050_DecisionMaker = lv_DoneDecisionsChart.SelectedItems(0).Tag
            For index As Integer = 0 To the_decision_object.GraphicCompositeDataList.Count - 1
                gdp_DisplayChart.AddCompositeData(the_decision_object.GraphicCompositeDataList(index))
            Next
        End If

        'Pause Time하자
        gdp_DisplayChart.PauseTime()
    End Sub

    'display messages in the message queue
    Private Sub MessageDisplay()
        'SafeEnterTrace(StoredMessagesKeyForDisplay, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim text_bar As String = ""
        If StoredMessagesMutex IsNot Nothing Then
            Dim mutex_wait_result As Boolean = False

            Try
                mutex_wait_result = StoredMessagesMutex.WaitOne(0)
            Catch ex As Exception
                'mutex error 일 경우 이렇게 될 수 있다. 가능성은 희박하지만.
                StoredMessagesMutex.ReleaseMutex()
                LogLocked = True
            End Try
            If mutex_wait_result Then
                '20220216: 아래 라인에서 OutOfMemoryException 남 (2시40분경)
                '20220216: 버퍼 싸이즈 500000 에서 200000으로 바꿈
                '20220825: 다시 OutOfMemoryException 남 (2시 40분경). 다른 곳에서 메모리를 많이 쓰기는 했음. 그래서 100000으로 바꿔봄.
                Dim message_buffer(99999) As Byte
                Dim stored_messages_access = StoredMessagesMMF.CreateViewAccessor(0, 100000)
                Dim current_length As UInt64 = stored_messages_access.ReadUInt64(0)     '현재 lenth 읽음
                stored_messages_access.ReadArray(Of Byte)(8, message_buffer, 0, current_length)
                Dim text_read As String = System.Text.Encoding.UTF8.GetString(message_buffer, 0, current_length)
                text_bar = text_read
                current_length = 0          '읽었으니까 비운다.
                stored_messages_access.Write(0, 0)   '새 lenth 0  저장
                StoredMessagesMutex.ReleaseMutex()
            Else
                '다음번으로 넘기는 것이다.
            End If
        End If

#If 0 Then
        For index As Integer = 0 To StoredMessagesForDisplay.Count - 1
            If index = StoredMessagesForDisplay.Count - 1 Then
                text_bar = text_bar & StoredMessagesForDisplay(index)
            Else
                text_bar = text_bar & StoredMessagesForDisplay(index) & vbCrLf
            End If
        Next
#End If

        If cb_ShowLog.Checked Then
            '20200403: 값이 예상범위를 벗어났습니다. 에러가 아무래도 메시지로그 표시와 관련이 있는 것 같아 원하지 않으면 끌 수 있도록 해봤다.
            If tb_Display.Text.Length + text_bar.Length > tb_Display.MaxLength Then
                tb_Display.Clear()
            End If
            If text_bar.Length < tb_Display.MaxLength Then
                '20200325: maxlength 넘게 붙여넣는 거를 방지하기 위함. 자꾸 에러(대신증권 COM API 콜 할 때 값이 예상 범위를 벗어났습니다 에러)나는 것이 이것 때문인가 싶기도 하고..
                '20200329: 이렇게 해도 해당 에러나는 것을 확인했다. 이것 때문만은 아니다.
                tb_Display.AppendText(text_bar)
            End If
        End If

        StoredMessagesForFileSave.Add(text_bar)        '디스플레이용 메세지들을 파일세이브용으로 카피한다.
        'StoredMessagesForDisplay.Clear()

        'SafeLeaveTrace(StoredMessagesKeyForDisplay, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Private Sub tm_5sClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tm_5sClock.Tick
        If IsLoadingDone AndAlso IsTimeToSave AndAlso SymbolList.Count > 0 AndAlso SymbolList(0).RecordList.Count > 2500 AndAlso SavePhase = 0 Then
            '2021.03.01: 보유중인 주식 정보 저장하자.
            SaveStocksForTomorrow()
            'DB 저장 시작하자
            SavePhase = 1 ' 가격정보 저장
            DBSupporter.SavePriceInformation()
            IsThreadExecuting = True
            MainForm.Text = MainForm.Text & " 저장중이다"
        End If

        If SavePhase = 1 AndAlso DBSupporter.SaveFinished Then
            SavePhase = 2   'Candle 정보 저장
            Dim chart_data_update_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDataUpdateThread)

            stm_PriceClock.Stop()         '가격 타이머 돌아가고 있으면 멈춘다.
            stm_PriceClock.Interval = 1 ' 안 돌아가고 있다는 걸 표시하려고 interval 을 1 으로 한다.

            chart_data_update_thread.IsBackground = True
            chart_data_update_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
            'tm_200msClock.Start()       '관리 timer 시작
            'IsThreadExecuting = True
            bt_ChartDataUpdate.Enabled = False
        End If
        If IsThreadExecuting Then
            '와치독 클리어
            MessageLogging("")
        End If

        'If (SavePhase = 1 Or SavePhase = 2) Then
        'MainForm.pb_Progress.Value = DBSupporter.NumberOfSaved
        'MainForm.Text = ProgressText1 '"저장, " & DBSupporter.NumberOfSaved.ToString & " / " & SymbolList.Count.ToString
        'End If
        'If LoggedIn Then
        '로그인 되어 있으면
        'AccountManager.Task5s()
        'End If
        'If SymbolList.Count > 0 Then '우헤헤헤헤
        'Label1.Text = SymbolList.Item(0).StockDecisionMakerList.Count.ToString
        'End If
    End Sub

    'Save stocks for tomorrow
    Private Sub SaveStocksForTomorrow()
        Dim the_symbol As c03_Symbol
        Dim the_decision As c050_DecisionMaker
        Dim xml_doc As New XmlDocument
        Dim root_node, main_account_node, sub_account_node, symbol_node, decision_node As XmlNode
        Dim quantity_attribute, ave_price_attribute, date_attribute As XmlAttribute

        xml_doc.LoadXml("<stocks></stocks>")
        root_node = xml_doc.SelectSingleNode("stocks")
        main_account_node = xml_doc.CreateNode(XmlNodeType.Element, "MainAccount", "")
        root_node.AppendChild(main_account_node)
        sub_account_node = xml_doc.CreateNode(XmlNodeType.Element, "SubAccount", "")
        root_node.AppendChild(sub_account_node)


        For index As Integer = 0 To SymbolList.Count - 1
            the_symbol = SymbolList(index)

#If 0 Then
            'main account
            symbol_node = Nothing   '심볼노드 초기화
            For multi_decision_index As Integer = 0 To the_symbol.MainDecisionMakerCenter.Count - 1
                For decision_list_index As Integer = 0 To the_symbol.MainDecisionMakerCenter(multi_decision_index).Count - 1
                    the_decision = the_symbol.MainDecisionMakerCenter(multi_decision_index)(decision_list_index)
                    If the_decision.StockOperator IsNot Nothing AndAlso (Not the_decision.IsDone) Then
                        If symbol_node Is Nothing Then
                            '전에 생성해둔 심볼노드 없으면 하나 만들자
                            symbol_node = xml_doc.CreateNode(XmlNodeType.Element, the_decision.LinkedSymbol.Code, "")
                            main_account_node.AppendChild(symbol_node)
                        End If
                        decision_node = xml_doc.CreateNode(XmlNodeType.Element, CType(the_decision, c05G_MovingAverageDifference).MABase.ToString, "")
                        quantity_attribute = xml_doc.CreateAttribute("quantity")
                        quantity_attribute.Value = the_decision.StockOperator.ThisAmount
                        decision_node.Attributes.SetNamedItem(quantity_attribute)
                        ave_price_attribute = xml_doc.CreateAttribute("ave_price")
                        ave_price_attribute.Value = the_decision.StockOperator.BuyDealPrice
                        decision_node.Attributes.SetNamedItem(ave_price_attribute)
                        symbol_node.AppendChild(decision_node)
                    End If
                Next
            Next
            If symbol_node IsNot Nothing Then
                main_account_node.AppendChild(symbol_node)
            End If
#End If

            'sub account
            symbol_node = Nothing   '심볼노드 초기화
            For multi_decision_index As Integer = 0 To the_symbol.SubDecisionMakerCenter.Count - 1
                For decision_list_index As Integer = 0 To the_symbol.SubDecisionMakerCenter(multi_decision_index).Count - 1
                    the_decision = the_symbol.SubDecisionMakerCenter(multi_decision_index)(decision_list_index)
                    If the_decision.StockOperator IsNot Nothing AndAlso the_decision.StockOperator.ThisAmount <> 0 AndAlso the_decision.StockOperator.BuyDealPrice <> 0 Then
                        If symbol_node Is Nothing Then
                            '전에 생성해둔 심볼노드 없으면 하나 만들자
                            symbol_node = xml_doc.CreateNode(XmlNodeType.Element, the_decision.LinkedSymbol.Code, "")
                            '2023.12.06 : scan start date attribute 추가
                            If the_symbol.ScanStartDate = [DateTime].MinValue Then
                                '2023.12.11 : scan start date 이 없다는 것은 전에 사둔 것이 없다는 것이고 그러면 오늘의 scan start date 으로 하면 된다.
                                date_attribute = xml_doc.CreateAttribute("scan_start_date")
                                date_attribute.Value = ChartDBSupporter.StartDate.ToString("yyyy.MM.dd")
                                symbol_node.Attributes.SetNamedItem(date_attribute)
                            Else
                                '2023.12.11 : scan start date 이 있다는 것은 전에 사둔 것이 있다는 것이고 그러면 전의 날짜를 유지하면 된다.
                                date_attribute = xml_doc.CreateAttribute("scan_start_date")
                                date_attribute.Value = the_symbol.ScanStartDate.ToString("yyyy.MM.dd")
                                symbol_node.Attributes.SetNamedItem(date_attribute)
                            End If
                            sub_account_node.AppendChild(symbol_node)
                        End If
                        decision_node = xml_doc.CreateNode(XmlNodeType.Element, CType(the_decision, c05G_MovingAverageDifference).MABase.ToString, "")
                        quantity_attribute = xml_doc.CreateAttribute("quantity")
                        quantity_attribute.Value = the_decision.StockOperator.ThisAmount
                        decision_node.Attributes.SetNamedItem(quantity_attribute)
                        ave_price_attribute = xml_doc.CreateAttribute("ave_price")
                        ave_price_attribute.Value = the_decision.StockOperator.BuyDealPrice
                        decision_node.Attributes.SetNamedItem(ave_price_attribute)
                        symbol_node.AppendChild(decision_node)
                    End If
                Next
            Next
            If symbol_node IsNot Nothing Then
                sub_account_node.AppendChild(symbol_node)
            End If
        Next

        xml_doc.Save("stocks_for_tomorrow.xml")

        Dim today_date = Now.Date
        '20221205: 히스토리 남기기 위해 아래 파일에 추가로 저장한다.
        xml_doc.Save("stocks_for_tomorrow_" & today_date.Year.ToString("D4") & today_date.Month.ToString("D2") & today_date.Day.ToString("D2") & ".xml")
    End Sub

    Public Sub LoadSavedStocks()
        Dim xml_doc As New XmlDocument
        Dim account_node, symbol_node, decision_node As XmlNode
        Dim scan_start_date As DateTime = [DateTime].MinValue
        xml_doc.Load("stocks_for_tomorrow.xml")
#If 0 Then
        'main 계좌 로드
        account_node = xml_doc.SelectSingleNode("stocks").SelectSingleNode("MainAccount")
        For index As Integer = 0 To account_node.ChildNodes.Count - 1
            symbol_node = account_node.ChildNodes(index)
            For decision_index As Integer = 0 To symbol_node.ChildNodes.Count - 1
                Dim a_stock As et1_AccountManager.StoredStockType

                decision_node = symbol_node.ChildNodes(decision_index)
                a_stock.Code = symbol_node.Name
                a_stock.MA_Base = decision_node.Name
                a_stock.Quantity = decision_node.Attributes.GetNamedItem("quantity").Value
                a_stock.AvePrice = decision_node.Attributes.GetNamedItem("ave_price").Value
                MainAccountManager.StoredStockList.Add(a_stock)
                MessageLogging("Main 잔고표시 " & a_stock.Code & ", " & a_stock.MA_Base & ", 갯수" & a_stock.Quantity & ", 평균단가 " & a_stock.AvePrice)
            Next
        Next
        '이 위로는 사실 StoreStockList 를 preemption으로 부터 보호할 필요가 없다. preemption 일어날 일이 없다.
        'main 계좌 잔고의 consistency를 체크한다.
        MainAccountManager.CheckMyStocks()
#End If
        Threading.Thread.Sleep(1100)        '좀 쉬어야 한다. 그렇지 않으면 계좌조회 실패한다.
        'sub 계좌 로드
        account_node = xml_doc.SelectSingleNode("stocks").SelectSingleNode("SubAccount")

        For index As Integer = 0 To account_node.ChildNodes.Count - 1
            symbol_node = account_node.ChildNodes(index)
            '2023.12.06 : scan start date 이 있으면 로드한다.
            If symbol_node.Attributes IsNot Nothing Then
                Dim my_attribute = symbol_node.Attributes.GetNamedItem("scan_start_date")
                If my_attribute IsNot Nothing Then
                    scan_start_date = Convert.ToDateTime(my_attribute.Value)
                End If
                'symbol list 를 iteration 돌려서 symbol 을 찾아서 이 날짜를 기록한다.
                For symbol_index As Integer = 0 To SymbolList.Count - 1
                    If SymbolList(symbol_index).Code = symbol_node.Name Then
                        '심볼을 찾았다.
                        SymbolList(symbol_index).ScanStartDate = scan_start_date
                        Exit For
                    End If
                Next
            End If

            For decision_index As Integer = 0 To symbol_node.ChildNodes.Count - 1
                Dim a_stock As et1_AccountManager.StoredStockType

                decision_node = symbol_node.ChildNodes(decision_index)
                a_stock.Code = symbol_node.Name
                a_stock.MA_Base = decision_node.Name
                a_stock.Quantity = decision_node.Attributes.GetNamedItem("quantity").Value
                a_stock.AvePrice = decision_node.Attributes.GetNamedItem("ave_price").Value
                SubAccountManager.StoredStockList.Add(a_stock)
                MessageLogging("MainForm Sub 잔고표시 " & a_stock.Code & ", " & a_stock.MA_Base & ", 갯수" & a_stock.Quantity & ", 평균단가 " & a_stock.AvePrice)
            Next
        Next
        '이 위로는 사실 StoreStockList 를 preemption으로 부터 보호할 필요가 없다. preemption 일어날 일이 없다.
        'sub 계좌 잔고의 consistency를 체크한다.
        SubAccountManager.CheckMyStocks()
    End Sub

    'login
    Private Sub bt_Login_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bt_Login.Click
        LoginProcess()
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

        '20230731:익셉션발생관리 디버깅 도우미 'DisconnectedContext' 
        ' 메시지 = 관리 디버깅 도우미 'DisconnectedContext' : '다음 오류로 인해 이 RuntimeCallableWrapper에 대한 COM 컨텍스트 0x8d61c0(으)로의 전환이 실패했습니다. 시스템 호출 실패입니다. (예외가 발생한 HRESULT: 0x80010100 (RPC_E_SYS_CALL_FAILED)). 일반적으로 이 RuntimeCallableWrapper가 생성된 COM 컨텍스트 0x8d61c0의 연결이 끊어졌거나 다른 작업을 수행하고 있어 컨텍스트 전환을 진행할 수 없기 때문입니다. COM 구성 요소에 대한 요청을 처리하는 데 프록시가 사용되지 않으며 COM 구성 요소를 직접 호출합니다. 이로 인해 손상 또는 데이터 손실이 발생할 수 있습니다. 이 문제를 방지하려면 응용 프로그램 내부의 COM 구성 요소를 나타내는 RuntimeCallableWrappers의 작업이 응용 프로그램에서 완료될 때까지 모든 COM 컨텍스트/아파트/스레드를 활성화된 상태로 유지하고 컨텍스트 전환에 사용할 수 있도록 하십시오.'


        xa_session = New XA_SESSIONLib.XASession

        ' 이미 접속이 되어 있으면 접속을 끊는다.
        xa_session.DisconnectServer()

        'Threading.Thread.Sleep(400) '2023.12.29 : 잠깐 시간두고 로그인하면 로그인 실패 확률 낮아질 것 같아서

        ' 서버에 연결한다.
        While xa_session.ConnectServer(svr_ip, svr_port) = False
#If 0 Then
            'MsgBox(xa_session.GetErrorMessage(xa_session.GetLastError()))
            ErrorLogging("MainForm" & xa_session.GetErrorMessage(xa_session.GetLastError()))
            Exit Sub
#End If
            '2024.02.18 : 성공할 때까지 계속 돌려보자.
            ErrorLogging("MainForm " & xa_session.GetErrorMessage(xa_session.GetLastError()))
            '2024.02.06 : 서버접속에 실패할 경우 한 번 더 시도하기로 한다.
            Threading.Thread.Sleep(400)
#If 0 Then
            If xa_session.ConnectServer(svr_ip, svr_port) = False Then
                ErrorLogging("MainForm 아씨 또 실패 " & xa_session.GetErrorMessage(xa_session.GetLastError()))
            Else
                MessageLogging("MainForm 휴.. 이제 됐다")
            End If
#End If
        End While

        ' 로그인 한다.
        If xa_session.Login(user_id, user_password, user_cert, virtual_account_login, False) = False Then
            'MsgBox("로그인 전송 실패")
            ErrorLogging("MainForm 로그인 전송 실패")
        Else
            '로그인 성공
            'MsgBox("로그인 전송 성공")
            MessageLogging("MainForm 로그인 전송 성공")
            LoggedIn = True
        End If

    End Sub

    Private Sub xa_session_Login(ByVal szCode As String, ByVal szMsg As String) Handles xa_session.Login
        ' 계좌번호 설정
        'Me.Text = szCode
        'MsgBox(szMsg & vbCrLf & xa_session.GetCommMedia() & " " & xa_session.GetErrorMessage(xa_session.GetLastError) & " " & xa_session.GetETKMedia & " " & xa_session.GetLastError & " " & xa_session.GetServerName & " " & xa_session.IsConnected().ToString & " " & xa_session.SendPacketSize.ToString)
        MessageLogging("MainForm" & szCode & " : " & szMsg)
        'AccountString = xa_session.GetAccountList(0)
        'MessageLogging(AccountString)

        LoggedButAccountNotRequested = True
    End Sub


    Private Sub XAReal_SC0_ReceiveRealData(ByVal szTrCode As String) Handles XAReal_SC0.ReceiveRealData
        'Dim account_number0 As String = XAReal_SC0.GetFieldData("OutBlock", "accno") '계좌번호 추출
        Dim account_number As String = XAReal_SC0.GetFieldData("OutBlock", "accno1") '계좌번호 추출
        Dim order_number As String = XAReal_SC0.GetFieldData("OutBlock", "ordno")
        Dim tr_code As String = XAReal_SC0.GetFieldData("OutBlock", "trcode")
        Dim short_code As String = XAReal_SC0.GetFieldData("OutBlock", "shtcode")
        Dim buy_or_sel As String = XAReal_SC0.GetFieldData("OutBlock", "bnstp")
        Dim ordprice As String = XAReal_SC0.GetFieldData("OutBlock", "ordprice")
        Dim ordamt As String = XAReal_SC0.GetFieldData("OutBlock", "ordamt")                '주문금액
        MessageLogging(short_code & " " & account_number & " SC0 " & ordprice & " " & ordamt & " " & buy_or_sel)

        Dim order_money As UInt64 = Math.Max(Convert.ToInt64(ordamt), 0)
        Dim buy_money As UInt64
        If buy_or_sel = "2" Then
            '2024.02.04 : 매수주문으로 인한 증거금 감소를 계산하기 위함
            buy_money = order_money
        Else
            buy_money = 0
        End If
        'Dim ordable_money As Long
        'Try
        'ordable_money = Convert.ToInt64(ordable_money_str) '주문가능대용,ordablesubstamt
        'Catch ex As Exception
        'ordable_money = 0
        'ExceptionCount += 1
        'End Try
        'ordable_money = Math.Max(ordable_money, 0)

        Dim simulate_thread As Threading.Thread
        Dim parameters() As Object
        If account_number = MainAccountString Then
            If SC0_CONFIRM_SUPPORT Then
                MainAccountManager.SubConfirmChecker_NotifiedBySC0(short_code, tr_code, order_number)
            End If
            simulate_thread = New Threading.Thread(AddressOf MainAccountManager.XAReal_SC0_ReceiveRealData_PostThread)
            parameters = {short_code, buy_money}
            MainAccountManager.XAReal_SC0_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = SubAccountString Then
            If SC0_CONFIRM_SUPPORT Then
                SubAccountManager.SubConfirmChecker_NotifiedBySC0(short_code, tr_code, order_number)
            End If
            simulate_thread = New Threading.Thread(AddressOf SubAccountManager.XAReal_SC0_ReceiveRealData_PostThread)
            parameters = {short_code, buy_money}
            SubAccountManager.XAReal_SC0_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = TestAccountString Then
            If SC0_CONFIRM_SUPPORT Then
                TestAccountManager.SubConfirmChecker_NotifiedBySC0(short_code, tr_code, order_number)
            End If
            simulate_thread = New Threading.Thread(AddressOf TestAccountManager.XAReal_SC0_ReceiveRealData_PostThread)
            parameters = {short_code, buy_money}
            TestAccountManager.XAReal_SC0_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        Else
            ErrorLogging("MainForm " & account_number & " SC0 이건 무슨 계좌인가?")
        End If
    End Sub

    Private Sub XAReal_SC1_ReceiveRealData(ByVal szTrCode As String) Handles XAReal_SC1.ReceiveRealData
        'Dim account_number0 As String = XAReal_SC1.GetFieldData("OutBlock", "accno") '계좌번호 추출
        Dim account_number As String = XAReal_SC1.GetFieldData("OutBlock", "accno1") '계좌번호 추출
        Dim order_number As Integer = Convert.ToInt32(XAReal_SC1.GetFieldData("OutBlock", "ordno")) '주문번호 추출
        Dim deal_price As Double = Convert.ToDouble(XAReal_SC1.GetFieldData("OutBlock", "ordavrexecprc"))   '주문평균체결가격 추출
        Dim rest_amount As UInt32 = Convert.ToUInt32(XAReal_SC1.GetFieldData("OutBlock", "unercqty"))    '미체결수량(주문) 추출
        Dim deal_amount As UInt32 = Convert.ToUInt32(XAReal_SC1.GetFieldData("OutBlock", "execqty"))    '체결수량 추출
        Dim buy_or_sel As Integer = Convert.ToUInt32(XAReal_SC1.GetFieldData("OutBlock", "bnstp"))      '매매구분 1:매도, 2: 매수
        Dim ordamt As String = XAReal_SC1.GetFieldData("OutBlock", "ordamt")                '주문금액
        Dim mnyexecamt As String = XAReal_SC1.GetFieldData("OutBlock", "mnyexecamt")                '현금체결금액
        Dim shtnIsuno As String = XAReal_SC1.GetFieldData("OutBlock", "shtnIsuno")                '단축종목번호
        MessageLogging(shtnIsuno & " " & account_number & " SC1 " & deal_price & " " & deal_amount & " " & buy_or_sel)

        Dim sel_money_back As UInt64
        If buy_or_sel = "1" Then
            '2024.02.04 : 매도 체결로 인한 증거금 회복
            sel_money_back = deal_amount * deal_price
        Else
            sel_money_back = 0
        End If

        '180527 TODO: 예상하지 못한 매수 확인이 들어왔을 때 바로 팔아버리는 로직 필요하다. 위의 매매구분을 PostThread 에 넘겨서 팔아버리는 로직 만들자.
        '거기에는 아래와 같이 팔아버리는 코드가 들어가야 한다.
#If 0 Then
        _CSPAT00600_List.Add(New et3_XAQuery_Wrapper)
        AddHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
        AddHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage

        _CSPAT00600.ResFileName = "Res\CSPAT00600.res"
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, order_amount.ToString)                     '매수수량
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, order_price.ToString & ".00")                   '주문가
        If enter_or_exit = EnterOrExit.EOE_Enter Then
            _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "2")                                   '매수/매도 구분 (매수)
        Else 'If enter_or_exit = EnterOrExit.EOE_Exit Then
            _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "1")                                   '매수/매도 구분 (매도)
        End If
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '주문조건 구분 (없음)
        nReqID = _CSPAT00600.Request(False)
#End If
        Dim simulate_thread As Threading.Thread
        Dim parameters() As Object
        If account_number = MainAccountString Then
            simulate_thread = New Threading.Thread(AddressOf MainAccountManager.XAReal_SC1_ReceiveRealData_PostThread)
            parameters = {order_number, deal_price, rest_amount, deal_amount, shtnIsuno, sel_money_back}
            MainAccountManager.XAReal_SC1_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = SubAccountString Then
            simulate_thread = New Threading.Thread(AddressOf SubAccountManager.XAReal_SC1_ReceiveRealData_PostThread)
            parameters = {order_number, deal_price, rest_amount, deal_amount, shtnIsuno, sel_money_back}
            SubAccountManager.XAReal_SC1_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = TestAccountString Then
            simulate_thread = New Threading.Thread(AddressOf TestAccountManager.XAReal_SC1_ReceiveRealData_PostThread)
            parameters = {order_number, deal_price, rest_amount, deal_amount, shtnIsuno, sel_money_back}
            TestAccountManager.XAReal_SC1_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        Else
            ErrorLogging("MainForm " & account_number & " SC1 이건 무슨 계좌인가?")
        End If
    End Sub

    Private Sub XAReal_SC3_ReceiveRealData(ByVal szTrCode As String) Handles XAReal_SC3.ReceiveRealData
        'Dim account_number0 As String = XAReal_SC3.GetFieldData("OutBlock", "accno") '계좌번호 추출
        Dim account_number As String = XAReal_SC3.GetFieldData("OutBlock", "accno1") '계좌번호 추출
        Dim order_number As Integer = Convert.ToInt32(XAReal_SC3.GetFieldData("OutBlock", "ordno")) '주문번호 추출
        Dim original_order_number As Integer = Convert.ToInt32(XAReal_SC3.GetFieldData("OutBlock", "orgordno"))     '원주문번호추출
        Dim deal_price As Double = Convert.ToDouble(XAReal_SC3.GetFieldData("OutBlock", "ordprc"))   '주문가격 추출
        Dim rest_amount As UInt32 = Convert.ToUInt32(XAReal_SC3.GetFieldData("OutBlock", "canccnfqty"))    '취소되어서 체결 안 된 수량 추출
        Dim ordamt As String = XAReal_SC3.GetFieldData("OutBlock", "ordamt")                '주문금액 <= 취소된 금액에 해당함. 주문가능금액 업데이트에 활용예정.
        Dim shtnIsuno As String = XAReal_SC3.GetFieldData("OutBlock", "shtnIsuno")                '단축종목번호
        Dim buy_or_sel As Integer = Convert.ToUInt32(XAReal_SC3.GetFieldData("OutBlock", "bnstp"))      '매매구분 1:매도, 2: 매수
        MessageLogging(shtnIsuno & " " & account_number & " SC3 " & deal_price & " " & rest_amount & " " & ordamt & " " & buy_or_sel)

        Dim canceled_money As UInt64
        If buy_or_sel = "2" Then
            '2024.02.04 : 매수주문 취소시 증거금 회복을 위한 cancel money
            canceled_money = Math.Max(Convert.ToInt64(ordamt), 0)
        Else
            canceled_money = 0
        End If

        Dim simulate_thread As Threading.Thread
        Dim parameters() As Object
        If account_number = MainAccountString Then
            MessageLogging("MainForm " & "Main" & " SC3 메시지 도착")

            simulate_thread = New Threading.Thread(AddressOf MainAccountManager.XAReal_SC3_ReceiveRealData_PostThread)
            parameters = {order_number, original_order_number, deal_price, rest_amount, shtnIsuno, canceled_money}
            MainAccountManager.XAReal_SC3_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = SubAccountString Then
            MessageLogging("MainForm " & "Sub" & " SC3 메시지 도착")

            simulate_thread = New Threading.Thread(AddressOf SubAccountManager.XAReal_SC3_ReceiveRealData_PostThread)
            parameters = {order_number, original_order_number, deal_price, rest_amount, shtnIsuno, canceled_money}
            SubAccountManager.XAReal_SC3_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = TestAccountString Then
            MessageLogging("MainForm " & "Test" & " SC3 메시지 도착")

            simulate_thread = New Threading.Thread(AddressOf TestAccountManager.XAReal_SC3_ReceiveRealData_PostThread)
            parameters = {order_number, original_order_number, deal_price, rest_amount, shtnIsuno, canceled_money}
            TestAccountManager.XAReal_SC3_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        Else
            ErrorLogging("MainForm " & account_number & " SC3 이건 무슨 계좌인가?")
        End If
    End Sub

    '주문 거부 실시간 확인
    Private Sub XAReal_SC4_ReceiveRealData(ByVal szTrCode As String) Handles XAReal_SC4.ReceiveRealData
        Dim account_number0 As String = XAReal_SC4.GetFieldData("OutBlock", "accno") '계좌번호 추출
        MessageLogging("MainForm " & "SC4 " & account_number0)
        Dim account_number As String = XAReal_SC4.GetFieldData("OutBlock", "accno1") '계좌번호 추출
        MessageLogging("MainForm " & "SC4_ " & account_number)
        Dim order_number_str As String = XAReal_SC4.GetFieldData("OutBlock", "ordno")
        Dim order_number As Integer
        Try
            order_number = Convert.ToInt32(order_number_str) '주문번호 추출
        Catch ex As Exception
            order_number = 0
            ExceptionCount += 1
        End Try

        Dim simulate_thread As Threading.Thread
        Dim parameters() As Object
        If account_number = MainAccountString Then
            MessageLogging("MainForm " & "Main" & " SC4 메시지 도착")

            simulate_thread = New Threading.Thread(AddressOf MainAccountManager.XAReal_SC4_ReceiveRealData_PostThread)
            parameters = {order_number}
            MainAccountManager.XAReal_SC4_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = SubAccountString Then
            MessageLogging("MainForm " & "Sub" & " SC4 메시지 도착")

            simulate_thread = New Threading.Thread(AddressOf SubAccountManager.XAReal_SC4_ReceiveRealData_PostThread)
            parameters = {order_number}
            SubAccountManager.XAReal_SC4_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        ElseIf account_number = TestAccountString Then
            MessageLogging("MainForm " & "Test" & " SC4 메시지 도착")

            simulate_thread = New Threading.Thread(AddressOf TestAccountManager.XAReal_SC4_ReceiveRealData_PostThread)
            parameters = {order_number}
            TestAccountManager.XAReal_SC4_ReceiveRealData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        Else
            ErrorLogging("MainForm " & account_number & " SC4 이건 무슨 계좌인가?")
        End If

    End Sub


    Private Sub tsmi_SaveClipboard_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tsmi_SaveClipboard.Click
        Dim line_str As String = ""
        Dim number_of_columns As Integer = lv_DoneDecisions.Columns.Count
        For index As Integer = 0 To lv_DoneDecisions.Items.Count - 1
            For index_col As Integer = 0 To number_of_columns - 1
                If lv_DoneDecisions.Items(index).SubItems.Count > index_col Then
                    line_str += lv_DoneDecisions.Items(index).SubItems(index_col).Text & vbTab
                Else
                    line_str += "-" & vbTab
                End If
            Next
            line_str += vbCrLf
        Next
        Try
            Clipboard.SetDataObject(line_str, False, 5, 200)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        'save clipboard 오른손 클릭 없이 하기
        tsmi_SaveClipboard_Click(Nothing, Nothing)
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

        Dim t8412_receive_data_thread As Threading.Thread = New Threading.Thread(AddressOf T8412ReceiveDataThread)
        t8412_receive_data_thread.IsBackground = True
        t8412_receive_data_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        '190622: 쓰레드 종료되는 거 처리 안 해줘서 OUTOFMEMORY 나는 거 아닐까? 위에꺼 진짜 thread 실행되는지 봐야하지 않을까?
    End Sub

    Public DBInsertTime As TimeSpan
    Private Sub T8412ReceiveDataThread()
        Dim symbol_code As String
        Dim count As Integer
        Dim date_str, time_str, open_str, high_str, low_str, close_str, amount_str As String
        Dim open_price, high_price, low_price, close_price As Int64
        Dim amount_int As Int32
        Dim validity As Boolean = True
        Dim current_sample_time = TimeSpan.FromHours(0)
        Dim old_sample_time = TimeSpan.FromHours(0)

        Dim year_str, month_str, day_str, hour_str, minute_str, second_str As String
        Dim line_text As String = ""
        Dim cmd As OleDb.OleDbCommand
        Dim insert_command As String
        Dim temp1, temp2 As TimeSpan

        symbol_code = _T8412.GetFieldData("t8412OutBlock", "shcode", 0)
        If _T8412.Decompress("t8412OutBlock1") > 0 Then
            count = _T8412.GetBlockCount("t8412OutBlock1")
            If count <= 3 Then
                MessageLogging("MainForm " & "Data 갯수가 3이하야 아니 이럴 수도 있어?")
            End If
        Else
            count = 0
        End If
        CtsDate = _T8412.GetFieldData("t8412OutBlock", "cts_date", 0)     ' 연속일자
        CtsTime = _T8412.GetFieldData("t8412OutBlock", "cts_time", 0)     ' 연속시간
        For index As Integer = 0 To count - 1
            temp1 = Now.TimeOfDay
            date_str = _T8412.GetFieldData("t8412OutBlock1", "date", index)     ' 날짜
            time_str = _T8412.GetFieldData("t8412OutBlock1", "time", index)     ' 시간
            year_str = date_str.Substring(0, 4)
            month_str = date_str.Substring(4, 2)
            day_str = date_str.Substring(6, 2)
            hour_str = time_str.Substring(0, 2)
            minute_str = time_str.Substring(2, 2)
            second_str = time_str.Substring(4, 2)

            '2024.03.31 : 바로 전 샘플과 비교해 시간이 10분 차이이면 insert 하지 않는다. 15:30 샘플은 이제 빼기로 한다. 이 시점 이후로 하루의 candle 갯수는 381이 아니라 380이다.
            current_sample_time = TimeSpan.FromHours(Convert.ToDouble(hour_str)).Add(TimeSpan.FromMinutes(Convert.ToDouble(minute_str)))
            If current_sample_time - old_sample_time = TimeSpan.FromMinutes(10) Then
                old_sample_time = current_sample_time
                Continue For
            End If
            old_sample_time = current_sample_time

            open_str = _T8412.GetFieldData("t8412OutBlock1", "open", index)     ' 시가
            high_str = _T8412.GetFieldData("t8412OutBlock1", "high", index)     ' 고가
            low_str = _T8412.GetFieldData("t8412OutBlock1", "low", index)     ' 저가
            close_str = _T8412.GetFieldData("t8412OutBlock1", "close", index)     ' 종가
            amount_str = _T8412.GetFieldData("t8412OutBlock1", "jdiff_vol", index)     ' 거래량
            temp2 = Now.TimeOfDay
            '유효성 검사
            Try
                open_price = Convert.ToInt32(open_str)
                high_price = Convert.ToInt32(high_str)
                low_price = Convert.ToInt32(low_str)
                close_price = Convert.ToInt32(close_str)
                amount_int = Convert.ToInt64(amount_str)
                If open_price < 0 OrElse high_price < 0 OrElse low_price < 0 OrElse close_price < 0 OrElse amount_int < 0 Then
                    validity = False
                End If
            Catch ex As Exception
                validity = False
            End Try

            If validity = True Then
                'line_text += date_str & ", " & time_str & ", " & open_str & ", " & high_str & ", " & low_str & ", " & last_str & ", " & amount_str & vbCrLf
                insert_command = "INSERT INTO " & db_table_name & " (CandleTime, OpenPrice, ClosePrice, HighPrice, LowPrice, Amount) VALUES ('" & year_str & "-" & month_str & "-" & day_str & " " & hour_str & ":" & minute_str & ":" & second_str & "', " & open_str & ", " & close_str & ", " & high_str & ", " & low_str & ", " & amount_str & ");"
            cmd = New OleDb.OleDbCommand(insert_command, _db_connection)
                cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                'cmd.CommandText = insert_command
                Try
                    cmd.ExecuteNonQuery()              'insert 명령 실행
                Catch ex As Exception
                    If ex.Message.Contains("중복 키를 삽입할 수 없습니다") Or ex.Message.Contains("Cannot insert duplicate key") Then
                        '이건 OK
                        _CandleChartDuplicateErrorCount += 1
                    Else
                        _CandleChartInsertErrorCount += 1
                        If LastExceptionErrorMesssage <> ex.Message Then
                            LastExceptionErrorMesssage = ex.Message
                            ErrorLogging("MainForm Exception: 삽입실패" & ex.Message & ", " & date_str & " " & time_str)
                        End If
                    End If
                End Try
            Else
                '이건 삽입실패와 동일취급한다.
                _CandleChartInsertErrorCount += 1
                ErrorLogging("MainForm Invalid Price or Amount: " & open_str & ", " & high_str & ", " & low_str & ", " & close_str & ", " & amount_str)
                validity = True
            End If
            temp1 = Now.TimeOfDay
            DBInsertTime = temp1 - temp2
            cmd.Dispose()
            ProgressText2 = date_str & ", insert error: " & _CandleChartInsertErrorCount & ", duplicate error: " & _CandleChartDuplicateErrorCount
            ProgressText3 = ", no response: " & NoResponseNumber & ", failed: " & FailedSymbolNumber & ", retry: " & _CandleChartRetryCount
        Next

        RxProcessCompleted = True
    End Sub

    Private Sub bt_ChartDataUpdate_Click(sender As Object, e As EventArgs) Handles bt_ChartDataUpdate.Click
        If IsBadTimeToMakeCandle Then
            'MessageLogging("Candle 만들기 좋은 시간이 아닙니다. 그냥 종료합니다.")
            'Exit Sub
            If MsgBox("Candle 만들기 좋은 시간 아닌데 그냥 할래요? 계속하면 가격 타이머 꺼집니다?", MsgBoxStyle.YesNo) <> MsgBoxResult.Yes Then
                Exit Sub
            End If
        End If

        Dim chart_data_update_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDataUpdateThread)

        stm_PriceClock.Stop()         '가격 타이머 돌아가고 있으면 멈춘다.
        stm_PriceClock.Interval = 1 ' 안 돌아가고 있다는 걸 표시하려고 interval 을 1 으로 한다.

        chart_data_update_thread.IsBackground = True
        chart_data_update_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        'tm_200msClock.Start()       '관리 timer 시작
        'IsThreadExecuting = True
        bt_ChartDataUpdate.Enabled = False
    End Sub

    '190812: Past candle 로드하는 시간을 좀 단축시켰다. 이제 로드시간은 6분 30초 이내로 큰 부담 없다.
    '190812:TODO 걸린애 done 되면서 업데이트할 때 뭔가 이상해지는 거 디버그
    '190812:TODO xml 저장은 추후 개발하고 우선 계좌에 있는 주식들을 자동연결해주는 로직을 먼저 개발하자.
    '190812: interpolation 대신 etrade 서버에 캔들 데이터를 요청해서 받아오는 것을 생각했는데, 연속 요청 제한 등을 생각하면 그다지 실용적이지 않을 것 같다.
    Private _db_connection As OleDb.OleDbConnection
    'Public TotalNumber, CurrentNumber As Integer
    Public SymbolCodeOnRequest As String
    Public CtsDate As String = ""
    Public CtsTime As String = ""
    Private _TableNameToCheck As String
    Private _CandleChartInsertErrorCount As Integer
    Private _CandleChartDuplicateErrorCount As Integer
    Private _CandleChartRetryCount As Integer
    Private temptemp As Integer
    Private db_table_name As String
    Private Sub ChartDataUpdateThread()
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim read_result As OleDb.OleDbDataReader

        '최근 Price data DB로부터 종목정보를 모은다.
        'Dim _DBList() As String = { _
        '"PriceGangdoDB_201810", _
        '"PriceGangdoDB_201811", _
        '"PriceGangdoDB_201812", _
        '"PriceGangdoDB_201901" _
        '}
        'Dim data_table As DataTable
        'Dim table_name As String
        Dim table_name_list As New List(Of String)
        Dim db_name, table_name As String
        If cb_AutoUpdate.Checked Then
            '종목검색에서 얻은 리스트와 DB 파일명에서 얻은 리스트를 합친다.
            Dim file_list = My.Computer.FileSystem.GetFiles(CANDLE_CHART_FOLDER)
            For index As Integer = 0 To file_list.Count - 1
                If IO.Path.GetExtension(file_list(index)) = ".mdf" Then
                    db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)
                    table_name = db_name.Substring(14, 7)
                    'If Not table_name_list.Contains(table_name) Then
                    table_name_list.Add(table_name)
                    'End If
                End If
            Next
            For index As Integer = 0 To SymbolList.Count - 1
                If Not table_name_list.Contains(SymbolList(index).Code) Then
                    table_name_list.Add(SymbolList(index).Code)
                End If
            Next
        Else
            table_name_list = My.Computer.FileSystem.ReadAllText(tb_DBListFileName.Text).Replace(vbCr, "").Split(vbLf).ToList
        End If
        'For db_index As Integer = 0 To _DBList.Count - 1
        '_db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Initial Catalog=" & _DBList(db_index) & "; Integrated Security=SSPI;")
        '_db_connection.Open()
        'data_table = _db_connection.GetSchema("tables")
        'For table_index As Integer = 0 To data_table.Rows.Count - 1
        'table_name = data_table.Rows(table_index).Item(2).ToString
        '테이블 이름이 종목코드와 같은 형식인지 검사(첫자가 A이고 길이가 7)
        'If table_name(0) = "A" And table_name.Length = 7 Then
        'If Not table_name_list.Contains(table_name) Then
        'table_name_list.Add(table_name)
        'End If
        'End If
        'Next
        '_db_connection.Close()
        'Next

        'DB connection
        _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Integrated Security=SSPI;")
        '_db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Integrated Security=SSPI; Initial Catalog=CandleChartDB_A000020")
        _db_connection.Open()

        'Candle Chart 테이블 이름 리스트를 만든다.
        'data_table = _db_connection.GetSchema("tables")
        'Dim candle_table_list As New List(Of String)
        'For table_index As Integer = 0 To data_table.Rows.Count - 1
        'candle_table_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
        'Next

        'Dim db_exist As Boolean
        Dim candle_table_list As New List(Of String)
        'Dim date_verified_with_candle, date_verified_without_candle As Date
        Dim last_candle_date As Date
        Dim candledate_string As String
        'Dim request_time As TimeSpan
        'TotalNumber = table_name_list.Count
        'CurrentNumber = 0
        For table_index As Integer = 0 To table_name_list.Count - 1

            'If table_index < 12 Then
            '190730: 왜 lsass 돌아가냐고.. DB를 자주 바꿔서 그러는건가? 처음엔 잘 되다가 몇 번 DB 바꾸니 안 된다.
            '190731: 왜냐면 DB connection을 자꿈 open/close 해서 그런 것 같다. 그럴 필요가 없다. 왜냐면 DB 바꾸려고 connection을 다시 할 필요 없이 그냥 [DB].[TABLE] 이런 식으로 쿼리를 짜면 되기 때문이다. MSSQL multiple databases 이 검색어로 구글링 해봐라.
            '190731: 오예 이거 먹는다
            'IF EXISTS (SELECT * from sys.databases WHERE name='PriceGangdoDB_201906')
            'SELECT TOP 1000 [SampledTime]
            '    ,[Price]
            '   ,[Amount]
            '  ,[Gangdo]
            '       FROM [PriceGangdoDB_201907].[dbo].[A000210]
            'Continue For
            'End If
            '190201: 1분 chart만 할꺼니까 t8412로만 하자.
            ' If Now.Hour = 4 AndAlso Now.Minute > 15 Then
            '야간에만 한다
            ' While Not (Now.Hour = 7 AndAlso Now.Minute > 15)
            'Threading.Thread.Sleep(100)
            ' End While
            'LoginProcess()
            ' Threading.Thread.Sleep(2000)
            'MessageLogging(table_index & "할 차례에요. 다음에 또 만나요~")
            'Exit For
            'End If
            ProgressText1 = table_index & "/" & table_name_list.Count & ", " & table_name_list(table_index)

            '해당 종목 DB가 있는지 보고 없으면 만든다.
            db_name = "CandleChartDB_" & table_name_list(table_index).Trim()
            If db_name = "CandleChartDB_" Then
                Continue For
            End If
            command_text = "SELECT * from sys.databases WHERE name='" & db_name & "'"
            cmd = New OleDb.OleDbCommand(command_text, _db_connection)
            cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
            read_result = cmd.ExecuteReader()
            If read_result.Read Then
                'DB 존재함 밑으로 내려감
                cmd.Dispose()
                _db_connection.Close()
                _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Integrated Security=SSPI;")
                _db_connection.Open()
            Else
                'DB 안 존재함 DB 생성
                cmd.Dispose()
                Try
                    command_text = "CREATE DATABASE " & db_name & " ON PRIMARY " &
                          "(NAME = " & db_name & "_main, " &
                          " FILENAME = '" & CANDLE_CHART_FOLDER & db_name & "_main.mdf', " &
                          " FILEGROWTH = 10%) " &
                          " LOG ON " &
                          "(NAME = " & db_name & "_log, " &
                          " FILENAME = '" & CANDLE_CHART_FOLDER & db_name & "_log.ldf', " &
                          " FILEGROWTH = 10%) "
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                    cmd.ExecuteNonQuery()
                    cmd.Dispose()
                    '_db_connection.Close()
                    Threading.Thread.Sleep(100) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
                Catch ex As Exception
                    'DB 생성에 실패했습니다.
                    If LastExceptionErrorMesssage <> ex.Message Then
                        LastExceptionErrorMesssage = ex.Message
                        ErrorLogging("MainForm Exception: 생성1실패 " & ex.Message)
                    End If
                    MsgBox("DB 생성에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                    _db_connection.Close()              'DB connection 닫기
                    Exit Sub
                End Try

            End If
#If 0 Then
            Try
                If _db_connection Is Nothing Then
                    _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;" & "Initial Catalog=" & db_name)
                    _db_connection.Open()
                Else
                    _db_connection.ConnectionString = "Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;" & "Initial Catalog=" & db_name
                    _db_connection.Open()
                End If
                'DB 존재함
                'db_exist = True
            Catch ex As Exception
                'DB 존재하지 않음
                'db_exist = False

                If LastExceptionErrorMesssage <> ex.Message Then
                    LastExceptionErrorMesssage = ex.Message
                    ErrorLogging("Exception: 한번실패 " & ex.Message)
                End If
                'DB 생성
                _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;")

                Try
                    'master DB 접속
                    _db_connection.Open()

                    command_text = "CREATE DATABASE " & db_name & " ON PRIMARY " & _
                          "(NAME = " & db_name & "_main, " & _
                          " FILENAME = '" & _CANDLE_CHART_FOLDER & db_name & "_main.mdf', " & _
                          " FILEGROWTH = 10%) " & _
                          " LOG ON " & _
                          "(NAME = " & db_name & "_log, " & _
                          " FILENAME = '" & _CANDLE_CHART_FOLDER & db_name & "_log.ldf', " & _
                          " FILEGROWTH = 10%) "
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.ExecuteNonQuery()
                    cmd.Dispose()
                    _db_connection.Close()
                    Threading.Thread.Sleep(10000) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
                    'DB 다시 열기 시도
                    _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;" & "Initial Catalog=" & db_name)

                    _db_connection.Open()

                Catch ex2 As Exception
                    'DB 생성에 실패했습니다.
                    If LastExceptionErrorMesssage <> ex2.Message Then
                        LastExceptionErrorMesssage = ex2.Message
                        ErrorLogging("Exception: 두번실패 " & ex2.Message)
                    End If
                    MsgBox("DB 생성에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                    _db_connection.Close()              'DB connection 닫기
                    Exit Sub
                End Try
            End Try
#End If
            '_db_connection.Close()              'DB connection 닫기
            'OleDb.OleDbConnection.ReleaseObjectPool()

            'DB 다시 열기 시도
            '_db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=localhost;Integrated Security=SSPI;" & "Initial Catalog=" & db_name)

            db_table_name = "[" & db_name & "].[dbo].[" & table_name_list(table_index) & "]"
            'db_table_name = table_name_list(table_index)
#If 1 Then
            Try
                '_db_connection.Open()
                'Candle Chart 테이블 있는지 확인한다.
                'command_text = "SELECT * from " & db_table_name
                'cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                'read_result = cmd.ExecuteReader()
#If 0 Then
                'Candle Chart 테이블 이름 리스트를 만든다.
                data_table = _db_connection.GetSchema("tables")
                'Dim candle_table_list As New List(Of String)
                For sys_table_index As Integer = 0 To data_table.Rows.Count - 1
                    candle_table_list.Add(data_table.Rows(sys_table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
                Next

                _TableNameToCheck = table_name_list(table_index)
                If candle_table_list.Exists(AddressOf TableExistCheck) Then
                    '테이블 존재함 밑으로 내려감
                    cmd.Dispose()
                    '190416: 마지막으로 저장된 날짜를 binary search를 통해 찾아가는 algorithm 구현해보자.
                    '190417: 그러려고 했지만, 주말/휴일과 기록이 없는 날을 구분하지 못해 그냥 오늘부터 하루씩 뒤로가며 검색하는 걸로 하기로 한다.

                    If cb_AutoUpdate.Checked Then
                        '어디까지 업데이트 완료했는지 뒤로가면서 확인 <= 이건 자동 업데이트일 때만 작동함
                        last_candle_date = Now.Date
                        Do
                            'candletime_string = date_to_be_verified.ToString("'yyyy-MM-dd 00:00:00' AND 'yyyy-MM-dd 23:59:59'")
                            candledate_string = last_candle_date.ToString("yyyy-MM-dd")
                            command_text = "SELECT * from " & db_table_name & " WHERE CandleTime BETWEEN '" & candledate_string & " 00:00:00' AND '" & candledate_string & " 23:59:59' ORDER BY CandleTime ASC"
                            cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                            read_result = cmd.ExecuteReader()
                            If read_result.Read Then
                                cmd.Dispose()
                                Exit Do
                            Else
                                cmd.Dispose()
                                last_candle_date = last_candle_date - TimeSpan.FromDays(1)
                            End If
                        Loop While last_candle_date > Now.Date - TimeSpan.FromDays(365)
                    Else
                        last_candle_date = dtp_LastCandleDate.Value
                    End If
                Else
                    '테이블 안 존재함 테이블 만듦
                    MessageLogging("설마 테이블생성?")
                    command_text = "CREATE TABLE " & db_table_name & " (CandleTime DATETIME PRIMARY KEY, OpenPrice INT, ClosePrice INT, HighPrice INT, LowPrice INT, Amount BIGINT);"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.ExecuteNonQuery()             '명령 실행
                    cmd.Dispose()
                    last_candle_date = Now.Date - TimeSpan.FromDays(365)
                End If
#End If

#If 1 Then
                'data_table = _db_connection.GetSchema("tables")
                'candle_table_list.Clear()
                'For candle_table_index As Integer = 0 To data_table.Rows.Count - 1
                'candle_table_list.Add(data_table.Rows(candle_table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
                'Next
                '_TableNameToCheck = table_name_list(table_index)
                'If Not candle_table_list.Exists(AddressOf TableExistCheck) Then

                'SELECT TOP 1000 [SampledTime]
                '    ,[Price]
                '   ,[Amount]
                '  ,[Gangdo]
                '       FROM [PriceGangdoDB_201907].[dbo].[A000210]

                command_text = "SELECT TOP 1000 [CandleTime] FROM " & db_table_name
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                read_result = cmd.ExecuteReader()
                If read_result.Read Then
                    'DB 존재함 밑으로 내려감
                    cmd.Dispose()
                    '190416: 마지막으로 저장된 날짜를 binary search를 통해 찾아가는 algorithm 구현해보자.
                    '190417: 그러려고 했지만, 주말/휴일과 기록이 없는 날을 구분하지 못해 그냥 오늘부터 하루씩 뒤로가며 검색하는 걸로 하기로 한다.
                    If cb_AutoUpdate.Checked Then
                        '어디까지 업데이트 완료했는지 뒤로가면서 확인 <= 이건 자동 업데이트일 때만 작동함
                        'last_candle_date = Now.Date
                        'Do
                        'candletime_string = date_to_be_verified.ToString("'yyyy-MM-dd 00:00:00' AND 'yyyy-MM-dd 23:59:59'")
                        'candledate_string = last_candle_date.ToString("yyyy-MM-dd")
                        'command_text = "SELECT * from [" & db_name & "].[dbo].[" & table_name_list(table_index) & "] WHERE CandleTime BETWEEN '" & candledate_string & " 00:00:00' AND '" & candledate_string & " 23:59:59' ORDER BY CandleTime ASC"
                        'command_text = "SELECT * from " & db_table_name & " WHERE CandleTime BETWEEN '" & candledate_string & " 00:00:00' AND '" & candledate_string & " 23:59:59' ORDER BY CandleTime ASC"
                        command_text = "SELECT [CandleTime] from " & db_table_name & " ORDER BY CandleTime DESC"
                        cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                        cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                        read_result = cmd.ExecuteReader()
                        If read_result.Read Then
                            last_candle_date = CType(read_result(0), DateTime).Date
                            cmd.Dispose()
                            'Exit Do
                        Else
                            cmd.Dispose()
                            'last_candle_date = last_candle_date - TimeSpan.FromDays(1)
                            last_candle_date = Now.Date - TimeSpan.FromDays(365)
                        End If
                        'Loop While last_candle_date > Now.Date - TimeSpan.FromDays(365)
                    Else
                        last_candle_date = dtp_LastCandleDate.Value
                    End If
                Else
                    cmd.Dispose()
                    '테이블 존재하지 않음 => 테이블 생성
                    MessageLogging("MainForm " & "설마 테이블생성?")
                    command_text = "CREATE TABLE " & db_table_name & " (CandleTime DATETIME PRIMARY KEY, OpenPrice INT, ClosePrice INT, HighPrice INT, LowPrice INT, Amount BIGINT);"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                    cmd.ExecuteNonQuery()             '명령 실행
                    cmd.Dispose()
                    last_candle_date = Now.Date - TimeSpan.FromDays(365)
                End If
                _db_connection.Close()
                _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Integrated Security=SSPI;")
                _db_connection.Open()
#End If

            Catch ex As Exception
#If 0 Then
                '생성에 성공했지만 여는데 실패.. why?
                If LastExceptionErrorMesssage <> ex.Message Then
                    LastExceptionErrorMesssage = ex.Message
                    ErrorLogging("Exception: 열기실패" & ex.Message)
                End If
                'MsgBox("DB 생성에 재~실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                _db_connection.Close()              'DB connection 닫기
                'Continue For            ' 실패했지만 다음 DB 부터 이어서 계속한다.
                Exit Sub
#End If
                '테이블 존재하지 않음 => 테이블 생성
                MessageLogging("MainForm " & "설마 테이블생성?")
                command_text = "CREATE TABLE " & db_table_name & " (CandleTime DATETIME PRIMARY KEY, OpenPrice INT, ClosePrice INT, HighPrice INT, LowPrice INT, Amount BIGINT);"
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                cmd.ExecuteNonQuery()             '명령 실행
                cmd.Dispose()
                last_candle_date = Now.Date - TimeSpan.FromDays(365)
            End Try

#End If
            'Candle Chart data 받기 위한 작업
            _T8412.ResFileName = "res\t8412.res"
            SymbolCodeOnRequest = table_name_list(table_index).Substring(1, table_name_list(table_index).Length - 1)

            _T8412.SetFieldData("t8412InBlock", "shcode", 0, SymbolCodeOnRequest)      '단축코드
            _T8412.SetFieldData("t8412InBlock", "ncnt", 0, "1L")        '단위(n틱/n분)
            _T8412.SetFieldData("t8412InBlock", "comp_yn", 0, "Y")      '압축여부(Y:압축,N:비압축)
            _T8412.SetFieldData("t8412InBlock", "sdate", 0, (last_candle_date + TimeSpan.FromDays(1)).ToString("yyyyMMdd"))       '시작일자(일/주/월 해당)
            _T8412.SetFieldData("t8412InBlock", "edate", 0, Now.Date.ToString("yyyyMMdd"))       '종료일자(일/주/월 해당)
            '_T8412.SetFieldData("t8412InBlock", "sdate", 0, (last_candle_date + TimeSpan.FromDays(1)).ToString("yyyyMMdd"))       '시작일자(일/주/월 해당)
            '_T8412.SetFieldData("t8412InBlock", "edate", 0, (Now.Date - TimeSpan.FromDays(300)).ToString("yyyyMMdd"))       '종료일자(일/주/월 해당)

            Dim local_retry_count As Integer = 0
            Do
                ResponseReceived = False
                RxProcessCompleted = False
                'request_time = Now.TimeOfDay
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
                            'My.Computer.FileSystem.WriteAllText("ChartData\" & SymbolCodeOnRequest & "_noresponse.txt", "", False)
                            ErrorLogging("MainForm ChartData " & table_index & ": " & SymbolCodeOnRequest & " no response")
                            NoResponseNumber = NoResponseNumber + 1
                            '_T8412 = New XAQuery
                            'LoginProcess()      '로긴을 다시 하고
                            'CtsDate = ""        '후속 data 받는 거 없이 다음으로
                            'CtsTime = ""        '넘어가기 위한 조치 취하고 가자
                            RxProcessCompleted = True
                            Exit Do
                        End If
                    Loop Until ResponseReceived
                    '190418: Response callback에서 대기하면 Form이 마비되는 것 같다. 그 안의 내용을 별도 Thread로 빼는 것을 생각해보자.
                    'Reponse 를 받았으면 이리 내려온다.
                    While Not RxProcessCompleted
                        Threading.Thread.Sleep(10)
                        'My.Application.DoEvents()  '2020.03.13 : 이거 문제 있나봐. 여기서 자꾸 에러나서 막아 놓음
                    End While
                    'Data 처리가 다 끝났으면 이리 내려온다.
                    If (_CandleChartInsertErrorCount Mod 10000) > 0 Or (NoResponseNumber Mod 10000) > 0 Or (FailedSymbolNumber Mod 10000) > 0 Then
                        '에러가 발생했는데 retry 해본다.
                        If local_retry_count < 3 Then
                            LoginProcess()      '로긴을 다시 하고
                            Threading.Thread.Sleep(2000)
                            _T8412 = et31_XAQuery_Wrapper.NewOrUsed()
                            _T8412.ResFileName = "res\t8412.res"
                            SymbolCodeOnRequest = table_name_list(table_index).Substring(1, table_name_list(table_index).Length - 1)

                            _T8412.SetFieldData("t8412InBlock", "shcode", 0, SymbolCodeOnRequest)      '단축코드
                            _T8412.SetFieldData("t8412InBlock", "ncnt", 0, "1L")        '단위(n틱/n분)
                            _T8412.SetFieldData("t8412InBlock", "comp_yn", 0, "Y")      '압축여부(Y:압축,N:비압축)
                            _T8412.SetFieldData("t8412InBlock", "sdate", 0, (last_candle_date + TimeSpan.FromDays(1)).ToString("yyyyMMdd"))       '시작일자(일/주/월 해당)
                            _T8412.SetFieldData("t8412InBlock", "edate", 0, Now.Date.ToString("yyyyMMdd"))       '종료일자(일/주/월 해당)
                            CtsDate = ""
                            CtsTime = ""
                            If _CandleChartInsertErrorCount Mod 10000 > 0 Then
                                _CandleChartInsertErrorCount = 10000 * (_CandleChartInsertErrorCount \ 10000 + 1)        'healing current error and store past error
                            End If
                            If NoResponseNumber Mod 10000 > 0 Then
                                NoResponseNumber = 10000 * (NoResponseNumber \ 10000 + 1)        'healing current error and store past error
                            End If
                            If FailedSymbolNumber Mod 10000 > 0 Then
                                FailedSymbolNumber = 10000 * (FailedSymbolNumber \ 10000 + 1)        'healing current error and store past error
                            End If
                            _CandleChartRetryCount += 1
                            local_retry_count += 1
                            Continue Do
                        Else
                            'retry 횟수가 3회가 넘어가면 다시 시도하지 않고 기록하고 스킵한다.
                            ErrorLogging("MainForm " & table_index & "/" & table_name_list.Count & ", " & table_name_list(table_index) & "3번 retry 소용없어 skip한다")
                            local_retry_count = 0
                        End If
                    End If
                Else
                    '에러나면 빠져나온다.. 리트라이 하자
                    If local_retry_count < 3 Then
                        LoginProcess()      '로긴을 다시 하고
                        Threading.Thread.Sleep(2000)
                        _T8412 = et31_XAQuery_Wrapper.NewOrUsed()
                        _T8412.ResFileName = "res\t8412.res"
                        SymbolCodeOnRequest = table_name_list(table_index).Substring(1, table_name_list(table_index).Length - 1)

                        _T8412.SetFieldData("t8412InBlock", "shcode", 0, SymbolCodeOnRequest)      '단축코드
                        _T8412.SetFieldData("t8412InBlock", "ncnt", 0, "1L")        '단위(n틱/n분)
                        _T8412.SetFieldData("t8412InBlock", "comp_yn", 0, "Y")      '압축여부(Y:압축,N:비압축)
                        _T8412.SetFieldData("t8412InBlock", "sdate", 0, (last_candle_date + TimeSpan.FromDays(1)).ToString("yyyyMMdd"))       '시작일자(일/주/월 해당)
                        _T8412.SetFieldData("t8412InBlock", "edate", 0, Now.Date.ToString("yyyyMMdd"))       '종료일자(일/주/월 해당)
                        CtsDate = ""
                        CtsTime = ""
                        _CandleChartRetryCount += 1
                        local_retry_count += 1
                        Continue Do
                    Else
                        'retry 횟수가 3회가 넘어가면 다시 시도하지 않고 기록하고 스킵한다.
                        'My.Computer.FileSystem.WriteAllText("ChartData\" & SymbolCodeOnRequest & "_failed" & RequestResult.ToString & ".txt", "", False)
                        FailedSymbolNumber = FailedSymbolNumber + 1
                        ErrorLogging("MainForm ChartData " & table_index & ": " & SymbolCodeOnRequest & " failed. " & RequestResult.ToString)
                        local_retry_count = 0
                        Exit Do
                    End If
                    'Threading.Thread.Sleep(3000)
                End If
                ProgressText3 = ", no response: " & NoResponseNumber & ", failed: " & FailedSymbolNumber & ", retry: " & _CandleChartRetryCount
                MessageLogging("MainForm " & "ChartData " & table_index & ": " & SymbolCodeOnRequest & " 만들기 종료됨")
                If CtsDate = "" OrElse CtsTime = "" Then
                    Exit Do
                End If
            Loop
            'CurrentNumber = table_index
            '_db_connection.Close()
            'OleDb.OleDbConnection.ReleaseObjectPool()
        Next

        'bt_ChartDataUpdate.Enabled = True
        'CurrentNumber = TotalNumber
        If SavePhase = 2 Then
            SavePhase = 3       '세이브 행동 다 끝났어요
        End If
        IsThreadExecuting = False
    End Sub
#If 0 Then
    Private Sub Button3_Click_1(sender As Object, e As EventArgs) Handles bt_ChartDataValidate.Click
        Dim chart_data_validate_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDataValidateThread)
        chart_data_validate_thread.IsBackground = True
        chart_data_validate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        tm_FormUpdate.Start()       '관리 timer 시작

        bt_ChartDataValidate.Enabled = False
    End Sub
#End If

    Private Sub ChartDataValidateThread()
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim read_result As OleDb.OleDbDataReader

        Dim file_list = My.Computer.FileSystem.GetFiles(CANDLE_CHART_FOLDER)
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
        If cb_AutoUpdate.Checked Then
            start_date = Now.Date - TimeSpan.FromDays(Math.Ceiling(c03_Symbol.MAX_NUMBER_OF_CANDLE / 381) * 26 / 5) 'dtp_LastCandleDate.Value '2022.02.14: 다른 곳처럼 여기도 휴일을 고려할 필요가 있다.
            start_date_string = start_date.ToString("yyyy-MM-dd")
        Else
            start_date = dtp_LastCandleDate.Value
            start_date_string = start_date.ToString("yyyy-MM-dd")
        End If
        end_date = Now.Date
        end_date_string = end_date.ToString("yyyy-MM-dd")

        For index As Integer = 0 To file_list.Count - 1
            'If Not file_list(index).Contains("CandleChartDB_A000030_main.mdf") Then
            'Continue For
            'End If

            file_extension = IO.Path.GetExtension(file_list(index))
            If file_extension = ".mdf" Then
                Try
                    db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)
                    table_name = db_name.Substring(14, 7)
                    'DB open
                    _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Initial Catalog=" & db_name & "; Integrated Security=SSPI;")
                    _db_connection.Open()

                    command_text = "SELECT CandleTime from " & table_name & " WHERE CandleTime BETWEEN '" & start_date_string & " 00:00:00' AND '" & end_date_string & " 23:59:59' ORDER BY CandleTime ASC"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
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
                    MessageLogging("MainForm " & "CharDataValidate : " & db_name & ", " & result_index & " / " & count_count & ", " & datA_count_list(result_index) & ", " & datE_count_list(result_index))
                Next

                ProgressText1 = index & "/" & file_list.Count
                _db_connection.Close()
            End If
            'CurrentNumber = index
        Next

        'CurrentNumber = TotalNumber
        IsThreadExecuting = False
    End Sub
#If 0 Then
    Public ChartObj As New CPSYSDIBLib.StockChart

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ChartObj.SetInputValue(0, "A005930")

        'ChartObj.SetInputValue(0, fields)
        ' ChartObj.SetInputValue(1, all_code.Split(","))
        ChartObj.SetInputValue(2, 1)
        'NumberOfRequest = NumberOfRequest + 1
        ChartObj.BlockRequest()
    End Sub
#End If

    Private Sub bt_ChartDataValidate_Click(sender As Object, e As EventArgs) Handles bt_ChartDataValidate.Click
        If IsBadTimeToMakeCandle Then
            If MsgBox("Candle 점검하기 좋은 시간 아닌데 그냥 할래요? 계속하면 가격 타이머 꺼집니다?", MsgBoxStyle.YesNo) <> MsgBoxResult.Yes Then
                Exit Sub
            End If
        End If

        Dim chart_data_validate_thread As Threading.Thread = New Threading.Thread(AddressOf ChartDataValidateThread)

        stm_PriceClock.Stop()         '가격 타이머 돌아가고 있으면 멈춘다.
        stm_PriceClock.Interval = 1 ' 안 돌아가고 있다는 걸 표시하려고 interval 을 1 으로 한다.

        chart_data_validate_thread.IsBackground = True
        chart_data_validate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        'tm_200msClock.Start()       '관리 timer 시작
        'IsThreadExecuting = True
        bt_ChartDataValidate.Enabled = False

    End Sub

    Private Sub cb_AutoUpdate_CheckedChanged(sender As Object, e As EventArgs) Handles cb_AutoUpdate.CheckedChanged
        If cb_AutoUpdate.Checked Then
            dtp_LastCandleDate.Enabled = False
            tb_DBListFileName.Enabled = False
        Else
            dtp_LastCandleDate.Enabled = True
            tb_DBListFileName.Enabled = True
        End If
    End Sub
#If 0 Then
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim remove_chart_db_contain_nothing_thread As Threading.Thread = New Threading.Thread(AddressOf RemoveAllChartDB)

        stm_PriceClock.Stop()         '가격 타이머 돌아가고 있으면 멈춘다.

        remove_chart_db_contain_nothing_thread.IsBackground = True
        remove_chart_db_contain_nothing_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        'tm_200msClock.Start()       '관리 timer 시작
        'IsThreadExecuting = True
    End Sub
#Else
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim nok_symbol_count As Integer = 0
        For index As Integer = 0 To SymbolList.Count - 1
            If SymbolList(index).MyOrderNumberList.Count <> 0 Then
                nok_symbol_count = nok_symbol_count + 1
                MessageLogging("MainForm " & SymbolList(index).Code & " : 이 심볼에 order data 가 조금 남아있어요. 오더 갯수는 이만큼 " & SymbolList(index).MyOrderNumberList.Count)
            End If
        Next

        MessageLogging("MainForm " & "MyOrderList 검사결과 order data가 조금 남아있는 심볼 갯수가 이만큼 되네요. " & nok_symbol_count.ToString)
        MessageLogging("MainForm " & "00600 주문객체 receive event 호출횟수 : " & OrderConfirmEventCount.ToString & ", Trace : " & OrderConfirmEventTracingKey.Var.ToString & ", " & OrderConfirmEventTracingKey.Time.ToString)
        MessageLogging("MainForm " & "00600 주문객체 post thread 호출횟수 : " & OrderConfirmPostCount.ToString & ", Trace : " & OrderConfirmPostTracingKey.Var.ToString & ", " & OrderConfirmPostTracingKey.Time.ToString)
        MessageLogging("MainForm " & "Main 계좌 sub confirm checker 등록수 : " & MainAccountManager.SubConfirmCheckerOperator.Count)
        MessageLogging("MainForm " & "Sub 계좌 sub confirm checker 등록수 : " & SubAccountManager.SubConfirmCheckerOperator.Count)
        MessageLogging("MainForm " & "Test 계좌 sub confirm checker 등록수 : " & TestAccountManager.SubConfirmCheckerOperator.Count)
    End Sub
#End If

    '모든 DB 제거 혹은 모든 DB 등록
    Private Sub RemoveAllChartDB()
        '리스트에서 읽어와서 drop 하자
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim db_name, table_name As String

        Dim table_name_list As New List(Of String)
        Dim file_list = My.Computer.FileSystem.GetFiles(CANDLE_CHART_FOLDER)
        For index As Integer = 0 To file_list.Count - 1
            If IO.Path.GetExtension(file_list(index)) = ".mdf" Then
                db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)
                table_name = db_name.Substring(14, 7)
                'If Not table_name_list.Contains(table_name) Then
                table_name_list.Add(table_name)
                'End If
                If table_name = "A269420" Then
                    Dim a = 1
                End If
            End If
        Next

        _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;")
        'master DB 접속
        _db_connection.Open()

        For index As Integer = 0 To table_name_list.Count - 1
            'If index <= 2506 Then
            'Continue For
            'End If
            'DB 삭제 혹은 붙이기
            db_name = "CandleChartDB_" & table_name_list(index)
            Try
#If 0 Then
                command_text = "DROP DATABASE " & db_name
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.ExecuteNonQuery()
                cmd.Dispose()
                MessageLogging("index " & index.ToString & ", " & db_name & " DB 삭제했습니다.")
                Threading.Thread.Sleep(100) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
#Else
                command_text = "CREATE DATABASE " & db_name & " ON PRIMARY " &
                      "(NAME = " & db_name & "_main, " &
                      " FILENAME = '" & CANDLE_CHART_FOLDER & db_name & "_main.mdf', " &
                      " FILEGROWTH = 10%) " &
                      " LOG ON " &
                      "(NAME = " & db_name & "_log, " &
                      " FILENAME = '" & CANDLE_CHART_FOLDER & db_name & "_log.ldf', " &
                      " FILEGROWTH = 10%) FOR ATTACH"
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                cmd.ExecuteNonQuery()
                cmd.Dispose()
                MessageLogging("MainForm " & "index " & index.ToString & ", " & db_name & " DB 붙였습니다.")
                Threading.Thread.Sleep(100) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
#End If
            Catch ex As Exception
                'DB 삭제에 실패했습니다.
                If LastExceptionErrorMesssage <> ex.Message Then
                    LastExceptionErrorMesssage = ex.Message
                    ErrorLogging("MainForm Exception:" & "index " & index.ToString & ex.Message)
                End If
                'MsgBox("DB 삭제에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                _db_connection.Close()              'DB connection 닫기
                Exit Sub
            End Try

            ProgressText1 = index & "/" & table_name_list.Count
        Next
        _db_connection.Close()

        IsThreadExecuting = False
    End Sub

    '아무것도 없는 DB 제거
    Private Sub RemoveChartDBContainNothing()
#If 0 Then
        '리스트에서 읽어와서 drop 하자
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String

        Dim table_name_list = My.Computer.FileSystem.ReadAllText(tb_DBListFileName.Text).Replace(vbCr, "").Split(vbLf).ToList
        Dim db_name As String = ""
        'Dim table_name As String

        _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=localhost;Integrated Security=SSPI;")
        'master DB 접속
        _db_connection.Open()

        For index As Integer = 0 To table_name_list.Count - 1
            'DB 삭제
            db_name = "CandleChartDB_" & table_name_list(index)
            Try
                command_text = "DROP DATABASE " & db_name
                cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                cmd.ExecuteNonQuery()
                cmd.Dispose()
                MessageLogging("index " & index.ToString & ", " & db_name & " DB 삭제했습니다.")
                Threading.Thread.Sleep(1000) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
            Catch ex As Exception
                'DB 삭제에 실패했습니다.
                If LastExceptionErrorMesssage <> ex.Message Then
                    LastExceptionErrorMesssage = ex.Message
                    ErrorLogging("Exception:" & "index " & index.ToString & ex.Message)
                End If
                'MsgBox("DB 삭제에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                _db_connection.Close()              'DB connection 닫기
                Exit Sub
            End Try

            ProgressText1 = index & "/" & table_name_list.Count
        Next
        _db_connection.Close()

        IsThreadExecuting = False

#End If
#If 0 Then          '이거는 지울 list 만드는 거
        IsThreadExecuting = True
        Dim cmd As OleDb.OleDbCommand
        Dim command_text As String
        Dim read_result As OleDb.OleDbDataReader

        Dim file_list = My.Computer.FileSystem.GetFiles(_CANDLE_CHART_FOLDER)
        Dim file_extension As String
        Dim db_name As String = ""
        Dim table_name As String

        For index As Integer = 0 To file_list.Count - 1
            'If Not file_list(index).Contains("CandleChartDB_A000030_main.mdf") Then
            'Continue For
            'End If

            file_extension = IO.Path.GetExtension(file_list(index))
            If file_extension = ".mdf" Then
                Try
                    db_name = IO.Path.GetFileName(file_list(index)).Substring(0, 21)
                    table_name = db_name.Substring(14, 7)
                    'DB open
                    _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Initial Catalog=" & db_name & "; Integrated Security=SSPI;")
                    _db_connection.Open()

                    command_text = "SELECT TOP 1000 CandleTime from " & table_name & " ORDER BY CandleTime ASC"
                    cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                    read_result = cmd.ExecuteReader()

                    '  While 1
                    If read_result.Read Then
                        '데이타가 있다.
                    Else
                        '데이타가 없다.
                        cmd.Dispose()

                        MessageLogging("index " & index.ToString & ", " & db_name & " DB 데이터 없어요 삭제해주세요.")
#If 0 Then
                        'DB 삭제
                        _db_connection.Close()
                        _db_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=localhost;Integrated Security=SSPI;")

                        Try
                            'master DB 접속
                            _db_connection.Open()

                            command_text = "DROP DATABASE " & db_name
                            cmd = New OleDb.OleDbCommand(command_text, _db_connection)
                            cmd.ExecuteNonQuery()
                            cmd.Dispose()
                            _db_connection.Close()
                            MessageLogging("index " & index.ToString & ", " & db_name & " DB 삭제했습니다.")
                            Threading.Thread.Sleep(1000) '좀 쉬었다가. 엄청 많이 쉬자. DB 파일 만든지 얼마 안 됐으니까
                        Catch ex As Exception
                            'DB 삭제에 실패했습니다.
                            If LastExceptionErrorMesssage <> ex.Message Then
                                LastExceptionErrorMesssage = ex.Message
                                ErrorLogging("Exception:" & ex.Message)
                            End If
                            'MsgBox("DB 삭제에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                            _db_connection.Close()              'DB connection 닫기
                            Exit Sub
                        End Try
#End If
                        'Exit While
                    End If
                    ' End While
                Catch ex As Exception
                    Dim a = 1
                End Try

                ProgressText1 = index & "/" & file_list.Count
                _db_connection.Close()
            End If
            'CurrentNumber = index
        Next

        'CurrentNumber = TotalNumber
        IsThreadExecuting = False
#End If
    End Sub

    Private Sub XAReal_News_ReceiveRealData(szTrCode As String) Handles XAReal_News.ReceiveRealData
        Dim output_date As String = XAReal_News.GetFieldData("OutBlock", "date")
        Dim output_time As String = XAReal_News.GetFieldData("OutBlock", "time")
        Dim output_id As String = XAReal_News.GetFieldData("OutBlock", "id")
        Dim output_realkey As String = XAReal_News.GetFieldData("OutBlock", "realkey")
        Dim output_title As String = XAReal_News.GetFieldData("OutBlock", "title")
        Dim output_code As String = XAReal_News.GetFieldData("OutBlock", "code")
        Dim output_bodysize As String = XAReal_News.GetFieldData("OutBlock", "bodysize")
        Dim one_line As String

        one_line = "Date: " & output_date & vbCrLf
        one_line += "Time: " & output_time & vbCrLf
        one_line += "Id: " & output_id & vbCrLf
        one_line += "Realkey: " & output_realkey & vbCrLf
        one_line += "Title: " & output_title & vbCrLf
        one_line += "Code: " & output_code & vbCrLf
        one_line += "Bodysize: " & output_bodysize & vbCrLf
        one_line += vbCrLf

        For retry_index As Integer = 0 To 10
            Try
                My.Computer.FileSystem.WriteAllText(LogFileFolder & "\" & "news" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", one_line, True)
                Exit For
            Catch ex As Exception
                If LastExceptionErrorMesssage <> ex.Message Then
                    LastExceptionErrorMesssage = ex.Message
                    ErrorLogging("MainForm Exception: 저장실패4" & ex.Message)
                End If
                Threading.Thread.Sleep(200)
            End Try
        Next
    End Sub
End Class