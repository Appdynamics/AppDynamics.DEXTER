using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MOBILENetworkRequestToBusinessTransaction : MOBILEEntityBase
    {
        public string RequestName { get; set; }
        public string RequestNameInternal { get; set; }
        public long RequestID { get; set; }

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

        public string ARTRange { get; set; }
        public long ART { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }

        public bool HasActivity { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MOBILENetworkRequestToBusinessTransaction: {0}/{1}({2}) {3}->{4}({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.RequestName,
                this.TierName,
                this.BTName);
        }
    }
}
