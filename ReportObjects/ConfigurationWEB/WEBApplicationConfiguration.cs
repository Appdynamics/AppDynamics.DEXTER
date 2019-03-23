using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBApplicationConfiguration : ConfigurationEntityBase
    {
        public string ApplicationDescription { get; set; }

        public string ApplicationKey { get; set; }

        public int NumPageRulesInclude { get; set; }
        public int NumPageRulesExclude { get; set; }
        public int NumVirtPageRulesInclude { get; set; }
        public int NumVirtPageRulesExclude { get; set; }
        public int NumAJAXRulesInclude { get; set; }
        public int NumAJAXRulesExclude { get; set; }
        public int NumSyntheticJobs { get; set; }

        public string AgentCode { get; set; }

        public string AgentHTTP { get; set; }
        public string AgentHTTPS { get; set; }

        public string GeoHTTP { get; set; }
        public string GeoHTTPS { get; set; }

        public string BeaconHTTP { get; set; }
        public string BeaconHTTPS { get; set; }

        public bool IsEnabled { get; set; }
        public bool IsXsccEnabled { get; set; }
        public int HostOption { get; set; }

        public bool IsJSErrorEnabled { get; set; }
        public bool IsAJAXErrorEnabled { get; set; }
        public string IgnoreJSErrors { get; set; }
        public string IgnorePageNames { get; set; }
        public string IgnoreURLs { get; set; }

        public string SlowThresholdType { get; set; }
        public int SlowThreshold { get; set; }
        public string VerySlowThresholdType { get; set; }
        public int VerySlowThreshold { get; set; }
        public string StallThresholdType { get; set; }
        public int StallThreshold { get; set; }

        public string Percentiles { get; set; }
        public int SessionTimeout { get; set; }
        public bool IsIPDisplayed { get; set; }

        public bool EnableSlowSnapshots { get; set; }
        public bool EnablePeriodicSnapshots { get; set; }
        public bool EnableErrorSnapshots { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.ApplicationName;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.ApplicationName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "WEBApplicationConfiguration";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return String.Empty;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "WEBApplicationConfiguration: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
