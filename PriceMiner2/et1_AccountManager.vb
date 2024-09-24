Imports XA_DATASETLib
'131009 :
'Account manager와 operation 객체에 보완이 필요하다. 매수 성공확률을 높이고 거래 볼륨을 더 확보하기 위해서이다.
'Decision maker도 조금 수정되어야 한다. 가장 중요한 하락률 변수가 threshold에 도달했을 때 거래를 시도하도록 지금 되어있지만,
'앞으로는 이것보다 미리 매수 주문이 되어 있는 방식으로 하려고 한다. 즉, 거래 기준이 되는 threshold에 도달하기 전 pre-threshold
'개념을 두어서 이 pre-threshold에 도달하는 경우 별도의 Boolean Flag가 set되고 이와 동시에 매수 주문을 threshold level에
'채워넣을 수 있는 조건이 갖춰진다. 이 flag가 set된다고 해서 다 매수주문이 들어가는 것은 아니다. pre-threshold를 통과한
'종목들은 Account manager가 관리 하는데 각 종목들마다 매수가능성이 얼마나 높은지를 나타내는 점수를 매기고
'점수순으로 정렬하여 매수가능성이 높은 순서대로 매수주문을 넣는다. 자금이 부족할 경우 매수주문을 못낼 수도 있다.
'Account manager는 관리하고 있는 종목들의 점수를 주기적으로 체크하여 매수가능성이 낮은 것들의 주문을 빼서 그 자금을
'매수가능성이 높은 종목을 매수하는데 사용하도록 관리한다.
'pre-threshould 도달여부 flag는 decision maker가 갖고 있도록 하고, 이 flag가 set 되는 동시에 operation 객체가 생성되도록 한다.
'매수가능성 점수는 operation 객체가 관리하고, Account manager가 갖는 list는 이 operation 객체들의 list가 될 것이다.
'decision maker의 flag가 reset되면(주가상승이 시작되는등) operation 객체가 파괴되고 매수에 사용된 자금은 회수된다.
'각 class에 새로 구현해야 될 것들을 정리하면 다음과 같다.
'Decision Maker => pre-threshold 도달 여부 나타내는 Boolean Flag 생성. operation 생성/파괴 시점 변경.
'Operation => 하락률과 매수1호가가 얼마나 낮은지의 정도록부터 계산되는 매수성공확률점수.
'Account Manager => pre-threshold 통과한 종목들의 operation 객체 list
'                => 종목 리스트를 주기적으로 체크하여 매수주문을 관리하는 주기태스크
' * 추가매수 (한 번 매수에 성공했어도 같은 가격, 또는 더 좋은 가격으로 매수하는 것)에 대한 전략을 생각해봐야겠다.
'    => 거래볼륨을 더 확보하기 위함이다.
'181214:
'AccountManager는 좀 더 기민한 매수를 위해 Decision이 내려지자 마자 매수하는 형식으로 바뀐다.
'기존에는 최대 5초까지 기다렸다가 매수하는 형태인데, 이게 매수성공률을 많이 떨어뜨리는 것으로 분석된다.
'그리고 score를 매겨서 sorting하는 것은 이제 구시대의 산물이라고 봐야 한다. score는 의미가 없고 지금 나온 가장 신선한 녀석이 제일 priority가 높다.
'컴파일 스위치를 써서 기존 코드는 backward compatibility를 유지하려고 했는데, 수정해야 할 부분이 너무 복잡해져서 그냥 전제를 싹 다 바꾸기로 했다.


Public Class et1_AccountManager
    Public Structure StoredStockType
        Dim Code As String
        Dim Quantity As Int32
        Dim AvePrice As Double
        Dim MA_Base As String
    End Structure

    Public Shared MINUTES_FOR_DISTRIBUTION As Integer = 24
    Private WithEvents _CSPAQ12200 As New et31_XAQuery_Wrapper()
    Private WithEvents _CSPAQ22200 As New et31_XAQuery_Wrapper()
    Private WithEvents _CSPAQ12300 As New et31_XAQuery_Wrapper()
    Public TestOutBlock(57) As String
    Public StoredStockList As New List(Of StoredStockType)
    Public StockListKey As TracingKey
    Public Ordable100 As UInt64
    'Public Ordable40 As UInt64
    'Public Ordable30 As UInt64
    Public OrdableKey As TracingKey
    Public TotalProperty As UInt64
    'Public SC0_Data As New List(Of String)
    Public SC1_Data As New List(Of String)
    Public SC3_Data As New List(Of String)
    Public SC4_Data As New List(Of String)
    Public OrderNumberListKey As TracingKey 'Integer
    Private orderOperationList As New List(Of et21_Operator)
    Private DealNumberList As New List(Of ULong)
    Private DealPriceList As New List(Of Double)
    Private RestAmountList As New List(Of UInt32)
    Private NothingDebugWhere As New List(Of Integer)
    Private NothingDebugTime As New List(Of DateTime)
    Public Prethresholders As New List(Of c050_DecisionMaker)       '이건 5초 마다 task 불릴 때 임시로 사용하는 리스트
    Public DecisionHolders As New List(Of c050_DecisionMaker)       '이건 항상 관리하는 것
    Public XAReal_SC0_ReceiveRealData_Thread As Threading.Thread
    Public XAReal_SC1_ReceiveRealData_Thread As Threading.Thread
    Public XAReal_SC3_ReceiveRealData_Thread As Threading.Thread
    Public XAReal_SC4_ReceiveRealData_Thread As Threading.Thread
    Public AccountCat As Integer
    Public BuyPower As Double = 0.007                         '180108: 매수 때 무리를 해서라도 사는 정도
    Public SilentInvolvingAmountRate As Double
    Private _AccountString As String
    Private _PasswordString As String
    Public ImmediateBuy As Boolean
    Public ScoreByFailureCount As Boolean
    Public SortByScore As Boolean
    Public ActiveOperationList As New List(Of et2_Operation)
    Public SubConfirmCheckerOperator As New List(Of et21_Operator)
    Public SubConfirmCheckerCode As New List(Of String)
    Public SubConfirmCheckerTRcode As New List(Of String)
    'Public SubConfirmCheckerAmount As New List(Of Integer)
    'Public SubConfirmCheckerPrice As New List(Of Integer)
    Public ConfirmCheckerKey As TracingKey
    Public LastTimeAccountTaskExecuted As DateTime

#If ALLOWED_SALE_COUNT Then
    Public AllowedSaleCountFromLastSale As Double = 0 '마지막 매매했던 시간 이후로 현시간까지 허용된 매매 카운트
#End If

    Public Sub SubConfirmCheck_AddOrder(ByVal et_operator As et21_Operator, ByVal symbol_code As String, ByVal expected_trcode As String)
        SafeEnterTrace(ConfirmCheckerKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        SubConfirmCheckerOperator.Add(et_operator)
        SubConfirmCheckerCode.Add(symbol_code)
        SubConfirmCheckerTRcode.Add(expected_trcode)

        If expected_trcode = "SONAT000" Then
            et_operator.Increase00600Request()
        Else 'trcode = "SONAT002"
            et_operator.Increase00800Request()
        End If
        SafeLeaveTrace(ConfirmCheckerKey, 10)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Function SubConfirmCheck_RemoveOrderAfterConfirmed(ByVal et_operator As et21_Operator, ByVal expected_trcode As String) As Boolean
        SafeEnterTrace(ConfirmCheckerKey, 11)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim found_index As Integer = -1
        For index As Integer = 0 To SubConfirmCheckerOperator.Count - 1
            If SubConfirmCheckerOperator(index) Is et_operator AndAlso SubConfirmCheckerTRcode(index) = expected_trcode Then
                found_index = index
                Exit For
            End If
        Next

        If found_index >= 0 Then
            If expected_trcode = "SONAT000" Then
                et_operator.Increase00600Response()
            Else 'trcode = "SONAT002"
                et_operator.Increase00800Response()
            End If

            SubConfirmCheckerOperator.RemoveAt(found_index)
            SubConfirmCheckerCode.RemoveAt(found_index)
            SubConfirmCheckerTRcode.RemoveAt(found_index)
            MessageLogging(et_operator.ParentOperation.LinkedSymbol.Code & " " & AccountCatString & " " & "SubConfirmList갯수: " & SubConfirmCheckerOperator.Count)

            SafeLeaveTrace(ConfirmCheckerKey, 19)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return True
        Else
            SafeLeaveTrace(ConfirmCheckerKey, 20)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return False
        End If
    End Function

    Public Sub SubConfirmChecker_NotifiedBySC0(ByVal symbol_code As String, ByVal trcode As String, ByVal order_number As String)
        SafeEnterTrace(ConfirmCheckerKey, 21)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim found_index = -1
        Dim extracted_order_number As String
        For index As Integer = 0 To SubConfirmCheckerOperator.Count - 1
            If SubConfirmCheckerCode(index) = symbol_code AndAlso SubConfirmCheckerTRcode(index) = trcode Then
                If SubConfirmCheckerTRcode(index) = "SONAT000" Then
                    extracted_order_number = SubConfirmCheckerOperator(index).Get00600OrderNumber()
                Else '"SONAT002"
                    extracted_order_number = SubConfirmCheckerOperator(index).Get00800OrderNumber()
                End If
                If extracted_order_number = order_number Then
                    'found it!
                    found_index = index
                    Exit For
                End If
            End If
        Next
        If found_index >= 0 Then
            'MessageLogging(symbol_code & " :오예 놓칠 뻔 한 거 잡았어~") <= 놓칠 뻔 한 게 아니다 00600 receive event보다 늦게 들어올 수도 있고 먼저 들어올 수도 있다.
            '20200325: 하지만 실제로 잡은 건지 확인해야 한다. 간혹, 같은 종목에서 같은 trcode라 하더라도, 두 개 이상의 매매가 동시에
            '  이루어 지고 있는 경우도 있다. 3월 24일 한프의 경우가 그렇다. 두 건의 매매가 동시에 진행되고 있었는데, 한 건은
            '  00600 receive event로 confirm되어 리스트에서 삭제되었는데, 그 건의 SC0가 들어와 다른 건의 confirm으로 오인되어
            '  결국 두 번째 매수건이 주문거부로 오해되는 건이 있었다. 이것을 방지하기 위해, 결국 operator 안의 Response counter를
            '  활용하기로 한다.
            If trcode = "SONAT000" Then
                If SubConfirmCheckerOperator(found_index).Is00600ResponsedAlready Then
                    '20200325: 이미 00600 receive event를 통해 response 되었으므로 지금 찾은 것은 찾고 있는 게 아니라는 뜻 => 따라서 그냥 나간다.
                    SafeLeaveTrace(ConfirmCheckerKey, 29)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    MessageLogging(SubConfirmCheckerOperator(found_index).ParentOperation.LinkedSymbol.Code & " " & AccountCatString & " " & "찾았는데 찾은게 아니다? 이게 뭐야 이게 삭제가 제대로 안 됐나?")
                    Exit Sub
                End If
                SubConfirmCheckerOperator(found_index).Increase00600Response()
                'SubConfirmCheckerOperator(found_index)._CSPAT00600_ReceiveData_PostThread() <= 이렇게 하려고 했는데 SC0 계속 잡고 있으면 안 되니까, 00600 receive event 에서처럼 post thread를 돌린다.
                Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf SubConfirmCheckerOperator(found_index)._CSPAT00600_ReceiveData_PostThread)
                Dim parameters() As Object = {order_number}
                simulate_thread.Start(parameters)
            Else 'trcode = "SONAT002"
                If SubConfirmCheckerOperator(found_index).Is00800ResponsedAlready Then
                    '20200325: 이미 00800 receive event를 통해 response 되었으므로 지금 찾은 것은 찾고 있는 게 아니라는 뜻 => 따라서 그냥 나간다.
                    SafeLeaveTrace(ConfirmCheckerKey, 28)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    MessageLogging(SubConfirmCheckerOperator(found_index).ParentOperation.LinkedSymbol.Code & " " & AccountCatString & " " & "찾았는데 찾은게 아니다? 이게 뭐야 이게 삭제가 제대로 안 됐나?")
                    Exit Sub
                End If
                SubConfirmCheckerOperator(found_index).Increase00800Response()
                'SubConfirmCheckerOperator(found_index)._CSPAT00800_ReceiveData_PostThread() <= 이렇게 하려고 했는데 SC0 계속 잡고 있으면 안 되니까, 00600 receive event 에서처럼 post thread를 돌린다.
                Dim simulate_thread As Threading.Thread = New Threading.Thread(AddressOf SubConfirmCheckerOperator(found_index)._CSPAT00800_ReceiveData_PostThread)
                Dim parameters() As Object = {order_number}
                simulate_thread.Start(parameters)
            End If
            SubConfirmCheckerOperator.RemoveAt(found_index)
            SubConfirmCheckerCode.RemoveAt(found_index)
            SubConfirmCheckerTRcode.RemoveAt(found_index)
            MessageLogging(SubConfirmCheckerOperator(found_index).ParentOperation.LinkedSymbol.Code & " " & AccountCatString & " " & "SubConfirmList갯수: " & SubConfirmCheckerOperator.Count)
        End If
        SafeLeaveTrace(ConfirmCheckerKey, 30)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub New(ByVal account_cat As Integer)
        AccountCat = account_cat
        '2024.06.09 : SilentInvolvingAmountRate 은 원래 StockSearcher 에서는 decision 전략에 따라 변하게 되어 있었다. PriceMiner 에서는 같은 TEST 계좌라 하더라도
        '2024.06.09 : PCRenew 냐 DoubleFall 이냐에 따라 달라지기도 하기 때문에 PriceMiner 에서는 SilentInvolvingAmountRate이 사용되는 곳에서 decision 전략에 따른 factor(ALLOWED_ENTERING_COUNT 같은 거) 를 곱하는 방식으로 사용한다.
        If AccountCat = 0 Then
            'Main Account
            BuyPower = MAIN_BUY_POWER '0.007
            SilentInvolvingAmountRate = MAIN_SILENT_INVOLVING_AMOUNT_RATE
            _AccountString = MainAccountString
            _PasswordString = MainAccountPW
        ElseIf AccountCat = 1 Then
            'Sub Account
            BuyPower = SUB_BUY_POWER '0.007
            SilentInvolvingAmountRate = SUB_SILENT_INVOLVING_AMOUNT_RATE
            _AccountString = SubAccountString
            _PasswordString = SubAccountPW
        Else 'if AccountCat = 2 Then
            BuyPower = TEST_BUY_POWER '0.007
            SilentInvolvingAmountRate = TEST_SILENT_INVOLVING_AMOUNT_RATE
            _AccountString = TestAccountString
            _PasswordString = TestAccountPW
        End If
    End Sub

    Public ReadOnly Property IsMA As Boolean
        Get
            If AccountCat = 1 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsFlex As Boolean
        Get
            If AccountCat = 0 Or AccountCat = 2 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property AccountCatString As String
        Get
            If AccountCat = 0 Then
                Return "Main"
            ElseIf AccountCat = 1 Then
                Return "Sub"
            Else 'if AccountCat = 2 Then
                Return "Test"
            End If
        End Get
    End Property

    'OneKey Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
    Public Sub DecisionRegister(ByVal decision_maker As c050_DecisionMaker)
