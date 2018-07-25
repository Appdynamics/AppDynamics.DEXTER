using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMMachineNetwork : EntitySIMBase
    {
        public const string ENTITY_TYPE = "SIM Machine Network";
        public const string ENTITY_FOLDER = "MCHN";

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

        public int Speed { get; set; }
        public string Enabled { get; set; }
        public string PluggedIn { get; set; }
        public string State { get; set; }
        public string Duplex { get; set; }
        public string MTU { get; set; }

        public string NetworkMetricName { get; set; }

        public override long EntityID
        {
            get
            {
                return this.MachineID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.MachineName;
            }
        }
        public override string EntityType
        {
            get
            {
                return ENTITY_TYPE;
            }
        }
        public override string FolderName
        {
            get
            {
                return ENTITY_FOLDER;
            }
        }

        public EntityNode Clone()
        {
            return (EntityNode)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntitySIMMachineNetwork: {0}/{1}({2})/{3}({4})/{5}({6})",
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
