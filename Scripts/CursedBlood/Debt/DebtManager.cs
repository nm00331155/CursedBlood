using System.Collections.Generic;
using CursedBlood.Core;

namespace CursedBlood.Debt
{
    public enum DebtPaymentKind
    {
        Full,
        Half,
        Minimum,
        None
    }

    public sealed class DebtPaymentOption
    {
        public DebtPaymentKind Kind { get; set; }

        public string Label { get; set; } = string.Empty;

        public long Amount { get; set; }

        public bool IsAvailable { get; set; }
    }

    public sealed class DebtManager
    {
        private const string SavePath = "user://debt.json";

        public long TotalDebt { get; set; } = 100000;

        public long PaidTotal { get; set; }

        public float InterestRate { get; set; } = 0.10f;

        public bool LiberationBonusActive { get; set; }

        public long RemainingDebt => System.Math.Max(0, TotalDebt - PaidTotal);

        public bool IsCleared => RemainingDebt <= 0;

        public static DebtManager Load()
        {
            return JsonStorage.Load(SavePath, () => new DebtManager());
        }

        public void Save()
        {
            JsonStorage.Save(SavePath, this);
        }

        public void ApplyInterest()
        {
            if (IsCleared)
            {
                LiberationBonusActive = true;
                return;
            }

            TotalDebt += (long)System.MathF.Ceiling(RemainingDebt * InterestRate);
        }

        public void Repay(long amount)
        {
            if (amount <= 0)
            {
                return;
            }

            PaidTotal += amount;
            if (IsCleared)
            {
                LiberationBonusActive = true;
            }
        }

        public long GetMinimumPayment()
        {
            return (long)System.MathF.Ceiling(RemainingDebt * InterestRate);
        }

        public float GetCollectorSpawnRate()
        {
            if (IsCleared)
            {
                return 0f;
            }

            var ratio = RemainingDebt / (float)System.Math.Max(1L, TotalDebt);
            return 1f + ratio * 2f;
        }

        public IReadOnlyList<DebtPaymentOption> GetPaymentOptions(long availableGold)
        {
            var full = RemainingDebt;
            var half = RemainingDebt / 2;
            var minimum = GetMinimumPayment();

            return new List<DebtPaymentOption>
            {
                new DebtPaymentOption { Kind = DebtPaymentKind.Full, Label = $"全額返済 {full:N0}G", Amount = full, IsAvailable = availableGold >= full },
                new DebtPaymentOption { Kind = DebtPaymentKind.Half, Label = $"半額返済 {half:N0}G", Amount = half, IsAvailable = availableGold >= half },
                new DebtPaymentOption { Kind = DebtPaymentKind.Minimum, Label = $"最低返済 {minimum:N0}G", Amount = minimum, IsAvailable = availableGold >= minimum },
                new DebtPaymentOption { Kind = DebtPaymentKind.None, Label = "返済しない 0G", Amount = 0L, IsAvailable = true }
            };
        }
    }
}