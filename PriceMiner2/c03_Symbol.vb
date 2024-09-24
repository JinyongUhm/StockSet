Imports System.Threading
Imports XA_DATASETLib
'Imports WcfCandleManage
Imports CandleServiceInterfacePrj
Imports System.Linq

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

    Public Structure DelayedSampleRecord
        Dim Price As UInt32
        Dim Time As TimeSpan
    End Structure

    Public Enum TypeRequestState
        RS_IDLE
        RS_CHECKINGPRICE_REQUESTED
        RS_REALTIMEPRICE_REQUESTED
        RS_REALTIMEPRICE_CONFIRMED
        RS_ERROR
    End Enum

    Public Const CATCH_SECONDS = 1
    '181206_TODO: 이 종목이 2분간 매매정지 상태인지 나타내는 변수가 필요함. 그래서 그런 종목은 매수 대상에서 제외해야 함.
    '181213_TODO: match 되는 종목을 발견하자마자 주문하는 방식으로 바뀌어야 됨. 그래야 매수성공률 올라감.

    Public Delegate Sub DelegateRegisterDecision(ByVal list_view_item)
    Public Delegate Sub DelegateUpdateDecision(ByVal the_decision_maker As c05G_MovingAverageDifference)
    'Public Delegate Sub DelegateListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String, ByVal text_4 As String)
    Public Delegate Sub DelegateListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String)

    'Public CodeIndex As Integer
    Public Code As String
    Public Name As String
    Public MarketKind As MARKET_KIND
    Public Caution As Boolean   '투자유의 여부
    Public Supervision As Boolean   '관리종목 여부
    Public _EvidanRate As Integer = 100
    Public EvidanComplete As Boolean = False
    Public _StartTime As DateTime = [DateTime].MinValue
    Public _CandleStartTime As DateTime = [DateTime].MinValue
    'Public LVItem As ListViewItem
    Public DBTableExist As Boolean = False
    'Private _PriceList As New List(Of UInt32)
    'Private _AmountList As New List(Of UInt64)
    'Private _GangdoList As New List(Of Double)
    'Private _BuyAmountList As New List(Of UInt64)
    'Private _SelAmountlist As New List(Of UInt64)
    Public RecordList As New List(Of SymbolCoreRecord)
    Private _BuyAmountList As New List(Of UInt64)
    Private _SelAmountList As New List(Of UInt64)
#If MOVING_AVERAGE_DIFFERENCE Then
    Public CandleServiceCenter As New CandleChunkList 'c06_CandleLink
    'Public CandleServiceCenter As New c06_CandleLink(SymTree.CandleFileSystem)
    Public GrowingCandle As CandleStructure
    Public LosingCandle As CandleStructure
    Public IsGrowingCandleExist As Boolean = False
    Public AmountAtZero As UInt32 = 0
    Public AmountAtLastTime As UInt32
    'Public NumberOfLastCandleUpdates As Integer
#End If
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
    Private _IsAverage5MinutesReady As Boolean
    Private _IsAverage35MinutesReady As Boolean
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
    Public Main_MADecisionFlag As Boolean = False
    Public Sub_MADecisionFlag As Boolean = False

    'Public StockDecisionMaker As c050_DecisionMaker
    'Public StockOperator As et2_Operation
#If MOVING_AVERAGE_DIFFERENCE Then
    Public MainDecisionMakerCenter As New List(Of List(Of c05G_DoubleFall))
    Public SubDecisionMakerCenter As New List(Of List(Of c05G_MovingAverageDifference))
#Else
    Public MainDecisionMakerCenter As New List(Of List(Of c05E_PatternChecker))
#End If
    Public TestDecisionMakerCenter As New List(Of List(Of c05F_FlexiblePCRenew))
    'Public StockDecisionMakerList As New List(Of c05E_PatternChecker)
    'Public StockDecisionMakerList_Copy1 As New List(Of c05E_PatternChecker_copy1)
    'Public StockDecisionMakerList_Copy2 As New List(Of c05E_PatternChecker_copy2)
    'Public StockDecisionMakerList_Copy3 As New List(Of c05E_PatternChecker_copy3)
    Public MyEnterExitStateList As New List(Of EnterOrExit)
    Public MyAccountList As New List(Of et1_AccountManager)
    Public MyOrderNumberList As New List(Of ULong)
    Public MyPriceList As New List(Of UInt32)
    Public MyAmountList As New List(Of UInt32)
    Public LowLimitPrice As Double
    Public HighLimitPrice As Double
    Public YesterPrice As UInt32
    Public ArgObjects(3) As Object
    Private WithEvents _H1_ As et32_XAReal_Wrapper
    Private WithEvents _HA_ As et32_XAReal_Wrapper
    Private WithEvents _T1101 As New et31_XAQuery_Wrapper()
    Private WithEvents _T1102 As et31_XAQuery_Wrapper
    'Public StockOperatorList As New List(Of et2_Operation)
    Private CodeForRequest As String
    Public RequestStatus As TypeRequestState = TypeRequestState.RS_IDLE
    Private _LastCallPrices As CallPrices
    Private _PriceUpdateCount As UInt32
    'Public Price As UInt32
    Public PriceRealMonitoring As Integer
    Public PriceRealMonitoringKey As TracingKey                    '이키도 내버려두자. 최말단키로 critical zone에서 다른 critical zone 부르는 일 없다.
    Public CallPriceKey As TracingKey ' Integer                      '콜프라이스키는 내버려두자. 최말단키로 critical zone에서 다른 critical zone 부르는 일 없다.
    Private _IsCallPriceAvailable As Boolean
    Public OneKey As TracingKey            '140422 : 한 종목 안에선 이키로 다 해결하자. 키가 너무 많아져 네스티드 구조 꼬일 염려가 있다.
    Public NoMoreDecisionMaker As Boolean = False
    Public CurrentAmount As UInt32
    Public AlreadyHooked As Boolean
    Public OpenPrice As UInt32
    Public StoredPrice As UInt32
    Public StoredAmount As UInt64
#If MOVING_AVERAGE_DIFFERENCE Then
    Public Event evNewMinuteCandleCreated()
    Public Shared MAX_NUMBER_OF_CANDLE As Integer = 9601
    '190817:2000으로 하면 530M, 4000으로 하면 870M, candle load 안 하면 160M
    '190817:4000에 candle structure에서 Double을 Single로 줄이면 610M
    Public OldDate As DateTime = [DateTime].MinValue
    Public AmountVar As Double = 0
    Public DiscontinuousPoint As Integer = -1
    Public DailyAmountStore As New List(Of UInt64)
    Public NUMBER_OF_DAYS_FOR_DAILY_AMOUNT_VAR As Integer = 10
    Public MAP_1 As Integer = 7
    Public ScanStartDate As DateTime = [DateTime].MinValue
#End If
    Public StoredGangdo As Double
    Public StoringKey As TracingKey
    Public BuyAveAmount As UInt32          '최근 3번 data 를 이용해 5초간 매수체결수량의 평균을 구함
    Public SelAveAmount As UInt32          '최근 3번 data 를 이용해 5초간 매도체결수량의 평균을 구함
    Private _VI As Boolean = False            '변동성 완화장치 발동 여부
    Private _T1102_ReceiveRealData_Thread As System.Threading.Thread
    Public SymbolIndex As Integer
    Public DelayedSampleListBuy As New List(Of DelayedSampleRecord)
    Public DelayedSampleListSel As New List(Of DelayedSampleRecord)
    Public DelayTimeBuy As TimeSpan = TimeSpan.FromSeconds(51)
    Public DelayTimeSel As TimeSpan = TimeSpan.FromSeconds(51)

    Public ReadOnly Property IsCallPriceAvailable As Boolean
        Get
            Dim is_call_price_available As Boolean
            SafeEnterTrace(CallPriceKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            is_call_price_available = _IsCallPriceAvailable
            SafeLeaveTrace(CallPriceKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return is_call_price_available
        End Get
    End Property

    Public ReadOnly Property VI As Boolean
        Get
            Dim local_vi As Boolean
            SafeEnterTrace(CallPriceKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            local_vi = _VI
            SafeLeaveTrace(CallPriceKey, 31)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return local_vi
        End Get
    End Property

    Public ReadOnly Property EvidanRate As Integer
        Get
            Dim evidan_rate As Integer
            SafeEnterTrace(CallPriceKey, 110)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            evidan_rate = _EvidanRate
            SafeLeaveTrace(CallPriceKey, 111)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return evidan_rate
        End Get
    End Property

    Public ReadOnly Property MainOperators As Integer
        Get
            Dim sum As Integer = 0
            For index As Integer = 0 To MainDecisionMakerCenter.Count - 1
                Try
                    If MainDecisionMakerCenter(index)(0).StockOperator IsNot Nothing Then
                        sum += 1
                    End If
                Catch ex As Exception
                    '2021.06.01: stock operator count하고 있는 중간에 decision maker center list가 변경되는 경우가 발생했다. 이런 경우 해당 index가 list에 없다는 exception이 뜨는데, 크게 중요하지 않으므로 그냥 count 하지 않고 넘어가기로 한다.
                End Try
            Next

            Return sum
        End Get
    End Property

    Public ReadOnly Property SubOperators As Integer
        Get
            Dim sum As Integer = 0
            For index As Integer = 0 To SubDecisionMakerCenter.Count - 1
                Try
                    If SubDecisionMakerCenter(index)(0).StockOperator IsNot Nothing Then
                        sum += 1
                    End If
                Catch ex As Exception
                    '2021.06.01: stock operator count하고 있는 중간에 decision maker center list가 변경되는 경우가 발생했다. 이런 경우 해당 index가 list에 없다는 exception이 뜨는데, 크게 중요하지 않으므로 그냥 count 하지 않고 넘어가기로 한다.
                End Try
            Next

            Return sum
        End Get
    End Property

    Public ReadOnly Property DelayedPriceBuy As UInt32
        Get
            Dim now_time = Now.TimeOfDay

            SafeEnterTrace(CallPriceKey, 120)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            If DelayedSampleListBuy.Count = 0 Then
                '기록된 샘플이 없다. failsafe 차원에서 현재값을 리턴한다.
                If RecordList.Count = 0 Then
                    '현재값조차 없다 => 0 을 리턴한다.
                    SafeLeaveTrace(CallPriceKey, 125)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return 0
                Else
                    SafeLeaveTrace(CallPriceKey, 124)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return RecordList.Last.Price
                End If
            ElseIf DelayedSampleListBuy.Count = 1 Then
                '기록된 샘플이 하나면 그 샘플의 시간은 중요하지 않다. 시간이 지났건 안 지났건 그샘플의 가격을 리턴한다.
                SafeLeaveTrace(CallPriceKey, 123)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Return DelayedSampleListBuy(0).Price
            Else
                '샘플이 2개보다 많은 경우 아래 루프를 돌린다. 0 이 아닌 1 부터 돌린다.
                For index As Integer = 1 To DelayedSampleListBuy.Count - 1
                    If DelayedSampleListBuy(index).Time + DelayTimeBuy < now_time Then
                        '현샘플 기준으로 시간이 이미 delay 되고도 더 되었다. delay된 직후의 값을 구하기 위해 다음 샘플로 이동한다.
                        Continue For
                    Else
                        '현샘플은 아직 delay time 을 넘어서지 않았다. 바로 직전의 값을 리턴한다.
                        SafeLeaveTrace(CallPriceKey, 122)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        Return DelayedSampleListBuy(index - 1).Price
                    End If
                Next
                '마지막까지 돌렸는데도 delay time을 넘어선 샘플이 없었다. 상당히 오랫동안 변하지 않은 제일 마지막에 기록된 샘플의 값을 리턴한다.
                SafeLeaveTrace(CallPriceKey, 126)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Return DelayedSampleListBuy.Last.Price
            End If
            SafeLeaveTrace(CallPriceKey, 121)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End Get
    End Property

    'Private LPF_coeff = {0.222222222, 0.194444444, 0.166666667, 0.138888889, 0.111111111, 0.083333333, 0.055555556, 0.027777778}
    Private LPF_coeff = {0.040472104, 0.040399094, 0.040310277, 0.040202322, 0.040071249, 0.03991231, 0.039719884, 0.039487357, 0.039207015, 0.038869958, 0.038466056, 0.037983974, 0.037411303, 0.036734843, 0.035941083, 0.035016921, 0.03395066, 0.032733258, 0.031359792, 0.029830977, 0.028154533, 0.026346121, 0.024429557, 0.022436075, 0.02040259, 0.018369105, 0.016375622, 0.014459058, 0.012650647, 0.010974203, 0.009445388, 0.008071922, 0.00685452, 0.005788258, 0.004864097, 0.004070337, 0.003393877, 0.002821206, 0.002339123, 0.001935221, 0.001598164, 0.001317822, 0.001085295, 0.000892869, 0.000733931, 0.000602857, 0.000494903, 0.000406085, 0.000333075, 0.000273103}
    Public ReadOnly Property LPFPriceBuy As UInt32
        Get
            Dim now_time = Now.TimeOfDay

            SafeEnterTrace(CallPriceKey, 140)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            If DelayedSampleListBuy.Count = 0 Then
                '기록된 샘플이 없다. failsafe 차원에서 현재값을 리턴한다.
                If RecordList.Count = 0 Then
                    '현재값조차 없다 => 0 을 리턴한다.
                    SafeLeaveTrace(CallPriceKey, 145)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return 0
                Else
                    SafeLeaveTrace(CallPriceKey, 144)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return RecordList.Last.Price
                End If
            ElseIf DelayedSampleListBuy.Count = 1 Then
                '기록된 샘플이 하나면 그 샘플의 시간은 중요하지 않다. 시간이 지났건 안 지났건 그샘플의 가격을 리턴한다.
                SafeLeaveTrace(CallPriceKey, 143)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Return DelayedSampleListBuy(0).Price
            Else
                '샘플이 2개보다 많은 경우 루프를 돌린다.
                Dim samples(50) As UInt32
                '50개의 샘플을 200ms 간격으로 만들어야 한다.
                samples(0) = DelayedSampleListBuy.Last.Price
                Dim in_ptr As Integer = DelayedSampleListBuy.Count - 1       'DelayedSampleListBuy 의 pointer
                Dim out_ptr As Integer = 1                                   'samples 의 pointer
                Dim current_time_for_sample As TimeSpan = now_time - TimeSpan.FromMilliseconds(200)
                Dim sum As Double = 0
                For out_ptr = 1 To 49
                    While current_time_for_sample < DelayedSampleListBuy(in_ptr).Time
                        in_ptr = in_ptr - 1
                        If in_ptr < 0 Then
                            'Delayed sample 이 부족하다. 이미 만들어진 샘플만으로 LPF 를 구성해야 한다.
                            '너무 많은 고민을 하지 말고 그냥 평균하는 걸로 하자
                            SafeLeaveTrace(CallPriceKey, 142)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

                            For index As Integer = 0 To out_ptr - 1
                                sum = samples(index)
                            Next
                            Return sum / out_ptr
                        End If
                    End While
                    samples(out_ptr) = DelayedSampleListBuy(in_ptr).Price
                Next
                SafeLeaveTrace(CallPriceKey, 146)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

                'LPF 계산
                For index As Integer = 0 To 49
                    sum += LPF_coeff(index) * samples(index)
                Next

                Return sum
            End If
        End Get
    End Property

    Public ReadOnly Property LPFPriceSel As UInt32
        Get
            Dim now_time = Now.TimeOfDay

            SafeEnterTrace(CallPriceKey, 150)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            If DelayedSampleListSel.Count = 0 Then
                '기록된 샘플이 없다. failsafe 차원에서 현재값을 리턴한다.
                If RecordList.Count = 0 Then
                    '현재값조차 없다 => 0 을 리턴한다.
                    SafeLeaveTrace(CallPriceKey, 155)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return 0
                Else
                    SafeLeaveTrace(CallPriceKey, 154)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return RecordList.Last.Price
                End If
            ElseIf DelayedSampleListSel.Count = 1 Then
                '기록된 샘플이 하나면 그 샘플의 시간은 중요하지 않다. 시간이 지났건 안 지났건 그샘플의 가격을 리턴한다.
                SafeLeaveTrace(CallPriceKey, 153)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Return DelayedSampleListSel(0).Price
            Else
                '샘플이 2개보다 많은 경우 루프를 돌린다.
                Dim samples(50) As UInt32
                '50개의 샘플을 200ms 간격으로 만들어야 한다.
                samples(0) = DelayedSampleListSel.Last.Price
                Dim in_ptr As Integer = DelayedSampleListSel.Count - 1       'DelayedSampleListSel 의 pointer
                Dim out_ptr As Integer = 1                                   'samples 의 pointer
                Dim current_time_for_sample As TimeSpan = now_time - TimeSpan.FromMilliseconds(200)
                Dim sum As Double = 0
                For out_ptr = 1 To 49
                    While current_time_for_sample < DelayedSampleListSel(in_ptr).Time
                        in_ptr = in_ptr - 1
                        If in_ptr < 0 Then
                            'Delayed sample 이 부족하다. 이미 만들어진 샘플만으로 LPF 를 구성해야 한다.
                            '너무 많은 고민을 하지 말고 그냥 평균하는 걸로 하자
                            SafeLeaveTrace(CallPriceKey, 152)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

                            For index As Integer = 0 To out_ptr - 1
                                sum = samples(index)
                            Next
                            Return sum / out_ptr
                        End If
                    End While
                    samples(out_ptr) = DelayedSampleListSel(in_ptr).Price
                Next
                SafeLeaveTrace(CallPriceKey, 156)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

                'LPF 계산
                For index As Integer = 0 To 49
                    sum += LPF_coeff(index) * samples(index)
                Next

                Return sum
            End If
        End Get
    End Property

    Public Sub New(ByVal symbol_code As String, ByVal symbol_name As String, ByVal symbol_index As Integer)
        'CodeIndex = code_index
        Code = symbol_code
        If Code.StartsWith("A") Then        '가격정보등 요청하는데 필요한 종목 code
            CodeForRequest = Code.Substring(1)
        Else
            CodeForRequest = Code
        End If

        Name = symbol_name

#If MOVING_AVERAGE_DIFFERENCE Then
        AddHandler evNewMinuteCandleCreated, AddressOf TasksAfterNewCandleCreated
#End If
        'Create each main decision maker list
        For index As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            MainDecisionMakerCenter.Add(New List(Of c05G_DoubleFall))
        Next

        For index As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
#If MOVING_AVERAGE_DIFFERENCE Then
            SubDecisionMakerCenter.Add(New List(Of c05G_MovingAverageDifference))
#Else
            SubDecisionMakerCenter.Add(New List(Of c05E_PatternChecker))
#End If
        Next

        'Create each test decision maker list
        For index As Integer = 0 To TEST_NUMBER_OF_DECIDERS - 1
            TestDecisionMakerCenter.Add(New List(Of c05F_FlexiblePCRenew))
        Next

        SymbolIndex = symbol_index
        Initialize()
    End Sub

    '종목 기록 초기화
    Public Sub Initialize()
        '180804 TODO: decision maker type 통일 필요
        'StartTime = start_time
        '_PriceList.Clear()
        '_AmountList.Clear()
        '_GangdoList.Clear()
        RecordList.Clear()
        _BuyAmountList.Clear()
        _SelAmountList.Clear()
        StartTime = [DateTime].MinValue
#If MOVING_AVERAGE_DIFFERENCE Then
        CandleServiceCenter.Initialize(SymbolIndex)
        AmountAtZero = 0
        DailyAmountStore.Clear()
        OldDate = [DateTime].MinValue
        AmountVar = 0
#End If

        'If StockDecisionMaker IsNot Nothing Then
        'StockDecisionMaker.Clear()
        'End If
        '160316: 실시간 호가 객체 초기화 (객체 만들고 버리고 하지 않고 처음 만든 객체 끝까지 간다)
        '20200408: 마켓종류에 맞는 객체만들고 맞지 않는 건 만들지 않는다. GDI 객체 너무 많아져 Exception 문제 생길 수 있다.
        '20200408: 아니다. 아예 만들지 말고 나중에 필요할 때 만들자.
#If 0 Then
        If MarketKind = MARKET_KIND.MK_KOSPI Then
            _H1_ = New et32_XAReal_Wrapper()
            _H1_.ResFileName = "Res\H1_.res"            '"S2_.res"
            _H1_.SetFieldData("InBlock", "shcode", CodeForRequest)
        Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
            _HA_ = New et32_XAReal_Wrapper()
            _HA_.ResFileName = "Res\HA_.res"            '"KS_.res"
            _HA_.SetFieldData("InBlock", "shcode", CodeForRequest)
        End If
#End If
        Dim monitoring_decision_maker_exist As Boolean
        Dim main_decision_maker_list As List(Of c05G_DoubleFall)
        Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)

        'MAIN
        For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)

            For index As Integer = main_decision_maker_list.Count - 1 To 0 Step -1
                main_decision_maker_list(index).ClearNow(0)
            Next
            main_decision_maker_list.Clear()      '리스트를 아예 클리어
            'StockDecisionMakerList.Add(New c057c_DeltaCurveCollective_DecisionMaker(Me, StartTime))      '새 디시전메이커 시작

            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            monitoring_decision_maker_exist = False
            For index As Integer = 0 To main_decision_maker_list.Count - 1
                If Not main_decision_maker_list(index).IsDone Then
                    monitoring_decision_maker_exist = True
                    Exit For
                End If
            Next
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                'decision_maker_list.Add(New c05E_PatternChecker(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center))
                '2023.11.13 : 이제부터 DoubleFall 전략의 전용계좌를 Main 계좌에서 Test 계좌로 옮기면서 아래 마지막 파라미터를 0에서 2로 바꾸었다.
                main_decision_maker_list.Add(New c05G_DoubleFall(Me, StartTime, index_decision_center, 2))
            End If

            If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                Exit For
            End If
        Next

        If SubAccount Then
            'SUB
            For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                sub_decision_maker_list = SubDecisionMakerCenter(index_decision_center)

                For index As Integer = sub_decision_maker_list.Count - 1 To 0 Step -1
                    sub_decision_maker_list(index).ClearNow(0)
                Next
                sub_decision_maker_list.Clear()      '리스트를 아예 클리어
                'StockDecisionMakerList.Add(New c057c_DeltaCurveCollective_DecisionMaker(Me, StartTime))      '새 디시전메이커 시작

                '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
                monitoring_decision_maker_exist = False
                For index As Integer = 0 To sub_decision_maker_list.Count - 1
                    If Not sub_decision_maker_list(index).IsDone Then
                        monitoring_decision_maker_exist = True
                        Exit For
                    End If
                Next
                If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                    '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                    'decision_maker_list.Add(New c05E_PatternChecker(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center))
#If MOVING_AVERAGE_DIFFERENCE Then
                    sub_decision_maker_list.Add(New c05G_MovingAverageDifference(Me, CandleStartTime + TimeSpan.FromSeconds(RecordList.Count * 5), index_decision_center, 1))
#Else
                main_decision_maker_list.Add(New c05E_PatternChecker(Me, StartTime, index_decision_center))
#End If
                End If

                If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                    Exit For
                End If
            Next
        End If

        Dim test_decision_maker_list As List(Of c05F_FlexiblePCRenew)
        For index_decision_center As Integer = 0 To TEST_NUMBER_OF_DECIDERS - 1
            test_decision_maker_list = TestDecisionMakerCenter(index_decision_center)

            For index As Integer = test_decision_maker_list.Count - 1 To 0 Step -1
                test_decision_maker_list(index).ClearNow(0)
            Next
            test_decision_maker_list.Clear()      '리스트를 아예 클리어
            'StockDecisionMakerList.Add(New c057c_DeltaCurveCollective_DecisionMaker(Me, StartTime))      '새 디시전메이커 시작

            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            monitoring_decision_maker_exist = False
            For index As Integer = 0 To test_decision_maker_list.Count - 1
                If Not test_decision_maker_list(index).IsDone Then
                    monitoring_decision_maker_exist = True
                    Exit For
                End If
            Next
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                ' decision_maker_list.Add(New c05E_PatternChecker(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center + TEST_NUMBER_OF_DECIDERS))
                test_decision_maker_list.Add(New c05F_FlexiblePCRenew(Me, StartTime, index_decision_center, 2))
            End If

            If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                Exit For
            End If
        Next

        '150209: 나머지 StockDecisionMakerList에 대해서도 카피본 완성하자.
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
            If index >= MA_BASE Then
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

    Public Property StartTime() As DateTime
        Get
            Return _StartTime
        End Get
        Set(ByVal value As DateTime)
            _StartTime = value

            Dim main_decision_maker_list As List(Of c05G_DoubleFall)
            For index_decision_center As Integer = 0 To MAIN_NUMBER_OF_DECIDERS - 1
                main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)
                For index As Integer = 0 To main_decision_maker_list.Count - 1
                    main_decision_maker_list(index).StartTime = _StartTime
                Next
                If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                    Exit For
                End If
            Next

            Dim test_decision_maker_list As List(Of c05F_FlexiblePCRenew)
            For index_decision_center As Integer = 0 To TEST_NUMBER_OF_DECIDERS - 1
                test_decision_maker_list = TestDecisionMakerCenter(index_decision_center)
                For index As Integer = 0 To test_decision_maker_list.Count - 1
                    test_decision_maker_list(index).StartTime = _StartTime
                Next
                If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                    Exit For
                End If
            Next
        End Set
    End Property

    Public Property CandleStartTime() As DateTime
        Get
            '190331: 초단위 반올림 로직은 Octan StockSearcher에서 가져온 건데, PriceMinor에 적용하려면 Get 보다는 Set에 적용하는 것이 낫겠다. 왜냐면 기존 pattern 전략들에서 이상동작하는 것을 방지하기 위해.

            '초단위 반올림 로직
            'If _StartTime.Second < 30 Then
            'Return _StartTime.Date + TimeSpan.FromHours(_StartTime.Hour) + TimeSpan.FromMinutes(_StartTime.Minute)
            'Else
            'Return _StartTime.Date + TimeSpan.FromHours(_StartTime.Hour) + TimeSpan.FromMinutes(_StartTime.Minute + 1)
            'End If
            Return _CandleStartTime
        End Get
        Set(ByVal value As DateTime)
            _CandleStartTime = value

