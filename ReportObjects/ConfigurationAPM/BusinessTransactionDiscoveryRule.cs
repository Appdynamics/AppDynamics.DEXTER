using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionDiscoveryRule : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string EntryPointType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsMonitoringEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DiscoveryType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDiscoveryEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string NamingConfigType { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string RuleRawValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}", this.EntryPointType, this.AgentType, this.TierName);
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
                return "APMBTDiscoveryRule";
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
                "BusinessTransactionDiscoveryRule: {0}/{1}/{2} {3} {4}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.EntryPointType);
        }
    }
}
