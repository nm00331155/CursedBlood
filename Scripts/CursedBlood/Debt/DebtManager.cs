using System;
using System.Collections.Generic;
using CursedBlood.Save;

namespace CursedBlood.Debt
{
    public sealed class DebtRepaymentOption
    {
        public DebtRepaymentChoice Choice { get; init; }

        public string Label { get; init; } = string.Empty;

        public long PaymentAmount { get; init; }

        public long DebtAfterPayment { get; init; }

        public long MoneyAfterPayment { get; init; }

        public bool IsEnabled { get; init; }

        public string Description { get; init; } = string.Empty;
    }

    public sealed class DebtSettlementPreview
    {
        public long DebtBeforeInterest { get; init; }

        public long InterestAmount { get; init; }

        public long DebtAfterInterest { get; init; }

        public long MoneyBeforePayment { get; init; }

        public IReadOnlyList<DebtRepaymentOption> Options { get; init; } = Array.Empty<DebtRepaymentOption>();

        public string SummaryText { get; init; } = string.Empty;
    }

    public sealed class DebtSettlementResult
    {
        public DebtRepaymentChoice Choice { get; init; }

        public long InterestAmount { get; init; }

        public long PaymentAmount { get; init; }

        public long DebtBefore { get; init; }

        public long DebtAfter { get; init; }

        public long MoneyBefore { get; init; }

        public long MoneyAfter { get; init; }

        public bool ClearedDebt { get; init; }

        public string BuildSummaryText()
        {
            return $"精算完了: 利息 {InterestAmount:N0} / 返済 {PaymentAmount:N0} / 残借金 {DebtAfter:N0} / 所持金 {MoneyAfter:N0}";
        }
    }

    public sealed class DebtManager
    {
        public DebtSettlementPreview BuildPreview(long currentMoney, long currentDebt)
        {
            var interest = CalculateInterest(currentDebt);
            var debtAfterInterest = currentDebt + interest;
            var options = new List<DebtRepaymentOption>(4)
            {
                CreateOption(DebtRepaymentChoice.Full, "全額返済", debtAfterInterest, currentMoney, debtAfterInterest),
                CreateOption(DebtRepaymentChoice.Half, "半額返済", CalculateHalfPayment(debtAfterInterest), currentMoney, debtAfterInterest),
                CreateOption(DebtRepaymentChoice.Minimum, "最低返済", CalculateMinimumPayment(debtAfterInterest), currentMoney, debtAfterInterest),
                CreateOption(DebtRepaymentChoice.None, "返済しない", 0L, currentMoney, debtAfterInterest)
            };

            var summary = debtAfterInterest <= 0L
                ? "借金は完済済みです。今回は精算のみ行います。"
                : $"今回の利息: {interest:N0} / 精算前借金: {debtAfterInterest:N0} / 返済後の残高を選択してください。";

            return new DebtSettlementPreview
            {
                DebtBeforeInterest = currentDebt,
                InterestAmount = interest,
                DebtAfterInterest = debtAfterInterest,
                MoneyBeforePayment = currentMoney,
                Options = options,
                SummaryText = summary
            };
        }

        public DebtSettlementResult ApplyRepayment(SaveData data, DebtRepaymentChoice choice)
        {
            var preview = BuildPreview(data.PlayerProfile.CurrentMoney, data.Debt.CurrentDebt);
            var option = FindOption(preview, choice);
            var paymentAmount = option.IsEnabled ? option.PaymentAmount : 0L;
            var newMoney = preview.MoneyBeforePayment - paymentAmount;
            var newDebt = Math.Max(0L, preview.DebtAfterInterest - paymentAmount);

            data.PlayerProfile.CurrentMoney = newMoney;
            data.Debt.CurrentDebt = newDebt;
            data.Debt.TotalInterestPaid += preview.InterestAmount;
            data.Debt.TotalRepaid += paymentAmount;
            data.Debt.DebtCleared = newDebt <= 0L;

            return new DebtSettlementResult
            {
                Choice = choice,
                InterestAmount = preview.InterestAmount,
                PaymentAmount = paymentAmount,
                DebtBefore = preview.DebtBeforeInterest,
                DebtAfter = newDebt,
                MoneyBefore = preview.MoneyBeforePayment,
                MoneyAfter = newMoney,
                ClearedDebt = newDebt <= 0L
            };
        }

        private static DebtRepaymentOption FindOption(DebtSettlementPreview preview, DebtRepaymentChoice choice)
        {
            for (var index = 0; index < preview.Options.Count; index++)
            {
                if (preview.Options[index].Choice == choice)
                {
                    return preview.Options[index];
                }
            }

            return preview.Options[^1];
        }

        private static DebtRepaymentOption CreateOption(DebtRepaymentChoice choice, string label, long paymentAmount, long currentMoney, long debtAfterInterest)
        {
            var normalizedPayment = Math.Clamp(paymentAmount, 0L, debtAfterInterest);
            var isEnabled = choice == DebtRepaymentChoice.None || (normalizedPayment > 0L && normalizedPayment <= currentMoney);
            var debtAfterPayment = Math.Max(0L, debtAfterInterest - (isEnabled ? normalizedPayment : 0L));
            var moneyAfterPayment = currentMoney - (isEnabled ? normalizedPayment : 0L);
            var description = isEnabled
                ? $"支払 {normalizedPayment:N0} / 残借金 {debtAfterPayment:N0} / 所持金 {moneyAfterPayment:N0}"
                : $"支払 {normalizedPayment:N0} / 所持金不足";

            return new DebtRepaymentOption
            {
                Choice = choice,
                Label = label,
                PaymentAmount = normalizedPayment,
                DebtAfterPayment = debtAfterPayment,
                MoneyAfterPayment = moneyAfterPayment,
                IsEnabled = isEnabled,
                Description = description
            };
        }

        private static long CalculateInterest(long currentDebt)
        {
            if (currentDebt <= 0L)
            {
                return 0L;
            }

            return Math.Max(DebtTerms.MinimumInterestCharge, (long)Math.Round(currentDebt * DebtTerms.InterestRatePerDive));
        }

        private static long CalculateHalfPayment(long debtAfterInterest)
        {
            if (debtAfterInterest <= 0L)
            {
                return 0L;
            }

            return (long)Math.Ceiling(debtAfterInterest * DebtTerms.HalfRepaymentRate);
        }

        private static long CalculateMinimumPayment(long debtAfterInterest)
        {
            if (debtAfterInterest <= 0L)
            {
                return 0L;
            }

            return Math.Min(debtAfterInterest, Math.Max(DebtTerms.MinimumRepayment, (long)Math.Ceiling(debtAfterInterest * DebtTerms.MinimumRepaymentRate)));
        }
    }
}