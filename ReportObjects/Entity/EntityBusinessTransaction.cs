using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityBusinessTransaction: EntityBase
    {
        public const string ENTITY_TYPE = "Business Transaction";
        public const string ENTITY_FOLDER = "BT";

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public long BTID { get; set; }
        public string BTLink { get; set; }
        public string BTName { get; set; }

        public string BTType { get; set; }
        public string BTNameOriginal { get; set; }
        public bool IsRenamed { get; set; }

        public override long EntityID
        {
            get
            {
                return this.BTID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.BTName;
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

        public EntityBusinessTransaction Clone()
        {
            return (EntityBusinessTransaction)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityBusinessTransaction: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.BTName,
                this.BTID);
        }
    }
}
