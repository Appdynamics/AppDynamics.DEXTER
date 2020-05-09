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
    public class ExtractWEBEntities : JobStepBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_WEB) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);

                    return true;
                }

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

                    stepTimingTarget.NumEntities = 3;

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

                            #region EUM Pages

                            loggerConsole.Info("Pages and AJAX Requests");

                            string pagesJSON = controllerApi.GetWEBPages(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                            if (pagesJSON != String.Empty) FileIOHelper.SaveFileToPath(pagesJSON, FilePathMap.WEBPagesDataFilePath(jobTarget));

                            #endregion

                            #region EUM Pages Performance

                            if (pagesJSON != String.Empty)
                            {
                                JObject pagesListContainer = JObject.Parse(pagesJSON);

                                if (isTokenPropertyNull(pagesListContainer, "data") == false)
                                {
                                    JArray pagesArray = (JArray)pagesListContainer["data"];

                                    loggerConsole.Info("Performance of Pages and Ajax Requests ({0}) entities", pagesArray.Count);

                                    int j = 0;

                                    var listOfPagesChunks = pagesArray.BreakListIntoChunks(ENTITIES_EXTRACT_NUMBER_OF_PAGES_TO_PROCESS_PER_THREAD);

                                    ParallelOptions parallelOptions = new ParallelOptions();
                                    if (programOptions.ProcessSequentially == true)
                                    {
                                        parallelOptions.MaxDegreeOfParallelism = 1;
                                    }
                                    else
                                    {
                                        parallelOptions.MaxDegreeOfParallelism = PAGES_EXTRACT_NUMBER_OF_THREADS;
                                    }

                                    Parallel.ForEach<List<JToken>, int>(
                                        listOfPagesChunks,
                                        parallelOptions,
                                        () => 0,
                                        (listOfPagesChunk, loop, subtotal) =>
                                        {
                                            // Set up controller access
                                            using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                            {
                                                controllerApiParallel.PrivateApiLogin();

                                                foreach (JToken pageToken in listOfPagesChunk)
                                                {
                                                    string pageName = getStringValueFromJToken(pageToken, "name");
                                                    string pageType = getStringValueFromJToken(pageToken, "type");
                                                    long pageID = getLongValueFromJToken(pageToken, "addId");

                                                    if (File.Exists(FilePathMap.WEBPagePerformanceDataFilePath(jobTarget, pageType, pageName, pageID, jobConfiguration.Input.TimeRange)) == false)
                                                    {
                                                        string pageJSON = controllerApi.GetWEBPagePerformance(jobTarget.ApplicationID, pageID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                        if (pageJSON != String.Empty) FileIOHelper.SaveFileToPath(pageJSON, FilePathMap.WEBPagePerformanceDataFilePath(jobTarget, pageType, pageName, pageID, jobConfiguration.Input.TimeRange));
                                                    }
                                                }
                                                return listOfPagesChunk.Count;
                                            }
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref j, finalResult);
                                            Console.Write("[{0}].", j);
                                        }
                                    );

                                    loggerConsole.Info("Completed {0} Pages", pagesArray.Count);
                                }
                            }

                            #endregion

                            #region Geo Regions

                            loggerConsole.Info("Geo Locations");

                            string geoRegionsJSON = controllerApi.GetWEBGeoRegions(jobTarget.ApplicationID, String.Empty, String.Empty, String.Empty, fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (geoRegionsJSON != String.Empty) FileIOHelper.SaveFileToPath(geoRegionsJSON, FilePathMap.WEBGeoLocationsDataFilePath(jobTarget, "all"));

                            if (geoRegionsJSON != String.Empty)
                            {
                                JObject geoRegionsContainerObject = JObject.Parse(geoRegionsJSON);
                                if (geoRegionsContainerObject != null)
                                {
                                    if (isTokenPropertyNull(geoRegionsContainerObject, "rumItems") == false)
                                    {
                                        int j = 0;
                                        JArray geoRegionsArray = (JArray)geoRegionsContainerObject["rumItems"];
                                        foreach (JObject geoRegionObject in geoRegionsArray)
                                        {
                                            if (isTokenPropertyNull(geoRegionObject, "eumRegionPerformanceSummaryData") == false)
                                            {
                                                string country = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"], "country");

                                                string geoRegionJSON = controllerApi.GetWEBGeoRegions(jobTarget.ApplicationID, country, String.Empty, String.Empty, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                if (geoRegionJSON != String.Empty) FileIOHelper.SaveFileToPath(geoRegionJSON, FilePathMap.WEBGeoLocationsDataFilePath(jobTarget, country));
                                            }

                                            j++;
                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                        }

                                        loggerConsole.Info("Completed {0} Geo Locations and Regions", j);
                                    }
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
