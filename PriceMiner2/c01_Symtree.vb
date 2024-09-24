Public Class c01_Symtree
    'Inherits List(Of c02_Bunch)

    Private _SymbolListing As Boolean
    Private _SetNewDataMutex As New Threading.Mutex(False, "SetNewDataMutex")
    Public BunchProcessKey As TracingKey
    Public SymbolProcessKey As TracingKey
    Public AccountProcessKey As TracingKey
    Public BunchProcessQueue As New List(Of Integer)
    Public SymbolProcessQueue As New List(Of c03_Symbol)
    Public AccountProcessQueue As New List(Of et1_AccountManager)
    Public LoopProcessThread As New Threading.Thread(AddressOf LoopProcess)
    Public PendingRequestCount As Integer
    Public NextBunchIndex As Integer
    Public BunchIndexKey As TracingKey
    Public CybosRealDataAccesser As System.IO.MemoryMappedFiles.MemoryMappedViewAccessor
    Public LastBunchProcessTime As DateTime = [DateTime].MaxValue
    Public RealDataMutexExceptionReported As Boolean = False
    Public CandleFileSystem As c06a_CandleFileSystem


    'Public WeAreStuckHere As Boolean = False
#If 0 Then
    '가장 마지막에 있는 번치
    Private ReadOnly Property LastBunch() As c02_Bunch
        Get
            If Count = 0 Then
                Return Nothing
            Else
                Return Item(Count - 1)
            End If
        End Get
    End Property
#End If

    Public Sub New()
        LoopProcessThread.IsBackground = True
        LoopProcessThread.Start()
    End Sub

    Public Sub StartSymbolListing()
        _SymbolListing = True
    End Sub

#If 0 Then
    Public Sub AddSymbol(ByVal symbol As c03_Symbol)
        If _SymbolListing Then
            If LastBunch Is Nothing OrElse LastBunch.Count = MaxNumberOfRequest Then
                '번치 하나 더 만들자
                Dim a_new_bunch As c02_Bunch = New c02_Bunch(symbol)
                Add(a_new_bunch)                'SymTree에 새 번치 더한다
            Else
                '있던 번치에 더하자
                LastBunch.Add(symbol)
            End If

            If LastBunch.Count = MaxNumberOfRequest Then
                '110개 다 모았으니 하나의 bunch 생성 마무리하자.
                LastBunch.SymbolListFix()    '번치의 종목리스트 마무리 (여기서 COM 객체도 생성)
            End If

        End If
    End Sub
#End If

    Public Sub FinishSymbolListing()
        _SymbolListing = False
    End Sub

    '주가 업데이트 위한 클락 들어옴
    Public Sub ClockSupply()
        '여기서 하는 것은 마지막으로 real data 처리한 시간을 보고 5초(마진 100ms 줘서 4.9초)가 지나 있으면 stored data 를 가지고 한 바퀴 돌려 limphome 처리하는 것이다.
        Dim start_time = Now
        If start_time - LastBunchProcessTime > TimeSpan.FromMilliseconds(4900) Then
            If _SetNewDataMutex.WaitOne(0) Then
                For index As Integer = 0 To SymbolList.Count - 1
                    SymbolList(index).SetNewData()  '이미 store된 data 로 limphome 처리하는 것이다.
                Next
                LastBunchProcessTime = start_time
                MessageLogging("Symtree " & "5초 동안 data 전송 안 되어 limphome 처리")
                _SetNewDataMutex.ReleaseMutex()
            End If
        End If
