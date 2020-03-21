using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractBIQEntities : JobStepBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_BIQ) == 0)
                {
                    return true;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_BIQ) continue;

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

                            controllerApi.PrivateApiLogin();

                            #region Prepare time range

                            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                            #endregion

                            #region Saved Searches

                            loggerConsole.Info("Saved Searches");

                            string savedSearchesJSON = controllerApi.GetBIQSearches();
                            if (savedSearchesJSON != String.Empty) FileIOHelper.SaveFileToPath(savedSearchesJSON, FilePathMap.BIQSearchesDataFilePath(jobTarget));

                            #endregion

                            #region Saved Metrics

                            loggerConsole.Info("Saved Metrics");

                            string savedMetricsJSON = controllerApi.GetBIQMetrics();
                            if (savedMetricsJSON != String.Empty) FileIOHelper.SaveFileToPath(savedMetricsJSON, FilePathMap.BIQMetricsDataFilePath(jobTarget));

                            #endregion

                            #region Business Journeys

                            loggerConsole.Info("Business Journeys");

                            string businessJourneysJSON = controllerApi.GetBIQBusinessJourneys(fromTimeUnix, toTimeUnix);
                            if (businessJourneysJSON != String.Empty) FileIOHelper.SaveFileToPath(businessJourneysJSON, FilePathMap.BIQBusinessJourneysDataFilePath(jobTarget));

                            #endregion

                            #region Experience Levels

                            loggerConsole.Info("Experience Levels");

                            string experienceLevelsJSON = controllerApi.GetBIQExperienceLevels();
                            if (experienceLevelsJSON != String.Empty) FileIOHelper.SaveFileToPath(experienceLevelsJSON, FilePathMap.BIQExperienceLevelsDataFilePath(jobTarget));

                            #endregion

                            #region Custom Schemas

                            loggerConsole.Info("Custom Schemas");

                            string customSchemasJSON = controllerApi.GetBIQCustomSchemas();
                            if (customSchemasJSON != String.Empty) FileIOHelper.SaveFileToPath(customSchemasJSON, FilePathMap.BIQCustomSchemasDataFilePath(jobTarget));

                            List<string> analyticsSchemas = new List<string>(BIQ_SCHEMA_TYPES.Count + 10);
                            analyticsSchemas.AddRange(BIQ_SCHEMA_TYPES);

                            if (customSchemasJSON != String.Empty)
                            {
                                JObject customSchemasContainer = JObject.Parse(customSchemasJSON);
                                if (customSchemasContainer != null)
                                {
                                    JArray customSchemas = JArray.Parse(getStringValueFromJToken(customSchemasContainer, "rawResponse"));
                                    foreach (JToken customSchemaToken in customSchemas)
                                    {
                                        string schemaName = customSchemaToken.ToString();

                                        analyticsSchemas.Add(schemaName);
                                    }
                                }
                            }

                            #endregion

                            #region Schema Fields

                            foreach (string schemaName in analyticsSchemas)
                            {
                                loggerConsole.Info("Schema {0}", schemaName);

                                string schemaFieldsJSON = controllerApi.GetBIQSchemaFields(schemaName);
                                if (schemaFieldsJSON != String.Empty) FileIOHelper.SaveFileToPath(schemaFieldsJSON, FilePathMap.BIQSchemaFieldsDataFilePath(jobTarget, schemaName));
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
            logger.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            loggerConsole.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            if (programOptions.LicensedReports.DetectedEntities == false)
            {
                loggerConsole.Warn("Not licensed for detected entities");
                return false;
            }

            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping export of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
        }
    }
}
