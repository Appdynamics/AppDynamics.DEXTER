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
        public int NumBTs { get; set; }

        public bool IsDeveloperModeEnabled { get; set; }
        public bool IsBTLockdownEnabled { get; set; }
        
        #endregion

        public bool IsBTOverflow { get; set; }
        public bool IsCustomBTRules { get; set; }
        public bool IsBTErrorRateHigh { get; set; }
        public bool IsBackendOverflow { get; set; }
        public bool IsDataCollectorsEnabled { get; set; }
        public bool IsInfoPointsEnabled { get; set; }

        public string IsHRViolationsHigh { get; set; }
        public string IsPoliciesAndActionsEnabled { get; set; }

        public string AppAgentVersion { get; set; }
        public string MachineAgentVersion { get; set; }
        public string IsMachineAgentEnabled { get; set; }
        public string PercentActiveTiers { get; set; }
        public string PercentActiveNodes { get; set; }


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
