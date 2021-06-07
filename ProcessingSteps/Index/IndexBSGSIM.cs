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
    public class IndexBSG_SIM : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_SIM) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_SIM);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_SIM);

                    return true;
                }

                //bool reportFolderCleaned = false;

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_SIM) continue;

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

                        var bsgSimResults = new List<BSGSimResult>();
                        var bsgContainerResults = new List<BSGContainersResult>();

                        #endregion

                        #region Preload all the reports that will be filtered by the subsequent entities

                        loggerConsole.Info("Entity Details Data Preloading");

                        
                        List<SIMNode> simNodes = FileIOHelper.ReadListFromCSVFile(FilePathMap.SIMNodesIndexFilePath(jobTarget), new SIMNodeReportMap());
                        
                        List<HealthRule> healthRulesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget), new HealthRuleReportMap());
                        List<Policy> policiesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget), new PolicyReportMap());
                        List<ReportObjects.Action> actionsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationActionsIndexFilePath(jobTarget), new ActionReportMap());
                        List<HealthRuleViolationEvent> healthRuleViolationEventsAllList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget), new HealthRuleViolationEventReportMap());
                        List<SIMMachineContainer> machineContainersList = FileIOHelper.ReadListFromCSVFile<SIMMachineContainer>(FilePathMap.SIMMachineContainersIndexFilePath(jobTarget), new SIMMachineContainerReportMap());
                        
                        loggerConsole.Info("Configuration Rules Data Preloading");

                        #endregion

                        #region SIM
                        
                        BSGSimResult bsgSimResult = new BSGSimResult();
                        
                        bsgSimResult.Controller = jobTarget.Controller;
                        bsgSimResult.ApplicationName = jobTarget.Application;
                        
                        bsgSimResult.NumHRs = healthRulesList.Count;
                        bsgSimResult.NumActions = actionsList.Count;
                        bsgSimResult.NumPolicies = policiesList.Count;

                        bsgSimResult.NumWarningViolations = healthRuleViolationEventsAllList
                            .FindAll(e => e.Severity.Equals("WARNING")).Count;
                        bsgSimResult.NumCriticalViolations = healthRuleViolationEventsAllList
                            .FindAll(e => e.Severity.Equals("CRITICAL")).Count;
                        
                        bsgSimResults.Add(bsgSimResult);
                        
                        #endregion

                        #region Containers

                        BSGContainersResult bsgContainersResult = new BSGContainersResult();
                        bsgContainersResult.Controller = jobTarget.Controller;
                        bsgContainersResult.ApplicationName = jobTarget.Application;
                        bsgContainersResult.ApplicationID = jobTarget.ApplicationID;
                        bsgContainersResult.NumContainersMonitored = machineContainersList.Count;
                        
                        bsgContainersResult.NumHRs = healthRulesList.Count;
                        bsgContainersResult.NumActions = actionsList.Count;
                        bsgContainersResult.NumPolicies = policiesList.Count;

                        bsgContainersResult.NumWarningViolations = healthRuleViolationEventsAllList
                            .FindAll(e => e.Severity.Equals("WARNING")).Count;
                        bsgContainersResult.NumCriticalViolations = healthRuleViolationEventsAllList
                            .FindAll(e => e.Severity.Equals("CRITICAL")).Count;
                        
                        bsgContainerResults.Add(bsgContainersResult);

                        #endregion

                        string versionOfDEXTER = Assembly.GetEntryAssembly().GetName().Version.ToString();

                        FileIOHelper.WriteListToCSVFile(bsgSimResults, new BSGSimResultMap(), FilePathMap.BSGSimResultsIndexFilePath(jobTarget));
                        FileIOHelper.WriteListToCSVFile(bsgContainerResults, new BSGContainersResultMap(), FilePathMap.BSGContainersResultsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = bsgSimResults.Count;

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        //if (reportFolderCleaned == false)
                        //{
                        //    FileIOHelper.DeleteFolder(FilePathMap.BSGReportFolderPath());
                        //    Thread.Sleep(1000);
                        //    FileIOHelper.CreateFolder(FilePathMap.BSGReportFolderPath());
                        //    reportFolderCleaned = true;
                        //}

                        // Append all the individual report files into one

                        if (File.Exists(FilePathMap.BSGSimResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BSGSimResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BSGSimResultsExcelReportFilePath(), FilePathMap.BSGSimResultsIndexFilePath(jobTarget));
                        }
                        
                        if (File.Exists(FilePathMap.BSGContainersResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BSGContainersResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BSGContainersResultsExcelReportFilePath(), FilePathMap.BSGContainersResultsIndexFilePath(jobTarget));
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
