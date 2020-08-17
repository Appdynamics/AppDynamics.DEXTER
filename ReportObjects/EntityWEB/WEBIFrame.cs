using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBIFrame : WEBPage
    {
        public new const string ENTITY_TYPE = "WEBIFrame";
        public new const string ENTITY_FOLDER = "IFRAME";

        public override String ToString()
        {
            return String.Format(
                "WEBIFrame: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.PageName,
                this.PageType,
                this.PageID);
        }
    }
}
