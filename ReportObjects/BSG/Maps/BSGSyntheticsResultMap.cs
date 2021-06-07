using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGSyntheticsResultMap : ClassMap<BSGSyntheticsResult>
    {
        public BSGSyntheticsResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.NumSyntheticJobs).Index(i); i++;
            Map(m => m.NumHRsWithSynthetics).Index(i); i++;
            Map(m => m.NumPoliciesForHRs).Index(i); i++;
            
            Map(m => m.NumActionsForPolicies).Index(i); i++;
            Map(m => m.NumWarningHRViolations).Index(i); i++;
            Map(m => m.NumCriticalHRViolations).Index(i); i++;            
        }
    }
}
