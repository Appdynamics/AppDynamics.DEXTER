using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DBCollectorDefinitionReportMap : ClassMap<DBCollectorDefinition>
    {
        public DBCollectorDefinitionReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.CollectorName).Index(i); i++;
            Map(m => m.CollectorType).Index(i); i++;

            Map(m => m.CollectorStatus).Index(i); i++;

            Map(m => m.AgentName).Index(i); i++;

            Map(m => m.Host).Index(i); i++;
            Map(m => m.Port).Index(i); i++;
            Map(m => m.UserName).Index(i); i++;

            Map(m => m.IsEnabled).Index(i); i++;
            Map(m => m.IsLoggingEnabled).Index(i); i++;

            Map(m => m.DatabaseName).Index(i); i++;
            Map(m => m.FailoverPartner).Index(i); i++;
            Map(m => m.SID).Index(i); i++;
            Map(m => m.CustomConnectionString).Index(i); i++;

            Map(m => m.UseWindowsAuth).Index(i); i++;
            Map(m => m.ConnectAsSysDBA).Index(i); i++;
            Map(m => m.UseServiceName).Index(i); i++;
            Map(m => m.UseSSL).Index(i); i++;

            Map(m => m.IsEnterpriseDB).Index(i); i++;

            Map(m => m.IsOSMonitoringEnabled).Index(i); i++;
            Map(m => m.UseLocalWMI).Index(i); i++;
            Map(m => m.HostOS).Index(i); i++;
            Map(m => m.HostDomain).Index(i); i++;
            Map(m => m.HostUserName).Index(i); i++;
            Map(m => m.UseCertificateAuth).Index(i); i++;
            Map(m => m.SSHPort).Index(i); i++;
            Map(m => m.DBInstanceID).Index(i); i++;
            Map(m => m.Region).Index(i); i++;
            Map(m => m.RemoveLiterals).Index(i); i++;
            Map(m => m.IsLDAPEnabled).Index(i); i++;

            Map(m => m.CreatedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            Map(m => m.ModifiedBy).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.ModifiedOn), i); i++;

            Map(m => m.ConfigID).Index(i); i++;

            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
        }
    }
}
