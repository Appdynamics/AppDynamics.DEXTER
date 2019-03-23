using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class SIMMachineContainerReportMap : ClassMap<SIMMachineContainer>
    {
        public SIMMachineContainerReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;

            Map(m => m.ContainerID).Index(i); i++;
            Map(m => m.ContainerName).Index(i); i++;
            Map(m => m.ImageName).Index(i); i++;
            Map(m => m.ContainerMachineID).Index(i); i++;
            Map(m => m.StartedAt).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
        }
    }
}
