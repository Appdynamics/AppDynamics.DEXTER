using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MOBILENetworkRequest : MOBILEEntityBase
    {
        public const string ENTITY_TYPE = "MOBILENetworkRequest";
        public const string ENTITY_FOLDER = "NR";

        public string RequestName { get; set; }
        public string RequestNameInternal { get; set; }
        public long RequestID { get; set; }
        public string RequestLink { get; set; }

        public string UserExperience { get; set; }

        public string Platform { get; set; }

        public bool IsExcluded { get; set; }
        public bool IsCorrelated { get; set; }

        public int NumBTs { get; set; }

        public string MetricLink { get; set; }
        public List<long> MetricsIDs { get; set; }

        public string ARTRange { get; set; }
        public long ART { get; set; }
        public long TimeTotal { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }

        public long Server { get; set; }

        public long HttpErrors { get; set; }
        public long HttpEPM { get; set; }
        public long NetworkErrors { get; set; }
        public long NetworkEPM { get; set; }

        public bool HasActivity { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MOBILENetworkRequest: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.RequestName,
                this.RequestNameInternal,
                this.RequestID);
        }
    }
}
