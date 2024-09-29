Imports CandleServiceInterfacePrj

'Compiler options
'180000, ETRADE_CONNECTION : Etrade Xing API ON
'181210, DEBUG_TOP_BOTTOM_PRICE_UPDATE : target pattern의 마지막 price를 Base Price라고 착각한 버그를 수정.
'181210, CHECK_PRE_PATTERN_STRATEGY : 매수성공률을 높이기 위한 방안으로 pre pattern 전략을 연구해보기로 함. =>버그로 인해 수익률 높게 나오는 걸 성공이라고 착각. 결론은 망했음.
'181210, MAKE_MEAN_SAME_BEFORE_GETTING_SCORE : Score 계산 전 pattern과 target의 mean을 같게 만듦
'190127, MOVING_AVERAGE_DIFFERENCE : moving average difference 전략 ON
'190312, NO_SHOW_TO_THE_FORM : 걸린애들 폼에 표시하려니 메모리 부족하여 폼에 표시안하고 대신 리스트에 저장해두기
'190319, ALLOW_MULTIPLE_ENTERING : 진입한 후 가격이 더 떨어지면 추가 매수
'210930, NO_GANGDO_DB : 2021년 10월부터 진행되는 체결강도 저장 안 하는 DB
Imports DSCBO1Lib
'Imports CPUTILLib

'버그 기록
'190514: Main 계좌에 SC0,1,2,3,4 안 불림
'190517: Main 계좌에 SC0,1,2,3,4 안 불림. 아마도 휴대폰으로 팔면 그런 것 같음
'190525: ebest 답변: 안녕하세요 관리자입니다. 서버 담당자와 해당 일자의 접속 및 실시간 발생여부 확인 한 결과 정상적으로 발생 한 것을 확인하였습니다. 주기적으로 발생하는 실시간을 같이 등록하시어 정상적으로 실시간을 받는지 같이 확인이 필요해 보입니다. 이용에 참고하시기 바랍니다. 감사합니다

Public Structure CallPrices
    Dim BuyPrice1 As UInt32
    Dim BuyPrice2 As UInt32
    Dim BuyPrice3 As UInt32
    Dim BuyPrice4 As UInt32
    Dim BuyPrice5 As UInt32
    Dim BuyPrice6 As UInt32
    Dim SelPrice1 As UInt32
    Dim SelPrice2 As UInt32
    Dim SelPrice3 As UInt32
    Dim SelPrice4 As UInt32
    Dim SelPrice5 As UInt32
    Dim SelPrice6 As UInt32
    Dim BuyAmount1 As UInt32
    Dim BuyAmount2 As UInt32
    Dim BuyAmount3 As UInt32
    Dim BuyAmount4 As UInt32
    Dim BuyAmount5 As UInt32
    Dim SelAmount1 As UInt32
    Dim SelAmount2 As UInt32
    Dim SelAmount3 As UInt32
    Dim SelAmount4 As UInt32
    Dim SelAmount5 As UInt32
End Structure

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

Public Enum ORDER_CHECK_REQUEST_RESULT
    OCRR_PLEASE_WAIT
    OCRR_DEAL_CONFIRMED_PARTIALLY
    OCRR_DEAL_CONFIRMED_COMPLETELY
    OCRR_CANCEL_CONFIRMED
    OCRR_DEAL_REJECTED
End Enum

Public Enum EnterOrExit
    EOE_Enter
    EOE_Exit
End Enum

Public Enum RequestFormat
    RF_STOCK_MST2
    RF_MARKET_EYE
End Enum

