# PROMPT 19A — online zanjirning uchidan-uchigacha sinovi (haqiqiy server, haqiqiy HTTP).
#   Foydalanish:  ./tools/e2e_online_test.ps1 -Server http://192.168.1.94:5080
#   Ikki akkaunt: Windows o'yinchisi (sotuvchi) va Android o'yinchisi (xaridor) — bitta WorldId.
param(
    [string]$Server = "http://localhost:5080",
    [int]$WorldId = 1,
    [string]$Tag = (Get-Date -Format "HHmmss"),
    [switch]$SkipWait
)
$ErrorActionPreference = "Stop"
$pass = 0; $fail = 0
function Check($name, $ok, $detail = "") {
    if ($ok) { $script:pass++; Write-Host "PASS | $name  $detail" -ForegroundColor Green }
    else { $script:fail++; Write-Host "FAIL | $name  $detail" -ForegroundColor Red }
}
function Api($method, $path, $token, $body) {
    $h = @{}; if ($token) { $h["Authorization"] = "Bearer $token" }
    $json = if ($body) { $body | ConvertTo-Json -Depth 6 } else { $null }
    return Invoke-RestMethod -Method $method -Uri "$Server$path" -Headers $h -Body $json -ContentType "application/json; charset=utf-8"
}
function NewId() { [guid]::NewGuid().ToString("N") }

$h = Invoke-RestMethod "$Server/health"
Check "Server ishlayapti" ($h.ok -eq $true) "provider=$($h.provider)"

# --- Ikki akkaunt ---
$a = Api POST "/api/auth/register" $null @{ Username = "pc_$Tag"; Email = "pc_$Tag@test.local"; Password = "Parol123!" }
$b = Api POST "/api/auth/register" $null @{ Username = "andr_$Tag"; Email = "andr_$Tag@test.local"; Password = "Parol123!" }
$b = Api POST "/api/auth/login" $null @{ Email = "andr_$Tag"; Password = "Parol123!" }   # username bilan kirish
Check "Ikki akkaunt login" ($a.token -and $b.token)
$sa = Api POST "/api/online/join" $a.token @{ WorldId = $WorldId; KingdomName = "Asaka $Tag" }
$sb = Api POST "/api/online/join" $b.token @{ WorldId = $WorldId; KingdomName = "Andijon $Tag" }
Check "Ikkala o'yinchi bitta dunyoda" ($sa.worldId -eq $sb.worldId -and $sa.kingdomId -ne $sb.kingdomId) "world=$($sa.worldId) A=$($sa.kingdomId) B=$($sb.kingdomId)"

# --- Guruch: maydon -> ekish -> o'sish -> hosil ---
$r = Api POST "/api/online/fields/build" $a.token @{ CommandId = (NewId) }
Check "Ekin maydoni qurildi" ($r.success -and $r.state.fields.Count -eq 1) $r.messageUz
$field = $r.state.fields[0].id
$coinBeforePlant = $r.state.coin
$plantCmd = NewId
$r = Api POST "/api/online/fields/plant" $a.token @{ CommandId = $plantCmd; FieldId = $field }
Check "Guruch ekildi" ($r.success -and $r.state.fields[0].state -eq 1) "$($r.messageUz) state=$($r.state.fields[0].state)"
$r2 = Api POST "/api/online/fields/plant" $a.token @{ CommandId = $plantCmd; FieldId = $field }
Check "Takroriy ekish buyrug'i qayta bajarilmadi" ($r2.duplicate -and $r2.state.coin -eq $r.state.coin) "tanga $($r.state.coin) -> $($r2.state.coin)"
$r = Api POST "/api/online/fields/harvest" $a.token @{ CommandId = (NewId); FieldId = $field }
Check "Yetilmagan hosil yig'ilmaydi" (-not $r.success) $r.messageUz

if (-not $SkipWait) {
    $wait = $r.state.fields[0].secondsToMature + 2
    Write-Host "Guruch yetilishini kutyapmiz: $wait soniya (server vaqti)..."
    $sawGrowing = $false
    $deadline = (Get-Date).AddSeconds($wait + 30)
    do {
        Start-Sleep -Seconds 10
        $s = Api GET "/api/online/state" $a.token $null
        if ($s.fields[0].state -eq 2) { $sawGrowing = $true }
        Write-Host ("  holat={0} progress={1:P0} qoldi={2}s" -f $s.fields[0].state, $s.fields[0].progress, $s.fields[0].secondsToMature)
    } while ($s.fields[0].state -ne 3 -and (Get-Date) -lt $deadline)
    Check "Guruch o'sdi (Growing bosqichi ko'rildi)" $sawGrowing
    Check "Guruch yetildi (Mature)" ($s.fields[0].state -eq 3)
}
$riceBefore = (Api GET "/api/online/state" $a.token $null).rice
$h1 = NewId; $h2 = NewId
$r = Api POST "/api/online/fields/harvest" $a.token @{ CommandId = $h1; FieldId = $field }
Check "Hosil yig'ildi va omborga qo'shildi" ($r.success -and $r.state.rice -eq $riceBefore + 120) "guruch $riceBefore -> $($r.state.rice)"
$r = Api POST "/api/online/fields/harvest" $a.token @{ CommandId = $h2; FieldId = $field }
Check "Bir hosil ikki marta yig'ilmaydi" (-not $r.success -and $r.state.rice -eq $riceBefore + 120) $r.messageUz

