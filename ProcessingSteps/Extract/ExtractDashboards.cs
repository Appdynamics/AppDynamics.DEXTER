using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractDashboards : JobStepBase
    {
        private const int DASHBOARDS_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 50;
        private const int DASHBOARDS_EXTRACT_NUMBER_OF_THREADS = 8;

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

                // Process each Controller once
                int i = 0;
                var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = controllerGroup.ToList()[0];

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region All Dashboards

                        // Set up controller access
                        string allDashboardsJSON = String.Empty;
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            controllerApi.PrivateApiLogin();

                            loggerConsole.Info("All Dashboards");

                            allDashboardsJSON = controllerApi.GetControllerDashboards();
                            if (allDashboardsJSON != String.Empty) FileIOHelper.SaveFileToPath(allDashboardsJSON, FilePathMap.ControllerDashboards(jobTarget));

                        }

                        #endregion

                        #region Dashboards

                        if (allDashboardsJSON != String.Empty)
                        {
                            JArray allDashboardsArray = JArray.Parse(allDashboardsJSON);

                            loggerConsole.Info("Dashboards ({0} entities)", allDashboardsArray.Count);

                            int numDashboards = 0;

                            var listOfDashboardsChunks = allDashboardsArray.BreakListIntoChunks(DASHBOARDS_EXTRACT_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD);

                            ParallelOptions parallelOptions = new ParallelOptions();
                            if (programOptions.ProcessSequentially == true)
                            {
                                parallelOptions.MaxDegreeOfParallelism = 1;
                            }
                            else
                            {
                                parallelOptions.MaxDegreeOfParallelism = DASHBOARDS_EXTRACT_NUMBER_OF_THREADS;
                            }

                            Parallel.ForEach<List<JToken>, int>(
                                listOfDashboardsChunks,
                                parallelOptions,
                                () => 0,
                                (listOfDashboardsChunk, loop, subtotal) =>
                                {
                                    // Set up controller access
                                    using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApiParallel.PrivateApiLogin();

                                        foreach (JObject dashboardObject in listOfDashboardsChunk)
                                        {
                                            if (File.Exists(FilePathMap.ControllerDashboard(jobTarget, dashboardObject["name"].ToString(), (long)dashboardObject["id"])) == false)
                                            {
                                                string dashboardJSON = controllerApiParallel.GetControllerDashboard((long)dashboardObject["id"]);
                                                if (dashboardJSON != String.Empty) FileIOHelper.SaveFileToPath(dashboardJSON, FilePathMap.ControllerDashboard(jobTarget, dashboardObject["name"].ToString(), (long)dashboardObject["id"]));
                                            }
                                        }
                                        return listOfDashboardsChunk.Count;
                                    }
                                },
                                (finalResult) =>
                                {
                                    Interlocked.Add(ref numDashboards, finalResult);
                                    Console.Write("[{0}].", numDashboards);
                                }
                            );

                            loggerConsole.Info("Completed {0} Dashboards", allDashboardsArray.Count);

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + allDashboardsArray.Count;
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

                    i++;
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
            logger.Trace("LicensedReports.Dashboards={0}", programOptions.LicensedReports.Dashboards);
            loggerConsole.Trace("LicensedReports.Dashboards={0}", programOptions.LicensedReports.Dashboards);
            if (programOptions.LicensedReports.Dashboards == false)
            {
                loggerConsole.Warn("Not licensed for dashboards");
                return false;
            }

            logger.Trace("Input.Dashboards={0}", jobConfiguration.Input.Dashboards);
            loggerConsole.Trace("Input.Dashboards={0}", jobConfiguration.Input.Dashboards);
            if (jobConfiguration.Input.Dashboards == false)
            {
                loggerConsole.Trace("Skipping export of dashboards");
            }
            return (jobConfiguration.Input.Dashboards == true);
        }
    }
}
