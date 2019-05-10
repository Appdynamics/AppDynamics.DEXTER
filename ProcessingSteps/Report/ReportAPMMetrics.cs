using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
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
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMMetrics : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS = "3.Controllers";
        private const string SHEET_APPLICATIONS_FULL = "4.Applications";
        private const string SHEET_APPLICATIONS_HOURLY = "4.Applications.Hourly";
        private const string SHEET_APPLICATIONS_PERF_PIVOT = "4.Applications.Perf";
        private const string SHEET_TIERS_FULL = "5.Tiers";
        private const string SHEET_TIERS_HOURLY = "5.Tiers.Hourly";
        private const string SHEET_TIERS_LICENSE_PIVOT = "5.Tiers.Availability";
        private const string SHEET_TIERS_PERF_PIVOT = "5.Tiers.Perf";
        private const string SHEET_NODES_FULL = "6.Nodes";
        private const string SHEET_NODES_HOURLY = "6.Nodes.Hourly";
        private const string SHEET_NODES_LICENSE_PIVOT = "6.Nodes.License";
        private const string SHEET_NODES_PERF_PIVOT = "6.Nodes.Perf";
        private const string SHEET_BACKENDS_FULL = "7.Backends";
        private const string SHEET_BACKENDS_HOURLY = "7.Backends.Hourly";
        private const string SHEET_BACKENDS_PERF_PIVOT = "7.Backends.Perf";
        private const string SHEET_BUSINESS_TRANSACTIONS_FULL = "8.BTs";
        private const string SHEET_BUSINESS_TRANSACTIONS_HOURLY = "8.BTs.Hourly";
        private const string SHEET_BUSINESS_TRANSACTIONS_PERF_PIVOT = "8.BTs.Perf";
        private const string SHEET_SERVICE_ENDPOINTS_FULL = "9.SEPs";
        private const string SHEET_SERVICE_ENDPOINTS_HOURLY = "9.SEPs.Hourly";
        private const string SHEET_SERVICE_ENDPOINTS_PERF_PIVOT = "9.SEPs.Perf";
        private const string SHEET_ERRORS_FULL = "10.Errors";
        private const string SHEET_ERRORS_HOURLY = "10.Errors.Hourly";
        private const string SHEET_ERRORS_PERF_PIVOT = "10.Errors.Perf";
        private const string SHEET_INFORMATION_POINTS_FULL = "11.Information Points";
        private const string SHEET_INFORMATION_POINTS_HOURLY = "11.Information Points.Hourly";
        private const string SHEET_INFORMATION_POINTS_PERF_PIVOT = "11.Information Points.Perf";

        private const string SHEET_APPLICATIONS_ACTIVITYFLOW = "4.Applications.Activity Flow";
        private const string SHEET_TIERS_ACTIVITYFLOW = "5.Tiers.Activity Flow";
        private const string SHEET_NODES_ACTIVITYFLOW = "6.Nodes.Activity Flow";
        private const string SHEET_BACKENDS_ACTIVITYFLOW = "7.Backends.Activity Flow";
        private const string SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW = "8.BTs.Activity Flow";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_FULL = "t_Applications_Full";
        private const string TABLE_APPLICATIONS_HOURLY = "t_Applications_Hourly";
        private const string TABLE_TIERS_FULL = "t_Tiers_Full";
        private const string TABLE_TIERS_HOURLY = "t_Tiers_Hourly";
        private const string TABLE_NODES_FULL = "t_Nodes_Full";
        private const string TABLE_NODES_HOURLY = "t_Nodes_Hourly";
        private const string TABLE_BACKENDS_FULL = "t_Backends_Full";
        private const string TABLE_BACKENDS_HOURLY = "t_Backends_Hourly";
        private const string TABLE_BUSINESS_TRANSACTIONS_FULL = "t_BusinessTransactions_Full";
        private const string TABLE_BUSINESS_TRANSACTIONS_HOURLY = "t_BusinessTransactions_Hourly";
        private const string TABLE_SERVICE_ENDPOINTS_FULL = "t_ServiceEndpoints_Full";
        private const string TABLE_SERVICE_ENDPOINTS_HOURLY = "t_ServiceEndpoints_Hourly";
        private const string TABLE_ERRORS_FULL = "t_Errors_Full";
        private const string TABLE_ERRORS_HOURLY = "t_Errors_Hourly";
        private const string TABLE_INFORMATION_POINTS_FULL = "t_InformationPoints_Full";
        private const string TABLE_INFORMATION_POINTS_HOURLY = "t_InformationPoints_Hourly";

        private const string TABLE_APPLICATIONS_ACTIVITYFLOW = "t_Applications_ActivityFlow";
        private const string TABLE_TIERS_ACTIVITYFLOW = "t_Tiers_ActivityFlow";
        private const string TABLE_NODES_ACTIVITYFLOW = "t_Nodes_ActivityFlow";
        private const string TABLE_BACKENDS_ACTIVITYFLOW = "t_Backends_ActivityFlow";
        private const string TABLE_BUSINESS_TRANSACTIONS_ACTIVITYFLOW = "t_BusinessTransactions_ActivityFlow";

        private const string PIVOT_APPLICATIONS = "p_Applications";
        private const string PIVOT_TIERS = "p_Tiers";
        private const string PIVOT_TIERS_LICENSE = "p_Tiers_License";
        private const string PIVOT_NODES = "p_Nodes";
        private const string PIVOT_NODES_LICENSE = "p_NodesLicense";
        private const string PIVOT_BACKENDS = "p_Backends";
        private const string PIVOT_BUSINESS_TRANSACTIONS = "p_BusinessTransactions";
        private const string PIVOT_SERVICE_ENDPOINTS = "p_ServiceEndpoints";
        private const string PIVOT_ERRORS = "p_Errors";
        private const string PIVOT_INFORMATION_POINTS = "p_InformationPoints";

        private const string GRAPH_APPLICATIONS_FULL = "g_Applications_Full_Scatter";
        private const string GRAPH_APPLICATIONS_HOURLY = "g_Applications_Hourly_Scatter";
        private const string GRAPH_TIERS_LICENSE = "g_TiersLicense";
        private const string GRAPH_TIERS_FULL = "g_Tiers_Full_Scatter";
        private const string GRAPH_TIERS_HOURLY = "g_Tiers_Hourly_Scatter";
        private const string GRAPH_NODES_LICENSE = "g_NodesLicense";
        private const string GRAPH_NODES_FULL = "g_Nodes_Full_Scatter";
        private const string GRAPH_NODES_HOURLY = "g_Nodes_Hourly_Scatter";
        private const string GRAPH_BACKENDS_FULL = "g_Backends_Full_Scatter";
        private const string GRAPH_BACKENDS_HOURLY = "g_Backends_Hourly_Scatter";
        private const string GRAPH_BUSINESS_TRANSACTIONS_FULL = "g_BusinessTransactions_Full_Scatter";
        private const string GRAPH_BUSINESS_TRANSACTIONS_HOURLY = "g_BusinessTransactions_Hourly_Scatter";
        private const string GRAPH_SERVICE_ENDPOINTS_FULL = "g_ServiceEndpoints_Full_Scatter";
        private const string GRAPH_SERVICE_ENDPOINTS_HOURLY = "g_ServiceEndpoints_Hourly_Scatter";
        private const string GRAPH_INFORMATION_POINTS_FULL = "g_InformationPoints_Full_Scatter";
        private const string GRAPH_INFORMATION_POINTS_HOURLY = "g_InformationPoints_Hourly_Scatter";

        private const int LIST_SHEET_START_TABLE_AT = 17;
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
                loggerConsole.Info("Prepare Entity Metrics Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Entity Metrics Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Entity Metrics Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONTROLLERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APPLICATIONS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APPLICATIONS_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APPLICATIONS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APPLICATIONS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_APPLICATIONS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[4, 1].Value = "See Agent Availability";
                sheet.Cells[4, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_LICENSE_PIVOT);
                sheet.Cells[4, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[4, 1].Value = "See License Consumption";
                sheet.Cells[4, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_LICENSE_PIVOT);
                sheet.Cells[4, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_LICENSE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TIERS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[4, 1].Value = "See License Consumption";
                sheet.Cells[4, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_LICENSE_PIVOT);
                sheet.Cells[4, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[4, 1].Value = "See License Consumption";
                sheet.Cells[4, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_LICENSE_PIVOT);
                sheet.Cells[4, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_LICENSE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_NODES_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BACKENDS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Activity Flow";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_TRANSACTIONS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINTS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINTS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINTS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINTS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ERRORS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ERRORS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ERRORS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ERRORS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_INFORMATION_POINTS_FULL);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_INFORMATION_POINTS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_INFORMATION_POINTS_HOURLY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_INFORMATION_POINTS_PERF_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_INFORMATION_POINTS_PERF_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_INFORMATION_POINTS_FULL);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 3, 1);

                #endregion

                loggerConsole.Info("Fill Entity Metrics Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMApplication.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Applications (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMApplication.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("Applications Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationsFlowmapReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Tiers

                loggerConsole.Info("List of Tiers (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMTier.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Tiers (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMTier.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("Tiers Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.TiersFlowmapReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Nodes

                loggerConsole.Info("List of Nodes (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMNode.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Nodes (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMNode.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("Nodes Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NodesFlowmapReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Backends

                loggerConsole.Info("List of Backends (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMBackend.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Backends (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMBackend.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("Backends Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BackendsFlowmapReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Business Transactions

                loggerConsole.Info("List of Business Transactions (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMBusinessTransaction.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Business Transactions (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMBusinessTransaction.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("Business Transactions Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionsFlowmapReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Service Endpoints

                loggerConsole.Info("List of Service Endpoints (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMServiceEndpoint.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Service Endpoints (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMServiceEndpoint.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Errors

                loggerConsole.Info("List of Errors (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMError.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                loggerConsole.Info("List of Errors (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMError.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Information Points

                loggerConsole.Info("List of Information Points (Full)");

                sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_FULL];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesFullReportFilePath(APMInformationPoint.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                loggerConsole.Info("List of Information Points (Hourly)");

                sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_HOURLY];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EntitiesHourReportFilePath(APMInformationPoint.ENTITY_FOLDER), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Entity Metrics Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["Version"].Position + 1).Width = 15;
                }

                #endregion

                #region Applications

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);

                    addScatterChartToEntityMetricSheet(sheet, table, "ApplicationName", GRAPH_APPLICATIONS_FULL);
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);

                    addScatterChartToEntityMetricSheet(sheet, table, "ApplicationName", GRAPH_APPLICATIONS_HOURLY);

                    sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_APPLICATIONS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "Exceptions", DataFieldFunctions.Sum, "Exceptions");
                    addDataFieldToPivot(pivot, "EXCPM", DataFieldFunctions.Average, "EXCPM");
                    addDataFieldToPivot(pivot, "HttpErrors", DataFieldFunctions.Sum, "HttpErrors");
                    addDataFieldToPivot(pivot, "HTTPEPM", DataFieldFunctions.Average, "HTTPEPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_TIERS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMTier.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMTier.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "TierName", GRAPH_TIERS_FULL);
                    }
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_TIERS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMTier.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMTier.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "TierName", GRAPH_TIERS_HOURLY);
                    }

                    sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_TIERS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "Exceptions", DataFieldFunctions.Sum, "Exceptions");
                    addDataFieldToPivot(pivot, "EXCPM", DataFieldFunctions.Average, "EXCPM");
                    addDataFieldToPivot(pivot, "HttpErrors", DataFieldFunctions.Sum, "HttpErrors");
                    addDataFieldToPivot(pivot, "HTTPEPM", DataFieldFunctions.Average, "HTTPEPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_LICENSE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_TIERS_LICENSE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AvailAgent");
                    addFilterFieldToPivot(pivot, "AgentType");
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["From"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "AvailAgent", DataFieldFunctions.Average);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_TIERS_LICENSE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_TIERS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMTier.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODES_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMNode.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMNode.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "NodeName", GRAPH_NODES_FULL);
                    }
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODES_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMNode.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMNode.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "NodeName", GRAPH_NODES_HOURLY);
                    }

                    sheet = excelReport.Workbook.Worksheets[SHEET_NODES_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_NODES);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "AgentType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "Exceptions", DataFieldFunctions.Sum, "Exceptions");
                    addDataFieldToPivot(pivot, "EXCPM", DataFieldFunctions.Average, "EXCPM");
                    addDataFieldToPivot(pivot, "HttpErrors", DataFieldFunctions.Sum, "HttpErrors");
                    addDataFieldToPivot(pivot, "HTTPEPM", DataFieldFunctions.Average, "HTTPEPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_NODES_LICENSE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1], range, PIVOT_NODES_LICENSE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsLicenseAgentUsed");
                    addFilterFieldToPivot(pivot, "IsLicenseMachineUsed");
                    addFilterFieldToPivot(pivot, "AvailAgent");
                    addFilterFieldToPivot(pivot, "AvailMachine");
                    addFilterFieldToPivot(pivot, "AgentType");
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["From"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "NodeID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_NODES_LICENSE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODES_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMNode.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Backends

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BACKENDS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMBackend.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMBackend.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "BackendName", GRAPH_BACKENDS_FULL);
                    }
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BACKENDS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMBackend.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMBackend.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "BackendName", GRAPH_BACKENDS_HOURLY);
                    }

                    sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_BACKENDS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "BackendType");
                    addRowFieldToPivot(pivot, "BackendName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BACKENDS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMBackend.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BUSINESS_TRANSACTIONS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "BTName", GRAPH_BUSINESS_TRANSACTIONS_FULL);
                    }
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BUSINESS_TRANSACTIONS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "BTName", GRAPH_BUSINESS_TRANSACTIONS_HOURLY);
                    }

                    sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_BUSINESS_TRANSACTIONS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTType");
                    addRowFieldToPivot(pivot, "BTName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BUSINESS_TRANSACTIONS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Service Endpoints

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_SERVICE_ENDPOINTS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMServiceEndpoint.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMServiceEndpoint.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "SEPName", GRAPH_SERVICE_ENDPOINTS_FULL);
                    }
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_SERVICE_ENDPOINTS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMServiceEndpoint.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMServiceEndpoint.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "SEPName", GRAPH_SERVICE_ENDPOINTS_HOURLY);
                    }

                    sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINTS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_SERVICE_ENDPOINTS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "SEPType");
                    addRowFieldToPivot(pivot, "SEPName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Errors

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ERRORS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMError.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMError.ENTITY_TYPE, sheet, table);
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ERRORS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMError.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMError.ENTITY_TYPE, sheet, table);

                    sheet = excelReport.Workbook.Worksheets[SHEET_ERRORS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_ERRORS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "ErrorType");
                    addRowFieldToPivot(pivot, "ErrorName");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Information Points

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_FULL];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_INFORMATION_POINTS_FULL);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMInformationPoint.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMInformationPoint.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "IPName", GRAPH_INFORMATION_POINTS_FULL);
                    }
                }

                sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_HOURLY];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_INFORMATION_POINTS_HOURLY);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMInformationPoint.ENTITY_TYPE, sheet, table);
                    addConditionalFormattingToTableInMetricReport(APMInformationPoint.ENTITY_TYPE, sheet, table);

                    if (sheet.Dimension.Rows < 2018)
                    {
                        addScatterChartToEntityMetricSheet(sheet, table, "IPName", GRAPH_INFORMATION_POINTS_HOURLY);
                    }

                    sheet = excelReport.Workbook.Worksheets[SHEET_INFORMATION_POINTS_PERF_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 1, 1], range, PIVOT_INFORMATION_POINTS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "From");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "IPType");
                    addRowFieldToPivot(pivot, "IPName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");
                    addDataFieldToPivot(pivot, "Errors", DataFieldFunctions.Sum, "Errors");
                    addDataFieldToPivot(pivot, "EPM", DataFieldFunctions.Average, "EPM");
                    addDataFieldToPivot(pivot, "ErrorsPercentage", DataFieldFunctions.Average, "Errors %");

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

                if (Directory.Exists(FilePathMap.ReportFolderPath()) == false)
                {
                    Directory.CreateDirectory(FilePathMap.ReportFolderPath());
                }

                string reportFilePath = FilePathMap.EntityMetricsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            logger.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            loggerConsole.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            logger.Trace("Output.EntityMetrics={0}", jobConfiguration.Output.EntityMetrics);
            loggerConsole.Trace("Output.EntityMetrics={0}", jobConfiguration.Output.EntityMetrics);
            if ((jobConfiguration.Input.Metrics == false && jobConfiguration.Input.Flowmaps == false) || jobConfiguration.Output.EntityMetrics == false)
            {
                loggerConsole.Trace("Skipping report of entity metrics");
            }
            return ((jobConfiguration.Input.Metrics == true || jobConfiguration.Input.Flowmaps == true) && jobConfiguration.Output.EntityMetrics == true);
        }

        private static void addConditionalFormattingToTableInMetricReport(string entityType, ExcelWorksheet sheet, ExcelTable table)
        {
            if (entityType == APMApplication.ENTITY_TYPE ||
                entityType == APMTier.ENTITY_TYPE ||
                entityType == APMNode.ENTITY_TYPE ||
                entityType == APMBackend.ENTITY_TYPE ||
                entityType == APMBusinessTransaction.ENTITY_TYPE ||
                entityType == APMServiceEndpoint.ENTITY_TYPE ||
                entityType == APMInformationPoint.ENTITY_TYPE)
            {
                ExcelAddress cfAddressErrorPercentage = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ErrorsPercentage"].Position + 1, sheet.Dimension.Rows, table.Columns["ErrorsPercentage"].Position + 1);
                var cfErrorPercentage = sheet.ConditionalFormatting.AddDatabar(cfAddressErrorPercentage, colorRedForDatabars);
                cfErrorPercentage.LowValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                cfErrorPercentage.LowValue.Value = 0;
                cfErrorPercentage.HighValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                cfErrorPercentage.HighValue.Value = 100;

                ExcelAddress cfAddressART = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ART"].Position + 1, sheet.Dimension.Rows, table.Columns["ART"].Position + 1);
                var cfART = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressART);
                cfART.LowValue.Color = colorGreenFor3ColorScales;
                cfART.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                cfART.MiddleValue.Value = 70;
                cfART.MiddleValue.Color = colorYellowFor3ColorScales;
                cfART.HighValue.Color = colorRedFor3ColorScales;

                ExcelAddress cfAddressCPM = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["CPM"].Position + 1, sheet.Dimension.Rows, table.Columns["CPM"].Position + 1);
                var cfCPM = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressCPM);
                cfCPM.LowValue.Color = colorGreenFor3ColorScales;
                cfCPM.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                cfCPM.MiddleValue.Value = 70;
                cfCPM.MiddleValue.Color = colorYellowFor3ColorScales;
                cfCPM.HighValue.Color = colorRedFor3ColorScales;

                ExcelAddress cfAddressEPM = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["EPM"].Position + 1, sheet.Dimension.Rows, table.Columns["EPM"].Position + 1);
                var cfEPM = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressEPM);
                cfEPM.LowValue.Color = colorGreenFor3ColorScales;
                cfEPM.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                cfEPM.MiddleValue.Value = 70;
                cfEPM.MiddleValue.Color = colorYellowFor3ColorScales;
                cfEPM.HighValue.Color = colorRedFor3ColorScales;
            }
            else if (entityType == APMError.ENTITY_TYPE)
            {
                ExcelAddress cfAddressEPM = new ExcelAddress(LIST_SHEET_START_TABLE_AT - 13 + 1, table.Columns["EPM"].Position + 1, sheet.Dimension.Rows, table.Columns["EPM"].Position + 1);
                var cfEPM = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressEPM);
                cfEPM.LowValue.Color = colorGreenFor3ColorScales;
                cfEPM.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                cfEPM.MiddleValue.Value = 70;
                cfEPM.MiddleValue.Color = colorYellowFor3ColorScales;
                cfEPM.HighValue.Color = colorRedFor3ColorScales;
            }
        }

        private static void addScatterChartToEntityMetricSheet(ExcelWorksheet sheet, ExcelTable table, string labelColumnName, string graphName)
        {
            int columnIndexART = table.Columns["ART"].Position;
            ExcelRangeBase rangeART = table.WorkSheet.Cells[
                table.Address.Start.Row + 1,
                table.Address.Start.Column + columnIndexART,
                table.Address.End.Row,
                table.Address.Start.Column + columnIndexART];

            int columnIndexCPM = table.Columns["CPM"].Position;
            ExcelRangeBase rangeCPM = table.WorkSheet.Cells[
                table.Address.Start.Row + 1,
                table.Address.Start.Column + columnIndexCPM,
                table.Address.End.Row,
                table.Address.Start.Column + columnIndexCPM];

            int columnIndexLabels = table.Columns[labelColumnName].Position;
            ExcelRangeBase rangeLabels = table.WorkSheet.Cells[
                table.Address.Start.Row + 1,
                table.Address.Start.Column + columnIndexLabels,
                table.Address.End.Row,
                table.Address.Start.Column + columnIndexLabels];

            //Scatter plot of activities
            ExcelChart chart = sheet.Drawings.AddChart(graphName, eChartType.XYScatter);
            ExcelScatterChart chart1 = (ExcelScatterChart)chart;
            chart.SetPosition(0, 0, 2, 0);
            chart.SetSize(300, 300);
            chart.Legend.Remove();
            chart.YAxis.Title.Text = "ART";
            chart.YAxis.Title.Font.Size = 8;
            chart.XAxis.Title.Text = "CPM";
            chart.XAxis.Title.Font.Size = 8;
            chart.VaryColors = true;
            //chart1.BubbleScale = 50;

            ExcelChartSerie series = chart.Series.Add(rangeART, rangeCPM);
            ExcelScatterChartSerie series1 = (ExcelScatterChartSerie)series;
            series.Header = "ARTvsCPM";
            series1.DataLabel.ShowValue = true;
            series1.DataLabel.ShowCategory = true;
            series1.DataLabel.ShowValue = false;
            series1.DataLabel.ShowCategory = false;
            series1.DataLabel.Position = eLabelPosition.Top;
            series1.MarkerSize = 10;
            series1.Marker = eMarkerStyle.Diamond;

            #region Update scatter to include nice Tier labels

            // This is what the Chart looks looks like
            //<ser xmlns="http://schemas.openxmlformats.org/drawingml/2006/chart">
            //    <c:idx val="0" />
            //    <c:order val="0" />
            //    <c:tx>
            //        <c:v>ARTvsCPM</c:v>
            //    </c:tx>
            //    <c:dLbls>
            //        <c:dLblPos val="ctr" />
            //        <c:showLegendKey val="0" />
            //        <c:showVal val="1" />
            //        <c:showCatName val="1" />
            //        <c:showSerName val="0" />
            //        <c:showPercent val="0" />
            //        <c:showBubbleSize val="0" />
            //        <c:separator>
            //        </c:separator>
            //        <c:showLeaderLines val="0" />
            //        <c:extLst>   
            //            <c:ext uri="{CE6537A1-D6FC-4f65-9D91-7224C49458BB}" xmlns:c15="http://schemas.microsoft.com/office/drawing/2012/chart">  <<< Magic GUID!!!!! Ai caramba
            //                <c15:showDataLabelsRange val="1"/>   <<<< This is the thing that turns on the data labels range
            //                <c15:showLeaderLines val="0"/>
            //            </c:ext>
            //        </c:extLst>
            //    </c:dLbls>
            //    <c:spPr>
            //        <a:ln w="28575">
            //            <a:noFill />
            //        </a:ln>
            //    </c:spPr>
            //    <c:xVal>
            //        <c:numRef>
            //            <c:f>'5.Tiers.Hourly'!$F$19:$F$40</c:f>
            //        </c:numRef>
            //    </c:xVal>
            //    <c:yVal>
            //        <c:numRef>
            //            <c:f>'5.Tiers.Hourly'!$I$19:$I$40</c:f>
            //        </c:numRef>
            //    </c:yVal>
            //    <c:smooth val="0" />
            //    <c:extLst>
            //        <c:ext uri="{02D57815-91ED-43cb-92C2-25804820EDAC}" xmlns:c15="http://schemas.microsoft.com/office/drawing/2012/chart">   <<< Magic GUID!!!!! Ai caramba
            //            <c15:datalabelsRange>
            //                <c15:f>'5.Tiers.Hourly'!$C$19:$C$40</c15:f>  <<<<< This is what specifies the range
            //            </c15:datalabelsRange>
            //        </c:ext>
            //    </c:extLst>
            //</ser>

            XmlDocument chartXMLdoc = chart.ChartXml;
            XmlNamespaceManager manager = new XmlNamespaceManager(chartXMLdoc.NameTable);
            manager.AddNamespace("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            manager.AddNamespace("c15", "http://schemas.microsoft.com/office/drawing/2012/chart");

            XmlNode seriesXmlNode = chartXMLdoc.GetElementsByTagName("ser")[0];

            // Turn on labels
            // /ser/c:dLbls/c:extLst/c:ext/c15:showDataLabelsRange
            XmlNode labelXmlNode = seriesXmlNode.SelectSingleNode("c:dLbls", manager);
            XmlNode extLstXmlNode = chartXMLdoc.CreateElement("c:extLst", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            labelXmlNode.AppendChild(extLstXmlNode);
            XmlNode extXmlNode = chartXMLdoc.CreateElement("c:ext", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            XmlAttribute attribXmlAttribute = chartXMLdoc.CreateAttribute("uri");
            attribXmlAttribute.Value = "{CE6537A1-D6FC-4f65-9D91-7224C49458BB}";
            extXmlNode.Attributes.Append(attribXmlAttribute);
            extLstXmlNode.AppendChild(extXmlNode);
            XmlNode showDataLabelsRangeXmlNode = chartXMLdoc.CreateElement("showDataLabelsRange", "http://schemas.microsoft.com/office/drawing/2012/chart");
            attribXmlAttribute = chartXMLdoc.CreateAttribute("val");
            attribXmlAttribute.Value = "1";
            showDataLabelsRangeXmlNode.Attributes.Append(attribXmlAttribute);
            extXmlNode.AppendChild(showDataLabelsRangeXmlNode);

            // Specify label range
            // /ser/c:extLst/c:ext/c15:datalabelsRange/c15:f
            extLstXmlNode = chartXMLdoc.CreateElement("c:extLst", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            seriesXmlNode.AppendChild(extLstXmlNode);
            extXmlNode = chartXMLdoc.CreateElement("c:ext", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            attribXmlAttribute = chartXMLdoc.CreateAttribute("uri");
            attribXmlAttribute.Value = "{02D57815-91ED-43cb-92C2-25804820EDAC}";
            extXmlNode.Attributes.Append(attribXmlAttribute);
            extLstXmlNode.AppendChild(extXmlNode);
            XmlNode datalabelsRangeXmlNode = chartXMLdoc.CreateElement("datalabelsRange", "http://schemas.microsoft.com/office/drawing/2012/chart");
            extXmlNode.AppendChild(datalabelsRangeXmlNode);
            XmlNode fXmlNode = chartXMLdoc.CreateElement("f", "http://schemas.microsoft.com/office/drawing/2012/chart");
            fXmlNode.InnerText = rangeLabels.FullAddress;
            datalabelsRangeXmlNode.AppendChild(fXmlNode);

            #endregion
        }
    }
}
