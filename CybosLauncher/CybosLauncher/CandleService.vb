Imports CandleServiceInterfacePrj

' 참고: 상황에 맞는 메뉴에서 "이름 바꾸기" 명령을 사용하여 코드 및 config 파일에서 클래스 이름 "CandleService"을 변경할 수 있습니다.
Public Class CandleService
    Implements ICandleService

    Public Shared CandleListSet As New List(Of List(Of CandleStructure))

    Public Sub Initialize(ByVal number_of_symbols As Integer) Implements ICandleService.Initialize
        For index As Integer = 0 To number_of_symbols - 1
            CandleListSet.Add(New List(Of CandleStructure))
        Next
    End Sub

    Public Sub ClearCandle(ByVal symbol_index As Integer) Implements ICandleService.ClearCandle
        CandleListSet(symbol_index).Clear()
    End Sub

    Public Sub AddCandle(ByVal symbol_index As Integer, ByVal candle_to_add As CandleStructure) Implements ICandleService.AddCandle
        CandleListSet(symbol_index).Add(candle_to_add)
    End Sub

    Public Sub RemoveCandle(ByVal symbol_index As Integer) Implements ICandleService.RemoveCandle
        CandleListSet(symbol_index).RemoveAt(0)
    End Sub

    Public Function Candle(ByVal symbol_index As Integer, ByVal index As Integer) As CandleStructure Implements ICandleService.Candle
        Return CandleListSet(symbol_index).Item(index)
    End Function

    Public Function CandleCount(ByVal symbol_index As Integer) Implements ICandleService.CandleCount
        Return CandleListSet(symbol_index).Count
    End Function

    Public Function LastCandle(ByVal symbol_index As Integer) As CandleStructure Implements ICandleService.LastCandle
        Return CandleListSet(symbol_index).Last
    End Function
End Class
