using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DetectedError
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
        public long SegmentID { get; set; }

        public string SegmentUserExperience { get; set; }
        public string SnapshotUserExperience { get; set; }

        public long ErrorID { get; set; }
        public string ErrorName { get; set; }
        public string ErrorType { get; set; }

        public bool ErrorIDMatchedToMessage { get; set; }

        public string ErrorCategory { get; set; }
        public string ErrorDetail { get; set; }

        public string ErrorMessage { get; set; }
        public string ErrorStack { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DetectedError: {0}:{1} {2}/{3}",
                this.RequestID,
                this.SegmentID,
                this.ErrorName,
                this.ErrorCategory);
        }
    }
}
