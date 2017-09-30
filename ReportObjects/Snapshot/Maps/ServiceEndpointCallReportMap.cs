using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ServiceEndpointCallReportMap : CsvClassMap<ServiceEndpointCall>
    {
        public ServiceEndpointCallReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.SegmentID).Index(i); i++;
            Map(m => m.SEPName).Index(i); i++;
            Map(m => m.SEPType).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
            Map(m => m.SEPID).Index(i); i++;

            //Map(m => m.ControllerLink).Index(i); i++;
            //Map(m => m.ApplicationLink).Index(i); i++;
            //Map(m => m.TierLink).Index(i); i++;
            //Map(m => m.NodeLink).Index(i); i++;
            //Map(m => m.BTLink).Index(i); i++;
            Map(m => m.SEPLink).Index(i); i++;
        }
    }
}