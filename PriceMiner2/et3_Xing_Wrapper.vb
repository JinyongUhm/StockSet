Imports System.Threading
Imports XA_DATASETLib

Public Class et30_XingBase
    Friend Shared XingKey As TracingKey
    Friend Shared ThreadList As New List(Of Threading.Thread)
    Friend AllowGetAPI As Boolean = 0
    Friend Shared COMServerThread As Threading.Thread
    Friend Shared OrderDelayCount As Long = 0
End Class

Public Class et32_XAReal_Wrapper
    Inherits et30_XingBase

    Private WithEvents RealObj As XAReal
    Public Event ReceiveRealData(ByVal szTrCode As String)

    Public Sub New()
        SafeEnterTrace(XingKey, 10010)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        RealObj = New XAReal
        SafeLeaveTrace(XingKey, 100110)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub ReleaseCOM()
#If 0 Then
        Try
            If RealObj IsNot Nothing Then
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(RealObj)
            End If
        Finally
            RealObj = Nothing
            GC.Collect()
            GC.WaitForPendingFinalizers()
        End Try

#End If
    End Sub

    Public WriteOnly Property ResFileName As String
        Set(value As String)
            SafeEnterTrace(XingKey, 10011)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            RealObj.ResFileName = value
            SafeLeaveTrace(XingKey, 100111)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        End Set
    End Property

    Public Sub AdviseRealData()
        SafeEnterTrace(XingKey, 10012)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        AddHandler RealObj.ReceiveRealData, AddressOf RealObjReceiveData
        RealObj.AdviseRealData()
        SafeLeaveTrace(XingKey, 100112)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub UnAdviseRealData()
        SafeEnterTrace(XingKey, 10014)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        RealObj.UnadviseRealData()
        RemoveHandler RealObj.ReceiveRealData, AddressOf RealObjReceiveData
        SafeLeaveTrace(XingKey, 100114)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Function GetFieldData(ByVal a As String, ByVal b As String) As String
        If AllowGetAPI Then
            Dim result As String = RealObj.GetFieldData(a, b)
            Return result
        Else
            SafeEnterTrace(XingKey, 10013)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            Dim result As String = RealObj.GetFieldData(a, b)
            SafeLeaveTrace(XingKey, 100113)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return result
        End If
    End Function

    Public Sub SetFieldData(ByVal a As String, ByVal b As String, ByVal c As String)
        SafeEnterTrace(XingKey, 10016)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        RealObj.SetFieldData(a, b, c)
        SafeLeaveTrace(XingKey, 100116)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub RealObjReceiveData(ByVal szTrCode As String) 'Handles RealObj.ReceiveRealData
        SafeSetTrace(XingKey, 10017)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        AllowGetAPI = True
        RaiseEvent ReceiveRealData(szTrCode)
        AllowGetAPI = False
        SafeLeaveTrace(XingKey, 100117)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
End Class

Public Class et31_XAQuery_Wrapper
    Inherits et30_XingBase

    Private WithEvents QueryObj As XAQuery
    Private _T8412RequestControl As Boolean     '개별 query 개체마다 달라야 하므로 Shared 하면 안 된다.
    '2021.01.10: 취소주문 1초 3회 제한 control
    '2024.06.30 : 매매, 취소 합산 1초 50회 제한 control
    Private _GeneralOrderRequestControl As Boolean
    Private _T1101RequestControl As Boolean         '2021.07.07: 호가요청 1초 10회 제한 control
    Private _T1102RequestControl As Boolean         '2021.07.07: 증거금률조회 1초 10회 제한 control
    Private Shared _T8412RequestedTimeQueue As New List(Of DateTime)
    Private Shared _T1101RequestedTimeQueue As New List(Of DateTime)
    Private Shared _T1102RequestedTimeQueue As New List(Of DateTime)
    Private Shared _GeneralOrderRequestedTimeQueue As New List(Of DateTime)
    Private Shared _RecycleList As New List(Of et31_XAQuery_Wrapper)
    Private Shared _RecycleTimeUsed As New List(Of DateTime)
    Private Shared _RecycleKey As TracingKey
    Private Shared _RecycleCount As Integer
    Public RequestCount As Integer = 0
    Public ResponseCount As Integer = 0
    Public Event ReceiveData(ByVal szTrCode As String)
    Public Event ReceiveMessage(ByVal bIsSystemError As Boolean, ByVal nMessageCode As String, ByVal szMessage As String)

    'Called in protected section [XingKey]   ------------------------------------┐
    Public Sub New()
        '190712: 여기에다 protection 걸면 stuck된다. 아마도 New 함수 안에서 ReceiveData나 아니면 GetFieldData 끝날 때까지 자체적으로 기다리는 로직이 있는 것 아닐까

