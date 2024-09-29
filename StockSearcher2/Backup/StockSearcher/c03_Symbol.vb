Public Class c03_Symbol
    Public Structure SymbolRecord
        Dim Price As UInt32         '현재가
        Dim Amount As UInt64        '거래량
        Dim Gangdo As Double        '체결강도
        Dim BuyAmount As UInt64     '누적 매수거래량
        Dim SelAmount As UInt64     '누적 매도거래량
        Dim MAPrice As UInt32       '현재가 이동평균
        'Dim BuyDelta As UInt64      '델타 매수거래량
        'Dim SelDelta As UInt64      '델타 매도거래량
    End Structure

    Public Delegate Sub DelegateRegisterDecision(ByVal list_view_item)

    'Public CodeIndex As Integer
    Public Code As String
    Public Name As String
    Public _StartTime As DateTime = [DateTime].MinValue
    Public LVItem As ListViewItem
    Public DBTableExist As Boolean = False
    'Private _PriceList As New List(Of UInt32)
    'Private _AmountList As New List(Of UInt64)
    'Private _GangdoList As New List(Of Double)
    'Private _BuyAmountList As New List(Of UInt64)
    'Private _SelAmountlist As New List(Of UInt64)
    Public RecordList As New List(Of SymbolRecord)
    Public StockDecisionMaker As c050_DecisionMaker


    Public Sub New(ByVal symbol_code As String, ByVal symbol_name As String)
        'CodeIndex = code_index
        Code = symbol_code
        Name = symbol_name
        Initialize()
    End Sub

    '종목 기록 초기화
    Public Sub Initialize()
        'StartTime = start_time
        '_PriceList.Clear()
        '_AmountList.Clear()
        '_GangdoList.Clear()
        '_BuyAmountList.Clear()
        '_SelAmountlist.Clear()
        RecordList.Clear()
        StartTime = [DateTime].MinValue
        If StockDecisionMaker IsNot Nothing Then
            StockDecisionMaker.Clear()
        End If
        StockDecisionMaker = New c051_BasicDecisionMaker(Me, StartTime)      '새 디시전메이커 시작
    End Sub

    '이동평균 데이타가 가능한지를 알려줌
    Public ReadOnly Property MAPossible() As Boolean
        Get
            'Dim index1 As Integer = _PriceList.Count
            'Dim index2 As Integer = _AmountList.Count
            'Dim index3 As Integer = _GangdoList.Count
            'Dim index4 As Integer = _BuyAmountList.Count
            'Dim index5 As Integer = _SelAmountlist.Count
            Dim index As Integer = RecordList.Count

            'If index1 = index2 AndAlso index1 = index3 AndAlso index1 = index4 AndAlso index1 = index5 Then
            If index >= MA_Base Then
                '평균을 위한 데이터 갯수가 충분함
                Return True
            Else
                '아직 데이터 갯수가 충분치 않음
                Return False
            End If
            'Else
            'Throw New Exception("데이터 갯수 차이 발생!")
            'Return False
            'End If
        End Get
    End Property

    '델타 거래량 데이타가 가능한지를 알려줌
    Public ReadOnly Property DeltaPossible() As Boolean
        Get
            If RecordList.Count >= 2 Then
                '데이타가 두개 이상이어야함
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    '이동평균가
    Public ReadOnly Property MAPrice(ByVal index As Integer) As UInt32
        Get
            If MAPossible Then
                Dim sum As UInt32 = 0
                For j_index As Integer = 0 To MA_Base - 1
                    sum += RecordList(index - j_index).Price
                Next
                Return sum / MA_Base
            Else
                Return 0
            End If
        End Get
    End Property

    Public Property StartTime() As DateTime
        Get
            Return _StartTime
        End Get
        Set(ByVal value As DateTime)
            _StartTime = value
            If StockDecisionMaker IsNot Nothing Then
                StockDecisionMaker.StartTime = _StartTime
            End If
        End Set
    End Property

#If 0 Then
    '델타매수량
    Public ReadOnly Property BuyDelta(ByVal index As Integer) As UInt64
        Get
            If DeltaPossible Then
                Dim diff As Int64 = CType(RecordList(index).BuyAmount, Int64) - CType(RecordList(index - 1).BuyAmount, Int64)
                If diff < 0 Then
                    '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
                    Return 0
                Else
                    '정상적인 델타매수량
                    Return diff
                End If
            Else
                Return 0
            End If
        End Get
    End Property

    '델타매도량
    Public ReadOnly Property SelDelta(ByVal index As Integer) As UInt64
        Get
            If DeltaPossible Then
                Dim diff As Int64 = CType(RecordList(index).SelAmount, Int64) - CType(RecordList(index - 1).SelAmount, Int64)
                If diff < 0 Then
                    '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
                    Return 0
                Else
                    '정상적인 델타매도량
                    Return diff
                End If
            Else
                Return 0
            End If
        End Get
    End Property
