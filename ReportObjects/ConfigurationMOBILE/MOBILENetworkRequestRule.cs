using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MOBILENetworkRequestRule : ConfigurationEntityBase
    {
        public string RuleName { get; set; }

        public string DetectionType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDefault { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Priority { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchURL { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchIPAddress { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchMobileApp { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchUserAgent { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchUserAgentType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseProtocol { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseDomain { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseURL { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseHTTP { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseRegex { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string NamingType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AnchorType { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string UrlSegments { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string AnchorSegments { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string RegexGroups { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string QueryStrings { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DomainNameType { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.RuleName, this.DetectionType);
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
                return "MOBILENetworkRequestRule";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.DetectionType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "MOBILENetworkRequestRule: {0}/{1}({2}) {3} {4}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.DetectionType,
                this.RuleName);
        }
    }
}
