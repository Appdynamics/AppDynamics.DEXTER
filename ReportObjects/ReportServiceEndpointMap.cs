using CsvHelper.Configuration;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportServiceEndpointRowMap : CsvClassMap<ReportServiceEndpointRow>
    {
        public ReportServiceEndpointRowMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.SEPName).Index(i); i++;
            Map(m => m.SEPType).Index(i); i++;
            Map(m => m.SEPID).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
        }
    }
}
