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
    public class ReportWEBEntities : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_APPLICATIONS_WEB_LIST = "5.Applications.WEB";
        private const string SHEET_WEB_PAGES_LIST = "6.Web Pages";
        private const string SHEET_WEB_PAGES_TYPE_PIVOT = "6.Web Pages.Type";
        private const string SHEET_PAGE_RESOURCES_LIST = "7.Page Resources";
        private const string SHEET_PAGE_RESOURCES_TYPE_PIVOT = "7.Page Resources.Type";
        private const string SHEET_PAGE_BUSINESS_TRANSACTIONS_LIST = "8.Page BTs";
        private const string SHEET_PAGE_BUSINESS_TRANSACTIONS_TYPE_PIVOT = "8.Page BTs.Type";
        private const string SHEET_GEO_LOCATIONS_LIST = "9.Geo Locations";
        private const string SHEET_GEO_LOCATIONS_TYPE_PIVOT = "9.Geo Locations.Type";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_APPLICATIONS_WEB = "t_Applications_WEB";
        private const string TABLE_WEB_PAGES = "t_WebPages";
        private const string TABLE_PAGE_RESOURCES = "t_WebPageResources";
        private const string TABLE_PAGE_BUSINESS_TRANSACTIONS = "t_WebPageBusinessTransactions";
        private const string TABLE_GEO_LOCATIONS = "t_GeoLocations";

        private const string PIVOT_WEB_PAGES_TYPE = "p_WebPagesType";
        private const string PIVOT_PAGE_RESOURCES_TYPE = "p_WebPageResourcesType";
        private const string PIVOT_PAGE_BUSINESS_TRANSACTIONS_TYPE = "t_WebPageBusinessTransactionsType";
        private const string PIVOT_GEO_LOCATIONS_TYPE = "t_GeoLocationsType";

        private const string GRAPH_WEB_PAGES_TYPE = "g_WebPagesType";
        private const string GRAPH_PAGE_RESOURCES_TYPE = "g_WebPageResourcesType";
        private const string GRAPH_PAGE_BUSINESS_TRANSACTIONS_TYPE = "t_WebPageBusinessTransactionsType";
        private const string GRAPH_GEO_LOCATIONS_TYPE = "t_GeoLocationsType";

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

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_WEB) == 0)
            {
                logger.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);
                loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);

                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Detected WEB Entities Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Detected WEB Entities Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Detected WEB Entities Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_WEB_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WEB_PAGES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_WEB_PAGES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WEB_PAGES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_WEB_PAGES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_PAGE_RESOURCES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_PAGE_RESOURCES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_PAGE_RESOURCES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_PAGE_RESOURCES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_PAGE_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_PAGE_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_PAGE_BUSINESS_TRANSACTIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_PAGE_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_GEO_LOCATIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GEO_LOCATIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_GEO_LOCATIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_GEO_LOCATIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                loggerConsole.Info("Fill Detected WEB Entities Report File");

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

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_WEB_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBApplicationsReportFilePath(), 0, typeof(WEBApplication), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Web Pages

                loggerConsole.Info("List of Web Pages");

                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_PAGES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBPagesReportFilePath(), 0, typeof(WEBPage), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Web Page Resources

                loggerConsole.Info("List of Web Page Resources");

                sheet = excelReport.Workbook.Worksheets[SHEET_PAGE_RESOURCES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBPageResourcesReportFilePath(), 0, typeof(WEBPageToWebPage), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Web Page Business Transactions

                loggerConsole.Info("List of Web Page Business Transactions");

                sheet = excelReport.Workbook.Worksheets[SHEET_PAGE_BUSINESS_TRANSACTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBPageBusinessTransactionsReportFilePath(), 0, typeof(WEBPageToBusinessTransaction), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Geo Locations

                loggerConsole.Info("List of Geo Locations");

                sheet = excelReport.Workbook.Worksheets[SHEET_GEO_LOCATIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.WEBGeoLocationsReportFilePath(), 0, typeof(WEBGeoLocation), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Detected WEB Entities Report File");

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
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_WEB_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_WEB);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumPages"].Position + 1, sheet.Dimension.Rows, table.Columns["NumPages"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumAJAXRequests"].Position + 1, sheet.Dimension.Rows, table.Columns["NumAJAXRequests"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumVirtualPages"].Position + 1, sheet.Dimension.Rows, table.Columns["NumVirtualPages"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumIFrames"].Position + 1, sheet.Dimension.Rows, table.Columns["NumIFrames"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumActivity"].Position + 1, sheet.Dimension.Rows, table.Columns["NumActivity"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNoActivity"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNoActivity"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Web Pages

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_WEB_PAGES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_WEB_PAGES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PageType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["PageName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FirstSegment"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_WEB_PAGES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_WEB_PAGES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "ARTRange", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "NumNameSegments", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "PageType");
                    addRowFieldToPivot(pivot, "FirstSegment");
                    addRowFieldToPivot(pivot, "PageName");
                    addDataFieldToPivot(pivot, "PageID", DataFieldFunctions.Count, "NumPages");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "TimeTotal", DataFieldFunctions.Sum, "Time");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_WEB_PAGES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Web Page Resources

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_PAGE_RESOURCES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_PAGE_RESOURCES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PageType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["PageName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ChildPageType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["ChildPageName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_PAGE_RESOURCES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_PAGE_RESOURCES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "ARTRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "PageName");
                    addRowFieldToPivot(pivot, "ChildPageType");
                    addRowFieldToPivot(pivot, "ChildPageName");
                    addDataFieldToPivot(pivot, "ChildPageID", DataFieldFunctions.Count, "NumPages");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_PAGE_RESOURCES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Web Page Business Transactions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_PAGE_BUSINESS_TRANSACTIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_PAGE_BUSINESS_TRANSACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PageType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["PageName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["BTType"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_PAGE_BUSINESS_TRANSACTIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_PAGE_RESOURCES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "ARTRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "PageName");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "BTName");
                    addDataFieldToPivot(pivot, "BTID", DataFieldFunctions.Count, "NumBTs");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_PAGE_BUSINESS_TRANSACTIONS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Geo Locations

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_GEO_LOCATIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_GEO_LOCATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LocationName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Country"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Region"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["GeoCode"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_GEO_LOCATIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_PAGE_RESOURCES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "HasActivity");
                    addFilterFieldToPivot(pivot, "ARTRange", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "ApplicationName");
                    addRowFieldToPivot(pivot, "LocationType");
                    addRowFieldToPivot(pivot, "Country");
                    addRowFieldToPivot(pivot, "Region");
                    addRowFieldToPivot(pivot, "LocationName");
                    addDataFieldToPivot(pivot, "ART", DataFieldFunctions.Average, "ART");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "CPM", DataFieldFunctions.Average, "CPM");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_GEO_LOCATIONS_TYPE, eChartType.ColumnClustered, pivot);
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

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.WEBEntitiesExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            loggerConsole.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            if (programOptions.LicensedReports.DetectedEntities == false)
            {
                loggerConsole.Warn("Not licensed for detected entities");
                return false;
            }

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
    }
}
