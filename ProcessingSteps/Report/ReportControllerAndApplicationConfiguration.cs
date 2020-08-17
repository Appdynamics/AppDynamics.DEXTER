using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportControllerAndApplicationConfiguration : JobStepReportBase
    {
        #region Constants for report contents

        // --------------------------------------------------
        // Sheets

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";

        // Controller wide settings
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_CONTROLLER_SETTINGS = "5.Controller Settings";

        // Alert and Response components
        private const string SHEET_CONTROLLER_EMAIL_ALERT_TEMPLATES = "6.Email Alert Templates";
        private const string SHEET_CONTROLLER_HTTP_ALERT_TEMPLATES = "7.HTTP Alert Templates";
        private const string SHEET_APP_HEALTH_RULES_SUMMARY = "8.Health Rules Summary";
        private const string SHEET_APP_HEALTH_RULES = "9.Health Rules";
        private const string SHEET_APP_HEALTH_RULES_PIVOT = "9.Health Rules.Type";
        private const string SHEET_APP_POLICIES = "10.Policies";
        private const string SHEET_APP_POLICIES_PIVOT = "10.Policies.Type";
        private const string SHEET_APP_ACTIONS = "11.Actions";
        private const string SHEET_APP_ACTIONS_PIVOT = "11.Actions.Type";
        private const string SHEET_APP_POLICIES_TO_ACTIONS_MAPPING = "12.Policy Actions";

        // APM Configuration
        private const string SHEET_APM_APPLICATION_CONFIGURATION = "20.APM Application Config";
        private const string SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES = "21.APM BT Discovery Rules";
        private const string SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES = "22.APM BT Entry Rules";
        private const string SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE = "22.APM BT Entry Rules.Type";
        private const string SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION = "22.APM BT Entry Rules.Location";
        private const string SHEET_APM_BUSINESS_TRANSACTION_SCOPES = "23.APM BT Scopes";
        private const string SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES_20 = "24.APM BT Discovery Rules 2.0";
        private const string SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20 = "25.APM BT Entry Rules 2.0";
        private const string SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE = "25.APM BT Entry 2.0.Type";
        private const string SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION = "25.APM BT Entry 2.0.Location";
        private const string SHEET_APM_BACKEND_DISCOVERY_RULES = "26.APM Backend Discovery Rules";
        private const string SHEET_APM_BACKEND_DISCOVERY_RULES_PIVOT_TYPE = "26.APM Backend Disc Rules.Type";
        private const string SHEET_APM_CUSTOM_EXIT_RULES = "27.APM Custom Exit Rules";
        private const string SHEET_APM_CUSTOM_EXIT_RULES_PIVOT_TYPE = "27.APM Custom Exit Rules.Type";
        private const string SHEET_APM_TIER_SETTINGS = "28.APM Tier Settings";
        private const string SHEET_APM_BUSINESS_TRANSACTION_SETTINGS = "29.APM BT Settings";
        private const string SHEET_APM_AGENT_CONFIGURATION_PROPERTIES = "30.APM Agent Properties";
        private const string SHEET_APM_AGENT_CONFIGURATION_PROPERTIES_PIVOT_TYPE = "30.APM Agent Properties.Type";
        private const string SHEET_APM_INFORMATION_POINT_RULES = "31.APM Information Points";
        private const string SHEET_APM_METHOD_INVOCATION_DATA_COLLECTORS = "32.APM MIDCs";
        private const string SHEET_APM_HTTP_DATA_COLLECTORS = "33.APM HTTP DCs";
        private const string SHEET_APM_AGENT_CALL_GRAPH_SETTINGS = "34.APM Call Graph Settings";
        private const string SHEET_APM_SERVICE_ENDPOINT_DISCOVERY_RULES = "35.APM SEP Discovery Rules";
        private const string SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES = "36.APM SEP Entry Rules";
        private const string SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES_PIVOT_TYPE = "36.APM SEP Entry Rules.Type";
        private const string SHEET_APM_DEVELOPER_MODE_NODES = "37.APM Developer Mode Nodes";
        private const string SHEET_APM_ERROR_DETECTION_RULES = "38.APM Error Detection Rules";
        private const string SHEET_APM_ERROR_DETECTION_IGNORE_MESSAGES = "39.APM Error Ignore Messages";
        private const string SHEET_APM_ERROR_DETECTION_IGNORE_LOGGERS = "40.APM Error Ignore Loggers";
        private const string SHEET_APM_ERROR_DETECTION_LOGGERS = "41.APM Error Loggers";
        private const string SHEET_APM_ERROR_DETECTION_HTTP_CODES = "42.APM Error HTTP Codes";
        private const string SHEET_APM_ERROR_DETECTION_REDIRECT_PAGES = "43.APM Error Redired Pages";

        // DB Configuration
        private const string SHEET_DB_APPLICATION_CONFIGURATION = "50.DB Application Config";
        private const string SHEET_DB_COLLECTOR_DEFINITIONS = "51.DB Collector Definitions";
        private const string SHEET_DB_CUSTOM_METRICS = "52.DB Custom Metrics";

        // WEB Configuration
        private const string SHEET_WEB_APPLICATION_CONFIGURATION = "60.WEB Application Config";
        private const string SHEET_WEB_PAGE_RULES = "61.WEB Page Rules";
        private const string SHEET_WEB_SYNTHETIC_JOBS = "62.WEB Synthetic Jobs";

        // MOBILE Configuration
        private const string SHEET_MOBILE_APPLICATION_CONFIGURATION = "70.MOBILE Application Config";
        private const string SHEET_MOBILE_NETWORK_REQUEST_RULES = "71.MOBILE Network Req Rules";

        // Configuration Differences
        private const string SHEET_CONFIGURATION_DIFFERENCES = "100.Config Differences";
        private const string SHEET_CONFIGURATION_DIFFERENCES_PIVOT = "100.Config Differences.Type";

        // --------------------------------------------------
        // Tables

        private const string TABLE_CONTROLLERS = "t_Controllers";

        // Controller wide settings
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_CONTROLLER_SETTINGS = "t_ControllerSettings";

        // Alert and Response components
        private const string TABLE_CONTROLLER_EMAIL_ALERT_TEMPLATES = "t_ALERT_EmailAlertTemplates";
        private const string TABLE_CONTROLLER_HTTP_ALERT_TEMPLATES = "t_ALERT_HttpAlertTemplates";
        private const string TABLE_APP_HEALTH_RULES_SUMMARY = "t_ALERT_HealthRulesSummary";
        private const string TABLE_APP_POLICIES = "t_ALERT_Policies";
        private const string TABLE_APP_ACTIONS = "t_ALERT_Actions";
        private const string TABLE_APP_POLICIES_TO_ACTIONS_MAPPING = "t_ALERT_PolicyToActionsMapping";

        // APM Configuration
        private const string TABLE_APM_APPLICATION_CONFIGURATION = "t_APM_ApplicationConfiguration";
        private const string TABLE_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES = "t_APM_BTDiscoveryRules";
        private const string TABLE_APM_BUSINESS_TRANSACTION_ENTRY_RULES = "t_APM_BTEntryRules";
        private const string TABLE_APM_BUSINESS_TRANSACTION_SCOPES = "t_APM_BTScopes";
        private const string TABLE_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES_20 = "t_APM_BTDiscoveryRules20";
        private const string TABLE_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20 = "t_APM_BTEntryRules20";
        private const string TABLE_APM_BACKEND_DISCOVERY_RULES = "t_APM_BackendDiscoveryRules";
        private const string TABLE_APM_CUSTOM_EXIT_RULES = "t_APM_CustomExitRules";
        private const string TABLE_APM_AGENT_CONFIGURATION_PROPERTIES = "t_APM_AgentProperties";
        private const string TABLE_APM_INFORMATION_POINT_RULES = "t_APM_InformationPointRules";
        private const string TABLE_APM_METHOD_INVOCATION_DATA_COLLECTORS = "t_APM_MIDCs";
        private const string TABLE_APM_HTTP_DATA_COLLECTORS = "t_APM_HTTPDCs";
        private const string TABLE_APM_HEALTH_RULES = "t_APM_HealthRules";
        private const string TABLE_APM_TIER_SETTINGS = "t_APM_Tiers";
        private const string TABLE_APM_BUSINESS_TRANSACTION_SETTINGS = "t_APM_BusinessTransactions";
        private const string TABLE_APM_AGENT_CALL_GRAPH_SETTINGS = "t_APM_AgentCallGraphSettings";
        private const string TABLE_APM_SERVICE_ENDPOINT_DISCOVERY_RULES = "t_APM_SEPDiscoveryRules";
        private const string TABLE_APM_SERVICE_ENDPOINT_ENTRY_RULES = "t_APM_SEPEntryRules";
        private const string TABLE_APM_DEVELOPER_MODE_NODES = "t_APM_DevModeNodes";
        private const string TABLE_APM_ERROR_DETECTION_RULES = "t_APM_ErrorDetectionRules";
        private const string TABLE_APM_ERROR_DETECTION_IGNORE_MESSAGES = "t_APM_ErrorIgnoreMessages";
        private const string TABLE_APM_ERROR_DETECTION_IGNORE_LOGGERS = "t_APM_ErrorIgnoreLoggers";
        private const string TABLE_APM_ERROR_DETECTION_LOGGERS = "t_APM_ErrorLoggers";
        private const string TABLE_APM_ERROR_DETECTION_HTTP_CODES = "t_APM_ErrorHTTPCodes";
        private const string TABLE_APM_ERROR_DETECTION_REDIRECT_PAGES = "t_APM_ErrorRediredPages";
        
        // DB Configuration
        private const string TABLE_DB_APPLICATION_CONFIGURATION = "t_DB_ApplicationConfiguration";
        private const string TABLE_DB_COLLECTOR_DEFINITIONS = "t_DB_CollectorDefinitions";
        private const string TABLE_DB_CUSTOM_METRICS = "t_DB_CustomMetrics";

        // WEB Configuration
        private const string TABLE_WEB_APPLICATION_CONFIGURATION = "t_WEB_ApplicationConfiguration";
        private const string TABLE_WEB_PAGE_RULES = "t_WEB_PageRules";
        private const string TABLE_WEB_SYNTHETIC_JOBS = "t_WEB_SyntheticJobs";

        // MOBILE Configuration
        private const string TABLE_MOBILE_APPLICATION_CONFIGURATION = "t_MOBILE_ApplicationConfiguration";
        private const string TABLE_MOBILE_NETWORK_REQUEST_RULES = "t_MOBILE_NetworkRequestRules";

        // Configuration Differences
        private const string TABLE_APM_CONFIGURATION_DIFFERENCES = "t_APM_ConfigurationDifferrences";

        // --------------------------------------------------
        // Pivots

        // Alert and Response components
        private const string PIVOT_APP_HEALTH_RULES_TYPE = "p_ALERT_HealthRules";
        private const string PIVOT_APP_POLICIES_TYPE = "p_ALERT_Policies";
        private const string PIVOT_APP_ACTIONS_TYPE = "p_ALERT_Actions";

        // APM Configuration
        private const string PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_TYPE = "p_APM_BTEntryRulesType";
        private const string PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_LOCATION = "p_APM_BTEntryRulesLocation";
        private const string PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_TYPE = "p_APM_BTEntryRules20Type";
        private const string PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_LOCATION = "p_APM_BTEntryRules20Location";
        private const string PIVOT_APM_SERVICE_ENDPOINT_ENTRY_RULES_TYPE = "p_APM_SEPEntryRulesType";
        private const string PIVOT_APM_BACKEND_DISCOVERY_RULES_TYPE = "p_APM_BackendDiscoveryRulesType";
        private const string PIVOT_APM_CUSTOM_EXIT_RULES_TYPE = "p_APM_CustomExitRulesType";
        private const string PIVOT_APM_AGENT_CONFIGURATION_PROPERTIES_TYPE = "p_APM_AgentPropertiesType";

        // Configuration Differences
        private const string PIVOT_CONFIGURATION_DIFFERENCES = "p_ConfigurationDifferrences";

        // --------------------------------------------------
        // Graphs

        // Alert and Response components
        private const string GRAPH_APP_HEALTH_RULES_TYPE = "g_ALERT_HealthRules";
        private const string GRAPH_APP_POLICIES_TYPE = "g_ALERT_Policies";
        private const string GRAPH_APP_ACTIONS_TYPE = "g_ALERT_Actions";

        // APM Configuration
        private const string GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_TYPE = "g_APM_BTEntryRulesType";
        private const string GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_LOCATION = "g_APM_BTEntryRulesLocation";
        private const string GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_TYPE = "g_APM_BTEntryRules20Type";
        private const string GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_LOCATION = "g_APM_BTEntryRules20Location";
        private const string GRAPH_APM_SERVICE_ENDPOINT_ENTRY_RULES_TYPE = "g_APM_SEPEntryRulesType";
        private const string GRAPH_APM_BACKEND_DISCOVERY_RULES_TYPE = "g_APM_BackendDiscoveryRulesType";
        private const string GRAPH_APM_CUSTOM_EXIT_RULES_TYPE = "g_APM_CustomExitRulesType";
        private const string GRAPH_APM_AGENT_CONFIGURATION_PROPERTIES_TYPE = "g_APM_AgentPropertiesType";

        // Configuration Differences
        private const string GRAPH_CONFIGURATION_DIFFERENCES = "g_ConfigurationDifferrences";


        private const int LIST_SHEET_START_TABLE_AT = 4;
        private const int PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int PIVOT_SHEET_CHART_HEIGHT = 14;

        #endregion

        public override bool Execute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            StepTiming stepTimingFunction = new StepTiming();
            stepTimingFunction.JobFileName = programOptions.OutputJobFilePath;
            stepTimingFunction.StepName = jobConfiguration.Status.ToString();
            stepTimingFunction.StepID = (int)jobConfiguration.Status;
            stepTimingFunction.StartTime = DateTime.Now;
            stepTimingFunction.NumEntities = jobConfiguration.Target.Count;

            this.DisplayJobStepStartingStatus(jobConfiguration);

            FilePathMap = new FilePathMap(programOptions, jobConfiguration);

            if (this.ShouldExecute(programOptions, jobConfiguration) == false)
            {
                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Controller and Application Configuration Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Controller and Application Configuration Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Controller and Application Configuration Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONTROLLERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_ALL_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONTROLLER_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONTROLLER_EMAIL_ALERT_TEMPLATES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONTROLLER_HTTP_ALERT_TEMPLATES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_HEALTH_RULES_SUMMARY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_HEALTH_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Health Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APP_HEALTH_RULES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_HEALTH_RULES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APP_HEALTH_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_POLICIES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Policies";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APP_POLICIES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_POLICIES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APP_POLICIES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_ACTIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Actions";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APP_ACTIONS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_ACTIONS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APP_ACTIONS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_POLICIES_TO_ACTIONS_MAPPING);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_APPLICATION_CONFIGURATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BT Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations BT Rules";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_SCOPES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES_20);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BT Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations BT Rules";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BACKEND_DISCOVERY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Backend Detection Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BACKEND_DISCOVERY_RULES_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BACKEND_DISCOVERY_RULES_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_BACKEND_DISCOVERY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_CUSTOM_EXIT_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Custom Exit Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_CUSTOM_EXIT_RULES_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_CUSTOM_EXIT_RULES_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_CUSTOM_EXIT_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_TIER_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_BUSINESS_TRANSACTION_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_AGENT_CONFIGURATION_PROPERTIES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Agent Configuration Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_AGENT_CONFIGURATION_PROPERTIES_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_AGENT_CONFIGURATION_PROPERTIES_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_AGENT_CONFIGURATION_PROPERTIES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_INFORMATION_POINT_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_METHOD_INVOCATION_DATA_COLLECTORS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_HTTP_DATA_COLLECTORS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_AGENT_CALL_GRAPH_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_SERVICE_ENDPOINT_DISCOVERY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_DEVELOPER_MODE_NODES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_ERROR_DETECTION_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_ERROR_DETECTION_IGNORE_MESSAGES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_ERROR_DETECTION_IGNORE_LOGGERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_ERROR_DETECTION_LOGGERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_ERROR_DETECTION_HTTP_CODES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APM_ERROR_DETECTION_REDIRECT_PAGES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DB_APPLICATION_CONFIGURATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DB_COLLECTOR_DEFINITIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DB_CUSTOM_METRICS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WEB_APPLICATION_CONFIGURATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WEB_PAGE_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WEB_SYNTHETIC_JOBS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MOBILE_APPLICATION_CONFIGURATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MOBILE_NETWORK_REQUEST_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONFIGURATION_DIFFERENCES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Differences";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_CONFIGURATION_DIFFERENCES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONFIGURATION_DIFFERENCES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_CONFIGURATION_DIFFERENCES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                #endregion

                loggerConsole.Info("Fill Controller and Application Configuration Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications - All

                loggerConsole.Info("List of Applications - All");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ALL_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerApplicationsReportFilePath(), 0, typeof(ControllerApplication), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Controller Settings

                loggerConsole.Info("List of Controller Settings");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLER_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSettingsReportFilePath(), 0, typeof(ControllerSetting), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Email Alert Templates

                loggerConsole.Info("List of Email Alert Templates");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLER_EMAIL_ALERT_TEMPLATES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EmailTemplatesReportFilePath(), 0, typeof(EmailAlertTemplate), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region HTTP Alert Templates

                loggerConsole.Info("List of HTTP Alert Templates");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLER_HTTP_ALERT_TEMPLATES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.HTTPTemplatesReportFilePath(), 0, typeof(HTTPAlertTemplate), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Application Health Rules Summary

                loggerConsole.Info("List of Application Health Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTH_RULES_SUMMARY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationConfigurationHealthRulesReportFilePath(), 0, typeof(ApplicationConfigurationPolicy), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Health Rules

                loggerConsole.Info("List of Health Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTH_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationHealthRulesReportFilePath(), 0, typeof(HealthRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Policies

                loggerConsole.Info("List of Policies");

                sheet = excelReport.Workbook.Worksheets[SHEET_APP_POLICIES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationPoliciesReportFilePath(), 0, typeof(Policy), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Actions

                loggerConsole.Info("List of Actions");

                sheet = excelReport.Workbook.Worksheets[SHEET_APP_ACTIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationActionsReportFilePath(), 0, typeof(ReportObjects.Action), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Policy and Action Mappings

                loggerConsole.Info("List of Policy and Action Mappings");

                sheet = excelReport.Workbook.Worksheets[SHEET_APP_POLICIES_TO_ACTIONS_MAPPING];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationPolicyActionMappingsReportFilePath(), 0, typeof(PolicyActionMapping), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Application Configuration

                loggerConsole.Info("List of APM Application Config");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_APPLICATION_CONFIGURATION];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMApplicationConfigurationReportFilePath(), 0, typeof(APMApplicationConfiguration), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Business Transaction Detection Rules

                loggerConsole.Info("List of APM Business Transaction Detection Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionDiscoveryRulesReportFilePath(), 0, typeof(BusinessTransactionDiscoveryRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Business Transaction Entry Rules

                loggerConsole.Info("List of APM Business Transaction Entry Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionEntryRulesReportFilePath(), 0, typeof(BusinessTransactionEntryRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Service Endpoint Discovery Rules

                loggerConsole.Info("List of APM Service Endpoint Discovery Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_SERVICE_ENDPOINT_DISCOVERY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMServiceEndpointDiscoveryRulesReportFilePath(), 0, typeof(ServiceEndpointDiscoveryRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Service Endpoint Entry Rules

                loggerConsole.Info("List of APM Service Endpoint Entry Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMServiceEndpointEntryRulesReportFilePath(), 0, typeof(ServiceEndpointEntryRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Business Transaction Scopes

                loggerConsole.Info("List of APM Business Transaction Scopes");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_SCOPES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionEntryScopesReportFilePath(), 0, typeof(BusinessTransactionEntryScope), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Business Transaction Detection Rules 2.0

                loggerConsole.Info("List of APM Business Transaction 2.0 Detection Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES_20];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionDiscoveryRules20ReportFilePath(), 0, typeof(BusinessTransactionDiscoveryRule20), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Business Transaction Entry Rules 2.0

                loggerConsole.Info("List of APM Business Transaction 2.0 Entry Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionEntryRules20ReportFilePath(), 0, typeof(BusinessTransactionEntryRule20), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Backend Discovery Rules

                loggerConsole.Info("List of APM Backend Detection Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BACKEND_DISCOVERY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBackendDiscoveryRulesReportFilePath(), 0, typeof(BackendDiscoveryRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Custom Exit Rules

                loggerConsole.Info("List of APM Custom Exit Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_CUSTOM_EXIT_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMCustomExitRulesReportFilePath(), 0, typeof(CustomExitRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Information Point Rules

                loggerConsole.Info("List of APM Information Point Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_INFORMATION_POINT_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMInformationPointRulesReportFilePath(), 0, typeof(InformationPointRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Agent Configuration Properties

                loggerConsole.Info("List of APM Agent Configuration Properties");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_AGENT_CONFIGURATION_PROPERTIES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMAgentConfigurationPropertiesReportFilePath(), 0, typeof(AgentConfigurationProperty), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Data Collectors

                loggerConsole.Info("List of APM Method Invocation Data Collectors");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_METHOD_INVOCATION_DATA_COLLECTORS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMMethodInvocationDataCollectorsReportFilePath(), 0, typeof(MethodInvocationDataCollector), sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of HTTP Data Collectors");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_HTTP_DATA_COLLECTORS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMHttpDataCollectorsReportFilePath(), 0, typeof(HTTPDataCollector), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Tier Settings

                loggerConsole.Info("List of APM Tier Settings");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_TIER_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMTierConfigurationsReportFilePath(), 0, typeof(TierConfiguration), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Business Transaction Configurations

                loggerConsole.Info("List of APM Business Transaction Configurations");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionConfigurationsReportFilePath(), 0, typeof(BusinessTransactionConfiguration), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Agent Call Graph Settings

                loggerConsole.Info("List of APM Agent Call Graph Settings");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_AGENT_CALL_GRAPH_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMAgentCallGraphSettingsReportFilePath(), 0, typeof(AgentCallGraphSetting), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Developer Mode Nodes

                loggerConsole.Info("List of APM Developer Mode Nodes");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_DEVELOPER_MODE_NODES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMDeveloperModeNodesReportFilePath(), 0, typeof(DeveloperModeNode), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region APM Error Settings

                loggerConsole.Info("List of Error Detection Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorDetectionRulesReportFilePath(), 0, typeof(ErrorDetectionRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Error Detection Ignore Messages");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_IGNORE_MESSAGES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorDetectionIgnoreMessagesReportFilePath(), 0, typeof(ErrorDetectionIgnoreMessage), sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Error Detection Ignore Loggers");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_IGNORE_LOGGERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorDetectionIgnoreLoggersReportFilePath(), 0, typeof(ErrorDetectionIgnoreLogger), sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Error Detection Loggers");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_LOGGERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorDetectionLoggersReportFilePath(), 0, typeof(ErrorDetectionLogger), sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Error Detection HTTP Codes");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_HTTP_CODES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorDetectionHTTPCodesReportFilePath(), 0, typeof(ErrorDetectionHTTPCode), sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Error Detection Redirect Pages");

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_REDIRECT_PAGES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorDetectionRedirectPagesReportFilePath(), 0, typeof(ErrorDetectionRedirectPage), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region DB Application Configuration

                loggerConsole.Info("List of DB Application Config");

                sheet = excelReport.Workbook.Worksheets[SHEET_DB_APPLICATION_CONFIGURATION];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBApplicationConfigurationReportFilePath(), 0, typeof(DBApplicationConfiguration), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region DB Collector Definitions

                loggerConsole.Info("List of DB Collector Definitions");

                sheet = excelReport.Workbook.Worksheets[SHEET_DB_COLLECTOR_DEFINITIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBCollectorDefinitionsReportFilePath(), 0, typeof(DBCollectorDefinition), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region DB Custom Metrics

                loggerConsole.Info("List of DB Custom Metrics");

                sheet = excelReport.Workbook.Worksheets[SHEET_DB_CUSTOM_METRICS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBCustomMetricsReportFilePath(), 0, typeof(DBCustomMetric), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region WEB Application Configuration

                loggerConsole.Info("List of WEB Application Config");

                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_APPLICATION_CONFIGURATION];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBApplicationConfigurationReportFilePath(), 0, typeof(WEBApplicationConfiguration), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region WEB Pages and AJAX Request Rules

                loggerConsole.Info("List of WEB Pages and AJAX Request Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_PAGE_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBPageAjaxVirtualPageRulesReportFilePath(), 0, typeof(WEBPageDetectionRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region WEB Synthetic Jobs

                loggerConsole.Info("List of WEB Synthetic Jobs");

                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_SYNTHETIC_JOBS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBSyntheticJobsReportFilePath(), 0, typeof(WEBSyntheticJobDefinition), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region MOBILE Application Configuration

                loggerConsole.Info("List of MOBILE Application Config");

                sheet = excelReport.Workbook.Worksheets[SHEET_MOBILE_APPLICATION_CONFIGURATION];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.MOBILEApplicationConfigurationReportFilePath(), 0, typeof(MOBILEApplicationConfiguration), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region MOBILE Network Request Rules

                loggerConsole.Info("List of MOBILE Network Request Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_MOBILE_NETWORK_REQUEST_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.MOBILENetworkRequestRulesReportFilePath(), 0, typeof(MOBILENetworkRequestRule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Configuration Differences

                loggerConsole.Info("List of Configuration Differences");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONFIGURATION_DIFFERENCES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ConfigurationComparisonReportFilePath(), 0, typeof(ConfigurationDifference), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Controller and Application Configuration Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["Version"].Position + 1).Width = 15;
                }

                #endregion

                #region Applications - All

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ALL_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_ALL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Description"].Position + 1).Width = 15;

                    sheet.Column(table.Columns["CreatedBy"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["UpdatedBy"].Position + 1).Width = 15;

                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOnUtc"].Position + 1).Width = 20;
                }

                #endregion

                #region Controller Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLER_SETTINGS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_CONTROLLER_SETTINGS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Name"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["Value"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Description"].Position + 1).Width = 30;
                }

                #endregion

                #region Email Alert Templates

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLER_EMAIL_ALERT_TEMPLATES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_CONTROLLER_EMAIL_ALERT_TEMPLATES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Name"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TestTo"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Subject"].Position + 1).Width = 20;
                }

                #endregion

                #region HTTP Alert Templates

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLER_HTTP_ALERT_TEMPLATES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_CONTROLLER_HTTP_ALERT_TEMPLATES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Name"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Path"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Query"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Headers"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ContentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Payload"].Position + 1).Width = 20;
                }

                #endregion

                #region Application Health Rules Summary

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTH_RULES_SUMMARY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APP_HEALTH_RULES_SUMMARY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHealthRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHealthRules"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumPolicies"].Position + 1, sheet.Dimension.Rows, table.Columns["NumPolicies"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumActions"].Position + 1, sheet.Dimension.Rows, table.Columns["NumActions"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Health Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTH_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_HEALTH_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["HRRuleType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AffectsEntityType"].Position + 1).Width = 15;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTH_RULES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APP_HEALTH_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsDefault");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsAlwaysEnabled");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "HRRuleType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APP_HEALTH_RULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Policies

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APP_POLICIES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APP_POLICIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PolicyName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["PolicyType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Actions"].Position + 1).Width = 30;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APP_POLICIES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1], range, PIVOT_APP_POLICIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsBatchActionsPerMinute");
                    addFilterFieldToPivot(pivot, "NumActions", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumHRs", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "PolicyName");
                    addRowFieldToPivot(pivot, "Actions");
                    addColumnFieldToPivot(pivot, "PolicyType");
                    addDataFieldToPivot(pivot, "PolicyID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APP_POLICIES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Actions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APP_ACTIONS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APP_ACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ActionName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ActionType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ActionTemplate"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APP_ACTIONS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APP_ACTIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Priority");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "ActionName");
                    addRowFieldToPivot(pivot, "To");
                    addColumnFieldToPivot(pivot, "ActionType");
                    addDataFieldToPivot(pivot, "ActionID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APP_ACTIONS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Policy and Action Mappings

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APP_POLICIES_TO_ACTIONS_MAPPING];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APP_POLICIES_TO_ACTIONS_MAPPING);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PolicyName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["PolicyType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ActionName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ActionType"].Position + 1).Width = 15;
                }

                #endregion

                #region APM Application Configuration

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_APPLICATION_CONFIGURATION];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_APPLICATION_CONFIGURATION);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTDiscoveryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTDiscoveryRules"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTEntryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTEntryRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTExcludeRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTExcludeRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20Scopes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20Scopes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20DiscoveryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20DiscoveryRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20EntryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20EntryRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20ExcludeRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20ExcludeRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBackendRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBackendRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumInfoPointRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumInfoPointRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumAgentProps"].Position + 1, sheet.Dimension.Rows, table.Columns["NumAgentProps"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHealthRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHealthRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCVariablesCollected"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCVariablesCollected"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCVariablesCollected"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCVariablesCollected"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBaselines"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBaselines"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTiers"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region APM Business Transaction Detection Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["NamingConfigType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DiscoveryType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleRawValue"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Business Transaction Entry Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BUSINESS_TRANSACTION_ENTRY_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleRawValue"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsBuiltIn");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsBuiltIn");
                    addRowFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count, "Rules");
                    addDataFieldToPivot(pivot, "NumDetectedBTs", DataFieldFunctions.Count, "BTs");

                    chart = sheet.Drawings.AddChart(GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_LOCATION, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                }

                #endregion

                #region APM Service Endpoint Discovery Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_SERVICE_ENDPOINT_DISCOVERY_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_SERVICE_ENDPOINT_DISCOVERY_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 15;
                }

                #endregion

                #region APM Service Endpoint Entry Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_SERVICE_ENDPOINT_ENTRY_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 15;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_SERVICE_ENDPOINT_ENTRY_RULES_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APM_SERVICE_ENDPOINT_ENTRY_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APM_SERVICE_ENDPOINT_ENTRY_RULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region APM Business Transaction Scopes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_SCOPES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BUSINESS_TRANSACTION_SCOPES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ScopeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ScopeType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["AffectedTiers"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["IncludedRules"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Business Transaction Detection Rules 2.0

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES_20];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BUSINESS_TRANSACTION_DISCOVERY_RULES_20);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ScopeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NamingConfigType"].Position + 1).Width = 15;
                }

                #endregion

                #region APM Business Transaction Entry Rules 2.0

                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ScopeName"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsBuiltIn");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "ScopeName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsBuiltIn");
                    addRowFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ScopeName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count, "Rules");
                    addDataFieldToPivot(pivot, "NumDetectedBTs", DataFieldFunctions.Count, "BTs");

                    chart = sheet.Drawings.AddChart(GRAPH_APM_BUSINESS_TRANSACTION_ENTRY_RULES_20_LOCATION, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                }

                #endregion

                #region APM Backend Discovery Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BACKEND_DISCOVERY_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BACKEND_DISCOVERY_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["ExitType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleRawValue"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_BACKEND_DISCOVERY_RULES_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_APM_BACKEND_DISCOVERY_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "ExitType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APM_BACKEND_DISCOVERY_RULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region APM Custom Exit Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_CUSTOM_EXIT_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_CUSTOM_EXIT_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["ExitType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleRawValue"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_CUSTOM_EXIT_RULES_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_APM_CUSTOM_EXIT_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "ExitType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APM_CUSTOM_EXIT_RULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region APM Information Point Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_INFORMATION_POINT_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_INFORMATION_POINT_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleRawValue"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Agent Configuration Properties

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_AGENT_CONFIGURATION_PROPERTIES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_AGENT_CONFIGURATION_PROPERTIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["PropertyName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PropertyName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["StringValue"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["IntegerValue"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["BooleanValue"].Position + 1).Width = 15;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_APM_AGENT_CONFIGURATION_PROPERTIES_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_APM_AGENT_CONFIGURATION_PROPERTIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsDefault");
                    addFilterFieldToPivot(pivot, "IsBuiltIn");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "PropertyName");
                    addColumnFieldToPivot(pivot, "PropertyType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "PropertyName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_APM_AGENT_CONFIGURATION_PROPERTIES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region APM Data Collectors

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_METHOD_INVOCATION_DATA_COLLECTORS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_METHOD_INVOCATION_DATA_COLLECTORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["MatchClass"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MatchMethod"].Position + 1).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_HTTP_DATA_COLLECTORS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_HTTP_DATA_COLLECTORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["DataGathererName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DataGathererValue"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Tier Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_TIER_SETTINGS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_TIER_SETTINGS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierType"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Business Transaction Configurations

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_BUSINESS_TRANSACTION_SETTINGS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_BUSINESS_TRANSACTION_SETTINGS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AssignedMIDCs"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Agent Call Graph Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_AGENT_CALL_GRAPH_SETTINGS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_AGENT_CALL_GRAPH_SETTINGS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;

                }

                #endregion

                #region APM Developer Mode Nodes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_DEVELOPER_MODE_NODES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_DEVELOPER_MODE_NODES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                }

                #endregion

                #region APM Error Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_ERROR_DETECTION_IGNORE_MESSAGES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["RuleValue"].Position + 1).Width = 15;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_IGNORE_MESSAGES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_ERROR_DETECTION_IGNORE_LOGGERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ExceptionClass"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MatchType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["MessagePattern"].Position + 1).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_IGNORE_LOGGERS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_ERROR_DETECTION_LOGGERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LoggerName"].Position + 1).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_LOGGERS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_ERROR_DETECTION_HTTP_CODES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LoggerName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MatchClass"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["MatchMethod"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["MatchType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["MatchParameterTypes"].Position + 1).Width = 15;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_HTTP_CODES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_ERROR_DETECTION_REDIRECT_PAGES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RangeName"].Position + 1).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APM_ERROR_DETECTION_REDIRECT_PAGES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_ERROR_DETECTION_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PageName"].Position + 1).Width = 20;
                }

                #endregion

                #region DB Application Configuration

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_DB_APPLICATION_CONFIGURATION];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DB_APPLICATION_CONFIGURATION);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCollectorDefinitions"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCollectorDefinitions"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCustomMetrics"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCustomMetrics"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region DB Collector Definitions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_DB_COLLECTOR_DEFINITIONS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DB_COLLECTOR_DEFINITIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ModifiedOn"].Position + 1).Width = 20;
                }

                #endregion

                #region DB Custom Metrics

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_DB_CUSTOM_METRICS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DB_CUSTOM_METRICS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MetricName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Query"].Position + 1).Width = 25;
                }

                #endregion

                #region WEB Application Configuration

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_APPLICATION_CONFIGURATION];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_WEB_APPLICATION_CONFIGURATION);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationKey"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentHTTP"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentHTTPS"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumPageRulesInclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumPageRulesInclude"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumPageRulesExclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumPageRulesExclude"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumVirtPageRulesInclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumVirtPageRulesInclude"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumVirtPageRulesExclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumVirtPageRulesExclude"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumAJAXRulesInclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumAJAXRulesInclude"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumAJAXRulesExclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumAJAXRulesExclude"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSyntheticJobs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSyntheticJobs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region WEB Pages and AJAX Request Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_PAGE_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_WEB_PAGE_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MatchURL"].Position + 1).Width = 25;
                }

                #endregion

                #region WEB Synthetic Jobs

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_SYNTHETIC_JOBS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_WEB_SYNTHETIC_JOBS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["JobName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Days"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Browsers"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Locations"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["URL"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOnUtc"].Position + 1).Width = 20;
                }

                #endregion

                #region MOBILE Application Configuration

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MOBILE_APPLICATION_CONFIGURATION];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MOBILE_APPLICATION_CONFIGURATION);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationKey"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNetworkRulesInclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNetworkRulesInclude"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNetworkRulesExclude"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNetworkRulesExclude"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region MOBILE Network Request Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MOBILE_NETWORK_REQUEST_RULES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MOBILE_NETWORK_REQUEST_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UrlSegments"].Position + 1).Width = 25;
                }

                #endregion

                #region Configuration Differences

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONFIGURATION_DIFFERENCES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APM_CONFIGURATION_DIFFERENCES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["RuleType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RuleSubType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["Property"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ReferenceApp"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DifferenceApp"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Difference"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Property"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["ReferenceValue"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DifferenceValue"].Position + 1).Width = 20;

                    ExcelAddress cfAddressDifference = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Difference"].Position + 1, sheet.Dimension.Rows, table.Columns["Difference"].Position + 1);
                    addDifferenceConditionalFormatting(sheet, cfAddressDifference);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_CONFIGURATION_DIFFERENCES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_CONFIGURATION_DIFFERENCES);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ReferenceApp");
                    addRowFieldToPivot(pivot, "RuleType");
                    addRowFieldToPivot(pivot, "RuleSubType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "EntityName");
                    addRowFieldToPivot(pivot, "Property");
                    addColumnFieldToPivot(pivot, "DifferenceController", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "DifferenceApp", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Difference", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EntityName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_CONFIGURATION_DIFFERENCES, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.ConfigurationExcelReportFilePath(jobConfiguration.Input.TimeRange);
                logger.Info("Saving Excel report {0}", reportFilePath);
                loggerConsole.Info("Saving Excel report {0}", reportFilePath);

                try
                {
                    // Save full report Excel files
                    excelReport.SaveAs(new FileInfo(reportFilePath));
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warn("Unable to save Excel file {0}", reportFilePath);
                    logger.Warn(ex);
                    loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);
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

                this.DisplayJobStepEndedStatus(jobConfiguration, stopWatch);

                stepTimingFunction.EndTime = DateTime.Now;
                stepTimingFunction.Duration = stopWatch.Elapsed;
                stepTimingFunction.DurationMS = stopWatch.ElapsedMilliseconds;

                List<StepTiming> stepTimings = new List<StepTiming>(1);
                stepTimings.Add(stepTimingFunction);
                FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
            }
        }

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.Configuration={0}", programOptions.LicensedReports.Configuration);
            loggerConsole.Trace("LicensedReports.Configuration={0}", programOptions.LicensedReports.Configuration);
            if (programOptions.LicensedReports.Configuration == false)
            {
                loggerConsole.Warn("Not licensed for configuration");
                return false;
            }

            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            logger.Trace("Output.Configuration={0}", jobConfiguration.Output.Configuration);
            loggerConsole.Trace("Output.Configuration={0}", jobConfiguration.Output.Configuration);
            if (jobConfiguration.Input.Configuration == false || jobConfiguration.Output.Configuration == false)
            {
                loggerConsole.Trace("Skipping report of configuration");
            }
            return (jobConfiguration.Input.Configuration == true && jobConfiguration.Output.Configuration == true);
        }
    }
}
