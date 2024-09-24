#If MOVING_AVERAGE_DIFFERENCE Then
Public Class c042_ChartDBSupport
    Inherits c04_DBSupport
    Public ChartHeaterProcessList As New List(Of ChartHeaterProcess)
    Private Shared _DB_FOLDER As String = "D:\Finance\Database\CandleChart\"
    Private Shared _CORE_COUNT As Integer = 8
    Private _TableNameToCheck As String
    Private _DB_connection As OleDb.OleDbConnection
    Private _NumberOfSaved As Integer
    Private _SaveFinished As Boolean
    Private _Initialized As Boolean = False
    Public DateListToCollect As New List(Of Date)
    Public DateListKey As Integer
    Public DateCollecting As Boolean
    Public GlobalTrendData As New List(Of List(Of Double))
    Public GlobalDeviationData As New List(Of List(Of Double))
    Public DateForGlobalData As New List(Of Date)
    Public ErrorCount As Integer = 0
    Public ErrorString As String = ""
    Public Sub New()
        'DB List 방식으로 바뀌면서 해 줄 게 하나도 없어졌다.
    End Sub

    'table list에 있는지 체크
    Private Function TableExistCheck(ByVal a_string As String) As Boolean
        If a_string = _TableNameToCheck Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Overrides Sub SetCpuLoadControl(accel_value As Integer)
        For index As Integer = 0 To ChartHeaterProcessList.Count - 1
            ChartHeaterProcessList(index).CPULoadControl = accel_value
        Next
    End Sub

    '시뮬레이션
    '#If Not SIMULATION_PERIOD_IN_ARRAY Then
    Public Overrides Sub Simulate(ByVal start_date As DateTime, ByVal end_date As DateTime)
        '#Else
        'Public Overrides Sub Simulate()
        '#End If
        'Dim start_date As DateTime = MainForm.dtp_StartDate.Value.Date
        'Dim end_date As DateTime = MainForm.dtp_EndDate.Value.Date

        For core_index As Integer = 0 To _CORE_COUNT - 1
            ChartHeaterProcessList.Add(New ChartHeaterProcess)
            ChartHeaterProcessList(core_index).ProcessStart(MainForm.AccelValueStored)   '일단 프로세스 돌려놈
            ChartHeaterProcessList(core_index).ThreadNumber = core_index
        Next

        If Not _Initialized Then
            _Initialized = True
        End If

        'MS SQL DB simulation
        Dim db_name As String
        Dim expected_db_list As New List(Of String)

        Dim read_cmd = New OleDb.OleDbCommand() '("SELECT * from TableName WHERE SampledTime BETWEEN @start_date AND @end_date", _DB_connection)
        read_cmd.Connection = _DB_connection
        'query parameter 설정
        Dim start_date_str As String = start_date.Year & "-" & start_date.Month.ToString("D2") & "-" & start_date.Day.ToString("D2") & " 00:00:00"
        Dim end_date_str As String = end_date.Year & "-" & end_date.Month.ToString("D2") & "-" & end_date.Day.ToString("D2") & " 23:59:59"
        'make expected_db_list
        For Each found_file As String In My.Computer.FileSystem.GetFiles(_DB_FOLDER, FileIO.SearchOption.SearchTopLevelOnly, "*.mdf")
            expected_db_list.Add(found_file.Substring(_DB_FOLDER.Length, 21))
        Next

        Dim table_name As String
        Dim symbol_obj As c03_Symbol
        Dim symbol_name As String
        Dim command_text As String
        Dim the_core As ChartHeaterProcess

        DBProgressMax = expected_db_list.Count
        For db_index As Integer = 0 To expected_db_list.Count - 1
            DBProgressValue = db_index
            db_name = expected_db_list(db_index)    ' "PriceDB_" & "12_1" 'current_year.Year
            'If db_name <> "CandleChartDB_A263750" Then
            'Continue For
            'End If
            'initial catalog이용해 DB_Connection 다시 만들어야 한다.
            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            _DB_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "\SQLEXPRESS; Initial Catalog=" & db_name & "; Integrated Security=SSPI;")

            Dim retry_open As Integer = 0
            Dim open_error As Boolean
            Do
                open_error = False
                Try
                    _DB_connection.Open()
                Catch ex As Exception
                    open_error = True
                    retry_open += 1
                    ErrorCount += 1
                    Threading.Thread.Sleep(1000)
                End Try
            Loop While open_error AndAlso retry_open < 10

            read_cmd.Connection = _DB_connection
            'read_cmd.CommandText = "DECLARE @start_date datetime"
            'read_cmd.ExecuteNonQuery()
            'read_cmd.CommandText = "DECLARE @end_date datetime"
            'read_cmd.ExecuteNonQuery()

            '각 core thread 에 DB connection 정보 전달
            'For core_index As Integer = 0 To ChartHeaterProcessList.Count - 1
            'ChartHeaterProcessList(core_index).DBConnection = _DB_connection
            'Next

            Dim data_table As DataTable = _DB_connection.GetSchema("tables")
            'ProgressMax = data_table.Rows.Count         'progress max 설정
            'ProgressValue = 0                           'progress value 초기화

            'For table_index As Integer = 0 To data_table.Rows.Count - 1
            'table_name_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
            table_name = db_name.Substring(14, 7)

            symbol_name = table_name        '이름 알려고 대신증권 서버를 물고 있어서 다른 프로그램이 실행이 안 되어서 이렇게 함.
            '종목객체를 만들고 리스트에 추가한다.
            symbol_obj = AddSymbolIfNew(table_name, symbol_name)

            'DB로부터 data 읽어온다.
            command_text = "SELECT * from " & table_name & " WHERE CandleTime BETWEEN '" & start_date_str & "' AND '" & end_date_str & "' ORDER BY CandleTime ASC"

            'available한 core가 있는지 알아본다.
            Do
                Threading.Thread.Sleep(20)

                For core_index As Integer = 0 To ChartHeaterProcessList.Count - 1
                    If ChartHeaterProcessList(core_index).CurrentStatus <> ChartHeaterProcess._RUNNING Then
                        the_core = ChartHeaterProcessList(core_index)
                        Exit Do
                    End If
                Next
            Loop
            the_core.DBConnection = _DB_connection
            the_core.ReadCmd.CommandText = command_text
            the_core.SymbolObj = symbol_obj
            the_core.DBSupporter = MainForm.DBSupporter

            the_core.CurrentStatus = ChartHeaterProcess._RUNNING           '쓰레드에게 일을 준다.
            '170929: 이제는 core에서 날짜를 받아서 global trend data 얻는 부분을 해줘야 한다. 만들어 놓은 Get함수는 아마도 index를 반환하는 걸로 바꿔야 할 듯 하다.
            ProgressValue = db_index             'progress 값 update

            'Next
