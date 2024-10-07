Imports System.Net.Security
Imports XA_DATASETLib

Public Class et2_Operation
    Public Enum OperationState
        OS_INITIALIZED
        OS_ORDER_REQUESTED
        OS_WAIT_UNTIL_EXIT_REQUEST
        OS_CHECKING_PRICE
        OS_ERROR
        OS_CANCEL_BUYING
        OS_STOP_BUYING
        OS_LOW_LIMIT_CAUTTION
        OS_LOW_LIMIT_CAUTTION_CANCEL
        OS_DONE
    End Enum
    '130710:다 본 것 같다. 이제 테스트해도 되는 걸까?..............................................................................................
    'Public Shared BUYABLE_PRICE_RATE As Double = 0.008  '진입가 기준 위아래 허용 진입가 변동
    'Public Shared QUIET_OPERATION_RATE As Double = 0.15     '고요수율. 전체 매수/매도 가능 수량 중 이번에 들어갈 수량 비율
    Public Shared NEXT_ORDER_COUNT As Integer = 750          '(청산시) 다음 주문까지 기다리는 카운트 = 2분30초
    'Public Shared LOW_LIMIT_CAUTION_RATE As Double = 0        '하한가에 얼만큼 가까워졌을 때 팔아버리냐 기준
    'Public Shared ALLOWED_DEAL_LOSS As Double = 0.006           '체결을 위해 감수하는 로스의 상한선
    Public Shared LIQ_COUNT As Integer = 1                  '청산시 나눠파는 횟수
    'Public Shared IGNORE_AMOUNT_RATE As Double = 0.1
    Public Shared DELAY_FISHING_RATIO As Double = 0.997     '지연낚시 전략에서 지연가격의 몇 % 위치에 fishing line 을 만들건지 결정

    '    Public Shared STANDBY_MIN_COUNT As Integer = 2              '다음 request 까지 대기시간 미니멈
    'Public Shared STANDBY_MAX_COUNT As Integer = 15              '다음 request 까지 대기시간 맥시멈
    'Private Shared _STANDBY_TIME_FOR_ALERT As Integer = 1500    '데드라인 넘겼을 때 대기시간 5분

    'Private WithEvents _H1_ As XAReal
    'Private WithEvents _HA_ As XAReal
    'Private WithEvents _T1101 As New XAQuery
    Private _AccountManager As et1_AccountManager
    Public SymbolCode As String
    Public LinkedSymbol As c03_Symbol
    Private _DecisionMaker As c050_DecisionMaker ' c05C_NoUnidelta_DecisionMaker
    '    Public ThisOrderAmount As Integer
    'Public FallVolume As UInt64
    'Public TargetAmount As UInt32
    'Public TotalAmountBought As Integer         '매수 강제 중단시까지 혹은 타겟 매수수량 다 채울 때까지 총 매수한 량
    Public _OpStatus As OperationState
    Public EnterExitState As EnterOrExit
    'Public CSPAQ03700_Data As New List(Of String)
    Public MarketKind As MARKET_KIND
    'Private _StandbyCount As Integer = 0
    'Private _StandbyLimit As Integer = 0
    'Private _RandomG As New Random
    'Private _LastCallPrices As CallPrices
    Public InitPrice As UInt32
    Public BuyStandardPrice As UInt32
    'Private _CuttingNumber As Integer = 0
    'Private _DeadlineCountdown As Integer = 0
    'Private _StandbyAlert As Boolean = False
    'Public EnterZeroPrice As Double
    'Public EnterDealPrice As Double
    'Public ExitDealPrice As Double
    'Public LowLimitPrice As Double
    Public OperatorList As New List(Of et21_Operator)
    Public _ClockCount As Integer = 0
    Public ThisAmount As UInt32 = 0         '체결 미체결 합한 수량
    Public BoughtAmount As UInt32 = 0
    Public SoldAmount As UInt32 = 0
    Public BuyDealPrice As Double = 0
    Public SelDealPrice As Double = 0
    Public RestOfBoughtAmount As UInt32
    Public Liquount As Integer = 0          '나눠서 청산하는 카운트
    Public EarlyExit As Boolean         '조기 매도
    Public OperationKey As TracingKey
    Public IgnoreAmountRateForBuy As Double = 0.1
    Public IgnoreAmountRateForSel As Double = 0.1
    Public BuyFailCount As Integer = 0
    'Public AssignedMoney As Long = 0
    'Public CallPriceKey As Integer
    'Public OperationKey As Integer
    Public DebugDONEDONE As String
    Public DelayFishing As Boolean = False

    '[OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public ReadOnly Property OpStatus
        Get
            Dim op_status As OperationState
            op_status = _OpStatus
            Return op_status
        End Get
    End Property

    '----------------------------------------------------------------------------------------------------------------------------┘
    '초기호가를 모를 때 생성자
    Public Sub New(ByVal decision_maker As c050_DecisionMaker, ByVal buy_standard_price As UInt32, ByVal order_amount As UInt32, ByVal account_manager As et1_AccountManager, ByVal is_restoring As Boolean)
        '131018:EnterPrice와 FallVolume(silent rate 곱하지 말고)를 넘겨주고 넘겨받도록 한다.
        _OpStatus = OperationState.OS_INITIALIZED
        'FallVolume = fall_volume
        'TargetAmount = target_amount
        'RestOfTargetAmount = target_amount
        BuyStandardPrice = buy_standard_price
        _DecisionMaker = decision_maker
        _AccountManager = account_manager
        If account_manager.AccountCat = 0 Then  'Main (Double Fall)
            IgnoreAmountRateForBuy = MAIN_IGNORE_AMOUNT_RATE_FOR_BUY
            IgnoreAmountRateForSel = MAIN_IGNORE_AMOUNT_RATE_FOR_SEL
        ElseIf account_manager.AccountCat = 1 Then  'Sub (Moving Average)
            IgnoreAmountRateForBuy = SUB_IGNORE_AMOUNT_RATE_FOR_BUY
            IgnoreAmountRateForSel = SUB_IGNORE_AMOUNT_RATE_FOR_SEL
        ElseIf account_manager.AccountCat = 2 Then  'Test (PCRenew)
            IgnoreAmountRateForBuy = TEST_IGNORE_AMOUNT_RATE_FOR_BUY
            IgnoreAmountRateForSel = TEST_IGNORE_AMOUNT_RATE_FOR_SEL
        End If

        Dim _symbol As c03_Symbol = decision_maker.LinkedSymbol
        LinkedSymbol = _symbol
        If _symbol.Code.StartsWith("A") Then
            SymbolCode = _symbol.Code.Substring(1)
        Else
            SymbolCode = _symbol.Code           '종목번호 기억
        End If
        MarketKind = _symbol.MarketKind            '마켓 종류 기억
        EnterExitState = EnterOrExit.EOE_Enter          '매수 중임을 표시함

        'EnterZeroPrice = init_price             '나중에 실시간 가격변동확인 때 확인할 진입가

        SafeEnterTrace(OperationKey, 122)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '주문해보자
        _OpStatus = OperationState.OS_ORDER_REQUESTED            '주문한 상태로 바꿈

        If order_amount > 0 Then
            If Not is_restoring Then
                '180113: 아래는 최초 매수가 설정
                Dim best_buy_price As UInt32 = GetBestBuyPrice(0, 0, 0)

                If best_buy_price < buy_standard_price Then
                    InitPrice = best_buy_price
                Else
                    InitPrice = NextCallPrice(buy_standard_price, 0)
                End If

                OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Enter, InitPrice, order_amount, _AccountManager))
            Else
                InitPrice = buy_standard_price
            End If
            ThisAmount = order_amount
        End If
        SafeLeaveTrace(OperationKey, 126)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        _ClockCount = 0                                     '주문한 것이 일정시간동안 체결안되면 취소하기 위해 타이머 돌리기 위해 존재함

        '가격조회TR 초기화
        '_T1101.ResFileName = "Res\t1101.res"
        '_T1101.SetFieldData("t1101InBlock", "shcode", 0, SymbolCode)

        ' 데이터 요청
        'If _T1101.Request(False) = False Then
        'OpStatus = OperationState.OS_ERROR
        'ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "대기시간 종료 후 호가요청 Fail")
        'Else
        'OpStatus = OperationState.OS_CHECKING_PRICE
        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "첫번째 호가요청 성공")
        'End If


        'Dim safe_rate As Double = (enter_price - _LinkedSymbol.LowLimitPrice) / (CAREFULL_FACTOR * (_LinkedSymbol.YesterPrice - _LinkedSymbol.LowLimitPrice))
        'If safe_rate > 1 Then
        'safe_rate = 1
        'ElseIf safe_rate < 0 Then
        'safe_rate = 0
        'End If
        'Dim main_rate As Double = MAIN_FACTOR * safe_rate
        'Dim possible_order_volume As Double

        'possible_order_volume = main_rate * MainForm.AccountManager.TotalProperty * 100 / _LinkedSymbol.EvidanRate   '주문가능금액  대신 총자산을 사용하기로 한다(주문마다 안 변하는 게 나을 것 같다)
        'Dim fall_volume_standard As Double = FallVolume * SILENT_INVOLVING_AMOUNT_RATE
        'Dim final_order_amount As UInt32 = Math.Round(Math.Min(possible_order_volume, fall_volume_standard) / enter_price)

        'If _LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And enter_price < 50000 Then
        'final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
        'End If
        'MessageLogging(_LinkedSymbol.Code & " :" & "safe rate " & safe_rate.ToString)

        'TargetAmount = final_order_amount       '우선 생성초기에 계산된 수량을 저장한다(0이라도 어쩔 수 없다.). 이 수치는 시간이 지나면서 업데이트될 것이다.
        'MessageLogging(_LinkedSymbol.Code & " :" & "계산된 target amount " & TargetAmount.ToString)
        '131021 : 이거 불러주는 데 정리좀 하고.. 호가 받고 주문하는데로 넘어가보자.
    End Sub

    Public Sub Terminate()
        For Each an_operator In OperatorList
            an_operator.Terminate()
        Next
    End Sub

    Public Function GetBestBuyPrice(ByVal current_order_price As UInt32, ByVal rest_amount As UInt32, ByVal current_amount_under_order As UInt32) As UInt32
        If DelayFishing Then
            '2024.07.07 : 지연낚시 전략은 기존 ignorable amount 에 의한 best buy price 를 대체한다.
            Dim delayed_price_buy As UInt32 = LinkedSymbol.DelayedPriceBuy
            Dim fishing_line As UInt32 = NextCallPrice(delayed_price_buy * DELAY_FISHING_RATIO, 0)
            Return fishing_line
        Else
            Dim local_call_price As CallPrices = _DecisionMaker.LinkedSymbol.LastCallPrices
            Dim sel_ave_amount As UInt64 = _DecisionMaker.LinkedSymbol.SelAveAmount
            Dim ignorable_amount As Int64 = _DecisionMaker.BasicIgnorableAmount + 2 * sel_ave_amount * IgnoreAmountRateForBuy
            Dim order_price_order As UInt16 = 0 '2024.03.28 : 어느 호가에 주문을 넣었느냐. 1=>1호가, 2=>2호가 ..
            Dim amount_over_my_order As UInt32 = 0  '2024.03.28 : 내가 주문한 것 포함하여 그 위에 쌓인 수량은 얼마나 되는가 '2024.04.08 : 내가 주문한 거 빼는 걸로 수정됨

            '이전 order 냈던 위치에 그 order보다 먼저 있던 amount를 고려한 계산
            Select Case current_order_price
                Case local_call_price.BuyPrice1
                    order_price_order = 1
                    If local_call_price.BuyAmount1 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                        local_call_price.BuyAmount1 -= rest_amount
                    Else
                        local_call_price.BuyAmount1 = 0
                    End If
                    If local_call_price.BuyAmount1 > current_amount_under_order Then
                        amount_over_my_order = local_call_price.BuyAmount1 - current_amount_under_order
                        local_call_price.BuyAmount1 = current_amount_under_order
                    Else 'If local_call_price.BuyAmount1 <= current_amount_under_order Then
                        '2020.0822 : 이건 매수되어 없어졌다는 뜻. 1호가만 해당되고 2호가 이상은 해당되지 않음. 내 order 위에 쌓인 것들은 확인할 수 없지만 굳이 BuyAmount1을 늘릴 필요는 없다.=>BuyAmount1은 그대로 둔다.
                        '2024.03.28 : 2020 년에 무슨 생각을 했던 건지 알 수 없다. 로직을 전면 뜯어 고친다.
                        'local_call_price.BuyAmount1 = current_amount_under_order
                    End If
                Case local_call_price.BuyPrice2
                    order_price_order = 2
                    If local_call_price.BuyAmount2 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                        local_call_price.BuyAmount2 -= rest_amount
                    Else
                        local_call_price.BuyAmount2 = 0
                    End If
                    If local_call_price.BuyAmount2 > current_amount_under_order Then
                        amount_over_my_order = local_call_price.BuyAmount2 - current_amount_under_order
                        local_call_price.BuyAmount2 = current_amount_under_order    '2024.03.28 : else case 에서는 할 필요가 없다는 것을 깨달아서 if 문 밖에서 안으로 들어왔다.
                    End If
                Case local_call_price.BuyPrice3
                    order_price_order = 3
                    If local_call_price.BuyAmount3 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                        local_call_price.BuyAmount3 -= rest_amount
                    Else
                        local_call_price.BuyAmount3 = 0
                    End If
                    If local_call_price.BuyAmount3 > current_amount_under_order Then
                        amount_over_my_order = local_call_price.BuyAmount3 - current_amount_under_order
                        local_call_price.BuyAmount3 = current_amount_under_order
                    End If
                Case local_call_price.BuyPrice4
                    order_price_order = 4
                    If local_call_price.BuyAmount4 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                        local_call_price.BuyAmount4 -= rest_amount
                    Else
                        local_call_price.BuyAmount4 = 0
                    End If
                    If local_call_price.BuyAmount4 > current_amount_under_order Then
                        amount_over_my_order = local_call_price.BuyAmount4 - current_amount_under_order
                        local_call_price.BuyAmount4 = current_amount_under_order
                    End If
                Case local_call_price.BuyPrice5
                    order_price_order = 5
                    If local_call_price.BuyAmount5 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                        local_call_price.BuyAmount5 -= rest_amount
                    Else
                        local_call_price.BuyAmount5 = 0
                    End If
                    If local_call_price.BuyAmount5 > current_amount_under_order Then
                        amount_over_my_order = local_call_price.BuyAmount5 - current_amount_under_order
                        local_call_price.BuyAmount5 = current_amount_under_order
                    End If
                Case Else
                    '6차 이상으로 올라갔다는 얘기밖에는 안 된다.
                    '2024.07.05 : 혹은 처음 order 내는 경우일 수도 있다.
            End Select

            If local_call_price.BuyPrice1 = 0 Then
                '사지 않아야 한다. 일부러 범위밖의 값을 리턴한다.
                Return NextCallPrice(LinkedSymbol.LowLimitPrice, -1)
            Else
                '1호가
                ignorable_amount -= local_call_price.BuyAmount1
                If ignorable_amount < 0 Then
                    '한 칸 전진한다
                    Return NextCallPrice(local_call_price.BuyPrice1, 1)
                End If
                If order_price_order = 1 Then
                    ignorable_amount -= amount_over_my_order
                    If ignorable_amount < 0 OrElse local_call_price.BuyPrice2 = 0 Then
                        '현상유지다
                        Return NextCallPrice(local_call_price.BuyPrice1, 0)
                    Else
                        '다음 호가로 넘어간다.
                    End If
                End If
                If local_call_price.BuyPrice2 = 0 Then
                    Return local_call_price.BuyPrice1
                Else
                    '2호가
                    ignorable_amount -= local_call_price.BuyAmount2
                    If ignorable_amount < 0 Then
                        '한 칸 전진한다
                        Return NextCallPrice(local_call_price.BuyPrice2, 1)
                    End If
                    If order_price_order = 2 Then
                        ignorable_amount -= amount_over_my_order
                        If ignorable_amount < 0 OrElse local_call_price.BuyPrice3 = 0 Then
                            '현상유지다
                            Return NextCallPrice(local_call_price.BuyPrice2, 0)
                        Else
                            '다음 호가로 넘어간다.
                        End If
                    End If
                    If local_call_price.BuyPrice3 = 0 Then
                        Return local_call_price.BuyPrice2
                    Else
                        '3호가
                        ignorable_amount -= local_call_price.BuyAmount3
                        If ignorable_amount < 0 Then
                            '한 칸 전진한다
                            Return NextCallPrice(local_call_price.BuyPrice3, 1)
                        End If
                        If order_price_order = 3 Then
                            ignorable_amount -= amount_over_my_order
                            If ignorable_amount < 0 OrElse local_call_price.BuyPrice4 = 0 Then
                                '현상유지다
                                Return NextCallPrice(local_call_price.BuyPrice3, 0)
                            Else
                                '다음 호가로 넘어간다.
                            End If
                        End If
                        If local_call_price.BuyPrice4 = 0 Then
                            Return local_call_price.BuyPrice3
                        Else
                            '4호가
                            ignorable_amount -= local_call_price.BuyAmount4
                            If ignorable_amount < 0 Then
                                '한 칸 전진한다
                                Return NextCallPrice(local_call_price.BuyPrice4, 1)
                            End If
                            If order_price_order = 4 Then
                                ignorable_amount -= amount_over_my_order
                                If ignorable_amount < 0 OrElse local_call_price.BuyPrice5 = 0 Then
                                    '현상유지다
                                    Return NextCallPrice(local_call_price.BuyPrice4, 0)
                                Else
                                    '다음 호가로 넘어간다.
                                End If
                            End If
                            If local_call_price.BuyPrice5 = 0 Then
                                Return local_call_price.BuyPrice4
                            Else
                                '5호가
                                ignorable_amount -= local_call_price.BuyAmount5
                                If ignorable_amount < 0 Then
                                    '한 칸 전진한다
                                    Return NextCallPrice(local_call_price.BuyPrice5, 1)
                                End If
                                If order_price_order = 5 Then
                                    ignorable_amount -= amount_over_my_order
                                    If ignorable_amount < 0 OrElse local_call_price.BuyPrice6 = 0 Then
                                        '현상유지다
                                        Return NextCallPrice(local_call_price.BuyPrice5, 0)
                                    Else
                                        '다음 호가로 넘어간다. => ..는 없다.
                                    End If
                                End If
                                'ignorable amount 가 5호가까지 뚫은 진귀한 상황이다. => 6호가로 한다.
                                Return NextCallPrice(local_call_price.BuyPrice6, 0)
                            End If
                        End If
                    End If
                End If
            End If
        End If
    End Function

    '180221: 상하한가 근처에서 SelAmount와 SelPrice가 0이 될 때를 고려해야 한다.
    Public Function GetBestSelPrice(ByVal current_order_price As UInt32, ByVal rest_amount As UInt32, ByVal current_amount_under_order As UInt32) As UInt32
        Dim local_call_price As CallPrices = _DecisionMaker.LinkedSymbol.LastCallPrices
        Dim buy_ave_amount As UInt64 = _DecisionMaker.LinkedSymbol.BuyAveAmount
        Dim ignorable_amount As Int64 = _DecisionMaker.BasicIgnorableAmount + 2 * buy_ave_amount * IgnoreAmountRateForSel
        Dim order_price_order As UInt16 = 0 '2024.03.28 : 어느 호가에 주문을 넣었느냐. 1=>1호가, 2=>2호가 ..
        Dim amount_over_my_order As UInt32 = 0  '2024.03.28 : 내가 주문한 것 포함하여 그 위에 쌓인 수량은 얼마나 되는가 '2024.04.08 : 내가 주문한 거 빼는 걸로 수정됨

        '이전 order 냈던 위치에 그 order보다 먼저 있던 amount를 고려한 계산
        Select Case current_order_price
            Case local_call_price.SelPrice1
                order_price_order = 1
                If local_call_price.SelAmount1 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                    local_call_price.SelAmount1 -= rest_amount
                Else
                    local_call_price.SelAmount1 = 0
                End If
                If local_call_price.SelAmount1 > current_amount_under_order Then
                    amount_over_my_order = local_call_price.SelAmount1 - current_amount_under_order
                    local_call_price.SelAmount1 = current_amount_under_order
                Else 'If local_call_price.SelAmount1 <= current_amount_under_order Then
                    '2020.0822 : 이건 매도되어 없어졌다는 뜻. 1호가만 해당되고 2호가 이상은 해당되지 않음. 내 order 위에 쌓인 것들은 확인할 수 없지만 굳이 SelAmount1을 늘릴 필요는 없다.=>SelAmount1은 그대로 둔다.
                    'local_call_price.SelAmount1 = current_amount_under_order
                End If
            Case local_call_price.SelPrice2
                order_price_order = 2
                If local_call_price.SelAmount2 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                    local_call_price.SelAmount2 -= rest_amount
                Else
                    local_call_price.SelAmount2 = 0
                End If
                If local_call_price.SelAmount2 > current_amount_under_order Then
                    amount_over_my_order = local_call_price.SelAmount2 - current_amount_under_order
                    local_call_price.SelAmount2 = current_amount_under_order
                End If
            Case local_call_price.SelPrice3
                order_price_order = 3
                If local_call_price.SelAmount3 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                    local_call_price.SelAmount3 -= rest_amount
                Else
                    local_call_price.SelAmount3 = 0
                End If
                If local_call_price.SelAmount3 > current_amount_under_order Then
                    amount_over_my_order = local_call_price.SelAmount3 - current_amount_under_order
                    local_call_price.SelAmount3 = current_amount_under_order
                End If
            Case local_call_price.SelPrice4
                order_price_order = 4
                If local_call_price.SelAmount4 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                    local_call_price.SelAmount4 -= rest_amount
                Else
                    local_call_price.SelAmount4 = 0
                End If
                If local_call_price.SelAmount4 > current_amount_under_order Then
                    amount_over_my_order = local_call_price.SelAmount4 - current_amount_under_order
                    local_call_price.SelAmount4 = current_amount_under_order
                End If
            Case local_call_price.SelPrice5
                order_price_order = 5
                If local_call_price.SelAmount5 > rest_amount Then   '2024.04.08 : 내가 주문한 수량이 반영이 계산에 반영이 안 되어 있었는데 이제 포함시킨다.
                    local_call_price.SelAmount5 -= rest_amount
                Else
                    local_call_price.SelAmount5 = 0
                End If
                If local_call_price.SelAmount5 > current_amount_under_order Then
                    amount_over_my_order = local_call_price.SelAmount5 - current_amount_under_order
                    local_call_price.SelAmount5 = current_amount_under_order
                End If
            Case Else
                '6차 이상으로 올라갔다는 얘기밖에는 안 된다.
        End Select

        If local_call_price.SelPrice1 = 0 Then
            Return LinkedSymbol.LowLimitPrice
        Else
            '1호가
            ignorable_amount -= local_call_price.SelAmount1
            If ignorable_amount < 0 Then
                '한 칸 전진한다
                Return NextCallPrice(local_call_price.SelPrice1, -1)
            End If
            If order_price_order = 1 Then
                ignorable_amount -= amount_over_my_order
                If ignorable_amount < 0 OrElse local_call_price.SelPrice2 = 0 Then
                    '현상유지다
                    Return NextCallPrice(local_call_price.SelPrice1, 0)
                Else
                    '다음 호가로 넘어간다.
                End If
            End If
            If local_call_price.SelPrice2 = 0 Then
                Return local_call_price.SelPrice1
            Else
                '2호가
                ignorable_amount -= local_call_price.SelAmount2
                If ignorable_amount < 0 Then
                    '한 칸 전진한다
                    Return NextCallPrice(local_call_price.SelPrice2, -1)
                End If
                If order_price_order = 2 Then
                    ignorable_amount -= amount_over_my_order
                    If ignorable_amount < 0 OrElse local_call_price.SelPrice3 = 0 Then
                        '현상유지다
                        Return NextCallPrice(local_call_price.SelPrice2, 0)
                    Else
                        '다음 호가로 넘어간다.
                    End If
                End If
                If local_call_price.SelPrice3 = 0 Then
                    Return local_call_price.SelPrice2
                Else
                    '3호가
                    ignorable_amount -= local_call_price.SelAmount3
                    If ignorable_amount < 0 Then
                        '한 칸 전진한다
                        Return NextCallPrice(local_call_price.SelPrice3, -1)
                    End If
                    If order_price_order = 3 Then
                        ignorable_amount -= amount_over_my_order
                        If ignorable_amount < 0 OrElse local_call_price.SelPrice4 = 0 Then
                            '현상유지다
                            Return NextCallPrice(local_call_price.SelPrice3, 0)
                        Else
                            '다음 호가로 넘어간다.
                        End If
                    End If
                    If local_call_price.SelPrice4 = 0 Then
                        Return local_call_price.SelPrice3
                    Else
                        '4호가
                        ignorable_amount -= local_call_price.SelAmount4
                        If ignorable_amount < 0 Then
                            '한 칸 전진한다
                            Return NextCallPrice(local_call_price.SelPrice4, -1)
                        End If
                        If order_price_order = 4 Then
                            ignorable_amount -= amount_over_my_order
                            If ignorable_amount < 0 OrElse local_call_price.SelPrice5 = 0 Then
                                '현상유지다
                                Return NextCallPrice(local_call_price.SelPrice4, 0)
                            Else
                                '다음 호가로 넘어간다.
                            End If
                        End If
                        If local_call_price.SelPrice5 = 0 Then
                            Return local_call_price.SelPrice4
                        Else
                            '5호가
                            ignorable_amount -= local_call_price.SelAmount5
                            If ignorable_amount < 0 Then
                                '한 칸 전진한다
                                Return NextCallPrice(local_call_price.SelPrice5, -1)
                            End If
                            If order_price_order = 5 Then
                                ignorable_amount -= amount_over_my_order
                                If ignorable_amount < 0 OrElse local_call_price.SelPrice6 = 0 Then
                                    '현상유지다
                                    Return NextCallPrice(local_call_price.SelPrice5, 0)
                                Else
                                    '다음 호가로 넘어간다. => ..는 없다.
                                End If
                            End If
                            'ignorable amount 가 5호가까지 뚫은 진귀한 상황이다. => 6호가로 한다.
                            Return NextCallPrice(local_call_price.SelPrice6, 0)
                        End If
                    End If
                End If
            End If
        End If
    End Function

    '추가로 주문함
    Public Sub AdditionalOrder(ByVal enter_price As UInt32, ByVal order_amount As UInt32)
        '180206: 추가주문할 때도 매수1호가 활용해서 하는 것으로 변경. 더불어 additional order threshold 도 0.3에서 0.1로 바꿔서 더 활발한 매수 이루어지게 함.
        If EnterExitState = EnterOrExit.EOE_Enter Then
            Dim best_buy_price As UInt32 = GetBestBuyPrice(0, 0, 0)
            Dim order_price As UInt32

            BuyStandardPrice = enter_price  '2021.07.15: 추가매수 이후 재주문할 때 참고하도록 buy standard price를 업데이트한다.

            If best_buy_price < enter_price Then
                order_price = best_buy_price
            Else
                order_price = NextCallPrice(enter_price, 0)
            End If

            SafeEnterTrace(OperationKey, 129)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐

            If order_amount > 0 Then
                OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Enter, order_price, order_amount, _AccountManager))
                ThisAmount = ThisAmount + order_amount
            End If

            '2024.01.07 : STOP_BUYING 구현하다가 아래 부분이 들어가는 것이 맞을 것 같다고 판단되어 넣었다.
            _OpStatus = OperationState.OS_ORDER_REQUESTED

            SafeLeaveTrace(OperationKey, 132)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Else
            ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "Exit 중인데 추가매수오는 에러")
        End If
    End Sub

