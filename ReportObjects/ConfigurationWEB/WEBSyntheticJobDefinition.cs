using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class WEBSyntheticJobDefinition : ConfigurationEntityBase
    {
        public string JobName { get; set; }
        public string JobType { get; set; }

        public bool IsUserEnabled { get; set; }
        public bool IsSystemEnabled { get; set; }
        public bool FailOnError { get; set; }
        public bool IsPrivateAgent { get; set; }

        public string RateUnit { get; set; }
        public int Rate { get; set; }
        public int Timeout { get; set; }

        public string Days { get; set; }
        public string Browsers { get; set; }
        public string Locations { get; set; }
        public int NumLocations { get; set; }
        public string ScheduleMode { get; set; }

        public string URL { get; set; }
        public string Script { get; set; }

        public string Network { get; set; }
        public string Config { get; set; }
        public string PerfCriteria { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public string JobID { get; set; }

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
                return "WEBSyntheticJobDefinition";
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
                "WEBSyntheticJobDefinition: {0}/{1}({2}) {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID, 
                this.JobName,
                this.JobType,
                this.URL);
        }
    }
}