#If 0 Then

            'DB 중지하기 전에 읽을 거 다 읽는다
            '190302_TODO: DB 종료하는 것은 HeatProcess 안으로 옮기자.
            Dim still_running As Boolean
            Do
                Threading.Thread.Sleep(20)
                still_running = False
                'My.Application.DoEvents()
                For core_index As Integer = 0 To ChartHeaterProcessList.Count - 1
                    If ChartHeaterProcessList(core_index).CurrentStatus = HeaterProcess._NOT_RUN Then
                        ChartHeaterProcessList(core_index).CurrentStatus = HeaterProcess._WAITING
                    ElseIf ChartHeaterProcessList(core_index).CurrentStatus = HeaterProcess._RUNNING Then
                        still_running = True
                    End If
                Next
                If still_running Then
                    Continue Do
                Else
                    Exit Do
                End If
            Loop
#End If
        Next

        Dim still_running As Boolean
        Do
            Threading.Thread.Sleep(20)
            still_running = False
            'My.Application.DoEvents()
            For core_index As Integer = 0 To ChartHeaterProcessList.Count - 1
                If ChartHeaterProcessList(core_index).CurrentStatus = ChartHeaterProcess._NOT_RUN Then
                    ChartHeaterProcessList(core_index).CurrentStatus = ChartHeaterProcess._WAITING
                ElseIf ChartHeaterProcessList(core_index).CurrentStatus = ChartHeaterProcess._RUNNING Then
                    still_running = True
                End If
            Next
            If still_running Then
                Continue Do
            Else
                Exit Do
            End If
        Loop

        ChartHeaterProcessList.Clear()       '종료시 Process 클래스 폐기함
    End Sub

