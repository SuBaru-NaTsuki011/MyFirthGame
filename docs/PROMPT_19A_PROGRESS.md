# PROMPT 19A — Windows + Android crossplay, guruch, o'yinchilar savdosi

Sana: 2026-11-15 · Holat: **server + Windows client ishlaydi va sinovdan o'tdi; Android APK yig'iladi, real telefonda SINALMAGAN**

## Arxitektura (server-authoritative)

```
Windows client (MonoGame DesktopGL) ─┐                       ┌─ SQLite (lokal sinov, standart)
                                     ├─ REST /api/online ─┐  │
Android client (MonoGame Android) ───┘   SignalR /gamehub ├─ ASP.NET Core server ─┤
         (bir xil o'yin kodi, link)                       ┘                       └─ PostgreSQL (docker-compose, prod)
                    AndijonSaltanatlar.Shared: RiceFarmingRules, MarketRules, DTO'lar
```

- Client faqat buyruq yuboradi (`CommandId` bilan); natija va holat faqat serverdan.
- Guruch o'sishi **server o'yin vaqtiga** bog'liq: `World.EpochUtc` dan hisoblanadi (1 real soniya = 1 o'yin daqiqasi; ekilgandan 1 daqiqada "O'smoqda", 3 daqiqada "Yetildi").
- Holatlar: `Empty → Planted → Growing → Mature → Harvested` (→ yana ekish mumkin). Suv: 1 ariq = 4 ta sug'oriladigan maydon.

## Ikki marta bajarilmaslik kafolatlari

| Xavf | Himoya |
|------|--------|
| Hosilni 2 marta yig'ish | `UPDATE RiceFields ... WHERE Id=@id AND Version=@v AND State=Planted` — ikkinchi so'rov 0 qator o'zgartiradi |
| Bitta taklifni 2 xaridor oladi | `UPDATE MarketOffers SET Status=Sold WHERE Id=@id AND Status=Active` + `TradeTransactions.OfferId` UNIQUE |
| Xaridor tangasi manfiy | `UPDATE ... SET Coin=Coin-@p WHERE Coin>=@p`; yetmasa butun tranzaksiya bekor, taklif Active qoladi |
| Bir guruchni 2 taklifga qo'yish | Taklif yaratilganda guruch `Rice → RiceReserved` ga o'tadi (shartli UPDATE) |
| Tarmoq uzilib so'rov qayta yuborilsa | `GameCommands.CommandId` PK — takroriy buyruq "allaqachon bajarilgan" deb qaytadi |

## O'zgartirilgan / yangi fayllar

**Shared:** `Farming/RiceFarmingRules.cs` (yangi), `DTOs/OnlineDtos.cs` (yangi)
**Server:** `Data/Entities.cs` (Rice, RiceReserved, WaterSources, RiceFieldEntity, MarketOfferEntity, TradeTransactionEntity, World.EpochUtc), `Data/GameDbContext.cs` (jadvallar + UNIQUE indekslar), `Services/OnlineGameService.cs` (yangi), `Controllers/OnlineController.cs` (yangi), `Hubs/GameHub.cs` (onlayn o'yinchilar, PlayersChanged), `Controllers/AuthController.cs` (username bilan kirish), `Program.cs` (Sqlite/Postgres tanlovi, /health), `appsettings.json` (0.0.0.0:5080, Sqlite), `.csproj` (Sqlite paketi, RollForward)
**Client (Windows+Android umumiy):** `Online/OnlineClient.cs`, `Online/OnlineClientSmoke.cs`, `UI/OnlinePanel.cs` (yangi); `Core/InputManager.cs` (sensor: tap, sudrash, pinch, matn kiritish), `Core/LaunchOptions.cs`, `Game1.cs`, `Program.cs`, `Core/Graphics/PixelFont.cs` (`_ * # @ = >`), `.csproj` (SignalR client, Shared reference)
**Android:** `AndijonSaltanatlar.Android/` (csproj, Activity1.cs, AndroidManifest.xml) — o'yin kodi link orqali
**Testlar:** `AndijonSaltanatlar.Tests/OnlineTradeTests.cs`, `tools/e2e_online_test.ps1`, `tools/e2e_restart_check.ps1`
**Boshqa sessiya fayllaridagi build tuzatishlari:** `Infrastructure/InfrastructureValidator.cs` (using), `Infrastructure/BridgeManager.cs` (enum taqqoslash)

## Test natijalari

| Sinov | Natija |
|-------|--------|
| Server E2E (haqiqiy HTTP, 2 akkaunt, LAN IP) — `tools/e2e_online_test.ps1` | **19/19 PASS** |
| Server restartdan keyin saqlanish — `tools/e2e_restart_check.ps1` | **5/5 PASS** |
| Windows client zanjiri (OnlineClient, 2 client, SignalR) — `--online-smoke` | **15/15 PASS** |
| xUnit — butun to'plam (shu jumladan 7 ta yangi OnlineTradeTests) | **63/63 PASS** |
| Windows client vizual (login, GURUCH, SOTIB OLISH ekranlari) | skrinshotlar bilan tekshirildi |
| Android APK build (`net10.0-android`, Release) | **Muvaffaqiyatli** — `AndijonSaltanatlar.Android\bin\Release\net10.0-android\publish\uz.andijon.saltanatlar-Signed.apk` (~16 MB, debug kalit bilan imzolangan) |
| Android real qurilmada | **SINALMAGAN** — `docs/ANDROID_BUILD.md`, 4-bo'lim |

## Ma'lum cheklovlar / keyingi qadamlar

- PostgreSQL bilan jonli sinov o'tkazilmadi (bu kompyuterda PostgreSQL/Docker yo'q) — SQLite bilan sinaldi. Kod ikkala provayderda bir xil EF Core so'rovlarini ishlatadi.
- `EnsureCreated` eski bazaga yangi jadval qo'shmaydi → EF migratsiyalari kerak (keyingi bosqich).
- Telefon ulanishi uchun Windows Firewall'da 5080 port ochilishi kerak (administrator huquqi; qo'llanmada buyruq bor).
- HTTPS hali sozlanmagan (lokal Wi-Fi uchun http). Karvon orqali yetkazish — keyingi bosqich.
- PROMPT 17 (NPC suhbatlari) UI qismi hali Game1 ga ulanmagan — alohida davom ettiriladi.
