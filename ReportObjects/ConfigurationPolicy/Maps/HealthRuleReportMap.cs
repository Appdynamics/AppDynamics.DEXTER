using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class HealthRuleReportMap : ClassMap<HealthRule>
    {
        public HealthRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsDefault).Index(i); i++;
            Map(m => m.IsAlwaysEnabled).Index(i); i++;
            Map(m => m.Schedule).Index(i); i++;
            Map(m => m.DurationOfEvalPeriod).Index(i); i++;
            Map(m => m.WaitTimeAfterViolation).Index(i); i++;

            Map(m => m.HRRuleType).Index(i); i++;
            Map(m => m.AffectsEntityType).Index(i); i++;
            Map(m => m.AffectsEntityMatchCriteria).Index(i); i++;
            Map(m => m.AffectsEntityMatchType).Index(i); i++;
            Map(m => m.AffectsEntityMatchPattern).Index(i); i++;
            Map(m => m.AffectsEntityMatchIsInverse).Index(i); i++;

            Map(m => m.CriticalEntityConditionType).Index(i); i++;
            Map(m => m.CriticalNumConditions).Index(i); i++;
            Map(m => m.CriticalAggregateType).Index(i); i++;

            Map(m => m.WarningEntityConditionType).Index(i); i++;
            Map(m => m.WarningNumConditions).Index(i); i++;
            Map(m => m.WarningAggregateType).Index(i); i++;

            Map(m => m.Crit1Name).Index(i); i++;
            Map(m => m.Crit1MetricFunction).Index(i); i++;
            Map(m => m.Crit1MetricName).Index(i); i++;
            Map(m => m.Crit1Type).Index(i); i++;
            Map(m => m.Crit1Operator).Index(i); i++;
            Map(m => m.Crit1Value).Index(i); i++;
            Map(m => m.Crit1Expression).Index(i); i++;
            Map(m => m.Crit1BaselineUsed).Index(i); i++;
            Map(m => m.Crit1TriggerOnNoData).Index(i); i++;
            Map(m => m.Crit1MetricExpressionConfig).Index(i); i++;

            Map(m => m.Crit2Name).Index(i); i++;
            Map(m => m.Crit2MetricFunction).Index(i); i++;
            Map(m => m.Crit2MetricName).Index(i); i++;
            Map(m => m.Crit2Type).Index(i); i++;
            Map(m => m.Crit2Operator).Index(i); i++;
            Map(m => m.Crit2Value).Index(i); i++;
            Map(m => m.Crit2Expression).Index(i); i++;
            Map(m => m.Crit2BaselineUsed).Index(i); i++;
            Map(m => m.Crit2TriggerOnNoData).Index(i); i++;
            Map(m => m.Crit2MetricExpressionConfig).Index(i); i++;

            Map(m => m.Crit3Name).Index(i); i++;
            Map(m => m.Crit3MetricFunction).Index(i); i++;
            Map(m => m.Crit3MetricName).Index(i); i++;
            Map(m => m.Crit3Type).Index(i); i++;
            Map(m => m.Crit3Operator).Index(i); i++;
            Map(m => m.Crit3Value).Index(i); i++;
            Map(m => m.Crit3Expression).Index(i); i++;
            Map(m => m.Crit3BaselineUsed).Index(i); i++;
            Map(m => m.Crit3TriggerOnNoData).Index(i); i++;
            Map(m => m.Crit3MetricExpressionConfig).Index(i); i++;

            Map(m => m.Crit4Name).Index(i); i++;
            Map(m => m.Crit4MetricFunction).Index(i); i++;
            Map(m => m.Crit4MetricName).Index(i); i++;
            Map(m => m.Crit4Type).Index(i); i++;
            Map(m => m.Crit4Operator).Index(i); i++;
            Map(m => m.Crit4Value).Index(i); i++;
            Map(m => m.Crit4Expression).Index(i); i++;
            Map(m => m.Crit4BaselineUsed).Index(i); i++;
            Map(m => m.Crit4TriggerOnNoData).Index(i); i++;
            Map(m => m.Crit4MetricExpressionConfig).Index(i); i++;

            Map(m => m.Crit5Name).Index(i); i++;
            Map(m => m.Crit5MetricFunction).Index(i); i++;
            Map(m => m.Crit5MetricName).Index(i); i++;
            Map(m => m.Crit5Type).Index(i); i++;
            Map(m => m.Crit5Operator).Index(i); i++;
            Map(m => m.Crit5Value).Index(i); i++;
            Map(m => m.Crit5Expression).Index(i); i++;
            Map(m => m.Crit5BaselineUsed).Index(i); i++;
            Map(m => m.Crit5TriggerOnNoData).Index(i); i++;
            Map(m => m.Crit5MetricExpressionConfig).Index(i); i++;

            Map(m => m.Warn1Name).Index(i); i++;
            Map(m => m.Warn1MetricFunction).Index(i); i++;
            Map(m => m.Warn1MetricName).Index(i); i++;
            Map(m => m.Warn1Type).Index(i); i++;
            Map(m => m.Warn1Operator).Index(i); i++;
            Map(m => m.Warn1Value).Index(i); i++;
            Map(m => m.Warn1Expression).Index(i); i++;
            Map(m => m.Warn1BaselineUsed).Index(i); i++;
            Map(m => m.Warn1TriggerOnNoData).Index(i); i++;
            Map(m => m.Warn1MetricExpressionConfig).Index(i); i++;

            Map(m => m.Warn2Name).Index(i); i++;
            Map(m => m.Warn2MetricFunction).Index(i); i++;
            Map(m => m.Warn2MetricName).Index(i); i++;
            Map(m => m.Warn2Type).Index(i); i++;
            Map(m => m.Warn2Operator).Index(i); i++;
            Map(m => m.Warn2Value).Index(i); i++;
            Map(m => m.Warn2Expression).Index(i); i++;
            Map(m => m.Warn2BaselineUsed).Index(i); i++;
            Map(m => m.Warn2TriggerOnNoData).Index(i); i++;
            Map(m => m.Warn2MetricExpressionConfig).Index(i); i++;

            Map(m => m.Warn3Name).Index(i); i++;
            Map(m => m.Warn3MetricFunction).Index(i); i++;
            Map(m => m.Warn3MetricName).Index(i); i++;
            Map(m => m.Warn3Type).Index(i); i++;
            Map(m => m.Warn3Operator).Index(i); i++;
            Map(m => m.Warn3Value).Index(i); i++;
            Map(m => m.Warn3Expression).Index(i); i++;
            Map(m => m.Warn3BaselineUsed).Index(i); i++;
            Map(m => m.Warn3TriggerOnNoData).Index(i); i++;
            Map(m => m.Warn3MetricExpressionConfig).Index(i); i++;

            Map(m => m.Warn4Name).Index(i); i++;
            Map(m => m.Warn4MetricFunction).Index(i); i++;
            Map(m => m.Warn4MetricName).Index(i); i++;
            Map(m => m.Warn4Type).Index(i); i++;
            Map(m => m.Warn4Operator).Index(i); i++;
            Map(m => m.Warn4Value).Index(i); i++;
            Map(m => m.Warn4Expression).Index(i); i++;
            Map(m => m.Warn4BaselineUsed).Index(i); i++;
            Map(m => m.Warn4TriggerOnNoData).Index(i); i++;
            Map(m => m.Warn4MetricExpressionConfig).Index(i); i++;

            Map(m => m.Warn5Name).Index(i); i++;
            Map(m => m.Warn5MetricFunction).Index(i); i++;
            Map(m => m.Warn5MetricName).Index(i); i++;
            Map(m => m.Warn5Type).Index(i); i++;
            Map(m => m.Warn5Operator).Index(i); i++;
            Map(m => m.Warn5Value).Index(i); i++;
            Map(m => m.Warn5Expression).Index(i); i++;
            Map(m => m.Warn5BaselineUsed).Index(i); i++;
            Map(m => m.Warn5TriggerOnNoData).Index(i); i++;
            Map(m => m.Warn5MetricExpressionConfig).Index(i); i++;

            Map(m => m.AffectedEntitiesRawValue).Index(i); i++;
            Map(m => m.CriticalConditionRawValue).Index(i); i++;
            Map(m => m.WarningConditionRawValue).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.HealthRuleID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.HealthRuleLink).Index(i); i++;
        }
    }
}