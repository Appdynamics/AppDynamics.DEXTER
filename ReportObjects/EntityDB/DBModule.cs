using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBModule : DBEntityBase
    {
        public string ModuleName { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public decimal Weight { get; set; }

        public long ModuleID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBModule: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.CollectorType, 
                this.ModuleName, 
                this.ModuleID);
        }
    }
}
