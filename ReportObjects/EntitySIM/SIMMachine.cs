using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMMachine : SIMEntityBase
    {
        public const string ENTITY_TYPE = "SIMMachine";
        public const string ENTITY_FOLDER = "MACHINE";

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }
        public string MachineLink { get; set; }

        public string MachineType { get; set; }
        public bool IsHistorical { get; set; }
        public bool IsEnabled { get; set; }
        public string DynamicMonitoringMode { get; set; }
        public long HostMachineID { get; set; }
        public bool DotnetCompatibilityMode { get; set; }
        public bool ForceMachineInstanceRegistration { get; set; }

        public string AgentConfigFeatures { get; set; }
        public string ControllerConfigFeatures { get; set; }

        public int MemPhysical { get; set; }
        public int MemSwap { get; set; }

        public string MachineInfo { get; set; }
        public string JVMInfo { get; set; }
        public string InstallDirectory { get; set; }
        public string AgentVersion { get; set; }
        public string AgentVersionRaw { get; set; }
        public bool AutoRegisterAgent { get; set; }
        public string AgentType { get; set; }
        public string APMApplicationName { get; set; }
        public string APMTierName { get; set; }
        public string APMNodeName { get; set; }

        public int NumProps { get; set; }
        public int NumTags { get; set; }
        public int NumCPUs { get; set; }
        public int NumVolumes { get; set; }
        public int NumNetworks { get; set; }
        public int NumContainers { get; set; }

        public override String ToString()
        {
            return String.Format(
                "SIMMachine: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.NodeName,
                this.NodeID);
        }
    }
}
