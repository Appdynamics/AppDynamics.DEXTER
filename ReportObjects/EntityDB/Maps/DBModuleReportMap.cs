using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DBModuleReportMap : ClassMap<DBModule>
    {
        public DBModuleReportMap()
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

            Map(m => m.ModuleName).Index(i); i++;

            Map(m => m.ExecTime).Index(i); i++;
            Map(m => m.ExecTimeSpan).Index(i); i++;

            Map(m => m.Weight).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ConfigID).Index(i); i++;
            Map(m => m.CollectorID).Index(i); i++;
            Map(m => m.ModuleID).Index(i); i++;
        }
    }
}
