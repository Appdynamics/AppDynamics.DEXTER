using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGErrorsResultMap : ClassMap<BSGErrorsResult>
    {
        public BSGErrorsResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.MaxErrorRate).Index(i); i++;
            Map(m => m.NumDetectionRules).Index(i); i++;
                        
        }
    }
}
