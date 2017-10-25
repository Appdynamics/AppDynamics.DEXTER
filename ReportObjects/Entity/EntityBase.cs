using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityBase
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

        public string MetricLink { get; set; }
        public List<long> MetricsIDs { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public long ART { get; set; }
        public long TimeTotal { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }
        public long Errors { get; set; }
        public long EPM { get; set; }
        public long Exceptions { get; set; }
        public long EXCPM { get; set; }
        public long HttpErrors { get; set; }
        public long HTTPEPM { get; set; }
        public double ErrorsPercentage { get; set; }
        public bool HasActivity { get; set; }

        public string DetailLink { get; set; }
    }
}
