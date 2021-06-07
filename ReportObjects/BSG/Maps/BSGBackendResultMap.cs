using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGBackendResultMap : ClassMap<BSGBackendResult>
    {
        public BSGBackendResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.BackendName).Index(i); i++;
            Map(m => m.BackendType).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
        }
    }
}
