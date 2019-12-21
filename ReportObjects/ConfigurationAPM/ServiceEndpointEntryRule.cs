using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ServiceEndpointEntryRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string EntryPointType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Version { get; set; }
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

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumDetectedSEPs { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string DetectedSEPs { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}/{3}", this.RuleName, this.EntryPointType, this.AgentType, this.TierName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.EntryPointType;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMServiceEndpointEntryRule";
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
                "ServiceEndpointEntryRule: {0}/{1}/{2} {3} {4}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.EntryPointType);
        }
    }
}
