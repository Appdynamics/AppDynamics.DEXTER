using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class AgentConfigurationProperty : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string StringValue { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int IntegerValue { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool BooleanValue { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string StringDefaultValue { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int StringMaxLength { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string StringAllowedValues { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int IntegerDefaultValue { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int IntegerMinValue { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int IntegerMaxValue { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool BooleanDefaultValue { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsRequired { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDefault { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}", this.PropertyName, this.AgentType, this.TierName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.PropertyName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMAgentProperty";
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
                "AgentConfigurationProperty: {0}/{1}/{2} {3}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.PropertyName);
        }
    }
}