#If 0 Then
        SafeEnterTrace(_Recycle00600Key, 1)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If going_to_create_00600 AndAlso _Recycle00600List.Count > 0 Then
            QueryObj = _Recycle00600List(0)
            _Recycle00600List.RemoveAt(0)
            _RecycleCount += 1
            SafeLeaveTrace(_Recycle00600Key, 101)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Else
            SafeLeaveTrace(_Recycle00600Key, 201)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            QueryObj = New XAQuery
        End If
#End If
        SafeEnterTrace(XingKey, 10018)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        QueryObj = New XAQuery
        SafeLeaveTrace(XingKey, 100118)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
    'Called in protected section [XingKey]   ------------------------------------┘

    Public Shared Function NewOrUsed() As et31_XAQuery_Wrapper
        SafeEnterTrace(_RecycleKey, 1)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If _RecycleList.Count > 0 AndAlso Now - _RecycleTimeUsed(0) > TimeSpan.FromMinutes(10) Then
            '폐기한지 10 분 이상 지난 재활용 객체가 있다면 재활용한다.
            Dim used_obj As et31_XAQuery_Wrapper = _RecycleList(0)
            _RecycleList.RemoveAt(0)
            _RecycleTimeUsed.RemoveAt(0)
            _RecycleCount += 1
            SafeLeaveTrace(_RecycleKey, 101)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return used_obj
        Else
            Dim new_obj As et31_XAQuery_Wrapper = New et31_XAQuery_Wrapper()
            SafeLeaveTrace(_RecycleKey, 201)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return new_obj
        End If
    End Function

    Public Sub ReleaseCOM()
#If 0 Then
        '20200412: 아직도 만연하는 돌발버그를 잡기 위해 연구중에, COM object가 제대로 해제되지 않으면 문제가 access violation 등
        ' 의 문제가 발생할 수 있다고 해서, 그거 잡으려고 아래 코드를 구현했는데, 해제를 해도 GDI object가 해제되지 않는 현상을
        ' 발견해서 Xing 게시판에 가보니, 각 객체마다 window 핸들을 사용해서 매 생성마다 GDI 리소스가 하나씩 증가한다고 한다.
        ' 그리고 생성하면 TR 코드가 같으면 웬만하면 재사용하라고 한다. 그래서 Marshal release 어쩌구 하는 코드는 사실 필요없고
        ' 00600 주문객체에 대해서만은 재활용 객체 리스트를 만들어 관리하기로 해본다.
#If 0 Then
        Try
            If QueryObj IsNot Nothing Then
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(QueryObj)
            End If
        Finally
            QueryObj = Nothing
            GC.Collect()
            GC.WaitForPendingFinalizers()
        End Try
#End If
        '20200421: handler는 없애줘야 한다.
        SafeEnterTrace(XingKey, 10018)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '20200422: handler 없애주되 handler 실행중에는 하지 말자. 그래서 protection 건다. 취소주문할 때 deadlock 문제가 이거 때문인거 같다.
        RemoveHandler QueryObj.ReceiveData, AddressOf QueryObjReceiveData
        RemoveHandler QueryObj.ReceiveMessage, AddressOf QueryObjReceiveMessage
        SafeLeaveTrace(XingKey, 100118)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        SafeEnterTrace(_Recycle00600Key, 2)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If QueryObj.ResFileName.Contains("00600") Then
            _Recycle00600List.Add(QueryObj)
        End If
        SafeLeaveTrace(_Recycle00600Key, 102)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
