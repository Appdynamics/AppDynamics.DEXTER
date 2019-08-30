using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ErrorDetectionHTTPCodeReportMap : ClassMap<ErrorDetectionHTTPCode>
    {
        public ErrorDetectionHTTPCodeReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.RangeName).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.CaptureURL).Index(i); i++;

            Map(m => m.CodeFrom).Index(i); i++;
            Map(m => m.CodeTo).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}