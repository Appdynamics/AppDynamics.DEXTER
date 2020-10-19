using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGBackendCustomizationResultMap : ClassMap<BSGBackendCustomizationResult>
    {
        public BSGBackendCustomizationResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.CustomDiscoveryRules).Index(i); i++;
            Map(m => m.CustomExitPoints).Index(i); i++;
        }
    }
}
