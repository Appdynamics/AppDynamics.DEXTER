using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class LicenseReportMap : ClassMap<License>
    {
        public LicenseReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.AccountName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;

            Map(m => m.Edition).Index(i); i++;
            Map(m => m.Model).Index(i); i++;

            Map(m => m.Provisioned).Index(i); i++;
            Map(m => m.MaximumAllowed).Index(i); i++;
            Map(m => m.Average).Index(i); i++;
            Map(m => m.Min).Index(i); i++;
            Map(m => m.Max).Index(i); i++;
            Map(m => m.Retention).Index(i); i++;

            Map(m => m.ExpirationDate).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.AccountID).Index(i); i++;
        }
    }
}
