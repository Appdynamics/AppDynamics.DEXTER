using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class SnapshotReportMap: ClassMap<Snapshot>
    {
        public SnapshotReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.UserExperience).Index(i); i++;
            Map(m => m.RequestID).Index(i); i++;

            Map(m => m.Occured).Index(i); i++;
            Map(m => m.OccuredUtc).Index(i); i++;

            Map(m => m.Duration).Index(i); i++;

            Map(m => m.URL).Index(i); i++;
            Map(m => m.CallChains).Index(i); i++;
            Map(m => m.ExitTypes).Index(i); i++;

            Map(m => m.CallGraphType).Index(i); i++;
            Map(m => m.IsArchived).Index(i); i++;

            Map(m => m.HasErrors).Index(i); i++;
            Map(m => m.NumErrors).Index(i); i++;

            Map(m => m.DiagSessionID).Index(i); i++;
            Map(m => m.TakenSummary).Index(i); i++;
            Map(m => m.TakenReason).Index(i); i++;

            Map(m => m.NumSegments).Index(i); i++;
            Map(m => m.NumCallGraphs).Index(i); i++;

            Map(m => m.NumCalledBackends).Index(i); i++;
            Map(m => m.NumCalledTiers).Index(i); i++;
            Map(m => m.NumCalledApplications).Index(i); i++;
            Map(m => m.NumCallsToBackends).Index(i); i++;
            Map(m => m.NumCallsToTiers).Index(i); i++;
            Map(m => m.NumCallsToApplications).Index(i); i++;

            Map(m => m.NumSEPs).Index(i); i++;
            Map(m => m.NumHTTPDCs).Index(i); i++;
            Map(m => m.NumMIDCs).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;

            Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.SnapshotLink).Index(i); i++;

            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.TierLink).Index(i); i++;
            Map(m => m.NodeLink).Index(i); i++;
            Map(m => m.BTLink).Index(i); i++;
        }
    }
}