using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractDBMetrics : JobStepExtractBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_DB) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_DB);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_DB);

                    return true;
                }

                List<MetricExtractMapping> entityMetricExtractMappingList = getMetricsExtractMappingList(jobConfiguration);

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_DB) continue;

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

                        stepTimingTarget.NumEntities = entityMetricExtractMappingList.Count;

                        #endregion

                        loggerConsole.Info("Extract Metrics for All Entities ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                        ParallelOptions parallelOptions = new ParallelOptions();
                        if (programOptions.ProcessSequentially == true)
                        {
                            parallelOptions.MaxDegreeOfParallelism = 1;
                        }

                        Parallel.Invoke(parallelOptions,
                            () =>
                            {
                                #region Database metrics

                                getMetricsForEntitiesDB(jobTarget, jobConfiguration, entityMetricExtractMappingList, DBApplication.ENTITY_FOLDER, DBApplication.ENTITY_TYPE);

                                #endregion
                            }
                        );
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

        private void getMetricsForEntitiesDB(
            JobTarget jobTarget,
            JobConfiguration jobConfiguration,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            string entityFolderName,
            string entityType)
        {
            using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
            {
                List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType).ToList();

                loggerConsole.Info("Extract {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);
                logger.Info("Extract {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);

                foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingListFiltered)
                {
                    // Get the full range
                    JobTimeRange jobTimeRange = jobConfiguration.Input.TimeRange;

                    string metricPath = String.Format("Databases|{0}|{1}", jobTarget.Application, metricExtractMapping.MetricPath);

                    loggerConsole.Trace("{0} {1}", metricExtractMapping.EntityType, metricPath);
                    logger.Info("Retrieving metric summary for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricPath, jobTimeRange.From, jobTimeRange.To);

                    string metricsJson = String.Empty;

                    string metricsDataFilePath = FilePathMap.MetricFullRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange);
                    if (File.Exists(metricsDataFilePath) == false)
                    {
                        // First range is the whole thing
                        metricsJson = controllerApi.GetMetricData(
                            jobTarget.ApplicationID,
                            metricPath,
                            UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From),
                            UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To),
                            true);

                        if (metricsJson != String.Empty && metricsJson != "[ ]") FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                    }

                    if (jobConfiguration.Input.MetricsSelectionCriteria.IncludeHourAndMinuteDetail == true)
                    {
                        // Get the hourly time ranges
                        for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                        {
                            jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                            logger.Info("Retrieving metric details for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricPath, jobTimeRange.From, jobTimeRange.To);

                            metricsDataFilePath = FilePathMap.MetricHourRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange);
                            if (File.Exists(metricsDataFilePath) == false)
                            {
                                // Subsequent ones are details
                                metricsJson = controllerApi.GetMetricData(
                                    jobTarget.ApplicationID,
                                    metricPath,
                                    UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From),
                                    UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To),
                                    false);

                                if (metricsJson != String.Empty && metricsJson != "[ ]") FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                            }
                        }
                    }
                }

                loggerConsole.Info("Completed {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);
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
                loggerConsole.Trace("Skipping export of entity metrics");
            }
            return (jobConfiguration.Input.Metrics == true);
        }
    }
}