#If 0 Then
    '131023: T1101이나 실시간 가격정보에서 호가정보를 받으면 그걸 바탕으로 점수를 계산하는 루틴을 따로 만든다.
    '  T1101같은 경우는 계산된 점수를 바로 account manager가 반영할 수 있도록 이벤트 날려주고
    '  실시간 호가정보 같은 경우는 너무 자주 일어나니까 이벤트 날리지 말고 account manager의 주기 task 에 의해 점수 계산을 하도록 한다.
    '  그리고 계산된 점수를 바탕으로 account manager가 주문을 날린다.
    '  FallVolume은 5초마다 날라오는 decision maker의 가격정보에 의존하여 업데이트한다.
    '131024: Operation의 상태에서 OS_WAIT_UNTIL_EXIT_REQUEST 상태에서 다시 새 매수준비하는 상태로 돌아가는 transition에 대해 생각해보자.
    '131025: 실제 주문은 안 했지만 account manager로부터 주문을 기다리는 새로운 상태를 두어야 한다.
    '  그 전에 decision maker에서 pre-entering bit clear하는 조건도 생각해봐야 한다.
    '호가 받음
    Private Sub _T1101_ReceiveData(ByVal szTrCode As String) Handles _T1101.ReceiveData
        SafeEnterTrace(OrderNumberListKey, 7)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If OpStatus = OperationState.OS_CHECKING_PRICE Then

            _LastCallPrices.SelPrice1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho1", 0))
            _LastCallPrices.SelPrice2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho2", 0))
            _LastCallPrices.SelPrice3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho3", 0))
            _LastCallPrices.SelPrice4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho4", 0))
            _LastCallPrices.SelPrice5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerho5", 0))
            _LastCallPrices.SelAmount1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem1", 0))
            _LastCallPrices.SelAmount2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem2", 0))
            _LastCallPrices.SelAmount3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem3", 0))
            _LastCallPrices.SelAmount4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem4", 0))
            _LastCallPrices.SelAmount5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "offerrem5", 0))
            _LastCallPrices.BuyPrice1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho1", 0))
            _LastCallPrices.BuyPrice2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho2", 0))
            _LastCallPrices.BuyPrice3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho3", 0))
            _LastCallPrices.BuyPrice4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho4", 0))
            _LastCallPrices.BuyPrice5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidho5", 0))
            _LastCallPrices.BuyAmount1 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem1", 0))
            _LastCallPrices.BuyAmount2 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem2", 0))
            _LastCallPrices.BuyAmount3 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem3", 0))
            _LastCallPrices.BuyAmount4 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem4", 0))
            _LastCallPrices.BuyAmount5 = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "bidrem5", 0))
            LowLimitPrice = Convert.ToUInt32(_T1101.GetFieldData("t1101OutBlock", "dnlmtprice", 0))             '하한가 추출
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.SelPrice5.ToString & "   " & _LastCallPrices.SelAmount5.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.SelPrice4.ToString & "   " & _LastCallPrices.SelAmount4.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.SelPrice3.ToString & "   " & _LastCallPrices.SelAmount3.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.SelPrice2.ToString & "   " & _LastCallPrices.SelAmount2.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.SelPrice1.ToString & "   " & _LastCallPrices.SelAmount1.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "desired " & InitPrice.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.BuyPrice1.ToString & "   " & _LastCallPrices.BuyAmount1.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.BuyPrice2.ToString & "   " & _LastCallPrices.BuyAmount2.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.BuyPrice3.ToString & "   " & _LastCallPrices.BuyAmount3.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.BuyPrice4.ToString & "   " & _LastCallPrices.BuyAmount4.ToString)
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & _LastCallPrices.BuyPrice5.ToString & "   " & _LastCallPrices.BuyAmount5.ToString)

            If EnterExitState = EnterOrExit.EOE_Enter Then
                '세 가격으로 나눠 주문
                '살 수량 계산 (ramdom치 포함)
                'Dim random_varying As UInt32 = TargetAmount * 0.1
                'Dim amount_zero As UInt32 = TargetAmount / 3 + _RandomG.Next(-random_varying, random_varying)
                Dim amount_zero As UInt32 = TargetAmount
                'Dim amount_high As UInt32 = TargetAmount * 2 / 9 + _RandomG.Next(-random_varying, random_varying)
                'Dim amount_low As UInt32 = TargetAmount - amount_zero - amount_high

                '가격 결정
                Dim price_zero As UInt32
                'Dim price_high As UInt32
                'Dim price_low As UInt32
                Dim local_call_price As CallPrices = LastCallPrices
                If local_call_price.BuyPrice1 = 0 Then
                    '하한가 상황 => 사지 않는다.
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "하한가 상황 => 사지 않는다.")
                    _OpStatus = OperationState.OS_DONE        '완료함
                    SafeLeaveTrace(OrderNumberListKey, 17)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return                                      '돌아감
#If 0 Then
                ElseIf InitPrice > local_call_price.BuyPrice1 Then
                    '초기주문가가 매수호가들보다 비싸면 현재 가장비싼 매수호가보다 한단계 높은 호가로 주문
                    price_zero = NextCallPrice(local_call_price.BuyPrice1, 1)
                ElseIf InitPrice = local_call_price.BuyPrice1 Then
                    '정확히 매수1호가이면 그가격에 주문
                    'price_zero = local_call_price.BuyPrice1
                    price_zero = NextCallPrice(local_call_price.BuyPrice1, 1)       '너무 안 사게 되어서 매수호가보다 한 단계 높은 걸로 사는 걸로 바꿨다.
                Else
                    '매수1호가보다 작으면 초기주문가보다 1단계 높은 호가로 주문
                    price_zero = NextCallPrice(InitPrice, 1)
#End If
                Else
                    Dim price_stepped_up As UInt32 = NextCallPrice(local_call_price.BuyPrice1, 1)
                    If local_call_price.SelPrice1 > (1 + ALLOWED_DEAL_LOSS) * InitPrice Then
                        '로스를 감수하면서까지 살 수 있는 상황이 아니면 그냥 매수1호가보다 한단계 위 가격으로 주문한다.
                        price_zero = price_stepped_up
                    Else
                        '로스가 감수할 수 있는 수준이면 매도1호가로 주문한다.
                        price_zero = local_call_price.SelPrice1
                    End If
                End If
                'price_high = NextCallPrice(price_zero, 1)
                'price_low = NextCallPrice(price_zero, -1)

                '하한가에 따른 보정
                Dim low_limit_price As UInt32 = NextCallPrice(_LinkedSymbol.LowLimitPrice, 1)
                If price_zero <= low_limit_price Then
                    '하한가면 사지 않는다
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "High price가 하한가 보다도 작다 => 사지 않는다.")
                    OpStatus = OperationState.OS_DONE        '완료함
                    SafeLeaveTrace(OrderNumberListKey, 18)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return                                      '돌아감
#If 0 Then
                ElseIf price_zero <= low_limit_price Then
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "High price만 하한가보다 크다 => High만 사본다.")
                    amount_zero = 0
                    amount_low = 0
                    amount_high = TargetAmount
                ElseIf price_low <= low_limit_price Then
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "Low price만 하한가보다 작거나 같다 => Low는 사지 않는다.")
                    amount_low = 0
                    amount_zero = TargetAmount - amount_high
#End If
                End If

                '수량 10단위 짜르기
                If _LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And price_zero < 50000 Then
                    'low price 하나만 걸려도 10단위로 짜르자
                    'amount_high = Math.Floor(amount_high / 10) * 10       '10단위로 자름
                    amount_zero = Math.Floor(amount_zero / 10) * 10       '10단위로 자름
                    'amount_low = Math.Floor(TargetAmount - amount_high - amount_zero)   '10단위로 자름
                End If

                EnterZeroPrice = price_zero             '나중에 실시간 가격변동확인 때 확인할 진입가

                '주문해보자
                OpStatus = OperationState.OS_ORDER_REQUESTED            '주문한 상태로 바꿈

                If amount_zero > 0 Then
                    OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Enter, price_zero, amount_zero, _AccountManager))
                End If
                'If amount_high > 0 Then
                'OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Enter, price_high, amount_high, _AccountManager))
                'End If
                'If amount_low > 0 Then
                'OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Enter, price_low, amount_low, _AccountManager))
                'End If

                _ClockCount = 0                                     '주문한 것이 일정시간동안 체결안되면 취소하기 위해 타이머 돌리기 위해 존재함

                '실시간 호가 초기화
                If MarketKind = MARKET_KIND.MK_KOSPI Then
                    _H1_ = New XAReal
                    _H1_.ResFileName = "Res\H1_.res"
                    _H1_.SetFieldData("InBlock", "shcode", SymbolCode)
                    _H1_.AdviseRealData()           '데이터 요청
                Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
                    _HA_ = New XAReal
                    _HA_.ResFileName = "Res\HA_.res"
                    _HA_.SetFieldData("InBlock", "shcode", SymbolCode)
                    _HA_.AdviseRealData()           '데이터 요청
                End If

                MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "실시간 호가요청되었음")
            End If
        Else
            OpStatus = OperationState.OS_ERROR
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "엉뚱한 가격확인 메시지 들어왔음")
        End If
        SafeLeaveTrace(OrderNumberListKey, 19)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
    '130701:위에 단계까지 리뷰 완료.
#End If

    '[OperationKey] All Deal Confirmed event handler- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub AllDealConfirmedEventHandlerForOperator(ByVal the_operator As et21_Operator, ByVal deal_amount As UInt32)
        'SafeEnter(LinkedSymbol.OneKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
                ' 분할해서 매수 의뢰했던 게 매수 됐다고 들어옴
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        '130620: Operation에도 수익률 계산을 위한 DealPrice변수를 만들었다. 이게 잘 업데이트되도록 코드를 짜보자.
                        If BuyDealPrice = 0 Then
                            BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                        Else
                            Dim old_price As Double = BuyDealPrice
                            Dim new_price As Double = OperatorList(index).DealPrice
                            Dim old_amount As UInt32 = BoughtAmount
                            Dim new_amount As UInt32 = deal_amount 'OperatorList(index).ThisAmount
                            '평균체결가 계산
                            BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                        End If
                        BoughtAmount += deal_amount 'OperatorList(index).ThisAmount      '산 수량을 기록함
                        '매수 되었으니 해당 operator 삭제
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수체결완료")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next

                If OperatorList.Count = 0 Then
                    '다 샀다 => 청산기다림 모드로 전환
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "다 샀음. 청산기다림 모드로 전환.")
                    _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                End If
            ElseIf _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL OrElse _OpStatus = OperationState.OS_STOP_BUYING Then
                '매수 종료위해 강제 취소했는데 그 사이에 체결됨
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If the_operator.DealPrice <> 0 Then
                            '취소전 체결 수량이 있으면
                            If BuyDealPrice = 0 Then
                                BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                            Else
                                Dim old_price As Double = BuyDealPrice
                                Dim new_price As Double = OperatorList(index).DealPrice
                                Dim old_amount As UInt32 = BoughtAmount
                                Dim new_amount As UInt32 = deal_amount 'OperatorList(index).ThisAmount
                                '평균체결가 계산
                                BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                            End If
                            BoughtAmount += deal_amount 'OperatorList(index).ThisAmount      '산 수량을 기록함
                        End If
                        '매수 되었으니 해당 operator 삭제
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":강제취소했는데 매수체결완료")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next

                If _OpStatus = OperationState.OS_STOP_BUYING Then
                    If OperatorList.Count = 0 Then
                        '2024.01.07 : 매수중단했는데 체결됨. 상태를 OS_WAIT_UNTIL_EXIT_REQUEST 로 바꾸고 기다림
                        '다 샀다 => 청산기다림 모드로 전환
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "안 사려고 했지만 다 사졌음. 청산기다림 모드로 전환.")
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    Else
                        '아직 안 끝난 operator들이 있으니 끝날 때까지 기다리자.
                    End If
                Else 'If _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL
                    If OperatorList.Count = 0 Then
                        '어쨌든 매수 시도가 끝났다
                        If BoughtAmount > 0 Then
                            '130702 : 여기서 바로 매도작업 들어가는 게 맞는 것 같다. 왜냐면 이미 매도 요청 들어온 상태니까.
                            '산 수량이 하나라도 있으면 바로 청산작업 시작

                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "바로청산")
                            '청산 작업
                            If _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL Then
                                _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                Liquidate(1)
                            Else
                                _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                Liquidate(0)
                            End If
                        Else
                            '하나도 못 사졌다면 => 종료함
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산할 것도 없음. 종료함.")
                            EnterExitState = EnterOrExit.EOE_Exit   '청산모드로 바꿈
                            _OpStatus = OperationState.OS_DONE
                            DebugDONEDONE = DebugDONEDONE & "1"
                        End If
                    End If
                End If
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 매수 체결 이벤트 들어왔음")
            End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED Or _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION Then
                ' 매도 의뢰했던 게 체결되었다고 들어옴
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If SelDealPrice = 0 Then
                            SelDealPrice = OperatorList(index).DealPrice        '판 가격을 기록함
                        Else
                            Dim old_price As Double = SelDealPrice
                            Dim new_price As Double = OperatorList(index).DealPrice
                            Dim old_amount As UInt32 = SoldAmount
                            Dim new_amount As UInt32 = deal_amount ' OperatorList(index).ThisAmount
                            '평균체결가 계산
                            SelDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                        End If
                        SoldAmount += deal_amount 'OperatorList(index).ThisAmount      '판 수량을 기록함
                        '매도 되었으니 해당 operator 삭제
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매도체결완료")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next

                If Liquount = 0 AndAlso OperatorList.Count = 0 Then
                    '다 팔았다 => 정상 종료함
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "다 팔았음. 정상 종료함.")
                    _OpStatus = OperationState.OS_DONE
                    DebugDONEDONE = DebugDONEDONE & "2"
                End If
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 매도 체결 이벤트 들어왔음")
            End If
            '130515: 여긴 대충 된 것 같고.. Canceled와  Blanked event도 처리하고 가야겠지.
        End If
        'SafeLeave(LinkedSymbol.OneKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '[OperationKey] All Deal Initiated event handler- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub DealInitiatedEventHandlerForOperator(ByVal the_operator As et21_Operator)
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
                ' 분할해서 매수 의뢰했던 게 일부 체결되었다고 들어옴
                _DecisionMaker.BuyingInitiated()
            ElseIf _OpStatus = OperationState.OS_STOP_BUYING Then
                '2024.01.07 : prethreshold exiting 으로 매수중단하려고 했는데 체결된 경우 => decision maker 에 알려줘야 한다.
                _DecisionMaker.BuyingInitiated()
            End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit Then
            'InitiatedEvent의 목적은 매수시 매수가격에 잠깐 도달하고 일부 체결되었다가 다시 올라가는 경우를 방지하기 위함이다. 따라서 매도시는 필요없다.
        End If
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '[OperationKey] Cancel Confirmed event handler- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub CancelConfirmedEventHandlerForOperator(ByVal the_operator As et21_Operator)
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
                '분할 매수한 것이 일부 취소되었다고 연락옴
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If the_operator.DealPrice <> 0 Then
                            '취소되기전 체결수량이 있다면
                            If BuyDealPrice = 0 Then
                                BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨a")
                            Else
                                Dim old_price As Double = BuyDealPrice
                                Dim new_price As Double = OperatorList(index).DealPrice
                                Dim old_amount As UInt32 = BoughtAmount
                                Dim new_amount As UInt32 = OperatorList(index).ThisAmount - OperatorList(index).RestAmount
                                '평균체결가 계산
                                '130621: 아래 분모가 0인 경우는 없는지 조사해보자. 그리고 이 밑으로 쭉 비슷한 부분 찾아서 수익률 계산코드 집어넣자.
                                BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨b")
                            End If
                        Else
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨c")
                        End If
                        BoughtAmount += OperatorList(index).ThisAmount - OperatorList(index).RestAmount      '취소되기전 산 수량을 기록함
                        If ThisAmount > OperatorList(index).RestAmount Then
                            ThisAmount -= OperatorList(index).RestAmount            '체결되지 않고 남은 양은 취소되므로 ThisAmount에서 제함
                        Else
                            ThisAmount = 0          '음수일리 없다.
                        End If
                        '취소 되었으니 해당 operator 삭제
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨")
                        Dim rest_amount = OperatorList(index).RestAmount
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString & ", ThisAmount " & ThisAmount & ", Operator RestAmount " & rest_amount)
                        Exit For
                    End If
                Next

                If OperatorList.Count = 0 Then
                    '어쨌든 매수 시도가 끝났다
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "모든 매수시도 종료")
                    If BoughtAmount > 0 Then
                        '산 수량이 하나라도 있으면 청산 대기
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산대기")
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    Else
                        '하나도 못 사졌다면 => 종료함
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "산 게 없음. 종료함.")
                        '종료하기전 실시간 걸어놓은 것 다 정리하고 감