#End If
        'Remove handlers
        'Wrapper 사용하는 모듈들에 ReceiveData, ReceiveMessage 이벤트 핸들러 remove 하고, Xing Query 객체에 대해서는 없애지 않고 그대로 둔다.
        SafeEnterTrace(XingKey, 10019)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If ReceiveDataEvent IsNot Nothing Then
            For Each d In ReceiveDataEvent.GetInvocationList()
                RemoveHandler ReceiveData, d
            Next
        End If
        If ReceiveMessageEvent IsNot Nothing Then
            For Each d In ReceiveMessageEvent.GetInvocationList()
                RemoveHandler ReceiveMessage, d
            Next
        End If
        SafeLeaveTrace(XingKey, 100119)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘

        SafeEnterTrace(_RecycleKey, 2)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        '재활용 센터로 보내기
        _RecycleList.Add(Me)
        _RecycleTimeUsed.Add(Now)

        '그 이외 초기화
        _T8412RequestControl = False
        _T1101RequestControl = False
        _T1102RequestControl = False
        _GeneralOrderRequestControl = False
        RequestCount = 0
        ResponseCount = 0
        SafeLeaveTrace(_RecycleKey, 102)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public WriteOnly Property ResFileName As String
        Set(value As String)
            SafeEnterTrace(XingKey, 1003)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            QueryObj.ResFileName = value
            SafeLeaveTrace(XingKey, 100103)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            If value.Contains("t8412") Then
                _T8412RequestControl = True
            ElseIf value.Contains("CSPAT00800") Or value.Contains("CSPAT00600") Then
                _GeneralOrderRequestControl = True
            ElseIf value.Contains("t1101") Then
                _T1101RequestControl = True
            ElseIf value.Contains("t1102") Then
                _T1102RequestControl = True
            End If
        End Set
    End Property

    Public Sub SetFieldData(ByVal a As String, ByVal b As String, ByVal c As Integer, ByVal d As String)
        SafeEnterTrace(XingKey, 1004)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        QueryObj.SetFieldData(a, b, c, d)
        SafeLeaveTrace(XingKey, 100104)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public XingDebugVar_a As String
    Public XingDebugVar_b As String
    Public XingDebugVar_c As String

    Public Function GetFieldData(ByVal a As String, ByVal b As String, ByVal c As Integer) As String
        If AllowGetAPI Then
            XingDebugVar_a = a
            XingDebugVar_b = b
            XingDebugVar_c = c
            Dim result As String = QueryObj.GetFieldData(a, b, c)
            Return result
        Else
            SafeEnterTrace(XingKey, 1005)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            Dim result As String = QueryObj.GetFieldData(a, b, c)
            SafeLeaveTrace(XingKey, 100105)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return result
        End If
    End Function

    Public Function GetBlockCount(ByVal szBlockName As String) As Integer
        If AllowGetAPI Then
            Dim result As Integer = QueryObj.GetBlockCount(szBlockName)
            Return result
        Else
            SafeEnterTrace(XingKey, 1006)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            Dim result As Integer = QueryObj.GetBlockCount(szBlockName)
            SafeLeaveTrace(XingKey, 100106)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return result
        End If
    End Function

    Public Function Decompress(ByVal szBlockName As String) As Integer
        SafeEnterTrace(XingKey, 1007)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        Dim result As Integer = QueryObj.Decompress(szBlockName)
        SafeLeaveTrace(XingKey, 100107)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return result
    End Function

    Public Function Request(ByVal a As Boolean) As Integer
        If _T8412RequestControl Then
            Dim current_time As DateTime = Now
            While _T8412RequestedTimeQueue.Count > 0 AndAlso current_time - _T8412RequestedTimeQueue.Last < TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(100)
                '바로 이전 request 이후 1초가 지날 때까지 기다린다.100ms는 마진임
                Threading.Thread.Sleep(10)
                current_time = Now
            End While
            While _T8412RequestedTimeQueue.Count >= 199 AndAlso current_time - _T8412RequestedTimeQueue(0) < TimeSpan.FromMinutes(10) + TimeSpan.FromMilliseconds(500)
                '10분당 200건. 500ms는 마진임
                Threading.Thread.Sleep(10)
                current_time = Now
            End While
        End If
        If _T1101RequestControl Then
            While T1101TimingCheck() = False
                Threading.Thread.Sleep(10)
            End While
        End If
        If _T1102RequestControl Then
            While T1102TimingCheck() = False
                Threading.Thread.Sleep(10)
            End While
        End If
        If _GeneralOrderRequestControl Then
            Dim enter_time = Now
            While GeneralOrderTimingCheck() = False
                Threading.Thread.Sleep(10)
            End While
            If Now - enter_time > TimeSpan.FromSeconds(1) Then
                OrderDelayCount += 1
                MessageLogging("XingWrapper 주문지연: " & OrderDelayCount)
            End If
        End If

        SafeEnterTrace(XingKey, 1002)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If _T8412RequestControl Then
            _T8412RequestedTimeQueue.Add(Now)
            If _T8412RequestedTimeQueue.Count = 200 Then
                _T8412RequestedTimeQueue.RemoveAt(0)
            End If
        End If
        If _T1101RequestControl Then
            _T1101RequestedTimeQueue.Add(Now)
            If _T1101RequestedTimeQueue.Count = 5 Then
                _T1101RequestedTimeQueue.RemoveAt(0)
            End If
        End If
        If _T1102RequestControl Then
            _T1102RequestedTimeQueue.Add(Now)
            If _T1102RequestedTimeQueue.Count = 5 Then
                _T1102RequestedTimeQueue.RemoveAt(0)
            End If
        End If
        If _GeneralOrderRequestControl Then
            _GeneralOrderRequestedTimeQueue.Add(Now)
            If _GeneralOrderRequestedTimeQueue.Count = 50 Then
                _GeneralOrderRequestedTimeQueue.RemoveAt(0)
            End If
        End If
        Dim result As Integer = QueryObj.Request(a)
        SafeLeaveTrace(XingKey, 100102)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
        Return result
    End Function

    Public Shared Function GeneralOrderTimingCheck() As Boolean
        Dim current_time As DateTime = Now

        SafeEnterTrace(XingKey, 10021)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If _GeneralOrderRequestedTimeQueue.Count >= 49 AndAlso current_time - _GeneralOrderRequestedTimeQueue(0) < TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(100) Then
            '1초당 50건. 100ms는 마진임
            SafeLeaveTrace(XingKey, 100121)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return False    '지금 취소 요청 안 된다고 알려줌
        Else
            SafeLeaveTrace(XingKey, 100221)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return True     '지금 취소 요청 가능하다고 알려줌
        End If
    End Function

    Public Shared Function T1101TimingCheck() As Boolean
        Dim current_time As DateTime = Now

        SafeEnterTrace(XingKey, 10022)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If _T1101RequestedTimeQueue.Count >= 4 AndAlso current_time - _T1101RequestedTimeQueue(0) < TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(100) Then
            '1초당 5건. 100ms는 마진임
            SafeLeaveTrace(XingKey, 100122)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return False    '지금 취소 요청 안 된다고 알려줌
        Else
            SafeLeaveTrace(XingKey, 100222)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return True     '지금 취소 요청 가능하다고 알려줌
        End If
    End Function

    Public Shared Function T1102TimingCheck() As Boolean
        Dim current_time As DateTime = Now

        SafeEnterTrace(XingKey, 10023)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If _T1102RequestedTimeQueue.Count >= 4 AndAlso current_time - _T1102RequestedTimeQueue(0) < TimeSpan.FromSeconds(1) + TimeSpan.FromMilliseconds(100) Then
            '1초당 10건. 100ms는 마진임
            SafeLeaveTrace(XingKey, 100123)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return False    '지금 취소 요청 안 된다고 알려줌
        Else
            SafeLeaveTrace(XingKey, 100223)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Return True     '지금 취소 요청 가능하다고 알려줌
        End If
    End Function

    Public Sub QueryObjReceiveData(ByVal szTrCode As String) Handles QueryObj.ReceiveData