End Class
'시뮬레이션 하는데 쓰인다.
Public Class ChartHeaterProcess
    Private Shared _WORK_COUNT As Integer = 0
    Private Shared _NUMBER_OF_SHAKE As Integer = 8
    Public Shared _NOT_RUN As Integer = 0
    Public Shared _WAITING As Integer = 1
    Public Shared _RUNNING As Integer = 2
    Private _CPULoadControl As Integer
    Public HeaterThread As New Threading.Thread(AddressOf HeaterProcess)
    Public CurrentStatus As Integer
    Public HeatSwitch As Boolean = False
    Public WithEvents HeatTimer As New System.Timers.Timer()
    Public TimeIndex As Integer
    Public OnTimeList(9) As Integer
    Public OffTimeList(9) As Integer
    Public random_gen As New Random
    Public DBResult As OleDb.OleDbDataReader
    Public SymbolObj As c03_Symbol
    Public DBSupporter As c04_DBSupport
    Public ReadCmd As New OleDb.OleDbCommand()
    Public DBConnection As OleDb.OleDbConnection
    Public ThreadNumber As Integer

    Public Property CPULoadControl() As Integer
        Get
            Return _CPULoadControl
        End Get
        Set(value As Integer)
            _CPULoadControl = value
            'On time calculation
            Dim total_on_time As Integer = value * 10
            Dim min_on_piece As Integer = (value \ 10) * 10
            Dim jatoori_on_time As Integer = total_on_time - min_on_piece * 10
            For index As Integer = 0 To jatoori_on_time / 10 - 1
                OnTimeList(index) = (min_on_piece + 10)
            Next
            For index As Integer = 0 To 10 - jatoori_on_time / 10 - 1
                OnTimeList(index + jatoori_on_time / 10) = (min_on_piece)
            Next

            'Off time calculation
            Dim total_off_time As Integer = (100 - value) * 10
            Dim min_off_piece As Integer = ((100 - value) \ 10) * 10
            Dim jatoori_off_time As Integer = total_off_time - min_off_piece * 10
            For index As Integer = 0 To jatoori_off_time / 10 - 1
                OffTimeList(index) = (min_off_piece + 10)
            Next
            For index As Integer = 0 To 10 - jatoori_off_time / 10 - 1
                OffTimeList(index + jatoori_off_time / 10) = (min_off_piece)
            Next

            'Shake it!
            Dim shake_a, shake_b As Integer
            Dim temp_time As Integer
            For index As Integer = 0 To _NUMBER_OF_SHAKE - 1
                shake_a = Math.Floor(random_gen.NextDouble * 10)
                shake_b = Math.Floor(random_gen.NextDouble * 10)
                temp_time = OnTimeList(shake_a)
                OnTimeList(shake_a) = OnTimeList(shake_b)
                OnTimeList(shake_b) = temp_time

                shake_a = Math.Floor(random_gen.NextDouble * 10)
                shake_b = Math.Floor(random_gen.NextDouble * 10)
                temp_time = OffTimeList(shake_b)
                OffTimeList(shake_a) = OffTimeList(shake_b)
                OffTimeList(shake_b) = temp_time
            Next
        End Set
    End Property

    Public Sub New()
        HeaterThread.IsBackground = True 'set the process to background
    End Sub

    Public Sub ProcessStart(cpu_load_control As Integer)
        CPULoadControl = cpu_load_control
        CurrentStatus = _WAITING
        HeaterThread.Start()        '그냥 빈 거를 일단 돌린다
    End Sub

    Private Sub HeaterProcess()
        Dim record_count As Integer
        Dim old_date As Date
        Dim sample_date As Date

        While 1
            If CurrentStatus = _NOT_RUN Then
                While CurrentStatus = _NOT_RUN
                    Threading.Thread.Sleep(1)
                End While
            ElseIf CurrentStatus = _WAITING Then
                While CurrentStatus = _WAITING
                    Threading.Thread.Sleep(20)
                    If MainForm.IsThreadExecuting = False Then
                        '150807 : 한 아이터레이션 종료시 모든 Process 종료되는게 확인되어야 한다. 이 클래스도 같이 버려져야 한다.
                        Exit Sub
                    End If
                End While
            Else 'if CurrentStatus = _RUNNING Then
                '일이 있게 되면 이쪽 밑으로 내려 온다.
		'2020.09.12: Try 이거 없앤 거는 exception error 잡으려고 없앴다.
                'Try
                ReadCmd.Connection = DBConnection
                    DBResult = ReadCmd.ExecuteReader()
                    HeatTimer.Interval = OnTimeList(TimeIndex)
                    HeatSwitch = True
                    HeatTimer.Start()
                    record_count = 0
                    old_date = [Date].MinValue

                    While DBResult.Read()
                        If record_count = 0 Then
                            '첫 데이타이면
                            SymbolObj.Initialize()         '이니셜라이즈 => 데이터 처음부터 다시 받기
                            'SymbolObj.CandleStartTime = CType(DBResult(0), DateTime)  '시작시간 기록하기
                            SymbolObj.StartTime = CType(DBResult(0), DateTime)  '시작시간 기록하기
                        End If
                        sample_date = CType(DBResult(0), DateTime).Date
                        If sample_date <> old_date Then
                            CollectSimulateDate(sample_date)
                            SymbolObj.UpdateStability()
                        End If
                        old_date = sample_date

                        'Candle data 뿌리기
                        SymbolObj.SetNewCandle(CType(DBResult(0), DateTime), CType(DBResult(1), UInt32), CType(DBResult(2), UInt32), CType(DBResult(3), UInt32), CType(DBResult(4), UInt32), CType(DBResult(5), UInt64))  '데이타 뿌리기

                        record_count += 1

                        If HeatSwitch = True Then
                            'keep running
                            'MessageLogging("Thread," & ThreadNumber.ToString & " keep running")
                        Else
                            'MessageLogging("Thread," & ThreadNumber.ToString & " enter sleeping")
                            'sleep for a while
                            'My.Application.DoEvents()
                            Threading.Thread.Sleep(OffTimeList(TimeIndex))
                            'MessageLogging("Thread," & ThreadNumber.ToString & " WAKEUP")
                            TimeIndex = (TimeIndex + 1) Mod 10
                            If OnTimeList(TimeIndex) = 0 Then
                                Continue While
                            Else
                                HeatTimer.Interval = OnTimeList(TimeIndex)
                                HeatSwitch = True
                                HeatTimer.Start()
                            End If
                        End If
                        'My.Application.DoEvents()
                    End While
                    DBResult.Close()
                'Catch ex As Exception
                'MessageLogging(SymbolObj.Code & ":" & SymbolObj.Name & " DB 없나보네요. " & ex.Message)
                'End Try

                'DBConnection.Close()

                HeatTimer.Stop()
                CurrentStatus = _NOT_RUN
            End If
        End While
    End Sub

    Private Sub HeatTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles HeatTimer.Elapsed
        HeatSwitch = False
    End Sub

End Class
#End If