#If 0 Then
                        If _H1_ IsNot Nothing Then
                            _H1_.UnadviseRealData()
                        End If
                        If _HA_ IsNot Nothing Then
                            _HA_.UnadviseRealData()
                        End If
#End If
                        _OpStatus = OperationState.OS_DONE
                        DebugDONEDONE = DebugDONEDONE & "3"
                    End If
                Else
                    '2024.01.07 : 분할매수하는데 그 중 하나가 취소확인들어왔는데 나머지 operator 가 남아 있는 경우가 있을 수 없을 것으로 보임. 이런 경우 있나 보기 위해 error log 걸어둠.
                    '2024.01.13 : 그런 경우도 있다는 것이 확인됨. 두 개의 operator들이 매수용으로 올라온 상태에서 매수중단 하게되면 첫번째 operator 취소확인 때 당연히 이쪽으로 들어온다.
                    'ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " : 분할 매수중 있을 수 없는 경우")
                End If
            ElseIf _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL OrElse _OpStatus = OperationState.OS_STOP_BUYING Then
                '매수 종료위해 강제 취소한 결과가 지금 날라옴
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If the_operator.DealPrice <> 0 Then
                            '취소전 일부 체결된 수량이 있다는 의미
                            If BuyDealPrice = 0 Then
                                BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨d")
                            Else
                                Dim old_price As Double = BuyDealPrice
                                Dim new_price As Double = OperatorList(index).DealPrice
                                Dim old_amount As UInt32 = BoughtAmount
                                Dim new_amount As UInt32 = OperatorList(index).ThisAmount - OperatorList(index).RestAmount
                                '평균체결가 계산
                                '130621: 아래 분모가 0인 경우는 없는지 조사해보자. 그리고 이 밑으로 쭉 비슷한 부분 찾아서 수익률 계산코드 집어넣자.
                                BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨e")
                            End If
                        Else
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매수취소됨f")
                        End If
                        '2021.02.06: 2021.02.06_기다리자a
                        '2021.02.06: 매수취소시도시 이쪽으로 들어왔을 때 아래 BoughtAmount 계산해주는 곳이 없어서 매수된 수량이 누락되는 경우가 발생하여
                        '2021.02.06: 결국 미청산 수량이 남는 문제가 있었다. 아래 BoughtAmount 와 ThisAmount 업데이트해주는 부분을 
                        '2021.02.06: OS_ORDER_REQUESTED case 에서 복사해 와서 해결하는 솔루션을 구현했다.
                        BoughtAmount += OperatorList(index).ThisAmount - OperatorList(index).RestAmount      '취소되기전 산 수량을 기록함
                        If ThisAmount > OperatorList(index).RestAmount Then
                            ThisAmount -= OperatorList(index).RestAmount            '체결되지 않고 남은 양은 취소되므로 ThisAmount에서 제함
                        Else
                            ThisAmount = 0          '음수일리 없다.
                        End If
                        '취소 되었으니 해당 operator 삭제
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가 " & OperatorList(index).OrderPrice.ToString & ":강제취소됨")
                        OperatorList(index).Terminate()
                        Dim rest_amount = OperatorList(index).RestAmount
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString & ", ThisAmount " & ThisAmount & ", Operator RestAmount " & rest_amount)
                        Exit For
                    End If
                Next

                If _OpStatus = OperationState.OS_STOP_BUYING Then
                    If OperatorList.Count = 0 Then
                        '매수중단 명령이었으면, 청산하지 않고 위에 보고 후 대기. 
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "매수중단 수행완료. 대기함. BoughtAmount " & BoughtAmount.ToString & " ThisAmount " & ThisAmount.ToString)
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                        If BoughtAmount = 0 Then
                            '2024.01.08 : 체결수량이 0 인 경우에만 매수중단 성공이라고 말할 수 있을 것 같다.
                            _DecisionMaker.StopBuyingCompleted()
                        End If
                    Else
                        '아직 안 끝난 operator들이 있으니 끝날 때까지 기다리자.
                    End If
                Else 'If _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL
                    If OperatorList.Count = 0 Then
                        '어쨌든 매수 시도가 끝났다
                        If BoughtAmount > 0 Then
                            '산 수량이 하나라도 있으면 바로 청산작업 시작

                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "바로청산")
                            '청산 작업
                            If _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL Then
                                _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                Liquidate(1)
                            Else
                                _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                Liquidate(0)
                            End If
                        Else
                            '하나도 못 사졌다면 => 종료함
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산할 것도 없음. 종료함.")
                            EnterExitState = EnterOrExit.EOE_Exit   '청산모드로 바꿈
                            _OpStatus = OperationState.OS_DONE
                            DebugDONEDONE = DebugDONEDONE & "4"
                        End If
                    End If
                End If
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 취소 이벤트 들어왔음")
            End If
        Else ' If EnterExitState = EnterOrExit.EOE_Exit
            '매도한 것이 취소되었다고 연락오는 경우는 없다고 봄 => nothing
            '아니다, 있다. 주문가 수정을 위해 취소되는 경우 있잖아. => 다시 아니다, 이건 operator 선에서 처리되는 것이고 operation까지는 inform 안오잖아.
        End If
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '130627: 죄다 protected zone이네.. 이러다 task 실행 밀려 문제되는 거 아냐.
    '[OperationKey] Order Blanked event hander- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub OrderBlankedEventHandlerForOperator(ByVal the_operator As et21_Operator)
        Dim cancel_requested_from_operation As Boolean = False

        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST OrElse _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL OrElse _OpStatus = OperationState.OS_STOP_BUYING Then
                '분할 매수 혹은 강제취소한 것이 어떤 문제로 체결이 불발됨
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If the_operator.DealPrice <> 0 Then
                            '불발되기ㄷ전 일부 체결된 수량이 있다는 의미
                            If BuyDealPrice = 0 Then
                                BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                            Else
                                Dim old_price As Double = BuyDealPrice
                                Dim new_price As Double = OperatorList(index).DealPrice
                                Dim old_amount As UInt32 = BoughtAmount
                                Dim new_amount As UInt32 = OperatorList(index).ThisAmount - OperatorList(index).RestAmount
                                '평균체결가 계산
                                '130621: 아래 분모가 0인 경우는 없는지 조사해보자. 그리고 이 밑으로 쭉 비슷한 부분 찾아서 수익률 계산코드 집어넣자.
                                BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                            End If
                            'BoughtAmount += OperatorList(index).ThisAmount - OperatorList(index).RestAmount      '취소되기전 산 수량을 기록함
                            '20220523: 아래 5월20일 일어났던 문제 심층 분석 결과, 위의 BoughtAmount 에 여태까지 산 수량을 업데이트하는 것은 Terminate 처리하지 않기로 한 이후로 필요없게 되었음이 확인되어 코멘트처리함.
                        End If
                        '불발 되었으니 일단 해당 operator 삭제. 하지만 원인 파악은 어떻게든 해서 이것이 다시 일어나지 않게 해야함
                        '20220314: 오늘 14:50 경 115160 종목 취소주문이 주문번호 공백이 되어 왔다. 원인은 취소주문 안 먹히고 체결되었던 것인데, 수량 50 개중 먼저 체결된 48개가 실시간으로 먼저 오고
                        '20220314: 취소주문확인이 주문번호 공백이 되어 온 다음 나중에 체결된 2개가 그 후에 들어왔다. 그래서 나중에 체결된 2개는 체결수량에 계산이 안 되어 청산이 안 되는 문제가 있었다.
                        '20220314: 아래에서 Terminate 처리해서 AllDealConfirmed 이벤트가 연결이 안 되어 청산이 불가해진 것이다. 대부분의 주문번호 공백 case가 이런 것이라면 Terminate 처리 안하고
                        '20220314: 기다리면 나중에 체결된 SC1 에서 AllDealConfirmed 를 통해 정상적으로 청산이 될 수 있을 것이다.
                        '20220407: 09:47:20 경 비슷한 case (취소주문 안 먹히고 체결되고 취소주문확인은 주문번호가 공백으로 옴
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":어쩐일인지 주문번호 공백.") '2023.12.29 : 일시적으로 ErrorLogging 대신 MessageLogging 을 쓰기로 한다. 
                        '20220520: 9시 11분경 취소주문한 20 주가 안 먹히고 주문번호 공백으로 튕기기 전에 이미 2주가 체결이 되어버렸다. 주문번호 공백으로 튕긴 후에 나머지 18주가 모두 체결되었다.
                        '20220520: 문제는 나중에 청산할 때 보니, 20 주가 아니라 22 주를 갖고 있는 것으로 나와서 22주 매도 주문 넣으니 주문번호 공백으로 튕겨버렸다.
                        '20220520: 취소주문이 주문번호 공백으로 튕길 때 이미 체결된 거는 빼줘야하는 거 맞는 것 같다.
                        '2023.11.13 : 14:57 경 덕성 주문이 처음에는 잘 들어갔다가 한 번 취소하고 재주문하는 사이에 다른 종목 녀석이 체결되어 증거금을 까먹에서 주문이 실패하였다. 이 때 주문번호 공백으로 처리되었다.

                        '2023.12.27 : 주문번호 공백으로 오는 경우는 다양하게 있는데, 그 중 취소주문 중에 체결이 이루어진 경우는 위에 2022년에 분석되었듯이 Terminate 이 필요가 없다.
                        '2023.12.27 : 그런데 매수주문 시 주문번호 공백으로 오는 경우에는 주로 증거금 문제로 인한 경우가 대부분인 듯 하다. 이 경우에는 Terminate 을 진행하도록 한다.
                        If OperatorList(index).State = et21_Operator.OperatorState.OS_ORDER_REQUESTED Then
                            '2024.01.10 : 매수주문 시 주문공백으로 오는 경우 BoughtAmount 와 ThisAmount 도 원상복귀 시킨다.
                            BoughtAmount += OperatorList(index).ThisAmount - OperatorList(index).RestAmount      '취소되기전 산 수량을 기록함
                            If ThisAmount > OperatorList(index).RestAmount Then
                                ThisAmount -= OperatorList(index).RestAmount            '체결되지 않고 남은 양은 취소되므로 ThisAmount에서 제함
                            Else
                                ThisAmount = 0          '음수일리 없다.
                            End If

                            If OperatorList(index).CancelRequestedFromOperation Then
                                '2024.10.14 : 매수주문 확인이 들어오기 전에 매수중단이 요청되었는데 매수주문 확인이 주문번호 공백으로 들어온 경우다
                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "매수주문 확인이 들어오기 전에 매수중단이 요청되었는데 매수주문 확인이 주문번호공백으로 들어옴. 매수중단 정상처리.")
                                cancel_requested_from_operation = True
                            End If

                            OperatorList(index).Terminate()
                            OperatorList.RemoveAt(index)
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        End If

                        Exit For
                    End If
                Next

                If _OpStatus = OperationState.OS_STOP_BUYING Then
                    '매수중단 명령이었으면, 청산하지 않고 위에 보고 후 대기
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "매수중단 시도 중 주문번호 공백. 필시 중단되지 않고 체결되었을 걸.")

                    If OperatorList.Count = 0 Then
                        '공백이 왔지만 어쨌든 끝났음
                        If BoughtAmount > 0 Then
                            '체결된 수량이 있음
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "안 사려고 했지만 다 사졌음. 청산기다림 모드로 전환.")
                            _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                        Else
                            '체결된 수량이 없음 => 아직 없는 거고 조만간 있을지도 모르지. 그러면 다른 경로를 통해 처리되겠지.
                            '2024.01.14 : 매수주문 확인이 들어오기 전에 매수중단이 요청되었는데 매수주문 확인이 주문번호 공백으로 들어온 경우로서 매수중단 정상처리함.
                            If cancel_requested_from_operation Then
                                _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                _DecisionMaker.StopBuyingCompleted()
                            End If
                        End If
                    Else
                        '아직 처리되지 않은 operator 기다리기로 함.
                    End If

                    '_DecisionMaker.StopBuyingCompleted()   '2024.01.08 : 공백이 들어왔는데 성공이라고 말할 수 없을 것 같다.
                Else
                    If OperatorList.Count = 0 Then
                        '어쨌든 매수 시도가 끝났다
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "모든 매수시도 종료")
                        If BoughtAmount > 0 Then
                            '산 수량이 하나라도 있다
                            If _OpStatus = OperationState.OS_CANCEL_BUYING Or _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL Then
                                '청산위한 강제취소상태였으면 바로 청산 시작

                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "바로청산")
                                '청산 작업
                                If _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL Then
                                    _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                    Liquidate(1)
                                Else
                                    _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                                    Liquidate(0)
                                End If
                            Else
                                '그렇지 않으면 청산 대기
                                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산대기")
                                _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                            End If
                        Else
                            '하나도 못 사졌다면 => 재구매할지도 모르니 일단 청산대기상태로 둠  <= 2024.01.07 부로 바뀜
                            MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "산 게 없음. 재구매할지도 모르니 일단 청산대기상태로 둠")
                            _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                        End If
                    End If
                End If
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 블랭크 이벤트 들어왔음")
            End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED Or _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION Then
                '매도한 것이 어떤 문제로 체결이 불발됨
#If 0 Then
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        'RestOfBoughtAmount에 환원시키고 다음 주문 때 반영한다.
                        RestOfBoughtAmount += OperatorList(index).ThisAmount
                        If Liquount = 0 Then
                            Liquount = 1
                        End If
                        '불발 되었으니 해당 operator 삭제.
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매도주문 블랭크됨")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next
#End If
                '2024.01.29 : 그 동안 "매도주문 블랭크됨" 으로 로깅된 경우가 적지 않게 있었던 걸로 보이는데 특별한 경우를 제외하면 대부분 취소주문 안 먹히고 매도체결될 때
                '2024.01.29 : 취소주문 확인이 주문번호 공백으로 들어오는 경우인 걸로 보인다. 이 때 Operation level 에서 해줘야 할 것은 아무것도 없다.
                '2024.01.29 : 그 동안 위에서 terminate 처리 해줬었는데 그래도 매도체결되었기 때문에 눈에 띄지 않았지만, 걸린애로 표시되진 않았을 것이다.
                '2024.01.29 : 오늘 10:09:21 에 A294630 종목에서 일어난 일이 그것이다. 그래서 위에 처리하던 부분 싹다 막아놨다.
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 블랭크 이벤트 들어왔음")
            End If
            '130516: Cancel 및 Blank도 처리된 것 같다. 거슬러 올라가보면 매도처리하는데 있어서 다음 작업이 무엇인지 알 수 있을 것 같다.
        End If
        'Operation 종료시 decision maker에서 처리 방법 고민 후 매도로 넘어가자.
        'Operation이 매수없이 종료되면 decision maker가 알아서 주문을 넣지 않기 때문에 신경 안 써도 됨=> 자 이제 매도로 넘어갈까?
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '[OneKey] Order rejected event 처리- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub OrderRejectedEventHandlerForOperator(ByVal the_operator As et21_Operator)
        '130506: 주문 reject 처리하자.
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
                '분할 매수 주문한 것이 거부됨
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If the_operator.DealPrice <> 0 Then
                            '거부전 일부 체결된 수량이 있다는 의미
                            If BuyDealPrice = 0 Then
                                BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                            Else
                                Dim old_price As Double = BuyDealPrice
                                Dim new_price As Double = OperatorList(index).DealPrice
                                Dim old_amount As UInt32 = BoughtAmount
                                Dim new_amount As UInt32 = OperatorList(index).ThisAmount - OperatorList(index).RestAmount
                                '평균체결가 계산
                                '130621: 아래 분모가 0인 경우는 없는지 조사해보자. 그리고 이 밑으로 쭉 비슷한 부분 찾아서 수익률 계산코드 집어넣자.
                                BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                            End If
                            BoughtAmount += OperatorList(index).ThisAmount - OperatorList(index).RestAmount      '취소되기전 산 수량을 기록함
                        End If
                        '거부 되었으니 일단 해당 operator 삭제.
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":주문 거부됨")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next

                If OperatorList.Count = 0 Then
                    '어쨌든 매수 시도가 끝났다
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "모든 매수시도 종료")
                    If BoughtAmount > 0 Then
                        '산 수량이 하나라도 있으면 청산 대기
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산대기")
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    Else
                        '하나도 못 사졌다면 => 재구매할지도 모르니 일단 청산대기상태로 둠  <= 2024.01.07 부로 바뀜
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "산 게 없음. 재구매할지도 모르니 일단 청산대기상태로 둠")
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    End If
                End If
            ElseIf _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_STOP_BUYING Then
                '매수 종료위해 강제 취소 혹은 매수중단 주문했는데 거부됨. 필시 취소전 체결되었기 때문일 것임
                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 거부됨")
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":취소주문 거부됨. 필시 취소전 체결때문일 것임. 기다리자.")
                    End If
                Next
                '130507:이런 형태로 되어 있는 것 index가 찾아지지 않을 수도 있으니까 일단 이런데 들어오면 메시지 표시하게끔 바꿔놓자.
                '130508:위에 todo는 어느정도 된 것 같다. 거슬러 올라가보자.
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 거부 이벤트 들어왔음")
            End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED Or _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION Then
                '분할 매도 주문한 것이 거부됨

                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        'RestOfBoughtAmount에 환원시키고 다음 주문 때 반영한다.
                        RestOfBoughtAmount += OperatorList(index).ThisAmount
                        If Liquount = 0 Then
                            Liquount = 1
                        End If
                        '거부 되었으니 해당 operator 삭제.
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":주문 거부됨")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next
            Else        'EOE_Exit에는 OS_WAIT_UNTIL_CANCEL_BUYING 도 없다.
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 거부 이벤트 들어왔음")
            End If
        End If
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '[OneKey] Order errored event 처리- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub OrderErroredEventHandlerForOperator(ByVal the_operator As et21_Operator)
        'Errored event 처리는 에러 났을 경우 해당 operator를 list에서 삭제하도록 한다.
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
                '분할 매수 주문한 것이 거부됨
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        If the_operator.DealPrice <> 0 Then
                            '불발되기ㄷ전 일부 체결된 수량이 있다는 의미
                            If BuyDealPrice = 0 Then
                                BuyDealPrice = OperatorList(index).DealPrice        '산 가격을 기록함
                            Else
                                Dim old_price As Double = BuyDealPrice
                                Dim new_price As Double = OperatorList(index).DealPrice
                                Dim old_amount As UInt32 = BoughtAmount
                                Dim new_amount As UInt32 = OperatorList(index).ThisAmount - OperatorList(index).RestAmount
                                '평균체결가 계산
                                '130621: 아래 분모가 0인 경우는 없는지 조사해보자. 그리고 이 밑으로 쭉 비슷한 부분 찾아서 수익률 계산코드 집어넣자.
                                BuyDealPrice = Math.Round((old_price * old_amount + new_price * new_amount) / (old_amount + new_amount))
                            End If
                            BoughtAmount += OperatorList(index).ThisAmount - OperatorList(index).RestAmount      '취소되기전 산 수량을 기록함
                        End If
                        '거부 되었으니 일단 해당 operator 삭제.
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":주문 에러남")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next

                '여기서 수익률 계산을 위한 코드가 들어갈 수도 있겠다.
                If OperatorList.Count = 0 Then
                    '어쨌든 매수 시도가 끝났다
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "모든 매수시도 종료")
                    If BoughtAmount > 0 Then
                        '산 수량이 하나라도 있으면 청산 대기
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산대기")
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    Else
                        '하나도 못 사졌다면 => 재구매할지도 모르니 일단 청산대기상태로 둠  <= 2024.01.07 부로 바뀜
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "산 게 없음. 재구매할지도 모르니 일단 청산대기상태로 둠")
                        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                    End If
                End If
            ElseIf _OpStatus = OperationState.OS_CANCEL_BUYING OrElse _OpStatus = OperationState.OS_STOP_BUYING Then
                '매수 종료위해 강제 취소 혹은 매수중단 주문했는데 에러남. 왜??
                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 에러남 왜??")
                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":취소주문 에러남.. 왜?? 일단 아무 동작 하지 말자.")
                    End If
                Next
            Else
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 에러 이벤트 들어왔음")
            End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED Or _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION Then
                '분할 매도 주문한 것이 거부됨

                For index As Integer = 0 To OperatorList.Count - 1
                    If OperatorList(index) Is the_operator Then
                        'RestOfBoughtAmount에 환원시키고 다음 주문 때 반영한다.
                        RestOfBoughtAmount += OperatorList(index).ThisAmount
                        If Liquount = 0 Then
                            Liquount = 1
                        End If
                        '거부 되었으니 해당 operator 삭제.
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문가" & OperatorList(index).OrderPrice.ToString & ":매도주문 에러남 왜??")
                        OperatorList(index).Terminate()
                        OperatorList.RemoveAt(index)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "OperatorList카운트 " & OperatorList.Count.ToString)
                        Exit For
                    End If
                Next
            Else        'EOE_Exit에는 OS_WAIT_UNTIL_CANCEL_BUYING 도 없다.
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :이상한 에러 이벤트 들어왔음")
            End If
        End If
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘
    '130617: 에러처리 잘 하고 있는 건지 확신이 없지만 그래도 그냥 가보자. 일단 모든 error 상황에 error handler를 연결시켜보자.

    'Set the account manager
    Public Sub SetAccountManager(ByVal the_account_manager As et1_AccountManager)
        _AccountManager = the_account_manager
    End Sub
    '130624: 체결가, 체결수량 잘 업데이트되도록 수정한 것 같다.

