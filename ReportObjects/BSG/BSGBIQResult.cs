using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGBIQResult
    {
        public string Controller { get; set; }
        
        public string ApplicationName { get; set; }

        public int NumAnalyticSearches { get; set; }
        public int NumAnalyticMetrics { get; set; }
        public int NumBusinessJourneys { get; set; }

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
                "BSGBIQResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName,
                this.NumAnalyticSearches);
        }
    }
}