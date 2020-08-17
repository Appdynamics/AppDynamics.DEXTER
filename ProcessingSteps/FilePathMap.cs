using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.IO;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class FilePathMap
    {

        #region Constants for the folder names

        // Parent Folder names
        private const string ENTITIES_FOLDER_NAME = "ENT";
        private const string ENTITIES_APM_FOLDER_NAME = "ENTAPM";
        private const string ENTITIES_SIM_FOLDER_NAME = "ENTSIM";
        private const string ENTITIES_DB_FOLDER_NAME = "ENTDB";
        private const string ENTITIES_WEB_FOLDER_NAME = "ENTWEB";
        private const string ENTITIES_MOBILE_FOLDER_NAME = "ENTMOBILE";
        private const string ENTITIES_BIQ_FOLDER_NAME = "ENTBIQ";

        private const string CONFIGURATION_FOLDER_NAME = "CFG";
        private const string CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME = "CFGAPP";
        private const string CONFIGURATION_APM_FOLDER_NAME = "CFGAPM";
        private const string CONFIGURATION_DB_FOLDER_NAME = "CFGDB";
        private const string CONFIGURATION_WEB_FOLDER_NAME = "CFGWEB";
        private const string CONFIGURATION_MOBILE_FOLDER_NAME = "CFGMOBILE";
        private const string CONFIGURATION_ANALYTICS_FOLDER_NAME = "CFGBIQ";
        private const string CONFIGURATION_COMPARISON_FOLDER_NAME = "CMPR";

        private const string DASHBOARDS_FOLDER_NAME = "DASH";

        private const string LICENSE_FOLDER_NAME = "LIC";

        private const string EVENTS_FOLDER_NAME = "EVT";
        private const string EVENTS_APPS_FOLDER_NAME = "EVTAPP";
        private const string EVENTS_CONTROLLER_FOLDER_NAME = "EVTCNTR";
        private const string EVENTS_SA_FOLDER_NAME = "EVTSA";

        private const string APM_ACTIVITYGRID_FOLDER_NAME = "FLOW";
        private const string APP_METRICS_FOLDER_NAME = "METR";
        private const string APP_METRICS_APM_FOLDER_NAME = "METRAPM";
        private const string APP_METRICS_WEB_FOLDER_NAME = "METRWEB";
        private const string APP_METRICS_MOBILE_FOLDER_NAME = "METRMOBILE";
        private const string APP_METRICS_SIM_FOLDER_NAME = "METRSIM";
        private const string APP_METRICS_BIQ_FOLDER_NAME = "METRBIQ";
        private const string APP_METRICS_DB_FOLDER_NAME = "METRDB";
        private const string APM_SNAPSHOTS_FOLDER_NAME = "SNAP";
        private const string APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME = "SCRN";
        private const string APP_METRICS_LIST_FOLDER_NAME = "METRLIST";

        private const string SIM_PROCESSES_FOLDER_NAME = "PROC";

        private const string DB_QUERIES_FOLDER_NAME = "QUERY";
        private const string DB_DATA_FOLDER_NAME = "DBDATA";

        private const string CONTROLLER_RBAC_FOLDER_NAME = "RBAC";

        private const string HEALTHCHECK_FOLDER_NAME = "HEALTH";
        private const string HEALTHCHECK_APM_FOLDER_NAME = "HEALTHAPM";

        private const string APM_METRICS_LIST_FOLDER_NAME = "METRLISTAPM";

        #endregion

        #region Constants for the folder and file names of data extract

        private const string DATA_FOLDER_NAME = "Data";

        private const string DBMON_APPLICATION_NAME = "DBMon";

        // Controller wide settings file names
        private const string EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME = "configuration.xml";
        private const string EXTRACT_CONFIGURATION_APPLICATION_DETAILS_FILE_NAME = "configuration.json";
        private const string EXTRACT_CONFIGURATION_APPLICATION_SEP_DETECTION_FILE_NAME = "seps.detection.json";
        private const string EXTRACT_CONFIGURATION_APPLICATION_TIER_SEP_DETECTION_FILE_NAME = "seps.detection.{0}.json";
        private const string EXTRACT_CONFIGURATION_APPLICATION_TIER_SEP_EXPLICIT_FILE_NAME = "seps.rules.{0}.json";

        private const string EXTRACT_CONTROLLER_VERSION_FILE_NAME = "controllerversion.xml";
        private const string EXTRACT_HTTP_TEMPLATES_FILE_NAME = "templateshttp.json";
        private const string EXTRACT_EMAIL_TEMPLATES_FILE_NAME = "templatesemail.json";
        private const string EXTRACT_HTTP_TEMPLATES_DETAIL_FILE_NAME = "templateshttpdetails.json";
        private const string EXTRACT_EMAIL_TEMPLATES_DETAIL_FILE_NAME = "templatesemaildetails.json";

        private const string EXTRACT_CONTROLLER_DASHBOARDS = "dashboards.json";
        private const string EXTRACT_CONTROLLER_DASHBOARD = "{0}.json";

        private const string EXTRACT_ENTITY_ALL_APPLICATIONS_FILE_NAME = "allapplications.json";
        private const string EXTRACT_ENTITY_APM_APPLICATIONS_FILE_NAME = "applications.json";
        private const string EXTRACT_ENTITY_MOBILE_APPLICATIONS_FILE_NAME = "mobileapplications.json";

        // License information
        private const string EXTRACT_LICENSE_ALL_APPLICATIONS_FILE_NAME = "allapplications.lic.json";
        private const string EXTRACT_LICENSE_SIM_MACHINES_FILE_NAME = "sim.machines.json";
        private const string EXTRACT_LICENSE_ACCOUNT_FILE_NAME = "account.json";
        private const string EXTRACT_LICENSE_MODULES_FILE_NAME = "licensemodules.json";
        private const string EXTRACT_LICENSE_MODULE_PROPERTIES_FILE_NAME = "license.props.{0}.json";
        private const string EXTRACT_LICENSE_MODULE_USAGES_FILE_NAME = "license.usage.{0}.json";
        private const string EXTRACT_LICENSE_RULES_FILE_NAME = "licenserules.json";
        private const string EXTRACT_LICENSE_RULE_USAGE_DETAIL_FILE_NAME = "licenserule.usage.{0}.json";
        private const string EXTRACT_LICENSE_RULE_CONFIGURATION_DETAIL_FILE_NAME = "licenserule.config.{0}.json";

        // APM Metadata file names
        private const string EXTRACT_ENTITY_APPLICATION_FILE_NAME = "application.json";
        private const string EXTRACT_ENTITY_TIERS_FILE_NAME = "tiers.json";
        private const string EXTRACT_ENTITY_TIER_BUSINESS_TRANSACTION_OVERFLOW_REGULAR_FILE_NAME = "overflowbt.regular.json";
        private const string EXTRACT_ENTITY_TIER_BUSINESS_TRANSACTION_OVERFLOW_DEBUG_FILE_NAME = "overflowbt.debug.json";
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
        private const string EXTRACT_ENTITY_NODE_METADATA_FILE_NAME = "nodemeta.{0}.json";
        private const string EXTRACT_ENTITY_BACKEND_TO_DBMON_MAPPING_FILE_NAME = "dbmonmap.{0}.json";
        private const string EXTRACT_ENTITY_BACKEND_TO_TIER_MAPPING_FILE_NAME = "backendmapping.json";

        // Configuration file names
        private const string EXTRACT_APPLICATION_HEALTH_RULES_FILE_NAME = "healthrules.xml";
        private const string EXTRACT_APPLICATION_HEALTH_RULES_DETAILS_FILE_NAME = "healthrules.json";
        private const string EXTRACT_APPLICATION_POLICIES_FILE_NAME = "policies.json";
        private const string EXTRACT_APPLICATION_ACTIONS_FILE_NAME = "actions.json";
        private const string EXTRACT_CONFIGURATION_DEVELOPER_MODE_NODES_FILE_NAME = "devmode.json";
        private const string EXTRACT_CONTROLLER_SETTINGS_FILE_NAME = "settings.json";

        // SIM metadata file names
        private const string EXTRACT_ENTITY_GROUPS_FILE_NAME = "groups.json";
        private const string EXTRACT_ENTITY_MACHINES_FILE_NAME = "machines.json";
        private const string EXTRACT_ENTITY_MACHINE_FILE_NAME = "machine.{0}.json";
        private const string EXTRACT_ENTITY_DOCKER_CONTAINERS_FILE_NAME = "dockercontainer.{0}.json";
        private const string EXTRACT_ENTITY_SERVICE_AVAILABILITIES_FILE_NAME = "serviceavailabilities.json";
        private const string EXTRACT_ENTITY_SERVICE_AVAILABILITY_FILE_NAME = "serviceavailability.{0}.json";

        // SIM process file names
        private const string EXTRACT_PROCESSES_FILE_NAME = "proc.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // DB metadata file names
        private const string EXTRACT_COLLECTOR_DEFINITIONS_FILE_NAME = "collectordefinitions.json";
        private const string EXTRACT_COLLECTORS_CALLS_FILE_NAME = "collectors.calls.json";
        private const string EXTRACT_COLLECTORS_TIME_SPENT_FILE_NAME = "collectors.timespent.json";
        private const string EXTRACT_ALL_WAIT_STATES_FILE_NAME = "waitstatesall.json";
        private const string EXTRACT_DB_CUSTOM_METRICS_FILE_NAME = "custommetrics.json";

        // DB data file names
        private const string EXTRACT_CURRENT_WAIT_STATES_FILE_NAME = "waitstates.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_QUERIES_FILE_NAME = "queries.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_CLIENTS_FILE_NAME = "clients.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_SESSIONS_FILE_NAME = "sessions.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_BLOCKING_SESSIONS_FILE_NAME = "blockedsessions.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_BLOCKED_SESSION_FILE_NAME = "blockedsession.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";
        private const string EXTRACT_DATABASES_FILE_NAME = "databases.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_DB_USERS_FILE_NAME = "users.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_MODULES_FILE_NAME = "modules.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_PROGRAMS_FILE_NAME = "programs.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_BUSINESS_TRANSACTIONS_FILE_NAME = "bts.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // WEB data file names
        private const string EXTRACT_WEB_APPLICATION_KEY = "applicationkeyweb.json";
        private const string EXTRACT_WEB_APPLICATION_MONITORING_STATE = "monitoringstateweb.json";
        private const string EXTRACT_WEB_APPLICATION_INSTRUMENTATION = "agentconfig.json";
        private const string EXTRACT_WEB_PAGE_IFRAME_RULES = "rulespage.json";
        private const string EXTRACT_WEB_AJAX_RULES = "rulesajax.json";
        private const string EXTRACT_WEB_VIRTUAL_PAGE_RULES = "rulesvirtpage.json";
        private const string EXTRACT_WEB_ERROR_RULES = "ruleserror.json";
        private const string EXTRACT_WEB_PAGE_SETTINGS = "pagesettings.json";
        private const string EXTRACT_WEB_PAGES = "webpages.json";
        private const string EXTRACT_WEB_PAGE_PERFORMANCE_FILE_NAME = "page.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";
        private const string EXTRACT_GEO_REGIONS_LIST = "georegions.{0}.json";
        private const string EXTRACT_SYNTHETIC_JOBS = "syntheticjobs.json";

        // MOBILE data file names
        private const string EXTRACT_MOBILE_APPLICATION_KEY = "applicationkeymobile.json";
        private const string EXTRACT_MOBILE_APPLICATION_MONITORING_STATE = "monitoringstatemobile.json";
        private const string EXTRACT_MOBILE_NETWORK_REQUESTS_RULES = "rulesnetworkreq.json";
        private const string EXTRACT_MOBILE_PAGE_SETTINGS = "pagesettings.json";
        private const string EXTRACT_MOBILE_NETWORK_REQUESTS = "networkrequests.json";
        private const string EXTRACT_MOBILE_NETWORK_REQUEST_PERFORMANCE_FILE_NAME = "nr.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // Analytics data file names
        private const string EXTRACT_ANALYTICS_SEARCHES_FILE_NAME = "searches.json";
        private const string EXTRACT_ANALYTICS_METRICS_FILE_NAME = "metrics.json";
        private const string EXTRACT_ANALYTICS_BUSINESS_JOURNEYS_FILE_NAME = "businessjourneys.json";
        private const string EXTRACT_ANALYTICS_EXPERIENCE_LEVELS_FILE_NAME = "experiencelevels.json";
        private const string EXTRACT_ANALYTICS_CUSTOM_SCHEMAS_FILE_NAME = "customschemas.json";
        private const string EXTRACT_ANALYTICS_SCHEMA_FIELDS_FILE_NAME = "fields.{0}.json";

        // RBAC data file names
        private const string EXTRACT_USERS_FILE_NAME = "users.json";
        private const string EXTRACT_GROUPS_FILE_NAME = "groups.json";
        private const string EXTRACT_ROLES_FILE_NAME = "roles.json";
        private const string EXTRACT_USER_FILE_NAME = "user.{0}.json";
        private const string EXTRACT_GROUP_FILE_NAME = "group.{0}.json";
        private const string EXTRACT_GROUP_USERS_FILE_NAME = "usersingroup.{0}.json";
        private const string EXTRACT_ROLE_FILE_NAME = "role.{0}.json";
        private const string EXTRACT_SECURITY_PROVIDER_FILE_NAME = "securityprovider.json";
        private const string EXTRACT_STRONG_PASSWORDS_FILE_NAME = "strongpasswords.json";

        // Metric data file names
        private const string EXTRACT_METRIC_FULL_FILE_NAME = "full.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_METRIC_HOUR_FILE_NAME = "hour.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // Metric list data file names
        private const string EXTRACT_METRIC_LIST = "metr.{0}.{1}.json";

        // Events data file names
        private const string EXTRACT_HEALTH_RULE_VIOLATIONS_FILE_NAME = "healthruleviolations.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_EVENTS_FILE_NAME = "{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";
        private const string EXTRACT_EVENTS_WITH_DETAILS_FILE_NAME = "{0}.details.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";
        private const string EXTRACT_AUDIT_EVENTS_FILE_NAME = "auditevents.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";
        private const string EXTRACT_NOTIFICATIONS_FILE_NAME = "notifications.json";

        // SIM Service Availability events data file names
        private const string EXTRACT_SERVICE_AVAILABILITY_EVENTS_FILE_NAME = "saevents.{0}.{1:yyyyMMddHHmm}-{2:yyyyMMddHHmm}.json";

        // List of Snapshots file names
        private const string EXTRACT_SNAPSHOTS_FILE_NAME = "snapshots.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // Snapshot file names
        private const string EXTRACT_SNAPSHOT_FILE_NAME = "{0}.{1}.{2:yyyyMMddHHmmss}.json";

        // Flowmap file names
        private const string EXTRACT_ENTITY_FLOWMAP_FILE_NAME = "flowmap.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.json";

        // Entity dashboard screenshot file names
        private const string EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME = "dashboard.png";
        private const string EXTRACT_DASHBOARD_SCREENSHOT_THUMB_FILE_NAME = "dashboard_thumb.png";


        #endregion

        #region Constants for the folder and file names of data index

        private const string INDEX_FOLDER_NAME = "Index";

        // Detected Controller entity report conversion file names
        private const string CONVERT_CONTROLLERS_SUMMARY_FILE_NAME = "controllers.csv";
        private const string CONVERT_CONTROLLER_APPLICATIONS_FILE_NAME = "applications.csv";

        // License information
        private const string CONVERT_LICENSE_ACCOUNT_FILE_NAME = "account.csv";
        private const string CONVERT_LICENSES_FILE_NAME = "licenses.csv";
        private const string CONVERT_LICENSE_USAGE_ACCOUNT_FILE_NAME = "licenseusage.account.csv";
        private const string CONVERT_LICENSE_RULES_FILE_NAME = "licenserules.csv";
        private const string CONVERT_LICENSE_RULE_SCOPES_FILE_NAME = "licenserulescopes.csv";
        private const string CONVERT_LICENSE_USAGE_RULES_FILE_NAME = "licenseusage.rules.csv";

        // Detected APM entity report conversion file names
        private const string CONVERT_APM_APPLICATIONS_FILE_NAME = "applications.apm.csv";
        private const string CONVERT_APM_TIERS_FILE_NAME = "tiers.csv";
        private const string CONVERT_APM_NODES_FILE_NAME = "nodes.csv";
        private const string CONVERT_APM_NODE_STARTUP_OPTIONS_FILE_NAME = "nodestartupoptions.csv";
        private const string CONVERT_APM_NODE_PROPERTIES_FILE_NAME = "nodeproperties.csv";
        private const string CONVERT_APM_NODE_ENVIRONMENT_VARIABLES_FILE_NAME = "nodeenvironmentvariables.csv";
        private const string CONVERT_APM_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.csv";
        private const string CONVERT_APM_OVERFLOW_BUSINESS_TRANSACTIONS_FILE_NAME = "businesstransactions.overflow.csv";
        private const string CONVERT_APM_BACKENDS_FILE_NAME = "backends.csv";
        private const string CONVERT_APM_SERVICE_ENDPOINTS_FILE_NAME = "serviceendpoints.csv";
        private const string CONVERT_APM_ERRORS_FILE_NAME = "errors.csv";
        private const string CONVERT_APM_INFORMATION_POINTS_FILE_NAME = "informationpoints.csv";
        private const string CONVERT_APM_MAPPED_BACKENDS_FILE_NAME = "mappedbackends.csv";

        // Detected SIM entity report conversion file names
        private const string CONVERT_SIM_APPLICATIONS_FILE_NAME = "applications.sim.csv";
        private const string CONVERT_SIM_TIERS_FILE_NAME = "simtiers.csv";
        private const string CONVERT_SIM_NODES_FILE_NAME = "simnodes.csv";
        private const string CONVERT_SIM_MACHINES_FILE_NAME = "machines.csv";
        private const string CONVERT_SIM_MACHINE_PROPERTIES_FILE_NAME = "machineproperties.csv";
        private const string CONVERT_SIM_MACHINE_CPUS_FILE_NAME = "machinecpus.csv";
        private const string CONVERT_SIM_MACHINE_VOLUMES_FILE_NAME = "machinevolumes.csv";
        private const string CONVERT_SIM_MACHINE_NETWORKS_FILE_NAME = "machinenetworks.csv";
        private const string CONVERT_SIM_MACHINE_CONTAINERS_FILE_NAME = "machinecontainers.csv";
        private const string CONVERT_SIM_MACHINE_PROCESSES_FILE_NAME = "machineprocesses.csv";

        // Detected DB entity report conversion file names
        private const string CONVERT_DB_APPLICATIONS_FILE_NAME = "applications.db.csv";
        private const string CONVERT_DB_COLLECTORS_FILE_NAME = "collectors.csv";
        private const string CONVERT_DB_WAIT_STATES_FILE_NAME = "waitstates.csv";
        private const string CONVERT_DB_QUERIES_FILE_NAME = "queries.csv";
        private const string CONVERT_DB_CLIENTS_FILE_NAME = "clients.csv";
        private const string CONVERT_DB_SESSIONS_FILE_NAME = "sessions.csv";
        private const string CONVERT_DB_BLOCKING_SESSIONS_FILE_NAME = "blockingsessions.csv";
        private const string CONVERT_DB_DATABASES_FILE_NAME = "databases.csv";
        private const string CONVERT_DB_USERS_FILE_NAME = "users.csv";
        private const string CONVERT_DB_MODULES_FILE_NAME = "modules.csv";
        private const string CONVERT_DB_PROGRAMS_FILE_NAME = "programs.csv";
        private const string CONVERT_DB_BUSINESS_TRANSACTIONS_FILE_NAME = "related.businesstransactions.csv";

        // Detected WEB entity report conversion file names
        private const string CONVERT_WEB_APPLICATIONS_FILE_NAME = "applications.web.csv";
        private const string CONVERT_WEB_PAGES_FILE_NAME = "webpages.csv";
        private const string CONVERT_WEB_PAGE_BUSINESS_TRANSACTIONS_FILE_NAME = "webpages.bts.csv";
        private const string CONVERT_WEB_PAGE_RESOURCES_FILE_NAME = "webpages.resources.csv";
        private const string CONVERT_WEB_GEO_LOCATIONS_FILE_NAME = "geolocations.csv";

        // Detected MOBILE entity report conversion file names
        private const string CONVERT_MOBILE_APPLICATIONS_FILE_NAME = "applications.mobile.csv";
        private const string CONVERT_NETWORK_REQUESTS_FILE_NAME = "networkrequests.csv";
        private const string CONVERT_NETWORK_REQUEST_BUSINESS_TRANSACTIONS_FILE_NAME = "networkrequests.bts.csv";

        // Detected Analytics entity report conversion file names
        private const string CONVERT_ANALYTICS_APPLICATIONS_FILE_NAME = "applications.biq.csv";
        private const string CONVERT_ANALYTICS_SEARCHES_FILE_NAME = "biqsearches.csv";
        private const string CONVERT_ANALYTICS_WIDGETS_FILE_NAME = "biqwidgets.csv";
        private const string CONVERT_ANALYTICS_METRICS_FILE_NAME = "biqmetrics.csv";
        private const string CONVERT_ANALYTICS_BUSINESS_JOURNEYS_FILE_NAME = "businessjourneys.csv";
        private const string CONVERT_ANALYTICS_EXPERIENCE_LEVELS_FILE_NAME = "experiencelevels.csv";
        private const string CONVERT_ANALYTICS_SCHEMAS_FILE_NAME = "biqschemas.csv";
        private const string CONVERT_ANALYTICS_SCHEMA_FIELDS_FILE_NAME = "biqfields.csv";

        // RBAC report conversion file names
        private const string CONVERT_USERS_FILE_NAME = "users.csv";
        private const string CONVERT_GROUPS_FILE_NAME = "groups.csv";
        private const string CONVERT_ROLES_FILE_NAME = "roles.csv";
        private const string CONVERT_PERMISSIONS_FILE_NAME = "permissions.csv";
        private const string CONVERT_GROUP_MEMBERSHIPS_FILE_NAME = "group.memberships.csv";
        private const string CONVERT_ROLE_MEMBERSHIPS_FILE_NAME = "role.memberships.csv";
        private const string CONVERT_USER_PERMISSIONS_FILE_NAME = "user.permissions.csv";
        private const string CONVERT_CONTROLLER_RBAC_SUMMARY_FILE_NAME = "controller.rbac.csv";

        // Controller Settings
        private const string CONVERT_CONTROLLER_SETTINGS_FILE_NAME = "controller.settings.csv";

        // Settings report list conversion file names - APM
        private const string CONVERT_CONFIG_APM_SUMMARY_FILE_NAME = "application.config.apm.csv";
        private const string CONVERT_CONFIG_BUSINESS_TRANSACTION_DISCOVERY_RULES_FILE_NAME = "btdiscovery.rules.csv";
        private const string CONVERT_CONFIG_BUSINESS_TRANSACTION_DISCOVERY_RULES_2_0_FILE_NAME = "btdiscovery.rules.2.0.csv";
        private const string CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_RULES_FILE_NAME = "btentry.rules.csv";
        private const string CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_RULES_2_0_FILE_NAME = "btentry.rules.2.0.csv";
        private const string CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_SCOPES_FILE_NAME = "btentry.scopes.csv";
        private const string CONVERT_CONFIG_SERVICE_ENDPOINT_DISCOVERY_RULES_FILE_NAME = "sep.discovery.csv";
        private const string CONVERT_CONFIG_SERVICE_ENDPOINT_ENTRY_RULES_FILE_NAME = "sep.entry.csv";
        private const string CONVERT_CONFIG_BACKEND_DISCOVERY_RULES_FILE_NAME = "backend.rules.csv";
        private const string CONVERT_CONFIG_CUSTOM_EXIT_RULES_FILE_NAME = "customexit.rules.csv";
        private const string CONVERT_CONFIG_INFORMATION_POINT_RULES_FILE_NAME = "infopoints.csv";
        private const string CONVERT_CONFIG_AGENT_CONFIGURATION_PROPERTIES_FILE_NAME = "agent.properties.csv";
        private const string CONVERT_CONFIG_METHOD_INVOCATION_DATA_COLLECTORS_FILE_NAME = "datacollectors.midc.csv";
        private const string CONVERT_CONFIG_HTTP_DATA_COLLECTORS_FILE_NAME = "datacollectors.http.csv";
        private const string CONVERT_CONFIG_ENTITY_TIERS_FILE_NAME = "tiers.configuration.csv";
        private const string CONVERT_CONFIG_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME = "bts.configuration.csv";
        private const string CONVERT_CONFIG_AGENT_CALL_GRAPH_SETTINGS_FILE_NAME = "callgraphs.configuration.csv";
        private const string CONVERT_CONFIG_DEVELOPER_MODE_NODES_FILE_NAME = "devmode.nodes.csv";
        private const string CONVERT_CONFIG_ERROR_DETECTION_RULES_FILE_NAME = "errordetection.rules.csv";
        private const string CONVERT_CONFIG_ERROR_DETECTION_IGNORE_MESSAGES_FILE_NAME = "errordetection.ignore.messages.csv";
        private const string CONVERT_CONFIG_ERROR_DETECTION_IGNORE_LOGGERS_FILE_NAME = "errordetection.ignore.loggers.csv";
        private const string CONVERT_CONFIG_ERROR_DETECTION_LOGGERS_FILE_NAME = "errordetection.loggers.csv";
        private const string CONVERT_CONFIG_ERROR_DETECTION_HTTP_CODES_FILE_NAME = "errordetection.httpcodes.csv";
        private const string CONVERT_CONFIG_ERROR_DETECTION_REDIRECT_PAGES_FILE_NAME = "errordetection.redirectpages.csv";

        // Settings report list conversion file names - WEB
        private const string CONVERT_CONFIG_WEB_SUMMARY_FILE_NAME = "application.config.web.csv";
        private const string CONVERT_CONFIG_WEB_PAGE_RULES_FILE_NAME = "pagerules.csv";
        private const string CONVERT_CONFIG_SYNTHETIC_JOB_DEFINITIONS_FILE_NAME = "syntheticjobs.csv";

        // Settings report list conversion file names - MOBILE
        private const string CONVERT_CONFIG_MOBILE_SUMMARY_FILE_NAME = "application.config.mobile.csv";
        private const string CONVERT_CONFIG_MOBILE_NETWORK_REQUEST_RULES_FILE_NAME = "networkrules.csv";

        // Settings report list conversion file names - DB
        private const string CONVERT_CONFIG_DB_SUMMARY_FILE_NAME = "application.config.db.csv";
        private const string CONVERT_CONFIG_DB_COLLECTOR_DEFINITIONS_FILE_NAME = "collectordefinitions.csv";
        private const string CONVERT_CONFIG_DB_CUSTOM_METRICS_FILE_NAME = "custommetrics.csv";

        // Health rules and policies and actions conversion file names
        private const string CONVERT_CONFIG_HEALTH_RULES_SUMMARY_FILE_NAME = "application.config.healthrules.csv";
        private const string CONVERT_CONFIG_HEALTH_RULES_FILE_NAME = "healthrules.csv";
        private const string CONVERT_CONFIG_POLICIES_FILE_NAME = "policies.csv";
        private const string CONVERT_CONFIG_ACTIONS_FILE_NAME = "actions.csv";
        private const string CONVERT_CONFIG_POLICY_ACTION_MAPPING_FILE_NAME = "policy.to.action.csv";

        // Alert templates conversion file names
        private const string CONVERT_HTTP_TEMPLATES_FILE_NAME = "templates.http.csv";
        private const string CONVERT_EMAIL_TEMPLATES_FILE_NAME = "templates.email.csv";

        // Dashboards conversion file names
        private const string CONVERT_DASHBOARDS_FILE_NAME = "dashboards.csv";
        private const string CONVERT_DASHBOARD_WIDGETS_FILE_NAME = "dashboard.widgets.csv";
        private const string CONVERT_DASHBOARD_WIDGET_DATA_SERIES_FILE_NAME = "dashboard.dataseries.csv";

        // Configuration comparison report list conversion file names
        private const string CONFIGURATION_DIFFERENCES_FILE_NAME = "configuration.differences.csv";
        private const string CONFIGURATION_DIFFERENCES_WEB_FILE_NAME = "configuration.differences.web.csv";

        // Entity Metrics report conversion file names
        private const string CONVERT_ENTITIES_METRICS_SUMMARY_FULLRANGE_FILE_NAME = "entities.full.csv";
        private const string CONVERT_ENTITIES_METRICS_SUMMARY_HOURLY_FILE_NAME = "entities.hour.csv";
        private const string CONVERT_ENTITIES_ALL_METRICS_SUMMARY_FULLRANGE_FILE_NAME = "{0}.entities.full.csv";
        private const string CONVERT_ENTITIES_ALL_METRICS_SUMMARY_HOURLY_FILE_NAME = "{0}.entities.hour.csv";
        private const string CONVERT_ENTITIES_METRICS_VALUES_FILE_NAME = "{0}.metricvalues.csv";
        private const string CONVERT_ENTITIES_ALL_METRICS_VALUES_FILE_NAME = "{0}.{1}.metricvalues.csv";
        private const string CONVERT_ENTITIES_METRICS_LOCATIONS_FILE_NAME = "metriclocations.csv";

        // Metrics List report conversion file names
        private const string CONVERT_METRICS_LIST = "metricslist.csv";
        private const string CONVERT_METRIC_SUMMARY_LIST = "metricssummary.csv";

        // Events list conversion file names
        private const string CONVERT_APPLICATION_EVENTS_SUMMARY_FILE_NAME = "application.events.csv";
        private const string CONVERT_APPLICATION_EVENTS_FILE_NAME = "events.csv";
        private const string CONVERT_APPLICATION_EVENTS_DETAILS_FILE_NAME = "event.details.csv";
        private const string CONVERT_APPLICATION_HEALTH_RULE_EVENTS_FILE_NAME = "hrviolationevents.csv";
        private const string CONVERT_CONTROLLER_AUDIT_EVENTS_FILE_NAME = "auditevents.csv";
        private const string CONVERT_CONTROLLER_NOTIFICATIONS_FILE_NAME = "notifications.csv";

        // Snapshots files
        private const string CONVERT_APPLICATION_SNAPSHOTS_SUMMARY_FILE_NAME = "application.snapshots.csv";
        private const string CONVERT_SNAPSHOTS_FILE_NAME = "snapshots.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME = "snapshots.segments.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME = "snapshots.exits.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME = "snapshots.serviceendpoints.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME = "snapshots.errors.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME = "snapshots.businessdata.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME = "snapshots.methodcalllines.csv";

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

        // Folded call stacks rollups for each BT and Nodes
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_TIMERANGE_FILE_NAME = "snapshots.foldedcallstacks.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";
        private const string CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_TIMERANGE_FILE_NAME = "snapshots.foldedcallstackswithtime.{0:yyyyMMddHHmm}-{1:yyyyMMddHHmm}.csv";

        // Flow map to flow grid conversion file names
        private const string CONVERT_ACTIVITY_GRIDS_FILE_NAME = "activitygrids.full.csv";
        private const string CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME = "{0}.activitygrids.full.csv";
        private const string CONVERT_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME = "activitygrids.perminute.full.csv";
        private const string CONVERT_ALL_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME = "{0}.activitygrids.perminute.full.csv";

        // Health check conversion file names
        private const string CONVERT_APM_HEALTH_CHECK_RULE_RESULTS_FILE_NAME = "healthcheck.APM.results.csv";
        private const string CONVERT_CONTROLLER_HEALTH_CHECK_RULE_RESULTS_FILE_NAME = "healthcheck.PLAT.results.csv";

        #endregion

        #region Constants for the folder and file names of data reports

        private const string REPORT_FOLDER_NAME = "Report";

        // Report file names
        private const string REPORT_DETECTED_APM_ENTITIES_FILE_NAME = "Entities.APM.{0}.xlsx";
        private const string REPORT_DETECTED_SIM_ENTITIES_FILE_NAME = "Entities.SIM.{0}.xlsx";
        private const string REPORT_DETECTED_DB_ENTITIES_FILE_NAME = "Entities.DB.{0}.xlsx";
        private const string REPORT_DETECTED_WEB_ENTITIES_FILE_NAME = "Entities.WEB.{0}.xlsx";
        private const string REPORT_DETECTED_MOBILE_ENTITIES_FILE_NAME = "Entities.MOBILE.{0}.xlsx";
        private const string REPORT_DETECTED_ANALYTICS_ENTITIES_FILE_NAME = "Entities.BIQ.{0}.xlsx";

        private const string REPORT_METRICS_ALL_ENTITIES_FILE_NAME = "EntityMetrics.{0}.xlsx";
        private const string REPORT_FLOWMAPS_ALL_ENTITIES_FILE_NAME = "Flowmaps.{0}.xlsx";
        private const string REPORT_DETECTED_EVENTS_FILE_NAME = "Events.{0}.xlsx";
        private const string REPORT_SNAPSHOTS_FILE_NAME = "Snapshots.{0}.xlsx";
        private const string REPORT_SNAPSHOTS_METHOD_CALL_LINES_FILE_NAME = "CallGraphs.{0}.xlsx";
        private const string REPORT_CONFIGURATION_FILE_NAME = "Configuration.{0}.xlsx";
        private const string REPORT_USERS_GROUPS_ROLES_PERMISSIONS_FILE_NAME = "UsersGroupsRoles.{0}.xlsx";
        private const string REPORT_DASHBOARDS_FILE_NAME = "Dashboards.{0}.xlsx";
        private const string REPORT_LICENSES_FILE_NAME = "Licenses.{0}.xlsx";
        private const string REPORT_APPLICATIONS_DASHBOARDS_FILE_NAME = "ApplicationsDashboards.{0}.html";
        private const string REPORT_APPLICATION_DASHBOARDS_FILE_NAME = "ApplicationDashboards.html";
        private const string REPORT_HEALTH_CHECK_RESULTS_FILE_NAME = "HealthCheck.{0}.xlsx";
        private const string REPORT_SNAPSHOT_FILE_NAME = "Snapshot.{0}-{1}-{2}-{3}-{4}.xlsx";

        // Per entity report names
        private const string REPORT_ENTITY_DETAILS_APPLICATION_FILE_NAME = "EntityDetails.{0}.{1}.xlsx";
        private const string REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME = "EntityDetails.{0}.{1}.{2}.xlsx";
        private const string REPORT_METRICS_GRAPHS_FILE_NAME = "MetricGraphs.{0}.{1}.{2}.xlsx";

        // Per entity flame graph report name
        private const string REPORT_FLAME_GRAPH_APPLICATION_FILE_NAME = "FlameGraph.Application.{0}.{1}.svg";
        private const string REPORT_FLAME_GRAPH_TIER_FILE_NAME = "FlameGraph.Tier.{0}.{1}.{2}.svg";
        private const string REPORT_FLAME_GRAPH_NODE_FILE_NAME = "FlameGraph.Node.{0}.{1}.{2}.svg";
        private const string REPORT_FLAME_GRAPH_BUSINESS_TRANSACTION_FILE_NAME = "FlameGraph.BT.{0}.{1}.{2}.svg";
        private const string REPORT_APPLICATION_FLAMEGRAPHS_FILE_NAME = "ApplicationFlamegraphs.html";

        // Per entity flame chart report name
        private const string REPORT_FLAME_CHART_APPLICATION_FILE_NAME = "FlameChart.Application.{0}.{1}.svg";
        private const string REPORT_FLAME_CHART_TIER_FILE_NAME = "FlameChart.Tier.{0}.{1}.{2}.svg";
        private const string REPORT_FLAME_CHART_NODE_FILE_NAME = "FlameChart.Node.{0}.{1}.{2}.svg";
        private const string REPORT_FLAME_CHART_BUSINESS_TRANSACTION_FILE_NAME = "FlameChart.BT.{0}.{1}.{2}.svg";

        // Per Application summary
        private const string REPORT_APPLICATION_SUMMARY_FILE_NAME = "ApplicationSummary.{0}.{1}.docx";

        // Metrics List report names
        private const string REPORT_METRICS_LIST_ALL_FILE_NAME = "MetricsList.{0}.xlsx";
        private const string REPORT_METRICS_LIST_APPLICATION_FILE_NAME = "MetricsList.{0}.{1}.xlsx";

        #endregion

        #region Constants for Step Timing report

        private const string TIMING_REPORT_FILE_NAME = "StepDurations.csv";

        #endregion

        #region Constants for lookup and external files

        // Settings for method and call mapping
        private const string METHOD_CALL_LINES_TO_FRAMEWORK_TYPE_MAPPING_FILE_NAME = "MethodNamespaceTypeMapping.csv";

        // Settings for the metric extracts
        private const string ENTITY_METRICS_EXTRACT_MAPPING_FILE_NAME = "EntityMetricsExtractMapping.csv";

        // Settings for the metric extracts
        private const string HEALTH_CHECK_SETTING_MAPPING_FILE_NAME = "HealthCheckSettingMapping.csv";

        // Flame graph template SVG XML file
        private const string FLAME_GRAPH_TEMPLATE_FILE_NAME = "FlameGraphTemplate.svg";
        // Application Flamegraphs template
        private const string LINKS_TO_APPLICATION_FLAMEGRAPHSS_TEMPLATE_FILE_NAME = "LinksToApplicationFlameGraphsTemplate.html";

        // Template controller export of an empty applications
        private const string TEMPLATE_CONTROLLER_AND_APPLICATIONS_FOLDER_NAME = "EmptyConfig";

        // Application Dashboard screenshots templates
        private const string LINKS_TO_APPLICATION_DASHBOARDS_TEMPLATE_FILE_NAME = "LinksToApplicationDashboardsTemplate.html";
        private const string LINKS_TO_APPLICATIONS_TEMPLATE_FILE_NAME = "LinksToApplicationsDashboardsTemplate.html";


        #endregion

        #region Snapshot UX to Folder Mapping

        private static Dictionary<string, string> USEREXPERIENCE_FOLDER_MAPPING = new Dictionary<string, string>
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

        public string ReportFolderName { get; set; }

        public FilePathMap(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            this.ProgramOptions = programOptions;
            this.JobConfiguration = jobConfiguration;
            this.ReportFolderName = REPORT_FOLDER_NAME;
            if (programOptions.IndividualSnapshotsNonDefaultReportFolderName != null && programOptions.IndividualSnapshotsNonDefaultReportFolderName.Length > 0)
            {
                this.ReportFolderName = programOptions.IndividualSnapshotsNonDefaultReportFolderName;
            }
        }

        #endregion


        #region Step Timing Report

        public string StepTimingReportFilePath()
        {
            return Path.Combine(this.ProgramOptions.OutputJobFolderPath, this.ReportFolderName, TIMING_REPORT_FILE_NAME);
        }

        #endregion


        #region APM Entity Data

        public string APMApplicationDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_APPLICATION_FILE_NAME);
        }

        public string APMTiersDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_TIERS_FILE_NAME);
        }

        public string APMTierOverflowBusinessTransactionRegularDataFilePath(JobTarget jobTarget, AppDRESTTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.name, tier.id),
                EXTRACT_ENTITY_TIER_BUSINESS_TRANSACTION_OVERFLOW_REGULAR_FILE_NAME);
        }

        public string APMTierOverflowBusinessTransactionDebugDataFilePath(JobTarget jobTarget, AppDRESTTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.name, tier.id),
                EXTRACT_ENTITY_TIER_BUSINESS_TRANSACTION_OVERFLOW_DEBUG_FILE_NAME);
        }

        public string APMNodesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_NODES_FILE_NAME);
        }

        public string APMBackendsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_BACKENDS_FILE_NAME);
        }

        public string APMBackendsDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_BACKENDS_DETAIL_FILE_NAME);
        }

        public string APMBackendToTierMappingDataFilePath(JobTarget jobTarget, AppDRESTTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.name, tier.id),
                EXTRACT_ENTITY_BACKEND_TO_TIER_MAPPING_FILE_NAME);
        }

        public string APMBackendToDBMonMappingDataFilePath(JobTarget jobTarget, AppDRESTBackend backend)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_BACKEND_TO_DBMON_MAPPING_FILE_NAME,
                getShortenedEntityNameForFileSystem(backend.name, backend.id));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        public string APMBusinessTransactionsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMServiceEndpointsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_SERVICE_ENDPOINTS_FILE_NAME);
        }

        public string APMServiceEndpointsDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_SERVICE_ENDPOINTS_DETAIL_FILE_NAME);
        }

        public string APMErrorsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_ERRORS_FILE_NAME);
        }

        public string APMInformationPointsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_INFORMATION_POINTS_FILE_NAME);
        }

        public string APMInformationPointsDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_INFORMATION_POINTS_DETAIL_FILE_NAME);
        }

        public string APMNodeRuntimePropertiesDataFilePath(JobTarget jobTarget, AppDRESTNode node)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_NODE_RUNTIME_PROPERTIES_FILE_NAME,
                getShortenedEntityNameForFileSystem(node.name, node.id));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                reportFileName);
        }

        public string APMNodeMetadataDataFilePath(JobTarget jobTarget, AppDRESTNode node)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_NODE_METADATA_FILE_NAME,
                getShortenedEntityNameForFileSystem(node.name, node.id));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                reportFileName);
        }

        #endregion

        #region APM Entity Index

        public string APMApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_APPLICATIONS_FILE_NAME);
        }

        public string APMTiersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_TIERS_FILE_NAME);
        }

        public string APMNodesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_NODES_FILE_NAME);
        }

        public string APMNodeStartupOptionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_NODE_STARTUP_OPTIONS_FILE_NAME);
        }

        public string APMNodePropertiesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_NODE_PROPERTIES_FILE_NAME);
        }

        public string APMNodeEnvironmentVariablesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_NODE_ENVIRONMENT_VARIABLES_FILE_NAME);
        }

        public string APMBackendsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_BACKENDS_FILE_NAME);
        }

        public string APMMappedBackendsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_MAPPED_BACKENDS_FILE_NAME);
        }

        public string APMBusinessTransactionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMOverflowBusinessTransactionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_OVERFLOW_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMServiceEndpointsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_SERVICE_ENDPOINTS_FILE_NAME);
        }

        public string APMErrorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_ERRORS_FILE_NAME);
        }

        public string APMInformationPointsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_APM_INFORMATION_POINTS_FILE_NAME);
        }

        #endregion

        #region APM Entity Report

        public string ReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName);
        }

        public string APMEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME);
        }

        public string APMApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_APPLICATIONS_FILE_NAME);
        }

        public string APMTiersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_TIERS_FILE_NAME);
        }

        public string APMNodesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_NODES_FILE_NAME);
        }

        public string APMNodeStartupOptionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_NODE_STARTUP_OPTIONS_FILE_NAME);
        }

        public string APMNodePropertiesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_NODE_PROPERTIES_FILE_NAME);
        }

        public string APMNodeEnvironmentVariablesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_NODE_ENVIRONMENT_VARIABLES_FILE_NAME);
        }

        public string APMBackendsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_BACKENDS_FILE_NAME);
        }

        public string APMMappedBackendsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_MAPPED_BACKENDS_FILE_NAME);
        }

        public string APMBusinessTransactionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMOverflowBusinessTransactionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_OVERFLOW_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMServiceEndpointsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_SERVICE_ENDPOINTS_FILE_NAME);
        }

        public string APMErrorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_ERRORS_FILE_NAME);
        }

        public string APMInformationPointsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_APM_FOLDER_NAME,
                CONVERT_APM_INFORMATION_POINTS_FILE_NAME);
        }

        public string APMEntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_APM_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region WEB Application Configuration Data

        public string WEBApplicationKeyDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_APPLICATION_KEY);
        }

        public string WEBAgentConfigDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_APPLICATION_INSTRUMENTATION);
        }

        public string WEBApplicationMonitoringStateDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_APPLICATION_MONITORING_STATE);
        }

        public string WEBAgentPageRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_PAGE_IFRAME_RULES);
        }

        public string WEBAgentAjaxRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_AJAX_RULES);
        }

        public string WEBAgentVirtualPageRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_VIRTUAL_PAGE_RULES);
        }

        public string WEBAgentErrorRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_ERROR_RULES);
        }

        public string WEBAgentPageSettingsRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_WEB_PAGE_SETTINGS);
        }

        public string WEBSyntheticJobsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_SYNTHETIC_JOBS);
        }

        #endregion

        #region WEB Application Configuration Index

        public string WEBApplicationConfigurationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_WEB_SUMMARY_FILE_NAME);
        }

        public string WEBPageAjaxVirtualPageRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_WEB_PAGE_RULES_FILE_NAME);
        }

        public string WEBSyntheticJobsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_SYNTHETIC_JOB_DEFINITIONS_FILE_NAME);
        }

        #endregion

        #region WEB Application Configuration Report

        public string WEBConfigurationReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_WEB_FOLDER_NAME);
        }

        public string WEBApplicationConfigurationReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_WEB_FOLDER_NAME,
                CONVERT_CONFIG_WEB_SUMMARY_FILE_NAME);
        }

        public string WEBPageAjaxVirtualPageRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_WEB_FOLDER_NAME,
                CONVERT_CONFIG_WEB_PAGE_RULES_FILE_NAME);
        }

        public string WEBSyntheticJobsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_WEB_FOLDER_NAME,
                CONVERT_CONFIG_SYNTHETIC_JOB_DEFINITIONS_FILE_NAME);
        }

        #endregion


        #region Web Application Entity Data

        public string WEBPagesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_WEB_PAGES);
        }

        public string WEBPagePerformanceDataFilePath(JobTarget jobTarget, string pageType, string pageName, long pageID, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_WEB_PAGE_PERFORMANCE_FILE_NAME,
                getShortenedEntityNameForFileSystem(pageName, pageID),
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                pageType,
                reportFileName);
        }

        public string WEBGeoLocationsDataFilePath(JobTarget jobTarget, string country)
        {
            string reportFileName = String.Format(EXTRACT_GEO_REGIONS_LIST,
                getFileSystemSafeString(country));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region Web Application Entity Index

        public string WEBApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_WEB_APPLICATIONS_FILE_NAME);
        }

        public string WEBPagesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_WEB_PAGES_FILE_NAME);
        }

        public string WEBPageBusinessTransactionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_WEB_PAGE_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string WEBPageResourcesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_WEB_PAGE_RESOURCES_FILE_NAME);
        }

        public string WEBGeoLocationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_WEB_GEO_LOCATIONS_FILE_NAME);
        }

        #endregion

        #region Web Application Entity Report

        public string WEBEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_WEB_FOLDER_NAME);
        }

        public string WEBApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_WEB_FOLDER_NAME,
                CONVERT_WEB_APPLICATIONS_FILE_NAME);
        }

        public string WEBPagesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_WEB_FOLDER_NAME,
                CONVERT_WEB_PAGES_FILE_NAME);
        }

        public string WEBPageBusinessTransactionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_WEB_FOLDER_NAME,
                CONVERT_WEB_PAGE_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string WEBPageResourcesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_WEB_FOLDER_NAME,
                CONVERT_WEB_PAGE_RESOURCES_FILE_NAME);
        }

        public string WEBGeoLocationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_WEB_FOLDER_NAME,
                CONVERT_WEB_GEO_LOCATIONS_FILE_NAME);
        }

        public string WEBEntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_WEB_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region MOBILE Application Configuration Data

        public string MOBILEApplicationKeyDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_MOBILE_APPLICATION_KEY);
        }

        public string MOBILEApplicationMonitoringStateDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_MOBILE_APPLICATION_MONITORING_STATE);
        }

        public string MOBILEAgentNetworkRequestsRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_MOBILE_NETWORK_REQUESTS_RULES);
        }

        public string MOBILEAgentPageSettingsRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_MOBILE_PAGE_SETTINGS);
        }

        #endregion

        #region MOBILE Application Configuration Index

        public string MOBILEApplicationConfigurationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_MOBILE_SUMMARY_FILE_NAME);
        }

        public string MOBILENetworkRequestRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_MOBILE_NETWORK_REQUEST_RULES_FILE_NAME);
        }

        #endregion

        #region MOBILE Application Configuration Report

        public string MOBILEConfigurationReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_MOBILE_FOLDER_NAME);
        }

        public string MOBILEApplicationConfigurationReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_MOBILE_FOLDER_NAME,
                CONVERT_CONFIG_MOBILE_SUMMARY_FILE_NAME);
        }

        public string MOBILENetworkRequestRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_MOBILE_FOLDER_NAME,
                CONVERT_CONFIG_MOBILE_NETWORK_REQUEST_RULES_FILE_NAME);
        }

        #endregion


        #region MOBILE Application Entity Data

        public string MOBILENetworkRequestsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_MOBILE_NETWORK_REQUESTS);
        }

        public string MOBILENetworkRequestPerformanceDataFilePath(JobTarget jobTarget, string networkRequestName, long networkRequestID, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_MOBILE_NETWORK_REQUEST_PERFORMANCE_FILE_NAME,
                getShortenedEntityNameForFileSystem(networkRequestName, networkRequestID),
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region MOBILE Application Entity Index

        public string MOBILEApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_MOBILE_APPLICATIONS_FILE_NAME);
        }

        public string MOBILENetworkRequestsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_NETWORK_REQUESTS_FILE_NAME);
        }

        public string MOBILENetworkRequestsBusinessTransactionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_NETWORK_REQUEST_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        #endregion

        #region MOBILE Application Entity Report

        public string MOBILEEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_MOBILE_FOLDER_NAME);
        }

        public string MOBILEApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_MOBILE_FOLDER_NAME,
                CONVERT_MOBILE_APPLICATIONS_FILE_NAME);
        }

        public string MOBILENetworkRequestsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_MOBILE_FOLDER_NAME,
                CONVERT_NETWORK_REQUESTS_FILE_NAME);
        }

        public string MOBILENetworkRequestsBusinessTransactionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_MOBILE_FOLDER_NAME,
                CONVERT_NETWORK_REQUEST_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string MOBILEEntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_MOBILE_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region SIM Entity Data

        public string SIMTiersDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_TIERS_FILE_NAME);
        }

        public string SIMNodesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_NODES_FILE_NAME);
        }

        public string SIMGroupsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_GROUPS_FILE_NAME);
        }

        public string SIMMachinesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        public string SIMServiceAvailabilitiesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_PROCESSES_FOLDER_NAME,
                reportFileName);
        }

        public string SIMServiceAvailabilityEventsDataFilePath(JobTarget jobTarget, string saName, long saID, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_SERVICE_AVAILABILITY_EVENTS_FILE_NAME,
                getShortenedEntityNameForFileSystem(saName, saID),
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                EVENTS_SA_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region SIM Entity Index

        public string SIMApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_APPLICATIONS_FILE_NAME);
        }

        public string SIMTiersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_TIERS_FILE_NAME);
        }

        public string SIMNodesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_NODES_FILE_NAME);
        }

        public string SIMMachinesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_MACHINES_FILE_NAME);
        }

        public string SIMMachinePropertiesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_MACHINE_PROPERTIES_FILE_NAME);
        }

        public string SIMMachineCPUsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_MACHINE_CPUS_FILE_NAME);
        }

        public string SIMMachineVolumesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_MACHINE_VOLUMES_FILE_NAME);
        }

        public string SIMMachineNetworksIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_MACHINE_NETWORKS_FILE_NAME);
        }

        public string SIMMachineContainersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_SIM_MACHINE_CONTAINERS_FILE_NAME);
        }

        public string SIMMachineProcessesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                SIM_PROCESSES_FOLDER_NAME,
                CONVERT_SIM_MACHINE_PROCESSES_FILE_NAME);
        }

        #endregion

        #region SIM Entity Report

        public string SIMEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME);
        }

        public string SIMApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_APPLICATIONS_FILE_NAME);
        }

        public string SIMTiersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_TIERS_FILE_NAME);
        }

        public string SIMNodesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_NODES_FILE_NAME);
        }

        public string SIMMachinesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINES_FILE_NAME);
        }

        public string SIMMachinePropertiesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINE_PROPERTIES_FILE_NAME);
        }

        public string SIMMachineCPUsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINE_CPUS_FILE_NAME);
        }

        public string SIMMachineVolumesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINE_VOLUMES_FILE_NAME);
        }

        public string SIMMachineNetworksReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINE_NETWORKS_FILE_NAME);
        }

        public string SIMMachineContainersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINE_CONTAINERS_FILE_NAME);
        }

        public string SIMMachineProcessesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_SIM_FOLDER_NAME,
                CONVERT_SIM_MACHINE_PROCESSES_FILE_NAME);
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
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region DB Application Configuration Data

        public string DBCollectorDefinitionsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_COLLECTOR_DEFINITIONS_FILE_NAME);
        }

        public string DBCustomMetricsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_DB_CUSTOM_METRICS_FILE_NAME);
        }

        #endregion

        #region DB Application Configuration Index

        public string DBApplicationConfigurationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_DB_SUMMARY_FILE_NAME);
        }

        public string DBCollectorDefinitionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_DB_COLLECTOR_DEFINITIONS_FILE_NAME);
        }

        public string DBCustomMetricsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_DB_CUSTOM_METRICS_FILE_NAME);
        }

        #endregion

        #region DB Application Configuration Report

        public string DBConfigurationReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_DB_FOLDER_NAME);
        }

        public string DBApplicationConfigurationReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_DB_FOLDER_NAME,
                CONVERT_CONFIG_DB_SUMMARY_FILE_NAME);
        }

        public string DBCollectorDefinitionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_DB_FOLDER_NAME,
                CONVERT_CONFIG_DB_COLLECTOR_DEFINITIONS_FILE_NAME);
        }

        public string DBCustomMetricsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_DB_FOLDER_NAME,
                CONVERT_CONFIG_DB_CUSTOM_METRICS_FILE_NAME);
        }

        #endregion


        #region DB Entity Data

        public string DBCollectorDefinitionsForEntitiesFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_COLLECTOR_DEFINITIONS_FILE_NAME);
        }

        public string DBCollectorsCallsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_COLLECTORS_CALLS_FILE_NAME);
        }

        public string DBCollectorsTimeSpentDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_COLLECTORS_TIME_SPENT_FILE_NAME);
        }

        public string DBAllWaitStatesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ALL_WAIT_STATES_FILE_NAME);
        }

        public string DBCurrentWaitStatesDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_CURRENT_WAIT_STATES_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBQueriesDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_QUERIES_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBClientsDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_CLIENTS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBSessionsDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_SESSIONS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBBlockingSessionsDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_BLOCKING_SESSIONS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBBlockingSessionDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, long sessionID)
        {
            string reportFileName = String.Format(
                EXTRACT_BLOCKED_SESSION_FILE_NAME,
                sessionID,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBDatabasesDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_DATABASES_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBUsersDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_DB_USERS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBModulesDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_MODULES_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBProgramsDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_PROGRAMS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        public string DBBusinessTransactionsDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_BUSINESS_TRANSACTIONS_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                DB_DATA_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region DB Entity Index

        public string DBCollectorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_COLLECTORS_FILE_NAME);
        }

        public string DBApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_APPLICATIONS_FILE_NAME);
        }

        public string DBWaitStatesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_WAIT_STATES_FILE_NAME);
        }

        public string DBQueriesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_QUERIES_FILE_NAME);
        }

        public string DBClientsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_CLIENTS_FILE_NAME);
        }

        public string DBSessionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_SESSIONS_FILE_NAME);
        }

        public string DBBlockingSessionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_BLOCKING_SESSIONS_FILE_NAME);
        }

        public string DBDatabasesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_DATABASES_FILE_NAME);
        }

        public string DBUsersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_USERS_FILE_NAME);
        }

        public string DBModulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_MODULES_FILE_NAME);
        }

        public string DBProgramsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_PROGRAMS_FILE_NAME);
        }

        public string DBBusinessTransactionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                ENTITIES_FOLDER_NAME,
                CONVERT_DB_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        #endregion

        #region DB Entity Report

        public string DBEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME);
        }

        public string DBCollectorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_COLLECTORS_FILE_NAME);
        }

        public string DBApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_APPLICATIONS_FILE_NAME);
        }

        public string DBWaitStatesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_WAIT_STATES_FILE_NAME);
        }

        public string DBQueriesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_QUERIES_FILE_NAME);
        }

        public string DBClientsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_CLIENTS_FILE_NAME);
        }

        public string DBSessionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_SESSIONS_FILE_NAME);
        }

        public string DBBlockingSessionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_BLOCKING_SESSIONS_FILE_NAME);
        }

        public string DBDatabasesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_DATABASES_FILE_NAME);
        }

        public string DBUsersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_USERS_FILE_NAME);
        }

        public string DBModulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_MODULES_FILE_NAME);
        }

        public string DBProgramsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_PROGRAMS_FILE_NAME);
        }

        public string DBBusinessTransactionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_DB_FOLDER_NAME,
                CONVERT_DB_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string DBEntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_DB_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region Analytics Entity Data

        public string BIQSearchesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ANALYTICS_SEARCHES_FILE_NAME);
        }

        public string BIQMetricsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ANALYTICS_METRICS_FILE_NAME);
        }

        public string BIQBusinessJourneysDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ANALYTICS_BUSINESS_JOURNEYS_FILE_NAME);
        }

        public string BIQExperienceLevelsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ANALYTICS_EXPERIENCE_LEVELS_FILE_NAME);
        }

        public string BIQCustomSchemasDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ANALYTICS_CUSTOM_SCHEMAS_FILE_NAME);
        }

        public string BIQSchemaFieldsDataFilePath(JobTarget jobTarget, string schemaName)
        {
            string reportFileName = String.Format(
                EXTRACT_ANALYTICS_SCHEMA_FIELDS_FILE_NAME,
                schemaName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region Analytics Entity Index

        public string BIQApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_APPLICATIONS_FILE_NAME);
        }

        public string BIQSearchesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_SEARCHES_FILE_NAME);
        }

        public string BIQWidgetsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_WIDGETS_FILE_NAME);
        }

        public string BIQMetricsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_METRICS_FILE_NAME);
        }

        public string BIQBusinessJourneysIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_BUSINESS_JOURNEYS_FILE_NAME);
        }

        public string BIQExperienceLevelsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_EXPERIENCE_LEVELS_FILE_NAME);
        }

        public string BIQSchemasIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_SCHEMAS_FILE_NAME);
        }

        public string BIQSchemaFieldsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                ENTITIES_FOLDER_NAME,
                CONVERT_ANALYTICS_SCHEMA_FIELDS_FILE_NAME);
        }

        #endregion

        #region Analytics Entity Report

        public string BIQEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME);
        }

        public string BIQApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_APPLICATIONS_FILE_NAME);
        }

        public string BIQSearchesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_SEARCHES_FILE_NAME);
        }

        public string BIQWidgetsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_WIDGETS_FILE_NAME);
        }

        public string BIQMetricsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_METRICS_FILE_NAME);
        }

        public string BIQBusinessJourneysReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_BUSINESS_JOURNEYS_FILE_NAME);
        }

        public string BIQExperienceLevelsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_EXPERIENCE_LEVELS_FILE_NAME);
        }

        public string BIQSchemasReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_SCHEMAS_FILE_NAME);
        }

        public string BIQSchemaFieldsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_BIQ_FOLDER_NAME,
                CONVERT_ANALYTICS_SCHEMA_FIELDS_FILE_NAME);
        }

        public string BIQEntitiesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DETECTED_ANALYTICS_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region Controller Wide Applications, Templates and Dashboards Data

        public string ControllerVersionDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONTROLLER_VERSION_FILE_NAME);
        }

        public string ControllerSettingsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONTROLLER_SETTINGS_FILE_NAME);
        }

        public string AllApplicationsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_ALL_APPLICATIONS_FILE_NAME);
        }

        public string MOBILEApplicationsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_MOBILE_APPLICATIONS_FILE_NAME);
        }

        public string APMApplicationsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                ENTITIES_FOLDER_NAME,
                EXTRACT_ENTITY_APM_APPLICATIONS_FILE_NAME);
        }

        public string HTTPTemplatesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_HTTP_TEMPLATES_FILE_NAME);
        }

        public string HTTPTemplatesDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_HTTP_TEMPLATES_DETAIL_FILE_NAME);
        }

        public string EmailTemplatesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_EMAIL_TEMPLATES_FILE_NAME);
        }

        public string EmailTemplatesDetailDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_EMAIL_TEMPLATES_DETAIL_FILE_NAME);
        }

        public string ControllerDashboards(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                DASHBOARDS_FOLDER_NAME,
                EXTRACT_CONTROLLER_DASHBOARDS);
        }

        public string ControllerDashboard(JobTarget jobTarget, string dashboardName, long dashboardID)
        {
            string reportFileName = String.Format(
                EXTRACT_CONTROLLER_DASHBOARD,
                getShortenedEntityNameForFileSystem(dashboardName, dashboardID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                DASHBOARDS_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region Controller Wide Applications, Templates and Dashboards  Index

        public string ControllerSummaryIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                ENTITIES_FOLDER_NAME,
                CONVERT_CONTROLLERS_SUMMARY_FILE_NAME);
        }

        public string ControllerApplicationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                ENTITIES_FOLDER_NAME,
                CONVERT_CONTROLLER_APPLICATIONS_FILE_NAME);
        }

        public string ControllerSettingsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONTROLLER_SETTINGS_FILE_NAME);
        }

        public string HTTPTemplatesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_HTTP_TEMPLATES_FILE_NAME);
        }

        public string EmailTemplatesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_EMAIL_TEMPLATES_FILE_NAME);
        }

        public string DashboardsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                DASHBOARDS_FOLDER_NAME,
                CONVERT_DASHBOARDS_FILE_NAME);
        }

        public string DashboardWidgetsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                DASHBOARDS_FOLDER_NAME,
                CONVERT_DASHBOARD_WIDGETS_FILE_NAME);
        }

        public string DashboardMetricSeriesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                DASHBOARDS_FOLDER_NAME,
                CONVERT_DASHBOARD_WIDGET_DATA_SERIES_FILE_NAME);
        }

        #endregion

        #region Controller Wide Applications, Templates and Dashboards  Report

        public string ControllerEntitiesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_FOLDER_NAME);
        }

        public string ControllerSettingsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_FOLDER_NAME);
        }

        public string ControllerDashboardsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                DASHBOARDS_FOLDER_NAME);
        }

        public string ControllerSummaryReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_FOLDER_NAME,
                CONVERT_CONTROLLERS_SUMMARY_FILE_NAME);
        }

        public string ControllerApplicationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                ENTITIES_FOLDER_NAME,
                CONVERT_CONTROLLER_APPLICATIONS_FILE_NAME);
        }

        public string ControllerSettingsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONTROLLER_SETTINGS_FILE_NAME);
        }

        public string HTTPTemplatesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_FOLDER_NAME,
                CONVERT_HTTP_TEMPLATES_FILE_NAME);
        }

        public string EmailTemplatesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_FOLDER_NAME,
                CONVERT_EMAIL_TEMPLATES_FILE_NAME);
        }

        public string DashboardsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                DASHBOARDS_FOLDER_NAME,
                CONVERT_DASHBOARDS_FILE_NAME);
        }

        public string DashboardWidgetsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                DASHBOARDS_FOLDER_NAME,
                CONVERT_DASHBOARD_WIDGETS_FILE_NAME);
        }

        public string DashboardMetricSeriesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                DASHBOARDS_FOLDER_NAME,
                CONVERT_DASHBOARD_WIDGET_DATA_SERIES_FILE_NAME);
        }

        public string DashboardsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_DASHBOARDS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region License Information Data

        public string LicenseApplicationsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                EXTRACT_LICENSE_ALL_APPLICATIONS_FILE_NAME);
        }

        public string LicenseSIMMachinesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                EXTRACT_LICENSE_SIM_MACHINES_FILE_NAME);
        }

        public string LicenseAccountDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                EXTRACT_LICENSE_ACCOUNT_FILE_NAME);
        }

        public string LicenseModulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                EXTRACT_LICENSE_MODULES_FILE_NAME);
        }

        public string LicenseModulePropertiesDataFilePath(JobTarget jobTarget, string licenseModuleName)
        {
            string reportFileName = String.Format(
                EXTRACT_LICENSE_MODULE_PROPERTIES_FILE_NAME,
                licenseModuleName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                reportFileName);
        }

        public string LicenseModuleUsagesDataFilePath(JobTarget jobTarget, string licenseModuleName)
        {
            string reportFileName = String.Format(
                EXTRACT_LICENSE_MODULE_USAGES_FILE_NAME,
                licenseModuleName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                reportFileName);
        }

        public string LicenseRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                EXTRACT_LICENSE_RULES_FILE_NAME);
        }

        public string LicenseRuleUsageDataFilePath(JobTarget jobTarget, string ruleName, string ruleID)
        {
            string reportFileName = String.Format(
                EXTRACT_LICENSE_RULE_USAGE_DETAIL_FILE_NAME,
                getShortenedEntityNameForFileSystem(ruleName, ruleID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                reportFileName);
        }

        public string LicenseRuleConfigurationDataFilePath(JobTarget jobTarget, string ruleName, string ruleID)
        {
            string reportFileName = String.Format(
                EXTRACT_LICENSE_RULE_CONFIGURATION_DETAIL_FILE_NAME,
                getShortenedEntityNameForFileSystem(ruleName, ruleID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region License Information Index

        public string LicenseAccountIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_ACCOUNT_FILE_NAME);
        }

        public string LicensesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSES_FILE_NAME);
        }

        public string LicenseRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_RULES_FILE_NAME);
        }

        public string LicenseRuleScopesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_RULE_SCOPES_FILE_NAME);
        }

        public string LicenseUsageAccountIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_USAGE_ACCOUNT_FILE_NAME);
        }

        public string LicenseUsageRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_USAGE_RULES_FILE_NAME);
        }

        #endregion

        #region License Information Report

        public string ControllerLicensesReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME);
        }

        public string LicenseAccountReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_ACCOUNT_FILE_NAME);
        }

        public string LicensesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSES_FILE_NAME);
        }

        public string LicenseRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_RULES_FILE_NAME);
        }

        public string LicenseRuleScopesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_RULE_SCOPES_FILE_NAME);
        }

        public string LicenseUsageAccountReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_USAGE_ACCOUNT_FILE_NAME);
        }

        public string LicenseUsageRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                LICENSE_FOLDER_NAME,
                CONVERT_LICENSE_USAGE_RULES_FILE_NAME);
        }

        public string LicensesExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_LICENSES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region All Application Configuration Data

        public string ApplicationHealthRulesDataFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_HEALTH_RULES_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_HEALTH_RULES_FILE_NAME);
            }
        }

        public string ApplicationHealthRulesDetailsDataFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_HEALTH_RULES_DETAILS_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_HEALTH_RULES_DETAILS_FILE_NAME);
            }
        }

        public string ApplicationPoliciesDataFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_POLICIES_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_POLICIES_FILE_NAME);
            }
        }

        public string ApplicationActionsDataFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_ACTIONS_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    EXTRACT_APPLICATION_ACTIONS_FILE_NAME);
            }
        }

        #endregion

        #region All Application Configuration Index

        public string ApplicationConfigurationHealthRulesIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_HEALTH_RULES_SUMMARY_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_HEALTH_RULES_SUMMARY_FILE_NAME);
            }
        }

        public string ApplicationHealthRulesIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_HEALTH_RULES_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_HEALTH_RULES_FILE_NAME);
            }
        }

        public string ApplicationPoliciesIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_POLICIES_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_POLICIES_FILE_NAME);
            }
        }

        public string ApplicationActionsIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_ACTIONS_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_ACTIONS_FILE_NAME);
            }
        }

        public string ApplicationPolicyActionMappingsIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_POLICY_ACTION_MAPPING_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_FOLDER_NAME,
                    CONVERT_CONFIG_POLICY_ACTION_MAPPING_FILE_NAME);
            }
        }

        #endregion

        #region All Application Configuration Report

        public string ApplicationConfigurationReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME);
        }

        public string ApplicationConfigurationHealthRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME,
                CONVERT_CONFIG_HEALTH_RULES_SUMMARY_FILE_NAME);
        }

        public string ApplicationHealthRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME,
                CONVERT_CONFIG_HEALTH_RULES_FILE_NAME);
        }

        public string ApplicationPoliciesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME,
                CONVERT_CONFIG_POLICIES_FILE_NAME);
        }

        public string ApplicationActionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME,
                CONVERT_CONFIG_ACTIONS_FILE_NAME);
        }

        public string ApplicationPolicyActionMappingsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_ALL_APPLICATIONS_FOLDER_NAME,
                CONVERT_CONFIG_POLICY_ACTION_MAPPING_FILE_NAME);
        }
        #endregion


        #region APM Application Configuration Data 

        public string APMApplicationConfigurationXMLDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONFIGURATION_APPLICATION_FILE_NAME);
        }

        public string APMApplicationConfigurationDetailsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONFIGURATION_APPLICATION_DETAILS_FILE_NAME);
        }


        public string APMApplicationConfigurationSEPDetectionRulesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONFIGURATION_APPLICATION_SEP_DETECTION_FILE_NAME);
        }

        public string APMApplicationConfigurationSEPTierDetectionRulesDataFilePath(JobTarget jobTarget, AppDRESTTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_CONFIGURATION_APPLICATION_TIER_SEP_DETECTION_FILE_NAME,
                getShortenedEntityNameForFileSystem(tier.name, tier.id));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                reportFileName);
        }

        public string APMApplicationConfigurationSEPTierDetectionRulesDataFilePath(JobTarget jobTarget, APMTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_CONFIGURATION_APPLICATION_TIER_SEP_DETECTION_FILE_NAME,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                reportFileName);
        }

        public string APMApplicationConfigurationSEPTierExplicitRulesDataFilePath(JobTarget jobTarget, AppDRESTTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_CONFIGURATION_APPLICATION_TIER_SEP_EXPLICIT_FILE_NAME,
                getShortenedEntityNameForFileSystem(tier.name, tier.id));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                reportFileName);
        }

        public string APMApplicationConfigurationSEPTierExplicitRulesDataFilePath(JobTarget jobTarget, APMTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_CONFIGURATION_APPLICATION_TIER_SEP_EXPLICIT_FILE_NAME,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                reportFileName);
        }

        public string APMApplicationDeveloperModeNodesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                EXTRACT_CONFIGURATION_DEVELOPER_MODE_NODES_FILE_NAME);
        }

        #endregion

        #region APM Application Configuration Index

        public string APMApplicationConfigurationIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_APM_SUMMARY_FILE_NAME);
        }

        public string APMBusinessTransactionDiscoveryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_DISCOVERY_RULES_FILE_NAME);
        }

        public string APMBusinessTransactionEntryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_RULES_FILE_NAME);
        }

        public string APMServiceEndpointDiscoveryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_SERVICE_ENDPOINT_DISCOVERY_RULES_FILE_NAME);
        }

        public string APMServiceEndpointEntryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_SERVICE_ENDPOINT_ENTRY_RULES_FILE_NAME);
        }

        public string APMBusinessTransactionEntryScopesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_SCOPES_FILE_NAME);
        }

        public string APMBusinessTransactionEntryRules20IndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_RULES_2_0_FILE_NAME);
        }

        public string APMBusinessTransactionDiscoveryRules20IndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_DISCOVERY_RULES_2_0_FILE_NAME);
        }

        public string APMBackendDiscoveryRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_BACKEND_DISCOVERY_RULES_FILE_NAME);
        }

        public string APMCustomExitRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_CUSTOM_EXIT_RULES_FILE_NAME);
        }

        public string APMAgentConfigurationPropertiesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_AGENT_CONFIGURATION_PROPERTIES_FILE_NAME);
        }

        public string APMInformationPointRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_INFORMATION_POINT_RULES_FILE_NAME);
        }

        public string APMBusinessTransactionConfigurationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMTierConfigurationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ENTITY_TIERS_FILE_NAME);
        }

        public string APMMethodInvocationDataCollectorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_METHOD_INVOCATION_DATA_COLLECTORS_FILE_NAME);
        }

        public string APMHttpDataCollectorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_HTTP_DATA_COLLECTORS_FILE_NAME);
        }

        public string APMAgentCallGraphSettingsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_AGENT_CALL_GRAPH_SETTINGS_FILE_NAME);
        }

        public string APMDeveloperModeNodesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_DEVELOPER_MODE_NODES_FILE_NAME);
        }

        public string APMErrorDetectionRulesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_RULES_FILE_NAME);
        }

        public string APMErrorDetectionIgnoreMessagesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_IGNORE_MESSAGES_FILE_NAME);
        }

        public string APMErrorDetectionIgnoreLoggersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_IGNORE_LOGGERS_FILE_NAME);
        }

        public string APMErrorDetectionLoggersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_LOGGERS_FILE_NAME);
        }

        public string APMErrorDetectionHTTPCodesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_HTTP_CODES_FILE_NAME);
        }

        public string APMErrorDetectionRedirectPagesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                CONFIGURATION_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_REDIRECT_PAGES_FILE_NAME);
        }

        #endregion

        #region APM Application Configuration Report

        public string APMConfigurationReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME);
        }

        public string APMApplicationConfigurationReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_APM_SUMMARY_FILE_NAME);
        }

        public string APMBusinessTransactionDiscoveryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_DISCOVERY_RULES_FILE_NAME);
        }

        public string APMBusinessTransactionEntryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_RULES_FILE_NAME);
        }

        public string APMServiceEndpointDiscoveryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_SERVICE_ENDPOINT_DISCOVERY_RULES_FILE_NAME);
        }

        public string APMServiceEndpointEntryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_SERVICE_ENDPOINT_ENTRY_RULES_FILE_NAME);
        }

        public string APMBusinessTransactionEntryScopesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_SCOPES_FILE_NAME);
        }

        public string APMBusinessTransactionEntryRules20ReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_ENTRY_RULES_2_0_FILE_NAME);
        }

        public string APMBusinessTransactionDiscoveryRules20ReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_BUSINESS_TRANSACTION_DISCOVERY_RULES_2_0_FILE_NAME);
        }

        public string APMBackendDiscoveryRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_BACKEND_DISCOVERY_RULES_FILE_NAME);
        }

        public string APMCustomExitRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_CUSTOM_EXIT_RULES_FILE_NAME);
        }

        public string APMAgentConfigurationPropertiesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_AGENT_CONFIGURATION_PROPERTIES_FILE_NAME);
        }

        public string APMInformationPointRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_INFORMATION_POINT_RULES_FILE_NAME);
        }

        public string APMBusinessTransactionConfigurationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ENTITY_BUSINESS_TRANSACTIONS_FILE_NAME);
        }

        public string APMTierConfigurationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ENTITY_TIERS_FILE_NAME);
        }

        public string APMMethodInvocationDataCollectorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_METHOD_INVOCATION_DATA_COLLECTORS_FILE_NAME);
        }

        public string APMHttpDataCollectorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_HTTP_DATA_COLLECTORS_FILE_NAME);
        }

        public string APMAgentCallGraphSettingsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_AGENT_CALL_GRAPH_SETTINGS_FILE_NAME);
        }

        public string APMDeveloperModeNodesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_DEVELOPER_MODE_NODES_FILE_NAME);
        }

        public string APMErrorDetectionRulesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_RULES_FILE_NAME);
        }

        public string APMErrorDetectionIgnoreMessagesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_IGNORE_MESSAGES_FILE_NAME);
        }

        public string APMErrorDetectionIgnoreLoggersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_IGNORE_LOGGERS_FILE_NAME);
        }

        public string APMErrorDetectionLoggersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_LOGGERS_FILE_NAME);
        }

        public string APMErrorDetectionHTTPCodesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_HTTP_CODES_FILE_NAME);
        }

        public string APMErrorDetectionRedirectPagesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_APM_FOLDER_NAME,
                CONVERT_CONFIG_ERROR_DETECTION_REDIRECT_PAGES_FILE_NAME);
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
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region Configuration Comparison Data

        public string TemplateControllerConfigurationSourceFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                TEMPLATE_CONTROLLER_AND_APPLICATIONS_FOLDER_NAME);
        }

        public string TemplateControllerConfigurationTargetFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME);
        }

        #endregion

        #region Configuration Comparison Index

        public string ConfigurationComparisonIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    CONFIGURATION_COMPARISON_FOLDER_NAME,
                    CONFIGURATION_DIFFERENCES_FILE_NAME);
            }
            else if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_WEB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_COMPARISON_FOLDER_NAME,
                    CONFIGURATION_DIFFERENCES_WEB_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    CONFIGURATION_COMPARISON_FOLDER_NAME,
                    CONFIGURATION_DIFFERENCES_FILE_NAME);
            }
        }

        #endregion

        #region Configuration Comparison Report

        public string ConfigurationComparisonReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_COMPARISON_FOLDER_NAME);
        }

        public string ConfigurationComparisonReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONFIGURATION_COMPARISON_FOLDER_NAME,
                CONFIGURATION_DIFFERENCES_FILE_NAME);
        }

        #endregion


        #region Health Check Index

        public string ControllerHealthCheckRuleResultsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                HEALTHCHECK_FOLDER_NAME,
                CONVERT_CONTROLLER_HEALTH_CHECK_RULE_RESULTS_FILE_NAME);
        }

        public string APMHealthCheckRuleResultsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                HEALTHCHECK_FOLDER_NAME,
                CONVERT_APM_HEALTH_CHECK_RULE_RESULTS_FILE_NAME);
        }

        #endregion

        #region Health Check Report

        public string HealthCheckSettingMappingFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                HEALTH_CHECK_SETTING_MAPPING_FILE_NAME);
        }

        public string ControllerHealthCheckReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                HEALTHCHECK_FOLDER_NAME);
        }

        public string APMHealthCheckReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                HEALTHCHECK_APM_FOLDER_NAME);
        }

        public string ControllerHealthCheckRuleResultsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                HEALTHCHECK_FOLDER_NAME,
                CONVERT_CONTROLLER_HEALTH_CHECK_RULE_RESULTS_FILE_NAME);
        }

        public string APMHealthCheckRuleResultsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                HEALTHCHECK_APM_FOLDER_NAME,
                CONVERT_APM_HEALTH_CHECK_RULE_RESULTS_FILE_NAME);
        }

        public string HealthCheckResultsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_HEALTH_CHECK_RESULTS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region RBAC Data

        public string UsersDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                EXTRACT_USERS_FILE_NAME);
        }

        public string GroupsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                EXTRACT_GROUPS_FILE_NAME);
        }

        public string RolesDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                EXTRACT_ROLES_FILE_NAME);
        }

        public string UserDataFilePath(JobTarget jobTarget, string userName, long userID)
        {
            string reportFileName = String.Format(
                EXTRACT_USER_FILE_NAME,
                getShortenedEntityNameForFileSystem(userName, userID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                reportFileName);
        }

        public string GroupDataFilePath(JobTarget jobTarget, string groupName, long groupID)
        {
            string reportFileName = String.Format(
                EXTRACT_GROUP_FILE_NAME,
                getShortenedEntityNameForFileSystem(groupName, groupID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                reportFileName);
        }

        public string GroupUsersDataFilePath(JobTarget jobTarget, string groupName, long groupID)
        {
            string reportFileName = String.Format(
                EXTRACT_GROUP_USERS_FILE_NAME,
                getShortenedEntityNameForFileSystem(groupName, groupID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                reportFileName);
        }

        public string RoleDataFilePath(JobTarget jobTarget, string roleName, long roleID)
        {
            string reportFileName = String.Format(
                EXTRACT_ROLE_FILE_NAME,
                getShortenedEntityNameForFileSystem(roleName, roleID));

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                reportFileName);
        }

        public string SecurityProviderTypeDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                EXTRACT_SECURITY_PROVIDER_FILE_NAME);
        }

        public string StrongPasswordsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                EXTRACT_STRONG_PASSWORDS_FILE_NAME);
        }

        #endregion

        #region RBAC Index

        public string UsersIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_USERS_FILE_NAME);
        }

        public string GroupsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_GROUPS_FILE_NAME);
        }

        public string RolesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_ROLES_FILE_NAME);
        }

        public string PermissionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_PERMISSIONS_FILE_NAME);
        }

        public string GroupMembershipsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_GROUP_MEMBERSHIPS_FILE_NAME);
        }

        public string RoleMembershipsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_ROLE_MEMBERSHIPS_FILE_NAME);
        }

        public string UserPermissionsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_USER_PERMISSIONS_FILE_NAME);
        }

        public string RBACControllerSummaryIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_CONTROLLER_RBAC_SUMMARY_FILE_NAME);
        }

        #endregion

        #region RBAC Report

        public string UsersGroupsRolesPermissionsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME);
        }

        public string UsersGroupsRolesPermissionsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_USERS_GROUPS_ROLES_PERMISSIONS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        public string UsersReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_USERS_FILE_NAME);
        }

        public string GroupsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_GROUPS_FILE_NAME);
        }

        public string RolesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_ROLES_FILE_NAME);
        }

        public string PermissionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_PERMISSIONS_FILE_NAME);
        }

        public string GroupMembershipsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_GROUP_MEMBERSHIPS_FILE_NAME);
        }

        public string RoleMembershipsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_ROLE_MEMBERSHIPS_FILE_NAME);
        }

        public string UserPermissionsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_USER_PERMISSIONS_FILE_NAME);
        }

        public string RBACControllerSummaryReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                CONTROLLER_RBAC_FOLDER_NAME,
                CONVERT_CONTROLLER_RBAC_SUMMARY_FILE_NAME);
        }

        public string RBACExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_USERS_GROUPS_ROLES_PERMISSIONS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
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

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                    APP_METRICS_FOLDER_NAME,
                    entityFolderName,
                    metricEntitySubFolderName,
                    reportFileName);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    APP_METRICS_FOLDER_NAME,
                    entityFolderName,
                    metricEntitySubFolderName,
                    reportFileName);
            }
        }

        public string MetricHourRangeDataFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                EXTRACT_METRIC_HOUR_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                    APP_METRICS_FOLDER_NAME,
                    entityFolderName,
                    metricEntitySubFolderName,
                    reportFileName);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    APP_METRICS_FOLDER_NAME,
                    entityFolderName,
                    metricEntitySubFolderName,
                    reportFileName);
            }
        }

        #endregion

        #region Entity Metrics Index

        public string APMEntitiesFullIndexFilePath(JobTarget jobTarget, string entityFolderName)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_FOLDER_NAME,
                entityFolderName,
                CONVERT_ENTITIES_METRICS_SUMMARY_FULLRANGE_FILE_NAME);
        }

        public string APMEntitiesHourIndexFilePath(JobTarget jobTarget, string entityFolderName)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_FOLDER_NAME,
                entityFolderName,
                CONVERT_ENTITIES_METRICS_SUMMARY_HOURLY_FILE_NAME);
        }

        public string MetricValuesIndexFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_METRICS_VALUES_FILE_NAME,
                metricEntitySubFolderName);

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                    APP_METRICS_FOLDER_NAME,
                    entityFolderName,
                    reportFileName);            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    APP_METRICS_FOLDER_NAME,
                    entityFolderName,
                    reportFileName);
            }
        }

        public string MetricsLocationIndexFilePath(JobTarget jobTarget, string entityFolderName)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_FOLDER_NAME,
                entityFolderName,
                CONVERT_ENTITIES_METRICS_LOCATIONS_FILE_NAME);
        }

        #endregion

        #region Entity Metrics Report

        public string MetricsReportFolderPath(JobTarget jobTarget)
        {
            string folderName = APP_METRICS_FOLDER_NAME;
            switch (jobTarget.Type)
            {
                case JobStepBase.APPLICATION_TYPE_APM:
                    folderName = APP_METRICS_APM_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_WEB:
                    folderName = APP_METRICS_WEB_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_MOBILE:
                    folderName = APP_METRICS_MOBILE_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_BIQ:
                    folderName = APP_METRICS_BIQ_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_SIM:
                    folderName = APP_METRICS_SIM_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_DB:
                    folderName = APP_METRICS_DB_FOLDER_NAME;
                    break;
            }

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                folderName);
        }

        public string APMEntitiesFullReportFilePath(string entityFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_SUMMARY_FULLRANGE_FILE_NAME,
                entityFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APP_METRICS_APM_FOLDER_NAME,
                reportFileName);
        }

        public string APMEntitiesHourReportFilePath(string entityFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_SUMMARY_HOURLY_FILE_NAME,
                entityFolderName);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APP_METRICS_APM_FOLDER_NAME,
                reportFileName);
        }

        public string MetricReportFilePath(string entityFolderName, string metricEntitySubFolderName, JobTarget jobTarget)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_VALUES_FILE_NAME,
                entityFolderName,
                metricEntitySubFolderName);

            string folderName = APP_METRICS_FOLDER_NAME;
            switch (jobTarget.Type)
            {
                case JobStepBase.APPLICATION_TYPE_APM:
                    folderName = APP_METRICS_APM_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_WEB:
                    folderName = APP_METRICS_WEB_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_MOBILE:
                    folderName = APP_METRICS_MOBILE_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_BIQ:
                    folderName = APP_METRICS_BIQ_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_SIM:
                    folderName = APP_METRICS_SIM_FOLDER_NAME;
                    break;
                case JobStepBase.APPLICATION_TYPE_DB:
                    folderName = APP_METRICS_DB_FOLDER_NAME;
                    break;
            }

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                folderName,
                reportFileName);
        }

        public string MetricReportPerAppFilePath(JobTarget jobTarget, string entityFolderName, string metricEntitySubFolderName)
        {
            string reportFileName = String.Format(
                CONVERT_ENTITIES_ALL_METRICS_VALUES_FILE_NAME,
                entityFolderName,
                metricEntitySubFolderName);

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.DBCollectorID),
                    APP_METRICS_FOLDER_NAME,
                    reportFileName);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    APP_METRICS_FOLDER_NAME,
                    reportFileName);
            }
        }

        public string APMEntityMetricsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_METRICS_ALL_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion

        #region Entity Metric Graphs Report

        public string APMEntityTypeMetricGraphsExcelReportFilePath(APMEntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Format(
                REPORT_METRICS_GRAPHS_FILE_NAME,
                entity.FolderName,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                jobTimeRange.From,
                jobTimeRange.To);

            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    entity.FolderName,
                    reportFileName);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    entity.FolderName,
                    reportFileName);
            }

            return reportFilePath;
        }

        #endregion


        #region List of Metrics Data

        public string MetricsListDataFilePath(JobTarget jobTarget, string filePrefix, int depth)
        {
            string reportFileName = String.Format(
                EXTRACT_METRIC_LIST,
                filePrefix,
                depth);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_LIST_FOLDER_NAME,
                reportFileName);
        }

        #endregion

        #region List of Metrics Index

        public string MetricsListIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_LIST_FOLDER_NAME,
                CONVERT_METRICS_LIST);
        }

        public string MetricPrefixSummaryIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_LIST_FOLDER_NAME,
                CONVERT_METRIC_SUMMARY_LIST);
        }

        #endregion

        #region List of Metrics Report

        public string APMMetricsListReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_METRICS_LIST_FOLDER_NAME);
        }

        public string MetricsListReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_METRICS_LIST_FOLDER_NAME,
                CONVERT_METRICS_LIST);
        }

        public string MetricPrefixSummaryReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_METRICS_LIST_FOLDER_NAME,
                CONVERT_METRIC_SUMMARY_LIST);
        }

        public string MetricsListApplicationReportFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APP_METRICS_LIST_FOLDER_NAME,
                CONVERT_METRICS_LIST);
        }

        public string MetricsListApplicationExcelReportFilePath(JobTarget jobTarget, bool absolutePath)
        {
            string reportFileName = String.Format(
                REPORT_METRICS_LIST_APPLICATION_FILE_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID));

            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    APP_METRICS_LIST_FOLDER_NAME,
                    reportFileName);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    APP_METRICS_LIST_FOLDER_NAME,
                    reportFileName);
            }

            return reportFilePath;
        }

        public string MetricsListExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_METRICS_LIST_ALL_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMApplication.ENTITY_FOLDER,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMTier.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(tier.name, tier.id),
                reportFileName);
        }

        public string TierFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, APMTier tier)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMTier.ENTITY_FOLDER,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.tierName, node.tierId),
                getShortenedEntityNameForFileSystem(node.name, node.id),
                reportFileName);
        }

        public string NodeFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, APMNode node)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMNode.ENTITY_FOLDER,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMBackend.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(backend.name, backend.id),
                reportFileName);
        }

        public string BackendFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, APMBackend backend)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMBackend.ENTITY_FOLDER,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMBusinessTransaction.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(businessTransaction.tierName, businessTransaction.tierId),
                getShortenedEntityNameForFileSystem(businessTransaction.name, businessTransaction.id),
                reportFileName);
        }

        public string BusinessTransactionFlowmapDataFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, APMBusinessTransaction businessTransaction)
        {
            string reportFileName = String.Format(
                EXTRACT_ENTITY_FLOWMAP_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMBusinessTransaction.ENTITY_FOLDER,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMApplication.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string ApplicationFlowmapPerMinuteIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMApplication.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME);
        }

        public string TiersFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMTier.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string NodesFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMNode.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string BackendsFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMBackend.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        public string BusinessTransactionsFlowmapIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_ACTIVITYGRID_FOLDER_NAME,
                APMBusinessTransaction.ENTITY_FOLDER,
                CONVERT_ACTIVITY_GRIDS_FILE_NAME);
        }

        #endregion

        #region Entity Flowmap Report

        public string ActivityGridReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME);
        }

        public string ApplicationsFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                APMApplication.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string ApplicationsFlowmapPerMinuteReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_PERMINUTE_FILE_NAME,
                APMApplication.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string TiersFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                APMTier.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string NodesFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                APMNode.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string BackendsFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                APMBackend.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string BusinessTransactionsFlowmapReportFilePath()
        {
            string reportFileName = String.Format(
                CONVERT_ALL_ACTIVITY_GRIDS_FILE_NAME,
                APMBusinessTransaction.ENTITY_FOLDER);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_ACTIVITYGRID_FOLDER_NAME,
                reportFileName);
        }

        public string FlowmapsExcelReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_FLOWMAPS_ALL_ENTITIES_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region Events Data

        public string ApplicationHealthRuleViolationsDataFilePath(JobTarget jobTarget)
        {
            string reportFileName = String.Format(
                EXTRACT_HEALTH_RULE_VIOLATIONS_FILE_NAME,
                this.JobConfiguration.Input.TimeRange.From,
                this.JobConfiguration.Input.TimeRange.To);

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    reportFileName);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    reportFileName);
            }
        }

        public string ApplicationEventsDataFilePath(JobTarget jobTarget, string eventType)
        {
            string reportFileName = String.Format(
                EXTRACT_EVENTS_FILE_NAME,
                eventType,
                this.JobConfiguration.Input.TimeRange.From,
                this.JobConfiguration.Input.TimeRange.To);

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    reportFileName);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    reportFileName);
            }
        }
        public string ApplicationEventsWithDetailsDataFilePath(JobTarget jobTarget, string eventType)
        {
            string reportFileName = String.Format(
                EXTRACT_EVENTS_WITH_DETAILS_FILE_NAME,
                eventType,
                this.JobConfiguration.Input.TimeRange.From,
                this.JobConfiguration.Input.TimeRange.To);

            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    reportFileName);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    DATA_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    reportFileName);
            }
        }

        public string AuditEventsDataFilePath(JobTarget jobTarget)
        {
            string reportFileName = String.Format(
                EXTRACT_AUDIT_EVENTS_FILE_NAME,
                this.JobConfiguration.Input.TimeRange.From,
                this.JobConfiguration.Input.TimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                EVENTS_FOLDER_NAME,
                reportFileName);
        }

        public string NotificationsDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                EVENTS_FOLDER_NAME,
                EXTRACT_NOTIFICATIONS_FILE_NAME);
        }


        #endregion

        #region Events Index

        public string ApplicationEventsSummaryIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_EVENTS_SUMMARY_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_EVENTS_SUMMARY_FILE_NAME);
            }
        }

        public string ApplicationHealthRuleViolationsIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_HEALTH_RULE_EVENTS_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_HEALTH_RULE_EVENTS_FILE_NAME);
            }
        }

        public string ApplicationEventsIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_EVENTS_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_EVENTS_FILE_NAME);
            }
        }

        public string ApplicationEventDetailsIndexFilePath(JobTarget jobTarget)
        {
            if (jobTarget.Type == JobStepBase.APPLICATION_TYPE_DB)
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(DBMON_APPLICATION_NAME, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_EVENTS_DETAILS_FILE_NAME);
            }
            else
            {
                return Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    INDEX_FOLDER_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                    getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                    EVENTS_FOLDER_NAME,
                    CONVERT_APPLICATION_EVENTS_DETAILS_FILE_NAME);
            }
        }

        public string AuditEventsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                EVENTS_FOLDER_NAME,
                CONVERT_CONTROLLER_AUDIT_EVENTS_FILE_NAME);
        }

        public string NotificationsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                EVENTS_FOLDER_NAME,
                CONVERT_CONTROLLER_NOTIFICATIONS_FILE_NAME);
        }

        #endregion

        #region Events Report

        public string ApplicationEventsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_APPS_FOLDER_NAME);
        }

        public string ControllerEventsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_CONTROLLER_FOLDER_NAME);
        }

        public string ApplicationHealthRuleViolationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_APPS_FOLDER_NAME,
                CONVERT_APPLICATION_HEALTH_RULE_EVENTS_FILE_NAME);
        }

        public string ApplicationEventsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_APPS_FOLDER_NAME,
                CONVERT_APPLICATION_EVENTS_FILE_NAME);
        }

        public string ApplicationEventDetailsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_APPS_FOLDER_NAME,
                CONVERT_APPLICATION_EVENTS_DETAILS_FILE_NAME);
        }

        public string AuditEventsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_CONTROLLER_FOLDER_NAME,
                CONVERT_CONTROLLER_AUDIT_EVENTS_FILE_NAME);
        }

        public string NotificationsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_CONTROLLER_FOLDER_NAME,
                CONVERT_CONTROLLER_NOTIFICATIONS_FILE_NAME);
        }

        public string ApplicationEventsSummaryReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                EVENTS_APPS_FOLDER_NAME,
                CONVERT_APPLICATION_EVENTS_SUMMARY_FILE_NAME);
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
                this.ReportFolderName,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
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

        public string SnapshotsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsSegmentsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsExitCallsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsServiceEndpointCallsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsDetectedErrorsIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsBusinessDataIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsMethodCallLinesIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_TIMERANGE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        #endregion

        #region Snapshots Business Transaction

        public string SnapshotsIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsSegmentsIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
        }

        public string SnapshotsExitCallsIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
        }

        public string SnapshotsServiceEndpointCallsIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
        }

        public string SnapshotsDetectedErrorsIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
        }

        public string SnapshotsBusinessDataIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesIndexBusinessTransactionFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
        }

        #endregion

        #region Snapshots All

        public string SnapshotsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsSegmentsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
        }

        public string SnapshotsExitCallsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
        }

        public string SnapshotsServiceEndpointCallsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
        }

        public string SnapshotsDetectedErrorsIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
        }

        public string SnapshotsBusinessDataIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesIndexFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
        }

        #endregion

        #region Snapshots Folded Call Stacks All

        public string SnapshotsFoldedCallStacksIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksIndexBusinessTransactionNodeHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, APMNode node, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                APMNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionNodeHourRangeFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction, APMNode node, JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_TIMERANGE_FILE_NAME,
                jobTimeRange.From,
                jobTimeRange.To);

            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                APMNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                reportFileName);
        }

        public string SnapshotsFoldedCallStacksIndexApplicationFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksIndexEntityFilePath(JobTarget jobTarget, APMTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksIndexEntityFilePath(JobTarget jobTarget, APMNode node)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                APMNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.TierName, node.TierID),
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksIndexEntityFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(businessTransaction.TierName, businessTransaction.TierID),
                getShortenedEntityNameForFileSystem(businessTransaction.BTName, businessTransaction.BTID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexApplicationFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(JobTarget jobTarget, APMTier tier)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                getShortenedEntityNameForFileSystem(tier.TierName, tier.TierID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(JobTarget jobTarget, APMNode node)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                APMNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(node.TierName, node.TierID),
                getShortenedEntityNameForFileSystem(node.NodeName, node.NodeID),
                CONVERT_SNAPSHOTS_SEGMENTS_FOLDED_CALL_STACKS_WITH_TIME_FILE_NAME);
        }

        public string SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(JobTarget jobTarget, APMBusinessTransaction businessTransaction)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                INDEX_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
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
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_APPLICATION_SNAPSHOTS_SUMMARY_FILE_NAME);
        }

        #endregion

        #region Snapshots Report

        public string SnapshotsReportFolderPath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME);
        }

        public string SnapshotsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_FILE_NAME);
        }

        public string SnapshotsSegmentsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
        }

        public string SnapshotsExitCallsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
        }

        public string SnapshotsServiceEndpointCallsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
        }

        public string SnapshotsDetectedErrorsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
        }

        public string SnapshotsBusinessDataReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
        }

        public string SnapshotsMethodCallLinesReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
        }

        public string ApplicationSnapshotsReportFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                APM_SNAPSHOTS_FOLDER_NAME,
                CONVERT_APPLICATION_SNAPSHOTS_SUMMARY_FILE_NAME);
        }

        public string SnapshotReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_FILE_NAME);
            }

            return reportFilePath;
        }

        public string SnapshotSegmentsReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_FILE_NAME);
            }

            return reportFilePath;
        }

        public string SnapshotExitCallsReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_EXIT_CALLS_FILE_NAME);
            }

            return reportFilePath;
        }

        public string SnapshotServiceEndpointCallsReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_SERVICE_ENDPOINTS_CALLS_FILE_NAME);
            }

            return reportFilePath;
        }

        public string SnapshotDetectedErrorsReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_DETECTED_ERRORS_FILE_NAME);
            }

            return reportFilePath;
        }

        public string SnapshotBusinessDataReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_BUSINESS_DATA_FILE_NAME);
            }

            return reportFilePath;
        }

        public string SnapshotMethodCallLinesReportFilePath(Snapshot snapshot, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    APM_SNAPSHOTS_FOLDER_NAME,
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                    getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                    getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                    getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                    snapshot.RequestID,
                    CONVERT_SNAPSHOTS_SEGMENTS_METHOD_CALL_LINES_FILE_NAME);
            }

            return reportFilePath;
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
                this.ReportFolderName,
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
                this.ReportFolderName,
                reportFileName);
        }

        public string SnapshotExcelReportFilePath(Snapshot snapshot)
        {
            string reportFileName = String.Format(
                REPORT_SNAPSHOT_FILE_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(snapshot.Controller)),
                getShortenedEntityNameForFileSystem(snapshot.ApplicationName, snapshot.ApplicationID),
                getShortenedEntityNameForFileSystem(snapshot.TierName, snapshot.TierID),
                getShortenedEntityNameForFileSystem(snapshot.BTName, snapshot.BTID),
                snapshot.RequestID);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        #endregion


        #region Entity Dashboard Screenshots Data

        public string ApplicationDashboardScreenshotDataFilePath(JobTarget jobTarget)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                APMApplication.ENTITY_FOLDER,
                EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME);
        }

        public string TierDashboardScreenshotDataFilePath(JobTarget jobTarget, AppDRESTTier tier)
        {
            return TierDashboardScreenshotDataFilePath(jobTarget, tier.name, tier.id);
        }

        public string TierDashboardScreenshotDataFilePath(JobTarget jobTarget, string tierName, long tierID)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                APMTier.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(tierName, tierID),
                EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME);
        }

        public string NodeDashboardScreenshotDataFilePath(JobTarget jobTarget, AppDRESTNode node)
        {
            return NodeDashboardScreenshotDataFilePath(jobTarget, node.tierName, node.tierId, node.name, node.id);
        }

        public string NodeDashboardScreenshotDataFilePath(JobTarget jobTarget, string tierName, long tierID, string nodeName, long nodeID)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                APMNode.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(tierName, tierID),
                getShortenedEntityNameForFileSystem(nodeName, nodeID),
                EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME);
        }

        public string BackendDashboardScreenshotDataFilePath(JobTarget jobTarget, AppDRESTBackend backend)
        {
            return BackendDashboardScreenshotDataFilePath(jobTarget, backend.name, backend.id);
        }

        public string BackendDashboardScreenshotDataFilePath(JobTarget jobTarget, string backendName, long backendID)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                APMBackend.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(backendName, backendID),
                EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME);
        }

        public string BusinessTransactionDashboardScreenshotDataFilePath(JobTarget jobTarget, AppDRESTBusinessTransaction businessTransaction)
        {
            return BusinessTransactionDashboardScreenshotDataFilePath(jobTarget, businessTransaction.tierName, businessTransaction.tierId, businessTransaction.name, businessTransaction.id);
        }

        public string BusinessTransactionDashboardScreenshotDataFilePath(JobTarget jobTarget, string tierName, long tierID, string businessTransactionName, long businessTransactionID)
        {
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                DATA_FOLDER_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                APMBusinessTransaction.ENTITY_FOLDER,
                getShortenedEntityNameForFileSystem(tierName, tierID),
                getShortenedEntityNameForFileSystem(businessTransactionName, businessTransactionID),
                EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME);
        }

        #endregion

        #region Entity Dashboard Screenshots Report

        public string ApplicationsLinksTemplateFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                LINKS_TO_APPLICATIONS_TEMPLATE_FILE_NAME);
        }

        public string ApplicationDashboardsLinksTemplateFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                LINKS_TO_APPLICATION_DASHBOARDS_TEMPLATE_FILE_NAME);
        }

        public string ApplicationsLinksReportFilePath(JobTimeRange jobTimeRange)
        {
            string reportFileName = String.Format(
                REPORT_APPLICATIONS_DASHBOARDS_FILE_NAME,
                this.ProgramOptions.JobName,
                jobTimeRange.From,
                jobTimeRange.To);
            return Path.Combine(
                this.ProgramOptions.OutputJobFolderPath,
                this.ReportFolderName,
                reportFileName);
        }

        public string ApplicationDashboardLinksReportFilePath(APMApplication application, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(application.Controller)),
                    getShortenedEntityNameForFileSystem(application.ApplicationName, application.ApplicationID),
                    REPORT_APPLICATION_DASHBOARDS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(application.Controller)),
                    getShortenedEntityNameForFileSystem(application.ApplicationName, application.ApplicationID),
                    REPORT_APPLICATION_DASHBOARDS_FILE_NAME);
            }

            return reportFilePath;
        }

        public string EntityDashboardScreenshotReportFilePath(APMEntityBase entity, bool returnAbsolutePath, bool returnThumb)
        {
            string reportFileName = EXTRACT_DASHBOARD_SCREENSHOT_FILE_NAME;
            if (returnThumb == true) reportFileName = EXTRACT_DASHBOARD_SCREENSHOT_THUMB_FILE_NAME;

            string reportFilePath = String.Empty;
            string reportRelativeFilePath = String.Empty;

            if (entity is APMApplication)
            {
                reportRelativeFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                    entity.FolderName,
                    reportFileName);
            }
            else if (entity is APMTier)
            {
                reportRelativeFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                    entity.FolderName,
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    reportFileName);
            }
            else if (entity is APMNode)
            {
                reportRelativeFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                    entity.FolderName,
                    getShortenedEntityNameForFileSystem(((APMNode)entity).TierName, ((APMNode)entity).TierID),
                    getShortenedEntityNameForFileSystem(((APMNode)entity).NodeName, ((APMNode)entity).NodeID),
                    reportFileName);
            }
            else if (entity is APMBusinessTransaction)
            {
                reportRelativeFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                    entity.FolderName,
                    getShortenedEntityNameForFileSystem(((APMBusinessTransaction)entity).TierName, ((APMBusinessTransaction)entity).TierID),
                    getShortenedEntityNameForFileSystem(((APMBusinessTransaction)entity).BTName, ((APMBusinessTransaction)entity).BTID),
                    reportFileName);
            }
            else if (entity is APMBackend)
            {
                reportRelativeFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    APM_DASHBOARD_SCREENSHOTS_FOLDER_NAME,
                    entity.FolderName,
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    reportFileName);
            }

            if (reportRelativeFilePath.Length > 0)
            {
                if (returnAbsolutePath == true)
                {
                    reportFilePath = Path.Combine(
                        this.ProgramOptions.OutputJobFolderPath,
                        this.ReportFolderName,
                        reportRelativeFilePath);
                }
                else
                {
                    reportFilePath = reportRelativeFilePath;
                }
            }

            return reportFilePath;
        }

        #endregion


        #region Flame Graph Report

        public string FlameGraphTemplateFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                FLAME_GRAPH_TEMPLATE_FILE_NAME);
        }

        public string ApplicationFlameGraphsLinksTemplateFilePath()
        {
            return Path.Combine(
                this.ProgramOptions.ProgramLocationFolderPath,
                LINKS_TO_APPLICATION_FLAMEGRAPHSS_TEMPLATE_FILE_NAME);
        }

        public string ApplicationFlameGraphsLinksReportFilePath(APMApplication application, bool absolutePath)
        {
            string reportFilePath = String.Empty;

            if (absolutePath == true)
            {
                reportFilePath = Path.Combine(
                    this.ProgramOptions.OutputJobFolderPath,
                    this.ReportFolderName,
                    getFileSystemSafeString(getControllerNameForFileSystem(application.Controller)),
                    getShortenedEntityNameForFileSystem(application.ApplicationName, application.ApplicationID),
                    REPORT_APPLICATION_FLAMEGRAPHS_FILE_NAME);
            }
            else
            {
                reportFilePath = Path.Combine(
                    getFileSystemSafeString(getControllerNameForFileSystem(application.Controller)),
                    getShortenedEntityNameForFileSystem(application.ApplicationName, application.ApplicationID),
                    REPORT_APPLICATION_FLAMEGRAPHS_FILE_NAME);
            }

            return reportFilePath;
        }


        public string FlameGraphReportFilePath(APMEntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entity is APMApplication)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_APPLICATION_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMTier)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_TIER_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMNode)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_NODE_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMBusinessTransaction)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_GRAPH_BUSINESS_TRANSACTION_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
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
                        this.ReportFolderName,
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        public string FlameChartReportFilePath(APMEntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entity is APMApplication)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_APPLICATION_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMTier)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_TIER_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMNode)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_NODE_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMBusinessTransaction)
            {
                reportFileName = String.Format(
                    REPORT_FLAME_CHART_BUSINESS_TRANSACTION_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
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
                        this.ReportFolderName,
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        #endregion


        #region Entity Details Report

        public string EntityMetricAndDetailExcelReportFilePath(APMEntityBase entity, JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            if (entity is APMApplication)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_APPLICATION_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMTier)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMNode)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMBackend)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMBusinessTransaction)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMServiceEndpoint)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
                    getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                    getShortenedEntityNameForFileSystem(entity.EntityName, entity.EntityID),
                    jobTimeRange.From,
                    jobTimeRange.To);
            }
            else if (entity is APMError)
            {
                reportFileName = String.Format(
                    REPORT_ENTITY_DETAILS_ENTITY_FILE_NAME,
                    getFileSystemSafeString(getControllerNameForFileSystem(entity.Controller)),
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
                        this.ReportFolderName,
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(entity.ApplicationName, entity.ApplicationID),
                        entity.FolderName,
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        #endregion


        #region Application Summary Report

        public string ApplicationSummaryWordReportFilePath(JobTarget jobTarget, JobTimeRange jobTimeRange, bool absolutePath)
        {
            string reportFileName = String.Empty;
            string reportFilePath = String.Empty;

            reportFileName = String.Format(
                REPORT_APPLICATION_SUMMARY_FILE_NAME,
                getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                jobTimeRange.From,
                jobTimeRange.To);

            if (reportFileName.Length > 0)
            {
                if (absolutePath == true)
                {
                    reportFilePath = Path.Combine(
                        this.ProgramOptions.OutputJobFolderPath,
                        this.ReportFolderName,
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                        reportFileName);
                }
                else
                {
                    reportFilePath = Path.Combine(
                        getFileSystemSafeString(getControllerNameForFileSystem(jobTarget.Controller)),
                        getShortenedEntityNameForFileSystem(jobTarget.Application, jobTarget.ApplicationID),
                        reportFileName);
                }
            }

            return reportFilePath;
        }

        #endregion


        #region Helper function for various entity naming

        public static string getFileSystemSafeString(string fileOrFolderNameToClear)
        {
            if (fileOrFolderNameToClear == null) fileOrFolderNameToClear = String.Empty;

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileOrFolderNameToClear = fileOrFolderNameToClear.Replace(c, '-');
            }

            return fileOrFolderNameToClear;
        }

        public static string getShortenedEntityNameForFileSystem(string entityName, long entityID)
        {
            return getShortenedEntityNameForFileSystem(entityName, entityID.ToString());
        }

        public static string getShortenedEntityNameForFileSystem(string entityName, string entityID)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getFileSystemSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 12) entityName = entityName.Substring(0, 12);

            return String.Format("{0}.{1}", entityName, entityID);
        }

        public static string getShortenedEntityNameForFileSystem(string entityName)
        {
            string originalEntityName = entityName;

            // First, strip out unsafe characters
            entityName = getFileSystemSafeString(entityName);

            // Second, shorten the string 
            if (entityName.Length > 12) entityName = entityName.Substring(0, 12);

            return entityName;
        }

        public static string getControllerNameForFileSystem(string controllerURL)
        {
            Uri controllerUri = new Uri(controllerURL);
            string controllerHost = controllerUri.Host.Replace("saas.appdynamics.com", "saas");
            return controllerHost;
        }

        #endregion
    }
}
