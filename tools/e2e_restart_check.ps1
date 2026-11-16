# e2e_online_test.ps1 dan keyin, SERVER QAYTA ISHGA TUSHIRILGACH ishlatiladi:
# saqlangan tanga/guruch va bajarilgan savdo yo'qolmaganini tekshiradi.
param([string]$Server = "http://localhost:5080")
$ErrorActionPreference = "Stop"
$saved = Get-Content (Join-Path $PSScriptRoot "e2e_last_state.json") -Raw | ConvertFrom-Json
function Get-State($tok) { Invoke-RestMethod -Uri "$Server/api/online/state" -Headers @{ Authorization = "Bearer $tok" } }
$a = Get-State $saved.tokenA
$b = Get-State $saved.tokenB
$offers = Invoke-RestMethod -Uri "$Server/api/online/market" -Headers @{ Authorization = "Bearer $($saved.tokenB)" }
$hist = Invoke-RestMethod -Uri "$Server/api/online/market/history" -Headers @{ Authorization = "Bearer $($saved.tokenB)" }
$fail = 0
function Check($n, $ok, $d) { if ($ok) { Write-Host "PASS | $n  $d" -ForegroundColor Green } else { $script:fail++; Write-Host "FAIL | $n  $d" -ForegroundColor Red } }
Check "Restartdan keyin sotuvchi tangasi saqlandi" ($a.coin -eq $saved.coinA) "$($saved.coinA) -> $($a.coin)"
Check "Restartdan keyin xaridor tangasi saqlandi" ($b.coin -eq $saved.coinB) "$($saved.coinB) -> $($b.coin)"
Check "Restartdan keyin xaridor guruchi saqlandi" ($b.rice -eq $saved.riceB) "$($saved.riceB) -> $($b.rice)"
Check "Sotilgan taklif bozorda qayta paydo bo'lmadi" (@($offers | Where-Object { $_.id -eq $saved.offerId }).Count -eq 0)
Check "Savdo yozuvi saqlandi" (@($hist | Where-Object { $_.offerId -eq $saved.offerId }).Count -eq 1)
if ($fail -gt 0) { exit 1 } else { Write-Host "NATIJA: restart sinovi o'tdi" }
