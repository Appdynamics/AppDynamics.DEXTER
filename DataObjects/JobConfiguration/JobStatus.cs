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
        IndexApplicationAndEntityFlowmaps = 14,
        IndexSnapshots = 15,
        IndexEvents = 16,

        ReportControlerApplicationsAndEntities = 21,
        ReportControllerAndApplicationConfiguration = 22,
        ReportEventsAndHealthRuleViolations = 23,
        ReportApplicationAndEntityMetrics = 24,
        ReportSnapshots = 25,
        ReportIndividualApplicationAndEntityDetails = 26,
        ReportFlameGraphs = 27,

        Done = 30,

        Error = 100
    }
}
