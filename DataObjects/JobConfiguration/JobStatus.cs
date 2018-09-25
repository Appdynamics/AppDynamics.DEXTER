namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        ExtractControllerApplicationsAndEntities = 1,
        ExtractControllerSIMApplicationsAndEntities = 2,
        ExtractControllerDBApplicationsAndEntities = 3,
        ExtractControllerAndApplicationConfiguration = 4,
        ExtractApplicationAndEntityMetrics = 5,
        ExtractApplicationAndEntityFlowmaps = 6,
        ExtractEventsAndHealthRuleViolations = 7,
        ExtractSnapshots = 8,

        IndexControllerApplicationsAndEntities = 11,
        IndexControllerSIMApplicationsAndEntities = 12,
        IndexControllerDBApplicationsAndEntities = 13,
        IndexControllerAndApplicationConfiguration = 14,
        IndexApplicationConfigurationComparison = 15,
        IndexApplicationAndEntityMetrics = 16,
        IndexApplicationAndEntityFlowmaps = 17,
        IndexEventsAndHealthRuleViolations = 18,
        IndexSnapshots = 19,

        ReportControlerApplicationsAndEntities = 21,
        ReportControlerSIMApplicationsAndEntities = 22,
        ReportControlerDBApplicationsAndEntities = 23,
        ReportControllerAndApplicationConfiguration = 24,
        ReportApplicationAndEntityMetrics = 25,
        ReportApplicationAndEntityMetricGraphs = 26,
        ReportEventsAndHealthRuleViolations = 27,
        ReportSnapshots = 28,
        ReportSnapshotsMethodCallLines = 29,
        ReportIndividualApplicationAndEntityDetails = 30,
        ReportFlameGraphs = 31,

        Done = 50,

        Error = 100
    }
}
