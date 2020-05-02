using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationHealthCheck
    {
        #region APMConfiguration Items
        public string Controller { get; set; }
        public string ApplicationName { get; set; }
        public long ApplicationID { get; set; }
        public int NumTiers { get; set; }
        public int NumNodes { get; set; }
        public int NumBTs { get; set; }
        public int NumSEPs { get; set; }

        public string DeveloperModeOff { get; set; }
        public string BTLockdownEnabled { get; set; }
        
        #endregion

        public string BTOverflow { get; set; }
        public string CustomBTRules { get; set; }
        public string BTErrorRateHigh { get; set; }
        public string CustomSEPRules { get; set; }
        public string BackendOverflow { get; set; }
        public string NumDataCollectorsEnabled { get; set; }
        public string NumInfoPoints { get; set; }

        public string HRViolationsHigh { get; set; }
        public string PoliciesActionsEnabled { get; set; }

        public string AppAgentVersion { get; set; }
        public string MachineAgentVersion { get; set; }
        public string MachineAgentEnabledPercent { get; set; }
        public string TiersActivePercent { get; set; }
        public string NodesActivePercent { get; set; }


        public override String ToString()
        {
            return String.Format(
                "ApplicationHealthCheck: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
