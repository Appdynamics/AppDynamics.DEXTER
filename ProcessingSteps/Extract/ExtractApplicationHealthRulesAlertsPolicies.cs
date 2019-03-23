using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractApplicationHealthRulesAlertsPolicies : JobStepBase
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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

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

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            #region Health Rules

                            long applicationID = jobTarget.ApplicationID;
                            if (jobTarget.Type == APPLICATION_TYPE_MOBILE) applicationID = jobTarget.ParentApplicationID;

                            loggerConsole.Info("Health Rules");

                            string healthRulesXml = controllerApi.GetApplicationHealthRules(applicationID);
                            if (healthRulesXml != String.Empty) FileIOHelper.SaveFileToPath(healthRulesXml, FilePathMap.ApplicationHealthRulesDataFilePath(jobTarget));

                            controllerApi.PrivateApiLogin();

                            string healthRulesJSON = controllerApi.GetApplicationHealthRulesWithIDs(applicationID);
                            if (healthRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(healthRulesJSON, FilePathMap.ApplicationHealthRulesDetailsDataFilePath(jobTarget));

                            #endregion

                            #region Policies

                            loggerConsole.Info("Policies");

                            string policiesJSON = controllerApi.GetApplicationPolicies(applicationID);
                            if (policiesJSON != String.Empty) FileIOHelper.SaveFileToPath(policiesJSON, FilePathMap.ApplicationPoliciesDataFilePath(jobTarget));

                            #endregion

                            #region Actions

                            loggerConsole.Info("Actions");

                            string actionsJSON = controllerApi.GetApplicationActions(applicationID);
                            if (actionsJSON != String.Empty) FileIOHelper.SaveFileToPath(actionsJSON, FilePathMap.ApplicationActionsDataFilePath(jobTarget));

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
