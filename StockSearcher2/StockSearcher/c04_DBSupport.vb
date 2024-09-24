Imports System.Data.SqlClient
'Imports DSCBO1Lib
'Imports CPUTILLib
'Imports ADOX

'Public Structure TableStatusStr
'Dim Code As String
'Dim DoesTableExist As Boolean
'End Structure

Public MustInherit Class c04_DBSupport
    '#If Not SIMULATION_PERIOD_IN_ARRAY Then
    MustOverride Sub Simulate(ByVal start_date As DateTime, ByVal end_date As DateTime)
    '#Else
    'MustOverride Sub Simulate()
    '#End If
    Public ProgressMax As Integer
    Public ProgressValue As Integer
    Public DBProgressMax As Integer
    Public DBProgressValue As Integer
    MustOverride Sub SetCpuLoadControl(ByVal accel_value As Integer)
End Class

Public Class c041_DefaultDBSupport
    Inherits c04_DBSupport
    'Public Delegate Sub Delegate_AttachTheCore(ByVal the_core As HeaterProcess)
    Public HeaterProcessList As New List(Of HeaterProcess)
    'Public TableStatus As New List(Of TableStatusStr)
    Private Shared _DB_FOLDER As String = "D:\Finance\Database\"
    Private Shared _CORE_COUNT As Integer = 22
    Public TREND_INDEX As Integer = 10
    Private _TableNameToCheck As String
    'Private _DB_connection As OleDb.OleDbConnection '= New OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\\Finance\\Database\\StockPrice.accdb;")
    Private _DB_connection As SqlConnection
    Private _NumberOfSaved As Integer
    Private _SaveFinished As Boolean
    Private _Initialized As Boolean = False
#If 0 Then
    Private _DBList() As String = { _
             "PriceFineDB_201401", _
             "PriceFineDB_201402", _
             "PriceFineDB_201403", _
             "PriceFineDB_201404", _
             "PriceFineDB_201405", _
             "PriceFineDB_201406", _
             "PriceFineDB_201407", _
             "PriceFineDB_201408", _
             "PriceFineDB_201409", _
             "PriceFineDB_201410", _
             "PriceFineDB_201411", _
             "PriceFineDB_201412" _
    }
#End If
    '"PriceDB_2012", _
    '"PriceDB_12_1" _
    'Public DBStatusOK As Boolean
    Public DateListToCollect As New List(Of Date)
    Public DateListKey As Integer
    Public DateCollecting As Boolean
    Public GlobalTrendData As New List(Of List(Of Double))
    'Public GlobalDeviationData As New List(Of List(Of Double))
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
        For index As Integer = 0 To HeaterProcessList.Count - 1
            HeaterProcessList(index).CPULoadControl = accel_value
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
        '        Try
        'Dim collect_date_thread As New Threading.Thread(AddressOf CollectSimulateDateByThreading)
        'DateCollecting = True
        'collect_date_thread.Start(Me)
        'Dim core_list As New List(Of TableProcess)

        For core_index As Integer = 0 To _CORE_COUNT - 1
            HeaterProcessList.Add(New HeaterProcess)
            HeaterProcessList(core_index).ProcessStart(MainForm.AccelValue)   '일단 프로세스 돌려놈
            HeaterProcessList(core_index).ThreadNumber = core_index
        Next

        If Not _Initialized Then
            _Initialized = True

            'global trend data 읽기
            Dim current_date As DateTime = start_date
            Dim trend_file_name As String ', deviation_file_name 
            Dim trend_file_contents() As String ', deviation_file_contents() 
            Dim trend_list As List(Of Double)       ', deviation_list 
            Do  '날짜에 대한 루프
                trend_file_name = "GlobalTrendData\GlobalTrend10_" & current_date.Year & current_date.Month.ToString("D2") & current_date.Day.ToString("D2") & ".txt"
                'deviation_file_name = "GlobalTrendData\GlobalDeviation" & current_date.Year & current_date.Month.ToString("D2") & current_date.Day.ToString("D2") & ".txt"
                If IO.File.Exists(trend_file_name) Then
                    trend_file_contents = IO.File.ReadAllLines(trend_file_name)
                    trend_list = New List(Of Double)
                    For index As Integer = 0 To trend_file_contents.Count - 1
                        '170922: Global trend 담는 double 변수 list 의 list, global deviation 담는 double 변수 list의 list, 그리고 날짜 정보를 담는 integer의 list 가 필요하다. 그리고 날짜 정보를 받아서 global trend data를 넘겨주는 함수도 필요하다.
                        trend_list.Add(Convert.ToDouble(trend_file_contents(index)))
                    Next
                    GlobalTrendData.Add(trend_list)
                    DateForGlobalData.Add(current_date)
                End If
