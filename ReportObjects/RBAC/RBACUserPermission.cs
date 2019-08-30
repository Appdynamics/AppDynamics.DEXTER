using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACUserPermission : RBACEntityBase
    {
        public string UserName { get; set; }
        public string UserSecurityProvider { get; set; }

        public string GroupName { get; set; }
        public string GroupSecurityProvider { get; set; }

        public string RoleName { get; set; }
        public string PermissionName { get; set; }

        public bool Allowed { get; set; }

        public string EntityName { get; set; }
        public string EntityType { get; set; }
        public long EntityID { get; set; }

        public long UserID { get; set; }
        public long GroupID { get; set; }

        public long RoleID { get; set; }
        public long PermissionID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACUserPermission: {0}/{1} is in {2} group and in {3} role, with {4}={5}",
                this.Controller,
                this.UserName,
                this.GroupName,
                this.RoleName,
                this.PermissionName,
                this.Allowed);
        }
    }
}
