using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class TierConfigurationReportMap : ClassMap<TierConfiguration>
    {
        public TierConfigurationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierType).Index(i); i++;
            Map(m => m.TierDescription).Index(i); i++;

            Map(m => m.NumBTs).Index(i); i++;
            Map(m => m.NumBTTypes).Index(i); i++;

            Map(m => m.IsDynamicScalingEnabled).Index(i); i++;

            Map(m => m.MemoryConfig).Index(i); i++;
            Map(m => m.CacheConfig).Index(i); i++;
            Map(m => m.CustomCacheConfig).Index(i); i++;

            Map(m => m.TierID).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}