#If 0 Then
                If IO.File.Exists(deviation_file_name) Then
                    deviation_file_contents = IO.File.ReadAllLines(deviation_file_name)
                    deviation_list = New List(Of Double)
                    For index As Integer = 0 To deviation_file_contents.Count - 1
                        deviation_list.Add(Convert.ToDouble(deviation_file_contents(index)))
                    Next
                    'GlobalDeviationData.Add(deviation_list)
                    DateForGlobalData.Add(current_date)
                End If
#End If
                current_date = current_date.AddDays(1)
            Loop While current_date <= end_date
            'If GlobalTrendData.Count <> GlobalDeviationData.Count Then
            'MsgBox("이거 뭐냐")
            'End If
        End If

        'MS SQL DB simulation
        Dim db_name As String
        Dim expected_db_list As New List(Of String)

        Dim read_cmd = New SqlCommand()
        'Dim read_cmd = New OleDb.OleDbCommand() '("SELECT * from TableName WHERE SampledTime BETWEEN @start_date AND @end_date", _DB_connection)
        read_cmd.Connection = _DB_connection
        'Dim read_result As OleDb.OleDbDataReader '= read_cmd.ExecuteReader()
        'query parameter 설정
        Dim start_date_str As String = start_date.Year & "-" & start_date.Month.ToString("D2") & "-" & start_date.Day.ToString("D2") & " 00:00:00"
        Dim end_date_str As String = end_date.Year & "-" & end_date.Month.ToString("D2") & "-" & end_date.Day.ToString("D2") & " 23:59:59"
        'make expected_db_list
        Dim this_year = start_date.Year
        Dim this_month = start_date.Month
        Do
            If (this_year = 2021 AndAlso this_month >= 10) OrElse (this_year > 2021) Then
                expected_db_list.Add("PriceNewDB_" & this_year.ToString("D2") & this_month.ToString("D2"))
            Else
                If GangdoDB Then
                    expected_db_list.Add("PriceGangdoDB_" & this_year.ToString("D2") & this_month.ToString("D2"))
                Else
                    expected_db_list.Add("PriceFineDB_" & this_year.ToString("D2") & this_month.ToString("D2"))
                End If
            End If
            this_month = this_month + 1
            If this_month > 12 Then
                this_month = 1
                this_year = this_year + 1
            End If
        Loop While this_year * 12 + this_month <= end_date.Year * 12 + end_date.Month


        'Dim current_year = start_year
        Dim table_name As String
        'Dim cp_code_mgr_obj As New CpCodeMgr
        Dim symbol_obj As c03_Symbol
        Dim symbol_name As String
        Dim command_text As String
        Dim the_core As HeaterProcess

        'end_date = end_date + TimeSpan.FromDays(1)              '종료일 + 1 (종료일까지 검색을 위해)

        DBProgressMax = expected_db_list.Count
        For index As Integer = 0 To expected_db_list.Count - 1
            DBProgressValue = index
            db_name = expected_db_list(index)    ' "PriceDB_" & "12_1" 'current_year.Year
            'initial catalog이용해 DB_Connection 다시 만들어야 한다.
            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            _DB_connection = New SqlConnection("Server=" & PCName & "; Database=" & db_name & ";Integrated Security=true; MultipleActiveResultSets=True;")
            '_DB_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Initial Catalog=" & db_name & "; Integrated Security=SSPI;")
            '_DB_connection = New OleDb.OleDbConnection("Provider=SQLNCLI10;Server=tcp:192.168.0.3; Database=" & db_name & ";UID=MyCOM\Duff; PWD=qlfkdjwth;")
            '_DB_connection = New OleDb.OleDbConnection("Provider=SQLNCLI10;Server=tcp:192.168.0.3; Database=" & db_name & ";UID=MyCOM\Duff; PWD=qlfkdjwth;")

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
            For core_index As Integer = 0 To HeaterProcessList.Count - 1
                HeaterProcessList(core_index).DBConnection = _DB_connection
            Next

            '모든 테이블에 대해 for문 돌린다.
            Dim data_table As DataTable = _DB_connection.GetSchema("tables")
            ProgressMax = data_table.Rows.Count         'progress max 설정
            ProgressValue = 0                           'progress value 초기화

            For table_index As Integer = 0 To data_table.Rows.Count - 1
                'table_name_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
                table_name = data_table.Rows(table_index).Item(2).ToString

                '테이블 이름이 종목코드와 같은 형식인지 검사(첫자가 A이고 길이가 7)
                If table_name(0) = "A" And table_name.Length = 7 Then
                    'If table_name = ",930" Then
                    '해당 테이블의 종목명을 알아본다.
                    'symbol_name = cp_code_mgr_obj.CodeToName(table_name)
                    'If symbol_name = "" Then
                    '종목명이 없다. => 상장폐지?
                    'symbol_name = table_name & "_폐지"
                    'End If
                    symbol_name = table_name        '이름 알려고 대신증권 서버를 물고 있어서 다른 프로그램이 실행이 안 되어서 이렇게 함.

                    '종목객체를 만들고 리스트에 추가한다.
                    '150708 : 아래에서 symbol 만들 때 바로 txt 파일 읽어서 symbol_obj 안에 갖고 있는 것이 좋을 듯 하다.
                    symbol_obj = AddSymbolIfNew(table_name, symbol_name)

                    'DB로부터 data 읽어온다.
                    'read_cmd.CommandText = ("DECLARE @start_date datetime = '" & start_date_str & "';DECLARE @end_date datetime = '" & end_date_str & "'; SELECT * from " & table_name & " WHERE SampledTime BETWEEN @start_date AND @end_date ORDER BY SampledTime ASC")
                    '사실 Access에서처럼 read_cmd.Parameters를 써도 될 것 같다. 그런데 아래와 같이 해도 된다는 것을 알고 있어라.
                    command_text = "SELECT * from " & table_name & " WHERE SampledTime BETWEEN '" & start_date_str & "' AND '" & end_date_str & "' ORDER BY SampledTime ASC"
                    'read_cmd = New OleDb.OleDbCommand("SELECT * from " & table_name & " WHERE SampledTime BETWEEN @start_date AND @end_date", _DB_connection)
                    'read_cmd.Parameters(0).Value = start_date
                    'read_cmd.Parameters(1).Value = end_date + TimeSpan.FromDays(1)

                    'available한 core가 있는지 알아본다.
                    Do
                        'My.Application.DoEvents()
                        'MessageLogging("MAIN task DO")
                        '2024.04.26 : 코어를 최대한 놀리지 않고 돌리기 위해 이 thread 는 sleep으로 못들어가게 한다.
                        'Threading.Thread.Sleep(1)
                        'My.Application.DoEvents()

                        For core_index As Integer = 0 To HeaterProcessList.Count - 1
                            If HeaterProcessList(core_index).CurrentStatus <> HeaterProcess._RUNNING Then
                                'HeaterProcessList.RemoveAt(core_index)
                                'the_core = New HeaterProcess
                                the_core = HeaterProcessList(core_index)
                                'HeaterProcessList.Insert(core_index, the_core)
                                Exit Do
                            End If
                        Next
                    Loop
                    'the_core.DBResult = read_result
                    the_core.DBConnection = _DB_connection
                    the_core.ReadCmd.CommandText = command_text
                    the_core.SymbolObj = symbol_obj
