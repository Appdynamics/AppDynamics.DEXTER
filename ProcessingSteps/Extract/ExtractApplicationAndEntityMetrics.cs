using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractApplicationAndEntityMetrics : JobStepBase
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

                        stepTimingTarget.NumEntities = entityMetricExtractMappingList.Count;

                        #endregion

                        loggerConsole.Info("Extract Metrics for All Entities ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityApplication.ENTITY_FOLDER, EntityApplication.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Tiers

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityTier.ENTITY_FOLDER, EntityTier.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityNode.ENTITY_FOLDER, EntityNode.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityBackend.ENTITY_FOLDER, EntityBackend.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityBusinessTransaction.ENTITY_FOLDER, EntityBusinessTransaction.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Service Endpoints

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityServiceEndpoint.ENTITY_FOLDER, EntityServiceEndpoint.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Errors

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityError.ENTITY_FOLDER, EntityError.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Information Points

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, EntityInformationPoint.ENTITY_FOLDER, EntityInformationPoint.ENTITY_TYPE);

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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            if (jobConfiguration.Input.Metrics == false)
            {
                loggerConsole.Trace("Skipping export of entity metrics");
            }
            return (jobConfiguration.Input.Metrics == true);
        }

        private void getMetricsForEntities(
            JobTarget jobTarget,
            JobConfiguration jobConfiguration,
            List<MetricExtractMapping> entityMetricExtractMappingList,
            string entityFolderName,
            string entityType)
        {
            ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

            List<MetricExtractMapping> entityMetricExtractMappingListFiltered = entityMetricExtractMappingList.Where(m => m.EntityType == entityType).ToList();

            loggerConsole.Info("Extract {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);
            logger.Info("Extract {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);

            foreach (MetricExtractMapping metricExtractMapping in entityMetricExtractMappingListFiltered)
            {
                // Get the full range
                JobTimeRange jobTimeRange = jobConfiguration.Input.TimeRange;

                logger.Info("Retrieving metric in Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricExtractMapping.MetricPath, jobTimeRange.From, jobTimeRange.To);

                string metricsJson = String.Empty;

                string metricsDataFilePath = FilePathMap.MetricFullRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange);
                if (File.Exists(metricsDataFilePath) == false)
                {
                    // First range is the whole thing
                    metricsJson = controllerApi.GetMetricData(
                        jobTarget.ApplicationID,
                        metricExtractMapping.MetricPath,
                        UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From),
                        UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To),
                        true);

                    if (metricsJson != String.Empty) FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                }

                // Get the hourly time ranges
                for (int j = 0; j < jobConfiguration.Input.HourlyTimeRanges.Count; j++)
                {
                    jobTimeRange = jobConfiguration.Input.HourlyTimeRanges[j];

                    logger.Info("Retrieving metric for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricExtractMapping.MetricPath, jobTimeRange.From, jobTimeRange.To);

                    metricsDataFilePath = FilePathMap.MetricHourRangeDataFilePath(jobTarget, entityFolderName, metricExtractMapping.FolderName, jobTimeRange);
                    if (File.Exists(metricsDataFilePath) == false)
                    {
                        // Subsequent ones are details
                        metricsJson = controllerApi.GetMetricData(
                            jobTarget.ApplicationID,
                            metricExtractMapping.MetricPath,
                            UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From),
                            UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To),
                            false);

                        if (metricsJson != String.Empty) FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                    }
                }
            }

            loggerConsole.Info("Completed {0} ({1} metrics)", entityType, entityMetricExtractMappingListFiltered.Count);
        }
    }
}
