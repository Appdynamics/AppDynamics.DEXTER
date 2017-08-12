using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityServiceEndpoint: EntityBase
    {
        public int SEPID { get; set; }
        public string SEPLink { get; set; }
        public string SEPName { get; set; }
        public string SEPType { get; set; }

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