Module m00_Global
    Public OnlyAFewChanceLeft As UInt32 = 12
    Public Const CALLBACK_FAST_RETURN_WAIT As Boolean = False
    Public Const SC0_CONFIRM_SUPPORT As Boolean = False
    Public Const ACCESS_SIMUL_DB As Boolean = False
    'Public StoredMessagesForDisplay As New List(Of String)
    'Public StoredMessagesKeyForDisplay As TracingKey 'Integer
    Public StoredMessagesMutex As New Threading.Mutex(False, "StoredMessagesMutex")
    Public StoredMessagesForFileSave As New List(Of String)
    Public StoredMessagesForWarning As New List(Of String)
    Public WarningMessageKey As TracingKey
    Public DecisionByPattern As Boolean = True
    Public Const MAX_HAVING_LENGTH As Integer = 500
    Public Const FALL_VOLUME_THRESHOLD As UInt64 = 400000000000
    Public Const MULTIPLE_DECIDER As Boolean = True
    Public Const MAIN_NUMBER_OF_DECIDERS As Integer = 1
    Public Const SUB_NUMBER_OF_DECIDERS As Integer = 9
    Public Const TEST_NUMBER_OF_DECIDERS As Integer = 1
    Public Const MAIN_NUMBER_OF_PIECES As Integer = 1
    Public Const SUB_NUMBER_OF_PIECES As Integer = 2
    Public Const TEST_NUMBER_OF_PIECES As Integer = 2
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
    Public Const MAIN_SILENT_INVOLVING_AMOUNT_RATE As Double = 0.4 '0.0333 / 2         'Moving Average Difference용
    Public Const SUB_SILENT_INVOLVING_AMOUNT_RATE As Double = 0.0333 / 2        'Moving Average Difference용
    Public Const TEST_SILENT_INVOLVING_AMOUNT_RATE As Double = 0.2

    Public PCName As String = "OCTAN"
    Public CANDLE_CHART_FOLDER As String = "D:\Finance\Database\CandleChart\"
    Public DB_FOLDER As String = "D:\Finance\Database\"
    Public PartCount() As Integer = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    Public PartMoney() As Double = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    Public PartKey As TracingKey
#If ALLOW_MULTIPLE_ENTERING Then
    Public Const ADDITIONAL_ORDER_THRESHOLD As Double = 0.5    '2021.04.29: multiple entering 때는 additional order 가능하도록 했다.
#Else
    '2021.03.15:완전 높여서 추가매수 못 이뤄지게 막았다.
    '2023.11.22: 추가매수는 필요하다. MA 계좌의 경우 한조각이 아니라 겨우 1/5 조각을 사놓고 더 못사는 경우가 자주 발생하므로 추가매수를 다시 가능하게 바꿨다.
    Public Const ADDITIONAL_ORDER_THRESHOLD As Double = 0.1
