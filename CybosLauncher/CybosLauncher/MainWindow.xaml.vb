Imports CPUTILLib

Class MainWindow
    Public ProgressText1, ProgressText2 As String
    Private Const _STOCK_HI_PRICE As Integer = 100000000
    Private Const _LO_VOLUME As Long = 100000
    Public IsLoadingDone As Boolean
    Private _IsInitializationAfterLoadingDone As Boolean = False
    Public IsThreadExecuting As Boolean
    Public WithEvents tm_200msClock As System.Windows.Threading.DispatcherTimer
    'Public WithEvents stm_PriceClock As New System.Timers.Timer()
    Public ClockThread As New Threading.Thread(AddressOf ClockLoop)
    Public CloseMe As Boolean = False
    'Declare Function SetSysColors Lib "user32" (ByVal nChanges As Long, lpSysColor As Long, lpColorValues As Long) As Long  'title bar 색깔 바꾸기 위함
    'Public CandleListSet As List(Of List(Of CandleServiceInterfacePrj.CandleStructure))
    'Public CandleServiceCenter As New CandleService()
    'Public CandleServiceHost As New ServiceModel.ServiceHost(GetType(CandleService))

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        'CandleServiceHost.Open()

        '대신증권 접속
        ConnectDaishinServer(True)     'confirm restart 를 true로 하니까 이상하게 안 넘어간다.
        MaxNumberOfRequest = 200

        'CpStuckMMF 생성
        Try
            CpStuckMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("CpStuckMMF")
            SetCpStuck(0)       '초기 CpStuck값 0으로 설정함
            MessageLogging("CpStuckMMF 0 세팅 완료")
        Catch ex As Exception
            MessageLogging("CpStuckMMF 생성 안 된 듯. PriceMiner없이 단독실행인가?")
        End Try

        Try
            StoredMessagesMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("StoredMessagesMMF")
        Catch ex As Exception
            MessageLogging("StoredMessagesMMF 생성 안 된 듯. PriceMiner없이 단독실행인가?")
        End Try

        Try
            StoredMessagesMutex = Threading.Mutex.OpenExisting("StoredMessagesMutex")
        Catch ex As Exception
            MessageLogging("StoredMessagesMutex 생성 안 된 듯. PriceMiner없이 단독실행인가?")
        End Try

        Try
            CybosBasicDataMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("CybosBasicDataMMF")
        Catch ex As Exception
            MessageLogging("CybosBasicDataMMF 생성 안 된 듯. PriceMiner없이 단독실행인가?")
        End Try

        Try
            CybosRealDataMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting("CybosRealDataMMF")
        Catch ex As Exception
            MessageLogging("CybosRealDataMMF 생성 안 된 듯. PriceMiner없이 단독실행인가?")
        End Try

        'MMF와는 달리 Mutex는 CybosLauncher가 가지고 있는 것으로 한다.
        Try
            CybosRealDataMutex = New Threading.Mutex(False, "CybosRealDataMutex")
        Catch ex As Exception
            MessageLogging("누가 CybosRealDataMutex 벌써 만들었어?")
        End Try

        Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf LoadingThread)
        simulate_thread.IsBackground = True
        IsLoadingDone = False
        simulate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        IsThreadExecuting = True

        GlobalVarInit(Me)                   '전역변수 초기화

        '200ms timer 시작
        tm_200msClock = New System.Windows.Threading.DispatcherTimer()
        tm_200msClock.Interval = TimeSpan.FromMilliseconds(200)
        tm_200msClock.Start()
        'Dim s = SetSysColors(1, 9, 759378674)   'title bar 색깔 바꿔보려고 했는데 잘 안 되네
    End Sub

    Private Sub tm_200msClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tm_200msClock.Tick
        'Me.Title = CandleServiceCenter.CandleListSet.Count
        If CloseMe Then
            Close()
        End If
        If IsLoadingDone AndAlso Not _IsInitializationAfterLoadingDone Then
#If 0 Then
            'Candle저장소의 메모리를 CybosLauncher가 갖고 있도록 한다.
            Try
                CybosCandleStoreMMF = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateNew("CybosCandleStoreMMF", CType(System.Runtime.InteropServices.Marshal.SizeOf(GetType(CandleServiceInterfacePrj.CandleStructure)), Long) * 9601 * SymTree.Count)
            Catch ex As Exception

            End Try
