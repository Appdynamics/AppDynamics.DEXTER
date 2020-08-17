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
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMMetricsList : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_METRICS_SUMMARY_LIST = "5.Metrics.Summary";
        private const string SHEET_METRICS_SUMMARY_TYPE_PIVOT = "5.Metrics.Summary.Type";
        private const string SHEET_METRICS_LIST = "6.Metrics";
        private const string SHEET_METRICS_TYPE_PIVOT = "6.Metrics.Type";
        private const string SHEET_METRICS_BUSINESS_TRANSACTIONS_PIVOT = "6.Metrics.BT";
        private const string SHEET_METRICS_TIER_PIVOT = "6.Metrics.Tier";
        private const string SHEET_METRICS_NODE_PIVOT = "6.Metrics.Node";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_METRICS_SUMMARY_LIST = "t_Metrics_Summary";
        private const string TABLE_METRICS_LIST = "t_Metrics_List";

        private const string PIVOT_METRICS_SUMMARY_TYPE = "p_Metrics_Summary";
        private const string PIVOT_METRICS_LIST_TYPE = "p_Metrics_List";
        private const string PIVOT_METRICS_BUSINESS_TRANSACTIONS = "p_Metrics_BusinessTransactions";
        private const string PIVOT_METRICS_TIER = "p_Metrics_Tier";
        private const string PIVOT_METRICS_NODE = "p_Metrics_Node";

        private const string GRAPH_METRICS_SUMMARY_TYPE = "g_Metrics_Summary";
        private const string GRAPH_METRICS_BUSINESS_TRANSACTIONS = "g_Metrics_BusinessTransactions";
        private const string GRAPH_METRICS_TIER = "g_Metrics_Tier";
        private const string GRAPH_METRICS_NODE = "g_Metrics_Node";

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

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
            {
                logger.Warn("No {0} targets to process", APPLICATION_TYPE_APM);
                loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_APM);

                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Detected APM Metrics Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics Detected APM Metrics Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics Detected APM Metrics Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_SUMMARY_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_SUMMARY_TYPE_PIVOT);
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_SUMMARY_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_SUMMARY_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                loggerConsole.Info("Fill Detected APM Metrics Report File");

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

                #region Metric Summary

                loggerConsole.Info("Metrics Summary");

                sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_SUMMARY_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.MetricPrefixSummaryReportFilePath(), 0, typeof(MetricSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Detected APM Metrics Report File");

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

                #region Metrics Summary

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_SUMMARY_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_METRICS_SUMMARY_LIST);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MetricPrefix"].Position + 1).Width = 30;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_SUMMARY_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_METRICS_SUMMARY_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "MetricPrefix");
                    addDataFieldToPivot(pivot, "NumAll", DataFieldFunctions.Sum, "All");
                    addDataFieldToPivot(pivot, "NumActivity", DataFieldFunctions.Sum, "Activity");
                    addDataFieldToPivot(pivot, "NumNoActivity", DataFieldFunctions.Sum, "NoActivity");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 30;

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_METRICS_SUMMARY_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.MetricsListExcelReportFilePath(jobConfiguration.Input.TimeRange);
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

                #region Prepare individual application reports

                ParallelOptions parallelOptions = new ParallelOptions();
                if (programOptions.ProcessSequentially == true)
                {
                    parallelOptions.MaxDegreeOfParallelism = 1;
                }

                int j = 0;
                Parallel.ForEach(
                    jobConfiguration.Target,
                    parallelOptions,
                    () => 0,
                    (jobTarget, loop, subtotal) =>
                    {
                        if (jobTarget.Type == APPLICATION_TYPE_APM)
                        {
                            createMetricListApplicationReport(programOptions, jobConfiguration, jobTarget);
                        }
                        return 1;
                    },
                    (finalResult) =>
                    {
                        Interlocked.Add(ref j, finalResult);
                        if (j % 10 == 0)
                        {
                            Console.Write("[{0}].", j);
                        }
                    }
                );

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
            logger.Trace("LicensedReports.MetricsList={0}", programOptions.LicensedReports.MetricsList);
            loggerConsole.Trace("LicensedReports.MetricsList={0}", programOptions.LicensedReports.MetricsList);
            if (programOptions.LicensedReports.MetricsList == false)
            {
                loggerConsole.Warn("Not licensed for list of metrics");
                return false;
            }

            logger.Trace("Input.MetricsList={0}", jobConfiguration.Input.MetricsList);
            loggerConsole.Trace("Input.MetricsList={0}", jobConfiguration.Input.MetricsList);
            logger.Trace("Output.MetricsList={0}", jobConfiguration.Output.MetricsList);
            loggerConsole.Trace("Output.MetricsList={0}", jobConfiguration.Output.MetricsList);
            if (jobConfiguration.Input.MetricsList == false && jobConfiguration.Output.MetricsList == false)
            {
                loggerConsole.Trace("Skipping report of list of metrics");
            }
            return (jobConfiguration.Input.MetricsList == true && jobConfiguration.Output.MetricsList == true);
        }

        private bool createMetricListApplicationReport(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget)
        {
            loggerConsole.Info("Prepare Detected APM Metrics Report File for {0}({1})", jobTarget.Application, jobTarget.ApplicationID);

            #region Prepare the report package

            // Prepare package
            ExcelPackage excelReport = new ExcelPackage();
            excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
            excelReport.Workbook.Properties.Title = "AppDynamics Detected APM Metrics Report";
            excelReport.Workbook.Properties.Subject = programOptions.JobName;

            excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

            #endregion

            #region Parameters sheet

            // Parameters sheet
            ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

            var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
            hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
            hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

            fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics Detected APM Metrics Report");

            int l = sheet.Dimension.Rows + 2;
            sheet.Cells[l, 1].Value = "Type";
            sheet.Cells[l, 2].Value = "Application";
            l++;
            sheet.Cells[l, 1].Value = "Name";
            sheet.Cells[l, 2].Value = jobTarget.Application;
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

            sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_SUMMARY_LIST);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

            sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_LIST);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[2, 1].Value = "See Pivot";
            sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_TYPE_PIVOT);
            sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[3, 1].Value = "See BTs";
            sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_BUSINESS_TRANSACTIONS_PIVOT);
            sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[3, 3].Value = "See Tiers";
            sheet.Cells[3, 4].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_TIER_PIVOT);
            sheet.Cells[3, 4].StyleName = "HyperLinkStyle";
            sheet.Cells[3, 5].Value = "See Nodes";
            sheet.Cells[3, 6].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_NODE_PIVOT);
            sheet.Cells[3, 6].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

            sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_TYPE_PIVOT);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[2, 1].Value = "See Table";
            sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_LIST);
            sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 9, 1);

            sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_BUSINESS_TRANSACTIONS_PIVOT);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[2, 1].Value = "See Table";
            sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_LIST);
            sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

            sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_TIER_PIVOT);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[2, 1].Value = "See Table";
            sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_LIST);
            sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

            sheet = excelReport.Workbook.Worksheets.Add(SHEET_METRICS_NODE_PIVOT);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.Cells[2, 1].Value = "See Table";
            sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METRICS_LIST);
            sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

            #endregion

            loggerConsole.Info("Fill Detected APM Metrics Report File for {0}({1})", jobTarget.Application, jobTarget.ApplicationID);

            #region Report file variables

            ExcelRangeBase range = null;
            ExcelTable table = null;

            #endregion

            #region Controllers

            loggerConsole.Info("List of Controllers");

            sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
            EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

            #endregion

            #region Metrics Summary

            loggerConsole.Info("Metric Summary");

            sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_SUMMARY_LIST];
            EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.MetricPrefixSummaryIndexFilePath(jobTarget), 0, typeof(MetricSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

            #endregion

            #region List of Metrics

            loggerConsole.Info("List of Metrics");

            sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_LIST];
            EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.MetricsListApplicationReportFilePath(jobTarget), 0, typeof(Metric), sheet, LIST_SHEET_START_TABLE_AT, 1);

            #endregion

            loggerConsole.Info("Finalize Detected APM Metrics Report File for {0}({1})", jobTarget.Application, jobTarget.ApplicationID);

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

            #region Metric Summary

            // Make table
            sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_SUMMARY_LIST];
            logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
            loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
            if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
            {
                range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, TABLE_METRICS_SUMMARY_LIST);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["MetricPrefix"].Position + 1).Width = 30;
            }

            #endregion

            #region List of Metrics

            // Make table
            sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_LIST];
            logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
            loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
            if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
            {
                range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, TABLE_METRICS_LIST);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                sheet.Column(table.Columns["MetricName"].Position + 1).Width = 20;
                for (int i = 1; i <= 22; i++)
                {
                    sheet.Column(table.Columns[String.Format("Segment{0}", i)].Position + 1).Width = 20;
                }
                sheet.Column(table.Columns["MetricPath"].Position + 1).Width = 30;

                // Make pivot
                sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_TYPE_PIVOT];
                ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 7, 1], range, PIVOT_METRICS_LIST_TYPE);
                setDefaultPivotTableSettings(pivot);
                addFilterFieldToPivot(pivot, "NumSegments", eSortType.Ascending, true);
                addFilterFieldToPivot(pivot, "EntityName", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "EntityType", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "TierAgentType", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "BTType", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "BackendName", eSortType.Ascending);
                addFilterFieldToPivot(pivot, "BackendType", eSortType.Ascending);
                addRowFieldToPivot(pivot, "Controller");
                addRowFieldToPivot(pivot, "ApplicationName");
                addRowFieldToPivot(pivot, "Segment1");
                addRowFieldToPivot(pivot, "Segment2");
                addRowFieldToPivot(pivot, "Segment3");
                addRowFieldToPivot(pivot, "Segment4");
                addRowFieldToPivot(pivot, "Segment5");
                addRowFieldToPivot(pivot, "Segment6");
                addRowFieldToPivot(pivot, "Segment7");
                addRowFieldToPivot(pivot, "Segment8");
                addRowFieldToPivot(pivot, "Segment9");
                addRowFieldToPivot(pivot, "Segment10");
                addColumnFieldToPivot(pivot, "HasActivity");
                addDataFieldToPivot(pivot, "MetricID", DataFieldFunctions.Count, "Count");

                sheet.Column(1).Width = 20;
                sheet.Column(2).Width = 20;
                sheet.Column(3).Width = 20;
                sheet.Column(4).Width = 20;
                sheet.Column(5).Width = 20;
                sheet.Column(6).Width = 20;
                sheet.Column(7).Width = 10;
                sheet.Column(8).Width = 10;
                sheet.Column(9).Width = 10;
                sheet.Column(10).Width = 10;
                sheet.Column(11).Width = 10;
                sheet.Column(12).Width = 10;

                sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_BUSINESS_TRANSACTIONS_PIVOT];
                pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_METRICS_BUSINESS_TRANSACTIONS);
                setDefaultPivotTableSettings(pivot);
                addFilterFieldToPivot(pivot, "NumSegments", eSortType.Ascending, true);
                addRowFieldToPivot(pivot, "BTType");
                addRowFieldToPivot(pivot, "BTName");
                addColumnFieldToPivot(pivot, "HasActivity");
                addDataFieldToPivot(pivot, "MetricID", DataFieldFunctions.Count, "Count");

                ExcelChart chart = sheet.Drawings.AddChart(GRAPH_METRICS_BUSINESS_TRANSACTIONS, eChartType.ColumnClustered, pivot);
                chart.SetPosition(2, 0, 0, 0);
                chart.SetSize(800, 300);

                sheet.Column(1).Width = 20;
                sheet.Column(2).Width = 20;

                sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_TIER_PIVOT];
                pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_METRICS_TIER);
                setDefaultPivotTableSettings(pivot);
                addFilterFieldToPivot(pivot, "NumSegments", eSortType.Ascending, true);
                addRowFieldToPivot(pivot, "TierAgentType");
                addRowFieldToPivot(pivot, "TierName");
                addColumnFieldToPivot(pivot, "HasActivity");
                addDataFieldToPivot(pivot, "MetricID", DataFieldFunctions.Count, "Count");

                chart = sheet.Drawings.AddChart(GRAPH_METRICS_TIER, eChartType.ColumnClustered, pivot);
                chart.SetPosition(2, 0, 0, 0);
                chart.SetSize(800, 300);

                sheet.Column(1).Width = 20;
                sheet.Column(2).Width = 20;

                sheet = excelReport.Workbook.Worksheets[SHEET_METRICS_NODE_PIVOT];
                pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_METRICS_NODE);
                setDefaultPivotTableSettings(pivot);
                addFilterFieldToPivot(pivot, "NumSegments", eSortType.Ascending, true);
                addRowFieldToPivot(pivot, "NodeAgentType");
                addRowFieldToPivot(pivot, "NodeName");
                addColumnFieldToPivot(pivot, "HasActivity");
                addDataFieldToPivot(pivot, "MetricID", DataFieldFunctions.Count, "Count");

                chart = sheet.Drawings.AddChart(GRAPH_METRICS_NODE, eChartType.ColumnClustered, pivot);
                chart.SetPosition(2, 0, 0, 0);
                chart.SetSize(800, 300);

                sheet.Column(1).Width = 20;
                sheet.Column(2).Width = 20;

            }

            #endregion

            #region TOC sheet

            // TOC sheet again
            sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
            fillTableOfContentsSheet(sheet, excelReport);

            #endregion

            #region Save file 

            string reportFilePath = FilePathMap.MetricsListApplicationExcelReportFilePath(jobTarget, true);
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
    }
}
