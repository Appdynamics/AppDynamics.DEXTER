using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ErrorDetectionIgnoreLogger : ConfigurationEntityBase
    {
        public string AgentType { get; set; }
        public string LoggerName { get; set; }

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
                return "APMErrorDetectionIgnoreLogger";
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
                "ErrorDetectionIgnoreLogger: {0}/{1}/{2} {3}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.LoggerName);
        }
    }
}
