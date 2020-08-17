using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class APMNodeReportMap : ClassMap<APMNode>
    {
        public APMNodeReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.AgentVersion).Index(i); i++;
            Map(m => m.AgentVersionRaw).Index(i); i++;
            Map(m => m.AgentPresent).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;
            Map(m => m.MachineAgentVersion).Index(i); i++;
            Map(m => m.MachineAgentVersionRaw).Index(i); i++;
            Map(m => m.MachineAgentPresent).Index(i); i++;
            Map(m => m.MachineOSType).Index(i); i++;
            Map(m => m.MachineType).Index(i); i++;

            Map(m => m.AgentRuntime).Index(i); i++;

            Map(m => m.InstallDirectory).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.InstallTime), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.LastStartTime), i); i++;
            Map(m => m.IsDisabled).Index(i); i++;
            Map(m => m.IsMonitoringDisabled).Index(i); i++;

            Map(m => m.WebHostContainerType).Index(i); i++;
            Map(m => m.CloudHostType).Index(i); i++;
            Map(m => m.CloudRegion).Index(i); i++;
            Map(m => m.ContainerRuntimeType).Index(i); i++;

            Map(m => m.HeapInitialSizeMB).Index(i); i++;
            Map(m => m.HeapMaxSizeMB).Index(i); i++;
            Map(m => m.HeapYoungInitialSizeMB).Index(i); i++;
            Map(m => m.HeapYoungMaxSizeMB).Index(i); i++;

            Map(m => m.ClassPath).Index(i); i++;
            Map(m => m.ClassVersion).Index(i); i++;
            Map(m => m.Home).Index(i); i++;
            Map(m => m.RuntimeName).Index(i); i++;
            Map(m => m.RuntimeVersion).Index(i); i++;
            Map(m => m.Vendor).Index(i); i++;
            Map(m => m.VendorVersion).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.VMInfo).Index(i); i++;
            Map(m => m.VMName).Index(i); i++;
            Map(m => m.VMVendor).Index(i); i++;
            Map(m => m.VMVersion).Index(i); i++;

            Map(m => m.OSArchitecture).Index(i); i++;
            Map(m => m.OSName).Index(i); i++;
            Map(m => m.OSVersion).Index(i); i++;
            Map(m => m.OSComputerName).Index(i); i++;

            Map(m => m.OSProcessorType).Index(i); i++;
            Map(m => m.OSProcessorRevision).Index(i); i++;
            Map(m => m.OSNumberOfProcs).Index(i); i++;

            Map(m => m.UserName).Index(i); i++;
            Map(m => m.Domain).Index(i); i++;

            Map(m => m.NumStartupOptions).Index(i); i++;
            Map(m => m.NumProperties).Index(i); i++;
            Map(m => m.NumEnvVariables).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.MetricGraphLink).Index(i); i++;
            Map(m => m.FlameGraphLink).Index(i); i++;
            Map(m => m.FlameChartLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.NodeLink).Index(i); i++;
        }
    }
}
