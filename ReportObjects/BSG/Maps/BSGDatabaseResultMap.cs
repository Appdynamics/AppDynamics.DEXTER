using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGDatabaseResultMap : ClassMap<BSGDatabaseResult>
    {
        public BSGDatabaseResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.NumDataCollectors).Index(i); i++;
            Map(m => m.NumCustomMetrics).Index(i); i++;

            Map(m => m.NumHRs).Index(i); i++;
            Map(m => m.NumPoliciesForHRs).Index(i); i++;
            Map(m => m.NumActionsForPolicies).Index(i); i++;
            Map(m => m.NumWarningHRViolations).Index(i); i++;
            Map(m => m.NumCriticalHRViolations).Index(i); i++;            
        }
    }
}
