using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DeveloperModeNode : ConfigurationEntityBase
    {
        public long TierID { get; set; }
        public string BTName { get; set; }
        public long BTID { get; set; }
        public string NodeName { get; set; }
        public long NodeID { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}", this.TierName, this.BTName, this.NodeName);
            }
        }

        public override string EntityName
        {
            get
            {
                return String.Format("{0}/{1}/{2}", this.TierName, this.BTName, this.NodeName);
            }
        }

        public override string RuleType
        {
            get
            {
                return "DeveloperModeSetting";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.TierName;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "DeveloperModeSetting: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.BTName,
                this.NodeName);
        }
    }
}