#If MOVING_AVERAGE_DIFFERENCE Then
            'Dim main_decision_maker_list As List(Of c05G_MovingAverageDifference)
            Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)
#Else
            Dim main_decision_maker_list As List(Of c05E_PatternChecker)
#End If
#If 0 Then
            'MAIN
            For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)
                For index As Integer = 0 To main_decision_maker_list.Count - 1
                    main_decision_maker_list(index).StartTime = _CandleStartTime
                Next
                If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                    Exit For
                End If
            Next
#End If
            If SubAccount Then
                'SUB
                For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                    sub_decision_maker_list = SubDecisionMakerCenter(index_decision_center)
                    For index As Integer = 0 To sub_decision_maker_list.Count - 1
                        sub_decision_maker_list(index).StartTime = _CandleStartTime
                    Next
                    If (Not MULTIPLE_DECIDER) And index_decision_center = 0 Then
                        Exit For
                    End If
                Next
            End If
        End Set
    End Property

    Public ReadOnly Property LastCallPrices As CallPrices
        Get
            Dim local_call_prices As CallPrices

            SafeEnterTrace(CallPriceKey, 40)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            local_call_prices = _LastCallPrices

#If 0 Then          '결국은 안 쓸 것을...
            'My orders 에 의한 보정
            For index As Integer = 0 To MyOrderNumberList.Count - 1
                If MyEnterExitStateList(index) = EnterOrExit.EOE_Enter Then
                    '매수
                    Select Case MyPriceList(index)
                        Case _LastCallPrices.BuyPrice1
                            If _LastCallPrices.BuyAmount1 >= MyAmountList(index) Then
                                _LastCallPrices.BuyAmount1 = _LastCallPrices.BuyAmount1 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.BuyPrice2
                            If _LastCallPrices.BuyAmount2 >= MyAmountList(index) Then
                                _LastCallPrices.BuyAmount2 = _LastCallPrices.BuyAmount2 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.BuyPrice3
                            If _LastCallPrices.BuyAmount3 >= MyAmountList(index) Then
                                _LastCallPrices.BuyAmount3 = _LastCallPrices.BuyAmount3 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.BuyPrice4
                            If _LastCallPrices.BuyAmount4 >= MyAmountList(index) Then
                                _LastCallPrices.BuyAmount4 = _LastCallPrices.BuyAmount4 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.BuyPrice5
                            If _LastCallPrices.BuyAmount5 >= MyAmountList(index) Then
                                _LastCallPrices.BuyAmount5 = _LastCallPrices.BuyAmount5 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.BuyPrice6
                    End Select
                Else
                    '매도
                    Select Case MyPriceList(index)
                        Case _LastCallPrices.SelPrice1
                            If _LastCallPrices.SelAmount1 >= MyAmountList(index) Then
                                _LastCallPrices.SelAmount1 = _LastCallPrices.SelAmount1 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.SelPrice2
                            If _LastCallPrices.SelAmount2 >= MyAmountList(index) Then
                                _LastCallPrices.SelAmount2 = _LastCallPrices.SelAmount2 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.SelPrice3
                            If _LastCallPrices.SelAmount3 >= MyAmountList(index) Then
                                _LastCallPrices.SelAmount3 = _LastCallPrices.SelAmount3 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.SelPrice4
                            If _LastCallPrices.SelAmount4 >= MyAmountList(index) Then
                                _LastCallPrices.SelAmount4 = _LastCallPrices.SelAmount4 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.SelPrice5
                            If _LastCallPrices.SelAmount5 >= MyAmountList(index) Then
                                _LastCallPrices.SelAmount5 = _LastCallPrices.SelAmount5 - MyAmountList(index)
                            End If
                        Case _LastCallPrices.SelPrice6
                    End Select
                End If
            Next
#End If
            SafeLeaveTrace(CallPriceKey, 41)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return local_call_prices
        End Get
    End Property

    Public Sub RegisterMyOrder(ByVal enter_or_exit As EnterOrExit, ByVal account_manager As et1_AccountManager, ByVal my_order_number As String, ByVal my_price As UInt32, ByVal my_amount As UInt32)
        SafeEnterTrace(CallPriceKey, 50)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        MyEnterExitStateList.Add(enter_or_exit)
        MyAccountList.Add(account_manager)
        MyOrderNumberList.Add(my_order_number)
        MyPriceList.Add(my_price)
        MyAmountList.Add(my_amount)
        SafeLeaveTrace(CallPriceKey, 51)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

#If 0 Then
    '20200327: 왜 만들었는지 기억이 잘 안 난다. 당장 필요없어서 comment 처리한다.
    Public Sub UpdateMyOrder(ByVal my_account_manager As et1_AccountManager, ByVal my_order_number As ULong, ByVal my_rest_amount As UInt32)
        SafeEnterTrace(CallPriceKey, 60)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        For index As Integer = 0 To MyOrderNumberList.Count - 1
            If my_account_manager Is MyAccountList(index) AndAlso my_order_number = MyOrderNumberList(index) Then
                If my_rest_amount = 0 Then
                    '끝났으니 없애자
                    MyEnterExitStateList.RemoveAt(index)
                    MyAccountList.RemoveAt(index)
                    MyOrderNumberList.RemoveAt(index)
                    MyPriceList.RemoveAt(index)
                    MyAmountList.RemoveAt(index)
                Else
                    '아직 남았으니 업데이트 하자
                    MyAmountList(index) = my_rest_amount
                End If
                SafeLeaveTrace(CallPriceKey, 61)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Exit Sub
            End If
        Next
        ErrorLogging("A" & CodeForRequest & " :" & "등록한 주문번호랑 계좌가 없는데 뭘 업데이트하라는 거지..")
        SafeLeaveTrace(CallPriceKey, 62)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
#End If


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

    Public Sub RequestPriceinfo()
        '아래는 테스트할 환경이 만들어질때까지는 열지 않는 것이 좋겠다.
        '131203 : T1101 request하고 실시간 가격 request하는 것까지 진행해보자. 단지 Operation 객체에서 복사해오면 된다.
        SafeEnterTrace(PriceRealMonitoringKey, 60)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
#If 1 Then
        PriceRealMonitoring = PriceRealMonitoring + 1
        MessageLogging(Code & " 호가등상태 호가 start 요청 " & PriceRealMonitoring.ToString & "번째")
        If PriceRealMonitoring = 1 Then
            '180226: 근냥 가격정보 요청할 때 같이 증거금률 요청하는 것으로 하자.
            '20200421: EvidanRate 요청 쫑나는 에러나서 바깥(이함수들어오자마자위치)에서 이 안쪽으로 옮겨왔다.
            If Not EvidanComplete AndAlso _T1102 Is Nothing Then        '20200421: is nothing 조건을 추가했다.
                RequestEvidanRate()
            End If
            '가격조회TR 초기화
            If _T1101 Is Nothing Then
                _T1101 = et31_XAQuery_Wrapper.NewOrUsed()
            End If
            _T1101.ResFileName = "Res\t1101.res"
            _T1101.SetFieldData("t1101InBlock", "shcode", 0, CodeForRequest)

            ' 데이터 요청
            If _T1101.Request(False) = False Then
                RequestStatus = TypeRequestState.RS_ERROR
                ErrorLogging("A" & CodeForRequest & " :" & "대기시간 종료 후 호가요청 Fail")
            Else
                RequestStatus = TypeRequestState.RS_CHECKINGPRICE_REQUESTED
                'MessageLogging("A" & CodeForRequest & " :" & "첫번째 호가요청 성공")
            End If
        End If
#End If
        SafeLeaveTrace(PriceRealMonitoringKey, 61)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub RequestEvidanRate()
        'TR 1102 초기화
        _T1102 = et31_XAQuery_Wrapper.NewOrUsed()
        _T1102.ResFileName = "Res\t1102.res"
        _T1102.SetFieldData("t1102InBlock", "shcode", 0, CodeForRequest)

        ' 데이터 요청
        If _T1102.Request(False) = False Then
            '증거금률 가져오기 실패. 디폴트 100 셋
            '_EvidanRate = 100      '20200614: 어차피 기본 100으로 되어 있으니 따로 세팅할 필요 없다.
            ErrorLogging("A" & CodeForRequest & " :" & "증거금률 request 실패. 디폴트 100 셋")
            EvidanComplete = True
        Else
            '증거금률 요청됨
        End If
    End Sub

    Public Sub StopPriceinfo()
        '140128 : 여러개의 decision maker를 운영하는 것이 쉬운지 어려운지 검토해보자. 어렵지 않다면 PriceRealMonitoring을 integer로 해서 자원관리하는 쪽으로 해보자. (가격정보 구독받는 decision maker마다 increase하는 방식.)
        '140129 : decision maker가 소멸될 때 이 것을 콜하도록 만들어보자.
        SafeEnterTrace(PriceRealMonitoringKey, 70)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        PriceRealMonitoring = PriceRealMonitoring - 1
        MessageLogging(Code & " 호가등상태 호가 stop 요청 " & PriceRealMonitoring.ToString & "번째")
        If PriceRealMonitoring < 0 Then
            PriceRealMonitoring = 0
        End If
        If PriceRealMonitoring <= 0 Then
            If _H1_ IsNot Nothing Then
                _H1_.UnAdviseRealData()
                _H1_.ReleaseCOM()
                _H1_ = Nothing
            End If
            If _HA_ IsNot Nothing Then
                _HA_.UnAdviseRealData()
                _HA_.ReleaseCOM()
                _HA_ = Nothing
            End If
            _IsCallPriceAvailable = False
        End If
        SafeLeaveTrace(PriceRealMonitoringKey, 71)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub RestartPriceMonitoring()
        SafeEnterTrace(PriceRealMonitoringKey, 80)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If PriceRealMonitoring > 0 Then
            If MarketKind = MARKET_KIND.MK_KOSPI Then
                If _H1_ Is Nothing Then
                    _H1_ = New et32_XAReal_Wrapper
                    _H1_.ResFileName = "Res\H1_.res"
                    _H1_.SetFieldData("InBlock", "shcode", CodeForRequest)
                End If
                _H1_.AdviseRealData()           '데이터 요청
            Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
                If _HA_ Is Nothing Then
                    _HA_ = New et32_XAReal_Wrapper
                    _HA_.ResFileName = "Res\HA_.res"
                    _HA_.SetFieldData("InBlock", "shcode", CodeForRequest)
                End If
                _HA_.AdviseRealData()           '데이터 요청
            End If
            MessageLogging(Code & " 호가등상태 실시간 모니터링 재시작 요청되었음")
        End If

        SafeLeaveTrace(PriceRealMonitoringKey, 81)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Private Sub _T1101_ReceiveData(ByVal szTrCode As String) Handles _T1101.ReceiveData
        Dim post_thread As Threading.Thread = New Threading.Thread(AddressOf _T1101_ReceiveData_PostThread)
        post_thread.IsBackground = True
        post_thread.Start()
    End Sub

    Private Sub _T1101_ReceiveData_PostThread()
        'SafeEnterTrace(CallPriceKey, 80)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'If RequestStatus = TypeRequestState.RS_CHECKINGPRICE_REQUESTED Then
        Dim temp_data As String
        temp_data = _T1101.GetFieldData("t1101OutBlock", "offerho1", 0)
        If temp_data = "" Then
            temp_data = _T1101.GetFieldData("t1101OutBlock", "bidho1", 0)
            temp_data = temp_data & " " & _T1101.GetFieldData("t1101OutBlock", "dnlmtprice", 0)             '하한가 추출
            ErrorLogging("A" & CodeForRequest & " :" & "T1101 공백반환 " & temp_data)

            '실시간 호가 초기화 invoke
            If MarketKind = MARKET_KIND.MK_KOSPI Then
                If _H1_ Is Nothing Then
                    _H1_ = New et32_XAReal_Wrapper
                    _H1_.ResFileName = "Res\H1_.res"
                    _H1_.SetFieldData("InBlock", "shcode", CodeForRequest)
                End If
                _H1_.AdviseRealData()           '데이터 요청
            Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
                If _HA_ Is Nothing Then
                    _HA_ = New et32_XAReal_Wrapper
                    _HA_.ResFileName = "Res\HA_.res"
                    _HA_.SetFieldData("InBlock", "shcode", CodeForRequest)
                End If
                _HA_.AdviseRealData()           '데이터 요청
            End If

            MessageLogging(Code & " 호가등상태 실시간 호가요청되었음")
            'SafeLeaveTrace(CallPriceKey, 81)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return
        End If

        Dim call_prices As CallPrices
        call_prices.SelPrice1 = Convert.ToUInt32(temp_data)
        call_prices.SelPrice2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho2", 0))
        call_prices.SelPrice3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho3", 0))
        call_prices.SelPrice4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho4", 0))
        call_prices.SelPrice5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho5", 0))
        call_prices.SelPrice6 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho6", 0))
        call_prices.SelAmount1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem1", 0))
        call_prices.SelAmount2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem2", 0))
        call_prices.SelAmount3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem3", 0))
        call_prices.SelAmount4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem4", 0))
        call_prices.SelAmount5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem5", 0))
        temp_data = _T1101.GetFieldData("t1101OutBlock", "bidho1", 0)
        call_prices.BuyPrice1 = Convert.ToUInt32(temp_data)
        call_prices.BuyPrice2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho2", 0))
        call_prices.BuyPrice3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho3", 0))
        call_prices.BuyPrice4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho4", 0))
        call_prices.BuyPrice5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho5", 0))
        call_prices.BuyPrice6 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho6", 0))
        call_prices.BuyAmount1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem1", 0))
        call_prices.BuyAmount2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem2", 0))
        call_prices.BuyAmount3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem3", 0))
        call_prices.BuyAmount4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem4", 0))
        call_prices.BuyAmount5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem5", 0))
        '
        LowLimitPrice = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "dnlmtprice", 0))             '하한가 추출
        HighLimitPrice = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "uplmtprice", 0))             '상한가 추출
        '            Price = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "price", 0))                          '현재가 추출
        '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice5.ToString & "   " & _LastCallPrices.SelAmount5.ToString)
        '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice4.ToString & "   " & _LastCallPrices.SelAmount4.ToString)
        '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice3.ToString & "   " & _LastCallPrices.SelAmount3.ToString)
        '            MessageLogging("A" & CodeForRequest & " :" & _LastCallPrices.SelPrice2.ToString) ' & "   " & _LastCallPrices.SelAmount2.ToString)
        MessageLogging(Code & " 호가등상태 매도1호가 " & call_prices.SelPrice1.ToString) ' & "   " & _LastCallPrices.SelAmount1.ToString)
        MessageLogging(Code & " 호가등상태 매수1호가 " & call_prices.BuyPrice1.ToString) ' & "   " & _LastCallPrices.BuyAmount1.ToString)
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
                _H1_ = New et32_XAReal_Wrapper
                _H1_.ResFileName = "Res\H1_.res"
                _H1_.SetFieldData("InBlock", "shcode", CodeForRequest)
            End If
            _H1_.AdviseRealData()           '데이터 요청
        Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
            If _HA_ Is Nothing Then
                _HA_ = New et32_XAReal_Wrapper
                _HA_.ResFileName = "Res\HA_.res"
                _HA_.SetFieldData("InBlock", "shcode", CodeForRequest)
            End If
            _HA_.AdviseRealData()           '데이터 요청
        End If

        MessageLogging(Code & " 호가등상태 실시간 호가요청되었음")
        'End If

        Dim local_vi As Boolean
        If call_prices.BuyAmount4 = 0 AndAlso call_prices.BuyAmount5 = 0 AndAlso call_prices.SelAmount4 = 0 AndAlso call_prices.SelAmount5 = 0 Then
            local_vi = True
            'VI 상태에서는 풀릴 때까지 기다려야 한다.
            MessageLogging(Code & " 호가등상태 호가를 받아보니 VI 상태네.")
        Else
            local_vi = False
            '호가 데이터를 기다리는 작업(매수시도지 뭐)들을 불러준다.
            'SymTree.SymbolTaskToQueue(Me)
        End If

        SymTree.SymbolTaskToQueue(Me)   '2024.01.09 : symbol task 를 이제 매번 호가변화마다 불러주도록 하자.

        Dim delayed_sample_buy As DelayedSampleRecord
        delayed_sample_buy.Price = call_prices.BuyPrice1
        delayed_sample_buy.Time = Now.TimeOfDay
        Dim delayed_sample_sel As DelayedSampleRecord
        delayed_sample_sel.Price = call_prices.SelPrice1
        delayed_sample_sel.Time = Now.TimeOfDay
        SafeEnterTrace(CallPriceKey, 80)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        _LastCallPrices = call_prices
        _VI = local_vi
        _IsCallPriceAvailable = True

        '2024.07.07 : DelayedSampleList 구현
        DelayedSampleListBuy.Add(delayed_sample_buy)
        While DelayedSampleListBuy.Count > 1 AndAlso DelayedSampleListBuy(1).Time + DelayTimeBuy < delayed_sample_buy.Time
            '샘플이 두 개 이상은 있어야 한다. 한 개 있으면 지우면 안 된다. DelayTime 을 갓 넘어선 녀석의 price 를 봐야 하기 때문에 끝에서 두번째 샘플의 시간을 조사한다.
#If 0 Then
            '지우기 전에 기록해서 비주얼라이즈해본다.
            If TestDecisionMakerCenter.Count > 0 AndAlso TestDecisionMakerCenter(0).Count > 0 AndAlso TestDecisionMakerCenter(0)(0).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count = 0 AndAlso OnlyAFewChanceLeft > 0 Then
                    'TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds, DelayedSampleListBuy(0).Price))
                    'DelayedBuy Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds + 1, DelayedSampleListBuy(0).Price * 0.98))
                    OnlyAFewChanceLeft = OnlyAFewChanceLeft - 1
                End If
                If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count > 0 Then
                    'TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds, DelayedSampleListBuy(0).Price))
                    'DelayedBuy Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds + 1, DelayedSampleListBuy(0).Price * 0.98))
                End If
            End If
#End If
            DelayedSampleListBuy.RemoveAt(0)
        End While

        DelayedSampleListSel.Add(delayed_sample_sel)
        While DelayedSampleListSel.Count > 1 AndAlso DelayedSampleListSel(1).Time + DelayTimeSel < delayed_sample_sel.Time
            '샘플이 두 개 이상은 있어야 한다. 한 개 있으면 지우면 안 된다. DelayTime 을 갓 넘어선 녀석의 price 를 봐야 하기 때문에 끝에서 두번째 샘플의 시간을 조사한다.
            '지우기 전에 기록해서 비주얼라이즈해본다.
#If 0 Then
            If TestDecisionMakerCenter.Count > 0 AndAlso TestDecisionMakerCenter(0).Count > 0 AndAlso TestDecisionMakerCenter(0)(0).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                If TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Count = 0 AndAlso OnlyAFewChanceLeft > 0 Then
                    'DelayedSel Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Add(New PointF(DelayedSampleListSel(0).Time.TotalSeconds + 1, DelayedSampleListSel(0).Price * 1.014))
                    OnlyAFewChanceLeft = OnlyAFewChanceLeft - 1
                End If
                If TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Count > 0 Then
                    'DelayedSel Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Add(New PointF(DelayedSampleListSel(0).Time.TotalSeconds + 1, DelayedSampleListSel(0).Price * 1.014))
                End If
            End If
#End If
            DelayedSampleListSel.RemoveAt(0)
        End While
#If 0 Then
        'Time integral 계산
        Dim price_integral_wrt_time As Double = DelayedSampleListBuy(0).Price
        For index As Integer = 1 To DelayedSampleListBuy.Count - 1
            price_integral_wrt_time = price_integral_wrt_time + ((CType(DelayedSampleListBuy(index).Price, Double) - DelayedSampleListBuy(index - 1).Price)) * (DelayedSampleListBuy(index).Time.Seconds - DelayedSampleListBuy(index - 1).Time.Seconds) / CATCH_SECONDS
            'CATCH_SECONDS 는 실제 price 를 얼마나 빨리 따라가는지를 나타냄. 길면 길수록 늦게 따라감. LOW PASS FILTER 의 효과.
        Next
        If TestDecisionMakerCenter.Count > 1 AndAlso TestDecisionMakerCenter(0).Count > 1 AndAlso TestDecisionMakerCenter(0)(0).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
            If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count = 0 AndAlso OnlyOneChangeLeft Then
                TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(delayed_sample.Time.TotalSeconds, price_integral_wrt_time))
                OnlyOneChangeLeft = False
            End If
            If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count > 0 Then
                TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(delayed_sample.Time.TotalSeconds, price_integral_wrt_time))
            End If
        End If
