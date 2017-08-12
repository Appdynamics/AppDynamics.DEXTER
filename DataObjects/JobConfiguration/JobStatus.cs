namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        ExtractControllerApplicationsAndEntities = 1,
        ExtractControllerAndApplicationConfiguration = 2,
        ExtractApplicationAndEntityMetrics = 3,
        ExtractApplicationAndEntityFlowmaps = 4,
        ExtractSnapshots = 5,

        IndexControllersApplicationsAndEntities = 11,
        IndexControllerAndApplicationConfiguration = 12,
        IndexApplicationAndEntityMetrics = 13,
        IndexApplicationAndEntityFlowmaps = 14,
        IndexSnapshots = 15,

        ReportControlerApplicationsAndEntities = 21,
        ReportControllerAndApplicationConfiguration = 22,
        ReportApplicationAndEntityMetrics = 23,
        ReportApplicationAndEntityMetricDetails = 24,
        ReportApplicationAndEntityFlowmaps = 25,
        ReportSnapshots = 26,
        ReportFlameGraphs = 27,

        Done = 30,

        Error = 100
    }
}
