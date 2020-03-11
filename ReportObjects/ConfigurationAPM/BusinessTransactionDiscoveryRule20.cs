using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionDiscoveryRule20 : ConfigurationEntityBase
    {
        public string ScopeName { get; set; }

        public string AgentType { get; set; }
        public string EntryPointType { get; set; }
        public string RuleName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Version { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Priority { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsMonitoringEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDiscoveryEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string NamingConfigType { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string HTTPAutoDiscovery { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}/{3}", this.RuleName, this.AgentType, this.EntryPointType, this.ScopeName);
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
                return "APMBTDiscoveryRule20";
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
                "BusinessTransactionDiscoveryRule20: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.ScopeName,
                this.AgentType,
                this.EntryPointType,
                this.RuleName);
        }
    }
}
