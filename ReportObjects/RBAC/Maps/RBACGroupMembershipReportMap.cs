using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class RBACGroupMembershipReportMap : ClassMap<RBACGroupMembership>
    {
        public RBACGroupMembershipReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.GroupName).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;

            Map(m => m.GroupID).Index(i); i++;
            Map(m => m.UserID).Index(i); i++;
        }
    }
}
