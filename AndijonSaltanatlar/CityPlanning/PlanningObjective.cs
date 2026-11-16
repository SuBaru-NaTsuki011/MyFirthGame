using System;

namespace AndijonSaltanatlar.CityPlanning
{
    /// <summary>
    /// Rejalashtirish maqsadi — PROMPT 15, 6 & 15-bo'lim. Mahalla turiga va shahar ehtiyojiga qarab
    /// quriladigan binolar tarkibini belgilaydi. Haqiqiy katalog narxlari CityPlanningManager'da olinadi.
    /// </summary>
    public sealed class PlanningObjective
    {
        public DistrictType Type { get; set; }
        public string LabelUz { get; set; } = "";
        public int Houses { get; set; }
        public int Markets { get; set; }
        public int Gardens { get; set; }
        public int Madrasas { get; set; }
        public int Mosques { get; set; }
        public int Caravanserais { get; set; }
        public int MaxBuildings { get; set; } = 12;

        public int RequestedBuildings => Houses + Markets + Gardens + Madrasas + Mosques + Caravanserais;

        public static PlanningObjective ForDistrict(DistrictType type, int housingShortage)
        {
            switch (type)
            {
                case DistrictType.Residential:
                    return new PlanningObjective
                    {
                        Type = type, LabelUz = "Turar joy mahallasi",
                        Houses = Math.Max(3, Math.Min(6, (Math.Max(0, housingShortage) + 4) / 5)),
                        Markets = 1, Gardens = 1, MaxBuildings = 10
                    };
                case DistrictType.Commercial:
                    return new PlanningObjective
                    {
                        Type = type, LabelUz = "Savdo mahallasi",
                        Houses = 1, Markets = 2, Gardens = 1, Caravanserais = 1, MaxBuildings = 8
                    };
                case DistrictType.Craft:
                    return new PlanningObjective
                    {
                        Type = type, LabelUz = "Hunarmandchilik hududi",
                        Houses = 2, Markets = 1, Gardens = 1, MaxBuildings = 8
                    };
                case DistrictType.Cultural:
                    return new PlanningObjective
                    {
                        Type = type, LabelUz = "Madaniyat mahallasi",
                        Houses = 1, Madrasas = 1, Mosques = 1, Gardens = 1, MaxBuildings = 8
                    };
                case DistrictType.Agricultural:
                    return new PlanningObjective
                    {
                        Type = type, LabelUz = "Dehqonchilik hududi",
                        Houses = 1, Markets = 1, Gardens = 4, MaxBuildings = 8
                    };
                case DistrictType.Garden:
                    return new PlanningObjective
                    {
                        Type = type, LabelUz = "Bog' va dam olish hududi",
                        Gardens = 3, MaxBuildings = 6
                    };
                default:
                    return new PlanningObjective
                    {
                        Type = DistrictType.Mixed, LabelUz = "Muvozanatli yangi mahalla",
                        Houses = 3, Markets = 1, Gardens = 1, MaxBuildings = 8
                    };
            }
        }

        /// <summary>Mavjud mahalla ichidagi bo'shliqlarni to'ldirish — kichik, tejamkor reja.</summary>
        public static PlanningObjective ForImprovement(int housingShortage)
        {
            return new PlanningObjective
            {
                Type = DistrictType.Mixed,
                LabelUz = "Mavjud mahallani yaxshilash",
                Houses = Math.Max(1, Math.Min(2, (Math.Max(0, housingShortage) + 4) / 5)),
                Markets = 1,
                Gardens = 2,
                MaxBuildings = 6
            };
        }
    }
}