#If 0 Then
        '2022.09.06: 이 호가 available 조건과 VI 조건은 이제 여기서 보지 않고 일단 decision등록을 해 놓고 나중에 매수하기 전에 조건을 봐서 거르는 걸로 바꾼다.
        '그 전에 일단 호가가 available한지 확인한다.
        If Not decision_maker.LinkedSymbol.IsCallPriceAvailable Then
            '호가가 available하지 않으면 빠져나간다. 다음에 호가가 available해지면 기회가 다시 있을 것이다.
            Exit Sub
        End If
        '191127_TODO: 날짜넘긴 걸린애들은 현재 호가요청하는 곳이 없기 때문에 Test 계좌에서 따로 호가요청을 하지 않는다면 장초기에 진입하지 못한다. 이점 수정 요헌다.
        '191127_TODO: 그리고 어떻게 Test 계좌의 도움으로 진입한다고 하더라도, test 계좌에서 호가 Real 을 꺼버리면, 호가를 받지 못하는 상황이기 때문에 청산가를 제대로 세팅할 수 없어 청산이 안 되는 문제도 있다.
        'VI 상태면 등록 안 하고 빠져나온다. VI가 풀릴 때 기회가 있을 것이다.
        If decision_maker.LinkedSymbol.VI Then
            'MessageLogging("VI면 여기 들어오면 안 되게 되어 있는데...")
            Exit Sub
        End If
#End If
        '일단 holder에 등록한다.
        SafeEnterTrace(StockListKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐

        '등록 전에 이미 이전 등록된 중복된 종목이 있는지 확인한다.
        For index As Integer = 0 To DecisionHolders.Count - 1
            If DecisionHolders(index).LinkedSymbol.Code = decision_maker.LinkedSymbol.Code AndAlso DecisionHolders(index).EnterTime = decision_maker.EnterTime Then
                '중복이다=> 등록하지 않고, 매수하지도 않고 빠져나온다.
                SafeLeaveTrace(StockListKey, 32)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Exit Sub
            End If
        Next
        '중복 안 된 게 확인되어 등록한다.
        If IsMA Then
            'MA 인 건 SAFE PRICE 이상일 때만 등록한다. 너무 싼 건 안전하지 않다고 판단한다. 웃기지만 상폐를 몇 번 당하다보면 이렇게라도 하게 된다.
            If decision_maker.EnterPrice > c05G_MovingAverageDifference.SAFE_PRICE Then
                DecisionHolders.Add(decision_maker)
            End If
        Else
            'MA 아닌 건 그냥 등록하고
            DecisionHolders.Add(decision_maker)
        End If

        SafeLeaveTrace(StockListKey, 31)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Dim call_prices As CallPrices = decision_maker.LinkedSymbol.LastCallPrices
        '첫 CallPrice에 의한 score 궁금하니까 저장해둔다.
        '2021.04.29: test 계좌 multiple enter 시 들어오게 개시되어, 이제 첫 CallPrice 의미는 없어진 듯 하다. 저장이 안 된 경우에만 저장되도록 바뀌었다.
        If decision_maker.ScoreCFirst_CallPrice = 0 Then
            decision_maker.ScoreCFirst_CallPrice = CType(GetUnitPrice(decision_maker.LastEnterPrice, decision_maker.LinkedSymbol.MarketKind), Single) / (CType(call_prices.SelPrice1, Integer) - call_prices.BuyPrice1)
        End If
#If 1 Then      '190507
        If IsMA AndAlso Not ImmediateBuy Then
            '이게 처음 등록한 거라면 매수 안 하고 그냥 나간다. 이것은 무겹침에서 진입하는 것보다 1장 겹침째에서 진입하는 것이 수익이 좋다는 통계를 근거로 한 작전이다. 여러 겹침도 소화할 만큼 자본이 충분하면 없애는 것이 좋을 듯 싶다.
            '190902:그냥 메인이면 빠져나가도록 한다. 모아서 score 매겨서 한꺼번에 판단하는 걸로 한다.
            'If DecisionHolders.Count <= 1 Then
            Exit Sub
            'End If
        End If
        If Not IsMA Then
            '200223: test 계좌 (flexible patter 방법)는 바로 안 사고 5초 마다 모아서 사기로 한다.
            Exit Sub
        End If
#End If

        '아래는 매수하는 코드들이다.
        '2021.04.29: 현재 어떤 계좌에서도 아래로 진입하지 않게 되어 있어 수정하는 게 지금은 무의미하지만, MoneyDistribute 에서 그동안 업데이트되었던 것이 많이 반영이
        '2021.04.29: 안 되어 있어, 나중을 위해서 많이 달라져 있지 않도록 미리 수정을 해놨다.
        '2024.01.13 : 이제부터는 관리 안 하기로 한다. 수시로 매수하는 것도 이제 MoneyDistribute 에서 한다.
#If 0 Then
        Dim possible_order_volume As Double
        Dim fall_volume_standard As Double
        Dim adaptive_silent_rate As Double = SilentInvolvingAmountRate
        Dim buy_standard_price As UInt32
        'Dim ordered_volume As Double = 0
        Dim current_phase As c050_DecisionMaker.SearchPhase
        Dim previously_ordered_volume As Double
        Dim money_for_this_one As Double

        '191019_TODO_DONE: 굳이 왜 합산하는가 예탁자산총액을 등분해서 활용하라.
        Dim pizza_one_piece, one_piece_for_this_one As Double
        If IsMA Then
            If AccountCat = 0 Then
                'Main
                pizza_one_piece = TotalProperty / MAIN_NUMBER_OF_PIECES
            Else 'If AccountCat = 1 Then
                'Sub
                pizza_one_piece = TotalProperty / SUB_NUMBER_OF_PIECES
            End If
        Else
            If AccountCat = 0 Then
                pizza_one_piece = TotalProperty / MAIN_NUMBER_OF_PIECES
            Else
                pizza_one_piece = TotalProperty / TEST_NUMBER_OF_PIECES
            End If
        End If
        SafeEnterTrace(OrdableKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim rest_of_ordable100 As Double = Ordable100
        'Dim rest_of_ordable40 As Double = Ordable40
        'Dim rest_of_ordable30 As Double = Ordable30
        SafeLeaveTrace(OrdableKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Dim over_today As Double = 0
        If IsMA Then
            Dim decision_ma_maker As c05G_MovingAverageDifference = decision_maker
            '현재 시간기준 매수시 장종료 직전까지 청산가능한지 확인
            Dim remained_minutes As Integer = 60 * MarketEndHour + MarketEndMinute - (60 * Now.Hour + Now.Minute)
            If remained_minutes > decision_ma_maker.DEFAULT_HAVING_MINUTE - decision_ma_maker.MinutesPassed + 20 Then
                '20 분 마진 줘서 장종료직전까지 청산가능 => 미수거래하자
                decision_maker.ScoreD_DepositBonus = 100 / decision_maker.LinkedSymbol.EvidanRate
            Else
                '장종료직전까지 청산불가능 => 미수거래 안 된
                decision_maker.ScoreD_DepositBonus = 10
            End If

            SafeEnterTrace(StockListKey, 40)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            For index As Integer = 0 To DecisionHolders.Count - 1
                If DecisionHolders(index).ScoreD_DepositBonus = 10 AndAlso DecisionHolders(index).StockOperator IsNot Nothing Then
                    If DecisionHolders(index).LinkedSymbol.RecordList.Count > 0 AndAlso DecisionHolders(index).LinkedSymbol.RecordList.Last.Price > 0 Then
                        over_today = over_today + DecisionHolders(index).StockOperator.ThisAmount * DecisionHolders(index).LinkedSymbol.RecordList.Last.Price '2022.09.22: over_today 계산 기준가를 구입가에서 현재가로 바꿨다.
                    Else
                        over_today = over_today + DecisionHolders(index).StockOperator.ThisAmount * DecisionHolders(index).StockOperator.InitPrice
                    End If
                End If
            Next
            SafeLeaveTrace(StockListKey, 41)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
            one_piece_for_this_one = pizza_one_piece * decision_maker.NumberOfEntering
            If over_today > TotalProperty - one_piece_for_this_one Then
                '물리기 전에 그만 두자.
                Exit Sub
            End If

            If decision_maker.Score <= -100 Then
                '2020.0818: 시간 다 지나서 하는 의미없는 투자는 그만두자(for sub). 9월 5일부로 main으로도 확대되었다.
                Exit Sub
            End If
        End If

        '이 종목의 투자금액 설정
        If IsMA Then
            Select Case decision_maker.LinkedSymbol.EvidanRate
                Case 30
                    If rest_of_ordable30 > 1 * one_piece_for_this_one Then
                        money_for_this_one = one_piece_for_this_one
                    Else
                        money_for_this_one = rest_of_ordable30
                    End If
                Case 40
                    If rest_of_ordable40 > 1 * one_piece_for_this_one Then
                        money_for_this_one = one_piece_for_this_one
                    Else
                        money_for_this_one = rest_of_ordable40
                    End If
                Case 50
                    If rest_of_ordable100 > 1 * one_piece_for_this_one Then
                        money_for_this_one = one_piece_for_this_one
                    Else
                        money_for_this_one = rest_of_ordable100
                    End If
                Case Else '100
                    If rest_of_ordable100 > 1 * one_piece_for_this_one Then
                        money_for_this_one = one_piece_for_this_one
                    Else
                        money_for_this_one = rest_of_ordable100
                    End If
            End Select
        Else
            '2023.11.09: 이제부터 MA 전략은 위와 같이 전에 하던 그대로 피자조각을 쪼개 매매하고, MA 아닌 전략 (DoubleFall 과 PCRenew 전략)은 레버리지를 최대화하기 위해 피자 한판으로 간다.
            '2023.11.09: 즉, 앞에 계산했던 pizza_one_piece 및 one_peice_for_this_one 이 필요없다.
            Select Case decision_maker.LinkedSymbol.EvidanRate
                Case 30
                    money_for_this_one = rest_of_ordable30
                Case 40
                    money_for_this_one = rest_of_ordable40
                Case 50
                    money_for_this_one = rest_of_ordable100
                Case Else '100
                    money_for_this_one = rest_of_ordable100
            End Select
        End If

        '해당 종목에 이미 주문한 금액 계산
        If decision_maker.StockOperator Is Nothing Then
            previously_ordered_volume = 0
        Else
            previously_ordered_volume = decision_maker.StockOperator.ThisAmount * decision_maker.StockOperator.InitPrice
        End If
        'Fall Volume Standard 구하기
        If IsMA Then
            fall_volume_standard = decision_maker.FallVolume * SilentInvolvingAmountRate
        Else
            fall_volume_standard = decision_maker.FallVolume / decision_maker.PatternLength * SilentInvolvingAmountRate  '패턴일 경우의 계산법이다. 패턴 아닌 경우는 또 다르다.
        End If

        'fall volume standard와 이 종목 투자금액 중 작은 것이 possible order volume이 된다
        possible_order_volume = Math.Min(fall_volume_standard, money_for_this_one)

        'possible order volume에서 기존 주문 들어간 것을 제한다.
        possible_order_volume = possible_order_volume - previously_ordered_volume

        If possible_order_volume < 0 Then
            '이런 경우가 있을지 모르겠는데.. 
            MessageLogging(decision_maker.LinkedSymbol.Code & " :" & "possible_order_volume이 0이 나왔다.")
            possible_order_volume = 0
        End If

        Dim final_order_amount As UInt32

        final_order_amount = Math.Floor(possible_order_volume / (decision_maker.TargetBuyPrice * (1 + BuyPower)))

        '2023.10.24 : 코스피 단주거래 허용된지 10년이 다 되어가는데 아직 업데이트가 안 됐냐 너무하다 정말
        'If decision_maker.LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And (decision_maker.TargetBuyPrice * (1 + BuyPower)) < 50000 Then
        'final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
        'End If

        Dim local_call_price As CallPrices = decision_maker.LinkedSymbol.LastCallPrices
        If local_call_price.BuyPrice1 = 0 Then
            '하한가 상황 => 사지 않는다.
            MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)하한가 상황 => 사지 않는다.")
            Exit Sub
        Else
            '180113: 매수시 Operation 에 넘겨주는 것은 기준매수가를 넘겨주자. Operation 또는 Operator에서 그 기준매수가를 기준에서 상황에 맞게 매수가를 변경시킬 수 있게..
            buy_standard_price = NextCallPrice(decision_maker.TargetBuyPrice * (1 + BuyPower), 0) ', decision_maker.LinkedSymbol.MarketKind)

            If buy_standard_price <= decision_maker.LinkedSymbol.LowLimitPrice Then
                '주문가가 하한가 아래면 아예 주문하지 않는다.
                MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)기준매수가(" & buy_standard_price.ToString & ")가 하한가(" & decision_maker.LinkedSymbol.LowLimitPrice.ToString & ")아래면 아예 주문하지 않는다.")
                Exit Sub
            End If


            '주문해보자. prethreshold list 만들고나서 current phase가 바뀌었을 수 있으니 주문전 다시 확인한다.
            'SafeEnterTrace(decision_maker.LinkedSymbol.OneKey, 50)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            current_phase = decision_maker.CurrentPhase
            'SafeLeaveTrace(decision_maker.LinkedSymbol.OneKey, 51)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)주문조건 " & current_phase.ToString & " BuyStandardPrice " & buy_standard_price.ToString & " FallVolumeStandard " & fall_volume_standard.ToString & " PossibleOrderVolume " & possible_order_volume.ToString & " FinalOrderAmount " & final_order_amount.ToString)
            If final_order_amount > 0 AndAlso (current_phase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED Or
                current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME) Then       '140703에 청산 기다리는 거 다시 넣기로 함. 매수주문 비율 높여야됨
                '140224 : StockOperator new 함수에서 t1101 요청하지 않고 바로 주문하도록 만들자.
                '140224 : AccountManager는 Prethresholders를 통해 Operation 객체에 접근할 수 있으므로 굳이 Operator List를 둘 필요는 없어보인다.
                If decision_maker.StockOperator Is Nothing Then
                    If Not decision_maker.NoMoreOperation Then
                        decision_maker.ScoreSave = decision_maker.Score     ' 살 때 score 저장한다.
                        MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)생성주문 / " & buy_standard_price.ToString & " / " & final_order_amount.ToString)
                        decision_maker.StockOperator = New et2_Operation(decision_maker, buy_standard_price, final_order_amount, Me, False)       'stock operator 생성과 동시에 진입 주문
                        decision_maker.StockOperator.SetAccountManager(Me)
                        If IsMA Then
                            decision_maker.SilentLevelVolume = decision_maker.FallVolume * SilentInvolvingAmountRate
                        Else
                            decision_maker.SilentLevelVolume = decision_maker.FallVolume / decision_maker.PatternLength * SilentInvolvingAmountRate
                        End If
                        rest_of_ordable100 -= final_order_amount * buy_standard_price * Math.Max(decision_maker.LinkedSymbol.EvidanRate / 100, 1)
                        'rest_of_ordable40 -= final_order_amount * buy_standard_price * Math.Max(decision_maker.LinkedSymbol.EvidanRate / 40, 1)
                        'rest_of_ordable30 -= final_order_amount * buy_standard_price * Math.Max(decision_maker.LinkedSymbol.EvidanRate / 30, 1)
                    Else
                        '이미 하한가 비상 발령한 위험한 종목은 진입하지 않는다.
                    End If
                ElseIf decision_maker.StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                    'stock operator가 있지만 상태가 DONE인 경우에는 추가주문을 하지 않는다. 왜냐면 하한가비상 발령되어서 다 팔고 DONE 되었기 때문이다.
                    Exit Sub
                Else
                    MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)인줄 알았는데 처음이 아니었던 건가요?")
                End If
            Else
                '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                'EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
            End If
        End If
