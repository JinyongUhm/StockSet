Imports System.ServiceModel

Public Structure CandleStructure
    'Dim MinutesAfterStart As UInt32
    'Dim Variation5Minutes As Single
    'Dim Average5Minutes As Single
    'Dim Average30Minutes As Single
    'Dim Average35Minutes As Single
    'Dim Average70Minutes As Single
    'Dim Average140Minutes As Single
    'Dim Average280Minutes As Single
    'Dim Average560Minutes As Single
    'Dim Average1200Minutes As Single
    Dim Average2400Minutes As Single
    Dim Average4800Minutes As Single
    Dim Average9600Minutes As Single
    'Dim VariationRatio As Single
    Dim CandleTime As DateTime
    Dim Open As UInt32
    Dim Close As UInt32
    Dim High As UInt32
    Dim Low As UInt32
    Dim Amount As UInt32
    Dim AccumAmount As UInt64
    'Dim Test_MA_Var As Single
    'Dim Test_MA_Price As Single
End Structure

' 참고: 상황에 맞는 메뉴에서 "이름 바꾸기" 명령을 사용하여 코드 및 config 파일에서 인터페이스 이름 "ICandleService"을 변경할 수 있습니다.
<ServiceContract()>
Public Interface ICandleService

    <OperationContract()>
    Sub Initialize(ByVal number_of_symbols As Integer)
    <OperationContract()>
    Sub AddCandle(ByVal symbol_index As Integer, ByVal candle_to_add As CandleStructure)
    <OperationContract()>
    Sub ClearCandle(ByVal symbol_index As Integer)
    <OperationContract()>
    Sub RemoveCandle(ByVal symbol_index As Integer)
    <OperationContract()>
    Function Candle(ByVal symbol_index As Integer, ByVal index As Integer) As CandleStructure
    <OperationContract()>
    Function CandleCount(ByVal symbol_index As Integer)
    <OperationContract()>
    Function LastCandle(ByVal symbol_index As Integer) As CandleStructure

End Interface
