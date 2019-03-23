using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ConfigurationDifferenceReportMap : ClassMap<ConfigurationDifference>
    {
        public ConfigurationDifferenceReportMap()
        {
            int i = 0;
            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.RuleType).Index(i); i++;
            Map(m => m.RuleSubType).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;

            Map(m => m.ReferenceConroller).Index(i); i++;
            Map(m => m.ReferenceApp).Index(i); i++;
            Map(m => m.DifferenceController).Index(i); i++;
            Map(m => m.DifferenceApp).Index(i); i++;

            Map(m => m.Difference).Index(i); i++;

            Map(m => m.Property).Index(i); i++;
            Map(m => m.ReferenceValue).Index(i); i++;
            Map(m => m.DifferenceValue).Index(i); i++;

        }
    }
}