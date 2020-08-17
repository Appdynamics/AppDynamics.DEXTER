using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class RBACUserReportMap : ClassMap<RBACUser>
    {
        public RBACUserReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;

            Map(m => m.UserName).Index(i); i++;
            Map(m => m.DisplayName).Index(i); i++;
            Map(m => m.Email).Index(i); i++;
            Map(m => m.SecurityProvider).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOnUtc), i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOn), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.UpdatedOnUtc), i); i++;

            Map(m => m.UserID).Index(i); i++;
        }
    }
}
