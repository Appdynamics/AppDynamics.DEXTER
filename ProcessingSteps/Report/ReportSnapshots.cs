using AppDynamics.Dexter.DataObjects;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportSnapshots : JobStepReportBase
    {
        #region Constants for Snapshots Report contents

        private const string REPORT_SNAPSHOTS_SHEET_CONTROLLERS = "3.Controllers";
        private const string REPORT_SNAPSHOTS_SHEET_APPLICATIONS = "4.Applications";

        private const string REPORT_SNAPSHOTS_SHEET_SNAPSHOTS = "5.Snapshots";
        private const string REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TYPE_PIVOT = "5.Snapshots.Type";
        private const string REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TIMELINE_PIVOT = "5.Snapshots.Timeline";

        private const string REPORT_SNAPSHOTS_SHEET_SEGMENTS = "6.Segments";
        private const string REPORT_SNAPSHOTS_SHEET_SEGMENTS_TYPE_PIVOT = "6.Segments.Type";
        private const string REPORT_SNAPSHOTS_SHEET_SEGMENTS_TIMELINE_PIVOT = "6.Segments.Timeline";

        private const string REPORT_SNAPSHOTS_SHEET_EXIT_CALLS = "7.Exit Calls";
        private const string REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TYPE_PIVOT = "7.Exit Calls.Type";
        private const string REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TIMELINE_PIVOT = "7.Exit Calls.Timeline";

        private const string REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS = "8.SEP Calls";
        private const string REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT = "8.SEP Calls.Type";
        private const string REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT = "8.SEP.Timeline";

        private const string REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS = "9.Errors";
        private const string REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TYPE_PIVOT = "9.Errors.Type";
        private const string REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TIMELINE_PIVOT = "9.Errors.Timeline";

        private const string REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA = "10.Business Data";
        private const string REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TYPE_PIVOT = "10.Business Data.Type";
        private const string REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TIMELINE_PIVOT = "10.Business Data.Timeline";

        private const string REPORT_SNAPSHOTS_TABLE_TOC = "t_TOC";

        private const string REPORT_SNAPSHOTS_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_SNAPSHOTS_TABLE_APPLICATIONS = "t_Applications";

        private const string REPORT_SNAPSHOTS_TABLE_SNAPSHOTS = "t_Snapshots";
        private const string REPORT_SNAPSHOTS_TABLE_SEGMENTS = "t_Segments";
        private const string REPORT_SNAPSHOTS_TABLE_EXIT_CALLS = "t_ExitCalls";
        private const string REPORT_SNAPSHOTS_TABLE_SERVICE_ENDPOINT_CALLS = "t_ServiceEndpointCalls";
        private const string REPORT_SNAPSHOTS_TABLE_DETECTED_ERRORS = "t_DetectedErrors";
        private const string REPORT_SNAPSHOTS_TABLE_BUSINESS_DATA = "t_BusinessData";

        private const string REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS = "p_Snapshots";
        private const string REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS_TIMELINE = "p_SnapshotsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_SEGMENTS = "p_Segments";
        private const string REPORT_SNAPSHOTS_PIVOT_SEGMENTS_TIMELINE = "p_SegmentsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS = "p_ServiceEndpointCalls";
        private const string REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS_TIMELINE = "p_ServiceEndpointCallsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS = "p_ExitCalls";
        private const string REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS_TIMELINE = "p_ExitCallsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS = "p_DetectedErrors";
        private const string REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS_TIMELINE = "p_DetectedErrorsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA = "p_BusinessData";
        private const string REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA_TIMELINE = "p_BusinessDataTimeline";

        private const string REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS_GRAPH = "g_Snapshots";
        private const string REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS_TIMELINE_GRAPH = "g_SnapshotsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_SEGMENTS_GRAPH = "g_Segments";
        private const string REPORT_SNAPSHOTS_PIVOT_SEGMENTS_TIMELINE_GRAPH = "g_SegmentsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS_GRAPH = "g_ServiceEndpointCalls";
        private const string REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS_TIMELINE_GRAPH = "g_ServiceEndpointCallsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS_GRAPH = "g_ExitCalls";
        private const string REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS_TIMELINE_GRAPH = "g_ExitCallsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS_GRAPH = "g_DetectedErrors";
        private const string REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS_TIMELINE_GRAPH = "g_DetectedErrorsTimeline";
        private const string REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA_GRAPH = "g_BusinessData";
        private const string REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA_TIMELINE_GRAPH = "g_BusinessDataTimeline";

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

            try
            {
                loggerConsole.Info("Prepare Snapshots Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Snapshots Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                var timelineStyle = sheet.Workbook.Styles.CreateNamedStyle("TimelineStyle");
                timelineStyle.Style.Font.Name = "Consolas";
                timelineStyle.Style.Font.Size = 8;

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Snapshots Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SNAPSHOTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SNAPSHOTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SNAPSHOTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 6, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SEGMENTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SEGMENTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SEGMENTS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SEGMENTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SEGMENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SEGMENTS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SEGMENTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 6, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_EXIT_CALLS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_EXIT_CALLS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_EXIT_CALLS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Timeline";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TIMELINE_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                loggerConsole.Info("Fill Snapshots Report File");

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

                #region Snapshots

                loggerConsole.Info("List of Snapshots");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SNAPSHOTS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Segments

                loggerConsole.Info("List of Segments");
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SEGMENTS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsSegmentsReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Exit Calls

                loggerConsole.Info("List of Exit Calls");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_EXIT_CALLS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsExitCallsReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Service Endpoint Calls

                loggerConsole.Info("List of Service Endpoint Calls");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsServiceEndpointCallsReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Detected Errors

                loggerConsole.Info("List of Detected Errors");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsDetectedErrorsCallsReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Data

                loggerConsole.Info("List of Business Data");

                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotsBusinessDataReportFilePath(), 0, sheet, REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Snapshots Report File");

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

                #region Snapshots

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SNAPSHOTS];
                logger.Info("Snapshots Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Snapshots Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_SNAPSHOTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["RequestID"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DurationRange"].Position + 1).Width = 15;

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["UserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["UserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    ExcelAddress cfAddressDuration = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["Duration"].Position + 1, sheet.Dimension.Rows, table.Columns["Duration"].Position + 1);
                    var cfDuration = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressDuration);
                    cfDuration.LowValue.Color = colorGreenFor3ColorScales;
                    cfDuration.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfDuration.MiddleValue.Value = 70;
                    cfDuration.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfDuration.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallGraphs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallGraphs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledBackends"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledTiers"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledApplications"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToBackends"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToTiers"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToApplications"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasErrors");
                    addFilterFieldToPivot(pivot, "CallGraphType");
                    addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "RequestID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SNAPSHOTS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 4, 1], range, REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "HasErrors");
                    addFilterFieldToPivot(pivot, "CallGraphType");
                    addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_SNAPSHOTS_TIMELINE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region Segments

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SEGMENTS];
                logger.Info("Segments Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Segments Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_SEGMENTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["FromSegmentID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["FromTierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;

                    // Make timeline fixed width
                    ExcelRangeBase rangeTimeline = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["Timeline"].Position + 1, sheet.Dimension.Rows, table.Columns["Timeline"].Position + 1];
                    rangeTimeline.StyleName = "TimelineStyle";

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["UserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["UserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    ExcelAddress cfAddressDuration = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["Duration"].Position + 1, sheet.Dimension.Rows, table.Columns["Duration"].Position + 1);
                    var cfDuration = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressDuration);
                    cfDuration.LowValue.Color = colorGreenFor3ColorScales;
                    cfDuration.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfDuration.MiddleValue.Value = 70;
                    cfDuration.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfDuration.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledBackends"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledTiers"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledApplications"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToBackends"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToTiers"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToApplications"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SEGMENTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_SNAPSHOTS_PIVOT_SEGMENTS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasErrors");
                    addFilterFieldToPivot(pivot, "CallGraphType");
                    addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_SEGMENTS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SEGMENTS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 4, 1], range, REPORT_SNAPSHOTS_PIVOT_SEGMENTS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "HasErrors");
                    addFilterFieldToPivot(pivot, "CallGraphType");
                    addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_SEGMENTS_TIMELINE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region Exit Calls

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_EXIT_CALLS];
                logger.Info("Exit Calls Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Exit Calls Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_EXIT_CALLS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SegmentUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["ToEntityName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExitType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["Detail"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Method"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToSegmentID"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    ExcelAddress cfAddressDuration = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["Duration"].Position + 1, sheet.Dimension.Rows, table.Columns["Duration"].Position + 1);
                    var cfDuration = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressDuration);
                    cfDuration.LowValue.Color = colorGreenFor3ColorScales;
                    cfDuration.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfDuration.MiddleValue.Value = 70;
                    cfDuration.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfDuration.HighValue.Color = colorRedFor3ColorScales;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ToEntityType");
                    addFilterFieldToPivot(pivot, "ToEntityName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "RequestID");
                    addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "ExitType");
                    addRowFieldToPivot(pivot, "Detail");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addDataFieldToPivot(pivot, "RequestID", DataFieldFunctions.Count, "Calls");
                    addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average, "Average");

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                    sheet.Column(6).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_EXIT_CALLS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 5, 1], range, REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ToEntityType");
                    addFilterFieldToPivot(pivot, "ToEntityName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Detail", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "ExitType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_EXIT_CALLS_TIMELINE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region Service Endpoint Calls

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS];
                logger.Info("Service Endpoint Calls Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Service Endpoint Calls Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_SERVICE_ENDPOINT_CALLS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SegmentUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT - 2, 1], range, REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "RequestID");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "SEPName");
                    addColumnFieldToPivot(pivot, "SEPType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "SEPType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_SERVICE_ENDPOINT_CALLS_TIMELINE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region Detected Errors

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS];
                logger.Info("Detected Errors Sheet ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Detected Errors Sheet ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_DETECTED_ERRORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SegmentUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["ErrorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ErrorMessage"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ErrorDetail"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT - 2, 1], range, REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "RequestID");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "ErrorMessage");
                    addColumnFieldToPivot(pivot, "ErrorType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_DETECTED_ERRORS_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 5, 1], range, REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ErrorName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ErrorMessage", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ErrorDetail", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "ErrorType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_DETECTED_ERRORS_TIMELINE_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region Business Data

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA];
                logger.Info("Detected Business Data ({0} rows)", sheet.Dimension.Rows);
                loggerConsole.Info("Detected Business Data ({0} rows)", sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_SNAPSHOTS_TABLE_BUSINESS_DATA);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SegmentUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["DataName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DataValue"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DataType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SegmentUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SegmentUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    cfAddressUserExperience = new ExcelAddress(REPORT_SNAPSHOTS_LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                    addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT - 2, 1], range, REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "RequestID");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addRowFieldToPivot(pivot, "DataName");
                    addColumnFieldToPivot(pivot, "DataType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[REPORT_SNAPSHOTS_SHEET_BUSINESS_DATA_TIMELINE_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_SNAPSHOTS_PIVOT_SHEET_START_PIVOT_AT + REPORT_SNAPSHOTS_PIVOT_SHEET_CHART_HEIGHT + 3, 1], range, REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "DataName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "DataValue", eSortType.Ascending);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "DataType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(REPORT_SNAPSHOTS_PIVOT_BUSINESS_DATA_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
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

                string reportFilePath = FilePathMap.SnapshotsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
                loggerConsole.Trace("Skipping report of snapshots");
            }
            return (jobConfiguration.Input.Snapshots == true && jobConfiguration.Output.Snapshots == true);
        }
    }
}
