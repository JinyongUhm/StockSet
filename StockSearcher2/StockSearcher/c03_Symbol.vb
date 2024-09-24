'Imports XA_DATASETLib

Public Class c03_Symbol
    Public Structure SymbolCoreRecord
        Dim Price As UInt32         '현재가
        Dim Amount As UInt64        '거래량
        Dim Gangdo As Single        '체결강도
        Dim MAPrice As UInt32       '현재가 이동평균
    End Structure

    Public Structure SymbolRecord
        Dim CoreRecord As SymbolCoreRecord
        'Dim Price As UInt32         '현재가
        'Dim Amount As UInt64        '거래량
        'Dim Gangdo As Single        '체결강도
        Dim BuyAmount As UInt64     '누적 매수거래량
        Dim SelAmount As UInt64     '누적 매도거래량
        'Dim MAPrice As UInt32       '현재가 이동평균
        'Dim BuyDelta As UInt64      '델타 매수거래량
        'Dim SelDelta As UInt64      '델타 매도거래량
    End Structure

    Public Structure BasePriceHistoryRecord
        Dim DateInteger As UInt32
        Dim BasePrice As UInt32
    End Structure

    Public Enum TypeRequestState
        RS_IDLE
        RS_CHECKINGPRICE_REQUESTED
        RS_REALTIMEPRICE_REQUESTED
        RS_REALTIMEPRICE_CONFIRMED
        RS_ERROR
    End Enum

    Public Delegate Sub DelegateRegisterDecision(ByVal list_view_item)
    'Public Delegate Sub DelegateListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String, ByVal text_4 As String)
    Public Delegate Sub DelegateListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String)

    'Public CodeIndex As Integer
    Public Code As String
    Public Name As String
    Public MarketKind As MARKET_KIND
    Public EvidanRate As Integer
    Public _StartTime As DateTime = [DateTime].MinValue
    'Public LVItem As ListViewItem
    Public DBTableExist As Boolean = False
    'Private _PriceList As New List(Of UInt32)
    'Private _AmountList As New List(Of UInt64)
    'Private _GangdoList As New List(Of Double)
    'Private _BuyAmountList As New List(Of UInt64)
    'Private _SelAmountlist As New List(Of UInt64)
    Public RecordList As New List(Of SymbolCoreRecord)
    Public MinuteCandleSeries As New List(Of CandleStructure)
    Public LosingCandle As CandleStructure
    Public AmountAtZero As UInt32 = 0
    Public NumberOfLastCandleUpdates As Integer
    '    Public MinuteIndexOf0 As Integer
    '    Public BaseFromPastCandles As Integer = 0
    Private _PrecalForAverage5Minutes As Long
    Private _PrecalForAverage30Minutes As Long
    Private _PrecalForAverage35Minutes As Long
    Private _PrecalForAverage70Minutes As Long
    Private _PrecalForAverage140Minutes As Long
    Private _PrecalForAverage280Minutes As Long
    Private _PrecalForAverage560Minutes As Long
    Private _PrecalForAverage1200Minutes As Long
    Private _PrecalForAverage2400Minutes As Long
    Private _PrecalForAverage4800Minutes As Long
    Private _PrecalForAverage9600Minutes As Long
    Public MadiffscaFor5M As Double = 0
    Public MadiffscaFor30M As Double = 0
    Public MadiffscaFor35M As Double = 0
    Public MadiffscaFor70M As Double = 0
    Public MadiffscaFor140M As Double = 0
    Public MadiffscaFor280M As Double = 0
    Public MadiffscaFor560M As Double = 0
    Public MadiffscaFor1200M As Double = 0
    Public MadiffscaFor2400M As Double = 0
    Public MadiffscaFor4800M As Double = 0
    Public MadiffscaFor9600M As Double = 0

    'Private _IsAverage5MinutesReady As Boolean
    'Private _IsAverage35MinutesReady As Boolean
    Private _MinPrecalForVariationRatio As UInt32 = [UInt32].MaxValue
    Private _MaxPrecalForVariationRatio As UInt32 = [UInt32].MinValue
    Private _MinAmountToday As UInt32 = [UInt32].MaxValue
    Private _MaxAmountToday As UInt32 = [UInt32].MinValue
    Private _SumAmountToday As UInt64 = 0
    Private _CandleCountToday As UInt32 = 0
    Public AmountAveToday As Single = -1
    Public StabilityList As New List(Of Single)
    Public Shared MAX_NUMBER_OF_STABILITY_LIST As Integer = 10
    Public Stability As Single = -1
    Public Newstab As Single = -1
    Public MA_MINUTE_COUNT As Integer = 35
    Public MADecisionFlag As Boolean = False

    'Public StockDecisionMaker As c050_DecisionMaker
    'Public StockOperator As et2_Operation
#If MOVING_AVERAGE_DIFFERENCE Then
    Public DecisionMakerCenter As New List(Of List(Of c05G_MovingAverageDifference))
#Else
#If PCRENEW_LEARN Then
    Public DecisionMakerCenter As New List(Of List(Of c05F_FlexiblePCRenew))
#ElseIf DOUBLE_FALL Then
    Public DecisionMakerCenter As New List(Of List(Of c05G_DoubleFall))
#ElseIf SHARP_LINES Then
    Public DecisionMakerCenter As New List(Of List(Of c05H_SharpLines))
#Else

#End If
#End If
    'Public StockDecisionMakerList As New List(Of c05E_PatternChecker)
    'Public StockDecisionMakerList_Copy1 As New List(Of c05E_PatternChecker_copy1)
    'Public StockDecisionMakerList_Copy2 As New List(Of c05E_PatternChecker_copy2)
    'Public StockDecisionMakerList_Copy3 As New List(Of c05E_PatternChecker_copy3)
    Public LowLimitPrice As Double
    'Public YesterPrice As UInt32
    Public ArgObjects(3) As Object
    'Private WithEvents _H1_ As XAReal
    'Private WithEvents _HA_ As XAReal
    'Private WithEvents _T1101 As New XAQuery
    'Public StockOperatorList As New List(Of et2_Operation)
    Private CodeForRequest As String
    Public RequestStatus As TypeRequestState = TypeRequestState.RS_IDLE
    'Private _LastCallPrices As CallPrices
    Private _PriceUpdateCount As UInt32
    'Public Price As UInt32
    Public PriceRealMonitoring As Integer
    '    Public PriceRealMonitoringKey As Integer                    '이키도 내버려두자. 최말단키로 critical zone에서 다른 critical zone 부르는 일 없다.
    Public CallPriceKey As TracingKey ' Integer                      '콜프라이스키는 내버려두자. 최말단키로 critical zone에서 다른 critical zone 부르는 일 없다.
    Private _IsCallPriceAvailable As Boolean
    'Public OneKey As Integer            '140422 : 한 종목 안에선 이키로 다 해결하자. 키가 너무 많아져 네스티드 구조 꼬일 염려가 있다.
    Public NoMoreDecisionMaker As Boolean = False
    Public CurrentAmount As UInt32
    Public AlreadyHooked As Boolean
    Public BasePriceHistory As New List(Of BasePriceHistoryRecord)
    Public OpenPrice As UInt32
    Public HighPrice As UInt32
    Public RecentGlobalTrend As Double  ', RecentGlobalDeviation 
    Public Event evNewMinuteCandleCreated()
    Public Shared MAX_NUMBER_OF_CANDLE As Integer = 9601
    Public DiscontinuousPoint As Integer = -1
    Public DailyAmountStore As New List(Of UInt64)
    Public NUMBER_OF_DAYS_FOR_DAILY_AMOUNT_VAR As Integer = 10
    'Public MAP_1 As Integer = 7
    Public OldDate As DateTime = [DateTime].MinValue
    Public AmountVar As Double = 0

    Public ReadOnly Property IsCallPriceAvailable As Boolean
        Get
            Dim is_call_price_available As Boolean
            'SafeEnter(CallPriceKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            is_call_price_available = _IsCallPriceAvailable
            'SafeLeave(CallPriceKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return is_call_price_available
        End Get
    End Property

    Public Sub New(ByVal symbol_code As String, ByVal symbol_name As String)
        'CodeIndex = code_index
        Code = symbol_code
        If Code.StartsWith("A") Then        '가격정보등 요청하는데 필요한 종목 code
            CodeForRequest = Code.Substring(1)
        Else
            CodeForRequest = Code
        End If

        Name = symbol_name

        AddHandler evNewMinuteCandleCreated, AddressOf TasksAfterNewCandleCreated

        'Create each decision maker list
        For index As Integer = 0 To NUMBER_OF_DECIDERS - 1
#If MOVING_AVERAGE_DIFFERENCE Then
            DecisionMakerCenter.Add(New List(Of c05G_MovingAverageDifference))
#Else
#If PCRENEW_LEARN Then
            DecisionMakerCenter.Add(New List(Of c05F_FlexiblePCRenew))
#ElseIf DOUBLE_FALL Then
            DecisionMakerCenter.Add(New List(Of c05G_DoubleFall))
#ElseIf SHARP_LINES Then
            DecisionMakerCenter.Add(New List(Of c05H_SharpLines))
#Else
#End If
#End If
        Next

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
        MinuteCandleSeries.Clear()
        AmountAtZero = 0
        DailyAmountStore.Clear()
        OldDate = [DateTime].MinValue
        AmountVar = 0

        MadiffscaFor5M = 0
        MadiffscaFor30M = 0
        MadiffscaFor35M = 0
        MadiffscaFor70M = 0
        MadiffscaFor140M = 0
        MadiffscaFor280M = 0
        MadiffscaFor560M = 0
        MadiffscaFor1200M = 0
        MadiffscaFor2400M = 0
        MadiffscaFor4800M = 0
        MadiffscaFor9600M = 0
        MADecisionFlag = 0
        'Public RecordList As New List(Of SymbolCoreRecord)

        AmountAtZero = 0
        NumberOfLastCandleUpdates = 0
        _PrecalForAverage5Minutes = 0
        _PrecalForAverage30Minutes = 0
        _PrecalForAverage35Minutes = 0
        _PrecalForAverage70Minutes = 0
        _PrecalForAverage140Minutes = 0     '2022.06.07 : 대박 버그 잡았다.
        _PrecalForAverage280Minutes = 0
        _PrecalForAverage560Minutes = 0
        _PrecalForAverage1200Minutes = 0
        _PrecalForAverage2400Minutes = 0
        _PrecalForAverage4800Minutes = 0
        _PrecalForAverage9600Minutes = 0


        _MinPrecalForVariationRatio = [UInt32].MaxValue
        _MaxPrecalForVariationRatio = [UInt32].MinValue
        _MinAmountToday = [UInt32].MaxValue
        _MaxAmountToday = [UInt32].MinValue
        _SumAmountToday = 0
        _CandleCountToday = 0
        AmountAveToday = -1
        StabilityList.Clear()
        Stability = -1
        Newstab = -1
        MADecisionFlag = False

        LowLimitPrice = 0
        RequestStatus = TypeRequestState.RS_IDLE
        _PriceUpdateCount = 0
        PriceRealMonitoring = 0
        _IsCallPriceAvailable = 0
        NoMoreDecisionMaker = False
        CurrentAmount = 0
        AlreadyHooked = False
        BasePriceHistory.Clear()
        OpenPrice = 0
        HighPrice = 0
        RecentGlobalTrend = 0
        DiscontinuousPoint = -1
        DailyAmountStore.Clear()
        OldDate = [DateTime].MinValue
        AmountVar = 0
        'If StockDecisionMaker IsNot Nothing Then
        'StockDecisionMaker.Clear()
        'End If
#If MOVING_AVERAGE_DIFFERENCE Then
        Dim decision_maker_list As List(Of c05G_MovingAverageDifference)
#Else
#If PCRENEW_LEARN Then
        Dim decision_maker_list As List(Of c05F_FlexiblePCRenew)
#ElseIf DOUBLE_FALL Then
        Dim decision_maker_list As List(Of c05G_DoubleFall)
#ElseIf SHARP_LINES Then
        Dim decision_maker_list As List(Of c05H_SharpLines)
