using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionEntryRule20 : ConfigurationEntityBase
    {
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ScopeName { get; set; }

        public string AgentType { get; set; }
        public string EntryPointType { get; set; }
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Version { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBackground { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsExclusion { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Priority { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string MatchConditions { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Actions { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Properties { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumDetectedBTs { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string DetectedBTs { get; set; }

        public string RuleRawValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}", this.RuleName, this.AgentType, this.EntryPointType);
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
                return "APMBTEntryRule20";
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
                "BusinessTransactionEntryRule20: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.ScopeName,
                this.AgentType,
                this.EntryPointType, 
                this.RuleName);
        }
    }
}
