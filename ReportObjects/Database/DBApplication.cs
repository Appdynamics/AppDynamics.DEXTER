using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBApplication : DBEntityBase
    {
        public int NumCollectorDefinitions { get; set; }

        public int NumCollectors { get; set; }
        public int NumOracle { get; set; }
        public int NumSQLServer { get; set; }
        public int NumMySQL { get; set; }
        public int NumMongo { get; set; }
        public int NumPostgres { get; set; }
        public int NumDB2 { get; set; }
        public int NumSybase { get; set; }
        public int NumOther { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBApplication: {0}",
                this.Controller);
        }
    }
}
