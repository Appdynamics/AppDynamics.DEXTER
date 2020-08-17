using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexWEBMetrics : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_WEB) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);

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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_WEB) continue;

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

                                List<WEBApplication> webApplicationList = FileIOHelper.ReadListFromCSVFile<WEBApplication>(FilePathMap.WEBApplicationsIndexFilePath(jobTarget), new WEBApplicationReportMap());
                                if (webApplicationList != null)
                                {
                                    Dictionary<string, WEBEntityBase> entitiesDictionaryByName = webApplicationList.ToDictionary(e => e.ApplicationName, e => (WEBEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, WEBApplication.ENTITY_FOLDER, WEBApplication.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBApplication.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, WEBApplication.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Pages

                                List<WEBPage> webPagesList = FileIOHelper.ReadListFromCSVFile<WEBPage>(FilePathMap.WEBPagesIndexFilePath(jobTarget), new WEBPageReportMap());
                                if (webPagesList != null)
                                {
                                    Dictionary<string, WEBEntityBase> entitiesDictionaryByName = webPagesList.Where(e => e.PageType == "BASE_PAGE").ToDictionary(e => e.PageName, e => (WEBEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, WEBPage.ENTITY_FOLDER, WEBPage.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBPage.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, WEBPage.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region AJAX Requests

                                List<WEBPage> webPagesList = FileIOHelper.ReadListFromCSVFile<WEBPage>(FilePathMap.WEBPagesIndexFilePath(jobTarget), new WEBPageReportMap());
                                if (webPagesList != null)
                                {
                                    Dictionary<string, WEBEntityBase> entitiesDictionaryByName = webPagesList.Where(e => e.PageType == "AJAX_REQUEST").ToDictionary(e => e.PageName, e => (WEBEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, WEBAJAXRequest.ENTITY_FOLDER, WEBAJAXRequest.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBAJAXRequest.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, WEBAJAXRequest.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Virtual Pages

                                List<WEBPage> webPagesList = FileIOHelper.ReadListFromCSVFile<WEBPage>(FilePathMap.WEBPagesIndexFilePath(jobTarget), new WEBPageReportMap());
                                if (webPagesList != null)
                                {
                                    Dictionary<string, WEBEntityBase> entitiesDictionaryByName = webPagesList.Where(e => e.PageType == "VIRTUAL_PAGE").ToDictionary(e => e.PageName, e => (WEBEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, WEBVirtualPage.ENTITY_FOLDER, WEBVirtualPage.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBVirtualPage.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, WEBVirtualPage.ENTITY_FOLDER, metricValuesListContainer.Key));
                                        }
                                    }

                                    Interlocked.Add(ref numEntitiesTotal, metricValuesDictionary.Keys.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region IFrames

                                List<WEBPage> webPagesList = FileIOHelper.ReadListFromCSVFile<WEBPage>(FilePathMap.WEBPagesIndexFilePath(jobTarget), new WEBPageReportMap());
                                if (webPagesList != null)
                                {
                                    Dictionary<string, WEBEntityBase> entitiesDictionaryByName = webPagesList.Where(e => e.PageType == "IFRAME").ToDictionary(e => e.PageName, e => (WEBEntityBase)e);

                                    Dictionary<string, List<MetricValue>> metricValuesDictionary = new Dictionary<string, List<MetricValue>>();

                                    for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                                    {
                                        JobTimeRange jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                                        readGranularRangeOfMetricsIntoEntities(entitiesDictionaryByName, jobTimeRange, jobTarget, entityMetricExtractMappingList, WEBIFrame.ENTITY_FOLDER, WEBIFrame.ENTITY_TYPE, metricValuesDictionary);
                                    }

                                    // Save individual metric files and create index of their internal structure
                                    foreach (KeyValuePair<string, List<MetricValue>> metricValuesListContainer in metricValuesDictionary)
                                    {
                                        if (metricValuesListContainer.Value.Count > 0)
                                        {
                                            List<MetricValue> metricValuesSorted = metricValuesListContainer.Value.OrderBy(o => o.EntityID).ThenBy(o => o.MetricID).ThenBy(o => o.EventTimeStampUtc).ToList();

                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBIFrame.ENTITY_FOLDER, metricValuesListContainer.Key));
                                            FileIOHelper.WriteListToCSVFile(metricValuesSorted, new MetricValueReportMap(), FilePathMap.MetricReportPerAppFilePath(jobTarget, WEBIFrame.ENTITY_FOLDER, metricValuesListContainer.Key));
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
                                case WEBApplication.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(WEBApplication.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBApplication.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case WEBPage.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(WEBPage.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBPage.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case WEBAJAXRequest.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(WEBAJAXRequest.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBAJAXRequest.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case WEBVirtualPage.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(WEBVirtualPage.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBVirtualPage.ENTITY_FOLDER, metricExtractMapping.FolderName));
                                    break;

                                case WEBIFrame.ENTITY_TYPE:
                                    FileIOHelper.AppendTwoCSVFiles(
                                        FilePathMap.MetricReportFilePath(WEBIFrame.ENTITY_FOLDER, metricExtractMapping.FolderName, jobTarget),
                                        FilePathMap.MetricValuesIndexFilePath(jobTarget, WEBIFrame.ENTITY_FOLDER, metricExtractMapping.FolderName));
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
            Dictionary<string, WEBEntityBase> entitiesDictionaryByName,
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
            Dictionary<string, WEBEntityBase> entitiesDictionaryByName,
            JobTarget jobTarget,
            JobTimeRange jobTimeRange)
        {
            WEBEntityBase entity = null;

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

                switch (metricPathComponents[1])
                {
                    case "App":

                        #region Application level

                        // MetricPath = End User Experience|App|Requests per Minute
                        // MetricName = EUM|App|Page Requests per Minute

                        entity = entitiesDictionaryByName.FirstOrDefault().Value;

                        #endregion

                        break;

                    case "Base Pages":

                        #region Web Pages

                        // MetricPath = End User Experience|Base Pages|dev-movietix.demo.appdynamics.com/login.aspx|Requests per Minute
                        // MetricName = BTM|Application Diagnostic Data|Base Page:976|Requests per Minute

                        entitiesDictionaryByName.TryGetValue(metricPathComponents[2], out entity);

                        #endregion

                        break;

                    case "AJAX Requests":

                        #region AJAX Requests

                        // MetricPath = End User Experience|AJAX Requests|dev-movietix.demo.appdynamics.com/api/search|Requests per Minute
                        // MetricName = BTM|Application Diagnostic Data|AJAX Request:2310|Requests per Minute

                        entitiesDictionaryByName.TryGetValue(metricPathComponents[2], out entity);

                        #endregion

                        break;

                    case "Virtual Pages":
                        
                        #region IFrame

                        //"metricName" : "BTM|Application Diagnostic Data|Virtual Page:4762|Requests per Minute",
                        //"metricPath" : "End User Experience|Virtual Pages|Login|Requests per Minute",

                        entitiesDictionaryByName.TryGetValue(metricPathComponents[2], out entity);

                        #endregion

                        break;

                    case "IFrames":

                        #region IFrame

                        // No examples I've seen have these

                        entitiesDictionaryByName.TryGetValue(metricPathComponents[2], out entity);

                        #endregion

                        break;

                    default:
                        // Unsupported type of metric
                        logger.Warn("Metric path='{0}' is not of supported type of metric for processing", appDRESTMetric.metricPath);

                        break;
                }

                #endregion

                List<MetricValue> metricValuesConverted = readMetricValuesIntoEntity(entity, jobTarget, metricName, appDRESTMetric, timerangeDuration);
                metricValues.AddRange(metricValuesConverted);
            }

            return metricValues;
        }

        private List<MetricValue> readMetricValuesIntoEntity(WEBEntityBase entity, JobTarget jobTarget, string metricName, AppDRESTMetric appDRESTMetric, int timerangeDuration)
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
                    if (entity is WEBApplication)
                    {
                        WEBApplication webApplication = (WEBApplication)entity;

                        metricValue.EntityID = webApplication.ApplicationID;
                        metricValue.EntityName = webApplication.ApplicationName;
                        metricValue.EntityType = WEBApplication.ENTITY_TYPE;
                    }
                    else if (entity is WEBPage)
                    {
                        WEBPage webPage = (WEBPage)entity;

                        metricValue.EntityID = webPage.PageID;
                        metricValue.EntityName = webPage.PageName;
                        metricValue.EntityType = webPage.PageType;
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
