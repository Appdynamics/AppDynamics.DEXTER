using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class AuditEvent
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long EntityID { get; set; }
        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public string Action { get; set; }

        public string UserName { get; set; }
        public string AccountName { get; set; }
        public string LoginType { get; set; }

        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AuditEvent: {0} did {1} to {2}({3}) on {4:o}",
                this.UserName,
                this.Action,
                this.EntityName,
                this.EntityType,
                this.Occurred);
        }
    }
}
