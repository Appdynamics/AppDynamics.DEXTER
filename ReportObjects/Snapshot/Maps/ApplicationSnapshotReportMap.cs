using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class ApplicationSnapshotReportMap : CsvClassMap<EntityApplication>
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
            Map(m => m.NumSegments).Index(i); i++;
            Map(m => m.NumExitCalls).Index(i); i++;
            Map(m => m.NumBusinessData).Index(i); i++;
            Map(m => m.NumErrors).Index(i); i++;
            Map(m => m.HasActivity).Index(i); i++;
            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}