using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class FilePathMap
    {

        #region Constants for the folder and file names of data extract

        private const string DATA_FOLDER_NAME = "Data";

        // Parent Folder names
        private const string ENTITIES_FOLDER_NAME = "ENT";
        private const string SIM_ENTITIES_FOLDER_NAME = "SIMENT";
        private const string CONFIGURATION_FOLDER_NAME = "CFG";
        private const string METRICS_FOLDER_NAME = "METR";
        private const string SNAPSHOTS_FOLDER_NAME = "SNAP";
        //private const string SNAPSHOT_FOLDER_NAME = "{0}.{1:yyyyMMddHHmmss}";
        private const string EVENTS_FOLDER_NAME = "EVT";
        private const string SA_EVENTS_FOLDER_NAME = "SAEVT";
        private const string ACTIVITYGRID_FOLDER_NAME = "FLOW";
        private const string CONFIGURATION_COMPARISON_FOLDER_NAME = "CMPR";
        private const string PROCESSES_FOLDER_NAME = "PROC";

        // Metadata file names
        private const string EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME = "configuration.xml";
        private const string EXTRACT_CONFIGURATION_CONTROLLER_FILE_NAME = "settings.json";
        private const string EXTRACT_CONTROLLER_VERSION_FILE_NAME = "controllerversion.xml";
        private const string EXTRACT_ENTITY_APPLICATIONS_FILE_NAME = "applications.json";
        private const string EXTRACT_ENTITY_APPLICATION_FILE_NAME = "application.json";
        private const string EXTRACT_ENTITY_TIERS_FILE_NAME = "tiers.json";
        private const string EXTRACT_ENTITY_NODES_FILE_NAME = "nodes.json";
        private const string EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.json";
        private const string EXTRACT_ENTITY_BACKENDS_FILE_NAME = "backends.json";
        private const string EXTRACT_ENTITY_BACKENDS_DETAIL_FILE_NAME = "backendsdetail.json";
        private const string EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME = "serviceendpoints.json";
        private const string EXTRACT_ENTITY_SERVICE_ENDPOINTS_DETAIL_FILE_NAME = "serviceendpointsdetail.json";
        private const string EXTRACT_ENTITY_ERRORS_FILE_NAME = "errors.json";
        private const string EXTRACT_ENTITY_INFORMATION_POINTS_FILE_NAME = "informationpoints.json";
        private const string EXTRACT_ENTITY_INFORMATION_POINTS_DETAIL_FILE_NAME = "informationpointsdetail.json";
        private const string EXTRACT_ENTITY_NODE_RUNTIME_PROPERTIES_FILE_NAME = "node.{0}.json";

        // SIM metadata file names
        private const string EXTRACT_ENTITY_GROUPS_FILE_NAME = "groups.json";
        private const string EXTRACT_ENTITY_MACHINES_FILE_NAME = "machines.json";
        private const string EXTRACT_ENTITY_MACHINE_FILE_NAME = "machine.{0}.json";
        private const string EXTRACT_ENTITY_DOCKER_CONTAINERS_FILE_NAME = "dockercontainer.{0}.json";
        private const string EXTRACT_ENTITY_SERVICE_AVAILABILITIES_FILE_NAME = "serviceavailabilities.json";
        private const string EXTRACT_ENTITY_SERVICE_AVAILABILITY_FILE_NAME = "serviceavailability.{0}.json";

        // SIM process file names
        private const string EXTRACT_PROCESSES_FILE_NAME = "proc.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // Metric data file names
        private const string EXTRACT_METRIC_FULL_FILE_NAME = "full.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_METRIC_HOUR_FILE_NAME = "hour.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // Events data file names
        private const string HEALTH_RULE_VIOLATIONS_FILE_NAME = "healthruleviolations.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EVENTS_FILE_NAME = "{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // SIM Service Availability events data file names
        private const string SERVICE_AVAILABILITY_EVENTS_FILE_NAME = "saevents.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // List of Snapshots file names
        private const string EXTRACT_SNAPSHOTS_FILE_NAME = "snapshots.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // Snapshot file names
        private const string EXTRACT_SNAPSHOT_FILE_NAME = "{0}.{1}.{2:yyyyMMddHHmmss}.json";

        // Flowmap file names
        private const string EXTRACT_ENTITY_FLOWMAP_FILE_NAME = "flowmap.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        #endregion

        #region Constants for the folder and file names of data index

        private const string INDEX_FOLDER_NAME = "Index";

        // Detected entity report conversion file names
        private const string CONVERT_ENTITY_CONTROLLER_FILE_NAME = "controller.csv";
        private const string CONVERT_ENTITY_CONTROLLERS_FILE_NAME = "controllers.csv";
        private const string CONVERT_ENTITY_APPLICATIONS_FILE_NAME = "applications.csv";
        private const string CONVERT_ENTITY_APPLICATION_FILE_NAME = "application.csv";
        private const string CONVERT_ENTITY_TIERS_FILE_NAME = "tiers.csv";
        private const string CONVERT_ENTITY_NODES_FILE_NAME = "nodes.csv";
        private const string CONVERT_ENTITY_NODE_STARTUP_OPTIONS_FILE_NAME = "nodestartupoptions.csv";
        private const string CONVERT_ENTITY_NODE_PROPERTIES_FILE_NAME = "nodeproperties.csv";
        private const string CONVERT_ENTITY_NODE_ENVIRONMENT_VARIABLES_FILE_NAME = "nodeenvironmentvariables.csv";
        private const string CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.csv";
        private const string CONVERT_ENTITY_BACKENDS_FILE_NAME = "backends.csv";
        private const string CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME = "serviceendpoints.csv";
        private const string CONVERT_ENTITY_ERRORS_FILE_NAME = "errors.csv";
        private const string CONVERT_ENTITY_INFORMATION_POINTS_FILE_NAME = "informationpoints.csv";

        // Detected SIM entity report conversion file names
        private const string CONVERT_ENTITY_MACHINES_FILE_NAME = "machines.csv";
        private const string CONVERT_ENTITY_MACHINE_PROPERTIES_FILE_NAME = "machineproperties.csv";
        private const string CONVERT_ENTITY_MACHINE_CPUS_FILE_NAME = "machinecpus.csv";
        private const string CONVERT_ENTITY_MACHINE_VOLUMES_FILE_NAME = "machinevolumes.csv";
        private const string CONVERT_ENTITY_MACHINE_NETWORKS_FILE_NAME = "machinenetworks.csv";
        private const string CONVERT_ENTITY_MACHINE_CONTAINERS_FILE_NAME = "machinecontainers.csv";
        private const string CONVERT_ENTITY_MACHINE_PROCESSES_FILE_NAME = "machineprocesses.csv";

        // Settings report list conversion file names
        private const string CONTROLLER_SETTINGS_FILE_NAME = "controller.settings.csv";
        private const string APPLICATION_CONFIGURATION_FILE_NAME = "application.configuration.csv";
        private const string APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_DISCOVERY_RULES_FILE_NAME = "btdiscovery.rules.csv";
        private const string APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_DISCOVERY_RULES_2_0_FILE_NAME = "btdiscovery.rules.2.0.csv";
        private const string APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_RULES_FILE_NAME = "btentry.rules.csv";
        private const string APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_RULES_2_0_FILE_NAME = "btentry.rules.2.0.csv";
        private const string APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_SCOPES_FILE_NAME = "btentry.scopes.csv";
        private const string APPLICATION_CONFIGURATION_BACKEND_DISCOVERY_RULES_FILE_NAME = "backend.rules.csv";
        private const string APPLICATION_CONFIGURATION_CUSTOM_EXIT_RULES_FILE_NAME = "customexit.rules.csv";
        private const string APPLICATION_CONFIGURATION_INFORMATION_POINT_RULES_FILE_NAME = "infopoints.csv";
        private const string APPLICATION_CONFIGURATION_AGENT_CONFIGURATION_PROPERTIES_FILE_NAME = "agent.properties.csv";
        private const string APPLICATION_CONFIGURATION_METHOD_INVOCATION_DATA_COLLECTORS_FILE_NAME = "datacollectors.midc.csv";
        private const string APPLICATION_CONFIGURATION_HTTP_DATA_COLLECTORS_FILE_NAME = "datacollectors.http.csv";
        private const string APPLICATION_CONFIGURATION_ENTITY_TIERS_FILE_NAME = "tiers.configuration.csv";
        private const string APPLICATION_CONFIGURATION_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "bts.configuration.csv";
        private const string APPLICATION_CONFIGURATION_AGENT_CALL_GRAPH_SETTINGS_FILE_NAME = "callgraphs.configuration.csv";
        private const string APPLICATION_CONFIGURATION_HEALTH_RULES_FILE_NAME = "healthrules.csv";

        // Configuration comparison report list conversion file names
        private const string CONFIGURATION_DIFFERENCES_FILE_NAME = "configuration.differences.csv";

        // Metric report conversion file names
        private const string CONVERT_ENTITIES_METRICS_SUMMARY_FULLRANGE_FILE_NAME = "entities.full.csv";
        private const string CONVERT_ENTITIES_METRICS_SUMMARY_HOURLY_FILE_NAME = "entities.hour.csv";
        private const string CONVERT_ENTITIES_ALL_METRICS_SUMMARY_FULLRANGE_FILE_NAME = "{0}.entities.full.csv";
        private const string CONVERT_ENTITIES_ALL_METRICS_SUMMARY_HOURLY_FILE_NAME = "{0}.entities.hour.csv";
        private const string CONVERT_ENTITIES_METRICS_VALUES_FILE_NAME = "{0}.metricvalues.csv";
        private const string CONVERT_ENTITIES_ALL_METRICS_VALUES_FILE_NAME = "{0}.{1}.metricvalues.csv";
        private const string CONVERT_ENTITIES_METRICS_LOCATIONS_FILE_NAME = "metriclocations.csv";

        // Events list conversion file names
        private const string CONVERT_APPLICATION_EVENTS_FILE_NAME = "application.events.csv";
        private const string CONVERT_EVENTS_FILE_NAME = "events.csv";
        private const string CONVERT_HEALTH_RULE_EVENTS_FILE_NAME = "hrviolationevents.csv";

        // Snapshots files
        private const string CONVERT_APPLICATION_SNAPSHOTS_FILE_NAME = "application.snapshots.csv";
        private const string CONVERT_SNAPSHOTS_FILE_NAME = "snapshots.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME = "snapshots.segments.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME = "snapshots.exits.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME = "snapshots.serviceendpoints.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME = "snapshots.errors.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME = "snapshots.businessdata.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME = "snapshots.methodcalllines.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_FILE_NAME = "snapshots.methodcalllinesoccurrences.csv";

        // Folded call stacks rollups
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME = "snapshots.foldedcallstacks.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME = "snapshots.foldedcallstackswithtime.csv";

        // Snapshots files for each BT and time ranges
        private const string CONVERT_SNAPSHOTS_TIMERANGE_FILE_NAME = "snapshots.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_TIMERANGE_FILE_NAME = "snapshots.segments.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_TIMERANGE_FILE_NAME = "snapshots.exits.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_TIMERANGE_FILE_NAME = "snapshots.serviceendpoints.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_TIMERANGE_FILE_NAME = "snapshots.errors.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_TIMERANGE_FILE_NAME = "snapshots.businessdata.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_TIMERANGE_NAME = "snapshots.methodcalllines.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_TIMERANGE_FILE_NAME = "snapshots.methodcalllinesoccurrences.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";

        // Folded call stacks rollups for each BT and Nodes
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_TIMERANGE_FILE_NAME = "snapshots.foldedcallstacks.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_TIMERANGE_FILE_NAME = "snapshots.foldedcallstackswithtime.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";


        // Snapshot files
        private const string CONVERT_SNAPSHOT_FILE_NAME = "snapshot.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_FILE_NAME = "snapshot.segments.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_EXIT_CALLS_FILE_NAME = "snapshot.exits.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME = "snapshot.serviceendpoints.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_DETECTED_ERRORS_FILE_NAME = "snapshot.errors.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_BUSINESS_DATA_FILE_NAME = "snapshot.businessdata.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_METHOD_CALL_LINES_FILE_NAME = "snapshot.methodcalllines.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_FILE_NAME = "snapshot.methodcalllinesoccurrences.csv";

        // Folded call stacks for snapshot
        private const string CONVERT_SNAPSHOT_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME = "snapshot.foldedcallstacks.csv";
        private const string CONVERT_SNAPSHOT_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME = "snapshot.foldedcallstacks.withtime.csv";

        // Flow map to flow grid conversion file names
        private const string CONVERT_ACTIVITY_GRIDS_FILE_NAME = "activitygrids.full.csv";
        private const string CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME = "{0}.activitygrids.full.csv";
        private const string CONVERT_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME = "activitygrids.perminute.full.csv";
        private const string CONVERT_ALL_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME = "{0}.activitygrids.perminute.full.csv";

        #endregion

        #region Constants for the folder and file names of data reports

        private const string REPORT_FOLDER_NAME = "Report";

        // Report file names
        private const string REPORT_DETECTED_ENTITIES_FILE_NAME = "DetectedEntities.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";
        private const string REPORT_DETECTED_SIM_ENTITIES_FILE_NAME = "DetectedEntities.SIM.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";
        private const string REPORT_METRICS_ALL_ENTITIES_FILE_NAME = "EntityMetrics.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";
        private const string REPORT_DETECTED_EVENTS_FILE_NAME = "Events.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";
        private const string REPORT_SNAPSHOTS_FILE_NAME = "Snapshots.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";
        private const string REPORT_SNAPSHOTS_METHOD_CALL_LINES_FILE_NAME = "Snapshots.MethodCallLines.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";
        private const string REPORT_CONFIGURATION_FILE_NAME = "Configuration.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.xlsx";

        // Per entity report names
        private const string REPORT_ENTITY_DETAILS_APPLICATION_FILE_NAME = "EntityDetails.{0}.{1}.{2:yyyyMMddHHmm}-{3:yyyyMMddHHmm}.xlsx";
        private const string REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME = "EntityDetails.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.xlsx";
        private const string REPORT_METRICS_GRAPHS_FILE_NAME = "MetricGraphs.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.xlsx";

        // Per entity flame graph report name
        private const string REPORT_FLAME_GRAPH_APPLICATION_FILE_NAME = "FlameGraph.Application.{0}.{1}.{2:yyyyMMddHHmm}-{3:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_GRAPH_TIER_FILE_NAME = "FlameGraph.Tier.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_GRAPH_NODE_FILE_NAME = "FlameGraph.Node.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_GRAPH_BUSINESS_TRANSACTION_FILE_NAME = "FlameGraph.BT.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_GRAPH_SNAPSHOT_FILE_NAME = "FlameGraph.Snapshot.{0}.{1:yyyyMMddHHmmss}.{2}.svg";

        // Per entity flame chart report name
        private const string REPORT_FLAME_CHART_APPLICATION_FILE_NAME = "FlameChart.Application.{0}.{1}.{2:yyyyMMddHHmm}-{3:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_CHART_TIER_FILE_NAME = "FlameChart.Tier.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_CHART_NODE_FILE_NAME = "FlameChart.Node.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.svg";
        private const string REPORT_FLAME_CHART_BUSINESS_TRANSACTION_FILE_NAME = "FlameChart.BT.{0}.{1}.{2}.{3:yyyyMMddHHmm}-{4:yyyyMMddHHmm}.svg";

        #endregion

        #region Constants for Step Timing report

        private const string TIMING_REPORT_FILE_NAME = "StepDurations.csv";

        #endregion

        #region Constants for lookup and external files

        // Settings for method and call mapping
        private const string METHOD_CALL_LINES_TO_FRAMEWORK_TYPE_MAPPING_FILE_NAME = "MethodNamespaceTypeMapping.csv";

        // Settings for the metric extracts
        private const string ENTITY_METRICS_EXTRACT_MAPPING_FILE_NAME = "EntityMetricsExtractMapping.csv";

        // Flame graph template SVG XML file
        private const string FLAME_GRAPH_TEMPLATE_FILE_NAME = "FlameGraphTemplate.svg";

        // Template application export of an empty application
        private const string TEMPLATE_APPLICATION_CONFIGURATION_FILE_NAME = "TemplateApplicationConfiguration.xml";


        #endregion

        #region Snapshot UX to Folder Mapping

        internal static Dictionary<string, string> USEREXPERIENCE_FOLDER_MAPPING = new Dictionary<string, string>
        {
            {"NORMAL", "NM"},
            {"SLOW", "SL"},
            {"VERY_SLOW", "VS"},
            {"STALL", "ST"},
            {"ERROR", "ER"}
        };

        #endregion

        #region Constructor and properties

        public ProgramOptions ProgramOptions { get; set; }

        public JobConfiguration JobConfiguration { get; set; }

        public FilePathMap(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            this.ProgramOptions = programOptions;
            this.JobConfiguration = jobConfiguration;
        }

        #endregion


        #region Step Timing Report

        public string StepTimingReportFilePath()
        {
            return Path.Combine(this.ProgramOptions.OutputJobFolderPath, REPORT_FOLDER_NAME, TIMING_REPORT_FILE_NAME);
        }

        #endregion


        #region Entity Metadata Data

        public string ControllerVersionDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                EXTRACT_CONTROLLER_VERSION_FILE_NAME);
        }

        public string ApplicationsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                EXTRACT_ENTITY_APPLICATIONS_FILE_NAME);
        }

        public string ApplicationDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EXTRACT_ENTITY_APPLICATION_FILE_NAME);
        }

        public string TiersDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_TIERS_FILE_NAME);
        }

        public string NodesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_NODES_FILE_NAME);
        }

        public string BackendsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_BACKENDS_FILE_NAME);
        }

        public string BackendsDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_BACKENDS_DETAIL_FILE_NAME);
        }

        public string BusinessTransactionsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string ServiceEndpointsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
        }

        public string ServiceEndpointsDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_SERVICE_ENDPOINTS_DETAIL_FILE_NAME);
        }

        public string ErrorsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_ERRORS_FILE_NAME);
        }

        public string InformationPointsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_INFORMATION_POINTS_FILE_NAME);
        }

        public string InformationPointsDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_INFORMATION_POINTS_DETAIL_FILE_NAME);
        }

        public string NodeRuntimePropertiesDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, AppDRESTNode node)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_NODE_RUNTIME_PROPERTIES_FILE_NAME,
                getShortenedEntityNameForFileSystem(node.name, node.id));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                reportFileName);
        }

        #endregion

        #region Entity Metadata Index

        public string ControllerIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                CONVERT_ENTITY_CONTROLLER_FILE_NAME);
        }

        public string ApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                CONVERT_ENTITY_APPLICATIONS_FILE_NAME);
        }

        public string ApplicationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONVERT_ENTITY_APPLICATION_FILE_NAME);
        }

        public string TiersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_TIERS_FILE_NAME);
        }

        public string NodesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODES_FILE_NAME);
        }

        public string NodeStartupOptionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODE_STARTUP_OPTIONS_FILE_NAME);
        }

        public string NodePropertiesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODE_PROPERTIES_FILE_NAME);
        }

        public string NodeEnvironmentVariablesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODE_ENVIRONMENT_VARIABLES_FILE_NAME);
        }

        public string BackendsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_BACKENDS_FILE_NAME);
        }

        public string BusinessTransactionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string ServiceEndpointsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
        }

        public string ErrorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_ERRORS_FILE_NAME);
        }

        public string InformationPointsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_INFORMATION_POINTS_FILE_NAME);
        }

        #endregion

        #region Entity Metadata Report

        public string ReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME);
        }

        public string EntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME);
        }

        public string ControllersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_CONTROLLERS_FILE_NAME);
        }

        public string ApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_APPLICATIONS_FILE_NAME);
        }

        public string TiersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_TIERS_FILE_NAME);
        }

        public string NodesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODES_FILE_NAME);
        }

        public string NodeStartupOptionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODE_STARTUP_OPTIONS_FILE_NAME);
        }

        public string NodePropertiesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODE_PROPERTIES_FILE_NAME);
        }

        public string NodeEnvironmentVariablesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODE_ENVIRONMENT_VARIABLES_FILE_NAME);
        }

        public string BackendsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_BACKENDS_FILE_NAME);
        }

        public string BusinessTransactionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string ServiceEndpointsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
        }

        public string ErrorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_ERRORS_FILE_NAME);
        }

        public string InformationPointsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_INFORMATION_POINTS_FILE_NAME);
        }

        public string EntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        #endregion


        #region SIM Entity Metadata Data

        public string SIMTiersDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_TIERS_FILE_NAME);
        }

        public string SIMNodesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_NODES_FILE_NAME);
        }

        public string SIMGroupsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_GROUPS_FILE_NAME);
        }

        public string SIMMachinesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_MACHINES_FILE_NAME);
        }

        public string SIMMachineDataFilePath(JobTarget jobTarget, string machineName, long machineID)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_MACHINE_FILE_NAME,
                getShortenedEntityNameForFileSystem(machineName, machineID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        public string SIMMachineDockerContainersDataFilePath(JobTarget jobTarget, string machineName, long machineID)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_DOCKER_CONTAINERS_FILE_NAME,
                getShortenedEntityNameForFileSystem(machineName, machineID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        public string SIMServiceAvailabilitiesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_SERVICE_AVAILABILITIES_FILE_NAME);
        }

        public string SIMServiceAvailabilityDataFilePath(JobTarget jobTarget, string saName, long saID)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_SERVICE_AVAILABILITY_FILE_NAME,
                getShortenedEntityNameForFileSystem(saName, saID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        public string SIMMachineProcessesDataFilePath(JobTarget jobTarget, string machineName, long machineID, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_PROCESSES_FILE_NAME,
                getShortenedEntityNameForFileSystem(machineName, machineID),
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                PROCESSES_FOLDER_NAME,
                reportFileName);
        }

        public string SIMServiceAvailabilityEventsDataFilePath(JobTarget jobTarget, string saName, long saID, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                SERVICE_AVAILABILITY_EVENTS_FILE_NAME,
                getShortenedEntityNameForFileSystem(saName, saID),
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SA_EVENTS_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region SIM Entity Metadata Index

        public string SIMApplicationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_APPLICATION_FILE_NAME);
        }

        public string SIMTiersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_TIERS_FILE_NAME);
        }

        public string SIMNodesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODES_FILE_NAME);
        }

        public string SIMMachinesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINES_FILE_NAME);
        }

        public string SIMMachinePropertiesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_PROPERTIES_FILE_NAME);
        }

        public string SIMMachineCPUsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_CPUS_FILE_NAME);
        }

        public string SIMMachineVolumesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_VOLUMES_FILE_NAME);
        }

        public string SIMMachineNetworksIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_NETWORKS_FILE_NAME);
        }

        public string SIMMachineContainersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_CONTAINERS_FILE_NAME);
        }

        public string SIMMachineProcessesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                PROCESSES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_PROCESSES_FILE_NAME);
        }

        #endregion

        #region SIM Entity Metadata Report

        public string SIMEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME);
        }

        public string SIMApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_APPLICATIONS_FILE_NAME);
        }

        public string SIMTiersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_TIERS_FILE_NAME);
        }

        public string SIMNodesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_NODES_FILE_NAME);
        }

        public string SIMMachinesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINES_FILE_NAME);
        }

        public string SIMMachinePropertiesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_PROPERTIES_FILE_NAME);
        }

        public string SIMMachineCPUsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_CPUS_FILE_NAME);
        }

        public string SIMMachineVolumesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_VOLUMES_FILE_NAME);
        }

        public string SIMMachineNetworksReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_NETWORKS_FILE_NAME);
        }

        public string SIMMachineContainersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_CONTAINERS_FILE_NAME);
        }

        public string SIMMachineProcessesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SIM_ENTITIES_FOLDER_NAME,
                CONVERT_ENTITY_MACHINE_PROCESSES_FILE_NAME);
        }

        public string SIMEntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_SIM_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        #endregion


        #region Configuration Data

        public string ControllerSettingsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                EXTRACT_CONFIGURATION_CONTROLLER_FILE_NAME);
        }

        public string ApplicationConfigurationDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME);
        }

        #endregion

        #region Configuration Index

        public string ControllerSettingsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                CONTROLLER_SETTINGS_FILE_NAME);
        }

        public string ApplicationConfigurationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_FILE_NAME);
        }

        public string BusinessTransactionDiscoveryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_DISCOVERY_RULES_FILE_NAME);
        }

        public string BusinessTransactionEntryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_RULES_FILE_NAME);
        }

        public string BusinessTransactionEntryScopesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_SCOPES_FILE_NAME);
        }

        public string BusinessTransactionEntryRules20IndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_RULES_2_0_FILE_NAME);
        }

        public string BusinessTransactionDiscoveryRules20IndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_DISCOVERY_RULES_2_0_FILE_NAME);
        }

        public string BackendDiscoveryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BACKEND_DISCOVERY_RULES_FILE_NAME);
        }

        public string CustomExitRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_CUSTOM_EXIT_RULES_FILE_NAME);
        }

        public string AgentConfigurationPropertiesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_AGENT_CONFIGURATION_PROPERTIES_FILE_NAME);
        }

        public string InformationPointRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_INFORMATION_POINT_RULES_FILE_NAME);
        }

        public string EntityBusinessTransactionConfigurationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string EntityTierConfigurationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_ENTITY_TIERS_FILE_NAME);
        }

        public string MethodInvocationDataCollectorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_METHOD_INVOCATION_DATA_COLLECTORS_FILE_NAME);
        }

        public string HttpDataCollectorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_HTTP_DATA_COLLECTORS_FILE_NAME);
        }

        public string AgentCallGraphSettingsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_AGENT_CALL_GRAPH_SETTINGS_FILE_NAME);
        }

        public string HealthRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_HEALTH_RULES_FILE_NAME);
        }

        #endregion

        #region Configuration Report

        public string ConfigurationReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME);
        }

        public string ControllerSettingsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                CONTROLLER_SETTINGS_FILE_NAME);
        }

        public string ApplicationConfigurationReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_FILE_NAME);
        }

        public string BusinessTransactionDiscoveryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_DISCOVERY_RULES_FILE_NAME);
        }

        public string BusinessTransactionEntryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_RULES_FILE_NAME);
        }

        public string BusinessTransactionEntryScopesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_SCOPES_FILE_NAME);
        }

        public string BusinessTransactionEntryRules20ReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_ENTRY_RULES_2_0_FILE_NAME);
        }

        public string BusinessTransactionDiscoveryRules20ReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BUSINESS_TRANSACTION_DISCOVERY_RULES_2_0_FILE_NAME);
        }

        public string BackendDiscoveryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_BACKEND_DISCOVERY_RULES_FILE_NAME);
        }

        public string CustomExitRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_CUSTOM_EXIT_RULES_FILE_NAME);
        }

        public string AgentConfigurationPropertiesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_AGENT_CONFIGURATION_PROPERTIES_FILE_NAME);
        }

        public string InformationPointRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_INFORMATION_POINT_RULES_FILE_NAME);
        }

        public string EntityBusinessTransactionConfigurationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string EntityTierConfigurationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_ENTITY_TIERS_FILE_NAME);
        }

        public string MethodInvocationDataCollectorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_METHOD_INVOCATION_DATA_COLLECTORS_FILE_NAME);
        }

        public string HttpDataCollectorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_HTTP_DATA_COLLECTORS_FILE_NAME);
        }

        public string AgentCallGraphSettingsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_AGENT_CALL_GRAPH_SETTINGS_FILE_NAME);
        }

        public string HealthRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_FOLDER_NAME,
                APPLICATION_CONFIGURATION_HEALTH_RULES_FILE_NAME);
        }

        public string ConfigurationExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_CONFIGURATION_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        #endregion


        #region Configuration Comparison Data

        public string TemplateApplicationConfigurationFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                TEMPLATE_APPLICATION_CONFIGURATION_FILE_NAME);
        }

        #endregion

        #region Configuration Comparison Index

        public string ConfigurationComparisonIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_COMPARISON_FOLDER_NAME,
                CONFIGURATION_DIFFERENCES_FILE_NAME);
        }

        #endregion

        #region Configuration Comparison Report

        public string ConfigurationComparisonReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_COMPARISON_FOLDER_NAME);
        }

        public string ConfigurationComparisonReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                CONFIGURATION_COMPARISON_FOLDER_NAME,
                CONFIGURATION_DIFFERENCES_FILE_NAME);
        }

        #endregion


        #region Entity Metrics Data

        public string EntityMetricExtractMappingFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                ENTITY_METRICS_EXTRACT_MAPPING_FILE_NAME);
        }

        public string MetricFullRangeDataFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_METRIC_FULL_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                entityFolderName,
                metricEntitySubFolderName,
                reportFileName);
        }

        public string MetricHourRangeDataFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_METRIC_HOUR_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                entityFolderName,
                metricEntitySubFolderName,
                reportFileName);
        }

        #endregion

        #region Entity Metrics Index

        public string EntitiesFullIndexFilePath(JobTarget jobTarget, string entityFolderName)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                entityFolderName,
                CONVERT_ENTITIES_METRICS_SUMMARY_FULLRANGE_FILE_NAME);
        }

        public string EntitiesHourIndexFilePath(JobTarget jobTarget, string entityFolderName)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                entityFolderName,
                CONVERT_ENTITIES_METRICS_SUMMARY_HOURLY_FILE_NAME);
        }

        public string MetricValuesIndexFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_METRICS_VALUES_FILE_NAME,
                metricEntitySubFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                entityFolderName,
                reportFileName);
        }

        public string MetricsLocationIndexFilePath(JobTarget jobTarget, string entityFolderName)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                entityFolderName,
                CONVERT_ENTITIES_METRICS_LOCATIONS_FILE_NAME);
        }

        #endregion

        #region Entity Metrics Report

        public string MetricsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                METRICS_FOLDER_NAME);
        }

        public string EntitiesFullReportFilePath(string entityFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_SUMMARY_FULLRANGE_FILE_NAME,
                entityFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,                
                METRICS_FOLDER_NAME,
                reportFileName);
        }

        public string EntitiesHourReportFilePath(string entityFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_SUMMARY_HOURLY_FILE_NAME,
                entityFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                METRICS_FOLDER_NAME,
                reportFileName);
        }

        public string MetricReportFilePath(string entityFolderName, string metricEntitySubFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_VALUES_FILE_NAME,
                entityFolderName,
                metricEntitySubFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                METRICS_FOLDER_NAME,
                reportFileName);
        }

        public string MetricReportPerAppFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_VALUES_FILE_NAME,
                entityFolderName,
                metricEntitySubFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                METRICS_FOLDER_NAME,
                reportFileName);

        }

        public string EntityMetricsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_METRICS_ALL_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region Entity Metric Graphs Report

        public string EntityTypeMetricGraphsExcelReportFilePath(EntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Format(
                REPORT_METRICS_GRAPHS_FILE_NAME,
                entity.FolderName,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                jobTimeRange.From,
                jobTimeRange.To);

            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    REPORT_FOLDER_NAME,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    entity.FolderName,
                    reportFileName);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    entity.FolderName,
                    reportFileName);
            }

            return reportFilePath;
        }

        #endregion


        #region Entity Flowmap Data

        public string ApplicationFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityApplication.ENTITY_FOLDER,
                reportFileName);
        }

        public string TierFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, AppDRESTTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityTier.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(tier.name, tier.id),
                reportFileName);
        }

        public string TierFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, EntityTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityTier.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID),
                reportFileName);
        }

        public string NodeFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, AppDRESTNode node)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                getShortenedEntityNameForFileSystem(node.name, node.id),
                reportFileName);
        }

        public string NodeFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, EntityNode node)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.TierName, node.TierID),
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                reportFileName);
        }

        public string BackendFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, AppDRESTBackend backend)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityBackend.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(backend.name, backend.id),
                reportFileName);
        }

        public string BackendFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, EntityBackend backend)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityBackend.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(backend.BackendName, backend.BackendID),
                reportFileName);
        }

        public string BusinessTransactionFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, AppDRESTBusinessTransaction businessTransaction)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityBusinessTransaction.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(businessTransaction.tierName, businessTransaction.tierId),
                getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id),
                reportFileName);
        }

        public string BusinessTransactionFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, EntityBusinessTransaction businessTransaction)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityBusinessTransaction.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        #endregion

        #region Entity Flowmap Index

        public string ApplicationFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityApplication.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string ApplicationFlowmapPerMinuteIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityApplication.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME);
        }

        public string TiersFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityTier.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string NodesFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityNode.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string BackendsFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityBackend.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string BusinessTransactionsFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ACTIVITYGRID_FOLDER_NAME,
                EntityBusinessTransaction.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        #endregion

        #region Entity Flowmap Report

        public string ActivityGridReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME);
        }

        public string ApplicationsFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                EntityApplication.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string ApplicationsFlowmapPerMinuteReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME,
                EntityApplication.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string TiersFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                EntityTier.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string NodesFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                EntityNode.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string BackendsFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                EntityBackend.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string BusinessTransactionsFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                EntityBusinessTransaction.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        #endregion


        #region Events Data

        public string HealthRuleViolationsDataFilePath(JobTarget jobTarget)
        {
            string reportFileName = String.Format(
                HEALTH_RULE_VIOLATIONS_FILE_NAME,
                this.JobConfiguration.Input.TimeRange.From,
                this.JobConfiguration.Input.TimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EVENTS_FOLDER_NAME,
                reportFileName);
        }

        public string EventsDataFilePath(JobTarget jobTarget, string eventType)
        {
            string reportFileName = String.Format(
                EVENTS_FILE_NAME,
                eventType,
                this.JobConfiguration.Input.TimeRange.From,
                this.JobConfiguration.Input.TimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EVENTS_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region Events Index

        public string HealthRuleViolationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EVENTS_FOLDER_NAME,
                CONVERT_HEALTH_RULE_EVENTS_FILE_NAME);
        }

        public string EventsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EVENTS_FOLDER_NAME,
                CONVERT_EVENTS_FILE_NAME);
        }

        public string ApplicationEventsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EVENTS_FOLDER_NAME,
                CONVERT_APPLICATION_EVENTS_FILE_NAME);
        }

        #endregion

        #region Events Report

        public string EventsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                EVENTS_FOLDER_NAME);
        }

        public string HealthRuleViolationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                EVENTS_FOLDER_NAME,
                CONVERT_HEALTH_RULE_EVENTS_FILE_NAME);
        }

        public string EventsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                EVENTS_FOLDER_NAME,
                CONVERT_EVENTS_FILE_NAME);
        }

        public string ApplicationEventsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                EVENTS_FOLDER_NAME,
                CONVERT_APPLICATION_EVENTS_FILE_NAME);
        }

        public string EventsAndHealthRuleViolationsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_EVENTS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        #endregion


        #region Snapshots Data

        public string SnapshotsDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_SNAPSHOTS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                reportFileName);
        }

        public string SnapshotDataFilePath(
            JobTarget jobTarget,
            string tierName, long tierID,
            string businessTransactionName, long businessTransactionID,
            DateTime snapshotTime,
            string userExperience,
            string requestID)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tierName, tierID),
                getShortenedEntityNameForFileSystem(businessTransactionName, businessTransactionID),
                String.Format(EXTRACT_SNAPSHOT_FILE_NAME, USEREXPERIENCE_FOLDER_MAPPING[userExperience], requestID, snapshotTime));
        }

        public string MethodCallLinesToFrameworkTypetMappingFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                METHOD_CALL_LINES_TO_FRAMEWORK_TYPE_MAPPING_FILE_NAME);
        }

        #endregion

        #region Snapshots Index

        #region Snapshots Business Transaction for Time Range

        public string SnapshotsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsSegmentsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsExitCallsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsServiceEndpointCallsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsDetectedErrorsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsBusinessDataIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsMethodCallLinesIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_TIMERANGE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsMethodCallLinesOccurrencesIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        #endregion

        #region Snapshots Business Transaction

        public string SnapshotsIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsSegmentsIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
        }

        public string SnapshotsExitCallsIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
        }

        public string SnapshotsServiceEndpointCallsIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
        }

        public string SnapshotsDetectedErrorsIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
        }

        public string SnapshotsBusinessDataIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesOccurrencesIndexBusinessTransactionFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_FILE_NAME);
        }

        #endregion

        #region Snapshots All

        public string SnapshotsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsSegmentsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOT_SEGMENTS_FILE_NAME);
        }

        public string SnapshotsExitCallsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
        }

        public string SnapshotsServiceEndpointCallsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
        }

        public string SnapshotsDetectedErrorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
        }

        public string SnapshotsBusinessDataIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesOccurrencesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_FILE_NAME);
        }

        #endregion

        #region Snapshots Folded Call Stacks All

        public string SnapshotsFoldedCallStacksIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksIndexBusinessTransactionNodeHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, EntityNode node, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                EntityNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionNodeHourRangeFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction, EntityNode node, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                EntityNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksIndexApplicationFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksIndexEntityFilePath(JobTarget jobTarget, EntityTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksIndexEntityFilePath(JobTarget jobTarget, EntityNode node)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                EntityNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.TierName, node.TierID),
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksIndexEntityFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexApplicationFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(JobTarget jobTarget, EntityTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(JobTarget jobTarget, EntityNode node)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                EntityNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.TierName, node.TierID),
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(JobTarget jobTarget, EntityBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        #endregion

        public string ApplicationSnapshotsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_APPLICATION_SNAPSHOTS_FILE_NAME);
        }

        #endregion

        #region Snapshots Report

        public string SnapshotsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME);
        }

        public string SnapshotsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsSegmentsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
        }

        public string SnapshotsExitCallsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
        }

        public string SnapshotsServiceEndpointCallsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
        }

        public string SnapshotsDetectedErrorsCallsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
        }

        public string SnapshotsBusinessDataReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesOccurrencesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_OCCURRENCES_FILE_NAME);
        }

        public string ApplicationSnapshotsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                SNAPSHOTS_FOLDER_NAME,
                CONVERT_APPLICATION_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_SNAPSHOTS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        public string SnapshotMethodCallsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_SNAPSHOTS_METHOD_CALL_LINES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                REPORT_FOLDER_NAME,
                reportFileName);
        }

        #endregion


        #region Flame Graph Report

        public string FlameGraphTemplateFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                FLAME_GRAPH_TEMPLATE_FILE_NAME);
        }

        public string FlameGraphReportFilePath(EntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entity is EntityApplication)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_APPLICATION_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityTier)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_TIER_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityNode)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_NODE_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityBusinessTransaction)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_BUSINESS_TRANSACTION_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }

            if (reportFileName.Length > 0)
            {
                if (absolutePath == true)
                {
                    reportFilePath = Path.Combine(
                        this.ProgramOptions.OutputJobFolderPath,
                        REPORT_FOLDER_NAME,
                        getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        public string FlameChartReportFilePath(EntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entity is EntityApplication)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_APPLICATION_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityTier)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_TIER_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityNode)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_NODE_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityBusinessTransaction)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_BUSINESS_TRANSACTION_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }

            if (reportFileName.Length > 0)
            {
                if (absolutePath == true)
                {
                    reportFilePath = Path.Combine(
                        this.ProgramOptions.OutputJobFolderPath,
                        REPORT_FOLDER_NAME,
                        getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        public string FlameGraphReportFilePath(Snapshot snapshot, JobTarget jobTarget, bool absolutePath)
        {
            string reportFileName = String.Format(
                REPORT_FLAME_GRAPH_SNAPSHOT_FILE_NAME,
                snapshot.UserExperience,
                snapshot.OccurredUtc,
                snapshot.RequestID);

            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    REPORT_FOLDER_NAME,
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    reportFileName);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    reportFileName);
            }

            return reportFilePath;
        }

        #endregion


        #region Entity Details Report

        public string EntityMetricAndDetailExcelReportFilePath(EntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entity is EntityApplication)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_APPLICATION_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityTier)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityNode)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityBackend)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityBusinessTransaction)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityServiceEndpoint)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is EntityError)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(new Uri(entity.Controller).Host),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }

            if (reportFileName.Length > 0)
            {
                if (absolutePath == true)
                {
                    reportFilePath = Path.Combine(
                        this.ProgramOptions.OutputJobFolderPath,
                        REPORT_FOLDER_NAME,
                        getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(new Uri(jobTarget.Controller).Host),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        #endregion


        #region Helper function for various entity naming

        public static string getFileSystemSafeString(string fileOrFolderNameToClear)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileOrFolderNameToClear = fileOrFolderNameToClear.Replace(c, '-');
            }

            return fileOrFolderNameToClear;
        }

        public static string getShortenedEntityNameForFileSystem(string entityName, long entityID)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getFileSystemSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 12) entityName = entityName.Substring(0, 12);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        #endregion
    }
}
