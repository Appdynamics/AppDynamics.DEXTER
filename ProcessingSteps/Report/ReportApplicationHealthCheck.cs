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
using System.Drawing;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportApplicationHealthCheck : JobStepReportBase
    {
        #region Constants for report contents
        // --------------------------------------------------
        // Sheets
        private const string SHEET_APP_HEALTHCHECK = "3.Health Check";

        // --------------------------------------------------
        // Tables
        private const string TABLE_APP_HEALTH_CHECK = "t_APP_HealthCheck";


        private const int LIST_SHEET_START_TABLE_AT = 4;

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
                loggerConsole.Info("Prepare Application Health Check Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Application Health Check Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Application Health Check Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);

                #endregion

                #region Entity Sheets

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APP_HEALTHCHECK);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Load Health Check to Sheet

                loggerConsole.Info("Fill Application Health Check Report File");

                sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTHCHECK];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ApplicationHealthCheckCSVFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Application Health Check Report File");

                #region Format Health Check Sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_APP_HEALTHCHECK];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APP_HEALTH_CHECK);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;
                    sheet.DefaultColWidth = 12;

                    //Format Table columns
                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationID"].Position + 1);

                    ExcelAddress cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["BTLockdownEnabled"].Position + 1, sheet.Dimension.Rows, table.Columns["BTLockdownEnabled"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["DeveloperModeOff"].Position + 1, sheet.Dimension.Rows, table.Columns["DeveloperModeOff"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTiers"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumBTs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumBTs"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);

                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["BTOverflow"].Position + 1, sheet.Dimension.Rows, table.Columns["BTOverflow"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["BackendOverflow"].Position + 1, sheet.Dimension.Rows, table.Columns["BackendOverflow"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);


                    //Advanced APM Configurations
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumInfoPoints"].Position + 1, sheet.Dimension.Rows, table.Columns["NumInfoPoints"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumDataCollectorsEnabled"].Position + 1, sheet.Dimension.Rows, table.Columns["NumDataCollectorsEnabled"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);

                    //Alerting Configurations
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["PoliciesActionsEnabled"].Position + 1, sheet.Dimension.Rows, table.Columns["PoliciesActionsEnabled"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["HRViolationsHigh"].Position + 1, sheet.Dimension.Rows, table.Columns["HRViolationsHigh"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);

                    //Infrastructure
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["AppAgentVersion"].Position + 1, sheet.Dimension.Rows, table.Columns["AppAgentVersion"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["MachineAgentVersion"].Position + 1, sheet.Dimension.Rows, table.Columns["MachineAgentVersion"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["MachineAgentEnabledPercent"].Position + 1, sheet.Dimension.Rows, table.Columns["MachineAgentEnabledPercent"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["TiersActivePercent"].Position + 1, sheet.Dimension.Rows, table.Columns["TiersActivePercent"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
                    cfAddress = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NodesActivePercent"].Position + 1, sheet.Dimension.Rows, table.Columns["NodesActivePercent"].Position + 1);
                    AddHealthCheckConditionalFormatting(sheet, cfAddress);
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

                string reportFilePath = FilePathMap.ApplicationHealthCheckExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            logger.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            loggerConsole.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            if (jobConfiguration.Input.Configuration == false || jobConfiguration.Output.HealthCheck == false)
            {
                loggerConsole.Trace("Skipping Health Check Report");
            }
            return (jobConfiguration.Input.Configuration == true && jobConfiguration.Output.HealthCheck == true);
        }

        internal static void AddHealthCheckConditionalFormatting(ExcelWorksheet sheet, ExcelAddress cfAddressAHC)
        {
            //Color Green if True or "Pass"
            var cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressAHC);
            cfUserExperience.Style.Font.Color.Color = Color.Green;
            cfUserExperience.Style.Fill.BackgroundColor.Color = Color.FromArgb(198, 239, 206);
            cfUserExperience.Formula = @"=TRUE";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressAHC);
            cfUserExperience.Style.Font.Color.Color = Color.Green;
            cfUserExperience.Style.Fill.BackgroundColor.Color = Color.FromArgb(198, 239, 206);
            cfUserExperience.Formula = @"=""PASS""";

            //Color Red if False or "Fail" or 0
            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressAHC);
            cfUserExperience.Style.Font.Color.Color = Color.Red;
            cfUserExperience.Style.Fill.BackgroundColor.Color = Color.FromArgb(255, 199, 206);
            cfUserExperience.Formula = @"=FALSE";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressAHC);
            cfUserExperience.Style.Font.Color.Color = Color.Red;
            cfUserExperience.Style.Fill.BackgroundColor.Color = Color.FromArgb(255, 199, 206);
            cfUserExperience.Formula = @"=""FAIL""";

            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressAHC);
            cfUserExperience.Style.Font.Color.Color = Color.Black;
            cfUserExperience.Style.Fill.BackgroundColor.Color = Color.FromArgb(255, 199, 206);
            cfUserExperience.Formula = @"=0";

            //Color Yellow if "Warning" or 2
            cfUserExperience = sheet.ConditionalFormatting.AddEqual(cfAddressAHC);
            cfUserExperience.Style.Font.Color.Color = Color.Yellow;
            cfUserExperience.Style.Fill.BackgroundColor.Color = Color.FromArgb(253,235,156);
            cfUserExperience.Formula = @"=""WARN""";

        }

    }
}