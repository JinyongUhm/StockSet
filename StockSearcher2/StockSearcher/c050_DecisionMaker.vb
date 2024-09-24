Imports ClassLibrary1

Public MustInherit Class c050_DecisionMaker

    Public Enum SearchPhase
        INIT            '초기상태에서 이미 현재 종가가 threshold보다 더 아래인 로우컷 밑으로 내려가 있으면 계속 걸린애가 탄생하는 오류를 막기 위해 만들었다.
        WAIT_FALLING
        UNWANTED_RISING_DETECTED
        WAIT_SECONDFALL
        PRETHRESHOLDED
        WAIT_EXIT_TIME
        DONE
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
    'Public FallVolumeLimited As UInt64
    'Public VOLUME_LIMIT As UInt64 = 500000000

    'Public _NumberOfEntering As Integer
    Public _EnterPriceMulti As New List(Of Integer)
    Public _FallVolumeMulti As New List(Of UInt64)
    Public Const ALLOWED_ENTERING_COUNT As Integer = 3
    Public TH_ATTENUATION As Double = 0.45 '0.4
    Public VOLUME_ATTENUATION As Double = 0.3 '0.3
    'Public ENTERING_POINT_FROM_LAST_TOP As Integer = 8
    'Public ProfitMUlti As New List(Of Double)
    'Public _LastEnteredPoint As Integer

    Public ExitPrice As Integer
    Public Profit As Double
    Public TookTime As TimeSpan
    Public GraphicCompositeDataList As New List(Of c011_PlainCompositeData)
    Public StockOperator As Boolean
    Friend _CurrentPhase As SearchPhase
    'Friend _CurrentPhaseKey As Integer
    Public ScoreSave As Double
    'Public OperatorList As New List(Of et21_Operator)
    Public PriceRateTrend As New List(Of Single)
    'Public StoredHavingTime As Integer
#If MOVING_AVERAGE_DIFFERENCE Then
    Public FALL_VOLUME_THRESHOLD As UInt64 = 400000000000 '33000000 '50000000
#Else
    Public FALL_VOLUME_THRESHOLD As UInt64 = 400000000000 '900000000
#End If
    Public FALL_VOLUME_LOWESHOLD As UInt64 = 2000000
    Public YieldForHighLevel As Boolean = False '2020.09.13: 고레벨 걸린애들을 위한 양보 여부
    Public FakeGullin As Boolean = False '2022.07.24: 걸리는 조건을 만족했지만 EXIT 할 때까지 패스하고 싶을 때 쓴다.

    Public MustOverride Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
    Public MustOverride Sub CreateGraphicData()
    Public MustOverride Sub GetSecondChanceInformation(ByVal old_decision_maker As c050_DecisionMaker)
    Public MustOverride Sub ClearNow(ByVal current_price As UInt32)

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        LinkedSymbol = linked_symbol
        StartTime = start_time
    End Sub

    '모은 데이타 폐기
    Public Sub Clear()

    End Sub

    Public Property EnterPrice As Integer
        Get
            Return _EnterPriceMulti.Average
        End Get
        Set(value As Integer)
            If AllowMultipleEntering AndAlso NumberOfEntering > 1 Then
                DebugNeedLogicCheck = True
            End If
            _EnterPriceMulti.Clear()
            _EnterPriceMulti.Add(value)
            '_LastEnteredPoint = value
            '_NumberOfEntering = 1
        End Set
    End Property

    Public Property LastEnterPrice As Integer
        Get
            Return _EnterPriceMulti.Last
        End Get
        Set(value As Integer)
            If Not AllowMultipleEntering AndAlso NumberOfEntering > 1 Then
                DebugNeedLogicCheck = True
            End If
            _EnterPriceMulti(_EnterPriceMulti.Count - 1) = value
        End Set
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
            If AllowMultipleEntering Then
                _FallVolumeMulti.Add(value)
            Else
                If NumberOfEntering > 1 Then
                    DebugNeedLogicCheck = True
                End If
                _FallVolumeMulti.Clear()
                _FallVolumeMulti.Add(value)
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

    Public ReadOnly Property PatternLength As Integer
        Get
            If TypeOf (Me) Is c05F_FlexiblePCRenew Then
                Dim me_as_pattern_decider As c05F_FlexiblePCRenew = Me
                Return me_as_pattern_decider.Pattern.Length
            ElseIf TypeOf (Me) Is c05G_DoubleFall Then
                Dim me_as_pattern_decider As c05G_DoubleFall = Me
                Return me_as_pattern_decider.Pattern2.Length
                'ElseIf TypeOf (Me) Is c05E_PatternChecker_copy1 Then
                'Dim me_as_pattern_decider As c05E_PatternChecker_copy1 = Me

                'Return me_as_pattern_decider.Pattern.Length
                'ElseIf TypeOf (Me) Is c05E_PatternChecker_copy2 Then
                'Dim me_as_pattern_decider As c05E_PatternChecker_copy2 = Me

                'Return me_as_pattern_decider.Pattern.Length
                'ElseIf TypeOf (Me) Is c05E_PatternChecker_copy3 Then
                'Dim me_as_pattern_decider As c05E_PatternChecker_copy3 = Me

                'Return me_as_pattern_decider.Pattern.Length
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
    Public VOLUME_LEVEL0 As Integer = 20000000
    Public VOLUME_LEVEL1 As Integer = 80000000
    Public VOLUME_LEVEL2 As Integer = 160000000
    ' ==================================================================

    Public NEWSTAB_EFFECT As Double = 10 ^ 30
    'Public ENTER_THRESHOLD_FROM_MA_PRICE35 As Double = 0.0432 '0.0924
    'Public ENTER_THRESHOLD_FROM_MA_PRICE70 As Double = 0.0632 '0.0924
    'Public ENTER_THRESHOLD_FROM_MA_PRICE140 As Double = 0.0832 '0.0924
    'Public EXIT_THRESHOLD_FROM_MA_PRICE As Double = -0.02 '.030285 '0.02
    Public LOW_CUT_FROM_MA_PRICE As Double = 1
    Public ENTER_POWER As Double = 0
    Public CUT_RELATIVE_FALL As Double
    'Public THRESH_VOL_A As Single = 0.02
    'Public THRESH_VOL_B As Single = 0.024
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
    Public PartIndex As Integer = 0
    Public UsedThreshold As Single
    Private _BasePrice As UInteger = 0
    Private _FallMinutePassed As Integer = 0
    Private _RiseMinutePassed As Integer = 0
    Private _CountFromLastBase As Integer = 0
    Public EnterMAFallRate As Single
    Public EnterMAPrice As UInteger = 0
    Public MABase As MA_Base_Type = MA_Base_Type.MA_BASE_0035
    Public OnlyAllowHighNewstab As Boolean = False
    Public RelativeFall As Double = 0
    'Public UsedMABase As Integer = 0
    'Public DaysPassed As Integer = 0
    'Public EnterPoint As Integer = 0
    'Public EXIT_THRESHOLD_FROM_MA_PRICE As Double = ENTER_THRESHOLD_FROM_MA_PRICE / 2
    'Public AdaptiveThreshold As Double
    'Public AdaptiveExitMul As Double = 0.5
    'Public MaxDepth As Double = [Double].MinValue
    Private HighPricePointList, LowPricePointList, MA_VarPointList, MA_PricePointList, DecideTimePointList, CandleTimePointList As PointList
    Private HighPriceCompositeData, LowPriceCompositeData, MA_VarCompositeData, MA_PriceCompositeData, DecideTimeCompositeData, CandleTimeCompositeData As c011_PlainCompositeData

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer)
        MyBase.New(linked_symbol, start_time)

        _CurrentPhase = SearchPhase.INIT

        Select Case index_decision_center
            Case 0
#If 0 Then
                MABase = MA_Base_Type.MA_BASE_9600
                DEFAULT_HAVING_MINUTE = 2000
                HAVING_MINUTE_FROM_BASE = 2000
                DecisionType = 2
                ENTER_THRESHOLD_LEVEL0 = 0.18
                ENTER_THRESHOLD_LEVEL1 = 0.18
                ENTER_THRESHOLD_LEVEL2 = 0.18
                ENTER_THRESHOLD_LEVEL3 = 0.18
                OnlyAllowHighNewstab = False
#Else
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
#End If
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
        If SmartLearning Then
            'LinkedSymbol.MAP_1 = MainForm.Form_MAP_1
            'ENTER_THRESHOLD_FROM_MA_PRICE35 = MainForm.Form_MAP_2
            'EXIT_THRESHOLD_FROM_MA_PRICE = MainForm.Form_MAP_3
            'ENTER_POWER = MainForm.Form_ENTER_POWER
            'EXIT_DIV = MainForm.Form_EXIT_DIV
        Else
            'EXIT_RATIO = TestArray(TestIndex)
        End If

        Dim x_data_spec, y_data_spec As c00_DataSpec

        'High가격 CopositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("High가격", DataType.REAL_NUMBER_DATA, Nothing)
        HighPriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "High가격")
        HighPricePointList = New PointList()
        HighPriceCompositeData.SetData(HighPricePointList)

        'Low가격 CopositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("Low가격", DataType.REAL_NUMBER_DATA, Nothing)
        LowPriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "Low가격")
        LowPricePointList = New PointList()
        LowPriceCompositeData.SetData(LowPricePointList)

        'MA_Var CopositeData
        x_data_spec = New c00_DataSpec("minute_index", DataType.REAL_NUMBER_DATA, Nothing)
        y_data_spec = New c00_DataSpec("MA_Var", DataType.REAL_NUMBER_DATA, Nothing)
        MA_VarCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "MA_Var")
        MA_VarPointList = New PointList()
        MA_VarCompositeData.SetData(MA_VarPointList)

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
        GraphicCompositeDataList.Add(HighPriceCompositeData)
        GraphicCompositeDataList.Add(LowPriceCompositeData)
        GraphicCompositeDataList.Add(MA_VarCompositeData)
        GraphicCompositeDataList.Add(MA_PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
        GraphicCompositeDataList.Add(CandleTimeCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)

    End Sub

    Public Overrides Sub CreateGraphicData()
        Dim end_time As TimeSpan = LinkedSymbol.MinuteCandleSeries.Last.CandleTime.TimeOfDay
        Dim a_point As PointF
        Dim min_price As Double = [Double].MaxValue
        Dim max_price As Double = [Double].MinValue

        '그 날 처음부터 걸리고 청산할 때까지 다 만든다.
        For index As Integer = 0 To LinkedSymbol.MinuteCandleSeries.Count - 1
            If LinkedSymbol.MinuteCandleSeries(index).Amount = 0 Then
                '체결가가 없으니 High, Low 가격 모두 Last 가격으로 한다.
                a_point = New PointF(index, LinkedSymbol.MinuteCandleSeries(index).Close)
                HighPricePointList.Add(a_point)
                LowPricePointList.Add(a_point)
            Else
                '체결가가 있으니 기록된 High, Low 가격으로 한다.
                a_point = New PointF(index, LinkedSymbol.MinuteCandleSeries(index).High)
                HighPricePointList.Add(a_point)
                a_point = New PointF(index, LinkedSymbol.MinuteCandleSeries(index).Low)
                LowPricePointList.Add(a_point)
                min_price = Math.Min(min_price, LinkedSymbol.MinuteCandleSeries(index).Low)
                max_price = Math.Max(max_price, LinkedSymbol.MinuteCandleSeries(index).High)
            End If
            a_point = New PointF(index, LinkedSymbol.MinuteCandleSeries(index).Test_MA_Var)
            If a_point.Y <> -1 Then
                MA_VarPointList.Add(a_point)
            End If
            a_point = New PointF(index, MA_PriceInThisContext(LinkedSymbol.MinuteCandleSeries(index)))
            If a_point.Y <> -1 Then
                MA_PricePointList.Add(a_point)
            End If
            a_point = New PointF(index, LinkedSymbol.MinuteCandleSeries(index).CandleTime.TimeOfDay.Hours * 100 + LinkedSymbol.MinuteCandleSeries(index).CandleTime.TimeOfDay.Minutes)
            CandleTimePointList.Add(a_point)
        Next
        DecideTimePointList.Add(New PointF(LinkedSymbol.MinuteCandleSeries.Count - 1 - MinutesPassed, min_price))
        DecideTimePointList.Add(New PointF(LinkedSymbol.MinuteCandleSeries.Count - 1 - MinutesPassed + 0.001, max_price))
        DecideTimePointList.Add(New PointF(LinkedSymbol.MinuteCandleSeries.Count - 1, max_price))
        DecideTimePointList.Add(New PointF(LinkedSymbol.MinuteCandleSeries.Count - 1 + 0.001, min_price))
        '190211: 걸린애 되고 DONE 하고나면 candle list 를 symbol 에서 복사해 오는 코드를 짜야겠다.
    End Sub

    Public Sub CandleArrived(ByVal a_candle As CandleStructure)
        DataArrived(Nothing)
    End Sub

    Public ReadOnly Property MA_PriceInThisContext(ByVal the_candle As CandleStructure) As Single
        Get
            Select Case MABase
                Case MA_Base_Type.MA_BASE_0035
                    Return the_candle.Average35Minutes
                Case MA_Base_Type.MA_BASE_0070
                    Return the_candle.Average70Minutes
                Case MA_Base_Type.MA_BASE_0140
                    Return the_candle.Average140Minutes
                Case MA_Base_Type.MA_BASE_0280
                    Return the_candle.Average280Minutes
                Case MA_Base_Type.MA_BASE_0560
                    Return the_candle.Average560Minutes
                Case MA_Base_Type.MA_BASE_1200
                    Return the_candle.Average1200Minutes
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


    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        '190206: a_data 를 실제로 사용하는 곳이 ClearNow 외에는 없다. 그러므로 Candle Chart 새로 받았을 때 이것을 콜하는 것도(수정은 조금 필요할 듯) 괜찮을 듯 싶다.
        Dim time_over_clearing As Boolean = False

        If LinkedSymbol.MinuteCandleSeries.Count = 0 Then
            'Record data 갯수가 하나인 경우는 아직 MinuteCandleSeries가 안 만들어졌으므로 그냥 나감
            Return
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = LinkedSymbol.MinuteCandleSeries.Last.CandleTime ' StartTime + TimeSpan.FromMinutes(LinkedSymbol.MinuteCandleSeries.Count - 1)
        Dim last_candle As CandleStructure = LinkedSymbol.MinuteCandleSeries.Last
        'Dim lastlast_candle As CandleStructure
        'If LinkedSymbol.MinuteCandleSeries.Count > 1 Then
        'lastlast_candle = LinkedSymbol.MinuteCandleSeries(LinkedSymbol.MinuteCandleSeries.Count - 2)
        'Else
        'lastlast_candle = last_candle
        'End If



#If 0 Then
        If IsClearingTime(current_time) Then
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
                adaptive_threshold = Math.Max(ENTER_THRESHOLD_FROM_MA_PRICE, ENTER_POWER * last_candle.VariationRatio)
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
#If 1 Then
                If fall_volume < VOLUME_LEVEL0 Then
                    adaptive_threshold = Math.Max(ENTER_THRESHOLD_LEVEL0, ENTER_POWER * last_candle.VariationRatio) - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                ElseIf fall_volume < VOLUME_LEVEL1 Then
                    adaptive_threshold = Math.Max(ENTER_THRESHOLD_LEVEL1, ENTER_POWER * last_candle.VariationRatio) - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                ElseIf fall_volume < VOLUME_LEVEL2 Then
                    adaptive_threshold = Math.Max(ENTER_THRESHOLD_LEVEL2, ENTER_POWER * last_candle.VariationRatio) - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                Else
                    adaptive_threshold = Math.Max(ENTER_THRESHOLD_LEVEL3, ENTER_POWER * last_candle.VariationRatio) - LinkedSymbol.Newstab / NEWSTAB_EFFECT
                End If
                If 0 AndAlso SecondStep Then
                    Dim gullin_count As Integer = NumberOfGullin(current_time)
                    adaptive_threshold = adaptive_threshold - 0.01 * Math.Min(gullin_count, 6)
                End If

#Else
                Select Case MABase
                    Case MA_Base_Type.MA_BASE_0035
                        If last_candle.Average35Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor35M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor35M) * MADIFFSCA_DETECT_SCALE_MA0035
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_0070
                        If last_candle.Average70Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor70M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor70M) * MADIFFSCA_DETECT_SCALE_MA0070
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_0140
                        If last_candle.Average140Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor140M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor140M) * MADIFFSCA_DETECT_SCALE_MA0140
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_0280
                        If last_candle.Average280Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor280M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor280M) * MADIFFSCA_DETECT_SCALE_MA0280
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_0560
                        If last_candle.Average560Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor560M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor560M) * MADIFFSCA_DETECT_SCALE_MA0560
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_1200
                        If last_candle.Average1200Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor1200M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor1200M) * MADIFFSCA_DETECT_SCALE_MA1200
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_2400
                        If last_candle.Average2400Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor2400M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor2400M) * MADIFFSCA_DETECT_SCALE_MA2400
                        Else
                            adaptive_threshold = 1
                        End If
                    Case MA_Base_Type.MA_BASE_4800
                        If last_candle.Average4800Minutes <> -1 AndAlso LinkedSymbol.MadiffscaFor4800M <> 0 Then
                            adaptive_threshold = Madiffsca2Threshold(LinkedSymbol.MadiffscaFor4800M) * MADIFFSCA_DETECT_SCALE_MA4800
                        Else
                            adaptive_threshold = 1
                        End If
                End Select
#End If

                If last_candle.Amount > 0 AndAlso MA_price - last_candle.Close > adaptive_threshold * MA_price AndAlso fall_volume > FALL_VOLUME_LOWESHOLD AndAlso ((Not OnlyAllowHighNewstab) OrElse LinkedSymbol.Newstab > 0.04) Then
#If 0 Then
                    If fall_volume < VOLUME_LEVEL0 Then
                        EXIT_NEW_RATIO = 0
                    ElseIf fall_volume < VOLUME_LEVEL1 Then
                        EXIT_NEW_RATIO = 0.5
                    ElseIf fall_volume < VOLUME_LEVEL2 Then
                        EXIT_NEW_RATIO = 0.75
                    Else
                        EXIT_NEW_RATIO = 0.9
                    End If
#End If
                    '20220710: RelativeFall 에 따른 EXIT_NEW_RATIO의 결정
                    Dim fall_index = Math.Log(RelativeFall * 100000, 2)
                    Dim amp = 0.22
                    Dim lag = 0.3
                    Dim yshift = -0.3
                    Dim xshift = 0

                    EXIT_NEW_RATIO = amp * (2 / (1 + Math.Exp(-(fall_index - xshift) * lag)) + yshift)
                    EXIT_NEW_RATIO = Math.Min(EXIT_NEW_RATIO, 1)

                    enter_gulin = True
                    If RelativeFall >= CUT_RELATIVE_FALL Then
                        '2022.07.24: 걸렸지만 자르기로 결정한 경우임. 아래 플래그를 셋해서 EXIT 했을 때 계산에 포함되지 않게 해야 한다.
                        'FakeGullin = True
                    End If

                    If 0 AndAlso SecondStep Then
                        Dim gullin_count As Integer = NumberOfGullin(current_time)
                        If gullin_count <= 1 Then
                            enter_gulin = False
                        End If
                    End If
                Else
                    enter_gulin = False
                End If
            Else    'Decision type = 3
                Dim relative_fall As Double = LinkedSymbol.MA_Amount(HOW_MANY_MINUTES_FOR_VOLUME_DECISION) / LinkedSymbol.DailyAmountStore.Last
                Select Case MABase
                    Case MA_Base_Type.MA_BASE_0035

                    Case MA_Base_Type.MA_BASE_0070
                    Case MA_Base_Type.MA_BASE_0140
                End Select
            End If
            If enter_gulin Then
                '20200510: 투자유의 종목은 진입 안 되게 막았다.
                'If last_candle.Amount > 0 AndAlso lastlast_candle.Test_MA_Price - lastlast_candle.Close > adaptive_threshold * lastlast_candle.Test_MA_Price AndAlso lastlast_candle.Test_MA_Price - lastlast_candle.Close > last_candle.Test_MA_Price - last_candle.Close Then
                '이러면 걸린애 된다.
                If LinkedSymbol.MADecisionFlag Then
                    '20200913: 이거는 이미 고레벨에서 진입했다는 거다. 저레벨에서는 진입이 금지되고, 기다리는 상태가 된다.
                    YieldForHighLevel = True
                Else
                    '20200914: 이것보다 아래 레벨에서 진입하지 못하게 플래그를 셋해둔다.
                    LinkedSymbol.MADecisionFlag = True
                End If
                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                MinutesPassed = 0
                'UsedMABase = ma_base_in_use
                UsedThreshold = adaptive_threshold
                EnterTime = current_time
                _BasePrice = last_candle.Close
                _FallMinutePassed = 0
                _RiseMinutePassed = 0
                'EnterPoint = LinkedSymbol.MinuteCandleSeries.Count - 1
                EnterPrice = last_candle.Close '(1 - ENTER_THRESHOLD_FROM_MA_PRICE) * last_candle.Test_MA_Price
                FallVolume = LinkedSymbol.MA_Amount(HOW_MANY_MINUTES_FOR_VOLUME_DECISION) * EnterPrice      'MovingAverageDifference 전략에서 FallVolume은 분당평균을 의미함
                '#If Not ALLOW_MULTIPLE_ENTERING Then
                'FallVolumeLimited = FallVolume * (1 - Math.Exp(-VOLUME_LIMIT / FallVolume * 2)) / (1 + Math.Exp(-VOLUME_LIMIT / FallVolume * 2))
                '#End If

                'AdaptiveExitMul = 1.5 / Math.Exp(0.03 * LinkedSymbol.AmountVar) - 1
                EnterMAFallRate = (2 * MA_PriceInThisContext(LinkedSymbol.MinuteCandleSeries(LinkedSymbol.MinuteCandleSeries.Count - 1)) - MA_PriceInThisContext(LinkedSymbol.MinuteCandleSeries(LinkedSymbol.MinuteCandleSeries.Count - 3)) - MA_PriceInThisContext(LinkedSymbol.MinuteCandleSeries(LinkedSymbol.MinuteCandleSeries.Count - 2))) / 2 / MA_PriceInThisContext(LinkedSymbol.MinuteCandleSeries(LinkedSymbol.MinuteCandleSeries.Count - 1))
                EnterMAPrice = MA_price
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            Dim gullin_count As Integer
            '청산대기
            MinutesPassed += 1
            'Dim MA_price As Single
            'MA_price = LinkedSymbol.GetAverage4800Minutes(0)
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
            'If LinkedSymbol.MinuteCandleSeries.Last.CandleTime.Date <> LinkedSymbol.MinuteCandleSeries(LinkedSymbol.MinuteCandleSeries.Count - 1).CandleTime.Date Then
            '날짜가 바뀐 것이여
            'DaysPassed += 1
            'End If
            If AllowMultipleEntering Then
                'If last_candle.Amount > 0 AndAlso last_candle.Test_MA_Price - last_candle.Close > (NumberOfEntering + 1) * ENTER_THRESHOLD_FROM_MA_PRICE * last_candle.Test_MA_Price Then
                gullin_count = NumberOfGullin(current_time)
                If last_candle.Amount > 0 AndAlso NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last - last_candle.Close > ENTER_THRESHOLD_FROM_MA_PRICE * TH_ATTENUATION * MA_PriceInThisContext(last_candle) Then
                    '추가매수해
                    _EnterPriceMulti.Add(last_candle.Close)
                    _FallVolumeMulti.Add(_FallVolumeMulti.Last * VOLUME_ATTENUATION)
                    'FallVolume = (NumberOfEntering + 1) * FallVolume / NumberOfEntering
                    'NumberOfEntering += 1
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
                If 0 AndAlso SecondStep Then
                    'Dim gullin_count As Integer = NumberOfGullin(current_time)
                    If last_candle.Close - MA_price > (gullin_count * 0.05 - EXIT_NEW_RATIO) * UsedThreshold * MA_price + (EnterMAPrice - MA_price) * SPIKE_FACTOR Then
                        exit_gulin = True
                    Else
                        exit_gulin = False
                    End If
                Else
                    If last_candle.Close - MA_price > -UsedThreshold * EXIT_NEW_RATIO * MA_price + (EnterMAPrice - MA_price) * SPIKE_FACTOR Then
                        exit_gulin = True
                    Else
                        exit_gulin = False
                    End If
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
        End If
    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub

    Public Function Madiffsca2Threshold(ByVal madiffsca As Double) As Double

        Dim return_value As Double
        return_value = MADIFFSCA_A * Math.Exp(-MADIFFSCA_B * madiffsca) + madiffsca
        Return return_value
#If 0 Then
        If madiffsca < 0.03 Then
            Return 0.03
        ElseIf madiffsca < 0.04 Then
            Return 0.04
        Else
            Return madiffsca
        End If
#End If
    End Function
End Class

Public Class c05H_SharpLines
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure SharpLinesStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
    End Structure

    Public ENTER_RATIO As Double = 1.1       'length1 이 length3의 이 배율이 되면 진입함
    Public EXIT_RATIO As Double = 0.7         'length1 이 length3의 이 배율이 되면 청산함

    Private PricePointList, SharpLinesPointList, DecideTimePointList As PointList
    Private PriceCompositeData, SharpLinesCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of SharpLinesStructure)
    Public LineDetector As New c07_SharpLines
    Private _DetectedLines As c07a_LineDetected
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Public _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    Private _ReadyForExit As Boolean = False

    Public DEFAULT_HAVING_TIME As Integer = 12
    Public TOP_STAY As Integer = 2
    Public VERTICAL_SCALE As Double = 80    '100이면 1%가 길이 1, 50이면 2%가 길이 1
    Public HOT_HEIGHT As Double = 2.5        '걸린애를 만들기 위한 최소 HEIGHT
    Public HOT_WIDTH As Double = 24           '걸린애를 만들기 위한 최대 WIDTH
    Public STANDARD_LENGTH As Double = 8
    Public X_LIMIT As Double = 60            '선분의 길이가 이거를 넘어가면 SharpLines 검출은 종료된다.
    Public SLOPE1_LIMIT As Double = -0.8
    Public SLOPE3_LIMIT As Double = -1

    Public BEGINNING_MARGIN As Integer = 7

    Public SCORE_THRESHOLD As Double = 0
    Public SCORE_LOWER_THRESHOLD As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer)
        MyBase.New(linked_symbol, start_time)

        FALL_VOLUME_THRESHOLD = 400000000000

        'FALLING 이라는 용어를 썼지만 진입의 의미로 받아들이고 그냥 쓰자
        _CurrentPhase = SearchPhase.WAIT_FALLING

#If SHARP_LINES Then
        If SmartLearning Then
            VERTICAL_SCALE = MainForm.Form_VERTICAL_SCALE
            HOT_HEIGHT = MainForm.Form_HOT_HEIGHT
            HOT_WIDTH = MainForm.Form_HOT_WIDTH
            STANDARD_LENGTH = MainForm.Form_STANDARD_LENGTH
            X_LIMIT = MainForm.Form_X_LIMIT
            SLOPE1_LIMIT = MainForm.Form_SLOPE1_LIMIT
            SLOPE3_LIMIT = MainForm.Form_SLOPE3_LIMIT
        Else
            VERTICAL_SCALE = MainForm.Form_VERTICAL_SCALE
            HOT_HEIGHT = MainForm.Form_HOT_HEIGHT
            HOT_WIDTH = MainForm.Form_HOT_WIDTH
            STANDARD_LENGTH = MainForm.Form_STANDARD_LENGTH
            X_LIMIT = MainForm.Form_X_LIMIT
            SLOPE1_LIMIT = MainForm.Form_SLOPE1_LIMIT
            SLOPE3_LIMIT = MainForm.Form_SLOPE3_LIMIT
        End If
#End If

        LineDetector.VERTICAL_SCALE = VERTICAL_SCALE    '100이면 1%가 길이 1, 50이면 2%가 길이 1
        LineDetector.HOT_HEIGHT = HOT_HEIGHT        '걸린애를 만들기 위한 최소 HEIGHT
        LineDetector.HOT_WIDTH = HOT_WIDTH           '걸린애를 만들기 위한 최대 WIDTH
        LineDetector.STANDARD_LENGTH = STANDARD_LENGTH
        LineDetector.X_LIMIT = X_LIMIT            '선분의 길이가 이거를 넘어가면 SharpLines 검출은 종료된다.


        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        'SharpLines CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("SharpLines", DataType.REAL_NUMBER_DATA, Nothing)
        SharpLinesCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "SharpLines")
        SharpLinesPointList = New PointList()
        SharpLinesCompositeData.SetData(SharpLinesPointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(SharpLinesCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)

        LineDetector.linked_symbol_debug = LinkedSymbol
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령.
        If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub

    Public Overrides Sub CreateGraphicData()
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        'SharpLines 를 그리자
        For index As Integer = 0 To _DetectedLines.Points.Count - 1
            a_point = New PointF(_DetectedLines.StartTime.TimeOfDay.TotalSeconds + 5 * _DetectedLines.Points(index).X, _DetectedLines.Points(index).Y)
            SharpLinesPointList.Add(a_point)
        Next

#If 1 Then
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (FlexPatternList(MatchedFlexIndex).Count - 1) * 5, 0))                '진입시간
        'DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - (FlexPatternList(MatchedFlexIndex).Count - 1) * 5 + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 0))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 0))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 1))                 '청산시간
