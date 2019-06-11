using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionConfiguration : ConfigurationEntityBase
    {
        public long TierID { get; set; }

        public long BTID { get; set; }
        public string BTName { get; set; }
        public string BTType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsExcluded { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsBackground { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEUMEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string IsEUMPossible { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAnalyticsEnabled { get; set; }

        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTSLAConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTSnapshotCollectionConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTRequestThresholdConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTBackgroundSnapshotCollectionConfig { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string BTBackgroundRequestThresholdConfig { get; set; }

        public int NumAssignedMIDCs { get; set; }
        public string AssignedMIDCs { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}/{2}", this.BTName, this.BTType, this.TierName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.BTName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "APMBTConfiguration";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.BTType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionConfiguration: {0}/{1}/{2}/{3}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.BTName);
        }
    }
}
