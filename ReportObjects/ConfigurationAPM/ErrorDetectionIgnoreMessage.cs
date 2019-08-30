using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ErrorDetectionIgnoreMessage : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string ExceptionClass { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MessagePattern { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.AgentType, this.ExceptionClass);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.ExceptionClass;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMErrorDetectionIgnoreMessage";
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
                "ErrorDetectionIgnoreMessage: {0}/{1}/{2} {3} {4}={5}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.ExceptionClass,
                this.MatchType, 
                this.MessagePattern);
        }
    }
}
