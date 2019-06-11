using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BackendDiscoveryRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string ExitType { get; set; }
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsCorrelationSupported { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsCorrelationEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Priority { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string IdentityOptions { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string DiscoveryConditions { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumDetectedBackends { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string DetectedBackends { get; set; }

        public string RuleRawValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}/{3}", this.RuleName, this.AgentType, this.ExitType, this.TierName);
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
                return "APMBackendRule";
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
                "BackendDiscoveryRule: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.ExitType,
                this.RuleName);
        }
    }
}
