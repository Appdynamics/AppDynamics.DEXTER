using System.Collections.Generic;

namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        ExtractControllerApplicationsAndEntities = 1,
        ExtractControllerAndApplicationConfiguration = 2,
        ExtractApplicationAndEntityMetrics = 3,
        ExtractApplicationAndEntityFlowmaps = 4,
        ExtractEventsAndHealthRuleViolations = 5,
        ExtractSnapshots = 6,

        IndexControllersApplicationsAndEntities = 11,
        IndexControllerAndApplicationConfiguration = 12,
        IndexApplicationAndEntityMetrics = 13,
        IndexApplicationAndEntityFlowmaps = 14,
        IndexEventsAndHealthRuleViolations = 15,
        IndexSnapshots = 16,

        ReportControlerApplicationsAndEntities = 21,
        ReportControllerAndApplicationConfiguration = 22,
        ReportApplicationAndEntityMetrics = 23,
        ReportApplicationAndEntityMetricGraphs = 24,
        ReportEventsAndHealthRuleViolations = 25,
        ReportSnapshots = 26,
        ReportIndividualApplicationAndEntityDetails = 27,
        ReportFlameGraphs = 28,

        Done = 30,

        Error = 100
    }
}
