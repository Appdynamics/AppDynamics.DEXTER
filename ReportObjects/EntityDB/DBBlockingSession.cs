using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBBlockingSession : DBEntityBase
    {
        public string BlockingSessionName { get; set; }
        public string BlockingClientName { get; set; }
        public string BlockingDBUserName { get; set; }

        public string OtherSessionName { get; set; }
        public string OtherClientName { get; set; }
        public string OtherDBUserName { get; set; }

        public string QueryHash { get; set; }
        public string Query { get; set; }
        public string LockObject { get; set; }

        public long BlockTime { get; set; }
        public TimeSpan BlockTimeSpan { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public DateTime FirstOccurrenceUtc { get; set; }

        public long BlockingSessionID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBBlockingSession: {0}/{1}({2}) [{3}] {4}({5})",
                this.Controller,
                this.CollectorName,
                this.ConfigID,
                this.CollectorType,
                this.BlockingSessionName,
                this.BlockingSessionID);
        }
    }
}
