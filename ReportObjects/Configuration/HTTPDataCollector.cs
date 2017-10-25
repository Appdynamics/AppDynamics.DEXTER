using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class HTTPDataCollector
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string CollectorName { get; set; }
        public bool IsURLEnabled { get; set; }
        public bool IsSessionIDEnabled { get; set; }
        public bool IsUserPrincipalEnabled { get; set; }

        public string DataGathererName { get; set; }
        public string DataGathererValue { get; set; }

        public bool IsAssignedToNewBTs { get; set; }
        public bool IsAPM { get; set; }
        public bool IsAnalytics { get; set; }

        public bool IsAssignedToBTs { get; set; }
        public int NumAssignedBTs { get; set; }
        public string AssignedBTs { get; set; }

        public string RuleRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MethodInvocationDataCollector: {0}/{1}/{2}",
                this.Controller,
                this.ApplicationName,
                this.CollectorName);
        }
    }
}
