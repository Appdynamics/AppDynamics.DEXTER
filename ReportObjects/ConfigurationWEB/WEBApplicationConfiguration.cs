using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBApplicationConfiguration : ConfigurationEntityBase
    {
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ApplicationDescription { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ApplicationKey { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumPageRulesInclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumPageRulesExclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumVirtPageRulesInclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumVirtPageRulesExclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumAJAXRulesInclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumAJAXRulesExclude { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumSyntheticJobs { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AgentCode { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AgentHTTP { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AgentHTTPS { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string GeoHTTP { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string GeoHTTPS { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string BeaconHTTP { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string BeaconHTTPS { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsXsccEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int HostOption { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsJSErrorEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAJAXErrorEnabled { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string IgnoreJSErrors { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string IgnorePageNames { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string IgnoreURLs { get; set; }

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
        public bool IsIPDisplayed { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool EnableSlowSnapshots { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool EnablePeriodicSnapshots { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool EnableErrorSnapshots { get; set; }

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
                return "WEBApplicationConfiguration";
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
                "WEBApplicationConfiguration: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
