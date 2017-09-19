using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class SegmentReportMap: CsvClassMap<Segment>
    {
        public SegmentReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.UserExperience).Index(i); i++;
            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.SegmentID).Index(i); i++;
            Map(m => m.ParentSegmentID).Index(i); i++;
            Map(m => m.ParentTierName).Index(i); i++;

            Map(m => m.Occured).Index(i); i++;
            Map(m => m.OccuredUtc).Index(i); i++;

            Map(m => m.ThreadID).Index(i); i++;
            Map(m => m.ThreadName).Index(i); i++;

            Map(m => m.Duration).Index(i); i++;
            Map(m => m.CPUDuration).Index(i); i++;
            Map(m => m.WaitDuration).Index(i); i++;
            Map(m => m.BlockDuration).Index(i); i++;
            Map(m => m.E2ELatency).Index(i); i++;

            Map(m => m.URL).Index(i); i++;
            Map(m => m.CallChain).Index(i); i++;
            Map(m => m.ExitTypes).Index(i); i++;
            Map(m => m.UserPrincipal).Index(i); i++;
            Map(m => m.HTTPSessionID).Index(i); i++;

            Map(m => m.CallGraphType).Index(i); i++;
            Map(m => m.HasErrors).Index(i); i++;
            Map(m => m.IsArchived).Index(i); i++;
            Map(m => m.IsAsync).Index(i); i++;
            Map(m => m.IsFirstInChain).Index(i); i++;

            Map(m => m.DiagSessionID).Index(i); i++;
            Map(m => m.TakenSummary).Index(i); i++;
            Map(m => m.TakenReason).Index(i); i++;
            Map(m => m.TakenPolicy).Index(i); i++;

            Map(m => m.WarningThreshold).Index(i); i++;
            Map(m => m.CriticalThreshold).Index(i); i++;

            Map(m => m.NumBackendCalls).Index(i); i++;
            Map(m => m.NumTierCalls).Index(i); i++;
            Map(m => m.NumApplicationCalls).Index(i); i++;
            Map(m => m.NumErrors).Index(i); i++;
            Map(m => m.NumHTTPDCs).Index(i); i++;
            Map(m => m.NumMIDCs).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;

            //Map(m => m.DetailLink).Index(i); i++;
            Map(m => m.SegmentLink).Index(i); i++;
            //Map(m => m.ControllerLink).Index(i); i++;
            //Map(m => m.ApplicationLink).Index(i); i++;
            //Map(m => m.TierLink).Index(i); i++;
            //Map(m => m.NodeLink).Index(i); i++;
            //Map(m => m.BTLink).Index(i); i++;
        }
    }
}