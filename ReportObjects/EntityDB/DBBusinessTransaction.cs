using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBBusinessTransaction : DBEntityBase
    {
        public string BTName { get; set; }

        public long Calls { get; set; }
        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }
        public long AvgExecTime { get; set; }
        public string AvgExecRange { get; set; }

        public decimal Weight { get; set; }

        public long BTID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBBusinessTransaction: {0}/{1}({2}) [{3}] {4}/{5}",
                this.Controller,
                this.CollectorName,
                this.ConfigID,
                this.CollectorType,
                this.ApplicationName,
                this.BTName);
        }
    }
}
