# PROMPT 19B — hudud qo'shish, aholi boshqaruvi, ko'p shahar, hokimlar, ekinlar, karvonlar, UI dizayn tizimi

Sana: 2026-11-15 · Asos: PROMPT 19A (ishlaydi, buzilmagan — sinovlar qayta o'tkazildi)

## Bajarilgan ishlar (server-authoritative)

| Tizim | Holat | Qayerda |
|---|---|---|
| Dunyo xaritasi: 2 AI saltanat (Andijon xonligi, Farg'ona begligi), 2 mustaqil hudud, o'yinchi hududlari | ISHLAYDI | `Server/Services/WorldSeeder.cs` |
| KINGDOM EXPANSION (shahar ichidagi yer ochishdan alohida): elchi → munosabat → diplomatik birlashish | ISHLAYDI | `KingdomService.AnnexAsync`, `/api/kingdom/annex` |
| Hudud transferi atomik: `Region.Owner` shartli UPDATE + shaharlar + binolar + rejalar bitta tranzaksiyada | ISHLAYDI, parallel test bilan | `KingdomService` |
| Aholi boshqaruvi: aholi SHAHARDA, shahar bilan birga o'tadi; saltanat aholisi = shaharlar yig'indisi (ikki marta sanalmaydi); ishonch yangi boshqaruvda pasayib, asta tiklanadi | ISHLAYDI | `CityEntity`, `KingdomService.GrowCity` |
| Ko'p shahar: har shaharda alohida aholi, oila, uy sig'imi, ish o'rni, mamnunlik, ishonch, binolar, daromad/xarajat; barcha shaharlar yengil statistik simulyatsiyada | ISHLAYDI | `WorldSimulationService` (10 s) |
| AI hokimlar: Qo'lda / Maslahat / Tasdiqlangan avtomatika; tahlil → reja → o'yinchi tasdig'i → qurilish → statistika | ISHLAYDI | `/api/kingdom/governor/*` |
| CropSystem: Guruch, Bug'doy, Paxta, Mevalar, Sabzavotlar — har biri o'z o'sish vaqti, urug' narxi, hosili | ISHLAYDI | `Shared/Farming/CropCatalog.cs` |
| Savdo: har qanday mahsulot; karvon bilan yetkazish (sotuvchi ombori → yuk → yo'l → xaridor omboriga tushirish → savdo yakuni). Offline bo'lsa ham server yetkazadi | ISHLAYDI | `OnlineGameService.BuyAsync/DeliverDueAsync` |
| AI saltanatlar bozorda sotadi va (2 daqiqadan eski arzon takliflarni) sotib oladi — o'sha server qoidalari bilan | KOD BOR, avtomatik sinovda alohida tekshirilmagan | `WorldSimulationService.AiMarketAsync` |
| Donishmand Vazir: yangi hudud/shahar, hokim rejasi, karvonlar, birlashish imkoniyati — haqiqiy holatdan, "SHAHRIXONNI BOSHQARISH" kabi tugmalar bilan | ISHLAYDI | `KingdomService.AdvisorNotes`, panel "Vazir" |
| SignalR: `TerritoryTransferred`, `MarketChanged`, `StateChanged` (karvon yetdi / sotildi), `PlayersChanged` | ISHLAYDI | `GameHub`, controllerlar |

## UI / UX

- **ThemeManager** (`UI/Design/ThemeManager.cs`): spec'dagi Light/Dark tokenlar; standart — tungi; bosh menyuda almashtirgich.
- **Typography** (`UI/Design/Typography.cs`): Noto Sans (SIL OFL, `Content/Fonts`, litsenziya — `OFL.txt`) FontStashSharp orqali. O'lchamlar 36/28/20/16/16/14. Matnda `o'`/`g'` avtomatik `oʻ`/`gʻ` ga, tutuq `ʼ` ga aylanadi. `Fit` (kesilmasdan "…" bilan qisqartirish) va `Wrap`. Shrift topilmasa — piksel shriftga qaytadi.
- **UiKit** (`UI/Design/UiKit.cs`): Button (Primary/Secondary/Ghost/Danger/Accent), IconButton, Panel, Card, Modal, TabBar, NavList (sidebar ↔ bottom navigation), ProgressBar, ResourceCounter, Chip, Badge, TextInput, Scroll, Toast, Tooltip. Holatlar: normal/hover/pressed/disabled/focus.
- Qayta qilingan ekranlar: **Bosh menyu**, **Login + dunyo tanlash**, **Dalalar**, **Saltanatlar bozori** (Sotib olish / Sotish / Mening takliflarim / Savdo tarixi + tasdiqlash modali), **Karvonlar** (harakatlanuvchi karvon), **Saltanat boshqaruvi** (Umumiy, Shaharlar, Hududlar xaritasi, Aholi, Iqtisodiyot, Diplomatiya, Hokimlar), **Vazir**.
- Responsive: PC — markaziy oyna, chap navigatsiya, hover/tooltip; telefon/tor ekran — to'liq ekran, pastki navigatsiya, katta tugmalar, UiScale 1.1–1.8. 854×480 da skrinshot bilan tekshirildi.
- **Hali yangi dizaynga o'tkazilmagan:** o'yin HUD (yuqori/pastki panel), qurilish menyusi, eski DAVLAT/AHOLI/QO'SHIN panellari, Vazir AI-gubernator paneli, hikoya overlay — ular piksel shriftda qolgan. Localization katalogi: yangi ekranlar matnlari kodda (o'zbekcha); umumiy katalogga ko'chirilmagan.

## Test natijalari

| Sinov | Natija |
|---|---|
| xUnit (shu jumladan 6 ta yangi KingdomTerritoryTests) | **69/69 PASS** |
| Hudud + hokim zanjiri (2 akkaunt, haqiqiy HTTP) — `tools/e2e_kingdom_test.ps1` | **14/14 PASS** |
| 19A savdo zanjiri karvon bilan — `tools/e2e_online_test.ps1` | **20/20 PASS** |
| Windows client zanjiri (OnlineClient + SignalR, karvon yetkazish bilan) — `--online-smoke` | **16/16 PASS** |
| UI skrinshotlari: menyu, dalalar, xarita, shaharlar, hokimlar (854×480) | tekshirildi |
| Android APK (Release, net10.0-android) | **Yig'ildi** — 17,8 MB, shriftlar `assets/Content/Fonts` da |
| Android real qurilmada | **SINALMAGAN** |

Sinovlar paytida topilib tuzatilgan xato: yangi dunyo so'ralgan `WorldId` bilan emas, avtomatik raqam bilan yaratilardi — bir xil WorldId so'ragan o'yinchilar turli dunyolarga tushib qolardi (19A dan qolgan xato).

## Ma'lum cheklovlar / bajarilmagan

- **Shaharning izometrik xaritasiga kirish** (har shahar uchun alohida qurilish sahnasi) qilinmadi: "Shaharni boshqarish" hokim/statistika paneliga olib boradi. Izometrik sahna hamon bitta lokal Asaka.
- Hudud qo'shish faqat AI va mustaqil hududlar uchun; o'yinchilar o'rtasidagi kelishuvlar va "abstrakt jang natijasida nazorat o'zgarishi" — keyingi bosqich.
- Ekin maydonlari saltanat darajasida (shaharga biriktirilmagan).
- PostgreSQL jonli sinovi yo'q (SQLite). `EnsureCreated` eski bazaga yangi ustun/jadval qo'shmaydi — 19A dev bazasi `andijon_server.19a-backup.db` ga ko'chirildi; prod uchun EF migratsiyalari kerak.
- Dunyo seed qilish bir vaqtda kelgan ikki "birinchi" so'rovda takrorlanishi mumkin (nazariy, sinovda uchramadi).
