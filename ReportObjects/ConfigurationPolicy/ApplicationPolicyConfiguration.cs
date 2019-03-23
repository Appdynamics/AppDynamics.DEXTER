using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ApplicationConfigurationPolicy : ConfigurationEntityBase
    {
        public string Type { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumHealthRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumPolicies { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumActions { get; set; }

        public string HealthRulesLink { get; set; }
        public string PoliciesLink { get; set; }
        public string ActionsLink { get; set; }


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
                return "ApplicationConfigurationPolicy";
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
                "ApplicationConfigurationPolicy: {0}/{1}({2})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}
