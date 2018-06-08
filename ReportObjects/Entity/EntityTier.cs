using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class EntityTier : EntityBase
    {
        public const string ENTITY_TYPE = "Tier";
        public const string ENTITY_FOLDER = "TIER";

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public string Description { get; set; }

        public string AgentType { get; set; }
        public string TierType { get; set; }

        public int NumErrors { get; set; }
        public int NumBTs { get; set; }
        public int NumNodes { get; set; }
        public int NumSEPs { get; set; }

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
                "EntityTier: {0}/{1}({2})/{3}({4})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID);
        }
    }
}
