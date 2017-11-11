using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class CustomExitRuleReportMap : ClassMap<CustomExitRule>
    {
        public CustomExitRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.ExitType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.MatchClass).Index(i); i++;
            Map(m => m.MatchMethod).Index(i); i++;
            Map(m => m.MatchType).Index(i); i++;
            Map(m => m.MatchParameterTypes).Index(i); i++;
            Map(m => m.IsApplyToAllBTs).Index(i); i++;
            
            Map(m => m.DataCollectorsConfig).Index(i); i++;
            Map(m => m.InfoPointsConfig).Index(i); i++;
            Map(m => m.RuleRawValue).Index(i); i++;

            Map(m => m.NumDetectedBackends).Index(i); i++;
            Map(m => m.DetectedBackends).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}