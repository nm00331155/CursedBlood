using Godot;

namespace CursedBlood.Core
{
    public enum DiveEndReason
    {
        SurfaceReturn,
        RecoveryPointReturn,
        RescueTimeout,
        RescueDowned
    }

    public sealed class DiveResultData
    {
        public int DiveCount { get; init; }

        public int MaxDepthMeters { get; init; }

        public DiveEndReason EndReason { get; init; }

        public string RescueReason { get; init; } = string.Empty;

        public bool ReturnedSafely { get; init; }

        public bool Rescued { get; init; }

        public long SalvageValue { get; init; }

        public long CarryValue { get; init; }

        public long LostValue { get; init; }

        public long RescueCost { get; init; }

        public long DebtBefore { get; init; }

        public long DebtAfter { get; init; }

        public long MoneyBefore { get; init; }

        public long MoneyAfter { get; init; }

        public long Score { get; init; }

        public int OresCollected { get; init; }

        public int BlocksDug { get; init; }

        public long DebtChange => DebtAfter - DebtBefore;

        public string OutcomeLabel => EndReason switch
        {
            DiveEndReason.SurfaceReturn => "地上帰還成功",
            DiveEndReason.RecoveryPointReturn => "回収ポイント帰還成功",
            DiveEndReason.RescueTimeout => "救助: 時間切れ",
            DiveEndReason.RescueDowned => "救助: 行動不能",
            _ => "潜行終了"
        };

        public string BuildSummaryText()
        {
            var debtChangeLabel = DebtChange switch
            {
                > 0L => $"+{DebtChange:N0}",
                < 0L => $"-{Mathf.Abs(DebtChange):N0}",
                _ => "0"
            };

            var rescueLine = RescueCost > 0L
                ? $"回収費: {RescueCost:N0}\nロスト額: {LostValue:N0}\n"
                : string.Empty;

            var rescueReasonLine = string.IsNullOrEmpty(RescueReason)
                ? string.Empty
                : $"理由: {RescueReason}\n";

            return
                $"潜行回数: {DiveCount}\n" +
                $"到達深度: {MaxDepthMeters}m\n" +
                $"未換金資源: {SalvageValue:N0}\n" +
                $"持ち帰り額: {CarryValue:N0}\n" +
                rescueLine +
                rescueReasonLine +
                $"鉱石回収: {OresCollected} / 掘削: {BlocksDug}\n" +
                $"借金変動: {debtChangeLabel}\n" +
                $"借金残高: {DebtAfter:N0}\n" +
                $"所持金: {MoneyAfter:N0}\n" +
                $"スコア: {Score:N0}";
        }
    }
}