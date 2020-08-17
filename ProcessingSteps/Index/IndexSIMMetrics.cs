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
    public class IndexSIMMetrics : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_SIM) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_SIM);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_SIM);

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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_SIM) continue;

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
                                #region Machine

                                List<SIMMachine> machinesList = FileIOHelper.ReadListFromCSVFile<SIMMachine>(FilePathMap.SIMMachinesIndexFilePath(jobTarget), new SIMMachineReportMap());
                                if (machinesList != null)
                                {
                                    Dictionary<string, SIMEntityBase> entitiesDictionaryByName = machinesList.ToDictionary(e => e.MachineName, e => (SIMEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, SIMMachine.ENTITY_FOLDER, SIMMachine.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, SIMMachine.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, SIMMachine.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Network

                                List<SIMMachine> machinesList = FileIOHelper.ReadListFromCSVFile<SIMMachine>(FilePathMap.SIMMachinesIndexFilePath(jobTarget), new SIMMachineReportMap());
                                if (machinesList != null)
                                {
                                    Dictionary<string, SIMEntityBase> entitiesDictionaryByName = machinesList.ToDictionary(e => e.MachineName, e => (SIMEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, SIMMachineNetwork.ENTITY_FOLDER, SIMMachineNetwork.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, SIMMachineNetwork.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, SIMMachineNetwork.ENTITY_FOLDER, metricValuesListContainer.Key));
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
                                case SIMMachine.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(SIMMachine.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, SIMMachine.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case SIMMachineNetwork.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(SIMMachineNetwork.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, SIMMachineNetwork.ENTITY_FOLDER, metricExtractMapping.FolderName));
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
            Dictionary<string, SIMEntityBase> entitiesDictionaryByName,
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
            Dictionary<string, SIMEntityBase> entitiesDictionaryByName,
            JobTarget jobTarget,
            JobTimeRange jobTimeRange)
        {
            SIMEntityBase entity = null;

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

                // Name of the metric is always the last one in the metric path, but we're going to take more then one
                string metricName = String.Format("{0} {1}", metricPathComponents[metricPathComponents.Length - 2], metricPathComponents[metricPathComponents.Length - 1]);

                #endregion

                #region Determine metric entity type, scope and name from metric path

                //"metricPath" : "Application Infrastructure Performance|Root|Containers|Individual Nodes|61b921e747a9|Hardware Resources|CPU|%Busy",
                //"metricPath" : "Application Infrastructure Performance|Root|Containers|Individual Nodes|8404a587a6c4|Hardware Resources|Disks|Reads/sec",
                //"metricPath" : "Application Infrastructure Performance|Root|Containers|Individual Nodes|61859e063c2e|Hardware Resources|Network|Incoming KB/sec",
                //"metricPath" : "Application Infrastructure Performance|Root|Containers|javaresourcehog|Individual Nodes|3c887b73cc52|Hardware Resources|Network|Incoming KB/sec",
                //"metricPath" : "Application Infrastructure Performance|Root|Containers|Individual Nodes|342508d53e0d|Hardware Resources|Memory|Used %",

                for (int i = 0; i < metricPathComponents.Length; i++)
                {
                    if (String.Compare(metricPathComponents[i], "Individual Nodes", true) == 0)
                    {
                        if (entitiesDictionaryByName.TryGetValue(metricPathComponents[i + 1], out entity) == false)
                        {
                            SIMMachine machine = new SIMMachine();
                            machine.MachineID = -1;
                            machine.MachineName = metricPathComponents[i + 1];
                            entity = machine;
                        }
                        break;
                    }
                }

                #endregion

                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, jobTarget, metricName, appDRESTMetric, timerangeDuration);
                metricValues.AddRange(metricValuesConverted);
            }

            return metricValues;
        }

        private List<MetricValue> readMetricValuesIntoEntity(SIMEntityBase entity, JobTarget jobTarget, string metricName, AppDRESTMetric appDRESTMetric, int timerangeDuration)
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
                    if (entity is SIMMachine)
                    {
                        SIMMachine machine = (SIMMachine)entity;

                        metricValue.EntityID = machine.MachineID;
                        metricValue.EntityName = machine.MachineName;
                        metricValue.EntityType = machine.MachineType;
                    }
                    else if (entity is SIMMachineNetwork)
                    {
                        SIMMachineNetwork machineNetwork = (SIMMachineNetwork)entity;

                        metricValue.EntityID = machineNetwork.MachineID;
                        metricValue.EntityName = machineNetwork.NetworkName;
                        metricValue.EntityType = machineNetwork.Speed.ToString();
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
