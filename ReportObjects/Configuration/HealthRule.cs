using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class HealthRule
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string RuleName { get; set; }
        public string RuleType { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public bool IsAlwaysEnabled { get; set; }
        public string Schedule { get; set; }
        public int DurationOfEvalPeriod { get; set; }
        public int WaitTimeAfterViolation { get; set; }

        public string AffectsEntityType { get; set; }
        public string AffectsEntityMatchCriteria { get; set; }
        public string AffectsEntityMatchType { get; set; }
        public string AffectsEntityMatchPattern { get; set; }
        public bool AffectsEntityMatchIsInverse { get; set; }

        public string CriticalEntityConditionType { get; set; }
        public int CriticalNumConditions { get; set; }
        public string CriticalAggregateType { get; set; }

        public string WarningEntityConditionType { get; set; }
        public int WarningNumConditions { get; set; }
        public string WarningAggregateType { get; set; }

        public string Crit1Name { get; set; }
        public string Crit1Type { get; set; }
        public int Crit1Value { get; set; }
        public string Crit1Operator { get; set; }
        public string Crit1Expression { get; set; }
        public string Crit1BaselineUsed { get; set; }
        public bool Crit1TriggerOnNoData { get; set; }
        public string Crit1MetricName { get; set; }
        public string Crit1MetricFunction { get; set; }
        public string Crit1MetricExpressionConfig { get; set; }

        public string Crit2Name { get; set; }
        public string Crit2Type { get; set; }
        public int Crit2Value { get; set; }
        public string Crit2Operator { get; set; }
        public string Crit2Expression { get; set; }
        public string Crit2BaselineUsed { get; set; }
        public bool Crit2TriggerOnNoData { get; set; }
        public string Crit2MetricName { get; set; }
        public string Crit2MetricFunction { get; set; }
        public string Crit2MetricExpressionConfig { get; set; }

        public string Crit3Name { get; set; }
        public string Crit3Type { get; set; }
        public int Crit3Value { get; set; }
        public string Crit3Operator { get; set; }
        public string Crit3Expression { get; set; }
        public string Crit3BaselineUsed { get; set; }
        public bool Crit3TriggerOnNoData { get; set; }
        public string Crit3MetricName { get; set; }
        public string Crit3MetricFunction { get; set; }
        public string Crit3MetricExpressionConfig { get; set; }

        public string Crit4Name { get; set; }
        public string Crit4Type { get; set; }
        public int Crit4Value { get; set; }
        public string Crit4Operator { get; set; }
        public string Crit4Expression { get; set; }
        public string Crit4BaselineUsed { get; set; }
        public bool Crit4TriggerOnNoData { get; set; }
        public string Crit4MetricName { get; set; }
        public string Crit4MetricFunction { get; set; }
        public string Crit4MetricExpressionConfig { get; set; }

        public string Crit5Name { get; set; }
        public string Crit5Type { get; set; }
        public int Crit5Value { get; set; }
        public string Crit5Operator { get; set; }
        public string Crit5Expression { get; set; }
        public string Crit5BaselineUsed { get; set; }
        public bool Crit5TriggerOnNoData { get; set; }
        public string Crit5MetricName { get; set; }
        public string Crit5MetricFunction { get; set; }
        public string Crit5MetricExpressionConfig { get; set; }

        public string Warn1Name { get; set; }
        public string Warn1Type { get; set; }
        public int Warn1Value { get; set; }
        public string Warn1Operator { get; set; }
        public string Warn1Expression { get; set; }
        public string Warn1BaselineUsed { get; set; }
        public bool Warn1TriggerOnNoData { get; set; }
        public string Warn1MetricName { get; set; }
        public string Warn1MetricFunction { get; set; }
        public string Warn1MetricExpressionConfig { get; set; }

        public string Warn2Name { get; set; }
        public string Warn2Type { get; set; }
        public int Warn2Value { get; set; }
        public string Warn2Operator { get; set; }
        public string Warn2Expression { get; set; }
        public string Warn2BaselineUsed { get; set; }
        public bool Warn2TriggerOnNoData { get; set; }
        public string Warn2MetricName { get; set; }
        public string Warn2MetricFunction { get; set; }
        public string Warn2MetricExpressionConfig { get; set; }

        public string Warn3Name { get; set; }
        public string Warn3Type { get; set; }
        public int Warn3Value { get; set; }
        public string Warn3Operator { get; set; }
        public string Warn3Expression { get; set; }
        public string Warn3BaselineUsed { get; set; }
        public bool Warn3TriggerOnNoData { get; set; }
        public string Warn3MetricName { get; set; }
        public string Warn3MetricFunction { get; set; }
        public string Warn3MetricExpressionConfig { get; set; }

        public string Warn4Name { get; set; }
        public string Warn4Type { get; set; }
        public int Warn4Value { get; set; }
        public string Warn4Operator { get; set; }
        public string Warn4Expression { get; set; }
        public string Warn4BaselineUsed { get; set; }
        public bool Warn4TriggerOnNoData { get; set; }
        public string Warn4MetricName { get; set; }
        public string Warn4MetricFunction { get; set; }
        public string Warn4MetricExpressionConfig { get; set; }

        public string Warn5Name { get; set; }
        public string Warn5Type { get; set; }
        public int Warn5Value { get; set; }
        public string Warn5Operator { get; set; }
        public string Warn5Expression { get; set; }
        public string Warn5BaselineUsed { get; set; }
        public bool Warn5TriggerOnNoData { get; set; }
        public string Warn5MetricName { get; set; }
        public string Warn5MetricFunction { get; set; }
        public string Warn5MetricExpressionConfig { get; set; }

        public string AffectedEntitiesRawValue { get; set; }
        public string CriticalConditionRawValue { get; set; }
        public string WarningConditionRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "HealthRule: {0}/{1}/{2} {3}",
                this.Controller,
                this.ApplicationName,
                this.RuleName);
        }
    }
}
