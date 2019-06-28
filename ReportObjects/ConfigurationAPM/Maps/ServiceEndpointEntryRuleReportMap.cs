using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ServiceEndpointEntryRuleReportMap : ClassMap<ServiceEndpointEntryRule>
    {
        public ServiceEndpointEntryRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.EntryPointType).Index(i); i++;
            Map(m => m.NamingConfigType).Index(i); i++;
            Map(m => m.IsMonitoringEnabled).Index(i); i++;
            Map(m => m.DiscoveryType).Index(i); i++;
            Map(m => m.IsOverride).Index(i); i++;

            Map(m => m.NumDetectedSEPs).Index(i); i++;
            Map(m => m.DetectedSEPs).Index(i); i++;

            Map(m => m.RuleRawValue).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}