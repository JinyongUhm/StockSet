Imports System.Reflection
Imports System.Runtime.InteropServices.ComTypes

Public Class c07_SharpLines
    Public VERTICAL_SCALE As Double = 80    '100이면 1%가 길이 1, 50이면 2%가 길이 1
    Public HOT_HEIGHT As Double = 3.5        '걸린애를 만들기 위한 최소 HEIGHT
    Public HOT_WIDTH As Double = 12           '걸린애를 만들기 위한 최대 WIDTH
    'Public Shared DULLNESS As Double = 0.75          'SharpLines 를 유지하기 위한 앞선분길이 대비 뒷선분길이의 최소 비율
    Public STANDARD_LENGTH As Double = 5
    Public X_LIMIT As Double = 30            '선분의 길이가 이거를 넘어가면 SharpLines 검출은 종료된다.

    Enum EnumLineStatus
        SEARCH_FOR_FIRST_POINT
        UPDATE_DETECTED_LINE
    End Enum
    Public linked_symbol_debug As c03_Symbol
    Public LineDetectStatus As EnumLineStatus
    Private _HistoryY As New List(Of Integer)   '흘러간 y좌표를 당분간 보관할 목적
    Public DetectedLine As c07a_LineDetected
    Public V As Boolean '현재 선분들의 방향이 아래 찍고 위로 가는 방향으로 가고 있으면 TRUE, 반대로 위를 찍고 아래로 가는 방향으로 있으면 FALSE
    Public X As Integer = -1 'SEARCH_FOR_FIRST_POINT 상태에서는 -1 이고, UPDATE_DETECTED_LINE 상태에서는 현재까지 입력된 점들 중 최신 점의 X 좌표를 나타낸다.

    Public Sub New()
        LineDetectStatus = EnumLineStatus.SEARCH_FOR_FIRST_POINT
    End Sub

    Function InputNewPoint(y As Integer, now_time As DateTime) As c07a_LineDetected
        '진입에러 체크 or 처리
        If y = 0 Then
            '가격이 0 이 들어왔다. 0 은 들어오면 안 되는 값이다.
            If _HistoryY.Count = 0 Then
                '처음부터 0 이 들어왔으면 아무것도 안 하고 되돌려보낸다.
                Return Nothing
            Else
                '중간에 0 이 들어왔으면 이전값을 유지하게 한다.
                y = _HistoryY.Last
            End If
        End If

        _HistoryY.Add(y)
        If _HistoryY.Count > HOT_WIDTH Then
            '너무 길면 앞에서부터 자른다.
            _HistoryY.RemoveAt(0)
        End If

        '이 함수에서 반드시 0이상의 값으로 업데이트되어야 함. 만약 끝에서도 -1이라면 뭔가 빠뜨렸다는 얘기. 0이면 돌려줄 값은 없지만 정상처리했다는 것. 1이면 돌려줄 것이 있다는 것.
        '돌려줄 객체가 있는 경우는 세 가지임. 첫째, 첫선분이 검출되었을 때. 둘째, 새로운 역전점이 추가되었을 때, 셋째, 선분검출이 complete되었을 때.
        Dim result As Integer = -1
        Dim height As Integer
        Dim point1, point2 As Point
        Select Case LineDetectStatus
            Case EnumLineStatus.SEARCH_FOR_FIRST_POINT
                For index As Integer = 0 To _HistoryY.Count - 2
                    '저장된 각점들과 최근점 사이의 높이차이를 계산하여 특정값보다 크면 검출된 것이다. (마지막 점은 point2 가 되므로 뺀다)
                    height = CalcHeight(_HistoryY(index), _HistoryY.Last)
                    If height > HOT_HEIGHT Then
                        '상승선분 검출됨
                        point1.X = 0
                        point1.Y = _HistoryY(index)
                        point2.X = _HistoryY.Count - 1 - index
                        point2.Y = _HistoryY.Last
                        '검출라인 객체를 생성하고 해당 점 2개를 같다 붙임
                        DetectedLine = New c07a_LineDetected(Me)
                        DetectedLine.Points.Add(point1)
                        DetectedLine.Points.Add(point2)
                        '상태를 업데이트함
                        LineDetectStatus = EnumLineStatus.UPDATE_DETECTED_LINE
                        V = False   '위를 찍고 아래로 가는 방향
                        X = point2.X
                        DetectedLine.StartTime = now_time - TimeSpan.FromSeconds(5 * (_HistoryY.Count - 1 - index)) '시작시간은 첫번째 점의 시간이다.
                        result = 1      '검출된 sharp lines 를 돌려준다.
                        Exit For '뒤로는 더 이상 안 보고 나간다.
                    ElseIf height < -HOT_HEIGHT Then
                        '하락선분 검출됨
                        point1.X = 0
                        point1.Y = _HistoryY(index)
                        point2.X = _HistoryY.Count - 1 - index
                        point2.Y = _HistoryY.Last
                        '검출라인 객체를 생성하고 해당 점 2개를 같다 붙임
                        DetectedLine = New c07a_LineDetected(Me)
                        DetectedLine.Points.Add(point1)
                        DetectedLine.Points.Add(point2)
                        '상태를 업데이트함
                        LineDetectStatus = EnumLineStatus.UPDATE_DETECTED_LINE
                        V = True    '아래 찍고 위로 가는 방향
                        X = point2.X
                        DetectedLine.StartTime = now_time - TimeSpan.FromSeconds(5 * (_HistoryY.Count - 1 - index)) '시작시간은 첫번째 점의 시간이다.
                        result = 1      '검출된 sharp lines 를 돌려준다.
                        Exit For '뒤로는 더 이상 안 보고 나간다.
                    Else
                        result = 0      '검출은 안 되었지만 처리는 잘 되었음을 알린다.
                    End If
                Next
            Case EnumLineStatus.UPDATE_DETECTED_LINE
                'X좌표 1증가
                X += 1
                '일단 새로 들어온 점이 업데이트점인지 역전후보점인지를 알아본다
                Dim update_or_reversion As Boolean
                If V Then
                    '아래 찍고 위로 가는 방향
                    If DetectedLine.GetUpdatePoint.Y >= _HistoryY.Last Then
                        '업데이트점이 업데이트되었다.
                        update_or_reversion = True
                    Else
                        '역전 후보점이 업데이트되었다.
                        update_or_reversion = False
                    End If
                Else
                    '위를 찍고 아래로 가는 방향
                    If DetectedLine.GetUpdatePoint.Y <= _HistoryY.Last Then
                        '업데이트점이 업데이트되었다.
                        update_or_reversion = True
                    Else
                        '역전 후보점이 업데이트되었다.
                        update_or_reversion = False
                    End If
                End If
                If update_or_reversion Then
                    '업데이트점이 업데이트되었다.
                    Dim updated_point As Point
                    updated_point.X = X
                    updated_point.Y = _HistoryY.Last
                    DetectedLine.Points(DetectedLine.Points.Count - 1) = updated_point
                    '새로 업데이트된 점과 그 이전점 사이의 x difference 를 구해서 X_LIMIT을 넘는지 조사하자.

                    Dim x_diff = X - DetectedLine.Points(DetectedLine.Points.Count - 2).X
                    If x_diff > X_LIMIT Then
                        'SharpLines 의 끝에 도달했다.
                        LineDetectStatus = EnumLineStatus.SEARCH_FOR_FIRST_POINT
                        X = -1
                        DetectedLine.Complete = True
                        result = 1
                    Else
                        '업데이트점이 업데이트되었고 앞으로 더 업데이트 될 수 있다.
                        result = 1
                    End If
                Else
                    '역전 후보점이 업데이트되었다.
                    '역전후보점의 실체는 사실 가장 최근점이다.
                    '역전후보점까지의 선분 길이 계산
                    Dim reversion_point As Point
                    reversion_point.X = X
                    reversion_point.Y = _HistoryY.Last
                    Dim new_length As Double = Length(DetectedLine.Points.Last, reversion_point)
                    '이전 선분들의 (평균)길이 계산
                    Dim old_length As Double = DetectedLine.RecentAveLength
                    'If new_length > old_length * DULLNESS Then
                    If new_length > STANDARD_LENGTH Then
                        '새로운 점 탄생
                        DetectedLine.Points.Add(reversion_point)
                        '꺾임 방향 역전
                        V = Not V
                        'SharpLines 는 방향을 바꿔 계속된다. 역전점이 추가되었음을 알린다.
                        result = 1
                    Else
                        '역전 후보점은 계속 자란다.
                        '역전 후보점과 업데이트점 사이의 x difference 를 구해서 X_LIMIT을 넘는지 조사하자. 할 필요 없을 것 같긴 하다.
                        result = 0
                    End If

                End If
        End Select
        If result = 1 Then
            Return DetectedLine
        ElseIf result = 0 Then
            Return Nothing
        Else
            '이런 경우는 없어야 한다.
        End If
    End Function

    Public Function CalcHeight(y1 As Double, y2 As Double) As Double
        Dim height As Double = (y2 - y1) / y1 * VERTICAL_SCALE
        Return height
    End Function

