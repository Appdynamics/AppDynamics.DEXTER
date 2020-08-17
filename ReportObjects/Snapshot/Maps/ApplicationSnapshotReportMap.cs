using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ApplicationSnapshotReportMap : ClassMap<APMApplication>
    {
        public ApplicationSnapshotReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.NumSnapshots).Index(i); i++;
            Map(m => m.NumSnapshotsNormal).Index(i); i++;
            Map(m => m.NumSnapshotsSlow).Index(i); i++;
            Map(m => m.NumSnapshotsVerySlow).Index(i); i++;
            Map(m => m.NumSnapshotsStall).Index(i); i++;
            Map(m => m.NumSnapshotsError).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}