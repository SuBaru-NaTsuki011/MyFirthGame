using System.Collections.Generic;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Shaharni jonlantirish uchun fuqarolarni joylashtiradi: bozor, maydon va bosh ko'chalar
    /// bo'ylab yuradigan savdogar, hunarmand, dehqon va oddiy aholi. Nuqtalar ko'cha/maydon
    /// ustida — keyin PatrolController ularni A* yo'liga aylantiradi.
    /// </summary>
    public static class CitizenSpawner
    {
        public static void Spawn(MapManager map, UnitManager units)
        {
            void C(int kind, float speed, params (float x, float y)[] pts)
            {
                var route = new List<Vector2>();
                foreach (var (x, y) in pts) route.Add(new Vector2(x + GameConfig.CityOffset, y + GameConfig.CityOffset));
                units.Add(new CitizenUnit(route, kind, speed));
            }

            // Bozor atrofida savdogar va xaridorlar
            C(CitizenArt.Merchant, 0.7f, (39.5f, 34.5f), (45.5f, 34.5f), (45.5f, 40.5f), (39.5f, 40.5f));
            C(CitizenArt.Buyer, 0.8f, (40.5f, 36.5f), (43.5f, 39.5f), (46.5f, 37.5f));
            C(CitizenArt.Merchant, 0.75f, (38.5f, 37.5f), (47.5f, 37.5f));

            // Bosh ko'chalarda aholi
            C(CitizenArt.Citizen, 0.9f, (16.5f, 31.5f), (31.5f, 31.5f), (48.5f, 31.5f));
            C(CitizenArt.Citizen, 0.85f, (31.5f, 15.5f), (31.5f, 31.5f), (31.5f, 49.5f));

            // Markaziy maydonda sayr
            C(CitizenArt.Citizen, 0.8f, (27.5f, 27.5f), (36.5f, 27.5f), (36.5f, 36.5f), (27.5f, 36.5f));
            C(CitizenArt.Buyer, 0.75f, (31.5f, 24.5f), (28.5f, 31.5f), (31.5f, 36.5f));

            // Hunarmand (qo'shinxona/g'arb tomoni) va dehqon (janubiy dala)
            C(CitizenArt.Craftsman, 0.8f, (16.5f, 20.5f), (21.5f, 20.5f), (21.5f, 31.5f));
            C(CitizenArt.Farmer, 0.7f, (24.5f, 50.5f), (31.5f, 50.5f), (36.5f, 50.5f), (42.5f, 36.5f));
            C(CitizenArt.Farmer, 0.7f, (31.5f, 44.5f), (31.5f, 50.5f), (39.5f, 40.5f));
            C(CitizenArt.Shepherd, 0.72f, (12.5f, 47.5f), (18.5f, 52.5f), (24.5f, 50.5f));
            // Qal'adan tashqaridagi yaylovlarda yana ikki cho'pon (har birining o'z podasi — LivestockSystem)
            C(CitizenArt.Shepherd, 0.6f, (40.5f, 6.5f), (48.5f, 5.5f), (46.5f, 9.5f));
            C(CitizenArt.Shepherd, 0.6f, (3.5f, 38.5f), (5.5f, 44.5f), (2.5f, 47.5f));

            // PROMPT 17: mahalla ko'chalarida yuradigan aholi (suhbatlashish mumkin bo'lgan fuqarolar ko'proq bo'lsin)
            C(CitizenArt.Citizen, 0.75f, (22.5f, 21.5f), (26.5f, 21.5f));
            C(CitizenArt.Buyer, 0.8f, (21.5f, 34.5f), (21.5f, 41.5f));
            C(CitizenArt.Citizen, 0.75f, (42.5f, 43.5f), (42.5f, 47.5f));
            C(CitizenArt.Craftsman, 0.8f, (37.5f, 42.5f), (47.5f, 42.5f));
        }
    }
}
