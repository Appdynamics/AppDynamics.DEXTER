using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ErrorDetectionLoggerReportMap : ClassMap<ErrorDetectionLogger>
    {
        public ErrorDetectionLoggerReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.LoggerName).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;

            Map(m => m.MatchClass).Index(i); i++;
            Map(m => m.MatchMethod).Index(i); i++;
            Map(m => m.MatchType).Index(i); i++;
            Map(m => m.MatchParameterTypes).Index(i); i++;

            Map(m => m.ExceptionParam).Index(i); i++;
            Map(m => m.MessageParam).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}