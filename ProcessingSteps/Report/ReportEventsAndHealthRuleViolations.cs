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
    public class ReportEventsAndHealthRuleViolations : JobStepReportBase
    {
        #region Constants for Detected Events and Health Rule Violations Report contents

        private const string REPORT_DETECTED_EVENTS_SHEET_CONTROLLERS = "3.Controllers";
        private const string REPORT_DETECTED_EVENTS_SHEET_APPLICATIONS = "4.Applications";

        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS = "5.Events";
        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT = "5.Events.Type";
        private const string REPORT_DETECTED_EVENTS_SHEET_EVENTS_TIMELINE_PIVOT = "5.Events.Timeline";

        private const string REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS = "6.Health Rule Violations";
        private const string REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS_PIVOT = "6.Health Rule Violations.Type";

        private const string REPORT_DETECTED_EVENTS_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_DETECTED_EVENTS_TABLE_APPLICATIONS = "t_Applications";

        private const string REPORT_DETECTED_EVENTS_TABLE_TOC = "t_TOC";

        private const string REPORT_DETECTED_EVENTS_TABLE_EVENTS = "t_Events";
        private const string REPORT_DETECTED_EVENTS_TABLE_HEALTH_RULE_VIOLATION_EVENTS = "t_HealthRuleViolationEvents";

        private const string REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TYPE = "p_EventsType";
        private const string REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TIMELINE = "p_EventsTimeline";
        private const string REPORT_DETECTED_EVENTS_PIVOT_HEALTH_RULE_VIOLATION_EVENTS_TYPE = "p_HealthRuleViolationEventsType";

        private const string REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TYPE_GRAPH = "g_EventsType";
        private const string REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TIMELINE_GRAPH = "g_EventsTimeline";
        private const string REPORT_DETECTED_EVENTS_PIVOT_HEALTH_RULE_VIOLATION_EVENTS_TYPE_GRAPH = "g_HealthRuleViolationEventsType";

        private const int REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT = 14;

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
                loggerConsole.Info("Prepare Events and Health Rule Violations Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Events and Health Rule Violations Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Events and Health Rule Violations Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_CONTROLLERS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_APPLICATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Duration";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_EVENTS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_EVENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                loggerConsole.Info("Fill Events and Health Rule Violations Report File");

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_CONTROLLERS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllersReportFilePath(), 0, sheet, REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_APPLICATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationEventsReportFilePath(), 0, sheet, REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Events

                loggerConsole.Info("List of Events");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.EventsReportFilePath(), 0, sheet, REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Health Rule Violation Events

                loggerConsole.Info("List of Health Rule Violation Events");

                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.HealthRuleViolationsReportFilePath(), 0, sheet, REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Events and Health Rule Violations Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_CONTROLLERS];
                logger.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Controllers Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_CONTROLLERS);
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
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_APPLICATIONS];
                logger.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Applications Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_APPLICATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfEntityRowTableInMetricReport(EntityApplication.ENTITY_TYPE, sheet, table);

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEvents"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEvents"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEventsInfo"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEventsInfo"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEventsWarning"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEventsWarning"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEventsError"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEventsError"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHRViolations"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHRViolations"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHRViolationsWarning"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHRViolationsWarning"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHRViolationsCritical"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHRViolationsCritical"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Events

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS];
                logger.Info("Events Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Events Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_EVENTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EventID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Summary"].Position + 1).Width = 35;
                    sheet.Column(table.Columns["Type"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SubType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TriggeredEntityType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TriggeredEntityName"].Position + 1).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "Type");
                    addRowFieldToPivot(pivot, "SubType");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "NodeName");
                    addColumnFieldToPivot(pivot, "Severity", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EventID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                    sheet.Column(7).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_EVENTS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT + 3, 1], range, REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ApplicationName");
                    addFilterFieldToPivot(pivot, "TierName");
                    addFilterFieldToPivot(pivot, "BTName");
                    addFilterFieldToPivot(pivot, "TriggeredEntityName");
                    addFilterFieldToPivot(pivot, "ApplicationName");
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Severity", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "Type", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "SubType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EventID", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_DETECTED_EVENTS_PIVOT_EVENTS_TIMELINE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Health Rule Violation Events

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS];
                logger.Info("Health Rule Events Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Health Rule Events Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_DETECTED_EVENTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_HEALTH_RULE_VIOLATION_EVENTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EventID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["HealthRuleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_DETECTED_EVENTS_SHEET_HEALTH_RULE_VIOLATIONS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_DETECTED_EVENTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_DETECTED_EVENTS_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_DETECTED_EVENTS_PIVOT_HEALTH_RULE_VIOLATION_EVENTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "Status");
                    addRowFieldToPivot(pivot, "HealthRuleName");
                    addRowFieldToPivot(pivot, "EntityType");
                    addRowFieldToPivot(pivot, "EntityName");
                    addColumnFieldToPivot(pivot, "Severity", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EventID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_DETECTED_EVENTS_PIVOT_HEALTH_RULE_VIOLATION_EVENTS_TYPE_GRAPH, eChartType.ColumnClustered, pivot);
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
                table = sheet.Tables.Add(range, REPORT_DETECTED_EVENTS_TABLE_TOC);
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

                string reportFilePath = FilePathMap.EventsAndHealthRuleViolationsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            loggerConsole.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            logger.Trace("Output.Events={0}", jobConfiguration.Output.Events);
            loggerConsole.Trace("Output.Events={0}", jobConfiguration.Output.Events);
            if (jobConfiguration.Input.Events == false || jobConfiguration.Output.Events == false)
            {
                loggerConsole.Trace("Skipping report of events");
            }
            return (jobConfiguration.Input.Events == true && jobConfiguration.Output.Events == true);
        }
    }
}
