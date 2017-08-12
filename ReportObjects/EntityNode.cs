using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityNode : EntityBase
    {
        public string AgentType { get; set; }
        public bool AgentPresent { get; set; }
        public string AgentVersion { get; set; }
        public string AgentVersionRaw { get; set; }

        public bool MachineAgentPresent { get; set; }
        public string MachineAgentVersion { get; set; }
        public string MachineAgentVersionRaw { get; set; }
        public int MachineID { get; set; }
        public string MachineName { get; set; }
        public string MachineOSType { get; set; }
        public string MachineType { get; set; }

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
