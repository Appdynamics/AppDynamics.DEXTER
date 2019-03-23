using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBDatabase : DBEntityBase
    {
        public string DatabaseName { get; set; }

        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }

        public decimal Weight { get; set; }

        public long DatabaseID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBDatabase: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.CollectorType, 
                this.DatabaseName, 
                this.DatabaseID);
        }
    }
}
