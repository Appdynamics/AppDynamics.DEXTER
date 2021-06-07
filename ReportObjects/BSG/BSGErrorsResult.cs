using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGErrorsResult
    {
        public string Controller { get; set; }
        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }
        public int MaxErrorRate { get; set; }
        public int NumDetectionRules { get; set; }

        public BSGErrorsResult Clone()
        {
            return (BSGErrorsResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGErrorsResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}