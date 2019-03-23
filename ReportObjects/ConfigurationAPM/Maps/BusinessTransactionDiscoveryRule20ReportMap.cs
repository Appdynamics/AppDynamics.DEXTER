using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BusinessTransactionDiscoveryRule20ReportMap : ClassMap<BusinessTransactionDiscoveryRule20>
    {
        public BusinessTransactionDiscoveryRule20ReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.EntryPointType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.ScopeName).Index(i); i++;

            Map(m => m.Priority).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;

            Map(m => m.NamingConfigType).Index(i); i++;
            Map(m => m.IsMonitoringEnabled).Index(i); i++;
            Map(m => m.IsDiscoveryEnabled).Index(i); i++;
            Map(m => m.HTTPAutoDiscovery).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}