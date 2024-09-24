Public Class c01_Symtree
    Inherits List(Of c02_Bunch)

    Private _SymbolListing As Boolean

    '가장 마지막에 있는 번치
    Private ReadOnly Property LastBunch() As c02_Bunch
        Get
            If Count = 0 Then
                Return Nothing
            Else
                Return Item(Count - 1)
            End If
        End Get
    End Property

#If 0 Then
    Public Sub StartSymbolListing()
        _SymbolListing = True
    End Sub

    Public Sub AddSymbol(ByVal symbol As c03_Symbol)
        If _SymbolListing Then
            If LastBunch Is Nothing OrElse LastBunch.Count = _MAX_NUMBER_OF_REQUEST Then
                '번치 하나 더 만들자
                Dim a_new_bunch As c02_Bunch = New c02_Bunch(symbol)
                Add(a_new_bunch)                'SymTree에 새 번치 더한다
            Else
                '있던 번치에 더하자
                LastBunch.Add(symbol)
            End If

            If LastBunch.Count = _MAX_NUMBER_OF_REQUEST Then
                '110개 다 모았으니 하나의 bunch 생성 마무리하자.
                LastBunch.SymbolListFix()    '번치의 종목리스트 마무리 (여기서 COM 객체도 생성)
            End If

        End If
    End Sub

    Public Sub FinishSymbolListing()
        _SymbolListing = False
        LastBunch.SymbolListFix()       '마지막 미완 번치도 종목리스트 마무리
    End Sub

    '주가 업데이트 위한 클락 들어옴
    Public Sub ClockSupply()
        For index As Integer = 0 To Count - 1
            Item(index).Mst2BlockRequest()     '각 번치마다 update request
        Next
    End Sub
#End If

    '모은 모든 종목의 시세정보를 DB에 저장한다.
    'Public Sub SaveToDB()
    'End Sub
End Class
