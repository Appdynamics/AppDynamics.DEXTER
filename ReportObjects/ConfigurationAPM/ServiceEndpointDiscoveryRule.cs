using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ServiceEndpointDiscoveryRule : ConfigurationEntityBase
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
        public string DiscoveryType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string NamingConfigType { get; set; }

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
                return "APMServiceEndpointDiscoveryRule";
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
                "ServiceEndpointDiscoveryRule: {0}/{1}/{2} {3} {4}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.EntryPointType);
        }
    }
}