#If 0 Then
    Private Function GetBuyAmount(ByVal call_prices As CallPrices, ByVal enter_price As UInt32, ByRef input_price As UInt32) As UInt32
        Dim last_buy_price As Double = (1 + BUYABLE_PRICE_RATE) * enter_price     '살 수 있는 마지노선 가격
        Dim total_buyable_amount_to_5th As UInt32 = 0
        Dim total_buyable_amount As UInt32 = 0
        Dim buyable_amount_this_time As UInt32 = 0


        '분할 매수 수량 계산을 위한 전체 물량 조사
        total_buyable_amount_to_5th = call_prices.SelAmount1 + call_prices.SelAmount2 + call_prices.SelAmount3 + call_prices.SelAmount4 + call_prices.SelAmount5
        '고요수율 곱함
        buyable_amount_this_time = total_buyable_amount_to_5th * QUIET_OPERATION_RATE


        '분할 매수 수량 계산을 위한 진짜로 살 수 있는 전체 물량 조사
        If last_buy_price < call_prices.SelPrice1 Then
            total_buyable_amount = 0
            '매도1호가 물량조차 살수 없음
            input_price = 0
            Return 0            '아무 것도 살 수 없음
        ElseIf last_buy_price < call_prices.SelPrice2 Then
            total_buyable_amount = call_prices.SelAmount1
            '매도1호가 물량만 살 수 있음
            input_price = call_prices.SelPrice1
        ElseIf last_buy_price < call_prices.SelPrice3 Then
            total_buyable_amount = call_prices.SelAmount1 + call_prices.SelAmount2
            '매도2호가 물량까지 살 수 있음
            input_price = call_prices.SelPrice2
        ElseIf last_buy_price < call_prices.SelPrice4 Then
            total_buyable_amount = call_prices.SelAmount1 + call_prices.SelAmount2 + call_prices.SelAmount3
            '매도3호가 물량까지 살 수 있음
            input_price = call_prices.SelPrice3
        ElseIf last_buy_price < call_prices.SelPrice5 Then
            total_buyable_amount = call_prices.SelAmount1 + call_prices.SelAmount2 + call_prices.SelAmount3 + call_prices.SelAmount4
            '매도4호가 물량까지 살 수 있음
            input_price = call_prices.SelPrice4
        Else
            total_buyable_amount = call_prices.SelAmount1 + call_prices.SelAmount2 + call_prices.SelAmount3 + call_prices.SelAmount4 + call_prices.SelAmount5
            '매도5호가 물량까지 살 수 있음
            input_price = call_prices.SelPrice5
        End If

        '이번 분할 매수할 수량과 살 수 있는 수량 중과 사야할 물량 중에 작은 놈을 산다
        Dim before_cutting As UInt32 = Math.Min(Math.Min(buyable_amount_this_time, total_buyable_amount), RestOfTargetAmount)

        If MarketKind = MARKET_KIND.MK_KOSPI Then
            '코스피면 10단위 절삭하여 사고
            Dim cutted_amount As UInt32 = before_cutting - (before_cutting Mod 10)
            Return cutted_amount
        Else
            '코스닥이면 그대로 산다
            Return before_cutting
        End If
    End Function

    Private Function GetSelAmount(ByVal call_prices As CallPrices, ByVal exit_price As UInt32, ByRef input_price As UInt32) As UInt32
        'Dim last_sel_price As Double = (1 - SELABLE_PRICE_RATE) * exit_price     '팔 수 있는 마지노선 가격
        Dim total_selable_amount_to_5th As UInt32 = 0
        Dim total_selable_amount As UInt32 = 0
        Dim selable_amount_this_time As UInt32 = 0


        '분할 매도 수량 계산을 위한 전체 물량 조사
        total_selable_amount_to_5th = call_prices.BuyAmount1 + call_prices.BuyAmount2 + call_prices.BuyAmount3 + call_prices.BuyAmount4 + call_prices.BuyAmount5
        '고요수율 곱함
        selable_amount_this_time = total_selable_amount_to_5th * QUIET_OPERATION_RATE

        '매수5호가 물량까지 다 살 수 있음. 하지만 하한가에 근접한 경우 호가가 0이 되는 것을 피해야 함
        If call_prices.BuyAmount5 <> 0 Then
            input_price = call_prices.BuyPrice5
        ElseIf call_prices.BuyAmount4 <> 0 Then
            input_price = call_prices.BuyPrice4
        ElseIf call_prices.BuyAmount3 <> 0 Then
            input_price = call_prices.BuyPrice3
        ElseIf call_prices.BuyAmount2 <> 0 Then
            input_price = call_prices.BuyPrice2
        Else
            input_price = call_prices.BuyPrice1
        End If

#If 0 Then      '최대한 지체없이 파는 것으로 수정되면서 무력화됨
        '분할 매도 수량 계산을 위한 진짜로 살 수 있는 전체 물량 조사
        If last_sel_price > call_prices.BuyPrice1 Then
            total_selable_amount = 0
            '매수1호가 물량조차 살수 없음
            input_price = 0
            Return 0            '아무 것도 살 수 없음
        ElseIf last_sel_price > call_prices.BuyPrice2 Then
            total_selable_amount = call_prices.BuyAmount1
            '매수1호가 물량만 살 수 있음
            input_price = call_prices.BuyPrice1
        ElseIf last_sel_price > call_prices.BuyPrice3 Then
            total_selable_amount = call_prices.BuyAmount1 + call_prices.BuyAmount2
            '매수2호가 물량까지 살 수 있음
            input_price = call_prices.BuyPrice2
        ElseIf last_sel_price > call_prices.BuyPrice4 Then
            total_selable_amount = call_prices.BuyAmount1 + call_prices.BuyAmount2 + call_prices.BuyAmount3
            '매수3호가 물량까지 살 수 있음
            input_price = call_prices.BuyPrice3
        ElseIf last_sel_price > call_prices.BuyPrice5 Then
            total_selable_amount = call_prices.BuyAmount1 + call_prices.BuyAmount2 + call_prices.BuyAmount3 + call_prices.BuyAmount4
            '매수4호가 물량까지 살 수 있음
            input_price = call_prices.BuyPrice4
        Else
            total_selable_amount = call_prices.BuyAmount1 + call_prices.BuyAmount2 + call_prices.BuyAmount3 + call_prices.BuyAmount4 + call_prices.BuyAmount5
            '매수5호가 물량까지 살 수 있음
            input_price = call_prices.BuyPrice5
        End If
#End If

        '이번 분할 매도할 수량과 팔 수 있는 수량 중과 사야할 물량 중에 작은 놈을 판다
        'Dim before_cutting As UInt32 = Math.Min(Math.Min(selable_amount_this_time, total_selable_amount), RestOfTargetAmount)
        Dim before_cutting As UInt32 = Math.Min(selable_amount_this_time, RestOfTargetAmount)

        If MarketKind = MARKET_KIND.MK_KOSPI Then
            '코스피면 10단위 절삭하여 팔고
            Dim cutted_amount As UInt32 = before_cutting - (before_cutting Mod 10)
            Return cutted_amount
        Else
            '코스닥이면 그대로 판다
            Return before_cutting
        End If
    End Function

#End If

    Private Function IsLowLimitCautionSet(ByVal local_call_price As CallPrices) As Boolean
        If LinkedSymbol.VI Then
            '2024.03.19 : VI 때는 하한가 체크 안 하기로 한다.
            Return False
        Else
            If local_call_price.BuyPrice1 = 0 Then
                If local_call_price.SelPrice1 = 0 Then
                    '매수1호가와 매도1호가가 동시에 0 이면 뭔가 호가가 이상한 것으로 판단하고 하한가로 보지 않는다.
                    Return False
                ElseIf local_call_price.SelPrice1 < LinkedSymbol.LowLimitPrice * 0.72 / 0.7 Then
                    '매수1호가가 0이고 매도1호가가 -28% 이하에서 형성되어 있으면 진짜 하한가로 본다.
                    Return True
                Else
                    '매수1호가가 0이고 매도1호가가 -28% 초과에서 형성되어 있으면 뭔가 호가가 이상한것으로 판단하고 하한가로 보지 않는다.
                    Return False
                End If
            ElseIf local_call_price.SelPrice1 = 0 Then  '2024.03.05 : 상한가 쳤을 때 대응로직이 실질적으로 하한가 비상시와 동일하기 때문에 똑같이 처리하기로 한다.
                If local_call_price.BuyPrice1 = 0 Then
                    '매도1호가와 매수1호가가 동시에 0 이면 뭔가 호가가 이상한 것으로 판단하고 상한가로 보지 않는다.
                    Return False
                ElseIf local_call_price.BuyPrice1 > LinkedSymbol.LowLimitPrice * 1.28 / 0.7 Then
                    '매도1호가가 0이고 매수1호가가 +28% 이상에서 형성되어 있으면 진짜 상한가로 본다.
                    Return True
                Else
                    '매도1호가가 0이고 매수1호가가 +28% 미만에서 형성되어 있으면 뭔가 호가가 이상한것으로 판단하고 상한가로 보지 않는다.
                    Return False
                End If
            End If
        End If
    End Function

    '200ms 타이머
    Public Sub Tm200ms_Tick()

        If TypeOf (_DecisionMaker) Is c05F_FlexiblePCRenew AndAlso _DecisionMaker.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
            Dim this_pcrenew As c05F_FlexiblePCRenew = _DecisionMaker
            Dim the_symbol As c03_Symbol = _DecisionMaker.LinkedSymbol
            If this_pcrenew.DelayBuyCallPricePointList.Count = 0 AndAlso OnlyAFewChanceLeft > 0 Then
                'LPF buy 가격을 비주얼라이즈 해보자
                'this_pcrenew.DelayBuyCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, the_symbol.LPFPriceBuy))
                this_pcrenew.DelayBuyCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, the_symbol.AveCallPrice))
                OnlyAFewChanceLeft = OnlyAFewChanceLeft - 1
            End If
            If this_pcrenew.DelayBuyCallPricePointList.Count > 0 Then
                'LPF buy 가격을 비주얼라이즈 해보자
                'this_pcrenew.DelayBuyCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, the_symbol.LPFPriceBuy))
                this_pcrenew.DelayBuyCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, the_symbol.AveCallPrice))
            End If

            If this_pcrenew.DelaySelCallPricePointList.Count = 0 AndAlso OnlyAFewChanceLeft > 0 Then
                'LPF sel 가격을 비주얼라이즈 해보자
                'this_pcrenew.DelaySelCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, the_symbol.LPFPriceSel))
                this_pcrenew.DelaySelCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, (the_symbol.MA_SelCallPrice(5) + the_symbol.MA_BuyCallPrice(5)) / 2))
                OnlyAFewChanceLeft = OnlyAFewChanceLeft - 1
            End If
            If this_pcrenew.DelaySelCallPricePointList.Count > 0 Then
                'LPF sel 가격을 비주얼라이즈 해보자
                'this_pcrenew.DelaySelCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, the_symbol.LPFPriceSel))
                this_pcrenew.DelaySelCallPricePointList.Add(New PointF(Now.TimeOfDay.TotalSeconds, (the_symbol.MA_SelCallPrice(5) + the_symbol.MA_BuyCallPrice(5)) / 2))
            End If
        End If

        SafeEnterTrace(OperationKey, 134)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        _ClockCount += 1

        'SafeEnterTrace(OrderNumberListKey, 8)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
#If 1 Then

        '각 operator들에 시간 공급
        For index As Integer = 0 To OperatorList.Count - 1
            OperatorList(index).Tm200ms_Tick()
        Next
        '매수 다 된 상태에서 가격이 하한가 가까이 하락하면 팔아버리는 로직이 필요하다. 이건 언제 하지? 청산 명령 받고 팔아버리는 로직이 먼저 필요할 듯 하다.
        '130527:여기서 각 operator들의 매도주문가를 확인하여 현재가에 비해 현저히 하한가에 가까워진 것이 판단되면 각 operator들에 즉시 팔아치우는 명령을 내리게 한다.

        Dim current_price As UInt32         '현재가 알아보기
        Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices
        If local_call_price.BuyPrice1 <> 0 Then
            current_price = local_call_price.BuyPrice1
        Else
            current_price = local_call_price.SelPrice1
        End If
        If EnterExitState = EnterOrExit.EOE_Enter Then
            '130528: 여기서 (현재가 - 하한가) 가 (진입가 - 하한가)에 비해 얼마나 떨어졌나 조사해서 특정 기준과 비교한다.
            '2021.03.16: 이제부터 Test 계좌에만 하한가 비상을 적용하기로 한다.
            If (_OpStatus = OperationState.OS_ORDER_REQUESTED OrElse _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST) AndAlso IsLowLimitCautionSet(local_call_price) AndAlso (_AccountManager.AccountCat = 0 Or _AccountManager.AccountCat = 2) Then ' 잡스러운 것보다는 그냥 BuyPrice1이 0인 게 하한가 판단하는 가장 좋은 조건이다. (((current_price - LinkedSymbol.LowLimitPrice) <= (EnterZeroPrice - LinkedSymbol.LowLimitPrice) * LOW_LIMIT_CAUTION_RATE) OrElse LinkedSymbol.LowLimitPrice >= current_price) Then
                '하한가 비상 발령!
                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "매수 상태중 하한가 비상 발령!")
                'OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST
                '청산 작업
                Liquidate(1)
                LinkedSymbol.LowLimitCautionIssued()        '해당 종목은 거래 금지하게끔 caution을 날리자
                '140512 : 아래와 같이 하는 것이 진짜 맞는 것인가 조사해보자.
                '140513 : 아래는 할 피료 없다. Liquidate안에 찾아들어가서 parameter 1일 경우 _OpStatusㄹㄹ OS_LOW_LIMIT_CAUTTION으로 바꾸기 하자.
                '_OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION
            End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit
            '130528: 여기서 (현재가 - 하한가) 가 (진입가 - 하한가)에 비해 얼마나 떨어졌나 조사해서 특정 기준과 비교한다.
            '2021.03.16: 이제부터 Test 계좌에만 하한가 비상을 적용하기로 한다.
            If _OpStatus = OperationState.OS_ORDER_REQUESTED AndAlso IsLowLimitCautionSet(local_call_price) AndAlso (_AccountManager.AccountCat = 0 Or _AccountManager.AccountCat = 2) Then ' 잡스러운 것보다는 그냥 BuyPrice1이 0인 게 하한가 판단하는 가장 좋은 조건이다. (((current_price - LinkedSymbol.LowLimitPrice) <= (EnterZeroPrice - LinkedSymbol.LowLimitPrice) * LOW_LIMIT_CAUTION_RATE) OrElse LinkedSymbol.LowLimitPrice >= current_price) Then
                '하한가 비상 발령!
                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "하한가 비상 발령!")
                For index As Integer = 0 To OperatorList.Count - 1
                    OperatorList(index).SellThemAll()   '다 팔아버려 그냥
                Next
                If Liquount > 0 And RestOfBoughtAmount > 0 Then
                    '130529: Liquout > 0일 때 남은 것 마저 모조리 팔아버려야 함.
                    '가격 결정
                    InitPrice = LinkedSymbol.LowLimitPrice  '하한가로 판다.
                    Dim init_amount As UInt32
                    init_amount = RestOfBoughtAmount
                    OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Exit, InitPrice, init_amount, _AccountManager))
                    'OperatorList.Last.LowLimitCaution = True        '마지막 오퍼레이터에게 하한가비상상황임음 알린다. 아니다 이미 하한가로 주문했기 때문에 안 알려도 된다.
                    RestOfBoughtAmount = 0
                    _ClockCount = 0                 '시계를 다시 새롭게 시작한다.

                    'OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION
                End If
                _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION
            ElseIf _OpStatus = OperationState.OS_ORDER_REQUESTED And Liquount > 0 Then
                '130520 : Count된 게 일정 시간 이상 지나면 차기 매도 이루어지게 한다.
                If _ClockCount >= NEXT_ORDER_COUNT Then
                    '가격 결정
                    'Dim local_call_price As CallPrices = LastCallPrices
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "남은 거 또 한 번 팔아보자")
                    If local_call_price.SelPrice1 = 0 Then
                        '상한가 상황 => BuyPrice1로 판다.
                        InitPrice = local_call_price.BuyPrice1
                    Else
                        '그냥 일반적 상황에서는 SelPrice1로 판다.
                        'InitPrice = local_call_price.SelPrice1
                        'InitPrice = NextCallPrice(local_call_price.SelPrice1, -1)          '한단계 싼 가격으로 판다. 잘 안 팔리면 더 곤란해진다.
#If 0 Then
                        Dim price_stepped_down As UInt32 = NextCallPrice(local_call_price.SelPrice1, -1)
                        If price_stepped_down < (1 - ALLOWED_DEAL_LOSS) * local_call_price.SelPrice1 Then
                            '로스를 감수하면서까지 팔 수 있는 상황이 아니면 그냥 매도1호가로 주문한다.
                            InitPrice = local_call_price.SelPrice1
                        Else
                            '로스가 감수할 수 있는 수준이면 한 단계 아래 가격으로 주문한다.
                            InitPrice = price_stepped_down
                        End If
#End If
                        Dim best_sel_price As UInt32 = GetBestSelPrice(0, 0, 0)
                        InitPrice = best_sel_price
                    End If

                    Dim init_amount As UInt32
                    init_amount = RestOfBoughtAmount \ Liquount
                    If init_amount = 0 Then
                        init_amount = RestOfBoughtAmount
                    End If
                    OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Exit, InitPrice, init_amount, _AccountManager))
                    RestOfBoughtAmount = RestOfBoughtAmount - init_amount
                    If RestOfBoughtAmount = 0 Then
                        Liquount = 0
                    Else
                        Liquount = Liquount - 1
                    End If
                    _ClockCount = 0                 'clock count reset
                    '130522:여기도 다 된 거 같지? 그러면 Operator에서 시간 틱 받아서 스스로 정정하는 메카니즘 구현.
                End If
            End If
        End If
#End If
        'SafeLeaveTrace(OrderNumberListKey, 20)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        SafeLeaveTrace(OperationKey, 137)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

    End Sub

