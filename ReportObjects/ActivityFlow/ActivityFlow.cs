using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ActivityFlow
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

        public long BTID { get; set; }
        public string BTLink { get; set; }
        public string BTName { get; set; }

        public long BackendID { get; set; }
        public string BackendLink { get; set; }
        public string BackendName { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public string CallDirection { get; set; }
        public string CallType { get; set; }

        public string FromName { get; set; }
        public string FromType { get; set; }
        public long FromEntityID { get; set; }
        public string FromLink { get; set; }

        public string ToName { get; set; }
        public string ToType { get; set; }
        public long ToEntityID { get; set; }
        public string ToLink { get; set; }

        public long ART { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }
        public long Errors { get; set; }
        public long EPM { get; set; }
        public double ErrorsPercentage { get; set; }

        public string MetricLink { get; set; }
        public List<long> MetricsIDs { get; set; }

        public ActivityFlow Clone()
        {
            return (ActivityFlow)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "ActivityFlow: {0}({1})->{2}({3}) {4}",
                this.FromName,
                this.FromType,
                this.ToName,
                this.ToType,
                this.CallType);
        }
    }
}