#Else
#End If
#End If
        Dim monitoring_decision_maker_exist As Boolean
        For index_decision_center As Integer = 0 To NUMBER_OF_DECIDERS - 1
            decision_maker_list = DecisionMakerCenter(index_decision_center)

            For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
                decision_maker_list(index).ClearNow(0)
            Next
            decision_maker_list.Clear()      '리스트를 아예 클리어
            'StockDecisionMakerList.Add(New c057c_DeltaCurveCollective_DecisionMaker(Me, StartTime))      '새 디시전메이커 시작

            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            monitoring_decision_maker_exist = False
            For index As Integer = 0 To decision_maker_list.Count - 1
                If Not decision_maker_list(index).IsDone Then
                    monitoring_decision_maker_exist = True
                    Exit For
                End If
            Next
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
#If MOVING_AVERAGE_DIFFERENCE Then
                decision_maker_list.Add(New c05G_MovingAverageDifference(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), index_decision_center))
#Else
#If PCRENEW_LEARN Then
                decision_maker_list.Add(New c05F_FlexiblePCRenew(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), index_decision_center))
#ElseIf DOUBLE_FALL Then
                decision_maker_list.Add(New c05G_DoubleFall(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), index_decision_center))
#ElseIf SHARP_LINES Then
                decision_maker_list.Add(New c05H_SharpLines(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), 0))
#Else
#End If
#End If
            End If

            If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                Exit For
            End If
        Next

        '150209: 나머지 StockDecisionMakerList에 대해서도 카피본 완성하자.
    End Sub

#If 0 Then
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
#End If

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

#If 0 Then
    '이동평균가
    Public ReadOnly Property MAPrice(ByVal index As Integer, ByVal new_price As UInt32) As UInt32
        Get
            If MAPossible Then
                Dim sum As UInt32 = 0
                Dim local_ma_base As Integer
                If new_price = 0 Then
                    local_ma_base = MA_BASE
                Else
                    sum = new_price
                    local_ma_base = MA_BASE - 1
                End If
                For j_index As Integer = 0 To local_ma_base - 1
                    sum += RecordList(index - j_index).Price
                Next
                Return sum / MA_BASE
            Else
                Return 0
            End If
        End Get
    End Property
#End If

    Public Property StartTime() As DateTime
        Get
            '초단위 반올림 로직
            If _StartTime.Second < 30 Then
                Return _StartTime.Date + TimeSpan.FromHours(_StartTime.Hour) + TimeSpan.FromMinutes(_StartTime.Minute)
            Else
                Return _StartTime.Date + TimeSpan.FromHours(_StartTime.Hour) + TimeSpan.FromMinutes(_StartTime.Minute + 1)
            End If
            Return _StartTime
        End Get
        Set(ByVal value As DateTime)
            _StartTime = value
#If MOVING_AVERAGE_DIFFERENCE Then
            Dim decision_maker_list As List(Of c05G_MovingAverageDifference)
#Else
#If PCRENEW_LEARN Then
            Dim decision_maker_list As List(Of c05F_FlexiblePCRenew)
#ElseIf DOUBLE_FALL Then
            Dim decision_maker_list As List(Of c05G_DoubleFall)
#ElseIf SHARP_LINES Then
            Dim decision_maker_list As List(Of c05H_SharpLines)
#End If
#End If
            For index_decision_center As Integer = 0 To NUMBER_OF_DECIDERS - 1
                decision_maker_list = DecisionMakerCenter(index_decision_center)
                For index As Integer = 0 To decision_maker_list.Count - 1
                    decision_maker_list(index).StartTime = _StartTime
                Next
                If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                    Exit For
                End If
            Next
        End Set
    End Property

#If 0 Then
    Public ReadOnly Property LastCallPrices As CallPrices
        Get
            Dim local_call_prices As CallPrices

            SafeEnterTrace(CallPriceKey, 40)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            local_call_prices = _LastCallPrices
            SafeLeaveTrace(CallPriceKey, 41)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return local_call_prices
        End Get
    End Property

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

    Public Sub RequestPriceinfo()
        '아래는 테스트할 환경이 만들어질때까지는 열지 않는 것이 좋겠다.
        '131203 : T1101 request하고 실시간 가격 request하는 것까지 진행해보자. 단지 Operation 객체에서 복사해오면 된다.
        SafeEnterTrace(CallPriceKey, 60)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
#If 1 Then
        PriceRealMonitoring = PriceRealMonitoring + 1
        If PriceRealMonitoring = 1 Then
            '가격조회TR 초기화
            _T1101.ResFileName = "C:\ETRADE\XingAPI\Res\t1101.res"
            _T1101.SetFieldData("t1101InBlock", "shcode", 0, CodeForRequest)

            ' 데이터 요청
            If _T1101.Request(False) = False Then
                RequestStatus = TypeRequestState.RS_ERROR
                'ErrorLogging("A" & CodeForRequest & " :" & "대기시간 종료 후 호가요청 Fail")
            Else
                RequestStatus = TypeRequestState.RS_CHECKINGPRICE_REQUESTED
                'MessageLogging("A" & CodeForRequest & " :" & "첫번째 호가요청 성공")
            End If
        End If
#End If
        SafeLeave(CallPriceKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub StopPriceinfo()
        '140128 : 여러개의 decision maker를 운영하는 것이 쉬운지 어려운지 검토해보자. 어렵지 않다면 PriceRealMonitoring을 integer로 해서 자원관리하는 쪽으로 해보자. (가격정보 구독받는 decision maker마다 increase하는 방식.)
        '140129 : decision maker가 소멸될 때 이 것을 콜하도록 만들어보자.
        SafeEnterTrace(CallPriceKey, 70)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        PriceRealMonitoring = PriceRealMonitoring - 1
        'MessageLogging("A" & CodeForRequest & " :" & "호가 stop 요청 " & PriceRealMonitoring.ToString & "번째")
        If PriceRealMonitoring <= 0 Then
            If _H1_ IsNot Nothing Then
                _H1_.UnAdviseRealData()
                '_H1_ = Nothing
            End If
            If _HA_ IsNot Nothing Then
                _HA_.UnAdviseRealData()
                '_HA_ = Nothing
            End If
            _IsCallPriceAvailable = False
        End If
        SafeLeaveTrace(CallPriceKey, 71)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Private Sub _T1101_ReceiveData(ByVal szTrCode As String) Handles _T1101.ReceiveData
        SafeEnterTrace(CallPriceKey, 80)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        _IsCallPriceAvailable = True
        If RequestStatus = TypeRequestState.RS_CHECKINGPRICE_REQUESTED Then
            Dim temp_data As String
            temp_data = _T1101.GetFieldData("t1101OutBlock", "offerho1", 0)
            If temp_data = "" Then
                temp_data = _T1101.GetFieldData("t1101OutBlock", "bidho1", 0)
                temp_data = temp_data & " " & _T1101.GetFieldData("t1101OutBlock", "dnlmtprice", 0)             '하한가 추출
                'ErrorLogging("A" & CodeForRequest & " :" & "T1101 공백반환 " & temp_data)

                '실시간 호가 초기화 invoke
                If MarketKind = MARKET_KIND.MK_KOSPI Then
                    If _H1_ Is Nothing Then
                        _H1_ = New XAReal
                        _H1_.ResFileName = "C:\ETRADE\XingAPI\Res\S2_.res"
                        _H1_.SetFieldData("InBlock", "shcode", CodeForRequest)
                    End If
                    _H1_.AdviseRealData()           '데이터 요청
                Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
                    If _HA_ Is Nothing Then
                        _HA_ = New XAReal
                        _HA_.ResFileName = "C:\ETRADE\XingAPI\Res\KS_.res"
                        _HA_.SetFieldData("InBlock", "shcode", CodeForRequest)
                    End If
                    _HA_.AdviseRealData()           '데이터 요청
                End If

                'MessageLogging("A" & CodeForRequest & " :" & "실시간 호가요청되었음")
                SafeLeaveTrace(CallPriceKey, 81)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Return
            End If
            _LastCallPrices.SelPrice1 = Convert.ToUInt32(temp_data)
            '            _LastCallPrices.SelPrice2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho2", 0))
            '            _LastCallPrices.SelPrice3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho3", 0))
            '            _LastCallPrices.SelPrice4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho4", 0))
            '            _LastCallPrices.SelPrice5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho5", 0))
            '            _LastCallPrices.SelAmount1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem1", 0))
            '            _LastCallPrices.SelAmount2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem2", 0))
            '            _LastCallPrices.SelAmount3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem3", 0))
            '            _LastCallPrices.SelAmount4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem4", 0))
            '            _LastCallPrices.SelAmount5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem5", 0))
            temp_data = _T1101.GetFieldData("t1101OutBlock", "bidho1", 0)
            _LastCallPrices.BuyPrice1 = Convert.ToUInt32(temp_data)
            '            _LastCallPrices.BuyPrice2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho2", 0))
            '            _LastCallPrices.BuyPrice3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho3", 0))
            '            _LastCallPrices.BuyPrice4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho4", 0))
            '            _LastCallPrices.BuyPrice5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho5", 0))
            '            _LastCallPrices.BuyAmount1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem1", 0))
            '            _LastCallPrices.BuyAmount2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem2", 0))
            '            _LastCallPrices.BuyAmount3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem3", 0))
            '            _LastCallPrices.BuyAmount4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem4", 0))
            '            _LastCallPrices.BuyAmount5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem5", 0))
            '
            LowLimitPrice = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "dnlmtprice", 0))             '하한가 추출
            Price = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "price", 0))                          '현재가 추출
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice5.ToString & "   " & _LastCallPrices.SelAmount5.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice4.ToString & "   " & _LastCallPrices.SelAmount4.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice3.ToString & "   " & _LastCallPrices.SelAmount3.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice2.ToString) ' & "   " & _LastCallPrices.SelAmount2.ToString)
            'MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice1.ToString) ' & "   " & _LastCallPrices.SelAmount1.ToString)
            'MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.BuyPrice1.ToString) ' & "   " & _LastCallPrices.BuyAmount1.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.BuyPrice2.ToString) ' & "   " & _LastCallPrices.BuyAmount2.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.BuyPrice3.ToString & "   " & _LastCallPrices.BuyAmount3.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.BuyPrice4.ToString & "   " & _LastCallPrices.BuyAmount4.ToString)
            '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.BuyPrice5.ToString & "   " & _LastCallPrices.BuyAmount5.ToString)

            '140204 : 이제 여기서 구독 요청한 decision maker를 불러서 주문하는 절차를 밟아야 한다.
            '140204 : 이미 실시간 구독까지 하고 있는 상황에서 decision maker가 pre thresholded 상태로 접어들었을 때는 바로 주문하는 절차가 필요할 것이다
            '140205 : 지금 혼동하고 있는데, 주문하는 것은 1초타이머 종료시 pre-thresholder들 list up한 후에 하는 것이다.

            '실시간 호가 초기화 invoke
            If MarketKind = MARKET_KIND.MK_KOSPI Then
                If _H1_ Is Nothing Then
                    _H1_ = New XAReal
                    _H1_.ResFileName = "C:\ETRADE\XingAPI\Res\S2_.res"
                    _H1_.SetFieldData("InBlock", "shcode", CodeForRequest)
                End If
                _H1_.AdviseRealData()           '데이터 요청
            Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
                If _HA_ Is Nothing Then
                    _HA_ = New XAReal
                    _HA_.ResFileName = "C:\ETRADE\XingAPI\Res\KS_.res"
                    _HA_.SetFieldData("InBlock", "shcode", CodeForRequest)
                End If
                _HA_.AdviseRealData()           '데이터 요청
            End If

            'MessageLogging("A" & CodeForRequest & " :" & "실시간 호가요청되었음")
        End If
        SafeLeaveTrace(CallPriceKey, 82)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '호가 받음
    Private Sub _HX_ReceiveData(ByVal szTrCode As String) Handles _H1_.ReceiveRealData, _HA_.ReceiveRealData
        SafeEnterTrace(CallPriceKey, 90)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If MarketKind = MARKET_KIND.MK_KOSPI Then
            _PriceUpdateCount = _PriceUpdateCount + 1
            _LastCallPrices.SelPrice1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho"))
            '            _LastCallPrices.SelPrice2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho2"))
            '            _LastCallPrices.SelPrice3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho3"))
            '            _LastCallPrices.SelPrice4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho4"))
            '            _LastCallPrices.SelPrice5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho5"))
            '            _LastCallPrices.SelPrice6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho6"))
            '            _LastCallPrices.SelPrice7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho7"))
            '            _LastCallPrices.SelPrice8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho8"))
            '            _LastCallPrices.SelPrice9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho9"))
            '            _LastCallPrices.SelPrice10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho10"))
            '            _LastCallPrices.SelAmount1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem1"))
            '            _LastCallPrices.SelAmount2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem2"))
            '            _LastCallPrices.SelAmount3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem3"))
            '            _LastCallPrices.SelAmount4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem4"))
            '            _LastCallPrices.SelAmount5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem5"))
            '            _LastCallPrices.SelAmount6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem6"))
            '            _LastCallPrices.SelAmount7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem7"))
            '            _LastCallPrices.SelAmount8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem8"))
            '            _LastCallPrices.SelAmount9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem9"))
            '            _LastCallPrices.SelAmount10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem10"))
            _LastCallPrices.BuyPrice1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho"))
            '            _LastCallPrices.BuyPrice2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho2"))
            '            _LastCallPrices.BuyPrice3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho3"))
            '            _LastCallPrices.BuyPrice4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho4"))
            '            _LastCallPrices.BuyPrice5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho5"))
            '            _LastCallPrices.BuyPrice6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho6"))
            '            _LastCallPrices.BuyPrice7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho7"))
            '            _LastCallPrices.BuyPrice8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho8"))
            '            _LastCallPrices.BuyPrice9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho9"))
            '            _LastCallPrices.BuyPrice10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho10"))
            '            _LastCallPrices.BuyAmount1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem1"))
            '            _LastCallPrices.BuyAmount2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem2"))
            '            _LastCallPrices.BuyAmount3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem3"))
            '            _LastCallPrices.BuyAmount4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem4"))
            '            _LastCallPrices.BuyAmount5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem5"))
            '            _LastCallPrices.BuyAmount6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem6"))
            '            _LastCallPrices.BuyAmount7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem7"))
            '            _LastCallPrices.BuyAmount8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem8"))
            '            _LastCallPrices.BuyAmount9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem9"))
            '            _LastCallPrices.BuyAmount10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem10"))
        Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
            _PriceUpdateCount = _PriceUpdateCount + 1
            _LastCallPrices.SelPrice1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho"))
            '            _LastCallPrices.SelPrice2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho2"))
            '            _LastCallPrices.SelPrice3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho3"))
            '            _LastCallPrices.SelPrice4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho4"))
            '            _LastCallPrices.SelPrice5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho5"))
            '            _LastCallPrices.SelPrice6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho6"))
            '            _LastCallPrices.SelPrice7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho7"))
            '            _LastCallPrices.SelPrice8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho8"))
            '            _LastCallPrices.SelPrice9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho9"))
            '            _LastCallPrices.SelPrice10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho10"))
            '            _LastCallPrices.SelAmount1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem1"))
            '            _LastCallPrices.SelAmount2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem2"))
            '            _LastCallPrices.SelAmount3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem3"))
            '            _LastCallPrices.SelAmount4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem4"))
            '            _LastCallPrices.SelAmount5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem5"))
            '            _LastCallPrices.SelAmount6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem6"))
            '            _LastCallPrices.SelAmount7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem7"))
            '            _LastCallPrices.SelAmount8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem8"))
            '            _LastCallPrices.SelAmount9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem9"))
            '            _LastCallPrices.SelAmount10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem10"))
            _LastCallPrices.BuyPrice1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho"))
            '            _LastCallPrices.BuyPrice2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho2"))
            '            _LastCallPrices.BuyPrice3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho3"))
            '            _LastCallPrices.BuyPrice4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho4"))
            '            _LastCallPrices.BuyPrice5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho5"))
            '            _LastCallPrices.BuyPrice6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho6"))
            '            _LastCallPrices.BuyPrice7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho7"))
            '            _LastCallPrices.BuyPrice8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho8"))
            '            _LastCallPrices.BuyPrice9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho9"))
            '            _LastCallPrices.BuyPrice10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho10"))
            '            _LastCallPrices.BuyAmount1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem1"))
            '            _LastCallPrices.BuyAmount2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem2"))
            '            _LastCallPrices.BuyAmount3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem3"))
            '            _LastCallPrices.BuyAmount4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem4"))
            '            _LastCallPrices.BuyAmount5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem5"))
            '            _LastCallPrices.BuyAmount6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem6"))
            '            _LastCallPrices.BuyAmount7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem7"))
            '            _LastCallPrices.BuyAmount8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem8"))
            '            _LastCallPrices.BuyAmount9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem9"))
            '            _LastCallPrices.BuyAmount10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem10"))
        End If
        SafeLeaveTrace(CallPriceKey, 91)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        For index As Integer = 0 To StockDecisionMakerList.Count - 1
            StockDecisionMakerList(index).CallPriceUpdated()
        Next

    End Sub