#If 0 Then
    '호가 받음
    Private Sub _HX_ReceiveData(ByVal szTrCode As String) Handles _H1_.ReceiveRealData, _HA_.ReceiveRealData
        SafeEnter(CallPriceKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If MarketKind = MARKET_KIND.MK_KOSPI Then
            _LastCallPrices.SelPrice1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho1"))
            _LastCallPrices.SelPrice2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho2"))
            _LastCallPrices.SelPrice3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho3"))
            _LastCallPrices.SelPrice4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho4"))
            _LastCallPrices.SelPrice5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho5"))
            _LastCallPrices.SelPrice6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho6"))
            _LastCallPrices.SelPrice7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho7"))
            _LastCallPrices.SelPrice8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho8"))
            _LastCallPrices.SelPrice9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho9"))
            _LastCallPrices.SelPrice10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerho10"))
            _LastCallPrices.SelAmount1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem1"))
            _LastCallPrices.SelAmount2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem2"))
            _LastCallPrices.SelAmount3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem3"))
            _LastCallPrices.SelAmount4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem4"))
            _LastCallPrices.SelAmount5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem5"))
            _LastCallPrices.SelAmount6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem6"))
            _LastCallPrices.SelAmount7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem7"))
            _LastCallPrices.SelAmount8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem8"))
            _LastCallPrices.SelAmount9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem9"))
            _LastCallPrices.SelAmount10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "offerrem10"))
            _LastCallPrices.BuyPrice1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho1"))
            _LastCallPrices.BuyPrice2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho2"))
            _LastCallPrices.BuyPrice3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho3"))
            _LastCallPrices.BuyPrice4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho4"))
            _LastCallPrices.BuyPrice5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho5"))
            _LastCallPrices.BuyPrice6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho6"))
            _LastCallPrices.BuyPrice7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho7"))
            _LastCallPrices.BuyPrice8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho8"))
            _LastCallPrices.BuyPrice9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho9"))
            _LastCallPrices.BuyPrice10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidho10"))
            _LastCallPrices.BuyAmount1 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem1"))
            _LastCallPrices.BuyAmount2 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem2"))
            _LastCallPrices.BuyAmount3 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem3"))
            _LastCallPrices.BuyAmount4 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem4"))
            _LastCallPrices.BuyAmount5 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem5"))
            _LastCallPrices.BuyAmount6 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem6"))
            _LastCallPrices.BuyAmount7 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem7"))
            _LastCallPrices.BuyAmount8 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem8"))
            _LastCallPrices.BuyAmount9 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem9"))
            _LastCallPrices.BuyAmount10 = Convert.ToUInt32(_H1_.GetFieldData("OutBlock", "bidrem10"))
        Else 'If MarketKind = MARKET_KIND.MK_KOSDAQ Then
            _LastCallPrices.SelPrice1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho1"))
            _LastCallPrices.SelPrice2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho2"))
            _LastCallPrices.SelPrice3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho3"))
            _LastCallPrices.SelPrice4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho4"))
            _LastCallPrices.SelPrice5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho5"))
            _LastCallPrices.SelPrice6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho6"))
            _LastCallPrices.SelPrice7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho7"))
            _LastCallPrices.SelPrice8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho8"))
            _LastCallPrices.SelPrice9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho9"))
            _LastCallPrices.SelPrice10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerho10"))
            _LastCallPrices.SelAmount1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem1"))
            _LastCallPrices.SelAmount2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem2"))
            _LastCallPrices.SelAmount3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem3"))
            _LastCallPrices.SelAmount4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem4"))
            _LastCallPrices.SelAmount5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem5"))
            _LastCallPrices.SelAmount6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem6"))
            _LastCallPrices.SelAmount7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem7"))
            _LastCallPrices.SelAmount8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem8"))
            _LastCallPrices.SelAmount9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem9"))
            _LastCallPrices.SelAmount10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "offerrem10"))
            _LastCallPrices.BuyPrice1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho1"))
            _LastCallPrices.BuyPrice2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho2"))
            _LastCallPrices.BuyPrice3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho3"))
            _LastCallPrices.BuyPrice4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho4"))
            _LastCallPrices.BuyPrice5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho5"))
            _LastCallPrices.BuyPrice6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho6"))
            _LastCallPrices.BuyPrice7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho7"))
            _LastCallPrices.BuyPrice8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho8"))
            _LastCallPrices.BuyPrice9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho9"))
            _LastCallPrices.BuyPrice10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidho10"))
            _LastCallPrices.BuyAmount1 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem1"))
            _LastCallPrices.BuyAmount2 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem2"))
            _LastCallPrices.BuyAmount3 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem3"))
            _LastCallPrices.BuyAmount4 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem4"))
            _LastCallPrices.BuyAmount5 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem5"))
            _LastCallPrices.BuyAmount6 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem6"))
            _LastCallPrices.BuyAmount7 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem7"))
            _LastCallPrices.BuyAmount8 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem8"))
            _LastCallPrices.BuyAmount9 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem9"))
            _LastCallPrices.BuyAmount10 = Convert.ToUInt32(_HA_.GetFieldData("OutBlock", "bidrem10"))
        End If
        SafeLeave(CallPriceKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
#End If

    '130628: 그래도 CallPrice key는 분리해서 좀 낫네.
#If 0 Then
    '140422:Liquidate 쓰기로 결정되면서 StopBuying은 사용할 곳이 없게 되었다.
    '매수 강제 중단 (사야될 물량 남아있어도)
    Public Sub StopBuying()
        For index As Integer = 0 To OperatorList.Count - 1
            OperatorList(index).CancelAll()
        Next
        '140312 : 이 클래스에서 나오는 SafeEnterTrace(OrderNumberListKey...) 과연 필요한 것인지 점검하자.
        SafeEnter(OperationKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST        '청산 명령 들어올 때까지 대기
        SafeLeave(OperationKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
#End If

#If 0 Then
    Public Function CancelVolume(ByVal lack_of_money As Double) As Double
        '140425:critical zone 생성 후 얼만큼 취소할 수 있는지 등 계산.
        SafeEnterTrace(OperationKey, 138)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim money_cancelled As Double = 0
        Dim index As Integer = OperatorList.Count - 1
        While money_cancelled < lack_of_money And index >= 0
            '140429 : CancelAll을 사용할 수 있는지 살펴보자. 거기엔 하한가시에만 사용한다고 되어 있는데 실제론 그렇지 않은 것 같다.
            '140430 : 이에 앞서 하한가 대응전략에 대해 전면 재검토해보자.
            money_cancelled = money_cancelled + OperatorList(index).OrderPrice * OperatorList(index).ThisAmount
            'ThisAmount = ThisAmount - OperatorList(index).ThisAmount
            OperatorList(index).CancelAll()

            index = index - 1
            MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "자금확보위한 취소중. 앞으로 취소할 돈 " & (lack_of_money - money_cancelled).ToString)
            '140514 : CancelVolume은 pre threshold상태소속의 operation 들어 대해서만 call된다는 사실 상기하고 CancelAll을 안심하고 사용해보자.
            '140515 : 취소에 대한 정산(취소전 매수체결량 등)은 취소 confirm 이벤트가 들어왔을 때 하게 된다.
            '140620 : 취소 confirm 이벤트 들어왔을 때 취소되지 않은 operator들에 대한 고려는 이미 되어 있다.
        End While
        SafeLeaveTrace(OperationKey, 141)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return lack_of_money - money_cancelled
    End Function
#End If

    '청산
    '[OperationKey] Order Blanked event hander- CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub Liquidate(ByVal low_limit_caution As Boolean)
        '청산 주문 한다.''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        'SafeEnterTrace(OrderNumberListKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'SafeEnter(_LinkedSymbol.OneKey)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
#If 1 Then

        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST And OperatorList.Count = 0 Then
                '매수작업 모두 끝나고 정상적으로 청산시도함
                '130509:이제 진짜로 청산작업.
                If BoughtAmount = 0 Then
                    '2024.01.07 : 지금까지 체결수량이 없는 상황에서 청산 명령이 내려지지 않도록 프로그램이 짜여있었지만 운이 좋았었던 것 같고, Prethreshold 적용 이후 앞으로는 이 경우를 대비 해야 한다.
                    '청산할 게 없으므로 DONE 때리고 나감
                    EnterExitState = EnterOrExit.EOE_Exit   '청산모드로 바꿈
                    _OpStatus = OperationState.OS_DONE
                    DebugDONEDONE = DebugDONEDONE & "5"
                Else
                    '가격 결정
                    Dim local_call_price As CallPrices = LinkedSymbol.LastCallPrices

                    If low_limit_caution Then
                        '하한가비상상황이면
                        InitPrice = LinkedSymbol.LowLimitPrice          '하한가로 판다

                        '주문해보자
                        _OpStatus = OperationState.OS_ORDER_REQUESTED            '주문한 상태로 바꿈

                        Dim init_amount As UInt32 = BoughtAmount

                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산 주문하기")
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice5.ToString & "   " & local_call_price.SelAmount5.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice4.ToString & "   " & local_call_price.SelAmount4.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice3.ToString & "   " & local_call_price.SelAmount3.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice2.ToString) ' & "   " & local_call_price.SelAmount2.ToString)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & local_call_price.SelPrice1.ToString) ' & "   " & local_call_price.SelAmount1.ToString)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "desired " & InitPrice.ToString)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & local_call_price.BuyPrice1.ToString) ' & "   " & local_call_price.BuyAmount1.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice2.ToString) ' & "   " & local_call_price.BuyAmount2.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice3.ToString & "   " & local_call_price.BuyAmount3.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice4.ToString & "   " & local_call_price.BuyAmount4.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice5.ToString & "   " & local_call_price.BuyAmount5.ToString)
                        OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Exit, InitPrice, init_amount, _AccountManager))
                        RestOfBoughtAmount = BoughtAmount - init_amount
                        Liquount = 0

                        _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION
                        EnterExitState = EnterOrExit.EOE_Exit   '청산모드로 바꿈
                        _ClockCount = 0                 '시계를 다시 새롭게 시작한다.
                    Else
                        '일반상황이면
                        If local_call_price.SelPrice1 = 0 Then
                            '상한가 상황 => BuyPrice1로 판다.
                            InitPrice = local_call_price.BuyPrice1
                        Else
                            '그냥 일반적 상황에서는 SelPrice1로 판다.
                            'InitPrice = local_call_price.SelPrice1
                            'InitPrice = NextCallPrice(local_call_price.SelPrice1, -1)          '한단계 싼 가격으로 판다. 잘 안 팔리면 더 곤란해진다.
#If 0 Then
                        Dim price_stepped_down As UInt32 = NextCallPrice(local_call_price.SelPrice1, -1)
                        If price_stepped_down < (1 - ALLOWED_DEAL_LOSS) * local_call_price.SelPrice1 Then
                            '로스를 감수하면서까지 팔 수 있는 상황이 아니면 그냥 매도1호가로 주문한다.
                            InitPrice = local_call_price.SelPrice1
                        Else
                            '로스가 감수할 수 있는 수준이면 한 단계 아래 가격으로 주문한다.
                            InitPrice = price_stepped_down
                        End If
#End If
                            Dim best_sel_price As UInt32 = GetBestSelPrice(0, 0, 0)
                            InitPrice = best_sel_price
                        End If
                        '주문해보자
                        _OpStatus = OperationState.OS_ORDER_REQUESTED            '주문한 상태로 바꿈
                        '130510:시간텀을 두고 세번에 걸처 매도한다. 몇 번째 매도인지 count 변수를 둔다.
                        '130510:주가 실시간 변화에 따른 정정주문(사실상 취소하고 재주문) 은 Operator에서 실시하게끔 한다.

                        'OperatorList(0).ExecuteLiquidation(InitPrice)
                        '130513: init_amount 계산 : 0은 안되게 1/3로 하자.
                        Dim init_amount As UInt32
                        If LIQ_COUNT = 1 Then
                            init_amount = BoughtAmount
                        Else
                            init_amount = BoughtAmount \ LIQ_COUNT
                            '2023.10.24 : 코스피 단주거래 허용된지 10년이 다 되어가는데 아직 업데이트가 안 됐냐 너무하다 정말
                            'If MarketKind = MARKET_KIND.MK_KOSPI Then
                            'init_amount = Math.Floor(init_amount / 10) * 10       '10단위로 자름
                            'End If
                            If init_amount = 0 Then
                                init_amount = BoughtAmount
                            End If
                        End If
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "청산 주문하기")
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice5.ToString & "   " & local_call_price.SelAmount5.ToString)
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice4.ToString & "   " & local_call_price.SelAmount4.ToString)
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice3.ToString & "   " & local_call_price.SelAmount3.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.SelPrice2.ToString) ' & "   " & local_call_price.SelAmount2.ToString)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & local_call_price.SelPrice1.ToString) ' & "   " & local_call_price.SelAmount1.ToString)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "desired " & InitPrice.ToString)
                        MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & local_call_price.BuyPrice1.ToString) ' & "   " & local_call_price.BuyAmount1.ToString)
                        '                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice2.ToString) ' & "   " & local_call_price.BuyAmount2.ToString)
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice3.ToString & "   " & local_call_price.BuyAmount3.ToString)
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice4.ToString & "   " & local_call_price.BuyAmount4.ToString)
                        'MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & local_call_price.BuyPrice5.ToString & "   " & local_call_price.BuyAmount5.ToString)
                        OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Exit, InitPrice, init_amount, _AccountManager))
                        RestOfBoughtAmount = BoughtAmount - init_amount
                        If RestOfBoughtAmount = 0 Then
                            Liquount = 0
                        Else
                            Liquount = LIQ_COUNT - 1
                        End If

                        EnterExitState = EnterOrExit.EOE_Exit   '청산모드로 바꿈
                        _ClockCount = 0                 '시계를 다시 새롭게 시작한다.

                    End If
                End If
            ElseIf _OpStatus = OperationState.OS_ORDER_REQUESTED OrElse OperatorList.Count > 0 Then
                '아직 매수작업 종료전이거나 남아있는 오퍼레이터가 있다 => 매수작업 강제 취소 명령 내리고 대기
                For index As Integer = 0 To OperatorList.Count - 1
                    OperatorList(index).CancelAll()
                Next

                If low_limit_caution Then
                    _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION_CANCEL
                Else
                    _OpStatus = OperationState.OS_CANCEL_BUYING
                End If
                '140509 : CancelAll은 하한가용이 아니라는 것을 알았다 이제 위의 상태 받아 쓰는 데를 조사해보자.
                '140509 : DealInitiate에서 Dealprice가 값이 바뀌는데 RestAmount는 안 바뀌게 되어 있음으로 해서 생기는 문제점은 없나 살펴봐야 한다.
                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "매수작업 강제취소 명령 내렸으니 기다립시다.")
                '취소 완료 이벤트 핸들러등에서 모니터하는 로직 작업하자.
                '130709:뭔가 한 번 더 돌아 봐야 할 것 같은데.. Operation에서 하한가 비상시 어떻게 동작하는지 체크해보자.
            Else
                '이도 저도 아닌데 뭐지?
                _OpStatus = OperationState.OS_ERROR
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "이도저도 아닌데 뭐지?")
            End If
        Else    'EOE_Exit
            If low_limit_caution Then
                '2024.02.08 : clearing time 시 매도 로직이라든지 이럴 때 Exit 상태임에도 Liquidate(1) 로 불릴 때가 있다.
                '하한가 비상 발령!
                MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "하한가 매도로 간다")
                For index As Integer = 0 To OperatorList.Count - 1
                    OperatorList(index).SellThemAll()   '다 팔아버려 그냥
                Next
                If Liquount > 0 And RestOfBoughtAmount > 0 Then
                    '130529: Liquout > 0일 때 남은 것 마저 모조리 팔아버려야 함.
                    '가격 결정
                    InitPrice = LinkedSymbol.LowLimitPrice  '하한가로 판다.
                    Dim init_amount As UInt32
                    init_amount = RestOfBoughtAmount
                    OperatorList.Add(New et21_Operator(Me, EnterOrExit.EOE_Exit, InitPrice, init_amount, _AccountManager))
                    RestOfBoughtAmount = 0
                End If
                _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION
            Else
                '매수상태도 아니고 하한가매도 전략도 아닌데 청산주문? 뭐지?

                _OpStatus = OperationState.OS_ERROR
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "매수상태도 아닌데 청산주문? 뭐지?")
            End If
        End If
#End If

        'SafeLeave(_LinkedSymbol.OneKey)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'SafeLeaveTrace(OrderNumberListKey, 1)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '2024.01.07 : 매수를 완전 포기하는 거는 아니고 현재 매수 주문 올라와 있는 것을 취소할 뿐이다. 혹시라도 매수된 수량이 있다면 기다렸다가 때되면 청산한다.
    Public Sub StopBuying()
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _OpStatus = OperationState.OS_ORDER_REQUESTED AndAlso OperatorList.Count > 0 Then
                If OperatorList.Count > 0 Then
                    '아직 매수작업 종료전이거나 남아있는 오퍼레이터가 있다 => 매수작업 강제 취소 명령 내리고 대기
                    For index As Integer = 0 To OperatorList.Count - 1
                        OperatorList(index).CancelAll()
                    Next

                    _OpStatus = OperationState.OS_STOP_BUYING
                    MessageLogging("A" & SymbolCode & " " & _AccountManager.AccountCatString & " " & "매수작업 중단 명령 내렸으니 기다립시다.")
                End If
            ElseIf _OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST Then
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "취소할 수량이 없다.")
            ElseIf _OpStatus = OperationState.OS_STOP_BUYING Then
                '매수중단 진행중이다.
            Else
                '이도 저도 아닌데 뭐지?
                '_OpStatus = OperationState.OS_ERROR
                ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "이도저도 아닌데 뭐지?")
            End If
        Else
            '매수상태도 아닌데 강제매수취소? 뭐지?
            _OpStatus = OperationState.OS_ERROR
            ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "매수상태도 아닌데 강제매수취소? 뭐지?")
        End If
    End Sub
#If 0 Then
    Public Sub ChangeLowLimitCautionFlag()
        If EnterExitState = EnterOrExit.EOE_Exit AndAlso _OpStatus = OperationState.OS_ORDER_REQUESTED Then
            _OpStatus = OperationState.OS_LOW_LIMIT_CAUTTION
            For Each an_operator In OperatorList
                an_operator.LowLimitCaution = True
            Next
        End If
    End Sub
#End If
End Class


