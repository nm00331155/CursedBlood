using System.Collections.Generic;
using CursedBlood.Core;
using CursedBlood.Player;

namespace CursedBlood.Curse
{
    public sealed class CurseResearchManager
    {
        private const string SavePath = "user://curse_research.json";

        private float _fractionalPointBuffer;

        public int TotalPoints { get; set; }

        public HashSet<int> ClaimedDepthBonuses { get; set; } = new();

        public bool EndingCleared { get; set; }

        public int BonusYears => TotalPoints / 100;

        public float BonusSeconds => BonusYears * (60f / 35f);

        public static CurseResearchManager Load()
        {
            return JsonStorage.Load(SavePath, () => new CurseResearchManager());
        }

        public void Save()
        {
            JsonStorage.Save(SavePath, this);
        }

        public void AddPoints(int amount)
        {
            TotalPoints += amount;
        }

        public void UpdateFromPlay(PlayerStats stats, float delta)
        {
            if (stats.HasCursedEquipment)
            {
                var gain = delta * 0.5f * (1f + stats.AchievementBonuses.CurseResearchBonus);
                _fractionalPointBuffer += gain;
                var wholePoints = (int)System.MathF.Floor(_fractionalPointBuffer);
                if (wholePoints <= 0)
                {
                    return;
                }

                _fractionalPointBuffer -= wholePoints;
                AddPoints(wholePoints);
            }
        }

        public void RegisterDepth(int depth)
        {
            AwardDepthBonus(depth, 200, 10);
            AwardDepthBonus(depth, 500, 30);
            AwardDepthBonus(depth, 1000, 100);
        }

        public void RegisterBossKill()
        {
            AddPoints(5);
        }

        private void AwardDepthBonus(int currentDepth, int threshold, int points)
        {
            if (currentDepth < threshold || ClaimedDepthBonuses.Contains(threshold))
            {
                return;
            }

            ClaimedDepthBonuses.Add(threshold);
            AddPoints(points);
        }
    }
}