using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DBQueryReportMap : ClassMap<DBQuery>
    {
        public DBQueryReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.CollectorName).Index(i); i++;
            Map(m => m.CollectorType).Index(i); i++;

            Map(m => m.CollectorStatus).Index(i); i++;

            Map(m => m.AgentName).Index(i); i++;

            Map(m => m.Host).Index(i); i++;
            Map(m => m.Port).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;

            Map(m => m.QueryHash).Index(i); i++;
            Map(m => m.Query).Index(i); i++;

            Map(m => m.SQLClauseType).Index(i); i++;
            Map(m => m.SQLJoinType).Index(i); i++;
            Map(m => m.SQLGroupBy).Index(i); i++;
            Map(m => m.SQLHaving).Index(i); i++;
            Map(m => m.SQLOrderBy).Index(i); i++;
            Map(m => m.SQLUnion).Index(i); i++;
            Map(m => m.SQLWhere).Index(i); i++;

            Map(m => m.SQLTable).Index(i); i++;
            Map(m => m.SQLTables).Index(i); i++;
            Map(m => m.NumSQLTables).Index(i); i++;

            Map(m => m.Name).Index(i); i++;
            Map(m => m.Namespace).Index(i); i++;
            Map(m => m.Client).Index(i); i++;

            Map(m => m.Calls).Index(i); i++;
            Map(m => m.ExecTime).Index(i); i++;
            Map(m => m.ExecTimeSpan).Index(i); i++;
            Map(m => m.AvgExecTime).Index(i); i++;
            Map(m => m.AvgExecRange).Index(i); i++;

            Map(m => m.Weight).Index(i); i++;

            Map(m => m.IsSnapCorrData).Index(i); i++;
            Map(m => m.IsSnapWindowData).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ConfigID).Index(i); i++;
            Map(m => m.CollectorID).Index(i); i++;
            Map(m => m.QueryID).Index(i); i++;
            Map(m => m.QueryLink).Index(i); i++;
        }
    }
}