Public Class et21_Operator
    Public Enum OperatorState
        OS_INITIALIZED
        OS_ORDER_REQUESTED
        OS_ORDER_CONFIRMED_CHECKING_DEAL
        OS_CANCELORDER_REQUESTED
        OS_CANCELORDER_CONFIRMED_CHECKING_DEAL
        OS_CANCELORDER_CHECKED_FIRST_WAIT_CONFIRM
        'OS_CANCELORDER_REQUESTED_FOR_SELLTHEMALL
        'OS_CANCELORDER_CHECKED_FIRST_WAIT_CONFIRM_FOR_SELLTHEMALL
        'OS_CHECKING_PRICE
        OS_ERROR
        OS_DONE
    End Enum

    Private _CSPAT00600_List As New List(Of et31_XAQuery_Wrapper)

    Private ReadOnly Property _CSPAT00600 As et31_XAQuery_Wrapper
        Get
            If _CSPAT00600_List.Count = 0 Then
                Return Nothing
            Else
                Return _CSPAT00600_List.Last
            End If
        End Get
    End Property

    Private WithEvents _CSPAT00800 As et31_XAQuery_Wrapper
    Public StartingOrder As ULong
    'Public CancelOrder As Integer
    Public ParentOperation As et2_Operation
    Public State As OperatorState
    Public EnterExitState As EnterOrExit
    Public CSPAT00600_Data As New List(Of String)
    Public CSPAT00800_Data As New List(Of String)
    Private _AccountManager As et1_AccountManager
    Public AccountString As String
    Public AccountPW As String
    Public ThisAmount As UInt32
    Private _RestAmount As UInt32
    Private _RestAmountKey As TracingKey
    Public OrderPrice As UInt32
    Public DealPrice As Double
    Public Event AllDealConfirmed(ByVal the_operator As et21_Operator, ByVal deal_amount As UInt32)  '전부 체결
    Public Event DealInitiated(ByVal the_operator As et21_Operator)  '일부 체결
    Public Event CancelConfirmed(ByVal the_operator As et21_Operator)
    Public Event OrderBlanked(ByVal the_operator As et21_Operator)
    Public Event OrderRejected(ByVal the_operator As et21_Operator)
    Public Event OrderErrored(ByVal the_operator As et21_Operator)
    Private _TimerTick As Integer
    Public BuyRetryTick As Integer
    Public LowLimitCaution As Boolean = False
    Private CSPAT00600_ReceiveData_Thread As Threading.Thread
    Private CSPAT00800_ReceiveData_Thread As Threading.Thread
    Public CancelRequestedFromOperation As Boolean = False      '취소요청이 Operation으로부터 온 건지 자체 timeout에 의한 재주문을 위한 취소요청인지 구분을 하기 위함
    Public AmountUnderOrder As UInt32 = 0

    Private Shared _BUY_DEADLINE As Integer = 4                   '0.8초 (매수 deadline)
    Private Shared _BUY_RETRY_DEADLINE As Integer = 200             '40초
    Private Shared _SEL_DEADLINE As Integer = 10                  '2초 (매도 deadline)
    '130531: 매도시 데드라인 넘겼을 때 재주문 하는 거 Selprice1이 주문가와 같으면 주문하지 않게 바꿔야 한다. 
    '130531: 그러기 위해선 OrderPrice가 재주문시 업데이트 되어야 한다.

    Public Property RestAmount As UInt32
        Get
            Dim rest_amount As UInt32
            SafeEnterTrace(_RestAmountKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            rest_amount = _RestAmount
            SafeLeaveTrace(_RestAmountKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return rest_amount
        End Get
        Set(value As UInt32)
            SafeEnterTrace(_RestAmountKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            _RestAmount = value
            SafeLeaveTrace(_RestAmountKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End Set
    End Property

    '생성되자마자 주문한다
    Public Sub New(ByVal parent_operation As et2_Operation, ByVal enter_or_exit As EnterOrExit, ByVal order_price As UInt32, ByVal order_amount As UInt32, ByVal account_manager As et1_AccountManager)
        ParentOperation = parent_operation
        _AccountManager = account_manager
        EnterExitState = enter_or_exit
        ThisAmount = order_amount
        RestAmount = ThisAmount
        OrderPrice = order_price

        If _AccountManager.AccountCat = 0 Then
            AccountString = MainAccountString
            AccountPW = MainAccountPW
        ElseIf _AccountManager.AccountCat = 1 Then
            AccountString = SubAccountString
            AccountPW = SubAccountPW
        Else ' if _AccountManager.AccountCat = 2 Then
            AccountString = TestAccountString
            AccountPW = TestAccountPW
        End If

        '처음 주문 받은 게 하한가인가 조사해서 하한가이면 LowLimitCaution Set한다
        If EnterExitState = EnterOrExit.EOE_Exit AndAlso OrderPrice = ParentOperation.LinkedSymbol.LowLimitPrice Then
            LowLimitCaution = True
        End If

        '부모 operation의 함수들에 이벤트 핸들러 건다
        AddHandler AllDealConfirmed, AddressOf parent_operation.AllDealConfirmedEventHandlerForOperator
        AddHandler DealInitiated, AddressOf parent_operation.DealInitiatedEventHandlerForOperator
        AddHandler CancelConfirmed, AddressOf parent_operation.CancelConfirmedEventHandlerForOperator
        AddHandler OrderBlanked, AddressOf parent_operation.OrderBlankedEventHandlerForOperator
        AddHandler OrderRejected, AddressOf parent_operation.OrderRejectedEventHandlerForOperator
        AddHandler OrderErrored, AddressOf parent_operation.OrderErroredEventHandlerForOperator
        Dim nReqID As Integer
        While 1
            Try
                '주문
                'If _CSPAT00600 IsNot Nothing Then
                'RemoveHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                'RemoveHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                '_CSPAT00600 = Nothing
                'End If

                '_CSPAT00600 = New XAQuery
                _CSPAT00600_List.Add(et31_XAQuery_Wrapper.NewOrUsed())
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
                    SetAmountUnderOrderBUY(order_price)
                Else 'If enter_or_exit = EnterOrExit.EOE_Exit Then
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "1")                                   '매수/매도 구분 (매도)
                    SetAmountUnderOrderSEL(order_price)
                End If
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '주문조건 구분 (없음)

                nReqID = _CSPAT00600.Request(False)
            Catch ex As Exception
                ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러!! " & ex.Message)
                Continue While
            End Try
            Exit While
        End While
        If nReqID < 0 Then
            'State = OperatorState.OS_ERROR          '에러상태로 전환
            ErrorLogging(ParentOperation.SymbolCode & " :" & "주문 전송 실패(" & nReqID & ")" & " 주문가는 " & order_price.ToString)
            'SafeEnterTrace(ParentOperation.LinkedSymbol.OneKey, 41)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            RaiseEvent OrderErrored(Me)
            'SafeLeaveTrace(ParentOperation.LinkedSymbol.OneKey, 53)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            '130618: 주문하면서 에러나는 데는 다 이벤트 발생하도록 넣어놨다. 다음은 태스크 리스트 참조.
        Else
            State = OperatorState.OS_ORDER_REQUESTED          '주문 전송된 상태로 전환
            _TimerTick = 0                                      '타이머틱 초기화
            '130614: 에러 발생시 처리하는 부분을 만들어야 한다.
            '_CuttingNumber += 1
            If SC0_CONFIRM_SUPPORT Then
                _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT000")
            End If
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 전송성공: " & order_price.ToString & "원, " & order_amount.ToString & "주")
        End If
    End Sub

    '종결자
    Public Sub Terminate()
        ReleaseCOMObj()
        '_CSPAT00600 = Nothing
        _CSPAT00600_List.Clear()
        _CSPAT00800 = Nothing
        RemoveHandler AllDealConfirmed, AddressOf ParentOperation.AllDealConfirmedEventHandlerForOperator
        RemoveHandler DealInitiated, AddressOf ParentOperation.DealInitiatedEventHandlerForOperator
        RemoveHandler CancelConfirmed, AddressOf ParentOperation.CancelConfirmedEventHandlerForOperator
        RemoveHandler OrderBlanked, AddressOf ParentOperation.OrderBlankedEventHandlerForOperator
        RemoveHandler OrderRejected, AddressOf ParentOperation.OrderRejectedEventHandlerForOperator
        RemoveHandler OrderErrored, AddressOf ParentOperation.OrderErroredEventHandlerForOperator
    End Sub

    Public Sub ReleaseCOMObj()
        For Each com_obj In _CSPAT00600_List
            com_obj.ReleaseCOM()
        Next
        If _CSPAT00800 IsNot Nothing Then
            _CSPAT00800.ReleaseCOM()
        End If
    End Sub

    Public Sub SetAmountUnderOrderBUY(ByVal order_price As UInt32)
        Dim local_call_price As CallPrices = ParentOperation.LinkedSymbol.LastCallPrices

        Select Case order_price
            Case local_call_price.BuyPrice1
                AmountUnderOrder = local_call_price.BuyAmount1
            Case local_call_price.BuyPrice2
                AmountUnderOrder = local_call_price.BuyAmount2
            Case local_call_price.BuyPrice3
                AmountUnderOrder = local_call_price.BuyAmount3
            Case local_call_price.BuyPrice4
                AmountUnderOrder = local_call_price.BuyAmount4
            Case local_call_price.BuyPrice5
                AmountUnderOrder = local_call_price.BuyAmount5
            Case Else
                AmountUnderOrder = 0
        End Select
    End Sub

    Public Sub SetAmountUnderOrderSEL(ByVal order_price As UInt32)
        Dim local_call_price As CallPrices = ParentOperation.LinkedSymbol.LastCallPrices

        Select Case order_price
            Case local_call_price.SelPrice1
                AmountUnderOrder = local_call_price.SelAmount1
            Case local_call_price.SelPrice2
                AmountUnderOrder = local_call_price.SelAmount2
            Case local_call_price.SelPrice3
                AmountUnderOrder = local_call_price.SelAmount3
            Case local_call_price.SelPrice4
                AmountUnderOrder = local_call_price.SelAmount4
            Case local_call_price.SelPrice5
                AmountUnderOrder = local_call_price.SelAmount5
            Case Else
                AmountUnderOrder = 0
        End Select
    End Sub

    '전량 취소 주문- [OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub CancelAll()
        'LowLimitCaution = True => 140509 : CancelAll은 매수상태에서만 불리고 LowLimitCaution일때만 불리는 것은 아니다. 그리고 매수상태이고 LowLimitCaution일 때 Operator가 해줄 일은 없다. Operation단에서 대책이 구현되어야 한다.
        If State = OperatorState.OS_ORDER_REQUESTED Then
            '2023.12.28 : 매수주문 후 주문확인이 들어오기 전에 매수강제취소 명령으로 이곳으로 들어오는 경우가 있음을 알았다. 이 경우에는 flag 만 세팅해놓고 매수주문확인이 들어왔을 때 바로 취소하게끔 해놓자.
            CancelRequestedFromOperation = True
        Else
            Dim nReqID As Integer
            While 1
                Try
                    '주문한다
                    If _CSPAT00800 Is Nothing Then
                        'RemoveHandler _CSPAT00800.ReceiveData, AddressOf _CSPAT00800_ReceiveData
                        _CSPAT00800 = et31_XAQuery_Wrapper.NewOrUsed()
                    End If
                    _CSPAT00800.ResFileName = "Res\CSPAT00800.res"
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrgOrdNo", 0, StartingOrder)        '원주문번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrdQty", 0, RestAmount.ToString)                     '취소수량

                    nReqID = _CSPAT00800.Request(False)
                Catch ex As Exception
                    ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러~~ " & ex.Message)
                    Continue While
                End Try
                Exit While
            End While
            If nReqID < 0 Then
                'State = OperatorState.OS_ERROR          '에러상태로 전환
                ErrorLogging(ParentOperation.SymbolCode & " :" & "취소주문 전송 실패(" & nReqID & ")")
                RaiseEvent OrderErrored(Me)
            Else
                CancelRequestedFromOperation = True
                State = OperatorState.OS_CANCELORDER_REQUESTED          '주문 전송된 상태로 전환
                If SC0_CONFIRM_SUPPORT Then
                    _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT002")
                End If
                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
            End If
        End If
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '140507:매도시 하한가전략은 리뷰완료되었다. 매수시 하한가전략의 리뷰가 필요하다.
    '전량 팔아버리기 (하한가 전략) - [OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub SellThemAll()
        LowLimitCaution = True                              '하한가비상 상태 set
        '130809: 8월 8일자 로그파일에서 종목 340을 주목하라. 체결을 기다리고 있는데 주문을 취소했는데 그동안 체결된 경우 대비책이 없다. 그리고 매수 상태에서 하한가 대비책이 없다.
        If State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL Then
            '체결을 기다리는 중이면 일단 취소한다.
            '일단취소
            Dim nReqID As Integer
            While 1
                Try
                    'CSPAT00800 주문.............................................................................................
                    If _CSPAT00800 Is Nothing Then
                        'RemoveHandler _CSPAT00800.ReceiveData, AddressOf _CSPAT00800_ReceiveData
                        _CSPAT00800 = et31_XAQuery_Wrapper.NewOrUsed()
                    End If
                    _CSPAT00800.ResFileName = "Res\CSPAT00800.res"
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrgOrdNo", 0, StartingOrder)        '원주문번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrdQty", 0, RestAmount.ToString)                     '취소수량

                    nReqID = _CSPAT00800.Request(False)
                Catch ex As Exception
                    ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러~~ " & ex.Message)
                    Continue While
                End Try
                Exit While
            End While
            If nReqID < 0 Then
                'State = OperatorState.OS_ERROR          '에러상태로 전환
                ErrorLogging(ParentOperation.SymbolCode & " :" & "취소주문 전송 실패(" & nReqID & ")")
                RaiseEvent OrderErrored(Me)
            Else
                State = OperatorState.OS_CANCELORDER_REQUESTED         '취소주문 상태로 바꿈
                If SC0_CONFIRM_SUPPORT Then
                    _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT002")
                End If
                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
            End If
        Else
            '체결을 기다리는 중이 아니면 일단 기다린다. 취소주문 내고 기다리는 temporary 상태이거나
            '일반 주문 냈는데 주문 확인 아직 안 온 경우일 수도 있다. 후자의 경우 200ms task에서 감시하여 주문확인 오고 LowLimitCaution이면 팔아버리는 로직 필요할 듯 하다.
        End If
        '130530: 새로 생긴 OS_CANCELORDER_REQUESTED_FOR_SELLTHEMALL 상태를 확인 받아서 받아서 재주문 하는 코드 집어넣자. => LowLimitCaution flag 전략으로 수정

    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '[OneKey] Timer tick - CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub Tm200ms_Tick()
        _TimerTick += 1
#If 1 Then

        If EnterExitState = EnterOrExit.EOE_Enter Then
            '매수상태
            BuyRetryTick += 1
            Dim temp_best_buy_price As UInt32 = ParentOperation.GetBestBuyPrice(OrderPrice, RestAmount, AmountUnderOrder)
            Dim temp_best_sel_price As UInt32 = ParentOperation.GetBestSelPrice(0, 0, 0)
            Dim price_active_gap As UInt32
            If temp_best_sel_price > temp_best_buy_price Then
                price_active_gap = temp_best_sel_price - temp_best_buy_price
                If price_active_gap > OrderPrice * 0.018 Then
                    Dim local_call_price As CallPrices = ParentOperation.LinkedSymbol.LastCallPrices
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "ActiveGap이 열렸다 " & temp_best_buy_price & ", " & local_call_price.BuyPrice1 & ", " & local_call_price.SelPrice1 & ", " & temp_best_sel_price)
                End If
            End If
#If 1 Then
            If State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL And _TimerTick > _BUY_DEADLINE Then
                '매수 주문이 deadline 시한을 넘기도록 체결되지 않았다. => 취소시키자

                Dim best_buy_price As UInt32 = ParentOperation.GetBestBuyPrice(OrderPrice, RestAmount, AmountUnderOrder)

                'If OrderPrice >= ParentOperation.LinkedSymbol.LastCallPrices.BuyPrice1 Or OrderPrice < best_buy_price Then
                'If OrderPrice >= ParentOperation.LinkedSymbol.LastCallPrices.BuyPrice1 Or OrderPrice = best_buy_price Then
                If ((best_buy_price < ParentOperation.BuyStandardPrice AndAlso OrderPrice = best_buy_price) Or
                    (best_buy_price >= ParentOperation.BuyStandardPrice AndAlso OrderPrice = NextCallPrice(ParentOperation.BuyStandardPrice, 0))) And
                    (BuyRetryTick <= _BUY_RETRY_DEADLINE) Then    '시간이 너무 지나도 매수 안 되면 취소 조건 안 맞아도 취소함.
                    '재주문 하지 않아도 될 상황에서는
                    _TimerTick = 0          '타이머는 리셋한다.
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소할 필요 없잖여: OrderPrice " & OrderPrice.ToString & "원, BuyPrice1 " & ParentOperation.LinkedSymbol.LastCallPrices.BuyPrice1.ToString & "원, best buy price " & best_buy_price.ToString)
                Else
                    '취소주문
                    Dim nReqID As Integer
                    While 1
                        Try
                            'CSPAT00800 주문.............................................................................................
                            If _CSPAT00800 Is Nothing Then
                                'RemoveHandler _CSPAT00800.ReceiveData, AddressOf _CSPAT00800_ReceiveData
                                _CSPAT00800 = et31_XAQuery_Wrapper.NewOrUsed()
                            End If
                            _CSPAT00800.ResFileName = "Res\CSPAT00800.res"
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrgOrdNo", 0, StartingOrder)        '원주문번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrdQty", 0, RestAmount.ToString)                     '취소수량

                            nReqID = _CSPAT00800.Request(False)
                        Catch ex As Exception
                            ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러~~ " & ex.Message)
                            Continue While
                        End Try
                        Exit While
                    End While
                    If nReqID < 0 Then
                        State = OperatorState.OS_ERROR          '에러상태로 전환
                        ErrorLogging(ParentOperation.SymbolCode & " :" & "취소주문 전송 실패(" & nReqID & ")")
                        RaiseEvent OrderErrored(Me)
                    Else
                        State = OperatorState.OS_CANCELORDER_REQUESTED          '주문 전송된 상태로 전환
                        If SC0_CONFIRM_SUPPORT Then
                            _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT002")
                        End If
                        MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주, BuyPrice1 " & ParentOperation.LinkedSymbol.LastCallPrices.BuyPrice1.ToString)
                    End If
                End If
            End If
#End If
        Else 'If EnterExitState = EnterOrExit.EOE_Exit
            '매도상태
            If State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL And _TimerTick > _SEL_DEADLINE Then
                '매도 주문이 deadline 시한을 넘기도록 체결되지 않았다. => 일단 취소시키자. 취소되면 재주문하는 방식으로 정정주문과 같은 효과얻을 것이다.

                If LowLimitCaution Then
                    '하한가 경고상태면 하한가로 매도 주문된 것을 취소하지 않고 그냥 둔다.
                    _TimerTick = 0          '타이머는 리셋한다.
                    'ElseIf OrderPrice = ParentOperation.LinkedSymbol.LastCallPrices.SelPrice1 Then
                Else
                    Dim best_sel_price As UInt32 = ParentOperation.GetBestSelPrice(OrderPrice, RestAmount, AmountUnderOrder)

                    If OrderPrice = best_sel_price Then
                        '재주문이 필요가 없으면
                        _TimerTick = 0          '타이머는 리셋한다.
                    Else
                        '취소주문
                        Dim nReqID As Integer
                        While 1
                            Try
                                'CSPAT00800 주문.............................................................................................
                                If _CSPAT00800 Is Nothing Then
                                    'RemoveHandler _CSPAT00800.ReceiveData, AddressOf _CSPAT00800_ReceiveData
                                    _CSPAT00800 = et31_XAQuery_Wrapper.NewOrUsed()
                                End If
                                _CSPAT00800.ResFileName = "Res\CSPAT00800.res"
                                _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrgOrdNo", 0, StartingOrder)        '원주문번호
                                _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                                _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                                _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                                _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrdQty", 0, RestAmount.ToString)                     '취소수량

                                nReqID = _CSPAT00800.Request(False)
                            Catch ex As Exception
                                ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러~~ " & ex.Message)
                                Continue While
                            End Try
                            Exit While
                        End While
                        If nReqID < 0 Then
                            'State = OperatorState.OS_ERROR          '에러상태로 전환
                            ErrorLogging(ParentOperation.SymbolCode & " :" & "취소주문 전송 실패(" & nReqID & ")")
                            RaiseEvent OrderErrored(Me)
                        Else
                            State = OperatorState.OS_CANCELORDER_REQUESTED          '주문 전송된 상태로 전환
                            If SC0_CONFIRM_SUPPORT Then
                                _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT002")
                            End If
                            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                        End If
                    End If
                End If
            End If
        End If
#End If
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '주문 확인 data callback
    Private Sub _CSPAT00600_ReceiveData(ByVal szTrCode As String) 'Handles _CSPAT00600.ReceiveData
        SafeEnterTrace(OrderConfirmEventTracingKey, 1)      '[] Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        OrderConfirmEventCount += 1
        SafeLeaveTrace(OrderConfirmEventTracingKey, 10)     '[] Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'MessageLogging("OrderConfirmEventCount " & OrderConfirmEventCount.ToString)  20200621: 불필요 로그 삭제

        Dim order_number As String = _CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdNo", 0)
        Dim do_we_need_to_run_post_thread As Boolean
        If SC0_CONFIRM_SUPPORT Then
            do_we_need_to_run_post_thread = _AccountManager.SubConfirmCheck_RemoveOrderAfterConfirmed(Me, "SONAT000")
        Else
            do_we_need_to_run_post_thread = True
        End If
        If do_we_need_to_run_post_thread Then
            Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf _CSPAT00600_ReceiveData_PostThread)
            Dim parameters() As Object = {order_number}
            '160218: COM 객체의 callback thread 안에서 다른 COM 객체 생성하고 하는 등의 작업 하지 않기 위해 아래와 같이 일단 현재 thread는 빨리 끝내고 PostThread에서 원하는 작업을 한다.
            CSPAT00600_ReceiveData_Thread = Threading.Thread.CurrentThread
            simulate_thread.Start(parameters)
        Else
            'the post thread was already run by SC0
        End If
    End Sub

    Public Sub _CSPAT00600_ReceiveData_PostThread(ByVal parameters As Object())
        Dim order_number As String = parameters(0)
        While CALLBACK_FAST_RETURN_WAIT AndAlso CSPAT00600_ReceiveData_Thread IsNot Nothing AndAlso CSPAT00600_ReceiveData_Thread.ThreadState <> Threading.ThreadState.Running
            Threading.Thread.Yield()
        End While

        'Dim order_number As String = _CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdNo", 0)
#If 0 Then
        Dim temp_str1 As String = _CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdQty", 0)
        Dim temp_str2 As String = _CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdPrc", 0)
        Dim order_price As UInt32 = 0
        Dim order_amount As UInt32 = 0
        Try
            order_price = Convert.ToUInt32(Convert.ToSingle(temp_str1))
        Catch ex As Exception
            ErrorLogging("변환실패: temp_str1=" & temp_str1)
        End Try
        Try
            order_amount = Convert.ToUInt32(Convert.ToSingle(temp_str2))
        Catch ex As Exception
            ErrorLogging("변환실패: temp_str2=" & temp_str2)
        End Try

        'Dim order_amount As UInt32 = Convert.ToUInt32(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdQty", 0))
        'Dim order_price As UInt32 = Convert.ToUInt32(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdPrc", 0))

        If order_number <> "" Then
            '주문 번호랑 수량을 등록한다. (호가잔량 compensation을 위해서)
            '2020.03.01: 00600이 계속 안 불리는 것 같아 디버깅 코드도 좀 집어넣었는데, 아마도 데이터 변환 과정에서 오류 때문인 것 같다.
            ' order number가 string type인데 아래 함수에서 넘겨주는 것은 ULong 으로 변환해서 넘겨주게 되어 있었다.
            ' 이 과정에서 오류가 나는 경우가 있어 문제가 되지 않았을까... 그 때 SC1 안 불리는 것 처럼 보였던 것도 추적해보니 이 때문이었던 것 같다.
            ParentOperation.LinkedSymbol.RegisterMyOrder(EnterExitState, _AccountManager, order_number, order_price, order_amount)
        End If
#End If
        SafeEnterTrace(OrderConfirmPostTracingKey, 1)      '[] Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        OrderConfirmPostCount += 1
        SafeLeaveTrace(OrderConfirmPostTracingKey, 10)     '[] Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'MessageLogging("OrderConfirmPostCount " & OrderConfirmPostCount.ToString)  20200621: 불필요 로그 삭제 

        CSPAT00600_Data.Clear()
#If 0 Then
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "RecCnt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "AcntNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "InptPwd", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "IsuNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdQty", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdPrc", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "BnsTpCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdprcPtnCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "PrgmOrdprcPtnCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "StslAbleYn", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "StslOrdprcTpCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "CommdaCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "MgntrnCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "LoanDt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "MbrNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdCndiTpCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "StrtgCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "GrpId", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OrdSeqNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "PtflNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "BskNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "TrchNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "ItemNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "OpDrtnNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "LpYn", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock1", "CvrgTpCode", 0))

        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "RecCnt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdTime", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdMktCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdPtnCode", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "ShtnIsuNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "MgempNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdAmt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "SpareOrdNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "CvrgSeqno", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "RsvOrdNo", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "SpotOrdQty", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "RuseOrdQty", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "MnyOrdAmt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "SubstOrdAmt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "RuseOrdAmt", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "AcntNm", 0))
        CSPAT00600_Data.Add(_CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "IsuNm", 0))
#End If
        '130826: 아래에서 하한가비상 체크하는 로직 들어가야 할 듯... 그리고 대체 매수중일 때는 하한가 체크 왜 안되는 거야??????
        SafeEnterTrace(ParentOperation.OperationKey, 142)      '[OneKey] Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If State = OperatorState.OS_ORDER_REQUESTED Then
            '주문 확인을 기다리는 상태이면
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 확인성공, " & order_number)
            If IsValidOrderNumber(order_number) Then
                StartingOrder = Convert.ToUInt64(order_number)        '주문번호만 따면 된다.
                State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL

                '체결확인 들어감
                Dim deal_price As Double
                Dim rest_amount As UInt32 = RestAmount
                Dim request_result As ORDER_CHECK_REQUEST_RESULT = _AccountManager.OrderDealCheck(StartingOrder, Me, deal_price, rest_amount)

                If request_result = ORDER_CHECK_REQUEST_RESULT.OCRR_DEAL_CONFIRMED_COMPLETELY Then
                    '이미 전부 체결확인 되었음
                    DealPrice = deal_price              '체결가 저장
                    RestAmount = rest_amount            '미체결 수량 저장
                    State = OperatorState.OS_DONE        '종료 상태로 바꿈
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "일부분할주문체결완료. 체결가 " & deal_price.ToString & ", 수량 " & ThisAmount)
                    RaiseEvent AllDealConfirmed(Me, ThisAmount)           'Operation 객체에 알림
                ElseIf request_result = ORDER_CHECK_REQUEST_RESULT.OCRR_DEAL_REJECTED Then
                    '이미 주문 거부되었음
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문이거부되었다. 주문가 " & OrderPrice.ToString & ", 수량 " & ThisAmount & ", 주문번호 " & order_number)
                    State = OperatorState.OS_DONE
                    RaiseEvent OrderRejected(Me)        'Operation 객체에 알림
                Else
                    '2020.10.13: 보라티알(250000) 매수 주문이 4주 중 3주가 먼저 체결된 SC1 이벤트가 주문확인00600 이벤트보다 먼저 들어왔는데
                    '2020.10.13: 이게 RestAmount에 반영이 안 되어 매수체결이 다 되었는데도 이를 인지 못해서 청산이 안 되는 문제가 발생했다.
                    '2020.10.13: 이를 해결하기 위하여 아래 한 줄 RestAmount = rest_amount 을 추가했다.
                    RestAmount = rest_amount
                    If LowLimitCaution OrElse CancelRequestedFromOperation Then
                        If EnterExitState = EnterOrExit.EOE_Exit Then
                            '이미 하한가로 매도주문이 들어간 상태다. 아무것도 하면 안 되고 그냥 기다려야 한다.
                        Else
                            '하한가 비상상태 또는 늦은 매수주문 강제취소에서 매수 주문이 확인 된 상황이므로 바로 취소주문들어간다.
                            Dim nReqID As Integer
                            While 1
                                Try
                                    'CSPAT00800 주문.............................................................................................
                                    If _CSPAT00800 Is Nothing Then
                                        'RemoveHandler _CSPAT00800.ReceiveData, AddressOf _CSPAT00800_ReceiveData
                                        _CSPAT00800 = et31_XAQuery_Wrapper.NewOrUsed()
                                    End If
                                    _CSPAT00800.ResFileName = "Res\CSPAT00800.res"
                                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrgOrdNo", 0, StartingOrder)        '원주문번호
                                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                                    _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrdQty", 0, RestAmount.ToString)                     '취소수량

                                    nReqID = _CSPAT00800.Request(False)
                                Catch ex As Exception
                                    ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러~~ " & ex.Message)
                                    Continue While
                                End Try
                                Exit While
                            End While
                            If nReqID < 0 Then
                                'State = OperatorState.OS_ERROR          '에러상태로 전환
                                ErrorLogging(ParentOperation.SymbolCode & " :" & "하한가비상 또는 늦은 매수주문 강제취소로 인한 취소주문 전송 실패(" & nReqID & ")")
                                RaiseEvent OrderErrored(Me)
                            Else
                                State = OperatorState.OS_CANCELORDER_REQUESTED         '취소주문 상태로 바꿈
                                If SC0_CONFIRM_SUPPORT Then
                                    _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT002")
                                End If
                                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "하한가비상 또는 늦은 매수주문 강제취소로 취소주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                            End If
                        End If
                    Else
                        '실시간 신호로 체결 확인될 때까지 기다림
                    End If
                End If
            Else
                'OpStatus = OperationState.OS_ERROR
                '2023.12.29 : 매수주문 확인에서 주문번호 공백은 흔하다. 그래서 ErrorLogging 에서 MessageLogging 으로 바꾸기로 한다.
                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 확인 들어왔는데 주문번호가 공백임, 주문가 " & OrderPrice)
                RaiseEvent OrderBlanked(Me)
            End If
        ElseIf State = OperatorState.OS_CANCELORDER_REQUESTED Then
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문확인 들어오기 전 취소주문하여 주문확인이 취소주문 후에 들어온 경우다.. 맞지?")
        Else
            ' State = OperatorState.OS_ERROR
            ErrorLogging(ParentOperation.SymbolCode & " :" & "엉뚱한 주문 확인 데이터가 들어왔음")
        End If
        SafeLeaveTrace(ParentOperation.OperationKey, 143)      '[OneKey] Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '주문 확인 message callback
    Private Sub _CSPAT00600_ReceiveMessage(ByVal bIsSystemError As Boolean, ByVal nMessageCode As String, ByVal szMessage As String) 'Handles _CSPAT00600.ReceiveMessage
        MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문확인 메시지: " & szMessage & ", " & bIsSystemError.ToString & ", " & nMessageCode.ToString)
    End Sub

    '취소주문 확인 data callback
    Private Sub _CSPAT00800_ReceiveData(ByVal szTrCode As String) Handles _CSPAT00800.ReceiveData
        Dim order_number As String = _CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdNo", 0)
        Dim do_we_need_to_run_post_thread As Boolean
        If SC0_CONFIRM_SUPPORT Then
            do_we_need_to_run_post_thread = _AccountManager.SubConfirmCheck_RemoveOrderAfterConfirmed(Me, "SONAT002")
        Else
            do_we_need_to_run_post_thread = True
        End If
        If do_we_need_to_run_post_thread Then
            Dim post_thread As Threading.Thread = New Threading.Thread(AddressOf _CSPAT00800_ReceiveData_PostThread)
            Dim parameters() As Object = {order_number}
            post_thread.IsBackground = True
            '160218: COM 객체의 callback thread 안에서 다른 COM 객체 생성하고 하는 등의 작업 하지 않기 위해 아래와 같이 일단 현재 thread는 빨리 끝내고 PostThread에서 원하는 작업을 한다.
            CSPAT00800_ReceiveData_Thread = Threading.Thread.CurrentThread
            post_thread.Start(parameters)
        End If
    End Sub

    Public Sub _CSPAT00800_ReceiveData_PostThread(ByVal parameters As Object())
        Dim order_number As String = parameters(0)
        While CALLBACK_FAST_RETURN_WAIT AndAlso CSPAT00800_ReceiveData_Thread IsNot Nothing AndAlso CSPAT00800_ReceiveData_Thread.ThreadState <> Threading.ThreadState.Running
            Threading.Thread.Yield()
        End While

        CSPAT00800_Data.Clear()
