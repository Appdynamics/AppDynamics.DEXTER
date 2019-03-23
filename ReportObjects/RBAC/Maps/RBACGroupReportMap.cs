using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class RBACGroupReportMap : ClassMap<RBACGroup>
    {
        public RBACGroupReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.GroupName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.SecurityProvider).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            Map(m => m.CreatedOn).Index(i); i++;
            Map(m => m.CreatedOnUtc).Index(i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            Map(m => m.UpdatedOn).Index(i); i++;
            Map(m => m.UpdatedOnUtc).Index(i); i++;

            Map(m => m.GroupID).Index(i); i++;
        }
    }
}
