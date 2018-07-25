using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntitySIMTier : EntitySIMBase
    {
        public const string ENTITY_TYPE = "SIM Tier";
        public const string ENTITY_FOLDER = "TIER";

        public long TierID { get; set; }
        public string TierName { get; set; }

        public int NumSegments { get; set; }

        public int NumNodes { get; set; }

        public override long EntityID
        {
            get
            {
                return this.TierID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.TierName;
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

        public EntityTier Clone()
        {
            return (EntityTier)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntitySIMTier: {0}/{1}({2})/{3}({4})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID);
        }
    }
}
