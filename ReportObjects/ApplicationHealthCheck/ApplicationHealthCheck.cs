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
        public bool isBTErrorRateHigh { get; set; }
        public bool isBackendOverflow { get; set; }
        public bool isDataCollectorsEnabled { get; set; }
        public bool isInfoPointsEnabled { get; set; }


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
