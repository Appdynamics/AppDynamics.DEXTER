using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class MetricExtractMappingReportMap : ClassMap<MetricExtractMapping>
    {
        public MetricExtractMappingReportMap()
        {
            int i = 0;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.MetricPath).Index(i); i++;
            Map(m => m.MetricName).Index(i); i++;
            Map(m => m.FolderName).Index(i); i++;
            Map(m => m.RangeRollupType).Index(i); i++;
            Map(m => m.Graph).Index(i); i++;
            Map(m => m.Axis).Index(i); i++;
            Map(m => m.LineColor).Index(i); i++;
            Map(m => m.MetricSet).Index(i); i++;
        }
    }
}
