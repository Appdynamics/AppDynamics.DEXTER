using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractAPMConfiguration : JobStepBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    return true;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

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

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            #region Application

                            loggerConsole.Info("Application Configuration");

                            if (File.Exists(FilePathMap.APMApplicationConfigurationDataFilePath(jobTarget)) == false)
                            {
                                controllerApi.Timeout = 3;
                                string applicationConfigXml = controllerApi.GetAPMConfiguration(jobTarget.ApplicationID);
                                if (applicationConfigXml != String.Empty) FileIOHelper.SaveFileToPath(applicationConfigXml, FilePathMap.APMApplicationConfigurationDataFilePath(jobTarget));
                            }
                            #endregion

                            #region Service Endpoints

                            // SEPs are not included in the extracted XML
                            // There is Flash/Flex API but I am not going to call it
                            // Otherwise there is accounts API https://docs.appdynamics.com/display/PRO45/Access+Swagger+and+Accounts+API
                            // This includes this one:
                            // GET /accounts/{acctId}/applications/{appId}/sep      Get all ServiceEndPointConfigs for application
                            // It requires pretty high admin level access but hey, it's not Flash

                            loggerConsole.Info("Service Endpoint Configuration");

                            if (File.Exists(FilePathMap.APMApplicationConfigurationSEPDataFilePath(jobTarget)) == false)
                            {
                                string myAccountJSON = controllerApi.GetAccountsMyAccount();
                                if (myAccountJSON != String.Empty)
                                {
                                    JObject myAccount = JObject.Parse(myAccountJSON);
                                    if (myAccount != null)
                                    {
                                        long accountID = -1;
                                        try { accountID = (long)myAccount["id"]; } catch { }

                                        if (accountID != -1)
                                        {
                                            string applicationConfigSEPJSON = controllerApi.GetAPMSEPConfiguration(accountID, jobTarget.ApplicationID);
                                            if (applicationConfigSEPJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationConfigSEPJSON, FilePathMap.APMApplicationConfigurationSEPDataFilePath(jobTarget));
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Dev mode information

                            loggerConsole.Info("Developer Mode Nodes");

                            if (File.Exists(FilePathMap.APMApplicationDeveloperModeNodesDataFilePath(jobTarget)) == false)
                            {
                                controllerApi.PrivateApiLogin();

                                string devModeConfigurationJSON = controllerApi.GetAPMDeveloperModeConfiguration(jobTarget.ApplicationID);
                                if (devModeConfigurationJSON != String.Empty) FileIOHelper.SaveFileToPath(devModeConfigurationJSON, FilePathMap.APMApplicationDeveloperModeNodesDataFilePath(jobTarget));
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