#End If
        SafeLeaveTrace(CallPriceKey, 82)       'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub SymbolTask()
        Dim local_call_price As CallPrices = LastCallPrices

        SafeEnterTrace(OneKey, 60)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐

        '2024.01.08 : EXITING_PRETHRESHOLD 상태에서 WAIT_FALLING 으로 돌아가는 작업을 여기서 해준다.
        For index1 As Integer = 0 To MainDecisionMakerCenter.Count - 1
            For index2 As Integer = 0 To MainDecisionMakerCenter(index1).Count - 1
                If MainDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.EXITING_PRETHRESHOLD AndAlso MainDecisionMakerCenter(index1)(index2).IsStopBuyingCompleted Then
                    If MainDecisionMakerCenter(index1)(index2).SecondFallWaitingCount = 0 Then
                        '2024.02.04 : second fall 기다리다가 횟수 다 돼서 count 를 이미 0으로 초기화시킨 후 들어온 것이다. => WAIT_FALLING 으로 보낸다.
                        MessageLogging(Code & " (DoubleFall) EXITING_PRETHRESHOLD => WAIT_FALLING")
                        MainDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING
                    Else
                        '2024.02.04 : second fall 을 계속 기다릴 수 있는 상황이다 => WAIT_SECONDFALL 로 간다.
                        MessageLogging(Code & " (DoubleFall) EXITING_PRETHRESHOLD => WAIT_SECONDFALL")
                        MainDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_SECONDFALL
                    End If
                    StopPriceinfo()             '종목에 대해 가격정보 그만요청
                End If
            Next
        Next
        For index1 As Integer = 0 To TestDecisionMakerCenter.Count - 1
            For index2 As Integer = 0 To TestDecisionMakerCenter(index1).Count - 1
                If TestDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.EXITING_PRETHRESHOLD AndAlso TestDecisionMakerCenter(index1)(index2).IsStopBuyingCompleted Then
                    MessageLogging(Code & " (PCRenew) EXITING_PRETHRESHOLD => WAIT_FALLING")
                    TestDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING
                    StopPriceinfo()             '종목에 대해 가격정보 그만요청
                End If
            Next
        Next

        '호가가 available해지기를 그리고 VI가 풀리기를 기다리는 매수할 녀석들을 부른다.
        'MAIN
#If 0 Then
        For index1 As Integer = 0 To MainDecisionMakerCenter.Count - 1
            For index2 As Integer = 0 To MainDecisionMakerCenter(index1).Count - 1
                If MainDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                    If MainDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.WAIT_UNLOCK Then
                        'VI가 처음 풀린 것이다. EnterPrice 업데이트 해야 된다. => main 에서는 하지말자. EnterPrice 엄청 높게 사게 된다.
                        'MainDecisionMakerCenter(index1)(index2).EnterPrice = MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, MarketKind)     'BuyPrice1 과 SelPrice1의 중가값으로 enterpice를 정한다.
                    End If
                    MainDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.UNLOCKED
                    'TargetBuyPrice 설정
                    'MainDecisionMakerCenter(index1)(index2).TargetBuyPrice = Math.Max(MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, MarketKind), MainDecisionMakerCenter(index1)(index2).EnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                    MainDecisionMakerCenter(index1)(index2).TargetBuyPrice = MainDecisionMakerCenter(index1)(index2).LastEnterPrice 'Moving average 전략은 1분마다 매매타임이기 때문에 BuyPrice1이 예상치 못하게 높을 수 있다. 그래서 target price는 그냥 EnterPrice로 정한다.
                    If IsGoodTimeToBuy(Now) AndAlso (Not Caution) AndAlso (Not Supervision) AndAlso (Not MainDecisionMakerCenter(index1)(index2).YieldForHighLevel) Then    'MainDecisionMakerCenter(index1)(index2).EnterTime 에서 now로 바뀌었다. enter time이 안 좋은 시간대에 있어도 그 시간대 벗어나면 산다.
                        MainForm.MainAccountManager.DecisionRegister(MainDecisionMakerCenter(index1)(index2))
                    End If
                End If
            Next
        Next
#End If
        '2023.11.13 : 이제부터 DoubleFall 전략도 Test 계좌를 사용하기로 하면서부터 MainDecisionMakerCenter 의 전용계좌는 Main 이 아니라 Test 로 바꾼다.
        Dim test_account_impacted As Boolean = False
        For index1 As Integer = 0 To MainDecisionMakerCenter.Count - 1
            For index2 As Integer = 0 To MainDecisionMakerCenter(index1).Count - 1
                If MainDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse
                    MainDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                    test_account_impacted = True
                    MainDecisionMakerCenter(index1)(index2).TargetBuyPrice = MainDecisionMakerCenter(index1)(index2).LastEnterPrice
                    MainDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.UNLOCKED
                    If Not VI Then
                        '20210927: 이제부터 test계좌에서 VI 인 것은 매수시도조차 하지 않도록 한다.
                        MainForm.TestAccountManager.DecisionRegister(MainDecisionMakerCenter(index1)(index2))
                    End If
                End If
            Next
        Next
        If SubAccount Then
            'SUB
            For index1 As Integer = 0 To SubDecisionMakerCenter.Count - 1
                For index2 As Integer = 0 To SubDecisionMakerCenter(index1).Count - 1
                    If SubDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                        If SubDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.WAIT_UNLOCK Then
                            'VI가 처음 풀린 것이다. EnterPrice 업데이트 해야 된다. => sub 에서는 하지말자. EnterPrice 엄청 높게 사게 된다.
                            'SubDecisionMakerCenter(index1)(index2).EnterPrice = MainForm.SubAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, MarketKind)     'BuyPrice1 과 SelPrice1의 중가값으로 enterpice를 정한다.
                        End If
                        SubDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.UNLOCKED
                        'TargetBuyPrice 설정
                        'SubDecisionMakerCenter(index1)(index2).TargetBuyPrice = Math.Max(MainForm.SubAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, MarketKind), SubDecisionMakerCenter(index1)(index2).EnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                        SubDecisionMakerCenter(index1)(index2).TargetBuyPrice = SubDecisionMakerCenter(index1)(index2).LastEnterPrice 'Moving average 전략은 1분마다 매매타임이기 때문에 BuyPrice1이 예상치 못하게 높을 수 있다. 그래서 target price는 그냥 EnterPrice로 정한다.
                        If (Not Caution) AndAlso (Not Supervision) AndAlso (Not SubDecisionMakerCenter(index1)(index2).YieldForHighLevel) Then    'SubDecisionMakerCenter(index1)(index2).EnterTime 에서 now로 바뀌었다. enter time이 안 좋은 시간대에 있어도 그 시간대 벗어나면 산다.
                            MainForm.SubAccountManager.DecisionRegister(SubDecisionMakerCenter(index1)(index2))
                        End If
                    End If
                Next
            Next
        End If
        'TEST
        For index1 As Integer = 0 To TestDecisionMakerCenter.Count - 1
            For index2 As Integer = 0 To TestDecisionMakerCenter(index1).Count - 1
                If TestDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse
                    TestDecisionMakerCenter(index1)(index2)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                    test_account_impacted = True
                    'If TestDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.WAIT_UNLOCK Then
                    If (Now.TimeOfDay - TestDecisionMakerCenter(index1)(index2).EnterTime.TimeOfDay).TotalSeconds > 7 Then
                        'VI가 처음 풀린 것이다. EnterPrice 업데이트 해야 된다. => 2021.06.21: test 에서도 하지말자. EnterPrice 엄청 높게 사게 된다.
                        'TestDecisionMakerCenter(index1)(index2).LastEnterPrice = MainForm.TestAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, MarketKind)     'BuyPrice1 과 SelPrice1의 중가값으로 enterpice를 정한다.
                        'TargetBuyPrice 설정
                        TestDecisionMakerCenter(index1)(index2).TargetBuyPrice = TestDecisionMakerCenter(index1)(index2).LastEnterPrice  '2021.06.21: 위 방식의 장점을 모르겠다. main, sub와 같은 방식으로 간다.
                    Else
                        'TargetBuyPrice 설정
                        '2021.07.06: TargetBuyPrice 설정법 여러번 바꾼다. 이번에는 VI 처음 풀린 case와 아닌 case를 나눠서 설정하도록 했다. 이게 최선이겠지. 그리고 마지막이겠지..
                        '2021.07.06: 안타깝게도 마지막이 아니었고, 고심끝에 내놓은 해결책은 VI고 나발이고 간에 진입시점에서 가까우면 호가를 이용하고, 진입하고 너무 오랜 시간이 지났으면
                        '2021.07.06: (여기서는 7초 이상) 호가는 사용하지 않고 EnterPrice를 사용하도록 한다. 아마 추후 VI_CheckStatus는 사용처가 없어저 삭제될 지도 모르겠다.
                        'TestDecisionMakerCenter(index1)(index2).TargetBuyPrice = Math.Max(MainForm.TestAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, MarketKind), TestDecisionMakerCenter(index1)(index2).LastEnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                        'TestDecisionMakerCenter(index1)(index2).TargetBuyPrice = Math.Max(NextCallPrice(local_call_price.BuyPrice1, 1), TestDecisionMakerCenter(index1)(index2).LastEnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                        '2023.12.28 : 7초 미만에 사용하던 매수1호가 매도1호가 중간값은 prethreshold 적용한 현시점부터 이제 안 쓰기로 한다. TargetBuyprice 에 대해 왜 이렇게 고민을 했었는지 잘 모르겠지만, 이제부터는 그냥 TargetBuyPrice 는 LastEnterPrice 이다.
                        TestDecisionMakerCenter(index1)(index2).TargetBuyPrice = TestDecisionMakerCenter(index1)(index2).LastEnterPrice
                    End If
                    TestDecisionMakerCenter(index1)(index2).VI_CheckStatus = c050_DecisionMaker.VI_CheckStatusType.UNLOCKED
                    If Not VI Then
                        '20210927: 이제부터 test계좌에서 VI 인 것은 매수시도조차 하지 않도록 한다.
                        MainForm.TestAccountManager.DecisionRegister(TestDecisionMakerCenter(index1)(index2))
                    End If
                End If
            Next
        Next

        '2024.01.08 : 호가 모니터링하는 모든 종목들의 호가가 수시로 바뀌는데 그 중 test 계좌에서 모니터링하는 종목만 추출하여 test 계좌 task 를 불러주도록 한다.
        If test_account_impacted Then
            SymTree.AccountTaskToQueue(MainForm.TestAccountManager)
        End If

        SafeLeaveTrace(OneKey, 62)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub


    Private Sub _T1102_ReceiveData(ByVal szTrCode As String) Handles _T1102.ReceiveData
        Dim post_thread = New Threading.Thread(AddressOf _T1102_ReceiveData_PostThread)
        _T1102_ReceiveRealData_Thread = Threading.Thread.CurrentThread
        post_thread.Start()
    End Sub

    Public Sub _T1102_ReceiveData_PostThread()
        While CALLBACK_FAST_RETURN_WAIT AndAlso _T1102_ReceiveRealData_Thread IsNot Nothing AndAlso _T1102_ReceiveRealData_Thread.ThreadState <> Threading.ThreadState.Running
            Threading.Thread.Yield()
        End While

        Dim temp_data As String

        temp_data = _T1102.GetFieldData("t1102OutBlock", "jkrate", 0)

        Dim evidan_rate As Integer
        Try
            evidan_rate = Convert.ToInt32(temp_data)
        Catch ex As Exception
            evidan_rate = 100
        End Try
        MessageLogging(Code & " 호가등상태 증거금률 수신됨 : " & evidan_rate)
        Dim evidan_complete As Boolean
        evidan_complete = True

        'T1102 객체는 더 이상 안 쓰므로 해방시킴
        _T1102.ReleaseCOM()
        _T1102 = Nothing
        SafeEnterTrace(CallPriceKey, 100)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        _EvidanRate = evidan_rate
        EvidanComplete = evidan_complete
        SafeLeaveTrace(CallPriceKey, 101)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '호가 받음
    Private Sub _HX_ReceiveData(ByVal szTrCode As String) Handles _H1_.ReceiveRealData, _HA_.ReceiveRealData
        'If PriceRealMonitoring > 0 Then
        Dim call_prices As CallPrices
        Dim local_h1 As et32_XAReal_Wrapper = _H1_
        Dim local_ha As et32_XAReal_Wrapper = _HA_

        Try
            If MarketKind = MARKET_KIND.MK_KOSPI Then
                _PriceUpdateCount = _PriceUpdateCount + 1
                call_prices.SelPrice1 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerho1"))
                call_prices.SelPrice2 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerho2"))
                call_prices.SelPrice3 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerho3"))
                call_prices.SelPrice4 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerho4"))
                call_prices.SelPrice5 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerho5"))
                call_prices.SelPrice6 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerho6"))
                '            _LastCallPrices.SelPrice7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho7"))
                '            _LastCallPrices.SelPrice8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho8"))
                '            _LastCallPrices.SelPrice9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho9"))
                '            _LastCallPrices.SelPrice10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho10"))
                call_prices.SelAmount1 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerrem1"))
                call_prices.SelAmount2 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerrem2"))
                call_prices.SelAmount3 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerrem3"))
                call_prices.SelAmount4 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerrem4"))
                call_prices.SelAmount5 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "offerrem5"))
                '            _LastCallPrices.SelAmount6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem6"))
                '            _LastCallPrices.SelAmount7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem7"))
                '            _LastCallPrices.SelAmount8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem8"))
                '            _LastCallPrices.SelAmount9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem9"))
                '            _LastCallPrices.SelAmount10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem10"))
                call_prices.BuyPrice1 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidho1"))
                call_prices.BuyPrice2 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidho2"))
                call_prices.BuyPrice3 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidho3"))
                call_prices.BuyPrice4 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidho4"))
                call_prices.BuyPrice5 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidho5"))
                call_prices.BuyPrice6 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidho6"))
                '            _LastCallPrices.BuyPrice7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho7"))
                '            _LastCallPrices.BuyPrice8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho8"))
                '            _LastCallPrices.BuyPrice9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho9"))
                '            _LastCallPrices.BuyPrice10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho10"))
                call_prices.BuyAmount1 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidrem1"))
                call_prices.BuyAmount2 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidrem2"))
                call_prices.BuyAmount3 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidrem3"))
                call_prices.BuyAmount4 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidrem4"))
                call_prices.BuyAmount5 = Convert.ToUInt32(local_h1.GetFieldData("OutBlock", "bidrem5"))
                '            _LastCallPrices.BuyAmount6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem6"))
                '            _LastCallPrices.BuyAmount7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem7"))
                '            _LastCallPrices.BuyAmount8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem8"))
                '            _LastCallPrices.BuyAmount9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem9"))
                '            _LastCallPrices.BuyAmount10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem10"))
            Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
                _PriceUpdateCount = _PriceUpdateCount + 1
                call_prices.SelPrice1 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerho1"))
                call_prices.SelPrice2 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerho2"))
                call_prices.SelPrice3 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerho3"))
                call_prices.SelPrice4 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerho4"))
                call_prices.SelPrice5 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerho5"))
                call_prices.SelPrice6 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerho6"))
                '            _LastCallPrices.SelPrice7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho7"))
                '            _LastCallPrices.SelPrice8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho8"))
                '            _LastCallPrices.SelPrice9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho9"))
                '            _LastCallPrices.SelPrice10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho10"))
                call_prices.SelAmount1 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerrem1"))
                call_prices.SelAmount2 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerrem2"))
                call_prices.SelAmount3 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerrem3"))
                call_prices.SelAmount4 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerrem4"))
                call_prices.SelAmount5 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "offerrem5"))
                '            _LastCallPrices.SelAmount6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem6"))
                '            _LastCallPrices.SelAmount7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem7"))
                '            _LastCallPrices.SelAmount8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem8"))
                '            _LastCallPrices.SelAmount9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem9"))
                '            _LastCallPrices.SelAmount10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem10"))
                call_prices.BuyPrice1 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidho1"))
                call_prices.BuyPrice2 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidho2"))
                call_prices.BuyPrice3 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidho3"))
                call_prices.BuyPrice4 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidho4"))
                call_prices.BuyPrice5 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidho5"))
                call_prices.BuyPrice6 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidho6"))
                '            _LastCallPrices.BuyPrice7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho7"))
                '            _LastCallPrices.BuyPrice8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho8"))
                '            _LastCallPrices.BuyPrice9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho9"))
                '            _LastCallPrices.BuyPrice10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho10"))
                call_prices.BuyAmount1 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidrem1"))
                call_prices.BuyAmount2 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidrem2"))
                call_prices.BuyAmount3 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidrem3"))
                call_prices.BuyAmount4 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidrem4"))
                call_prices.BuyAmount5 = Convert.ToUInt32(local_ha.GetFieldData("OutBlock", "bidrem5"))
                '            _LastCallPrices.BuyAmount6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem6"))
                '            _LastCallPrices.BuyAmount7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem7"))
                '            _LastCallPrices.BuyAmount8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem8"))
                '            _LastCallPrices.BuyAmount9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem9"))
                '            _LastCallPrices.BuyAmount10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem10"))
            End If
        Catch ex As Exception
            '2021.10.20: 여기를 실행하고 있던 중 다른 곳에서 _H1_ 이 nothing으로 바뀌는 경우가 있는 것 같다. (실시간 호가 모니터링 종료) 이런 경우 어차피 호가정보는 무의미해지므로,
            '2021.10.20: 이로 인해 익셉션 발생시 바로 빠져나오도록 한다.

        End Try
        'End If

        Dim local_vi As Boolean = _VI
        If call_prices.BuyAmount4 = 0 AndAlso call_prices.BuyAmount5 = 0 AndAlso call_prices.SelAmount4 = 0 AndAlso call_prices.SelAmount5 = 0 Then
            local_vi = True
        Else
            If local_vi = True Then
                local_vi = False
                MessageLogging(Code & " 호가등상태 VI가 풀린 것을 축하하자")
                'VI 가 풀린 것을 축하하자
                'SymTree.SymbolTaskToQueue(Me)       ''2024.01.09 : symbol task 를 이제 매번 호가변화마다 불러주도록 하자.
            End If
        End If

        SymTree.SymbolTaskToQueue(Me)       ''2024.01.09 : symbol task 를 이제 매번 호가변화마다 불러주도록 하자.

        Dim delayed_sample_buy As DelayedSampleRecord
        delayed_sample_buy.Price = call_prices.BuyPrice1
        delayed_sample_buy.Time = Now.TimeOfDay
        Dim delayed_sample_sel As DelayedSampleRecord
        delayed_sample_sel.Price = call_prices.SelPrice1
        delayed_sample_sel.Time = Now.TimeOfDay
        SafeEnterTrace(CallPriceKey, 90)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        _LastCallPrices = call_prices
        _VI = local_vi

        '2024.07.07 : DelayedSampleList 구현
        DelayedSampleListBuy.Add(delayed_sample_buy)
        While DelayedSampleListBuy.Count > 1 AndAlso DelayedSampleListBuy(1).Time + DelayTimeBuy < delayed_sample_buy.Time
            '샘플이 두 개 이상은 있어야 한다. 한 개 있으면 지우면 안 된다. DelayTime 을 갓 넘어선 녀석의 price 를 봐야 하기 때문에 끝에서 두번째 샘플의 시간을 조사한다.
#If 0 Then
            '지우기 전에 기록해서 비주얼라이즈해본다.
            If TestDecisionMakerCenter.Count > 0 AndAlso TestDecisionMakerCenter(0).Count > 0 AndAlso TestDecisionMakerCenter(0)(0).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count = 0 AndAlso OnlyAFewChanceLeft > 0 Then
                    'TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds, DelayedSampleListBuy(0).Price))
                    'DelayedBuy Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds + 1, DelayedSampleListBuy(0).Price * 0.98))
                    OnlyAFewChanceLeft = OnlyAFewChanceLeft - 1
                End If
                If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count > 0 Then
                    'TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds, DelayedSampleListBuy(0).Price))
                    'DelayedBuy Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(DelayedSampleListBuy(0).Time.TotalSeconds + 1, DelayedSampleListBuy(0).Price * 0.98))
                End If
            End If
#End If
            DelayedSampleListBuy.RemoveAt(0)
        End While

        DelayedSampleListSel.Add(delayed_sample_sel)
        While DelayedSampleListSel.Count > 1 AndAlso DelayedSampleListSel(1).Time + DelayTimeSel < delayed_sample_sel.Time
            '샘플이 두 개 이상은 있어야 한다. 한 개 있으면 지우면 안 된다. DelayTime 을 갓 넘어선 녀석의 price 를 봐야 하기 때문에 끝에서 두번째 샘플의 시간을 조사한다.
#If 0 Then
            '지우기 전에 기록해서 비주얼라이즈해본다.
            If TestDecisionMakerCenter.Count > 0 AndAlso TestDecisionMakerCenter(0).Count > 0 AndAlso TestDecisionMakerCenter(0)(0).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                If TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Count = 0 AndAlso OnlyAFewChanceLeft > 0 Then
                    'DelayedSel Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Add(New PointF(DelayedSampleListSel(0).Time.TotalSeconds + 1, DelayedSampleListSel(0).Price * 1.014))
                    OnlyAFewChanceLeft = OnlyAFewChanceLeft - 1
                End If
                If TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Count > 0 Then
                    'DelayedSel Fishing 가격을 비주얼라이즈 해보자
                    TestDecisionMakerCenter(0)(0).DelaySelCallPricePointList.Add(New PointF(DelayedSampleListSel(0).Time.TotalSeconds + 1, DelayedSampleListSel(0).Price * 1.014))
                End If
            End If
#End If
            DelayedSampleListSel.RemoveAt(0)
        End While
#If 0 Then
        'Time integral 계산
        Dim price_integral_wrt_time As Double = DelayedSampleListBuy(0).Price
        For index As Integer = 1 To DelayedSampleListBuy.Count - 1
            price_integral_wrt_time = price_integral_wrt_time + ((CType(DelayedSampleListBuy(index).Price, Double) - DelayedSampleListBuy(index - 1).Price)) * (DelayedSampleListBuy(index).Time.Seconds - DelayedSampleListBuy(index - 1).Time.Seconds) / CATCH_SECONDS
            'CATCH_SECONDS 는 실제 price 를 얼마나 빨리 따라가는지를 나타냄. 길면 길수록 늦게 따라감. LOW PASS FILTER 의 효과.
        Next
        If TestDecisionMakerCenter.Count > 1 AndAlso TestDecisionMakerCenter(0).Count > 0 AndAlso TestDecisionMakerCenter(0)(0).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
            If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count = 0 AndAlso OnlyOneChangeLeft Then
                TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(delayed_sample.Time.TotalSeconds, price_integral_wrt_time))
                OnlyOneChangeLeft = False
            End If
            If TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Count > 0 Then
                TestDecisionMakerCenter(0)(0).DelayBuyCallPricePointList.Add(New PointF(delayed_sample.Time.TotalSeconds, price_integral_wrt_time))
            End If
        End If
