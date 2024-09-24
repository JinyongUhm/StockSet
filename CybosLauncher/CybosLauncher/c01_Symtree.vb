Public Class c01_Symtree
    Inherits List(Of c02_Bunch)

    Private _SymbolListing As Boolean
    Public BunchProcessKey As TracingKey
    Public SymbolProcessKey As TracingKey
    Public AccountProcessKey As TracingKey
    'Public BunchProcessQueue As New List(Of Integer)
    '2021.06.08: CybosLauncher 에서는 Bunch process queue를 없애기로 했다. bunch process 가 실행기회를 얻으면 그냥 그동안 처리 안 되어 있던 번치들을 모아서
    '2021.06.08: 한 번에 처리하는 것으로 바꾼다. BeingIndex 는 bunch의 시작 index 이고, bunch count 는 미처리되고 남아있는 번치들의 갯수이다.
    Public BunchProcessBeginIndex As Integer
    Public BunchProcessBunchCount As Integer
    Public SymbolProcessQueue As New List(Of c03_Symbol)
    Public AccountProcessIndicator As Boolean
    Public LoopProcessThread As New Threading.Thread(AddressOf LoopProcess)
    Public PendingRequestCount As Integer
    Public PendingAccumCount As Integer = 0
    Public NextBunchIndex As Integer
    Public BunchIndexKey As TracingKey
    'Public WeAreStuckHere As Boolean = False

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

    Public Sub New()
        LoopProcessThread.IsBackground = True
        LoopProcessThread.Start()
    End Sub

    Public Sub StartSymbolListing()
        _SymbolListing = True
    End Sub

    Public Sub AddSymbol(ByVal symbol As c03_Symbol)
        If _SymbolListing Then
            If LastBunch Is Nothing OrElse LastBunch.Count = MaxNumberOfRequest Then
                '번치 하나 더 만들자
                Dim a_new_bunch As c02_Bunch = New c02_Bunch(symbol)
                a_new_bunch.MyIndex = Count  '2021.06.14: 공유 data 속에서 자신의 위치를 알기 위해 추가한 bunch index
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

    Public Sub FinishSymbolListing()
        _SymbolListing = False
        LastBunch.SymbolListFix()       '마지막 미완 번치도 종목리스트 마무리
    End Sub

    '주가 업데이트 위한 클락 들어옴
    Public Sub ClockSupply()
        'MessageLogging("C_En_BR")

        SafeEnterTrace(GettingPricesKey, 0)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If StillGettingPrices = True Then
            '160326: 아마도 15초내 60회 요청회수 제한 때문에 이전 요청이 아직 대기중인 것으로 파악됨.=> 밀림 횟수 counting 하고 그냥 나가자
            PendingRequestCount = PendingRequestCount + 1
            'WeAreStuckHere = True
            ErrorLogging("아마도 15초내 60회 요청회수 제한 때문에 이전 요청이 아직 대기중인 것으로 파악됨")
            PendingAccumCount += 1
            SafeLeaveTrace(GettingPricesKey, 1)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If PendingAccumCount > 3 Then
                '2024.02.02 : 어제 로그에서 아래 for loop 중 한 bunch 에서의 소요 시간이 4초가 넘게 걸리는 이상한 증상이 계속적으로 나타났다.
                '2024.02.02 : 이로 인해 5초당 1회가 아니라 10초당 1회의 price mining 이 일어나게 되어 하루 장사를 망쳐버렸다. 원인은 알 수 없다.
                '2024.02.02 : Cybos 다시 돌리니까 잘 돌아간다. 이제는 이 증상이 3번을 초과해 일어나면 대신 Cybos가 이상이 있는 것으로 판단하여
                '2024.02.02 : 자동종료 시키고 PriceMiner로 하여금 재시작하게 만들도록 한다.
                ErrorLogging("Cybos이상행동으로 닫히는 중")
                CpStuckAction()
            End If
            Exit Sub
        End If
        StillGettingPrices = True
        SafeLeaveTrace(GettingPricesKey, 2)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Dim request_ok As Boolean = False
        Dim duplicated As Boolean = False

        For index As Integer = 0 To Count - 1
            'If index = 8 Then
            '중간이다. Test 계좌 조회하자. MainAcount는 Form에서 조회하고 TestAccount 는 Symtree 에서 조회하고 개판이다
            'MainForm.TestAccountManager.RequestAccountInfo(TestAccountString, TestAccountPW)
            'End If
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
            MessageLogging(index.ToString & "done")
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
#If 0 Then
        'CybosLauncher로 옮긴 후 펜딩된 리퀘스트 처리 안 하기로 함
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
#End If
        PendingRequestCount = 0
        StillGettingPrices = False
        SafeLeaveTrace(GettingPricesKey, 4)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        'MessageLogging("C_Le_BR")
    End Sub

    Public Sub LoopProcess()
        '원래 여기는 bunch의 할 일만 관리한는 Loop process 였는데, 그래서 Symtree에 넣어둔 프로세스인데 thread관리가 복잡해지는 것을 막기 위해
        'Symbol의 할 일도 여기서 관리하기로 결정되었다. 그래서 queue도 두개 key도 두 개가 되었다. 그 중 symbol이 더 우선이다. 왜냐면 주문을 빨리하는 게 중요하니까
        '추가로 account의 할 일도 여기서 관리하기로 결정되었다. 그래서 queue도 key도 세 개가 되었다. account는 우선순위가 가장 낮다.
        'Dim bunch_index_to_work As Integer
        Dim symbol_obj As c03_Symbol
        While 1
            SafeEnterTrace(SymbolProcessKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            If SymbolProcessQueue.Count > 0 Then
                '심볼의 할 일을 찾았다.
                symbol_obj = SymbolProcessQueue(0)
                SymbolProcessQueue.RemoveAt(0)
            Else
                '심볼의 할 일이 없으면 여기로 들어온다.
                SafeLeaveTrace(SymbolProcessKey, 3)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                '이제는 번치의 할일을 찾아본다.
                SafeEnterTrace(BunchProcessKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                If BunchProcessBunchCount > 0 Then
                    '번치의 할 일을 찾았다.
                Else
                    '번치의 할 일이 없으면 여기로 들어온다.
                    SafeLeaveTrace(BunchProcessKey, 3)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                    '이제는 어카운트의 할일을 찾아본다.
                    SafeEnterTrace(AccountProcessKey, 1)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
                    If AccountProcessIndicator Then
                        '어카운트의 할 일을 찾았다.
                        AccountProcessIndicator = False
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
                        MessageLogging("Sub Search & distribute" & ", decision holders : " & MainForm.SubAccountManager.DecisionHolders.Count)
                        MainForm.SubAccountManager.SymbolSearching()        '매매할 종목 찾기
                        MainForm.SubAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
                    End If
                    MessageLogging("Test Search & distribute" & ", decision holders : " & MainForm.TestAccountManager.DecisionHolders.Count)
                    MainForm.TestAccountManager.SymbolSearching()        '매매할 종목 찾기
                    MainForm.TestAccountManager.MoneyDistribute()        '매수를 위한 돈 분배
#End If

                    Continue While
                End If

                '번치큐에서 할 일을 찾아서 여기로 내려왔다.
                If CybosRealDataMutex IsNot Nothing AndAlso (Not IsCpStuck) Then
                    CybosRealDataMutex.WaitOne()
                    Dim begin_bunch_index As Integer = BunchProcessBeginIndex
                    Dim end_bunch_index As Integer = BunchProcessBeginIndex + BunchProcessBunchCount
                    Dim current_bunch_index As Integer = BunchProcessBeginIndex
                    Dim number_of_symbols_updated As Integer = 0
                    '번치별로 돌면서 심볼 가격등을 업데이트한다.
                    While current_bunch_index <> end_bunch_index
                        Item(current_bunch_index Mod Me.Count).SetNewDataProcess()
                        number_of_symbols_updated += Item(current_bunch_index Mod Me.Count).Count
                        current_bunch_index += 1
                    End While
                    '첫머리에 price miner쪽에서 아직 미처리한 심볼들 갯수를 업데이트하고 release mutex 한다.
                    Dim cybos_real_data_accessor = CybosRealDataMMF.CreateViewAccessor(0, 8)
                    Dim total_number_of_symbols_need_transfer As Integer = cybos_real_data_accessor.ReadInt32(4) + number_of_symbols_updated
                    cybos_real_data_accessor.Write(4, total_number_of_symbols_need_transfer)
                    CybosRealDataMutex.ReleaseMutex()
                    'BunchProcess 관련 변수들을 업데이트한다.
                    BunchProcessBeginIndex = (BunchProcessBeginIndex + BunchProcessBunchCount) Mod Me.Count
                    BunchProcessBunchCount = 0
                    SafeLeaveTrace(BunchProcessKey, 2)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Else
                    SafeLeaveTrace(BunchProcessKey, 4)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                End If

                Continue While
            End If
            SafeLeaveTrace(SymbolProcessKey, 2)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            '심볼큐에서 할 일을 찾아서 여기로 내려왔다.
            MessageLogging(symbol_obj.Code & " 심볼처리함")
            'symbol_obj.SymbolTask()
        End While
    End Sub

    'Bunch Queue에 task를 추가한다.
    Public Function BunchTaskToQueue(ByVal bunch_index_to_work As Integer) As Boolean
        Dim duplicated As Boolean = False

        SafeEnterTrace(BunchProcessKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        BunchProcessBunchCount += 1
        If (BunchProcessBeginIndex + BunchProcessBunchCount - 1) Mod Me.Count <> bunch_index_to_work Then
            'bunch process 변수의 consistency check 이다.
            ErrorLogging("번치Process 일관성 안 맞습니다.")
        End If
#If 0 Then
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
#End If
        SafeLeaveTrace(BunchProcessKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        Return duplicated
    End Function

#If 0 Then
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
    Public Function AccountTaskToIndicator() As Boolean
        SafeEnterTrace(AccountProcessKey, 10)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '어카운트 인디케이터에 추가한다.
        AccountProcessIndicator = True
        SafeLeaveTrace(AccountProcessKey, 11)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Function

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
