using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MachineCPU : SIMEntityBase
    {
        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string CPUID { get; set; }
        public int NumCores { get; set; }
        public int NumLogical { get; set; }
        public string Vendor { get; set; }
        public string Flags { get; set; }
        public int NumFlags { get; set; }
        public string Model { get; set; }
        public string Speed { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MachineCPU: {0}/{1}({2})/{3}({4})/{5}({6})",
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
