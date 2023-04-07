# Load Log
$logPath = "$($env:USERPROFILE)\Downloads\Bedroom TV Agent\logs"
$logFilename = "$(Get-Date -Format "yyyy-MM-dd")"
IF(!(Test-Path $logPath)) { New-Item -Path $logPath -ItemType Directory -Force | Out-Null }

# Load Config
$configpath = "$($env:USERPROFILE)\Downloads\Bedroom TV Agent\configs"
$Settings = Get-Content -Path "$($configpath)\settings.cfg" | ConvertFrom-Json

$blacklist = Import-CSV "$($configpath)\blacklist.cfg"
$blacklistedtvs = $blacklist | WHERE { ((Get-Date) -ge (Get-Date("$($_.startdate) $($_.starttime)"))) -and ((Get-Date) -lt (Get-Date("$($_.enddate) $($_.endtime)"))) }

$tvlist = Import-CSV "$($configpath)\tvlist.cfg"

# Determine if running during permitted time
$permittedtimefound = $false
$tvschedule_normal = Import-CSV "$($configpath)\tvschedule_normal.cfg"
ForEach ($timeslot in ($tvschedule_normal | WHERE { $_.DayOfWeek -eq (Get-Date).DayOfWeek } )) {
    if (((Get-Date) -ge (Get-Date($timeslot.StartTime))) -and ((Get-Date) -le ((Get-Date($timeslot.EndTime)).AddMinutes($Settings.GracePeriodMins)))) { $permittedtimefound = $true }
}

$blockedtimefound = $false
$tvschedule_special = Import-CSV "$($configpath)\tvschedule_special.cfg"
ForEach ($timeslot in $tvschedule_special) {
    if (((Get-Date) -ge (Get-Date("$($timeslot.StartDate) $($timeslot.StartTime)"))) -and ((Get-Date) -le (Get-Date("$($timeslot.EndDate) $($timeslot.EndTime)")))) {
        if ($timeslot.Action -eq "Allow") { $permittedtimefound = $true }
        if ($timeslot.Action -eq "Block") { $blockedtimefound = $true }
    }
}

if (($permittedtimefound -eq $false) -or ($blockedtimefound -eq $true)) {
    $IsOutsidePermittedTime = $true
} else {
    $IsOutsidePermittedTime = $false
}


ForEach ($tv in $tvlist) {
    Write-Host "Processing: $($tv.Description) ($($tv.IP))" -ForegroundColor Cyan
    if ($tv.PingTest -eq 1) { 
        $pingsuccess = $false
        $pingtries = 0
        do {
            $pingtries += 1
            $pingtest = ping $tv.IP -n 1
            if ($pingtest -like "*Reply from $($tv.IP)*") { $pingsuccess = $true }
        } while (($pingsuccess -eq $false) -and ($pingtries -lt 3))
    } else {
        $pingsuccess = $true
    }

    if ($pingsuccess -eq $true) {
        $deviceinfo = (Invoke-RestMethod -Method Get -Uri "http://$($tv.IP):8060/query/device-info").'device-info'
        if ($deviceinfo.'power-mode' -eq "PowerOn") {
            $mediaplayer = (Invoke-RestMethod -Method Get -Uri "http://$($tv.IP):8060/query/media-player").player
            if ($blacklistedtvs.ip -contains $TV.IP) {
                Write-Host "$($TV.Description) ($($deviceinfo.'friendly-device-name')) observed powered on while blacklisted. Powering off." -ForegroundColor Red
                Invoke-RestMethod -Method Post -Uri "http://$($TV.IP):8060/keypress/PowerOff"
                Start-Sleep -Seconds 2
                $deviceinfo = (Invoke-RestMethod -Method Get -Uri "http://$($TV.IP):8060/query/device-info").'device-info'                
                "[ $(Get-Date -Format "MM/dd/yyyy hh:mm:ss tt") ] BLACKLISTED $($TV.Description) ($($deviceinfo.'friendly-device-name')) was observed powered on watching $($mediaplayer.plugin.name). Power off signal sent. Current device status is now $($deviceinfo.'power-mode')." | Out-File -FilePath "$($logPath)\$($logFilename).log" -Append
            }
            
            if ($IsOutsidePermittedTime -eq $true) {
                Write-Host "$($TV.Description) ($($deviceinfo.'friendly-device-name')) observed powered on outside permitted hours. Powering off." -ForegroundColor Red
                Invoke-RestMethod -Method Post -Uri "http://$($TV.IP):8060/keypress/PowerOff"
                Start-Sleep -Seconds 2
                $deviceinfo = (Invoke-RestMethod -Method Get -Uri "http://$($TV.IP):8060/query/device-info").'device-info'                
                "[ $(Get-Date -Format "MM/dd/yyyy hh:mm:ss tt") ] $($TV.Description) ($($deviceinfo.'friendly-device-name')) was observed powered on watching $($mediaplayer.plugin.name). Power off signal sent. Current device status is now $($deviceinfo.'power-mode')." | Out-File -FilePath "$($logPath)\$($logFilename).log" -Append
                
            }

            $approvedappinuse = $false
            if ($Settings.ApprovedApps -contains $mediaplayer.plugin.name) { $approvedappinuse = $true }

            if ($approvedappinuse -eq $false) {
                "[ $(Get-Date -Format "MM/dd/yyyy hh:mm:ss tt") ] $($TV.Description) ($($deviceinfo.'friendly-device-name')) was observed running an unapproved app ($($mediaplayer.plugin.name))" | Out-File -FilePath "$($logPath)\$($logFilename).log" -Append
            }
        }
    } else {
        "[ $(Get-Date -Format "MM/dd/yyyy hh:mm:ss tt") ] TV ($($TV.IP)) not reachable"
    }
}
    
#$ip = "172.16.100.193"
#$ip = "10.100.100.149"
#Invoke-RestMethod -Method Post -Uri "http://$($ip):8060/keypress/PowerOff"
#Invoke-RestMethod -Method Post -Uri "http://$($ip):8060/keypress/PowerOn"

#$mediaplayer = Invoke-RestMethod -Method Get -Uri "http://$($ip):8060/query/media-player"
#$mediaplayer.player.plugin


#$deviceinfo.'device-info'
#$deviceinfo.'device-info'.'power-mode'


#$tvinfo = Invoke-RestMethod -Method Get -Uri "http://$($ip):8060/query/tv-channels"

#$test = Invoke-RestMethod -Method Get -Uri "http://$($ip):8060/query/sgnodes/all"
