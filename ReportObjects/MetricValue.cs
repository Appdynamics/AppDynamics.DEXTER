using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class MetricValue
    {
        public MetricResolution MetricResolution { get; set; }
        public int MetricID { get; set; }

        public DateTime EventTime { get; set; }
        public DateTime EventTimeUtc { get; set; }

        public long Count { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Occurences { get; set; }
        public long Sum { get; set; }
        public long Value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricValue: value={0}, count={1}, EventTime={2:o}, startTime={3:o}",
                this.Value,
                this.Count,
                this.EventTime,
                this.EventTimeUtc);
        }
    }
}