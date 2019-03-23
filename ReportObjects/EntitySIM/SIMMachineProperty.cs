using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class SIMMachineProperty : SIMEntityBase
    {
        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string PropType { get; set; }
        public string PropName { get; set; }
        public string PropValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "SIMMachineProperty: {0}/{1}({2})/{3}({4})/{5}({6}) {7}={8}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.NodeName,
                this.NodeID,
                this.PropName,
                this.PropValue);
        }
    }
}
