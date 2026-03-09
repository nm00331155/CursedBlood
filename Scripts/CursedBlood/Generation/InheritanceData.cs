using CursedBlood.Equipment;

namespace CursedBlood.Generation
{
    public sealed class InheritanceData
    {
        public EquipmentData Heirloom { get; set; }

        public long Gold { get; set; }

        public int Generation { get; set; }

        public string CharacterName { get; set; } = string.Empty;

        public bool IsMale { get; set; }
    }
}