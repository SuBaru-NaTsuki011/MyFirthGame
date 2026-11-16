using System.Collections.Generic;

namespace AndijonSaltanatlar.Military
{
    /// <summary>Qo'shin guruhi (masalan: Saroy soqchilari, Shahar qo'riqchilari, Asosiy qo'shin, Zaxira).</summary>
    public sealed class Army
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int AssignedRegionId { get; set; }
        public int? CommanderId { get; set; }
        public List<int> UnitIds { get; } = new List<int>();
    }
}
