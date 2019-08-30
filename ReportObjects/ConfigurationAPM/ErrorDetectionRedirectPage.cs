using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ErrorDetectionRedirectPage : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string PageName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchPattern { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.AgentType, this.PageName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.PageName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMErrorDetectionRedirectPage";
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
                "ErrorDetectionRedirectPage: {0}/{1}/{2} {3} {4}={5}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.PageName,
                this.MatchType, 
                this.MatchPattern);
        }
    }
}
