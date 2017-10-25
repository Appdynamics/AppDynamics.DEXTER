using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class MethodInvocationDataCollector
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string CollectorName { get; set; }
        public string MatchClass { get; set; }
        public string MatchMethod { get; set; }
        public string MatchType { get; set; }
        public string MatchParameterTypes { get; set; }

        public string DataGathererName { get; set; }
        public string DataGathererType { get; set; }
        public int DataGathererPosition { get; set; }
        public string DataGathererTransform { get; set; }
        public string DataGathererGetter { get; set; }

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