#If Not MOVING_AVERAGE_DIFFERENCE Then
                    the_core.DBSupporter = MainForm.DBSupporter
#End If

                    'Dim delegate_the_core As New Delegate_AttachTheCore(AddressOf AttachTheCore)
                    'MainForm.BeginInvoke(delegate_the_core, the_core)
                    'the_core.StartHeating(MainForm.AccelValueStored)
                    the_core.CurrentStatus = HeaterProcess._RUNNING           '쓰레드에게 일을 준다.
                    '170929: 이제는 core에서 날짜를 받아서 global trend data 얻는 부분을 해줘야 한다. 만들어 놓은 Get함수는 아마도 index를 반환하는 걸로 바꿔야 할 듯 하다.
#If 0 Then
                    '읽어온 data를 뿌린다.
                    record_count = 0
                    sample_time = [DateTime].MinValue       '샘플 타임 초기화
                    While read_result.Read()
                        If record_count = 0 OrElse read_result(0) <> sample_time + TimeSpan.FromSeconds(5) Then
                            '첫 데이타이거나 샘플타임이 5초 차이가 아니면
                            symbol_obj.Initialize()         '이니셜라이즈 => 데이터 처음부터 다시 받기
                            symbol_obj.StartTime = CType(read_result(0), DateTime)  '시작시간 기록하기
                            CollectSimulateDate(CType(read_result(0), DateTime).Date)
                        End If
                        sample_time = read_result(0)            '샘플타임 저장
                        'symbol_obj.SetNewData(CType(read_result(1), UInt32), CType(read_result(2), UInt64), CType(read_result(3), Double))  '데이타 뿌리기
                        symbol_obj.SetNewData(CType(read_result(1), UInt32), CType(read_result(2), UInt64))  '데이타 뿌리기

                        record_count += 1
                        My.Application.DoEvents()
                    End While
                    read_result.Close()
