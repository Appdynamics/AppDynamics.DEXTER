using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class EmailAlertTemplateReportMap : ClassMap<EmailAlertTemplate>
    {
        public EmailAlertTemplateReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.Name).Index(i); i++;

            Map(m => m.OneEmailPerEvent).Index(i); i++;
            Map(m => m.EventLimit).Index(i); i++;

            Map(m => m.To).Index(i); i++;
            Map(m => m.CC).Index(i); i++;
            Map(m => m.BCC).Index(i); i++;
            Map(m => m.TestTo).Index(i); i++;
            Map(m => m.TestCC).Index(i); i++;
            Map(m => m.TestBCC).Index(i); i++;
            Map(m => m.TestLogLevel).Index(i); i++;

            Map(m => m.Headers).Index(i); i++;
            Map(m => m.Subject).Index(i); i++;
            Map(m => m.TextBody).Index(i); i++;
            Map(m => m.HTMLBody).Index(i); i++;
            Map(m => m.IncludeHTMLBody).Index(i); i++;

            Map(m => m.Properties).Index(i); i++;
            Map(m => m.TestProperties).Index(i); i++;

            Map(m => m.EventTypes).Index(i); i++;

            Map(m => m.TemplateID).Index(i); i++;
        }
    }
}