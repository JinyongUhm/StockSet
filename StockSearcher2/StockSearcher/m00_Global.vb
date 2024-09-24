'Compiler options
'180000, ETRADE_CONNECTION : Etrade Xing API ON
'181210, DEBUG_TOP_BOTTOM_PRICE_UPDATE : target pattern의 마지막 price를 Base Price라고 착각한 버그를 수정.
'181210, CHECK_PRE_PATTERN_STRATEGY : 매수성공률을 높이기 위한 방안으로 pre pattern 전략을 연구해보기로 함. =>버그로 인해 수익률 높게 나오는 걸 성공이라고 착각. 결론은 망했음.
'181210, MAKE_MEAN_SAME_BEFORE_GETTING_SCORE : Score 계산 전 pattern과 target의 mean을 같게 만듦
'190127, MOVING_AVERAGE_DIFFERENCE : moving average difference 전략 ON
'190128, MARKET_ENDTIME_DEBUG : 장종료시간 30분 연장된 거 뒤늦게 적용. (장종료시 열려있는 decision maker들 무시하지 않게 수정 하는 건 Moving Average Difference 전략에 default로 들어가있음. 그 이전 전략은 디폴트로 안 들어감. 그러므로 이 옵션과는 관계없음)
'190312, NO_SHOW_TO_THE_FORM : 걸린애들 폼에 표시하려니 메모리 부족하여 폼에 표시안하고 대신 리스트에 저장해두기
'190319, ALLOW_MULTIPLE_ENTERING : 진입한 후 가격이 더 떨어지면 추가 매수
'210427, DONT_FIX_THE_BUG_FOR_FLEXIBLE_FIX_20210427 : flexible pattern checker 전략 고정 전에 있던 버그 fix 할 건지 말 건지 여부
'210509, SIMULATION_PERIOD_IN_ARRAY : simulation 기간 선택을 form 에서 할 건지 test array 에서 할 건지 결정 => 2024.06.22 이후로 더 이상 안 씀
'210905, FLEXIBLE_BLUE : 파란색용 (고정된 Flexible pattern 전략) 할 때 enable 해야 빌드가 된다.
'210108, PCRENEW_LEARN => Flexible_PCRenew 전략 주기 학습시 사용됨.
'220210, DOUBLEFALL_LEARN => Double Fall 전략 주기 학습시 사용됨.
'240319, DOUBLE_GULLIN_BUG_FIX => 걸리자 마자 VI 상태가 될 때 걸린애가 중복으로 만들어지는 버그 개선. 걸리자 마자 VI 상태가 되더라도 걸린애를 포기하지 않는 걸로 바꿈.
'240506, OLD_FLEXIBLE_HAVING_TIME => PCRenew 에서 MatchedFlexIndex 에 따라 having time 을 다르게 가져가는 옛날 스타일. 지금부터는 하지 말자.
'240521, ONETIME_FALL_FILTER_DISABLED => 단 한 번의 하락으로 걸리는 놈들 filtering 하는 로직 추가 이전으로 설정
'240601, LEARN_UMEP => LEARNing with Unified having time and Multiple Entering Parameters 
'Imports DSCBO1Lib
'Imports CPUTILLib

Public Structure TracingKey
    Dim Var As Integer
    Dim Key As Integer
    Dim Time As TimeSpan
End Structure

Public Enum MARKET_KIND
    MK_NULL = 0
    MK_KOSPI = 1
    MK_KOSDAQ = 2
End Enum

Public Structure CandleStructure
    'Dim MinutesAfterStart As UInt32
    'Dim Variation5Minutes As Single
    Dim Average5Minutes As Single
    Dim Average30Minutes As Single
    Dim Average35Minutes As Single
    Dim Average70Minutes As Single
    Dim Average140Minutes As Single
    Dim Average280Minutes As Single
    Dim Average560Minutes As Single
    Dim Average1200Minutes As Single
    Dim Average2400Minutes As Single
    Dim Average4800Minutes As Single
    Dim Average9600Minutes As Single
    Dim VariationRatio As Single
    Dim CandleTime As DateTime
    Dim Open As UInt32
    Dim Close As UInt32
    Dim High As UInt32
    Dim Low As UInt32
    Dim Amount As UInt32
    Dim AccumAmount As UInt64
    Dim Test_MA_Var As Single
    'Dim Test_MA_Price As Single
End Structure

'161229: 빌드좀 어떻게 되게 해봐
Module m00_Global
    'Public SymTree As New c01_Symtree
    Public SymbolList As New List(Of c03_Symbol)
    Public MainForm As Form1
    Public MarketStartTime As Integer
#If MARKET_ENDTIME_DEBUG Then
    Public MarketEndHour As Integer
    Public MarketEndMinute As Integer
#Else
    Public MarketEndTime As Integer
