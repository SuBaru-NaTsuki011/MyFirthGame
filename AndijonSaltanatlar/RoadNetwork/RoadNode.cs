using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>Yo'l grafi tuguni: bitta yo'l katagi.</summary>
    public sealed class RoadNode
    {
        public int Id { get; set; }
        public RoadType Type { get; set; }
        public Point GridPosition { get; set; }
        public float Capacity { get; set; }

        /// <summary>Qo'shni tugun → ularni bog'lovchi qirra Id'si.</summary>
        public Dictionary<int, int> Outgoing { get; } = new Dictionary<int, int>();

        public int Degree => Outgoing.Count;

        public RoadNode() { }

        public RoadNode(int id, RoadType type, Point grid, float capacity = 5f)
        {
            Id = id;
            Type = type;
            GridPosition = grid;
            Capacity = capacity;
        }

        public void AddOutgoing(int target, int edgeId) => Outgoing[target] = edgeId;

        public void RemoveOutgoing(int target, int edgeId)
        {
            if (Outgoing.TryGetValue(target, out int eid) && eid == edgeId)
                Outgoing.Remove(target);
        }
    }
}
