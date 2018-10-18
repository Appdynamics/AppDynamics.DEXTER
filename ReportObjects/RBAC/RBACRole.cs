using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACRole : RBACEntityBase
    {
        public string RoleName { get; set; }
        public string Description { get; set; }

        public int NumPermissions { get; set; }

        public bool ReadOnly { get; set; }

        public long RoleID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACRole: {0}/{1}",
                this.Controller,
                this.RoleName);
        }
    }
}
