using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractApplicationEventsAndHealthRuleViolations : JobStepBase
    {
        private const int EVENTS_EXTRACT_NUMBER_OF_THREADS = 5;
        private const int EVENTS_EXTRACT_NUMBER_OF_EVENTS_TO_PROCESS_PER_THREAD = 200;

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

                bool haveProcessedAtLeastOneDBCollector = false;

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

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

                        if (jobTarget.Type == APPLICATION_TYPE_DB)
                        {
                            if (haveProcessedAtLeastOneDBCollector == false)
                            {
                                haveProcessedAtLeastOneDBCollector = true;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        int numEventsTotal = 0;

                        #region Health Rule violations

                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            loggerConsole.Info("Extract List of Health Rule Violations ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                            JArray listOfHealthRuleViolationsArray = new JArray();
                            if (File.Exists(FilePathMap.ApplicationHealthRuleViolationsDataFilePath(jobTarget)) == false)
                            {
                                foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                                {
                                    long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
                                    long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);

                                    string healthRuleViolationsJSON = controllerApi.GetApplicationHealthRuleViolations(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (healthRuleViolationsJSON != String.Empty)
                                    {
                                        try
                                        {
                                            // Load health rule violations
                                            JArray healthRuleViolationsInHourArray = JArray.Parse(healthRuleViolationsJSON);
                                            foreach (JObject healthRuleViolationObject in healthRuleViolationsInHourArray)
                                            {
                                                listOfHealthRuleViolationsArray.Add(healthRuleViolationObject);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Warn(ex);
                                            logger.Warn("Unable to parse JSON for HR Violations Application={0}, From={1}, To={2}", jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                        }
                                    }
                                }

                                if (listOfHealthRuleViolationsArray.Count > 0)
                                {
                                    FileIOHelper.WriteJArrayToFile(listOfHealthRuleViolationsArray, FilePathMap.ApplicationHealthRuleViolationsDataFilePath(jobTarget));

                                    logger.Info("{0} health rule violations from {1:o} to {2:o}", listOfHealthRuleViolationsArray.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                                    loggerConsole.Info("{0} health rule violations", listOfHealthRuleViolationsArray.Count);
                                }
                            }

                            numEventsTotal = numEventsTotal + listOfHealthRuleViolationsArray.Count;
                        }

                        #endregion

                        #region Events

                        // Filter Event Types
                        List<string> eventTypesToRetrieve = new List<string>(EVENT_TYPES.Count);
                        string allElement = Array.Find(jobConfiguration.Input.EventsSelectionCriteria, e => e.ToLower() == "all");
                        if (allElement != null && allElement.ToLower() == "all")
                        {
                            // No need to filter list of events
                            eventTypesToRetrieve = EVENT_TYPES;
                        }
                        else
                        {
                            // Filter events by the array
                            foreach (string eventTypeInSelectionCriteria in jobConfiguration.Input.EventsSelectionCriteria)
                            { 
                                string eventTypeInSelectionCriteriaFound = EVENT_TYPES.Find(e => e.ToLower() == eventTypeInSelectionCriteria.ToLower());
                                if (eventTypeInSelectionCriteriaFound != null && eventTypeInSelectionCriteriaFound.Length > 0)
                                {
                                    eventTypesToRetrieve.Add(eventTypeInSelectionCriteriaFound);
                                }
                            }
                        }

                        loggerConsole.Info("Extract {0} event types out of possible {1} ({2} time ranges)", eventTypesToRetrieve.Count, EVENT_TYPES.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                        ParallelOptions parallelOptions = new ParallelOptions();
                        if (programOptions.ProcessSequentially == true)
                        {
                            parallelOptions.MaxDegreeOfParallelism = 1;
                        }
                        else
                        { 
                            parallelOptions.MaxDegreeOfParallelism = EVENTS_EXTRACT_NUMBER_OF_THREADS;
                        }

                        Parallel.ForEach(
                            eventTypesToRetrieve,
                            parallelOptions,
                            () => 0,
                            (eventType, loop, subtotal) =>
                            {
                                using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                {
                                    int numEventsInType = extractEvents(jobConfiguration, jobTarget, controllerApiParallel, eventType);
                                    subtotal = subtotal + numEventsInType;
                                    return subtotal;
                                }
                            },
                            (finalResult) =>
                            {
                                Interlocked.Add(ref numEventsTotal, finalResult);
                            }
                        );
                        loggerConsole.Info("{0} events total", numEventsTotal);

                        
                        // Extract event details
                        foreach (String eventType in eventTypesToRetrieve)
                        {
                            if (File.Exists(FilePathMap.ApplicationEventsWithDetailsDataFilePath(jobTarget, eventType)) == false)
                            {
                                JArray listOfEventsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ApplicationEventsDataFilePath(jobTarget, eventType));
                                if (listOfEventsArray != null)
                                {
                                    loggerConsole.Info("Extract Details for {0} {1} events", listOfEventsArray.Count, eventType);

                                    List<JToken> listOfEvents = listOfEventsArray.ToObject<List<JToken>>();
                                    JArray listOfEventsWithDetailsArray = new JArray();
                                    object localLockObject = new object();

                                    int k = 0;
                                    var listOfEventsInChunks = listOfEvents.BreakListIntoChunks(EVENTS_EXTRACT_NUMBER_OF_EVENTS_TO_PROCESS_PER_THREAD);

                                    Parallel.ForEach<List<JToken>, int>(
                                        listOfEventsInChunks,
                                        parallelOptions,
                                        () => 0,
                                        (listOfEventsChunk, loop, subtotal) =>
                                        {
                                            using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                            {
                                            // Login into private API
                                            controllerApiParallel.PrivateApiLogin();

                                                List<JToken> listOfEventsWithDetailsInChunk = new List<JToken>(listOfEventsChunk.Count);

                                                foreach (JToken eventToken in listOfEventsChunk)
                                                {
                                                    JObject eventObject = (JObject)eventToken;

                                                // Retrieve details
                                                string eventDetailsJSON = controllerApiParallel.GetApplicationEventDetails(getLongValueFromJToken(eventObject, "id"), getLongValueFromJToken(eventObject, "eventTime"));
                                                    if (eventDetailsJSON != String.Empty)
                                                    {
                                                        JObject eventDetails = JObject.Parse(eventDetailsJSON);

                                                        eventObject.Add("details", eventDetails);
                                                    }

                                                    listOfEventsWithDetailsInChunk.Add(eventObject);
                                                }

                                                // Had to move this instead of into final result block, some items weren't getting returned there
                                                lock (localLockObject)
                                                {
                                                    foreach (JObject eventObject in listOfEventsWithDetailsInChunk)
                                                    {
                                                        listOfEventsWithDetailsArray.Add(eventObject);
                                                    }
                                                }
                                                Console.Write("[{0}].", listOfEventsWithDetailsArray.Count);

                                                return listOfEventsWithDetailsInChunk.Count;
                                            }
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref k, finalResult);
                                            Console.Write("[{0}].", k);
                                        }
                                    );

                                    loggerConsole.Info("{0} events total", listOfEventsWithDetailsArray.Count);

                                    if (listOfEventsWithDetailsArray.Count > 0)
                                    {
                                        FileIOHelper.WriteJArrayToFile(listOfEventsWithDetailsArray, FilePathMap.ApplicationEventsWithDetailsDataFilePath(jobTarget, eventType));

                                        logger.Info("{0} {1} events from {2:o} to {3:o}", eventType, listOfEventsWithDetailsArray.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                                        loggerConsole.Info("{0} {1} events", eventType, listOfEventsWithDetailsArray.Count);
                                    }
                                }
                            }
                        }

                        #endregion

                        stepTimingTarget.NumEntities = numEventsTotal;
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

        private int extractEvents(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, string eventType)
        {
            loggerConsole.Info("Extract List of Events {0} ({1} time ranges)", eventType, jobConfiguration.Input.HourlyTimeRanges.Count);

            JArray listOfEventsArray = new JArray();
            if (File.Exists(FilePathMap.ApplicationEventsDataFilePath(jobTarget, eventType)) == false)
            {
                foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                {
                    long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
                    long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);

                    try
                    {
                        string eventsJSON = controllerApi.GetApplicationEvents(jobTarget.ApplicationID, eventType, fromTimeUnix, toTimeUnix);
                        if (eventsJSON != String.Empty)
                        {
                            // Load events
                            JArray eventsInHourArray = JArray.Parse(eventsJSON);

                            foreach (JObject eventObject in eventsInHourArray)
                            {
                                listOfEventsArray.Add(eventObject);
                            }

                            Console.Write("[{0}]+", eventsInHourArray.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        logger.Warn("Unable to parse JSON for Events Application={0}, EventType={1}, From={2}, To={3}", jobTarget.ApplicationID, eventType, fromTimeUnix, toTimeUnix);
                    }
                }

                if (listOfEventsArray.Count > 0)
                {
                    FileIOHelper.WriteJArrayToFile(listOfEventsArray, FilePathMap.ApplicationEventsDataFilePath(jobTarget, eventType));

                    logger.Info("{0} {1} events from {2:o} to {3:o}", eventType, listOfEventsArray.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                    loggerConsole.Info("{0} {1} events", eventType, listOfEventsArray.Count);
                }
            }

            return listOfEventsArray.Count;
        }

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.Events={0}", programOptions.LicensedReports.Events);
            loggerConsole.Trace("LicensedReports.Events={0}", programOptions.LicensedReports.Events);
            if (programOptions.LicensedReports.Events == false)
            {
                loggerConsole.Warn("Not licensed for events");
                return false;
            }

            logger.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            loggerConsole.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            if (jobConfiguration.Input.Events == false)
            {
                loggerConsole.Trace("Skipping export of events");
            }
            return (jobConfiguration.Input.Events == true);
        }
    }
}
