Imports DSCBO1Lib
Imports CPUTILLib

Public Class c02_Bunch
    Inherits List(Of c03_Symbol)

    'Public WithEvents COMObj As StockMst2
    'Public WithEvents COMObj As et5_MarketEye_Wrapper
    Public COMObj As et5_MarketEye_Wrapper
    Public AllCode As String
    Public DataReceivedNothing As Boolean = False
    Public RequestCount As Integer = 0
    'Public BunchRequestKey As TracingKey
    Public MyIndex As Integer = 0

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
        COMObj = New et5_MarketEye_Wrapper
        'COMObj.SetInputValue(0, AllCode)    '코드명 세팅
    End Sub

    'StockMst2 의 request 부르기
#If 0 Then
    Public Function Mst2Request() As Boolean
        While True
            '160405: 현재 기다리고 있는 응답이 없을 때만 새로 요청한다. 기다리는 응답이 있다면 응답이 올 때까지 계속 기다리게 된다.
            SafeEnterTrace(BunchRequestKey, 0)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
            If RequestCount = 0 Then
                RequestCount = 1
                SafeLeaveTrace(BunchRequestKey, 1)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                Exit While
            Else
                SafeLeaveTrace(BunchRequestKey, 201)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
                System.Threading.Thread.Yield()
            End If
        End While

        DataReceivedNothing = False
        COMObj.SetCodeAndRequest(AllCode)       '코드세팅과 리퀘스트를 한번에
    End Function
#End If

    'StockMst2 의 block request 부르기
    Public Function Mst2BlockRequest(ByVal is_symbol_collection_mode As Boolean) As Boolean
        Dim debug_string As String = ""
        Try
            If RequestCount = 0 Then
                RequestCount = 1
            Else
                RequestCount = 1
                MessageLogging("림프홈")

                Return False        'NOK 의미한다.
            End If

            debug_string = debug_string & "0,"
            Dim com_obj_dib_status = COMObj.GetDibStatus()
            debug_string = debug_string & "1,"
            If com_obj_dib_status <= 0 Then
                If com_obj_dib_status = -1 Then
                    MessageLogging("에헷! 알고보니 요청가능한 상태였어!")
                    MessageLogging(COMObj.GetDibMsg1())
                End If
                DataReceivedNothing = False
                debug_string = debug_string & "2,"
                COMObj.SetCodeAndBlockRequest(AllCode, is_symbol_collection_mode)       '코드세팅과 리퀘스트를 한번에
                debug_string = debug_string & "3,"
                GetData()   '170213: 자꾸 에러나고 프로그램 종료되고 그래서 원인 한 번 찾아보려고 COMObj 읽어오는 것을 receive event 에서 안 하고 밖으로 빼놓고 한 번 해보려 한다.
                debug_string = debug_string & "4,"
            Else
                ErrorLogging(AllCode & " 요청 못한대!!!")

                '2021.06.11: 몇 달 간의 로그를 들여다봤는데, 연결 끊겨도 이쪽으로는 이제 안 들어오는 것 같다. 잘 모르겠지만 이쪽으로 다시 오더라도
                '2021.06.11: 컴객체 다시 만들고 이런 거는 이제 도움이 안 될 것 같다. 아래 exception case와 마찬가지로 끊고 재시작하는 걸로 하는 게 좋겠다.

                CpStuckAction()

                Return False 'NOK 의미한다.

            End If
        Catch ex As Exception
            '2021.06.11: 이쪽으로 들어오면 연결을 끊고, PriceMiner로 하여금 CybosLauncher를 재시작하게 만든다.
            ErrorLogging("닫히는익셉션1: " & ex.Message & ", debug_string:" & debug_string)
            CpStuckAction()

            Return False    'NOK
        End Try

        Return True 'OK
    End Function

    Public Sub SetNewDataProcess()
        '2021.06.08: CybosLauncher의 이곳에서 할 일은 real data 를 price miner에 전송하는 것이다.
        'mutex protection 처리된 후 들어오는 곳이다.
        Dim cybos_real_data_accesser = CybosRealDataMMF.CreateViewAccessor(SYMBOL_REAL_START_OFFSET + SYMBOL_REAL_DATA_SIZE * MyIndex * MaxNumberOfRequest, SYMBOL_REAL_DATA_SIZE * Count)
        For index As Integer = 0 To Count - 1
            cybos_real_data_accesser.Write(SYMBOL_REAL_DATA_SIZE * index, Item(index).StoredPrice)      'length:4
            cybos_real_data_accesser.Write(SYMBOL_REAL_DATA_SIZE * index + 4, Item(index).StoredAmount) 'length:8
            cybos_real_data_accesser.Write(SYMBOL_REAL_DATA_SIZE * index + 12, Item(index).StoredGangdo) 'length:8
        Next
        'For index As Integer = 0 To Count - 1
        'Item(index).SetNewData()
        'Next
    End Sub

    'Private Sub COMObj_Received() Handles COMObj.Received
    Private Sub GetData()