#End If
#If 0 Then      'simulation 에서는 안 쓰인다.
    '바로 이전 데이터를 새 데이터인 것처럼 보낸다.
    Public Sub SetLimpHomeData()
        SetNewData(RecordList.Last.Price, RecordList.Last.Amount)
    End Sub
#End If

    '새 데이터 받아들임
    'Public Sub SetNewData(ByVal price As UInt32, ByVal amount As UInt64, ByVal gangdo As Double, ByVal global_trend As Double) ', ByVal global_deviation As Double
    Public Sub SetNewData(ByVal price As UInt32, ByVal amount As UInt64, ByVal global_trend As Double) ', ByVal global_deviation As Double)
        'RecentGlobalDeviation = global_deviation
        'check all indice of three variables are same
        Dim buy_amount As UInt64

        'If index1 = index2 AndAlso index2 = index3 Then
        If RecordList.Count = 0 AndAlso StartTime = [DateTime].MinValue Then
            '최초 데이터 & StartTime이 안 정해져있을 때 => start time 기록
            StartTime = Now
            MsgBox("여기 들어오면 안 되는 곳")
        End If

        Dim a_record As SymbolRecord
        If price <> 0 Or RecordList.Count = 0 Then
            CurrentAmount = amount
            a_record.CoreRecord.Price = price
            a_record.CoreRecord.Amount = amount
        Else
            CurrentAmount = RecordList.Last.Amount
            a_record.CoreRecord.Price = RecordList.Last.Price
            a_record.CoreRecord.Amount = RecordList.Last.Amount
        End If
        If GangdoDB Then
#If 0 Then
            a_record.CoreRecord.Gangdo = gangdo
            If gangdo >= 0 Then
                buy_amount = gangdo * amount / (100 + gangdo)
                a_record.BuyAmount = buy_amount
                If buy_amount > amount Then
                    'float 계산이 잘못되었을 수 있다.
                    a_record.SelAmount = 0
                Else
                    a_record.SelAmount = amount - buy_amount
                End If
            End If
#End If
        End If

        '2020.11.21: 최고가 업데이트하기
        If price > HighPrice Then
            HighPrice = price
        End If
        'a_record.MAPrice = MAPrice(RecordList.Count - 1, a_record.Price)
        'a_record.Gangdo = gangdo

        RecordList.Add(a_record.CoreRecord)
#If MOVING_AVERAGE_DIFFERENCE Then
        Dim candle_update_complited As Boolean = MinuteCandleNewData(a_record)

        If Not candle_update_complited Then
            '마지막 candle 이 완성되기 전에는 아래로 내려가지 않기로 하자.
            Return
        End If
#End If
#If 0 Then
        '리스트뷰 아이템 업데이트
        Dim str_MAPrice As String = ""
        If MAPossible Then
            '이평 가능한 조건이면
            str_MAPrice = MAPrice(RecordList.Count - 1, 0).ToString
        End If
        Dim delegate_list_view_update As New DelegateListViewUpdate(AddressOf ListViewUpdate)
        ArgObjects(0) = price.ToString
        ArgObjects(1) = amount.ToString
        'ArgObjects(2) = gangdo.ToString
        ArgObjects(2) = RecordList.Count.ToString
        ArgObjects(3) = str_MAPrice.ToString

        MainForm.BeginInvoke(delegate_list_view_update, ArgObjects)
#End If
        'SafeEnterTrace(OneKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
#If MOVING_AVERAGE_DIFFERENCE Then
        Dim decision_maker_list As List(Of c05G_MovingAverageDifference)
#Else
#If PCRENEW_LEARN Then
        Dim decision_maker_list As List(Of c05F_FlexiblePCRenew)
#ElseIf DOUBLE_FALL Then
        Dim decision_maker_list As List(Of c05G_DoubleFall)
#ElseIf SHARP_LINES Then
        Dim decision_maker_list As List(Of c05H_SharpLines)
#End If
#End If
        For index_decision_center As Integer = NUMBER_OF_DECIDERS - 1 To 0 Step -1
            decision_maker_list = DecisionMakerCenter(index_decision_center)
            '디시전 메이커에 알려줌
            For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
                decision_maker_list(index).DataArrived(a_record)
                '160519: 이런식으로 decision maker와 비슷한 방식으로 하면 될 것 같다. pattern 확인되면 화면에 보여주고나서 다음 객체 생성하고...
                If decision_maker_list(index).IsDone Then
                    'If StockDecisionMakerList(index).StockOperator Is Nothing OrElse StockDecisionMakerList(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                    '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
#If MOVING_AVERAGE_DIFFERENCE Then
                    '일반용 (하지만 지금은 쓰지 않지) or MA 용
#If Not NO_SHOW_TO_THE_FORM Then
                    decision_maker_list(index).CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
                    Dim lv_done_decision_item As New ListViewItem(decision_maker_list(index).StartTime.Date.ToString("d"))    '날짜 
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).StartTime.TimeOfDay.ToString)     '시작시간
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterTime.TimeOfDay.ToString)     '진입시간
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).ExitTime.TimeOfDay.ToString)      '청산시간 
                    lv_done_decision_item.SubItems.Add(Code)                                                '코드 
                    lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                    'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).GetType.ToString.Substring(StockDecisionMakerList(index).GetType.ToString.Length - 1, 1))     '이거 뭐였지? 아마도 multi했을 때 몇 번째 꺼인가 표시?
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterPrice)                       '진입가
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).ExitPrice)                        '청산가
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).Profit.ToString("p"))                           '수익률
                    If decision_maker_list(index).FallVolume = 0 Then
                        lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                    Else
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).FallVolume.ToString("##,#"))      '하강볼륨
                    End If
                    If GangdoDB Then
                        'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).BuyAmountCenter)               'BuyAmountCenter
                        'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).SelAmountCenter)               'SelAmountCenter
                        'lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterDeltaGangdo)               'DeltaGangdo
                    End If
                    lv_done_decision_item.Tag = decision_maker_list(index)                      '리스트뷰 아이템 태그에 디시전 객체 걸기
#If NO_SHOW_TO_THE_FORM Then
                    MainForm.HitList.Add(lv_done_decision_item)
#Else
                    MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
#End If
#ElseIf PCRENEW_LEARN Or DOUBLE_FALL Or SHARP_LINES Then
                    '패턴용 or 선분용
#If CHECK_PRE_PATTERN_STRATEGY Then
                        If decision_maker_list(index).PostPatternOk Then
#End If
#If Not NO_SHOW_TO_THE_FORM Then
                    decision_maker_list(index).CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
                    Dim lv_done_decision_item As New ListViewItem(decision_maker_list(index).StartTime.Date.ToString("d"))    '날짜
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).StartTime.TimeOfDay.ToString)     '시작시간
                    'lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterTime.AddSeconds(decision_maker_list(index).Pattern.Length * 5).TimeOfDay.ToString)     '진입시간. 190403:잘못된 계산이었다. 왜 이런 생각을 했는지 모르겠다
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterTime.TimeOfDay.ToString)     '진입시간
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).ExitTime.TimeOfDay.ToString)      '청산시간
                    lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Code)                                                '코드
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterPrice)                       '진입가
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).ExitPrice)                        '청산가
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).Profit)                        '수익률
                    If decision_maker_list(index).FallVolume = 0 Then
                        lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                    Else
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).FallVolume.ToString("##,#"))      '하강볼륨
                    End If
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).ScoreSave.ToString())                  '스코어
                    'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).EnteringGangdoChange.ToString())                  '스코어