#End If
    End Sub

    Private Function GetScore(ByVal target_array() As UInt32) As Double
        'Score 계산
        Dim score As Double = 0

        Return score
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim sharplines_str As SharpLinesStructure
        Dim time_over_clearing As Boolean = False

        sharplines_str.Price = a_data.CoreRecord.Price         '주가 저장
        sharplines_str.Amount = a_data.CoreRecord.Amount       '거래량 저장

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.CoreRecord.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(sharplines_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING AndAlso RecordList.Count > HOT_WIDTH + BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1

        'line detector 의 input 으로 집어 넣기
        Dim lines_obj = LineDetector.InputNewPoint(RecordList.Last.Price, StartTime + TimeSpan.FromSeconds(5 * (RecordList.Count - 1)))

        If _CurrentPhase = SearchPhase.WAIT_FALLING Then
            '선분 검출하기 위한 모드

            '진입에러 확인 절차
            If _DetectedLines IsNot Nothing Then
                MsgBox("선분 검출 전인데 선분객체가 있다!", MsgBoxStyle.Critical)
                Return
            End If

            If lines_obj IsNot Nothing Then ' AndAlso lines_obj.Points.Count >= 3 Then
                '선분 한 개 검출되었다.
                _DetectedLines = lines_obj
                _CurrentPhase = SearchPhase.PRETHRESHOLDED  '선분 두 개가 검출되어야 걸린애 된다. 한개는 pre-threshold 상태로 처리한다. 한 개 검출되어도 RecordList 점갯수 컨트롤 하려면 상태 특정이 필요하다.
            Else
                '선분이 검출되지 않았다. 다음번 input을 기다리면 된다.
            End If
        ElseIf _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '선분 한 개 검출되었고, 적어도 선분 세 개 이상 그리고 V 가 TRUE 일 때를 기다린다. 만약 선분 한 개가 계속 길어져 X_LIMIT 초과하면 다시 WAIT_FALL 상태로 돌아간다.
            If LinkedSymbol.Code = "A000105" Then
                Dim a = 1
            End If
            If lines_obj IsNot Nothing Then
                '점이 추가되었거나 선분 complete 되었다.
                If lines_obj.Complete Then
                    '최종 걸린애 되기 전에 complete 되었다. => 버리고 WAIT_FALL 상태로 돌아간다.
                    _DetectedLines = Nothing
                    LineDetector = New c07_SharpLines
                    _CurrentPhase = SearchPhase.WAIT_FALLING
                Else
                    'Complete 은 되지 않았는데 진입조건이 되었는지 확인한다.
                    'If lines_obj.V AndAlso lines_obj.Points.Count >= 4 AndAlso lines_obj.Length1 > lines_obj.Length3 * ENTER_RATIO AndAlso lines_obj.Length2 > STANDARD_LENGTH * 1.2 AndAlso lines_obj.Length3 > STANDARD_LENGTH * 1.2 Then
                    If lines_obj.V AndAlso lines_obj.Points.Count >= 4 AndAlso lines_obj.Slope1 < SLOPE1_LIMIT AndAlso lines_obj.Slope3 < SLOPE3_LIMIT Then 'AndAlso lines_obj.Slope3 < -1 Then
                        '진입이다.
                        'fall volume 계산
                        _DetectedLines = lines_obj
                        Dim x_length = _DetectedLines.Points.Last.X - _DetectedLines.Points(_DetectedLines.Points.Count - 4).X
                        Dim fall_volume As UInt64 = (RecordList.Last.Amount - RecordList(RecordList.Count - 1 - x_length).Amount) / x_length * RecordList.Last.Price   '검출 볼륨 업데이트
                        'If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (RecordList.Last.Price = RecordList(RecordList.Count - 2).Price AndAlso RecordList.Last.Amount = RecordList(RecordList.Count - 2).Amount) Then
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)             '진입시간
                        EnterPrice = RecordList.Last.Price                  '진입가
                        _BasePrice = RecordList.Last.Price             '바닥가 기록
                        _TopPrice = RecordList.Last.Price             '천정가 기록
                        FallVolume = fall_volume           '하락 볼륨 업데이트
                        'Else
                        'FallVolume이 너무 크면 오히려 마이너스가 된다.
                        '또는 2분간 단일가매매 상황이다.
                        'End If
                    End If
                End If
            Else
                '선분 두 개가 아직 안 되었다. 다음 input을 기다리면 된다.
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            '선분 계속 모드 (청산 기다리기 모드)
            'If RecordList.Last.Price = RecordList(RecordList.Count - 2).Price AndAlso RecordList.Last.Amount = RecordList(RecordList.Count - 2).Amount AndAlso _WaitExitCount < 2 Then
            '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거는 이제 치지 말자
            '_WaitExitCount = 0
            '_CurrentPhase = SearchPhase.WAIT_FALLING
            'Else

            '진입에러 확인 절차
            If _DetectedLines Is Nothing Then
                MsgBox("선분 검출을 했는데도 불구하고 선분객체가 없다!", MsgBoxStyle.Critical)
                Return
            End If

            _WaitExitCount += 1         '청산기다림 카운트
            'Base 업데이트는 여기서
            If _BasePrice >= RecordList.Last.Price Then
                _BasePrice = RecordList.Last.Price
                _CountFromLastBase = 0
            Else
                _CountFromLastBase += 1
            End If


            If (Not _ReadyForExit) Then
                '아직 꺾이기 전
                If (Not _DetectedLines.V) Then
                    '꺾였다.
                    _ReadyForExit = True
                ElseIf _DetectedLines.Complete Then
                    '선분은 꺾이지 않고 V 상태로 종료되었다.
                    '선분 complete 시 decision maker 종료 조건은 저점 찍은 후 DEFAULT_HAVING_TIME 이 넘어가는 때로 하자
                    If _CountFromLastBase >= DEFAULT_HAVING_TIME Then
                        _CurrentPhase = SearchPhase.DONE
                        ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                        ExitPrice = RecordList.Last.Price                   '청산가
                        Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                        TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                        _Done = True                             '청산완료 알리는 비트 셋
                    End If
#If 0 Then
                    SafeEnterTrace(PartKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    PartCount(MatchedFallingStartIndex) += 1
                    PartMoney(MatchedFallingStartIndex) += Profit
                    SafeLeaveTrace(PartKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
#End If
                End If
            Else
                '꺾인 후
                'exit 조건은 TOP 에 어느정도 머물렀을 만큼이다.
                'Top 업데이트는 여기서
                If _TopPrice <= RecordList.Last.Price Then
                    _TopPrice = RecordList.Last.Price
                    _CountFromLastTop = 0
                Else
                    _CountFromLastTop += 1
                End If
                If _CountFromLastTop >= TOP_STAY Then
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                    ExitPrice = RecordList.Last.Price                   '청산가
                    Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                End If
            End If
        Else
            '장 끝나갈 때 자동청산으로 DONE 이 되어 이곳으로 올 수 있다.
        End If
    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class


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
    Public DEFAULT_HAVING_TIME As Integer = 13 'HAVING_LENGTH
    'Public DEFAULT_HAVING_TIME2 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME3 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME4 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME5 As Integer = DEFAULT_HAVING_TIME
    'Public DEFAULT_HAVING_TIME6 As Integer = DEFAULT_HAVING_TIME

    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_THRESHOLD1 As Double = 4.2
    Public FALL_SCALE_LOWER_THRESHOLD1 As Double = 1.1
    Public SCORE_THRESHOLD2 As Double = 4.2
    Public FALL_SCALE_LOWER_THRESHOLD2 As Double = 1.1
    Public GL1 As Double = 1.1845
    Public GL2 As Double = 1.111
    Public GL3 As Double = 1
    Public GL4 As Double = 0.873
    Public GL5 As Double = 0.784

    Public SCORE_LOWER_THRESHOLD As Double = 0
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1.4
    Public FLEXIBILITY1 As Double = 0.3
    Public FLEXIBILITY2 As Double = 0.3
    Public MAX_SECONDFALL_WAITING As Integer = 30
    Public PatternName As String = "DoubleFall"

    Public PRETHRESHOLD_RATIO As Double = 0.4
    Public CANDIDATE_STEPPING As Double = 0.004
    Public DebugPrethresholdYes As Boolean = False
    Public DebugItWorked As Boolean = False
    Public MyPriceWindowList As PriceWindowList

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer)
        MyBase.New(linked_symbol, start_time)

        FALL_VOLUME_THRESHOLD = 400000000000
        Select Case index_decision_center
            Case 0
                SCORE_THRESHOLD1 = 0.0557
                FALL_SCALE_LOWER_THRESHOLD1 = 1.0375
                SCORE_THRESHOLD2 = 0.07
                FALL_SCALE_LOWER_THRESHOLD2 = 1.045
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

#If DOUBLE_FALL Then
        If SmartLearning Then
            SCORE_THRESHOLD1 = MainForm.Form_SCORE_THRESHOLD1
            FALL_SCALE_LOWER_THRESHOLD1 = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD1
            SCORE_THRESHOLD2 = MainForm.Form_SCORE_THRESHOLD2
            FALL_SCALE_LOWER_THRESHOLD2 = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD2
            MAX_SECONDFALL_WAITING = MainForm.Form_MAX_SECONDFALL_WAITING
            DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
        Else
            SCORE_THRESHOLD1 = MainForm.Form_SCORE_THRESHOLD1
            FALL_SCALE_LOWER_THRESHOLD1 = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD1
            SCORE_THRESHOLD2 = MainForm.Form_SCORE_THRESHOLD2
            FALL_SCALE_LOWER_THRESHOLD2 = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD2
            MAX_SECONDFALL_WAITING = MainForm.Form_MAX_SECONDFALL_WAITING
            DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
        End If
#End If

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

                    For candidate_index As Integer = last_price_candidate.Count - 1 To 0 Step -1
                        target_list.Add(last_price_candidate(candidate_index))  '마지막에 후보를 붙인다.
                        score_for_candidate.Add(GetScore2WithNormalize(target_list.ToArray()))    '스코어를 계산한다.
                        target_list.RemoveAt(target_list.Count - 1)             '마지막에 붙인 후보를 다시 지운다.

                        If score_for_candidate.Last < SCORE_THRESHOLD2 Then
                            result_list.Add(last_price_candidate(candidate_index))
                            DebugPrethresholdYes = True
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
            '2024.01.18 : 여러개가 걸렸으면 그 중에 가격이 제일 높은 것을 골라 보낸다. 낚시 성공률을 높이기 위함이다.
            Return result_list.Max
        End If
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

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
                _CurrentPhase = SearchPhase.WAIT_SECONDFALL
            End If

        ElseIf _CurrentPhase = SearchPhase.WAIT_SECONDFALL OrElse _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '두번째 하락 기다리기 모드

            Dim relative_price As Double
            If LinkedSymbol.OpenPrice = 0 Then
                relative_price = 1
            Else
                relative_price = a_data.CoreRecord.Price / LinkedSymbol.OpenPrice
            End If
            Dim matching As Boolean = CheckMatching2()

            If matching = True AndAlso Not IsClearingTime(current_time) Then 'AndAlso Not IsFirstHalfTime(current_time) Then
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
                If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount) Then
                    If matching Then
                        DebugTotalGullin += 1
                        If DebugPrethresholdYes Then
                            '오 맞췄어!
                            DebugItWorked = True
                            DebugPrethresholdGullin += 1
                        Else
                            '못 맟췄군
                            DebugItWorked = False
                        End If
                        'DebugPrethresholdYes = False
                    Else
                        'DebugPrethresholdYes = False
                    End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
#If DEBUG_TOP_BOTTOM_PRICE_UPDATE Then
#Else
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
#End If
                    FallVolume = fall_volume           '하락 볼륨 업데이트
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
            Else
                SecondFallWaitingCount += 1
                If SecondFallWaitingCount >= MAX_SECONDFALL_WAITING Then
                    '많이 기다렸는데도 second pattern이 안 나타나면 다시 원점으로 돌아간다.
                    SecondFallWaitingCount = 0
                    _CurrentPhase = SearchPhase.WAIT_FALLING
                End If
            End If
            '2024.1.1 : check prethreshold 디버깅용 코드 ========================================
            DebugPrethresholdYes = False
            'Dim b = CheckPrethreshold()
            '======================================================================================
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
#If Not DOUBLE_GULLIN_BUG_FIX Then
            If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount AndAlso _WaitExitCount < 2 Then
                '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거는 이제 치지 말자
                _WaitExitCount = 0
                _CurrentPhase = SearchPhase.WAIT_FALLING
            Else
#End If
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
                End If
#End If
            End If
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매상태로 인해 매도 못한다. 기다려야 된다.
                Else
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                    ExitPrice = a_data.CoreRecord.Price                   '청산가
                    'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    ProfitCalculation()
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                    SafeEnterTrace(PartKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    PartCount(MatchedFlexIndex2) += 1
                    PartMoney(MatchedFlexIndex2) += Profit
                    SafeLeaveTrace(PartKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
            End If
#If Not DOUBLE_GULLIN_BUG_FIX Then
            End If
#End If
        ElseIf _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED Then
            End If

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

'20211113: FlexiblePCRenew type은 전에 롤러코스터 전략 fix했던 전략이 시간이 지남에 따라 수익률이 떨어져 분석해보니 fix하는 것이 좋은 방법이 아니고, 매달마다 지난 2개월간 데이터를
'20211113: 학습을 통해 얻어진 전략으로 업데이트하는 것이 좋겠다는 판단에 따라, 이전 롤러코스터 fix 전략은 버리고, 이전 롤러코스터 fix 전략 때 썼던 pattern length 에 따라 달리 가져간
'20211113: score threshold 를 과감히 없애버리고 (세분화하면 학습이 오래걸리고 어려워진다) 다시 예전처럼 간단한 전략으로 돌아간 것이다.
Public Class c05F_FlexiblePCRenew
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        'Dim Gangdo As Double
        'Dim BuyAmount As UInt64
        'Dim SelAmount As UInt64
        'Dim DeltaGangdo As Single
    End Structure

    Private PricePointList, DecideTimePointList As PointList ', DeltaGangdoPointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData As c011_PlainCompositeData ', DeltaGangdoCompositeData 
    Private RecordList As New List(Of PatternCheckStructure)
    'Private BackupPricelist As New List(Of UInt32)
    Public RecordCount As Integer
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
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.4896}  'Golden
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}  'Golden 꼬리 올리기
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadraple silver
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -32, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -12, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Mild Sangbong
    'Public Pattern() As Double = {23.99197616, 32.55771001, 33.79806153, 24, 10, -32, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Tailered Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Tough Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -9.367132189, -5, -6, -15.47813968, -31.32709401, -40}            'Brother Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 7, -38, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}     'Brutal Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -5, 4, 3, -15.47813968, -31.32709401, -40}       'couple
    'Public Pattern() As Double = {26, 30, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -5, 4, 3, -15.47813968, -31.32709401, -40}         'husbig
    'Public Pattern() As Double = {14, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Clear Sangbong
    'Public Pattern() As Double = {14, 23.99197616, 32.55771001, 33.79806153, 24, 10, -8, -35, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}       'Far Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -40, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Toughest Sangbong
    'Public Pattern() As Double = {18, 21, 24.5, 28.3, 32.4, 33.5, 33.79806153, 29.4, 25, 18.8, 10, -7, -35, -31, -26.77410577, -21, -15.10251023, -11.5, -8.6, -5, -2.240000465, -1, -2.000099933, -8, -15.47813968, -23, -31.32709401, -36.5, -40}    'Double Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -12, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -43, -46, -49, -52, -55, -57, -58}  'Water Slide
    'Public Pattern() As Double = {18, 24.60381429, 32.73490309, 31.69847691, 20, -6.071428571, -31.47461676, -20.938308, -11.82515135, -4.785404652, -2.068642942, -12.58998831, -29.06295768, -39.38050671}    'Tough Sangbong-1
    'Public Pattern() As Double = {18, 23.56397787, 31.33403375, 33.53227192, 26.79944615, 15, -15.71428571, -30.88705288, -21.7719934, -13.05416093, -7.330808839, -2.188593208, -3.925534183, -16.61020785, -31.32709401, -39.38050671} 'Tough Sangbong+1
    'Public Pattern() As Double = {19.5, 26.99197616, 37.05771001, 39.79806153, 31.5, 19, -24.5, -14.77410577, -1.602510233, 5.632867811, 14.25999954, 15.99990007, 4.021860317, -10.32709401, -17.5}    'Rising Tough Sangbong
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.002707806, -0.265102023, -0.426021484, -0.520208505, -0.572599588, -0.600375}       'TwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.002707806, -0.265102023, -0.426021484, -0.520208505, -0.572599588, -0.600375, -0.614767219, -0.622628258, -0.627755859, -0.631978567, -0.635999665, -0.64}         'CompleteTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, -0.265102023, -0.426021484, -0.520208505, -0.572599588, -0.600375}       'ShortTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.002030854, -0.198826517, -0.319516113, -0.390156379, -0.429449691, -0.45028125}     'ScaleDownTwoStair
    Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}     'ScaleUpTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.004061708, -0.397653035, -0.639032227, -0.780312757, -0.858899382, -0.9005625}      'Scale1.5TwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, 0.002004789, -0.307620388, -0.497505352, -0.608646036, -0.670467514, -0.7032425}          'Scale1.18TwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, 0, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}       'ScaleUpLongTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 5.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, 0, 0, 0, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}       'ScaleUpLongerTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.804061708, -1.197653035, -1.439032227, -1.580312757, -1.658899382}    'ScaleUpThreeStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair

    'Public Pattern() As Double = {18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1}
    Public NormalizedX() As Double
    Public MinFlexLength As Integer
    Public MaxFlexLength As Integer
    Public FlexPatternList As New List(Of Double())
    Public FlexNormalizedXList As New List(Of Double())
    Public MatchedFlexIndex As Integer
    Public MatchedFallingStartIndex As Integer
#If UNWANTED_RISING_DETECTION Then
    Public TIME_DIFF_FOR_RISING_DETECTION As UInt32 = 50
    Public RISING_SLOPE_THRESHOLD As Double = 0.15
    Public ENTERING_PROHIBIT_TIME As UInt32 = 60
    Public TimeCountAfterRising As Integer = 0
#End If

    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    Public DEFAULT_HAVING_TIME1 As Integer = DEFAULT_HAVING_TIME
    Public DEFAULT_HAVING_TIME2 As Integer = DEFAULT_HAVING_TIME
    Public DEFAULT_HAVING_TIME3 As Integer = DEFAULT_HAVING_TIME
    Public DEFAULT_HAVING_TIME4 As Integer = 16 'DEFAULT_HAVING_TIME
    Public DEFAULT_HAVING_TIME5 As Integer = 25 'DEFAULT_HAVING_TIME
    Public DEFAULT_HAVING_TIME6 As Integer = 22 'DEFAULT_HAVING_TIME

    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7 '+ 100
    Public SCORE_THRESHOLD As Double = 4.2 '3.85
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 1.1
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
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 5
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
    Public MyPriceWindowList As PriceWindowList

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer)
        MyBase.New(linked_symbol, start_time)

        'SavedIndexDecisionCenter = index_decision_center
        FALL_VOLUME_THRESHOLD = 400000000000
        Select Case index_decision_center
            Case 0
                PatternName = "RollerCoaster"
                Pattern = {2, 15, 25, 30.5, 34, 35, 33.8, 30, 22, 13, 0, -19, -43}
                DEFAULT_HAVING_TIME = 13
                SCORE_THRESHOLD = 0.0557
                FALL_SCALE_LOWER_THRESHOLD = 1.0375
                'POST_FALL_SCALE_LOWER_THRESHOLD = 1.042
#If 0 Then
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
            Case Else
                MsgBox("예상치 못한 index_decision_center")
        End Select


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

#If PCRENEW_LEARN Then
        If SmartLearning Then
            SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            DEFAULT_HAVING_TIME1 = MainForm.Form_DEFAULT_HAVING_TIME1
            DEFAULT_HAVING_TIME2 = MainForm.Form_DEFAULT_HAVING_TIME2
            DEFAULT_HAVING_TIME3 = MainForm.Form_DEFAULT_HAVING_TIME3
            DEFAULT_HAVING_TIME4 = MainForm.Form_DEFAULT_HAVING_TIME4
            DEFAULT_HAVING_TIME5 = MainForm.Form_DEFAULT_HAVING_TIME5
            DEFAULT_HAVING_TIME6 = MainForm.Form_DEFAULT_HAVING_TIME6
            TH_ATTENUATION = MainForm.Form_TH_ATTENUATION
            VOLUME_ATTENUATION = MainForm.Form_VOLUME_ATTENUATION
#If UNWANTED_RISING_DETECTION Then
            TIME_DIFF_FOR_RISING_DETECTION = MainForm.Form_TIME_DIFF_FOR_RISING_DETECTION
            RISING_SLOPE_THRESHOLD = MainForm.Form_RISING_SLOPE_THRESHOLD
            ENTERING_PROHIBIT_TIME = MainForm.Form_ENTERING_PROHIBIT_TIME
#End If
        Else
            SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            DEFAULT_HAVING_TIME1 = MainForm.Form_DEFAULT_HAVING_TIME1
            DEFAULT_HAVING_TIME2 = MainForm.Form_DEFAULT_HAVING_TIME2
            DEFAULT_HAVING_TIME3 = MainForm.Form_DEFAULT_HAVING_TIME3
            DEFAULT_HAVING_TIME4 = MainForm.Form_DEFAULT_HAVING_TIME4
            DEFAULT_HAVING_TIME5 = MainForm.Form_DEFAULT_HAVING_TIME5
            DEFAULT_HAVING_TIME6 = MainForm.Form_DEFAULT_HAVING_TIME6
            TH_ATTENUATION = MainForm.Form_TH_ATTENUATION
            VOLUME_ATTENUATION = MainForm.Form_VOLUME_ATTENUATION

            'TH_ATTENUATION = TestArray(TestIndex)
            'DEFAULT_HAVING_TIME1 = TestArray(TestIndex)
            'DEFAULT_HAVING_TIME2 = TestArray(TestIndex)
            'DEFAULT_HAVING_TIME3 = TestArray(TestIndex)
            'DEFAULT_HAVING_TIME4 = TestArray(TestIndex)
            'DEFAULT_HAVING_TIME5 = TestArray(TestIndex)
            'DEFAULT_HAVING_TIME6 = TestArray(TestIndex)

#If UNWANTED_RISING_DETECTION Then
            TIME_DIFF_FOR_RISING_DETECTION = MainForm.Form_TIME_DIFF_FOR_RISING_DETECTION
            RISING_SLOPE_THRESHOLD = MainForm.Form_RISING_SLOPE_THRESHOLD
            ENTERING_PROHIBIT_TIME = MainForm.Form_ENTERING_PROHIBIT_TIME
#End If
        End If
#End If

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

        'DeltaGangdo CompositeData
        'x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("DeltaGangdo", DataType.REAL_NUMBER_DATA, Nothing)
        'DeltaGangdoCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DeltaGangdo")
        'DeltaGangdoPointList = New PointList()
        'DeltaGangdoCompositeData.SetData(DeltaGangdoPointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
        'GraphicCompositeDataList.Add(DeltaGangdoCompositeData)
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
                    _TargetHeight = target_max - target_min           'target height 업데이트
                    _TargetBasePrice = target_min
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

                    For candidate_index As Integer = last_price_candidate.Count - 1 To 0 Step -1
                        target_list.Add(last_price_candidate(candidate_index))  '마지막에 후보를 붙인다.
                        score_for_candidate.Add(GetScoreWithNormalize(target_list.ToArray()))    '스코어를 계산한다.
                        target_list.RemoveAt(target_list.Count - 1)             '마지막에 붙인 후보를 다시 지운다.

                        If score_for_candidate.Last < SCORE_THRESHOLD Then
                            result_list.Add(last_price_candidate(candidate_index))
                            'DebugPrethresholdCount_T += 1
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

        patterncheck_str.Price = a_data.CoreRecord.Price         '주가 저장
        patterncheck_str.Amount = a_data.CoreRecord.Amount         '거래량 저장
        'patterncheck_str.Gangdo = a_data.Gangdo
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
#If UNWANTED_RISING_DETECTION Then
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Math.Max(Pattern.Count, TIME_DIFF_FOR_RISING_DETECTION) + BEGINNING_MARGIN
#Else
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + BEGINNING_MARGIN
#End If
            'BackupPricelist.Add(RecordList(0).Price)
            'If BackupPricelist.Count > 4 * 12 - Pattern.Count - BEGINNING_MARGIN Then
            'BackupPricelist.RemoveAt(0)
            'End If
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.
        '181006: EnterTime은 패턴의 시작점이 아니고 끝점이 맞다.

        '2024.04.26: RecordList 에 새 데이터가 들어옴에 따라 price window list 를 업데이트한다.
        MyPriceWindowList.Insert(patterncheck_str.Price)

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드

            'unwanted rising detect
#If UNWANTED_RISING_DETECTION Then
            If RecordList.Count > TIME_DIFF_FOR_RISING_DETECTION + 1 Then
                Dim rising_slope As Double = (CType(a_data.CoreRecord.Price, Double) - RecordList(RecordList.Count - 1 - TIME_DIFF_FOR_RISING_DETECTION).Price) / RecordList(RecordList.Count - 1 - TIME_DIFF_FOR_RISING_DETECTION).Price
                If rising_slope > RISING_SLOPE_THRESHOLD Then
                    _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED
                    Exit Sub
                End If
            End If
#End If
            Dim relative_price As Double
            If LinkedSymbol.OpenPrice = 0 Then
                relative_price = 1
            Else
                relative_price = a_data.CoreRecord.Price / LinkedSymbol.OpenPrice
            End If

            Dim matching As Boolean = CheckMatching()

            'If matching = True And (BackupPricelist.Count = 0 OrElse (BackupPricelist.Count > 0 And BackupPricelist(0) > patterncheck_str.Price)) Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
            If matching = True AndAlso Not IsClearingTime(current_time) Then 'AndAlso IsFirstHalfTime(current_time) Then 'AndAlso relative_price < GL1) Then

                '2020.12.22: Not ClearingTime 조건을 추가했다.
                If relative_price > GL1 Then
                    DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME1
                    MatchedFallingStartIndex = 0
                ElseIf relative_price > GL2 Then
                    DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME2
                    MatchedFallingStartIndex = 1
                ElseIf relative_price > GL3 Then
                    DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME3
                    MatchedFallingStartIndex = 2
                ElseIf relative_price > GL4 Then
                    DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME4 '16 
                    MatchedFallingStartIndex = 3
                ElseIf relative_price > GL5 Then
                    DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME5 '25 
                    MatchedFallingStartIndex = 4
                Else
                    DEFAULT_HAVING_TIME = DEFAULT_HAVING_TIME6 '22 
                    MatchedFallingStartIndex = 5
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
                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - FlexPatternList(MatchedFlexIndex).Count).Amount) * patterncheck_str.Price          '하락 볼륨 업데이트
                '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                fall_volume = fall_volume * PatternLength / FlexPatternList(MatchedFlexIndex).Length
                If fall_volume < FALL_VOLUME_THRESHOLD AndAlso Not (patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount) Then
                    If matching Then
                        DebugTotalGullin += 1
                        If DebugPrethresholdYes Then
                            '오 맞췄어!
                            DebugItWorked = True
                            DebugPrethresholdGullin += 1
                        Else
                            '못 맟췄군
                            DebugItWorked = False
                        End If
                        'DebugPrethresholdYes = False
                    Else
                        'DebugPrethresholdYes = False
                    End If
                    'If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                    'TwoMinutesHolding = True
                    'End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
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
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
#End If
            End If
#If 1 Then
            '2023.12.17 : check prethreshold 디버깅용 코드 ========================================
            DebugPrethresholdYes = False
            'CheckPrethreshold()
            '======================================================================================
#End If

        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
#If Not DOUBLE_GULLIN_BUG_FIX Then
            If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount AndAlso _WaitExitCount < 2 Then
                '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거는 이제 치지 말자
                _WaitExitCount = 0
                _CurrentPhase = SearchPhase.WAIT_FALLING
            Else
#End If
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
            _WaitExitCount += 1         '청산기다림 카운트
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
                If AllowMultipleEntering Then
                'multiple entering
#If 0 Then
                'Old style
            'If NumberOfEntering < 3 AndAlso _CountFromLastTop > ENTERING_POINT_FROM_LAST_TOP Then
            If NumberOfEntering < 3 AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD - 1) * (TH_ATTENUATION ^ NumberOfEntering)) Then
                '추가매수해
                _EnterPriceMulti.Add(patterncheck_str.Price)
                'LastEnteredPoint = patterncheck_str.Price
                'FallVolume = (_NumberOfEntering + 1) * FallVolume / _NumberOfEntering
                _FallVolumeMulti.Add(_FallVolumeMulti.Last * VOLUME_ATTENUATION)
                '_NumberOfEntering += 1
                _TopPrice = 0
                _CountFromLastTop = 0
            End If
