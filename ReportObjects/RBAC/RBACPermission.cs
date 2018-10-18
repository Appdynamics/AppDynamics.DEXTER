using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACPermission : RBACEntityBase
    {
        public string RoleName { get; set; }
        public string PermissionName { get; set; }

        public bool Allowed { get; set; }

        public string EntityName { get; set; }
        public string EntityType { get; set; }
        public long EntityID { get; set; }

        public long RoleID { get; set; }
        public long PermissionID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACPermission: {0}/{1}/{2} = {3}",
                this.Controller,
                this.RoleName,
                this.PermissionName,
                this.Allowed);
        }
    }
}
