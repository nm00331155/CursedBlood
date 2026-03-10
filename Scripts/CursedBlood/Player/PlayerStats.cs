using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Player
{
    public enum DivePhase
    {
        Stable,
        Worn,
        Critical
    }

    public sealed class PlayerStats
    {
        public const float DefaultMaxDiveTime = 60f;
        public const int DefaultMaxHp = 100;
        public const long DefaultDebt = 500_000L;
        public static readonly Vector2I StartGridPosition = new(33, 8);

        public float MaxDiveTime { get; } = DefaultMaxDiveTime;

        public float CurrentDiveTime { get; private set; }

        public float OxygenRatio => Mathf.Clamp(1f - (CurrentDiveTime / MaxDiveTime), 0f, 1f);

        public float FilterRatio => Phase switch
        {
            DivePhase.Stable => 1f,
            DivePhase.Worn => 0.65f,
            _ => 0.28f
        };

        public int MaxHp { get; } = DefaultMaxHp;

        public int CurrentHp { get; private set; }

        public Vector2I GridPosition { get; set; } = StartGridPosition;

        public int PlayerSize { get; } = 5;

        public int MaxDepthRow { get; private set; } = StartGridPosition.Y;

        public int MaxDepthPixels => MaxDepthRow * ChunkManager.CellSize;

        public int MaxDepthMeters => MaxDepthRow;

        public float BaseMoveInterval { get; } = 0.02f;

        public int DigWidth { get; } = 5;

        public DigShape DigShape { get; } = DigShape.Square;

        public int DiveCount { get; private set; } = 1;

        public int Generation { get; set; } = 1;

        public long CurrentDebt { get; private set; } = DefaultDebt;

        public long CurrentMoney { get; private set; }

        public long SalvageValue { get; private set; }

        public int BlocksDug { get; private set; }

        public int OresCollected { get; private set; }

        public bool ReturnedSafely { get; private set; }

        public bool Rescued { get; private set; }

        public bool HasLeftSurface { get; private set; }

        public float RemainingDiveTime => Mathf.Max(0f, MaxDiveTime - CurrentDiveTime);

        public int RemainingDiveSeconds => Mathf.CeilToInt(RemainingDiveTime);

        public int CurrentDepthMeters => GridPosition.Y;

        public string PhaseLabel => Phase switch
        {
            DivePhase.Stable => "Stable",
            DivePhase.Worn => "Worn",
            _ => "Critical"
        };

        public DivePhase Phase => CurrentDiveTime switch
        {
            <= 20f => DivePhase.Stable,
            <= 45f => DivePhase.Worn,
            _ => DivePhase.Critical
        };

        public float PhaseMultiplier => Phase switch
        {
            DivePhase.Stable => 1.0f,
            DivePhase.Worn => 0.82f,
            _ => 0.62f
        };

        public float EffectiveMoveInterval => BaseMoveInterval / PhaseMultiplier;

        public bool IsAlive => CurrentHp > 0 && CurrentDiveTime < MaxDiveTime;

        public bool IsOperational => IsAlive;

        public bool IsTimeExpired => CurrentDiveTime >= MaxDiveTime;

        public bool CanReturnFromSurface => HasLeftSurface && GridPosition.Y <= StartGridPosition.Y + 2;

        public void ConfigureDive(int diveCount, long currentDebt, long currentMoney)
        {
            DiveCount = Math.Max(1, diveCount);
            CurrentDebt = Math.Max(0L, currentDebt);
            CurrentMoney = Math.Max(0L, currentMoney);
            Reset();
        }

        public void Reset()
        {
            CurrentDiveTime = 0f;
            CurrentHp = MaxHp;
            GridPosition = StartGridPosition;
            MaxDepthRow = StartGridPosition.Y;
            SalvageValue = 0L;
            BlocksDug = 0;
            OresCollected = 0;
            ReturnedSafely = false;
            Rescued = false;
            HasLeftSurface = false;
        }

        public void AdvanceTime(float delta)
        {
            CurrentDiveTime = Mathf.Min(MaxDiveTime, CurrentDiveTime + Mathf.Max(0f, delta));
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
        }

        public void RegisterDig(int dugBlocks)
        {
            BlocksDug += Math.Max(0, dugBlocks);
        }

        public void RegisterDepth(int row)
        {
            MaxDepthRow = Math.Max(MaxDepthRow, row);
            if (row >= StartGridPosition.Y + 6)
            {
                HasLeftSurface = true;
            }
        }

        public long RegisterLoot(CellType type, int row)
        {
            if (type != CellType.Ore)
            {
                return 0L;
            }

            OresCollected++;
            var value = 120L + Math.Max(0, row - StartGridPosition.Y) * 4L;
            SalvageValue += value;
            return value;
        }

        public long CalculateScore()
        {
            var depthScore = Math.Max(1, MaxDepthMeters) * 100L;
            var salvageScore = SalvageValue;
            var digScore = BlocksDug * 8L;
            var stabilityBonus = ReturnedSafely ? 5_000L : 0L;
            return depthScore + salvageScore + digScore + stabilityBonus;
        }

        public DiveResultData FinalizeDive(DiveEndReason endReason, string rescueReason)
        {
            ReturnedSafely = endReason is DiveEndReason.SurfaceReturn or DiveEndReason.RecoveryPointReturn;
            Rescued = !ReturnedSafely;

            var debtBefore = CurrentDebt;
            var moneyBefore = CurrentMoney;
            var lostValue = ReturnedSafely ? 0L : (long)Mathf.Round(SalvageValue * 0.70f);
            var carriedValue = Math.Max(0L, SalvageValue - lostValue);
            var rescueCost = ReturnedSafely ? 0L : CalculateRescueCost();
            var postDiveMoney = moneyBefore + carriedValue - rescueCost;
            if (postDiveMoney >= 0L)
            {
                CurrentMoney = postDiveMoney;
            }
            else
            {
                CurrentMoney = 0L;
                CurrentDebt += -postDiveMoney;
            }

            return new DiveResultData
            {
                DiveCount = DiveCount,
                MaxDepthMeters = MaxDepthMeters,
                EndReason = endReason,
                RescueReason = rescueReason,
                ReturnedSafely = ReturnedSafely,
                Rescued = Rescued,
                CarryValue = carriedValue,
                LostValue = lostValue,
                RescueCost = rescueCost,
                DebtBefore = debtBefore,
                DebtAfter = CurrentDebt,
                MoneyBefore = moneyBefore,
                MoneyAfter = CurrentMoney,
                Score = CalculateScore(),
                SalvageValue = SalvageValue,
                OresCollected = OresCollected,
                BlocksDug = BlocksDug
            };
        }

        private long CalculateRescueCost()
        {
            var depthFee = Math.Max(0, MaxDepthMeters - StartGridPosition.Y) * 6L;
            var phaseFee = Phase == DivePhase.Critical ? 400L : 0L;
            return 900L + depthFee + phaseFee;
        }
    }
}