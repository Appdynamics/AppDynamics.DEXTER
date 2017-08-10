using CsvHelper.Configuration;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class MetricSummaryMetricReportMap : CsvClassMap<MetricSummary>
    {
        public MetricSummaryMetricReportMap()
        {
            int i = 0;
            Map(m => m.PropertyName).Index(i); i++;
            Map(m => m.PropertyValue).Index(i); i++;
            Map(m => m.Link).Index(i); i++;
        }
    }
}
