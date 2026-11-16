namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>Yo'l grafi qirrasi: ikki qo'shni yo'l katagi orasidagi harakat yo'li.</summary>
    public sealed class RoadEdge
    {
        public int Id { get; set; }
        public int Source { get; set; }
        public int Target { get; set; }
        public float MovementCost { get; set; } = 1f;
        /// <summary>Qirra yopiqmi (yo'l buzilgan, ko'prik vayron...). Yopiq qirra bo'ylab marshrut qurilmaydi.</summary>
        public bool Blocked { get; set; }

        public RoadEdge() { }

        public RoadEdge(int id, int source, int target, float movementCost = 1f)
        {
            Id = id;
            Source = source;
            Target = target;
            MovementCost = movementCost;
        }
    }
}
