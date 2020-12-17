using System;
namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGSimResult
    {
        public string Controller { get; set; }
        public string ApplicationName { get; set; }

        public int NumHRs { get; set; }
        public int NumPolicies { get; set; }
        public int NumActions { get; set; }
        public int NumWarningViolations { get; set; }
        public int NumCriticalViolations { get; set; }

        public BSGSimResult Clone()
        {
            return (BSGSimResult)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGSimResult: {0}/{1}",
                this.Controller,
                this.ApplicationName);
        }
    }
}

