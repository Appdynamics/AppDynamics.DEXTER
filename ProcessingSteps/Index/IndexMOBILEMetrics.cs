using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexMOBILEMetrics : JobStepIndexBase
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
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_MOBILE) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_MOBILE);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_MOBILE);

                    return true;
                }

                bool reportFolderCleaned = false;

                List<MetricExtractMapping> entityMetricExtractMappingList = getMetricsExtractMappingList(jobConfiguration);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_MOBILE) continue;

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

                        ParallelOptions parallelOptions = new ParallelOptions();
                        if (programOptions.ProcessSequentially == true)
                        {
                            parallelOptions.MaxDegreeOfParallelism = 1;
                        }

                        Parallel.Invoke(parallelOptions,
                            () =>
                            {
                                #region Application

                                List<MOBILEApplication> mobileApplicationList = FileIOHelper.ReadListFromCSVFile<MOBILEApplication>(FilePathMap.MOBILEApplicationsIndexFilePath(jobTarget), new MOBILEApplicationReportMap());
                                if (mobileApplicationList != null)
                                {
                                    Dictionary<string, MOBILEEntityBase> entitiesDictionaryByName = mobileApplicationList.ToDictionary(e => e.ApplicationName, e => (MOBILEEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, MOBILEApplication.ENTITY_FOLDER, MOBILEApplication.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, MOBILEApplication.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, MOBILEApplication.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Network Requests

                                List<MOBILENetworkRequest> networkRequestsList = FileIOHelper.ReadListFromCSVFile<MOBILENetworkRequest>(FilePathMap.MOBILENetworkRequestsIndexFilePath(jobTarget), new MOBILENetworkRequestReportMap());
                                if (networkRequestsList != null)
                                {
                                    Dictionary<string, MOBILEEntityBase> entitiesDictionaryByName = networkRequestsList.ToDictionary(e => e.RequestName, e => (MOBILEEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, MOBILENetworkRequest.ENTITY_FOLDER, MOBILENetworkRequest.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, MOBILENetworkRequest.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, MOBILENetworkRequest.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            }
                        );

                        stepTimingTarget.NumEntities = numEntitiesTotal;

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.MetricsReportFolderPath(jobTarget));
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.MetricsReportFolderPath(jobTarget));
                            reportFolderCleaned = true;
                        }

                        // Combine the generated detailed metric value files
                        foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingList)
                        {
                            switch (metricExtractMapping.EntityType)
                            {
                                case MOBILEApplication.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(MOBILEApplication.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, MOBILEApplication.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case MOBILENetworkRequest.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(MOBILENetworkRequest.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, MOBILENetworkRequest.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                default:
                                    break;
                            }
                        }

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
            if (jobConfiguration.Input.Metrics == false)
            {
                loggerConsole.Trace("Skipping index of entity metrics");
            }
            return (jobConfiguration.Input.Metrics == true);
        }

        private void readGranularRangeOfMetricsIntoEntities(
            Dictionary<string, MOBILEEntityBase> entitiesDictionaryByName,
            JobTimeRange jobTimeRange,
            JobTarget jobTarget,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            string entityFolderName,
            string entityType,
            Dictionary<string, List<MetricValue>> metricValuesDictionary)
        {
            List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType).ToList();
            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingListFiltered)
            {
                List<AppDRESTMetric> metricData = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTMetric>(FilePathMap.MetricHourRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange));
                if (metricData != null)
                {
                    List<MetricValue> metricValues = readMetricsIntoEntities(metricData, entitiesDictionaryByName, jobTarget, jobTimeRange);
                    if (metricValues != null)
                    {
                        if (metricValuesDictionary.ContainsKey(metricExtractMapping.FolderName) == false)
                        {
                            metricValuesDictionary.Add(metricExtractMapping.FolderName, metricValues);
                        }
                        else
                        {
                            metricValuesDictionary[metricExtractMapping.FolderName].AddRange(metricValues);
                        }
                    }
                }
            }
        }

        private List<MetricValue> readMetricsIntoEntities(
            List<AppDRESTMetric> metricData,
            Dictionary<string, MOBILEEntityBase> entitiesDictionaryByName,
            JobTarget jobTarget,
            JobTimeRange jobTimeRange)
        {
            MOBILEEntityBase entity = null;

            int timerangeDuration = (int)(jobTimeRange.To - jobTimeRange.From).Duration().TotalMinutes;

            List<MetricValue> metricValues = new List<MetricValue>(metricData.Count * timerangeDuration);

            foreach (AppDRESTMetric appDRESTMetric in metricData)
            {
                if (appDRESTMetric.metricValues.Count == 0)
                {
                    // No metrics in this chunk
                    continue;
                }

                #region Get metric path components and metric name

                // Analyze metric path returned by the call to controller
                string[] metricPathComponents = appDRESTMetric.metricPath.Split('|');

                if (metricPathComponents.Length == 0)
                {
                    // Metric name was no good
                    logger.Warn("Metric path='{0}' could not be parsed into individual components", appDRESTMetric.metricPath);
                    continue;
                }

                string[] metricNameComponents = appDRESTMetric.metricName.Split('|');

                if (metricNameComponents.Length == 0)
                {
                    // Metric name was no good
                    logger.Warn("Metric name='{0}' could not be parsed into individual components", appDRESTMetric.metricName);
                    continue;
                }

                // Name of the metric is always the last one in the metric path
                string metricName = metricPathComponents[metricPathComponents.Length - 1];

                #endregion

                #region Determine metric entity type, scope and name from metric path

                if (metricPathComponents.Length == 3)
                {
                    #region Application level

                    // MetricPath = Mobile|iOS|App Crashes
                    // MetricName = MOBILE|Global|iOS|App Crashes

                    entity = entitiesDictionaryByName.FirstOrDefault().Value;

                    #endregion
                }
                else if (String.Compare(metricPathComponents[4], "Requests", true) == 0)
                {

                    #region Network Request

                    // MetricPath = Mobile|iOS|Apps|AD-DevOps-iOS|Requests|devops-payments-web:8080/devops-payments-web/checkout|Application Server Time (ms)
                    // MetricName = BTM|Application Diagnostic Data|MOBILE_REQUEST:17808|Application Server Time (ms)

                    entitiesDictionaryByName.TryGetValue(metricPathComponents[5], out entity);

                    #endregion
                }
                else
                {
                    // Unsupported type of metric
                    logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);
                }

                #endregion

                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, jobTarget, metricName, appDRESTMetric, timerangeDuration);
                metricValues.AddRange(metricValuesConverted);
            }

            return metricValues;
        }

        private List<MetricValue> readMetricValuesIntoEntity(MOBILEEntityBase entity, JobTarget jobTarget, string metricName, AppDRESTMetric appDRESTMetric, int timerangeDuration)
        {
            List<MetricValue> metricValues = new List<MetricValue>(appDRESTMetric.metricValues.Count);
            foreach (AppDRESTMetricValue appDRESTMetricValue in appDRESTMetric.metricValues)
            {
                // Populate metrics into the list for output into CSV
                MetricValue metricValue = new MetricValue();

                metricValue.Controller = jobTarget.Controller;
                metricValue.ApplicationID = jobTarget.ApplicationID;
                metricValue.ApplicationName = jobTarget.Application;

                if (entity != null)
                {
                    if (entity is MOBILEApplication)
                    {
                        MOBILEApplication mobileApplication = (MOBILEApplication)entity;

                        metricValue.EntityID = mobileApplication.ApplicationID;
                        metricValue.EntityName = mobileApplication.ApplicationName;
                        metricValue.EntityType = MOBILEApplication.ENTITY_TYPE;
                    }
                    else if (entity is MOBILENetworkRequest)
                    {
                        MOBILENetworkRequest networkRequest = (MOBILENetworkRequest)entity;

                        metricValue.EntityID = networkRequest.RequestID;
                        metricValue.EntityName = networkRequest.RequestName;
                        metricValue.EntityType = MOBILENetworkRequest.ENTITY_TYPE;
                    }
                }

                metricValue.EventTimeStampUtc = UnixTimeHelper.ConvertFromUnixTimestamp(appDRESTMetricValue.startTimeInMillis);
                metricValue.EventTimeStamp = metricValue.EventTimeStampUtc.ToLocalTime();
                metricValue.EventTime = metricValue.EventTimeStamp;

                metricValue.MetricName = metricName;
                metricValue.MetricID = appDRESTMetric.metricId;
                switch (appDRESTMetric.frequency)
                {
                    case "SIXTY_MIN":
                        {
                            metricValue.MetricResolution = 60;
                            break;
                        }
                    case "TEN_MIN":
                        {
                            metricValue.MetricResolution = 10;
                            break;
                        }
                    case "ONE_MIN":
                        {
                            metricValue.MetricResolution = 1;
                            break;
                        }
                    default:
                        {
                            metricValue.MetricResolution = 1;
                            break;
                        }
                }

                metricValue.Count = appDRESTMetricValue.count;
                metricValue.Min = appDRESTMetricValue.min;
                metricValue.Max = appDRESTMetricValue.max;
                metricValue.Occurrences = appDRESTMetricValue.occurrences;
                metricValue.Sum = appDRESTMetricValue.sum;
                metricValue.Value = appDRESTMetricValue.value;

                metricValues.Add(metricValue);
            }

            return metricValues;
        }
    }
}
