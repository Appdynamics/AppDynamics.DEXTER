namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportTierRow
    {
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public string AgentType { get; set; }
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public int NumNodes { get; set; }
        public int TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }
        public string TierType { get; set; }
    }
}
