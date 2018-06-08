using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MetricExtractMapping
    {
        public string EntityType { get; set; }
        public string MetricPath { get; set; }
        public string MetricName { get; set; }
        public string FolderName { get; set; }
        public string RangeRollupType { get; set; }
        public string Graph { get; set; }
        public string Axis { get; set; }
        public string LineColor { get; set; }
        public string MetricSet { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricExtractMapping: EntityType={0}, FolderName={1}, MetricSet={2}",
                this.EntityType,
                this.FolderName,
                this.MetricSet);
        }
    }
}