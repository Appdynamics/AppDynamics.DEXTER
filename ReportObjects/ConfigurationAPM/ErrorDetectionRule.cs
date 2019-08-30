using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ErrorDetectionRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string RuleValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.AgentType, this.RuleName);
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
                return "APMErrorDetectionRule";
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
                "ErrorDetectionRule: {0}/{1}/ {2} {3}={4}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.RuleName,
                this.RuleValue);
        }
    }
}
