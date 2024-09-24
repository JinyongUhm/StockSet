Public Enum ListViewSortType
    LV_SORT_STRING
    LV_SORT_INT
    LV_SORT_FLOAT
End Enum

Public Class uc0_ListViewSortable
    Private _ColumnSorted As Integer = -1

    Public Sub ColumnSort(ByVal column_index As Integer)
        If column_index >= 0 AndAlso column_index < Me.Columns.Count Then
            If column_index <> _ColumnSorted Then
                'sort a column different than the previous one => sort ascending
                _ColumnSorted = column_index
                Sorting = SortOrder.Ascending
            Else
                'sort the column which has been sorted previously => change sort order
                If Sorting = SortOrder.Ascending Then
                    Sorting = SortOrder.Descending
                Else
                    Sorting = SortOrder.Ascending
                End If
            End If

            'check whether the clicked column is integer type
            If Me.Columns(column_index).Tag IsNot Nothing Then
                Dim sort_type As String = Me.Columns(column_index).Tag.ToString
                Select Case sort_type
                    Case "i"
                        ListViewItemSorter = New ListViewItemComparer(column_index, Me.Sorting, ListViewSortType.LV_SORT_INT)
                    Case "f"
                        ListViewItemSorter = New ListViewItemComparer(column_index, Me.Sorting, ListViewSortType.LV_SORT_FLOAT)
                    Case Else
                        ListViewItemSorter = New ListViewItemComparer(column_index, Me.Sorting, ListViewSortType.LV_SORT_STRING)
                End Select
            Else
                ListViewItemSorter = New ListViewItemComparer(column_index, Me.Sorting, ListViewSortType.LV_SORT_STRING)
            End If
            Sort()                  'execute sorting
        Else
            MsgBox("Specifed column index is not found.", MsgBoxStyle.Critical)
        End If
    End Sub


    Private Sub uc0_ListViewSortable_ColumnClick(ByVal sender As Object, ByVal e As System.Windows.Forms.ColumnClickEventArgs) Handles Me.ColumnClick
        ColumnSort(e.Column)
    End Sub


    Private Class ListViewItemComparer
        Implements IComparer

        Private _ColumnNumber As Integer
        Private _SortOrder As SortOrder
        Private _SortType As ListViewSortType
        'Private _IsNumberSorting As Boolean

        Public Sub New(ByVal column_number As Integer, ByVal sort_order As SortOrder, ByVal lv_sort_type As ListViewSortType)
            _ColumnNumber = column_number
            _SortOrder = sort_order
            _SortType = lv_sort_type
        End Sub

        Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Dim x_item As ListViewItem = x
            Dim y_item As ListViewItem = y
            Dim return_val As Integer = -1

            Select Case _SortType
                Case ListViewSortType.LV_SORT_STRING
                    'string sorting
                    return_val = [String].Compare(x_item.SubItems(_ColumnNumber).Text, y_item.SubItems(_ColumnNumber).Text)
                Case ListViewSortType.LV_SORT_INT
                    'integer sorting
                    Dim x_int As Long = Convert.ToInt64(x_item.SubItems(_ColumnNumber).Text.Replace(",", ""))
                    Dim y_int As Long = Convert.ToInt64(y_item.SubItems(_ColumnNumber).Text.Replace(",", ""))
                    Dim diff As Long = x_int - y_int
                    If diff > 0 Then
                        return_val = 1
                    ElseIf diff = 0 Then
                        return_val = 0
                    Else
                        return_val = -1
                    End If
                Case ListViewSortType.LV_SORT_FLOAT
                    'float sort
                    Dim x_float As Double = Convert.ToDouble(x_item.SubItems(_ColumnNumber).Text.Replace("%", ""))
                    Dim y_float As Double = Convert.ToDouble(y_item.SubItems(_ColumnNumber).Text.Replace("%", ""))
                    Dim diff As Double = x_float - y_float
                    If diff > 0 Then
                        return_val = 1
                    ElseIf diff = 0 Then
                        return_val = 0
                    Else
                        return_val = -1
                    End If
            End Select

            If _SortOrder = SortOrder.Descending Then
                return_val *= -1
            End If

            Return return_val
        End Function
    End Class
End Class
