# PROMPT 20 — ittifoqlar, dunyo siyosati, umumiy bozor, qo'shma loyihalar va kengaytirilgan saltanat boshqaruvi

Sana: 2026-11-15 · Asos: PROMPT 19A/19B (buzilmagan — 74/74 test o'tdi)

## Bajarilgan ishlar (server-authoritative)

| Tizim | Holat | Qayerda |
|---|---|---|
| DiplomacyService (ittifoq, taklif, a'zolik, chiqish, xazina hissalari, qo'shma loyihalar, kelishuvlar, hudud transferi, dunyo yangiliklari, AI diplomatiya) | ISHLAYDI | `Server/Services/DiplomacyService.cs` |
| DiplomacyController REST API (/api/diplomacy/*) | ISHLAYDI | `Server/Controllers/DiplomacyController.cs` |
| DB modellari (Alliance*, DiplomaticAgreement, WorldNews) + SchemaUpgrader | ISHLAYDI | `Server/Data/DiplomacyEntities.cs`, `GameDbContext.cs` |
| Ittifoq bozori (AllianceId bilan taklif) | ISHLAYDI | `OnlineGameService.CreateOfferAsync`/`OffersAsync` |
| AI dunyo diplomatiyasi (do'stlik kelishuvlari) | ISHLAYDI | `WorldSimulationService` + `DiplomacyService.AiDiplomacyAsync` |
| SignalR eventlar (AllianceInvitationReceived, AllianceMemberJoined, DiplomaticAgreementCreated, WorldNewsPublished, AllianceProjectUpdated) | ISHLAYDI | `GameHub` orqali controller/service |
| Client OnlineClient: yangi endpointlar, hub handlerlar, holat yuklash | ISHLAYDI | `Online/OnlineClient.cs` |
| UI: Diplomatiya markazi (Munosabatlar / Ittifoqlar / Kelishuvlar / Takliflar / Yangiliklar) | ISHLAYDI | `UI/OnlinePanel.cs` DrawDiplomacy |
| Shared DTOs (Alliance*, DiplomaticAgreement, WorldNews, so'rovlar) | ISHLAYDI | `Shared/DTOs/AllianceDtos.cs` |
| Testlar: 5 ta yangi (ittifoq zanjiri, hissa idempotentligi, loyiha yakuni, hudud kelishuvi, yangilik) | 74/74 PASS | `Tests/DiplomacyTests.cs` |

## O'zgartirilgan fayllar

- `Shared/DTOs/AllianceDtos.cs` (yangi)
- `Shared/DTOs/OnlineDtos.cs` (CreateOfferRequest.AllianceId)
- `Server/Services/DiplomacyService.cs` (yangi)
- `Server/Controllers/DiplomacyController.cs` (yangi)
- `Server/Services/OnlineGameService.cs` (AllianceId qo'llab-quvvatlash)
- `Server/Simulation/WorldSimulationService.cs` (AI diplomatiya)
- `Server/Program.cs` (DI ro'yxati)
- `Online/OnlineClient.cs` (endpointlar + hub handlerlar)
- `UI/OnlinePanel.cs` (Diplomatiya markazi)
- `Tests/DiplomacyTests.cs` (yangi)

## Build holati

- `dotnet build` (client): muvaffaqiyatli
- `dotnet build` (server): muvaffaqiyatli
- `dotnet test`: 74/74 PASS
- Android APK: bu bosqichda qayta yig'ilmadi (19B dan qolgan APK ishlaydi)

## Bajarilmagan / keyingi qadamlar

- Android real qurilmada sinov (oldindan qolgan cheklov).
- PostgreSQL jonli sinovi (SQLite bilan sinaldi).
- UI'da ittifoq yaratishda nom kiritish (hozir standart nom bilan yaratiladi; TextInput orqali to'liq form keyingi bosqich).
- Ittifoq bozori uchun alohida UI tab (endpoint tayyor, UI'da integratsiya keyingi bosqich).
- Ko'proq a'zoli ittifoqlar va rollar (arxitektura tayyor).
- AI saltanatlar o'rtasida hudud kelishuvlari (hozir faqat do'stlik).

## Haqiqiy holat

Server-authoritative, idempotent, barcha operatsiyalar tranzaksiyada. Resurslar ikki marta hisoblanmaydi (CommandId UNIQUE + shartli UPDATE). Hudud transferi atomik. Aholi statistikasi shaharlardan hisoblanadi (ikki marta sanalmaydi).
