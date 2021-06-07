using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGBackendCustomizationResult
    {
        public string Controller { get; set; }

        public string ApplicationName { get; set; }

        public int CustomDiscoveryRules { get; set; }
        public int CustomExitPoints { get; set; }

        public BSGBackendCustomizationResult Clone()
        {
            return (BSGBackendCustomizationResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGBackendCustomizationResult: {0}/{1}({2}) {3}={4}",
                this.Controller,
                this.ApplicationName);
        }
    }
}