#End If

            If CybosBasicDataMMF IsNot Nothing Then
                'PriceMiner에 시간정보와 심볼기본정보를 전송한다.
                Dim cybos_data_accessor As System.IO.MemoryMappedFiles.MemoryMappedViewAccessor = CybosBasicDataMMF.CreateViewAccessor(0, SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * 5000)
                Dim number_of_bunches As Integer = SymTree.Count
                Dim number_of_symbols As Integer = 0
                For bunch_index As Integer = 0 To number_of_bunches - 1
                    number_of_symbols += SymTree(bunch_index).Count
                Next
                Dim empty_code_name() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
                Dim code_buff, name_buff As Byte()
                cybos_data_accessor.Write(0, MarketStartTime)
                cybos_data_accessor.Write(4, MarketEndTime)
                cybos_data_accessor.Write(8, MarketStartHour)
                cybos_data_accessor.Write(12, MarketStartMinute)
                cybos_data_accessor.Write(16, MarketEndHour)
                cybos_data_accessor.Write(20, MarketEndMinute)
                cybos_data_accessor.Write(24, number_of_symbols)
                Dim global_symbol_index As Integer = 0
                For bunch_index As Integer = 0 To number_of_bunches - 1
                    For symbol_index_in_bunch As Integer = 0 To SymTree(bunch_index).Count - 1
                        cybos_data_accessor.WriteArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index, empty_code_name, 0, 32)       'length:32
                        code_buff = System.Text.Encoding.UTF8.GetBytes(SymTree(bunch_index)(symbol_index_in_bunch).Code)
                        name_buff = System.Text.Encoding.UTF8.GetBytes(SymTree(bunch_index)(symbol_index_in_bunch).Name)
                        cybos_data_accessor.WriteArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index, code_buff, 0, code_buff.Length)       'length:12
                        cybos_data_accessor.WriteArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 12, name_buff, 0, name_buff.Length)       'length:20
                        cybos_data_accessor.Write(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 32, (CType(SymTree(bunch_index)(symbol_index_in_bunch).MarketKind, Integer)))       'length:4
                        cybos_data_accessor.Write(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 36, (CType(SymTree(bunch_index)(symbol_index_in_bunch).Caution, Integer)))       'length:4
                        cybos_data_accessor.Write(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 40, (CType(SymTree(bunch_index)(symbol_index_in_bunch).Supervision, Integer)))       'length:4
                        cybos_data_accessor.Write(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 44, SymTree(bunch_index)(symbol_index_in_bunch).OpenPrice)       'length:4
                        cybos_data_accessor.Write(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 48, SymTree(bunch_index)(symbol_index_in_bunch).LowLimitPrice)       'length:8
                        cybos_data_accessor.Write(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * global_symbol_index + 56, SymTree(bunch_index)(symbol_index_in_bunch).YesterPrice)       'length:4
                        global_symbol_index += 1
                    Next
                Next
            End If

            'CybosBasicDataMutex를 만들어 basic data 가 ready 되었음을 알린다.
            Try
                CybosBasicDataMutex = New Threading.Mutex(False, "CybosBasicDataMutex")
            Catch ex As Exception
                MessageLogging("누가 CybosBasicDataMutex 벌써 만들었어?")
            End Try

            '가격 타이머 시작
            'stm_PriceClock_Tick(Nothing, Nothing)       'Get the very first data
            'stm_PriceClock.Interval = 5000           '5초 타이머 interval 설정
            'stm_PriceClock.Start()                     '5초 타이머 돌리기
            '2024.07.11 : clock supply의 시간 정확도를 높이기 위해 Now 를 사용하는 thread 를 만든다.
            Dim clock_thread As Threading.Thread = New Threading.Thread(AddressOf ClockLoop)
            clock_thread.IsBackground = True
            clock_thread.Start()

            _IsInitializationAfterLoadingDone = True
        End If

        If IsThreadExecuting Then
            Me.Title = ProgressText1 & " " & ProgressText2
        Else
            'thread 종료 되었음
        End If

        MessageDisplay()
    End Sub
#If 0 Then
    Private Sub stm_PriceClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles stm_PriceClock.Elapsed
        If IsMarketTime AndAlso Not IsThreadExecuting Then
            '장중이고 로딩 thread 등 돌아가지 않을 때면
            SymTree.ClockSupply()           '시세 업데이트 클락 공급
        End If
    End Sub
