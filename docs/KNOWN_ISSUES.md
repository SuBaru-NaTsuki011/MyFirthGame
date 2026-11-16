# KNOWN ISSUES (PROMPT 14)

Halol ro'yxat — bu funksiyalar hali to'liq ishlamaydi yoki tekshirilmagan:

1. **Savdo / diplomatiya / shahar-darajasi quest hodisalari** (P2)
   Engine bu objective turlarini qo'llab-quvvatlaydi, lekin `TradeCompleted`, `TradeAgreementCreated`, `DiplomaticRelationChanged`, `CityLevelChanged` hodisalari hali o'yin kodidan emit qilinmagan. Natijada 5-7 boblardagi savdo/diplomatiya vazifalari jonli o'yinда hozircha yakunlanmaydi. (Ulangan hodisalar: BuildingConstructed, BuildingUpgraded, LandUnlocked, PopulationChanged, TechnologyUnlocked.)

2. **Online multiplayer / server** (P2)
   ASP.NET Core server (`AndijonSaltanatlar.Server`) build bo'ladi, lekin jonli PostgreSQL bilan ishga tushirilmagan — bu muhitда Docker va `psql` o'rnatilmagan. Birinchi release **offline single player**.

3. **Interaktiv FPS** (P3)
   Headless/screenshot rejimida ishonchli o'lchab bo'lmadi. 40 kunlik simulyatsiya hang'siz. Maqsadli apparatda (Intel UHD 730) interaktiv profiling qilinmagan.

4. **Settings ekrani** (P3)
   Resolution/Fullscreen/UI Scale/Audio/Language to'liq sozlash paneli hali yo'q; o'yin standart 1280×720 oynada ishlaydi (oyna o'lchamini o'zgartirish mumkin). Grafika sifati G tugmasi bilan almashtiriladi.

5. **Audio** (P3)
   Ovoz tizimi hali qo'shilmagan.

6. **Kunlik/haftalik vazifalar** (P3)
   Modellari bor, davr-rejalashtiruvchi (scheduler) ulanmagan.

Bu ro'yxat `release/windows/README.txt` dagi "Ma'lum cheklovlar" bo'limi bilan mos.
