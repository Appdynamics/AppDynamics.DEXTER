using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBCustomMetric : ConfigurationEntityBase
    {
        public string CollectorName { get; set; }
        public string CollectorType { get; set; }

        public long ConfigID { get; set; }

        public string MetricName { get; set; }
        public long MetricID { get; set; }

        public int Frequency { get; set; }

        public string Query { get; set; }

        public string SQLClauseType { get; set; }
        public string SQLJoinType { get; set; }
        public bool SQLWhere { get; set; }
        public bool SQLOrderBy { get; set; }
        public bool SQLGroupBy { get; set; }
        public bool SQLHaving { get; set; }
        public bool SQLUnion { get; set; }

        public bool IsEvent { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBCustomMetric: {0}/{1}({2}) {3}({4})",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.MetricName, 
                this.MetricID);
        }
    }
}
