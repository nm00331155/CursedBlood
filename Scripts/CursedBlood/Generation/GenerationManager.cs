using CursedBlood.Equipment;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Generation
{
    public sealed class GenerationManager
    {
        public InheritanceData ProcessInheritance(PlayerStats stats, EquipmentData selectedHeirloom, long remainingGold)
        {
            var nextGeneration = stats.Generation + 1;
            var isMale = GD.Randf() >= 0.5f;
            var inheritanceRate = stats?.EffectiveInheritanceRate ?? 0.3f;
            return new InheritanceData
            {
                Heirloom = selectedHeirloom?.Clone(),
                Gold = (long)(remainingGold * inheritanceRate),
                Generation = nextGeneration,
                CharacterName = NameGenerator.Generate(isMale),
                IsMale = isMale
            };
        }

        public GenerationRecord CreateRecord(PlayerStats stats, string deathCause, long repaidDebt, long inheritedGold)
        {
            return new GenerationRecord
            {
                Generation = stats.Generation,
                Name = stats.CharacterName,
                IsMale = stats.IsMale,
                MaxDepth = stats.MaxDepth,
                DeathCause = deathCause,
                WeaponName = stats.Inventory.GetEquipped(EquipmentCategory.Pickaxe)?.Name ?? "素手",
                Score = stats.CalculateScore(),
                EnemiesKilled = stats.EnemiesKilled,
                HumanAge = (int)stats.HumanAge,
                GoldEarned = stats.LifetimeGold,
                GoldInherited = inheritedGold,
                RepaidDebt = repaidDebt
            };
        }
    }
}