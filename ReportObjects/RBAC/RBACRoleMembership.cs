using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACRoleMembership : RBACEntityBase
    {
        public string RoleName { get; set; }

        public string EntityName { get; set; }
        public string EntityType { get; set; }

        public long EntityID { get; set; }
        public long RoleID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACRoleMembership: {0}/{1}({2}) is in {3}",
                this.Controller,
                this.EntityName,
                this.EntityType,
                this.RoleName);
        }
    }
}
