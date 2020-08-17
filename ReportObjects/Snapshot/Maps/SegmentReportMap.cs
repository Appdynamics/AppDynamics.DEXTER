using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class SegmentReportMap : ClassMap<Segment>
    {
        public SegmentReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;
            Map(m => m.TierType).Index(i); i++;
            Map(m => m.NodeName).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.BTName).Index(i); i++;
            Map(m => m.BTType).Index(i); i++;

            Map(m => m.UserExperience).Index(i); i++;
            Map(m => m.SnapshotUserExperience).Index(i); i++;

            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.SegmentID).Index(i); i++;
            Map(m => m.FromSegmentID).Index(i); i++;
            Map(m => m.FromTierName).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.Occurred), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.OccurredUtc), i); i++;

            Map(m => m.Timeline).Index(i); i++;
            Map(m => m.TimelineResolution).Index(i); i++;

            Map(m => m.Duration).Index(i); i++;
            Map(m => m.DurationRange).Index(i); i++;
            Map(m => m.CPUDuration).Index(i); i++;
            Map(m => m.WaitDuration).Index(i); i++;
            Map(m => m.BlockDuration).Index(i); i++;
            Map(m => m.E2ELatency).Index(i); i++;

            Map(m => m.URL).Index(i); i++;
            Map(m => m.CallChains).Index(i); i++;
            Map(m => m.ExitTypes).Index(i); i++;
            Map(m => m.UserPrincipal).Index(i); i++;
            Map(m => m.HTTPSessionID).Index(i); i++;

            Map(m => m.ThreadID).Index(i); i++;
            Map(m => m.ThreadName).Index(i); i++;

            Map(m => m.CallGraphType).Index(i); i++;
            Map(m => m.IsArchived).Index(i); i++;
            Map(m => m.IsAsync).Index(i); i++;
            Map(m => m.IsFirstInChain).Index(i); i++;

            Map(m => m.HasErrors).Index(i); i++;
            Map(m => m.NumErrors).Index(i); i++;

            Map(m => m.DiagSessionID).Index(i); i++;
            Map(m => m.TakenSummary).Index(i); i++;
            Map(m => m.TakenReason).Index(i); i++;
            Map(m => m.TakenPolicy).Index(i); i++;

            Map(m => m.IsDelayedDeepDive).Index(i); i++;
            Map(m => m.DelayedDeepDiveOffSet).Index(i); i++;

            Map(m => m.WarningThreshold).Index(i); i++;
            Map(m => m.CriticalThreshold).Index(i); i++;

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

            Map(m => m.SegmentLink).Index(i); i++;
        }
    }
}