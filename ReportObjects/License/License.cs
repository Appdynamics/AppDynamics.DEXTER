using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class License
    {
        public string Controller { get; set; }

        public long AccountID { get; set; }
        public string AccountName { get; set; }

        public string AgentType { get; set; }
        public string Edition { get; set; }
        public string Model { get; set; }

        public long Provisioned { get; set; }
        public long MaximumAllowed { get; set; }
        public long Min { get; set; }
        public long Max { get; set; }
        public long Average { get; set; }
        //public double Peak { get; set; }
        public int Retention { get; set; }

        public DateTime ExpirationDate { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public override String ToString()
        {
            return String.Format(
                "License: {0}/{1} {2}/{3}",
                this.Controller,
                this.AccountName, 
                this.Average,
                this.Provisioned);
        }
    }
}
