using System;
using CursedBlood.Core;
using Godot;

namespace CursedBlood.Player
{
    public enum LifePhase
    {
        Youth,
        Prime,
        Twilight
    }

    public sealed class PlayerStats
    {
        public const float DefaultMaxLifespan = 60f;
        public const int DefaultMaxHp = 100;
        public static readonly Vector2I StartGridPosition = new(33, 8);

        public float MaxLifespan { get; } = DefaultMaxLifespan;

        public float CurrentAge { get; private set; }

        public int MaxHp { get; } = DefaultMaxHp;

        public int CurrentHp { get; private set; }

        public Vector2I GridPosition { get; set; } = StartGridPosition;

        public int PlayerSize { get; private set; } = 3;

        public int MaxDepthRow { get; private set; } = StartGridPosition.Y;

        public int MaxDepthPixels => MaxDepthRow * ChunkManager.CellSize;

        public int MaxDepthMeters => MaxDepthRow;

        public float BaseMoveInterval { get; } = 0.02f;

        public int DigWidth { get; } = 5;

        public DigShape DigShape { get; } = DigShape.Square;

        public int Generation { get; set; } = 1;

        public int BlocksDug { get; private set; }

        public int EnemiesKilled { get; private set; }

        public int MaxCombo { get; private set; }

        public int CurrentCombo { get; private set; }

        public long Gold { get; private set; }

        public float HumanAge => CurrentAge * 35f / MaxLifespan;

        public LifePhase Phase => CurrentAge switch
        {
            <= 20f => LifePhase.Youth,
            <= 45f => LifePhase.Prime,
            _ => LifePhase.Twilight
        };

        public float PhaseMultiplier => Phase switch
        {
            LifePhase.Youth => 0.6f,
            LifePhase.Prime => 1.0f,
            LifePhase.Twilight => 0.7f,
            _ => 1.0f
        };

        public float EffectiveMoveInterval => BaseMoveInterval / PhaseMultiplier;

        public bool IsAlive => CurrentHp > 0 && CurrentAge < MaxLifespan;

        public void AdvanceTime(float delta)
        {
            CurrentAge = Mathf.Min(MaxLifespan, CurrentAge + Mathf.Max(0f, delta));
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
        }

        public long CalculateScore()
        {
            var depthScore = Math.Max(1, MaxDepthMeters);
            var enemyMultiplier = Math.Max(1, EnemiesKilled);
            var comboMultiplier = Math.Max(1, 1 + MaxCombo / 5);
            var generationBonus = Math.Max(1, Generation);
            return (long)depthScore * enemyMultiplier * comboMultiplier * generationBonus;
        }

        public void Reset()
        {
            CurrentAge = 0f;
            CurrentHp = MaxHp;
            GridPosition = StartGridPosition;
            PlayerSize = 3;
            MaxDepthRow = StartGridPosition.Y;
            BlocksDug = 0;
            EnemiesKilled = 0;
            MaxCombo = 0;
            CurrentCombo = 0;
            Gold = 0L;
        }

        public bool UpdatePhaseState()
        {
            var desiredSize = Phase == LifePhase.Youth ? 3 : 5;
            if (desiredSize == PlayerSize)
            {
                return false;
            }

            PlayerSize = desiredSize;
            return true;
        }

        public void RegisterDig(int dugBlocks)
        {
            BlocksDug += Math.Max(0, dugBlocks);
        }

        public void RegisterDepth(int row)
        {
            MaxDepthRow = Math.Max(MaxDepthRow, row);
        }
    }
}