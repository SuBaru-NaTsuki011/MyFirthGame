# O'zbekiston xaritasi, yurt tanlash va bayroqlar

## Xarita

Ma'lumotlar bitta manbada: `AndijonSaltanatlar.Shared/Geo/UzMap.cs`. Uni server ham, o'yin ham ishlatadi.

- **Viloyatlar:** 13 ta. Qoraqalpog'iston, Xorazm, Navoiy, Buxoro, Samarqand, Qashqadaryo, Surxondaryo, Jizzax,
  Sirdaryo, Toshkent, Namangan, Farg'ona, Andijon.
- **Hududlar:** 93 ta tuman yoki shahar, har birida bitta shahar. Haqiqiy uzunlik va kenglikda saqlanadi. O'yin ularni
  0..100 xarita birligiga o'giradi (`RegionEntity.Latitude` = X, `Longitude` = Y).
- **AI saltanatlar:** 5 ta tarixiy saltanat.
  - Andijon xonligi
  - Farg'ona begligi
  - Xiva xonligi
  - Buxoro amirligi (Buxoro, Samarqand, Qarshi)
  - Qo'qon xonligi (Qo'qon, Namangan, Toshkent)

  Qolgan hududlar mustaqil.
- **Bezaklar:** chegara, Orol dengizi (eski xaritalardagidek to'la holatda), Amudaryo, Sirdaryo, Zarafshon va boshqa
  daryolar, tog' tizmalari, Qizilqum va Ustyurt.
- **Masofa:** km da hisoblanadi (`UzMap.DistanceKm`).
  - Qo'shin yurishi: qo'shni tumanga taxminan 20 s, Andijondan Toshkentgacha taxminan 1 daqiqa, Andijondan Nukusgacha
    taxminan 2–3 daqiqa (eng ko'pi 4 daqiqa).
  - Karvon vaqti ham shu tarzda hisoblanadi.

O'yindagi xarita `UI/UzMapCanvas.cs` da chiziladi, uslubi pergament va siyoh:

- **Hudud maydoni:** har hudud eng yaqin shahar atrofidagi maydonni egallaydi, lekin 85 km dan uzoqqa cho'zilmaydi.
  Maydon egasi rangiga bo'yaladi.
- **Chegaralar:** saltanatlar orasida qalin, viloyatlar orasida o'rtacha, tumanlar orasida ingichka chiziq.
- **Boshqaruv:** kattalashtirish g'ildirak, ikki barmoq yoki `+ −` tugmalari bilan; `O` tugmasi butun mamlakatni
  ko'rsatadi. Xarita sichqoncha yoki barmoq bilan suriladi.
- **Qasrlar:** egasining bayrog'i bilan chiziladi. Mustaqil hududlar kichik qishloq ko'rinishida.

## Eski dunyoni ko'chirish

Har bir dunyoda `World.MapVersion` bor. Dunyoga birinchi kirishda u `UzMap.Version` dan kichik bo'lsa:

1. Yetishmayotgan AI saltanatlar qo'shiladi. Saltanat nomi bo'yicha qidiriladi, mavjudlari qayta yaratilmaydi.
2. Mavjud hududlarning koordinatasi va viloyati yangilanadi. Egalik, shaharlar, aholi va binolar o'zgarmaydi.
   Katalogda yo'q hududlar (masalan "Yangi yurt 12") eski Farg'ona koordinatasidan o'giriladi.
3. Yetishmayotgan hududlar shahri bilan qo'shiladi.

Bu jarayon ikki marta ishlasa ham natija o'zgarmaydi. U haqiqiy bazaning nusxasida sinaldi. Zaxira nusxa:
`release/server-google/andijon_server.before-uzmap.db`.

## Boshlang'ich yurtni tanlash

- **Yangi mijoz:** `JoinWorldRequest.ChooseStart = true` yuboradi. Saltanat yurtsiz yaratiladi va
  `OnlineStateDto.NeedsHome = true` qaytadi. O'yin "Yurtingizni tanlang" ekranini ochadi:
  1. xaritadan mustaqil hudud tanlanadi;
  2. bayroq va saltanat nomi kiritiladi;
  3. `POST api/online/start` yuboriladi.
- **API:**
  - `GET api/online/start-regions` — barcha hududlar, bo'sh yoki band holati bilan.
  - `POST api/online/start` — `{ RegionId, KingdomName, Flag }`. Faqat mustaqil hudud va faqat bir marta. Band hudud
    so'ralsa 409 qaytadi.
- **Eski mijoz (eski APK):** avtomatik joy oladi. Avval Asaka, Xo'jaobod va boshqalar, keyin Andijon viloyati, keyin
  istalgan mustaqil hudud.

## Bayroqlar

`AndijonSaltanatlar.Shared/Heraldry/FlagSpec.cs` da 11 ta naqsh, 14 ta rang va 10 ta ramz bor.

- **Matn ko'rinishi:** `naqsh-rang1-rang2-rang3-ramz-ramzRangi`, masalan `1-2-0-0-2-6`.
- **Tekshiruv:** server faqat yaroqli qiymatni qabul qiladi.
- **Qayerda ko'rinadi:** xaritadagi qasrlar, yurayotgan qo'shinlar, "Hududlar" xaritasi va jang maydoni.
- **O'zgartirish:** "Saltanat → Bayroq" bo'limida yoki `POST api/online/flag`.

## Keyingi bosqich: Yevropa

Xarita ma'lumoti bitta katalogda, versiyasi bilan. Yevropa alohida xarita varag'i sifatida qo'shiladi: o'z
proyeksiyasi, hududlari va saltanatlari bilan. Dunyo `MapVersion` orqali xuddi shu tarzda ko'chiriladi.