#End If
    'Public MA_Base As Integer = 2
    Public PCName As String = "OCTAN"
    Public SimulationDateCollector As New List(Of Date)
    Public TraceVar As Integer
    'Public StoredMessagesForDisplay As New List(Of String)
    Public StoredMessagesKeyForDisplay As TracingKey 'Integer
    Public StoredMessagesForFileSave As New List(Of String)
    '#If Not MOVING_AVERAGE_DIFFERENCE Then
    '    Public DecisionByPattern As Boolean = True
    '#End If
    Public GangdoDB As Boolean = True
    Public MixedLearning As Boolean = True
    Public SmartLearning As Boolean = False
    Public LearnApplyRepeat As Boolean = False
    Public DateRepeat As Boolean = False
    Public SimulationStarted As Boolean = False
    Public TwoStepSearching As Boolean = False
    Public AllowMultipleEntering As Boolean = True
    Public SimulStartDate As Date = [DateTime].MinValue
    Public SimulEndDate As Date = [DateTime].MinValue
    Public WeeklyLearning As Boolean = False
    Public SecondStep As Boolean = False
    Public MAX_HAVING_LENGTH As Integer = 300
    Public Const MULTIPLE_DECIDER As Boolean = True
#If MOVING_AVERAGE_DIFFERENCE Then
    Public Const NUMBER_OF_DECIDERS As Integer = 9
#Else
    Public Const NUMBER_OF_DECIDERS As Integer = 1
#End If
    Public Const _MAX_NUMBER_OF_REQUEST As Integer = 110
    'Public Const LVSUB_YESTER_PRiCE As Integer = 2
    'Public Const LVSUB_YESTER_AMOUNT As Integer = 3
    'Public Const LVSUB_VOLUME As Integer = 4
    'Public Const LVSUB_INIT_PRICE As Integer = 5
    'Public Const LVSUB_NOW_PRICE As Integer = 6
    'Public Const LVSUB_AMOUNT As Integer = 7
    'Public Const LVSUB_GANGDO As Integer = 8
    'Public Const LVSUB_DATA_COUNT As Integer = 8
    'Public Const LVSUB_MOVING_AVERAGE_PRICE As Integer = 9
    'Public Const LVSUB_BUY_DELTA As Integer = 10
    'Public Const LVSUB_SEL_DELTA As Integer = 11
    Public Const TAX As Double = 0.003
    Public Const FEE As Double = 0.0002
    Public SILENT_INVOLVING_AMOUNT_BASE_RATE As Double = 0
    Public Const INVEST_LIMIT As Double = Double.MaxValue ' 100000000
#If PCRENEW_LEARN Then
    Public NUMBER_OF_COEFFS As Integer = 8
#ElseIf DOUBLE_FALL Then
    Public Const NUMBER_OF_COEFFS As Integer = 6
#ElseIf SHARP_LINES Then
    Public Const NUMBER_OF_COEFFS As Integer = 7
#End If
    Public PartCount() As Integer = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    Public PartMoney() As Double = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    Public PartKey As TracingKey

    Public slope_stat(6) As Integer
    Public tt_1, tt_2 As Integer

    Public DebugPrethresholdCount As Integer = 0
    Public DebugTotalGullin As Integer = 0
    Public DebugPrethresholdGullin As Integer = 0
    Public DebugPrethresholdCount_M As Integer = 0
    Public DebugNeedLogicCheck As Boolean = False

    'MA 용
#If 0 Then
    Public TestArray() As String = {
      "2018-01-01",
      "2019-01-01",
      "2019-10-01"
    }
    Public TestArray2() As String = {
      "2018-09-30",
      "2019-09-30",
      "2020-06-30"
    }
#Else
    '패턴용
