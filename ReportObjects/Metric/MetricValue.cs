using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MetricValue
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long EntityID { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }

        public string MetricName { get; set; }
        public long MetricID { get; set; }

        public DateTime EventTime { get; set; }
        public DateTime EventTimeStamp { get; set; }
        public DateTime EventTimeStampUtc { get; set; }

        public long Count { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Occurrences { get; set; }
        public long Sum { get; set; }
        public long Value { get; set; }

        public int MetricResolution { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricValue: Controller={0}, ApplicationName={1}, EntityName={2:o}, MetricName={3}, EventTimeStamp={4:o}, Value={5}",
                this.Controller,
                this.ApplicationName,
                this.EntityName,
                this.MetricName,
                this.EventTimeStamp,
                this.Value);
        }
    }
}