#If 0 Then       '_CSPAT00800 를 critical section 밖에서 access 해서 access violation나는 것 같다. 어차피 필요없으니 없애버리자.
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "RecCnt", 0))                   '레코드갯수
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "OrgOrdNo", 0))                   '원주문번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "AcntNo", 0))                   '계좌번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "InptPwd", 0))                  '입력비밀번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "IsuNo", 0))                    '종목번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "OrdQty", 0))                   '주문수량
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "CommdaCode", 0))               '통신매체코드
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "GrpId", 0))                '그룹ID
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "StrtgCode", 0))            '전략코드
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "OrdSeqNo", 0))             '주문회차
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "PtflNo", 0))               '포트폴리오번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "BskNo", 0))                '바스켓번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "TrchNo", 0))               '트렌치번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock1", "ItemNo", 0))               '아이템번호

        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "RecCnt", 0))           '레코드갯수
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdNo", 0))            '주문번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "PrntOrdNo", 0))            '모주문번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdTime", 0))              '주문시각
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdMktCode", 0))           '주문시장코드
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdPtnCode", 0))           '주문유형코드
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "ShtnIsuNo", 0))            '단축종목번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "PrgmOrdprcPtnCode", 0))            '프로그램호가유형코드
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "StslOrdprcTpCode", 0))            '공매도호가구분
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "StslAbleYn", 0))            '공매도가능여부
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "MgntrnCode", 0))            '신용거래코드
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "LoanDt", 0))            '대출일
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "CvrgOrdTp", 0))            '반대매매주문구분
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "LpYn", 0))            '유동성공급자여부
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "MgempNo", 0))              '관리사원번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "BnsTpCode", 0))              '매매구분
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "SpareOrdNo", 0))           '예비주문번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "CvrgSeqno", 0))            '반대매매일련번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "RsvOrdNo", 0))             '예약주문번호
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "AcntNm", 0))            '계좌명
        CSPAT00800_Data.Add(_CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "IsuNm", 0))            '종목명
#End If
        SafeEnterTrace(ParentOperation.OperationKey, 144)      '[OneKey] Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If State = OperatorState.OS_CANCELORDER_REQUESTED Then
            '130812:취소주문 확인 과정 공부좀 하자. 그리고 LowLimitCaution일 때 하한가로 주문 날리는 거 여기 어디에 둬야되나 공부하자.
            '취소주문 확인을 기다리는 상태이면
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 확인성공")

            'If _CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdNo", 0) <> "" Then
            If IsValidOrderNumber(order_number) Then
                'CancelOrder = Convert.ToInt32(CSPAT00600_Data(15))        '주문번호만 따면 된다.
                State = OperatorState.OS_CANCELORDER_CONFIRMED_CHECKING_DEAL

                '취소체결확인은 할 필요가 없음. 왜냐면 취소체결확인이 되었다면 상태가 다른 상태로 바뀌었기 때문에 이쪽으로 안 들어옴
#If 0 Then
                '취소체결확인 들어감

                'Dim deal_price As Double
                'Dim rest_amount As UInt32 = ThisAmount
                Dim deal_price As Double
                Dim rest_amount As UInt32 = RestAmount
                Dim request_result As ORDER_CHECK_REQUEST_RESULT = _AccountManager.CancelOrderDealCheck(CancelOrder, Me, deal_price, rest_amount)
                '130422:취소 되어도 취소되기전 일부 체결된 수량은 카운트되어야 한다.
                '130422:이 곳에서 해당 작업을 수행하도록 하고 Cancel 완료 event handler에서도 해당작업 수행하도록 한다.
                If rest_amount < ThisAmount Then
                    '취소되기전 일부 체결 사실이 있다
                    DealPrice = deal_price
                    RestAmount = rest_amount
                Else
                    '최초수량 전량 취소되었다
                    DealPrice = 0
                    RestAmount = ThisAmount
                End If

                If request_result = ORDER_CHECK_REQUEST_RESULT.OCRR_CANCEL_CONFIRMED Then
                    '이미 전부 취소체결확인 되었음

                    State = OperatorState.OS_DONE        '종료 상태로 바꿈
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & ParentOperation.SymbolCode & " : 부분취소주문체결 완료. 원주문가 " & OrderPrice.ToString)
                    RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                Else
                    '실시간 신호로 취소 체결 확인될 때까지 기다림
                End If
#End If
            Else
                'OpStatus = OperationState.OS_ERROR
                ErrorLogging("A" & ParentOperation.SymbolCode & " :" & "취소주문 확인 들어왔는데 주문번호가 공백임, 주문가 " & OrderPrice)

                RaiseEvent OrderBlanked(Me)

                '2024.03.11 : 취소주문 확인이 주문번호 공백으로 들어오면 취소할 수량이 남아 있는 경우에 한해 무조건 다시 취소주문을 날리도록 하기로 함.
                If RestAmount > 0 Then
                    Dim nReqID As Integer
                    While 1
                        Try
                            'CSPAT00800 주문.............................................................................................
                            If _CSPAT00800 Is Nothing Then
                                'RemoveHandler _CSPAT00800.ReceiveData, AddressOf _CSPAT00800_ReceiveData
                                _CSPAT00800 = et31_XAQuery_Wrapper.NewOrUsed()
                            End If
                            _CSPAT00800.ResFileName = "Res\CSPAT00800.res"
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrgOrdNo", 0, StartingOrder)        '원주문번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                            _CSPAT00800.SetFieldData("CSPAT00800InBlock1", "OrdQty", 0, RestAmount.ToString)                     '취소수량

                            nReqID = _CSPAT00800.Request(False)
                        Catch ex As Exception
                            ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러~~ " & ex.Message)
                            Continue While
                        End Try
                        Exit While
                    End While
                    If nReqID < 0 Then
                        'State = OperatorState.OS_ERROR          '에러상태로 전환
                        ErrorLogging(ParentOperation.SymbolCode & " :" & "취소주문 블랭크됨으로 인한 재취소주문 전송 실패(" & nReqID & ")")
                        RaiseEvent OrderErrored(Me)
                    Else
                        State = OperatorState.OS_CANCELORDER_REQUESTED         '취소주문 상태로 바꿈
                        If SC0_CONFIRM_SUPPORT Then
                            _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT002")
                        End If
                        MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 블랭크됨으로 인한 재취소주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                    End If
                End If
            End If
        ElseIf State = OperatorState.OS_CANCELORDER_CHECKED_FIRST_WAIT_CONFIRM Then
            '실시간 확인으로 이미 취소주문 체결되고나서 취소주문 확인 들어옴.
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "실시간 확인으로 이미 취소주문 체결되고나서 취소주문 확인 들어옴")
#If 1 Then
            If EnterExitState = EnterOrExit.EOE_Enter Then
                '매수상태였으면
                If CancelRequestedFromOperation Then
                    '180110: 상부에서 온 취소명령이다. 위에 보고하라
                    CancelRequestedFromOperation = False
                    State = OperatorState.OS_DONE        '종료 상태로 바꿈
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "부분취소주문체결 완료. 원주문가 " & OrderPrice.ToString)
                    RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                Else
                    '180110: 재주문을 위한 취소이다. 재주문하라.
                    If BuyRetryTick > _BUY_RETRY_DEADLINE Then
                        '180110: 하지만 너무 오래 매수 안 돼서 그냥 취소하자
                        State = OperatorState.OS_DONE        '종료 상태로 바꿈
                        ParentOperation.BuyFailCount = ParentOperation.BuyFailCount + 1
                        MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "너무 오래 매수 안 돼 retry 그만하고 진짜 취소함. 원주문가 " & OrderPrice.ToString & ", Buy Fail Count " & ParentOperation.BuyFailCount)
                        RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                    Else
                        '180110: 진짜 재주문이다.
                        If LowLimitCaution Then
                            '하한가비상상황이면 => 사지 않는다.. 아마 이런 경우 없을 것이다
                            State = OperatorState.OS_DONE        '종료 상태로 바꿈
                            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "하한가 비상상황에서 매수 취소임. 진짜 취소함. 원주문가 " & OrderPrice.ToString)
                            RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                        Else
                            '180110: 진짜진짜 재주문이다.
                            '일반상황이면
                            Dim best_buy_price As UInt32 = ParentOperation.GetBestBuyPrice(OrderPrice, RestAmount, AmountUnderOrder)

                            If best_buy_price < ParentOperation.BuyStandardPrice Then
                                OrderPrice = best_buy_price
                            Else
                                OrderPrice = NextCallPrice(ParentOperation.BuyStandardPrice, 0)
                            End If
                            SetAmountUnderOrderBUY(OrderPrice)

                            '130523:재주문을 구현한다.
                            Dim nReqID As Integer
                            While 1
                                Try
                                    '주문
                                    'If _CSPAT00600 IsNot Nothing Then
                                    'RemoveHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                                    'RemoveHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                                    '_CSPAT00600 = Nothing
                                    'End If
                                    '_CSPAT00600 = New XAQuery
                                    _CSPAT00600_List.Add(et31_XAQuery_Wrapper.NewOrUsed())
                                    AddHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                                    AddHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage

                                    _CSPAT00600.ResFileName = "Res\CSPAT00600.res"
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, RestAmount.ToString)                     '매수수량
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, OrderPrice.ToString & ".00")                   '주문가
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "2")                                   '매수/매도 구분 (매수)
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '주문조건 구분 (없음)
                                    nReqID = _CSPAT00600.Request(False)
                                Catch ex As Exception
                                    ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러!! " & ex.Message)
                                    Continue While
                                End Try
                                Exit While
                            End While

                            If nReqID < 0 Then
                                'State = OperatorState.OS_ERROR          '에러상태로 전환
                                ErrorLogging(ParentOperation.SymbolCode & " :" & "주문 전송 실패(" & nReqID & ")")
                                RaiseEvent OrderErrored(Me)
                            Else
                                State = OperatorState.OS_ORDER_REQUESTED          '주문 전송된 상태로 전환
                                'LowLimitCaution = False                         '200ms task에서 모니터되지 않기 위해 Flag를 False로 놓는다. 2024.02.18 : 어차피 False 였다. 다시 세팅할 필요 없다.
                                _TimerTick = 0                                      '타이머틱 초기화
                                '_CuttingNumber += 1
                                If SC0_CONFIRM_SUPPORT Then
                                    _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT000")
                                End If
                                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                            End If
                        End If
                    End If
                End If
                '130708: 취소쪽 그리고 매수쪽은 어느정도 확인 된 것 같아보임. 취소쪽 매도쪽과, 일반체결쪽 매도쪽은 확인이 필요해보임.
                '190110: 매수주문 취소시 재주문 구현 필요함.
            Else
                '매도상태였으면
                '바로 재주문 들어간다.
                '가격 결정
                'Dim init_price As UInt32
                If LowLimitCaution Then
                    '하한가비상상황이면
                    OrderPrice = ParentOperation.LinkedSymbol.LowLimitPrice          '하한가로 판다
                Else
                    Dim local_call_price As CallPrices = ParentOperation.LinkedSymbol.LastCallPrices
                    '일반상황이면
                    If local_call_price.SelPrice1 = 0 Then
                        '상한가 상황 => BuyPrice1로 판다.
                        OrderPrice = local_call_price.BuyPrice1
                    Else
                        '일반상황이면
                        Dim best_sel_price As UInt32 = ParentOperation.GetBestSelPrice(OrderPrice, RestAmount, AmountUnderOrder)
                        OrderPrice = best_sel_price
                    End If
                End If
                SetAmountUnderOrderSEL(OrderPrice)
                '130523:재주문을 구현한다.
                Dim nReqID As Integer
                While 1
                    Try
                        'If _CSPAT00600 IsNot Nothing Then
                        'RemoveHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                        'RemoveHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                        '_CSPAT00600 = Nothing
                        'End If
                        '_CSPAT00600 = New XAQuery
                        _CSPAT00600_List.Add(et31_XAQuery_Wrapper.NewOrUsed())
                        AddHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                        AddHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                        '주문
                        _CSPAT00600.ResFileName = "Res\CSPAT00600.res"
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, RestAmount.ToString)                     '매수수량
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, OrderPrice.ToString & ".00")                   '주문가
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "1")                                   '매수/매도 구분 (매도)
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '주문조건 구분 (없음)

                        nReqID = _CSPAT00600.Request(False)
                    Catch ex As Exception
                        ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러!! " & ex.Message)
                        Continue While
                    End Try
                    Exit While
                End While
                If nReqID < 0 Then
                    'State = OperatorState.OS_ERROR          '에러상태로 전환
                    ErrorLogging(ParentOperation.SymbolCode & " :" & "주문 전송 실패(" & nReqID & ")")
                    RaiseEvent OrderErrored(Me)
                Else
                    State = OperatorState.OS_ORDER_REQUESTED          '주문 전송된 상태로 전환
                    'LowLimitCaution = False                         '200ms task에서 모니터되지 않기 위해 Flag를 False로 놓는다. 2024.02.18 : 하한가비상 상황에서 매도면 취소된 것도 이상하지만, 취소되었다 하더라도 다시 하한가로 재매도되어야 하며, False 로 두면 안 된다.
                    _TimerTick = 0                                      '타이머틱 초기화
                    '_CuttingNumber += 1
                    If SC0_CONFIRM_SUPPORT Then
                        _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT000")
                    End If
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                End If
            End If
