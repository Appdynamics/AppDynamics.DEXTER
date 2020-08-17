using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MetricSummaryReportMap : ClassMap<MetricSummary>
    {
        public MetricSummaryReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.MetricPrefix).Index(i); i++;
            
            Map(m => m.NumAll).Index(i); i++;
            Map(m => m.NumActivity).Index(i); i++;
            Map(m => m.NumNoActivity).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.MetricsListLink).Index(i); i++;
        }
    }
}
