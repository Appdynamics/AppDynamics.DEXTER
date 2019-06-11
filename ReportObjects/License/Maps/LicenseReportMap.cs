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

            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.AccountID).Index(i); i++;
        }
    }
}
