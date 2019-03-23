using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBCollectorDefinition : ConfigurationEntityBase
    {
        public string AgentName { get; set; }

        public string CollectorStatus { get; set; }

        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }

        public string CollectorName { get; set; }
        public string CollectorType { get; set; }

        public long ConfigID { get; set; }

        public bool IsEnabled { get; set; }
        public bool IsLoggingEnabled { get; set; }

        public string DatabaseName { get; set; }
        public string FailoverPartner { get; set; }
        public string SID { get; set; }
        public string CustomConnectionString { get; set; }

        public bool UseWindowsAuth { get; set; }
        public bool ConnectAsSysDBA { get; set; }
        public bool UseServiceName { get; set; }
        public bool UseSSL { get; set; }

        public bool IsEnterpriseDB { get; set; }

        public bool IsOSMonitoringEnabled { get; set; }
        public bool UseLocalWMI { get; set; }
        public string HostOS { get; set; }
        public string HostDomain { get; set; }
        public string HostUserName { get; set; }
        public bool UseCertificateAuth { get; set; }
        public int SSHPort { get; set; }
        public string DBInstanceID { get; set; }
        public string Region { get; set; }
        public bool RemoveLiterals { get; set; }
        public bool IsLDAPEnabled { get; set; }

        public string CreatedBy{ get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DBCollectorDefinition: {0}/{1}({2}) [{3}]",
                this.Controller,
                this.CollectorName,
                this.ConfigID, 
                this.CollectorType);
        }
    }
}
