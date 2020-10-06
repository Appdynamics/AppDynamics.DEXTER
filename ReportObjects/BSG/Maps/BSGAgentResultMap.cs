using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGAgentResultMap : ClassMap<BSGAgentResult>
    {
        public BSGAgentResultMap()
        {
            int i = 0;
            Map(m => m.Application).Index(i); i++;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.AgentPresent).Index(i); i++;
            Map(m => m.AgentVersion).Index(i); i++;
            
            Map(m => m.MachineAgentVersion).Index(i); i++;
            Map(m => m.MachineAgentPresent).Index(i); i++;

            Map(m => m.IsDisabled).Index(i); i++;
            Map(m => m.IsMonitoringDisabled).Index(i); i++;
        }
    }
}