#End If
                    ProgressValue = table_index             'progress 값 update
                End If

            Next
            'DB 중지하기 전에 읽을 거 다 읽는다

            Dim still_running As Boolean
            Do
                Threading.Thread.Sleep(20)
                still_running = False
                'My.Application.DoEvents()
                For core_index As Integer = 0 To HeaterProcessList.Count - 1
                    If HeaterProcessList(core_index).CurrentStatus = HeaterProcess._NOT_RUN Then
                        HeaterProcessList(core_index).CurrentStatus = HeaterProcess._WAITING
                    ElseIf HeaterProcessList(core_index).CurrentStatus = HeaterProcess._RUNNING Then
                        still_running = True
                    End If
                Next
                If still_running Then
                    Continue Do
                Else
                    Exit Do
                End If
            Loop

            _DB_connection.Close()
        Next

        HeaterProcessList.Clear()       '종료시 Process 클래스 폐기함
    End Sub

    'GlobalTrend data 계산
    Public Sub GlobalTrending(ByVal start_date As DateTime, ByVal end_date As DateTime)
        'MS SQL DB simulation
        Dim db_name As String
        Dim old_db_name As String = ""
        Dim expected_db_list As New List(Of String)

        Dim read_cmd = New SqlCommand ' New OleDb.OleDbCommand() '("SELECT * from TableName WHERE SampledTime BETWEEN @start_date AND @end_date", _DB_connection)
        read_cmd.Connection = _DB_connection

        Dim current_date As DateTime = start_date
        Dim start_date_str, end_date_str As String
        Dim data_table As DataTable
        Dim table_list As New List(Of String)
        Dim table_name As String
        Dim command_text As String
        Dim ReadCmd As New SqlCommand 'OleDb.OleDbCommand()
        'Dim DBConnection As OleDb.OleDbConnection
        Dim DBResult As SqlDataReader 'OleDb.OleDbDataReader
        Dim symbol_list_of_price_data As New List(Of List(Of UInt32))
        Dim price_data As List(Of UInt32)
        Dim price_count As Integer
        'Dim count_sum_for_averaging As Integer
        Dim global_deviation As New List(Of Double)
        Dim global_trend As New List(Of Double)
        Dim trend_sum, nn, nn_1 As Double ', deviation_sum
        'Dim global_deviation_string As String = ""
        Dim global_trend_string As String = ""
        Do  '날짜에 대한 루프
            'query parameter 설정
            start_date_str = current_date.Year & "-" & current_date.Month.ToString("D2") & "-" & current_date.Day.ToString("D2") & " 00:00:00"
            end_date_str = current_date.Year & "-" & current_date.Month.ToString("D2") & "-" & current_date.Day.ToString("D2") & " 23:59:59"

            'DB 확정
            db_name = "PriceGangdoDB_" & current_date.Year.ToString("D2") & current_date.Month.ToString("D2")

            If db_name <> old_db_name Then
                If old_db_name <> "" Then
                    '최초만 아니면 전에 쓰던 DB close 한다.
                    _DB_connection.Close()
                End If
                'DB가 최초이거나 바뀌었으므로 열어야 한다.
                _DB_connection = New SqlConnection("Server=" & PCName & "; Database=" & db_name & ";Integrated Security=true; MultipleActiveResultSets=True;")
                '_DB_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & "; Initial Catalog=" & db_name & "; Integrated Security=SSPI;")
                '_DB_connection = New OleDb.OleDbConnection("Provider=SQLNCLI10;Server=tcp:192.168.0.3; Database=" & db_name & ";UID=MyCOM\Duff; PWD=qlfkdjwth;")
                '_DB_connection = New OleDb.OleDbConnection("Provider=SQLNCLI10;Server=tcp:192.168.0.3; Database=" & db_name & ";UID=MyCOM\Duff; PWD=qlfkdjwth;")

                _DB_connection.Open()
                read_cmd.Connection = _DB_connection

                '170825: 해당 DB의 table list를 만들어야 한다.
                data_table = _DB_connection.GetSchema("tables")
                table_list.Clear()
                For table_index As Integer = 0 To data_table.Rows.Count - 1
                    table_name = data_table.Rows(table_index).Item(2).ToString
                    If table_name(0) = "A" And table_name.Length = 7 Then
                        table_list.Add(table_name)
                    End If
                Next
            End If

            symbol_list_of_price_data.Clear()
            global_deviation.Clear()
            global_trend.Clear()
            'global_deviation_string = ""
            global_trend_string = ""

            '모든 테이블에 대해 for문 돌린다.
            For table_index As Integer = 0 To table_list.Count - 1
                '170829: 각 table에 대해 query 돌려서 가격 읽는다.
                table_name = table_list(table_index)
                '테이블 이름이 종목코드와 같은 형식인지 검사(첫자가 A이고 길이가 7)
                'symbol_name = table_name        '이름 알려고 대신증권 서버를 물고 있어서 다른 프로그램이 실행이 안 되어서 이렇게 함.

                ''종목객체를 만들고 리스트에 추가한다.
                'symbol_obj = AddSymbolIfNew(table_name, symbol_name)

                command_text = "SELECT * from " & table_name & " WHERE SampledTime BETWEEN '" & start_date_str & "' AND '" & end_date_str & "' ORDER BY SampledTime ASC"

                '170831: 쓰레드 내부에서 하는 작업 여기에 카피해다 놓고 작업하자.
                ReadCmd.CommandText = command_text

                ReadCmd.Connection = _DB_connection
                '170905: DBResult 이거 이어서 하자
                DBResult = ReadCmd.ExecuteReader()
                'sample_time = [DateTime].MinValue       '샘플 타임 초기화

                price_data = New List(Of UInt32)
                While DBResult.Read()

