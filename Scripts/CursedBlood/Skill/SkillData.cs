namespace CursedBlood.Skill
{
    public enum SkillType
    {
        LinearPierce,
        AreaBreak3x3,
        InvincibleDash,
        ScreenAttack
    }

    public sealed class SkillData
    {
        public SkillType Type { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public float GaugeCost { get; init; } = 100f;

        public static SkillData FromType(SkillType type)
        {
            return type switch
            {
                SkillType.LinearPierce => new SkillData
                {
                    Type = type,
                    Name = "貫通掘削",
                    Description = "前方5マスを一直線に貫通する。"
                },
                SkillType.AreaBreak3x3 => new SkillData
                {
                    Type = type,
                    Name = "範囲破壊",
                    Description = "周囲3x3を一気に掘り崩す。"
                },
                SkillType.InvincibleDash => new SkillData
                {
                    Type = type,
                    Name = "無敵ダッシュ",
                    Description = "短時間無敵になり移動が加速する。"
                },
                SkillType.ScreenAttack => new SkillData
                {
                    Type = type,
                    Name = "全画面攻撃",
                    Description = "画面内の敵をまとめて攻撃する。"
                },
                _ => new SkillData
                {
                    Type = SkillType.AreaBreak3x3,
                    Name = "範囲破壊",
                    Description = "周囲3x3を一気に掘り崩す。"
                }
            };
        }
    }
}