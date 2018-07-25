using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMMachineCPU : EntitySIMBase
    {
        public const string ENTITY_TYPE = "SIM Machine CPU";
        public const string ENTITY_FOLDER = "MCHN";

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public long MachineID { get; set; }
        public string MachineName { get; set; }

        public string CPUID { get; set; }
        public int NumCores { get; set; }
        public int NumLogical { get; set; }
        public string Vendor { get; set; }
        public string Flags { get; set; }
        public int NumFlags { get; set; }
        public string Model { get; set; }
        public string Speed { get; set; }

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
                "EntitySIMMachineCPU: {0}/{1}({2})/{3}({4})/{5}({6})",
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
