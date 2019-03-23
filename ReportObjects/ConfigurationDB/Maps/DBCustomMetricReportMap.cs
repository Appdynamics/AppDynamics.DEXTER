using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DBCustomMetricReportMap : ClassMap<DBCustomMetric>
    {
        public DBCustomMetricReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.CollectorName).Index(i); i++;
            Map(m => m.CollectorType).Index(i); i++;

            Map(m => m.MetricName).Index(i); i++;

            Map(m => m.Frequency).Index(i); i++;

            Map(m => m.Query).Index(i); i++;

            Map(m => m.SQLClauseType).Index(i); i++;
            Map(m => m.SQLJoinType).Index(i); i++;
            Map(m => m.SQLGroupBy).Index(i); i++;
            Map(m => m.SQLHaving).Index(i); i++;
            Map(m => m.SQLOrderBy).Index(i); i++;
            Map(m => m.SQLUnion).Index(i); i++;
            Map(m => m.SQLWhere).Index(i); i++;

            Map(m => m.IsEvent).Index(i); i++;

            Map(m => m.ConfigID).Index(i); i++;
            Map(m => m.MetricID).Index(i); i++;

            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
