using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class WEBApplicationConfigurationReportMap : ClassMap<WEBApplicationConfiguration>
    {
        public WEBApplicationConfigurationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationDescription).Index(i); i++;

            Map(m => m.ApplicationKey).Index(i); i++;

            Map(m => m.NumPageRulesInclude).Index(i); i++;
            Map(m => m.NumPageRulesExclude).Index(i); i++;
            Map(m => m.NumVirtPageRulesInclude).Index(i); i++;
            Map(m => m.NumVirtPageRulesExclude).Index(i); i++;
            Map(m => m.NumAJAXRulesInclude).Index(i); i++;
            Map(m => m.NumAJAXRulesExclude).Index(i); i++;
            Map(m => m.NumSyntheticJobs).Index(i); i++;

            Map(m => m.AgentHTTP).Index(i); i++;
            Map(m => m.AgentHTTPS).Index(i); i++;
            Map(m => m.GeoHTTP).Index(i); i++;
            Map(m => m.GeoHTTPS).Index(i); i++;
            Map(m => m.BeaconHTTP).Index(i); i++;
            Map(m => m.BeaconHTTPS).Index(i); i++;

            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsXsccEnabled).Index(i); i++;
            Map(m => m.HostOption).Index(i); i++;

            Map(m => m.AgentCode).Index(i); i++;

            Map(m => m.IsJSErrorEnabled).Index(i); i++;
            Map(m => m.IsAJAXErrorEnabled).Index(i); i++;
            Map(m => m.IgnoreJSErrors).Index(i); i++;
            Map(m => m.IgnorePageNames).Index(i); i++;
            Map(m => m.IgnoreURLs).Index(i); i++;

            Map(m => m.SlowThresholdType).Index(i); i++;
            Map(m => m.SlowThreshold).Index(i); i++;
            Map(m => m.VerySlowThresholdType).Index(i); i++;
            Map(m => m.VerySlowThreshold).Index(i); i++;
            Map(m => m.StallThresholdType).Index(i); i++;
            Map(m => m.StallThreshold).Index(i); i++;

            Map(m => m.Percentiles).Index(i); i++;
            Map(m => m.SessionTimeout).Index(i); i++;
            Map(m => m.IsIPDisplayed).Index(i); i++;

            Map(m => m.EnableSlowSnapshots).Index(i); i++;
            Map(m => m.EnablePeriodicSnapshots).Index(i); i++;
            Map(m => m.EnableErrorSnapshots).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}