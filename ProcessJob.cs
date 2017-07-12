using AppDynamics.OfflineData.DataObjects;
using AppDynamics.OfflineData.JobParameters;
using AppDynamics.OfflineData.ReportObjects;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AppDynamics.OfflineData
{
    public class ProcessJob
    {
        #region Constants for metric retrieval

        // Constants for metric naming
        private const string METRIC_ART = "Average Response Time (ms)";
        private const string METRIC_CPM = "Calls per Minute";
        private const string METRIC_EPM = "Errors per Minute";
        private const string METRIC_EXCPM = "Exceptions per Minute";
        private const string METRIC_HTTPEPM = "HTTP Error Codes per Minute";

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

        // More folder names for entity types
        private const string APPLICATION_FOLDER_NAME = "APP";
        private const string TIERS_FOLDER_NAME = "TIER";
        private const string NODES_FOLDER_NAME = "NODE";
        private const string BACKENDS_FOLDER_NAME = "BACK";
        private const string BUSINESS_TRANSACTIONS_FOLDER_NAME = "BT";
        private const string SERVICE_ENDPOINTS_FOLDER_NAME = "SEP";
        private const string ERRORS_FOLDER_NAME = "ERR";

        // Metric folder names
        private const string METRIC_ART_FOLDER_NAME = "ART";
        private const string METRIC_CPM_FOLDER_NAME = "CPM";
        private const string METRIC_EPM_FOLDER_NAME = "EPM";
        private const string METRIC_EXCPM_FOLDER_NAME = "EXCPM";
        private const string METRIC_HTTPEPM_FOLDER_NAME = "HTTPEPM";
        private const string METRIC_FLOWMAP_FOLDER_NAME = "FLOW";

        // Metadata file names
        private const string EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME = "configuration.xml";
        private const string EXTRACT_CONFIGURATION_CONTROLLER_FILE_NAME = "settings.json";
        private const string EXTRACT_ENTITY_APPLICATIONS_FILE_NAME = "applications.json";
        private const string EXTRACT_ENTITY_APPLICATION_FILE_NAME = "application.json";
        private const string EXTRACT_ENTITY_TIERS_FILE_NAME = "tiers.json";
        private const string EXTRACT_ENTITY_NODES_FILE_NAME = "nodes.json";
        private const string EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.json";
        private const string EXTRACT_ENTITY_BACKENDS_FILE_NAME = "backends.json";
        private const string EXTRACT_ENTITY_SERVICE_ENDPOINTS_ALL_FILE_NAME = "serviceendpoints.json";
        private const string EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME = "serviceendpoints.{0}.json";
        private const string EXTRACT_ENTITY_ERRORS_FILE_NAME = "errors.{0}.json";
        private const string EXTRACT_ENTITY_NAME_FILE_NAME = "name.json";

        // Metric file names
        private const string EXTRACT_METRIC_FULL_FILE_NAME = "full.{0}-{1}.json";
        private const string EXTRACT_METRIC_HOUR_FILE_NAME = "hour.{0}-{1}.json";

        // Flowmap file names
        private const string EXTRACT_ENTITY_FLOWMAP_FILE_NAME = "flowmap.{0}-{1}.json";

        // Snapshots file names
        private const string EXTRACT_SNAPSHOTS_FILE_NAME = "snapshots.{0}-{1}.json";
        private const int SNAPSHOTS_QUERY_PAGE_SIZE = 1000;

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

        #endregion

        #region Constants for the folder and file names of data convert

        // Intermediate report conversion file names
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

        #endregion

        #region Constants for the folder and file names of data report

        // Report values
        private const string REPORT_ENTITIES_FILE_NAME = "{0}.Rpt.Entities.xlsx";
        private const string REPORT_CONFIGS_FILE_NAME = "{0}.Configuration.xlsx";
        private const string REPORT_SNAPSHOTS_FILE_NAME = "{0}.Snapshots.xlsx";

        private const string REPORT_ENTITY_METRICS_APPLICATIONS_FILE_NAME = "{0}.Metrics.Applications.xlsx";
        private const string REPORT_ENTITY_METRICS_TIERS_FILE_NAME = "{0}.Metrics.Tiers.xlsx";
        private const string REPORT_ENTITY_METRICS_NODES_FILE_NAME = "{0}.Metrics.Nodes.xlsx";
        private const string REPORT_ENTITY_METRICS_BUSINESS_TRANSACTIONS_FILE_NAME = "{0}.Metrics.BusinessTransactions.xlsx";
        private const string REPORT_ENTITY_METRICS_BACKENDS_FILE_NAME = "{0}.Metrics.Backends.xlsx";
        private const string REPORT_ENTITY_METRICS_SERVICE_ENDPOINTS_FILE_NAME = "{0}.Metrics.ServiceEndpoints.xlsx";
        private const string REPORT_ENTITY_METRICS_ERRORS_FILE_NAME = "{0}.Metrics.Errors.xlsx";

        private const string REPORT_ENTITY_FLOWMAPS_APPLICATIONS_FILE_NAME = "{0}.DepsAndGraphs.Applications.{1}.xlsx";
        private const string REPORT_ENTITY_FLOWMAPS_TIERS_FILE_NAME = "{0}.DepsAndGraphs.Tiers.{1}.xlsx";
        private const string REPORT_ENTITY_FLOWMAPS_NODES_FILE_NAME = "{0}.DepsAndGraphs.Nodes.{1}.xlsx";
        private const string REPORT_ENTITY_FLOWMAPS_BUSINESS_TRANSACTIONS_FILE_NAME = "{0}.DepsAndGraphs.BusinessTransactions.{1}.xlsx";
        private const string REPORT_ENTITY_FLOWMAPS_BACKENDS_FILE_NAME = "{0}.DepsAndGraphs.Backends.{1}.xlsx";
        private const string REPORT_ENTITY_FLOWMAPS_SERVICE_ENDPOINTS_FILE_NAME = "{0}.DepsAndGraphs.ServiceEndpoints.{1}.xlsx";
        private const string REPORT_ENTITY_FLOWMAPS_ERRORS_FILE_NAME = "{0}.DepsAndGraphs.Errors.{1}.xlsx";

        private const string REPORT_SNAPSHOT_FLOWMAP_FILE_NAME = "{0}.{1}.{2}.{3}.Snap.xlsx";

        #endregion

        #region Constants for Report generation Excel contents

        private const string REPORT_PARAMETERS_SHEET_NAME = "1.Report.Parameters";
        private const string REPORT_TOC_SHEET_NAME = "2.TOC";       
        private const string REPORT_CONTROLLERS_LIST_SHEET_NAME = "3.Controllers";
        private const string REPORT_APPLICATIONS_LIST_SHEET_NAME = "4.Applications";
        private const string REPORT_TIERS_LIST_SHEET_NAME = "5.Tiers";
        private const string REPORT_TIERS_PIVOT_SHEET_NAME = "5.Tiers.Pivot";
        private const string REPORT_NODES_LIST_SHEET_NAME = "6.Nodes";
        private const string REPORT_NODES_PIVOT_APPAGENT_SHEET_NAME = "6.Nodes.Type.AppAgent";
        private const string REPORT_NODES_PIVOT_MACHINEAGENT_SHEET_NAME = "6.Nodes.Type.MachineAgent";
        private const string REPORT_BACKENDS_LIST_SHEET_NAME = "7.Backends";
        private const string REPORT_BACKENDS_PIVOT_SHEET_NAME = "7.Backends.Pivot";
        private const string REPORT_BUSINESS_TRANSACTIONS_LIST_SHEET_NAME = "8.BTs";
        private const string REPORT_BUSINESS_TRANSACTIONS_PIVOT_SHEET_NAME = "8.BTs.Pivot";
        private const string REPORT_SERVICE_ENDPOINTS_LIST_SHEET_NAME = "9.SEPs";
        private const string REPORT_SERVICE_ENDPOINTS_PIVOT_SHEET_NAME = "9.SEPs.Pivot";
        private const string REPORT_ERRORS_LIST_SHEET_NAME = "10.Errors";
        private const string REPORT_ERRORS_PIVOT_SHEET_NAME = "10.Errors.Pivot";

        private const string REPORT_TOC_TABLE_NAME = "t_TableOfContents";
        private const string REPORT_PARAMETERS_TARGETS_TABLE_NAME = "t_InputTargets";
        private const string REPORT_CONTROLLERS_TABLE_NAME = "t_Controllers";
        private const string REPORT_APPLICATIONS_TABLE_NAME = "t_Applications";
        private const string REPORT_TIERS_TABLE_NAME = "t_Tiers";
        private const string REPORT_NODES_TABLE_NAME = "t_Nodes";
        private const string REPORT_BACKENDS_TABLE_NAME = "t_Backends";
        private const string REPORT_BUSINESS_TRANSACTIONS_TABLE_NAME = "t_BusinessTransactions";
        private const string REPORT_SERVICE_ENDPOINTS_TABLE_NAME = "t_ServiceEndpoints";
        private const string REPORT_ERRORS_TABLE_NAME = "t_Errors";

        private const string REPORT_TIERS_PIVOT_NAME = "p_Tiers";
        private const string REPORT_NODES_APPAGENT_PIVOT_NAME = "p_NodesAppAgent";
        private const string REPORT_NODES_MACHINEAGENT_PIVOT_NAME = "p_NodesMachineAgent";
        private const string REPORT_BACKENDS_PIVOT_NAME = "p_Backends";
        private const string REPORT_BUSINESS_TRANSACTIONS_PIVOT_NAME = "p_BusinessTransactions";
        private const string REPORT_SERVICE_ENDPOINTS_PIVOT_NAME = "p_ServiceEndpoints";
        private const string REPORT_ERRORS_PIVOT_NAME = "p_Errors";

        private const int REPORT_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_PIVOT_SHEET_START_PIVOT_AT = 6;

        #endregion

        internal static void startOrContinueJob(ProgramOptions programOptions)
        {
            extractData(programOptions);

            convertData(programOptions);

            createReport(programOptions);
        }

        private static void extractData(ProgramOptions programOptions)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Load job configuration
                JobConfiguration jobConfiguration = JobConfigurationHelper.readJobConfigurationFromFile(programOptions.OutputJobFilePath);
                if (jobConfiguration == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to load job input file={0} ", programOptions.OutputJobFilePath);
                    Console.ResetColor();

                    return;
                }
            
                Console.WriteLine("Extract data ({0})", jobConfiguration.Status);

                #region Output diagnostic parameters to log

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOB_STATUS_INFORMATION,
                    "ProcessJob.extractData",
                    String.Format("Job status='{0}', to execute need status='{1}'", jobConfiguration.Status, JobStatus.Extract));

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOB_INPUT_AND_OUTPUT_PARAMETERS,
                    "ProcessJob.extractData",
                    String.Format("Job input: TimeRange.From='{0:o}', TimeRange.To='{1:o}', ExpandedTimeRange.From='{2:o}', ExpandedTimeRange.To='{3:o}', Time ranges='{4}', Flowmaps='{5}', Metrics='{6}', Snapshots='{7}', Configuration='{8}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To, jobConfiguration.Input.HourlyTimeRanges.Count, jobConfiguration.Input.Flowmaps, jobConfiguration.Input.Metrics, jobConfiguration.Input.Snapshots, jobConfiguration.Input.Configuration));

                foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Verbose,
                        EventId.JOB_INPUT_AND_OUTPUT_PARAMETERS,
                        "ProcessJob.extractData",
                        String.Format("Expanded time ranges: From='{0:o}', To='{1:o}'", jobTimeRange.From, jobTimeRange.To));
                }

                #endregion

                // If not the expected status, move on
                if (jobConfiguration.Status != JobStatus.Extract)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipping");
                    Console.ResetColor();

                    return;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Extracting [{0}/{1}], {2} {3} ({4})", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.Status);
                        Console.ResetColor();

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, jobTarget.UserPassword);

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string configFolderPath = Path.Combine(applicationFolderPath, CONFIGURATION_FOLDER_NAME);
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);
                        string snapshotsFolderPath = Path.Combine(applicationFolderPath, SNAPSHOTS_FOLDER_NAME);

                        // Entity files
                        string applicationsFilePath = Path.Combine(controllerFolderPath, EXTRACT_ENTITY_APPLICATIONS_FILE_NAME);
                        string applicationConfigFilePath = Path.Combine(configFolderPath, EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME);
                        string controllerSettingsFilePath = Path.Combine(controllerFolderPath, EXTRACT_CONFIGURATION_CONTROLLER_FILE_NAME);
                        string applicationFilePath = Path.Combine(applicationFolderPath, EXTRACT_ENTITY_APPLICATION_FILE_NAME);
                        string tiersFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_TIERS_FILE_NAME);
                        string nodesFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_NODES_FILE_NAME);
                        string backendsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BACKENDS_FILE_NAME);
                        string businessTransactionsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
                        string serviceEndPointsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
                        string serviceEndPointsAllFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_SERVICE_ENDPOINTS_ALL_FILE_NAME);
                        string errorsFilePath = Path.Combine(entitiesFolderPath, EXTRACT_ENTITY_ERRORS_FILE_NAME);

                        #endregion

                        #region Target states check

                        List<JobTargetStatus> validStates = new List<JobTargetStatus>();
                        validStates.Add(JobTargetStatus.Extract);
                        validStates.Add(JobTargetStatus.ExtractApplications);
                        validStates.Add(JobTargetStatus.ExtractEntities);
                        validStates.Add(JobTargetStatus.ExtractConfiguration);
                        validStates.Add(JobTargetStatus.ExtractMetrics);
                        validStates.Add(JobTargetStatus.ExtractFlowmaps);
                        validStates.Add(JobTargetStatus.ExtractSnapshots);

                        if (validStates.Contains(jobTarget.Status) == false)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Target in invalid state, skipping");
                            Console.ResetColor();

                            continue;
                        }

                        #endregion

                        #region Extract

                        if (jobTarget.Status == JobTargetStatus.Extract)
                        {
                            jobTarget.Status = JobTargetStatus.ExtractApplications;
                        }

                        #endregion

                        #region ExtractApplications

                            if (jobTarget.Status == JobTargetStatus.ExtractApplications)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.extractData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            Console.WriteLine("List of Applications");

                            // All applications
                            string applicationsJSON = controllerApi.GetListOfApplications();
                            if (applicationsJSON != String.Empty) saveFileToFolder(applicationsJSON, applicationsFilePath);

                            jobTarget.Status = JobTargetStatus.ExtractEntities;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ExtractEntities

                        if (jobTarget.Status == JobTargetStatus.ExtractEntities)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.extractData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            #region Application

                            // Application
                            Console.WriteLine("Extract Application");

                            string applicationJSON = controllerApi.GetSingleApplication(jobTarget.Application);
                            if (applicationJSON != String.Empty) saveFileToFolder(applicationJSON, applicationFilePath);

                            #endregion

                            #region Tiers

                            // Tiers
                            Console.WriteLine("Extract Tiers");

                            string tiersJSON = controllerApi.GetListOfTiers(jobTarget.Application);
                            if (tiersJSON != String.Empty) saveFileToFolder(tiersJSON, tiersFilePath);

                            #endregion

                            #region Nodes

                            // Nodes
                            Console.WriteLine("Extract Nodes");

                            string nodesJSON = controllerApi.GetListOfNodes(jobTarget.Application);
                            if (nodesJSON != String.Empty) saveFileToFolder(nodesJSON, nodesFilePath);

                            #endregion

                            #region Backends

                            // Backends
                            Console.WriteLine("Extract Backends");

                            string backendsJSON = controllerApi.GetListOfBackends(jobTarget.Application);
                            if (backendsJSON != String.Empty) saveFileToFolder(backendsJSON, backendsFilePath);

                            #endregion

                            #region Business Transactions

                            // Business Transactions
                            Console.WriteLine("Extract Business Transactions");

                            string businessTransactionsJSON = controllerApi.GetListOfBusinessTransactions(jobTarget.Application);
                            if (businessTransactionsJSON != String.Empty) saveFileToFolder(businessTransactionsJSON, businessTransactionsFilePath);

                            #endregion

                            #region Service Endpoints

                            List<AppDRESTTier> tiersList = loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                            if (tiersList != null)
                            {
                                // Service Endpoints
                                Console.WriteLine("Extract Service Endpoints ({0} tiers)", tiersList.Count());

                                controllerApi.PrivateApiLogin();
                                string serviceEndPointsJSON = controllerApi.GetListOfServiceEndpoints(jobTarget.ApplicationID);
                                if (serviceEndPointsJSON != String.Empty) saveFileToFolder(serviceEndPointsJSON, serviceEndPointsAllFilePath);

                                foreach (AppDRESTTier tier in tiersList)
                                {
                                    Console.Write(".");
                                    serviceEndPointsJSON = controllerApi.GetListOfServiceEndpoints(jobTarget.Application, tier.name);
                                    string serviceEndPointsForThisEntryFilePath = String.Format(serviceEndPointsFilePath, getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                    if (serviceEndPointsJSON != String.Empty) saveFileToFolder(serviceEndPointsJSON, serviceEndPointsForThisEntryFilePath);
                                }
                                Console.WriteLine();
                            }

                            #endregion

                            #region Errors

                            if (tiersList != null)
                            {
                                // Errors
                                Console.WriteLine("Extract Errors ({0} tiers)", tiersList.Count());

                                foreach (AppDRESTTier tier in tiersList)
                                {
                                    Console.Write(".");
                                    string errorsJSON = controllerApi.GetListOfErrors(jobTarget.Application, tier.name);
                                    string errorsForThisEntryFilePath = String.Format(errorsFilePath, getShortenedEntityNameForFileSystem(tier.name, tier.id));
                                    if (errorsJSON != String.Empty) saveFileToFolder(errorsJSON, errorsForThisEntryFilePath);
                                }
                                Console.WriteLine();
                            }

                            #endregion

                            jobTarget.Status = JobTargetStatus.ExtractConfiguration;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ExtractConfig

                        if (jobTarget.Status == JobTargetStatus.ExtractConfiguration)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.extractData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            if (jobConfiguration.Input.Configuration == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Configuration: input says to skip");
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.WriteLine("Extract Controller Configuration");

                                string controllerSettingsJSON = controllerApi.GetControllerConfiguration();
                                if (controllerSettingsJSON != String.Empty) saveFileToFolder(controllerSettingsJSON, controllerSettingsFilePath);

                                Console.WriteLine("Extract Application Configuration");

                                // Application configuration
                                string applicationConfigXml = controllerApi.GetApplicationConfiguration(jobTarget.ApplicationID);
                                if (applicationConfigXml != String.Empty) saveFileToFolder(applicationConfigXml, applicationConfigFilePath);
                            }

                            jobTarget.Status = JobTargetStatus.ExtractMetrics;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ExtractMetrics

                        if (jobTarget.Status == JobTargetStatus.ExtractMetrics)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.extractData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            if (jobConfiguration.Input.Metrics == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Metrics: input says to skip");
                                Console.ResetColor();
                            }
                            else
                            {
                                string metricsEntityFolderPath = String.Empty;
                                string metricsDataFilePath = String.Empty;
                                string metricsJson = String.Empty;

                                #region Application

                                // Application
                                Console.WriteLine("Extract Metrics for Application ({0} entities * {1} time ranges * {2} metrics = {3})", 1, jobConfiguration.Input.HourlyTimeRanges.Count + 1, 5, 5 * (jobConfiguration.Input.HourlyTimeRanges.Count + 1));

                                metricsEntityFolderPath = Path.Combine(metricsFolderPath, APPLICATION_FOLDER_NAME);

                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_APPLICATION, METRIC_ART), jobConfiguration, metricsEntityFolderPath, METRIC_ART_FOLDER_NAME);
                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_APPLICATION, METRIC_CPM), jobConfiguration, metricsEntityFolderPath, METRIC_CPM_FOLDER_NAME);
                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_APPLICATION, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);
                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_APPLICATION, METRIC_EXCPM), jobConfiguration, metricsEntityFolderPath, METRIC_EXCPM_FOLDER_NAME);
                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_APPLICATION, METRIC_HTTPEPM), jobConfiguration, metricsEntityFolderPath, METRIC_HTTPEPM_FOLDER_NAME);

                                Console.WriteLine();

                                #endregion

                                #region Tiers

                                List<AppDRESTTier> tiersList = loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                                if (tiersList != null)
                                {
                                    Console.WriteLine("Extract Metrics for Tiers ({0} entities * {1} time ranges * {2} metrics = {3})", tiersList.Count, jobConfiguration.Input.HourlyTimeRanges.Count + 1, 5, 5 * tiersList.Count * (jobConfiguration.Input.HourlyTimeRanges.Count + 1));

                                    // Todo debug
                                    // craps out httpclient sometimes
                                    // output is no good either
                                    //Parallel.ForEach(tiersList, (tier) =>
                                    //{

                                    //    string metricsEntityFolderPathLocal = Path.Combine(metricsFolderPath, TIERS_FOLDER_NAME, getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                    //    getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_ART), jobConfiguration, metricsEntityFolderPathLocal, METRIC_ART_FOLDER_NAME);
                                    //    getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_CPM), jobConfiguration, metricsEntityFolderPathLocal, METRIC_CPM_FOLDER_NAME);
                                    //    getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPathLocal, METRIC_EPM_FOLDER_NAME);
                                    //    getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_EXCPM), jobConfiguration, metricsEntityFolderPathLocal, METRIC_EXCPM_FOLDER_NAME);
                                    //    getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_HTTPEPM), jobConfiguration, metricsEntityFolderPathLocal, METRIC_FOLDER_HTTPEPM);

                                    //    j++;
                                    //    if (j % 10 == 0)
                                    //    {
                                    //        Console.WriteLine("{0} entities", j);
                                    //    }
                                    //});
                                    //Console.WriteLine("{0} entities", -1);

                                    int j = 0;

                                    foreach (AppDRESTTier tier in tiersList)
                                    {
                                        metricsEntityFolderPath = Path.Combine(
                                            metricsFolderPath,
                                            TIERS_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_ART), jobConfiguration, metricsEntityFolderPath, METRIC_ART_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_CPM), jobConfiguration, metricsEntityFolderPath, METRIC_CPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_EXCPM), jobConfiguration, metricsEntityFolderPath, METRIC_EXCPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_TIER, tier.name, METRIC_HTTPEPM), jobConfiguration, metricsEntityFolderPath, METRIC_HTTPEPM_FOLDER_NAME);

                                        writeJSONObjectToFile(tier, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Nodes

                                List<AppDRESTNode> nodesList = loadListOfObjectsFromFile<AppDRESTNode>(nodesFilePath);
                                if (nodesList != null)
                                {
                                    Console.WriteLine("Extract Metrics for Nodes ({0} entities * {1} time ranges * {2} metrics = {3})", nodesList.Count, jobConfiguration.Input.HourlyTimeRanges.Count + 1, 5, 5 * nodesList.Count * (jobConfiguration.Input.HourlyTimeRanges.Count + 1));

                                    int j = 0;

                                    foreach (AppDRESTNode node in nodesList)
                                    {
                                        metricsEntityFolderPath = Path.Combine(
                                            metricsFolderPath,
                                            NODES_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                                            getShortenedEntityNameForFileSystem(node.name, node.id));

                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_ART), jobConfiguration, metricsEntityFolderPath, METRIC_ART_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_CPM), jobConfiguration, metricsEntityFolderPath, METRIC_CPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_EXCPM), jobConfiguration, metricsEntityFolderPath, METRIC_EXCPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_NODE, node.tierName, node.name, METRIC_HTTPEPM), jobConfiguration, metricsEntityFolderPath, METRIC_HTTPEPM_FOLDER_NAME);

                                        writeJSONObjectToFile(node, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Backends

                                List<AppDRESTBackend> backendsList = loadListOfObjectsFromFile<AppDRESTBackend>(backendsFilePath);
                                if (backendsList != null)
                                {
                                    Console.WriteLine("Extract Metrics for Backends ({0} entities * {1} time ranges * {2} metrics = {3})", backendsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 3, 3 * backendsList.Count * (jobConfiguration.Input.HourlyTimeRanges.Count + 1));

                                    int j = 0;

                                    foreach (AppDRESTBackend backend in backendsList)
                                    {
                                        metricsEntityFolderPath = Path.Combine(
                                            metricsFolderPath,
                                            BACKENDS_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(backend.name, backend.id));

                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_BACKEND, backend.name, METRIC_ART), jobConfiguration, metricsEntityFolderPath, METRIC_ART_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_BACKEND, backend.name, METRIC_CPM), jobConfiguration, metricsEntityFolderPath, METRIC_CPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_BACKEND, backend.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);

                                        writeJSONObjectToFile(backend, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Business Transactions

                                List<AppDRESTBusinessTransaction> businessTransactionsList = loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);
                                if (businessTransactionsList != null)
                                {
                                    Console.WriteLine("Extract Metrics for Business Transactions ({0} entities * {1} time ranges * {2} metrics = {3})", businessTransactionsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 3, 3 * businessTransactionsList.Count * (jobConfiguration.Input.HourlyTimeRanges.Count + 1));

                                    int j = 0;

                                    foreach (AppDRESTBusinessTransaction businessTransaction in businessTransactionsList)
                                    {
                                        metricsEntityFolderPath = Path.Combine(
                                            metricsFolderPath,
                                            BUSINESS_TRANSACTIONS_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(businessTransaction.tierName, businessTransaction.tierId),
                                            getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id));

                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_BUSINESS_TRANSACTION, businessTransaction.tierName, businessTransaction.name, METRIC_ART), jobConfiguration, metricsEntityFolderPath, METRIC_ART_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_BUSINESS_TRANSACTION, businessTransaction.tierName, businessTransaction.name, METRIC_CPM), jobConfiguration, metricsEntityFolderPath, METRIC_CPM_FOLDER_NAME);
                                        getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_BUSINESS_TRANSACTION, businessTransaction.tierName, businessTransaction.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);

                                        writeJSONObjectToFile(businessTransaction, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Service Endpoints

                                if (tiersList != null)
                                {
                                    JObject serviceEndpointsAll = loadObjectFromFile(serviceEndPointsAllFilePath);
                                    JArray serviceEndpointsDetail = null;
                                    if (serviceEndpointsAll != null)
                                    {
                                        serviceEndpointsDetail = (JArray)serviceEndpointsAll["serviceEndpointListEntries"];
                                    }

                                    foreach (AppDRESTTier tier in tiersList)
                                    {
                                        string serviceEndPointsForThisEntryFilePath = String.Format(serviceEndPointsFilePath, getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                        List<AppDRESTMetricFolder> serviceEndpointsList = loadListOfObjectsFromFile<AppDRESTMetricFolder>(serviceEndPointsForThisEntryFilePath);
                                        if (serviceEndpointsList != null)
                                        {
                                            Console.WriteLine("Extract Metrics for Service Endpoints in Tier {4}, ({0} entities * {1} time ranges * {2} metrics = {3})", serviceEndpointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 3, 3 * serviceEndpointsList.Count * (jobConfiguration.Input.HourlyTimeRanges.Count + 1), tier.name);

                                            int j = 0;

                                            foreach (AppDRESTMetricFolder serviceEndpoint in serviceEndpointsList)
                                            {
                                                // Look up the ID of SEP
                                                JObject serviceEndpointDetail = (JObject)serviceEndpointsDetail.Where(sep => (string)sep["name"] == serviceEndpoint.name && (int)sep["applicationComponentId"] == tier.id).FirstOrDefault();
                                                int serviceEndpointId = -1;
                                                if (serviceEndpointDetail != null) serviceEndpointId = (int)serviceEndpointDetail["id"];

                                                metricsEntityFolderPath = Path.Combine(
                                                    metricsFolderPath,
                                                    SERVICE_ENDPOINTS_FOLDER_NAME,
                                                    getShortenedEntityNameForFileSystem(tier.name, tier.id),
                                                    getShortenedEntityNameForFileSystem(serviceEndpoint.name, serviceEndpointId));

                                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_SERVICE_ENDPOINT, tier.name, serviceEndpoint.name, METRIC_ART), jobConfiguration, metricsEntityFolderPath, METRIC_ART_FOLDER_NAME);
                                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_SERVICE_ENDPOINT, tier.name, serviceEndpoint.name, METRIC_CPM), jobConfiguration, metricsEntityFolderPath, METRIC_CPM_FOLDER_NAME);
                                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_SERVICE_ENDPOINT, tier.name, serviceEndpoint.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);

                                                writeJSONObjectToFile(serviceEndpoint, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                                j++;
                                                if (j % 10 == 0)
                                                {
                                                    Console.Write("[{0}]", j);
                                                }
                                            }
                                            Console.WriteLine("{0} entities", j);
                                        }
                                    }
                                }

                                #endregion

                                #region Errors

                                if (tiersList != null)
                                {
                                    foreach (AppDRESTTier tier in tiersList)
                                    {
                                        string errorsForThisEntryFilePath = String.Format(errorsFilePath, getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                        List<AppDRESTMetricFolder> errorsList = loadListOfObjectsFromFile<AppDRESTMetricFolder>(errorsForThisEntryFilePath);
                                        if (errorsList != null)
                                        {
                                            Console.WriteLine("Extract Metrics for Errors in Tier {4}, ({0} entities * {1} time ranges * {2} metrics = {3})", errorsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count, 1, 1 * errorsList.Count * (jobConfiguration.Input.HourlyTimeRanges.Count + 1), tier.name);

                                            int j = 0;

                                            foreach (AppDRESTMetricFolder error in errorsList)
                                            {
                                                metricsEntityFolderPath = Path.Combine(
                                                    metricsFolderPath,
                                                    ERRORS_FOLDER_NAME,
                                                    getShortenedEntityNameForFileSystem(tier.name, tier.id),
                                                    getShortenedEntityNameForFileSystem(error.name, -1));

                                                getMetricDataForMetricForAllRanges(controllerApi, jobTarget.Application, String.Format(METRIC_PATH_ERROR, tier.name, error.name, METRIC_EPM), jobConfiguration, metricsEntityFolderPath, METRIC_EPM_FOLDER_NAME);

                                                writeJSONObjectToFile(error, Path.Combine(metricsEntityFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                                j++;
                                                if (j % 10 == 0)
                                                {
                                                    Console.Write("[{0}]", j);
                                                }
                                            }
                                            Console.WriteLine("{0} entities", j);
                                        }
                                    }
                                }

                                #endregion
                            }

                            jobTarget.Status = JobTargetStatus.ExtractFlowmaps;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ExtractFlowmaps

                        if (jobTarget.Status == JobTargetStatus.ExtractFlowmaps)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.extractData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            if (jobConfiguration.Input.Flowmaps == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Flowmaps: input says to skip");
                                Console.ResetColor();
                            }
                            else
                            {
                                // Login into private API
                                controllerApi.PrivateApiLogin();

                                long fromTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.From);
                                long toTimeUnix = convertToUnixTimestamp(jobConfiguration.Input.ExpandedTimeRange.To);
                                long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                                string flowmapDataFilePath = String.Empty;
                                string flowmapJson = String.Empty;

                                #region Application

                                // Application
                                Console.WriteLine("Extract Flowmap for Application");

                                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                    TraceEventType.Information,
                                    EventId.FLOWMAP_RETRIEVAL,
                                    "ProcessJob.extractData",
                                    String.Format("Retrieving flowmap for Application='{0}', Application, From='{1:o}', To='{2:o}'", jobTarget.Application, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                                flowmapDataFilePath = Path.Combine(
                                    metricsFolderPath,
                                    APPLICATION_FOLDER_NAME,
                                    METRIC_FLOWMAP_FOLDER_NAME,
                                    String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From.ToString("yyyyMMddHHmm"), jobConfiguration.Input.ExpandedTimeRange.To.ToString("yyyyMMddHHmm")));

                                if (File.Exists(flowmapDataFilePath) == false)
                                {
                                    flowmapJson = controllerApi.GetFlowmapApplication(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (flowmapJson != String.Empty) saveFileToFolder(flowmapJson, flowmapDataFilePath);
                                }

                                #endregion

                                #region Tiers

                                List<AppDRESTTier> tiersList = loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                                if (tiersList != null)
                                {
                                    Console.WriteLine("Extract Flowmaps for Tiers ({0} entities)", tiersList.Count);

                                    int j = 0;

                                    foreach (AppDRESTTier tier in tiersList)
                                    {
                                        Console.Write(".");

                                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                            TraceEventType.Information,
                                            EventId.FLOWMAP_RETRIEVAL,
                                            "ProcessJob.extractData",
                                            String.Format("Retrieving flowmap for Application='{0}', Tier='{1}', From='{2:o}', To='{3:o}'", jobTarget.Application, tier.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                                        flowmapDataFilePath = Path.Combine(
                                            metricsFolderPath,
                                            TIERS_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(tier.name, tier.id),
                                            METRIC_FLOWMAP_FOLDER_NAME,
                                            String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From.ToString("yyyyMMddHHmm"), jobConfiguration.Input.ExpandedTimeRange.To.ToString("yyyyMMddHHmm")));

                                        if (File.Exists(flowmapDataFilePath) == false)
                                        {
                                            flowmapJson = controllerApi.GetFlowmapTier(tier.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (flowmapJson != String.Empty) saveFileToFolder(flowmapJson, flowmapDataFilePath);
                                        }

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Nodes

                                List<AppDRESTNode> nodesList = loadListOfObjectsFromFile<AppDRESTNode>(nodesFilePath);
                                if (nodesList != null)
                                {
                                    Console.WriteLine("Extract Flowmaps for Nodes ({0} entities)", nodesList.Count);

                                    int j = 0;

                                    foreach (AppDRESTNode node in nodesList)
                                    {
                                        Console.Write(".");

                                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                            TraceEventType.Information,
                                            EventId.FLOWMAP_RETRIEVAL,
                                            "ProcessJob.extractData",
                                            String.Format("Retrieving flowmap for Application='{0}', Tier='{1}', Node='{2}', From='{2:o}', To='{3:o}'", jobTarget.Application, node.tierName, node.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                                        flowmapDataFilePath = Path.Combine(
                                            metricsFolderPath,
                                            NODES_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                                            getShortenedEntityNameForFileSystem(node.name, node.id),
                                            METRIC_FLOWMAP_FOLDER_NAME,
                                            String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From.ToString("yyyyMMddHHmm"), jobConfiguration.Input.ExpandedTimeRange.To.ToString("yyyyMMddHHmm")));

                                        if (File.Exists(flowmapDataFilePath) == false)
                                        {
                                            flowmapJson = controllerApi.GetFlowmapNode(node.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (flowmapJson != String.Empty) saveFileToFolder(flowmapJson, flowmapDataFilePath);
                                        }

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Backends

                                List<AppDRESTBackend> backendsList = loadListOfObjectsFromFile<AppDRESTBackend>(backendsFilePath);
                                if (backendsList != null)
                                {
                                    Console.WriteLine("Extract Flowmaps for Backends ({0} entities)", backendsList.Count);

                                    int j = 0;

                                    foreach (AppDRESTBackend backend in backendsList)
                                    {
                                        Console.Write(".");

                                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                            TraceEventType.Information,
                                            EventId.FLOWMAP_RETRIEVAL,
                                            "ProcessJob.extractData",
                                            String.Format("Retrieving flowmap for Application='{0}', Backend='{1}', From='{2:o}', To='{3:o}'", jobTarget.Application, backend.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                                        flowmapDataFilePath = Path.Combine(
                                            metricsFolderPath,
                                            BACKENDS_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(backend.name, backend.id),
                                            METRIC_FLOWMAP_FOLDER_NAME,
                                            String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From.ToString("yyyyMMddHHmm"), jobConfiguration.Input.ExpandedTimeRange.To.ToString("yyyyMMddHHmm")));

                                        if (File.Exists(flowmapDataFilePath) == false)
                                        {
                                            flowmapJson = controllerApi.GetFlowmapBackend(backend.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (flowmapJson != String.Empty) saveFileToFolder(flowmapJson, flowmapDataFilePath);
                                        }

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion

                                #region Business Transactions

                                List<AppDRESTBusinessTransaction> businessTransactionsList = loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);
                                if (businessTransactionsList != null)
                                {
                                    Console.WriteLine("Extract Flowmaps for Business Transactions ({0} entities)", businessTransactionsList.Count);

                                    int j = 0;

                                    foreach (AppDRESTBusinessTransaction businessTransaction in businessTransactionsList)
                                    {
                                        Console.Write(".");

                                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                            TraceEventType.Information,
                                            EventId.FLOWMAP_RETRIEVAL,
                                            "ProcessJob.extractData",
                                            String.Format("Retrieving flowmap for Application='{0}', Tier='{1}', Business Transaction='{2}', From='{2:o}', To='{3:o}'", jobTarget.Application, businessTransaction.tierName, businessTransaction.name, jobConfiguration.Input.ExpandedTimeRange.From, jobConfiguration.Input.ExpandedTimeRange.To));

                                        flowmapDataFilePath = Path.Combine(
                                            metricsFolderPath,
                                            BUSINESS_TRANSACTIONS_FOLDER_NAME,
                                            getShortenedEntityNameForFileSystem(businessTransaction.tierName, businessTransaction.tierId),
                                            getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id),
                                            METRIC_FLOWMAP_FOLDER_NAME,
                                            String.Format(EXTRACT_ENTITY_FLOWMAP_FILE_NAME, jobConfiguration.Input.ExpandedTimeRange.From.ToString("yyyyMMddHHmm"), jobConfiguration.Input.ExpandedTimeRange.To.ToString("yyyyMMddHHmm")));

                                        if (File.Exists(flowmapDataFilePath) == false)
                                        {
                                            flowmapJson = controllerApi.GetFlowmapBusinessTransaction(jobTarget.ApplicationID, businessTransaction.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (flowmapJson != String.Empty) saveFileToFolder(flowmapJson, flowmapDataFilePath);
                                        }

                                        j++;
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}]", j);
                                        }
                                    }
                                    Console.WriteLine("{0} entities", j);
                                }

                                #endregion
                            }

                            jobTarget.Status = JobTargetStatus.ExtractSnapshots;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }

                        }

                        #endregion

                        #region ExtractSnapshots

                        if (jobTarget.Status == JobTargetStatus.ExtractSnapshots)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.extractData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            if (jobConfiguration.Input.Snapshots == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Snapshots: input says to skip");
                                Console.ResetColor();
                            }
                            else
                            {
                                // Login into private API
                                controllerApi.PrivateApiLogin();

                                #region List of Snapshots in time ranges

                                Console.WriteLine("Extract List of Snapshots ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                                // Get list of snapshots in each time range
                                int totalSnapshotsFound = 0;
                                foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                                {
                                    Console.Write("Extract List of Snapshots from {0:o} to {1:o}", jobTimeRange.From, jobTimeRange.To);

                                    string snapshotsFilePath = Path.Combine(snapshotsFolderPath, String.Format(EXTRACT_SNAPSHOTS_FILE_NAME, jobTimeRange.From.ToString("yyyyMMddHHmm"), jobTimeRange.To.ToString("yyyyMMddHHmm")));

                                    long fromTimeUnix = convertToUnixTimestamp(jobTimeRange.From);
                                    long toTimeUnix = convertToUnixTimestamp(jobTimeRange.To);
                                    int differenceInMinutes = (int)(jobTimeRange.To - jobTimeRange.From).TotalMinutes;

                                    if (File.Exists(snapshotsFilePath) == false)
                                    {
                                        JArray listOfSnapshots = new JArray();

                                        // Extract snapshot list
                                        long serverCursorId = 0;
                                        do
                                        {
                                            string snapshotsJSON = controllerApi.GetListOfSnapshots(jobTarget.ApplicationID, jobTimeRange.From, jobTimeRange.To, differenceInMinutes, SNAPSHOTS_QUERY_PAGE_SIZE, serverCursorId);

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
                                                }
                                                if (serverCursorIdObj == null)
                                                {
                                                    serverCursorId = 0;
                                                }
                                                else
                                                {
                                                    serverCursorId = -1;
                                                    Int64.TryParse(serverCursorIdObj.ToString(), out serverCursorId);
                                                }

                                                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                                    TraceEventType.Information,
                                                    EventId.SNAPSHOT_LIST_RETRIEVAL,
                                                    "ProcessJob.extractData",
                                                    String.Format("Controller='{0}', Application='{1}', From='{2:o}', To='{3:o}', Snapshots='{4}', CursorId='{5}'", jobTarget.Controller, jobTarget.Application, jobTimeRange.From, jobTimeRange.To, snapshots.Count, serverCursorId));

                                                Console.Write("+{0}", listOfSnapshots.Count);
                                            }
                                        }
                                        while (serverCursorId > 0);

                                        Console.Write("={0} snapshots", listOfSnapshots.Count);
                                        totalSnapshotsFound = totalSnapshotsFound + listOfSnapshots.Count;

                                        writeJArrayToFile(listOfSnapshots, snapshotsFilePath);
                                    }
                                    Console.WriteLine();
                                }

                                Console.WriteLine("Found total {0} snapshots", totalSnapshotsFound);

                                #endregion

                                #region Individual Snapshots

                                // Extract individual snapshots
                                Console.WriteLine("Extract Individual Snapshots");

                                // Load lookups for Tiers and Business Transactions
                                List<AppDRESTTier> tiersList = loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                                List<AppDRESTBusinessTransaction> businessTransactionsList = loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);

                                if (tiersList != null && businessTransactionsList != null)
                                {
                                    int totalSnapshotsFirstInChainSegments = 0;
                                    int totalSnapshotsSegments = 0;

                                    foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                                    {
                                        string snapshotsFilePath = Path.Combine(snapshotsFolderPath, String.Format(EXTRACT_SNAPSHOTS_FILE_NAME, jobTimeRange.From.ToString("yyyyMMddHHmm"), jobTimeRange.To.ToString("yyyyMMddHHmm")));
                                        JArray listOfSnapshotsInHour = loadArrayFromFile(snapshotsFilePath);
                                        if (listOfSnapshotsInHour != null && listOfSnapshotsInHour.Count > 0)
                                        {
                                            Console.WriteLine("Extract Snapshots {0:o} to {1:o} ({2} snapshots)", jobTimeRange.From, jobTimeRange.To, listOfSnapshotsInHour.Count);

                                            int j = 0;

                                            foreach (JToken snapshot in listOfSnapshotsInHour)
                                            {
                                                totalSnapshotsSegments++;

                                                // Only retrieve first in chain snapshots. The rest will be picked up from the other calls
                                                if ((bool)snapshot["firstInChain"] == false)
                                                {
                                                    Console.Write("-");
                                                }
                                                else
                                                {
                                                    totalSnapshotsFirstInChainSegments++;

                                                    // Look up tiers and business transaction for this snapshot
                                                    AppDRESTTier tier = tiersList.Where<AppDRESTTier>(t => t.id == (int)snapshot["applicationComponentId"]).FirstOrDefault();
                                                    AppDRESTBusinessTransaction businessTransaction = businessTransactionsList.Where<AppDRESTBusinessTransaction>(t => t.id == (int)snapshot["businessTransactionId"]).FirstOrDefault();

                                                    if (tier != null && businessTransaction != null)
                                                    {
                                                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                                            TraceEventType.Information,
                                                            EventId.SNAPSHOT_RETRIEVAL,
                                                            "ProcessJob.extractData",
                                                            String.Format("Retrieving snapshot for Application='{0}', Tier='{1}', Business Transaction='{2}', RequestGUID='{3}'", jobTarget.Application, tier.name, businessTransaction.name, snapshot["requestGUID"]));

                                                        Console.Write(">");

                                                        string snapshotTierFolderPath = Path.Combine(
                                                            snapshotsFolderPath,
                                                            getShortenedEntityNameForFileSystem(tier.name, tier.id));
                                                        writeJSONObjectToFile(tier, Path.Combine(snapshotTierFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                                        string snapshotBusinessTransactionFolderPath = Path.Combine(
                                                            snapshotsFolderPath,
                                                            getShortenedEntityNameForFileSystem(tier.name, tier.id),
                                                            getShortenedEntityNameForFileSystem(businessTransaction.name.ToString(), businessTransaction.id));
                                                        writeJSONObjectToFile(businessTransaction, Path.Combine(snapshotBusinessTransactionFolderPath, EXTRACT_ENTITY_NAME_FILE_NAME));

                                                        DateTime snapshotTime = convertFromUnixTimestamp((long)snapshot["serverStartTime"]);

                                                        string snapshotFolderPath = Path.Combine(
                                                            snapshotsFolderPath,
                                                            getShortenedEntityNameForFileSystem(tier.name, tier.id),
                                                            getShortenedEntityNameForFileSystem(businessTransaction.name.ToString(), businessTransaction.id),
                                                            String.Format("{0}", snapshotTime.ToString("yyyyMMddHH")),
                                                            userExperienceFolderNameMapping[snapshot["userExperience"].ToString()],
                                                            String.Format(SNAPSHOT_FOLDER_NAME, snapshotTime.ToString("yyyyMMddHHmmss"), snapshot["requestGUID"]));

                                                        // Must strip out the milliseconds, because the segment list retireval doesn't seem to like them in the datetimes
                                                        DateTime snapshotTimeFrom = snapshotTime.AddMinutes(-30).AddMilliseconds(snapshotTime.Millisecond * -1);
                                                        DateTime snapshotTimeTo = snapshotTime.AddMinutes(30).AddMilliseconds(snapshotTime.Millisecond * -1);

                                                        long fromTimeUnix = convertToUnixTimestamp(snapshotTimeFrom);
                                                        long toTimeUnix = convertToUnixTimestamp(snapshotTimeTo);
                                                        int differenceInMinutes = (int)(snapshotTimeTo - snapshotTimeFrom).TotalMinutes;

                                                        #region Get Snapshot Flowmap

                                                        Console.Write(".");

                                                        // Get snapshot flow map
                                                        string snapshotFlowmapDataFilePath = Path.Combine(snapshotFolderPath, EXTRACT_SNAPSHOT_FLOWMAP_FILE_NAME);

                                                        if (File.Exists(snapshotFlowmapDataFilePath) == false)
                                                        {
                                                            string snapshotFlowmapJson = controllerApi.GetFlowmapSnapshot(jobTarget.ApplicationID, (int)snapshot["businessTransactionId"], snapshot["requestGUID"].ToString(), fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                            if (snapshotFlowmapJson != String.Empty) saveFileToFolder(snapshotFlowmapJson, snapshotFlowmapDataFilePath);
                                                        }

                                                        #endregion

                                                        #region Get List of Segments

                                                        Console.Write(".");

                                                        // Get list of segments
                                                        string snapshotSegmentsDataFilePath = Path.Combine(snapshotFolderPath, EXTRACT_SNAPSHOT_SEGMENT_LIST_NAME);

                                                        if (File.Exists(snapshotSegmentsDataFilePath) == false)
                                                        {
                                                            string snapshotSegmentsJson = controllerApi.GetSnapshotSegments(snapshot["requestGUID"].ToString(), snapshotTimeFrom, snapshotTimeTo, differenceInMinutes);
                                                            if (snapshotSegmentsJson != String.Empty) saveFileToFolder(snapshotSegmentsJson, snapshotSegmentsDataFilePath);
                                                        }

                                                        #endregion

                                                        #region Get details for each segment

                                                        JArray snapshotSegmentsList = loadArrayFromFile(snapshotSegmentsDataFilePath);

                                                        if (snapshotSegmentsList != null)
                                                        {
                                                            // Get details for segment
                                                            foreach (JToken snapshotSegment in snapshotSegmentsList)
                                                            {
                                                                Console.Write(".");

                                                                string snapshotSegmentDataFilePath = Path.Combine(snapshotFolderPath, String.Format(EXTRACT_SNAPSHOT_SEGMENT_DATA_FILE_NAME, snapshotSegment["id"]));

                                                                if (File.Exists(snapshotSegmentDataFilePath) == false)
                                                                {
                                                                    string snapshotSegmentJson = controllerApi.GetSnapshotSegmentDetails((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                                    if (snapshotSegmentJson != String.Empty) saveFileToFolder(snapshotSegmentJson, snapshotSegmentDataFilePath);
                                                                }
                                                            }

                                                            // Get errors for segment
                                                            foreach (JToken snapshotSegment in snapshotSegmentsList)
                                                            {
                                                                Console.Write(".");

                                                                string snapshotSegmentErrorFilePath = Path.Combine(snapshotFolderPath, String.Format(EXTRACT_SNAPSHOT_SEGMENT_ERROR_FILE_NAME, snapshotSegment["id"]));

                                                                if (File.Exists(snapshotSegmentErrorFilePath) == false)
                                                                {
                                                                    string snapshotSegmentJson = controllerApi.GetSnapshotSegmentErrors((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                                    if (snapshotSegmentJson != String.Empty) saveFileToFolder(snapshotSegmentJson, snapshotSegmentErrorFilePath);
                                                                }
                                                            }

                                                            // Get call graphs for segment
                                                            foreach (JToken snapshotSegment in snapshotSegmentsList)
                                                            {
                                                                Console.Write(".");

                                                                string snapshotSegmentCallGraphFilePath = Path.Combine(snapshotFolderPath, String.Format(EXTRACT_SNAPSHOT_SEGMENT_CALLGRAPH_FILE_NAME, snapshotSegment["id"]));

                                                                if (File.Exists(snapshotSegmentCallGraphFilePath) == false)
                                                                {
                                                                    string snapshotSegmentJson = controllerApi.GetSnapshotSegmentCallGraph((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                                    if (snapshotSegmentJson != String.Empty) saveFileToFolder(snapshotSegmentJson, snapshotSegmentCallGraphFilePath);
                                                                }
                                                            }
                                                        }

                                                        #endregion
                                                    }

                                                    j++;
                                                    if (j % 10 == 0)
                                                    {
                                                        Console.Write("[{0}]", j);
                                                    }
                                                }
                                            }
                                            Console.WriteLine("{0} snapshots", listOfSnapshotsInHour.Count);
                                        }
                                    }

                                    Console.WriteLine("Extracted Snapshots: {0} segments total, {1} first in chain segments", totalSnapshotsSegments, totalSnapshotsFirstInChainSegments);
                                }

                                #endregion
                            }

                            jobTarget.Status = JobTargetStatus.Convert;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion
                    }

                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Failed in {0}, skipping this target", jobTarget.Status);
                        Console.WriteLine(ex);
                        Console.ResetColor();

                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Error,
                            EventId.EXCEPTION_GENERIC,
                            "ProcessJob.extractData",
                            ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();
                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Verbose,
                            EventId.FUNCTION_DURATION_EVENT,
                            String.Format("Extracting [{0}/{1}], {2} {3} ({4})", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.Status),
                            String.Format("Execution took {0:c} ({1} ms)", stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("Extracting execution took {0:c} ({1} ms)", stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds));
                        Console.ResetColor();
                    }
                }

                // Move job to next status
                jobConfiguration.Status = JobStatus.Convert;

                // Save the resulting JSON file to the job target folder
                if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                    Console.ResetColor();

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.extractData",
                    ex);
            }
            finally
            {
                stopWatch.Stop();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FUNCTION_DURATION_EVENT,
                    "ProcessJob.extractData",
                    String.Format("Execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(String.Format("Extracting execution overall took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));
                Console.ResetColor();
            }
        }

        private static void convertData(ProgramOptions programOptions)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Load job configuration
                JobConfiguration jobConfiguration = JobConfigurationHelper.readJobConfigurationFromFile(programOptions.OutputJobFilePath);
                if (jobConfiguration == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to load job input file={0} ", programOptions.OutputJobFilePath);
                    Console.ResetColor();

                    return;
                }

                Console.WriteLine("Convert data ({0})", jobConfiguration.Status);

                #region Output diagnostic parameters to log

                // Check current process status
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOB_STATUS_INFORMATION,
                    "ProcessJob.convertData",
                    String.Format("Job status='{0}', to execute need status='{1}'", jobConfiguration.Status, JobStatus.Convert));

                #endregion

                // If not the expected status, move on
                if (jobConfiguration.Status != JobStatus.Convert)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipping");
                    Console.ResetColor();

                    return;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Converting [{0}/{1}], {2} {3} ({4})", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.Status);
                        Console.ResetColor();

                        #region Target step variables

                        // Various folders
                        string controllerFolderPath = Path.Combine(programOptions.OutputJobFolderPath, getFileSystemSafeString(new Uri(jobTarget.Controller).Host));
                        string applicationFolderPath = Path.Combine(controllerFolderPath, getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));
                        string entitiesFolderPath = Path.Combine(applicationFolderPath, ENTITIES_FOLDER_NAME);
                        string configFolderPath = Path.Combine(applicationFolderPath, CONFIGURATION_FOLDER_NAME);
                        string metricsFolderPath = Path.Combine(applicationFolderPath, METRICS_FOLDER_NAME);
                        string snapshotsFolderPath = Path.Combine(applicationFolderPath, SNAPSHOTS_FOLDER_NAME);

                        // Entity files
                        string applicationsFilePath = Path.Combine(controllerFolderPath, EXTRACT_ENTITY_APPLICATIONS_FILE_NAME);
                        string applicationConfigFilePath = Path.Combine(configFolderPath, EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME);
                        string applicationFilePath = Path.Combine(applicationFolderPath, EXTRACT_ENTITY_APPLICATION_FILE_NAME);
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

                        #region Target states check

                        List<JobTargetStatus> validStates = new List<JobTargetStatus>();
                        validStates.Add(JobTargetStatus.Convert);
                        validStates.Add(JobTargetStatus.ConvertApplications);
                        validStates.Add(JobTargetStatus.ConvertEntities);
                        validStates.Add(JobTargetStatus.ConvertConfiguration);
                        validStates.Add(JobTargetStatus.ConvertMetrics);
                        validStates.Add(JobTargetStatus.ConvertFlowmaps);
                        validStates.Add(JobTargetStatus.ConvertSnapshots);

                        if (validStates.Contains(jobTarget.Status) == false)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Target in invalid state, skipping");
                            Console.ResetColor();

                            continue;
                        }

                        #endregion

                        #region Convert

                        if (jobTarget.Status == JobTargetStatus.Convert)
                        {
                            jobTarget.Status = JobTargetStatus.ConvertApplications;
                        }

                        #endregion

                        #region ConvertApplications

                        // Output list of controllers in tabular form
                        if (jobTarget.Status == JobTargetStatus.ConvertApplications)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.convertData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            Console.WriteLine("Convert Applications");

                            // Create this row 
                            ReportControllerRow controllerRow = new ReportControllerRow();
                            controllerRow.Controller = jobTarget.Controller;
                            controllerRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", controllerRow.Controller);
                            controllerRow.UserName = jobTarget.UserName;
                            
                            // Lookup number of applications
                            // Load JSON file from the file system in case we are continuing the step after stopping
                            List<AppDRESTApplication> applicationsList = loadListOfObjectsFromFile<AppDRESTApplication>(applicationsFilePath);
                            if (applicationsList != null)
                            {
                                controllerRow.NumApps = applicationsList.Count;
                            }
                            
                            // Lookup version
                            // Load the configuration.xml from the child to parse the version
                            XmlDocument configXml = loadXmlDocumentFromFile(applicationConfigFilePath);
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

                            List<ReportControllerRow> controllerRows = new List<ReportControllerRow>(1);
                            controllerRows.Add(controllerRow);

                            if (File.Exists(controllerReportFilePath) == false)
                            {
                                writeListToCSVFile(controllerRows, new ReportControllerRowMap(), controllerReportFilePath);
                            }

                            // Append this controller to the list of all controllers
                            List<ReportControllerRow> controllersRows = readListFromCSVFile<ReportControllerRow>(controllersReportFilePath);
                            if (controllersRows == null || controllersRows.Count == 0)
                            {
                                // First time, let's output these rows
                                controllersRows = controllerRows;
                            }
                            else
                            {
                                ReportControllerRow controllerRowExisting = controllersRows.Where(c => c.Controller == controllerRow.Controller).FirstOrDefault();
                                if (controllerRowExisting == null)
                                {
                                    controllersRows.Add(controllerRow);
                                }
                            }
                            controllersRows = controllersRows.OrderBy(o => o.Controller).ToList();

                            writeListToCSVFile(controllersRows, new ReportControllerRowMap(), controllersReportFilePath);

                            jobTarget.Status = JobTargetStatus.ConvertEntities;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ConvertEntities

                        // Output list of Applications
                        if (jobTarget.Status == JobTargetStatus.ConvertEntities)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.convertData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            #region Tiers

                            List<AppDRESTTier> tiersList = loadListOfObjectsFromFile<AppDRESTTier>(tiersFilePath);
                            if (tiersList != null)
                            {
                                Console.WriteLine("Convert Tiers ({0} entities)", tiersList.Count);

                                List<ReportTierRow> tiersRows = new List<ReportTierRow>(tiersList.Count);

                                foreach (AppDRESTTier tier in tiersList)
                                {
                                    ReportTierRow tierRow = new ReportTierRow();
                                    tierRow.AgentType = tier.agentType;
                                    tierRow.ApplicationID = jobTarget.ApplicationID;
                                    tierRow.ApplicationName = jobTarget.Application;
                                    tierRow.Controller = jobTarget.Controller;
                                    tierRow.NumNodes = tier.numberOfNodes;
                                    tierRow.TierID = tier.id;
                                    tierRow.TierName = tier.name;
                                    tierRow.TierType = tier.type;
                                    tierRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", tierRow.Controller);
                                    tierRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", tierRow.Controller, tierRow.ApplicationID);
                                    tierRow.TierLink= String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Tier>"")", tierRow.Controller, tierRow.ApplicationID, tierRow.TierID);

                                    tiersRows.Add(tierRow);
                                }

                                // Sort them
                                tiersRows = tiersRows.OrderBy(o => o.TierName).ToList();

                                if (File.Exists(tiersReportFilePath) == false)
                                {
                                    writeListToCSVFile(tiersRows, new ReportTierRowMap(), tiersReportFilePath);
                                }
                            }

                            #endregion

                            #region Nodes

                            List<AppDRESTNode> nodesList = loadListOfObjectsFromFile<AppDRESTNode>(nodesFilePath);
                            if (nodesList != null)
                            {
                                Console.WriteLine("Convert Nodes ({0} entities)", nodesList.Count);

                                List<ReportNodeRow> nodesRows = new List<ReportNodeRow>(nodesList.Count);

                                foreach (AppDRESTNode node in nodesList)
                                {
                                    ReportNodeRow nodeRow = new ReportNodeRow();
                                    nodeRow.AgentID = node.id;
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
                                    nodeRow.Type = node.type;
                                    nodeRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", nodeRow.Controller);
                                    nodeRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", nodeRow.Controller, nodeRow.ApplicationID);
                                    nodeRow.TierLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Tier>"")", nodeRow.Controller, nodeRow.ApplicationID, nodeRow.TierID);
                                    nodeRow.NodeLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Node>"")", nodeRow.Controller, nodeRow.ApplicationID, nodeRow.AgentID);

                                    nodesRows.Add(nodeRow);
                                }

                                // Sort them
                                nodesRows = nodesRows.OrderBy(o => o.TierName).ThenBy(o => o.NodeName).ToList();

                                if (File.Exists(nodesReportFilePath) == false)
                                {
                                    writeListToCSVFile(nodesRows, new ReportNodeRowMap(), nodesReportFilePath);
                                }
                            }

                            #endregion

                            #region Backends

                            List<AppDRESTBackend> backendsList = loadListOfObjectsFromFile<AppDRESTBackend>(backendsFilePath);
                            if (backendsList != null)
                            {
                                Console.WriteLine("Convert Backends ({0} entities)", backendsList.Count);

                                List<ReportBackendRow> backendsRows = new List<ReportBackendRow>(backendsList.Count);

                                foreach (AppDRESTBackend backend in backendsList)
                                {
                                    ReportBackendRow backendRow = new ReportBackendRow();
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
                                    backendRow.TierID = backend.tierId;
                                    if (backendRow.TierID > 0)
                                    {
                                        // Look it up
                                        AppDRESTTier tier = tiersList.Where<AppDRESTTier>(t => t.id == backendRow.TierID).FirstOrDefault();
                                        if (tier != null) backendRow.TierName = tier.name;                                        
                                    }
                                    backendRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", backendRow.Controller);
                                    backendRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", backendRow.Controller, backendRow.ApplicationID);
                                    backendRow.BackendLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Backend>"")", backendRow.Controller, backendRow.ApplicationID, backendRow.BackendID);

                                    backendsRows.Add(backendRow);
                                }

                                // Sort them
                                backendsRows = backendsRows.OrderBy(o => o.BackendType).ThenBy(o => o.BackendName).ToList();

                                if (File.Exists(backendsReportFilePath) == false)
                                {
                                    writeListToCSVFile(backendsRows, new ReportBackendRowMap(), backendsReportFilePath);
                                }
                            }

                            #endregion

                            #region Business Transactions

                            List<AppDRESTBusinessTransaction> businessTransactionsList = loadListOfObjectsFromFile<AppDRESTBusinessTransaction>(businessTransactionsFilePath);
                            if (businessTransactionsList != null)
                            {
                                Console.WriteLine("Convert Business Transactions ({0} entities)", businessTransactionsList.Count);

                                List<ReportBusinessTransactionRow> businessTransactionRows = new List<ReportBusinessTransactionRow>(nodesList.Count);

                                foreach (AppDRESTBusinessTransaction businessTransaction in businessTransactionsList)
                                {
                                    ReportBusinessTransactionRow businessTransactionRow = new ReportBusinessTransactionRow();
                                    businessTransactionRow.ApplicationID = jobTarget.ApplicationID;
                                    businessTransactionRow.ApplicationName = jobTarget.Application;
                                    businessTransactionRow.BTID = businessTransaction.id;
                                    businessTransactionRow.BTName = businessTransaction.name;
                                    businessTransactionRow.BTType = businessTransaction.entryPointType;
                                    businessTransactionRow.Controller = jobTarget.Controller;
                                    businessTransactionRow.TierID = businessTransaction.tierId;
                                    businessTransactionRow.TierName = businessTransaction.tierName;
                                    businessTransactionRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", businessTransactionRow.Controller);
                                    businessTransactionRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", businessTransactionRow.Controller, businessTransactionRow.ApplicationID);
                                    businessTransactionRow.TierLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Tier>"")", businessTransactionRow.Controller, businessTransactionRow.ApplicationID, businessTransactionRow.TierID);
                                    businessTransactionRow.BTLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<BT>"")", businessTransactionRow.Controller, businessTransactionRow.ApplicationID, businessTransactionRow.BTID);

                                    businessTransactionRows.Add(businessTransactionRow);
                                }

                                // Sort them
                                businessTransactionRows = businessTransactionRows.OrderBy(o => o.TierName).ThenBy(o => o.BTName).ToList();

                                if (File.Exists(businessTransactionsReportFilePath) == false)
                                {
                                    writeListToCSVFile(businessTransactionRows, new ReportBusinessTransactionRowMap(), businessTransactionsReportFilePath);
                                }
                            }

                            #endregion

                            #region Service Endpoints

                            List<ReportServiceEndpointRow> serviceEndpointsRows = new List<ReportServiceEndpointRow>();

                            if (tiersList != null)
                            {
                                Console.WriteLine("Convert Service Endpoints in Tiers ({0} tiers)", tiersList.Count);

                                JObject serviceEndpointsAll = loadObjectFromFile(serviceEndPointsAllFilePath);
                                JArray serviceEndpointsDetail = null;
                                if (serviceEndpointsAll != null)
                                {
                                    serviceEndpointsDetail = (JArray)serviceEndpointsAll["serviceEndpointListEntries"];
                                }

                                foreach (AppDRESTTier tier in tiersList)
                                {
                                    string serviceEndPointsForThisEntryFilePath = String.Format(serviceEndPointsFilePath, getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                    List<AppDRESTMetricFolder> serviceEndpointsList = loadListOfObjectsFromFile<AppDRESTMetricFolder>(serviceEndPointsForThisEntryFilePath);
                                    if (serviceEndpointsList != null)
                                    {
                                        Console.Write(".{0}", serviceEndpointsList.Count);

                                        foreach (AppDRESTMetricFolder serviceEndpoint in serviceEndpointsList)
                                        {
                                            JObject serviceEndpointDetail = (JObject)serviceEndpointsDetail.Where(sep => (string)sep["name"] == serviceEndpoint.name && (int)sep["applicationComponentId"] == tier.id).FirstOrDefault();

                                            ReportServiceEndpointRow serviceEndpointRow = new ReportServiceEndpointRow();
                                            serviceEndpointRow.ApplicationID = jobTarget.ApplicationID;
                                            serviceEndpointRow.ApplicationName = jobTarget.Application;
                                            serviceEndpointRow.Controller = jobTarget.Controller;
                                            if (serviceEndpointDetail != null) serviceEndpointRow.SEPID = (int)serviceEndpointDetail["id"];
                                            serviceEndpointRow.SEPName = serviceEndpoint.name;
                                            if (serviceEndpointDetail != null) serviceEndpointRow.SEPType = serviceEndpointDetail["type"].ToString();
                                            serviceEndpointRow.TierID = tier.id;
                                            serviceEndpointRow.TierName = tier.name;
                                            serviceEndpointRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", serviceEndpointRow.Controller);
                                            serviceEndpointRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", serviceEndpointRow.Controller, serviceEndpointRow.ApplicationID);
                                            serviceEndpointRow.TierLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Tier>"")", serviceEndpointRow.Controller, serviceEndpointRow.ApplicationID, serviceEndpointRow.TierID);
                                            serviceEndpointRow.SEPLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<SEP>"")", serviceEndpointRow.Controller, serviceEndpointRow.ApplicationID, serviceEndpointRow.SEPID);

                                            serviceEndpointsRows.Add(serviceEndpointRow);
                                        }
                                    }
                                }
                                Console.WriteLine();

                                // Sort them
                                serviceEndpointsRows = serviceEndpointsRows.OrderBy(o => o.TierName).ThenBy(o => o.SEPName).ToList();

                                if (File.Exists(serviceEndpointsReportFilePath) == false)
                                {
                                    writeListToCSVFile(serviceEndpointsRows, new ReportServiceEndpointRowMap(), serviceEndpointsReportFilePath);
                                }
                            }
                            #endregion

                            #region Errors

                            List<ReportErrorRow> errorRows = new List<ReportErrorRow>();

                            if (tiersList != null)
                            {
                                Console.WriteLine("Convert Errors in Tiers ({0} tiers)", tiersList.Count);

                                foreach (AppDRESTTier tier in tiersList)
                                {
                                    string errorsForThisEntryFilePath = String.Format(errorsFilePath, getShortenedEntityNameForFileSystem(tier.name, tier.id));

                                    List<AppDRESTMetricFolder> errorsList = loadListOfObjectsFromFile<AppDRESTMetricFolder>(errorsForThisEntryFilePath);
                                    if (errorsList != null)
                                    {
                                        Console.Write(".{0}", errorsList.Count);

                                        foreach (AppDRESTMetricFolder error in errorsList)
                                        {
                                            ReportErrorRow errorRow = new ReportErrorRow();
                                            errorRow.ApplicationID = jobTarget.ApplicationID;
                                            errorRow.ApplicationName = jobTarget.Application;
                                            errorRow.Controller = jobTarget.Controller;
                                            errorRow.ErrorName = error.name;
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

                                            errorRow.TierID = tier.id;
                                            errorRow.TierName = tier.name;
                                            errorRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", errorRow.Controller);
                                            errorRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", errorRow.Controller, errorRow.ApplicationID);
                                            errorRow.TierLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Tier>"")", errorRow.Controller, errorRow.ApplicationID, errorRow.TierID);
                                            errorRow.ErrorLink = String.Format(@"=HYPERLINK(""{0}/controller/#/TODO"", ""<Error>"")", errorRow.Controller, errorRow.ApplicationID, errorRow.ErrorName);

                                            errorRows.Add(errorRow);
                                        }
                                    }
                                }
                                Console.WriteLine();

                                // Sort them
                                errorRows = errorRows.OrderBy(o => o.TierName).ThenBy(o => o.ErrorName).ToList();

                                if (File.Exists(errorsReportFilePath) == false)
                                {
                                    writeListToCSVFile(errorRows, new ReportErrorRowMap(), errorsReportFilePath);
                                }
                            }

                            #endregion

                            #region Application

                            Console.WriteLine("Convert Application");

                            List<AppDRESTApplication> applicationsList = loadListOfObjectsFromFile<AppDRESTApplication>(applicationsFilePath);
                            if (applicationsList != null)
                            {
                                List<ReportApplicationRow> applicationsRows = readListFromCSVFile<ReportApplicationRow>(applicationsReportFilePath);

                                if (applicationsRows == null || applicationsRows.Count == 0)
                                {
                                    // First time, let's output these rows
                                    applicationsRows = new List<ReportApplicationRow>(applicationsList.Count);
                                    foreach (AppDRESTApplication application in applicationsList)
                                    {
                                        ReportApplicationRow applicationsRow = new ReportApplicationRow();
                                        applicationsRow.ApplicationName = application.name;
                                        applicationsRow.ApplicationID = application.id;
                                        applicationsRow.Controller = jobTarget.Controller;
                                        applicationsRow.ControllerLink = String.Format(@"=HYPERLINK(""{0}"", ""<Controller>"")", applicationsRow.Controller);
                                        applicationsRow.ApplicationLink = String.Format(@"=HYPERLINK(""{0}/controller/#/location=APP_DASHBOARD&application={1}"", ""<Application>"")", applicationsRow.Controller, applicationsRow.ApplicationID);

                                        applicationsRows.Add(applicationsRow);
                                    }
                                }

                                // Update counts of entities for this application row
                                ReportApplicationRow applicationRow = applicationsRows.Where(a => a.ApplicationID == jobTarget.ApplicationID).FirstOrDefault();
                                if (applicationRow != null)
                                {
                                    if (tiersList != null) applicationRow.NumTiers = tiersList.Count;
                                    if (nodesList != null) applicationRow.NumNodes = nodesList.Count;
                                    if (backendsList != null) applicationRow.NumBackends = backendsList.Count;
                                    if (businessTransactionsList != null) applicationRow.NumBTs = businessTransactionsList.Count;
                                    if (serviceEndpointsRows != null) applicationRow.NumSEPs = serviceEndpointsRows.Count;
                                    if (errorRows != null) applicationRow.NumErrors = errorRows.Count;

                                    List<ReportApplicationRow> applicationRows = new List<ReportApplicationRow>(1);
                                    applicationRows.Add(applicationRow);

                                    // Write just this row for this application
                                    writeListToCSVFile(applicationRows, new ReportApplicationRowMap(), applicationReportFilePath);
                                }

                                // Sort them
                                applicationsRows = applicationsRows.OrderBy(o => o.Controller).ThenBy(o => o.ApplicationName).ToList();

                                writeListToCSVFile(applicationsRows, new ReportApplicationRowMap(), applicationsReportFilePath);
                            }


                            #endregion

                            jobTarget.Status = JobTargetStatus.ConvertConfiguration;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ConvertConfig

                        if (jobTarget.Status == JobTargetStatus.ConvertConfiguration)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.convertData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            //TODO 
                            Console.WriteLine("Convert Configuration - TODO");

                            // Business Transaction Rules

                            // Backend Rules configuration/backend-match-point-configurations

                            // Data Collectors

                            // Agent properties

                            // Health Rules configuration/health-rules

                            // Error Detection configuration/error-configuration

                            jobTarget.Status = JobTargetStatus.ConvertMetrics;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ConvertMetrics

                        if (jobTarget.Status == JobTargetStatus.ConvertMetrics)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.convertData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            // TODO
                            Console.WriteLine("Convert Metrics - TODO");

                            jobTarget.Status = JobTargetStatus.ConvertFlowmaps;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ConvertFlowmaps

                        if (jobTarget.Status == JobTargetStatus.ConvertFlowmaps)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.convertData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            // TODO
                            Console.WriteLine("Convert Flowmaps - TODO");

                            jobTarget.Status = JobTargetStatus.ConvertSnapshots;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                        #region ConvertSnapshots

                        if (jobTarget.Status == JobTargetStatus.ConvertSnapshots)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.convertData",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            // TODO
                            Console.WriteLine("Convert Snapshots - TODO");

                            jobTarget.Status = JobTargetStatus.Report;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Failed in {0}, skipping this target", jobTarget.Status);
                        Console.WriteLine(ex);
                        Console.ResetColor();

                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Error,
                            EventId.EXCEPTION_GENERIC,
                            "ProcessJob.convertData",
                            ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();
                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Verbose,
                            EventId.FUNCTION_DURATION_EVENT,
                            String.Format("Converting [{0}/{1}], {2} {3} ({4})", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.Status),
                            String.Format("Execution took {0:c} ({1} ms)", stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("Converting execution took {0:c} ({1} ms)", stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds));
                        Console.ResetColor();
                    }

                    // Move job to next status
                    jobConfiguration.Status = JobStatus.Report;

                    // Save the resulting JSON file to the job target folder
                    if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                        Console.ResetColor();

                        return;
                    }
                }

                // Move job to next status
                jobConfiguration.Status = JobStatus.Report;

                // Save the resulting JSON file to the job target folder
                if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                    Console.ResetColor();

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.convertData",
                    ex);
            }
            finally
            {
                stopWatch.Stop();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FUNCTION_DURATION_EVENT,
                    "ProcessJob.convertData",
                    String.Format("Execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(String.Format("Converting execution overall took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));
                Console.ResetColor();
            }
        }

        private static void createReport(ProgramOptions programOptions)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                // Load job configuration
                JobConfiguration jobConfiguration = JobConfigurationHelper.readJobConfigurationFromFile(programOptions.OutputJobFilePath);
                if (jobConfiguration == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to load job input file={0} ", programOptions.OutputJobFilePath);
                    Console.ResetColor();

                    return;
                }

                Console.WriteLine("Output data ({0})", jobConfiguration.Status);

                #region Output diagnostic parameters to log

                // Check current process status
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOB_STATUS_INFORMATION,
                    "ProcessJob.createReport",
                    String.Format("Job status='{0}', to execute need status='{1}'", jobConfiguration.Status, JobStatus.Report));

                #endregion

                // If not the expected status, move on
                if (jobConfiguration.Status != JobStatus.Report)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Skipping");
                    Console.ResetColor();

                    return;
                }

                #region Whole report variables

                // Report files
                string entitiesReportFilePath = Path.Combine(programOptions.OutputJobFolderPath, String.Format(REPORT_ENTITIES_FILE_NAME, programOptions.JobName));

                #endregion

                #region Prepare Excel sheets 

                // Prepare package
                ExcelPackage reportEntities = new ExcelPackage();
                reportEntities.Workbook.Properties.Author = "AppDOfflineDataExport";
                reportEntities.Workbook.Properties.Title = String.Format("AppDynamics Entity Report {0}", programOptions.JobName);
                reportEntities.Workbook.Properties.Comments = String.Format("Targets={0}\r\nFrom={1:o}\r\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = reportEntities.Workbook.Worksheets.Add(REPORT_PARAMETERS_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[3, 1].Value = "Requested From (UTC/Local)";
                sheet.Cells[3, 2].Value = jobConfiguration.Input.TimeRange.From.ToString("o");
                sheet.Cells[3, 3].Value = jobConfiguration.Input.TimeRange.From.ToLocalTime().ToString("o");
                sheet.Cells[4, 1].Value = "Requested To (UTC/Local)";
                sheet.Cells[4, 2].Value = jobConfiguration.Input.TimeRange.To.ToString("o");
                sheet.Cells[4, 3].Value = jobConfiguration.Input.TimeRange.To.ToLocalTime().ToString("o");
                sheet.Cells[5, 1].Value = "Expanded From (UTC/Local)";
                sheet.Cells[5, 2].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToString("o");
                sheet.Cells[5, 3].Value = jobConfiguration.Input.ExpandedTimeRange.From.ToLocalTime().ToString("o");
                sheet.Cells[6, 1].Value = "Expanded To (UTC)";
                sheet.Cells[6, 2].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToString("o");
                sheet.Cells[6, 3].Value = jobConfiguration.Input.ExpandedTimeRange.To.ToLocalTime().ToString("o");
                sheet.Cells[7, 1].Value = "Number of Hours Intervals";
                sheet.Cells[7, 2].Value = jobConfiguration.Input.HourlyTimeRanges.Count;
                sheet.Cells[8, 1].Value = "Export Metrics";
                sheet.Cells[8, 2].Value = jobConfiguration.Input.Metrics;
                sheet.Cells[9, 1].Value = "Export Snapshots";
                sheet.Cells[9, 2].Value = jobConfiguration.Input.Snapshots;
                sheet.Cells[10, 1].Value = "Export Flowmaps";
                sheet.Cells[10, 2].Value = jobConfiguration.Input.Flowmaps;
                sheet.Cells[11, 1].Value = "Export Configuration";
                sheet.Cells[11, 2].Value = jobConfiguration.Input.Configuration;
                sheet.Cells[13, 1].Value = "Targets:";
                ExcelRangeBase range = sheet.Cells[12, 1].LoadFromCollection(from jobTarget in jobConfiguration.Target
                                                     select new {
                                                         Controller = jobTarget.Controller,
                                                         UserName = jobTarget.UserName,
                                                         Application = jobTarget.Application,
                                                         ApplicationID = jobTarget.ApplicationID,
                                                         NameRegex = jobTarget.NameRegex,
                                                         Status = jobTarget.Status.ToString()
                                                     }, true);
                ExcelTable table = sheet.Tables.Add(range, REPORT_PARAMETERS_TARGETS_TABLE_NAME);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(1).AutoFit();
                sheet.Column(2).AutoFit();
                sheet.Column(3).AutoFit();

                #endregion

                #region Entity sheets

                // Navigation sheet with link to other sheets
                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_TOC_SHEET_NAME);

                #endregion

                #region Entity sheets

                // Entity sheets
                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_CONTROLLERS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_APPLICATIONS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_TIERS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TIERS_PIVOT_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_TIERS_PIVOT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TIERS_LIST_SHEET_NAME);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_NODES_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "App Agent By Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_NODES_PIVOT_APPAGENT_SHEET_NAME);
                sheet.Cells[3, 1].Value = "Machine Agent By Type";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_NODES_PIVOT_MACHINEAGENT_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_NODES_PIVOT_APPAGENT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_NODES_LIST_SHEET_NAME);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_NODES_PIVOT_MACHINEAGENT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_NODES_LIST_SHEET_NAME);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_BACKENDS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_BACKENDS_PIVOT_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_BACKENDS_PIVOT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_BACKENDS_LIST_SHEET_NAME);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_BUSINESS_TRANSACTIONS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_BUSINESS_TRANSACTIONS_PIVOT_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_BUSINESS_TRANSACTIONS_PIVOT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_BUSINESS_TRANSACTIONS_LIST_SHEET_NAME);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_SERVICE_ENDPOINTS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SERVICE_ENDPOINTS_PIVOT_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_SERVICE_ENDPOINTS_PIVOT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SERVICE_ENDPOINTS_LIST_SHEET_NAME);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_ERRORS_LIST_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_ERRORS_PIVOT_SHEET_NAME);
                sheet.View.FreezePanes(REPORT_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = reportEntities.Workbook.Worksheets.Add(REPORT_ERRORS_PIVOT_SHEET_NAME);
                sheet.Cells[1, 1].Value = "Table to Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_TOC_SHEET_NAME);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_ERRORS_LIST_SHEET_NAME);

                #endregion

                #endregion

                List<string> listOfControllersAlreadyProcessed = new List<string>(jobConfiguration.Target.Count);

                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Reporting entities [{0}/{1}], {2} {3} ({4})", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.Status);
                    Console.ResetColor();

                    try
                    {
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

                        #endregion

                        #region Target states check

                        List<JobTargetStatus> validStates = new List<JobTargetStatus>();
                        validStates.Add(JobTargetStatus.Report);
                        validStates.Add(JobTargetStatus.ReportEntities);

                        if (validStates.Contains(jobTarget.Status) == false)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Target in invalid state, skipping");
                            Console.ResetColor();

                            continue;
                        }

                        #endregion

                        #region Report

                        if (jobTarget.Status == JobTargetStatus.Report)
                        {
                            jobTarget.Status = JobTargetStatus.ReportEntities;
                        }

                        #endregion

                        #region ReportEntities

                        if (jobTarget.Status == JobTargetStatus.ReportEntities)
                        {
                            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                                TraceEventType.Information,
                                EventId.TARGET_STATUS_INFORMATION,
                                "ProcessJob.createReport",
                                String.Format("Controller='{0}', Application='{1}', status='{2}'", jobTarget.Controller, jobTarget.Application, jobTarget.Status));

                            int numRowsToSkipInCSVFile = 0;
                            int fromRow = 1;

                            #region Controllers and Applications

                            // Only output this once per controller
                            if (listOfControllersAlreadyProcessed.Contains(jobTarget.Controller) == false)
                            {
                                listOfControllersAlreadyProcessed.Add(jobTarget.Controller);

                                Console.WriteLine("Report Controllers");

                                sheet = reportEntities.Workbook.Worksheets[REPORT_CONTROLLERS_LIST_SHEET_NAME];
                                if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                                {
                                    fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
                                    numRowsToSkipInCSVFile = 0;
                                }
                                else
                                {
                                    fromRow = sheet.Dimension.Rows + 1;
                                    numRowsToSkipInCSVFile = 1;
                                }
                                readCSVFileIntoExcelRange(controllerReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                                Console.WriteLine("Report Applications");

                                sheet = reportEntities.Workbook.Worksheets[REPORT_APPLICATIONS_LIST_SHEET_NAME];
                                if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                                {
                                    fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
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

                            Console.WriteLine("Report Tiers");

                            sheet = reportEntities.Workbook.Worksheets[REPORT_TIERS_LIST_SHEET_NAME];
                            if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
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

                            Console.WriteLine("Report Nodes");

                            sheet = reportEntities.Workbook.Worksheets[REPORT_NODES_LIST_SHEET_NAME];
                            if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
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

                            Console.WriteLine("Report Backends");

                            sheet = reportEntities.Workbook.Worksheets[REPORT_BACKENDS_LIST_SHEET_NAME];
                            if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
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

                            Console.WriteLine("Report Business Transactions");

                            sheet = reportEntities.Workbook.Worksheets[REPORT_BUSINESS_TRANSACTIONS_LIST_SHEET_NAME];
                            if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
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

                            Console.WriteLine("Report Service Endpoints");

                            sheet = reportEntities.Workbook.Worksheets[REPORT_SERVICE_ENDPOINTS_LIST_SHEET_NAME];
                            if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
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

                            Console.WriteLine("Report Errors");

                            sheet = reportEntities.Workbook.Worksheets[REPORT_ERRORS_LIST_SHEET_NAME];
                            if (sheet.Dimension.Rows < REPORT_LIST_SHEET_START_TABLE_AT)
                            {
                                fromRow = REPORT_LIST_SHEET_START_TABLE_AT;
                                numRowsToSkipInCSVFile = 0;
                            }
                            else
                            {
                                fromRow = sheet.Dimension.Rows + 1;
                                numRowsToSkipInCSVFile = 1;
                            }
                            readCSVFileIntoExcelRange(errorsReportFilePath, numRowsToSkipInCSVFile, sheet, fromRow, 1);

                            #endregion

                            jobTarget.Status = JobTargetStatus.ReportConfiguration;

                            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                                Console.ResetColor();

                                return;
                            }
                        }

                        #endregion

                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Failed in {0}, skipping this target", jobTarget.Status);
                        Console.WriteLine(ex);
                        Console.ResetColor();

                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Error,
                            EventId.EXCEPTION_GENERIC,
                            "ProcessJob.createReport",
                            ex);
                    }
                    finally
                    {
                        stopWatchTarget.Stop();
                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Verbose,
                            EventId.FUNCTION_DURATION_EVENT,
                            String.Format("Reporting entities [{0}/{1}], {2} {3} ({4})", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTarget.Status),
                            String.Format("Execution took {0:c} ({1} ms)", stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds));

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("Reporting execution took {0:c} ({1} ms)", stopWatchTarget.Elapsed, stopWatchTarget.ElapsedMilliseconds));
                        Console.ResetColor();
                    }
                }

                #region Finalize all the sheets and create the pivots

                // Make data in sheets loaded from multiple targets into tables and add pivots

                #region Controllers sheet

                // Make table
                sheet = reportEntities.Workbook.Worksheets[REPORT_CONTROLLERS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONTROLLERS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;
                }

                sheet.Column(1).AutoFit();
                sheet.Column(2).AutoFit();
                sheet.Column(3).AutoFit();
                sheet.Column(4).AutoFit();
                sheet.Column(5).AutoFit();

                #endregion

                #region Applications

                // Make table
                sheet = reportEntities.Workbook.Worksheets[REPORT_APPLICATIONS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_APPLICATIONS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    sheet.Column(12).AutoFit();
                    sheet.Column(13).AutoFit();
                }

                #endregion

                #region Tiers

                // Make table
                sheet = reportEntities.Workbook.Worksheets[REPORT_TIERS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Tiers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_TIERS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    //sheet.Column(4).AutoFit();
                    sheet.Column(4).Width = 35;
                    sheet.Column(5).AutoFit();
                    sheet.Column(7).AutoFit();
                    sheet.Column(9).AutoFit();
                    sheet.Column(10).AutoFit();
                    sheet.Column(11).AutoFit();

                    // Make pivot
                    sheet = reportEntities.Workbook.Worksheets[REPORT_TIERS_PIVOT_SHEET_NAME];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_TIERS_PIVOT_NAME);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Controller"]);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    fieldR = pivot.RowFields.Add(pivot.Fields["ApplicationName"]);
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
                sheet = reportEntities.Workbook.Worksheets[REPORT_NODES_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Nodes Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_NODES_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    //sheet.Column(4).AutoFit();
                    sheet.Column(4).Width = 35;
                    //sheet.Column(6).AutoFit();
                    sheet.Column(6).Width = 35;
                    sheet.Column(7).AutoFit();
                    sheet.Column(8).AutoFit();
                    sheet.Column(11).AutoFit();
                    sheet.Column(12).AutoFit();
                    sheet.Column(17).AutoFit();
                    sheet.Column(18).AutoFit();
                    sheet.Column(19).AutoFit();

                    // Make pivot
                    sheet = reportEntities.Workbook.Worksheets[REPORT_NODES_PIVOT_APPAGENT_SHEET_NAME];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_NODES_APPAGENT_PIVOT_NAME);
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

                    sheet = reportEntities.Workbook.Worksheets[REPORT_NODES_PIVOT_MACHINEAGENT_SHEET_NAME];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_NODES_MACHINEAGENT_PIVOT_NAME);
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
                sheet = reportEntities.Workbook.Worksheets[REPORT_BACKENDS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Backends Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_BACKENDS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    //sheet.Column(4).AutoFit();
                    sheet.Column(4).Width = 35;
                    sheet.Column(5).AutoFit();
                    sheet.Column(7).AutoFit();
                    sheet.Column(8).Width = 35;
                    sheet.Column(9).Width = 35;
                    sheet.Column(10).Width = 35;
                    sheet.Column(11).Width = 35;
                    sheet.Column(12).Width = 35;
                    sheet.Column(13).Width = 35;
                    sheet.Column(14).Width = 35;
                    sheet.Column(15).Width = 35;
                    sheet.Column(20).AutoFit();
                    sheet.Column(21).AutoFit();
                    sheet.Column(22).AutoFit();

                    // Make pivot
                    sheet = reportEntities.Workbook.Worksheets[REPORT_BACKENDS_PIVOT_SHEET_NAME];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_BACKENDS_PIVOT_NAME);
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
                    //fieldD.Name = "Tiers Of Type";
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = reportEntities.Workbook.Worksheets[REPORT_BUSINESS_TRANSACTIONS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Business Transactions Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_BUSINESS_TRANSACTIONS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    //sheet.Column(4).AutoFit();
                    sheet.Column(4).Width = 35;
                    sheet.Column(5).AutoFit();
                    //sheet.Column(6).AutoFit();
                    sheet.Column(6).Width = 35;
                    sheet.Column(7).AutoFit();
                    sheet.Column(9).AutoFit();
                    sheet.Column(10).AutoFit();
                    sheet.Column(11).AutoFit();
                    sheet.Column(12).AutoFit();

                    // Make pivot
                    sheet = reportEntities.Workbook.Worksheets[REPORT_BUSINESS_TRANSACTIONS_PIVOT_SHEET_NAME];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_BUSINESS_TRANSACTIONS_PIVOT_NAME);
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
                }

                #endregion

                #region Service Endpoints

                // Make table
                sheet = reportEntities.Workbook.Worksheets[REPORT_SERVICE_ENDPOINTS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Service Endpoints Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SERVICE_ENDPOINTS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    //sheet.Column(4).AutoFit();
                    sheet.Column(4).Width = 35;
                    sheet.Column(5).AutoFit();
                    //sheet.Column(6).AutoFit();
                    sheet.Column(6).Width = 35;
                    sheet.Column(7).AutoFit();
                    sheet.Column(9).AutoFit();
                    sheet.Column(10).AutoFit();
                    sheet.Column(11).AutoFit();
                    sheet.Column(12).AutoFit();

                    // Make pivot
                    sheet = reportEntities.Workbook.Worksheets[REPORT_SERVICE_ENDPOINTS_PIVOT_SHEET_NAME];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_SERVICE_ENDPOINTS_PIVOT_NAME);
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
                }

                #endregion

                #region Errors

                // Make table
                sheet = reportEntities.Workbook.Worksheets[REPORT_ERRORS_LIST_SHEET_NAME];
                Console.WriteLine("Finalize Errors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_ERRORS_TABLE_NAME);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(1).AutoFit();
                    sheet.Column(2).AutoFit();
                    //sheet.Column(4).AutoFit();
                    sheet.Column(4).Width = 35;
                    sheet.Column(5).AutoFit();
                    //sheet.Column(6).AutoFit();
                    sheet.Column(6).Width = 35;
                    sheet.Column(7).AutoFit();
                    sheet.Column(10).Width = 35;
                    sheet.Column(11).Width = 35;
                    sheet.Column(12).Width = 35;
                    sheet.Column(13).Width = 35;
                    sheet.Column(14).Width = 35;
                    sheet.Column(15).AutoFit();
                    sheet.Column(16).AutoFit();
                    sheet.Column(17).AutoFit();
                    sheet.Column(18).AutoFit();

                    // Make pivot
                    sheet = reportEntities.Workbook.Worksheets[REPORT_ERRORS_PIVOT_SHEET_NAME];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_ERRORS_PIVOT_NAME);
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
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = reportEntities.Workbook.Worksheets[REPORT_TOC_SHEET_NAME];
                sheet.Cells[1, 1].Value = "Sheet Name";
                sheet.Cells[1, 2].Value = "# Entities";
                sheet.Cells[1, 3].Value = "Sheet Link";
                int rowNum = 1;
                foreach (ExcelWorksheet s in reportEntities.Workbook.Worksheets)
                {
                    rowNum++;
                    sheet.Cells[rowNum, 1].Value = s.Name;
                    sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                    table = null;
                    switch (s.Name)
                    {
                        case (REPORT_CONTROLLERS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_CONTROLLERS_TABLE_NAME];
                                break;

                            }
                        case (REPORT_APPLICATIONS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_APPLICATIONS_TABLE_NAME];
                                break;
                            }
                        case (REPORT_TIERS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_TIERS_TABLE_NAME];
                                break;
                            }
                        case (REPORT_NODES_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_NODES_TABLE_NAME];
                                break;
                            }
                        case (REPORT_BACKENDS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_BACKENDS_TABLE_NAME];
                                break;
                            }
                        case (REPORT_BUSINESS_TRANSACTIONS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_BUSINESS_TRANSACTIONS_TABLE_NAME];
                                break;
                            }
                        case (REPORT_SERVICE_ENDPOINTS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_SERVICE_ENDPOINTS_TABLE_NAME];
                                break;
                            }
                        case (REPORT_ERRORS_LIST_SHEET_NAME):
                            {
                                table = s.Tables[REPORT_ERRORS_TABLE_NAME];
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    if (table != null)
                    {
                        sheet.Cells[rowNum, 2].Value = table.Address.Rows - 1;
                    }
                }
                range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, REPORT_TOC_TABLE_NAME);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(1).AutoFit();
                sheet.Column(2).AutoFit();
                
                #endregion
                
                // Save full report Excel files
                reportEntities.SaveAs(new FileInfo(entitiesReportFilePath));

                #endregion

                // Move job to next status
                jobConfiguration.Status = JobStatus.Done;

                // Save the resulting JSON file to the job target folder
                if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                    Console.ResetColor();

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.createReport",
                    ex);
            }
            finally
            {
                stopWatch.Stop();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FUNCTION_DURATION_EVENT,
                    "ProcessJob.createReport",
                    String.Format("Execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(String.Format("Reporting execution overall took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));
                Console.ResetColor();
            }
        }

        private static bool saveFileToFolder(string fileContents, string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);

            try
            {

                if (!Directory.Exists(folderPath))
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Verbose,
                        EventId.FOLDER_CREATE,
                        "ProcessJob.createFolder",
                        String.Format("Creating folder='{0}'", folderPath));

                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.saveFileToFolder",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.FOLDER_CREATE_FAILED,
                    "ProcessJob.saveFileToFolder",
                    String.Format("Unable to create folder='{0}'", folderPath));

                return false;
            }

            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FILE_WRITE,
                    "ProcessJob.saveFileToFolder",
                    String.Format("Writing string length='{0}' to file='{1}'", fileContents.Length, filePath));

                File.WriteAllText(filePath, fileContents, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.saveFileToFolder",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_WRITE_FILE,
                    "ProcessJob.saveFileToFolder",
                    String.Format("Unable to write to file='{0}'", filePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.saveFileToFolder",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_WRITE_FILE,
                    "ProcessJob.saveFileToFolder",
                    String.Format("Unable to write to file='{0}'", filePath));
            }

            return true;
        }

        private static void getMetricDataForMetricForAllRanges(ControllerApi controllerApi, string applicationNameOrID, string metricPath, JobConfiguration jobConfiguration, string metricsEntityFolderPath, string metricEntitySubFolderName)
        {
            // Get the full range
            JobTimeRange jobTimeRange = jobConfiguration.Input.ExpandedTimeRange;

            Console.Write(".");

            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                TraceEventType.Information,
                EventId.METRIC_RETRIEVAL,
                "ProcessJob.getMetricDataForMetricForAllRanges",
                String.Format("Retrieving metric for Application='{0}', Metric='{1}', From='{2:o}', To='{3:o}'", applicationNameOrID, metricPath, jobTimeRange.From, jobTimeRange.To));

            string metricsJson = String.Empty;
            string metricsDataFilePath = String.Empty;

            metricsDataFilePath = Path.Combine(metricsEntityFolderPath, metricEntitySubFolderName, String.Format(EXTRACT_METRIC_FULL_FILE_NAME, jobTimeRange.From.ToString("yyyyMMddHHmm"), jobTimeRange.To.ToString("yyyyMMddHHmm")));
            if (File.Exists(metricsDataFilePath) == false)
            {
                // First range is the whole thing
                metricsJson = controllerApi.GetMetricData(
                    applicationNameOrID,
                    metricPath,
                    convertToUnixTimestamp(jobTimeRange.From),
                    convertToUnixTimestamp(jobTimeRange.To),
                    true);

                if (metricsJson != String.Empty) saveFileToFolder(metricsJson, metricsDataFilePath);
            }

            // Get the hourly time ranges
            for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
            {
                jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                Console.Write(".");

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Information,
                    EventId.METRIC_RETRIEVAL,
                    "ProcessJob.getMetricDataForMetricForAllRanges",
                    String.Format("Application='{0}', Metric='{1}', From='{2:o}', To='{3:o}'", applicationNameOrID, metricPath, jobTimeRange.From, jobTimeRange.To));

                metricsDataFilePath = Path.Combine(metricsEntityFolderPath, metricEntitySubFolderName, String.Format(EXTRACT_METRIC_HOUR_FILE_NAME, jobTimeRange.From.ToString("yyyyMMddHHmm"), jobTimeRange.To.ToString("yyyyMMddHHmm")));

                if (File.Exists(metricsDataFilePath) == false)
                {
                    // Subsequent ones are details
                    metricsJson = controllerApi.GetMetricData(
                        applicationNameOrID,
                        metricPath,
                        convertToUnixTimestamp(jobTimeRange.From),
                        convertToUnixTimestamp(jobTimeRange.To),
                        false);

                    if (metricsJson != String.Empty) saveFileToFolder(metricsJson, metricsDataFilePath);
                }
            }
        }

        private static JArray loadArrayFromFile(string jsonFilePath)
        {
            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FILE_READ,
                    "ProcessJob.loadArrayFromFile",
                    String.Format("Reading JSON from file='{0}'", jsonFilePath));

                if (File.Exists(jsonFilePath) == false)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.UNABLE_TO_READ_FILE,
                        "ProcessJob.loadArrayFromFile",
                        String.Format("Unable to find file='{0}'", jsonFilePath));
                }
                else
                {
                    return JArray.Parse(File.ReadAllText(jsonFilePath));
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.loadArrayFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "ProcessJob.loadArrayFromFile",
                    String.Format("Unable to read from JSON file='{0}'", jsonFilePath));
            }
            catch (JsonReaderException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_JSONREADEREXCEPTION,
                    "ProcessJob.loadArrayFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "ProcessJob.loadArrayFromFile",
                    String.Format("Invalid JSON in JSON file='{0}'", jsonFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.loadArrayFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "ProcessJob.loadArrayFromFile",
                    String.Format("Unable to load JSON from JSON file='{0}'", jsonFilePath));
            }

            return null;
        }

        private static JObject loadObjectFromFile(string jsonFilePath)
        {
            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FILE_READ,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Reading JSON from file='{0}'", jsonFilePath));

                if (File.Exists(jsonFilePath) == false)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.UNABLE_TO_READ_FILE,
                        "ProcessJob.loadObjectFromFile",
                        String.Format("Unable to find file='{0}'", jsonFilePath));
                }
                else
                {
                    return JObject.Parse(File.ReadAllText(jsonFilePath));
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.loadObjectFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Unable to read from JSON file='{0}'", jsonFilePath));
            }
            catch (JsonReaderException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_JSONREADEREXCEPTION,
                    "ProcessJob.loadObjectFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Invalid JSON in JSON file='{0}'", jsonFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.loadObjectFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Unable to load JSON from JSON file='{0}'", jsonFilePath));
            }

            return null;
        }

        internal static List<T> loadListOfObjectsFromFile<T>(string jsonFilePath)
        {
            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FILE_READ,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Reading JSON from job file='{0}'", jsonFilePath));

                if (File.Exists(jsonFilePath) == false)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.UNABLE_TO_READ_FILE,
                        "ProcessJob.loadObjectFromFile",
                        String.Format("Unable to find file='{0}'", jsonFilePath));
                }
                else
                {
                    return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(jsonFilePath));
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.loadObjectFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Unable to read from file='{0}'", jsonFilePath));
            }
            catch (JsonReaderException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_JSONREADEREXCEPTION,
                    "ProcessJob.loadObjectFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Invalid JSON in file='{0}'", jsonFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.loadObjectFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_JSON_FORMAT,
                    "ProcessJob.loadObjectFromFile",
                    String.Format("Unable to load JSON from file='{0}'", jsonFilePath));
            }

            return null;
        }

        private static XmlDocument loadXmlDocumentFromFile(string xmlFilePath)
        {
            try
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FILE_READ,
                    "ProcessJob.loadXmlDocumentFromFile",
                    String.Format("Reading XML from file='{0}'", xmlFilePath));

                if (File.Exists(xmlFilePath) == false)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.UNABLE_TO_READ_FILE,
                        "ProcessJob.loadXmlDocumentFromFile",
                        String.Format("Unable to find file='{0}'", xmlFilePath));
                }
                else
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(xmlFilePath);
                    return doc;
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.loadXmlDocumentFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "ProcessJob.loadXmlDocumentFromFile",
                    String.Format("Unable to read from XML file='{0}'", xmlFilePath));
            }
            catch (XmlException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_XMLEXCEPTION,
                    "ProcessJob.loadXmlDocumentFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_XML_FORMAT,
                    "ProcessJob.loadXmlDocumentFromFile",
                    String.Format("Invalid XML in XML file='{0}'", xmlFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.loadXmlDocumentFromFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_XML_FORMAT,
                    "ProcessJob.loadArrayFromFile",
                    String.Format("Unable to load XML from XML file='{0}'", xmlFilePath));
            }

            return null;
        }

        private static bool writeJSONObjectToFile(object objectToWrite, string jsonFilePath)
        {
            string folderPath = Path.GetDirectoryName(jsonFilePath);

            try
            {

                if (!Directory.Exists(folderPath))
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Verbose,
                        EventId.FOLDER_CREATE,
                        "ProcessJob.writeJSONObjectToFile",
                        String.Format("Creating folder='{0}'", folderPath));

                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.writeJSONObjectToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.FOLDER_CREATE_FAILED,
                    "ProcessJob.writeJSONObjectToFile",
                    String.Format("Unable to create folder='{0}'", folderPath));

                return false;
            }

            try
            {
                using (StreamWriter sw = File.CreateText(jsonFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Include;
                    serializer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    serializer.Serialize(sw, objectToWrite);
                }

                return true;
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.writeJSONObjectToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_WRITE_FILE,
                    "ProcessJob.writeJSONObjectToFile",
                    String.Format("Unable to write to file='{0}'", jsonFilePath));
            }
            catch (JsonWriterException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_JSONWRITEREXCEPTION,
                    "ProcessJob.writeJSONObjectToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_RENDER_JSON,
                    "ProcessJob.writeJSONObjectToFile",
                    String.Format("Unable to serialize JSON to file='{0}'", jsonFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.writeJSONObjectToFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_RENDER_JSON,
                    "ProcessJob.writeJSONObjectToFile",
                    String.Format("Unable to write JSON to file='{0}'", jsonFilePath));
            }

            return false;
        }

        private static bool writeJArrayToFile(JArray array, string jsonFilePath)
        {

            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                TraceEventType.Verbose,
                EventId.FILE_WRITE,
                "ProcessJob.writeJArrayToFile",
                String.Format("Writing JSON Array length='{0}' to file='{1}'", array.Count, jsonFilePath));

            return writeJSONObjectToFile(array, jsonFilePath);
        }

        private static bool writeListToCSVFile<T>(List<T> listToWrite, CsvClassMap<T> classMap, string csvFilePath)
        {
            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                TraceEventType.Verbose,
                EventId.FILE_WRITE,
                "ProcessJob.writeListToCSVFile",
                String.Format("Writing List elements='{0}', type='{1}', to file='{1}'", listToWrite.Count, typeof(T), csvFilePath));

            string folderPath = Path.GetDirectoryName(csvFilePath);

            try
            {

                if (!Directory.Exists(folderPath))
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Verbose,
                        EventId.FOLDER_CREATE,
                        "ProcessJob.writeListToCSVFile",
                        String.Format("Creating folder='{0}'", folderPath));

                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.writeListToCSVFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.FOLDER_CREATE_FAILED,
                    "ProcessJob.writeListToCSVFile",
                    String.Format("Unable to create folder='{0}'", folderPath));

                return false;
            }

            try
            {
                using (StreamWriter sw = File.CreateText(csvFilePath))
                {
                    CsvWriter csvWriter = new CsvWriter(sw);
                    csvWriter.Configuration.RegisterClassMap(classMap);
                    csvWriter.WriteRecords(listToWrite);
                }

                return true;
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.writeListToCSVFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_WRITE_FILE,
                    "ProcessJob.writeListToCSVFile",
                    String.Format("Unable to write to file='{0}'", csvFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.writeListToCSVFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_RENDER_CSV,
                    "ProcessJob.writeListToCSVFile",
                    String.Format("Unable to write CSV to file='{0}'", csvFilePath));
            }

            return false;
        }

        private static List<T> readListFromCSVFile<T>(string csvFilePath)
        {
            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                TraceEventType.Verbose,
                EventId.FILE_READ,
                "ProcessJob.readListFromCSVFile",
                String.Format("Reading List, type='{0}', from file='{1}'", typeof(T), csvFilePath));

            try
            {
                using (StreamReader sr = File.OpenText(csvFilePath))
                {
                    CsvReader csvReader = new CsvReader(sr);
                    return csvReader.GetRecords<T>().ToList();
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.readListFromCSVFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "ProcessJob.readListFromCSVFile",
                    String.Format("Unable to read from file='{0}'", csvFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.readListFromCSVFile",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_CSV,
                    "ProcessJob.readListFromCSVFile",
                    String.Format("Unable to read CSV from file='{0}'", csvFilePath));
            }

            return null;
        }

        private static ExcelRangeBase readCSVFileIntoExcelRange(string csvFilePath, int skipLinesFromBeginning, ExcelWorksheet sheet, int startRow, int startColumn)
        {
            LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                TraceEventType.Verbose,
                EventId.CSV_FILE_TO_EXCEL_RANGE,
                "ProcessJob.readCSVFileIntoExcelRange",
                String.Format("Reading CSV file={0} to Excel Worksheet={1} at (row={2}, column={3})", csvFilePath, sheet.Name, startRow, startColumn));

            try
            {
                int csvRowIndex = -1;
                int numColumnsInCSV = 0;

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

                        // Should we skip?
                        if (csvRowIndex < skipLinesFromBeginning)
                        {
                            // Skip this line
                            continue;
                        }

                        // Read row one field at a time
                        int csvFieldIndex = 0;
                        numColumnsInCSV = rowValues.Length;
                        foreach (string fieldValue in rowValues)
                        {
                            ExcelRange cell = sheet.Cells[csvRowIndex + startRow - skipLinesFromBeginning, csvFieldIndex + startColumn];
                            if (fieldValue.StartsWith("=") == true)
                            {
                                cell.Formula = fieldValue;
                            }
                            
                            else
                            {
                                Double numValue;
                                bool boolValue;
                                // Try some casting
                                if (Double.TryParse(fieldValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out numValue) == true)
                                {
                                    cell.Value = numValue;
                                }
                                else if (Boolean.TryParse(fieldValue, out boolValue) == true)
                                {
                                    cell.Value = boolValue;
                                }
                                else
                                {
                                    cell.Value = fieldValue;
                                }
                            }
                            csvFieldIndex++;
                        }
                    }
                }

                return sheet.Cells[startRow, startColumn, startRow + csvRowIndex , startColumn + numColumnsInCSV - 1];

            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "ProcessJob.readCSVFileIntoExcelRange",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_FILE,
                    "ProcessJob.readCSVFileIntoExcelRange",
                    String.Format("Unable to read from file='{0}'", csvFilePath));
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "ProcessJob.readCSVFileIntoExcelRange",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.UNABLE_TO_READ_CSV,
                    "ProcessJob.readCSVFileIntoExcelRange",
                    String.Format("Unable to read CSV from file='{0}'", csvFilePath));
            }

            return null;
        }

        private static int getApplicationIdFromApplicationFile(string applicationFilePath)
        {
            //JArray application = loadArrayFromFile(applicationFilePath);
            List<AppDRESTApplication> applicationsList = loadListOfObjectsFromFile<AppDRESTApplication>(applicationFilePath);
            if (applicationsList != null && applicationsList.Count > 0)
            {
                return applicationsList[0].id;
                //Int32.TryParse(application.First["id"].ToString(), out appicationId);
            }
            else
            {
                return -1;
            }
        }
        private static string getShortenedEntityNameForFileSystem(string entityName, int entityID)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getFileSystemSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 25) entityName = entityName.Substring(0, 25);


            if (entityID < 0)
            {
                entityID = originalEntityName.GetHashCode();
            }

            // Third, add hash
            //return String.Format("{0}.{1}.{2:X}", entityName, entityID, originalEntityName.GetHashCode());
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

    }
}