#End If
        Else
            'State = OperatorState.OS_ERROR
            ErrorLogging(ParentOperation.SymbolCode & " :" & "엉뚱한 취소 주문 확인 데이터 들어왔는데 혹시 취소주문 확인 event 두 번 불렸나? State=" & State.ToString & ", RestAmount=" & RestAmount.ToString)
        End If
        SafeLeaveTrace(ParentOperation.OperationKey, 145)      '[OneKey] Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'SafeLeaveTrace(OrderNumberListKey, 22)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '주문체결 확인 이벤트 받음 - [OneKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub ReceiveDealConfirmed(ByVal deal_price As Double)
        If State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL Then
            '체결확인 되었음
            DealPrice = deal_price              '매수체결가 저장
            RestAmount = 0                          '남은 수량은 0
            State = OperatorState.OS_DONE        '종료 상태로 바꿈
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "거래수량 다 채웠음. 체결가 " & deal_price.ToString & ", 수량 " & ThisAmount)
            RaiseEvent AllDealConfirmed(Me, ThisAmount)           'Operation 객체에 알림. 전에 deal_amount였던 걸 ThisAmount로 고침
        ElseIf State = OperatorState.OS_CANCELORDER_REQUESTED Or State = OperatorState.OS_CANCELORDER_CONFIRMED_CHECKING_DEAL Then
            '취소했는데 체결 들어온 경우
            DealPrice = deal_price              '매수체결가 저장
            RestAmount = 0                          '남은 수량은 0
            State = OperatorState.OS_DONE        '종료 상태로 바꿈
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소 안 먹히고 체결됨. 체결가 " & deal_price.ToString & ", 수량 " & ThisAmount)
            RaiseEvent AllDealConfirmed(Me, ThisAmount)           'Operation 객체에 알림. 전에 deal_amount였던 걸 ThisAmount로 고침
        Else
            'State = OperatorState.OS_ERROR
            ErrorLogging(ParentOperation.SymbolCode & " :" & "어? 주문 안 했는데?")
        End If
    End Sub
    '[OneKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '주문 일부 체결 확인 이벤트 받음 - [OperationKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub ReceiveDealInitiated(ByVal deal_price As Double, ByVal rest_amount As UInt32)
        If State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL Then
            '체결확인 되었음
            DealPrice = deal_price              '매수체결가 저장       '굳이 할 필요는 없어보인다.
            RestAmount = Math.Min(RestAmount, rest_amount)                  '남은 수량은 남은 수량. Min을 하는 이유는 연달아 체결되었을 때 SC1 메시지의 순서가 뒤바뀌어 들어올 수도 있을 것 같기 때문
            'State = OperatorState.OS_DONE        '종료 상태로 바꿈
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 일부 체결됨. 체결가 " & deal_price.ToString & ", 레스트 " & RestAmount)
            RaiseEvent DealInitiated(Me)           'Operation 객체에 알림
        ElseIf State = OperatorState.OS_CANCELORDER_REQUESTED Or State = OperatorState.OS_CANCELORDER_CONFIRMED_CHECKING_DEAL Then
            '취소했는데 체결 들어온 경우
            DealPrice = deal_price              '매수체결가 저장       '위에는 모르겠지만 여기서는 반드시 필요함. 나중에 이걸로 BoughtAmount 계산 조건이 만들어짐
            RestAmount = Math.Min(RestAmount, rest_amount)                  'ResetAmount update도 필요한 것 같아서 위에서 복사해왔다.
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소 안 먹히고 일부 체결됨. 체결가 " & deal_price.ToString & ", 수량 " & ThisAmount)
            '2024.01.26 : prethreshold 상태에서 STOP_BUYING 했을 때 아래 한 줄 필요하다. 이게 없어서 2024.01.26 09:20:19 쯤 체결되고나서 청산 안되는 문제가 생겼었다.
            RaiseEvent DealInitiated(Me)           'Operation 객체에 알림
        Else
            'State = OperatorState.OS_ERROR
            ErrorLogging(ParentOperation.SymbolCode & " :" & "어? 주문 안 했는데?")
        End If
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '130704:Operator에서 DealPrice는 단 한 번 업데이트된다. 굳이 기존에 일부체결 사실이 있는지 확인할 필요가 없다. 게다가 이미 평균된 값이다.
    '130705:cyclic task에서 체크되는 부분 훑어보자. ........................................................................................
    '주문취소 확인 이벤트 받음 - [OperationKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub ReceiveCancelConfirmed(ByVal cancel_order_number As UInt32, ByRef deal_price As Double, ByRef rest_amount As UInt32)
        If State = OperatorState.OS_CANCELORDER_CONFIRMED_CHECKING_DEAL Then
            '취소 확인 되었음
            '130423취소 전에 일부 체결내역이 있는지 deal_price 및 rest_amount 체크해봐야함
            If rest_amount < ThisAmount Then
                '취소되기전 일부 체결 사실이 있다.
                '141015: DealPrice와 RestAmount는 이미 DealInitiated event에 의해 update 되어 있다.
                'DealPrice = deal_price              '체결가 저장
                'RestAmount = rest_amount
            Else
                '최초수량 전량 취소되었다 => 아니다 진짜 전량 취소되었는지 확인해야 한다.
                Dim deal_confirmed_before_cancel_confirmed As UInt32 = ThisAmount - RestAmount
                If deal_confirmed_before_cancel_confirmed > 0 Then
                    'DealPrice와 RestAmount는 이미 DealInitiated event에 의해 update 되어 있다.
                Else
                    DealPrice = 0
                    RestAmount = ThisAmount
                End If
            End If
#If 1 Then
            If EnterExitState = EnterOrExit.EOE_Enter Then
                '매수상태였으면
                If CancelRequestedFromOperation Then
                    '180110: 상부에서 온 취소명령이다. 위에 보고하라
                    CancelRequestedFromOperation = False
                    State = OperatorState.OS_DONE        '종료 상태로 바꿈
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "부분취소주문체결 완료. 원주문가 " & OrderPrice.ToString)
                    RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                Else
                    '180110: 재주문을 위한 취소이다. 재주문하라.
                    If BuyRetryTick > _BUY_RETRY_DEADLINE Then
                        '180110: 하지만 너무 오래 매수 안 돼서 그냥 취소하자
                        State = OperatorState.OS_DONE        '종료 상태로 바꿈
                        ParentOperation.BuyFailCount = ParentOperation.BuyFailCount + 1
                        MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "너무 오래 매수 안 돼 retry 그만하고 진짜 취소함. 원주문가 " & OrderPrice.ToString & ", Buy Fail Count " & ParentOperation.BuyFailCount)
                        RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                    Else
                        '180110: 진짜 재주문이다.
                        If LowLimitCaution Then
                            '하한가비상상황이면 => 사지 않는다.. 아마 이런 경우 없을 것이다
                            State = OperatorState.OS_DONE        '종료 상태로 바꿈
                            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "하한가 비상상황에서 매수 취소임. 진짜 취소함. 원주문가 " & OrderPrice.ToString)
                            RaiseEvent CancelConfirmed(Me)           'Operation 객체에 알림
                        Else
                            '180110: 진짜진짜 재주문이다.
                            '일반상황이면
                            Dim best_buy_price As UInt32 = ParentOperation.GetBestBuyPrice(OrderPrice, RestAmount, AmountUnderOrder)

                            If best_buy_price < ParentOperation.BuyStandardPrice Then
                                OrderPrice = best_buy_price
                            Else
                                OrderPrice = NextCallPrice(ParentOperation.BuyStandardPrice, 0)
                            End If
                            SetAmountUnderOrderBUY(OrderPrice)

                            '130523:재주문을 구현한다.
                            Dim nReqID As Integer
                            While 1
                                Try
                                    '주문
                                    'If _CSPAT00600 IsNot Nothing Then
                                    'RemoveHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                                    'RemoveHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                                    '_CSPAT00600 = Nothing
                                    'End If
                                    '_CSPAT00600 = New XAQuery
                                    _CSPAT00600_List.Add(et31_XAQuery_Wrapper.NewOrUsed())
                                    AddHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                                    AddHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage

                                    _CSPAT00600.ResFileName = "Res\CSPAT00600.res"
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, RestAmount.ToString)                     '매수수량
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, OrderPrice.ToString & ".00")                   '주문가
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "2")                                   '매수/매도 구분 (매수)
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '주문조건 구분 (없음)
                                    nReqID = _CSPAT00600.Request(False)
                                Catch ex As Exception
                                    ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러!! " & ex.Message)
                                    Continue While
                                End Try
                                Exit While
                            End While

                            If nReqID < 0 Then
                                'State = OperatorState.OS_ERROR          '에러상태로 전환
                                ErrorLogging(ParentOperation.SymbolCode & " :" & "주문 전송 실패(" & nReqID & ")")
                                RaiseEvent OrderErrored(Me)
                            Else
                                State = OperatorState.OS_ORDER_REQUESTED          '주문 전송된 상태로 전환
                                'LowLimitCaution = False                         '200ms task에서 모니터되지 않기 위해 Flag를 False로 놓는다. 2024.02.18 : 어차피 False 였다. 다시 세팅할 필요 없다.
                                _TimerTick = 0                                      '타이머틱 초기화
                                '_CuttingNumber += 1
                                If SC0_CONFIRM_SUPPORT Then
                                    _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT000")
                                End If
                                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                            End If
                        End If
                    End If
                End If
                '130708: 취소쪽 그리고 매수쪽은 어느정도 확인 된 것 같아보임. 취소쪽 매도쪽과, 일반체결쪽 매도쪽은 확인이 필요해보임.
                '190110: 매수주문 취소시 재주문 구현 필요함.
            Else
                '매도상태였으면
                '바로 재주문 들어간다.
                '가격 결정
                'Dim init_price As UInt32
                If LowLimitCaution Then
                    '하한가비상상황이면
                    OrderPrice = ParentOperation.LinkedSymbol.LowLimitPrice          '하한가로 판다
                Else
                    Dim local_call_price As CallPrices = ParentOperation.LinkedSymbol.LastCallPrices
                    '일반상황이면
                    If local_call_price.SelPrice1 = 0 Then
                        '상한가 상황 => BuyPrice1로 판다.
                        OrderPrice = local_call_price.BuyPrice1
                    Else
                        '일반상황이면
                        Dim best_sel_price As UInt32 = ParentOperation.GetBestSelPrice(OrderPrice, RestAmount, AmountUnderOrder)
                        OrderPrice = best_sel_price
                    End If
                End If
                SetAmountUnderOrderSEL(OrderPrice)
                '130523:재주문을 구현한다.
                Dim nReqID As Integer
                While 1
                    Try
                        'If _CSPAT00600 IsNot Nothing Then
                        'RemoveHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                        'RemoveHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                        '_CSPAT00600 = Nothing
                        'End If
                        '_CSPAT00600 = New XAQuery
                        _CSPAT00600_List.Add(et31_XAQuery_Wrapper.NewOrUsed())
                        AddHandler _CSPAT00600.ReceiveData, AddressOf _CSPAT00600_ReceiveData
                        AddHandler _CSPAT00600.ReceiveMessage, AddressOf _CSPAT00600_ReceiveMessage
                        '주문
                        _CSPAT00600.ResFileName = "Res\CSPAT00600.res"
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & ParentOperation.SymbolCode)                '종목번호
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, RestAmount.ToString)                     '매수수량
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, OrderPrice.ToString & ".00")                   '주문가
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "1")                                   '매수/매도 구분 (매도)
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                        _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '주문조건 구분 (없음)

                        nReqID = _CSPAT00600.Request(False)
                    Catch ex As Exception
                        ErrorLogging(ParentOperation.SymbolCode & " :" & "이런 에러!! " & ex.Message)
                        Continue While
                    End Try
                    Exit While
                End While
                If nReqID < 0 Then
                    'State = OperatorState.OS_ERROR          '에러상태로 전환
                    ErrorLogging(ParentOperation.SymbolCode & " :" & "주문 전송 실패(" & nReqID & ")")
                    RaiseEvent OrderErrored(Me)
                Else
                    State = OperatorState.OS_ORDER_REQUESTED          '주문 전송된 상태로 전환
                    'LowLimitCaution = False                         '200ms task에서 모니터되지 않기 위해 Flag를 False로 놓는다. 2024.02.18 : 하한가비상 상황에서 매도면 취소된 것도 이상하지만, 취소되었다 하더라도 다시 하한가로 재매도되어야 하며, False 로 두면 안 된다.
                    _TimerTick = 0                                      '타이머틱 초기화
                    '_CuttingNumber += 1
                    If SC0_CONFIRM_SUPPORT Then
                        _AccountManager.SubConfirmCheck_AddOrder(Me, "A" & ParentOperation.SymbolCode, "SONAT000")
                    End If
                    MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "주문 전송성공: " & OrderPrice.ToString & "원, " & RestAmount.ToString & "주")
                End If
            End If
#End If
        ElseIf State = OperatorState.OS_CANCELORDER_REQUESTED Then
            '취소주문 확인보다 실시간 취소주문 완료 이벤트가 먼저 들어옴
            '취소주문 확인 이벤트를 기다린다.
            'MessageLogging(_AccountManager.AccountCatString & " " & "A" & ParentOperation.SymbolCode & " : 취소주문 확인보다 실시간 취소주문 완료 이벤트 먼저 들어옴. 조금만 기다리자.")
            '취소 전에 일부 체결내역이 있는지 deal_price 및 rest_amount 체크해봐야함
            If rest_amount < ThisAmount Then
                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 확인보다 실시간 취소주문 완료 이벤트 먼저 들어옴. 조금만 기다리자a.")
                '취소되기전 일부 체결 사실이 있다.
                '2021.02.05: 09:48:42 경 이쪽으로 들어오서 DealPrice에 0이 세팅되고 그 전에 체결되었던 52개의 수량이 청산이 안 되는 문제 발생.
                '2021.02.05: 그러면 이 워닝 텍스트로 검색했을 때 나오는 수많은 로그들은 어떻게 그동안 정상적으로 청산이 되었을까 의문...
                '2021.02.06: 몇 개를 찾아봤는데, 매수 중 이렇게 된 경우는 main, sub의 경우, 장 끝나서까지도 남아 있어 다음날 강제 청산하게 된 경우가 있다. 그걸 모르고 걸린애인 줄 판단했으니.. (1월 19일)
                '2021.02.06: 매도 중 이렇게 된 경우는 따로 문제가 발생 안 하는 것 같다. (1월 25일). 다른 날짜도 좀 조사를 해보는 게 좋을듯 싶다.
                '2021.02.06: 몇 날짜 더 조사해보자.
                '2021.02.06: 솔루션은 여기(검색해보세요)=>2021.02.06_기다리자a

                DealPrice = deal_price              '체결가 저장
                RestAmount = rest_amount
            Else
                MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 확인보다 실시간 취소주문 완료 이벤트 먼저 들어옴. 조금만 기다리자b.")
                '최초수량 전량 취소되었다
                DealPrice = 0
                RestAmount = ThisAmount
            End If
            '2021.06.28: 10시33분경 연결끊김 발생하여 Cybos Launcher 재시작했는데, 그 전에 A900280종목 매수나갔던 게 취소도 안 되고 장종료까지 남아있는 문제 발생
            '2021.06.28: 이상한 건, 끝나고 디버깅 때 확인한 상태가 OS_CANCELORDER_CHECKED_FIRST_WAIT_CONFIRM 로 되어 있었는데, log에는 아래 if도 else도 타지 않은 것으로 나옴.
            '2021.06.28: 연결끊김 발생시, 이렇게 남아 있는 미체결 건들은 수동으로 취소해주는 것이 필요할 듯 하다. 아니다. 그러면 오류나려나..
            '2021.06.28: 솔루션은 추후 재현시 구현하도록 하자.
            State = OperatorState.OS_CANCELORDER_CHECKED_FIRST_WAIT_CONFIRM
            '130429:OS_CANCELORDER_CHECKED_FIRST_WAIT_CONFIRM 이거 사용하는데 잘 되어 있나 확인.
        Else
            'State = OperatorState.OS_ERROR
            ErrorLogging(ParentOperation.SymbolCode & " :" & "어? 취소주문 안 했는데?")
        End If
    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

    '주문 거부 혹은 취소주문 거부 확인이 들어옴 - [OperationKey] CALLED IN THE PROTECTED ZONE--------------------------------------------------------------------┐
    Public Sub ReceiveConfirmFailed()
        '130430:각 state별로(특히 order시 혹은 cancel시로 나누어) 거부에 대처하는 코드를 작성한다.
        If State = OperatorState.OS_ORDER_REQUESTED Then
            '매매주문 확인 들어오기 전에 거부 먼저 떨어짐
            '더이상 매매주문 확인 필요없고 cancel확인과 동일하게 처리
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "매매주문 확인도 되기전에 거부됨")
            State = OperatorState.OS_DONE        '종료 상태로 바꿈
            RaiseEvent OrderRejected(Me)           'Operation 객체에 알림
        ElseIf State = OperatorState.OS_ORDER_CONFIRMED_CHECKING_DEAL Then
            '매매주문 확인 후 거부 떨어짐
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "매매주문 확인후 거부됨")
            State = OperatorState.OS_DONE        '종료 상태로 바꿈
            RaiseEvent OrderRejected(Me)           'Operation 객체에 알림
        ElseIf State = OperatorState.OS_CANCELORDER_REQUESTED Then
            '취소주문이 확인 전에 거부됨
            '취소주문이 거부되는 경우는 그동안 체결되었기 때문인 것 빼고 다른 경우는 없는 것 같음 => 체결이벤트로 처리하고 여기서는 메시지표시외에 다른 일은 하지 않음
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 확인 전에 거부됨")
        ElseIf State = OperatorState.OS_CANCELORDER_CONFIRMED_CHECKING_DEAL Then
            '취소주문 확인 후 거부됨. 위의 경우와 마찬가지로 처리
            MessageLogging("A" & ParentOperation.SymbolCode & " " & _AccountManager.AccountCatString & " " & "취소주문 확인 후 실시간확인 전에 거부됨")
        Else
            '가능하지 않은 상태에서 취소주문 거부됨
            ErrorLogging(ParentOperation.SymbolCode & " :" & "가능하지 않은 상태에서 취소주문 거부됨")
        End If

    End Sub
    '[OperationKey] ----------------------------------------------------------------------------------------------------------------------------┘

#If 0 Then
    Public Sub Buy()
        SafeEnterTrace(OrderNumberListKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim input_price As UInt32

        '수량 계산
        If EnterExitState = EnterOrExit.EOE_Enter Then
            If _DeadlineCountdown = 0 Then
                '데드라인 도달 
                If RestOfTargetAmount = TargetAmount Then
                    '데드라인 도달할 때까지 하나도 못샀다=> DONE
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "데드라인 도달로 인해 하나도 못 사고 끝남")
                    EnterDealPrice = 0
                    ExitDealPrice = 0
                    OpStatus = OperationState.OS_DONE
                Else
                    '산 게 한 개 이상 있다 => 그만 사고 청산 대기
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "데드라인 도달로 인해 나머지는 못 사고 청산대기 들어감")
                    OpStatus = OperationState.OS_WAIT_UNTIL_EXIT_REQUEST        '청산 요청시까지 기다림
                End If
            Else
                '데드라인 전임 => 매수 시도
                Dim buy_amount As UInt32 = GetBuyAmount(_LastCallPrices, InitPrice, input_price)
                ThisOrderAmount = buy_amount

                If buy_amount > 0 Then
                    '위에서 계산한 수량, 가격으로 매수주문한다.
                    _CSPAT00600.ResFileName = "Res\CSPAT00600.res"
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & SymbolCode)                '종목번호
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, buy_amount.ToString)                     '매수수량
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, input_price.ToString & ".00")                   '주문가
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "2")                                   '매수/매도 구분 (매수)
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                    'If MainForm.rb_VirtualAcount.Checked Then
                    '_CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '가상계좌면 주문조건 구분 (없음)
                    'Else
                    _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "1")                         '실계좌면 주문조건 구분 (IOC)
                    'End If
                    Dim nReqID = _CSPAT00600.Request(False)
                    If nReqID < 0 Then
                        OpStatus = OperationState.OS_ERROR          '에러상태로 전환
                        ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "두번째이후주문 전송 실패(" & nReqID & ")")
                    Else
                        OpStatus = OperationState.OS_2ND_SO_ON_REQUESTED          '주문 전송된 상태로 전환
                        '_CuttingNumber += 1
                        MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "주문 전송성공: " & input_price.ToString & "원, " & buy_amount.ToString & "주")
                    End If
                Else
                    '수량이 0이다 => 잠깐 대기
                    OpStatus = OperationState.OS_STANDBY_UNTIL_NEXT_REQUEST
                    _StandbyCount = 0       '스탠바이 카운트 리셋
                    If _DeadlineCountdown = 0 Then
                        '데드라인 지난 시점에서는 스탠바이 타임을 늘린다.
                        _StandbyLimit = _STANDBY_TIME_FOR_ALERT
                    Else
                        _StandbyLimit = _RandomG.Next(STANDBY_MIN_COUNT, STANDBY_MAX_COUNT + 1)     '대기할 시간을 랜덤으로 생성
                    End If
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "계산수량 0. 잠깐 대기." & " deadline counter:" & _DeadlineCountdown)
                End If
                '                End If
            End If
        Else
            Dim sel_amount As UInt32 = GetSelAmount(_LastCallPrices, InitPrice, input_price)
            ThisOrderAmount = sel_amount
            '받쳐주는 호가 조사
            'Dim supporting_price As UInt32 = InitPrice * (1 + GAP_SUPPORT_CHECK_RATE)     '매도시엔 갭크기 조사하지 않는다

            If _DeadlineCountdown = 0 Then
                '데드라인 도달 => 수량 계산 필요없이 몽땅 팖
                sel_amount = RestOfTargetAmount
                ThisOrderAmount = sel_amount
                input_price = _LastCallPrices.BuyPrice5            '가격은 매수5호가까지
                'supporting_price = _LastCallPrices.SelPrice1        '받쳐주는 가격도 상관없이 다 팔아야 함
            Else
                '데드라인 전임 => 수량 계산 (위에서 계산 되었음)
            End If

            '갭크기 괜찮음
            If sel_amount > 0 Then
                '위에서 계산한 수량, 가격으로 매도주문한다.
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "AcntNo", 0, AccountString)        '계좌번호
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "InptPwd", 0, AccountPW)               '비밀번호
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "IsuNo", 0, "A" & SymbolCode)                '종목번호
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdQty", 0, sel_amount.ToString)                     '매도수량
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdPrc", 0, input_price.ToString & ".00")                   '주문가
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "BnsTpCode", 0, "1")                                   '매수/매도 구분 (매수)
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdprcPtnCode", 0, "00")                               '호가 유형 코드 (지정가)
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "MgntrnCode", 0, "000")                                '신용거래코드 (보통)
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "LoanDt", 0, "00000000")                                 '대출일
                'If MainForm.rb_VirtualAcount.Checked Then
                '_CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "0")                         '가상계좌면 주문조건 구분 (없음)
                'Else
                _CSPAT00600.SetFieldData("CSPAT00600InBlock1", "OrdCndiTpCode", 0, "1")                         '실계좌면 주문조건 구분 (IOC)
                'End If
                Dim nReqID = _CSPAT00600.Request(False)
                If nReqID < 0 Then
                    OpStatus = OperationState.OS_ERROR          '에러상태로 전환
                    ErrorLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "두번째이후주문 전송 실패(" & nReqID & ")")
                Else
                    OpStatus = OperationState.OS_2ND_SO_ON_REQUESTED          '주문 전송된 상태로 전환
                    '_CuttingNumber += 1
                    MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "주문 전송성공: " & input_price.ToString & "원, " & sel_amount.ToString & "주")
                End If
            Else
                '수량이 0이다 => 잠깐 대기
                OpStatus = OperationState.OS_STANDBY_UNTIL_NEXT_REQUEST
                _StandbyCount = 0       '스탠바이 카운트 리셋
                If _DeadlineCountdown = 0 Then
                    '데드라인 지난 시점에서는 스탠바이 타임을 늘린다.
                    _StandbyLimit = _STANDBY_TIME_FOR_ALERT
                Else
                    _StandbyLimit = _RandomG.Next(STANDBY_MIN_COUNT, STANDBY_MAX_COUNT + 1)     '대기할 시간을 랜덤으로 생성
                End If
                MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "계산수량 0. 잠깐 대기." & " deadline counter:" & _DeadlineCountdown)
            End If
            '            End If
        End If
        '        Else
        OpStatus = OperationState.OS_ERROR
        MessageLogging(_AccountManager.AccountCatString & " " & "A" & SymbolCode & " :" & "엉뚱한 가격확인 메시지 들어왔음")
        '       End If
        SafeLeaveTrace(OrderNumberListKey, 1)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
#End If

    'Enter critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┐
    Public Sub Increase00600Request()
        _CSPAT00600.RequestCount += 1
    End Sub
    'Leave critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┘

    'Enter critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┐
    Public Sub Increase00600Response()
        _CSPAT00600.ResponseCount += 1
    End Sub
    'Leave critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┘

    'Enter critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┐
    Public Function Is00600ResponsedAlready() As Boolean
        If _CSPAT00600.RequestCount = _CSPAT00600.ResponseCount Then
            Return True
        Else
            Return False
        End If
    End Function
    'Leave critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┘

    'Enter critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┐
    Public Sub Increase00800Request()
        _CSPAT00800.RequestCount += 1
    End Sub
    'Leave critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┘

    'Enter critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┐
    Public Sub Increase00800Response()
        _CSPAT00800.ResponseCount += 1
    End Sub
    'Leave critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┘

    'Enter critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┐
    Public Function Is00800ResponsedAlready() As Boolean
        If _CSPAT00800.RequestCount = _CSPAT00800.ResponseCount Then
            Return True
        Else
            Return False
        End If
    End Function
    'Leave critical zone, Thread간 공유문제 발생예방(ConfirmCheckerKey)------------------------------------┘

    Public Function Get00600OrderNumber() As String
        Dim return_str As String = _CSPAT00600.GetFieldData("CSPAT00600OutBlock2", "OrdNo", 0)
        Return return_str
    End Function

    Public Function Get00800OrderNumber() As String
        Dim return_str As String = _CSPAT00800.GetFieldData("CSPAT00800OutBlock2", "OrdNo", 0)
        Return return_str
    End Function

    Protected Overrides Sub Finalize()
        Terminate()
        MyBase.Finalize()
    End Sub

    Private Function IsValidOrderNumber(ByVal order_number As String) As Boolean
        If order_number = "" Then
            Return False
        End If

        Dim check1 = order_number.IndexOf("1")
        Dim check2 = order_number.IndexOf("2")
        Dim check3 = order_number.IndexOf("3")
        Dim check4 = order_number.IndexOf("4")
        Dim check5 = order_number.IndexOf("5")
        Dim check6 = order_number.IndexOf("6")
        Dim check7 = order_number.IndexOf("7")
        Dim check8 = order_number.IndexOf("8")
        Dim check9 = order_number.IndexOf("9")

        If check1 = -1 AndAlso check2 = -1 AndAlso check3 = -1 AndAlso check4 = -1 AndAlso check5 = -1 AndAlso check6 = -1 AndAlso check7 = -1 AndAlso check8 = -1 AndAlso check9 = -1 Then
            Return False
        Else
            Return True
        End If

    End Function
End Class