#If 0 Then
                    If record_count = 0 OrElse DBResult(0) <> sample_time + TimeSpan.FromSeconds(5) Then
                        '첫 데이타이거나 샘플타임이 5초 차이가 아니면
                        SymbolObj.Initialize()         '이니셜라이즈 => 데이터 처음부터 다시 받기
                        SymbolObj.StartTime = CType(DBResult(0), DateTime)  '시작시간 기록하기
                        CollectSimulateDate(CType(DBResult(0), DateTime).Date)
                    End If
#End If
                    price_data.Add(CType(DBResult(1), UInt32))
                    'sample_time = DBResult(0)            '샘플타임 저장
                    'symbol_obj.SetNewData(CType(read_result(1), UInt32), CType(read_result(2), UInt64), CType(read_result(3), Double))  '데이타 뿌리기
                    'SymbolObj.SetNewData(CType(DBResult(1), UInt32), CType(DBResult(2), UInt64))  '데이타 뿌리기

                    'record_count += 1
                End While
                If price_data.Count > 2 Then
                    symbol_list_of_price_data.Add(price_data)
                End If
                price_data = Nothing
                DBResult.Close()
            Next

            '가져온 심볼별 price_data로부터 global trend를 계산해낸다.
            '일단 price_data의 갯수를 파악한다.
            price_count = Integer.MaxValue
            'count_sum_for_averaging = 0
            For table_index As Integer = 0 To symbol_list_of_price_data.Count - 1
                price_count = Math.Min(price_count, symbol_list_of_price_data(table_index).Count)
                'count_sum_for_averaging += symbol_list_of_price_data(table_index).Count
            Next
            If price_count = Integer.MaxValue Then
                price_count = 0
            End If

            '이제 global trend 계산해보자.
            For time_index As Integer = 0 To price_count - 1
                'Global Deviation
