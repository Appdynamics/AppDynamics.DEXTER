namespace AppDynamics.Dexter
{
    public class JobOutput
    {
        public bool DetectedEntities { get; set; }
        public bool EntityMetrics { get; set; }
        public bool EntityMetricGraphs { get; set; }
        public bool EntityDetails { get; set; }
        public bool EntityDashboards { get; set; }
        public bool Snapshots { get; set; }
        public bool IndividualSnapshots { get; set; }
        public bool Flowmaps { get; set; }
        public bool FlameGraphs { get; set; }
        public bool Configuration { get; set; }
        public bool Events { get; set; }
        public bool UsersGroupsRolesPermissions { get; set; }
        public bool Dashboards { get; set; }
        public bool Licenses { get; set; }
        public bool HealthCheck { get; set; }
        public bool ApplicationSummary { get; set; }
        public bool MetricsList { get; set; }
    }
}
