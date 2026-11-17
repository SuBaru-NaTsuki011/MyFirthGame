# ULTRA — holat (2026-11-17)

| Bosqich | Holat | Asosiy fayllar |
|---|---|---|
| P1 Matn sig'ishi | Tayyor | `Core/Graphics/PixelFont.cs` (ClipRight), `UI/AIGovernorPanel.cs`, `UI/CivilizationView.cs`, `UI/StoryOverlay.cs`, `UI/StrategicView.cs`, `UI/UIManager.cs`, `UI/BattleScene.cs` |
| P2 Navigatsiya + DIAGNOSTIKA | Tayyor | `UI/UIManager.cs`, `UI/UIManager.Diagnostics.cs` |
| P3 Binoni ko'chirish | Tayyor | `Core/GameMode.cs`, `Buildings/BuildingManager.cs` (Move), `Buildings/ConstructionSystem.cs`, `SaveSystem/SaveGameData.cs` (MovedBuildings) |
| P4 Vaqt va fasllar | Tayyor | `Core/TimePrefs.cs`, `Core/GameClock.cs`, `Environment/SeasonVisuals.cs`, `Environment/WeatherManager.cs`, `World/MapManager.cs`, `World/WorldRenderer.cs`, `UI/UIManager.Time.cs` |
| P5 10 xil jangchi, mashg'ulot, jang maydoni | Tayyor | `Military/UnitType.cs`, `Core/GameStateManager.cs` (RecruitUnits, mashg'ulot), `UI/ArenaController.cs` |
| P6 Uy ichi: o'tirish, ovqatlanish, yotish | Tayyor | `Interiors/HouseInterior.cs` |
| P7 Fermerlar | Tayyor | `Economy/FarmingManager.cs`, `World/FarmWorkController.cs`, `UI/UIManager.Farming.cs` |
| P8 Tekshiruv | Tayyor | `AndijonSaltanatlar.Tests/UltraFeatureTests.cs` |

## Muhim qarorlar
- Pastki menyu: QURILISH · DIAGNOSTIKA (SHAHAR / AHOLI / QO'SHIN / DAVLAT) · FERMERLIK · TARAQQIYOT · DUNYO · SOZLAMALAR.
- 1 o'yin kuni = 60 real daqiqa (VAQT VA MUHIT oynasida 10/30/60/120). Jangchilarni tayyorlash vaqti endi o'yin **soati** bilan o'lchanadi.
- Vaqt va fasl qulfi faqat shu qurilmadagi ko'rinishga ta'sir qiladi (`vaqt_muhit.json`); iqtisod haqiqiy faslga bog'liq.
- Jang maydoni — mashq jangi: haqiqiy qo'shin tarkibi, `WarRules.ResolveRound`; jangchilar o'lmaydi, tajriba oladi. Onlayn PvP o'zgarmagan.
- Saroyni ko'chirib bo'lmaydi. Ko'chirilgan "yaratilgan" binolar `MovedBuildings` orqali saqlanadi.
- Fermer xaridi faqat o'yinchi tasdiqlasa bo'ladi; "Keyinroq" — shu so'rov 3 kun so'ralmaydi.

## Sinov
- `dotnet test`: 143/143.
- Skrinshotlar: 1280×720, 1920×1080, telefon 2400×1080 (`--phone`).
- Sinov parametrlari: `--tab diag|pop|army|buy|state|farm|time`, `--season 0..3`, `--arena`, `--farm-request`, `--enterhouse 0 --pose sit|eat|lie`.

## Keyingi
- Uy ichida oila a'zolari ham dasturxonga o'tirsin.
- Jangchilarning 10 turi uchun alohida spraytlar (hozircha 6 ta spraytdan foydalaniladi).
