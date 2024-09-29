Imports ClassLibrary1
Imports CandleServiceInterfacePrj
Imports System.Reflection

Public MustInherit Class c050_DecisionMaker

    Public Enum SearchPhase
        INIT            '초기상태에서 이미 현재 종가가 threshold보다 더 아래인 로우컷 밑으로 내려가 있으면 계속 걸린애가 탄생하는 오류를 막기 위해 만들었다.
        WAIT_FALLING
        PRETHRESHOLDED
        WAIT_EXIT_TIME
        WAIT_SECONDFALL
#If PATTERN_PRETHRESHOLD Then
        EXITING_PRETHRESHOLD
        'UPDATING_PRETHRESHOLD
#End If
        DONE
    End Enum

    Public Enum VI_CheckStatusType
        NOT_CHECKED
        WAIT_UNLOCK
        UNLOCKED
    End Enum

    Public LinkedSymbol As c03_Symbol
    'Friend _DecisionWindowSize As Integer
    Friend _Done As Boolean
    Friend _SecondChance As Boolean = False
    Public StartTime As DateTime
    Public EnterTime As DateTime
    Public ExitTime As DateTime
    'Private _EnterPrice As Integer
    'Private _FallVolume As UInt64
    'Public _NumberOfEntering As Integer
    Public _EnterPriceMulti As New List(Of Integer)
    Public _FallVolumeMulti As New List(Of UInt64)
    Public ALLOWED_ENTERING_COUNT As Integer = 3
    Public TH_ATTENUATION As Double = 0.4
    Public VOLUME_ATTENUATION As Double = 0.3
    'Public ProfitMUlti As New List(Of Double)
    'Public _LastEnteredPoint As Integer

    Public TargetBuyPrice As UInt32
    Public ExitPrice As Integer
    Public Profit As Double
    Public TookTime As TimeSpan
    Public SilentLevelVolume As UInt64
    Public BasicIgnorableAmount As UInt32 = 0       '2024.03.28 : 매매시 앞쪽에 위치한 사소한 물량에 매매가가 민감히 반응하는 문제를 해결하기 위함.
    Public GraphicCompositeDataList As New List(Of c011_PlainCompositeData)
    Public StockOperator As et2_Operation
    Public StockOperatorDebug As Long = 1
    Friend _CurrentPhase As SearchPhase
    'Friend _CurrentPhaseKey As Integer
    Public Score As Double
    Public ScoreSave As Double
    Public ScoreA_RelTime As Single = 0.5   ' 0 < AScore < 1
    Public ScoreB_Stability As Single      ' 0 < BScore < 1
    Public ScoreC_CallPrice As Single      ' 0 < CScore < 1
    Public ScoreCFirst_CallPrice As Single      ' 0 < CScore < 1
    Public ScoreD_DepositBonus As Single
    Public ScoreE_BuyFailMinus As Single
    Public ScoreF_OperatorsMinus As Single
    'Public OperatorList As New List(Of et21_Operator)
    'Public PriceRateTrend As New List(Of Single)
    Public NoMoreOperation As Boolean = False
#If MOVING_AVERAGE_DIFFERENCE Then
    Public FALL_VOLUME_THRESHOLD As UInt64 = 33000000   '이제 이건 안 쓰인다.
#Else
    Public FALL_VOLUME_THRESHOLD As UInt64 = 400000000000
#End If
    Public FALL_VOLUME_LOWESHOLD As UInt64 = 2000000
    Public BASIC_IGNORABLE_FACTOR As UInt32 = 1000
    Public YieldForHighLevel As Boolean = False '2020.09.13: 고레벨 걸린애들을 위한 양보 여부
    'Public SavedIndexDecisionCenter As Integer
    Public _AccountCat As Integer
    Public VI_CheckStatus As VI_CheckStatusType = VI_CheckStatusType.NOT_CHECKED
    Public AccumBuyFailCount As Integer = 0
    Public FakeGullin As Boolean = False '2022.07.24: 걸리는 조건을 만족했지만 EXIT 할 때까지 패스하고 싶을 때 쓴다.
    Public IsStopBuyingCompleted As Boolean = False
    Public AllowMultipleEntering As Boolean = False

    Public MustOverride Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
    Public MustOverride Sub CreateGraphicData()
    Public MustOverride Sub GetSecondChanceInformation(ByVal old_decision_maker As c050_DecisionMaker)
    Public MustOverride Sub ClearNow(ByVal current_price As UInt32)
    Public MustOverride Sub BuyingInitiated()
    Public MustOverride Sub StopBuyingCompleted()

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        LinkedSymbol = linked_symbol
        StartTime = start_time
    End Sub

    '모은 데이타 폐기
    Public Sub Clear()

    End Sub

    Public Property EnterPrice As Integer
        Get
            'Return _EnterPriceMulti.Average
            ' weighted average
            If _EnterPriceMulti.Count <> _FallVolumeMulti.Count Then
                '이런 경우는 상정하지 않았다.
                DebugNeedLogicCheck = True
                Return _EnterPriceMulti.Average
            Else
                Dim volume_weighted_sum As Double = 0
                Dim volume_sum As Double = 0
                For index As Integer = 0 To _EnterPriceMulti.Count - 1
                    volume_weighted_sum += _EnterPriceMulti(index) * _FallVolumeMulti(index)
                    volume_sum += _FallVolumeMulti(index)
                Next
                Dim result As Double = 0
                If volume_sum = 0 Then
                    '이런 경우는 없겠지만
                    result = _EnterPriceMulti.Average
                Else
                    result = volume_weighted_sum / volume_sum
                End If
                Return result
            End If
        End Get

        Set(value As Integer)
            'If AllowMultipleEntering AndAlso _EnterPriceMulti.Count > 1 Then
            'DebugNeedLogicCheck = True
            'End If
            _EnterPriceMulti.Clear()
            _EnterPriceMulti.Add(value)
            '_LastEnteredPoint = value
            '_NumberOfEntering = 1
        End Set
    End Property

    Public ReadOnly Property LastEnterPrice As Integer
        Get
            Return _EnterPriceMulti.Last
        End Get
    End Property

    Public ReadOnly Property NumberOfEntering As Integer
        Get
            Return _EnterPriceMulti.Count
        End Get
    End Property

    Public Property FallVolume As UInt64
        Get
            Dim sum As UInt64
            For index As Integer = 0 To NumberOfEntering - 1
                sum += _FallVolumeMulti(index)
            Next

            Return sum
        End Get
        Set(value As UInt64)
            If AllowMultipleEntering AndAlso _FallVolumeMulti.Count > 1 Then
                _FallVolumeMulti.Add(value)
            Else
                If NumberOfEntering > 1 Then
                    DebugNeedLogicCheck = True
                End If
                _FallVolumeMulti.Clear()
                _FallVolumeMulti.Add(value)
            End If

            If IsMA Then
                BasicIgnorableAmount = 0
            Else
                '2024.03.28 : Fall volume 이 결정되는 순간 basic ignorable amount 를 계산한다.
                'BasicIgnorableAmount = _FallVolume / PatternLength / _EnterPrice / BASIC_IGNORABLE_FACTOR
                BasicIgnorableAmount = FallVolume / PatternLength / EnterPrice / BASIC_IGNORABLE_FACTOR
            End If
        End Set
    End Property

    '
    Public ReadOnly Property IsDone() As Boolean
        Get
            Return _Done
        End Get
    End Property

    Public ReadOnly Property IsGivenSecondChance() As Boolean
        Get
            Return _SecondChance
        End Get
    End Property

    Public ReadOnly Property CurrentPhase As SearchPhase
        Get
            Dim current_phase As SearchPhase
            current_phase = _CurrentPhase

            Return current_phase

        End Get
    End Property

    Public ReadOnly Property AccountCat As Integer
        Get
            Return _AccountCat
        End Get
    End Property

    Public ReadOnly Property IsMA As Boolean
        Get
            If _AccountCat = 1 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsFlex As Boolean
        Get
            If _AccountCat = 0 Or _AccountCat = 2 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    'Public MustOverride ReadOnly Property PatternLength As Integer
    Public ReadOnly Property PatternLength As Integer
        Get
#If 0 Then
            If TypeOf (Me) Is c05E_PatternChecker Then
                Dim me_as_pattern_decider As c05E_PatternChecker = Me
                Return me_as_pattern_decider.Pattern.Length
#End If
            If TypeOf (Me) Is c05F_FlexiblePCRenew Then
                Dim me_as_pattern_decider As c05F_FlexiblePCRenew = Me
                Return me_as_pattern_decider.Pattern.Length
            ElseIf TypeOf (Me) Is c05G_DoubleFall Then
                Dim me_as_pattern_decider As c05G_DoubleFall = Me
                Return me_as_pattern_decider.Pattern2.Length
            Else
                Return -1
            End If

        End Get
    End Property

    Public Function ProfitCalculation() As Double
        If AllowMultipleEntering Then
            'Dim profit_sum As Double = 0
            'For index As Integer = 0 To _EnterPriceMulti.Count - 1
            'profit_sum += ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * _EnterPriceMulti(index)) / _EnterPriceMulti(index)        '수익률
            'Next
            'Profit = profit_sum / _EnterPriceMulti.Count
            Dim buy_money, sell_money, tax_and_fee As Double
            Dim buy_money_sum As Double = 0
            Dim sell_money_sum As Double = 0
            Dim tax_and_fee_sum As Double = 0
            For index As Integer = 0 To NumberOfEntering - 1
                buy_money = _FallVolumeMulti(index)
                sell_money = _FallVolumeMulti(index) * ExitPrice / _EnterPriceMulti(index)
                tax_and_fee = buy_money * FEE + sell_money * FEE + sell_money * TAX
                buy_money_sum += buy_money
                sell_money_sum += sell_money
                tax_and_fee_sum += tax_and_fee
            Next
            Profit = (sell_money_sum - buy_money_sum - tax_and_fee_sum) / buy_money_sum
        Else
            Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
        End If
    End Function

End Class

Public Class c05G_MovingAverageDifference
    Inherits c050_DecisionMaker
    Public Enum MA_Base_Type
        MA_BASE_0035
        MA_BASE_0070
        MA_BASE_0140
        MA_BASE_0280
        MA_BASE_0560
        MA_BASE_1200
        MA_BASE_2400
        MA_BASE_4800
        MA_BASE_9600
    End Enum

    Public HOW_MANY_MINUTES_FOR_VOLUME_DECISION As Integer = 10
    Public DEFAULT_HAVING_MINUTE As Integer = 600
    Public HAVING_MINUTE_FROM_BASE As Integer = 600
    Public DecisionType As Integer = 2
    ' For A type decision ==============================================
    Public ENTER_THRESHOLD_FROM_MA_PRICE As Double = 0.0432 '0.0924
    Public EXIT_THRESHOLD_FROM_MA_PRICE As Double = 0.030285 '0.02
    ' ==================================================================
    ' For B type decision ==============================================
    Public ENTER_THRESHOLD_LEVEL0 As Double = 0.16
    Public ENTER_THRESHOLD_LEVEL1 As Double = 0.16
    Public ENTER_THRESHOLD_LEVEL2 As Double = 0.16
    Public ENTER_THRESHOLD_LEVEL3 As Double = 0.16
    Public EXIT_NEW_RATIO As Double = 2
    Public SPIKE_FACTOR As Single = 0 '8
    'Public VOLUME_LOMI As Integer = 18000000
    'Public VOLUME_MIHI As Integer = 79000000
    Public VOLUME_LEVEL0 As Integer = 18000000
    Public VOLUME_LEVEL1 As Integer = 79000000
    Public VOLUME_LEVEL2 As Integer = 160000000
    ' ==================================================================

    Public NEWSTAB_EFFECT As Double = 10 ^ 30
    Public LOW_CUT_FROM_MA_PRICE As Double = 1
    Public ENTER_POWER As Double = 0
    Public CUT_RELATIVE_FALL As Double
    Public Shared SAFE_PRICE As UInt32 = 27000
    'Public EXIT_DIV As Double = 2
    'Public EXIT_MUL As Double = 0.1
    ' Public FV_AD1 As Double = 0.01
    'Public FV_AD2 As Double = 0.4
    'Public MADE_A As Double = 0
    'Public MADE_B As Double = 0
    'Public MADE_C As Double = 0
    'Public MADE_D As Double = 0.04
    'Public DCO As Double = 100

    Public MinutesPassed As Integer = 0
    Public IsDisplayed As Boolean = False
    Public UsedThreshold As Single
    Private _BasePrice As UInteger = 0
    Public IsPriceInfoRequested As Boolean = False
    Private _FallMinutePassed As Integer = 0
    Private _RiseMinutePassed As Integer = 0
    Private _CountFromLastBase As Integer = 0
    Public EnterMAFallRate As Single
    Public EnterMAPrice As UInteger = 0
    Public MABase As MA_Base_Type = MA_Base_Type.MA_BASE_2400
    Public OnlyAllowHighNewstab As Boolean = False
    Public RelativeFall As Double = 0
    'Public DaysPassed As Integer = 0
    'Public EnterPoint As Integer = 0
    'Public EXIT_THRESHOLD_FROM_MA_PRICE As Double = ENTER_THRESHOLD_FROM_MA_PRICE / 2
    'Public AdaptiveThreshold As Double
    'Public AdaptiveExitMul As Double = 0.5
    'Public MaxDepth As Double = [Double].MinValue
    'Private HighPricePointList, LowPricePointList, MA_VarPointList, MA_PricePointList, DecideTimePointList, CandleTimePointList As PointList
    '2024.08.06 : 메모리 다이어트의 일환으로 그래프에서 비교적 쓸모 없는 HighPrice 와 MA_Var 를 없애기로 한다.
    Private LowPricePointList, MA_PricePointList, DecideTimePointList, CandleTimePointList As PointList
    'Private HighPriceCompositeData, LowPriceCompositeData, MA_VarCompositeData, MA_PriceCompositeData, DecideTimeCompositeData, CandleTimeCompositeData As c011_PlainCompositeData
    Private LowPriceCompositeData, MA_PriceCompositeData, DecideTimeCompositeData, CandleTimeCompositeData As c011_PlainCompositeData

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer, ByVal account_cat As Integer) ', ByVal index_decision_center As Integer)
        MyBase.New(linked_symbol, start_time)

        _AccountCat = account_cat
        AllowMultipleEntering = False
        ALLOWED_ENTERING_COUNT = 1
        If account_cat = 0 Then
            'main
            DecisionType = 2
            MABase = MA_Base_Type.MA_BASE_0035
            DEFAULT_HAVING_MINUTE = 135
            HAVING_MINUTE_FROM_BASE = 16
            ENTER_THRESHOLD_LEVEL0 = 0.028
            ENTER_THRESHOLD_LEVEL1 = 0.06
            ENTER_THRESHOLD_LEVEL2 = 0.1
            ENTER_THRESHOLD_LEVEL3 = 0.1
            OnlyAllowHighNewstab = False
        ElseIf account_cat = 1 Then
            'sub
            Select Case index_decision_center
                Case 0
                    MABase = MA_Base_Type.MA_BASE_0035
                    DEFAULT_HAVING_MINUTE = 2000 '135
                    HAVING_MINUTE_FROM_BASE = 2000 '135 '16
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 1 '0.0432
                    ENTER_THRESHOLD_LEVEL1 = 1 '0.0432
                    ENTER_THRESHOLD_LEVEL2 = 1 '0.0432
                    ENTER_THRESHOLD_LEVEL3 = 1 '0.0432
                    OnlyAllowHighNewstab = True
                 'EXIT_RATIO = 1.428571
                Case 1
                    MABase = MA_Base_Type.MA_BASE_0070
                    DEFAULT_HAVING_MINUTE = 2000 '150
                    HAVING_MINUTE_FROM_BASE = 2000 '150 '16
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 1 '0.05
                    ENTER_THRESHOLD_LEVEL1 = 1 '0.05
                    ENTER_THRESHOLD_LEVEL2 = 1 '0.05
                    ENTER_THRESHOLD_LEVEL3 = 1 '0.05
                    OnlyAllowHighNewstab = True
                    'EXIT_RATIO = -2.5
                Case 2
                    MABase = MA_Base_Type.MA_BASE_0140
                    DEFAULT_HAVING_MINUTE = 2000 '150
                    HAVING_MINUTE_FROM_BASE = 2000 '150 '16
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 1 '0.057
                    ENTER_THRESHOLD_LEVEL1 = 1 '0.057
                    ENTER_THRESHOLD_LEVEL2 = 1 '0.057
                    ENTER_THRESHOLD_LEVEL3 = 1 '0.057
                    OnlyAllowHighNewstab = True
                    'EXIT_RATIO = -10
                Case 3
                    MABase = MA_Base_Type.MA_BASE_0280
                    DEFAULT_HAVING_MINUTE = 2000 '150
                    HAVING_MINUTE_FROM_BASE = 2000 '150 '16
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 1 '0.065
                    ENTER_THRESHOLD_LEVEL1 = 1 '0.065
                    ENTER_THRESHOLD_LEVEL2 = 1 '0.065
                    ENTER_THRESHOLD_LEVEL3 = 1 '0.065
                    OnlyAllowHighNewstab = True
                    'EXIT_RATIO = -0.97222
                Case 4
                    MABase = MA_Base_Type.MA_BASE_0560
                    DEFAULT_HAVING_MINUTE = 2000 '150
                    HAVING_MINUTE_FROM_BASE = 2000 '150 '70
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 1 '0.075
                    ENTER_THRESHOLD_LEVEL1 = 1 '0.075
                    ENTER_THRESHOLD_LEVEL2 = 1 '0.075
                    ENTER_THRESHOLD_LEVEL3 = 1 '0.075
                    OnlyAllowHighNewstab = False
                    'EXIT_RATIO = -0.69444
                Case 5
                    MABase = MA_Base_Type.MA_BASE_1200
                    DEFAULT_HAVING_MINUTE = 2000 '160
                    HAVING_MINUTE_FROM_BASE = 2000 '160 '140
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 1 '0.09
                    ENTER_THRESHOLD_LEVEL1 = 1 '0.09
                    ENTER_THRESHOLD_LEVEL2 = 1 '0.09
                    ENTER_THRESHOLD_LEVEL3 = 1 '0.09
                    OnlyAllowHighNewstab = False
                    'EXIT_RATIO = -1.25
                Case 6
                    MABase = MA_Base_Type.MA_BASE_2400
                    DEFAULT_HAVING_MINUTE = 3000 '600
                    HAVING_MINUTE_FROM_BASE = 3000 '600
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 0.12
                    ENTER_THRESHOLD_LEVEL1 = 0.12
                    ENTER_THRESHOLD_LEVEL2 = 0.12
                    ENTER_THRESHOLD_LEVEL3 = 0.12
                    OnlyAllowHighNewstab = False
                    CUT_RELATIVE_FALL = 0.01
                    'EXIT_RATIO = -0.69444
                Case 7
                    MABase = MA_Base_Type.MA_BASE_4800
                    DEFAULT_HAVING_MINUTE = 4000
                    HAVING_MINUTE_FROM_BASE = 4000
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 0.15
                    ENTER_THRESHOLD_LEVEL1 = 0.15
                    ENTER_THRESHOLD_LEVEL2 = 0.15
                    ENTER_THRESHOLD_LEVEL3 = 0.15
                    OnlyAllowHighNewstab = False
                    CUT_RELATIVE_FALL = 0.011
                    'EXIT_RATIO = -10
                Case 8
                    MABase = MA_Base_Type.MA_BASE_9600
                    DEFAULT_HAVING_MINUTE = 6000
                    HAVING_MINUTE_FROM_BASE = 6000
                    DecisionType = 2
                    ENTER_THRESHOLD_LEVEL0 = 0.18
                    ENTER_THRESHOLD_LEVEL1 = 0.18
                    ENTER_THRESHOLD_LEVEL2 = 0.18
                    ENTER_THRESHOLD_LEVEL3 = 0.18
                    OnlyAllowHighNewstab = False
                    CUT_RELATIVE_FALL = 0.018
                    'EXIT_RATIO = -10
            End Select
#If 0 Then
            '2021년 3월 15일부터 5월 10일까지 했던 전략
            MABase = MA_Base_Type.MA_BASE_1200
            DEFAULT_HAVING_MINUTE = 3000
            HAVING_MINUTE_FROM_BASE = 3000
            ENTER_THRESHOLD_LO = 0.19
            ENTER_THRESHOLD_MI = 0.19 '0.06
            ENTER_THRESHOLD_HI = 0.19 '0.1
            EXIT_RATIO = 2
            BTypeDecision = True
            OnlyAllowHighNewstab = False
#End If
#If 0 Then
            '2021년 1월 22일부터 3월 12일까지 했던 전략
            MABase = MA_Base_Type.MA_BASE_1200
            DEFAULT_HAVING_MINUTE = 160
            HAVING_MINUTE_FROM_BASE = 140
            BTypeDecision = True
            ENTER_THRESHOLD_LO = 0.09
            ENTER_THRESHOLD_MI = 0.09
            ENTER_THRESHOLD_HI = 0.09
            OnlyAllowHighNewstab = False