#If 0 Then
    '2020.03.15 부터 2024년 4월까지 매주 과거 2주간 데이터를 학습하기 위함
    Public TestStartDateArray() As String = {
        "2020-03-29",
        "2020-04-05",
        "2020-04-12",
        "2020-04-19",
        "2020-04-26",
        "2020-05-03",
        "2020-05-10",
        "2020-05-17",
        "2020-05-24",
        "2020-05-31",
        "2020-06-07",
        "2020-06-14",
        "2020-06-21",
        "2020-06-28",
        "2020-07-05",
        "2020-07-12",
        "2020-07-19",
        "2020-07-26",
        "2020-08-02",
        "2020-08-09",
        "2020-08-16",
        "2020-08-23",
        "2020-08-30",
        "2020-09-06",
        "2020-09-13",
        "2020-09-20",
        "2020-09-27",
        "2020-10-04",
        "2020-10-11",
        "2020-10-18",
        "2020-10-25",
        "2020-11-01",
        "2020-11-08",
        "2020-11-15",
        "2020-11-22",
        "2020-11-29",
        "2020-12-06",
        "2020-12-13",
        "2020-12-20",
        "2020-12-27",
        "2021-01-03",
        "2021-01-10",
        "2021-01-17",
        "2021-01-24",
        "2021-01-31",
        "2021-02-07",
        "2021-02-14",
        "2021-02-21",
        "2021-02-28",
        "2021-03-07",
        "2021-03-14",
        "2021-03-21",
        "2021-03-28",
        "2021-04-04",
        "2021-04-11",
        "2021-04-18",
        "2021-04-25",
        "2021-05-02",
        "2021-05-09",
        "2021-05-16",
        "2021-05-23",
        "2021-05-30",
        "2021-06-06",
        "2021-06-13",
        "2021-06-20",
        "2021-06-27",
        "2021-07-04",
        "2021-07-11",
        "2021-07-18",
        "2021-07-25",
        "2021-08-01",
        "2021-08-08",
        "2021-08-15",
        "2021-08-22",
        "2021-08-29",
        "2021-09-05",
        "2021-09-12",
        "2021-09-19",
        "2021-09-26",
        "2021-10-03",
        "2021-10-10",
        "2021-10-17",
        "2021-10-24",
        "2021-10-31",
        "2021-11-07",
        "2021-11-14",
        "2021-11-21",
        "2021-11-28",
        "2021-12-05",
        "2021-12-12",
        "2021-12-19",
        "2021-12-26",
        "2022-01-02",
        "2022-01-09",
        "2022-01-16",
        "2022-01-23",
        "2022-01-30",
        "2022-02-06",
        "2022-02-13",
        "2022-02-20",
        "2022-02-27",
        "2022-03-06",
        "2022-03-13",
        "2022-03-20",
        "2022-03-27",
        "2022-04-03",
        "2022-04-10",
        "2022-04-17",
        "2022-04-24",
        "2022-05-01",
        "2022-05-08",
        "2022-05-15",
        "2022-05-22",
        "2022-05-29",
        "2022-06-05",
        "2022-06-12",
        "2022-06-19",
        "2022-06-26",
        "2022-07-03",
        "2022-07-10",
        "2022-07-17",
        "2022-07-24",
        "2022-07-31",
        "2022-08-07",
        "2022-08-14",
        "2022-08-21",
        "2022-08-28",
        "2022-09-04",
        "2022-09-11",
        "2022-09-18",
        "2022-09-25",
        "2022-10-02",
        "2022-10-09",
        "2022-10-16",
        "2022-10-23",
        "2022-10-30",
        "2022-11-06",
        "2022-11-13",
        "2022-11-20",
        "2022-11-27",
        "2022-12-04",
        "2022-12-11",
        "2022-12-18",
        "2022-12-25",
        "2023-01-01",
        "2023-01-08",
        "2023-01-15",
        "2023-01-22",
        "2023-01-29",
        "2023-02-05",
        "2023-02-12",
        "2023-02-19",
        "2023-02-26",
        "2023-03-05",
        "2023-03-12",
        "2023-03-19",
        "2023-03-26",
        "2023-04-02",
        "2023-04-09",
        "2023-04-16",
        "2023-04-23",
        "2023-04-30",
        "2023-05-07",
        "2023-05-14",
        "2023-05-21",
        "2023-05-28",
        "2023-06-04",
        "2023-06-11",
        "2023-06-18",
        "2023-06-25",
        "2023-07-02",
        "2023-07-09",
        "2023-07-16",
        "2023-07-23",
        "2023-07-30",
        "2023-08-06",
        "2023-08-13",
        "2023-08-20",
        "2023-08-27",
        "2023-09-03",
        "2023-09-10",
        "2023-09-17",
        "2023-09-24",
        "2023-10-01",
        "2023-10-08",
        "2023-10-15",
        "2023-10-22",
        "2023-10-29",
        "2023-11-05",
        "2023-11-12",
        "2023-11-19",
        "2023-11-26",
        "2023-12-03",
        "2023-12-10",
        "2023-12-17",
        "2023-12-24",
        "2023-12-31",
        "2024-01-07",
        "2024-01-14",
        "2024-01-21",
        "2024-01-28",
        "2024-02-04",
        "2024-02-11",
        "2024-02-18",
        "2024-02-25",
        "2024-03-03",
        "2024-03-10",
        "2024-03-17",
        "2024-03-24",
        "2024-03-31",
        "2024-04-07",
        "2024-04-14",
        "2024-04-21"
    }
    Public TestEndDateArray() As String = {
        "2020-04-05",
        "2020-04-12",
        "2020-04-19",
        "2020-04-26",
        "2020-05-03",
        "2020-05-10",
        "2020-05-17",
        "2020-05-24",
        "2020-05-31",
        "2020-06-07",
        "2020-06-14",
        "2020-06-21",
        "2020-06-28",
        "2020-07-05",
        "2020-07-12",
        "2020-07-19",
        "2020-07-26",
        "2020-08-02",
        "2020-08-09",
        "2020-08-16",
        "2020-08-23",
        "2020-08-30",
        "2020-09-06",
        "2020-09-13",
        "2020-09-20",
        "2020-09-27",
        "2020-10-04",
        "2020-10-11",
        "2020-10-18",
        "2020-10-25",
        "2020-11-01",
        "2020-11-08",
        "2020-11-15",
        "2020-11-22",
        "2020-11-29",
        "2020-12-06",
        "2020-12-13",
        "2020-12-20",
        "2020-12-27",
        "2021-01-03",
        "2021-01-10",
        "2021-01-17",
        "2021-01-24",
        "2021-01-31",
        "2021-02-07",
        "2021-02-14",
        "2021-02-21",
        "2021-02-28",
        "2021-03-07",
        "2021-03-14",
        "2021-03-21",
        "2021-03-28",
        "2021-04-04",
        "2021-04-11",
        "2021-04-18",
        "2021-04-25",
        "2021-05-02",
        "2021-05-09",
        "2021-05-16",
        "2021-05-23",
        "2021-05-30",
        "2021-06-06",
        "2021-06-13",
        "2021-06-20",
        "2021-06-27",
        "2021-07-04",
        "2021-07-11",
        "2021-07-18",
        "2021-07-25",
        "2021-08-01",
        "2021-08-08",
        "2021-08-15",
        "2021-08-22",
        "2021-08-29",
        "2021-09-05",
        "2021-09-12",
        "2021-09-19",
        "2021-09-26",
        "2021-10-03",
        "2021-10-10",
        "2021-10-17",
        "2021-10-24",
        "2021-10-31",
        "2021-11-07",
        "2021-11-14",
        "2021-11-21",
        "2021-11-28",
        "2021-12-05",
        "2021-12-12",
        "2021-12-19",
        "2021-12-26",
        "2022-01-02",
        "2022-01-09",
        "2022-01-16",
        "2022-01-23",
        "2022-01-30",
        "2022-02-06",
        "2022-02-13",
        "2022-02-20",
        "2022-02-27",
        "2022-03-06",
        "2022-03-13",
        "2022-03-20",
        "2022-03-27",
        "2022-04-03",
        "2022-04-10",
        "2022-04-17",
        "2022-04-24",
        "2022-05-01",
        "2022-05-08",
        "2022-05-15",
        "2022-05-22",
        "2022-05-29",
        "2022-06-05",
        "2022-06-12",
        "2022-06-19",
        "2022-06-26",
        "2022-07-03",
        "2022-07-10",
        "2022-07-17",
        "2022-07-24",
        "2022-07-31",
        "2022-08-07",
        "2022-08-14",
        "2022-08-21",
        "2022-08-28",
        "2022-09-04",
        "2022-09-11",
        "2022-09-18",
        "2022-09-25",
        "2022-10-02",
        "2022-10-09",
        "2022-10-16",
        "2022-10-23",
        "2022-10-30",
        "2022-11-06",
        "2022-11-13",
        "2022-11-20",
        "2022-11-27",
        "2022-12-04",
        "2022-12-11",
        "2022-12-18",
        "2022-12-25",
        "2023-01-01",
        "2023-01-08",
        "2023-01-15",
        "2023-01-22",
        "2023-01-29",
        "2023-02-05",
        "2023-02-12",
        "2023-02-19",
        "2023-02-26",
        "2023-03-05",
        "2023-03-12",
        "2023-03-19",
        "2023-03-26",
        "2023-04-02",
        "2023-04-09",
        "2023-04-16",
        "2023-04-23",
        "2023-04-30",
        "2023-05-07",
        "2023-05-14",
        "2023-05-21",
        "2023-05-28",
        "2023-06-04",
        "2023-06-11",
        "2023-06-18",
        "2023-06-25",
        "2023-07-02",
        "2023-07-09",
        "2023-07-16",
        "2023-07-23",
        "2023-07-30",
        "2023-08-06",
        "2023-08-13",
        "2023-08-20",
        "2023-08-27",
        "2023-09-03",
        "2023-09-10",
        "2023-09-17",
        "2023-09-24",
        "2023-10-01",
        "2023-10-08",
        "2023-10-15",
        "2023-10-22",
        "2023-10-29",
        "2023-11-05",
        "2023-11-12",
        "2023-11-19",
        "2023-11-26",
        "2023-12-03",
        "2023-12-10",
        "2023-12-17",
        "2023-12-24",
        "2023-12-31",
        "2024-01-07",
        "2024-01-14",
        "2024-01-21",
        "2024-01-28",
        "2024-02-04",
        "2024-02-11",
        "2024-02-18",
        "2024-02-25",
        "2024-03-03",
        "2024-03-10",
        "2024-03-17",
        "2024-03-24",
        "2024-03-31",
        "2024-04-07",
        "2024-04-14",
        "2024-04-21",
        "2024-04-28"
    }
