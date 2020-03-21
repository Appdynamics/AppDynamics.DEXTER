using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexApplicationHealthRulesAlertsPolicies : JobStepIndexBase
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

                #region Template comparisons 

                // Check to see if the reference application is the template or specific application, and add one of them to the 
                if (jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller == BLANK_APPLICATION_CONTROLLER &&
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application == BLANK_APPLICATION_APM)
                {
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                }
                else
                {
                    // Check if there is a valid reference application
                    JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t =>
                        t.Type == APPLICATION_TYPE_APM &&
                        String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
                    if (jobTargetReferenceApp == null)
                    {
                        // No valid reference, fall back to comparing against template
                        logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                        loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);

                        jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller = BLANK_APPLICATION_CONTROLLER;
                        jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application = BLANK_APPLICATION_APM;
                        jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Type = APPLICATION_TYPE_APM;

                        jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                    }
                }

                // Check to see if the reference application is the template or specific application, and add one of them to the 
                if (jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller == BLANK_APPLICATION_CONTROLLER &&
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Application == BLANK_APPLICATION_WEB)
                {
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceWEB);
                }
                else
                {
                    // Check if there is a valid reference application
                    JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t =>
                        t.Type == APPLICATION_TYPE_WEB &&
                        String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Application, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
                    if (jobTargetReferenceApp == null)
                    {
                        // No valid reference, fall back to comparing against template
                        logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceWEB);
                        loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceWEB);

                        jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller = BLANK_APPLICATION_CONTROLLER;
                        jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Application = BLANK_APPLICATION_WEB;
                        jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Type = APPLICATION_TYPE_WEB;

                        jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceWEB);
                    }
                }

                // Check to see if the reference application is the template or specific application, and add one of them to the 
                if (jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller == BLANK_APPLICATION_CONTROLLER &&
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Application == BLANK_APPLICATION_MOBILE)
                {
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE);
                }
                else
                {
                    // Check if there is a valid reference application
                    JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t =>
                        t.Type == APPLICATION_TYPE_MOBILE &&
                        String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Application, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
                    if (jobTargetReferenceApp == null)
                    {
                        // No valid reference, fall back to comparing against template
                        logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE);
                        loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE);

                        jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller = BLANK_APPLICATION_CONTROLLER;
                        jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Application = BLANK_APPLICATION_MOBILE;
                        jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Type = APPLICATION_TYPE_MOBILE;

                        jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE);
                    }
                }

                // Check to see if the reference application is the template or specific application, and add one of them to the 
                if (jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller == BLANK_APPLICATION_CONTROLLER &&
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Application == BLANK_APPLICATION_DB)
                {
                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceDB);
                }
                else
                {
                    // Check if there is a valid reference application
                    JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t =>
                        t.Type == APPLICATION_TYPE_DB &&
                        String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                        String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceDB.Application, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
                    if (jobTargetReferenceApp == null)
                    {
                        // No valid reference, fall back to comparing against template
                        logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceDB);
                        loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceDB);

                        jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller = BLANK_APPLICATION_CONTROLLER;
                        jobConfiguration.Input.ConfigurationComparisonReferenceDB.Application = BLANK_APPLICATION_DB;
                        jobConfiguration.Input.ConfigurationComparisonReferenceDB.Type = APPLICATION_TYPE_DB;

                        jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceDB);
                    }
                }

                #endregion

                List<JobTarget> listOfTargetsAlreadyProcessed = new List<JobTarget>(jobConfiguration.Target.Count);

                bool reportFolderCleaned = false;

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
                    stepTimingTarget.NumEntities = 0;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        if (listOfTargetsAlreadyProcessed.Count(j => (j.Controller == jobTarget.Controller) && (j.ApplicationID == jobTarget.ApplicationID) && (jobTarget.ApplicationID > 0)) > 0)
                        {
                            // Already saw this target, like an APM and WEB pairs together
                            continue;
                        }
                        else
                        {
                            if (jobTarget.ParentApplicationID != 0)
                            {
                                if (listOfTargetsAlreadyProcessed.Count(j => (j.Controller == jobTarget.Controller) && (j.ApplicationID == jobTarget.ParentApplicationID)) > 0)
                                {
                                    // Already saw this target, like an APM and WEB pairs together
                                    continue;
                                }
                            }
                        }

                        // For databases, we only process this once for the first collector we've seen
                        if (jobTarget.Type == APPLICATION_TYPE_DB)
                        {
                            if (listOfTargetsAlreadyProcessed.Count(j => (j.Controller == jobTarget.Controller) && (j.Type == APPLICATION_TYPE_DB)) > 0)
                            {
                                // Already saw this target, DB Collectors for controller are all part of the same thing
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

                        #region Application

                        ApplicationConfigurationPolicy applicationConfiguration = new ApplicationConfigurationPolicy();
                        applicationConfiguration.Controller = jobTarget.Controller;
                        applicationConfiguration.ControllerLink = String.Format(DEEPLINK_CONTROLLER, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ApplicationName = jobTarget.Application;
                        applicationConfiguration.ApplicationID = jobTarget.ApplicationID;
                        applicationConfiguration.Type = jobTarget.Type;
                        applicationConfiguration.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                        applicationConfiguration.HealthRulesLink = String.Format(DEEPLINK_HEALTH_RULES, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_THIS_TIMERANGE);
                        applicationConfiguration.PoliciesLink = String.Format(DEEPLINK_POLICIES_RULES, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ActionsLink = String.Format(DEEPLINK_ACTIONS_RULES, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                        #endregion

                        #region Health Rules

                        List<HealthRule> healthRulesList = new List<HealthRule>();
                        XmlDocument configXml = FileIOHelper.LoadXmlDocumentFromFile(FilePathMap.ApplicationHealthRulesDataFilePath(jobTarget));
                        JArray healthRulesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.ApplicationHealthRulesDetailsDataFilePath(jobTarget));
                        if (configXml != null)
                        {
                            loggerConsole.Info("Index Health Rules");

                            // Application level
                            // application
                            //      configuration
                            //          call-graph
                            foreach (XmlNode healthRuleConfigurationNode in configXml.SelectNodes("health-rules/health-rule"))
                            {
                                HealthRule healthRule = fillHealthRule(healthRuleConfigurationNode, applicationConfiguration);

                                // Look up IDs of the Health Rule
                                if (healthRulesArray != null)
                                {
                                    JObject healthRuleObject = (JObject)healthRulesArray.Where(h => h["type"].ToString() == healthRule.HRRuleType && h["name"].ToString() == healthRule.RuleName).FirstOrDefault();
                                    if (healthRuleObject != null)
                                    {
                                        healthRule.HealthRuleID = (long)healthRuleObject["id"];
                                        // TODO the health rule can't be hotlinked to until platform rewrites the screen that opens from Flash
                                        healthRule.HealthRuleLink = String.Format(DEEPLINK_HEALTH_RULE, jobTarget.Controller, jobTarget.ApplicationID, healthRule.HealthRuleID, DEEPLINK_THIS_TIMERANGE);
                                    }
                                }

                                healthRulesList.Add(healthRule);
                            }

                            loggerConsole.Info("{0} Health Rules", healthRulesList.Count);

                            applicationConfiguration.NumHealthRules = healthRulesList.Count;

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + healthRulesList.Count;

                            // Sort them
                            healthRulesList = healthRulesList.OrderBy(h => h.HRRuleType).ThenBy(h => h.RuleName).ToList();
                            FileIOHelper.WriteListToCSVFile(healthRulesList, new HealthRuleReportMap(), FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Actions

                        loggerConsole.Info("Index Actions");

                        List<ReportObjects.Action> actionsList = new List<ReportObjects.Action>();

                        List<HTTPAlertTemplate> httpTemplatesList = FileIOHelper.ReadListFromCSVFile<HTTPAlertTemplate>(FilePathMap.HTTPTemplatesIndexFilePath(jobTarget), new HTTPAlertTemplateReportMap());
                        List<EmailAlertTemplate> emailTemplatesList = FileIOHelper.ReadListFromCSVFile<EmailAlertTemplate>(FilePathMap.EmailTemplatesIndexFilePath(jobTarget), new EmailAlertTemplateReportMap());

                        JObject actionsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.ApplicationActionsDataFilePath(jobTarget));
                        if (isTokenPropertyNull(actionsContainerObject, "actions") == false)
                        {
                            foreach (JObject actionObject in actionsContainerObject["actions"])
                            {
                                ReportObjects.Action action = new ReportObjects.Action();
                                action.Controller = jobTarget.Controller;
                                action.ApplicationName = jobTarget.Application;
                                action.ApplicationID = jobTarget.ApplicationID;

                                action.ControllerLink = String.Format(DEEPLINK_CONTROLLER, action.Controller, DEEPLINK_THIS_TIMERANGE);
                                action.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, action.Controller, action.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                action.ActionName = getStringValueFromJToken(actionObject, "name");
                                action.Description = getStringValueFromJToken(actionObject, "description");
                                action.ActionID = getLongValueFromJToken(actionObject, "id");
                                action.Priority = getIntValueFromJToken(actionObject, "priority");

                                action.ActionType = getStringValueFromJToken(actionObject, "instanceType");

                                switch (action.ActionType)
                                {
                                    case "SCRIPT_ACTION":
                                        action.IsAdjudicate = getBoolValueFromJToken(actionObject, "adjudicate");
                                        action.AdjudicatorEmail = getStringValueFromJToken(actionObject, "adjudicatorEmail");
                                        action.ScriptPath = getStringValueFromJToken(actionObject, "scriptPath");
                                        action.LogPaths = getStringValueOfObjectFromJToken(actionObject, "logPaths");
                                        action.ScriptOutputPaths = getStringValueOfObjectFromJToken(actionObject, "scriptOutputs");
                                        action.CollectScriptOutputs = getBoolValueFromJToken(actionObject, "collectScriptOutputs");
                                        action.TimeoutMinutes = getIntValueFromJToken(actionObject, "timeoutMinutes");

                                        break;

                                    case "CUSTOM_ACTION":
                                        action.CustomType = getStringValueFromJToken(actionObject, "customType");
                                        action.ActionName = String.Format("{0} ({1})", action.CustomType, action.ActionName);

                                        break;

                                    case "EMAIL_ACTION":
                                        action.To = getStringValueFromJToken(actionObject, "toAddress");
                                        action.Subject = getStringValueFromJToken(actionObject, "subject");
                                        action.ActionName = String.Format("{0} ({1})", action.To, action.ActionName);

                                        break;

                                    case "SMS_ACTION":
                                        action.To = getStringValueFromJToken(actionObject, "toNumber");
                                        action.ActionName = String.Format("{0} ({1})", action.To, action.ActionName);

                                        break;

                                    case "HTTP_REQUEST_ACTION":
                                        action.TemplateID = getLongValueFromJToken(actionObject, "planId");
                                        try
                                        {
                                            string[] nameValues = actionObject["customProperties"].Select(s => String.Format("{0}={1}", getStringValueFromJToken(s, "name"), getStringValueFromJToken(s, "value"))).ToArray();
                                            action.CustomProperties = String.Join(";", nameValues);
                                        }
                                        catch { }
                                        if (httpTemplatesList != null)
                                        {
                                            HTTPAlertTemplate httpAlertTemplate = httpTemplatesList.Where(t => t.TemplateID == action.TemplateID).FirstOrDefault();
                                            if (httpAlertTemplate != null)
                                            {
                                                action.ActionTemplate = httpAlertTemplate.Name;
                                            }
                                        }

                                        break;

                                    case "CUSTOM_EMAIL_ACTION":
                                        action.TemplateID = getLongValueFromJToken(actionObject, "planId");
                                        try
                                        {
                                            string[] emails = actionObject["to"].Select(s => getStringValueFromJToken(s, "emailAddress")).ToArray();
                                            action.To = String.Join(";", emails);
                                        }
                                        catch { }
                                        try
                                        {
                                            string[] nameValues = actionObject["customProperties"].Select(s => String.Format("{0}={1}", getStringValueFromJToken(s, "name"), getStringValueFromJToken(s, "value"))).ToArray();
                                            action.CustomProperties = String.Join(";", nameValues);
                                        }
                                        catch { }
                                        if (emailTemplatesList != null)
                                        {
                                            EmailAlertTemplate emailTemplate = emailTemplatesList.Where(t => t.TemplateID == action.TemplateID).FirstOrDefault();
                                            if (emailTemplate != null)
                                            {
                                                action.ActionTemplate = emailTemplate.Name;
                                            }
                                        }

                                        break;

                                    case "THREAD_DUMP_ACTION":
                                        action.NumSamples = getIntValueFromJToken(actionObject, "numberOfSamples");
                                        action.SampleInterval = getLongValueFromJToken(actionObject, "samplingIntervalMills");

                                        break;


                                    default:
                                        break;
                                }

                                actionsList.Add(action);
                            }
                        }

                        loggerConsole.Info("{0} Actions", actionsList.Count);

                        applicationConfiguration.NumActions = actionsList.Count;

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + actionsList.Count;

                        // Sort them
                        actionsList = actionsList.OrderBy(o => o.ActionName).ToList();
                        FileIOHelper.WriteListToCSVFile<ReportObjects.Action>(actionsList, new ActionReportMap(), FilePathMap.ApplicationActionsIndexFilePath(jobTarget));

                        #endregion

                        #region Policies

                        loggerConsole.Info("Index Policies");

                        List<Policy> policiesList = new List<Policy>();
                        List<PolicyActionMapping> policyActionMappingList = new List<PolicyActionMapping>();

                        JObject policiesContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.ApplicationPoliciesDataFilePath(jobTarget));
                        if (isTokenPropertyNull(policiesContainerObject, "eventReactors") == false)
                        {
                            foreach (JObject policyObject in policiesContainerObject["eventReactors"])
                            {
                                Policy policy = new Policy();
                                policy.Controller = jobTarget.Controller;
                                policy.ApplicationName = jobTarget.Application;
                                policy.ApplicationID = jobTarget.ApplicationID;

                                policy.ControllerLink = String.Format(DEEPLINK_CONTROLLER, policy.Controller, DEEPLINK_THIS_TIMERANGE);
                                policy.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, policy.Controller, policy.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                policy.PolicyName = getStringValueFromJToken(policyObject, "name");
                                policy.PolicyType = getStringValueFromJToken(policyObject, "type");
                                policy.Duration = getIntValueFromJToken(policyObject, "durationInMins");
                                policy.IsEnabled = getBoolValueFromJToken(policyObject, "enabled");
                                policy.IsBatchActionsPerMinute = getBoolValueFromJToken(policyObject, "batchActionsPerMinute");

                                if (isTokenPropertyNull(policyObject, "createdBy") == false) policy.CreatedBy = getStringValueFromJToken(policyObject["createdBy"], "name");
                                if (isTokenPropertyNull(policyObject, "modifiedBy") == false) policy.ModifiedBy = getStringValueFromJToken(policyObject["modifiedBy"], "name");

                                policy.EventFilterRawValue = getStringValueOfObjectFromJToken(policyObject, "eventFilter");
                                policy.EntityFiltersRawValue = getStringValueOfObjectFromJToken(policyObject, "entityFilters");

                                if (isTokenPropertyNull(policyObject, "eventFilter") == false)
                                {
                                    policy.RequestExperiences = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "requestExperiences");
                                    policy.CustomEventFilters = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "customEventFilters");
                                    policy.MissingEntities = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "missingEntities");
                                    policy.FilterProperties = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "filterProperties");
                                    policy.MaxRows = getIntValueFromJToken(policyObject["eventFilter"], "maxRows");

                                    policy.ApplicationIDs = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "applicationIds", true);
                                    policy.BTIDs = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "businessTransactionIds", true);
                                    policy.TierIDs = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "applicationComponentIds", true);
                                    policy.NodeIDs = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "applicationComponentNodeIds", true);
                                    policy.ErrorIDs = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "errorIDs", true);
                                    policy.HRIDs = getStringValueOfObjectFromJToken(policyObject["eventFilter"], "policyIds", true);
                                    try { policy.NumHRs = policyObject["eventFilter"]["policyIds"].Count(); } catch { }

                                    try
                                    {
                                        List<HealthRule> thisHealthRules = new List<HealthRule>();
                                        foreach (JToken policyIDToken in policyObject["eventFilter"]["policyIds"])
                                        {
                                            HealthRule healthRule = healthRulesList.Where(h => h.HealthRuleID == (long)policyIDToken).FirstOrDefault();
                                            if (healthRule != null)
                                            {
                                                thisHealthRules.Add(healthRule);
                                            }
                                        }
                                        string[] nameValues = thisHealthRules.Select(h => String.Format("{0}({1})", h.RuleName, h.HRRuleType)).ToArray();
                                        policy.HRNames = String.Join(";", nameValues);
                                    }
                                    catch { }

                                    policy.HRVStartedWarning = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationStartedWarning");
                                    policy.HRVStartedCritical = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationStartedCritical");
                                    policy.HRVWarningToCritical = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationWarningToCritical");
                                    policy.HRVCriticalToWarning = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationCriticalToWarning");
                                    policy.HRVContinuesCritical = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationContinuesCritical");
                                    policy.HRVContinuesWarning = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationContinuesWarning");
                                    policy.HRVEndedCritical = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationEndedCritical");
                                    policy.HRVEndedWarning = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationEndedWarning");
                                    policy.HRVCanceledCritical = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationCanceledCritical");
                                    policy.HRVCanceledWarning = getBoolValueFromJToken(policyObject["eventFilter"], "policyViolationCanceledWarning");

                                    policy.RequestSlow = getBoolValueFromJToken(policyObject["eventFilter"], "slowRequest");
                                    policy.RequestVerySlow = getBoolValueFromJToken(policyObject["eventFilter"], "verySlowRequest");
                                    policy.RequestStall = getBoolValueFromJToken(policyObject["eventFilter"], "stalledRequest");
                                    policy.AllError = getBoolValueFromJToken(policyObject["eventFilter"], "allError");

                                    policy.AppCrashCLR = getBoolValueFromJToken(policyObject["eventFilter"], "clrCrash");
                                    policy.AppCrash = getBoolValueFromJToken(policyObject["eventFilter"], "applicationCrash");
                                    policy.AppRestart = getBoolValueFromJToken(policyObject["eventFilter"], "appServerRestart");
                                }

                                policy.PolicyID = getLongValueFromJToken(policyObject, "id");

                                if (isTokenPropertyNull(policyObject, "actionWrappers") == false)
                                {
                                    policy.NumActions = policyObject["actionWrappers"].Count();

                                    if (policy.NumActions > 0)
                                    {
                                        List<ReportObjects.Action> thisPolicyActions = new List<ReportObjects.Action>(policy.NumActions);

                                        foreach (JObject policyActionMappingObject in policyObject["actionWrappers"])
                                        {
                                            PolicyActionMapping policyActionMapping = new PolicyActionMapping();
                                            policyActionMapping.Controller = jobTarget.Controller;
                                            policyActionMapping.ApplicationName = jobTarget.Application;
                                            policyActionMapping.ApplicationID = jobTarget.ApplicationID;

                                            policyActionMapping.ControllerLink = String.Format(DEEPLINK_CONTROLLER, policyActionMapping.Controller, DEEPLINK_THIS_TIMERANGE);
                                            policyActionMapping.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, policyActionMapping.Controller, policyActionMapping.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                            policyActionMapping.PolicyName = policy.PolicyName;
                                            policyActionMapping.PolicyType = policy.PolicyType;
                                            policyActionMapping.PolicyID = policy.PolicyID;

                                            policyActionMapping.ActionID = getLongValueFromJToken(policyActionMappingObject, "actionId");
                                            ReportObjects.Action action = actionsList.Where(a => a.ActionID == policyActionMapping.ActionID).FirstOrDefault();
                                            if (action != null)
                                            {
                                                policyActionMapping.ActionName = action.ActionName;
                                                policyActionMapping.ActionType = action.ActionType;

                                                thisPolicyActions.Add(action);
                                            }

                                            policyActionMappingList.Add(policyActionMapping);
                                        }

                                        string[] nameValues = thisPolicyActions.Select(a => String.Format("{0}({1})", a.ActionName, a.ActionType)).ToArray();
                                        policy.Actions = String.Join(";", nameValues);
                                    }
                                }

                                policiesList.Add(policy);
                            }
                        }

                        loggerConsole.Info("{0} Policies", policiesList.Count);

                        applicationConfiguration.NumPolicies = policiesList.Count;

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + policiesList.Count;

                        // Sort them
                        policiesList = policiesList.OrderBy(o => o.PolicyName).ToList();
                        FileIOHelper.WriteListToCSVFile<Policy>(policiesList, new PolicyReportMap(), FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget));

                        loggerConsole.Info("{0} Policy to Action Mappings", policyActionMappingList.Count);

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + policyActionMappingList.Count;

                        // Sort them
                        policyActionMappingList = policyActionMappingList.OrderBy(o => o.PolicyName).ThenBy(o => o.ActionName).ToList();
                        FileIOHelper.WriteListToCSVFile<PolicyActionMapping>(policyActionMappingList, new PolicyActionMappingReportMap(), FilePathMap.ApplicationPolicyActionMappingsIndexFilePath(jobTarget));

                        #endregion

                        #region Application

                        List<ApplicationConfigurationPolicy> applicationConfigurationsList = new List<ApplicationConfigurationPolicy>(1);
                        applicationConfigurationsList.Add(applicationConfiguration);
                        FileIOHelper.WriteListToCSVFile(applicationConfigurationsList, new ApplicationConfigurationPolicyReportMap(), FilePathMap.ApplicationConfigurationHealthRulesIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ApplicationConfigurationReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ApplicationConfigurationReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.ApplicationConfigurationHealthRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationConfigurationHealthRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationConfigurationHealthRulesReportFilePath(), FilePathMap.ApplicationConfigurationHealthRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationHealthRulesReportFilePath(), FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationPoliciesReportFilePath(), FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationActionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationActionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationActionsReportFilePath(), FilePathMap.ApplicationActionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationPolicyActionMappingsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationPolicyActionMappingsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationPolicyActionMappingsReportFilePath(), FilePathMap.ApplicationPolicyActionMappingsIndexFilePath(jobTarget));
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

                // Remove all templates from the list
                jobConfiguration.Target.RemoveAll(t => t.Controller == BLANK_APPLICATION_CONTROLLER);

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
            logger.Trace("LicensedReports.Configuration={0}", programOptions.LicensedReports.Configuration);
            loggerConsole.Trace("LicensedReports.Configuration={0}", programOptions.LicensedReports.Configuration);
            if (programOptions.LicensedReports.Configuration == false)
            {
                loggerConsole.Warn("Not licensed for configuration");
                return false;
            }

            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            if (jobConfiguration.Input.Configuration == false)
            {
                loggerConsole.Trace("Skipping index of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }

        private static HealthRule fillHealthRule(XmlNode healthRuleConfigurationNode, ApplicationConfigurationPolicy applicationConfiguration)
        {
            HealthRule healthRule = new HealthRule();

            healthRule.Controller = applicationConfiguration.Controller;
            healthRule.ControllerLink = applicationConfiguration.ControllerLink;
            healthRule.ApplicationName = applicationConfiguration.ApplicationName;
            healthRule.ApplicationID = applicationConfiguration.ApplicationID;
            healthRule.ApplicationLink = applicationConfiguration.ApplicationLink;

            healthRule.RuleName = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("name"));
            healthRule.HRRuleType = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("type"));
            healthRule.Description = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("description"));
            healthRule.IsEnabled = getBoolValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("enabled"));
            healthRule.IsDefault = getBoolValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("is-default"));
            healthRule.IsAlwaysEnabled = getBoolValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("always-enabled"));
            healthRule.Schedule = getStringValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("schedule"));
            healthRule.DurationOfEvalPeriod = getIntegerValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("duration-min"));
            healthRule.WaitTimeAfterViolation = getIntegerValueFromXmlNode(healthRuleConfigurationNode.SelectSingleNode("wait-time-min"));

            healthRule.AffectedEntitiesRawValue = makeXMLFormattedAndIndented(healthRuleConfigurationNode.SelectSingleNode("affected-entities-match-criteria"));
            healthRule.CriticalConditionRawValue = makeXMLFormattedAndIndented(healthRuleConfigurationNode.SelectSingleNode("critical-execution-criteria"));
            healthRule.WarningConditionRawValue = makeXMLFormattedAndIndented(healthRuleConfigurationNode.SelectSingleNode("warning-execution-criteria"));

            // Affected entity selection
            XmlNode affectedWrapperXmlNode = healthRuleConfigurationNode.SelectSingleNode("affected-entities-match-criteria").ChildNodes[0];
            if (affectedWrapperXmlNode != null)
            {
                healthRule.AffectsEntityType = getStringValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("type"));
                healthRule.AffectsEntityMatchType = getStringValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("match-type"));
                healthRule.AffectsEntityMatchPattern = getStringValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("match-pattern"));
                healthRule.AffectsEntityMatchIsInverse = getBoolValueFromXmlNode(affectedWrapperXmlNode.SelectSingleNode("inverse"));
                healthRule.AffectsEntityMatchCriteria = makeXMLFormattedAndIndented(String.Format("<match-criteria>{0}</match-criteria>", makeXMLFormattedAndIndented(affectedWrapperXmlNode.SelectNodes("*[not(self::type) and not(self::match-type) and not(self::match-pattern) and not(self::inverse)]"))));
            }

            // XML can look like that for single element
            //<critical-execution-criteria>
            //    <entity-aggregation-scope>
            //        <type>ANY</type>
            //        <value>0</value>
            //    </entity-aggregation-scope>
            //    <policy-condition>
            //        <type>leaf</type>
            //        <display-name>condition 1</display-name>
            //        <condition-value-type>ABSOLUTE</condition-value-type>
            //        <condition-value>0.0</condition-value>
            //        <operator>GREATER_THAN</operator>
            //        <condition-expression/>
            //        <use-active-baseline>false</use-active-baseline>
            //        <trigger-on-no-data>true</trigger-on-no-data>
            //        <metric-expression>
            //            <type>leaf</type>
            //            <function-type>VALUE</function-type>
            //            <value>0</value>
            //            <is-literal-expression>false</is-literal-expression>
            //            <display-name>null</display-name>
            //            <metric-definition>
            //                <type>LOGICAL_METRIC</type>
            //                <logical-metric-name>Agent|App|Availability</logical-metric-name>
            //            </metric-definition>
            //        </metric-expression>
            //    </policy-condition>
            //</critical-execution-criteria>

            // Or like that for multiple
            //<critical-execution-criteria>
            //    <entity-aggregation-scope>
            //        <type>AGGREGATE</type>
            //        <value>0</value>
            //    </entity-aggregation-scope>
            //    <policy-condition>
            //        <type>boolean</type>
            //        <operator>AND</operator>
            //        <condition1>
            //            <type>leaf</type>
            //            <display-name>Average Response Time (ms) Baseline Condition</display-name>
            //            <condition-value-type>BASELINE_STANDARD_DEVIATION</condition-value-type>
            //            <condition-value>3.0</condition-value>
            //            <operator>GREATER_THAN</operator>
            //            <condition-expression/>
            //            <use-active-baseline>true</use-active-baseline>
            //            <trigger-on-no-data>false</trigger-on-no-data>
            //            <metric-expression>
            //                <type>leaf</type>
            //                <function-type>VALUE</function-type>
            //                <value>0</value>
            //                <is-literal-expression>false</is-literal-expression>
            //                <display-name>null</display-name>
            //                <metric-definition>
            //                    <type>LOGICAL_METRIC</type>
            //                    <logical-metric-name>Average Response Time (ms)</logical-metric-name>
            //                </metric-definition>
            //            </metric-expression>
            //        </condition1>
            //        <condition2>
            //            <type>leaf</type>
            //            <display-name>Calls per Minute Condition</display-name>
            //            <condition-value-type>ABSOLUTE</condition-value-type>
            //            <condition-value>50.0</condition-value>
            //            <operator>GREATER_THAN</operator>
            //            <condition-expression/>
            //            <use-active-baseline>false</use-active-baseline>
            //            <trigger-on-no-data>false</trigger-on-no-data>
            //            <metric-expression>
            //                <type>leaf</type>
            //                <function-type>VALUE</function-type>
            //                <value>0</value>
            //                <is-literal-expression>false</is-literal-expression>
            //                <display-name>null</display-name>
            //                <metric-definition>
            //                    <type>LOGICAL_METRIC</type>
            //                    <logical-metric-name>Calls per Minute</logical-metric-name>
            //                </metric-definition>
            //            </metric-expression>
            //        </condition2>
            //    </policy-condition>
            //</critical-execution-criteria>            

            // Critical
            XmlNode criticalExecutionCriteriaXmlNode = healthRuleConfigurationNode.SelectSingleNode("critical-execution-criteria");
            if (criticalExecutionCriteriaXmlNode != null)
            {
                healthRule.CriticalAggregateType = getStringValueFromXmlNode(criticalExecutionCriteriaXmlNode.SelectSingleNode("entity-aggregation-scope/type"));

                XmlNode firstCondition = criticalExecutionCriteriaXmlNode.SelectSingleNode("policy-condition");
                XmlNodeList condition1sXmlNodeList = criticalExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition1");
                XmlNodeList condition2sXmlNodeList = criticalExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition2");

                List<XmlNode> conditionsList = new List<XmlNode>();
                if (condition1sXmlNodeList.Count == 0)
                {
                    healthRule.CriticalEntityConditionType = "AND";
                    conditionsList.Add(firstCondition);
                }
                else
                {
                    healthRule.CriticalEntityConditionType = getStringValueFromXmlNode(criticalExecutionCriteriaXmlNode.SelectSingleNode("policy-condition/operator"));
                    foreach (XmlNode xmlNode in condition1sXmlNodeList)
                    {
                        conditionsList.Add(xmlNode);
                    }
                    conditionsList.Add(condition2sXmlNodeList[condition2sXmlNodeList.Count - 1]);
                }

                healthRule.CriticalNumConditions = conditionsList.Count;

                int i = 1;
                foreach (XmlNode conditionXmlNode in conditionsList)
                {
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Name", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("display-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Type", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Value", i)).SetValue(healthRule, getLongValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Operator", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("operator")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}Expression", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-expression")), null);
                    if (getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("use-active-baseline")) == true)
                    {
                        healthRule.GetType().GetProperty(String.Format("Crit{0}BaselineUsed", i)).SetValue(healthRule, "Default Baseline", null);
                    }
                    else
                    {
                        healthRule.GetType().GetProperty(String.Format("Crit{0}BaselineUsed", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-baseline/name")), null);
                    }
                    healthRule.GetType().GetProperty(String.Format("Crit{0}TriggerOnNoData", i)).SetValue(healthRule, getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("trigger-on-no-data")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}MetricName", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/metric-definition/logical-metric-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}MetricFunction", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/function-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Crit{0}MetricExpressionConfig", i)).SetValue(healthRule, makeXMLFormattedAndIndented(conditionXmlNode.SelectSingleNode("metric-expression")), null);

                    i++;
                    if (i > 5) break;
                }
            }

            // Warning
            XmlNode warningExecutionCriteriaXmlNode = healthRuleConfigurationNode.SelectSingleNode("warning-execution-criteria");
            if (warningExecutionCriteriaXmlNode != null)
            {
                healthRule.WarningAggregateType = getStringValueFromXmlNode(warningExecutionCriteriaXmlNode.SelectSingleNode("entity-aggregation-scope/type"));

                XmlNode firstCondition = warningExecutionCriteriaXmlNode.SelectSingleNode("policy-condition");
                XmlNodeList condition1sXmlNodeList = warningExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition1");
                XmlNodeList condition2sXmlNodeList = warningExecutionCriteriaXmlNode.SelectNodes("policy-condition//condition2");

                List<XmlNode> conditionsList = new List<XmlNode>();
                if (condition1sXmlNodeList.Count == 0)
                {
                    healthRule.WarningEntityConditionType = "AND";
                    conditionsList.Add(firstCondition);
                }
                else
                {
                    healthRule.WarningEntityConditionType = getStringValueFromXmlNode(warningExecutionCriteriaXmlNode.SelectSingleNode("policy-condition/operator"));
                    foreach (XmlNode xmlNode in condition1sXmlNodeList)
                    {
                        conditionsList.Add(xmlNode);
                    }
                    conditionsList.Add(condition2sXmlNodeList[condition2sXmlNodeList.Count - 1]);
                }

                healthRule.WarningNumConditions = conditionsList.Count;

                int i = 1;
                foreach (XmlNode conditionXmlNode in conditionsList)
                {
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Name", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("display-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Type", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Value", i)).SetValue(healthRule, getLongValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-value")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Operator", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("operator")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}Expression", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("condition-expression")), null);
                    if (getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("use-active-baseline")) == true)
                    {
                        healthRule.GetType().GetProperty(String.Format("Warn{0}BaselineUsed", i)).SetValue(healthRule, "Default Baseline", null);
                    }
                    else
                    {
                        healthRule.GetType().GetProperty(String.Format("Warn{0}BaselineUsed", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-baseline/name")), null);
                    }
                    healthRule.GetType().GetProperty(String.Format("Warn{0}TriggerOnNoData", i)).SetValue(healthRule, getBoolValueFromXmlNode(conditionXmlNode.SelectSingleNode("trigger-on-no-data")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}MetricName", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/metric-definition/logical-metric-name")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}MetricFunction", i)).SetValue(healthRule, getStringValueFromXmlNode(conditionXmlNode.SelectSingleNode("metric-expression/function-type")), null);
                    healthRule.GetType().GetProperty(String.Format("Warn{0}MetricExpressionConfig", i)).SetValue(healthRule, makeXMLFormattedAndIndented(conditionXmlNode.SelectSingleNode("metric-expression")), null);

                    i++;
                    if (i > 5) break;
                }
            }

            return healthRule;
        }

    }
}
