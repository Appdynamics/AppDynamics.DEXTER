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
    public class ReportControllerUsersGroupsRolesAndPermissions : JobStepReportBase
    {
        #region Constants for report contents

        private const string REPORT_RBAC_SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string REPORT_RBAC_SHEET_USERS_LIST = "4.Users";
        private const string REPORT_RBAC_SHEET_USERS_TYPE_PIVOT = "4.Users.Type";
        private const string REPORT_RBAC_SHEET_GROUPS_LIST = "5.Groups";
        private const string REPORT_RBAC_SHEET_GROUPS_TYPE_PIVOT = "5.Groups.Type";
        private const string REPORT_RBAC_SHEET_ROLES_LIST = "6.Roles";
        private const string REPORT_RBAC_SHEET_ROLES_TYPE_PIVOT = "6.Roles.Type";
        private const string REPORT_RBAC_SHEET_PERMISSIONS_LIST = "7.Permissions";
        private const string REPORT_RBAC_SHEET_PERMISSIONS_TYPE_PIVOT = "7.Permissions.Type";
        private const string REPORT_RBAC_SHEET_USER_PERMISSIONS_LIST = "8.User Permissions";
        private const string REPORT_RBAC_SHEET_USER_PERMISSIONS_TYPE_PIVOT = "8.User Permissions.Type";
        private const string REPORT_RBAC_SHEET_GROUP_MEMBERSHIPS_LIST = "9.Group Memberships";
        private const string REPORT_RBAC_SHEET_ROLE_MEMBERSHIPS_LIST = "10.Role Memberships";

        private const string REPORT_RBAC_TABLE_TOC = "t_TOC";
        private const string REPORT_RBAC_TABLE_CONTROLLERS = "t_Controllers";
        private const string REPORT_RBAC_TABLE_USERS = "t_Users";
        private const string REPORT_RBAC_TABLE_GROUPS = "t_Groups";
        private const string REPORT_RBAC_TABLE_ROLES = "t_Roles";
        private const string REPORT_RBAC_TABLE_PERMISSIONS = "t_Permissions";
        private const string REPORT_RBAC_TABLE_USER_PERMISSIONS = "t_UserPermissions";
        private const string REPORT_RBAC_TABLE_GROUP_MEMBERSHIPS = "t_GroupMemberships";
        private const string REPORT_RBAC_TABLE_ROLE_MEMBERSHIPS = "t_RoleMemberships";

        private const string REPORT_RBAC_PIVOT_USERS = "p_Users";
        private const string REPORT_RBAC_PIVOT_GROUPS = "p_Groups";
        private const string REPORT_RBAC_PIVOT_ROLES = "p_Roles";
        private const string REPORT_RBAC_PIVOT_PERMISSIONS = "p_Permissions";
        private const string REPORT_RBAC_PIVOT_USER_PERMISSIONS = "p_UserPermissions";

        private const string REPORT_RBAC_PIVOT_USERS_GRAPH = "g_Users";
        private const string REPORT_RBAC_PIVOT_GROUPS_GRAPH = "g_Groups";
        private const string REPORT_RBAC_PIVOT_ROLES_GRAPH = "g_Roles";
        private const string REPORT_RBAC_PIVOT_PERMISSIONS_GRAPH = "g_Permissions";
        private const string REPORT_RBAC_PIVOT_USER_PERMISSIONS_GRAPH = "g_UserPermissions";

        private const int REPORT_RBAC_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT = 14;

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
                loggerConsole.Info("Prepare Users, Groups, Roles and Permissions Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER RBAC Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER RBAC Report");

                #endregion

                #region TOC sheet

                // Navigation sheet with link to other sheets
                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TOC);

                #endregion

                #region Entity sheets and their associated pivots

                // Entity sheets
                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_CONTROLLERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_USERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Users";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_USERS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_USERS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_USERS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_GROUPS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Groups";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_GROUPS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_GROUPS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_GROUPS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_ROLES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Roles";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_ROLES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_ROLES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_ROLES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_PERMISSIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Permissions";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_PERMISSIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_PERMISSIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_PERMISSIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_USER_PERMISSIONS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of User Permissions";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_USER_PERMISSIONS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_USER_PERMISSIONS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_RBAC_SHEET_USER_PERMISSIONS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_GROUP_MEMBERSHIPS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(REPORT_RBAC_SHEET_ROLE_MEMBERSHIPS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(REPORT_RBAC_LIST_SHEET_START_TABLE_AT + 1, 1);

                #endregion

                loggerConsole.Info("Fill Users, Groups, Roles and Permissions Report File");

                #region Report file variables

                ExcelRangeBase range = null;
                ExcelTable table = null;

                #endregion

                #region Controllers

                loggerConsole.Info("List of Controllers");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_CONTROLLERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.RBACControllerSummaryReportFilePath(), 0, typeof(RBACControllerSummary), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Users

                loggerConsole.Info("List of Users");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_USERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.UsersReportFilePath(), 0, typeof(RBACUser), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Groups

                loggerConsole.Info("List of Groups");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_GROUPS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.GroupsReportFilePath(), 0, typeof(RBACGroup), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Roles

                loggerConsole.Info("List of Roles");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_ROLES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.RolesReportFilePath(), 0, typeof(RBACRole), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Permissions

                loggerConsole.Info("List of Permissions");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_PERMISSIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.PermissionsReportFilePath(), 0, typeof(RBACPermission), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region User Permissions

                loggerConsole.Info("List of User Permissions");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_USER_PERMISSIONS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.UserPermissionsReportFilePath(), 0, typeof(RBACUserPermission), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Group Memberships

                loggerConsole.Info("List of Group Memberships");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_GROUP_MEMBERSHIPS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.GroupMembershipsReportFilePath(), 0, typeof(RBACGroupMembership), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Role Memberships

                loggerConsole.Info("List of Role Memberships");

                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_ROLE_MEMBERSHIPS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.RoleMembershipsReportFilePath(), 0, typeof(RBACRoleMembership), sheet, REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Users, Groups, Roles and Permissions Report File");

                #region Controllers sheet

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_CONTROLLERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_CONTROLLERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SecurityProvider"].Position + 1).Width = 20;
                }

                #endregion

                #region Users

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_USERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_USERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UserName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["DisplayName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SecurityProvider"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOnUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_USERS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_RBAC_PIVOT_USERS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "CreatedBy");
                    addFilterFieldToPivot(pivot, "UpdatedBy");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "UserName");
                    addColumnFieldToPivot(pivot, "SecurityProvider", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "UserID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_RBAC_PIVOT_USERS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                #endregion

                #region Groups

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_GROUPS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_GROUPS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["GroupName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Description"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["SecurityProvider"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOnUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_GROUPS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_RBAC_PIVOT_GROUPS);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "CreatedBy");
                    addFilterFieldToPivot(pivot, "UpdatedBy");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "GroupName");
                    addColumnFieldToPivot(pivot, "SecurityProvider", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "GroupID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_RBAC_PIVOT_GROUPS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                #endregion

                #region Roles

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_ROLES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_ROLES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RoleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["Description"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["CreatedOnUtc"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOn"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UpdatedOnUtc"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_ROLES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_RBAC_PIVOT_ROLES);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "CreatedBy");
                    addFilterFieldToPivot(pivot, "UpdatedBy");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "RoleName");
                    addDataFieldToPivot(pivot, "RoleID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_RBAC_PIVOT_ROLES_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                }

                #endregion

                #region Permissions

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_PERMISSIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_PERMISSIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RoleName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PermissionName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_PERMISSIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_RBAC_PIVOT_PERMISSIONS);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "RoleName");
                    addRowFieldToPivot(pivot, "EntityName");
                    addRowFieldToPivot(pivot, "PermissionName");
                    addColumnFieldToPivot(pivot, "Allowed");
                    addDataFieldToPivot(pivot, "PermissionID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_RBAC_PIVOT_PERMISSIONS_GRAPH, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region User Permissions

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_USER_PERMISSIONS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_USER_PERMISSIONS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UserName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UserSecurityProvider"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["GroupName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["GroupSecurityProvider"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["RoleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["PermissionName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_USER_PERMISSIONS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[REPORT_RBAC_PIVOT_SHEET_START_PIVOT_AT + REPORT_RBAC_PIVOT_SHEET_CHART_HEIGHT, 1], range, REPORT_RBAC_PIVOT_USER_PERMISSIONS);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "UserName");
                    addRowFieldToPivot(pivot, "GroupName");
                    addRowFieldToPivot(pivot, "RoleName");
                    addRowFieldToPivot(pivot, "EntityName");
                    addRowFieldToPivot(pivot, "PermissionName");
                    addColumnFieldToPivot(pivot, "Allowed");
                    addDataFieldToPivot(pivot, "PermissionID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(REPORT_RBAC_PIVOT_USER_PERMISSIONS_GRAPH, eChartType.ColumnClustered, pivot);
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

                #region Group Memberships

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_GROUP_MEMBERSHIPS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_GROUP_MEMBERSHIPS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["GroupName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["UserName"].Position + 1).Width = 20;
                }

                #endregion

                #region Role Memberships

                // Make table
                sheet = excelReport.Workbook.Worksheets[REPORT_RBAC_SHEET_ROLE_MEMBERSHIPS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > REPORT_RBAC_LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[REPORT_RBAC_LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, REPORT_RBAC_TABLE_ROLE_MEMBERSHIPS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["RoleName"].Position + 1).Width = 30;
                    sheet.Column(table.Columns["EntityName"].Position + 1).Width = 20;
                }

                #endregion

                #region TOC sheet

                // TOC sheet again
                sheet = excelReport.Workbook.Worksheets[SHEET_TOC];
                fillTableOfContentsSheet(sheet, excelReport);

                #endregion

                #region Save file 

                FileIOHelper.CreateFolder(FilePathMap.ReportFolderPath());

                string reportFilePath = FilePathMap.RBACExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
            logger.Trace("LicensedReports.UsersGroupsRolesPermissions={0}", programOptions.LicensedReports.UsersGroupsRolesPermissions);
            loggerConsole.Trace("LicensedReports.UsersGroupsRolesPermissions={0}", programOptions.LicensedReports.UsersGroupsRolesPermissions);
            if (programOptions.LicensedReports.UsersGroupsRolesPermissions == false)
            {
                loggerConsole.Warn("Not licensed for users, groups, roles and permissions");
                return false;
            }

            logger.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            loggerConsole.Trace("Input.UsersGroupsRolesPermissions={0}", jobConfiguration.Input.UsersGroupsRolesPermissions);
            logger.Trace("Output.UsersGroupsRolesPermissions={0}", jobConfiguration.Output.UsersGroupsRolesPermissions);
            loggerConsole.Trace("Output.UsersGroupsRolesPermissions={0}", jobConfiguration.Output.UsersGroupsRolesPermissions);
            if (jobConfiguration.Input.UsersGroupsRolesPermissions == false || jobConfiguration.Output.UsersGroupsRolesPermissions == false)
            {
                loggerConsole.Trace("Skipping users, groups, roles and permissions");
            }
            return (jobConfiguration.Input.UsersGroupsRolesPermissions == true && jobConfiguration.Output.UsersGroupsRolesPermissions == true);
        }
    }
}
