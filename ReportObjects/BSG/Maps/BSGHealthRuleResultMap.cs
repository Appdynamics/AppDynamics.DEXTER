using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGHealthRuleResultMap : ClassMap<BSGHealthRuleResult>
    {
        public BSGHealthRuleResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.CriticalViolations).Index(i); i++;
            Map(m => m.WarningViolations).Index(i); i++;
            Map(m => m.DefaultHealthRulesModified).Index(i); i++;
            Map(m => m.CustomHealthRules).Index(i); i++;
            Map(m => m.LinkedPolicies).Index(i); i++;
            Map(m => m.LinkedActions).Index(i); i++;
        }
    }
}
