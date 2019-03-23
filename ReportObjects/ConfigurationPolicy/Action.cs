using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class Action : ConfigurationEntityBase
    {
        public string ActionName { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ActionType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Priority { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]

        public bool IsAdjudicate { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AdjudicatorEmail { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ScriptPath { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string LogPaths { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string ScriptOutputPaths { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool CollectScriptOutputs { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int TimeoutMinutes  { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string To { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CC { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string BCC { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Subject { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CustomProperties { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CustomType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumSamples { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long SampleInterval { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ActionTemplate { get; set; }

        public long TemplateID { get; set; }

        public long ActionID { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.ActionName, this.ActionType);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.ActionName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "Action";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.ActionType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "Action: {0}/{1}/{2}/{3}",
                this.Controller,
                this.ApplicationName,
                this.ActionName,
                this.ActionType);
        }
    }
}