#End If
    Public Const MA_BASE As Integer = 2
    Public Const MAIN_IGNORE_AMOUNT_RATE_FOR_BUY = 0
    Public Const MAIN_IGNORE_AMOUNT_RATE_FOR_SEL = 0.2
    Public Const SUB_IGNORE_AMOUNT_RATE_FOR_BUY = 0.1
    Public Const SUB_IGNORE_AMOUNT_RATE_FOR_SEL = 0.1
    Public Const TEST_IGNORE_AMOUNT_RATE_FOR_BUY = 0
    Public Const TEST_IGNORE_AMOUNT_RATE_FOR_SEL = 0.2
    Public Const MAIN_BUY_POWER = 0.0
    Public Const SUB_BUY_POWER = 0.0
    Public Const TEST_BUY_POWER = 0.0
    Public Const ACCOUNT_TASK_MIN_PERIOD = 600
    Public Const PRETH_DISCOUNT = 0.997

    Public SymTree As New c01_Symtree
    Public SymbolList As New List(Of c03_Symbol)
    Public MainForm As Form1
    Public MarketStartTime As Integer
    Public MarketEndTime As Integer
    Public MarketStartHour As Integer
    Public MarketStartMinute As Integer
    Public MarketEndHour As Integer
    Public MarketEndMinute As Integer
    'Public SimulationDateCollector As New List(Of Date)
    Public MainAccountString As String = "20000410603"
    Public MainAccountPW As String = "7125"
    Public SubAccountString As String = "20133954101"
    Public SubAccountPW As String = "7125"
    Public TestAccountString As String = "20134041801"
    Public TestAccountPW As String = "7125"
    Public LogFileFolder As String = "E:\LogFileFolder"
    Public SubAccount As Boolean = True
    Public ExceptionCount As Integer = 0

    Public slope_stat(6) As Integer

    Public LoggedIn As Boolean = False
    Public OrderNumberListKey As Integer
    'Public CallPriceKey As Integer
    Public StillGettingPrices As Boolean = False        '2021.03.01: Symtree 에서 이쪽으로 옮겨왔다. bunch 에서도 유사시 필요하기 때문이다.
    Public GettingPricesKey As TracingKey                     '2021.03.01: 이것도 같이 왔다.
    Public TraceVar As Integer
    Public DebugMsgKey As TracingKey
    Public MaxNumberOfRequest As Integer
    Public TheRequestFormat As RequestFormat
    Public OrderConfirmEventCount As Integer = 0
    Public OrderConfirmEventTracingKey As TracingKey
    Public OrderConfirmPostCount As Integer = 0
    Public OrderConfirmPostTracingKey As TracingKey
    Public WarningNotified As Boolean = False
    Public LogLocked As Boolean = False
    Public LogLocked2 As Boolean = False '20211006: Log locked 되었을 때 어디서 걸리는지 디버깅을 위해 넣어놓는다.
    Public LogDebugFlag1 As Integer = 0
    Public StoredMessagesMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public CpStuckMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public CpStuckAccessor As System.IO.MemoryMappedFiles.MemoryMappedViewAccessor
    Public CybosBasicDataMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public CybosBasicDataMutex As Threading.Mutex
    Public CybosRealDataMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public CybosRealDataMutex As Threading.Mutex
    Public CybosCandleStoreMMF As System.IO.MemoryMappedFiles.MemoryMappedFile

    'Public Const SYMBOL_CODE_LENGTH As Long = 12
    'Public Const SYMBOL_NAME_LENGTH As Long = 20
    Public Const SYMBOL_BASIC_START_OFFSET As Long = 28
    Public Const SYMBOL_BASIC_DATA_SIZE As Long = 60
    Public Const SYMBOL_REAL_START_OFFSET As Long = 8
    Public Const SYMBOL_REAL_DATA_SIZE As Long = 20

    Public DebugFunctionReachCount1 As Integer = 0
    Public DebugFunctionReachCount2 As Integer = 0
    Public DebugPrethresholdCount_M As Integer = 0
    Public DebugPrethresholdCount_T As Integer = 0
    Public DebugNeedLogicCheck As Boolean = False
    Public DebugTotalGullin_M As Integer = 0
    Public DebugTotalGullin_T As Integer = 0
    Public DebugPrethresholdGullin_M As Integer = 0
    Public DebugPrethresholdGullin_T As Integer = 0

    'Public CandleServiceCenter As CandleServiceInterfacePrj.ICandleService

#If CHECK_PRE_PATTERN_STRATEGY Then
    Public CountPostPatternFail As Integer = 0
