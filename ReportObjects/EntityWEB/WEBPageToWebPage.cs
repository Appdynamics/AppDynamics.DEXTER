using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBPageToWebPage : WEBEntityBase
    {
        public string PageType { get; set; }
        public string PageName { get; set; }
        public long PageID { get; set; }

        public string ChildPageType { get; set; }
        public string ChildPageName { get; set; }
        public long ChildPageID { get; set; }

        public string ARTRange { get; set; }
        public long ART { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }

        public bool HasActivity { get; set; }

        public override String ToString()
        {
            return String.Format(
                "WEBPageToWebPage: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.PageName,
                this.PageType,
                this.PageID);
        }
    }
}