#Else
    Public TestStartDateArray() As String = {
        "2018-01-01",
        "2018-02-01",
        "2018-03-01",
        "2018-04-01",
        "2018-05-01",
        "2018-06-01",
        "2018-07-01",
        "2018-08-01",
        "2018-09-01",
        "2018-10-01",
        "2018-11-01",
        "2018-12-01",
        "2019-01-01",
        "2019-02-01",
        "2019-03-01",
        "2019-04-01",
        "2019-05-01",
        "2019-06-01",
        "2019-07-01",
        "2019-08-01",
        "2019-09-01",
        "2019-10-01",
        "2019-11-01",
        "2019-12-01",
        "2020-03-01",  '두달간 디버깅으로 인한 공백
        "2020-04-01",
        "2020-05-01",
        "2020-06-01",
        "2020-07-01",
        "2020-08-01",
        "2020-09-01",
        "2020-10-01",
        "2020-11-01",
        "2020-12-01",
        "2021-01-01",
        "2021-02-01",
        "2021-03-01",
        "2021-04-01",
        "2021-05-01",
        "2021-06-01",
        "2021-07-01",
        "2021-08-01",
        "2021-09-01",
        "2021-10-01",
        "2021-11-01",
        "2021-12-01",
        "2022-01-01",
        "2022-02-01",
        "2022-03-01",
        "2022-04-01",
        "2022-05-01",
        "2022-06-01",
        "2022-07-01",
        "2022-08-01",
        "2022-09-01",
        "2022-10-01",
        "2022-11-01",
        "2022-12-01",
        "2023-01-01",
        "2023-02-01",
        "2023-03-01",
        "2023-04-01",
        "2023-05-01",
        "2023-06-01",
        "2023-07-01",
        "2023-08-01",
        "2023-09-01",
        "2023-10-01",
        "2023-11-01",
        "2023-12-01",
        "2024-01-01",
        "2024-02-01",
        "2024-03-01",
        "2024-04-01"
    }
    Public TestEndDateArray() As String = {
        "2018-01-31",
        "2018-02-28",
        "2018-03-31",
        "2018-04-30",
        "2018-05-31",
        "2018-06-30",
        "2018-07-31",
        "2018-08-31",
        "2018-09-30",
        "2018-10-31",
        "2018-11-30",
        "2018-12-31",
        "2019-01-31",
        "2019-02-28",
        "2019-03-31",
        "2019-04-30",
        "2019-05-31",
        "2019-06-30",
        "2019-07-31",
        "2019-08-31",
        "2019-09-30",
        "2019-10-31",
        "2019-11-30",
        "2019-12-31",
        "2020-03-31",  '두달간 디버깅으로 인한 공백
        "2020-04-30",
        "2020-05-31",
        "2020-06-30",
        "2020-07-31",
        "2020-08-31",
        "2020-09-30",
        "2020-10-31",
        "2020-11-30",
        "2020-12-31",
        "2021-01-31",
        "2021-02-28",
        "2021-03-31",
        "2021-04-30",
        "2021-05-31",
        "2021-06-30",
        "2021-07-31",
        "2021-08-31",
        "2021-09-30",
        "2021-10-31",
        "2021-11-30",
        "2021-12-31",
        "2022-01-31",
        "2022-02-28",
        "2022-03-31",
        "2022-04-30",
        "2022-05-31",
        "2022-06-30",
        "2022-07-31",
        "2022-08-31",
        "2022-09-30",
        "2022-10-31",
        "2022-11-30",
        "2022-12-31",
        "2023-01-31",
        "2023-02-28",
        "2023-03-31",
        "2023-04-30",
        "2023-05-31",
        "2023-06-30",
        "2023-07-31",
        "2023-08-31",
        "2023-09-30",
        "2023-10-31",
        "2023-11-30",
        "2023-12-31",
        "2024-01-31",
        "2024-02-29",
        "2024-03-31",
        "2024-04-30"
    }
