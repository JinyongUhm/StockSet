Imports DSCBO1Lib
Imports CPUTILLib

Public Class c02_Bunch
    Inherits List(Of c03_Symbol)

    Public COMObj As StockMst2
    Public AllCode As String

    Public Sub New(ByVal first_symbol)
        Add(first_symbol)           '첫번째 종목을 더한다.
    End Sub

    '없어버리자
    Public Sub Terminate()
        Clear()
    End Sub

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
            COMObj.BlockRequest()

            '주가정보 가져왔으면 각 종목 객체에 통보한다.
            Dim now_price As UInt32
            Dim amount As UInt64
            Dim gangdo As Double
            For index As Integer = 0 To Count - 1
                now_price = GetNowPrice(index)
                amount = GetAmount(index)
                gangdo = GetGangdo(index)

                Item(index).SetNewData(now_price, amount, gangdo)
            Next
#If 1 Then
        Catch ex As Exception
            MainForm.tb_Display.AppendText("에러!! - " & ex.Message)

            '소속된 종목들은 모두 초기화한다.
            For index As Integer = 0 To Count - 1
                Item(index).Initialize()
            Next
            '컴객체는 버리고 다시 만들자
            COMObj = Nothing
            COMObj = New StockMst2()
        End Try
#End If
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

    'request한 결과에서 해당 인덱스의 체결강도 반환
    Public ReadOnly Property GetGangdo(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(21, index)
        End Get
    End Property

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
