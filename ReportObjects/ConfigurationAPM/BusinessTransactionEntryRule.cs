using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionEntryRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string EntryPointType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBackground { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsExcluded { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsExclusion { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Priority { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string MatchClass { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string MatchMethod { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string MatchURI { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string SplitConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Parameters { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumDetectedBTs { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string DetectedBTs { get; set; }

        public string RuleRawValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}/{3}", this.RuleName, this.AgentType, this.EntryPointType, this.TierName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.RuleName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMBTEntryRule";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.AgentType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionEntryRule: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.EntryPointType, 
                this.RuleName);
        }
    }
}
