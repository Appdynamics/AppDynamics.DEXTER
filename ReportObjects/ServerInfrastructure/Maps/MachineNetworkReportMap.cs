using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MachineNetworkReportMap : ClassMap<MachineNetwork>
    {
        public MachineNetworkReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.MachineName).Index(i); i++;

            Map(m => m.NetworkName).Index(i); i++;
            Map(m => m.MacAddress).Index(i); i++;
            Map(m => m.IP4Address).Index(i); i++;
            Map(m => m.IP4Gateway).Index(i); i++;
            Map(m => m.IP6Address).Index(i); i++;
            Map(m => m.IP6Gateway).Index(i); i++;

            Map(m => m.Speed).Index(i); i++;
            Map(m => m.Enabled).Index(i); i++;
            Map(m => m.PluggedIn).Index(i); i++;
            Map(m => m.State).Index(i); i++;
            Map(m => m.Duplex).Index(i); i++;
            Map(m => m.MTU).Index(i); i++;

            Map(m => m.NetworkMetricName).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.MachineID).Index(i); i++;
        }
    }
}
