using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Military;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.War;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// JANG MAYDONI — mashq jangi. O'yinchining haqiqiy qo'shini (tarkibi va darajasi) mashq raqibiga qarshi
    /// onlayn jangdagi bilan bir xil sahnada (BattleScene) va bir xil qoidalar bilan (WarRules.ResolveRound) jang qiladi.
    /// Mashq bo'lgani uchun jangchilar o'lmaydi — g'alaba yoki mag'lubiyatdan keyin tajriba oladi.
    /// Onlayn urush va PvP'ga tegmaydi.
    /// </summary>
    public sealed class ArenaController
    {
        private const int MeId = 1, FoeId = 2;
        private readonly AssetManager _assets;
        private readonly GameStateManager _state;
        private BattleScene _scene;
        private float _lastTime = -1f;
        private int _seed;
        private bool _rewarded;

        public BattleDto Battle { get; private set; }
        public bool Active => Battle != null;

        public ArenaController(AssetManager assets, GameStateManager state)
        {
            _assets = assets;
            _state = state;
        }

        public static UnitLine LineOf(UnitType t)
        {
            switch (UnitCatalog.Get(t).Role)
            {
                case UnitRole.Otliq: return UnitLine.Otliq;
                case UnitRole.Masofaviy: return UnitLine.Masofaviy;
                case UnitRole.Yordamchi: return UnitLine.Lager;
                default: return UnitLine.Piyoda;
            }
        }

        /// <summary>Qo'shin tarkibidan sahna guruhlari (tur chizig'i × daraja).</summary>
        public static List<UnitGroupDto> GroupsOf(ArmyManager mil)
        {
            return mil.Units
                .GroupBy(u => (LineOf(u.UnitType), Math.Clamp(u.Level, 1, 3)))
                .Select(g => new UnitGroupDto { Line = g.Key.Item1, Level = g.Key.Item2, Count = g.Count() })
                .OrderBy(g => g.Line).ThenByDescending(g => g.Level).ToList();
        }

        /// <summary>Mashq jangini boshlaydi. Jangchi bo'lmasa — xabar beradi va false qaytaradi.</summary>
        public bool Start()
        {
            ArmyManager mil = _state.Military;
            if (mil.TotalUnits == 0) { _state.ShowMessage("Jang maydoni uchun avval jangchi sotib oling", true); return false; }
            _seed = System.Environment.TickCount & 0x7fffffff;
            int power = Math.Max(20, mil.TotalStrength);
            var mine = GroupsOf(mil);
            // Raqib: kuchi biroz past yoki teng (mashq), tarkibi aralash
            var rng = new Random(_seed);
            int foePower = (int)(power * (0.8 + rng.NextDouble() * 0.25));
            int foeCount = Math.Max(4, mine.Sum(g => g.Count));
            var foe = new List<UnitGroupDto>
            {
                new UnitGroupDto { Line = UnitLine.Piyoda, Level = 2, Count = Math.Max(2, foeCount * 5 / 10) },
                new UnitGroupDto { Line = UnitLine.Piyoda, Level = 1, Count = Math.Max(1, foeCount * 2 / 10) },
                new UnitGroupDto { Line = UnitLine.Masofaviy, Level = 1, Count = Math.Max(1, foeCount * 2 / 10) },
                new UnitGroupDto { Line = UnitLine.Otliq, Level = 1, Count = Math.Max(1, foeCount / 10) },
            };
            Battle = new BattleDto
            {
                Id = 7000 + (_seed % 1000), Status = BattleStatus.InProgress, Round = 1, IsPvp = false,
                RegionName = "Mashq maydoni",
                AttackerKingdomId = MeId, DefenderKingdomId = FoeId,
                AttackerName = GameConfig.CityName + " qo'shini", DefenderName = "Mashq raqibi",
                AttackerCommander = "Siz", DefenderCommander = "Ustoz",
                AttackerColor = new[] { 70, 130, 220 }, DefenderColor = new[] { 150, 70, 50 },
                AttackerStartPower = power, AttackerPower = power, DefenderStartPower = foePower, DefenderPower = foePower,
                AttackerMorale = 80, DefenderMorale = 75,
                IAmAttacker = true, IAmParticipant = true,
                AttackerGroups = mine, DefenderGroups = foe,
            };
            _rewarded = false;
            _scene ??= new BattleScene(_assets);
            _scene.Reset();
            _lastTime = -1f;
            return true;
        }

        public void Close() { Battle = null; }

        /// <summary>Bir raund: o'yinchi qarori + raqib (AI) qarori → WarRules.ResolveRound.</summary>
        public void Resolve(BattleDecision mine)
        {
            BattleDto b = Battle;
            if (b == null || b.Status == BattleStatus.Completed) return;
            int round = b.Round;
            BattleDecision foe = WarRules.AiDecision("balanced", _seed, round, b.DefenderPower, b.DefenderStartPower, b.DefenderMorale, b.AttackerPower);
            RoundOutcome o = WarRules.ResolveRound(_seed, round,
                b.AttackerPower, b.AttackerStartPower, b.AttackerMorale, mine, 0,
                b.DefenderPower, b.DefenderStartPower, b.DefenderMorale, foe, 0, 0);
            Shrink(b.AttackerGroups, o.AttackerLoss, b.AttackerPower);
            Shrink(b.DefenderGroups, o.DefenderLoss, b.DefenderPower);
            b.AttackerPower = o.AttackerPowerAfter; b.DefenderPower = o.DefenderPowerAfter;
            b.AttackerMorale = o.AttackerMoraleAfter; b.DefenderMorale = o.DefenderMoraleAfter;
            b.Rounds.Add(new BattleRoundDto { Round = round, AttackerDecision = mine, DefenderDecision = foe, AttackerLoss = o.AttackerLoss, DefenderLoss = o.DefenderLoss, SummaryUz = o.SummaryUz });
            if (o.Finished)
            {
                b.Status = BattleStatus.Completed;
                b.Peace = o.Winner == 0;
                b.WinnerKingdomId = o.Winner == 1 ? MeId : o.Winner == 2 ? FoeId : 0;
                Reward(b);
            }
            else b.Round = round + 1;
        }

        private static void Shrink(List<UnitGroupDto> groups, int powerLost, int powerBefore)
        {
            foreach (UnitGroupDto g in groups)
                g.Count = Math.Max(0, g.Count - WarRules.UnitsLost(g.Count, powerLost, powerBefore));
        }

        private void Reward(BattleDto b)
        {
            if (_rewarded) return;
            _rewarded = true;
            bool win = b.WinnerKingdomId == MeId;
            int xp = win ? 80 : b.Peace ? 50 : 35;
            int up = _state.Military.TrainAll(xp);
            b.ResultUz = (win ? "G'alaba! " : b.Peace ? "Durang. " : "Mag'lubiyat — lekin saboq oldik. ")
                         + "Mashq jangi: jangchilar o'lmadi, har biri +" + xp + " tajriba oldi" + (up > 0 ? " (" + up + " ta daraja oshdi)." : ".");
            b.DiplomacyUz = "Mashq jangi diplomatiyaga ta'sir qilmaydi.";
            _state.ShowMessage(b.ResultUz);
        }

        /// <summary>Sahnani chizadi va tugmalarni qayta ishlaydi (UiKit — immediate-mode).</summary>
        public void Draw(UiKit ui, SpriteBatch sb, InputManager input, int vw, int vh, float time)
        {
            BattleDto b = Battle;
            if (b == null) return;
            float dt = _lastTime < 0 ? 0f : Math.Clamp(time - _lastTime, 0f, 0.1f);
            _lastTime = time;
            ui.Begin(sb, input, vw, vh, time);
            ui.Fill(new Rectangle(0, 0, vw, vh), ui.T.Background);
            BattleScene.Bind(sb);
            BattleSceneAction act = _scene.Draw(ui, sb, b, vw, vh, dt, false);
            ui.End();
            if (act == BattleSceneAction.Decide) Resolve(_scene.SelectedDecision);
            else if (act != BattleSceneAction.None) Close();
        }
    }
}
