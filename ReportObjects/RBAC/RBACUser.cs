using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACUser : RBACEntityBase
    {
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }

        public string SecurityProvider { get; set; }

        public long UserID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACUser: {0}/{1}({2})",
                this.Controller,
                this.UserName,
                this.SecurityProvider);
        }
    }
}