#End If

        SafeLeaveTrace(CallPriceKey, 91)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        ' For index As Integer = 0 To StockDecisionMakerList.Count - 1
        'StockDecisionMakerList(index).CallPriceUpdated()
        'Next
    End Sub

    '바로 이전 데이터를 새 데이터인 것처럼 보낸다.
    'Public Sub SetLimpHomeData()
    'StoreNewData(RecordList.Last.Price, RecordList.Last.Amount)
    'End Sub

    '새 데이터를 일단 저장함
    Public Sub StoreNewData(ByVal price As UInt32, ByVal amount As UInt64, ByVal gangdo As Double)
        SafeEnterTrace(StoringKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If price = 0 Then
            Dim a = 1
        End If
        StoredPrice = price
        StoredAmount = amount
        StoredGangdo = gangdo
        SafeLeaveTrace(StoringKey, 101)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '새 데이터 받아들임
    'Public Sub SetNewData(ByVal price As UInt32, ByVal amount As UInt64, ByVal gangdo As Double)
    Public Sub SetNewData() 'ByVal price As UInt32, ByVal amount As UInt64)
        'check all indice of three variables are same
        Dim buy_amount As UInt64
        Dim sel_amount As UInt64
        Dim price As UInt32
        Dim amount As UInt64
        Dim gangdo As Double
        SafeEnterTrace(StoringKey, 2)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        price = StoredPrice
        amount = StoredAmount
        gangdo = StoredGangdo
        SafeLeaveTrace(StoringKey, 102)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        'If index1 = index2 AndAlso index2 = index3 Then
        If RecordList.Count = 0 AndAlso StartTime = [DateTime].MinValue Then
            '최초 데이터 & StartTime이 안 정해져있을 때 => start time 기록
            StartTime = Now
        End If
        If CandleServiceCenter.CandleCount() = 0 AndAlso CandleStartTime = [DateTime].MinValue Then
            CandleStartTime = Now
        End If
        CurrentAmount = amount
        Dim a_record As SymbolRecord
        a_record.CoreRecord.Price = price
        a_record.CoreRecord.Amount = amount
        a_record.CoreRecord.Gangdo = gangdo
        If gangdo > 0 Then
            buy_amount = gangdo * amount / (100 + gangdo)
        Else
            buy_amount = 0
        End If
        a_record.BuyAmount = buy_amount
        If buy_amount > amount Then
            'float 계산이 잘못되었을 수 있다.
            sel_amount = 0
        Else
            sel_amount = amount - buy_amount
        End If
        a_record.SelAmount = sel_amount
#If 0 Then
        If RecordList.Count > 3 Then
            If buy_amount >= RecordList(RecordList.Count - 3).BuyAmount Then
                BuyAveAmount = (buy_amount - RecordList(RecordList.Count - 3).BuyAmount) / 3
            Else
                BuyAveAmount = 0
            End If
            If sel_amount >= RecordList(RecordList.Count - 3).SelAmount Then
                SelAveAmount = (sel_amount - RecordList(RecordList.Count - 3).SelAmount) / 3
            Else
                SelAveAmount = 0
            End If
        End If
#End If
        If _BuyAmountList.Count > 0 Then
            If buy_amount >= _BuyAmountList(0) Then
                BuyAveAmount = (buy_amount - _BuyAmountList(0)) / 1
            Else
                BuyAveAmount = 0
            End If
            If sel_amount >= _SelAmountList(0) Then
                SelAveAmount = (sel_amount - _SelAmountList(0)) / 1
            Else
                SelAveAmount = 0
            End If
            _BuyAmountList.RemoveAt(0)
            _SelAmountList.RemoveAt(0)
        End If
        a_record.CoreRecord.MAPrice = MAPrice(RecordList.Count - 1, a_record.CoreRecord.Price)

        RecordList.Add(a_record.CoreRecord)
        '위에서 아웃오브 메모리
        _BuyAmountList.Add(a_record.BuyAmount)
        _SelAmountList.Add(a_record.SelAmount)

#If MOVING_AVERAGE_DIFFERENCE Then
        Dim number_of_candle_update As Integer = MinuteCandleNewData(a_record)
        '190608:DONE 리턴값을 추가된 candle 수로 하는 게 좋을 것 같다.
#End If

        '리스트뷰 아이템 업데이트
        Dim str_MAPrice As String = ""
        If MAPossible Then
            '이평 가능한 조건이면
            str_MAPrice = MAPrice(RecordList.Count - 1, 0).ToString
        End If
#If 0 Then
        Dim delegate_list_view_update As New DelegateListViewUpdate(AddressOf ListViewUpdate)
        ArgObjects(0) = price.ToString
        ArgObjects(1) = amount.ToString
        'ArgObjects(2) = gangdo.ToString
        ArgObjects(2) = RecordList.Count.ToString
        ArgObjects(3) = str_MAPrice.ToString
        MainForm.BeginInvoke(delegate_list_view_update, ArgObjects)

#End If

        SafeEnterTrace(OneKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim real_profit As Double
        Dim buy_rate As Double
        Dim temp_record As SymbolRecord
        Dim temp_str As String
        'MAIN
#If 0 Then
#If MOVING_AVERAGE_DIFFERENCE Then
        If number_of_candle_update > 0 Then
            Dim main_decision_maker_list As List(Of c05G_MovingAverageDifference)
            Dim main_decision_maker As c05G_MovingAverageDifference
            Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)
            Dim sub_decision_maker As c05G_MovingAverageDifference
#Else
       Dim main_decision_maker_list As List(Of c05E_PatternChecker)
       Dim main_decision_maker As c05E_PatternChecker
#End If
            For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)
                '디시전 메이커에 알려줌
                For index As Integer = main_decision_maker_list.Count - 1 To 0 Step -1
                    main_decision_maker = main_decision_maker_list(index)
                    If main_decision_maker.StockOperator IsNot Nothing Then
                        SafeEnterTrace(main_decision_maker.StockOperator.OperationKey, 111)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    End If
                    temp_record.CoreRecord.Price = number_of_candle_update            'temp_record는 업데이트된 캔들 갯수를 담고 있는 container일 뿐이다.
                    main_decision_maker.DataArrived(temp_record)
#If MOVING_AVERAGE_DIFFERENCE Then
                    If main_decision_maker._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME AndAlso Not main_decision_maker.IsDisplayed Then
                        '190518: 화면에 추가한다.
                        NewDecisionChart(main_decision_maker)
                    ElseIf main_decision_maker._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE AndAlso (main_decision_maker.StockOperator Is Nothing OrElse main_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE) Then
                        '190518: 청산되었으니 업데이트한다
#If Not NO_SHOW_TO_THE_FORM Then
                        main_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성 (재생성)
#End If
                        MainForm.BeginInvoke(New DelegateUpdateDecision(AddressOf UpdatedDecisionChart), New Object() {main_decision_maker})
                        '190813:TODO 여기는 상시 돌아가는 thread이고 위에서 invoke한 thread가 MainForm에 대해 실행되기 전에 아래에서 StockOperator가 nothing이 되어 버리면 업데이트가 안 되는 경우 발생한다.
                    End If
                    If main_decision_maker.IsDone Then
                        If main_decision_maker.StockOperator Is Nothing OrElse main_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                            '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
                            Dim lv_done_decision_item As New ListViewItem(main_decision_maker.StartTime.Date.ToString("d"))    '날짜
                            '일반용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            'lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                           '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                            lv_done_decision_item.SubItems.Add(main_decision_maker.NumberOfEntering.ToString)
                            '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                            '130625: 아래 실제 수익률을 수정할 때가 왔다.
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                '실제 수익률은 stock operator가 있을 때만 행한다.
                                If main_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                                    real_profit = 0
                                ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                                    real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                                Else
                                    real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                                End If

                                lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                            ElseIf main_decision_maker.YieldForHighLevel Then
                                '타레벨을 위한 양보일 경우 수익률 계산에 포함되지 않게 -200%로 표시한다.
                                lv_done_decision_item.SubItems.Add("-200%")
                            Else
                                lv_done_decision_item.SubItems.Add("")
                            End If
                            '180609: Silent level 볼륨 대비 매수율
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                                buy_rate = main_decision_maker.StockOperator.BoughtAmount * main_decision_maker.StockOperator.InitPrice / main_decision_maker.SilentLevelVolume

                                lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                            Else
                                buy_rate = 0
                                lv_done_decision_item.SubItems.Add("")
                            End If

                            'lv_done_decision_item.SubItems.Add(main_decision_maker.ScoreSave.ToString())                  '스코어
                            If Caution Then
                                temp_str = "C"
                            Else
                                temp_str = "_"
                            End If
                            If Supervision Then
                                temp_str += "S"
                            Else
                                temp_str += "_"
                            End If
                            lv_done_decision_item.SubItems.Add(temp_str)        '2021.04.03: 오늘부터 스코어대신 Caution과 Supervision을 모니터링 한다.
                            '    Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                            '    Dim sudden_rate As Double = decision_maker.FallVolume / average_volume_per_15s
                            '    lv_done_decision_item.SubItems.Add(sudden_rate.ToString())
                            lv_done_decision_item.Tag = main_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
                            MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & main_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
                            Else
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & main_decision_maker.NumberOfEntering.ToString() & "," & "," & "," & temp_str)
                            End If

                            StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                            '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                            'If main_decision_maker.StockOperator IsNot Nothing Then
                            'SafeLeaveTrace(main_decision_maker.StockOperator.OperationKey, 112)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                            'End If
                            'main_decision_maker.StockOperator = Nothing         'stock operator 초기화. 190813: UpdatedDecisionChart 해야 되기 때문에 막아놨다. nothing 안 해도 될 것 같다.
                            main_decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                        Else
                            'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                        End If
                    End If
#Else
                    If main_decision_maker.IsDone Then
                        If main_decision_maker.StockOperator Is Nothing OrElse main_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                            '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
#If Not NO_SHOW_TO_THE_FORM Then
                            main_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
                            Dim lv_done_decision_item As New ListViewItem(main_decision_maker.StartTime.Date.ToString("d"))    '날짜
                        If Not DecisionByPattern Then
                            '일반용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            'lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
#If ALLOW_MULTIPLE_ENTERING Then
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPriceMulti.Average)                       '진입가
#Else
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
#End If
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                           '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                        Else
                            '패턴용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                        '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                        End If
                            '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                            '130625: 아래 실제 수익률을 수정할 때가 왔다.
#If 1 Then
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                '실제 수익률은 stock operator가 있을 때만 행한다.
                                If main_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                                    real_profit = 0
                                ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                                    real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                                Else
                                    real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                                End If

                                lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                            Else
                                lv_done_decision_item.SubItems.Add("")
                            End If
#Else

                    Dim real_profit As Double
                    real_profit = ((1 - TAX - FEE) * decision_maker.ExitPrice - (1 + FEE) * decision_maker.EnterPrice) / decision_maker.StockOperator.BuyDealPrice        '수익률
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))   'make subitem to meet the column number
#End If
                            '180609: Silent level 볼륨 대비 매수율
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                                buy_rate = main_decision_maker.StockOperator.BoughtAmount * main_decision_maker.StockOperator.InitPrice / main_decision_maker.SilentLevelVolume

                                lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                            Else
                                buy_rate = 0
                                lv_done_decision_item.SubItems.Add("")
                            End If

                            '    Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                            '    Dim sudden_rate As Double = decision_maker.FallVolume / average_volume_per_15s
                            '    lv_done_decision_item.SubItems.Add(sudden_rate.ToString())
                        If Not DecisionByPattern Then
#If 0 Then          '빌드에러 땜에 닫아놨다. 일반용일 때 열어라 (예스터)
                        '일반용
                        Dim yester_end_price As UInt32 = YesterPrice
                        If yester_end_price = 0 Then
                            lv_done_decision_item.SubItems.Add("no_data")
                        Else
                            Dim start_price_yester_rate As Double = StockDecisionMakerList(index)._FallingStartPrice / yester_end_price - 1
                            lv_done_decision_item.SubItems.Add(start_price_yester_rate.ToString("p"))
                        End If
#End If
                        Else
                            '패턴용		
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ScoreSave.ToString())                  '스코어
                            'For sub_index As Integer = 0 To StockDecisionMakerList(index).PriceRateTrend.Count - 1
                            'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).PriceRateTrend(sub_index).ToString("p"))                           '수익률
                            'Next
                        End If
                            lv_done_decision_item.Tag = main_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
                            MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & main_decision_maker.ScoreSave.ToString())
                            Else
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & "," & "," & main_decision_maker.ScoreSave.ToString())
                            End If

                            StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                            '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                            If main_decision_maker.StockOperator IsNot Nothing Then
                                SafeLeaveTrace(main_decision_maker.StockOperator.OperationKey, 112)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                            End If
                            main_decision_maker.StockOperator = Nothing         'stock operator 초기화
                            main_decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                        Else
                            'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                        End If
                    End If
#End If
                    If main_decision_maker.StockOperator IsNot Nothing Then
                        SafeLeaveTrace(main_decision_maker.StockOperator.OperationKey, 113)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    End If
                Next
                '140206 : 일단 이 함수만 decision maker list 위주로 훑어봤는데 특별한 게 없네.. 공유신경쓸게 생각보다 별로 없다.
                '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
                Dim monitoring_decision_maker_exist As Boolean = False

                For index As Integer = 0 To main_decision_maker_list.Count - 1
#If MOVING_AVERAGE_DIFFERENCE Then
                    'MovingAverageDifference 인 경우
                    monitoring_decision_maker_exist = True       '무조건 하나라도 있으면 더 이상 생성되면 안 된다.
#Else
                main_decision_maker = main_decision_maker_list(index)
                If main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING OrElse main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.UNWANTED_RISING_DETECTED OrElse _
                    main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse _
                    (main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.DONE And Not main_decision_maker.IsDone) Then
                    '위의 윗줄은 pre threshold 추가되면서 추가되었고, 위의 마지막 조건은 2시 48분 이후로 Decision maker 객체가 계속 증가하는 것을 방지하기 위해 추가되었다.
                    monitoring_decision_maker_exist = True
                    Exit For
                End If
#End If
                Next
                'SafeLeaveTrace(OneKey, 32)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                    '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
#If MOVING_AVERAGE_DIFFERENCE Then
                    'Moving Average Difference 전략
                    main_decision_maker_list.Add(New c05G_MovingAverageDifference(Me, CandleStartTime, index_decision_center, 0))
#Else
                main_decision_maker_list.Add(New c05E_PatternChecker(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center))
#End If
                End If
            Next
            Main_MADecisionFlag = False   '2020.09.14: 한 iteration 에서 진입하는 decision maker를 1개로 제한하기 위한 flag다. 매 iteration 마다 초기화한다. (StockSearcher에서 가져왔다)
#Else
        Dim main_decision_maker_list As List(Of c05G_DoubleFall)
        Dim main_decision_maker As c05G_DoubleFall
        For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)
            '디시전 메이커에 알려줌
            For index As Integer = main_decision_maker_list.Count - 1 To 0 Step -1
                main_decision_maker = main_decision_maker_list(index)
                If main_decision_maker.StockOperator IsNot Nothing Then
                    SafeEnterTrace(main_decision_maker.StockOperator.OperationKey, 111)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                End If
                main_decision_maker.DataArrived(a_record)
                If main_decision_maker.IsDone Then
                    If main_decision_maker.StockOperator Is Nothing OrElse main_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                        '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
#If Not NO_SHOW_TO_THE_FORM Then
                        main_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
                        Dim lv_done_decision_item As New ListViewItem(main_decision_maker.StartTime.Date.ToString("d"))    '날짜
                        If 0 Then       '일반용은 이제 갔다. test 계좌에서는 다 pattern 방식이다.
                            '일반용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            'lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                           '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                        Else
                            '패턴용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            If main_decision_maker.PrethresholdSucceed Then
                                lv_done_decision_item.SubItems.Add("N." & Code)                                                '코드, prethreshold 표시이다
                            Else
                                lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            End If
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                        '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                            lv_done_decision_item.SubItems.Add(main_decision_maker.NumberOfEntering.ToString)
                        End If
                        '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                        '130625: 아래 실제 수익률을 수정할 때가 왔다.
#If 1 Then
                        If main_decision_maker.StockOperator IsNot Nothing Then
                            '실제 수익률은 stock operator가 있을 때만 행한다.
                            If main_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                                real_profit = 0
                            ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                                real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                            Else
                                real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                            End If

                            lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                        Else
                            lv_done_decision_item.SubItems.Add("")
                        End If
#Else

                    Dim real_profit As Double
                    real_profit = ((1 - TAX - FEE) * test_decision_maker.ExitPrice - (1 + FEE) * test_decision_maker.EnterPrice) / test_decision_maker.StockOperator.BuyDealPrice        '수익률
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))   'make subitem to meet the column number
#End If
                        '180609: Silent level 볼륨 대비 매수율
                        If main_decision_maker.StockOperator IsNot Nothing Then
                            'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                            buy_rate = main_decision_maker.StockOperator.BoughtAmount * main_decision_maker.StockOperator.InitPrice / main_decision_maker.SilentLevelVolume

                            lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                        Else
                            buy_rate = 0
                            lv_done_decision_item.SubItems.Add("")
                        End If

                        '    Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                        '    Dim sudden_rate As Double = decision_maker.FallVolume / average_volume_per_15s
                        '    lv_done_decision_item.SubItems.Add(sudden_rate.ToString())
                        If 0 Then   '일반용은 이제 갔다. test 게좌는 항상 패턴이다.
#If 0 Then          '빌드에러 땜에 닫아놨다. 일반용일 때 열어라 (예스터)
                        '일반용
                        Dim yester_end_price As UInt32 = YesterPrice
                        If yester_end_price = 0 Then
                            lv_done_decision_item.SubItems.Add("no_data")
                        Else
                            Dim start_price_yester_rate As Double = StockDecisionMakerList(index)._FallingStartPrice / yester_end_price - 1
                            lv_done_decision_item.SubItems.Add(start_price_yester_rate.ToString("p"))
                        End If
#End If
                        Else
                            '패턴용		
                            'lv_done_decision_item.SubItems.Add(test_decision_maker.ScoreSave.ToString())                  '스코어
                            If Caution Then
                                temp_str = "C"
                            Else
                                temp_str = "_"
                            End If
                            If Supervision Then
                                temp_str += "S"
                            Else
                                temp_str += "_"
                            End If
                            lv_done_decision_item.SubItems.Add(temp_str)        '2021.04.03: 오늘부터 스코어대신 Caution과 Supervision을 모니터링 한다.
                            'For sub_index As Integer = 0 To StockDecisionMakerList(index).PriceRateTrend.Count - 1
                            'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).PriceRateTrend(sub_index).ToString("p"))                           '수익률
                            'Next
                        End If
                        lv_done_decision_item.Tag = main_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
                        MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
                        If main_decision_maker.StockOperator IsNot Nothing Then
                            If main_decision_maker.PrethresholdSucceed Then
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",N." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & main_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
                            Else
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & main_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
                            End If
                        Else
                            MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & main_decision_maker.NumberOfEntering.ToString() & "," & "," & "," & temp_str)
                        End If

                        StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                        '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                        'If test_decision_maker.StockOperator IsNot Nothing Then
                        'SafeLeaveTrace(test_decision_maker.StockOperator.OperationKey, 112)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        'test_decision_maker.StockOperator.ReleaseCOMInside()
                        'End If
                        If main_decision_maker.StockOperator Is Nothing Then
                            main_decision_maker.StockOperatorDebug = main_decision_maker.StockOperatorDebug ^ 2
                        End If
                        main_decision_maker.StockOperator = Nothing         'stock operator 초기화
                        main_decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                    Else
                        'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                    End If
                End If
                If main_decision_maker.StockOperator IsNot Nothing Then
                    SafeLeaveTrace(main_decision_maker.StockOperator.OperationKey, 113)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
            Next
            '140206 : 일단 이 함수만 decision maker list 위주로 훑어봤는데 특별한 게 없네.. 공유신경쓸게 생각보다 별로 없다.
            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            Dim monitoring_decision_maker_exist As Boolean = False

            For index As Integer = 0 To main_decision_maker_list.Count - 1
                'test_decision_maker = test_decision_maker_list(index)
                'If test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING OrElse test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.UNWANTED_RISING_DETECTED OrElse
                'test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse
                '(test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.DONE And Not test_decision_maker.IsDone) OrElse
                '(test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.DONE AndAlso test_decision_maker.StockOperator IsNot Nothing) Then
                '위의 윗줄은 pre threshold 추가되면서 추가되었고, 위의 마지막 조건은 2시 48분 이후로 Decision maker 객체가 계속 증가하는 것을 방지하기 위해 추가되었다.
                '2021.08.18: 급속히 떨어지는 종목을 5 조각 사서 순식간에 45만원에서 38만원으로 떨어지는 나락을 맛보았다 (A005320). 이것은 아직 다 팔지 않은 decision 객체가 있음에도 불구하고
                '2021.08.18: 새로운 decision 객체를 만들어 다시 진입시키기 때문이었다. 그래서 다 팔지 않은 게 있으면 다음 decision 객체를 만들지 않도록 수정한다.
                '2021.09.09: 비슷하게 급속히 떨어지는 종복 3 조각을 사서 큰 손실을 입었다. 위 8월 18일 처럼 막대한 손실은 아니지만.. 굳이 test 계좌에서 여러개의 decision 객체를 
                '2021.09.09: 둘 필요가 있나 의구심이 든다. 그냥 한 개만 허용하도록 해보자.
                monitoring_decision_maker_exist = True
                'Exit For
                'End If
            Next
            'SafeLeaveTrace(OneKey, 32)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                main_decision_maker_list.Add(New c05G_DoubleFall(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center, 2))
            End If
        Next
#End If
        If number_of_candle_update > 0 Then
            Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)
            Dim sub_decision_maker As c05G_MovingAverageDifference

            If SubAccount Then
                'SUB
                For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                    sub_decision_maker_list = SubDecisionMakerCenter(index_decision_center)
                    '디시전 메이커에 알려줌
                    For index As Integer = sub_decision_maker_list.Count - 1 To 0 Step -1
                        sub_decision_maker = sub_decision_maker_list(index)
                        If sub_decision_maker.StockOperator IsNot Nothing Then
                            SafeEnterTrace(sub_decision_maker.StockOperator.OperationKey, 111)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                        End If
                        temp_record.CoreRecord.Price = number_of_candle_update            'temp_record는 업데이트된 캔들 갯수를 담고 있는 container일 뿐이다.
                        sub_decision_maker.DataArrived(temp_record)
#If MOVING_AVERAGE_DIFFERENCE Then
                        If sub_decision_maker._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME AndAlso Not sub_decision_maker.IsDisplayed Then
                            '190518: 화면에 추가한다.
                            NewDecisionChart(sub_decision_maker)
                        ElseIf sub_decision_maker._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE AndAlso (sub_decision_maker.StockOperator Is Nothing OrElse sub_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE) Then
                            '190518: 청산되었으니 업데이트한다
#If Not NO_SHOW_TO_THE_FORM Then
                            sub_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성 (재생성)
#End If
                            MainForm.BeginInvoke(New DelegateUpdateDecision(AddressOf UpdatedDecisionChart), New Object() {sub_decision_maker})
                            '190813:TODO 여기는 상시 돌아가는 thread이고 위에서 invoke한 thread가 MainForm에 대해 실행되기 전에 아래에서 StockOperator가 nothing이 되어 버리면 업데이트가 안 되는 경우 발생한다.
                        ElseIf CandleServiceCenter.LastCandle.CandleTime.Hour = 15 AndAlso CandleServiceCenter.LastCandle.CandleTime.Minute = 20 AndAlso sub_decision_maker.IsDisplayed AndAlso Not (sub_decision_maker.ScoreA_RelTime = 0) Then
                            '2022.11.26:마지막에 장종료하기 전에 한 번 업데이트 하자
#If Not NO_SHOW_TO_THE_FORM Then
                            sub_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성 (재생성)
