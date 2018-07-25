using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMMachineEntityReportMap : ClassMap<EntitySIMMachine>
    {
        public SIMMachineEntityReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;

            Map(m => m.MachineType).Index(i); i++;

            Map(m => m.IsHistorical).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.DynamicMonitoringMode).Index(i); i++;
            Map(m => m.HostMachineID).Index(i); i++;

            Map(m => m.DotnetCompatibilityMode).Index(i); i++;
            Map(m => m.ForceMachineInstanceRegistration).Index(i); i++;

            Map(m => m.AgentConfigFeatures).Index(i); i++;
            Map(m => m.ControllerConfigFeatures).Index(i); i++;

            Map(m => m.MemPhysical).Index(i); i++;
            Map(m => m.MemSwap).Index(i); i++;

            Map(m => m.MachineInfo).Index(i); i++;
            Map(m => m.JVMInfo).Index(i); i++;
            Map(m => m.InstallDirectory).Index(i); i++;
            Map(m => m.AgentVersion).Index(i); i++;
            Map(m => m.AgentVersionRaw).Index(i); i++;
            Map(m => m.AutoRegisterAgent).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.APMApplicationName).Index(i); i++;
            Map(m => m.APMTierName).Index(i); i++;
            Map(m => m.APMNodeName).Index(i); i++;

            Map(m => m.NumProps).Index(i); i++;
            Map(m => m.NumTags).Index(i); i++;
            Map(m => m.NumCPUs).Index(i); i++;
            Map(m => m.NumVolumes).Index(i); i++;
            Map(m => m.NumNetworks).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
