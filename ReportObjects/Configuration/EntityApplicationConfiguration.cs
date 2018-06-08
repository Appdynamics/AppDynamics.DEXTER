using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntityApplicationConfiguration : ConfigurationEntityBase
    {
        public string ApplicationDescription { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBTEntryRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBTExcludeRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBTDiscoveryRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBT20Scopes { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBT20EntryRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBT20ExcludeRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBT20DiscoveryRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBackendRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumInfoPointRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumAgentProps { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumHealthRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumErrorRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumHTTPDCVariablesCollected { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumHTTPDCs { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumMIDCVariablesCollected { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumMIDCs { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBaselines { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumTiers { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBTs { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBT20ConfigEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsHREngineEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDeveloperModeEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBTLockdownEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAsyncSupported { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int SnapshotEvalInterval { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int SnapshotQuietTime { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTSLAConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTSnapshotCollectionConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTRequestThresholdConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTBackgroundSnapshotCollectionConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTBackgroundRequestThresholdConfig { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string EUMConfigExclude { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string EUMConfigPage { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string EUMConfigMobilePage { get; set; }
        [FieldComparison(FieldComparisonType.JSONValueComparison)]
        public string EUMConfigMobileAgent { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string AnalyticsConfig { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string WorkflowsConfig { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string TasksConfig { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTGroupsConfig { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string MetricBaselinesConfig { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string ErrorAgentConfig { get; set; }

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
                return "ApplicationConfiguration";
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
                "EntityApplicationConfiguration: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
