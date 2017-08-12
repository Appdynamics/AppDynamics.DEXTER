using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityBusinessTransaction: EntityBase
    {
        public int BTID { get; set; }
        public string BTLink { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

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
