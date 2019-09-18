using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ApplicationHealthCheckComparisonMap : ClassMap<ApplicationHealthCheckComparison>
    {
        public ApplicationHealthCheckComparisonMap()
        {
            int i = 0;
            Map(m => m.LatestAppAgentVersion).Index(i); i++;
            Map(m => m.LatestMachineAgentVersion).Index(i); i++;
            Map(m => m.AgentOldPercent).Index(i); i++;
            Map(m => m.InfoPointUpper).Index(i); i++;
            Map(m => m.InfoPointLower).Index(i); i++;
            Map(m => m.InfoPointLower).Index(i); i++;
            Map(m => m.DCUpper).Index(i); i++;
            Map(m => m.DCLower).Index(i); i++;

        }
    }
}
