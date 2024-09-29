Public MustInherit Class c050_DecisionMaker

    Public LinkedSymbol As c03_Symbol
    Friend _DecisionWindowSize As Integer
    Friend _Done As Boolean
    Public StartTime As DateTime
    Public EnterPrice As Integer
    Public ExitPrice As Integer
    Public Profit As Double
    Public TookTime As TimeSpan

    Public MustOverride Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        LinkedSymbol = linked_symbol
        StartTime = start_time
    End Sub

    '모은 데이타 폐기
    Public Sub Clear()

    End Sub

    '
    Public ReadOnly Property IsDone() As Boolean
        Get
            Return _Done
        End Get
    End Property
End Class

#If 0 Then
Public Class c051_BasicDecisionMaker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure BasicStrategy
        Dim MAPrice As UInt32
        Dim BuyDelta As UInt64      '델타 매수거래량
        Dim SelDelta As UInt64      '델타 매도거래량
    End Structure

    '써치 페이즈
    Public Enum SearchPhase
        WAIT_FALLING
        WAIT_RISING
        WAIT_EXIT_TIME
        DONE
    End Enum

    Private Const _DELTA_SUM_COUNT As Integer = 2       '1분간의 amount를 누적하여 delta 값 계산
    Private Const _RISING_THRESHOLD_RATE As Double = 0.7       '총 떨어진 양의 30%를 복구하면 반등으로 판단.
    Private Const _EXIT_THRESHOLD_RATE As Double = 1.3       '누적매도수량 최대값의 30%를 복구하면 청산시점으로 판단.
    Private Const _SELL_SUPERIOR_THRESHOLD As Integer = 36
    Public RecordList As New List(Of BasicStrategy)
    Private _BuyAmountOld As UInt64
    Private _SelAmountOld As UInt64
    Private _DeltaCalcCount As Integer
    Private _SellSuperiorCount As Integer
    Private _WaitRisingCount As Integer
    Private _CurrentPhase As SearchPhase
    Private _SelTotal As Integer
    Private _SelToBeCompensatedBottom As Integer
    Private _EnterPrice As Integer
    Private _SelToBeCompensatedTop As Integer
    Private _ExitPrice As Integer
    'Private _BuyTotal As Integer

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        _DeltaCalcCount = 0
        _DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING
    End Sub

    '새 데이터 도착
    Public Overrides Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
        Dim basic_strategy_str As BasicStrategy
        Dim diff As Int64

        basic_strategy_str.MAPrice = a_data.MAPrice     '이동평균 값 저장
        If _DeltaCalcCount = _DELTA_SUM_COUNT Then
            '델타 계산해서 저장
            diff = CType(a_data.BuyAmount, Int64) - CType(_BuyAmountOld, Int64)
            If diff < 0 Then
                '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
                basic_strategy_str.BuyDelta = 0
            Else
                '정상적인 델타매수량
                basic_strategy_str.BuyDelta = diff
            End If
            'basic_strategy_str.BuyDelta = a_data.BuyAmount - _BuyAmountOld
            diff = CType(a_data.SelAmount, Int64) - CType(_SelAmountOld, Int64)
            If diff < 0 Then
                '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
                basic_strategy_str.SelDelta = 0
            Else
                '정상적인 델타매수량
                basic_strategy_str.SelDelta = diff
            End If
            'basic_strategy_str.SelDelta = a_data.SelAmount - _SelAmountOld
            'old amount값을 저장해둔다.
            _BuyAmountOld = a_data.BuyAmount
            _SelAmountOld = a_data.SelAmount
            _DeltaCalcCount = 0     'delta 계산용 카운터를 리셋한다.

            If _CurrentPhase = SearchPhase.WAIT_FALLING Then
                '하락 기다리기 모드
                If basic_strategy_str.BuyDelta < basic_strategy_str.SelDelta Then
                    '하락하고 있군
                    _SellSuperiorCount += _DELTA_SUM_COUNT      'delta sum count 만큼 몰아서 더한다.
                    _SelTotal += basic_strategy_str.SelDelta - basic_strategy_str.BuyDelta
                    If _SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD Then
                        '이만큼 떨어졌으면 언제 반등할지 눈여겨봐야한다.
                        _CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
                        _SelToBeCompensatedBottom = _SelTotal             '보상될 매도수량 초기값
                    End If
                Else
                    '하락하지 않았음
                    _SellSuperiorCount = 0
                    _SelTotal = 0           '누적 매도 수량 reset
                    RecordList.Clear()      '레코드 리스트 클리어
                End If
            ElseIf _CurrentPhase = SearchPhase.WAIT_RISING Then
                '반등 기다리기 모드
                _SelTotal += CType(basic_strategy_str.SelDelta, Int64) - CType(basic_strategy_str.BuyDelta, Int64)
                _SelToBeCompensatedBottom = Math.Max(_SelToBeCompensatedBottom, _SelTotal)
                If _SelTotal < _SelToBeCompensatedBottom * _RISING_THRESHOLD_RATE Then
                    '많이 올랐다.
                    _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                    _EnterPrice = a_data.Price                  '진입가
                    _SelToBeCompensatedTop = _SelTotal          '보상될 매도수량 초기값 (누적매도량 최고값 유지하도록 함)
                End If
            ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
                '청산 기다리기 모드
                _SelTotal += CType(basic_strategy_str.SelDelta, Int64) - CType(basic_strategy_str.BuyDelta, Int64)
                _SelToBeCompensatedTop = Math.Min(_SelToBeCompensatedTop, _SelTotal)
                If _SelTotal - _SelToBeCompensatedTop > (_SelToBeCompensatedBottom - _SelToBeCompensatedTop) * _EXIT_THRESHOLD_RATE Then
                    '얼추 내렸다.
                    _CurrentPhase = SearchPhase.DONE
                    _ExitPrice = a_data.Price                   '청산가
                    Dim a = 1
                    '_SellSuperiorCount = 0
                    '_SelTotal = 0           '누적 매도 수량 reset
                    'RecordList.Clear()      '레코드 리스트 클리어

                End If
            End If
        Else
            '델타는 현재 모으고 있음. 기존 값을 그대로 사용
            If RecordList.Count = 0 Then
                basic_strategy_str.BuyDelta = 0
                basic_strategy_str.SelDelta = 0
            Else
                basic_strategy_str.BuyDelta = RecordList.Last.BuyDelta
                basic_strategy_str.SelDelta = RecordList.Last.SelDelta
            End If
        End If
        _DeltaCalcCount += 1    ' delta계산에 필요한 counter를 증가시킨다.


        RecordList.Add(basic_strategy_str)          '하나의 record완성
        'If RecordList.Count > _DecisionWIndowSize Then
        'RecordList.RemoveAt(0)          '오래된 것부터 지워버린다.
        'End If


        If _SellSuperiorCount > MA_Base / 2 Then

        End If
    End Sub

