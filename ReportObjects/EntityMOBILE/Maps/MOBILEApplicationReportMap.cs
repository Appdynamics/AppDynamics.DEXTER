using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MOBILEApplicationReportMap : ClassMap<MOBILEApplication>
    {
        public MOBILEApplicationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.NumNetworkRequests).Index(i); i++;

            Map(m => m.NumActivity).Index(i); i++;
            Map(m => m.NumNoActivity).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
