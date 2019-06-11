using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBSyntheticJobDefinition : ConfigurationEntityBase
    {
        public string JobName { get; set; }
        public string JobType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsUserEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsSystemEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool FailOnError { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsPrivateAgent { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Rate { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string RateUnit { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Timeout { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Days { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Browsers { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Locations { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumLocations { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ScheduleMode { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string URL { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Script { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Network { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Config { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string PerfCriteria { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public string JobID { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.JobName;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.JobName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "WEBSyntheticJobDefinition";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.JobType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "WEBSyntheticJobDefinition: {0}/{1}({2}) {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID, 
                this.JobName,
                this.JobType,
                this.URL);
        }
    }
}
