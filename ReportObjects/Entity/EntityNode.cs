using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntityNode : EntityBase
    {
        public const string ENTITY_TYPE = "Node";
        public const string ENTITY_FOLDER = "NODE";

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeLink { get; set; }
        public string NodeName { get; set; }

        public string AgentType { get; set; }
        public bool AgentPresent { get; set; }
        public string AgentVersion { get; set; }
        public string AgentVersionRaw { get; set; }

        public bool MachineAgentPresent { get; set; }
        public string MachineAgentVersion { get; set; }
        public string MachineAgentVersionRaw { get; set; }
        public long MachineID { get; set; }
        public string MachineName { get; set; }
        public string MachineOSType { get; set; }
        public string MachineType { get; set; }

        public bool IsDisabled { get; set; }
        public bool IsMonitoringDisabled { get; set; }

        public string InstallDirectory { get; set; }
        public DateTime LastStartTime { get; set; }
        public DateTime InstallTime { get; set; }

        public int NumStartupOptions { get; set; }
        public int NumProperties { get; set; }
        public int NumEnvVariables { get; set; }

        public string AgentRuntime { get; set; }

        public override long EntityID
        {
            get
            {
                return this.NodeID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.NodeName;
            }
        }
        public override string EntityType
        {
            get
            {
                return ENTITY_TYPE;
            }
        }
        public override string FolderName
        {
            get
            {
                return ENTITY_FOLDER;
            }
        }

        public EntityNode Clone()
        {
            return (EntityNode)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityNode: {0}/{1}({2})/{3}({4})/{5}({6})",
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
