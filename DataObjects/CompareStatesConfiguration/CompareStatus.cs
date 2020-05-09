namespace AppDynamics.Dexter
{
    public enum CompareStatus
    {
        // Compare Steps
        CompareAPMConfiguration = 1,
        //CompareDBConfiguration = 2,
        CompareWEBConfiguration = 3,
        CompareMOBILEConfiguration = 4,
        //CompareBIQConfiguration = 5,

        CompareUserGroupsRolesPermissions = 10,
        CompareDashboards = 11,
        CompareLicenses = 12,

        CompareAPMEntities = 20,
        //CompareSIMEntities = 21,
        //CompareDBEntities = 22,
        CompareWEBEntities = 23,
        CompareMOBILEEntities = 24,
        //CompareBIQEntities = 25,

        CompareAPMMetrics = 30,
        //CompareSIMMetrics = 21,
        //CompareDBMetrics = 22,
        //CompareWEBMetrics = 23,
        //CompareMOBILEMetrics = 24,
        //CompareBIQMetrics = 25,

        CompareAPMFlowmaps = 40,

        CompareAPMSnapshots = 50,

        // Report steps
        ReportConfigurationDifferences = 100,

        ReportUserGroupsRolesPermissionsDifferences = 110,
        ReportDashboardsDifferences = 111,
        ReportLicensesDifferences = 112,

        ReportAPMEntitiesDifferences = 120,
        //ReportSIMEntities = 111,
        //ReportDBEntities = 112,
        ReportWEBEntitiesDifferences = 123,
        ReportMOBILEEntities = 124,
        //ReportBIQEntities = 115,

        ReportAPMMetricsDifferences = 130,
        ReportAPMMetricGraphsDifferences = 131,

        ReportAPMFlowmapsDifferences = 133,

        ReportAPMSnapshotsDifferences = 140,
        ReportAPMFlameGraphsDifferences = 141,

        // The rest
        Done = 500,

        Error = -1
    }
}
