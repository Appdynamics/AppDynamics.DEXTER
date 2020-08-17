using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBAJAXRequest : WEBPage
    {
        public new const string ENTITY_TYPE = "WEBAjax";
        public new const string ENTITY_FOLDER = "AJAX";

        public override String ToString()
        {
            return String.Format(
                "WEBAJAXRequest: {0}/{1}({2}) {3}[{4}]({5})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.PageName,
                this.PageType,
                this.PageID);
        }
    }
}
