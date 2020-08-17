using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class MethodCallLineReportMap : ClassMap<MethodCallLine>
    {
        public MethodCallLineReportMap()
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

            Map(m => m.SegmentUserExperience).Index(i); i++;
            Map(m => m.SnapshotUserExperience).Index(i); i++;

            Map(m => m.RequestID).Index(i); i++;
            Map(m => m.SegmentID).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.Occurred), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.OccurredUtc), i); i++;

            Map(m => m.Type).Index(i); i++;
            Map(m => m.Framework).Index(i); i++;
            Map(m => m.FullNameIndent).Index(i); i++;

            Map(m => m.Exec).Index(i); i++;
            Map(m => m.ExecTotal).Index(i); i++;
            Map(m => m.ExecToHere).Index(i); i++;
            Map(m => m.Wait).Index(i); i++;
            Map(m => m.WaitTotal).Index(i); i++;
            Map(m => m.Block).Index(i); i++;
            Map(m => m.BlockTotal).Index(i); i++;
            Map(m => m.CPU).Index(i); i++;
            Map(m => m.CPUTotal).Index(i); i++;
            Map(m => m.ExecRange).Index(i); i++;

            Map(m => m.ExitCalls).Index(i); i++;
            Map(m => m.NumExits).Index(i); i++;
            Map(m => m.HasErrors).Index(i); i++;

            Map(m => m.SEPs).Index(i); i++;
            Map(m => m.NumSEPs).Index(i); i++;

            Map(m => m.MIDCs).Index(i); i++;
            Map(m => m.NumMIDCs).Index(i); i++;

            Map(m => m.NumChildren).Index(i); i++;
            Map(m => m.ElementType).Index(i); i++;
            Map(m => m.SequenceNumber).Index(i); i++;
            Map(m => m.Depth).Index(i); i++;

            Map(m => m.PrettyName).Index(i); i++;
            Map(m => m.FullName).Index(i); i++;
            Map(m => m.Class).Index(i); i++;
            Map(m => m.Method).Index(i); i++;
            Map(m => m.LineNumber).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
        }
    }
}