#If 0 Then
                If time_index = 0 Then
                    global_deviation.Add(0)
                Else
                    deviation_sum = 0
                    For symbol_index As Integer = 0 To symbol_list_of_price_data.Count - 1
                        nn = symbol_list_of_price_data(symbol_index).Item(time_index)
                        nn_1 = symbol_list_of_price_data(symbol_index).Item(time_index - 1)
                        If nn_1 <> 0 Then
                            deviation_sum = deviation_sum + (((nn - nn_1) / nn_1) ^ 2)
                        End If
                    Next
                    global_deviation.Add(Math.Sqrt(deviation_sum / symbol_list_of_price_data.Count))
                    If global_deviation.Last = Double.NaN Then
                        Dim a = 1
                    End If
                End If
#End If

                'Global Trend
                If time_index < TREND_INDEX Then
                    global_trend.Add(0)
                Else
                    trend_sum = 0
                    For symbol_index As Integer = 0 To symbol_list_of_price_data.Count - 1
                        nn = symbol_list_of_price_data(symbol_index).Item(time_index)
                        nn_1 = symbol_list_of_price_data(symbol_index).Item(time_index - TREND_INDEX)
                        If nn_1 <> 0 Then
                            trend_sum = trend_sum + (nn - nn_1) / nn_1
                        End If
                    Next
                    global_trend.Add(trend_sum / symbol_list_of_price_data.Count)
                End If
            Next
            '파일에 쓰자.
            For data_index As Integer = 0 To global_trend.Count - 1
                'global_deviation_string = global_deviation_string & global_deviation(data_index).ToString & vbCrLf
                global_trend_string = global_trend_string & global_trend(data_index).ToString & vbCrLf
            Next
            'My.Computer.FileSystem.WriteAllText("GlobalDeviation" & current_date.Year & current_date.Month.ToString("D2") & current_date.Day.ToString("D2") & ".txt", global_deviation_string, False)
            My.Computer.FileSystem.WriteAllText("GlobalTrend" & TREND_INDEX.ToString & "_" & current_date.Year & current_date.Month.ToString("D2") & current_date.Day.ToString("D2") & ".txt", global_trend_string, False)
            '170912: 의도한 대로 나오기는 하는 것 같다. 결과 조금 더 살펴보고 유익한 가 살펴보고 진행하자.
            'ProgressValue = table_index             'progress 값 update

            '다음날
            old_db_name = db_name       'old db_name 백업
            current_date = current_date.AddDays(1)
        Loop While current_date <= end_date

    End Sub

    Public Function GetGlobalTrendIndex(the_date As Date) As Integer
        Dim top_date_index As Integer = 0
        Dim bottom_date_index As Integer = DateForGlobalData.Count - 1
        Dim mid As Integer
        While 1
            If DateForGlobalData(top_date_index) = the_date Then
                Return top_date_index
            End If
            If DateForGlobalData(bottom_date_index) = the_date Then
                Return bottom_date_index
            End If
            If top_date_index = bottom_date_index Then
                'MsgBox("찾는 날짜가 없는 것 같다")
                Return Nothing
            End If
            mid = (top_date_index + bottom_date_index) / 2
            If DateForGlobalData(mid) = the_date Then
                Return mid
            ElseIf DateForGlobalData(mid) > the_date Then
                bottom_date_index = mid
            Else
                top_date_index = mid
            End If
        End While
    End Function

    'Public Sub AttachTheCore(ByVal the_core As HeaterProcess)
    'the_core.StartHeating(MainForm.AccelValueStored)
    'End Sub
