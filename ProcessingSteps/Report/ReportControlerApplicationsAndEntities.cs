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
    public class ReportControlerApplicationsAndEntities : JobStepReportBase
    {
        #region Constants for Detected Entities Report contents

        private const string REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS = "3.Controllers";
        private const string REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST = "4.Applications";
        private const string REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST = "5.Tiers";
        private const string REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT = "5.Tiers.Pivot";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST = "6.Nodes";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT = "6.Nodes.Type.AppAgent";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT = "6.Nodes.Type.MachineAgent";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LIST = "6.Node Startup Options";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT = "6.Node Startup Options.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT = "6.Node Startup Options.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LIST = "6.Node VM Properties";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_TYPE_PIVOT = "6.Node VM Properties.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LOCATION_PIVOT = "6.Node VM Properties.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LIST = "6.Node Env Variables";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT = "6.Node Env Variables.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT = "6.Node Env Variables.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST = "7.Backends";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT = "7.Backends.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT = "7.Backends.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST = "8.Business Transactions";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT = "8.BTs.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT = "8.BTs.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST = "9.SEPs";
        private const string REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT = "9.SEPs.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT = "9.SEPs.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST = "10.Errors";
        private const string REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT = "10.Errors.Type";
        private const string REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION = "10.Errors.Location";
        private const string REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_LIST = "11.Information Points";
        private const string REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_TYPE_PIVOT = "11.Information Points.Type";

        private const string REPORT_DETECTED_ENTITIES_TABLE_TOC = "t_TOC";
        private const string REPORT_DETECTED_ENTITIES_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_DETECTED_ENTITIES_TABLE_APPLICATIONS = "t_Applications";
        private const string REPORT_DETECTED_ENTITIES_TABLE_TIERS = "t_Tiers";
        private const string REPORT_DETECTED_ENTITIES_TABLE_NODES = "t_Nodes";
        private const string REPORT_DETECTED_ENTITIES_TABLE_NODE_STARTUP_OPTIONS = "t_NodeStartupOptions";
        private const string REPORT_DETECTED_ENTITIES_TABLE_NODE_PROPERTIES = "t_NodeProperties";
        private const string REPORT_DETECTED_ENTITIES_TABLE_NODE_ENVIRONMENT_VARIABLES = "t_NodeEnvironmentVariables";
        private const string REPORT_DETECTED_ENTITIES_TABLE_BACKENDS = "t_Backends";
        private const string REPORT_DETECTED_ENTITIES_TABLE_BUSINESS_TRANSACTIONS = "t_BusinessTransactions";
        private const string REPORT_DETECTED_ENTITIES_TABLE_SERVICE_ENDPOINTS = "t_ServiceEndpoints";
        private const string REPORT_DETECTED_ENTITIES_TABLE_ERRORS = "t_Errors";
        private const string REPORT_DETECTED_ENTITIES_TABLE_INFORMATION_POINTS = "t_InformationPoints";

        private const string REPORT_DETECTED_ENTITIES_PIVOT_TIERS = "p_Tiers";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_APPAGENT = "p_NodesTypeAppAgent";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_MACHINEAGENT = "p_NodesTypeMachineAgent";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_TYPE = "p_NodeStartupOptionsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_LOCATION = "p_NodeStartupOptionsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_TYPE = "p_NodePropertiesType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_LOCATION = "p_NodePropertiesLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_TYPE = "p_NodeEnvironmentVariablesType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_LOCATION = "p_NodeEnvironmentVariablesLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_TYPE = "p_BackendsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_LOCATION = "p_BackendsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_TYPE = "p_BusinessTransactionsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_LOCATION = "p_BusinessTransactionsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_TYPE = "p_ServiceEndpointsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_LOCATION = "p_ServiceEndpointsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_TYPE = "p_ErrorsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_LOCATION = "p_ErrorsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_INFORMATION_POINTS_TYPE = "p_InformationPointsType";

        private const string REPORT_DETECTED_ENTITIES_PIVOT_TIERS_GRAPH = "g_Tiers";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_APPAGENT_GRAPH = "g_NodesTypeAppAgent";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_MACHINEAGENT_GRAPH = "g_NodesTypeMachineAgent";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_TYPE_GRAPH = "g_NodeStartupOptionsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_LOCATION_GRAPH = "g_NodeStartupOptionsLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_TYPE_GRAPH = "g_NodePropertiesType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_LOCATION_GRAPH = "g_NodePropertiesLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_TYPE_GRAPH = "g_NodeEnvironmentVariablesType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_LOCATION_GRAPH = "g_NodeEnvironmentVariablesLocation";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_TYPE_GRAPH = "g_BackendsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_TYPE_GRAPH = "g_BusinessTransactionsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_TYPE_GRAPH = "g_ServiceEndpointsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_TYPE_GRAPH = "g_ErrorsType";
        private const string REPORT_DETECTED_ENTITIES_PIVOT_INFORMATION_POINTS_TYPE_GRAPH = "g_InformationPointsType";

        private const int REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT = 14;

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
                loggerConsole.Info("Prepare Detected Entities Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Detected Entities Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Detected Entities Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of App Agent";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Types of Machine Agent";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Options";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Options";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Properties";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Properties";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Backends";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Backends";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BTs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Location of BTs";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Type of SEPs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Location of SEPs";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Errors by Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Location of Errors";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Information Points by Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                loggerConsole.Info("Fill Detected Entities Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllersReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Tiers

                loggerConsole.Info("List of Tiers");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.TiersReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Nodes

                loggerConsole.Info("List of Nodes");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NodesReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Node Startup Options");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NodeStartupOptionsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Node VM Properties");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NodePropertiesReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Node Environment Variables");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NodeEnvironmentVariablesReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Backends

                loggerConsole.Info("List of Backends");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BackendsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transactions

                loggerConsole.Info("List of Business Transactions");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Service Endpoints

                loggerConsole.Info("List of Service Endpoints");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ServiceEndpointsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Errors

                loggerConsole.Info("List of Errors");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ErrorsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Information Points

                loggerConsole.Info("List of Information Points");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.InformationPointsReportFilePath(), 0, sheet, REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Detected Entities Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_CONTROLLERS];
                logger.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["UserName"].Position + 1).Width = 25;
                }

                #endregion

                #region Applications

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_APPLICATIONS_LIST];
                logger.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_APPLICATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityApplication.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTiers"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNodes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNodes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBackends"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumIPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumIPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_TIERS_LIST];
                logger.Info("Tiers Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Tiers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_TIERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityTier.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNodes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNodes"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_TIERS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_TIERS);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "TierName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_TIERS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_LIST];
                logger.Info("Nodes Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Nodes Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_NODES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityNode.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_APPAGENT_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_APPAGENT);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentPresent");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "AgentVersion", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_APPAGENT_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODES_TYPE_MACHINEAGENT_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_MACHINEAGENT);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "MachineAgentPresent");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "MachineAgentVersion", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "MachineName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODES_TYPE_MACHINEAGENT_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LIST];
                logger.Info("Node Startup Options Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Node Startup Options Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_NODE_STARTUP_OPTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityNodeProperty.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODE_STARTUP_OPTIONS_LOCATION_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LIST];
                logger.Info("Node VM Properties Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Node VM Properties Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_NODE_PROPERTIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityNodeProperty.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_PROPERTIES_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODE_PROPERTIES_LOCATION_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LIST];
                logger.Info("Node Environment Variables Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Node Environment Variables Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_NODE_ENVIRONMENT_VARIABLES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityNodeProperty.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_NODE_ENVIRONMENT_VARIABLES_LOCATION_GRAPH, eChartType.ColumnClustered, pivot);
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

                #region Backends

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LIST];
                logger.Info("Backends Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Backends Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_BACKENDS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityBackend.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "BackendName");
                    addColumnFieldToPivot(pivot, "BackendType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "BackendName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BACKENDS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BACKENDS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "BackendType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "BackendName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addDataFieldToPivot(pivot, "BackendName", DataFieldFunctions.Count);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LIST];
                logger.Info("Business Transactions Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Business Transactions Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_BUSINESS_TRANSACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityBusinessTransaction.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsRenamed");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addColumnFieldToPivot(pivot, "BTType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "BTName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_BUSINESS_TRANSACTIONS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsRenamed");
                    addRowFieldToPivot(pivot, "BTType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addDataFieldToPivot(pivot, "BTName", DataFieldFunctions.Count);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Service Endpoints

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LIST];
                logger.Info("Service Endpoints Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Service Endpoints Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_SERVICE_ENDPOINTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityServiceEndpoint.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "SEPName");
                    addColumnFieldToPivot(pivot, "SEPType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SEPName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_SERVICE_ENDPOINTS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "SEPType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "SEPName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addDataFieldToPivot(pivot, "SEPName", DataFieldFunctions.Count);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Errors

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LIST];
                logger.Info("Errors Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Errors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_ERRORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityError.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ErrorDepth", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "ErrorName");
                    addColumnFieldToPivot(pivot, "ErrorType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "ErrorName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_ERRORS_LOCATION_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_ERRORS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ErrorDepth", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ErrorType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ErrorName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addDataFieldToPivot(pivot, "ErrorName", DataFieldFunctions.Count);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Information Points

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_LIST];
                logger.Info("Information Points Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Information Points Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_ENTITIES_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_INFORMATION_POINTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(EntityInformationPoint.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_ENTITIES_SHEET_INFORMATION_POINTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_ENTITIES_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_ENTITIES_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_ENTITIES_PIVOT_INFORMATION_POINTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "IPName");
                    addColumnFieldToPivot(pivot, "IPType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "IPName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_ENTITIES_PIVOT_INFORMATION_POINTS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
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
                table = sheet.Tables.Add(range, REPORT_DETECTED_ENTITIES_TABLE_TOC);
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

                string reportFilePath = FilePathMap.EntitiesExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("Output.DetectedEntities={0}", jobConfiguration.Output.DetectedEntities);
            loggerConsole.Trace("Output.DetectedEntities={0}", jobConfiguration.Output.DetectedEntities);
            if (jobConfiguration.Output.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping report of detected entities");
            }
            return (jobConfiguration.Output.DetectedEntities == true);
        }

        private static void adjustColumnsOfEntityRowTableInEntitiesReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            if (entityType == EntityApplication.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
            }
            else if (entityType == EntityTier.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["TierType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["AgentType"].Position + 1).Width = 15;
            }
            else if (entityType == EntityNode.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["AgentType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["AgentVersion"].Position + 1).Width = 10;
                sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["MachineAgentVersion"].Position + 1).Width = 10;
            }
            else if (entityType == EntityBackend.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["Prop1Name"].Position + 1).Width = 25;
                sheet.Column(table.Columns["Prop2Name"].Position + 1).Width = 25;
                sheet.Column(table.Columns["Prop3Name"].Position + 1).Width = 25;
                sheet.Column(table.Columns["Prop4Name"].Position + 1).Width = 25;
                sheet.Column(table.Columns["Prop5Name"].Position + 1).Width = 25;
                sheet.Column(table.Columns["Prop1Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop2Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop3Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop4Value"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Prop5Value"].Position + 1).Width = 20;
            }
            else if (entityType == EntityBusinessTransaction.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTNameOriginal"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTType"].Position + 1).Width = 10;
            }
            else if (entityType == EntityServiceEndpoint.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPType"].Position + 1).Width = 10;
            }
            else if (entityType == EntityError.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ErrorName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["HttpCode"].Position + 1).Width = 25;
                sheet.Column(table.Columns["ErrorDepth"].Position + 1).Width = 25;
                sheet.Column(table.Columns["ErrorLevel1"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel2"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel3"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel4"].Position + 1).Width = 20;
                sheet.Column(table.Columns["ErrorLevel5"].Position + 1).Width = 20;
            }
            else if (entityType == EntityInformationPoint.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["IPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["IPType"].Position + 1).Width = 10;
            }
            else if (entityType == EntityNodeProperty.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["AgentType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["PropName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["PropValue"].Position + 1).Width = 25;
            }
        }
    }
}
