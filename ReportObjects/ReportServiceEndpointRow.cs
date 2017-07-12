namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportServiceEndpointRow
    {
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public int SEPID { get; set; }
        public string SEPLink { get; set; }
        public string SEPName { get; set; }
        public string SEPType { get; set; }
        public int TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }
    }
}
