' Load Log
Dim logPath As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\Downloads\Bedroom TV Agent\logs"
Dim logFilename As String = DateTime.Now.ToString("yyyy-MM-dd")
If Not System.IO.Directory.Exists(logPath) Then
    System.IO.Directory.CreateDirectory(logPath)
End If

' Load Config
Dim configpath As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) & "\Downloads\Bedroom TV Agent\configs"
Dim settingsJson As String = System.IO.File.ReadAllText(configpath & "\settings.cfg")
Dim settings As JObject = JObject.Parse(settingsJson)

Dim blacklist As DataTable = New DataTable()
blacklist = GetDataTableFromCsv(configpath & "\blacklist.cfg")
Dim blacklistedtvs As EnumerableRowCollection(Of DataRow) = blacklist.AsEnumerable().Where(Function(row) (DateTime.Now >= DateTime.Parse(row("startdate").ToString() & " " & row("starttime").ToString())) And (DateTime.Now < DateTime.Parse(row("enddate").ToString() & " " & row("endtime").ToString())))

Dim tvlist As DataTable = New DataTable()
tvlist = GetDataTableFromCsv(configpath & "\tvlist.cfg")

' Determine if running during permitted time
Dim permittedtimefound As Boolean = False
Dim tvschedule_normal As DataTable = New DataTable()
tvschedule_normal = GetDataTableFromCsv(configpath & "\tvschedule_normal.cfg")
For Each row As DataRow In tvschedule_normal.AsEnumerable().Where(Function(row) row("DayOfWeek") = DateTime.Now.DayOfWeek)
    If (DateTime.Now >= DateTime.Parse(row("StartTime").ToString())) And (DateTime.Now <= DateTime.Parse(row("EndTime").ToString()).AddMinutes(settings("GracePeriodMins").ToObject(Of Integer)())) Then
        permittedtimefound = True
    End If
Next

Dim blockedtimefound As Boolean = False
Dim tvschedule_special As DataTable = New DataTable()
tvschedule_special = GetDataTableFromCsv(configpath & "\tvschedule_special.cfg")
For Each row As DataRow In tvschedule_special.AsEnumerable()
    If (DateTime.Now >= DateTime.Parse(row("StartDate").ToString() & " " & row("StartTime").ToString())) And (DateTime.Now <= DateTime.Parse(row("EndDate").ToString() & " " & row("EndTime").ToString())) Then
        If row("Action").ToString() = "Allow" Then
            permittedtimefound = True
        ElseIf row("Action").ToString() = "Block" Then
            blockedtimefound = True
        End If
    End If
Next

Dim IsOutsidePermittedTime As Boolean
If (permittedtimefound = False) Or (blockedtimefound = True) Then
    IsOutsidePermittedTime = True
Else
    IsOutsidePermittedTime = False
End If

For Each row As DataRow In tvlist.AsEnumerable()
    Console.ForegroundColor = ConsoleColor.Cyan
    Console.WriteLine("Processing: " & row("Description").ToString() & " (" & row("IP").ToString() & ")")
    Console.ResetColor()
    If row("PingTest").ToString() = "1" Then
        Dim pingsuccess As Boolean = False
        Dim pingtries As Integer = 0
        Do
            pingtries += 1
            Dim pingtest As String = RunCommandAndCaptureOutput("ping " & row("IP").ToString() & " -n 1")
            If pingtest.Contains("Reply from " & row("IP").ToString()) Then
                pingsuccess = True
            End If
        Loop While (pingsuccess = False) And (pingtries < 3)
    Else
        pingsuccess = True
        
If pingsuccess = True Then
    Dim deviceinfo As String = (Invoke-RestMethod -Method Get -Uri "http://" & tv.IP & ":8060/query/device-info")("device-info")
    If deviceinfo("power-mode") = "PowerOn" Then
        Dim mediaplayer As String = (Invoke-RestMethod -Method Get -Uri "http://" & tv.IP & ":8060/query/media-player")("player")
        If blacklistedtvs.ip.Contains(TV.IP) Then
            Console.WriteLine(TV.Description & " (" & deviceinfo("friendly-device-name") & ") observed powered on while blacklisted. Powering off.")
            Invoke-RestMethod -Method Post -Uri "http://" & TV.IP & ":8060/keypress/PowerOff"
            Threading.Thread.Sleep(2000)
            deviceinfo = (Invoke-RestMethod -Method Get -Uri "http://" & TV.IP & ":8060/query/device-info")("device-info")
            My.Computer.FileSystem.WriteAllText(logPath & "\" & logFilename & ".log", "[ " & DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") & " ] BLACKLISTED " & TV.Description & " (" & deviceinfo("friendly-device-name") & ") was observed powered on watching " & mediaplayer("plugin")("name") & ". Power off signal sent. Current device status is now " & deviceinfo("power-mode") & "." & vbCrLf, True)
        End If

        If IsOutsidePermittedTime = True Then
            Console.WriteLine(TV.Description & " (" & deviceinfo("friendly-device-name") & ") observed powered on outside permitted hours. Powering off.")
            Invoke-RestMethod -Method Post -Uri "http://" & TV.IP & ":8060/keypress/PowerOff"
            Threading.Thread.Sleep(2000)
            deviceinfo = (Invoke-RestMethod -Method Get -Uri "http://" & TV.IP & ":8060/query/device-info")("device-info")
            My.Computer.FileSystem.WriteAllText(logPath & "\" & logFilename & ".log", "[ " & DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") & " ] " & TV.Description & " (" & deviceinfo("friendly-device-name") & ") was observed powered on watching " & mediaplayer("plugin")("name") & ". Power off signal sent. Current device status is now " & deviceinfo("power-mode") & "." & vbCrLf, True)
        End If

        Dim approvedappinuse As Boolean = False
        If Settings.ApprovedApps.Contains(mediaplayer("plugin")("name")) Then
            approvedappinuse = True
        End If

        If approvedappinuse = False Then
            My.Computer.FileSystem.WriteAllText(logPath & "\" & logFilename & ".log", "[ " & DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") & " ] " & TV.Description & " (" & deviceinfo("friendly-device-name") & ") was observed running an unapproved app (" & mediaplayer("plugin")("name") & ")" & vbCrLf, True)
        End If
    End If
Else
    My.Computer.FileSystem.WriteAllText(logPath & "\" & logFilename & ".log", "[ " & DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") & " ] TV (" & TV.IP & ") not reachable" & vbCrLf, True)
End If

'Invoke-RestMethod -Method Post -Uri "http://" & ip & ":8060/keypress/PowerOff"
'Invoke-RestMethod -Method Post -Uri "http://" & ip & ":8060/
