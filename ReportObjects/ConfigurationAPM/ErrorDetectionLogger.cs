using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ErrorDetectionLogger : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string LoggerName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchClass { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchMethod { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchParameterTypes { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int ExceptionParam { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int MessageParam { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.AgentType, this.LoggerName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.LoggerName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMErrorDetectionLogger";
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
                "ErrorDetectionLogger: {0}/{1}/{2} {3} {4}:{5}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.LoggerName,
                this.MatchClass, 
                this.MatchMethod);
        }
    }
}
