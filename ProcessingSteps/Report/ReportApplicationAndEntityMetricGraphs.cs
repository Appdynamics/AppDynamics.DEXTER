using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportApplicationAndEntityMetricGraphs : JobStepReportBase
    {
        #region Constants for Entity Metric Graphs Reports

        private const string REPORT_METRICS_GRAPHS_SHEET_CONTROLLERS = "3.Controllers";
        private const string REPORT_METRICS_GRAPHS_SHEET_APPLICATIONS_GRAPHS = "4.App.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_APPLICATIONS_SCATTER = "4.App.Calls Scatter";
        private const string REPORT_METRICS_GRAPHS_SHEET_TIERS_GRAPHS = "5.Tiers.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_TIERS_SCATTER = "5.Tiers.Calls Scatter";
        private const string REPORT_METRICS_GRAPHS_SHEET_NODES_GRAPHS = "6.Nodes.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_NODES_SCATTER = "6.Nodes.Calls Scatter";
        private const string REPORT_METRICS_GRAPHS_SHEET_BACKENDS_GRAPHS = "7.Backends.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_BACKENDS_SCATTER = "7.Backends.Calls Scatter";
        private const string REPORT_METRICS_GRAPHS_SHEET_BUSINESS_TRANSACTIONS_GRAPHS = "8.BTs.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_BUSINESS_TRANSACTIONS_SCATTER = "8.BTs.Calls Scatter";
        private const string REPORT_METRICS_GRAPHS_SHEET_SERVICE_ENDPOINTS_GRAPHS = "9.SEPs.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_SERVICE_ENDPOINTS_SCATTER = "9.SEPs.Calls Scatter";
        private const string REPORT_METRICS_GRAPHS_SHEET_ERRORS_GRAPHS = "10.Errors.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_INFORMATION_POINTS_GRAPHS = "11.IPs.{0}";
        private const string REPORT_METRICS_GRAPHS_SHEET_INFORMATION_POINTS_SCATTER = "11.IPs.Calls Scatter";

        private const string REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS = "Entity.Metrics";

        private const string REPORT_METRICS_GRAPHS_TABLE_TOC = "t_TOC";
        private const string REPORT_METRICS_GRAPHS_TABLE_CONTROLLERS = "t_Controllers";

        // Metric data tables from metric.values.csv
        private const string REPORT_METRICS_GRAPHS_METRIC_TABLE_METRIC_VALUES = "t_Metric_Values_{0}_{1}";

        // Hourly graph data
        private const string REPORT_METRICS_GRAPHS_METRIC_GRAPH = "g_Metrics_{0}_{1:yyyyMMddHHss}_{2}";

        // Hourly scatter data
        private const string REPORT_METRICS_GRAPHS_METRIC_ARTCPMEPM_SCATTER = "g_ScatterARTCPMEPM_{0}_{1:yyyyMMddHHss}_{2}";

        private const string REPORT_METRICS_GRAPHS_TABLE_APPLICATIONS = "t_Applications_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_APPLICATIONS_SCATTER = "t_ApplicationsScatter";
        private const string REPORT_METRICS_GRAPHS_TABLE_TIERS = "t_Tiers_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_TIERS_SCATTER = "t_TiersScatter";
        private const string REPORT_METRICS_GRAPHS_TABLE_NODES = "t_Nodes_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_NODES_SCATTER = "t_NodesScatter";
        private const string REPORT_METRICS_GRAPHS_TABLE_BACKENDS = "t_Backends_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_BACKENDS_SCATTER = "t_BackendsScatter";
        private const string REPORT_METRICS_GRAPHS_TABLE_BUSINESS_TRANSACTIONS = "t_BusinessTransactions_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_BUSINESS_TRANSACTIONS_SCATTER = "t_BusinessTransactionsScatter";
        private const string REPORT_METRICS_GRAPHS_TABLE_SERVICE_ENDPOINTS = "t_ServiceEndpoints_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_SERVICE_ENDPOINTS_SCATTER = "t_ServiceEndpointsScatter";
        private const string REPORT_METRICS_GRAPHS_TABLE_ERRORS = "t_Errors_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_INFORMATION_POINTS = "t_InformationPoints_{0}";
        private const string REPORT_METRICS_GRAPHS_TABLE_INFORMATION_POINTS_SCATTER = "t_InformationPointsScatter";

        private const int REPORT_METRICS_GRAPHS_LIST_SHEET_START_TABLE_AT = 4;
        private const int REPORT_METRICS_GRAPHS_GRAPH_SHEET_START_TABLE_AT = 6;

        // 5 minutes out of 1440 minutes (24 hours) == 0.0034722222222222
        private const double FIVE_MINUTES = 0.0034722222222222;

        #endregion

        #region Constants for metric retrieval and mapping

        // In EntityMetricExtractMapping.csv
        private const string RANGE_ROLLUP_TYPE_AVERAGE = "AVERAGE";
        private const string RANGE_ROLLUP_TYPE_SUM = "SUM";

        private const string METRIC_GRAPH_AXIS_PRIMARY = "PRIMARY";
        private const string METRIC_GRAPH_AXIS_SECONDARY = "SECONDARY";

        private const string TRANSACTIONS_METRICS_SET = "CallsAndResponse";

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

            try
            {
                if (this.ShouldExecute(jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    return true;
                }

                List<MetricExtractMapping> entityMetricExtractMappingList = getMetricsExtractMappingList(jobConfiguration);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Target step variables

                        int numEntitiesTotal = 0;

                        #endregion

                        loggerConsole.Info("Prepare Entity Metrics Graphs Report with {0} metrics", entityMetricExtractMappingList.Count);

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                List<EntityApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<EntityApplication>(FilePathMap.ApplicationIndexFilePath(jobTarget), new ApplicationEntityReportMap());
                                if (applicationsList != null && applicationsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Applications ({0} entities, {1} timeranges)", applicationsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityApplication.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityApplication.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, applicationsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityApplication.ENTITY_FOLDER, EntityApplication.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, applicationsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityApplication.ENTITY_FOLDER, EntityApplication.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(applicationsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, applicationsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Tier

                                List<EntityTier> tiersList = FileIOHelper.ReadListFromCSVFile<EntityTier>(FilePathMap.TiersIndexFilePath(jobTarget), new TierEntityReportMap());
                                if (tiersList != null && tiersList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Tiers ({0} entities, {1} timeranges)", tiersList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityTier.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityTier.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, tiersList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityTier.ENTITY_FOLDER, EntityTier.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, tiersList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityTier.ENTITY_FOLDER, EntityTier.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(tiersList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, tiersList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                List<EntityNode> nodesList = FileIOHelper.ReadListFromCSVFile<EntityNode>(FilePathMap.NodesIndexFilePath(jobTarget), new NodeEntityReportMap());
                                if (nodesList != null && nodesList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Nodes ({0} entities, {1} timeranges)", nodesList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityNode.ENTITY_TYPE && m.Graph.Length > 0).ToList();

                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityNode.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, nodesList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityNode.ENTITY_FOLDER, EntityNode.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, nodesList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityNode.ENTITY_FOLDER, EntityNode.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(nodesList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, nodesList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                List<EntityBackend> backendsList = FileIOHelper.ReadListFromCSVFile<EntityBackend>(FilePathMap.BackendsIndexFilePath(jobTarget), new BackendEntityReportMap());
                                if (backendsList != null && backendsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Backends ({0} entities, {1} timeranges)", backendsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityBackend.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityBackend.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, backendsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityBackend.ENTITY_FOLDER, EntityBackend.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, backendsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityBackend.ENTITY_FOLDER, EntityBackend.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(backendsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, backendsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                List<EntityBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<EntityBusinessTransaction>(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget), new BusinessTransactionEntityReportMap());
                                if (businessTransactionsList != null && businessTransactionsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Business Transactions ({0} entities, {1} timeranges)", businessTransactionsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityBusinessTransaction.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityBusinessTransaction.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, businessTransactionsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityBusinessTransaction.ENTITY_FOLDER, EntityBusinessTransaction.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, businessTransactionsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityBusinessTransaction.ENTITY_FOLDER, EntityBusinessTransaction.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(businessTransactionsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, businessTransactionsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Service Endpoints

                                List<EntityServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<EntityServiceEndpoint>(FilePathMap.ServiceEndpointsIndexFilePath(jobTarget), new ServiceEndpointEntityReportMap());
                                if (serviceEndpointsList != null && serviceEndpointsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Service Endpoints ({0} entities, {1} timeranges)", serviceEndpointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityServiceEndpoint.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityServiceEndpoint.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, serviceEndpointsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityServiceEndpoint.ENTITY_FOLDER, EntityServiceEndpoint.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, serviceEndpointsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityServiceEndpoint.ENTITY_FOLDER, EntityServiceEndpoint.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(serviceEndpointsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, serviceEndpointsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Errors

                                List<EntityError> errorsList = FileIOHelper.ReadListFromCSVFile<EntityError>(FilePathMap.ErrorsIndexFilePath(jobTarget), new ErrorEntityReportMap());
                                if (errorsList != null && errorsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Errors ({0} entities, {1} timeranges)", errorsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityError.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityError.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, errorsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityError.ENTITY_FOLDER, EntityError.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(errorsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, errorsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Information Points

                                List<EntityInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<EntityInformationPoint>(FilePathMap.InformationPointsIndexFilePath(jobTarget), new InformationPointEntityReportMap());
                                if (informationPointsList != null && informationPointsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Information Points ({0} entities, {1} timeranges)", informationPointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == EntityInformationPoint.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, EntityInformationPoint.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, informationPointsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityInformationPoint.ENTITY_FOLDER, EntityInformationPoint.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, informationPointsList.OfType<EntityBase>().ToList(), jobConfiguration, jobTarget, EntityInformationPoint.ENTITY_FOLDER, EntityInformationPoint.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.EntityTypeMetricGraphsExcelReportFilePath(informationPointsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, informationPointsList.Count);
                                }

                                #endregion
                            }
                        );

                        stepTimingTarget.NumEntities = numEntitiesTotal;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
                        return false;
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        this.DisplayJobTargetEndedStatus(jobConfiguration, jobTarget, i + 1, stopWatchTarget);

                        stepTimingTarget.EndTime = DateTime.Now;
                        stepTimingTarget.Duration = stopWatchTarget.Elapsed;
                        stepTimingTarget.DurationMS = stopWatchTarget.ElapsedMilliseconds;

                        List<StepTiming> stepTimings = new List<StepTiming>(1);
                        stepTimings.Add(stepTimingTarget);
                        FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
                    }
                }

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
            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            logger.Trace("Output.EntityMetricGraphs={0}", jobConfiguration.Output.EntityMetricGraphs);
            loggerConsole.Trace("Output.EntityMetricGraphs={0}", jobConfiguration.Output.EntityMetricGraphs);
            if (jobConfiguration.Input.Metrics == false || jobConfiguration.Output.EntityMetricGraphs == false)
            {
                loggerConsole.Trace("Skipping report of entity metric graphs");
            }
            return (jobConfiguration.Input.Metrics == true && jobConfiguration.Output.EntityMetricGraphs == true);
        }

        private ExcelPackage createIndividualEntityMetricGraphsReportTemplate(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            #region Prepare the report package

            // Prepare package
            ExcelPackage excelMetricGraphs = new ExcelPackage();
            excelMetricGraphs.Workbook.Properties.Author = String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version);
            excelMetricGraphs.Workbook.Properties.Title = "AppDynamics DEXTER Entity Metric Graphs Report";
            excelMetricGraphs.Workbook.Properties.Subject = programOptions.JobName;

            excelMetricGraphs.Workbook.Properties.Comments = String.Format("Targets={0}\nFrom={1:o}\nTo={2:o}", jobConfiguration.Target.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

            #endregion

            #region Parameters sheet

            // Parameters sheet
            ExcelWorksheet sheet = excelMetricGraphs.Workbook.Worksheets.Add(REPORT_SHEET_PARAMETERS);

            var hyperLinkStyle = sheet.Workbook.Styles.CreateNamedStyle("HyperLinkStyle");
            hyperLinkStyle.Style.Font.UnderLineType = ExcelUnderLineType.Single;
            hyperLinkStyle.Style.Font.Color.SetColor(colorBlueForHyperlinks);

            var grayTextStyle = sheet.Workbook.Styles.CreateNamedStyle("GrayTextStyle");
            grayTextStyle.Style.Font.Color.SetColor(colorGrayForRepeatedText);

            var metricNameStyle = sheet.Workbook.Styles.CreateNamedStyle("MetricNameStyle");
            metricNameStyle.Style.Font.Size = 8;

            fillReportParametersSheet(sheet, jobConfiguration, "AppDynamics DEXTER Entities Metric Graphs Report");

            ExcelRangeBase range = null;
            ExcelTable table = null;

            #endregion

            #region TOC sheet

            // Navigation sheet with link to other sheets
            sheet = excelMetricGraphs.Workbook.Worksheets.Add(REPORT_SHEET_TOC);

            #endregion

            #region Controller sheet

            sheet = excelMetricGraphs.Workbook.Worksheets.Add(REPORT_METRICS_GRAPHS_SHEET_CONTROLLERS);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(REPORT_METRICS_GRAPHS_LIST_SHEET_START_TABLE_AT + 1, 1);

            range = EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllersReportFilePath(), 0, sheet, REPORT_METRICS_GRAPHS_LIST_SHEET_START_TABLE_AT, 1);
            if (range != null)
            {
                table = sheet.Tables.Add(range, REPORT_METRICS_GRAPHS_TABLE_CONTROLLERS);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Controller"].Position + 1).Width = 25;
                sheet.Column(table.Columns["UserName"].Position + 1).Width = 25;
            }

            #endregion

            #region Metrics sheet

            sheet = excelMetricGraphs.Workbook.Worksheets.Add(REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(REPORT_METRICS_GRAPHS_LIST_SHEET_START_TABLE_AT + 1, 1);

            #endregion

            return excelMetricGraphs;
        }

        private bool finalizeAndSaveIndividualEntityMetricReport(ExcelPackage excelMetricGraphs, string reportFilePath)
        {
            logger.Info("Finalize Entity Metric Graphs Report File {0}", reportFilePath);

            ExcelWorksheet sheet;
            ExcelRangeBase range;
            ExcelTable table;

            #region TOC sheet

            // TOC sheet again
            sheet = excelMetricGraphs.Workbook.Worksheets[REPORT_SHEET_TOC];
            sheet.Cells[1, 1].Value = "Sheet Name";
            sheet.Cells[1, 2].Value = "# Entities";
            sheet.Cells[1, 3].Value = "Link";
            int rowNum = 1;
            foreach (ExcelWorksheet s in excelMetricGraphs.Workbook.Worksheets)
            {
                rowNum++;
                sheet.Cells[rowNum, 1].Value = s.Name;
                sheet.Cells[rowNum, 3].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", s.Name);
                sheet.Cells[rowNum, 3].StyleName = "HyperLinkStyle";
                if (s.Tables.Count == 1)
                {
                    table = s.Tables[0];
                    sheet.Cells[rowNum, 2].Value = table.Address.Rows - 1;
                }
                else if (s.Tables.Count > 0)
                {
                    sheet.Cells[rowNum, 2].Value = String.Format("{0} tables", s.Tables.Count);
                }
            }
            range = sheet.Cells[1, 1, sheet.Dimension.Rows, sheet.Dimension.Columns];
            table = sheet.Tables.Add(range, REPORT_METRICS_GRAPHS_TABLE_TOC);
            table.ShowHeader = true;
            table.TableStyle = TableStyles.Medium2;
            table.ShowFilter = true;
            table.ShowTotal = false;

            sheet.Column(table.Columns["Sheet Name"].Position + 1).Width = 25;
            sheet.Column(table.Columns["# Entities"].Position + 1).Width = 25;

            #endregion

            #region Save file 

            Console.WriteLine();

            // Report files
            logger.Info("Saving Excel report {0}", reportFilePath);
            loggerConsole.Info("Saving Excel report {0}", reportFilePath);

            string folderPath = Path.GetDirectoryName(reportFilePath);
            if (Directory.Exists(folderPath) == false)
            {
                Directory.CreateDirectory(folderPath);
            }

            try
            {
                // Save full report Excel files
                excelMetricGraphs.SaveAs(new FileInfo(reportFilePath));
            }
            catch (InvalidOperationException ex)
            {
                logger.Warn("Unable to save Excel file {0}", reportFilePath);
                logger.Warn(ex);
                loggerConsole.Warn("Unable to save Excel file {0}", reportFilePath);

                return false;
            }

            #endregion

            return true;
        }

        private void fillMetricValueTablesForEntityType(
            ExcelPackage excelReport,
            string sheetName,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            JobTarget jobTarget,
            string entityFolderName)
        {
            ExcelWorksheet sheetMetrics = excelReport.Workbook.Worksheets[sheetName];

            int fromRow = REPORT_METRICS_GRAPHS_LIST_SHEET_START_TABLE_AT;
            int fromColumn = 1;

            // Load each of the metrics in the mapping table that apply to this entity type
            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingList)
            {
                string metricsValuesFilePath = FilePathMap.MetricValuesIndexFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName);
                if (File.Exists(metricsValuesFilePath) == true)
                {
                    ExcelRangeBase range = EPPlusCSVHelper.ReadCSVFileIntoExcelRange(metricsValuesFilePath, 0, sheetMetrics, fromRow, fromColumn);
                    if (range != null)
                    {
                        if (range.Rows == 1)
                        {
                            // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                            range = sheetMetrics.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                        }
                        ExcelTable table = sheetMetrics.Tables.Add(range, String.Format(REPORT_METRICS_GRAPHS_METRIC_TABLE_METRIC_VALUES, entityFolderName, metricExtractMapping.FolderName));
                        table.ShowHeader = true;
                        table.TableStyle = TableStyles.Medium2;
                        table.ShowFilter = true;
                        table.ShowTotal = false;

                        sheetMetrics.Column(table.Columns["Controller"].Position + fromColumn).Width = 15;
                        sheetMetrics.Column(table.Columns["ApplicationName"].Position + fromColumn).Width = 15;
                        sheetMetrics.Column(table.Columns["EntityName"].Position + fromColumn).Width = 15;
                        sheetMetrics.Column(table.Columns["EntityType"].Position + fromColumn).Width = 15;
                        sheetMetrics.Column(table.Columns["MetricName"].Position + fromColumn).Width = 15;
                        sheetMetrics.Column(table.Columns["EventTimeStamp"].Position + fromColumn).Width = 20;

                        fromColumn = fromColumn + range.Columns + 1;
                    }
                }
            }
        }

        private void fillMetricGraphsForEntityType(
            ExcelPackage excelReportMetrics,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            List<EntityBase> entityList,
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            string entityFolderName,
            string entityType)
        {
            ExcelWorksheet sheetMetrics = excelReportMetrics.Workbook.Worksheets[REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS];

            Dictionary<long, Dictionary<string, EntityHourlyMetricValueLocation[]>> entityMetricValuesLocationsDictionary = indexMetricValueLocationsForFasterAccess(jobConfiguration, jobTarget, entityFolderName, entityType);

            // Output all graphs
            var metricMappingsGrouped = entityMetricExtractMappingList.GroupBy(m => new { m.Graph });
            foreach (var metricMappingGroup in metricMappingsGrouped)
            {
                List<MetricExtractMapping> entityMetricExtractMappingForThisGraph = metricMappingGroup.ToList();

                Console.Write("{0} {1} starting for {2} entities ", entityFolderName, metricMappingGroup.Key.Graph, entityList.Count);

                // Measure the width of the time range to be at least 16 cells
                int numCellsPerHourRange = entityMetricExtractMappingForThisGraph.Count * 2;
                if (numCellsPerHourRange < 16) numCellsPerHourRange = 16;

                #region Set variables for the sheet output based on Entity Type

                string sheetName = String.Empty;
                string sheetEntityTableName = String.Empty;
                int columnsBetweenHourRanges = 1;
                int columnsBeforeFirstHourRange = 0;

                switch (entityFolderName)
                {
                    case EntityApplication.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_APPLICATIONS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_APPLICATIONS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 2;
                        break;
                    case EntityTier.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_TIERS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_TIERS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case EntityNode.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_NODES_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_NODES, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityBackend.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_BACKENDS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_BACKENDS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case EntityBusinessTransaction.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_BUSINESS_TRANSACTIONS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_BUSINESS_TRANSACTIONS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityServiceEndpoint.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_SERVICE_ENDPOINTS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_SERVICE_ENDPOINTS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityError.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_ERRORS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_ERRORS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityInformationPoint.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_SHEET_INFORMATION_POINTS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(REPORT_METRICS_GRAPHS_TABLE_INFORMATION_POINTS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 3;
                        break;
                    default:
                        break;
                }

                #endregion

                #region Create sheet and put it in the right place

                // Add new sheet for this graph
                ExcelWorksheet sheetGraphs = excelReportMetrics.Workbook.Worksheets.Add(sheetName);
                sheetGraphs.Cells[1, 1].Value = "TOC";
                sheetGraphs.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheetGraphs.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[2, 1].Value = "See Data";
                sheetGraphs.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS);
                sheetGraphs.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[3, 1].Value = "Controller";
                sheetGraphs.Cells[3, 2].Value = jobTarget.Controller;
                sheetGraphs.Cells[4, 1].Value = "Application";
                sheetGraphs.Cells[4, 2].Value = jobTarget.Application;
                sheetGraphs.Cells[5, 1].Value = "Type";
                sheetGraphs.Cells[5, 2].Value = entityType;

                sheetGraphs.View.FreezePanes(REPORT_METRICS_GRAPHS_GRAPH_SHEET_START_TABLE_AT + 1, columnsBeforeFirstHourRange + 1);

                // Move before all the Metrics sheets
                excelReportMetrics.Workbook.Worksheets.MoveBefore(sheetName, REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS);

                #endregion

                #region Add outlines and time range labels for each of the hourly ranges

                for (int i = 0; i < jobConfiguration.Input.HourlyTimeRanges.Count; i++)
                {
                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[i];

                    int columnIndexTimeRangeStart = columnsBeforeFirstHourRange + 1 + i * columnsBetweenHourRanges + i * numCellsPerHourRange;
                    int columnIndexTimeRangeEnd = columnIndexTimeRangeStart + numCellsPerHourRange;
                    for (int columnIndex = columnIndexTimeRangeStart; columnIndex < columnIndexTimeRangeEnd; columnIndex++)
                    {
                        sheetGraphs.Column(columnIndex).OutlineLevel = 1;
                    }

                    sheetGraphs.Cells[1, columnIndexTimeRangeStart + 1].Value = "From";
                    sheetGraphs.Cells[1, columnIndexTimeRangeStart + 4].Value = "To";
                    sheetGraphs.Cells[2, columnIndexTimeRangeStart].Value = "Local";
                    sheetGraphs.Cells[3, columnIndexTimeRangeStart].Value = "UTC";
                    sheetGraphs.Cells[2, columnIndexTimeRangeStart + 1].Value = jobTimeRange.From.ToLocalTime().ToString("G");
                    sheetGraphs.Cells[2, columnIndexTimeRangeStart + 4].Value = jobTimeRange.To.ToLocalTime().ToString("G");
                    sheetGraphs.Cells[3, columnIndexTimeRangeStart + 1].Value = jobTimeRange.From.ToString("G");
                    sheetGraphs.Cells[3, columnIndexTimeRangeStart + 4].Value = jobTimeRange.To.ToString("G");
                }

                // Output table headers for the entities
                int entityTableHeaderRow = REPORT_METRICS_GRAPHS_GRAPH_SHEET_START_TABLE_AT;
                switch (entityFolderName)
                {
                    case EntityApplication.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "ApplicationName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "HasActivity";
                        break;
                    case EntityTier.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case EntityNode.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "NodeName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityBackend.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "BackendName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BackendType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case EntityBusinessTransaction.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BTName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "BTType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityServiceEndpoint.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "SEPName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "SEPType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityError.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "ErrorName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "ErrorType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityInformationPoint.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "IPName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "IPType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    default:
                        break;
                }
                int currentMaxRow = entityTableHeaderRow + 1;

                #endregion

                // Output entity one at a time
                for (int indexOfEntity = 0; indexOfEntity < entityList.Count; indexOfEntity++)
                {
                    EntityBase entity = entityList[indexOfEntity];

                    string entityNameForExcelTable = getShortenedEntityNameForExcelTable(entity.EntityName, entity.EntityID);

                    bool entityHasActivity = false;

                    // Output graphs for every hour range one at a time
                    for (int indexOfTimeRange = 0; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
                    {
                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[indexOfTimeRange];

                        ExcelChart chartPrimaryAxis = null;
                        ExcelChart chartSecondaryAxis = null;

                        // Output metrics into numbers row and graphs one at a time
                        for (int indexOfMetricMapping = 0; indexOfMetricMapping < entityMetricExtractMappingForThisGraph.Count; indexOfMetricMapping++)
                        {
                            MetricExtractMapping metricExtractMapping = entityMetricExtractMappingForThisGraph[indexOfMetricMapping];

                            #region Headers and legend for each range

                            // Output headers for each of the metrics
                            // Calculate where the index of the column value (ART, Total etc) are going to be output
                            // Sum of (Offset of tables + offset of the time range + number of columns between + the number of the metric
                            int columnIndexOfCurrentRangeBegin = columnsBeforeFirstHourRange + 1 + indexOfTimeRange * columnsBetweenHourRanges + indexOfTimeRange * numCellsPerHourRange;
                            int columnIndexOfValueOfCurrentMetric = columnIndexOfCurrentRangeBegin + indexOfMetricMapping * 2;
                            sheetGraphs.Cells[entityTableHeaderRow - 1, columnIndexOfValueOfCurrentMetric].Value = metricExtractMapping.MetricName;
                            sheetGraphs.Cells[entityTableHeaderRow - 1, columnIndexOfValueOfCurrentMetric].StyleName = "MetricNameStyle";
                            sheetGraphs.Cells[entityTableHeaderRow, columnIndexOfValueOfCurrentMetric].Value = "Sum";
                            sheetGraphs.Cells[entityTableHeaderRow, columnIndexOfValueOfCurrentMetric + 1].Value = "Avg";

                            // Output legend at the top for each of the metrics that we will display
                            sheetGraphs.Cells[entityTableHeaderRow - 2, columnIndexOfValueOfCurrentMetric].Value = LEGEND_THICK_LINE;
                            sheetGraphs.Cells[entityTableHeaderRow - 2, columnIndexOfValueOfCurrentMetric].Style.Font.Color.SetColor(getColorFromHexString(metricExtractMapping.LineColor));

                            #endregion

                            EntityHourlyMetricValueLocation entityMetricValuesLocationForThisMetric = null;
                            if (entityMetricValuesLocationsDictionary.ContainsKey(entity.EntityID) == true &&
                                entityMetricValuesLocationsDictionary[entity.EntityID].ContainsKey(metricExtractMapping.MetricName) == true)
                            {
                                entityMetricValuesLocationForThisMetric = entityMetricValuesLocationsDictionary[entity.EntityID][metricExtractMapping.MetricName][indexOfTimeRange];
                            }

                            if (entityMetricValuesLocationForThisMetric != null)
                            {
                                // We must have some activity here
                                entityHasActivity = true;

                                // Output the numeric values
                                ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(REPORT_METRICS_GRAPHS_METRIC_TABLE_METRIC_VALUES, entityFolderName, metricExtractMapping.FolderName)];

                                if (tableMetrics != null)
                                {
                                    // Get the range that contains the Sum column of the metric
                                    ExcelRangeBase rangeMetricValuesForSum = getSingleColumnRangeFromTable(tableMetrics, "Sum", entityMetricValuesLocationForThisMetric.RowStart, entityMetricValuesLocationForThisMetric.RowEnd);

                                    if (rangeMetricValuesForSum != null)
                                    {
                                        sheetGraphs.Cells[currentMaxRow, columnIndexOfValueOfCurrentMetric].Formula = String.Format(@"=SUM({0})", rangeMetricValuesForSum.FullAddress);

                                        if (metricExtractMapping.RangeRollupType == RANGE_ROLLUP_TYPE_AVERAGE)
                                        {
                                            ExcelRangeBase rangeMetricValuesForCount = getSingleColumnRangeFromTable(tableMetrics, "Count", entityMetricValuesLocationForThisMetric.RowStart, entityMetricValuesLocationForThisMetric.RowEnd);
                                            if (rangeMetricValuesForCount != null)
                                            {
                                                sheetGraphs.Cells[currentMaxRow, columnIndexOfValueOfCurrentMetric + 1].Formula = String.Format(@"=ROUND(SUM({0})/SUM({1}), 0)", rangeMetricValuesForSum.FullAddress, rangeMetricValuesForCount.FullAddress);
                                            }
                                        }
                                        else if (metricExtractMapping.RangeRollupType == RANGE_ROLLUP_TYPE_SUM)
                                        {
                                            int duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                                            sheetGraphs.Cells[currentMaxRow, columnIndexOfValueOfCurrentMetric + 1].Formula = String.Format(@"=ROUND(SUM({0})/{1}, 0)", rangeMetricValuesForSum.FullAddress, duration);
                                        }
                                    }

                                    // Get reference to or create graphs if they don't exist
                                    // Always create primary chart
                                    ExcelChart chart = null;
                                    if (chartPrimaryAxis == null)
                                    {
                                        chartPrimaryAxis = sheetGraphs.Drawings.AddChart(String.Format(REPORT_METRICS_GRAPHS_METRIC_GRAPH, entityType, jobTimeRange.From, entityNameForExcelTable), eChartType.XYScatterLinesNoMarkers);
                                        chartPrimaryAxis.SetPosition(currentMaxRow, 0, columnIndexOfCurrentRangeBegin - 1, 0);
                                        chartPrimaryAxis.SetSize(1020, 200);
                                        chartPrimaryAxis.Style = eChartStyle.Style17;
                                        DateTime intervalStartTime = new DateTime(
                                            jobTimeRange.From.Year,
                                            jobTimeRange.From.Month,
                                            jobTimeRange.From.Day,
                                            jobTimeRange.From.Hour,
                                            0,
                                            0,
                                            DateTimeKind.Utc);
                                        DateTime intervalEndTime = intervalStartTime.AddHours(1);
                                        chartPrimaryAxis.Legend.Remove();
                                        chartPrimaryAxis.XAxis.MinValue = intervalStartTime.ToLocalTime().ToOADate();
                                        chartPrimaryAxis.XAxis.MaxValue = intervalEndTime.ToLocalTime().ToOADate();
                                        chartPrimaryAxis.XAxis.MajorUnit = FIVE_MINUTES;
                                    }
                                    chart = chartPrimaryAxis;

                                    // Create secondary axis only if needed
                                    if (metricExtractMapping.Axis == METRIC_GRAPH_AXIS_SECONDARY)
                                    {
                                        if (chartSecondaryAxis == null)
                                        {
                                            chartSecondaryAxis = chartPrimaryAxis.PlotArea.ChartTypes.Add(eChartType.XYScatterLinesNoMarkers);
                                            chartSecondaryAxis.UseSecondaryAxis = true;
                                            chartSecondaryAxis.Legend.Remove();
                                            chartSecondaryAxis.XAxis.MinValue = chartPrimaryAxis.XAxis.MinValue;
                                            chartSecondaryAxis.XAxis.MaxValue = chartPrimaryAxis.XAxis.MaxValue;
                                            chartSecondaryAxis.XAxis.MajorUnit = chartPrimaryAxis.XAxis.MajorUnit;
                                        }
                                        chart = chartSecondaryAxis;
                                    }

                                    // Output metrics
                                    if (chart != null)
                                    {
                                        // The order of extracted metrics is sorted in EntityID, MetricsID, EventTime
                                        // Therefore all the metrics for single item will stick together
                                        // We want the full hour range indexed, including the beginning of the next hour in this hour's window
                                        // therefore we have to advance 1 extra, but only for all the time ranges except the last one
                                        int advanceToNextHourExtra = 1;
                                        if (indexOfTimeRange == jobConfiguration.Input.HourlyTimeRanges.Count - 1)
                                        {
                                            advanceToNextHourExtra = 0;
                                        }

                                        ExcelRangeBase rangeYValues = getSingleColumnRangeFromTable(tableMetrics, "Value", entityMetricValuesLocationForThisMetric.RowStart, entityMetricValuesLocationForThisMetric.RowEnd + advanceToNextHourExtra);
                                        ExcelRangeBase rangeXTime = getSingleColumnRangeFromTable(tableMetrics, "EventTime", entityMetricValuesLocationForThisMetric.RowStart, entityMetricValuesLocationForThisMetric.RowEnd + advanceToNextHourExtra);
                                        if (rangeYValues != null && rangeXTime != null)
                                        {
                                            ExcelChartSerie series = chart.Series.Add(rangeYValues, rangeXTime);
                                            series.Header = metricExtractMapping.MetricName;
                                            ((ExcelScatterChartSerie)series).LineColor = getColorFromHexString(metricExtractMapping.LineColor);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    #region Output entity detail in table

                    // Output the first row
                    switch (entityFolderName)
                    {
                        case EntityApplication.ENTITY_FOLDER:
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entity.ApplicationName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityHasActivity;
                            break;
                        case EntityTier.ENTITY_FOLDER:
                            EntityTier entityTier = (EntityTier)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityTier.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityTier.TierType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case EntityNode.ENTITY_FOLDER:
                            EntityNode entityNode = (EntityNode)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityNode.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityNode.NodeName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityNode.AgentType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityBackend.ENTITY_FOLDER:
                            EntityBackend entityBackend = (EntityBackend)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBackend.BackendName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBackend.BackendType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case EntityBusinessTransaction.ENTITY_FOLDER:
                            EntityBusinessTransaction entityBusinessTransaction = (EntityBusinessTransaction)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBusinessTransaction.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBusinessTransaction.BTName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityBusinessTransaction.BTType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityServiceEndpoint.ENTITY_FOLDER:
                            EntityServiceEndpoint entityServiceEndpoint = (EntityServiceEndpoint)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityServiceEndpoint.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityServiceEndpoint.SEPName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityServiceEndpoint.SEPType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityError.ENTITY_FOLDER:
                            EntityError entityError = (EntityError)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityError.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityError.ErrorName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityError.ErrorType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityInformationPoint.ENTITY_FOLDER:
                            EntityInformationPoint entityInformationPoint = (EntityInformationPoint)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityInformationPoint.IPName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityInformationPoint.IPType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        default:
                            break;
                    }

                    // Make copes of the first entity row in gray color if the graphs were output, to support filtering
                    if (entityHasActivity == true)
                    {
                        for (int rowIndex = 1; rowIndex <= 10; rowIndex++)
                        {
                            for (int columnIndex = 1; columnIndex <= columnsBeforeFirstHourRange; columnIndex++)
                            {
                                sheetGraphs.Cells[currentMaxRow + rowIndex, columnIndex].Value = sheetGraphs.Cells[currentMaxRow, columnIndex].Value;
                                sheetGraphs.Cells[currentMaxRow + rowIndex, columnIndex].StyleName = "GrayTextStyle";
                            }
                        }
                        currentMaxRow = currentMaxRow + 10;
                    }
                    currentMaxRow++;

                    #endregion

                    if (indexOfEntity % 50 == 0)
                    {
                        Console.Write("[{0}].", indexOfEntity);
                    }
                }

                #region Create tables for entities

                if (sheetGraphs.Dimension.Rows > entityTableHeaderRow)
                {
                    ExcelRangeBase range = sheetGraphs.Cells[entityTableHeaderRow, 1, sheetGraphs.Dimension.Rows, columnsBeforeFirstHourRange];
                    ExcelTable table = sheetGraphs.Tables.Add(range, sheetEntityTableName);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.None;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    for (int i = 1; i < columnsBeforeFirstHourRange; i++)
                    {
                        sheetGraphs.Column(i).Width = 20;
                    }
                }

                #endregion

                Console.WriteLine("{0} {1} complete", entityFolderName, metricMappingGroup.Key.Graph);
            }

            return;
        }

        private void fillTransactionalScatterPlotsForEntityType(
            ExcelPackage excelReportMetrics,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            List<EntityBase> entityList,
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            string entityFolderName,
            string entityType)
        {
            ExcelWorksheet sheetMetrics = excelReportMetrics.Workbook.Worksheets[REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS];

            // Load metric index locations file for later use
            List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = FileIOHelper.ReadListFromCSVFile<EntityHourlyMetricValueLocation>(FilePathMap.MetricsLocationIndexFilePath(jobTarget, entityFolderName), new EntityHourlyMetricValueLocationReportMap());

            // Process metrics in the CPM, ART and EPM group
            List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType && m.Graph == TRANSACTIONS_METRICS_SET).ToList();

            Dictionary<long, Dictionary<string, EntityHourlyMetricValueLocation[]>> entityMetricValuesLocationsDictionary = indexMetricValueLocationsForFasterAccess(jobConfiguration, jobTarget, entityFolderName, entityType);

            if (entityMetricExtractMappingListFiltered.Count > 0)
            {
                #region Set variables for the sheet output based on Entity Type

                string sheetName = String.Empty;
                string sheetEntityTableName = String.Empty;
                int columnsBetweenHourRanges = 1;
                int columnsBeforeFirstHourRange = 0;
                int numCellsPerHourRange = 5;

                switch (entityFolderName)
                {
                    case EntityApplication.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_APPLICATIONS_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_APPLICATIONS_SCATTER;
                        columnsBeforeFirstHourRange = 2;
                        break;
                    case EntityTier.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_TIERS_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_TIERS_SCATTER;
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case EntityNode.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_NODES_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_NODES_SCATTER;
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityBackend.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_BACKENDS_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_BACKENDS_SCATTER;
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case EntityBusinessTransaction.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_BUSINESS_TRANSACTIONS_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_BUSINESS_TRANSACTIONS_SCATTER;
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityServiceEndpoint.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_SERVICE_ENDPOINTS_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_SERVICE_ENDPOINTS_SCATTER;
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case EntityError.ENTITY_FOLDER:
                        return;
                    case EntityInformationPoint.ENTITY_FOLDER:
                        sheetName = REPORT_METRICS_GRAPHS_SHEET_INFORMATION_POINTS_SCATTER;
                        sheetEntityTableName = REPORT_METRICS_GRAPHS_TABLE_INFORMATION_POINTS_SCATTER;
                        columnsBeforeFirstHourRange = 3;
                        break;
                    default:
                        break;
                }

                // Get the trusty trio of metrics from the mapping. Should always be there, but best to check
                MetricExtractMapping memART = entityMetricExtractMappingListFiltered.Where(m => m.MetricName == METRIC_ART_FULLNAME).FirstOrDefault();
                MetricExtractMapping memCPM = entityMetricExtractMappingListFiltered.Where(m => m.MetricName == METRIC_CPM_FULLNAME).FirstOrDefault();
                MetricExtractMapping memEPM = entityMetricExtractMappingListFiltered.Where(m => m.MetricName == METRIC_EPM_FULLNAME).FirstOrDefault();

                if (memART == null && memCPM == null && memEPM == null) return;

                #endregion

                #region Create sheet and put it in the right place

                // Add new sheet for this graph
                ExcelWorksheet sheetGraphs = excelReportMetrics.Workbook.Worksheets.Add(sheetName);
                sheetGraphs.Cells[1, 1].Value = "TOC";
                sheetGraphs.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_SHEET_TOC);
                sheetGraphs.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[2, 1].Value = "See Data";
                sheetGraphs.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", REPORT_METRICS_GRAPHS_SHEET_ENTITIES_METRICS);
                sheetGraphs.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[3, 1].Value = "Controller";
                sheetGraphs.Cells[3, 2].Value = jobTarget.Controller;
                sheetGraphs.Cells[4, 1].Value = "Application";
                sheetGraphs.Cells[4, 2].Value = jobTarget.Application;
                sheetGraphs.Cells[5, 1].Value = "Type";
                sheetGraphs.Cells[5, 2].Value = entityType;

                sheetGraphs.View.FreezePanes(REPORT_METRICS_GRAPHS_GRAPH_SHEET_START_TABLE_AT + 1, columnsBeforeFirstHourRange + 1);

                // Move before all the Graphs sheets
                excelReportMetrics.Workbook.Worksheets.MoveAfter(sheetName, REPORT_METRICS_GRAPHS_SHEET_CONTROLLERS);

                #endregion

                #region Add outlines and time range labels for each of the hourly ranges

                for (int i = 0; i < jobConfiguration.Input.HourlyTimeRanges.Count; i++)
                {
                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[i];

                    int columnIndexTimeRangeStart = columnsBeforeFirstHourRange + 1 + i * columnsBetweenHourRanges + i * numCellsPerHourRange;
                    int columnIndexTimeRangeEnd = columnIndexTimeRangeStart + numCellsPerHourRange;
                    for (int columnIndex = columnIndexTimeRangeStart; columnIndex < columnIndexTimeRangeEnd; columnIndex++)
                    {
                        sheetGraphs.Column(columnIndex).OutlineLevel = 1;
                    }

                    sheetGraphs.Cells[1, columnIndexTimeRangeStart + 1].Value = "From";
                    sheetGraphs.Cells[1, columnIndexTimeRangeStart + 4].Value = "To";
                    sheetGraphs.Cells[2, columnIndexTimeRangeStart].Value = "Local";
                    sheetGraphs.Cells[3, columnIndexTimeRangeStart].Value = "UTC";
                    sheetGraphs.Cells[2, columnIndexTimeRangeStart + 1].Value = jobTimeRange.From.ToLocalTime().ToString("G");
                    sheetGraphs.Cells[2, columnIndexTimeRangeStart + 4].Value = jobTimeRange.To.ToLocalTime().ToString("G");
                    sheetGraphs.Cells[3, columnIndexTimeRangeStart + 1].Value = jobTimeRange.From.ToString("G");
                    sheetGraphs.Cells[3, columnIndexTimeRangeStart + 4].Value = jobTimeRange.To.ToString("G");
                }

                // Output table headers for the entities
                int entityTableHeaderRow = REPORT_METRICS_GRAPHS_GRAPH_SHEET_START_TABLE_AT;
                switch (entityFolderName)
                {
                    case EntityApplication.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "ApplicationName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "HasActivity";
                        break;
                    case EntityTier.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case EntityNode.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "NodeName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityBackend.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "BackendName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BackendType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case EntityBusinessTransaction.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BTName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "BTType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityServiceEndpoint.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "SEPName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "SEPType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityError.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "ErrorName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "ErrorType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case EntityInformationPoint.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "IPName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "IPType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    default:
                        break;
                }
                int currentMaxRow = entityTableHeaderRow + 1;

                #endregion

                Console.Write("{0} {1} starting for {2} entities ", entityFolderName, sheetName, entityList.Count);

                // Output entity one at a time
                for (int indexOfEntity = 0; indexOfEntity < entityList.Count; indexOfEntity++)
                {
                    EntityBase entity = entityList[indexOfEntity];

                    string entityNameForExcelTable = getShortenedEntityNameForExcelTable(entity.EntityName, entity.EntityID);

                    bool entityHasActivity = false;

                    // Output graphs for every hour range one at a time
                    for (int indexOfTimeRange = 0; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
                    {
                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[indexOfTimeRange];

                        #region Headers and legend for each range

                        int columnIndexOfCurrentRangeBegin = columnsBeforeFirstHourRange + 1 + indexOfTimeRange * columnsBetweenHourRanges + indexOfTimeRange * numCellsPerHourRange;

                        #endregion

                        EntityHourlyMetricValueLocation entityMetricValuesLocationsForART = null;
                        if (entityMetricValuesLocationsDictionary.ContainsKey(entity.EntityID) == true &&
                            entityMetricValuesLocationsDictionary[entity.EntityID].ContainsKey(memART.MetricName) == true)
                        {
                            entityMetricValuesLocationsForART = entityMetricValuesLocationsDictionary[entity.EntityID][memART.MetricName][indexOfTimeRange];
                        }

                        EntityHourlyMetricValueLocation entityMetricValuesLocationsForCPM = null;
                        if (entityMetricValuesLocationsDictionary.ContainsKey(entity.EntityID) == true &&
                            entityMetricValuesLocationsDictionary[entity.EntityID].ContainsKey(memCPM.MetricName) == true)
                        {
                            entityMetricValuesLocationsForCPM = entityMetricValuesLocationsDictionary[entity.EntityID][memCPM.MetricName][indexOfTimeRange];
                        }

                        EntityHourlyMetricValueLocation entityMetricValuesLocationsForEPM = null;
                        if (entityMetricValuesLocationsDictionary.ContainsKey(entity.EntityID) == true &&
                            entityMetricValuesLocationsDictionary[entity.EntityID].ContainsKey(memEPM.MetricName) == true)
                        {
                            entityMetricValuesLocationsForEPM = entityMetricValuesLocationsDictionary[entity.EntityID][memEPM.MetricName][indexOfTimeRange];
                        }

                        // Get ranges with the metrics
                        ExcelRangeBase rangeARTValues = null;
                        ExcelRangeBase rangeCPMValues = null;
                        ExcelRangeBase rangeEPMValues = null;

                        if (entityMetricValuesLocationsForART != null)
                        {
                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(REPORT_METRICS_GRAPHS_METRIC_TABLE_METRIC_VALUES, entityFolderName, memART.FolderName)];
                            if (tableMetrics != null)
                            {
                                rangeARTValues = getSingleColumnRangeFromTable(tableMetrics, "Value", entityMetricValuesLocationsForART.RowStart, entityMetricValuesLocationsForART.RowEnd);
                            }
                        }

                        if (entityMetricValuesLocationsForCPM != null)
                        {
                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(REPORT_METRICS_GRAPHS_METRIC_TABLE_METRIC_VALUES, entityFolderName, memCPM.FolderName)];
                            if (tableMetrics != null)
                            {
                                rangeCPMValues = getSingleColumnRangeFromTable(tableMetrics, "Value", entityMetricValuesLocationsForCPM.RowStart, entityMetricValuesLocationsForCPM.RowEnd);
                            }
                        }

                        if (entityMetricValuesLocationsForEPM != null)
                        {
                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(REPORT_METRICS_GRAPHS_METRIC_TABLE_METRIC_VALUES, entityFolderName, memEPM.FolderName)];
                            if (tableMetrics != null)
                            {
                                rangeEPMValues = getSingleColumnRangeFromTable(tableMetrics, "Value", entityMetricValuesLocationsForEPM.RowStart, entityMetricValuesLocationsForEPM.RowEnd);
                            }
                        }

                        if ((rangeARTValues != null && rangeCPMValues != null) ||
                            (rangeARTValues != null && rangeEPMValues != null))
                        {
                            // We must have some activity here
                            entityHasActivity = true;

                            ExcelChart chart = sheetGraphs.Drawings.AddChart(String.Format(REPORT_METRICS_GRAPHS_METRIC_ARTCPMEPM_SCATTER, entityType, jobTimeRange.From, entityNameForExcelTable), eChartType.XYScatter);
                            ExcelScatterChart chart1 = (ExcelScatterChart)chart;
                            chart.SetPosition(currentMaxRow - 1, 0, columnIndexOfCurrentRangeBegin - 1, 0);
                            chart.SetSize(300, 300);
                            chart.Legend.Remove();
                            chart.YAxis.Title.Text = memART.MetricName;
                            chart.YAxis.Title.Font.Size = 8;
                            chart.XAxis.Title.Text = memCPM.MetricName;
                            chart.XAxis.Title.Font.Size = 8;
                            chart.Style = eChartStyle.Style10;

                            if (rangeARTValues != null && rangeCPMValues != null)
                            {
                                ExcelChartSerie series = chart.Series.Add(rangeCPMValues, rangeARTValues);
                                ExcelScatterChartSerie series1 = (ExcelScatterChartSerie)series;
                                series.Header = String.Format("{0} vs {1}", memART.MetricName, memCPM.MetricName);
                                series1.DataLabel.ShowValue = true;
                                series1.DataLabel.ShowCategory = true;
                                series1.DataLabel.ShowValue = false;
                                series1.DataLabel.ShowCategory = false;
                                series1.DataLabel.Position = eLabelPosition.Top;
                                series1.MarkerColor = getColorFromHexString(memCPM.LineColor);
                                series1.Marker = eMarkerStyle.Circle;
                                ExcelChartTrendline tl = series1.TrendLines.Add(eTrendLine.Linear);
                                tl.DisplayEquation = false;
                                tl.DisplayRSquaredValue = false;
                            }

                            if (rangeARTValues != null && rangeEPMValues != null)
                            {
                                ExcelChartSerie series = chart.Series.Add(rangeEPMValues, rangeARTValues);
                                ExcelScatterChartSerie series1 = (ExcelScatterChartSerie)series;
                                series.Header = String.Format("{0} vs {1}", memART.MetricName, memEPM.MetricName);
                                series1.DataLabel.ShowValue = true;
                                series1.DataLabel.ShowCategory = true;
                                series1.DataLabel.ShowValue = false;
                                series1.DataLabel.ShowCategory = false;
                                series1.DataLabel.Position = eLabelPosition.Top;
                                series1.MarkerColor = getColorFromHexString(memEPM.LineColor);
                                series1.Marker = eMarkerStyle.Circle;
                                ExcelChartTrendline tl = series1.TrendLines.Add(eTrendLine.Linear);
                                tl.DisplayEquation = false;
                                tl.DisplayRSquaredValue = false;
                            }
                        }
                    }

                    #region Output entity detail in table

                    // Output the first row
                    switch (entityFolderName)
                    {
                        case EntityApplication.ENTITY_FOLDER:
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entity.ApplicationName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityHasActivity;
                            break;
                        case EntityTier.ENTITY_FOLDER:
                            EntityTier entityTier = (EntityTier)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityTier.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityTier.TierType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case EntityNode.ENTITY_FOLDER:
                            EntityNode entityNode = (EntityNode)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityNode.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityNode.NodeName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityNode.AgentType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityBackend.ENTITY_FOLDER:
                            EntityBackend entityBackend = (EntityBackend)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBackend.BackendName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBackend.BackendType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case EntityBusinessTransaction.ENTITY_FOLDER:
                            EntityBusinessTransaction entityBusinessTransaction = (EntityBusinessTransaction)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBusinessTransaction.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBusinessTransaction.BTName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityBusinessTransaction.BTType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityServiceEndpoint.ENTITY_FOLDER:
                            EntityServiceEndpoint entityServiceEndpoint = (EntityServiceEndpoint)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityServiceEndpoint.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityServiceEndpoint.SEPName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityServiceEndpoint.SEPType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityError.ENTITY_FOLDER:
                            EntityError entityError = (EntityError)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityError.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityError.ErrorName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityError.ErrorType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case EntityInformationPoint.ENTITY_FOLDER:
                            EntityInformationPoint entityInformationPoint = (EntityInformationPoint)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityInformationPoint.IPName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityInformationPoint.IPType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        default:
                            break;
                    }

                    // Make copes of the first entity row in gray color if the graphs were output, to support filtering
                    if (entityHasActivity == true)
                    {
                        for (int rowIndex = 1; rowIndex <= 14; rowIndex++)
                        {
                            for (int columnIndex = 1; columnIndex <= columnsBeforeFirstHourRange; columnIndex++)
                            {
                                sheetGraphs.Cells[currentMaxRow + rowIndex, columnIndex].Value = sheetGraphs.Cells[currentMaxRow, columnIndex].Value;
                                sheetGraphs.Cells[currentMaxRow + rowIndex, columnIndex].StyleName = "GrayTextStyle";
                            }
                        }
                        currentMaxRow = currentMaxRow + 14;
                    }
                    currentMaxRow++;

                    #endregion

                    if (indexOfEntity % 50 == 0)
                    {
                        Console.Write("[{0}].", indexOfEntity);
                    }
                }

                #region Create tables for entities

                if (sheetGraphs.Dimension.Rows > entityTableHeaderRow)
                {
                    ExcelRangeBase range = sheetGraphs.Cells[entityTableHeaderRow, 1, sheetGraphs.Dimension.Rows, columnsBeforeFirstHourRange];
                    ExcelTable table = sheetGraphs.Tables.Add(range, sheetEntityTableName);
                    table.ShowHeader = true;
                    table.TableStyle = TableStyles.None;
                    table.ShowFilter = true;
                    table.ShowTotal = false;

                    for (int i = 1; i < columnsBeforeFirstHourRange; i++)
                    {
                        sheetGraphs.Column(i).Width = 20;
                    }
                }

                #endregion

                Console.WriteLine("{0} {1} complete", entityFolderName, sheetName);
            }
        }

        private Dictionary<long, Dictionary<string, EntityHourlyMetricValueLocation[]>> indexMetricValueLocationsForFasterAccess(
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            string entityFolderName,
            string entityType)
        {
            // Load metric index locations file for later use
            List<EntityHourlyMetricValueLocation> entityMetricValuesLocations = FileIOHelper.ReadListFromCSVFile<EntityHourlyMetricValueLocation>(FilePathMap.MetricsLocationIndexFilePath(jobTarget, entityFolderName), new EntityHourlyMetricValueLocationReportMap());

            // Index the metric value locations into this funky dictionary of dictionaries with array in following hierarchy
            //      Entity ID (123)
            //          Metric Name (ART, CPM)
            //              Time range 0
            //              ...
            //              Time range N
            Dictionary<long, Dictionary<string, EntityHourlyMetricValueLocation[]>> entityMetricValuesLocationsDictionary = null;

            // Group them by Entity ID first
            var entityMetricValuesLocationsGroupedByEntityID = entityMetricValuesLocations.GroupBy(m => new { m.EntityID });
            if (entityMetricValuesLocationsGroupedByEntityID != null)
            {
                entityMetricValuesLocationsDictionary = new Dictionary<long, Dictionary<string, EntityHourlyMetricValueLocation[]>>(entityMetricValuesLocationsGroupedByEntityID.Count());
                foreach (var entityMetricMappingsGroupForEntity in entityMetricValuesLocationsGroupedByEntityID)
                {
                    List<EntityHourlyMetricValueLocation> entityHourlyMetricValueLocationsForThisEntity = entityMetricMappingsGroupForEntity.ToList();

                    // Group them by Metric Name second
                    var entityMetricValuesLocationsGroupedByMetricName = entityHourlyMetricValueLocationsForThisEntity.GroupBy(m => new { m.MetricName });

                    long entityID = entityMetricMappingsGroupForEntity.Key.EntityID;
                    entityMetricValuesLocationsDictionary.Add(entityID, new Dictionary<string, EntityHourlyMetricValueLocation[]>(5));
                    foreach (var entityMetricMappingsGroupForEntityMetric in entityMetricValuesLocationsGroupedByMetricName)
                    {
                        List<EntityHourlyMetricValueLocation> entityHourlyMetricValueLocationsForThisEntityAndMetric = entityMetricMappingsGroupForEntityMetric.ToList();

                        // Preallocate the array for each of the timeranges in anticipation of metric data to be inserted there
                        EntityHourlyMetricValueLocation[] entityHourlyMetricValueLocationsForThisEntityAndMetricArray = new EntityHourlyMetricValueLocation[jobConfiguration.Input.HourlyTimeRanges.Count];
                        entityMetricValuesLocationsDictionary[entityID].Add(entityMetricMappingsGroupForEntityMetric.Key.MetricName, entityHourlyMetricValueLocationsForThisEntityAndMetricArray);

                        if (entityHourlyMetricValueLocationsForThisEntityAndMetric != null && entityHourlyMetricValueLocationsForThisEntityAndMetric.Count > 0)
                        {
                            // Roll through the array of all the time ranges populating the array slots for each hour that has data
                            int indexOfMetricValueLocation = 0;
                            for (int indexOfTimeRange = 0; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
                            {
                                JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[indexOfTimeRange];
                                EntityHourlyMetricValueLocation entityHourlyMetricValueLocation = entityHourlyMetricValueLocationsForThisEntityAndMetric[indexOfMetricValueLocation];

                                if (entityHourlyMetricValueLocation.FromUtc >= jobTimeRange.From &&
                                    entityHourlyMetricValueLocation.ToUtc < jobTimeRange.To)
                                {
                                    // Found metric values for this entity for this metric in this time range
                                    entityHourlyMetricValueLocationsForThisEntityAndMetricArray[indexOfTimeRange] = entityHourlyMetricValueLocation;

                                    indexOfMetricValueLocation++;
                                }
                                if (indexOfMetricValueLocation >= entityHourlyMetricValueLocationsForThisEntityAndMetric.Count)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return entityMetricValuesLocationsDictionary;
        }
    }
}
