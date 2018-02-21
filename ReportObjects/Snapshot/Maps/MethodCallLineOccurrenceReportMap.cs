using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class MethodCallLineOccurrenceReportMap : ClassMap<MethodCallLine>
    {
        public MethodCallLineOccurrenceReportMap()
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

            Map(m => m.Occurred).Index(i); i++;
            Map(m => m.OccurredUtc).Index(i); i++;

            Map(m => m.Type).Index(i); i++;
            Map(m => m.Framework).Index(i); i++;

            Map(m => m.FullName).Index(i); i++;
            Map(m => m.PrettyName).Index(i); i++;
            Map(m => m.Class).Index(i); i++;
            Map(m => m.Method).Index(i); i++;
            Map(m => m.LineNumber).Index(i); i++;
            Map(m => m.NumCalls).Index(i); i++;

            Map(m => m.Exec).Index(i); i++;
            Map(m => m.Wait).Index(i); i++;
            Map(m => m.Block).Index(i); i++;
            Map(m => m.CPU).Index(i); i++;
            Map(m => m.ExecRange).Index(i); i++;

            Map(m => m.NumExits).Index(i); i++;
            Map(m => m.NumSEPs).Index(i); i++;
            Map(m => m.NumMIDCs).Index(i); i++;
            Map(m => m.NumChildren).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.TierID).Index(i); i++;
            Map(m => m.NodeID).Index(i); i++;
            Map(m => m.BTID).Index(i); i++;
        }
    }
}