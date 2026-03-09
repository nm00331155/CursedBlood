using System.Collections.Generic;
using System.Linq;
using CursedBlood.Core;
using CursedBlood.Player;

namespace CursedBlood.Achievement
{
    public sealed class RankingEntry
    {
        public string Name { get; set; } = string.Empty;

        public int Generation { get; set; }

        public string DateString { get; set; } = string.Empty;

        public int Depth { get; set; }

        public long Score { get; set; }

        public float RunSeconds { get; set; }
    }

    public sealed class RankingBoard
    {
        private const string SavePath = "user://ranking_board.json";

        public List<RankingEntry> TopDepths { get; set; } = new();

        public List<RankingEntry> TopScores { get; set; } = new();

        public List<RankingEntry> FastestDepth1000s { get; set; } = new();

        public float BestTimeToDepth100 { get; set; } = float.MaxValue;

        public int BestDepth => TopDepths.FirstOrDefault()?.Depth ?? 0;

        public long BestScore => TopScores.FirstOrDefault()?.Score ?? 0L;

        public float BestTimeToDepth1000 => FastestDepth1000s.FirstOrDefault()?.RunSeconds ?? BestTimeToDepth100;

        public static RankingBoard Load()
        {
            return JsonStorage.Load(SavePath, () => new RankingBoard());
        }

        public void Save()
        {
            JsonStorage.Save(SavePath, this);
        }

        public void RegisterRun(PlayerStats stats)
        {
            var entry = new RankingEntry
            {
                Name = stats.CharacterName,
                Generation = stats.Generation,
                DateString = System.DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Depth = stats.MaxDepth,
                Score = stats.CalculateScore(),
                RunSeconds = stats.CurrentAge
            };

            TopDepths.Add(entry);
            TopDepths = TopDepths.OrderByDescending(candidate => candidate.Depth).Take(10).ToList();

            TopScores.Add(entry);
            TopScores = TopScores.OrderByDescending(candidate => candidate.Score).Take(10).ToList();

            if (stats.MaxDepth >= 1000)
            {
                FastestDepth1000s.Add(entry);
                FastestDepth1000s = FastestDepth1000s.OrderBy(candidate => candidate.RunSeconds).Take(10).ToList();
                BestTimeToDepth100 = System.MathF.Min(BestTimeToDepth100, stats.CurrentAge);
            }
        }
    }
}