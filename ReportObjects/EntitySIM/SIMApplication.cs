using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMApplication : SIMEntityBase
    {
        public int NumTiers { get; set; }
        public int NumNodes { get; set; }
        public int NumMachines { get; set; }

        public int NumSAs { get; set; }

        public int NumSAEvents { get; set; }

        public override String ToString()
        {
            return String.Format(
                "SIMApplication: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
