namespace CursedBlood.Debt
{
    public static class DebtTerms
    {
        public const long MinimumRepayment = 1_000L;

        public const long MinimumInterestCharge = 500L;

        public const float InterestRatePerDive = 0.08f;

        public const float HalfRepaymentRate = 0.50f;

        public const float MinimumRepaymentRate = 0.10f;
    }

    public enum DebtRepaymentChoice
    {
        Full,
        Half,
        Minimum,
        None
    }
}