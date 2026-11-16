# PROMPT 19B - hudud qo'shish va hokim zanjiri (haqiqiy server, haqiqiy HTTP, ikki akkaunt).
#   ./tools/e2e_kingdom_test.ps1 -Server http://192.168.1.94:5080
param([string]$Server = "http://localhost:5080", [string]$Tag = (Get-Date -Format "HHmmss"))
$ErrorActionPreference = "Stop"
$pass = 0; $fail = 0
function Check($name, $ok, $detail = "") {
    if ($ok) { $script:pass++; Write-Host "PASS | $name  $detail" -ForegroundColor Green }
    else { $script:fail++; Write-Host "FAIL | $name  $detail" -ForegroundColor Red }
}
function Api($method, $path, $token, $body) {
    $h = @{}; if ($token) { $h["Authorization"] = "Bearer $token" }
    $json = if ($body) { $body | ConvertTo-Json -Depth 6 } else { $null }
    Invoke-RestMethod -Method $method -Uri "$Server$path" -Headers $h -Body $json -ContentType "application/json; charset=utf-8"
}
function NewId() { [guid]::NewGuid().ToString("N") }

$a = Api POST "/api/auth/register" $null @{ Username = "win_k$Tag"; Email = "win_k$Tag@t.local"; Password = "Parol123!" }
$b = Api POST "/api/auth/register" $null @{ Username = "and_k$Tag"; Email = "and_k$Tag@t.local"; Password = "Parol123!" }
$W = 100 + ([int]$Tag % 90000)   # har bir sinov - o'z toza dunyosi
$sa = Api POST "/api/online/join" $a.token @{ WorldId = $W }
$sb = Api POST "/api/online/join" $b.token @{ WorldId = $W }
Check "Ikki o'yinchi bitta dunyoda" ($sa.worldId -eq $sb.worldId)

$regions = Api GET "/api/kingdom/regions" $a.token $null
Check "Dunyo xaritasida hududlar bor" ($regions.Count -ge 8) "$($regions.Count) hudud"
# Sinov uchun o'yinchi A ga yetarli tanga (faqat test bazasida): mustaqil hudud tanlanadi - elchi shart emas.
$target = $regions | Where-Object { $_.ownerKingdomId -ne $sa.kingdomId -and $_.ownerName -ne "" -and -not $_.isMine } |
          Sort-Object { -[int]$_.canAnnex }, annexCost | Select-Object -First 1
$ai = $regions | Where-Object { $_.name -eq "Shahrixon" }
Check "AI hududi (Shahrixon) dastlab birlashtirib bo'lmaydi (munosabat past)" (-not $ai.canAnnex) $ai.annexBlockReasonUz

$e1 = Api POST "/api/kingdom/envoy" $a.token @{ CommandId = (NewId); RegionId = $ai.id }
$e2 = Api POST "/api/kingdom/envoy" $a.token @{ CommandId = (NewId); RegionId = $ai.id }
$ai2 = (Api GET "/api/kingdom/regions" $a.token $null) | Where-Object { $_.id -eq $ai.id }
Check "Elchilar munosabatni oshirdi" ($e1.success -and $e2.success -and $ai2.relation -ge 20) "munosabat $($ai2.relation), tanga $($e2.state.coin)"

$ovA0 = Api GET "/api/kingdom/overview" $a.token $null
if ($ovA0.coin -lt $ai2.annexCost) {
    Write-Host "  (Shahrixon narxi $($ai2.annexCost) > tanga $($ovA0.coin); mustaqil kichik hudud bilan davom etamiz)"
    $ai2 = (Api GET "/api/kingdom/regions" $a.token $null) | Where-Object { $_.ownerKingdomId -eq 0 -and $_.canAnnex } | Sort-Object annexCost | Select-Object -First 1
}
$cityPop = $ai2.population
$annex = Api POST "/api/kingdom/annex" $a.token @{ CommandId = (NewId); RegionId = $ai2.id }
Check "Diplomatik birlashish (server validatsiyasi) o'tdi" $annex.success $annex.messageUz
$again = Api POST "/api/kingdom/annex" $b.token @{ CommandId = (NewId); RegionId = $ai2.id }
Check "Ikkinchi o'yinchi o'sha hududni ola olmaydi" (-not $again.success) $again.messageUz

