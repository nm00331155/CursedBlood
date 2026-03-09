using System;
using System.Collections.Generic;
using CursedBlood.Core;
using CursedBlood.Debt;

namespace CursedBlood.Save
{
    public sealed class SaveData
    {
        public SaveMeta Meta { get; set; } = new();

        public PlayerProfileData PlayerProfile { get; set; } = new();

        public DebtData Debt { get; set; } = new();

        public ResearchData Research { get; set; } = new();

        public EquipmentData Equipment { get; set; } = new();

        public RecordsData Records { get; set; } = new();

        public AchievementData Achievement { get; set; } = new();

        public RankingData Ranking { get; set; } = new();

        public SettingsData Settings { get; set; } = new();
    }

    public sealed class SaveMeta
    {
        public int Version { get; set; } = 1;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class PlayerProfileData
    {
        public string Gender { get; set; } = "Unknown";

        public string Name { get; set; } = "Diver";

        public bool IsProfileConfigured { get; set; }

        public int TotalDiveCount { get; set; }

        public long CurrentMoney { get; set; }
    }

    public sealed class DebtData
    {
        public long CurrentDebt { get; set; } = Player.PlayerStats.DefaultDebt;

        public long TotalRepaid { get; set; }

        public long TotalInterestPaid { get; set; }

        public long TotalRescueCost { get; set; }

        public bool DebtCleared { get; set; }
    }

    public sealed class ResearchData
    {
        public int SonarRangeLevel { get; set; }

        public int SonarPrecisionLevel { get; set; }

        public int SonarIdentifyLevel { get; set; }

        public int FilterLevel { get; set; }

        public int OxygenLevel { get; set; }

        public int RescueCostReductionLevel { get; set; }
    }

    public sealed class EquipmentData
    {
        public List<string> EquippedItems { get; set; } = new();

        public List<string> Inventory { get; set; } = new();
    }

    public sealed class RecordsData
    {
        public List<DiveRecordData> DiveRecords { get; set; } = new();
    }

    public sealed class AchievementData
    {
        public List<string> UnlockedAchievements { get; set; } = new();

        public Dictionary<string, long> Counters { get; set; } = new();
    }

    public sealed class RankingData
    {
        public int BestDepth { get; set; }

        public long BestSingleProfit { get; set; }

        public int FastestDebtClear { get; set; }

        public long TotalMoneyEarned { get; set; }
    }

    public sealed class SettingsData
    {
        public float VirtualPadOpacity { get; set; } = 0.28f;

        public bool ScreenShakeEnabled { get; set; } = true;

        public bool DebugOverlayEnabled { get; set; }
    }

    public sealed class DiveRecordData
    {
        public int DiveCount { get; set; }

        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        public int MaxDepthMeters { get; set; }

        public bool ReturnedSafely { get; set; }

        public long CarryValue { get; set; }

        public long LostValue { get; set; }

        public long RescueCost { get; set; }

        public long InterestCharged { get; set; }

        public long RepaymentAmount { get; set; }

        public long DebtChange { get; set; }

        public string RepaymentChoice { get; set; } = string.Empty;

        public string RescueReason { get; set; } = string.Empty;

        public long Score { get; set; }

        public static DiveRecordData FromResult(DiveResultData result, DebtSettlementResult settlement)
        {
            return new DiveRecordData
            {
                DiveCount = result.DiveCount,
                Timestamp = DateTimeOffset.UtcNow,
                MaxDepthMeters = result.MaxDepthMeters,
                ReturnedSafely = result.ReturnedSafely,
                CarryValue = result.CarryValue,
                LostValue = result.LostValue,
                RescueCost = result.RescueCost,
                InterestCharged = settlement.InterestAmount,
                RepaymentAmount = settlement.PaymentAmount,
                DebtChange = settlement.DebtAfter - result.DebtBefore,
                RepaymentChoice = settlement.Choice.ToString(),
                RescueReason = result.RescueReason,
                Score = result.Score
            };
        }
    }
}