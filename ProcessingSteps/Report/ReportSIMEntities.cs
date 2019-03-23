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
    public class ReportSIMEntities : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_ALL_LIST = "4.Applications.All";
        private const string SHEET_APPLICATIONS_SIM_LIST = "5.Applications.SIM";
        private const string SHEET_TIERS_LIST = "6.Tiers";
        private const string SHEET_NODES_LIST = "7.Nodes";
        private const string SHEET_MACHINES_LIST = "8.Machines";
        private const string SHEET_MACHINES_TYPE_PIVOT = "8.Machines.Type";
        private const string SHEET_MACHINE_PROPERTIES_LIST = "9.Machine Properties";
        private const string SHEET_MACHINE_PROPERTIES_TYPE_PIVOT = "9.Machine Properties.Type";
        private const string SHEET_MACHINE_VOLUMES_LIST = "10.Machine Volumes";
        private const string SHEET_MACHINE_VOLUMES_TYPE_PIVOT = "10.Machine Volumes.Type";
        private const string SHEET_MACHINE_NETWORKS_LIST = "11.Machine Networks";
        private const string SHEET_MACHINE_NETWORKS_TYPE_PIVOT = "11.Machine Networks.Type";
        private const string SHEET_MACHINE_CPUS_LIST = "12.Machine CPUs";
        private const string SHEET_MACHINE_CPUS_TYPE_PIVOT = "12.Machine CPUs.Type";
        private const string SHEET_MACHINE_CONTAINERS_LIST = "13.Machine Containers";
        private const string SHEET_MACHINE_PROCESSES_LIST = "14.Machine Processes";
        private const string SHEET_MACHINE_PROCESSES_TYPE_PIVOT = "14.Machine Processes.Type";

        private const string TABLE_CONTROLLERS = "t_Controllers";
        private const string TABLE_APPLICATIONS_ALL = "t_Applications_All";
        private const string TABLE_APPLICATIONS_SIM = "t_Applications_SIM";
        private const string TABLE_TIERS = "t_Tiers";
        private const string TABLE_NODES = "t_Nodes";
        private const string TABLE_MACHINES = "t_Machines";
        private const string TABLE_MACHINE_PROPERTIES = "t_Machine_Properties";
        private const string TABLE_MACHINE_VOLUMES = "t_Machine_Volumes";
        private const string TABLE_MACHINE_NETWORKS = "t_Machine_Networks";
        private const string TABLE_MACHINE_CPUS = "t_Machine_CPUs";
        private const string TABLE_MACHINE_CONTAINERS = "t_Machine_Containers";
        private const string TABLE_MACHINE_PROCESSES = "t_Machine_Processes";

        private const string PIVOT_MACHINES_TYPE = "p_MachinesType";
        private const string PIVOT_MACHINE_PROPERTIES_TYPE = "p_MachinePropertiesType";
        private const string PIVOT_MACHINE_VOLUMES_TYPE = "p_MachineVolumesType";
        private const string PIVOT_MACHINE_NETWORKS_TYPE = "p_MachineNetworksType";
        private const string PIVOT_MACHINE_CPUS_TYPE = "p_MachineCPUsType";
        private const string PIVOT_MACHINE_PROCESSES_TYPE = "p_MachineProcessesType";

        private const string GRAPH_PIVOT_MACHINES_TYPE = "g_MachinesType";
        private const string GRAPH_MACHINE_PROPERTIES_TYPE = "g_MachinePropertiesType";
        private const string GRAPH_MACHINE_VOLUMES_TYPE = "g_MachineVolumesType";
        private const string GRAPH_MACHINE_NETWORKS_TYPE = "g_MachineNetworksType";
        private const string GRAPH_MACHINE_CPUS_TYPE = "g_MachineCPUsType";
        private const string GRAPH_MACHINE_PROCESSES_TYPE = "g_MachineProcessesType";

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

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_SIM) == 0)
            {
                return true;
            }

            try
            {
                loggerConsole.Info("Prepare Detected SIM Entities Report File");

                #region Prepare the report package

                // Prepare package
                ExcelPackage excelReport = new ExcelPackage();
                excelReport.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
                excelReport.Workbook.Properties.Title = "AppDynamics DEXTER Detected SIM Entities Report";
                excelReport.Workbook.Properties.Subject = programOptions.JobName;

                excelReport.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                #endregion

                #region Parameters sheet

                // Parameters sheet
                ExcelWorksheet sheet = excelReport.Workbook.Worksheets.Add(SHEET_PARAMETERS);

                var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
                hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
                hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

                fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Detected SIM Entities Report");

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

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_APPLICATIONS_SIM_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_TIERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_NODES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Machines";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_PROPERTIES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Properties";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_PROPERTIES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_PROPERTIES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_PROPERTIES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_VOLUMES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Type of Volumes";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_VOLUMES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_VOLUMES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_VOLUMES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_NETWORKS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Networks";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_NETWORKS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_NETWORKS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_NETWORKS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_CPUS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of CPUs";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_CPUS_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_CPUS_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_CPUS_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 3, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_CONTAINERS_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_PROCESSES_LIST);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "Types of Processes";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_PROCESSES_TYPE_PIVOT);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

                sheet = excelReport.Workbook.Worksheets.Add(SHEET_MACHINE_PROCESSES_TYPE_PIVOT);
                sheet.Cells[1, 1].Value = "Table of Contents";
                sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheet.Cells[2, 1].Value = "See Table";
                sheet.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_MACHINE_PROCESSES_LIST);
                sheet.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheet.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + 5, 1);

                #endregion

                loggerConsole.Info("Fill Detected SIM Entities Report File");

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

                #region Applications

                loggerConsole.Info("List of Applications");

                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_SIM_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMApplicationsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Tiers

                loggerConsole.Info("List of Tiers");

                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMTiersReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Nodes

                loggerConsole.Info("List of Nodes");

                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMNodesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machines

                loggerConsole.Info("List of Machines");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachinesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machine Properties

                loggerConsole.Info("List of Machine Properties");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_PROPERTIES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachinePropertiesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machine Volumes

                loggerConsole.Info("List of Machine Volumes");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_VOLUMES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachineVolumesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machine Networks

                loggerConsole.Info("List of Machine Networks");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_NETWORKS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachineNetworksReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machine CPUs

                loggerConsole.Info("List of Machine CPUs");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_CPUS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachineCPUsReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machine Containers

                loggerConsole.Info("List of Machine Containers");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_CONTAINERS_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachineContainersReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                #region Machine Processes

                loggerConsole.Info("List of Machine Processes");

                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_PROCESSES_LIST];
                EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.SIMMachineProcessesReportFilePath(), 0, sheet, LIST_SHEET_START_TABLE_AT, 1);

                #endregion

                loggerConsole.Info("Finalize Detected SIM Entities Report File");

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
                sheet = excelReport.Workbook.Worksheets[SHEET_APPLICATIONS_SIM_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_APPLICATIONS_SIM);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTiers"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTiers"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNodes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNodes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumMachines"].Position + 1, sheet.Dimension.Rows, table.Columns["NumMachines"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Tiers

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_TIERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_TIERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumSegments"].Position + 1, sheet.Dimension.Rows, table.Columns["NumSegments"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNodes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNodes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);
                }

                #endregion

                #region Nodes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_NODES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_NODES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                }

                #endregion

                #region Machines

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineType"].Position + 1).Width = 15;

                    ExcelAddress cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumProps"].Position + 1, sheet.Dimension.Rows, table.Columns["NumProps"].Position + 1);
                    var cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumTags"].Position + 1, sheet.Dimension.Rows, table.Columns["NumTags"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumCPUs"].Position + 1, sheet.Dimension.Rows, table.Columns["NumCPUs"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumVolumes"].Position + 1, sheet.Dimension.Rows, table.Columns["NumVolumes"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    cfAddressNum = new ExcelAddress(LIST_SHEET_START_TABLE_AT + 1, table.Columns["NumNetworks"].Position + 1, sheet.Dimension.Rows, table.Columns["NumNetworks"].Position + 1);
                    cfNum = sheet.ConditionalFormatting.AddDatabar(cfAddressNum, colorLightBlueForDatabars);

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MACHINES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MACHINES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "IsEnabled");
                    addFilterFieldToPivot(pivot, "APMApplicationName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addColumnFieldToPivot(pivot, "MachineType", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "AgentVersion", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(PIVOT_MACHINES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                }

                #endregion

                #region Machine Properties

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_PROPERTIES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINE_PROPERTIES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PropType"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["PropName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["PropValue"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_PROPERTIES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MACHINE_PROPERTIES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "PropType");
                    addRowFieldToPivot(pivot, "PropName");
                    addRowFieldToPivot(pivot, "PropValue");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_MACHINE_PROPERTIES_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Machine Volumes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_VOLUMES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINE_VOLUMES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MountPoint"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Partition"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_VOLUMES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MACHINE_VOLUMES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addRowFieldToPivot(pivot, "Partition");
                    addRowFieldToPivot(pivot, "MountPoint");
                    addRowFieldToPivot(pivot, "SizeMB");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_MACHINE_VOLUMES_TYPE, eChartType.ColumnClustered, pivot);
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

                #region Machine Networks

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_NETWORKS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINE_NETWORKS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["NetworkName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["IP4Address"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["IP4Gateway"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["IP6Address"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["IP6Gateway"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_NETWORKS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 2, 1], range, PIVOT_MACHINE_NETWORKS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Enabled");
                    addFilterFieldToPivot(pivot, "PluggedIn");
                    addFilterFieldToPivot(pivot, "Duplex");
                    addFilterFieldToPivot(pivot, "MTU");
                    addColumnFieldToPivot(pivot, "Speed", eSortType.Descending);
                    addRowFieldToPivot(pivot, "State");
                    addRowFieldToPivot(pivot, "NetworkName");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addRowFieldToPivot(pivot, "IP4Address");
                    addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_MACHINE_NETWORKS_TYPE, eChartType.ColumnClustered, pivot);
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

                #region Machine CPUs

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_CPUS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINE_CPUS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Vendor"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Model"].Position + 1).Width = 25;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_CPUS_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT, 1], range, PIVOT_MACHINE_CPUS_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "Vendor");
                    addColumnFieldToPivot(pivot, "NumCores", eSortType.Descending);
                    addColumnFieldToPivot(pivot, "NumLogical", eSortType.Descending);
                    addRowFieldToPivot(pivot, "Model");
                    addRowFieldToPivot(pivot, "Speed");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

                    ExcelChart chart = sheet.Drawings.AddChart(GRAPH_MACHINE_CPUS_TYPE, eChartType.ColumnClustered, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(800, 300);

                    sheet.Column(1).Width = 20;
                    sheet.Column(2).Width = 20;
                    sheet.Column(3).Width = 20;
                    sheet.Column(4).Width = 20;
                    sheet.Column(5).Width = 20;
                }

                #endregion

                #region Machine Containers

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_CONTAINERS_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINE_CONTAINERS);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ContainerID"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ContainerName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["ImageName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["StartedAt"].Position + 1).Width = 20;
                }

                #endregion

                #region Machine Processes

                // Make table
                sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_PROCESSES_LIST];
                logger.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                loggerConsole.Info("{0} Sheet ({1} rows)", sheet.Name, sheet.Dimension.Rows);
                if (sheet.Dimension.Rows > LIST_SHEET_START_TABLE_AT)
                {
                    range = sheet.Cells[LIST_SHEET_START_TABLE_AT, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
                    table = sheet.Tables.Add(range, TABLE_MACHINE_PROCESSES);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.Medium2;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    sheet.Column(table.Columns["Controller"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["ApplicationName"].Position + 1).Width = 25;
                    sheet.Column(table.Columns["TierName"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["NodeName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["MachineName"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["Class"].Position + 1).Width = 15;
                    sheet.Column(table.Columns["Name"].Position + 1).Width = 10;
                    sheet.Column(table.Columns["CommandLine"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["StartTime"].Position + 1).Width = 20;
                    sheet.Column(table.Columns["EndTime"].Position + 1).Width = 20;

                    // Make pivot
                    sheet = excelReport.Workbook.Worksheets[SHEET_MACHINE_PROCESSES_TYPE_PIVOT];
                    ExcelPivotTable pivot = sheet.PivotTables.Add(sheet.Cells[PIVOT_SHEET_START_PIVOT_AT + 3, 1], range, PIVOT_MACHINE_PROCESSES_TYPE);
                    setDefaultPivotTableSettings(pivot);
                    addFilterFieldToPivot(pivot, "EffectiveGroup");
                    addFilterFieldToPivot(pivot, "EffectiveUser");
                    addFilterFieldToPivot(pivot, "RealGroup");
                    addFilterFieldToPivot(pivot, "RealUser");
                    addFilterFieldToPivot(pivot, "NiceLevel");
                    addColumnFieldToPivot(pivot, "State");
                    addRowFieldToPivot(pivot, "Name", eSortType.Ascending);
                    addRowFieldToPivot(pivot, "CommandLine");
                    addRowFieldToPivot(pivot, "Controller");
                    addRowFieldToPivot(pivot, "TierName");
                    addRowFieldToPivot(pivot, "MachineName");
                    addDataFieldToPivot(pivot, "MachineID", DataFieldFunctions.Count);

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

                if (Directory.Exists(FilePathMap.ReportFolderPath()) == false)
                {
                    Directory.CreateDirectory(FilePathMap.ReportFolderPath());
                }

                string reportFilePath = FilePathMap.SIMEntitiesExcelReportFilePath(jobConfiguration.Input.TimeRange);
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
