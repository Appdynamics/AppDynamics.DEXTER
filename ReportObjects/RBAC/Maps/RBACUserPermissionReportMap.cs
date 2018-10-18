using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACUserPermissionReportMap : ClassMap<RBACUserPermission>
    {
        public RBACUserPermissionReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.UserName).Index(i); i++;
            Map(m => m.UserSecurityProvider).Index(i); i++;

            Map(m => m.GroupName).Index(i); i++;
            Map(m => m.GroupSecurityProvider).Index(i); i++;

            Map(m => m.RoleName).Index(i); i++;
            Map(m => m.PermissionName).Index(i); i++;

            Map(m => m.Allowed).Index(i); i++;

            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;

            Map(m => m.UserID).Index(i); i++;
            Map(m => m.GroupID).Index(i); i++;

            Map(m => m.RoleID).Index(i); i++;
            Map(m => m.PermissionID).Index(i); i++;
        }
    }
}
