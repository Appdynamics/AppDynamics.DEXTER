using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationHealthCheck
    {
        public string Controller { get; set; }
        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }
        public int NumTiers { get; set; }
        public int NumBTs { get; set; }

        public bool IsDeveloperModeEnabled { get; set; }
        public bool IsBTLockdownEnabled { get; set; }
 
        public override String ToString()
        {
            return String.Format(
                "ApplicationHealthCheck: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
