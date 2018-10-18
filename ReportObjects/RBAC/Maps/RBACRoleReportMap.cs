using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
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
            Map(m => m.CreatedOn).Index(i); i++;
            Map(m => m.CreatedOnUtc).Index(i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            Map(m => m.UpdatedOn).Index(i); i++;
            Map(m => m.UpdatedOnUtc).Index(i); i++;

            Map(m => m.RoleID).Index(i); i++;
        }
    }
}
