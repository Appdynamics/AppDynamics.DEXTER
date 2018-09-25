using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MachineContainer : SIMEntityBase
    {
        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string ContainerID { get; set; }
        public string ContainerName { get; set; }
        public string ImageName { get; set; }
        public long ContainerMachineID { get; set; }
        public string StartedAt { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MachineContainer: {0}/{1}({2})/{3}({4})/{5}({6})",
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
