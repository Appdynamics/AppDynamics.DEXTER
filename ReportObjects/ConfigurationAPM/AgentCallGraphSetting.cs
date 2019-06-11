using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class AgentCallGraphSetting : ConfigurationEntityBase
    {
        public string AgentType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int SamplingRate { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string IncludePackages { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumIncludePackages { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string ExcludePackages { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumExcludePackages { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int MinSQLDuration { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsRawSQLEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsHotSpotEnabled { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.AgentType;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.AgentType;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMCallGraphSetting";
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
                "AgentCallGraphSetting: {0}/{1} {2}",
                this.Controller,
                this.ApplicationName,
                this.AgentType);
        }
    }
}
