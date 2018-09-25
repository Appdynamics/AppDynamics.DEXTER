using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MachineProcessReportMap : ClassMap<MachineProcess>
    {
        public MachineProcessReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;

            Map(m => m.Class).Index(i); i++;
            Map(m => m.ClassID).Index(i); i++;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.CommandLine).Index(i); i++;
            Map(m => m.RealUser).Index(i); i++;
            Map(m => m.RealGroup).Index(i); i++;
            Map(m => m.EffectiveUser).Index(i); i++;
            Map(m => m.EffectiveGroup).Index(i); i++;
            Map(m => m.State).Index(i); i++;
            Map(m => m.NiceLevel).Index(i); i++;

            Map(m => m.StartTime).Index(i); i++;
            Map(m => m.EndTime).Index(i); i++;

            Map(m => m.PID).Index(i); i++;
            Map(m => m.ParentPID).Index(i); i++;
            Map(m => m.PGID).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
        }
    }
}
