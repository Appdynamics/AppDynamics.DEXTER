using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class WEBApplicationReportMap : ClassMap<WEBApplication>
    {
        public WEBApplicationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.NumPages).Index(i); i++;
            Map(m => m.NumAJAXRequests).Index(i); i++;
            Map(m => m.NumVirtualPages).Index(i); i++;
            Map(m => m.NumIFrames).Index(i); i++;

            Map(m => m.NumRealGeoLocations).Index(i); i++;
            Map(m => m.NumRealGeoLocationsRegion).Index(i); i++;

            Map(m => m.NumSynthGeoLocations).Index(i); i++;
            Map(m => m.NumSynthGeoLocationsRegion).Index(i); i++;

            Map(m => m.NumActivity).Index(i); i++;
            Map(m => m.NumNoActivity).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