#If 0 Then
        Dim thread_index As Integer = 0
        While thread_index < ThreadList.Count
            If ThreadList(thread_index) Is Threading.Thread.CurrentThread Then
                '현재 pending된 쓰레드들 중에 하나다. 추가할 필요 없다.
                Exit While
            End If
            thread_index += 1
        End While
        If thread_index = ThreadList.Count Then
            'pending된 쓰레드 리스트에 지금 쓰레드를 추가한다.
            ThreadList.Add(Threading.Thread.CurrentThread)
        End If
#End If

        SafeSetTrace(XingKey, 10020)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        AllowGetAPI = True
        RaiseEvent ReceiveData(szTrCode)
        AllowGetAPI = False
        SafeLeaveTrace(XingKey, 100120)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub

    Public Sub QueryObjReceiveMessage(ByVal bIsSystemError As Boolean, ByVal nMessageCode As String, ByVal szMessage As String) Handles QueryObj.ReceiveMessage
#If 0 Then
        Dim thread_index As Integer = 0
        While thread_index < ThreadList.Count
            If ThreadList(thread_index) Is Threading.Thread.CurrentThread Then
                '현재 pending된 쓰레드들 중에 하나다. 추가할 필요 없다.
                Exit While
            End If
            thread_index += 1
        End While
        If thread_index = ThreadList.Count Then
            'pending된 쓰레드 리스트에 지금 쓰레드를 추가한다.
            ThreadList.Add(Threading.Thread.CurrentThread)
        End If
#End If
        SafeSetTrace(XingKey, 10015)     'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        AllowGetAPI = True
        RaiseEvent ReceiveMessage(bIsSystemError, nMessageCode, szMessage)
        AllowGetAPI = False
        SafeLeaveTrace(XingKey, 100115)   'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
    End Sub
End Class
