using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBSession : DBEntityBase
    {
        public string SessionName { get; set; }

        public string ClientName { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public decimal Weight { get; set; }

        public long SessionID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBSession: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID,
                this.CollectorType,
                this.SessionName,
                this.SessionID);
        }
    }
}
