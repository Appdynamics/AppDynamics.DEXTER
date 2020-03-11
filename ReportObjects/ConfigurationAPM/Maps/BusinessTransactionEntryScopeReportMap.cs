using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BusinessTransactionEntryScopeReportMap : ClassMap<BusinessTransactionEntryScope>
    {
        public BusinessTransactionEntryScopeReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.ScopeName).Index(i); i++;
            Map(m => m.ScopeType).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Version).Index(i); i++;

            Map(m => m.AffectedTiers).Index(i); i++;
            Map(m => m.NumTiers).Index(i); i++;

            Map(m => m.IncludedRules).Index(i); i++;
            Map(m => m.NumRules).Index(i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}