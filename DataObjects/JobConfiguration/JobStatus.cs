namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        ExtractControllerApplicationsAndEntities = 1,
        ExtractControllerSIMApplicationsAndEntities = 2,
        ExtractControllerAndApplicationConfiguration = 3,
        ExtractApplicationAndEntityMetrics = 4,
        ExtractApplicationAndEntityFlowmaps = 5,
        ExtractEventsAndHealthRuleViolations = 6,
        ExtractSnapshots = 7,

        IndexControllerApplicationsAndEntities = 11,
        IndexControllerSIMApplicationsAndEntities = 12,
        IndexControllerAndApplicationConfiguration = 13,
        IndexApplicationConfigurationComparison = 14,
        IndexApplicationAndEntityMetrics = 15,
        IndexApplicationAndEntityFlowmaps = 16,
        IndexEventsAndHealthRuleViolations = 17,
        IndexSnapshots = 18,

        ReportControlerApplicationsAndEntities = 21,
        ReportControlerSIMApplicationsAndEntities = 22,
        ReportControllerAndApplicationConfiguration = 23,
        ReportApplicationAndEntityMetrics = 24,
        ReportApplicationAndEntityMetricGraphs = 25,
        ReportEventsAndHealthRuleViolations = 26,
        ReportSnapshots = 27,
        ReportSnapshotsMethodCallLines = 28,
        ReportIndividualApplicationAndEntityDetails = 29,
        ReportFlameGraphs = 30,

        Done = 50,

        Error = 100
    }
}
