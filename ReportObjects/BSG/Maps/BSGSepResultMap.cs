using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGSepResultMap : ClassMap<BSGSepResult>
    {
        public BSGSepResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.NumServiceEndpoints).Index(i); i++;
            Map(m => m.NumServiceEndpointsWithLoad).Index(i); i++;
            Map(m => m.NumServiceEndpointDetectionRules).Index(i); i++;
            
            Map(m => m.EjbAutoDiscoveryEnabled).Index(i); i++;
            Map(m => m.JmsAutoDiscoveryEnabled).Index(i); i++;
            Map(m => m.PojoAutoDiscoveryEnabled).Index(i); i++;
            Map(m => m.ServletAutoDiscoveryEnabled).Index(i); i++;
            Map(m => m.SpringBeanAutoDiscoveryEnabled).Index(i); i++;
            Map(m => m.StrutsAutoDiscoveryEnabled).Index(i); i++;
            Map(m => m.WebServiceAutoDiscoveryEnabled).Index(i); i++;
            
        }
    }
}
