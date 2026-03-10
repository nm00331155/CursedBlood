using System;
using Godot;

namespace CursedBlood.Core
{
    public readonly record struct ChainVisualState(bool HasCheckpoint, Vector2I CheckpointCell, float TimeRatio, int CurrentChainCount);

    public sealed class ChainManager
    {
        private readonly RandomNumberGenerator _rng = new();

        private Vector2I _activeCheckpoint;
        private bool _hasActiveCheckpoint;
        private bool _countdownActive;
        private float _timeRemaining;
        private float _temporaryBoostTimeRemaining;
        private string _pendingNotification = string.Empty;

        public float ChainTimeLimitSeconds { get; set; } = 5f;

        public int CheckpointDistanceMin { get; set; } = 12;

        public int CheckpointDistanceMax { get; set; } = 20;

        public int CheckpointDownwardBiasMin { get; set; } = 6;

        public int CheckpointDownwardBiasMax { get; set; } = 15;

        public int CheckpointHorizontalReach { get; set; } = 9;

        public float ChainRewardBaseRate { get; set; } = 0.0022f;

        public int ChainMilestoneInterval { get; set; } = 50;

        public float ChainMilestoneBonusRate { get; set; } = 0.06f;

        public float EveryFiveBonusSeconds { get; set; } = 1f;

        public float TemporaryMoveSpeedMultiplier { get; set; } = 1.12f;

        public float TemporaryDigSpeedMultiplier { get; set; } = 1.16f;

        public float TemporaryBoostDurationSeconds { get; set; } = 4.5f;

        public int TemporarySonarBonusRadius { get; set; } = 6;

        public float SonarGuidanceStrength { get; set; } = 0.35f;

        public int CurrentChainCount { get; private set; }

        public int BestChainCount { get; private set; }

        public float CarryValueMultiplier { get; private set; } = 1f;

        public bool HasActiveCheckpoint => _hasActiveCheckpoint;

        public Vector2I ActiveCheckpoint => _activeCheckpoint;

        public float TimeRemainingSeconds => _countdownActive ? _timeRemaining : ChainTimeLimitSeconds;

        public float TimeRatio => !_countdownActive || ChainTimeLimitSeconds <= 0.001f
            ? 1f
            : Mathf.Clamp(_timeRemaining / ChainTimeLimitSeconds, 0f, 1f);

        public int CurrentSonarBonusRadius => _temporaryBoostTimeRemaining > 0f ? TemporarySonarBonusRadius : 0;

        public float CurrentMoveSpeedMultiplier => _temporaryBoostTimeRemaining > 0f ? TemporaryMoveSpeedMultiplier : 1f;

        public float CurrentDigSpeedMultiplier => _temporaryBoostTimeRemaining > 0f ? TemporaryDigSpeedMultiplier : 1f;

        public float ActiveBoostTimeRemaining => Mathf.Max(0f, _temporaryBoostTimeRemaining);

        public void Reset(ChunkManager chunks, Vector2I startCell, Player.PlayerStats stats)
        {
            _rng.Randomize();
            CurrentChainCount = 0;
            BestChainCount = 0;
            CarryValueMultiplier = 1f;
            _countdownActive = false;
            _timeRemaining = ChainTimeLimitSeconds;
            _temporaryBoostTimeRemaining = 0f;
            _pendingNotification = string.Empty;
            SpawnCheckpoint(chunks, startCell, startCountdown: false);
            SyncStats(stats);
        }

        public void Update(float delta, ChunkManager chunks, Vector2I playerCell, Player.PlayerStats stats)
        {
            if (chunks == null || stats == null)
            {
                return;
            }

            if (_temporaryBoostTimeRemaining > 0f)
            {
                _temporaryBoostTimeRemaining = Mathf.Max(0f, _temporaryBoostTimeRemaining - Mathf.Max(0f, delta));
            }

            if (!_hasActiveCheckpoint)
            {
                SpawnCheckpoint(chunks, playerCell, startCountdown: CurrentChainCount > 0);
            }

            if (_hasActiveCheckpoint && IsPlayerTouchingCheckpoint(playerCell, stats.PlayerSize))
            {
                HandleCheckpointReached(chunks, playerCell, stats);
                return;
            }

            if (_countdownActive)
            {
                _timeRemaining = Mathf.Max(0f, _timeRemaining - Mathf.Max(0f, delta));
                if (_timeRemaining <= 0f)
                {
                    BreakChain(chunks, playerCell, stats);
                    return;
                }
            }

            SyncStats(stats);
        }

        public bool TryConsumeNotification(out string message)
        {
            if (string.IsNullOrWhiteSpace(_pendingNotification))
            {
                message = string.Empty;
                return false;
            }

            message = _pendingNotification;
            _pendingNotification = string.Empty;
            return true;
        }

        public ChainVisualState BuildVisualState()
        {
            return new ChainVisualState(_hasActiveCheckpoint, _activeCheckpoint, TimeRatio, CurrentChainCount);
        }

        private void HandleCheckpointReached(ChunkManager chunks, Vector2I playerCell, Player.PlayerStats stats)
        {
            CurrentChainCount++;
            BestChainCount = Math.Max(BestChainCount, CurrentChainCount);
            CarryValueMultiplier += ChainRewardBaseRate;

            var milestoneReached = ChainMilestoneInterval > 0 && CurrentChainCount % ChainMilestoneInterval == 0;
            if (milestoneReached)
            {
                CarryValueMultiplier += ChainMilestoneBonusRate;
            }

            if (CurrentChainCount % 5 == 0 && EveryFiveBonusSeconds > 0f)
            {
                stats.GrantDiveTime(EveryFiveBonusSeconds);
            }

            if (CurrentChainCount % 10 == 0)
            {
                _temporaryBoostTimeRemaining = Math.Max(_temporaryBoostTimeRemaining, TemporaryBoostDurationSeconds);
            }

            _pendingNotification = BuildCheckpointNotification(milestoneReached);
            SpawnCheckpoint(chunks, playerCell, startCountdown: true);
            SyncStats(stats);
        }

