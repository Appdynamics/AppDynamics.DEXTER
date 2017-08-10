namespace AppDynamics.OfflineData.JobParameters
{
    public class JobTarget
    {
        public string Controller { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string Application { get; set; }
        public int ApplicationID { get; set; }
        public bool NameRegex { get; set; }
        public JobTargetStatus Status { get; set; }
    }
}