$ovA = Api GET "/api/kingdom/overview" $a.token $null
Check "Shahar va aholi yangi saltanatga o'tdi (bir marta)" ($ovA.cityCount -eq 2 -and $ovA.population -eq $ovA0.population + $cityPop) "aholi $($ovA0.population) -> $($ovA.population), shaharlar $($ovA.cityCount)"
$seenByB = (Api GET "/api/kingdom/regions" $b.token $null) | Where-Object { $_.id -eq $ai2.id }
Check "Ikkinchi clientda xarita yangilandi (egasi o'zgardi)" ($seenByB.ownerKingdomId -eq $sa.kingdomId) "$($seenByB.name) -> $($seenByB.ownerName)"
$note = $ovA.advisorNotes | Where-Object { $_ -like "*$($ai2.name)*" } | Select-Object -First 1
Check "Donishmand Vazir yangi shahar haqida tavsiya berdi" ($null -ne $note) $note

# --- Hokim zanjiri: ikkinchi o'yinchi (tangasi yetarli) yangi shahar oladi va hokim orqali quradi ---
$freeB = (Api GET "/api/kingdom/regions" $b.token $null) | Where-Object { $_.canAnnex } | Sort-Object annexCost | Select-Object -First 1
$annexB = Api POST "/api/kingdom/annex" $b.token @{ CommandId = (NewId); RegionId = $freeB.id }
Check "Ikkinchi o'yinchi ham yangi hudud qo'shdi" $annexB.success "$($freeB.name): $($annexB.messageUz)"
$ovB = Api GET "/api/kingdom/overview" $b.token $null
$city = $ovB.cities | Where-Object { -not $_.isCapital } | Select-Object -First 1
$m = Api POST "/api/kingdom/governor/mode" $b.token @{ CommandId = (NewId); CityId = $city.id; Mode = 2 }
$r = Api POST "/api/kingdom/governor/analyze" $b.token @{ CommandId = (NewId); CityId = $city.id }
$ov2 = Api GET "/api/kingdom/overview" $b.token $null
$cap0 = $ov2.cities | Where-Object { $_.id -eq $city.id }
$plan = $cap0.pendingPlan
Check "Hokim shaharni tahlil qilib reja tuzdi" ($m.success -and $r.success -and $null -ne $plan) "$($plan.titleUz): $($plan.houses) uy, $($plan.markets) bozor, $($plan.gardens) bog' = $($plan.cost) tanga ($($r.messageUz))"
$coin0 = $ov2.coin
$ok = Api POST "/api/kingdom/governor/approve" $b.token @{ CommandId = (NewId); PlanId = $plan.id }
Check "Reja tasdiqlandi va xarajat bir marta yechildi" ($ok.success -and $ok.state.coin -eq $coin0 - $plan.cost) "$($ok.messageUz) (tanga $coin0 -> $($ok.state.coin))"
$ok2 = Api POST "/api/kingdom/governor/approve" $b.token @{ CommandId = (NewId); PlanId = $plan.id }
Check "Xuddi shu rejani ikkinchi marta tasdiqlab bo'lmaydi" (-not $ok2.success) $ok2.messageUz
Start-Sleep -Seconds 25
$c2 = (Api GET "/api/kingdom/overview" $b.token $null).cities | Where-Object { $_.id -eq $city.id }
Check "Qurilish tugagach shahar statistikasi yangilandi" ($c2.buildings -ge 1 -and ($c2.housingCapacity -gt $cap0.housingCapacity -or $c2.jobs -gt $cap0.jobs)) "sig'im $($cap0.housingCapacity)->$($c2.housingCapacity), ish $($cap0.jobs)->$($c2.jobs), binolar $($c2.buildings)"

Write-Host "NATIJA: $pass PASS / $fail FAIL"
if ($fail -gt 0) { exit 1 }
