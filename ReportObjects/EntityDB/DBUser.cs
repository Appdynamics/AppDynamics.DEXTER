using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBUser : DBEntityBase
    {
        public string DBUserName { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public decimal Weight { get; set; }

        public long UserID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBUser: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.CollectorType, 
                this.DBUserName, 
                this.UserID);
        }
    }
}
