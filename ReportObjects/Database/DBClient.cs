using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBClient : DBEntityBase
    {
        public string ClientName { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public decimal Weight { get; set; }

        public long ClientID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBClient: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.CollectorType, 
                this.ClientName, 
                this.ClientID);
        }
    }
}
