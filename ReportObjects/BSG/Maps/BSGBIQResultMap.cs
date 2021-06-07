using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGBIQResultMap : ClassMap<BSGBIQResult>
    {
        public BSGBIQResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.NumAnalyticSearches).Index(i); i++;
            Map(m => m.NumAnalyticMetrics).Index(i); i++;
            Map(m => m.NumBusinessJourneys).Index(i); i++;

            Map(m => m.NumHRs).Index(i); i++;
            Map(m => m.NumPoliciesForHRs).Index(i); i++;
            Map(m => m.NumActionsForPolicies).Index(i); i++;
            Map(m => m.NumWarningHRViolations).Index(i); i++;
            Map(m => m.NumCriticalHRViolations).Index(i); i++;            
        }
    }
}
