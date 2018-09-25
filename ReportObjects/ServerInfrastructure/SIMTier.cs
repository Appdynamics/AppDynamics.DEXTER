using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMTier : SIMEntityBase
    {
        public long TierID { get; set; }
        public string TierName { get; set; }

        public int NumSegments { get; set; }

        public int NumNodes { get; set; }

        public override String ToString()
        {
            return String.Format(
                "SIMTier: {0}/{1}({2})/{3}({4})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID);
        }
    }
}
