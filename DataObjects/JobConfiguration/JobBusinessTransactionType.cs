namespace AppDynamics.Dexter
{
    /// <summary>
    /// codebase/controller/controller-api/agent/src/main/java/com/singularity/ee/controller/api/dto/transactionmonitor/BusinessTransactionType.java
    /// </summary>
    public class JobBusinessTransactionType
    {
        public bool All { get; set; }

        // Java
        public bool SERVLET { get; set; }
        public bool HTTP { get; set; }
        public bool WEB_SERVICE { get; set; }
        public bool POJO { get; set; }
        public bool JMS { get; set; }
        public bool EJB { get; set; }
        public bool SPRING_BEAN { get; set; }
        public bool STRUTS_ACTION { get; set; }

        //.net
        public bool ASP_DOTNET { get; set; }
        public bool ASP_DOTNET_WEB_SERVICE { get; set; }
        public bool DOTNET_REMOTING { get; set; }
        public bool WCF { get; set; }
        public bool DOTNET_JMS { get; set; }
        public bool POCO { get; set; }

        //common
        public bool BINARY_REMOTING { get; set; }

        //php
        public bool PHP_WEB { get; set; }
        public bool PHP_MVC { get; set; }
        public bool PHP_DRUPAL { get; set; }
        public bool PHP_WORDPRESS { get; set; }
        public bool PHP_CLI { get; set; }
        public bool PHP_WEB_SERVICE { get; set; }

        //node.js
        public bool NODEJS_WEB { get; set; }

        //apache
        public bool NATIVE { get; set; }
        public bool WEB { get; set; }

        //Python
        public bool PYTHON_WEB { get; set; }

        // Ruby
        public bool RUBY_WEB { get; set; }
        public bool RUBY_RAILS { get; set; }
    }
}
