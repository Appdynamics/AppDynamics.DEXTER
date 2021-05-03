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
    public class IndexBSG_Mobile : JobStepIndexBase
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

                //bool reportFolderCleaned = false;

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

                        #region MRUM

                        var bsgMrumResults = new List<BSGMrumResult>();
                        foreach (var mobileApplication in mobileApplicationList)
                        {
                            BSGMrumResult bsgMrumResult = new BSGMrumResult();
                            bsgMrumResult.Controller = mobileApplication.Controller;
                            bsgMrumResult.ApplicationName = mobileApplication.ApplicationName;
                            bsgMrumResult.ApplicationID = mobileApplication.ApplicationID;

                            bsgMrumResult.NumNetworkrequests = mobileApplication.NumNetworkRequests;

                            var mobileApplicationConfiguration = mobileApplicationConfigurationList
                                .First(it => it.ApplicationID == mobileApplication.ApplicationID);

                            bsgMrumResult.NumCustomNetworkRequestRules =
                                mobileApplicationConfiguration.NumNetworkRulesInclude;
                            
                            bsgMrumResult.MrumHealthRules = healthRulesList.FindAll(e => e.ApplicationID == mobileApplication.ApplicationID).Count;
                            bsgMrumResult.LinkedActions = actionsList.FindAll(e => e.ApplicationID == mobileApplication.ApplicationID).Count;
                            bsgMrumResult.LinkedPolicies = policiesList.FindAll(e => e.ApplicationID == mobileApplication.ApplicationID).Count;
                            
                            bsgMrumResult.WarningViolations = healthRuleViolationEventsAllList
                                .FindAll(e => e.Severity.Equals("WARNING") && e.ApplicationID == mobileApplication.ApplicationID).Count;
                            bsgMrumResult.CriticalViolations = healthRuleViolationEventsAllList
                                .FindAll(e => e.Severity.Equals("CRITICAL") && e.ApplicationID == mobileApplication.ApplicationID).Count;
                            
                            bsgMrumResults.Add(bsgMrumResult);
                        }
                        
                        #endregion
                        
                        string versionOfDEXTER = Assembly.GetEntryAssembly().GetName().Version.ToString();

                        FileIOHelper.WriteListToCSVFile(bsgMrumResults, new BSGMrumResultMap(), FilePathMap.BSGMrumResultsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = bsgMrumResults.Count;

                        #region Combine All for Report CSV

                        if (File.Exists(FilePathMap.BSGMrumResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BSGMrumResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BSGMrumResultsExcelReportFilePath(), FilePathMap.BSGMrumResultsIndexFilePath(jobTarget));
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
