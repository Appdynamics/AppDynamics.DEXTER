using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBVirtualPage : WEBPage
    {
        public new const string ENTITY_TYPE = "WEBVirtualPage";
        public new const string ENTITY_FOLDER = "VIRTPAGE";

        public override String ToString()
        {
            return String.Format(
                "WEBVirtualPage: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.PageName,
                this.PageType,
                this.PageID);
        }
    }
}
