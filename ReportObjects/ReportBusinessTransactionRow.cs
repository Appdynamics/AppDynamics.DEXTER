namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportBusinessTransactionRow
    {
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public int BTID { get; set; }
        public string BTLink { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public int TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }
    }
}
