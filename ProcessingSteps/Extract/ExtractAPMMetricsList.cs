using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml.ConditionalFormatting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractAPMMetricsList : JobStepExtractBase
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

                    stepTimingTarget.NumEntities = 10;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        loggerConsole.Info("Extract List Metrics for All Entities");

                        ParallelOptions parallelOptions = new ParallelOptions();
                        if (programOptions.ProcessSequentially == true)
                        {
                            parallelOptions.MaxDegreeOfParallelism = 1;
                        }

                        Parallel.Invoke(parallelOptions,
                            () =>
                            {
                                #region Application Infrastructure Performance

                                extractListOfMetrics(jobTarget, jobConfiguration, "Application Infrastructure Performance", "INFRA", 14);

                                #endregion
                            },
                            () =>
                            {
                                #region Overall Application Performance

                                extractListOfMetrics(jobTarget, jobConfiguration, "Overall Application Performance", "APP", 12);

                                #endregion
                            },
                            () =>
                            {
                                #region Backends 

                                extractListOfMetrics(jobTarget, jobConfiguration, "Backends", "BACKEND", 6);

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                extractListOfMetrics(jobTarget, jobConfiguration, "Business Transaction Performance", "BT", 22);

                                #endregion
                            },
                            () =>
                            {
                                #region Service Endpoints

                                extractListOfMetrics(jobTarget, jobConfiguration, "Service Endpoints", "SEP", 5);

                                #endregion
                            },
                            () =>
                            {
                                #region Errors

                                extractListOfMetrics(jobTarget, jobConfiguration, "Errors", "ERR", 7);

                                #endregion
                            },
                            () =>
                            {
                                #region Information Points

                                extractListOfMetrics(jobTarget, jobConfiguration, "Information Points", "IP", 6);

                                #endregion
                            },
                            () =>
                            {
                                #region Web End User Experience

                                extractListOfMetrics(jobTarget, jobConfiguration, "End User Experience", "WEB", 6);

                                #endregion
                            },
                            () =>
                            {
                                #region Mobile End User Experience

                                extractListOfMetrics(jobTarget, jobConfiguration, "Mobile", "MOBILE", 8);

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

        private void extractListOfMetrics(
            JobTarget jobTarget,
            JobConfiguration jobConfiguration,
            string metricPathPrefix,
            string fileNamePrefix,
            int maxDepth)
        {
            using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
            {
                DateTime startTime = jobConfiguration.Input.HourlyTimeRanges[jobConfiguration.Input.HourlyTimeRanges.Count - 1].From;
                DateTime endTime = jobConfiguration.Input.HourlyTimeRanges[jobConfiguration.Input.HourlyTimeRanges.Count - 1].To;

                StringBuilder sbMetricPath = new StringBuilder(128);
                sbMetricPath.Append(metricPathPrefix);

                for (int currentDepth = 0; currentDepth <= maxDepth; currentDepth++)
                {
                    sbMetricPath.Append("|*");

                    string metricPath = sbMetricPath.ToString();
                    loggerConsole.Trace("Depth {0:00}/{1:00}, {2}", currentDepth, maxDepth, metricPath);
                    logger.Info("Retrieving metric lists for Application {0}({1}), Metric='{2}', From {3:o}, To {4:o}", jobTarget.Application, jobTarget.ApplicationID, metricPath, startTime, endTime);

                    string metricsJson = String.Empty;

                    string metricsDataFilePath = FilePathMap.MetricsListDataFilePath(jobTarget, fileNamePrefix, currentDepth);
                    if (File.Exists(metricsDataFilePath) == false)
                    {
                        metricsJson = controllerApi.GetMetricData(
                            jobTarget.ApplicationID,
                            metricPath,
                            UnixTimeHelper.ConvertToUnixTimestamp(startTime),
                            UnixTimeHelper.ConvertToUnixTimestamp(endTime),
                            true);

                        FileIOHelper.SaveFileToPath(metricsJson, metricsDataFilePath);
                    }
                }
            }
        }

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.EntityMetrics={0}", programOptions.LicensedReports.MetricsList);
            loggerConsole.Trace("LicensedReports.EntityMetrics={0}", programOptions.LicensedReports.MetricsList);
            if (programOptions.LicensedReports.MetricsList == false)
            {
                loggerConsole.Warn("Not licensed for list of metrics");
                return false;
            }

            logger.Trace("Input.MetricsList={0}", jobConfiguration.Input.MetricsList);
            loggerConsole.Trace("Input.MetricsList={0}", jobConfiguration.Input.MetricsList);
            if (jobConfiguration.Input.MetricsList == false)
            {
                loggerConsole.Trace("Skipping export of list of metrics");
            }
            return (jobConfiguration.Input.MetricsList == true);
        }
    }
}
