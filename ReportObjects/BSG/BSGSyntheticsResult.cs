using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGSyntheticsResult
    {
        public string Controller { get; set; }

        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }

        public int NumSyntheticJobs { get; set; }
        public int NumSyntheticJobsWithData { get; set; }

        public int NumHRsWithSynthetics { get; set; }
        public int NumPoliciesForHRs { get; set; }
        public int NumActionsForPolicies { get; set; }

        public int NumWarningHRViolations { get; set; }
        public int NumCriticalHRViolations { get; set; }


        public BSGDataCollectorResult Clone()
        {
            return (BSGDataCollectorResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGSynthetics:  {0}/{1}({2})/{3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.NumSyntheticJobs);
        }
    }
}