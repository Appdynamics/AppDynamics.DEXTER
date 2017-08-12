using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class EntityError: EntityBase
    {
        public int ErrorID { get; set; }
        public int ErrorDepth { get; set; }
        public string ErrorLevel1 { get; set; }
        public string ErrorLevel2 { get; set; }
        public string ErrorLevel3 { get; set; }
        public string ErrorLevel4 { get; set; }
        public string ErrorLevel5 { get; set; }
        public string ErrorLevel6 { get; set; }
        public string ErrorLink { get; set; }
        public string ErrorName { get; set; }
        public string ErrorType { get; set; }
        public int HttpCode { get; set; }

        public EntityError Clone()
        {
            return (EntityError)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "EntityError: {0}/{1}({2})/{3}({4})/{5}({6})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.TierName,
                this.TierID,
                this.ErrorName,
                this.ErrorID);
        }
    }
}
