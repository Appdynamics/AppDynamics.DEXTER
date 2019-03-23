using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class RBACPermissionReportMap : ClassMap<RBACPermission>
    {
        public RBACPermissionReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.RoleName).Index(i); i++;
            Map(m => m.PermissionName).Index(i); i++;

            Map(m => m.Allowed).Index(i); i++;

            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;

            Map(m => m.RoleID).Index(i); i++;
            Map(m => m.PermissionID).Index(i); i++;
        }
    }
}
