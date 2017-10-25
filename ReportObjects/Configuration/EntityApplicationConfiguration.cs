using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityApplicationConfiguration
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationDescription { get; set; }

        public int NumBTEntryRules { get; set; }
        public int NumBTExcludeRules { get; set; }
        public int NumBTDiscoveryRules { get; set; }
        public int NumBackendRules { get; set; }
        public int NumInfoPointRules { get; set; }
        public int NumAgentProps { get; set; }
        public int NumHealthRules { get; set; }
        public int NumErrorRules { get; set; }
        public int NumHTTPDCVariablesCollected { get; set; }
        public int NumHTTPDCs { get; set; }
        public int NumMIDCVariablesCollected { get; set; }
        public int NumMIDCs { get; set; }
        public int NumBaselines { get; set; }
        public int NumMDSScopes { get; set; }
        public int NumMDSRules { get; set; }
        public int NumTiers { get; set; }
        public int NumBTs { get; set; }

        public bool IsMDSEnabled { get; set; }
        public bool IsHREngineEnabled { get; set; }
        public bool IsDeveloperModeEnabled { get; set; }
        public bool IsBTLockdownEnabled { get; set; }
        public bool IsAsyncSupported { get; set; }
        public int SnapshotEvalInterval { get; set; }
        public int SnapshotQuietTime { get; set; }

        public string BTSLAConfig { get; set; }
        public string BTSnapshotCollectionConfig { get; set; }
        public string BTRequestThresholdConfig { get; set; }
        public string BTBackgroundSnapshotCollectionConfig { get; set; }
        public string BTBackgroundRequestThresholdConfig { get; set; }

        public string EUMConfigExclude { get; set; }
        public string EUMConfigPage { get; set; }
        public string EUMConfigMobilePage { get; set; }
        public string EUMConfigMobileAgent { get; set; }

        public string AnalyticsConfig { get; set; }

        public string WorkflowsConfig { get; set; }

        public string TasksConfig { get; set; }

        public string BTGroupsConfig { get; set; }

        public string MetricBaselinesConfig { get; set; }

        public string ErrorAgentConfig { get; set; }

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
