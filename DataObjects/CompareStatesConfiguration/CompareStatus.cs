namespace AppDynamics.Dexter
{
    public enum CompareStatus
    {
        // Compare Steps
        CompareAPMConfiguration = 1,
        //CompareDBConfiguration = 2,
        //CompareWEBConfiguration = 3,
        //CompareMOBILEConfiguration = 4,
        //CompareBIQConfiguration = 5,

        CompareAPMEntities = 10,
        //CompareSIMEntities = 21,
        //CompareDBEntities = 22,
        //CompareWEBEntities = 23,
        //CompareMOBILEEntities = 24,
        //CompareBIQEntities = 25,

        CompareAPMMetrics = 20,
        //CompareSIMMetrics = 21,
        //CompareDBMetrics = 22,
        //CompareWEBMetrics = 23,
        //CompareMOBILEMetrics = 24,
        //CompareBIQMetrics = 25,

        CompareAPMFlowmaps = 30,

        CompareAPMSnapshots = 40,

        // Report steps
        ReportConfigurationDifferences = 100,

        ReportAPMEntitiesDifferences = 110,
        //ReportSIMEntities = 111,
        //ReportDBEntities = 112,
        //ReportWEBEntities = 113,
        //ReportMOBILEEntities = 114,
        //ReportBIQEntities = 115,

        ReportAPMMetricsDifferences = 120,
        ReportAPMMetricGraphsDifferences = 121,

        ReportAPMFlowmapsDifferences = 122,

        ReportAPMSnapshotsDifferences = 130,
        ReportAPMFlameGraphsDifferences = 132,

        // The rest
        Done = 500,

        Error = -1
    }
}
