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

                
                List<ApplicationHealthCheck> healthChecksList = new List<ApplicationHealthCheck>(listAPMConfigurations.Count);

                if (listAPMConfigurations != null)
                {
                    foreach(APMApplicationConfiguration apmAppConfig in listAPMConfigurations)
                    {
                        ApplicationHealthCheck healthCheck = new ApplicationHealthCheck();

                        healthCheck.Controller = apmAppConfig.Controller;
                        healthCheck.ApplicationName = apmAppConfig.ApplicationName;
                        healthCheck.ApplicationID = apmAppConfig.ApplicationID;
                        healthCheck.NumTiers = apmAppConfig.NumTiers;
                        healthCheck.NumBTs = apmAppConfig.NumBTs;

                        healthCheck.IsDeveloperModeEnabled = apmAppConfig.IsDeveloperModeEnabled;
                        healthCheck.IsBTLockdownEnabled = apmAppConfig.IsBTLockdownEnabled;

                        healthChecksList.Add(healthCheck);
                        Console.WriteLine("****{0}****",healthCheck);
                        

                        #region TO DELETE
                        //write to CSV controller name, app name, BTLockdown and others
                        /*    listHealthCheck.Add(apmAppConfig.Controller);
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
                        #endregion
                    }
                    FileIOHelper.WriteListToCSVFile(healthChecksList, new ApplicationHealthCheckReportMap(), FilePathMap.ApplicationHealthCheckCSVFilePath());
                }

                //listHealthCheck.ForEach(x => { Console.WriteLine(x); });

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