# --- Bozor: A 100 guruchni 500 tangaga sotuvga qo'yadi ---
$r = Api POST "/api/online/market/offers" $a.token @{ CommandId = (NewId); Quantity = 100; TotalPrice = 500 }
Check "Savdo taklifi yaratildi, guruch rezervga o'tdi" ($r.success -and $r.state.riceReserved -eq 100 -and $r.state.rice -eq 20) "ombor=$($r.state.rice) rezerv=$($r.state.riceReserved)"
$r = Api POST "/api/online/market/offers" $a.token @{ CommandId = (NewId); Quantity = 100; TotalPrice = 500 }
Check "Rezervdagi guruchni ikkinchi marta sotuvga qo'yib bo'lmaydi" (-not $r.success) $r.messageUz

$offers = Api GET "/api/online/market" $b.token $null
$offer = $offers | Where-Object { $_.sellerKingdomId -eq $sa.kingdomId } | Select-Object -First 1
Check "Ikkinchi o'yinchi taklifni ko'radi" ($null -ne $offer -and $offer.quantity -eq 100 -and $offer.totalPrice -eq 500) "offerId=$($offer.id)"

$A0 = Api GET "/api/online/state" $a.token $null
$B0 = Api GET "/api/online/state" $b.token $null
# Bir vaqtda ikki xil buyruq bilan sotib olishga urinish (parallel): faqat bittasi o'tishi kerak.
$jobs = 1..2 | ForEach-Object {
    $id = NewId
    Start-Job -ArgumentList $Server, $b.token, $offer.id, $id -ScriptBlock {
        param($srv, $tok, $oid, $cid)
        Invoke-RestMethod -Method POST -Uri "$srv/api/online/market/buy" -Headers @{ Authorization = "Bearer $tok" } `
            -Body (@{ CommandId = $cid; OfferId = $oid } | ConvertTo-Json) -ContentType "application/json"
    }
}
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
$ok = @($results | Where-Object { $_.success -and -not $_.duplicate }).Count
Check "Parallel 2 ta xarid: faqat bittasi bajarildi" ($ok -eq 1) (($results | ForEach-Object { $_.messageUz }) -join " / ")

$B1 = Api GET "/api/online/state" $b.token $null
Check "Xaridor tangasi rezervga olindi, karvon yo'lga chiqdi (PROMPT 19B)" ($B1.coin -eq $B0.coin - 500 -and $B1.rice -eq $B0.rice -and @($B1.caravans | Where-Object { $_.status -eq 0 }).Count -eq 1) "tanga $($B0.coin)->$($B1.coin), karvon $($B1.caravans[0].secondsLeft)s"
$deadline = (Get-Date).AddSeconds(220)
do { Start-Sleep -Seconds 5; $B1 = Api GET "/api/online/state" $b.token $null } while ($B1.rice -eq $B0.rice -and (Get-Date) -lt $deadline)
$A1 = Api GET "/api/online/state" $a.token $null
Check "Karvon yetdi: xaridorga +100 guruch" ($B1.coin -eq $B0.coin - 500 -and $B1.rice -eq $B0.rice + 100) "tanga $($B0.coin)->$($B1.coin), guruch $($B0.rice)->$($B1.rice)"
Check "Karvon yetdi: sotuvchiga +500 tanga, rezerv yopildi" ($A1.coin -eq $A0.coin + 500 -and $A1.riceReserved -eq 0) "tanga $($A0.coin)->$($A1.coin), rezerv $($A0.riceReserved)->$($A1.riceReserved)"
$again = Api POST "/api/online/market/buy" $b.token @{ CommandId = (NewId); OfferId = $offer.id }
$B2 = Api GET "/api/online/state" $b.token $null
Check "Sotilgan taklifni qayta sotib bo'lmaydi" (-not $again.success -and $B2.coin -eq $B1.coin) $again.messageUz
$hist = Api GET "/api/online/market/history" $a.token $null
Check "Savdo tarixida bitta yozuv" (@($hist | Where-Object { $_.offerId -eq $offer.id }).Count -eq 1)

# Keyingi bosqich (server restart) uchun natijalarni saqlaymiz
@{ tokenA = $a.token; tokenB = $b.token; coinA = $A1.coin; coinB = $B1.coin; riceB = $B1.rice; offerId = $offer.id } |
    ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot "e2e_last_state.json")
Write-Host "NATIJA: $pass PASS / $fail FAIL"
if ($fail -gt 0) { exit 1 }
