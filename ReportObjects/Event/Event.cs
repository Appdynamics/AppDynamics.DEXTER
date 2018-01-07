using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class Event
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeLink { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public long BTID { get; set; }
        public string BTLink { get; set; }
        public string BTName { get; set; }

        public long TriggeredEntityID { get; set; }
        public string TriggeredEntityType { get; set; }
        public string TriggeredEntityName { get; set; }

        public long EventID { get; set; }
        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }
        public string Summary { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Severity { get; set; }
        public string EventLink { get; set; }

        public Event Clone()
        {
            return (Event)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "Event: {0}: {1}/{2} {3} {4:o}",
                this.EventID,
                this.Type,
                this.SubType,
                this.Severity,
                this.Occurred);
        }
    }
}
