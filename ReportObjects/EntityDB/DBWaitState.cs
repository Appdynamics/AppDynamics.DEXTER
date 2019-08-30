using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBWaitState : DBEntityBase
    {
        public string State { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public long WaitStateID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBWaitState: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID,
                this.CollectorType,
                this.State,
                this.WaitStateID);
        }
    }
}
