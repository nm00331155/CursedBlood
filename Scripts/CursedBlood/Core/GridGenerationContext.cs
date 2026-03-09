using CursedBlood.Config;

namespace CursedBlood.Core
{
    public sealed class GridGenerationContext
    {
        public BalanceConfig BalanceConfig { get; set; } = new();

        public float CollectorSpawnMultiplier { get; set; } = 0f;

        public bool EnableBosses { get; set; } = true;

        public bool EnableDemonLord { get; set; } = true;
    }
}