#If 0 Then
        SafeEnterTrace(OrdableKey, 50)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Ordable100 = Math.Max(rest_of_ordable100, 0)
        Ordable40 = Math.Max(rest_of_ordable40, 0)
        Ordable30 = Math.Max(rest_of_ordable30, 0)
        SafeLeaveTrace(OrdableKey, 51)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
#End If
#If 0 Then
        Dim possible_order_volume As Double
        Dim fall_volume_standard As Double
        Dim adaptive_silent_rate As Double = SilentInvolvingAmountRate
        Dim buy_standard_price As UInt32
        Dim ordered_volume As Double = 0
        Dim current_phase As c050_DecisionMaker.SearchPhase

        If IsMain Then
#If MOVING_AVERAGE_DIFFERENCE Then
            'Moving average 전략에서는 증거금률 100 미만인 거래 안 된다.
            possible_order_volume = Ordable100
#Else
            possible_order_volume = Ordable100 * 100 / decision_maker.LinkedSymbol.EvidanRate
#End If
        Else
            possible_order_volume = Ordable100 * 100 / decision_maker.LinkedSymbol.EvidanRate
        End If

        If possible_order_volume < 0 Then
            '이런 경우가 있을지 모르겠는데..
            ErrorLogging(decision_maker.LinkedSymbol.Code & " :" & "(처음)possible_order_volume이 0이 나왔다 계산이 이상하다")
            possible_order_volume = 0
        End If

        '첫주문이면
        If IsMain Then
            fall_volume_standard = decision_maker.FallVolume * SilentInvolvingAmountRate
        Else
            fall_volume_standard = decision_maker.FallVolume / decision_maker.PatternLength * adaptive_silent_rate  '패턴일 경우의 계산법이다. 패턴 아닌 경우는 또 다르다.
        End If

        Dim final_order_amount As UInt32
        'lack_of_money = fall_volume_standard - possible_order_volume
        If possible_order_volume > fall_volume_standard Then
            '이 한 종목 이상으로 살 만큼의 돈이 있다.
            final_order_amount = Math.Floor(fall_volume_standard / (decision_maker.TargetBuyPrice * (1 + BuyPower)))
        Else
            '이 한 종목도 못 살만큼 돈이 없다.
            final_order_amount = Math.Floor(possible_order_volume / (decision_maker.TargetBuyPrice * (1 + BuyPower)))

            '이 함수 마지막에 지금 주문 들어가 있는 놈들 중 순위 낮은 놈들을 취소 시킨다.
        End If

        If decision_maker.LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And (decision_maker.TargetBuyPrice * (1 + BuyPower)) < 50000 Then
            final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
        End If

        Dim local_call_price As CallPrices = decision_maker.LinkedSymbol.LastCallPrices
        If local_call_price.BuyPrice1 = 0 Then
            '하한가 상황 => 사지 않는다.
            MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)하한가 상황 => 사지 않는다.")
        Else
            '180113: 매수시 Operation 에 넘겨주는 것은 기준매수가를 넘겨주자. Operation 또는 Operator에서 그 기준매수가를 기준에서 상황에 맞게 매수가를 변경시킬 수 있게..
            buy_standard_price = NextCallPrice(decision_maker.TargetBuyPrice * (1 + BuyPower), 0, decision_maker.LinkedSymbol.MarketKind)

            If buy_standard_price <= decision_maker.LinkedSymbol.LowLimitPrice Then
                '주문가가 하한가 아래면 아예 주문하지 않는다.
                MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)기준매수가(" & buy_standard_price.ToString & ")가 하한가(" & decision_maker.LinkedSymbol.LowLimitPrice.ToString & ")아래면 아예 주문하지 않는다.")
            End If

            If IsMain Then
#If MOVING_AVERAGE_DIFFERENCE Then
                'Moving average 전략에서는 증거금률 100 미만인 거래 안 된다.
                ordered_volume += final_order_amount * buy_standard_price
#Else
                ordered_volume += final_order_amount * buy_standard_price * decision_maker.LinkedSymbol.EvidanRate / 100
#End If
            Else
                ordered_volume += final_order_amount * buy_standard_price * decision_maker.LinkedSymbol.EvidanRate / 100
            End If

            '주문해보자. prethreshold list 만들고나서 current phase가 바뀌었을 수 있으니 주문전 다시 확인한다.
            'SafeEnterTrace(decision_maker.LinkedSymbol.OneKey, 50)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            current_phase = decision_maker.CurrentPhase
            'SafeLeaveTrace(decision_maker.LinkedSymbol.OneKey, 51)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)주문조건 " & current_phase.ToString & " BuyStandardPrice " & buy_standard_price.ToString & " FallVolumeStandard " & fall_volume_standard.ToString & " PossibleOrderVolume " & possible_order_volume.ToString & " FinalOrderAmount " & final_order_amount.ToString)
            If final_order_amount > 0 AndAlso (current_phase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED Or _
                current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME) Then       '140703에 청산 기다리는 거 다시 넣기로 함. 매수주문 비율 높여야됨
                '140224 : StockOperator new 함수에서 t1101 요청하지 않고 바로 주문하도록 만들자.
                '140224 : AccountManager는 Prethresholders를 통해 Operation 객체에 접근할 수 있으므로 굳이 Operator List를 둘 필요는 없어보인다.
                If decision_maker.StockOperator Is Nothing Then
                    If Not decision_maker.NoMoreOperation Then
                        MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)생성주문 / " & buy_standard_price.ToString & " / " & final_order_amount.ToString)
                        decision_maker.StockOperator = New et2_Operation(decision_maker, buy_standard_price, final_order_amount, Me, False)       'stock operator 생성과 동시에 진입 주문
                        decision_maker.StockOperator.SetAccountManager(Me)
                        decision_maker.SilentLevelVolume = fall_volume_standard 'decision_maker.FallVolume / decision_maker.PatternLength * adaptive_silent_rate
                    Else
                        '이미 하한가 비상 발령한 위험한 종목은 진입하지 않는다.
                    End If
                Else
                    MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)인줄 알았는데 처음이 아니었던 건가요?")
                End If
            Else
                '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                'EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                MessageLogging(AccountCatString & " " & decision_maker.LinkedSymbol.Code & " :" & "(처음)주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.")
            End If
        End If
#End If
#End If
    End Sub
    'OneKey Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

    '시세정보 받고나서 1초 후 불리며 매매할 종목을 검색한다.
    Public Sub SymbolSearching()
        'Dim each_symbol As c03_Symbol
        Dim each_decision As c050_DecisionMaker ' c05E_PatternChecker
        Dim current_phase As c050_DecisionMaker.SearchPhase
        Dim call_prices As CallPrices
        Dim number_of_entered As Integer
        'Dim number_of_deciders As Integer

        'Prethresholders.Clear()
        number_of_entered = 0

        'If IsMA Then
        'number_of_deciders = MAIN_NUMBER_OF_DECIDERS
        'Else
        'number_of_deciders = TEST_NUMBER_OF_DECIDERS
        'End If

        'Dim decision_maker_list As List(Of c05E_PatternChecker)
        'For index As Integer = 0 To SymbolList.Count - 1
        'each_symbol = SymbolList(index)
        'If each_symbol.IsCallPriceAvailable Then
        'For index_decision_center As Integer = 0 To number_of_deciders - 1
        'If IsMain Then
        'decision_maker_list = SymbolList(index).MainDecisionMakerCenter(index_decision_center)
        'Else
        'decision_maker_list = SymbolList(index).TestDecisionMakerCenter(index_decision_center)
        'End If
        For sub_index As Integer = DecisionHolders.Count - 1 To 0 Step -1
            '각 종목의 각 decision maker들의 점수를 계산한다.
            each_decision = DecisionHolders(sub_index)
            '131128 : 각 디시전 메이커의 상태가 pre_threshold 진입한 상태이면 점수 계산. 그렇지 않으면 점수는 빵점처리
            'If each_decision.CurrentPhase = c05C_NoUnidelta_DecisionMaker.SearchPhase.PRETHRESHOLDED Then
            '131206 : 여기에 하강 볼륨도 점수에 포함시키면 좋을 것 같다.
            '131209 : 하강볼륨은 왜 포함시키려고 했는지 잘 모르겠고 어쨌든 현재가는 포함시키기 위해 etrade 매뉴얼을 뒤져봐야 한다.
            '현재가는 정보를 얻기도 어렵고 매수성공률을 높이는데 그렇게 큰 기여를 하지 않을 것 같아서 뺀다.
            '131129:매수1호가 + 매도1호가
            call_prices = each_decision.LinkedSymbol.LastCallPrices
            'each_decision.Score = each_decision.EnterPrice / (call_prices.BuyPrice1 + call_prices.SelPrice1)
            If IsMA Then
                Dim each_ma_decision As c05G_MovingAverageDifference = each_decision
                'Dim AScore_RelTime As Single        ' 0 < AScore < 1
                'Dim BScore_Stability As Single      ' 0 < BScore < 1
                'Dim CScore_CallPrice As Single      ' 0 < CScore < 1
                '191003: 왜 Score가 0인 decision 객체가 나오는지 설명이 안 됨. => 아.. good time to buy가 아니거나 중복이거나 하면 거래대상이 아니므로 패쓰함.



                Dim rel_time As Single = each_ma_decision.MinutesPassed / each_ma_decision.DEFAULT_HAVING_MINUTE
                each_ma_decision.ScoreA_RelTime = Math.Exp(-4 * (rel_time - 0.1) ^ 2) 'EXP(-4*((B2-0.1)^2))
                Dim S As Single = each_decision.LinkedSymbol.Stability
                each_ma_decision.ScoreB_Stability = 0.2 * Math.Exp(-0.1 * ((S + 9) ^ 2)) + 0.8 * (0.5 * (-Math.Exp(0.4 * (S + 4)) + Math.Exp(0.4 * (-S - 4))) / (Math.Exp(0.4 * (S + 4)) + Math.Exp(0.4 * (-S - 4))) + 0.5) '0.2*EXP(-0.1*(((B3+9)^2)))+ 0.8*(0.5*(-EXP(0.4*(B3+4)) + EXP(0.4*(-B3-4)))/(EXP(0.4*(B3+4)) + EXP(0.4*(-B3-4)))+0.5)
                each_ma_decision.ScoreC_CallPrice = CType(GetUnitPrice(each_decision.LastEnterPrice, each_decision.LinkedSymbol.MarketKind), Single) / (CType(call_prices.SelPrice1, Integer) - call_prices.BuyPrice1)
                '2020.02.27_done 상한가일 때 위에 산술연산 오버플로우 뜸 디버깅 바람
                '현재 시간기준 매수시 장종료 직전까지 청산가능한지 확인
                Dim remained_minutes As Integer = 60 * MarketEndHour + MarketEndMinute - (60 * Now.Hour + Now.Minute)
                If remained_minutes > each_ma_decision.DEFAULT_HAVING_MINUTE - each_ma_decision.MinutesPassed + 20 Then
                    '20 분 마진 줘서 장종료직전까지 청산가능 => 미수거래하자
                    each_ma_decision.ScoreD_DepositBonus = 100 / each_decision.LinkedSymbol.EvidanRate
                Else
                    '장종료직전까지 청산불가능 => 미수거래 안 된
                    each_ma_decision.ScoreD_DepositBonus = 10
                End If
                If ScoreByFailureCount Then
                    '2022.01.24: 매수 실패한 decision maker를 계속적으로 무의미하게 재매수시도와 취소를 반복하는 문제가 있다.
                    '2022.01.24: 주로 사용하는 Score의 범위가 -3 에서 1.5 정도인데, Score가 높은 종목을 우선적으로 매수하는 것도 중요하지만, 어차피 안 사질 종목을 계속적으로 사려고 시도하는 것도 문제다.
                    '2022.01.24: 이를 해결하기 위해 E_BuyFailMinus의 weight를 0.1 에서 2로 대폭 늘렸다.
                    If each_decision.StockOperator Is Nothing Then
                        each_ma_decision.ScoreE_BuyFailMinus = 2 * each_ma_decision.AccumBuyFailCount
                    Else
                        each_ma_decision.ScoreE_BuyFailMinus = 2 * (each_ma_decision.AccumBuyFailCount + each_ma_decision.StockOperator.BuyFailCount)
                    End If
                End If

                If AccountCat = 0 Then
                    'main
                    'If each_ma_decision.StockOperator Is Nothing Then
                    '2021.07.05: 계속 하락하는 종목에서 재진입 기회가 있는데도 종목내 다른 Decision Maker에 매수기회가 먼저 주어져 결국 진입한 Decision Maker 모두
                    '2021.07.05: 에서 이론수익률보다 낮은 수익률이 기록되는 경우가 많아서 재진입 기회를 좀 더 주기 위해 이와 같이 진입하지 않은 Decision Maker들만
                    '2021.07.05: Operator Minus를 준다.
                    '2021.09.15: Multiple Entering을 당분간 포기한다. Operator Minus는 StockOperator 유무여부와 관계없이 같은 계좌면 종목 전체에 적용된다. Factor는 높였다.
                    each_ma_decision.ScoreF_OperatorsMinus = 5 * each_ma_decision.LinkedSymbol.MainOperators
                    'End If
                ElseIf AccountCat = 1 Then
                    'sub
                    'If each_ma_decision.StockOperator Is Nothing Then
                    '2021.07.05: 계속 하락하는 종목에서 재진입 기회가 있는데도 종목내 다른 Decision Maker에 매수기회가 먼저 주어져 결국 진입한 Decision Maker 모두
                    '2021.07.05: 에서 이론수익률보다 낮은 수익률이 기록되는 경우가 많아서 재진입 기회를 좀 더 주기 위해 이와 같이 진입하지 않은 Decision Maker들만
                    '2021.07.05: Operator Minus를 준다.
                    '2021.09.15: Multiple Entering을 당분간 포기한다. Operator Minus는 StockOperator 유무여부와 관계없이 같은 계좌면 종목 전체에 적용된다. Factor는 높였다.
                    each_ma_decision.ScoreF_OperatorsMinus = 5 * each_ma_decision.LinkedSymbol.SubOperators
                    'End If
                End If

                If AccountCat = 0 Then
                    'Main account
                    If each_ma_decision.MinutesPassed / each_ma_decision.DEFAULT_HAVING_MINUTE > 0.8 AndAlso each_ma_decision.StockOperator Is Nothing Then
                        '2020.0818: 들어가지 말고 포기해라
                        '2021.08.08: 단 첫진입만 포기하고 재진입은 포기하지 말아라.
                        each_decision.Score = -100
                    Else
                        'each_decision.Score = each_ma_decision.LinkedSymbol.Newstab - each_ma_decision.ScoreE_BuyFailMinus - each_ma_decision.ScoreF_OperatorsMinus
                        '2021.08.17: main계좌 score도 sub랑 동일하게 바꾼다.
                        each_decision.Score = Math.Log10(50000000 / each_ma_decision.FallVolume) - each_ma_decision.ScoreE_BuyFailMinus - each_ma_decision.ScoreF_OperatorsMinus
                        'each_decision.Score = each_ma_decision.ScoreA_RelTime * each_ma_decision.ScoreB_Stability * each_ma_decision.ScoreC_CallPrice + each_ma_decision.ScoreD_DepositBonus - each_ma_decision.ScoreE_BuyFailMinus
                    End If
                    'each_decision.Score = 5000 - Math.Abs(CType(each_decision, c05G_MovingAverageDifference).MinutesPassed - 5) + GetUnitPrice(each_decision.EnterPrice, each_decision.LinkedSymbol.MarketKind) / (call_prices.SelPrice1 - call_prices.BuyPrice1)
                Else
                    'Sub account
                    If each_ma_decision.MinutesPassed / each_ma_decision.DEFAULT_HAVING_MINUTE > 0.8 AndAlso (remained_minutes > 15) AndAlso each_ma_decision.StockOperator Is Nothing Then
                        '2020.0818: 들어가지 말고 포기해라
                        '2021.08.08: 단 첫진입만 포기하고 재진입은 포기하지 말아라.
                        '2021.10.19: 0.6에서 0.8로 고치고 장종료 15분남았을 때에는 진입하자.
                        each_decision.Score = -100
                    Else
                        'each_decision.Score = each_ma_decision.LinkedSymbol.Newstab - each_ma_decision.ScoreE_BuyFailMinus - each_ma_decision.ScoreF_OperatorsMinus
                        '2021.08.17: 떨어질 종목만 집중적으로 노골적으로 매수하고 있다. 폐단을 막기 위해 무슨 수라도 써야 했는데, 분명한 사실 하나는 크게 봤을 때 FallVolume이 커질 수록
                        '2021.08.17: 수익률이 추락한다는 점이다. 이것을 Score에 이용하고자 FallVolume Score를 만들었다. Log10(50000000 / FallVolume)을 하면 200만원대는 1.35 정도, 수십억원대는
                        '2021.08.17: -3 정도의 값을 가지게 된다. 그래서 FallVolume이 낮은 것들 위주로 매수하려고 한다.
                        each_decision.Score = Math.Log10(50000000 / each_ma_decision.FallVolume) - each_ma_decision.ScoreE_BuyFailMinus - each_ma_decision.ScoreF_OperatorsMinus
                    End If
                End If
            Else
