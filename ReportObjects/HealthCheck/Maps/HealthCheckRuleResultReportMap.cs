using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class HealthCheckRuleResultReportMap : ClassMap<HealthCheckRuleResult>
    {
        public HealthCheckRuleResultReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.Application).Index(i); i++;

            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;
            
            Map(m => m.Category).Index(i); i++;
            Map(m => m.Code).Index(i); i++;
            Map(m => m.Name).Index(i); i++;
            Map(m => m.Grade).Index(i); i++;
            Map(m => m.Description).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.EvaluationTime), i); i++;
            Map(m => m.Version).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;

            Map(m => m.RuleLink).Index(i); i++;
        }
    }
}