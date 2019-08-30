using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ErrorDetectionRedirectPageReportMap : ClassMap<ErrorDetectionRedirectPage>
    {
        public ErrorDetectionRedirectPageReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.PageName).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.MatchType).Index(i); i++;
            Map(m => m.MatchPattern).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}