#If 0 Then
        MessageLogging("C_En_BR")
        SafeEnterTrace(GettingPricesKey, 0)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If StillGettingPrices = True Then
            '160326: 아마도 15초내 60회 요청회수 제한 때문에 이전 요청이 아직 대기중인 것으로 파악됨.=> 밀림 횟수 counting 하고 그냥 나가자
            PendingRequestCount = PendingRequestCount + 1
            'WeAreStuckHere = True
            ErrorLogging("아마도 15초내 60회 요청회수 제한 때문에 이전 요청이 아직 대기중인 것으로 파악됨")
            SafeLeaveTrace(GettingPricesKey, 1)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Exit Sub
        End If
        StillGettingPrices = True
        SafeLeaveTrace(GettingPricesKey, 2)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Dim request_ok As Boolean = False
        Dim duplicated As Boolean = False

        For index As Integer = 0 To Count - 1
            request_ok = Item(index).Mst2BlockRequest(False)     '각 번치마다 update request
            '위에서 받아온 data를 처리하기 위해 작업Queue에 넣는다.
            duplicated = BunchTaskToQueue(index)
            If duplicated Then
                MessageLogging("이전 작업이 아직 처리되지 않아 중복 queueing")
            End If
            'If request_ok Then
            '160328: 15초 60회 요청제한 때문에 모든 데이터 0으로 받아진 거다. 재요청하면 제대로 받을 수 있다.
            'request_ok = Item(index).Mst2BlockRequest()     '각 번치마다 update request
            'End If
            MessageLogging(index.ToString & " request done")
            'SafeEnterTrace(DebugMsgKey, 4)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & index.ToString & " request done" & vbCrLf, True)
            'SafeLeaveTrace(DebugMsgKey, 5)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Next

        'new log for debug
        'SafeEnterTrace(DebugMsgKey, 0)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & "Daishin End" & vbCrLf, True)
        'SafeLeaveTrace(DebugMsgKey, 1)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        '160330: 위에서 store한 data를 아래에서 처리한다.
        'Dim data_receive_process As Threading.Thread = New Threading.Thread(AddressOf SymtreeDataReceiveProcess)
        'data_receive_process.Start()
        'SymtreeDataReceiveProcess()

        'new log for debug
        'SafeEnterTrace(DebugMsgKey, 2)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & "Ebest End" & vbCrLf, True)
        'SafeLeaveTrace(DebugMsgKey, 3)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        SafeEnterTrace(GettingPricesKey, 3)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '160326: 밀린 요청에 대해 limphome 처리해준다
        If PendingRequestCount > 0 Then
            For pending_index As Integer = 0 To PendingRequestCount - 1
                MessageLogging("펜딩된 리퀘스트에 대해 림프홈 처리")
                For index As Integer = 0 To Count - 1
                    'Item(index).COMObj_LimpHome()     '각 번치마다 limphome 처리
                    duplicated = BunchTaskToQueue(index)     '이건 duplicated 일수밖에 없지
                Next
            Next

            '각 bunch 객체의 COM 객체 교체
            For bunch_index As Integer = 0 To Count - 1
                Item(bunch_index).COMObj = Nothing
                Item(bunch_index).COMObj = New et5_MarketEye_Wrapper
            Next
        End If
        PendingRequestCount = 0
        StillGettingPrices = False
        SafeLeaveTrace(GettingPricesKey, 4)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        MessageLogging("C_Le_BR")
