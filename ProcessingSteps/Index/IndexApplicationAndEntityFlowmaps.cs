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
    public class IndexApplicationAndEntityFlowmaps : JobStepIndexBase
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

                        #region Target step variables

                        int numEntitiesTotal = 0;

                        #endregion

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                List<APMApplication> applicationList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.ApplicationIndexFilePath(jobTarget), new APMApplicationReportMap());
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

                                List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.TiersIndexFilePath(jobTarget), new APMTierReportMap());
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

                                List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.NodesIndexFilePath(jobTarget), new APMNodeReportMap());
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

                                List<Backend> backendsList = FileIOHelper.ReadListFromCSVFile<Backend>(FilePathMap.BackendsIndexFilePath(jobTarget), new BackendReportMap());
                                if (backendsList != null)
                                {
                                    loggerConsole.Info("Index Flowmap for Backends ({0} entities)", backendsList.Count);

                                    FileIOHelper.DeleteFile(FilePathMap.BackendsFlowmapIndexFilePath(jobTarget));

                                    foreach (Backend backend in backendsList)
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

                                List<BusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<BusinessTransaction>(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget), new BusinessTransactionReportMap());
                                if (businessTransactionsList != null)
                                {
                                    loggerConsole.Info("Index Flowmap for Business Transactions ({0} entities)", businessTransactionsList.Count);

                                    FileIOHelper.DeleteFile(FilePathMap.BusinessTransactionsFlowmapIndexFilePath(jobTarget));

                                    foreach (BusinessTransaction businessTransaction in businessTransactionsList)
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

                        // Append all the individual application files into one
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
                    ActivityFlow activityFlowRow = new ActivityFlow();
                    activityFlowRow.MetricsIDs = new List<long>(3);

                    activityFlowRow.Controller = application.Controller;
                    activityFlowRow.ApplicationName = application.ApplicationName;
                    activityFlowRow.ApplicationID = application.ApplicationID;

                    activityFlowRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRow.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRow.Controller, activityFlowRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRow.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowRow.From = jobTimeRange.From.ToLocalTime();
                    activityFlowRow.To = jobTimeRange.To.ToLocalTime();
                    activityFlowRow.FromUtc = jobTimeRange.From;
                    activityFlowRow.ToUtc = jobTimeRange.To;

                    activityFlowRow.CallDirection = "Total";

                    activityFlowRow.FromEntityID = (long)entity["idNum"];
                    activityFlowRow.FromName = entity["name"].ToString();

                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRow.ApplicationID;
                    switch (entity["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRow.FromEntityID;
                            activityFlowRow.CallType = "Total";
                            activityFlowRow.FromType = APMTier.ENTITY_TYPE;
                            activityFlowRow.FromLink = String.Format(DEEPLINK_TIER, activityFlowRow.Controller, activityFlowRow.ApplicationID, activityFlowRow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRow.CallType = "Total";
                            activityFlowRow.FromType = Backend.ENTITY_TYPE;
                            activityFlowRow.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRow.Controller, activityFlowRow.ApplicationID, activityFlowRow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRow.CallType = "Total";
                            activityFlowRow.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowRow.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRow.Controller, activityFlowRow.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRow.CallType = entity["entityType"].ToString();
                            activityFlowRow.FromType = "Unknown";
                            break;
                    }

                    //activityFlowRow.ToName = activityFlowRow.FromName;
                    //activityFlowRow.ToType= activityFlowRow.FromType;
                    activityFlowRow.ToEntityID = activityFlowRow.FromEntityID;
                    //activityFlowRow.ToLink = activityFlowRow.FromLink;

                    activityFlowRow.ART = (long)entity["stats"]["averageResponseTime"]["metricValue"];
                    activityFlowRow.CPM = (long)entity["stats"]["callsPerMinute"]["metricValue"];
                    activityFlowRow.EPM = (long)entity["stats"]["errorsPerMinute"]["metricValue"];
                    activityFlowRow.Calls = (long)entity["stats"]["numberOfCalls"]["metricValue"];
                    activityFlowRow.Errors = (long)entity["stats"]["numberOfErrors"]["metricValue"];

                    if (activityFlowRow.ART < 0) { activityFlowRow.ART = 0; }
                    if (activityFlowRow.CPM < 0) { activityFlowRow.ART = 0; }
                    if (activityFlowRow.EPM < 0) { activityFlowRow.EPM = 0; }
                    if (activityFlowRow.Calls < 0) { activityFlowRow.Calls = 0; }
                    if (activityFlowRow.Errors < 0) { activityFlowRow.Errors = 0; }

                    activityFlowRow.ErrorsPercentage = Math.Round((double)(double)activityFlowRow.Errors / (double)activityFlowRow.Calls * 100, 2);
                    if (Double.IsNaN(activityFlowRow.ErrorsPercentage) == true) activityFlowRow.ErrorsPercentage = 0;

                    activityFlowRow.MetricsIDs.Add((int)entity["stats"]["averageResponseTime"]["metricId"]);
                    activityFlowRow.MetricsIDs.Add((int)entity["stats"]["callsPerMinute"]["metricId"]);
                    activityFlowRow.MetricsIDs.Add((int)entity["stats"]["errorsPerMinute"]["metricId"]);
                    activityFlowRow.MetricsIDs.RemoveAll(m => m == -1);

                    if (activityFlowRow.MetricsIDs != null && activityFlowRow.MetricsIDs.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder(128);
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

                // Process each call between Tiers, Tiers and Backends, and Tiers and Applications
                foreach (JToken entityConnection in flowmapEntityConnections)
                {
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowRowTemplate.Controller = application.Controller;
                    activityFlowRowTemplate.ApplicationName = application.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = application.ApplicationID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowRowTemplate.CallDirection = "Exit";

                    activityFlowRowTemplate.FromEntityID = (long)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.FromType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (long)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowRowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.ToType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowRowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (activityFlowRow.CallType.Length == 0)
                        {
                            activityFlowRow.CallType = entity["backendType"].ToString();
                            if (activityFlowRow.CallType.Length == 0)
                            {
                                if ((bool)entityConnectionStat["exitPointCall"]["customExitPoint"] == true)
                                {
                                    activityFlowRow.CallType = "Custom";
                                }
                            }
                        }
                        if (((bool)entityConnectionStat["async"]) == true)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        if (entityConnectionStat["averageResponseTime"].HasValues == true)
                        {
                            activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["averageResponseTime"]["metricId"]);
                        }
                        if (entityConnectionStat["callsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["callsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["errorsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["numberOfCalls"].HasValues == true) { activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"]; }
                        if (entityConnectionStat["numberOfErrors"].HasValues == true) { activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"]; }

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
                            StringBuilder sb = new StringBuilder(128);
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
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowRowTemplate.Controller = tier.Controller;
                    activityFlowRowTemplate.ApplicationName = tier.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = tier.ApplicationID;
                    activityFlowRowTemplate.TierName = tier.TierName;
                    activityFlowRowTemplate.TierID = tier.TierID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.TierLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.TierID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (long)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.FromType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (long)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowRowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.ToType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowRowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    if (activityFlowRowTemplate.FromEntityID == tier.TierID)
                    {
                        activityFlowRowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowRowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (activityFlowRow.CallType.Length == 0)
                        {
                            activityFlowRow.CallType = entity["backendType"].ToString();
                            if (activityFlowRow.CallType.Length == 0)
                            {
                                if ((bool)entityConnectionStat["exitPointCall"]["customExitPoint"] == true)
                                {
                                    activityFlowRow.CallType = "Custom";
                                }
                            }
                        }
                        if (((bool)entityConnectionStat["async"]) == true)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        if (entityConnectionStat["averageResponseTime"].HasValues == true)
                        {
                            activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["averageResponseTime"]["metricId"]);
                        }
                        if (entityConnectionStat["callsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["callsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["errorsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["numberOfCalls"].HasValues == true) { activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"]; }
                        if (entityConnectionStat["numberOfErrors"].HasValues == true) { activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"]; }

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
                            StringBuilder sb = new StringBuilder(128);
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
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowRowTemplate.Controller = node.Controller;
                    activityFlowRowTemplate.ApplicationName = node.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = node.ApplicationID;
                    activityFlowRowTemplate.TierName = node.TierName;
                    activityFlowRowTemplate.TierID = node.TierID;
                    activityFlowRowTemplate.NodeName = node.NodeName;
                    activityFlowRowTemplate.NodeID = node.NodeID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.TierLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.TierID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.NodeLink = String.Format(DEEPLINK_NODE, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.NodeID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (long)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_NODE:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_NODE_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = APMNode.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_NODE, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.FromType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (long)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case "APPLICATION_COMPONENT_NODE":
                            activityFlowRowTemplate.ToType = APMNode.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_NODE, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowRowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.ToType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowRowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    // Haven't seen the incoming calls on the flowmap for Nodes. But maybe?
                    if (activityFlowRowTemplate.FromEntityID == node.NodeID)
                    {
                        activityFlowRowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowRowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (activityFlowRow.CallType.Length == 0)
                        {
                            activityFlowRow.CallType = entity["backendType"].ToString();
                            if (activityFlowRow.CallType.Length == 0)
                            {
                                if ((bool)entityConnectionStat["exitPointCall"]["customExitPoint"] == true)
                                {
                                    activityFlowRow.CallType = "Custom";
                                }
                            }
                        }
                        if (((bool)entityConnectionStat["async"]) == true)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        if (entityConnectionStat["averageResponseTime"].HasValues == true)
                        {
                            activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["averageResponseTime"]["metricId"]);
                        }
                        if (entityConnectionStat["callsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["callsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["errorsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["numberOfCalls"].HasValues == true) { activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"]; }
                        if (entityConnectionStat["numberOfErrors"].HasValues == true) { activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"]; }

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
                            StringBuilder sb = new StringBuilder(128);
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
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }

        private List<ActivityFlow> convertFlowmapBackend(Backend backend, JobTarget jobTarget, JobTimeRange jobTimeRange)
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
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowRowTemplate.Controller = backend.Controller;
                    activityFlowRowTemplate.ApplicationName = backend.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = backend.ApplicationID;
                    activityFlowRowTemplate.BackendName = backend.BackendName;
                    activityFlowRowTemplate.BackendID = backend.BackendID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.BackendLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.BackendID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (long)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.FromType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (long)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowRowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.ToType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowRowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    if (activityFlowRowTemplate.FromEntityID == backend.BackendID)
                    {
                        activityFlowRowTemplate.CallDirection = "Outgoing";
                    }
                    else
                    {
                        activityFlowRowTemplate.CallDirection = "Incoming";
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (activityFlowRow.CallType.Length == 0)
                        {
                            activityFlowRow.CallType = entity["backendType"].ToString();
                            if (activityFlowRow.CallType.Length == 0)
                            {
                                if ((bool)entityConnectionStat["exitPointCall"]["customExitPoint"] == true)
                                {
                                    activityFlowRow.CallType = "Custom";
                                }
                            }
                        }
                        if (((bool)entityConnectionStat["async"]) == true)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        if (entityConnectionStat["averageResponseTime"].HasValues == true)
                        {
                            activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["averageResponseTime"]["metricId"]);
                        }
                        if (entityConnectionStat["callsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["callsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["errorsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["numberOfCalls"].HasValues == true) { activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"]; }
                        if (entityConnectionStat["numberOfErrors"].HasValues == true) { activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"]; }

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
                            StringBuilder sb = new StringBuilder(128);
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
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }

        private List<ActivityFlow> convertFlowmapsBusinessTransaction(BusinessTransaction businessTransaction, JobTarget jobTarget, JobTimeRange jobTimeRange)
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
                    ActivityFlow activityFlowRowTemplate = new ActivityFlow();

                    // Prepare the row
                    activityFlowRowTemplate.MetricsIDs = new List<long>(3);

                    activityFlowRowTemplate.Controller = businessTransaction.Controller;
                    activityFlowRowTemplate.ApplicationName = businessTransaction.ApplicationName;
                    activityFlowRowTemplate.ApplicationID = businessTransaction.ApplicationID;
                    activityFlowRowTemplate.TierName = businessTransaction.TierName;
                    activityFlowRowTemplate.TierID = businessTransaction.TierID;
                    activityFlowRowTemplate.BTName = businessTransaction.BTName;
                    activityFlowRowTemplate.BTID = businessTransaction.BTID;

                    activityFlowRowTemplate.ControllerLink = String.Format(DEEPLINK_CONTROLLER, activityFlowRowTemplate.Controller, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.ApplicationLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.TierLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.TierID, DEEPLINK_THIS_TIMERANGE);
                    activityFlowRowTemplate.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.BTID, DEEPLINK_THIS_TIMERANGE);

                    activityFlowRowTemplate.Duration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;
                    activityFlowRowTemplate.From = jobTimeRange.From.ToLocalTime();
                    activityFlowRowTemplate.To = jobTimeRange.To.ToLocalTime();
                    activityFlowRowTemplate.FromUtc = jobTimeRange.From;
                    activityFlowRowTemplate.ToUtc = jobTimeRange.To;

                    activityFlowRowTemplate.FromEntityID = (long)entityConnection["sourceNodeDefinition"]["entityId"];
                    JObject entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.FromEntityID && e["entityType"].ToString() == entityConnection["sourceNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.FromName = entity["name"].ToString();
                    }
                    string deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID;
                    long entityIdForMetricBrowser = activityFlowRowTemplate.ApplicationID;
                    switch (entityConnection["sourceNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            entityIdForMetricBrowser = activityFlowRowTemplate.FromEntityID;
                            activityFlowRowTemplate.FromType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.FromType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            deepLinkMetricTemplateInMetricBrowser = DEEPLINK_METRIC_TIER_TARGET_METRIC_ID;
                            activityFlowRowTemplate.FromType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.FromLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.FromEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.FromName = entityConnection["sourceNode"].ToString();
                            activityFlowRowTemplate.FromType = entityConnection["sourceNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    activityFlowRowTemplate.ToEntityID = (long)entityConnection["targetNodeDefinition"]["entityId"];
                    entity = (JObject)flowmapEntities.Where(e => (long)e["idNum"] == activityFlowRowTemplate.ToEntityID && e["entityType"].ToString() == entityConnection["targetNodeDefinition"]["entityType"].ToString()).FirstOrDefault();
                    if (entity != null)
                    {
                        activityFlowRowTemplate.ToName = entity["name"].ToString();
                    }
                    switch (entityConnection["targetNodeDefinition"]["entityType"].ToString())
                    {
                        case ENTITY_TYPE_FLOWMAP_TIER:
                            activityFlowRowTemplate.ToType = APMTier.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_TIER, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                            activityFlowRowTemplate.ToType = Backend.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_BACKEND, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ApplicationID, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                            activityFlowRowTemplate.ToType = APMApplication.ENTITY_TYPE;
                            activityFlowRowTemplate.ToLink = String.Format(DEEPLINK_APPLICATION, activityFlowRowTemplate.Controller, activityFlowRowTemplate.ToEntityID, DEEPLINK_THIS_TIMERANGE);
                            break;

                        default:
                            activityFlowRowTemplate.ToName = entityConnection["targetNode"].ToString();
                            activityFlowRowTemplate.ToType = entityConnection["targetNodeDefinition"]["entityType"].ToString();
                            break;
                    }

                    // Haven't seen the incoming calls on the flowmap for Nodes. But maybe?
                    if (startTier != null)
                    {
                        if (activityFlowRowTemplate.FromEntityID == (long)startTier["idNum"])
                        {
                            activityFlowRowTemplate.CallDirection = "FirstHop";
                        }
                        else
                        {
                            activityFlowRowTemplate.CallDirection = "SubsequentHop";
                        }
                    }

                    // Process each of the stats nodes, duplicating things as we need them
                    foreach (JToken entityConnectionStat in entityConnection["stats"])
                    {
                        ActivityFlow activityFlowRow = activityFlowRowTemplate.Clone();

                        activityFlowRow.CallType = entityConnectionStat["exitPointCall"]["exitPointType"].ToString();
                        if (activityFlowRow.CallType.Length == 0)
                        {
                            activityFlowRow.CallType = entity["backendType"].ToString();
                            if (activityFlowRow.CallType.Length == 0)
                            {
                                if ((bool)entityConnectionStat["exitPointCall"]["customExitPoint"] == true)
                                {
                                    activityFlowRow.CallType = "Custom";
                                }
                            }
                        }
                        if (((bool)entityConnectionStat["async"]) == true)
                        {
                            activityFlowRow.CallType = String.Format("{0} async", activityFlowRow.CallType);
                        }

                        if (entityConnectionStat["averageResponseTime"].HasValues == true)
                        {
                            activityFlowRow.ART = (long)entityConnectionStat["averageResponseTime"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["averageResponseTime"]["metricId"]);
                        }
                        if (entityConnectionStat["callsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.CPM = (long)entityConnectionStat["callsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["callsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["errorsPerMinute"].HasValues == true)
                        {
                            activityFlowRow.EPM = (long)entityConnectionStat["errorsPerMinute"]["metricValue"];
                            activityFlowRow.MetricsIDs.Add((long)entityConnectionStat["errorsPerMinute"]["metricId"]);
                        }
                        if (entityConnectionStat["numberOfCalls"].HasValues == true) { activityFlowRow.Calls = (long)entityConnectionStat["numberOfCalls"]["metricValue"]; }
                        if (entityConnectionStat["numberOfErrors"].HasValues == true) { activityFlowRow.Errors = (long)entityConnectionStat["numberOfErrors"]["metricValue"]; }

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
                            StringBuilder sb = new StringBuilder(128);
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
            activityFlowsList = activityFlowsList.OrderBy(a => a.CallDirection).ThenBy(a => a.FromType).ThenBy(a => a.FromName).ThenBy(a => a.ToType).ThenBy(a => a.ToName).ThenBy(a => a.CallType).ThenBy(a => a.CPM).ToList();

            return activityFlowsList;
        }
    }
}
