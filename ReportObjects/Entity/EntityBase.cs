using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntityBase
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string MetricLink { get; set; }
        public List<long> MetricsIDs { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public string ARTRange { get; set; }
        public long ART { get; set; }
        public long TimeTotal { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }
        public long Errors { get; set; }
        public long EPM { get; set; }
        public long Exceptions { get; set; }
        public long EXCPM { get; set; }
        public long HttpErrors { get; set; }
        public long HTTPEPM { get; set; }
        public double ErrorsPercentage { get; set; }
        public bool HasActivity { get; set; }

        public string DetailLink { get; set; }

        public string MetricGraphLink { get; set; }

        public string FlameGraphLink { get; set; }
        public string FlameChartLink { get; set; }

        public virtual long EntityID { get; }
        public virtual string EntityName { get; }
        public virtual string FolderName { get; }
        public virtual string EntityType { get; }
    }
}
