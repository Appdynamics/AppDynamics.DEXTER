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
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportAPMMetricGraphs : JobStepReportBase
    {
        #region Constants for report contents

        private const string SHEET_CONTROLLERS_LIST = "3.Controllers";
        private const string SHEET_APPLICATIONS_GRAPHS = "4.App.{0}";
        private const string SHEET_APPLICATIONS_SCATTER = "4.App.Calls Scatter";
        private const string SHEET_TIERS_GRAPHS = "5.Tiers.{0}";
        private const string SHEET_TIERS_SCATTER = "5.Tiers.Calls Scatter";
        private const string SHEET_NODES_GRAPHS = "6.Nodes.{0}";
        private const string SHEET_NODES_SCATTER = "6.Nodes.Calls Scatter";
        private const string SHEET_BACKENDS_GRAPHS = "7.Backends.{0}";
        private const string SHEET_BACKENDS_SCATTER = "7.Backends.Calls Scatter";
        private const string SHEET_BUSINESS_TRANSACTIONS_GRAPHS = "8.BTs.{0}";
        private const string SHEET_BUSINESS_TRANSACTIONS_SCATTER = "8.BTs.Calls Scatter";
        private const string SHEET_SERVICE_ENDPOINTS_GRAPHS = "9.SEPs.{0}";
        private const string SHEET_SERVICE_ENDPOINTS_SCATTER = "9.SEPs.Calls Scatter";
        private const string SHEET_ERRORS_GRAPHS = "10.Errors.{0}";
        private const string SHEET_INFORMATION_POINTS_GRAPHS = "11.IPs.{0}";
        private const string SHEET_INFORMATION_POINTS_SCATTER = "11.IPs.Calls Scatter";

        private const string SHEET_ENTITIES_METRICS = "Entity.Metrics";

        private const string SHEET_PIVOT_GRAPH_METRICS_ALL_ENTITIES = "12.G.{0}";

        private const string TABLE_CONTROLLERS = "t_Controllers";

        // Metric data tables from metric.values.csv
        private const string TABLE_METRIC_VALUES = "t_Metric_Values_{0}_{1}";

        private const string TABLE_APPLICATIONS = "t_Applications_{0}";
        private const string TABLE_APPLICATIONS_SCATTER = "t_ApplicationsScatter";
        private const string TABLE_TIERS = "t_Tiers_{0}";
        private const string TABLE_TIERS_SCATTER = "t_TiersScatter";
        private const string TABLE_NODES = "t_Nodes_{0}";
        private const string TABLE_NODES_SCATTER = "t_NodesScatter";
        private const string TABLE_BACKENDS = "t_Backends_{0}";
        private const string TABLE_BACKENDS_SCATTER = "t_BackendsScatter";
        private const string TABLE_BUSINESS_TRANSACTIONS = "t_BusinessTransactions_{0}";
        private const string TABLE_BUSINESS_TRANSACTIONS_SCATTER = "t_BusinessTransactionsScatter";
        private const string TABLE_SERVICE_ENDPOINTS = "t_ServiceEndpoints_{0}";
        private const string TABLE_SERVICE_ENDPOINTS_SCATTER = "t_ServiceEndpointsScatter";
        private const string TABLE_ERRORS = "t_Errors_{0}";
        private const string TABLE_INFORMATION_POINTS = "t_InformationPoints_{0}";
        private const string TABLE_INFORMATION_POINTS_SCATTER = "t_InformationPointsScatter";

        private const string PIVOT_GRAPH_METRICS_ALL_ENTITIES = "p_All_{0}";

        private const string GRAPH_METRICS_ALL_ENTITIES = "g_All_{0}";

        private const int LIST_SHEET_START_TABLE_AT = 4;
        private const int PIVOT_SHEET_START_PIVOT_AT = 7;
        private const int PIVOT_SHEET_CHART_HEIGHT = 14;
        private const int GRAPH_SHEET_START_TABLE_AT = 6;

        // Hourly graph data
        private const string GRAPH_METRICS = "g_Metrics_{0}_{1:yyyyMMddHHss}_{2}";

        // Hourly scatter data
        private const string GRAPH_ARTCPMEPM_SCATTER = "g_ScatterARTCPMEPM_{0}_{1:yyyyMMddHHss}_{2}";

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

                        ParallelOptions parallelOptions = new ParallelOptions();
                        if (programOptions.ProcessSequentially == true)
                        {
                            parallelOptions.MaxDegreeOfParallelism = 1;
                        }

                        Parallel.Invoke(parallelOptions,
                            () =>
                            {
                                #region Application

                                List<APMApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMApplicationsIndexFilePath(jobTarget), new APMApplicationReportMap());
                                if (applicationsList != null && applicationsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Applications ({0} entities, {1} timeranges)", applicationsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMApplication.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMApplication.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, applicationsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMApplication.ENTITY_FOLDER, APMApplication.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, applicationsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMApplication.ENTITY_FOLDER, APMApplication.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, applicationsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMApplication.ENTITY_FOLDER, APMApplication.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(applicationsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, applicationsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Tier

                                List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                                if (tiersList != null && tiersList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Tiers ({0} entities, {1} timeranges)", tiersList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMTier.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMTier.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, tiersList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMTier.ENTITY_FOLDER, APMTier.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, tiersList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMTier.ENTITY_FOLDER, APMTier.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, tiersList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMTier.ENTITY_FOLDER, APMTier.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(tiersList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, tiersList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                                if (nodesList != null && nodesList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Nodes ({0} entities, {1} timeranges)", nodesList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMNode.ENTITY_TYPE && m.Graph.Length > 0).ToList();

                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMNode.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, nodesList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMNode.ENTITY_FOLDER, APMNode.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, nodesList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMNode.ENTITY_FOLDER, APMNode.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, nodesList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMNode.ENTITY_FOLDER, APMNode.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(nodesList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, nodesList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                                if (backendsList != null && backendsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Backends ({0} entities, {1} timeranges)", backendsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMBackend.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMBackend.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, backendsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMBackend.ENTITY_FOLDER, APMBackend.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, backendsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMBackend.ENTITY_FOLDER, APMBackend.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, backendsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMBackend.ENTITY_FOLDER, APMBackend.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(backendsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, backendsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                                if (businessTransactionsList != null && businessTransactionsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Business Transactions ({0} entities, {1} timeranges)", businessTransactionsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMBusinessTransaction.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMBusinessTransaction.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, businessTransactionsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMBusinessTransaction.ENTITY_FOLDER, APMBusinessTransaction.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, businessTransactionsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMBusinessTransaction.ENTITY_FOLDER, APMBusinessTransaction.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, businessTransactionsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMBusinessTransaction.ENTITY_FOLDER, APMBusinessTransaction.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(businessTransactionsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, businessTransactionsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Service Endpoints

                                List<APMServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget), new APMServiceEndpointReportMap());
                                if (serviceEndpointsList != null && serviceEndpointsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Service Endpoints ({0} entities, {1} timeranges)", serviceEndpointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMServiceEndpoint.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMServiceEndpoint.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, serviceEndpointsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMServiceEndpoint.ENTITY_FOLDER, APMServiceEndpoint.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, serviceEndpointsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMServiceEndpoint.ENTITY_FOLDER, APMServiceEndpoint.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, serviceEndpointsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMServiceEndpoint.ENTITY_FOLDER, APMServiceEndpoint.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(serviceEndpointsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, serviceEndpointsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Errors

                                List<APMError> errorsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.APMErrorsIndexFilePath(jobTarget), new APMErrorReportMap());
                                if (errorsList != null && errorsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Errors ({0} entities, {1} timeranges)", errorsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMError.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMError.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, errorsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMError.ENTITY_FOLDER, APMError.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, errorsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMError.ENTITY_FOLDER, APMError.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(errorsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

                                    Interlocked.Add(ref numEntitiesTotal, errorsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Information Points

                                List<APMInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.APMInformationPointsIndexFilePath(jobTarget), new APMInformationPointReportMap());
                                if (informationPointsList != null && informationPointsList.Count > 0)
                                {
                                    loggerConsole.Info("Report Metric Graphs Metrics for Information Points ({0} entities, {1} timeranges)", informationPointsList.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                                    ExcelPackage excelReport = createIndividualEntityMetricGraphsReportTemplate(programOptions, jobConfiguration, jobTarget);
                                    List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == APMInformationPoint.ENTITY_TYPE && m.Graph.Length > 0).ToList();
                                    fillMetricValueTablesForEntityType(excelReport, SHEET_ENTITIES_METRICS, entityMetricExtractMappingListFiltered, jobTarget, APMInformationPoint.ENTITY_FOLDER);

                                    fillMetricGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, informationPointsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMInformationPoint.ENTITY_FOLDER, APMInformationPoint.ENTITY_TYPE);
                                    fillTransactionalScatterPlotsForEntityType(excelReport, entityMetricExtractMappingListFiltered, informationPointsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMInformationPoint.ENTITY_FOLDER, APMInformationPoint.ENTITY_TYPE);
                                    fillPivotGraphsForEntityType(excelReport, entityMetricExtractMappingListFiltered, informationPointsList.OfType<APMEntityBase>().ToList(), jobConfiguration, jobTarget, APMInformationPoint.ENTITY_FOLDER, APMInformationPoint.ENTITY_TYPE);

                                    finalizeAndSaveIndividualEntityMetricReport(excelReport, FilePathMap.APMEntityTypeMetricGraphsExcelReportFilePath(informationPointsList[0], jobTarget, jobConfiguration.Input.TimeRange, true));

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

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.EntityMetrics={0}", programOptions.LicensedReports.EntityMetrics);
            loggerConsole.Trace("LicensedReports.EntityMetrics={0}", programOptions.LicensedReports.EntityMetrics);
            if (programOptions.LicensedReports.EntityMetrics == false)
            {
                loggerConsole.Warn("Not licensed for entity metrics");
                return false;
            }

            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            logger.Trace("Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail={0}", jobConfiguration.Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail);
            loggerConsole.Trace("Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail={0}", jobConfiguration.Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail);
            logger.Trace("Output.EntityMetricGraphs={0}", jobConfiguration.Output.EntityMetricGraphs);
            loggerConsole.Trace("Output.EntityMetricGraphs={0}", jobConfiguration.Output.EntityMetricGraphs);
            if (jobConfiguration.Input.Metrics == false || jobConfiguration.Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail == false || jobConfiguration.Output.EntityMetricGraphs == false)
            {
                loggerConsole.Trace("Skipping report of entity metric graphs");
            }
            return (jobConfiguration.Input.Metrics == true && jobConfiguration.Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail == true && jobConfiguration.Output.EntityMetricGraphs == true);
        }

        private ExcelPackage createIndividualEntityMetricGraphsReportTemplate(ProgramOptions programOptions, JobConfiguration jobConfiguration, JobTarget jobTarget)
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
            ExcelWorksheet sheet = excelMetricGraphs.Workbook.Worksheets.Add(SHEET_PARAMETERS);

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
            sheet = excelMetricGraphs.Workbook.Worksheets.Add(SHEET_TOC);

            #endregion

            #region Controller sheet

            sheet = excelMetricGraphs.Workbook.Worksheets.Add(SHEET_CONTROLLERS_LIST);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

            range = EPPlusCSVHelper.ReadCSVFileIntoExcelRange(FilePathMap.ControllerSummaryIndexFilePath(jobTarget), 0, typeof(ControllerSummary), sheet, LIST_SHEET_START_TABLE_AT, 1);
            if (range != null)
            {
                table = sheet.Tables.Add(range, TABLE_CONTROLLERS);
                table.ShowHeader = true;
                table.TableStyle = TableStyles.Medium2;
                table.ShowFilter = true;
                table.ShowTotal = false;

                sheet.Column(table.Columns["Controller"].Position + 1).Width = 25;
                sheet.Column(table.Columns["Version"].Position + 1).Width = 15;
            }

            #endregion

            #region Metrics sheet

            sheet = excelMetricGraphs.Workbook.Worksheets.Add(SHEET_ENTITIES_METRICS);
            sheet.Cells[1, 1].Value = "Table of Contents";
            sheet.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
            sheet.Cells[1, 2].StyleName = "HyperLinkStyle";
            sheet.View.FreezePanes(LIST_SHEET_START_TABLE_AT + 1, 1);

            #endregion

            return excelMetricGraphs;
        }

        private bool finalizeAndSaveIndividualEntityMetricReport(ExcelPackage excelMetricGraphs, string reportFilePath)
        {
            logger.Info("Finalize Entity Metric Graphs Report File {0}", reportFilePath);

            ExcelWorksheet sheet;

            #region TOC sheet

            // TOC sheet again
            sheet = excelMetricGraphs.Workbook.Worksheets[SHEET_TOC];
            fillTableOfContentsSheet(sheet, excelMetricGraphs);

            #endregion

            #region Save file 

            Console.WriteLine();

            // Report files
            logger.Info("Saving Excel report {0}", reportFilePath);
            loggerConsole.Info("Saving Excel report {0}", reportFilePath);

            FileIOHelper.CreateFolderForFile(reportFilePath);

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

            int fromRow = LIST_SHEET_START_TABLE_AT;
            int fromColumn = 1;

            // Load each of the metrics in the mapping table that apply to this entity type
            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingList)
            {
                string metricsValuesFilePath = FilePathMap.MetricValuesIndexFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName);
                if (File.Exists(metricsValuesFilePath) == true)
                {
                    ExcelRangeBase range = EPPlusCSVHelper.ReadCSVFileIntoExcelRange(metricsValuesFilePath, 0, typeof(MetricValue), sheetMetrics, fromRow, fromColumn);
                    if (range != null)
                    {
                        if (range.Rows == 1)
                        {
                            // If there was no data in the table, adjust the range to have at least one blank line, otherwise Excel thinks table is corrupt
                            range = sheetMetrics.Cells[range.Start.Row, range.Start.Column, range.End.Row + 1, range.End.Column];
                        }
                        ExcelTable table = sheetMetrics.Tables.Add(range, String.Format(TABLE_METRIC_VALUES, entityFolderName, metricExtractMapping.FolderName));
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
            List<APMEntityBase> entityList,
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            string entityFolderName,
            string entityType)
        {
            ExcelWorksheet sheetMetrics = excelReportMetrics.Workbook.Worksheets[SHEET_ENTITIES_METRICS];

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
                    case APMApplication.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_APPLICATIONS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_APPLICATIONS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 2;
                        break;
                    case APMTier.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_TIERS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_TIERS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case APMNode.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_NODES_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_NODES, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMBackend.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_BACKENDS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_BACKENDS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case APMBusinessTransaction.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_BUSINESS_TRANSACTIONS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_BUSINESS_TRANSACTIONS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMServiceEndpoint.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_SERVICE_ENDPOINTS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_SERVICE_ENDPOINTS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMError.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_ERRORS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_ERRORS, metricMappingGroup.Key.Graph));
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMInformationPoint.ENTITY_FOLDER:
                        sheetName = getShortenedNameForExcelSheet(String.Format(SHEET_INFORMATION_POINTS_GRAPHS, metricMappingGroup.Key.Graph));
                        sheetEntityTableName = getShortenedNameForExcelSheet(String.Format(TABLE_INFORMATION_POINTS, metricMappingGroup.Key.Graph));
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
                sheetGraphs.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheetGraphs.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[2, 1].Value = "See Data";
                sheetGraphs.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ENTITIES_METRICS);
                sheetGraphs.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[3, 1].Value = "Controller";
                sheetGraphs.Cells[3, 2].Value = jobTarget.Controller;
                sheetGraphs.Cells[4, 1].Value = "Application";
                sheetGraphs.Cells[4, 2].Value = jobTarget.Application;
                sheetGraphs.Cells[5, 1].Value = "Type";
                sheetGraphs.Cells[5, 2].Value = entityType;

                sheetGraphs.View.FreezePanes(GRAPH_SHEET_START_TABLE_AT + 1, columnsBeforeFirstHourRange + 1);

                // Move before all the Metrics sheets
                excelReportMetrics.Workbook.Worksheets.MoveBefore(sheetName, SHEET_ENTITIES_METRICS);

                #endregion

                #region Add outlines and time range labels for each of the hourly ranges

                int indexOfTimeRangeToStartWith = 0;
                if (jobConfiguration.Input.HourlyTimeRanges.Count > 8)
                {
                    indexOfTimeRangeToStartWith = jobConfiguration.Input.HourlyTimeRanges.Count - 8;
                }

                for (int indexOfTimeRange = indexOfTimeRangeToStartWith; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
                {
                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[indexOfTimeRange];

                    int columnIndexTimeRangeStart = columnsBeforeFirstHourRange + 1 + (indexOfTimeRange - indexOfTimeRangeToStartWith) * (columnsBetweenHourRanges + numCellsPerHourRange);
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
                int entityTableHeaderRow = GRAPH_SHEET_START_TABLE_AT;
                switch (entityFolderName)
                {
                    case APMApplication.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "ApplicationName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "HasActivity";
                        break;
                    case APMTier.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case APMNode.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "NodeName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMBackend.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "BackendName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BackendType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case APMBusinessTransaction.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BTName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "BTType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMServiceEndpoint.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "SEPName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "SEPType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMError.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "ErrorName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "ErrorType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMInformationPoint.ENTITY_FOLDER:
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
                    APMEntityBase entity = entityList[indexOfEntity];

                    string entityNameForExcelTable = getShortenedEntityNameForExcelTable(entity.EntityName, entity.EntityID);

                    bool entityHasActivity = false;

                    // Output graphs for every hour range one at a time
                    for (int indexOfTimeRange = indexOfTimeRangeToStartWith; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
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
                            int columnIndexOfCurrentRangeBegin = columnsBeforeFirstHourRange + 1 + (indexOfTimeRange - indexOfTimeRangeToStartWith) * (columnsBetweenHourRanges + numCellsPerHourRange);
                            int columnIndexOfValueOfCurrentMetric = columnIndexOfCurrentRangeBegin + indexOfMetricMapping * 2;
                            sheetGraphs.Cells[entityTableHeaderRow - 1, columnIndexOfValueOfCurrentMetric].Value = metricExtractMapping.MetricName;
                            sheetGraphs.Cells[entityTableHeaderRow - 1, columnIndexOfValueOfCurrentMetric].StyleName = "MetricNameStyle";
                            sheetGraphs.Cells[entityTableHeaderRow, columnIndexOfValueOfCurrentMetric].Value = "Sum";
                            sheetGraphs.Cells[entityTableHeaderRow, columnIndexOfValueOfCurrentMetric + 1].Value = "Avg";

                            // Output legend at the top for each of the metrics that we will display
                            sheetGraphs.Cells[entityTableHeaderRow - 2, columnIndexOfValueOfCurrentMetric].Value = LEGEND_THICK_LINE;
                            sheetGraphs.Cells[entityTableHeaderRow - 2, columnIndexOfValueOfCurrentMetric].Style.Font.Color.SetColor(getColorFromHexString(metricExtractMapping.LineColor));

                            #endregion

                            if (entityMetricValuesLocationsDictionary.ContainsKey(entity.EntityID) == true)
                            {
                                int numberOfMetricsOutput = 0;
                                // This will support outputting more than one metric
                                foreach (string entityMetricValuesLocationForThisMetricKey in entityMetricValuesLocationsDictionary[entity.EntityID].Keys)
                                {
                                    if (entityMetricValuesLocationForThisMetricKey.EndsWith(metricExtractMapping.MetricName) == true)
                                    {
                                        EntityHourlyMetricValueLocation entityMetricValuesLocationForThisMetric = null;

                                        try { entityMetricValuesLocationForThisMetric = entityMetricValuesLocationsDictionary[entity.EntityID][entityMetricValuesLocationForThisMetricKey][indexOfTimeRange]; } catch { }

                                        if (entityMetricValuesLocationForThisMetric != null)
                                        {
                                            // We must have some activity here
                                            entityHasActivity = true;

                                            // Output the numeric values
                                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(TABLE_METRIC_VALUES, entityFolderName, metricExtractMapping.FolderName)];

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
                                                    chartPrimaryAxis = sheetGraphs.Drawings.AddChart(String.Format(GRAPH_METRICS, entityType, jobTimeRange.From, entityNameForExcelTable), eChartType.XYScatterLinesNoMarkers);
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
                                                        series.Header = entityMetricValuesLocationForThisMetricKey;
                                                        //series.Header = metricExtractMapping.MetricName;
                                                        ((ExcelScatterChartSerie)series).LineColor = getColorFromHexString(metricExtractMapping.LineColor);

                                                        numberOfMetricsOutput++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    // Stop at the reasonable amount of metrics or Excel barfs
                                    if (numberOfMetricsOutput >= 10)
                                    {
                                        logger.Warn("There are >10 metrics for {0} with {1}, skipping the rest for display in the graph", entity, entityMetricValuesLocationForThisMetricKey);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    #region Output entity detail in table

                    // Output the first row
                    switch (entityFolderName)
                    {
                        case APMApplication.ENTITY_FOLDER:
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entity.ApplicationName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityHasActivity;
                            break;
                        case APMTier.ENTITY_FOLDER:
                            APMTier entityTier = (APMTier)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityTier.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityTier.TierType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case APMNode.ENTITY_FOLDER:
                            APMNode entityNode = (APMNode)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityNode.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityNode.NodeName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityNode.AgentType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMBackend.ENTITY_FOLDER:
                            APMBackend entityBackend = (APMBackend)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBackend.BackendName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBackend.BackendType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case APMBusinessTransaction.ENTITY_FOLDER:
                            APMBusinessTransaction entityBusinessTransaction = (APMBusinessTransaction)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBusinessTransaction.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBusinessTransaction.BTName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityBusinessTransaction.BTType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMServiceEndpoint.ENTITY_FOLDER:
                            APMServiceEndpoint entityServiceEndpoint = (APMServiceEndpoint)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityServiceEndpoint.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityServiceEndpoint.SEPName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityServiceEndpoint.SEPType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMError.ENTITY_FOLDER:
                            APMError entityError = (APMError)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityError.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityError.ErrorName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityError.ErrorType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMInformationPoint.ENTITY_FOLDER:
                            APMInformationPoint entityInformationPoint = (APMInformationPoint)entity;
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
            List<APMEntityBase> entityList,
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            string entityFolderName,
            string entityType)
        {
            ExcelWorksheet sheetMetrics = excelReportMetrics.Workbook.Worksheets[SHEET_ENTITIES_METRICS];

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
                    case APMApplication.ENTITY_FOLDER:
                        sheetName = SHEET_APPLICATIONS_SCATTER;
                        sheetEntityTableName = TABLE_APPLICATIONS_SCATTER;
                        columnsBeforeFirstHourRange = 2;
                        break;
                    case APMTier.ENTITY_FOLDER:
                        sheetName = SHEET_TIERS_SCATTER;
                        sheetEntityTableName = TABLE_TIERS_SCATTER;
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case APMNode.ENTITY_FOLDER:
                        sheetName = SHEET_NODES_SCATTER;
                        sheetEntityTableName = TABLE_NODES_SCATTER;
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMBackend.ENTITY_FOLDER:
                        sheetName = SHEET_BACKENDS_SCATTER;
                        sheetEntityTableName = TABLE_BACKENDS_SCATTER;
                        columnsBeforeFirstHourRange = 3;
                        break;
                    case APMBusinessTransaction.ENTITY_FOLDER:
                        sheetName = SHEET_BUSINESS_TRANSACTIONS_SCATTER;
                        sheetEntityTableName = TABLE_BUSINESS_TRANSACTIONS_SCATTER;
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMServiceEndpoint.ENTITY_FOLDER:
                        sheetName = SHEET_SERVICE_ENDPOINTS_SCATTER;
                        sheetEntityTableName = TABLE_SERVICE_ENDPOINTS_SCATTER;
                        columnsBeforeFirstHourRange = 4;
                        break;
                    case APMError.ENTITY_FOLDER:
                        return;
                    case APMInformationPoint.ENTITY_FOLDER:
                        sheetName = SHEET_INFORMATION_POINTS_SCATTER;
                        sheetEntityTableName = TABLE_INFORMATION_POINTS_SCATTER;
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
                sheetGraphs.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheetGraphs.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[2, 1].Value = "See Data";
                sheetGraphs.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ENTITIES_METRICS);
                sheetGraphs.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[3, 1].Value = "Controller";
                sheetGraphs.Cells[3, 2].Value = jobTarget.Controller;
                sheetGraphs.Cells[4, 1].Value = "Application";
                sheetGraphs.Cells[4, 2].Value = jobTarget.Application;
                sheetGraphs.Cells[5, 1].Value = "Type";
                sheetGraphs.Cells[5, 2].Value = entityType;

                sheetGraphs.View.FreezePanes(GRAPH_SHEET_START_TABLE_AT + 1, columnsBeforeFirstHourRange + 1);

                // Move before all the Graphs sheets
                excelReportMetrics.Workbook.Worksheets.MoveAfter(sheetName, SHEET_CONTROLLERS_LIST);

                #endregion

                #region Add outlines and time range labels for each of the hourly ranges

                int indexOfTimeRangeToStartWith = 0;
                if (jobConfiguration.Input.HourlyTimeRanges.Count > 8)
                {
                    indexOfTimeRangeToStartWith = jobConfiguration.Input.HourlyTimeRanges.Count - 8;
                }

                for (int indexOfTimeRange = indexOfTimeRangeToStartWith; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
                {
                    JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[indexOfTimeRange];

                    int columnIndexTimeRangeStart = columnsBeforeFirstHourRange + 1 + (indexOfTimeRange - indexOfTimeRangeToStartWith) * (columnsBetweenHourRanges + numCellsPerHourRange);
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
                int entityTableHeaderRow = GRAPH_SHEET_START_TABLE_AT;
                switch (entityFolderName)
                {
                    case APMApplication.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "ApplicationName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "HasActivity";
                        break;
                    case APMTier.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case APMNode.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "NodeName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "AgentType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMBackend.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "BackendName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BackendType";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "HasActivity";
                        break;
                    case APMBusinessTransaction.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "BTName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "BTType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMServiceEndpoint.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "SEPName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "SEPType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMError.ENTITY_FOLDER:
                        sheetGraphs.Cells[entityTableHeaderRow, 1].Value = "TierName";
                        sheetGraphs.Cells[entityTableHeaderRow, 2].Value = "ErrorName";
                        sheetGraphs.Cells[entityTableHeaderRow, 3].Value = "ErrorType";
                        sheetGraphs.Cells[entityTableHeaderRow, 4].Value = "HasActivity";
                        break;
                    case APMInformationPoint.ENTITY_FOLDER:
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
                    APMEntityBase entity = entityList[indexOfEntity];

                    string entityNameForExcelTable = getShortenedEntityNameForExcelTable(entity.EntityName, entity.EntityID);

                    bool entityHasActivity = false;

                    // Output graphs for every hour range one at a time
                    for (int indexOfTimeRange = indexOfTimeRangeToStartWith; indexOfTimeRange < jobConfiguration.Input.HourlyTimeRanges.Count; indexOfTimeRange++)
                    {
                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[indexOfTimeRange];

                        #region Headers and legend for each range

                        int columnIndexOfCurrentRangeBegin = columnsBeforeFirstHourRange + 1 + (indexOfTimeRange - indexOfTimeRangeToStartWith) * (columnsBetweenHourRanges + numCellsPerHourRange);

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
                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(TABLE_METRIC_VALUES, entityFolderName, memART.FolderName)];
                            if (tableMetrics != null)
                            {
                                rangeARTValues = getSingleColumnRangeFromTable(tableMetrics, "Value", entityMetricValuesLocationsForART.RowStart, entityMetricValuesLocationsForART.RowEnd);
                            }
                        }

                        if (entityMetricValuesLocationsForCPM != null)
                        {
                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(TABLE_METRIC_VALUES, entityFolderName, memCPM.FolderName)];
                            if (tableMetrics != null)
                            {
                                rangeCPMValues = getSingleColumnRangeFromTable(tableMetrics, "Value", entityMetricValuesLocationsForCPM.RowStart, entityMetricValuesLocationsForCPM.RowEnd);
                            }
                        }

                        if (entityMetricValuesLocationsForEPM != null)
                        {
                            ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(TABLE_METRIC_VALUES, entityFolderName, memEPM.FolderName)];
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

                            ExcelChart chart = sheetGraphs.Drawings.AddChart(String.Format(GRAPH_ARTCPMEPM_SCATTER, entityType, jobTimeRange.From, entityNameForExcelTable), eChartType.XYScatter);
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
                                ExcelChartSerie series = chart.Series.Add(rangeARTValues, rangeCPMValues);
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
                                ExcelChartSerie series = chart.Series.Add(rangeARTValues, rangeEPMValues);
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
                        case APMApplication.ENTITY_FOLDER:
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entity.ApplicationName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityHasActivity;
                            break;
                        case APMTier.ENTITY_FOLDER:
                            APMTier entityTier = (APMTier)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityTier.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityTier.TierType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case APMNode.ENTITY_FOLDER:
                            APMNode entityNode = (APMNode)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityNode.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityNode.NodeName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityNode.AgentType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMBackend.ENTITY_FOLDER:
                            APMBackend entityBackend = (APMBackend)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBackend.BackendName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBackend.BackendType;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityHasActivity;
                            break;
                        case APMBusinessTransaction.ENTITY_FOLDER:
                            APMBusinessTransaction entityBusinessTransaction = (APMBusinessTransaction)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityBusinessTransaction.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityBusinessTransaction.BTName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityBusinessTransaction.BTType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMServiceEndpoint.ENTITY_FOLDER:
                            APMServiceEndpoint entityServiceEndpoint = (APMServiceEndpoint)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityServiceEndpoint.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityServiceEndpoint.SEPName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityServiceEndpoint.SEPType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMError.ENTITY_FOLDER:
                            APMError entityError = (APMError)entity;
                            sheetGraphs.Cells[currentMaxRow, 1].Value = entityError.TierName;
                            sheetGraphs.Cells[currentMaxRow, 2].Value = entityError.ErrorName;
                            sheetGraphs.Cells[currentMaxRow, 3].Value = entityError.ErrorType;
                            sheetGraphs.Cells[currentMaxRow, 4].Value = entityHasActivity;
                            break;
                        case APMInformationPoint.ENTITY_FOLDER:
                            APMInformationPoint entityInformationPoint = (APMInformationPoint)entity;
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

        private void fillPivotGraphsForEntityType(
            ExcelPackage excelReportMetrics,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            List<APMEntityBase> entityList,
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            string entityFolderName,
            string entityType)
        {
            ExcelWorksheet sheetMetrics = excelReportMetrics.Workbook.Worksheets[SHEET_ENTITIES_METRICS];

            // Load each of the metrics in the mapping table that apply to this entity type
            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingList)
            {
                string worksheetName = getExcelTableOrSheetSafeString(String.Format(SHEET_PIVOT_GRAPH_METRICS_ALL_ENTITIES, metricExtractMapping.FolderName));
                ExcelWorksheet sheetGraphs = excelReportMetrics.Workbook.Worksheets.Add(worksheetName);
                sheetGraphs.Cells[1, 1].Value = "Table of Contents";
                sheetGraphs.Cells[1, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_TOC);
                sheetGraphs.Cells[1, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.Cells[2, 1].Value = "See Data";
                sheetGraphs.Cells[2, 2].Formula = String.Format(@"=HYPERLINK(""#'{0}'!A1"", ""<Go>"")", SHEET_ENTITIES_METRICS);
                sheetGraphs.Cells[2, 2].StyleName = "HyperLinkStyle";
                sheetGraphs.View.FreezePanes(PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 4, 1);
                excelReportMetrics.Workbook.Worksheets.MoveBefore(sheetGraphs.Name, SHEET_ENTITIES_METRICS);

                sheetGraphs.Cells[1, 3].Value = metricExtractMapping.MetricName;
                sheetGraphs.Cells[2, 3].Value = metricExtractMapping.MetricPath;

                ExcelTable tableMetrics = sheetMetrics.Tables[String.Format(TABLE_METRIC_VALUES, entityFolderName, metricExtractMapping.FolderName)];
                if (tableMetrics != null)
                {
                    ExcelRangeBase rangeTableMetrics = (ExcelRangeBase)tableMetrics.Address;
                    ExcelPivotTable pivot= sheetGraphs.PivotTables.Add(sheetGraphs.Cells[PIVOT_SHEET_START_PIVOT_AT + PIVOT_SHEET_CHART_HEIGHT + 1, 1], rangeTableMetrics, String.Format(PIVOT_GRAPH_METRICS_ALL_ENTITIES, metricExtractMapping.FolderName));
                    setDefaultPivotTableSettings(pivot);
                    ExcelPivotTableField fieldR = pivot.RowFields.Add(pivot.Fields["EventTimeStamp"]);
                    fieldR.AddDateGrouping(eDateGroupBy.Days | eDateGroupBy.Hours | eDateGroupBy.Minutes);
                    fieldR.Compact = false;
                    fieldR.Outline = false;
                    addColumnFieldToPivot(pivot, "EntityName", eSortType.Ascending);
                    addColumnFieldToPivot(pivot, "MetricName", eSortType.Ascending);
                    addDataFieldToPivot(pivot, "Value", DataFieldFunctions.Average);

                    ExcelChart chart = sheetGraphs.Drawings.AddChart(String.Format(GRAPH_METRICS_ALL_ENTITIES, metricExtractMapping.FolderName), eChartType.Line, pivot);
                    chart.SetPosition(2, 0, 0, 0);
                    chart.SetSize(1200, 350);

                    sheetGraphs.Column(1).Width = 20;
                    sheetGraphs.Column(2).Width = 20;
                    sheetGraphs.Column(3).Width = 20;
                }
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

                                if (entityHourlyMetricValueLocation.FromUtc.ToUniversalTime() >= jobTimeRange.From &&
                                    entityHourlyMetricValueLocation.ToUtc.ToUniversalTime() <= jobTimeRange.To)
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
