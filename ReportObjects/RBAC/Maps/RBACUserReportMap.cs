using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
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
            Map(m => m.CreatedOn).Index(i); i++;
            Map(m => m.CreatedOnUtc).Index(i); i++;

            Map(m => m.UpdatedBy).Index(i); i++;
            Map(m => m.UpdatedOn).Index(i); i++;
            Map(m => m.UpdatedOnUtc).Index(i); i++;

            Map(m => m.UserID).Index(i); i++;
        }
    }
}
