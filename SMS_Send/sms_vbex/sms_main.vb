Imports System.Security.Cryptography
Imports CPUTILLib

Public Class sms_main
    Public MAX_WARINING_COUNT = 10
    Public smsID As String = "jeanion" '부여 받은 너나우리 SMS 아이디를 넣으십시요.
    Public smsPwd As String = "critical101*" '부여 받은 너나우리 SMS 패스워드를 넣으십시요.
    Public LogFileName As String
    Public WarningFileName As String
    Public LastWarningTime As Date
    Public WarningHistory As String
    Public WarningCount As Integer = 0

    Public LogFileFolder As String = "E:\LogFileFolder"
    'Public DaishinServerObj As CpCybos
    Public PriceMinerRunning As Boolean = False

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

        'Get the log file name
        LogFileName = LogFileFolder & "\" & "log" & Now.Year.ToString("D4") & Now.Month.ToString("D2") & Now.Day.ToString("D2") & ".txt"
        WarningFileName = LogFileFolder & "\" & "warning.txt"
        LastWarningTime = My.Computer.FileSystem.GetFileInfo(WarningFileName).LastWriteTime
        WarningHistory = My.Computer.FileSystem.ReadAllText(WarningFileName)

        'Log file check
        Dim file_info As System.IO.FileInfo = My.Computer.FileSystem.GetFileInfo(LogFileName)
        Dim retry_count As Integer = 0
        While Now.TimeOfDay - file_info.LastWriteTime.TimeOfDay > TimeSpan.FromSeconds(40)
            Threading.Thread.Sleep(100)
            retry_count += 1
            If retry_count > 50 Then
                Exit While
            End If
        End While

        retry_count = 0 'test
        If retry_count > 50 Then
            'PriceMiner 안 도는 것 같다.
            Me.Text = "PriceMiner 안 도는 것 같아요. 난 그냥 쉬고 있을 께요."
            Process.Start("C:\DAISHIN\STARTER\ncStarter.exe", "/prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")
        Else
            'PriceMiner 돈다
            PriceMinerRunning = True

#If 0 Then
            '대신 서비스 도나 확인
            Dim cp_start_checked As Boolean = False
            Dim dib_server_checked As Boolean = False
            For Each prog As Process In Process.GetProcesses()
                If prog.ProcessName = "CpStart" Then
                    cp_start_checked = True
                End If
                If prog.ProcessName = "DibServer" Then
                    dib_server_checked = True
                End If
            Next
            If cp_start_checked AndAlso dib_server_checked Then
                '대신 서비스 잘 돌아가고 있음 => 돌아가게 그냥 둔다.
#If 0 Then
                Process.Start("C:\DAISHIN\STARTER\ncStarter.exe", "/prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")
#End If
            Else
                '대신 서비스 안 돌아감 => 서비스 start
                Process.Start("C:\DAISHIN\STARTER\ncStarter.exe", "/prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")

                '서비스 프로세스 시작된 거 확인한다.
                While Not (cp_start_checked = True AndAlso dib_server_checked = True)
                    For Each prog As Process In Process.GetProcesses()
                        If prog.ProcessName = "CpStart" Then
                            cp_start_checked = True
                        End If
                        If prog.ProcessName = "DibServer" Then
                            dib_server_checked = True
                        End If
                    Next
                    Threading.Thread.Sleep(10)
                End While

            End If
            Me.Text = Me.Text & "_20210530"
#End If

            tm_MyTimer.Start()
        End If
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

    Private Sub tm_MyTimer_Tick(sender As Object, e As EventArgs) Handles tm_MyTimer.Tick
        'Log file check
        Dim file_info As System.IO.FileInfo = My.Computer.FileSystem.GetFileInfo(LogFileName)

        If Now.TimeOfDay - file_info.LastWriteTime.TimeOfDay > TimeSpan.FromSeconds(40) Then
            PriceMinerRunning = True

#If 1 Then
            Dim hashValue As String
            Dim smsContent As String
            Dim receivePhone As String
            Dim senderPhone As String
            Dim oSOAP As New youiwe.ServiceSMS()

            senderPhone = "0316454159"    '보내는 분 핸드폰번호
            receivePhone = "01033529263"   '받는 분 핸드폰번호
            smsContent = "프로그램이 돌아가지 않습니다. 확인해 주세요."  '전문 내용

            hashValue = MD5Encrypt(smsID + smsPwd + receivePhone)     '해쉬값을 받습니다.

            Dim result As String = oSOAP.SendSMS(smsID, hashValue, senderPhone, receivePhone, smsContent)

            Me.Text = "결과코드:" + result
