using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class ErrorDetectionRuleReportMap : ClassMap<ErrorDetectionRule>
    {
        public ErrorDetectionRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.RuleValue).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}