End Class

Public Class HeaterProcess
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
    Public DBResult As SqlDataReader 'OleDb.OleDbDataReader
    Public SymbolObj As c03_Symbol
    Public DBSupporter As c041_DefaultDBSupport
    Public ReadCmd As New SqlCommand ' OleDb.OleDbCommand()
    Public DBConnection As SqlConnection ' OleDb.OleDbConnection
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

#If 0 Then
    Public Sub StartHeating(cpu_load_control As Integer)
        CPULoadControl = cpu_load_control
        If HeaterThread IsNot Nothing AndAlso Not HeaterThread.IsAlive Then
            IsRun = True
            TimeIndex = 0
            HeatTimer.Interval = OnTimeList(TimeIndex)
            HeatSwitch = True
            HeatTimer.Start()
            HeaterThread.Start()
        End If
    End Sub
#End If

    Private Sub HeaterProcess()
#If 0 Then
        While 1
            If HeatSwitch = True Then
            Else
                'MessageLogging("Thread," & ThreadNumber.ToString & " enter sleeping")
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
#Else
        Dim record_count As Integer
        Dim sample_time As DateTime
        'Dim global_trend_list As List(Of Double) ', global_deviation_list 
        Dim global_trend As Double ', global_deviation 
        'Dim date_index As Integer
        Dim data_index As Integer = 0

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
                ReadCmd.Connection = DBConnection
                ReadCmd.CommandTimeout = 120     '2023.10.22 : 기본이 30초이고 timeout 에러가 자꾸나서 60초로 늘려놨다. 11월 21일 다시 120초로 늘렸다.
                DBResult = ReadCmd.ExecuteReader()
                HeatTimer.Interval = OnTimeList(TimeIndex)
                HeatSwitch = True
                HeatTimer.Start()
                record_count = 0
                sample_time = [DateTime].MinValue       '샘플 타임 초기화
                Dim old_date As Date

                While DBResult.Read()
                    'My.Application.DoEvents()
                    'Work to do
                    If record_count = 0 OrElse DBResult(0) <> sample_time + TimeSpan.FromSeconds(5) Then
                        '첫 데이타이거나 샘플타임이 5초 차이가 아니면
                        SymbolObj.Initialize()         '이니셜라이즈 => 데이터 처음부터 다시 받기
                        SymbolObj.StartTime = CType(DBResult(0), DateTime)  '시작시간 기록하기
                        SymbolObj.OpenPrice = CType(DBResult(1), UInt32)    '최초가 기록하기
                        SymbolObj.HighPrice = SymbolObj.OpenPrice           '최고가를 init하기
                        'SymbolObj.OpenPrice = SymbolObj.GetYesterPrice() 'CType(DBResult(1), UInt32)    '최초가 기록하기
                        CollectSimulateDate(CType(DBResult(0), DateTime).Date)
                        '170915: 이 위에 날짜 정보를 이용해서 global trend data 를 가져오자. 그 전에 파일에서 읽어와 변수로 읽어들이는 작업이 먼저 필요하겠지만..
                        old_date = CType(DBResult(0), DateTime).Date
#If 0 Then
#If Not MOVING_AVERAGE_DIFFERENCE Then
                        If DBSupporter.GlobalTrendData.Count > 0 Then
                            date_index = DBSupporter.GetGlobalTrendIndex(SymbolObj.StartTime.Date)
                            global_trend_list = DBSupporter.GlobalTrendData(date_index)
                        End If
