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
    public class ReportDashboards : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_DASHBOARDS_LIST = "5.Dashboards";
        private const string SHEET_DASHBOARDS_TYPE_PIVOT = "5.Dashboards.Type";
        private const string SHEET_WIDGETS_LIST = "6.Widgets";
        private const string SHEET_WIDGETS_TYPE_PIVOT = "6.Widgets.Type";
        private const string SHEET_DATA_SERIES_LIST = "8.Data Series";
        private const string SHEET_DATA_SERIES_TYPE_PIVOT = "8.Data Series.Type";
        private const string SHEET_DATA_SERIES_LOCATION_PIVOT = "8.Data Series.Location";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_DASHBOARDS = "t_Dashboards";
        private const string TABLE_WIDGETS = "t_Widgets";
        private const string TABLE_DATA_SERIES = "t_DataSeries";

        private const string PIVOT_DASHBOARDS_TYPE = "p_DashboardsType";
        private const string PIVOT_WIDGETS_TYPE = "p_WidgetsType";
        private const string PIVOT_DATA_SERIES_TYPE = "p_DataSeriesType";
        private const string PIVOT_DATA_SERIES_LOCATION = "p_DataSeriesLocation";

        private const string GRAPH_DASHBOARDS_TYPE = "g_DashboardsType";
        private const string GRAPH_WIDGETS_TYPE = "g_WidgetsType";
        private const string GRAPH_DATA_SERIES_TYPE = "g_DataSeriesType";
        private const string GRAPH_DATA_SERIES_LOCATION = "g_DataSeriesLocation";

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

            try
            {
                loggerConsole.Info("Prepare Dashboards Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Dashboards Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Dashboards Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DASHBOARDS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DASHBOARDS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DASHBOARDS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DASHBOARDS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WIDGETS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_WIDGETS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WIDGETS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_WIDGETS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DATA_SERIES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Type";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DATA_SERIES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Location";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DATA_SERIES_LOCATION_PIVOT);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DATA_SERIES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DATA_SERIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DATA_SERIES_LOCATION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DATA_SERIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                loggerConsole.Info("Fill Dashboards Report File");

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

                #region Dashboards

                loggerConsole.Info("List of Dashboards");

                sheet = excelReport.Workbook.Worksheets[SHEET_DASHBOARDS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DashboardsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Widgets

                loggerConsole.Info("List of Widgets");

                sheet = excelReport.Workbook.Worksheets[SHEET_WIDGETS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DashboardWidgetsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Widget Data Series

                loggerConsole.Info("List of Widget Data Series");

                sheet = excelReport.Workbook.Worksheets[SHEET_DATA_SERIES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DashboardMetricSeriesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Dashboards Report File");

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

                #region Dashboards

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_DASHBOARDS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DASHBOARDS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumWidgets"].Position + 1, sheet.Dimension.Rows, table.Columns["NumWidgets"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DashboardName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["StartTime"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["StartTimeUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOnUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_DASHBOARDS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_DASHBOARDS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "CreatedBy", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "UpdatedBy", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "IsShared");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CanvasType");
                    addRowFieldToPivot(pivot, "DashboardName");
                    addDataFieldToPivot(pivot, "DashboardID", DataFieldFunctions.Count, "Dashboards");
                    addDataFieldToPivot(pivot, "NumWidgets", DataFieldFunctions.Sum, "Widgets");
                    addDataFieldToPivot(pivot, "NumAnalyticsWidgets", DataFieldFunctions.Sum, "Analytics");
                    addDataFieldToPivot(pivot, "NumEventListWidgets", DataFieldFunctions.Sum, "Events");
                    addDataFieldToPivot(pivot, "NumGaugeWidgets", DataFieldFunctions.Sum, "Gauges");
                    addDataFieldToPivot(pivot, "NumGraphWidgets", DataFieldFunctions.Sum, "Graphs");
                    addDataFieldToPivot(pivot, "NumIFrameWidgets", DataFieldFunctions.Sum, "IFrames");
                    addDataFieldToPivot(pivot, "NumImageWidgets", DataFieldFunctions.Sum, "Images");
                    addDataFieldToPivot(pivot, "NumMetricLabelWidgets", DataFieldFunctions.Sum, "Labels");
                    addDataFieldToPivot(pivot, "NumPieWidgets", DataFieldFunctions.Sum, "Pies");
                    addDataFieldToPivot(pivot, "NumTextWidgets", DataFieldFunctions.Sum, "Texts");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_DASHBOARDS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Widgets

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_WIDGETS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_WIDGETS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DashboardName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["WidgetType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Title"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_WIDGETS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1], range, PIVOT_WIDGETS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "EntityType", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "EntitySelectionType");
                    addFilterFieldToPivot(pivot, "NumSelectedEntities", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumDataSeries", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CanvasType");
                    addRowFieldToPivot(pivot, "DashboardName");
                    addColumnFieldToPivot(pivot, "WidgetType");
                    addDataFieldToPivot(pivot, "DashboardName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_WIDGETS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region Data Series

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_DATA_SERIES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DATA_SERIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["DashboardName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["WidgetType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["SeriesName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MetricType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["MetricPath"].Position + 1).Width = 25;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_DATA_SERIES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_DATA_SERIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "WidgetType");
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CanvasType");
                    addRowFieldToPivot(pivot, "DashboardName");
                    addRowFieldToPivot(pivot, "MetricType");
                    addRowFieldToPivot(pivot, "MetricPath");
                    addColumnFieldToPivot(pivot, "SeriesType");
                    addDataFieldToPivot(pivot, "DashboardName", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_DATA_SERIES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;

                    sheet = excelReport.Workbook.Worksheets[SHEET_DATA_SERIES_LOCATION_PIVOT];
                    pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_DATA_SERIES_LOCATION);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "ApplicationName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "EntityType", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "WidgetType");
                    addRowFieldToPivot(pivot, "MetricType");
                    addRowFieldToPivot(pivot, "MetricPath");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CanvasType");
                    addRowFieldToPivot(pivot, "DashboardName");
                    addColumnFieldToPivot(pivot, "SeriesType");
                    addDataFieldToPivot(pivot, "DashboardName", DataFieldFunctions.Count);

                    chart = sheet.Drawings.AddChart(GRAPH_DATA_SERIES_LOCATION, eChartType.ColumnClustered, pivot);
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
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                if (Directory.Exists(FilePathMap.ReportFolderPath()) == false)
                {
                    Directory.CreateDirectory(FilePathMap.ReportFolderPath());
                }

                string reportFilePath = FilePathMap.DashboardsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("Input.Dashboards={0}", jobConfiguration.Input.Dashboards);
            loggerConsole.Trace("Input.Dashboards={0}", jobConfiguration.Input.Dashboards);
            logger.Trace("Output.Dashboards={0}", jobConfiguration.Output.Dashboards);
            loggerConsole.Trace("Output.Dashboards={0}", jobConfiguration.Output.Dashboards);
            if (jobConfiguration.Input.Dashboards == false || jobConfiguration.Output.Dashboards == false)
            {
                loggerConsole.Trace("Skipping report of dashboards");
            }
            return (jobConfiguration.Input.Dashboards == true && jobConfiguration.Output.Dashboards == true);
        }
    }
}
