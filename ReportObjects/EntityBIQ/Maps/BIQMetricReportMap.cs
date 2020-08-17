using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQMetricReportMap : ClassMap<BIQMetric>
    {
        public BIQMetricReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.MetricName).Index(i); i++;
            Map(m => m.MetricDescription).Index(i); i++;
            Map(m => m.DataSource).Index(i); i++;
            Map(m => m.EventType).Index(i); i++;
            Map(m => m.Query).Index(i); i++;

            Map(m => m.IsEnabled).Index(i); i++;

            Map(m => m.LastExecStatus).Index(i); i++;
            Map(m => m.LastExecDuration).Index(i); i++;
            Map(m => m.SuccessCount).Index(i); i++;
            Map(m => m.FailureCount).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.MetricID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}