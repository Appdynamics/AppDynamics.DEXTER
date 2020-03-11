namespace AppDynamics.Dexter
{
    public enum JobStatus
    {
        // Extract steps
        ExtractControllerVersionAndApplications = 1,
        ExtractControllerConfiguration = 2,
        ExtractControllerUsersGroupsRolesAndPermissions = 3,
        ExtractDashboards = 4,
        ExtractLicenses = 5,

        ExtractControllerAuditEventsAndNotifications = 6,
        ExtractApplicationEventsAndHealthRuleViolations = 7,

        ExtractApplicationHealthRulesAlertsPolicies = 10,
        ExtractAPMConfiguration = 11,
        ExtractDBConfiguration = 12,
        ExtractWEBConfiguration = 13,
        ExtractMOBILEConfiguration = 14,
        ExtractBIQConfiguration = 15,

        ExtractAPMEntities = 20,
        ExtractSIMEntities = 21,
        ExtractDBEntities = 22,
        ExtractWEBEntities = 23,
        ExtractMOBILEEntities = 24,
        ExtractBIQEntities = 25,

        ExtractAPMMetrics = 30,
        ExtractAPMFlowmaps = 31,
        ExtractAPMEntityDashboardScreenshots = 32,
        ExtractAPMSnapshots = 33,

        // Index steps
        IndexControllerVersionAndApplications = 50,
        IndexControllerConfiguration = 51,
        IndexControllerUsersGroupsRolesAndPermissions = 52,

        IndexDashboards = 53,
        IndexLicenses = 54,

        IndexControllerAuditEventsAndNotifications = 55,
        IndexApplicationEventsAndHealthRuleViolations = 56,

        IndexAPMEntities = 60,
        IndexSIMEntities = 61,
        IndexDBEntities = 62,
        IndexWEBEntities = 63,
        IndexMOBILEEntities = 64,
        IndexBIQEntities = 65,

        IndexApplicationHealthRulesAlertsPolicies = 70,
        IndexAPMConfiguration = 71,
        IndexDBConfiguration = 72,
        IndexWEBConfiguration = 73,
        IndexMOBILEConfiguration = 74,
        IndexBIQConfiguration = 75,

        IndexApplicationConfigurationDifferences = 76,

        IndexAPMMetrics = 80,
        IndexAPMFlowmaps = 81,
        IndexAPMSnapshots = 82,

        IndexControllerHealthCheck = 90,
        IndexAPMHealthCheck = 91,

        // Report steps
        ReportControllerAndApplicationConfiguration = 100,
        ReportControllerUsersGroupsRolesAndPermissions = 101,

        ReportDashboards = 102,
        ReportLicenses = 103,

        ReportApplicationEventsAndHealthRuleViolations = 104,

        ReportAPMEntities = 110,
        ReportSIMEntities = 111,
        ReportDBEntities = 112,
        ReportWEBEntities = 113,
        ReportMOBILEEntities = 114,
        ReportBIQEntities = 115,

        ReportAPMMetrics = 120,
        ReportAPMMetricGraphs = 121,

        ReportAPMSnapshots = 130,
        ReportAPMSnapshotsMethodCallLines = 131,
        ReportAPMFlameGraphs = 132,

        ReportAPMEntityDetails = 133,
        
        ReportHealthCheck = 140,
        ReportAPMApplicationSummary = 141,

        ReportAPMEntityDashboardScreenshots = 150,

        // The rest
        Done = 500,

        Error = -1
    }
}