#If 0 Then
                    If GangdoDB Then
                        'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).BuyAmountCenter)               'BuyAmountCenter
                        'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).SelAmountCenter)               'SelAmountCenter
                        lv_done_decision_item.SubItems.Add(0) 'decision_maker_list(index).PatternGangdo)               'PatternGangdo
                    End If
#End If
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).NumberOfEntering)               'PatternGangdo
                    For sub_index As Integer = 0 To decision_maker_list(index).PriceRateTrend.Count - 1
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).PriceRateTrend(sub_index).ToString("p"))  '트렌드기록
                    Next
                    '150722 : 위에 마지막 칼럼에 StartTime.Date로 찾은 어제 종가대비 하강 start값을 percentage로 나타내서 보여주면 될것 같다.
                    lv_done_decision_item.Tag = decision_maker_list(index)                      '리스트뷰 아이템 태그에 디시전 객체 걸기
#If NO_SHOW_TO_THE_FORM Then
                    MainForm.HitList.Add(lv_done_decision_item)
#Else
                    MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
#End If

#If CHECK_PRE_PATTERN_STRATEGY Then
                    Else
                        'PostPattern NOK
                        '카운트 안 한다.
                        CountPostPatternFail = CountPostPatternFail + 1
                    End If
#End If

#End If
                    '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                    '130625: 아래 실제 수익률을 수정할 때가 왔다.
#If 0 Then
                If StockDecisionMakerList(index).StockOperator IsNot Nothing Then
                    '실제 수익률은 stock operator가 있을 때만 행한다.
                    Dim real_profit As Double
                    If StockDecisionMakerList(index).StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                        real_profit = 0
                    ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                        real_profit = ((1 - TAX - FEE) * StockDecisionMakerList(index).StockOperator.SelDealPrice - (1 + FEE) * StockDecisionMakerList(index).StockOperator.BuyDealPrice) / StockDecisionMakerList(index).StockOperator.BuyDealPrice        '수익률
                    Else
                        real_profit = ((1 - TAX - FEE) * StockDecisionMakerList(index).StockOperator.SelDealPrice - (1 + FEE) * StockDecisionMakerList(index).StockOperator.BuyDealPrice) / StockDecisionMakerList(index).StockOperator.BuyDealPrice        '수익률
                    End If

                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                Else
                    lv_done_decision_item.SubItems.Add("")
                End If
#Else

                    'Dim real_profit As Double
                    'real_profit = 0 ' ((1 - TAX - FEE) * StockDecisionMakerList(index).ExitPrice - (1 + FEE) * StockDecisionMakerList(index).EnterPrice) / StockDecisionMakerList(index).StockOperator.BuyDealPrice        '수익률
                    'lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                    'lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))   'make subitem to meet the column number
#End If
                    'Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                    'Dim sudden_rate As Double = StockDecisionMakerList(index).FallVolume / average_volume_per_15s
#If 0 Then
#If Not MOVING_AVERAGE_DIFFERENCE Then
                    If Not DecisionByPattern Then
                        '일반용
#If 0 Then      '패턴일 때 빌드에러나서 닫아놨는데 일반용이면 열어라 (예스터)
                        Dim yester_end_price As UInt32 = GetYesterPrice()
                        If yester_end_price = 0 Then
                            lv_done_decision_item.SubItems.Add("no_data")
                        Else
                            Dim start_price_yester_rate As Double = StockDecisionMakerList(index)._FallingStartPrice / yester_end_price - 1
                            lv_done_decision_item.SubItems.Add(start_price_yester_rate.ToString("p"))
                        End If
#End If
                    Else
                        '패턴용		
                    End If
#End If
#End If

                    'StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                    '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                    'StockDecisionMakerList(index).StockOperator = Nothing         'stock operator 초기화
                    decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                    'Else
                    'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                    'End If
                ElseIf decision_maker_list(index)._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE Then
                    '180109: 원래 이 조건은 없었으나, WAIT_EXIT 상태에서 취소된 경우를 구분하기 위해 만들게 되었다..
                    decision_maker_list.RemoveAt(index)          '아무 동작 안 하고 decision maker를 삭제한다.
                End If
            Next
            '140206 : 일단 이 함수만 decision maker list 위주로 훑어봤는데 특별한 게 없네.. 공유신경쓸게 생각보다 별로 없다.
            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            Dim monitoring_decision_maker_exist As Boolean = False

            For index As Integer = 0 To decision_maker_list.Count - 1
#If MOVING_AVERAGE_DIFFERENCE Then
                'MovingAverageDifference 인 경우
                monitoring_decision_maker_exist = True       '무조건 하나라도 있으면 더 이상 생성되면 안 된다.
#Else
                '그 이외의 경우
                If decision_maker_list(index).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING OrElse decision_maker_list(index).CurrentPhase = c050_DecisionMaker.SearchPhase.UNWANTED_RISING_DETECTED OrElse
                    decision_maker_list(index).CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse decision_maker_list(index).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_SECONDFALL OrElse
                                        (decision_maker_list(index).CurrentPhase = c050_DecisionMaker.SearchPhase.DONE And Not decision_maker_list(index).IsDone) Then
                    '위의 윗줄은 pre threshold 추가되면서 추가되었고, 위의 마지막 조건은 2시 48분 이후로 Decision maker 객체가 계속 증가하는 것을 방지하기 위해 추가되었다.
                    monitoring_decision_maker_exist = True
                    Exit For
                End If
#End If
            Next
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
#If MOVING_AVERAGE_DIFFERENCE Then
                'Moving Average Difference 전략
                decision_maker_list.Add(New c05G_MovingAverageDifference(Me, StartTime, index_decision_center))
#Else
                '그 이외의 전략
#If PCRENEW_LEARN Then
                decision_maker_list.Add(New c05F_FlexiblePCRenew(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), index_decision_center))
#ElseIf DOUBLE_FALL Then
                decision_maker_list.Add(New c05G_DoubleFall(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), index_decision_center))
#ElseIf SHARP_LINES Then
                decision_maker_list.Add(New c05H_SharpLines(Me, StartTime + TimeSpan.FromSeconds(RecordList.Count * 5), 0))
#End If
#End If
            End If
        Next
        MADecisionFlag = False   '2020.09.14: 한 iteration 에서 진입하는 decision maker를 1개로 제한하기 위한 flag다. 매 iteration 마다 초기화한다.
        AlreadyHooked = False   '이건 매 iteration 그 때 마다 초기화된다.
        RecentGlobalTrend = global_trend
        'SafeLeaveTrace(OneKey, 44)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

#If MOVING_AVERAGE_DIFFERENCE Then
    Public Sub SetNewCandle(ByVal candle_time As DateTime, ByVal open_price As UInt32, ByVal close_price As UInt32, ByVal high_price As UInt32, ByVal low_price As UInt32, ByVal amount As UInt64)
        'If Code <> "A260660" Then
        'Exit Sub
        'End If
        If OldDate = [DateTime].MinValue Then
            'OldDate 초기화
            OldDate = candle_time.Date
        End If
        Dim new_candle As CandleStructure
        new_candle.CandleTime = candle_time
        new_candle.Open = open_price
        new_candle.Close = close_price
        new_candle.High = high_price
        new_candle.Low = low_price
        new_candle.Amount = amount
        If MinuteCandleSeries.Count = 0 Then
            new_candle.AccumAmount = amount
        Else
            new_candle.AccumAmount = MinuteCandleSeries.Last.AccumAmount + amount
        End If

#If 0 Then
        If MinuteCandleSeries.Count >= 4 Then
            'Variation5Minutes 업데이트
            Dim max_price, min_price As UInt32

            If new_candle.Amount > 0 Then
                min_price = new_candle.Low
                max_price = new_candle.High
            Else
                min_price = UInt32.MaxValue
                max_price = UInt32.MinValue
            End If
            For index As Integer = MinuteCandleSeries.Count - 4 To MinuteCandleSeries.Count - 1
                If MinuteCandleSeries(index).Amount > 0 Then
                    min_price = Math.Min(min_price, MinuteCandleSeries(index).Low)
                    max_price = Math.Max(max_price, MinuteCandleSeries(index).High)
                Else
                    'min_price 변경 없음
                End If
            Next
            If max_price = 0 Then
                new_candle.Variation5Minutes = 0
            Else
                new_candle.Variation5Minutes = max_price - min_price
            End If

            'Average5Minutes 업데이트
            new_candle.Average5Minutes = (new_candle.Close + MinuteCandleSeries(MinuteCandleSeries.Count - 4).Close + MinuteCandleSeries(MinuteCandleSeries.Count - 3).Close + MinuteCandleSeries(MinuteCandleSeries.Count - 2).Close + MinuteCandleSeries(MinuteCandleSeries.Count - 1).Close) / 5

            'VariationRatio 업데이트
            new_candle.VariationRatio = new_candle.Variation5Minutes / new_candle.Average5Minutes
        End If
#End If
        'discontinuous point 업데이트
        If MinuteCandleSeries.Count > 0 Then
            If open_price / MinuteCandleSeries.Last.Close < 0.7 OrElse open_price / MinuteCandleSeries.Last.Close > 1.3 Then
                DiscontinuousPoint = MinuteCandleSeries.Count
            End If
        End If

        new_candle.Average5Minutes = GetAverage5Minutes(new_candle.Close)
        new_candle.Average30Minutes = GetAverage30Minutes(new_candle.Close)
        new_candle.Average35Minutes = GetAverage35Minutes(new_candle.Close)
        new_candle.Average70Minutes = GetAverage70Minutes(new_candle.Close)
        new_candle.Average140Minutes = GetAverage140Minutes(new_candle.Close)
        new_candle.Average280Minutes = GetAverage280Minutes(new_candle.Close)
        new_candle.Average560Minutes = GetAverage560Minutes(new_candle.Close)
        new_candle.Average1200Minutes = GetAverage1200Minutes(new_candle.Close)
        new_candle.Average2400Minutes = GetAverage2400Minutes(new_candle.Close)
        new_candle.Average4800Minutes = GetAverage4800Minutes(new_candle.Close)
        new_candle.Average9600Minutes = GetAverage9600Minutes(new_candle.Close)
        new_candle.VariationRatio = GetVariationRatio(new_candle.Close)

        'DailyAmount 업데이트
        If new_candle.CandleTime.Date <> OldDate Then
            DailyAmountStore.Add(MinuteCandleSeries.Last.AccumAmount)
            OldDate = new_candle.CandleTime.Date
            If DailyAmountStore.Count > NUMBER_OF_DAYS_FOR_DAILY_AMOUNT_VAR Then
                DailyAmountStore.RemoveAt(0)
            End If
            'AmountVar 계산
            If DailyAmountStore.Count > 1 Then
                Dim average_amount As Double = DailyAmountStore.Last / DailyAmountStore.Count
                Dim sum As Double = (DailyAmountStore(0) - average_amount) ^ 2
                For index As Integer = 1 To DailyAmountStore.Count - 1
                    sum = sum + (DailyAmountStore(index) - DailyAmountStore(index - 1) - average_amount) ^ 2
                Next
                AmountVar = Math.Sqrt(sum) / average_amount / DailyAmountStore.Count
            End If
        End If

        MinuteCandleSeries.Add(new_candle)
        UpdateAverage5MinutesPrecal()
        UpdateAverage30MinutesPrecal()
        UpdateAverage35MinutesPrecal()
        UpdateAverage70MinutesPrecal()
        UpdateAverage140MinutesPrecal()
        UpdateAverage280MinutesPrecal()
        UpdateAverage560MinutesPrecal()
        UpdateAverage1200MinutesPrecal()
        UpdateAverage2400MinutesPrecal()
        UpdateAverage4800MinutesPrecal()
        UpdateAverage9600MinutesPrecal()
        UpdateVariationRatioPrecal()
        UpdateMinMaxAmountToday()
        If MinuteCandleSeries.Count > MAX_NUMBER_OF_CANDLE Then
            LosingCandle = MinuteCandleSeries(0)
            MinuteCandleSeries.RemoveAt(0)
            StartTime = MinuteCandleSeries(0).CandleTime
            If DiscontinuousPoint <> -1 Then
                DiscontinuousPoint -= 1
            End If
        End If

        RaiseEvent evNewMinuteCandleCreated()

        Dim decision_maker_list As List(Of c05G_MovingAverageDifference)
        For index_decision_center As Integer = NUMBER_OF_DECIDERS - 1 To 0 Step -1
            decision_maker_list = DecisionMakerCenter(index_decision_center)
            '디시전 메이커에 알려줌
            For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
                decision_maker_list(index).CandleArrived(new_candle)
                '160519: 이런식으로 decision maker와 비슷한 방식으로 하면 될 것 같다. pattern 확인되면 화면에 보여주고나서 다음 객체 생성하고...
                If decision_maker_list(index).IsDone Then
                    'If StockDecisionMakerList(index).StockOperator Is Nothing OrElse StockDecisionMakerList(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                    '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
                    If decision_maker_list(index).YieldForHighLevel Or decision_maker_list(index).FakeGullin Then
                        '2020.09.14: 양보하기로 약속했으니 등록은 하지 않고 다만 지우기만 해서 다음 걸린애를 찾는 여정을 계속하도록 한다.
                        '2022.07.24: 여기에 FakeGullin 조건이 추가되었다.
                        decision_maker_list.RemoveAt(index)
                    Else
