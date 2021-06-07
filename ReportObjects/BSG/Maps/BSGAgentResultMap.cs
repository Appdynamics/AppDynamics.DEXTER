using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGAgentResultMap : ClassMap<BSGAgentResult>
    {
        public BSGAgentResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            
            Map(m => m.IsAPMAgentUsed).Index(i); i++;
            Map(m => m.AgentVersion).Index(i); i++;
            
            Map(m => m.IsMachineAgentUsed).Index(i); i++;
            Map(m => m.MachineAgentVersion).Index(i); i++;
        }
    }
}
