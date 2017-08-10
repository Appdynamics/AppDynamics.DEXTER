using System.Collections.Generic;

namespace AppDynamics.OfflineData.JobParameters
{
    public class JobInput
    {
        public JobTimeRange TimeRange { get; set; }
        public JobTimeRange ExpandedTimeRange { get; set; }
        public List<JobTimeRange> HourlyTimeRanges { get; set; }
        public bool Flowmaps { get; set; }
        public bool Metrics { get; set; }
        public bool Snapshots { get; set; }
        public bool Configuration { get; set; }
    }
}
