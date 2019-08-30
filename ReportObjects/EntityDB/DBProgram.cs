using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBProgram : DBEntityBase
    {
        public string ProgramName { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public decimal Weight { get; set; }

        public long ProgramID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBProgram: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID,
                this.CollectorType,
                this.ProgramName,
                this.ProgramID);
        }
    }
}
