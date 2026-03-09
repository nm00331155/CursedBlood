using System.Collections.Generic;
using System.Linq;
using CursedBlood.Skill;
using Godot;

namespace CursedBlood.Equipment
{
    public enum EquipmentCategory
    {
        Pickaxe,
        Armor,
        Accessory,
        Boots
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Cursed
    }

    public sealed class EquipmentData
    {
        public string Name { get; set; } = string.Empty;

        public EquipmentCategory Category { get; set; }

        public Rarity Rarity { get; set; }

        public int DropDepth { get; set; }

        public float BaseValue { get; set; }

        public List<EquipmentEffect> Effects { get; set; } = new();

        public EquipmentEffect Demerit { get; set; }

        public SkillType SkillType { get; set; } = SkillType.AreaBreak3x3;

        public float PowerScore => BaseValue * (1f + Effects.Count * 0.15f) - (Demerit?.Value ?? 0f);

        public EquipmentData Clone()
        {
            return new EquipmentData
            {
                Name = Name,
                Category = Category,
                Rarity = Rarity,
                DropDepth = DropDepth,
                BaseValue = BaseValue,
                SkillType = SkillType,
                Effects = Effects.Select(effect => effect.Clone()).ToList(),
                Demerit = Demerit?.Clone()
            };
        }

        public Color GetRarityColor()
        {
            return Rarity switch
            {
                Rarity.Common => new Color(0.6f, 0.6f, 0.6f),
                Rarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),
                Rarity.Rare => new Color(0.3f, 0.5f, 1f),
                Rarity.Epic => new Color(0.7f, 0.3f, 0.9f),
                Rarity.Legendary => new Color(1f, 0.85f, 0.2f),
                Rarity.Cursed => new Color(0.8f, 0.1f, 0.1f),
                _ => Colors.White
            };
        }

        public string GetSummary()
        {
            var effectCount = Effects.Count;
            var demeritText = Demerit == null ? string.Empty : $" / 呪い {Demerit.DemeritType}";
            return $"{Name} [{Rarity}] 基礎:{BaseValue:F1} 効果:{effectCount}{demeritText}";
        }
    }
}