using AppDynamics.Dexter.ReportObjectMaps;
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
    public class IndexControllerAuditEventsAndNotifications : JobStepIndexBase
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

                bool reportFolderCleaned = false;

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

                    stepTimingTarget.NumEntities = 0;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Prepare time variables

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        #endregion

                        #region Notification Events

                        loggerConsole.Info("Index Notification Events");

                        List<Event> eventsList = new List<Event>();
                        JObject notificationsContainer = FileIOHelper.LoadJObjectFromFile(FilePathMap.NotificationsDataFilePath(jobTarget));
                        if (notificationsContainer != null)
                        {
                            if (isTokenPropertyNull(notificationsContainer, "notifications") == false)
                            {
                                foreach (JObject interestingEvent in notificationsContainer["notifications"])
                                {
                                    if (isTokenPropertyNull(interestingEvent, "notificationData") == false &&
                                        isTokenPropertyNull(interestingEvent["notificationData"], "affectedEntities") == false)
                                    {
                                        foreach (JObject affectedEntity in interestingEvent["notificationData"]["affectedEntities"])
                                        {
                                            Event @event = new Event();
                                            @event.Controller = jobTarget.Controller;

                                            @event.EventID = getLongValueFromJToken(interestingEvent, "id");
                                            @event.OccurredUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(interestingEvent["notificationData"], "time"));
                                            try { @event.Occurred = @event.OccurredUtc.ToLocalTime();} catch { }

                                            @event.Type = getStringValueFromJToken(interestingEvent["notificationData"], "eventType");
                                            @event.Severity = getStringValueFromJToken(interestingEvent["notificationData"], "severity");
                                            @event.Summary = getStringValueFromJToken(interestingEvent["notificationData"], "summary");

                                            @event.TriggeredEntityID = getLongValueFromJToken(affectedEntity, "entityId");
                                            @event.TriggeredEntityType = getStringValueFromJToken(affectedEntity, "entityType");

                                            @event.ApplicationID = getLongValueFromJToken(interestingEvent["notificationData"], "applicationId");
                                            @event.TierID = getLongValueFromJToken(interestingEvent["notificationData"], "applicationComponentId");
                                            @event.NodeID = getLongValueFromJToken(interestingEvent["notificationData"], "applicationComponentNodeId");
                                            @event.BTID = getLongValueFromJToken(interestingEvent["notificationData"], "businessTransactionId");

                                            @event.ControllerLink = String.Format(DEEPLINK_CONTROLLER, @event.Controller, DEEPLINK_THIS_TIMERANGE);

                                            eventsList.Add(@event);
                                        }
                                    }
                                }
                            }
                        }

                        loggerConsole.Info("{0} Notification Events", eventsList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + eventsList.Count;

                        // Sort them
                        eventsList = eventsList.OrderBy(o => o.Type).ThenBy(o => o.Occurred).ThenBy(o => o.Severity).ToList();
                        FileIOHelper.WriteListToCSVFile<Event>(eventsList, new EventReportMap(), FilePathMap.NotificationsIndexFilePath(jobTarget));

                        #endregion

                        #region Audit Events

                        loggerConsole.Info("Index Audit Log Events");

                        List<AuditEvent> auditEventsList = new List<AuditEvent>();
                        JArray auditEvents = FileIOHelper.LoadJArrayFromFile(FilePathMap.AuditEventsDataFilePath(jobTarget));
                        if (auditEvents != null)
                        {
                            foreach (JObject interestingEvent in auditEvents)
                            {
                                AuditEvent @event = new AuditEvent();

                                @event.Controller = jobTarget.Controller;

                                @event.EntityID = getLongValueFromJToken(interestingEvent, "objectId");
                                @event.EntityType = getStringValueFromJToken(interestingEvent, "objectType");
                                @event.EntityName = getStringValueFromJToken(interestingEvent, "objectName");

                                @event.UserName = getStringValueFromJToken(interestingEvent, "userName");
                                @event.AccountName = getStringValueFromJToken(interestingEvent, "accountName");
                                @event.LoginType = getStringValueFromJToken(interestingEvent, "securityProviderType");

                                @event.Action = getStringValueFromJToken(interestingEvent, "action");
                                @event.EntityID = getLongValueFromJToken(interestingEvent, "objectId");
                                @event.EntityType = getStringValueFromJToken(interestingEvent, "objectType");
                                @event.EntityName = getStringValueFromJToken(interestingEvent, "objectName");

                                @event.OccurredUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(interestingEvent, "timeStamp"));
                                try { @event.Occurred = @event.OccurredUtc.ToLocalTime(); } catch { }

                                auditEventsList.Add(@event);
                            }
                        }

                        loggerConsole.Info("{0} Audit Events", auditEventsList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + auditEventsList.Count;

                        // Sort them
                        auditEventsList = auditEventsList.OrderBy(o => o.Occurred).ToList();
                        FileIOHelper.WriteListToCSVFile<AuditEvent>(auditEventsList, new AuditEventReportMap(), FilePathMap.AuditEventsIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ControllerEventsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ControllerEventsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.NotificationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.NotificationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.NotificationsReportFilePath(), FilePathMap.NotificationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.AuditEventsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.AuditEventsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.AuditEventsReportFilePath(), FilePathMap.AuditEventsIndexFilePath(jobTarget));
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
