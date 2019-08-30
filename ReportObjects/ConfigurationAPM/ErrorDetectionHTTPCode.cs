using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ErrorDetectionHTTPCode : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string RangeName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool CaptureURL { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int CodeFrom { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int CodeTo { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.AgentType, this.RangeName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.RangeName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMErrorDetectionHTTPCode";
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
                "ErrorDetectionHTTPCode: {0}/{1}/{2} {3} {4}-{5}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.RangeName,
                this.CodeFrom, 
                this.CodeTo);
        }
    }
}
