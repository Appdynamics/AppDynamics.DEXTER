using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BSGBusinessTransactionResultMap : ClassMap<BSGBusinessTransactionResult>
    {
        public BSGBusinessTransactionResultMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;
            
            Map(m => m.NumBTs).Index(i); i++;
            Map(m => m.NumBTsWithLoad).Index(i); i++;
            Map(m => m.NumBTsOverflow).Index(i); i++;

            Map(m => m.BTLimitExceeded).Index(i); i++;
            Map(m => m.BTCleanupEnabled).Index(i); i++;
            Map(m => m.BTLockdownEnabled).Index(i); i++;

            Map(m => m.NumBT20DiscoveryRules).Index(i); i++;
            Map(m => m.NumBT20EntryRules).Index(i); i++;
            Map(m => m.NumBT20ExcludeRules).Index(i); i++;
            Map(m => m.NumBT20CustomEntryRules).Index(i); i++;
            Map(m => m.NumBT20CustomExcludeRules).Index(i); i++;
            Map(m => m.NumBT20ExcludeRules).Index(i); i++;
            Map(m => m.NumBT20Scopes).Index(i); i++;

            Map(m => m.NumBTDiscoveryRules).Index(i); i++;
            Map(m => m.NumBTEntryRules).Index(i); i++;
            Map(m => m.NumBTExcludeRules).Index(i); i++;
            Map(m => m.NumBTCustomEntryRules).Index(i); i++;
            Map(m => m.NumBTCustomExcludeRules).Index(i); i++;
            Map(m => m.NumBTScopes).Index(i); i++;

        }
    }
}