#Else
                'New style
                Dim refall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - 4).Amount) * patterncheck_str.Price           '재하락 볼륨 업데이트
                '2024.03.19 : fall volume 은 떨어지는 구간 전체의 하강볼륨이다. matched pattern 의 길이에 따라 클 수도 작을 수도 있는데, normalize 할 필요가 있어 아래행을 추가했다.
                refall_volume = refall_volume * PatternLength / 3
                ＇If NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD - 1) * (TH_ATTENUATION ^ NumberOfEntering)) AndAlso refall_volume / 3 > (_FallVolumeMulti.First / PatternLength) * VOLUME_ATTENUATION Then
                If NumberOfEntering < ALLOWED_ENTERING_COUNT AndAlso _EnterPriceMulti.Last / patterncheck_str.Price > 1 + ((FALL_SCALE_LOWER_THRESHOLD - 1) * (TH_ATTENUATION)) AndAlso refall_volume > _FallVolumeMulti.First * VOLUME_ATTENUATION Then

                    '추가매수해
                    _EnterPriceMulti.Add(patterncheck_str.Price)
                    _FallVolumeMulti.Add(refall_volume)
                    '_NumberOfEntering += 1
                    _TopPrice = 0
                    _CountFromLastTop = 0
                End If
#End If
            End If
                '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
                '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
                PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
            'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
#If OLD_FLEXIBLE_HAVING_TIME Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME * (Pattern.Length + (MatchedFlexIndex - (Pattern.Length - FlexPatternList(0).Length))) / Pattern.Length) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
#Else
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
#End If
                'If (_CountFromLastBase >= (DEFAULT_HAVING_TIME + 3 * (NumberOfEntering - 1)) * (Pattern.Length + (MatchedFlexIndex - (Pattern.Length - FlexPatternList(0).Length))) / Pattern.Length) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매상태로 인해 매도 못한다. 기다려야 된다.
                Else
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                    ExitPrice = a_data.CoreRecord.Price                   '청산가
                    'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    ProfitCalculation()
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                    SafeEnterTrace(PartKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
#If 0 Then
                        'MatchedFlexIndex 로 부분합
                        PartCount(MatchedFlexIndex) += 1
                        PartMoney(MatchedFlexIndex) += Profit
#Else
                    'MatchedFallingStartIndex 로 부분합
                    PartCount(MatchedFallingStartIndex) += 1
                    PartMoney(MatchedFallingStartIndex) += Profit
#End If
                    SafeLeaveTrace(PartKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    'slope_stat(slope_index) += 1
                End If

            ElseIf (CType(patterncheck_str.Price, Double) - _BasePrice) / _TargetHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - 1) * 5)              '청산시간
                ExitPrice = a_data.CoreRecord.Price                   '청산가
                'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                ProfitCalculation()
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                SafeEnterTrace(PartKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                'MatchedFallingStartIndex 로 부분합
                PartCount(MatchedFallingStartIndex) += 1
                PartMoney(MatchedFallingStartIndex) += Profit
                SafeLeaveTrace(PartKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
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
#If Not DOUBLE_GULLIN_BUG_FIX Then
            End If
#End If
#If UNWANTED_RISING_DETECTION Then
        ElseIf _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED Then
            TimeCountAfterRising += 1
            If TimeCountAfterRising > ENTERING_PROHIBIT_TIME Then
                TimeCountAfterRising = 0
                _CurrentPhase = SearchPhase.WAIT_FALLING
            End If
#End If
        End If

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
        Dim BuyAmount As UInt64
        Dim SelAmount As UInt64
        Dim DeltaGangdo As Double
    End Structure

    Private PricePointList, DecideTimePointList, DeltaGangdoPointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData, DeltaGangdoCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _TargetHeight As UInt32
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.4896}  'Golden
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}  'Golden 꼬리 올리기
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadraple silver
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -32, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -12, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Mild Sangbong
    'Public Pattern() As Double = {23.99197616, 32.55771001, 33.79806153, 24, 10, -32, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Tailered Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Tough Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -9.367132189, -5, -6, -15.47813968, -31.32709401, -40}            'Brother Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 7, -38, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}     'Brutal Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -5, 4, 3, -15.47813968, -31.32709401, -40}       'couple
    'Public Pattern() As Double = {26, 30, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -5, 4, 3, -15.47813968, -31.32709401, -40}         'husbig
    'Public Pattern() As Double = {14, 23.99197616, 32.55771001, 33.79806153, 24, 10, -35, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Clear Sangbong
    'Public Pattern() As Double = {14, 23.99197616, 32.55771001, 33.79806153, 24, 10, -8, -35, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}       'Far Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -40, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40}        'Toughest Sangbong
    'Public Pattern() As Double = {18, 21, 24.5, 28.3, 32.4, 33.5, 33.79806153, 29.4, 25, 18.8, 10, -7, -35, -31, -26.77410577, -21, -15.10251023, -11.5, -8.6, -5, -2.240000465, -1, -2.000099933, -8, -15.47813968, -23, -31.32709401, -36.5, -40}    'Double Sangbong
    'Public Pattern() As Double = {18, 23.99197616, 32.55771001, 33.79806153, 24, 10, -12, -26.77410577, -15.10251023, -9.367132189, -2.240000465, -2.000099933, -15.47813968, -31.32709401, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -40, -43, -46, -49, -52, -55, -57, -58}  'Water Slide
    'Public Pattern() As Double = {18, 24.60381429, 32.73490309, 31.69847691, 20, -6.071428571, -31.47461676, -20.938308, -11.82515135, -4.785404652, -2.068642942, -12.58998831, -29.06295768, -39.38050671}    'Tough Sangbong-1
    'Public Pattern() As Double = {18, 23.56397787, 31.33403375, 33.53227192, 26.79944615, 15, -15.71428571, -30.88705288, -21.7719934, -13.05416093, -7.330808839, -2.188593208, -3.925534183, -16.61020785, -31.32709401, -39.38050671} 'Tough Sangbong+1
    'Public Pattern() As Double = {19.5, 26.99197616, 37.05771001, 39.79806153, 31.5, 19, -24.5, -14.77410577, -1.602510233, 5.632867811, 14.25999954, 15.99990007, 4.021860317, -10.32709401, -17.5}    'Rising Tough Sangbong
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.002707806, -0.265102023, -0.426021484, -0.520208505, -0.572599588, -0.600375}       'TwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.002707806, -0.265102023, -0.426021484, -0.520208505, -0.572599588, -0.600375, -0.614767219, -0.622628258, -0.627755859, -0.631978567, -0.635999665, -0.64}         'CompleteTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, -0.265102023, -0.426021484, -0.520208505, -0.572599588, -0.600375}       'ShortTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.002030854, -0.198826517, -0.319516113, -0.390156379, -0.429449691, -0.45028125}     'ScaleDownTwoStair
    Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}     'ScaleUpTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.004061708, -0.397653035, -0.639032227, -0.780312757, -0.858899382, -0.9005625}      'Scale1.5TwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, 0.002004789, -0.307620388, -0.497505352, -0.608646036, -0.670467514, -0.7032425}          'Scale1.18TwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, 0, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}       'ScaleUpLongTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 5.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, 0, 0, 0, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}       'ScaleUpLongerTwoStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.804061708, -1.197653035, -1.439032227, -1.580312757, -1.658899382}    'ScaleUpThreeStair
    'Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair

    'Public Pattern() As Double = {18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1}
    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_THRESHOLD As Double = 4.2 '3.85
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 1.1
    Public DELTA_PERIOD As Integer = 3
    Public DELTA_GANGDO_THRESHOLD As Double = 5
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.5

    Public SCORE_LOWER_THRESHOLD As Double = 0.1
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1.4
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 10

    Public OneMoreSampleCheck As Boolean = False
    Public BuyAmountCenter As Double = 0
    Public SelAmountCenter As Double = 0
    Public PatternGangdo As Double = 0
    Public PatternName As String
    'Public EnteringGangdoChange As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime, ByVal index_decision_center As Integer)
        MyBase.New(linked_symbol, start_time)

        Select Case index_decision_center
#If 0 Then
            Case 0
                PatternName = "ScaleUpFourStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair
                DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.2
                FALL_SCALE_LOWER_THRESHOLD = 1.13
            Case 0
                PatternName = "ScaleUpFourStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair
                DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.2
                FALL_SCALE_LOWER_THRESHOLD = 1.13
#End If
#If 1 Then
            Case 0
                PatternName = "Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.1
                FALL_SCALE_LOWER_THRESHOLD = 1.069
            Case 1
                PatternName = "Double Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver            Case Else
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 3.45
                FALL_SCALE_LOWER_THRESHOLD = 1.08
            Case 2
                PatternName = "Quadruple Silver"
                Pattern = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.668919699999996, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadruple silver
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 2.5
                FALL_SCALE_LOWER_THRESHOLD = 1.1
            Case 3
                PatternName = "ScaleUpTwoStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875}     'ScaleUpTwoStair
                DEFAULT_HAVING_TIME = 23
                SCORE_THRESHOLD = 4.5
                FALL_SCALE_LOWER_THRESHOLD = 1.1
            Case 4
                PatternName = "ScaleUpThreeStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.804061708, -1.197653035, -1.439032227, -1.580312757, -1.658899382}    'ScaleUpThreeStair
                DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.5
                FALL_SCALE_LOWER_THRESHOLD = 1.12
            Case 5
                PatternName = "ScaleUpFourStair"
                Pattern = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair
                DEFAULT_HAVING_TIME = 54
                SCORE_THRESHOLD = 4.2
                FALL_SCALE_LOWER_THRESHOLD = 1.13
#End If
            Case Else
                MsgBox("예상치 못한 index_decision_center")
        End Select


        If SmartLearning Then
            'DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
            'SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            'FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            'MAX_HAVING_LENGTH = MainForm.Form_MAX_HAVING_LENGTH
        Else
            'DEFAULT_HAVING_TIME = TestArray(TestIndex)
            '_POSITIVE_RELATIVE_CUT_THRESHOLD = TestArray(TestIndex)
        End If

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

        'DeltaGangdo CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("DeltaGangdo", DataType.REAL_NUMBER_DATA, Nothing)
        DeltaGangdoCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DeltaGangdo")
        DeltaGangdoPointList = New PointList()
        DeltaGangdoCompositeData.SetData(DeltaGangdoPointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
        GraphicCompositeDataList.Add(DeltaGangdoCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다. 현재는 pattern check 에 주력
    End Sub

    Public Overrides Sub CreateGraphicData()
        '160520: 그래프 어떻게 그릴까 생각해보자. 앞뒤로 쪼금씩 더 보여주는 게 필요하겠지.
        '160521: 기본적으로 보유중인 패턴들 중 최대길이 가진 놈보다 몇 개정도 더 긴 길이 만큼의 히스토리를 관리하도록 한다.
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).DeltaGangdo)
            DeltaGangdoPointList.Add(a_point)
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
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds - Pattern.Count * 5 + 0.001, 1))        '진입시간 + epsilon
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

            _TargetHeight = b_max - b_min           'target height 업데이트

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

            If score < SCORE_THRESHOLD And score > SCORE_LOWER_THRESHOLD And b_max / b_min > FALL_SCALE_LOWER_THRESHOLD And b_max / b_min < FALL_SCALE_UPPER_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
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
        patterncheck_str.BuyAmount = a_data.BuyAmount       '매수거래량
        patterncheck_str.SelAmount = a_data.SelAmount       '매도거래량
        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            Dim delta_buy As UInt64 = Math.Max(1, CType(patterncheck_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            Dim delta_sel As UInt64 = Math.Max(1, CType(patterncheck_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            patterncheck_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            patterncheck_str.DeltaGangdo = -1
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

            If matching = True Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
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
                If GangdoDB Then
                    '핵심지시자 3인방 계산
                    If patterncheck_str.BuyAmount > 0 And patterncheck_str.SelAmount > 0 Then
                        Dim buy_sum As Int64 = Pattern.Count * patterncheck_str.BuyAmount
                        Dim sel_sum As Int64 = Pattern.Count * patterncheck_str.SelAmount
                        For index As Integer = 0 To Pattern.Count - 1
                            buy_sum -= RecordList(RecordList.Count - 2 - index).BuyAmount
                            sel_sum -= RecordList(RecordList.Count - 2 - index).SelAmount
                        Next
                        If (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) > 0 Then
                            BuyAmountCenter = buy_sum / ((CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) * Pattern.Count)
                        Else
                            BuyAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) > 0 Then
                            SelAmountCenter = sel_sum / ((CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) * Pattern.Count)
                        Else
                            SelAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount) > 0 Then
                            PatternGangdo = 100 * (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - 5).BuyAmount) / (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount)
                        Else
                            PatternGangdo = Double.MaxValue
                        End If
                    Else
                        '180127: 어떤 때는 Gangdo가 0보다 작은 경우도 이쪽으로 들어와서 divide by 0 에러를 일으킨다..
                    End If
                End If

                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                If fall_volume < FALL_VOLUME_THRESHOLD Then
                    If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                        '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                        TwoMinutesHolding = True
                    End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
                    'EnteringGangdoChange = patterncheck_str.DeltaGangdo / RecordList(RecordList.Count - 1 - DELTA_PERIOD).DeltaGangdo
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
                    'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                    FallVolume = fall_volume           '하락 볼륨 업데이트
                    OneMoreSampleCheck = True
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
#End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
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
            If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                _TopPrice = patterncheck_str.Price
                _CountFromLastTop = 0
            Else
                _CountFromLastTop += 1
            End If
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
            'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매상태로 인해 매도 못한다. 기다려야 된다.
                Else
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    ExitPrice = a_data.CoreRecord.Price                   '청산가
                    Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                End If

            ElseIf (CType(patterncheck_str.Price, Double) - _BasePrice) / _TargetHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.CoreRecord.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
            ElseIf ((CType(patterncheck_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.CoreRecord.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                'If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                '_SecondChance = True
                'End If
            End If
        End If

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class
#End If

#If 0 Then
Public Class c05E_PatternChecker_copy1
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        Dim BuyAmount As UInt64
        Dim SelAmount As UInt64
        Dim DeltaGangdo As Double
    End Structure

    Private PricePointList, DecideTimePointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.4896}  'Golden
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}  'Golden 꼬리 올리기
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadraple silver
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 44}
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 110, 106, 93, 74, 69, 71, 77}
    'Public Pattern() As Double = {48, 66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 44}
    'Public Pattern() As Double = {66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 47, 58}
    'Public Pattern() As Double = {11, 11.2, 11.4, 11.7, 12.3, 12.9, 13.7, 14.5, 15.4, 16.4, 17.5, 18.7, 20, 21.5, 23.2, 25.8, 30, 38, 66, 100, 110, 106, 93, 74, 69, 71, 77}
    'Public Pattern() As Double = {11, 11.2, 11.4, 11.7, 12.3, 12.9, 13.7, 14.5, 15.4, 16.4, 17.5, 18.7, 20, 21.5, 23.2, 25.8, 30, 38, 66, 100, 110, 106, 93, 74, 69, 71, 77, 88, 99}
    'Public Pattern() As Double = {100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5, 0, 1, 2, 3}
    'Public Pattern() As Double = {38.22370772, 37.24723526, 36.27323722, 35.30197383, 34.3337327, 33.36883167, 32.40762204, 31.45049202, 30.49787068, 29.5502322, 28.60810063, 27.67205513, 26.74273578, 25.82084999, 24.90717953, 24.00258844, 23.10803158, 22.22456428, 21.35335283, 20.49568619, 19.65298888, 18.82683524, 18.01896518, 17.2313016, 16.46596964, 15.72531793, 15.01194212, 14.32871084, 13.67879441, 13.0656966, 12.49328964, 11.96585304, 11.48811636, 11.0653066, 10.70320046, 10.40818221, 10.18730753, 10.04837418, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10}
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 106, 104, 102, 100, 98, 102, 106}
    Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.804061708, -1.197653035, -1.439032227, -1.580312757, -1.658899382}    'ScaleUpThreeStair


    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public DEFAULT_HAVING_TIME As Integer = 54 'HAVING_LENGTH
    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_THRESHOLD As Double = 4.5 '3.85
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 1.12
    Public DELTA_PERIOD As Integer = 3
    Public DELTA_GANGDO_THRESHOLD As Double = 5
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.5
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1.4

    Public OneMoreSampleCheck As Boolean = False
    Public BuyAmountCenter As Double = 0
    Public SelAmountCenter As Double = 0
    Public PatternGangdo As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        If SmartLearning Then
            DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
            SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            MAX_HAVING_LENGTH = MainForm.Form_MAX_HAVING_LENGTH
        Else
            'FALL_SCALE_LOWER_THRESHOLD = TestArray(TestIndex)
        End If

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
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다. 현재는 pattern check 에 주력
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
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5 + 0.001, 0))        '진입시간 + epsilon
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

            If score < SCORE_THRESHOLD And b_max / b_min > FALL_SCALE_LOWER_THRESHOLD And b_max / b_min < FALL_SCALE_UPPER_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
        Else
            result = False
        End If

        Return result
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

        patterncheck_str.Price = a_data.Price         '주가 저장
        patterncheck_str.Amount = a_data.Amount         '거래량 저장
        patterncheck_str.BuyAmount = a_data.BuyAmount       '매수거래량
        patterncheck_str.SelAmount = a_data.SelAmount       '매도거래량
        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            Dim delta_buy As UInt64 = Math.Max(1, CType(patterncheck_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            Dim delta_sel As UInt64 = Math.Max(1, CType(patterncheck_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            patterncheck_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            patterncheck_str.DeltaGangdo = -1
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드
            Dim matching As Boolean = CheckMatching()

            If matching = True Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
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
                If GangdoDB Then
                    '핵심지시자 3인방 계산
                    If patterncheck_str.BuyAmount > 0 And patterncheck_str.SelAmount > 0 Then
                        Dim buy_sum As Int64 = Pattern.Count * patterncheck_str.BuyAmount
                        Dim sel_sum As Int64 = Pattern.Count * patterncheck_str.SelAmount
                        For index As Integer = 0 To Pattern.Count - 1
                            buy_sum -= RecordList(RecordList.Count - 2 - index).BuyAmount
                            sel_sum -= RecordList(RecordList.Count - 2 - index).SelAmount
                        Next
                        If (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) > 0 Then
                            BuyAmountCenter = buy_sum / ((CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) * Pattern.Count)
                        Else
                            BuyAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) > 0 Then
                            SelAmountCenter = sel_sum / ((CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) * Pattern.Count)
                        Else
                            SelAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount) > 0 Then
                            PatternGangdo = 100 * (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - 5).BuyAmount) / (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount)
                        Else
                            PatternGangdo = Double.MaxValue
                        End If
                    Else
                        '180127: 어떤 때는 Gangdo가 0보다 작은 경우도 이쪽으로 들어와서 divide by 0 에러를 일으킨다..
                    End If
                End If

                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                If fall_volume < FALL_VOLUME_THRESHOLD Then
                    If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                        '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                        TwoMinutesHolding = True
                    End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - Pattern.Count) * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
                    'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                    FallVolume = fall_volume           '하락 볼륨 업데이트
                    OneMoreSampleCheck = True
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
#End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
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
            If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                _TopPrice = patterncheck_str.Price
                _CountFromLastTop = 0
            Else
                _CountFromLastTop += 1
            End If
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
            'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
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
                'ElseIf ((CType(patterncheck_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                ' _CurrentPhase = SearchPhase.DONE
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

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

Public Class c05E_PatternChecker_copy2
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        Dim BuyAmount As UInt64
        Dim SelAmount As UInt64
        Dim DeltaGangdo As Double
    End Structure

    Private PricePointList, DecideTimePointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.4896}  'Golden
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}  'Golden 꼬리 올리기
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadruple silver
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 44}
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 110, 106, 93, 74, 69, 71, 77}
    'Public Pattern() As Double = {48, 66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 44}
    'Public Pattern() As Double = {66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 47, 58}
    'Public Pattern() As Double = {11, 11.2, 11.4, 11.7, 12.3, 12.9, 13.7, 14.5, 15.4, 16.4, 17.5, 18.7, 20, 21.5, 23.2, 25.8, 30, 38, 66, 100, 110, 106, 93, 74, 69, 71, 77}
    'Public Pattern() As Double = {11, 11.2, 11.4, 11.7, 12.3, 12.9, 13.7, 14.5, 15.4, 16.4, 17.5, 18.7, 20, 21.5, 23.2, 25.8, 30, 38, 66, 100, 110, 106, 93, 74, 69, 71, 77, 88, 99}
    'Public Pattern() As Double = {100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5, 0, 1, 2, 3}
    'Public Pattern() As Double = {38.22370772, 37.24723526, 36.27323722, 35.30197383, 34.3337327, 33.36883167, 32.40762204, 31.45049202, 30.49787068, 29.5502322, 28.60810063, 27.67205513, 26.74273578, 25.82084999, 24.90717953, 24.00258844, 23.10803158, 22.22456428, 21.35335283, 20.49568619, 19.65298888, 18.82683524, 18.01896518, 17.2313016, 16.46596964, 15.72531793, 15.01194212, 14.32871084, 13.67879441, 13.0656966, 12.49328964, 11.96585304, 11.48811636, 11.0653066, 10.70320046, 10.40818221, 10.18730753, 10.04837418, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10}
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 106, 104, 102, 100, 98, 102, 106}
    Public Pattern() As Double = {0.637292194, 0.374897977, 0.213978516, 0.119791495, 0.067400412, 0.039625, 0.025232781, 0.017371742, 0.012244141, 0.008021433, 0.004000335, 0, -0.003384757, -0.331377529, -0.532526855, -0.650260631, -0.715749485, -0.75046875, -0.768459024, -0.778285322, -0.784694824, -0.789973208, -0.794999581, -0.8, -0.8, -1.127992772, -1.329142099, -1.446875874, -1.512364728, -1.547083993, -1.565074267, -1.574900565, -1.581310067, -1.586588451, -1.591614824, -1.596615243, -1.596615243, -1.99020657, -2.231585761, -2.372866292, -2.451452917} 'ScaleUpFourStair


    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public DEFAULT_HAVING_TIME As Integer = 54 'HAVING_LENGTH
    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_THRESHOLD As Double = 4.2 '3.85
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 1.13
    Public DELTA_PERIOD As Integer = 3
    Public DELTA_GANGDO_THRESHOLD As Double = 5
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.5
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1.4

    Public OneMoreSampleCheck As Boolean = False
    Public BuyAmountCenter As Double = 0
    Public SelAmountCenter As Double = 0
    Public PatternGangdo As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        If SmartLearning Then
            DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
            SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            MAX_HAVING_LENGTH = MainForm.Form_MAX_HAVING_LENGTH
        Else
            'FALL_SCALE_LOWER_THRESHOLD = TestArray(TestIndex)
        End If

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
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다. 현재는 pattern check 에 주력
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
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5 + 0.001, 0))        '진입시간 + epsilon
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

            If score < SCORE_THRESHOLD And b_max / b_min > FALL_SCALE_LOWER_THRESHOLD And b_max / b_min < FALL_SCALE_UPPER_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
        Else
            result = False
        End If

        Return result
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

        patterncheck_str.Price = a_data.Price         '주가 저장
        patterncheck_str.Amount = a_data.Amount         '거래량 저장
        patterncheck_str.BuyAmount = a_data.BuyAmount       '매수거래량
        patterncheck_str.SelAmount = a_data.SelAmount       '매도거래량
        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            Dim delta_buy As UInt64 = Math.Max(1, CType(patterncheck_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            Dim delta_sel As UInt64 = Math.Max(1, CType(patterncheck_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            patterncheck_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            patterncheck_str.DeltaGangdo = -1
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드
            Dim matching As Boolean = CheckMatching()

            If matching = True Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
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
                If GangdoDB Then
                    '핵심지시자 3인방 계산
                    If patterncheck_str.BuyAmount > 0 And patterncheck_str.SelAmount > 0 Then
                        Dim buy_sum As Int64 = Pattern.Count * patterncheck_str.BuyAmount
                        Dim sel_sum As Int64 = Pattern.Count * patterncheck_str.SelAmount
                        For index As Integer = 0 To Pattern.Count - 1
                            buy_sum -= RecordList(RecordList.Count - 2 - index).BuyAmount
                            sel_sum -= RecordList(RecordList.Count - 2 - index).SelAmount
                        Next
                        If (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) > 0 Then
                            BuyAmountCenter = buy_sum / ((CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) * Pattern.Count)
                        Else
                            BuyAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) > 0 Then
                            SelAmountCenter = sel_sum / ((CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) * Pattern.Count)
                        Else
                            SelAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount) > 0 Then
                            PatternGangdo = 100 * (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - 5).BuyAmount) / (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount)
                        Else
                            PatternGangdo = Double.MaxValue
                        End If
                    Else
                        '180127: 어떤 때는 Gangdo가 0보다 작은 경우도 이쪽으로 들어와서 divide by 0 에러를 일으킨다..
                    End If
                End If

                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                If fall_volume < FALL_VOLUME_THRESHOLD Then
                    If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                        '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                        TwoMinutesHolding = True
                    End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - Pattern.Count) * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
                    'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                    FallVolume = fall_volume           '하락 볼륨 업데이트
                    OneMoreSampleCheck = True
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
#End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
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
            If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                _TopPrice = patterncheck_str.Price
                _CountFromLastTop = 0
            Else
                _CountFromLastTop += 1
            End If
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
            'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
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
                'ElseIf ((CType(patterncheck_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                ' _CurrentPhase = SearchPhase.DONE
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

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

Public Class c05E_PatternChecker_copy3
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        Dim BuyAmount As UInt64
        Dim SelAmount As UInt64
        Dim DeltaGangdo As Double
    End Structure

    Private PricePointList, DecideTimePointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.4896}  'Golden
    'Public Pattern() As Double = {0, -0.00200016, -0.00400256, -0.00601296, -0.00804096, -0.0101, -0.01220736, -0.01438416, -0.01665536, -0.01904976, -0.0216, -0.02434256, -0.02731776, -0.03056976, -0.03414656, -0.0381, -0.04248576, -0.04736336, -0.05279616, -0.05885136, -0.0656, -0.07311696, -0.08148096, -0.09077456, -0.10108416, -0.1125, -0.12511616, -0.13903056, -0.15434496, -0.17116496, -0.1896, -0.20976336, -0.23177216, -0.25574736, -0.28181376, -0.3101, -0.34073856, -0.37386576, -0.40962176, -0.38}  'Golden 꼬리 올리기
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
    Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051}        ' Double Silver
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194, -1.048, -1.668488568, -2.577626372, -3.874697266, -5.682655693, -8.15162302, -11.462625, -15.83157036, -21.51347051, -28.80690039, -38.05870045, -49.6689197, -64.096, -81.86220134, -103.5592683, -129.8543379, -161.4960878, -199.3211267, -244.260625, -297.3471871, -359.7219643, -432.6420098, -517.4878733, -615.7714384, -729.144, -859.4045831, -1008.508503, -1178.576166}  'quadraple silver
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 44}
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 110, 106, 93, 74, 69, 71, 77}
    'Public Pattern() As Double = {48, 66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 44}
    'Public Pattern() As Double = {66, 96, 108, 110, 107, 97, 72, 53, 42, 41, 47, 58}
    'Public Pattern() As Double = {11, 11.2, 11.4, 11.7, 12.3, 12.9, 13.7, 14.5, 15.4, 16.4, 17.5, 18.7, 20, 21.5, 23.2, 25.8, 30, 38, 66, 100, 110, 106, 93, 74, 69, 71, 77}
    'Public Pattern() As Double = {11, 11.2, 11.4, 11.7, 12.3, 12.9, 13.7, 14.5, 15.4, 16.4, 17.5, 18.7, 20, 21.5, 23.2, 25.8, 30, 38, 66, 100, 110, 106, 93, 74, 69, 71, 77, 88, 99}
    'Public Pattern() As Double = {100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50, 45, 40, 35, 30, 25, 20, 15, 10, 5, 0, 1, 2, 3}
    'Public Pattern() As Double = {38.22370772, 37.24723526, 36.27323722, 35.30197383, 34.3337327, 33.36883167, 32.40762204, 31.45049202, 30.49787068, 29.5502322, 28.60810063, 27.67205513, 26.74273578, 25.82084999, 24.90717953, 24.00258844, 23.10803158, 22.22456428, 21.35335283, 20.49568619, 19.65298888, 18.82683524, 18.01896518, 17.2313016, 16.46596964, 15.72531793, 15.01194212, 14.32871084, 13.67879441, 13.0656966, 12.49328964, 11.96585304, 11.48811636, 11.0653066, 10.70320046, 10.40818221, 10.18730753, 10.04837418, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10}
    'Public Pattern() As Double = {0, 1, 2, 3, 4.3, 5.7, 6.6, 8.3, 9.6, 11.16666667, 12.9, 15, 17.7, 22, 28, 36, 48, 66, 96, 108, 106, 104, 102, 100, 98, 102, 106}


    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_THRESHOLD As Double = 3.1
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 2 '1.3
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 3 '1.4
    Public DELTA_PERIOD As Integer = 2
    Public DELTA_GANGDO_THRESHOLD As Double = 5
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.1

    Public OneMoreSampleCheck As Boolean = False
    Public BuyAmountCenter As Double = 0
    Public SelAmountCenter As Double = 0
    Public PatternGangdo As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        If SmartLearning Then
            DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
            SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            MAX_HAVING_LENGTH = MainForm.Form_MAX_HAVING_LENGTH
        Else
            'FALL_SCALE_LOWER_THRESHOLD = TestArray(TestIndex)
        End If

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
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다. 현재는 pattern check 에 주력
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
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5 + 0.001, 0))        '진입시간 + epsilon
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

            If score < SCORE_THRESHOLD And b_max / b_min > FALL_SCALE_LOWER_THRESHOLD And b_max / b_min < FALL_SCALE_UPPER_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
        Else
            result = False
        End If

        Return result
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

        patterncheck_str.Price = a_data.Price         '주가 저장
        patterncheck_str.Amount = a_data.Amount         '거래량 저장
        patterncheck_str.BuyAmount = a_data.BuyAmount       '매수거래량
        patterncheck_str.SelAmount = a_data.SelAmount       '매도거래량
        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            Dim delta_buy As UInt64 = Math.Max(1, CType(patterncheck_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            Dim delta_sel As UInt64 = Math.Max(1, CType(patterncheck_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            patterncheck_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            patterncheck_str.DeltaGangdo = -1
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드
            Dim matching As Boolean = CheckMatching()

            If matching = True Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
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
                If GangdoDB Then
                    '핵심지시자 3인방 계산
                    If patterncheck_str.BuyAmount > 0 And patterncheck_str.SelAmount > 0 Then
                        Dim buy_sum As Int64 = Pattern.Count * patterncheck_str.BuyAmount
                        Dim sel_sum As Int64 = Pattern.Count * patterncheck_str.SelAmount
                        For index As Integer = 0 To Pattern.Count - 1
                            buy_sum -= RecordList(RecordList.Count - 2 - index).BuyAmount
                            sel_sum -= RecordList(RecordList.Count - 2 - index).SelAmount
                        Next
                        If (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) > 0 Then
                            BuyAmountCenter = buy_sum / ((CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) * Pattern.Count)
                        Else
                            BuyAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) > 0 Then
                            SelAmountCenter = sel_sum / ((CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) * Pattern.Count)
                        Else
                            SelAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount) > 0 Then
                            PatternGangdo = 100 * (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - 5).BuyAmount) / (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount)
                        Else
                            PatternGangdo = Double.MaxValue
                        End If
                    Else
                        '180127: 어떤 때는 Gangdo가 0보다 작은 경우도 이쪽으로 들어와서 divide by 0 에러를 일으킨다..
                    End If
                End If

                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                If fall_volume < FALL_VOLUME_THRESHOLD Then
                    If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                        '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                        TwoMinutesHolding = True
                    End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - Pattern.Count) * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
                    'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                    FallVolume = fall_volume           '하락 볼륨 업데이트
                    OneMoreSampleCheck = True
                Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                End If
