using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityServiceEndpoint: EntityBase
    {
        public const string ENTITY_TYPE = "Service Endpoint";
        public const string ENTITY_FOLDER = "SEP";

        public long TierID { get; set; }
        public string TierLink { get; set; }
        public string TierName { get; set; }

        public long SEPID { get; set; }
        public string SEPLink { get; set; }
        public string SEPName { get; set; }

        public string SEPType { get; set; }

        public override long EntityID
        {
            get
            {
                return this.SEPID;
            }
        }
        public override string EntityName
        {
            get
            {
                return this.SEPName;
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

        public EntityServiceEndpoint Clone()
        {
            return (EntityServiceEndpoint)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityServiceEndpoint: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.SEPName,
                this.SEPID);
        }
    }
}
