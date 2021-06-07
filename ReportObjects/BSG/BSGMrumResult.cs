using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGMrumResult
    {
        public string Controller { get; set; }
        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }
        public int NumNetworkrequests { get; set; }
        // not currently grabbed by Dexter
        // public bool NetworkRequestLimitReached { get; set; }
        public int NumCustomNetworkRequestRules { get; set; }
        // not currently grabbed by Dexter
        // public bool CustomMappingFileUploaded { get; set; }
        // not currently grabbed by Dexter
        // public bool CustomUserData { get; set; }
        public int MrumHealthRules { get; set; }
        public int LinkedPolicies { get; set; }
        public int LinkedActions { get; set; }
        public int WarningViolations { get; set; }
        public int CriticalViolations { get; set; }

        public BSGBrumResult Clone()
        {
            return (BSGBrumResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGMrumResult: {0}/{1}({2}) {3}={4}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}