namespace LaundryLegends.Economy
{
    /// <summary>
    /// One day's fixed running cost: rent, utilities, and water. The charge is
    /// issued through <see cref="EconomyService.TrySpend"/> so it appears in the
    /// same auditable transaction log as every other balance change.
    /// The business may drop to 0 but never below it: when the balance cannot
    /// cover the charge nothing is deducted and nothing is logged.
    /// </summary>
    public static class DailyOverhead
    {
        /// <summary>Default daily charge in coins.</summary>
        public const int DefaultAmount = 15;

        /// <summary>Transaction reason recorded for every successful charge.</summary>
        public const string Reason = "Daily overhead";

        /// <summary>
        /// Charges <paramref name="amount"/> once for the day that starts at
        /// <paramref name="atSeconds"/>. Returns false and leaves both the
        /// balance and the transaction log untouched when the economy is
        /// missing, the amount is not positive, or the balance is too low.
        /// </summary>
        public static bool TryCharge(EconomyService economy, float atSeconds, int amount = DefaultAmount)
        {
            if (economy == null || amount <= 0)
            {
                return false;
            }

            return economy.TrySpend(amount, Reason, atSeconds);
        }
    }
}
