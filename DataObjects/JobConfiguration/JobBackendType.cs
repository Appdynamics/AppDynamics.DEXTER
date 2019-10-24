namespace AppDynamics.Dexter
{
    /// <summary>
    /// codebase\controller\controller-api\agent\src\main\java\com\singularity\ee\controller\api\dto\transactionmonitor\TransactionExitPointType.java
    /// </summary>
    public class JobBackendType
    {
        public bool All { get; set; }

        // Common Across Runtimes     
        public bool SOCKET { get; set; }
        public bool HTTP { get; set; }
        public bool CUSTOM { get; set; }
        public bool CUSTOM_ASYNC { get; set; }
        public bool FILE_SERVER { get; set; }
        public bool MAIL_SERVER { get; set; }
        public bool WEB_SERVICE { get; set; }
        public bool ERP { get; set; }
        public bool CACHE { get; set; }
        public bool WEBSPHERE_MQ { get; set; }
        public bool MAINFRAME { get; set; }
        public bool TIBCO_ASYNC { get; set; }
        public bool TIBCO { get; set; }
        public bool ESB { get; set; }
        public bool SAP { get; set; }
        public bool AVRO { get; set; }
        public bool THRIFT { get; set; }
        public bool CASSANDRA { get; set; }
        public bool MQ { get; set; }
        public bool JMS { get; set; }
        public bool WEBSOCKET { get; set; }

        // Java Only
        public bool JDBC { get; set; }
        public bool RMI { get; set; }
        public bool LDAP { get; set; }
        public bool CORBA { get; set; }
        public bool RABBITMQ { get; set; }

        // .NET Only
        public bool ADODOTNET { get; set; }
        public bool DOTNETDirectoryServices { get; set; }
        public bool DOTNETRemoting { get; set; }
        public bool DOTNETMessaging { get; set; }
        public bool WCF { get; set; }
        public bool MSMQ { get; set; }

        // PHP specific
        public bool DB { get; set; }

        // network stats
        public bool NETWORK { get; set; }
    }
}