using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class MOBILEApplicationConfiguration : ConfigurationEntityBase
    {
        public string ApplicationDescription { get; set; }

        public string ApplicationKey { get; set; }

        public int NumNetworkRulesInclude { get; set; }
        public int NumNetworkRulesExclude { get; set; }

        public bool IsEnabled { get; set; }

        public string SlowThresholdType { get; set; }
        public int SlowThreshold { get; set; }
        public string VerySlowThresholdType { get; set; }
        public int VerySlowThreshold { get; set; }
        public string StallThresholdType { get; set; }
        public int StallThreshold { get; set; }

        public string Percentiles { get; set; }
        public int SessionTimeout { get; set; }

        public int CrashThreshold { get; set; }

        public bool IsIPDisplayed { get; set; }
        public bool EnableScreenshot{ get; set; }
        public bool AutoScreenshot { get; set; }
        public bool UseCellular { get; set; }

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
                return "MOBILEApplicationConfiguration";
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
                "MOBILEApplicationConfiguration: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
