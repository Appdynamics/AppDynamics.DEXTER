using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class RBACControllerSummaryReportMap : ClassMap<RBACControllerSummary>
    {
        public RBACControllerSummaryReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.SecurityProvider).Index(i); i++;
            Map(m => m.IsStrongPasswords).Index(i); i++;

            Map(m => m.NumUsers).Index(i); i++;
            Map(m => m.NumGroups).Index(i); i++;
            Map(m => m.NumRoles).Index(i); i++;
        }
    }
}
