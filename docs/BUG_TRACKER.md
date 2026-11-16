# BUG TRACKER (PROMPT 14)

Prioritet: P0 (kritik) · P1 (yuqori) · P2 (o'rta) · P3 (past)

## Tuzatilgan
| # | Pri | Muammo | Fayl | Holat |
|---|-----|--------|------|-------|
| 1 | P1 | Smoke harness `BuildSave()`da NullReferenceException — kamera yaratilishidan oldin chaqirilgan edi | `Game1.cs` (LoadContent tartibi) | FIXED — smoke chaqiruvi kamera yaratilgandan keyinga ko'chirildi |
| 2 | P2 | `ConstructionSystem.TryPlace` yopiq (private, qaytarmas) — avtomatik test qilib bo'lmasди | `Buildings/ConstructionSystem.cs` | FIXED — `PlaceAt(Point,bool)` ommaviy qo'shildi, `TryPlace` endi `bool` qaytaradi; click yo'li o'zgarmadi |

## Tekshirilgan — xato yo'q (smoke 18/18 PASS)
- O'yin crashsiz ochiladi (startup), xarita render qilinadi.
- Uy qurilganda tanga **bir marta** ayiriladi (−50), bino soni +1.
- Bosqichli qurilish 40 kun ichida yakunlanadi.
- Resurslar manfiy qiymatga tushmaydi.
- Yangi yer ochiladi va ochilgan yerda qurilish mumkin.
- Save → Load'da tanga, kun, ochilgan yerlar va binolar to'g'ri tiklanadi (eski binolar yo'qolmaydi).
- Maslahatchi GameState asosida tavsiya beradi.

## Ochiq (keyingi ish — KNOWN_ISSUES.md ga qarang)
| # | Pri | Muammo |
|---|-----|--------|
| 3 | P2 | Savdo/diplomatiya/shahar-darajasi quest hodisalari hali emit qilinmagan (5-7 bob vazifalari jonli o'yinда yakunlanmaydi) |
| 4 | P3 | FPS interaktiv tarzda o'lchanmagan (headless muhit) |
| 5 | P2 | Server jonli PostgreSQL bilan ishga tushirilmagan (Docker/psql yo'q) |
