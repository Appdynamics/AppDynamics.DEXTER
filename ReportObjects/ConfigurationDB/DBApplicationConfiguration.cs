using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBApplicationConfiguration : ConfigurationEntityBase
    {
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumCollectorDefinitions { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumCustomMetrics { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.ApplicationName;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.ApplicationName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "DBApplicationConfiguration";
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
                "DBApplicationConfiguration: {0} {1}",
                this.Controller,
                this.NumCollectorDefinitions);
        }
    }
}
