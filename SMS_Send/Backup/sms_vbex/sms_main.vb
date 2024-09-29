Imports System.Security.Cryptography
Public Class sms_main

    Public smsID As String = "*****" '부여 받은 너나우리 SMS 아이디를 넣으십시요.
    Public smsPwd As String = "*****" '부여 받은 너나우리 SMS 패스워드를 넣으십시요.

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        Dim hashValue As String
        Dim smsContent As String
        Dim receivePhone As String
        Dim senderPhone As String
        Dim reservedate As String
        Dim reservetime As String
        Dim userdefine As String
        Dim canclemode As String

        userdefine = smsID
        canclemode = "1"


        senderPhone = TextBox1.Text    '보내는 분 핸드폰번호
        receivePhone = TextBox2.Text   '받는 분 핸드폰번호
        smsContent = sms_content.Text  '전문 내용
        reservedate = TextBox3.Text '예약일자
        reservetime = TextBox4.Text '예약일자

        Dim oSOAP As New youiwe.ServiceSMS()

        If sendkind.SelectedItem.ToString() = "즉시전송" Then         '즉시전송
            hashValue = MD5Encrypt(smsID + smsPwd + receivePhone)     '해쉬값을 받습니다.

            Dim result As String = oSOAP.SendSMS(smsID, hashValue, senderPhone, receivePhone, smsContent)

            MsgBox("결과코드:" + result)

        ElseIf sendkind.SelectedItem.ToString() = "예약전송" Then     '예약전송
            hashValue = MD5Encrypt(smsID + smsPwd + receivePhone)     '해쉬값을 받습니다.

            Dim result As String = oSOAP.SendSMSReserve(smsID, hashValue, senderPhone, receivePhone, smsContent, reservedate, reservetime, userdefine)

            MsgBox("결과코드:" + result)

        ElseIf sendkind.SelectedItem.ToString() = "예약취소" Then
            hashValue = MD5Encrypt(smsID + smsPwd + userdefine)   '해쉬값을 받습니다.

            Dim result As String = oSOAP.ReserveCancle(smsID, hashValue, userdefine, canclemode)

            MsgBox("결과코드:" + result)

        End If


        hashValue = MD5Encrypt(smsID + smsPwd)     '해쉬값을 받습니다.

        Dim result2 As String = oSOAP.GetRemainCount(smsID, hashValue)

        Label8.Text = result2.ToString()

    End Sub


    Public Shared Function MD5Encrypt(ByVal str As String) As String

        Dim md5 As MD5CryptoServiceProvider
        Dim bytValue() As Byte
        Dim bytHash() As Byte
        Dim strOutput As String
        Dim i As Integer

        md5 = New MD5CryptoServiceProvider

        bytValue = System.Text.Encoding.UTF8.GetBytes(str)

        bytHash = md5.ComputeHash(bytValue)
        md5.Clear()

        For i = 0 To bytHash.Length - 1
            strOutput &= bytHash(i).ToString("x").PadLeft(2, "0")
        Next

        MD5Encrypt = strOutput



    End Function


    Private Sub sms_main_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Dim oSOAP As New youiwe.ServiceSMS()
        Dim hashValue As String = MD5Encrypt(smsID + smsPwd)     '해쉬값을 받습니다.

        Dim result As String = oSOAP.GetRemainCount(smsID, hashValue)

        Label8.Text = result.ToString()


    End Sub

    Private Sub sms_content_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles sms_content.TextChanged
        Me.TextCut(Me.sms_content)
    End Sub

    Private Sub TextCut(ByRef CutTextBox As TextBox)

        If Me.String_Size(CutTextBox.Text) > 90 Then

            Dim Tmp As String = CutTextBox.Text

            Dim Position As Integer = CutTextBox.SelectionStart

            While Me.String_Size(Tmp) > 90

                Tmp = Tmp.Remove(Tmp.Length - 1, 1)

            End While

            CutTextBox.Text = Tmp

            CutTextBox.SelectionStart = Position

        End If

    End Sub
    Public Function String_Size(ByVal value As String) As Integer

        Return System.Text.Encoding.GetEncoding(949).GetBytes(value).GetLength(0)

    End Function

    Private Sub TextBox4_TextChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TextBox4.TextChanged

    End Sub
End Class
