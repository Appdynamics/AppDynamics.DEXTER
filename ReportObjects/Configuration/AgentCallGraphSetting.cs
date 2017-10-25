using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class AgentCallGraphSetting
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string AgentType { get; set; }

        public int SamplingRate { get; set; }
        public string IncludePackages { get; set; }
        public int NumIncludePackages { get; set; }
        public string ExcludePackages { get; set; }
        public int NumExcludePackages { get; set; }
        public int MinSQLDuration { get; set; }
        public bool IsRawSQLEnabled { get; set; }
        public bool IsHotSpotEnabled { get; set; }

        public override String ToString()
        {
            return String.Format(
                "AgentCallGraphSetting: {0}/{1} {2}",
                this.Controller,
                this.ApplicationName,
                this.AgentType);
        }
    }
}
