using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class HTTPAlertTemplateReportMap : ClassMap<HTTPAlertTemplate>
    {
        public HTTPAlertTemplateReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.Name).Index(i); i++;

            Map(m => m.Method).Index(i); i++;
            Map(m => m.Scheme).Index(i); i++;
            Map(m => m.Host).Index(i); i++;
            Map(m => m.Port).Index(i); i++;
            Map(m => m.Path).Index(i); i++;
            Map(m => m.Query).Index(i); i++;

            Map(m => m.AuthType).Index(i); i++;
            Map(m => m.AuthUsername).Index(i); i++;
            Map(m => m.AuthPassword).Index(i); i++;

            Map(m => m.Headers).Index(i); i++;
            Map(m => m.ContentType).Index(i); i++;
            Map(m => m.FormData).Index(i); i++;
            Map(m => m.Payload).Index(i); i++;

            Map(m => m.ConnectTimeout).Index(i); i++;
            Map(m => m.SocketTimeout).Index(i); i++;

            Map(m => m.ResponseAny).Index(i); i++;
            Map(m => m.ResponseNone).Index(i); i++;

            Map(m => m.TemplateID).Index(i); i++;
        }
    }
}