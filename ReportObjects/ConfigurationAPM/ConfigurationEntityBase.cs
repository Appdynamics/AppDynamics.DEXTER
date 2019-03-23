using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ConfigurationEntityBase
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string TierName { get; set; }

        public virtual string EntityIdentifier { get; }

        public virtual string EntityName { get; }
        public virtual string RuleType { get; }
        public virtual string RuleSubType { get; }
    }
}