#End If
#End If

    'Public TestArray() As Double
    Public TestArray() As Double = {23}
    'Public TestArray() As Double = {0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9}
    'Public TestArray() As Double = ｛3， 5， 7， 9， 11， 13， 15， 17， 19， 21， 23｝
    'Public TestArray() As Double = {59, 61, 63, 65, 67, 69, 71, 73}
    'Public TestArray() As Double = {3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49, 51, 53, 55, 57, 59}
    'Public TestArray() As Double = {1.111111111, 1.25, 1.428571429, 1.666666667, 2, 2.5, 3.333333333, 5, 10, 10000, -10, -5, -3.333333333, -2.5, -2, -1.666666667, -1.428571429, -1.25, -1.111111111, -1, -0.909090909, -0.833333333}
    'Public TestArray() As Double = {3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49, 51, 53, 55, 57, 59, 61, 63, 65, 67, 69, 71, 73, 75, 77, 79, 81}
    'Public TestArray() As Double = {0.7, 0.75, 0.8, 0.85, 0.9, 0.95, 1, 1.05, 1.1, 1.15, 1.2, 1.25, 1.3, 1.35, 1.4, 1.45, 1.5, 1.55, 1.6, 1.65, 1.7, 1.75, 1.8, 1.85, 1.9}
    Public TestArray_a() As Double = {3, 7, 11, 15, 19, 23, 27, 31, 35, 39, 43, 47, 51, 55, 59, 63, 67, 71, 75, 79, 83, 87, 91, 95, 99, 103, 109, 115, 121, 127, 133, 139, 145, 151, 157, 163}
    Public TestArray_b() As Double = {3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43, 45, 47, 49, 51, 53, 55, 57, 59, 61, 63, 65, 67, 69, 71, 73}

    Public TestIndex As Integer
    Public TestDateIndex As Integer
    Public CoefficientsAlterListLenth As Integer
    Public NumberOfCoeffsInTrial As Integer
