using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using CsvHelper;
using CsvHelper.Configuration;
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
using System.Runtime.CompilerServices;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMIndividualSnapshots : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS = "4.Applications";

        private const string SHEET_SNAPSHOTS = "5.Snapshots";
        private const string SHEET_SNAPSHOTS_TYPE_PIVOT = "5.Snapshots.Type";
        private const string SHEET_SNAPSHOTS_TIMELINE_PIVOT = "5.Snapshots.Timeline";

        private const string SHEET_SEGMENTS = "6.Segments";
        private const string SHEET_SEGMENTS_TYPE_PIVOT = "6.Segments.Type";
        private const string SHEET_SEGMENTS_TIMELINE_PIVOT = "6.Segments.Timeline";

        private const string SHEET_EXIT_CALLS = "7.Exit Calls";
        private const string SHEET_EXIT_CALLS_TYPE_PIVOT = "7.Exit Calls.Type";
        private const string SHEET_EXIT_CALLS_TIMELINE_PIVOT = "7.Exit Calls.Timeline";
        private const string SHEET_EXIT_CALLS_ERRORS_PIVOT = "7.Exit Calls.Errors";

        private const string SHEET_SERVICE_ENDPOINT_CALLS = "8.SEP Calls";
        private const string SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT = "8.SEP Calls.Type";
        private const string SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT = "8.SEP.Timeline";

        private const string SHEET_DETECTED_ERRORS = "9.Errors";
        private const string SHEET_DETECTED_ERRORS_TYPE_PIVOT = "9.Errors.Type";
        private const string SHEET_DETECTED_ERRORS_TIMELINE_PIVOT = "9.Errors.Timeline";

        private const string SHEET_BUSINESS_DATA = "10.Business Data";
        private const string SHEET_BUSINESS_DATA_TYPE_PIVOT = "10.Business Data.Type";
        private const string SHEET_BUSINESS_DATA_TIMELINE_PIVOT = "10.Business Data.Timeline";

        private const string SHEET_METHOD_CALL_LINES = "11.Method Calls";
        private const string SHEET_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE_PIVOT = "11.Calls.Type";
        private const string SHEET_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE_PIVOT = "11.Calls.Location";
        private const string SHEET_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE_PIVOT = "11.Calls.Timeline";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS = "t_Applications";

        private const string TABLE_SNAPSHOTS = "t_Snapshots";
        private const string TABLE_SEGMENTS = "t_Segments";
        private const string TABLE_EXIT_CALLS = "t_ExitCalls";
        private const string TABLE_SERVICE_ENDPOINT_CALLS = "t_ServiceEndpointCalls";
        private const string TABLE_DETECTED_ERRORS = "t_DetectedErrors";
        private const string TABLE_BUSINESS_DATA = "t_BusinessData";
        private const string TABLE_METHOD_CALL_LINES = "t_MethodCallLines";

        private const string PIVOT_SNAPSHOTS_TYPE = "p_SnapshotsType";
        private const string PIVOT_SNAPSHOTS_TIMELINE = "p_SnapshotsTimeline";
        private const string PIVOT_SEGMENTS_TYPE = "p_SegmentsType";
        private const string PIVOT_SEGMENTS_TIMELINE = "p_SegmentsTimeline";
        private const string PIVOT_SERVICE_ENDPOINT_CALLS_TYPE = "p_ServiceEndpointCallsType";
        private const string PIVOT_SERVICE_ENDPOINT_CALLS_TIMELINE = "p_ServiceEndpointCallsTimeline";
        private const string PIVOT_EXIT_CALLS_TYPE = "p_ExitCallsType";
        private const string PIVOT_EXIT_CALLS_TIMELINE = "p_ExitCallsTimeline";
        private const string PIVOT_EXIT_CALLS_ERRORS = "p_ExitCallsType";
        private const string PIVOT_DETECTED_ERRORS_TYPE = "p_DetectedErrorsType";
        private const string PIVOT_DETECTED_ERRORS_TIMELINE = "p_DetectedErrorsTimeline";
        private const string PIVOT_BUSINESS_DATA_TYPE = "p_BusinessDataType";
        private const string PIVOT_BUSINESS_DATA_TIMELINE = "p_BusinessDataTimeline";
        private const string PIVOT_METHOD_CALL_LINES_TYPE_EXEC_AVERAGE = "p_MethodCallLinesTypeAverage";
        private const string PIVOT_METHOD_CALL_LINES_LOCATION_EXEC_AVERAGE = "p_MethodCallLinesLocationAverage";
        private const string PIVOT_METHOD_CALL_LINES_TIMELINE_EXEC_AVERAGE = "p_MethodCallLinesTimelineAverage";

        private const string GRAPH_SNAPSHOTS_TYPE = "g_SnapshotsType";
        private const string GRAPH_SNAPSHOTS_TIMELINE = "g_SnapshotsTimeline";
        private const string GRAPH_SEGMENTS_TYPE = "g_SegmentsType";
        private const string GRAPH_SEGMENTS_TIMELINE = "g_SegmentsTimeline";
        private const string GRAPH_SERVICE_ENDPOINT_CALLS_TYPE = "g_ServiceEndpointCallsType";
        private const string GRAPH_SERVICE_ENDPOINT_CALLS_TIMELINE = "g_ServiceEndpointCallsTimeline";
        private const string GRAPH_EXIT_CALLS_TYPE = "g_ExitCallsType";
        private const string GRAPH_EXIT_CALLS_TIMELINE = "g_ExitCallsTimeline";
        private const string GRAPH_EXIT_CALLS_ERRORS = "g_ExitCallsErrors";
        private const string GRAPH_DETECTED_ERRORS_TYPE = "g_DetectedErrorsType";
        private const string GRAPH_DETECTED_ERRORS_TIMELINE = "g_DetectedErrorsTimeline";
        private const string GRAPH_BUSINESS_DATA_TYPE = "g_BusinessDataType";
        private const string GRAPH_BUSINESS_DATA_TIMELINE = "g_BusinessDataTimeline";
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
            stepTimingFunction.NumEntities = jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.Length;

            this.DisplayJobStepStartingStatus(jobConfiguration);

            FilePathMap = new FilePathMap(programOptions, jobConfiguration);

            if (this.ShouldExecute(programOptions, jobConfiguration) == false)
            {
                return true;
            }

            if (jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.Length == 0)
            {
                logger.Warn("No snapshot RequestIDs specified to process");
                loggerConsole.Warn("No snapshot RequestIDs specified to process");

                return true;
            }

            try
            {
                #region Filter list of all Snapshot data

                loggerConsole.Info("Looking for {0} RequestIDs ({1})", jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.Length, String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs));

                loggerConsole.Info("List of Snapshots");
                List<Snapshot> snapshotsList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsReportFilePath(), new SnapshotReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                loggerConsole.Info("List of Segments");
                List<Segment> segmentsList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsSegmentsReportFilePath(), new SegmentReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                loggerConsole.Info("List of Exit Calls");
                List<ExitCall> exitCallsList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsExitCallsReportFilePath(), new ExitCallReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                loggerConsole.Info("List of Service Endpoint Calls");
                List<ServiceEndpointCall> serviceEndpointCallsList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsServiceEndpointCallsReportFilePath(), new ServiceEndpointCallReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                loggerConsole.Info("List of Detected Errors");
                List<DetectedError> detectedErrorsList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsDetectedErrorsReportFilePath(), new DetectedErrorReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                loggerConsole.Info("List of Business Data");
                List<BusinessData> businessDataList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsBusinessDataReportFilePath(), new BusinessDataReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                loggerConsole.Info("List of Method Call Lines");
                List<MethodCallLine> methodCallLinesList = FileIOHelper.ReadListFromCSVFileForSnapshotsWithRequestIDs(FilePathMap.SnapshotsMethodCallLinesReportFilePath(), new MethodCallLineReportMap(), jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs.ToList());

                #endregion

                #region Save individual snapshot CSV files

                loggerConsole.Info("Saving individual snapshots data");

                foreach (Snapshot snapshot in snapshotsList)
                {
                    loggerConsole.Info("Saving {0}/{1}[{2}]/{3} in {4}", snapshot.TierName, snapshot.BTName, snapshot.BTType, snapshot.RequestID, FilePathMap.SnapshotReportFilePath(snapshot, true));

                    List<Snapshot> snapshotsSnapshotList = new List<Snapshot>();
                    snapshotsSnapshotList.Add(snapshot);
                    FileIOHelper.WriteListToCSVFile(snapshotsSnapshotList, new SnapshotReportMap(), FilePathMap.SnapshotReportFilePath(snapshot, true));

                    List<Segment> segmentsSnapshotList = segmentsList.Where(s => String.Compare(s.RequestID, snapshot.RequestID, true) == 0).ToList();
                    FileIOHelper.WriteListToCSVFile(segmentsSnapshotList, new SegmentReportMap(), FilePathMap.SnapshotSegmentsReportFilePath(snapshot, true));

                    List<ExitCall> exitCallsSnapshotList = exitCallsList.Where(s => String.Compare(s.RequestID, snapshot.RequestID, true) == 0).ToList();
                    FileIOHelper.WriteListToCSVFile(exitCallsSnapshotList, new ExitCallReportMap(), FilePathMap.SnapshotExitCallsReportFilePath(snapshot, true));

                    List<ServiceEndpointCall> serviceEndpointCallsSnapshotList = serviceEndpointCallsList.Where(s => String.Compare(s.RequestID, snapshot.RequestID, true) == 0).ToList();
                    FileIOHelper.WriteListToCSVFile(serviceEndpointCallsSnapshotList, new ServiceEndpointCallReportMap(), FilePathMap.SnapshotServiceEndpointCallsReportFilePath(snapshot, true));

                    List<DetectedError> detectedErrorsSnapshotList = detectedErrorsList.Where(s => String.Compare(s.RequestID, snapshot.RequestID, true) == 0).ToList();
                    FileIOHelper.WriteListToCSVFile(detectedErrorsSnapshotList, new DetectedErrorReportMap(), FilePathMap.SnapshotDetectedErrorsReportFilePath(snapshot, true));

                    List<BusinessData> businessDataSnapshotList = businessDataList.Where(s => String.Compare(s.RequestID, snapshot.RequestID, true) == 0).ToList();
                    FileIOHelper.WriteListToCSVFile(businessDataSnapshotList, new BusinessDataReportMap(), FilePathMap.SnapshotBusinessDataReportFilePath(snapshot, true));

                    List<MethodCallLine> methodCallLinesSnapshotList = methodCallLinesList.Where(s => String.Compare(s.RequestID, snapshot.RequestID, true) == 0).ToList();
                    FileIOHelper.WriteListToCSVFile(methodCallLinesSnapshotList, new MethodCallLineReportMap(), FilePathMap.SnapshotMethodCallLinesReportFilePath(snapshot, true));
                }

                #endregion

                #region Render individual Snapshot reports

                foreach (Snapshot snapshot in snapshotsList)
                {
                    loggerConsole.Info("Prepare Snapshot Report File for {0}/{1}[{2}]/{3}", snapshot.TierName, snapshot.BTName, snapshot.BTType, snapshot.RequestID);

                    #region Prepare the report package

                    // Prepare package
                    ExcelPackage excelReport = new ExcelPackage();
                    excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                    excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Snapshot Report";
                    excelReport.Workbook.Properties.Subject = programOptions.JobName;

                    excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                    #endregion

                    #region Parameters sheet

                    // Parameters sheet
                    ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                    var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                    hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                    hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                    var timelineStyle = sheet.Workbook.Styles.CreateNamedStyle("TimelineStyle");
                    timelineStyle.Style.Font.Name = "Consolas";
                    timelineStyle.Style.Font.Size = 8;

                    fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Snapshot Report");

                    int l = sheet.Dimension.Rows + 2;
                    sheet.Cells[l, 1].Value = "Controller";
                    sheet.Cells[l, 2].Value = snapshot.Controller;
                    l++;
                    sheet.Cells[l, 1].Value = "ApplicationName";
                    sheet.Cells[l, 2].Value = snapshot.ApplicationName;
                    l++;
                    sheet.Cells[l, 1].Value = "TierName";
                    sheet.Cells[l, 2].Value = snapshot.TierName;
                    l++;
                    sheet.Cells[l, 1].Value = "BTName";
                    sheet.Cells[l, 2].Value = snapshot.BTName;
                    l++;
                    sheet.Cells[l, 1].Value = "RequestID";
                    sheet.Cells[l, 2].Value = snapshot.RequestID;

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

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SNAPSHOTS);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Pivot";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SNAPSHOTS_TYPE_PIVOT);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[3, 1].Value = "See Timeline";
                    sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SNAPSHOTS_TIMELINE_PIVOT);
                    sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SNAPSHOTS_TYPE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SNAPSHOTS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SNAPSHOTS_TIMELINE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SNAPSHOTS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 6, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SEGMENTS);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Pivot";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SEGMENTS_TYPE_PIVOT);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[3, 1].Value = "See Timeline";
                    sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SEGMENTS_TIMELINE_PIVOT);
                    sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SEGMENTS_TYPE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SEGMENTS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SEGMENTS_TIMELINE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SEGMENTS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 6, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_EXIT_CALLS);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Pivot";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EXIT_CALLS_TYPE_PIVOT);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 3].Value = "See Errors";
                    sheet.Cells[2, 4].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EXIT_CALLS_ERRORS_PIVOT);
                    sheet.Cells[2, 4].StyleName = "HyperLinkStyle";
                    sheet.Cells[3, 1].Value = "See Timeline";
                    sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EXIT_CALLS_TIMELINE_PIVOT);
                    sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_EXIT_CALLS_TYPE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EXIT_CALLS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_EXIT_CALLS_ERRORS_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EXIT_CALLS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_EXIT_CALLS_TIMELINE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_EXIT_CALLS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINT_CALLS);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Pivot";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[3, 1].Value = "See Timeline";
                    sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT);
                    sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINT_CALLS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SERVICE_ENDPOINT_CALLS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_DETECTED_ERRORS);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Pivot";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DETECTED_ERRORS_TYPE_PIVOT);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[3, 1].Value = "See Timeline";
                    sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DETECTED_ERRORS_TIMELINE_PIVOT);
                    sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_DETECTED_ERRORS_TYPE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DETECTED_ERRORS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_DETECTED_ERRORS_TIMELINE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DETECTED_ERRORS);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 7, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_DATA);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Pivot";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_DATA_TYPE_PIVOT);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[3, 1].Value = "See Timeline";
                    sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_DATA_TIMELINE_PIVOT);
                    sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_DATA_TYPE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_DATA);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1);

                    sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_DATA_TIMELINE_PIVOT);
                    sheet.Cells[1, 1].Value = "Table of Contents";
                    sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                    sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                    sheet.Cells[2, 1].Value = "See Table";
                    sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_BUSINESS_DATA);
                    sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                    sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1);

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

                    loggerConsole.Info("Fill Snapshot Report File");

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

                    #region Snapshots

                    loggerConsole.Info("List of Snapshots");

                    sheet = excelReport.Workbook.Worksheets[SHEET_SNAPSHOTS];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotReportFilePath(snapshot, true), 0, typeof(Snapshot), sheet, LIST_SHEET_START_TABLE_AT, 1);

                    #endregion

                    #region Segments

                    loggerConsole.Info("List of Segments");
                    sheet = excelReport.Workbook.Worksheets[SHEET_SEGMENTS];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotSegmentsReportFilePath(snapshot, true), 0, typeof(Segment), sheet, LIST_SHEET_START_TABLE_AT, 1);

                    #endregion

                    #region Exit Calls

                    loggerConsole.Info("List of Exit Calls");

                    sheet = excelReport.Workbook.Worksheets[SHEET_EXIT_CALLS];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotExitCallsReportFilePath(snapshot, true), 0, typeof(ExitCall), sheet, LIST_SHEET_START_TABLE_AT, 1);

                    #endregion

                    #region Service Endpoint Calls

                    loggerConsole.Info("List of Service Endpoint Calls");

                    sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINT_CALLS];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotServiceEndpointCallsReportFilePath(snapshot, true), 0, typeof(ServiceEndpointCall), sheet, LIST_SHEET_START_TABLE_AT, 1);

                    #endregion

                    #region Detected Errors

                    loggerConsole.Info("List of Detected Errors");

                    sheet = excelReport.Workbook.Worksheets[SHEET_DETECTED_ERRORS];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotDetectedErrorsReportFilePath(snapshot, true), 0, typeof(DetectedError), sheet, LIST_SHEET_START_TABLE_AT, 1);

                    #endregion

                    #region Business Data

                    loggerConsole.Info("List of Business Data");

                    sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_DATA];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotBusinessDataReportFilePath(snapshot, true), 0, typeof(BusinessData), sheet, LIST_SHEET_START_TABLE_AT, 1);

                    #endregion

                    #region Method Call Lines

                    loggerConsole.Info("List of Method Call Lines");

                    sheet = excelReport.Workbook.Worksheets[SHEET_METHOD_CALL_LINES];
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SnapshotMethodCallLinesReportFilePath(snapshot, true), 0, typeof(MethodCallLine), sheet, LIST_SHEET_START_TABLE_AT + 1, 1);

                    #endregion

                    loggerConsole.Info("Finalize Snapshot Report File");

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

                    #region Snapshots

                    // Make table
                    sheet = excelReport.Workbook.Worksheets[SHEET_SNAPSHOTS];
                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                    {
                        range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        table = sheet.Tables.Add(range, TABLE_SNAPSHOTS);
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
                            sheet.Column(table.Columns["UserExperience"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["RequestID"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["DurationRange"].Position + 1).Width = 15;
                        }
                        catch (OutOfMemoryException ex)
                        {
                            // Do nothing, we must have a lot of cells
                            logger.Warn("Ran out of memory due to too many rows/cells");
                            logger.Warn(ex);
                        }

                        ExcelAddress cfAddressUserExperience = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["UserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["UserExperience"].Position + 1);
                        addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                        ExcelAddress cfAddressDuration = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Duration"].Position + 1, sheet.Dimension.Rows, table.Columns["Duration"].Position + 1);
                        var cfDuration = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressDuration);
                        cfDuration.LowValue.Color = colorGreenFor3ColorScales;
                        cfDuration.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                        cfDuration.MiddleValue.Value = 70;
                        cfDuration.MiddleValue.Color = colorYellowFor3ColorScales;
                        cfDuration.HighValue.Color = colorRedFor3ColorScales;

                        ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                        var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallGraphs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallGraphs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledBackends"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledTiers"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledApplications"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToBackends"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToTiers"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToApplications"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        sheet = excelReport.Workbook.Worksheets[SHEET_SNAPSHOTS_TYPE_PIVOT];
                        ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_SNAPSHOTS_TYPE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "HasErrors");
                        addFilterFieldToPivot(pivot, "CallGraphType");
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "RequestID", DataFieldFunctions.Count);

                        ExcelChart chart = sheet.Drawings.AddChart(GRAPH_SNAPSHOTS_TYPE, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_SNAPSHOTS_TIMELINE_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1], range, PIVOT_SNAPSHOTS_TIMELINE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "HasErrors");
                        addFilterFieldToPivot(pivot, "CallGraphType");
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                        fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                        fieldR.Compact = false;
                        fieldR.Outline = false;
                        addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "RequestID", DataFieldFunctions.Count);
                        addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average);

                        chart = sheet.Drawings.AddChart(GRAPH_SNAPSHOTS_TIMELINE, eChartType.Line, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                    }

                    #endregion

                    #region Segments

                    // Make table
                    sheet = excelReport.Workbook.Worksheets[SHEET_SEGMENTS];
                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                    {
                        range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        table = sheet.Tables.Add(range, TABLE_SEGMENTS);
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
                            sheet.Column(table.Columns["UserExperience"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["SnapshotUserExperience"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                            sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["FromSegmentID"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["FromTierName"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["Occurred"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["OccurredUtc"].Position + 1).Width = 20;
                        }
                        catch (OutOfMemoryException ex)
                        {
                            // Do nothing, we must have a lot of cells
                            logger.Warn("Ran out of memory due to too many rows/cells");
                            logger.Warn(ex);
                        }

                        // Make timeline fixed width
                        ExcelRangeBase rangeTimeline = sheet.Cells[LIST_SHEET_START_TABLE_AT + 1, table.Columns["Timeline"].Position + 1, sheet.Dimension.Rows, table.Columns["Timeline"].Position + 1];
                        rangeTimeline.StyleName = "TimelineStyle";

                        ExcelAddress cfAddressUserExperience = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["UserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["UserExperience"].Position + 1);
                        addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                        cfAddressUserExperience = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["SnapshotUserExperience"].Position + 1, sheet.Dimension.Rows, table.Columns["SnapshotUserExperience"].Position + 1);
                        addUserExperienceConditionalFormatting(sheet, cfAddressUserExperience);

                        ExcelAddress cfAddressDuration = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Duration"].Position + 1, sheet.Dimension.Rows, table.Columns["Duration"].Position + 1);
                        var cfDuration = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressDuration);
                        cfDuration.LowValue.Color = colorGreenFor3ColorScales;
                        cfDuration.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                        cfDuration.MiddleValue.Value = 70;
                        cfDuration.MiddleValue.Color = colorYellowFor3ColorScales;
                        cfDuration.HighValue.Color = colorRedFor3ColorScales;

                        ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumErrors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumErrors"].Position + 1);
                        var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledBackends"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledTiers"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCalledApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCalledApplications"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToBackends"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToBackends"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToTiers"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCallsToApplications"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCallsToApplications"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSEPs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSEPs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumHTTPDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumHTTPDCs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMIDCs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMIDCs"].Position + 1);
                        cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                        sheet = excelReport.Workbook.Worksheets[SHEET_SEGMENTS_TYPE_PIVOT];
                        ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_SEGMENTS_TYPE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "HasErrors");
                        addFilterFieldToPivot(pivot, "CallGraphType");
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                        ExcelChart chart = sheet.Drawings.AddChart(GRAPH_SEGMENTS_TYPE, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_SEGMENTS_TIMELINE_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1], range, PIVOT_SEGMENTS_TIMELINE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "HasErrors");
                        addFilterFieldToPivot(pivot, "CallGraphType");
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                        fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                        fieldR.Compact = false;
                        fieldR.Outline = false;
                        addColumnFieldToPivot(pivot, "UserExperience", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);
                        addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average);

                        chart = sheet.Drawings.AddChart(GRAPH_SEGMENTS_TIMELINE, eChartType.Line, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                    }

                    #endregion

                    #region Exit Calls

                    // Make table
                    sheet = excelReport.Workbook.Worksheets[SHEET_EXIT_CALLS];
                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                    {
                        range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        table = sheet.Tables.Add(range, TABLE_EXIT_CALLS);
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
                            sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                            sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["ToEntityName"].Position + 1).Width = 15;
                            sheet.Column(table.Columns["ExitType"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["Detail"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["Method"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["ToSegmentID"].Position + 1).Width = 15;
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

                        ExcelAddress cfAddressDuration = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Duration"].Position + 1, sheet.Dimension.Rows, table.Columns["Duration"].Position + 1);
                        var cfDuration = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressDuration);
                        cfDuration.LowValue.Color = colorGreenFor3ColorScales;
                        cfDuration.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                        cfDuration.MiddleValue.Value = 70;
                        cfDuration.MiddleValue.Color = colorYellowFor3ColorScales;
                        cfDuration.HighValue.Color = colorRedFor3ColorScales;

                        sheet = excelReport.Workbook.Worksheets[SHEET_EXIT_CALLS_TYPE_PIVOT];
                        ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_EXIT_CALLS_TYPE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "ToEntityType");
                        addFilterFieldToPivot(pivot, "ToEntityName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "RequestID");
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        addRowFieldToPivot(pivot, "ExitType");
                        addRowFieldToPivot(pivot, "Detail");
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addDataFieldToPivot(pivot, "NumCalls", DataFieldFunctions.Sum, "Calls");
                        addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average, "Average");

                        ExcelChart chart = sheet.Drawings.AddChart(GRAPH_EXIT_CALLS_TYPE, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;
                        sheet.Column(5).Width = 20;
                        sheet.Column(6).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_EXIT_CALLS_TIMELINE_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1], range, PIVOT_EXIT_CALLS_TIMELINE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "Controller", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "TierName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "BTName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "ToEntityType");
                        addFilterFieldToPivot(pivot, "ToEntityName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        addFilterFieldToPivot(pivot, "Detail", eSortType.Ascending);
                        ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["Occurred"]);
                        fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                        fieldR.Compact = false;
                        fieldR.Outline = false;
                        addColumnFieldToPivot(pivot, "ExitType", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "NumCalls", DataFieldFunctions.Sum, "Calls");
                        addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average, "Average");

                        chart = sheet.Drawings.AddChart(GRAPH_EXIT_CALLS_TIMELINE, eChartType.Line, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_EXIT_CALLS_ERRORS_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1], range, PIVOT_EXIT_CALLS_ERRORS);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "ToEntityType");
                        addFilterFieldToPivot(pivot, "ToEntityName", eSortType.Ascending);
                        addFilterFieldToPivot(pivot, "RequestID");
                        addFilterFieldToPivot(pivot, "DurationRange", eSortType.Ascending, true);
                        addFilterFieldToPivot(pivot, "HasErrors", eSortType.Ascending);
                        addRowFieldToPivot(pivot, "ExitType");
                        addRowFieldToPivot(pivot, "ErrorDetail");
                        addRowFieldToPivot(pivot, "Detail");
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addDataFieldToPivot(pivot, "NumCalls", DataFieldFunctions.Sum, "Calls");
                        addDataFieldToPivot(pivot, "Duration", DataFieldFunctions.Average, "Average");

                        chart = sheet.Drawings.AddChart(GRAPH_EXIT_CALLS_ERRORS, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;
                        sheet.Column(5).Width = 20;
                        sheet.Column(6).Width = 20;
                        sheet.Column(7).Width = 20;
                    }

                    #endregion

                    #region Service Endpoint Calls

                    // Make table
                    sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINT_CALLS];
                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                    {
                        range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        table = sheet.Tables.Add(range, TABLE_SERVICE_ENDPOINT_CALLS);
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
                            sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                            sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["SEPName"].Position + 1).Width = 20;
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

                        sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINT_CALLS_TYPE_PIVOT];
                        ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT - 2, 1], range, PIVOT_SERVICE_ENDPOINT_CALLS_TYPE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "RequestID");
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addRowFieldToPivot(pivot, "SEPName");
                        addColumnFieldToPivot(pivot, "SEPType", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                        ExcelChart chart = sheet.Drawings.AddChart(GRAPH_SERVICE_ENDPOINT_CALLS_TYPE, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;
                        sheet.Column(5).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINT_CALLS_TIMELINE_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_SERVICE_ENDPOINT_CALLS_TIMELINE);
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

                        chart = sheet.Drawings.AddChart(GRAPH_SERVICE_ENDPOINT_CALLS_TIMELINE, eChartType.Line, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                    }

                    #endregion

                    #region Detected Errors

                    // Make table
                    sheet = excelReport.Workbook.Worksheets[SHEET_DETECTED_ERRORS];
                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                    {
                        range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        table = sheet.Tables.Add(range, TABLE_DETECTED_ERRORS);
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
                            sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                            sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["ErrorName"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["ErrorMessage"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["ErrorDetail"].Position + 1).Width = 20;
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

                        sheet = excelReport.Workbook.Worksheets[SHEET_DETECTED_ERRORS_TYPE_PIVOT];
                        ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT - 2, 1], range, PIVOT_DETECTED_ERRORS_TYPE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "RequestID");
                        addRowFieldToPivot(pivot, "ErrorType");
                        addRowFieldToPivot(pivot, "ErrorName");
                        addRowFieldToPivot(pivot, "ErrorMessage");
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                        ExcelChart chart = sheet.Drawings.AddChart(GRAPH_DETECTED_ERRORS_TYPE, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;
                        sheet.Column(5).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_DETECTED_ERRORS_TIMELINE_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1], range, PIVOT_DETECTED_ERRORS_TIMELINE);
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

                        chart = sheet.Drawings.AddChart(GRAPH_DETECTED_ERRORS_TIMELINE, eChartType.Line, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                    }

                    #endregion

                    #region Business Data

                    // Make table
                    sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_DATA];
                    logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                    if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                    {
                        range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                        table = sheet.Tables.Add(range, TABLE_BUSINESS_DATA);
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
                            sheet.Column(table.Columns["RequestID"].Position + 1).Width = 15;
                            sheet.Column(table.Columns["SegmentID"].Position + 1).Width = 10;
                            sheet.Column(table.Columns["DataName"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["DataValue"].Position + 1).Width = 20;
                            sheet.Column(table.Columns["DataType"].Position + 1).Width = 10;
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

                        sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_DATA_TYPE_PIVOT];
                        ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT - 2, 1], range, PIVOT_BUSINESS_DATA_TYPE);
                        setDefaultPivotTableSettings(pivot);
                        addFilterFieldToPivot(pivot, "RequestID");
                        addRowFieldToPivot(pivot, "Controller");
                        addRowFieldToPivot(pivot, "ApplicationName");
                        addRowFieldToPivot(pivot, "TierName");
                        addRowFieldToPivot(pivot, "BTName");
                        addRowFieldToPivot(pivot, "DataName");
                        addColumnFieldToPivot(pivot, "DataType", eSortType.Ascending);
                        addDataFieldToPivot(pivot, "SegmentID", DataFieldFunctions.Count);

                        ExcelChart chart = sheet.Drawings.AddChart(GRAPH_BUSINESS_DATA_TYPE, eChartType.ColumnClustered, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
                        sheet.Column(2).Width = 20;
                        sheet.Column(3).Width = 20;
                        sheet.Column(4).Width = 20;
                        sheet.Column(5).Width = 20;

                        sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_DATA_TIMELINE_PIVOT];
                        pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1], range, PIVOT_BUSINESS_DATA_TIMELINE);
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

                        chart = sheet.Drawings.AddChart(GRAPH_BUSINESS_DATA_TIMELINE, eChartType.Line, pivot);
                        chart.SetPosition(2, 0, 0, 0);
                        chart.SetSize(800, 300);

                        sheet.Column(1).Width = 20;
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

                    string reportFilePath = FilePathMap.SnapshotExcelReportFilePath(snapshot);
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
            logger.Trace("Output.IndividualSnapshots={0}", jobConfiguration.Output.IndividualSnapshots);
            loggerConsole.Trace("Output.IndividualSnapshots={0}", jobConfiguration.Output.IndividualSnapshots);
            if (jobConfiguration.Input.Snapshots == false || jobConfiguration.Output.IndividualSnapshots == false)
            {
                loggerConsole.Trace("Skipping report of individual snapshots");
            }
            return (jobConfiguration.Input.Snapshots == true && jobConfiguration.Output.IndividualSnapshots == true);
        }

    }
}
