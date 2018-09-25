using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class Controller : APMEntityBase
    {
        public string UserName { get; set; }
        public string Version { get; set; }
        public string VersionDetail { get; set; }

        public int NumApps { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Controller: {0}",
                this.Controller);
        }

    }
}