#End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
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
            If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                _TopPrice = patterncheck_str.Price
                _CountFromLastTop = 0
            Else
                _CountFromLastTop += 1
            End If
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
            'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
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
                'ElseIf ((CType(patterncheck_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                ' _CurrentPhase = SearchPhase.DONE
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

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

Public Class c05C_NoUnidelta_DecisionMaker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure UnideltaStructure
        Dim Price As UInt32
        Dim MAPrice As UInt32
        'Dim BuyMiniSum As UInt64    '델타 매수거래량 소계
        'Dim SelMiniSum As UInt64    '델타 매도거래량 소계
        Dim Amount As UInt64       '델타 거래량 소계
        Dim BuyDelta As UInt64      '델타 매수거래량
        Dim SelDelta As UInt64      '델타 매도거래량
        ' Dim Unidelta As Double      'Uni-delta
    End Structure

    '써치 페이즈
    '131015:MA는 4번의 가격을 평균하는 것으로 알고 있는데.. 그 4가 어디있지?.
    Public _SELL_SUPERIOR_THRESHOLD As Integer = 7
    Public _BUY_SUPERIOR_THRESHOLD As Integer = 7
    Public _BUY_ALLOW_THRESHOLD As Integer = 5
    Public _SELL_ALLOW_THRESHOLD As Integer = 5
    Public _BUY_APPEARANCE_THRESHOLD As Integer = 5
    Public _SELL_APPEARANCE_THRESHOLD As Integer = 5
    Public _FALL_PRE_THRESHOLD As Double = 0.03
    Public _FALL_PERCENT_THRESHOLD As Double = 0.95     '09:00 ~ 09:03
    Public _FALL_PERCENT_THRESHOLD1 As Double = 0.046   '09:03 ~ 09:06
    Public _FALL_PERCENT_THRESHOLD2 As Double = 0.043   '09:06 ~ 09:30
    Public _FALL_PERCENT_THRESHOLD3 As Double = 0.057    '09:30 ~ 10:00
    Public _FALL_PERCENT_THRESHOLD4 As Double = 0.057   '10:00 ~ 10:30
    Public _FALL_PERCENT_THRESHOLD5 As Double = 0.042   '10:30 ~ 11:30
    Public _FALL_PERCENT_THRESHOLD6 As Double = 0.068   '11:30 ~ 12:00
    Public _FALL_PERCENT_THRESHOLD7 As Double = 0.063   '12:00 ~ 13:00
    Public _FALL_PERCENT_THRESHOLD8 As Double = 0.08   '13:00 ~ 13:30
    Public _FALL_PERCENT_THRESHOLD9 As Double = 0.105   '13:30 ~ 14:10
    Public _FALL_PERCENT_THRESHOLD10 As Double = 0.076    '14:10 ~ 14:30
    Public _FALL_PERCENT_THRESHOLD11 As Double = 0.07   '14:30 ~
    Public _RISE_PERCENT_THRESHOLD As Double = 0.052
    Public _FALLBACK_PERCENT_THRESHOLD As Double = 0.058
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 1.54
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.26
    Public _DEFAULT_HAVING_TIME0 As Integer = 265   '09:00 ~ 09:03
    Public _DEFAULT_HAVING_TIME1 As Integer = 265   '09:03 ~ 09:06
    Public _DEFAULT_HAVING_TIME2 As Integer = 265   '09:06 ~ 09:30
    Public _DEFAULT_HAVING_TIME3 As Integer = 265   '09:30 ~ 10:00
    Public _DEFAULT_HAVING_TIME4 As Integer = 265   '10:00 ~ 10:30
    Public _DEFAULT_HAVING_TIME5 As Integer = 265   '10:30 ~ 11:30
    Public _DEFAULT_HAVING_TIME6 As Integer = 265   '11:30 ~ 12:00
    Public _DEFAULT_HAVING_TIME7 As Integer = 265   '12:00 ~ 13:00
    Public _DEFAULT_HAVING_TIME8 As Integer = 265   '13:00 ~ 13:30
    Public _DEFAULT_HAVING_TIME9 As Integer = 265   '13:30 ~ 14:10
    Public _DEFAULT_HAVING_TIME10 As Integer = 265   '14:10 ~ 14:30
    Public _DEFAULT_HAVING_TIME11 As Integer = 265   '14:30 ~
    Public _SECOND_CHANCE_THRESHOLD_TIME As Integer = 0
    Public _SECOND_CHANCE_FACTOR As Double = 1.13
    Public _YESTER_ADDITION_28P As Double = 0.028
    Public _YESTER_ADDITION_26P As Double = 0.024
    Public _YESTER_ADDITION_24P As Double = 0.013
    Public _YESTER_ADDITION_22P As Double = 0.03
    Public _YESTER_ADDITION_20P As Double = 0.026
    Public _YESTER_ADDITION_18P As Double = 0.029
    Public _YESTER_ADDITION_16P As Double = 0.03
    Public _YESTER_ADDITION_14P As Double = 0.018
    Public _YESTER_ADDITION_12P As Double = 0.019
    Public _YESTER_ADDITION_10P As Double = 0.027
    Public _YESTER_ADDITION_8P As Double = 0.016
    Public _YESTER_ADDITION_6P As Double = 0.009
    Public _YESTER_ADDITION_4P As Double = 0.005
    Public _YESTER_ADDITION_2P As Double = 0.022
    Public _YESTER_ADDITION_0 As Double = -0.009
    Public _YESTER_ADDITION_2N As Double = 0.028
    Public _YESTER_ADDITION_4N As Double = -0.012
    Public _YESTER_ADDITION_6N As Double = -0.006
    Public _YESTER_ADDITION_8N As Double = 0.027
    Public _YESTER_ADDITION_10N As Double = -0.011
    Public _YESTER_ADDITION_12N As Double = -0.007
    Public _YESTER_ADDITION_14N As Double = -0.005
    Public _YESTER_ADDITION_16N As Double = -0.012
    Public _YESTER_ADDITION_18N As Double = -0.017
    Public _YESTER_ADDITION_20N As Double = 0.02
    Public _YESTER_ADDITION_22N As Double = 0.0
    Public _YESTER_ADDITION_24N As Double = 0.008
    Public _YESTER_ADDITION_26N As Double = -0.01
    Public _YESTER_ADDITION_28N As Double = -0.013
    Public _YESTER_ADDITION_30N As Double = -0.013
#If 0 Then      '+/- 15% 시절
    Public _YESTER_ADDITION_13P As Double = 0
    Public _YESTER_ADDITION_11P As Double = 0
    Public _YESTER_ADDITION_9P As Double = 0
    Public _YESTER_ADDITION_7P As Double = 0
    Public _YESTER_ADDITION_5P As Double = 0
    Public _YESTER_ADDITION_3P As Double = 0
    Public _YESTER_ADDITION_1P As Double = 0
    Public _YESTER_ADDITION_1N As Double = 0
    Public _YESTER_ADDITION_3N As Double = 0
    Public _YESTER_ADDITION_5N As Double = 0
    Public _YESTER_ADDITION_7N As Double = 0
    Public _YESTER_ADDITION_9N As Double = 0
    Public _YESTER_ADDITION_11N As Double = 0
    Public _YESTER_ADDITION_13N As Double = 0
    Public _YESTER_ADDITION_15N As Double = 0
#End If

    Private RecordList As New List(Of UnideltaStructure)
    Private RecordListForFallback As New List(Of UnideltaStructure)
    'Private _BuyAmountOld As UInt64
    'Private _SelAmountOld As UInt64
    Private _SellSuperiorCount As Integer
    Private _BuySuperiorCount As Integer
    Private _BuyAllowCount As Integer = 0
    Private _SellAllowCount As Integer = 0
    Private _BuyAppearanceCount As Integer = 0
    Private _SellAppearanceCount As Integer = 0
    '    Private _UnideltaSum As Double = 0
    '    Private _UnideltaCount As Integer = 0
    '    Private _UnideltaAve As Double
    Private _MaxUnidelta As Double = [Double].MinValue
    Private _MinUnidelta As Double = [Double].MaxValue
    Private _WaitExitCount As Integer = 0
    Public _FallingStartPrice As UInt32 = 0
    Private _RisingStartPrice As UInt32 = 0
    Private _FallingStartAmount As UInt32 = 0
    Private _FallHeight As UInt32 = 0
    Private _RiseHeight As UInt32 = 0
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _RecordCountForPreThreshold As Integer = 0
    'Public PricePointList, MAPointList, BuyMiniSumPointList, SelMiniSumPointList, UnideltaPointList, DecideTimePointList As PointList
    Private PricePointList, MAPointList, DecideTimePointList As PointList
    '    Public PriceCompositeData, MACompositeData, BuyMiniSumCompositeData, SelMiniSumCompositeData, UnideltaCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private PriceCompositeData, MACompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Public NoMoreOperation As Boolean = False
    Private FixedFallThreshold As Double

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)
        _FALL_PERCENT_THRESHOLD5 = TestArray(TestIndex)

        '_DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
        FixedFallThreshold = AdaptiveFallThreshold(current_time)

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '이평가(MAPrice) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("이평가", DataType.REAL_NUMBER_DATA, Nothing)
        MACompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "이평가")
        MAPointList = New PointList()
        MACompositeData.SetData(MAPointList)

#If 0 Then
        'BuyMiniSum CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("BuyMiniSum", DataType.REAL_NUMBER_DATA, Nothing)
        BuyMiniSumCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "BuyMiniSum")
        BuyMiniSumPointList = New PointList()
        BuyMiniSumCompositeData.SetData(BuyMiniSumPointList)

        'SelMiniSum CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("SelMiniSum", DataType.REAL_NUMBER_DATA, Nothing)
        SelMiniSumCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "SelMiniSum")
        SelMiniSumPointList = New PointList()
        SelMiniSumCompositeData.SetData(SelMiniSumPointList)
#End If

        'Unidelta CompositeData
        'x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("Unidelta", DataType.REAL_NUMBER_DATA, Nothing)
        'UnideltaCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "Unidelta")
        'UnideltaPointList = New PointList()
        'UnideltaCompositeData.SetData(UnideltaPointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(MACompositeData)
        'GraphicCompositeDataList.Add(BuyMiniSumCompositeData)
        'GraphicCompositeDataList.Add(SelMiniSumCompositeData)
        'GraphicCompositeDataList.Add(UnideltaCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)

    End Sub

    Public ReadOnly Property AdaptiveDefaultHavingTime(ByVal current_time As DateTime) As Integer
        Get
            Dim current_minutes As Integer = current_time.TimeOfDay.TotalSeconds
            If current_minutes < MarketStartTime * 3600 + 193 Then
                Return _DEFAULT_HAVING_TIME0
            ElseIf current_minutes < MarketStartTime * 3600 + 6 * 60 Then
                Return _DEFAULT_HAVING_TIME1
            ElseIf current_minutes < MarketStartTime * 3600 + 30 * 60 Then
                Return _DEFAULT_HAVING_TIME2
            ElseIf current_minutes < MarketStartTime * 3600 + 60 * 60 Then
                Return _DEFAULT_HAVING_TIME3
            ElseIf current_minutes < MarketStartTime * 3600 + 90 * 60 Then
                Return _DEFAULT_HAVING_TIME4
            ElseIf current_minutes < MarketStartTime * 3600 + 150 * 60 Then
                Return _DEFAULT_HAVING_TIME5
            ElseIf current_minutes < MarketStartTime * 3600 + 180 * 60 Then
                Return _DEFAULT_HAVING_TIME6
            ElseIf current_minutes < MarketStartTime * 3600 + 240 * 60 Then
                Return _DEFAULT_HAVING_TIME7
            ElseIf current_minutes < MarketStartTime * 3600 + 270 * 60 Then
                Return _DEFAULT_HAVING_TIME8
            ElseIf current_minutes < MarketStartTime * 3600 + 310 * 60 Then
                Return _DEFAULT_HAVING_TIME9
            ElseIf current_minutes < MarketStartTime * 3600 + 330 * 60 Then
                Return _DEFAULT_HAVING_TIME10
            Else 'If current_minutes < MarketStartTime * 60 + 240 Then    '14:28 ~ 
                Return _DEFAULT_HAVING_TIME11
            End If
        End Get
    End Property

    Public ReadOnly Property AdaptiveFallThreshold(ByVal current_time As DateTime) As Double
        Get
            Dim current_minutes As Integer = current_time.TimeOfDay.TotalSeconds
            Dim yester_price As UInt32 = LinkedSymbol.GetYesterPrice
            Dim start_price_yester_rate As Double
            If yester_price = 0 Then
                start_price_yester_rate = 0
            Else
                start_price_yester_rate = _FallingStartPrice / yester_price - 1
            End If
            Dim yester_addition As Double
            If start_price_yester_rate > 0.28 Then
                yester_addition = _YESTER_ADDITION_28P
            ElseIf start_price_yester_rate > 0.26 Then
                yester_addition = _YESTER_ADDITION_26P
            ElseIf start_price_yester_rate > 0.24 Then
                yester_addition = _YESTER_ADDITION_24P
            ElseIf start_price_yester_rate > 0.22 Then
                yester_addition = _YESTER_ADDITION_22P
            ElseIf start_price_yester_rate > 0.2 Then
                yester_addition = _YESTER_ADDITION_20P
            ElseIf start_price_yester_rate > 0.18 Then
                yester_addition = _YESTER_ADDITION_18P
            ElseIf start_price_yester_rate > 0.16 Then
                yester_addition = _YESTER_ADDITION_16P
            ElseIf start_price_yester_rate > 0.14 Then
                yester_addition = _YESTER_ADDITION_14P
            ElseIf start_price_yester_rate > 0.12 Then
                yester_addition = _YESTER_ADDITION_12P
            ElseIf start_price_yester_rate > 0.1 Then
                yester_addition = _YESTER_ADDITION_10P
            ElseIf start_price_yester_rate > 0.08 Then
                yester_addition = _YESTER_ADDITION_8P
            ElseIf start_price_yester_rate > 0.06 Then
                yester_addition = _YESTER_ADDITION_6P
            ElseIf start_price_yester_rate > 0.04 Then
                yester_addition = _YESTER_ADDITION_4P
            ElseIf start_price_yester_rate > 0.02 Then
                yester_addition = _YESTER_ADDITION_2P
            ElseIf start_price_yester_rate > 0 Then
                yester_addition = _YESTER_ADDITION_0
            ElseIf start_price_yester_rate > -0.02 Then
                yester_addition = _YESTER_ADDITION_2N
            ElseIf start_price_yester_rate > -0.04 Then
                yester_addition = _YESTER_ADDITION_4N
            ElseIf start_price_yester_rate > -0.06 Then
                yester_addition = _YESTER_ADDITION_6N
            ElseIf start_price_yester_rate > -0.08 Then
                yester_addition = _YESTER_ADDITION_8N
            ElseIf start_price_yester_rate > -0.1 Then
                yester_addition = _YESTER_ADDITION_10N
            ElseIf start_price_yester_rate > -0.12 Then
                yester_addition = _YESTER_ADDITION_12N
            ElseIf start_price_yester_rate > -0.14 Then
                yester_addition = _YESTER_ADDITION_14N
            ElseIf start_price_yester_rate > -0.16 Then
                yester_addition = _YESTER_ADDITION_16N
            ElseIf start_price_yester_rate > -0.18 Then
                yester_addition = _YESTER_ADDITION_18N
            ElseIf start_price_yester_rate > -0.2 Then
                yester_addition = _YESTER_ADDITION_20N
            ElseIf start_price_yester_rate > -0.22 Then
                yester_addition = _YESTER_ADDITION_22N
            ElseIf start_price_yester_rate > -0.24 Then
                yester_addition = _YESTER_ADDITION_24N
            ElseIf start_price_yester_rate > -0.26 Then
                yester_addition = _YESTER_ADDITION_26N
            ElseIf start_price_yester_rate > -0.28 Then
                yester_addition = _YESTER_ADDITION_28N
            Else 'If start_price_yester_rate > -0.3 Then
                yester_addition = _YESTER_ADDITION_30N
            End If

            If current_minutes < MarketStartTime * 3600 + 193 Then
                Return _FALL_PERCENT_THRESHOLD + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 6 * 60 Then
                Return _FALL_PERCENT_THRESHOLD1 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 30 * 60 Then
                Return _FALL_PERCENT_THRESHOLD2 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 60 * 60 Then
                Return _FALL_PERCENT_THRESHOLD3 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 90 * 60 Then
                Return _FALL_PERCENT_THRESHOLD4 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 150 * 60 Then
                Return _FALL_PERCENT_THRESHOLD5 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 180 * 60 Then
                Return _FALL_PERCENT_THRESHOLD6 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 240 * 60 Then
                Return _FALL_PERCENT_THRESHOLD7 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 270 * 60 Then
                Return _FALL_PERCENT_THRESHOLD8 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 310 * 60 Then
                Return _FALL_PERCENT_THRESHOLD9 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 330 * 60 Then
                Return _FALL_PERCENT_THRESHOLD10 + yester_addition
            Else 'If current_minutes < MarketStartTime * 60 + 240 Then    '14:28 ~ 
                Return _FALL_PERCENT_THRESHOLD11 + yester_addition
            End If
        End Get
    End Property

    '새 데이터 도착
    '131111 : gangdo의 최종 사용처 MiniSelSum의 대체작업이 끝났다. 이제 gangdo와 관련된 모든 쓸모없는 로직을 지워버리자.
    Public Overrides Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
        Dim unidelta_str As UnideltaStructure
        Dim time_over_clearing As Boolean = False
        'Dim diff As Int64

        unidelta_str.Price = a_data.Price         '주가 저장
        unidelta_str.MAPrice = a_data.MAPrice     '이동평균 값 저장
        unidelta_str.Amount = a_data.Amount         '거래량 저장
        If RecordList.Count > 0 Then
            '델타 계산해서 저장
            'diff = CType(a_data.BuyAmount, Int64) - CType(_BuyAmountOld, Int64)
            'If diff < 0 Then
            '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
            'unidelta_str.BuyDelta = 0
            'Else
            '정상적인 델타매수량
            'unidelta_str.BuyDelta = diff
            'End If
            'diff = CType(a_data.SelAmount, Int64) - CType(_SelAmountOld, Int64)
            'If diff < 0 Then
            '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
            'unidelta_str.SelDelta = 0
            'Else
            '정상적인 델타매수량
            'unidelta_str.SelDelta = diff
            'End If
            '매도/매수 미니섬 계산
            'If RecordList.Count > 0 Then
            'unidelta_str.BuyMiniSum = RecordList.Last.BuyMiniSum + unidelta_str.BuyDelta
            'unidelta_str.SelMiniSum = RecordList.Last.SelMiniSum + unidelta_str.SelDelta
            'Else
            'unidelta_str.BuyMiniSum = unidelta_str.BuyDelta
            'unidelta_str.SelMiniSum = unidelta_str.SelDelta
            'End If
        Else
            'record list 없음
            unidelta_str.BuyDelta = 0
            unidelta_str.SelDelta = 0
            'unidelta_str.BuyMiniSum = 0
            'unidelta_str.SelMiniSum = 0
        End If
        'uni-delta  계산
        'Dim delta_unidelta As Double = 0
        'delta_unidelta = CType(unidelta_str.BuyDelta, Double) - unidelta_str.SelDelta
        'For index As Integer = 1 To _DELTA_CURVE.Length
        'If RecordList.Count - index >= 0 Then
        'delta_unidelta += _DELTA_CURVE(index - 1) * (CType(RecordList(RecordList.Count - index).BuyDelta, Double) - RecordList(RecordList.Count - index).SelDelta)
        'End If
        'Next
        'If RecordList.Count > 0 Then
        'unidelta_str.Unidelta = RecordList(RecordList.Count - 1).Unidelta + delta_unidelta
        'Else
        'unidelta_str.Unidelta = delta_unidelta
        'End If

        'old amount값을 저장해둔다.
        '_BuyAmountOld = a_data.BuyAmount
        '_SelAmountOld = a_data.SelAmount

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드

            'unidelta min,max 계산
            '_MinUnidelta = Math.Min(_MinUnidelta, unidelta_str.Unidelta)
            '_MaxUnidelta = Math.Max(_MaxUnidelta, unidelta_str.Unidelta)

            If RecordList.Count > 0 AndAlso unidelta_str.MAPrice < RecordList.Last.MAPrice Then
                '이동평균가가 하락하고 있다
                If _FallingStartPrice = 0 Then
                    '하락폭 계산을 위한 하락 시작가격 기록
                    _FallingStartPrice = RecordList.Last.Price
                    '하락 스케일 계산을 위한 하락 시작 거래량 기록
                    _FallingStartAmount = RecordList.Last.Amount
                End If
                _SellSuperiorCount += 1
                _BuyAllowCount = 0

                If _CurrentPhase <> SearchPhase.PRETHRESHOLDED Then
                    If ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / CType(_FallingStartPrice, Double) > _FALL_PRE_THRESHOLD) AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME11 * 5) Then
                        _CurrentPhase = SearchPhase.PRETHRESHOLDED
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 진입 time
                        '140109 : 새로운 변수 PrePrice set, clear 조건 잘 관리하자. 그리고 갖다 쓰는 데로 이제 넘어가자.
                        '140109 : PrePrice를 어디다 쓰는가? PrePrice 밑으로 가격이 내려왔을 때 돈 때려박는 데는 EnterPrice다!
                        '160126: FixedFallThreshold 결정시점은 Prethreshold 걸렸을 때가 아니고 WAIT_FALLING 시작할 때, 즉 처음 모니터링을 시작할 때로 변경했다. FixedFallThreshold가 Prethreshold 보다 작은 경우 PRETHRESHOLD 단계를 안 거치고 바로 WAIT_EXITING으로 들어가는 case 가 있기 때문이다.
                        FixedFallThreshold = AdaptiveFallThreshold(current_time)
                        EnterPrice = Convert.ToInt32(_FallingStartPrice * (1 - FixedFallThreshold))
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 기록
                        'LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청

                        '140529 : pre thresold를 거치지 않고 바로 WAIT EXIT TIME으로 가는 경우... 이 경우 주문 안 하나? => 이런 경우는 없게 만들어놨다. 우선 반드시 pre threshold를 한 번 거치게 해 놨다. 따라서 뚝 떨어졌다가 바로 FALL PRECENT THRESHOLD 이상으로 올라가는 놈들은 검출이 안 될 수도 있다.
                    ElseIf (_CurrentPhase = SearchPhase.PRETHRESHOLDED) And (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / CType(_FallingStartPrice, Double) > AdaptiveFallThreshold(current_time)) AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME11 * 5) Then
                        '이만큼 떨어졌으면 언제 반등할지 눈여겨봐야한다.
                        '                    If 1 Then
                        '이미 반등조건이 갖춰졌다면 바로 진입한다
                        '131016: 바닥가나 진입가 이런 것들을 어떻게 정의할까 생각했는데 실제 가격이 아닌 떨어지기 시작한 점 대비
                        'THRESHOLD만큼 떨어진 가격으로 정하는 게 맞는 것 같다. 실제로도 대부분 그 가격으로 거래가 이루어질 테니까
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                        StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
                        'EnterPrice = a_data.Price                  '진입가
                        _BasePrice = unidelta_str.Price             '바닥가 기록
                        'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                        'Else
                        '반등기다리기 모드로 변경
                        '_CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
                        '_BasePrice = unidelta_str.Price             '바닥가 기록
                        'End If
                        _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                        'ElseIf StockOperator = True AndAlso (RecordList.Count - _RecordCountForPreThreshold) >= 2 Then
                        'pre threshold에서 wait falling으로 돌아오고 나서 2 이상의 시간이 흘렀다.=>주문 취소
                        '청산여부 혹은 진입 취소 여부 판단
                        'If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                        '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                        'StockOperator = Nothing
                        'Else
                        '140305 : 아래처럼 청산 코드를 써야 하는지 Cancel 코드를 써야 하는지 감을 잡아보자.
#If 0 Then
                        If StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                            If (StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) Then ' AndAlso ExitPrice <> 0 Then
                                '140310 : 올라와 있는 매수주문들은 취소하고 ㄴ본 decision maker는 wait exit 상태로 바꾸는 것이 맞는 것 같다.
                                '140323 :여기서 해야될 것을 잘 생각해야 된다. BoughtAmount는 Operator 단에서 전부 종료되어야 업데이트되는 변수이다. 일부체결된 수량은 Operator만 알고 있다. 따라서 Liquidate을 부르는 것이 낫지 않을까. Liquidate을 부르는 것이 아무래도 낫다. decision maker의 상태는 나중에 바뀌는 것으로 해야 될 것 같다.
                                '140325 : 방향을 바꾼다. 원칙적으로 ORDER_REQUESTED이면서 체결된 수량이 있는 상태는 없게 해야 한다. 이것은 종목의 실시간 호가데이터가 pre threshold 가격에 한 번이라도 머물렀던 적이 있는지로 확인하도록 한다.
                                '140325 : 그러면 여기서 해야할 것은 Liquidate을 통해 올라온 주문을 모두 취소하는 것이고, 취소 직전에 체결된 것들은 어쩔 수 없이 곧바로 청산절차를 밟도록 한다. 이런 경우가 흔하지는 않을 것으로 예상된다. decision maker에서 Liquidate 콜하는 부분을 참고하도록 한다.
                                '140527 : 두 달동안의 구현으로 인해 이미 매수주문이 일부 체결되었으면 상태가 WAIT_EXIT_TIME으로 변하게 되었다.
                                'If StockOperator.BoughtAmount > 0 Then
                                MessageLogging(LinkedSymbol.Code & " :Prethresholded에서 waitfalling으로 돌아오고나서.. 일로는 아무래도 안 들어올 것 같다~")
                                StockOperator.Liquidate(False)       '청산 주문
                                'End If
                            Else
                                ErrorLogging(LinkedSymbol.Code & " :PreThreshold위로 올라갔는데 주문한 게 어떻게 된건지...")
                            End If
                        Else
                            ErrorLogging(LinkedSymbol.Code & " :Exit 상태라면 여기서 조작해선 안되고 다른데서 분명 할 것이다.")
                        End If
