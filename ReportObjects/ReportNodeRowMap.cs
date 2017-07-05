using CsvHelper.Configuration;

namespace AppDynamics.OfflineData.ReportObjects
{
    public class ReportNodeRowMap : CsvClassMap<ReportNodeRow>
    {
        public ReportNodeRowMap()
        {
            int i = 0; 
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.AgentVersion).Index(i); i++;
            Map(m => m.AgentPresent).Index(i); i++;
            Map(m => m.AgentID).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;
            Map(m => m.MachineAgentVersion).Index(i); i++;
            Map(m => m.MachineAgentPresent).Index(i); i++;
            Map(m => m.MachineOSType).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
            Map(m => m.Type).Index(i); i++;
        }
    }
}
