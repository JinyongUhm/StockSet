Imports DSCBO1Lib
Imports CPUTILLib
Imports CPSYSDIBLib

Public Class et5_MarketEye_Wrapper
    '0: 종목코드, 4: 현재가, 10: 거래량, 17: 종목명, 22: 전일거래량, 23: 전일종가
    Private Const _MARKEYEYE_INDEX_SYMBOL_CODE As UInt32 = 0            '종목코드
    Private Const _MARKEYEYE_INDEX_NOW_PRICE As UInt32 = 4              '현재가
    Private Const _MARKETEYE_INDEX_AMOUNT As UInt32 = 10                '거래량
    Private Const _MARKETEYE_INDEX_SYMBOL_NAME As UInt32 = 17           '종목명
    Private Const _MARKETEYE_INDEX_YESTERDAY_AMOUNT As UInt32 = 22      '전일거래량
    Private Const _MARKETEYE_INDEX_YESTERDAY_PRICE As UInt32 = 23       '전일종가
    Private Const _MARKETEYE_INDEX_GANGDO As UInt32 = 24                '체결강도

    Public WrapperIndex_SymbolCode As Integer
    Public WrapperIndex_NowPrice As Integer
    Public WrapperIndex_Amount As Integer
    Public WrapperIndex_SymbolName As Integer
    Public WrapperIndex_YesterdayAmount As Integer
    Public WrapperIndex_YesterdayPrice As Integer
    Public WrapperIndex_Gangdo As Integer

    'Public WithEvents COMObj As MarketEye
    Public COMObj As MarketEye
    Public Shared RequestKey As TracingKey
    Public Shared NumberOfRequest As Integer
    Public Event Received()

    Public Sub New()
        SafeEnterTrace(RequestKey, 1)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj = New MarketEye
        SafeLeaveTrace(RequestKey, 101)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    '네트웍 문제 등으로 어디 쳐박혀 있을 때 리셋해주기 위함
    Public Shared Sub ResetRequestKey()
        SafeLeaveTrace(RequestKey, 215)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
#If 0 Then
    Public Sub SetInputValue(ByVal set_input_value_param1 As Integer, ByVal set_input_value_param2 As String)
        SafeEnterTrace(RequestKey, 2)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj.SetInputValue(set_input_value_param1, set_input_value_param2)
        SafeLeaveTrace(RequestKey, 102)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub Request()
        SafeEnterTrace(RequestKey, 3)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj.Request()
        NumberOfRequest = NumberOfRequest + 1
        SafeLeaveTrace(RequestKey, 103)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
    Public Sub BlockRequest()
        'SafeEnterTrace(RequestKey, 9)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj.BlockRequest2(1)
        ' NumberOfRequest = NumberOfRequest + 1
        'SafeLeaveTrace(RequestKey, 109)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
    Public Sub SetCodeAndRequest(ByVal all_code As String)
        SafeEnterTrace(RequestKey, 4)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj.SetInputValue(0, all_code)
        NumberOfRequest = NumberOfRequest + 1
        COMObj.Request()
        SafeLeaveTrace(RequestKey, 104)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
#End If
    '180120: 요청량 최소한으로 줄이기 연구
    '160714: 대신증권 홈페이지 들어가서 마켓아이 예제좀 찾아본다. 필드 배열을 어떻게 넘기지?
    Public Sub SetCodeAndBlockRequest(ByVal all_code As String, ByVal is_symbol_collection_mode As Boolean)
        Dim fields() As Object
        If is_symbol_collection_mode Then
            '180120: 처음 시작할 때 심볼 정보 모을 때는 여러 정보가 필요하다.
            fields = {0, 4, 10, 17, 22, 23}  '0: 종목코드, 4: 현재가, 10: 거래량, 17: 종목명, 22: 전일거래량, 23: 전일종가
            WrapperIndex_SymbolCode = 0
            WrapperIndex_NowPrice = 1
            WrapperIndex_Amount = 2
            WrapperIndex_SymbolName = 3
            WrapperIndex_YesterdayAmount = 4
            WrapperIndex_YesterdayPrice = 5
            WrapperIndex_Gangdo = -1
        Else
            '180120: 그 다음부터는 현재가, 거래량, 체결강도 정보만 필요하다
            fields = {4, 10, 24}  '4: 현재가, 10: 거래량, 24: 체결강도
            WrapperIndex_SymbolCode = -1
            WrapperIndex_NowPrice = 0
            WrapperIndex_Amount = 1
            WrapperIndex_SymbolName = -1
            WrapperIndex_YesterdayAmount = -1
            WrapperIndex_YesterdayPrice = -1
            WrapperIndex_Gangdo = 2
        End If
        SafeEnterTrace(RequestKey, 12)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj.SetInputValue(0, fields)
        COMObj.SetInputValue(1, all_code.Split(","))
        COMObj.SetInputValue(2, 1)
        'NumberOfRequest = NumberOfRequest + 1
        COMObj.BlockRequest()
        'COMObj_Received()
        SafeLeaveTrace(RequestKey, 112)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Function GetDataValue(get_data_value_param1 As Integer, ByVal get_data_value_param2 As Integer) As String
        Dim return_string As String
        If get_data_value_param1 < 0 Then
            ErrorLogging("마켓아이 인덱스가 음수인 오류입니다.")
            Return "0"
        End If

        'SafeEnterTrace(RequestKey, 7)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        'return_string = COMObj.GetDataValue(get_data_value_param1, get_data_value_param2)
        Dim obj As Object
        Try
            obj = COMObj.GetDataValue(get_data_value_param1, get_data_value_param2)
            return_string = Convert.ToString(obj)
        Catch ex As Exception
            '2021.06.05: 오류나면 cp stuck을 set하고 자진 종료하여 Price Miner로 하여금 CybosLauncher를 재시작하게 만든다.
            ErrorLogging("닫히는익셉션2: " & ex.Message)
            CpStuckAction()
#If 0 Then
            ErrorLogging("오류번호1: " & ex.Message)
            If Not WarningNotified Then
                WarningLogging(Now.Year & "-" & Now.Month & "-" & Now.Day & "Err: " & ex.Message)
                WarningNotified = True
            End If
#End If
            return_string = "0"
        End Try

        'SafeLeaveTrace(RequestKey, 107)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return return_string
    End Function

    Public Function GetDibStatus() As Short
        Dim dib_status As Short
        SafeEnterTrace(RequestKey, 13)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        dib_status = COMObj.GetDibStatus
        SafeLeaveTrace(RequestKey, 113)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return dib_status
    End Function

    Public Function GetDibMsg1() As String
        Dim dib_msg1 As String
        SafeEnterTrace(RequestKey, 14)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        dib_msg1 = COMObj.GetDibMsg1
        SafeLeaveTrace(RequestKey, 114)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return dib_msg1
    End Function
#If 0 Then
    Private Sub COMObj_Received() Handles COMObj.Received
        'SafeEnterTrace(RequestKey, 5)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        ' NumberOfRequest = NumberOfRequest - 1
        'SafeLeaveTrace(RequestKey, 105)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        RaiseEvent Received()
    End Sub
#End If

End Class
