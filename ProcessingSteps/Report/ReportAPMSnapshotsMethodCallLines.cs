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
    public class ReportAPMSnapshotsMethodCallLines : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS = "4.Applications";

        private const string SHEET_METHOD_CALL_LINES = "11.Method Calls";
        private const string SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT = "11.Calls.Type";
        private const string SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT = "11.Calls.Location";
        private const string SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT = "11.Calls.Timeline";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS = "t_Applications";

        private const string TABLE_METHOD_CALL_LINES = "t_MethodCallLines";

        private const string PIVOT_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE = "p_MethodCallLinesTypeAverage";
        private const string PIVOT_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE = "p_MethodCallLinesLocationAverage";
        private const string PIVOT_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE = "p_MethodCallLinesTimelineAverage";

        private const string GRAPH_METHOD_CALL_LINESTYPE_EXEC_AVERAGE = "g_MethodCallLinesTypeAverage";
        private const string GRAPH_METHOD_CALL_LINESLOCATION_EXEC_AVERAGE = "g_MethodCallLinesLocationAverage";
        private const string GRAPH_METHOD_CALL_LINESTIMELINE_EXEC_AVERAGE = "g_MethodCallLinesTimelineAverage";

        private const int LIST_SHEET_START_TABLE_AT = 4;
        private const int PIVOT_SHEET_START_PIVOT_AT = 8;
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
                loggerConsole.Info("Prepare Snapshots Method Calls Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Snapshots Method Call Lines Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Snapshots Method Call Lines Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivot

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CONTROLLERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_METHOD_CALL_LINES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Location";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[4, 1].Value = "See Timeline";
                sheet.Cells[4, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[4, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METHOD_CALL_LINES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METHOD_CALL_LINES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_METHOD_CALL_LINES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 9, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                loggerConsole.Info("Fill Snapshots Method Call Lines Report File");

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationSnapshotsReportFilePath(), 0, typeof(APMApplication), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Method Call Lines

                loggerConsole.Info("List of Method Call Lines");

                sheet = excelReport.Workbook.Worksheets[SHEET_METHOD_CALL_LINES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsMethodCallLinesReportFilePath(), 0, typeof(MethodCallLine),sheet, LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                loggerConsole.Info("Finalize Snapshots Method Call Lines Report File");

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

                #region Applications

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshots"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshots"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsNormal"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsNormal"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsVerySlow"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsVerySlow"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsStall"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsStall"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsSlow"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsSlow"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsError"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsError"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Method Call Lines

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_METHOD_CALL_LINES];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT + 1)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT + 1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_METHOD_CALL_LINES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    try
                    {
                        sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["SegmentUserExperience"].Position + 1).Width = 10;
                        sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                        sheet.Column(table.Columns["RequestID"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                        sheet.Column(table.Columns["Type"].Position + 1).Width = 10;
                        sheet.Column(table.Columns["Framework"].Position + 1).Width = 15;
                        sheet.Column(table.Columns["FullNameIndent"].Position + 1).Width = 45;
                        sheet.Column(table.Columns["ExitCalls"].Position + 1).Width = 15;
                        sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;
                    }
                    catch (OutOfMemoryException ex)
                    {
                        // Do nothing, we must have a lot of cells
                        logger.Warn("Ran out of memory due to too many rows/cells");
                        logger.Warn(ex);
                    }

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    sheet = excelReport.Workbook.Worksheets[SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1], range, PIVOT_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ElementType");
                    addFilterFieldToPivot(pivot, "NumChildren", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumExits", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Depth", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ExecRange", eSortType.Ascending, true);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "FullName");
                    addColumnFieldToPivot(pivot, "Type", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Framework", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Exec", DataFieldFunctions.Average);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_METHOD_CALL_LINESTYPE_EXEC_AVERAGE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ElementType");
                    addFilterFieldToPivot(pivot, "NumChildren", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumExits", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Depth", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Type");
                    addRowFieldToPivot(pivot, "Framework");
                    addRowFieldToPivot(pivot, "FullName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addColumnFieldToPivot(pivot, "ExecRange", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Exec", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_METHOD_CALL_LINESLOCATION_EXEC_AVERAGE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                    sheet.Column(7).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 6, 1], range, PIVOT_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ElementType");
                    addFilterFieldToPivot(pivot, "NumChildren", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumExits", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Depth", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Class", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Method", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "FullName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ExecRange", eSortType.Ascending, true);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Type", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Framework", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Exec", DataFieldFunctions.Average);

                    chart = sheet.Drawings.AddChart(GRAPH_METHOD_CALL_LINESTIMELINE_EXEC_AVERAGE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.SnapshotMethodCallsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.Snapshots={0}", programOptions.LicensedReports.Snapshots);
            loggerConsole.Trace("LicensedReports.Snapshots={0}", programOptions.LicensedReports.Snapshots);
            if (programOptions.LicensedReports.Snapshots == false)
            {
                loggerConsole.Warn("Not licensed for snapshots");
                return false;
            }

            logger.Trace("Input.Snapshots={0}", jobConfiguration.Input.Snapshots);
            loggerConsole.Trace("Input.Snapshots={0}", jobConfiguration.Input.Snapshots);
            logger.Trace("Output.Snapshots={0}", jobConfiguration.Output.Snapshots);
            loggerConsole.Trace("Output.Snapshots={0}", jobConfiguration.Output.Snapshots);
            if (jobConfiguration.Input.Snapshots == false || jobConfiguration.Output.Snapshots == false)
            {
                loggerConsole.Trace("Skipping report of snapshot method call lines");
            }
            return (jobConfiguration.Input.Snapshots == true && jobConfiguration.Output.Snapshots == true);
        }
    }
}
