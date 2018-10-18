using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class RBACRoleMembershipReportMap : ClassMap<RBACRoleMembership>
    {
        public RBACRoleMembershipReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.RoleName).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;

            Map(m => m.EntityID).Index(i); i++;
            Map(m => m.RoleID).Index(i); i++;
        }
    }
}