#If Not NO_SHOW_TO_THE_FORM Then
                        decision_maker_list(index).CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
#If MOVING_AVERAGE_DIFFERENCE Then
                        Dim lv_done_decision_item As New ListViewItem(decision_maker_list(index).EnterTime.Date.ToString("d"))    '날짜
#Else
                    Dim lv_done_decision_item As New ListViewItem(decision_maker_list(index).StartTime.Date.ToString("d"))    '날짜
#End If

                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).StartTime.TimeOfDay.ToString)     '시작시간
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterTime.TimeOfDay.ToString)     '진입시간
#If MOVING_AVERAGE_DIFFERENCE Then
                        lv_done_decision_item.SubItems.Add(CType(decision_maker_list(index), c05G_MovingAverageDifference).MinutesPassed.ToString)      '보유 분 수
#Else
                    lv_done_decision_item.SubItems.Add(decision_maker_list(index).ExitTime.TimeOfDay.ToString)      '청산시간
#End If
                        lv_done_decision_item.SubItems.Add(Code)                                                 '코드
                        lv_done_decision_item.SubItems.Add(Name)                                               '종목명
                        'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).GetType.ToString.Substring(StockDecisionMakerList(index).GetType.ToString.Length - 1, 1))     '이거 뭐였지? 아마도 multi했을 때 몇 번째 꺼인가 표시?
#If ALLOW_MULTIPLE_ENTERING Then
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterPrice)                       '진입가
#Else
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterPrice)                       '진입가
#End If
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).ExitPrice)                        '청산가
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).Profit.ToString("p"))                           '수익률
                        If decision_maker_list(index).FallVolume = 0 Then
                            lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                        Else
                            lv_done_decision_item.SubItems.Add(decision_maker_list(index).FallVolume.ToString("##,#"))      '하강볼륨
                        End If
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).MABase.ToString)               '파트인덱스
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).RelativeFall.ToString)               'RelativeFall
#If ALLOW_MULTIPLE_ENTERING Then
                        lv_done_decision_item.SubItems.Add(decision_maker_list(index).NumberOfEntering.ToString("##,#"))      '
#End If
                        If decision_maker_list(index).FallVolumeLimited = 0 Then
                            lv_done_decision_item.SubItems.Add("0")      '리미티드 하강볼륨
                        Else
                            lv_done_decision_item.SubItems.Add(decision_maker_list(index).FallVolumeLimited.ToString("##,#"))      '리미티드 하강볼륨
                        End If
                        If GangdoDB Then
                            'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).BuyAmountCenter)               'BuyAmountCenter
                            'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).SelAmountCenter)               'SelAmountCenter
                            'lv_done_decision_item.SubItems.Add(decision_maker_list(index).EnterDeltaGangdo)               'DeltaGangdo
                        End If
                        lv_done_decision_item.Tag = decision_maker_list(index)                      '리스트뷰 아이템 태그에 디시전 객체 걸기
#If NO_SHOW_TO_THE_FORM Then
                        MainForm.HitList.Add(lv_done_decision_item)
#Else
                        MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
#End If
                        'StockDecisionMakerList(index).StockOperator = Nothing         'stock operator 초기화
                        decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                        'Else
                        'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                        'End If
                    End If
                ElseIf decision_maker_list(index)._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE Then
                    '180109: 원래 이 조건은 없었으나, WAIT_EXIT 상태에서 취소된 경우를 구분하기 위해 만들게 되었다..
                    decision_maker_list.RemoveAt(index)          '아무 동작 안 하고 decision maker를 삭제한다.
                End If
            Next

            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            Dim monitoring_decision_maker_exist As Boolean = False

            For index As Integer = 0 To decision_maker_list.Count - 1
                'MovingAverageDifference 인 경우
                monitoring_decision_maker_exist = True       '무조건 하나라도 있으면 더 이상 생성되면 안 된다.
            Next
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                'Moving Average Difference 전략
                decision_maker_list.Add(New c05G_MovingAverageDifference(Me, StartTime, index_decision_center))
            End If
        Next
        Dim new_madiffsca As Double
        If new_candle.Average5Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average5Minutes) / new_candle.Average5Minutes
            If new_madiffsca > MadiffscaFor5M Then
                MadiffscaFor5M = (new_madiffsca - MadiffscaFor5M) * REDUCE_ASSIGN_RATE + MadiffscaFor5M
            Else
                MadiffscaFor5M *= MADIFFSCA_FADE_FACTOR_DEFAULT ^ (1 / 5)
            End If
        End If
            If new_candle.Average30Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average30Minutes) / new_candle.Average30Minutes
            If new_madiffsca > MadiffscaFor30M Then
                MadiffscaFor30M = (new_madiffsca - MadiffscaFor30M) * REDUCE_ASSIGN_RATE + MadiffscaFor30M
            Else
                MadiffscaFor30M *= MADIFFSCA_FADE_FACTOR_DEFAULT ^ (1 / 30)
            End If
        End If
        If new_candle.Average35Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average35Minutes) / new_candle.Average35Minutes
            If new_madiffsca > MadiffscaFor35M Then
                MadiffscaFor35M = (new_madiffsca - MadiffscaFor35M) * REDUCE_ASSIGN_RATE + MadiffscaFor35M
            Else
                MadiffscaFor35M *= MADIFFSCA_FADE_FACTOR_MA0035 ^ (1 / 35)
            End If
        End If
        If new_candle.Average70Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average70Minutes) / new_candle.Average70Minutes
            If new_madiffsca > MadiffscaFor70M Then
                MadiffscaFor70M = (new_madiffsca - MadiffscaFor70M) * REDUCE_ASSIGN_RATE + MadiffscaFor70M
            Else
                MadiffscaFor70M *= MADIFFSCA_FADE_FACTOR_MA0070 ^ (1 / 70)
            End If
        End If
        If new_candle.Average140Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average140Minutes) / new_candle.Average140Minutes
            If new_madiffsca > MadiffscaFor140M Then
                MadiffscaFor140M = (new_madiffsca - MadiffscaFor140M) * REDUCE_ASSIGN_RATE + MadiffscaFor140M
            Else
                MadiffscaFor140M *= MADIFFSCA_FADE_FACTOR_MA0140 ^ (1 / 140)
            End If
        End If
        If new_candle.Average280Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average280Minutes) / new_candle.Average280Minutes
            If new_madiffsca > MadiffscaFor280M Then
                MadiffscaFor280M = (new_madiffsca - MadiffscaFor280M) * REDUCE_ASSIGN_RATE + MadiffscaFor280M
            Else
                MadiffscaFor280M *= MADIFFSCA_FADE_FACTOR_MA0280 ^ (1 / 280)
            End If
        End If
            If new_candle.Average560Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average560Minutes) / new_candle.Average560Minutes
            If new_madiffsca > MadiffscaFor560M Then
                MadiffscaFor560M = (new_madiffsca - MadiffscaFor560M) * REDUCE_ASSIGN_RATE + MadiffscaFor560M
            Else
                MadiffscaFor560M *= MADIFFSCA_FADE_FACTOR_MA0560 ^ (1 / 560)
            End If
        End If
            If new_candle.Average1200Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average1200Minutes) / new_candle.Average1200Minutes
            If new_madiffsca > MadiffscaFor1200M Then
                MadiffscaFor1200M = (new_madiffsca - MadiffscaFor1200M) * REDUCE_ASSIGN_RATE + MadiffscaFor1200M
            Else
                MadiffscaFor1200M *= MADIFFSCA_FADE_FACTOR_MA1200 ^ (1 / 1200)
            End If
        End If
        If new_candle.Average2400Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average2400Minutes) / new_candle.Average2400Minutes
            If new_madiffsca > MadiffscaFor2400M Then
                MadiffscaFor2400M = (new_madiffsca - MadiffscaFor2400M) * REDUCE_ASSIGN_RATE + MadiffscaFor2400M
            Else
                MadiffscaFor2400M *= MADIFFSCA_FADE_FACTOR_MA2400 ^ (1 / 2400)
            End If
        End If
        If new_candle.Average4800Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average4800Minutes) / new_candle.Average4800Minutes
            If new_madiffsca > MadiffscaFor4800M Then
                MadiffscaFor4800M = (new_madiffsca - MadiffscaFor4800M) * REDUCE_ASSIGN_RATE + MadiffscaFor4800M
            Else
                MadiffscaFor4800M *= MADIFFSCA_FADE_FACTOR_MA4800 ^ (1 / 4800)
            End If
        End If
        If new_candle.Average9600Minutes <> -1 Then
            new_madiffsca = Math.Abs(new_candle.Close - new_candle.Average9600Minutes) / new_candle.Average9600Minutes
            If new_madiffsca > MadiffscaFor9600M Then
                MadiffscaFor9600M = (new_madiffsca - MadiffscaFor9600M) * REDUCE_ASSIGN_RATE + MadiffscaFor9600M
            Else
                MadiffscaFor9600M *= MADIFFSCA_FADE_FACTOR_MA9600 ^ (1 / 9600)
            End If
        End If
        MADecisionFlag = False   '2020.09.14: 한 iteration 에서 진입하는 decision maker를 1개로 제한하기 위한 flag다. 매 iteration 마다 초기화한다.
    End Sub
