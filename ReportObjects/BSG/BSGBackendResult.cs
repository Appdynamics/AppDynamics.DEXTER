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


        public BSGAgentResult Clone()
        {
            return (BSGAgentResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGResolvedBackend:  {0}/{1}({2})/{3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.BackendName);
        }
    }
}