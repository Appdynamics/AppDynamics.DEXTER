using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGSepResult
    {
        public string Controller { get; set; }
        
        public string ApplicationName { get; set; }

        public int NumServiceEndpoints { get; set; }
        public int NumServiceEndpointsWithLoad { get; set; }
        public int NumServiceEndpointDetectionRules { get; set; }
        
        public bool EjbAutoDiscoveryEnabled { get; set; }
        public bool JmsAutoDiscoveryEnabled { get; set; }
        public bool PojoAutoDiscoveryEnabled { get; set; }
        public bool ServletAutoDiscoveryEnabled { get; set; }
        public bool SpringBeanAutoDiscoveryEnabled { get; set; }
        public bool StrutsAutoDiscoveryEnabled { get; set; }
        public bool WebServiceAutoDiscoveryEnabled { get; set; }

        public BSGSepResult Clone()
        {
            return (BSGSepResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGSepResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName,
                this.NumServiceEndpoints);
        }
    }
}