using System;

namespace AppDynamics.Dexter
{
    public class JobSnapshotSelectionCriteria
    {
        public string[] Tiers { get; set; }
        public JobTierType TierType { get; set; }
        public string[] BusinessTransactions { get; set; }
        public JobBusinessTransactionType BusinessTransactionType { get; set; }
        public JobUserExperience UserExperience { get; set; }
        public JobSnapshotType SnapshotType { get; set; }
    }
}
