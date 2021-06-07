using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGDatabaseResult
    {
        public string Controller { get; set; }
        
        public string ApplicationName { get; set; }

        public int NumDataCollectors { get; set; }
        public int NumCustomMetrics { get; set; }
        public int NumHRs { get; set; }
        public int NumPoliciesForHRs { get; set; }
        public int NumActionsForPolicies { get; set; }

        public int NumWarningHRViolations { get; set; }
        public int NumCriticalHRViolations { get; set; }


        public BSGDatabaseResult Clone()
        {
            return (BSGDatabaseResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGDatabaseResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName,
                this.NumDataCollectors);
        }
    }
}