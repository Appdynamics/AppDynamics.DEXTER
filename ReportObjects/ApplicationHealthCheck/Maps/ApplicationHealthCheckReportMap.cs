using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ApplicationHealthCheckReportMap : ClassMap<ApplicationHealthCheck>
    {
        public ApplicationHealthCheckReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
        
            Map(m => m.NumTiers).Index(i); i++;
            Map(m => m.NumBTs).Index(i); i++;

            Map(m => m.CustomBTRules).Index(i); i++;
            Map(m => m.BTOverflow).Index(i); i++;
            Map(m => m.BTLockdownEnabled).Index(i); i++;
            Map(m => m.DeveloperModeOff).Index(i); i++;
            Map(m => m.BTErrorRateHigh).Index(i); i++;
            Map(m => m.CustomSEPRules).Index(i); i++;
            Map(m => m.BackendOverflow).Index(i); i++;

            Map(m => m.NumDataCollectorsEnabled).Index(i); i++;
            Map(m => m.NumInfoPoints).Index(i); i++;

            Map(m => m.HRViolationsHigh).Index(i); i++;
            Map(m => m.PoliciesActionsEnabled).Index(i); i++;

            Map(m => m.AppAgentVersion).Index(i); i++;
            Map(m => m.MachineAgentVersion).Index(i); i++;
            Map(m => m.MachineAgentEnabledPercent).Index(i); i++;

            Map(m => m.TiersActivePercent).Index(i); i++;
            Map(m => m.NodesActivePercent).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;

        }
    }
}