#If 0 Then
    Function CalcSlope(point1 As Point, point2 As Point) As Double
        Dim height As Double = CalcHeight(point1.Y, point2.Y)
        Dim width As Double = point2.X - point1.X

        Return height / width
    End Function
#End If
    Public Function Length(point1 As Point, point2 As Point) As Double
        Return Math.Sqrt((point2.X - point1.X) ^ 2 + CalcHeight(point1.Y, point2.Y) ^ 2)
    End Function

    Public Function Slope(point1 As Point, point2 As Point) As Double
        Return CalcHeight(point1.Y, point2.Y) / (point2.X - point1.X)
    End Function
End Class

Public Class c07a_LineDetected
    Public StartTime As DateTime    '검출된 첫번째 점의 시간
    Public Points As New List(Of Point) '검출된 점들의 좌표를 이어붙인 것
    Public Complete As Boolean = False         '선분들이 모두 검출이 되었음을 알림
    Public Parent As c07_SharpLines

    Public Sub New(ByVal the_parent As c07_SharpLines)
        Parent = the_parent
    End Sub

    Public Function RecentAveLength() As Double
        If Points.Count < 2 Then
            '비정상적인 경우임
            Return -1
        ElseIf Points.Count = 2 Then
            '선분 한 개의 길이를 반환함
            Dim length = Parent.Length(Points(0), Points(1))
            Return length
        Else 'Points.Count > 2 Then
            '마지막 선분 두 개의 길이를 평균하여 반환함
            Dim length1 = Parent.Length(Points(Points.Count - 3), Points(Points.Count - 2))
            Dim length2 = Parent.Length(Points(Points.Count - 2), Points(Points.Count - 1))
            Return (length1 + length2) / 2
        End If
    End Function

    '마지막에서 첫선분
    Public Function Length1() As Double
        If Points.Count < 2 Then
            '비정상적인 경우임
            Return -1
        Else
            '마지막 선분의 길이를 반환함
            Dim length = Parent.Length(Points(Points.Count - 2), Points(Points.Count - 1))
            Return length
        End If
    End Function

    '마지막에서 두번째 선분
    Public Function Length2() As Double
        If Points.Count < 3 Then
            '비정상적인 경우임
            Return -1
        Else
            '마지막에서 두번째 선분의 길이를 반환함
            Dim length = Parent.Length(Points(Points.Count - 3), Points(Points.Count - 2))
            Return length
        End If
    End Function

    '마지막에서 세번째 선분
    Public Function Length3() As Double
        If Points.Count < 4 Then
            '비정상적인 경우임
            Return -1
        Else
            '마지막에서 세번째 선분의 길이를 반환함
            Dim length = Parent.Length(Points(Points.Count - 4), Points(Points.Count - 3))
            Return length
        End If
    End Function

    Public Function Slope1() As Double
        If Points.Count < 2 Then
            '비정상적인 경우임
            Return -1
        Else
            '마지막 선분의 기울기를 반환함
            Dim slope = Parent.Slope(Points(Points.Count - 2), Points(Points.Count - 1))
            Return slope
        End If
    End Function

    Public Function Slope2() As Double
        If Points.Count < 3 Then
            '비정상적인 경우임
            Return -1
        Else
            '마지막 선분의 기울기를 반환함
            Dim slope = Parent.Slope(Points(Points.Count - 3), Points(Points.Count - 2))
            Return slope
        End If
    End Function

    Public Function Slope3() As Double
        If Points.Count < 4 Then
            '비정상적인 경우임
            Return -1
        Else
            '마지막 선분의 기울기를 반환함
            Dim slope = Parent.Slope(Points(Points.Count - 4), Points(Points.Count - 3))
            Return slope
        End If
    End Function

    '마지막 선분의 방향
    Public Function V() As Boolean
        If Points.Count < 2 Then
            '비정상적인 경우임
            Return False
        Else
            Return (Points(Points.Count - 2).Y > Points(Points.Count - 1).Y)
        End If
    End Function

    Public Function GetUpdatePoint() As Point
        If Points.Count > 1 Then
            Return Points.Last
        Else
            Return Nothing
        End If
    End Function

    Public Function GetLastFixPoint() As Point
        If Points.Count > 1 Then
            Return Points(Points.Count - 2)
        Else
            Return Nothing
        End If
    End Function
End Class