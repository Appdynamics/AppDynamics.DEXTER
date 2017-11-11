using CsvHelper.Configuration;

namespace AppDynamics.Dexter.DataObjects
{
    public class BusinessTransactionEntryRuleScopeReportMap : ClassMap<BusinessTransactionEntryScope>
    {
        public BusinessTransactionEntryRuleScopeReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.ScopeName).Index(i); i++;
            Map(m => m.ScopeType).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Version).Index(i); i++;

            Map(m => m.IncludedTiers).Index(i); i++;
            Map(m => m.NumTiers).Index(i); i++;

            Map(m => m.IncludedRules).Index(i); i++;
            Map(m => m.NumRules).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}