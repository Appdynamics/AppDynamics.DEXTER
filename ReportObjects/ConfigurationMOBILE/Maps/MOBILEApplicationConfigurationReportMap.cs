using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MOBILEApplicationConfigurationReportMap : ClassMap<MOBILEApplicationConfiguration>
    {
        public MOBILEApplicationConfigurationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.ApplicationDescription).Index(i); i++;

            Map(m => m.ApplicationKey).Index(i); i++;

            Map(m => m.NumNetworkRulesInclude).Index(i); i++;
            Map(m => m.NumNetworkRulesExclude).Index(i); i++;

            Map(m => m.IsEnabled).Index(i); i++;

            Map(m => m.SlowThresholdType).Index(i); i++;
            Map(m => m.SlowThreshold).Index(i); i++;
            Map(m => m.VerySlowThresholdType).Index(i); i++;
            Map(m => m.VerySlowThreshold).Index(i); i++;
            Map(m => m.StallThresholdType).Index(i); i++;
            Map(m => m.StallThreshold).Index(i); i++;

            Map(m => m.Percentiles).Index(i); i++;
            Map(m => m.SessionTimeout).Index(i); i++;

            Map(m => m.CrashThreshold).Index(i); i++;

            Map(m => m.IsIPDisplayed).Index(i); i++;
            Map(m => m.EnableScreenshot).Index(i); i++;
            Map(m => m.AutoScreenshot).Index(i); i++;
            Map(m => m.UseCellular).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}