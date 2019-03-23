using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class CustomExitRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string ExitType { get; set; }
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchClass { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchMethod { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchParameterTypes { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string DataCollectorsConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string InfoPointsConfig { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsApplyToAllBTs { get; set; }

        public int NumDetectedBackends { get; set; }
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
                return "CustomExit";
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
                "CustomExitRule: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.ExitType,
                this.RuleName);
        }
    }
}
