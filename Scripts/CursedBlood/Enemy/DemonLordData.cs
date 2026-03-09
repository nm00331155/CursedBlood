using Godot;

namespace CursedBlood.Enemy
{
    public sealed class DemonLordData
    {
        public int MaxHp { get; set; } = 999999;

        public int CurrentHp { get; set; } = 999999;

        public Vector2I CenterPosition { get; set; } = new(3, 9999);

        public BossPhase CurrentPhase => CurrentHp switch
        {
            > 699999 => BossPhase.Phase1,
            > 399999 => BossPhase.Phase2,
            > 99999 => BossPhase.Phase3,
            _ => BossPhase.Phase4
        };
    }
}