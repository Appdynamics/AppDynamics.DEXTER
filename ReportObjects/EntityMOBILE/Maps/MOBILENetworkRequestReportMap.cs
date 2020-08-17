using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MOBILENetworkRequestReportMap : ClassMap<MOBILENetworkRequest>
    {
        public MOBILENetworkRequestReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.RequestName).Index(i); i++;
            Map(m => m.RequestNameInternal).Index(i); i++;

            Map(m => m.UserExperience).Index(i); i++;
            Map(m => m.Platform).Index(i); i++;

            Map(m => m.IsExcluded).Index(i); i++;
            Map(m => m.IsCorrelated).Index(i); i++;

            Map(m => m.NumBTs).Index(i); i++;

            Map(m => m.ART).Index(i); i++;
            Map(m => m.ARTRange).Index(i); i++;
            Map(m => m.TimeTotal).Index(i); i++;
            Map(m => m.Calls).Index(i); i++;
            Map(m => m.CPM).Index(i); i++;

            Map(m => m.Server).Index(i); i++;

            Map(m => m.HttpErrors).Index(i); i++;
            Map(m => m.HttpEPM).Index(i); i++;
            Map(m => m.NetworkErrors).Index(i); i++;
            Map(m => m.NetworkEPM).Index(i); i++;

            Map(m => m.HasActivity).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.RequestLink).Index(i); i++;
            Map(m => m.MetricLink).Index(i); i++;
        }
    }
}