using CsvHelper.Configuration;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportControllerRowMap: CsvClassMap<ReportControllerRow>
    {
        public ReportControllerRowMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.NumApps).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
        }
    }
}