#End If

            tm_MyTimer.Stop()
        End If

        'Warning file check
        Dim warning_file_info As System.IO.FileInfo = My.Computer.FileSystem.GetFileInfo(WarningFileName)

        If warning_file_info.LastWriteTime <> LastWarningTime Then
            Dim new_warning_history As String = My.Computer.FileSystem.ReadAllText(WarningFileName)
            Dim updated_history As String = new_warning_history.Substring(WarningHistory.Length, new_warning_history.Length - WarningHistory.Length)
            Dim breakdown_string As String() = updated_history.Replace(vbCrLf, vbLf).Split(vbLf)
            Dim hashValue As String
            'Dim smsContent As String
            Dim receivePhone As String
            Dim senderPhone As String
            Dim oSOAP As New youiwe.ServiceSMS()

            WarningCount += breakdown_string.Length
            If WarningCount > MAX_WARINING_COUNT Then
                breakdown_string = {"Too many warning messages" & vbCrLf}
            End If
            senderPhone = "0316454159"    '보내는 분 핸드폰번호
            receivePhone = "01033529263"   '받는 분 핸드폰번호
            'smsContent = updated_history   '전문 내용

            For index As Integer = 0 To breakdown_string.Length - 1
                breakdown_string(index) = Trim(breakdown_string(index))
                If breakdown_string(index) <> "" Then
                    hashValue = MD5Encrypt(smsID + smsPwd + receivePhone)     '해쉬값을 받습니다.
                    Dim result As String = oSOAP.SendSMS(smsID, hashValue, senderPhone, receivePhone, breakdown_string(index))
#If 0 Then
                    If breakdown_string(index) = "Cybos service restart requested" Then
                        Dim daishin_restart_thread As Threading.Thread = New Threading.Thread(AddressOf DaishinRestartThread)
                        daishin_restart_thread.IsBackground = True
                        'IsLoadingDone = False
                        daishin_restart_thread.Start()     '시뮬레이션 스레드 돌리고  빠져나옴
                        'IsThreadExecuting = True

                    End If
#End If
                End If
            Next

            WarningHistory = new_warning_history
            LastWarningTime = My.Computer.FileSystem.GetFileInfo(WarningFileName).LastWriteTime
            If WarningCount > MAX_WARINING_COUNT Then
                tm_MyTimer.Stop()
            End If
        End If
    End Sub

    Public Sub DaishinRestartThread()
        '재연결을 시도한다.
        Dim cp_start_checked As Boolean = False
        Dim dib_server_checked As Boolean = False
        Dim alive_time_count As Integer = 0
        'task killing
        For Each prog As Process In Process.GetProcesses()
            If prog.ProcessName = "CpStart" Then
                prog.Kill()
            End If
            If prog.ProcessName = "coStarter" Then
                prog.Kill()
            End If
            If prog.ProcessName = "DibServer" Then
                prog.Kill()
            End If
        Next

        'confirm the process is really killed
        Dim is_really_killed As Boolean = False
        While (Not is_really_killed)
            is_really_killed = True
            For Each prog As Process In Process.GetProcesses()
                If prog.ProcessName = "CpStart" Then
                    is_really_killed = False
                End If
                If prog.ProcessName = "coStarter" Then
                    is_really_killed = False
                End If
                If prog.ProcessName = "DibServer" Then
                    is_really_killed = False
                End If
            Next

            Threading.Thread.Sleep(10)
        End While

        '프로그램 재시작
        'Process.Start("C:\DAISHIN\STARTER\ncStarter.exe /prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")
        'Dim start_info As New ProcessStartInfo
        'start_info.FileName = "C:\DAISHIN\STARTER\ncStarter.exe"
        'start_info.Arguments = "/prj:cp /id:jeanion /pwd:qlfkdjdt /autostart"
        'start_info.UseShellExecute = True
        'Process.Start(start_info)
        Process.Start("C:\DAISHIN\STARTER\ncStarter.exe", "/prj:cp /id:jeanion /pwd:qlfkdjdt /autostart")
    End Sub
End Class
