using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.AdvancedEconomy
{
    /// <summary>
    /// Iqtisodiy operatsiya turlari. Davlat darajasidagi savdo (StateBuy / StateSell) va fuqarolarning
    /// chakana xaridi (Retail) ALOHIDA turlar — ular hech qachon aralashtirilmaydi.
    /// </summary>
    public enum TransactionType
    {
        Production,    // ishlab chiqarish -> ombor
        Import,        // karvon keltirgan mol -> ombor
        Wholesale,     // ombor -> bozor (ulgurji), pul ishlab chiqaruvchiga
        Retail,        // bozor -> oila (chakana xarid)
        RetailTax,     // chakana savdodan savdo solig'i -> xazina
        Ration,        // puli yetmagan oilaga ombordan bepul ulush
        Subsistence,   // bozor yetishmaganda ombordan to'g'ridan-to'g'ri ta'minot
        Wage,          // ishlab chiqaruvchi / savdogar / karvonchi daromadi
        StateWage,     // davlat maoshi (soqchi, amaldor, muallim)
        Welfare,       // muhtoj oilalarga davlat yordami
        PopulationTax, // aholi solig'i: oila -> xazina
        StateBuy,      // davlat bozordan sotib oldi (xazina -> bozor, mol -> ombor)
        StateSell,     // davlat ombordan bozorga sotdi (bozor -> xazina, mol -> bozor)
        MarketFee,     // egasiz foyda / ombor sotuvi -> xazina
        CitizenTrade
    }

    /// <summary>Bitta qayd etilgan operatsiya (faqat muhimlari alohida saqlanadi: davlat savdosi).</summary>
    public sealed class EconomyTransaction
    {
        public string Id { get; set; } = "";
        public int Day { get; set; }
        public TransactionType Type { get; set; }
        public ProductType Product { get; set; }
        public float Quantity { get; set; }
        public float Amount { get; set; }
        public string NoteUz { get; set; } = "";
    }

    /// <summary>
    /// Barcha iqtisodiy operatsiyalar shu xizmat orqali qayd etiladi:
    ///  - har bir operatsiyaning takrorlanmas Id'si bor — bir xil Id ikki marta bajarilmaydi (idempotent);
    ///  - mayda chakana xaridlar alohida obyekt qilinmaydi, kunlik jamlanma (tur bo'yicha) yuritiladi;
    ///  - davlat savdosi kabi muhim operatsiyalar savdo tarixida saqlanadi.
    /// Online rejimda aynan shu xizmat serverda ishlashi kerak (client faqat so'rov yuboradi).
    /// </summary>
    public sealed class EconomyTransactionService
    {
        private const int MaxProcessedIds = 512;
        private const int MaxHistory = 40;

        private readonly HashSet<string> _processed = new HashSet<string>();
        private readonly Queue<string> _processedOrder = new Queue<string>();
        private readonly float[] _qtyToday = new float[Enum.GetValues(typeof(TransactionType)).Length];
        private readonly float[] _amountToday = new float[Enum.GetValues(typeof(TransactionType)).Length];
        private readonly float[] _qtyTotal = new float[Enum.GetValues(typeof(TransactionType)).Length];
        private readonly float[] _amountTotal = new float[Enum.GetValues(typeof(TransactionType)).Length];

        /// <summary>Savdo tarixi (eng yangisi oxirida).</summary>
        public List<EconomyTransaction> History { get; } = new List<EconomyTransaction>();
        public int Rejected { get; private set; }

        /// <summary>
        /// Operatsiyani boshlashga ruxsat so'raydi. Shu Id oldin bajarilgan bo'lsa false qaytaradi —
        /// chaqiruvchi hech narsani o'zgartirmasligi kerak.
        /// </summary>
        public bool TryBegin(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_processed.Contains(id)) { Rejected++; return false; }
            _processed.Add(id);
            _processedOrder.Enqueue(id);
            while (_processedOrder.Count > MaxProcessedIds) _processed.Remove(_processedOrder.Dequeue());
            return true;
        }

        public bool WasProcessed(string id) => _processed.Contains(id);

        /// <summary>Kunlik jamlanmaga qo'shadi (mayda operatsiyalar uchun).</summary>
        public void Add(TransactionType type, float quantity, float amount)
        {
            int i = (int)type;
            _qtyToday[i] += quantity; _amountToday[i] += amount;
            _qtyTotal[i] += quantity; _amountTotal[i] += amount;
        }

        /// <summary>Muhim operatsiyani jamlanmaga ham, tarixga ham yozadi.</summary>
        public void Log(string id, int day, TransactionType type, ProductType product, float quantity, float amount, string noteUz)
        {
            Add(type, quantity, amount);
            History.Add(new EconomyTransaction { Id = id, Day = day, Type = type, Product = product, Quantity = quantity, Amount = amount, NoteUz = noteUz });
            while (History.Count > MaxHistory) History.RemoveAt(0);
        }

        public void BeginDay()
        {
            Array.Clear(_qtyToday, 0, _qtyToday.Length);
            Array.Clear(_amountToday, 0, _amountToday.Length);
        }

        public float QuantityToday(TransactionType t) => _qtyToday[(int)t];
        public float AmountToday(TransactionType t) => _amountToday[(int)t];
        public float QuantityTotal(TransactionType t) => _qtyTotal[(int)t];
        public float AmountTotal(TransactionType t) => _amountTotal[(int)t];

        // ---- Save / Load ----
        public List<string> ProcessedIds() => new List<string>(_processedOrder);

        public void Restore(List<string> processedIds, List<EconomyTransaction> history)
        {
            _processed.Clear();
            _processedOrder.Clear();
            if (processedIds != null)
                foreach (string id in processedIds)
                    if (_processed.Add(id)) _processedOrder.Enqueue(id);
            History.Clear();
            if (history != null) History.AddRange(history);
        }
    }
}