#If CHECK_PRE_PATTERN_STRATEGY Then
    Public CountPostPatternFail As Integer = 0
#End If
    Public GlobalDataTime As New List(Of DateTime)
    Public GlobalDataCount As New List(Of Integer)

    'Madiffsca
    Public MADIFFSCA_FADE_FACTOR_DEFAULT As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA0035 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA0070 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA0140 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA0280 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA0560 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA1200 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA2400 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA4800 As Double = 0.44
    Public MADIFFSCA_FADE_FACTOR_MA9600 As Double = 0.44
    Public MADIFFSCA_DETECT_SCALE_MA0035 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA0070 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA0140 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA0280 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA0560 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA1200 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA2400 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA4800 As Double = 1.2
    Public MADIFFSCA_DETECT_SCALE_MA9600 As Double = 1.2
    Public REDUCE_ASSIGN_RATE As Double = 0.0846
    Public MADIFFSCA_A As Double = 0.027
    Public MADIFFSCA_B As Double = 16.2


    Public Sub MessageLogging(ByVal message As String)
        SafeEnterTrace(StoredMessagesKeyForDisplay, 80)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        StoredMessagesForFileSave.Add("- " & Now.TimeOfDay.ToString & " : " & message & vbCrLf)
        SafeLeaveTrace(StoredMessagesKeyForDisplay, 81)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

    End Sub
#If 0 Then
    Public Sub SafeEnter(ByRef using_flag As Integer)
        While System.Threading.Interlocked.CompareExchange(using_flag, 1, 0) = 1
            'Thread간 공유문제 발생예방
            System.Threading.Thread.Yield()
            '190806: account쪽에서 쓰는 곳 트레이스 가능하도록 바꾸고 이건 없애버리자
        End While
    End Sub

    Public Sub SafeLeave(ByRef using_flag As Integer)
        System.Threading.Interlocked.Exchange(using_flag, False) 'Thread간 공유문제 발생예방
    End Sub
#End If

    Public Sub SafeEnterTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        While System.Threading.Interlocked.CompareExchange(tracing_key.Key, 1, 0) = 1
            'Thread간 공유문제 발생예방
            'System.Threading.Thread.Yield()
            System.Threading.Thread.Sleep(1)
        End While
        tracing_key.Var = user_index
        tracing_key.Time = Now.TimeOfDay
    End Sub

    Public Sub SafeLeaveTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        tracing_key.Var = user_index
        System.Threading.Interlocked.Exchange(tracing_key.Key, False) 'Thread간 공유문제 발생예방
    End Sub

    Public Sub GlobalVarInit(ByVal main_form As Form1)
        MainForm = main_form

        'Dim cp_code_mgr As New CpCodeMgr()
        MarketStartTime = 9 'CType(cp_code_mgr.GetMarketStartTime(), Integer)
#If MARKET_ENDTIME_DEBUG Then
        MarketEndHour = 15
        MarketEndMinute = 30
#Else
        MarketEndTime = 15 ' CType(cp_code_mgr.GetMarketEndTime(), Integer)
#End If

#If MOVING_AVERAGE_DIFFERENCE Then
        SILENT_INVOLVING_AMOUNT_RATE = 0.0333        'Moving Average Difference용
#Else
        SILENT_INVOLVING_AMOUNT_BASE_RATE = 0.4
#End If
    End Sub

    Public ReadOnly Property SilentInvolvingAmountRate As Double
        Get
#If MOVING_AVERAGE_DIFFERENCE Then
                Return SILENT_INVOLVING_AMOUNT_BASE_RATE
#Else
            If AllowMultipleEntering Then
                Return SILENT_INVOLVING_AMOUNT_BASE_RATE / c050_DecisionMaker.ALLOWED_ENTERING_COUNT
            Else
                Return SILENT_INVOLVING_AMOUNT_BASE_RATE
            End If
