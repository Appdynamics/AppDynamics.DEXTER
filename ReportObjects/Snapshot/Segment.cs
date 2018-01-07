using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class Segment
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

        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }

        public string Timeline { get; set; }
        public int TimelineResolution { get; set; }

        public string RequestID { get; set; }
        public long SegmentID { get; set; }
        public string UserExperience { get; set; }
        public long Duration { get; set; }
        public string DurationRange { get; set; }

        public long E2ELatency { get; set; }
        public double CPUDuration { get; set; }
        public long WaitDuration { get; set; }
        public long BlockDuration { get; set; }
        public string DiagSessionID { get; set; }
        public string URL { get; set; }
        public string ThreadID { get; set; }
        public string ThreadName { get; set; }
        public string UserPrincipal { get; set; }
        public string HTTPSessionID { get; set; }

        public string TakenSummary { get; set; }
        public string TakenReason { get; set; }
        public string TakenPolicy { get; set; }

        public string WarningThreshold { get; set; }
        public string CriticalThreshold { get; set; }

        public long FromSegmentID { get; set; }
        public string FromTierName { get; set; }

        public string CallGraphType { get; set; }
        public bool IsArchived { get; set; }
        public bool IsAsync { get; set; }
        public bool IsFirstInChain { get; set; }

        public int NumCalledBackends { get; set; }
        public int NumCalledTiers { get; set; }
        public int NumCalledApplications { get; set; }

        public int NumCallsToBackends { get; set; }
        public int NumCallsToTiers { get; set; }
        public int NumCallsToApplications { get; set; }

        public bool HasErrors { get; set; }
        public int NumErrors { get; set; }

        public int NumSEPs { get; set; }

        public int NumHTTPDCs { get; set; }
        public int NumMIDCs { get; set; }

        public string CallChains { get; set; }
        public string ExitTypes { get; set; }

        public string SegmentLink { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Segment: {0}:{1} {2}/{3}/{4}/{5}/{6:o}",
                this.RequestID,
                this.SegmentID,
                this.ApplicationName,
                this.BTName,
                this.TierName,
                this.NodeName,
                this.Occurred);
        }
    }
}
