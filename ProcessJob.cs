using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.Extensions;
using CsvHelper;
using Newtonsoft.Json.Linq;
using NLog;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AppDynamics.Dexter
{
    public class ProcessJob
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        #region Constants for metric retrieval

        private const string METRIC_TIME_MS = "Time (ms)";

        // Constants for metric naming
        private const string METRIC_ART_FULLNAME = "Average Response Time (ms)";
        private const string METRIC_CPM_FULLNAME = "Calls per Minute";
        private const string METRIC_EPM_FULLNAME = "Errors per Minute";
        private const string METRIC_EXCPM_FULLNAME = "Exceptions per Minute";
        private const string METRIC_HTTPEPM_FULLNAME = "HTTP Error Codes per Minute";

        //Overall Application Performance|Calls per Minute
        private const string METRIC_PATH_APPLICATION = "Overall Application Performance|{0}";

        //Overall Application Performance|Web|Calls per Minute
        //Overall Application Performance|*|Calls per Minute
        private const string METRIC_PATH_TIER = "Overall Application Performance|{0}|{1}";

        //Overall Application Performance|Web|Individual Nodes|*|Calls per Minute
        //Overall Application Performance|*|Individual Nodes|*|Calls per Minute
        private const string METRIC_PATH_NODE = "Overall Application Performance|{0}|Individual Nodes|{1}|{2}";

        //Business Transaction Performance|Business Transactions|Web|AppHttpHandler ashx services|Calls per Minute
        //Business Transaction Performance|Business Transactions|*|AppHttpHandler ashx services|Calls per Minute
        private const string METRIC_PATH_BUSINESS_TRANSACTION = "Business Transaction Performance|Business Transactions|{0}|{1}|{2}";

        //Business Transaction Performance|Business Transactions|Web|AppHttpHandler ashx services|Individual Nodes|*|Calls per Minute
        //Business Transaction Performance|Business Transactions|*|AppHttpHandler ashx services|Individual Nodes|*|Calls per Minute
        // Not going to support that one

        //Backends|Discovered backend call - Azure ACS OAuth CloudSync-login.windows.net-443|Calls per Minute
        private const string METRIC_PATH_BACKEND = "Backends|Discovered backend call - {0}|{1}";

        //Overall Application Performance|Web|External Calls|Call-HTTP to Discovered backend call - Azure ACS OAuth CloudSync-login.windows.net-443|Calls per Minute
        //Overall Application Performance|Web|Individual Nodes|*|External Calls|Call-HTTP to Discovered backend call - Azure ACS OAuth CloudSync-login.windows.net-443|Calls per Minute
        //Overall Application Performance|*|Individual Nodes|*|External Calls|Call-HTTP to Discovered backend call - Azure ACS OAuth CloudSync-login.windows.net-443|Calls per Minute
        // Not going to support that one

        //Errors|Web|CrmException|Errors per Minute
        private const string METRIC_PATH_ERROR = "Errors|{0}|{1}|{2}";

        //Errors|Web|CrmException|Individual Nodes|*|Errors per Minute
        // Not going to support that one

        //Service Endpoints|Web|CrmAction.Execute|Calls per Minute
        private const string METRIC_PATH_SERVICE_ENDPOINT = "Service Endpoints|{0}|{1}|{2}";

        //Service End Points|Web|CrmAction.Execute|Individual Nodes|*|Calls per Minute
        //Service End Points|*|CrmAction.Execute|Individual Nodes|*|Calls per Minute
        // Not going to support that one

        #endregion

        #region Constants for the folder and file names of data extract

        // Parent Folder names
        private const string ENTITIES_FOLDER_NAME = "ENT";
        private const string CONFIGURATION_FOLDER_NAME = "CFG";
        private const string METRICS_FOLDER_NAME = "METR";
        private const string SNAPSHOTS_FOLDER_NAME = "SNAP";
        private const string SNAPSHOT_FOLDER_NAME = "{0}.{1}";
        private const string EVENTS_FOLDER_NAME = "EVT";
        private const string REPORTS_FOLDER_NAME = "RPT";

        // More folder names for entity types
        private const string APPLICATION_TYPE_SHORT = "APP";
        private const string TIERS_TYPE_SHORT = "TIER";
        private const string NODES_TYPE_SHORT = "NODE";
        private const string BACKENDS_TYPE_SHORT = "BACK";
        private const string BUSINESS_TRANSACTIONS_TYPE_SHORT = "BT";
        private const string SERVICE_ENDPOINTS_TYPE_SHORT = "SEP";
        private const string ERRORS_TYPE_SHORT = "ERR";

        // Metric folder names
        private const string METRIC_ART_SHORTNAME = "ART";
        private const string METRIC_CPM_SHORTNAME = "CPM";
        private const string METRIC_EPM_SHORTNAME = "EPM";
        private const string METRIC_EXCPM_SHORTNAME = "EXCPM";
        private const string METRIC_HTTPEPM_SHORTNAME = "HTTPEPM";

        private static Dictionary<string, string> metricNameToShortMetricNameMapping = new Dictionary<string, string>()
        {
            {METRIC_ART_FULLNAME, METRIC_ART_SHORTNAME},
            {METRIC_CPM_FULLNAME, METRIC_CPM_SHORTNAME},
            {METRIC_EPM_FULLNAME, METRIC_EPM_SHORTNAME},
            {METRIC_EXCPM_FULLNAME, METRIC_EXCPM_SHORTNAME},
            {METRIC_HTTPEPM_FULLNAME, METRIC_HTTPEPM_SHORTNAME},
        };

        // Metadata file names
        private const string EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME = "configuration.xml";
        private const string EXTRACT_CONFIGURATION_CONTROLLER_FILE_NAME = "settings.json";
        private const string EXTRACT_ENTITY_APPLICATIONS_FILE_NAME = "applications.json";
        private const string EXTRACT_ENTITY_APPLICATION_FILE_NAME = "application.json";
        private const string EXTRACT_ENTITY_TIERS_FILE_NAME = "tiers.json";
        private const string EXTRACT_ENTITY_NODES_FILE_NAME = "nodes.json";
        private const string EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.json";
        private const string EXTRACT_ENTITY_BACKENDS_FILE_NAME = "backends.json";
        private const string EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME = "serviceendpoints.json";
        private const string EXTRACT_ENTITY_SERVICE_ENDPOINTS_ALL_FILE_NAME = "serviceendpointsdetail.json";
        private const string EXTRACT_ENTITY_ERRORS_FILE_NAME = "errors.json";
        private const string EXTRACT_ENTITY_NAME_FILE_NAME = "name.json";

        // Metric data file names
        private const string EXTRACT_METRIC_FULL_FILE_NAME = "full.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_METRIC_HOUR_FILE_NAME = "hour.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // Flowmap file names
        private const string EXTRACT_ENTITY_FLOWMAP_FILE_NAME = "flowmap.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // List of Snapshots file names
        private const string EXTRACT_SNAPSHOTS_FILE_NAME = "snapshots.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const int SNAPSHOTS_QUERY_PAGE_SIZE = 600;

        // Snapshot file names
        private const string EXTRACT_SNAPSHOT_FLOWMAP_FILE_NAME = "flowmap.json";
        private const string EXTRACT_SNAPSHOT_SEGMENT_LIST_NAME = "segments.json";
        private const string EXTRACT_SNAPSHOT_SEGMENT_DATA_FILE_NAME = "segment.{0}.json";
        private const string EXTRACT_SNAPSHOT_SEGMENT_CALLGRAPH_FILE_NAME = "callgraph.{0}.json";
        private const string EXTRACT_SNAPSHOT_SEGMENT_ERROR_FILE_NAME = "error.{0}.json";

        // Mapping for snapshot names
        private static Dictionary<string, string> userExperienceFolderNameMapping = new Dictionary<string, string>
        {
            {"NORMAL", "NM"},
            {"SLOW", "SL"},
            {"VERY_SLOW", "VS"},
            {"STALL", "ST"},
            {"ERROR", "ER"}
        };

        // Snapshots file names
        private const string HEALTH_RULE_VIOLATIONS_FILE_NAME = "healthruleviolations.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EVENTS_FILE_NAME = "{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // There are a bazillion types of events
        // Source C:\appdynamics\codebase\controller\controller-api\agent\src\main\java\com\singularity\ee\controller\api\constants\EventType.java
        // They are sort of documented here:
        //      https://docs.appdynamics.com/display/PRO43/Remediation+Scripts
        //      https://docs.appdynamics.com/display/PRO43/Build+a+Custom+Action
        // Choosing select few that I care about
        private static List<string> eventTypes = new List<string>
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

            // Events UI: Not possible - this is just a query here
            // Diagnostic session.  There are several subTypes for this.
            { "DIAGNOSTIC_SESSION" },
        };

        private const string ENTITY_TYPE_APPLICATION = "APPLICATION";
        private const string ENTITY_TYPE_APPLICATION_MOBILE = "MOBILE_APPLICATION";
        private const string ENTITY_TYPE_TIER = "APPLICATION_COMPONENT";
        private const string ENTITY_TYPE_NODE = "APPLICATION_COMPONENT_NODE";
        private const string ENTITY_TYPE_MACHINE = "MACHINE_INSTANCE";
        private const string ENTITY_TYPE_BUSINESS_TRANSACTION = "BUSINESS_TRANSACTION";
        private const string ENTITY_TYPE_BACKEND = "BACKEND";
        private const string ENTITY_TYPE_HEALTH_RULE = "POLICY";

        // Mapping of long entity types to human readable ones
        private static Dictionary<string, string> entityTypeStringMapping = new Dictionary<string, string>
        {
            {ENTITY_TYPE_APPLICATION, "Application"},
            {ENTITY_TYPE_APPLICATION_MOBILE, "Mobile App"},
            {ENTITY_TYPE_TIER, "Tier"},
            {ENTITY_TYPE_NODE, "Node"},
            {ENTITY_TYPE_MACHINE, "Machine"},
            {ENTITY_TYPE_BUSINESS_TRANSACTION, "BT"},
            {ENTITY_TYPE_BACKEND, "Backend" },
            {ENTITY_TYPE_HEALTH_RULE, "Health Rule"}
        };

        #endregion

        #region Constants for the folder and file names of data index

        // Detected entity report conversion file names
        private const string CONVERT_ENTITY_CONTROLLER_FILE_NAME = "controller.csv";
        private const string CONVERT_ENTITY_CONTROLLERS_FILE_NAME = "controllers.csv";
        private const string CONVERT_ENTITY_APPLICATIONS_FILE_NAME = "applications.csv";
        private const string CONVERT_ENTITY_APPLICATION_FILE_NAME = "application.csv";
        private const string CONVERT_ENTITY_TIERS_FILE_NAME = "tiers.csv";
        private const string CONVERT_ENTITY_NODES_FILE_NAME = "nodes.csv";
        private const string CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.csv";
        private const string CONVERT_ENTITY_BACKENDS_FILE_NAME = "backends.csv";
        private const string CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME = "serviceendpoints.csv";
        private const string CONVERT_ENTITY_ERRORS_FILE_NAME = "errors.csv";

        // Metric report conversion file name
        private const string CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME = "entities.full.csv";
        private const string CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME = "entities.hour.csv";
        private const string CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME = "entity.full.csv";
        private const string CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME = "entity.hour.csv";
        private const string CONVERT_METRIC_VALUES_FILE_NAME = "metric.values.csv";
        private const string CONVERT_METRIC_SUMMARY_FILE_NAME = "metric.summary.csv";

        // Flow map to flow grid conversion file name
        private const string CONVERT_ACTIVITY_GRID_FILE_NAME = "activitygrid.full.csv";

        // Events list
        private const string CONVERT_EVENTS_FILE_NAME = "events.csv";
        private const string CONVERT_HEALTH_RULE_EVENTS_FILE_NAME = "hrviolationevents.csv";
        private const string CONVERT_EVENTS_FILTERED_FILE_NAME = "events.{0}.csv";
        private const string CONVERT_HEALTH_RULE_EVENTS_FILTERED_FILE_NAME = "hrviolationevents.{0}.csv";

        #endregion

        #region Constants for the folder and file names of data reports

        // Report file names
        private const string REPORT_DETECTED_ENTITIES_FILE_NAME = "{0}.{1:yyyyMMddHH}-{2:yyyyMMddHH}.DetectedEntities.xlsx";
        private const string REPORT_METRICS_ALL_ENTITIES_FILE_NAME = "{0}.{1:yyyyMMddHH}-{2:yyyyMMddHH}.EntityMetrics.xlsx";
        private const string REPORT_DETECTED_EVENTS_FILE_NAME = "{0}.{1:yyyyMMddHH}-{2:yyyyMMddHH}.Events.xlsx";
        private const string REPORT_ENTITY_DETAILS_APPLICATION_FILE_NAME = "{0}.{1:yyyyMMddHH}-{2:yyyyMMddHH}.{3}.{4}.xlsx";
        private const string REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME = "{0}.{1:yyyyMMddHH}-{2:yyyyMMddHH}.{3}.{4}.{5}.xlsx";

        #endregion

        #region Constants for Common Reports sheets

        private const string REPORT_SHEET_PARAMETERS = "1.Parameters";
        private const string REPORT_SHEET_TOC = "2.TOC";

        #endregion

        #region Constants for Detected Entities Report generation Excel contents

        private const string REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST = "4.Applications";
        private const string REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST = "5.Tiers";
        private const string REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT = "5.Tiers.Pivot";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST = "6.Nodes";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT = "6.Nodes.Type.AppAgent";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT = "6.Nodes.Type.MachineAgent";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST = "7.Backends";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT = "7.Backends.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT = "7.Backends.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST = "8.BTs";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT = "8.BTs.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT = "8.BTs.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST = "9.SEPs";
        private const string REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT = "9.SEPs.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT = "9.SEPs.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST = "10.Errors";
        private const string REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT = "10.Errors.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION = "10.Errors.Location";

        private const string REPORT_DETECTED_ENTITIES_TABLE_TOC = "t_TOC";
        private const string REPORT_DETECTED_ENTITIES_TABLE_PARAMETERS_TARGETS = "t_InputTargets";
        private const string REPORT_DETECTED_ENTITIES_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_DETECTED_ENTITIES_TABLE_APPLICATIONS = "t_Applications";
        private const string REPORT_DETECTED_ENTITIES_TABLE_TIERS = "t_Tiers";
        private const string REPORT_DETECTED_ENTITIES_TABLE_NODES = "t_Nodes";
        private const string REPORT_DETECTED_ENTITIES_TABLE_BACKENDS = "t_Backends";
        private const string REPORT_DETECTED_ENTITIES_TABLE_BUSINESS_TRANSACTIONS = "t_BusinessTransactions";
        private const string REPORT_DETECTED_ENTITIES_TABLE_SERVICE_ENDPOINTS = "t_ServiceEndpoints";
        private const string REPORT_DETECTED_ENTITIES_TABLE_ERRORS = "t_Errors";

        private const string REPORT_DETECTED_ENTITIES_PIVOT_TIERS = "p_Tiers";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_APPAGENT = "p_NodesTypeAppAgent";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_MACHINEAGENT = "p_NodesTypeMachineAgent";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_TYPE = "p_BackendsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_LOCATION = "p_BackendsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_TYPE = "p_BusinessTransactionsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_LOCATION_SHEET = "p_BusinessTransactionsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_TYPE = "p_ServiceEndpointsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_LOCATION = "p_ServiceEndpointsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_TYPE = "p_ErrorsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_LOCATION = "p_ErrorsLocation";

        private const int REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT = 6;

        #endregion

        #region Constants for All Entities Metrics Report generation Excel contents

        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_FULL = "4.Applications";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_HOURLY = "4.Applications.Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_FULL = "5.Tiers.Full";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_HOURLY = "5.Tiers.Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_FULL = "6.Nodes";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_HOURLY = "6.Nodes.Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_FULL = "7.Backends";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_HOURLY = "7.Backends.Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_FULL = "8.BTs";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_HOURLY = "8.BTs.Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_FULL = "9.SEPs";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_HOURLY = "9.SEPs.Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_FULL = "10.Errors";
        private const string REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_HOURLY = "10.Errors.Hourly";

        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_TOC = "t_TOC";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_PARAMETERS_TARGETS = "t_InputTargets";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_APPLICATIONS_FULL = "t_Applications_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_APPLICATIONS_HOURLY = "t_Applications_Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_TIERS_FULL = "t_Tiers_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_TIERS_HOURLY = "t_Tiers_Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_NODES_FULL = "t_Nodes_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_NODES_HOURLY = "t_Nodes_Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_BACKENDS_FULL = "t_Backends_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_BACKENDS_HOURLY = "t_Backends_Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_BUSINESS_TRANSACTIONS_FULL = "t_BusinessTransactions_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_BUSINESS_TRANSACTIONS_HOURLY = "t_BusinessTransactions_Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_SERVICE_ENDPOINTS_FULL = "t_ServiceEndpoints_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_SERVICE_ENDPOINTS_HOURLY = "t_ServiceEndpoints_Hourly";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_ERRORS_FULL = "t_Errors_Full";
        private const string REPORT_METRICS_ALL_ENTITIES_TABLE_ERRORS_HOURLY = "t_Errors_Hourly";

        private const int REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT = 4;

        #endregion

        #region Constants for Detected Events and Health Rule Violations contents

        private const string REPORT_DETECTED_EVENTS_SHEET_CONTROLLERS_LIST = "3.Controllers";

        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS = "4.Events";
        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT = "4.Events.Type";

        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR = "5.HR Violations";
        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR_PIVOT = "5.HR Violations.Type";

        private const string REPORT_DETECTED_EVENTS_TABLE_EVENTS = "t_Events";
        private const string REPORT_DETECTED_EVENTS_TABLE_EVENTS_HR = "t_HealthRuleViolationEvents";

        private const string REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TYPE = "p_EventsType";
        private const string REPORT_DETECTED_EVENTS_PIVOT_EVENTS_HR_TYPE = "p_HealthRuleViolationEventsType";

        private const int REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT = 6;

        #endregion

        #region Constants for Entity Metric Details Report generation Excel contents

        private const string REPORT_ENTITY_DETAILS_SHEET_CONTROLLERS_LIST = "3.Controllers";

        // Metric summaries full, hourly
        private const string REPORT_ENTITY_DETAILS_SHEET_SUMMARY = "4.Summary";
        // Raw metric data
        private const string REPORT_ENTITY_DETAILS_SHEET_METRICS = "5.Metric Detail";
        // Flowmap in grid view
        private const string REPORT_ENTITY_DETAILS_SHEET_ACTIVITYGRID = "6.Activity Flow";
        // Event data 
        private const string REPORT_ENTITY_DETAILS_SHEET_EVENTS = "7.Events";
        private const string REPORT_ENTITY_DETAILS_SHEET_EVENTS_PIVOT = "7.Events.Type";
        // Health rule 
        private const string REPORT_ENTITY_DETAILS_SHEET_EVENTS_HR = "8.HR Violations";
        private const string REPORT_ENTITY_DETAILS_SHEET_EVENTS_HR_PIVOT = "8.HR Violations.Type";
        // Graphs, snapshots and events all lined up in timeline
        private const string REPORT_ENTITY_DETAILS_SHEET_HOURLY_TIMELINE = "9.Timeline";

        private const string REPORT_ENTITY_DETAILS_TABLE_TOC = "t_TOC";
        private const string REPORT_ENTITY_DETAILS_TABLE_CONTROLLERS = "t_Controllers";

        // Full and hourly metric data
        private const string REPORT_ENTITY_DETAILS_TABLE_ENTITY_FULL = "t_M_Summary_{0}_{1}_Full";
        private const string REPORT_ENTITY_DETAILS_TABLE_ENTITY_HOURLY = "t_M_Summary_{0}_{1}_Hourly";
        // Description tables from metric.summary.csv
        private const string REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_DESCRIPTION = "t_M_Descr_{0}_{1}_{2}";
        // Metric data tables from metric.values.csv
        private const string REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_VALUES = "t_M_Values_{0}_{1}_{2}";
        // Grid data
        private const string REPORT_ENTITY_DETAILS_ACTIVITY_GRID = "t_Activity_{0}_{1}";
        // Events from events.csv and hrviolationevents.csv
        private const string REPORT_ENTITY_DETAILS_TABLE_EVENTS = "t_Events_{0}_{1}";
        private const string REPORT_ENTITY_DETAILS_TABLE_EVENTS_HEALTH_RULE_VIOLATIONS = "t_EventsHealthRuleViolations_{0}_{1}";

        // Hourly graph data
        private const string REPORT_ENTITY_DETAILS_GRAPH = "g_M_{0}_{1}_{2:yyyyMMddHHss}";

        private const string REPORT_ENTITY_DETAIL_PIVOT_EVENTS = "p_Events";
        private const string REPORT_ENTITY_DETAIL_PIVOT_EVENTS_HR = "p_Events_HR";


        private const int REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_ENTITY_DETAILS_GRAPHS_SHEET_START_TABLE_AT = 15;
        private const int REPORT_ENTITY_DETAILS_PIVOT_SHEET_START_PIVOT_AT = 6;

        #endregion

        #region Constants for Deeplinks

        private const string DEEPLINK_CONTROLLER = @"{0}/controller/#/location=AD_HOME_OVERVIEW&timeRange={1}";
        private const string DEEPLINK_APPLICATION = @"{0}/controller/#/location=APP_DASHBOARD&timeRange={2}&application={1}&dashboardMode=force";
        private const string DEEPLINK_TIER = @"{0}/controller/#/location=APP_COMPONENT_MANAGER&timeRange={3}&application={1}&component={2}&dashboardMode=force";
        private const string DEEPLINK_NODE = @"{0}/controller/#/location=APP_NODE_MANAGER&timeRange={3}&application={1}&node={2}&dashboardMode=force";
        private const string DEEPLINK_BACKEND = @"{0}/controller/#/location=APP_BACKEND_DASHBOARD&timeRange={3}&application={1}&backendDashboard={2}&dashboardMode=force";
        private const string DEEPLINK_BUSINESS_TRANSACTION = @"{0}/controller/#/location=APP_BT_DETAIL&timeRange={3}&application={1}&businessTransaction={2}&dashboardMode=force";
        private const string DEEPLINK_SERVICE_ENDPOINT = @"{0}/controller/#/location=APP_SERVICE_ENDPOINT_DETAIL&timeRange={4}&application={1}&component={2}&serviceEndpoint={3}";
        private const string DEEPLINK_ERROR = @"{0}/controller/#/location=APP_ERROR_DASHBOARD&timeRange={3}&application={1}&error={2}";
        private const string DEEPLINK_APPLICATION_MOBILE = @"{0}/controller/#/location=EUM_MOBILE_MAIN_DASHBOARD&timeRange={3}&application={1}&mobileApp={2}";
        private const string DEEPLINK_HEALTH_RULE = @"{0}/controller/#/location=ALERT_RESPOND_HEALTH_RULES&timeRange={3}&application={1}";
        private const string DEEPLINK_INCIDENT = @"{0}/controller/#/location=APP_INCIDENT_DETAIL_MODAL&timeRange={4}&application={1}&incident={2}&incidentTime={3}";

        private const string DEEPLINK_METRIC = @"{0}/controller/#/location=METRIC_BROWSER&timeRange={3}&application={1}&metrics={2}";
        private const string DEEPLINK_TIMERANGE_LAST_15_MINUTES = "last_15_minutes.BEFORE_NOW.-1.-1.15";
        private const string DEEPLINK_TIMERANGE_BETWEEN_TIMES = "Custom_Time_Range.BETWEEN_TIMES.{0}.{1}.{2}";
        private const string DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID = "APPLICATION.{0}.{1}";
        private const string DEEPLINK_METRIC_TIER_TARGET_METRIC_ID = "APPLICATION_COMPONENT.{0}.{1}";
        private const string DEEPLINK_METRIC_NODE_TARGET_METRIC_ID = "APPLICATION_COMPONENT_NODE.{0}.{1}";

        #endregion

        #region Constants for parallelization of processes

        private const int METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 10;
        private const int METRIC_EXTRACT_NUMBER_OF_THREADS = 5;

        private const int FLOWMAP_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 20;
        private const int FLOWMAP_EXTRACT_NUMBER_OF_THREADS = 5;

        private const int SNAPSHOTS_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 50;
        private const int SNAPSHOTS_EXTRACT_NUMBER_OF_THREADS = 10;

        private const int METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 10;
        private const int METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS = 10;

        #endregion

        #region Job steps router

        internal static void startOrContinueJob(ProgramOptions programOptions)
        {
            JobConfiguration jobConfiguration = FileIOHelper.readJobConfigurationFromFile(programOptions.OutputJobFilePath);
            if (jobConfiguration == null)
            {
                loggerConsole.Error("Unable to load job input file {0}", programOptions.InputJobFilePath);

                return;
            }

            List<JobStatus> jobSteps = new List<JobStatus>
            {
                // Get data
                JobStatus.ExtractControllerApplicationsAndEntities,
                JobStatus.ExtractControllerAndApplicationConfiguration,
                JobStatus.ExtractApplicationAndEntityMetrics,
                JobStatus.ExtractApplicationAndEntityFlowmaps,
                JobStatus.ExtractSnapshots,
                JobStatus.ExtractEvents,

                // Process data
                JobStatus.IndexControllersApplicationsAndEntities,
                JobStatus.IndexControllerAndApplicationConfiguration,
                JobStatus.IndexApplicationAndEntityMetrics,
                JobStatus.IndexApplicationAndEntityFlowmaps,
                JobStatus.IndexSnapshots,
                JobStatus.IndexEvents,

                // Report data
                JobStatus.ReportControlerApplicationsAndEntities,
                JobStatus.ReportControllerAndApplicationConfiguration,
                JobStatus.ReportEventsAndHealthRuleViolations,
                JobStatus.ReportApplicationAndEntityMetrics,
                JobStatus.ReportIndividualApplicationAndEntityDetails,
                JobStatus.ReportSnapshots,
                //JobStatus.ReportFlameGraphs,

                // Done 
                JobStatus.Done,

                JobStatus.Error
            };
            LinkedList<JobStatus> jobStepsLinked = new LinkedList<JobStatus>(jobSteps);

            #region Output diagnostic parameters to log

            loggerConsole.Info("Job status {0}({0:d})", jobConfiguration.Status);
            logger.Info("Job status {0}({0:d})", jobConfiguration.Status);
            logger.Info("Job input: TimeRange.From='{0:o}', TimeRange.To='{1:o}', ExpandedTimeRange.From='{2:o}', ExpandedTimeRange.To='{3:o}', Time ranges='{4}', Flowmaps='{5}', Metrics='{6}', Snapshots='{7}', Configuration='{8}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To, jobConfiguration.Input.HourlyTimeRanges.Count, jobConfiguration.Input.Flowmaps, jobConfiguration.Input.Metrics, jobConfiguration.Input.Snapshots, jobConfiguration.Input.Configuration);

            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
            {
                logger.Info("Expanded time ranges: From='{0:o}', To='{1:o}'", jobTimeRange.From, jobTimeRange.To);
            }

            #endregion

            // Run the step and move to next until things are done
            while (jobConfiguration.Status != JobStatus.Done && jobConfiguration.Status != JobStatus.Error)
            {
                switch (jobConfiguration.Status)
                {
                    case JobStatus.ExtractControllerApplicationsAndEntities:
                        if (stepExtractControllerApplicationsAndEntities(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                        }
                        else
                        {
                            jobConfiguration.Status = JobStatus.Error;
                        }
                        break;

                    case JobStatus.ExtractControllerAndApplicationConfiguration:
                        if (jobConfiguration.Input.Configuration == true)
                        {
                            if (stepExtractControllerAndApplicationConfiguration(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping export of configuration");
                        }
                        break;

                    case JobStatus.ExtractApplicationAndEntityMetrics:
                        if (jobConfiguration.Input.Metrics == true)
                        {
                            if (stepExtractApplicationAndEntityMetrics(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping export of entity metrics");
                        }
                        break;

                    case JobStatus.ExtractApplicationAndEntityFlowmaps:
                        if (jobConfiguration.Input.Flowmaps == true)
                        {
                            if (stepExtractApplicationAndEntityFlowmaps(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping export of entity flowmaps");
                        }
                        break;

                    case JobStatus.ExtractSnapshots:
                        if (jobConfiguration.Input.Snapshots == true)
                        {
                            if (stepExtractSnapshots(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping export of snapshots");
                        }
                        break;

                    case JobStatus.ExtractEvents:
                        if (jobConfiguration.Input.Events == true)
                        {
                            if (stepExtractEvents(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping export of events");
                        }
                        break;

                    case JobStatus.IndexControllersApplicationsAndEntities:
                        if (stepIndexControllersApplicationsAndEntities(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                        }
                        else
                        {
                            jobConfiguration.Status = JobStatus.Error;
                        }
                        break;

                    case JobStatus.IndexControllerAndApplicationConfiguration:
                        if (jobConfiguration.Input.Configuration == true)
                        {
                            if (stepIndexControllerAndApplicationConfiguration(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping index of configuration");
                        }
                        break;

                    case JobStatus.IndexApplicationAndEntityMetrics:
                        if (jobConfiguration.Input.Metrics == true)
                        {
                            if (stepIndexApplicationAndEntityMetrics(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping index of entity metrics");
                        }
                        break;

                    case JobStatus.IndexApplicationAndEntityFlowmaps:
                        if (jobConfiguration.Input.Flowmaps == true)
                        {
                            if (stepIndexApplicationAndEntityFlowmaps(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping index of entity flowmaps");
                        }
                        break;

                    case JobStatus.IndexSnapshots:
                        if (jobConfiguration.Input.Snapshots == true)
                        {
                            if (stepIndexSnapshots(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping index of snapshots");
                        }
                        break;

                    case JobStatus.IndexEvents:
                        if (jobConfiguration.Input.Events == true)
                        {
                            if (stepIndexEvents(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping index of events");
                        }
                        break;

                    case JobStatus.ReportControlerApplicationsAndEntities:
                        if (stepReportControlerApplicationsAndEntities(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                        }
                        break;

                    case JobStatus.ReportControllerAndApplicationConfiguration:
                        if (jobConfiguration.Input.Configuration == true)
                        {
                            if (stepReportControllerAndApplicationConfiguration(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping report of configuration");
                        }
                        break;

                    case JobStatus.ReportEventsAndHealthRuleViolations:
                        if (jobConfiguration.Input.Events == true)
                        {
                            if (stepReportEventsAndHealthRuleViolations(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping report of events and health rule violations");
                        }
                        break;

                    case JobStatus.ReportApplicationAndEntityMetrics:
                        if (jobConfiguration.Input.Metrics == true)
                        {
                            if (stepReportApplicationAndEntityMetrics(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping report of entity metrics");
                        }
                        break;

                    case JobStatus.ReportIndividualApplicationAndEntityDetails:
                        if (jobConfiguration.Input.Metrics == true ||
                            jobConfiguration.Input.Events == true ||
                            jobConfiguration.Input.Flowmaps == true ||
                            jobConfiguration.Input.Snapshots == true)
                        {
                            if (stepReportIndividualApplicationAndEntityDetails(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping report of entity metric details");
                        }
                        break;

                    case JobStatus.ReportSnapshots:
                        if (jobConfiguration.Input.Snapshots == true)
                        {
                            if (stepReportSnapshots(programOptions, jobConfiguration, jobConfiguration.Status) == true)
                            {
                                jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                            }
                            else
                            {
                                jobConfiguration.Status = JobStatus.Error;
                            }
                        }
                        else
                        {
                            jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;

                            loggerConsole.Debug("Skipping report of snapshots");
                        }
                        break;

                    default:
                        jobConfiguration.Status = JobStatus.Error;
                        break;
                }

                // Save the resulting JSON file to the job target folder
                if (FileIOHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                    return;
                }
            }
        }

        #endregion

        #region Extract steps

        private static bool stepExtractControllerApplicationsAndEntities(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);

                        // Entity files
                        string applicationsFilePath = Path.Combine(controllerFolderPath, EXTRACT_ENTITY_APPLICATIONS_FILE_NAME);
                        string applicationFilePath = Path.Combine(applicationFolderPath, EXTRACT_ENTITY_APPLICATION_FILE_NAME);
                        string tiersFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_TIERS_FILE_NAME);
                        string nodesFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_NODES_FILE_NAME);
                        string backendsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndPointsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string serviceEndPointsAllFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_ALL_FILE_NAME);
                        string errorsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_ERRORS_FILE_NAME);

                        #endregion

                        #region Applications

                        // Only do it once per controller, if processing multiple applications
                        if (File.Exists(applicationsFilePath) != true)
                        {
                            loggerConsole.Info("List of Applications");

                            string applicationsJSON = controllerApi.GetListOfApplications();
                            if (applicationsJSON != String.Empty) FileIOHelper.saveFileToFolder(applicationsJSON, applicationsFilePath);
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("This Application");

                        string applicationJSON = controllerApi.GetSingleApplication(jobTarget.ApplicationID);
                        if (applicationJSON != String.Empty) FileIOHelper.saveFileToFolder(applicationJSON, applicationFilePath);

                        #endregion

                        #region Tiers

                        loggerConsole.Info("List of Tiers");

                        string tiersJSON = controllerApi.GetListOfTiers(jobTarget.ApplicationID);
                        if (tiersJSON != String.Empty) FileIOHelper.saveFileToFolder(tiersJSON, tiersFilePath);

                        #endregion

                        #region Nodes

                        loggerConsole.Info("List of Nodes");

                        string nodesJSON = controllerApi.GetListOfNodes(jobTarget.ApplicationID);
                        if (nodesJSON != String.Empty) FileIOHelper.saveFileToFolder(nodesJSON, nodesFilePath);

                        #endregion

                        #region Backends

                        loggerConsole.Info("List of Backends");

                        string backendsJSON = controllerApi.GetListOfBackends(jobTarget.ApplicationID);
                        if (backendsJSON != String.Empty) FileIOHelper.saveFileToFolder(backendsJSON, backendsFilePath);

                        #endregion

                        #region Business Transactions

                        loggerConsole.Info("List of Business Transactions");

                        string businessTransactionsJSON = controllerApi.GetListOfBusinessTransactions(jobTarget.ApplicationID);
                        if (businessTransactionsJSON != String.Empty) FileIOHelper.saveFileToFolder(businessTransactionsJSON, businessTransactionsFilePath);

                        #endregion

                        #region Service Endpoints

                        loggerConsole.Info("List of Service Endpoints");

                        string serviceEndPointsJSON = controllerApi.GetListOfServiceEndpoints(jobTarget.ApplicationID);
                        if (serviceEndPointsJSON != String.Empty) FileIOHelper.saveFileToFolder(serviceEndPointsJSON, serviceEndPointsFilePath);

                        controllerApi.PrivateApiLogin();
                        serviceEndPointsJSON = controllerApi.GetListOfServiceEndpointsWithDetail(jobTarget.ApplicationID);
                        if (serviceEndPointsJSON != String.Empty) FileIOHelper.saveFileToFolder(serviceEndPointsJSON, serviceEndPointsAllFilePath);

                        #endregion

                        #region Errors

                        loggerConsole.Info("List of Errors");

                        string errorsJSON = controllerApi.GetListOfErrors(jobTarget.ApplicationID);
                        if (errorsJSON != String.Empty) FileIOHelper.saveFileToFolder(errorsJSON, errorsFilePath);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepExtractControllerAndApplicationConfiguration(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string configFolderPath = Path.Combine(applicationFolderPath, CONFIGURATION_FOLDER_NAME);

                        // Entity files
                        string applicationsFilePath = Path.Combine(controllerFolderPath, EXTRACT_ENTITY_APPLICATIONS_FILE_NAME);
                        string applicationConfigFilePath = Path.Combine(configFolderPath, EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME);
                        string controllerSettingsFilePath = Path.Combine(controllerFolderPath, EXTRACT_CONFIGURATION_CONTROLLER_FILE_NAME);

                        #endregion

                        #region Controller

                        if (File.Exists(controllerSettingsFilePath) != true)
                        {
                            loggerConsole.Info("Controller Settings");

                            string controllerSettingsJSON = controllerApi.GetControllerConfiguration();
                            if (controllerSettingsJSON != String.Empty) FileIOHelper.saveFileToFolder(controllerSettingsJSON, controllerSettingsFilePath);
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("Application Configuration");

                        // Application configuration
                        string applicationConfigXml = controllerApi.GetApplicationConfiguration(jobTarget.ApplicationID);
                        if (applicationConfigXml != String.Empty) FileIOHelper.saveFileToFolder(applicationConfigXml, applicationConfigFilePath);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepExtractApplicationAndEntityMetrics(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);

                        // Entity files
                        string tiersFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_TIERS_FILE_NAME);
                        string nodesFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_NODES_FILE_NAME);
                        string backendsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndPointsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string errorsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_ERRORS_FILE_NAME);

                        #endregion

                        #region Application

                        // Application
                        loggerConsole.Info("Extract Metrics for Application ({0} entities * {1} time ranges * {2} metrics)", 1, jobConfiguration.Input.HourlyTimeRanges.Count + 1, 5);

                        extractMetricsApplication(jobConfiguration, jobTarget, controllerApi, metricsFolderPath);

                        #endregion

                        #region Tiers

                        List<AppDRESTTier> tiersList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                        if (tiersList != null)
                        {
                            loggerConsole.Info("Extract Metrics for Tiers ({0} entities * {1} time ranges * {2} metrics)", tiersList.Count, jobConfiguration.Input.HourlyTimeRanges.Count + 1, 5);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var tiersListChunks = tiersList.BreakListIntoChunks(METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTTier>, int>(
                                    tiersListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (tiersListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractMetricsTiers(jobConfiguration, jobTarget, controllerApi, tiersListChunk, metricsFolderPath, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractMetricsTiers(jobConfiguration, jobTarget, controllerApi, tiersList, metricsFolderPath, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Nodes

                        List<AppDRESTNode> nodesList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTNode>(nodesFilePath);
                        if (nodesList != null)
                        {
                            loggerConsole.Info("Extract Metrics for Nodes ({0} entities * {1} time ranges * {2} metrics)", nodesList.Count, jobConfiguration.Input.HourlyTimeRanges.Count + 1, 5);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var nodesListChunks = nodesList.BreakListIntoChunks(METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTNode>, int>(
                                    nodesListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (nodesListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractMetricsNodes(jobConfiguration, jobTarget, controllerApi, nodesListChunk, metricsFolderPath, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractMetricsNodes(jobConfiguration, jobTarget, controllerApi, nodesList, metricsFolderPath, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Backends

                        List<AppDRESTBackend> backendsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBackend>(backendsFilePath);
                        if (backendsList != null)
                        {
                            loggerConsole.Info("Extract Metrics for Backends ({0} entities * {1} time ranges * {2} metrics)", backendsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 3);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var backendsListChunks = backendsList.BreakListIntoChunks(METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTBackend>, int>(
                                    backendsListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (backendsListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractMetricsBackends(jobConfiguration, jobTarget, controllerApi, backendsListChunk, metricsFolderPath, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractMetricsBackends(jobConfiguration, jobTarget, controllerApi, backendsList, metricsFolderPath, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Business Transactions

                        List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);
                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Extract Metrics for Business Transactions ({0} entities * {1} time ranges * {2} metrics)", businessTransactionsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 3);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var businessTransactionsListChunks = businessTransactionsList.BreakListIntoChunks(METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTBusinessTransaction>, int>(
                                    businessTransactionsListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (businessTransactionsListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractMetricsBusinessTransactions(jobConfiguration, jobTarget, controllerApi, businessTransactionsListChunk, metricsFolderPath, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractMetricsBusinessTransactions(jobConfiguration, jobTarget, controllerApi, businessTransactionsList, metricsFolderPath, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Service Endpoints

                        List<AppDRESTMetric> serviceEndpointsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(serviceEndPointsFilePath);
                        if (serviceEndpointsList != null)
                        {
                            loggerConsole.Info("Extract Metrics for Service Endpoints ({0} entities * {1} time ranges * {2} metrics)", serviceEndpointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 3);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var serviceEndpointsListChunks = serviceEndpointsList.BreakListIntoChunks(METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTMetric>, int>(
                                    serviceEndpointsListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (serviceEndpointsListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractMetricsServiceEndpoints(jobConfiguration, jobTarget, controllerApi, serviceEndpointsListChunk, tiersList, metricsFolderPath, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractMetricsServiceEndpoints(jobConfiguration, jobTarget, controllerApi, serviceEndpointsList, tiersList, metricsFolderPath, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Errors

                        List<AppDRESTMetric> errorsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(errorsFilePath);
                        if (errorsList != null)
                        {
                            loggerConsole.Info("Extract Metrics for Errors ({0} entities * {1} time ranges * {2} metrics)", errorsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 1);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var errorsListChunks = errorsList.BreakListIntoChunks(METRIC_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTMetric>, int>(
                                    errorsListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (errorsListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractMetricsErrors(jobConfiguration, jobTarget, controllerApi, errorsListChunk, tiersList, metricsFolderPath, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractMetricsErrors(jobConfiguration, jobTarget, controllerApi, errorsList, tiersList, metricsFolderPath, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepExtractApplicationAndEntityFlowmaps(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);

                        // Entity files
                        string tiersFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_TIERS_FILE_NAME);
                        string nodesFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_NODES_FILE_NAME);
                        string backendsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndPointsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);

                        #endregion

                        // Login into private API
                        controllerApi.PrivateApiLogin();

                        #region Prepare time range

                        long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
                        long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                        #endregion

                        #region Application

                        loggerConsole.Info("Extract Flowmap for Application");

                        extractFlowmapsApplication(jobConfiguration, jobTarget, controllerApi, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes);

                        #endregion

                        #region Tiers

                        List<AppDRESTTier> tiersList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                        if (tiersList != null)
                        {
                            loggerConsole.Info("Extract Flowmaps for Tiers ({0} entities)", tiersList.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var tiersListChunks = tiersList.BreakListIntoChunks(FLOWMAP_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTTier>, int>(
                                    tiersListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (tiersListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractFlowmapsTiers(jobConfiguration, jobTarget, controllerApi, tiersListChunk, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractFlowmapsTiers(jobConfiguration, jobTarget, controllerApi, tiersList, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Nodes

                        List<AppDRESTNode> nodesList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTNode>(nodesFilePath);
                        if (nodesList != null)
                        {
                            loggerConsole.Info("Extract Flowmaps for Nodes ({0} entities)", nodesList.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var nodesListChunks = nodesList.BreakListIntoChunks(FLOWMAP_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTNode>, int>(
                                    nodesListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (nodesListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractFlowmapsNodes(jobConfiguration, jobTarget, controllerApi, nodesListChunk, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractFlowmapsNodes(jobConfiguration, jobTarget, controllerApi, nodesList, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Backends

                        List<AppDRESTBackend> backendsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBackend>(backendsFilePath);
                        if (backendsList != null)
                        {
                            loggerConsole.Info("Extract Flowmaps for Backends ({0} entities)", backendsList.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var backendsListChunks = backendsList.BreakListIntoChunks(FLOWMAP_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTBackend>, int>(
                                    backendsListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (backendsListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractFlowmapsBackends(jobConfiguration, jobTarget, controllerApi, backendsListChunk, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = extractFlowmapsBackends(jobConfiguration, jobTarget, controllerApi, backendsList, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Business Transactions

                        List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);
                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Extract Flowmaps for Business Transactions ({0} entities)", businessTransactionsList.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var businessTransactionsListChunks = businessTransactionsList.BreakListIntoChunks(FLOWMAP_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTBusinessTransaction>, int>(
                                    businessTransactionsListChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (businessTransactionsListChunk, loop, subtotal) =>
                                    {
                                        subtotal += extractFlowmapsBusinessTransactions(jobConfiguration, jobTarget, controllerApi, businessTransactionsListChunk, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            {
                                j = extractFlowmapsBusinessTransactions(jobConfiguration, jobTarget, controllerApi, businessTransactionsList, metricsFolderPath, fromTimeUnix, toTimeUnix, differenceInMinutes, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepExtractSnapshots(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string snapshotsFolderPath = Path.Combine(applicationFolderPath, SNAPSHOTS_FOLDER_NAME);

                        // Entity files
                        string tiersFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_TIERS_FILE_NAME);
                        string businessTransactionsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);

                        #endregion

                        #region List of Snapshots in time ranges

                        // Login into private API
                        controllerApi.PrivateApiLogin();

                        loggerConsole.Info("Extract List of Snapshots ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                        // Get list of snapshots in each time range
                        int totalSnapshotsFound = 0;
                        foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                        {
                            loggerConsole.Info("Extract List of Snapshots from {0:o} to {1:o}", jobTimeRange.From, jobTimeRange.To);

                            string snapshotsFilePath = Path.Combine(snapshotsFolderPath, String.Format(EXTRACT_SNAPSHOTS_FILE_NAME, jobTimeRange.From, jobTimeRange.To));

                            int differenceInMinutes = (int)(jobTimeRange.To - jobTimeRange.From).TotalMinutes;

                            if (File.Exists(snapshotsFilePath) == false)
                            {
                                JArray listOfSnapshots = new JArray();

                                // Extract snapshot list
                                long serverCursorId = 0;
                                string serverCursorIdName = "rsdScrollId";
                                do
                                {
                                    string snapshotsJSON = controllerApi.GetListOfSnapshots(jobTarget.ApplicationID, jobTimeRange.From, jobTimeRange.To, differenceInMinutes, SNAPSHOTS_QUERY_PAGE_SIZE, serverCursorIdName, serverCursorId);

                                    if (snapshotsJSON == String.Empty)
                                    {
                                        // No snapshots in this page, exit
                                        serverCursorId = 0;
                                    }
                                    else
                                    {
                                        Console.Write(".");

                                        // Load snapshots
                                        JObject snapshotsParsed = JObject.Parse(snapshotsJSON);
                                        JArray snapshots = (JArray)snapshotsParsed["requestSegmentDataListItems"];
                                        foreach (JObject snapshot in snapshots)
                                        {
                                            listOfSnapshots.Add(snapshot);
                                        }

                                        // If there are more snapshots on the server, the server cursor would be non-0 
                                        object serverCursorIdObj = snapshotsParsed["serverCursor"]["rsdScrollId"];
                                        if (serverCursorIdObj == null)
                                        {
                                            // Sometimes - >4.3.3? the value of scroll is in scrollId, not rsdScrollId
                                            serverCursorIdObj = snapshotsParsed["serverCursor"]["scrollId"];
                                            if (serverCursorIdObj != null)
                                            {
                                                // And the name of the cursor changes too
                                                serverCursorIdName = "scrollId";
                                            }
                                            else
                                            {
                                                serverCursorId = 0;
                                            }
                                        }
                                        if (serverCursorIdObj != null)
                                        {
                                            serverCursorId = -1;
                                            Int64.TryParse(serverCursorIdObj.ToString(), out serverCursorId);
                                        }

                                        logger.Info("Retrieved snapshots from Controller {0}, Application {1}, From {2:o}, To {3:o}', number of snapshots {4}, continuation CursorId {5}", jobTarget.Controller, jobTarget.Application, jobTimeRange.From, jobTimeRange.To, snapshots.Count, serverCursorId);

                                        Console.Write("+{0}", listOfSnapshots.Count);
                                    }
                                }
                                while (serverCursorId > 0);

                                Console.WriteLine();

                                FileIOHelper.writeJArrayToFile(listOfSnapshots, snapshotsFilePath);

                                totalSnapshotsFound = totalSnapshotsFound + listOfSnapshots.Count;

                                logger.Info("{0} snapshots from {1:o} to {2:o}", listOfSnapshots.Count, jobTimeRange.From, jobTimeRange.To);
                                loggerConsole.Info("{0} snapshots from {1:o} to {2:o}", listOfSnapshots.Count, jobTimeRange.From, jobTimeRange.To);
                            }
                        }

                        logger.Info("{0} snapshots in all time ranges", totalSnapshotsFound);
                        loggerConsole.Info("{0} snapshots in all time ranges", totalSnapshotsFound);

                        #endregion

                        #region Individual Snapshots

                        // Extract individual snapshots
                        loggerConsole.Info("Extract Individual Snapshots");

                        // Load lookups for Tiers and Business Transactions
                        List<AppDRESTTier> tiersList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                        List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);

                        if (tiersList != null && businessTransactionsList != null)
                        {
                            // Process each hour at a time
                            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                            {
                                string snapshotsFilePath = Path.Combine(snapshotsFolderPath, String.Format(EXTRACT_SNAPSHOTS_FILE_NAME, jobTimeRange.From, jobTimeRange.To));
                                JArray listOfSnapshotsInHour = FileIOHelper.loadJArrayFromFile(snapshotsFilePath);
                                if (listOfSnapshotsInHour != null && listOfSnapshotsInHour.Count > 0)
                                {
                                    loggerConsole.Info("Extract Snapshots {0:o} to {1:o} ({2} snapshots)", jobTimeRange.From, jobTimeRange.To, listOfSnapshotsInHour.Count);

                                    int j = 0;

                                    if (programOptions.ProcessSequentially == false)
                                    {
                                        var listOfSnapshotsInHourChunks = listOfSnapshotsInHour.BreakListIntoChunks(SNAPSHOTS_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                        Parallel.ForEach<List<JToken>, int>(
                                            listOfSnapshotsInHourChunks,
                                            new ParallelOptions { MaxDegreeOfParallelism = SNAPSHOTS_EXTRACT_NUMBER_OF_THREADS },
                                            () => 0,
                                            (listOfSnapshotsInHourChunk, loop, subtotal) =>
                                            {
                                                subtotal += extractSnapshots(jobConfiguration, jobTarget, controllerApi, listOfSnapshotsInHourChunk, tiersList, businessTransactionsList, snapshotsFolderPath, false);
                                                return subtotal;
                                            },
                                            (finalResult) =>
                                            {
                                                j = Interlocked.Add(ref j, finalResult);
                                                Console.Write("[{0}].", j);
                                            }
                                        );
                                    }
                                    else
                                    {
                                        j = extractSnapshots(jobConfiguration, jobTarget, controllerApi, listOfSnapshotsInHour.ToList<JToken>(), tiersList, businessTransactionsList, snapshotsFolderPath, true);
                                    }

                                    loggerConsole.Info("{0} snapshots", j);
                                }
                            }
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepExtractEvents(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string eventsFolderPath = Path.Combine(applicationFolderPath, EVENTS_FOLDER_NAME);

                        #endregion

                        #region Health Rule violations

                        loggerConsole.Info("Extract List of Health Rule Violations ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                        JArray listOfHealthRuleViolations = new JArray();
                        int totalHealthRuleViolationsFound = 0;
                        foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                        {
                            long fromTimeUnix = convertToUnixTimestamp(jobTimeRange.From);
                            long toTimeUnix = convertToUnixTimestamp(jobTimeRange.To);

                            string healthRuleViolationsJSON = controllerApi.GetHealthRuleViolations(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                            if (healthRuleViolationsJSON != String.Empty)
                            {
                                Console.Write(".");

                                // Load health rule violations
                                JArray healthRuleViolationsInHour = JArray.Parse(healthRuleViolationsJSON);
                                foreach (JObject healthRuleViolation in healthRuleViolationsInHour)
                                {
                                    listOfHealthRuleViolations.Add(healthRuleViolation);
                                }
                                totalHealthRuleViolationsFound = totalHealthRuleViolationsFound + healthRuleViolationsInHour.Count;
                                Console.Write("+{0}", healthRuleViolationsInHour.Count);
                            }
                        }

                        Console.WriteLine();

                        if (listOfHealthRuleViolations.Count > 0)
                        {
                            string healthRuleViolationsFilePath = Path.Combine(
                                eventsFolderPath, 
                                String.Format(HEALTH_RULE_VIOLATIONS_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                            FileIOHelper.writeJArrayToFile(listOfHealthRuleViolations, healthRuleViolationsFilePath);

                            logger.Info("{0} health rule violations from {1:o} to {2:o}", listOfHealthRuleViolations.Count, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);
                            loggerConsole.Info("{0} health rule violations from {1:o} to {2:o}", listOfHealthRuleViolations.Count, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);
                        }

                        #endregion

                        #region Events

                        foreach (string eventType in eventTypes)
                        {
                            loggerConsole.Info("Extract {0} events ({1} time ranges)", eventType, jobConfiguration.Input.HourlyTimeRanges.Count);

                            JArray listOfEvents = new JArray();
                            int totalEventsFound = 0;
                            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                            {
                                long fromTimeUnix = convertToUnixTimestamp(jobTimeRange.From);
                                long toTimeUnix = convertToUnixTimestamp(jobTimeRange.To);

                                string eventsJSON = controllerApi.GetEvents(jobTarget.ApplicationID, eventType, fromTimeUnix, toTimeUnix);
                                if (eventsJSON != String.Empty)
                                {
                                    Console.Write(".");

                                    // Load health rule violations
                                    JArray eventsInHour = JArray.Parse(eventsJSON);
                                    foreach (JObject interestingEvent in eventsInHour)
                                    {
                                        listOfEvents.Add(interestingEvent);
                                    }
                                    totalEventsFound = totalEventsFound + eventsInHour.Count;
                                    Console.Write("+{0}", eventsInHour.Count);
                                }
                            }

                            Console.WriteLine();

                            if (listOfEvents.Count > 0)
                            {
                                string eventsFilePath = Path.Combine(
                                    eventsFolderPath, 
                                    String.Format(EVENTS_FILE_NAME, eventType, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                                FileIOHelper.writeJArrayToFile(listOfEvents, eventsFilePath);

                                logger.Info("{0} events from {1:o} to {2:o}", listOfEvents.Count, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);
                                loggerConsole.Info("{0} events from {1:o} to {2:o}", listOfEvents.Count, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);
                            }
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        #endregion

        #region Indexing steps

        private static bool stepIndexControllersApplicationsAndEntities(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string configFolderPath = Path.Combine(applicationFolderPath, CONFIGURATION_FOLDER_NAME);

                        // Entity files
                        string applicationsFilePath = Path.Combine(controllerFolderPath, EXTRACT_ENTITY_APPLICATIONS_FILE_NAME);
                        string applicationConfigFilePath = Path.Combine(configFolderPath, EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME);
                        string tiersFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_TIERS_FILE_NAME);
                        string nodesFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_NODES_FILE_NAME);
                        string backendsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndPointsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string serviceEndPointsAllFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_ALL_FILE_NAME);
                        string errorsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_ERRORS_FILE_NAME);

                        // Report files
                        string controllersReportFilePath = Path.Combine(programOptions.OutputJobFolderPath, CONVERT_ENTITY_CONTROLLERS_FILE_NAME);
                        string controllerReportFilePath = Path.Combine(controllerFolderPath, CONVERT_ENTITY_CONTROLLER_FILE_NAME);
                        string applicationsReportFilePath = Path.Combine(controllerFolderPath, CONVERT_ENTITY_APPLICATIONS_FILE_NAME);
                        string applicationReportFilePath = Path.Combine(applicationFolderPath, CONVERT_ENTITY_APPLICATION_FILE_NAME);
                        string tiersReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_TIERS_FILE_NAME);
                        string nodesReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_NODES_FILE_NAME);
                        string backendsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndpointsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string errorsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_ERRORS_FILE_NAME);

                        #endregion

                        #region Controller

                        loggerConsole.Info("Index List of Controllers");

                        // Create this row 
                        EntityController controllerRow = new EntityController();
                        controllerRow.Controller = jobTarget.Controller;
                        controllerRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, controllerRow.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        controllerRow.UserName = jobTarget.UserName;

                        // Lookup number of applications
                        // Load JSON file from the file system in case we are continuing the step after stopping
                        List<AppDRESTApplication> applicationsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTApplication>(applicationsFilePath);
                        if (applicationsList != null)
                        {
                            controllerRow.NumApps = applicationsList.Count;
                        }

                        // Lookup version
                        // Load the configuration.xml from the child to parse the version
                        XmlDocument configXml = FileIOHelper.loadXmlDocumentFromFile(applicationConfigFilePath);
                        if (configXml != null)
                        {
                            string controllerVersion = configXml.SelectSingleNode("application").Attributes["controller-version"].Value;
                            // The version is in 
                            // <application controller-version="004-002-005-001">
                            string[] controllerVersionArray = controllerVersion.Split('-');
                            int[] controllerVersionArrayNum = new int[controllerVersionArray.Length];
                            for (int j = 0; j < controllerVersionArray.Length; j++)
                            {
                                controllerVersionArrayNum[j] = Convert.ToInt32(controllerVersionArray[j]);
                            }
                            controllerVersion = String.Join(".", controllerVersionArrayNum);
                            controllerRow.Version = controllerVersion;
                        }
                        else
                        {
                            controllerRow.Version = "Did not extract configuration data";
                        }

                        // Output single controller report CSV
                        List<EntityController> controllerRows = new List<EntityController>(1);
                        controllerRows.Add(controllerRow);
                        if (File.Exists(controllerReportFilePath) == false)
                        {
                            FileIOHelper.writeListToCSVFile(controllerRows, new ControllerEntityReportMap(), controllerReportFilePath);
                        }

                        // Now append this controller to the list of all controllers
                        List<EntityController> controllersRows = FileIOHelper.readListFromCSVFile<EntityController>(controllersReportFilePath, new ControllerEntityReportMap());
                        if (controllersRows == null || controllersRows.Count == 0)
                        {
                            // First time, let's output these rows
                            controllersRows = controllerRows;
                        }
                        else
                        {
                            EntityController controllerRowExisting = controllersRows.Where(c => c.Controller == controllerRow.Controller).FirstOrDefault();
                            if (controllerRowExisting == null)
                            {
                                controllersRows.Add(controllerRow);
                            }
                        }
                        controllersRows = controllersRows.OrderBy(o => o.Controller).ToList();
                        FileIOHelper.writeListToCSVFile(controllersRows, new ControllerEntityReportMap(), controllersReportFilePath);

                        #endregion

                        #region Nodes

                        List<AppDRESTNode> nodesList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTNode>(nodesFilePath);
                        if (nodesList != null)
                        {
                            loggerConsole.Info("Index List of Nodes ({0} entities)", nodesList.Count);

                            List<EntityNode> nodesRows = new List<EntityNode>(nodesList.Count);

                            foreach (AppDRESTNode node in nodesList)
                            {
                                EntityNode nodeRow = new EntityNode();
                                nodeRow.NodeID = node.id;
                                nodeRow.AgentPresent = node.appAgentPresent;
                                nodeRow.AgentType = node.agentType;
                                nodeRow.AgentVersion = node.appAgentVersion;
                                nodeRow.ApplicationName = jobTarget.Application;
                                nodeRow.ApplicationID = jobTarget.ApplicationID;
                                nodeRow.Controller = jobTarget.Controller;
                                nodeRow.MachineAgentPresent = node.machineAgentPresent;
                                nodeRow.MachineAgentVersion = node.machineAgentVersion;
                                nodeRow.MachineID = node.machineId;
                                nodeRow.MachineName = node.machineName;
                                nodeRow.MachineOSType = node.machineOSType;
                                nodeRow.NodeName = node.name;
                                nodeRow.TierID = node.tierId;
                                nodeRow.TierName = node.tierName;
                                nodeRow.MachineType = node.type;
                                if (nodeRow.AgentVersion != String.Empty)
                                {
                                    // Java agent looks like that
                                    //Server Agent v4.2.3.2 GA #12153 r13c5eb6a7acbfea4d6da465a3ae47412715e26fa 59-4.2.3.next-build
                                    //Server Agent v3.7.16.0 GA #2014-02-26_21-19-08 raf61d5f54753290c983f95173e74e6865f6ad123 130-3.7.16
                                    //Server Agent v4.2.7.1 GA #13005 rc04adaef4741dbb8f2e7c206bdb2a6614046798a 11-4.2.7.next-analytics
                                    //Server Agent v4.0.6.0 GA #2015-05-11_20-44-33 r7cb8945756a0779766bf1b4c32e49a96da7b8cfe 10-4.0.6.next
                                    //Server Agent v3.8.3.0 GA #2014-06-06_17-06-05 r34b2744775df248f79ffb2da2b4515b1f629aeb5 7-3.8.3.next
                                    //Server Agent v3.9.3.0 GA #2014-09-23_22-14-15 r05918cd8a4a8a63504a34f0f1c85511e207049b3 20-3.9.3.next
                                    //Server Agent v4.1.7.1 GA #9949 ra4a2721d52322207b626e8d4c88855c846741b3d 18-4.1.7.next-build
                                    //Server Agent v3.7.11.1 GA #2013-10-23_17-07-44 r41149afdb8ce39025051c25382b1cf77e2a7fed0 21
                                    //Server Agent v4.1.8.5 GA #10236 r8eca32e4695e8f6a5902d34a66bfc12da1e12241 45-4.1.8.next-controller

                                    // Apache agent looks like this
                                    // Proxy v4.2.5.1 GA SHA-1:.ad6c804882f518b3350f422489866ea2008cd664 #13146 35-4.2.5.next-build

                                    Regex regexVersion = new Regex(@"(?i).*v(\d*\.\d*\.\d*\.\d*).*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(nodeRow.AgentVersion);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            nodeRow.AgentVersionRaw = nodeRow.AgentVersion;
                                            nodeRow.AgentVersion = match.Groups[1].Value;
                                        }
                                    }
                                }
                                if (nodeRow.MachineAgentVersion != String.Empty)
                                {
                                    // Machine agent looks like that 
                                    //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:26:01
                                    //Machine Agent v3.7.16.0 GA Build Date 2014 - 02 - 26 21:20:29
                                    //Machine Agent v4.2.3.2 GA Build Date 2016 - 07 - 11 10:17:54
                                    //Machine Agent v4.0.6.0 GA Build Date 2015 - 05 - 11 20:56:44
                                    //Machine Agent v3.8.3.0 GA Build Date 2014 - 06 - 06 17:09:13
                                    //Machine Agent v4.1.7.1 GA Build Date 2015 - 11 - 24 20:49:24

                                    Regex regexVersion = new Regex(@"(?i).*Machine Agent.*v(\d*\.\d*\.\d*\.\d*).*", RegexOptions.IgnoreCase);
                                    Match match = regexVersion.Match(nodeRow.MachineAgentVersion);
                                    if (match != null)
                                    {
                                        if (match.Groups.Count > 1)
                                        {
                                            nodeRow.MachineAgentVersionRaw = nodeRow.MachineAgentVersion;
                                            nodeRow.MachineAgentVersion = match.Groups[1].Value;
                                        }
                                    }
                                }

                                updateEntityWithDeeplinks(nodeRow);

                                nodesRows.Add(nodeRow);
                            }

                            // Sort them
                            nodesRows = nodesRows.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ToList();

                            updateEntitiesWithReportDetailLinksNodes(programOptions, jobConfiguration, jobTarget, nodesRows);

                            FileIOHelper.writeListToCSVFile(nodesRows, new NodeEntityReportMap(), nodesReportFilePath);
                        }

                        #endregion

                        #region Backends

                        List<AppDRESTBackend> backendsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBackend>(backendsFilePath);
                        List<AppDRESTTier> tiersList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);

                        if (backendsList != null)
                        {
                            loggerConsole.Info("Index List of Backends ({0} entities)", backendsList.Count);

                            List<EntityBackend> backendsRows = new List<EntityBackend>(backendsList.Count);

                            foreach (AppDRESTBackend backend in backendsList)
                            {
                                EntityBackend backendRow = new EntityBackend();
                                backendRow.ApplicationName = jobTarget.Application;
                                backendRow.ApplicationID = jobTarget.ApplicationID;
                                backendRow.BackendID = backend.id;
                                backendRow.BackendName = backend.name;
                                backendRow.BackendType = backend.exitPointType;
                                backendRow.Controller = jobTarget.Controller;
                                backendRow.NodeID = backend.applicationComponentNodeId;
                                if (backendRow.NodeID > 0)
                                {
                                    // Look it up
                                    AppDRESTNode node = nodesList.Where<AppDRESTNode>(n => n.id == backendRow.NodeID).FirstOrDefault();
                                    if (node != null) backendRow.NodeName = node.name;
                                }
                                backendRow.NumProps = backend.properties.Count;
                                if (backend.properties.Count >= 1)
                                {
                                    backendRow.Prop1Name = backend.properties[0].name;
                                    backendRow.Prop1Value = backend.properties[0].value;
                                }
                                if (backend.properties.Count >= 2)
                                {
                                    backendRow.Prop2Name = backend.properties[1].name;
                                    backendRow.Prop2Value = backend.properties[1].value;
                                }
                                if (backend.properties.Count >= 3)
                                {
                                    backendRow.Prop3Name = backend.properties[2].name;
                                    backendRow.Prop3Value = backend.properties[2].value;
                                }
                                if (backend.properties.Count >= 4)
                                {
                                    backendRow.Prop4Name = backend.properties[3].name;
                                    backendRow.Prop4Value = backend.properties[3].value;
                                }
                                if (backend.properties.Count >= 5)
                                {
                                    backendRow.Prop5Name = backend.properties[4].name;
                                    backendRow.Prop5Value = backend.properties[4].value;
                                }
                                backendRow.TierID = backend.tierId;
                                if (backendRow.TierID > 0)
                                {
                                    // Look it up
                                    AppDRESTTier tier = tiersList.Where<AppDRESTTier>(t => t.id == backendRow.TierID).FirstOrDefault();
                                    if (tier != null) backendRow.TierName = tier.name;
                                }

                                updateEntityWithDeeplinks(backendRow);

                                backendsRows.Add(backendRow);
                            }

                            // Sort them
                            backendsRows = backendsRows.OrderBy(o => o.BackendType).ThenBy(o => o.BackendName).ToList();

                            updateEntitiesWithReportDetailLinksBackends(programOptions, jobConfiguration, jobTarget, backendsRows);

                            FileIOHelper.writeListToCSVFile(backendsRows, new BackendEntityReportMap(), backendsReportFilePath);
                        }

                        #endregion

                        #region Business Transactions

                        List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);
                        if (businessTransactionsList != null)
                        {
                            loggerConsole.Info("Index List of Business Transactions ({0} entities)", businessTransactionsList.Count);

                            List<EntityBusinessTransaction> businessTransactionRows = new List<EntityBusinessTransaction>(businessTransactionsList.Count);

                            foreach (AppDRESTBusinessTransaction businessTransaction in businessTransactionsList)
                            {
                                EntityBusinessTransaction businessTransactionRow = new EntityBusinessTransaction();
                                businessTransactionRow.ApplicationID = jobTarget.ApplicationID;
                                businessTransactionRow.ApplicationName = jobTarget.Application;
                                businessTransactionRow.BTID = businessTransaction.id;
                                businessTransactionRow.BTName = businessTransaction.name;
                                if (businessTransactionRow.BTName == "_APPDYNAMICS_DEFAULT_TX_")
                                {
                                    businessTransactionRow.BTType = "OVERFLOW";
                                }
                                else
                                {
                                    businessTransactionRow.BTType = businessTransaction.entryPointType;
                                }
                                businessTransactionRow.Controller = jobTarget.Controller;
                                businessTransactionRow.TierID = businessTransaction.tierId;
                                businessTransactionRow.TierName = businessTransaction.tierName;

                                updateEntityWithDeeplinks(businessTransactionRow);

                                businessTransactionRows.Add(businessTransactionRow);
                            }

                            // Sort them
                            businessTransactionRows = businessTransactionRows.OrderBy(o => o.TierName).ThenBy(o => o.BTName).ToList();

                            updateEntitiesWithReportDetailLinksBusinessTransactions(programOptions, jobConfiguration, jobTarget, businessTransactionRows);

                            FileIOHelper.writeListToCSVFile(businessTransactionRows, new BusinessTransactionEntityReportMap(), businessTransactionsReportFilePath);
                        }

                        #endregion

                        #region Service Endpoints

                        List<AppDRESTMetric> serviceEndpointsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(serviceEndPointsFilePath);
                        List<EntityServiceEndpoint> serviceEndpointsRows = null;
                        if (serviceEndpointsList != null)
                        {
                            loggerConsole.Info("Index List of Service Endpoints ({0} entities)", tiersList.Count);

                            serviceEndpointsRows = new List<EntityServiceEndpoint>(serviceEndpointsList.Count);

                            JObject serviceEndpointsAll = FileIOHelper.loadJObjectFromFile(serviceEndPointsAllFilePath);
                            JArray serviceEndpointsDetail = null;
                            if (serviceEndpointsAll != null)
                            {
                                serviceEndpointsDetail = (JArray)serviceEndpointsAll["serviceEndpointListEntries"];
                            }

                            foreach (AppDRESTMetric serviceEndpoint in serviceEndpointsList)
                            {
                                EntityServiceEndpoint serviceEndpointRow = new EntityServiceEndpoint();
                                serviceEndpointRow.ApplicationID = jobTarget.ApplicationID;
                                serviceEndpointRow.ApplicationName = jobTarget.Application;
                                serviceEndpointRow.Controller = jobTarget.Controller;

                                // metricName
                                // BTM|Application Diagnostic Data|SEP:4855|Calls per Minute
                                //                                     ^^^^
                                //                                     ID
                                serviceEndpointRow.SEPID = Convert.ToInt32(serviceEndpoint.metricName.Split('|')[2].Split(':')[1]);

                                // metricPath
                                // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest|Calls per Minute
                                //                                      ^^^^^^^^^^^^^^^^^^^^^^
                                //                                      SEP Name
                                serviceEndpointRow.SEPName = serviceEndpoint.metricPath.Split('|')[2];

                                serviceEndpointRow.TierName = serviceEndpoint.metricPath.Split('|')[1];
                                if (tiersList != null)
                                {
                                    // metricPath
                                    // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest|Calls per Minute
                                    //                   ^^^^^^^^^^^^^^^^^^
                                    //                   Tier
                                    AppDRESTTier tierForThisEntity = tiersList.Where(tier => tier.name == serviceEndpointRow.TierName).FirstOrDefault();
                                    if (tierForThisEntity != null)
                                    {
                                        serviceEndpointRow.TierID = tierForThisEntity.id;
                                    }
                                }

                                JObject serviceEndpointDetail = (JObject)serviceEndpointsDetail.Where(sep => (int)sep["id"] == serviceEndpointRow.SEPID).FirstOrDefault();
                                if (serviceEndpointDetail != null)
                                {
                                    serviceEndpointRow.SEPType = serviceEndpointDetail["type"].ToString();
                                }

                                updateEntityWithDeeplinks(serviceEndpointRow);

                                serviceEndpointsRows.Add(serviceEndpointRow);
                            }

                            // Sort them
                            serviceEndpointsRows = serviceEndpointsRows.OrderBy(o => o.TierName).ThenBy(o => o.SEPName).ToList();

                            updateEntitiesWithReportDetailLinksServiceEndpoints(programOptions, jobConfiguration, jobTarget, serviceEndpointsRows);

                            FileIOHelper.writeListToCSVFile(serviceEndpointsRows, new ServiceEndpointEntityReportMap(), serviceEndpointsReportFilePath);
                        }

                        #endregion

                        #region Errors

                        List<AppDRESTMetric> errorsList = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(errorsFilePath);
                        List<EntityError> errorRows = null;
                        if (errorsList != null)
                        {
                            loggerConsole.Info("Index List of Errors ({0} entities)", errorsList.Count);

                            errorRows = new List<EntityError>(errorsList.Count);

                            foreach (AppDRESTMetric error in errorsList)
                            {
                                EntityError errorRow = new EntityError();
                                errorRow.ApplicationID = jobTarget.ApplicationID;
                                errorRow.ApplicationName = jobTarget.Application;
                                errorRow.Controller = jobTarget.Controller;

                                // metricName
                                // BTM|Application Diagnostic Data|Error:11626|Errors per Minute
                                //                                       ^^^^^
                                //                                       ID
                                errorRow.ErrorID = Convert.ToInt32(error.metricName.Split('|')[2].Split(':')[1]);

                                // metricPath
                                // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                                //                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                                //                           Error Name
                                errorRow.ErrorName = error.metricPath.Split('|')[2];

                                errorRow.ErrorType = "Error";
                                // Do some analysis of the error type based on their name
                                if (errorRow.ErrorName.IndexOf("exception", 0, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    errorRow.ErrorType = "Exception";
                                }
                                // For things like 
                                // CommunicationException : IOException : CommunicationException : SocketException
                                // ServletException : RollbackException : DatabaseException : SQLNestedException : NoSuchElementException
                                string[] errorTokens = errorRow.ErrorName.Split(':');
                                for (int j = 0; j < errorTokens.Length; j++)
                                {
                                    errorTokens[j] = errorTokens[j].Trim();
                                }
                                if (errorTokens.Length >= 1)
                                {
                                    errorRow.ErrorLevel1 = errorTokens[0];
                                }
                                if (errorTokens.Length >= 2)
                                {
                                    errorRow.ErrorLevel2 = errorTokens[1];
                                }
                                if (errorTokens.Length >= 3)
                                {
                                    errorRow.ErrorLevel3 = errorTokens[2];
                                }
                                if (errorTokens.Length >= 4)
                                {
                                    errorRow.ErrorLevel4 = errorTokens[3];
                                }
                                if (errorTokens.Length >= 5)
                                {
                                    errorRow.ErrorLevel5 = errorTokens[4];
                                }
                                errorRow.ErrorDepth = errorTokens.Length;

                                // Check if last thing is a 3 digit number, then cast it and see what comes out
                                if (errorTokens[errorTokens.Length - 1].Length == 3)
                                {
                                    int httpCode = -1;
                                    if (Int32.TryParse(errorTokens[errorTokens.Length - 1], out httpCode) == true)
                                    {
                                        // Hmm, likely to be a HTTP code
                                        errorRow.ErrorType = "HTTP";
                                        errorRow.HttpCode = httpCode;
                                    }
                                }

                                errorRow.TierName = error.metricPath.Split('|')[1];
                                if (tiersList != null)
                                {
                                    // metricPath
                                    // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                                    //        ^^^^^^^^^^^^^^^^^^
                                    //        Tier
                                    AppDRESTTier tierForThisEntity = tiersList.Where(tier => tier.name == errorRow.TierName).FirstOrDefault();
                                    if (tierForThisEntity != null)
                                    {
                                        errorRow.TierID = tierForThisEntity.id;
                                    }
                                }

                                updateEntityWithDeeplinks(errorRow);

                                errorRows.Add(errorRow);
                            }

                            // Sort them
                            errorRows = errorRows.OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ToList();

                            updateEntitiesWithReportDetailLinksErrors(programOptions, jobConfiguration, jobTarget, errorRows);

                            FileIOHelper.writeListToCSVFile(errorRows, new ErrorEntityReportMap(), errorsReportFilePath);
                        }

                        #endregion

                        #region Tiers

                        if (tiersList != null)
                        {
                            loggerConsole.Info("Index List of Tiers ({0} entities)", tiersList.Count);

                            List<EntityTier> tiersRows = new List<EntityTier>(tiersList.Count);

                            foreach (AppDRESTTier tier in tiersList)
                            {
                                EntityTier tierRow = new EntityTier();
                                tierRow.AgentType = tier.agentType;
                                tierRow.ApplicationID = jobTarget.ApplicationID;
                                tierRow.ApplicationName = jobTarget.Application;
                                tierRow.Controller = jobTarget.Controller;
                                tierRow.TierID = tier.id;
                                tierRow.TierName = tier.name;
                                tierRow.TierType = tier.type;
                                tierRow.NumNodes = tier.numberOfNodes;
                                if (businessTransactionsList != null)
                                {
                                    tierRow.NumBTs = businessTransactionsList.Where<AppDRESTBusinessTransaction>(b => b.tierId == tierRow.TierID).Count();
                                }
                                if (serviceEndpointsRows != null)
                                {
                                    tierRow.NumSEPs = serviceEndpointsRows.Where<EntityServiceEndpoint>(s => s.TierID == tierRow.TierID).Count();
                                }
                                if (errorRows != null)
                                {
                                    tierRow.NumErrors = errorRows.Where<EntityError>(s => s.TierID == tierRow.TierID).Count();
                                }

                                updateEntityWithDeeplinks(tierRow);

                                tiersRows.Add(tierRow);
                            }

                            // Sort them
                            tiersRows = tiersRows.OrderBy(o => o.TierName).ToList();

                            updateEntitiesWithReportDetailLinksTiers(programOptions, jobConfiguration, jobTarget, tiersRows);

                            FileIOHelper.writeListToCSVFile(tiersRows, new TierEntityReportMap(), tiersReportFilePath);
                        }

                        #endregion

                        #region Application

                        if (applicationsList != null)
                        {
                            loggerConsole.Info("Index List of Applications");

                            List<EntityApplication> applicationsRows = FileIOHelper.readListFromCSVFile<EntityApplication>(applicationsReportFilePath, new ApplicationEntityReportMap());

                            if (applicationsRows == null || applicationsRows.Count == 0)
                            {
                                // First time, let's output these rows
                                applicationsRows = new List<EntityApplication>(applicationsList.Count);
                                foreach (AppDRESTApplication application in applicationsList)
                                {
                                    EntityApplication applicationsRow = new EntityApplication();
                                    applicationsRow.ApplicationName = application.name;
                                    applicationsRow.ApplicationID = application.id;
                                    applicationsRow.Controller = jobTarget.Controller;

                                    updateEntityWithDeeplinks(applicationsRow);

                                    applicationsRows.Add(applicationsRow);
                                }
                            }

                            // Update counts of entities for this application row
                            EntityApplication applicationRow = applicationsRows.Where(a => a.ApplicationID == jobTarget.ApplicationID).FirstOrDefault();
                            if (applicationRow != null)
                            {
                                if (tiersList != null) applicationRow.NumTiers = tiersList.Count;
                                if (nodesList != null) applicationRow.NumNodes = nodesList.Count;
                                if (backendsList != null) applicationRow.NumBackends = backendsList.Count;
                                if (businessTransactionsList != null) applicationRow.NumBTs = businessTransactionsList.Count;
                                if (serviceEndpointsRows != null) applicationRow.NumSEPs = serviceEndpointsRows.Count;
                                if (errorRows != null) applicationRow.NumErrors = errorRows.Count;

                                List<EntityApplication> applicationRows = new List<EntityApplication>(1);
                                applicationRows.Add(applicationRow);

                                // Write just this row for this application
                                FileIOHelper.writeListToCSVFile(applicationRows, new ApplicationEntityReportMap(), applicationReportFilePath);
                            }

                            // Sort them
                            applicationsRows = applicationsRows.OrderBy(o => o.Controller).ThenBy(o => o.ApplicationName).ToList();

                            updateEntitiesWithReportDetailLinksApplication(programOptions, jobConfiguration, jobTarget, applicationsRows);

                            FileIOHelper.writeListToCSVFile(applicationsRows, new ApplicationEntityReportMap(), applicationsReportFilePath);
                        }

                        #endregion

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepIndexControllerAndApplicationConfiguration(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);
            return true;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        // Business Transaction Rules

                        // Backend Rules configuration/backend-match-point-configurations

                        // Data Collectors

                        // Agent properties

                        // Health Rules configuration/health-rules

                        // Error Detection configuration/error-configuration

                        loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepIndexApplicationAndEntityMetrics(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);

                        // Report files
                        string applicationReportFilePath = Path.Combine(applicationFolderPath, CONVERT_ENTITY_APPLICATION_FILE_NAME);
                        string tiersReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_TIERS_FILE_NAME);
                        string nodesReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_NODES_FILE_NAME);
                        string backendsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndpointsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string errorsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_ERRORS_FILE_NAME);

                        // Metric files
                        string metricsEntityFolderPath = String.Empty;
                        string metricsDataFilePath = String.Empty;
                        string entityFullRangeReportFilePath = String.Empty;
                        string entityHourlyRangeReportFilePath = String.Empty;
                        string entitiesFullRangeReportFilePath = String.Empty;
                        string entitiesHourlyRangeReportFilePath = String.Empty;

                        #endregion

                        #region Application

                        List<EntityApplication> applicationRows = FileIOHelper.readListFromCSVFile<EntityApplication>(applicationReportFilePath, new ApplicationEntityReportMap());
                        if (applicationRows != null && applicationRows.Count > 0)
                        {
                            loggerConsole.Info("Index Metrics for Application ({0} entities)", applicationRows.Count);

                            List<EntityApplication> applicationFullRows = new List<EntityApplication>(1);
                            List<EntityApplication> applicationHourlyRows = new List<EntityApplication>(jobConfiguration.Input.HourlyTimeRanges.Count);

                            metricsEntityFolderPath = Path.Combine(
                                metricsFolderPath,
                                APPLICATION_TYPE_SHORT);

                            #region Full Range

                            EntityApplication applicationRow = applicationRows[0].Clone();
                            if (fillFullRangeMetricEntityRow(applicationRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                            {
                                applicationFullRows.Add(applicationRow);
                            }

                            #endregion

                            #region Hourly ranges

                            for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                            {
                                JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                applicationRow = applicationRows[0].Clone();
                                if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(applicationRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                {
                                    applicationHourlyRows.Add(applicationRow);
                                }
                            }

                            #endregion

                            updateEntitiesWithReportDetailLinksApplication(programOptions, jobConfiguration, jobTarget, applicationFullRows);
                            updateEntitiesWithReportDetailLinksApplication(programOptions, jobConfiguration, jobTarget, applicationHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(applicationFullRows, new ApplicationMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(applicationHourlyRows, new ApplicationMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion

                        #region Tier

                        List<EntityTier> tiersRows = FileIOHelper.readListFromCSVFile<EntityTier>(tiersReportFilePath, new TierEntityReportMap());
                        if (tiersRows != null)
                        {
                            loggerConsole.Info("Index Metrics for Tiers ({0} entities)", tiersRows.Count);

                            List<EntityTier> tiersFullRows = new List<EntityTier>(tiersRows.Count);
                            List<EntityTier> tiersHourlyRows = new List<EntityTier>(tiersRows.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                            int j = 0;

                            foreach (EntityTier tierRowOriginal in tiersRows)
                            {
                                List<EntityTier> tierFullRows = new List<EntityTier>(1);
                                List<EntityTier> tierHourlyRows = new List<EntityTier>(jobConfiguration.Input.HourlyTimeRanges.Count);

                                metricsEntityFolderPath = Path.Combine(
                                    metricsFolderPath,
                                    TIERS_TYPE_SHORT,
                                    getShortenedEntityNameForFileSystem(tierRowOriginal.TierName, tierRowOriginal.TierID));

                                #region Full Range

                                EntityTier tierRow = tierRowOriginal.Clone();
                                if (fillFullRangeMetricEntityRow(tierRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                                {
                                    tiersFullRows.Add(tierRow);
                                    tierFullRows.Add(tierRow);
                                }

                                #endregion

                                #region Hourly ranges

                                for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                                {
                                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                    tierRow = tierRowOriginal.Clone();
                                    if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(tierRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                    {
                                        tiersHourlyRows.Add(tierRow);
                                        tierHourlyRows.Add(tierRow);
                                    }
                                }

                                #endregion

                                entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(tierFullRows, new TierMetricReportMap(), entityFullRangeReportFilePath);

                                entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(tierHourlyRows, new TierMetricReportMap(), entityHourlyRangeReportFilePath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);

                            // Sort them
                            tiersHourlyRows = tiersHourlyRows.OrderBy(o => o.TierName).ThenBy(o => o.From).ToList();

                            updateEntitiesWithReportDetailLinksTiers(programOptions, jobConfiguration, jobTarget, tiersFullRows);
                            updateEntitiesWithReportDetailLinksTiers(programOptions, jobConfiguration, jobTarget, tiersHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsFolderPath, TIERS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(tiersFullRows, new TierMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsFolderPath, TIERS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(tiersHourlyRows, new TierMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion

                        #region Nodes

                        List<EntityNode> nodesRows = FileIOHelper.readListFromCSVFile<EntityNode>(nodesReportFilePath, new NodeEntityReportMap());
                        if (nodesRows != null)
                        {
                            loggerConsole.Info("Index Metrics for Nodes ({0} entities)", nodesRows.Count);

                            List<EntityNode> nodesFullRows = new List<EntityNode>(nodesRows.Count);
                            List<EntityNode> nodesHourlyRows = new List<EntityNode>(nodesRows.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                            int j = 0;

                            foreach (EntityNode nodeRowOriginal in nodesRows)
                            {
                                List<EntityNode> nodeFullRows = new List<EntityNode>(1);
                                List<EntityNode> nodeHourlyRows = new List<EntityNode>(jobConfiguration.Input.HourlyTimeRanges.Count);

                                metricsEntityFolderPath = Path.Combine(
                                    metricsFolderPath,
                                    NODES_TYPE_SHORT,
                                    getShortenedEntityNameForFileSystem(nodeRowOriginal.TierName, nodeRowOriginal.TierID),
                                    getShortenedEntityNameForFileSystem(nodeRowOriginal.NodeName, nodeRowOriginal.NodeID));

                                #region Full Range

                                EntityNode nodeRow = nodeRowOriginal.Clone();
                                if (fillFullRangeMetricEntityRow(nodeRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                                {
                                    nodesFullRows.Add(nodeRow);
                                    nodeFullRows.Add(nodeRow);
                                }

                                #endregion

                                #region Hourly ranges

                                for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                                {
                                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                    nodeRow = nodeRowOriginal.Clone();
                                    if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(nodeRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                    {
                                        nodesHourlyRows.Add(nodeRow);
                                        nodeHourlyRows.Add(nodeRow);
                                    }
                                }

                                #endregion

                                entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(nodeFullRows, new NodeMetricReportMap(), entityFullRangeReportFilePath);

                                entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(nodeHourlyRows, new NodeMetricReportMap(), entityHourlyRangeReportFilePath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);

                            // Sort them
                            nodesHourlyRows = nodesHourlyRows.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ThenBy(o => o.From).ToList();

                            updateEntitiesWithReportDetailLinksNodes(programOptions, jobConfiguration, jobTarget, nodesFullRows);
                            updateEntitiesWithReportDetailLinksNodes(programOptions, jobConfiguration, jobTarget, nodesHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsFolderPath, NODES_TYPE_SHORT, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(nodesFullRows, new NodeMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsFolderPath, NODES_TYPE_SHORT, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(nodesHourlyRows, new NodeMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion

                        #region Backends

                        List<EntityBackend> backendsRows = FileIOHelper.readListFromCSVFile<EntityBackend>(backendsReportFilePath, new BackendEntityReportMap());
                        if (backendsRows != null)
                        {
                            loggerConsole.Info("Index Metrics for Backends ({0} entities)", backendsRows.Count);

                            List<EntityBackend> backendsFullRows = new List<EntityBackend>(backendsRows.Count);
                            List<EntityBackend> backendsHourlyRows = new List<EntityBackend>(backendsRows.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                            int j = 0;

                            foreach (EntityBackend backendRowOriginal in backendsRows)
                            {
                                List<EntityBackend> backendFullRows = new List<EntityBackend>(1);
                                List<EntityBackend> backendHourlyRows = new List<EntityBackend>(jobConfiguration.Input.HourlyTimeRanges.Count);

                                metricsEntityFolderPath = Path.Combine(
                                    metricsFolderPath,
                                    BACKENDS_TYPE_SHORT,
                                    getShortenedEntityNameForFileSystem(backendRowOriginal.BackendName, backendRowOriginal.BackendID));

                                #region Full Range
                                
                                EntityBackend backendRow = backendRowOriginal.Clone();
                                if (fillFullRangeMetricEntityRow(backendRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                                {
                                    backendsFullRows.Add(backendRow);
                                    backendFullRows.Add(backendRow);
                                }

                                #endregion

                                #region Hourly ranges

                                for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                                {
                                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                    backendRow = backendRowOriginal.Clone();
                                    if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(backendRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                    {
                                        backendsHourlyRows.Add(backendRow);
                                        backendHourlyRows.Add(backendRow);
                                    }
                                }

                                #endregion

                                entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(backendFullRows, new BackendMetricReportMap(), entityFullRangeReportFilePath);

                                entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(backendHourlyRows, new BackendMetricReportMap(), entityHourlyRangeReportFilePath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);

                            // Sort them
                            backendsHourlyRows = backendsHourlyRows.OrderBy(o => o.BackendType).ThenBy(o => o.BackendName).ThenBy(o => o.From).ToList();

                            updateEntitiesWithReportDetailLinksBackends(programOptions, jobConfiguration, jobTarget, backendsFullRows);
                            updateEntitiesWithReportDetailLinksBackends(programOptions, jobConfiguration, jobTarget, backendsHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsFolderPath, BACKENDS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(backendsFullRows, new BackendMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsFolderPath, BACKENDS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(backendsHourlyRows, new BackendMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion

                        #region Business Transactions

                        List<EntityBusinessTransaction> businessTransactionsRows = FileIOHelper.readListFromCSVFile<EntityBusinessTransaction>(businessTransactionsReportFilePath, new BusinessTransactionEntityReportMap());
                        if (businessTransactionsRows != null)
                        {
                            loggerConsole.Info("Index Metrics for Business Transactions ({0} entities)", businessTransactionsRows.Count);

                            List<EntityBusinessTransaction> businessTransactionsFullRows = new List<EntityBusinessTransaction>(businessTransactionsRows.Count);
                            List<EntityBusinessTransaction> businessTransactionsHourlyRows = new List<EntityBusinessTransaction>(businessTransactionsRows.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                            int j = 0;

                            foreach (EntityBusinessTransaction businessTransactionRowOriginal in businessTransactionsRows)
                            {
                                List<EntityBusinessTransaction> businessTransactionFullRows = new List<EntityBusinessTransaction>(1);
                                List<EntityBusinessTransaction> businessTransactionHourlyRows = new List<EntityBusinessTransaction>(jobConfiguration.Input.HourlyTimeRanges.Count);

                                metricsEntityFolderPath = Path.Combine(
                                    metricsFolderPath,
                                    BUSINESS_TRANSACTIONS_TYPE_SHORT,
                                    getShortenedEntityNameForFileSystem(businessTransactionRowOriginal.TierName, businessTransactionRowOriginal.TierID),
                                    getShortenedEntityNameForFileSystem(businessTransactionRowOriginal.BTName, businessTransactionRowOriginal.BTID));

                                #region Full Range

                                EntityBusinessTransaction businessTransactionRow = businessTransactionRowOriginal.Clone();
                                if (fillFullRangeMetricEntityRow(businessTransactionRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                                {
                                    businessTransactionsFullRows.Add(businessTransactionRow);
                                    businessTransactionFullRows.Add(businessTransactionRow);
                                }

                                #endregion

                                #region Hourly ranges

                                for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                                {
                                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                    businessTransactionRow = businessTransactionRowOriginal.Clone();
                                    if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(businessTransactionRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                    {
                                        businessTransactionsHourlyRows.Add(businessTransactionRow);
                                        businessTransactionHourlyRows.Add(businessTransactionRow);
                                    }
                                }

                                #endregion

                                entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(businessTransactionFullRows, new BusinessTransactionMetricReportMap(), entityFullRangeReportFilePath);

                                entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(businessTransactionHourlyRows, new BusinessTransactionMetricReportMap(), entityHourlyRangeReportFilePath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);

                            // Sort them
                            businessTransactionsHourlyRows = businessTransactionsHourlyRows.OrderBy(o => o.TierName).ThenBy(o => o.BTName).ThenBy(o => o.From).ToList();

                            updateEntitiesWithReportDetailLinksBusinessTransactions(programOptions, jobConfiguration, jobTarget, businessTransactionsFullRows);
                            updateEntitiesWithReportDetailLinksBusinessTransactions(programOptions, jobConfiguration, jobTarget, businessTransactionsHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsFolderPath, BUSINESS_TRANSACTIONS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(businessTransactionsFullRows, new BusinessTransactionMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsFolderPath, BUSINESS_TRANSACTIONS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(businessTransactionsHourlyRows, new BusinessTransactionMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion

                        #region Service Endpoints

                        List<EntityServiceEndpoint> serviceEndpointsRows = FileIOHelper.readListFromCSVFile<EntityServiceEndpoint>(serviceEndpointsReportFilePath, new ServiceEndpointEntityReportMap());
                        if (serviceEndpointsRows != null)
                        {
                            loggerConsole.Info("Index Metrics for Service Endpoints ({0} entities)", serviceEndpointsRows.Count);

                            List<EntityServiceEndpoint> serviceEndpointsFullRows = new List<EntityServiceEndpoint>(serviceEndpointsRows.Count);
                            List<EntityServiceEndpoint> serviceEndpointsHourlyRows = new List<EntityServiceEndpoint>(serviceEndpointsRows.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                            int j = 0;

                            foreach (EntityServiceEndpoint serviceEndpointRowOriginal in serviceEndpointsRows)
                            {
                                List<EntityServiceEndpoint> serviceEndpointFullRows = new List<EntityServiceEndpoint>(1);
                                List<EntityServiceEndpoint> serviceEndpointHourlyRows = new List<EntityServiceEndpoint>(jobConfiguration.Input.HourlyTimeRanges.Count);

                                metricsEntityFolderPath = Path.Combine(
                                    metricsFolderPath,
                                    SERVICE_ENDPOINTS_TYPE_SHORT,
                                    getShortenedEntityNameForFileSystem(serviceEndpointRowOriginal.TierName, serviceEndpointRowOriginal.TierID),
                                    getShortenedEntityNameForFileSystem(serviceEndpointRowOriginal.SEPName, serviceEndpointRowOriginal.SEPID));

                                #region Full Range

                                EntityServiceEndpoint serviceEndpointRow = serviceEndpointRowOriginal.Clone();
                                if (fillFullRangeMetricEntityRow(serviceEndpointRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                                {
                                    serviceEndpointsFullRows.Add(serviceEndpointRow);
                                    serviceEndpointFullRows.Add(serviceEndpointRow);
                                }

                                #endregion

                                #region Hourly ranges

                                for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                                {
                                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                    serviceEndpointRow = serviceEndpointRowOriginal.Clone();
                                    if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(serviceEndpointRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                    {
                                        serviceEndpointsHourlyRows.Add(serviceEndpointRow);
                                        serviceEndpointHourlyRows.Add(serviceEndpointRow);
                                    }
                                }

                                #endregion

                                entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(serviceEndpointFullRows, new ServiceEndpointMetricReportMap(), entityFullRangeReportFilePath);

                                entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(serviceEndpointHourlyRows, new ServiceEndpointMetricReportMap(), entityHourlyRangeReportFilePath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);

                            // Sort them
                            serviceEndpointsHourlyRows = serviceEndpointsHourlyRows.OrderBy(o => o.TierName).ThenBy(o => o.SEPName).ThenBy(o => o.From).ToList();

                            updateEntitiesWithReportDetailLinksServiceEndpoints(programOptions, jobConfiguration, jobTarget, serviceEndpointsFullRows);
                            updateEntitiesWithReportDetailLinksServiceEndpoints(programOptions, jobConfiguration, jobTarget, serviceEndpointsHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsFolderPath, SERVICE_ENDPOINTS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(serviceEndpointsFullRows, new ServiceEndpointMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsFolderPath, SERVICE_ENDPOINTS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(serviceEndpointsHourlyRows, new ServiceEndpointMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion

                        #region Errors

                        List<EntityError> errorsRows = FileIOHelper.readListFromCSVFile<EntityError>(errorsReportFilePath, new ErrorEntityReportMap());
                        if (errorsRows != null)
                        {
                            loggerConsole.Info("Index Metrics for Errors ({0} entities)", errorsRows.Count);

                            List<EntityError> errorsFullRows = new List<EntityError>(errorsRows.Count);
                            List<EntityError> errorsHourlyRows = new List<EntityError>(errorsRows.Count * jobConfiguration.Input.HourlyTimeRanges.Count);

                            int j = 0;

                            foreach (EntityError errorRowOriginal in errorsRows)
                            {
                                List<EntityError> errorFullRows = new List<EntityError>(1);
                                List<EntityError> errorHourlyRows = new List<EntityError>(jobConfiguration.Input.HourlyTimeRanges.Count);

                                metricsEntityFolderPath = Path.Combine(
                                    metricsFolderPath,
                                    ERRORS_TYPE_SHORT,
                                    getShortenedEntityNameForFileSystem(errorRowOriginal.TierName, errorRowOriginal.TierID),
                                    getShortenedEntityNameForFileSystem(errorRowOriginal.ErrorName, errorRowOriginal.ErrorID));

                                #region Full Range

                                EntityError errorRow = errorRowOriginal.Clone();
                                if (fillFullRangeMetricEntityRow(errorRow, metricsEntityFolderPath, jobConfiguration.Input.ExpandedTimeRange) == true)
                                {
                                    errorsFullRows.Add(errorRow);
                                    errorFullRows.Add(errorRow);
                                }

                                #endregion

                                #region Hourly ranges

                                for (int k = 0; k < jobConfiguration.Input.HourlyTimeRanges.Count; k++)
                                {
                                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[k];

                                    errorRow = errorRowOriginal.Clone();
                                    if (fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(errorRow, metricsEntityFolderPath, jobTimeRange, k) == true)
                                    {
                                        errorsHourlyRows.Add(errorRow);
                                        errorHourlyRows.Add(errorRow);
                                    }
                                }

                                #endregion

                                entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(errorFullRows, new ErrorMetricReportMap(), entityFullRangeReportFilePath);

                                entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                                FileIOHelper.writeListToCSVFile(errorHourlyRows, new ErrorMetricReportMap(), entityHourlyRangeReportFilePath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);

                            // Sort them
                            errorsHourlyRows = errorsHourlyRows.OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ThenBy(o => o.From).ToList();

                            updateEntitiesWithReportDetailLinksErrors(programOptions, jobConfiguration, jobTarget, errorsFullRows);
                            updateEntitiesWithReportDetailLinksErrors(programOptions, jobConfiguration, jobTarget, errorsHourlyRows);

                            entityFullRangeReportFilePath = Path.Combine(metricsFolderPath, ERRORS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(errorsFullRows, new ErrorMetricReportMap(), entityFullRangeReportFilePath);

                            entityHourlyRangeReportFilePath = Path.Combine(metricsFolderPath, ERRORS_TYPE_SHORT, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                            FileIOHelper.writeListToCSVFile(errorsHourlyRows, new ErrorMetricReportMap(), entityHourlyRangeReportFilePath);
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepIndexApplicationAndEntityFlowmaps(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);

                        // Report files
                        string applicationReportFilePath = Path.Combine(applicationFolderPath, CONVERT_ENTITY_APPLICATION_FILE_NAME);
                        string tiersReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_TIERS_FILE_NAME);
                        string nodesReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_NODES_FILE_NAME);
                        string backendsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndpointsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string errorsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_ERRORS_FILE_NAME);

                        #endregion

                        #region Application

                        List<EntityApplication> applicationRows = FileIOHelper.readListFromCSVFile<EntityApplication>(applicationReportFilePath, new ApplicationEntityReportMap());
                        if (applicationRows != null && applicationRows.Count > 0)
                        {
                            loggerConsole.Info("Index Flowmap for Application");

                            convertFlowmapApplication(programOptions, jobConfiguration, jobTarget, applicationRows[0], metricsFolderPath);
                        }

                        #endregion

                        #region Tiers

                        List<EntityTier> tiersRows = FileIOHelper.readListFromCSVFile<EntityTier>(tiersReportFilePath, new TierEntityReportMap());
                        if (tiersRows != null)
                        {
                            loggerConsole.Info("Index Flowmap for Tiers ({0} entities)", tiersRows.Count);

                            int j = 0;

                            foreach (EntityTier tierRow in tiersRows)
                            {
                                convertFlowmapTier(programOptions, jobConfiguration, jobTarget, tierRow, metricsFolderPath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Nodes

                        List<EntityNode> nodesRows = FileIOHelper.readListFromCSVFile<EntityNode>(nodesReportFilePath, new NodeEntityReportMap());
                        if (nodesRows != null)
                        {
                            loggerConsole.Info("Index Flowmap for Nodes ({0} entities)", tiersRows.Count);

                            int j = 0;

                            foreach (EntityNode nodeRow in nodesRows)
                            {
                                convertFlowmapNode(programOptions, jobConfiguration, jobTarget, nodeRow, metricsFolderPath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Backends

                        List<EntityBackend> backendsRows = FileIOHelper.readListFromCSVFile<EntityBackend>(backendsReportFilePath, new BackendEntityReportMap());
                        if (backendsRows != null)
                        {
                            loggerConsole.Info("Index Flowmap for Backends ({0} entities)", backendsRows.Count);

                            int j = 0;

                            foreach (EntityBackend backendRow in backendsRows)
                            {
                                convertFlowmapBackend(programOptions, jobConfiguration, jobTarget, backendRow, metricsFolderPath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Business Transactions

                        List<EntityBusinessTransaction> businessTransactionsRows = FileIOHelper.readListFromCSVFile<EntityBusinessTransaction>(businessTransactionsReportFilePath, new BusinessTransactionEntityReportMap());
                        if (businessTransactionsRows != null)
                        {
                            loggerConsole.Info("Index Flowmap for Business Transactions ({0} entities)", businessTransactionsRows.Count);

                            int j = 0;

                            foreach (EntityBusinessTransaction businessTransactionRow in businessTransactionsRows)
                            {
                                convertFlowmapsBusinessTransaction(programOptions, jobConfiguration, jobTarget, businessTransactionRow, metricsFolderPath);

                                j++;
                                if (j % 10 == 0)
                                {
                                    Console.Write("[{0}].", j);
                                }
                            }
                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepIndexSnapshots(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);
            return true;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepIndexEvents(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string eventsFolderPath = Path.Combine(applicationFolderPath, EVENTS_FOLDER_NAME);

                        #endregion

                        #region Health Rule violations

                        loggerConsole.Info("Index health rule violations");

                        List<HealthRuleViolationEvent> healthRuleViolationRows = new List<HealthRuleViolationEvent>();

                        long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
                        long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        string healthRuleViolationsDataFilePath = Path.Combine(
                            eventsFolderPath,
                            String.Format(HEALTH_RULE_VIOLATIONS_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                        if (File.Exists(healthRuleViolationsDataFilePath))
                        {
                            JArray eventsInHour = FileIOHelper.loadJArrayFromFile(healthRuleViolationsDataFilePath);
                            if (eventsInHour != null)
                            {
                                foreach (JObject interestingEvent in eventsInHour)
                                {
                                    HealthRuleViolationEvent eventRow = new HealthRuleViolationEvent();
                                    eventRow.Controller = jobTarget.Controller;
                                    eventRow.ApplicationName = jobTarget.Application;
                                    eventRow.ApplicationID = jobTarget.ApplicationID;

                                    eventRow.EventID = (long)interestingEvent["id"];
                                    eventRow.FromUtc = convertFromUnixTimestamp((long)interestingEvent["startTimeInMillis"]);
                                    eventRow.From = eventRow.FromUtc.ToLocalTime();
                                    if ((long)interestingEvent["endTimeInMillis"] > 0)
                                    {
                                        eventRow.ToUtc = convertFromUnixTimestamp((long)interestingEvent["endTimeInMillis"]);
                                        eventRow.To = eventRow.FromUtc.ToLocalTime();
                                    }
                                    eventRow.Status = interestingEvent["incidentStatus"].ToString();
                                    eventRow.Severity = interestingEvent["severity"].ToString();
                                    //eventRow.EventLink = interestingEvent["deepLinkUrl"].ToString();
                                    eventRow.EventLink = String.Format(DEEPLINK_INCIDENT, eventRow.Controller, eventRow.ApplicationID, eventRow.EventID, interestingEvent["startTimeInMillis"], DEEPLINK_THIS_TIMERANGE); ;
                                    
                                    eventRow.Description = interestingEvent["description"].ToString();

                                    if (interestingEvent["triggeredEntityDefinition"].HasValues == true)
                                    {
                                        eventRow.HealthRuleID = (int)interestingEvent["triggeredEntityDefinition"]["entityId"];
                                        eventRow.HealthRuleName = interestingEvent["triggeredEntityDefinition"]["name"].ToString();
                                        eventRow.HealthRuleLink = String.Format(DEEPLINK_HEALTH_RULE, eventRow.Controller, eventRow.ApplicationID, eventRow.HealthRuleID, DEEPLINK_THIS_TIMERANGE);
                                    }

                                    if (interestingEvent["affectedEntityDefinition"].HasValues == true)
                                    {
                                        eventRow.EntityID = (int)interestingEvent["affectedEntityDefinition"]["entityId"];
                                        eventRow.EntityName = interestingEvent["affectedEntityDefinition"]["name"].ToString();

                                        string entityType = interestingEvent["affectedEntityDefinition"]["entityType"].ToString();
                                        if (entityTypeStringMapping.ContainsKey(entityType) == true)
                                        {
                                            eventRow.EntityType = entityTypeStringMapping[entityType];
                                        }
                                        else
                                        {
                                            eventRow.EntityType = entityType;
                                        }

                                        // Come up with links
                                        switch (entityType)
                                        {
                                            case ENTITY_TYPE_APPLICATION:
                                                eventRow.EntityLink = String.Format(DEEPLINK_APPLICATION, eventRow.Controller, eventRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_APPLICATION_MOBILE:
                                                eventRow.EntityLink = String.Format(DEEPLINK_APPLICATION_MOBILE, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_TIER:
                                                eventRow.EntityLink = String.Format(DEEPLINK_TIER, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_NODE:
                                                eventRow.EntityLink = String.Format(DEEPLINK_NODE, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_BUSINESS_TRANSACTION:
                                                eventRow.EntityLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_BACKEND:
                                                eventRow.EntityLink = String.Format(DEEPLINK_BACKEND, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            default:
                                                logger.Warn("Unknown entity type {0} in affectedEntityDefinition in health rule violations", entityType);
                                                break;
                                        }
                                    }

                                    eventRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, eventRow.Controller, DEEPLINK_THIS_TIMERANGE);
                                    eventRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, eventRow.Controller, eventRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                    healthRuleViolationRows.Add(eventRow);
                                }
                            }
                        }

                        loggerConsole.Info("{0} events", healthRuleViolationRows.Count);

                        // Sort them
                        healthRuleViolationRows = healthRuleViolationRows.OrderBy(o => o.HealthRuleName).ThenBy(o => o.From).ThenBy(o => o.Severity).ToList();

                        string healthRuleViolationEventsFilePath = Path.Combine(eventsFolderPath, CONVERT_HEALTH_RULE_EVENTS_FILE_NAME);
                        FileIOHelper.writeListToCSVFile<HealthRuleViolationEvent>(healthRuleViolationRows, new HealthRuleViolationEventReportMap(), healthRuleViolationEventsFilePath);

                        #endregion

                        #region Events

                        loggerConsole.Info("Index events");

                        List<Event> eventsRows = new List<Event>();
                        foreach (string eventType in eventTypes)
                        {
                            loggerConsole.Info("Type {0} events", eventType);

                            string eventsDataFilePath = Path.Combine(
                                eventsFolderPath, 
                                String.Format(EVENTS_FILE_NAME, eventType, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                            if (File.Exists(eventsDataFilePath))
                            {
                                JArray eventsInHour = FileIOHelper.loadJArrayFromFile(eventsDataFilePath);
                                if (eventsInHour != null)
                                {
                                    foreach (JObject interestingEvent in eventsInHour)
                                    {
                                        Event eventRow = new Event();
                                        eventRow.Controller = jobTarget.Controller;
                                        eventRow.ApplicationName = jobTarget.Application;
                                        eventRow.ApplicationID = jobTarget.ApplicationID;

                                        eventRow.EventID = (long)interestingEvent["id"];
                                        eventRow.OccuredUtc = convertFromUnixTimestamp((long)interestingEvent["eventTime"]);
                                        eventRow.Occured = eventRow.OccuredUtc.ToLocalTime();
                                        eventRow.Type = interestingEvent["type"].ToString();
                                        eventRow.SubType = interestingEvent["subType"].ToString();
                                        eventRow.Severity = interestingEvent["severity"].ToString();
                                        eventRow.EventLink = interestingEvent["deepLinkUrl"].ToString();
                                        eventRow.Summary = interestingEvent["summary"].ToString();

                                        if (interestingEvent["triggeredEntity"].HasValues == true)
                                        {
                                            eventRow.TriggeredEntityID = (int)interestingEvent["triggeredEntity"]["entityId"];
                                            eventRow.TriggeredEntityName = interestingEvent["triggeredEntity"]["name"].ToString();
                                            string entityType = interestingEvent["triggeredEntity"]["entityType"].ToString();
                                            if (entityTypeStringMapping.ContainsKey(entityType) == true)
                                            {
                                                eventRow.TriggeredEntityType = entityTypeStringMapping[entityType];
                                            }
                                            else
                                            {
                                                eventRow.TriggeredEntityType = entityType;
                                            }
                                        }

                                        foreach (JObject affectedEntity in interestingEvent["affectedEntities"])
                                        {
                                            string entityType = affectedEntity["entityType"].ToString();
                                            switch (entityType)
                                            {
                                                case ENTITY_TYPE_APPLICATION:
                                                    // already have this data
                                                    break;

                                                case ENTITY_TYPE_TIER:
                                                    eventRow.TierID = (int)affectedEntity["entityId"];
                                                    eventRow.TierName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_NODE:
                                                    eventRow.NodeID = (int)affectedEntity["entityId"];
                                                    eventRow.NodeName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_MACHINE:
                                                    eventRow.MachineID = (int)affectedEntity["entityId"];
                                                    eventRow.MachineName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_BUSINESS_TRANSACTION:
                                                    eventRow.BTID = (int)affectedEntity["entityId"];
                                                    eventRow.BTName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_HEALTH_RULE:
                                                    eventRow.TriggeredEntityID = (int)affectedEntity["entityId"];
                                                    eventRow.TriggeredEntityType = entityTypeStringMapping[affectedEntity["entityType"].ToString()];
                                                    eventRow.TriggeredEntityName = affectedEntity["name"].ToString();
                                                    break;

                                                default:
                                                    logger.Warn("Unknown entity type {0} in affectedEntities in events", entityType);
                                                    break;
                                            }
                                        }

                                        eventRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, eventRow.Controller, DEEPLINK_THIS_TIMERANGE);
                                        eventRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, eventRow.Controller, eventRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                                        if (eventRow.TierID != 0)
                                        {
                                            eventRow.TierLink = String.Format(DEEPLINK_TIER, eventRow.Controller, eventRow.ApplicationID, eventRow.TierID, DEEPLINK_THIS_TIMERANGE);
                                        }
                                        if (eventRow.NodeID != 0)
                                        {
                                            eventRow.NodeLink = String.Format(DEEPLINK_NODE, eventRow.Controller, eventRow.ApplicationID, eventRow.NodeID, DEEPLINK_THIS_TIMERANGE);
                                        }
                                        if (eventRow.BTID != 0)
                                        {
                                            eventRow.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, eventRow.Controller, eventRow.ApplicationID, eventRow.BTID, DEEPLINK_THIS_TIMERANGE);
                                        }

                                        eventsRows.Add(eventRow);
                                    }
                                }
                            }
                        }
                        loggerConsole.Info("{0} events", eventsRows.Count);

                        // Sort them
                        eventsRows = eventsRows.OrderBy(o => o.Type).ThenBy(o => o.Occured).ThenBy(o => o.Severity).ToList();

                        string eventsFilePath = Path.Combine(eventsFolderPath, CONVERT_EVENTS_FILE_NAME);
                        FileIOHelper.writeListToCSVFile<Event>(eventsRows, new EventReportMap(), eventsFilePath);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        #endregion

        #region Reporting steps

        private static bool stepReportControlerApplicationsAndEntities(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                loggerConsole.Info("Prepare Detected Entities Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelDetectedEntities = new ExcelPackage();
                excelDetectedEntities.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelDetectedEntities.Workbook.Properties.Title = "AppDynamics DEXTER Detected Entities Report";
                excelDetectedEntities.Workbook.Properties.Subject = programOptions.JobName;

                excelDetectedEntities.Workbook.Properties.Comments = String.Format("Targets={0}\r\nFrom={1:o}\r\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(Color.Blue);

                int l = 1;
                sheet.Cells[l, 1].Value = "Table of Contents";
                sheet.Cells[l, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                l++; l++;
                sheet.Cells[l, 1].Value = "AppDynamics DEXTER Detected Entities Report";
                l++; l++;
                sheet.Cells[l, 1].Value = "From";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.From.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "To";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.To.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded From (UTC)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded From (Local)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime().ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded To (UTC)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded To (Local)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime().ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Number of Hours Intervals";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.HourlyTimeRanges.Count;
                l++;
                sheet.Cells[l, 1].Value = "Export Metrics";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Metrics;
                l++;
                sheet.Cells[l, 1].Value = "Export Snapshots";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Snapshots;
                l++;
                sheet.Cells[l, 1].Value = "Export Flowmaps";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Flowmaps;
                l++;
                sheet.Cells[l, 1].Value = "Export Configuration";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Configuration;
                l++; l++;
                sheet.Cells[l, 1].Value = "Targets:";
                ExcelRangeBase range = sheet.Cells[l, 1].LoadFromCollection(from jobTarget in jobConfiguration.Target
                                                                            select new
                                                                            {
                                                                                Controller = jobTarget.Controller,
                                                                                UserName = jobTarget.UserName,
                                                                                Application = jobTarget.Application,
                                                                                ApplicationID = jobTarget.ApplicationID,
                                                                                Status = jobTarget.Status.ToString()
                                                                            }, true);
                ExcelTable table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_PARAMETERS_TARGETS);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(1).AutoFit();
                sheet.Column(2).AutoFit();
                sheet.Column(3).AutoFit();

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 4);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "Types of App Agent";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT);
                sheet.Cells[3, 1].Value = "Types of Machine Agent";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 3, 5);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 5);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "Types of Backends";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT);
                sheet.Cells[3, 1].Value = "Locations of Backends";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 4);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 5);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "Types of BTs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[3, 1].Value = "Location of BTs";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 5);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 6);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "Type of SEPs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT);
                sheet.Cells[3, 1].Value = "Location of SEPs";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 5);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 6);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "Errors by Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT);
                sheet.Cells[3, 1].Value = "Location of Errors";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 5);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 6);

                #endregion

                List<string> listOfControllersAlreadyProcessed = new List<string>(jobConfiguration.Target.Count);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);

                        // Report files
                        string controllerReportFilePath = Path.Combine(controllerFolderPath, CONVERT_ENTITY_CONTROLLER_FILE_NAME);
                        string applicationsReportFilePath = Path.Combine(controllerFolderPath, CONVERT_ENTITY_APPLICATIONS_FILE_NAME);
                        string tiersReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_TIERS_FILE_NAME);
                        string nodesReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_NODES_FILE_NAME);
                        string backendsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndpointsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string errorsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_ERRORS_FILE_NAME);

                        // Sheet row counters
                        int numRowsToSkipInCSVFile = 0;
                        int fromRow = 1;

                        #endregion

                        #region Controllers and Applications

                        // Only output this once per controller
                        if (listOfControllersAlreadyProcessed.Contains(jobTarget.Controller) == false)
                        {
                            listOfControllersAlreadyProcessed.Add(jobTarget.Controller);

                            loggerConsole.Info("List of Controllers");

                            sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS_LIST];
                            if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                                numRowsToSkipInCSVFile = 0;
                            }
                            else
                            {
                                fromRow = sheet.Dimension.Rows + 1;
                                numRowsToSkipInCSVFile = 1;
                            }
                            readCSVFileIntoExcelRange(controllerReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                            loggerConsole.Info("List of Applications");

                            sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST];
                            if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                                numRowsToSkipInCSVFile = 0;
                            }
                            else
                            {
                                fromRow = sheet.Dimension.Rows + 1;
                                numRowsToSkipInCSVFile = 1;
                            }
                            readCSVFileIntoExcelRange(applicationsReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);
                        }

                        #endregion

                        #region Tiers

                        loggerConsole.Info("List of Tiers");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(tiersReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Nodes

                        loggerConsole.Info("List of Nodes");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(nodesReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Backends

                        loggerConsole.Info("List of Backends");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(backendsReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Business Transactions

                        loggerConsole.Info("List of Business Transactions");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(businessTransactionsReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Service Endpoints

                        loggerConsole.Info("List of Service Endpoints");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(serviceEndpointsReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Errors

                        loggerConsole.Info("List of Errors");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(errorsReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                loggerConsole.Info("Finalize Detected Entities Report File");

                #region Controllers sheet

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS_LIST];
                loggerConsole.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["UserName"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                }

                #endregion

                #region Applications

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST];
                loggerConsole.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_APPLICATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APPLICATION_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST];
                loggerConsole.Info("Tiers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_TIERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(TIERS_TYPE_SHORT, sheet, table);

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_TIERS);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["AgentType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["TierName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "Tiers Of Type";
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST];
                loggerConsole.Info("Nodes Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_NODES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(NODES_TYPE_SHORT, sheet, table);

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_APPAGENT);
                    ExcelPivotTableField fieldF = pivot.PageFields.Add(pivot.Fields["AgentPresent"]);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["NodeName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["AgentType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    fieldC = pivot.ColumnFields.Add(pivot.Fields["AgentVersion"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["TierName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "Agents Of Type";

                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_MACHINEAGENT);
                    fieldF = pivot.PageFields.Add(pivot.Fields["MachineAgentPresent"]);
                    fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["MachineName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldC = pivot.ColumnFields.Add(pivot.Fields["MachineAgentVersion"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    fieldD = pivot.DataFields.Add(pivot.Fields["TierName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "Machine Agents Of Type";
                }

                #endregion

                #region Backends

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST];
                loggerConsole.Info("Backends Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_BACKENDS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(BACKENDS_TYPE_SHORT, sheet, table);

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_TYPE);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["BackendName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["BackendType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["BackendName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "Backends Of Type";

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_LOCATION);
                    fieldR = pivot.RowFields.Add(pivot.Fields["BackendType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["BackendName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldD = pivot.DataFields.Add(pivot.Fields["BackendName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST];
                loggerConsole.Info("Business Transactions Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_BUSINESS_TRANSACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(BUSINESS_TRANSACTIONS_TYPE_SHORT, sheet, table);

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_TYPE);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["BTName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["BTType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["BTName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "BTs Of Type";

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_LOCATION_SHEET);
                    fieldR = pivot.RowFields.Add(pivot.Fields["BTType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["BTName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldD = pivot.DataFields.Add(pivot.Fields["BTName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }

                #endregion

                #region Service Endpoints

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST];
                loggerConsole.Info("Service Endpoints Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_SERVICE_ENDPOINTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(SERVICE_ENDPOINTS_TYPE_SHORT, sheet, table);

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_TYPE);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["SEPName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["SEPType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["SEPName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "SEPs Of Type";

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_LOCATION);
                    fieldR = pivot.RowFields.Add(pivot.Fields["SEPType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["SEPName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldD = pivot.DataFields.Add(pivot.Fields["SEPName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }

                #endregion

                #region Errors

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST];
                loggerConsole.Info("Errors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_ERRORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(ERRORS_TYPE_SHORT, sheet, table);

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_TYPE);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ErrorName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["ErrorType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["ErrorName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                    //fieldD.Name = "Errors Of Type";

                    // Make pivot
                    sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_LOCATION);
                    fieldR = pivot.RowFields.Add(pivot.Fields["ErrorType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ErrorName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldD = pivot.DataFields.Add(pivot.Fields["ErrorName"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_SHEET_TOC];
                sheet.Cells[1, 1].Value = "Sheet Name";
                sheet.Cells[1, 2].Value = "# Entities";
                sheet.Cells[1, 3].Value = "Link";
                int rowNum = 1;
                foreach (ExcelWorksheet s in excelDetectedEntities.Workbook.Worksheets)
                {
                    rowNum++;
                    sheet.Cells[rowNum, 1].Value = s.Name;
                    sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                    if (s.Tables.Count > 0)
                    {
                        table = s.Tables[0];
                        sheet.Cells[rowNum, 2].Value = table.Address.Rows - 1;
                    }
                }
                range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_TOC);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Sheet Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["# Entities"].Position + 1).AutoFit();

                #endregion

                #region Save file 

                // Report files
                string reportFileName = String.Format(
                    REPORT_DETECTED_ENTITIES_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To);
                string reportFilePath = Path.Combine(programOptions.OutputJobFolderPath, reportFileName);

                logger.Info("Saving Excel report {0}", reportFilePath);
                loggerConsole.Info("Saving Excel report {0}", reportFilePath);

                try
                {
                    // Save full report Excel files
                    excelDetectedEntities.SaveAs(new FileInfo(reportFilePath));
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warn("Unable to save Excel file {0}", reportFilePath);
                    logger.Warn(ex);
                    loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);

                    return false;
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepReportControllerAndApplicationConfiguration(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);
            return true;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);

                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepReportApplicationAndEntityMetrics(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                loggerConsole.Info("Prepare Entity Metrics Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelDetectedEntities = new ExcelPackage();
                excelDetectedEntities.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelDetectedEntities.Workbook.Properties.Title = "AppDynamics DEXTER Entity Metrics Report";
                excelDetectedEntities.Workbook.Properties.Subject = programOptions.JobName;

                excelDetectedEntities.Workbook.Properties.Comments = String.Format("Targets={0}\r\nFrom={1:o}\r\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(Color.Blue);

                int l = 1;
                sheet.Cells[l, 1].Value = "Table of Contents";
                sheet.Cells[l, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                l++; l++;
                sheet.Cells[l, 1].Value = "AppDynamics DEXTER Entity Metrics Report";
                l++; l++;
                sheet.Cells[l, 1].Value = "From";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.From.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "To";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.To.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded From (UTC)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded From (Local)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime().ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded To (UTC)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded To (Local)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime().ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Number of Hours Intervals";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.HourlyTimeRanges.Count;
                l++;
                sheet.Cells[l, 1].Value = "Export Metrics";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Metrics;
                l++;
                sheet.Cells[l, 1].Value = "Export Snapshots";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Snapshots;
                l++;
                sheet.Cells[l, 1].Value = "Export Flowmaps";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Flowmaps;
                l++;
                sheet.Cells[l, 1].Value = "Export Configuration";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Configuration;
                l++; l++;
                sheet.Cells[l, 1].Value = "Targets:";
                ExcelRangeBase range = sheet.Cells[l, 1].LoadFromCollection(from jobTarget in jobConfiguration.Target
                                                                            select new
                                                                            {
                                                                                Controller = jobTarget.Controller,
                                                                                UserName = jobTarget.UserName,
                                                                                Application = jobTarget.Application,
                                                                                ApplicationID = jobTarget.ApplicationID,
                                                                                Status = jobTarget.Status.ToString()
                                                                            }, true);
                ExcelTable table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_PARAMETERS_TARGETS);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(1).AutoFit();
                sheet.Column(2).AutoFit();
                sheet.Column(3).AutoFit();

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_CONTROLLERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEntities.Workbook.Worksheets.Add(REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.View.FreezePanes(REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                List<string> listOfControllersAlreadyProcessed = new List<string>(jobConfiguration.Target.Count);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);

                        // Report files
                        string controllerReportFilePath = Path.Combine(controllerFolderPath, CONVERT_ENTITY_CONTROLLER_FILE_NAME);

                        // Metric paths and files
                        string metricsEntityFolderPath = String.Empty;
                        string entityFullRangeReportFilePath = String.Empty;
                        string entityHourlyRangeReportFilePath = String.Empty;
                        string entitiesFullRangeReportFilePath = String.Empty;
                        string entitiesHourlyRangeReportFilePath = String.Empty;

                        // Sheet row counters
                        int numRowsToSkipInCSVFile = 0;
                        int fromRow = 1;

                        #endregion

                        #region Controllers

                        // Only output this once per controller
                        if (listOfControllersAlreadyProcessed.Contains(jobTarget.Controller) == false)
                        {
                            listOfControllersAlreadyProcessed.Add(jobTarget.Controller);

                            loggerConsole.Info("List of Controllers");

                            sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_CONTROLLERS_LIST];
                            if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                                numRowsToSkipInCSVFile = 0;
                            }
                            else
                            {
                                fromRow = sheet.Dimension.Rows + 1;
                                numRowsToSkipInCSVFile = 1;
                            }
                            readCSVFileIntoExcelRange(controllerReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);
                        }

                        #endregion

                        #region Applications

                        loggerConsole.Info("List of Applications (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, APPLICATION_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Applications (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Tiers

                        loggerConsole.Info("List of Tiers (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, TIERS_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Tiers (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Nodes

                        loggerConsole.Info("List of Nodes (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, NODES_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Nodes (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Backends

                        loggerConsole.Info("List of Backends (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, BACKENDS_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Backends (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Business Transactions

                        loggerConsole.Info("List of Business Transactions (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, BUSINESS_TRANSACTIONS_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Business Transactions (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Service Endpoints

                        loggerConsole.Info("List of Service Endpoints (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, SERVICE_ENDPOINTS_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Service Endpoints (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Errors

                        loggerConsole.Info("List of Errors (Full)");

                        metricsEntityFolderPath = Path.Combine(metricsFolderPath, ERRORS_TYPE_SHORT);

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_FULL];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_FULLRANGE_FILE_NAME);
                        readCSVFileIntoExcelRange(entityFullRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        loggerConsole.Info("List of Errors (Hourly)");

                        sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_HOURLY];
                        if (sheet.Dimension.Rows < REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITIES_METRICS_HOURLY_FILE_NAME);
                        readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                loggerConsole.Info("Finalize Entity Metrics Report File");

                #region Controllers sheet

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_CONTROLLERS_LIST];
                loggerConsole.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["UserName"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                }

                #endregion

                #region Applications

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_FULL];
                loggerConsole.Info("Applications Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_APPLICATIONS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APPLICATION_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_APPLICATIONS_HOURLY];
                loggerConsole.Info("Applications Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_APPLICATIONS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APPLICATION_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_FULL];
                loggerConsole.Info("Tiers Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_TIERS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(TIERS_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_TIERS_HOURLY];
                loggerConsole.Info("Tiers Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_TIERS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(TIERS_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_FULL];
                loggerConsole.Info("Nodes Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_NODES_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(NODES_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_NODES_HOURLY];
                loggerConsole.Info("Nodes Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_NODES_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(NODES_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Backends

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_FULL];
                loggerConsole.Info("Backends Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_BACKENDS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(BACKENDS_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BACKENDS_HOURLY];
                loggerConsole.Info("Backends Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_BACKENDS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(BACKENDS_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_FULL];
                loggerConsole.Info("Business Transactions Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_BUSINESS_TRANSACTIONS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(BUSINESS_TRANSACTIONS_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_HOURLY];
                loggerConsole.Info("Business Transactions Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_BUSINESS_TRANSACTIONS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(BUSINESS_TRANSACTIONS_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Service Endpoints

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_FULL];
                loggerConsole.Info("Service Endpoints Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_SERVICE_ENDPOINTS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(SERVICE_ENDPOINTS_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_SERVICE_ENDPOINTS_HOURLY];
                loggerConsole.Info("Service Endpoints Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_SERVICE_ENDPOINTS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(SERVICE_ENDPOINTS_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region Errors

                // Make table
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_FULL];
                loggerConsole.Info("Errors Sheet Full ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_ERRORS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(ERRORS_TYPE_SHORT, sheet, table);
                }

                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_METRICS_ALL_ENTITIES_SHEET_ERRORS_HOURLY];
                loggerConsole.Info("Errors Sheet Hourly ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_METRICS_ALL_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_ERRORS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(ERRORS_TYPE_SHORT, sheet, table);
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelDetectedEntities.Workbook.Worksheets[REPORT_SHEET_TOC];
                sheet.Cells[1, 1].Value = "Sheet Name";
                sheet.Cells[1, 2].Value = "# Rows";
                sheet.Cells[1, 3].Value = "Link";
                int rowNum = 1;
                foreach (ExcelWorksheet s in excelDetectedEntities.Workbook.Worksheets)
                {
                    rowNum++;
                    sheet.Cells[rowNum, 1].Value = s.Name;
                    sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                    if (s.Tables.Count > 0)
                    {
                        table = s.Tables[0];
                        sheet.Cells[rowNum, 2].Value = table.Address.Rows - 1;
                    }
                }
                range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, REPORT_METRICS_ALL_ENTITIES_TABLE_TOC);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Sheet Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["# Rows"].Position + 1).AutoFit();

                #endregion

                #region Save file 

                // Report files
                string reportFileName = String.Format(
                    REPORT_METRICS_ALL_ENTITIES_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To);
                string reportFilePath = Path.Combine(programOptions.OutputJobFolderPath, reportFileName);

                logger.Info("Saving Excel report {0}", reportFilePath);
                loggerConsole.Info("Saving Excel report {0}", reportFilePath);

                try
                {
                    // Save full report Excel files
                    excelDetectedEntities.SaveAs(new FileInfo(reportFilePath));
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warn("Unable to save Excel file {0}", reportFilePath);
                    logger.Warn(ex);
                    loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);

                    return false;
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepReportEventsAndHealthRuleViolations(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                loggerConsole.Info("Prepare Events and Health Rule Violations Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelDetectedEvents = new ExcelPackage();
                excelDetectedEvents.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelDetectedEvents.Workbook.Properties.Title = "AppDynamics DEXTER Events and Health Rule Violations Report";
                excelDetectedEvents.Workbook.Properties.Subject = programOptions.JobName;

                excelDetectedEvents.Workbook.Properties.Comments = String.Format("Targets={0}\r\nFrom={1:o}\r\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelDetectedEvents.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(Color.Blue);

                int l = 1;
                sheet.Cells[l, 1].Value = "Table of Contents";
                sheet.Cells[l, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                l++; l++;
                sheet.Cells[l, 1].Value = "AppDynamics DEXTER Events and Health Rule Violations Report";
                l++; l++;
                sheet.Cells[l, 1].Value = "From";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.From.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "To";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.To.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded From (UTC)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded From (Local)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime().ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded To (UTC)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Expanded To (Local)";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime().ToString("G");
                l++;
                sheet.Cells[l, 1].Value = "Number of Hours Intervals";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.HourlyTimeRanges.Count;
                l++;
                sheet.Cells[l, 1].Value = "Export Metrics";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Metrics;
                l++;
                sheet.Cells[l, 1].Value = "Export Snapshots";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Snapshots;
                l++;
                sheet.Cells[l, 1].Value = "Export Flowmaps";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Flowmaps;
                l++;
                sheet.Cells[l, 1].Value = "Export Configuration";
                sheet.Cells[l, 2].Value = jobConfiguration.Input.Configuration;
                l++; l++;
                sheet.Cells[l, 1].Value = "Targets:";
                ExcelRangeBase range = sheet.Cells[l, 1].LoadFromCollection(from jobTarget in jobConfiguration.Target
                                                                            select new
                                                                            {
                                                                                Controller = jobTarget.Controller,
                                                                                UserName = jobTarget.UserName,
                                                                                Application = jobTarget.Application,
                                                                                ApplicationID = jobTarget.ApplicationID,
                                                                                Status = jobTarget.Status.ToString()
                                                                            }, true);
                ExcelTable table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_PARAMETERS_TARGETS);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(1).AutoFit();
                sheet.Column(2).AutoFit();
                sheet.Column(3).AutoFit();

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelDetectedEvents.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelDetectedEvents.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEvents.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS);
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + 2, 9);

                sheet = excelDetectedEvents.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR_PIVOT);
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelDetectedEvents.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR);
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + 2, 9);

                #endregion

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string eventsFolderPath = Path.Combine(applicationFolderPath, EVENTS_FOLDER_NAME);

                        // Sheet row counters
                        int numRowsToSkipInCSVFile = 0;
                        int fromRow = 1;

                        #endregion

                        #region Events

                        loggerConsole.Info("List of Events");

                        string eventsFilePath = Path.Combine(eventsFolderPath, CONVERT_EVENTS_FILE_NAME);

                        sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(eventsFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion

                        #region Health Rule Violation Events

                        loggerConsole.Info("List of Health Rule Violation Events");

                        eventsFilePath = Path.Combine(eventsFolderPath, CONVERT_HEALTH_RULE_EVENTS_FILE_NAME);

                        sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR];
                        if (sheet.Dimension.Rows < REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                        {
                            fromRow = REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT;
                            numRowsToSkipInCSVFile = 0;
                        }
                        else
                        {
                            fromRow = sheet.Dimension.Rows + 1;
                            numRowsToSkipInCSVFile = 1;
                        }
                        readCSVFileIntoExcelRange(eventsFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                loggerConsole.Info("Finalize Events and Health Rule Violations Report File");

                #region Events

                // Make table
                sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS];
                loggerConsole.Info("Events Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_EVENTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EventID"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["Occured"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["OccuredUtc"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["Summary"].Position + 1).Width = 35;
                    sheet.Column(table.Columns["Type"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SubType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TriggeredEntityType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TriggeredEntityName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["NodeLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["BTLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["EventLink"].Position + 1).AutoFit();

                    sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TYPE);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Type"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["SubType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Severity"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["BTName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["NodeName"]);
                    // Adding this in Excel produces days, hours and minutes subfields, very nice
                    // Adding this in code produces dates formatted strings, not very good
                    //ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["Occured"]);
                    //fieldC.Compact = false;
                    //fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["EventID"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }

                #endregion

                #region Health Rule Violation Events

                // Make table
                sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR];
                loggerConsole.Info("Health Rule Events Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_EVENTS_HR);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EventID"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["HealthRuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["HealthRuleLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["EntityLink"].Position + 1).AutoFit();
                    sheet.Column(table.Columns["EventLink"].Position + 1).AutoFit();

                    sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS_HR_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_EVENTS_PIVOT_EVENTS_HR_TYPE);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["HealthRuleName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Severity"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Status"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["EntityType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.ColumnFields.Add(pivot.Fields["EntityName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["EventID"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }

                #endregion


                #region TOC sheet

                // TOC sheet again
                sheet = excelDetectedEvents.Workbook.Worksheets[REPORT_SHEET_TOC];
                sheet.Cells[1, 1].Value = "Sheet Name";
                sheet.Cells[1, 2].Value = "# Entities";
                sheet.Cells[1, 3].Value = "Link";
                int rowNum = 1;
                foreach (ExcelWorksheet s in excelDetectedEvents.Workbook.Worksheets)
                {
                    rowNum++;
                    sheet.Cells[rowNum, 1].Value = s.Name;
                    sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                    if (s.Tables.Count > 0)
                    {
                        table = s.Tables[0];
                        sheet.Cells[rowNum, 2].Value = table.Address.Rows - 1;
                    }
                }
                range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_TOC);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Sheet Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["# Entities"].Position + 1).AutoFit();

                #endregion

                #region Save file 

                // Report files
                string reportFileName = String.Format(
                    REPORT_DETECTED_EVENTS_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To);
                string reportFilePath = Path.Combine(programOptions.OutputJobFolderPath, reportFileName);

                logger.Info("Saving Excel report {0}", reportFilePath);
                loggerConsole.Info("Saving Excel report {0}", reportFilePath);

                try
                {
                    // Save full report Excel files
                    excelDetectedEvents.SaveAs(new FileInfo(reportFilePath));
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warn("Unable to save Excel file {0}", reportFilePath);
                    logger.Warn(ex);
                    loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);

                    return false;
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepReportIndividualApplicationAndEntityDetails(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        //string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);

                        // Report files
                        string applicationReportFilePath = Path.Combine(applicationFolderPath, CONVERT_ENTITY_APPLICATION_FILE_NAME);
                        string tiersReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_TIERS_FILE_NAME);
                        string nodesReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_NODES_FILE_NAME);
                        string backendsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndpointsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string errorsReportFilePath = Path.Combine(entitiesFolderPath, CONVERT_ENTITY_ERRORS_FILE_NAME);

                        #endregion

                        #region Application

                        List<EntityApplication> applicationRows = FileIOHelper.readListFromCSVFile<EntityApplication>(applicationReportFilePath, new ApplicationEntityReportMap());
                        if (applicationRows != null && applicationRows.Count > 0)
                        {
                            loggerConsole.Info("Metric Details for Application");

                            reportMetricDetailApplication(programOptions, jobConfiguration, jobTarget, applicationRows[0]);
                        }

                        #endregion

                        #region Tier

                        List<EntityTier> tiersRows = FileIOHelper.readListFromCSVFile<EntityTier>(tiersReportFilePath, new TierEntityReportMap());
                        if (tiersRows != null)
                        {
                            loggerConsole.Info("Metric Details for Tiers ({0} entities)", tiersRows.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var tiersRowsChunks = tiersRows.BreakListIntoChunks(METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<EntityTier>, int>(
                                    tiersRowsChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (tiersRowsChunk, loop, subtotal) =>
                                    {
                                        subtotal += reportMetricDetailTiers(programOptions, jobConfiguration, jobTarget, tiersRowsChunk, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = reportMetricDetailTiers(programOptions, jobConfiguration, jobTarget, tiersRows, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Nodes

                        List<EntityNode> nodesRows = FileIOHelper.readListFromCSVFile<EntityNode>(nodesReportFilePath, new NodeEntityReportMap());
                        if (nodesRows != null)
                        {
                            loggerConsole.Info("Metric Details for Nodes ({0} entities)", nodesRows.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var nodesRowsChunks = nodesRows.BreakListIntoChunks(METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<EntityNode>, int>(
                                    nodesRowsChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (nodesRowsChunk, loop, subtotal) =>
                                    {
                                        subtotal += reportMetricDetailNodes(programOptions, jobConfiguration, jobTarget, nodesRowsChunk, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = reportMetricDetailNodes(programOptions, jobConfiguration, jobTarget, nodesRows, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Backends

                        List<EntityBackend> backendsRows = FileIOHelper.readListFromCSVFile<EntityBackend>(backendsReportFilePath, new BackendEntityReportMap());
                        if (backendsRows != null)
                        {
                            loggerConsole.Info("Metric Details for Backends ({0} entities)", backendsRows.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var backendsRowsChunks = backendsRows.BreakListIntoChunks(METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<EntityBackend>, int>(
                                    backendsRowsChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (backendsRowsChunk, loop, subtotal) =>
                                    {
                                        subtotal += reportMetricDetailBackends(programOptions, jobConfiguration, jobTarget, backendsRowsChunk, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = reportMetricDetailBackends(programOptions, jobConfiguration, jobTarget, backendsRows, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Business Transactions

                        List<EntityBusinessTransaction> businessTransactionsRows = FileIOHelper.readListFromCSVFile<EntityBusinessTransaction>(businessTransactionsReportFilePath, new BusinessTransactionEntityReportMap());
                        if (businessTransactionsRows != null)
                        {
                            loggerConsole.Info("Metric Details for Business Transactions ({0} entities)", businessTransactionsRows.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var businessTransactionsRowsChunks = businessTransactionsRows.BreakListIntoChunks(METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<EntityBusinessTransaction>, int>(
                                    businessTransactionsRowsChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (businessTransactionsRowsChunk, loop, subtotal) =>
                                    {
                                        subtotal += reportMetricDetailBusinessTransactions(programOptions, jobConfiguration, jobTarget, businessTransactionsRowsChunk, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = reportMetricDetailBusinessTransactions(programOptions, jobConfiguration, jobTarget, businessTransactionsRows, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Service Endpoints

                        List<EntityServiceEndpoint> serviceEndpointsRows = FileIOHelper.readListFromCSVFile<EntityServiceEndpoint>(serviceEndpointsReportFilePath, new ServiceEndpointEntityReportMap());
                        if (serviceEndpointsRows != null)
                        {
                            loggerConsole.Info("Metric Details for Service Endpoints ({0} entities)", serviceEndpointsRows.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var serviceEndpointsRowsChunks = serviceEndpointsRows.BreakListIntoChunks(METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<EntityServiceEndpoint>, int>(
                                    serviceEndpointsRowsChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (serviceEndpointsRowsChunk, loop, subtotal) =>
                                    {
                                        subtotal += reportMetricDetailServiceEndpoints(programOptions, jobConfiguration, jobTarget, serviceEndpointsRowsChunk, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = reportMetricDetailServiceEndpoints(programOptions, jobConfiguration, jobTarget, serviceEndpointsRows, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                        #region Errors

                        List<EntityError> errorsRows = FileIOHelper.readListFromCSVFile<EntityError>(errorsReportFilePath, new ErrorEntityReportMap());
                        if (errorsRows != null)
                        {
                            loggerConsole.Info("Metric Details for Errors ({0} entities)", errorsRows.Count);

                            int j = 0;

                            if (programOptions.ProcessSequentially == false)
                            {
                                var errorsRowsChunks = errorsRows.BreakListIntoChunks(METRIC_DETAILS_REPORT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<EntityError>, int>(
                                    errorsRowsChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = METRIC_DETAILS_REPORT_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (errorsRowsChunk, loop, subtotal) =>
                                    {
                                        subtotal += reportMetricDetailErrors(programOptions, jobConfiguration, jobTarget, errorsRowsChunk, false);
                                        return subtotal;
                                    },
                                    (finalResult) =>
                                    {
                                        j = Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );
                            }
                            else
                            {
                                j = reportMetricDetailErrors(programOptions, jobConfiguration, jobTarget, errorsRows, true);
                            }

                            loggerConsole.Info("{0} entities", j);
                        }

                        #endregion

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        private static bool stepReportSnapshots(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobStatus jobStatus)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        #region Output status

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4}", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application);

                        #endregion

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        loggerConsole.Fatal("TODO {0}({0:d})", jobStatus);

                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        logger.Info("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                        loggerConsole.Trace("{0}({0:d}): [{1}/{2}], {3} {4} duration {5:c} ({6} ms)", jobStatus, i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("{0}({0:d}): total duration {1:c} ({2} ms)", jobStatus, stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
            }
        }

        #endregion


        #region Metric extraction functions

        private static int extractMetricsApplication(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, string metricsFolderPath)
        {
            string metricsEntityFolderPath = Path.Combine(
                metricsFolderPath,
                APPLICATION_TYPE_SHORT);

            getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_APPLICATION, METRIC_ART_FULLNAME), METRIC_ART_FULLNAME, jobConfiguration, metricsEntityFolderPath);
            getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_APPLICATION, METRIC_CPM_FULLNAME), METRIC_CPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
            getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_APPLICATION, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
            getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_APPLICATION, METRIC_EXCPM_FULLNAME), METRIC_EXCPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
            getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_APPLICATION, METRIC_HTTPEPM_FULLNAME), METRIC_HTTPEPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

            return 1;
        }

        private static int extractMetricsTiers(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTTier> entityList, string metricsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTTier tier in entityList)
            {
                string metricsEntityFolderPath = Path.Combine(
                    metricsFolderPath,
                    TIERS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(tier.name, tier.id));

                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_TIER, tier.name, METRIC_ART_FULLNAME), METRIC_ART_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_TIER, tier.name, METRIC_CPM_FULLNAME), METRIC_CPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_TIER, tier.name, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_TIER, tier.name, METRIC_EXCPM_FULLNAME), METRIC_EXCPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_TIER, tier.name, METRIC_HTTPEPM_FULLNAME), METRIC_HTTPEPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

                FileIOHelper.writeObjectToFile(tier, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractMetricsNodes(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTNode> entityList, string metricsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTNode node in entityList)
            {
                string metricsEntityFolderPath = Path.Combine(
                    metricsFolderPath,
                    NODES_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                    getShortenedEntityNameForFileSystem(node.name, node.id));

                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_ART_FULLNAME), METRIC_ART_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_CPM_FULLNAME), METRIC_CPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_EXCPM_FULLNAME), METRIC_EXCPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_HTTPEPM_FULLNAME), METRIC_HTTPEPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

                FileIOHelper.writeObjectToFile(node, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractMetricsBackends(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTBackend> entityList, string metricsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTBackend backend in entityList)
            {
                string metricsEntityFolderPath = Path.Combine(
                    metricsFolderPath,
                    BACKENDS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(backend.name, backend.id));

                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_BACKEND, backend.name, METRIC_ART_FULLNAME), METRIC_ART_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_BACKEND, backend.name, METRIC_CPM_FULLNAME), METRIC_CPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_BACKEND, backend.name, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

                FileIOHelper.writeObjectToFile(backend, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractMetricsBusinessTransactions(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTBusinessTransaction> entityList, string metricsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTBusinessTransaction businessTransaction in entityList)
            {
                string metricsEntityFolderPath = Path.Combine(
                    metricsFolderPath,
                    BUSINESS_TRANSACTIONS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(businessTransaction.tierName, businessTransaction.tierId),
                    getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id));

                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_BUSINESS_TRANSACTION, businessTransaction.tierName, businessTransaction.name, METRIC_ART_FULLNAME), METRIC_ART_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_BUSINESS_TRANSACTION, businessTransaction.tierName, businessTransaction.name, METRIC_CPM_FULLNAME), METRIC_CPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_BUSINESS_TRANSACTION, businessTransaction.tierName, businessTransaction.name, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

                FileIOHelper.writeObjectToFile(businessTransaction, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractMetricsServiceEndpoints(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTMetric> entityList, List<AppDRESTTier> tiersList, string metricsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTMetric serviceEndpoint in entityList)
            {
                // Parse SEP values
                string serviceEndpointTierName = serviceEndpoint.metricPath.Split('|')[1];
                long serviceEndpointTierID = -1;
                if (tiersList != null)
                {
                    // metricPath
                    // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest|Calls per Minute
                    //                   ^^^^^^^^^^^^^^^^^^
                    //                   Tier
                    AppDRESTTier tierForThisEntity = tiersList.Where(tier => tier.name == serviceEndpointTierName).FirstOrDefault();
                    if (tierForThisEntity != null)
                    {
                        serviceEndpointTierID = tierForThisEntity.id;
                    }
                }
                // metricName
                // BTM|Application Diagnostic Data|SEP:4855|Calls per Minute
                //                                     ^^^^
                //                                     ID
                int serviceEndpointID = Convert.ToInt32(serviceEndpoint.metricName.Split('|')[2].Split(':')[1]);
                // metricPath
                // Service Endpoints|ECommerce-Services|/appdynamicspilot/rest|Calls per Minute
                //                                      ^^^^^^^^^^^^^^^^^^^^^^
                //                                      Name
                string serviceEndpointName = serviceEndpoint.metricPath.Split('|')[2];

                string metricsEntityFolderPath = Path.Combine(
                    metricsFolderPath,
                    SERVICE_ENDPOINTS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(serviceEndpointTierName, serviceEndpointTierID),
                    getShortenedEntityNameForFileSystem(serviceEndpointName, serviceEndpointID));

                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_SERVICE_ENDPOINT, serviceEndpointTierName, serviceEndpointName, METRIC_ART_FULLNAME), METRIC_ART_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_SERVICE_ENDPOINT, serviceEndpointTierName, serviceEndpointName, METRIC_CPM_FULLNAME), METRIC_CPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);
                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_SERVICE_ENDPOINT, serviceEndpointTierName, serviceEndpointName, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

                FileIOHelper.writeObjectToFile(serviceEndpoint, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractMetricsErrors(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTMetric> entityList, List<AppDRESTTier> tiersList, string metricsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTMetric error in entityList)
            {
                // Parse Error values
                string errorTierName = error.metricPath.Split('|')[1];
                long errorTierID = -1;
                if (tiersList != null)
                {
                    // metricPath
                    // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                    //        ^^^^^^^^^^^^^^^^^^
                    //        Tier
                    AppDRESTTier tierForThisEntity = tiersList.Where(tier => tier.name == errorTierName).FirstOrDefault();
                    if (tierForThisEntity != null)
                    {
                        errorTierID = tierForThisEntity.id;
                    }
                }
                // metricName
                // BTM|Application Diagnostic Data|Error:11626|Errors per Minute
                //                                       ^^^^^
                //                                       ID
                int errorID = Convert.ToInt32(error.metricName.Split('|')[2].Split(':')[1]);
                // metricPath
                // Errors|ECommerce-Services|CommunicationsException : EOFException|Errors per Minute
                //                           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                //                           Name
                string errorName = error.metricPath.Split('|')[2];

                string metricsEntityFolderPath = Path.Combine(
                    metricsFolderPath,
                    ERRORS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(errorTierName, errorTierID),
                    getShortenedEntityNameForFileSystem(errorName, errorID));

                getMetricDataForMetricForAllRanges(controllerApi, jobTarget, String.Format(METRIC_PATH_ERROR, errorTierName, errorName, METRIC_EPM_FULLNAME), METRIC_EPM_FULLNAME, jobConfiguration, metricsEntityFolderPath);

                FileIOHelper.writeObjectToFile(error, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static void getMetricDataForMetricForAllRanges(ControllerApi controllerApi, JobTarget jobTarget, string metricPath, string metricName, JobConfiguration jobConfiguration, string metricsEntityFolderPath)
        {
            string metricEntitySubFolderName = metricNameToShortMetricNameMapping[metricName];

            // Get the full range
            JobTimeRange jobTimeRange = jobConfiguration.Input.ExpandedTimeRange;

            logger.Info("Retrieving metric for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricPath, jobTimeRange.From, jobTimeRange.To);

            string metricsJson = String.Empty;

            string metricsDataFilePath = Path.Combine(metricsEntityFolderPath, metricEntitySubFolderName, String.Format(EXTRACT_METRIC_FULL_FILE_NAME, jobTimeRange.From, jobTimeRange.To));
            if (File.Exists(metricsDataFilePath) == false)
            {
                // First range is the whole thing
                metricsJson = controllerApi.GetMetricData(
                    jobTarget.ApplicationID,
                    metricPath,
                    convertToUnixTimestamp(jobTimeRange.From),
                    convertToUnixTimestamp(jobTimeRange.To),
                    true);

                if (metricsJson != String.Empty) FileIOHelper.saveFileToFolder(metricsJson, metricsDataFilePath);
            }

            // Get the hourly time ranges
            for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
            {
                jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                logger.Info("Retrieving metric for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricPath, jobTimeRange.From, jobTimeRange.To);

                metricsDataFilePath = Path.Combine(metricsEntityFolderPath, metricEntitySubFolderName, String.Format(EXTRACT_METRIC_HOUR_FILE_NAME, jobTimeRange.From, jobTimeRange.To));

                if (File.Exists(metricsDataFilePath) == false)
                {
                    // Subsequent ones are details
                    metricsJson = controllerApi.GetMetricData(
                        jobTarget.ApplicationID,
                        metricPath,
                        convertToUnixTimestamp(jobTimeRange.From),
                        convertToUnixTimestamp(jobTimeRange.To),
                        false);

                    if (metricsJson != String.Empty) FileIOHelper.saveFileToFolder(metricsJson, metricsDataFilePath);
                }
            }
        }

        #endregion

        #region Flowmap extraction functions

        private static int extractFlowmapsApplication(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, string metricsFolderPath, long fromTimeUnix, long toTimeUnix, long differenceInMinutes)
        {
            logger.Info("Retrieving flowmap for Application {0}, From {1:o}, To {2:o}", jobTarget.Application, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);

            string flowmapDataFilePath = Path.Combine(
                metricsFolderPath,
                APPLICATION_TYPE_SHORT,
                String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

            string flowmapJson = String.Empty;

            if (File.Exists(flowmapDataFilePath) == false)
            {
                flowmapJson = controllerApi.GetFlowmapApplication(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                if (flowmapJson != String.Empty) FileIOHelper.saveFileToFolder(flowmapJson, flowmapDataFilePath);
            }

            return 1;
        }

        private static int extractFlowmapsTiers(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTTier> entityList, string metricsFolderPath, long fromTimeUnix, long toTimeUnix, long differenceInMinutes, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTTier tier in entityList)
            {
                logger.Info("Retrieving flowmap for Application {0}, Tier {1}, From {2:o}, To {3:o}", jobTarget.Application, tier.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);

                string flowmapDataFilePath = Path.Combine(
                    metricsFolderPath,
                    TIERS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(tier.name, tier.id),
                    String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                if (File.Exists(flowmapDataFilePath) == false)
                {
                    string flowmapJson = controllerApi.GetFlowmapTier(tier.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                    if (flowmapJson != String.Empty) FileIOHelper.saveFileToFolder(flowmapJson, flowmapDataFilePath);
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractFlowmapsNodes(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTNode> entityList, string metricsFolderPath, long fromTimeUnix, long toTimeUnix, long differenceInMinutes, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTNode node in entityList)
            {
                logger.Info("Retrieving flowmap for Application {0}, Tier {1}, Node {2}, From {3:o}, To {4:o}", jobTarget.Application, node.tierName, node.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);

                string flowmapDataFilePath = Path.Combine(
                    metricsFolderPath,
                    NODES_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                    getShortenedEntityNameForFileSystem(node.name, node.id),
                    String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                if (File.Exists(flowmapDataFilePath) == false)
                {
                    string flowmapJson = controllerApi.GetFlowmapNode(node.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                    if (flowmapJson != String.Empty) FileIOHelper.saveFileToFolder(flowmapJson, flowmapDataFilePath);
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractFlowmapsBackends(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTBackend> entityList, string metricsFolderPath, long fromTimeUnix, long toTimeUnix, long differenceInMinutes, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTBackend backend in entityList)
            {
                logger.Info("Retrieving flowmap for Application {0}, Backend {1}, From {2:o}, To {3:o}", jobTarget.Application, backend.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);

                string flowmapDataFilePath = Path.Combine(
                    metricsFolderPath,
                    BACKENDS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(backend.name, backend.id),
                    String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                if (File.Exists(flowmapDataFilePath) == false)
                {
                    string flowmapJson = controllerApi.GetFlowmapBackend(backend.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                    if (flowmapJson != String.Empty) FileIOHelper.saveFileToFolder(flowmapJson, flowmapDataFilePath);
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int extractFlowmapsBusinessTransactions(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<AppDRESTBusinessTransaction> entityList, string metricsFolderPath, long fromTimeUnix, long toTimeUnix, long differenceInMinutes, bool progressToConsole)
        {
            int j = 0;

            foreach (AppDRESTBusinessTransaction businessTransaction in entityList)
            {
                logger.Info("Retrieving flowmap for Application {0}, Tier {1}, Business Transaction {2}, From {3:o}, To {4:o}", jobTarget.Application, businessTransaction.tierName, businessTransaction.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To);

                string flowmapDataFilePath = Path.Combine(
                    metricsFolderPath,
                    BUSINESS_TRANSACTIONS_TYPE_SHORT,
                    getShortenedEntityNameForFileSystem(businessTransaction.tierName, businessTransaction.tierId),
                    getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id),
                    String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                if (File.Exists(flowmapDataFilePath) == false)
                {
                    string flowmapJson = controllerApi.GetFlowmapBusinessTransaction(jobTarget.ApplicationID, businessTransaction.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                    if (flowmapJson != String.Empty) FileIOHelper.saveFileToFolder(flowmapJson, flowmapDataFilePath);
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        #endregion

        #region Snapshot extraction functions

        private static int extractSnapshots(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, List<JToken> entityList, List<AppDRESTTier> tiersList, List<AppDRESTBusinessTransaction> businessTransactionsList, string snapshotsFolderPath, bool progressToConsole)
        {
            int j = 0;

            foreach (JToken snapshot in entityList)
            {
                // Only do first in chain
                if ((bool)snapshot["firstInChain"] == true)
                {
                    // Look up tiers and business transaction for this snapshot
                    AppDRESTTier tier = tiersList.Where<AppDRESTTier>(t => t.id == (int)snapshot["applicationComponentId"]).FirstOrDefault();
                    AppDRESTBusinessTransaction businessTransaction = businessTransactionsList.Where<AppDRESTBusinessTransaction>(t => t.id == (int)snapshot["businessTransactionId"]).FirstOrDefault();

                    if (tier != null && businessTransaction != null)
                    {
                        logger.Info("Retrieving snapshot for Application {0}, Tier {1}, Business Transaction {2}, RequestGUID {3}", jobTarget.Application, tier.name, businessTransaction.name, snapshot["requestGUID"]);

                        #region Prepare paths and variables

                        string snapshotTierFolderPath = Path.Combine(
                            snapshotsFolderPath,
                            getShortenedEntityNameForFileSystem(tier.name, tier.id));
                        string snapshotTierNameFilePath = Path.Combine(
                            snapshotTierFolderPath,
                            EXTRACT_ENTITY_NAME_FILE_NAME);
                        if (File.Exists(snapshotTierNameFilePath) == false)
                        {
                            FileIOHelper.writeObjectToFile(tier, snapshotTierNameFilePath);
                        }

                        string snapshotBusinessTransactionFolderPath = Path.Combine(
                            snapshotsFolderPath,
                            getShortenedEntityNameForFileSystem(tier.name, tier.id),
                            getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id));

                        string businessTransactionNameFilePath = Path.Combine(
                            snapshotBusinessTransactionFolderPath,
                            EXTRACT_ENTITY_NAME_FILE_NAME);

                        if (File.Exists(businessTransactionNameFilePath) == false)
                        {
                            FileIOHelper.writeObjectToFile(businessTransaction, businessTransactionNameFilePath);
                        }

                        DateTime snapshotTime = convertFromUnixTimestamp((long)snapshot["serverStartTime"]);

                        string snapshotFolderPath = Path.Combine(
                            snapshotsFolderPath,
                            getShortenedEntityNameForFileSystem(tier.name, tier.id),
                            getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id),
                            String.Format("{0:yyyyMMddHH}", snapshotTime),
                            userExperienceFolderNameMapping[snapshot["userExperience"].ToString()],
                            String.Format(SNAPSHOT_FOLDER_NAME, snapshotTime.ToString("yyyyMMddHHmmss"), snapshot["requestGUID"]));

                        // Must strip out the milliseconds, because the segment list retireval doesn't seem to like them in the datetimes
                        DateTime snapshotTimeFrom = snapshotTime.AddMinutes(-30).AddMilliseconds(snapshotTime.Millisecond * -1);
                        DateTime snapshotTimeTo = snapshotTime.AddMinutes(30).AddMilliseconds(snapshotTime.Millisecond * -1);

                        long fromTimeUnix = convertToUnixTimestamp(snapshotTimeFrom);
                        long toTimeUnix = convertToUnixTimestamp(snapshotTimeTo);
                        int differenceInMinutes = (int)(snapshotTimeTo - snapshotTimeFrom).TotalMinutes;

                        #endregion

                        #region Get Snapshot Flowmap

                        // Get snapshot flow map
                        string snapshotFlowmapDataFilePath = Path.Combine(snapshotFolderPath, EXTRACT_SNAPSHOT_FLOWMAP_FILE_NAME);

                        if (File.Exists(snapshotFlowmapDataFilePath) == false)
                        {
                            string snapshotFlowmapJson = controllerApi.GetFlowmapSnapshot(jobTarget.ApplicationID, (int)snapshot["businessTransactionId"], snapshot["requestGUID"].ToString(), fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (snapshotFlowmapJson != String.Empty) FileIOHelper.saveFileToFolder(snapshotFlowmapJson, snapshotFlowmapDataFilePath);
                        }

                        #endregion

                        #region Get List of Segments

                        // Get list of segments
                        string snapshotSegmentsDataFilePath = Path.Combine(snapshotFolderPath, EXTRACT_SNAPSHOT_SEGMENT_LIST_NAME);

                        if (File.Exists(snapshotSegmentsDataFilePath) == false)
                        {
                            string snapshotSegmentsJson = controllerApi.GetSnapshotSegments(snapshot["requestGUID"].ToString(), snapshotTimeFrom, snapshotTimeTo, differenceInMinutes);
                            if (snapshotSegmentsJson != String.Empty) FileIOHelper.saveFileToFolder(snapshotSegmentsJson, snapshotSegmentsDataFilePath);
                        }

                        #endregion

                        #region Get Details for Each Segment

                        JArray snapshotSegmentsList = FileIOHelper.loadJArrayFromFile(snapshotSegmentsDataFilePath);

                        if (snapshotSegmentsList != null)
                        {
                            // Get details for segment
                            foreach (JToken snapshotSegment in snapshotSegmentsList)
                            {
                                string snapshotSegmentDataFilePath = Path.Combine(snapshotFolderPath, String.Format(EXTRACT_SNAPSHOT_SEGMENT_DATA_FILE_NAME, snapshotSegment["id"]));

                                if (File.Exists(snapshotSegmentDataFilePath) == false)
                                {
                                    string snapshotSegmentJson = controllerApi.GetSnapshotSegmentDetails((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (snapshotSegmentJson != String.Empty) FileIOHelper.saveFileToFolder(snapshotSegmentJson, snapshotSegmentDataFilePath);
                                }
                            }

                            // Get errors for segment
                            foreach (JToken snapshotSegment in snapshotSegmentsList)
                            {
                                string snapshotSegmentErrorFilePath = Path.Combine(snapshotFolderPath, String.Format(EXTRACT_SNAPSHOT_SEGMENT_ERROR_FILE_NAME, snapshotSegment["id"]));

                                if (File.Exists(snapshotSegmentErrorFilePath) == false)
                                {
                                    string snapshotSegmentJson = controllerApi.GetSnapshotSegmentErrors((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (snapshotSegmentJson != String.Empty) FileIOHelper.saveFileToFolder(snapshotSegmentJson, snapshotSegmentErrorFilePath);
                                }
                            }

                            // Get call graphs for segment
                            foreach (JToken snapshotSegment in snapshotSegmentsList)
                            {
                                string snapshotSegmentCallGraphFilePath = Path.Combine(snapshotFolderPath, String.Format(EXTRACT_SNAPSHOT_SEGMENT_CALLGRAPH_FILE_NAME, snapshotSegment["id"]));

                                if (File.Exists(snapshotSegmentCallGraphFilePath) == false)
                                {
                                    string snapshotSegmentJson = controllerApi.GetSnapshotSegmentCallGraph((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (snapshotSegmentJson != String.Empty) FileIOHelper.saveFileToFolder(snapshotSegmentJson, snapshotSegmentCallGraphFilePath);
                                }
                            }
                        }

                        #endregion
                    }
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        #endregion


        #region Metric detail conversion functions

        private static bool fillFullRangeMetricEntityRow(EntityBase entityRow, string metricsEntityFolderPath, JobTimeRange jobTimeRange)
        {
            string fullRangeFileName = String.Format(EXTRACT_METRIC_FULL_FILE_NAME, jobTimeRange.From, jobTimeRange.To);

            logger.Info("Retrieving full range metrics for Entity Type {0} from path={1}, file {2}, From={3:o}, To={4:o}", entityRow.GetType().Name, metricsEntityFolderPath, fullRangeFileName, jobTimeRange.From, jobTimeRange.To);

            entityRow.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
            entityRow.From = jobTimeRange.From.ToLocalTime();
            entityRow.To = jobTimeRange.To.ToLocalTime();
            entityRow.FromUtc = jobTimeRange.From;
            entityRow.ToUtc = jobTimeRange.To;

            #region Read and convert metrics

            if (entityRow.MetricsIDs == null) { entityRow.MetricsIDs = new List<long>(3); }

            string metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_ART_SHORTNAME);
            string metricsDataFilePath = Path.Combine(metricsDataFolderPath, fullRangeFileName);
            string entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.ART = metricData[0].metricValues[0].value;
                        entityRow.TimeTotal = metricData[0].metricValues[0].sum;
                    }

                    if (File.Exists(entityMetricSummaryReportFilePath) == false)
                    {
                        List<MetricSummary> metricSummaries = convertMetricSummaryToTypedListForCSV(metricData[0], entityRow, jobTimeRange);
                        FileIOHelper.writeListToCSVFile(metricSummaries, new MetricSummaryMetricReportMap(), entityMetricSummaryReportFilePath, false);
                    }

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_CPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, fullRangeFileName);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.CPM = metricData[0].metricValues[0].value;
                        entityRow.Calls = metricData[0].metricValues[0].sum;
                    }

                    if (File.Exists(entityMetricSummaryReportFilePath) == false)
                    {
                        List<MetricSummary> metricSummaries = convertMetricSummaryToTypedListForCSV(metricData[0], entityRow, jobTimeRange);
                        FileIOHelper.writeListToCSVFile(metricSummaries, new MetricSummaryMetricReportMap(), entityMetricSummaryReportFilePath, false);
                    }

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, fullRangeFileName);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.EPM = metricData[0].metricValues[0].value;
                        entityRow.Errors = metricData[0].metricValues[0].sum;
                        entityRow.ErrorsPercentage = Math.Round((double)(double)entityRow.Errors / (double)entityRow.Calls * 100, 2);
                        if (Double.IsNaN(entityRow.ErrorsPercentage) == true) entityRow.ErrorsPercentage = 0;
                    }

                    if (File.Exists(entityMetricSummaryReportFilePath) == false)
                    {
                        List<MetricSummary> metricSummaries = convertMetricSummaryToTypedListForCSV(metricData[0], entityRow, jobTimeRange);
                        FileIOHelper.writeListToCSVFile(metricSummaries, new MetricSummaryMetricReportMap(), entityMetricSummaryReportFilePath, false);
                    }

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EXCPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, fullRangeFileName);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.EXCPM = metricData[0].metricValues[0].value;
                        entityRow.Exceptions = metricData[0].metricValues[0].sum;
                    }

                    if (File.Exists(entityMetricSummaryReportFilePath) == false)
                    {
                        List<MetricSummary> metricSummaries = convertMetricSummaryToTypedListForCSV(metricData[0], entityRow, jobTimeRange);
                        FileIOHelper.writeListToCSVFile(metricSummaries, new MetricSummaryMetricReportMap(), entityMetricSummaryReportFilePath, false);
                    }

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_HTTPEPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, fullRangeFileName);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.HTTPEPM = metricData[0].metricValues[0].value;
                        entityRow.HttpErrors = metricData[0].metricValues[0].sum;
                    }

                    if (File.Exists(entityMetricSummaryReportFilePath) == false)
                    {
                        List<MetricSummary> metricSummaries = convertMetricSummaryToTypedListForCSV(metricData[0], entityRow, jobTimeRange);
                        FileIOHelper.writeListToCSVFile(metricSummaries, new MetricSummaryMetricReportMap(), entityMetricSummaryReportFilePath, false);
                    }

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            if (entityRow.ART == 0 &&
                entityRow.CPM == 0 &&
                entityRow.EPM == 0 &&
                entityRow.EXCPM == 0 &&
                entityRow.HTTPEPM == 0)
            {
                entityRow.HasActivity = false;
            }
            else
            {
                entityRow.HasActivity = true;
            }

            #endregion

            updateEntityWithDeeplinks(entityRow, jobTimeRange);

            return true;
        }

        private static bool fillHourlyRangeMetricEntityRowAndConvertMetricsToCSV(EntityBase entityRow, string metricsEntityFolderPath, JobTimeRange jobTimeRange, int timeRangeIndex)
        {
            string hourRangeFileName = String.Format(EXTRACT_METRIC_HOUR_FILE_NAME, jobTimeRange.From, jobTimeRange.To);

            logger.Info("Retrieving hourly range metrics for Entity Type {0} from path={1}, file {2}, From={3:o}, To={4:o}", entityRow.GetType().Name, metricsEntityFolderPath, hourRangeFileName, jobTimeRange.From, jobTimeRange.To);

            entityRow.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
            entityRow.From = jobTimeRange.From.ToLocalTime();
            entityRow.To = jobTimeRange.To.ToLocalTime();
            entityRow.FromUtc = jobTimeRange.From;
            entityRow.ToUtc = jobTimeRange.To;

            #region Read and convert metrics

            if (entityRow.MetricsIDs == null) { entityRow.MetricsIDs = new List<long>(5); }

            string metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_ART_SHORTNAME);
            string metricsDataFilePath = Path.Combine(metricsDataFolderPath, hourRangeFileName);
            string entityMetricReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            bool appendRecordsToExistingFile = true;
            if (timeRangeIndex == 0) { appendRecordsToExistingFile = false; }

            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.ART = (long)Math.Round((double)((double)metricData[0].metricValues.Sum(mv => mv.sum) / (double)metricData[0].metricValues.Sum(mv => mv.count)), 0);
                        entityRow.TimeTotal = metricData[0].metricValues.Sum(mv => mv.sum);
                    }

                    List<MetricValue> metricValues = convertMetricValueToTypedListForCSV(metricData[0]);

                    FileIOHelper.writeListToCSVFile(metricValues, new MetricValueMetricReportMap(), entityMetricReportFilePath, appendRecordsToExistingFile);

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_CPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, hourRangeFileName);
            entityMetricReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.CPM = (long)Math.Round((double)((double)metricData[0].metricValues.Sum(mv => mv.sum) / (double)entityRow.Duration), 0);
                        entityRow.Calls = metricData[0].metricValues.Sum(mv => mv.sum);
                    }

                    List<MetricValue> metricValues = convertMetricValueToTypedListForCSV(metricData[0]);
                    FileIOHelper.writeListToCSVFile(metricValues, new MetricValueMetricReportMap(), entityMetricReportFilePath, appendRecordsToExistingFile);

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, hourRangeFileName);
            entityMetricReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.EPM = (long)Math.Round((double)((double)metricData[0].metricValues.Sum(mv => mv.sum) / (double)entityRow.Duration), 0);
                        entityRow.Errors = metricData[0].metricValues.Sum(mv => mv.sum);
                        entityRow.ErrorsPercentage = Math.Round((double)(double)entityRow.Errors / (double)entityRow.Calls * 100, 2);
                        if (Double.IsNaN(entityRow.ErrorsPercentage) == true) entityRow.ErrorsPercentage = 0;
                    }

                    List<MetricValue> metricValues = convertMetricValueToTypedListForCSV(metricData[0]);
                    FileIOHelper.writeListToCSVFile(metricValues, new MetricValueMetricReportMap(), entityMetricReportFilePath, appendRecordsToExistingFile);

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EXCPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, hourRangeFileName);
            entityMetricReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.EXCPM = (long)Math.Round((double)((double)metricData[0].metricValues.Sum(mv => mv.sum) / (double)entityRow.Duration), 0);
                        entityRow.Exceptions = metricData[0].metricValues.Sum(mv => mv.sum);
                    }

                    List<MetricValue> metricValues = convertMetricValueToTypedListForCSV(metricData[0]);
                    FileIOHelper.writeListToCSVFile(metricValues, new MetricValueMetricReportMap(), entityMetricReportFilePath, appendRecordsToExistingFile);

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_HTTPEPM_SHORTNAME);
            metricsDataFilePath = Path.Combine(metricsDataFolderPath, hourRangeFileName);
            entityMetricReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            if (File.Exists(metricsDataFilePath) == true)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.loadListOfObjectsFromFile<AppDRESTMetric>(metricsDataFilePath);
                if (metricData != null && metricData.Count > 0)
                {
                    if (metricData[0].metricValues.Count > 0)
                    {
                        entityRow.HTTPEPM = (long)Math.Round((double)((double)metricData[0].metricValues.Sum(mv => mv.sum) / (double)entityRow.Duration), 0);
                        entityRow.HttpErrors = metricData[0].metricValues.Sum(mv => mv.sum);
                    }

                    List<MetricValue> metricValues = convertMetricValueToTypedListForCSV(metricData[0]);
                    FileIOHelper.writeListToCSVFile(metricValues, new MetricValueMetricReportMap(), entityMetricReportFilePath, appendRecordsToExistingFile);

                    entityRow.MetricsIDs.Add(metricData[0].metricId);
                }
            }

            if (entityRow.ART == 0 &&
                entityRow.CPM == 0 &&
                entityRow.EPM == 0 &&
                entityRow.EXCPM == 0 &&
                entityRow.HTTPEPM == 0)
            {
                entityRow.HasActivity = false;
            }
            else
            {
                entityRow.HasActivity = true;
            }

            #endregion

            // Add link to the metrics
            updateEntityWithDeeplinks(entityRow, jobTimeRange);

            return true;
        }

        private static bool updateEntityWithDeeplinks(EntityBase entityRow)
        {
            return updateEntityWithDeeplinks(entityRow, null);
        }

        private static bool updateEntityWithDeeplinks(EntityBase entityRow, JobTimeRange jobTimeRange)
        {
            // Decide what kind of timerange
            string DEEPLINK_THIS_TIMERANGE = DEEPLINK_TIMERANGE_LAST_15_MINUTES;
            if (jobTimeRange != null)
            {
                long fromTimeUnix = convertToUnixTimestamp(jobTimeRange.From);
                long toTimeUnix = convertToUnixTimestamp(jobTimeRange.To);
                long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);
            }

            // Determine what kind of entity we are dealing with and adjust accordingly
            string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
            long entityIdForMetricBrowser = entityRow.ApplicationID;
            if (entityRow is EntityApplication)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is EntityTier)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entityRow.TierLink = String.Format(DEEPLINK_TIER, entityRow.Controller, entityRow.ApplicationID, entityRow.TierID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entityRow.TierID;
            }
            else if (entityRow is EntityNode)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entityRow.TierLink = String.Format(DEEPLINK_TIER, entityRow.Controller, entityRow.ApplicationID, entityRow.TierID, DEEPLINK_THIS_TIMERANGE);
                entityRow.NodeLink = String.Format(DEEPLINK_NODE, entityRow.Controller, entityRow.ApplicationID, entityRow.NodeID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entityRow.NodeID;
            }
            else if (entityRow is EntityBackend)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                ((EntityBackend)entityRow).BackendLink = String.Format(DEEPLINK_BACKEND, entityRow.Controller, entityRow.ApplicationID, ((EntityBackend)entityRow).BackendID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is EntityBusinessTransaction)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entityRow.TierLink = String.Format(DEEPLINK_TIER, entityRow.Controller, entityRow.ApplicationID, entityRow.TierID, DEEPLINK_THIS_TIMERANGE);
                ((EntityBusinessTransaction)entityRow).BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, entityRow.Controller, entityRow.ApplicationID, ((EntityBusinessTransaction)entityRow).BTID, DEEPLINK_THIS_TIMERANGE);
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entityRow.TierID;
            }
            else if (entityRow is EntityServiceEndpoint)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entityRow.TierLink = String.Format(DEEPLINK_TIER, entityRow.Controller, entityRow.ApplicationID, entityRow.TierID, DEEPLINK_THIS_TIMERANGE);
                ((EntityServiceEndpoint)entityRow).SEPLink = String.Format(DEEPLINK_SERVICE_ENDPOINT, entityRow.Controller, entityRow.ApplicationID, entityRow.TierID, ((EntityServiceEndpoint)entityRow).SEPID, DEEPLINK_THIS_TIMERANGE);
            }
            else if (entityRow is EntityError)
            {
                entityRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, entityRow.Controller, DEEPLINK_THIS_TIMERANGE);
                entityRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, entityRow.Controller, entityRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                entityRow.TierLink = String.Format(DEEPLINK_TIER, entityRow.Controller, entityRow.ApplicationID, entityRow.TierID, DEEPLINK_THIS_TIMERANGE);
                ((EntityError)entityRow).ErrorLink = String.Format(DEEPLINK_ERROR, entityRow.Controller, entityRow.ApplicationID, ((EntityError)entityRow).ErrorID, DEEPLINK_THIS_TIMERANGE);
            }

            if (entityRow.MetricsIDs != null && entityRow.MetricsIDs.Count > 0)
            {
                StringBuilder sb = new StringBuilder(128);
                foreach (int metricID in entityRow.MetricsIDs)
                {
                    sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                    sb.Append(",");
                }
                sb.Remove(sb.Length - 1, 1);
                entityRow.MetricLink = String.Format(DEEPLINK_METRIC, entityRow.Controller, entityRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
            }

            return true;
        }

        private static void updateEntitiesWithReportDetailLinksApplication(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityApplication> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static void updateEntitiesWithReportDetailLinksTiers(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityTier> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static void updateEntitiesWithReportDetailLinksNodes(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityNode> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static void updateEntitiesWithReportDetailLinksBackends(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityBackend> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static void updateEntitiesWithReportDetailLinksBusinessTransactions(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityBusinessTransaction> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static void updateEntitiesWithReportDetailLinksServiceEndpoints(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityServiceEndpoint> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static void updateEntitiesWithReportDetailLinksErrors(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityError> entityList)
        {
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            for (int i = 0; i < entityList.Count; i++)
            {
                EntityBase entityRow = entityList[i];
                entityRow.DetailLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, entityRow).Substring(programOptions.OutputJobFolderPath.Length + 1));
            }
        }

        private static List<MetricValue> convertMetricValueToTypedListForCSV(AppDRESTMetric metricValueObject)
        {
            List<MetricValue> metricValues = new List<MetricValue>(metricValueObject.metricValues.Count);
            foreach (AppDRESTMetricValue mv in metricValueObject.metricValues)
            {
                MetricValue metricValue = new MetricValue();
                metricValue.EventTimeStampUtc = convertFromUnixTimestamp(mv.startTimeInMillis);
                metricValue.EventTimeStamp = metricValue.EventTimeStampUtc.ToLocalTime();
                metricValue.EventTime = metricValue.EventTimeStamp;
                metricValue.Count = mv.count;
                metricValue.Min = mv.min;
                metricValue.Max = mv.max;
                metricValue.Occurences = mv.occurrences;
                metricValue.Sum = mv.sum;
                metricValue.Value = mv.value;

                metricValue.MetricID = metricValueObject.metricId;
                switch (metricValueObject.frequency)
                {
                    case "SIXTY_MIN":
                        {
                            metricValue.MetricResolution = MetricResolution.SIXTY_MIN;
                            break;
                        }
                    case "TEN_MIN":
                        {
                            metricValue.MetricResolution = MetricResolution.TEN_MIN;
                            break;
                        }
                    case "ONE_MIN":
                        {
                            metricValue.MetricResolution = MetricResolution.ONE_MIN;
                            break;
                        }
                    default:
                        {
                            metricValue.MetricResolution = MetricResolution.ONE_MIN;
                            break;
                        }
                }
                metricValues.Add(metricValue);
            }

            return metricValues;
        }

        private static List<MetricSummary> convertMetricSummaryToTypedListForCSV(AppDRESTMetric metricValueObject, EntityBase entityRow, JobTimeRange jobTimeRange)
        {
            List<MetricSummary> metricSummaries = new List<MetricSummary>();
            metricSummaries.Add(new MetricSummary() {
                PropertyName = "Controller",
                PropertyValue = entityRow.Controller,
                Link = entityRow.ControllerLink });
            metricSummaries.Add(new MetricSummary() {
                PropertyName = "Application",
                PropertyValue = entityRow.ApplicationName,
                Link = entityRow.ApplicationLink });

            string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
            long entityIdForMetricBrowser = entityRow.ApplicationID;
            if (entityRow is EntityApplication)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Application" });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
            }
            else if (entityRow is EntityTier)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Tier" });
                metricSummaries.Add(new MetricSummary() {
                    PropertyName = "Tier",
                    PropertyValue = entityRow.TierName,
                    Link = entityRow.TierLink });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "TierID", PropertyValue = entityRow.TierID });
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entityRow.TierID;
            }
            else if (entityRow is EntityNode)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Node" });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Tier",
                    PropertyValue = entityRow.TierName,
                    Link = entityRow.TierLink
                });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Node",
                    PropertyValue = entityRow.NodeName,
                    Link = entityRow.NodeLink
                });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "TierID", PropertyValue = entityRow.TierID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "NodeID", PropertyValue = entityRow.NodeID });
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entityRow.NodeID;
            }
            else if (entityRow is EntityBackend)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Backend" });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Tier",
                    PropertyValue = entityRow.TierName,
                    Link = entityRow.TierLink
                });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Backend",
                    PropertyValue = ((EntityBackend)entityRow).BackendName,
                    Link = ((EntityBackend)entityRow).BackendLink
                });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "TierID", PropertyValue = entityRow.TierID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "BackendID", PropertyValue = ((EntityBackend)entityRow).BackendID });
            }
            else if (entityRow is EntityBusinessTransaction)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Business Transaction" });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Tier",
                    PropertyValue = entityRow.TierName,
                    Link = entityRow.TierLink
                });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Business Transaction",
                    PropertyValue = ((EntityBusinessTransaction)entityRow).BTName,
                    Link = ((EntityBusinessTransaction)entityRow).BTLink
                });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "TierID", PropertyValue = entityRow.TierID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "BTID", PropertyValue = ((EntityBusinessTransaction)entityRow).BTID });
                deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                entityIdForMetricBrowser = entityRow.TierID;
            }
            else if (entityRow is EntityServiceEndpoint)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Service Endpoint" });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Tier",
                    PropertyValue = entityRow.TierName,
                    Link = entityRow.TierLink
                });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Service Endpoint",
                    PropertyValue = ((EntityServiceEndpoint)entityRow).SEPName,
                    Link = ((EntityServiceEndpoint)entityRow).SEPLink
                });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "TierID", PropertyValue = entityRow.TierID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "SEPID", PropertyValue = ((EntityServiceEndpoint)entityRow).SEPID });
            }
            else if (entityRow is EntityError)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "EntityType", PropertyValue = "Error" });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Tier",
                    PropertyValue = entityRow.TierName,
                    Link = entityRow.TierLink
                });
                metricSummaries.Add(new MetricSummary()
                {
                    PropertyName = "Error",
                    PropertyValue = ((EntityError)entityRow).ErrorName,
                    Link = ((EntityError)entityRow).ErrorLink
                });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ApplicationID", PropertyValue = entityRow.ApplicationID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "TierID", PropertyValue = entityRow.TierID });
                metricSummaries.Add(new MetricSummary() { PropertyName = "ErrorID", PropertyValue = ((EntityError)entityRow).ErrorID });
            }

            // Decide what kind of timerange
            string DEEPLINK_THIS_TIMERANGE = DEEPLINK_TIMERANGE_LAST_15_MINUTES;
            if (jobTimeRange != null)
            {
                long fromTimeUnix = convertToUnixTimestamp(jobTimeRange.From);
                long toTimeUnix = convertToUnixTimestamp(jobTimeRange.To);
                long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);
            }

            metricSummaries.Add(new MetricSummary()
            {
                PropertyName = "Metric ID",
                PropertyValue = metricValueObject.metricId,
                Link = String.Format(DEEPLINK_METRIC, entityRow.Controller, entityRow.ApplicationID, String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricValueObject.metricId), DEEPLINK_THIS_TIMERANGE)
            });

            // Name of the metric is always the last one in the metric path
            string[] metricPathComponents = metricValueObject.metricPath.Split('|');
            string metricName = metricPathComponents[metricPathComponents.Length - 1];
            MetricSummary metricNameMetricSummary = new MetricSummary() { PropertyName = "Metric Name", PropertyValue = metricName };
            metricSummaries.Add(metricNameMetricSummary);
            metricSummaries.Add(new MetricSummary() { PropertyName = "Metric Name (Short)", PropertyValue = metricNameToShortMetricNameMapping[metricNameMetricSummary.PropertyValue.ToString()] });
            metricSummaries.Add(new MetricSummary() { PropertyName = "Metric Name (Full)", PropertyValue = metricValueObject.metricName });
            metricSummaries.Add(new MetricSummary() { PropertyName = "Metric Path", PropertyValue = metricValueObject.metricPath });

            // Only the metrics with Average Response Time (ms) are times            
            // As long as we are not in Application Infrastructure Performance area
            if (metricName.IndexOf(METRIC_TIME_MS) > 0)
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "Rollup Type", PropertyValue = MetricType.Duration.ToString() });
            }
            else
            {
                metricSummaries.Add(new MetricSummary() { PropertyName = "Rollup Type", PropertyValue = MetricType.Count.ToString() });
            }

            return metricSummaries;
        }

        #endregion

        #region Flowmap detail conversion functions

        private static void convertFlowmapApplication(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, EntityApplication applicationRow, string metricsFolderPath)
        {
            string metricsEntityFolderPath = Path.Combine(
                metricsFolderPath,
                APPLICATION_TYPE_SHORT);

            string flowmapDataFilePath = Path.Combine(
                metricsEntityFolderPath,
                String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

            JObject flowmapData = FileIOHelper.loadJObjectFromFile(flowmapDataFilePath);
            if (flowmapData == null)
            {
                return;
            }

            long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
            long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowRows = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowRows = new List<ActivityFlow>(flowmapEntities.Count + flowmapEntityConnections.Count);

                // Process each of the individual Tiers, Backends and Applications as individual icons on the flow map
                foreach (JToken entity in flowmapEntities)
                {
                    ActivityFlow activityFlowRow = new ActivityFlow();
                    activityFlowRow.MetricsIDs = new List<int>(3);

                    activityFlowRow.Controller = applicationRow.Controller;
                    activityFlowRow.ApplicationName = applicationRow.ApplicationName;
                    activityFlowRow.ApplicationID = applicationRow.ApplicationID;

                    activityFlowRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRow.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRow.Controller, activityFlowRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRow.Duration = (int)(jobConfiguration.Input.ExpandedTimeRange.To - jobConfiguration.Input.ExpandedTimeRange.From).Duration().TotalMinutes;
                    activityFlowRow.From = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime();
                    activityFlowRow.To = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime();
                    activityFlowRow.FromUtc = jobConfiguration.Input.ExpandedTimeRange.From;
                    activityFlowRow.ToUtc = jobConfiguration.Input.ExpandedTimeRange.To;

                    activityFlowRow.CallDirection = "Total";

                    activityFlowRow.FromEntityID = (int)entity["idNum"];
                    activityFlowRow.FromName = entity["name"].ToString();

                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRow.ApplicationID;
                    switch (entity["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRow.FromEntityID;
                            activityFlowRow.CallType = "Total";
                            activityFlowRow.FromType = "Tier";
                            activityFlowRow.FromLink = String.Format(DEEPLINK_TIER, activityFlowRow.Controller, activityFlowRow.ApplicationID, activityFlowRow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRow.CallType = "Total";
                            activityFlowRow.FromType = "Backend";
                            activityFlowRow.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRow.Controller, activityFlowRow.ApplicationID, activityFlowRow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRow.CallType = "Total";
                            activityFlowRow.FromType = "Application";
                            activityFlowRow.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRow.Controller, activityFlowRow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRow.CallType = entity["entityType"].ToString();
                            activityFlowRow.FromType = "Unknown";
                            break;
                    }

                    //activityFlowRow.ToName = activityFlowRow.FromName;
                    //activityFlowRow.ToType= activityFlowRow.FromType;
                    activityFlowRow.ToEntityID = activityFlowRow.FromEntityID;
                    //activityFlowRow.ToLink = activityFlowRow.FromLink;

                    activityFlowRow.ART = (long)entity["stats"]["averageResponseTime"]["metricValue"];
                    activityFlowRow.CPM = (long)entity["stats"]["callsPerMinute"]["metricValue"];
                    activityFlowRow.EPM = (long)entity["stats"]["errorsPerMinute"]["metricValue"];
                    activityFlowRow.Calls = (long)entity["stats"]["numberOfCalls"]["metricValue"];
                    activityFlowRow.Errors = (long)entity["stats"]["numberOfErrors"]["metricValue"];

                    if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                    if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                    if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                    if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                    if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                    activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                    if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                    activityFlowRow.MetricsIDs.Add((int)entity["stats"]["averageResponseTime"]["metricId"]);
                    activityFlowRow.MetricsIDs.Add((int)entity["stats"]["callsPerMinute"]["metricId"]);
                    activityFlowRow.MetricsIDs.Add((int)entity["stats"]["errorsPerMinute"]["metricId"]);
                    activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                    if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder(128);
                        foreach (int metricID in activityFlowRow.MetricsIDs)
                        {
                            sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                            sb.Append(",");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                    }

                    activityFlowRows.Add(activityFlowRow);
                }

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<int>(3);

                    activityFlowRowTemplate.Controller = applicationRow.Controller;
                    activityFlowRowTemplate.ApplicationName = applicationRow.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = applicationRow.ApplicationID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobConfiguration.Input.ExpandedTimeRange.To - jobConfiguration.Input.ExpandedTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobConfiguration.Input.ExpandedTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobConfiguration.Input.ExpandedTimeRange.To;

                    activityFlowRowTemplate.CallDirection = "Exit";

                    activityFlowRowTemplate.FromEntityID = (int)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = "Tier";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.FromType = "Backend";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = "Application";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (int)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            activityFlowRowTemplate.ToType = "Tier";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.ToType = "Backend";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            activityFlowRowTemplate.ToType = "Application";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (((bool)entityConnectionStat["exitPointCall"]["synchronous"]) == false)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                        activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                        activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                        activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"];
                        activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"];

                        if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                        if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                        if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                        activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["averageResponseTime"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["callsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(128);
                            foreach (int metricID in activityFlowRow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowRows.Add(activityFlowRow);
                    }
                }
            }

            // Sort them
            activityFlowRows = activityFlowRows.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            string activityGridReportFileName = Path.Combine(
                metricsEntityFolderPath,
                CONVERT_ACTIVITY_GRID_FILE_NAME);

            FileIOHelper.writeListToCSVFile(activityFlowRows, new ActivityFlowReportMap(), activityGridReportFileName);

            return;
        }

        private static void convertFlowmapTier(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, EntityTier tierRow, string metricsFolderPath)
        {
            string metricsEntityFolderPath = Path.Combine(
                metricsFolderPath,
                TIERS_TYPE_SHORT,
                getShortenedEntityNameForFileSystem(tierRow.TierName, tierRow.TierID));

            string flowmapDataFilePath = Path.Combine(
                metricsEntityFolderPath,
                String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

            JObject flowmapData = FileIOHelper.loadJObjectFromFile(flowmapDataFilePath);
            if (flowmapData == null)
            {
                return;
            }

            long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
            long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowRows = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowRows = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // For Tiers, not going to process individual entities, but only connecting lines

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<int>(3);

                    activityFlowRowTemplate.Controller = tierRow.Controller;
                    activityFlowRowTemplate.ApplicationName = tierRow.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = tierRow.ApplicationID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobConfiguration.Input.ExpandedTimeRange.To - jobConfiguration.Input.ExpandedTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobConfiguration.Input.ExpandedTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobConfiguration.Input.ExpandedTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (int)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = "Tier";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.FromType = "Backend";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = "Application";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (int)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            activityFlowRowTemplate.ToType = "Tier";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.ToType = "Backend";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            activityFlowRowTemplate.ToType = "Application";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    if (activityFlowRowTemplate.FromEntityID == tierRow.TierID)
                    {
                        activityFlowRowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowRowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (((bool)entityConnectionStat["exitPointCall"]["synchronous"]) == false)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                        activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                        activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                        activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"];
                        activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"];

                        if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                        if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                        if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                        activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["averageResponseTime"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["callsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(128);
                            foreach (int metricID in activityFlowRow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowRows.Add(activityFlowRow);
                    }
                }
            }

            // Sort them
            activityFlowRows = activityFlowRows.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            string activityGridReportFileName = Path.Combine(
                metricsEntityFolderPath,
                CONVERT_ACTIVITY_GRID_FILE_NAME);

            FileIOHelper.writeListToCSVFile(activityFlowRows, new ActivityFlowReportMap(), activityGridReportFileName);
        }

        private static void convertFlowmapNode(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, EntityNode nodeRow, string metricsFolderPath)
        {
            string metricsEntityFolderPath = Path.Combine(
                metricsFolderPath,
                NODES_TYPE_SHORT,
                getShortenedEntityNameForFileSystem(nodeRow.TierName, nodeRow.TierID),
                getShortenedEntityNameForFileSystem(nodeRow.NodeName, nodeRow.NodeID));

            string flowmapDataFilePath = Path.Combine(
                metricsEntityFolderPath,
                String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

            JObject flowmapData = FileIOHelper.loadJObjectFromFile(flowmapDataFilePath);
            if (flowmapData == null)
            {
                return;
            }

            long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
            long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowRows = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowRows = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // For Nodes, not going to process individual entities, but only connecting lines

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<int>(3);

                    activityFlowRowTemplate.Controller = nodeRow.Controller;
                    activityFlowRowTemplate.ApplicationName = nodeRow.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = nodeRow.ApplicationID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobConfiguration.Input.ExpandedTimeRange.To - jobConfiguration.Input.ExpandedTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobConfiguration.Input.ExpandedTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobConfiguration.Input.ExpandedTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (int)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_NODE:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = "Node";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_NODE, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = "Tier";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.FromType = "Backend";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = "Application";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (int)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case "APPLICATION_COMPONENT_NODE":
                            activityFlowRowTemplate.ToType = "Node";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_NODE, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_TIER:
                            activityFlowRowTemplate.ToType = "Tier";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.ToType = "Backend";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            activityFlowRowTemplate.ToType = "Application";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    // Haven't seen the incoming calls on the flowmap for Nodes. But maybe?
                    if (activityFlowRowTemplate.FromEntityID == nodeRow.NodeID)
                    {
                        activityFlowRowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowRowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (((bool)entityConnectionStat["exitPointCall"]["synchronous"]) == false)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                        activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                        activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                        activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"];
                        activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"];

                        if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                        if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                        if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                        activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["averageResponseTime"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["callsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(128);
                            foreach (int metricID in activityFlowRow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowRows.Add(activityFlowRow);
                    }
                }
            }

            // Sort them
            activityFlowRows = activityFlowRows.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            string activityGridReportFileName = Path.Combine(
                metricsEntityFolderPath,
                CONVERT_ACTIVITY_GRID_FILE_NAME);

            FileIOHelper.writeListToCSVFile(activityFlowRows, new ActivityFlowReportMap(), activityGridReportFileName);
        }

        private static void convertFlowmapsBusinessTransaction(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, EntityBusinessTransaction businessTransactionRow, string metricsFolderPath)
        {
            string metricsEntityFolderPath = Path.Combine(
                metricsFolderPath,
                BUSINESS_TRANSACTIONS_TYPE_SHORT,
                getShortenedEntityNameForFileSystem(businessTransactionRow.TierName, businessTransactionRow.TierID),
                getShortenedEntityNameForFileSystem(businessTransactionRow.BTName, businessTransactionRow.BTID));

            string flowmapDataFilePath = Path.Combine(
                metricsEntityFolderPath,
                String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

            JObject flowmapData = FileIOHelper.loadJObjectFromFile(flowmapDataFilePath);
            if (flowmapData == null)
            {
                return;
            }

            long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
            long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowRows = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowRows = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // Controller shows a pretty complex grid view for jumps that continue from other tiers.
                // I couldn't figure out how the JSON is converted into that
                // For Business Transactions, not going to process individual entities, but only connecting lines

                // Assume that the first node is the 
                JObject startTier = (JObject)flowmapEntities.Where(e => (bool)e["startComponent"] == true).FirstOrDefault();

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<int>(3);

                    activityFlowRowTemplate.Controller = businessTransactionRow.Controller;
                    activityFlowRowTemplate.ApplicationName = businessTransactionRow.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = businessTransactionRow.ApplicationID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobConfiguration.Input.ExpandedTimeRange.To - jobConfiguration.Input.ExpandedTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobConfiguration.Input.ExpandedTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobConfiguration.Input.ExpandedTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (int)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = "Tier";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.FromType = "Backend";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = "Application";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (int)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            activityFlowRowTemplate.ToType = "Tier";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.ToType = "Backend";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            activityFlowRowTemplate.ToType = "Application";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    // Haven't seen the incoming calls on the flowmap for Nodes. But maybe?
                    if (startTier != null)
                    {
                        if (activityFlowRowTemplate.FromEntityID == (int)startTier["idNum"])
                        {
                            activityFlowRowTemplate.CallDirection = "FirstHop";
                        }
                        else
                        {
                            activityFlowRowTemplate.CallDirection = "SubsequentHop";
                        }
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (((bool)entityConnectionStat["exitPointCall"]["synchronous"]) == false)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                        activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                        activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                        activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"];
                        activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"];

                        if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                        if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                        if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                        activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["averageResponseTime"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["callsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(128);
                            foreach (int metricID in activityFlowRow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowRows.Add(activityFlowRow);
                    }
                }
            }

            // Sort them
            activityFlowRows = activityFlowRows.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            string activityGridReportFileName = Path.Combine(
                metricsEntityFolderPath,
                CONVERT_ACTIVITY_GRID_FILE_NAME);

            FileIOHelper.writeListToCSVFile(activityFlowRows, new ActivityFlowReportMap(), activityGridReportFileName);
        }

        private static void convertFlowmapBackend(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, EntityBackend backendRow, string metricsFolderPath)
        {
            string metricsEntityFolderPath = Path.Combine(
                metricsFolderPath,
                BACKENDS_TYPE_SHORT,
                getShortenedEntityNameForFileSystem(backendRow.BackendName, backendRow.BackendID));

            string flowmapDataFilePath = Path.Combine(
                metricsEntityFolderPath,
                String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

            JObject flowmapData = FileIOHelper.loadJObjectFromFile(flowmapDataFilePath);
            if (flowmapData == null)
            {
                return;
            }

            long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
            long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowRows = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowRows = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // We don't display grid for Backends. But it is quite similar to Tier view
                // For Backends, not going to process individual entities, but only connecting lines

                // Process each call between Tiers, Tiers and Backends
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<int>(3);

                    activityFlowRowTemplate.Controller = backendRow.Controller;
                    activityFlowRowTemplate.ApplicationName = backendRow.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = backendRow.ApplicationID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobConfiguration.Input.ExpandedTimeRange.To - jobConfiguration.Input.ExpandedTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobConfiguration.Input.ExpandedTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobConfiguration.Input.ExpandedTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (int)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = "Tier";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.FromType = "Backend";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = "Application";
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (int)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (int)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_TIER:
                            activityFlowRowTemplate.ToType = "Tier";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_BACKEND:
                            activityFlowRowTemplate.ToType = "Backend";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_APPLICATION:
                            activityFlowRowTemplate.ToType = "Application";
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    if (activityFlowRowTemplate.FromEntityID == backendRow.BackendID)
                    {
                        activityFlowRowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowRowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (((bool)entityConnectionStat["exitPointCall"]["synchronous"]) == false)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                        activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                        activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                        activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"];
                        activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"];

                        if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                        if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                        if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                        activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["averageResponseTime"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["callsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.Add((int)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(128);
                            foreach (int metricID in activityFlowRow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowRows.Add(activityFlowRow);
                    }
                }
            }

            // Sort them
            activityFlowRows = activityFlowRows.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            string activityGridReportFileName = Path.Combine(
                metricsEntityFolderPath,
                CONVERT_ACTIVITY_GRID_FILE_NAME);

            FileIOHelper.writeListToCSVFile(activityFlowRows, new ActivityFlowReportMap(), activityGridReportFileName);
        }

        #endregion


        #region Entity report function

        private static void adjustColumnsOfEntityRowTableInEntitiesReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            if (entityType == APPLICATION_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
            }
            else if (entityType == TIERS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["AgentType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
            }
            else if (entityType == NODES_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationName"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["AgentType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["AgentVersion"].Position + 1).AutoFit();
                sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["MachineAgentVersion"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["NodeLink"].Position + 1).AutoFit();
            }
            else if (entityType == BACKENDS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationName"].Position + 1).AutoFit();
                sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["Prop1Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["Prop2Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["Prop3Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["Prop4Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["Prop5Name"].Position + 1).AutoFit();
                sheet.Column(table.Columns["Prop1Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop2Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop3Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop4Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop5Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["BackendLink"].Position + 1).AutoFit();
            }
            else if (entityType == BUSINESS_TRANSACTIONS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationName"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["BTLink"].Position + 1).AutoFit();
            }
            else if (entityType == SERVICE_ENDPOINTS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationName"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["SEPLink"].Position + 1).AutoFit();
            }
            else if (entityType == ERRORS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationName"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["HttpCode"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ErrorDepth"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ErrorLevel1"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel2"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel3"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel4"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel5"].Position + 1).Width = 20;
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ErrorLink"].Position + 1).AutoFit();
            }
        }

        #endregion


        #region Entity metric detail report functions

        private static int reportMetricDetailApplication(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, EntityApplication applicationRow)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
            fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, APPLICATION_TYPE_SHORT, applicationRow);

            // Report file
            string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, applicationRow);

            finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);

            return 1;
        }

        private static int reportMetricDetailTiers(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityTier> entityList, bool progressToConsole)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            int j = 0;

            foreach (EntityTier tierRow in entityList)
            {
                //string metricsEntityFolderPath = Path.Combine(
                //    metricsFolderPath,
                //    TIERS_TYPE_SHORT,
                //    getShortenedEntityNameForFileSystem(tierRow.TierName, tierRow.TierID));

                ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
                fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, TIERS_TYPE_SHORT, tierRow);

                // Report file
                string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, tierRow);

                finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);
                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int reportMetricDetailNodes(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityNode> entityList, bool progressToConsole)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            int j = 0;

            foreach (EntityNode nodeRow in entityList)
            {
                ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
                fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, NODES_TYPE_SHORT, nodeRow);

                // Report file
                string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, nodeRow);

                finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);
                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int reportMetricDetailBackends(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityBackend> entityList, bool progressToConsole)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            int j = 0;

            foreach (EntityBackend backendRow in entityList)
            {
                ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
                fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, BACKENDS_TYPE_SHORT, backendRow);

                // Report file
                string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, backendRow);

                finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);
                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int reportMetricDetailBusinessTransactions(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityBusinessTransaction> entityList, bool progressToConsole)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            int j = 0;

            foreach (EntityBusinessTransaction businessTransactionRow in entityList)
            {
                ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
                fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, BUSINESS_TRANSACTIONS_TYPE_SHORT, businessTransactionRow);

                // Report file
                string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, businessTransactionRow);

                finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);
                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int reportMetricDetailServiceEndpoints(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityServiceEndpoint> entityList, bool progressToConsole)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            int j = 0;

            foreach (EntityServiceEndpoint serviceEndpointRow in entityList)
            {
                ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
                fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, SERVICE_ENDPOINTS_TYPE_SHORT, serviceEndpointRow);

                // Report file
                string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, serviceEndpointRow);

                finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);
                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static int reportMetricDetailErrors(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, List<EntityError> entityList, bool progressToConsole)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string reportsFolderPath = Path.Combine(applicationFolderPath, REPORTS_FOLDER_NAME);

            #endregion

            int j = 0;

            foreach (EntityError errorRow in entityList)
            {
                ExcelPackage excelEntitiesDetail = createIndividualEntityMetricReportTemplate(programOptions, jobConfiguration, jobTarget);
                fillIndividualEntityMetricReportForEntity(programOptions, jobConfiguration, jobTarget, excelEntitiesDetail, ERRORS_TYPE_SHORT, errorRow);

                // Report file
                string reportFilePath = getEntityMetricReportFilePath(programOptions, jobConfiguration, jobTarget, reportsFolderPath, errorRow);

                finalizeAndSaveIndividualEntityMetricReport(excelEntitiesDetail, reportFilePath);
                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return entityList.Count;
        }

        private static string getEntityMetricReportFilePath(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, string reportsFolderPath, EntityBase entityRow)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entityRow is EntityApplication)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_APPLICATION_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    APPLICATION_TYPE_SHORT,
                    reportFileName);
            }
            else if (entityRow is EntityTier)
            {
                EntityTier tierRow = (EntityTier)entityRow;
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID),
                    getShortenedEntityNameForFileSystem(tierRow.TierName, tierRow.TierID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    TIERS_TYPE_SHORT,
                    reportFileName);
            }
            else if (entityRow is EntityNode)
            {
                EntityNode nodeRow = (EntityNode)entityRow;
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID),
                    getShortenedEntityNameForFileSystem(nodeRow.NodeName, nodeRow.NodeID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    NODES_TYPE_SHORT,
                    reportFileName);
            }
            else if (entityRow is EntityBackend)
            {
                EntityBackend backendRow = (EntityBackend)entityRow;
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID),
                    getShortenedEntityNameForFileSystem(backendRow.BackendName, backendRow.BackendID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    BACKENDS_TYPE_SHORT,
                    reportFileName);
            }
            else if (entityRow is EntityBusinessTransaction)
            {
                EntityBusinessTransaction businessTransactionRow = (EntityBusinessTransaction)entityRow;
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID),
                    getShortenedEntityNameForFileSystem(businessTransactionRow.BTName, businessTransactionRow.BTID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    BUSINESS_TRANSACTIONS_TYPE_SHORT,
                    reportFileName);
            }
            else if (entityRow is EntityServiceEndpoint)
            {
                EntityServiceEndpoint serviceEndpointRow = (EntityServiceEndpoint)entityRow;
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID),
                    getShortenedEntityNameForFileSystem(serviceEndpointRow.SEPName, serviceEndpointRow.SEPID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    SERVICE_ENDPOINTS_TYPE_SHORT,
                    reportFileName);
            }
            else if (entityRow is EntityError)
            {
                EntityError errorRow = (EntityError)entityRow;
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    programOptions.JobName,
                    jobConfiguration.Input.ExpandedTimeRange.From,
                    jobConfiguration.Input.ExpandedTimeRange.To,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entityRow.ApplicationName, entityRow.ApplicationID),
                    getShortenedEntityNameForFileSystem(errorRow.ErrorName, errorRow.ErrorID));
                reportFilePath = Path.Combine(
                    reportsFolderPath,
                    ERRORS_TYPE_SHORT,
                    reportFileName);
            }

            return reportFilePath;
        }

        private static ExcelPackage createIndividualEntityMetricReportTemplate(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget)
        {
            #region Target step variables

            // Various folders
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));

            // Report files
            string controllerReportFilePath = Path.Combine(controllerFolderPath, CONVERT_ENTITY_CONTROLLER_FILE_NAME);

            #endregion

            #region Prepare the report package

            // Prepare package
            ExcelPackage excelEntitiesDetail = new ExcelPackage();
            excelEntitiesDetail.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
            excelEntitiesDetail.Workbook.Properties.Title = "AppDynamics DEXTER Entities Detail Report";
            excelEntitiesDetail.Workbook.Properties.Subject = programOptions.JobName;

            excelEntitiesDetail.Workbook.Properties.Comments = String.Format("Targets={0}\r\nFrom={1:o}\r\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

            #endregion

            #region Parameters sheet

            // Parameters sheet
            ExcelWorksheet sheet = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

            var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
            hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
            hyperLinkStyle.Style.Font.Color.SetColor(Color.Blue);

            int l = 1;
            sheet.Cells[l, 1].Value = "Table of Contents";
            sheet.Cells[l, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            l++; l++;
            sheet.Cells[l, 1].Value = "AppDynamics DEXTER Entities Detail Report";
            l++; l++;
            sheet.Cells[l, 1].Value = "From";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.From.ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "To";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.TimeRange.To.ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Expanded From (UTC)";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Expanded From (Local)";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime().ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Expanded To (UTC)";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Expanded To (Local)";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime().ToString("G");
            l++;
            sheet.Cells[l, 1].Value = "Number of Hours Intervals";
            sheet.Cells[l, 2].Value = jobConfiguration.Input.HourlyTimeRanges.Count;

            sheet.Column(1).AutoFit();
            sheet.Column(2).AutoFit();

            #endregion

            #region TOC sheet

            // Navigation sheet with link to other sheets
            sheet = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

            #endregion

            #region Controller sheet

            sheet = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_CONTROLLERS_LIST);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheet.View.FreezePanes(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT + 1, 1);

            ExcelRangeBase range = readCSVFileIntoExcelRange(controllerReportFilePath, 0, sheet, REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT, 1);
            if (range != null)
            {
                ExcelTable table = sheet.Tables.Add(range, REPORT_ENTITY_DETAILS_TABLE_CONTROLLERS);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Controller"].Position + 1).AutoFit();
                sheet.Column(table.Columns["UserName"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
            }

            #endregion

            return excelEntitiesDetail;
        }

        private static void fillIndividualEntityMetricReportForEntity(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget, ExcelPackage excelEntitiesDetail, string entityType, EntityBase entityRow)
        {
            #region Target step variables

            // Various folders and files
            string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
            string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
            string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
            string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);
            string eventsFolderPath = Path.Combine(applicationFolderPath, EVENTS_FOLDER_NAME);
            string metricsEntityFolderTypePath = Path.Combine(metricsFolderPath, entityType);
            string metricsEntityFolderPath = String.Empty;

            // Metric paths and files
            string entityFullRangeReportFilePath = String.Empty;
            string entityHourlyRangeReportFilePath = String.Empty;
            string metricsDataFolderPath = String.Empty;
            string entityMetricSummaryReportFilePath = String.Empty;
            string entityMetricValuesReportFilePath = String.Empty;
            string entityName = String.Empty;
            long entityID = -1;
            string entityNameForDisplay = String.Empty;
            string entityTypeForDisplay = String.Empty;
            int fromRow = 1;

            // Report tables and ranges
            ExcelRangeBase range;
            ExcelTable table;

            ExcelTable tableValuesART = null;
            ExcelTable tableValuesCPM = null;
            ExcelTable tableValuesEPM = null;
            ExcelTable tableValuesEXCPM = null;
            ExcelTable tableValuesHTTPEPM = null;

            ExcelTable tableEvents = null;

            #endregion

            logger.Info("Creating Entity Metrics Report for Metrics in {0}", metricsEntityFolderPath);

            #region Fill sheet and table name variables based on entity type

            if (entityType == APPLICATION_TYPE_SHORT)
            {
                entityName = entityRow.ApplicationName;
                entityID = entityRow.ApplicationID;
                entityNameForDisplay = entityName;
                entityTypeForDisplay = "Application";
                metricsEntityFolderPath = metricsEntityFolderTypePath;
            }
            else if (entityType == TIERS_TYPE_SHORT)
            {
                entityName = entityRow.TierName;
                entityID = entityRow.TierID;
                entityNameForDisplay = entityName;
                entityTypeForDisplay = "Tier";
                metricsEntityFolderPath = Path.Combine(
                    metricsEntityFolderTypePath,
                    getShortenedEntityNameForFileSystem(entityRow.TierName, entityRow.TierID));
            }
            else if (entityType == NODES_TYPE_SHORT)
            {
                entityName = entityRow.NodeName;
                entityID = entityRow.NodeID;
                entityNameForDisplay = String.Format(@"{0}\{1}", entityRow.TierName, entityName);
                entityTypeForDisplay = "Node";
                metricsEntityFolderPath = Path.Combine(
                    metricsEntityFolderTypePath,
                    getShortenedEntityNameForFileSystem(entityRow.TierName, entityRow.TierID),
                    getShortenedEntityNameForFileSystem(entityRow.NodeName, entityRow.NodeID));
            }
            else if (entityType == BACKENDS_TYPE_SHORT)
            {
                entityName = ((EntityBackend)entityRow).BackendName;
                entityID = ((EntityBackend)entityRow).BackendID;
                entityNameForDisplay = entityName;
                entityTypeForDisplay = "Backend";
                metricsEntityFolderPath = Path.Combine(
                    metricsEntityFolderTypePath,
                    getShortenedEntityNameForFileSystem(((EntityBackend)entityRow).BackendName, ((EntityBackend)entityRow).BackendID));
            }
            else if (entityType == BUSINESS_TRANSACTIONS_TYPE_SHORT)
            {
                entityName = ((EntityBusinessTransaction)entityRow).BTName;
                entityID = ((EntityBusinessTransaction)entityRow).BTID;
                entityNameForDisplay = String.Format(@"{0}\{1}", entityRow.TierName, entityName);
                entityTypeForDisplay = "Business Transaction";
                metricsEntityFolderPath = Path.Combine(
                    metricsEntityFolderTypePath,
                    getShortenedEntityNameForFileSystem(entityRow.TierName, entityRow.TierID),
                    getShortenedEntityNameForFileSystem(((EntityBusinessTransaction)entityRow).BTName, ((EntityBusinessTransaction)entityRow).BTID));
            }
            else if (entityType == SERVICE_ENDPOINTS_TYPE_SHORT)
            {
                entityName = ((EntityServiceEndpoint)entityRow).SEPName;
                entityID = ((EntityServiceEndpoint)entityRow).SEPID;
                entityNameForDisplay = String.Format(@"{0}\{1}", entityRow.TierName, entityName);
                entityTypeForDisplay = "Service Endpoint";
                metricsEntityFolderPath = Path.Combine(
                    metricsEntityFolderTypePath,
                    getShortenedEntityNameForFileSystem(entityRow.TierName, entityRow.TierID),
                    getShortenedEntityNameForFileSystem(((EntityServiceEndpoint)entityRow).SEPName, ((EntityServiceEndpoint)entityRow).SEPID));
            }
            else if (entityType == ERRORS_TYPE_SHORT)
            {
                entityName = ((EntityError)entityRow).ErrorName;
                entityID = ((EntityError)entityRow).ErrorID;
                entityNameForDisplay = String.Format(@"{0}\{1}", entityRow.TierName, entityName);
                entityTypeForDisplay = "Error";
                metricsEntityFolderPath = Path.Combine(
                    metricsEntityFolderTypePath,
                    getShortenedEntityNameForFileSystem(entityRow.TierName, entityRow.TierID),
                    getShortenedEntityNameForFileSystem(((EntityError)entityRow).ErrorName, ((EntityError)entityRow).ErrorID));
            }

            string entityNameForTable = getShortenedEntityNameForExcelTable(entityName, entityID);

            #endregion

            #region Entity Metrics Summary sheet

            ExcelWorksheet sheetMetrics = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_SUMMARY);
            sheetMetrics.Cells[1, 1].Value = "Table of Contents";
            sheetMetrics.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheetMetrics.Cells[2, 1].Value = "Entity Type";
            sheetMetrics.Cells[2, 2].Value = entityTypeForDisplay;
            sheetMetrics.Cells[3, 1].Value = "Entity Name";
            sheetMetrics.Cells[3, 2].Value = entityNameForDisplay;
            sheetMetrics.View.FreezePanes(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT + 1, 1);

            #region Full and Hourly ranges

            // Full range table
            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            entityFullRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_FULLRANGE_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityFullRangeReportFilePath, 0, sheetMetrics, fromRow, 1);
            if (range != null)
            {
                table = sheetMetrics.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_TABLE_ENTITY_FULL, entityType, entityNameForTable));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                adjustColumnsOfEntityRowTableInMetricReport(entityType, sheetMetrics, table);

                fromRow = fromRow + range.Rows + 2;
            }

            // Hourly table
            sheetMetrics.Cells[fromRow - 1, 1].Value = "Hourly";

            entityHourlyRangeReportFilePath = Path.Combine(metricsEntityFolderPath, CONVERT_ENTITY_METRICS_HOURLY_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityHourlyRangeReportFilePath, 0, sheetMetrics, fromRow, 1);
            if (range != null)
            {
                table = sheetMetrics.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_TABLE_ENTITY_HOURLY, entityType, entityNameForTable));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                adjustColumnsOfEntityRowTableInMetricReport(entityType, sheetMetrics, table);

                sheetMetrics.DeleteColumn(table.Columns["DetailLink"].Position + 1);
            }

            #endregion

            #endregion

            #region Entity Metric Detail sheet

            ExcelWorksheet sheetMetricsDetail = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_METRICS);
            sheetMetricsDetail.Cells[1, 1].Value = "Table of Contents";
            sheetMetricsDetail.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheetMetricsDetail.Cells[2, 1].Value = "Entity Type";
            sheetMetricsDetail.Cells[2, 2].Value = entityTypeForDisplay;
            sheetMetricsDetail.Cells[3, 1].Value = "Entity Name";
            sheetMetricsDetail.Cells[3, 2].Value = entityNameForDisplay;
            sheetMetricsDetail.View.FreezePanes(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT + 1, 1);

            #region ART Table

            // ART table
            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            int fromColumnMetricSummary = 1;

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_ART_SHORTNAME);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricSummaryReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_DESCRIPTION, entityType, entityNameForTable, METRIC_ART_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["PropertyName"].Position + fromColumnMetricSummary).AutoFit();
                sheetMetricsDetail.Column(table.Columns["PropertyValue"].Position + fromColumnMetricSummary).Width = 20;

                fromRow = fromRow + range.Rows + 2;
            }

            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricValuesReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_VALUES, entityType, entityNameForTable, METRIC_ART_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["EventTimeStamp"].Position + fromColumnMetricSummary).AutoFit();

                tableValuesART = table;
            }

            #endregion

            #region CPM table

            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            fromColumnMetricSummary = sheetMetricsDetail.Dimension.Columns + 2;

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_CPM_SHORTNAME);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricSummaryReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_DESCRIPTION, entityType, entityNameForTable, METRIC_CPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["PropertyName"].Position + fromColumnMetricSummary).AutoFit();
                sheetMetricsDetail.Column(table.Columns["PropertyValue"].Position + fromColumnMetricSummary).Width = 20;

                fromRow = fromRow + range.Rows + 2;
            }

            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricValuesReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_VALUES, entityType, entityNameForTable, METRIC_CPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["EventTimeStamp"].Position + fromColumnMetricSummary).AutoFit();

                tableValuesCPM = table;
            }

            #endregion

            #region EPM table

            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            fromColumnMetricSummary = sheetMetricsDetail.Dimension.Columns + 2;

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EPM_SHORTNAME);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricSummaryReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_DESCRIPTION, entityType, entityNameForTable, METRIC_EPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["PropertyName"].Position + fromColumnMetricSummary).AutoFit();
                sheetMetricsDetail.Column(table.Columns["PropertyValue"].Position + fromColumnMetricSummary).Width = 20;

                fromRow = fromRow + range.Rows + 2;
            }

            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricValuesReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_VALUES, entityType, entityNameForTable, METRIC_EPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["EventTimeStamp"].Position + fromColumnMetricSummary).AutoFit();

                tableValuesEPM = table;
            }

            #endregion

            #region EXCPM table

            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            fromColumnMetricSummary = sheetMetricsDetail.Dimension.Columns + 2;

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EXCPM_SHORTNAME);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricSummaryReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_DESCRIPTION, entityType, entityNameForTable, METRIC_EXCPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["PropertyName"].Position + fromColumnMetricSummary).AutoFit();
                sheetMetrics.Column(table.Columns["PropertyValue"].Position + fromColumnMetricSummary).Width = 20;

                fromRow = fromRow + range.Rows + 2;
            }

            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricValuesReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_VALUES, entityType, entityNameForTable, METRIC_EXCPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["EventTimeStamp"].Position + fromColumnMetricSummary).AutoFit();

                tableValuesEXCPM = table;

                fromRow = fromRow + range.Rows + 2;
            }

            #endregion

            #region HTTPEPM table

            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            fromColumnMetricSummary = sheetMetricsDetail.Dimension.Columns + 2;

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_HTTPEPM_SHORTNAME);
            entityMetricSummaryReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_SUMMARY_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricSummaryReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_DESCRIPTION, entityType, entityNameForTable, METRIC_HTTPEPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["PropertyName"].Position + fromColumnMetricSummary).AutoFit();
                sheetMetricsDetail.Column(table.Columns["PropertyValue"].Position + fromColumnMetricSummary).Width = 20;

                fromRow = fromRow + range.Rows + 2;
            }

            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            range = readCSVFileIntoExcelRange(entityMetricValuesReportFilePath, 0, sheetMetricsDetail, fromRow, fromColumnMetricSummary);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetMetricsDetail.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }
                table = sheetMetricsDetail.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_METRIC_TABLE_METRIC_VALUES, entityType, entityNameForTable, METRIC_HTTPEPM_SHORTNAME));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetMetricsDetail.Column(table.Columns["EventTimeStamp"].Position + fromColumnMetricSummary).AutoFit();

                tableValuesHTTPEPM = table;
            }

            #endregion

            #endregion

            #region Activity grid sheet

            ExcelWorksheet sheetFlowmap = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_ACTIVITYGRID);
            sheetFlowmap.Cells[1, 1].Value = "Table of Contents";
            sheetFlowmap.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheetFlowmap.Cells[2, 1].Value = "Entity Type";
            sheetFlowmap.Cells[2, 2].Value = entityTypeForDisplay;
            sheetFlowmap.Cells[3, 1].Value = "Entity Name";
            sheetFlowmap.Cells[3, 2].Value = entityNameForDisplay;
            sheetFlowmap.View.FreezePanes(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT + 1, 1);

            fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
            string activityGridReportFileName = Path.Combine(metricsEntityFolderPath, CONVERT_ACTIVITY_GRID_FILE_NAME);
            range = readCSVFileIntoExcelRange(activityGridReportFileName, 0, sheetFlowmap, fromRow, 1);
            if (range != null)
            {
                if (range.Rows == 1)
                {
                    // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                    range = sheetFlowmap.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                }

                table = sheetFlowmap.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_ACTIVITY_GRID, entityType, entityNameForTable));
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheetFlowmap.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheetFlowmap.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheetFlowmap.Column(table.Columns["CallType"].Position + 1).Width = 10;
                sheetFlowmap.Column(table.Columns["FromName"].Position + 1).Width = 35;
                sheetFlowmap.Column(table.Columns["ToName"].Position + 1).Width = 35;
                sheetFlowmap.Column(table.Columns["From"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["To"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["FromLink"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["ToLink"].Position + 1).AutoFit();
                sheetFlowmap.Column(table.Columns["MetricLink"].Position + 1).AutoFit();
            }

            #endregion

            #region Events and Health Rules

            #region Filter events by type of entity output

            // Filter events if necessary
            string eventsFilePath = Path.Combine(eventsFolderPath, CONVERT_EVENTS_FILE_NAME);
            string eventsFilePathFiltered = String.Empty;
            string healthRuleViolationEventsFilePath = Path.Combine(eventsFolderPath, CONVERT_HEALTH_RULE_EVENTS_FILE_NAME);
            string healthRuleViolationEventsFilePathFiltered = String.Empty;
            if (entityType == APPLICATION_TYPE_SHORT)
            {
                // The Application report has all events
            }
            else if (entityType == TIERS_TYPE_SHORT)
            {
                // Filter
                List<Event> eventsRows = FileIOHelper.readListFromCSVFile<Event>(eventsFilePath, new EventReportMap());
                eventsRows = eventsRows.Where(e => e.TierID == entityRow.TierID).ToList();
                if (eventsRows.Count > 0)
                {
                    eventsFilePathFiltered = Path.Combine(eventsFolderPath, String.Format(CONVERT_EVENTS_FILTERED_FILE_NAME, Guid.NewGuid()));
                    FileIOHelper.writeListToCSVFile<Event>(eventsRows, new EventReportMap(), eventsFilePathFiltered);
                }
                else
                {
                    eventsFilePath = String.Empty;
                }

                List<HealthRuleViolationEvent> healthRuleViolationRows = FileIOHelper.readListFromCSVFile<HealthRuleViolationEvent>(healthRuleViolationEventsFilePath, new HealthRuleViolationEventReportMap());
                healthRuleViolationRows = healthRuleViolationRows.Where(e => e.EntityType == entityTypeStringMapping[ENTITY_TYPE_TIER] && e.EntityID == entityRow.TierID).ToList();
                if (healthRuleViolationRows.Count > 0)
                {
                    healthRuleViolationEventsFilePathFiltered = Path.Combine(eventsFolderPath, String.Format(CONVERT_HEALTH_RULE_EVENTS_FILTERED_FILE_NAME, Guid.NewGuid()));
                    FileIOHelper.writeListToCSVFile<HealthRuleViolationEvent>(healthRuleViolationRows, new HealthRuleViolationEventReportMap(), healthRuleViolationEventsFilePathFiltered);
                }
                else
                {
                    healthRuleViolationEventsFilePath = String.Empty;
                }
            }
            else if (entityType == NODES_TYPE_SHORT)
            {
                // Filter
                List<Event> eventsRows = FileIOHelper.readListFromCSVFile<Event>(eventsFilePath, new EventReportMap());
                eventsRows = eventsRows.Where(e => e.NodeID == entityRow.NodeID).ToList();
                if (eventsRows.Count > 0)
                {
                    eventsFilePathFiltered = Path.Combine(eventsFolderPath, String.Format(CONVERT_EVENTS_FILTERED_FILE_NAME, Guid.NewGuid()));
                    FileIOHelper.writeListToCSVFile<Event>(eventsRows, new EventReportMap(), eventsFilePathFiltered);
                }
                else
                {
                    eventsFilePath = String.Empty;
                }

                List<HealthRuleViolationEvent> healthRuleViolationRows = FileIOHelper.readListFromCSVFile<HealthRuleViolationEvent>(healthRuleViolationEventsFilePath, new HealthRuleViolationEventReportMap());
                healthRuleViolationRows = healthRuleViolationRows.Where(e => e.EntityType == entityTypeStringMapping[ENTITY_TYPE_NODE] && e.EntityID == entityRow.NodeID).ToList();
                if (healthRuleViolationRows.Count > 0)
                {
                    healthRuleViolationEventsFilePathFiltered = Path.Combine(eventsFolderPath, String.Format(CONVERT_HEALTH_RULE_EVENTS_FILTERED_FILE_NAME, Guid.NewGuid()));
                    FileIOHelper.writeListToCSVFile<HealthRuleViolationEvent>(healthRuleViolationRows, new HealthRuleViolationEventReportMap(), healthRuleViolationEventsFilePathFiltered);
                }
                else
                {
                    healthRuleViolationEventsFilePath = String.Empty;
                }
            }
            else if (entityType == BUSINESS_TRANSACTIONS_TYPE_SHORT)
            {
                // Filter
                List<Event> eventsRows = FileIOHelper.readListFromCSVFile<Event>(eventsFilePath, new EventReportMap());
                eventsRows = eventsRows.Where(e => e.BTID == ((EntityBusinessTransaction)entityRow).BTID).ToList();
                if (eventsRows.Count > 0)
                {
                    eventsFilePathFiltered = Path.Combine(eventsFolderPath, String.Format(CONVERT_EVENTS_FILTERED_FILE_NAME, Guid.NewGuid()));
                    FileIOHelper.writeListToCSVFile<Event>(eventsRows, new EventReportMap(), eventsFilePathFiltered);
                }
                else
                {
                    eventsFilePath = String.Empty;
                }

                List<HealthRuleViolationEvent> healthRuleViolationRows = FileIOHelper.readListFromCSVFile<HealthRuleViolationEvent>(healthRuleViolationEventsFilePath, new HealthRuleViolationEventReportMap());
                healthRuleViolationRows = healthRuleViolationRows.Where(e => e.EntityType == entityTypeStringMapping[ENTITY_TYPE_BUSINESS_TRANSACTION] && e.EntityID == ((EntityBusinessTransaction)entityRow).BTID).ToList();
                if (healthRuleViolationRows.Count > 0)
                {
                    healthRuleViolationEventsFilePathFiltered = Path.Combine(eventsFolderPath, String.Format(CONVERT_HEALTH_RULE_EVENTS_FILTERED_FILE_NAME, Guid.NewGuid()));
                    FileIOHelper.writeListToCSVFile<HealthRuleViolationEvent>(healthRuleViolationRows, new HealthRuleViolationEventReportMap(), healthRuleViolationEventsFilePathFiltered);
                }
                else
                {
                    healthRuleViolationEventsFilePath = String.Empty;
                }
            }
            else
            {
                eventsFilePath = String.Empty;
                healthRuleViolationEventsFilePath = String.Empty;
            }

            if (eventsFilePathFiltered != String.Empty)
            {
                eventsFilePath = eventsFilePathFiltered;
            }
            if (healthRuleViolationEventsFilePathFiltered != String.Empty)
            {
                healthRuleViolationEventsFilePath = healthRuleViolationEventsFilePathFiltered;
            }

            #endregion

            #region Load Events Data

            // Do this for the output to the Events sheet later

            List<Event> eventsAll = null;
            if (eventsFilePath != String.Empty)
            {
                eventsAll = FileIOHelper.readListFromCSVFile<Event>(eventsFilePath, new EventReportMap());
            }

            #endregion

            #region Events

            if (eventsFilePath != String.Empty)
            {
                // Detail
                ExcelWorksheet sheetEvents = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_EVENTS);
                sheetEvents.Cells[1, 1].Value = "Table of Contents";
                sheetEvents.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheetEvents.Cells[2, 1].Value = "Entity Type";
                sheetEvents.Cells[2, 2].Value = entityTypeForDisplay;
                sheetEvents.Cells[3, 1].Value = "Entity Name";
                sheetEvents.Cells[3, 2].Value = entityNameForDisplay;
                sheetEvents.View.FreezePanes(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT + 1, 1);

                fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
                range = readCSVFileIntoExcelRange(eventsFilePath, 0, sheetEvents, fromRow, 1);
                if (range != null)
                {
                    if (range.Rows == 1)
                    {
                        // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                        range = sheetEvents.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                    }
                    table = sheetEvents.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_TABLE_EVENTS, entityType, entityNameForTable));
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheetEvents.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["EventID"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["Occured"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["OccuredUtc"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["Summary"].Position + 1).Width = 35;
                    sheetEvents.Column(table.Columns["Type"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["SubType"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["TriggeredEntityType"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["TriggeredEntityName"].Position + 1).Width = 20;
                    sheetEvents.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["NodeLink"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["BTLink"].Position + 1).AutoFit();
                    sheetEvents.Column(table.Columns["EventLink"].Position + 1).AutoFit();

                    tableEvents = table;
                }

                // Pivot
                ExcelWorksheet sheetEventsPivot = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_EVENTS_PIVOT);
                sheetEventsPivot.Cells[1, 1].Value = "Table of Contents";
                sheetEventsPivot.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheetEventsPivot.Cells[2, 1].Value = "See Table";
                sheetEventsPivot.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_ENTITY_DETAILS_SHEET_EVENTS);
                sheetEventsPivot.View.FreezePanes(REPORT_ENTITY_DETAILS_PIVOT_SHEET_START_PIVOT_AT + 2, 9);

                if (range != null)
                {
                    ExcelPivotTable pivot = sheetEventsPivot.PivotTables.Add(sheetEventsPivot.Cells[REPORT_ENTITY_DETAILS_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_ENTITY_DETAIL_PIVOT_EVENTS);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    // Tried to filter on this instead of having column, but don't like it
                    //ExcelPivotTableField fieldF = pivot.PageFields.Add(pivot.Fields["Type"]);
                    //fieldF = pivot.PageFields.Add(pivot.Fields["SubType"]);
                    fieldR = pivot.RowFields.Add(pivot.Fields["Type"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["SubType"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Severity"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["TierName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["BTName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["NodeName"]);
                    // Adding this in Excel produces days, hours and minutes subfields, very nice
                    // Adding this in code produces dates formatted strings, not very good
                    //ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["Occured"]);
                    //fieldC.Compact = false;
                    //fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["EventID"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }
            }

            #endregion

            #region HR Violations

            if (healthRuleViolationEventsFilePath != String.Empty)
            { 
                // Details
                ExcelWorksheet sheetHRViolations = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_EVENTS_HR);
                sheetHRViolations.Cells[1, 1].Value = "Table of Contents";
                sheetHRViolations.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheetHRViolations.Cells[2, 1].Value = "Entity Type";
                sheetHRViolations.Cells[2, 2].Value = entityTypeForDisplay;
                sheetHRViolations.Cells[3, 1].Value = "Entity Name";
                sheetHRViolations.Cells[3, 2].Value = entityNameForDisplay;
                sheetHRViolations.View.FreezePanes(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT + 1, 1);

                fromRow = REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT;
                range = readCSVFileIntoExcelRange(healthRuleViolationEventsFilePath, 0, sheetHRViolations, fromRow, 1);
                if (range != null)
                {
                    if (range.Rows == 1)
                    {
                        // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                        range = sheetHRViolations.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                    }
                    table = sheetHRViolations.Tables.Add(range, String.Format(REPORT_ENTITY_DETAILS_TABLE_EVENTS_HEALTH_RULE_VIOLATIONS, entityType, entityNameForTable));
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheetHRViolations.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheetHRViolations.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheetHRViolations.Column(table.Columns["EventID"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["From"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["To"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["HealthRuleName"].Position + 1).Width = 20;
                    sheetHRViolations.Column(table.Columns["EntityName"].Position + 1).Width = 20;
                    sheetHRViolations.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["HealthRuleLink"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["EntityLink"].Position + 1).AutoFit();
                    sheetHRViolations.Column(table.Columns["EventLink"].Position + 1).AutoFit();
                }

                // Pivot
                ExcelWorksheet sheetHRViolationsPivot = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_EVENTS_HR_PIVOT);
                sheetHRViolationsPivot.Cells[1, 1].Value = "Table of Contents";
                sheetHRViolationsPivot.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheetHRViolationsPivot.Cells[2, 1].Value = "See Table";
                sheetHRViolationsPivot.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_ENTITY_DETAILS_SHEET_EVENTS_HR);
                sheetHRViolationsPivot.View.FreezePanes(REPORT_ENTITY_DETAILS_PIVOT_SHEET_START_PIVOT_AT + 2, 5);

                if (range != null)
                {
                    ExcelPivotTable pivot = sheetHRViolationsPivot.PivotTables.Add(sheetHRViolationsPivot.Cells[REPORT_ENTITY_DETAILS_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_ENTITY_DETAIL_PIVOT_EVENTS_HR);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Severity"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["Status"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    ExcelPivotTableField fieldC = pivot.ColumnFields.Add(pivot.Fields["EntityType"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    fieldC = pivot.ColumnFields.Add(pivot.Fields["EntityName"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    fieldC = pivot.ColumnFields.Add(pivot.Fields["HealthRuleName"]);
                    fieldC.Compact = false;
                    fieldC.Outline = false;
                    ExcelPivotTableDataField fieldD = pivot.DataFields.Add(pivot.Fields["EventID"]);
                    fieldD.Function = DataFieldFunctions.Count;
                }
            }

            #endregion

            #region Clear out temporary files

            if (eventsFilePathFiltered != String.Empty)
            {
                FileIOHelper.deleteFile(eventsFilePathFiltered);
            }
            if (healthRuleViolationEventsFilePathFiltered != String.Empty)
            {
                FileIOHelper.deleteFile(healthRuleViolationEventsFilePathFiltered);
            }

            #endregion  

            #endregion  

            #region Graphs, Snapshots and Events sheet

            ExcelWorksheet sheetDetails = excelEntitiesDetail.Workbook.Worksheets.Add(REPORT_ENTITY_DETAILS_SHEET_HOURLY_TIMELINE);
            sheetDetails.Cells[1, 1].Value = "Table of Contents";
            sheetDetails.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheetDetails.Cells[2, 1].Value = "Entity Type";
            sheetDetails.Cells[2, 2].Value = entityTypeForDisplay;
            sheetDetails.Cells[3, 1].Value = "Entity Name";
            sheetDetails.Cells[3, 2].Value = entityNameForDisplay;
            sheetDetails.View.FreezePanes(REPORT_ENTITY_DETAILS_GRAPHS_SHEET_START_TABLE_AT + 1, 6);

            sheetDetails.Column(1).Width = 10;
            sheetDetails.Column(2).Width = 10;
            sheetDetails.Column(3).Width = 10;
            sheetDetails.Column(4).Width = 12;
            sheetDetails.Column(5).Width = 12;

            #region Load Metric Data

            // Load metric data for each of the tables because it is faster than enumerating it from Excel sheet
            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_ART_SHORTNAME);
            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            List<MetricValue> metricValuesAPM = FileIOHelper.readListFromCSVFile<MetricValue>(entityMetricValuesReportFilePath, new MetricValueMetricReportMap());

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_CPM_SHORTNAME);
            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            List<MetricValue> metricValuesCPM = FileIOHelper.readListFromCSVFile<MetricValue>(entityMetricValuesReportFilePath, new MetricValueMetricReportMap());

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EPM_SHORTNAME);
            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            List<MetricValue> metricValuesEPM = FileIOHelper.readListFromCSVFile<MetricValue>(entityMetricValuesReportFilePath, new MetricValueMetricReportMap());

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_EXCPM_SHORTNAME);
            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            List<MetricValue> metricValuesEXCPM = FileIOHelper.readListFromCSVFile<MetricValue>(entityMetricValuesReportFilePath, new MetricValueMetricReportMap());

            metricsDataFolderPath = Path.Combine(metricsEntityFolderPath, METRIC_HTTPEPM_SHORTNAME);
            entityMetricValuesReportFilePath = Path.Combine(metricsDataFolderPath, CONVERT_METRIC_VALUES_FILE_NAME);
            List<MetricValue> metricValuesHTTPEPM = FileIOHelper.readListFromCSVFile<MetricValue>(entityMetricValuesReportFilePath, new MetricValueMetricReportMap());

            // Break up the metric data into timeranges for each hour
            int[,] timeRangeAPM = null;
            int[,] timeRangeCPM = null;
            int[,] timeRangeEPM = null;
            int[,] timeRangeEXCPM = null;
            int[,] timeRangeHTTPEPM = null;
            if (metricValuesAPM != null)
            {
                timeRangeAPM = getLocationsOfHourlyTimeRangesFromMetricValues(metricValuesAPM, jobConfiguration.Input.HourlyTimeRanges);
            }
            if (metricValuesCPM != null)
            {
                timeRangeCPM = getLocationsOfHourlyTimeRangesFromMetricValues(metricValuesCPM, jobConfiguration.Input.HourlyTimeRanges);
            }
            if (metricValuesEPM != null)
            {
                timeRangeEPM = getLocationsOfHourlyTimeRangesFromMetricValues(metricValuesEPM, jobConfiguration.Input.HourlyTimeRanges);
            }
            if (metricValuesEXCPM != null)
            {
                timeRangeEXCPM = getLocationsOfHourlyTimeRangesFromMetricValues(metricValuesEXCPM, jobConfiguration.Input.HourlyTimeRanges);
            }
            if (metricValuesHTTPEPM != null)
            {
                timeRangeHTTPEPM = getLocationsOfHourlyTimeRangesFromMetricValues(metricValuesHTTPEPM, jobConfiguration.Input.HourlyTimeRanges);
            }

            #endregion

            #region Prepare Styles and Resize Columns for each of the hour ranges

            var minuteHeadingtyle = sheetDetails.Workbook.Styles.CreateNamedStyle("MinuteHeadingStyle");
            minuteHeadingtyle.Style.Font.Size = 9;

            var eventHeadingStyle = sheetDetails.Workbook.Styles.CreateNamedStyle("EventHeadingStyle");
            eventHeadingStyle.Style.Font.Size = 9;

            var infoEventLinkStyle = sheetDetails.Workbook.Styles.CreateNamedStyle("InfoEventLinkStyle");
            infoEventLinkStyle.Style.Font.Color.SetColor(Color.Blue);
            infoEventLinkStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var warnEventLinkStyle = sheetDetails.Workbook.Styles.CreateNamedStyle("WarnEventLinkStyle");
            warnEventLinkStyle.Style.Font.Color.SetColor(Color.Orange);
            warnEventLinkStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var errorEventLinkStyle = sheetDetails.Workbook.Styles.CreateNamedStyle("ErrorEventLinkStyle");
            errorEventLinkStyle.Style.Font.Color.SetColor(Color.DarkRed);
            errorEventLinkStyle.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            #endregion

            #region Resize Columns for each of the hour ranges

            // Prepare vertical section for each of the hours
            int columnOffsetBegin = 6;
            int columnOffsetBetweenRanges = 1;
            for (int i = 0; i < jobConfiguration.Input.HourlyTimeRanges.Count; i++)
            {
                JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[i];

                // Adjust columns in sheet
                int columnIndexTimeRangeStart = columnOffsetBegin + i * columnOffsetBetweenRanges + i * 60;
                int columnIndexTimeRangeEnd = columnIndexTimeRangeStart + 60;
                int minuteNumber = 0;
                for (int columnIndex = columnIndexTimeRangeStart; columnIndex < columnIndexTimeRangeEnd; columnIndex++)
                {
                    sheetDetails.Column(columnIndex).Width = 2.5;
                    sheetDetails.Cells[REPORT_ENTITY_DETAILS_GRAPHS_SHEET_START_TABLE_AT, columnIndex].Value = minuteNumber;
                    sheetDetails.Cells[REPORT_ENTITY_DETAILS_GRAPHS_SHEET_START_TABLE_AT, columnIndex].StyleName = "MinuteHeadingStyle";
                    minuteNumber++;
                }

                sheetDetails.Cells[1, columnIndexTimeRangeStart + 40].Value = "From";
                sheetDetails.Cells[1, columnIndexTimeRangeStart + 50].Value = "To";
                sheetDetails.Cells[2, columnIndexTimeRangeStart + 37].Value = "Local";
                sheetDetails.Cells[2, columnIndexTimeRangeStart + 40].Value = jobTimeRange.From.ToLocalTime().ToString("G");
                sheetDetails.Cells[2, columnIndexTimeRangeStart + 50].Value = jobTimeRange.To.ToLocalTime().ToString("G");
                sheetDetails.Cells[3, columnIndexTimeRangeStart + 37].Value = "UTC";
                sheetDetails.Cells[3, columnIndexTimeRangeStart + 40].Value = jobTimeRange.From.ToString("G");
                sheetDetails.Cells[3, columnIndexTimeRangeStart + 50].Value = jobTimeRange.To.ToString("G");
            }

            #endregion

            #region Output Metric Graphs

            int rowIndexART = 1;
            int rowIndexCPM = 1;
            int rowIndexEPM = 1;
            int rowIndexEXCPM = 1;
            int rowIndexHTTPEPM = 1;

            for (int i = 0; i < jobConfiguration.Input.HourlyTimeRanges.Count; i++)
            {
                JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[i];

                int columnIndexTimeRangeStart = columnOffsetBegin + i * columnOffsetBetweenRanges + i * 60;

                ExcelChart chart = sheetDetails.Drawings.AddChart(String.Format(REPORT_ENTITY_DETAILS_GRAPH, entityType, entityNameForTable, jobTimeRange.From), eChartType.XYScatterLinesNoMarkers);
                chart.SetPosition(REPORT_ENTITY_DETAILS_LIST_SHEET_START_TABLE_AT, 0, columnIndexTimeRangeStart - 1, 0);
                chart.SetSize(1020, 200);
                chart.Style = eChartStyle.Style17;

                ExcelChart chartART = chart.PlotArea.ChartTypes.Add(eChartType.XYScatterLinesNoMarkers);
                chartART.UseSecondaryAxis = true;

                // ART
                if (tableValuesART != null)
                {
                    ExcelRangeBase rangeXTime = getRangeMetricDataTableForThisHourDateTimeSeries(tableValuesART, timeRangeAPM[i, 0], timeRangeAPM[i, 1]);
                    ExcelRangeBase rangeYValues = getRangeMetricDataTableForThisHourValueSeries(tableValuesART, timeRangeAPM[i, 0], timeRangeAPM[i, 1]);
                    if (rangeXTime != null & rangeYValues != null)
                    {
                        ExcelChartSerie series = chartART.Series.Add(rangeYValues, rangeXTime);
                        series.Header = "ART";
                        ((ExcelScatterChartSerie)series).LineColor = Color.Green;

                        rowIndexART = rowIndexART + rangeXTime.Rows - 1;
                    }
                }

                // CPM
                if (tableValuesCPM != null)
                {
                    ExcelRangeBase rangeXTime = getRangeMetricDataTableForThisHourDateTimeSeries(tableValuesCPM, timeRangeCPM[i, 0], timeRangeCPM[i, 1]);
                    ExcelRangeBase rangeYValues = getRangeMetricDataTableForThisHourValueSeries(tableValuesCPM, timeRangeCPM[i, 0], timeRangeCPM[i, 1]);
                    if (rangeXTime != null & rangeYValues != null)
                    {
                        ExcelChartSerie series = chart.Series.Add(rangeYValues, rangeXTime);
                        series.Header = "CPM";
                        ((ExcelScatterChartSerie)series).LineColor = Color.Blue;

                        rowIndexCPM = rowIndexCPM + rangeXTime.Rows - 1;
                    }
                }

                // EPM
                if (tableValuesEPM != null)
                {
                    ExcelRangeBase rangeXTime = getRangeMetricDataTableForThisHourDateTimeSeries(tableValuesEPM, timeRangeEPM[i, 0], timeRangeEPM[i, 1]);
                    ExcelRangeBase rangeYValues = getRangeMetricDataTableForThisHourValueSeries(tableValuesEPM, timeRangeEPM[i, 0], timeRangeEPM[i, 1]);
                    if (rangeXTime != null & rangeYValues != null)
                    {
                        ExcelChartSerie series = chart.Series.Add(rangeYValues, rangeXTime);
                        series.Header = "EPM";
                        ((ExcelScatterChartSerie)series).LineColor = Color.Red;

                        rowIndexEPM = rowIndexEPM + rangeXTime.Rows - 1;
                    }
                }

                // EXCPM
                if (tableValuesEXCPM != null)
                {
                    ExcelRangeBase rangeXTime = getRangeMetricDataTableForThisHourDateTimeSeries(tableValuesEXCPM, timeRangeEXCPM[i, 0], timeRangeEXCPM[i, 1]);
                    ExcelRangeBase rangeYValues = getRangeMetricDataTableForThisHourValueSeries(tableValuesEXCPM, timeRangeEXCPM[i, 0], timeRangeEXCPM[i, 1]);
                    if (rangeXTime != null & rangeYValues != null)
                    {
                        ExcelChartSerie series = chart.Series.Add(rangeYValues, rangeXTime);
                        series.Header = "EXCPM";
                        ((ExcelScatterChartSerie)series).LineColor = Color.Orange;

                        rowIndexEXCPM = rowIndexEXCPM + rangeXTime.Rows - 1;
                    }
                }

                // HTTPEPM
                if (tableValuesHTTPEPM != null)
                {
                    ExcelRangeBase rangeXTime = getRangeMetricDataTableForThisHourDateTimeSeries(tableValuesHTTPEPM, timeRangeHTTPEPM[i, 0], timeRangeHTTPEPM[i, 1]);
                    ExcelRangeBase rangeYValues = getRangeMetricDataTableForThisHourValueSeries(tableValuesHTTPEPM, timeRangeHTTPEPM[i, 0], timeRangeHTTPEPM[i, 1]);
                    if (rangeXTime != null & rangeYValues != null)
                    {
                        ExcelChartSerie series = chart.Series.Add(rangeYValues, rangeXTime);
                        series.Header = "HTTPEPM";
                        ((ExcelScatterChartSerie)series).LineColor = Color.Pink;

                        rowIndexHTTPEPM = rowIndexHTTPEPM + rangeXTime.Rows - 1;
                    }
                }
            }

            #endregion

            #region Output Events

            fromRow = REPORT_ENTITY_DETAILS_GRAPHS_SHEET_START_TABLE_AT;

            if (eventsAll != null)
            {
                // Group by type and subtype to break the overall list in manageable chunks
                var eventsAllGroupedByType = eventsAll.GroupBy(e => new { e.Type, e.SubType });
                List<List<Event>> eventsAllGrouped = new List<List<Event>>();

                // Group by the additional columns for some of the events
                foreach (var eventsGroup in eventsAllGroupedByType)
                {
                    switch (eventsGroup.Key.Type)
                    {
                        case "RESOURCE_POOL_LIMIT":
                            var eventsGroup_RESOURCE_POOL_LIMIT = eventsGroup.ToList().GroupBy(e => e.TierName);
                            foreach (var eventsGrouping in eventsGroup_RESOURCE_POOL_LIMIT)
                            {
                                eventsAllGrouped.Add(eventsGrouping.ToList());
                            }
                            break;

                        case "APPLICATION_ERROR":
                            var eventsGroup_APPLICATION_ERROR = eventsGroup.ToList().GroupBy(e => e.TierName);
                            foreach (var eventsGrouping in eventsGroup_APPLICATION_ERROR)
                            {
                                eventsAllGrouped.Add(eventsGrouping.ToList());
                            }
                            break;

                        case "DIAGNOSTIC_SESSION":
                            var eventsGroup_DIAGNOSTIC_SESSION = eventsGroup.ToList().GroupBy(e => new { e.TierName, e.BTName });
                            foreach (var eventsGrouping in eventsGroup_DIAGNOSTIC_SESSION)
                            {
                                eventsAllGrouped.Add(eventsGrouping.ToList());
                            }
                            break;

                        case "CUSTOM":
                            var eventsGroup_CUSTOM = eventsGroup.ToList().GroupBy(e => e.TierName);
                            foreach (var eventsGrouping in eventsGroup_CUSTOM)
                            {
                                eventsAllGrouped.Add(eventsGrouping.ToList());
                            }
                            break;

                        case "POLICY_OPEN_WARNING":
                        case "POLICY_OPEN_CRITICAL":
                        case "POLICY_CLOSE_WARNING":
                        case "POLICY_CLOSE_CRITICAL":
                        case "POLICY_UPGRADED":
                        case "POLICY_DOWNGRADED":
                        case "POLICY_CANCELED_WARNING":
                        case "POLICY_CANCELED_CRITICAL":
                        case "POLICY_CONTINUES_CRITICAL":
                        case "POLICY_CONTINUES_WARNING":
                            var eventsGroup_POLICY_ALL = eventsGroup.ToList().GroupBy(e => new { e.TriggeredEntityName, e.TierName });
                            foreach (var eventsGrouping in eventsGroup_POLICY_ALL)
                            {
                                eventsAllGrouped.Add(eventsGrouping.ToList());
                            }
                            break;

                        default:
                            eventsAllGrouped.Add(eventsGroup.ToList());
                            break;
                    }
                }

                // At this point we have the events partitioned just the way we want them
                // Each entry is guaranteed to have at least one item

                sheetDetails.Cells[fromRow, 1].Value = "Type";
                sheetDetails.Cells[fromRow, 2].Value = "SubType";
                sheetDetails.Cells[fromRow, 3].Value = "Tier";
                sheetDetails.Cells[fromRow, 4].Value = "BT";
                sheetDetails.Cells[fromRow, 5].Value = "Trigger";

                fromRow++;
                for (int i = 0; i < eventsAllGrouped.Count; i++)
                {
                    int toRow = fromRow;
                    // Output headings in the event heading columns columns
                    Event firstEvent = eventsAllGrouped[i][0];

                    sheetDetails.Cells[fromRow, 1].Value = firstEvent.Type;
                    if (firstEvent.SubType != String.Empty) { sheetDetails.Cells[fromRow, 2].Value = firstEvent.SubType; }
                    if (firstEvent.TierName != String.Empty) { sheetDetails.Cells[fromRow, 3].Value = firstEvent.TierName; }
                    if (firstEvent.BTName != String.Empty) { sheetDetails.Cells[fromRow, 4].Value = firstEvent.BTName; }
                    if (firstEvent.TriggeredEntityName != String.Empty) { sheetDetails.Cells[fromRow, 5].Value = firstEvent.TriggeredEntityName; }
                    sheetDetails.Cells[fromRow, 1].StyleName = "EventHeadingStyle";
                    sheetDetails.Cells[fromRow, 2].StyleName = "EventHeadingStyle";
                    sheetDetails.Cells[fromRow, 3].StyleName = "EventHeadingStyle";
                    sheetDetails.Cells[fromRow, 4].StyleName = "EventHeadingStyle";
                    sheetDetails.Cells[fromRow, 5].StyleName = "EventHeadingStyle";

                    // Go through each hour range at a time
                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                    {
                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                        List<Event> eventsInThisTimeRange = eventsAllGrouped[i].Where(e => e.OccuredUtc >= jobTimeRange.From && e.OccuredUtc < jobTimeRange.To).ToList();

                        // Now we finally have all the events for this type in this hour. Output
                        int columnIndexTimeRangeStart = columnOffsetBegin + j * columnOffsetBetweenRanges + j * 60;
                        foreach (Event interestingEvent in eventsInThisTimeRange)
                        {
                            int columnInThisTimeRange = columnIndexTimeRangeStart + interestingEvent.OccuredUtc.Minute;
                            int rowToOutputThisEventTo = fromRow;
                            while (true)
                            {
                                if (sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Value == null && sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Formula == String.Empty)
                                {
                                    break;
                                }
                                else
                                {
                                    rowToOutputThisEventTo++;
                                }
                            }
                            if (rowToOutputThisEventTo > fromRow)
                            {
                                toRow = rowToOutputThisEventTo;
                            }

                            int rowIndexOfThisEvent = eventsAll.FindIndex(e => e.EventID == interestingEvent.EventID);
                            // Finally output the value
                            switch (interestingEvent.Severity)
                            {
                                case "INFO":
                                    //sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Value = "I";
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Formula = String.Format(@"=HYPERLINK(""#'{0}'!{1}"", ""I"")", REPORT_ENTITY_DETAILS_SHEET_EVENTS, getRangeEventDataTableThisEvent(tableEvents, rowIndexOfThisEvent));
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].StyleName = "InfoEventLinkStyle";
                                    break;
                                case "WARN":
                                    //sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Value = "W";
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Formula = String.Format(@"=HYPERLINK(""#'{0}'!{1}"", ""W"")", REPORT_ENTITY_DETAILS_SHEET_EVENTS, getRangeEventDataTableThisEvent(tableEvents, rowIndexOfThisEvent));
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].StyleName = "WarnEventLinkStyle";
                                    break;
                                case "ERROR":
                                    //sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Value = "E";
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Formula = String.Format(@"=HYPERLINK(""#'{0}'!{1}"", ""E"")", REPORT_ENTITY_DETAILS_SHEET_EVENTS, getRangeEventDataTableThisEvent(tableEvents, rowIndexOfThisEvent));
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].StyleName = "ErrorEventLinkStyle";
                                    break;
                                default:
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].Formula = String.Format(@"=HYPERLINK(""#'{0}'!{1}"", ""?"")", REPORT_ENTITY_DETAILS_SHEET_EVENTS, getRangeEventDataTableThisEvent(tableEvents, rowIndexOfThisEvent));
                                    sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].StyleName = "ErrorEventLinkStyle";
                                    break;

                            }
                            // Comments are too small otherwise a cool idea
                            //sheetDetails.Cells[rowToOutputThisEventTo, columnInThisTimeRange].AddComment(interestingEvent.Summary, interestingEvent.EventID.ToString());
                        }
                    }
                    fromRow = toRow;
                    fromRow++;
                }
            }

            #endregion

            #endregion
        }

        private static bool finalizeAndSaveIndividualEntityMetricReport(ExcelPackage excelEntitiesDetail, string reportFilePath)
        {
            logger.Info("Finalize Entity Metrics Report File {0}", reportFilePath);

            #region TOC sheet

            // TOC sheet again
            ExcelWorksheet sheet = excelEntitiesDetail.Workbook.Worksheets[REPORT_SHEET_TOC];
            sheet.Cells[1, 1].Value = "Sheet Name";
            sheet.Cells[1, 2].Value = "Entity Type";
            sheet.Cells[1, 3].Value = "Entity Name";
            sheet.Cells[1, 4].Value = "# Tables";
            sheet.Cells[1, 5].Value = "Link";
            int rowNum = 1;
            foreach (ExcelWorksheet s in excelEntitiesDetail.Workbook.Worksheets)
            {
                rowNum++;
                sheet.Cells[rowNum, 1].Value = s.Name;
                sheet.Cells[rowNum, 2].Value = s.Cells[2, 2].Value;
                sheet.Cells[rowNum, 3].Value = s.Cells[3, 2].Value;
                sheet.Cells[rowNum, 4].Value = s.Tables.Count;
                sheet.Cells[rowNum, 5].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
            }
            ExcelRangeBase range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
            ExcelTable table = sheet.Tables.Add(range, REPORT_ENTITY_DETAILS_TABLE_TOC);
            table.ShowHeader = true;
            table.TableStyle = TableStyles.Medium2;
            table.ShowFilter = true;
            table.ShowTotal = false;

            sheet.Column(table.Columns["Sheet Name"].Position + 1).AutoFit();
            sheet.Column(table.Columns["Entity Type"].Position + 1).AutoFit();
            sheet.Column(table.Columns["Entity Name"].Position + 1).Width = 70;
            sheet.Column(table.Columns["# Tables"].Position + 1).AutoFit();

            #endregion

            #region Save file 

            // Report files
            logger.Info("Saving Excel report {0}", reportFilePath);
            //loggerConsole.Info("Saving Excel report {0}", reportFilePath);

            string folderPath = Path.GetDirectoryName(reportFilePath);
            if (Directory.Exists(folderPath) == false)
            {
                Directory.CreateDirectory(folderPath);
            }

            try
            {
                // Save full report Excel files
                excelEntitiesDetail.SaveAs(new FileInfo(reportFilePath));
            }
            catch (InvalidOperationException ex)
            {
                logger.Warn("Unable to save Excel file {0}", reportFilePath);
                logger.Warn(ex);
                loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);

                return false;
            }

            #endregion

            return true;
        }

        private static void adjustColumnsOfEntityRowTableInMetricReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            if (entityType == APPLICATION_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
            }
            else if (entityType == TIERS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["AgentType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
            }
            else if (entityType == NODES_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["AgentType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["NodeLink"].Position + 1).AutoFit();
            }
            else if (entityType == BACKENDS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendType"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["BackendLink"].Position + 1).AutoFit();
            }
            else if (entityType == BUSINESS_TRANSACTIONS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["BTLink"].Position + 1).AutoFit();
            }
            else if (entityType == SERVICE_ENDPOINTS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPType"].Position + 1).AutoFit();
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["SEPLink"].Position + 1).AutoFit();
            }
            else if (entityType == ERRORS_TYPE_SHORT)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["From"].Position + 1).AutoFit();
                sheet.Column(table.Columns["To"].Position + 1).AutoFit();
                sheet.Column(table.Columns["FromUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ToUtc"].Position + 1).AutoFit();
                sheet.Column(table.Columns["DetailLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ControllerLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ApplicationLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["TierLink"].Position + 1).AutoFit();
                sheet.Column(table.Columns["ErrorLink"].Position + 1).AutoFit();
            }
        }

        private static int[,] getLocationsOfHourlyTimeRangesFromMetricValues(List<MetricValue> metricValues, List<JobTimeRange> jobTimeRanges)
        {
            // Element 0 of each row is index of time range Start
            // Element 1 of each row is index of time range End
            int[,] timeRangePartitionData = new int[jobTimeRanges.Count, 2];
            int fromIndexColumn = 0;
            int toIndexColumn = 1;
            int metricValuesCount = metricValues.Count;

            // First pass - scroll through and bucketize each our into invidual hour chunk
            int currentMetricValueRowIndex = 0;
            for (int i = 0; i < jobTimeRanges.Count; i++)
            {
                JobTimeRange jobTimeRange = jobTimeRanges[i];

                timeRangePartitionData[i, fromIndexColumn] = -1;
                timeRangePartitionData[i, toIndexColumn] = -1;

                while (currentMetricValueRowIndex < metricValuesCount)
                {
                    if (metricValues[currentMetricValueRowIndex].EventTimeStampUtc >= jobTimeRange.From &&
                        metricValues[currentMetricValueRowIndex].EventTimeStampUtc < jobTimeRange.To)
                    {
                        if (timeRangePartitionData[i, fromIndexColumn] == -1)
                        {
                            // Found From
                            timeRangePartitionData[i, fromIndexColumn] = currentMetricValueRowIndex;
                            timeRangePartitionData[i, toIndexColumn] = currentMetricValueRowIndex;
                        }
                        else
                        {
                            // Found potential To
                            timeRangePartitionData[i, toIndexColumn] = currentMetricValueRowIndex;
                        }
                        currentMetricValueRowIndex++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Second pass - adjust end times to overlap with the on-the-hour entries from subsequent ones because we're going to want those entries on the graphs
            // But don't adjust last entry 
            for (int i = 0; i < jobTimeRanges.Count - 1; i++)
            {
                JobTimeRange jobTimeRange = jobTimeRanges[i];

                if (timeRangePartitionData[i, fromIndexColumn] != -1 && 
                    timeRangePartitionData[i, toIndexColumn] != -1 &&
                    timeRangePartitionData[i + 1, fromIndexColumn] != -1 &&
                    timeRangePartitionData[i + 1, toIndexColumn] != -1)
                {
                    if (metricValues[timeRangePartitionData[i + 1, fromIndexColumn]].EventTimeStampUtc == jobTimeRange.To)
                    {
                        timeRangePartitionData[i, toIndexColumn] = timeRangePartitionData[i + 1, fromIndexColumn];
                    }
                }
            }

            return timeRangePartitionData;
        }

        private static ExcelRangeBase getRangeMetricDataTableForThisHourDateTimeSeries(ExcelTable table, int rowIndexStart, int rowIndexEnd)
        {
            // Find index of the important columns
            int columnIndexEventTime = table.Columns["EventTime"].Position;

            if (rowIndexStart != -1 && rowIndexEnd != -1)
            {
                return table.WorkSheet.Cells[
                    table.Address.Start.Row + rowIndexStart + 1,
                    table.Address.Start.Column + columnIndexEventTime,
                    table.Address.Start.Row + rowIndexEnd + 1,
                    table.Address.Start.Column + columnIndexEventTime];
            }

            return null;
        }

        private static ExcelRangeBase getRangeMetricDataTableForThisHourValueSeries(ExcelTable table, int rowIndexStart, int rowIndexEnd)
        {
            // Find index of the important columns
            int columnIndexEventTime = table.Columns["Value"].Position;

            if (rowIndexStart != -1 && rowIndexEnd != -1)
            {
                return table.WorkSheet.Cells[
                    table.Address.Start.Row + rowIndexStart + 1,
                    table.Address.Start.Column + columnIndexEventTime,
                    table.Address.Start.Row + rowIndexEnd + 1,
                    table.Address.Start.Column + columnIndexEventTime];
            }

            return null;
        }

        private static ExcelRangeBase getRangeEventDataTableThisEvent(ExcelTable table, int rowIndex)
        {
            return table.WorkSheet.Cells[
                table.Address.Start.Row + rowIndex + 1,
                table.Address.Start.Column,
                table.Address.Start.Row + rowIndex + 1,
                table.Address.End.Column];
        }

        #endregion


        #region Helper functions for reading CSV into Excel worksheet

        private static ExcelRangeBase readCSVFileIntoExcelRange(string csvFilePath, int skipLinesFromBeginning, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            logger.Trace("Reading CSV file {0} to Excel Worksheet {1} at (row {2}, column {3})", csvFilePath, sheet.Name, startRow, startColumn);

            if (File.Exists(csvFilePath) == false)
            {
                logger.Warn("Unable to find file {0}", csvFilePath);

                return null;
            }

            try
            {
                int csvRowIndex = -1;
                int numColumnsInCSV = 0;
                string[] headerRowValues = null;

                using (StreamReader sr = File.OpenText(csvFilePath))
                {
                    CsvParser csvParser = new CsvParser(sr);

                    // Read all rows
                    while (true)
                    {
                        string[] rowValues = csvParser.Read();
                        if (rowValues == null)
                        {
                            break;
                        }
                        csvRowIndex++;

                        // Grab the headers
                        if (csvRowIndex == 0)
                        {
                            headerRowValues = rowValues;
                            numColumnsInCSV = headerRowValues.Length;
                        }

                        // Should we skip?
                        if (csvRowIndex < skipLinesFromBeginning)
                        {
                            // Skip this line
                            continue;
                        }

                        // Read row one field at a time
                        int csvFieldIndex = 0;
                        foreach (string fieldValue in rowValues)
                        {
                            ExcelRange cell = sheet.Cells[csvRowIndex + startRow - skipLinesFromBeginning, csvFieldIndex + startColumn];
                            if (fieldValue.StartsWith("=") == true)
                            {
                                cell.Formula = fieldValue;

                                if (fieldValue.StartsWith("=HYPERLINK") == true)
                                {
                                    cell.StyleName = "HyperLinkStyle";
                                }
                            }
                            else if (fieldValue.StartsWith("http://") == true || fieldValue.StartsWith("https://") == true)
                            {
                                // If it is in the column ending in Link, I want it to be hyperlinked and use the column name
                                if (headerRowValues[csvFieldIndex].EndsWith("Link"))
                                {
                                    cell.Hyperlink = new Uri(fieldValue);
                                    string linkName = String.Format("<{0}>", headerRowValues[csvFieldIndex].Replace("Link", ""));
                                    if (linkName == "<>") linkName = "<Go>";
                                    cell.Value = linkName;
                                    cell.StyleName = "HyperLinkStyle";
                                }
                                else
                                {
                                    // Otherwise dump it as text
                                    cell.Value = fieldValue;
                                }
                            }
                            else
                            {
                                Double numValue;
                                bool boolValue;
                                DateTime dateValue;

                                // Try some casting
                                if (Double.TryParse(fieldValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out numValue) == true)
                                {
                                    // Number
                                    cell.Value = numValue;
                                }
                                else if (Boolean.TryParse(fieldValue, out boolValue) == true)
                                {
                                    // Boolean
                                    cell.Value = boolValue;
                                }
                                else if (DateTime.TryParse(fieldValue, out dateValue))
                                {
                                    // DateTime
                                    cell.Value = dateValue;
                                    if (headerRowValues[csvFieldIndex] == "EventTime")
                                    {
                                        cell.Style.Numberformat.Format = "hh:mm";
                                    }
                                    else
                                    {
                                        cell.Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss";
                                    }
                                }
                                else
                                {
                                    // Something else, dump as is
                                    cell.Value = fieldValue;
                                }
                            }
                            csvFieldIndex++;
                        }
                    }
                }

                return sheet.Cells[startRow, startColumn, startRow + csvRowIndex, startColumn + numColumnsInCSV - 1];
            }
            catch (Exception ex)
            {
                logger.Error("Unable to read CSV from file {0}", csvFilePath);
                logger.Error(ex);
            }

            return null;
        }

        #endregion

        #region Helper function for various entity naming

        private static string getShortenedEntityNameForFileSystem(string entityName, long entityID)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getFileSystemSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 12) entityName = entityName.Substring(0, 12);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        private static string getFileSystemSafeString(string fileOrFolderNameToClear)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileOrFolderNameToClear = fileOrFolderNameToClear.Replace(c, '-');
            }

            return fileOrFolderNameToClear;
        }

        private static string getShortenedEntityNameForExcelTable(string entityName, long entityID)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getExcelTableOrSheetSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 50) entityName = entityName.Substring(0, 50);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        private static string getShortenedEntityNameForExcelSheet(string entityName, int entityID, int maxLength)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getExcelTableOrSheetSafeString(entityName);

            // Second, measure the unique ID length and shorten the name of string down
            maxLength = maxLength - 1 - entityID.ToString().Length;

            // Third, shorten the string 
            if (entityName.Length > maxLength) entityName = entityName.Substring(0, maxLength);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        private static string getExcelTableOrSheetSafeString(string stringToClear)
        {
            char[] excelTableInvalidChars = { ' ', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=', ',', '/', '\\', '[', ']', ':', '?', '|', '"'};
            foreach (var c in excelTableInvalidChars)
            {
                stringToClear = stringToClear.Replace(c, '-');
            }

            return stringToClear;
        }

        #endregion

        #region Helper functions for Unix time handling

        /// <summary>
        /// Converts UNIX timestamp to DateTime
        /// </summary>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        private static DateTime convertFromUnixTimestamp(long timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Converts DateTime to Unix timestamp
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static long convertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return (long)Math.Floor(diff.TotalMilliseconds);
        }

        #endregion
    }
}