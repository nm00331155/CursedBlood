using System;
using CursedBlood.Core;
using CursedBlood.Enemy;
using CursedBlood.Effects;
using CursedBlood.Player;
using Godot;

namespace CursedBlood.Skill
{
    public partial class SkillManager : Node
    {
        public GridManager Grid { get; set; }

        public PlayerStats Stats { get; set; }

        public EnemyManager EnemyManager { get; set; }

        public BossController BossController { get; set; }

        public ScreenEffects ScreenEffects { get; set; }

        public ParticleManager ParticleManager { get; set; }

        public event Action<SkillType> SkillActivated;

        public bool TryActivate(Vector2I direction)
        {
            if (Stats == null || Grid == null || !Stats.ConsumeSkill())
            {
                return false;
            }

            var skillType = Stats.CurrentSkillType;
            switch (skillType)
            {
                case SkillType.LinearPierce:
                    SkillEffects.LinearPierce(Grid, Stats, EnemyManager, BossController, direction == Vector2I.Zero ? Vector2I.Down : direction);
                    break;
                case SkillType.AreaBreak3x3:
                    SkillEffects.AreaBreak(Grid, Stats, EnemyManager, BossController);
                    break;
                case SkillType.InvincibleDash:
                    Stats.ActivateDash(3f);
                    break;
                case SkillType.ScreenAttack:
                    SkillEffects.ScreenAttack(EnemyManager, BossController);
                    break;
            }

            ScreenEffects?.Flash(new Color(1f, 1f, 1f), 0.2f);
            ParticleManager?.SpawnBossExplosion(Grid.GridToWorld(Stats.GridPosition.X, Stats.GridPosition.Y));
            SkillActivated?.Invoke(skillType);
            return true;
        }
    }
}