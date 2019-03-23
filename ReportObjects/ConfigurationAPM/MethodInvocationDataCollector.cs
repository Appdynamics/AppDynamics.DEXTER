using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MethodInvocationDataCollector : ConfigurationEntityBase
    {
        public string CollectorName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchClass { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchMethod { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string MatchParameterTypes { get; set; }

        public string DataGathererName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DataGathererType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int DataGathererPosition { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DataGathererTransform { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DataGathererGetter { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAssignedToNewBTs { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAPM { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAnalytics { get; set; }

        public bool IsAssignedToBTs { get; set; }
        public int NumAssignedBTs { get; set; }
        public string AssignedBTs { get; set; }

        public string RuleRawValue { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.DataGathererName, this.CollectorName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.DataGathererName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "MIDC";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.CollectorName;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "MethodInvocationDataCollector: {0}/{1}/{2}",
                this.Controller,
                this.ApplicationName,
                this.CollectorName);
        }
    }
}
