using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public class JobInput
    {
        public JobTimeRange TimeRange { get; set; }
        public List<JobTimeRange> HourlyTimeRanges { get; set; }
        public JobSnapshotSelectionCriteria SnapshotSelectionCriteria { get; set; }

        public bool DetectedEntities { get; set; }
        public bool Flowmaps { get; set; }
        public bool Metrics { get; set; }
        public string[] MetricsSelectionCriteria { get; set; }
        public bool Snapshots { get; set; }
        public bool Configuration { get; set; }
        public JobTarget ConfigurationComparisonReferenceAPM { get; set; }
        public JobTarget ConfigurationComparisonReferenceWEB { get; set; }
        public JobTarget ConfigurationComparisonReferenceMOBILE { get; set; }
        public JobTarget ConfigurationComparisonReferenceDB { get; set; }
        public bool Events { get; set; }
        public bool UsersGroupsRolesPermissions { get; set; }
        public bool Dashboards { get; set; }
        public bool Licenses { get; set; }
    }
}
