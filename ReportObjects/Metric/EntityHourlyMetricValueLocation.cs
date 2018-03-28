using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityHourlyMetricValueLocation
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long EntityID { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }

        public string MetricName { get; set; }
        public long MetricID { get; set; }

        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public int RowStart { get; set; }
        public int RowEnd { get; set; }

        public override String ToString()
        {
            return String.Format(
                "EntityHourlyMetricValueLocation: Controller={0}, ApplicationName={1}, EntityName={2:o}, MetricName={3}, From={4:o}, RowStart={5}, RowEnd={6}",
                this.Controller,
                this.ApplicationName,
                this.EntityName,
                this.MetricName,
                this.FromUtc,
                this.RowStart,
                this.RowEnd);
        }
    }
}