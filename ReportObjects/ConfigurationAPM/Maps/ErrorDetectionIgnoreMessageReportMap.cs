using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ErrorDetectionIgnoreMessageReportMap : ClassMap<ErrorDetectionIgnoreMessage>
    {
        public ErrorDetectionIgnoreMessageReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.ExceptionClass).Index(i); i++;
            Map(m => m.MatchType).Index(i); i++;
            Map(m => m.MessagePattern).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}