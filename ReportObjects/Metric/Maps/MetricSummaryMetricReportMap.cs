using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class MetricSummaryMetricReportMap : ClassMap<MetricSummary>
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
