using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MachineProcess : SIMEntityBase
    {
        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string Class { get; set; }
        public string ClassID { get; set; }
        public string Name { get; set; }
        public string CommandLine { get; set; }
        public string RealUser { get; set; }
        public string RealGroup { get; set; }
        public string EffectiveUser { get; set; }
        public string EffectiveGroup { get; set; }
        public string State { get; set; }
        public int NiceLevel { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int PID { get; set; }
        public int ParentPID { get; set; }
        public int PGID { get; set; }
        public override String ToString()
        {
            return String.Format(
                "MachineProcess: {0}/{1}({2})/{3}({4})/{5}({6})",
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
