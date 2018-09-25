using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MachineVolume : SIMEntityBase
    {
        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string MountPoint { get; set; }
        public string Partition { get; set; }
        public string PartitionMetricName { get; set; }
        public string VolumeMetricName { get; set; }
        public int SizeMB { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MachineVolume: {0}/{1}({2})/{3}({4})/{5}({6})",
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