End Class
#End If

Public Class c051_BasicDecisionMaker
    Inherits c050_DecisionMaker

    '데이터 구조
    Public Structure BasicStrategy
        Dim MAPrice As UInt32
        Dim BuyMiniSum As UInt64    '델타 매수거래량 소계
        Dim SelMiniSum As UInt64    '델타 매도거래량 소계
        Dim Rate As Double
        Dim BuyDelta As UInt64      '델타 매수거래량
        Dim SelDelta As UInt64      '델타 매도거래량
    End Structure

    '써치 페이즈
    Public Enum SearchPhase
        WAIT_FALLING
        WAIT_RISING
        WAIT_EXIT_TIME
        DONE
    End Enum

    Private Const _DELTA_SUM_COUNT As Integer = 4       '1분간의 amount를 누적하여 delta 값 계산
    Private Const _RISING_THRESHOLD_RATE As Double = 0.7       '총 떨어진 양의 30%를 복구하면 반등으로 판단.
    Private Const _EXIT_THRESHOLD_RATE As Double = 1.3       '누적매도수량 최대값의 30%를 복구하면 청산시점으로 판단.
    Private Const _SELL_SUPERIOR_THRESHOLD As Integer = 20
    Public RecordList As New List(Of BasicStrategy)
    Private _BuyAmountOld As UInt64
    Private _SelAmountOld As UInt64
    Private _DeltaCalcCount As Integer
    Private _SellSuperiorCount As Integer
    Private _WaitRisingCount As Integer
    Private _CurrentPhase As SearchPhase
    Private _SelTotal As Integer
    Private _SelToBeCompensatedBottom As Integer
    Private _SelToBeCompensatedTop As Integer
    'Private _BuyTotal As Integer

    Public Sub New(ByVal linked_symbol As c03_Symbol, ByVal start_time As DateTime)
        MyBase.New(linked_symbol, start_time)

        _DeltaCalcCount = 0
        _DecisionWindowSize = 32
        _CurrentPhase = SearchPhase.WAIT_FALLING
    End Sub

    '새 데이터 도착
    Public Overrides Sub DataArrived(ByVal a_data As c03_Symbol.SymbolRecord)
        Dim basic_strategy_str As BasicStrategy
        Dim diff As Int64

        basic_strategy_str.MAPrice = a_data.MAPrice     '이동평균 값 저장
        If _DeltaCalcCount = _DELTA_SUM_COUNT Then
            '델타 계산해서 저장
            diff = CType(a_data.BuyAmount, Int64) - CType(_BuyAmountOld, Int64)
            If diff < 0 Then
                '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
                basic_strategy_str.BuyDelta = 0
            Else
                '정상적인 델타매수량
                basic_strategy_str.BuyDelta = diff
            End If
            diff = CType(a_data.SelAmount, Int64) - CType(_SelAmountOld, Int64)
            If diff < 0 Then
                '체결강도의 정밀도 부족으로 이런 계산의 부정확성이 발생할 수 있다
                basic_strategy_str.SelDelta = 0
            Else
                '정상적인 델타매수량
                basic_strategy_str.SelDelta = diff
            End If
            '매도/매수 미니섬, 거래량비율 계산
            If RecordList.Count > 0 Then
                basic_strategy_str.BuyMiniSum = RecordList.Last.BuyMiniSum + basic_strategy_str.BuyDelta
                basic_strategy_str.SelMiniSum = RecordList.Last.SelMiniSum + basic_strategy_str.SelDelta
            Else
                basic_strategy_str.BuyMiniSum = basic_strategy_str.BuyDelta
                basic_strategy_str.SelMiniSum = basic_strategy_str.SelDelta
            End If
            If basic_strategy_str.SelMiniSum + basic_strategy_str.BuyMiniSum = 0 Then
                basic_strategy_str.Rate = 0
            Else
                basic_strategy_str.Rate = CType(basic_strategy_str.BuyMiniSum, Double) / (CType(basic_strategy_str.SelMiniSum, Double) + basic_strategy_str.BuyMiniSum)
            End If
            'old amount값을 저장해둔다.
            _BuyAmountOld = a_data.BuyAmount
            _SelAmountOld = a_data.SelAmount
            _DeltaCalcCount = 0     'delta 계산용 카운터를 리셋한다.
        Else
            '델타는 현재 모으고 있음. 기존 값을 그대로 사용
            If RecordList.Count = 0 Then
                basic_strategy_str.BuyDelta = 0
                basic_strategy_str.SelDelta = 0
                basic_strategy_str.BuyMiniSum = 0
                basic_strategy_str.SelMiniSum = 0
                basic_strategy_str.Rate = 0
            Else
                basic_strategy_str.BuyDelta = RecordList.Last.BuyDelta
                basic_strategy_str.SelDelta = RecordList.Last.SelDelta
                basic_strategy_str.BuyMiniSum = RecordList.Last.BuyMiniSum
                basic_strategy_str.SelMiniSum = RecordList.Last.SelMiniSum
                basic_strategy_str.Rate = RecordList.Last.Rate
            End If
        End If
        _DeltaCalcCount += 1    ' delta계산에 필요한 counter를 증가시킨다.

        If _CurrentPhase = SearchPhase.WAIT_FALLING Then
            '하락 기다리기 모드
            If RecordList.Count > 0 AndAlso basic_strategy_str.MAPrice < RecordList.Last.MAPrice Then 'AndAlso (basic_strategy_str.BuyDelta + basic_strategy_str.SelDelta) > (RecordList.Last.BuyDelta + RecordList.Last.SelDelta) Then
                '이동평균가가 하락하고 있고 델타 거래량이 증가하고 있다.
                _SellSuperiorCount += 1
                If _SellSuperiorCount > _SELL_SUPERIOR_THRESHOLD Then
                    '이만큼 떨어졌으면 언제 반등할지 눈여겨봐야한다.
                    _CurrentPhase = SearchPhase.WAIT_RISING     '반등기다리기 모드로 변경
                End If
            Else
                '하락하지 않았음
                _SellSuperiorCount = 0
                StartTime = StartTime + TimeSpan.FromSeconds(RecordList.Count * 15)
                RecordList.Clear()      '레코드 리스트 클리어
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_RISING Then
            '반등 기다리기 모드
            If (basic_strategy_str.MAPrice + RecordList(RecordList.Count - 2).MAPrice) > 2 * RecordList(RecordList.Count - 1).MAPrice AndAlso _
                (basic_strategy_str.MAPrice + RecordList(RecordList.Count - 4).MAPrice) > 2 * RecordList(RecordList.Count - 2).MAPrice AndAlso _
                (basic_strategy_str.MAPrice + RecordList(RecordList.Count - 8).MAPrice) > 2 * RecordList(RecordList.Count - 4).MAPrice AndAlso _
                (basic_strategy_str.Rate + RecordList(RecordList.Count - 2).Rate > 2 * RecordList(RecordList.Count - 1).Rate) AndAlso _
                (basic_strategy_str.Rate + RecordList(RecordList.Count - 4).Rate > 2 * RecordList(RecordList.Count - 2).Rate) AndAlso _
                (basic_strategy_str.Rate + RecordList(RecordList.Count - 8).Rate > 2 * RecordList(RecordList.Count - 4).Rate) Then
                '많이 올랐다.
                _CurrentPhase = SearchPhase.WAIT_EXIT_TIME
                EnterPrice = a_data.Price                  '진입가
            End If
        ElseIf _CurrentPhase = SearchPhase.WAIT_EXIT_TIME Then
            '청산 기다리기 모드
            If (basic_strategy_str.MAPrice + RecordList(RecordList.Count - 2).MAPrice) < 2 * RecordList(RecordList.Count - 1).MAPrice AndAlso _
                (basic_strategy_str.MAPrice + RecordList(RecordList.Count - 4).MAPrice) < 2 * RecordList(RecordList.Count - 2).MAPrice AndAlso _
                (basic_strategy_str.Rate + RecordList(RecordList.Count - 2).Rate < 2 * RecordList(RecordList.Count - 1).Rate) AndAlso _
                (basic_strategy_str.Rate + RecordList(RecordList.Count - 4).Rate < 2 * RecordList(RecordList.Count - 2).Rate) Then
                '얼추 내렸다.
                _CurrentPhase = SearchPhase.DONE
                ExitPrice = a_data.Price                   '청산가
                Profit = (ExitPrice - EnterPrice) * 100 / EnterPrice        '수익률
                TookTime = TimeSpan.FromSeconds(RecordList.Count * 15)      '걸린 시간
                _Done = True                             '청산완료 알리는 비트 셋
                '_SellSuperiorCount = 0
                '_SelTotal = 0           '누적 매도 수량 reset
                'RecordList.Clear()      '레코드 리스트 클리어

            End If
        End If

        RecordList.Add(basic_strategy_str)          '하나의 record완성
        'If RecordList.Count > _DecisionWIndowSize Then
        'RecordList.RemoveAt(0)          '오래된 것부터 지워버린다.
        'End If
    End Sub

End Class
