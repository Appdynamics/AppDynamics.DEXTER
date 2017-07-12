namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportApplicationRow
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationID { get; set; }
        public int NumBackends { get; set; }
        public int NumBTs { get; set; }
        public int NumErrors { get; set; }
        public int NumHTTPDCs { get; set; }
        public int NumMIDCs { get; set; }
        public int NumNodes { get; set; }
        public int NumSEPs { get; set; }
        public int NumTiers { get; set; }
    }
}