#End If
        End Get
    End Property

    Public ReadOnly Property IsClearingTime(ByVal the_time As Date) As Boolean
        Get
            Dim current_time_of_day As TimeSpan = the_time.TimeOfDay
#If MARKET_ENDTIME_DEBUG Then
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndHour) + TimeSpan.FromMinutes(MarketEndMinute)
#Else
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndTime)
#End If

            If current_time_of_day >= market_end_time.Subtract(TimeSpan.FromMinutes(12)) AndAlso market_end_time > current_time_of_day Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsFirstHalfTime(ByVal the_time As Date) As Boolean
        Get
            Dim current_time_of_day As TimeSpan = the_time.TimeOfDay
            If current_time_of_day.Hours < 12 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

#If 0 Then
    Public ReadOnly Property NoMoreEnteringTime(ByVal the_time As Date, ByVal having_time_in_seconds As Integer) As Boolean
        Get
            Dim current_time_of_day As TimeSpan = the_time.TimeOfDay
#If MARKET_ENDTIME_DEBUG Then
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndHour) + TimeSpan.FromMinutes(MarketEndMinute)
#Else
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndTime)
#End If

            If current_time_of_day >= market_end_time.Subtract(TimeSpan.FromSeconds(having_time_in_seconds + 12 * 60)) AndAlso market_end_time > current_time_of_day Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property
#End If
    Public Function AddSymbolIfNew(ByVal symbol_code As String, ByVal symbol_name As String) As c03_Symbol
        Dim DoesSymbolExist As Boolean = False
        Dim symbol_obj As c03_Symbol = Nothing

        For index As Integer = 0 To SymbolList.Count - 1
            If SymbolList(index).Code = symbol_code Then
                DoesSymbolExist = True
                symbol_obj = SymbolList(index)
                Exit For
            End If
        Next

        If Not DoesSymbolExist Then
            'create a new symbol
            symbol_obj = New c03_Symbol(symbol_code, symbol_name)    '종목 개체 생성
            symbol_obj.DBTableExist = True
            Dim file_name As String = "history1_" & symbol_code.Substring(1, 6) & ".txt"
            If IO.File.Exists(file_name) Then
                Dim file_contents() As String = IO.File.ReadAllLines(file_name)
                'symbol_obj.MakeBasePriceHistory(file_contents)
            Else
                '파일이 없다는 것은 상장폐지
            End If
            SymbolList.Add(symbol_obj)                                     '종목리스트에 추가
            Return symbol_obj
        End If
        Return symbol_obj
    End Function

    Public Sub CollectSimulateDate(ByVal a_date As Date)
        Dim index As Integer = 0
        While index <= SimulationDateCollector.Count - 1
            If a_date < SimulationDateCollector(index) Then
                SimulationDateCollector.Insert(index, a_date)
                Exit While
            ElseIf a_date = SimulationDateCollector(index) Then
                Exit While
            End If
            index += 1
        End While
        If index = SimulationDateCollector.Count Then
            '가장 최근의 놈이다 => 마지막에 더한다
            SimulationDateCollector.Add(a_date)
        End If
    End Sub

#If 0 Then
    '시뮬레이션 속도를 높이기 위해 date collect하는 로직을 별도 thread로 돌리자
    Public Sub CollectSimulateDateByThreading(ByVal db_support_obj As c04_DBSupport)
        Dim a_date As Date
        Dim index As Integer = 0
        While db_support_obj.DateCollecting
            'SafeEnter(db_support_obj.DateListKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            'date list에서 date를 하나 추출한다
            If db_support_obj.DateListToCollect.Count > 0 Then
                a_date = db_support_obj.DateListToCollect(0)
                db_support_obj.DateListToCollect.RemoveAt(0)
            Else
                a_date = [Date].MinValue
            End If
            'SafeLeave(db_support_obj.DateListKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

            If a_date <> [Date].MinValue Then
                While index <= SimulationDateCollector.Count - 1
                    If a_date < SimulationDateCollector(index) Then
                        SimulationDateCollector.Insert(index, a_date)
                        Exit While
                    ElseIf a_date = SimulationDateCollector(index) Then
                        Exit While
                    End If
                    index += 1
                End While
                If index = SimulationDateCollector.Count Then
                    '가장 최근의 놈이다 => 마지막에 더한다
                    SimulationDateCollector.Add(a_date)
                End If
            End If
        End While

    End Sub
