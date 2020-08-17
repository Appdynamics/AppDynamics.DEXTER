using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class HealthRuleViolationEventReportMap : ClassMap<HealthRuleViolationEvent>
    {
        public HealthRuleViolationEventReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.EventID).Index(i); i++;
            Map(m => m.Severity).Index(i); i++;
            Map(m => m.Status).Index(i); i++;
            Map(m => m.Description).Index(i); i++;

            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.From), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.To), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.FromUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ToUtc), i); i++;

            Map(m => m.HealthRuleName).Index(i); i++;

            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.HealthRuleID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.HealthRuleLink).Index(i); i++;
            Map(m => m.EntityLink).Index(i); i++;
            Map(m => m.EventLink).Index(i); i++;
        }
    }
}