#End If

    '새 데이터 받아들임
    Public Sub SetNewData(ByVal price As UInt32, ByVal amount As UInt64, ByVal gangdo As Double)
        'check all indice of three variables are same
        'Dim index1 As Integer = _PriceList.Count
        'Dim index2 As Integer = _AmountList.Count
        'Dim index3 As Integer = _GangdoList.Count
        Dim buy_amount As UInt64

        'If index1 = index2 AndAlso index2 = index3 Then
        If RecordList.Count = 0 AndAlso StartTime = [DateTime].MinValue Then
            '최초 데이터 & StartTime이 안 정해져있을 때 => start time 기록
            StartTime = Now
        End If
        Dim a_record As SymbolRecord
        a_record.Price = price
        a_record.Amount = amount
        a_record.Gangdo = gangdo
        buy_amount = gangdo * amount / (100 + gangdo)
        a_record.BuyAmount = buy_amount
        If buy_amount > amount Then
            'float 계산이 잘못되었을 수 있다.
            a_record.SelAmount = 0
        Else
            a_record.SelAmount = amount - buy_amount
        End If
        a_record.MAPrice = MAPrice(RecordList.Count - 1)
        'a_record.BuyDelta = BuyDelta(RecordList.Count - 1)
        'a_record.SelDelta = SelDelta(RecordList.Count - 1)

        RecordList.Add(a_record)

        '리스트뷰 아이템 업데이트
        If LVItem IsNot Nothing Then
            LVItem.SubItems(LVSUB_NOW_PRICE).Text = price.ToString
            LVItem.SubItems(LVSUB_AMOUNT).Text = amount.ToString
            LVItem.SubItems(LVSUB_GANGDO).Text = gangdo.ToString
            LVItem.SubItems(LVSUB_DATA_COUNT).Text = RecordList.Count
            If MAPossible Then
                '이평 가능한 조건이면
                LVItem.SubItems(LVSUB_MOVING_AVERAGE_PRICE).Text = MAPrice(RecordList.Count - 1)
            End If
            'If DeltaPossible Then
            '델타 가능한 조건이면
            'LVItem.SubItems(LVSUB_BUY_DELTA).Text = BuyDelta(RecordList.Count - 1)
            'LVItem.SubItems(LVSUB_SEL_DELTA).Text = SelDelta(RecordList.Count - 1)
            'End If
        End If
        'Else
        '각 데이터들의 인덱스가 같아야 되는데 다르다 => do nothing
        'End If

        '디시전 메이커에 알려줌
        StockDecisionMaker.DataArrived(a_record)
        If StockDecisionMaker.IsDone Then
            '디시전 메이커 종료됨 => 디시전 리스트에 표시함
            Dim lv_done_decision_item As New ListViewItem(StockDecisionMaker.StartTime.TimeOfDay.ToString)    '시작시간
            lv_done_decision_item.SubItems.Add(Code)                                        '코드
            lv_done_decision_item.SubItems.Add(Name)                                        '종목명
            lv_done_decision_item.SubItems.Add(StockDecisionMaker.EnterPrice)               '진입가
            lv_done_decision_item.SubItems.Add(StockDecisionMaker.ExitPrice)                '청산가
            lv_done_decision_item.SubItems.Add(StockDecisionMaker.Profit)                   '수익률
            lv_done_decision_item.SubItems.Add(StockDecisionMaker.TookTime.ToString)        '걸린시간
            lv_done_decision_item.Tag = StockDecisionMaker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
            MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})

            '새 디시전 메이커 시작
            StockDecisionMaker = New c051_BasicDecisionMaker(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 15))
        End If
    End Sub

    '디시전 관련 리스트뷰 아이템 등록
    Private Sub RegisterDoneDecision(ByVal list_view_item As ListViewItem)
        MainForm.lv_DoneDecisions.Items.Add(list_view_item)
    End Sub

    Public Sub SaveToDB(ByVal db_connection As OleDb.OleDbConnection)
        Dim cmd As OleDb.OleDbCommand
        Dim result As Integer
        Dim insert_command As String
        Dim sample_time As DateTime
        Dim price As UInt32
        Dim amount As UInt64
        Dim gangdo As Double
        Dim error_count As Integer = 0

        If Not DBTableExist Then
            'Table이 존재하지 않음 => DB 생성
            'create table A00001 (SampledTime DateTime primary key, Price LONG, Amount DECIMAL, Gangdo FLOAT);
            Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price LONG, Amount DECIMAL, Gangdo DOUBLE);"
            cmd = New OleDb.OleDbCommand(table_create_command, db_connection)
            result = cmd.ExecuteNonQuery()             '명령 실행
            cmd.Dispose()
        End If

        For index As Integer = 0 To RecordList.Count - 1
            sample_time = StartTime + TimeSpan.FromSeconds(index * 15)
            price = RecordList(index).Price
            amount = RecordList(index).Amount
            gangdo = RecordList(index).Gangdo
            insert_command = "INSERT INTO " & Code & " (SampledTime, Price, Amount, Gangdo) VALUES ('" & sample_time.ToString & "', " & price.ToString & ", " & amount.ToString & ", " & gangdo.ToString & ");"
            cmd = New OleDb.OleDbCommand(insert_command, db_connection)
            Do
                Try
                    result = cmd.ExecuteNonQuery()              'insert 명령 실행
                    error_count = 0
                Catch ex As Exception
                    error_count += 1
                    MainForm.tb_Display.AppendText(Code & ": Error during saving" & error_count & vbCrLf)
                    Threading.Thread.Sleep(1000)
                    'error 시 db 닫았다 다시 연다. db가 커질 수록 이상한 에러가 많이 뜨는 것 같다.
                    db_connection.Close()
                    db_connection.Open()
                    cmd.Dispose()
                    cmd = New OleDb.OleDbCommand(insert_command, db_connection)
                End Try
            Loop While error_count > 0

            cmd.Dispose()
        Next
    End Sub
End Class
