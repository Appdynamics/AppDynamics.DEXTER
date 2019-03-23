using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class SIMMachineVolumeReportMap : ClassMap<SIMMachineVolume>
    {
        public SIMMachineVolumeReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;

            Map(m => m.MountPoint).Index(i); i++;
            Map(m => m.Partition).Index(i); i++;
            Map(m => m.PartitionMetricName).Index(i); i++;
            Map(m => m.VolumeMetricName).Index(i); i++;
            Map(m => m.SizeMB).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
        }
    }
}
