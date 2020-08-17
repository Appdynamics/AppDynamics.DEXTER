using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class LicenseRuleReportMap : ClassMap<LicenseRule>
    {
        public LicenseRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.AccountName).Index(i); i++;

            Map(m => m.RuleName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;

            Map(m => m.RuleLicenses).Index(i); i++;
            Map(m => m.RulePeak).Index(i); i++;
            Map(m => m.Licenses).Index(i); i++;

            Map(m => m.NumApplications).Index(i); i++;
            Map(m => m.NumServers).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.AccessKey).Index(i); i++;

            Map(m => m.AccountID).Index(i); i++;
            Map(m => m.RuleID).Index(i); i++;
        }
    }
}