#If 0 Then
                If CType(call_prices.SelPrice1, Integer) - call_prices.BuyPrice1 = 0 Then
                    '2023.11.11 : divide by 0  exception 처리로직 구현
                    each_decision.Score = 0
                Else
                    each_decision.Score = GetUnitPrice(each_decision.LastEnterPrice, each_decision.LinkedSymbol.MarketKind) / (CType(call_prices.SelPrice1, Integer) - call_prices.BuyPrice1)
                End If
#End If
                If call_prices.BuyPrice1 = 0 Then
                    '이럴리는 없겠지만
                    each_decision.Score = 0
                Else
                    each_decision.Score = each_decision.TargetBuyPrice / call_prices.BuyPrice1
                End If
#If PATTERN_PRETHRESHOLD Then
                If each_decision.CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
                    '2024.01.08 : WAIT_EXIT_TIME 일 경우 PRETHRESHOLD 대비 가산점 있음.
                    'each_decision.Score += 10
                End If
#End If
            End If

            'End If
            '131211 : Symbolsearching은 5초마다 주기적으로 하고 pre-threshold 종목 리스트를 만든다.
            '이 리스트는 실시간 가격정보에 의해 업데이트 되고 약 200ms 간격으로 이 리스트를 체크하여 매매에 반영하도록 한다.
            '140603 : 이 위에 200ms 타이머 어떻게 하기로 했었는지 기억이 안 난다. => 200ms 타이머는 pre threshold와는 상관없고 하한가 비상 검출시에만 사용된다.

            SafeEnterTrace(each_decision.LinkedSymbol.OneKey, 31)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            current_phase = each_decision.CurrentPhase
            SafeLeaveTrace(each_decision.LinkedSymbol.OneKey, 34)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If current_phase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED Then
                'pre threshold 상태일 경우 가산점
                'each_decision.Score = each_decision.Score + 0.5
            End If
            'If current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME AndAlso sub_index < DecisionHolders.Count - 1 AndAlso DecisionHolders(sub_index + 1).CurrentPhase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
            'wait exit time 뒤에 다른 decision maker가 있고 그것도 wait exit time 이라면 그걸 먼저할 필요가 있지 않을까 해서...
            'each_decision.Score = each_decision.Score - 0.1 * (DecisionHolders.Count - 1 - sub_index)
            'End If
            'If current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then
            '이미 진입한 놈들을 count한다.
            'number_of_entered = number_of_entered + 1
            'End If
            If current_phase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED Or
              current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME Then       '청산 기다리는 것도 pre-threshold에 포함시킴, '청산 기다리는 거 140320에 빼기로 함 140703에 다시 넣기로 함. 매수주문 비율 높여야됨
                If each_decision.StockOperator IsNot Nothing AndAlso each_decision.StockOperator.EnterExitState = EnterOrExit.EOE_Exit Then
                    'decision maker가 진입가능한 조건이더라도 stock operator가 이미 끝나려고 하면 진입안한다.
                    SafeEnterTrace(StockListKey, 70)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    DecisionHolders.RemoveAt(sub_index)
                    SafeLeaveTrace(StockListKey, 71)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    'Else
                    'MessageLogging(AccountCatString & " " & each_decision.LinkedSymbol.Code & " : 점수 " & each_decision.Score.ToString & " EnterPrice " & each_decision.EnterPrice.ToString & " BuyPrice1 " & each_symbol.LastCallPrices.BuyPrice1.ToString & " SelPrice1 " & each_symbol.LastCallPrices.SelPrice1.ToString)
                    'pre threshold였다가 WAIT FALLING상태로 돌아오고나서 아직 stock operator 철수를 안 했을 때를 고려할까 했는데 그럴필요 없을 것 같다. 왜냐면 몇초지나면 어차피 철수될 거기 때문에.
                    'DecisionHolders.Add(each_decision)
                End If
            Else 'if current_phase = c050_DecisionMaker.SearchPhase.WAIT_FALLING or DONE
                SafeEnterTrace(StockListKey, 80)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                DecisionHolders.RemoveAt(sub_index)
                SafeLeaveTrace(StockListKey, 81)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
            End If
        Next
        'Next
        'Else
        'MessageLogging(each_symbol.Code & " : 아직 호가가 available하지 않다")
        'End If
        'Next
        'MessageLogging("Debugdebug " & debugdebug.ToString)

        If SortByScore Then
            SafeEnterTrace(StockListKey, 60)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            DecisionHolders.Sort(AddressOf PrethresholderComparer)
            SafeLeaveTrace(StockListKey, 61)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End If

        'Prethresholders 에서 중복된 종목이 있으면 잘라버린다.
#If 0 Then      ' 이미 real time 에서 중복제거된 상태니까 안 해도 된다.
        '중복된 종목도 진입시점이 다르다면 잘라버리지 않는 게 필요하다. Done in 180614
        Dim prethreshold_symbol_list As New List(Of String)
        Dim prethreshold_entertime_list As New List(Of Date)
        Dim is_duplicate_detected As Boolean
        Dim index_i As Integer = 0
        While index_i < Prethresholders.Count
            is_duplicate_detected = False
            For index_j As Integer = 0 To prethreshold_symbol_list.Count - 1
                '프리쓰레시홀더 24개인데 인덱스아이가 24인 경우 발생하여 익셉션 20180516
                If Prethresholders(index_i).LinkedSymbol.Code = prethreshold_symbol_list(index_j) AndAlso Prethresholders(index_i).EnterTime = prethreshold_entertime_list(index_j) Then
                    Prethresholders.RemoveAt(index_i)
                    is_duplicate_detected = True
                    Exit For
                End If
            Next
            If Not is_duplicate_detected Then
                prethreshold_symbol_list.Add(Prethresholders(index_i).LinkedSymbol.Code)
                prethreshold_entertime_list.Add(Prethresholders(index_i).EnterTime)
                index_i = index_i + 1
            End If
        End While
        prethreshold_symbol_list.Clear()
#End If
        'DecisionHolder의 복사본을 만들어 Prethresholder에 넣는다
        Prethresholders.Clear()
        SafeEnterTrace(StockListKey, 50)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Prethresholders = DecisionHolders.GetRange(0, DecisionHolders.Count)
        SafeLeaveTrace(StockListKey, 51)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
#If 0 Then      '190401
        If IsMain Then
            '181008: Prethresholder 중에 가장 오래된 놈을 하나 제거한다. 이것은 무겹침에서 진입하는 것보다 1장 겹침째에서 진입하는 것이 수익이 좋다는 통계를 근거로 한 작전이다. 여러 겹침도 소화할 만큼 자본이 충분하면 없애는 것이 좋을 듯 싶다.
            Dim oldest_enter_time As Date = [Date].MaxValue
            Dim index_of_oldest As Integer = -1
            For index As Integer = 0 To Prethresholders.Count - 1
                If Prethresholders(index).EnterTime < oldest_enter_time Then
                    'update the oldest enter time
                    oldest_enter_time = Prethresholders(index).EnterTime
                    index_of_oldest = index
                End If
            Next
            If index_of_oldest >= 0 Then
                Prethresholders.RemoveAt(index_of_oldest)
            End If
        End If
#End If
#If 0 Then
        '2024.01.08 : test 계좌에서도 VI 인 놈들은 Money Distribuet 에서 처리하기로 한다. 왜냐면 VI 인데도 주문들어가 있는 놈들은 매수중단 명령 날려야 하기 때문이다.
        '현재 VI 인 놈들은 제거한다.
        For index As Integer = Prethresholders.Count - 1 To 0 Step -1
            '2022.09.07: MA 계좌는 VI 여도 제거하지 않는다. 나중에 Money distribute 할 때 VI 인 녀석들은 알아서 제외된다.
            If (Not IsMA) AndAlso Prethresholders(index).LinkedSymbol.VI Then
                Prethresholders.RemoveAt(index)
            End If
        Next
#End If

        'NumberOfEnteredList.Add(Prethresholders.Count)
        'If NumberOfEnteredList.Count > 3 Then
        'NumberOfEnteredList.RemoveAt(0)
        'End If

    End Sub

    '매수를 위한 돈 분배
    Public Sub MoneyDistribute()
        '140317: 이 함수, 이전에 주문 들어갔던 놈들에 대한 고려 해야된다. 그리고 프라이오리티 낮은 놈들 취소시키는 로직도 개발해야 된다.
        '140317: 프라이오리티는 decision maker 별로 account manager가 직접 관리하는 것이 좋지 않을까.
        Dim possible_order_volume As Double
        Dim fall_volume_standard As Double
        Dim buy_standard_price As UInt32
        Dim current_phase As c050_DecisionMaker.SearchPhase
        'Dim ordered_volume As Double = 0
        Dim misoo_ok As Boolean = False
        Dim previously_ordered_volume As Double
        Dim money_for_this_one As Double

        'active operation들을 조사하여 금액을 합산한다
        '191019_TODO_DONE: 굳이 왜 합산하는가 예탁자산총액을 등분해서 활용하라.
        Dim pizza_one_piece, one_piece_for_this_one As Double
        If IsMA Then
            If AccountCat = 0 Then
                'Main
                pizza_one_piece = TotalProperty / MAIN_NUMBER_OF_PIECES
            Else 'If AccountCat = 1 Then
                'Sub
                pizza_one_piece = TotalProperty / SUB_NUMBER_OF_PIECES
            End If
        Else
            If AccountCat = 0 Then
                'Main
                pizza_one_piece = TotalProperty / MAIN_NUMBER_OF_PIECES
            Else
                pizza_one_piece = TotalProperty / TEST_NUMBER_OF_PIECES
            End If
        End If
        SafeEnterTrace(OrdableKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim rest_of_ordable100 As Double = Ordable100
        'Dim rest_of_ordable40 As Double = Ordable40
        'Dim rest_of_ordable30 As Double = Ordable30
        SafeLeaveTrace(OrdableKey, 31)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        '191019_TODO_DONE: 그래도 active operation이 필요한 이유는 오늘을 넘기는 operation들의 합이 예탁자산 총액을 넘지 않도록 관리해야 하기 때문이다.
        'Dim total_money_in_order As Long = 0
        'For index As Integer = 0 To ActiveOperationList.Count - 1
        'total_money_in_order += ActiveOperationList(index).AssignedMoney
        'Next
        'Dim possible_order_volume_piece As Long = (total_money_in_order + Ordable100) / 3
        '2023.11.11 : 오버투데이 계산은 MA 일 때만이다. 다른 전략들은 무효하다.
        Dim over_today As Double = 0
        Dim sale_count As Integer = 0
        Dim sale_money As Double = 0
        For index As Integer = 0 To Prethresholders.Count - 1
            If Prethresholders(index).ScoreD_DepositBonus = 10 AndAlso Prethresholders(index).StockOperator IsNot Nothing Then
                '2022.09.06: Not Prethresholders(index).LinkedSymbol.IsCallPriceAvailable  Orelse Prethresholders(index).LinkedSymbol.VI 조건인 것도 over_today 계산에 포함시켜야 됨. 아침에 아직 초기화가 덜 된 상태이기 때문인데 이걸 포함 안 시키면 사 놓은 종목리스트에서 빠지게 되어 주문가능금액이 있는 것으로 착각한다.
                If Prethresholders(index).LinkedSymbol.RecordList.Count > 0 AndAlso Prethresholders(index).LinkedSymbol.RecordList.Last.Price > 0 Then
                    over_today = over_today + Prethresholders(index).StockOperator.ThisAmount * Prethresholders(index).LinkedSymbol.RecordList.Last.Price '2022.09.22: over_today 계산 기준가를 구입가에서 현재가로 바꿨다.
                Else
                    over_today = over_today + Prethresholders(index).StockOperator.ThisAmount * Prethresholders(index).StockOperator.InitPrice
                End If
            End If

            If IsMA AndAlso sale_money < TotalProperty Then
                sale_money += Math.Min(Prethresholders(index).FallVolume * SilentInvolvingAmountRate / Prethresholders(index).ALLOWED_ENTERING_COUNT, pizza_one_piece)
                sale_count += 1
            End If
        Next
#If ALLOWED_SALE_COUNT Then
        If IsMA Then
            If sale_money = 0 Then
                AllowedSaleCountFromLastSale = 0
            Else
                AllowedSaleCountFromLastSale += CType(sale_count, Double) / CType(12 * MINUTES_FOR_DISTRIBUTION, Double) * (TotalProperty / Math.Min(TotalProperty, sale_money))       '허용된 매매 카운트 증가
                '위에서 뒤에 괄호텀은 TotalProperty가 무지막지하게 많을 경우 딱히 시간배분하여 매매를 할 필요가 없을 것 같아서 그렇게 했다. 보통의 가난한 경우라면 1이 된다.
                '2021.12.27: 알고 보니 여기에 분당 1회가 아니라 5초당 1회 들어오게 되어 있었다. 따라서 의도대로 하려고 MINUTES_FOR_DISTRIBUTION 앞에 12를 곱했다.
                '2021.12.29: 12곱하고 이틀동안 해보니 매수율이 줄어들어 다시 원래대로 했다. 그래도 sale_money가 0일 때의 버그가 그동안 있었으므로 좀 차이가 있을 수도 있겠다.
            End If
        End If
#End If
        If IsMA Then
            MessageLogging("계좌공통" & AccountCatString & " " & "오버투데이 점검: " & Prethresholders.Count & "개, " & over_today.ToString)
        End If
        Dim actual_sale_count As Integer = 0
        Dim stop_buying_flag As Boolean = False
        For index As Integer = 0 To Prethresholders.Count - 1
            '190514: 여기서 계좌별종목별 주문나간 금액 합산해서 Ordable100 넘어가지 않게 조절해주는 것 필요하다.
            '190515: 그럴 필요 없이 EvidanRate을 사용 안 하기만 하면 되는 것 같다.
            '2024.06.10 : 피자조각에 NumberOfEntering 을 곱하는 것은 더 이상 하지 않기로 했다. 과거에 왜 그렇게 했었는지 모르겠다.
            one_piece_for_this_one = pizza_one_piece '* Prethresholders(index).NumberOfEntering

            '2024.04.12 : SilentLevelVolume 계산 빼먹는 경우가 있어서 if 바깥으로 빼놨다.
            '2024.06.10 : SilentLevelVolume 계산을 제일 먼저 하는 것이 좋겠다. 
            If IsMA Then
                Prethresholders(index).SilentLevelVolume = Prethresholders(index).FallVolume * SilentInvolvingAmountRate / Prethresholders(index).ALLOWED_ENTERING_COUNT
            Else
                Prethresholders(index).SilentLevelVolume = Prethresholders(index).FallVolume / Prethresholders(index).PatternLength * SilentInvolvingAmountRate / Prethresholders(index).ALLOWED_ENTERING_COUNT
            End If

            If IsMA AndAlso over_today > TotalProperty - one_piece_for_this_one Then
                '물리기 전에 그만 두자.
                MessageLogging("계좌공통" & AccountCatString & " " & "물리기 전에 그만둬, over_today: " & over_today.ToString)
                Exit For
            End If

            If Not IsMA AndAlso (stop_buying_flag Or Prethresholders(index).LinkedSymbol.VI) Then
                '2024.01.08 : 우선순위 높은 녀석에게 몰아주기 위해 현재 VI 이거나 매수시도못한 종목보다 우선순위 낮은 매수중인 것들은 매수중단
                SafeEnterTrace(Prethresholders(index).LinkedSymbol.OneKey, 70)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                current_phase = Prethresholders(index).CurrentPhase
                If (current_phase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED OrElse current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME) AndAlso
                  Prethresholders(index).StockOperator IsNot Nothing AndAlso Prethresholders(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_ORDER_REQUESTED AndAlso Prethresholders(index).StockOperator.OperatorList.Count > 0 Then
                    MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " :" & "Score 역전으로 인한 매수중단 들어간다. VI " & Prethresholders(index).LinkedSymbol.VI.ToString)
                    Prethresholders(index).StockOperator.StopBuying()
                    Prethresholders(index).IsStopBuyingCompleted = False
                Else
                    MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " :" & "찌그러짐. Score " & Prethresholders(index).Score & " VI " & Prethresholders(index).LinkedSymbol.VI.ToString)
                End If
                SafeLeaveTrace(Prethresholders(index).LinkedSymbol.OneKey, 71)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Continue For
            End If


            If Prethresholders(index).Score <= -100 Then
                '2020.0818: 시간 다 지나서 하는 의미없는 투자는 그만두자(for sub). 9월 5일부로 main으로도 확대되었다.
                Continue For
            End If

            If (Not Prethresholders(index).LinkedSymbol.IsCallPriceAvailable) OrElse Prethresholders(index).LinkedSymbol.VI Then
                '2022.09.06: VI 중인 종목등 decision목록에는 일단 포함시켰다가 여기서 거르는 걸로 바뀜.
                Continue For
            End If

            If IsMA Then
                Dim MA_active_decisionmaker As c05G_MovingAverageDifference = Prethresholders(index)
                'If MA_active_decisionmaker.MinutesPassed < 381 Then
                '20230610: 걸린 지 만 하루가 지나지 않은 것들은 진입하지 못하도록 바꿨다. 왜냐면 상장폐지사유 발생 등 갑작스러운 부정적인 이유로 물려버리는 경우가 종종 있기 때문이다.
                If MA_active_decisionmaker.EnterTime.Date = Today.Date Then
                    '20230619: 걸린 지 하루(만 하루 아님)가 지나지 않은 것들은 진입하지 못하도록 바꿨다. 왜냐면 상장폐지사유 발생 등 갑작스러운 부정적인 이유로 물려버리는 경우가 종종 있기 때문이다.

                    Continue For
                End If
            End If
