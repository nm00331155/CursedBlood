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
        public const int SurfaceRow = 8;
        public static readonly Vector2I StartGridPosition = new(0, 10);

        private float _currentMoveSpeedMultiplier = 1f;
        private float _currentDigSpeedMultiplier = 1f;
        private float _hazardMoveSlowdownMultiplier = 1f;
        private float _hazardDigSlowdownMultiplier = 1f;
        private string _hazardStatusLabel = string.Empty;

        public float MaxDiveTime { get; } = DefaultMaxDiveTime;

        public FailureCostCalculator FailureCostCalculator { get; set; } = new();

        public float CurrentDiveTime { get; private set; }

        public float OxygenRatio => Mathf.Clamp(1f - (CurrentDiveTime / MaxDiveTime), 0f, 1f);

        public float FilterRatio => Phase switch
        {
            DivePhase.Stable => 1f,
            DivePhase.Worn => 0.70f,
            _ => 0.34f
        };

        public int MaxHp { get; } = DefaultMaxHp;

        public int CurrentHp { get; private set; }

        public Vector2I GridPosition { get; set; } = StartGridPosition;

        public int PlayerSize { get; } = 5;

        public int MaxDepthRow { get; private set; } = StartGridPosition.Y;

        public int MaxDepthPixels => MaxDepthMeters * ChunkManager.CellSize;

        public int MaxDepthMeters => Mathf.Max(0, MaxDepthRow - SurfaceRow);

        public float BaseMoveInterval { get; } = 0.0125f;

        public int DigWidth { get; } = 7;

        public DigShape DigShape { get; } = DigShape.Square;

        public int DiveCount { get; private set; } = 1;

        public long CurrentDebt { get; private set; } = DefaultDebt;

        public long CurrentMoney { get; private set; }

        public long SalvageValue { get; private set; }

        public int BlocksDug { get; private set; }

        public int OresCollected { get; private set; }

        public bool ReturnedSafely { get; private set; }

        public bool Rescued { get; private set; }

        public bool HasLeftSurface { get; private set; }

        public int CurrentChainCount { get; private set; }

        public int BestChainCount { get; private set; }

        public float CarryValueMultiplier { get; private set; } = 1f;

        public int CurrentSonarBonusRadius { get; private set; }

        public float ActiveBoostTimeRemaining { get; private set; }

        public float TotalBonusTimeGranted { get; private set; }

        public float HazardDebuffTimeRemaining { get; private set; }

        public float RemainingDiveTime => Mathf.Max(0f, MaxDiveTime - CurrentDiveTime);

        public int RemainingDiveSeconds => Mathf.CeilToInt(RemainingDiveTime);

        public int CurrentDepthMeters => Mathf.Max(0, GridPosition.Y - SurfaceRow);

        public string PhaseLabel => Phase switch
        {
            DivePhase.Stable => "余裕",
            DivePhase.Worn => "消耗",
            _ => "危険"
        };

        public string ActiveHazardLabel => HazardDebuffTimeRemaining > 0f && !string.IsNullOrWhiteSpace(_hazardStatusLabel)
            ? $"{_hazardStatusLabel} {HazardDebuffTimeRemaining:0.0}s"
            : string.Empty;

        public DivePhase Phase => CurrentDiveTime switch
        {
            <= 18f => DivePhase.Stable,
            <= 42f => DivePhase.Worn,
            _ => DivePhase.Critical
        };

        public float PhaseMultiplier => Phase switch
        {
            DivePhase.Stable => 1.00f,
            DivePhase.Worn => 0.88f,
            _ => 0.72f
        };

        public float EffectiveMoveInterval => ResolveMoveDuration(1f, false, 1f);

        public bool IsAlive => CurrentHp > 0 && CurrentDiveTime < MaxDiveTime;

        public bool IsOperational => IsAlive;

        public bool IsTimeExpired => CurrentDiveTime >= MaxDiveTime;

        public bool CanReturnFromSurface => HasLeftSurface && GridPosition.Y <= StartGridPosition.Y;

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
            CurrentChainCount = 0;
            BestChainCount = 0;
            CarryValueMultiplier = 1f;
            CurrentSonarBonusRadius = 0;
            ActiveBoostTimeRemaining = 0f;
            TotalBonusTimeGranted = 0f;
            _currentMoveSpeedMultiplier = 1f;
            _currentDigSpeedMultiplier = 1f;
            _hazardMoveSlowdownMultiplier = 1f;
            _hazardDigSlowdownMultiplier = 1f;
            _hazardStatusLabel = string.Empty;
            HazardDebuffTimeRemaining = 0f;
        }

        public void AdvanceTime(float delta)
        {
            CurrentDiveTime = Mathf.Min(MaxDiveTime, CurrentDiveTime + Mathf.Max(0f, delta));
            if (HazardDebuffTimeRemaining <= 0f)
            {
                return;
            }

            HazardDebuffTimeRemaining = Mathf.Max(0f, HazardDebuffTimeRemaining - Mathf.Max(0f, delta));
            if (HazardDebuffTimeRemaining > 0f)
            {
                return;
            }

            _hazardMoveSlowdownMultiplier = 1f;
            _hazardDigSlowdownMultiplier = 1f;
            _hazardStatusLabel = string.Empty;
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
            if (row >= StartGridPosition.Y + 8)
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
            var value = 140L + (Mathf.Max(0, row - SurfaceRow) * 5L);
            SalvageValue += value;
            return value;
        }

        public void GrantDiveTime(float bonusSeconds)
        {
            if (bonusSeconds <= 0f)
            {
                return;
            }

            TotalBonusTimeGranted += bonusSeconds;
            CurrentDiveTime = Mathf.Max(0f, CurrentDiveTime - bonusSeconds);
        }

        public void ConsumeDiveTime(float penaltySeconds)
        {
            if (penaltySeconds <= 0f)
            {
                return;
            }

            CurrentDiveTime = Mathf.Min(MaxDiveTime, CurrentDiveTime + penaltySeconds);
        }

        public void ApplyHazard(int damage, float oxygenPenaltySeconds, float moveSlowdownMultiplier, float digSlowdownMultiplier, float debuffDurationSeconds, string statusLabel)
        {
            TakeDamage(damage);
            ConsumeDiveTime(oxygenPenaltySeconds);

            if (debuffDurationSeconds <= 0f)
            {
                return;
            }

            HazardDebuffTimeRemaining = Math.Max(HazardDebuffTimeRemaining, debuffDurationSeconds);
            _hazardMoveSlowdownMultiplier = Math.Max(_hazardMoveSlowdownMultiplier, Mathf.Max(1f, moveSlowdownMultiplier));
            _hazardDigSlowdownMultiplier = Math.Max(_hazardDigSlowdownMultiplier, Mathf.Max(1f, digSlowdownMultiplier));
            _hazardStatusLabel = string.IsNullOrWhiteSpace(statusLabel) ? "妨害継続" : statusLabel;
        }

        public void UpdateChainState(int currentChainCount, int bestChainCount, float carryValueMultiplier, float moveSpeedMultiplier, float digSpeedMultiplier, int sonarBonusRadius, float boostTimeRemaining)
        {
            CurrentChainCount = Math.Max(0, currentChainCount);
            BestChainCount = Math.Max(BestChainCount, Math.Max(0, bestChainCount));
            CarryValueMultiplier = Mathf.Max(1f, carryValueMultiplier);
            _currentMoveSpeedMultiplier = Mathf.Max(1f, moveSpeedMultiplier);
            _currentDigSpeedMultiplier = Mathf.Max(1f, digSpeedMultiplier);
            CurrentSonarBonusRadius = Math.Max(0, sonarBonusRadius);
            ActiveBoostTimeRemaining = Mathf.Max(0f, boostTimeRemaining);
        }

        public float ResolveMoveDuration(float terrainHardness, bool requiresDig, float contextSlowdownMultiplier)
        {
            var effectiveSpeed = PhaseMultiplier * _currentMoveSpeedMultiplier * (requiresDig ? _currentDigSpeedMultiplier : 1f);
            var clampedSpeed = Mathf.Max(0.18f, effectiveSpeed);
            var hazardSlowdownMultiplier = requiresDig ? _hazardDigSlowdownMultiplier : _hazardMoveSlowdownMultiplier;
            return (BaseMoveInterval * Mathf.Max(1f, terrainHardness) * Mathf.Max(1f, contextSlowdownMultiplier) * Mathf.Max(1f, hazardSlowdownMultiplier)) / clampedSpeed;
        }

        public long CalculateScore()
        {
            var depthScore = Math.Max(1, MaxDepthMeters) * 100L;
            var salvageScore = (long)Math.Round(SalvageValue * CarryValueMultiplier);
            var digScore = BlocksDug * 8L;
            var stabilityBonus = ReturnedSafely ? 5_000L : 0L;
            var chainBonus = BestChainCount * 24L;
            return depthScore + salvageScore + digScore + stabilityBonus + chainBonus;
        }

        public DiveResultData FinalizeDive(DiveEndReason endReason, string rescueReason)
        {
            ReturnedSafely = endReason is DiveEndReason.SurfaceReturn or DiveEndReason.RecoveryPointReturn;
            Rescued = !ReturnedSafely;

            var debtBefore = CurrentDebt;
            var moneyBefore = CurrentMoney;
            var boostedSalvageValue = (long)Math.Round(SalvageValue * CarryValueMultiplier);
            var chainBonusValue = Math.Max(0L, boostedSalvageValue - SalvageValue);
            var lostValue = ReturnedSafely ? 0L : (long)Math.Round(boostedSalvageValue * 0.70f);
            var carriedValue = Math.Max(0L, boostedSalvageValue - lostValue);
            var rescueCostBreakdown = ReturnedSafely
                ? default
                : FailureCostCalculator.Calculate(MaxDepthMeters, boostedSalvageValue, CurrentChainCount);
            var rescueCost = ReturnedSafely ? 0L : rescueCostBreakdown.TotalCost;
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
                RescueCostBase = rescueCostBreakdown.BaseCost,
                RescueCostDepth = rescueCostBreakdown.DepthCost,
                RescueCostInventory = rescueCostBreakdown.InventoryCost,
                RescueCostChain = rescueCostBreakdown.ChainCost,
                DebtBefore = debtBefore,
                DebtAfter = CurrentDebt,
                MoneyBefore = moneyBefore,
                MoneyAfter = CurrentMoney,
                Score = CalculateScore(),
                SalvageValue = boostedSalvageValue,
                OresCollected = OresCollected,
                BlocksDug = BlocksDug,
                FinalChainCount = CurrentChainCount,
                BestChainCount = BestChainCount,
                CarryMultiplier = CarryValueMultiplier,
                ChainBonusValue = chainBonusValue,
                BonusTimeGranted = TotalBonusTimeGranted
            };
        }
    }
}