#If 0 Then
        SafeEnterTrace(SymTree.GettingPricesKey, 12)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If SymTree.WeAreStuckHere Then
            SymTree.WeAreStuckHere = False
            Exit Sub
        End If
        SafeLeaveTrace(SymTree.GettingPricesKey, 13)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
#End If

        '주가정보 가져왔으면 각 종목 객체에 통보한다.
        'SafeEnterTrace(BunchRequestKey, 6)      'Enter critical zone, Thread간 공유문제 발생예방------------------------------------┐
        If RequestCount > 0 Then
            RequestCount = RequestCount - 1
            'SafeLeaveTrace(BunchRequestKey, 7)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            Dim now_price As UInt32
            Dim amount As UInt64
            Dim gangdo As Double
            For index As Integer = 0 To Count - 1
                amount = GetAmount(index)
                If amount = Item(index).StoredAmount Then
                    '거래량이 지난 번과 같기 때문에 price와 gangdo 를 가져올 필요가 없다. => 이렇게 함으로써 시간을 절약해보자.
                    Continue For
                End If
                now_price = GetNowPrice(index)
                If now_price = 0 And amount = 0 Then
                    '160328: 15초 60회 요청제한에 걸렸을 때 15초 후 다 0으로 불리게 되어 있다. 이럴 땐 빠져나가서 재요청
                    '160630: 그런 줄 알았는데 아닌 거 같다. 재요청 해도 똑같이 둘 다 0으로 되는 거 보면 그냥 종목에 따라 둘 다 0으로 불리는 종목이 있는 것 같다.
                    '160630: 그래서 재요청 하지 않는 것으로 바꾼다.
                    'DataReceivedNothing = True      '160328: 빠져나가서 재요청하기 위한 flag
                    ErrorLogging("오 이런 가격과 거래량 다 0!")
                    'Exit Sub
                    '160707: 다 0으로 불릴 경우 바로 나가지 않고 뒤 이은 종목들을 읽는다.
                    Continue For
                End If
                gangdo = GetGangdo(index)
                'Item(index).SetNewData(now_price, amount, gangdo)
                'Item(index).SetNewData(now_price, amount)
                '160321: 여기서는 값을 저장만 하고 아래 별도 쓰레드에서 SetNewData 한다. 빨리 반환해서 밀려 실행됨을 막기 위함이다.
                Item(index).StoreNewData(now_price, amount, gangdo)
            Next
        Else
            'Request한 적이 없다면 아무것도 안 한다.
            'SafeLeaveTrace(BunchRequestKey, 8)      'Leave critical zone, Thread간 공유문제 발생예방------------------------------------┘
            ErrorLogging("요청한 적 없는데 data 받았다.")
        End If
    End Sub
#If 0 Then
    Public Sub COMObj_LimpHome()
        'COMObj가 무슨 이유에서인지 고장났을 때 바로 전 데이터를 그냥 카피해서 이번 데이터인 것처럼 입력되게 한다.
        SetNewDataProcess()
        'For index As Integer = 0 To Count - 1
        'Item(index).SetLimpHomeData()
        'Next
    End Sub
#End If
    'request한 결과에서 해당 인덱스의 종목코드 반환
    Public ReadOnly Property GetSymbolCode(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(COMObj.WrapperIndex_SymbolCode, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 현재가 반환
    Public ReadOnly Property GetNowPrice(ByVal index As Integer) As String
        Get
            Dim the_price As String
            the_price = COMObj.GetDataValue(COMObj.WrapperIndex_NowPrice, index)
            Return the_price
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 거래량 반환
    Public ReadOnly Property GetAmount(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(COMObj.WrapperIndex_Amount, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 종목명 반환
    Public ReadOnly Property GetSymbolName(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(COMObj.WrapperIndex_SymbolName, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 전일거래량 반환
    Public ReadOnly Property GetYesterdayAmount(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(COMObj.WrapperIndex_YesterdayAmount, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 전일종가 반환
    Public ReadOnly Property GetYesterdayPrice(ByVal index As Integer) As String
        Get
            Return COMObj.GetDataValue(COMObj.WrapperIndex_YesterdayPrice, index)
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 체결강도 반환
    Public ReadOnly Property GetGangdo(ByVal index As Integer) As String
        Get
            If COMObj.WrapperIndex_Gangdo = -1 Then
                '처음 시작할 때면 강도도 0일 수 있지
                Return 0
            Else
                Return COMObj.GetDataValue(COMObj.WrapperIndex_Gangdo, index)
            End If
        End Get
    End Property

    'request한 결과에서 해당 인덱스의 현재가 반환
    'Public ReadOnly Property GetSelPrice(ByVal index As Integer) As String
    'Get
    'Return COMObj.GetDataValue(9, index)
    'End Get
    'End Property

    'request한 결과에서 해당 인덱스의 현재가 반환
    'Public ReadOnly Property GetBuyPrice(ByVal index As Integer) As String
    'Get
    'Return COMObj.GetDataValue(10, index)
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
