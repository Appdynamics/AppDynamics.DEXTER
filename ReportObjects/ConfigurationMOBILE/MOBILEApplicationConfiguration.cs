using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MOBILEApplicationConfiguration : ConfigurationEntityBase
    {
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ApplicationDescription { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ApplicationKey { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumNetworkRulesInclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumNetworkRulesExclude { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string SlowThresholdType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int SlowThreshold { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string VerySlowThresholdType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int VerySlowThreshold { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string StallThresholdType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int StallThreshold { get; set; }

        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string Percentiles { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int SessionTimeout { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int CrashThreshold { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsIPDisplayed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool EnableScreenshot{ get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool AutoScreenshot { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseCellular { get; set; }

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
                return "MOBILEApplicationConfiguration";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return String.Empty;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "MOBILEApplicationConfiguration: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
