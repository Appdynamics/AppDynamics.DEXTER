using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGBackendResult
    {
        public string Controller { get; set; }

        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }

        public string BackendName { get; set; }
        public string BackendType { get; set; }
        public bool HasActivity { get; set; }


        public BSGBackendResult Clone()
        {
            return (BSGBackendResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGBackendResult:  {0}/{1}({2})/{3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.BackendName);
        }
    }
}