using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ServiceEndpointEntityReportMap : ClassMap<EntityServiceEndpoint>
    {
        public ServiceEndpointEntityReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.SEPName).Index(i); i++;
            Map(m => m.SEPType).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.SEPID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.SEPLink).Index(i); i++;
        }
    }
}
