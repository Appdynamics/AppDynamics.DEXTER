using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexWEBConfiguration : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_WEB) == 0)
                {
                    return true;
                }

                #region Template comparisons 

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

                #endregion

                bool reportFolderCleaned = false;

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

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Preload list of detected entities

                        // For later cross-reference
                        List<ControllerApplication> controllerApplicationList = FileIOHelper.ReadListFromCSVFile<ControllerApplication>(FilePathMap.ControllerApplicationsIndexFilePath(jobTarget), new ControllerApplicationReportMap());

                        #endregion

                        #region Application Summary

                        WEBApplicationConfiguration applicationConfiguration = new WEBApplicationConfiguration();

                        loggerConsole.Info("Application Summary");

                        applicationConfiguration.Controller = jobTarget.Controller;
                        applicationConfiguration.ControllerLink = String.Format(DEEPLINK_CONTROLLER, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ApplicationName = jobTarget.Application;
                        applicationConfiguration.ApplicationID = jobTarget.ApplicationID;
                        applicationConfiguration.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        if (controllerApplicationList != null)
                        {
                            ControllerApplication controllerApplication = controllerApplicationList.Where(a => a.Type == APPLICATION_TYPE_WEB && a.ApplicationID == applicationConfiguration.ApplicationID).FirstOrDefault();
                            if (controllerApplication != null)
                            {
                                applicationConfiguration.ApplicationDescription = controllerApplication.Description;
                            }
                        }

                        // Application Key
                        JObject appKeyObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBApplicationKeyDataFilePath(jobTarget));
                        if (appKeyObject != null)
                        {
                            applicationConfiguration.ApplicationKey = getStringValueFromJToken(appKeyObject, "appKey");
                        }

                        // Instrumentation Options
                        JObject instrumentationObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBAgentConfigDataFilePath(jobTarget));
                        if (instrumentationObject != null)
                        {
                            applicationConfiguration.IsXsccEnabled = getBoolValueFromJToken(instrumentationObject, "enableXssc");
                            applicationConfiguration.HostOption = getIntValueFromJToken(instrumentationObject, "hostOption");

                            applicationConfiguration.AgentHTTP = getStringValueFromJToken(instrumentationObject, "jsAgentUrlHttp");
                            applicationConfiguration.AgentHTTPS = getStringValueFromJToken(instrumentationObject, "jsAgentUrlHttps");
                            applicationConfiguration.GeoHTTP = getStringValueFromJToken(instrumentationObject, "geoUrlHttp");
                            applicationConfiguration.GeoHTTPS = getStringValueFromJToken(instrumentationObject, "geoUrlHttps");
                            applicationConfiguration.BeaconHTTP = getStringValueFromJToken(instrumentationObject, "beaconUrlHttp");
                            applicationConfiguration.BeaconHTTPS = getStringValueFromJToken(instrumentationObject, "beaconUrlHttps");

                            applicationConfiguration.AgentCode = getStringValueFromJToken(instrumentationObject, "codeSnippet");
                        }

                        // Monitoring State
                        string monitoringState = FileIOHelper.ReadFileFromPath(FilePathMap.WEBApplicationMonitoringStateDataFilePath(jobTarget));
                        if (monitoringState != String.Empty)
                        {
                            bool parsedBool = false;
                            Boolean.TryParse(monitoringState, out parsedBool);
                            applicationConfiguration.IsEnabled = parsedBool;
                        }

                        // Error Detection
                        JObject errorDetectionRulesObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBAgentErrorRulesDataFilePath(jobTarget));
                        if (errorDetectionRulesObject != null)
                        {
                            applicationConfiguration.IsJSErrorEnabled = getBoolValueFromJToken(errorDetectionRulesObject, "javaScriptErrorCaptureEnabled");
                            applicationConfiguration.IsAJAXErrorEnabled = getBoolValueFromJToken(errorDetectionRulesObject, "ajaxRequestErrorCaptureEnabled");
                            applicationConfiguration.IgnoreJSErrors = getStringValueOfObjectFromJToken(errorDetectionRulesObject, "ignoreJavaScriptErrorConfigRules", true);
                            applicationConfiguration.IgnorePageNames = getStringValueOfObjectFromJToken(errorDetectionRulesObject, "ignorePageNames", true);
                            applicationConfiguration.IgnoreURLs = getStringValueOfObjectFromJToken(errorDetectionRulesObject, "ignoreUrls", true);
                        }

                        // Page Settings
                        JObject pageSettingsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBAgentPageSettingsRulesDataFilePath(jobTarget));
                        if (pageSettingsObject != null)
                        {
                            if (isTokenPropertyNull(pageSettingsObject, "thresholds") == false)
                            {
                                applicationConfiguration.SlowThresholdType = getStringValueFromJToken(pageSettingsObject["thresholds"]["slowThreshold"], "type");
                                applicationConfiguration.SlowThreshold = getIntValueFromJToken(pageSettingsObject["thresholds"]["slowThreshold"], "value");

                                applicationConfiguration.VerySlowThresholdType = getStringValueFromJToken(pageSettingsObject["thresholds"]["verySlowThreshold"], "type");
                                applicationConfiguration.VerySlowThreshold = getIntValueFromJToken(pageSettingsObject["thresholds"]["verySlowThreshold"], "value");

                                applicationConfiguration.StallThresholdType = getStringValueFromJToken(pageSettingsObject["thresholds"]["stallThreshold"], "type");
                                applicationConfiguration.StallThreshold = getIntValueFromJToken(pageSettingsObject["thresholds"]["stallThreshold"], "value");
                            }
                            applicationConfiguration.Percentiles= getStringValueOfObjectFromJToken(pageSettingsObject, "percentileMetrics", true);
                            applicationConfiguration.SessionTimeout = getIntValueFromJToken(pageSettingsObject["sessionsMonitor"], "sessionTimeoutMins");
                            applicationConfiguration.IsIPDisplayed = getBoolValueFromJToken(pageSettingsObject, "ipAddressDisplayed");

                            applicationConfiguration.EnableSlowSnapshots = getBoolValueFromJToken(pageSettingsObject["eventPolicy"], "enableSlowSnapshotCollection");
                            applicationConfiguration.EnablePeriodicSnapshots = getBoolValueFromJToken(pageSettingsObject["eventPolicy"], "enablePeriodicSnapshotCollection");
                            applicationConfiguration.EnableErrorSnapshots = getBoolValueFromJToken(pageSettingsObject["eventPolicy"], "enableErrorSnapshotCollection");
                        }

                        #endregion

                        #region Rules of all kinds
                         
                        loggerConsole.Info("Page and AJAX Request Rules");

                        #region Page Rules

                        List<WEBPageDetectionRule> pageDetectionRulesList = new List<WEBPageDetectionRule>(1024);

                        JObject pageRulesObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBAgentPageRulesDataFilePath(jobTarget));
                        if (pageRulesObject != null)
                        {
                            if (isTokenPropertyNull(pageRulesObject, "customNamingIncludeRules") == false)
                            {
                                JArray includeRulesArray = (JArray)pageRulesObject["customNamingIncludeRules"];
                                foreach (JObject includeRuleObject in includeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(includeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "INCLUDE";
                                        webPageDetectionRule.EntityCategory = "Pages&IFrames";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }

                            if (isTokenPropertyNull(pageRulesObject, "customNamingExcludeRules") == false)
                            {
                                JArray excludeRulesArray = (JArray)pageRulesObject["customNamingExcludeRules"];
                                foreach (JObject excludeRuleObject in excludeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(excludeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "EXCLUDE";
                                        webPageDetectionRule.EntityCategory = "Pages&IFrames";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }
                        }

                        #endregion

                        #region AJAX Rules

                        JObject ajaxRulesObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBAgentAjaxRulesDataFilePath(jobTarget));
                        if (ajaxRulesObject != null)
                        {
                            if (isTokenPropertyNull(ajaxRulesObject, "customNamingIncludeRules") == false)
                            {
                                JArray includeRulesArray = (JArray)ajaxRulesObject["customNamingIncludeRules"];
                                foreach (JObject includeRuleObject in includeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(includeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "INCLUDE";
                                        webPageDetectionRule.EntityCategory = "Ajax";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }

                            if (isTokenPropertyNull(ajaxRulesObject, "customNamingExcludeRules") == false)
                            {
                                JArray excludeRulesArray = (JArray)ajaxRulesObject["customNamingExcludeRules"];
                                foreach (JObject excludeRuleObject in excludeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(excludeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "EXCLUDE";
                                        webPageDetectionRule.EntityCategory = "Ajax";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }

                            if (isTokenPropertyNull(ajaxRulesObject, "eventServiceIncludeRules") == false)
                            {
                                JArray includeRulesArray = (JArray)ajaxRulesObject["eventServiceIncludeRules"];
                                foreach (JObject includeRuleObject in includeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(includeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "INCLUDE";
                                        webPageDetectionRule.EntityCategory = "AjaxEventsSvc";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }

                            if (isTokenPropertyNull(ajaxRulesObject, "eventServiceExcludeRules") == false)
                            {
                                JArray excludeRulesArray = (JArray)ajaxRulesObject["eventServiceExcludeRules"];
                                foreach (JObject excludeRuleObject in excludeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(excludeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "EXCLUDE";
                                        webPageDetectionRule.EntityCategory = "AjaxEventsSvc";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }
                        }

                        #endregion

                        #region Virtual Page Rules

                        JObject virtualPageRulesObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBAgentVirtualPageRulesDataFilePath(jobTarget));
                        if (virtualPageRulesObject != null)
                        {
                            if (isTokenPropertyNull(virtualPageRulesObject, "customNamingIncludeRules") == false)
                            {
                                JArray includeRulesArray = (JArray)virtualPageRulesObject["customNamingIncludeRules"];
                                foreach (JObject includeRuleObject in includeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(includeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "INCLUDE";
                                        webPageDetectionRule.EntityCategory = "VirtualPage";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }

                            if (isTokenPropertyNull(virtualPageRulesObject, "customNamingExcludeRules") == false)
                            {
                                JArray excludeRulesArray = (JArray)virtualPageRulesObject["customNamingExcludeRules"];
                                foreach (JObject excludeRuleObject in excludeRulesArray)
                                {
                                    WEBPageDetectionRule webPageDetectionRule = fillWebPageDetectionRule(excludeRuleObject, jobTarget);
                                    if (webPageDetectionRule != null)
                                    {
                                        webPageDetectionRule.DetectionType = "EXCLUDE";
                                        webPageDetectionRule.EntityCategory = "VirtualPage";

                                        pageDetectionRulesList.Add(webPageDetectionRule);
                                    }
                                }
                            }
                        }

                        #endregion

                        // Sort them
                        pageDetectionRulesList = pageDetectionRulesList.OrderBy(o => o.EntityCategory).ThenBy(o => o.DetectionType).ThenBy(o => o.Priority).ToList();
                        FileIOHelper.WriteListToCSVFile(pageDetectionRulesList, new WEBPageDetectionRuleReportMap(), FilePathMap.WEBPageAjaxVirtualPageRulesIndexFilePath(jobTarget));

                        loggerConsole.Info("Completed {0} Rules", pageDetectionRulesList.Count);

                        #endregion

                        #region Synthetic Jobs

                        loggerConsole.Info("Synthetic Jobs");

                        List<WEBSyntheticJobDefinition> syntheticJobDefinitionsList = null;

                        JObject syntheticJobsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBSyntheticJobsDataFilePath(jobTarget));
                        if (syntheticJobsObject != null)
                        {
                            if (isTokenPropertyNull(syntheticJobsObject, "jobListDatas") == false)
                            {
                                JArray syntheticJobsArray = (JArray)syntheticJobsObject["jobListDatas"];

                                syntheticJobDefinitionsList = new List<WEBSyntheticJobDefinition>(syntheticJobsArray.Count);

                                foreach (JObject syntheticJobObject in syntheticJobsArray)
                                {
                                    if (isTokenPropertyNull(syntheticJobObject, "config") == false)
                                    {
                                        JObject syntheticJobConfigObject = (JObject)syntheticJobObject["config"];

                                        WEBSyntheticJobDefinition syntheticJobDefinition = new WEBSyntheticJobDefinition();

                                        syntheticJobDefinition.Controller = jobTarget.Controller;
                                        syntheticJobDefinition.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                                        syntheticJobDefinition.ApplicationName = jobTarget.Application;
                                        syntheticJobDefinition.ApplicationID = jobTarget.ApplicationID;
                                        syntheticJobDefinition.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, jobTarget.Controller, jobTarget.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

                                        syntheticJobDefinition.JobName = getStringValueFromJToken(syntheticJobConfigObject, "description");
                                        syntheticJobDefinition.JobID = getStringValueFromJToken(syntheticJobConfigObject, "id");

                                        syntheticJobDefinition.IsUserEnabled = getBoolValueFromJToken(syntheticJobConfigObject, "userEnabled");
                                        syntheticJobDefinition.IsSystemEnabled = getBoolValueFromJToken(syntheticJobConfigObject, "systemEnabled");
                                        syntheticJobDefinition.FailOnError = getBoolValueFromJToken(syntheticJobConfigObject, "failOnPageError");
                                        syntheticJobDefinition.IsPrivateAgent = getBoolValueFromJToken(syntheticJobObject, "hasPrivateAgent");

                                        syntheticJobDefinition.RateUnit = getStringValueFromJToken(syntheticJobConfigObject["rate"], "unit");
                                        syntheticJobDefinition.Rate = getIntValueFromJToken(syntheticJobConfigObject["rate"], "value");
                                        syntheticJobDefinition.Timeout = getIntValueFromJToken(syntheticJobConfigObject, "timeoutSeconds");

                                        syntheticJobDefinition.Days = getStringValueOfObjectFromJToken(syntheticJobConfigObject, "daysOfWeek", true);
                                        syntheticJobDefinition.Browsers = getStringValueOfObjectFromJToken(syntheticJobConfigObject, "browserCodes", true);
                                        syntheticJobDefinition.Locations = getStringValueOfObjectFromJToken(syntheticJobConfigObject, "locationCodes", true);
                                        if (syntheticJobDefinition.Locations.Length > 0)
                                        {
                                            syntheticJobDefinition.NumLocations = ((JArray)syntheticJobConfigObject["locationCodes"]).Count();
                                        }
                                        syntheticJobDefinition.ScheduleMode = getStringValueFromJToken(syntheticJobConfigObject, "scheduleMode");

                                        syntheticJobDefinition.URL = getStringValueFromJToken(syntheticJobConfigObject, "url");
                                        syntheticJobDefinition.Script = getStringValueFromJToken(syntheticJobConfigObject["script"], "script");

                                        if (syntheticJobDefinition.URL.Length > 0)
                                        {
                                            syntheticJobDefinition.JobType = "URL";
                                        }
                                        else
                                        {
                                            syntheticJobDefinition.JobType = "SCRIPT";
                                        }

                                        syntheticJobDefinition.Network = getStringValueOfObjectFromJToken(syntheticJobConfigObject, "networkProfile", false);
                                        syntheticJobDefinition.Config = getStringValueOfObjectFromJToken(syntheticJobConfigObject, "composableConfig", false);
                                        syntheticJobDefinition.PerfCriteria = getStringValueOfObjectFromJToken(syntheticJobConfigObject, "performanceCriteria", false);

                                        syntheticJobDefinition.CreatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(syntheticJobConfigObject, "created"));
                                        try { syntheticJobDefinition.CreatedOn = syntheticJobDefinition.CreatedOnUtc.ToLocalTime(); } catch { }
                                        syntheticJobDefinition.UpdatedOnUtc = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(syntheticJobConfigObject, "updated"));
                                        try { syntheticJobDefinition.UpdatedOn = syntheticJobDefinition.UpdatedOnUtc.ToLocalTime(); } catch { }

                                        syntheticJobDefinitionsList.Add(syntheticJobDefinition);
                                    }
                                }

                                // Sort them
                                syntheticJobDefinitionsList = syntheticJobDefinitionsList.OrderBy(o => o.JobName).ToList();
                                FileIOHelper.WriteListToCSVFile(syntheticJobDefinitionsList, new WEBSyntheticJobDefinitionReportMap(), FilePathMap.WEBSyntheticJobsIndexFilePath(jobTarget));

                                loggerConsole.Info("Completed {0} Synthetic Jobs", syntheticJobDefinitionsList.Count);
                            }
                        }

                        #endregion

                        #region Application Settings

                        if (pageDetectionRulesList != null)
                        {
                            applicationConfiguration.NumPageRulesInclude = pageDetectionRulesList.Count(r => r.EntityCategory == "Pages&IFrames" && r.DetectionType == "INCLUDE");
                            applicationConfiguration.NumPageRulesExclude = pageDetectionRulesList.Count(r => r.EntityCategory == "Pages&IFrames" && r.DetectionType == "EXCLUDE");
                            applicationConfiguration.NumAJAXRulesInclude = pageDetectionRulesList.Count(r => r.EntityCategory == "Ajax" && r.DetectionType == "INCLUDE");
                            applicationConfiguration.NumAJAXRulesExclude = pageDetectionRulesList.Count(r => r.EntityCategory == "Ajax" && r.DetectionType == "EXCLUDE");
                            applicationConfiguration.NumVirtPageRulesInclude = pageDetectionRulesList.Count(r => r.EntityCategory == "VirtualPage" && r.DetectionType == "INCLUDE");
                            applicationConfiguration.NumVirtPageRulesExclude = pageDetectionRulesList.Count(r => r.EntityCategory == "VirtualPage" && r.DetectionType == "EXCLUDE");
                        }

                        if (syntheticJobDefinitionsList != null)
                        {
                            applicationConfiguration.NumSyntheticJobs = syntheticJobDefinitionsList.Count;
                        }

                        List<WEBApplicationConfiguration> applicationConfigurationsList = new List<WEBApplicationConfiguration>(1);
                        applicationConfigurationsList.Add(applicationConfiguration);
                        FileIOHelper.WriteListToCSVFile(applicationConfigurationsList, new WEBApplicationConfigurationReportMap(), FilePathMap.WEBApplicationConfigurationIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + applicationConfigurationsList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.WEBConfigurationReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.WEBConfigurationReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.WEBApplicationConfigurationIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBApplicationConfigurationIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBApplicationConfigurationReportFilePath(), FilePathMap.WEBApplicationConfigurationIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.WEBPageAjaxVirtualPageRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBPageAjaxVirtualPageRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBPageAjaxVirtualPageRulesReportFilePath(), FilePathMap.WEBPageAjaxVirtualPageRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.WEBSyntheticJobsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBSyntheticJobsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBSyntheticJobsReportFilePath(), FilePathMap.WEBSyntheticJobsIndexFilePath(jobTarget));
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            if (jobConfiguration.Input.Configuration == false)
            {
                loggerConsole.Trace("Skipping index of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }

        private static WEBPageDetectionRule fillWebPageDetectionRule(JObject ruleObject, JobTarget jobTarget)
        {
            WEBPageDetectionRule webPageDetectionRule = new WEBPageDetectionRule();

            webPageDetectionRule.Controller = jobTarget.Controller;
            webPageDetectionRule.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
            webPageDetectionRule.ApplicationName = jobTarget.Application;
            webPageDetectionRule.ApplicationID = jobTarget.ApplicationID;
            webPageDetectionRule.ApplicationLink = String.Format(DEEPLINK_WEB_APPLICATION, jobTarget.Controller, jobTarget.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

            webPageDetectionRule.RuleName = getStringValueFromJToken(ruleObject, "name");

            webPageDetectionRule.IsEnabled = getBoolValueFromJToken(ruleObject, "enabled");
            webPageDetectionRule.IsDefault = getBoolValueFromJToken(ruleObject, "isDefault");
            if (webPageDetectionRule.IsDefault && webPageDetectionRule.RuleName.Length == 0)
            {
                webPageDetectionRule.RuleName = "[DEFAULT]";
            }
            webPageDetectionRule.Priority = getIntValueFromJToken(ruleObject, "priority");

            webPageDetectionRule.MatchMobileApp = getMatchRuleDescription((JToken)ruleObject["matchOnMobileApplicationName"]);
            webPageDetectionRule.MatchURL = getMatchRuleDescription((JToken)ruleObject["matchOnURL"]);
            webPageDetectionRule.MatchIPAddress = getMatchRuleDescription((JToken)ruleObject["matchOnIpAddressMasks"]);
            webPageDetectionRule.MatchUserAgent = getMatchRuleDescription((JToken)ruleObject["matchOnUserAgent"]);
            webPageDetectionRule.MatchUserAgentType = getMatchRuleDescription((JToken)ruleObject["matchOnUserAgentType"]);

            if (isTokenPropertyNull(ruleObject, "pageNamingConfig") == false)
            {
                JObject pageNamingConfigObject = (JObject)ruleObject["pageNamingConfig"];
                webPageDetectionRule.UseProtocol = getBoolValueFromJToken(pageNamingConfigObject, "useProtocol");
                webPageDetectionRule.UseDomain = getBoolValueFromJToken(pageNamingConfigObject, "useDomainName");
                webPageDetectionRule.UseURL = getBoolValueFromJToken(pageNamingConfigObject, "useURL");
                webPageDetectionRule.UseRegex = getBoolValueFromJToken(pageNamingConfigObject, "useRegex");
                webPageDetectionRule.UseHTTP = getBoolValueFromJToken(pageNamingConfigObject, "useHttpMethod");
                webPageDetectionRule.NamingType = getStringValueFromJToken(pageNamingConfigObject, "type");
                webPageDetectionRule.AnchorType = getStringValueFromJToken(pageNamingConfigObject, "anchorType");
                webPageDetectionRule.UrlSegments = getStringValueOfObjectFromJToken(pageNamingConfigObject, "urlMatchSegments", true);
                webPageDetectionRule.AnchorSegments = getStringValueOfObjectFromJToken(pageNamingConfigObject, "anchorMatchSegments", true);
                webPageDetectionRule.RegexGroups = getStringValueOfObjectFromJToken(pageNamingConfigObject, "regexGroupConfig", true);
                webPageDetectionRule.QueryStrings = getStringValueOfObjectFromJToken(pageNamingConfigObject, "queryStringMatch", true).Replace("[]", "");
                webPageDetectionRule.DomainNameType = getStringValueFromJToken(pageNamingConfigObject, "domainNameType");
            }

            return webPageDetectionRule;
        }

        private static string getMatchRuleDescription(JToken ruleObject)
        {
            string type = getStringValueFromJToken(ruleObject, "type");
            string value = getStringValueFromJToken(ruleObject, "value");
            string methods = getStringValueOfObjectFromJToken(ruleObject, "httpMethods", true);

            StringBuilder sb = new StringBuilder(128);
            if (type.Length > 0)
            {
                sb.AppendFormat("{0}={1}", type, value);
            }
            if (methods.Length > 0)
            {
                sb.AppendFormat(" ({0})", methods);
            }
            return sb.ToString();
        }
    }
}
