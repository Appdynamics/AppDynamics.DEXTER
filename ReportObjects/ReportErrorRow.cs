namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportErrorRow
    {
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public int ErrorDepth { get; set; }
        public string ErrorLevel1 { get; set; }
        public string ErrorLevel2 { get; set; }
        public string ErrorLevel3 { get; set; }
        public string ErrorLevel4 { get; set; }
        public string ErrorLevel5 { get; set; }
        public string ErrorLevel6 { get; set; }
        public string ErrorLink { get; set; }
        public string ErrorName { get; set; }
        public string ErrorType { get; set; }
        public int HttpCode { get; set; }
        public int TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }
    }
}
