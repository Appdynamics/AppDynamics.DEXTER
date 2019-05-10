using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class LicenseAccountSummaryReportMap : ClassMap<LicenseAccountSummary>
    {
        public LicenseAccountSummaryReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.AccountName).Index(i); i++;
            Map(m => m.AccountNameGlobal).Index(i); i++;
            Map(m => m.AccountNameEUM).Index(i); i++;
            Map(m => m.AccessKey1).Index(i); i++;
            Map(m => m.AccessKey2).Index(i); i++;
            Map(m => m.LicenseKeyEUM).Index(i); i++;
            Map(m => m.ServiceKeyES).Index(i); i++;
            Map(m => m.ExpirationDate).Index(i); i++;

            Map(m => m.AccountID).Index(i); i++;
            Map(m => m.LicenseLink).Index(i); i++;
        }
    }
}