#End If
    Public MessageMutexErrorCount As Integer = 0

    Public Sub ErrorLogging(ByVal error_message As String)
        'SafeEnterTrace(StoredMessagesKeyForDisplay, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If error_message.Length > 100000 Then
            error_message = "에러 메시지 너무 큰 에러"
        End If
        Dim text_to_display As String = "! " & Now.TimeOfDay.ToString & " : " & error_message & vbCrLf
        'StoredMessagesForDisplay.Add(text_to_display)
        If StoredMessagesMutex IsNot Nothing Then
            Dim mutex_wait_result As Boolean = False

            Try
                mutex_wait_result = StoredMessagesMutex.WaitOne(1000)
            Catch ex As Exception
                'mutex error 일 경우 이렇게 될 수 있다. 가능성은 희박하지만.
                StoredMessagesMutex.ReleaseMutex()
                LogLocked = True
                LogDebugFlag1 += 1
            End Try

            If mutex_wait_result Then
                If MessageMutexErrorCount > 0 Then
                    text_to_display = "! 메시지 잘라먹음 횟수 " & MessageMutexErrorCount.ToString & " " & text_to_display
                    MessageMutexErrorCount = 0
                    LogLocked = False
                    LogLocked2 = False
                End If
                Dim text_to_send As String = text_to_display
                Dim message_buffer As Byte() = Text.Encoding.UTF8.GetBytes(text_to_send)

                Dim stored_messages_access = StoredMessagesMMF.CreateViewAccessor(0, 100000)
                Dim current_length As UInt32 = stored_messages_access.ReadUInt64(0)     '현재 lenth 읽음
                stored_messages_access.WriteArray(Of Byte)(8 + current_length, message_buffer, 0, message_buffer.Length)    '새 message 저장
                stored_messages_access.Write(0, current_length + message_buffer.Length)   '새 lenth 저장
                StoredMessagesMutex.ReleaseMutex()
            Else
                'CybosLauncher 에서 너무 오래 잡고 있거나 하면 이렇게 된다.
                LogLocked = True
                LogLocked2 = True
                MessageMutexErrorCount += 1
            End If
        End If
        'SafeLeaveTrace(StoredMessagesKeyForDisplay, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub MessageLogging(ByVal message As String)
        'SafeEnterTrace(StoredMessagesKeyForDisplay, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If message.Length > 100000 Then
            message = "일반 메시지 너무 큰 메시지"
        End If
        Dim text_to_display As String = "- " & Now.TimeOfDay.ToString & " : " & message & vbCrLf
        'StoredMessagesForDisplay.Add(text_to_display)
        If StoredMessagesMutex IsNot Nothing Then
            Dim mutex_wait_result As Boolean = False

            Try
                mutex_wait_result = StoredMessagesMutex.WaitOne(1000)
            Catch ex As Exception
                'mutex error 일 경우 이렇게 될 수 있다. 가능성은 희박하지만.
                StoredMessagesMutex.ReleaseMutex()
                LogLocked = True
                LogDebugFlag1 += 1
            End Try

            If mutex_wait_result Then
                If MessageMutexErrorCount > 0 Then
                    text_to_display = "! 메시지 잘라먹음 횟수 " & MessageMutexErrorCount.ToString & " " & text_to_display
                    MessageMutexErrorCount = 0
                    LogLocked = False
                    LogLocked2 = False
                End If
                Dim text_to_send As String = text_to_display
                Dim message_buffer As Byte() = Text.Encoding.UTF8.GetBytes(text_to_send)

                Dim stored_messages_access = StoredMessagesMMF.CreateViewAccessor(0, 100000)
                Dim current_length As UInt32 = stored_messages_access.ReadUInt64(0)     '현재 lenth 읽음
                '2021.11.30: 어제 System.Threading.AbandonedMutexException 익셉션 난 것이 아무래도 log size가 한계보다 커져서 문제가 생기는 것 같아서
                '2021.11.30: 한계 사이즈를 50000 에서 500000으로 늘리고, 심지어 이것보다 더 큰 경우에는 아래와 같이 중간에 짤라서 기록하기로 했다.
                '2024.08.06: 장시작하자마자 매수 사이트카 발동되고 난리난 날이다. 9시1분에 아래 else 에서 current_length 가 100021인가 나와서 익셉션 걸렸다. 구체적인 원인파악은 하지 못했다.
                If 8 + current_length + message_buffer.Length < 100000 Then
                    stored_messages_access.WriteArray(Of Byte)(8 + current_length, message_buffer, 0, message_buffer.Length)    '새 message 저장
                Else
                    stored_messages_access.WriteArray(Of Byte)(8 + current_length, message_buffer, 0, 99999 - 8 - current_length)    '새 message 저장
                End If

                stored_messages_access.Write(0, current_length + message_buffer.Length)   '새 lenth 저장
                StoredMessagesMutex.ReleaseMutex()
            Else
                'CybosLauncher 에서 너무 오래 잡고 있거나 하면 이렇게 된다.
                LogLocked = True
                LogLocked2 = True
                MessageMutexErrorCount += 1
            End If
        End If
        'SafeLeaveTrace(StoredMessagesKeyForDisplay, 31)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub WarningLogging(ByVal message As String)
        SafeEnterTrace(WarningMessageKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        StoredMessagesForWarning.Add("- " & Now.TimeOfDay.ToString & " : " & message & vbCrLf)
        SafeLeaveTrace(WarningMessageKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
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
    Public SafeEnterLeaveLog As New List(Of TracingKey)
    Public Sub SafeEnterTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        While System.Threading.Interlocked.CompareExchange(tracing_key.Key, 1, 0) >= 1
            'Thread간 공유문제 발생예방
            'System.Threading.Thread.Yield()
            System.Threading.Thread.Sleep(1)
        End While
        tracing_key.Var = user_index
        tracing_key.Time = Now.TimeOfDay
        If user_index > 1000 Then
            'XingKey 버그 찾기 위함이다.
            If SafeEnterLeaveLog.Count > 200 Then
                SafeEnterLeaveLog.RemoveAt(0)
            End If
            SafeEnterLeaveLog.Add(tracing_key)
        End If
    End Sub

    Public Sub SafeSetTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        System.Threading.Interlocked.Increment(tracing_key.Key)

        tracing_key.Var = user_index
        tracing_key.Time = Now.TimeOfDay
        If user_index > 1000 Then
            'XingKey 버그 찾기 위함이다.
            If SafeEnterLeaveLog.Count > 200 Then
                SafeEnterLeaveLog.RemoveAt(0)
            End If
            SafeEnterLeaveLog.Add(tracing_key)
        End If
    End Sub

    Public Sub SafeLeaveTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        tracing_key.Var = user_index
        System.Threading.Interlocked.Decrement(tracing_key.Key) 'Thread간 공유문제 발생예방
        If user_index > 1000 Then
            'XingKey 버그 찾기 위함이다.
            If SafeEnterLeaveLog.Count > 200 Then
                SafeEnterLeaveLog.RemoveAt(0)
            End If
            SafeEnterLeaveLog.Add(tracing_key)
        End If
        If tracing_key.Key < 0 Then
            Dim a = 1
        End If
    End Sub

    Public Sub GlobalVarInit(ByVal main_form As Form1)
        MainForm = main_form
#If 0 Then
        Dim cp_code_mgr As New CpCodeMgr()

        MarketStartTime = CType(cp_code_mgr.GetMarketStartTime(), Integer)
        MarketEndTime = CType(cp_code_mgr.GetMarketEndTime(), Integer)
        MarketStartHour = MarketStartTime / 100
        MarketStartMinute = MarketStartTime Mod 100
        MarketEndHour = MarketEndTime / 100
        MarketEndMinute = MarketEndTime Mod 100
#End If
    End Sub

    Public ReadOnly Property IsMarketTime() As Boolean
        Get
            Dim current_hour As Integer = Now.Hour
            Dim current_minute As Integer = Now.Minute
            Dim current_time As Integer = 100 * current_hour + current_minute

            If current_time - MarketStartTime >= 0 AndAlso MarketEndTime - current_time > 0 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsTimeToSave() As Boolean
        Get
            Dim current_hour As Integer = Now.Hour
            Dim current_minute As Integer = Now.Minute
            Dim current_time As Integer = 100 * current_hour + current_minute

            If MarketEndTime - current_time < -5 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsClearingTime(ByVal the_time As Date) As Boolean
        Get
            Dim current_time_of_day As TimeSpan = the_time.TimeOfDay
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndHour) + TimeSpan.FromMinutes(MarketEndMinute)

            If current_time_of_day >= market_end_time.Subtract(TimeSpan.FromMinutes(9)) AndAlso market_end_time > current_time_of_day Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsTimeToKeepLastCandle(ByVal the_time As Date) As Boolean
        Get
            Dim current_time_of_day As TimeSpan = the_time.TimeOfDay
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndHour) + TimeSpan.FromMinutes(MarketEndMinute)
            Dim keeptime_start As TimeSpan = market_end_time.Subtract(TimeSpan.FromMinutes(9))
            Dim keeptime_end As TimeSpan = market_end_time.Subtract(TimeSpan.FromMinutes(2))
            '15:21 시작 ~ 15:28 끝 까지 새 candle을 만들지 않는다. 29분 하나가 더 추가될 수 있지만 뭐.. 마지막 주문을 할 수 있게 이렇게 하자.
            '2024.02.19 : 가끔 나오는 미연결잔고의 원인이 이렇게 매도가 불가능한 시간대에 나오는 candle 때문이 아닐까 하는 생각이 들어, 15:21 분부터 끝까지 아예 새 candle 생성을 막아놓겠다. 이를 기점으로 하루 candle 갯수는 381개에서 379개가 될 듯 하다.
            '2024.03.17 : 위에 뭔가 잘못 생각한 듯 하다. 캔들 막는 것은 3시 30분 마지막 캔들을 DB 에 저장하지 못하도록 막는 것을 해야 하고, 15:20 에 생성된 캔들에 따른 매매는 당일날
            '2024.03.17 : 반영이 가능하도록 막지 말아야 한다. 저장되는 candle 갯수는 여기서 정해지는 것이 아니라, Xing 에서 보내주는 데이터에 의해 정해진다.
            '2024.03.17 : 그래서 endtime 은 다시 15:28 으로..


            If current_time_of_day.Hours = market_end_time.Hours AndAlso current_time_of_day.Minutes >= keeptime_start.Minutes AndAlso current_time_of_day.Minutes <= keeptime_end.Minutes Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property IsBadTimeToMakeCandle() As Boolean
        Get
            Dim current_hour As Integer = Now.Hour
            Dim current_minute As Integer = Now.Minute
            Dim current_time As Integer = 100 * current_hour + current_minute

            If current_time - (MarketStartTime - 200) >= 0 AndAlso (MarketEndTime + 150) - current_time > 0 Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