#End If
#End If
                        'global_deviation_list = DBSupporter.GlobalDeviationData(date_index)
                        '171013: SetNewData에 global data를 넘겨주는데 바로 전 timing 것을 넘겨준다.
                        data_index = 0
                    End If
                    sample_time = DBResult(0)            '샘플타임 저장
                    'symbol_obj.SetNewData(CType(read_result(1), UInt32), CType(read_result(2), UInt64), CType(read_result(3), Double))  '데이타 뿌리기
                    If data_index = 0 Then
                        '-1 timing의 data는 available하지 않으므로 아래의 data를 넘겨줌
                        global_trend = CType(DBResult(1), UInt32)
                        'global_deviation = 0
                    Else
                        '현시점 -1 timing의 데이터를 넘겨줌
                        'If data_index - 1 > global_trend_list.Count - 1 Then
                        '15년 9월 16일 말고 또 이런 경우가 있나?
                        'global_trend = 0
                        'global_deviation = 0
                        'Else
                        'global_trend = global_trend_list(data_index - 1)
                        'global_deviation = global_deviation_list(data_index - 1)
                        'End If
                    End If
                    If GangdoDB Then
                        SymbolObj.SetNewData(CType(DBResult(1), UInt32), CType(DBResult(2), UInt64), global_trend)  '데이타 뿌리기 ,, global_deviation
                        'SymbolObj.SetNewData(CType(DBResult(1), UInt32), CType(DBResult(2), UInt64), CType(DBResult(3), Double), global_trend)  '데이타 뿌리기 ,, global_deviation
                    Else
                        'SymbolObj.SetNewData(CType(DBResult(1), UInt32), CType(DBResult(2), UInt64), 0) ', global_trend, global_deviation)  '데이타 뿌리기
                    End If
                    data_index += 1
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

                HeatTimer.Stop()
                If MainForm.DBIntervalStored > 0 Then
                    Threading.Thread.Sleep(20 * MainForm.DBIntervalStored)
                End If
                CurrentStatus = _NOT_RUN
            End If
        End While
#End If
    End Sub

    Private Sub HeatTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles HeatTimer.Elapsed
        HeatSwitch = False
    End Sub

End Class
#If 0 Then
Public Class TableProcess
    Public TableThread As Threading.Thread
    Public IsRunning As Boolean
    Public DBResult As OleDb.OleDbDataReader
    Public SymbolObj As c03_Symbol
    Public ReadCmd As New OleDb.OleDbCommand()
    Public DBConnection As OleDb.OleDbConnection
    Private WithEvents BreakTimer As New System.Timers.Timer(60)
    Private IsBreakTime As Boolean

    Public Sub StartTheThread()
        IsRunning = True
        TableThread = New Threading.Thread(AddressOf TableProcess)
        TableThread.IsBackground = True
        BreakTimer.Start()
        TableThread.Start()
    End Sub

    Public Sub TableProcess()
        Dim record_count As Integer
        Dim sample_time As DateTime

        ReadCmd.Connection = DBConnection
        DBResult = ReadCmd.ExecuteReader()

        record_count = 0
        sample_time = [DateTime].MinValue       '샘플 타임 초기화
        While DBResult.Read()
            If record_count = 0 OrElse DBResult(0) <> sample_time + TimeSpan.FromSeconds(5) Then
                '첫 데이타이거나 샘플타임이 5초 차이가 아니면
                SymbolObj.Initialize()         '이니셜라이즈 => 데이터 처음부터 다시 받기
                SymbolObj.StartTime = CType(DBResult(0), DateTime)  '시작시간 기록하기
                CollectSimulateDate(CType(DBResult(0), DateTime).Date)
            End If
            sample_time = DBResult(0)            '샘플타임 저장
            'symbol_obj.SetNewData(CType(read_result(1), UInt32), CType(read_result(2), UInt64), CType(read_result(3), Double))  '데이타 뿌리기
            SymbolObj.SetNewData(CType(DBResult(1), UInt32), CType(DBResult(2), UInt64))  '데이타 뿌리기

            record_count += 1
            If IsBreakTime Then
                My.Application.DoEvents()
                IsBreakTime = False
                Threading.Thread.Sleep(1)
            End If
        End While
        DBResult.Close()

        IsRunning = False
    End Sub


    Private Sub BreakTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles BreakTimer.Elapsed
        IsBreakTime = True
        BreakTimer.Start()
    End Sub
End Class
#End If