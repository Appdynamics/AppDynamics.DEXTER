using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexBSG_Web : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_WEB) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_WEB);
                    return true;
                }

                //bool reportFolderCleaned = false;

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

                        #region Target step variables

                        List<BSGSyntheticsResult> bsgSyntheticsResults = new List<BSGSyntheticsResult>();

                        #endregion

                        #region Preload all the reports that will be filtered by the subsequent entities

                        loggerConsole.Info("Entity Details Data Preloading");

                        List<ControllerSummary> controllerSummariesList = FileIOHelper.ReadListFromCSVFile<ControllerSummary>(FilePathMap.ControllerSummaryIndexFilePath(jobTarget), new ControllerSummaryReportMap());
                        List<ControllerSetting> controllerSettingsList = FileIOHelper.ReadListFromCSVFile<ControllerSetting>(FilePathMap.ControllerSettingsIndexFilePath(jobTarget), new ControllerSettingReportMap());
                        
                        List<WEBApplication> webApplicationList = FileIOHelper.ReadListFromCSVFile(FilePathMap.WEBApplicationsIndexFilePath(jobTarget), new WEBApplicationReportMap());
                        List<WEBApplicationConfiguration> webApplicationConfigurationList = FileIOHelper.ReadListFromCSVFile(FilePathMap.WEBApplicationConfigurationIndexFilePath(jobTarget), new WEBApplicationConfigurationReportMap());
                        
                        List<MOBILEApplication> mobileApplicationList = FileIOHelper.ReadListFromCSVFile<MOBILEApplication>(FilePathMap.MOBILEApplicationsIndexFilePath(jobTarget), new MOBILEApplicationReportMap());
                        List<MOBILEApplicationConfiguration> mobileApplicationConfigurationList = FileIOHelper.ReadListFromCSVFile<MOBILEApplicationConfiguration>(FilePathMap.MOBILEApplicationConfigurationIndexFilePath(jobTarget), new MOBILEApplicationConfigurationReportMap());
                        
                        List<HealthRule> healthRulesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget), new HealthRuleReportMap());
                        List<Policy> policiesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget), new PolicyReportMap());
                        List<ReportObjects.Action> actionsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationActionsIndexFilePath(jobTarget), new ActionReportMap());
                        List<WEBPage> webPageList = FileIOHelper.ReadListFromCSVFile(FilePathMap.WEBPagesIndexFilePath(jobTarget), new WEBPageReportMap());
                        List<HealthRuleViolationEvent> healthRuleViolationEventsAllList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget), new HealthRuleViolationEventReportMap());

                        loggerConsole.Info("Configuration Rules Data Preloading");

                        #endregion

                        #region Synthetics

                        foreach (var webApplicationConfig in webApplicationConfigurationList)
                        {
                            var webApplication = webApplicationList.Find(w => w.ApplicationName == webApplicationConfig.ApplicationName);
                            BSGSyntheticsResult bsgSyntheticsResult = new BSGSyntheticsResult();
                            bsgSyntheticsResult.ApplicationName = webApplicationConfig.ApplicationName;
                            bsgSyntheticsResult.Controller = jobTarget.Controller;
                            bsgSyntheticsResult.NumSyntheticJobs = webApplicationConfig.NumSyntheticJobs;
                            bsgSyntheticsResult.NumSyntheticJobsWithData = webPageList.FindAll(w => w.IsSynthetic == true).Count();
                            var syntheticHRs = healthRulesList.FindAll(h => h.HRRuleType == "EUMPAGES" && (h.Warn1MetricName.ToLower().Contains("Synthetic") || h.Warn2MetricName.ToLower().Contains("Synthetic") ||
                                h.Warn3MetricName.ToLower().Contains("Synthetic") || h.Warn4MetricName.ToLower().Contains("Synthetic") ||
                                h.Warn5MetricName.ToLower().Contains("Synthetic") || h.WarningConditionRawValue.ToLower().Contains("Synthetic") ||
                                h.Crit1MetricName.ToLower().Contains("Synthetic") || h.Crit2MetricName.ToLower().Contains("Synthetic") ||
                                h.Crit3MetricName.ToLower().Contains("Synthetic") || h.Crit4MetricName.ToLower().Contains("Synthetic") || h.Crit5MetricName.ToLower().Contains("Synthetic") || h.CriticalConditionRawValue.ToLower().Contains("Synthetic")));
                            if (syntheticHRs.Count() > 0)
                            {
                                bsgSyntheticsResult.NumHRsWithSynthetics = syntheticHRs.Count();
                                foreach (var policy in policiesList)
                                {
                                    foreach (var hr in syntheticHRs)
                                    {
                                        if (policy.HRIDs.Contains(hr.HealthRuleID.ToString()))
                                        {
                                            bsgSyntheticsResult.NumPoliciesForHRs++;
                                            if (policy.NumActions > 0)
                                            {
                                                bsgSyntheticsResult.NumActionsForPolicies += policy.NumActions;

                                            }
                                            break;
                                        }

                                    }
                                }
                                foreach (var hr in syntheticHRs)
                                {
                                    bsgSyntheticsResult.NumWarningHRViolations += healthRuleViolationEventsAllList.FindAll(hv => hv.HealthRuleID == hr.HealthRuleID && hv.Severity == "WARNING").Count();
                                    bsgSyntheticsResult.NumCriticalHRViolations += healthRuleViolationEventsAllList.FindAll(hv => hv.HealthRuleID == hr.HealthRuleID && hv.Severity == "CRITICAL").Count();
                                }

                            }
                            bsgSyntheticsResults.Add(bsgSyntheticsResult);
                        }
                        #endregion

                        #region BRUM

                        var bsgBrumResults = new List<BSGBrumResult>();
                        foreach (var webApplication in webApplicationList)
                        {
                            BSGBrumResult bsgBrumResult = new BSGBrumResult();
                            bsgBrumResult.Controller = webApplication.Controller;
                            bsgBrumResult.ApplicationName = webApplication.ApplicationName;
                            bsgBrumResult.ApplicationID = webApplication.ApplicationID;
                            bsgBrumResult.DataReported = webApplication.NumActivity > 0;
                            bsgBrumResult.NumPages = webApplication.NumPages;
                            bsgBrumResult.NumAjax = webApplication.NumAJAXRequests;
                            
                            // following two are not available, need to call API
                            // controller/restui/pageList/getEumPageListViewData
                            // bsgBrumResult.PageLimitHit = false;
                            // bsgBrumResult.AjaxLimitHit = false;

                            var webApplicationConfiguration = webApplicationConfigurationList
                                .First(it => it.ApplicationID == webApplication.ApplicationID);
                            bsgBrumResult.NumCustomPageRules = webApplicationConfiguration.NumPageRulesInclude;
                            bsgBrumResult.NumCustomAjaxRules = webApplicationConfiguration.NumAJAXRulesInclude;
                            
                            bsgBrumResult.BrumHealthRules = healthRulesList.FindAll(e => e.ApplicationID == webApplication.ApplicationID).Count;
                            bsgBrumResult.LinkedActions = actionsList.FindAll(e => e.ApplicationID == webApplication.ApplicationID).Count;
                            bsgBrumResult.LinkedPolicies = policiesList.FindAll(e => e.ApplicationID == webApplication.ApplicationID).Count;
                            
                            bsgBrumResult.WarningViolations = healthRuleViolationEventsAllList
                                .FindAll(e => e.Severity.Equals("WARNING") && e.ApplicationID == webApplication.ApplicationID).Count;
                            bsgBrumResult.CriticalViolations = healthRuleViolationEventsAllList
                                .FindAll(e => e.Severity.Equals("CRITICAL") && e.ApplicationID == webApplication.ApplicationID).Count;
                            
                            bsgBrumResults.Add(bsgBrumResult);
                        }

                        #endregion

                        string versionOfDEXTER = Assembly.GetEntryAssembly().GetName().Version.ToString();

                        FileIOHelper.WriteListToCSVFile(bsgSyntheticsResults, new BSGSyntheticsResultMap(), FilePathMap.BSGSyntheticsResultsIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(bsgBrumResults, new BSGBrumResultMap(), FilePathMap.BSGBrumResultsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = bsgSyntheticsResults.Count;

                        #region Combine All for Report CSV

                        if (File.Exists(FilePathMap.BSGSyntheticsResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BSGSyntheticsResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BSGSyntheticsResultsExcelReportFilePath(), FilePathMap.BSGSyntheticsResultsIndexFilePath(jobTarget));
                        }
                        
                        if (File.Exists(FilePathMap.BSGBrumResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BSGBrumResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BSGBrumResultsExcelReportFilePath(), FilePathMap.BSGBrumResultsIndexFilePath(jobTarget));
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
            logger.Trace("LicensedReports.BSG={0}", programOptions.LicensedReports.BSG);
            loggerConsole.Trace("LicensedReports.BSG={0}", programOptions.LicensedReports.BSG);
            if (programOptions.LicensedReports.BSG == false)
            {
                loggerConsole.Warn("Not licensed for BSG");
                return false;
            }

            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            logger.Trace("Output.BSG={0}", jobConfiguration.Output.BSG);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Output.BSG={0}", jobConfiguration.Output.BSG);
            if (jobConfiguration.Input.DetectedEntities == false ||
                jobConfiguration.Input.Metrics == false ||
                jobConfiguration.Input.Configuration == false ||
                jobConfiguration.Output.BSG == false)
            {
                loggerConsole.Trace("Skipping index of BSG");
            }
            return (jobConfiguration.Input.DetectedEntities == true &&
                jobConfiguration.Input.Metrics == true &&
                jobConfiguration.Input.Configuration == true &&
                jobConfiguration.Output.BSG == true);
        }


    }
}