#End If
                    End If
                Else
                    If ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / unidelta_str.Price <= _FALL_PRE_THRESHOLD) AndAlso (RecordList.Count - _RecordCountForPreThreshold) >= 2 Then
                        '가격이 다시 pre threshold 레벨 이상으로 올라감
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                        'FixedFallThreshold = AdaptiveFallThreshold(current_time)
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                        'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                        'MessageLogging(LinkedSymbol.Code & " :가격이 다시 pre threshold 레벨 이상으로 올라감")
                    ElseIf (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And EnterPrice >= unidelta_str.Price AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME11 * 5) Then
                        '140618, 이걸 안 하다니 위에서 복사해 왔다..
                        If MULTIPLE_DECIDER Then
                            If LinkedSymbol.AlreadyHooked Then
                                '이미 다른 decider가 가져갔으니 초기화시킨다.
                                _CurrentPhase = SearchPhase.WAIT_FALLING
                                current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                                'FixedFallThreshold = AdaptiveFallThreshold(current_time)
                                _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                                'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                                _SellSuperiorCount = 0
                                _BuyAllowCount = 0
                                _BuyAppearanceCount = 0
                                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                                RecordList.Clear()      '레코드 리스트 클리어
                                'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                                'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                                _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                                _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                                _FallingStartPrice = 0              '하락 시작가격 초기화
                                _FallingStartAmount = 0             '하락 시작거래량 초기화
                                _BasePrice = 0                      '바닥가 초기화
                            Else
                                '아무도 안 가져갔으니 이게 가져간다.
                                LinkedSymbol.AlreadyHooked = True
                                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                                EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                                StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
                                'EnterPrice = a_data.Price                  '진입가
                                _BasePrice = unidelta_str.Price             '바닥가 기록
                                'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                                FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                                _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                            End If
                        Else
                            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                            EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                            StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
                            'EnterPrice = a_data.Price                  '진입가
                            _BasePrice = unidelta_str.Price             '바닥가 기록
                            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                            FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                            _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                        End If
                    Else
                        '계속 Prethreshold 상태를 유지함
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                    End If
                    '131122:prethresholded 되고나서 뭔가 hysteresis 작업이 필요할 것 같기도 해다. => pre threshold된 시간에서 2 * 5초 동안은 유지되게 해놨다.
                End If

            Else
                '하락하지 않았음
                If _BuyAllowCount = 0 Then
                    '새로운 상승이 시작되고 있음
                    _BuyAppearanceCount += 1
                End If

                _BuyAllowCount += 1
                _SellSuperiorCount += 1         '이것도 하나의 상승으로 본다.

                If (_BuyAllowCount > _BUY_ALLOW_THRESHOLD) Or (_BuyAppearanceCount > _BUY_APPEARANCE_THRESHOLD) Then
                    '이미 너무 많은 상승...=> 하락 처음부터 다시 기다림
                    If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                        'FixedFallThreshold = AdaptiveFallThreshold(current_time)
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                        'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                        'MessageLogging(LinkedSymbol.Code & " :가격이 다시 많이 올라가면서 pre threshold 중단")
                    End If
                    _SellSuperiorCount = 0
                    _BuyAllowCount = 0
                    _BuyAppearanceCount = 0
                    StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                    RecordList.Clear()      '레코드 리스트 클리어
                    'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                    'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                    _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                    _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                    _FallingStartPrice = 0              '하락 시작가격 초기화
                    _FallingStartAmount = 0             '하락 시작거래량 초기화
                    _BasePrice = 0                      '바닥가 초기화
                End If
            End If
            'Unwanted rising 을 detect한다
            If RecordListForFallback.Count > 0 AndAlso unidelta_str.MAPrice > RecordListForFallback.Last.MAPrice Then
                '이동평균가가 상승하고 있다
                If _RisingStartPrice = 0 Then
                    '상승폭 계산을 위한 상승 시작가격 기록
                    _RisingStartPrice = RecordListForFallback.Last.Price
                End If
                _BuySuperiorCount += 1
                _SellAllowCount = 0

                If (_BuySuperiorCount > _BUY_SUPERIOR_THRESHOLD) And ((CType(unidelta_str.Price, Double) - _RisingStartPrice) / unidelta_str.Price > _RISE_PERCENT_THRESHOLD) Then
                    '이만큼 올라갔으면 언제 반락할지 눈여겨봐야한다.
                    '반락기다리기 모드로 변경(Unwanted Rising Detected)
                    'MessageLogging(LinkedSymbol.Code & " :" & "겁나 상승해서 반락기다리기 모드됨. 이런 거 사면 안 된다.")
                    _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED     '반락기다리기 모드로 변경
                    _TopPrice = unidelta_str.Price             '최고가 기록
                    _RiseHeight = unidelta_str.Price - _RisingStartPrice            '하락폭 기록
                End If
            Else
                '상승하지 않았음
                If _SellAllowCount = 0 Then
                    '새로운 하락이 시작되고 있음
                    _SellAppearanceCount += 1
                End If

                _SellAllowCount += 1
                _BuySuperiorCount += 1         '이것도 하나의 상승으로 본다.

                If (_SellAllowCount > _SELL_ALLOW_THRESHOLD) Or (_SellAppearanceCount > _SELL_APPEARANCE_THRESHOLD) Then
                    '이미 너무 많은 하락...=> 상승 처음부터 다시 기다림
                    _BuySuperiorCount = 0
                    _SellAllowCount = 0
                    _SellAppearanceCount = 0
                    _RisingStartPrice = 0              '상승 시작가격 초기화
                    _TopPrice = 0                      '최고가 초기화
                    RecordListForFallback.Clear()      '레코드 리스트 클리어
                End If
            End If
        ElseIf _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED Then
            '반락 기다리기 모드
            _TopPrice = Math.Max(_TopPrice, unidelta_str.Price)             '최고가 업데이트
            _RiseHeight = _TopPrice - _RisingStartPrice                     '상승폭 업데이트
            If (CType(_TopPrice, Double) - unidelta_str.Price) / _RiseHeight > _FALLBACK_PERCENT_THRESHOLD Then
                '상승폭의 많은 부분이 반락되었다. => 일반 falling 기다리기 모드로 전환
                _BuySuperiorCount = 0
                _SellAllowCount = 0
                _SellAppearanceCount = 0
                _RisingStartPrice = 0              '상승 시작가격 초기화
                _TopPrice = 0                      '최고가 초기화

                _SellSuperiorCount = 0
                _BuyAllowCount = 0
                _BuyAppearanceCount = 0
                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                RecordList.Clear()      '레코드 리스트 클리어
                'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                _FallingStartPrice = 0              '하락 시작가격 초기화
                _FallingStartAmount = 0             '하락 시작거래량 초기화
                _BasePrice = 0                      '바닥가 초기화
                _CurrentPhase = SearchPhase.WAIT_FALLING
                current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                'FixedFallThreshold = AdaptiveFallThreshold(current_time)
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _WaitExitCount += 1         '청산기다림 카운트
            '청산 기다리기 모드
            _BasePrice = Math.Min(_BasePrice, unidelta_str.Price)           '바닥가 업데이트
            _FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            If (_WaitExitCount >= StoredHavingTime) Then
                '그냥 때가 되었다.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    _SecondChance = True
                End If
            ElseIf (CType(unidelta_str.Price, Double) - _BasePrice) / _FallHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
            ElseIf ((CType(unidelta_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    _SecondChance = True
                End If
            End If
        End If

        RecordList.Add(unidelta_str)          '하나의 record완성
        RecordListForFallback.Add(unidelta_str)          '하나의 record (for detecting fallback) 완성

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
#If 0 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator = False Then
#If 1 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                'Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / ((LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                'If safe_rate > 1 Then
                'safe_rate = 1
                'ElseIf safe_rate < 0 Then
                'safe_rate = 0
                'End If
                'Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                'Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                'possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                'Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                'Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                'If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                'final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                'End If
                'MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                'If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                'StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                'StockOperator.SetAccountManager(MainForm.AccountManager)
                'Else
                '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                'EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                'MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                'End If
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

    'symbol 객체가 호가 update되었다고 신호보내옴
    Public Sub CallPriceUpdated()
        'If StockOperator IsNot Nothing Then
        'If StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
        'If (StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) Then
        'If _CurrentPhase = SearchPhase.PRETHRESHOLDED Or _CurrentPhase = SearchPhase.WAIT_FALLING Then
        '140328 : pre-threshold가 들어가면서 _SELL_SUPERIOR_THRESHOLD가 유명무실해지고 NoMoreEnteringTime도 제구실을 못하게 생겼다. 대책강구가 필요하다.
        '140402 : _SELL_SUPERIOR_THRESHOLD에 따른 변화는 다행히 크지 않아 보인다. 그리고 NoMoreEnteringTime은 pre threshold 진입때 하기 때문에 괜찮다.
        '140405 : 실시간 호가에 의한 prethrehold-> wait_exit 상태변경은 불가능하다. 왜냐면 호가는 체결을 말해주지 않기 때문이다. 따라서 체결에 의한 상태변경을 생각해야 한다.
        'End If
        'End If
        'End If
        'End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub BuyingInitiated()
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        'If _CurrentPhase = SearchPhase.WAIT_FALLING OrElse _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
            EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
            StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
            'EnterPrice = a_data.Price                  '진입가
            _BasePrice = EnterPrice             '바닥가 기록
            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
            'FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

            'Else
            '반등기다리기 모드로 변경
            '_CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
            '_BasePrice = unidelta_str.Price             '바닥가 기록
            'End If
            _FallHeight = _FallingStartPrice - EnterPrice           '하락폭 기록
        End If
    End Sub
    '[OneKey] -----------------------------------------------------------------------------------------------------------------┘

    '    SafeEnter(_CurrentPhaseKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
    Public Overrides Sub ClearNow(ByVal current_price As UInt32)
        If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub
    '    SafeLeave(_CurrentPhaseKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

    Public Overrides Sub CreateGraphicData()
        'Public PricePointList, MAPointList, BuyMiniSumPointList, SelMiniSumPointList, RatePointList, DecideTimePointList As PointList
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            a_point.Y = RecordList(index).MAPrice
            MAPointList.Add(a_point)                                '이동평균 그래프자료 만들기
            'a_point.Y = RecordList(index).BuyMiniSum
            'BuyMiniSumPointList.Add(a_point)                        'BuyMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).SelMiniSum
            'SelMiniSumPointList.Add(a_point)                        'SelMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).Unidelta
            'UnideltaPointList.Add(a_point)                          'Unidelta 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        DecideTimePointList.Add(New PointF(StartTime.TimeOfDay.TotalSeconds, 1))                '시작시간
        DecideTimePointList.Add(New PointF(StartTime.TimeOfDay.TotalSeconds + 0.001, 0))        '시작시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 1))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 0))                 '청산시간
    End Sub


    'old decision maker로부터 second chance information을 빼냄
    Public Overrides Sub GetSecondChanceInformation(ByVal old_decision_maker As c050_DecisionMaker)

    End Sub
End Class
#End If

#If 0 Then '160519: 패턴 인식 위해 일단 보류
Public Class c05D_DoubleFall_DecisionMaker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure DoubleFallStructure
        Dim Price As UInt32
        'Dim MAPrice As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        'Dim BuyDelta As UInt64      '델타 매수거래량
        'Dim SelDelta As UInt64      '델타 매도거래량
        Dim SlopeList As List(Of Double)            '기울기 리스트
        Dim DifferianceList As List(Of Double)          '이향률 리스트
    End Structure

    '써치 페이즈
    Public PAST_DEPTH As Integer = 30
    Public _SELL_SUPERIOR_THRESHOLD As Integer = 15
    Public _BUY_SUPERIOR_THRESHOLD As Integer = 24
    Public _BUY_ALLOW_THRESHOLD As Integer = 12
    Public _SELL_ALLOW_THRESHOLD As Integer = 8
    Public _BUY_APPEARANCE_THRESHOLD As Integer = 14
    Public _SELL_APPEARANCE_THRESHOLD As Integer = 15
    Public _FALL_PRE_THRESHOLD As Double = 0.03
    Public _FALL_PERCENT_THRESHOLD As Double = 0.29     '09:00 ~ 09:03
    Public _FALL_PERCENT_THRESHOLD1 As Double = 0.085   '09:03 ~ 09:06
    Public _FALL_PERCENT_THRESHOLD2 As Double = 0.069   '09:06 ~ 09:30
    Public _FALL_PERCENT_THRESHOLD3 As Double = 0.111    '09:30 ~ 10:00
    Public _FALL_PERCENT_THRESHOLD4 As Double = 0.133   '10:00 ~ 10:30
    Public _FALL_PERCENT_THRESHOLD5 As Double = 0.071   '10:30 ~ 11:30
    Public _FALL_PERCENT_THRESHOLD6 As Double = 0.119   '11:30 ~ 12:00
    Public _FALL_PERCENT_THRESHOLD7 As Double = 0.093   '12:00 ~ 13:00
    Public _FALL_PERCENT_THRESHOLD8 As Double = 0.106   '13:00 ~ 13:30
    Public _FALL_PERCENT_THRESHOLD9 As Double = 0.059   '13:30 ~ 14:10
    Public _FALL_PERCENT_THRESHOLD10 As Double = 0.079    '14:10 ~ 14:30
    Public _FALL_PERCENT_THRESHOLD11 As Double = 0.07   '14:30 ~
    Public _RISE_PERCENT_THRESHOLD As Double = 0.057
    Public _FALLBACK_PERCENT_THRESHOLD As Double = 0.37
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 0.86
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.2
    Public _DEFAULT_HAVING_TIME As Integer = 85
    Public _SECOND_CHANCE_THRESHOLD_TIME As Integer = 0
    Public _SECOND_CHANCE_FACTOR As Double = 1.13
    Public _YESTER_ADDITION_28P As Double = 0.01
    Public _YESTER_ADDITION_26P As Double = 0.015
    Public _YESTER_ADDITION_24P As Double = 0.019
    Public _YESTER_ADDITION_22P As Double = 0.022
    Public _YESTER_ADDITION_20P As Double = 0.022
    Public _YESTER_ADDITION_18P As Double = 0.018
    Public _YESTER_ADDITION_16P As Double = 0.013
    Public _YESTER_ADDITION_14P As Double = 0.009
    Public _YESTER_ADDITION_12P As Double = 0.006
    Public _YESTER_ADDITION_10P As Double = 0.004
    Public _YESTER_ADDITION_8P As Double = 0.002
    Public _YESTER_ADDITION_6P As Double = 0.001
    Public _YESTER_ADDITION_4P As Double = -0.001
    Public _YESTER_ADDITION_2P As Double = -0.001
    Public _YESTER_ADDITION_0 As Double = 0.001
    Public _YESTER_ADDITION_2N As Double = 0
    Public _YESTER_ADDITION_4N As Double = -0.002
    Public _YESTER_ADDITION_6N As Double = -0.004
    Public _YESTER_ADDITION_8N As Double = -0.006
    Public _YESTER_ADDITION_10N As Double = -0.007
    Public _YESTER_ADDITION_12N As Double = -0.008
    Public _YESTER_ADDITION_14N As Double = -0.007
    Public _YESTER_ADDITION_16N As Double = -0.005
    Public _YESTER_ADDITION_18N As Double = 0
    Public _YESTER_ADDITION_20N As Double = 0.011
    Public _YESTER_ADDITION_22N As Double = 0.015
    Public _YESTER_ADDITION_24N As Double = 0.013
    Public _YESTER_ADDITION_26N As Double = 0.003
    Public _YESTER_ADDITION_28N As Double = -0.008
    Public _YESTER_ADDITION_30N As Double = -0.02

    Private RecordList As New List(Of DoubleFallStructure)
    Private _SellSuperiorCount As Integer
    Private _BuySuperiorCount As Integer
    Private _BuyAllowCount As Integer = 0
    Private _SellAllowCount As Integer = 0
    Private _BuyAppearanceCount As Integer = 0
    Private _SellAppearanceCount As Integer = 0
    '    Private _UnideltaSum As Double = 0
    '    Private _UnideltaCount As Integer = 0
    '    Private _UnideltaAve As Double
    Private _MaxUnidelta As Double = [Double].MinValue
    Private _MinUnidelta As Double = [Double].MaxValue
    Private _WaitExitCount As Integer = 0
    Public _FallingStartPrice As UInt32 = 0
    Private _RisingStartPrice As UInt32 = 0
    Private _FallingStartAmount As UInt32 = 0
    Private _FallHeight As UInt32 = 0
    Private _RiseHeight As UInt32 = 0
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _RecordCountForPreThreshold As Integer = 0
    'Public PricePointList, MAPointList, BuyMiniSumPointList, SelMiniSumPointList, UnideltaPointList, DecideTimePointList As PointList
    Private PricePointList, MAPointList, DecideTimePointList As PointList
    '    Public PriceCompositeData, MACompositeData, BuyMiniSumCompositeData, SelMiniSumCompositeData, UnideltaCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private PriceCompositeData, MACompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Public NoMoreOperation As Boolean = False
    Private FixedFallThreshold As Double

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)
        _FALLBACK_PERCENT_THRESHOLD = TestArray(TestIndex)

        _DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '이평가(MAPrice) CompositeData
        'x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("이평가", DataType.REAL_NUMBER_DATA, Nothing)
        'MACompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "이평가")
        'MAPointList = New PointList()
        'MACompositeData.SetData(MAPointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        'GraphicCompositeDataList.Add(MACompositeData)
        'GraphicCompositeDataList.Add(BuyMiniSumCompositeData)
        'GraphicCompositeDataList.Add(SelMiniSumCompositeData)
        'GraphicCompositeDataList.Add(UnideltaCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)

    End Sub

    Public ReadOnly Property AdaptiveFallThreshold(ByVal current_time As DateTime) As Double
        Get
            Dim current_minutes As Integer = current_time.TimeOfDay.TotalSeconds
            Dim yester_price As UInt32 = LinkedSymbol.GetYesterPrice
            Dim start_price_yester_rate As Double
            If yester_price = 0 Then
                start_price_yester_rate = 0
            Else
                start_price_yester_rate = _FallingStartPrice / yester_price - 1
            End If
            Dim yester_addition As Double
            If start_price_yester_rate > 0.28 Then
                yester_addition = _YESTER_ADDITION_28P
            ElseIf start_price_yester_rate > 0.26 Then
                yester_addition = _YESTER_ADDITION_26P
            ElseIf start_price_yester_rate > 0.24 Then
                yester_addition = _YESTER_ADDITION_24P
            ElseIf start_price_yester_rate > 0.22 Then
                yester_addition = _YESTER_ADDITION_22P
            ElseIf start_price_yester_rate > 0.2 Then
                yester_addition = _YESTER_ADDITION_20P
            ElseIf start_price_yester_rate > 0.18 Then
                yester_addition = _YESTER_ADDITION_18P
            ElseIf start_price_yester_rate > 0.16 Then
                yester_addition = _YESTER_ADDITION_16P
            ElseIf start_price_yester_rate > 0.14 Then
                yester_addition = _YESTER_ADDITION_14P
            ElseIf start_price_yester_rate > 0.12 Then
                yester_addition = _YESTER_ADDITION_12P
            ElseIf start_price_yester_rate > 0.1 Then
                yester_addition = _YESTER_ADDITION_10P
            ElseIf start_price_yester_rate > 0.08 Then
                yester_addition = _YESTER_ADDITION_8P
            ElseIf start_price_yester_rate > 0.06 Then
                yester_addition = _YESTER_ADDITION_6P
            ElseIf start_price_yester_rate > 0.04 Then
                yester_addition = _YESTER_ADDITION_4P
            ElseIf start_price_yester_rate > 0.02 Then
                yester_addition = _YESTER_ADDITION_2P
            ElseIf start_price_yester_rate > 0 Then
                yester_addition = _YESTER_ADDITION_0
            ElseIf start_price_yester_rate > -0.02 Then
                yester_addition = _YESTER_ADDITION_2N
            ElseIf start_price_yester_rate > -0.04 Then
                yester_addition = _YESTER_ADDITION_4N
            ElseIf start_price_yester_rate > -0.06 Then
                yester_addition = _YESTER_ADDITION_6N
            ElseIf start_price_yester_rate > -0.08 Then
                yester_addition = _YESTER_ADDITION_8N
            ElseIf start_price_yester_rate > -0.1 Then
                yester_addition = _YESTER_ADDITION_10N
            ElseIf start_price_yester_rate > -0.12 Then
                yester_addition = _YESTER_ADDITION_12N
            ElseIf start_price_yester_rate > -0.14 Then
                yester_addition = _YESTER_ADDITION_14N
            ElseIf start_price_yester_rate > -0.16 Then
                yester_addition = _YESTER_ADDITION_16N
            ElseIf start_price_yester_rate > -0.18 Then
                yester_addition = _YESTER_ADDITION_18N
            ElseIf start_price_yester_rate > -0.2 Then
                yester_addition = _YESTER_ADDITION_20N
            ElseIf start_price_yester_rate > -0.22 Then
                yester_addition = _YESTER_ADDITION_22N
            ElseIf start_price_yester_rate > -0.24 Then
                yester_addition = _YESTER_ADDITION_24N
            ElseIf start_price_yester_rate > -0.26 Then
                yester_addition = _YESTER_ADDITION_26N
            ElseIf start_price_yester_rate > -0.28 Then
                yester_addition = _YESTER_ADDITION_28N
            Else 'If start_price_yester_rate > -0.3 Then
                yester_addition = _YESTER_ADDITION_30N
            End If

            If current_minutes < MarketStartTime * 3600 + 193 Then
                Return _FALL_PERCENT_THRESHOLD + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 6 * 60 Then
                Return _FALL_PERCENT_THRESHOLD1 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 30 * 60 Then
                Return _FALL_PERCENT_THRESHOLD2 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 60 * 60 Then
                Return _FALL_PERCENT_THRESHOLD3 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 90 * 60 Then
                Return _FALL_PERCENT_THRESHOLD4 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 150 * 60 Then
                Return _FALL_PERCENT_THRESHOLD5 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 180 * 60 Then
                Return _FALL_PERCENT_THRESHOLD6 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 240 * 60 Then
                Return _FALL_PERCENT_THRESHOLD7 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 270 * 60 Then
                Return _FALL_PERCENT_THRESHOLD8 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 310 * 60 Then
                Return _FALL_PERCENT_THRESHOLD9 + yester_addition
            ElseIf current_minutes < MarketStartTime * 3600 + 330 * 60 Then
                Return _FALL_PERCENT_THRESHOLD10 + yester_addition
            Else 'If current_minutes < MarketStartTime * 60 + 240 Then    '14:28 ~ 
                Return _FALL_PERCENT_THRESHOLD11 + yester_addition
            End If
        End Get
    End Property

    '새 데이터 도착
    Public Overrides Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
        Dim double_fall_str As DoubleFallStructure
        Dim time_over_clearing As Boolean = False
        Dim temp_sum As Double = 0
        'Dim diff As Int64

        double_fall_str.Price = a_data.Price         '주가 저장
        double_fall_str.Amount = a_data.Amount         '거래량 저장
        double_fall_str.SlopeList = New List(Of Double)
        double_fall_str.DifferianceList = New List(Of Double)

        '160414: DoubleFallStructure에 기울기 arrary를 계산해 저장하는 로직을 짜라.
        For n_minus_j As Integer = RecordList.Count - 1 To Math.Max(0, RecordList.Count - PAST_DEPTH) Step -1
            '160415 : 여기서 하나씩 뒤로 가면서 기울기와 이향률을 계산함.
            double_fall_str.SlopeList.Add((double_fall_str.Price - RecordList(n_minus_j).Price) / (RecordList(n_minus_j).Price * (RecordList.Count - n_minus_j)))
            temp_sum = 0
            If RecordList.Count - n_minus_j <> 1 Then
                For i_index As Integer = 1 To RecordList.Count - n_minus_j - 1
                    temp_sum = temp_sum + (i_index / (RecordList.Count - n_minus_j) - (RecordList(n_minus_j).Price - RecordList(n_minus_j + i_index).Price) / (RecordList(n_minus_j).Price - double_fall_str.Price)) ^ 2
                Next
                temp_sum = Math.Sqrt(temp_sum) / (RecordList.Count - n_minus_j - 1)
            Else
                temp_sum = 0
            End If
            double_fall_str.DifferianceList.Add(temp_sum)
        Next

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드

            If RecordList.Count > 0 AndAlso unidelta_str.MAPrice < RecordList.Last.MAPrice Then
                '이동평균가가 하락하고 있다
                If _FallingStartPrice = 0 Then
                    '하락폭 계산을 위한 하락 시작가격 기록
                    _FallingStartPrice = RecordList.Last.Price
                    '하락 스케일 계산을 위한 하락 시작 거래량 기록
                    _FallingStartAmount = RecordList.Last.Amount
                End If
                _SellSuperiorCount += 1
                _BuyAllowCount = 0

                '160513: 현재 기울기 추세로 미래 데이터들을 몇 개 (가장 많은 데이터수의 조건을 계산할 수 있을 만큼) 만들자
                '160513: 그래서 그 미래 데이터를 조건 detector 함수에 넘겨서 조건이 미래에 맞을 가능성을 살펴보자. 맞을 가능성이 있으면 prethreshold를 건다.
                '160513: 조건 detector 함수는 data 길이별 조건만족점수를 계산을 하고 그 점수가 specific threshold 이상이면 돈을 걸 수 있는 조건이라고 판단한다.

                '160513: 정해진 패턴을 스캔하는 프로그램을 만든다. 이 패턴은 세로로 normalize되어 있고 세로 scale은 상관없이 패턴형태만 맞으면 된다.
                '160513: 패턴이 맞으면 화면에 표시한다. 그리고 패턴 이후 추세를 통계내는 것도 만들어보자.

                '160518: 데이터를 하나씩 받으면서 가장 최근 데이터 (패턴 길이수)개 만큼을 저장하는 어레이를 만들어 관리한다.
                '160518: 이 어레이가 패턴과 얼마나 가까운지를 계산해보면 된다. 그러면 패턴 저장 위치 및 최근 데이터 저장위치를 정해보자

                If _CurrentPhase <> SearchPhase.PRETHRESHOLDED Then
                    If ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / unidelta_str.Price > _FALL_PRE_THRESHOLD) AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME * 5) Then
                        _CurrentPhase = SearchPhase.PRETHRESHOLDED
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 진입 time
                        '140109 : 새로운 변수 PrePrice set, clear 조건 잘 관리하자. 그리고 갖다 쓰는 데로 이제 넘어가자.
                        '140109 : PrePrice를 어디다 쓰는가? PrePrice 밑으로 가격이 내려왔을 때 돈 때려박는 데는 EnterPrice다!
                        FixedFallThreshold = AdaptiveFallThreshold(current_time)
                        EnterPrice = Convert.ToInt32(_FallingStartPrice * (1 - FixedFallThreshold))
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 기록
                        'LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청

                        '140529 : pre thresold를 거치지 않고 바로 WAIT EXIT TIME으로 가는 경우... 이 경우 주문 안 하나? => 이런 경우는 없게 만들어놨다. 우선 반드시 pre threshold를 한 번 거치게 해 놨다. 따라서 뚝 떨어졌다가 바로 FALL PRECENT THRESHOLD 이상으로 올라가는 놈들은 검출이 안 될 수도 있다.
                    ElseIf (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / CType(_FallingStartPrice, Double) > AdaptiveFallThreshold(current_time)) AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME * 5) Then
                        '이만큼 떨어졌으면 언제 반등할지 눈여겨봐야한다.
                        '                    If 1 Then
                        '이미 반등조건이 갖춰졌다면 바로 진입한다
                        '131016: 바닥가나 진입가 이런 것들을 어떻게 정의할까 생각했는데 실제 가격이 아닌 떨어지기 시작한 점 대비
                        'THRESHOLD만큼 떨어진 가격으로 정하는 게 맞는 것 같다. 실제로도 대부분 그 가격으로 거래가 이루어질 테니까
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                        'EnterPrice = a_data.Price                  '진입가
                        _BasePrice = unidelta_str.Price             '바닥가 기록
                        'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                        'Else
                        '반등기다리기 모드로 변경
                        '_CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
                        '_BasePrice = unidelta_str.Price             '바닥가 기록
                        'End If
                        _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                        'ElseIf StockOperator = True AndAlso (RecordList.Count - _RecordCountForPreThreshold) >= 2 Then
                        'pre threshold에서 wait falling으로 돌아오고 나서 2 이상의 시간이 흘렀다.=>주문 취소
                        '청산여부 혹은 진입 취소 여부 판단
                        'If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                        '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                        'StockOperator = Nothing
                        'Else
                        '140305 : 아래처럼 청산 코드를 써야 하는지 Cancel 코드를 써야 하는지 감을 잡아보자.
