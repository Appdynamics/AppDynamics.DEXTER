using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionEntryRuleReportMap : ClassMap<BusinessTransactionEntryRule>
    {
        public BusinessTransactionEntryRuleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.TierName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.EntryPointType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.Priority).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsBackground).Index(i); i++;
            Map(m => m.IsExcluded).Index(i); i++;
            Map(m => m.IsExclusion).Index(i); i++;

            Map(m => m.MatchClass).Index(i); i++;
            Map(m => m.MatchMethod).Index(i); i++;
            Map(m => m.MatchURI).Index(i); i++;
            Map(m => m.Parameters).Index(i); i++;
            Map(m => m.SplitConfig).Index(i); i++;

            Map(m => m.RuleRawValue).Index(i); i++;

            Map(m => m.NumDetectedBTs).Index(i); i++;
            Map(m => m.DetectedBTs).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}