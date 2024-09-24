Imports CPUTILLib

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

Module m00_Global
    Public SymTree As New c01_Symtree
    Public SymbolList As New List(Of c03_Symbol)
    Public MainForm As MainWindow
    Public MarketStartTime As Integer
    Public MarketEndTime As Integer
    Public MarketStartHour As Integer
    Public MarketStartMinute As Integer
    Public MarketEndHour As Integer
    Public MarketEndMinute As Integer
    Public StillGettingPrices As Boolean = False
    Public GettingPricesKey As TracingKey
    Public DebugMsgKey As TracingKey
    Public MaxNumberOfRequest As Integer

    Public Const MA_BASE As Integer = 2

    Public StoredMessagesForDisplay As New List(Of String)
    Public StoredMessagesKeyForDisplay As TracingKey 'Integer
    Public StoredMessagesMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public StoredMessagesMutex As Threading.Mutex
    Public CpStuckMMF As System.IO.MemoryMappedFiles.MemoryMappedFile 'PriceMiner에 나좀 재실행해주세요 라는 메시지를 보내기 위해 쓰인다.
    Public CybosBasicDataMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public CybosBasicDataMutex As Threading.Mutex
    Public CybosRealDataMMF As System.IO.MemoryMappedFiles.MemoryMappedFile
    Public CybosRealDataMutex As Threading.Mutex
    'Public CybosCandleStoreMMF As System.IO.MemoryMappedFiles.MemoryMappedFile

    Public Const SYMBOL_CODE_LENGTH As Long = 12
    Public Const SYMBOL_NAME_LENGTH As Long = 20
    Public Const SYMBOL_BASIC_START_OFFSET As Long = 28
    Public Const SYMBOL_BASIC_DATA_SIZE As Long = 60
    Public Const SYMBOL_REAL_START_OFFSET As Long = 8
    Public Const SYMBOL_REAL_DATA_SIZE As Long = 20

    Public IsCpStuck As Boolean = False

    Public MessageMutexErrorCount As Integer = 0

    Public Sub ErrorLogging(ByVal error_message As String)
        SafeEnterTrace(StoredMessagesKeyForDisplay, 20)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If error_message.Length > 100000 Then
            error_message = "에러 메시지 너무 큰 에러"
        End If
        Dim text_to_display As String = "! " & Now.TimeOfDay.ToString & " [Cybos]: " & error_message & vbCrLf
        StoredMessagesForDisplay.Add(text_to_display)
        If StoredMessagesMutex IsNot Nothing AndAlso (Not IsCpStuck) Then
            Dim mutex_wait_result As Boolean = False

            Try
                mutex_wait_result = StoredMessagesMutex.WaitOne(1000)
            Catch ex As Exception
                'mutex error 일 경우 이렇게 될 수 있다. 가능성은 희박하지만.
                StoredMessagesMutex.ReleaseMutex()
                'mutex error 일 경우 이렇게 될 수 있다. 가능성은 희박하지만.
            End Try

            If mutex_wait_result Then
                If MessageMutexErrorCount > 0 Then
                    text_to_display = "! 메시지 잘라먹음 횟수 " & MessageMutexErrorCount.ToString & " " & text_to_display
                    MessageMutexErrorCount = 0
                End If
                Dim text_to_send As String = text_to_display
                Dim message_buffer As Byte() = Text.Encoding.UTF8.GetBytes(text_to_send)

                Dim stored_messages_access = StoredMessagesMMF.CreateViewAccessor(0, 100000)
                Dim current_length As UInt32 = stored_messages_access.ReadUInt64(0)     '현재 lenth 읽음
                stored_messages_access.WriteArray(Of Byte)(8 + current_length, message_buffer, 0, message_buffer.Length)    '새 message 저장
                stored_messages_access.Write(0, current_length + message_buffer.Length)   '새 lenth 저장
                StoredMessagesMutex.ReleaseMutex()
            Else
                'PriceMiner 에서 너무 오래 잡고 있거나 하면 이렇게 된다.
                MessageMutexErrorCount += 1
            End If
        End If
        SafeLeaveTrace(StoredMessagesKeyForDisplay, 21)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub MessageLogging(ByVal message As String)
        SafeEnterTrace(StoredMessagesKeyForDisplay, 30)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If message.Length > 100000 Then
            message = "일반 메시지 너무 큰 메시지"
        End If
        Dim text_to_display As String = "- " & Now.TimeOfDay.ToString & " [Cybos]: " & message & vbCrLf
        StoredMessagesForDisplay.Add(text_to_display)
        If StoredMessagesMutex IsNot Nothing AndAlso (Not IsCpStuck) Then
            Dim mutex_wait_result As Boolean = False

            Try
                mutex_wait_result = StoredMessagesMutex.WaitOne(1000)
            Catch ex As Exception
                'mutex error 일 경우 이렇게 될 수 있다. 가능성은 희박하지만.
                StoredMessagesMutex.ReleaseMutex()
            End Try
            If mutex_wait_result Then
                If MessageMutexErrorCount > 0 Then
                    text_to_display = "! 메시지 잘라먹음 횟수 " & MessageMutexErrorCount.ToString & " " & text_to_display
                    MessageMutexErrorCount = 0
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
                MessageMutexErrorCount += 1
            End If
        End If
        SafeLeaveTrace(StoredMessagesKeyForDisplay, 31)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub SafeEnterTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        While System.Threading.Interlocked.CompareExchange(tracing_key.Key, 1, 0) >= 1
            'Thread간 공유문제 발생예방
            'System.Threading.Thread.Yield()
            System.Threading.Thread.Sleep(1)
        End While
        tracing_key.Var = user_index
        tracing_key.Time = Now.TimeOfDay
    End Sub

    Public Sub SafeSetTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        System.Threading.Interlocked.Increment(tracing_key.Key)

        tracing_key.Var = user_index
        tracing_key.Time = Now.TimeOfDay
    End Sub

    Public Sub SafeLeaveTrace(ByRef tracing_key As TracingKey, ByVal user_index As Integer)
        tracing_key.Var = user_index
        System.Threading.Interlocked.Decrement(tracing_key.Key) 'Thread간 공유문제 발생예방
    End Sub

    Public Sub GlobalVarInit(ByVal main_form As MainWindow)
        MainForm = main_form
#If 1 Then
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

    Public Sub SetCpStuck(ByVal cp_stuck As Byte)
        CpStuckMMF.CreateViewAccessor(0, 1).Write(0, cp_stuck)
    End Sub

    Public Sub CpStuckAction()
        SetCpStuck(1)
        Try
            StoredMessagesMutex.ReleaseMutex()
        Catch ex As Exception
            Dim a = 1   '소유하지 않았는데도 release mutex하면 당연히 exception 난다.
        End Try
        Try
            CybosRealDataMutex.ReleaseMutex()
        Catch ex As Exception
            Dim a = 1   '소유하지 않았는데도 release mutex하면 당연히 exception 난다.
        End Try
        ErrorLogging("STUCK되었음. 나 닫힌다.")
        MainForm.CloseMe = True
    End Sub
End Module
