using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQExperienceLevel : BIQEntityBase
    {
        public string ExperienceLevelName { get; set; }
        public string ExperienceLevelID { get; set; }

        public string DataSource { get; set; }
        public string EventField { get; set; }
        public string Criteria { get; set; }
        public string ThresholdOperator { get; set; }
        public string ThresholdValue { get; set; }

        public string Period { get; set; }
        public string Timezone { get; set; }

        public bool IsActive { get; set; }
        public bool IsIncludeErrors { get; set; }

        public int NormalThreshold { get; set; }
        public int WarningThreshold { get; set; }
        //public int CriticalThreshold { get; set; }

        public int NumExclusionPeriods { get; set; }
        public string ExclusionPeriodsRaw { get; set; }

        public DateTime StartOn { get; set; }
        public DateTime StartOnUtc { get; set; }

        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQExperienceLevel: {0}/{1}({2}) {3} {4}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.ExperienceLevelName,
                this.Criteria);
        }
    }
}
