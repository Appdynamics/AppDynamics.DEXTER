using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DBCollectorDefinition : ConfigurationEntityBase
    {
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string AgentName { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CollectorStatus { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Host { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int Port { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string UserName { get; set; }

        public string CollectorName { get; set; }
        public string CollectorType { get; set; }

        public long ConfigID { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsLoggingEnabled { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DatabaseName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string FailoverPartner { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string SID { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string CustomConnectionString { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseWindowsAuth { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool ConnectAsSysDBA { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseServiceName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseSSL { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsEnterpriseDB { get; set; }

        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsOSMonitoringEnabled { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseLocalWMI { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string HostOS { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string HostDomain { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string HostUserName { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool UseCertificateAuth { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public int SSHPort { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string DBInstanceID { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public string Region { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool RemoveLiterals { get; set; }
        [FieldComparison(FieldComparisonType.ValueComparison)]
        public bool IsLDAPEnabled { get; set; }

        public string CreatedBy{ get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return this.CollectorName;
            }
        }

        public override string EntityName
        {
            get
            {
                return this.CollectorName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "DBCollectorDefinition";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.CollectorType;
            }
        }

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
