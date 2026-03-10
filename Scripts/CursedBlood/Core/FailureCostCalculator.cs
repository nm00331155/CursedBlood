using System;

namespace CursedBlood.Core
{
    public readonly record struct FailureCostBreakdown(long BaseCost, long DepthCost, long InventoryCost, long ChainCost)
    {
        public long TotalCost => BaseCost + DepthCost + InventoryCost + ChainCost;

        public string BuildBreakdownText()
        {
            return $"基本 {BaseCost:N0} / 深度 {DepthCost:N0} / 資源 {InventoryCost:N0} / チェイン {ChainCost:N0}";
        }
    }

    public sealed class FailureCostCalculator
    {
        public long BaseCost { get; set; } = 900L;

        public float DepthMultiplier { get; set; } = 6.5f;

        public float InventoryMultiplier { get; set; } = 0.06f;

        public float ChainMultiplier { get; set; } = 55f;

        public FailureCostBreakdown Calculate(int maxDepthMeters, long inventoryValue, int currentChainCount)
        {
            var depthCost = (long)Math.Round(Math.Max(0, maxDepthMeters) * Math.Max(0f, DepthMultiplier));
            var inventoryCost = (long)Math.Round(Math.Max(0L, inventoryValue) * Math.Max(0f, InventoryMultiplier));
            var chainCost = (long)Math.Round(Math.Max(0, currentChainCount) * Math.Max(0f, ChainMultiplier));
            return new FailureCostBreakdown(Math.Max(0L, BaseCost), Math.Max(0L, depthCost), Math.Max(0L, inventoryCost), Math.Max(0L, chainCost));
        }
    }
}