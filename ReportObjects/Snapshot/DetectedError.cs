using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class DetectedError
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

        public long ErrorID { get; set; }
        public string ErrorLink { get; set; }
        public string ErrorName { get; set; }
        public string ErrorType { get; set; }

        public bool ErrorIDMatchedToMessage { get; set; }

        public string ErrorMessage { get; set; }
        public string ErrorDetail { get; set; }

        public string RequestID { get; set; }
        public long SegmentID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DetectedError: {0}:{1} {2}/{3}",
                this.RequestID,
                this.SegmentID,
                this.ErrorName,
                this.ErrorMessage);
        }
    }
}
