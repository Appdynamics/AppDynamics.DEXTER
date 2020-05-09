using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json;
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
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_APM);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_APM);

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

                            if (File.Exists(FilePathMap.APMApplicationConfigurationXMLDataFilePath(jobTarget)) == false)
                            {
                                controllerApi.Timeout = 3;
                                string applicationConfigXml = controllerApi.GetAPMConfigurationExportXML(jobTarget.ApplicationID);
                                if (applicationConfigXml != String.Empty) FileIOHelper.SaveFileToPath(applicationConfigXml, FilePathMap.APMApplicationConfigurationXMLDataFilePath(jobTarget));
                            }

                            controllerApi.PrivateApiLogin();

                            if (File.Exists(FilePathMap.APMApplicationConfigurationDetailsDataFilePath(jobTarget)) == false)
                            {
                                controllerApi.PrivateApiLogin();

                                string applicationConfigJSON = controllerApi.GetAPMConfigurationDetailsJSON(jobTarget.ApplicationID);
                                if (applicationConfigJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationConfigJSON, FilePathMap.APMApplicationConfigurationDetailsDataFilePath(jobTarget));
                            }

                            #endregion

                            #region Service Endpoints

                            // SEPs are not included in the extracted XML
                            // 2018:
                            //  There is Flash/Flex API but I am not going to call it
                            //  Otherwise there is accounts API https://docs.appdynamics.com/display/PRO45/Access+Swagger+and+Accounts+API
                            //  This includes this one:
                            //  GET /accounts/{acctId}/applications/{appId}/sep      Get all ServiceEndPointConfigs for application
                            //  It requires pretty high admin level access but hey, it's not Flash
                            // 12/19/2019:
                            //  Previous API is removed, so we're going to call the RESTUI API

                            loggerConsole.Info("Service Endpoint Detection");

                            if (File.Exists(FilePathMap.APMApplicationConfigurationSEPDetectionRulesDataFilePath(jobTarget)) == false)
                            {
                                string applicationConfigSEPJSON = controllerApi.GetAPMSEPAutodetectionConfiguration(jobTarget.ApplicationID);
                                if (applicationConfigSEPJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationConfigSEPJSON, FilePathMap.APMApplicationConfigurationSEPDetectionRulesDataFilePath(jobTarget));
                            }

                            string tiersJSON = controllerApi.GetAPMTiers(jobTarget.ApplicationID);
                            if (tiersJSON != String.Empty)
                            {
                                List<AppDRESTTier> tiersRESTList = JsonConvert.DeserializeObject<List<AppDRESTTier>>(tiersJSON);
                                if (tiersRESTList != null)
                                {
                                    loggerConsole.Info("Service Endpoint Detection for Tiers");
                                    foreach (AppDRESTTier tier in tiersRESTList)
                                    {
                                        string tierConfigOverrideJSON = controllerApi.GetAPMSEPTierConfigurationOverride(tier.id, tier.agentType);
                                        if (tierConfigOverrideJSON != String.Empty)
                                        {
                                            JObject tierOverrideObject = JObject.Parse(tierConfigOverrideJSON);
                                            if (tierOverrideObject != null)
                                            {
                                                if (getBoolValueFromJToken(tierOverrideObject, "override") == true)
                                                {
                                                    loggerConsole.Info("Service Endpoint Detection for Tier {0}", tier.name);

                                                    string tierConfigSEPJSON = controllerApi.GetAPMSEPTierAutodetectionConfiguration(tier.id, tier.agentType);
                                                    if (tierConfigSEPJSON != String.Empty && tierConfigSEPJSON != "[]" && tierConfigSEPJSON != "[ ]")
                                                    {
                                                        FileIOHelper.SaveFileToPath(tierConfigSEPJSON, FilePathMap.APMApplicationConfigurationSEPTierDetectionRulesDataFilePath(jobTarget, tier));
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    loggerConsole.Info("Explicit Service Endpoint Rules for Tiers");
                                    foreach (AppDRESTTier tier in tiersRESTList)
                                    {
                                        string tierConfigSEPJSON = controllerApi.GetAPMSEPTierRules(tier.id, tier.agentType);
                                        if (tierConfigSEPJSON != String.Empty && tierConfigSEPJSON != "[]" && tierConfigSEPJSON != "[ ]")
                                        {
                                            loggerConsole.Info("Service Endpoint Rule for Tier {0}", tier.name);

                                            FileIOHelper.SaveFileToPath(tierConfigSEPJSON, FilePathMap.APMApplicationConfigurationSEPTierExplicitRulesDataFilePath(jobTarget, tier));
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Dev mode information

                            loggerConsole.Info("Developer Mode Nodes");

                            if (File.Exists(FilePathMap.APMApplicationDeveloperModeNodesDataFilePath(jobTarget)) == false)
                            {
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
                loggerConsole.Trace("Skipping export of configuration");
            }
            return (jobConfiguration.Input.Configuration == true);
        }
    }
}
