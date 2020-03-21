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
    public class ExtractMOBILEEntities : JobStepBase
    {
        private const int PAGES_EXTRACT_NUMBER_OF_THREADS = 10;
        private const int ENTITIES_EXTRACT_NUMBER_OF_PAGES_TO_PROCESS_PER_THREAD = 10;

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
                    return true;
                }

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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            controllerApi.PrivateApiLogin();

                            #region Prepare time range

                            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                            #endregion

                            #region Network Requests

                            loggerConsole.Info("Network Requests");

                            string networkRequestsJSON = controllerApi.GetMOBILENetworkRequests(jobTarget.ParentApplicationID, jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                            if (networkRequestsJSON != String.Empty) FileIOHelper.SaveFileToPath(networkRequestsJSON, FilePathMap.MOBILENetworkRequestsDataFilePath(jobTarget));

                            #endregion

                            #region Network Requests Performance

                            if (networkRequestsJSON != String.Empty)
                            {
                                JObject networkRequestsContainer = JObject.Parse(networkRequestsJSON);

                                if (isTokenPropertyNull(networkRequestsContainer, "data") == false)
                                {
                                    JArray networkRequestsArray = (JArray)networkRequestsContainer["data"];

                                    loggerConsole.Info("Performance of Network Requests ({0}) entities", networkRequestsArray.Count);

                                    int j = 0;

                                    var listOfNetworkRequetsChunks = networkRequestsArray.BreakListIntoChunks(ENTITIES_EXTRACT_NUMBER_OF_PAGES_TO_PROCESS_PER_THREAD);

                                    Parallel.ForEach<List<JToken>, int>(
                                        listOfNetworkRequetsChunks,
                                        new ParallelOptions { MaxDegreeOfParallelism = PAGES_EXTRACT_NUMBER_OF_THREADS },
                                        () => 0,
                                        (listOfNetworkRequetsChunk, loop, subtotal) =>
                                        {
                                            // Set up controller access
                                            using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                            {
                                                controllerApiParallel.PrivateApiLogin();

                                                foreach (JToken networkRequestToken in listOfNetworkRequetsChunk)
                                                {
                                                    string networkRequestName = getStringValueFromJToken(networkRequestToken, "name");
                                                    long networkRequestID = getLongValueFromJToken(networkRequestToken, "addId");

                                                    if (File.Exists(FilePathMap.MOBILENetworkRequestPerformanceDataFilePath(jobTarget, networkRequestName, networkRequestID, jobConfiguration.Input.TimeRange)) == false)
                                                    {
                                                        string pageJSON = controllerApi.GetMOBILENetworkRequestPerformance(jobTarget.ParentApplicationID, jobTarget.ApplicationID, networkRequestID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                        if (pageJSON != String.Empty) FileIOHelper.SaveFileToPath(pageJSON, FilePathMap.MOBILENetworkRequestPerformanceDataFilePath(jobTarget, networkRequestName, networkRequestID, jobConfiguration.Input.TimeRange));
                                                    }
                                                }
                                                return listOfNetworkRequetsChunk.Count;
                                            }
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref j, finalResult);
                                            Console.Write("[{0}].", j);
                                        }
                                    );

                                    loggerConsole.Info("Completed {0} Network Requests", networkRequestsArray.Count);
                                }
                            }

                            #endregion
                        }
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
            logger.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            loggerConsole.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            if (programOptions.LicensedReports.DetectedEntities == false)
            {
                loggerConsole.Warn("Not licensed for detected entities");
                return false;
            }

            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping export of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
        }
    }
}
