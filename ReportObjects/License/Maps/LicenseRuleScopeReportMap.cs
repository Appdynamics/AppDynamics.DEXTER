using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class LicenseRuleScopeReportMap : ClassMap<LicenseRuleScope>
    {
        public LicenseRuleScopeReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.AccountName).Index(i); i++;

            Map(m => m.RuleName).Index(i); i++;

            Map(m => m.ScopeSelector).Index(i); i++;
            Map(m => m.MatchType).Index(i); i++;

            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;

            Map(m => m.AccountID).Index(i); i++;
            Map(m => m.RuleID).Index(i); i++;
        }
    }
}
