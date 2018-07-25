using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMNode : EntitySIMBase
    {
        public const string ENTITY_TYPE = "SIM Node";
        public const string ENTITY_FOLDER = "NODE";

        public long TierID { get; set; }
        public string TierName { get; set; }

        public long NodeID { get; set; }
        public string NodeName { get; set; }

        public override long EntityID
        {
            get
            {
                return this.NodeID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.NodeName;
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
                "EntitySIMNode: {0}/{1}({2})/{3}({4})/{5}({6})",
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
