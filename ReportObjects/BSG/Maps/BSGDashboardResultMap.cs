using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGDashboardResultMap : ClassMap<BSGDashboardResult>
    {
        public BSGDashboardResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.DashboardName).Index(i); i++;
            Map(m => m.LastModifiedOn).Index(i); i++;

            Map(m => m.LastModifiedOnUtc).Index(i); i++;
            Map(m => m.DaysSince_LastUpdate).Index(i); i++;
            Map(m => m.HasAnalyticsWidgets).Index(i); i++;
            Map(m => m.NumWidgets).Index(i); i++;
            Map(m => m.NumAnalyticsWidgets).Index(i); i++;
        }
    }
}
