using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBPageDetectionRule : ConfigurationEntityBase
    {
        public string RuleName { get; set; }

        public string DetectionType { get; set; }
        public string EntityCategory { get; set; }

        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public int Priority { get; set; }

        public string MatchURL { get; set; }
        public string MatchIPAddress { get; set; }
        public string MatchMobileApp { get; set; }
        public string MatchUserAgent { get; set; }
        public string MatchUserAgentType { get; set; }

        public bool UseProtocol { get; set; }
        public bool UseDomain { get; set; }
        public bool UseURL { get; set; }
        public bool UseHTTP { get; set; }
        public bool UseRegex { get; set; }
        public string NamingType { get; set; }
        public string AnchorType { get; set; }
        public string UrlSegments { get; set; }
        public string AnchorSegments { get; set; }
        public string RegexGroups { get; set; }
        public string QueryStrings { get; set; }
        public string DomainNameType { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.ApplicationName;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.ApplicationName;
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
