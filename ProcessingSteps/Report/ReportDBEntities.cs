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

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportDBEntities : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_APPLICATIONS_DB_LIST = "5.Applications.DB";
        private const string SHEET_COLLECTORS_LIST = "6.Collectors";
        private const string SHEET_COLLECTORS_TYPE_PIVOT = "6.Collectors.Type";
        private const string SHEET_QUERIES_LIST = "7.Queries";
        private const string SHEET_QUERIES_TYPE_PIVOT = "7.Queries.Type";
        private const string SHEET_USERS_LIST = "8.Users";
        private const string SHEET_USERS_TYPE_PIVOT = "8.Users.Type";
        private const string SHEET_SESSIONS_LIST = "9.Sessions";
        private const string SHEET_SESSIONS_TYPE_PIVOT = "9.Sessions.Type";
        private const string SHEET_BLOCKING_SESSIONS_LIST = "10.Blocking Sessions";
        private const string SHEET_CLIENTS_LIST = "11.Clients";
        private const string SHEET_CLIENTS_TYPE_PIVOT = "11.Clients.Type";
        private const string SHEET_DATABASES_LIST = "12.Databases";
        private const string SHEET_DATABASES_TYPE_PIVOT = "12.Databases.Type";
        private const string SHEET_MODULES_LIST = "13.Modules";
        private const string SHEET_MODULES_TYPE_PIVOT = "13.Modules.Type";
        private const string SHEET_PROGRAMS_LIST = "14.Programs";
        private const string SHEET_PROGRAMS_TYPE_PIVOT = "14.Programs.Type";
        private const string SHEET_BUSINESS_TRANSACTIONS_LIST = "15.BTs";
        private const string SHEET_WAITSTATES_LIST = "16.Wait States";
        private const string SHEET_WAITSTATES_TYPE_PIVOT = "16.Wait States.Type";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_APPLICATIONS_DB = "t_Applications_DB";
        private const string TABLE_COLLECTOR_DEFINITIONS = "t_Collector_Definitions";
        private const string TABLE_COLLECTORS = "t_Collectors";
        private const string TABLE_QUERIES = "t_Queries";
        private const string TABLE_USERS = "t_Users";
        private const string TABLE_SESSIONS = "t_Sessions";
        private const string TABLE_BLOCKING_SESSIONS = "t_Blocking_Sessions";
        private const string TABLE_CLIENTS = "t_Clients";
        private const string TABLE_DATABASES = "t_Databases";
        private const string TABLE_MODULES = "t_Modules";
        private const string TABLE_PROGRAMS = "t_Programs";
        private const string TABLE_BUSINESS_TRANSACTIONS = "t_Business_Transactions";
        private const string TABLE_WAITSTATES = "t_Wait_States";

        private const string PIVOT_COLLECTORS_TYPE = "p_CollectorsType";
        private const string PIVOT_QUERIES_TYPE = "p_QueryType";
        private const string PIVOT_USERS_TYPE = "p_UserType";
        private const string PIVOT_SESSIONS_TYPE = "p_SessionType";
        private const string PIVOT_CLIENTS_TYPE = "p_ClientType";
        private const string PIVOT_DATABASES_TYPE = "p_DatabasesType";
        private const string PIVOT_MODULES_TYPE = "p_ModuleType";
        private const string PIVOT_PROGRAMS_TYPE = "p_ProgramsType";
        private const string PIVOT_WAITSTATES_TYPE = "p_WaitStateType";

        private const string GRAPH_COLLECTORS_TYPE = "g_CollectorsType";
        private const string GRAPH_QUERIES_TYPE = "g_QueryType";
        private const string GRAPH_USERS_TYPE = "g_UserType";
        private const string GRAPH_SESSIONS_TYPE = "g_SessionType";
        private const string GRAPH_CLIENTS_TYPE = "g_ClientType";
        private const string GRAPH_DATABASES_TYPE = "g_DatabaseType";
        private const string GRAPH_MODULES_TYPE = "g_ModuleType";
        private const string GRAPH_PROGRAMS_TYPE = "g_ProgramType";
        private const string GRAPH_WAITSTATES_TYPE = "g_WaitStateType";

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

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_DB) == 0)
            {
                logger.Warn("No {0} targets to process", APPLICATION_TYPE_DB);
                loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_DB);

                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Detected DB Entities Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Detected DB Entities Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Detected DB Entities Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_DB_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_COLLECTORS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Definitions";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_COLLECTORS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_COLLECTORS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_COLLECTORS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_QUERIES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Queries";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_QUERIES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_QUERIES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_QUERIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 8, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_USERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Users";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_USERS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_USERS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_USERS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SESSIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Sessions";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SESSIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_SESSIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_SESSIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BLOCKING_SESSIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CLIENTS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Clients";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_CLIENTS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_CLIENTS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_CLIENTS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DATABASES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Databases";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DATABASES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_DATABASES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_DATABASES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MODULES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Modules";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MODULES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MODULES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MODULES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_PROGRAMS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Programs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_PROGRAMS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_PROGRAMS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_PROGRAMS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_BUSINESS_TRANSACTIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WAITSTATES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Wait States";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_WAITSTATES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_WAITSTATES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_WAITSTATES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                #endregion

                loggerConsole.Info("Fill Detected DB Entities Report File");

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

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_DB_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBApplicationsReportFilePath(), 0, typeof(DBApplication), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Collectors

                loggerConsole.Info("List of Collectors");

                sheet = excelReport.Workbook.Worksheets[SHEET_COLLECTORS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBCollectorsReportFilePath(), 0, typeof(DBCollector), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Queries

                loggerConsole.Info("List of Queries");

                sheet = excelReport.Workbook.Worksheets[SHEET_QUERIES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBQueriesReportFilePath(), 0, typeof(DBQuery), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Users

                loggerConsole.Info("List of Users");

                sheet = excelReport.Workbook.Worksheets[SHEET_USERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBUsersReportFilePath(), 0, typeof(DBUser), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Sessions

                loggerConsole.Info("List of Sessions");

                sheet = excelReport.Workbook.Worksheets[SHEET_SESSIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBSessionsReportFilePath(), 0, typeof(DBSession), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Blocking Sessions

                loggerConsole.Info("List of Blocking Sessions");

                sheet = excelReport.Workbook.Worksheets[SHEET_BLOCKING_SESSIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBBlockingSessionsReportFilePath(), 0, typeof(DBBlockingSession), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Clients

                loggerConsole.Info("List of Clients");

                sheet = excelReport.Workbook.Worksheets[SHEET_CLIENTS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBClientsReportFilePath(), 0, typeof(DBClient), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Databases

                loggerConsole.Info("List of Databases");

                sheet = excelReport.Workbook.Worksheets[SHEET_DATABASES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBDatabasesReportFilePath(), 0, typeof(DBDatabase), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Modules

                loggerConsole.Info("List of Modules");

                sheet = excelReport.Workbook.Worksheets[SHEET_MODULES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBModulesReportFilePath(), 0, typeof(DBModule), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Programs

                loggerConsole.Info("List of Programs");

                sheet = excelReport.Workbook.Worksheets[SHEET_PROGRAMS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBProgramsReportFilePath(), 0, typeof(DBProgram), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Business Transactions

                loggerConsole.Info("List of Business Transactions");

                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBBusinessTransactionsReportFilePath(), 0, typeof(DBBusinessTransaction), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Programs

                loggerConsole.Info("List of Wait States");

                sheet = excelReport.Workbook.Worksheets[SHEET_WAITSTATES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.DBWaitStatesReportFilePath(), 0, typeof(DBProgram), sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Detected DB Entities Report File");

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
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_DB_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_DB);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCollectors"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCollectors"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Collectors

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_COLLECTORS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_COLLECTORS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressCalls = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Calls"].Position + 1, sheet.Dimension.Rows, table.Columns["Calls"].Position + 1);
                    var cfCalls = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressCalls);
                    cfCalls.LowValue.Color = colorGreenFor3ColorScales;
                    cfCalls.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfCalls.MiddleValue.Value = 70;
                    cfCalls.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfCalls.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_COLLECTORS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_COLLECTORS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addColumnFieldToPivot(pivot, "CollectorStatus");
                    addDataFieldToPivot(pivot, "CollectorID", DataFieldFunctions.Count, "NumCollectors");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_COLLECTORS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Queries

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_QUERIES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_QUERIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Query"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressAvgExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["AvgExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["AvgExecTime"].Position + 1);
                    var cfAvgExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfAvgExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfAvgExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfAvgExecTime.MiddleValue.Value = 70;
                    cfAvgExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfAvgExecTime.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressCalls = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Calls"].Position + 1, sheet.Dimension.Rows, table.Columns["Calls"].Position + 1);
                    var cfCalls = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressCalls);
                    cfCalls.LowValue.Color = colorGreenFor3ColorScales;
                    cfCalls.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfCalls.MiddleValue.Value = 70;
                    cfCalls.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfCalls.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_QUERIES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 6, 1], range, PIVOT_QUERIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addFilterFieldToPivot(pivot, "AvgExecRange", eSortType.Ascending, true);
                    addFilterFieldToPivot(pivot, "SQLJoinType");
                    addFilterFieldToPivot(pivot, "SQLGroupBy");
                    addFilterFieldToPivot(pivot, "SQLHaving");
                    addFilterFieldToPivot(pivot, "SQLOrderBy");
                    addFilterFieldToPivot(pivot, "SQLUnion");
                    addFilterFieldToPivot(pivot, "SQLWhere");
                    addRowFieldToPivot(pivot, "SQLClauseType");
                    addRowFieldToPivot(pivot, "Query");
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "QueryID", DataFieldFunctions.Count, "NumQueries");
                    addDataFieldToPivot(pivot, "Calls", DataFieldFunctions.Sum, "Calls");
                    addDataFieldToPivot(pivot, "AvgExecTime", DataFieldFunctions.Average, "AvgExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_QUERIES_TYPE, eChartType.ColumnClustered, pivot);
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

                #region Users

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_USERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_USERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DBUserName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_USERS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_USERS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "DBUserName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "UserID", DataFieldFunctions.Count, "NumUsers");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_USERS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Sessions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_SESSIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_SESSIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ClientName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_SESSIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_SESSIONS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "ClientName");
                    addRowFieldToPivot(pivot, "SessionName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "SessionID", DataFieldFunctions.Count, "NumSessions");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_SESSIONS_TYPE, eChartType.ColumnClustered, pivot);
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

                #region Blocking Sessions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_BLOCKING_SESSIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BLOCKING_SESSIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Query"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["BlockTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["BlockTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["FirstOccurrence"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FirstOccurrenceUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["BlockTime"].Position + 1, sheet.Dimension.Rows, table.Columns["BlockTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    //// Make pivot
                    //sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_VOLUMES_TYPE_PIVOT];
                    //ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MACHINE_VOLUMES_TYPE);
                    //setDefaultPivotTableSettings(pivot);
                    //addRowFieldToPivot(pivot, "Partition");
                    //addRowFieldToPivot(pivot, "MountPoint");
                    //addRowFieldToPivot(pivot, "SizeMB");
                    //addRowFieldToPivot(pivot, "Controller");
                    //addRowFieldToPivot(pivot, "TierName");
                    //addRowFieldToPivot(pivot, "MachineName");
                    //addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

                    //ExcelChart chart = sheet.Drawings.AddChart(PIVOT_MACHINE_VOLUMES_TYPE, eChartType.ColumnClustered, pivot);
                    //chart.SetPosition(2, 0, 0, 0);
                    //chart.SetSize(800, 300);

                    //sheet.Column(1).Width = 20;
                    //sheet.Column(2).Width = 20;
                    //sheet.Column(3).Width = 20;
                    //sheet.Column(4).Width = 20;
                    //sheet.Column(5).Width = 20;
                    //sheet.Column(6).Width = 20;
                }

                #endregion

                #region Clients

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_CLIENTS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_CLIENTS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ClientName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_CLIENTS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_CLIENTS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "ClientName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "ClientID", DataFieldFunctions.Count, "NumClients");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_CLIENTS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Databases

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_DATABASES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_DATABASES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DatabaseName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_DATABASES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_DATABASES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "DatabaseName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "DatabaseID", DataFieldFunctions.Count, "NumDBs");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_DATABASES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Modules

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MODULES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MODULES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ModuleName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MODULES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MODULES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "ModuleName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "ModuleID", DataFieldFunctions.Count, "NumModules");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_MODULES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Programs

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_PROGRAMS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_PROGRAMS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ProgramName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_PROGRAMS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_PROGRAMS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "ProgramName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "ProgramID", DataFieldFunctions.Count, "NumPrograms");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");
                    addDataFieldToPivot(pivot, "Weight", DataFieldFunctions.Average, "Weight");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_PROGRAMS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Business Transactions

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_BUSINESS_TRANSACTIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_BUSINESS_TRANSACTIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["BTName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressAvgExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["AvgExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["AvgExecTime"].Position + 1);
                    var cfAvgExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfAvgExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfAvgExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfAvgExecTime.MiddleValue.Value = 70;
                    cfAvgExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfAvgExecTime.HighValue.Color = colorRedFor3ColorScales;

                    ExcelAddress cfAddressCalls = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["Calls"].Position + 1, sheet.Dimension.Rows, table.Columns["Calls"].Position + 1);
                    var cfCalls = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressCalls);
                    cfCalls.LowValue.Color = colorGreenFor3ColorScales;
                    cfCalls.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfCalls.MiddleValue.Value = 70;
                    cfCalls.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfCalls.HighValue.Color = colorRedFor3ColorScales;
                }

                #endregion

                #region Programs

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_WAITSTATES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_WAITSTATES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["CollectorName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CollectorStatus"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["AgentName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Host"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["State"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ExecTime"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["ExecTimeSpan"].Position + 1).Width = 12;
                    sheet.Column(table.Columns["From"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["To"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["FromUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ToUtc"].Position + 1).Width = 20;

                    ExcelAddress cfAddressExecTime = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["ExecTime"].Position + 1, sheet.Dimension.Rows, table.Columns["ExecTime"].Position + 1);
                    var cfExecTime = sheet.ConditionalFormatting.AddThreeColorScale(cfAddressExecTime);
                    cfExecTime.LowValue.Color = colorGreenFor3ColorScales;
                    cfExecTime.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Percentile;
                    cfExecTime.MiddleValue.Value = 70;
                    cfExecTime.MiddleValue.Color = colorYellowFor3ColorScales;
                    cfExecTime.HighValue.Color = colorRedFor3ColorScales;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_WAITSTATES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_WAITSTATES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "AgentName", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CollectorType");
                    addRowFieldToPivot(pivot, "State");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "CollectorName");
                    addRowFieldToPivot(pivot, "Host");
                    addDataFieldToPivot(pivot, "WaitStateID", DataFieldFunctions.Count, "NumWaitStates");
                    addDataFieldToPivot(pivot, "ExecTime", DataFieldFunctions.Average, "ExecTime");

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_WAITSTATES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.DBEntitiesExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
