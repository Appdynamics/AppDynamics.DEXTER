namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportTierRow
    {
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public string AgentType { get; set; }
        public string Controller { get; set; }
        public int NumNodes { get; set; }
        public int TierID { get; set; }
        public string TierName { get; set; }
        public string TierType { get; set; }
    }
}
