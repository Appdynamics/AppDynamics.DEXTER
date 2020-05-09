namespace AppDynamics.Dexter
{
    public class JobSnapshotSelectionCriteria
    {
        public string[] Tiers { get; set; }
        public string[] TierTypes { get; set; }
        public string[] BusinessTransactions { get; set; }
        public string[] BusinessTransactionTypes { get; set; }
        public JobUserExperience UserExperience { get; set; }
        public JobSnapshotType SnapshotType { get; set; }
    }
}
