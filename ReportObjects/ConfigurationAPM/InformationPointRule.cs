using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class InformationPointRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
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
        public string MatchCondition { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string InfoPointsConfig { get; set; }

        public string RuleRawValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.RuleName, this.AgentType);
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
                return "APMInformationPointRule";
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
                "InformationPointRule: {0}/{1}/{2} {3}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.RuleName);
        }
    }
}
