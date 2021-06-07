using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGContainersResult
    {
        public string Controller { get; set; }
        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }
        public int NumContainersMonitored { get; set; }
        // not in DEXTER
        // public bool ClusterAgentReportingData { get; set; }
        // not in DEXTER
        // public bool APMAutoInstrumentationEnabled { get; set; }
        public int NumHRs { get; set; }
        public int NumPolicies { get; set; }
        public int NumActions { get; set; }
        public int NumWarningViolations { get; set; }
        public int NumCriticalViolations { get; set; }
        

        public BSGContainersResult Clone()
        {
            return (BSGContainersResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGContainersResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}