#If ALLOWED_SALE_COUNT Then
            If IsMA AndAlso AllowedSaleCountFromLastSale <= 0 Then
                '더이상의 매매는 허용되지 않는다.
                Exit For
            End If
#End If

            '이 종목의 투자금액 설정
            If IsMA Then
                '2024.04.01 : 증거금률 30~40 되는 녀석들이 여기서 너무 많이 매수되어 며칠 뒤 반대매매되게 생겼다.
                '2024.04.01 : 여기서는 증거금률 100으로 계산하여 매수해야 되겠다. 증거금률 낮은 녀석들은 운이 좋으면 추가매수로 좀 더 매수되겠지
                money_for_this_one = Math.Min(rest_of_ordable100, one_piece_for_this_one)
#If 0 Then
                '2024.01.13 : 증거금률로부터 간단히 계산됨을 알았다 이제 if 나 select는 필요없다.
                If rest_of_ordable100 * 100 / Prethresholders(index).LinkedSymbol.EvidanRate > 1 * one_piece_for_this_one Then
                    money_for_this_one = one_piece_for_this_one
                Else
                    money_for_this_one = rest_of_ordable100 * 100 / Prethresholders(index).LinkedSymbol.EvidanRate
                End If
#End If
#If 0 Then
                Select Case Prethresholders(index).LinkedSymbol.EvidanRate
                    Case 30
                        If rest_of_ordable30 > 1 * one_piece_for_this_one Then
                            money_for_this_one = one_piece_for_this_one
                        Else
                            money_for_this_one = rest_of_ordable30
                        End If
                    Case 40
                        If rest_of_ordable40 > 1 * one_piece_for_this_one Then
                            money_for_this_one = one_piece_for_this_one
                        Else
                            money_for_this_one = rest_of_ordable40
                        End If
                    Case 50
                        If rest_of_ordable100 > 1 * one_piece_for_this_one Then
                            money_for_this_one = one_piece_for_this_one
                        Else
                            money_for_this_one = rest_of_ordable100
                        End If
                    Case Else '100
                        If rest_of_ordable100 > 1 * one_piece_for_this_one Then
                            money_for_this_one = one_piece_for_this_one
                        Else
                            money_for_this_one = rest_of_ordable100
                        End If
                End Select
#End If
            Else
                '2023.11.09: 이제부터 MA 전략은 위와 같이 전에 하던 그대로 피자조각을 쪼개 매매하고, MA 아닌 전략 (DoubleFall 과 PCRenew 전략)은 레버리지를 최대화하기 위해 피자 한판으로 간다.
                '2023.11.09: 즉, 앞에 계산했던 pizza_one_piece 및 one_peice_for_this_one 이 필요없다.
#If 0 Then
                Select Case Prethresholders(index).LinkedSymbol.EvidanRate
                    Case 30
                        money_for_this_one = rest_of_ordable35
                    Case 40
                        money_for_this_one = rest_of_ordable50
                    Case 50
                        money_for_this_one = rest_of_ordable50
                    Case Else '100
                        money_for_this_one = rest_of_ordable100
                End Select
#End If
#If 0 Then
                '2023.11.11 : 증거금률이 생각보다 다양하게 존재한다. 30,40,50 까지는 봤는데 20도 있는 것은 처음 봤다. 모든 증거금률 대응을 위해 위의 Select Case 에서 아래 If 문으로 바꿨다.
                If Prethresholders(index).LinkedSymbol.EvidanRate < 30 Then
                    money_for_this_one = rest_of_ordable30
                ElseIf Prethresholders(index).LinkedSymbol.EvidanRate < 40 Then
                    money_for_this_one = rest_of_ordable40
                Else
                    money_for_this_one = rest_of_ordable100
                End If
#End If
                '2024.01.13 : 증거금률로부터 간단히 계산됨을 알았다 이제 if 나 select는 필요없다.
                money_for_this_one = rest_of_ordable100 * 100 / Prethresholders(index).LinkedSymbol.EvidanRate

            End If

            '해당 종목에 이미 주문한 금액 계산
            If Prethresholders(index).StockOperator Is Nothing Then
                previously_ordered_volume = 0
            Else
                previously_ordered_volume = Prethresholders(index).StockOperator.ThisAmount * Prethresholders(index).StockOperator.InitPrice
            End If
#If 0 Then
            If IsMain Then
#If MOVING_AVERAGE_DIFFERENCE Then
                'Moving average 전략에서는 증거금률 100 미만인 거래 안 된다.
                'possible_order_volume = (Ordable100 - ordered_volume)
                '현재 시간기준 매수시 장종료 직전까지 청산가능한지 확인
                If Prethresholders(index).DScore_DepositBonus <> 1 Then
                    '20 분 마진 줘서 장종료직전까지 청산가능 => 미수거래하자
                    misoo_ok = True
                    possible_order_volume = (Ordable100 - ordered_volume) * 100 / Prethresholders(index).LinkedSymbol.EvidanRate
                Else
                    '장종료직전까지 청산불가능 => 미수거래 안 된다.
                    misoo_ok = False
                    possible_order_volume = (Ordable100 - ordered_volume)
                End If
#Else
                possible_order_volume = (Ordable100 - ordered_volume) * 100 / Prethresholders(index).LinkedSymbol.EvidanRate
#End If
            Else
                possible_order_volume = (Ordable100 - ordered_volume) * 100 / Prethresholders(index).LinkedSymbol.EvidanRate
            End If
#End If
            'Fall Volume Standard 구하기
            If IsMA Then
                fall_volume_standard = Prethresholders(index).FallVolume * SilentInvolvingAmountRate / Prethresholders(index).ALLOWED_ENTERING_COUNT
            Else
                fall_volume_standard = Prethresholders(index).FallVolume / Prethresholders(index).PatternLength * SilentInvolvingAmountRate / Prethresholders(index).ALLOWED_ENTERING_COUNT  '패턴일 경우의 계산법이다. 패턴 아닌 경우는 또 다르다.
            End If

            '2024.01.08 : test 계좌에서 previously_ordered_volume / fall_volume_standard 비율이 특정비율을 못 넘어간다면 이하 낮은 score 종목에서 매수중인 것들은 모두 중단시키도록 한다.
            If IsFlex AndAlso fall_volume_standard > rest_of_ordable100 * 0.7 AndAlso previously_ordered_volume / fall_volume_standard < 0.5 Then
                stop_buying_flag = True
            End If

#If 0 Then
            '2024.06.10 이전
            'fall volume standard와 이 종목 투자금액 중 작은 것이 possible order volume이 된다
            possible_order_volume = Math.Min(fall_volume_standard, money_for_this_one)

            'possible order volume에서 기존 주문 들어간 것을 제한다.
            possible_order_volume = possible_order_volume - previously_ordered_volume
#Else
            '2024.06.10 이후
            '2024.06.19 : 아래 비교기준이 fall volume standard 가 아니라 기 주문들어간 금액을 뺀 금액을 비교기준으로 하는 게 맞기 때문에 fall_volume_standard - previously_ordered_volume 으로 하는 게 맞고
            '2024.06.19 : 오버플로우를 피하기 위해 previously_ordered_volume 은 우측으로 이항한다.
            If fall_volume_standard > money_for_this_one + previously_ordered_volume Then
                '이 종목 투자금액이 더 작으니 이걸 기본으로 하고 이 종목 투자금액 계산시 기 주문 들어간 금액은 이미 계산이 되어 있기 때문에 그냥 이걸 possible_order_volume 으로 하면 된다.
                possible_order_volume = money_for_this_one
            Else
                '투자금액이 넘치는 상황. 이 때는 fall_volume_standard 를 기본으로 하되, fall_volume_standard 에는 기 주문 금액이 계산이 안 되어 있기 때문에 이걸 빼줘야 한다.
                possible_order_volume = fall_volume_standard - previously_ordered_volume
            End If
#End If

            If possible_order_volume < 0 Then
                '이런 경우가 있을지 모르겠는데..  => (2024.01.09) 이런 경우 많이 있다. 그래서 아래 Message log 없앴다.
                'MessageLogging(Prethresholders(index).LinkedSymbol.Code & " :" & "possible_order_volume이 0이 나왔다.")
                possible_order_volume = 0
            End If

#If 0 Then
            'TODO: silent level 볼륨 대비 매수된 볼륨 percentage 기록 필요하다.=>180804 시점 완료
            If Prethresholders(index).StockOperator Is Nothing Then
                '첫주문이면
                If IsMain Then
                    fall_volume_standard = Prethresholders(index).FallVolume * SilentInvolvingAmountRate
                Else
                    fall_volume_standard = Prethresholders(index).FallVolume / Prethresholders(index).PatternLength * SilentInvolvingAmountRate  '패턴일 경우의 계산법이다. 패턴 아닌 경우는 또 다르다.
                End If
            Else
                '두번째 이상 주문이면 => 주문된 거에 대한 fall volume은 제한다.
                '두번째 이상 주문엔 굳이 adaptive silent rate이 필요없겠지
                If IsMain Then
                    fall_volume_standard = Math.Max(Prethresholders(index).FallVolume * SilentInvolvingAmountRate - Prethresholders(index).StockOperator.ThisAmount * Prethresholders(index).StockOperator.BuyStandardPrice, 0)
                Else
                    fall_volume_standard = Math.Max(Prethresholders(index).FallVolume / Prethresholders(index).PatternLength * SilentInvolvingAmountRate - Prethresholders(index).StockOperator.ThisAmount * Prethresholders(index).StockOperator.BuyStandardPrice, 0) '패턴일 경우의 계산법이다. 패턴 아닌 경우는 또 다르다.
                End If
            End If
