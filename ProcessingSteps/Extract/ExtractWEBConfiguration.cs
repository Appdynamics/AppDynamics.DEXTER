using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractWEBConfiguration : JobStepBase
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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Target step variables

                        Version version4_5_13 = new Version(4, 5, 13);
                        Version versionThisController = new Version(jobTarget.ControllerVersion);

                        #endregion

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            controllerApi.PrivateApiLogin();

                            #region Application Key

                            loggerConsole.Info("Application Key");

                            string appKeyJSON = controllerApi.GetEUMorMOBILEApplicationKey(jobTarget.ApplicationID);
                            if (appKeyJSON != String.Empty) FileIOHelper.SaveFileToPath(appKeyJSON, FilePathMap.WEBApplicationKeyDataFilePath(jobTarget));

                            #endregion

                            #region Instrumentation Options

                            loggerConsole.Info("Instrumentation Options");

                            string instrumentationJSON = controllerApi.GetEUMApplicationInstrumentationOption(jobTarget.ApplicationID);
                            if (instrumentationJSON != String.Empty) FileIOHelper.SaveFileToPath(instrumentationJSON, FilePathMap.WEBAgentConfigDataFilePath(jobTarget));

                            #endregion

                            #region Monitoring State

                            loggerConsole.Info("Monitoring State");

                            string monitoringStateJSON = controllerApi.GetEUMApplicationMonitoringState(jobTarget.ApplicationID);
                            if (monitoringStateJSON != String.Empty) FileIOHelper.SaveFileToPath(monitoringStateJSON, FilePathMap.WEBApplicationMonitoringStateDataFilePath(jobTarget));

                            #endregion

                            #region Settings

                            loggerConsole.Info("Error Detection");

                            string errorDetectionRulesJSON = controllerApi.GetEUMConfigErrorDetection(jobTarget.ApplicationID);
                            if (errorDetectionRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(errorDetectionRulesJSON, FilePathMap.WEBAgentErrorRulesDataFilePath(jobTarget));

                            loggerConsole.Info("Page Settings");

                            string pageSettingsJSON = controllerApi.GetEUMConfigSettings(jobTarget.ApplicationID);
                            if (pageSettingsJSON != String.Empty) FileIOHelper.SaveFileToPath(pageSettingsJSON, FilePathMap.WEBAgentPageSettingsRulesDataFilePath(jobTarget));

                            #endregion

                            #region Rules

                            loggerConsole.Info("Page and IFrame Rules");

                            string pageRulesJSON = controllerApi.GetEUMConfigPagesAndFrames(jobTarget.ApplicationID);
                            if (pageRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(pageRulesJSON, FilePathMap.WEBAgentPageRulesDataFilePath(jobTarget));

                            loggerConsole.Info("AJAX Rules");

                            string ajaxRulesJSON = controllerApi.GetEUMConfigAjax(jobTarget.ApplicationID);
                            if (ajaxRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(ajaxRulesJSON, FilePathMap.WEBAgentAjaxRulesDataFilePath(jobTarget));

                            loggerConsole.Info("Virtual Page Rules");

                            string virtualPageRulesJSON = controllerApi.GetEUMConfigVirtualPages(jobTarget.ApplicationID);
                            if (virtualPageRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(virtualPageRulesJSON, FilePathMap.WEBAgentVirtualPageRulesDataFilePath(jobTarget));

                            #endregion

                            #region Synthetic Jobs

                            loggerConsole.Info("Synthetic Jobs");

                            if (versionThisController >= version4_5_13)
                            {
                                string syntheticJobsJSON = controllerApi.GetWEBSyntheticJobs(jobTarget.ApplicationID);
                                if (syntheticJobsJSON != String.Empty) FileIOHelper.SaveFileToPath(syntheticJobsJSON, FilePathMap.WEBSyntheticJobsDataFilePath(jobTarget));
                            }
                            else
                            {
                                string syntheticJobsJSON = controllerApi.GetWEBSyntheticJobs_Before_4_5_13(jobTarget.ApplicationID);
                                if (syntheticJobsJSON != String.Empty) FileIOHelper.SaveFileToPath(syntheticJobsJSON, FilePathMap.WEBSyntheticJobsDataFilePath(jobTarget));
                            }

                            #endregion
                        }
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
            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            if (jobConfiguration.Input.Configuration == false)
            {
                loggerConsole.Trace("Skipping export of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }
    }
}
