using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class MetricValue
    {
        public MetricResolution MetricResolution { get; set; }
        public int MetricID { get; set; }

        public DateTime EventTime { get; set; }
        public DateTime EventTimeStamp { get; set; }
        public DateTime EventTimeStampUtc { get; set; }

        public long Count { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Occurences { get; set; }
        public long Sum { get; set; }
        public long Value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricValue: Value={0}, Count={1}, EventTime={2:o}, EventTimeUtc={3:o}",
                this.Value,
                this.Count,
                this.EventTimeStamp,
                this.EventTimeStampUtc);
        }
    }
}