#End If

    Private Sub ClockLoop()
        Dim start_time As TimeSpan = Now.TimeOfDay
        Dim current_time As TimeSpan
        Dim five_sec_count As UInt32 = 0
        While (1)
            current_time = Now.TimeOfDay
            If current_time > start_time + TimeSpan.FromSeconds(5 * five_sec_count) Then
                five_sec_count += 1
                If IsMarketTime AndAlso Not IsThreadExecuting Then
                    '장중이고 로딩 thread 등 돌아가지 않을 때면
                    SymTree.ClockSupply()           '시세 업데이트 클락 공급
                End If
            End If
            Threading.Thread.Sleep(10)
        End While
    End Sub

    Public Sub ConnectDaishinServer(ByVal confirm_restart As Boolean)
        '재연결을 시도한다.
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
    End Sub

    Private Sub LoadingThread()
        Dim number_of_symbols_recorded As Integer
        Dim basic_data_accesser As IO.MemoryMappedFiles.MemoryMappedViewAccessor
        If CybosBasicDataMMF Is Nothing Then
            number_of_symbols_recorded = 0
        Else
            basic_data_accesser = CybosBasicDataMMF.CreateViewAccessor(24, 4)
            basic_data_accesser.Read(0, number_of_symbols_recorded)
        End If
        If number_of_symbols_recorded > 0 Then
            '이미 기록이 되어 있는 것이다.
            'LoadBasicData()                         '종목 로드하기
            SymbolCollection()                      '2021.06.13: LoadBasicData하니 종목수가 달라진다. 이상하니 일단 이걸로 쓰도록 하자.
        Else
            '베이직 데이터 없으므로 symbol collection 해야 된다.
            SymbolCollection()                      '종목 알아내기
        End If

        IsLoadingDone = True
        IsThreadExecuting = False
    End Sub
#If 0 Then
    Private Sub LoadBasicData()
        SymTree.StartSymbolListing()            '종목 모으기 시작
        Dim cybos_data_accessor As System.IO.MemoryMappedFiles.MemoryMappedViewAccessor = CybosBasicDataMMF.CreateViewAccessor(0, SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * 5000)
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
        cybos_data_accessor.Read(24, number_of_symbols)
        For index As Integer = 0 To number_of_symbols - 1
            cybos_data_accessor.ReadArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index, code_buff, 0, 12)        'length:12
            cybos_data_accessor.ReadArray(Of Byte)(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 12, name_buff, 0, 20)       'length:20
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 32, market_kind_int)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 36, caution)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 40, supervision)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 44, open_price)       'length:4
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 48, low_limit_price)       'length:8
            cybos_data_accessor.Read(SYMBOL_BASIC_START_OFFSET + SYMBOL_BASIC_DATA_SIZE * index + 56, yester_price)       'length:4

            symbol = New c03_Symbol(System.Text.Encoding.UTF8.GetString(code_buff).Replace(vbNullChar, ""), System.Text.Encoding.UTF8.GetString(name_buff).Replace(vbNullChar, ""))
            symbol.MarketKind = CType(market_kind_int, MARKET_KIND)
            symbol.Caution = CType(caution, Boolean)
            symbol.Supervision = CType(supervision, Boolean)
            symbol.OpenPrice = open_price
            symbol.LowLimitPrice = low_limit_price
            symbol.YesterPrice = yester_price
            SymTree.AddSymbol(symbol)       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
            ProgressText2 = index & " / " & number_of_symbols
        Next
        SymTree.FinishSymbolListing()   '종목 모으기 종료
        ProgressText1 = "Number of symbols loaded: " & number_of_symbols & ", Number of bunches: " & SymTree.Count
    End Sub
