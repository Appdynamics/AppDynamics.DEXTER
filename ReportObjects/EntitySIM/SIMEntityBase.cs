namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMEntityBase
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
    }
}
