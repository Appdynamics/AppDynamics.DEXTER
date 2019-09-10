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
            Map(m => m.ApplicationID).Index(i); i++;
        
            Map(m => m.NumTiers).Index(i); i++;
            Map(m => m.NumBTs).Index(i); i++;

            Map(m => m.IsDeveloperModeEnabled).Index(i); i++;
            Map(m => m.IsBTLockdownEnabled).Index(i); i++;

        }
    }
}