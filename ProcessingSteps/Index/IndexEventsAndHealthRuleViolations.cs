using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexEventsAndHealthRuleViolations : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    return true;
                }

                bool reportFolderCleaned = false;

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

                        #region Health Rule violations

                        loggerConsole.Info("Index health rule violations");

                        List<HealthRuleViolationEvent> healthRuleViolationList = new List<HealthRuleViolationEvent>();

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        if (File.Exists(FilePathMap.HealthRuleViolationsDataFilePath(jobTarget)))
                        {
                            JArray eventsInHour = FileIOHelper.LoadJArrayFromFile(FilePathMap.HealthRuleViolationsDataFilePath(jobTarget));
                            if (eventsInHour != null)
                            {
                                foreach (JObject interestingEvent in eventsInHour)
                                {
                                    HealthRuleViolationEvent eventRow = new HealthRuleViolationEvent();
                                    eventRow.Controller = jobTarget.Controller;
                                    eventRow.ApplicationName = jobTarget.Application;
                                    eventRow.ApplicationID = jobTarget.ApplicationID;

                                    eventRow.EventID = (long)interestingEvent["id"];
                                    eventRow.FromUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)interestingEvent["startTimeInMillis"]);
                                    eventRow.From = eventRow.FromUtc.ToLocalTime();
                                    if ((long)interestingEvent["endTimeInMillis"] > 0)
                                    {
                                        eventRow.ToUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)interestingEvent["endTimeInMillis"]);
                                        eventRow.To = eventRow.FromUtc.ToLocalTime();
                                    }
                                    eventRow.Status = interestingEvent["incidentStatus"].ToString();
                                    eventRow.Severity = interestingEvent["severity"].ToString();
                                    eventRow.EventLink = String.Format(DEEPLINK_INCIDENT, eventRow.Controller, eventRow.ApplicationID, eventRow.EventID, interestingEvent["startTimeInMillis"], DEEPLINK_THIS_TIMERANGE); ;

                                    eventRow.Description = interestingEvent["description"].ToString();

                                    if (interestingEvent["triggeredEntityDefinition"].HasValues == true)
                                    {
                                        eventRow.HealthRuleID = (int)interestingEvent["triggeredEntityDefinition"]["entityId"];
                                        eventRow.HealthRuleName = interestingEvent["triggeredEntityDefinition"]["name"].ToString();
                                        // TODO the health rule can't be hotlinked to until platform rewrites the screen that opens from Flash
                                        eventRow.HealthRuleLink = String.Format(DEEPLINK_HEALTH_RULE, eventRow.Controller, eventRow.ApplicationID, eventRow.HealthRuleID, DEEPLINK_THIS_TIMERANGE);
                                    }

                                    if (interestingEvent["affectedEntityDefinition"].HasValues == true)
                                    {
                                        eventRow.EntityID = (int)interestingEvent["affectedEntityDefinition"]["entityId"];
                                        eventRow.EntityName = interestingEvent["affectedEntityDefinition"]["name"].ToString();

                                        string entityType = interestingEvent["affectedEntityDefinition"]["entityType"].ToString();
                                        if (entityTypeStringMapping.ContainsKey(entityType) == true)
                                        {
                                            eventRow.EntityType = entityTypeStringMapping[entityType];
                                        }
                                        else
                                        {
                                            eventRow.EntityType = entityType;
                                        }

                                        // Come up with links
                                        switch (entityType)
                                        {
                                            case ENTITY_TYPE_FLOWMAP_APPLICATION:
                                                eventRow.EntityLink = String.Format(DEEPLINK_APPLICATION, eventRow.Controller, eventRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_APPLICATION_MOBILE:
                                                eventRow.EntityLink = String.Format(DEEPLINK_APPLICATION_MOBILE, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_TIER:
                                                eventRow.EntityLink = String.Format(DEEPLINK_TIER, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_NODE:
                                                eventRow.EntityLink = String.Format(DEEPLINK_NODE, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION:
                                                eventRow.EntityLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_BACKEND:
                                                eventRow.EntityLink = String.Format(DEEPLINK_BACKEND, eventRow.Controller, eventRow.ApplicationID, eventRow.EntityID, DEEPLINK_THIS_TIMERANGE);
                                                break;

                                            default:
                                                logger.Warn("Unknown entity type {0} in affectedEntityDefinition in health rule violations", entityType);
                                                break;
                                        }
                                    }

                                    eventRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, eventRow.Controller, DEEPLINK_THIS_TIMERANGE);
                                    eventRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, eventRow.Controller, eventRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                    healthRuleViolationList.Add(eventRow);
                                }
                            }
                        }

                        loggerConsole.Info("{0} events", healthRuleViolationList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + healthRuleViolationList.Count;

                        // Sort them
                        healthRuleViolationList = healthRuleViolationList.OrderBy(o => o.HealthRuleName).ThenBy(o => o.From).ThenBy(o => o.Severity).ToList();

                        FileIOHelper.WriteListToCSVFile<HealthRuleViolationEvent>(healthRuleViolationList, new HealthRuleViolationEventReportMap(), FilePathMap.HealthRuleViolationsIndexFilePath(jobTarget));

                        #endregion

                        #region Events

                        loggerConsole.Info("Index events");

                        List<Event> eventsList = new List<Event>();
                        foreach (string eventType in EVENT_TYPES)
                        {
                            loggerConsole.Info("Type {0} events", eventType);

                            if (File.Exists(FilePathMap.EventsDataFilePath(jobTarget, eventType)))
                            {
                                JArray eventsInHour = FileIOHelper.LoadJArrayFromFile(FilePathMap.EventsDataFilePath(jobTarget, eventType));
                                if (eventsInHour != null)
                                {
                                    foreach (JObject interestingEvent in eventsInHour)
                                    {
                                        Event eventRow = new Event();
                                        eventRow.Controller = jobTarget.Controller;
                                        eventRow.ApplicationName = jobTarget.Application;
                                        eventRow.ApplicationID = jobTarget.ApplicationID;

                                        eventRow.EventID = (long)interestingEvent["id"];
                                        eventRow.OccurredUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)interestingEvent["eventTime"]);
                                        eventRow.Occurred = eventRow.OccurredUtc.ToLocalTime();
                                        eventRow.Type = interestingEvent["type"].ToString();
                                        eventRow.SubType = interestingEvent["subType"].ToString();
                                        eventRow.Severity = interestingEvent["severity"].ToString();
                                        eventRow.EventLink = interestingEvent["deepLinkUrl"].ToString();
                                        eventRow.Summary = interestingEvent["summary"].ToString();

                                        if (interestingEvent["triggeredEntity"].HasValues == true)
                                        {
                                            eventRow.TriggeredEntityID = (int)interestingEvent["triggeredEntity"]["entityId"];
                                            eventRow.TriggeredEntityName = interestingEvent["triggeredEntity"]["name"].ToString();
                                            string entityType = interestingEvent["triggeredEntity"]["entityType"].ToString();
                                            if (entityTypeStringMapping.ContainsKey(entityType) == true)
                                            {
                                                eventRow.TriggeredEntityType = entityTypeStringMapping[entityType];
                                            }
                                            else
                                            {
                                                eventRow.TriggeredEntityType = entityType;
                                            }
                                        }

                                        foreach (JObject affectedEntity in interestingEvent["affectedEntities"])
                                        {
                                            string entityType = affectedEntity["entityType"].ToString();
                                            switch (entityType)
                                            {
                                                case ENTITY_TYPE_FLOWMAP_APPLICATION:
                                                    // already have this data
                                                    break;

                                                case ENTITY_TYPE_FLOWMAP_TIER:
                                                    eventRow.TierID = (int)affectedEntity["entityId"];
                                                    eventRow.TierName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_FLOWMAP_NODE:
                                                    eventRow.NodeID = (int)affectedEntity["entityId"];
                                                    eventRow.NodeName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_FLOWMAP_MACHINE:
                                                    eventRow.MachineID = (int)affectedEntity["entityId"];
                                                    eventRow.MachineName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION:
                                                    eventRow.BTID = (int)affectedEntity["entityId"];
                                                    eventRow.BTName = affectedEntity["name"].ToString();
                                                    break;

                                                case ENTITY_TYPE_FLOWMAP_HEALTH_RULE:
                                                    eventRow.TriggeredEntityID = (int)affectedEntity["entityId"];
                                                    eventRow.TriggeredEntityType = entityTypeStringMapping[affectedEntity["entityType"].ToString()];
                                                    eventRow.TriggeredEntityName = affectedEntity["name"].ToString();
                                                    break;

                                                default:
                                                    logger.Warn("Unknown entity type {0} in affectedEntities in events", entityType);
                                                    break;
                                            }
                                        }

                                        eventRow.ControllerLink = String.Format(DEEPLINK_CONTROLLER, eventRow.Controller, DEEPLINK_THIS_TIMERANGE);
                                        eventRow.ApplicationLink = String.Format(DEEPLINK_APPLICATION, eventRow.Controller, eventRow.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                                        if (eventRow.TierID != 0)
                                        {
                                            eventRow.TierLink = String.Format(DEEPLINK_TIER, eventRow.Controller, eventRow.ApplicationID, eventRow.TierID, DEEPLINK_THIS_TIMERANGE);
                                        }
                                        if (eventRow.NodeID != 0)
                                        {
                                            eventRow.NodeLink = String.Format(DEEPLINK_NODE, eventRow.Controller, eventRow.ApplicationID, eventRow.NodeID, DEEPLINK_THIS_TIMERANGE);
                                        }
                                        if (eventRow.BTID != 0)
                                        {
                                            eventRow.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, eventRow.Controller, eventRow.ApplicationID, eventRow.BTID, DEEPLINK_THIS_TIMERANGE);
                                        }

                                        eventsList.Add(eventRow);
                                    }
                                }
                            }
                        }
                        loggerConsole.Info("{0} events", eventsList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + eventsList.Count;

                        // Sort them
                        eventsList = eventsList.OrderBy(o => o.Type).ThenBy(o => o.Occurred).ThenBy(o => o.Severity).ToList();

                        FileIOHelper.WriteListToCSVFile<Event>(eventsList, new EventReportMap(), FilePathMap.EventsIndexFilePath(jobTarget));

                        #endregion

                        #region Application

                        List<APMApplication> applicationList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.ApplicationIndexFilePath(jobTarget), new APMApplicationReportMap());
                        if (applicationList != null && applicationList.Count > 0)
                        {
                            APMApplication applicationsRow = applicationList[0];

                            applicationsRow.NumEvents = eventsList.Count;
                            applicationsRow.NumEventsError = eventsList.Count(e => e.Severity == "ERROR");
                            applicationsRow.NumEventsWarning = eventsList.Count(e => e.Severity == "WARN");
                            applicationsRow.NumEventsInfo = eventsList.Count(e => e.Severity == "INFO");

                            applicationsRow.NumHRViolations = healthRuleViolationList.Count;
                            applicationsRow.NumHRViolationsCritical = healthRuleViolationList.Count(e => e.Severity == "CRITICAL");
                            applicationsRow.NumHRViolationsWarning = healthRuleViolationList.Count(e => e.Severity == "WARNING");

                            applicationsRow.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                            applicationsRow.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                            applicationsRow.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                            applicationsRow.FromUtc = jobConfiguration.Input.TimeRange.From;
                            applicationsRow.ToUtc = jobConfiguration.Input.TimeRange.To;

                            if (applicationsRow.NumEvents > 0 || applicationsRow.NumHRViolations > 0)
                            {
                                applicationsRow.HasActivity = true;
                            }

                            FileIOHelper.WriteListToCSVFile(applicationList, new ApplicationEventReportMap(), FilePathMap.ApplicationEventsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.EventsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.EventsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.ApplicationEventsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationEventsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationEventsReportFilePath(), FilePathMap.ApplicationEventsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.HealthRuleViolationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.HealthRuleViolationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.HealthRuleViolationsReportFilePath(), FilePathMap.HealthRuleViolationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.EventsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.EventsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.EventsReportFilePath(), FilePathMap.EventsIndexFilePath(jobTarget));
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            loggerConsole.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            if (jobConfiguration.Input.Events == false)
            {
                loggerConsole.Trace("Skipping index of events");
            }
            return (jobConfiguration.Input.Events == true);
        }
    }
}
