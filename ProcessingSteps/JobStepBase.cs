using AppDynamics.Dexter.ReportObjects;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class JobStepBase 
    {
        #region Constants for metric retrieval and mapping

        // Constants for metric naming
        internal const string METRIC_ART_FULLNAME = "Average Response Time (ms)";
        internal const string METRIC_CPM_FULLNAME = "Calls per Minute";
        internal const string METRIC_EPM_FULLNAME = "Errors per Minute";
        internal const string METRIC_EXCPM_FULLNAME = "Exceptions per Minute";
        internal const string METRIC_HTTPEPM_FULLNAME = "HTTP Error Codes per Minute";

        #endregion

        #region Event types

        // There are a bazillion types of events
        //      https://docs.appdynamics.com/display/PRO44/Events+Reference
        //      https://docs.appdynamics.com/display/PRO43/Remediation+Scripts
        //      https://docs.appdynamics.com/display/PRO43/Build+a+Custom+Action
        // They are defined here :
        //      C:\appdynamics\codebase\controller\controller-api\agent\src\main\java\com\singularity\ee\controller\api\constants\EventType.java
        // But filtering that to only the ones that aren't deprecated
        internal static List<string> EVENT_TYPES = new List<string>
        {
            // Events UI: Application Changes
            // App Server Restart
            { "APP_SERVER_RESTART" },
            // Thrown when application parameters change, like JVM options, etc
            { "APPLICATION_CONFIG_CHANGE" },
            // This is injected by user / REST API.
            { "APPLICATION_DEPLOYMENT" },

            // Events UI: Code problems
            // Code deadlock detected by Agent
            { "DEADLOCK" },
            // This is thrown when any resource pool size is reached, thread pool, connection pool etc. fall into this category
            { "RESOURCE_POOL_LIMIT" },
                       
            // Events UI: Custom
            // Custom Events thrown by API calls using REST or machine agent API
            { "CUSTOM" },

            // Events UI: Server Crashes
            { "APPLICATION_CRASH" },
            // CLR Crash
            { "CLR_CRASH" },            

            // Events UI: Health Rule Violations
            // Health rules
            { "POLICY_OPEN_WARNING" },
            { "POLICY_OPEN_CRITICAL" },
            { "POLICY_CLOSE_WARNING" },
            { "POLICY_CLOSE_CRITICAL" },
            { "POLICY_UPGRADED" },
            { "POLICY_DOWNGRADED" },
            { "POLICY_CANCELED_WARNING" },
            { "POLICY_CANCELED_CRITICAL" },
            { "POLICY_CONTINUES_CRITICAL" },
            { "POLICY_CONTINUES_WARNING" },

            // Events UI: Error
            // This is thrown when the agent detects and error NOT during a BT (no BT id on thread)
            { "APPLICATION_ERROR" },
            { "BUSINESS_ERROR" },

            // Events UI: Not possible - this is just a query here
            // Diagnostic session.  There are several subTypes for this.
            { "DIAGNOSTIC_SESSION" },

            // Agent Config
            { "AGENT_CONFIGURATION_ERROR" },

            // Registration limits for Agent
            { "AGENT_ADD_BLACKLIST_REG_LIMIT_REACHED" },
            { "AGENT_ASYNC_ADD_REG_LIMIT_REACHED" },
            { "AGENT_ERROR_ADD_REG_LIMIT_REACHED" },
            { "AGENT_METRIC_BLACKLIST_REG_LIMIT_REACHED" },
            { "AGENT_METRIC_REG_LIMIT_REACHED" },

            // Registration limits for Controller
            { "CONTROLLER_ASYNC_ADD_REG_LIMIT_REACHED" },
            { "CONTROLLER_COLLECTIONS_ADD_REG_LIMIT_REACHED" },
            { "CONTROLLER_ERROR_ADD_REG_LIMIT_REACHED" },
            { "CONTROLLER_EVENT_UPLOAD_LIMIT_REACHED" },
            { "CONTROLLER_MEMORY_ADD_REG_LIMIT_REACHED" },
            { "CONTROLLER_METADATA_REGISTRATION_LIMIT_REACHED" },
            { "CONTROLLER_METRIC_DATA_BUFFER_OVERFLOW" },
            { "CONTROLLER_METRIC_REG_LIMIT_REACHED" },
            { "CONTROLLER_PSD_UPLOAD_LIMIT_REACHED" },
            { "CONTROLLER_RSD_UPLOAD_LIMIT_REACHED" },
            { "CONTROLLER_SEP_ADD_REG_LIMIT_REACHED" },
            { "CONTROLLER_STACKTRACE_ADD_REG_LIMIT_REACHED" },
            { "CONTROLLER_TRACKED_OBJECT_ADD_REG_LIMIT_REACHED" },

            // Custom Actions
            { "CUSTOM_ACTION_STARTED" },
            { "CUSTOM_ACTION_FAILED" },
            { "CUSTOM_ACTION_END" },
            { "CUSTOM_EMAIL_ACTION_STARTED" },
            { "CUSTOM_EMAIL_ACTION_FAILED" },
            { "EMAIL_ACTION_FAILED" },
            { "CUSTOM_EMAIL_ACTION_END" },
            { "HTTP_REQUEST_ACTION_STARTED" },
            { "HTTP_REQUEST_ACTION_FAILED" },
            { "HTTP_REQUEST_ACTION_END" },
            { "RUNBOOK_DIAGNOSTIC SESSION_STARTED" },
            { "RUNBOOK_DIAGNOSTIC SESSION_FAILED" },
            { "RUNBOOK_DIAGNOSTIC SESSION_END" },
            { "RUN_LOCAL_SCRIPT_ACTION_STARTED" },
            { "RUN_LOCAL_SCRIPT_ACTION_FAILED" },
            { "RUN_LOCAL_SCRIPT_ACTION_END" },
            { "THREAD_DUMP_ACTION_END" },
            { "THREAD_DUMP_ACTION_FAILED" },
            { "THREAD_DUMP_ACTION_STARTED" },
            { "WORKFLOW_ACTION_END" },
            { "WORKFLOW_ACTION_FAILED" },
            { "WORKFLOW_ACTION_STARTED" },

            // Notifications
            { "EMAIL_SENT" },
            { "SMS_SENT" },

            // Discovery
            { "APPLICATION_DISCOVERED" },
            { "MACHINE_DISCOVERED" },
            { "NODE_DISCOVERED" },
            { "SERVICE_ENDPOINT_DISCOVERED" },
            { "TIER_DISCOVERED" },
            { "BT_DISCOVERED" },
            { "BACKEND_DISCOVERED" },

            // Database
            // Yeah we can't spell
            { "DB_SERVER_PARAMTER_CHANGE" },

            // Memory Leaks
            { "MEMORY" },
            { "MEMORY_LEAK_DIAGNOSTICS" },
            { "OBJECT_CONTENT_SUMMARY" },

            // Others
            { "CONTROLLER_AGENT_VERSION_INCOMPATIBILITY" },
            { "LICENSE" },
            { "DISK_SPACE" },
            { "DEV_MODE_CONFIG_UPDATE" },
            { "AGENT_STATUS" },
            { "AGENT_DIAGNOSTICS" },
            { "AGENT_EVENT" },
            { "APPDYNAMICS_DATA" },
            { "APPDYNAMICS_INTERNAL_DIAGNOSTICS" },
            { "WARROOM_NOTE" }
        };

        #endregion

        #region Configuration comparison variables

        public const string BLANK_APPLICATION_CONTROLLER = "https://reference.controller";
        public const string BLANK_APPLICATION_APPLICATION = "ReferenceApp";

        public const string PROPERTY_ENTIRE_OBJECT = "EntireObject";
        public const string DIFFERENCE_IDENTICAL = "IDENTICAL";
        public const string DIFFERENCE_MISSING = "MISSING";
        public const string DIFFERENCE_EXTRA = "EXTRA";
        public const string DIFFERENCE_DIFFERENT = "DIFFERENT";

        #endregion

        internal static Logger logger = LogManager.GetCurrentClassLogger();
        internal static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        public FilePathMap FilePathMap { get; set; }

        public void DisplayJobStepStartingStatus(JobConfiguration jobConfiguration)
        {
            logger.Info("{0}({0:d}): Starting", jobConfiguration.Status);
            loggerConsole.Trace("{0}({0:d}): Starting", jobConfiguration.Status);
        }

        public void DisplayJobStepEndedStatus(JobConfiguration jobConfiguration, Stopwatch stopWatch)
        {
            logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobConfiguration.Status, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobConfiguration.Status, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }

        public void DisplayJobTargetStartingStatus(JobConfiguration jobConfiguration, JobTarget jobTarget, int jobTargetIndex)
        {
            logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}({5})", jobConfiguration.Status, jobTargetIndex, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.ApplicationID);
            loggerConsole.Info("{0}({0:d}): [{1}/{2}], {3} {4}({5})", jobConfiguration.Status, jobTargetIndex, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.ApplicationID);
        }

        public void DisplayJobTargetEndedStatus(JobConfiguration jobConfiguration, JobTarget jobTarget, int jobTargetIndex, Stopwatch stopWatch)
        {
            logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobConfiguration.Status, jobTargetIndex, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobConfiguration.Status, jobTargetIndex, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
        }

        public virtual bool Execute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            return false;
        }

        public virtual bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            return false;
        }

        internal List<MetricExtractMapping> getMetricsExtractMappingList(JobConfiguration jobConfiguration)
        {

            List<MetricExtractMapping> entityMetricExtractMappingList = FileIOHelper.ReadListFromCSVFile<MetricExtractMapping>(FilePathMap.EntityMetricExtractMappingFilePath(), new MetricExtractMappingReportMap());

            List<MetricExtractMapping> entityMetricExtractMappingListFiltered = new List<MetricExtractMapping>(entityMetricExtractMappingList.Count);
            foreach (string metricSet in jobConfiguration.Input.MetricsSelectionCriteria)
            {
                List<MetricExtractMapping> entityMetricExtractMappingListForMetricSet = entityMetricExtractMappingList.Where(m => m.MetricSet == metricSet).ToList();
                if (entityMetricExtractMappingListForMetricSet != null)
                {
                    logger.Info("Input job specified {0} metric set, resulted in {1} metrics from mapping file", metricSet, entityMetricExtractMappingListForMetricSet.Count);
                    loggerConsole.Info("Input job specified {0} metric set, resulted in {1} metrics from mapping file", metricSet, entityMetricExtractMappingListForMetricSet.Count);
                    entityMetricExtractMappingListFiltered.AddRange(entityMetricExtractMappingListForMetricSet);
                    foreach (MetricExtractMapping mem in entityMetricExtractMappingListForMetricSet)
                    {
                        logger.Trace("{0}, path={1}", mem, mem.MetricPath);
                    }
                }
            }
            logger.Info("Selected {0} metrics from mapping file", entityMetricExtractMappingListFiltered.Count);
            loggerConsole.Info("Selected {0} metrics from mapping file", entityMetricExtractMappingListFiltered.Count);

            return entityMetricExtractMappingListFiltered;
        }

        internal Dictionary<string, List<MethodCallLineClassTypeMapping>> populateMethodCallMappingDictionary(string methodCallLinesToFrameworkTypeMappingFilePath)
        {
            List<MethodCallLineClassTypeMapping> methodCallLineClassToFrameworkTypeMappingList = FileIOHelper.ReadListFromCSVFile<MethodCallLineClassTypeMapping>(methodCallLinesToFrameworkTypeMappingFilePath, new MethodCallLineClassTypeMappingReportMap());            
            methodCallLineClassToFrameworkTypeMappingList = methodCallLineClassToFrameworkTypeMappingList.OrderByDescending(m => m.ClassPrefix).ToList();
            Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary = new Dictionary<string, List<MethodCallLineClassTypeMapping>>(26);
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("a", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "a").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("b", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "b").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("c", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "c").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("d", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "d").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("e", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "e").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("f", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "f").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("g", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "g").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("h", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "h").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("i", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "i").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("j", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "j").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("k", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "k").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("l", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "l").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("m", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "m").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("n", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "n").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("o", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "o").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("p", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "p").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("q", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "q").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("r", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "r").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("s", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "s").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("t", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "t").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("u", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "u").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("v", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "v").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("w", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "w").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("x", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "x").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("y", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "y").ToList());
            methodCallLineClassToFrameworkTypeMappingDictionary.Add("z", methodCallLineClassToFrameworkTypeMappingList.Where(m => m.ClassPrefix.Substring(0, 1).ToLower() == "z").ToList());

            return methodCallLineClassToFrameworkTypeMappingDictionary;
        }
    }
}
