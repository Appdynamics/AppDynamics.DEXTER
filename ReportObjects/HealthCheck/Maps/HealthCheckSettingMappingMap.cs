using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class HealthCheckSettingMappingMap : ClassMap<HealthCheckSettingMapping>
    {
        public HealthCheckSettingMappingMap()
        {
            int i = 0;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Value).Index(i); i++;
            Map(m => m.DataType).Index(i); i++;
        }
    }
}
