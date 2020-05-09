namespace AppDynamics.Dexter
{
    public class JobEntityDashboardSelectionCriteria
    {
        public string[] Tiers { get; set; }
        public string[] TierTypes { get; set; }
        public string[] Nodes { get; set; }
        public string[] NodeTypes { get; set; }
        public string[] BusinessTransactions { get; set; }
        public string[] BusinessTransactionTypes { get; set; }
        public string[] Backends { get; set; }
        public string[] BackendTypes { get; set; }
    }
}