#If 0 Then
                        If StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                            If (StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) Then ' AndAlso ExitPrice <> 0 Then
                                '140310 : 올라와 있는 매수주문들은 취소하고 ㄴ본 decision maker는 wait exit 상태로 바꾸는 것이 맞는 것 같다.
                                '140323 :여기서 해야될 것을 잘 생각해야 된다. BoughtAmount는 Operator 단에서 전부 종료되어야 업데이트되는 변수이다. 일부체결된 수량은 Operator만 알고 있다. 따라서 Liquidate을 부르는 것이 낫지 않을까. Liquidate을 부르는 것이 아무래도 낫다. decision maker의 상태는 나중에 바뀌는 것으로 해야 될 것 같다.
                                '140325 : 방향을 바꾼다. 원칙적으로 ORDER_REQUESTED이면서 체결된 수량이 있는 상태는 없게 해야 한다. 이것은 종목의 실시간 호가데이터가 pre threshold 가격에 한 번이라도 머물렀던 적이 있는지로 확인하도록 한다.
                                '140325 : 그러면 여기서 해야할 것은 Liquidate을 통해 올라온 주문을 모두 취소하는 것이고, 취소 직전에 체결된 것들은 어쩔 수 없이 곧바로 청산절차를 밟도록 한다. 이런 경우가 흔하지는 않을 것으로 예상된다. decision maker에서 Liquidate 콜하는 부분을 참고하도록 한다.
                                '140527 : 두 달동안의 구현으로 인해 이미 매수주문이 일부 체결되었으면 상태가 WAIT_EXIT_TIME으로 변하게 되었다.
                                'If StockOperator.BoughtAmount > 0 Then
                                MessageLogging(LinkedSymbol.Code & " :Prethresholded에서 waitfalling으로 돌아오고나서.. 일로는 아무래도 안 들어올 것 같다~")
                                StockOperator.Liquidate(False)       '청산 주문
                                'End If
                            Else
                                ErrorLogging(LinkedSymbol.Code & " :PreThreshold위로 올라갔는데 주문한 게 어떻게 된건지...")
                            End If
                        Else
                            ErrorLogging(LinkedSymbol.Code & " :Exit 상태라면 여기서 조작해선 안되고 다른데서 분명 할 것이다.")
                        End If
#End If
                    End If
                Else
                    If ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / unidelta_str.Price <= _FALL_PRE_THRESHOLD) AndAlso (RecordList.Count - _RecordCountForPreThreshold) >= 2 Then
                        '가격이 다시 pre threshold 레벨 이상으로 올라감
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                        'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                        'MessageLogging(LinkedSymbol.Code & " :가격이 다시 pre threshold 레벨 이상으로 올라감")
                    ElseIf (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And EnterPrice >= unidelta_str.Price AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME * 5) Then
                        '140618, 이걸 안 하다니 위에서 복사해 왔다..
                        If MULTIPLE_DECIDER Then
                            If LinkedSymbol.AlreadyHooked Then
                                '이미 다른 decider가 가져갔으니 초기화시킨다.
                                _CurrentPhase = SearchPhase.WAIT_FALLING
                                _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                                'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                                _SellSuperiorCount = 0
                                _BuyAllowCount = 0
                                _BuyAppearanceCount = 0
                                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                                RecordList.Clear()      '레코드 리스트 클리어
                                'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                                'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                                _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                                _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                                _FallingStartPrice = 0              '하락 시작가격 초기화
                                _FallingStartAmount = 0             '하락 시작거래량 초기화
                                _BasePrice = 0                      '바닥가 초기화
                            Else
                                '아무도 안 가져갔으니 이게 가져간다.
                                LinkedSymbol.AlreadyHooked = True
                                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                                EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                                'EnterPrice = a_data.Price                  '진입가
                                _BasePrice = unidelta_str.Price             '바닥가 기록
                                'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                                FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                                _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                            End If
                        Else
                            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                            EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                            'EnterPrice = a_data.Price                  '진입가
                            _BasePrice = unidelta_str.Price             '바닥가 기록
                            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                            FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                            _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                        End If
                    Else
                        '계속 Prethreshold 상태를 유지함
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                    End If
                    '131122:prethresholded 되고나서 뭔가 hysteresis 작업이 필요할 것 같기도 해다. => pre threshold된 시간에서 2 * 5초 동안은 유지되게 해놨다.
                End If

            Else
                '하락하지 않았음
                If _BuyAllowCount = 0 Then
                    '새로운 상승이 시작되고 있음
                    _BuyAppearanceCount += 1
                End If

                _BuyAllowCount += 1
                _SellSuperiorCount += 1         '이것도 하나의 상승으로 본다.

                If (_BuyAllowCount > _BUY_ALLOW_THRESHOLD) Or (_BuyAppearanceCount > _BUY_APPEARANCE_THRESHOLD) Then
                    '이미 너무 많은 상승...=> 하락 처음부터 다시 기다림
                    If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                        'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                        'MessageLogging(LinkedSymbol.Code & " :가격이 다시 많이 올라가면서 pre threshold 중단")
                    End If
                    _SellSuperiorCount = 0
                    _BuyAllowCount = 0
                    _BuyAppearanceCount = 0
                    StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                    RecordList.Clear()      '레코드 리스트 클리어
                    'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                    'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                    _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                    _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                    _FallingStartPrice = 0              '하락 시작가격 초기화
                    _FallingStartAmount = 0             '하락 시작거래량 초기화
                    _BasePrice = 0                      '바닥가 초기화
                End If
            End If
            'Unwanted rising 을 detect한다
            If RecordListForFallback.Count > 0 AndAlso unidelta_str.MAPrice > RecordListForFallback.Last.MAPrice Then
                '이동평균가가 상승하고 있다
                If _RisingStartPrice = 0 Then
                    '상승폭 계산을 위한 상승 시작가격 기록
                    _RisingStartPrice = RecordListForFallback.Last.Price
                End If
                _BuySuperiorCount += 1
                _SellAllowCount = 0

                If (_BuySuperiorCount > _BUY_SUPERIOR_THRESHOLD) And ((CType(unidelta_str.Price, Double) - _RisingStartPrice) / unidelta_str.Price > _RISE_PERCENT_THRESHOLD) Then
                    '이만큼 올라갔으면 언제 반락할지 눈여겨봐야한다.
                    '반락기다리기 모드로 변경(Unwanted Rising Detected)
                    'MessageLogging(LinkedSymbol.Code & " :" & "겁나 상승해서 반락기다리기 모드됨. 이런 거 사면 안 된다.")
                    _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED     '반락기다리기 모드로 변경
                    _TopPrice = unidelta_str.Price             '최고가 기록
                    _RiseHeight = unidelta_str.Price - _RisingStartPrice            '하락폭 기록
                End If
            Else
                '상승하지 않았음
                If _SellAllowCount = 0 Then
                    '새로운 하락이 시작되고 있음
                    _SellAppearanceCount += 1
                End If

                _SellAllowCount += 1
                _BuySuperiorCount += 1         '이것도 하나의 상승으로 본다.

                If (_SellAllowCount > _SELL_ALLOW_THRESHOLD) Or (_SellAppearanceCount > _SELL_APPEARANCE_THRESHOLD) Then
                    '이미 너무 많은 하락...=> 상승 처음부터 다시 기다림
                    _BuySuperiorCount = 0
                    _SellAllowCount = 0
                    _SellAppearanceCount = 0
                    _RisingStartPrice = 0              '상승 시작가격 초기화
                    _TopPrice = 0                      '최고가 초기화
                    RecordListForFallback.Clear()      '레코드 리스트 클리어
                End If
            End If
        ElseIf _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED Then
            '반락 기다리기 모드
            _TopPrice = Math.Max(_TopPrice, unidelta_str.Price)             '최고가 업데이트
            _RiseHeight = _TopPrice - _RisingStartPrice                     '상승폭 업데이트
            If (CType(_TopPrice, Double) - unidelta_str.Price) / _RiseHeight > _FALLBACK_PERCENT_THRESHOLD Then
                '상승폭의 많은 부분이 반락되었다. => 일반 falling 기다리기 모드로 전환
                _BuySuperiorCount = 0
                _SellAllowCount = 0
                _SellAppearanceCount = 0
                _RisingStartPrice = 0              '상승 시작가격 초기화
                _TopPrice = 0                      '최고가 초기화

                _SellSuperiorCount = 0
                _BuyAllowCount = 0
                _BuyAppearanceCount = 0
                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                RecordList.Clear()      '레코드 리스트 클리어
                'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                _FallingStartPrice = 0              '하락 시작가격 초기화
                _FallingStartAmount = 0             '하락 시작거래량 초기화
                _BasePrice = 0                      '바닥가 초기화
                _CurrentPhase = SearchPhase.WAIT_FALLING
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _WaitExitCount += 1         '청산기다림 카운트
            '청산 기다리기 모드
            _BasePrice = Math.Min(_BasePrice, unidelta_str.Price)           '바닥가 업데이트
            _FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            If (_WaitExitCount >= _DEFAULT_HAVING_TIME) Then
                '그냥 때가 되었다.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    _SecondChance = True
                End If
            ElseIf (CType(unidelta_str.Price, Double) - _BasePrice) / _FallHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
            ElseIf ((CType(unidelta_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    _SecondChance = True
                End If
            End If
        End If

        RecordList.Add(unidelta_str)          '하나의 record완성
        RecordListForFallback.Add(unidelta_str)          '하나의 record (for detecting fallback) 완성

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
#If 0 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator = False Then
#If 1 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                'Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / ((LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                'If safe_rate > 1 Then
                'safe_rate = 1
                'ElseIf safe_rate < 0 Then
                'safe_rate = 0
                'End If
                'Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                'Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                'possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                'Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                'Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                'If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                'final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                'End If
                'MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                'If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                'StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                'StockOperator.SetAccountManager(MainForm.AccountManager)
                'Else
                '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                'EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                'MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                'End If
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

    'symbol 객체가 호가 update되었다고 신호보내옴
    Public Sub CallPriceUpdated()
        'If StockOperator IsNot Nothing Then
        'If StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
        'If (StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) Then
        'If _CurrentPhase = SearchPhase.PRETHRESHOLDED Or _CurrentPhase = SearchPhase.WAIT_FALLING Then
        '140328 : pre-threshold가 들어가면서 _SELL_SUPERIOR_THRESHOLD가 유명무실해지고 NoMoreEnteringTime도 제구실을 못하게 생겼다. 대책강구가 필요하다.
        '140402 : _SELL_SUPERIOR_THRESHOLD에 따른 변화는 다행히 크지 않아 보인다. 그리고 NoMoreEnteringTime은 pre threshold 진입때 하기 때문에 괜찮다.
        '140405 : 실시간 호가에 의한 prethrehold-> wait_exit 상태변경은 불가능하다. 왜냐면 호가는 체결을 말해주지 않기 때문이다. 따라서 체결에 의한 상태변경을 생각해야 한다.
        'End If
        'End If
        'End If
        'End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub BuyingInitiated()
        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        'If _CurrentPhase = SearchPhase.WAIT_FALLING OrElse _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
            EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
            'EnterPrice = a_data.Price                  '진입가
            _BasePrice = EnterPrice             '바닥가 기록
            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
            'FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

            'Else
            '반등기다리기 모드로 변경
            '_CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
            '_BasePrice = unidelta_str.Price             '바닥가 기록
            'End If
            _FallHeight = _FallingStartPrice - EnterPrice           '하락폭 기록
        End If
    End Sub
    '[OneKey] -----------------------------------------------------------------------------------------------------------------┘

    '    SafeEnter(_CurrentPhaseKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
    Public Overrides Sub ClearNow(ByVal current_price As UInt32)
        If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub
    '    SafeLeave(_CurrentPhaseKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

    Public Overrides Sub CreateGraphicData()
        'Public PricePointList, MAPointList, BuyMiniSumPointList, SelMiniSumPointList, RatePointList, DecideTimePointList As PointList
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            a_point.Y = RecordList(index).MAPrice
            MAPointList.Add(a_point)                                '이동평균 그래프자료 만들기
            'a_point.Y = RecordList(index).BuyMiniSum
            'BuyMiniSumPointList.Add(a_point)                        'BuyMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).SelMiniSum
            'SelMiniSumPointList.Add(a_point)                        'SelMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).Unidelta
            'UnideltaPointList.Add(a_point)                          'Unidelta 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        DecideTimePointList.Add(New PointF(StartTime.TimeOfDay.TotalSeconds, 1))                '시작시간
        DecideTimePointList.Add(New PointF(StartTime.TimeOfDay.TotalSeconds + 0.001, 0))        '시작시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 1))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 0))                 '청산시간
    End Sub


    'old decision maker로부터 second chance information을 빼냄
    Public Overrides Sub GetSecondChanceInformation(ByVal old_decision_maker As c050_DecisionMaker)

    End Sub
End Class
#End If

#If 0 Then
Public Class c05F_HighAmountShortTime
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure HighAmountShortTimeStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
    End Structure

    Public _DEFAULT_HAVING_TIME As Integer = HAVING_LENGTH
    Public _BEGINNING_MARGIN As Integer = 7
    Public _ENDING_MARGIN As Integer = 7

    Private PricePointList, AmountPointList, DecideTimePointList As PointList
    Private PriceCompositeData, AmountCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of HighAmountShortTimeStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0

    Public Shared AMOUNT_AVE_THRESHOLD As Double = 3.12
    Public Shared AMOUNT_VAR_THRESHOLD As Double = 1.057
    Public Shared PRICE_VAR_THRESHOLD As Double = 1
    'Public OneMoreSampleCheck As Boolean = False

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        AMOUNT_AVE_THRESHOLD = TestArray(TestIndex)


        '_DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '거래량(Amount) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("거래량", DataType.REAL_NUMBER_DATA, Nothing)
        AmountCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "거래량")
        AmountPointList = New PointList()
        AmountCompositeData.SetData(PricePointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(AmountCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다.
    End Sub

    Public Overrides Sub CreateGraphicData()
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        '170119: HighAmountShortTime의 기본 조건을 만들었다. 10초 동안 Amount ave몇 이상, Amount var 몇 이하, Price var 몇 이하.
        '170119: RecordList는 처음부터 끝까지 계속 기록하는 것으로 한다. 패턴의 위치 등은 처음부터의 인덱스로 표시하도록 한다.
        '170119: 아래를 잘 만들어보자.

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
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5 + 0.001, 0))        '진입시간 + epsilon
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

            If score < SCORE_THRESHOLD And b_max / b_min > FALL_SCALE_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
        Else
            result = False
        End If

        Return result
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

        patterncheck_str.Price = a_data.Price         '주가 저장
        patterncheck_str.Amount = a_data.Amount         '거래량 저장

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
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

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드

            Dim matching As Boolean = CheckMatching()

            If matching = True Then
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
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            If OneMoreSampleCheck Then
                OneMoreSampleCheck = False
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    'WAIT_EXIT_TIME 들어오고나서 첫번째 샘플까지 봐서 변동성완화장치 걸린 거라면 취소시키자.
                    _CurrentPhase = SearchPhase.WAIT_FALLING
                    Exit Sub
                End If
            End If
            _WaitExitCount += 1         '청산기다림 카운트
            '청산 기다리기 모드
            '_BasePrice = Math.Min(_BasePrice, unidelta_str.Price)           '바닥가 업데이트
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            If (_WaitExitCount >= _DEFAULT_HAVING_TIME) Then
                '그냥 때가 되었다.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
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

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class
#End If

#If 0 Then
Public Class c05F_GlobalTrend_DecisionMaker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure UnideltaStructure
        Dim Price As UInt32
        Dim MAPrice As UInt32
        Dim Amount As UInt64       '거래량
        Dim PriceDelta As Int32      '델타 가격
        Dim AmountDelta As UInt64      '델타 거래량
    End Structure

    '써치 페이즈
    '131015:MA는 4번의 가격을 평균하는 것으로 알고 있는데.. 그 4가 어디있지?.
    Public _SELL_SUPERIOR_THRESHOLD As Integer = 15
    Public _BUY_SUPERIOR_THRESHOLD As Integer = 15
    Public _BUY_ALLOW_THRESHOLD As Integer = 12
    Public _SELL_ALLOW_THRESHOLD As Integer = 14
    Public _BUY_APPEARANCE_THRESHOLD As Integer = 14
    Public _SELL_APPEARANCE_THRESHOLD As Integer = 14
    Public _FALL_PRE_THRESHOLD As Double = 0.03
    'Public _FALL_PERCENT_THRESHOLD As Double = 0.95
    Public _RISE_PERCENT_THRESHOLD As Double = 0.05432
    Public _FALLBACK_PERCENT_THRESHOLD As Double = 0.3811
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 0.966296
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.2
    Public _DEFAULT_HAVING_TIME As Integer = 169   '09:00 ~ 09:03
    'Public _DEFAULT_HAVING_TIME1 As Integer = 160   '09:03 ~ 09:06
    'Public _DEFAULT_HAVING_TIME2 As Integer = 160   '09:06 ~ 09:30
    'Public _DEFAULT_HAVING_TIME3 As Integer = 160   '09:30 ~ 10:00
    'Public _DEFAULT_HAVING_TIME4 As Integer = 160   '10:00 ~ 10:30
    'Public _DEFAULT_HAVING_TIME5 As Integer = 160   '10:30 ~ 11:30
    'Public _DEFAULT_HAVING_TIME6 As Integer = 160   '11:30 ~ 12:00
    'Public _DEFAULT_HAVING_TIME7 As Integer = 160   '12:00 ~ 13:00
    'Public _DEFAULT_HAVING_TIME8 As Integer = 160   '13:00 ~ 13:30
    'Public _DEFAULT_HAVING_TIME9 As Integer = 160   '13:30 ~ 14:10
    'Public _DEFAULT_HAVING_TIME10 As Integer = 160   '14:10 ~ 14:30
    'Public _DEFAULT_HAVING_TIME11 As Integer = 160   '14:30 ~
    Public _SECOND_CHANCE_THRESHOLD_TIME As Integer = 0
    Public _SECOND_CHANCE_FACTOR As Double = 1.13
    Public _YESTER_ADDITION_28P As Double = 0.026
    Public _YESTER_ADDITION_26P As Double = 0.029
    Public _YESTER_ADDITION_24P As Double = 0.03
    Public _YESTER_ADDITION_22P As Double = 0.03
    Public _YESTER_ADDITION_20P As Double = 0.029
    Public _YESTER_ADDITION_18P As Double = 0.028
    Public _YESTER_ADDITION_16P As Double = 0.026
    Public _YESTER_ADDITION_14P As Double = 0.023
    Public _YESTER_ADDITION_12P As Double = 0.021
    Public _YESTER_ADDITION_10P As Double = 0.017
    Public _YESTER_ADDITION_8P As Double = 0.013
    Public _YESTER_ADDITION_6P As Double = 0.009
    Public _YESTER_ADDITION_4P As Double = 0.005
    Public _YESTER_ADDITION_2P As Double = 0
    Public _YESTER_ADDITION_0 As Double = -0.004
    Public _YESTER_ADDITION_2N As Double = -0.008
    Public _YESTER_ADDITION_4N As Double = -0.01
    Public _YESTER_ADDITION_6N As Double = -0.011
    Public _YESTER_ADDITION_8N As Double = -0.011
    Public _YESTER_ADDITION_10N As Double = -0.01
    Public _YESTER_ADDITION_12N As Double = -0.009
    Public _YESTER_ADDITION_14N As Double = -0.007
    Public _YESTER_ADDITION_16N As Double = -0.003
    Public _YESTER_ADDITION_18N As Double = 0.001
    Public _YESTER_ADDITION_20N As Double = 0.016
    Public _YESTER_ADDITION_22N As Double = 0.012
    Public _YESTER_ADDITION_24N As Double = 0.002
    Public _YESTER_ADDITION_26N As Double = -0.007
    Public _YESTER_ADDITION_28N As Double = -0.011
    Public _YESTER_ADDITION_30N As Double = -0.011
    Public _T_A As Double = 624.8000286
    Public _T_B As Double = -0.008415
    Public _D_A As Double = 4601.37348
    Public _D_B As Double = -0.0002803248
    Public _TD As Double = 9000
    Public _CONST As Double = 0.09127118
    'Public _T5_A As Double = 624.8000286
    'Public _T5_B As Double = -0.008415
    'Public _D5_A As Double = 4601.37348
    'Public _D5_B As Double = -0.0002803248
    'Public _TD5 As Double = 9000
    'Public _CONST5 As Double = 0.09127118
    Public _THRESHOLD_NOISE_FILTER As Double = 0.06336
    'Public _A_A As Double = 1
    'Public _A_B As Double = 0.5
    'Public _A_C As Double = 1
    Public _APRICE_THRESHOLD As Double = 0.015
    Public _ADCR_THRESHOLD As Double = 2
    Public _APRICE_F_THRESHOLD As Double = -0.015
    Public _ADCR_F_THRESHOLD As Double = 2
    Public _L_A As Double = -1
    Public _L_B As Double = 1
    Public _L_C As Double = 3
    Public _L_D As Double = 100
    Public _H_A As Double = -0.0008
    Public _H_B As Double = 726
    Public _H_C As Double = 33



    Private RecordList As New List(Of UnideltaStructure)
    Private RecordListForFallback As New List(Of UnideltaStructure)
    Private _PriceOld As UInt32
    Private _AmountOld As UInt64
    Private _PriceDeltaOld As Int32
    Private _AmountDeltaOld As UInt64
    Private _VPriceOld As Double
    Private _APrice As Double
    Private _AmountDensityOld As Double
    Private _AmountDensityChangeRatio As Double
    Private _SellSuperiorCount As Integer
    Private _BuySuperiorCount As Integer
    Private _BuyAllowCount As Integer = 0
    Private _SellAllowCount As Integer = 0
    Private _BuyAppearanceCount As Integer = 0
    Private _SellAppearanceCount As Integer = 0
    '    Private _UnideltaSum As Double = 0
    '    Private _UnideltaCount As Integer = 0
    '    Private _UnideltaAve As Double
    Private _MaxUnidelta As Double = [Double].MinValue
    Private _MinUnidelta As Double = [Double].MaxValue
    Private _WaitExitCount As Integer = 0
    Private _NoChangeCount As Integer = 0
    Public _FallingStartPrice As UInt32 = 0
    Private _RisingStartPrice As UInt32 = 0
    Private _FallingStartAmount As UInt32 = 0
    Private _FallHeight As UInt32 = 0
    Private _RiseHeight As UInt32 = 0
    Private _BasePrice As UInt32 = 0
    Private _BasePriceUpdatedTime As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _RecordCountForPreThreshold As Integer = 0
    'Public PricePointList, MAPointList, BuyMiniSumPointList, SelMiniSumPointList, UnideltaPointList, DecideTimePointList As PointList
    Private PricePointList, MAPointList, DecideTimePointList As PointList
    '    Public PriceCompositeData, MACompositeData, BuyMiniSumCompositeData, SelMiniSumCompositeData, UnideltaCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private PriceCompositeData, MACompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Public NoMoreOperation As Boolean = False
    Private FixedFallThreshold As Double
    Private FixedRiseThreshold As Double
    Private OldFixedFallThreshold As Double = 0.99
    Private OldFixedRiseThreshold As Double = 0.99
    Private _LiquidateTimer As Double = 0
    Private _LIQUIDATE_TIMEOUT As Double = 8000

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        '_DEFAULT_HAVING_TIME = TestArray(TestIndex)

        '_T_A = MainForm.Form_T_A
        '_T_B = MainForm.Form_T_B
        '_D_A = MainForm.Form_D_A
        '_D_B = MainForm.Form_D_B
        '_TD = MainForm.Form_TD
        '_CONST = MainForm.Form_CONST

        '_H_A = MainForm.Form_H_A
        '_H_B = MainForm.Form_H_B
        '_H_C = MainForm.Form_H_C
        '_H_D = MainForm.Form_H_D
        '_H_E = MainForm.Form_H_E
        '_H_F = MainForm.Form_H_F
        '_L_A = MainForm.Form_L_A
        '_L_B = MainForm.Form_L_B
        '_L_C = MainForm.Form_L_C
        '_L_D = MainForm.Form_L_D
        '_T5_A = MainForm.Form_T5_A
        '_T5_B = MainForm.Form_T5_B
        '_D5_A = MainForm.Form_D5_A
        '_D5_B = MainForm.Form_D5_B
        '_TD5 = MainForm.Form_TD5
        '_CONST5 = MainForm.Form_CONST5
        '_THRESHOLD_NOISE_FILTER = MainForm.Form_THRESHOLD_NOISE_FILTER
        '_APRICE_THRESHOLD = MainForm.Form_APRICE_THRESHOLD
        '_ADCR_THRESHOLD = MainForm.Form_APRICE_THRESHOLD
        '_APRICE_F_THRESHOLD = MainForm.Form_APRICE_F_THRESHOLD
        '_ADCR_F_THRESHOLD = MainForm.Form_APRICE_F_THRESHOLD
        '_SELL_SUPERIOR_THRESHOLD = MainForm.Form_TH1
        '_BUY_SUPERIOR_THRESHOLD = MainForm.Form_TH2
        '_BUY_ALLOW_THRESHOLD = MainForm.Form_TH3
        '_SELL_ALLOW_THRESHOLD = MainForm.Form_TH4
        '_BUY_APPEARANCE_THRESHOLD = MainForm.Form_TH5
        '_SELL_APPEARANCE_THRESHOLD = MainForm.Form_TH6
        '_RISE_PERCENT_THRESHOLD = MainForm.Form_RISE_PERCENT_THRESHOLD
        '_FALLBACK_PERCENT_THRESHOLD = MainForm.Form_FALLBACK_PERCENT_THRESHOLD
        '_POSITIVE_RELATIVE_CUT_THRESHOLD = MainForm.Form_POSITIVE_RELATIVE_CUT_THRESHOLD
        '_NEGATIVE_CUT_THRESHOLD = MainForm.Form_NEGATIVE_CUT_THRESHOLD

        '_DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
        UpdateFixedFallThreshold(current_time)

        Dim x_data_spec, y_data_spec As c00_DataSpec

        '가격(Price) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("가격", DataType.REAL_NUMBER_DATA, Nothing)
        PriceCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "가격")
        PricePointList = New PointList()
        PriceCompositeData.SetData(PricePointList)

        '이평가(MAPrice) CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("이평가", DataType.REAL_NUMBER_DATA, Nothing)
        MACompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "이평가")
        MAPointList = New PointList()
        MACompositeData.SetData(MAPointList)

#If 0 Then
        'BuyMiniSum CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("BuyMiniSum", DataType.REAL_NUMBER_DATA, Nothing)
        BuyMiniSumCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "BuyMiniSum")
        BuyMiniSumPointList = New PointList()
        BuyMiniSumCompositeData.SetData(BuyMiniSumPointList)

        'SelMiniSum CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("SelMiniSum", DataType.REAL_NUMBER_DATA, Nothing)
        SelMiniSumCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "SelMiniSum")
        SelMiniSumPointList = New PointList()
        SelMiniSumCompositeData.SetData(SelMiniSumPointList)
