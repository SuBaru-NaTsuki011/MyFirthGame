using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Dalada ishlaydigan dehqonlar: har fermaga (eng ko'pi MaxWorkers) bitta dehqon chiqadi — dala chetini aylanib
    /// ekadi / sug'oradi / hosil o'radi va hosilni bozorga (bo'lmasa saroyga) olib boradi. Nima qilayotganini aytadi.
    /// Haqiqiy hosil hisobi GameStateManager'da (kunlik); bu — ko'rinish va jonlilik.
    /// </summary>
    public sealed class FarmWorkController
    {
        public const int MaxWorkers = 6;
        private readonly Dictionary<int, CitizenUnit> _workers = new Dictionary<int, CitizenUnit>();
        private readonly Dictionary<CitizenUnit, float> _talk = new Dictionary<CitizenUnit, float>();
        private UnitManager _owner;
        private float _timer;
        private readonly Random _rng = new Random();

        public int WorkerCount => _workers.Count;

        public void Update(float dt, MapManager map, BuildingManager buildings, UnitManager units, GameStateManager state)
        {
            if (!ReferenceEquals(units, _owner)) { _workers.Clear(); _talk.Clear(); _owner = units; _timer = 0f; }
            _timer -= dt;
            if (_timer <= 0f)
            {
                _timer = 2f;
                Sync(map, buildings, units);
            }
            Talk(dt, buildings, state);
        }

        private void Sync(MapManager map, BuildingManager buildings, UnitManager units)
        {
            Building hall = buildings.Buildings.FirstOrDefault(b => b.Type == BuildingType.MainHall);
            Vector2 center = hall != null ? new Vector2(hall.Position.X, hall.Position.Y) : Vector2.Zero;
            var farms = buildings.Buildings.OfType<Farm>().Where(f => f.BuildProgress >= 1f)
                .OrderBy(f => Vector2.DistanceSquared(new Vector2(f.Position.X, f.Position.Y), center))
                .Take(MaxWorkers).ToList();
            var keep = new HashSet<int>(farms.Select(f => f.Id));

            // Fermasi buzilgan/ko'chirilgan dehqonlar ketadi (ko'chirilsa yangi yo'l bilan qaytadi)
            foreach (int id in _workers.Keys.ToList())
            {
                Farm f = farms.FirstOrDefault(x => x.Id == id);
                if (f == null || !keep.Contains(id) || Vector2.Distance(_workers[id].PatrolPoints.Count > 0 ? _workers[id].PatrolPoints[0] : Vector2.Zero, FieldFront(f)) > 6f)
                {
                    units.Remove(_workers[id]);
                    _talk.Remove(_workers[id]);
                    _workers.Remove(id);
                }
            }

            foreach (Farm f in farms)
            {
                if (_workers.ContainsKey(f.Id)) continue;
                Building market = buildings.Buildings.Where(b => b.Type == BuildingType.Market)
                    .OrderBy(b => Vector2.DistanceSquared(new Vector2(b.Position.X, b.Position.Y), new Vector2(f.Position.X, f.Position.Y)))
                    .FirstOrDefault() ?? hall;
                var route = new List<Vector2>
                {
                    FieldFront(f),
                    new Vector2(f.Position.X + f.Size.X + 0.5f, f.Position.Y + f.Size.Y / 2f),   // dala sharqiy cheti
                    new Vector2(f.Position.X + f.Size.X / 2f, f.Position.Y - 0.5f),            // shimoliy cheti
                    FieldFront(f),
                };
                if (market != null) route.Add(new Vector2(market.FrontEntranceTile.X + 0.5f, market.FrontEntranceTile.Y + 0.5f));
                var worker = units.Add(new CitizenUnit(route, CitizenArt.Farmer, 0.75f));
                PatrolController.RouteUnit(map, worker);
                worker.PauseTime = 0f;
                _workers[f.Id] = worker;
                _talk[worker] = 2f + (float)_rng.NextDouble() * 6f;
            }
        }

        private static Vector2 FieldFront(Farm f) => new Vector2(f.FrontEntranceTile.X + 0.5f, f.FrontEntranceTile.Y + 0.5f);

        /// <summary>Dehqon nima qilayotganini aytadi: dala yonida — ekin holatiga qarab, bozorda — hosil topshirdim.</summary>
        private void Talk(float dt, BuildingManager buildings, GameStateManager state)
        {
            foreach (var kv in _workers)
            {
                CitizenUnit w = kv.Value;
                if (!_talk.TryGetValue(w, out float t)) t = 5f;
                t -= dt;
                if (t > 0f) { _talk[w] = t; continue; }
                _talk[w] = 7f + (float)_rng.NextDouble() * 7f;
                Farm f = buildings.Buildings.OfType<Farm>().FirstOrDefault(x => x.Id == kv.Key);
                if (f == null || !string.IsNullOrEmpty(w.SpeechText)) continue;
                bool atField = Vector2.Distance(w.Position, new Vector2(f.Position.X + f.Size.X / 2f, f.Position.Y + f.Size.Y / 2f)) < f.Size.X;
                string line;
                if (atField)
                {
                    switch (f.CropStage)
                    {
                        case FarmCropStage.Empty: line = state.Farming.Seeds > 0 ? "Urug' sepyapman" : "Urug' tugadi! Sotib olish kerak"; break;
                        case FarmCropStage.Growing: line = (f.GrowthDays % 2 == 0) ? "Ekinni sug'oryapman" : "Begona o'tlarni yulyapman"; break;
                        default: line = "Hosil pishdi — o'ryapman!"; break;
                    }
                }
                else line = f.StoredProduce > 0 || f.CropStage == FarmCropStage.Mature ? "Hosilni bozorga olib ketyapman" : "Bozordan qaytyapman";
                w.Say(line, 3f);
            }
        }
    }
}
