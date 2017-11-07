using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityTier : EntityBase
    {
        public string Description { get; set; }

        public string AgentType { get; set; }
        public string TierType { get; set; }

        public int NumErrors { get; set; }
        public int NumBTs { get; set; }
        public int NumNodes { get; set; }
        public int NumSEPs { get; set; }

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
