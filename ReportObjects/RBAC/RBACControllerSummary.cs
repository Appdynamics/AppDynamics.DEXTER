using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACControllerSummary
    {
        public string Controller { get; set; }

        public string SecurityProvider { get; set; }
        public bool IsStrongPasswords { get; set; }

        public int NumUsers { get; set; }
        public int NumGroups { get; set; }
        public int NumRoles { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Controller: {0} ({1})",
                this.Controller,
                this.SecurityProvider);
        }

    }
}
