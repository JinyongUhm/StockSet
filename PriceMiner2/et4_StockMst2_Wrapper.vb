Imports DSCBO1Lib
Imports CPUTILLib

Public Class et4_StockMst2_Wrapper
    Public WithEvents COMObj As StockMst2
    Public Shared RequestKey As TracingKey
    Public Shared NumberOfRequest As Integer
    Public Event Received()

    Public Sub New()
        SafeEnterTrace(RequestKey, 1)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj = New StockMst2
        SafeLeaveTrace(RequestKey, 101)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
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

    Public Sub SetCodeAndBlockRequest(ByVal all_code As String)
        SafeEnterTrace(RequestKey, 12)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        COMObj.SetInputValue(0, all_code)
        'NumberOfRequest = NumberOfRequest + 1
        COMObj.BlockRequest()
        'COMObj_Received()
        SafeLeaveTrace(RequestKey, 112)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Function GetDataValue(get_data_value_param1 As Integer, ByVal get_data_value_param2 As Integer) As String
        Dim return_string As String

        'SafeEnterTrace(RequestKey, 7)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        return_string = COMObj.GetDataValue(get_data_value_param1, get_data_value_param2)
        'SafeLeaveTrace(RequestKey, 107)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return return_string
    End Function

    Public Function GetDibStatus() As Short
        Dim dib_status As Boolean
        SafeEnterTrace(RequestKey, 13)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        dib_status = COMObj.GetDibStatus
        SafeLeaveTrace(RequestKey, 113)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return dib_status
    End Function

    Private Sub COMObj_Received() Handles COMObj.Received
        'SafeEnterTrace(RequestKey, 5)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        ' NumberOfRequest = NumberOfRequest - 1
        'SafeLeaveTrace(RequestKey, 105)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        RaiseEvent Received()
    End Sub
End Class