#End If

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
        Dim number_of_symbols_in_interest As Integer = 0
        Dim yester_price As Long
        Dim yester_amount As ULong
        Dim volume As Long
        Dim init_price As UInt32
        '        Dim amount As UInt64
        '        Dim gangdo As Double
        Dim symbol_obj As c03_Symbol = Nothing
        Dim bunch_obj As c02_Bunch = Nothing
        Dim selected_bunch_obj As c02_Bunch = Nothing
        Dim request_ok As Boolean

        SymTree.StartSymbolListing()            '종목 모으기 시작
        If cp_stock_code_obj.GetCount = 0 Then
            'COM 연결이 안 되어있을 가능성 99.9 %
            Exit Sub
        End If
        ProgressText1 = "Symbol Collection step - "
        ProgressText2 = "0 / " & cp_stock_code_obj.GetCount
        For index As Integer = 0 To cp_stock_code_obj.GetCount - 1
            symbol_code = cp_stock_code_obj.GetData(0, index)
            symbol_name = cp_stock_code_obj.GetData(1, index)

            market_kind = cp_code_mgr_obj.GetStockMarketKind(symbol_code)
            '130604: 대신증권 도움말에 종목별 증거금(100%,75%,50% 등) 알아내는 법을 알아보자. 근데 도움말 어떻게 읽어.
            control_kind = cp_code_mgr_obj.GetStockControlKind(symbol_code)
            supervision_kind = cp_code_mgr_obj.GetStockSupervisionKind(symbol_code)
            status_kind = cp_code_mgr_obj.GetStockStatusKind(symbol_code)
            last_end_price = cp_code_mgr_obj.GetStockYdClosePrice(symbol_code)

            If symbol_code.Substring(0, 1) = "A" Then
                'A로 시작하는 종목이면
                If market_kind = 1 Or market_kind = 2 Then
                    '소속부가 거래소 또는 코스닥이면
                    'If control_kind = 0 Or control_kind = 1 Then
                    '감리구분이 정상 또는 주의 이면
                    'If supervision_kind = 0 Then       '190816:관리구분 보는 거 완화. restore된 잔고계좌 연결이 안 되는 경우 많음
                    '관리구분이 일반종목이면
                    If status_kind = 0 Then
                        '주식상태가 거래정지나 거래중단이 아니면
                        symbol_obj = New c03_Symbol(symbol_code, symbol_name)    '종목 개체 생성
                        symbol_obj.MarketKind = market_kind         '마켓 종류 : 1: KOSPI, 2: KOSDAQ
                        'symbol_obj.EvidanRate = cp_code_mgr_obj.GetStockMarginRate(symbol_code)     '증거금률 읽어오기
                        symbol_obj.Caution = control_kind
                        symbol_obj.Supervision = supervision_kind

                        'symbol_obj.MyIndex = SymbolList.Count           '2021.06.08: 공유 data 속에서 자신의 위치를 알기 위해 추가한 index
                        SymbolList.Add(symbol_obj)                                     '종목리스트에 추가
                        If bunch_obj Is Nothing Then
                            '아직 번치가 생성되지 않았으면 하나 만든다
                            bunch_obj = New c02_Bunch(symbol_obj)
                        Else
                            '생성되어 있으면 종목을 덧붙인다.
                            bunch_obj.Add(symbol_obj)
                        End If

                        If bunch_obj.Count = MaxNumberOfRequest Then
                            '110개 다 모았으니 request 해야 된다.
                            bunch_obj.SymbolListFix()           '번치의 종목 리스트 마무리
                            request_ok = bunch_obj.Mst2BlockRequest(True)     '각 번치마다 update request
                            'If request_ok Then
                            '160328: 모든 데이터 0으로 받아진 거다. 재요청하면 제대로 받을 수 있다.
                            'request_ok = bunch_obj.Mst2BlockRequest()     '각 번치마다 update request
                            'End If
                            'bunch_obj.Mst2BlockRequest()        '리퀘스트...

                            For result_count As Integer = 0 To bunch_obj.Count - 1
                                symbol_code = bunch_obj.GetSymbolCode(result_count)
                                symbol_name = bunch_obj.GetSymbolName(result_count)
                                yester_price = bunch_obj.GetYesterdayPrice(result_count)      '전일종가
                                yester_amount = bunch_obj.GetYesterdayAmount(result_count)    '전일거래량
                                volume = yester_price * yester_amount                                '전일거대래금
                                bunch_obj(result_count).OpenPrice = yester_price

                                If yester_price < _STOCK_HI_PRICE And volume >= _LO_VOLUME Then
                                    '너무 비싸지 않은 가격에 거래량이 충분히 있는 종목을 고른다.
                                    init_price = bunch_obj.GetNowPrice(result_count)
                                    'amount = bunch_obj.GetAmount(result_count)
                                    'gangdo = bunch_obj.GetGangdo(result_count)
