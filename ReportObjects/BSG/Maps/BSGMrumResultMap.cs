using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGMrumResultMap : ClassMap<BSGMrumResult>
    {
        public BSGMrumResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.NumNetworkrequests).Index(i); i++;
            // Map(m => m.NetworkRequestLimitReached).Index(i); i++;
            Map(m => m.NumCustomNetworkRequestRules).Index(i); i++;
            Map(m => m.MrumHealthRules).Index(i); i++;
            Map(m => m.LinkedPolicies).Index(i); i++;
            Map(m => m.LinkedActions).Index(i); i++;
            Map(m => m.WarningViolations).Index(i); i++;
            Map(m => m.CriticalViolations).Index(i); i++;
        }
    }
}
