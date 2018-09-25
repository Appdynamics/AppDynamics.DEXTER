using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBBlockingSessionReportMap : ClassMap<DBBlockingSession>
    {
        public DBBlockingSessionReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.CollectorName).Index(i); i++;
            Map(m => m.CollectorType).Index(i); i++;

            Map(m => m.CollectorStatus).Index(i); i++;

            Map(m => m.AgentName).Index(i); i++;

            Map(m => m.Host).Index(i); i++;
            Map(m => m.Port).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;

            Map(m => m.BlockingSessionName).Index(i); i++;
            Map(m => m.BlockingClientName).Index(i); i++;
            Map(m => m.BlockingDBUserName).Index(i); i++;

            Map(m => m.OtherSessionName).Index(i); i++;
            Map(m => m.OtherClientName).Index(i); i++;
            Map(m => m.OtherDBUserName).Index(i); i++;

            Map(m => m.QueryHash).Index(i); i++;
            Map(m => m.Query).Index(i); i++;

            Map(m => m.BlockTime).Index(i); i++;
            Map(m => m.BlockTimeSpan).Index(i); i++;
            Map(m => m.FirstOccurrence).Index(i); i++;
            Map(m => m.FirstOccurrenceUtc).Index(i); i++;

            Map(m => m.From).Index(i); i++;
            Map(m => m.To).Index(i); i++;
            Map(m => m.FromUtc).Index(i); i++;
            Map(m => m.ToUtc).Index(i); i++;
            Map(m => m.Duration).Index(i); i++;

            Map(m => m.ConfigID).Index(i); i++;
            Map(m => m.CollectorID).Index(i); i++;
            Map(m => m.BlockingSessionID).Index(i); i++;
        }
    }
}
