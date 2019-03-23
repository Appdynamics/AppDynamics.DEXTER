using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class HealthRule : ConfigurationEntityBase
    {
        public string RuleName { get; set; }
        public string HRRuleType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Description { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsDefault { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsAlwaysEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Schedule { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int DurationOfEvalPeriod { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int WaitTimeAfterViolation { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AffectsEntityType { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string AffectsEntityMatchCriteria { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AffectsEntityMatchType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AffectsEntityMatchPattern { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool AffectsEntityMatchIsInverse { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CriticalEntityConditionType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int CriticalNumConditions { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CriticalAggregateType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string WarningEntityConditionType { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int WarningNumConditions { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string WarningAggregateType { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Crit1Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Crit1TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit1MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Crit1MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Crit2Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Crit2TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit2MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Crit2MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Crit3Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Crit3TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit3MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Crit3MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Crit4Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Crit4TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit4MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Crit4MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Crit5Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Crit5TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Crit5MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Crit5MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Warn1Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Warn1TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn1MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Warn1MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Warn2Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Warn2TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn2MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Warn2MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Warn3Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Warn3TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn3MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Warn3MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Warn4Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Warn4TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn4MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Warn4MetricExpressionConfig { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5Name { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5Type { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public long Warn5Value { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5Operator { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5Expression { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5BaselineUsed { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool Warn5TriggerOnNoData { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5MetricName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Warn5MetricFunction { get; set; }
        [FieldComparison(FieldComparisonType.XmlValueComparison)]
        public string Warn5MetricExpressionConfig { get; set; }

        public string AffectedEntitiesRawValue { get; set; }
        public string CriticalConditionRawValue { get; set; }
        public string WarningConditionRawValue { get; set; }

        public long HealthRuleID { get; set; }
        public string HealthRuleLink { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.RuleName, this.HRRuleType);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.RuleName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "HealthRule";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.HRRuleType;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "HealthRule: {0}/{1}/{2}/{3}",
                this.Controller,
                this.ApplicationName,
                this.RuleName,
                this.HRRuleType);
        }
    }
}
