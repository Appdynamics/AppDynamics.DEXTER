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
using System.Reflection;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportControllerAndApplicationConfiguration : JobStepReportBase
    {
        #region Constants for Configuration Report contents

        private const string REPORT_CONFIGURATION_SHEET_CONTROLLERS = "3.Controllers";

        private const string REPORT_CONFIGURATION_SHEET_CONTROLLER_SETTINGS = "4.Controller Settings";
        private const string REPORT_CONFIGURATION_SHEET_APPLICATION_CONFIGURATION = "5.Application Configuration";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES = "6.BT Discovery Rules";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES = "7.BT Entry Rules";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE = "7.BT Entry Rules.Type";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION = "7.BT Entry Rules.Location";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SCOPES = "8.BT Scopes";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES_20 = "9.BT Discovery Rules 2.0";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20 = "10.BT Entry Rules 2.0";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE = "10.BT Entry Rules 2.0.Type";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION = "10.BT Entry Rules 2.0.Location";
        private const string REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES = "11.Backend Discovery Rules";
        private const string REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES_PIVOT = "11.Backend Discovery Rules.Type";
        private const string REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES = "12.Custom Exit Rules";
        private const string REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES_PIVOT = "12.Custom Exit Rules.Type";
        private const string REPORT_CONFIGURATION_SHEET_HEALTH_RULES = "13.Health Rules";
        private const string REPORT_CONFIGURATION_SHEET_HEALTH_RULES_PIVOT = "13.Health Rules.Type";
        private const string REPORT_CONFIGURATION_SHEET_TIER_SETTINGS = "14.Tier Settings";
        private const string REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SETTINGS = "15.BT Settings";
        private const string REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES = "16.Agent Properties";
        private const string REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES_PIVOT = "16.Agent Properties.Type";
        private const string REPORT_CONFIGURATION_SHEET_INFORMATION_POINT_RULES = "17.Information Points";
        private const string REPORT_CONFIGURATION_SHEET_METHOD_INVOCATION_DATA_COLLECTORS = "18.MIDCs";
        private const string REPORT_CONFIGURATION_SHEET_HTTP_DATA_COLLECTORS = "19.HTTP DCs";
        private const string REPORT_CONFIGURATION_SHEET_AGENT_CALL_GRAPH_SETTINGS = "20.Call Graph Settings";
        private const string REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES = "30.Config Differences";
        private const string REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES_PIVOT = "30.Config Differences.Type";

        private const string REPORT_CONFIGURATION_DETAILS_TABLE_TOC = "t_TOC";
        private const string REPORT_CONFIGURATION_DETAILS_TABLE_CONTROLLERS = "t_Controllers";

        // Full and hourly metric data
        private const string REPORT_CONFIGURATION_TABLE_CONTROLLER_SETTINGS = "t_ControllerSettings";
        private const string REPORT_CONFIGURATION_TABLE_APPLICATION_CONFIGURATION = "t_ApplicationConfiguration";
        private const string REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_DISCOVERY_RULES = "t_BTDiscoveryRules";
        private const string REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_ENTRY_RULES = "t_BTEntryRules";
        private const string REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_SCOPES = "t_BTScopes";
        private const string REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_DISCOVERY_RULES_20 = "t_BTDiscoveryRules20";
        private const string REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_ENTRY_RULES_20 = "t_BTEntryRules20";
        private const string REPORT_CONFIGURATION_TABLE_BACKEND_DISCOVERY_RULES = "t_BackendDiscoveryRules";
        private const string REPORT_CONFIGURATION_TABLE_CUSTOM_EXIT_RULES = "t_CustomExitRules";
        private const string REPORT_CONFIGURATION_TABLE_AGENT_CONFIGURATION_PROPERTIES = "t_AgentProperties";
        private const string REPORT_CONFIGURATION_TABLE_INFORMATION_POINT_RULES = "t_InformationPointRules";
        private const string REPORT_CONFIGURATION_TABLE_METHOD_INVOCATION_DATA_COLLECTORS = "t_MIDCs";
        private const string REPORT_CONFIGURATION_TABLE_HTTP_DATA_COLLECTORS = "t_HTTPDCs";
        private const string REPORT_CONFIGURATION_TABLE_HEALTH_RULES = "t_HealthRules";
        private const string REPORT_CONFIGURATION_TABLE_TIER_SETTINGS = "t_Tiers";
        private const string REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_SETTINGS = "t_BusinessTransactions";
        private const string REPORT_CONFIGURATION_TABLE_AGENT_CALL_GRAPH_SETTINGS = "t_AgentCallGraphSettings";
        private const string REPORT_CONFIGURATION_TABLE_CONFIGURATION_DIFFERENCES = "t_ConfigurationDifferrences";

        private const string REPORT_CONFIGURATION_PIVOT_BT_RULES_TYPE = "p_BTEntryRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_BT_RULES_LOCATION = "p_BTEntryRulesLocation";
        private const string REPORT_CONFIGURATION_PIVOT_BT_RULES_20_TYPE = "p_BTEntryRules20Type";
        private const string REPORT_CONFIGURATION_PIVOT_BT_RULES_20_LOCATION = "p_BTEntryRules20Location";
        private const string REPORT_CONFIGURATION_PIVOT_BACKEND_DISCOVERY_RULES_TYPE = "p_BackendDiscoveryRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_CUSTOM_EXIT_RULES_TYPE = "p_CustomExitRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_AGENT_CONFIGURATION_PROPERTIES_TYPE = "p_AgentPropertiesType";
        private const string REPORT_CONFIGURATION_PIVOT_HEALTH_RULES_TYPE = "p_HealthRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_CONFIGURATION_DIFFERENCES = "p_ConfigurationDifferrences";

        private const string REPORT_CONFIGURATION_PIVOT_BT_RULES_TYPE_GRAPH = "g_BTEntryRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_BT_RULES_20_TYPE_GRAPH = "g_BTEntryRules20Type";
        private const string REPORT_CONFIGURATION_PIVOT_BACKEND_DISCOVERY_RULES_TYPE_GRAPH = "g_BackendDiscoveryRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_CUSTOM_EXIT_RULES_TYPE_GRAPH = "g_CustomExitRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_AGENT_CONFIGURATION_PROPERTIES_TYPE_GRAPH = "g_AgentPropertiesType";
        private const string REPORT_CONFIGURATION_PIVOT_HEALTH_RULES_TYPE_GRAPH = "g_HealthRulesType";
        private const string REPORT_CONFIGURATION_PIVOT_CONFIGURATION_DIFFERENCES_GRAPH = "g_ConfigurationDifferrences";

        private const int REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT = 14;

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

            if (this.ShouldExecute(jobConfiguration) == false)
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
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Controller and Application Configuration Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_CONTROLLERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_CONTROLLER_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_APPLICATION_CONFIGURATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BT Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations BT Rules";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SCOPES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES_20);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BT Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations BT Rules";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Backend Detection Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Custom Exit Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_HEALTH_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Health Rules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_HEALTH_RULES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_HEALTH_RULES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_HEALTH_RULES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_TIER_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Agent Configuration Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_INFORMATION_POINT_RULES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_METHOD_INVOCATION_DATA_COLLECTORS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_HTTP_DATA_COLLECTORS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_AGENT_CALL_GRAPH_SETTINGS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Differences";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                #endregion

                loggerConsole.Info("Fill Controller and Application Configuration Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONTROLLERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllersReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Controller Settings

                loggerConsole.Info("List of Controller Settings");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONTROLLER_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSettingsReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Application Configuration

                loggerConsole.Info("List of Application Configuration");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_APPLICATION_CONFIGURATION];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationConfigurationReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transaction Detection Rules

                loggerConsole.Info("List of Business Transaction Detection Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionDiscoveryRulesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transaction Entry Rules

                loggerConsole.Info("List of Business Transaction Entry Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionEntryRulesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transaction Scopes

                loggerConsole.Info("List of Business Transaction Scopes");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SCOPES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionEntryScopesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transaction Detection Rules 2.0

                loggerConsole.Info("List of Business Transaction 2.0 Detection Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES_20];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionDiscoveryRules20ReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transaction Entry Rules 2.0

                loggerConsole.Info("List of Business Transaction 2.0 Entry Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionEntryRules20ReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Backend Discovery Rules

                loggerConsole.Info("List of Backend Detection Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BackendDiscoveryRulesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);
                
                #endregion

                #region Custom Exit Rules

                loggerConsole.Info("List of Custom Exit Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.CustomExitRulesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Information Point Rules

                loggerConsole.Info("List of Information Point Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_INFORMATION_POINT_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.InformationPointRulesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Agent Configuration Properties

                loggerConsole.Info("List of Agent Configuration Properties");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.AgentConfigurationPropertiesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Data Collectors

                loggerConsole.Info("List of Method Invocation Data Collectors");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_METHOD_INVOCATION_DATA_COLLECTORS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.MethodInvocationDataCollectorsReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of HTTP Data Collectors");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_HTTP_DATA_COLLECTORS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.HttpDataCollectorsReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Tier Settings

                loggerConsole.Info("List of Tier Settings");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_TIER_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntityTierConfigurationsReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Detected Business Transaction and Assigned Data Collectors

                loggerConsole.Info("List of Detected Business Transaction and Assigned Data Collectors");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntityBusinessTransactionConfigurationsReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Agent Call Graph Settings

                loggerConsole.Info("List of Agent Call Graph Settings");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_AGENT_CALL_GRAPH_SETTINGS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.AgentCallGraphSettingsReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Health Rules

                loggerConsole.Info("List of Health Rules");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_HEALTH_RULES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.HealthRulesReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Configuration Differences

                loggerConsole.Info("List of Configuration Differences");

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ConfigurationComparisonReportFilePath(), 0, sheet, REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Controller and Application Configuration Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONTROLLERS];
                logger.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_DETAILS_TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["UserName"].Position + 1).Width = 25;
                }

                #endregion

                #region Controller Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONTROLLER_SETTINGS];
                logger.Info("Controller Settings Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Controller Settings Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_CONTROLLER_SETTINGS);
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

                #region Application Configuration

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_APPLICATION_CONFIGURATION];
                logger.Info("Application Configuration Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Application Configuration Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_APPLICATION_CONFIGURATION);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTDiscoveryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTDiscoveryRules"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTEntryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTEntryRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTExcludeRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTExcludeRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20Scopes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20Scopes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20DiscoveryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20DiscoveryRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20EntryRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20EntryRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBT20ExcludeRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBT20ExcludeRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBackendRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBackendRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumInfoPointRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumInfoPointRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumAgentProps"].Position + 1, sheet.Dimension.Rows, table.Columns["NumAgentProps"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHealthRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHealthRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrorRules"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrorRules"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCVariablesCollected"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCVariablesCollected"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCVariablesCollected"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCVariablesCollected"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBaselines"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBaselines"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);


                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTiers"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Business Transaction Detection Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES];
                logger.Info("Business Transaction Detection Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Business Transaction Detection Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_DISCOVERY_RULES);
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

                #region Business Transaction Entry Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES];
                logger.Info("Business Transaction Entry Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Business Transaction Entry Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_ENTRY_RULES);
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
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_CONFIGURATION_PIVOT_BT_RULES_TYPE);
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

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_BT_RULES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_CONFIGURATION_PIVOT_BT_RULES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addRowFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count, "Rules");
                    addDataFieldToPivot(pivot, "NumDetectedBTs", DataFieldFunctions.Count, "BTs");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                }

                #endregion

                #region Business Transaction Scopes

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SCOPES];
                logger.Info("Business Transaction Scopes Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Business Transaction Scopes Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_SCOPES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ScopeName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ScopeType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["IncludedTiers"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["IncludedRules"].Position + 1).Width = 20;
                }

                #endregion

                #region Business Transaction Detection Rules 2.0

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_DISCOVERY_RULES_20];
                logger.Info("Business Transaction 2.0 Detection Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Business Transaction 2.0 Detection Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_DISCOVERY_RULES_20);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ScopeName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["NamingConfigType"].Position + 1).Width = 15;
                }

                #endregion

                #region Business Transaction Entry Rules 2.0

                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20];
                logger.Info("Business Transaction 2.0 Entry Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Business Transaction 2.0 Entry Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_ENTRY_RULES_20);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntryPointType"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["ScopeName"].Position + 1).Width = 30;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_TYPE];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_CONFIGURATION_PIVOT_BT_RULES_20_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "ScopeName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_BT_RULES_20_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_ENTRY_RULES_20_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_CONFIGURATION_PIVOT_BT_RULES_20_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExclusion");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addRowFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "EntryPointType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ScopeName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count, "Rules");
                    addDataFieldToPivot(pivot, "NumDetectedBTs", DataFieldFunctions.Count, "BTs");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                }

                #endregion

                #region Backend Discovery Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES];
                logger.Info("Backend Discovery Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Backend Discovery Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BACKEND_DISCOVERY_RULES);
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
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BACKEND_DISCOVERY_ENTRY_RULES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_CONFIGURATION_PIVOT_BACKEND_DISCOVERY_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "ExitType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_BACKEND_DISCOVERY_RULES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Custom Exit Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES];
                logger.Info("Custom Exit Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Custom Exit Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_CUSTOM_EXIT_RULES);
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
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CUSTOM_EXIT_RULES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_CONFIGURATION_PIVOT_CUSTOM_EXIT_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "ExitType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_CUSTOM_EXIT_RULES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Information Point Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_INFORMATION_POINT_RULES];
                logger.Info("Information Point Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Information Point Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_INFORMATION_POINT_RULES);
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

                #region Agent Configuration Properties

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES];
                logger.Info("Agent Configuration Properties Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Agent Configuration Properties Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_AGENT_CONFIGURATION_PROPERTIES);
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
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_AGENT_CONFIGURATION_PROPERTIES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_CONFIGURATION_PIVOT_AGENT_CONFIGURATION_PROPERTIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsDefault");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "PropertyName");
                    addColumnFieldToPivot(pivot, "PropertyType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "PropertyName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_AGENT_CONFIGURATION_PROPERTIES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Data Collectors

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_METHOD_INVOCATION_DATA_COLLECTORS];
                logger.Info("Method Invocation Data Collectors Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Method Invocation Data Collectors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_METHOD_INVOCATION_DATA_COLLECTORS);
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
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_HTTP_DATA_COLLECTORS];
                logger.Info("HTTP Data Collectors Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("HTTP Data Collectors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_HTTP_DATA_COLLECTORS);
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

                #region Tier Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_TIER_SETTINGS];
                logger.Info("Tier Settings Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Tier Settings Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_TIER_SETTINGS);
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

                #region Detected Business Transaction and Assigned Data Collectors

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_BUSINESS_TRANSACTION_SETTINGS];
                logger.Info("Detected Business Transaction and Assigned Data Collectors Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Detected Business Transaction and Assigned Data Collectors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_BUSINESS_TRANSACTION_SETTINGS);
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

                #region Agent Call Graph Settings

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_AGENT_CALL_GRAPH_SETTINGS];
                logger.Info("Agent Call Graph Settings Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Agent Call Graph Settings Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_AGENT_CALL_GRAPH_SETTINGS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 25;

                }

                #endregion

                #region Health Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_HEALTH_RULES];
                logger.Info("Health Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Health Rules Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_HEALTH_RULES);
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
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_HEALTH_RULES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, REPORT_CONFIGURATION_PIVOT_HEALTH_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsDefault");
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "IsAlwaysEnabled");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "HRRuleType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RuleName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_HEALTH_RULES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Configuration Differences

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES];
                logger.Info("Configuration Differences Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Configuration Differences Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_CONFIGURATION_TABLE_CONFIGURATION_DIFFERENCES);
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

                    ExcelAddress cfAddressDifference = new ExcelAddress(REPORT_CONFIGURATION_LIST_SHEET_START_TABLE_AT + 1, table.Columns["Difference"].Position + 1, sheet.Dimension.Rows, table.Columns["Difference"].Position + 1);
                    addDifferenceConditionalFormatting(sheet, cfAddressDifference);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_CONFIGURATION_SHEET_CONFIGURATION_DIFFERENCES_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_CONFIGURATION_PIVOT_SHEET_START_PIVOT_AT + REPORT_CONFIGURATION_PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, REPORT_CONFIGURATION_PIVOT_CONFIGURATION_DIFFERENCES);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "RuleType");
                    addRowFieldToPivot(pivot, "RuleSubType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "EntityName");
                    addRowFieldToPivot(pivot, "Property");
                    addColumnFieldToPivot(pivot, "DifferenceController", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "DifferenceApp", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Difference", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EntityName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_CONFIGURATION_PIVOT_CONFIGURATION_DIFFERENCES_GRAPH, eChartType.ColumnClustered, pivot);
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
                sheet = excelReport.Workbook.Worksheets[REPORT_SHEET_TOC];
                sheet.Cells[1, 1].Value = "Sheet Name";
                sheet.Cells[1, 2].Value = "# Entities";
                sheet.Cells[1, 3].Value = "Link";
                int rowNum = 1;
                foreach (ExcelWorksheet s in excelReport.Workbook.Worksheets)
                {
                    rowNum++;
                    sheet.Cells[rowNum, 1].Value = s.Name;
                    sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                    sheet.Cells[rowNum, 3].StyleName = "HyperLinkStyle";
                    if (s.Tables.Count > 0)
                    {
                        sheet.Cells[rowNum, 2].Value = s.Tables[0].Address.Rows - 1;
                    }
                }
                range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, REPORT_CONFIGURATION_DETAILS_TABLE_TOC);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Sheet Name"].Position + 1).Width = 25;
                sheet.Column(table.Columns["# Entities"].Position + 1).Width = 25;

                #endregion

                #region Save file 

                if (Directory.Exists(FilePathMap.ReportFolderPath()) == false)
                {
                    Directory.CreateDirectory(FilePathMap.ReportFolderPath());
                }

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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
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
