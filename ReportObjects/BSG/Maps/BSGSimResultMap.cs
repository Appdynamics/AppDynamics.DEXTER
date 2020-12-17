using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGSimResultMap : ClassMap<BSGSimResult>
    {
        public BSGSimResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.NumHRs).Index(i); i++;
            Map(m => m.NumPolicies).Index(i); i++;
            Map(m => m.NumActions).Index(i); i++;
            Map(m => m.NumWarningViolations).Index(i); i++;
            Map(m => m.NumCriticalViolations).Index(i); i++;
        }
    }
}
