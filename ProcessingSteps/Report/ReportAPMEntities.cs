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
    public class ReportAPMEntities : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_APPLICATIONS_APM_LIST = "5.Applications.APM";
        private const string SHEET_TIERS_LIST = "6.Tiers";
        private const string SHEET_TIERS_TYPE_PIVOT = "6.Tiers.Type";
        private const string SHEET_NODES_LIST = "7.Nodes";
        private const string SHEET_NODES_TYPE_APPAGENT_PIVOT = "7.Nodes.Type.AppAgent";
        private const string SHEET_NODES_TYPE_MACHINEAGENT_PIVOT = "7.Nodes.Type.MachineAgent";
        private const string SHEET_NODE_STARTUP_OPTIONS_LIST = "8.Node Startup Options";
        private const string SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT = "8.Node Startup Options.Type";
        private const string SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT = "8.Node Startup Options.Location";
        private const string SHEET_NODE_PROPERTIES_LIST = "8.Node VM Properties";
        private const string SHEET_NODE_PROPERTIES_TYPE_PIVOT = "8.Node VM Properties.Type";
        private const string SHEET_NODE_PROPERTIES_LOCATION_PIVOT = "8.Node VM Properties.Location";
        private const string SHEET_NODE_ENVIRONMENT_VARIABLES_LIST = "8.Node Env Variables";
        private const string SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT = "8.Node Env Variables.Type";
        private const string SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT = "8.Node Env Variables.Location";
        private const string SHEET_BACKENDS_LIST = "9.Backends";
        private const string SHEET_BACKENDS_TYPE_PIVOT = "9.Backends.Type";
        private const string SHEET_BACKENDS_LOCATION_PIVOT = "9.Backends.Location";
        private const string SHEET_BUSINESS_TRANSACTIONS_LIST = "10.Business Transactions";
        private const string SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT = "10.BTs.Type";
        private const string SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT = "10.BTs.Location";
        private const string SHEET_SERVICE_ENDPOINTS_LIST = "11.SEPs";
        private const string SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT = "11.SEPs.Type";
        private const string SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT = "11.SEPs.Location";
        private const string SHEET_ERRORS_LIST = "12.Errors";
        private const string SHEET_ERRORS_TYPE_PIVOT = "12.Errors.Type";
        private const string SHEET_ERRORS_LOCATION_PIVOT_LOCATION = "12.Errors.Location";
        private const string SHEET_INFORMATION_POINTS_LIST = "13.Information Points";
        private const string SHEET_INFORMATION_POINTS_TYPE_PIVOT = "13.Information Points.Type";
        private const string SHEET_MAPPED_BACKENDS_LIST = "14.Mapped Backends";
        private const string SHEET_MAPPED_BACKENDS_TYPE_PIVOT = "14.Mapped Backends.Type";
        private const string SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_LIST = "15.Overflow BTs";
        private const string SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE_PIVOT = "15.Overflow.BTs.Type";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_APPLICATIONS_APM = "t_Applications_APM";
        private const string TABLE_TIERS = "t_Tiers";
        private const string TABLE_NODES = "t_Nodes";
        private const string TABLE_NODE_STARTUP_OPTIONS = "t_NodeStartupOptions";
        private const string TABLE_NODE_PROPERTIES = "t_NodeProperties";
        private const string TABLE_NODE_ENVIRONMENT_VARIABLES = "t_NodeEnvironmentVariables";
        private const string TABLE_BACKENDS = "t_Backends";
        private const string TABLE_BUSINESS_TRANSACTIONS = "t_BusinessTransactions";
        private const string TABLE_SERVICE_ENDPOINTS = "t_ServiceEndpoints";
        private const string TABLE_ERRORS = "t_Errors";
        private const string TABLE_INFORMATION_POINTS = "t_InformationPoints";
        private const string TABLE_MAPPED_BACKENDS = "t_MappedBackends";
        private const string TABLE_OVERFLOW_BUSINESS_TRANSACTIONS = "t_OverflowBusinessTransactions";

        private const string PIVOT_TIERS_TYPE = "p_TiersType";
        private const string PIVOT_NODES_APPAGENT_TYPE = "p_NodesTypeAppAgent";
        private const string PIVOT_NODES_MACHINEAGENT_TYPE = "p_NodesTypeMachineAgent";
        private const string PIVOT_NODE_STARTUP_OPTIONS_TYPE = "p_NodeStartupOptionsType";
        private const string PIVOT_NODE_STARTUP_OPTIONS_LOCATION = "p_NodeStartupOptionsLocation";
        private const string PIVOT_NODE_PROPERTIES_TYPE = "p_NodePropertiesType";
        private const string PIVOT_NODE_PROPERTIES_LOCATION = "p_NodePropertiesLocation";
        private const string PIVOT_NODE_ENVIRONMENT_VARIABLES_TYPE = "p_NodeEnvironmentVariablesType";
        private const string PIVOT_NODE_ENVIRONMENT_VARIABLES_LOCATION = "p_NodeEnvironmentVariablesLocation";
        private const string PIVOT_BACKENDS_TYPE = "p_BackendsType";
        private const string PIVOT_BACKENDS_LOCATION = "p_BackendsLocation";
        private const string PIVOT_BUSINESS_TRANSACTIONS_TYPE = "p_BusinessTransactionsType";
        private const string PIVOT_BUSINESS_TRANSACTIONS_LOCATION = "p_BusinessTransactionsLocation";
        private const string PIVOT_SERVICE_ENDPOINTS_TYPE = "p_ServiceEndpointsType";
        private const string PIVOT_SERVICE_ENDPOINTS_LOCATION = "p_ServiceEndpointsLocation";
        private const string PIVOT_ERRORS_TYPE = "p_ErrorsType";
        private const string PIVOT_ERRORS_LOCATION = "p_ErrorsLocation";
        private const string PIVOT_INFORMATION_POINTS_TYPE = "p_InformationPointsType";
        private const string PIVOT_MAPPED_BACKENDS_TYPE = "p_MappedBackendsType";
        private const string PIVOT_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE = "p_OverflowBusinessTransactionsType";

        private const string GRAPH_TIERS_TYPE = "g_TiersType";
        private const string GRAPH_NODES_APPAGENT_TYPE = "g_NodesTypeAppAgent";
        private const string GRAPH_NODES_MACHINEAGENT_TYPE = "g_NodesTypeMachineAgent";
        private const string GRAPH_NODE_STARTUP_OPTIONS_TYPE = "g_NodeStartupOptionsType";
        private const string GRAPH_NODE_STARTUP_OPTIONS_LOCATION = "g_NodeStartupOptionsLocation";
        private const string GRAPH_NODE_PROPERTIES_TYPE = "g_NodePropertiesType";
        private const string GRAPH_NODE_PROPERTIES_LOCATION = "g_NodePropertiesLocation";
        private const string GRAPH_NODE_ENVIRONMENT_VARIABLES_TYPE = "g_NodeEnvironmentVariablesType";
        private const string GRAPH_NODE_ENVIRONMENT_VARIABLES_LOCATION = "g_NodeEnvironmentVariablesLocation";
        private const string GRAPH_BACKENDS_TYPE = "g_BackendsType";
        private const string GRAPH_BUSINESS_TRANSACTIONS_TYPE = "g_BusinessTransactionsType";
        private const string GRAPH_SERVICE_ENDPOINTS_TYPE = "g_ServiceEndpointsType";
        private const string GRAPH_ERRORS_TYPE = "g_ErrorsType";
        private const string GRAPH_INFORMATION_POINTS_TYPE = "g_InformationPointsType";
        private const string GRAPH_MAPPED_BACKENDS_TYPE = "g_MappedBackends";
        private const string GRAPH_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE = "g_OverflowBusinessTransactionsType";

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

            if (this.ShouldExecute(jobConfiguration) == false)
            {
                return true;
            }

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
            {
                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Detected APM Entities Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Detected APM Entities Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Detected APM Entities Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_APM_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of App Agent";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_TYPE_APPAGENT_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Types of Machine Agent";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_TYPE_MACHINEAGENT_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_TYPE_APPAGENT_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_TYPE_MACHINEAGENT_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_STARTUP_OPTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Options";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Options";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_STARTUP_OPTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_STARTUP_OPTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_PROPERTIES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_PROPERTIES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Properties";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_PROPERTIES_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_PROPERTIES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_PROPERTIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_PROPERTIES_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_PROPERTIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_ENVIRONMENT_VARIABLES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Properties";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_ENVIRONMENT_VARIABLES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODE_ENVIRONMENT_VARIABLES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Backends";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Locations of Backends";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BTs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Location of BTs";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 3].Value = "Overflow BTs";
                sheet.Cells[2, 4].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 4].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Type of SEPs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Location of SEPs";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ERRORS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Errors by Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "Location of Errors";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ERRORS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ERRORS_LOCATION_PIVOT_LOCATION);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_INFORMATION_POINTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Information Points by Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_INFORMATION_POINTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_INFORMATION_POINTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_INFORMATION_POINTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MAPPED_BACKENDS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Mapped Backends by Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MAPPED_BACKENDS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MAPPED_BACKENDS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MAPPED_BACKENDS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of BTs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                loggerConsole.Info("Fill Detected APM Entities Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications - All

                loggerConsole.Info("List of Applications - All");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ALL_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerApplicationsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_APM_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMApplicationsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Tiers

                loggerConsole.Info("List of Tiers");

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMTiersReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Nodes

                loggerConsole.Info("List of Nodes");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMNodesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Node Startup Options");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODE_STARTUP_OPTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMNodeStartupOptionsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Node VM Properties");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODE_PROPERTIES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMNodePropertiesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Node Environment Variables");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODE_ENVIRONMENT_VARIABLES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMNodeEnvironmentVariablesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Backends

                loggerConsole.Info("List of Backends");

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBackendsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transactions

                loggerConsole.Info("List of Business Transactions");

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMBusinessTransactionsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Service Endpoints

                loggerConsole.Info("List of Service Endpoints");

                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMServiceEndpointsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Errors

                loggerConsole.Info("List of Errors");

                sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMErrorsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Information Points

                loggerConsole.Info("List of Information Points");

                sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMInformationPointsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Mapped Backends

                loggerConsole.Info("List of Backends");

                sheet = excelReport.Workbook.Worksheets[SHEET_MAPPED_BACKENDS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMMappedBackendsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Overflow Business Transactions

                loggerConsole.Info("List of Overflow Business Transactions");

                sheet = excelReport.Workbook.Worksheets[SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMOverflowBusinessTransactionsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Detected APM Entities Report File");

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

                #region Applications

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_APM_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_APM);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMApplication.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTiers"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNodes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNodes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBackends"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumIPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumIPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_TIERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMTier.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNodes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNodes"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_TIERS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "TierName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_TIERS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMNode.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_NODES_TYPE_APPAGENT_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODES_APPAGENT_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentPresent");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "AgentVersion", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_NODES_APPAGENT_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_NODES_TYPE_MACHINEAGENT_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODES_MACHINEAGENT_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "MachineAgentPresent");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "MachineAgentVersion", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "MachineName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_NODES_MACHINEAGENT_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_NODE_STARTUP_OPTIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODE_STARTUP_OPTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMNodeProperty.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_NODE_STARTUP_OPTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODE_STARTUP_OPTIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_NODE_STARTUP_OPTIONS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_NODE_STARTUP_OPTIONS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODE_STARTUP_OPTIONS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_NODE_STARTUP_OPTIONS_LOCATION, eChartType.ColumnClustered, pivot);
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
                sheet = excelReport.Workbook.Worksheets[SHEET_NODE_PROPERTIES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODE_PROPERTIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMNodeProperty.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_NODE_PROPERTIES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODE_PROPERTIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_NODE_PROPERTIES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_NODE_PROPERTIES_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODE_PROPERTIES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_NODE_PROPERTIES_LOCATION, eChartType.ColumnClustered, pivot);
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
                sheet = excelReport.Workbook.Worksheets[SHEET_NODE_ENVIRONMENT_VARIABLES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODE_ENVIRONMENT_VARIABLES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMNodeProperty.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_NODE_ENVIRONMENT_VARIABLES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODE_ENVIRONMENT_VARIABLES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_NODE_ENVIRONMENT_VARIABLES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_NODE_ENVIRONMENT_VARIABLES_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_NODE_ENVIRONMENT_VARIABLES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_NODE_ENVIRONMENT_VARIABLES_LOCATION, eChartType.ColumnClustered, pivot);
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
                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BACKENDS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMBackend.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_BACKENDS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExplicitRule");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "BackendName");
                    addColumnFieldToPivot(pivot, "BackendType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "BackendName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_BACKENDS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_BACKENDS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExplicitRule");
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
                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BUSINESS_TRANSACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_BUSINESS_TRANSACTIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsExplicitRule");
                    addFilterFieldToPivot(pivot, "IsRenamed");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addColumnFieldToPivot(pivot, "BTType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "BTName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_BUSINESS_TRANSACTIONS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_BUSINESS_TRANSACTIONS_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsRenamed");
                    addFilterFieldToPivot(pivot, "IsExplicitRule");
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
                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_SERVICE_ENDPOINTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMServiceEndpoint.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_SERVICE_ENDPOINTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "SEPName");
                    addColumnFieldToPivot(pivot, "SEPType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SEPName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_SERVICE_ENDPOINTS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_SERVICE_ENDPOINTS_LOCATION);
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
                sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ERRORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMError.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_ERRORS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ErrorDepth", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "ErrorName");
                    addColumnFieldToPivot(pivot, "ErrorType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "ErrorName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_ERRORS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_LOCATION_PIVOT_LOCATION];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT, 1], range, PIVOT_ERRORS_LOCATION);
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
                sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_INFORMATION_POINTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInEntitiesReport(APMInformationPoint.ENTITY_TYPE, sheet, table);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_INFORMATION_POINTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "IPName");
                    addColumnFieldToPivot(pivot, "IPType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "IPName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_INFORMATION_POINTS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Mapped Backends

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MAPPED_BACKENDS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MAPPED_BACKENDS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BackendType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Prop1Name"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["Prop2Name"].Position + 1).Width = 25;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MAPPED_BACKENDS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MAPPED_BACKENDS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BackendName");
                    addColumnFieldToPivot(pivot, "BackendType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "BackendID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_BACKENDS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Overflow Business Transactions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_OVERFLOW_BUSINESS_TRANSACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTType"].Position + 1).Width = 10;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addColumnFieldToPivot(pivot, "BTType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_OVERFLOW_BUSINESS_TRANSACTIONS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.APMEntitiesExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            logger.Trace("Output.DetectedEntities={0}", jobConfiguration.Output.DetectedEntities);
            loggerConsole.Trace("Output.DetectedEntities={0}", jobConfiguration.Output.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false || jobConfiguration.Output.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping report of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true && jobConfiguration.Output.DetectedEntities == true);
        }

        private static void adjustColumnsOfEntityRowTableInEntitiesReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            if (entityType == APMApplication.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["Description"].Position + 1).Width = 15;
            }
            else if (entityType == APMTier.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["TierType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["AgentType"].Position + 1).Width = 15;
            }
            else if (entityType == APMNode.ENTITY_TYPE)
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
            else if (entityType == APMBackend.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["BackendName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BackendType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
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
            else if (entityType == APMBusinessTransaction.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTNameOriginal"].Position + 1).Width = 20;
                sheet.Column(table.Columns["BTType"].Position + 1).Width = 10;
                sheet.Column(table.Columns["RuleName"].Position + 1).Width = 20;
            }
            else if (entityType == APMServiceEndpoint.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["SEPType"].Position + 1).Width = 10;
            }
            else if (entityType == APMError.ENTITY_TYPE)
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
            else if (entityType == APMInformationPoint.ENTITY_TYPE)
            {
                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                sheet.Column(table.Columns["IPName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["IPType"].Position + 1).Width = 10;
            }
            else if (entityType == APMNodeProperty.ENTITY_TYPE)
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
