using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBQuery : DBEntityBase
    {
        public string QueryHash { get; set; }
        public string Query { get; set; }

        public string SQLClauseType { get; set; }
        public string SQLJoinType { get; set; }
        public bool SQLWhere { get; set; }
        public bool SQLOrderBy { get; set; }
        public bool SQLGroupBy { get; set; }
        public bool SQLHaving { get; set; }
        public bool SQLUnion { get; set; }

        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Client { get; set; }

        public long Calls { get; set; }
        public long ExecTime { get; set; }
        public TimeSpan ExecTimeSpan { get; set; }
        public long AvgExecTime { get; set; }
        public string AvgExecRange { get; set; }

        public decimal Weight { get; set; }

        public bool IsSnapWindowData { get; set; }
        public bool IsSnapCorrData { get; set; }

        public long QueryID { get; set; }
        public string QueryLink { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBQuery: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.CollectorType, 
                this.QueryHash, 
                this.QueryID);
        }
    }
}