#End If
        Else
            'nothing
        End If

        _CurrentPhase = SearchPhase.INIT
        '        FALL_VOLUME_THRESHOLD = 33000000

        Dim x_data_spec, y_data_spec As c00_DataSpec

        'High가격 CopositeData
        'x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("High가격", DataType.REAL_NUMBER_DATA, Nothing)
        'HighPriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "High가격")
        'HighPricePointList = New PointList()
        'HighPriceCompositeData.SetData(HighPricePointList)

        'Low가격 CopositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("Low가격", DataType.REAL_NUMBER_DATA, Nothing)
        LowPriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "Low가격")
        LowPricePointList = New PointList()
        LowPriceCompositeData.SetData(LowPricePointList)

        'MA_Var CopositeData
        'x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("MA_Var", DataType.REAL_NUMBER_DATA, Nothing)
        'MA_VarCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "MA_Var")
        'MA_VarPointList = New PointList()
        'MA_VarCompositeData.SetData(MA_VarPointList)

        'MA_Price CopositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("MA_Price", DataType.REAL_NUMBER_DATA, Nothing)
        MA_PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "MA_Price")
        MA_PricePointList = New PointList()
        MA_PriceCompositeData.SetData(MA_PricePointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'CandleTime CompositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("CandleTime", DataType.REAL_NUMBER_DATA, Nothing)
        CandleTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "CandleTime")
        CandleTimePointList = New PointList()
        CandleTimeCompositeData.SetData(CandleTimePointList)

        'GraphicCompositeDataList 만들기
        'GraphicCompositeDataList.Add(HighPriceCompositeData)
        GraphicCompositeDataList.Add(LowPriceCompositeData)
        'GraphicCompositeDataList.Add(MA_VarCompositeData)
        GraphicCompositeDataList.Add(MA_PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
        GraphicCompositeDataList.Add(CandleTimeCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)

    End Sub

    Public Overrides Sub CreateGraphicData()
        Dim end_time As TimeSpan = LinkedSymbol.CandleServiceCenter.LastCandle().CandleTime.TimeOfDay
        Dim a_point As PointF
        Dim min_price As Double = [Double].MaxValue
        Dim max_price As Double = [Double].MinValue

        '이전에 데이터들을 삭제한다.
        'HighPricePointList.Clear()
        LowPricePointList.Clear()
        'MA_VarPointList.Clear()
        MA_PricePointList.Clear()
        CandleTimePointList.Clear()
        DecideTimePointList.Clear()

        '그 날 처음부터 걸리고 청산할 때까지 다 만든다.
        '191130: 이러니까 너무 메모리 많이 먹어서 문제다. 앞에 어느 정도 자르자
        Dim start_index As Integer = Math.Max(0, LinkedSymbol.CandleServiceCenter.CandleCount() - MinutesPassed - 381)
        For index As Integer = start_index To LinkedSymbol.CandleServiceCenter.CandleCount() - 1
            If LinkedSymbol.CandleServiceCenter.Candle(index).Amount = 0 Then
                '체결가가 없으니 High, Low 가격 모두 Last 가격으로 한다.
                a_point = New PointF(index, LinkedSymbol.CandleServiceCenter.Candle(index).Close)
                'HighPricePointList.Add(a_point)
                LowPricePointList.Add(a_point)
            Else
                '체결가가 있으니 기록된 High, Low 가격으로 한다.
                a_point = New PointF(index, LinkedSymbol.CandleServiceCenter.Candle(index).High)
                'HighPricePointList.Add(a_point)
                a_point = New PointF(index, LinkedSymbol.CandleServiceCenter.Candle(index).Low)
                LowPricePointList.Add(a_point)
                min_price = Math.Min(min_price, LinkedSymbol.CandleServiceCenter.Candle(index).Low)
                max_price = Math.Max(max_price, LinkedSymbol.CandleServiceCenter.Candle(index).High)
            End If
            'a_point = New PointF(index, LinkedSymbol.CandleServiceCenter.Candle(index).Test_MA_Var)
            a_point = New PointF(index, 0)
            If a_point.Y <> -1 Then
                'MA_VarPointList.Add(a_point)
            End If
            a_point = New PointF(index, MA_PriceInThisContext(LinkedSymbol.CandleServiceCenter.Candle(index)))
            If a_point.Y <> -1 Then
                MA_PricePointList.Add(a_point)
            End If
            a_point = New PointF(index, LinkedSymbol.CandleServiceCenter.Candle(index).CandleTime.TimeOfDay.Hours * 100 + LinkedSymbol.CandleServiceCenter.Candle(index).CandleTime.TimeOfDay.Minutes)
            CandleTimePointList.Add(a_point)
        Next
        '190918: 아 이상해.. 왜 어떤 때는 decide time point 가 뒤로 7분씩 밀려 있어..
        DecideTimePointList.Add(New PointF(LinkedSymbol.CandleServiceCenter.CandleCount() - 1 - MinutesPassed, min_price))
        DecideTimePointList.Add(New PointF(LinkedSymbol.CandleServiceCenter.CandleCount() - 1 - MinutesPassed + 0.001, max_price))
        DecideTimePointList.Add(New PointF(LinkedSymbol.CandleServiceCenter.CandleCount() - 1, max_price))
        DecideTimePointList.Add(New PointF(LinkedSymbol.CandleServiceCenter.CandleCount() - 1 + 0.001, min_price))
        '190211: 걸린애 되고 DONE 하고나면 candle list 를 symbol 에서 복사해 오는 코드를 짜야겠다.
    End Sub

    Public Sub CandleArrived(ByVal a_candle As CandleStructure)
        Dim number_of_updated_candles_container As c03_Symbol.SymbolRecord
        number_of_updated_candles_container.CoreRecord.Price = 1
        DataArrived(number_of_updated_candles_container)
    End Sub

    Public ReadOnly Property MA_PriceInThisContext(ByVal the_candle As CandleStructure) As Single
        Get
            Select Case MABase
                Case MA_Base_Type.MA_BASE_0035
                    'Return the_candle.Average35Minutes
                Case MA_Base_Type.MA_BASE_0070
                    'Return the_candle.Average70Minutes
                Case MA_Base_Type.MA_BASE_0140
                    'Return the_candle.Average140Minutes
                Case MA_Base_Type.MA_BASE_0280
                    'Return the_candle.Average280Minutes
                Case MA_Base_Type.MA_BASE_0560
                    'Return the_candle.Average560Minutes
                Case MA_Base_Type.MA_BASE_1200
                    'Return the_candle.Average1200Minutes
                Case MA_Base_Type.MA_BASE_2400
                    Return the_candle.Average2400Minutes
                Case MA_Base_Type.MA_BASE_4800
                    Return the_candle.Average4800Minutes
                Case MA_Base_Type.MA_BASE_9600
                    Return the_candle.Average9600Minutes
                Case Else
            End Select
        End Get
    End Property


    Public Overrides Sub DataArrived(number_of_updated_candles_container As c03_Symbol.SymbolRecord)
        '190206: a_data 를 실제로 사용하는 곳이 ClearNow 외에는 없다. 그러므로 Candle Chart 새로 받았을 때 이것을 콜하는 것도(수정은 조금 필요할 듯) 괜찮을 듯 싶다.
        '190610:DONE a_data 실제 사용처는 없다. 그래도 부모 클래스에서 정해진 거라 없앨 순 없고, number of candles updated 를 parameter로 써야 하는데, 새로 parameter를 만들기 어려우니
        '190610:DONE a_data 를 고쳐서 number of candles updated를 받아오는 container로 활용하도록 한다.
        '190610: 끝날 때 WAIT_EXIT_TIME  인 decision maker들을 xml로 저장하는 로직을 먼저 개발하자.
        Dim number_of_updated_candles As Integer = number_of_updated_candles_container.CoreRecord.Price
        Dim time_over_clearing As Boolean = False

        If LinkedSymbol.CandleServiceCenter.CandleCount() = 0 Then
            'Record data 갯수가 하나인 경우는 아직 MinuteCandleSeries가 안 만들어졌으므로 그냥 나감
            Return
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = LinkedSymbol.CandleServiceCenter.LastCandle().CandleTime ' StartTime + TimeSpan.FromMinutes(LinkedSymbol.CandleServiceCenter.CandleCount() - 1)
        Dim last_candle As CandleStructure = LinkedSymbol.CandleServiceCenter.LastCandle()
        'Dim lastlast_candle As CandleStructure
        'If LinkedSymbol.CandleServiceCenter.CandleCount() > 1 Then
        'lastlast_candle = LinkedSymbol.MinuteCandleSeries.Candle(LinkedSymbol.CandleServiceCenter.CandleCount() - 2)
        'Else
        'lastlast_candle = last_candle
        'End If

        '190510: clearing time 일 때 청산은 아니더라도 주문 나간거 취소는 필요할 듯 =>마켓타임일 때만 operator 객체에 200ms tick 공급하는 방법으로 문제 해결
#If 0 Then  '날짜 넘어가기 기술이 개발될 때까지는 enable해둔다.
        If IsClearingTime(current_time) AndAlso MainForm.IsLoadingDone Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
                '청산이 필요한 상태면 이대로 그냥 청산함.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = current_time
                ExitPrice = last_candle.Close
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = ExitTime - EnterTime
                _Done = True
                PartCount(PartIndex) += 1
                PartMoney(PartIndex) += Profit
            Else
                _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
            End If
        End If
#End If
        Dim MA_price As Single = MA_PriceInThisContext(last_candle)
        'MA_price = LinkedSymbol.GetAverage4800Minutes(0)
        If _CurrentPhase = SearchPhase.INIT Then
            'If last_candle.Close >= MA_price Then
            If MA_price - last_candle.Close < ENTER_THRESHOLD_LEVEL0 * MA_price / 2 Then
                _CurrentPhase = SearchPhase.WAIT_FALLING
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            If LinkedSymbol.Newstab > 0.1 Then 'Stability > -5 Then 'If last_candle.Test_MA_Var > 0.007 Then
                'ENTER_THRESHOLD_FROM_MA_PRICE = 0.015
                'DEFAULT_HAVING_MINUTE = 45
                'PartIndex = 0
            ElseIf LinkedSymbol.Newstab > 0.08 Then 'Stability > -6.5 Then 'ElseIf last_candle.Test_MA_Var > 0.004 Then
                'ENTER_THRESHOLD_FROM_MA_PRICE = 0.02
                'DEFAULT_HAVING_MINUTE = 60
                'PartIndex = 1
            ElseIf LinkedSymbol.Newstab > 0.06 Then 'Stability > -8 Then 'ElseIf last_candle.Test_MA_Var > 0.002 Then
                'ENTER_THRESHOLD_FROM_MA_PRICE = 0.025
                'DEFAULT_HAVING_MINUTE = 75
                'PartIndex = 2
            ElseIf LinkedSymbol.Newstab > 0.04 Then 'Stability > -9.5 Then 'ElseIf last_candle.Test_MA_Var > 0.001 Then
                'ENTER_THRESHOLD_FROM_MA_PRICE = 0.03
                'DEFAULT_HAVING_MINUTE = 90
                'PartIndex = 3
            ElseIf LinkedSymbol.Newstab > 0.02 Then 'Stability > -11 Then 'ElseIf last_candle.Test_MA_Var > 0.0 Then
                'ENTER_THRESHOLD_FROM_MA_PRICE = 0.035
                'DEFAULT_HAVING_MINUTE = 105
                'PartIndex = 4
            Else
                'ENTER_THRESHOLD_FROM_MA_PRICE = 0.03
                'DEFAULT_HAVING_MINUTE = 30
                'PartIndex = 5
            End If
#If 0 Then
            If last_candle.Test_MA_Var > 0.007 Then
                ENTER_THRESHOLD_HI = 0.09
                ENTER_THRESHOLD_MI = 0.09
                ENTER_THRESHOLD_LO = 0.09
                PartIndex = 0
            ElseIf last_candle.Test_MA_Var > 0.004 Then
                ENTER_THRESHOLD_HI = 0.09
                ENTER_THRESHOLD_MI = 0.09
                ENTER_THRESHOLD_LO = 0.09
                PartIndex = 1
            ElseIf last_candle.Test_MA_Var > 0.002 Then
                ENTER_THRESHOLD_HI = 0.09
                ENTER_THRESHOLD_MI = 0.09
                ENTER_THRESHOLD_LO = 0.09
                PartIndex = 2
            ElseIf last_candle.Test_MA_Var > 0.001 Then
                ENTER_THRESHOLD_HI = 0.09
                ENTER_THRESHOLD_MI = 0.09
                ENTER_THRESHOLD_LO = 0.09
                PartIndex = 3
            ElseIf last_candle.Test_MA_Var > 0.0 Then
                ENTER_THRESHOLD_HI = 0.09
                ENTER_THRESHOLD_MI = 0.09
                ENTER_THRESHOLD_LO = 0.09
                PartIndex = 4
            Else
                ENTER_THRESHOLD_HI = 0.09
                ENTER_THRESHOLD_MI = 0.09
                ENTER_THRESHOLD_LO = 0.09
                PartIndex = 5
            End If
#End If
            '하락 기다리기 모드
            Dim fall_volume As Double = LinkedSymbol.MA_Amount(HOW_MANY_MINUTES_FOR_VOLUME_DECISION) * last_candle.Close
            Dim adaptive_threshold As Double
            ' adaptive_threshold = ENTER_THRESHOLD_FROM_MA_PRICE + FV_AD1 * (fall_volume / 10 ^ 11) ^ FV_AD2
            'adaptive_threshold = Math.Max(ENTER_THRESHOLD_FROM_MA_PRICE, ENTER_POWER * last_candle.VariationRatio)
            'If last_candle.Test_MA_Var = -1 Then
            'AdaptiveThreshold = 100     'imposible
            'Else
            'AdaptiveThreshold = MADE_A * last_candle.Test_MA_Var * LinkedSymbol.AmountVar + MADE_B * last_candle.Test_MA_Var + MADE_C * LinkedSymbol.AmountVar + MADE_D
            'End If

            'If AdaptiveThreshold < 0.02 Then
            '이렇게 작은 값으로 하려니 안 하는 게 낫다 (수수료, 세금 떼면 없다)
            'AdaptiveThreshold = 100     'impossible
            'End If
#If 0 Then
            Dim ma_base_in_use As Integer = 35
            Dim MA_price As Single = LinkedSymbol.GetAverage35Minutes(0)
            adaptive_threshold = Math.Log10(fall_volume / 1000000) * THRESH_VOL_A + THRESH_VOL_B
#End If
#If 0 Then
            Dim ma_base_in_use As Integer
            Dim MA_price As Single
            If fall_volume < 18000000 Then
                ma_base_in_use = 35
                MA_price = LinkedSymbol.GetAverage35Minutes(0)
                adaptive_threshold = Math.Max(ENTER_THRESHOLD_FROM_MA_PRICE35, ENTER_POWER * last_candle.VariationRatio)
            ElseIf fall_volume < 79000000 Then
                ma_base_in_use = 70
                MA_price = LinkedSymbol.GetAverage35Minutes(0)
                adaptive_threshold = Math.Max(ENTER_THRESHOLD_FROM_MA_PRICE70, ENTER_POWER * last_candle.VariationRatio)
            Else
                ma_base_in_use = 140
                MA_price = LinkedSymbol.GetAverage35Minutes(0)
                adaptive_threshold = Math.Max(ENTER_THRESHOLD_FROM_MA_PRICE140, ENTER_POWER * last_candle.VariationRatio)
            End If
#End If
            'Dim MA_price As Single = LinkedSymbol.GetAverage4800Minutes(0)
            Dim enter_gulin As Boolean
            If DecisionType = 1 Then
                'A type decision (original)
                adaptive_threshold = ENTER_THRESHOLD_FROM_MA_PRICE
                If last_candle.Amount > 0 AndAlso MA_price <> -1 AndAlso MA_price - last_candle.Close > adaptive_threshold * MA_price AndAlso fall_volume > FALL_VOLUME_LOWESHOLD AndAlso fall_volume < FALL_VOLUME_THRESHOLD AndAlso ((Not OnlyAllowHighNewstab) OrElse LinkedSymbol.Newstab > 0.04) Then ' AndAlso (LinkedSymbol.Newstab >= 0 OrElse AccountCat = 0 OrElse MainForm.CandleLoadNotFinishedYet) Then
                    enter_gulin = True
                Else
                    enter_gulin = False
                End If
            ElseIf DecisionType = 2 Then
                'B type decision
                If LinkedSymbol.DailyAmountStore.Count = 0 Then
                    RelativeFall = -1
                ElseIf LinkedSymbol.DailyAmountStore.Count = 1 Then
                    RelativeFall = LinkedSymbol.MA_Amount(HOW_MANY_MINUTES_FOR_VOLUME_DECISION) / LinkedSymbol.DailyAmountStore.Last
                Else 'If LinkedSymbol.DailyAmountStore.Count > 1 Then
                    RelativeFall = LinkedSymbol.MA_Amount(HOW_MANY_MINUTES_FOR_VOLUME_DECISION) / (LinkedSymbol.DailyAmountStore.Last - LinkedSymbol.DailyAmountStore(LinkedSymbol.DailyAmountStore.Count - 2))
                End If
                If fall_volume < VOLUME_LEVEL0 Then
                    adaptive_threshold = ENTER_THRESHOLD_LEVEL0 - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                ElseIf fall_volume < VOLUME_LEVEL1 Then
                    adaptive_threshold = ENTER_THRESHOLD_LEVEL1 - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                ElseIf fall_volume < VOLUME_LEVEL2 Then
                    adaptive_threshold = ENTER_THRESHOLD_LEVEL2 - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                Else
                    adaptive_threshold = ENTER_THRESHOLD_LEVEL3 - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                End If
                If last_candle.Amount > 0 AndAlso MA_price - last_candle.Close > adaptive_threshold * MA_price AndAlso fall_volume > FALL_VOLUME_LOWESHOLD AndAlso ((Not OnlyAllowHighNewstab) OrElse LinkedSymbol.Newstab > 0.04) Then
                    '20220710: RelativeFall 에 따른 EXIT_NEW_RATIO의 결정
                    Dim fall_index = Math.Log(RelativeFall * 100000, 2)
                    Dim amp = 0.22
                    Dim lag = 0.3
                    Dim yshift = -0.3
                    Dim xshift = 0

                    EXIT_NEW_RATIO = amp * (2 / (1 + Math.Exp(-(fall_index - xshift) * lag)) + yshift)
                    EXIT_NEW_RATIO = Math.Min(EXIT_NEW_RATIO, 1)

                    enter_gulin = True
#If 0 Then
                    '2022.07.25: CUT_RELATIVE_FALL 은 큰 relative fall 에서 대폭 하락에 의한 손실을 막기 위한 것이다. 하지만 이것은 투자금이 클 때만 효용이 있을 것으로 보이고
                    '2022.07.25: 현재 10만원 미만으로의 투자에서는 효용이 극히 미미할 것으로 판단된다. 일단 평균수익률이 좋기 때문에 더 만은 걸린애들을 만들기 위해 CUT_RELATIVE_FALL 에 의한 FakeGullin 은 당분간(아마 꽤 오랫동안) 사용하지 않기로 한다.
                    If RelativeFall >= CUT_RELATIVE_FALL Then
                        '2022.07.24: 걸렸지만 자르기로 결정한 경우임. 아래 플래그를 셋해서 EXIT 했을 때 계산에 포함되지 않게 해야 한다.
                        FakeGullin = True
                    End If
#End If
                Else
                    enter_gulin = False
                End If
            End If
            If enter_gulin AndAlso Not MainForm.MemoryCautionIssued Then
                '20200510: 투자유의 종목은 진입 안 되게 막았다.
                'If last_candle.Amount > 0 AndAlso lastlast_candle.Test_MA_Price - lastlast_candle.Close > adaptive_threshold * lastlast_candle.Test_MA_Price AndAlso lastlast_candle.Test_MA_Price - lastlast_candle.Close > last_candle.Test_MA_Price - last_candle.Close Then
                '이러면 걸린애 된다.    날짜 넘기는 기술개발되면 NoMoreEnteringTime은 지우자.
                If AccountCat = 0 Then
                    If LinkedSymbol.Main_MADecisionFlag Then
                        '20200913: 이거는 이미 고레벨에서 진입했다는 거다. 저레벨에서는 진입이 금지되고, 기다리는 상태가 된다.
                        YieldForHighLevel = True
                    Else
                        '20200914: 이것보다 아래 레벨에서 진입하지 못하게 플래그를 셋해둔다.
                        LinkedSymbol.Main_MADecisionFlag = True
                    End If
                Else 'If AccountCat = 1 Then
                    If LinkedSymbol.Sub_MADecisionFlag Then
                        '20200913: 이거는 이미 고레벨에서 진입했다는 거다. 저레벨에서는 진입이 금지되고, 기다리는 상태가 된다.
                        YieldForHighLevel = True
                    Else
                        '20200914: 이것보다 아래 레벨에서 진입하지 못하게 플래그를 셋해둔다.
                        LinkedSymbol.Sub_MADecisionFlag = True
                    End If
                End If
                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                MinutesPassed = 0
                'UsedMABase = ma_base_in_use
                UsedThreshold = adaptive_threshold
                EnterTime = current_time
                _BasePrice = last_candle.Close
                _FallMinutePassed = 0
                _RiseMinutePassed = 0
                'EnterPoint = LinkedSymbol.CandleServiceCenter.CandleCount() - 1
                EnterPrice = last_candle.Close '(1 - ENTER_THRESHOLD_FROM_MA_PRICE) * last_candle.Test_MA_Price
                FallVolume = LinkedSymbol.MA_Amount(HOW_MANY_MINUTES_FOR_VOLUME_DECISION) * EnterPrice      'MovingAverageDifference 전략에서 FallVolume은 분당평균을 의미함

                'AdaptiveExitMul = 1.5 / Math.Exp(0.03 * LinkedSymbol.AmountVar) - 1
                EnterMAFallRate = (2 * MA_PriceInThisContext(LinkedSymbol.CandleServiceCenter.Candle(LinkedSymbol.CandleServiceCenter.CandleCount() - 1)) - MA_PriceInThisContext(LinkedSymbol.CandleServiceCenter.Candle(LinkedSymbol.CandleServiceCenter.CandleCount() - 3)) - MA_PriceInThisContext(LinkedSymbol.CandleServiceCenter.Candle(LinkedSymbol.CandleServiceCenter.CandleCount() - 2))) / 2 / MA_PriceInThisContext(LinkedSymbol.CandleServiceCenter.Candle(LinkedSymbol.CandleServiceCenter.CandleCount() - 1))
                EnterMAPrice = MA_price
                '190526: load past candle 할 때는 RequetPriceinfo 하지 않기로 하자
                'TargetBuyPrice 설정 , 190816: IsLoadingDone if절 안쪽에 있다가 밖으로 나왔다.LoadPastCandle할 때도 필요하기 때문이다.
                Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                '190918: VI일 때 TargetBuyPrice가 너무 높게되는 수가 있다. 수정이 좀 필요하겠다.
                'TargetBuyPrice = Math.Max(MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, LinkedSymbol.MarketKind), EnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                TargetBuyPrice = LastEnterPrice     'Moving average 전략은 1분마다 매매타임이기 때문에 BuyPrice1이 예상치 못하게 높을 수 있다. 그래서 target price는 그냥 EnterPrice로 정한다.
                If MainForm.IsLoadingDone Then
                    IsPriceInfoRequested = True
                    LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                    If LinkedSymbol.IsCallPriceAvailable Then
                        '호가 available하면
                        If LinkedSymbol.VI Then
                            '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                            VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                            MessageLogging(LinkedSymbol.Code & " :(MA) VI 풀리기 기다려야 한다.")
                        Else
                            VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                            MessageLogging(LinkedSymbol.Code & " :(MA) VI 이미 풀려있어 매수 시도 하면 된다.")
                            '매수시도를 하도록 하면 된다.
                            If IsMA Then
                                If AccountCat = 0 Then
                                    If (Not LinkedSymbol.Caution) AndAlso (Not LinkedSymbol.Supervision) AndAlso (Not YieldForHighLevel) Then    'EnterTime 에서 now로 바뀌었다. enter time이 안 좋은 시간대에 있어도 그 시간대 벗어나면 산다.
                                        MainForm.MainAccountManager.DecisionRegister(Me)
                                    End If
                                Else 'if AccountCat = 1
                                    If (Not LinkedSymbol.Caution) AndAlso (Not LinkedSymbol.Supervision) AndAlso (Not YieldForHighLevel) Then    'EnterTime 에서 now로 바뀌었다. enter time이 안 좋은 시간대에 있어도 그 시간대 벗어나면 산다.
                                        MainForm.SubAccountManager.DecisionRegister(Me)
                                    End If
                                End If
                            Else    'IsForTest
                                MainForm.TestAccountManager.DecisionRegister(Me)
                            End If
                        End If
                    Else
                        '호가 available하지 않으면
                        VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                        MessageLogging(LinkedSymbol.Code & " :(MA) VI 는 커녕 호가도 available하지 않아 기다려야 한다.")
                    End If
                End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            '청산대기
            MinutesPassed += number_of_updated_candles
            If _BasePrice >= last_candle.Close Then
                _BasePrice = last_candle.Close
                _CountFromLastBase = 0
            Else
                _CountFromLastBase += 1
            End If
#If 0 Then
            Select Case UsedMABase
                Case 35
                    MA_price = LinkedSymbol.GetAverage35Minutes(0)
                    EXIT_THRESHOLD_FROM_MA_PRICE = -ENTER_THRESHOLD_FROM_MA_PRICE35 / 2.5
                Case 70
                    MA_price = LinkedSymbol.GetAverage35Minutes(0)
                    EXIT_THRESHOLD_FROM_MA_PRICE = -ENTER_THRESHOLD_FROM_MA_PRICE70 / 2.5
                Case Else '140
                    MA_price = LinkedSymbol.GetAverage35Minutes(0)
                    EXIT_THRESHOLD_FROM_MA_PRICE = -ENTER_THRESHOLD_FROM_MA_PRICE140 / 2.5
            End Select
#End If
            '날짜 바뀜 검출
            'If LinkedSymbol.CandleServiceCenter.LastCandle().CandleTime.Date <> LinkedSymbol.MinuteCandleSeries.Candle(LinkedSymbol.CandleServiceCenter.CandleCount() - 1).CandleTime.Date Then
            '날짜가 바뀐 것이여
            'DaysPassed += 1
            'End If
            If MainForm.IsLoadingDone AndAlso Not IsPriceInfoRequested Then
                '로딩은 됐지만 아직 PriceInfo가 request 안 됐다면 request 하도록 한다.
                IsPriceInfoRequested = True
                LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
            End If
            If IsMA Then
                '191128: 매수 가능한 상태임에도 DecisionHolders에 등록이 되지 않아 자꾸 놓치는 경우가 발생하여 차라리 여기서 매번 등록을 시도하기로 한다.
                '191128: 함수명은 NewDecisionMade 에서 DecisionRegiser로 바뀌었다. 그리고 그 안에서 중복 등록되지 않도록 걸러준다.
                If AccountCat = 0 Then
                    If (Not LinkedSymbol.Caution) AndAlso (Not LinkedSymbol.Supervision) AndAlso (Not YieldForHighLevel) Then    'EnterTime 에서 now로 바뀌었다. enter time이 안 좋은 시간대에 있어도 그 시간대 벗어나면 산다.
                        MainForm.MainAccountManager.DecisionRegister(Me)
                    End If
                Else 'if AccountCat = 1
                    If (Not LinkedSymbol.Caution) AndAlso (Not LinkedSymbol.Supervision) AndAlso (Not YieldForHighLevel) Then    'EnterTime 에서 now로 바뀌었다. enter time이 안 좋은 시간대에 있어도 그 시간대 벗어나면 산다.
                        MainForm.SubAccountManager.DecisionRegister(Me)
                    End If
                End If
            End If
            If AllowMultipleEntering Then
                'If last_candle.Amount > 0 AndAlso last_candle.Test_MA_Price - last_candle.Close > (NumberOfEntering + 1) * ENTER_THRESHOLD_FROM_MA_PRICE * last_candle.Test_MA_Price Then
                If last_candle.Amount > 0 AndAlso NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last - last_candle.Close > ENTER_THRESHOLD_FROM_MA_PRICE * TH_ATTENUATION * MA_PriceInThisContext(last_candle) Then
                    '추가매수해
                    _EnterPriceMulti.Add(last_candle.Close)
                    _FallVolumeMulti.Add(_FallVolumeMulti.Last * VOLUME_ATTENUATION)
                    'FallVolume = (NumberOfEntering + 1) * FallVolume / NumberOfEntering
                    'NumberOfEntering += 1

                    '아래에서 Target price 정하고 Decision register 한다.
                    If MainForm.IsLoadingDone Then
                        If LinkedSymbol.IsCallPriceAvailable Then
                            '호가 available하면
                            If LinkedSymbol.VI Then
                                '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                                VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                                MessageLogging(LinkedSymbol.Code & " : VI 풀리기 기다려야 한다.additional2")
                            Else
                                Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                                VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                                MessageLogging(LinkedSymbol.Code & " : VI 이미 풀려있어 매수 시도 하면 된다.additional2")
                                '매수시도를 하도록 하면 된다.
                                'TargetBuyPrice 설정
                                'TargetBuyPrice = Math.Max(MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, LinkedSymbol.MarketKind), LastEnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                                '2021.05.11: 윗 줄로 했다가 오늘 많이 높아진 진입가격을 봤다. MA 전략에서 target buy price 를 왜 EnterPrice로 그냥 하는지 실감했다. 그래서 윗 줄 코멘트 처리하고 아랫줄로 바꾼다.
                                TargetBuyPrice = LastEnterPrice     'Moving average 전략은 1분마다 매매타임이기 때문에 BuyPrice1이 예상치 못하게 높을 수 있다. 그래서 target price는 그냥 EnterPrice로 정한다.
                                If AccountCat = 0 Then
                                    If IsGoodTimeToBuy(Now) AndAlso (Not LinkedSymbol.Caution) AndAlso (Not LinkedSymbol.Supervision) AndAlso (Not YieldForHighLevel) Then
                                        MainForm.MainAccountManager.DecisionRegister(Me)
                                    End If
                                ElseIf AccountCat = 1 Then
                                    If IsGoodTimeToBuy(Now) AndAlso (Not LinkedSymbol.Caution) AndAlso (Not LinkedSymbol.Supervision) AndAlso (Not YieldForHighLevel) Then
                                        MainForm.SubAccountManager.DecisionRegister(Me)
                                    End If
                                Else    'if AccountCat = 2 then
                                    MainForm.TestAccountManager.DecisionRegister(Me)
                                End If
                            End If
                        Else
                            '호가 available하지 않으면
                            VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                            MessageLogging(LinkedSymbol.Code & " : VI 는 커녕 호가도 available하지 않아 기다려야 한다.additional2")
                        End If
                    End If
                End If
                'If NumberOfEntering > 1 AndAlso last_candle.Close - EnterPriceMulti.Last > ENTER_THRESHOLD_FROM_MA_PRICE * EXIT_MUL * EnterPriceMulti.Last Then
                '멀티 하나 뺀다. (청산)
                'ProfitMUlti.Add(((1 - TAX - FEE) * last_candle.Close - (1 + FEE) * EnterPriceMulti.Last) / EnterPriceMulti.Last)       '수익률
                'EnterPriceMulti.RemoveAt(EnterPriceMulti.Count - 1)
                'NumberOfEntering -= 1
                'End If
            End If
            Dim exit_gulin As Boolean
            If DecisionType = 1 Then
                'A type decision (original)
                If last_candle.Close - MA_price > EXIT_THRESHOLD_FROM_MA_PRICE * MA_price Then
                    'If (CType(last_candle.Close, Double) - EnterPrice) / EnterPrice > 0.03 Then
                    exit_gulin = True
                Else
                    exit_gulin = False
                End If
            ElseIf DecisionType = 2 Then
                'B type decision
                If last_candle.Close - MA_price > -UsedThreshold * EXIT_NEW_RATIO * MA_price + (EnterMAPrice - MA_price) * SPIKE_FACTOR Then
                    exit_gulin = True
                Else
                    exit_gulin = False
                End If
            End If
            If exit_gulin Then
                'If last_candle.Close - MA_price > -UsedThreshold / EXIT_RATIO * MA_price Then
                'If last_candle.Test_MA_Var < 0.007 AndAlso MinutesPassed > 100 Then
                'If last_candle.Close - MA_price > EXIT_THRESHOLD_FROM_MA_PRICE * MA_price Then
                'If last_candle.Close - last_candle.Test_MA_Price > EXIT_THRESHOLD_FROM_MA_PRICE * last_candle.Test_MA_Price Then
                'If (CType(last_candle.Close, Double) - EnterPrice) / EnterPrice > 0.03 Then
                '목표 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = current_time
                ExitPrice = last_candle.Close '(1 + EXIT_THRESHOLD_FROM_MA_PRICE) * last_candle.Test_MA_Price
                ProfitCalculation()
                TookTime = TimeSpan.FromMinutes(MinutesPassed)
                _Done = True
                If Not YieldForHighLevel Then
                    SafeEnterTrace(PartKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    PartCount(MABase) += 1
                    PartMoney(MABase) += Profit
                    SafeLeaveTrace(PartKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
#If 0 Then
            ElseIf EnterPrice - last_candle.Close > LOW_CUT_FROM_MA_PRICE * EnterPrice Then ' MA_price - last_candle.Close > LOW_CUT_FROM_MA_PRICE * MA_price Then
                '로우컷
                _CurrentPhase = SearchPhase.DONE
                ExitTime = current_time
                ExitPrice = last_candle.Close '(1 + EXIT_THRESHOLD_FROM_MA_PRICE) * last_candle.Test_MA_Price
#If ALLOW_MULTIPLE_ENTERING Then
            Dim profit_sum As Double = 0
            For index As Integer = 0 To EnterPriceMulti.Count - 1
                profit_sum += ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPriceMulti(index)) / EnterPriceMulti(index)        '수익률
            Next
            Profit = profit_sum / EnterPriceMulti.Count
#Else
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
#End If
                TookTime = TimeSpan.FromMinutes(MinutesPassed)
                _Done = True
                If Not YieldForHighLevel Then
                    SafeEnterTrace(PartKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    PartCount(MABase) += 1
                    PartMoney(MABase) += Profit
                    SafeLeaveTrace(PartKey, 31)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
#End If
            ElseIf _CountFromLastBase >= HAVING_MINUTE_FROM_BASE OrElse MinutesPassed >= DEFAULT_HAVING_MINUTE Then ' current_time > EnterTime + TimeSpan.FromMinutes(DEFAULT_HAVING_MINUTE) Then
                '최대시간만큼 기다려서 끝낸다.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = current_time
                ExitPrice = last_candle.Close
                ProfitCalculation()
                TookTime = TimeSpan.FromMinutes(MinutesPassed)
                _Done = True
                If Not YieldForHighLevel Then
                    SafeEnterTrace(PartKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    PartCount(MABase) += 1
                    PartMoney(MABase) += Profit
                    SafeLeaveTrace(PartKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
            End If
        ElseIf _CurrentPhase = SearchPhase.DONE Then
            'Done 상태라도 MinutesPassed는 증가시켜야 한다. 그래야 보는 그래프에 오류가 없게 된다.
            MinutesPassed += number_of_updated_candles
        End If

        '2024.01.30 : 새 candle 이 만들어졌을 때 money distribute 를 한꺼번에 해주기 위해 아래 global flag 를 셋해준다.
        MainForm.NeedToProcessMAAccount = True

#If 1 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator IsNot Nothing AndAlso MainForm.IsLoadingDone Then
            '청산여부 혹은 진입 취소 여부 판단
            If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                If _CurrentPhase <> SearchPhase.DONE Then
                    '140701 : DONE이면 Stock operator 초기화는 Symbol에서 수익률 계산 후에 하고, 아니면, 즉, WAIT FALLING 상태로 돌아갔다던가 하면 여기서 초기화해준다.
                    If StockOperator.EnterExitState = EnterOrExit.EOE_Exit AndAlso (StockOperator.InitPrice = LinkedSymbol.LowLimitPrice Or StockOperator.EarlyExit) Then
                        '단 한가지, 하한가로 판 이력이 있거나 조기 매도 했다면 추가로 매수가 되지 않게 stock operator 초기화를 하지 않는다
                    Else
                        'nothing으로 만들기 전 buy fail count는 누적되도록 해준다.
                        AccumBuyFailCount += StockOperator.BuyFailCount
                        StockOperator.Terminate()
                        StockOperator = Nothing
                    End If
                End If
            Else
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso _CurrentPhase = SearchPhase.WAIT_FALLING Then
                    'pre threshold로 주문 들어갔던 것이 가격이 올라가면서 취소해야 되는 상항.
                    '140619 : Liquidate이 적절할 지 모르겠지만 현 상황에서는 최선인 것 같다. 나중에 검토해보자....................................
                    StockOperator.Liquidate(0)
                End If
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso (StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Or StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) AndAlso ExitPrice <> 0 Then
                    '청산 시점.
                    If time_over_clearing Then
                        StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                    Else
                        StockOperator.Liquidate(0)       '청산 주문
                    End If
                End If
            End If
        End If
#End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Overrides Sub BuyingInitiated()
        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
            EnterTime = Now 'StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
            'EnterPrice = a_data.Price                  '진입가
            '_BasePrice = EnterPrice             '바닥가 기록
            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
            'FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

            '_FallHeight = _FallingStartPrice - EnterPrice           '하락폭 기록
        End If
    End Sub

    Public Overrides Sub StopBuyingCompleted()

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

'20211118: 오늘부터 main 계좌에 적용할 새로운 전략이다.
Public Class c05G_DoubleFall
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
    End Structure

    Private PricePointList, DecideTimePointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Private _WaitExitCount As Integer = 0
    Public _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Public Pattern1() As Double = {2, 15, 25, 30.5, 34, 35, 33.8, 30, 22, 13, 0, -19, -43}
    Public Pattern2() As Double = {2, 15, 25, 30.5, 34, 35, 33.8, 30, 22, 13, 0, -19, -43} '{50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 35, 33.4, 30, 22, 13, 0, -19, -43}

    Public NormalizedX1() As Double
    Public MinFlexLength1 As Integer
    Public MaxFlexLength1 As Integer
    Public FlexPatternList1 As New List(Of Double())
    Public FlexNormalizedXList1 As New List(Of Double())
    Public MatchedFlexIndex1 As Integer
    Public NormalizedX2() As Double
    Public MinFlexLength2 As Integer
    Public MaxFlexLength2 As Integer
    Public FlexPatternList2 As New List(Of Double())
    Public FlexNormalizedXList2 As New List(Of Double())
    Public MatchedFlexIndex2 As Integer
    Public SecondFallWaitingCount As Integer = 0
    'Public DEFAULT_HAVING_TIME2 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME3 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME4 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME5 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME6 As Integer = DEFAULT_HAVING_TIME

    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_THRESHOLD1 As Double = 0.0549836
    Public FALL_SCALE_LOWER_THRESHOLD1 As Double = 1.030083
    Public SCORE_THRESHOLD2 As Double = 0.07399661
    Public FALL_SCALE_LOWER_THRESHOLD2 As Double = 1.066653
    Public MAX_SECONDFALL_WAITING As Integer = 36
    Public DEFAULT_HAVING_TIME As Integer = 32
    Public GL1 As Double = 1.1845
    Public GL2 As Double = 1.111
    Public GL3 As Double = 1
    Public GL4 As Double = 0.873
    Public GL5 As Double = 0.784

    Public SCORE_LOWER_THRESHOLD As Double = 0
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1.4
    Public FLEXIBILITY1 As Double = 0.3
    Public FLEXIBILITY2 As Double = 0.3
    Public PatternName As String = "DoubleFall"

#If PATTERN_PRETHRESHOLD Then
    Public PRETHRESHOLD_RATIO As Double = 0.4
    Public CANDIDATE_STEPPING As Double = 0.004
    Public DebugPrethresholdYes As Boolean = False
    Public DebugItWorked As Boolean = False
    Public PrethresholdSucceed As Boolean = False
    Public PrethresholdExtendCount As Integer = 0
    Public CheckPrethresholdWorkedCount As Integer = 0
#End If
    Public MyPriceWindowList As PriceWindowList

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer, ByVal account_cat As Integer)
        MyBase.New(linked_symbol, start_time)

        FALL_VOLUME_THRESHOLD = 400000000000
        _AccountCat = account_cat
        AllowMultipleEntering = False
        ALLOWED_ENTERING_COUNT = 1
        Select Case index_decision_center
            Case 0
                'SCORE_THRESHOLD1 = 0.05064138
                'FALL_SCALE_LOWER_THRESHOLD1 = 1.035284
                'SCORE_THRESHOLD2 = 0.076426
                'FALL_SCALE_LOWER_THRESHOLD2 = 1.04905
            Case Else
                MsgBox("예상치 못한 index_decision_center")
        End Select


        'Pattern1 normalizing
        Dim b_min1 As Double = SafeMin(Pattern1)
        Dim b_max1 As Double = SafeMax(Pattern1)

        'Pattern normalizing
        NormalizedX1 = Pattern1.Clone   'NormalizedX의 어레이 길이를 맞추기 위한 초기화
        For index As Integer = 0 To Pattern1.Length - 1
            Pattern1(index) = (Pattern1(index) - b_min1) / (b_max1 - b_min1)
            NormalizedX1(index) = Convert.ToDouble(index) / (Pattern1.Length - 1)
        Next

        '각 flex_index 에 대해 flex_pattern_normalized를 구하고 flex_normalized_x를 구해놓는다. '''''' normalized_x 구하고 target 길이에 맞는 pattern을 생성해야겠다.
        MinFlexLength1 = Pattern1.Length * (1 - FLEXIBILITY1)
        MaxFlexLength1 = Pattern1.Length * (1 + FLEXIBILITY1)
        Dim flex_pattern_normalized1 As New List(Of Double)
        Dim flex_normalized_x1 As New List(Of Double)
        Dim current_region1 As Integer = 0
        For flex_index As Integer = MinFlexLength1 To MaxFlexLength1
            flex_pattern_normalized1.Clear()
            flex_normalized_x1.Clear()
            current_region1 = 0

            flex_pattern_normalized1.Add(Pattern1.First)
            flex_normalized_x1.Add(0)
            For index As Integer = 1 To flex_index - 2
                flex_normalized_x1.Add(Convert.ToDouble(index) / (flex_index - 1))
                While flex_normalized_x1(index) < NormalizedX1(current_region1) OrElse NormalizedX1(current_region1 + 1) < flex_normalized_x1(index)
                    current_region1 = current_region1 + 1
                End While
                flex_pattern_normalized1.Add(Pattern1(current_region1) + (flex_normalized_x1(index) - NormalizedX1(current_region1)) * (Pattern1(current_region1 + 1) - Pattern1(current_region1)) / (NormalizedX1(current_region1 + 1) - NormalizedX1(current_region1)))
            Next
            flex_pattern_normalized1.Add(Pattern1.Last)
            flex_normalized_x1.Add(1)

            FlexPatternList1.Add(flex_pattern_normalized1.ToArray)
            FlexNormalizedXList1.Add(flex_normalized_x1.ToArray)
        Next

        'Pattern2 normalizing
        Dim b_min2 As Double = SafeMin(Pattern2)
        Dim b_max2 As Double = SafeMax(Pattern2)

        NormalizedX2 = Pattern2.Clone   'NormalizedX의 어레이 길이를 맞추기 위한 초기화
        For index As Integer = 0 To Pattern2.Length - 1
            Pattern2(index) = (Pattern2(index) - b_min2) / (b_max2 - b_min2)
            NormalizedX2(index) = Convert.ToDouble(index) / (Pattern2.Length - 1)
        Next

        '각 flex_index 에 대해 flex_pattern_normalized를 구하고 flex_normalized_x를 구해놓는다. '''''' normalized_x 구하고 target 길이에 맞는 pattern을 생성해야겠다.
        MinFlexLength2 = Pattern2.Length * (1 - FLEXIBILITY2)
        MaxFlexLength2 = Pattern2.Length * (1 + FLEXIBILITY2)
        Dim flex_pattern_normalized2 As New List(Of Double)
        Dim flex_normalized_x2 As New List(Of Double)
        Dim current_region2 As Integer = 0
        For flex_index As Integer = MinFlexLength2 To MaxFlexLength2
            flex_pattern_normalized2.Clear()
            flex_normalized_x2.Clear()
            current_region2 = 0

            flex_pattern_normalized2.Add(Pattern2.First)
            flex_normalized_x2.Add(0)
            For index As Integer = 1 To flex_index - 2
                flex_normalized_x2.Add(Convert.ToDouble(index) / (flex_index - 1))
                While flex_normalized_x2(index) < NormalizedX2(current_region2) OrElse NormalizedX2(current_region2 + 1) < flex_normalized_x2(index)
                    current_region2 = current_region2 + 1
                End While
                flex_pattern_normalized2.Add(Pattern2(current_region2) + (flex_normalized_x2(index) - NormalizedX2(current_region2)) * (Pattern2(current_region2 + 1) - Pattern2(current_region2)) / (NormalizedX2(current_region2 + 1) - NormalizedX2(current_region2)))
            Next
            flex_pattern_normalized2.Add(Pattern2.Last)
            flex_normalized_x2.Add(1)

            FlexPatternList2.Add(flex_pattern_normalized2.ToArray)
            FlexNormalizedXList2.Add(flex_normalized_x2.ToArray)
        Next

        'PriceWindowList 초기화
        '2024.05.18 : 현재는 Pattern1 과 Pattern2 가 같고, Flexibility 도 같기 때문에 PriceWindowList 는 공통된 한 개만 있으면 된다.
        MyPriceWindowList = New PriceWindowList(MaxFlexLength2, MinFlexLength2)

        _CurrentPhase = SearchPhase.WAIT_FALLING

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)

        '2024.01.27 : 이전에 돌던 decision maker가 DONE 되고 빠지면서 새로운 decision maker 를 생성했을 때 RecordList 를 symbol 에 저장되어 있던 RecordList 에서 가져오는 로직이다.
        Dim a_record As PatternCheckStructure
        Dim symbol_list_index As Integer = LinkedSymbol.RecordList.Count - 1
        While symbol_list_index >= 0 AndAlso RecordList.Count < Pattern1.Count + BEGINNING_MARGIN
            a_record.Price = LinkedSymbol.RecordList(symbol_list_index).Price
            a_record.Amount = LinkedSymbol.RecordList(symbol_list_index).Amount
            RecordList.Insert(0, a_record)
            StartTime = StartTime - TimeSpan.FromSeconds(5)
            symbol_list_index -= 1
        End While
        '2024.06.10 : PriceWindowList도 거기에 맞춰 업데이트를 해줘야 한다.
        For index As Integer = 0 To RecordList.Count - 1
            MyPriceWindowList.Insert(RecordList(index).Price)
        Next
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령.
        If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
            ProfitCalculation()
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub

    Public Overrides Sub CreateGraphicData()
        '160520: 그래프 어떻게 그릴까 생각해보자. 앞뒤로 쪼금씩 더 보여주는 게 필요하겠지.
        '160521: 기본적으로 보유중인 패턴들 중 최대길이 가진 놈보다 몇 개정도 더 긴 길이 만큼의 히스토리를 관리하도록 한다.
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (FlexPatternList1(MatchedFlexIndex1).Count + SecondFallWaitingCount - 1) * 5, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (FlexPatternList1(MatchedFlexIndex1).Count + SecondFallWaitingCount - 1) * 5 + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 0))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 0))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 1))                 '청산시간
    End Sub

    '등록된 pattern과 현재 RecordList의 최신 data를 비교하여 matching 여부 체크함.
    Private Function CheckMatching1() As Boolean
        Dim result As Boolean
        Dim target_min, target_max As UInt32

        '2024.04.26 : 아래 부등호가 > 가 아니라 >= 가 맞는 것으로 드러났지만, 아주 미미한 영향이기 때문에 무시하고 그냥 두기로 했다.
        If RecordList.Count > MinFlexLength1 Then
            '패턴체크할 미니멈 길이가 되었다.
#If 0 Then
            Dim target_source_list As New List(Of UInt32)
            Dim target_list As New List(Of UInt32)
            '2024.04.24 : 타겟 (후보가 되는) 리스트 만들기
            For index As Integer = 0 To MaxFlexLength1 - 1
                If RecordList.Count < index + 1 Then
                    '길이가 부족하다
                    Exit For
                End If
                target_source_list.Insert(0, RecordList(RecordList.Count - 1 - index).Price)
            Next
#End If
            Dim score As Double
            For flex_index As Integer = MinFlexLength1 To MaxFlexLength1
                If RecordList.Count < flex_index Then
                    '아직 record 갯수가 부족하다면 종료하고 나감
                    Exit For
                End If

                'target_list.Clear()
                '타겟 만들기
                'For index As Integer = flex_index - 1 To 0 Step -1
                'target_list.Add(RecordList(RecordList.Count - 1 - index).Price)
                'Next
                '2024.04.24 : PCRENEW 전략에서 access violation 을 막기 위해 뜯어고치고, 이제 여기서도 같은 방식으로 뜯어고쳐야 한다. 효과가 있는 것 같지만 확실치는 않다.
                'target_list = target_source_list.GetRange(target_source_list.Count - flex_index, flex_index)
                target_min = MyPriceWindowList.MinList(flex_index - 1)
                target_max = MyPriceWindowList.MaxList(flex_index - 1)

                '타겟이 전부 같은 숫자로 이루어져 있으면 score 계산 필요없이 그냥 다음으로 진행
                If target_max = target_min Then
                    result = False
                    Continue For
                End If

                'score 계산
                score = GetScore1(MyPriceWindowList.NormalizedList(flex_index - MinFlexLength1))

                '계산한 score 판단
                If score < SCORE_THRESHOLD1 And score > SCORE_LOWER_THRESHOLD And target_max / target_min > FALL_SCALE_LOWER_THRESHOLD1 And target_max / target_min < FALL_SCALE_UPPER_THRESHOLD Then
                    ScoreSave = score
                    result = True       'matching 된 것으로 판정
                    MatchedFlexIndex1 = flex_index - MinFlexLength1
                    Exit For
                Else
                    result = False      'matching 안 된 것으로 판정
                End If
            Next

        Else
            result = False
        End If

        Return result
    End Function

    Private Function GetScore1(ByVal target_array() As Double) As Double
        'Dim target_normalized(target_array.Length - 1) As Double
        'Dim pattern_normalized(target_array.Length - 1) As Double

        Dim target_normalized = target_array

#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
        _TopPrice = b_max
        _BasePrice = b_min
#End If

        'target 길이에 맞는 pattern을 특정한다.
        Dim flex_number As Integer = target_array.Length - MinFlexLength1
        Dim flex_pattern() As Double = FlexPatternList1(flex_number)
#If MAKE_MEAN_SAME_BEFORE_GETTING_SCORE Then
        Dim pattern_mean As Double = 0
        Dim target_mean As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            pattern_mean = pattern_mean + flex_pattern(index)
            target_mean = target_mean + target_normalized(index)
        Next
        pattern_mean = pattern_mean / pattern_normalized.Length
        target_mean = target_mean / pattern_normalized.Length
        For index As Integer = 0 To pattern_normalized.Length - 1
            target_normalized(index) = target_normalized(index) + (pattern_mean - target_mean)
        Next
#End If
        'Score 계산
        Dim score As Double = 0
        For index As Integer = 0 To flex_pattern.Length - 1
            score = score + (target_normalized(index) - flex_pattern(index)) ^ 2
        Next
        score = Math.Sqrt(score) / flex_pattern.Length

        Return score
    End Function

    'normalize 후 score 계산
    Private Function GetScore1WithNormalize(ByVal target_array() As UInt32) As Double
        'Target normalization
        Dim target_normalized(target_array.Length - 1) As Double
        Dim b_min As UInt32 = target_array.Min
        Dim b_max As UInt32 = target_array.Max
        Dim pattern_normalized(target_array.Length - 1) As Double

        'normalizing
        For index As Integer = 0 To target_array.Length - 1
            target_normalized(index) = (target_array(index) - b_min) / (b_max - b_min)
        Next

        'target 길이에 맞는 pattern을 특정한다.
        Dim flex_number As Integer = target_array.Length - MinFlexLength1
        Dim flex_pattern() As Double = FlexPatternList1(flex_number)

        'Score 계산
        Dim score As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            score = score + (target_normalized(index) - flex_pattern(index)) ^ 2
        Next
        score = Math.Sqrt(score) / pattern_normalized.Length

        Return score
    End Function

    '등록된 pattern과 현재 RecordList의 최신 data를 비교하여 matching 여부 체크함.
    Private Function CheckMatching2() As Boolean
        Dim result As Boolean
        Dim target_min, target_max As UInt32

        '2024.04.26 : 아래 부등호가 > 가 아니라 >= 가 맞는 것으로 드러났지만, 아주 미미한 영향이기 때문에 무시하고 그냥 두기로 했다.
        If RecordList.Count > MinFlexLength2 + BEGINNING_MARGIN + Pattern1.Count Then
            '패턴체크할 미니멈 길이가 되었다.
#If 0 Then
            Dim target_source_list As New List(Of UInt32)
            Dim target_list As New List(Of UInt32)
            '2024.04.24 : 타겟 (후보가 되는) 리스트 만들기
            For index As Integer = 0 To MaxFlexLength2 - 1
                If RecordList.Count < index + 1 Then
                    '길이가 부족하다
                    Exit For
                End If
                target_source_list.Insert(0, RecordList(RecordList.Count - 1 - index).Price)
            Next
#End If
            Dim score As Double
            For flex_index As Integer = MinFlexLength2 To MaxFlexLength2
                If RecordList.Count < flex_index + BEGINNING_MARGIN + Pattern1.Count Then
                    '아직 record 갯수가 부족하다면 종료하고 나감
                    Exit For
                End If

                'target_list.Clear()
                '타겟 만들기
                'For index As Integer = flex_index - 1 To 0 Step -1
                'target_list.Add(RecordList(RecordList.Count - 1 - index).Price)
                'Next
                '2024.04.24 : PCRENEW 전략에서 access violation 을 막기 위해 뜯어고치고, 이제 여기서도 같은 방식으로 뜯어고쳐야 한다. 효과가 있는 것 같지만 확실치는 않다.
                'target_list = target_source_list.GetRange(target_source_list.Count - flex_index, flex_index)
                target_min = MyPriceWindowList.MinList(flex_index - 1)
                target_max = MyPriceWindowList.MaxList(flex_index - 1)

                '타겟이 전부 같은 숫자로 이루어져 있으면 score 계산 필요없이 그냥 다음으로 진행
                If target_max = target_min Then
                    result = False
                    Continue For
                End If

                'score 계산
                score = GetScore2(MyPriceWindowList.NormalizedList(flex_index - MinFlexLength2))

                '계산한 score 판단
                If score < SCORE_THRESHOLD2 And score > SCORE_LOWER_THRESHOLD And target_max / target_min > FALL_SCALE_LOWER_THRESHOLD2 And target_max / target_min < FALL_SCALE_UPPER_THRESHOLD Then
                    ScoreSave = score
                    result = True       'matching 된 것으로 판정
                    MatchedFlexIndex2 = flex_index - MinFlexLength2
                    Exit For
                Else
                    result = False      'matching 안 된 것으로 판정
                End If
            Next

        Else
            result = False
        End If

        Return result
    End Function

    Private Function GetScore2(ByVal target_array() As Double) As Double
        'Dim target_normalized(target_array.Length - 1) As Double
        'Dim pattern_normalized(target_array.Length - 1) As Double

        Dim target_normalized = target_array

#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
        _TopPrice = b_max
        _BasePrice = b_min
#End If

        'target 길이에 맞는 pattern을 특정한다.
        Dim flex_number As Integer = target_array.Length - MinFlexLength2
        Dim flex_pattern() As Double = FlexPatternList2(flex_number)
#If MAKE_MEAN_SAME_BEFORE_GETTING_SCORE Then
        Dim pattern_mean As Double = 0
        Dim target_mean As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            pattern_mean = pattern_mean + flex_pattern(index)
            target_mean = target_mean + target_normalized(index)
        Next
        pattern_mean = pattern_mean / pattern_normalized.Length
        target_mean = target_mean / pattern_normalized.Length
        For index As Integer = 0 To pattern_normalized.Length - 1
            target_normalized(index) = target_normalized(index) + (pattern_mean - target_mean)
        Next
#End If
        'Score 계산
        Dim score As Double = 0
        For index As Integer = 0 To flex_pattern.Length - 1
            score = score + (target_normalized(index) - flex_pattern(index)) ^ 2
        Next
        score = Math.Sqrt(score) / flex_pattern.Length

        Return score
    End Function

    'normalize 후 score 계산
    Private Function GetScore2WithNormalize(ByVal target_array() As UInt32) As Double
        'Target normalization
        Dim target_normalized(target_array.Length - 1) As Double
        Dim b_min As UInt32 = target_array.Min
        Dim b_max As UInt32 = target_array.Max
        Dim pattern_normalized(target_array.Length - 1) As Double

        'normalizing
        For index As Integer = 0 To target_array.Length - 1
            target_normalized(index) = (target_array(index) - b_min) / (b_max - b_min)
        Next

        'target 길이에 맞는 pattern을 특정한다.
        Dim flex_number As Integer = target_array.Length - MinFlexLength2
        Dim flex_pattern() As Double = FlexPatternList2(flex_number)

        'Score 계산
        Dim score As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            score = score + (target_normalized(index) - flex_pattern(index)) ^ 2
        Next
        score = Math.Sqrt(score) / pattern_normalized.Length

        Return score
    End Function

#If PATTERN_PRETHRESHOLD Then
    Public Function CheckPrethreshold() As Integer
        'Dim result As Integer = -1
        Dim result_list As New List(Of Integer)

        If RecordList.Count > MinFlexLength2 + BEGINNING_MARGIN + Pattern1.Length Then
            '패턴체크할 미니멈 길이가 되었다.
            Dim target_list As New List(Of UInt32)
            Dim last_price_candidate As New List(Of UInt32)
            Dim score_for_candidate As New List(Of Double)
            Dim next_y, near_point As UInt32
            Dim score As Double
            Dim target_min, target_max As UInt32
            For flex_index As Integer = MinFlexLength2 To MaxFlexLength2
                last_price_candidate.Clear()      '마지막점 후보를 클리어하고 시작함.
                score_for_candidate.Clear()         '스코어도 클리어하고 시작함.
                If RecordList.Count < flex_index + BEGINNING_MARGIN + Pattern1.Length Then
                    '아직 record 갯수가 부족하다면 종료하고 나감
                    Exit For
                End If

                target_list.Clear()
                '타겟 만들기 (첫번째 하나는 버려야 함)
                For index As Integer = flex_index - 2 To 0 Step -1
                    target_list.Add(RecordList(RecordList.Count - 1 - index).Price)
                Next
                target_min = SafeMin(target_list.ToArray)
                target_max = SafeMax(target_list.ToArray)

                '타겟이 전부 같은 숫자로 이루어져 있으면 score 계산 필요없이 그냥 다음으로 진행. Min 이 0 이하여도 (그럴리 없겠지만 혹시라도) 그냥 다음으로 진행
                If target_max = target_min OrElse target_min <= 0 Then
                    'result = -1
                    Continue For
                End If

                'Prethreshold 계산할만큼 많은 하락인가 확인
                If (target_min = target_list.Last OrElse target_min = target_list(target_list.Count - 2)) AndAlso target_max / target_min > PRETHRESHOLD_RATIO * (FALL_SCALE_LOWER_THRESHOLD2 - 1) + 1 Then
                    '예상되는 다음값의 후보들을 만들고 Score 를 계산해서 prethreshold 여부를 판단한다.
                    '연장선 상의 다음 점을 구해본다.　연장선은　끝점과　끝에서　세번째점　사이의　연장선이다．
                    ＇next_y = NextCallPrice(target_list.Last + (target_list.Last - target_list(target_list.Count - 3)) / 2, 0)
                    next_y = NextCallPrice(1.5 * target_list.Last - 0.5 * target_list(target_list.Count - 3), 0)   ＇위에거랑　같은데　오버플로우　방지　위함이다．
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD2 Then
                        last_price_candidate.Add(next_y)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '연장선 상의 다음 점보다 좀 높은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 + CANDIDATE_STEPPING), 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD2 Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '연장선 상의 다음 점보다 좀 낮은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 - CANDIDATE_STEPPING), 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD2 Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '그냥 threshold 에 딱 맞는 점을 계산한다.
                    next_y = NextCallPrice(target_max / FALL_SCALE_LOWER_THRESHOLD2, 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD2 Then
                        last_price_candidate.Add(next_y)    '범위 안에 들어오면 일단 후보에 넣는다.
                    Else
                        last_price_candidate.Add(NextCallPrice(next_y, -1))    '범위 안에 안 들어오면 한계단 아래값을 후보에 넣는다.
                    End If

                    '그냥 threshold 에 딱 맞는 점보다 한 계단 낮은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 - CANDIDATE_STEPPING), 0)
                    If target_max / near_point >= FALL_SCALE_LOWER_THRESHOLD2 Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '그냥 threshold 에 딱 맞는 점보다 두 계단 낮은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 - 2 * CANDIDATE_STEPPING), 0)
                    If target_max / near_point >= FALL_SCALE_LOWER_THRESHOLD2 Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    last_price_candidate.Sort() '쏘팅

                    '2024.09.22 : 매수가를 더 낮추기 위해 아래 반복문의 순서를 반대로 했다.
		    '2024.09.29 : 다시 원상 복귀
                    For candidate_index As Integer = last_price_candidate.Count - 1 To 0 Step -1
                        target_list.Add(last_price_candidate(candidate_index))  '마지막에 후보를 붙인다.
                        score_for_candidate.Add(GetScore2WithNormalize(target_list.ToArray()))    '스코어를 계산한다.
                        target_list.RemoveAt(target_list.Count - 1)             '마지막에 붙인 후보를 다시 지운다.

                        If score_for_candidate.Last < SCORE_THRESHOLD2 Then
                            result_list.Add(last_price_candidate(candidate_index))
                            DebugPrethresholdCount_M += 1
                            DebugPrethresholdYes = True
                            MatchedFlexIndex2 = flex_index - MinFlexLength2
                            Exit For
                        End If
                    Next

#If 0 Then
                    '제일 좋은 점수를 찾는다.
                    Dim best_score = score_for_candidate.Min
                    Dim best_index As Integer = -1
                    If best_score < SCORE_THRESHOLD Then
                        '제일 좋은 점수의 index 를 찾는다.
                        For candidate_index As Integer = 0 To score_for_candidate.Count - 1
                            If best_score = score_for_candidate(candidate_index) Then
                                best_index = candidate_index
                                Exit For
                            End If
                        Next

                        '제일 좋은 점수를 가진 놈으로 할 건지, 아니면 threshold 바로 아래 녀석의 점수도 커트라인 통과한 준수한 값이라면
                        '이 녀석으로 해도 괜찮을 거고, 어떤 놈으로 하는 게 좋을 지 판단을 여기서 한다.
                        '세밀한 조정을 좀 더 해도 괜찮을 듯
                        DebugPrethresholdCount_M += 1
                        DebugPrethresholdYes = True
                        MatchedFlexIndex2 = matchedindex_list(best_index) - MinFlexLength2
                        Exit For
                    Else
                        '커트라인 통과한 후보가 하나도 없다는 얘기이므로 다음으로 넘어가면 된다.
                        Continue For
                    End If
#End If

                End If

            Next

        Else
            'result = -1
        End If

        If result_list.Count = 0 Then
            Return -1
        Else
            '2024.01.18 : 여러개가 걸렸으면 그 중에 가격이 제일 높은 것을 골라 보낸다. 낚시 성공률을 높이기 위함이다.
            '2024.09.22 : 매수가를 더 낮추기 위해 아래 Max 에서 Min 으로 바꿨다.
	    '2024.09.29 : 다시 원상 복귀
            Return result_list.Max
        End If
    End Function
#End If

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

#If PATTERN_PRETHRESHOLD Then
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED AndAlso PrethresholdSucceed = True Then
            '2023.12.27 : prethreshold 낚시가 성공했구나!
            CheckPrethresholdWorkedCount = 3
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
        ElseIf _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD AndAlso PrethresholdSucceed = True AndAlso StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
            '2024.01.07 : prethreshold 낚시 잠깐 중단하려고 했는데 그새 걸렸구나!
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
        End If
#End If

        patterncheck_str.Price = a_data.CoreRecord.Price         '주가 저장
        patterncheck_str.Amount = a_data.CoreRecord.Amount         '거래량 저장

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.CoreRecord.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern1.Count + BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.
        '181006: EnterTime은 패턴의 시작점이 아니고 끝점이 맞다.

        '2024.04.26: RecordList 에 새 데이터가 들어옴에 따라 price window list 를 업데이트한다.
        MyPriceWindowList.Insert(patterncheck_str.Price)

        If _CurrentPhase = SearchPhase.WAIT_FALLING Then
            '하락 기다리기 모드

            Dim matching As Boolean = CheckMatching1()

            If matching = True Then
                SecondFallWaitingCount = 0          '전에 clear 안 된 만약의 경우를 대비하기 위함.
                _CurrentPhase = SearchPhase.WAIT_SECONDFALL
            End If

        ElseIf _CurrentPhase = SearchPhase.WAIT_SECONDFALL OrElse _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '두번째 하락 기다리기 모드

            'Dim relative_price As Double
            'If LinkedSymbol.OpenPrice = 0 Then
            'relative_price = 1
            'Else
            'relative_price = a_data.CoreRecord.Price / LinkedSymbol.OpenPrice
            'End If
            Dim matching As Boolean = CheckMatching2()

            If IsFlex AndAlso matching = True AndAlso Not IsClearingTime(current_time) Then 'AndAlso Not IsFirstHalfTime(current_time) Then
                'If relative_price > GL2 Then
                'DEFAULT_HAVING_TIME = 10
                'ElseIf relative_price > GL3 Then
                'DEFAULT_HAVING_TIME = 11
                'ElseIf relative_price > GL4 Then
                'DEFAULT_HAVING_TIME = 16
                'ElseIf relative_price > GL5 Then
                'DEFAULT_HAVING_TIME = 26
                'Else
                'DEFAULT_HAVING_TIME = 22
                'End If
                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - FlexPatternList2(MatchedFlexIndex2).Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                fall_volume = fall_volume * PatternLength / FlexPatternList2(MatchedFlexIndex2).Length
                If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount) Then
#If PATTERN_PRETHRESHOLD Then
                    '2023.12.17 : check prethreshold 디버깅용 코드 ========================================
                    DebugTotalGullin_M += 1
                    If DebugPrethresholdYes Then
                        '오 맞췄어!
                        DebugItWorked = True
                        DebugPrethresholdGullin_M += 1
                    Else
                        '못 맟췄군
                        DebugItWorked = False
                    End If
                    '======================================================================================
#End If
                    EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
#Else
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
#End If
                    FallVolume = fall_volume           '하락 볼륨 업데이트

                    If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso StockOperator.OperatorList.Count > 0 AndAlso LastEnterPrice <> TargetBuyPrice Then
                            '이전에 prethreshold 였고 주문이 나가있다. StopBuying 로직이 구현되어 이제 매수중단 명령이 가능하다.
                            '2024.01.18 : 이전과 현재 주문가가 다른 경우에만 매수중단 하도록 조건이 추가됨
                            StockOperator.StopBuying()
                            IsStopBuyingCompleted = False
                        Else
                            '이외의 것은 현재로선 해줄 거 없음
                        End If
                        TargetBuyPrice = LastEnterPrice
                        MessageLogging(LinkedSymbol.Code & " :(DoubleFall) Prethresold 에서 걸린애로 전환. EnterPrice " & LastEnterPrice.ToString)
                    Else 'if _CurrentPhase = WAIT_FALLING
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                        If LinkedSymbol.IsCallPriceAvailable Then
                            '호가 available하면
                            If LinkedSymbol.VI Then
                                '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                                VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                                MessageLogging(LinkedSymbol.Code & " :(DoubleFall) VI 풀리기 기다려야 한다.")
                            Else
                                Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                                VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                                MessageLogging(LinkedSymbol.Code & " :(DoubleFall) VI 이미 풀려있어 매수 시도 하면 된다.")
                                '매수시도를 하도록 하면 된다.
                                'TargetBuyPrice 설정
                                'TargetBuyPrice = Math.Max(MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, LinkedSymbol.MarketKind), LastEnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                                TargetBuyPrice = LastEnterPrice     '2022.07.18: 위의 방식의 장점을 모르겠다. 이제부터는 EnterPrice가 곧 TargetPrice이다.
                                If AccountCat = 0 Then
                                    MainForm.MainAccountManager.DecisionRegister(Me)
                                ElseIf AccountCat = 1 Then
                                    MainForm.SubAccountManager.DecisionRegister(Me)
                                Else    'if AccountCat = 2 then
                                    MainForm.TestAccountManager.DecisionRegister(Me)
                                End If
                            End If
                        Else
                            '호가 available하지 않으면
                            VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                            MessageLogging(LinkedSymbol.Code & " :(DoubleFall) VI 는 커녕 호가도 available하지 않아 기다려야 한다.")
                        End If
                    End If
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
            Else
                SecondFallWaitingCount += 1
                If SecondFallWaitingCount >= MAX_SECONDFALL_WAITING Then
                    '많이 기다렸는데도 second pattern이 안 나타나면 다시 원점으로 돌아간다.
                    SecondFallWaitingCount = 0
                    If CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                        If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et21_Operator.OperatorState.OS_ORDER_REQUESTED Then
                            '주문이 들어갔다. => 주문한 것을 취소한다. 여기서는 상태만 바꾸고 실제 취소는 아래서 한다.
                            _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD
                        Else
                            '주문이 들어가지 않았거나 들어갔더라도 취소나 중단되었다. => 곧바로 상태를 WAIT_FALLING 으로 바꾼다.
                            _CurrentPhase = SearchPhase.WAIT_FALLING
                            MessageLogging(LinkedSymbol.Code & " :(DoubleFall) Prethreshold => WAIT_FALLING")
                            LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
                        End If
                    Else 'WAIT_SECONDFALL
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                    End If
                Else
                    '5초후의 가격이 score 를 넘을 것이 예상될 때 예상가격을 계산하고, 그 가격에 주문하는 시스템이다.
                    DebugPrethresholdYes = False
                    Dim check_prethresold_price = CheckPrethreshold()
                    If check_prethresold_price > 0 Then
                        'relative price 도 5초후 가격을 바탕으로 계산해야 한다.
                        'If LinkedSymbol.OpenPrice = 0 Then
                        'relative_price = 1
                        'Else
                        'relative_price = check_prethresold_price / LinkedSymbol.OpenPrice
                        'End If

                        'Fall valume 은 마지막 하나 모자란 볼륨을 먼저 구한 후, 마지막 하나는 예상치를 구해 더한다.
                        Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - FlexPatternList2(MatchedFlexIndex2).Count + 1).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                        '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                        fall_volume = fall_volume * PatternLength / FlexPatternList2(MatchedFlexIndex2).Length
                        fall_volume += (patterncheck_str.Amount - RecordList(RecordList.Count - 2).Amount) * patterncheck_str.Price * 1.3           '마지막 5초동안 추가될 거래량을 예상하여 더한다.
                        If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount) Then
                            PrethresholdExtendCount = 0     'extend count 는 prethreshold 걸릴 때마다 초기화 하는 게 맞다.

                            EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5 + 2.5)             '진입시간
                            '여기까지 내려와 체결이 된다는 가정하에 진입가/바닥가/천정가 는 모두 예상한 prethreshold 값이 된다.
                            '2024.08.05 : 알아낸 prethreshold 값보다 약간 작은 값으로 결정한다. 분석에 의하면 좀 낮은 값으로 해도 대부분의 경우 낚시는 성공한다.
                            check_prethresold_price = NextCallPrice(check_prethresold_price * PRETH_DISCOUNT, 0)
                            EnterPrice = check_prethresold_price                  '진입가
#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
#Else
                            _BasePrice = check_prethresold_price             '바닥가 기록
                            _TopPrice = check_prethresold_price             '천정가 기록
#End If
                            FallVolume = fall_volume           '하락 볼륨 업데이트
                            '이 위까지는 이전 상태에 관계없이 공통적인 내용이고 아래는 이전 상태에 따라 다른 부분이다.
                            If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                                MessageLogging(LinkedSymbol.Code & " :(DoubleFall) updating prethreshold 진입, enterprice " & LastEnterPrice.ToString)
                                If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso StockOperator.OperatorList.Count > 0 AndAlso LastEnterPrice <> TargetBuyPrice Then
                                    '이전에 prethreshold 였고 주문이 나가있다. StopBuying 로직이 구현되어 이제 매수중단 명령이 가능하다.
                                    '2024.01.18 : 이전과 현재 주문가가 다른 경우에만 매수중단 하도록 조건이 추가됨
                                    StockOperator.StopBuying()
                                    IsStopBuyingCompleted = False
                                Else
                                    '이외의 것은 현재로선 해줄 거 없음
                                End If
                                TargetBuyPrice = LastEnterPrice
                            Else    'if _CurrentPhase = WAIT_FALLING
                                MessageLogging(LinkedSymbol.Code & " :(DoubleFall) prethreshold 진입, enterprice " & LastEnterPrice.ToString)
                                _CurrentPhase = SearchPhase.PRETHRESHOLDED
                                LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                                If LinkedSymbol.IsCallPriceAvailable Then
                                    '호가 available하면
                                    If LinkedSymbol.VI Then
                                        '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                                        VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                                        MessageLogging(LinkedSymbol.Code & " :(DoubleFall_prethreshold) VI 풀리기 기다려야 한다.")
                                    Else
                                        Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                                        VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                                        MessageLogging(LinkedSymbol.Code & " :(DoubleFall_prethreshold) VI 이미 풀려있어 매수 시도 하면 된다.")
                                        '매수시도를 하도록 하면 된다.
                                        'TargetBuyPrice 설정
                                        TargetBuyPrice = check_prethresold_price     '예상한 값에 매수를 건다.
                                        If AccountCat = 0 Then
                                            MainForm.MainAccountManager.DecisionRegister(Me)
                                        ElseIf AccountCat = 1 Then
                                            MainForm.SubAccountManager.DecisionRegister(Me)
                                        Else    'if AccountCat = 2 then
                                            MainForm.TestAccountManager.DecisionRegister(Me)
                                            '2024.01.14 : 바로 매수 시도 한다.
                                            SymTree.AccountTaskToQueue(MainForm.TestAccountManager)
                                        End If
                                    End If
                                Else
                                    '호가 available하지 않으면
                                    VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                                    MessageLogging(LinkedSymbol.Code & " :(DoubleFall_prethreshold) VI 는 커녕 호가도 available하지 않아 기다려야 한다.")
                                End If
                            End If
                        Else
                            'FallVolume이 너무 크면 오히려 마이너스가 된다.
                        End If
                    Else 'if check_prethresold_price <=0 then
                        'matching 도 안 되고 pre threshold 도 안 걸렸다
                        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                            '이전에 pre threshold 에 걸렸다는 것이다.
                            If PrethresholdExtendCount = 0 Then
                                '주문하고 바로 다음 prethreshold가 안 걸렸을 경우 취소하게 되면 그 간격이 너무 짧아 주문이 올라가 있는 시간을 좀 늘리는 방책이다.
                                PrethresholdExtendCount += 1
                            Else
                                '한 번 기다린 다음번이니 취소하는 게 맞다.
                                If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et21_Operator.OperatorState.OS_ORDER_REQUESTED Then
                                    '주문이 들어갔다. => 주문한 것을 취소한다. 여기서는 상태만 바꾸고 실제 취소는 아래서 한다.
                                    _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD
                                Else
                                    '주문이 들어가지 않았거나 들어갔더라도 취소나 중단되었다. => 곧바로 상태를 WAIT_FALLING 으로 바꾼다.
                                    '2024.02.04 : WAIT_FALLING 이 아니고 WAIT_SECONDFALL 로 전환하는 것으로 변경되었다. second fall 기다리는 횟수가 길어지면 알아서 WAIT_FALLING 으로 돌아간다.
                                    _CurrentPhase = SearchPhase.WAIT_SECONDFALL
                                    MessageLogging(LinkedSymbol.Code & " :(DoubleFall) Prethreshold => WAIT_SECONDFALL")
                                    LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            If CheckPrethresholdWorkedCount > 0 Then
                '2024.01.27 : prethreshold 낚시 성공한 경우 다음 data 샘플을 보고 걸린애 될 운명이었는지 아닌지 판별해서 걸린애 될 운명이었다면 PrethresholdSucceed flag 를 0으로 두어 N 이 아니라 M 으로 기록되게 한다.
                '2024.03.05 : 걸린애될 운명이었는지 확인하는 횟수를 1번에서 3번으로 늘림
                Dim matching As Boolean = CheckMatching2()
                If matching Then
                    PrethresholdSucceed = False
                    CheckPrethresholdWorkedCount = 0
                Else
                    CheckPrethresholdWorkedCount -= 1
                End If
            End If
            If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount AndAlso _WaitExitCount < 2 AndAlso StockOperator Is Nothing Then
                '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거는 이제 치지 말자
                '20220225: Stockoperator가 있음에도 불구하고 여기로 들어와 WAIT_FALLING으로 빠지는 경우가 발생하는 문제가 있다.
                '20220227: Stockoperator 있는 경우에는 운 좋게 진입한 걸로 판단해서 빠져나가지 않고 계속 WAIT_EXIT_TIME 을 유지하기로 한다.
                _WaitExitCount = 0
                _CurrentPhase = SearchPhase.WAIT_FALLING
                MessageLogging(LinkedSymbol.Code & " :(DoubleFall) 초반에 VI 걸려서 포기함")
                LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
            Else
                _WaitExitCount += 1         '청산기다림 카운트
                '청산 기다리기 모드
                If _BasePrice >= patterncheck_str.Price Then
                    _BasePrice = patterncheck_str.Price
                    _CountFromLastBase = 0
                Else
                    _CountFromLastBase += 1
                End If
                If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                    _TopPrice = patterncheck_str.Price
                Else
                End If
                If AllowMultipleEntering Then
#If 0 Then
                    'Old style
                    'multiple entering
                    'If NumberOfEntering < 3 AndAlso _CountFromLastTop > ENTERING_POINT_FROM_LAST_TOP Then
                    If NumberOfEntering < 3 AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD2 - 1) * (TH_ATTENUATION ^ NumberOfEntering)) Then
                        '추가매수해
                        _EnterPriceMulti.Add(patterncheck_str.Price)
                        'LastEnteredPoint = patterncheck_str.Price
                        'FallVolume = (_NumberOfEntering + 1) * FallVolume / _NumberOfEntering
                        _FallVolumeMulti.Add(_FallVolumeMulti.Last * VOLUME_ATTENUATION)
                        '_NumberOfEntering += 1
                        _TopPrice = 0
                        '_CountFromLastTop = 0
                    End If
#Else
                    'New style
                    Dim refall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - 4).Amount) * patterncheck_str.Price           '재하락 볼륨 업데이트
                    '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                    refall_volume = refall_volume * PatternLength / 3
                    ＇If NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD - 1) * (TH_ATTENUATION ^ NumberOfEntering)) AndAlso refall_volume / 3 > (_FallVolumeMulti.First / PatternLength) * VOLUME_ATTENUATION Then
                    If NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD2 - 1) * (TH_ATTENUATION)) AndAlso refall_volume > _FallVolumeMulti.First * VOLUME_ATTENUATION Then
                        '추가매수해
                        _EnterPriceMulti.Add(patterncheck_str.Price)
                        _FallVolumeMulti.Add(refall_volume)
                        '_NumberOfEntering += 1
                        TargetBuyPrice = LastEnterPrice
                    End If
#End If
                End If

                '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
                'PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
                If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                    '그냥 때가 되었다.
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                    ExitPrice = a_data.CoreRecord.Price                   '청산가
                    ProfitCalculation()
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                End If
            End If
        End If

#If 1 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator Is Nothing Then
#If 0 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / (CAREFULL_FACTOR * (LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                If safe_rate > 1 Then
                    safe_rate = 1
                ElseIf safe_rate < 0 Then
                    safe_rate = 0
                End If
                Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                    final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                End If
                MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                    StockOperator.SetAccountManager(MainForm.AccountManager)
                Else
                    '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                    EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                    MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                    '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                End If
            End If
#End If
        Else
            '청산여부 혹은 진입 취소 여부 판단
            If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                If _CurrentPhase = SearchPhase.DONE Then
                    '140701 : DONE이면 Stock operator 초기화는 Symbol에서 수익률 계산 후에 하고,
                Else
                    '아니면, 즉, WAIT FALLING 상태로 돌아갔다던가 하면 여기서 초기화해준다.
                    If StockOperator.EnterExitState = EnterOrExit.EOE_Exit AndAlso (StockOperator.InitPrice = LinkedSymbol.LowLimitPrice Or StockOperator.EarlyExit) Then
                        '단 한가지, 하한가로 판 이력이 있거나 조기 매도 했다면 추가로 매수가 되지 않게 stock operator 초기화를 하지 않는다
                    Else
                        DebugFunctionReachCount1 = DebugFunctionReachCount1 + 1
                        '2024.01.17 : 이쪽으로 들어오는 경우가 두 번 있는데 뭐지??
                        '2024.02.13 : 여기 들어올 때 _CurrentPhase 는 prehresholded 임이 밝혀졌다. 그렇다면 stock operator 는 DONE 이면서 _CurrentPhase 는 prehresholded 인 경우가 있다는 얘기다.
                        '2024.02.15 : EOE_Exit WAIT_FALLING 인 경우도 있음이 밝혀졌다. 전에는 EOE_Enter PRETHRESHOLDED 였다.
                        '2024.02.21 : 이번엔 또 EOE_Enter WAIT_EXIT_TIME
                        MessageLogging(LinkedSymbol.Code & " :DebugFunctionReachCount1 이건 이런 경우입니다. " & StockOperator.EnterExitState.ToString & " " & _CurrentPhase.ToString & " " & StockOperator.DebugDONEDONE)

                        StockOperator.Terminate()
                        StockOperator = Nothing
                    End If
                End If
            Else ''청산 혹은 매수중단 완료되지 않은 경우
                If _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then
                    '매수중단 명령 보낸다.
                    StockOperator.StopBuying()
                    IsStopBuyingCompleted = False
                Else
                    If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso (StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Or StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) AndAlso ExitPrice <> 0 Then
                        '청산 시점.
                        If time_over_clearing Then
                            StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                        Else
                            StockOperator.Liquidate(0)       '청산 주문
                        End If
                    ElseIf StockOperator.EnterExitState = EnterOrExit.EOE_Exit Then
                        '2024.02.18 : time over clearing 으로 하한가로 청산 주문 들어갔는데도 다시 취소하는 문제가 있어 아래에 AndAlso 뒤의 조건을 달았다.
                        If time_over_clearing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED Then
                            '2021.08.01: 지난 금요일 청산시도중 clearing time이 도래했는데 하한가로 파는 로직이 발동이 안 되어 청산이 안 되었다.
                            '이미 청산중이면 재주문시 바로 체결될 수 있게 LowLimitCaution flag를 셋해둔다
                            'StockOperator.ChangeLowLimitCautionFlag()
                            '2024.02.08 : ChangeLowLimitCautionFlag() 를 써도 200ms timer 에서 매도취소가 안 되기 때문에 결국 하한가매도는 동작하지 않는다. Liquidate 에 Exit 상태용 하한가매도 로직을 구현하고 이걸로 바꾸도록 한다.
                            StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                        End If
                    End If
                End If
            End If
        End If
#End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Overrides Sub BuyingInitiated()
        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            PrethresholdSucceed = True
            MessageLogging(LinkedSymbol.Code & " :(DoubleFall) prethreshold 낚시 성공 오예~")
        ElseIf _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then
            '매수중지 중에 체결된 경우. 마찬가지로 Prehreshold 성공으로 처리한다.
            PrethresholdSucceed = True
            MessageLogging(LinkedSymbol.Code & " :(DoubleFall) prethreshold 중지중에 체결됨. 낚시 성공으로 간주함~")
        End If
    End Sub

    Public Overrides Sub StopBuyingCompleted()
        If _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then
            'decision maker 의 task 를 만들어야 한다. 그 안에서 _CurrentPhase 를 WAIT_FALLING 으로 바꾼다.
            'symbol task 를 call한다.
            IsStopBuyingCompleted = True
            SymTree.SymbolTaskToQueue(LinkedSymbol)
        End If

        '또한 account task 를 실행시켜 score 계산을 통해 가장 score 높은 놈에게 매수기회를 줘야 한다.
        SymTree.AccountTaskToQueue(MainForm.TestAccountManager)
    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

Public Class c05F_FlexiblePCRenew
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        'Dim BuyAmount As UInt64
        'Dim SelAmount As UInt64
        'Dim DeltaGangdo As Single
    End Structure

    Public PricePointList, DecideTimePointList, DelayBuyCallPricePointList, DelaySelCallPricePointList As PointList ', DeltaGangdoPointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData, DelayBuyCallPriceCompositeData, DelaySelCallPriceCompositeData As c011_PlainCompositeData ', DeltaGangdoCompositeData 
    Private RecordList As New List(Of PatternCheckStructure)
    'Private BackupPricelist As New List(Of UInt32)
    'Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Public _TargetHeight As UInt32
    Private _TargetBasePrice As UInt32
    Public _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    Public Pattern() As Double = {2, 15, 25, 30.5, 34, 35, 33.8, 30, 22, 13, 0, -19, -43}

    Public NormalizedX() As Double
    Public MinFlexLength As Integer
    Public MaxFlexLength As Integer
    Public FlexPatternList As New List(Of Double())
    Public FlexNormalizedXList As New List(Of Double())
    Public MatchedFlexIndex As Integer
    Public DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    'Parameters for learning ========================================================
    Public SCORE_THRESHOLD As Double = 0.042436
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 1.07
    Public DEFAULT_HAVING_TIME1 As Integer = 13
    Public DEFAULT_HAVING_TIME2 As Integer = 13
    Public DEFAULT_HAVING_TIME3 As Integer = 13
    Public DEFAULT_HAVING_TIME4 As Integer = 13
    Public DEFAULT_HAVING_TIME5 As Integer = 13
    Public DEFAULT_HAVING_TIME6 As Integer = 13
    ' ===============================================================================

    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7
    Public DELTA_PERIOD As Integer = 3
    Public DELTA_GANGDO_THRESHOLD As Double = 5
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.5
    Public GL1 As Double = 1.1845
    Public GL2 As Double = 1.111
    Public GL3 As Double = 1
    Public GL4 As Double = 0.873
    Public GL5 As Double = 0.784

    Public SCORE_LOWER_THRESHOLD As Double = 0
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1.4
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 10
    Public FLEXIBILITY As Double = 0.3
    Public OneMoreSampleCheck As Boolean = False
    Public BuyAmountCenter As Double = 0
    Public SelAmountCenter As Double = 0
    Public PatternGangdo As Double = 0
    Public PatternName As String
#If CHECK_PRE_PATTERN_STRATEGY Then
    Public PrePatternToPattern As Boolean = False
    Public POST_FALL_SCALE_LOWER_THRESHOLD As Double = 1.0375
    Public PostPatternOk As Boolean = False
#End If
    'Public EnteringGangdoChange As Double = 0
    Public PRETHRESHOLD_RATIO As Double = 0.4
    Public CANDIDATE_STEPPING As Double = 0.005
    Public DebugPrethresholdYes As Boolean = False
    Public DebugItWorked As Boolean = False
    Public PrethresholdSucceed As Boolean = False
    Public PrethresholdExtendCount As Integer = 0
    Public CheckPrethresholdWorkedCount As Integer = 0
    Public MyPriceWindowList As PriceWindowList

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer, ByVal account_cat As Integer)
        MyBase.New(linked_symbol, start_time)

        'SavedIndexDecisionCenter = index_decision_center
        _AccountCat = account_cat
        AllowMultipleEntering = False
        ALLOWED_ENTERING_COUNT = 1
        FALL_VOLUME_THRESHOLD = 400000000000
        TH_ATTENUATION = 0.117157287525381
        VOLUME_ATTENUATION = 0.45
#If 0 Then
        Select Case index_decision_center
            Case 0
                PatternName = "RollerCoaster"
                Pattern = {2, 15, 25, 30.5, 34, 35, 33.8, 30, 22, 13, 0, -19, -43}
                DEFAULT_HAVING_TIME = 13
                SCORE_THRESHOLD = 0.0683351496380636 '0.0644704559092
                'SCORE_THRESHOLD = {0.049, 0.042, 0.039, 0.045, 0.048, 0.045, 0.045, 0.043, 0.04}
                FALL_SCALE_LOWER_THRESHOLD = 1.06272 '1.06094995 '1.045 '1.0375 '2021.03.14: 1.0375 로 여태껏(32만원까지) 잘 해왔지만 32만원을 더 넣고 64만원을 만든다음에는 20만원을 잃었다. 매수실패율이 높아서인지 실제수익률이 지지부진하고 있다. 그래서 Fix된 Flexible 전략에서 평균수익률을 조금 높이는 설정으로 가려고 1.045로 바꿔보았다.
                'POST_FALL_SCALE_LOWER_THRESHOLD = 1.042
            Case 0
                PatternName = "Double Silver-2"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625}
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.25
                FALL_SCALE_LOWER_THRESHOLD = 1.0375
            Case 0
                PatternName = "Double Silver-1"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036}        ' Double Silver            Case Else
                DEFAULT_HAVING_TIME = 14 ' 23
                SCORE_THRESHOLD = 3.25
                FALL_SCALE_LOWER_THRESHOLD = 1.035
            Case 0
                PatternName = "Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.1
                FALL_SCALE_LOWER_THRESHOLD = 1.069
            Case 0
                PatternName = "Double Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver            Case Else
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.25
                FALL_SCALE_LOWER_THRESHOLD = 1.045
            Case 0 '2
                PatternName = "Quadruple Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadruple silver
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 2.5
                FALL_SCALE_LOWER_THRESHOLD = 1.1
            Case 0 '3
                PatternName = "ScaleUpTwoStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}     'ScaleUpTwoStair
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 4.8
                FALL_SCALE_LOWER_THRESHOLD = 1.05
            Case 0
                PatternName = "ScaleUpTwoStair/2"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433} ', 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}     'ScaleUpTwoStair
                DEFAULT_HAVING_TIME = 46
                SCORE_THRESHOLD = 5.3
                FALL_SCALE_LOWER_THRESHOLD = 1.0375
#End If
#If 0 Then
            Case 0 '4
                PatternName = "ScaleUpThreeStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.804061708, -1.197653035, -1.439032227, -1.580312757, -1.658899382}    'ScaleUpThreeStair
                DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.5
                FALL_SCALE_LOWER_THRESHOLD = 1.12
            Case 0 '5
                PatternName = "ScaleUpFourStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair
                DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.2
                FALL_SCALE_LOWER_THRESHOLD = 1.05
#End If
#If 0 Then
        Case Else
                MsgBox("예상치 못한 index_decision_center")
        End Select
#End If

        'Pattern normalizing
        Dim b_min As Double = SafeMin(Pattern)
        Dim b_max As Double = SafeMax(Pattern)

        'Pattern normalizing
        NormalizedX = Pattern.Clone   'NormalizedX의 어레이 길이를 맞추기 위한 초기화
        For index As Integer = 0 To Pattern.Length - 1
            Pattern(index) = (Pattern(index) - b_min) / (b_max - b_min)
            NormalizedX(index) = Convert.ToDouble(index) / (Pattern.Length - 1)
        Next

        '각 flex_index 에 대해 flex_pattern_normalized를 구하고 flex_normalized_x를 구해놓는다. '''''' normalized_x 구하고 target 길이에 맞는 pattern을 생성해야겠다.
        MinFlexLength = Pattern.Length * (1 - FLEXIBILITY)
        MaxFlexLength = Pattern.Length * (1 + FLEXIBILITY)
        Dim flex_pattern_normalized As New List(Of Double)
        Dim flex_normalized_x As New List(Of Double)
        Dim current_region As Integer = 0
        For flex_index As Integer = MinFlexLength To MaxFlexLength
            flex_pattern_normalized.Clear()
            flex_normalized_x.Clear()
            current_region = 0

            flex_pattern_normalized.Add(Pattern.First)
            flex_normalized_x.Add(0)
            For index As Integer = 1 To flex_index - 2
                flex_normalized_x.Add(Convert.ToDouble(index) / (flex_index - 1))
                While flex_normalized_x(index) < NormalizedX(current_region) OrElse NormalizedX(current_region + 1) < flex_normalized_x(index)
                    current_region = current_region + 1
                End While
                flex_pattern_normalized.Add(Pattern(current_region) + (flex_normalized_x(index) - NormalizedX(current_region)) * (Pattern(current_region + 1) - Pattern(current_region)) / (NormalizedX(current_region + 1) - NormalizedX(current_region)))
            Next
            flex_pattern_normalized.Add(Pattern.Last)
            flex_normalized_x.Add(1)

            FlexPatternList.Add(flex_pattern_normalized.ToArray)
            FlexNormalizedXList.Add(flex_normalized_x.ToArray)
        Next

        'PriceWindowList 초기화
        MyPriceWindowList = New PriceWindowList(MaxFlexLength, MinFlexLength)

        '_DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'DelayBuyPrice CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("DelayBuyPrice", DataType.REAL_NUMBER_DATA, Nothing)
        DelayBuyCallPriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DelayBuyPrice")
        DelayBuyCallPricePointList = New PointList()
        DelayBuyCallPriceCompositeData.SetData(DelayBuyCallPricePointList)

        'DelaySelPrice CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("DelaySelPrice", DataType.REAL_NUMBER_DATA, Nothing)
        DelaySelCallPriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DelaySelPrice")
        DelaySelCallPricePointList = New PointList()
        DelaySelCallPriceCompositeData.SetData(DelaySelCallPricePointList)

        'DeltaGangdo CompositeData
        'x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("DeltaGangdo", DataType.REAL_NUMBER_DATA, Nothing)
        'DeltaGangdoCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DeltaGangdo")
        'DeltaGangdoPointList = New PointList()
        'DeltaGangdoCompositeData.SetData(DeltaGangdoPointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
        GraphicCompositeDataList.Add(DelayBuyCallPriceCompositeData)
        GraphicCompositeDataList.Add(DelaySelCallPriceCompositeData)
        'GraphicCompositeDataList.Add(DeltaGangdoCompositeData)

        '2024.01.27 : 이전에 돌던 decision maker가 DONE 되고 빠지면서 새로운 decision maker 를 생성했을 때 RecordList 를 symbol 에 저장되어 있던 RecordList 에서 가져오는 로직이다.
        Dim a_record As PatternCheckStructure
        Dim symbol_list_index As Integer = LinkedSymbol.RecordList.Count - 1
        While symbol_list_index >= 0 AndAlso RecordList.Count < Pattern.Count + BEGINNING_MARGIN
            a_record.Price = LinkedSymbol.RecordList(symbol_list_index).Price
            a_record.Amount = LinkedSymbol.RecordList(symbol_list_index).Amount
            RecordList.Insert(0, a_record)
            StartTime = StartTime - TimeSpan.FromSeconds(5)
            symbol_list_index -= 1
        End While
        '2024.06.10 : PriceWindowList도 거기에 맞춰 업데이트를 해줘야 한다.
        For index As Integer = 0 To RecordList.Count - 1
            MyPriceWindowList.Insert(RecordList(index).Price)
        Next
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령.
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED OrElse _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = Now 'StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            ProfitCalculation()
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub

    Public Overrides Sub CreateGraphicData()
        '160520: 그래프 어떻게 그릴까 생각해보자. 앞뒤로 쪼금씩 더 보여주는 게 필요하겠지.
        '160521: 기본적으로 보유중인 패턴들 중 최대길이 가진 놈보다 몇 개정도 더 긴 길이 만큼의 히스토리를 관리하도록 한다.
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            'a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).DeltaGangdo)
            'DeltaGangdoPointList.Add(a_point)
            'a_point.Y = RecordList(index).MAPrice
            'MAPointList.Add(a_point)                                '이동평균 그래프자료 만들기
            'a_point.Y = RecordList(index).BuyMiniSum
            'BuyMiniSumPointList.Add(a_point)                        'BuyMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).SelMiniSum
            'SelMiniSumPointList.Add(a_point)                        'SelMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).Unidelta
            'UnideltaPointList.Add(a_point)                          'Unidelta 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5, 1))                '패턴시작시간
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5 + 0.001, 0))        '패턴시작시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (FlexPatternList(MatchedFlexIndex).Count - 1) * 5, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (FlexPatternList(MatchedFlexIndex).Count - 1) * 5 + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 0))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 0))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 1))                 '청산시간
    End Sub

    '등록된 pattern과 현재 RecordList의 최신 data를 비교하여 matching 여부 체크함.
    Private Function CheckMatching() As Boolean
        Dim result As Boolean
        Dim target_min, target_max As UInt32

        '2024.04.26 : 아래 부등호가 > 가 아니라 >= 가 맞는 것으로 드러났지만, 아주 미미한 영향이기 때문에 무시하고 그냥 두기로 했다.
        If RecordList.Count > MinFlexLength Then
            '패턴체크할 미니멈 길이가 되었다.
#If 0 Then
            Dim target_source_list As New List(Of UInt32)
            Dim target_list As List(Of UInt32)
            '2024.04.07 : 타겟 (후보가 되는) 리스트 만들기
            For index As Integer = 0 To MaxFlexLength - 1
                If RecordList.Count < index + 1 Then
                    '길이가 부족하다
                    Exit For
                End If
                target_source_list.Insert(0, RecordList(RecordList.Count - 1 - index).Price)
            Next
#End If
            Dim score As Double
            For flex_index As Integer = MinFlexLength To MaxFlexLength
                If RecordList.Count < flex_index Then
                    '아직 record 갯수가 부족하다면 종료하고 나감
                    Exit For
                End If

                'target_list.Clear()
                '타겟 만들기
                'For index As Integer = flex_index - 1 To 0 Step -1
                'target_list.Add(RecordList(RecordList.Count - 1 - index).Price)
                'Next
                '2024.04.07 : 최근 target_list.add 할 때 EnsureCapacity 에서 access violation exception 이 많이 난다. 왜 최근에 이러는지 모르겠지만 workaround solution 으로 add 를 피하는 구현을 해봤다.
                'target_list = target_source_list.GetRange(target_source_list.Count - flex_index, flex_index)
                target_min = MyPriceWindowList.MinList(flex_index - 1)
                target_max = MyPriceWindowList.MaxList(flex_index - 1)

                '타겟이 전부 같은 숫자로 이루어져 있으면 score 계산 필요없이 그냥 다음으로 진행
#If ONETIME_FALL_FILTER_DISABLED Then
                If target_max = target_min Then
#Else
                If target_max = target_min OrElse MyPriceWindowList.OldMinList(flex_index - 2) = MyPriceWindowList.OldMaxList(flex_index - 2) Then
#End If
                    '2024.05.21 : 뒤에 old min/max list 조건은 쭉 같은 가격으로 가다가 단 한 번의 하락으로 걸리는 놈이 되는 녀석들을 걸러내기 위해 추가되었다.
                    result = False
                    Continue For
                End If

                'score 계산
                score = GetScore(MyPriceWindowList.NormalizedList(flex_index - MinFlexLength))

                '계산한 score 판단
                If score < SCORE_THRESHOLD And score > SCORE_LOWER_THRESHOLD And target_max / target_min > FALL_SCALE_LOWER_THRESHOLD And target_max / target_min < FALL_SCALE_UPPER_THRESHOLD Then
                    ScoreSave = score
                    result = True       'matching 된 것으로 판정
                    MatchedFlexIndex = flex_index - MinFlexLength
                    Exit For
                Else
                    result = False      'matching 안 된 것으로 판정
                End If
            Next

        Else
            result = False
        End If

        Return result
    End Function

    Public Function CheckPrethreshold() As Integer
        'Dim result As Integer = -1
        Dim result_list As New List(Of Integer)

        If RecordList.Count > MinFlexLength Then
            '패턴체크할 미니멈 길이가 되었다.
            Dim target_list As New List(Of UInt32)
            Dim last_price_candidate As New List(Of UInt32)
            Dim score_for_candidate As New List(Of Double)
            Dim next_y, near_point As UInt32
            Dim score As Double
            Dim target_min, target_max As UInt32
            For flex_index As Integer = MinFlexLength To MaxFlexLength
                last_price_candidate.Clear()      '마지막점 후보를 클리어하고 시작함.
                score_for_candidate.Clear()         '스코어도 클리어하고 시작함.
                If RecordList.Count < flex_index Then
                    '아직 record 갯수가 부족하다면 종료하고 나감
                    Exit For
                End If

                target_list.Clear()
                '타겟 만들기 (첫번째 하나는 버려야 함)
                For index As Integer = flex_index - 2 To 0 Step -1
                    target_list.Add(RecordList(RecordList.Count - 1 - index).Price)
                Next
                target_min = SafeMin(target_list.ToArray)
                target_max = SafeMax(target_list.ToArray)

                '타겟이 전부 같은 숫자로 이루어져 있으면 score 계산 필요없이 그냥 다음으로 진행. Min 이 0 이하여도 (그럴리 없겠지만 혹시라도) 그냥 다음으로 진행
                If target_max = target_min OrElse target_min <= 0 Then
                    'result = -1
                    Continue For
                End If

                'Prethreshold 계산할만큼 많은 하락인가 확인
                If target_min = target_list.Last AndAlso target_max / target_min > PRETHRESHOLD_RATIO * (FALL_SCALE_LOWER_THRESHOLD - 1) + 1 Then
                    '예상되는 다음값의 후보들을 만들고 Score 를 계산해서 prethreshold 여부를 판단한다.
                    '연장선 상의 다음 점을 구해본다.
                    next_y = NextCallPrice(2 * CType(target_list.Last, Integer) - target_list(target_list.Count - 2), 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD Then
                        last_price_candidate.Add(next_y)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '연장선 상의 다음 점보다 좀 높은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 + CANDIDATE_STEPPING), 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '연장선 상의 다음 점보다 좀 낮은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 - CANDIDATE_STEPPING), 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '그냥 threshold 에 딱 맞는 점을 계산한다.
                    next_y = NextCallPrice(target_max / FALL_SCALE_LOWER_THRESHOLD, 0)
                    If target_max / next_y >= FALL_SCALE_LOWER_THRESHOLD Then
                        last_price_candidate.Add(next_y)    '범위 안에 들어오면 일단 후보에 넣는다.
                    Else
                        last_price_candidate.Add(NextCallPrice(next_y, -1))    '범위 안에 안 들어오면 한계단 아래값을 후보에 넣는다.
                    End If

                    '그냥 threshold 에 딱 맞는 점보다 한 계단 낮은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 - CANDIDATE_STEPPING), 0)
                    If target_max / near_point >= FALL_SCALE_LOWER_THRESHOLD Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    '그냥 threshold 에 딱 맞는 점보다 두 계단 낮은 점을 하나 구해본다.
                    near_point = NextCallPrice(next_y * (1 - 2 * CANDIDATE_STEPPING), 0)
                    If target_max / near_point >= FALL_SCALE_LOWER_THRESHOLD Then
                        last_price_candidate.Add(near_point)    '범위 안에 들어오면 일단 후보에 넣는다.
                    End If

                    last_price_candidate.Sort() '쏘팅

                    '2024.09.22 : 매수가를 더 낮추기 위해 아래 반복문의 순서를 반대로 했다.
		    '2024.09.29 : 다시 원상 복귀
                    For candidate_index As Integer = last_price_candidate.Count - 1 To 0 Step -1 ' 0 To last_price_candidate.Count - 1 
                        target_list.Add(last_price_candidate(candidate_index))  '마지막에 후보를 붙인다.
                        score_for_candidate.Add(GetScoreWithNormalize(target_list.ToArray()))    '스코어를 계산한다.
                        target_list.RemoveAt(target_list.Count - 1)             '마지막에 붙인 후보를 다시 지운다.

                        If score_for_candidate.Last < SCORE_THRESHOLD Then
                            result_list.Add(last_price_candidate(candidate_index))
                            DebugPrethresholdCount_T += 1
                            DebugPrethresholdYes = True
                            MatchedFlexIndex = flex_index - MinFlexLength
                            Exit For
                        End If
                    Next
#If 0 Then
                    For candidate_index As Integer = 0 To last_price_candidate.Count - 1
                        target_list.Add(last_price_candidate(candidate_index))  '마지막에 후보를 붙인다.
                        score_for_candidate.Add(GetScore(target_list.ToArray()))    '스코어를 계산한다.
                        target_list.RemoveAt(target_list.Count - 1)             '마지막에 붙인 후보를 다시 지운다.
                    Next

                    '제일 좋은 점수를 찾는다.
                    Dim max_score = score_for_candidate.Min
                    Dim max_index As Integer = -1
                    If max_score < SCORE_THRESHOLD Then
                        '제일 좋은 점수의 index 를 찾는다.
                        For candidate_index As Integer = 0 To score_for_candidate.Count - 1
                            If max_score = score_for_candidate(candidate_index) Then
                                max_index = candidate_index
                                Exit For
                            End If
                        Next

                        '제일 좋은 점수를 가진 놈으로 할 건지, 아니면 threshold 바로 아래 녀석의 점수도 커트라인 통과한 준수한 값이라면
                        '이 녀석으로 해도 괜찮을 거고, 어떤 놈으로 하는 게 좋을 지 판단을 여기서 한다.
                        '세밀한 조정을 좀 더 해도 괜찮을 듯
                        DebugPrethresholdCount += 1
                        DebugPrethresholdYes = True
                        Exit For
                    Else
                        '커트라인 통과한 후보가 하나도 없다는 얘기이므로 다음으로 넘어가면 된다.
                        Continue For
                    End If
#End If
                End If

            Next

        Else
            'result = -1
        End If

        If result_list.Count = 0 Then
            Return -1
        Else
            '2024.09.22 : 매수가를 더 낮추기 위해 아래 Max 에서 Min 으로 바꿨다.
	    '2024.09.29 : 다시 원상 복귀
            Return result_list.Max
        End If
    End Function

    Private Function GetScore(ByVal target_array() As Double) As Double
        'Dim target_normalized(target_array.Length - 1) As Double
        'Dim pattern_normalized(target_array.Length - 1) As Double

        Dim target_normalized = target_array

#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
        _TopPrice = b_max
        _BasePrice = b_min
#End If

        'target 길이에 맞는 pattern을 특정한다.
        Dim flex_number As Integer = target_array.Length - MinFlexLength
        Dim flex_pattern() As Double = FlexPatternList(flex_number)
#If MAKE_MEAN_SAME_BEFORE_GETTING_SCORE Then
        Dim pattern_mean As Double = 0
        Dim target_mean As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            pattern_mean = pattern_mean + flex_pattern(index)
            target_mean = target_mean + target_normalized(index)
        Next
        pattern_mean = pattern_mean / pattern_normalized.Length
        target_mean = target_mean / pattern_normalized.Length
        For index As Integer = 0 To pattern_normalized.Length - 1
            target_normalized(index) = target_normalized(index) + (pattern_mean - target_mean)
        Next
#End If
        'Score 계산
        Dim score As Double = 0
        For index As Integer = 0 To flex_pattern.Length - 1
            score = score + (target_normalized(index) - flex_pattern(index)) ^ 2
        Next
        score = Math.Sqrt(score) / flex_pattern.Length

        Return score
    End Function

    'normalize 후 score 계산
    Private Function GetScoreWithNormalize(ByVal target_array() As UInt32) As Double
        'Target normalization
        Dim target_normalized(target_array.Length - 1) As Double
        Dim b_min As UInt32 = target_array.Min
        Dim b_max As UInt32 = target_array.Max
        Dim pattern_normalized(target_array.Length - 1) As Double

#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
        _TopPrice = b_max
        _BasePrice = b_min
#End If

        'normalizing
        For index As Integer = 0 To target_array.Length - 1
            target_normalized(index) = (target_array(index) - b_min) / (b_max - b_min)
        Next

        'target 길이에 맞는 pattern을 특정한다.
        Dim flex_number As Integer = target_array.Length - MinFlexLength
        Dim flex_pattern() As Double = FlexPatternList(flex_number)
#If MAKE_MEAN_SAME_BEFORE_GETTING_SCORE Then
        Dim pattern_mean As Double = 0
        Dim target_mean As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            pattern_mean = pattern_mean + flex_pattern(index)
            target_mean = target_mean + target_normalized(index)
        Next
        pattern_mean = pattern_mean / pattern_normalized.Length
        target_mean = target_mean / pattern_normalized.Length
        For index As Integer = 0 To pattern_normalized.Length - 1
            target_normalized(index) = target_normalized(index) + (pattern_mean - target_mean)
        Next
#End If
        'Score 계산
        Dim score As Double = 0
        For index As Integer = 0 To pattern_normalized.Length - 1
            score = score + (target_normalized(index) - flex_pattern(index)) ^ 2
        Next
        score = Math.Sqrt(score) / pattern_normalized.Length

        Return score
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

#If PATTERN_PRETHRESHOLD Then
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED AndAlso PrethresholdSucceed = True Then
            '2023.12.27 : prethreshold 낚시가 성공했구나!
            CheckPrethresholdWorkedCount = 3
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
        ElseIf _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD AndAlso PrethresholdSucceed = True AndAlso StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
            '2024.01.07 : prethreshold 낚시 잠깐 중단하려고 했는데 그새 걸렸구나!
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
        End If
#End If

        patterncheck_str.Price = a_data.CoreRecord.Price         '주가 저장
        patterncheck_str.Amount = a_data.CoreRecord.Amount         '거래량 저장
        'patterncheck_str.BuyAmount = a_data.BuyAmount       '매수거래량
        'patterncheck_str.SelAmount = a_data.SelAmount       '매도거래량
        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            'Dim delta_buy As UInt64 = Math.Max(1, CType(patterncheck_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            'Dim delta_sel As UInt64 = Math.Max(1, CType(patterncheck_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            'patterncheck_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            'patterncheck_str.DeltaGangdo = -1
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.CoreRecord.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + BEGINNING_MARGIN
            'BackupPricelist.Add(RecordList(0).Price)
            'If BackupPricelist.Count > 4 * 12 - Pattern.Count - BEGINNING_MARGIN Then
            'BackupPricelist.RemoveAt(0)
            'End If
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        'RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.
        '181006: EnterTime은 패턴의 시작점이 아니고 끝점이 맞다.

        '2024.04.26: RecordList 에 새 데이터가 들어옴에 따라 price window list 를 업데이트한다.
        MyPriceWindowList.Insert(patterncheck_str.Price)

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            'Dim flag_prethreshold_previously As Boolean = False
            '하락 기다리기 모드
            If Not IsClearingTime(current_time) Then
                Dim relative_price As Double

                Dim matching As Boolean = CheckMatching()

                'If matching = True And (BackupPricelist.Count = 0 OrElse (BackupPricelist.Count > 0 And BackupPricelist(0) > patterncheck_str.Price)) Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
                If matching = True Then 'AndAlso relative_price < GL1) Then
                    If LinkedSymbol.OpenPrice = 0 Then
                        relative_price = 1
                    Else
                        relative_price = a_data.CoreRecord.Price / LinkedSymbol.OpenPrice
                    End If
                    '2020.12.22: Not ClearingTime 조건을 추가했다.
                    If relative_price > GL1 Then
                        DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME1
                    ElseIf relative_price > GL2 Then
                        DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME2
                    ElseIf relative_price > GL3 Then
                        DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME3
                    ElseIf relative_price > GL4 Then
                        DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME4
                    ElseIf relative_price > GL5 Then
                        DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME5
                    Else
                        DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME6
                    End If
#If 0 Then
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이건 안 치기로 하자.
                Else
                    Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                    If fall_volume < FALL_VOLUME_THRESHOLD Then
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - Pattern.Count) * 5)             '진입시간
                        EnterPrice = patterncheck_str.Price                  '진입가
                        '_BasePrice = patterncheck_str.Price             '바닥가 기록
                        'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                        FallVolume = fall_volume           '하락 볼륨 업데이트
                        OneMoreSampleCheck = True
                    Else
                        'FallVolume이 너무 크면 오히려 마이너스가 된다.
                    End If
                End If
#Else
                    Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - FlexPatternList(MatchedFlexIndex).Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                    '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                    fall_volume = fall_volume * PatternLength / FlexPatternList(MatchedFlexIndex).Length
                    If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount) Then
#If PATTERN_PRETHRESHOLD Then
                        '2023.12.17 : check prethreshold 디버깅용 코드 ========================================
                        DebugTotalGullin_T += 1
                        If DebugPrethresholdYes Then
                            '오 맞췄어!
                            DebugItWorked = True
                            DebugPrethresholdGullin_T += 1
                        Else
                            '못 맟췄군
                            DebugItWorked = False
                        End If
                        '======================================================================================
#End If
                        '20220301: StockSearcher에 맞춰서 이제 변동성완화장치에 걸려 2분간 단일가매매는 안 쳐주기로 한다.
                        'If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                        '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                        'TwoMinutesHolding = True
                        'End If
#If CHECK_PRE_PATTERN_STRATEGY Then
                    PrePatternToPattern = True
                    PostFallScaleThreshold = (1 + CType(_TargetHeight, Double) / _TargetBasePrice - FALL_SCALE_LOWER_THRESHOLD) * POST_B + POST_C
#End If
                        EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)             '진입시간
                        EnterPrice = patterncheck_str.Price                  '진입가
                        'EnteringGangdoChange = patterncheck_str.DeltaGangdo / RecordList(RecordList.Count - 1 - DELTA_PERIOD).DeltaGangdo
#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
#Else
                        _BasePrice = patterncheck_str.Price             '바닥가 기록
                        _TopPrice = patterncheck_str.Price             '천정가 기록
#End If
                        'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                        FallVolume = fall_volume           '하락 볼륨 업데이트
                        OneMoreSampleCheck = True
                        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                            If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso StockOperator.OperatorList.Count > 0 AndAlso LastEnterPrice <> TargetBuyPrice Then
                                '2023.12.28 : 이전에 prethreshold 였고 주문이 나가있다. => 새로운 가격으로 다시 주문하기 위해 이전주문을 일단 취소해야 한다. 이걸 update prethreshold 라고 표현한다.
                                '2023.12.28 : 취소후 다시 주문은 시간도 많이 걸리고 취소하다가 체결되면 골치아프고 문제가 많다. 실험적으로 일단 operator 객체에 가격을 낮추도록 하는 방법을 써 본다.
                                '2024.01.09 : StopBuying 로직이 구현되어 이제 매수중단 명령이 가능하다.
                                '2024.01.18 : 이전과 현재 주문가가 다른 경우에만 매수중단 하도록 조건이 추가됨
                                StockOperator.StopBuying()
                                IsStopBuyingCompleted = False
                            Else
                                '이외의 것은 현재로선 해줄 거 없음
                            End If
                            TargetBuyPrice = LastEnterPrice
#If 0 Then
                            'operator 의 가격만 바꿔준다.
                            If StockOperator IsNot Nothing AndAlso StockOperator.BuyStandardPrice > EnterPrice Then
                                StockOperator.BuyStandardPrice = EnterPrice
                            End If
#End If
                            MessageLogging(LinkedSymbol.Code & " :(PCRenew) Prethresold 에서 걸린애로 전환. EnterPrice " & LastEnterPrice.ToString)
                        Else 'if _CurrentPhase = WAIT_FALLING
                            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                            LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                            If LinkedSymbol.IsCallPriceAvailable Then
                                '호가 available하면
                                If LinkedSymbol.VI Then
                                    '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                                    VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                                    MessageLogging(LinkedSymbol.Code & " :(PCRenew) VI 풀리기 기다려야 한다.")
                                Else
                                    Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                                    VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                                    MessageLogging(LinkedSymbol.Code & " :(PCRenew) VI 이미 풀려있어 매수 시도 하면 된다.")
                                    '매수시도를 하도록 하면 된다.
                                    'TargetBuyPrice 설정
                                    'TargetBuyPrice = Math.Max(MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, LinkedSymbol.MarketKind), LastEnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                                    '2022.06.29: 위에처럼 하니 EnterPrice보다 높은 값이 세팅되는데 이대로 괜찮은 걸까? 그리고 좀 더 일찍 매수시도를 할 방법은 없나? 약 2초 정도 늦게 매수 들어가는 경우가 있는데 너무 늦게 들어가는 것 같다.
                                    TargetBuyPrice = LastEnterPrice     '2022.07.18: 위의 방식의 장점을 모르겠다. 이제부터는 EnterPrice가 곧 TargetPrice이다.
                                    If AccountCat = 0 Then
                                        MainForm.MainAccountManager.DecisionRegister(Me)
                                    ElseIf AccountCat = 1 Then
                                        MainForm.SubAccountManager.DecisionRegister(Me)
                                    Else    'if AccountCat = 2 then
                                        MainForm.TestAccountManager.DecisionRegister(Me)
                                        '2024.01.14 : 바로 매수 시도 한다.
                                        SymTree.AccountTaskToQueue(MainForm.TestAccountManager)
                                    End If
                                End If
                            Else
                                '호가 available하지 않으면
                                VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                                MessageLogging(LinkedSymbol.Code & " :(PCRenew) VI 는 커녕 호가도 available하지 않아 기다려야 한다.")
                            End If
                        End If
                    Else
                        'FallVolume이 너무 크면 오히려 마이너스가 된다.
                    End If
#End If
#If PATTERN_PRETHRESHOLD Then
                Else    'if not matching
                    '2023.12.25 : 5초후의 가격이 score 를 넘을 것이 예상될 때 예상가격을 계산하고, 그 가격에 주문하는 시스템이다.
                    DebugPrethresholdYes = False
                    Dim check_prethresold_price = CheckPrethreshold()
                    If check_prethresold_price > 0 Then
                        '2023.12.25 : relative price 도 5초후 가격을 바탕으로 계산해야 한다.
                        If LinkedSymbol.OpenPrice = 0 Then
                            relative_price = 1
                        Else
                            relative_price = check_prethresold_price / LinkedSymbol.OpenPrice
                        End If

                        If relative_price > GL1 Then
                            DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME1
                        ElseIf relative_price > GL2 Then
                            DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME2
                        ElseIf relative_price > GL3 Then
                            DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME3
                        ElseIf relative_price > GL4 Then
                            DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME4
                        ElseIf relative_price > GL5 Then
                            DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME5
                        Else
                            DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME6
                        End If
                        '2023.12.25 : Fall valume 은 마지막 하나 모자란 볼륨을 먼저 구한 후, 마지막 하나는 예상치를 구해 더한다.
                        Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - FlexPatternList(MatchedFlexIndex).Count + 1).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                        fall_volume += (patterncheck_str.Amount - RecordList(RecordList.Count - 2).Amount) * patterncheck_str.Price * 1.3           '2023.12.25 : 마지막 5초동안 추가될 거래량을 예상하여 더한다.
                        '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                        fall_volume = fall_volume * PatternLength / FlexPatternList(MatchedFlexIndex).Length
                        If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount) Then
                            PrethresholdExtendCount = 0     'extend count 는 prethreshold 걸릴 때마다 초기화 하는 게 맞다.

                            EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5 + 2.5)             '진입시간
                            '2023.12.25 : 여기까지 내려와 체결이 된다는 가정하에 진입가/바닥가/천정가 는 모두 예상한 prethreshold 값이 된다.
                            '2024.08.05 : 알아낸 prethreshold 값보다 약간 작은 값으로 결정한다. 분석에 의하면 좀 낮은 값으로 해도 대부분의 경우 낚시는 성공한다.
                            check_prethresold_price = NextCallPrice(check_prethresold_price * PRETH_DISCOUNT, 0)
                            EnterPrice = check_prethresold_price                  '진입가
#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
#Else
                            _BasePrice = check_prethresold_price             '바닥가 기록
                            _TopPrice = check_prethresold_price             '천정가 기록
#End If
                            FallVolume = fall_volume           '하락 볼륨 업데이트
                            '2023.12.28 : 이 위까지는 이전 상태에 관계없이 공통적인 내용이고 아래는 이전 상태에 따라 다른 부분이다.
                            If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                                MessageLogging(LinkedSymbol.Code & " :(PCRenew) updating prethreshold 진입, enterprice " & LastEnterPrice.ToString)
                                If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso StockOperator.OperatorList.Count > 0 AndAlso LastEnterPrice <> TargetBuyPrice Then
                                    '2023.12.28 : 이전에 prethreshold 였고 주문이 나가있다. => 새로운 가격으로 다시 주문하기 위해 이전주문을 일단 취소해야 한다. 이걸 update prethreshold 라고 표현한다.
                                    '2023.12.28 : 취소후 다시 주문은 시간도 많이 걸리고 취소하다가 체결되면 골치아프고 문제가 많다. 실험적으로 일단 operator 객체에 가격을 낮추도록 하는 방법을 써 본다.
                                    '2024.01.09 : StopBuying 로직이 구현되어 이제 매수중단 명령이 가능하다.
                                    '2024.01.18 : 이전과 현재 주문가가 다른 경우에만 매수중단 하도록 조건이 추가됨
                                    StockOperator.StopBuying()
                                    IsStopBuyingCompleted = False
                                Else
                                    '이외의 것은 현재로선 해줄 거 없음
                                End If
                                TargetBuyPrice = LastEnterPrice
                            Else    'if _CurrentPhase = WAIT_FALLING
                                MessageLogging(LinkedSymbol.Code & " :(PCRenew) prethreshold 진입, enterprice " & LastEnterPrice.ToString)
                                _CurrentPhase = SearchPhase.PRETHRESHOLDED
                                LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                                If LinkedSymbol.IsCallPriceAvailable Then
                                    '호가 available하면
                                    If LinkedSymbol.VI Then
                                        '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                                        VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                                        MessageLogging(LinkedSymbol.Code & " :(PCRenew_prethreshold) VI 풀리기 기다려야 한다.")
                                    Else
                                        Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                                        VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                                        MessageLogging(LinkedSymbol.Code & " :(PCRenew_prethreshold) VI 이미 풀려있어 매수 시도 하면 된다.")
                                        '매수시도를 하도록 하면 된다.
                                        'TargetBuyPrice 설정
                                        TargetBuyPrice = check_prethresold_price     '2023.12.25 : 예상한 값에 매수를 건다.
                                        If AccountCat = 0 Then
                                            MainForm.MainAccountManager.DecisionRegister(Me)
                                        ElseIf AccountCat = 1 Then
                                            MainForm.SubAccountManager.DecisionRegister(Me)
                                        Else    'if AccountCat = 2 then
                                            MainForm.TestAccountManager.DecisionRegister(Me)
                                            '2024.01.14 : 바로 매수 시도 한다.
                                            SymTree.AccountTaskToQueue(MainForm.TestAccountManager)
                                        End If
                                    End If
                                Else
                                    '호가 available하지 않으면
                                    VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                                    MessageLogging(LinkedSymbol.Code & " :(PCRenew_prethreshold) VI 는 커녕 호가도 available하지 않아 기다려야 한다.")
                                End If
                                'If flag_prethreshold_previously Then
                                '2023.12.27 : 전에 prethreshold였다면 RequestPriceInfo 한 번 해준 게 있기 때문에 한 번 StopPriceInfo 해줘야 한다.
                                'LinkedSymbol.StopPriceinfo()
                                'End If
                            End If
                        Else
                            'FallVolume이 너무 크면 오히려 마이너스가 된다.
                        End If
                    Else 'if check_prethresold_price <=0 then
                        '2023.12.25 : matching 도 안 되고 pre threshold 도 안 걸렸다
                        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                            '이전에 pre threshold 에 걸렸다는 것이다.
                            If PrethresholdExtendCount = 0 Then
                                '2023.12.27 : 주문하고 바로 다음 prethreshold가 안 걸렸을 경우 취소하게 되면 그 간격이 너무 짧아 주문이 올라가 있는 시간을 좀 늘리는 방책이다.
                                PrethresholdExtendCount += 1
                            Else
                                '2023.12.27 : 한 번 기다린 다음번이니 취소하는 게 맞다.
#If 0 Then
                                If StockOperator Is Nothing Then
                                    '주문이 들어가지 않았다 => 곧바로 상태를 WAIT_FALLING 으로 바꾼다.
                                    _CurrentPhase = SearchPhase.WAIT_FALLING
                                    LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
                                Else 'if StockOperator IsNot Nothing Then
                                    '주문이 들어갔다. => 주문한 것을 취소한다. 여기서는 상태만 바꾸고 실제 취소는 아래서 한다.
                                    _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD
                                End If
#End If
                                If StockOperator IsNot Nothing AndAlso StockOperator.OpStatus = et21_Operator.OperatorState.OS_ORDER_REQUESTED Then
                                    '2024.01.09 : 주문이 들어갔다. => 주문한 것을 취소한다. 여기서는 상태만 바꾸고 실제 취소는 아래서 한다.
                                    _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD
                                Else
                                    '2024.01.09 : 주문이 들어가지 않았거나 들어갔더라도 취소나 중단되었다. => 곧바로 상태를 WAIT_FALLING 으로 바꾼다.
                                    _CurrentPhase = SearchPhase.WAIT_FALLING
                                    MessageLogging(LinkedSymbol.Code & " :(PCRenew) Prethreshold => WAIT_FALLING")
                                    LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
                                End If
                            End If
                        End If
                    End If
                End If
#Else
                            End If  'not matching
#End If
            End If 'If Not IsClearingTime

        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
#If CHECK_PRE_PATTERN_STRATEGY Then
            If PrePatternToPattern = True Then
                PrePatternToPattern = False
                If _TopPrice / patterncheck_str.Price >= PostFallScaleThreshold AndAlso patterncheck_str.Amount > RecordList(RecordList.Count - 2).Amount Then
                    PostPatternOk = True
                    EnterPrice = _TopPrice / PostFallScaleThreshold
                Else
                    PostPatternOk = False
                    FallVolume = 0
                End If
#If 0 Then
                If _TopPrice / _BasePrice > PostFallScaleThreshold Then
                    If patterncheck_str.Price <= _BasePrice Then
                        PostPatternOk = True
                        'EnterPrice = patterncheck_str.Price
                        EnterPrice = _TopPrice / PostFallScaleThreshold
                    Else
                        PostPatternOk = False
                        FallVolume = 0
                    End If
                Else
                    If _TopPrice / patterncheck_str.Price > PostFallScaleThreshold Then
                        PostPatternOk = True
                        'EnterPrice = patterncheck_str.Price
                        EnterPrice = _TopPrice / POST_FALL_SCALE_LOWER_THRESHOLD
                    Else
                        PostPatternOk = False
                        FallVolume = 0
                    End If
                End If
#End If
            End If
#End If
#If 0 Then
            If OneMoreSampleCheck Then
                OneMoreSampleCheck = False
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
#If 1 Then
                    '180117: 변동성완화장치 풀릴 때까지 기다린다.
                    TwoMinutesHolding = True
#Else
                    'WAIT_EXIT_TIME 들어오고나서 첫번째 샘플까지 봐서 변동성완화장치 걸린 거라면 취소시키자.
                    '_CurrentPhase = SearchPhase.WAIT_FALLING
                    '취소보다는 그냥 끝내자. 이상한 결과가 나온다.
                    _CurrentPhase = SearchPhase.DONE
                    'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    'ExitPrice = a_data.Price                   '청산가
                    'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    '_Done = True                             '청산완료 알리는 비트 셋
                    Exit Sub
#End If
                End If
            End If
#If 1 Then
            If TwoMinutesHolding Then
                If patterncheck_str.Price <> RecordList(RecordList.Count - 2).Price Or patterncheck_str.Amount <> RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매 종료됨
                    TwoMinutesHolding = False
                    '180117: EnterPrice 업데이트함
#If ALLOW_MULTIPLE_ENTERING Then
                    _EnterPriceMulti(_EnterPriceMulti.Count - 1) = patterncheck_str.Price
#Else
                    EnterPrice = patterncheck_str.Price                  '진입가
#End If

                End If
            End If
#End If
#End If
            If CheckPrethresholdWorkedCount Then
                '2024.01.27 : prethreshold 낚시 성공한 경우 다음 data 샘플을 보고 걸린애 될 운명이었는지 아닌지 판별해서 걸린애 될 운명이었다면 PrethresholdSucceed flag 를 0으로 두어 U 이 아니라 T 으로 기록되게 한다.
                Dim matching As Boolean = CheckMatching()
                If matching Then
                    PrethresholdSucceed = False
                    CheckPrethresholdWorkedCount = 0
                Else
                    CheckPrethresholdWorkedCount -= 1
                End If
            End If
            If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount AndAlso _WaitExitCount < 2 AndAlso StockOperator Is Nothing Then
                '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거는 이제 치지 말자
                '20220301: Double Fall 과 마찬가지로 FlexiblePCRenew 도 이제 진입하고 바로 변동성 완화 빠지는 경우 안 쳐주기로 한다. 운 좋게 진입한 경우 핸들링 하기 위해 StockOperator 확인 조건이 Double Fall 에서와 같이 추가되었다.
                _WaitExitCount = 0
                _CurrentPhase = SearchPhase.WAIT_FALLING
                MessageLogging(LinkedSymbol.Code & " :(PCRenew) 초반에 VI 걸려서 포기함")
                LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
            Else
                _WaitExitCount += 1         '청산기다림 카운트

                If AllowMultipleEntering Then
                    'multiple entering
                    Dim refall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - 4).Amount) * patterncheck_str.Price           '재하락 볼륨 업데이트
                    '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                    refall_volume = refall_volume * PatternLength / 3
                    If NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD - 1) * (TH_ATTENUATION)) AndAlso refall_volume > _FallVolumeMulti.First * VOLUME_ATTENUATION Then
                        '추가매수해
                        _EnterPriceMulti.Add(patterncheck_str.Price)
                        _FallVolumeMulti.Add(refall_volume)
                        '_NumberOfEntering += 1
                        TargetBuyPrice = LastEnterPrice
                    End If
                End If

                '청산 기다리기 모드
                '_BasePrice = Math.Min(_BasePrice, unidelta_str.Price)           '바닥가 업데이트
                If _BasePrice >= patterncheck_str.Price Then
                    _BasePrice = patterncheck_str.Price
                    _CountFromLastBase = 0
                Else
                    _CountFromLastBase += 1
                End If
                If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                    _TopPrice = patterncheck_str.Price
                    _CountFromLastTop = 0
                Else
                    _CountFromLastTop += 1
                End If
                '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
                '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
                'PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
                'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
                'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
#If OLD_FLEXIBLE_HAVING_TIME Then
                If (_CountFromLastBase >= DEFAULT_HAVING_TIME * (Pattern.Length + (MatchedFlexIndex - (Pattern.Length - FlexPatternList(0).Length))) / Pattern.Length) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
#Else
                If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
#End If
                    '그냥 때가 되었다.
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                    ExitPrice = a_data.CoreRecord.Price                   '청산가
                    ProfitCalculation()
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋

                    'ElseIf (CType(patterncheck_str.Price, Double) - _BasePrice) / _TargetHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                    '목표수익 달성
                    '_CurrentPhase = SearchPhase.DONE
                    'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    'ExitPrice = a_data.Price                   '청산가
                    'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    '_Done = True                             '청산완료 알리는 비트 셋
                    'ElseIf ((CType(patterncheck_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                    '손절매
                    '_CurrentPhase = SearchPhase.DONE
                    'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    'ExitPrice = a_data.Price                   '청산가
                    'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    '_Done = True                             '청산완료 알리는 비트 셋
                    'If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    '_SecondChance = True
                    'End If
                End If
            End If
        End If

#If 1 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator Is Nothing Then
#If 0 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / (CAREFULL_FACTOR * (LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                If safe_rate > 1 Then
                    safe_rate = 1
                ElseIf safe_rate < 0 Then
                    safe_rate = 0
                End If
                Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                    final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                End If
                MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                    StockOperator.SetAccountManager(MainForm.AccountManager)
                Else
                    '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                    EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                    MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                    '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                End If
            End If
#End If
        Else
            '청산여부 혹은 진입 취소 여부 판단
            If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                If _CurrentPhase = SearchPhase.DONE Then
                    '140701 : DONE이면 Stock operator 초기화는 Symbol에서 수익률 계산 후에 하고
#If 0 Then
                    '2024.01.07 : EXITING_PRETHRESHOLD 이면서 상태가 DONE 인 경우는 이제 없을 것임
#If PATTERN_PRETHRESHOLD Then
                ElseIf _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then
                    'prethreshold 취소하려는 경우
#If 0 Then
                    If StockOperator.BoughtAmount > 0 Then
                        '체결된 수량이 있는 경우 => 다시 WAIT_FALLING 으로 돌아가는 것을 포기하고, Symbol 에서 수익률 계산 후에 stock operator 초기화 한다.
                        _CurrentPhase = SearchPhase.DONE
                        ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5 + 2.5)              '청산시간
                        ExitPrice = a_data.CoreRecord.Price                   '청산가
                        ProfitCalculation()
                        TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                        _Done = True                             '청산완료 알리는 비트 셋
                        PrethresholdSucceed = True              '2023.12.27 : 체결이 너무 늦어서 그리 성공이라 할 수는 없는 케이스지만 그래도 화면상엔 어쨌든 표시해주기 위함이다.
                    Else
                        '체결된 수량이 없는 경우 => 상태에 따라 다른 상태로 이동한다.
                        'prethreshold 가 정상적으로 취소되었다.
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        LinkedSymbol.StopPriceinfo()             '종목에 대해 가격정보 그만요청
                        'StockOperator 는 terminate 시킨다.
                        StockOperator.Terminate()
                        StockOperator = Nothing
                    End If
#End If
                    If StockOperator.BoughtAmount > 0 Then
                        '체결된 수량이 있음 => DealInitialte 에서 WAIT_EXIT_TIME 상태로 이동했을 거기 때문에 이런 경우는 없을 거라고 여겨진다.
                        ErrorLogging(LinkedSymbol.Code & " : PRETHRESHOLD 취소하려는데 체결된 수량이 있음")
                        _CurrentPhase = SearchPhase.DONE
                    Else
                        '체결된 수량이 없음 => WAIT_FALLIG 상태로 돌아감
                        MessageLogging(LinkedSymbol.Code & " : PRETHRESHOLD 취소되었고 WAIT_FALLING 상태로 돌아감")
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                    End If
#End If
#End If
                Else
                    '140701 : DONE이 아니면, 즉, WAIT FALLING 상태로 돌아갔다던가 하면 여기서 초기화해준다.
                    If StockOperator.EnterExitState = EnterOrExit.EOE_Exit AndAlso (StockOperator.InitPrice = LinkedSymbol.LowLimitPrice Or StockOperator.EarlyExit) Then
                        '단 한가지, 하한가로 판 이력이 있거나 조기 매도 했다면 추가로 매수가 되지 않게 stock operator 초기화를 하지 않는다
                        StockOperatorDebug += 100
                    Else
                        StockOperatorDebug += 1000
                        StockOperator.Terminate()
                        StockOperator = Nothing
                    End If
                End If
            Else  ''청산 혹은 매수중단 완료되지 않은 경우
#If PATTERN_PRETHRESHOLD Then
                If _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then
                    '매수중단 명령 보낸다.
                    'StockOperator.Liquidate(0)       '청산 주문
                    '2024.01.07 : Liquidate 에서 StopBuying 함수를 부르는 것으로 바뀌었다.
                    StockOperator.StopBuying()
                    IsStopBuyingCompleted = False
                Else
#Else
                '2023.12.25 : 예전 prethreshold 로직이다. 쓸모 없는 로직이지만 만약을 위해 남겨둔다.
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso _CurrentPhase = SearchPhase.WAIT_FALLING Then
                    'pre threshold로 주문 들어갔던 것이 가격이 올라가면서 취소해야 되는 상항.
                    '140619 : Liquidate이 적절할 지 모르겠지만 현 상황에서는 최선인 것 같다. 나중에 검토해보자....................................
                    '20230526 : pre threshold 안 쓴지가 언젠데.. 이 코드가 남아서 어떤 영향을 미치는지 조사할 필요가 있다. (일단 FlexiblePCRenew 에 대해서만이라도)
                    StockOperatorDebug += 10000
                    StockOperator.Liquidate(0)
                Else
#End If

                    If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso (StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Or StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) AndAlso ExitPrice <> 0 Then
                        StockOperatorDebug += 100000
                        '청산 시점.
                        If time_over_clearing Then
                            StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                        Else
                            StockOperator.Liquidate(0)       '청산 주문
                        End If
                    ElseIf StockOperator.EnterExitState = EnterOrExit.EOE_Exit Then
                        StockOperatorDebug += 1000000
                        '2024.02.18 : time over clearing 으로 하한가로 청산 주문 들어갔는데도 다시 취소하는 문제가 있어 아래에 AndAlso 뒤의 조건을 달았다.
                        If time_over_clearing AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED Then
                            '2021.08.01: 지난 금요일 청산시도중 clearing time이 도래했는데 하한가로 파는 로직이 발동이 안 되어 청산이 안 되었다.
                            '이미 청산중이면 재주문시 바로 체결될 수 있게 LowLimitCaution flag를 셋해둔다
                            'StockOperator.ChangeLowLimitCautionFlag()
                            '2024.02.08 : ChangeLowLimitCautionFlag() 를 써도 200ms timer 에서 매도취소가 안 되기 때문에 결국 하한가매도는 동작하지 않는다. Liquidate 에 Exit 상태용 하한가매도 로직을 구현하고 이걸로 바꾸도록 한다.
                            StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                        End If
                    End If
                End If
            End If
        End If
#End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    Public Overrides Sub BuyingInitiated()
        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '2023.12.27 : 여기서 직접 _CurrentPhase 를 WAIT_EXIT_TIME 으로 바꾸기에는 race condition 의 위험부담이 있다.
            '2023.12.27 : 그렇다고 여기다 OneKey 에 대한 critical section 을 만들기에는 OperationKey 와 꼬일 염려가 있다.
            '2023.12.27 : 그리고 어차피 걸린애 리스트에 표시하기 위해 Prethreshold Succeed 를 나타내는 변수가 필요하였다.
            '2023.12.27 : 다음번 data received 이벤트에서 이 비트를 봐서 WAIT_EXIT_TIME 으로 바꾸면 된다.
            PrethresholdSucceed = True
            MessageLogging(LinkedSymbol.Code & " :(PCRenew) prethreshold 낚시 성공 오예~")
        ElseIf _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then
            '2024.01.07 : 매수중지 중에 체결된 경우. 마찬가지로 Prehreshold 성공으로 처리한다.
            PrethresholdSucceed = True
            MessageLogging(LinkedSymbol.Code & " :(PCRenew) prethreshold 중지중에 체결됨. 낚시 성공으로 간주함~")
        End If
    End Sub

    Public Overrides Sub StopBuyingCompleted()
        If _CurrentPhase = SearchPhase.EXITING_PRETHRESHOLD Then

            '2024.01.07 : decision maker 의 task 를 만들어야 한다. 그 안에서 _CurrentPhase 를 WAIT_FALLING 으로 바꾼다.

            '2024.01.08 : symbol task 를 call한다.
            IsStopBuyingCompleted = True
            SymTree.SymbolTaskToQueue(LinkedSymbol)
        End If

        '2024.01.07 : 또한 account task 를 실행시켜 score 계산을 통해 가장 score 높은 놈에게 매수기회를 줘야 한다.
        SymTree.AccountTaskToQueue(MainForm.TestAccountManager)
    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

#If 0 Then

Public Class c05E_PatternChecker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
    End Structure

    Private PricePointList, DecideTimePointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _BasePrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    'Public TwoMinutesHolding As Boolean = False
    Public PatternName As String
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    'Public Pattern() As UInt32 = {100, 96, 92, 88, 84, 80, 76, 72, 63, 54, 44, 34, 23, 12, 0}
    'Public Pattern() As UInt32 = {100, 98, 96, 94, 92, 90, 88, 85, 76, 66, 54, 42, 30, 15, 0}
    'Public Pattern() As Double = {100, 100, 100, 100, 100, 100, 98, 96, 94, 92, 90, 88, 85, 76, 66, 54, 42, 30}
    'Public Pattern() As UInt32 = {68, 80, 88, 97, 100, 96, 87, 75, 60, 40, 50, 63, 70, 75, 77, 73, 64, 48, 30, 11, 0}
    'Public Pattern() As Double = {0, 8.991976163, 17.10155794, 23.55771001, 27.79806153, 29.53987788, 28.81574212, 25.96944288, 21.61258799, 16.54741888, 11.66556696, 7.835528828, 5.793061114, 6.048327289, 8.821514507, 14.01504577, 21.22589423, 29.79644562, 38.89748977, 47.63286781, 55.15256831, 60.75999954, 63.99990007, 64.71579322, 63.06973455, 59.52186032, 54.77231779, 49.67290599, 45.11957778, 41.93936973, 40.78602928}
    'Public Pattern() As Double = {0, 4.991976163, 9.101557944, 11.55771001, 11.79806153, 9.539877883, 4.815742116, -2.030557118, -10.38741201, -19.45258112, -28.33443304, -36.16447117, -42.20693889, -45.95167271, -47.17848549, -45.98495423, -42.77410577, -38.20355438, -33.10251023, -28.36713219, -24.84743169, -23.24000046, -24.00009993, -27.28420678, -32.93026545, -40.47813968, -50, -63, -80, -100, -120}        'sheet13
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -32, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40, -33, -20, -16, -22, -37, -44, -33}
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}      :Sheet11
    'Public Pattern() As Double = {9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 0, 0, 0}
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}      'Gold with 꼬리올림
    Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver

    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public _DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    Public _BEGINNING_MARGIN As Integer = 7

    Public SCORE_THRESHOLD As Double = 3.1
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 1.069

    Public OneMoreSampleCheck As Boolean = False

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer, ByVal account_cat As Integer)
        MyBase.New(linked_symbol, start_time)

        'SavedIndexDecisionCenter = index_decision_center
        '200109: New로 전달되는 is_for_main을 AccountCat으로 바꿔야 할 것 같다.그리고 아래서 저장하는 부분 수정해야 함.그리고는 MainAccount 참조하는 곳 다 찾아서 SubAccount도 같은 방식으로 써넣는다.
        '200109: 그리고는 Sub Account 관련내용은 화면에 표시 안 되도록 해야 한ㄷ.
        _AccountCat = account_cat
        FALL_VOLUME_THRESHOLD = 400000000000
        Select Case index_decision_center
            'MAIN DECISION MAKERS
            Case 0
                PatternName = "Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
                _DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.1
                FALL_SCALE_LOWER_THRESHOLD = 1.069
            Case 1
                PatternName = "Double Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver            Case Else
                _DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.45
                FALL_SCALE_LOWER_THRESHOLD = 1.08
            Case 2
                PatternName = "Quadruple Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadruple silver
                _DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 2.5
                FALL_SCALE_LOWER_THRESHOLD = 1.1
            Case 3
                PatternName = "ScaleUpTwoStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}     'ScaleUpTwoStair
                _DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 4.5
                FALL_SCALE_LOWER_THRESHOLD = 1.1
            Case 4
                PatternName = "ScaleUpThreeStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.804061708, -1.197653035, -1.439032227, -1.580312757, -1.658899382}    'ScaleUpThreeStair
                _DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.5
                FALL_SCALE_LOWER_THRESHOLD = 1.12
            Case 5
                PatternName = "ScaleUpFourStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair
                _DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.2
                FALL_SCALE_LOWER_THRESHOLD = 1.13
                'TEST DECISION MAKERS
            Case 6
                PatternName = "Double Silver-1"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036}        ' Double Silver            Case Else
                _DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.25
                FALL_SCALE_LOWER_THRESHOLD = 1.0375
#If 0 Then
                PatternName = "[TEST]Double Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver            Case Else
                _DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.45
                FALL_SCALE_LOWER_THRESHOLD = 1.06
#End If
            Case Else
                MsgBox("예상치 못한 index_decision_center")
        End Select
        'SCORE_THRESHOLD = TestArray(TestIndex)

        'Pattern normalizing
        Dim b_min As Double = Pattern.Min
        Dim b_max As Double = Pattern.Max

        'normalizing
        For index As Integer = 0 To Pattern.Length - 1
            Pattern(index) = 100 * (Pattern(index) - b_min) / (b_max - b_min)
        Next

        '_DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령.
        If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = Now 'StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            ProfitCalculation()
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub

    Public Overrides Sub CreateGraphicData()
        '160520: 그래프 어떻게 그릴까 생각해보자. 앞뒤로 쪼금씩 더 보여주는 게 필요하겠지.
        '160521: 기본적으로 보유중인 패턴들 중 최대길이 가진 놈보다 몇 개정도 더 긴 길이 만큼의 히스토리를 관리하도록 한다.
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            'a_point.Y = RecordList(index).MAPrice
            'MAPointList.Add(a_point)                                '이동평균 그래프자료 만들기
            'a_point.Y = RecordList(index).BuyMiniSum
            'BuyMiniSumPointList.Add(a_point)                        'BuyMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).SelMiniSum
            'SelMiniSumPointList.Add(a_point)                        'SelMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).Unidelta
            'UnideltaPointList.Add(a_point)                          'Unidelta 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5, 1))                '패턴시작시간
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5 + 0.001, 0))        '패턴시작시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (Pattern.Count - 1) * 5, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (Pattern.Count - 1) * 5 + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 0))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 0))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 1))                 '청산시간
    End Sub

    '등록된 pattern과 현재 RecordList의 최신 data를 비교하여 matching 여부 체크함.
    Private Function CheckMatching() As Boolean
        Dim result As Boolean

        If RecordList.Count > Pattern.Length Then
            '패턴체크할 길이가 되었다.
            Dim target_normalized(Pattern.Length - 1) As UInt32
            For index As Integer = 0 To Pattern.Length - 1
                target_normalized(index) = RecordList(RecordList.Count - Pattern.Length + index).Price
            Next
            Dim b_min As UInt32 = target_normalized.Min
            Dim b_max As UInt32 = target_normalized.Max

            'normalizing
            If b_max = b_min Then
                result = False
                Return result
            Else
                For index As Integer = 0 To Pattern.Length - 1
                    target_normalized(index) = Math.Round(100 * (target_normalized(index) - b_min) / (b_max - b_min))
                Next
            End If

            Dim score As Double = 0
            For index As Integer = 0 To Pattern.Length - 1
                'If target_normalized(index) > Pattern(index) Then
                score = score + (target_normalized(index) - Pattern(index)) ^ 2
                'Else
                'score = score + (Pattern(index) - target_normalized(index)) ^ 2
                'End If
            Next
            score = Math.Sqrt(score) / Pattern.Length

            If score < SCORE_THRESHOLD And b_max / b_min > FALL_SCALE_LOWER_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
            'If LinkedSymbol.Code = "A053060" Then
            'MessageLogging("IndexForDebug " & SavedIndexDecisionenterForDebug.ToString & " Score " & score.ToString & " / threshold " & SCORE_THRESHOLD & ", Fall scale threshold " & FALL_SCALE_LOWER_THRESHOLD)
            'End If
        Else
            result = False
        End If

        Return result
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

        patterncheck_str.Price = a_data.CoreRecord.Price         '주가 저장
        patterncheck_str.Amount = a_data.CoreRecord.Amount         '거래량 저장

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)       'Recordlist에 추가하기 전이니까 count에 -1하면 안 된다.

        If IsClearingTime(current_time) Then
            ClearNow(a_data.CoreRecord.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + _BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.
        '181006: EnterTime은 패턴의 시작점이 아니고 끝점이 맞다.

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드

            Dim matching As Boolean = CheckMatching()

            If matching = True Then
#If 0 Then
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                    TwoMinutesHolding = True
                End If
#End If
                'Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                'If fall_volume < FALL_VOLUME_THRESHOLD Then
                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)             '진입시간
                EnterPrice = patterncheck_str.Price                  '진입가
                _BasePrice = patterncheck_str.Price             '바닥가 기록
                FallVolume = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                OneMoreSampleCheck = True          '실전에서는 원모어샘플 없다. => 있다 (180707)
                LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                If LinkedSymbol.IsCallPriceAvailable Then
                    '호가 available하면
                    If LinkedSymbol.VI Then
                        '매수시도는 나중에 하고 VI 풀리면 EnterPrice 업데이트될 수 있도록 기록해둔다.
                        VI_CheckStatus = VI_CheckStatusType.WAIT_UNLOCK
                        MessageLogging(LinkedSymbol.Code & " : VI 풀리기 기다려야 한다.")
                    Else
                        Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
                        VI_CheckStatus = VI_CheckStatusType.UNLOCKED
                        MessageLogging(LinkedSymbol.Code & " : VI 이미 풀려있어 매수 시도 하면 된다.")
                        '매수시도를 하도록 하면 된다.
                        'TargetBuyPrice 설정
                        'TargetBuyPrice = Math.Max(MainForm.MainAccountManager.NextCallPrice(local_call_price.BuyPrice1, 1, LinkedSymbol.MarketKind), LastEnterPrice)     'BuyPrice1 과 SelPrice1의 중간값과 EnterPrice 둘 중에 큰 값으로 한다.
                        TargetBuyPrice = LastEnterPrice     '2022.07.18: 위의 방식의 장점을 모르겠다. 이제부터는 EnterPrice가 곧 TargetPrice이다.
                        If AccountCat = 0 Then
                            MainForm.MainAccountManager.DecisionRegister(Me)
                        ElseIf AccountCat = 1 Then
                            MainForm.SubAccountManager.DecisionRegister(Me)
                        Else    'If AccountCat = 2
                            MainForm.TestAccountManager.DecisionRegister(Me)
                        End If
                    End If
                Else
                    '호가 available하지 않으면
                    VI_CheckStatus = VI_CheckStatusType.NOT_CHECKED
                    MessageLogging(LinkedSymbol.Code & " : VI 는 커녕 호가도 available하지 않아 기다려야 한다.")
                End If
                'Else
                'FallVolume이 너무 크면 오히려 마이너스가 된다.
                'End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
#If 0 Then
            If OneMoreSampleCheck Then
                OneMoreSampleCheck = False
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 변동성완화장치 풀릴 때까지 기다린다.
                    TwoMinutesHolding = True
                End If
            End If
            If TwoMinutesHolding Then
                If patterncheck_str.Price <> RecordList(RecordList.Count - 2).Price Or patterncheck_str.Amount <> RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매 종료됨
                    TwoMinutesHolding = False
                    '180117: EnterPrice 업데이트함
                    EnterPrice = patterncheck_str.Price                  '진입가
                End If
            End If
#End If
            _WaitExitCount += 1         '청산기다림 카운트
            '청산 기다리기 모드
            '_BasePrice = Math.Min(_BasePrice, unidelta_str.Price)           '바닥가 업데이트
            If _BasePrice >= patterncheck_str.Price Then
                _BasePrice = patterncheck_str.Price
                _CountFromLastBase = 0
            Else
                _CountFromLastBase += 1
            End If
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= _DEFAULT_HAVING_TIME) Then
            If (_CountFromLastBase >= _DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                ExitPrice = a_data.CoreRecord.Price                   '청산가
                ProfitCalculation()
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                'ElseIf (CType(unidelta_str.Price, Double) - _BasePrice) / _FallHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                '_CurrentPhase = SearchPhase.DONE
                'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                'ExitPrice = a_data.Price                   '청산가
                'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                '_Done = True                             '청산완료 알리는 비트 셋
                'ElseIf ((CType(unidelta_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                '_CurrentPhase = SearchPhase.DONE
                'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                'ExitPrice = a_data.Price                   '청산가
                'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                '_Done = True                             '청산완료 알리는 비트 셋
                'If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                '_SecondChance = True
                'End If
            End If
        End If

        '140124 : Decision maker에서 pre threshold 걸린 직후 T1101을 request하는 방법을 생각해보자.=> 그리 됐다.

        '131115 : 사실 이것보다 더 간단한 방법이 있었을 수 있다. 그냥 threshold를 올리고 주문 넣는 가격을 낮추는 것이다.
        ' 그러면 미리 주문한 효과를 얻을 수 있다. 다만 threshold를 올리는 만큼 후보가 되는 종목들이 많아질텐데
        ' 그 중에서 실제 거래될 확률이 높은 놈을 찾아내는 것이 쉽지 않을 듯 하다.
        ' 그리고 15초를 5초로 바꾸면서 갑자기 순식간에 threshold 아래로 떨어지는 놈들을 미리 발견할 확률이 높아졌기 때문에
        ' 이것만으로 거래될 확률은 높일 수 있다고 생각한다. 좀 더 깊이 생각해보도록 하자.
        '131121 : 다시 pre-threshold를 넣는 방향으로 급선회. 대신 flag가 아니라 새로운 상태를 두도록 하고
        ' Operation class는 없어지고 account manager가 해당 decision객체와 거기에 딸린 operator들을 관리하도록 한다.
        '131127 : 사실 이제 주문 넣는데 굳이 다시 현재가 문의를 할 필요는 없어보인다. 그냥 account manager가 threshold level 가격에
        ' 끼워 넣으면 된다.
#If 1 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator Is Nothing Then
#If 0 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / (CAREFULL_FACTOR * (LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                If safe_rate > 1 Then
                    safe_rate = 1
                ElseIf safe_rate < 0 Then
                    safe_rate = 0
                End If
                Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                    final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                End If
                MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                    StockOperator.SetAccountManager(MainForm.AccountManager)
                Else
                    '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                    EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                    MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                    '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                End If
            End If
#End If
        Else
            '청산여부 혹은 진입 취소 여부 판단
            If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                If _CurrentPhase <> SearchPhase.DONE Then
                    '140701 : DONE이면 Stock operator 초기화는 Symbol에서 수익률 계산 후에 하고, 아니면, 즉, WAIT FALLING 상태로 돌아갔다던가 하면 여기서 초기화해준다.
                    If StockOperator.EnterExitState = EnterOrExit.EOE_Exit AndAlso (StockOperator.InitPrice = LinkedSymbol.LowLimitPrice Or StockOperator.EarlyExit) Then
                        '단 한가지, 하한가로 판 이력이 있거나 조기 매도 했다면 추가로 매수가 되지 않게 stock operator 초기화를 하지 않는다
                    Else
                        StockOperator.Terminate()
                        StockOperator = Nothing
                    End If
                End If
            Else
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso _CurrentPhase = SearchPhase.WAIT_FALLING Then
                    'pre threshold로 주문 들어갔던 것이 가격이 올라가면서 취소해야 되는 상항.
                    '140619 : Liquidate이 적절할 지 모르겠지만 현 상황에서는 최선인 것 같다. 나중에 검토해보자....................................
                    StockOperator.Liquidate(0)
                End If
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso (StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Or StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) AndAlso ExitPrice <> 0 Then
                    '청산 시점.
                    If time_over_clearing Then
                        StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                    Else
                        StockOperator.Liquidate(0)       '청산 주문
                    End If
                End If
            End If
        End If
#End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Overrides Sub BuyingInitiated()
        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
            EnterTime = Now 'StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
            'EnterPrice = a_data.Price                  '진입가
            '_BasePrice = EnterPrice             '바닥가 기록
            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
            'FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

            '_FallHeight = _FallingStartPrice - EnterPrice           '하락폭 기록
        End If
    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

Public Class c05G_DeltaGangdo
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure DeltaGangdoStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        Dim BuyAmount As UInt64
        Dim SelAmount As UInt64
        Dim DeltaGangdo As Double
    End Structure

    Public DEFAULT_HAVING_TIME As Integer = 30 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 50
    Public DELTA_PERIOD As Integer = 9
    'Public Shared DELTA_GANGDO_THRESHOLD_PRE As Double = 160
    Public DELTA_GANGDO_THRESHOLD As Double = 221.7984
    Public MINIMUM_AMOUNT As UInt64 = 10
    Public MINIMUM_PRICE_RATE As Double = -0.025

    Private PricePointList, DeltaGangdoPointList, DecideTimePointList As PointList
    Private PriceCompositeData, DeltaGangdoCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of DeltaGangdoStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _BasePrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Public EnterDeltaGangdo As Double = 0
    'Public TwoMinutesHolding As Boolean = False

    'Public OneMoreSampleCheck As Boolean = False
    'Public BuyAmountCenter As Double = 0
    'Public SelAmountCenter As Double = 0
    'Public PatternGangdo As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        _CurrentPhase = SearchPhase.WAIT_FALLING

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        'DeltaGangdo CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("DeltaGangdo", DataType.REAL_NUMBER_DATA, Nothing)
        DeltaGangdoCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DeltaGangdo")
        DeltaGangdoPointList = New PointList()
        DeltaGangdoCompositeData.SetData(DeltaGangdoPointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DeltaGangdoCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다. 현재는 pattern check 에 주력
    End Sub

    Public Overrides Sub CreateGraphicData()
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            a_point.Y = RecordList(index).DeltaGangdo
            DeltaGangdoPointList.Add(a_point)                       'DeltaGangdo 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5, 1))                '패턴시작시간
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5 + 0.001, 0))        '패턴시작시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 0))        '진입시간 + epsilon
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5, 1))                '진입시간
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5 + 0.001, 0))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 0))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 1))                 '청산시간
    End Sub

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim deltagando_str As DeltaGangdoStructure
        Dim time_over_clearing As Boolean = False
        Dim buy_amount As UInt64

        deltagando_str.Price = a_data.Price         '주가 저장
        deltagando_str.Amount = a_data.Amount         '거래량 저장
        If a_data.Gangdo >= 0 Then
            buy_amount = a_data.Gangdo * a_data.Amount / (100 + a_data.Gangdo)
            deltagando_str.BuyAmount = buy_amount
            If buy_amount > a_data.Amount Then
                'float 계산이 잘못되었을 수 있다.
                deltagando_str.SelAmount = 0
            Else
                deltagando_str.SelAmount = a_data.Amount - buy_amount
            End If
        End If

        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            Dim delta_buy As UInt64 = Math.Max(1, CType(deltagando_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            Dim delta_sel As UInt64 = Math.Max(1, CType(deltagando_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            deltagando_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            deltagando_str.DeltaGangdo = -1
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(deltagando_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드
            'If RecordList.Count >= 5 AndAlso (deltagando_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (RecordList(RecordList.Count - 2).DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (RecordList(RecordList.Count - 3).DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (RecordList(RecordList.Count - 4).DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (RecordList(RecordList.Count - 5).DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (CType(a_data.Amount, Long) - RecordList(RecordList.Count - DELTA_PERIOD).Amount) > MINIMUM_AMOUNT AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MINIMUM_PRICE_RATE) Then
            If (deltagando_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (RecordList(RecordList.Count - 2).DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (RecordList(RecordList.Count - 3).DeltaGangdo > DELTA_GANGDO_THRESHOLD) AndAlso (CType(a_data.Amount, Long) - RecordList(RecordList.Count - DELTA_PERIOD).Amount) > MINIMUM_AMOUNT AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MINIMUM_PRICE_RATE) Then
                'If deltagando_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD AndAlso (CType(a_data.Amount, Long) - RecordList(RecordList.Count - DELTA_PERIOD).Amount) > MINIMUM_AMOUNT AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MINIMUM_PRICE_RATE) Then
                'If deltagando_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MINIMUM_PRICE_RATE) Then
                Dim fall_volume As UInt64 = (deltagando_str.Amount - RecordList(RecordList.Count - DELTA_PERIOD).Amount) * deltagando_str.Price           '대충 볼륨 계산
                'If fall_volume > FALL_VOLUME_THRESHOLD Then
                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                EnterPrice = deltagando_str.Price                  '진입가
                _BasePrice = deltagando_str.Price             '바닥가 기록
                FallVolume = fall_volume           '볼륨 업데이트
                EnterDeltaGangdo = deltagando_str.DeltaGangdo               '진입 델타강도
                LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청
                'Else
                'FallVolume이 너무 작으면 하지 말자
                'End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _WaitExitCount += 1         '청산기다림 카운트
            '청산 기다리기 모드
            '_BasePrice = Math.Min(_BasePrice, unidelta_str.Price)           '바닥가 업데이트
            If _BasePrice >= deltagando_str.Price Then
                _BasePrice = deltagando_str.Price
                _CountFromLastBase = 0
            Else
                _CountFromLastBase += 1
            End If
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            'PriceRateTrend.Add(deltagando_str.Price / EnterPrice)
            'If (_WaitExitCount >= _DEFAULT_HAVING_TIME) Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If deltagando_str.Price = RecordList(RecordList.Count - 2).Price AndAlso deltagando_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매상태로 인해 매도 못한다. 기다려야 된다.
                Else
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    ExitPrice = a_data.Price                   '청산가
                    Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                End If

                'ElseIf (CType(unidelta_str.Price, Double) - _BasePrice) / _FallHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                '_CurrentPhase = SearchPhase.DONE
                'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                'ExitPrice = a_data.Price                   '청산가
                'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                '_Done = True                             '청산완료 알리는 비트 셋
                'ElseIf ((CType(unidelta_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                '_CurrentPhase = SearchPhase.DONE
                'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                'ExitPrice = a_data.Price                   '청산가
                'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                '_Done = True                             '청산완료 알리는 비트 셋
                'If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                '_SecondChance = True
                'End If
            End If
        End If
#If 1 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator Is Nothing Then
#If 0 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / (CAREFULL_FACTOR * (LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                If safe_rate > 1 Then
                    safe_rate = 1
                ElseIf safe_rate < 0 Then
                    safe_rate = 0
                End If
                Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                    final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                End If
                MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                    StockOperator.SetAccountManager(MainForm.AccountManager)
                Else
                    '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                    EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                    MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                    '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                End If
            End If
#End If
        Else
            '청산여부 혹은 진입 취소 여부 판단
            If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                If _CurrentPhase <> SearchPhase.DONE Then
                    '140701 : DONE이면 Stock operator 초기화는 Symbol에서 수익률 계산 후에 하고, 아니면, 즉, WAIT FALLING 상태로 돌아갔다던가 하면 여기서 초기화해준다.
                    If StockOperator.EnterExitState = EnterOrExit.EOE_Exit AndAlso (StockOperator.InitPrice = LinkedSymbol.LowLimitPrice Or StockOperator.EarlyExit) Then
                        '단 한가지, 하한가로 판 이력이 있거나 조기 매도 했다면 추가로 매수가 되지 않게 stock operator 초기화를 하지 않는다
                    Else
                        StockOperator = Nothing
                    End If
                End If
            Else
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso _CurrentPhase = SearchPhase.WAIT_FALLING Then
                    'pre threshold로 주문 들어갔던 것이 가격이 올라가면서 취소해야 되는 상항.
                    '140619 : Liquidate이 적절할 지 모르겠지만 현 상황에서는 최선인 것 같다. 나중에 검토해보자....................................
                    StockOperator.Liquidate(0)
                End If
                If StockOperator.EnterExitState = EnterOrExit.EOE_Enter AndAlso (StockOperator.OpStatus = et2_Operation.OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Or StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) AndAlso ExitPrice <> 0 Then
                    '청산 시점.
                    If time_over_clearing Then
                        StockOperator.Liquidate(1)       '종료시간이 다되면 무조건 팔아야 하기 때문에 하한가 처럼 판다.
                    Else
                        StockOperator.Liquidate(0)       '청산 주문
                    End If
                End If
            End If
        End If
#End If

    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Overrides Sub BuyingInitiated()
        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
            EnterTime = Now 'StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
            'EnterPrice = a_data.Price                  '진입가
            '_BasePrice = EnterPrice             '바닥가 기록
            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
            'FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

            '_FallHeight = _FallingStartPrice - EnterPrice           '하락폭 기록
        End If
    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class
#End If
'2024.04.26 : decision maker 에서 price의 과거값 리스트들의 부분집합의 min max 값을 구하는데 들어가는 CPU load 를 절약하기 위해서 사용된다.
Public Class PriceWindowList
    Public BaseList As New List(Of UInt32)
    Public WindowSize As UInt32
    Public MinList As New List(Of UInt32)   'index n 의 값은 BaseList 0~n 까지의 min 값
    Public MaxList As New List(Of UInt32)   'index n 의 값은 BaseList 0~n 까지의 max 값
    Public OldMinList As List(Of UInt32)   'index n 의 값은 한 iteration 전의 BaseList 0~n 까지의 min 값
    Public OldMaxList As List(Of UInt32)   'index n 의 값은 한 iteration 전의 BaseList 0~n 까지의 max 값
    Public NormalizedList As New List(Of Double())
    Public MIN_NORM_LENGTH As UInt32

    Public Sub New(ByVal window_size As UInt32, minimum_length_for_normalize As UInt32)
        WindowSize = window_size
        MIN_NORM_LENGTH = minimum_length_for_normalize

        Dim norm_array As Double()
        For norm_index As Integer = MIN_NORM_LENGTH - 1 To WindowSize - 1
            norm_array = New Double(norm_index) {}
            NormalizedList.Add(norm_array)
        Next
    End Sub

    Public Sub Insert(ByVal value As UInt32)
        'base list 에 추가한다.
        BaseList.Insert(0, value)

        'limit 에 다다르면 맨 앞의 값을 지운다.
        If BaseList.Count > WindowSize Then
            BaseList.RemoveAt(BaseList.Count - 1)
            MinList.RemoveAt(BaseList.Count - 1)
            MaxList.RemoveAt(BaseList.Count - 1)
        End If

        '2024.05.21 : 쭉 같은 가격으로 가다가 마지막 단 한 번 하락으로 걸리는 녀석들을 걸러내기 위해 이런 걸 만들어냈다.
        OldMinList = MinList.ToList()
        OldMaxList = MaxList.ToList()

        'min max 값을 계산한다.
        MinList.Insert(0, value)
        MaxList.Insert(0, value)
        For index As Integer = 1 To BaseList.Count - 1
            MinList(index) = Math.Min(MinList(index), value)
            MaxList(index) = Math.Max(MaxList(index), value)
        Next

        'normalized list 를 업데이트한다.
        'NormalizedList.Clear()  '새로운 값이 들어오면서 기존에 계산했던 normalized list 는 재활용이 불가능하여 폐기한다.
        'Dim norm_array() As Double
        For norm_index As Integer = MIN_NORM_LENGTH - 1 To BaseList.Count - 1
            'norm_array = New Double(norm_index) {}
            For index As Integer = 0 To norm_index
                NormalizedList(norm_index - MIN_NORM_LENGTH + 1)(norm_index - index) = (BaseList(index) - MinList(norm_index)) / (MaxList(norm_index) - MinList(norm_index))
            Next
            'NormalizedList.Add(norm_array)
        Next
    End Sub
End Class
