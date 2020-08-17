using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class RBACRoleReportMap : ClassMap<RBACRole>
    {
        public RBACRoleReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.RoleName).Index(i); i++;
            Map(m => m.Description).Index(i); i++;

            Map(m => m.NumPermissions).Index(i); i++;

            Map(m => m.ReadOnly).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOnUtc), i); i++;

            Map(m => m.RoleID).Index(i); i++;
        }
    }
}
