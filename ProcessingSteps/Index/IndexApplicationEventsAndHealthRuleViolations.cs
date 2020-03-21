using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexApplicationEventsAndHealthRuleViolations : JobStepIndexBase
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

                List<JobTarget> listOfTargetsAlreadyProcessed = new List<JobTarget>(jobConfiguration.Target.Count);

                bool reportFolderCleaned = false;
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

                        if (listOfTargetsAlreadyProcessed.Count(j => (j.Controller == jobTarget.Controller) && (j.ApplicationID == jobTarget.ApplicationID)) > 0)
                        {
                            // Already saw this target, like an APM and WEB pairs together
                            continue;
                        }

                        // For databases, we only process this once for the first collector we've seen
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
                        listOfTargetsAlreadyProcessed.Add(jobTarget);

                        #region Prepare time variables

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        #endregion

                        #region Health Rule violations

                        List<HealthRuleViolationEvent> healthRuleViolationList = new List<HealthRuleViolationEvent>();

                        loggerConsole.Info("Index Health Rule Violations");

                        JArray healthRuleViolationsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ApplicationHealthRuleViolationsDataFilePath(jobTarget));
                        if (healthRuleViolationsArray != null)
                        {
                            foreach (JObject interestingEventObject in healthRuleViolationsArray)
                            {
                                HealthRuleViolationEvent healthRuleViolationEvent = new HealthRuleViolationEvent();
                                healthRuleViolationEvent.Controller = jobTarget.Controller;
                                healthRuleViolationEvent.ApplicationName = jobTarget.Application;
                                healthRuleViolationEvent.ApplicationID = jobTarget.ApplicationID;

                                healthRuleViolationEvent.EventID = getLongValueFromJToken(interestingEventObject, "id");
                                healthRuleViolationEvent.FromUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(interestingEventObject, "startTimeInMillis"));
                                healthRuleViolationEvent.From = healthRuleViolationEvent.FromUtc.ToLocalTime();
                                if (getLongValueFromJToken(interestingEventObject, "endTimeInMillis") > 0)
                                {
                                    healthRuleViolationEvent.ToUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(interestingEventObject, "endTimeInMillis"));
                                    healthRuleViolationEvent.To = healthRuleViolationEvent.FromUtc.ToLocalTime();
                                }
                                healthRuleViolationEvent.Status = getStringValueFromJToken(interestingEventObject, "incidentStatus");
                                healthRuleViolationEvent.Severity = getStringValueFromJToken(interestingEventObject, "severity");
                                healthRuleViolationEvent.EventLink = String.Format(DEEPLINK_INCIDENT, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.EventID, getStringValueFromJToken(interestingEventObject, "startTimeInMillis"), DEEPLINK_THIS_TIMERANGE); ;

                                healthRuleViolationEvent.Description = getStringValueFromJToken(interestingEventObject, "description");

                                if (isTokenPropertyNull(interestingEventObject, "triggeredEntityDefinition") == false)
                                {
                                    healthRuleViolationEvent.HealthRuleID = getLongValueFromJToken(interestingEventObject["triggeredEntityDefinition"], "entityId");
                                    healthRuleViolationEvent.HealthRuleName = getStringValueFromJToken(interestingEventObject["triggeredEntityDefinition"], "name");
                                    // TODO the health rule can't be hotlinked to until platform rewrites the screen that opens from Flash
                                    healthRuleViolationEvent.HealthRuleLink = String.Format(DEEPLINK_HEALTH_RULE, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.HealthRuleID, DEEPLINK_THIS_TIMERANGE);
                                }

                                if (isTokenPropertyNull(interestingEventObject, "affectedEntityDefinition") == false)
                                {
                                    healthRuleViolationEvent.EntityID = getIntValueFromJToken(interestingEventObject["affectedEntityDefinition"], "entityId");
                                    healthRuleViolationEvent.EntityName = getStringValueFromJToken(interestingEventObject["affectedEntityDefinition"], "name");

                                    string entityType = getStringValueFromJToken(interestingEventObject["affectedEntityDefinition"], "entityType");
                                    if (entityTypeStringMapping.ContainsKey(entityType) == true)
                                    {
                                        healthRuleViolationEvent.EntityType = entityTypeStringMapping[entityType];
                                    }
                                    else
                                    {
                                        healthRuleViolationEvent.EntityType = entityType;
                                    }

                                    // Come up with links
                                    switch (entityType)
                                    {
                                        case ENTITY_TYPE_FLOWMAP_APPLICATION:
                                            healthRuleViolationEvent.EntityLink = String.Format(DEEPLINK_APM_APPLICATION, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                                            break;

                                        case ENTITY_TYPE_FLOWMAP_APPLICATION_MOBILE:
                                            healthRuleViolationEvent.EntityLink = String.Format(DEEPLINK_APPLICATION_MOBILE, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.EntityID, DEEPLINK_THIS_TIMERANGE);
                                            break;

                                        case ENTITY_TYPE_FLOWMAP_TIER:
                                            healthRuleViolationEvent.EntityLink = String.Format(DEEPLINK_TIER, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.EntityID, DEEPLINK_THIS_TIMERANGE);
                                            break;

                                        case ENTITY_TYPE_FLOWMAP_NODE:
                                            healthRuleViolationEvent.EntityLink = String.Format(DEEPLINK_NODE, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.EntityID, DEEPLINK_THIS_TIMERANGE);
                                            break;

                                        case ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION:
                                            healthRuleViolationEvent.EntityLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.EntityID, DEEPLINK_THIS_TIMERANGE);
                                            break;

                                        case ENTITY_TYPE_FLOWMAP_BACKEND:
                                            healthRuleViolationEvent.EntityLink = String.Format(DEEPLINK_BACKEND, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, healthRuleViolationEvent.EntityID, DEEPLINK_THIS_TIMERANGE);
                                            break;

                                        default:
                                            logger.Warn("Unknown entity type {0} in affectedEntityDefinition in health rule violations", entityType);
                                            break;
                                    }
                                }

                                healthRuleViolationEvent.ControllerLink = String.Format(DEEPLINK_CONTROLLER, healthRuleViolationEvent.Controller, DEEPLINK_THIS_TIMERANGE);
                                healthRuleViolationEvent.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, healthRuleViolationEvent.Controller, healthRuleViolationEvent.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                healthRuleViolationList.Add(healthRuleViolationEvent);
                            }
                        }

                        loggerConsole.Info("{0} Health Rule Violations", healthRuleViolationList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + healthRuleViolationList.Count;

                        // Sort them
                        healthRuleViolationList = healthRuleViolationList.OrderBy(o => o.HealthRuleName).ThenBy(o => o.From).ThenBy(o => o.Severity).ToList();
                        FileIOHelper.WriteListToCSVFile<HealthRuleViolationEvent>(healthRuleViolationList, new HealthRuleViolationEventReportMap(), FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget));

                        #endregion

                        #region Events

                        List<Event> eventsList = new List<Event>();
                        List<EventDetail> eventDetailsList = new List<EventDetail>();

                        loggerConsole.Info("Index Events");

                        foreach (string eventType in EVENT_TYPES)
                        {
                            JArray eventsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ApplicationEventsWithDetailsDataFilePath(jobTarget, eventType));
                            if (eventsArray == null) eventsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ApplicationEventsDataFilePath(jobTarget, eventType));
                            if (eventsArray != null)
                            {
                                loggerConsole.Info("{0} Events", eventType);

                                foreach (JObject interestingEventObject in eventsArray)
                                {
                                    Event @event = new Event();
                                    @event.Controller = jobTarget.Controller;
                                    @event.ApplicationName = jobTarget.Application;
                                    @event.ApplicationID = jobTarget.ApplicationID;

                                    @event.EventID = getLongValueFromJToken(interestingEventObject, "id");
                                    @event.OccurredUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(interestingEventObject, "eventTime"));
                                    @event.Occurred = @event.OccurredUtc.ToLocalTime();
                                    @event.Type = getStringValueFromJToken(interestingEventObject, "type");
                                    @event.SubType = getStringValueFromJToken(interestingEventObject, "subType");
                                    @event.Severity = getStringValueFromJToken(interestingEventObject, "severity");
                                    @event.EventLink = getStringValueFromJToken(interestingEventObject, "deepLinkUrl");
                                    @event.Summary = getStringValueFromJToken(interestingEventObject, "summary");

                                    if (isTokenPropertyNull(interestingEventObject, "triggeredEntity") == false)
                                    {
                                        @event.TriggeredEntityID = getLongValueFromJToken(interestingEventObject["triggeredEntity"], "entityId");
                                        @event.TriggeredEntityName = getStringValueFromJToken(interestingEventObject["triggeredEntity"], "name");
                                        string entityType = getStringValueFromJToken(interestingEventObject["triggeredEntity"], "entityType");
                                        if (entityTypeStringMapping.ContainsKey(entityType) == true)
                                        {
                                            @event.TriggeredEntityType = entityTypeStringMapping[entityType];
                                        }
                                        else
                                        {
                                            @event.TriggeredEntityType = entityType;
                                        }
                                    }

                                    foreach (JObject affectedEntity in interestingEventObject["affectedEntities"])
                                    {
                                        string entityType = getStringValueFromJToken(affectedEntity, "entityType");
                                        switch (entityType)
                                        {
                                            case ENTITY_TYPE_FLOWMAP_APPLICATION:
                                                // already have this data
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_TIER:
                                                @event.TierID = getIntValueFromJToken(affectedEntity, "entityId");
                                                @event.TierName = getStringValueFromJToken(affectedEntity, "name");
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_NODE:
                                                @event.NodeID = getIntValueFromJToken(affectedEntity, "entityId");
                                                @event.NodeName = getStringValueFromJToken(affectedEntity, "name");
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_MACHINE:
                                                @event.MachineID = getIntValueFromJToken(affectedEntity, "entityId");
                                                @event.MachineName = getStringValueFromJToken(affectedEntity, "name");
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_BUSINESS_TRANSACTION:
                                                @event.BTID = getIntValueFromJToken(affectedEntity, "entityId");
                                                @event.BTName = getStringValueFromJToken(affectedEntity, "name");
                                                break;

                                            case ENTITY_TYPE_FLOWMAP_HEALTH_RULE:
                                                @event.TriggeredEntityID = getLongValueFromJToken(affectedEntity, "entityId");
                                                @event.TriggeredEntityType = entityTypeStringMapping[getStringValueFromJToken(affectedEntity, "entityType")];
                                                @event.TriggeredEntityName = getStringValueFromJToken(affectedEntity, "name");
                                                break;

                                            default:
                                                logger.Warn("Unknown entity type {0} in affectedEntities in events", entityType);
                                                break;
                                        }
                                    }

                                    @event.ControllerLink = String.Format(DEEPLINK_CONTROLLER, @event.Controller, DEEPLINK_THIS_TIMERANGE);
                                    @event.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, @event.Controller, @event.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                                    if (@event.TierID != 0)
                                    {
                                        @event.TierLink = String.Format(DEEPLINK_TIER, @event.Controller, @event.ApplicationID, @event.TierID, DEEPLINK_THIS_TIMERANGE);
                                    }
                                    if (@event.NodeID != 0)
                                    {
                                        @event.NodeLink = String.Format(DEEPLINK_NODE, @event.Controller, @event.ApplicationID, @event.NodeID, DEEPLINK_THIS_TIMERANGE);
                                    }
                                    if (@event.BTID != 0)
                                    {
                                        @event.BTLink = String.Format(DEEPLINK_BUSINESS_TRANSACTION, @event.Controller, @event.ApplicationID, @event.BTID, DEEPLINK_THIS_TIMERANGE);
                                    }

                                    if (isTokenPropertyNull(interestingEventObject, "details") == false &&
                                        isTokenPropertyNull(interestingEventObject["details"], "eventDetails") == false)
                                    {
                                        JArray eventDetailsArray = (JArray)interestingEventObject["details"]["eventDetails"];
                                        if (eventDetailsArray != null)
                                        {
                                            List<EventDetail> eventDetailsForThisEventList = new List<EventDetail>(eventDetailsArray.Count);

                                            foreach (JObject eventDetailObject in eventDetailsArray)
                                            {
                                                EventDetail eventDetail = new EventDetail();

                                                eventDetail.Controller = @event.Controller;
                                                eventDetail.ApplicationName = @event.ApplicationName;
                                                eventDetail.ApplicationID = @event.ApplicationID;
                                                eventDetail.TierName = @event.TierName;
                                                eventDetail.TierID = @event.TierID;
                                                eventDetail.NodeName = @event.NodeName;
                                                eventDetail.NodeID = @event.NodeID;
                                                eventDetail.MachineName = @event.MachineName;
                                                eventDetail.MachineID = @event.MachineID;
                                                eventDetail.BTName = @event.BTName;
                                                eventDetail.BTID = @event.BTID ;

                                                eventDetail.EventID = @event.EventID;
                                                eventDetail.OccurredUtc = @event.OccurredUtc;
                                                eventDetail.Occurred = @event.Occurred;
                                                eventDetail.Type = @event.Type;
                                                eventDetail.SubType = @event.SubType;
                                                eventDetail.Severity = @event.Severity;
                                                eventDetail.Summary = @event.Summary;

                                                eventDetail.DetailName = getStringValueFromJToken(eventDetailObject, "name");
                                                eventDetail.DetailValue = getStringValueFromJToken(eventDetailObject, "value");

                                                // Parse the special types of the event types, such as options and envinronment variable changes
                                                bool shouldContinue = true;   
                                                switch (@event.Type)
                                                {
                                                    #region APPLICATION_CONFIG_CHANGE

                                                    case "APPLICATION_CONFIG_CHANGE":
                                                        if (shouldContinue == true)
                                                        {
                                                            // Added Option 1
                                                            // Removed Option 1
                                                            Regex regex = new Regex(@"(.*\sOption)\s\d+", RegexOptions.IgnoreCase);
                                                            Match match = regex.Match(eventDetail.DetailName);
                                                            if (match != null && match.Groups.Count == 2)
                                                            {
                                                                eventDetail.DetailAction = match.Groups[1].Value;
                                                                //-Dgw.cc.full.upgrade.intended.date=20191112
                                                                //-XX:+UseGCLogFileRotation 
                                                                //-XX:NumberOfGCLogFiles=< number of log files > 
                                                                //-XX:GCLogFileSize=< file size >[ unit ]
                                                                //-Xloggc:/path/to/gc.log                
                                                                parseJavaStartupOptionIntoEventDetail(eventDetail);
                                                                shouldContinue = false;
                                                            }
                                                        }

                                                        if (shouldContinue == true)
                                                        {
                                                            // Added Variable 1:
                                                            // Removed Variable 1:
                                                            Regex regex = new Regex(@"((Added|Removed) Variable)\s\d+", RegexOptions.IgnoreCase);
                                                            Match match = regex.Match(eventDetail.DetailName);
                                                            if (match != null && match.Groups.Count == 3)
                                                            {
                                                                eventDetail.DetailAction = match.Groups[1].Value;
                                                                // SSH_TTY=/dev/pts/0
                                                                // HOSTNAME=felvqcap1174.farmersinsurance.com
                                                                parseEnvironmentVariableIntoEventDetail(eventDetail);
                                                                shouldContinue = false;
                                                            }
                                                        }

                                                        if (shouldContinue == true)
                                                        {
                                                            // Modified Variable 1:
                                                            Regex regex = new Regex(@"(Modified Variable)\s\d+", RegexOptions.IgnoreCase);
                                                            Match match = regex.Match(eventDetail.DetailName);
                                                            if (match != null && match.Groups.Count == 2)
                                                            {
                                                                eventDetail.DetailAction = match.Groups[1].Value;
                                                                // MAIL=/var/mail/jbossapp (changed to) MAIL=/var/spool/mail/jbossapp
                                                                // SHLVL=5 (changed to) SHLVL=3
                                                                parseModifiedEnvironmentVariableIntoEventDetail(eventDetail);
                                                                shouldContinue = false;
                                                            }
                                                        }

                                                        if (shouldContinue == true)
                                                        {
                                                            // Modified Property 2:
                                                            Regex regex = new Regex(@"(Modified Property)\s\d+", RegexOptions.IgnoreCase);
                                                            Match match = regex.Match(eventDetail.DetailName);
                                                            if (match != null && match.Groups.Count == 2)
                                                            {
                                                                eventDetail.DetailAction = match.Groups[1].Value;
                                                                // gw.cc.full.upgrade.intended.date=20191112 (changed to) gw.cc.full.upgrade.intended.date=20191113
                                                                // user.dir=/jboss/scripts/jenkins/28/slave/workspace/ClaimsCenter/GWCC_GW_DB_APP_DP/perfcc06post (changed to) user.dir=/jboss/scripts/ccperf06
                                                                parseModifiedPropertyIntoEventDetail(eventDetail);
                                                                shouldContinue = false;
                                                            }
                                                        }

                                                        break;

                                                    #endregion

                                                    #region POLICY_***

                                                    case "POLICY_OPEN_WARNING":
                                                    case "POLICY_OPEN_CRITICAL":
                                                    case "POLICY_CLOSE_WARNING":
                                                    case "POLICY_CLOSE_CRITICAL":
                                                    case "POLICY_UPGRADED":
                                                    case "POLICY_DOWNGRADED":
                                                    case "POLICY_CANCELED_WARNING":
                                                    case "POLICY_CANCELED_CRITICAL":
                                                    case "POLICY_CONTINUES_CRITICAL":
                                                    case "POLICY_CONTINUES_WARNING":
                                                        if (eventDetail.DetailName == "Evaluation End Time" ||
                                                            eventDetail.DetailName == "Evaluation Start Time")
                                                        {
                                                            // These are a unix datetime value
                                                            DateTime dateTimeVal1 = UnixTimeHelper.ConvertFromUnixTimestamp(Convert.ToInt64(eventDetail.DetailValue));
                                                            eventDetail.DataType = "DateTime";
                                                            eventDetail.DetailValue = dateTimeVal1.ToLocalTime().ToString("G");
                                                        }
                                                        break;

                                                    #endregion

                                                    default:
                                                        break;
                                                }

                                                if (eventDetail.DataType == null || eventDetail.DataType.Length == 0)
                                                {
                                                    // Get datatype of value
                                                    long longVal = 0;
                                                    double doubleVal = 0;
                                                    DateTime dateTimeVal = DateTime.MinValue;
                                                    if (Int64.TryParse(eventDetail.DetailValue, out longVal) == true)
                                                    {
                                                        eventDetail.DataType = "Integer";
                                                    }
                                                    else if (Double.TryParse(eventDetail.DetailValue, out doubleVal) == true)
                                                    {
                                                        eventDetail.DataType = "Double";
                                                    }
                                                    else if (DateTime.TryParse(eventDetail.DetailValue, out dateTimeVal) == true)
                                                    {
                                                        eventDetail.DataType = "DateTime";
                                                    }
                                                    else
                                                    {
                                                        eventDetail.DataType = "String";
                                                    }
                                                }

                                                eventDetailsForThisEventList.Add(eventDetail);
                                            }
                                            @event.NumDetails = eventDetailsForThisEventList.Count;

                                            // Sort them
                                            eventDetailsForThisEventList = eventDetailsForThisEventList.OrderBy(o => o.DetailName).ToList();

                                            eventDetailsList.AddRange(eventDetailsForThisEventList);
                                        }
                                    }

                                    eventsList.Add(@event);
                                }
                            }
                        }

                        loggerConsole.Info("{0} Events", eventsList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + eventsList.Count;

                        // Sort them
                        eventsList = eventsList.OrderBy(o => o.Type).ThenBy(o => o.Occurred).ThenBy(o => o.Severity).ToList();
                        FileIOHelper.WriteListToCSVFile<Event>(eventsList, new EventReportMap(), FilePathMap.ApplicationEventsIndexFilePath(jobTarget));

                        FileIOHelper.WriteListToCSVFile<EventDetail>(eventDetailsList, new EventDetailReportMap(), FilePathMap.ApplicationEventDetailsIndexFilePath(jobTarget));

                        #endregion

                        #region Application

                        ApplicationEventSummary application = new ApplicationEventSummary();

                        application.Controller = jobTarget.Controller;
                        application.ApplicationName = jobTarget.Application;
                        application.ApplicationID = jobTarget.ApplicationID;
                        application.Type = jobTarget.Type;

                        application.NumEvents = eventsList.Count;
                        application.NumEventsError = eventsList.Count(e => e.Severity == "ERROR");
                        application.NumEventsWarning = eventsList.Count(e => e.Severity == "WARN");
                        application.NumEventsInfo = eventsList.Count(e => e.Severity == "INFO");

                        application.NumHRViolations = healthRuleViolationList.Count;
                        application.NumHRViolationsCritical = healthRuleViolationList.Count(e => e.Severity == "CRITICAL");
                        application.NumHRViolationsWarning = healthRuleViolationList.Count(e => e.Severity == "WARNING");

                        application.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                        application.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                        application.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                        application.FromUtc = jobConfiguration.Input.TimeRange.From;
                        application.ToUtc = jobConfiguration.Input.TimeRange.To;

                        // Determine what kind of entity we are dealing with and adjust accordingly
                        application.ControllerLink = String.Format(DEEPLINK_CONTROLLER, application.Controller, DEEPLINK_THIS_TIMERANGE);
                        application.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, application.Controller, application.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                        if (application.NumEvents > 0 || application.NumHRViolations > 0)
                        {
                            application.HasActivity = true;
                        }

                        List<ApplicationEventSummary> applicationList = new List<ApplicationEventSummary>(1);
                        applicationList.Add(application);
                        FileIOHelper.WriteListToCSVFile(applicationList, new ApplicationEventSummaryReportMap(), FilePathMap.ApplicationEventsSummaryIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ApplicationEventsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ApplicationEventsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationHealthRuleViolationsReportFilePath(), FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationEventsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationEventsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationEventsReportFilePath(), FilePathMap.ApplicationEventsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationEventDetailsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationEventDetailsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationEventDetailsReportFilePath(), FilePathMap.ApplicationEventDetailsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationEventsSummaryIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationEventsSummaryIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationEventsSummaryReportFilePath(), FilePathMap.ApplicationEventsSummaryIndexFilePath(jobTarget));
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
                loggerConsole.Trace("Skipping index of events");
            }
            return (jobConfiguration.Input.Events == true);
        }

        private void parseJavaStartupOptionIntoEventDetail(EventDetail eventDetail)
        {
            //-Dgw.cc.full.upgrade.intended.date=20191112
            //-XX:+UseGCLogFileRotation 
            //-XX:NumberOfGCLogFiles=< number of log files > 
            //-XX:GCLogFileSize=< file size >[ unit ]
            //-Xloggc:/path/to/gc.log                
            Regex regexOptionValue = new Regex(@"-(.*)(:|=)(.*)", RegexOptions.IgnoreCase);
            Match match = regexOptionValue.Match(eventDetail.DetailValue);
            if (match != null && match.Groups.Count == 4)
            {
                eventDetail.DetailName = match.Groups[1].Value;
                eventDetail.DetailValue = match.Groups[3].Value;
            }
            else
            {
                // -Xmx<heap size>[unit]
                eventDetail.DetailName = eventDetail.DetailValue.TrimStart('-');
                eventDetail.DetailValue = "";
            }
        }

        private void parseEnvironmentVariableIntoEventDetail(EventDetail eventDetail)
        {
            // SSH_TTY=/dev/pts/0
            // HOSTNAME=felvqcap1174.farmersinsurance.com
            int indexOfEqualSign = eventDetail.DetailValue.IndexOf('=');
            if (indexOfEqualSign >= 0)
            {
                eventDetail.DetailName = eventDetail.DetailValue.Substring(0, indexOfEqualSign);
                eventDetail.DetailValue = eventDetail.DetailValue.Substring(indexOfEqualSign).TrimStart('=');
            }
        }

        private void parseModifiedEnvironmentVariableIntoEventDetail(EventDetail eventDetail)
        {
            // MAIL=/var/mail/jbossapp (changed to) MAIL=/var/spool/mail/jbossapp
            // SHLVL=5 (changed to) SHLVL=3
            Regex regexOptionValue = new Regex(@"(.*)(\s\(changed to\)\s)(.*)", RegexOptions.IgnoreCase);
            Match match = regexOptionValue.Match(eventDetail.DetailValue);
            if (match != null && match.Groups.Count == 4)
            {
                string envVariableBefore = match.Groups[1].Value;
                int indexOfEqualSign = envVariableBefore.IndexOf('=');
                if (indexOfEqualSign >= 0)
                {
                    eventDetail.DetailName = envVariableBefore.Substring(0, indexOfEqualSign);
                    eventDetail.DetailValueOld = envVariableBefore.Substring(indexOfEqualSign).TrimStart('=');
                }

                string envVariableAfter = match.Groups[3].Value;
                indexOfEqualSign = envVariableAfter.IndexOf('=');
                if (indexOfEqualSign >= 0)
                {
                    eventDetail.DetailValue = envVariableAfter.Substring(indexOfEqualSign).TrimStart('=');
                }
            }
        }

        private void parseModifiedPropertyIntoEventDetail(EventDetail eventDetail)
        {
            // gw.cc.full.upgrade.intended.date=20191112 (changed to) gw.cc.full.upgrade.intended.date=20191113
            // user.dir=/jboss/scripts/jenkins/28/slave/workspace/ClaimsCenter/GWCC_GW_DB_APP_DP/perfcc06post (changed to) user.dir=/jboss/scripts/ccperf06
            Regex regexOptionValue = new Regex(@"(.*)(\s\(changed to\)\s)(.*)", RegexOptions.IgnoreCase);
            Match match = regexOptionValue.Match(eventDetail.DetailValue);
            if (match != null && match.Groups.Count == 4)
            {
                string envVariableBefore = match.Groups[1].Value;
                int indexOfEqualSign = envVariableBefore.IndexOf('=');
                if (indexOfEqualSign >= 0)
                {
                    eventDetail.DetailName = envVariableBefore.Substring(0, indexOfEqualSign);
                    eventDetail.DetailValueOld = envVariableBefore.Substring(indexOfEqualSign).TrimStart('=');
                }

                string envVariableAfter = match.Groups[3].Value;
                indexOfEqualSign = envVariableAfter.IndexOf('=');
                if (indexOfEqualSign >= 0)
                {
                    eventDetail.DetailValue = envVariableAfter.Substring(indexOfEqualSign).TrimStart('=');
                }
            }
        }
    }
}
