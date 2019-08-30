using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBCustomMetric : ConfigurationEntityBase
    {
        public string CollectorName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CollectorType { get; set; }

        public long ConfigID { get; set; }

        public string MetricName { get; set; }
        public long MetricID { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Frequency { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Query { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string SQLClauseType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string SQLJoinType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool SQLWhere { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool SQLOrderBy { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool SQLGroupBy { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool SQLHaving { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool SQLUnion { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEvent { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.CollectorName, this.MetricName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.MetricName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "DBCustomMetric";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return String.Empty;
            }
        }

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
