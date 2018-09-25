using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBCollector : DBEntityBase
    {
        public string Role { get; set; }

        public long Calls { get; set; }
        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBCollector: {0}/{1}({2}) [{3}]",
                this.Controller,
                this.CollectorName,
                this.CollectorID, 
                this.CollectorType);
        }
    }
}
