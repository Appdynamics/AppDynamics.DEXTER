using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBEntityBase
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
    
        public long ConfigID { get; set; }

        public long CollectorID { get; set; }
        public string CollectorLink { get; set; }

        public string CollectorName { get; set; }
        public string CollectorType { get; set; }

        public string AgentName { get; set; }

        public string CollectorStatus { get; set; }

        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
    }
}