#End If
            Dim final_order_amount As UInt32
            'If possible_order_volume > fall_volume_standard Then
            '이 한 종목 이상으로 살 만큼의 돈이 있다.
            'final_order_amount = Math.Floor(fall_volume_standard / (Prethresholders(index).TargetBuyPrice * (1 + BuyPower)))
            'Else
            '이 한 종목도 못 살만큼 돈이 없다.
            final_order_amount = Math.Floor(possible_order_volume / (Prethresholders(index).TargetBuyPrice * (1 + BuyPower)))

            'End If

            '2023.10.24 : 코스피 단주거래 허용된지 10년이 다 되어가는데 아직 업데이트가 안 됐냐 너무하다 정말
            'If Prethresholders(index).LinkedSymbol.MarketKind = MARKET_KIND.MK_KOSPI And (Prethresholders(index).TargetBuyPrice * (1 + BuyPower)) < 50000 Then
            'final_order_amount = Math.Floor(final_order_amount / 10) * 10       '10단위로 자름
            'End If

            Dim local_call_price As CallPrices = Prethresholders(index).LinkedSymbol.LastCallPrices
            If local_call_price.BuyPrice1 = 0 Then
                '하한가 상황 => 사지 않는다.
                MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " " & "하한가 상황 => 사지 않는다.")
                Continue For
            Else
                '180113: 매수시 Operation 에 넘겨주는 것은 기준매수가를 넘겨주자. Operation 또는 Operator에서 그 기준매수가를 기준에서 상황에 맞게 매수가를 변경시킬 수 있게..
                buy_standard_price = NextCallPrice(Prethresholders(index).TargetBuyPrice * (1 + BuyPower), 0) ', Prethresholders(index).LinkedSymbol.MarketKind)

                If buy_standard_price <= Prethresholders(index).LinkedSymbol.LowLimitPrice Then
                    '주문가가 하한가 아래면 아예 주문하지 않는다.
                    MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " " & "기준매수가(" & buy_standard_price.ToString & ")가 하한가(" & Prethresholders(index).LinkedSymbol.LowLimitPrice.ToString & ")아래면 아예 주문하지 않는다.")
                    Continue For
                End If

#If 0 Then
                'ordered volume 계산하여 다음 종목 주문시 주문 가능금액에서 주문된 금액을 제함
                If IsMain Then
#If MOVING_AVERAGE_DIFFERENCE Then
                    If Prethresholders(index).ScoreD_DepositBonus <> 1 Then
                        '미수 ok
                        ordered_volume += final_order_amount * buy_standard_price * Prethresholders(index).LinkedSymbol.EvidanRate / 100
                    Else
                        '미수 불허
                        ordered_volume += final_order_amount * buy_standard_price
                    End If
#Else
                    ordered_volume += final_order_amount * buy_standard_price * Prethresholders(index).LinkedSymbol.EvidanRate / 100
#End If
                Else
                    ordered_volume += final_order_amount * buy_standard_price * Prethresholders(index).LinkedSymbol.EvidanRate / 100
                End If
#End If

                '주문해보자. prethreshold list 만들고나서 current phase가 바뀌었을 수 있으니 주문전 다시 확인한다.
                SafeEnterTrace(Prethresholders(index).LinkedSymbol.OneKey, 50)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                current_phase = Prethresholders(index).CurrentPhase
                MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " " & "주문조건 " & current_phase.ToString & " Score " & Prethresholders(index).Score & " BuyStandardPrice " & buy_standard_price.ToString & " FallVolumeStandard " & fall_volume_standard.ToString & " PossibleOrderVolume " & possible_order_volume.ToString & " PreviouslyOrderVolume " & previously_ordered_volume.ToString & " FinalOrderAmount " & final_order_amount.ToString)
                If final_order_amount > 0 AndAlso (current_phase = c050_DecisionMaker.SearchPhase.PRETHRESHOLDED Or
                    current_phase = c050_DecisionMaker.SearchPhase.WAIT_EXIT_TIME) Then       '140703에 청산 기다리는 거 다시 넣기로 함. 매수주문 비율 높여야됨
                    '140224 : StockOperator new 함수에서 t1101 요청하지 않고 바로 주문하도록 만들자.
                    '140224 : AccountManager는 Prethresholders를 통해 Operation 객체에 접근할 수 있으므로 굳이 Operator List를 둘 필요는 없어보인다.

                    If Prethresholders(index).StockOperator Is Nothing Then
                        If Not Prethresholders(index).NoMoreOperation Then
#If ALLOWED_SALE_COUNT Then
                            If IsMA Then
                                AllowedSaleCountFromLastSale -= 1               '한 종목 주문할 때마다 허용된 매매 종목수를 1씩 차감한다.
                            End If
#End If
                            Prethresholders(index).ScoreSave = Prethresholders(index).Score     ' 살 때 score 저장한다.
                            MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " " & "생성주문 / " & buy_standard_price.ToString & " / " & final_order_amount.ToString)
                            Prethresholders(index).StockOperator = New et2_Operation(Prethresholders(index), buy_standard_price, final_order_amount, Me, False)       'stock operator 생성과 동시에 진입 주문
                            Prethresholders(index).StockOperatorDebug += 10
                            Prethresholders(index).StockOperator.SetAccountManager(Me)

                            rest_of_ordable100 -= final_order_amount * buy_standard_price * Math.Max(Prethresholders(index).LinkedSymbol.EvidanRate / 100, 1)
                            'rest_of_ordable40 -= final_order_amount * buy_standard_price * Math.Max(Prethresholders(index).LinkedSymbol.EvidanRate / 40, 1)
                            'rest_of_ordable30 -= final_order_amount * buy_standard_price * Math.Max(Prethresholders(index).LinkedSymbol.EvidanRate / 30, 1)
                            If Prethresholders(index).ScoreD_DepositBonus = 10 Then
                                over_today += final_order_amount * buy_standard_price
                            End If
                        Else
                            '이미 하한가 비상 발령한 위험한 종목은 진입하지 않는다.
                        End If
                    ElseIf Prethresholders(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_DONE Then
                        'stock operator가 있지만 상태가 DONE인 경우에는 추가주문을 하지 않는다. 왜냐면 하한가비상 발령되어서 다 팔고 DONE 되었기 때문이다.
                        SafeLeaveTrace(Prethresholders(index).LinkedSymbol.OneKey, 51)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        Continue For
                    ElseIf Prethresholders(index).StockOperator.OpStatus = et2_Operation.OperationState.OS_STOP_BUYING Then
                        '2024.01.15 : STOP_BUYING 상태인 경우도 취소를 기다리는 상태이기 때문에 아직 추가주문을 하면 안 된다.
                        SafeLeaveTrace(Prethresholders(index).LinkedSymbol.OneKey, 53)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        Continue For
                    Else
                        '140318 : 이미 Operation 객체가 있는 경우 생성하지 않고 추가주문하도록 한다.
                        Dim amount_already_on_order As UInt32 = Prethresholders(index).StockOperator.ThisAmount
                        '2021.04.29: multiple entering 일 때는 ADDITIONAL_ORDER_THRESHOLD가 처음 fall volume에 맞춰져 있을 필요가 있어 number of entering으로 나누는 게 들어갔다.
                        '2021.05.11: fall volume 이 같이 증가하는 것으로 바뀌면서 number of enering 으로 나누는 게 다시 없어졌다.
                        '2021.05.13: 왜 나누는 걸 없앴는지 이해가 안 간다. 다시 넣었다.
                        If final_order_amount > amount_already_on_order / Prethresholders(index).NumberOfEntering * ADDITIONAL_ORDER_THRESHOLD Then
#If ALLOWED_SALE_COUNT Then
                            If IsMA Then
                                AllowedSaleCountFromLastSale -= 1               '한 종목 주문할 때마다 허용된 매매 종목수를 1씩 차감한다.
                            End If
#End If
                            Prethresholders(index).StockOperator.AdditionalOrder(buy_standard_price, final_order_amount)
                            MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " " & "추가주문 / " & buy_standard_price.ToString & " / " & final_order_amount.ToString)
                            rest_of_ordable100 -= final_order_amount * buy_standard_price * Math.Max(Prethresholders(index).LinkedSymbol.EvidanRate / 100, 1)
                            'rest_of_ordable40 -= final_order_amount * buy_standard_price * Math.Max(Prethresholders(index).LinkedSymbol.EvidanRate / 40, 1)
                            'rest_of_ordable30 -= final_order_amount * buy_standard_price * Math.Max(Prethresholders(index).LinkedSymbol.EvidanRate / 30, 1)
                            If Prethresholders(index).ScoreD_DepositBonus = 10 Then
                                over_today += final_order_amount * buy_standard_price
                            End If
                        Else
                            '이미 주문된 수량과 비교해 그다지 큰 비율로의 증가가 아니면 사지 않는다.
                            MessageLogging(Prethresholders(index).LinkedSymbol.Code & " " & AccountCatString & " " & "비율낮아 안 삼 / " & final_order_amount.ToString & " / " & amount_already_on_order.ToString)
                        End If
                    End If

                Else
                    '주문할 수 있는 수량이 0이면 주문하지 않고 바로 포기한다.
                    'EnterPrice = 0      '다시 이리로 들어오지 못하게 진입가를 0으로 만든다.
                    'MessageLogging(AccountCatString & " " & Prethresholders(index).LinkedSymbol.Code & " :" & "주문할 수 없음(주문가능수량 0). 아마도 너무 많은 다른 종목 주문.") 20200621: 불필요 로그 삭제
                End If
                SafeLeaveTrace(Prethresholders(index).LinkedSymbol.OneKey, 52)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            End If
            'If rest_of_ordable_money < 0.3 * pizza_one_piece Then
            '0.3 조각 이하로 남았을 때는 그냥 나간다.
            'Exit For
            'End If
        Next
#If ALLOWED_SALE_COUNT Then
        If IsMA Then
            If AllowedSaleCountFromLastSale >= 0 Then
                '허용이 되었는데 AllowedSaleCountFromLastSale이 감소하지 않았다는 것은 살 돈이 없었다는 것이므로 아쉽지만 이번에 허용된 카운트는 누적되지 않도록 버린다.
                AllowedSaleCountFromLastSale = 0
            Else
                '허용 카운트가 0보다 작다는 것은, 예를 들어 0.25 에서 한 개 종목 매수해서 -0.75가 된 케이스이고 0이상이 될 때까지 몇 분 더 기다려야 할 것이다.
            End If
        End If
#End If
#If 0 Then
        SafeEnterTrace(OrdableKey, 40)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Ordable100 = Math.Max(rest_of_ordable100, 0)
        'Ordable40 = Math.Max(rest_of_ordable40, 0)
        'Ordable30 = Math.Max(rest_of_ordable30, 0)
        SafeLeaveTrace(OrdableKey, 41)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
