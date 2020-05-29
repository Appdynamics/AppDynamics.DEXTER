using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ApplicationHealthCheckComparisonMap : ClassMap<ApplicationHealthCheckComparisonMapping>
    {
        public ApplicationHealthCheckComparisonMap()
        {
            int i = 0;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Pass).Index(i); i++;
            Map(m => m.Fail).Index(i); i++;
            Map(m => m.EvalCondition).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
        }
    }
}
