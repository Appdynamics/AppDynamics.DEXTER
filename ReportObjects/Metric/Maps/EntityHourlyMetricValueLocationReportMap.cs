using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class EntityHourlyMetricValueLocationReportMap : ClassMap<EntityHourlyMetricValueLocation>
    {
        public EntityHourlyMetricValueLocationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.MetricName).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;

            Map(m => m.RowStart).Index(i); i++;
            Map(m => m.RowEnd).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;
            Map(m => m.MetricID).Index(i); i++;
        }
    }
}