#End If

    'Get VariationRatio
    Public Function GetVariationRatio(ByVal new_price As UInt32) As Single
        If IsVariationRatioReady() Then
            Dim new_min_of_5_prices As UInt32 = Math.Min(_MinPrecalForVariationRatio, new_price)
            Dim new_max_of_5_prices As UInt32 = Math.Max(_MaxPrecalForVariationRatio, new_price)
            Dim new_variation_of_5_prices As UInt32 = new_max_of_5_prices - new_min_of_5_prices

            Return new_variation_of_5_prices / GetAverage5Minutes(new_price)
        Else
            Return 0
        End If
    End Function

    'Update Precals of VariationRatio
    Public Sub UpdateVariationRatioPrecal()
        'Precal은 지난 4개 종가의 min, max 값이 유지된다.
        _MinPrecalForVariationRatio = Math.Min(_MinPrecalForVariationRatio, MinuteCandleSeries.Last.Low)
        _MaxPrecalForVariationRatio = Math.Max(_MaxPrecalForVariationRatio, MinuteCandleSeries.Last.High)

        If MinuteCandleSeries.Count >= 5 Then
            Dim losing_price_low As UInt32 = MinuteCandleSeries(MinuteCandleSeries.Count - 5).Low
            If losing_price_low = _MinPrecalForVariationRatio Then
                Dim temp_min As UInt32 = MinuteCandleSeries.Last.Low
                For index As Integer = 0 To 2
                    temp_min = Math.Min(temp_min, MinuteCandleSeries(MinuteCandleSeries.Count - 2 - index).Low)
                Next
                _MinPrecalForVariationRatio = temp_min
            End If

            Dim losing_price_high As UInt32 = MinuteCandleSeries(MinuteCandleSeries.Count - 5).High
            If losing_price_high = _MaxPrecalForVariationRatio Then
                Dim temp_max As UInt32 = MinuteCandleSeries.Last.High
                For index As Integer = 0 To 2
                    temp_max = Math.Max(temp_max, MinuteCandleSeries(MinuteCandleSeries.Count - 2 - index).High)
                Next
                _MaxPrecalForVariationRatio = temp_max
            End If
        End If
    End Sub

    'Check if VariationRatio is ready to be calculated
    Public Function IsVariationRatioReady() As Boolean
        If MinuteCandleSeries.Count >= 4 Then
            Return True
        Else
            Return False
        End If
    End Function

    'Get Average5Minutes
    Public Function GetAverage5Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 5개의 price들을 평균냄
            If MinuteCandleSeries.Count >= 5 Then
                Dim new_sum_of_5_prices As Single = _PrecalForAverage5Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 5).Close
                Return new_sum_of_5_prices / 5
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 4개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 4 Then
                Dim new_sum_of_5_prices As Single = _PrecalForAverage5Minutes + new_price
                Return new_sum_of_5_prices / 5
            Else
                Return -1
            End If
        End If
    End Function

    'Update Precal of Average5Minutes
    Public Sub UpdateAverage5MinutesPrecal()
        'Precal은 지난 4개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage5Minutes += new_price
        If MinuteCandleSeries.Count >= 5 Then
            _PrecalForAverage5Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 5).Close
        End If
    End Sub

    'Get Average30Minutes
    Public Function GetAverage30Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 20개의 price들을 평균냄
            If MinuteCandleSeries.Count >= MA_MINUTE_COUNT AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 20 Then
                Dim new_sum_of_20_prices As Single = _PrecalForAverage30Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - MA_MINUTE_COUNT).Close
                Return new_sum_of_20_prices / MA_MINUTE_COUNT
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 39개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= MA_MINUTE_COUNT - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 20 Then
                Dim new_sum_of_20_prices As Single = _PrecalForAverage30Minutes + new_price
                Return new_sum_of_20_prices / MA_MINUTE_COUNT
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average35Minutes
    Public Function GetAverage35Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 35개의 price들을 평균냄
            If MinuteCandleSeries.Count >= MA_MINUTE_COUNT AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 35 Then
                Dim new_sum_of_35_prices As Single = _PrecalForAverage35Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - MA_MINUTE_COUNT).Close
                Return new_sum_of_35_prices / MA_MINUTE_COUNT
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 34개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= MA_MINUTE_COUNT - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 35 Then
                Dim new_sum_of_35_prices As Single = _PrecalForAverage35Minutes + new_price
                Return new_sum_of_35_prices / MA_MINUTE_COUNT
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average70Minutes
    Public Function GetAverage70Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 70개의 price들을 평균냄
            If MinuteCandleSeries.Count >= 70 AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 70 Then
                Dim new_sum_of_70_prices As Single = _PrecalForAverage70Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 70).Close
                Return new_sum_of_70_prices / 70
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 69개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 70 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 70 Then
                Dim new_sum_of_70_prices As Single = _PrecalForAverage70Minutes + new_price
                Return new_sum_of_70_prices / 70
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average140Minutes
    Public Function GetAverage140Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 140개의 price들을 평균냄
            If MinuteCandleSeries.Count >= 140 AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 140 Then
                Dim new_sum_of_140_prices As Single = _PrecalForAverage140Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 140).Close
                Return new_sum_of_140_prices / 140
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 139개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 140 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 140 Then
                Dim new_sum_of_140_prices As Single = _PrecalForAverage140Minutes + new_price
                Return new_sum_of_140_prices / 140
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average280Minutes
    Public Function GetAverage280Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 280개의 price들을 평균냄
            If MinuteCandleSeries.Count >= 280 AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 280 Then
                Dim new_sum_of_280_prices As Single = _PrecalForAverage280Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 280).Close
                Return new_sum_of_280_prices / 280
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 279개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 280 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 280 Then
                Dim new_sum_of_280_prices As Single = _PrecalForAverage280Minutes + new_price
                Return new_sum_of_280_prices / 280
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average560Minutes
    Public Function GetAverage560Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 560개의 price들을 평균냄
            If MinuteCandleSeries.Count >= 560 AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 560 Then
                Dim new_sum_of_560_prices As Single = _PrecalForAverage560Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 560).Close
                Return new_sum_of_560_prices / 560
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 559개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 560 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 560 Then
                Dim new_sum_of_560_prices As Single = _PrecalForAverage560Minutes + new_price
                Return new_sum_of_560_prices / 560
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average1200Minutes
    Public Function GetAverage1200Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 1200개의 price들을 평균냄
            If (MinuteCandleSeries.Count >= 1200) AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 1200 Then
                Dim new_sum_of_1200_prices As Single = _PrecalForAverage1200Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 1200).Close
                Return new_sum_of_1200_prices / 1200
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 1199개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 1200 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 1200 Then
                Dim new_sum_of_1200_prices As Single = _PrecalForAverage1200Minutes + new_price
                Return new_sum_of_1200_prices / 1200
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average2400Minutes
    Public Function GetAverage2400Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 2400개의 price들을 평균냄
            If (MinuteCandleSeries.Count >= 2400) AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 2400 Then
                Dim new_sum_of_2400_prices As Single = _PrecalForAverage2400Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 2400).Close
                Return new_sum_of_2400_prices / 2400
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 2399개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 2400 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 2400 Then
                Dim new_sum_of_2400_prices As Single = _PrecalForAverage2400Minutes + new_price
                Return new_sum_of_2400_prices / 2400
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average4800Minutes
    Public Function GetAverage4800Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 4800개의 price들을 평균냄
            If (MinuteCandleSeries.Count >= 4800) AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 4800 Then
                Dim new_sum_of_4800_prices As Single = _PrecalForAverage4800Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 4800).Close
                Return new_sum_of_4800_prices / 4800
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 4799개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 4800 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 4800 Then
                Dim new_sum_of_4800_prices As Single = _PrecalForAverage4800Minutes + new_price
                Return new_sum_of_4800_prices / 4800
            Else
                Return -1
            End If
        End If
    End Function

    'Get Average9600Minutes
    Public Function GetAverage9600Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'MinuteCandleSeries에 저장되어 있는 9600개의 price들을 평균냄
            If (MinuteCandleSeries.Count >= 9600) AndAlso DiscontinuousPoint - 1 < MinuteCandleSeries.Count - 9600 Then
                Dim new_sum_of_9600_prices As Single = _PrecalForAverage9600Minutes + MinuteCandleSeries(MinuteCandleSeries.Count - 9600).Close
                Return new_sum_of_9600_prices / 9600
            Else
                Return -1
            End If
        Else
            'MinuteCandleSeries에 저장되어 있는 9599개의 price + new_price 의 평균
            If MinuteCandleSeries.Count >= 9600 - 1 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - 9600 Then
                Dim new_sum_of_9600_prices As Single = _PrecalForAverage9600Minutes + new_price
                Return new_sum_of_9600_prices / 9600
            Else
                Return -1
            End If
        End If
    End Function

    'Update Precal of Average30Minutes
    Public Sub UpdateAverage30MinutesPrecal()
        'Precal은 지난 29개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage30Minutes += new_price
        If MinuteCandleSeries.Count >= MA_MINUTE_COUNT Then
            _PrecalForAverage30Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - MA_MINUTE_COUNT).Close
        End If
    End Sub

    'Update Precal of Average35Minutes
    Public Sub UpdateAverage35MinutesPrecal()
        'Precal은 지난 34개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage35Minutes += new_price
        If MinuteCandleSeries.Count >= MA_MINUTE_COUNT Then
            _PrecalForAverage35Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - MA_MINUTE_COUNT).Close
        End If
    End Sub

    'Update Precal of Average70Minutes
    Public Sub UpdateAverage70MinutesPrecal()
        'Precal은 지난 69개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage70Minutes += new_price
        If MinuteCandleSeries.Count >= 70 Then
            _PrecalForAverage70Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 70).Close
        End If
    End Sub

    'Update Precal of Average140Minutes
    Public Sub UpdateAverage140MinutesPrecal()
        'Precal은 지난 139개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage140Minutes += new_price
        If MinuteCandleSeries.Count >= 140 Then
            _PrecalForAverage140Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 140).Close
        End If
    End Sub

    'Update Precal of Average280Minutes
    Public Sub UpdateAverage280MinutesPrecal()
        'Precal은 지난 279개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage280Minutes += new_price
        If MinuteCandleSeries.Count >= 280 Then
            _PrecalForAverage280Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 280).Close
        End If
    End Sub

    'Update Precal of Average560Minutes
    Public Sub UpdateAverage560MinutesPrecal()
        'Precal은 지난 559개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage560Minutes += new_price
        If MinuteCandleSeries.Count >= 560 Then
            _PrecalForAverage560Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 560).Close
        End If
    End Sub

    'Update Precal of Average1200Minutes
    Public Sub UpdateAverage1200MinutesPrecal()
        'Precal은 지난 1199개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage1200Minutes += new_price
        If MinuteCandleSeries.Count >= 1200 Then
            _PrecalForAverage1200Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 1200).Close
        End If
    End Sub

    'Update Precal of Average2400Minutes
    Public Sub UpdateAverage2400MinutesPrecal()
        'Precal은 지난 2399개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage2400Minutes += new_price
        If MinuteCandleSeries.Count >= 2400 Then
            _PrecalForAverage2400Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 2400).Close
        End If
    End Sub

    'Update Precal of Average4800Minutes
    Public Sub UpdateAverage4800MinutesPrecal()
        'Precal은 지난 4799개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage4800Minutes += new_price
        If MinuteCandleSeries.Count >= 4800 Then
            _PrecalForAverage4800Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 4800).Close
        End If
    End Sub

    'Update Precal of Average9600Minutes
    Public Sub UpdateAverage9600MinutesPrecal()
        'Precal은 지난 9600개 종가의 합이 유지된다.
        Dim new_price = MinuteCandleSeries.Last.Close

        _PrecalForAverage9600Minutes += new_price
        If MinuteCandleSeries.Count >= 9600 Then
            _PrecalForAverage9600Minutes -= MinuteCandleSeries(MinuteCandleSeries.Count - 9600).Close
        End If
    End Sub

    'Update min/max amount today
    Public Sub UpdateMinMaxAmountToday()
        '오늘 분당거래량의 min max값을 업데이트하자.
        Dim last_amount As UInt32 = MinuteCandleSeries.Last.Amount

        If last_amount = 0 Then
            'VI 일수도 있다. 이거는 계산에 포함시키지 말자.
        Else
            _MinAmountToday = Math.Min(_MinAmountToday, last_amount)
        End If

        _MaxAmountToday = Math.Max(_MaxAmountToday, last_amount)

        _SumAmountToday += last_amount
        _CandleCountToday += 1
    End Sub

    Public Sub UpdateStability()
        If _MaxAmountToday = [UInt32].MinValue Then
            '첫 데이터 들어온 경우일 것이다. stability 계산할 데이터가 아직 없다.
            '아니면 진짜로 Amount가 없는 경우도 있다. 이때는 candle count를 초기화해줘야 한다.
            _SumAmountToday = 0
            _CandleCountToday = 0
        Else
            '20200621: 여기서 지난 하루의 뉴스탭을 계산한다.
            CalcNewstab()

            'Average Amount 업데이트
            AmountAveToday = _SumAmountToday / _CandleCountToday
            _SumAmountToday = 0
            _CandleCountToday = 0

            'Stability List 업데이트
            StabilityList.Add(Math.Log(Math.Min(_MinAmountToday, AmountAveToday) / _MaxAmountToday))
            If StabilityList.Count > MAX_NUMBER_OF_STABILITY_LIST Then
                StabilityList.RemoveAt(0)
            End If
            If StabilityList.Count > 8 AndAlso MinuteCandleSeries.Last.CandleTime.Date.Month = 3 AndAlso MinuteCandleSeries.Last.CandleTime.Date.Day = 20 Then
                Dim b = 1
                If Code = "A001290" Then
                    Dim a = 1
                ElseIf Code = "A005930" Then
                    Dim a = 1
                End If
            End If
            _MinAmountToday = [UInt32].MaxValue
            _MaxAmountToday = [UInt32].MinValue

            'Stability 업데이트
            Dim total_number_for_average As Integer = Math.Min(1, StabilityList.Count)
            Dim temp_sum As Single = 0
            For index As Integer = 0 To total_number_for_average - 1
                temp_sum += StabilityList(StabilityList.Count - index - 1)
            Next
            Stability = temp_sum / total_number_for_average

        End If
    End Sub

    Private Sub CalcNewstab()
        Dim ten_minutes_amount_list As New List(Of UInt32)
        Dim temp_amount_sum As UInt32 = 0
        Dim this_10minutes_index As Integer = 0
        Dim current_10minutes_index As Integer = 0
        Dim start_index As Integer = MinuteCandleSeries.Count - _CandleCountToday
        Dim end_index As Integer = MinuteCandleSeries.Count - 1
        '거래량을 10분씩 잘라서 더한다.
        For index As Integer = start_index To end_index
            this_10minutes_index = Math.Floor((MinuteCandleSeries(index).CandleTime.TimeOfDay.TotalMinutes - 1) / 10)
            If current_10minutes_index <> this_10minutes_index Then
                If current_10minutes_index = 0 Then
                    '처음으로 들어온 거라 아무것도 없는 것이다. current 값만 업데이트하고 나간다.
                    current_10minutes_index = this_10minutes_index
                Else
                    '지난 10분을 정리한다.
                    ten_minutes_amount_list.Add(temp_amount_sum)

                    temp_amount_sum = 0
                    current_10minutes_index = this_10minutes_index
                End If
            End If
            temp_amount_sum += MinuteCandleSeries(index).Amount
        Next
        If temp_amount_sum <> 0 Then
            '마지막 남은 수량을 정리한다.
            ten_minutes_amount_list.Add(temp_amount_sum)
        End If

        '10분씩 자른 거래량들을 분석한다.
        '앞에 10분 뒤에 10분은 분석에서 제외한다.
        If ten_minutes_amount_list.Count > 2 Then
            ten_minutes_amount_list.RemoveAt(ten_minutes_amount_list.Count - 1)
            ten_minutes_amount_list.RemoveAt(0)
            Dim max_10minutes_amount As UInt32 = ten_minutes_amount_list.Max
            Dim normalized_10minutes_amount As New List(Of Double)
            For index As Integer = 0 To ten_minutes_amount_list.Count - 1
                normalized_10minutes_amount.Add(ten_minutes_amount_list(index) / max_10minutes_amount)
            Next
            'Dim normalized_mid As Double = (1 + normalized_10minutes_amount.Min) / 2
            Dim distance_sum As Double = 0
            Dim the_distance As Double
            For index As Integer = 0 To normalized_10minutes_amount.Count - 2
                the_distance = normalized_10minutes_amount(index) - normalized_10minutes_amount(index + 1)
                If the_distance > 0 Then
                    distance_sum += the_distance
                Else
                    distance_sum += -the_distance
                End If
            Next
            Dim new_stab As Double = normalized_10minutes_amount.Min ' distance_sum / (normalized_10minutes_amount.Count - 1)
            If Code = "A005930" Then 'new_stab > 0.1 Then 'Code = "A005930" Then
                Dim a = 1

            End If
            Newstab = new_stab
        Else
            Newstab = 0
        End If
    End Sub

    '190913:StockSearcher에서 이제 이걸 쓸일이 있을까 5초 DB 에서 데이터를 꺼내 candle을 만드는 일이..
    Public Function MinuteCandleNewData(a_record As SymbolRecord) As Boolean
        If RecordList.Count = 1 Then
            AmountAtZero = a_record.CoreRecord.Amount

            Return 0
        End If

        Dim candle_update_complited As Boolean = False
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(5 * (RecordList.Count - 1))
        Dim minute_index As Integer = current_time.Hour * 60 + current_time.Minute - StartTime.Hour * 60 - StartTime.Minute

        If MinuteCandleSeries.Count = 0 Then
            Dim first_candle As CandleStructure

            first_candle.CandleTime = StartTime
            If a_record.CoreRecord.Amount > AmountAtZero Then
                first_candle.Amount = a_record.CoreRecord.Amount - AmountAtZero
            Else
                first_candle.Amount = 0
            End If
            first_candle.AccumAmount = a_record.CoreRecord.Amount
            If first_candle.Amount > 0 Then
                '새로 거래가 이루어진 경우에만 업데이트
                first_candle.Open = a_record.CoreRecord.Price
                first_candle.Close = a_record.CoreRecord.Price
                first_candle.High = a_record.CoreRecord.Price
                first_candle.Low = a_record.CoreRecord.Price
            Else
                '안 그러면 Last만 그 이전가로 업데이트
                first_candle.Close = a_record.CoreRecord.Price
            End If

            MinuteCandleSeries.Add(first_candle)
            RaiseEvent evNewMinuteCandleCreated()
            NumberOfLastCandleUpdates = 1
        ElseIf minute_index = MinuteCandleSeries.Count - 1 Then
            'Last candle update 하기
            Dim last_amount As UInt32
            If MinuteCandleSeries.Count = 1 Then
                last_amount = AmountAtZero
            Else
                last_amount = MinuteCandleSeries(MinuteCandleSeries.Count - 2).AccumAmount
            End If

            If a_record.CoreRecord.Amount > last_amount Then
                Dim last_candle As CandleStructure = MinuteCandleSeries.Last
                If last_candle.Open = 0 Then
                    '첫거래로 인한 업데이트면
                    last_candle.Open = a_record.CoreRecord.Price
                    last_candle.Close = a_record.CoreRecord.Price
                    last_candle.High = a_record.CoreRecord.Price
                    last_candle.Low = a_record.CoreRecord.Price
                Else
                    '아니면
                    last_candle.Close = a_record.CoreRecord.Price
                    last_candle.High = Math.Max(last_candle.High, a_record.CoreRecord.Price)
                    last_candle.Low = Math.Min(last_candle.Low, a_record.CoreRecord.Price)
                End If
                If a_record.CoreRecord.Amount > last_amount Then
                    last_candle.Amount = a_record.CoreRecord.Amount - last_amount
                Else
                    last_candle.Amount = 0
                End If
                last_candle.AccumAmount = a_record.CoreRecord.Amount

                MinuteCandleSeries(MinuteCandleSeries.Count - 1) = last_candle

                TasksAfterNewCandleCreated()        'Test_ 변수도 모니터링으로 같이 업데이트 해보자. 임시다 이건.
            Else
                '추가 거래가 없다면 굳이 업데이트 안 한다.
            End If
            NumberOfLastCandleUpdates += 1
        Else
            '새로운 candle 만들기
            Dim new_candle As CandleStructure
            new_candle.CandleTime = StartTime + TimeSpan.FromMinutes(MinuteCandleSeries.Count)
            If a_record.CoreRecord.Amount > MinuteCandleSeries.Last.AccumAmount Then
                new_candle.Amount = a_record.CoreRecord.Amount - MinuteCandleSeries.Last.AccumAmount
            Else
                new_candle.Amount = 0
            End If
            new_candle.AccumAmount = a_record.CoreRecord.Amount
            If new_candle.Amount > 0 Then
                '새로 거래가 이루어진 경우에만 업데이트
                new_candle.Open = a_record.CoreRecord.Price
                new_candle.Close = a_record.CoreRecord.Price
                new_candle.High = a_record.CoreRecord.Price
                new_candle.Low = a_record.CoreRecord.Price
            Else
                '안 그러면 Last만 그 이전가로 업데이트
                new_candle.Close = a_record.CoreRecord.Price
            End If

