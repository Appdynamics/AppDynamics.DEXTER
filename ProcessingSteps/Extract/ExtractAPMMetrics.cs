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
    public class ExtractAPMMetrics : JobStepExtractBase
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
                                #region Application

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMApplication.ENTITY_FOLDER, APMApplication.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Tiers

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMTier.ENTITY_FOLDER, APMTier.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMNode.ENTITY_FOLDER, APMNode.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMBackend.ENTITY_FOLDER, APMBackend.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMBusinessTransaction.ENTITY_FOLDER, APMBusinessTransaction.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Service Endpoints

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMServiceEndpoint.ENTITY_FOLDER, APMServiceEndpoint.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Errors

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMError.ENTITY_FOLDER, APMError.ENTITY_TYPE);

                                #endregion
                            },
                            () =>
                            {
                                #region Information Points

                                getMetricsForEntities(jobTarget, jobConfiguration, entityMetricExtractMappingList, APMInformationPoint.ENTITY_FOLDER, APMInformationPoint.ENTITY_TYPE);

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
