namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        ExtractControllerApplicationsAndEntities = 1,
        ExtractControllerSIMApplicationsAndEntities = 2,
        ExtractControllerDBApplicationsAndEntities = 3,
        ExtractControllerAndApplicationConfiguration = 4,
        ExtractUsersGroupsRolesAndPermissions = 5,
        ExtractApplicationAndEntityMetrics = 6,
        ExtractApplicationAndEntityFlowmaps = 7,
        ExtractEventsAndHealthRuleViolations = 8,
        ExtractSnapshots = 9,

        IndexControllerApplicationsAndEntities = 11,
        IndexControllerSIMApplicationsAndEntities = 12,
        IndexControllerDBApplicationsAndEntities = 13,
        IndexControllerAndApplicationConfiguration = 14,
        IndexApplicationConfigurationComparison = 15,
        IndexUsersGroupsRolesAndPermissions = 16,
        IndexApplicationAndEntityMetrics = 17,
        IndexApplicationAndEntityFlowmaps = 18,
        IndexEventsAndHealthRuleViolations = 19,
        IndexSnapshots = 20,

        ReportControlerApplicationsAndEntities = 21,
        ReportControlerSIMApplicationsAndEntities = 22,
        ReportControlerDBApplicationsAndEntities = 23,
        ReportControllerAndApplicationConfiguration = 24,
        ReportUsersGroupsRolesAndPermissions = 25,
        ReportApplicationAndEntityMetrics = 26,
        ReportApplicationAndEntityMetricGraphs = 27,
        ReportEventsAndHealthRuleViolations = 28,
        ReportSnapshots = 29,
        ReportSnapshotsMethodCallLines = 30,
        ReportIndividualApplicationAndEntityDetails = 31,
        ReportFlameGraphs = 32,

        Done = 50,

        Error = 100
    }
}
