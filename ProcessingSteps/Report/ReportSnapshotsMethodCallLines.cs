using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportSnapshotsMethodCallLines : JobStepReportBase
    {
        #region Constants for Snapshots Report contents

        private const string REPORT_SNAPSHOTS_SHEET_CONTROLLERS = "3.Controllers";
        private const string REPORT_SNAPSHOTS_SHEET_APPLICATIONS = "4.Applications";

        private const string REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES = "11.Method Calls";
        private const string REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT = "11.Calls.Type";
        private const string REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT = "11.Calls.Location";
        private const string REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT = "11.Calls.Timeline";

        private const string REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES = "12.Call Occurrences";
        private const string REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES_TYPE_PIVOT = "12.Call Occurrences.Type";

        private const string REPORT_SNAPSHOTS_TABLE_TOC = "t_TOC";

        private const string REPORT_SNAPSHOTS_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_SNAPSHOTS_TABLE_APPLICATIONS = "t_Applications";

        private const string REPORT_SNAPSHOTS_TABLE_METHOD_CALL_LINES = "t_MethodCallLines";
        private const string REPORT_SNAPSHOTS_TABLE_METHOD_CALL_LINES_OCCURRENCES = "t_MethodCallLinesOccurrences";

        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE = "p_MethodCallLinesTypeAverage";
        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE = "p_MethodCallLinesLocationAverage";
        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE = "p_MethodCallLinesTimelineAverage";

        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_OCCURRENCES_TYPE = "p_MethodCallLinesOccurrencesType";

        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_GRAPH_TYPE_EXEC_AVERAGE = "g_MethodCallLinesTypeAverage";
        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_GRAPH_LOCATION_EXEC_AVERAGE = "g_MethodCallLinesLocationAverage";
        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_GRAPH_TIMELINE_EXEC_AVERAGE = "g_MethodCallLinesTimelineAverage";

        private const string REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_OCCURRENCES_GRAPH_TYPE = "g_MethodCallLinesOccurrencesType";

        private const int REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT = 8;
        private const int REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT = 14;

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
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Snapshots Method Call Lines Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivot

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_CONTROLLERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_APPLICATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Location";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[4, 1].Value = "See Timeline";
                sheet.Cells[4, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[4, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 9, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                loggerConsole.Info("Fill Snapshots Method Call Lines Report File");

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_CONTROLLERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllersReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_APPLICATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationSnapshotsReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Method Call Lines

                loggerConsole.Info("List of Method Call Lines");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsMethodCallLinesReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                #region Method Call Occurrences

                loggerConsole.Info("List of Method Call Occurrences");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsMethodCallLinesOccurrencesReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Snapshots Method Call Lines Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_CONTROLLERS];
                logger.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_CONTROLLERS);
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
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_APPLICATIONS];
                logger.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_APPLICATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(EntityApplication.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshots"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshots"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsNormal"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsNormal"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsVerySlow"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsVerySlow"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsStall"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsStall"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsSlow"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsSlow"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSnapshotsError"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSnapshotsError"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Method Call Lines

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES];
                logger.Info("Method Call Lines Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Method Call Lines Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_METHOD_CALL_LINES);
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
                    }

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 2, 1], range, REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ElementType");
                    addFilterFieldToPivot(pivot, "NumChildren", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumExits", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Depth", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ExecRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "FullName");
                    addColumnFieldToPivot(pivot, "Type", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Framework", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Exec", DataFieldFunctions.Average);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_GRAPH_TYPE_EXEC_AVERAGE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE);
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

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_GRAPH_LOCATION_EXEC_AVERAGE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                    sheet.Column(7).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 6, 1], range, REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ElementType");
                    addFilterFieldToPivot(pivot, "NumChildren", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumExits", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Depth", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Class", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Method", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "FullName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ExecRange", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Type", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Framework", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Exec", DataFieldFunctions.Average);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_GRAPH_TIMELINE_EXEC_AVERAGE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region Method Call Occurrences

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES];
                logger.Info("Method Call Occurrences Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Method Call Occurrences Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_METHOD_CALL_LINES_OCCURRENCES);
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
                        sheet.Column(table.Columns["FullName"].Position + 1).Width = 45;
                    }
                    catch (OutOfMemoryException ex)
                    {
                        // Do nothing, we must have a lot of cells
                    }

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_METHOD_CALL_LINES_OCCURRENCES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_OCCURRENCES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "NumChildren", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumExits", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ExecRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "NumCalls", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "FullName", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Type", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Framework", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Exec", DataFieldFunctions.Average);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_METHOD_CALL_LINES_OCCURRENCES_GRAPH_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
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
                        table = s.Tables[0];
                        sheet.Cells[rowNum, 2].Value = table.Address.Rows - 1;
                    }
                }
                range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_TOC);
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
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
