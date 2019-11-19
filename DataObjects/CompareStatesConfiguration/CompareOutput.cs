namespace AppDynamics.Dexter
{
    public class CompareOutput
    {
        public bool DetectedEntities { get; set; }
        public bool EntityMetrics { get; set; }
        public bool EntityMetricGraphs { get; set; }
        public bool Snapshots { get; set; }
        public bool Flowmaps { get; set; }
        public bool FlameGraphs { get; set; }
        public bool Configuration { get; set; }
    }
}