#End If
                            MainForm.BeginInvoke(New DelegateUpdateDecision(AddressOf UpdatedDecisionChart), New Object() {sub_decision_maker})
                            sub_decision_maker.ScoreA_RelTime = 0
                        End If
                        If sub_decision_maker.IsDone Then
                            If sub_decision_maker.StockOperator Is Nothing OrElse sub_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                                '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
                                Dim lv_done_decision_item As New ListViewItem(sub_decision_maker.StartTime.Date.ToString("d"))    '날짜
                                '일반용
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                                lv_done_decision_item.SubItems.Add("S." & Code)                                                '코드
                                'lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                                lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.EnterPrice)                       '진입가
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.ExitPrice)                        '청산가
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.Profit.ToString("p"))                           '수익률
                                If sub_decision_maker.FallVolume = 0 Then
                                    lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                                Else
                                    lv_done_decision_item.SubItems.Add(sub_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                                End If
                                lv_done_decision_item.SubItems.Add(sub_decision_maker.NumberOfEntering.ToString)
                                '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                                '130625: 아래 실제 수익률을 수정할 때가 왔다.
                                If sub_decision_maker.StockOperator IsNot Nothing Then
                                    '실제 수익률은 stock operator가 있을 때만 행한다.
                                    If sub_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                                        real_profit = 0
                                    ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                                        real_profit = ((1 - TAX - FEE) * sub_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * sub_decision_maker.StockOperator.BuyDealPrice) / sub_decision_maker.StockOperator.BuyDealPrice        '수익률
                                    Else
                                        real_profit = ((1 - TAX - FEE) * sub_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * sub_decision_maker.StockOperator.BuyDealPrice) / sub_decision_maker.StockOperator.BuyDealPrice        '수익률
                                    End If

                                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                                ElseIf sub_decision_maker.YieldForHighLevel Then
                                    '타레벨을 위한 양보일 경우 수익률 계산에 포함되지 않게 -200%로 표시한다.
                                    lv_done_decision_item.SubItems.Add("-200%")
                                Else
                                    lv_done_decision_item.SubItems.Add("")
                                End If
                                '180609: Silent level 볼륨 대비 매수율
                                If sub_decision_maker.StockOperator IsNot Nothing Then
                                    'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                                    buy_rate = sub_decision_maker.StockOperator.BoughtAmount * sub_decision_maker.StockOperator.InitPrice / sub_decision_maker.SilentLevelVolume

                                    lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                                Else
                                    buy_rate = 0
                                    lv_done_decision_item.SubItems.Add("")
                                End If

                                'lv_done_decision_item.SubItems.Add(sub_decision_maker.ScoreSave.ToString())                  '스코어
                                If Caution Then
                                    temp_str = "C"
                                Else
                                    temp_str = "_"
                                End If
                                If Supervision Then
                                    temp_str += "S"
                                Else
                                    temp_str += "_"
                                End If
                                lv_done_decision_item.SubItems.Add(temp_str)        '2021.04.03: 오늘부터 스코어대신 Caution과 Supervision을 모니터링 한다.
                                '    Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                                '    Dim sudden_rate As Double = decision_maker.FallVolume / average_volume_per_15s
                                '    lv_done_decision_item.SubItems.Add(sudden_rate.ToString())
                                lv_done_decision_item.Tag = sub_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
                                MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
                                If sub_decision_maker.StockOperator IsNot Nothing Then
                                    MessageLogging(",걸린애," & sub_decision_maker.StartTime.TimeOfDay.ToString & "," & sub_decision_maker.EnterTime.TimeOfDay.ToString & "," & sub_decision_maker.ExitTime.TimeOfDay.ToString & ",S." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & sub_decision_maker.EnterPrice & "," & sub_decision_maker.ExitPrice & "," & sub_decision_maker.Profit.ToString("p") & "," & sub_decision_maker.FallVolume.ToString() & "," & sub_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
                                Else
                                    MessageLogging(",걸린애," & sub_decision_maker.StartTime.TimeOfDay.ToString & "," & sub_decision_maker.EnterTime.TimeOfDay.ToString & "," & sub_decision_maker.ExitTime.TimeOfDay.ToString & ",S." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & sub_decision_maker.EnterPrice & "," & sub_decision_maker.ExitPrice & "," & sub_decision_maker.Profit.ToString("p") & "," & sub_decision_maker.FallVolume.ToString() & "," & sub_decision_maker.NumberOfEntering.ToString() & "," & "," & "," & temp_str)
                                End If

                                StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                                '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                                'If sub_decision_maker.StockOperator IsNot Nothing Then
                                'SafeLeaveTrace(sub_decision_maker.StockOperator.OperationKey, 112)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                                'End If
                                'sub_decision_maker.StockOperator = Nothing         'stock operator 초기화. 190813: UpdatedDecisionChart 해야 되기 때문에 막아놨다. nothing 안 해도 될 것 같다.
                                sub_decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제

                            Else
                                'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                            End If
                        End If
#Else
                    If main_decision_maker.IsDone Then
                        If main_decision_maker.StockOperator Is Nothing OrElse main_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                            '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
#If Not NO_SHOW_TO_THE_FORM Then
                            main_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
                            Dim lv_done_decision_item As New ListViewItem(main_decision_maker.StartTime.Date.ToString("d"))    '날짜
                        If Not DecisionByPattern Then
                            '일반용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            'lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
#If ALLOW_MULTIPLE_ENTERING Then
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPriceMulti.Average)                       '진입가
#Else
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
#End If
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                           '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                        Else
                            '패턴용
                            lv_done_decision_item.SubItems.Add(main_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("M." & Code)                                                '코드
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(main_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(main_decision_maker.Profit.ToString("p"))                        '수익률
                            If main_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(main_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                        End If
                            '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                            '130625: 아래 실제 수익률을 수정할 때가 왔다.
#If 1 Then
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                '실제 수익률은 stock operator가 있을 때만 행한다.
                                If main_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                                    real_profit = 0
                                ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                                    real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                                Else
                                    real_profit = ((1 - TAX - FEE) * main_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * main_decision_maker.StockOperator.BuyDealPrice) / main_decision_maker.StockOperator.BuyDealPrice        '수익률
                                End If

                                lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                            Else
                                lv_done_decision_item.SubItems.Add("")
                            End If
#Else

                    Dim real_profit As Double
                    real_profit = ((1 - TAX - FEE) * decision_maker.ExitPrice - (1 + FEE) * decision_maker.EnterPrice) / decision_maker.StockOperator.BuyDealPrice        '수익률
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))   'make subitem to meet the column number
#End If
                            '180609: Silent level 볼륨 대비 매수율
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                                buy_rate = main_decision_maker.StockOperator.BoughtAmount * main_decision_maker.StockOperator.InitPrice / main_decision_maker.SilentLevelVolume

                                lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                            Else
                                buy_rate = 0
                                lv_done_decision_item.SubItems.Add("")
                            End If

                            '    Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                            '    Dim sudden_rate As Double = decision_maker.FallVolume / average_volume_per_15s
                            '    lv_done_decision_item.SubItems.Add(sudden_rate.ToString())
                        If Not DecisionByPattern Then
#If 0 Then          '빌드에러 땜에 닫아놨다. 일반용일 때 열어라 (예스터)
                        '일반용
                        Dim yester_end_price As UInt32 = YesterPrice
                        If yester_end_price = 0 Then
                            lv_done_decision_item.SubItems.Add("no_data")
                        Else
                            Dim start_price_yester_rate As Double = StockDecisionMakerList(index)._FallingStartPrice / yester_end_price - 1
                            lv_done_decision_item.SubItems.Add(start_price_yester_rate.ToString("p"))
                        End If
#End If
                        Else
                            '패턴용		
                            lv_done_decision_item.SubItems.Add(main_decision_maker.ScoreSave.ToString())                  '스코어
                            'For sub_index As Integer = 0 To StockDecisionMakerList(index).PriceRateTrend.Count - 1
                            'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).PriceRateTrend(sub_index).ToString("p"))                           '수익률
                            'Next
                        End If
                            lv_done_decision_item.Tag = main_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
                            MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})
                            If main_decision_maker.StockOperator IsNot Nothing Then
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & main_decision_maker.ScoreSave.ToString())
                            Else
                                MessageLogging(",걸린애," & main_decision_maker.StartTime.TimeOfDay.ToString & "," & main_decision_maker.EnterTime.TimeOfDay.ToString & "," & main_decision_maker.ExitTime.TimeOfDay.ToString & ",M." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & main_decision_maker.EnterPrice & "," & main_decision_maker.ExitPrice & "," & main_decision_maker.Profit.ToString("p") & "," & main_decision_maker.FallVolume.ToString() & "," & "," & "," & main_decision_maker.ScoreSave.ToString())
                            End If

                            StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                            '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                            If main_decision_maker.StockOperator IsNot Nothing Then
                                SafeLeaveTrace(main_decision_maker.StockOperator.OperationKey, 112)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                            End If
                            main_decision_maker.StockOperator = Nothing         'stock operator 초기화
                            main_decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                        Else
                            'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                        End If
                    End If
#End If
                        If sub_decision_maker.StockOperator IsNot Nothing Then
                            SafeLeaveTrace(sub_decision_maker.StockOperator.OperationKey, 113)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        End If
                    Next
                    '140206 : 일단 이 함수만 decision maker list 위주로 훑어봤는데 특별한 게 없네.. 공유신경쓸게 생각보다 별로 없다.
                    '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
                    Dim monitoring_decision_maker_exist As Boolean = False

                    For index As Integer = 0 To sub_decision_maker_list.Count - 1
#If MOVING_AVERAGE_DIFFERENCE Then
                        'MovingAverageDifference 인 경우
                        monitoring_decision_maker_exist = True       '무조건 하나라도 있으면 더 이상 생성되면 안 된다.
#Else
                main_decision_maker = main_decision_maker_list(index)
                If main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING OrElse main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.UNWANTED_RISING_DETECTED OrElse _
                    main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse _
                    (main_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.DONE And Not main_decision_maker.IsDone) Then
                    '위의 윗줄은 pre threshold 추가되면서 추가되었고, 위의 마지막 조건은 2시 48분 이후로 Decision maker 객체가 계속 증가하는 것을 방지하기 위해 추가되었다.
                    monitoring_decision_maker_exist = True
                    Exit For
                End If
#End If
                    Next
                    'SafeLeaveTrace(OneKey, 32)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                        '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
#If MOVING_AVERAGE_DIFFERENCE Then
                        'Moving Average Difference 전략
                        sub_decision_maker_list.Add(New c05G_MovingAverageDifference(Me, CandleStartTime, index_decision_center, 1))
#Else
                sub_decision_maker_list.Add(New c05E_PatternChecker(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center))
#End If
                    End If
                Next
                Sub_MADecisionFlag = False   '2020.09.14: 한 iteration 에서 진입하는 decision maker를 1개로 제한하기 위한 flag다. 매 iteration 마다 초기화한다. (StockSearcher에서 가져왔다)

            End If
#If MOVING_AVERAGE_DIFFERENCE Then
        End If
#End If


        Dim test_decision_maker_list As List(Of c05F_FlexiblePCRenew)
        Dim test_decision_maker As c05F_FlexiblePCRenew
        For index_decision_center As Integer = TEST_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            test_decision_maker_list = TestDecisionMakerCenter(index_decision_center)
            '디시전 메이커에 알려줌
            For index As Integer = test_decision_maker_list.Count - 1 To 0 Step -1
                test_decision_maker = test_decision_maker_list(index)
                If test_decision_maker.StockOperator IsNot Nothing Then
                    SafeEnterTrace(test_decision_maker.StockOperator.OperationKey, 111)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                End If
                test_decision_maker.DataArrived(a_record)
                If test_decision_maker.IsDone Then
                    If test_decision_maker.StockOperator Is Nothing OrElse test_decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                        '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함
