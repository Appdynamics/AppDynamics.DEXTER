using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class ServiceEndpointCall
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

        public string RequestID { get; set; }
        public long SegmentID { get; set; }

        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }

        public long SEPID { get; set; }
        public string SEPLink { get; set; }
        public string SEPName { get; set; }
        public string SEPType { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ServiceEndpointCall: {0}:{1}->{2}",
                this.RequestID,
                this.SegmentID,
                this.SEPName);
        }
    }
}
