using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityInformationPoint: EntityBase
    {
        public long IPID { get; set; }
        public string IPLink { get; set; }
        public string IPName { get; set; }
        public string IPType { get; set; }

        public EntityInformationPoint Clone()
        {
            return (EntityInformationPoint)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityInformationPoint: {0}/{1}({2})/{3}({4})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.IPName,
                this.IPID);
        }
    }
}