#If Not NO_SHOW_TO_THE_FORM Then
                        test_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
                        Dim lv_done_decision_item As New ListViewItem(test_decision_maker.StartTime.Date.ToString("d"))    '날짜
                        If 0 Then       '일반용은 이제 갔다. test 계좌에서는 다 pattern 방식이다.
                            '일반용
                            lv_done_decision_item.SubItems.Add(test_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(test_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(test_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
                            lv_done_decision_item.SubItems.Add("T." & Code)                                                '코드
                            'lv_done_decision_item.SubItems.Add(Name)                                                '종목명
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(test_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(test_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(test_decision_maker.Profit.ToString("p"))                           '수익률
                            If test_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(test_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                        Else
                            '패턴용
                            lv_done_decision_item.SubItems.Add(test_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
                            lv_done_decision_item.SubItems.Add(test_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
                            lv_done_decision_item.SubItems.Add(test_decision_maker.ExitTime.TimeOfDay.ToString)      '청산시간
#If PATTERN_PRETHRESHOLD Then
                            If test_decision_maker.PrethresholdSucceed Then
                                lv_done_decision_item.SubItems.Add("U." & Code)                                                '코드, prethreshold 표시이다
                            Else
                                lv_done_decision_item.SubItems.Add("T." & Code)                                                '코드
                            End If
#Else
                            lv_done_decision_item.SubItems.Add("T." & Code)                                                '코드
#End If
                            lv_done_decision_item.SubItems.Add("(" & index_decision_center.ToString & ")" & Name)
                            lv_done_decision_item.SubItems.Add(test_decision_maker.EnterPrice)                       '진입가
                            lv_done_decision_item.SubItems.Add(test_decision_maker.ExitPrice)                        '청산가
                            lv_done_decision_item.SubItems.Add(test_decision_maker.Profit.ToString("p"))                        '수익률
                            If test_decision_maker.FallVolume = 0 Then
                                lv_done_decision_item.SubItems.Add("0")      '하강볼륨
                            Else
                                lv_done_decision_item.SubItems.Add(test_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
                            End If
                            lv_done_decision_item.SubItems.Add(test_decision_maker.NumberOfEntering.ToString)
                        End If
                        '실제 수익률과 이론 수익률 비교를 위한 데이터를 수집한다.

                        '130625: 아래 실제 수익률을 수정할 때가 왔다.
#If 1 Then
                        If test_decision_maker.StockOperator IsNot Nothing Then
                            '실제 수익률은 stock operator가 있을 때만 행한다.
                            If test_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                                real_profit = 0
                            ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                                If test_decision_maker.StockOperator.BuyDealPrice = 0 Then
                                    '2024.07.03 : 실제수익률 무한대 나오는 거 디버깅 목적이다.
                                    real_profit = test_decision_maker.StockOperator.BoughtAmount
                                Else
                                    real_profit = ((1 - TAX - FEE) * test_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * test_decision_maker.StockOperator.BuyDealPrice) / test_decision_maker.StockOperator.BuyDealPrice        '수익률
                                End If
                            Else
                                If test_decision_maker.StockOperator.BuyDealPrice = 0 Then
                                    '2024.07.03 : 실제수익률 무한대 나오는 거 디버깅 목적이다.
                                    real_profit = test_decision_maker.StockOperator.BoughtAmount
                                Else
                                    real_profit = ((1 - TAX - FEE) * test_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * test_decision_maker.StockOperator.BuyDealPrice) / test_decision_maker.StockOperator.BuyDealPrice        '수익률
                                End If
                            End If

                            lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                        Else
                            lv_done_decision_item.SubItems.Add("")
                        End If
#Else

                    Dim real_profit As Double
                    real_profit = ((1 - TAX - FEE) * test_decision_maker.ExitPrice - (1 + FEE) * test_decision_maker.EnterPrice) / test_decision_maker.StockOperator.BuyDealPrice        '수익률
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))   'make subitem to meet the column number
#End If
                        '180609: Silent level 볼륨 대비 매수율
                        If test_decision_maker.StockOperator IsNot Nothing Then
                            'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                            buy_rate = test_decision_maker.StockOperator.BoughtAmount * test_decision_maker.StockOperator.InitPrice / test_decision_maker.SilentLevelVolume

                            lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                        Else
                            buy_rate = 0
                            lv_done_decision_item.SubItems.Add("")
                        End If

                        '    Dim average_volume_per_15s As Double = RecordList.Last.Amount * RecordList.Last.Price * 3 / RecordList.Count
                        '    Dim sudden_rate As Double = decision_maker.FallVolume / average_volume_per_15s
                        '    lv_done_decision_item.SubItems.Add(sudden_rate.ToString())
                        If 0 Then   '일반용은 이제 갔다. test 게좌는 항상 패턴이다.
#If 0 Then          '빌드에러 땜에 닫아놨다. 일반용일 때 열어라 (예스터)
                        '일반용
                        Dim yester_end_price As UInt32 = YesterPrice
                        If yester_end_price = 0 Then
                            lv_done_decision_item.SubItems.Add("no_data")
                        Else
                            Dim start_price_yester_rate As Double = StockDecisionMakerList(index)._FallingStartPrice / yester_end_price - 1
                            lv_done_decision_item.SubItems.Add(start_price_yester_rate.ToString("p"))
                        End If
#End If
                        Else
                            '패턴용		
                            'lv_done_decision_item.SubItems.Add(test_decision_maker.ScoreSave.ToString())                  '스코어
                            If Caution Then
                                temp_str = "C"
                            Else
                                temp_str = "_"
                            End If
                            If Supervision Then
                                temp_str += "S"
                            Else
                                temp_str += "_"
                            End If
                            lv_done_decision_item.SubItems.Add(temp_str)        '2021.04.03: 오늘부터 스코어대신 Caution과 Supervision을 모니터링 한다.
                            'For sub_index As Integer = 0 To StockDecisionMakerList(index).PriceRateTrend.Count - 1
                            'lv_done_decision_item.SubItems.Add(StockDecisionMakerList(index).PriceRateTrend(sub_index).ToString("p"))                           '수익률
                            'Next
                        End If
                        lv_done_decision_item.Tag = test_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
                        MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecision), New Object() {lv_done_decision_item})

                        If test_decision_maker.StockOperator IsNot Nothing Then
#If PATTERN_PRETHRESHOLD Then
                            If test_decision_maker.PrethresholdSucceed Then
                                MessageLogging(",걸린애," & test_decision_maker.StartTime.TimeOfDay.ToString & "," & test_decision_maker.EnterTime.TimeOfDay.ToString & "," & test_decision_maker.ExitTime.TimeOfDay.ToString & ",U." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & test_decision_maker.EnterPrice & "," & test_decision_maker.ExitPrice & "," & test_decision_maker.Profit.ToString("p") & "," & test_decision_maker.FallVolume.ToString() & "," & test_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
                            Else
                                MessageLogging(",걸린애," & test_decision_maker.StartTime.TimeOfDay.ToString & "," & test_decision_maker.EnterTime.TimeOfDay.ToString & "," & test_decision_maker.ExitTime.TimeOfDay.ToString & ",T." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & test_decision_maker.EnterPrice & "," & test_decision_maker.ExitPrice & "," & test_decision_maker.Profit.ToString("p") & "," & test_decision_maker.FallVolume.ToString() & "," & test_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
                            End If
#Else
                                MessageLogging(",걸린애," & test_decision_maker.StartTime.TimeOfDay.ToString & "," & test_decision_maker.EnterTime.TimeOfDay.ToString & "," & test_decision_maker.ExitTime.TimeOfDay.ToString & ",T." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & test_decision_maker.EnterPrice & "," & test_decision_maker.ExitPrice & "," & test_decision_maker.Profit.ToString("p") & "," & test_decision_maker.FallVolume.ToString() & "," & test_decision_maker.NumberOfEntering.ToString() & "," & real_profit.ToString("p") & "," & buy_rate.ToString("p") & "," & temp_str)
#End If
                        Else
                            MessageLogging(",걸린애," & test_decision_maker.StartTime.TimeOfDay.ToString & "," & test_decision_maker.EnterTime.TimeOfDay.ToString & "," & test_decision_maker.ExitTime.TimeOfDay.ToString & ",T." & Code & ",(" & index_decision_center.ToString & ")" & Name & "," & test_decision_maker.EnterPrice & "," & test_decision_maker.ExitPrice & "," & test_decision_maker.Profit.ToString("p") & "," & test_decision_maker.FallVolume.ToString() & "," & test_decision_maker.NumberOfEntering.ToString() & "," & "," & "," & temp_str)
                        End If

                        StopPriceinfo()     '구독하는 decision maker 소멸로 인한 구독 중단
                        '140204 : pre threshold 거치지 않고 바로 wait exiting 상태로 들어왔을 때 구독 여부 체크해보자.

                        'If test_decision_maker.StockOperator IsNot Nothing Then
                        'SafeLeaveTrace(test_decision_maker.StockOperator.OperationKey, 112)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        'test_decision_maker.StockOperator.ReleaseCOMInside()
                        'End If
                        test_decision_maker.StockOperator = Nothing         'stock operator 초기화
                        test_decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                    Else
                        'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                    End If
                End If
                If test_decision_maker.StockOperator IsNot Nothing Then
                    SafeLeaveTrace(test_decision_maker.StockOperator.OperationKey, 113)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
            Next
            '140206 : 일단 이 함수만 decision maker list 위주로 훑어봤는데 특별한 게 없네.. 공유신경쓸게 생각보다 별로 없다.
            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            Dim monitoring_decision_maker_exist As Boolean = False

            For index As Integer = 0 To test_decision_maker_list.Count - 1
                'test_decision_maker = test_decision_maker_list(index)
                'If test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_FALLING OrElse test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.UNWANTED_RISING_DETECTED OrElse
                'test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse
                '(test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.DONE And Not test_decision_maker.IsDone) OrElse
                '(test_decision_maker.CurrentPhase = c050_DecisionMaker.SearchPhase.DONE AndAlso test_decision_maker.StockOperator IsNot Nothing) Then
                '위의 윗줄은 pre threshold 추가되면서 추가되었고, 위의 마지막 조건은 2시 48분 이후로 Decision maker 객체가 계속 증가하는 것을 방지하기 위해 추가되었다.
                '2021.08.18: 급속히 떨어지는 종목을 5 조각 사서 순식간에 45만원에서 38만원으로 떨어지는 나락을 맛보았다 (A005320). 이것은 아직 다 팔지 않은 decision 객체가 있음에도 불구하고
                '2021.08.18: 새로운 decision 객체를 만들어 다시 진입시키기 때문이었다. 그래서 다 팔지 않은 게 있으면 다음 decision 객체를 만들지 않도록 수정한다.
                '2021.09.09: 비슷하게 급속히 떨어지는 종복 3 조각을 사서 큰 손실을 입었다. 위 8월 18일 처럼 막대한 손실은 아니지만.. 굳이 test 계좌에서 여러개의 decision 객체를 
                '2021.09.09: 둘 필요가 있나 의구심이 든다. 그냥 한 개만 허용하도록 해보자.
                monitoring_decision_maker_exist = True
                'Exit For
                'End If
            Next
            'SafeLeaveTrace(OneKey, 32)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                test_decision_maker_list.Add(New c05F_FlexiblePCRenew(Me, StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5), index_decision_center, 2))
            End If
        Next
        SafeLeaveTrace(OneKey, 32)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        SafeEnterTrace(CallPriceKey, 130)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '2024.08.05 : 오늘 주가가 폭락하였고, OutOfMemory 익셉션으로 PriceMiner가 종료되었다. 아무래도 최근에 추가된 DelayedSample 로직이 문제인 듯 하여,
        '2024.08.05 : 실시간 호가 모니터링이 끝난 후에는 필요없어진 DelayedSampleListBuy 를 줄이는 로직을 적용하기로 한다.
        Dim current_time = Now.TimeOfDay
        While DelayedSampleListBuy.Count > 1 AndAlso DelayedSampleListBuy(1).Time + DelayTimeBuy < current_time
            '샘플이 두 개 이상은 있어야 한다. 한 개 있으면 지우면 안 된다. DelayTime 을 갓 넘어선 녀석의 price 를 봐야 하기 때문에 끝에서 두번째 샘플의 시간을 조사한다.
            DelayedSampleListBuy.RemoveAt(0)
        End While
        SafeLeaveTrace(CallPriceKey, 131)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘


        AlreadyHooked = False   '이건 매 iteration 그 때 마다 초기화된다.
    End Sub

    '190508: 거래 시작전에 Chart DB 에 있는 내용을 읽어 걸린 애들을 화면에 표시할 필요가 있고
    '190508: xml 저장파일에 들어있는 보유중이고 아직 청산하지 않은 종목들도 화면에 표시할 필요가 있다.
    '190509: 거래 시작전에 Chart DB 에 있는 내용을 읽을 때 SetNewCandle 에서 하는 일은 과거에 걸린 애들 말고 현재 걸려있는 애들을 표시해줄 필요가 있다.

#If MOVING_AVERAGE_DIFFERENCE Then
    '이건 DB에서 읽어서 뿌릴 때 불린다.
    '190412: moving average difference 전용 list item과 graphic view를 만들자.
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
        If CandleServiceCenter.CandleCount() = 0 Then
            new_candle.AccumAmount = amount
        Else
            new_candle.AccumAmount = CandleServiceCenter.LastCandle().AccumAmount + amount
        End If

#If 0 Then
        If CandleServiceCenter.CandleCount() >= 4 Then
            'Variation5Minutes 업데이트
            Dim max_price, min_price As UInt32

            If new_candle.Amount > 0 Then
                min_price = new_candle.Low
                max_price = new_candle.High
            Else
                min_price = UInt32.MaxValue
                max_price = UInt32.MinValue
            End If
            For index As Integer = CandleServiceCenter.CandleCount() - 4 To CandleServiceCenter.CandleCount() - 1
                If CandleServiceCenter.Candle(index).Amount > 0 Then
                    min_price = Math.Min(min_price, CandleServiceCenter.Candle(index).Low)
                    max_price = Math.Max(max_price, CandleServiceCenter.Candle(index).High)
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
            new_candle.Average5Minutes = (new_candle.Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 3).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).Close) / 5

            'VariationRatio 업데이트
            new_candle.VariationRatio = new_candle.Variation5Minutes / new_candle.Average5Minutes
        End If
#End If
        'discontinuous point 업데이트
        If CandleServiceCenter.CandleCount() > 0 Then
            If open_price / CandleServiceCenter.LastCandle().Close < 0.7 OrElse open_price / CandleServiceCenter.LastCandle().Close > 1.3 Then
                DiscontinuousPoint = CandleServiceCenter.CandleCount()
            End If
        End If

        'new_candle.Average5Minutes = GetAverage5Minutes(new_candle.Close)
        'new_candle.Average30Minutes = GetAverage30Minutes(new_candle.Close)
        'new_candle.Average35Minutes = GetAverage35Minutes(new_candle.Close)
        'new_candle.Average70Minutes = GetAverage70Minutes(new_candle.Close)
        'new_candle.Average140Minutes = GetAverage140Minutes(new_candle.Close)
        'new_candle.Average280Minutes = GetAverage280Minutes(new_candle.Close)
        'new_candle.Average560Minutes = GetAverage560Minutes(new_candle.Close)
        'new_candle.Average1200Minutes = GetAverage1200Minutes(new_candle.Close)
        new_candle.Average2400Minutes = GetAverage2400Minutes(new_candle.Close)
        new_candle.Average4800Minutes = GetAverage4800Minutes(new_candle.Close)
        new_candle.Average9600Minutes = GetAverage9600Minutes(new_candle.Close)
        'new_candle.VariationRatio = GetVariationRatio(new_candle.Close)

        'DailyAmount 업데이트
        If new_candle.CandleTime.Date <> OldDate Then
            DailyAmountStore.Add(CandleServiceCenter.LastCandle().AccumAmount)
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

        CandleServiceCenter.AddCandle(new_candle)
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
        If MainForm.SymbolCollectionDone Then
            UpdateMinMaxAmountToday()
        End If
        If CandleServiceCenter.CandleCount() > MAX_NUMBER_OF_CANDLE Then
            LosingCandle = CandleServiceCenter.Candle(0)
            CandleServiceCenter.RemoveCandle()
            CandleStartTime = CandleServiceCenter.Candle(0).CandleTime
            If DiscontinuousPoint <> -1 Then
                DiscontinuousPoint -= 1
            End If
        End If

        RaiseEvent evNewMinuteCandleCreated()

        Dim decision_maker_list As List(Of c05G_MovingAverageDifference)
#If 0 Then

        'MAIN
        For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            decision_maker_list = MainDecisionMakerCenter(index_decision_center)
            '디시전 메이커에 알려줌
            '191001: 15:30에 done 된 경우 다음날 청산해야되는 거 인식 못하는 경우 있어서 아래와 같이 done확인후 remove 하는 부분을 CandleArrive하고
            '191001: 다음번 iteration에 하도록 바꿨다. 그리고 decision_maker_list는 사실상 count1개뿐이라서 for 문도 간단하게 없앴다.
            Dim index As Integer = 0
            'For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
            If decision_maker_list(index).IsDone Then
                'If StockDecisionMakerList(index).StockOperator Is Nothing OrElse StockDecisionMakerList(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함 =>190518: LoadPastCandles 끝나고 제일 마지막에 펜딩된 걸 표시하기로  함.

                'StockDecisionMakerList(index).StockOperator = Nothing         'stock operator 초기화
                decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                'Else
                'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                'End If
            ElseIf decision_maker_list(index)._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE Then
                '180109: 원래 이 조건은 없었으나, WAIT_EXIT 상태에서 취소된 경우를 구분하기 위해 만들게 되었다..
                decision_maker_list.RemoveAt(index)          '아무 동작 안 하고 decision maker를 삭제한다.
            End If

            '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
            Dim monitoring_decision_maker_exist As Boolean = False

            'For index As Integer = 0 To decision_maker_list.Count - 1
            'MovingAverageDifference 인 경우
            If decision_maker_list.Count <> 0 Then
                monitoring_decision_maker_exist = True       '무조건 하나라도 있으면 더 이상 생성되면 안 된다.
            End If
            'Next
            If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                'Moving Average Difference 전략
                decision_maker_list.Add(New c05G_MovingAverageDifference(Me, CandleStartTime, index_decision_center, 0))
            End If

            decision_maker_list(index).CandleArrived(new_candle)
            'Next

        Next
        Main_MADecisionFlag = False   '2020.09.14: 한 iteration 에서 진입하는 decision maker를 1개로 제한하기 위한 flag다. 매 iteration 마다 초기화한다. (StockSearcher에서 가져왔다)
#End If
        If SubAccount Then
            'SUB
            For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                decision_maker_list = SubDecisionMakerCenter(index_decision_center)
                '디시전 메이커에 알려줌
                '191001: 15:30에 done 된 경우 다음날 청산해야되는 거 인식 못하는 경우 있어서 아래와 같이 done확인후 remove 하는 부분을 CandleArrive하고
                '191001: 다음번 iteration에 하도록 바꿨다. 그리고 decision_maker_list는 사실상 count1개뿐이라서 for 문도 간단하게 없앴다.
                Dim index As Integer = 0
                'For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
                If decision_maker_list(index).IsDone Then
                    'If StockDecisionMakerList(index).StockOperator Is Nothing OrElse StockDecisionMakerList(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                    '디시전 메이커 종료되었고 진입조차 안 했거나 진입후 청산이 완료되었음 => 디시전 리스트에 표시함 =>190518: LoadPastCandles 끝나고 제일 마지막에 펜딩된 걸 표시하기로  함.

                    'StockDecisionMakerList(index).StockOperator = Nothing         'stock operator 초기화
                    decision_maker_list.RemoveAt(index)          '디시전 메이커 종료되었거나 청산까지 완료되었으므로 리스트에서 삭제
                    'Else
                    'stock operator가 있고 청산진행중인 것이므로 청산 종료될 때까지 기다린다.
                    'End If
                ElseIf decision_maker_list(index)._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE Then
                    '180109: 원래 이 조건은 없었으나, WAIT_EXIT 상태에서 취소된 경우를 구분하기 위해 만들게 되었다..
                    decision_maker_list.RemoveAt(index)          '아무 동작 안 하고 decision maker를 삭제한다.
                End If

                '가격 모니터링 중인 decision maker가 한 개는 있어야 된다.
                Dim monitoring_decision_maker_exist As Boolean = False

                'For index As Integer = 0 To decision_maker_list.Count - 1
                'MovingAverageDifference 인 경우
                If decision_maker_list.Count <> 0 Then
                    monitoring_decision_maker_exist = True       '무조건 하나라도 있으면 더 이상 생성되면 안 된다.
                End If
                'Next
                If Not monitoring_decision_maker_exist AndAlso Not NoMoreDecisionMaker Then
                    '가격 모니터링 중인 decision maker가 한 개도 없다면 하나 만들어 리스트에 붙인다
                    'Moving Average Difference 전략
                    decision_maker_list.Add(New c05G_MovingAverageDifference(Me, CandleStartTime, index_decision_center, 1))
                End If

                decision_maker_list(index).CandleArrived(new_candle)
                'Next

            Next
            Sub_MADecisionFlag = False   '2020.09.14: 한 iteration 에서 진입하는 decision maker를 1개로 제한하기 위한 flag다. 매 iteration 마다 초기화한다. (StockSearcher에서 가져왔다)
        End If
    End Sub

    '190518: LoadPastCandle이 끝나고 할 일을 한다.
    Public Sub PastCandlesLoaded()
        'Load된 과거 candles 갯수를 base로 저장한다.
        'BaseFromPastCandles = CandleServiceCenter.CandleCount()

        '걸려 있는 애들을 표시한다.
        Dim decision_maker_list As List(Of c05G_MovingAverageDifference)
        Dim the_maker As c05G_MovingAverageDifference
#If 0 Then
        For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            decision_maker_list = MainDecisionMakerCenter(index_decision_center)
            For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
                If decision_maker_list(index)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                    the_maker = decision_maker_list(index)
                    NewDecisionChart(the_maker)
                End If
            Next
        Next
#End If
#If 1 Then
        For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            decision_maker_list = SubDecisionMakerCenter(index_decision_center)
            For index As Integer = decision_maker_list.Count - 1 To 0 Step -1
                If decision_maker_list(index)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                    the_maker = decision_maker_list(index)
                    NewDecisionChart(the_maker)
                End If
            Next
        Next
#End If
    End Sub

    Private Sub NewDecisionChart(ByVal the_decision_maker As c05G_MovingAverageDifference)
#If Not NO_SHOW_TO_THE_FORM Then
        the_decision_maker.CreateGraphicData()          '그래픽에 쓰일 데이타 생성
#End If
        Dim lv_done_decision_item As New ListViewItem(the_decision_maker.EnterTime.Date.ToString("d"))    '날짜

        lv_done_decision_item.SubItems.Add(the_decision_maker.StartTime.TimeOfDay.ToString)     '시작시간
        lv_done_decision_item.SubItems.Add(the_decision_maker.EnterTime.TimeOfDay.ToString)     '진입시간
        lv_done_decision_item.SubItems.Add(the_decision_maker.MinutesPassed.ToString)      '보유 분 수
        If the_decision_maker.AccountCat = 0 Then
            lv_done_decision_item.SubItems.Add("M." & Code)                                                 '코드
        Else 'if the_decision_maker.AccountCat = 1 Then
            lv_done_decision_item.SubItems.Add("S." & Code)                                                 '코드
        End If
        lv_done_decision_item.SubItems.Add("(" & the_decision_maker.MABase.ToString & ")" & Name)                                               '종목명
        lv_done_decision_item.SubItems.Add(the_decision_maker.EnterPrice)                       '진입가
        lv_done_decision_item.SubItems.Add(the_decision_maker.ExitPrice)                        '청산가
        lv_done_decision_item.SubItems.Add(the_decision_maker.Profit.ToString("p"))                           '수익률
        If the_decision_maker.FallVolume = 0 Then
            lv_done_decision_item.SubItems.Add("0")      '하강볼륨
        Else
            lv_done_decision_item.SubItems.Add(the_decision_maker.FallVolume.ToString("##,#"))      '하강볼륨
        End If
#If ALLOW_MULTIPLE_ENTERING Then
        lv_done_decision_item.SubItems.Add(the_decision_maker.NumberOfEntering.ToString("##,#"))      'Number of entering
#End If
        lv_done_decision_item.Tag = the_decision_maker                      '리스트뷰 아이템 태그에 디시전 객체 걸기
        the_decision_maker.IsDisplayed = True                       '화면에 표시되고 있음을 표시해둔다.
#If NO_SHOW_TO_THE_FORM Then
        '2024.08.11 : 일주일동안 계속 OutOfMemory 에러가 났다. 월요일 대폭락의 여파가 아직 가시지 않은 걸로 보인다.
        '2024.08.11 : 내일도 OutOfMemory 나는 것이 겁나서 일단 NO_SHOW_TO_THE_FORM 을 enable 하기로 한다. 아래 HitList 코드는 StockSearcher 에서 온 걸로 보이는데
        '2024.08.11 : PriceMiner 에서는 파일로 걸린애들 모두 로그가 남으니까 안 돌려도 괜찮다. 그래서 disable 한다.
        'MainForm.HitList.Add(lv_done_decision_item)
#Else
        MainForm.BeginInvoke(New DelegateRegisterDecision(AddressOf RegisterDoneDecisionChart), New Object() {lv_done_decision_item})
#End If
    End Sub

    Private Sub UpdatedDecisionChart(ByVal the_decision_maker As c05G_MovingAverageDifference)
        Dim lv_done_decision_item As ListViewItem
        Dim real_profit As Double
        Dim buy_rate As Double
        For index As Integer = 0 To MainForm.lv_DoneDecisionsChart.Items.Count - 1
            lv_done_decision_item = MainForm.lv_DoneDecisionsChart.Items(index)
            If lv_done_decision_item.Tag Is the_decision_maker Then
                lv_done_decision_item.SubItems(3).Text = the_decision_maker.MinutesPassed.ToString      '보유 분 수
                lv_done_decision_item.SubItems(7).Text = the_decision_maker.ExitPrice                        '청산가
                lv_done_decision_item.SubItems(8).Text = the_decision_maker.Profit.ToString("p")            '수익률
#If ALLOW_MULTIPLE_ENTERING Then
                lv_done_decision_item.SubItems(10).Text = the_decision_maker.NumberOfEntering            'number of entering
#End If

                If the_decision_maker.StockOperator IsNot Nothing Then
                    '실제 수익률은 stock operator가 있을 때만 행한다.
                    If the_decision_maker.StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                        real_profit = 0
                    ElseIf MarketKind = MARKET_KIND.MK_KOSPI Then
                        real_profit = ((1 - TAX - FEE) * the_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * the_decision_maker.StockOperator.BuyDealPrice) / the_decision_maker.StockOperator.BuyDealPrice        '수익률
                    Else
                        real_profit = ((1 - TAX - FEE) * the_decision_maker.StockOperator.SelDealPrice - (1 + FEE) * the_decision_maker.StockOperator.BuyDealPrice) / the_decision_maker.StockOperator.BuyDealPrice        '수익률
                    End If

                    lv_done_decision_item.SubItems.Add(real_profit.ToString("p"))
                Else
                    lv_done_decision_item.SubItems.Add("")
                End If
                '180609: Silent level 볼륨 대비 매수율
                If the_decision_maker.StockOperator IsNot Nothing Then
                    'Silent level 볼륨 대비 매수율은 stock operator가 있을 때만 행한다.
                    buy_rate = the_decision_maker.StockOperator.BoughtAmount * the_decision_maker.StockOperator.InitPrice / the_decision_maker.SilentLevelVolume

                    lv_done_decision_item.SubItems.Add(buy_rate.ToString("p"))
                Else
                    buy_rate = 0
                    lv_done_decision_item.SubItems.Add("")
                End If
                lv_done_decision_item.SubItems.Add(the_decision_maker.ScoreSave)   'AScore
                lv_done_decision_item.SubItems.Add(the_decision_maker.ScoreB_Stability)   'BScore
                lv_done_decision_item.SubItems.Add(the_decision_maker.ScoreCFirst_CallPrice)   'CScore
                lv_done_decision_item.SubItems.Add(the_decision_maker.ScoreD_DepositBonus)   'DScore
            End If
        Next
    End Sub

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
        _MinPrecalForVariationRatio = Math.Min(_MinPrecalForVariationRatio, CandleServiceCenter.LastCandle().Low)
        _MaxPrecalForVariationRatio = Math.Max(_MaxPrecalForVariationRatio, CandleServiceCenter.LastCandle().High)

        If CandleServiceCenter.CandleCount() >= 5 Then
            Dim losing_price_low As UInt32 = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 5).Low
            If losing_price_low = _MinPrecalForVariationRatio Then
                Dim temp_min As UInt32 = CandleServiceCenter.LastCandle().Low
                For index As Integer = 0 To 2
                    temp_min = Math.Min(temp_min, CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2 - index).Low)
                Next
                _MinPrecalForVariationRatio = temp_min
            End If

            Dim losing_price_high As UInt32 = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 5).High
            If losing_price_high = _MaxPrecalForVariationRatio Then
                Dim temp_max As UInt32 = CandleServiceCenter.LastCandle().High
                For index As Integer = 0 To 2
                    temp_max = Math.Max(temp_max, CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2 - index).High)
                Next
                _MaxPrecalForVariationRatio = temp_max
            End If
        End If
    End Sub

    'Check if VariationRatio is ready to be calculated
    Public Function IsVariationRatioReady() As Boolean
        If CandleServiceCenter.CandleCount() >= 4 Then
            Return True
        Else
            Return False
        End If
    End Function

    'Get Average5Minutes
    Public Function GetAverage5Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'CandleServiceCenter에 저장되어 있는 5개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= 5 Then
                Dim new_sum_of_5_prices As Single = _PrecalForAverage5Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 5).Close
                Return new_sum_of_5_prices / 5
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 4개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 4 Then
                Dim new_sum_of_5_prices As Single = _PrecalForAverage5Minutes + new_price
                Return new_sum_of_5_prices / 5
            Else
                Return -1
            End If
        End If
    End Function

    'Check if Average5Minutes is ready to be calculated
    Public Function IsAverage5MinutesReady() As Boolean
        If CandleServiceCenter.CandleCount() >= 4 Then
            Return True
        Else
            Return False
        End If
    End Function

    'Update Precal of Average5Minutes
    Public Sub UpdateAverage5MinutesPrecal()
        'Precal은 지난 4개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage5Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 5 Then
            _PrecalForAverage5Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 5).Close
        End If
    End Sub

    'Get Average30Minutes
    Public Function GetAverage30Minutes(ByVal new_price As UInt32) As Single
        If new_price = 0 Then
            'CandleServiceCenter에 저장되어 있는 20개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= MA_MINUTE_COUNT AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 20 Then
                Dim new_sum_of_20_prices As Single = _PrecalForAverage30Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - MA_MINUTE_COUNT).Close
                Return new_sum_of_20_prices / MA_MINUTE_COUNT
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 39개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= MA_MINUTE_COUNT - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 20 Then
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
            'CandleServiceCenter에 저장되어 있는 35개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= MA_MINUTE_COUNT AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 35 Then
                Dim new_sum_of_35_prices As Single = _PrecalForAverage35Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - MA_MINUTE_COUNT).Close
                Return new_sum_of_35_prices / MA_MINUTE_COUNT
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 34개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= MA_MINUTE_COUNT - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 35 Then
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
            'CandleServiceCenter에 저장되어 있는 70개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= 70 AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 70 Then
                Dim new_sum_of_70_prices As Single = _PrecalForAverage70Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 70).Close
                Return new_sum_of_70_prices / 70
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 69개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 70 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 70 Then
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
            'CandleServiceCenter에 저장되어 있는 140개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= 140 AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 140 Then
                Dim new_sum_of_140_prices As Single = _PrecalForAverage140Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 140).Close
                Return new_sum_of_140_prices / 140
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 139개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 140 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 140 Then
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
            'CandleServiceCenter에 저장되어 있는 280개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= 280 AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 280 Then
                Dim new_sum_of_280_prices As Single = _PrecalForAverage280Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 280).Close
                Return new_sum_of_280_prices / 280
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 279개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 280 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 280 Then
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
            'CandleServiceCenter에 저장되어 있는 560개의 price들을 평균냄
            If CandleServiceCenter.CandleCount() >= 560 AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 560 Then
                Dim new_sum_of_560_prices As Single = _PrecalForAverage560Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 560).Close
                Return new_sum_of_560_prices / 560
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 559개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 560 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 560 Then
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
            'CandleServiceCenter에 저장되어 있는 1200개의 price들을 평균냄
            If (CandleServiceCenter.CandleCount() >= 1200) AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 1200 Then
                Dim new_sum_of_1200_prices As Single = _PrecalForAverage1200Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1200).Close
                Return new_sum_of_1200_prices / 1200
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 1199개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 1200 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 1200 Then
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
            'CandleServiceCenter에 저장되어 있는 2400개의 price들을 평균냄
            If (CandleServiceCenter.CandleCount() >= 2400) AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 2400 Then
                Dim new_sum_of_2400_prices As Single = _PrecalForAverage2400Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2400).Close
                Return new_sum_of_2400_prices / 2400
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 2399개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 2400 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 2400 Then
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
            'CandleServiceCenter에 저장되어 있는 4800개의 price들을 평균냄
            If (CandleServiceCenter.CandleCount() >= 4800) AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 4800 Then
                Dim new_sum_of_4800_prices As Single = _PrecalForAverage4800Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4800).Close
                Return new_sum_of_4800_prices / 4800
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 4799개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 4800 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 4800 Then
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
            'CandleServiceCenter에 저장되어 있는 9600개의 price들을 평균냄
            If (CandleServiceCenter.CandleCount() >= 9600) AndAlso DiscontinuousPoint - 1 < CandleServiceCenter.CandleCount() - 9600 Then
                Dim new_sum_of_9600_prices As Single = _PrecalForAverage9600Minutes + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 9600).Close
                Return new_sum_of_9600_prices / 9600
            Else
                Return -1
            End If
        Else
            'CandleServiceCenter에 저장되어 있는 9599개의 price + new_price 의 평균
            If CandleServiceCenter.CandleCount() >= 9600 - 1 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - 9600 Then
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
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage30Minutes += new_price
        If CandleServiceCenter.CandleCount() >= MA_MINUTE_COUNT Then
            _PrecalForAverage30Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - MA_MINUTE_COUNT).Close
        End If
    End Sub

    'Update Precal of Average35Minutes
    Public Sub UpdateAverage35MinutesPrecal()
        'Precal은 지난 34개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage35Minutes += new_price
        If CandleServiceCenter.CandleCount() >= MA_MINUTE_COUNT Then
            _PrecalForAverage35Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - MA_MINUTE_COUNT).Close
        End If
    End Sub

    'Update Precal of Average70Minutes
    Public Sub UpdateAverage70MinutesPrecal()
        'Precal은 지난 69개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage70Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 70 Then
            _PrecalForAverage70Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 70).Close
        End If
    End Sub

    'Update Precal of Average140Minutes
    Public Sub UpdateAverage140MinutesPrecal()
        'Precal은 지난 139개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage140Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 140 Then
            _PrecalForAverage140Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 140).Close
        End If
    End Sub

    'Update Precal of Average280Minutes
    Public Sub UpdateAverage280MinutesPrecal()
        'Precal은 지난 279개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage280Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 280 Then
            _PrecalForAverage280Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 280).Close
        End If
    End Sub

    'Update Precal of Average560Minutes
    Public Sub UpdateAverage560MinutesPrecal()
        'Precal은 지난 559개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage560Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 560 Then
            _PrecalForAverage560Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 560).Close
        End If
    End Sub

    'Update Precal of Average1200Minutes
    Public Sub UpdateAverage1200MinutesPrecal()
        'Precal은 지난 1199개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage1200Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 1200 Then
            _PrecalForAverage1200Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1200).Close
        End If
    End Sub

    'Update Precal of Average2400Minutes
    Public Sub UpdateAverage2400MinutesPrecal()
        'Precal은 지난 2399개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage2400Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 2400 Then
            _PrecalForAverage2400Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2400).Close
        End If
    End Sub

    'Update Precal of Average4800Minutes
    Public Sub UpdateAverage4800MinutesPrecal()
        'Precal은 지난 4799개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage4800Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 4800 Then
            _PrecalForAverage4800Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4800).Close
        End If
    End Sub

    'Update Precal of Average9600Minutes
    Public Sub UpdateAverage9600MinutesPrecal()
        'Precal은 지난 9600개 종가의 합이 유지된다.
        Dim new_price = CandleServiceCenter.LastCandle().Close

        _PrecalForAverage9600Minutes += new_price
        If CandleServiceCenter.CandleCount() >= 9600 Then
            _PrecalForAverage9600Minutes -= CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 9600).Close
        End If
    End Sub

    'Update min/max amount today
    Public Sub UpdateMinMaxAmountToday()
        '오늘 분당거래량의 min max값을 업데이트하자.
        Dim last_amount As UInt32 = CandleServiceCenter.LastCandle().Amount

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
            If StabilityList.Count > 8 AndAlso CandleServiceCenter.LastCandle().CandleTime.Date.Month = 3 AndAlso CandleServiceCenter.LastCandle().CandleTime.Date.Day = 20 Then
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
        Dim start_index As Integer = CandleServiceCenter.CandleCount() - _CandleCountToday
        Dim end_index As Integer = CandleServiceCenter.CandleCount() - 1
        '거래량을 10분씩 잘라서 더한다.
        For index As Integer = start_index To end_index
            this_10minutes_index = Math.Floor((CandleServiceCenter.Candle(index).CandleTime.TimeOfDay.TotalMinutes - 1) / 10)
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
            temp_amount_sum += CandleServiceCenter.Candle(index).Amount
        Next
        If temp_amount_sum <> 0 Then
            '마지막 남은 수량을 정리한다.
            ten_minutes_amount_list.Add(temp_amount_sum)
        End If

        '10분씩 자른 거래량들을 분석한다.
        '앞에 10분 뒤에 10분은 분석에서 제외한다.
        '20200728:interpolation시에 아래코드에서 index -1로 에러난다 확인하자
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
    End Sub

    Public Function MinuteCandleNewData(a_record As SymbolRecord) As Integer
        Dim return_value As Integer
        'If RecordList.Count = 1 Then
        'AmountAtZero = a_record.Amount

        'Return 0
        'End If

        'Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(5 * (RecordList.Count - 1))
        'Dim minute_index As Integer = current_time.Hour * 60 + current_time.Minute - StartTime.Hour * 60 - StartTime.Minute
        Dim current_time As DateTime = Now
        Dim minute_index As Integer = Math.Max(current_time.Hour * 60 + current_time.Minute - MarketStartHour * 60 - MarketStartMinute, 0)

        If IsGrowingCandleExist Then
            If (GrowingCandle.CandleTime.TimeOfDay = TimeSpan.FromHours(current_time.Hour) + TimeSpan.FromMinutes(current_time.Minute + 1)) OrElse IsTimeToKeepLastCandle(current_time) Then  '만들고 있는 캔들은 분 단위가 바뀔 때 기준으로 한다. 예를 들면 9시에 시작한 캔들은 9시 1분 캔들에 반영된다.
                '현재 Growing candle에 업데이트
                'Dim last_candle As CandleStructure = CandleServiceCenter.LastCandle()
                If GrowingCandle.Open = 0 Then
                    '첫거래로 인한 업데이트면
                    If a_record.CoreRecord.Price <> 0 Then
                        GrowingCandle.Open = a_record.CoreRecord.Price
                        GrowingCandle.Close = a_record.CoreRecord.Price
                        GrowingCandle.High = a_record.CoreRecord.Price
                        GrowingCandle.Low = a_record.CoreRecord.Price
                    Else
                        '0 이면 첫거래가 아닌 것이니 기존 값을 유지하면 된다.
                    End If
                Else
                    '아니면
                    GrowingCandle.Close = a_record.CoreRecord.Price
                    GrowingCandle.High = Math.Max(GrowingCandle.High, a_record.CoreRecord.Price)
                    GrowingCandle.Low = Math.Min(GrowingCandle.Low, a_record.CoreRecord.Price)
                End If
                If a_record.CoreRecord.Amount > AmountAtLastTime Then
                    GrowingCandle.Amount = GrowingCandle.Amount + (a_record.CoreRecord.Amount - AmountAtLastTime)
                Else
                    'do nothing
                End If
                AmountAtLastTime = a_record.CoreRecord.Amount
                If CandleServiceCenter.CandleCount() > 0 Then
                    GrowingCandle.AccumAmount = CandleServiceCenter.LastCandle().AccumAmount + GrowingCandle.Amount
                Else
                    GrowingCandle.AccumAmount = GrowingCandle.Amount
                End If
                'GrowingCandle.Average5Minutes = GetAverage5Minutes(GrowingCandle.Close)
                'GrowingCandle.Average35Minutes = GetAverage35Minutes(GrowingCandle.Close)
                'GrowingCandle.Average70Minutes = GetAverage70Minutes(GrowingCandle.Close)
                'GrowingCandle.Average140Minutes = GetAverage140Minutes(GrowingCandle.Close)
                'GrowingCandle.Average280Minutes = GetAverage280Minutes(GrowingCandle.Close)
                'GrowingCandle.Average560Minutes = GetAverage560Minutes(GrowingCandle.Close)
                'GrowingCandle.Average1200Minutes = GetAverage1200Minutes(GrowingCandle.Close)
                GrowingCandle.Average2400Minutes = GetAverage2400Minutes(GrowingCandle.Close)
                GrowingCandle.Average4800Minutes = GetAverage4800Minutes(GrowingCandle.Close)
                GrowingCandle.Average9600Minutes = GetAverage9600Minutes(GrowingCandle.Close)
                'GrowingCandle.VariationRatio = GetVariationRatio(GrowingCandle.Close)

                return_value = 0
                'TasksAfterNewCandleCreated()        'Test_ 변수도 모니터링으로 같이 업데이트 해보자. 임시다 이건.
            Else 'If GrowingCandle.CandleTime.TimeOfDay + TimeSpan.FromMinutes(1) = TimeSpan.FromHours(current_time.Hour) + TimeSpan.FromMinutes(current_time.Minute + 1) Then
                'Growing candle을 candle list로 보내고 새로운 growing candle 생성
                '190824: 이거를 5초마다 데이터 들어와도 하게끔 그렇게 부담스럽지 않게 로드를 좀 줄이자.
