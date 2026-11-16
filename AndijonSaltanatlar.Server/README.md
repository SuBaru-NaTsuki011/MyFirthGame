# Andijon: Saltanatlar — Server (Backend)

Professional online infratuzilma: **ASP.NET Core Web API + SignalR + PostgreSQL + EF Core + ASP.NET Identity (parol hashlash) + JWT**.

## Loyiha tuzilmasi

| Loyiha | Vazifasi |
|--------|----------|
| `AndijonSaltanatlar` | O'yin klienti (MonoGame, mavjud — o'zgartirilmagan) |
| `AndijonSaltanatlar.Shared` | Umumiy kutubxona: DTO, buyruqlar (`GameCommand`), enumlar |
| `AndijonSaltanatlar.Server` | Backend: auth, bulutli saqlash, doimiy dunyo simulyatsiyasi |

## Arxitektura

- **Auth** (`/api/auth`): `register` / `login`. Parollar `PasswordHasher<AppUser>` (ASP.NET Identity) bilan hashlanadi — hech qachon ochiq saqlanmaydi. Javobda **JWT** (7 kun) qaytadi.
- **Server-authoritative buyruqlar** (`/api/game/command`): narx va qoidalar **serverda** tekshiriladi, klient yuborgan narxga ishonilmaydi. Har bir buyruqda `CommandId` bo'lib, **idempotentlik** ta'minlanadi (bir buyruq ikki marta bajarilmaydi).
- **Egalik tekshiruvi**: buyruq JWT'dagi `NameIdentifier` orqali saltanat egasiga tegishliligini tekshiradi (`403 Forbid` aks holda).
- **Bulutli saqlash** (`/api/game/state`): saltanat, resurslar va binolar DB'da saqlanadi — logout/login'dan keyin ham qoladi.
- **Doimiy dunyo** (`WorldSimulationService : BackgroundService`): o'yinchi offline bo'lsa ham dunyo kuni, qurilish progressi va AI saltanatlar yangilanadi.
- **Real-vaqt** (SignalR `/gamehub`): o'zgarishlar `StateChanged` orqali dunyo guruhiga yuboriladi (butun xarita emas).

## Ma'lumotlar bazasi

PostgreSQL, EF Core (Npgsql). Jadvallar: `Users`, `Worlds`, `Kingdoms`, `KingdomResources`, `Cities`, `Regions`, `Buildings`, `LandParcels`, `ResearchProgress`, `DiplomaticRelations`, `TradeOffers`, `GameCommands` (idempotentlik), `PlayerNotifications`.

Hozircha dev qulayligi uchun `Database.EnsureCreated()` ishlatiladi. Production uchun migratsiyalar:

```bash
dotnet ef migrations add Initial --project AndijonSaltanatlar.Server
dotnet ef database update --project AndijonSaltanatlar.Server
```

## Ishga tushirish

1. PostgreSQL'ni ko'taring (Docker kerak):

```bash
docker compose up -d
```

2. JWT kalitini o'rnating (production uchun majburiy — kodda hardcode yo'q):

```bash
set ANDIJON_JWT_KEY=kamida-32-belgili-maxfiy-kalit-shu-yerda
```

3. Serverni ishga tushiring:

```bash
dotnet run --project AndijonSaltanatlar.Server
```

Server `http://localhost:5080` da ishlaydi.

## Xavfsizlik

- Parollar faqat hash ko'rinishida saqlanadi (ASP.NET Identity).
- JWT kaliti konfiguratsiya/environment'dan olinadi — manba kodida sir yo'q.
- Barcha o'yin endpointlari `[Authorize]` bilan himoyalangan.
- Narxlar va qoidalar serverda hal qilinadi (cheat'ga qarshi).
- `appsettings.json` da real sir yo'q (bo'sh `Jwt:Key`, lokal dev parol).

## ⚠️ Halol status (sinalmagan narsalar)

- **Server build: ✅ 0 xato, 0 ogohlantirish** (Server + Shared + Client — barchasi).
- **Server JONLI PostgreSQL'ga qarshi ISHGA TUSHIRILMAGAN**: bu kompyuterda Docker va `psql` o'rnatilmagan, shuning uchun register→login→saqlash→qayta login oqimi real DB bilan **sinab ko'rilmadi**. Server DB'siz ham ishga tushadi (ulanish xatosi `try/catch` bilan ushlanadi), lekin auth va saqlash funksiyalari ishlashi uchun PostgreSQL shart.
- **Klient hali serverga ulanmagan**: bu bosqichda backend qurildi; MonoGame klientiga tarmoq qatlami (login ekrani, HTTP klient, SignalR ulanish) keyingi bosqichda qo'shiladi.
- Migratsiyalar yaratilmagan (hozir `EnsureCreated`).
