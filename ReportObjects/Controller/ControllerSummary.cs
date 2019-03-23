using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ControllerSummary
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public string Version { get; set; }
        public string VersionDetail { get; set; }

        public int NumApps { get; set; }
        public int NumAPMApps { get; set; }
        public int NumSIMApps { get; set; }
        public int NumDBApps { get; set; }
        public int NumWEBApps { get; set; }
        public int NumIOTApps { get; set; }
        public int NumMOBILEApps { get; set; }
        public int NumBIQApps { get; set; }

        public int StartupTime { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ControllerSummary: {0} {1}",
                this.Controller, 
                this.Version);
        }

    }
}
