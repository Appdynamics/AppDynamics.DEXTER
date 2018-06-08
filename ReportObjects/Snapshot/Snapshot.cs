using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class Snapshot
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }
        public string TierType { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }
        public string AgentType { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

        public DateTime Occurred { get; set; }
        public DateTime OccurredUtc { get; set; }

        public string RequestID { get; set; }
        public string UserExperience { get; set; }
        public long Duration { get; set; }
        public string DurationRange { get; set; }
        public long EndToEndDuration { get; set; }
        public bool IsEndToEndDurationDifferent { get; set; }
        public string EndToEndDurationRange { get; set; }
        public string DiagSessionID { get; set; }
        public string URL { get; set; }

        public string TakenSummary { get; set; }
        public string TakenReason { get; set; }

        public string CallGraphType { get; set; }
        public bool IsArchived { get; set; }

        public int NumSegments { get; set; }
        public int NumCallGraphs { get; set; }

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

        public string FlameGraphLink { get; set; }
        public string SnapshotLink { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Snapshot: {0}: {1}/{2}/{3}/{4}/{5:o}",
                this.RequestID,
                this.ApplicationName,
                this.BTName,
                this.TierName,
                this.NodeName,
                this.Occurred);
        }
    }
}
