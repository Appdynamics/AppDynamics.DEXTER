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
    public class ReportBSG : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_AGENTS_LIST = "3.Agents";
        private const string SHEET_BT_Info_List = "4.Business Transaction Info";
        private const string SHEET_BACKENDS_LIST = "5.Backends";
        private const string SHEET_BACKEND_CUSTOMIZATION_LIST = "6.Backend Customization";
        private const string SHEET_SERVICE_ENDPOINT_DETECTION_LIST = "6.Service Endpoints";
        private const string SHEET_HEALTH_RULES_DETECTION_LIST = "6.Health Rules";
        private const string SHEET_APPLICATION_OVERHEAD_LIST = "7.Overhead";
        // private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        // private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        //
        // private const string SHEET_HEALTH_CHECK_RULE_RESULTS = "5.Health Check Results";
        // private const string SHEET_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_PIVOT = "5.Health Check Results.Desc";
        // private const string SHEET_HEALTH_CHECK_RULE_RESULTS_DISPLAY = "5.Health Check Results.Display";
        // private const string SHEET_HEALTH_CHECK_RULE_CATEGORY_RESULTS_DISPLAY = "6.{0}";
        //
        // private const string TABLE_CONTROLLERS = "t_Controllers";
        // private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        //
        // private const string TABLE_HEALTH_CHECK_RULE_RESULTS = "t_HealthCheckRuleResults";
        // private const string TABLE_HEALTH_CHECK_RULE_APPLICATIONS = "t_HealthCheckRuleApplications";
        //
        // private const string TABLE_HEALTH_CHECK_RULE_CATEGORY_RESULTS = "t_H_{0}";
        //
        // private const string PIVOT_HEALTH_CHECK_RULE_RESULTS_DESCRIPTION_TYPE = "p_HealthCheckRuleDescription";

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
                loggerConsole.Info("Prepare BSG Report File");
                
                loggerConsole.Info("Executing BSG");

                #region Prepare the report package
                
                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER APM BSG Report";
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
                
                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER APM BSG Report");
                
                #endregion
                
                #region TOC sheet
                
                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);
                
                #endregion
                
                #region Entity sheets and their associated pivot
                
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_AGENTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BT_Info_List);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKENDS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
                
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BACKEND_CUSTOMIZATION_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
                
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SERVICE_ENDPOINT_DETECTION_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);
                
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_HEALTH_RULES_DETECTION_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATION_OVERHEAD_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                
                #endregion

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;
                
                #endregion
                
                loggerConsole.Info("Fill BSG Report File");
                
                #region Agents
                
                loggerConsole.Info("List of Agents");
                
                sheet = excelReport.Workbook.Worksheets[SHEET_AGENTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGAgentResultsExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region BT

                loggerConsole.Info("Business Transactions Info");

                sheet = excelReport.Workbook.Worksheets[SHEET_BT_Info_List];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGBusinessTransactionExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Backends

                loggerConsole.Info("List of Backends");
                
                sheet = excelReport.Workbook.Worksheets[SHEET_BACKENDS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGBackendResultsExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion
                
                #region Backend Customization
                
                loggerConsole.Info("List of Backend Customizations");
                
                sheet = excelReport.Workbook.Worksheets[SHEET_BACKEND_CUSTOMIZATION_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGBackendCustomizationResultsExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion
                
                #region SEPs
                
                loggerConsole.Info("List of SEP Discovery and Customization");
                
                sheet = excelReport.Workbook.Worksheets[SHEET_SERVICE_ENDPOINT_DETECTION_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGSepResultsExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Health Rules

                loggerConsole.Info("List of Health Rule Customizations");
                
                sheet = excelReport.Workbook.Worksheets[SHEET_HEALTH_RULES_DETECTION_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGHealthRuleResultsExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Overhead

                loggerConsole.Info("List of Overhead configurations");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATION_OVERHEAD_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.BSGOverheadResultsExcelReportFilePath(), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion
                
                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);
                
                #endregion
                
                #region Save file 
                
                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());
                
                string reportFilePath = FilePathMap.BSGResultsExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.BSG={0}", programOptions.LicensedReports.BSG);
            loggerConsole.Trace("LicensedReports.BSG={0}", programOptions.LicensedReports.BSG);
            if (programOptions.LicensedReports.BSG == false)
            {
                loggerConsole.Warn("Not licensed for BSG");
                return false;
            }

            logger.Trace("Output.BSG={0}", jobConfiguration.Output.BSG);
            loggerConsole.Trace("Output.BSG={0}", jobConfiguration.Output.BSG);
            if (jobConfiguration.Output.BSG == false)
            {
                loggerConsole.Trace("Skipping report of BSG");
            }
            return (jobConfiguration.Output.BSG == true);
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
