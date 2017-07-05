using System.Collections.Generic;

namespace AppDynamics.OfflineData.JobParameters
{
    public class JobConfiguration
    {
        public JobInput Input { get; set; }
        public JobOutput Output { get; set; }
        public List<JobTarget> Target { get; set; }
        public JobStatus Status { get; set; }
    }
}
