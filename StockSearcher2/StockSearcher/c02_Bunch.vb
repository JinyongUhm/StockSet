'Imports DSCBO1Lib
'Imports CPUTILLib

Public Class c02_Bunch
    Inherits List(Of c03_Symbol)

    'Public WithEvents COMObj As StockMst2
    Public AllCode As String

    Public Sub New(ByVal first_symbol)
        Add(first_symbol)           '첫번째 종목을 더한다.
    End Sub

    '없어버리자
    Public Sub Terminate()
        Clear()
    End Sub

#If 0 Then
    '종목이 다 정해지면 불리는 함수
    Public Sub SymbolListFix()
        '붙임 코드 만들기
        AllCode = AllCodeFunction()

        'StockMst2 컴객체 만들기
        COMObj = New StockMst2()
        'COMObj.SetInputValue(0, AllCode)    '코드명 세팅

    End Sub

    'StockMst2 의 block request 부르기
    Public Sub Mst2BlockRequest()
        COMObj.SetInputValue(0, AllCode)    '코드명 세팅
        Try
            '140115 : GetDibStatus가 0(정상)이나 -1(오류) 일때만 가능함
            If COMObj.GetDibStatus() = 0 Then
                '현재 request 가능한 상태임
                '140612: 여기서 바로 에러나네 무슨 문제지? 그냥 Request고 BlockRequest고 똑같네... 뭘 바꿨지? => error log 너무 많이 기록해 과부하 걸려서 그랬음
                COMObj.BlockRequest()
                '140205 : RequestBlock 대신 Request를 쓰기로 결정되면서 공유문제를 신경써야 한다.
            Else
                '현재 응답을 기다리는 상태임
                'ErrorLogging(AllCode & " 요청 누락 한 번 발생했음!!!")

                '예전 데이터를 그냥 더미로 한 번 채운다
                COMObj_LimpHome()

                '컴객체는 버리고 다시 만들자
                COMObj = Nothing
                COMObj = New StockMst2()

                '새로 만든 객체에서 최초로 한 번 신청해본다.
                'COMObj.BlockRequest()
            End If
#If 1 Then
        Catch ex As Exception
            'SafeEnter(StoredMessagesKeyForDisplay)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            'ErrorLogging("에러!! - " & ex.Message)
            'MainForm.tb_Display.AppendText("에러!! - " & ex.Message)
            'SafeLeave(StoredMessagesKeyForDisplay)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

            '소속된 종목들은 모두 초기화한다.
            'For index As Integer = 0 To Count - 1
            'Item(index).Initialize()
            'Next

            '예전 데이터를 그냥 더미로 한 번 채운다
            COMObj_LimpHome()

            '컴객체는 버리고 다시 만들자
            COMObj = Nothing
            COMObj = New StockMst2()
        End Try
#End If
    End Sub

    Private Sub COMObj_Received() Handles COMObj.Received
        '주가정보 가져왔으면 각 종목 객체에 통보한다.
        Dim now_price As UInt32
        Dim amount As UInt64
        'Dim gangdo As Double
        For index As Integer = 0 To Count - 1
            now_price = GetNowPrice(index)
            amount = GetAmount(index)
            'gangdo = GetGangdo(index)
            'Item(index).SetNewData(now_price, amount, gangdo)
            Item(index).SetNewData(now_price, amount)
        Next
    End Sub

    Private Sub COMObj_LimpHome()
        'COMObj가 무슨 이유에서인지 고장났을 때 바로 전 데이터를 그냥 카피해서 이번 데이터인 것처럼 입력되게 한다.
        For index As Integer = 0 To Count - 1
            Item(index).SetLimpHomeData()
        Next
    End Sub

    'request한 결과에서 해당 인덱스의 종목코드 반환
    Public ReadOnly Property GetSymbolCode(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(0, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 종목명 반환
    Public ReadOnly Property GetSymbolName(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(1, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 현재가 반환
    Public ReadOnly Property GetNowPrice(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(3, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 현재가 반환
    Public ReadOnly Property GetSelPrice(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(9, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 현재가 반환
    Public ReadOnly Property GetBuyPrice(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(10, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 거래량 반환
    Public ReadOnly Property GetAmount(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(11, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 전일종가 반환
    Public ReadOnly Property GetYesterdayPrice(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(19, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 전일거래량 반환
    Public ReadOnly Property GetYesterdayAmount(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(20, index)
        End Get
    End Property
#End If


    'request한 결과에서 해당 인덱스의 체결강도 반환
    'Public ReadOnly Property GetGangdo(ByVal index As Integer) As String
    'Get
    'Return COMObj.GetDataValue(21, index)
    'End Get
    'End Property

    Private Function AllCodeFunction() As String
        Dim return_value As String = ""
        For index As Integer = 0 To Count - 1
            If index = 0 Then
                return_value = Item(0).Code
            Else
                return_value = return_value & "," & Item(index).Code          '코드 붙이기, 중간에 쉼표 있어야 됨
            End If
        Next
        Return return_value
    End Function

    'DB에 저장
    'Public Sub SaveToDB(ByVal db_connection As OleDb.OleDbConnection)
    'For index As Integer = 0 To Count - 1
    'Item(index).SaveToDB(db_connection)
    'Next
    'End Sub
End Class