#End If

        'Unidelta CompositeData
        'x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        'y_data_spec = New c00_DataSpec("Unidelta", DataType.REAL_NUMBER_DATA, Nothing)
        'UnideltaCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "Unidelta")
        'UnideltaPointList = New PointList()
        'UnideltaCompositeData.SetData(UnideltaPointList)

        '판단시간 CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("판단시간", DataType.REAL_NUMBER_DATA, Nothing)
        DecideTimeCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "판단시간")
        DecideTimePointList = New PointList()
        DecideTimeCompositeData.SetData(DecideTimePointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(MACompositeData)
        'GraphicCompositeDataList.Add(BuyMiniSumCompositeData)
        'GraphicCompositeDataList.Add(SelMiniSumCompositeData)
        'GraphicCompositeDataList.Add(UnideltaCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)

    End Sub

    Public ReadOnly Property AdaptiveDefaultHavingTime(ByVal current_time As DateTime) As Integer
        Get
            Return _DEFAULT_HAVING_TIME 'not any more adaptive
#If 0 Then
            Dim current_minutes As Integer = current_time.TimeOfDay.TotalSeconds
            If current_minutes < MarketStartTime * 3600 + 193 Then
                Return _DEFAULT_HAVING_TIME0
            ElseIf current_minutes < MarketStartTime * 3600 + 6 * 60 Then
                Return _DEFAULT_HAVING_TIME1
            ElseIf current_minutes < MarketStartTime * 3600 + 30 * 60 Then
                Return _DEFAULT_HAVING_TIME2
            ElseIf current_minutes < MarketStartTime * 3600 + 60 * 60 Then
                Return _DEFAULT_HAVING_TIME3
            ElseIf current_minutes < MarketStartTime * 3600 + 90 * 60 Then
                Return _DEFAULT_HAVING_TIME4
            ElseIf current_minutes < MarketStartTime * 3600 + 150 * 60 Then
                Return _DEFAULT_HAVING_TIME5
            ElseIf current_minutes < MarketStartTime * 3600 + 180 * 60 Then
                Return _DEFAULT_HAVING_TIME6
            ElseIf current_minutes < MarketStartTime * 3600 + 240 * 60 Then
                Return _DEFAULT_HAVING_TIME7
            ElseIf current_minutes < MarketStartTime * 3600 + 270 * 60 Then
                Return _DEFAULT_HAVING_TIME8
            ElseIf current_minutes < MarketStartTime * 3600 + 310 * 60 Then
                Return _DEFAULT_HAVING_TIME9
            ElseIf current_minutes < MarketStartTime * 3600 + 330 * 60 Then
                Return _DEFAULT_HAVING_TIME10
            Else 'If current_minutes < MarketStartTime * 60 + 240 Then    '14:28 ~ 
                Return _DEFAULT_HAVING_TIME11
            End If
#End If
        End Get
    End Property

    Public Sub UpdateFixedFallThreshold(ByVal current_time As DateTime)
        Dim current_minutes As Integer = current_time.TimeOfDay.TotalSeconds
        Dim yester_price As UInt32 = LinkedSymbol.GetYesterPrice
        Dim start_price_yester_rate As Double
        'Dim temp_threshold As Double

        If yester_price = 0 Then
            start_price_yester_rate = 0
        Else
            start_price_yester_rate = _FallingStartPrice / yester_price - 1
        End If
        Dim yester_addition As Double
        If start_price_yester_rate > 0.28 Then
            yester_addition = _YESTER_ADDITION_28P
        ElseIf start_price_yester_rate > 0.26 Then
            yester_addition = _YESTER_ADDITION_26P
        ElseIf start_price_yester_rate > 0.24 Then
            yester_addition = _YESTER_ADDITION_24P
        ElseIf start_price_yester_rate > 0.22 Then
            yester_addition = _YESTER_ADDITION_22P
        ElseIf start_price_yester_rate > 0.2 Then
            yester_addition = _YESTER_ADDITION_20P
        ElseIf start_price_yester_rate > 0.18 Then
            yester_addition = _YESTER_ADDITION_18P
        ElseIf start_price_yester_rate > 0.16 Then
            yester_addition = _YESTER_ADDITION_16P
        ElseIf start_price_yester_rate > 0.14 Then
            yester_addition = _YESTER_ADDITION_14P
        ElseIf start_price_yester_rate > 0.12 Then
            yester_addition = _YESTER_ADDITION_12P
        ElseIf start_price_yester_rate > 0.1 Then
            yester_addition = _YESTER_ADDITION_10P
        ElseIf start_price_yester_rate > 0.08 Then
            yester_addition = _YESTER_ADDITION_8P
        ElseIf start_price_yester_rate > 0.06 Then
            yester_addition = _YESTER_ADDITION_6P
        ElseIf start_price_yester_rate > 0.04 Then
            yester_addition = _YESTER_ADDITION_4P
        ElseIf start_price_yester_rate > 0.02 Then
            yester_addition = _YESTER_ADDITION_2P
        ElseIf start_price_yester_rate > 0 Then
            yester_addition = _YESTER_ADDITION_0
        ElseIf start_price_yester_rate > -0.02 Then
            yester_addition = _YESTER_ADDITION_2N
        ElseIf start_price_yester_rate > -0.04 Then
            yester_addition = _YESTER_ADDITION_4N
        ElseIf start_price_yester_rate > -0.06 Then
            yester_addition = _YESTER_ADDITION_6N
        ElseIf start_price_yester_rate > -0.08 Then
            yester_addition = _YESTER_ADDITION_8N
        ElseIf start_price_yester_rate > -0.1 Then
            yester_addition = _YESTER_ADDITION_10N
        ElseIf start_price_yester_rate > -0.12 Then
            yester_addition = _YESTER_ADDITION_12N
        ElseIf start_price_yester_rate > -0.14 Then
            yester_addition = _YESTER_ADDITION_14N
        ElseIf start_price_yester_rate > -0.16 Then
            yester_addition = _YESTER_ADDITION_16N
        ElseIf start_price_yester_rate > -0.18 Then
            yester_addition = _YESTER_ADDITION_18N
        ElseIf start_price_yester_rate > -0.2 Then
            yester_addition = _YESTER_ADDITION_20N
        ElseIf start_price_yester_rate > -0.22 Then
            yester_addition = _YESTER_ADDITION_22N
        ElseIf start_price_yester_rate > -0.24 Then
            yester_addition = _YESTER_ADDITION_24N
        ElseIf start_price_yester_rate > -0.26 Then
            yester_addition = _YESTER_ADDITION_26N
        ElseIf start_price_yester_rate > -0.28 Then
            yester_addition = _YESTER_ADDITION_28N
        Else 'If start_price_yester_rate > -0.3 Then
            yester_addition = _YESTER_ADDITION_30N
        End If
#If 0 Then

        If LinkedSymbol.RecentGlobalTrend = 0 Or LinkedSymbol.RecentGlobalDeviation = 0 Then
            FixedFallThreshold = 0.99
            FixedRiseThreshold = 0.99
        Else
            'fall threshold
            Dim T_term, D_term, TD_term As Double
            Dim result As Double

            T_term = _T_A * (LinkedSymbol.RecentGlobalTrend - _T_B) * LinkedSymbol.RecentGlobalTrend
            D_term = _D_A * (LinkedSymbol.RecentGlobalDeviation - _D_B) * LinkedSymbol.RecentGlobalDeviation
            TD_term = _TD * LinkedSymbol.RecentGlobalTrend * LinkedSymbol.RecentGlobalDeviation
            result = T_term + D_term + TD_term + _CONST
            If result < 0.01 Or result > 0.5 Then
                FixedFallThreshold = 0.99
            Else
                If OldFixedFallThreshold = 0.99 Then
                    FixedFallThreshold = result
                    OldFixedFallThreshold = FixedFallThreshold
                Else
                    If result > OldFixedFallThreshold Then
                        temp_threshold = Math.Min(result, OldFixedFallThreshold * (1 + _THRESHOLD_NOISE_FILTER))
                    Else
                        temp_threshold = Math.Max(result, OldFixedFallThreshold * (1 - _THRESHOLD_NOISE_FILTER))
                    End If
                    FixedFallThreshold = temp_threshold + yester_addition
                    If FixedFallThreshold < 0.01 Then
                        FixedFallThreshold = 0.01
                    End If
                    OldFixedFallThreshold = temp_threshold
                End If
            End If

#If 0 Then
            'rise threshold
            Dim T5_term, D5_term, TD5_term As Double
            Dim result5 As Double

            T5_term = _T5_A * (LinkedSymbol.RecentGlobalTrend - _T5_B) * LinkedSymbol.RecentGlobalTrend
            D5_term = _D5_A * (LinkedSymbol.RecentGlobalDeviation - _D5_B) * LinkedSymbol.RecentGlobalDeviation
            TD5_term = _TD5 * LinkedSymbol.RecentGlobalTrend * LinkedSymbol.RecentGlobalDeviation
            result5 = T5_term + D5_term + TD5_term + _CONST5
            If result5 < 0.01 Or result5 > 0.5 Then
                FixedRiseThreshold = 0.99
            Else
                If OldFixedRiseThreshold = 0.99 Then
                    FixedRiseThreshold = result5
                    OldFixedRiseThreshold = FixedRiseThreshold
                Else
                    If result5 > OldFixedRiseThreshold Then
                        temp_threshold = Math.Min(result5, OldFixedRiseThreshold * (1 + _THRESHOLD_NOISE_FILTER))
                    Else
                        temp_threshold = Math.Max(result5, OldFixedRiseThreshold * (1 - _THRESHOLD_NOISE_FILTER))
                    End If
                    FixedRiseThreshold = temp_threshold '+ yester_addition
                    If FixedRiseThreshold < 0.01 Then
                        FixedRiseThreshold = 0.01
                    End If
                    OldFixedRiseThreshold = temp_threshold
                End If
            End If
#End If
        End If
#End If
    End Sub

    Private Function LiquidateTimeIncrement(ByVal v_price As Double) As Double
        Dim in_percent As Double = 100 * v_price
        Return _L_A * in_percent ^ 3 + _L_B * in_percent ^ 2 + _L_C * in_percent + _L_D
    End Function

    Private Function GetHighPointTime(ByVal low_time As Integer) As Integer
        Return Math.Round(_H_A * low_time * (low_time - _H_B) + _H_C) '+ _H_D * Math.Exp(_H_E * LinkedSymbol.RecentGlobalDeviation) + _H_F * LinkedSymbol.RecentGlobalTrend)
        'Return Math.Round(_H_C + _H_D * Math.Exp(_H_E * LinkedSymbol.RecentGlobalDeviation) + _H_F * LinkedSymbol.RecentGlobalTrend)
    End Function

    '새 데이터 도착
    '131111 : gangdo의 최종 사용처 MiniSelSum의 대체작업이 끝났다. 이제 gangdo와 관련된 모든 쓸모없는 로직을 지워버리자.
    Public Overrides Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
        '170914 : Global trend data의 전달을 위해 a_data의 type을 변경해야 겠다.
        Dim unidelta_str As UnideltaStructure
        Dim time_over_clearing As Boolean = False
        Dim diff_amount As Int64
        Dim v_price As Double
        Dim amount_density As Double

        unidelta_str.Price = a_data.Price         '주가 저장
        unidelta_str.MAPrice = a_data.MAPrice     '이동평균 값 저장
        unidelta_str.Amount = a_data.Amount         '거래량 저장
        If RecordList.Count > 0 Then
            '델타 계산해서 저장
            unidelta_str.PriceDelta = CType(a_data.Price, Int32) - CType(_PriceOld, Int32)
            v_price = unidelta_str.PriceDelta / _PriceOld
            diff_amount = CType(a_data.Amount, Int64) - CType(_AmountOld, Int64)
            If diff_amount < 0 Then
                '누적 거래량이 줄어든 오류 상황
                unidelta_str.AmountDelta = 0
                MsgBox("누적거래량이 줄어든 오류 상황.")
            Else
                '정상적인 델타거래량
                unidelta_str.AmountDelta = diff_amount
            End If
            Dim unit_price As Integer
            If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI Then
                If unidelta_str.Price < 1000 Then
                    unit_price = 1
                ElseIf unidelta_str.Price < 5000 Then
                    unit_price = 5
                ElseIf unidelta_str.Price < 10000 Then
                    unit_price = 10
                ElseIf unidelta_str.Price < 50000 Then
                    unit_price = 50
                ElseIf unidelta_str.Price < 100000 Then
                    unit_price = 100
                ElseIf unidelta_str.Price < 500000 Then
                    unit_price = 500
                Else
                    unit_price = 1000
                End If
            Else 'If KOSDAQ Then
                If unidelta_str.Price < 1000 Then
                    unit_price = 1
                ElseIf unidelta_str.Price < 5000 Then
                    unit_price = 5
                ElseIf unidelta_str.Price < 10000 Then
                    unit_price = 10
                ElseIf unidelta_str.Price < 50000 Then
                    unit_price = 50
                Else
                    unit_price = 100
                End If
            End If

            If unidelta_str.PriceDelta = 0 Then
                amount_density = 2 * unidelta_str.AmountDelta / unit_price
            Else
                amount_density = unidelta_str.AmountDelta / unidelta_str.PriceDelta
            End If
            If RecordList.Count > 1 Then
                '델델타 등 계산
                _APrice = v_price - _VPriceOld
                If _AmountDensityOld = 0 Then
                    _AmountDensityChangeRatio = 0
                Else
                    _AmountDensityChangeRatio = amount_density / _AmountDensityOld
                End If
            End If

            'Old delta 값들을 저장해둔다.
            _PriceDeltaOld = unidelta_str.PriceDelta
            _VPriceOld = v_price
            _AmountDeltaOld = unidelta_str.AmountDelta
            _AmountDensityOld = amount_density
        Else
            'record list 없음
            unidelta_str.PriceDelta = 0
            unidelta_str.AmountDelta = 0
        End If
        'uni-delta  계산
        'Dim delta_unidelta As Double = 0
        'delta_unidelta = CType(unidelta_str.BuyDelta, Double) - unidelta_str.SelDelta
        'For index As Integer = 1 To _DELTA_CURVE.Length
        'If RecordList.Count - index >= 0 Then
        'delta_unidelta += _DELTA_CURVE(index - 1) * (CType(RecordList(RecordList.Count - index).BuyDelta, Double) - RecordList(RecordList.Count - index).SelDelta)
        'End If
        'Next
        'If RecordList.Count > 0 Then
        'unidelta_str.Unidelta = RecordList(RecordList.Count - 1).Unidelta + delta_unidelta
        'Else
        'unidelta_str.Unidelta = delta_unidelta
        'End If

        'old amount값등을 저장해둔다.
        _AmountOld = a_data.Amount
        _PriceOld = a_data.Price

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드

            'unidelta min,max 계산
            '_MinUnidelta = Math.Min(_MinUnidelta, unidelta_str.Unidelta)
            '_MaxUnidelta = Math.Max(_MaxUnidelta, unidelta_str.Unidelta)

            If RecordList.Count > 0 AndAlso unidelta_str.MAPrice < RecordList.Last.MAPrice Then
                '이동평균가가 하락하고 있다
                If _FallingStartPrice = 0 Then
                    '하락폭 계산을 위한 하락 시작가격 기록
                    _FallingStartPrice = RecordList.Last.Price
                    '하락 스케일 계산을 위한 하락 시작 거래량 기록
                    _FallingStartAmount = RecordList.Last.Amount
                End If
                _SellSuperiorCount += 1
                _BuyAllowCount = 0

                If _CurrentPhase <> SearchPhase.PRETHRESHOLDED Then
                    If ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / CType(_FallingStartPrice, Double) > Math.Min(_FALL_PRE_THRESHOLD, FixedFallThreshold)) AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME * 5) Then
                        _CurrentPhase = SearchPhase.PRETHRESHOLDED
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 진입 time
                        '140109 : 새로운 변수 PrePrice set, clear 조건 잘 관리하자. 그리고 갖다 쓰는 데로 이제 넘어가자.
                        '140109 : PrePrice를 어디다 쓰는가? PrePrice 밑으로 가격이 내려왔을 때 돈 때려박는 데는 EnterPrice다!
                        '160126: FixedFallThreshold 결정시점은 Prethreshold 걸렸을 때가 아니고 WAIT_FALLING 시작할 때, 즉 처음 모니터링을 시작할 때로 변경했다. FixedFallThreshold가 Prethreshold 보다 작은 경우 PRETHRESHOLD 단계를 안 거치고 바로 WAIT_EXITING으로 들어가는 case 가 있기 때문이다.
                        '171018: 이 날짜 이전까지 FixedFallThreshold는 처음 decision maker 생성시점 및 Prethreshold 걸렸을 시점 두 개의 업데이트 시점밖에 없었다. 지난 1년 9개월간 FixedFallThreshold가 의도하지 않은 시점에서 업데이트 되었다는 얘기다.
                        '171018: 16년1월26일날 무슨짓을 했던 걸까? 그래서 지금 결과가 이모양인걸까? 좌우간 현재부터는 처음 모니터링 시작할 때 업데이트하는 것으로 바뀌었다. FixedFallThreshold가 pre threshold 보다 작은 경우는 Math.Min 을 써서 해결했다.
                        'FixedFallThreshold = AdaptiveFallThreshold(current_time)
                        EnterPrice = Convert.ToInt32(_FallingStartPrice * (1 - FixedFallThreshold))
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 기록
                        'LinkedSymbol.RequestPriceinfo()             '종목에 대해 가격정보 요청

                        '140529 : pre thresold를 거치지 않고 바로 WAIT EXIT TIME으로 가는 경우... 이 경우 주문 안 하나? => 이런 경우는 없게 만들어놨다. 우선 반드시 pre threshold를 한 번 거치게 해 놨다. 따라서 뚝 떨어졌다가 바로 FALL PRECENT THRESHOLD 이상으로 올라가는 놈들은 검출이 안 될 수도 있다.
                    ElseIf (_CurrentPhase = SearchPhase.PRETHRESHOLDED) And (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / CType(_FallingStartPrice, Double) > Math.Min(_FALL_PRE_THRESHOLD, FixedFallThreshold)) AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME * 5) Then
                        '이만큼 떨어졌으면 언제 반등할지 눈여겨봐야한다.
                        '                    If 1 Then
                        '이미 반등조건이 갖춰졌다면 바로 진입한다
                        '131016: 바닥가나 진입가 이런 것들을 어떻게 정의할까 생각했는데 실제 가격이 아닌 떨어지기 시작한 점 대비
                        'THRESHOLD만큼 떨어진 가격으로 정하는 게 맞는 것 같다. 실제로도 대부분 그 가격으로 거래가 이루어질 테니까
                        _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                        EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                        StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
                        'EnterPrice = a_data.Price                  '진입가
                        _BasePrice = unidelta_str.Price             '바닥가 기록
                        _BasePriceUpdatedTime = RecordList.Count
                        'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                        'Else
                        '반등기다리기 모드로 변경
                        '_CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
                        '_BasePrice = unidelta_str.Price             '바닥가 기록
                        'End If
                        _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                        'ElseIf StockOperator = True AndAlso (RecordList.Count - _RecordCountForPreThreshold) >= 2 Then
                        'pre threshold에서 wait falling으로 돌아오고 나서 2 이상의 시간이 흘렀다.=>주문 취소
                        '청산여부 혹은 진입 취소 여부 판단
                        'If StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                        '청산 혹은 진입 취소 완료됨 => stock operator 초기화
                        'StockOperator = Nothing
                        'Else
                        '140305 : 아래처럼 청산 코드를 써야 하는지 Cancel 코드를 써야 하는지 감을 잡아보자.
#If 0 Then
                        If StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
                            If (StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) Then ' AndAlso ExitPrice <> 0 Then
                                '140310 : 올라와 있는 매수주문들은 취소하고 ㄴ본 decision maker는 wait exit 상태로 바꾸는 것이 맞는 것 같다.
                                '140323 :여기서 해야될 것을 잘 생각해야 된다. BoughtAmount는 Operator 단에서 전부 종료되어야 업데이트되는 변수이다. 일부체결된 수량은 Operator만 알고 있다. 따라서 Liquidate을 부르는 것이 낫지 않을까. Liquidate을 부르는 것이 아무래도 낫다. decision maker의 상태는 나중에 바뀌는 것으로 해야 될 것 같다.
                                '140325 : 방향을 바꾼다. 원칙적으로 ORDER_REQUESTED이면서 체결된 수량이 있는 상태는 없게 해야 한다. 이것은 종목의 실시간 호가데이터가 pre threshold 가격에 한 번이라도 머물렀던 적이 있는지로 확인하도록 한다.
                                '140325 : 그러면 여기서 해야할 것은 Liquidate을 통해 올라온 주문을 모두 취소하는 것이고, 취소 직전에 체결된 것들은 어쩔 수 없이 곧바로 청산절차를 밟도록 한다. 이런 경우가 흔하지는 않을 것으로 예상된다. decision maker에서 Liquidate 콜하는 부분을 참고하도록 한다.
                                '140527 : 두 달동안의 구현으로 인해 이미 매수주문이 일부 체결되었으면 상태가 WAIT_EXIT_TIME으로 변하게 되었다.
                                'If StockOperator.BoughtAmount > 0 Then
                                MessageLogging(LinkedSymbol.Code & " :Prethresholded에서 waitfalling으로 돌아오고나서.. 일로는 아무래도 안 들어올 것 같다~")
                                StockOperator.Liquidate(False)       '청산 주문
                                'End If
                            Else
                                ErrorLogging(LinkedSymbol.Code & " :PreThreshold위로 올라갔는데 주문한 게 어떻게 된건지...")
                            End If
                        Else
                            ErrorLogging(LinkedSymbol.Code & " :Exit 상태라면 여기서 조작해선 안되고 다른데서 분명 할 것이다.")
                        End If
#End If
                    End If
                Else
                    If ((CType(_FallingStartPrice, Double) - unidelta_str.Price) / unidelta_str.Price <= Math.Min(_FALL_PRE_THRESHOLD, FixedFallThreshold)) AndAlso (RecordList.Count - _RecordCountForPreThreshold) >= 2 Then
                        '가격이 다시 pre threshold 레벨 이상으로 올라감
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                        UpdateFixedFallThreshold(current_time)
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                        'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                        'MessageLogging(LinkedSymbol.Code & " :가격이 다시 pre threshold 레벨 이상으로 올라감")
                    ElseIf (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And EnterPrice >= unidelta_str.Price AndAlso Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME * 5) Then
                        'ElseIf (_SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD) And (EnterPrice >= unidelta_str.Price) And (_APrice > _APRICE_THRESHOLD) And (_AmountDensityChangeRatio > _ADCR_THRESHOLD) AndAlso (Not NoMoreEnteringTime(current_time, _DEFAULT_HAVING_TIME11 * 5)) Then
                        '140618, 이걸 안 하다니 위에서 복사해 왔다..
                        If MULTIPLE_DECIDER Then
                            If LinkedSymbol.AlreadyHooked Then
                                '이미 다른 decider가 가져갔으니 초기화시킨다.
                                _CurrentPhase = SearchPhase.WAIT_FALLING
                                current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                                'FixedFallThreshold = AdaptiveFallThreshold(current_time)
                                _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                                'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                                _SellSuperiorCount = 0
                                _BuyAllowCount = 0
                                _BuyAppearanceCount = 0
                                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                                RecordList.Clear()      '레코드 리스트 클리어
                                'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                                'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                                _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                                _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                                _FallingStartPrice = 0              '하락 시작가격 초기화
                                _FallingStartAmount = 0             '하락 시작거래량 초기화
                                _BasePrice = 0                      '바닥가 초기화
                            Else
                                '아무도 안 가져갔으니 이게 가져간다.
                                LinkedSymbol.AlreadyHooked = True
                                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                                EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                                StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
                                'EnterPrice = a_data.Price                  '진입가
                                _BasePrice = unidelta_str.Price             '바닥가 기록
                                'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                                FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                                _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                            End If
                        Else
                            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                            EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                            StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
                            'EnterPrice = a_data.Price                  '진입가
                            _BasePrice = unidelta_str.Price             '바닥가 기록
                            _BasePriceUpdatedTime = RecordList.Count
                            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                            FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                            _FallHeight = _FallingStartPrice - unidelta_str.Price           '하락폭 기록
                        End If
                    Else
                        '계속 Prethreshold 상태를 유지함
                        FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

                    End If
                    '131122:prethresholded 되고나서 뭔가 hysteresis 작업이 필요할 것 같기도 해다. => pre threshold된 시간에서 2 * 5초 동안은 유지되게 해놨다.
                End If

            Else
                '하락하지 않았음
                If _BuyAllowCount = 0 Then
                    '새로운 상승이 시작되고 있음
                    _BuyAppearanceCount += 1
                End If

                _BuyAllowCount += 1
                _SellSuperiorCount += 1         '이것도 하나의 상승으로 본다.

                If (_BuyAllowCount > _BUY_ALLOW_THRESHOLD) Or (_BuyAppearanceCount > _BUY_APPEARANCE_THRESHOLD) Then
                    '이미 너무 많은 상승...=> 하락 처음부터 다시 기다림
                    If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
                        _CurrentPhase = SearchPhase.WAIT_FALLING
                        current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                        UpdateFixedFallThreshold(current_time)
                        _RecordCountForPreThreshold = RecordList.Count      'prethreshold 빠져나가는 time
                        'LinkedSymbol.StopPriceinfo()                '종목 가격정보 리퀘스트 중단
                        'MessageLogging(LinkedSymbol.Code & " :가격이 다시 많이 올라가면서 pre threshold 중단")
                    End If
                    _SellSuperiorCount = 0
                    _BuyAllowCount = 0
                    _BuyAppearanceCount = 0
                    StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                    RecordList.Clear()      '레코드 리스트 클리어
                    'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                    'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                    _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                    _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                    _FallingStartPrice = 0              '하락 시작가격 초기화
                    _FallingStartAmount = 0             '하락 시작거래량 초기화
                    _BasePrice = 0                      '바닥가 초기화
                End If
            End If
            'Unwanted rising 을 detect한다
            If RecordListForFallback.Count > 0 AndAlso unidelta_str.MAPrice > RecordListForFallback.Last.MAPrice Then
                '이동평균가가 상승하고 있다
                If _RisingStartPrice = 0 Then
                    '상승폭 계산을 위한 상승 시작가격 기록
                    _RisingStartPrice = RecordListForFallback.Last.Price
                End If
                _BuySuperiorCount += 1
                _SellAllowCount = 0

                If (_BuySuperiorCount > _BUY_SUPERIOR_THRESHOLD) And ((CType(unidelta_str.Price, Double) - _RisingStartPrice) / unidelta_str.Price > _RISE_PERCENT_THRESHOLD) Then
                    '이만큼 올라갔으면 언제 반락할지 눈여겨봐야한다.
                    '반락기다리기 모드로 변경(Unwanted Rising Detected)
                    'MessageLogging(LinkedSymbol.Code & " :" & "겁나 상승해서 반락기다리기 모드됨. 이런 거 사면 안 된다.")
                    _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED     '반락기다리기 모드로 변경
                    _TopPrice = unidelta_str.Price             '최고가 기록
                    _RiseHeight = unidelta_str.Price - _RisingStartPrice            '하락폭 기록
                End If
            Else
                '상승하지 않았음
                If _SellAllowCount = 0 Then
                    '새로운 하락이 시작되고 있음
                    _SellAppearanceCount += 1
                End If

                _SellAllowCount += 1
                _BuySuperiorCount += 1         '이것도 하나의 상승으로 본다.

                If (_SellAllowCount > _SELL_ALLOW_THRESHOLD) Or (_SellAppearanceCount > _SELL_APPEARANCE_THRESHOLD) Then
                    '이미 너무 많은 하락...=> 상승 처음부터 다시 기다림
                    _BuySuperiorCount = 0
                    _SellAllowCount = 0
                    _SellAppearanceCount = 0
                    _RisingStartPrice = 0              '상승 시작가격 초기화
                    _TopPrice = 0                      '최고가 초기화
                    RecordListForFallback.Clear()      '레코드 리스트 클리어
                End If
            End If
        ElseIf _CurrentPhase = SearchPhase.UNWANTED_RISING_DETECTED Then
            '반락 기다리기 모드
            _TopPrice = Math.Max(_TopPrice, unidelta_str.Price)             '최고가 업데이트
            _RiseHeight = _TopPrice - _RisingStartPrice                     '상승폭 업데이트
            If (CType(_TopPrice, Double) - unidelta_str.Price) / _RiseHeight > _FALLBACK_PERCENT_THRESHOLD Then
                '상승폭의 많은 부분이 반락되었다. => 일반 falling 기다리기 모드로 전환
                _BuySuperiorCount = 0
                _SellAllowCount = 0
                _SellAppearanceCount = 0
                _RisingStartPrice = 0              '상승 시작가격 초기화
                _TopPrice = 0                      '최고가 초기화

                _SellSuperiorCount = 0
                _BuyAllowCount = 0
                _BuyAppearanceCount = 0
                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                RecordList.Clear()      '레코드 리스트 클리어
                'unidelta_str.BuyMiniSum = 0       '거래량 MiniSum초기화
                'unidelta_str.SelMiniSum = 0       '거래량 MiniSum초기화
                _MaxUnidelta = [Double].MinValue    'max unidelta 초기화
                _MinUnidelta = [Double].MaxValue    'min unidelta 초기화
                _FallingStartPrice = 0              '하락 시작가격 초기화
                _FallingStartAmount = 0             '하락 시작거래량 초기화
                _BasePrice = 0                      '바닥가 초기화
                _CurrentPhase = SearchPhase.WAIT_FALLING
                current_time = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)
                UpdateFixedFallThreshold(current_time)
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
#If 0 Then
            If unidelta_str.Price = RecordList(RecordList.Count - 1).Price AndAlso unidelta_str.Amount = RecordList(RecordList.Count - 1).Amount Then
                '변동성완화장치에 걸려 2분간 단일가매매 상태 => 청산기다림 카운트 안 해본다.
                _NoChangeCount += 1
            Else
                If _NoChangeCount > 22 Then
                    '변동성완화장치에 걸려 2분간 단일가매매 상태 => 청산기다림 카운트로 안 친다.
                    _NoChangeCount = 0      '변동성완화장치 구간은 이순간 이후로 끝난 것으로 본다.
                ElseIf _NoChangeCount > 0 Then
                    '몇 회 정도 가격 및 거래량 변화가 없는 구간이 있지만 변동성완화장치에 걸린 것은 아니다. => 청산기다림 카운트 한다.
                    _WaitExitCount += _NoChangeCount + 1
                    _NoChangeCount = 0
                Else
                    '청산기다림 카운트 1회 한다.
                    _WaitExitCount += 1         '청산기다림 카운트
                End If
            End If
#End If
            _WaitExitCount += 1

            '청산 기다리기 모드
            If _BasePrice > unidelta_str.Price Then
                _BasePrice = unidelta_str.Price
                _BasePriceUpdatedTime = RecordList.Count
            End If
            _FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            Dim high_point_time As Integer = GetHighPointTime(_BasePriceUpdatedTime)

            '_LiquidateTimer = _LiquidateTimer + LiquidateTimeIncrement(v_price)
            'If _LiquidateTimer >= _LIQUIDATE_TIMEOUT Or _WaitExitCount >= StoredHavingTime Then
            'If (_WaitExitCount >= StoredHavingTime) And (_APrice < _APRICE_F_THRESHOLD) And (_AmountDensityChangeRatio > _ADCR_F_THRESHOLD) Then
            'If (_WaitExitCount >= StoredHavingTime) Then
            If (RecordList.Count >= _BasePriceUpdatedTime + high_point_time) OrElse (_WaitExitCount >= StoredHavingTime) Then
                '그냥 때가 되었다.
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    _SecondChance = True
                End If
