using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class SIMMachineCPUReportMap : ClassMap<SIMMachineCPU>
    {
        public SIMMachineCPUReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;

            Map(m => m.CPUID).Index(i); i++;
            Map(m => m.NumCores).Index(i); i++;
            Map(m => m.NumLogical).Index(i); i++;
            Map(m => m.Vendor).Index(i); i++;
            Map(m => m.Flags).Index(i); i++;
            Map(m => m.NumFlags).Index(i); i++;
            Map(m => m.Model).Index(i); i++;
            Map(m => m.Speed).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
        }
    }
}