        private void BreakChain(ChunkManager chunks, Vector2I playerCell, Player.PlayerStats stats)
        {
            if (CurrentChainCount > 0)
            {
                _pendingNotification = $"下降チェイン終了 / {CurrentChainCount} で途切れた";
            }

            CurrentChainCount = 0;
            SpawnCheckpoint(chunks, playerCell, startCountdown: false);
            SyncStats(stats);
        }

        private void SpawnCheckpoint(ChunkManager chunks, Vector2I anchor, bool startCountdown)
        {
            if (!TryResolveCheckpoint(chunks, anchor, out var checkpoint))
            {
                _hasActiveCheckpoint = false;
                _countdownActive = false;
                _timeRemaining = ChainTimeLimitSeconds;
                return;
            }

            _activeCheckpoint = checkpoint;
            _hasActiveCheckpoint = true;
            _countdownActive = startCountdown;
            _timeRemaining = ChainTimeLimitSeconds;
        }

        private bool TryResolveCheckpoint(ChunkManager chunks, Vector2I anchor, out Vector2I checkpoint)
        {
            checkpoint = default;
            var bestScore = float.MaxValue;
            var found = false;
            var targetDistance = _rng.RandfRange(CheckpointDistanceMin, CheckpointDistanceMax);
            var minRow = Math.Max(anchor.Y + CheckpointDownwardBiasMin, Player.PlayerStats.StartGridPosition.Y + 3);
            var maxRow = anchor.Y + CheckpointDownwardBiasMax;

            for (var row = minRow; row <= maxRow; row++)
            {
                for (var col = anchor.X - CheckpointHorizontalReach; col <= anchor.X + CheckpointHorizontalReach; col++)
                {
                    if (!chunks.IsInBounds(col, row))
                    {
                        continue;
                    }

                    var candidateType = (CellType)chunks.GetCell(col, row);
                    if (!CellTypeUtil.IsDiggable(candidateType) || candidateType == CellType.RecoveryPoint)
                    {
                        continue;
                    }

                    var delta = new Vector2I(col - anchor.X, row - anchor.Y);
                    var distance = Mathf.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));
                    if (distance < CheckpointDistanceMin - 1 || distance > CheckpointDistanceMax + 2)
                    {
                        continue;
                    }

                    var hardnessPenalty = CellTypeUtil.GetHardness(candidateType) * 1.4f;
                    var lateralPenalty = Mathf.Abs(delta.X) * 0.12f;
                    var distancePenalty = Mathf.Abs(distance - targetDistance);
                    var surroundingPenalty = GetSurroundingPenalty(chunks, col, row);
                    var score = distancePenalty + hardnessPenalty + lateralPenalty + surroundingPenalty;

                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    checkpoint = new Vector2I(col, row);
                    found = true;
                }
            }

            if (found)
            {
                return true;
            }

            checkpoint = new Vector2I(anchor.X, Math.Max(Player.PlayerStats.SurfaceRow + 6, anchor.Y + CheckpointDownwardBiasMin));
            return chunks.IsInBounds(checkpoint.X, checkpoint.Y);
        }

        private static float GetSurroundingPenalty(ChunkManager chunks, int centerCol, int centerRow)
        {
            var penalty = 0f;
            for (var row = centerRow - 2; row <= centerRow + 2; row++)
            {
                for (var col = centerCol - 2; col <= centerCol + 2; col++)
                {
                    if (!chunks.IsInBounds(col, row))
                    {
                        penalty += 4f;
                        continue;
                    }

                    var type = (CellType)chunks.GetCell(col, row);
                    penalty += type switch
                    {
                        CellType.Bedrock => 3.2f,
                        CellType.HardRock => 0.95f,
                        CellType.Stone => 0.45f,
                        CellType.RecoveryPoint => 1.25f,
                        _ => 0f
                    };
                }
            }

            return penalty * 0.08f;
        }

        private bool IsPlayerTouchingCheckpoint(Vector2I playerCell, int playerSize)
        {
            if (!_hasActiveCheckpoint)
            {
                return false;
            }

            var half = playerSize / 2;
            return Mathf.Abs(_activeCheckpoint.X - playerCell.X) <= half && Mathf.Abs(_activeCheckpoint.Y - playerCell.Y) <= half;
        }

        private string BuildCheckpointNotification(bool milestoneReached)
        {
            if (CurrentChainCount <= 1)
            {
                return "下降チェイン開始 / 次の目標は 5 秒以内";
            }

            if (milestoneReached)
            {
                return $"{CurrentChainCount} チェイン到達 / 節目ボーナス x{CarryValueMultiplier:0.000}";
            }

            if (CurrentChainCount % 10 == 0)
            {
                return $"{CurrentChainCount} チェイン / 一時ブースト発動";
            }

            if (CurrentChainCount % 5 == 0)
            {
                return $"{CurrentChainCount} チェイン / 潜行猶予 +{EveryFiveBonusSeconds:0.#} 秒";
            }

            return $"{CurrentChainCount} チェイン / 換金 x{CarryValueMultiplier:0.000}";
        }

        private void SyncStats(Player.PlayerStats stats)
        {
            stats.UpdateChainState(
                CurrentChainCount,
                BestChainCount,
                CarryValueMultiplier,
                CurrentMoveSpeedMultiplier,
                CurrentDigSpeedMultiplier,
                CurrentSonarBonusRadius,
                ActiveBoostTimeRemaining);
        }
    }
}