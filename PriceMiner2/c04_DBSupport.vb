Imports DSCBO1Lib
'Imports CPUTILLib
'Imports ADOX

'Public Structure TableStatusStr
'Dim Code As String
'Dim DoesTableExist As Boolean
'End Structure

Public MustInherit Class c04_DBSupport
    'MustOverride Sub Simulate()
    Public ProgressMax As Integer
    Public ProgressValue As Integer
    Public DBProgressMax As Integer
    Public DBProgressValue As Integer
    'MustOverride Sub SetCpuLoadControl(ByVal accel_value As Integer)
End Class

Public Class c041_DefaultDBSupport
    Inherits c04_DBSupport
    'Public TableStatus As New List(Of TableStatusStr)
    Private _TableNameToCheck As String
    Private _DB_connection As OleDb.OleDbConnection '= New OleDb.OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=D:\\Finance\\Database\\StockPrice.accdb;")
    'Public NumberOfSaved As Integer
    Public SaveFinished As Boolean
    Public DBStatusOK As Boolean
    'Public ProgressMax As Integer
    'Public ProgressValue As Integer

    Public Sub New()
        '날짜를 보고 DB를 생성하든지 읽어오든지 한다.
        'Dim year_folder As String = Today.Year & "\"
#If NO_GANGDO_DB Then
        Dim db_name As String = "PriceNewDB_" & Today.Year & Today.Month.ToString("D2")
#Else
        Dim db_name As String = "PriceGangdoDB_" & Today.Year & Today.Month.ToString("D2")
#End If
        Dim DoesDBExist As Boolean = False
        '해당 DB가 등록되어 있나 확인
        _DB_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;" & "Initial Catalog=" & db_name)
        Try
            _DB_connection.Open()

            'DB 존재함
            DoesDBExist = True
        Catch ex As Exception
            'DB 존재하지 않음
            DoesDBExist = False
        End Try

        If Not DoesDBExist Then
            'DB 생성


            _DB_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;")
            Try
                'master DB 접속
                _DB_connection.Open()

                '연도 폴더 생성되었는지 확인
                ''''''''''''''''''''''''''''''''''''''

                Dim Str As String = "CREATE DATABASE " & db_name & " ON PRIMARY " &
                      "(NAME = " & db_name & "_main, " &
                      " FILENAME = '" & DB_FOLDER & db_name & "_main.mdf', " &
                      " FILEGROWTH = 10%) " &
                      " LOG ON " &
                      "(NAME = " & db_name & "_log, " &
                      " FILENAME = '" & DB_FOLDER & db_name & "_log.ldf', " &
                      " FILEGROWTH = 10%) "
                Dim cmd As New OleDb.OleDbCommand(Str, _DB_connection)
                cmd.CommandTimeout = 60 '2023.10.25 : 시간만료 에러가 뜨는 경우가 있어 timeout 값을 default 30 초에서 60초로 바꿈
                cmd.ExecuteNonQuery()

            Catch ex As Exception
                'DB 생성에 실패했습니다.
                MsgBox("DB 생성에 실패했습니다. 관리자에게 문의하세요.", MsgBoxStyle.Critical)
                DBStatusOK = False
                _DB_connection.Close()              'DB connection 닫기
                Exit Sub
            End Try

            _DB_connection.Close()              'DB connection 닫기

            'DB 다시 열기 시도
            _DB_connection = New OleDb.OleDbConnection("Provider=SQLOLEDB;Data Source=" & PCName & ";Integrated Security=SSPI;" & "Initial Catalog=" & db_name)

            Try
                _DB_connection.Open()
                DoesDBExist = True
            Catch ex As Exception
                '생성에 성공했지만 여는데 실패.. why?
            End Try
        End If
        _DB_connection.Close()              'DB connection 닫기
        DBStatusOK = True
    End Sub

    '종목리스트에 있는 모든 종목들에 대해 Table이 있는지 조사한다.
    Public Sub UpdateDBStatus()
        Dim table_name_list As New List(Of String)
        Try
            '_DB_connection.Open()

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

        '_DB_connection.Close()
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
        Try
            Dim save_thread As Threading.Thread = New Threading.Thread(AddressOf SavePriceInfomationThread)

            'NumberOfSaved = 0      '저장된 종목갯수 초기화
            MainForm.ProgressText1 = ""
            MainForm.ProgressText2 = ""
            MainForm.ProgressText3 = ""
            SaveFinished = False       '저장 완료 플래그 초기화

            MainForm.pb_Progress.Maximum = SymbolList.Count       'status bar 초기화

            'SavePriceInfomationThread()
            save_thread.Start()         '저장 쓰레드 시작

            'While Not _SaveFinished
            'MainForm.pb_Progress.Value = _NumberOfSaved
            'MainForm.Text = _NumberOfSaved & "/" & SymbolList.Count
            'Threading.Thread.Sleep(100)
            'End While
        Catch ex As Exception
            MsgBox("DB 저장에 실패했습니다. - " & ex.Message, MsgBoxStyle.Critical)
        End Try
    End Sub

    Private Sub SavePriceInfomationThread()
        _DB_connection.Open()

        UpdateDBStatus()        '종목별 table 존재 여부 업데이트

        For index As Integer = 0 To SymbolList.Count - 1
            '모든 종목에 대해 작업
            SymbolList(index).SaveToDB(_DB_connection)
            'NumberOfSaved = index              '저장된 종목수 업데이트
            MainForm.ProgressText1 = "저장, " & index & " / " & SymbolList.Count
            'MainForm.pb_Progress.Value = _NumberOfSaved
        Next

        SaveFinished = True
        MainForm.IsThreadExecuting = False
    End Sub

End Class