#If 0 Then
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
#End If
                                    number_of_symbols_in_interest += 1
                                    bunch_obj.Item(result_count).Initialize()           '해당 종목 주가 모니터링 시작
                                    'bunch_obj.Item(result_count).LVItem = list_view_item    '리스트뷰아이템 설정
                                    '아래에서 하한가 계산하여 넘겨준다
                                    bunch_obj.Item(result_count).LowLimitPrice = Convert.ToInt32(yester_price) * 0.7
                                    bunch_obj.Item(result_count).YesterPrice = yester_price               '전날가
                                    SymTree.AddSymbol(bunch_obj.Item(result_count))       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
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
                    'End If
                    'End If
                End If
            End If
            ProgressText2 = index & " / " & cp_stock_code_obj.GetCount
        Next

        If bunch_obj IsNot Nothing AndAlso bunch_obj.Count > 0 Then
            '110개 안 모였어도 request 해야 된다.
            bunch_obj.SymbolListFix()           '번치의 종목 리스트 마무리

            request_ok = bunch_obj.Mst2BlockRequest(True)     '각 번치마다 update request
            'If request_ok Then
            '160328: 모든 데이터 0으로 받아진 거다. 재요청하면 제대로 받을 수 있다.
            'request_ok = bunch_obj.Mst2BlockRequest()     '각 번치마다 update request
            'End If
            'bunch_obj.Mst2BlockRequest()        '리퀘스트...

            For result_count As Integer = 0 To bunch_obj.Count - 1
                symbol_code = bunch_obj.GetSymbolCode(result_count)
                symbol_name = bunch_obj.GetSymbolName(result_count)
                yester_price = bunch_obj.GetYesterdayPrice(result_count)      '전일종가
                yester_amount = bunch_obj.GetYesterdayAmount(result_count)    '전일거래량
                volume = yester_price * yester_amount                                '전일거대래금
                bunch_obj(result_count).OpenPrice = yester_price

                If yester_price < _STOCK_HI_PRICE And volume >= _LO_VOLUME Then
                    '너무 비싸지 않은 가격에 거래량이 충분히 있는 종목을 고른다.
                    init_price = bunch_obj.GetNowPrice(result_count)
                    'amount = bunch_obj.GetAmount(result_count)
                    'gangdo = bunch_obj.GetGangdo(result_count)
#If 0 Then
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
#End If
                    number_of_symbols_in_interest += 1

                    bunch_obj.Item(result_count).Initialize()           '해당 종목 주가 모니터링 시작
                    'bunch_obj.Item(result_count).LVItem = list_view_item    '리스트뷰아이템 설정
                    '아래에서 하한가 계산하여 넘겨준다
                    bunch_obj.Item(result_count).LowLimitPrice = Convert.ToInt32(yester_price) * 0.7
                    bunch_obj.Item(result_count).YesterPrice = yester_price               '전날가
                    SymTree.AddSymbol(bunch_obj.Item(result_count))       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
                Else
                    '선택되지 않은 종목은 초기 가격정보를 지운다
                    bunch_obj.Item(result_count).Initialize()
                End If

            Next

            MessageLogging(SymTree.Count.ToString & " 마지막 번치 끝")
            '번치 클리어
            bunch_obj.Terminate()
            bunch_obj = Nothing
        Else
            '번치 클리어 할 필요 없다.
        End If

        SymTree.FinishSymbolListing()           '종목 모으기 끝

        '종목리스트에 대해 DB 상태 정보 업데이트 =>나중에 저장할 때 한다.
        'DBSupporter.UpdateDBStatus()

        ProgressText1 = "Number of symbols : " & number_of_symbols_in_interest & " / " & number_of_symbols & ", Number of bunches: " & SymTree.Count
    End Sub

    'display messages in the message queue
    Private Sub MessageDisplay()
        SafeEnterTrace(StoredMessagesKeyForDisplay, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim text_bar As String = ""
        For index As Integer = 0 To StoredMessagesForDisplay.Count - 1
            If index = StoredMessagesForDisplay.Count - 1 Then
                text_bar = text_bar & StoredMessagesForDisplay(index)
            Else
                text_bar = text_bar & StoredMessagesForDisplay(index) & vbCrLf
            End If
        Next

        If tb_Display.Text.Length + text_bar.Length > tb_Display.MaxLength Then
            tb_Display.Clear()
        End If
        If text_bar.Length < tb_Display.MaxLength Then
            '20200325: maxlength 넘게 붙여넣는 거를 방지하기 위함. 자꾸 에러(대신증권 COM API 콜 할 때 값이 예상 범위를 벗어났습니다 에러)나는 것이 이것 때문인가 싶기도 하고..
            '20200329: 이렇게 해도 해당 에러나는 것을 확인했다. 이것 때문만은 아니다.
            tb_Display.AppendText(text_bar)
            tb_Display.ScrollToEnd()
        End If

        'StoredMessagesForFileSave.AddRange(StoredMessagesForDisplay.ToArray)        '디스플레이용 메세지들을 파일세이브용으로 카피한다.
        StoredMessagesForDisplay.Clear()

        SafeLeaveTrace(StoredMessagesKeyForDisplay, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        'CandleServiceHost.Close()
    End Sub
End Class