#If 0 Then
            If MinuteCandleSeries.Count >= 4 Then
                'Variation5Minutes 업데이트
                Dim max_price, min_price As UInt32

                If new_candle.Amount > 0 Then
                    min_price = new_candle.Low
                    max_price = new_candle.High
                Else
                    min_price = UInt32.MaxValue
                    max_price = UInt32.MinValue
                End If
                For index As Integer = MinuteCandleSeries.Count - 4 To MinuteCandleSeries.Count - 1
                    If MinuteCandleSeries(index).Amount > 0 Then
                        min_price = Math.Min(min_price, MinuteCandleSeries(index).Low)
                        max_price = Math.Max(max_price, MinuteCandleSeries(index).High)
                    Else
                        'min_price 변경 없음
                    End If
                Next
                If max_price = 0 Then
                    new_candle.Variation5Minutes = 0
                Else
                    new_candle.Variation5Minutes = max_price - min_price
                End If

                'Average5Minutes 업데이트
                new_candle.Average5Minutes = (new_candle.Close + MinuteCandleSeries(MinuteCandleSeries.Count - 4).Close + MinuteCandleSeries(MinuteCandleSeries.Count - 3).Close + MinuteCandleSeries(MinuteCandleSeries.Count - 2).Close + MinuteCandleSeries(MinuteCandleSeries.Count - 1).Close) / 5

                'VariationRatio 업데이트
                new_candle.VariationRatio = new_candle.Variation5Minutes / new_candle.Average5Minutes
            End If
#End If

            MinuteCandleSeries.Add(new_candle)
            If MinuteCandleSeries.Count > MAX_NUMBER_OF_CANDLE Then
                MinuteCandleSeries.RemoveAt(0)
                StartTime = MinuteCandleSeries(0).CandleTime
            End If

            RaiseEvent evNewMinuteCandleCreated()

            NumberOfLastCandleUpdates = 1
        End If

        If NumberOfLastCandleUpdates = 12 Then
            Return True
        Else
            Return False
        End If
    End Function

    '편차의 이동평균
    Public Function MA_Var(ByVal how_long_in_minutes As UInt32) As Double
#If 0 Then
        If MinuteCandleSeries.Count >= 5 Then
            Dim sum As Double = 0
            Dim number_of_samples As Integer = Math.Min(how_long_in_5minutes, MinuteCandleSeries.Count / 5)
            For index As Integer = 0 To number_of_samples - 1
                sum = sum + MinuteCandleSeries(MinuteCandleSeries.Count - 1 - 5 * index).VariationRatio
            Next
            Return sum / number_of_samples
        Else
            '아직 average variation을 계산할 만큼 candle들이 안 모였다.
            Return -1
        End If
#Else
#If 0 Then
        If MinuteCandleSeries.Count >= 1 Then
            Dim sum As Double = 0
            Dim number_of_samples As Integer = Math.Min(how_long_in_minutes, MinuteCandleSeries.Count)
            For index As Integer = 0 To number_of_samples - 1
                sum = sum + MinuteCandleSeries(MinuteCandleSeries.Count - 1 - index).VariationRatio
            Next
            Return sum / number_of_samples
        Else
            '아직 average variation을 계산할 만큼 candle들이 안 모였다.
            Return -1
        End If
