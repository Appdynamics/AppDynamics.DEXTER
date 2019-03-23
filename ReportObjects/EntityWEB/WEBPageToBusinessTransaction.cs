using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBPageToBusinessTransaction : WEBEntityBase
    {
        public string PageType { get; set; }
        public string PageName { get; set; }
        public long PageID { get; set; }

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
                "WEBPageToBusinessTransaction: {0}/{1}({2}) {3}->{4}({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.PageName,
                this.TierName,
                this.BTName);
        }
    }
}
