using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class WEBPageReportMap : ClassMap<WEBPage>
    {
        public WEBPageReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.PageType).Index(i); i++;

            Map(m => m.PageName).Index(i); i++;
            Map(m => m.FirstSegment).Index(i); i++;
            Map(m => m.NumNameSegments).Index(i); i++;

            Map(m => m.IsCorrelated).Index(i); i++;
            Map(m => m.IsCookie).Index(i); i++;
            Map(m => m.IsNavTime).Index(i); i++;
            Map(m => m.IsSynthetic).Index(i); i++;

            Map(m => m.NumBTs).Index(i); i++;
            Map(m => m.NumPages).Index(i); i++;

            Map(m => m.ART).Index(i); i++;
            Map(m => m.ARTRange).Index(i); i++;
            Map(m => m.TimeTotal).Index(i); i++;
            Map(m => m.Calls).Index(i); i++;
            Map(m => m.CPM).Index(i); i++;

            Map(m => m.DOMReady).Index(i); i++;
            Map(m => m.FirstByte).Index(i); i++;
            Map(m => m.ServerConnection).Index(i); i++;
            Map(m => m.DNS).Index(i); i++;
            Map(m => m.TCP).Index(i); i++;
            Map(m => m.SSL).Index(i); i++;
            Map(m => m.Server).Index(i); i++;
            Map(m => m.HTMLDownload).Index(i); i++;
            Map(m => m.DOMBuild).Index(i); i++;
            Map(m => m.ResourceFetch).Index(i); i++;

            Map(m => m.JSErrors).Index(i); i++;
            Map(m => m.JSEPM).Index(i); i++;
            Map(m => m.AJAXErrors).Index(i); i++;
            Map(m => m.AJAXEPM).Index(i); i++;

            Map(m => m.HasActivity).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.PageID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.PageLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}