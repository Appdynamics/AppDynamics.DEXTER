using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BusinessTransactionConfigurationReportMap : ClassMap<BusinessTransactionConfiguration>
    {
        public BusinessTransactionConfigurationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.TierName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;

            Map(m => m.IsExcluded).Index(i); i++;
            Map(m => m.IsBackground).Index(i); i++;
            Map(m => m.IsEUMEnabled).Index(i); i++;
            Map(m => m.IsEUMPossible).Index(i); i++;
            Map(m => m.IsAnalyticsEnabled).Index(i); i++;

            Map(m => m.NumAssignedMIDCs).Index(i); i++;
            Map(m => m.AssignedMIDCs).Index(i); i++;

            Map(m => m.BTSLAConfig).Index(i); i++;
            Map(m => m.BTSnapshotCollectionConfig).Index(i); i++;
            Map(m => m.BTRequestThresholdConfig).Index(i); i++;
            Map(m => m.BTBackgroundSnapshotCollectionConfig).Index(i); i++;
            Map(m => m.BTBackgroundRequestThresholdConfig).Index(i); i++;

            Map(m => m.TierID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}