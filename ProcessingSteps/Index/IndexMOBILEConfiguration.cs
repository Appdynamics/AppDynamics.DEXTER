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
    public class IndexMOBILEConfiguration : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_MOBILE) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_MOBILE);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_MOBILE);

                    return true;
                }

                #region Template comparisons 

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

                #endregion

                bool reportFolderCleaned = false;

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

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Preload list of detected entities

                        // For later cross-reference
                        List<ControllerApplication> controllerApplicationList = FileIOHelper.ReadListFromCSVFile<ControllerApplication>(FilePathMap.ControllerApplicationsIndexFilePath(jobTarget), new ControllerApplicationReportMap());

                        #endregion

                        #region Application Summary

                        MOBILEApplicationConfiguration applicationConfiguration = new MOBILEApplicationConfiguration();

                        loggerConsole.Info("Application Summary");

                        applicationConfiguration.Controller = jobTarget.Controller;
                        applicationConfiguration.ControllerLink = String.Format(DEEPLINK_CONTROLLER, applicationConfiguration.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        applicationConfiguration.ApplicationName = jobTarget.Application;
                        applicationConfiguration.ApplicationID = jobTarget.ApplicationID;
                        applicationConfiguration.ApplicationLink = String.Format(DEEPLINK_APM_APPLICATION, applicationConfiguration.Controller, applicationConfiguration.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
                        if (controllerApplicationList != null)
                        {
                            ControllerApplication controllerApplication = controllerApplicationList.Where(a => a.Type == APPLICATION_TYPE_MOBILE && a.ApplicationID == jobTarget.ApplicationID).FirstOrDefault();
                            if (controllerApplication != null)
                            {
                                applicationConfiguration.ApplicationDescription = controllerApplication.Description;
                            }
                        }

                        // Application Key
                        JObject appKeyObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.MOBILEApplicationKeyDataFilePath(jobTarget));
                        if (appKeyObject != null)
                        {
                            applicationConfiguration.ApplicationKey = getStringValueFromJToken(appKeyObject, "appKey");
                        }

                        // Monitoring State
                        string monitoringState = FileIOHelper.ReadFileFromPath(FilePathMap.MOBILEApplicationMonitoringStateDataFilePath(jobTarget));
                        if (monitoringState != String.Empty)
                        {
                            bool parsedBool = false;
                            Boolean.TryParse(monitoringState, out parsedBool);
                            applicationConfiguration.IsEnabled = parsedBool;
                        }

                        // Configuration Settings
                        JObject configurationSettingsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.MOBILEAgentPageSettingsRulesDataFilePath(jobTarget));
                        if (configurationSettingsObject != null)
                        {
                            if (isTokenPropertyNull(configurationSettingsObject, "thresholds") == false)
                            {
                                applicationConfiguration.SlowThresholdType = getStringValueFromJToken(configurationSettingsObject["thresholds"]["slowThreshold"], "type");
                                applicationConfiguration.SlowThreshold = getIntValueFromJToken(configurationSettingsObject["thresholds"]["slowThreshold"], "value");

                                applicationConfiguration.VerySlowThresholdType = getStringValueFromJToken(configurationSettingsObject["thresholds"]["verySlowThreshold"], "type");
                                applicationConfiguration.VerySlowThreshold = getIntValueFromJToken(configurationSettingsObject["thresholds"]["verySlowThreshold"], "value");

                                applicationConfiguration.StallThresholdType = getStringValueFromJToken(configurationSettingsObject["thresholds"]["stallThreshold"], "type");
                                applicationConfiguration.StallThreshold = getIntValueFromJToken(configurationSettingsObject["thresholds"]["stallThreshold"], "value");
                            }
                            applicationConfiguration.Percentiles = getStringValueOfObjectFromJToken(configurationSettingsObject, "percentileMetrics", true);
                            applicationConfiguration.SessionTimeout = getIntValueFromJToken(configurationSettingsObject["sessionsMonitor"], "sessionTimeoutMins");

                            applicationConfiguration.CrashThreshold = getIntValueFromJToken(configurationSettingsObject["crashAlerts"], "threshold");

                            applicationConfiguration.IsIPDisplayed = getBoolValueFromJToken(configurationSettingsObject, "ipAddressDisplayed");
                            applicationConfiguration.EnableScreenshot = getBoolValueFromJToken(configurationSettingsObject["agentConfigData"], "enableScreenshot");
                            applicationConfiguration.AutoScreenshot = getBoolValueFromJToken(configurationSettingsObject["agentConfigData"], "autoScreenshot");
                            applicationConfiguration.UseCellular = getBoolValueFromJToken(configurationSettingsObject["agentConfigData"], "screenshotUseCellular");
                        }

                        #endregion

                        #region Network Requests

                        List<MOBILENetworkRequestRule> networkRequestRulesList = new List<MOBILENetworkRequestRule>(128);

                        JObject networkRequestRulesObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.MOBILEAgentNetworkRequestsRulesDataFilePath(jobTarget));
                        if (networkRequestRulesObject != null)
                        {
                            if (isTokenPropertyNull(networkRequestRulesObject, "customNamingIncludeRules") == false)
                            {
                                JArray includeRulesArray = (JArray)networkRequestRulesObject["customNamingIncludeRules"];
                                foreach (JObject includeRuleObject in includeRulesArray)
                                {
                                    MOBILENetworkRequestRule networkRequestRule = fillNetworkRequestRule(includeRuleObject, jobTarget);
                                    if (networkRequestRule != null)
                                    {
                                        networkRequestRule.DetectionType = "INCLUDE";

                                        networkRequestRulesList.Add(networkRequestRule);
                                    }
                                }
                            }

                            if (isTokenPropertyNull(networkRequestRulesObject, "customNamingExcludeRules") == false)
                            {
                                JArray excludeRulesArray = (JArray)networkRequestRulesObject["customNamingExcludeRules"];
                                foreach (JObject excludeRuleObject in excludeRulesArray)
                                {
                                    MOBILENetworkRequestRule networkRequestRule = fillNetworkRequestRule(excludeRuleObject, jobTarget);
                                    if (networkRequestRule != null)
                                    {
                                        networkRequestRule.DetectionType = "EXCLUDE";

                                        networkRequestRulesList.Add(networkRequestRule);
                                    }
                                }
                            }
                        }

                        // Sort them
                        networkRequestRulesList = networkRequestRulesList.OrderBy(o => o.DetectionType).ThenBy(o => o.Priority).ToList();
                        FileIOHelper.WriteListToCSVFile(networkRequestRulesList, new MOBILENetworkRequestRuleReportMap(), FilePathMap.MOBILENetworkRequestRulesIndexFilePath(jobTarget));

                        loggerConsole.Info("Completed {0} Rules", networkRequestRulesList.Count);


                        #endregion

                        #region Application Settings

                        if (networkRequestRulesList != null)
                        {
                            applicationConfiguration.NumNetworkRulesInclude = networkRequestRulesList.Count(r => r.DetectionType == "INCLUDE");
                            applicationConfiguration.NumNetworkRulesExclude = networkRequestRulesList.Count(r => r.DetectionType == "EXCLUDE");
                        }

                        List<MOBILEApplicationConfiguration> applicationConfigurationsList = new List<MOBILEApplicationConfiguration>(1);
                        applicationConfigurationsList.Add(applicationConfiguration);
                        FileIOHelper.WriteListToCSVFile(applicationConfigurationsList, new MOBILEApplicationConfigurationReportMap(), FilePathMap.MOBILEApplicationConfigurationIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + applicationConfigurationsList.Count;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.MOBILEConfigurationReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.WEBConfigurationReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.MOBILEApplicationConfigurationIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MOBILEApplicationConfigurationIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MOBILEApplicationConfigurationReportFilePath(), FilePathMap.MOBILEApplicationConfigurationIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.MOBILENetworkRequestRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MOBILENetworkRequestRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MOBILENetworkRequestRulesReportFilePath(), FilePathMap.MOBILENetworkRequestRulesIndexFilePath(jobTarget));
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

        private static MOBILENetworkRequestRule fillNetworkRequestRule(JObject ruleObject, JobTarget jobTarget)
        {
            MOBILENetworkRequestRule webPageDetectionRule = new MOBILENetworkRequestRule();

            webPageDetectionRule.Controller = jobTarget.Controller;
            webPageDetectionRule.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_TIMERANGE_LAST_15_MINUTES);
            webPageDetectionRule.ApplicationName = jobTarget.Application;
            webPageDetectionRule.ApplicationID = jobTarget.ApplicationID;
            webPageDetectionRule.ApplicationLink = String.Format(DEEPLINK_MOBILE_APPLICATION, jobTarget.Controller, jobTarget.ParentApplicationID, jobTarget.ApplicationID, DEEPLINK_TIMERANGE_LAST_15_MINUTES);

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
