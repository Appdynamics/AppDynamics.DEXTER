namespace AppDynamics.OfflineData.JobParameters
{
    public enum JobTargetStatus
    {
        InvalidConfiguration = -3,
        NoController = -2,
        NoApplication = -1,
        ExtractApplications = 1,
        ExtractEntities = 2,
        ExtractConfig = 3,
        ExtractMetrics = 4,
        ExtractFlowmaps = 5,
        ExtractSnapshots = 6,
        ConvertApplications = 10,
        ConvertConfiguration = 10,
        ConvertEntities = 11,
        ConvertConfig = 12,
        ConvertMetrics = 13,
        ConvertFlowmaps = 14,
        ConvertSnapshots = 15,
        OutputControllers = 20,
        Done = 30
    }
}
