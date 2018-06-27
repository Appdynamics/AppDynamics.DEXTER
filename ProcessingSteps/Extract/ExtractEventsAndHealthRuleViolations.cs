using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractEventsAndHealthRuleViolations : JobStepBase
    {
        private const int EVENTS_EXTRACT_NUMBER_OF_THREADS = 5;

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

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        int numEventsTotal = 0;

                        #endregion

                        #region Health Rule violations

                        loggerConsole.Info("Extract List of Health Rule Violations ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                        numEventsTotal = numEventsTotal + extractHealthRuleViolations(jobConfiguration, jobTarget, controllerApi);

                        #endregion

                        #region Events

                        loggerConsole.Info("Extract {0} event types ({1} time ranges)", EVENT_TYPES.Count, jobConfiguration.Input.HourlyTimeRanges.Count);

                        Parallel.ForEach(
                            EVENT_TYPES,
                            new ParallelOptions { MaxDegreeOfParallelism = EVENTS_EXTRACT_NUMBER_OF_THREADS },
                            () => 0,
                            (eventType, loop, subtotal) =>
                            {
                                ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                                int numEventsInType = extractEvents(jobConfiguration, jobTarget, controllerApiParallel, eventType);
                                subtotal = subtotal + numEventsInType;
                                return subtotal;
                            },
                            (finalResult) =>
                            {
                                Interlocked.Add(ref numEventsTotal, finalResult);
                            }
                        );
                        loggerConsole.Info("{0} events total", numEventsTotal);

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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            loggerConsole.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            if (jobConfiguration.Input.Events == false)
            {
                loggerConsole.Trace("Skipping export of events");
            }
            return (jobConfiguration.Input.Events == true);
        }

        private int extractHealthRuleViolations(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi)
        {
            JArray listOfHealthRuleViolations = new JArray();
            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
            {
                long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
                long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);

                string healthRuleViolationsJSON = controllerApi.GetHealthRuleViolations(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                if (healthRuleViolationsJSON != String.Empty)
                {
                    // Load health rule violations
                    JArray healthRuleViolationsInHour = JArray.Parse(healthRuleViolationsJSON);
                    foreach (JObject healthRuleViolation in healthRuleViolationsInHour)
                    {
                        listOfHealthRuleViolations.Add(healthRuleViolation);
                    }
                }
            }

            if (listOfHealthRuleViolations.Count > 0)
            {
                FileIOHelper.WriteJArrayToFile(listOfHealthRuleViolations, FilePathMap.HealthRuleViolationsDataFilePath(jobTarget));

                logger.Info("{0} health rule violations from {1:o} to {2:o}", listOfHealthRuleViolations.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                loggerConsole.Info("{0} health rule violations", listOfHealthRuleViolations.Count);
            }

            return listOfHealthRuleViolations.Count;
        }

        private int extractEvents(JobConfiguration jobConfiguration, JobTarget jobTarget, ControllerApi controllerApi, string eventType)
        {
            JArray listOfEvents = new JArray();
            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
            {
                long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
                long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);

                string eventsJSON = controllerApi.GetEvents(jobTarget.ApplicationID, eventType, fromTimeUnix, toTimeUnix);
                if (eventsJSON != String.Empty)
                {
                    // Load events
                    JArray eventsInHour = JArray.Parse(eventsJSON);
                    foreach (JObject interestingEvent in eventsInHour)
                    {
                        listOfEvents.Add(interestingEvent);
                    }
                }
            }

            if (listOfEvents.Count > 0)
            {
                FileIOHelper.WriteJArrayToFile(listOfEvents, this.FilePathMap.EventsDataFilePath(jobTarget, eventType));

                logger.Info("{0} {1} events from {2:o} to {3:o}", eventType, listOfEvents.Count, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                loggerConsole.Info("{0} {1} events", eventType, listOfEvents.Count);
            }

            return listOfEvents.Count;
        }
    }
}
