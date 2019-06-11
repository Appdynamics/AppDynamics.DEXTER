using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBPageDetectionRule : ConfigurationEntityBase
    {
        public string RuleName { get; set; }

        public string DetectionType { get; set; }
        public string EntityCategory { get; set; }

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
                return String.Format("{0}/{1}/{2}", this.RuleName, this.EntityCategory, this.DetectionType);
            }
        }

        public override string EntityName
        {
            get
            {
                return String.Format("{0} [{1}]", this.RuleName, this.EntityCategory);
            }
        }

        public override string RuleType
        {
            get
            {
                return "WEBPageDetectionRule";
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
                "WEBPageDetectionRule: {0}/{1}({2}) {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID, 
                this.EntityCategory,
                this.DetectionType,
                this.RuleName);
        }
    }
}
