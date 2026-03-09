using System.Collections.Generic;
using CursedBlood.Core;

namespace CursedBlood.Generation
{
    public sealed class GenerationRecord
    {
        public int Generation { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsMale { get; set; }

        public int MaxDepth { get; set; }

        public string DeathCause { get; set; } = string.Empty;

        public string WeaponName { get; set; } = string.Empty;

        public long Score { get; set; }

        public int EnemiesKilled { get; set; }

        public int HumanAge { get; set; }

        public long GoldEarned { get; set; }

        public long GoldInherited { get; set; }

        public long RepaidDebt { get; set; }
    }

    public sealed class FamilyTree
    {
        private const string SavePath = "user://family_tree.json";

        public List<GenerationRecord> Records { get; set; } = new();

        public static FamilyTree Load()
        {
            return JsonStorage.Load(SavePath, () => new FamilyTree());
        }

        public void AddRecord(GenerationRecord record)
        {
            Records.Insert(0, record);
        }

        public void Save()
        {
            JsonStorage.Save(SavePath, this);
        }
    }
}