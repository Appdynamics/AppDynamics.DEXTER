namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportBackendRow
    {
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public int BackendID { get; set; }
        public string BackendLink { get; set; }
        public string BackendName { get; set; }
        public string BackendType { get; set; }
        public int NodeID { get; set; }
        public string NodeName { get; set; }
        public int NumProps { get; set; }
        public int TierID { get; set; }
        public string TierName { get; set; }
        public string Prop1Name { get; set; }
        public string Prop1Value { get; set; }
        public string Prop2Name { get; set; }
        public string Prop2Value { get; set; }
        public string Prop3Name { get; set; }
        public string Prop3Value { get; set; }
        public string Prop4Name { get; set; }
        public string Prop4Value { get; set; }
    }
}
