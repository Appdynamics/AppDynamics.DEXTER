using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class BackendDiscoveryRuleReportMap : CsvClassMap<BackendDiscoveryRule>
    {
        public BackendDiscoveryRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.ExitType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.Priority).Index(i); i++;
            Map(m => m.IsCorrelationSupported).Index(i); i++;
            Map(m => m.IsCorrelationEnabled).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;

            Map(m => m.IdentityOptions).Index(i); i++;
            Map(m => m.DiscoveryConditions).Index(i); i++;
            Map(m => m.RuleRawValue).Index(i); i++;

            Map(m => m.NumDetectedBackends).Index(i); i++;
            Map(m => m.DetectedBackends).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}