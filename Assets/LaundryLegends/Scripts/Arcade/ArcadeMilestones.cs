namespace LaundryLegends.Arcade
{
    /// <summary>Stable ids and player-facing text for the Phase 9 milestones.</summary>
    public static class ArcadeMilestones
    {
        public const string FirstMachine = "first-machine";
        public const string SecondMachine = "second-machine";
        public const string Income250 = "income-250";
        public const string Reputation50 = "reputation-50";
        public const string Expanded = "expanded";

        /// <summary>Milestones in the order they are shown to the player.</summary>
        public static readonly string[] Ordered =
        {
            FirstMachine,
            SecondMachine,
            Income250,
            Reputation50,
            Expanded
        };

        public static string Describe(string id)
        {
            switch (id)
            {
                case FirstMachine:
                    return "Power on the first machine";
                case SecondMachine:
                    return "Install a second machine";
                case Income250:
                    return "Earn $250 from the arcade";
                case Reputation50:
                    return "Reach 50 laundry reputation";
                case Expanded:
                    return "Open the expansion bay";
                default:
                    return id;
            }
        }

        /// <summary>The next unearned milestone, or null once all are earned.</summary>
        public static string Next(System.Collections.Generic.IReadOnlyList<string> earned)
        {
            foreach (string id in Ordered)
            {
                bool has = false;
                if (earned != null)
                {
                    for (int i = 0; i < earned.Count; i++)
                    {
                        if (earned[i] == id)
                        {
                            has = true;
                            break;
                        }
                    }
                }

                if (!has)
                {
                    return id;
                }
            }

            return null;
        }
    }
}
