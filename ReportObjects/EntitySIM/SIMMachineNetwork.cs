using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMMachineNetwork : SIMEntityBase
    {
        public const string ENTITY_TYPE = "SIMNetwork";
        public const string ENTITY_FOLDER = "NIC";
        
        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string NetworkName { get; set; }
        public string MacAddress { get; set; }
        public string IP4Address { get; set; }
        public string IP4Gateway { get; set; }
        public string IP6Address { get; set; }
        public string IP6Gateway { get; set; }

        public long Speed { get; set; }
        public string Enabled { get; set; }
        public string PluggedIn { get; set; }
        public string State { get; set; }
        public string Duplex { get; set; }
        public string MTU { get; set; }

        public string NetworkMetricName { get; set; }

        public override String ToString()
        {
            return String.Format(
                "SIMMachineNetwork: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.NodeName,
                this.NodeID);
        }
    }
}
