# ANDIJON: SALTANATLAR — doimiy ishlayotgan serverni (faqat port 5081) va tunnelni to'xtatish. Boshqa serverlarga tegmaydi.
# Keyingi safar Windows'ga kirganda server yana o'zi ishga tushadi.
# Avtomatik ishga tushishni butunlay o'chirish:  .\Server_toxtatish.ps1 -Butunlay
param([switch]$Butunlay)

$task = "Andijon Saltanatlar Server"
Stop-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue
Get-CimInstance Win32_Process | Where-Object {
    ($_.Name -eq "powershell.exe" -and $_.CommandLine -match "Server_doimiy\.ps1") -or
    ($_.Name -eq "dotnet.exe" -and $_.CommandLine -match "AndijonSaltanatlar\.Server\.dll" -and $_.CommandLine -match "0\.0\.0\.0:5081") -or
    ($_.Name -eq "cloudflared.exe" -and $_.CommandLine -match "localhost:5081")
} | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue; Write-Host "To'xtatildi: $($_.Name) ($($_.ProcessId))" }

if ($Butunlay) {
    Unregister-ScheduledTask -TaskName $task -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host "Avtomatik ishga tushish o'chirildi."
}
