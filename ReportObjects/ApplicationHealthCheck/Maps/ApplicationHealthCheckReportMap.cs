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

            Map(m => m.IsCustomBTRules).Index(i); i++;
            Map(m => m.IsBTOverflow).Index(i); i++;
            Map(m => m.IsBTLockdownEnabled).Index(i); i++;
            Map(m => m.IsDeveloperModeEnabled).Index(i); i++;
            Map(m => m.IsBTErrorRateHigh).Index(i); i++;
            Map(m => m.IsBackendOverflow).Index(i); i++;
            Map(m => m.NumDataCollectorsEnabled).Index(i); i++;
            Map(m => m.NumInfoPoints).Index(i); i++;

            Map(m => m.IsHRViolationsHigh).Index(i); i++;
            Map(m => m.IsPoliciesAndActionsEnabled).Index(i); i++;

            Map(m => m.AppAgentVersion).Index(i); i++;
            Map(m => m.MachineAgentVersion).Index(i); i++;
            Map(m => m.IsMachineAgentEnabled).Index(i); i++;

            Map(m => m.PercentActiveTiers).Index(i); i++;
            Map(m => m.PercentActiveNodes).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;

        }
    }
}