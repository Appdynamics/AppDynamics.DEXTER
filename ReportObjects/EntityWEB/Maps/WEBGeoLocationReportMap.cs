using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class WEBGeoLocationReportMap : ClassMap<WEBGeoLocation>
    {
        public WEBGeoLocationReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.LocationType).Index(i); i++;
            Map(m => m.LocationName).Index(i); i++;

            Map(m => m.Country).Index(i); i++;
            Map(m => m.Region).Index(i); i++;
            Map(m => m.City).Index(i); i++;

            Map(m => m.GeoCode).Index(i); i++;
            Map(m => m.Latitude).Index(i); i++;
            Map(m => m.Longitude).Index(i); i++;

            Map(m => m.ART).Index(i); i++;
            Map(m => m.ARTRange).Index(i); i++;
            Map(m => m.Calls).Index(i); i++;
            Map(m => m.CPM).Index(i); i++;

            Map(m => m.DOMReady).Index(i); i++;
            Map(m => m.FirstByte).Index(i); i++;
            Map(m => m.ServerConnection).Index(i); i++;
            Map(m => m.HTMLDownload).Index(i); i++;
            Map(m => m.DOMBuild).Index(i); i++;
            Map(m => m.ResourceFetch).Index(i); i++;

            Map(m => m.JSErrors).Index(i); i++;
            Map(m => m.JSEPM).Index(i); i++;
            Map(m => m.AJAXEPM).Index(i); i++;

            Map(m => m.HasActivity).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}