#If 0 Then
                If CandleServiceCenter.CandleCount() >= 4 Then
                    'Variation5Minutes 업데이트
                    Dim max_price, min_price As UInt32

                    If GrowingCandle.Amount > 0 Then
                        min_price = GrowingCandle.Low
                        max_price = GrowingCandle.High
                    Else
                        min_price = UInt32.MaxValue
                        max_price = UInt32.MinValue
                    End If
                    For index As Integer = CandleServiceCenter.CandleCount() - 4 To CandleServiceCenter.CandleCount() - 1
                        If CandleServiceCenter.Candle(index).Amount > 0 Then
                            min_price = Math.Min(min_price, CandleServiceCenter.Candle(index).Low)
                            max_price = Math.Max(max_price, CandleServiceCenter.Candle(index).High)
                        Else
                            'min_price 변경 없음
                        End If
                    Next
                    If max_price = 0 Then
                        GrowingCandle.Variation5Minutes = 0
                    Else
                        GrowingCandle.Variation5Minutes = max_price - min_price
                    End If

                    'Average5Minutes 업데이트
                    GrowingCandle.Average5Minutes = (GrowingCandle.Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 3).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).Close) / 5

                    'VariationRatio 업데이트
                    GrowingCandle.VariationRatio = GrowingCandle.Variation5Minutes / GrowingCandle.Average5Minutes
                End If
#End If
                If CandleServiceCenter.CandleCount() > 0 Then
                    Dim growing_candle_low, growing_candle_high As UInt32
                    If GrowingCandle.Low = 0 Then
                        growing_candle_low = GrowingCandle.Close
                    Else
                        growing_candle_low = GrowingCandle.Low
                    End If
                    If GrowingCandle.High = 0 Then
                        growing_candle_high = GrowingCandle.Close
                    Else
                        growing_candle_high = GrowingCandle.High
                    End If
                    If growing_candle_low / CandleServiceCenter.LastCandle().Close < 0.7 OrElse growing_candle_high / CandleServiceCenter.LastCandle().Close > 1.3 Then
                        '2021.03.17: 장초반 권리락 종목이 걸리는 것을 방지하기 위해 추가되었다.
                        '2021.03.22: 1분동안 거래가 없을 때는 low나 high가 0이 될 수도 있어 이에 대한 보완을 했다.
                        DiscontinuousPoint = CandleServiceCenter.CandleCount()
                    End If
                End If

                'GrowingCandle.Average5Minutes = GetAverage5Minutes(GrowingCandle.Close)
                'GrowingCandle.Average35Minutes = GetAverage35Minutes(GrowingCandle.Close)
                'GrowingCandle.Average70Minutes = GetAverage70Minutes(GrowingCandle.Close)
                'GrowingCandle.Average140Minutes = GetAverage140Minutes(GrowingCandle.Close)
                'GrowingCandle.Average280Minutes = GetAverage280Minutes(GrowingCandle.Close)
                'GrowingCandle.Average560Minutes = GetAverage560Minutes(GrowingCandle.Close)
                'GrowingCandle.Average1200Minutes = GetAverage1200Minutes(GrowingCandle.Close)
                GrowingCandle.Average2400Minutes = GetAverage2400Minutes(GrowingCandle.Close)
                GrowingCandle.Average4800Minutes = GetAverage4800Minutes(GrowingCandle.Close)
                GrowingCandle.Average9600Minutes = GetAverage9600Minutes(GrowingCandle.Close)
                'GrowingCandle.VariationRatio = GetVariationRatio(GrowingCandle.Close)

                CandleServiceCenter.AddCandle(GrowingCandle)
                If CandleServiceCenter.CandleCount() > MAX_NUMBER_OF_CANDLE Then
                    CandleServiceCenter.RemoveCandle()
                    CandleStartTime = CandleServiceCenter.Candle(0).CandleTime
                    If DiscontinuousPoint <> -1 Then
                        DiscontinuousPoint -= 1
                    End If
                End If
                UpdateAverage5MinutesPrecal()
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

                RaiseEvent evNewMinuteCandleCreated()

                '새 growing candle
                Dim new_candle As CandleStructure
                new_candle.CandleTime = current_time.Date + TimeSpan.FromHours(current_time.Hour) + TimeSpan.FromMinutes(current_time.Minute + 1)    '분 미만 버리는 로직

                If a_record.CoreRecord.Amount > AmountAtLastTime Then
                    new_candle.Amount = a_record.CoreRecord.Amount - AmountAtLastTime
                Else
                    new_candle.Amount = 0        '이런 경우 없어야 하는데 있는 것 같다.
                End If
                AmountAtLastTime = a_record.CoreRecord.Amount

                new_candle.AccumAmount = CandleServiceCenter.LastCandle().AccumAmount + new_candle.Amount
                If new_candle.Amount > 0 Then
                    '새로 거래가 이루어진 경우에만 업데이트
                    new_candle.Open = a_record.CoreRecord.Price
                    new_candle.Close = a_record.CoreRecord.Price
                    new_candle.High = a_record.CoreRecord.Price
                    new_candle.Low = a_record.CoreRecord.Price
                Else
                    '안 그러면 Last만 그 이전가로 업데이트
                    If CandleServiceCenter.CandleCount() > 0 Then
                        new_candle.Close = CandleServiceCenter.LastCandle().Close
                    Else
                        new_candle.Close = a_record.CoreRecord.Price    ' 그 이전가 없을 때는 그냥 현재 close값(아마0)
                    End If
                End If
                GrowingCandle = new_candle
                'GrowingCandle.Average5Minutes = GetAverage5Minutes(GrowingCandle.Close)
                'GrowingCandle.Average35Minutes = GetAverage35Minutes(GrowingCandle.Close)
                'GrowingCandle.Average70Minutes = GetAverage70Minutes(GrowingCandle.Close)
                'GrowingCandle.Average140Minutes = GetAverage140Minutes(GrowingCandle.Close)
                'GrowingCandle.Average280Minutes = GetAverage280Minutes(GrowingCandle.Close)
                'GrowingCandle.Average560Minutes = GetAverage560Minutes(GrowingCandle.Close)
                'GrowingCandle.Average1200Minutes = GetAverage1200Minutes(GrowingCandle.Close)
                GrowingCandle.Average2400Minutes = GetAverage2400Minutes(GrowingCandle.Close)
                GrowingCandle.Average4800Minutes = GetAverage4800Minutes(GrowingCandle.Close)
                GrowingCandle.Average9600Minutes = GetAverage9600Minutes(GrowingCandle.Close)
                'GrowingCandle.VariationRatio = GetVariationRatio(GrowingCandle.Close)

                return_value = 1
            End If
        Else
            '최초 new data(past candle 말고) 받아들일 시 이쪽으로 들어옴
            If minute_index = 0 Then
                '정상적으로 처음부터 시작함
                '새로운 candle 만들기
                Dim new_candle As CandleStructure
                new_candle.CandleTime = current_time.Date + TimeSpan.FromHours(current_time.Hour) + TimeSpan.FromMinutes(current_time.Minute + 1)    '분 미만 버리는 로직

                new_candle.Amount = a_record.CoreRecord.Amount - AmountAtLastTime
                AmountAtLastTime = a_record.CoreRecord.Amount

                If CandleServiceCenter.CandleCount() = 0 Then
                    new_candle.AccumAmount = new_candle.Amount
                Else
                    new_candle.AccumAmount = CandleServiceCenter.LastCandle().AccumAmount + new_candle.Amount
                End If
                If new_candle.Amount > 0 Then
                    '새로 거래가 이루어진 경우에만 업데이트
                    new_candle.Open = a_record.CoreRecord.Price
                    new_candle.Close = a_record.CoreRecord.Price
                    new_candle.High = a_record.CoreRecord.Price
                    new_candle.Low = a_record.CoreRecord.Price
                Else
                    '안 그러면 Last만 그 이전가로 업데이트
                    If CandleServiceCenter.CandleCount() > 0 Then
                        new_candle.Close = CandleServiceCenter.LastCandle().Close
                    Else
                        new_candle.Close = a_record.CoreRecord.Price    ' 그 이전가 없을 때는 그냥 현재 close값(아마0)
                    End If
                End If
#If 0 Then
                If CandleServiceCenter.CandleCount() >= 4 Then
                    'Variation5Minutes 업데이트
                    Dim max_price, min_price As UInt32

                    If new_candle.Amount > 0 Then
                        min_price = new_candle.Low
                        max_price = new_candle.High
                    Else
                        min_price = UInt32.MaxValue
                        max_price = UInt32.MinValue
                    End If
                    For index As Integer = CandleServiceCenter.CandleCount() - 4 To CandleServiceCenter.CandleCount() - 1
                        If CandleServiceCenter.Candle(index).Amount > 0 Then
                            min_price = Math.Min(min_price, CandleServiceCenter.Candle(index).Low)
                            max_price = Math.Max(max_price, CandleServiceCenter.Candle(index).High)
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
                    new_candle.Average5Minutes = (new_candle.Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 3).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).Close) / 5

                    'VariationRatio 업데이트
                    new_candle.VariationRatio = new_candle.Variation5Minutes / new_candle.Average5Minutes
                End If
#End If

                GrowingCandle = new_candle
                'GrowingCandle.Average5Minutes = GetAverage5Minutes(GrowingCandle.Close)
                'GrowingCandle.Average35Minutes = GetAverage35Minutes(GrowingCandle.Close)
                'GrowingCandle.Average70Minutes = GetAverage70Minutes(GrowingCandle.Close)
                'GrowingCandle.Average140Minutes = GetAverage140Minutes(GrowingCandle.Close)
                'GrowingCandle.Average280Minutes = GetAverage280Minutes(GrowingCandle.Close)
                'GrowingCandle.Average560Minutes = GetAverage560Minutes(GrowingCandle.Close)
                'GrowingCandle.Average1200Minutes = GetAverage1200Minutes(GrowingCandle.Close)
                GrowingCandle.Average2400Minutes = GetAverage2400Minutes(GrowingCandle.Close)
                GrowingCandle.Average4800Minutes = GetAverage4800Minutes(GrowingCandle.Close)
                GrowingCandle.Average9600Minutes = GetAverage9600Minutes(GrowingCandle.Close)
                'GrowingCandle.VariationRatio = GetVariationRatio(GrowingCandle.Close)
                IsGrowingCandleExist = True

                return_value = 0
            Else
                '처음부터 못 하고 건너뛴 시간이 있음 => 인터폴레이션
                If CandleServiceCenter.CandleCount() = 0 Then
                    '인터폴레이션 할 필요 없다.
                    'Growing하는 candle을 하나 만든다.
                    Dim new_candle As CandleStructure
                    new_candle.CandleTime = current_time.Date + TimeSpan.FromHours(MarketStartHour) + TimeSpan.FromMinutes(MarketStartMinute) + TimeSpan.FromMinutes(minute_index + 1)
                    new_candle.Amount = 0
                    AmountAtLastTime = a_record.CoreRecord.Amount
                    new_candle.AccumAmount = a_record.CoreRecord.Amount
                    new_candle.Open = a_record.CoreRecord.Price
                    new_candle.Close = new_candle.Open
                    new_candle.High = new_candle.Open
                    new_candle.Low = new_candle.Open

                    GrowingCandle = new_candle
                    IsGrowingCandleExist = True

                    return_value = 0
                Else
                    '인터폴레이션 해야 된다.
                    Dim last_candle As CandleStructure = CandleServiceCenter.LastCandle()
                    Dim one_minute_price_change As Double = (CType(a_record.CoreRecord.Price, Double) - last_candle.Close) / (minute_index + 1)
                    Dim one_minute_amount As Double = CType(a_record.CoreRecord.Amount, Double) / (minute_index + 1)
                    Dim new_candle As CandleStructure
                    For sub_index As Integer = 0 To minute_index - 1
                        new_candle.CandleTime = current_time.Date + TimeSpan.FromHours(MarketStartHour) + TimeSpan.FromMinutes(MarketStartMinute) + TimeSpan.FromMinutes(sub_index + 1)
                        new_candle.Amount = one_minute_amount
                        new_candle.AccumAmount = last_candle.AccumAmount + sub_index * one_minute_amount
                        new_candle.Open = CType(last_candle.Close, Integer) + sub_index * one_minute_price_change
                        new_candle.Close = new_candle.Open
                        new_candle.High = new_candle.Open
                        new_candle.Low = new_candle.Open

#If 0 Then
                        If CandleServiceCenter.CandleCount() >= 4 Then
                            'Variation5Minutes 업데이트
                            Dim max_price, min_price As UInt32

                            If new_candle.Amount > 0 Then
                                min_price = new_candle.Low
                                max_price = new_candle.High
                            Else
                                min_price = UInt32.MaxValue
                                max_price = UInt32.MinValue
                            End If
                            For index As Integer = CandleServiceCenter.CandleCount() - 4 To CandleServiceCenter.CandleCount() - 1
                                If CandleServiceCenter.Candle(index).Amount > 0 Then
                                    min_price = Math.Min(min_price, CandleServiceCenter.Candle(index).Low)
                                    max_price = Math.Max(max_price, CandleServiceCenter.Candle(index).High)
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
                            new_candle.Average5Minutes = (new_candle.Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 3).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).Close) / 5

                            'VariationRatio 업데이트
                            new_candle.VariationRatio = new_candle.Variation5Minutes / new_candle.Average5Minutes
                        End If
#End If
                        'new_candle.Average5Minutes = GetAverage5Minutes(new_candle.Close)
                        'new_candle.Average35Minutes = GetAverage35Minutes(new_candle.Close)
                        'new_candle.Average70Minutes = GetAverage70Minutes(new_candle.Close)
                        'new_candle.Average140Minutes = GetAverage140Minutes(new_candle.Close)
                        'new_candle.Average280Minutes = GetAverage280Minutes(new_candle.Close)
                        'new_candle.Average560Minutes = GetAverage560Minutes(new_candle.Close)
                        'new_candle.Average1200Minutes = GetAverage1200Minutes(new_candle.Close)
                        new_candle.Average2400Minutes = GetAverage2400Minutes(new_candle.Close)
                        new_candle.Average4800Minutes = GetAverage4800Minutes(new_candle.Close)
                        new_candle.Average9600Minutes = GetAverage9600Minutes(new_candle.Close)
                        'new_candle.VariationRatio = GetVariationRatio(new_candle.Close)

                        CandleServiceCenter.AddCandle(new_candle)
                        UpdateAverage5MinutesPrecal()
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
                        If CandleServiceCenter.CandleCount() > MAX_NUMBER_OF_CANDLE Then
                            LosingCandle = CandleServiceCenter.Candle(0)
                            CandleServiceCenter.RemoveCandle()
                            CandleStartTime = CandleServiceCenter.Candle(0).CandleTime
                            If DiscontinuousPoint <> -1 Then
                                DiscontinuousPoint -= 1
                            End If
                        End If

                        RaiseEvent evNewMinuteCandleCreated()
                    Next
                    'Growing하는 candle을 하나 만든다.
                    new_candle.CandleTime = current_time.Date + TimeSpan.FromHours(MarketStartHour) + TimeSpan.FromMinutes(MarketStartMinute) + TimeSpan.FromMinutes(minute_index + 1)
                    new_candle.Amount = 0
                    AmountAtLastTime = a_record.CoreRecord.Amount
                    new_candle.AccumAmount = CandleServiceCenter.LastCandle().AccumAmount
                    new_candle.Open = a_record.CoreRecord.Price
                    new_candle.Close = new_candle.Open
                    new_candle.High = new_candle.Open
                    new_candle.Low = new_candle.Open

                    GrowingCandle = new_candle
                    IsGrowingCandleExist = True

                    return_value = minute_index
                End If
            End If
        End If
