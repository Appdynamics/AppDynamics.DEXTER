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
using System.Reflection;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportApplicationEventsAndHealthRuleViolations : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS = "4.Applications";

        private const string SHEET_EVENTS = "5.Events";
        private const string SHEET_EVENTS_PIVOT = "5.Events.Type";
        private const string SHEET_EVENTS_TIMELINE_PIVOT = "5.Events.Timeline";

        private const string SHEET_EVENT_DETAILS = "6.Event Details";
        private const string SHEET_EVENT_DETAILS_PIVOT = "6.Event Details.Type";

        private const string SHEET_HEALTH_RULE_VIOLATIONS = "7.Health Rule Violations";
        private const string SHEET_HEALTH_RULE_VIOLATIONS_PIVOT = "7.Health Rule Violations.Type";

        private const string SHEET_AUDIT_EVENTS = "8.Audit Events";
        private const string SHEET_AUDIT_EVENTS_PIVOT = "8.Audit Events.Type";
        private const string AUDIT_SHEET_EVENTS_TIMELINE_PIVOT = "8.Audit Events.Timeline";

        private const string SHEET_NOTIFICATIONS = "9.Notifications";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS = "t_Applications";

        private const string TABLE_EVENTS = "t_Events";
        private const string TABLE_EVENT_DETAILS = "t_EventDetails";
        private const string TABLE_HEALTH_RULE_VIOLATION_EVENTS = "t_HealthRuleViolationEvents";
        private const string TABLE_AUDIT_EVENTS = "t_AuditEvents";
        private const string TABLE_NOTIFICATIONS = "t_Notifications";

        private const string PIVOT_EVENTS_TYPE = "p_EventsType";
        private const string PIVOT_EVENT_DETAILS_TYPE = "p_EventDetailsType";
        private const string PIVOT_EVENTS_TIMELINE = "p_EventsTimeline";
        private const string PIVOT_HEALTH_RULE_VIOLATION_EVENTS_TYPE = "p_HealthRuleViolationEventsType";
        private const string PIVOT_AUDIT_EVENTS_TYPE = "p_AuditEventsType";
        private const string PIVOT_AUDIT_EVENTS_TIMELINE = "p_AuditEventsTimeline";

        private const string GRAPH_EVENTS_TYPE = "g_EventsType";
        private const string GRAPH_EVENT_DETAILS_TYPE = "g_EventDetailsType";
        private const string GRAPH_EVENTS_TIMELINE = "g_EventsTimeline";
        private const string GRAPH_HEALTH_RULE_VIOLATION_EVENTS_TYPE = "g_HealthRuleViolationEventsType";
        private const string GRAPH_AUDIT_EVENTS_TYPE = "g_AuditEventsType";
        private const string GRAPH_AUDIT_EVENTS_TIMELINE = "g_AuditEventsTimeline";

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
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Events and Health Rule Violations Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_EVENTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EVENTS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Duration";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EVENTS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_EVENTS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EVENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_EVENTS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EVENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_EVENT_DETAILS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EVENT_DETAILS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_EVENT_DETAILS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EVENT_DETAILS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_HEALTH_RULE_VIOLATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_RULE_VIOLATIONS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_HEALTH_RULE_VIOLATIONS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_RULE_VIOLATIONS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_AUDIT_EVENTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_AUDIT_EVENTS_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Duration";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", AUDIT_SHEET_EVENTS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_AUDIT_EVENTS_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_AUDIT_EVENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(AUDIT_SHEET_EVENTS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_AUDIT_EVENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NOTIFICATIONS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                loggerConsole.Info("Fill Events and Health Rule Violations Report File");

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationEventsSummaryReportFilePath(), 0, typeof(ApplicationEventSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Events

                loggerConsole.Info("List of Events");

                sheet = excelReport.Workbook.Worksheets[SHEET_EVENTS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationEventsReportFilePath(), 0, typeof(Event), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Event Details

                sheet = excelReport.Workbook.Worksheets[SHEET_EVENT_DETAILS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationEventDetailsReportFilePath(), 0, typeof(EventDetail), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Health Rule Violation Events

                loggerConsole.Info("List of Health Rule Violation Events");

                sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_RULE_VIOLATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationHealthRuleViolationsReportFilePath(), 0, typeof(HealthRuleViolationEvent), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Audit Events

                loggerConsole.Info("List of Audit Events");

                sheet = excelReport.Workbook.Worksheets[SHEET_AUDIT_EVENTS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.AuditEventsReportFilePath(), 0, typeof(AuditEvent), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Notifications

                loggerConsole.Info("List of Notifications");

                sheet = excelReport.Workbook.Worksheets[SHEET_NOTIFICATIONS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NotificationsReportFilePath(), 0, typeof(Event), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Events and Health Rule Violations Report File");

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

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEvents"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEvents"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEventsInfo"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEventsInfo"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEventsWarning"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEventsWarning"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumEventsError"].Position + 1, sheet.Dimension.Rows, table.Columns["NumEventsError"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHRViolations"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHRViolations"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHRViolationsWarning"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHRViolationsWarning"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHRViolationsCritical"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHRViolationsCritical"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Events

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_EVENTS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_EVENTS);
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

                    sheet = excelReport.Workbook.Worksheets[SHEET_EVENTS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_EVENTS_TYPE);
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

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_EVENTS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;
                    sheet.Column(7).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_EVENTS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1], range, PIVOT_EVENTS_TIMELINE);
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

                    chart = sheet.Drawings.AddChart(GRAPH_EVENTS_TIMELINE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Event Details

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_EVENT_DETAILS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_EVENT_DETAILS);
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
                    sheet.Column(table.Columns["DetailAction"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DetailName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DetailValue"].Position + 1).Width = 25;

                    sheet = excelReport.Workbook.Worksheets[SHEET_EVENT_DETAILS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_EVENT_DETAILS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Severity");
                    addFilterFieldToPivot(pivot, "DetailAction");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "Type");
                    addRowFieldToPivot(pivot, "DetailName");
                    addColumnFieldToPivot(pivot, "DataType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EventID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_EVENT_DETAILS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Health Rule Violation Events

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_RULE_VIOLATIONS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_HEALTH_RULE_VIOLATION_EVENTS);
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

                    sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_RULE_VIOLATIONS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_HEALTH_RULE_VIOLATION_EVENTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "Status");
                    addRowFieldToPivot(pivot, "HealthRuleName");
                    addRowFieldToPivot(pivot, "EntityType");
                    addRowFieldToPivot(pivot, "EntityName");
                    addColumnFieldToPivot(pivot, "Severity", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EventID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_HEALTH_RULE_VIOLATION_EVENTS_TYPE, eChartType.ColumnClustered, pivot);
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

                #region Audit Events

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_AUDIT_EVENTS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_AUDIT_EVENTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Username"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LoginType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Action"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["EntityType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_AUDIT_EVENTS_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_AUDIT_EVENTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "Action");
                    addRowFieldToPivot(pivot, "EntityType");
                    addRowFieldToPivot(pivot, "EntityName");
                    addColumnFieldToPivot(pivot, "LoginType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "UserName", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EntityID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_AUDIT_EVENTS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 30;

                    sheet = excelReport.Workbook.Worksheets[AUDIT_SHEET_EVENTS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_AUDIT_EVENTS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "UserName");
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Action", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "EntityType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "EntityName", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "EntityID", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_AUDIT_EVENTS_TIMELINE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Notifications

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_NOTIFICATIONS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NOTIFICATIONS);
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
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

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

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.Events={0}", programOptions.LicensedReports.Events);
            loggerConsole.Trace("LicensedReports.Events={0}", programOptions.LicensedReports.Events);
            if (programOptions.LicensedReports.Events == false)
            {
                loggerConsole.Warn("Not licensed for events");
                return false;
            }

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
