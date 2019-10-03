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
                #region HARDCODED Variables
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

                string LatestAppAgentVersion = "4.5";
                string LatestMachineAgentVersion = "4.5";
                int AllowedVersionsBehind = 2;
                int AgentOldPercent = 25;
                int MachineAgentEnabledUpper = 80;
                int MachineAgentEnabledLower = 60;
                int TierActivePercentUpper = 90;
                int TierActivePercentLower = 70;
                int NodeActivePercentUpper = 90;
                int NodeActivePercentLower = 70;

                /**********************************************/
                #endregion

                loggerConsole.Info("Prepare Application Health Check Summary File");

                loggerConsole.Info("Building Health Check List");

                #region Preload Entity Lists
                List<ApplicationHealthCheckComparison> AppHealthCheckComparisonList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationHealthCheckComparisonMappingFilePath(), new ApplicationHealthCheckComparisonMap());

                //Read List of Configurations from CSV files
                List<APMApplicationConfiguration> APMApplicationConfigurationsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMApplicationConfigurationReportFilePath(), new APMApplicationConfigurationReportMap());
                List<HTTPDataCollector> httpDataCollectorsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMHttpDataCollectorsReportFilePath(), new HTTPDataCollectorReportMap());
                List<MethodInvocationDataCollector> methodInvocationDataCollectorsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMMethodInvocationDataCollectorsReportFilePath(), new MethodInvocationDataCollectorReportMap());
                List<Policy> policiesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationPoliciesReportFilePath(), new PolicyReportMap());
                List<APMTier> apmTierList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMTiersReportFilePath(), new APMTierReportMap());
                List<APMNode> apmNodeList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMNodesReportFilePath(), new APMNodeReportMap());
                List<APMBackend> backendList = FileIOHelper.ReadListFromCSVFile(FilePathMap.APMBackendsReportFilePath(), new APMBackendReportMap());
                List<APMBusinessTransaction> apmEntitiesList = FileIOHelper.ReadListFromCSVFile(FilePathMap.EntitiesFullReportFilePath(APMBusinessTransaction.ENTITY_FOLDER), new BusinessTransactionMetricReportMap());
                List<ApplicationEventSummary> appEventsList = FileIOHelper.ReadListFromCSVFile(FilePathMap.ApplicationEventsSummaryReportFilePath(), new ApplicationEventSummaryReportMap());

                #endregion

                #region Fill HealthCheckList

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

                        //Add BTLockdownOn to Health Check
                        healthCheck.BTLockdownEnabled = apmAppConfig.IsBTLockdownEnabled;

                        //Add DevModeEnabled to Health Check
                        healthCheck.DeveloperModeOff = GetHealthCheckScore(apmAppConfig.IsDeveloperModeEnabled == false, apmAppConfig.IsDeveloperModeEnabled == true);

                        //Add InfoPoints score to Health Check
                        healthCheck.NumInfoPoints = GetHealthCheckScore(apmAppConfig.NumInfoPointRules > InfoPointUpper, apmAppConfig.NumInfoPointRules < InfoPointLower);

                        //Add Data collector score to Health Check
                        //Get count of HTTP & MIDC data collectors where IsAssignedToBTs is true
                        List<HTTPDataCollector> httpDataCollectorThisAppList = null;
                        List<MethodInvocationDataCollector> methodInvocationDataCollectorThisAppList = null;

                        if (httpDataCollectorsList != null) httpDataCollectorThisAppList = httpDataCollectorsList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<HTTPDataCollector>();
                        int HTTPDataCollectorCount = httpDataCollectorThisAppList.Count(b => b.IsAssignedToBTs == true);

                        if (methodInvocationDataCollectorsList != null) methodInvocationDataCollectorThisAppList = methodInvocationDataCollectorsList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<MethodInvocationDataCollector>();
                        int MethodInvocationDataCollectorCount = methodInvocationDataCollectorThisAppList.Count(b => b.IsAssignedToBTs == true);

                        int CombinedDataCollectorCount = HTTPDataCollectorCount + MethodInvocationDataCollectorCount;
                        healthCheck.NumDataCollectorsEnabled = GetHealthCheckScore(CombinedDataCollectorCount > DataCollectorUpper, CombinedDataCollectorCount < DataCollectorLower);

                        //Add Policy To Action into Health Check
                        //If (policy active & has associated actions): Add count of policies to healthcheck list
                        List<Policy> policiesThisAppList = null;

                        if (policiesList != null) policiesThisAppList = policiesList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<Policy>();
                        int PolicyCount = policiesThisAppList.Count(p => p.IsEnabled == true && p.NumActions > 0);
                        healthCheck.PoliciesActionsEnabled = GetHealthCheckScore(PolicyCount > PolicyUpper, PolicyCount < PolicyLower);

                        //Add TiersActivePercent to Health Check
                        //CountOfTiersWithNumNodesGreaterThanZero/CountOfTiers *100
                        int ActiveTierPercent = 0;
                        List<APMTier> apmTierThisAppList = null;

                        if (apmTierList != null) apmTierThisAppList = apmTierList.Where(t => t.Controller.StartsWith(apmAppConfig.Controller) == true && t.ApplicationName == apmAppConfig.ApplicationName).ToList<APMTier>();
                        if (apmTierThisAppList.Count(t => t.NumNodes > 0) > 0)
                            ActiveTierPercent = (int)Math.Round((double)(apmTierThisAppList.Count(t => t.NumNodes > 0) * 100) / apmTierThisAppList.Count());

                        healthCheck.TiersActivePercent = GetHealthCheckScore(ActiveTierPercent > TierActivePercentUpper, ActiveTierPercent < TierActivePercentLower);

                        //Add BackendOverflow to Health Check
                        //If BackendOverflow contains "All Other Traffic": Fail
                        List<APMBackend> backendThisAppList = null;
                        if (backendList != null) backendThisAppList = backendList.Where(c => c.Controller.StartsWith(apmAppConfig.Controller) == true && c.ApplicationName == apmAppConfig.ApplicationName).ToList<APMBackend>();
                        if (backendThisAppList != null)
                        {
                            var getBackendOverflow = backendThisAppList.FirstOrDefault(o => o.Prop1Name.Contains("Backend limit reached"));
                            healthCheck.BackendOverflow = GetHealthCheckScore(getBackendOverflow == null, getBackendOverflow != null);
                        }

                        //Add NodesActivePercent & MachineAgentEnabledPercent to Health Check
                        int ActiveNodePercent = 0;
                        int ActiveMachineAgentPercent = 0;
                        List<APMNode> apmNodesThisAppList = null;

                        if (apmNodeList != null) apmNodesThisAppList = apmNodeList.Where(t => t.Controller.StartsWith(apmAppConfig.Controller) == true && t.ApplicationName == apmAppConfig.ApplicationName).ToList<APMNode>();
                        int NodeActiveCount = apmNodesThisAppList.Count(n => n.AgentPresent == true && n.IsDisabled == false);
                        int MachineAgentPresentCount = apmNodesThisAppList.Count(n => n.MachineAgentPresent == true && n.IsDisabled == false);

                        if (NodeActiveCount > 0)
                            ActiveNodePercent = (int)Math.Round((double)(NodeActiveCount * 100) / apmNodesThisAppList.Count());
                        healthCheck.NodesActivePercent = GetHealthCheckScore(ActiveNodePercent > NodeActivePercentUpper, ActiveNodePercent < NodeActivePercentLower);

                        if (MachineAgentPresentCount > 0)
                            ActiveMachineAgentPercent = (int)Math.Round((double)(MachineAgentPresentCount * 100) / apmNodesThisAppList.Count());
                        healthCheck.MachineAgentEnabledPercent = GetHealthCheckScore(ActiveNodePercent > MachineAgentEnabledUpper, ActiveNodePercent < MachineAgentEnabledLower);

                        //Add AppAgentVersion & MachineAgentVersion to Health Check
                        //Count Active Agents with versions older than 2. Compare with total agent count as percent
                        int LatestAppAgentCount = apmNodesThisAppList.Count(c => c.AgentVersion.Contains(LatestAppAgentVersion) && c.IsDisabled == false);
                        int AcceptableAppAgentCount = apmNodesThisAppList.Count(c => c.AgentVersion.Contains(Convert.ToString((Convert.ToDecimal(LatestAppAgentVersion) * 10 - 1) / 10)) && c.IsDisabled == false);
                        int LatestMachineAgentCount = apmNodesThisAppList.Count(c => c.MachineAgentVersion.Contains(LatestMachineAgentVersion) && c.IsDisabled == false);
                        int AcceptableMachineAgentCount = apmNodesThisAppList.Count(c => c.MachineAgentVersion.Contains(Convert.ToString((Convert.ToDecimal(LatestMachineAgentVersion) * 10 - 1) / 10)) && c.IsDisabled == false);

                        if (apmNodesThisAppList.Count() > 0)
                        {
                            if ((LatestAppAgentCount * 100 / apmNodesThisAppList.Count()) >= 80)
                                healthCheck.AppAgentVersion = "PASS";
                            else if (((LatestAppAgentCount + AcceptableAppAgentCount) * 100 / apmNodesThisAppList.Count()) >= 80)
                                healthCheck.AppAgentVersion = "WARN";
                            else healthCheck.AppAgentVersion = "FAIL";

                            if (apmNodesThisAppList != null && (LatestMachineAgentCount * 100 / apmNodesThisAppList.Count()) >= 80)
                                healthCheck.MachineAgentVersion = "PASS";
                            else if (apmNodesThisAppList != null && ((LatestMachineAgentCount + AcceptableMachineAgentCount) * 100 / apmNodesThisAppList.Count()) >= 80)
                                healthCheck.MachineAgentVersion = "WARN";
                            else healthCheck.MachineAgentVersion = "FAIL";
                        }
                        else
                        {
                            healthCheck.AppAgentVersion = "FAIL";
                            healthCheck.MachineAgentVersion = "FAIL";
                        }

                        //Add BTErrorRateHigh & BTOverflow to Health Check

                        List<APMBusinessTransaction> apmEntitiesThisAppList = null;
                        if (apmEntitiesList != null) apmEntitiesThisAppList = apmEntitiesList.Where(t => t.Controller.StartsWith(apmAppConfig.Controller) == true && t.ApplicationName == apmAppConfig.ApplicationName).ToList<APMBusinessTransaction>();
                        if (apmEntitiesThisAppList != null)
                        {
                            //Add BTErrorRateHigh to Health Check
                            //If ErrorPercentage < 60%: Pass, Else if > 80%: Fail, Else: Warning

                            foreach (APMBusinessTransaction BTEntity in apmEntitiesThisAppList)
                            {
                                if (BTEntity.ErrorsPercentage > BTErrorRateUpper)
                                {
                                    healthCheck.BTErrorRateHigh = "FAIL";
                                    break;
                                }
                                else if (BTEntity.ErrorsPercentage > BTErrorRateLower)
                                {
                                    healthCheck.BTErrorRateHigh = "WARN";
                                    break;
                                }
                                healthCheck.BTErrorRateHigh = "PASS";

                            }

                            //Add BTOverflow to Health Check
                            //If Overflow BT Type has activity & BT Lockdown is disabled: Fail
                            foreach (APMBusinessTransaction BTEntity in apmEntitiesThisAppList)
                            {
                                if (BTEntity.BTType == "OVERFLOW" && BTEntity.HasActivity == true && healthCheck.BTLockdownEnabled == false)
                                {
                                    healthCheck.BTOverflow = "FAIL";
                                    break;
                                }
                                healthCheck.BTOverflow = "PASS";
                            }
                        }

                        //Add HRViolationHigh to Health Check
                        int HRViolationCount = 0;
                        ApplicationEventSummary appEventThisApp = null;

                        if (appEventsList != null) appEventThisApp = appEventsList.SingleOrDefault(a => a.Controller.StartsWith(apmAppConfig.Controller) == true && a.ApplicationName == apmAppConfig.ApplicationName);

                        if (appEventThisApp != null) HRViolationCount = appEventThisApp.NumHRViolations;
                        healthCheck.HRViolationsHigh = GetHealthCheckScore(HRViolationCount < HRViolationLower, HRViolationCount > HRViolationUpper);

                        //Console.WriteLine("{0} - HRViolations: {1}", apmAppConfig.ApplicationName, HRViolationCount);

                        //Add properties to HealthCheckList
                        healthChecksList.Add(healthCheck);
                    }
                }
                #endregion

                #region Write HealthChecks to CSV

                if (healthChecksList.Count != 0)
                {
                    FileIOHelper.WriteListToCSVFile(healthChecksList, new ApplicationHealthCheckReportMap(), FilePathMap.ApplicationHealthCheckCSVFilePath());
                }

                loggerConsole.Info("Finalize Application Health Check Summary File");

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
            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            logger.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            loggerConsole.Trace("Input.Metrics={0}", jobConfiguration.Input.Metrics);
            logger.Trace("Input.Events={0}", jobConfiguration.Input.Events);
            loggerConsole.Trace("Input.Events={0}", jobConfiguration.Input.Events);

            logger.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            loggerConsole.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);

            if (jobConfiguration.Input.Configuration == false || jobConfiguration.Input.DetectedEntities == false || jobConfiguration.Input.Metrics == false || jobConfiguration.Input.Events == false || jobConfiguration.Output.HealthCheck == false)
            {
                loggerConsole.Trace("Skipping building Health Check Summary File");
            }
            return (jobConfiguration.Input.Configuration == true && jobConfiguration.Input.DetectedEntities == true && jobConfiguration.Input.Metrics == true && jobConfiguration.Input.Events == true && jobConfiguration.Output.HealthCheck == true);
        }

        internal string GetHealthCheckScore(bool PassCondition, bool FailCondition)
        {
            try
            {
            if (PassCondition)
                return "PASS";
            else if (FailCondition)
                return "FAIL";
            else return "WARN";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return null;
            }
        }
    }
}