#End If
    End Sub

    Public Function GetUnitPrice(ByVal current_price As UInt32, ByVal market_kind As MARKET_KIND) As UInt32
        Dim unit_price As UInt32 = 0

        '2023.10.24 : 올해 1월부로 호가단위가 더 촘촘하게 바뀌었다. 코스닥 코스피 구분도 없어졌다.
        'If market_kind = PriceMiner.MARKET_KIND.MK_KOSPI Then
        If current_price < 2000 Then
            unit_price = 1
        ElseIf current_price < 5000 Then
            unit_price = 5
        ElseIf current_price < 20000 Then
            unit_price = 10
        ElseIf current_price < 50000 Then
            unit_price = 50
        ElseIf current_price < 200000 Then
            unit_price = 100
        ElseIf current_price < 500000 Then
            unit_price = 500
        Else
            unit_price = 1000
        End If
        'Else 'If KOSDAQ Then
        'If current_price < 1000 Then
        'unit_price = 1
        'ElseIf current_price < 5000 Then
        'unit_price = 5
        'ElseIf current_price < 10000 Then
        'unit_price = 10
        'ElseIf current_price < 50000 Then
        'unit_price = 50
        'Else
        'unit_price = 100
        'End If
        'End If

        Return unit_price
    End Function

    Private Function PrethresholderComparer(ByVal x_decision_maker As c050_DecisionMaker, ByVal y_decision_maker As c050_DecisionMaker) As Integer
        '점수 높은 것이 위에 올라오게 소팅되도록 한다.
        If x_decision_maker.Score > y_decision_maker.Score Then
            Return -1
        ElseIf x_decision_maker.Score = y_decision_maker.Score Then
            Return 0
        Else
            Return 1
        End If
    End Function

    '140407 : Order Number key 관련 정리해보자
    Public Function OrderDealCheck(ByVal request_number As ULong, ByVal operator_obj As et21_Operator, ByRef deal_price As Double, ByRef rest_amount As UInt32) As ORDER_CHECK_REQUEST_RESULT
        SafeEnterTrace(OrderNumberListKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'check if the request number is already in the deal list

        'check deal list to check the deal is already confirmed
        Dim found_index As Integer = -1
        For index As Integer = 0 To DealNumberList.Count - 1
            If DealNumberList(index) = request_number Then
                found_index = index
                Exit For
            End If
        Next
        'If DealNumberList.Contains(request_number) Then
        If found_index <> -1 Then
            'Deal is already done
            If DealPriceList(found_index) = 0 Then
                '주문 거부되었음
                SafeLeaveTrace(OrderNumberListKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Return ORDER_CHECK_REQUEST_RESULT.OCRR_DEAL_REJECTED
            Else
                '체결된 주문에 의해 업데이트된 호가잔량을 반영한다.
                'operator_obj.ParentOperation.LinkedSymbol.UpdateMyOrder(Me, DealNumberList(found_index), RestAmountList(found_index))

                If RestAmountList(found_index) = 0 Then
                    '모두 체결되었음
                    deal_price = DealPriceList(found_index)     '평균체결가 넘겨줌
                    rest_amount = RestAmountList(found_index)   '미체결수량 넘겨줌

                    DealNumberList.Remove(request_number)       '딜넘버리스트에서 지움
                    orderOperationList.RemoveAt(found_index)    '주문자 리스트에서 지움 
                    DealPriceList.RemoveAt(found_index)        '체결가리스트에서 지움
                    RestAmountList.RemoveAt(found_index)        '미체결수량 삭제
                    NothingDebugWhere.RemoveAt(found_index)
                    NothingDebugTime.RemoveAt(found_index)
                    'CancelNumberList.RemoveAt(found_index)        '취소넘버리스트에서 지움
                    SafeLeaveTrace(OrderNumberListKey, 12)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return ORDER_CHECK_REQUEST_RESULT.OCRR_DEAL_CONFIRMED_COMPLETELY
                Else
                    '일부 체결되었음
                    deal_price = DealPriceList(found_index)     '평균체결가 넘겨줌
                    rest_amount = RestAmountList(found_index)   '미체결수량 넘겨줌
                    '2020.03.05: nothing을 감지 error message를 타는 이유를 찾은 것 같다. 주문들어가고나서 일부수량에 대해 SC1 발생->주문확인 발생 -> 나머지 일부 수량에 대해 SC1 발생
                    ' 이러는 경우 orderOperationList에 nothing 이 먼저 등록되지만 위의 모두 체결이 아니고 여기 일부 체결로 들어온다.
                    ' 근데 여기서 nothing을 해당 operator 객체로 바꿔줘야 다음 나머지 SC1 체결 때 정상적으로 남은 수량 업데이트 루틴이 타질 것 같다.
                    ' 그래서 아래 라인 한 줄을 추가했다.
                    orderOperationList(found_index) = operator_obj
                    SafeLeaveTrace(OrderNumberListKey, 13)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return ORDER_CHECK_REQUEST_RESULT.OCRR_DEAL_CONFIRMED_PARTIALLY
                End If
            End If
        End If

        '해당 주문은 아직 체결 confirm이 오지 않았다 => deal Number List와 Order Operation List에 기록해두고 기다림
        DealNumberList.Add(request_number)
        orderOperationList.Add(operator_obj)
        DealPriceList.Add(0)
        RestAmountList.Add(rest_amount)
        NothingDebugWhere.Add(1)
        NothingDebugTime.Add(Now)
        'CancelNumberList.Add(0)                     '취소넘버리스트도 같이 관리됨
        SafeLeaveTrace(OrderNumberListKey, 14)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return ORDER_CHECK_REQUEST_RESULT.OCRR_PLEASE_WAIT
    End Function
    '----------------------------------------------------------------------------------------------------------------------------┘

    'request account info
    Public Function RequestAccountInfo_CSPAQ12200() As Integer
        'Inblock setting
        _CSPAQ12200.ResFileName = "Res\CSPAQ12200.res" '181102:이거 하다가 access violation 남 뭐지
        _CSPAQ12200.SetFieldData("CSPAQ12200InBlock1", "RecCnt", 0, "1")        '레코드 갯수
        _CSPAQ12200.SetFieldData("CSPAQ12200InBlock1", "MgmtBrnNo", 0, "")          '관리지점 번호
        '_CSPAQ12200.SetFieldData("CSPAQ12200InBlock1", "AcntNo", 0, AccountString)    '계좌번호
        _CSPAQ12200.SetFieldData("CSPAQ12200InBlock1", "AcntNo", 0, _AccountString)    '계좌번호
        _CSPAQ12200.SetFieldData("CSPAQ12200InBlock1", "Pwd", 0, _PasswordString)               '비밀번호
        _CSPAQ12200.SetFieldData("CSPAQ12200InBlock1", "BalCreTp", 0, "0")          '잔고생성구분

        Dim req_id As Integer = _CSPAQ12200.Request(False)
        If req_id < 0 Then
            '조회실패
            ErrorLogging("계좌조회에~ 실패했습니다(CSPAQ12200). 계좌번호 " & _AccountString & ", 에러코드 " & req_id.ToString)
            Return -1
        Else
            Return 0
        End If
    End Function

    Public Function RequestAccountInfo_CSPAQ22200() As Integer
        'Inblock setting
        _CSPAQ22200.ResFileName = "Res\CSPAQ22200.res"
        _CSPAQ22200.SetFieldData("CSPAQ22200InBlock1", "RecCnt", 0, "1")        '레코드 갯수
        _CSPAQ22200.SetFieldData("CSPAQ22200InBlock1", "MgmtBrnNo", 0, "")          '관리지점 번호
        _CSPAQ22200.SetFieldData("CSPAQ22200InBlock1", "AcntNo", 0, _AccountString)    '계좌번호
        _CSPAQ22200.SetFieldData("CSPAQ22200InBlock1", "Pwd", 0, _PasswordString)               '비밀번호
        _CSPAQ22200.SetFieldData("CSPAQ22200InBlock1", "BalCreTp", 0, "0")          '잔고생성구분

        Dim req_id As Integer = _CSPAQ22200.Request(False)
        If req_id < 0 Then
            '조회실패
            ErrorLogging("계좌조회에~ 실패했습니다(CSPAQ22200). 계좌번호 " & _AccountString & ", 에러코드 " & req_id.ToString)
            Return -1
        Else
            Return 0
        End If
    End Function

    Private Sub _CSPAQ12200_ReceiveData(ByVal szTrCode As String) Handles _CSPAQ12200.ReceiveData
        'TestOutBlock(0) = _CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "BalEvalAmt", 0)       '현금주문가능금액
        'TestOutBlock(1) = _CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "RcvblAmt", 0)       '현금주문가능금액
        'TestOutBlock(2) = _CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "DpsastTotamt", 0)       '예탁자산총액
        'TestOutBlock(3) = _CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "SubstOrdAbleAmt", 0)       '대용주문가능금액 <= 주목해야할 녀석이다. 주문하면 증거금 만큼 마이너스값이 점점 커진다.
        'TestOutBlock(4) = _CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "Dps", 0)       '현금주문가능금액
        'TestOutBlock(5) = _CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "SubstAmt", 0)       '현금주문가능금액
        Dim ordable_money = Convert.ToInt64(_CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "MnyOrdAbleAmt", 0))    '현금주문가능금액
        Dim ordable_100 = Convert.ToUInt64(_CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "MgnRat100pctOrdAbleAmt", 0)) '증거금 100% 주문가능금액
        Dim ordable_40 = Convert.ToUInt64(_CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "MgnRat50ordAbleAmt", 0)) '증거금 50% 주문가능금액      '2024.01.11 : 50로 요청하지만 들어오는 것은 40이다. 버그다.
        Dim ordable_30 = Convert.ToUInt64(_CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "MgnRat35ordAbleAmt", 0)) '증거금 30% 주문가능금액      '2024.01.11 : 35로 요청하지만 들어오는 것은 30이다. 버그다.
        Try
            TotalProperty = Convert.ToUInt64(_CSPAQ12200.GetFieldData("CSPAQ12200OutBlock2", "DpsastTotamt", 0))               '
        Catch ex As Exception
            TotalProperty = 0
        End Try
        SafeEnterTrace(OrdableKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim diff_factor As Double = 0
        If TotalProperty <> 0 Then
            diff_factor = (CType(ordable_100, Double) - Ordable100) / TotalProperty
        End If
        Ordable100 = ordable_100
        SafeLeaveTrace(OrdableKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'MessageLogging(AccountCatString & " " & "현금주문가능금액 : " & TestOutBlock(3) & " 원")
        MessageLogging("계좌공통 " & AccountCatString & " CSPAQ12200 " & "증거금100주문가능금액 : " & ordable_100 & ", 증거금40주문가능금액 : " & ordable_40 & ", 증거금30주문가능금액 : " & ordable_30 & " 원, 예탁자산총액 :" & TotalProperty & " 원, 현금주문가능금액 :" & ordable_money)
        If Math.Abs(diff_factor) > 0.02 Then
            MessageLogging("계좌공통 " & AccountCatString & " CSPAQ12200 " & "diff_factor 왤케차이나 " & diff_factor.ToString)
        End If
    End Sub

    Private Sub _CSPAQ22200_ReceiveData(ByVal szTrCode As String) Handles _CSPAQ22200.ReceiveData
        'Dim ordable_money = Convert.ToInt64(_CSPAQ22200.GetFieldData("CSPAQ22200OutBlock2", "MnyOrdAbleAmt", 0))    '현금주문가능금액
        Dim ordable_100 = Convert.ToUInt64(_CSPAQ22200.GetFieldData("CSPAQ22200OutBlock2", "MgnRat100pctOrdAbleAmt", 0)) '증거금 100% 주문가능금액
        SafeEnterTrace(OrdableKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim diff_factor As Double = 0
        If TotalProperty <> 0 Then
            diff_factor = (CType(ordable_100, Double) - Ordable100) / TotalProperty
        End If
        Ordable100 = ordable_100
        SafeLeaveTrace(OrdableKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        MessageLogging("계좌공통 " & AccountCatString & " CSPAQ22200 " & "증거금100주문가능금액 : " & ordable_100)
        If Math.Abs(diff_factor) > 0.02 Then
            MessageLogging("계좌공통 " & AccountCatString & " CSPAQ22200 " & "diff_factor 왤케차이나 " & diff_factor.ToString)
        End If
    End Sub

    Public Sub XAReal_SC0_ReceiveRealData_PostThread(ByVal parameters As Object())
        Dim short_code As String = parameters(0) '종목코드
        Dim buy_money As UInt64 = parameters(1) '매수주문금액

        '2024.01.13 : 주문가능금액 업데이트
        Dim the_symbol = SymbolSearchService(short_code)
        If the_symbol Is Nothing Then
            '2024.04.01 : ebest 앱에서 팔았는데 symbol list 에는 없어서 익셉션 난 case가 있다. ebest 에서는 매매가 허용되었는데 대신증권에서는 허용 안 되는 종목이 있는 것도 같다.
            Exit Sub
        End If
        Dim evidan_rate As Integer = the_symbol.EvidanRate
        SafeEnterTrace(OrdableKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If Ordable100 > buy_money * evidan_rate / 100 Then
            Ordable100 -= buy_money * evidan_rate / 100
        Else
            Ordable100 = 0
        End If
        SafeLeaveTrace(OrdableKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '130703:아래 주문 체결 확인 부터 한 번 보자.
    Private OrderNumberForSearching As ULong
    Public Sub XAReal_SC1_ReceiveRealData_PostThread(ByVal parameters As Object())
        Dim order_number As ULong = parameters(0) '주문번호 추출
        Dim deal_price As Double = parameters(1)   '주문평균체결가격 추출
        Dim rest_amount As UInt32 = parameters(2)    '미체결수량(주문) 추출
        Dim deal_amount As UInt32 = parameters(3)    '체결수량 추출
        Dim short_code As String = parameters(4)   '종목코드
        Dim sel_money_back As UInt32 = parameters(5)    '매도체결금액

        '2024.01.13 : 주문가능금액 업데이트
        Dim the_symbol = SymbolSearchService(short_code)
        If the_symbol Is Nothing Then
            '2024.04.01 : ebest 앱에서 팔았는데 symbol list 에는 없어서 익셉션 난 case가 있다. ebest 에서는 매매가 허용되었는데 대신증권에서는 허용 안 되는 종목이 있는 것도 같다.
            Exit Sub
        End If
        Dim evidan_rate As Integer = the_symbol.EvidanRate
        SafeEnterTrace(OrdableKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Ordable100 += sel_money_back * evidan_rate / 100
        SafeLeaveTrace(OrdableKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        While CALLBACK_FAST_RETURN_WAIT AndAlso XAReal_SC1_ReceiveRealData_Thread IsNot Nothing AndAlso XAReal_SC1_ReceiveRealData_Thread.ThreadState <> Threading.ThreadState.Running
            Threading.Thread.Yield()
        End While

        If deal_price = 0 Then
            ErrorLogging("아니 SC1 에서 deal_price가 0이라니 이럴 수도 있어요?")
        End If

        '130626: Critical zone에서 접근되는 전역변수들 하나하나 다 조사해야 한다
        SafeEnterTrace(OrderNumberListKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        OrderNumberForSearching = order_number          'FindIndex 를 위한 전역변수 세팅
        Dim the_order_index As Integer = DealNumberList.FindIndex(AddressOf FindIndexForOrder)         '주문번호리스트에 해당 주문 있나 확인
        If the_order_index >= 0 Then
            If the_order_index <= orderOperationList.Count - 1 AndAlso orderOperationList(the_order_index) IsNot Nothing Then
                If orderOperationList(the_order_index).ParentOperation IsNot Nothing Then
                    '2019년 3월4일9시7분발생; 리스트 개수3개인데 알맹이는 다 nothing임
                    '2019년 3월21일9시3분에도 비슷하게 발생
                    '2019년 6월25일10시40분경에도 비슷하게 발생 (리스트 갯수 2개인데 2번째 알맹이가 없음.)
                    '190626: 이 list에 nothing을 추가하는 경우도 있으니 이렇게 뭔가가 있을 거라고 가정하는 코드 다시 살펴봐야 함.
                    MessageLogging(orderOperationList(the_order_index).ParentOperation.LinkedSymbol.Code & " " & AccountCatString & " SC1 메시지 도착. ")
                    '기다리던 주문체결 확인이 들어왔음
                    Dim the_operator As et21_Operator = orderOperationList(the_order_index)


                    SafeLeaveTrace(OrderNumberListKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

                    '체결된 주문에 의해 업데이트된 호가잔량을 반영한다.
                    'the_operator.ParentOperation.LinkedSymbol.UpdateMyOrder(Me, order_number, rest_amount)

                    '140411 : 여기다! 여기서 일부만 이루어졌더라도 decision maker에 통보해서 상태변경을 시켜야 한다.
                    '140414 : Operator -> Operation으로 가는 새로운 이벤트를 만들어야 한다.그렇지 않으면 decision maker까지 전달될 방법이없다.
                    '190214: 2월13일 미청산 건 하나의 원인은 deal_price가 0으로 들어왔기 때문이라고 밖에 설명할 수 없음.
                    '20200423: 취소주문 후 전체 3개 수량중 1개에 대한 체결 SC1이 먼저 들어오고 (미체결 0, 체결 1), 그 후 SC3이 취소수량 2로 들어옴
                    '20200423: 이런 경우 rest_amount 를 0으로 아래함수를 콜하면 3개 수량 모두 체결된 것으로 간주하게 됨.
                    '20200423: 이 문제를 해결하기 위해 rest_amount 를 아래와 같이 보정
                    '2020.09.24: OrderNumberListKey와 OperationKey를 분리한다. 먹통되는 버그 한 번 재현되어 그에 대한 fix다.
                    '2020.09.24: RestAmount 를 OperationKey로 커버가 불가능하다. RestAmount용 키만 별도로 만들자. 좀 웃기긴 하지만 이것밖에 더 좋은 방법이 생각이 안 난다.

                    '2024.03.05 : 언제부터인지 rest_amount가 parameter로 넘어오면서 rest_amount 를 계산할 필요가 없어진 것이다.
#If 0 Then
                    If the_operator.RestAmount >= deal_amount Then
                        rest_amount = the_operator.RestAmount - deal_amount
                    Else
                        ErrorLogging(orderOperationList(the_order_index).ParentOperation.LinkedSymbol.Code & " " & AccountCatString & " 남은 수량이 마이너스 " & orderOperationList(the_order_index).ParentOperation.SymbolCode)
                        rest_amount = 0
                    End If
#End If

                    If rest_amount = 0 Then
                        '체결이 전부 이루어졌다면
                        DealNumberList.RemoveAt(the_order_index)           '해당주문번호 삭제
                        orderOperationList.RemoveAt(the_order_index)        '해당오퍼레이션객체 삭제
                        DealPriceList.RemoveAt(the_order_index)             '평균체결가 리스트에서 삭제
                        RestAmountList.RemoveAt(the_order_index)            '미체결수량 리스트에서 삭제
                        NothingDebugWhere.RemoveAt(the_order_index)
                        NothingDebugTime.RemoveAt(the_order_index)
                        'CancelNumberList.RemoveAt(the_order_index)          '취소넘버리스트에서 삭제
                    Else
                        '체결이 일부만 이루어졌다면
                        DealPriceList(the_order_index) = deal_price
                        RestAmountList(the_order_index) = rest_amount       '미체결수량만 업데이트함
                    End If
                    '2024.03.04 : 순식간에 여러개의 매수체결이벤트가 들어오는 경우, 다수의 thread가 위의 로직 수행후 여기서 펜딩상태가 된다.
                    '2024.03.04 : 이 때 위에서 계산된 rest_amount 가 the_operator.RestAmount 에 업데이트되는 시점이 아래 로직이기 때문에 다른 thread 에서 업데이트 이전의 the_operator.RestAmount 가 사용되는 오류가 발생한다.
                    '2024.03.04 : 이것은 결과적으로 전체체결 후에도 the_operator.RestAmount 가 0 보다 크게 남는 현상으로 나타나고, 이 허구의 수량을 취소할 때 주문번호가 공백이 되어 결국 process가 stuck 된다.
                    '2024.03.05 : 이것은 위에 rest_amount 계산로직을 없애는 걸로 해결하기로 했다.다수의 thread 가 순서가 전도되어 펜딩상태가 되더라도, 어쨌든 rest_amount 가 가장 작은 녀석이 마지막에 체결된 걸로 인식되어
                    '2024.03.05 : 해당 데이터는 order number list 로부터 삭제될 것이다. 순서가 바뀌어 먼저 체결되었지만 나중에 들어온 녀석들은 아래 else case "근데 너무 일찍 들어왔는데" 로 들어오지만 기록만 되고
                    '2024.03.05 : operator 객체도 없기 때문에 이후 아무 영향도 미치지 않을 것이다. 아래 ReceiveDealInitiated 에서는 Math.Min 으로 방어로직이 이미 구현되어 있다. 서비스로 ReceiveDealConfirmed 의 두번째 파라미터는 미사용으로 삭제처리했다.
                    SafeEnterTrace(the_operator.ParentOperation.OperationKey, 115)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    the_operator.ReceiveDealInitiated(deal_price, rest_amount)         '해당 오퍼레이션 객체에 체결확인 notification 보냄
                    If rest_amount = 0 Then
                        the_operator.ReceiveDealConfirmed(deal_price)         '해당 오퍼레이션 객체에 체결확인 notification 보냄
                    End If
                    SafeLeaveTrace(the_operator.ParentOperation.OperationKey, 121)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Else
                    ErrorLogging(AccountCatString & " Parent operation이 없다니 어떻게 이럴수가?")
                    SafeLeaveTrace(OrderNumberListKey, 23)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If
            Else
                If orderOperationList(the_order_index) Is Nothing Then
                    '2020.12.18: 이날 13:47:33 경, A028080 종목 20주 매수주문의 주문확인이 들어오기 전 10주씩 체결확인이 연달아 들어와 결국 이쪽 루트를 타게 됨.
                    '2020.12.18: 처음10주는 아래 "근데 너무 일찍..." 여기로 들어오고 두번째10주 때 이쪽으로 들어옴.
                    '2020.12.18: 아마도 여기에서 RestAmount 등 업데이트하고 주문확인이 늦게나마 올 때 OrderDealCheck 안에서 처리하는 것이 맞을 듯 하다.
                    DealPriceList(the_order_index) = deal_price               '평균체결가 업데이트
                    RestAmountList(the_order_index) = rest_amount             '미체결수량 업데이트

                    'ErrorLogging(AccountCatString & " nothing을 감지?")   '2020.12.21 이날부로 에러 해제
                    MessageLogging("NothingDebugWhere : " & NothingDebugWhere(the_order_index).ToString & ", NothingDebugTime : " & NothingDebugTime(the_order_index).TimeOfDay.ToString)
                Else
                    ErrorLogging(AccountCatString & " operator list 갯수보다 큰 게 감지되다니 어떻게 이럴수가?")
                End If
                SafeLeaveTrace(OrderNumberListKey, 24)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            End If
        Else
            '주문 체결이 너무 일찍 들어왔음
            MessageLogging("계좌공통" & AccountCatString & " SC1 메시지 도착. 근데 너무 일찍 들어왔는데.. 아니면 순서 뒤바뀌어 너무 늦게 들어와 operator 객체가 이미 삭제된 경우이거나..")
            DealNumberList.Add(order_number)                '딜 넘버리스트에 기록해둠
            orderOperationList.Add(Nothing)             '아직 주문자객체가 없으므로 nothing을 채워넣어야 함.
            DealPriceList.Add(deal_price)               '평균체결가 기록
            RestAmountList.Add(rest_amount)             '미체결수량 기록
            NothingDebugWhere.Add(2)
            NothingDebugTime.Add(Now)

            'CancelNumberList.Add(0)                     '취소넘버리스트에 기록해둠
            SafeLeaveTrace(OrderNumberListKey, 25)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End If
    End Sub

    Public Sub XAReal_SC3_ReceiveRealData_PostThread(ByVal parameters As Object())
        Dim order_number As ULong = parameters(0)
        Dim original_order_number As ULong = parameters(1)
        Dim deal_price As Double = parameters(2)
        Dim rest_amount As UInt32 = parameters(3)
        Dim short_code As String = parameters(4)
        Dim canceled_money As UInt64 = parameters(5)
        'OrdableMoney = parameters(4)

        While CALLBACK_FAST_RETURN_WAIT AndAlso XAReal_SC3_ReceiveRealData_Thread IsNot Nothing AndAlso XAReal_SC3_ReceiveRealData_Thread.ThreadState <> Threading.ThreadState.Running
            Threading.Thread.Yield()
        End While

        '2024.01.13 : 주문가능금액 업데이트
        Dim the_symbol = SymbolSearchService(short_code)
        If the_symbol Is Nothing Then
            '2024.04.01 : ebest 앱에서 팔았는데 symbol list 에는 없어서 익셉션 난 case가 있다. ebest 에서는 매매가 허용되었는데 대신증권에서는 허용 안 되는 종목이 있는 것도 같다.
            Exit Sub
        End If
        Dim evidan_rate As Integer = the_symbol.EvidanRate
        SafeEnterTrace(OrdableKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Ordable100 += canceled_money * evidan_rate / 100
        SafeLeaveTrace(OrdableKey, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        SafeEnterTrace(OrderNumberListKey, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        OrderNumberForSearching = original_order_number          'FindIndex 를 위한 전역변수 세팅
        Dim the_order_index As Integer = DealNumberList.FindIndex(AddressOf FindIndexForOrder)         '원주문번호리스트에 해당 주문 있나 확인
        If the_order_index >= 0 Then
            '주문취소 확인이 들어왔음. 제 때 들어온 건지 너무 일찍 들어온건지 체크는 operator에서 할 것임
            Dim the_operation As et21_Operator = orderOperationList(the_order_index)

            DealNumberList.Remove(original_order_number)       '딜넘버리스트에서 지움
            orderOperationList.RemoveAt(the_order_index)    '주문자 리스트에서 지움 
            DealPriceList.RemoveAt(the_order_index)        '체결가리스트에서 지움
            RestAmountList.RemoveAt(the_order_index)        '미체결수량 삭제
            NothingDebugWhere.RemoveAt(the_order_index)
            NothingDebugTime.RemoveAt(the_order_index)
            'CancelNumberList.RemoveAt(the_order_index)        '체결가리스트에서 지움
            SafeLeaveTrace(OrderNumberListKey, 31)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If the_operation IsNot Nothing Then
                '체결된 주문에 의해 업데이트된 호가잔량을 반영한다.
                'the_operation.ParentOperation.LinkedSymbol.UpdateMyOrder(Me, original_order_number, 0)

                'DealNumberList.RemoveAt(the_order_index)           '해당주문번호 삭제
                'orderOperationList.RemoveAt(the_order_index)        '해당오퍼레이션객체 삭제
                'CancelNumberList(the_order_index) = order_number                '취소주문번호를 기록함
                SafeEnterTrace(the_operation.ParentOperation.OperationKey, 123)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                the_operation.ReceiveCancelConfirmed(order_number, deal_price, rest_amount)                       '해당 오퍼레이션 객체에 취소확인 notification 보냄
                SafeLeaveTrace(the_operation.ParentOperation.OperationKey, 127)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            End If
        Else
            '원주문 리스트에 취소된 원주문이 없다??
            ErrorLogging(AccountCatString & " 어떤 주문이 취소되었다는 건지 기록에 없음." & original_order_number.ToString)
            SafeLeaveTrace(OrderNumberListKey, 32)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End If
    End Sub

    Public Sub XAReal_SC4_ReceiveRealData_PostThread(ByVal parameters As Object())
        Dim order_number As ULong = parameters(0)

        While CALLBACK_FAST_RETURN_WAIT AndAlso XAReal_SC4_ReceiveRealData_Thread IsNot Nothing AndAlso XAReal_SC4_ReceiveRealData_Thread.ThreadState <> Threading.ThreadState.Running
            Threading.Thread.Yield()
        End While

        SafeEnterTrace(OrderNumberListKey, 40)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        OrderNumberForSearching = order_number          'FindIndex 를 위한 전역변수 세팅
        Dim the_order_index As Integer = DealNumberList.FindIndex(AddressOf FindIndexForOrder)         '주문번호리스트에 해당 주문 있나 확인
        If the_order_index >= 0 Then
            '주문거부 확인이 들어왔음. 원주문 거부인지 취소주문 거부인지, 제 때 들어온 건지 너무 일찍 들어온건지 체크는 operator에서 할 것임
            Dim the_operation As et21_Operator = orderOperationList(the_order_index)
            'OrderNumberList.RemoveAt(the_order_index)           '해당주문번호 삭제
            'orderOperationList.RemoveAt(the_order_index)        '해당오퍼레이션객체 삭제
            '130502:아래함수 구현이 끝났으므로 이 함수 구현을 본격적으로 시작한다.
            SafeLeaveTrace(OrderNumberListKey, 41)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If the_operation IsNot Nothing Then
                SafeEnterTrace(the_operation.ParentOperation.OperationKey, 130)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                the_operation.ReceiveConfirmFailed()                '주문거부이건 취소주문거부이건 거부되었다는 사실을 operation객체에 알려줌
                SafeLeaveTrace(the_operation.ParentOperation.OperationKey, 133)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            End If
        Else
            '20240911 : 오랜만에 SC4 주문거부가 들어왔는데 the_order_index 가 음수가 되어 이쪽으로 들어와 아래에서 orderOperationList 에 음수 인덱스로 접근하여 exception 발생되었다. 아마 최초인 것 같다.
            '20240911 : 덕분에 버그를 발견하여 수정하게 되었음
            '주문거부확인이 너무 일찍 들어왔음
            'MessageLogging(orderOperationList(the_order_index).ParentOperation.LinkedSymbol.Code & AccountCatString & " 근데 너무 일찍 들어왔는데..")
            MessageLogging("unknown_symbol " & AccountCatString & " 근데 너무 일찍 들어왔는데..")
            DealNumberList.Add(order_number)                '딜 넘버리스트에 기록해둠
            orderOperationList.Add(Nothing)             '아직 주문자객체가 없으므로 nothing을 채워넣어야 함.
            DealPriceList.Add(0)               '평균체결가 기록
            RestAmountList.Add(0)             '미체결수량 기록
            NothingDebugWhere.Add(3)
            NothingDebugTime.Add(Now)
            SafeLeaveTrace(OrderNumberListKey, 42)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End If
    End Sub

    Private Function FindIndexForOrder(ByVal order_number_item As ULong) As Boolean
        If order_number_item = OrderNumberForSearching Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub CheckMyStocks()

        _CSPAQ12300.ResFileName = "Res\CSPAQ12300.res"
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "RecCnt", 0, "1")        '레코드 갯수
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "AcntNo", 0, _AccountString)    '계좌번호
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "Pwd", 0, _PasswordString)               '비밀번호
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "BalCreTp", 0, "0")          '잔고생성구분
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "CmsnAppTpCode", 0, "1")          '수수료적용구분
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "D2balBaseQryTp", 0, "0")          'D2잔고기준조회구분
        _CSPAQ12300.SetFieldData("CSPAQ12300InBlock1", "UprcTpCode", 0, "0")          '단가구분

        Dim req_id As Integer = _CSPAQ12300.Request(False)
        If req_id < 0 Then
            '조회실패
            ErrorLogging("계좌 잔고조회에~ 실패했습니다. 계좌번호 " & _AccountString & ", 에러코드 " & req_id.ToString)
        End If
    End Sub

    Private Sub _CSPAQ12300_ReceiveData(szTrCode As String) Handles _CSPAQ12300.ReceiveData
        Dim block_count As Int16 = Convert.ToInt16(_CSPAQ12300.GetBlockCount("CSPAQ12300OutBlock3"))

        'Dim stored_stock As StoredStockType
        Dim symbol_name As String
        Dim symbol_code As String
        Dim total_quantity, sum_from_stored_list As Integer


        SafeEnterTrace(StockListKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        For index As Integer = 0 To block_count - 1
            symbol_code = _CSPAQ12300.GetFieldData("CSPAQ12300OutBlock3", "IsuNo", index)            '종목번호
            symbol_name = _CSPAQ12300.GetFieldData("CSPAQ12300OutBlock3", "IsuNm", index)            '종목이름
            total_quantity = Convert.ToInt32(_CSPAQ12300.GetFieldData("CSPAQ12300OutBlock3", "SellAbleQty", index))            '매도가능수량
            Select Case symbol_code
                Case "A053590" '한국테크놀로지
                    Continue For
                Case "A078130" '국일제지
                    Continue For
                Case "A068940" '셀피글로벌
                    Continue For
                Case "A214870" '뉴지랩파마
                    Continue For
                Case "A217480" '에스디생명공학
                    Continue For
                Case "A101140" '인바이오젠
                    Continue For
                Case "A117670" '알파홀딩스
                    Continue For
                Case "A290380" '대유
                    Continue For
                Case "A044060" '조광ILI
                    Continue For
                Case "A089530" '에이티세미콘
                    Continue For
                Case "A096040" '이트론
                    Continue For
                Case "A001140" '국보
                    Continue For
                Case "A016790" '카나리아바이오
                    Continue For
                Case "A096610" '알에프세미
                    Continue For
                Case "A217620" '디딤이앤에프
                    Continue For
                Case Else

                    'Nothing
            End Select
            'If symbol_code = "A003620" Then
            '쌍용차는 상관하지 말자.
            'Continue For
            'End If
            'stored_stock.AvePrice = Convert.ToDouble(_CSPAQ12300.GetFieldData("CSPAQ12300OutBlock3", "AvrUprc", index))            '평균단가
            'get quantity summation of store stocks
            sum_from_stored_list = 0
            For stock_list_index As Integer = 0 To StoredStockList.Count - 1
                If StoredStockList(stock_list_index).Code = symbol_code Then
                    sum_from_stored_list += StoredStockList(stock_list_index).Quantity
                End If
            Next
            If sum_from_stored_list = total_quantity Then
                'quantity is consistent
                MessageLogging(symbol_code & " " & AccountCatString & " " & symbol_name & " 계좌잔고 갯수가 저장된값과 일치함 : " & total_quantity.ToString)
            Else
                'quantity is inconsistent
                '20220419: 아래의 로깅을 ErrorLogging 에서 WarningLogging으로 바꿨다.
                WarningLogging(symbol_code & " " & AccountCatString & " " & symbol_name & " 잔고가 저장값보다 " & (total_quantity - sum_from_stored_list).ToString & " 많다.")
                MessageLogging(symbol_code & " " & AccountCatString & " " & symbol_name & " 잔고가 저장값보다 " & (total_quantity - sum_from_stored_list).ToString & " 많다.")
            End If
        Next
        'For index As Integer = 0 To StoredStockList.Count - 1
        'MessageLogging("잔고표시 " & StoredStockList(index).Code & ", 갯수" & StoredStockList(index).Quantity & ", 평균단가 " & StoredStockList(index).AvePrice)
        'Next

        SafeLeaveTrace(StockListKey, 11)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '읽은 잔고에서 해당 주식수를 잘라서 리턴해준다
    Public Function TakeMyStock(ByVal symbol_code As String, ByVal ma_base As c05G_MovingAverageDifference.MA_Base_Type) As StoredStockType
        Dim stored_stock As StoredStockType
        stored_stock.Code = ""
        stored_stock.Quantity = 0
        stored_stock.AvePrice = 0
        stored_stock.MA_Base = ""

        SafeEnterTrace(StockListKey, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        For index As Integer = StoredStockList.Count - 1 To 0 Step -1
            If StoredStockList(index).Code = symbol_code AndAlso StoredStockList(index).MA_Base = ma_base.ToString Then
                stored_stock = StoredStockList(index)
                'return_stock_list.Add(StoredStockList(index))
                StoredStockList.RemoveAt(index)
                Exit For
            End If
        Next
        SafeLeaveTrace(StockListKey, 21)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Return stored_stock
    End Function
End Class
