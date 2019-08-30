using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ControllerSetting : ConfigurationEntityBase
    {
        public string Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Scope { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Updateable { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.Name;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.Name;
            }
        }

        public override string RuleType
        {
            get
            {
                return "ControllerSetting";
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
                "ControllerSetting: {0} {1}={2}",
                this.Controller,
                this.Name,
                this.Value);
        }
    }
}
