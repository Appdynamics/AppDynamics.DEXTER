using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
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
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMFlowmaps : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";

        private const string SHEET_APPLICATIONS_ACTIVITYFLOW = "4.Applications.Activity Flow";
        private const string SHEET_TIERS_ACTIVITYFLOW = "5.Tiers.Activity Flow";
        private const string SHEET_NODES_ACTIVITYFLOW = "6.Nodes.Activity Flow";
        private const string SHEET_BACKENDS_ACTIVITYFLOW = "7.Backends.Activity Flow";
        private const string SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW = "8.BTs.Activity Flow";

        private const string TABLE_CONTROLLERS = "t_Controllers";

        private const string TABLE_APPLICATIONS_ACTIVITYFLOW = "t_Applications_ActivityFlow";
        private const string TABLE_TIERS_ACTIVITYFLOW = "t_Tiers_ActivityFlow";
        private const string TABLE_NODES_ACTIVITYFLOW = "t_Nodes_ActivityFlow";
        private const string TABLE_BACKENDS_ACTIVITYFLOW = "t_Backends_ActivityFlow";
        private const string TABLE_BUSINESS_TRANSACTIONS_ACTIVITYFLOW = "t_BusinessTransactions_ActivityFlow";

        private const int LIST_SHEET_START_TABLE_AT = 17;
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

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
            {
                logger.Warn("No {0} targets to process", APPLICATION_TYPE_APM);
                loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_APM);

                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Entity Flowmaps Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Entity Flowmaps Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Entity Flowmaps Report");

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
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT - 13 + 1, 1);

                #endregion

                loggerConsole.Info("Fill Entity Flowmaps Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Applications

                loggerConsole.Info("Applications Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationsFlowmapReportFilePath(), 0, typeof(ActivityFlow), sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Tiers

                loggerConsole.Info("Tiers Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.TiersFlowmapReportFilePath(), 0, typeof(ActivityFlow), sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Nodes

                loggerConsole.Info("Nodes Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.NodesFlowmapReportFilePath(), 0, typeof(ActivityFlow), sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Backends

                loggerConsole.Info("Backends Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BackendsFlowmapReportFilePath(), 0, typeof(ActivityFlow), sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                #region Business Transactions

                loggerConsole.Info("Business Transactions Flowmap");

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BusinessTransactionsFlowmapReportFilePath(), 0, typeof(ActivityFlow), sheet, LIST_SHEET_START_TABLE_AT - 13, 1);

                #endregion

                loggerConsole.Info("Finalize Entity Flowmaps Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CONTROLLERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
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
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMApplication.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_TIERS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMTier.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODES_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMNode.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Backends

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BACKENDS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMBackend.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_ACTIVITYFLOW];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT - 13)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT - 13, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BUSINESS_TRANSACTIONS_ACTIVITYFLOW);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    adjustColumnsOfActivityFlowRowTableInMetricReport(APMBusinessTransaction.ENTITY_TYPE, sheet, table);
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.FlowmapsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.Flowmaps={0}", programOptions.LicensedReports.Flowmaps);
            loggerConsole.Trace("LicensedReports.Flowmaps={0}", programOptions.LicensedReports.Flowmaps);
            if (programOptions.LicensedReports.EntityMetrics == false)
            {
                loggerConsole.Warn("Not licensed for entity flowmaps");
                return false;
            }

            logger.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            loggerConsole.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            logger.Trace("Output.Flowmaps={0}", jobConfiguration.Output.Flowmaps);
            loggerConsole.Trace("Output.Flowmaps={0}", jobConfiguration.Output.Flowmaps);
            if (jobConfiguration.Input.Flowmaps == false && jobConfiguration.Output.Flowmaps == false)
            {
                loggerConsole.Trace("Skipping report of entity flowmaps");
            }
            return (jobConfiguration.Input.Flowmaps == true && jobConfiguration.Output.Flowmaps == true);
        }
    }
}
