namespace AppDynamics.Dexter
{
    public class JobEntityDashboardSelectionCriteria
    {
        public string[] Tiers { get; set; }
        public JobTierType TierType { get; set; }
        public string[] Nodes { get; set; }
        public JobTierType NodeType { get; set; }
        public string[] BusinessTransactions { get; set; }
        public JobBusinessTransactionType BusinessTransactionType { get; set; }
        public string[] Backends { get; set; }
        public JobBackendType BackendType { get; set; }
    }
}
