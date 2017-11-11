using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class InformationPointRuleReportMap : ClassMap<InformationPointRule>
    {
        public InformationPointRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.MatchClass).Index(i); i++;
            Map(m => m.MatchMethod).Index(i); i++;
            Map(m => m.MatchType).Index(i); i++;
            Map(m => m.MatchParameterTypes).Index(i); i++;
            Map(m => m.MatchCondition).Index(i); i++;
            Map(m => m.InfoPointsConfig).Index(i); i++;
            Map(m => m.RuleRawValue).Index(i); i++;
            
            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}