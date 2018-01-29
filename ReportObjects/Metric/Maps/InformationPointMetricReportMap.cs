using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class InformationPointMetricReportMap : ClassMap<EntityInformationPoint>
    {
        public InformationPointMetricReportMap()
        {
            int i = 0; 
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.IPName).Index(i); i++;
            Map(m => m.IPType).Index(i); i++;
            Map(m => m.ART).Index(i); i++;
            Map(m => m.TimeTotal).Index(i); i++;
            Map(m => m.Calls).Index(i); i++;
            Map(m => m.CPM).Index(i); i++;
            Map(m => m.Errors).Index(i); i++;
            Map(m => m.EPM).Index(i); i++;
            Map(m => m.ErrorsPercentage).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.IPID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.IPLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}
