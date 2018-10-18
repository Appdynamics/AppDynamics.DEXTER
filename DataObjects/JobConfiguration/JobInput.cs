using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public class JobInput
    {
        public JobTimeRange TimeRange { get; set; }
        public List<JobTimeRange> HourlyTimeRanges { get; set; }
        public JobSnapshotSelectionCriteria SnapshotSelectionCriteria { get; set; }
        public bool Flowmaps { get; set; }
        public bool Metrics { get; set; }
        public string[] MetricsSelectionCriteria { get; set; }
        public bool Snapshots { get; set; }
        public bool Configuration { get; set; }
        public JobTarget ConfigurationComparisonReferenceCriteria { get; set; }
        public bool Events { get; set; }
        public bool UsersGroupsRolesPermissions { get; set; }
    }
}
