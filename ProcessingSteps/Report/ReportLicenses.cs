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
    public class ReportLicenses : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_ACCOUNTS_LIST = "5.Accounts";
        private const string SHEET_LICENSES_LIST = "6.Licenses";
        private const string SHEET_LICENSES_TYPE_PIVOT = "6.Licenses.Type";
        private const string SHEET_LICENSES_USAGE_LIST = "7.Licenses Usage";
        private const string SHEET_LICENSES_USAGE_TIMELINE_PIVOT = "7.Licenses Usage.Timeline";
        private const string SHEET_LICENSE_RULES_LIST = "8.License Rules";
        private const string SHEET_LICENSE_RULES_TYPE_PIVOT = "8.License Rules.Type";
        private const string SHEET_LICENSE_RULES_USAGE_LIST = "9.License Rules Usage";
        private const string SHEET_LICENSE_RULES_USAGE_TIMELINE_PIVOT = "9.License Rules Usage.Timeline";
        private const string SHEET_LICENSE_RULE_SCOPES_LIST = "10.License Rule Scopes";
        private const string SHEET_LICENSE_RULE_SCOPES_TYPE_PIVOT = "10.License Rule Scopes.Type";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_ACCOUNTS = "t_Dashboards";
        private const string TABLE_LICENSES = "t_Licenses";
        private const string TABLE_LICENSES_USAGE = "t_LicenseUsage";
        private const string TABLE_LICENSE_RULES = "t_LicenseRules";
        private const string TABLE_LICENSE_RULES_USAGE = "t_LicenseRulesUsage";
        private const string TABLE_LICENSE_RULE_SCOPES = "t_LicenseRuleScopes";

        private const string PIVOT_LICENSES_TYPE = "p_LicensesType";
        private const string PIVOT_LICENSES_USAGE_TIMELINE = "p_LicensesUsageTimeline";
        private const string PIVOT_LICENSE_RULES_TYPE = "p_LicenseRulesType";
        private const string PIVOT_LICENSE_RULES_USAGE_TIMELINE = "p_LicenseRulesUsageTimeline";
        private const string PIVOT_LICENSE_RULE_SCOPES_TYPE = "p_LicenseRulesUsageTimeline";

        private const string GRAPH_LICENSES_TYPE = "g_LicensesType";
        private const string GRAPH_LICENSES_USAGE_TIMELINE = "g_LicensesUsageTimeline";
        private const string GRAPH_LICENSE_RULES_TYPE = "g_LicenseRulesType";
        private const string GRAPH_LICENSE_RULES_USAGE_TIMELINE = "g_LicenseRulesUsageTimeline";
        private const string GRAPH_LICENSE_RULE_SCOPES_TYPE = "g_LicensesType";

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
                loggerConsole.Info("Prepare Licenses Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Licenses Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Licenses Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_ACCOUNTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSES_USAGE_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSES_USAGE_TIMELINE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSES_USAGE_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSES_USAGE_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSE_RULES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSE_RULES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSE_RULES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSE_RULES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSE_RULES_USAGE_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSE_RULES_USAGE_TIMELINE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSE_RULES_USAGE_TIMELINE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSE_RULES_USAGE_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 5, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSE_RULE_SCOPES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
                sheet.Cells[2, 1].Value = "See Pivot";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSE_RULE_SCOPES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_LICENSE_RULE_SCOPES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_LICENSE_RULE_SCOPES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                #endregion

                loggerConsole.Info("Fill Licenses Report File");

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

                #region Accounts

                loggerConsole.Info("List of Accounts");

                sheet = excelReport.Workbook.Worksheets[SHEET_ACCOUNTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.LicenseAccountReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Licenses

                loggerConsole.Info("List of Licenses");

                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.LicensesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region License Usage

                loggerConsole.Info("License Usage");

                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSES_USAGE_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.LicenseUsageAccountReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region License Rules

                loggerConsole.Info("List of License Rules");

                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.LicenseRulesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region License Rules Usage

                loggerConsole.Info("License Rules Usage");

                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULES_USAGE_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.LicenseUsageRulesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region License Rule Scopes

                loggerConsole.Info("List of License Rule Scopes");

                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULE_SCOPES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.LicenseRuleScopesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

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

                #region Accounts

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_ACCOUNTS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_ACCOUNTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountNameGlobal"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["AccessKey1"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccessKey2"].Position + 1).Width = 35;
                    sheet.Column(table.Columns["ExpirationDate"].Position + 1).Width = 20;
                }

                #endregion

                #region Licenses

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_LICENSES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ExpirationDate"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_LICENSES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_LICENSES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Edition", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "Model", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "Controller");
                    addColumnFieldToPivot(pivot, "AgentType");
                    addDataFieldToPivot(pivot, "Average", DataFieldFunctions.Sum);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_LICENSES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                }

                #endregion

                #region License Usage

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSES_USAGE_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_LICENSES_USAGE);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LicenseEventTime"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LicenseEventTimeUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_LICENSES_USAGE_TIMELINE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_LICENSES_USAGE_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["LicenseEventTime"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Average", DataFieldFunctions.Average);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_LICENSES_USAGE_TIMELINE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                #endregion

                #region License Rules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_LICENSE_RULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_LICENSE_RULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "NumApplications");
                    addRowFieldToPivot(pivot, "RuleName");
                    addColumnFieldToPivot(pivot, "AgentType");
                    addDataFieldToPivot(pivot, "Licenses", DataFieldFunctions.Sum);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_LICENSE_RULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 10;
                    sheet.Column(3).Width = 20;
                }

                #endregion

                #region License Rules Usage

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULES_USAGE_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_LICENSE_RULES_USAGE);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["AgentType"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LicenseEventTime"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["LicenseEventTimeUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULES_USAGE_TIMELINE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_LICENSE_RULES_USAGE_TIMELINE);
                    setDefaultPivotTableSettings(pivot);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["LicenseEventTime"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "Controller", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "RuleName", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "AgentType", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Average", DataFieldFunctions.Average);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_LICENSE_RULES_USAGE_TIMELINE, eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                #endregion

                #region License Rule Scopes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULE_SCOPES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_LICENSE_RULE_SCOPES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AccountName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["RuleName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["ScopeSelector"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["MatchType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["EntityType"].Position + 1).Width = 15;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_LICENSE_RULE_SCOPES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], range, PIVOT_LICENSE_RULE_SCOPES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "RuleName");
                    addRowFieldToPivot(pivot, "MatchType");
                    addRowFieldToPivot(pivot, "EntityName");
                    addColumnFieldToPivot(pivot, "EntityType");
                    addDataFieldToPivot(pivot, "RuleID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_LICENSE_RULE_SCOPES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

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

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.LicensesExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.Licenses={0}", programOptions.LicensedReports.Licenses);
            loggerConsole.Trace("LicensedReports.Licenses={0}", programOptions.LicensedReports.Licenses);
            if (programOptions.LicensedReports.Licenses == false)
            {
                loggerConsole.Warn("Not licensed for licenses");
                return false;
            }

            logger.Trace("Output.Licenses={0}", jobConfiguration.Output.Licenses);
            loggerConsole.Trace("Output.Licenses={0}", jobConfiguration.Output.Licenses);
            if (jobConfiguration.Output.Licenses == false)
            {
                loggerConsole.Trace("Skipping report of licenses");
            }
            return (jobConfiguration.Output.Licenses == true);
        }
    }
}
