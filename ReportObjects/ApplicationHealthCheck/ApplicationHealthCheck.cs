using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationHealthCheck : ConfigurationEntityBase
    {
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumTiers { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumBTs { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDeveloperModeEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBTLockdownEnabled { get; set; }
 
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
                return "ApplicationHealthCheck";
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
                "ApplicationHealthCheck: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