#If 1 Then
            ElseIf (CType(unidelta_str.Price, Double) - _BasePrice) / _FallHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
            ElseIf ((CType(unidelta_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                    '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                    _SecondChance = True
                End If
#End If
#If 0 Then
            Else
                Dim relative_price As Double = ((CType(unidelta_str.Price, Double) - EnterPrice) / EnterPrice)
                Dim amount_threshold As Double = _A_A * (relative_price - _A_B) * relative_price + _A_C
                If ((CType(unidelta_str.Amount, Double) - _FallingStartAmount) / FallVolume) > amount_threshold Then
                    'Amount에 의한 매도시점
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    ExitPrice = a_data.Price                   '청산가
                    Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                End If
#End If
#If 0 Then
            ElseIf (_WaitExitCount >= 4) And (_APrice < _APRICE_F_THRESHOLD) And (_AmountDensityChangeRatio > _ADCR_F_THRESHOLD) Then
                'Amount에 의한 매도시점
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
#End If
            End If
        End If

        RecordList.Add(unidelta_str)          '하나의 record완성
        RecordListForFallback.Add(unidelta_str)          '하나의 record (for detecting fallback) 완성

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
#If 0 Then ' => 이제 매수주문은 MoneyDistribute 에서 한다. 여기서는 청산절차만 진행한다.
        'StockOperator 관리
        If StockOperator = False Then
#If 1 Then
            '진입여부 판단
            If EnterPrice <> 0 Then
                '130603: 현재가에 따라, 하강볼륨에 따라 그리고 현재 주문가능금액에 따라 매수수량 정해지도록 로직 만든다.
                '현재가 : EnterPrice, 하한가 : LinkedSymbol.LowLimitPrice
                '하강볼륨 : FallVolume
                '주문가능금액 : AccountManager.Ordable100 35, 50.....
                '130610: 위에 써진 대로 잘 해보자.
                'Dim safe_rate As Double = (EnterPrice - LinkedSymbol.LowLimitPrice) / ((LinkedSymbol.YesterPrice - LinkedSymbol.LowLimitPrice))
                'If safe_rate > 1 Then
                'safe_rate = 1
                'ElseIf safe_rate < 0 Then
                'safe_rate = 0
                'End If
                'Dim main_rate As Double = MAIN_FACTOR * safe_rate
                '130611: main_rate에 주문가능금액 곱하여 수량 나오고 그 수량과 하강 볼륨*some_rate과 비교하여 하강볼륨이 작을 경우 추가로 자른다.
                'Dim possible_order_volume As Double
                '                Select Case LinkedSymbol.EvidanRate
                '                    Case 35
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable35
                '                    Case 50
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable50
                '                    Case 100
                '                    Case Else
                '                possible_order_volume = main_rate * MainForm.AccountManager.Ordable100
                '                End Select
                'possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
                'Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
                'Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / EnterPrice)

                'If LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And EnterPrice < 50000 Then
                'final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
                'End If
                'MessageLogging(LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

                '130612: 주문수량 계산은 이걸로 됐나... 진짜로... 겹쳐질 수록 주문가능 수량이 점점 줄어드는 문제가 있음!
                'If final_order_amount > 0 Then
                    'StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, final_order_amount)       'stock operator 생성과 동시에 진입 주문
                'StockOperator = New et2_Operation(LinkedSymbol, Convert.ToInt32(_FallingStartPrice * (1 - _FALL_PERCENT_THRESHOLD)), final_order_amount)       'stock operator 생성과 동시에 진입 주문
                    '130613: StockOperator에서 자를때 10단위에 맞게 자르는 것이 필요함.....................................................
                    '                If EnterPrice < 57500 Then
                    '                '(진입 시점.코스피)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 10)       'stock operator 생성과 동시에 진입 주문
                    '            Else
                    '                '(진입 시점.코스닥)
                    '                StockOperator = New et2_Operation(LinkedSymbol, EnterPrice, 1)       'stock operator 생성과 동시에 진입 주문
                    '            End If

                'StockOperator.SetAccountManager(MainForm.AccountManager)
                'Else
                '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                'EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                'MessageLogging(LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
                '130716: 이쪽으로 들어와서 StockOperator가 생기지 않은 경우 StockOperator접근하지 못하게 해야 한다.............................................
                'End If
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

    'symbol 객체가 호가 update되었다고 신호보내옴
    Public Sub CallPriceUpdated()
        'If StockOperator IsNot Nothing Then
        'If StockOperator.EnterExitState = EnterOrExit.EOE_Enter Then
        'If (StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED) Then
        'If _CurrentPhase = SearchPhase.PRETHRESHOLDED Or _CurrentPhase = SearchPhase.WAIT_FALLING Then
        '140328 : pre-threshold가 들어가면서 _SELL_SUPERIOR_THRESHOLD가 유명무실해지고 NoMoreEnteringTime도 제구실을 못하게 생겼다. 대책강구가 필요하다.
        '140402 : _SELL_SUPERIOR_THRESHOLD에 따른 변화는 다행히 크지 않아 보인다. 그리고 NoMoreEnteringTime은 pre threshold 진입때 하기 때문에 괜찮다.
        '140405 : 실시간 호가에 의한 prethrehold-> wait_exit 상태변경은 불가능하다. 왜냐면 호가는 체결을 말해주지 않기 때문이다. 따라서 체결에 의한 상태변경을 생각해야 한다.
        'End If
        'End If
        'End If
        'End If
    End Sub

    '매수 요청한 것 중의 일부가 체결되었다고 통보옴
    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub BuyingInitiated()
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        '140417 : 여기서 필요할 경우 상태를 바꾼다.
        'If _CurrentPhase = SearchPhase.WAIT_FALLING OrElse _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
        If _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
            EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
            StoredHavingTime = AdaptiveDefaultHavingTime(current_time)
            'EnterPrice = a_data.Price                  '진입가
            _BasePrice = EnterPrice             '바닥가 기록
            'FallVolume = unidelta_str.SelMiniSum * EnterPrice
            'FallVolume = (unidelta_str.Amount - _FallingStartAmount) * EnterPrice           '하락 볼륨 업데이트

            'Else
            '반등기다리기 모드로 변경
            '_CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
            '_BasePrice = unidelta_str.Price             '바닥가 기록
            'End If
            _FallHeight = _FallingStartPrice - EnterPrice           '하락폭 기록
        End If
    End Sub
    '[OneKey] -----------------------------------------------------------------------------------------------------------------┘

    '    SafeEnter(_CurrentPhaseKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
    Public Overrides Sub ClearNow(ByVal current_price As UInt32)
        If _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            _CurrentPhase = SearchPhase.DONE
            ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
            ExitPrice = current_price                  '청산가
            Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
            TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
            _Done = True                             '청산완료 알리는 비트 셋
        End If
    End Sub
    '    SafeLeave(_CurrentPhaseKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

    Public Overrides Sub CreateGraphicData()
        'Public PricePointList, MAPointList, BuyMiniSumPointList, SelMiniSumPointList, RatePointList, DecideTimePointList As PointList
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            a_point.Y = RecordList(index).MAPrice
            MAPointList.Add(a_point)                                '이동평균 그래프자료 만들기
            'a_point.Y = RecordList(index).BuyMiniSum
            'BuyMiniSumPointList.Add(a_point)                        'BuyMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).SelMiniSum
            'SelMiniSumPointList.Add(a_point)                        'SelMiniSum 그래프자료 만들기
            'a_point.Y = RecordList(index).Unidelta
            'UnideltaPointList.Add(a_point)                          'Unidelta 그래프자료 만들기
            stock_time = stock_time + TimeSpan.FromSeconds(5)
        Next

        DecideTimePointList.Add(New PointF(StartTime.TimeOfDay.TotalSeconds, 1))                '시작시간
        DecideTimePointList.Add(New PointF(StartTime.TimeOfDay.TotalSeconds + 0.001, 0))        '시작시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 1))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 0))                 '청산시간
    End Sub


    'old decision maker로부터 second chance information을 빼냄
    Public Overrides Sub GetSecondChanceInformation(ByVal old_decision_maker As c050_DecisionMaker)

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

    Public DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 50
    Public DELTA_PERIOD As Integer = 36
    'Public Shared DELTA_GANGDO_THRESHOLD_PRE As Double = 160
    Public DELTA_GANGDO_LOWER_THRESHOLD As Double = 1
    Public DELTA_GANGDO_UPPER_THRESHOLD As Double = 10
    Public MINIMUM_VOLUME As UInt64 = 10000000000
    'Public MINIMUM_AMOUNT As UInt64 = 10
    Public MAXIMUM_PRICE_RATE As Double = -0.02 '-0.019173
    Public MINIMUM_PRICE_RATE As Double = -0.06 '-0.02

    Private PricePointList, DeltaGangdoPointList, DecideTimePointList As PointList
    Private PriceCompositeData, DeltaGangdoCompositeData, DecideTimeCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of DeltaGangdoStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    Private _BasePrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Public EnterDeltaGangdo As Double = 0
    Public TwoMinutesHolding As Boolean = False

    Public OneMoreSampleCheck As Boolean = False
    'Public BuyAmountCenter As Double = 0
    'Public SelAmountCenter As Double = 0
    'Public PatternGangdo As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        If SmartLearning Then
            'DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
            'DELTA_PERIOD = MainForm.Form_DELTA_PERIOD
            ''DELTA_GANGDO_THRESHOLD = MainForm.Form_DELTA_GANGDO_THRESHOLD
            'MINIMUM_PRICE_RATE = MainForm.Form_MINIMUM_PRICE_RATE
            'MINIMUM_AMOUNT = MainForm.Form_MINIMUM_AMOUNT
            'MAX_HAVING_LENGTH = MainForm.Form_MAX_HAVING_LENGTH
        Else
            'DEFAULT_HAVING_TIME = TestArray(TestIndex)
        End If


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

        deltagando_str.Price = a_data.Price         '주가 저장
        deltagando_str.Amount = a_data.Amount         '거래량 저장
        deltagando_str.BuyAmount = a_data.BuyAmount       '매수거래량
        deltagando_str.SelAmount = a_data.SelAmount       '매도거래량
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
            If (deltagando_str.DeltaGangdo > DELTA_GANGDO_LOWER_THRESHOLD) AndAlso (deltagando_str.DeltaGangdo < DELTA_GANGDO_UPPER_THRESHOLD) AndAlso (RecordList(RecordList.Count - 2).DeltaGangdo > DELTA_GANGDO_LOWER_THRESHOLD) AndAlso (RecordList(RecordList.Count - 2).DeltaGangdo < DELTA_GANGDO_UPPER_THRESHOLD) AndAlso (RecordList(RecordList.Count - 3).DeltaGangdo > DELTA_GANGDO_LOWER_THRESHOLD) AndAlso (RecordList(RecordList.Count - 3).DeltaGangdo < DELTA_GANGDO_UPPER_THRESHOLD) AndAlso (CType(a_data.Amount, Long) - RecordList(RecordList.Count - DELTA_PERIOD).Amount) * CType(deltagando_str.Price, Double) > MINIMUM_VOLUME AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MAXIMUM_PRICE_RATE) AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price >= MINIMUM_PRICE_RATE) Then
                'If deltagando_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD AndAlso (CType(a_data.Amount, Long) - RecordList(RecordList.Count - DELTA_PERIOD).Amount) > MINIMUM_AMOUNT AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MINIMUM_PRICE_RATE) Then
                'If deltagando_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD AndAlso ((CType(deltagando_str.Price, Double) - RecordList(RecordList.Count - DELTA_PERIOD).Price) / RecordList(RecordList.Count - DELTA_PERIOD).Price < MINIMUM_PRICE_RATE) Then
                If deltagando_str.Price = RecordList(RecordList.Count - 2).Price AndAlso deltagando_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                    TwoMinutesHolding = True
                End If
                Dim fall_volume As UInt64 = (deltagando_str.Amount - RecordList(RecordList.Count - DELTA_PERIOD).Amount) * deltagando_str.Price           '대충 볼륨 계산
                'If fall_volume > FALL_VOLUME_THRESHOLD Then
                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                EnterTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)             '진입시간
                EnterPrice = deltagando_str.Price                  '진입가
                _BasePrice = deltagando_str.Price             '바닥가 기록
                FallVolume = fall_volume           '볼륨 업데이트
                EnterDeltaGangdo = deltagando_str.DeltaGangdo               '진입 델타강도
                'Else
                'FallVolume이 너무 작으면 하지 말자
                'End If
                OneMoreSampleCheck = True
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            If OneMoreSampleCheck Then
                OneMoreSampleCheck = False
                If deltagando_str.Price = RecordList(RecordList.Count - 2).Price AndAlso deltagando_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 변동성완화장치 풀릴 때까지 기다린다.
                    TwoMinutesHolding = True
                End If
            End If

            If TwoMinutesHolding Then
                If deltagando_str.Price <> RecordList(RecordList.Count - 2).Price Or deltagando_str.Amount <> RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매 종료됨
                    TwoMinutesHolding = False
                    '180117: EnterPrice 업데이트함
                    EnterPrice = deltagando_str.Price                  '진입가
                End If
            End If

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

    End Sub

    Public Overrides Sub GetSecondChanceInformation(old_decision_maker As c050_DecisionMaker)

    End Sub
End Class

Public Class c05E_GangdoPatternChecker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure PatternCheckStructure
        Dim Price As UInt32
        Dim Amount As UInt64       '델타 거래량 소계
        Dim BuyAmount As UInt64
        Dim SelAmount As UInt64
        Dim DeltaGangdo As Double
    End Structure

    Private PricePointList, DecideTimePointList, DeltaGangdoPointList As PointList
    Private PriceCompositeData, DecideTimeCompositeData, DeltaGangdoCompositeData As c011_PlainCompositeData
    Private RecordList As New List(Of PatternCheckStructure)
    Public RecordCount As Integer
    Private _WaitExitCount As Integer = 0
    'Private _TargetHeight As UInt32
    Private _BasePrice As UInt32 = 0
    Private _TopPrice As UInt32 = 0
    Private _CountFromLastBase As UInt32 = 0
    Private _CountFromLastTop As UInt32 = 0
    Public TwoMinutesHolding As Boolean = False
    'Private _BasePrice As UInt32 = 0
    'Private _FallingStartAmount As UInt32 = 0
    'Public Pattern() As Double = {0, -0.004000335, -0.008021433, -0.012244141, -0.017371742, -0.025232781, -0.039625, -0.067400412, -0.119791495, -0.213978516, -0.374897977, -0.637292194}        'Silver
    Public Pattern() As Double = {-2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -1, 0, 1}        'LogSilver_6_1.5
    'Public Shared SCORE_THRESHOLD As Double = 3.12
    'Public Shared FALL_SCALE_LOWER_THRESHOLD As Double = 1.03
    Public DEFAULT_HAVING_TIME As Integer = 23 'HAVING_LENGTH
    Public DEFAULT_HAVING_TIME_TOP As Integer = 5 'HAVING_LENGTH
    Public BEGINNING_MARGIN As Integer = 7
    Public SCORE_LOWER_THRESHOLD As Double = 0.00000001
    Public SCORE_THRESHOLD As Double = 0.15
    Public FALL_SCALE_LOWER_THRESHOLD As Double = 0 '1.02
    Public FALL_SCALE_UPPER_THRESHOLD As Double = 1000000 '1.04 '1.4
    Public DELTA_PERIOD As Integer = 20
    'Public DELTA_GANGDO_THRESHOLD As Double = 5
    Public _NEGATIVE_CUT_THRESHOLD As Double = -0.1
    Public _POSITIVE_RELATIVE_CUT_THRESHOLD As Double = 10
    Public FALL_UNIT_AMOUNT_THRESHOLD As Double = 2000
    Public OneMoreSampleCheck As Boolean = False
    Public BuyAmountCenter As Double = 0
    Public SelAmountCenter As Double = 0
    Public PatternGangdo As Double = 0

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        If SmartLearning Then
            DEFAULT_HAVING_TIME = MainForm.Form_DEFAULT_HAVING_TIME
            SCORE_THRESHOLD = MainForm.Form_SCORE_THRESHOLD
            FALL_SCALE_LOWER_THRESHOLD = MainForm.Form_FALL_SCALE_LOWER_THRESHOLD
            MAX_HAVING_LENGTH = MainForm.Form_MAX_HAVING_LENGTH
        Else
            '_POSITIVE_RELATIVE_CUT_THRESHOLD = TestArray(TestIndex)
        End If

#If 0 Then
        'Pattern normalizing
        Dim b_min As Double = Pattern.Min
        Dim b_max As Double = Pattern.Max

        'normalizing
        For index As Integer = 0 To Pattern.Length - 1
            Pattern(index) = 100 * (Pattern(index) - b_min) / (b_max - b_min)
        Next
#End If

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

        'DeltaGangdo CompositeData
        x_data_spec = New c00_DataSpec("time", DataType.TIME_DATA, Nothing)
        y_data_spec = New c00_DataSpec("DeltaGangdo", DataType.REAL_NUMBER_DATA, Nothing)
        DeltaGangdoCompositeData = New c011_PlainCompositeData(x_data_spec, y_data_spec, "DeltaGangdo")
        DeltaGangdoPointList = New PointList()
        DeltaGangdoCompositeData.SetData(DeltaGangdoPointList)

        'GraphicCompositeDataList 만들기
        GraphicCompositeDataList.Add(PriceCompositeData)
        GraphicCompositeDataList.Add(DecideTimeCompositeData)
        GraphicCompositeDataList.Add(DeltaGangdoCompositeData)
    End Sub

    Public Overrides Sub ClearNow(current_price As UInteger)
        '폐장시간 다되었을 때 무조건 청산하는 명령. 아직은 굳이 필요없다. 현재는 pattern check 에 주력
    End Sub

    Public Overrides Sub CreateGraphicData()
        '160520: 그래프 어떻게 그릴까 생각해보자. 앞뒤로 쪼금씩 더 보여주는 게 필요하겠지.
        '160521: 기본적으로 보유중인 패턴들 중 최대길이 가진 놈보다 몇 개정도 더 긴 길이 만큼의 히스토리를 관리하도록 한다.
        Dim stock_time As DateTime = StartTime
        Dim a_point As PointF

        For index As Integer = 0 To RecordList.Count - 1
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).Price)
            PricePointList.Add(a_point)                             '주가 그래프자료 만들기
            a_point = New PointF(stock_time.TimeOfDay.TotalSeconds, RecordList(index).DeltaGangdo)
            DeltaGangdoPointList.Add(a_point)
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
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds, 0))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + 0.001, 1))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5, 1))                '진입시간
        DecideTimePointList.Add(New PointF(EnterTime.TimeOfDay.TotalSeconds + Pattern.Count * 5 + 0.001, 0))        '진입시간 + epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds - 0.001, 0))         '청산시간 - epsilon
        DecideTimePointList.Add(New PointF(ExitTime.TimeOfDay.TotalSeconds, 1))                 '청산시간
    End Sub

    '등록된 pattern과 현재 RecordList의 최신 data를 비교하여 matching 여부 체크함.
    Private Function CheckMatching() As Boolean
        Dim result As Boolean

        If RecordList.Count > Pattern.Length Then
            '패턴체크할 길이가 되었다.
            Dim target(Pattern.Length - 1) As Double
            For index As Integer = 0 To Pattern.Length - 1
                If RecordList(RecordList.Count - Pattern.Length + index).DeltaGangdo <= 0 Then
                    Return False
                End If
                target(index) = Math.Log10(RecordList(RecordList.Count - Pattern.Length + index).DeltaGangdo)
            Next
            Dim b_min As Double = target.Min
            Dim b_max As Double = target.Max

            '_TargetHeight = b_max - b_min           'target height 업데이트

#If 0 Then
            'normalizing
            If b_max = b_min Then
                result = False
                Return result
            Else
                For index As Integer = 0 To Pattern.Length - 1
                    target_normalized(index) = Math.Round(100 * (target_normalized(index) - b_min) / (b_max - b_min))
                Next
            End If
#End If
            Dim score As Double = 0
            For index As Integer = 0 To Pattern.Length - 1
                'If target_normalized(index) > Pattern(index) Then
                score = score + (target(index) - Pattern(index)) ^ 2
                'Else
                'score = score + (Pattern(index) - target_normalized(index)) ^ 2
                'End If
            Next
            score = Math.Sqrt(score) / Pattern.Length

            If score < SCORE_THRESHOLD And score > SCORE_LOWER_THRESHOLD And b_max / b_min > FALL_SCALE_LOWER_THRESHOLD And b_max / b_min < FALL_SCALE_UPPER_THRESHOLD Then
                ScoreSave = score
                result = True       'matching 된 것으로 판정
            Else
                result = False      'matching 안 된 것으로 판정
            End If
        Else
            result = False
        End If

        Return result
    End Function

    Public Overrides Sub DataArrived(a_data As c03_Symbol.SymbolRecord)
        Dim patterncheck_str As PatternCheckStructure
        Dim time_over_clearing As Boolean = False

        patterncheck_str.Price = a_data.Price         '주가 저장
        patterncheck_str.Amount = a_data.Amount         '거래량 저장
        patterncheck_str.BuyAmount = a_data.BuyAmount       '매수거래량
        patterncheck_str.SelAmount = a_data.SelAmount       '매도거래량
        'DeltaGangdo 계산
        If RecordList.Count >= DELTA_PERIOD Then
            'DeltaGangdo 계산 가능
            Dim delta_buy As UInt64 = Math.Max(1, CType(patterncheck_str.BuyAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).BuyAmount, Long))
            Dim delta_sel As UInt64 = Math.Max(1, CType(patterncheck_str.SelAmount, Long) - CType(RecordList(RecordList.Count - DELTA_PERIOD).SelAmount, Long))
            patterncheck_str.DeltaGangdo = delta_buy / delta_sel
        Else
            'DeltaGangdo 계산 불가능
            patterncheck_str.DeltaGangdo = -1
        End If

        'ClearingTime 시 강제청산 코드
        Dim current_time As DateTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)

        If IsClearingTime(current_time) Then
            ClearNow(a_data.Price)      '강제청산 (여기서 현재상태가 청산기다림상태인지 확인)
            time_over_clearing = True
            _CurrentPhase = SearchPhase.DONE        '청산여부와 상관없이 현재 상태 청산으로 둠
        End If

        '레코드 기록
        RecordList.Add(patterncheck_str)
        While _CurrentPhase = SearchPhase.WAIT_FALLING And RecordList.Count > Pattern.Count + BEGINNING_MARGIN
            RecordList.RemoveAt(0)
            StartTime = StartTime + TimeSpan.FromSeconds(5)
        End While
        RecordCount = RecordCount + 1
        '160525: StartTime 기록법을 생각해보자. StartTime은 pattern이 걸렸을 때, 즉 WAIT_EXIT 된 지점에서 pattern의 시작지점으로 하도록 한다.
        '160525: 다만 현재 record의 시간을 계산하기 위해 현재까지 들어온 record의 갯수를 increase하는 count를 두어서 사용하게 한다
        '160527: StartTime은 그냥 RecordList의 첫째 element의 시간으로 정의하자. 왜냐면 StartTime은 DecisionMaker 밖에서도 그런 의미로 사용되고 있기 때문이다.
        '160527: 패턴의 시작점은 StartTime 이 아니고 EnterTime으로 기록하도록 한다.

        If _CurrentPhase = SearchPhase.WAIT_FALLING Or _CurrentPhase = SearchPhase.PRETHRESHOLDED Then
            '하락 기다리기 모드
            Dim matching As Boolean = CheckMatching()

            If matching = True Then 'And patterncheck_str.DeltaGangdo > DELTA_GANGDO_THRESHOLD Then
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
                If GangdoDB Then
                    '핵심지시자 3인방 계산
                    If patterncheck_str.BuyAmount > 0 And patterncheck_str.SelAmount > 0 Then
                        Dim buy_sum As Int64 = Pattern.Count * patterncheck_str.BuyAmount
                        Dim sel_sum As Int64 = Pattern.Count * patterncheck_str.SelAmount
                        For index As Integer = 0 To Pattern.Count - 1
                            buy_sum -= RecordList(RecordList.Count - 2 - index).BuyAmount
                            sel_sum -= RecordList(RecordList.Count - 2 - index).SelAmount
                        Next
                        If (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) > 0 Then
                            BuyAmountCenter = buy_sum / ((CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).BuyAmount) * Pattern.Count)
                        Else
                            BuyAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) > 0 Then
                            SelAmountCenter = sel_sum / ((CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - Pattern.Count).SelAmount) * Pattern.Count)
                        Else
                            SelAmountCenter = 1
                        End If
                        If (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount) > 0 Then
                            PatternGangdo = 100 * (CType(patterncheck_str.BuyAmount, Int64) - RecordList(RecordList.Count - 1 - 5).BuyAmount) / (CType(patterncheck_str.SelAmount, Int64) - RecordList(RecordList.Count - 1 - 5).SelAmount)
                        Else
                            PatternGangdo = Double.MaxValue
                        End If
                    Else
                        '180127: 어떤 때는 Gangdo가 0보다 작은 경우도 이쪽으로 들어와서 divide by 0 에러를 일으킨다..
                    End If
                End If

                Dim fall_volume As UInt64 = (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) * patterncheck_str.Price           '하락 볼륨 업데이트
                'If fall_volume < FALL_VOLUME_THRESHOLD Then
                If (patterncheck_str.Amount - RecordList(RecordList.Count - Pattern.Count).Amount) / Pattern.Count > FALL_UNIT_AMOUNT_THRESHOLD Then
                    If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                        '변동성완화장치에 걸려 2분간 단일가매매 상태 => 이거까지 치기로 한다. 대신 단일가매매 풀리고 바로 다음 샘플링 때 EnterPrice를 업데이트 한다.
                        TwoMinutesHolding = True
                    End If
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    EnterTime = StartTime + TimeSpan.FromSeconds((RecordList.Count - Pattern.Count) * 5)             '진입시간
                    EnterPrice = patterncheck_str.Price                  '진입가
                    _BasePrice = patterncheck_str.Price             '바닥가 기록
                    _TopPrice = patterncheck_str.Price             '천정가 기록
                    'FallVolume = unidelta_str.SelMiniSum * EnterPrice
                    FallVolume = fall_volume           '하락 볼륨 업데이트
                    OneMoreSampleCheck = True
                    'Else
                    'FallVolume이 너무 크면 오히려 마이너스가 된다.
                    'End If
                End If
#End If
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
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
            If _TopPrice <= patterncheck_str.Price Then                '천정가 업데이트
                _TopPrice = patterncheck_str.Price
                _CountFromLastTop = 0
            Else
                _CountFromLastTop += 1
            End If
            '_FallHeight = _FallingStartPrice - _BasePrice           '하락폭 업데이트
            '160527: _DEFAULT_HAVING_TIME까지 기다리면서 그간의 가격변화를 매 샘플마다 기록한다.
            PriceRateTrend.Add(patterncheck_str.Price / EnterPrice)
            'If (_WaitExitCount >= DEFAULT_HAVING_TIME) Then
            'if ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) >= a_data.Price) And (_CountFromLastBase >= DEFAULT_HAVING_TIME)) Or ((EnterPrice * Math.Sqrt(FALL_SCALE_LOWER_THRESHOLD) < a_data.Price) And (_CountFromLastTop >= DEFAULT_HAVING_TIME_TOP)) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
            If (_CountFromLastBase >= DEFAULT_HAVING_TIME) Or (_WaitExitCount >= MAX_HAVING_LENGTH) Then
                '그냥 때가 되었다.
                If patterncheck_str.Price = RecordList(RecordList.Count - 2).Price AndAlso patterncheck_str.Amount = RecordList(RecordList.Count - 2).Amount Then
                    '180117: 2분간 단일가매매상태로 인해 매도 못한다. 기다려야 된다.
                Else
                    _CurrentPhase = SearchPhase.DONE
                    ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                    ExitPrice = a_data.Price                   '청산가
                    Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                    TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                    _Done = True                             '청산완료 알리는 비트 셋
                End If
#If 0 Then
            ElseIf (CType(patterncheck_str.Price, Double) - _BasePrice) / _TargetHeight > _POSITIVE_RELATIVE_CUT_THRESHOLD Then
                '목표수익 달성
                _CurrentPhase = SearchPhase.DONE
                ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                ExitPrice = a_data.Price                   '청산가
                Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                'ElseIf ((CType(patterncheck_str.Price, Double) - EnterPrice) / EnterPrice) < _NEGATIVE_CUT_THRESHOLD Then
                '손절매
                ' _CurrentPhase = SearchPhase.DONE
                'ExitTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 5)              '청산시간
                'ExitPrice = a_data.Price                   '청산가
                'Profit = ((1 - TAX - FEE) * ExitPrice - (1 + FEE) * EnterPrice) / EnterPrice        '수익률
                'TookTime = TimeSpan.FromSeconds(RecordList.Count * 5)      '걸린 시간
                '_Done = True                             '청산완료 알리는 비트 셋
                'If Profit < 0 AndAlso ((ExitTime - EnterTime).TotalSeconds <= _SECOND_CHANCE_THRESHOLD_TIME * 5) Then
                '추가하락 여지가 있는 것으로 판단되어 두번째 기회를 줌
                '_SecondChance = True
                'End If
#End If
            End If
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
