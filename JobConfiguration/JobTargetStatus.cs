namespace AppDynamics.OfflineData.JobParameters
{
    public enum JobTargetStatus
    {
        InvalidConfiguration = -3,
        NoController = -2,
        NoApplication = -1,
        Extract = 0,
        ExtractApplications = 1,
        ExtractEntities = 2,
        ExtractConfiguration = 3,
        ExtractMetrics = 4,
        ExtractFlowmaps = 5,
        ExtractSnapshots = 6,
        Convert = 10,
        ConvertApplications = 11,
        ConvertEntities = 12,
        ConvertConfiguration = 13,
        ConvertMetrics = 14,
        ConvertFlowmaps = 15,
        ConvertSnapshots = 16,
        Report = 20,
        //ReportApplications = 21,
        ReportEntities = 22,
        ReportConfiguration = 23,
        ReportMetrics = 24,
        ReportFlowmaps = 25,
        ReportHourlyGraphs = 26,
        ReportSnapshots = 27,
        ReportFlameGraphs = 28,
        Done = 30
    }
}
