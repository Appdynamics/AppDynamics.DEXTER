using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        ExtractControllerApplicationsAndEntities = 1,
        ExtractControllerAndApplicationConfiguration = 2,
        ExtractApplicationAndEntityMetrics = 3,
        ExtractApplicationAndEntityFlowmaps = 4,
        ExtractSnapshots = 5,
        ExtractEvents = 6,

        IndexControllersApplicationsAndEntities = 11,
        IndexControllerAndApplicationConfiguration = 12,
        IndexApplicationAndEntityMetrics = 13,
        IndexSnapshots = 14,
        IndexEvents = 15,
        IndexApplicationAndEntityFlowmaps = 16,

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
