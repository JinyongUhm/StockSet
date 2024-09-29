Imports DSCBO1Lib
Imports CPUTILLib
Imports System.Data.SqlClient

Public Class Form1

    'Public StockMst2Obj As New StockMst2
    'Public StockBidObj As StockBid
    Private Const _STOCK_HI_PRICE As Integer = 10000
    Private Const _LO_VOLUME As Long = 30000000
    Private _Counting As Integer = 0
    Public DBSupporter As New c04_DBSupport
    Public RealTimeMode As Boolean
    Public IsThreadExecuting As Boolean

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If RealTimeMode Then
            If IsThreadExecuting Then
                '스레드 실행중엔 종료전에 물어봄
                If MsgBox("스레드 실행중입니다. 진짜로 종료할까요?", MsgBoxStyle.YesNo) = MsgBoxResult.No Then
                    e.Cancel = True
                    Exit Sub
                End If
            End If
            Select Case MsgBox("종목시세 저장할까요?", MsgBoxStyle.YesNoCancel)
                Case MsgBoxResult.Yes
                    '저장한다.
                    DBSupporter.SavePriceInformation()
                Case MsgBoxResult.No
                    '저장 안 하고 버린다
                Case Else
                    '닫는 거 취소
                    e.Cancel = True
            End Select
        End If
    End Sub

    Private Sub Form1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'tm_1mClock.Start()          'start 1minute timer
        'StockMst2Obj.SetInputValue(0, tb_StockCode.Text)        '종목 코드 세팅
        GlobalVarInit(Me)                   '전역변수 초기화

        If MsgBox("실시간 모드로 할래요? (No는 시뮬레이션 모드)", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            '실시간 모드
            RealTimeMode = True
            SymbolCollection()                      '종목 알아내기
            tm_15sClock_Tick(Nothing, Nothing)       'Get the very first data
            tm_15sClock.Start()                     '15초 타이머 돌리기
            TabControl1.SelectedTab = TabControl1.TabPages(1)
        Else
            '시뮬레이션 모드
            RealTimeMode = False
            TabControl1.SelectedTab = TabControl1.TabPages(2)
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
        Dim symbol_obj As c03_Symbol
        Dim bunch_obj As c02_Bunch = Nothing
        Dim selected_bunch_obj As c02_Bunch = Nothing

        SymTree.StartSymbolListing()            '종목 모으기 시작
        For index As Integer = 0 To cp_stock_code_obj.GetCount - 1
            symbol_code = cp_stock_code_obj.GetData(0, index)
            symbol_name = cp_stock_code_obj.GetData(1, index)

            symbol_obj = New c03_Symbol(symbol_code, symbol_name)    '종목 개체 생성
            SymbolList.Add(symbol_obj)                                     '종목리스트에 추가
            market_kind = cp_code_mgr_obj.GetStockMarketKind(symbol_code)
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
                                        list_view_item.SubItems.Add(0)      '현재 체결강도
                                        list_view_item.SubItems.Add(0)          'data 갯수
                                        list_view_item.SubItems.Add(0)          '이동평균
                                        list_view_item.SubItems.Add(0)          '델타매수량
                                        list_view_item.SubItems.Add(0)          '델타매도량
                                        lv_Symbols.Items.Add(list_view_item)

                                        bunch_obj.Item(result_count).Initialize()           '해당 종목 주가 모니터링 시작
                                        bunch_obj.Item(result_count).LVItem = list_view_item    '리스트뷰아이템 설정
                                        '아래에서 첫 데이터를 넘겨준다.
                                        'bunch_obj.Item(result_count).SetNewData(now_price, amount, gangdo)
                                        SymTree.AddSymbol(bunch_obj.Item(result_count))       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
                                    Else
                                        '선택되지 않은 종목은 초기 가격정보를 지운다
                                        bunch_obj.Item(result_count).Initialize()
                                    End If

                                Next
                                '다음 번치를 위한 준비과정
                                bunch_obj.Terminate()
                                bunch_obj = Nothing
                            End If
                        End If
                    End If
                End If

            End If
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
                    list_view_item.SubItems.Add(0)      '현재 체결강도
                    list_view_item.SubItems.Add(0)          'data 갯수
                    list_view_item.SubItems.Add(0)          '이동평균
                    list_view_item.SubItems.Add(0)          '델타매수량
                    list_view_item.SubItems.Add(0)          '델타매도량
                    lv_Symbols.Items.Add(list_view_item)

                    bunch_obj.Item(result_count).Initialize()           '해당 종목 주가 모니터링 시작
                    bunch_obj.Item(result_count).LVItem = list_view_item    '리스트뷰아이템 설정
                    '아래에서 첫 데이터를 넘겨준다.
                    'bunch_obj.Item(result_count).SetNewData(now_price, amount, gangdo)
                    SymTree.AddSymbol(bunch_obj.Item(result_count))       'SymTree에 선택된 종목 넘긴다. 그러면 여기서 적당한 번치에 분배된다.
                End If

            Next
        End If
        '번치 클리어
        bunch_obj.Terminate()

        SymTree.FinishSymbolListing()           '종목 모으기 끝

        '종목리스트에 대해 DB 상태 정보 업데이트
        DBSupporter.UpdateDBStatus()

        Text = lv_Symbols.Items.Count
    End Sub

    '15초마다 종목시세 업데이트
    Private Sub tm_15sClock_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tm_15sClock.Tick
        If IsMarketTime Then
            '장중이면
            SymTree.ClockSupply()           '시세 업데이트 클락 공급
        End If
    End Sub

    '시뮬레이션 시작
    Private Sub bt_StartSimul_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bt_StartSimul.Click
        Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf SimulateThread)

        simulate_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
        tm_FormUpdate.Start()       '관리 timer 시작
        IsThreadExecuting = True
        bt_StartSimul.Enabled = False
    End Sub

    '시뮬레이션 스레드용
    Private Sub SimulateThread()
        DBSupporter.Simulate(dtp_StartDate.Value.Date, dtp_EndDate.Value.Date)
        IsThreadExecuting = False           'thread 종료 flag reset
    End Sub

    'Form update timer
    Private Sub tm_FormUpdate_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles tm_FormUpdate.Tick
        If IsThreadExecuting Then
            Me.Text = DBSupporter.ProgressValue & " / " & DBSupporter.ProgressMax
            If DBSupporter.ProgressMax <> 0 Then
                pb_Progress.Value = DBSupporter.ProgressValue * 100 / DBSupporter.ProgressMax
            End If
        Else
            'thread 종료 되었음
            IsThreadExecuting = False
            bt_StartSimul.Enabled = True
            tm_FormUpdate.Stop()
        End If
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
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
        Dim my_conn As SqlConnection = New SqlConnection("Server=Printemp; database=master;")
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
        Dim _DB_connection As OleDb.OleDbConnection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=PRINTEMP\SQLEXPRESS;Integrated Security=SSPI")
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
    End Sub
End Class