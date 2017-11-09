using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityApplicationConfigurationReportMap : CsvClassMap<EntityApplicationConfiguration>
    {
        public EntityApplicationConfigurationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationDescription).Index(i); i++;

            Map(m => m.NumBTDiscoveryRules).Index(i); i++;
            Map(m => m.NumBTEntryRules).Index(i); i++;
            Map(m => m.NumBTExcludeRules).Index(i); i++;
            Map(m => m.NumBT20Scopes).Index(i); i++;
            Map(m => m.NumBT20DiscoveryRules).Index(i); i++;
            Map(m => m.NumBT20EntryRules).Index(i); i++;
            Map(m => m.NumBT20ExcludeRules).Index(i); i++;
            Map(m => m.NumBackendRules).Index(i); i++;
            Map(m => m.NumInfoPointRules).Index(i); i++;
            Map(m => m.NumAgentProps).Index(i); i++;
            Map(m => m.NumHealthRules).Index(i); i++;
            Map(m => m.NumErrorRules).Index(i); i++;
            Map(m => m.NumHTTPDCVariablesCollected).Index(i); i++;
            Map(m => m.NumHTTPDCs).Index(i); i++;
            Map(m => m.NumMIDCVariablesCollected).Index(i); i++;
            Map(m => m.NumMIDCs).Index(i); i++;
            Map(m => m.NumBaselines).Index(i); i++;
            Map(m => m.NumTiers).Index(i); i++;
            Map(m => m.NumBTs).Index(i); i++;

            Map(m => m.IsBT20ConfigEnabled).Index(i); i++;
            Map(m => m.IsHREngineEnabled).Index(i); i++;
            Map(m => m.IsDeveloperModeEnabled).Index(i); i++;
            Map(m => m.IsBTLockdownEnabled).Index(i); i++;
            Map(m => m.IsAsyncSupported).Index(i); i++;
            Map(m => m.SnapshotEvalInterval).Index(i); i++;
            Map(m => m.SnapshotQuietTime).Index(i); i++;

            Map(m => m.BTSLAConfig).Index(i); i++;
            Map(m => m.BTSnapshotCollectionConfig).Index(i); i++;
            Map(m => m.BTRequestThresholdConfig).Index(i); i++;
            Map(m => m.BTBackgroundSnapshotCollectionConfig).Index(i); i++;
            Map(m => m.BTBackgroundRequestThresholdConfig).Index(i); i++;

            Map(m => m.EUMConfigExclude).Index(i); i++;
            Map(m => m.EUMConfigPage).Index(i); i++;
            Map(m => m.EUMConfigMobilePage).Index(i); i++;
            Map(m => m.EUMConfigMobileAgent).Index(i); i++;

            Map(m => m.AnalyticsConfig).Index(i); i++;
            Map(m => m.WorkflowsConfig).Index(i); i++;
            Map(m => m.TasksConfig).Index(i); i++;
            Map(m => m.BTGroupsConfig).Index(i); i++;
            Map(m => m.MetricBaselinesConfig).Index(i); i++;
            Map(m => m.ErrorAgentConfig).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}