using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using OfficeOpenXml.Table.PivotTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ReportApplicationHealthCheck : JobStepReportBase
    {
        #region Constants for report contents

        #endregion

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

            if (this.ShouldExecute(jobConfiguration) == false)
            {
                return true;
            }

            if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
            {
                return true;
            }

            #region Template comparisons 

            // Check to see if the reference application is the template or specific application, and add one of them to the 
            if (jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller == BLANK_APPLICATION_CONTROLLER &&
                jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application == BLANK_APPLICATION_APM)
            {
                jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
            }
            else
            {
                // Check if there is a valid reference application
                JobTarget jobTargetReferenceApp = jobConfiguration.Target.Where(t =>
                    t.Type == APPLICATION_TYPE_APM &&
                    String.Compare(t.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller, StringComparison.InvariantCultureIgnoreCase) == 0 &&
                    String.Compare(t.Application, jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application, StringComparison.InvariantCultureIgnoreCase) == 0).FirstOrDefault();
                if (jobTargetReferenceApp == null)
                {
                    // No valid reference, fall back to comparing against template
                    logger.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                    loggerConsole.Warn("Unable to find reference target {0}, will index default template", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);

                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller = BLANK_APPLICATION_CONTROLLER;
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application = BLANK_APPLICATION_APM;
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Type = APPLICATION_TYPE_APM;

                    jobConfiguration.Target.Add(jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
                }
            }

            #endregion

            try
            {
                loggerConsole.Info("Prepare CS Healthcheck Report File");
  
                loggerConsole.Info("List of Health Check");

                #region Preload Entity Lists

                //Read List of APM Configurations
                List<APMApplicationConfiguration> listAPMConfigurations = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMApplicationConfigurationReportFilePath(), new APMApplicationConfigurationReportMap());
                #endregion

                #region Health Check

                //Create new list to temporarily store HealthCheck entities
                //List<object> listHealthCheck = new List<object>();

                ApplicationHealthCheck healthCheck = new ApplicationHealthCheck();
                List<ApplicationHealthCheck> healthChecksList = new List<ApplicationHealthCheck>();

                if (listAPMConfigurations != null)
                {
                    foreach(APMApplicationConfiguration apmAppConfig in listAPMConfigurations)
                    {
                        healthCheck.Controller = apmAppConfig.Controller;
                        healthCheck.ApplicationName = apmAppConfig.ApplicationName;
                        healthCheck.ApplicationID = apmAppConfig.ApplicationID;
                        healthCheck.NumTiers = apmAppConfig.NumTiers;
                        healthCheck.NumBTs = apmAppConfig.NumBTs;

                        healthCheck.IsDeveloperModeEnabled = apmAppConfig.IsDeveloperModeEnabled;
                        healthCheck.IsBTLockdownEnabled = apmAppConfig.IsBTLockdownEnabled;

                        healthChecksList.Add(healthCheck);

                        //write to CSV controller name, app name, BTLockdown and others
                        /*      listHealthCheck.Add(apmAppConfig.Controller);
                              listHealthCheck.Add(apmAppConfig.ApplicationName);
                              listHealthCheck.Add(apmAppConfig.NumTiers);
                              listHealthCheck.Add(apmAppConfig.NumBTs);
                              listHealthCheck.Add(apmAppConfig.IsDeveloperModeEnabled);
                              listHealthCheck.Add(apmAppConfig.IsBTLockdownEnabled);*/

                        /*
                        if (apmAppConfig.IsBTLockdownEnabled == true)
                        {
                            //set as true

                        }
                        else
                        {
                            //set as false
                        }
                        */
                    }

                }

                //listHealthCheck.ForEach(x => { Console.Write(x); });

                #endregion

                #region Create HealthCheck CSV

                healthChecksList.Add(healthCheck);
                FileIOHelper.WriteListToCSVFile(healthChecksList, new ApplicationHealthCheckReportMap(),FilePathMap.ApplicationHealthCheckCSVFilePath());

                #endregion

                loggerConsole.Info("Finalize CS Healthcheck Report File");

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
            logger.Trace("Output.Configuration={0}", jobConfiguration.Output.Configuration);
            loggerConsole.Trace("Output.Configuration={0}", jobConfiguration.Output.Configuration);
            if (jobConfiguration.Input.Configuration == false || jobConfiguration.Output.Configuration == false)
            {
                loggerConsole.Trace("Skipping report of configuration");
            }
            return (jobConfiguration.Input.Configuration == true && jobConfiguration.Output.Configuration == true);
        }
    }
}