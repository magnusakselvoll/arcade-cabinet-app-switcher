param([string]$InstallDir)
$exe = Join-Path $InstallDir 'ArcadeCabinetSwitcher.exe'
$a = New-ScheduledTaskAction -Execute $exe
$t = New-ScheduledTaskTrigger -AtLogOn
$s = New-ScheduledTaskSettingsSet -RestartCount 3 -RestartInterval (New-TimeSpan -Seconds 5) -DisallowStartIfOnBatteries:$false -Hidden
$p = New-ScheduledTaskPrincipal -UserId "$env:USERNAME" -LogonType Interactive -RunLevel Limited
Register-ScheduledTask ArcadeCabinetSwitcher -Action $a -Trigger $t -Settings $s -Principal $p -Force
