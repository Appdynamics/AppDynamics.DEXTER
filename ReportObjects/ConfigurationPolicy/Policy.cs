using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class Policy : ConfigurationEntityBase
    {
        public string PolicyName { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string PolicyType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Duration { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBatchActionsPerMinute { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CreatedBy { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ModifiedBy { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string RequestExperiences { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string CustomEventFilters { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string MissingEntities { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string FilterProperties { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int MaxRows { get; set; }

        public string ApplicationIDs { get; set; }
        public string BTIDs { get; set; }
        public string TierIDs { get; set; }
        public string NodeIDs { get; set; }
        public string ErrorIDs { get; set; }
        public int NumHRs { get; set; }
        public string HRIDs { get; set; }
        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string HRNames { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVStartedWarning { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVStartedCritical { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVWarningToCritical { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVCriticalToWarning { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVContinuesCritical { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVContinuesWarning { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVEndedCritical { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVEndedWarning { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVCanceledCritical { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool HRVCanceledWarning { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool RequestSlow { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool RequestVerySlow { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool RequestStall { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool AllError { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool AppCrashCLR { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool AppCrash { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool AppRestart { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumActions { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Actions { get; set; }

        public string EventFilterRawValue { get; set; }
        public string EntityFiltersRawValue { get; set; }

        public long PolicyID { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.PolicyName, this.PolicyType);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.PolicyName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "Policy";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.PolicyType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "Policy: {0}/{1}/{2}/{3}",
                this.Controller,
                this.ApplicationName,
                this.PolicyName,
                this.PolicyType);
        }
    }
}
