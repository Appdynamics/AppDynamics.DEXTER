using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexAPMFlowmaps : JobStepIndexBase
    {
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

                bool reportFolderCleaned = false;

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

                        int numEntitiesTotal = 0;

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                List<APMApplication> applicationList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMApplicationsIndexFilePath(jobTarget), new APMApplicationReportMap());
                                if (applicationList != null && applicationList.Count > 0)
                                {
                                    loggerConsole.Info("Index Flowmap for Application");

                                    FileIOHelper.DeleteFile(FilePathMap.ApplicationFlowmapIndexFilePath(jobTarget));

                                    List<ActivityFlow> activityFlowsList = convertFlowmapApplication(applicationList[0], jobTarget, jobConfiguration.Input.TimeRange);

                                    if (activityFlowsList != null)
                                    {
                                        FileIOHelper.WriteListToCSVFile(activityFlowsList, new ApplicationActivityFlowReportMap(), FilePathMap.ApplicationFlowmapIndexFilePath(jobTarget));
                                    }

                                    if (activityFlowsList != null)
                                    {
                                        loggerConsole.Info("Index Flowmap for Application per Minute");

                                        int numberOfMinutes = Convert.ToInt32((jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).TotalMinutes);

                                        List<ActivityFlow> activityFlowsPerMinuteList = new List<ActivityFlow>(activityFlowsList.Count * numberOfMinutes);

                                        for (int minute = 0; minute < numberOfMinutes; minute++)
                                        {
                                            JobTimeRange thisMinuteJobTimeRange = new JobTimeRange();
                                            thisMinuteJobTimeRange.From = jobConfiguration.Input.TimeRange.From.AddMinutes(minute);
                                            thisMinuteJobTimeRange.To = jobConfiguration.Input.TimeRange.From.AddMinutes(minute + 1);

                                            activityFlowsList = convertFlowmapApplication(applicationList[0], jobTarget, thisMinuteJobTimeRange);

                                            if (activityFlowsList != null)
                                            {
                                                activityFlowsPerMinuteList.AddRange(activityFlowsList);
                                            }
                                        }

                                        FileIOHelper.WriteListToCSVFile(activityFlowsPerMinuteList, new ApplicationActivityFlowReportMap(), FilePathMap.ApplicationFlowmapPerMinuteIndexFilePath(jobTarget));
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, applicationList.Count);

                                    loggerConsole.Info("Completed Application");
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Tiers

                                List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                                if (tiersList != null)
                                {
                                    loggerConsole.Info("Index Flowmap for Tiers ({0} entities)", tiersList.Count);

                                    FileIOHelper.DeleteFile(FilePathMap.TiersFlowmapIndexFilePath(jobTarget));

                                    foreach (APMTier tier in tiersList)
                                    {
                                        List<ActivityFlow> activityFlowsList = convertFlowmapTier(tier, jobTarget, jobConfiguration.Input.TimeRange);

                                        if (activityFlowsList != null)
                                        {
                                            FileIOHelper.WriteListToCSVFile(activityFlowsList, new TierActivityFlowReportMap(), FilePathMap.TiersFlowmapIndexFilePath(jobTarget), true);
                                        }
                                    }

                                    loggerConsole.Info("Completed {0} Tiers", tiersList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, tiersList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                                if (nodesList != null)
                                {
                                    loggerConsole.Info("Index Flowmap for Nodes ({0} entities)", nodesList.Count);

                                    FileIOHelper.DeleteFile(FilePathMap.NodesFlowmapIndexFilePath(jobTarget));

                                    foreach (APMNode node in nodesList)
                                    {
                                        List<ActivityFlow> activityFlowsList = convertFlowmapNode(node, jobTarget, jobConfiguration.Input.TimeRange);

                                        if (activityFlowsList != null)
                                        {
                                            FileIOHelper.WriteListToCSVFile(activityFlowsList, new NodeActivityFlowReportMap(), FilePathMap.NodesFlowmapIndexFilePath(jobTarget), true);
                                        }
                                    }

                                    loggerConsole.Info("Completed {0} Nodes", nodesList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, nodesList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                                if (backendsList != null)
                                {
                                    loggerConsole.Info("Index Flowmap for Backends ({0} entities)", backendsList.Count);

                                    FileIOHelper.DeleteFile(FilePathMap.BackendsFlowmapIndexFilePath(jobTarget));

                                    foreach (APMBackend backend in backendsList)
                                    {
                                        List<ActivityFlow> activityFlowsList = convertFlowmapBackend(backend, jobTarget, jobConfiguration.Input.TimeRange);

                                        if (activityFlowsList != null)
                                        {
                                            FileIOHelper.WriteListToCSVFile(activityFlowsList, new BackendActivityFlowReportMap(), FilePathMap.BackendsFlowmapIndexFilePath(jobTarget), true);
                                        }
                                    }

                                    loggerConsole.Info("Completed {0} Backends", backendsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, backendsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                                if (businessTransactionsList != null)
                                {
                                    loggerConsole.Info("Index Flowmap for Business Transactions ({0} entities)", businessTransactionsList.Count);

                                    FileIOHelper.DeleteFile(FilePathMap.BusinessTransactionsFlowmapIndexFilePath(jobTarget));

                                    foreach (APMBusinessTransaction businessTransaction in businessTransactionsList)
                                    {
                                        List<ActivityFlow> activityFlowsList = convertFlowmapsBusinessTransaction(businessTransaction, jobTarget, jobConfiguration.Input.TimeRange);

                                        FileIOHelper.WriteListToCSVFile(activityFlowsList, new BusinessTransactionActivityFlowReportMap(), FilePathMap.BusinessTransactionsFlowmapIndexFilePath(jobTarget), true);

                                    }

                                    loggerConsole.Info("Completed {0} Business Transactions", businessTransactionsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, businessTransactionsList.Count);
                                }

                                #endregion
                            }
                        );

                        stepTimingTarget.NumEntities = numEntitiesTotal;

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ActivityGridReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ActivityGridReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationsFlowmapReportFilePath(), FilePathMap.ApplicationFlowmapIndexFilePath(jobTarget));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationsFlowmapPerMinuteReportFilePath(), FilePathMap.ApplicationFlowmapPerMinuteIndexFilePath(jobTarget));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.TiersFlowmapReportFilePath(), FilePathMap.TiersFlowmapIndexFilePath(jobTarget));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.NodesFlowmapReportFilePath(), FilePathMap.NodesFlowmapIndexFilePath(jobTarget));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.BackendsFlowmapReportFilePath(), FilePathMap.BackendsFlowmapIndexFilePath(jobTarget));
                        FileIOHelper.AppendTwoCSVFiles(FilePathMap.BusinessTransactionsFlowmapReportFilePath(), FilePathMap.BusinessTransactionsFlowmapIndexFilePath(jobTarget));

                        #endregion

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
            logger.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            loggerConsole.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            if (jobConfiguration.Input.Flowmaps == false)
            {
                loggerConsole.Trace("Skipping index of entity flowmaps");
            }
            return (jobConfiguration.Input.Flowmaps == true);
        }

        private List<ActivityFlow> convertFlowmapApplication(APMApplication application, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            JObject flowmapData = FileIOHelper.LoadJObjectFromFile(FilePathMap.ApplicationFlowmapDataFilePath(jobTarget, jobTimeRange));
            if (flowmapData == null)
            {
                return null;
            }

            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowsList = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowsList = new List<ActivityFlow>(flowmapEntities.Count + flowmapEntityConnections.Count);

                // Process each of the individual Tiers, Backends and Applications as individual icons on the flow map
                foreach (JToken entity in flowmapEntities)
                {
                    ActivityFlow activityFlow = new ActivityFlow();
                    activityFlow.MetricsIDs = new List<long>(3);

                    activityFlow.Controller = application.Controller;
                    activityFlow.ApplicationName = application.ApplicationName;
                    activityFlow.ApplicationID = application.ApplicationID;

                    activityFlow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlow.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlow.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlow.Controller, activityFlow.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlow.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlow.From = jobTimeRange.From.ToLocalTime();
                    activityFlow.To = jobTimeRange.To.ToLocalTime();
                    activityFlow.FromUtc = jobTimeRange.From;
                    activityFlow.ToUtc = jobTimeRange.To;

                    activityFlow.CallDirection = "Total";

                    activityFlow.FromEntityID = getLongValueFromJToken(entity, "idNum");
                    activityFlow.FromName = getStringValueFromJToken(entity, "name");

                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlow.ApplicationID;
                    switch (getStringValueFromJToken(entity, "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlow.FromEntityID;
                            activityFlow.CallType = "Total";
                            activityFlow.FromType = APMTier.ENTITY_TYPE;
                            activityFlow.FromLink = String.Format(DEEPLINK_TIER, activityFlow.Controller, activityFlow.ApplicationID, activityFlow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlow.CallType = "Total";
                            activityFlow.FromType = APMBackend.ENTITY_TYPE;
                            activityFlow.FromLink = String.Format(DEEPLINK_BACKEND, activityFlow.Controller, activityFlow.ApplicationID, activityFlow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlow.CallType = "Total";
                            activityFlow.FromType = APMApplication.ENTITY_TYPE;
                            activityFlow.FromLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlow.Controller, activityFlow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlow.CallType = getStringValueFromJToken(entity, "entityType");
                            activityFlow.FromType = "Unknown";
                            break;
                    }

                    //activityFlowRow.ToName = activityFlowRow.FromName;
                    //activityFlowRow.ToType= activityFlowRow.FromType;
                    activityFlow.ToEntityID = activityFlow.FromEntityID;
                    //activityFlowRow.ToLink = activityFlowRow.FromLink;

                    try { activityFlow.ART = getLongValueFromJToken(entity["stats"]["averageResponseTime"], "metricValue"); } catch { }
                    try { activityFlow.CPM = getLongValueFromJToken(entity["stats"]["callsPerMinute"], "metricValue"); } catch { }
                    try { activityFlow.EPM = getLongValueFromJToken(entity["stats"]["errorsPerMinute"], "metricValue"); } catch { }
                    try { activityFlow.Calls = getLongValueFromJToken(entity["stats"]["numberOfCalls"], "metricValue"); } catch { }
                    try { activityFlow.Errors = getLongValueFromJToken(entity["stats"]["numberOfErrors"], "metricValue"); } catch { }

                    if (activityFlow.ART < 0) { activityFlow.ART = 0; }
                    if (activityFlow.CPM < 0) { activityFlow.ART = 0; }
                    if (activityFlow.EPM < 0) { activityFlow.EPM = 0; }
                    if (activityFlow.Calls < 0) { activityFlow.Calls = 0; }
                    if (activityFlow.Errors < 0) { activityFlow.Errors = 0; }

                    activityFlow.ErrorsPercentage = Math.Round((double)(double)activityFlow.Errors / (double)activityFlow.Calls * 100, 2);
                    if (Double.IsNaN(activityFlow.ErrorsPercentage) == true) activityFlow.ErrorsPercentage = 0;

                    try { activityFlow.MetricsIDs.Add(getLongValueFromJToken(entity["stats"]["averageResponseTime"], "metricId")); } catch { }
                    try { activityFlow.MetricsIDs.Add(getLongValueFromJToken(entity["stats"]["callsPerMinute"], "metricId")); } catch { }
                    try { activityFlow.MetricsIDs.Add(getLongValueFromJToken(entity["stats"]["errorsPerMinute"], "metricId")); } catch { }
                    activityFlow.MetricsIDs.RemoveAll(m => m == -1);

                    if (activityFlow.MetricsIDs != null && activityFlow.MetricsIDs.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder(256);
                        foreach (long metricID in activityFlow.MetricsIDs)
                        {
                            sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                            sb.Append(",");
                        }
                        sb.Remove(sb.Length - 1, 1);
                        activityFlow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlow.Controller, activityFlow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                    }

                    activityFlowsList.Add(activityFlow);
                }

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowTemplate.Controller = application.Controller;
                    activityFlowTemplate.ApplicationName = application.ApplicationName;
                    activityFlowTemplate.ApplicationID = application.ApplicationID;

                    activityFlowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowTemplate.CallDirection = "Exit";

                    activityFlowTemplate.FromEntityID = getLongValueFromJToken(entityConnection["sourceNodeDefinition"], "entityId");
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.FromEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.FromName = getStringValueFromJToken(entity, "name");
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowTemplate.ApplicationID;
                    switch (getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowTemplate.FromEntityID;
                            activityFlowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.FromType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.FromName = getStringValueFromJToken(entityConnection, "sourceNode");
                            activityFlowTemplate.FromType = getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType");
                            break;
                    }

                    activityFlowTemplate.ToEntityID = getLongValueFromJToken(entityConnection["targetNodeDefinition"], "entityId");
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.ToEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.ToName = getStringValueFromJToken(entity, "name");
                    }
                    switch (getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.ToType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.ToName = getStringValueFromJToken(entityConnection, "targetNode");
                            activityFlowTemplate.ToType = getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType");
                            break;
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowTemplate.Clone();

                        activityFlowRow.CallType = getStringValueFromJToken(entityConnectionStat["exitPointCall"], "exitPointType");
                        if (activityFlowRow.CallType.Length == 0)
                        {
                            activityFlowRow.CallType = getStringValueFromJToken(entity, "backendType");
                            if (activityFlowRow.CallType.Length == 0)
                            {
                                if (getBoolValueFromJToken(entityConnectionStat["exitPointCall"], "customExitPoint") == true)
                                {
                                    activityFlowRow.CallType = "Custom";
                                }
                            }
                        }
                        if ((getBoolValueFromJToken(entityConnectionStat, "async")) == true)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        if (isTokenPropertyNull(entityConnectionStat, "averageResponseTime") == false)
                        {
                            activityFlowRow.ART = getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricValue");
                            activityFlowRow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "callsPerMinute") == false)
                        {
                            activityFlowRow.CPM = getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricValue");
                            activityFlowRow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "errorsPerMinute") == false)
                        {
                            activityFlowRow.EPM = getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricValue");
                            activityFlowRow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfCalls") == false) { activityFlowRow.Calls = getLongValueFromJToken(entityConnectionStat["numberOfCalls"], "metricValue"); }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfErrors") == false) { activityFlowRow.Errors = getLongValueFromJToken(entityConnectionStat["numberOfErrors"], "metricValue"); }

                        if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                        if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                        if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                        if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                        activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                        activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(256);
                            foreach (int metricID in activityFlowRow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlowRow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlowRow.Controller, activityFlowRow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowsList.Add(activityFlowRow);
                    }
                }
            }

            // Sort them
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }

        private List<ActivityFlow> convertFlowmapTier(APMTier tier, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            JObject flowmapData = FileIOHelper.LoadJObjectFromFile(FilePathMap.TierFlowmapDataFilePath(jobTarget, jobTimeRange, tier));
            if (flowmapData == null)
            {
                return null;
            }

            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowsList = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowsList = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // For Tiers, not going to process individual entities, but only connecting lines

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowTemplate.Controller = tier.Controller;
                    activityFlowTemplate.ApplicationName = tier.ApplicationName;
                    activityFlowTemplate.ApplicationID = tier.ApplicationID;
                    activityFlowTemplate.TierName = tier.TierName;
                    activityFlowTemplate.TierID = tier.TierID;

                    activityFlowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.TierLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.TierID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowTemplate.FromEntityID = getLongValueFromJToken(entityConnection["sourceNodeDefinition"], "entityId");
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.FromEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.FromName = getStringValueFromJToken(entity, "name");
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowTemplate.ApplicationID;
                    switch (getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowTemplate.FromEntityID;
                            activityFlowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.FromType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.FromName = getStringValueFromJToken(entityConnection, "sourceNode");
                            activityFlowTemplate.FromType = getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType");
                            break;
                    }

                    activityFlowTemplate.ToEntityID = getLongValueFromJToken(entityConnection["targetNodeDefinition"], "entityId");
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.ToEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.ToName = getStringValueFromJToken(entity, "name");
                    }
                    switch (getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.ToType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.ToName = getStringValueFromJToken(entityConnection, "targetNode");
                            activityFlowTemplate.ToType = getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType");
                            break;
                    }

                    if (activityFlowTemplate.FromEntityID == tier.TierID)
                    {
                        activityFlowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlow = activityFlowTemplate.Clone();

                        activityFlow.CallType = getStringValueFromJToken(entityConnectionStat["exitPointCall"], "exitPointType");
                        if (activityFlow.CallType.Length == 0)
                        {
                            activityFlow.CallType = getStringValueFromJToken(entity, "backendType");
                            if (activityFlow.CallType.Length == 0)
                            {
                                if (getBoolValueFromJToken(entityConnectionStat["exitPointCall"], "customExitPoint") == true)
                                {
                                    activityFlow.CallType = "Custom";
                                }
                            }
                        }
                        if ((getBoolValueFromJToken(entityConnectionStat, "async")) == true)
                        {
                            activityFlow.CallType = String.Format("{0} async", activityFlow.CallType);
                        }

                        if (isTokenPropertyNull(entityConnectionStat, "averageResponseTime") == false)
                        {
                            activityFlow.ART = getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "callsPerMinute") == false)
                        {
                            activityFlow.CPM = getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "errorsPerMinute") == false)
                        {
                            activityFlow.EPM = getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfCalls") == false) { activityFlow.Calls = getLongValueFromJToken(entityConnectionStat["numberOfCalls"], "metricValue"); }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfErrors") == false) { activityFlow.Errors = getLongValueFromJToken(entityConnectionStat["numberOfErrors"], "metricValue"); }

                        if (activityFlow.ART < 0) { activityFlow.ART = 0; }
                        if (activityFlow.CPM < 0) { activityFlow.ART = 0; }
                        if (activityFlow.EPM < 0) { activityFlow.EPM = 0; }
                        if (activityFlow.Calls < 0) { activityFlow.Calls = 0; }
                        if (activityFlow.Errors < 0) { activityFlow.Errors = 0; }

                        activityFlow.ErrorsPercentage = Math.Round((double)(double)activityFlow.Errors / (double)activityFlow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlow.ErrorsPercentage) == true) activityFlow.ErrorsPercentage = 0;

                        activityFlow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlow.MetricsIDs != null && activityFlow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(256);
                            foreach (long metricID in activityFlow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlow.Controller, activityFlow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowsList.Add(activityFlow);
                    }
                }
            }

            // Sort them
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }

        private List<ActivityFlow> convertFlowmapNode(APMNode node, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            JObject flowmapData = FileIOHelper.LoadJObjectFromFile(FilePathMap.NodeFlowmapDataFilePath(jobTarget, jobTimeRange, node));
            if (flowmapData == null)
            {
                return null;
            }

            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowsList = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowsList = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // For Nodes, not going to process individual entities, but only connecting lines

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowTemplate.Controller = node.Controller;
                    activityFlowTemplate.ApplicationName = node.ApplicationName;
                    activityFlowTemplate.ApplicationID = node.ApplicationID;
                    activityFlowTemplate.TierName = node.TierName;
                    activityFlowTemplate.TierID = node.TierID;
                    activityFlowTemplate.NodeName = node.NodeName;
                    activityFlowTemplate.NodeID = node.NodeID;

                    activityFlowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.TierLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.TierID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.NodeLink = String.Format(DEEPLINK_NODE, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.NodeID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowTemplate.FromEntityID = getLongValueFromJToken(entityConnection["sourceNodeDefinition"], "entityId");
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.FromEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.FromName = getStringValueFromJToken(entity, "name");
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowTemplate.ApplicationID;
                    switch (getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_NODE:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowTemplate.FromEntityID;
                            activityFlowTemplate.FromType = APMNode.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_NODE, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowTemplate.FromEntityID;
                            activityFlowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.FromType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.FromName = getStringValueFromJToken(entityConnection, "sourceNode");
                            activityFlowTemplate.FromType = getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType");
                            break;
                    }

                    activityFlowTemplate.ToEntityID = getLongValueFromJToken(entityConnection["targetNodeDefinition"], "entityId");
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.ToEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.ToName = getStringValueFromJToken(entity, "name");
                    }
                    switch (getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_NODE:
                            activityFlowTemplate.ToType = APMNode.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_NODE, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.ToType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.ToName = getStringValueFromJToken(entityConnection, "targetNode");
                            activityFlowTemplate.ToType = getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType");
                            break;
                    }

                    // Haven't seen the incoming calls on the flowmap for Nodes. But maybe?
                    if (activityFlowTemplate.FromEntityID == node.NodeID)
                    {
                        activityFlowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlow = activityFlowTemplate.Clone();

                        activityFlow.CallType = getStringValueFromJToken(entityConnectionStat["exitPointCall"], "exitPointType");
                        if (activityFlow.CallType.Length == 0)
                        {
                            activityFlow.CallType = getStringValueFromJToken(entity, "backendType");
                            if (activityFlow.CallType.Length == 0)
                            {
                                if (getBoolValueFromJToken(entityConnectionStat["exitPointCall"], "customExitPoint") == true)
                                {
                                    activityFlow.CallType = "Custom";
                                }
                            }
                        }
                        if ((getBoolValueFromJToken(entityConnectionStat, "async")) == true)
                        {
                            activityFlow.CallType = String.Format("{0} async", activityFlow.CallType);
                        }

                        if (isTokenPropertyNull(entityConnectionStat, "averageResponseTime") == false)
                        {
                            activityFlow.ART = getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "callsPerMinute") == false)
                        {
                            activityFlow.CPM = getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "errorsPerMinute") == false)
                        {
                            activityFlow.EPM = getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfCalls") == false) { activityFlow.Calls = getLongValueFromJToken(entityConnectionStat["numberOfCalls"], "metricValue"); }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfErrors") == false) { activityFlow.Errors = getLongValueFromJToken(entityConnectionStat["numberOfErrors"], "metricValue"); }

                        if (activityFlow.ART < 0) { activityFlow.ART = 0; }
                        if (activityFlow.CPM < 0) { activityFlow.ART = 0; }
                        if (activityFlow.EPM < 0) { activityFlow.EPM = 0; }
                        if (activityFlow.Calls < 0) { activityFlow.Calls = 0; }
                        if (activityFlow.Errors < 0) { activityFlow.Errors = 0; }

                        activityFlow.ErrorsPercentage = Math.Round((double)(double)activityFlow.Errors / (double)activityFlow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlow.ErrorsPercentage) == true) activityFlow.ErrorsPercentage = 0;

                        activityFlow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlow.MetricsIDs != null && activityFlow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(256);
                            foreach (long metricID in activityFlow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlow.Controller, activityFlow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowsList.Add(activityFlow);
                    }
                }
            }

            // Sort them
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }

        private List<ActivityFlow> convertFlowmapBackend(APMBackend backend, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            JObject flowmapData = FileIOHelper.LoadJObjectFromFile(FilePathMap.BackendFlowmapDataFilePath(jobTarget, jobTimeRange, backend));
            if (flowmapData == null)
            {
                return null;
            }

            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowsList = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowsList = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // We don't display grid for Backends. But it is quite similar to Tier view
                // For Backends, not going to process individual entities, but only connecting lines

                // Process each call between Tiers, Tiers and Backends
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowTemplate.Controller = backend.Controller;
                    activityFlowTemplate.ApplicationName = backend.ApplicationName;
                    activityFlowTemplate.ApplicationID = backend.ApplicationID;
                    activityFlowTemplate.BackendName = backend.BackendName;
                    activityFlowTemplate.BackendID = backend.BackendID;

                    activityFlowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.BackendLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.BackendID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowTemplate.FromEntityID = getLongValueFromJToken(entityConnection["sourceNodeDefinition"], "entityId");
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.FromEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.FromName = getStringValueFromJToken(entity, "name");
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowTemplate.ApplicationID;
                    switch (getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowTemplate.FromEntityID;
                            activityFlowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.FromType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.FromName = getStringValueFromJToken(entityConnection, "sourceNode");
                            activityFlowTemplate.FromType = getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType");
                            break;
                    }

                    activityFlowTemplate.ToEntityID = getLongValueFromJToken(entityConnection["targetNodeDefinition"], "entityId");
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.ToEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.ToName = getStringValueFromJToken(entity, "name");
                    }
                    switch (getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.ToType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.ToName = getStringValueFromJToken(entityConnection, "targetNode");
                            activityFlowTemplate.ToType = getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType");
                            break;
                    }

                    if (activityFlowTemplate.FromEntityID == backend.BackendID)
                    {
                        activityFlowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlow = activityFlowTemplate.Clone();

                        activityFlow.CallType = getStringValueFromJToken(entityConnectionStat["exitPointCall"], "exitPointType");
                        if (activityFlow.CallType.Length == 0)
                        {
                            activityFlow.CallType = getStringValueFromJToken(entity, "backendType");
                            if (activityFlow.CallType.Length == 0)
                            {
                                if (getBoolValueFromJToken(entityConnectionStat["exitPointCall"], "customExitPoint") == true)
                                {
                                    activityFlow.CallType = "Custom";
                                }
                            }
                        }
                        if ((getBoolValueFromJToken(entityConnectionStat, "async")) == true)
                        {
                            activityFlow.CallType = String.Format("{0} async", activityFlow.CallType);
                        }

                        if (isTokenPropertyNull(entityConnectionStat, "averageResponseTime") == false)
                        {
                            activityFlow.ART = getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "callsPerMinute") == false)
                        {
                            activityFlow.CPM = getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "errorsPerMinute") == false)
                        {
                            activityFlow.EPM = getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfCalls") == false) { activityFlow.Calls = getLongValueFromJToken(entityConnectionStat["numberOfCalls"], "metricValue"); }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfErrors") == false) { activityFlow.Errors = getLongValueFromJToken(entityConnectionStat["numberOfErrors"], "metricValue"); }

                        if (activityFlow.ART < 0) { activityFlow.ART = 0; }
                        if (activityFlow.CPM < 0) { activityFlow.ART = 0; }
                        if (activityFlow.EPM < 0) { activityFlow.EPM = 0; }
                        if (activityFlow.Calls < 0) { activityFlow.Calls = 0; }
                        if (activityFlow.Errors < 0) { activityFlow.Errors = 0; }

                        activityFlow.ErrorsPercentage = Math.Round((double)(double)activityFlow.Errors / (double)activityFlow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlow.ErrorsPercentage) == true) activityFlow.ErrorsPercentage = 0;

                        activityFlow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlow.MetricsIDs != null && activityFlow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(256);
                            foreach (long metricID in activityFlow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlow.Controller, activityFlow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowsList.Add(activityFlow);
                    }
                }
            }

            // Sort them
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }

        private List<ActivityFlow> convertFlowmapsBusinessTransaction(APMBusinessTransaction businessTransaction, JobTarget jobTarget, JobTimeRange jobTimeRange)
        {
            JObject flowmapData = FileIOHelper.LoadJObjectFromFile(FilePathMap.BusinessTransactionFlowmapDataFilePath(jobTarget, jobTimeRange, businessTransaction));
            if (flowmapData == null)
            {
                return null;
            }

            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
            string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

            List<ActivityFlow> activityFlowsList = null;
            JArray flowmapEntities = (JArray)flowmapData["nodes"];
            JArray flowmapEntityConnections = (JArray)flowmapData["edges"];
            if (flowmapEntities != null && flowmapEntityConnections != null)
            {
                activityFlowsList = new List<ActivityFlow>(flowmapEntityConnections.Count);

                // Controller shows a pretty complex grid view for jumps that continue from other tiers.
                // I couldn't figure out how the JSON is converted into that
                // For Business Transactions, not going to process individual entities, but only connecting lines

                // Assume that the first node is the 
                JObject startTier = (JObject)flowmapEntities.Where(e => (bool)e["startComponent"] == true).FirstOrDefault();

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowTemplate.Controller = businessTransaction.Controller;
                    activityFlowTemplate.ApplicationName = businessTransaction.ApplicationName;
                    activityFlowTemplate.ApplicationID = businessTransaction.ApplicationID;
                    activityFlowTemplate.TierName = businessTransaction.TierName;
                    activityFlowTemplate.TierID = businessTransaction.TierID;
                    activityFlowTemplate.BTName = businessTransaction.BTName;
                    activityFlowTemplate.BTID = businessTransaction.BTID;

                    activityFlowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.TierLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.TierID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowTemplate.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.BTID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowTemplate.FromEntityID = getLongValueFromJToken(entityConnection["sourceNodeDefinition"], "entityId");
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.FromEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.FromName = getStringValueFromJToken(entity, "name");
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowTemplate.ApplicationID;
                    switch (getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowTemplate.FromEntityID;
                            activityFlowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.FromType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.FromLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.FromName = getStringValueFromJToken(entityConnection, "sourceNode");
                            activityFlowTemplate.FromType = getStringValueFromJToken(entityConnection["sourceNodeDefinition"], "entityType");
                            break;
                    }

                    activityFlowTemplate.ToEntityID = getLongValueFromJToken(entityConnection["targetNodeDefinition"], "entityId");
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowTemplate.ToEntityID && e["entityType"].ToString() == getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType")).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowTemplate.ToName = getStringValueFromJToken(entity, "name");
                    }
                    switch (getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType"))
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowTemplate.ToType = APMBackend.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowTemplate.Controller, activityFlowTemplate.ApplicationID, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowTemplate.ToLink = String.Format(DEEPLINK_APM_APPLICATION, activityFlowTemplate.Controller, activityFlowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowTemplate.ToName = getStringValueFromJToken(entityConnection, "targetNode");
                            activityFlowTemplate.ToType = getStringValueFromJToken(entityConnection["targetNodeDefinition"], "entityType");
                            break;
                    }

                    // Haven't seen the incoming calls on the flowmap for Nodes. But maybe?
                    if (startTier != null)
                    {
                        if (activityFlowTemplate.FromEntityID == getLongValueFromJToken(startTier, "idNum"))
                        {
                            activityFlowTemplate.CallDirection = "FirstHop";
                        }
                        else
                        {
                            activityFlowTemplate.CallDirection = "SubsequentHop";
                        }
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlow = activityFlowTemplate.Clone();

                        activityFlow.CallType = getStringValueFromJToken(entityConnectionStat["exitPointCall"], "exitPointType");
                        if (activityFlow.CallType.Length == 0)
                        {
                            activityFlow.CallType = getStringValueFromJToken(entity, "backendType");
                            if (activityFlow.CallType.Length == 0)
                            {
                                if (getBoolValueFromJToken(entityConnectionStat["exitPointCall"], "customExitPoint") == true)
                                {
                                    activityFlow.CallType = "Custom";
                                }
                            }
                        }
                        if ((getBoolValueFromJToken(entityConnectionStat, "async")) == true)
                        {
                            activityFlow.CallType = String.Format("{0} async", activityFlow.CallType);
                        }

                        if (isTokenPropertyNull(entityConnectionStat, "averageResponseTime") == false)
                        {
                            activityFlow.ART = getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["averageResponseTime"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "callsPerMinute") == false)
                        {
                            activityFlow.CPM = getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["callsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "errorsPerMinute") == false)
                        {
                            activityFlow.EPM = getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricValue");
                            activityFlow.MetricsIDs.Add(getLongValueFromJToken(entityConnectionStat["errorsPerMinute"], "metricId"));
                        }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfCalls") == false) { activityFlow.Calls = getLongValueFromJToken(entityConnectionStat["numberOfCalls"], "metricValue"); }
                        if (isTokenPropertyNull(entityConnectionStat, "numberOfErrors") == false) { activityFlow.Errors = getLongValueFromJToken(entityConnectionStat["numberOfErrors"], "metricValue"); }

                        if (activityFlow.ART < 0) { activityFlow.ART = 0; }
                        if (activityFlow.CPM < 0) { activityFlow.ART = 0; }
                        if (activityFlow.EPM < 0) { activityFlow.EPM = 0; }
                        if (activityFlow.Calls < 0) { activityFlow.Calls = 0; }
                        if (activityFlow.Errors < 0) { activityFlow.Errors = 0; }

                        activityFlow.ErrorsPercentage = Math.Round((double)(double)activityFlow.Errors / (double)activityFlow.Calls * 100, 2);
                        if (Double.IsNaN(activityFlow.ErrorsPercentage) == true) activityFlow.ErrorsPercentage = 0;

                        activityFlow.MetricsIDs.RemoveAll(m => m == -1);

                        if (activityFlow.MetricsIDs != null && activityFlow.MetricsIDs.Count > 0)
                        {
                            StringBuilder sb = new StringBuilder(256);
                            foreach (long metricID in activityFlow.MetricsIDs)
                            {
                                sb.Append(String.Format(deepLinkMetricTemplateInMetricBrowser, entityIdForMetricBrowser, metricID));
                                sb.Append(",");
                            }
                            sb.Remove(sb.Length - 1, 1);
                            activityFlow.MetricLink = String.Format(DEEPLINK_METRIC, activityFlow.Controller, activityFlow.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                        }
                        activityFlowsList.Add(activityFlow);
                    }
                }
            }

            // Sort them
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }
    }
}