using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjects
{
    public class AuditEventReportMap : ClassMap<AuditEvent>
    {
        public AuditEventReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            //Map(m => m.ApplicationName).Index(i); i++;
            //Map(m => m.ApplicationID).Index(i); i++;

            Map(m => m.UserName).Index(i); i++;
            Map(m => m.LoginType).Index(i); i++;
            Map(m => m.AccountName).Index(i); i++;

            Map(m => m.Action).Index(i); i++;
            Map(m => m.EntityName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntityID).Index(i); i++;

            Map(m => m.Occurred).Index(i); i++;
            Map(m => m.OccurredUtc).Index(i); i++;

        }
    }
}