using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACGroupMembership : RBACEntityBase
    {
        public string GroupName { get; set; }

        public string UserName { get; set; }

        public long GroupID { get; set; }
        public long UserID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACGroupMembership: {0}/{1} is in {2}",
                this.Controller,
                this.UserName,
                this.GroupName);
        }
    }
}
