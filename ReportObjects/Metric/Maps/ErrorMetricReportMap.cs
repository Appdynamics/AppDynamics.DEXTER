using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ErrorMetricReportMap : ClassMap<EntityError>
    {
        public ErrorMetricReportMap()
        {
            int i = 0; 
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.ErrorName).Index(i); i++;
            Map(m => m.ErrorType).Index(i); i++;
            Map(m => m.Errors).Index(i); i++;
            Map(m => m.EPM).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.ErrorID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.ErrorLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}