#End If

    Public Function NumberOfGullin(the_time As DateTime) As Integer
        Dim the_pointer As Integer = 0

        If GlobalDataTime.Count = 0 Then
            Return -1
        End If

        If the_time < GlobalDataTime(the_pointer) Then
            Return 0
        Else
            Do
                the_pointer += 1
            Loop While the_pointer < GlobalDataTime.Count AndAlso the_time >= GlobalDataTime(the_pointer)

            Return GlobalDataCount(the_pointer - 1)
        End If
    End Function

    Public Function NextCallPrice(ByVal price0 As UInt32, ByVal call_steps As Integer) As UInt32 ', ByVal market_kind As MARKET_KIND) As UInt32
        Dim current_price As UInt32 = price0
        Dim current_round_price As UInt32
        Dim next_price As UInt32
        Dim unit_step As Integer
        Dim number_of_steps As Integer

        If call_steps = 0 Then
            '이건 호가단위에 맞지 않는 가격을 호가 단위에 맞는 가격으로 만들고 싶을 때 콜 되는데 입력받은 가격을 넘지 않는 최대의 호가가격이 리턴된다.
            unit_step = 0
            number_of_steps = 1
        ElseIf call_steps > 0 Then
            unit_step = 1
            number_of_steps = call_steps
        Else
            unit_step = -1
            number_of_steps = -call_steps
        End If
        For index As Integer = 0 To number_of_steps - 1
            'If market_kind = PriceMiner.MARKET_KIND.MK_KOSPI Then
            '2023.01.27 : 25일부터 호가가격단위 (Tick Size) 변경됨. KOSPI, KOSDAQ 동일함.
            If current_price < 2000 Then
                current_round_price = current_price
            ElseIf current_price < 5000 Then
                current_round_price = ((current_price + 3) \ 5) * 5
            ElseIf current_price < 20000 Then
                current_round_price = ((current_price + 5) \ 10) * 10
            ElseIf current_price < 50000 Then
                current_round_price = ((current_price + 25) \ 50) * 50
            ElseIf current_price < 200000 Then
                current_round_price = ((current_price + 50) \ 100) * 100
            ElseIf current_price < 500000 Then
                current_round_price = ((current_price + 250) \ 500) * 500
            Else
                current_round_price = ((current_price + 500) \ 1000) * 1000
            End If

            '2023.01.27 : 호가가격단위가 달라지는 가격대 부근에서 버그가 있어 current_round_price 를 도입하고 unit_step 의 부호에 따라 부등호에 =를 넣는지 안 넣는지 결정하도록 바꿨다.
            If unit_step = 0 Then
                next_price = current_round_price
            ElseIf unit_step > 0 Then
                If current_round_price < 2000 Then
                    next_price = current_round_price + 1
                ElseIf current_round_price < 5000 Then
                    next_price = current_round_price + 5
                ElseIf current_round_price < 20000 Then
                    next_price = current_round_price + 10
                ElseIf current_round_price < 50000 Then
                    next_price = current_round_price + 50
                ElseIf current_round_price < 200000 Then
                    next_price = current_round_price + 100
                ElseIf current_round_price < 500000 Then
                    next_price = current_round_price + 500
                Else
                    next_price = current_round_price + 1000
                End If
            Else 'if unit_step < 0 Then
                If current_round_price <= 2000 Then
                    next_price = current_round_price - 1
                ElseIf current_round_price <= 5000 Then
                    next_price = current_round_price - 5
                ElseIf current_round_price <= 20000 Then
                    next_price = current_round_price - 10
                ElseIf current_round_price <= 50000 Then
                    next_price = current_round_price - 50
                ElseIf current_round_price <= 200000 Then
                    next_price = current_round_price - 100
                ElseIf current_round_price <= 500000 Then
                    next_price = current_round_price - 500
                Else
                    next_price = current_round_price - 1000
                End If
            End If

            current_price = next_price
        Next
        Return current_price
    End Function

    Public Function SafeMin(Of T As {Structure, IConvertible, IComparable(Of T)})(ByVal list_or_array As T()) As T
        Dim min_so_far As T

        If list_or_array.Count = 0 Then
            Return Nothing
        ElseIf list_or_array.Count = 1 Then
            Return list_or_array(0)
        Else
            min_so_far = list_or_array(0)
            For index As Integer = 1 To list_or_array.Count - 1
                If list_or_array(index).CompareTo(min_so_far) < 0 Then
                    min_so_far = list_or_array(index)
                End If
            Next

            Return min_so_far
        End If
    End Function

    Public Function SafeMax(Of T As {Structure, IConvertible, IComparable(Of T)})(ByVal list_or_array As T()) As T
        Dim max_so_far As T

        If list_or_array.Count = 0 Then
            Return Nothing
        ElseIf list_or_array.Count = 1 Then
            Return list_or_array(0)
        Else
            max_so_far = list_or_array(0)
            For index As Integer = 1 To list_or_array.Count - 1
                If list_or_array(index).CompareTo(max_so_far) > 0 Then
                    max_so_far = list_or_array(index)
                End If
            Next

            Return max_so_far
        End If
    End Function

End Module
