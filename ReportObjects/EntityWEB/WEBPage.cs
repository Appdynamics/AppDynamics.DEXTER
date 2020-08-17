using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBPage : WEBEntityBase
    {
        public const string ENTITY_TYPE = "WEBPage";
        public const string ENTITY_FOLDER = "PAGE";

        public string PageType { get; set; }
        public string PageName { get; set; }
        public long PageID { get; set; }
        public string PageLink { get; set; }

        public string FirstSegment { get; set; }
        public int NumNameSegments { get; set; }

        public bool IsCorrelated { get; set; }
        public bool IsNavTime { get; set; }
        public bool IsCookie { get; set; }
        public bool IsSynthetic { get; set; }

        public int NumBTs { get; set; }
        public int NumPages { get; set; }

        public string MetricLink { get; set; }
        public List<long> MetricsIDs { get; set; }

        public string ARTRange { get; set; }
        public long ART { get; set; }
        public long TimeTotal { get; set; }
        public long Calls { get; set; }
        public long CPM { get; set; }
        public long DOMReady { get; set; }
        public long FirstByte { get; set; }
        public long ServerConnection { get; set; }
        public long DNS { get; set; }
        public long TCP { get; set; }
        public long SSL { get; set; }
        public long Server { get; set; }
        public long HTMLDownload { get; set; }
        public long DOMBuild { get; set; }
        public long ResourceFetch { get; set; }

        public long JSErrors { get; set; }
        public long JSEPM { get; set; }
        public long AJAXErrors { get; set; }
        public long AJAXEPM { get; set; }

        public bool HasActivity { get; set; }

        public override String ToString()
        {
            return String.Format(
                "WEBPage: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.PageName,
                this.PageType,
                this.PageID);
        }
    }
}