#End If
    End Sub

    Public Sub LoopProcess()
        '원래 여기는 bunch의 할 일만 관리한는 Loop process 였는데, 그래서 Symtree에 넣어둔 프로세스인데 thread관리가 복잡해지는 것을 막기 위해
        'Symbol의 할 일도 여기서 관리하기로 결정되었다. 그래서 queue도 두개 key도 두 개가 되었다. 그 중 symbol이 더 우선이다. 왜냐면 주문을 빨리하는 게 중요하니까
        '추가로 account의 할 일도 여기서 관리하기로 결정되었다. 그래서 queue도 key도 세 개가 되었다. account는 우선순위가 가장 낮다.
        'Dim bunch_index_to_work As Integer
        Dim number_of_symbols_need_transfer, number_of_symbols_need_transfer_save As Integer
        Dim begin_symbol_index, end_symbol_index, current_symbol_index As Integer
        Dim saved_begin_index, saved_end_index As Integer
        Dim symbol_obj As c03_Symbol
        Dim account_obj As et1_AccountManager
        Dim bunch_work As Boolean = False
        Dim need_mutex_release As Boolean = False
        While 1
            SafeEnterTrace(SymbolProcessKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            If SymbolProcessQueue.Count > 0 Then
                '심볼의 할 일을 찾았다.
                symbol_obj = SymbolProcessQueue(0)
                SymbolProcessQueue.RemoveAt(0)
            Else
                '심볼의 할 일이 없으면 여기로 들어온다.
                SafeLeaveTrace(SymbolProcessKey, 3)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                '2021.06.09: CybosLauncher로 번치가 옮겨간 후로 PriceMiner에서 번치는 사라졌지만, CybosLauncher 에서 일부분씩 넘겨주는 real data 를 처리하기
                '2021.06.09: 위한 process 로 이곳을 남겨두었다.
                If CybosRealDataMutex IsNot Nothing Then
                    Try
                        If CybosRealDataMutex.WaitOne(0) Then
                            number_of_symbols_need_transfer = CybosRealDataAccesser.ReadInt32(4)
                            If number_of_symbols_need_transfer > 0 Then
                                '번치 할 일 있다. mutex release 필요하다.
                                bunch_work = True
                                need_mutex_release = True
                            Else
                                '번치 할 일 없다. mutex release 필요하다.
                                bunch_work = False
                                need_mutex_release = True
                            End If
                        Else
                            '번치 할 일 없다. mutex release 필요없다.
                            bunch_work = False
                            need_mutex_release = False
                        End If
                    Catch ex As Exception
                        If Not RealDataMutexExceptionReported Then
                            ErrorLogging("RealDataMutex 고장")
                            WarningLogging("RealDataMutex 고장")
                            RealDataMutexExceptionReported = True
                        End If
                    End Try
                Else
                    '번치 할 일 없다.  mutex release 필요없다.
                    bunch_work = False
                    need_mutex_release = False
                End If
                If Not bunch_work Then
                    '번치의 할 일이 없으면 여기로 들어온다.
                    If need_mutex_release Then
                        '2022.08.26: 아래에서 System.NullReferenceException 남. 그런데 CybosRealDataMutex 는 사실 NULL 이 아니었음.1시54분쯤?
                        CybosRealDataMutex.ReleaseMutex()
                    End If
                    '이제는 어카운트의 할일을 찾아본다.
                    SafeEnterTrace(AccountProcessKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    If AccountProcessQueue.Count > 0 AndAlso Now - AccountProcessQueue(0).LastTimeAccountTaskExecuted > TimeSpan.FromMilliseconds(ACCOUNT_TASK_MIN_PERIOD) Then
                        '어카운트의 할 일을 찾았다.
                        '2024.01.08 : prethreshold 로 인해 account task 는 이제 전보다 실행횟수가 늘어날 것이다. 너무 자주 불리는 것을 방지하기 위해 최소주기를 설정하고 그 이상 지났을 때만 실행된다.
                        'AccountProcessIndicator = False
                        account_obj = AccountProcessQueue(0)
                        AccountProcessQueue.RemoveAt(0)
                        account_obj.LastTimeAccountTaskExecuted = Now   '마지막 실행시간 업데이트
                    Else
                        '어카운트의 할 일이 없으면 여기로 들어온다.
                        SafeLeaveTrace(AccountProcessKey, 3)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        Threading.Thread.Sleep(10)
                        Continue While
                    End If
                    SafeLeaveTrace(AccountProcessKey, 2)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    '어카운트에서 할 일을 찾아서 여기로 내려왔다.
#If 0 Then
                    MessageLogging("Main Search & distribute" & ", decision holders : " & MainForm.MainAccountManager.DecisionHolders.Count)
                    MainForm.MainAccountManager.SymbolSearching()        '매매할 종목 찾기
                    MainForm.MainAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
                    If SubAccount Then
#If ALLOWED_SALE_COUNT Then
                        MessageLogging("Sub Search & distribute" & ", decision holders : " & MainForm.SubAccountManager.DecisionHolders.Count & ", AllowedSaleCountFromLastSale : " & MainForm.SubAccountManager.AllowedSaleCountFromLastSale.ToString)
#Else
                        MessageLogging("Sub Search & distribute" & ", decision holders : " & MainForm.SubAccountManager.DecisionHolders.Count)
#End If
                        MainForm.SubAccountManager.SymbolSearching()        '매매할 종목 찾기
                        MainForm.SubAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
                    End If
                    MessageLogging("Test Search & distribute" & ", decision holders : " & MainForm.TestAccountManager.DecisionHolders.Count)
                    MainForm.TestAccountManager.SymbolSearching()        '매매할 종목 찾기
                    MainForm.TestAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
#End If
                    account_obj.SymbolSearching()   '매매할 종목 찾기
                    account_obj.MoneyDistribute()   '매수를 위한 돈 분배

                    Continue While
                Else
                    '번치큐에서 할 일을 찾아서 여기로 내려왔다.
                    If MainForm.stm_PriceClock.Interval = 1 Then
                        '0이면 가격타이머 안 돌아가는 거니까 아무것도 할 게 없다.
                        CybosRealDataMutex.ReleaseMutex()
                    Else
                        SafeEnterTrace(MainForm.TestAccountManager.StockListKey, 100)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                        Dim decision_holder_count_before = MainForm.TestAccountManager.DecisionHolders.Count
                        SafeLeaveTrace(MainForm.TestAccountManager.StockListKey, 101)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        number_of_symbols_need_transfer = CybosRealDataAccesser.ReadInt32(4)
                        begin_symbol_index = CybosRealDataAccesser.ReadInt32(0)
                        end_symbol_index = begin_symbol_index + Math.Min(number_of_symbols_need_transfer, SymbolList.Count) '이미 몇 바퀴 돌아서 써진거라면 한 바퀴만 돌면 된다.
                        current_symbol_index = begin_symbol_index
                        '심볼별로 돌면서 심볼 가격등을 업데이트한다.
                        Dim price As UInteger
                        Dim amount As ULong
                        Dim gangdo As Double
                        While current_symbol_index <> end_symbol_index
                            price = CybosRealDataAccesser.ReadUInt32(SYMBOL_REAL_START_OFFSET + SYMBOL_REAL_DATA_SIZE * (current_symbol_index Mod SymbolList.Count))
                            amount = CybosRealDataAccesser.ReadUInt64(SYMBOL_REAL_START_OFFSET + SYMBOL_REAL_DATA_SIZE * (current_symbol_index Mod SymbolList.Count) + 4)
                            gangdo = CybosRealDataAccesser.ReadDouble(SYMBOL_REAL_START_OFFSET + SYMBOL_REAL_DATA_SIZE * (current_symbol_index Mod SymbolList.Count) + 12)
                            SymbolList(current_symbol_index Mod SymbolList.Count).StoredPrice = price
                            SymbolList(current_symbol_index Mod SymbolList.Count).StoredAmount = amount
                            SymbolList(current_symbol_index Mod SymbolList.Count).StoredGangdo = gangdo
                            'SymbolList(current_symbol_index Mod SymbolList.Count).SetNewData()  '심볼에서 새데이터 처리하도록 해준다. =>2021.07.07: 오래 걸리는 경우를 대비해 아래로 내린다.
                            current_symbol_index += 1
                        End While
                        saved_begin_index = begin_symbol_index
                        saved_end_index = end_symbol_index
                        '첫머리에 begin index 와 symbol 갯수 업데이트하고 release mutex 한다.
                        begin_symbol_index = (begin_symbol_index + number_of_symbols_need_transfer) Mod SymbolList.Count    '굳이 end index 의 mod 를 계산하지 않고 begin index + number of symbols 로 한 거는 한 바퀴 넘어서 써진 거를 한 바퀴만 돌렸을 때 다음 시작 위치를 정확하게 하기 위해서이다.
                        number_of_symbols_need_transfer_save = number_of_symbols_need_transfer
                        number_of_symbols_need_transfer = 0
                        CybosRealDataAccesser.Write(0, begin_symbol_index)
                        CybosRealDataAccesser.Write(4, number_of_symbols_need_transfer)
                        '2021.06.09: 위에 NUMBER OF SYMBOLS NEED TRANSFER position이 0으로 되어 있는 거 fix했다. 이제 또 잘돌아가나 확인해보자.
                        '2021.06.09: 그리고 symbol collection이 생각보다 오래 걸리기 때문에, symbol collection 하고 있는 동안 cybos launcher는 5초 싸이클을 몇 바퀴 돌 수도 있겠다. 이럴 때 문제 없는지 확인해보자.
                        '2021.06.10: symbol collection 이 생각보다 오래 걸리는 정도가 아니라, 시스템 전체를 느리게 한다. visual studio 없이 해보는 등의 실험이 필요하다.
                        CybosRealDataMutex.ReleaseMutex()
                        current_symbol_index = saved_begin_index
                        _SetNewDataMutex.WaitOne()
                        While current_symbol_index <> saved_end_index
                            '2021.07.07: SetNewData 안에서 ebest TR request 할 때 횟수 제한에 걸려 대기타고 있을 때도 있어서 오래 걸리는 부분을 이렇게 빼놓았다. 여기 도는 동안 CybosLauncher는 열심히 real data 영역을 업데이트하고 있을 것이다.
                            SymbolList(current_symbol_index Mod SymbolList.Count).SetNewData()
                            current_symbol_index += 1
                        End While
                        _SetNewDataMutex.ReleaseMutex()
                        MessageLogging("Symtree " & number_of_symbols_need_transfer_save & " 번치처리함")
                        LastBunchProcessTime = Now          '2021.09.20: tortoise 에 commit 하가다 리뷰하면서 이것 실수로 빠진 것 같아 다시 추가했다.

                        '2024.01.08 : Test 계좌에서 걸린애들이 추가된 경우 account task를 실행한다.
                        SafeEnterTrace(MainForm.TestAccountManager.StockListKey, 110)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                        Dim decision_holder_count_after = MainForm.TestAccountManager.DecisionHolders.Count
                        SafeLeaveTrace(MainForm.TestAccountManager.StockListKey, 111)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┘
                        If decision_holder_count_before < decision_holder_count_after Then
                            AccountTaskToQueue(MainForm.TestAccountManager)
                        End If
                    End If
                    Continue While
                End If
            End If
            SafeLeaveTrace(SymbolProcessKey, 2)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            '심볼큐에서 할 일을 찾아서 여기로 내려왔다.
            'MessageLogging(symbol_obj.Code & " 심볼처리함")
            symbol_obj.SymbolTask()
        End While
    End Sub

    'Bunch Queue에 task를 추가한다.
    Public Function BunchTaskToQueue(ByVal bunch_index_to_work As Integer) As Boolean
        Dim duplicated As Boolean = False

        SafeEnterTrace(BunchProcessKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '해당 bunch가 이미 queue에 미처리 상태로 있는지 알아본다.
        For index As Integer = 0 To BunchProcessQueue.Count - 1
            If BunchProcessQueue(index) = bunch_index_to_work Then
                duplicated = True
                Exit For
            End If
        Next
        '큐에 추가한다.
        If Not duplicated Then
            BunchProcessQueue.Add(bunch_index_to_work)
        End If
        SafeLeaveTrace(BunchProcessKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Return duplicated
    End Function

    'Symbol Queue에 task를 추가한다.
    Public Function SymbolTaskToQueue(ByVal symbol_obj As c03_Symbol) As Boolean
        Dim duplicated As Boolean = False

        SafeEnterTrace(SymbolProcessKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '해당 symbol이 이미 queue에 미처리 상태로 있는지 알아본다.
        For index As Integer = 0 To SymbolProcessQueue.Count - 1
            If SymbolProcessQueue(index) Is symbol_obj Then
                duplicated = True
                Exit For
            End If
        Next
        '큐에 추가한다.
        If Not duplicated Then
            SymbolProcessQueue.Add(symbol_obj)
        End If
        SafeLeaveTrace(SymbolProcessKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Return duplicated
    End Function

    'Account indicator에 task 시행이 필요함을 표시한다.
    '2023.11.11 : MA 계좌는 기존대로 5초마다 한번씩, 다른 계좌들은 걸린애들이 있을 때에만 처리하는 것으로 변경하기로 하면서 Account 도 Queue 사용으로 감.
    Public Function AccountTaskToQueue(ByVal account_obj As et1_AccountManager) As Boolean
        Dim duplicated As Boolean = False

        SafeEnterTrace(AccountProcessKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '어카운트 인디케이터에 추가한다.
        'AccountProcessIndicator = True
        '해당 account가 이미 queue에 미처리 상태로 있는지 알아본다.
        For index As Integer = 0 To AccountProcessQueue.Count - 1
            If AccountProcessQueue(index) Is account_obj Then
                duplicated = True
                Exit For
            End If
        Next
        '큐에 추가한다.
        If Not duplicated Then
            AccountProcessQueue.Add(account_obj)
        End If
        SafeLeaveTrace(AccountProcessKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Function

#If 0 Then
    Public Sub BlockRequestThread()
        Dim data_received_nothing As Boolean = False
        Dim index_to_do As Integer = -1

        SafeEnterTrace(BunchIndexKey, 3)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        index_to_do = NextBunchIndex
        NextBunchIndex = NextBunchIndex + 1
        SafeLeaveTrace(BunchIndexKey, 4)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        While index_to_do < Count
            data_received_nothing = Item(index_to_do).Mst2BlockRequest()     '각 번치마다 update request
            If data_received_nothing Then
                '160328: 15초 60회 요청제한 때문에 모든 데이터 0으로 받아진 거다. 재요청하면 제대로 받을 수 있다.
                data_received_nothing = Item(index_to_do).Mst2BlockRequest()     '각 번치마다 update request
            End If
            SafeEnterTrace(DebugMsgKey, 4)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            My.Computer.FileSystem.WriteAllText("debug" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt", Now.TimeOfDay.ToString & " : " & index_to_do.ToString & " Done" & vbCrLf, True)
            SafeLeaveTrace(DebugMsgKey, 5)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

            SafeEnterTrace(BunchIndexKey, 3)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            index_to_do = NextBunchIndex
            NextBunchIndex = NextBunchIndex + 1
            SafeLeaveTrace(BunchIndexKey, 4)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End While
    End Sub
#End If

#If 0 Then
    Public Sub SymtreeDataReceiveProcess()
        MessageLogging("C_En_DT")
        For bunch_index As Integer = 0 To Count - 1
            For symbol_index As Integer = 0 To Item(bunch_index).Count - 1
                Item(bunch_index).Item(symbol_index).SetNewData()
            Next
        Next
        MessageLogging("C_Le_DT")
    End Sub
#End If

    '모은 모든 종목의 시세정보를 DB에 저장한다.
    'Public Sub SaveToDB()
    'End Sub
End Class
