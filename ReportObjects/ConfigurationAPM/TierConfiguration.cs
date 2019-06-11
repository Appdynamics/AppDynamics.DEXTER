using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class TierConfiguration : ConfigurationEntityBase
    {
        public long TierID { get; set; }
        public string TierType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string TierDescription { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDynamicScalingEnabled { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string MemoryConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string CacheConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string CustomCacheConfig { get; set; }

        public int NumBTs { get; set; }
        public int NumBTTypes { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.TierName, this.TierType);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.TierName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMTierConfiguration";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.TierType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "TierConfiguration: {0}/{1}/{2}",
                this.Controller,
                this.ApplicationName,
                this.TierName);
        }
    }
}