#End If
        If MinuteCandleSeries.Count = 1 Then
            '아직 average variation을 계산할 만큼 candle들이 안 모였다.
            Return MinuteCandleSeries.Last.VariationRatio
        Else
            If how_long_in_minutes < MinuteCandleSeries.Count Then
                '이전까지의 sum of VariationRatio에 losing된 VariationRatio를 빼고 새 VariationRatio를 더한다.
                Dim losing_candle As CandleStructure = MinuteCandleSeries(MinuteCandleSeries.Count - how_long_in_minutes - 1)
                Dim last_sum_of_variation_ratio As Double = MinuteCandleSeries(MinuteCandleSeries.Count - 2).Test_MA_Var * how_long_in_minutes
                Dim new_sum_of_variation_ratio As Double = last_sum_of_variation_ratio - losing_candle.VariationRatio + MinuteCandleSeries.Last.VariationRatio
                Return new_sum_of_variation_ratio / how_long_in_minutes
            Else
                '모아놓은 candle들이 아직 원하는 MA_VAR를 만들기에 부족한 상황
                If MinuteCandleSeries.Count = MAX_NUMBER_OF_CANDLE Then
                    'losing candle이 있다.
                    '이전까지의 sum of VariationRatio에 losing된 VariationRatio를 빼고 새 VariationRatio를 더한다.
                    Dim last_sum_of_variation_ratio As Double = MinuteCandleSeries(MinuteCandleSeries.Count - 2).Test_MA_Var * MAX_NUMBER_OF_CANDLE
                    Dim new_sum_of_variation_ratio As Double = last_sum_of_variation_ratio - LosingCandle.VariationRatio + MinuteCandleSeries.Last.VariationRatio
                    Return new_sum_of_variation_ratio / MAX_NUMBER_OF_CANDLE
                Else    'If MinuteCandleSeries.Count < MAX_NUMBER_OF_CANDLE
                    'losing candle이 없다.
                    'VariationRatio 를 계속 더해야 한다.
                    '이전까지의 sum of VariationRatio는 이전 MA_Var로부터 계산한다.
                    Dim last_candle_count As Integer = MinuteCandleSeries.Count - 1
                    Dim last_sum_of_variation_ratio As Double = MinuteCandleSeries(MinuteCandleSeries.Count - 2).Test_MA_Var * last_candle_count
                    Dim new_sum_of_variation_ratio As Double = last_sum_of_variation_ratio + MinuteCandleSeries.Last.VariationRatio
                    Return new_sum_of_variation_ratio / (last_candle_count + 1)
                End If
            End If
        End If
#End If
    End Function

    '가격의 이동평균
    Public Function MA_Price(ByVal how_long_in_5minutes As UInt32) As Double
        If MinuteCandleSeries.Count >= how_long_in_5minutes * 5 AndAlso DiscontinuousPoint < MinuteCandleSeries.Count - how_long_in_5minutes * 5 Then
            Dim sum As Double = 0
            For index As Integer = 0 To how_long_in_5minutes - 1
                sum = sum + MinuteCandleSeries(MinuteCandleSeries.Count - 1 - 5 * index).Average5Minutes
            Next
            Return sum / how_long_in_5minutes
        Else
            '아직 average price 를 계산할 만큼 candle들이 안 모였다.
            Return -1
        End If
    End Function

    '거래량의 이동평균
    Public Function MA_Amount(ByVal how_long_in_minutes As UInt32) As Double
        If MinuteCandleSeries.Count > how_long_in_minutes Then
            If MinuteCandleSeries.Count = 1 Then
                '2020.09.12: AmountAtZero 빼는 걸 왜 뺐는지 기억이 안 난다. 하지만 AmountAtZero 는 StockSearcher에서 실제로 안쓰이는 것 같다.
                'Return CType(MinuteCandleSeries(MinuteCandleSeries.Count - 1).AccumAmount - AmountAtZero, Double)
                Return CType(MinuteCandleSeries(MinuteCandleSeries.Count - 1).AccumAmount, Double)
            Else
                'Return CType(MinuteCandleSeries(MinuteCandleSeries.Count - 1).AccumAmount - MinuteCandleSeries(MinuteCandleSeries.Count - 1 - how_long_in_minutes).AccumAmount, Double) / how_long_in_minutes
                Return CType(MinuteCandleSeries(MinuteCandleSeries.Count - 1).AccumAmount - MinuteCandleSeries(MinuteCandleSeries.Count - 1 - how_long_in_minutes).AccumAmount, Double) / how_long_in_minutes
            End If
        Else
            'Return CType(MinuteCandleSeries(MinuteCandleSeries.Count - 1).AccumAmount - AmountAtZero, Double) / MinuteCandleSeries.Count
            Return CType(MinuteCandleSeries(MinuteCandleSeries.Count - 1).AccumAmount, Double) / MinuteCandleSeries.Count
        End If
    End Function

    Public Sub TasksAfterNewCandleCreated()
#If 0 Then
        Dim last_candle As CandleStructure = MinuteCandleSeries.Last

        last_candle.Test_MA_Var = MA_Var(500)
        If last_candle.Test_MA_Var > 0.007 Then
            last_candle.Test_MA_Price = GetAverage4800Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.004 Then
            last_candle.Test_MA_Price = GetAverage4800Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.002 Then
            last_candle.Test_MA_Price = GetAverage4800Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.001 Then
            last_candle.Test_MA_Price = GetAverage4800Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.0 Then
            last_candle.Test_MA_Price = GetAverage4800Minutes(0)
        Else
            last_candle.Test_MA_Price = -1
        End If

        MinuteCandleSeries(MinuteCandleSeries.Count - 1) = last_candle
#End If
    End Sub

    Public Function GetYesterPrice() As UInt32
        Dim top, bottom As Integer
        Dim new_index As Integer
        Dim index As Integer = Math.Ceiling(BasePriceHistory.Count / 2)
        Dim the_date As UInt32 = StartTime.Year * 10000 + StartTime.Month * 100 + StartTime.Date.Day

        'History Count부터 조사해본다.
        If BasePriceHistory.Count = 0 Then
            '자료 없다
            Return 0
        End If
        '인덱스 0부터 조사해본다.
        If the_date = BasePriceHistory(0).DateInteger Then
            Return BasePriceHistory(0).BasePrice
        End If

        top = BasePriceHistory.Count - 1
        bottom = 0
        Dim debug As Integer = 0
        Do
            debug = debug + 1
            'history 데이터는 date의 역순으로 저장되어 있음에 유의하라
            If the_date > BasePriceHistory(index).DateInteger Then
                new_index = Math.Ceiling((index + bottom) / 2)
                If new_index = index Then
                    '답이 없다.
                    Return 0
                Else
                    top = index
                    index = new_index
                End If
            ElseIf the_date < BasePriceHistory(index).DateInteger Then
                new_index = Math.Ceiling((index + top) / 2)
                If new_index = index Then
                    '답이 없다.
                    Return 0
                Else
                    bottom = index
                    index = new_index
                End If
            Else
                '찾았다.
                Return BasePriceHistory(index).BasePrice
            End If
        Loop
    End Function

#If 0 Then

    '하한가 비상상황임이 전해졌을 때
    '[OneKey] Order Blanked event hander- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub LowLimitCautionIssued()
        For index As Integer = 0 To StockDecisionMakerList.Count - 1
            If StockDecisionMakerList(index).StockOperator IsNot Nothing Then
                'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                StockDecisionMakerList(index).NoMoreOperation = True
            Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
            End If
        Next
        If MULTIPLE_DECIDER Then
            'Copy1
            For index As Integer = 0 To StockDecisionMakerList_Copy1.Count - 1
                If StockDecisionMakerList_Copy1(index).StockOperator IsNot Nothing Then
                    'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                    StockDecisionMakerList_Copy1(index).NoMoreOperation = True
                Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
                End If
            Next
            'Copy2
            For index As Integer = 0 To StockDecisionMakerList_Copy2.Count - 1
                If StockDecisionMakerList_Copy2(index).StockOperator IsNot Nothing Then
                    'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                    StockDecisionMakerList_Copy2(index).NoMoreOperation = True
                Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
                End If
            Next
            'Copy3
            For index As Integer = 0 To StockDecisionMakerList_Copy3.Count - 1
                If StockDecisionMakerList_Copy3(index).StockOperator IsNot Nothing Then
                    'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                    StockDecisionMakerList_Copy3(index).NoMoreOperation = True
                Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
                End If
            Next
        End If
        NoMoreDecisionMaker = True      '오늘 이 종목은 끝났다.
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘
#End If

    '디시전 관련 리스트뷰 아이템 등록
    Private Sub RegisterDoneDecision(ByVal list_view_item As ListViewItem)
        'If MainForm.lv_DoneDecisions.Items.Count > 1000 Then
        '못보여줌
        'MainForm.tb_Display.Text = "못보여줌" & MainForm.Form_DEFAULT_HAVING_TIME.ToString & ", " & MainForm.Form_FALL_SCALE_LOWER_THRESHOLD.ToString() & ", " & MainForm.Form_MAX_HAVING_LENGTH.ToString() & ", " & MainForm.Form_SCORE_THRESHOLD.ToString()
        'Else
        MainForm.lv_DoneDecisions.Items.Add(list_view_item)
        MainForm.lv_DoneDecisions.Items(MainForm.lv_DoneDecisions.Items.Count - 1).Focused = True
        'End If
    End Sub

#If 0 Then
    '리스트뷰 업데이트
    'Private Sub ListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String, ByVal text_4 As String)
    Private Sub ListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String)
        If LVItem IsNot Nothing Then
            LVItem.SubItems(LVSUB_NOW_PRICE).Text = text_0
            LVItem.SubItems(LVSUB_AMOUNT).Text = text_1
            'LVItem.SubItems(LVSUB_GANGDO).Text = text_2
            LVItem.SubItems(LVSUB_DATA_COUNT).Text = text_2
            'If MAPossible Then
            '이평 가능한 조건이면
            'LVItem.SubItems(LVSUB_MOVING_AVERAGE_PRICE).Text = text_3
            'End If
        End If
    End Sub

    Public Sub SaveToDB(ByVal db_connection As OleDb.OleDbConnection)
        Dim cmd As OleDb.OleDbCommand
        Dim result As Integer
        Dim insert_command As String
        Dim sample_time As DateTime
        Dim price As UInt32
        Dim amount As UInt64
        'Dim gangdo As Double
        Dim error_count As Integer = 0

        If Not DBTableExist Then
            'Table이 존재하지 않음 => DB 생성
            'create table A00001 (SampledTime DateTime primary key, Price LONG, Amount DECIMAL, Gangdo FLOAT);
            'Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price LONG, Amount DECIMAL, Gangdo DOUBLE);"
            'Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price BIGINT, Amount DECIMAL, Gangdo float);"
            Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price INT, Amount BIGINT);"
            cmd = New OleDb.OleDbCommand(table_create_command, db_connection)
            result = cmd.ExecuteNonQuery()             '명령 실행
            cmd.Dispose()
        End If

        For index As Integer = 0 To RecordList.Count - 1
            sample_time = StartTime + TimeSpan.FromSeconds(index * 5)
            price = RecordList(index).Price
            amount = RecordList(index).Amount
            'gangdo = RecordList(index).Gangdo
            'insert_command = "INSERT INTO " & Code & " (SampledTime, Price, Amount, Gangdo) VALUES ('" & sample_time.ToString("yyyy'-'MM'-'dd HH':'mm':'ss") & "', " & price.ToString & ", " & amount.ToString & ", " & gangdo.ToString & ");"
            insert_command = "INSERT INTO " & Code & " (SampledTime, Price, Amount) VALUES ('" & sample_time.ToString("yyyy'-'MM'-'dd HH':'mm':'ss") & "', " & price.ToString & ", " & amount.ToString & ");"
            cmd = New OleDb.OleDbCommand(insert_command, db_connection)
            Do
                Try
                    result = cmd.ExecuteNonQuery()              'insert 명령 실행
                    error_count = 0
                Catch ex As Exception
                    error_count += 1
                    'MainForm.tb_Display.AppendText(Code & ": Error during saving" & error_count & vbCrLf)
                    Threading.Thread.Sleep(10000)
                    'error 시 db 닫았다 다시 연다. db가 커질 수록 이상한 에러가 많이 뜨는 것 같다.
                    'db_connection.Close()
                    'db_connection.Open()
                    cmd.Dispose()
                    cmd = New OleDb.OleDbCommand(insert_command, db_connection)
                End Try
            Loop While error_count > 0 AndAlso error_count < 10

            cmd.Dispose()
        Next
    End Sub
#End If

    Public Sub MakeBasePriceHistory(ByVal history_file_contents() As String)
        Dim base_price_history_item As BasePriceHistoryRecord
        For index As Integer = 0 To history_file_contents.Count - 1
            base_price_history_item.DateInteger = Convert.ToUInt32(history_file_contents(index).Substring(0, 8))
            base_price_history_item.BasePrice = Convert.ToUInt32(history_file_contents(index).Substring(10, history_file_contents(index).Length - 10))
            BasePriceHistory.Add(base_price_history_item)
        Next
    End Sub
End Class
