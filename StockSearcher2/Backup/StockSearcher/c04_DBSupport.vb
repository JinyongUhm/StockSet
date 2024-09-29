Imports DSCBO1Lib
Imports CPUTILLib
Imports ADOX

'Public Structure TableStatusStr
'Dim Code As String
'Dim DoesTableExist As Boolean
'End Structure

Public Class c04_DBSupport
    'Public TableStatus As New List(Of TableStatusStr)
    Private _TableNameToCheck As String
    Private _DB_connection As OleDb.OleDbConnection = New OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\\Finance\\Database\\StockPrice.accdb;")
    Private _NumberOfSaved As Integer
    Private _SaveFinished As Boolean
    Public ProgressMax As Integer
    Public ProgressValue As Integer

    '종목리스트에 있는 모든 종목들에 대해 Table이 있는지 조사한다.
    Public Sub UpdateDBStatus()
        Dim table_name_list As New List(Of String)
        Try
            _DB_connection.Open()

            '테이블 이름 리스트를 만든다.
            Dim data_table As DataTable = _DB_connection.GetSchema("tables")
            For table_index As Integer = 0 To data_table.Rows.Count - 1
                table_name_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
            Next

            '종목리스트에 대해 for문 돌린다.
            For index As Integer = 0 To SymbolList.Count - 1
                _TableNameToCheck = SymbolList.Item(index).Code     '비교위한 전역변수 세팅
                If table_name_list.Exists(AddressOf TableExistCheck) Then
                    '테이블 존재
                    SymbolList.Item(index).DBTableExist = True
                Else
                    '테이블 존재하지 않음
                    SymbolList.Item(index).DBTableExist = False
                End If
            Next

        Catch ex As Exception
            MsgBox("DB 접근에 실패했습니다. - " & ex.Message, MsgBoxStyle.Critical)
        End Try

        _DB_connection.Close()
    End Sub

    'table list에 있는지 체크
    Private Function TableExistCheck(ByVal a_string As String) As Boolean
        If a_string = _TableNameToCheck Then
            Return True
        Else
            Return False
        End If
    End Function

    '얻어진 가격정보 저장
    Public Sub SavePriceInformation()
        'Try
        'Dim save_thread As Threading.Thread = New Threading.Thread(AddressOf SavePriceInfomationThread)

        _NumberOfSaved = 0      '저장된 종목갯수 초기화
        _SaveFinished = False       '저장 완료 플래그 초기화

        MainForm.pb_Progress.Maximum = SymbolList.Count       'status bar 초기화

        SavePriceInfomationThread()
        'save_thread.Start()         '저장 쓰레드 시작

        'While Not _SaveFinished
        'MainForm.pb_Progress.Value = _NumberOfSaved
        'MainForm.Text = _NumberOfSaved & "/" & SymbolList.Count
        'Threading.Thread.Sleep(100)
        'End While
        'Catch ex As Exception
        'MsgBox("DB 저장에 실패했습니다. - " & ex.Message, MsgBoxStyle.Critical)
        'End Try
    End Sub

    Private Sub SavePriceInfomationThread()
        _DB_connection.Open()

        For index As Integer = 0 To SymbolList.Count - 1
            '모든 종목에 대해 작업
            SymbolList(index).SaveToDB(_DB_connection)
            _NumberOfSaved = index              '저장된 종목수 업데이트
            MainForm.pb_Progress.Value = _NumberOfSaved
        Next
        _SaveFinished = true
    End Sub

    '시뮬레이션
    Public Sub Simulate(ByVal start_date As DateTime, ByVal end_date As DateTime)
        '        Try
        Dim cp_code_mgr_obj As New CpCodeMgr
        Dim symbol_obj As c03_Symbol
        Dim record_count As Integer
        'Dim table_name_list As New List(Of String)
        Dim table_name As String
        Dim symbol_name As String
        Dim read_cmd As New OleDb.OleDbCommand("SELECT * from TableName WHERE SampledTime BETWEEN @start_date AND @end_date", _DB_connection)
        Dim read_result As OleDb.OleDbDataReader
        Dim sample_time As DateTime

        _DB_connection.Open()

        'query parameter 설정
        read_cmd.Parameters.Add("@start_date", OleDb.OleDbType.Date, 8)
        read_cmd.Parameters.Add("@end_date", OleDb.OleDbType.Date, 8)

        '모든 테이블에 대해 for문 돌린다.
        Dim data_table As DataTable = _DB_connection.GetSchema("tables")
        ProgressMax = data_table.Rows.Count         'progress max 설정
        ProgressValue = 0                           'progress value 초기화
        For table_index As Integer = 0 To data_table.Rows.Count - 1
            'table_name_list.Add(data_table.Rows(table_index).Item(2).ToString)           '2번째 컬럼이 TABLE_NAME이다.
            table_name = data_table.Rows(table_index).Item(2).ToString

            '테이블 이름이 종목코드와 같은 형식인지 검사(첫자가 A이고 길이가 7)
            If table_name(0) = "A" And table_name.Length = 7 Then
                '해당 테이블의 종목명을 알아본다.
                symbol_name = cp_code_mgr_obj.CodeToName(table_name)
                If symbol_name = "" Then
                    '종목명이 없다. => 상장폐지?
                    symbol_name = table_name & "_폐지"
                End If
                '종목객체를 만들고 리스트에 추가한다.
                symbol_obj = New c03_Symbol(table_name, symbol_name)    '종목 개체 생성
                symbol_obj.DBTableExist = True
                SymbolList.Add(symbol_obj)                                     '종목리스트에 추가

                'DB로부터 data 읽어온다.
                read_cmd.CommandText = "SELECT * from " & table_name & " WHERE SampledTime BETWEEN @start_date AND @end_date ORDER BY SampledTime ASC"
                'read_cmd = New OleDb.OleDbCommand("SELECT * from " & table_name & " WHERE SampledTime BETWEEN @start_date AND @end_date", _DB_connection)
                read_cmd.Parameters(0).Value = start_date
                read_cmd.Parameters(1).Value = end_date + TimeSpan.FromDays(1)
                read_result = read_cmd.ExecuteReader()
                '읽어온 data를 뿌린다.
                record_count = 0
                sample_time = [DateTime].MinValue       '샘플 타임 초기화
                While read_result.Read()
                    If record_count = 0 OrElse read_result(0) <> sample_time + TimeSpan.FromSeconds(15) Then
                        '첫 데이타이거나 샘플타임이 15초 차이가 아니면
                        symbol_obj.Initialize()         '이니셜라이즈 => 데이터 처음부터 다시 받기
                        symbol_obj.StartTime = CType(read_result(0), DateTime)  '시작시간 기록하기
                    End If
                    sample_time = read_result(0)            '샘플타임 저장
                    symbol_obj.SetNewData(CType(read_result(1), UInt32), CType(read_result(2), UInt64), CType(read_result(3), Double))  '데이타 뿌리기
                    record_count += 1
                End While
                read_result.Close()
                ProgressValue = table_index             'progress 값 update
            End If

        Next
        _DB_connection.Close()

        '        Catch ex As Exception
        'MsgBox("DB 열기에 실패했습니다. - " & ex.Message, MsgBoxStyle.Critical)

        'End Try

    End Sub
End Class
