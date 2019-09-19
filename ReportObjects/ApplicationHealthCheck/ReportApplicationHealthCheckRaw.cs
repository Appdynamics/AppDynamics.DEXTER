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
    public class ReportApplicationHealthCheckRaw : JobStepReportBase
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
                /*REMOVE HARDCODED: Variables to be read from AppHealthCheckProperties.csv*/
                /**********************************************/
                int BTErrorRateUpper = 80;
                int BTErrorRateLower = 60;

                int InfoPointUpper = 3;
                int InfoPointLower = 1;
                int DataCollectorUpper = 3;
                int DataCollectorLower = 1;

                int HRViolationUpper = 100;
                int HRViolationLower = 50;
                int PolicyUpper = 2;
                int PolicyLower = 1;

                double LatestAppAgentVersion = 4.5;
                double LatestMachineAgentVersion = 4.5;
                int MachineAgentEnabledUpper = 80;
                int MachineAgentEnabledLower = 60;
                int TierEnabledPercentUpper = 90;
                int TierEnabledPercentLower = 70;
                int NodeActivePercentUpper = 90;
                int NodeActivePercentLower = 70;
                
                /**********************************************/

                loggerConsole.Info("Prepare Application Healthcheck Summary File");
  
                loggerConsole.Info("List of Health Check");

                #region Preload Entity Lists
                List<ApplicationHealthCheckComparison> AppHealthCheckComparisonList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthCheckComparisonMappingFilePath(), new ApplicationHealthCheckComparisonMap());

                //Read List of Configurations from CSV files
                List<APMApplicationConfiguration> APMApplicationConfigurationsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMApplicationConfigurationReportFilePath(), new APMApplicationConfigurationReportMap());
                List <HTTPDataCollector> httpDataCollectorsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMHttpDataCollectorsReportFilePath(), new HTTPDataCollectorReportMap());
                List<MethodInvocationDataCollector> methodInvocationDataCollectorsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMMethodInvocationDataCollectorsReportFilePath(), new MethodInvocationDataCollectorReportMap());
                List<Policy> policiesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationPoliciesReportFilePath(), new PolicyReportMap());
                List<APMTier> apmTierList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMTiersReportFilePath(), new APMTierReportMap());
                List<APMNode> apmNodeList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMNodesReportFilePath(), new APMNodeReportMap());


                //List<> BTOverflowList

                #endregion

                #region Add APMConfigurations into HealthCheckList

                List<ApplicationHealthCheck> healthChecksList = new List<ApplicationHealthCheck>(APMApplicationConfigurationsList.Count);

                if (APMApplicationConfigurationsList != null)
                {
                    foreach (APMApplicationConfiguration apmAppConfig in APMApplicationConfigurationsList)
                    {
                        ApplicationHealthCheck healthCheck = new ApplicationHealthCheck();

                        healthCheck.Controller = apmAppConfig.Controller;
                        healthCheck.ApplicationName = apmAppConfig.ApplicationName;
                        healthCheck.ApplicationID = apmAppConfig.ApplicationID;
                        healthCheck.NumTiers = apmAppConfig.NumTiers;
                        healthCheck.NumBTs = apmAppConfig.NumBTs;

                        healthCheck.IsDeveloperModeEnabled = apmAppConfig.IsDeveloperModeEnabled;
                        healthCheck.IsBTLockdownEnabled = apmAppConfig.IsBTLockdownEnabled;

                        //Add InfoPoints score to Health Check
                        if (apmAppConfig.NumInfoPointRules > InfoPointUpper)
                            healthCheck.NumInfoPoints = "PASS";
                        else if (apmAppConfig.NumInfoPointRules < InfoPointLower)
                            healthCheck.NumInfoPoints = "FAIL";
                        else healthCheck.NumInfoPoints = "WARN";

                        //Add Data collector score to Health Check
                        //Get count of HTTP & MIDC data collectors where IsAssignedToBTs is true
                        List<HTTPDataCollector> httpDataCollectorThisAppList = null;
                        List<MethodInvocationDataCollector> methodInvocationDataCollectorThisAppList = null;

                        if (httpDataCollectorsList != null) httpDataCollectorThisAppList = httpDataCollectorsList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<HTTPDataCollector>();
                        int HTTPDataCollectorCount = httpDataCollectorThisAppList.Count(b => b.IsAssignedToBTs == true);

                        if (methodInvocationDataCollectorsList != null) methodInvocationDataCollectorThisAppList = methodInvocationDataCollectorsList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<MethodInvocationDataCollector>();
                        int MethodInvocationDataCollectorCount = methodInvocationDataCollectorThisAppList.Count(b => b.IsAssignedToBTs == true);

                        if ((HTTPDataCollectorCount + MethodInvocationDataCollectorCount) > DataCollectorUpper)
                            healthCheck.NumDataCollectorsEnabled = "PASS";
                        else if ((HTTPDataCollectorCount + MethodInvocationDataCollectorCount) < DataCollectorLower)
                            healthCheck.NumDataCollectorsEnabled = "FAIL";
                        else healthCheck.NumDataCollectorsEnabled = "WARN";

                        //Add Policy To Action into HealthCheckList
                        //If (policy active & has associated actions): Add count of policies to healthcheck list
                        List<Policy> policiesThisAppList = null;

                        if (policiesList != null) policiesThisAppList = policiesList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<Policy>();
                        int PolicyCount = policiesThisAppList.Count(p => p.IsEnabled == true && p.NumActions > 0);

                        if (PolicyCount > PolicyUpper)
                            healthCheck.IsPoliciesAndActionsEnabled = "PASS";
                        else if (PolicyCount < PolicyLower)
                            healthCheck.IsPoliciesAndActionsEnabled = "FAIL";
                        else healthCheck.IsPoliciesAndActionsEnabled = "WARN";

                        //Add PercentActiveTiers to HealthCheckList
                        //CountOfTiersWithNumNodesGreaterThanZero/CountOfTiers *100
                        List<APMTier> apmTierThisAppList = null;
                        if (apmTierList != null) apmTierThisAppList = apmTierList.Where(t => t.Controller.StartsWith(apmAppConfig.Controller) == true && t.ApplicationName == apmAppConfig.ApplicationName).ToList<APMTier>();
                        int TierActiveCount = apmTierThisAppList.Count(t => t.NumNodes > 0);
                        int TierCount = apmTierThisAppList.Count();

                        int ActiveTierPercent = 0;
                        if (apmTierThisAppList.Count(t => t.NumNodes > 0) > 0)
                            ActiveTierPercent = apmTierThisAppList.Count(t => t.NumNodes > 0) * 100 / apmTierThisAppList.Count();

                        if (ActiveTierPercent > TierEnabledPercentUpper)
                            healthCheck.PercentActiveTiers = "PASS";
                        else if (ActiveTierPercent < TierEnabledPercentLower)
                            healthCheck.PercentActiveTiers = "FAIL";
                        else healthCheck.PercentActiveTiers = "WARN";

                        Console.WriteLine("{0} Active Tiers: {1}, Total: {2}", apmAppConfig.ApplicationName, TierActiveCount, TierCount);

                        //Add PercentActiveNodes to HealthCheckList
                        //TO DO






                        //Add properties to HealthCheckList
                        healthChecksList.Add(healthCheck);
                    }
                }
                #endregion

                #region TODO Add BTOverflow into HealthCheckList
                /*If BTOverflow count > 0, add FAIL to healthchecklist*/
                #endregion

                

                #region Write HealthChecks to CSV

                if (healthChecksList.Count != 0)
                {
                    FileIOHelper.WriteListToCSVFile(healthChecksList, new ApplicationHealthCheckReportMap(), FilePathMap.ApplicationHealthCheckCSVFilePath());
                }

                loggerConsole.Info("Finalize Application Healthcheck Summary File");

                #endregion

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