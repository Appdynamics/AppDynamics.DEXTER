namespace AppDynamics.Dexter
{
    public class CompareInput
    {
        public CompareTimeRange TimeRange { get; set; }
        public bool DetectedEntities { get; set; }
        public bool Flowmaps { get; set; }
        public bool Metrics { get; set; }
        public bool Snapshots { get; set; }
        public bool Configuration { get; set; }
        public bool UsersGroupsRolesPermissions { get; set; }
        public bool Dashboards { get; set; }
        public bool Licenses { get; set; }
    }
}
