using CsvHelper.Configuration;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class MetricValueMetricReportMap : CsvClassMap<MetricValue>
    {
        public MetricValueMetricReportMap()
        {
            int i = 0;
            Map(m => m.EventTime).Index(i); i++;
            Map(m => m.EventTimeUtc).Index(i); i++;
            Map(m => m.Value).Index(i); i++;
            Map(m => m.Count).Index(i); i++;
            Map(m => m.Min).Index(i); i++;
            Map(m => m.Max).Index(i); i++;
            Map(m => m.Occurences).Index(i); i++;
            Map(m => m.Sum).Index(i); i++;
            Map(m => m.MetricID).Index(i); i++;
            Map(m => m.MetricResolution).Index(i).TypeConverterOption("g"); i++;
        }
    }
}
