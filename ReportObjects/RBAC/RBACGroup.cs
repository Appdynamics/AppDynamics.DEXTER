using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACGroup : RBACEntityBase
    {
        public string GroupName { get; set; }
        public string Description { get; set; }

        public string SecurityProvider { get; set; }

        public long GroupID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "RBACGroup: {0}/{1}({2})",
                this.Controller,
                this.GroupName, 
                this.SecurityProvider);
        }
    }
}
