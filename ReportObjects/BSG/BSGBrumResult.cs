using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGBrumResult
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }
        public bool DataReported { get; set; }
        public int NumPages { get; set; }
        public int NumAjax { get; set; }
        // public bool PageLimitHit { get; set; }
        // public bool AjaxLimitHit { get; set; }
        public int NumCustomPageRules { get; set; }
        public int NumCustomAjaxRules { get; set; }
        public int BrumHealthRules { get; set; }
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
                "BSGBrumResult: {0}/{1}({2}) {3}={4}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}