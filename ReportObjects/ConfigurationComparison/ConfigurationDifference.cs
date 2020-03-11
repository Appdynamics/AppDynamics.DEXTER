using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class ConfigurationDifference
    {
        public string EntityName { get; set; }
        public string RuleType { get; set; }
        public string RuleSubType { get; set; }
        public string TierName { get; set; }
        public string EntityIdentifier { get; set; }

        public string ReferenceConroller { get; set; }
        public string ReferenceApp { get; set; }
        public string DifferenceController { get; set; }
        public string DifferenceApp { get; set; }
        public string Difference { get; set; }

        public string Property { get; set; }
        public string ReferenceValue { get; set; }
        public string DifferenceValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "ConfigurationDifference: {0} {1} {2} {3} {4} {5} {6}",
                this.RuleType,
                this.EntityName,
                this.DifferenceApp,
                this.Property,
                this.Difference,
                this.ReferenceValue,
                this.DifferenceValue);
        }
    }
}
