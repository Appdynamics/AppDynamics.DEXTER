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
    public class IndexBSG_Database : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_DB) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_DB);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_DB);

                    return true;
                }

                //bool reportFolderCleaned = false;
                int i = 0;
                var controllers = jobConfiguration.Target.Where(t => t.Type == APPLICATION_TYPE_DB).ToList().GroupBy(t => t.Controller);

                // Process each target
                foreach (var controllerGroup in controllers)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = controllerGroup.ToList()[0];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_DB) continue;

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

                        List<BSGDatabaseResult> bsgDatabaseResults = new List<BSGDatabaseResult>();

                        #endregion

                        #region Preload all the reports that will be filtered by the subsequent entities

                        loggerConsole.Info("Entity Details Data Preloading");


                        List<DBApplicationConfiguration> dbApplicationConfigurationList = FileIOHelper.ReadListFromCSVFile(FilePathMap.DBApplicationConfigurationIndexFilePath(jobTarget), new DBApplicationConfigurationReportMap());
                        List<WEBApplicationConfiguration> webApplicationConfigurationList = FileIOHelper.ReadListFromCSVFile(FilePathMap.WEBApplicationConfigurationIndexFilePath(jobTarget), new WEBApplicationConfigurationReportMap());
                        List<HealthRule> healthRulesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthRulesIndexFilePath(jobTarget), new HealthRuleReportMap());
                        List<Policy> policiesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationPoliciesIndexFilePath(jobTarget), new PolicyReportMap());
                        List<ReportObjects.Action> actionsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationActionsIndexFilePath(jobTarget), new ActionReportMap());
                        List<WEBPage> webPageList = FileIOHelper.ReadListFromCSVFile(FilePathMap.WEBPagesIndexFilePath(jobTarget), new WEBPageReportMap());
                        List<HealthRuleViolationEvent> healthRuleViolationEventsAllList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget), new HealthRuleViolationEventReportMap());

                        loggerConsole.Info("Configuration Rules Data Preloading");

                        #endregion

                        #region Synthetics

                        foreach (var dbApplicationConfig in dbApplicationConfigurationList)
                        {
                            var dbApplication = dbApplicationConfigurationList.Find(w => w.ApplicationName == dbApplicationConfig.ApplicationName);
                            BSGDatabaseResult bsgDatabaseResult = new BSGDatabaseResult();
                            bsgDatabaseResult.ApplicationName = dbApplicationConfig.ApplicationName;
                            bsgDatabaseResult.Controller = jobTarget.Controller;
                            bsgDatabaseResult.NumDataCollectors= dbApplicationConfig.NumCollectorDefinitions;
                            bsgDatabaseResult.NumCustomMetrics = dbApplicationConfig.NumCustomMetrics;
                            var databaseHRs = healthRulesList;
                            if (databaseHRs.Count() > 0)
                            {
                                bsgDatabaseResult.NumHRs = databaseHRs.Count();
                                foreach (var policy in policiesList)
                                {
                                    foreach (var hr in databaseHRs)
                                    {
                                        if (policy.HRIDs.Contains(hr.HealthRuleID.ToString()))
                                        {
                                            bsgDatabaseResult.NumPoliciesForHRs++;
                                            if (policy.NumActions > 0 && bsgDatabaseResult.NumActionsForPolicies <= actionsList.Count())
                                            {
                                                bsgDatabaseResult.NumActionsForPolicies += policy.NumActions;

                                            }
                                            break;
                                        }

                                    }
                                }
                                foreach (var hr in databaseHRs)
                                {
                                    bsgDatabaseResult.NumWarningHRViolations += healthRuleViolationEventsAllList.FindAll(hv => hv.HealthRuleID == hr.HealthRuleID && hv.Severity == "WARNING").Count();
                                    bsgDatabaseResult.NumCriticalHRViolations += healthRuleViolationEventsAllList.FindAll(hv => hv.HealthRuleID == hr.HealthRuleID && hv.Severity == "CRITICAL").Count();
                                }

                            }
                            bsgDatabaseResults.Add(bsgDatabaseResult);

                        }
                        #endregion

                        string versionOfDEXTER = Assembly.GetEntryAssembly().GetName().Version.ToString();

                        FileIOHelper.WriteListToCSVFile(bsgDatabaseResults, new BSGDatabaseResultMap(), FilePathMap.BSGDatabaseResultsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = bsgDatabaseResults.Count;

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

                        if (File.Exists(FilePathMap.BSGDatabaseResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.BSGDatabaseResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.BSGDatabaseResultsExcelReportFilePath(), FilePathMap.BSGDatabaseResultsIndexFilePath(jobTarget));
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
