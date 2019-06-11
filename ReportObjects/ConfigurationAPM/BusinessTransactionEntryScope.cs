using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionEntryScope : ConfigurationEntityBase
    {

        public string ScopeName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string ScopeType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Version { get; set; }

        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string IncludedTiers { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumTiers { get; set; }

        [FieldComparison(FieldComparisonType.SemicolonMultiLineValueComparison)]
        public string IncludedRules { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int NumRules { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.ScopeName;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.ScopeName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMBTScope";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.ScopeType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionEntryScope: {0}/{1} {2} {3}",
                this.Controller,
                this.ApplicationName,
                this.ScopeName,
                this.ScopeType);
        }
    }
}
