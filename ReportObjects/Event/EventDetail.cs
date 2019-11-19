using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EventDetail
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }

        public long EventID { get; set; }
        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }
        public string Summary { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Severity { get; set; }

        public string DetailAction { get; set; }
        public string DetailName { get; set; }
        public string DetailValue { get; set; }
        public string DetailValueOld { get; set; }
        public string DataType { get; set; }

        public override String ToString()
        {
            return String.Format(
                "EventDetails: {0}: {1}={2} ({3}) {4:o}",
                this.EventID,
                this.DetailName,
                this.DetailValue,
                this.DataType,
                this.Occurred);
        }
    }
}