#If 0 Then
        If BaseFromPastCandles = CandleServiceCenter.CandleCount() Then
            'BaseFromPastCandles 위에 처음 만드는 candle
            Dim first_candle As CandleStructure

            first_candle.CandleTime = StartTime
            If a_record.Amount > AmountAtZero Then
                first_candle.Amount = a_record.Amount - AmountAtZero
            Else
                first_candle.Amount = 0
            End If
            first_candle.AccumAmount = a_record.Amount
            If first_candle.Amount > 0 Then
                '새로 거래가 이루어진 경우에만 업데이트
                first_candle.Open = a_record.Price
                first_candle.Close = a_record.Price
                first_candle.High = a_record.Price
                first_candle.Low = a_record.Price
            Else
                '안 그러면 Last만 그 이전가로 업데이트
                first_candle.Close = a_record.Price
            End If

            CandleServiceCenter.AddCandle(first_candle)
            RaiseEvent evNewMinuteCandleCreated()
            NumberOfLastCandleUpdates = 1
        ElseIf minute_index + BaseFromPastCandles = CandleServiceCenter.CandleCount() - 1 Then
            '190602: 이부분을 보완해야 한다. 앞으로 LoadPastCandles 들어가면 안 맞게 된다. 더구나 지금도 제 시간 지나서 좀 늦게 시작하면 안 맞는다.
            '190604: LoadPastCandles 완료후 CandleServiceCenter의 count를 기록해두어 base로 사용해야 한다.
            'Last candle update 하기
            Dim last_amount As UInt32
            If CandleServiceCenter.CandleCount() = 1 Then
                last_amount = AmountAtZero
            Else
                last_amount = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).AccumAmount
            End If

            If a_record.Amount > last_amount Then
                Dim last_candle As CandleStructure = CandleServiceCenter.LastCandle()
                If last_candle.Open = 0 Then
                    '첫거래로 인한 업데이트면
                    last_candle.Open = a_record.Price
                    last_candle.Close = a_record.Price
                    last_candle.High = a_record.Price
                    last_candle.Low = a_record.Price
                Else
                    '아니면
                    last_candle.Close = a_record.Price
                    last_candle.High = Math.Max(last_candle.High, a_record.Price)
                    last_candle.Low = Math.Min(last_candle.Low, a_record.Price)
                End If
                If a_record.Amount > last_amount Then
                    last_candle.Amount = a_record.Amount - last_amount
                Else
                    last_candle.Amount = 0
                End If
                last_candle.AccumAmount = a_record.Amount

                CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1) = last_candle

                TasksAfterNewCandleCreated()        'Test_ 변수도 모니터링으로 같이 업데이트 해보자. 임시다 이건.
            Else
                '추가 거래가 없다면 굳이 업데이트 안 한다.
            End If
            NumberOfLastCandleUpdates += 1
        ElseIf minute_index + BaseFromPastCandles = CandleServiceCenter.CandleCount() Then
            '190605: 여기서 Else If로 Minuteindex 쪽이 1차이가 날 때는 아래 새로운 candle 만드는 곳에 진입하고
            '기존 만들던 캔들 list에 입성
            CandleServiceCenter.AddCandle(GrowingCandle)
            If CandleServiceCenter.CandleCount() > MAX_NUMBER_OF_CANDLE Then
                CandleServiceCenter.RemoveAt(0)
                StartTime = CandleServiceCenter.Candle(0).CandleTime
            End If

            RaiseEvent evNewMinuteCandleCreated()

            '새로운 candle 만들기
            Dim new_candle As CandleStructure
            new_candle.CandleTime = Now ' StartTime + TimeSpan.FromMinutes(CandleServiceCenter.CandleCount())
            new_candle.CandleTime = new_candle.CandleTime.Date + TimeSpan.FromHours(new_candle.CandleTime.Hour) + TimeSpan.FromMinutes(new_candle.CandleTime.Minute)    '분 이하 버리는 로직

            'If a_record.Amount > CandleServiceCenter.LastCandle().AccumAmount Then
            'new_candle.Amount = a_record.Amount - CandleServiceCenter.LastCandle().AccumAmount
            'Else
            'new_candle.Amount = 0
            'End If
            new_candle.Amount = a_record.Amount - AmountAtLastTime
            AmountAtLastTime = a_record.Amount

            'new_candle.AccumAmount = a_record.Amount
            new_candle.AccumAmount = CandleServiceCenter.LastCandle().AccumAmount + new_candle.Amount
            If new_candle.Amount > 0 Then
                '새로 거래가 이루어진 경우에만 업데이트
                new_candle.Open = a_record.Price
                new_candle.Close = a_record.Price
                new_candle.High = a_record.Price
                new_candle.Low = a_record.Price
            Else
                '안 그러면 Last만 그 이전가로 업데이트
                new_candle.Close = a_record.Price
            End If

            If CandleServiceCenter.CandleCount() >= 4 Then
                'Variation5Minutes 업데이트
                Dim max_price, min_price As UInt32

                If new_candle.Amount > 0 Then
                    min_price = new_candle.Low
                    max_price = new_candle.High
                Else
                    min_price = UInt32.MaxValue
                    max_price = UInt32.MinValue
                End If
                For index As Integer = CandleServiceCenter.CandleCount() - 4 To CandleServiceCenter.CandleCount() - 1
                    If CandleServiceCenter.Candle(index).Amount > 0 Then
                        min_price = Math.Min(min_price, CandleServiceCenter.Candle(index).Low)
                        max_price = Math.Max(max_price, CandleServiceCenter.Candle(index).High)
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
                new_candle.Average5Minutes = (new_candle.Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 4).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 3).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Close + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).Close) / 5

                'VariationRatio 업데이트
                new_candle.VariationRatio = new_candle.Variation5Minutes / new_candle.Average5Minutes
            End If

            'CandleServiceCenter.AddCandle(new_candle)
            GrowingCandle = new_candle
            'If CandleServiceCenter.CandleCount() > MAX_NUMBER_OF_CANDLE Then
            'CandleServiceCenter.RemoveAt(0)
            'StartTime = CandleServiceCenter.Candle(0).CandleTime
            'End If

            'RaiseEvent evNewMinuteCandleCreated()

            'NumberOfLastCandleUpdates = 1
        ElseIf minute_index + BaseFromPastCandles > CandleServiceCenter.CandleCount() Then
            '그 이상 차이가 나 때는 interpolation 로직을 타게 만든다.

        Else
            '어떻게 이런 경우가?
        End If

        If NumberOfLastCandleUpdates = 12 Then
            Return True
        Else
            Return False
        End If
#End If
        Return return_value
    End Function

#If 0 Then

    '편차의 이동평균
    Public Function MA_Var(ByVal how_long_in_minutes As UInt32) As Double
#If 0 Then
        If CandleServiceCenter.CandleCount() >= 5 Then
            Dim sum As Double = 0
            Dim number_of_samples As Integer = Math.Min(how_long_in_5minutes, CandleServiceCenter.CandleCount() / 5)
            For index As Integer = 0 To number_of_samples - 1
                sum = sum + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1 - 5 * index).VariationRatio
            Next
            Return sum / number_of_samples
        Else
            '아직 average variation을 계산할 만큼 candle들이 안 모였다.
            Return -1
        End If
#Else
#If 0 Then
        If CandleServiceCenter.CandleCount() >= 1 Then
            Dim sum As Double = 0
            Dim number_of_samples As Integer = Math.Min(how_long_in_minutes, CandleServiceCenter.CandleCount())
            For index As Integer = 0 To number_of_samples - 1
                sum = sum + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1 - index).VariationRatio
            Next
            Return sum / number_of_samples
        Else
            '아직 average variation을 계산할 만큼 candle들이 안 모였다.
            Return -1
        End If
#End If
        If CandleServiceCenter.CandleCount() = 1 Then
            '아직 average variation을 계산할 만큼 candle들이 안 모였다.
            Return CandleServiceCenter.LastCandle().VariationRatio
        Else
            If how_long_in_minutes < CandleServiceCenter.CandleCount() Then
                '이전까지의 sum of VariationRatio에 losing된 VariationRatio를 빼고 새 VariationRatio를 더한다.
                Dim losing_candle As CandleStructure = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - how_long_in_minutes - 1)
                Dim last_sum_of_variation_ratio As Double = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Test_MA_Var * how_long_in_minutes
                Dim new_sum_of_variation_ratio As Double = last_sum_of_variation_ratio - losing_candle.VariationRatio + CandleServiceCenter.LastCandle().VariationRatio
                Return new_sum_of_variation_ratio / how_long_in_minutes
            Else
                '모아놓은 candle들이 아직 원하는 MA_VAR를 만들기에 부족한 상황
                If CandleServiceCenter.CandleCount() = MAX_NUMBER_OF_CANDLE Then
                    'losing candle이 있다.
                    '이전까지의 sum of VariationRatio에 losing된 VariationRatio를 빼고 새 VariationRatio를 더한다.
                    Dim last_sum_of_variation_ratio As Double = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Test_MA_Var * MAX_NUMBER_OF_CANDLE
                    Dim new_sum_of_variation_ratio As Double = last_sum_of_variation_ratio - LosingCandle.VariationRatio + CandleServiceCenter.LastCandle().VariationRatio
                    Return new_sum_of_variation_ratio / MAX_NUMBER_OF_CANDLE
                Else    'If CandleServiceCenter.CandleCount() < MAX_NUMBER_OF_CANDLE
                    'losing candle이 없다.
                    'VariationRatio 를 계속 더해야 한다.
                    '이전까지의 sum of VariationRatio는 이전 MA_Var로부터 계산한다.
                    Dim last_candle_count As Integer = CandleServiceCenter.CandleCount() - 1
                    Dim last_sum_of_variation_ratio As Double = CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 2).Test_MA_Var * last_candle_count
                    Dim new_sum_of_variation_ratio As Double = last_sum_of_variation_ratio + CandleServiceCenter.LastCandle().VariationRatio
                    Return new_sum_of_variation_ratio / (last_candle_count + 1)
                End If
            End If
        End If
#End If
    End Function
#End If

#If 0 Then
    '가격의 이동평균
    Public Function MA_Price(ByVal how_long_in_5minutes As UInt32) As Double
        If CandleServiceCenter.CandleCount() >= how_long_in_5minutes * 5 AndAlso DiscontinuousPoint < CandleServiceCenter.CandleCount() - how_long_in_5minutes * 5 Then
            Dim sum As Double = 0
            For index As Integer = 0 To how_long_in_5minutes - 1
                sum = sum + CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1 - 5 * index).Average5Minutes
            Next
            Return sum / how_long_in_5minutes
        Else
            '아직 average price 를 계산할 만큼 candle들이 안 모였다.
            Return -1
        End If
    End Function
#End If

    '거래량의 이동평균
    Public Function MA_Amount(ByVal how_long_in_minutes As UInt32) As Double
        If CandleServiceCenter.CandleCount() > how_long_in_minutes Then
            If CandleServiceCenter.CandleCount() = 1 Then
                'Return CType(CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).AccumAmount - AmountAtZero, Double)
                Return CType(CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).AccumAmount, Double)
            Else
                'Return CType(CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).AccumAmount - CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1 - how_long_in_minutes).AccumAmount, Double) / how_long_in_minutes
                Return CType(CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).AccumAmount - CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1 - how_long_in_minutes).AccumAmount, Double) / how_long_in_minutes
            End If
        Else
            'Return CType(CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).AccumAmount - AmountAtZero, Double) / CandleServiceCenter.CandleCount()
            Return CType(CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1).AccumAmount, Double) / CandleServiceCenter.CandleCount()
        End If
    End Function

    Public Sub TasksAfterNewCandleCreated()
#If 0 Then
        Dim last_candle As CandleStructure = CandleServiceCenter.LastCandle()
        last_candle.Test_MA_Var = MA_Var(5000)
        If last_candle.Test_MA_Var > 0.007 Then
            'last_candle.Test_MA_Price = GetAverage35Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.004 Then
            'last_candle.Test_MA_Price = GetAverage35Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.002 Then
            'last_candle.Test_MA_Price = GetAverage35Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.001 Then
            'last_candle.Test_MA_Price = GetAverage35Minutes(0)
        ElseIf last_candle.Test_MA_Var > 0.0 Then
            'last_candle.Test_MA_Price = GetAverage35Minutes(0)
        Else
            'last_candle.Test_MA_Price = -1
        End If

        CandleServiceCenter.Candle(CandleServiceCenter.CandleCount() - 1) = last_candle
#End If
    End Sub
#End If

    '하한가 비상상황임이 전해졌을 때
    '[OneKey] Order Blanked event hander- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub LowLimitCautionIssued()
        'MAIN
        Dim main_decision_maker_list As List(Of c05G_DoubleFall)
        For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)
            For index As Integer = 0 To main_decision_maker_list.Count - 1
                'If StockDecisionMakerList(index).StockOperator IsNot Nothing Then
                'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                main_decision_maker_list(index).NoMoreOperation = True
                'Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
                'End If
            Next
        Next

        If SubAccount Then
            'SUB
            Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)
            For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
                sub_decision_maker_list = SubDecisionMakerCenter(index_decision_center)
                For index As Integer = 0 To sub_decision_maker_list.Count - 1
                    'If StockDecisionMakerList(index).StockOperator IsNot Nothing Then
                    'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                    sub_decision_maker_list(index).NoMoreOperation = True
                    'Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
                    'End If
                Next
            Next
        End If

        'TEST
        Dim test_decision_maker_list As List(Of c05F_FlexiblePCRenew)
        For index_decision_center As Integer = 0 To TEST_NUMBER_OF_DECIDERS - 1
            test_decision_maker_list = TestDecisionMakerCenter(index_decision_center)
            For index As Integer = 0 To test_decision_maker_list.Count - 1
                'If StockDecisionMakerList(index).StockOperator IsNot Nothing Then
                'stock operator가 아직 없는 경우는 더 이상 만들지 않게 조치
                test_decision_maker_list(index).NoMoreOperation = True
                'Else '그렇지 않은 경우는 stock operator가 알아서 다 팔아버리는 처리를 한다.
                'End If
            Next
        Next

        NoMoreDecisionMaker = True      '오늘 이 종목은 끝났다.
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '디시전 관련 리스트뷰 아이템 등록
    Private Sub RegisterDoneDecision(ByVal list_view_item As ListViewItem)
        MainForm.lv_DoneDecisions.Items.Add(list_view_item)
        MainForm.lv_DoneDecisions.Items(MainForm.lv_DoneDecisions.Items.Count - 1).Focused = True
    End Sub

    '디시전 관련 리스트뷰 아이템 등록
    Private Sub RegisterDoneDecisionChart(ByVal list_view_item As ListViewItem)
        '2024.08.09 : 4일 연속 OutOfMemory 에러로 인한 종료. 별로 필요없는 DoneDecisioNChart 를 업데이트하지 않기로 한다.
        MainForm.lv_DoneDecisionsChart.Items.Add(list_view_item)
        MainForm.lv_DoneDecisionsChart.Items(MainForm.lv_DoneDecisionsChart.Items.Count - 1).Focused = True
    End Sub

    '리스트뷰 업데이트
    'Private Sub ListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String, ByVal text_4 As String)

#If 0 Then
    Private Sub ListViewUpdate(ByVal text_0 As String, ByVal text_1 As String, ByVal text_2 As String, ByVal text_3 As String)
        If LVItem IsNot Nothing Then
            LVItem.SubItems(LVSUB_NOW_PRICE).Text = text_0
            LVItem.SubItems(LVSUB_AMOUNT).Text = text_1
            'LVItem.SubItems(LVSUB_GANGDO).Text = text_2
            LVItem.SubItems(LVSUB_DATA_COUNT).Text = text_2
            If MAPossible Then
                '이평 가능한 조건이면
                LVItem.SubItems(LVSUB_MOVING_AVERAGE_PRICE).Text = text_3
            End If
        End If
    End Sub
#End If

    Public Sub SaveToDB(ByVal db_connection As OleDb.OleDbConnection)
        Dim cmd As OleDb.OleDbCommand
        Dim result As Integer
        Dim insert_command As String
        Dim sample_time As DateTime
        Dim price As UInt32
        Dim amount As UInt64
#If NO_GANGDO_DB Then
#Else
        Dim gangdo As Double
#End If
        Dim error_count As Integer = 0

        If Not DBTableExist Then
            'Table이 존재하지 않음 => DB 생성
            'create table A00001 (SampledTime DateTime primary key, Price LONG, Amount DECIMAL, Gangdo FLOAT);
            'Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price LONG, Amount DECIMAL, Gangdo DOUBLE);"
            'Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price BIGINT, Amount DECIMAL, Gangdo float);"
            'Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price INT, Amount BIGINT);"
#If NO_GANGDO_DB Then
            Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price INT, Amount BIGINT);"
#Else
            Dim table_create_command As String = "CREATE TABLE " & Code & "(SampledTime DATETIME PRIMARY KEY, Price INT, Amount BIGINT, Gangdo REAL);"
#End If
            cmd = New OleDb.OleDbCommand(table_create_command, db_connection)
            cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
            result = cmd.ExecuteNonQuery()             '명령 실행
            cmd.Dispose()
        End If

        For index As Integer = 0 To RecordList.Count - 1
            sample_time = StartTime + TimeSpan.FromSeconds(index * 5)
            price = RecordList(index).Price
            amount = RecordList(index).Amount
#If NO_GANGDO_DB Then
            insert_command = "INSERT INTO " & Code & " (SampledTime, Price, Amount) VALUES ('" & sample_time.ToString("yyyy'-'MM'-'dd HH':'mm':'ss") & "', " & price.ToString & ", " & amount.ToString & ");"
#Else
            gangdo = RecordList(index).Gangdo
            insert_command = "INSERT INTO " & Code & " (SampledTime, Price, Amount, Gangdo) VALUES ('" & sample_time.ToString("yyyy'-'MM'-'dd HH':'mm':'ss") & "', " & price.ToString & ", " & amount.ToString & ", " & gangdo.ToString & ");"
#End If
            'insert_command = "INSERT INTO " & Code & " (SampledTime, Price, Amount) VALUES ('" & sample_time.ToString("yyyy'-'MM'-'dd HH':'mm':'ss") & "', " & price.ToString & ", " & amount.ToString & ");"
            cmd = New OleDb.OleDbCommand(insert_command, db_connection)
            cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
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
                    cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                End Try
            Loop While error_count > 0 AndAlso error_count < 10

            cmd.Dispose()
        Next
    End Sub

    '계좌내 조회된 잔고를 연결시킨다
    Public Sub LoadStock()
#If 0 Then
        'Main 계좌
        Dim main_decision_maker_list As List(Of c05G_MovingAverageDifference)
        For index_decision_center As Integer = MAIN_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            main_decision_maker_list = MainDecisionMakerCenter(index_decision_center)
            '맞는 저장된 stock을 잘라온다.
            Dim main_stock As et1_AccountManager.StoredStockType = MainForm.MainAccountManager.TakeMyStock(Code, main_decision_maker_list(0).MABase)
            If main_stock.Code <> "" Then
                '잘라온 게 있다.=> 연결한다.
                'WAITING_EXIT 인 decision maker를 찾는다
                If main_decision_maker_list(0)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Or main_decision_maker_list(0)._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE Then
                    '191001: 15:30에 done 된 경우를 대비하기 위해 done 상태도 추가되었다.
                    '지난 거래일 끝날 때와 같은 상황으로 만들어준다.
                    Dim buy_standard_price As UInteger = MainForm.MainAccountManager.NextCallPrice(main_decision_maker_list(0).TargetBuyPrice * (1 + MainForm.MainAccountManager.BuyPower), 0, MarketKind)
                    Dim stock_operator As New et2_Operation(main_decision_maker_list(0), buy_standard_price, main_stock.Quantity, MainForm.MainAccountManager, True)
                    RequestPriceinfo()      '호가 모니터링 시작
                    MainForm.MainAccountManager.DecisionHolders.Add(main_decision_maker_list(0))        'DecisionHolders 사용하는 다른 thread와 쫑날 가능성은 없다고 본다. 왜냐면 이건 init 단계에서 한 번 만 부르기 때문에 Run 타임에서 상시 불리는 것과 상호배타적이다.
                    main_decision_maker_list(0).StockOperator = stock_operator          'stock operator 연결
                    main_decision_maker_list(0).SilentLevelVolume = main_decision_maker_list(0).FallVolume * MainForm.MainAccountManager.SilentInvolvingAmountRate
                    stock_operator.BoughtAmount = main_stock.Quantity
                    stock_operator.InitPrice = main_stock.AvePrice   '살 때 InitPrice와 팔 때 InitPrice가 있는데 참조에 유의미한 Price는 팔 때 밖에 없기 때문에 아무 값이어도 상관없다.
                    stock_operator.BuyDealPrice = main_stock.AvePrice
                    stock_operator._OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    MessageLogging(Code & " 잔고 " & main_stock.Quantity & " 개 연결 성공")
                Else
                    MessageLogging("미연결 잔고: " & Code & " : " & main_stock.Quantity & " 개")
                    WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & " Main 미연결 잔고: " & Name & " " & main_stock.MA_Base & " " & main_stock.Quantity & "개")
                End If
            Else
                '잘라온 게 없다. =>잔고 없는 것은 신경 쓸 필요 없다.
            End If
        Next
#End If

        'sub 계좌
        Dim sub_decision_maker_list As List(Of c05G_MovingAverageDifference)
        For index_decision_center As Integer = SUB_NUMBER_OF_DECIDERS - 1 To 0 Step -1
            sub_decision_maker_list = SubDecisionMakerCenter(index_decision_center)
            '맞는 저장된 stock을 잘라온다.
            Dim sub_stock As et1_AccountManager.StoredStockType = MainForm.SubAccountManager.TakeMyStock(Code, sub_decision_maker_list(0).MABase)
            If sub_stock.Code <> "" Then
                '잘라온 게 있다.=> 연결한다.
                'WAITING_EXIT 인 decision maker를 찾는다
                If sub_decision_maker_list(0)._CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Or sub_decision_maker_list(0)._CurrentPhase = c050_DecisionMaker.SearchPhase.DONE Then
                    '191001: 15:30에 done 된 경우를 대비하기 위해 done 상태도 추가되었다.
                    '지난 거래일 끝날 때와 같은 상황으로 만들어준다.
                    'Dim buy_standard_price As UInteger = MainForm.SubAccountManager.NextCallPrice(sub_decision_maker_list(0).TargetBuyPrice * (1 + MainForm.SubAccountManager.BuyPower), 0, MarketKind)
                    Dim buy_standard_price As UInteger = NextCallPrice(sub_decision_maker_list(0).TargetBuyPrice * (1 + MainForm.SubAccountManager.BuyPower), 0)
                    Dim stock_operator As New et2_Operation(sub_decision_maker_list(0), buy_standard_price, sub_stock.Quantity, MainForm.SubAccountManager, True)
                    '2023.12.29 : RequestPriceinfo 는 LoadingThread 에서 WAIT_EXIT_TIME 이 있는 종목은 다 하게 되어 있으므로 아래에서는 굳이 안 해도 된다. 그래서 comment 처리
                    'RequestPriceinfo()      '호가 모니터링 시작
                    SafeEnterTrace(MainForm.SubAccountManager.StockListKey, 90)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    '등록 전에 이미 이전 등록된 중복된 종목이 있는지 확인한다.
                    Dim a As Boolean
                    a = False
                    For index As Integer = 0 To MainForm.SubAccountManager.DecisionHolders.Count - 1
                        If MainForm.SubAccountManager.DecisionHolders(index).LinkedSymbol.Code = Code AndAlso MainForm.SubAccountManager.DecisionHolders(index).EnterTime = sub_decision_maker_list(0).EnterTime Then
                            '중복이다=> 등록하지 않고, 매수하지도 않고 빠져나온다.
                            a = True
                        End If
                    Next
                    '중복 안 된 게 확인되어 등록한다.
                    If Not a Then
                        'MA 인 건 SAFE PRICE 이상일 때만 등록한다. 너무 싼 건 안전하지 않다고 판단한다. 웃기지만 상폐를 몇 번 당하다보면 이렇게라도 하게 된다.
                        If sub_decision_maker_list(0).EnterPrice > c05G_MovingAverageDifference.SAFE_PRICE Then
                            MainForm.SubAccountManager.DecisionHolders.Add(sub_decision_maker_list(0))        'DecisionHolders 사용하는 다른 thread와 쫑날 가능성은 없다고 본다. 왜냐면 이건 init 단계에서 한 번 만 부르기 때문에 Run 타임에서 상시 불리는 것과 상호배타적이다.
                        End If
                    End If
                    SafeLeaveTrace(MainForm.SubAccountManager.StockListKey, 91)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    sub_decision_maker_list(0).StockOperator = stock_operator          'stock operator 연결
                    sub_decision_maker_list(0).SilentLevelVolume = sub_decision_maker_list(0).FallVolume * MainForm.SubAccountManager.SilentInvolvingAmountRate / sub_decision_maker_list(0).ALLOWED_ENTERING_COUNT
                    stock_operator.BoughtAmount = sub_stock.Quantity
                    stock_operator.InitPrice = sub_stock.AvePrice   '살 때 InitPrice와 팔 때 InitPrice가 있는데 참조에 유의미한 Price는 팔 때 밖에 없기 때문에 아무 값이어도 상관없다.
                    stock_operator.BuyDealPrice = sub_stock.AvePrice
                    stock_operator._OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    MessageLogging(Code & " 잔고 " & sub_stock.Quantity & " 개 연결 성공")
                Else
                    Select Case Code
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
                            MessageLogging(Code & " 미연결 잔고 " & sub_stock.Quantity & " 개")
                            WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & " Sub 미연결 잔고: " & Code & ", " & Name & " " & sub_stock.MA_Base & " " & sub_stock.Quantity & "개")
                    End Select
                End If
            Else
                '잘라온 게 없다. =>잔고 없는 것은 신경 쓸 필요 없다.
            End If
        Next

    End Sub
End Class