#If 0 Then
    Public ReadOnly Property NoMoreEnteringTime(ByVal the_time As Date, ByVal having_time_in_minutes As Integer) As Boolean ', ByVal having_time_in_seconds As Integer) As Boolean
        Get
            Dim current_time_of_day As TimeSpan = the_time.TimeOfDay
            Dim market_end_time As TimeSpan = TimeSpan.FromHours(MarketEndHour) + TimeSpan.FromMinutes(MarketEndMinute)

            'If current_time_of_day >= market_end_time.Subtract(TimeSpan.FromSeconds(having_time_in_seconds + 12 * 60)) AndAlso market_end_time > current_time_of_day Then
            If current_time_of_day >= market_end_time.Subtract(TimeSpan.FromMinutes(having_time_in_minutes + 10)) AndAlso market_end_time > current_time_of_day Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property
#End If

    Public Function IsGoodTimeToBuy(ByVal enter_time As DateTime) As Boolean
        Dim enter_hour As Integer = enter_time.Hour
        If enter_hour <> 14 Then
            Return True
        Else
            Return False
        End If
    End Function

#If 0 Then
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
#End If

    '넘겨받은 가격을 포함하지 않고 넘겨받은 step수만큼 뛴 호가 계산
    '2023.12.17 : 전에 Operation 과 AccountManager class 에 각각 있던 함수를 공용으로 쓰기 위해 여기로 끌어왔다.
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

    Public Function SymbolSearchService(ByVal symbol_code As String) As c03_Symbol ', ByRef debug_search_count As Integer) As c03_Symbol
        Dim last_symbol_index As Integer = SymbolList.Count - 1
        Dim a As Integer = 0
        Dim b As Integer = last_symbol_index
        Dim position As Integer = (a + b) / 2

        If SymbolList.First.Code = symbol_code Then
            Return SymbolList.First
        ElseIf SymbolList.Last.Code = symbol_code Then
            Return SymbolList.Last
        End If
        'debug_search_count = 2

        '간격이 5 미만으로 줄 때까지 binary search
        While b - a > 5
            If SymbolList(position).Code = symbol_code Then
                '찾았다.
                'debug_search_count += 1
                Return SymbolList(position)
            ElseIf SymbolList(position).Code > symbol_code Then
                'debug_search_count += 2
                '아래쪽 반에 있다.
                b = position
            Else
                'debug_search_count += 2
                '위쪽 반에 있다.
                a = position
            End If
            position = (a + b) / 2
        End While

        '5이하로 줄면 앞에서부터 search
        For index As Integer = a + 1 To b - 1
            If SymbolList(index).Code = symbol_code Then
                'debug_search_count += 1
                Return SymbolList(index)
            End If
        Next

        '여기로 오면 뭔가 이상이 있어서 못 찾았다는 얘기 => 그냥 처음부터 찾는다.
        For index As Integer = 0 To SymbolList.Count - 1
            'debug_search_count += 1
            If SymbolList(index).Code = symbol_code Then
                Return SymbolList(index)
            End If
        Next

        '그래도 못찾았다면 input 이 이상하다는 얘기
        ErrorLogging(symbol_code & " 이런 종목은 없어요")
        Return Nothing
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

