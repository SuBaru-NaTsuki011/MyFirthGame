# TEST RESULTS (PROMPT 14)

Sana: 2026-11-15 · Build config: Release · .NET 8

## 1. xUnit birlik testlari — 20/20 PASS
`dotnet test AndijonSaltanatlar.Tests -c Release` → **Passed: 20, Failed: 0, Skipped: 0**

Qamrov:
- Qurilish/quest: bino qurish, idempotentlik (bir event ikki marta sanalmaydi), talab bajarilmaguncha yakunlanmaslik, keyingi vazifa ochilishi, yer ochish.
- Iqtisod: mukofot bir marta beriladi.
- Validatsiya: noto'g'ri prerequisite, cycle aniqlanishi.
- Yutuq: bir marta ochilishi, chegaraviy (baxt≥80).
- Dialog: tanlov saqlanishi.
- Save: CampaignState JSON round-trip, 7 bob, kanonik katalog yaroqliligi, data-driven JSON round-trip.
- Tarixiy metadata: manbasiz da'vo rad etiladi.

## 2. In-game smoke test — 18/18 PASS
`AndijonSaltanatlar.exe --smoke <fayl>` — haqiqiy o'yin tizimlari ustida to'liq zanjir.

```
PASS | Startup: shahar yuklandi (166 bino)
PASS | Startup: aholi > 0 (42 kishi)
PASS | Qurilish: bo'sh katak topildi
PASS | Qurilish: uy joylashtirildi
PASS | Qurilish: bino soni +1
PASS | Iqtisod: tanga BIR marta ayirildi (-50)  [1000 -> 950]
PASS | Simulyatsiya: o'yin kuni o'tdi  [1 -> 42]
PASS | Qurilish: bosqichli qurilish yakunlandi
PASS | Iqtisod: resurslar manfiy emas
PASS | Yer ochish: uchastka ochildi (gating-path)
PASS | Yer ochish: ochilgan yerda qurilish mumkin
PASS | Maslahatchi: tavsiya beradi
PASS | Hikoya: faol vazifa mavjud
PASS | Save: fayl yozildi
PASS | Load: tanga tiklandi
PASS | Load: o'yin kuni tiklandi
PASS | Load: ochilgan yerlar tiklandi
PASS | Load: binolar tiklandi
NATIJA: 18 PASS / 0 FAIL
```

**Muhim:** aynan shu smoke-test published `release/windows/AndijonSaltanatlar.exe` ustida ham ishga tushirilib, **18/18 PASS** berdi (GATE 10 — toza start).

## 3. Vizual tasdiq (screenshot)
Published `.exe` to'liq grafika bilan ochiladi: izometrik Asaka shahri + Donishmand Vazir dialog paneli + HUD + topshiriqlar ekrani.

## O'lchanmagan
- Interaktiv FPS (headless muhitda ishonchli o'lchab bo'lmadi). 40 kунlik simulyatsiya tiklanishsiz, darhol bajarildi.
- Server jonli PostgreSQL integratsiya testi (baza yo'q).
