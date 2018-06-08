using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BusinessTransactionEntryRule20ReportMap : ClassMap<BusinessTransactionEntryRule20>
    {
        public BusinessTransactionEntryRule20ReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.EntryPointType).Index(i); i++;
            Map(m => m.RuleName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Version).Index(i); i++;
            Map(m => m.ScopeName).Index(i); i++;

            Map(m => m.Priority).Index(i); i++;
            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsBackground).Index(i); i++;
            Map(m => m.IsExclusion).Index(i); i++;

            Map(m => m.MatchConditions).Index(i); i++;
            Map(m => m.Actions).Index(i); i++;
            Map(m => m.Properties).Index(i); i++;

            Map(m => m.NumDetectedBTs).Index(i); i++;
            Map(m => m.DetectedBTs).Index(i); i++;

            Map(m => m.RuleRawValue).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}