#If 1 Then
Public Class CandleChunkList
    Private _ChunkList As New List(Of List(Of CandleStructure))
    Private Const _ChunkSize As Integer = 400
    Private _Count = 0

    Public CandleChunkKey As TracingKey

    Public Sub Initialize(ByVal symbol_index As Integer)
        _Count = 0
        ClearCandle()
    End Sub

    Public Sub ClearCandle()
        For index As Integer = 0 To _ChunkList.Count - 1
            _ChunkList.Item(index).Clear()
        Next
        _ChunkList.Clear()
    End Sub

    Public Sub AddCandle(ByVal candle_to_add As CandleStructure)
        SafeEnterTrace(CandleChunkKey, 10)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If _ChunkList.Count > 0 Then
            Dim last_chunk As List(Of CandleStructure) = _ChunkList.Last

            If last_chunk.Count >= _ChunkSize Then
                '새로운 청크를 만들어야 한다.
                Dim new_chunk As List(Of CandleStructure) = New List(Of CandleStructure)
                new_chunk.Add(candle_to_add)
                _ChunkList.Add(new_chunk)
                _Count += 1
            Else
                '현재 청크 뒷부분에 붙여야 한다.
                last_chunk.Add(candle_to_add)
                _Count += 1
            End If
        Else 'if _ChunkList.Count = 0
            '새로운 청크를 만들어야 한다.
            Dim new_chunk As List(Of CandleStructure) = New List(Of CandleStructure)
            new_chunk.Add(candle_to_add)
            _ChunkList.Add(new_chunk)
            _Count += 1
        End If
        SafeLeaveTrace(CandleChunkKey, 11)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub RemoveCandle()
        SafeEnterTrace(CandleChunkKey, 20)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐

        Dim first_chunk As List(Of CandleStructure)

        If _ChunkList.Count > 0 Then

            first_chunk = _ChunkList.Item(0)
            If first_chunk.Count = 0 Then
                '익셉션처리 하고 싶지만 실력이 부족하다.
            ElseIf first_chunk.Count = 1 Then
                _ChunkList.RemoveAt(0)
                _Count -= 1
            Else
                first_chunk.RemoveAt(0)
                _Count -= 1
            End If
        Else
            '익셉션처리 하고 싶지만 실력이 부족하다.
        End If
        SafeLeaveTrace(CandleChunkKey, 21)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public ReadOnly Property Candle(ByVal index As Integer) As CandleStructure
        Get
            SafeEnterTrace(CandleChunkKey, 30)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐

            Dim indicator As Integer = index
            Dim candle_to_return As CandleStructure

            For chunk_index As Integer = 0 To _ChunkList.Count - 1
                If _ChunkList.Item(chunk_index).Count > indicator Then
                    candle_to_return = _ChunkList.Item(chunk_index).Item(indicator)
                    SafeLeaveTrace(CandleChunkKey, 31)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    Return candle_to_return
                Else
                    indicator -= _ChunkList.Item(chunk_index).Count
                End If
            Next

            SafeLeaveTrace(CandleChunkKey, 31)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            '이 밑으로 왔다는 거는 index 의 범위가 valid하지 않다는 거다. 이 때 역시 익셉션처리  하고 싶지만 실력이 부족하다. 그래서 아래같이 그냥 빈 캔들을 넘긴다.
            Return New CandleStructure
        End Get
    End Property

    Public ReadOnly Property CandleCount As Integer
        Get
            Return _Count
        End Get
    End Property

    Public ReadOnly Property LastCandle As CandleStructure
        Get
            SafeEnterTrace(CandleChunkKey, 40)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            Dim last_candle As CandleStructure
            If _ChunkList.Count > 0 Then
                If _ChunkList.Last.Count > 0 Then
                    last_candle = _ChunkList.Last.Last
                Else
                    '익셉션처리 하고 싶지만 실력이 부족하다.
                End If
            Else
                '익셉션처리 하고 싶지만 실력이 부족하다.
            End If
            SafeLeaveTrace(CandleChunkKey, 41)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

            Return last_candle

        End Get
    End Property
End Class

#End If
