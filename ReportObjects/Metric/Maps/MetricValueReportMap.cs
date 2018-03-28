using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class MetricValueReportMap : ClassMap<MetricValue>
    {
        public MetricValueReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.MetricName).Index(i); i++;

            Map(m => m.EventTimeStamp).Index(i); i++;
            Map(m => m.EventTimeStampUtc).Index(i); i++;
            Map(m => m.EventTime).Index(i); i++;

            Map(m => m.Value).Index(i); i++;
            Map(m => m.Count).Index(i); i++;
            Map(m => m.Min).Index(i); i++;
            Map(m => m.Max).Index(i); i++;
            Map(m => m.Occurrences).Index(i); i++;
            Map(m => m.Sum).Index(i); i++;

            Map(m => m.MetricResolution); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;
            Map(m => m.MetricID).Index(i); i++;
        }
    }
}
