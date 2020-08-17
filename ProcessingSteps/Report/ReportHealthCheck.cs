using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportHealthCheck : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";

        private const string SHEET_HEALTH_CHECK_RULE_RESULTS = "5.Health Check Results";
        private const string SHEET_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_PIVOT = "5.Health Check Results.Desc";
        private const string SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY = "5.Health Check Results.Display";
        private const string SHEET_HEALTH_CHECK_RULE_CATEGORY_RESULTS_DISPLAY = "6.{0}";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";

        private const string TABLE_HEALTH_CHECK_RULE_RESULTS = "t_HealthCheckRuleResults";
        private const string TABLE_HEALTH_CHECK_RULE_APPLICATIONS = "t_HealthCheckRuleApplications";

        private const string TABLE_HEALTH_CHECK_RULE_CATEGORY_RESULTS = "t_H_{0}";

        private const string PIVOT_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_TYPE = "p_HealthCheckRuleDescription";

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

            try
            {
                loggerConsole.Info("Prepare APM Health Check Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER APM Health Check Report";
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

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER APM Health Check Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_ALL_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_HEALTH_CHECK_RULE_RESULTS);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Rating";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[3, 1].Value = "See Summary";
                sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY);
                sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
                
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_CHECK_RULE_RESULTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT - 4 + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_CHECK_RULE_RESULTS);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                loggerConsole.Info("Fill APM Health Check Report File");

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Applications

                loggerConsole.Info("List of Applications - All");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ALL_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerApplicationsReportFilePath(), 0, typeof(ControllerApplication), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Health Check Results

                loggerConsole.Info("List of Health Check Results");

                sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_CHECK_RULE_RESULTS];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.APMHealthCheckRuleResultsReportFilePath(), 0, typeof(HealthCheckRuleResult), sheet, LIST_SHEET_START_TABLE_AT, 1);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerHealthCheckRuleResultsReportFilePath(), 1, typeof(HealthCheckRuleResult), sheet, sheet.Dimension.Rows + 1, 1);
                }
                else
                { 
                    EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerHealthCheckRuleResultsReportFilePath(), 0, typeof(HealthCheckRuleResult), sheet, LIST_SHEET_START_TABLE_AT, 1);
                }

                #endregion

                loggerConsole.Info("Finalize APM Health Check Report File");

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

                #region Health Check Results

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_CHECK_RULE_RESULTS];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_HEALTH_CHECK_RULE_RESULTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Application"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Category"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["Code"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Name"].Position + 1).Width = 50;
                    sheet.Column(table.Columns["Description"].Position + 1).Width = 40;

                    ExcelAddress cfAddressGrade = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Grade"].Position + 1, sheet.Dimension.Rows, table.Columns["Grade"].Position + 1);
                    var cfGrade = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressGrade);
                    cfGrade.LowValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cfGrade.LowValue.Color = colorRedFor3ColorScales;
                    cfGrade.LowValue.Value = 1;
                    cfGrade.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cfGrade.MiddleValue.Value = 3;
                    cfGrade.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfGrade.HighValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cfGrade.HighValue.Color = colorGreenFor3ColorScales;
                    cfGrade.HighValue.Value = 5;

                    sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT - 4, 1], range, PIVOT_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "Application");
                    addRowFieldToPivot(pivot, "Category");
                    addRowFieldToPivot(pivot, "EntityType");
                    addRowFieldToPivot(pivot, "Name");
                    addRowFieldToPivot(pivot, "Description");
                    addColumnFieldToPivot(pivot, "Grade", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Name", DataFieldFunctions.Count, "Rating");

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 30;
                    sheet.Column(6).Width = 30;
                }

                #endregion

                #region Health Check Results Display

                sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY];

                List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();
                
                List<HealthCheckRuleResult> healthCheckRuleResultsController = FileIOHelper.ReadListFromCSVFile(FilePathMap.ControllerHealthCheckRuleResultsReportFilePath(), new HealthCheckRuleResultReportMap());
                if (healthCheckRuleResultsController != null) healthCheckRuleResults.AddRange(healthCheckRuleResultsController);
                
                List<HealthCheckRuleResult> healthCheckRuleResultsAPM = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMHealthCheckRuleResultsReportFilePath(), new HealthCheckRuleResultReportMap());
                if (healthCheckRuleResultsAPM != null) healthCheckRuleResults.AddRange(healthCheckRuleResultsAPM);

                if (healthCheckRuleResults != null)
                {
                    #region Output summary of Applications by Category table

                    // Make this following table out of the list of health rule evaluations
                    // Controller | Application | Category1 | Category2 | Category 3
                    // -------------------------------------------------------------
                    // CntrVal    | AppName     | Avg(Grade)| Avg(Grade)| Avg(Grade)

                    // To do this, we measure number of rows (Controller/Application pairs) and Columns (Category values), and build a table
                    int numRows = healthCheckRuleResults.Select(h => String.Format("{0}/{1}", h.Controller, h.Application)).Distinct().Count();
                    int numColumns = healthCheckRuleResults.Select(h => h.Category).Distinct().Count();

                    Dictionary<string, int> categoryTableRowsLookup = new Dictionary<string, int>(numRows);
                    Dictionary<string, int> categoryTableColumnsLookup = new Dictionary<string, int>(numColumns);

                    List<HealthCheckRuleResult>[,] categoryTableValues = new List<HealthCheckRuleResult>[numRows, numColumns];

                    foreach (HealthCheckRuleResult healthCheckRuleResult in healthCheckRuleResults)
                    {
                        int rowIndex = 0;
                        int columnIndex = 0;

                        string rowIndexValue = String.Format("{0}/{1}", healthCheckRuleResult.Controller, healthCheckRuleResult.Application);
                        string columnIndexValue = healthCheckRuleResult.Category;

                        if (categoryTableRowsLookup.ContainsKey(rowIndexValue) == true)
                        {
                            rowIndex = categoryTableRowsLookup[rowIndexValue];
                        }
                        else
                        {
                            rowIndex = categoryTableRowsLookup.Count;
                            categoryTableRowsLookup.Add(rowIndexValue, rowIndex);
                        }

                        if (categoryTableColumnsLookup.ContainsKey(columnIndexValue) == true)
                        {
                            columnIndex = categoryTableColumnsLookup[columnIndexValue];
                        }
                        else
                        {
                            columnIndex = categoryTableColumnsLookup.Count;
                            categoryTableColumnsLookup.Add(columnIndexValue, columnIndex);
                        }

                        // Fill in the cell
                        List<HealthCheckRuleResult> healthCheckRuleResultsInCell = categoryTableValues[rowIndex, columnIndex];
                        if (healthCheckRuleResultsInCell == null)
                        {
                            healthCheckRuleResultsInCell = new List<HealthCheckRuleResult>();
                            categoryTableValues[rowIndex, columnIndex] = healthCheckRuleResultsInCell;
                        }
                        healthCheckRuleResultsInCell.Add(healthCheckRuleResult);
                    }

                    // Output headers
                    int rowTableStart = 4;
                    int gradeColumnStart = 4;
                    int fromRow = rowTableStart;
                    int fromColumn = gradeColumnStart;
                    sheet.Cells[fromRow, 1].Value = "Controller";
                    sheet.Cells[fromRow, 2].Value = "Application";
                    sheet.Cells[fromRow, 3].Value = "ApplicationID";
                    foreach (KeyValuePair<string, int> categoriesKVP in categoryTableColumnsLookup)
                    {
                        sheet.Cells[fromRow, fromColumn].Value = categoriesKVP.Key;
                        sheet.Cells[fromRow - 1, fromColumn].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<See Details>"")", String.Format(SHEET_HEALTH_CHECK_RULE_CATEGORY_RESULTS_DISPLAY, categoriesKVP.Key));
                        sheet.Cells[fromRow - 1, fromColumn].StyleName = "HyperLinkStyle";
                        fromColumn++;
                    }
                    fromRow++;

                    // Output table
                    for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < numColumns; columnIndex++)
                        {
                            List<HealthCheckRuleResult> healthCheckRuleResultsInCell = categoryTableValues[rowIndex, columnIndex];
                            if (healthCheckRuleResultsInCell != null && healthCheckRuleResultsInCell.Count > 0)
                            {
                                double gradeAverage = Math.Round((double)healthCheckRuleResultsInCell.Sum(h => h.Grade) / healthCheckRuleResultsInCell.Count, 1);

                                sheet.Cells[fromRow + rowIndex, gradeColumnStart + columnIndex].Value = gradeAverage;

                                sheet.Cells[fromRow + rowIndex, 1].Value = healthCheckRuleResultsInCell[0].Controller;
                                sheet.Cells[fromRow + rowIndex, 2].Value = healthCheckRuleResultsInCell[0].Application;
                                sheet.Cells[fromRow + rowIndex, 3].Value = healthCheckRuleResultsInCell[0].ApplicationID;
                            }
                            else
                            {
                                sheet.Cells[fromRow + rowIndex, gradeColumnStart + columnIndex].Value = "-";
                            }
                        }
                    }
                    fromRow = fromRow + numRows;

                    // Insert the table
                    range = sheet.Cells[4, 1, 4 + numRows, 3 + numColumns];
                    table = sheet.Tables.Add(range, TABLE_HEALTH_CHECK_RULE_APPLICATIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.None;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    // Resize the columns
                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Application"].Position + 1).Width = 25;
                    for (int columnIndex = 0; columnIndex < numColumns; columnIndex++)
                    {
                        sheet.Column(gradeColumnStart + columnIndex).Width = 15;
                        // Make the header column cells wrap text for Categories headings
                        sheet.Cells[rowTableStart, gradeColumnStart + columnIndex].Style.WrapText = true;
                    }

                    // Make header row taller
                    sheet.Row(rowTableStart).Height = 40;

                    if (sheet.Dimension.Rows > rowTableStart)
                    {
                        // Color code it
                        ExcelAddress cfGradeNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, 4, sheet.Dimension.Rows, 4 + numColumns);
                        var cfGrade = sheet.ConditionalFormatting.AddThreeColorScale(cfGradeNum);
                        cfGrade.LowValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                        cfGrade.LowValue.Color = colorRedFor3ColorScales;
                        cfGrade.LowValue.Value = 1;
                        cfGrade.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                        cfGrade.MiddleValue.Value = 3;
                        cfGrade.MiddleValue.Color = colorYellowFor3ColorScales;
                        cfGrade.HighValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                        cfGrade.HighValue.Color = colorGreenFor3ColorScales;
                        cfGrade.HighValue.Value = 5;
                    }

                    #endregion

                    #region Output individual categories on separate sheets

                    // Get list of categories for which we'll be making things
                    List<string> listOfCategories = healthCheckRuleResults.Select(h => h.Category).Distinct().ToList<string>();

                    foreach (string category in listOfCategories)
                    {
                        sheet = excelReport.Workbook.Worksheets.Add(String.Format(SHEET_HEALTH_CHECK_RULE_CATEGORY_RESULTS_DISPLAY, category));
                        sheet.Cells[1, 1].Value = "Table of Contents";
                        sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                        sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                        sheet.Cells[2, 1].Value = "See Table";
                        sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_CHECK_RULE_RESULTS);
                        sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                        sheet.Cells[3, 1].Value = "See Display";
                        sheet.Cells[3, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY);
                        sheet.Cells[3, 2].StyleName = "HyperLinkStyle";
                        sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);


                        // Make this following table out of the list of health rule evaluations
                        // Controller | Application | EntityType | EntityName | Rule 1 | Rule 2 | Rule 3
                        // -----------------------------------------------------------------------------
                        // CntrVal    | AppName     | APMApp     | AppName    | Grade  | Grade  | Grade (with comment of value)

                        // To do this, we measure number of rows (Controller/Application/EntityType/EntityName quads) and Columns (Rules within Category), and build a table
                        numRows = healthCheckRuleResults.Where(h => h.Category == category).Select(h => String.Format("{0}/{1}/{2}/{3}", h.Controller, h.Application, h.EntityType, h.EntityName)).Distinct().Count();
                        numColumns = healthCheckRuleResults.Where(h => h.Category == category).Select(h => h.Name).Distinct().Count();

                        Dictionary<string, int> nameTableRowsLookup = new Dictionary<string, int>(numRows);
                        Dictionary<string, int> nameTableColumnsLookup = new Dictionary<string, int>(numColumns);

                        List<HealthCheckRuleResult>[,] nameTableValues = new List<HealthCheckRuleResult>[numRows, numColumns];

                        foreach (HealthCheckRuleResult healthCheckRuleResult in healthCheckRuleResults)
                        {
                            // Only process the rules with the desired category
                            if (healthCheckRuleResult.Category != category) continue;

                            int rowIndex = 0;
                            int columnIndex = 0;

                            string rowIndexValue = String.Format("{0}/{1}/{2}/{3}", healthCheckRuleResult.Controller, healthCheckRuleResult.Application, healthCheckRuleResult.EntityType, healthCheckRuleResult.EntityName);
                            string columnIndexValue = healthCheckRuleResult.Name;

                            if (nameTableRowsLookup.ContainsKey(rowIndexValue) == true)
                            {
                                rowIndex = nameTableRowsLookup[rowIndexValue];
                            }
                            else
                            {
                                rowIndex = nameTableRowsLookup.Count;
                                nameTableRowsLookup.Add(rowIndexValue, rowIndex);
                            }

                            if (nameTableColumnsLookup.ContainsKey(columnIndexValue) == true)
                            {
                                columnIndex = nameTableColumnsLookup[columnIndexValue];
                            }
                            else
                            {
                                columnIndex = nameTableColumnsLookup.Count;
                                nameTableColumnsLookup.Add(columnIndexValue, columnIndex);
                            }

                            // Fill in the cell
                            List<HealthCheckRuleResult> healthCheckRuleResultsInCell = nameTableValues[rowIndex, columnIndex];
                            if (healthCheckRuleResultsInCell == null)
                            {
                                healthCheckRuleResultsInCell = new List<HealthCheckRuleResult>();
                                nameTableValues[rowIndex, columnIndex] = healthCheckRuleResultsInCell;
                            }
                            healthCheckRuleResultsInCell.Add(healthCheckRuleResult);
                        }

                        // Output headers
                        rowTableStart = 4;
                        gradeColumnStart = 6;
                        fromRow = rowTableStart;
                        fromColumn = gradeColumnStart;
                        sheet.Cells[fromRow, 1].Value = "Controller";
                        sheet.Cells[fromRow, 2].Value = "Application";
                        sheet.Cells[fromRow, 3].Value = "ApplicationID";
                        sheet.Cells[fromRow, 4].Value = "EntityType";
                        sheet.Cells[fromRow, 5].Value = "EntityName";
                        foreach (KeyValuePair<string, int> namesKVP in nameTableColumnsLookup)
                        {
                            sheet.Cells[fromRow, fromColumn].Value = namesKVP.Key;
                            fromColumn++;
                        }
                        fromRow++;

                        // Output table
                        for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
                        {
                            for (int columnIndex = 0; columnIndex < numColumns; columnIndex++)
                            {
                                List<HealthCheckRuleResult> healthCheckRuleResultsInCell = nameTableValues[rowIndex, columnIndex];
                                if (healthCheckRuleResultsInCell != null && healthCheckRuleResultsInCell.Count > 0)
                                {
                                    double gradeAverage = Math.Round((double)healthCheckRuleResultsInCell.Sum(h => h.Grade) / healthCheckRuleResultsInCell.Count, 1);

                                    sheet.Cells[fromRow + rowIndex, gradeColumnStart + columnIndex].Value = gradeAverage;

                                    sheet.Cells[fromRow + rowIndex, 1].Value = healthCheckRuleResultsInCell[0].Controller;
                                    sheet.Cells[fromRow + rowIndex, 2].Value = healthCheckRuleResultsInCell[0].Application;
                                    sheet.Cells[fromRow + rowIndex, 3].Value = healthCheckRuleResultsInCell[0].ApplicationID;
                                    sheet.Cells[fromRow + rowIndex, 4].Value = healthCheckRuleResultsInCell[0].EntityType;
                                    sheet.Cells[fromRow + rowIndex, 5].Value = healthCheckRuleResultsInCell[0].EntityName;

                                    StringBuilder sb = new StringBuilder(healthCheckRuleResultsInCell.Count * 128);
                                    for (int k = 0; k < healthCheckRuleResultsInCell.Count; k++)
                                    {
                                        HealthCheckRuleResult healthCheckRuleResult = healthCheckRuleResultsInCell[k];
                                        sb.AppendFormat("{0}: {1}\n", k + 1, wordWrapString(healthCheckRuleResult.Description, 100));
                                    }

                                    // Limit the size of the comment generated to ~2K of text because I think the Excel barfs when the comments are too long.
                                    if (sb.Length > 2500)
                                    {
                                        sb.Length = 2547;
                                        sb.Append("...");
                                    }

                                    // Excessive comments in the workbook lead to poor use experience. Need to rethink and refactor this
                                    ExcelComment comment = sheet.Cells[fromRow + rowIndex, gradeColumnStart + columnIndex].AddComment(sb.ToString(), healthCheckRuleResultsInCell[0].Code);
                                    comment.AutoFit = true;
                                }
                                else
                                {
                                    sheet.Cells[fromRow + rowIndex, gradeColumnStart + columnIndex].Value = "-";
                                }
                            }
                        }
                        fromRow = fromRow + numRows;

                        // Insert the table
                        range = sheet.Cells[4, 1, 4 + numRows, 5 + numColumns];
                        table = sheet.Tables.Add(range, getExcelTableOrSheetSafeString(String.Format(TABLE_HEALTH_CHECK_RULE_CATEGORY_RESULTS, category)));
                        table.ShowHeader = true;
                        table.TableStyle = TableStyles.None;
                        table.TableStyle = TableStyles.Medium2;
                        table.ShowFilter = true;
                        table.ShowTotal = false;

                        // Resize the columns
                        sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                        sheet.Column(table.Columns["Application"].Position + 1).Width = 25;
                        sheet.Column(table.Columns["EntityName"].Position + 1).Width = 25;
                        sheet.Column(table.Columns["EntityType"].Position + 1).Width = 25;
                        for (int columnIndex = 0; columnIndex < numColumns; columnIndex++)
                        {
                            sheet.Column(gradeColumnStart + columnIndex).Width = 15;
                            // Make the header column cells wrap text for Categories headings
                            sheet.Cells[rowTableStart, gradeColumnStart + columnIndex].Style.WrapText = true;
                        }

                        // Make header row taller
                        sheet.Row(rowTableStart).Height = 50;

                        if (sheet.Dimension.Rows > rowTableStart)
                        {
                            // Color code it
                            ExcelAddress cfGradeNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, 5, sheet.Dimension.Rows, 5 + numColumns);
                            var cfGrade = sheet.ConditionalFormatting.AddThreeColorScale(cfGradeNum);
                            cfGrade.LowValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                            cfGrade.LowValue.Color = colorRedFor3ColorScales;
                            cfGrade.LowValue.Value = 1;
                            cfGrade.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                            cfGrade.MiddleValue.Value = 3;
                            cfGrade.MiddleValue.Color = colorYellowFor3ColorScales;
                            cfGrade.HighValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                            cfGrade.HighValue.Color = colorGreenFor3ColorScales;
                            cfGrade.HighValue.Value = 5;
                        }
                    }

                    #endregion
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.HealthCheckResultsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.HealthCheck={0}", programOptions.LicensedReports.HealthCheck);
            loggerConsole.Trace("LicensedReports.HealthCheck={0}", programOptions.LicensedReports.HealthCheck);
            if (programOptions.LicensedReports.HealthCheck == false)
            {
                loggerConsole.Warn("Not licensed for health check");
                return false;
            }

            logger.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            loggerConsole.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            if (jobConfiguration.Output.HealthCheck == false)
            {
                loggerConsole.Trace("Skipping report of health check");
            }
            return (jobConfiguration.Output.HealthCheck == true);
        }

        public string wordWrapString(string stringToBreak, int lengthOfEachLine)
        {
            StringBuilder sb = new StringBuilder(stringToBreak.Length + stringToBreak.Length / lengthOfEachLine);
            string[] stringToBreakWordsArray = stringToBreak.Split(' ');

            int lineLength = 0;
            foreach (string word in stringToBreakWordsArray)
            {
                lineLength = lineLength + word.Length + 1;
                if (lineLength > lengthOfEachLine)
                {
                    sb.AppendLine();
                    lineLength = word.Length + 1;

                }
                sb.Append(word);
                sb.Append(" ");
            }

            return sb.ToString();
        }
    }
}
