using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexAPMHealthCheck : JobStepIndexBase
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

                bool reportFolderCleaned = false;

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

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Target step variables

                        List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

                        #endregion

                        #region Preload all the reports that will be filtered by the subsequent entities

                        loggerConsole.Info("Entity Details Data Preloading");

                        // This file will always be there?
                        List<HealthCheckSettingMapping> healthCheckSettingsList = FileIOHelper.ReadListFromCSVFile<HealthCheckSettingMapping>(FilePathMap.HealthCheckSettingMappingFilePath(), new HealthCheckSettingMappingMap());
                        if (healthCheckSettingsList == null || healthCheckSettingsList.Count == 0)
                        {
                            loggerConsole.Warn("Health check settings file did not load. Exiting the health checks");

                            return false;
                        }
                        Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary = healthCheckSettingsList.ToDictionary(h => h.Name, h => h);

                        List<ControllerSummary> controllerSummariesList = FileIOHelper.ReadListFromCSVFile<ControllerSummary>(FilePathMap.ControllerSummaryIndexFilePath(jobTarget), new ControllerSummaryReportMap());
                        List<APMApplication> applicationsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.APMApplicationsIndexFilePath(jobTarget), new APMApplicationReportMap());
                        List<APMApplication> applicationsMetricsList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMApplication.ENTITY_FOLDER), new ApplicationMetricReportMap());

                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.APMTiersIndexFilePath(jobTarget), new APMTierReportMap());
                        List<APMTier> tiersMetricsList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMTier.ENTITY_FOLDER), new TierMetricReportMap());

                        List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.APMNodesIndexFilePath(jobTarget), new APMNodeReportMap());
                        List<APMNode> nodesMetricsList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMNode.ENTITY_FOLDER), new NodeMetricReportMap());

                        List<APMBusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget), new APMBusinessTransactionReportMap());
                        List<APMBusinessTransaction> businessTransactionsMetricsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMBusinessTransaction.ENTITY_FOLDER), new BusinessTransactionMetricReportMap());

                        List<APMBackend> backendsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.APMBackendsIndexFilePath(jobTarget), new APMBackendReportMap());
                        List<APMBackend> backendsMetricsList = FileIOHelper.ReadListFromCSVFile<APMBackend>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMBackend.ENTITY_FOLDER), new BackendMetricReportMap());

                        List<APMServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget), new APMServiceEndpointReportMap());
                        List<APMServiceEndpoint> serviceEndpointsMetricsList = FileIOHelper.ReadListFromCSVFile<APMServiceEndpoint>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMServiceEndpoint.ENTITY_FOLDER), new ServiceEndpointMetricReportMap());

                        List<APMError> errorsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.APMErrorsIndexFilePath(jobTarget), new APMErrorReportMap());
                        List<APMError> errorsMetricsList = FileIOHelper.ReadListFromCSVFile<APMError>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMError.ENTITY_FOLDER), new ErrorMetricReportMap());

                        List<APMInformationPoint> informationPointsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.APMInformationPointsIndexFilePath(jobTarget), new APMInformationPointReportMap());
                        List<APMInformationPoint> informationPointsMetricsList = FileIOHelper.ReadListFromCSVFile<APMInformationPoint>(FilePathMap.EntitiesFullIndexFilePath(jobTarget, APMInformationPoint.ENTITY_FOLDER), new InformationPointMetricReportMap());

                        List<APMResolvedBackend> resolvedBackendsList = FileIOHelper.ReadListFromCSVFile<APMResolvedBackend>(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget), new APMResolvedBackendReportMap());

                        //List<Event> eventsAllList = FileIOHelper.ReadListFromCSVFile<Event>(FilePathMap.ApplicationEventsIndexFilePath(jobTarget), new EventReportMap());
                        //List<HealthRuleViolationEvent> healthRuleViolationEventsAllList = FileIOHelper.ReadListFromCSVFile<HealthRuleViolationEvent>(FilePathMap.ApplicationHealthRuleViolationsIndexFilePath(jobTarget), new HealthRuleViolationEventReportMap());
                        //List<Snapshot> snapshotsAllList = FileIOHelper.ReadListFromCSVFile<Snapshot>(FilePathMap.SnapshotsIndexFilePath(jobTarget), new SnapshotReportMap());
                        //List<Segment> segmentsAllList = FileIOHelper.ReadListFromCSVFile<Segment>(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget), new SegmentReportMap());
                        //List<ExitCall> exitCallsAllList = FileIOHelper.ReadListFromCSVFile<ExitCall>(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget), new ExitCallReportMap());
                        //List<ServiceEndpointCall> serviceEndpointCallsAllList = FileIOHelper.ReadListFromCSVFile<ServiceEndpointCall>(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget), new ServiceEndpointCallReportMap());
                        //List<DetectedError> detectedErrorsAllList = FileIOHelper.ReadListFromCSVFile<DetectedError>(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget), new DetectedErrorReportMap());
                        //List<BusinessData> businessDataAllList = FileIOHelper.ReadListFromCSVFile<BusinessData>(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget), new BusinessDataReportMap());

                        List<AgentConfigurationProperty> agentPropertiesList = FileIOHelper.ReadListFromCSVFile<AgentConfigurationProperty>(FilePathMap.APMAgentConfigurationPropertiesIndexFilePath(jobTarget), new AgentConfigurationPropertyReportMap());

                        #endregion

                        #region Controller

                        healthCheckRuleResults.Add(
                            evaluate_Controller_Version(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                controllerSummariesList));

                        healthCheckRuleResults.Add(
                            evaluate_Controller_SaaS_OnPrem(
                                jobTarget,
                                healthCheckSettingsDictionary));

                        #endregion

                        #region Application Naming

                        healthCheckRuleResults.Add(
                            evaluate_APMApplication_Name_Length(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration));

                        healthCheckRuleResults.Add(
                            evaluate_APMApplication_Name_Environment(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration));

                        #endregion

                        #region Application Agent Properties

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Properties_BuiltIn_Modified(
                                jobTarget, 
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList));

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Properties_New_Added(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList));

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Properties_WellKnown_analytics_dynamic_service_enabled(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList));

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Properties_WellKnown_find_entry_points(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList));

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Properties_WellKnown_use_old_servlet_split_for_get_parm_value_rule(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList));

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Properties_WellKnown_use_max_business_transactions(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList));

                        #endregion

                        #region Tier Agent Property Overrides

                        healthCheckRuleResults.AddRange(
                            evaluate_APMApplication_Tier_Properties_Overriden(
                                jobTarget,
                                healthCheckSettingsDictionary,
                                jobConfiguration,
                                agentPropertiesList,
                                tiersList));
                        
                        //#region Tier Property Overrides on

                        //healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMTier", "Tier1", -123, "Agent Property Configuration", "APM-005-TIER-PROPERTIES-OVERRIDE", "Tier Agent Properties Override");
                        //healthCheckRuleResult.Grade = 3;
                        //healthCheckRuleResult.Description = "Tiers {0} in Application {1} overridden App-level Agent Properties";
                        //healthCheckRuleResult.Code = "APM-0005";
                        //healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
                        //healthCheckRuleResults.Add(healthCheckRuleResult);

                        //#endregion

                        //#region Built-in properties modified from their default values

                        //healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMTier", "Tier1", -123, "Agent Property Configuration", "APM-006-TIER-PROPERTIES-MODIFIED", "Tier Agent Properties Modified");
                        //healthCheckRuleResult.Grade = 3;
                        //healthCheckRuleResult.Description = "Built-in Agent Properties {0} modified from their default values in Application {1} Tier {2}";
                        //healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
                        //healthCheckRuleResults.Add(healthCheckRuleResult);

                        //#endregion

                        //#region New properties added

                        //healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMTier", "Tier1", -123, "Agent Property Configuration", "APM-007-TIER-PROPERTIES-ADDED", "Tier Agent Properties Added");
                        //healthCheckRuleResult.Grade = 3;
                        //healthCheckRuleResult.Description = "Non-default Agent Properties {0} added to Application {1} Tier {2}";
                        //healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
                        //healthCheckRuleResults.Add(healthCheckRuleResult);

                        //#endregion

                        #endregion

                        // Sort them
                        healthCheckRuleResults = healthCheckRuleResults.OrderBy(h => h.EntityType).ThenBy(h => h.EntityName).ThenBy(h => h.Category).ThenBy(h => h.Name).ToList();

                        // Set version
                        string versionOfDEXTER = Assembly.GetEntryAssembly().GetName().Version.ToString();
                        foreach (HealthCheckRuleResult healthCheckRuleResult in healthCheckRuleResults)
                        {
                            healthCheckRuleResult.Version = versionOfDEXTER;
                        }

                        FileIOHelper.WriteListToCSVFile(healthCheckRuleResults, new HealthCheckRuleResultReportMap(), FilePathMap.APMHealthCheckRuleResultsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = healthCheckRuleResults.Count;

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.APMHealthCheckReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.APMHealthCheckReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.APMHealthCheckRuleResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMHealthCheckRuleResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMHealthCheckRuleResultsReportFilePath(), FilePathMap.APMHealthCheckRuleResultsIndexFilePath(jobTarget));
                        }
                        //if (File.Exists(FilePathMap.APMTiersIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMTiersIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMTiersReportFilePath(), FilePathMap.APMTiersIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMNodesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodesIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodesReportFilePath(), FilePathMap.APMNodesIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodeStartupOptionsReportFilePath(), FilePathMap.APMNodeStartupOptionsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMNodePropertiesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodePropertiesIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodePropertiesReportFilePath(), FilePathMap.APMNodePropertiesIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMNodeEnvironmentVariablesReportFilePath(), FilePathMap.APMNodeEnvironmentVariablesIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMBackendsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBackendsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBackendsReportFilePath(), FilePathMap.APMBackendsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMMappedBackendsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMMappedBackendsReportFilePath(), FilePathMap.APMMappedBackendsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMBusinessTransactionsReportFilePath(), FilePathMap.APMBusinessTransactionsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMOverflowBusinessTransactionsReportFilePath(), FilePathMap.APMOverflowBusinessTransactionsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMServiceEndpointsReportFilePath(), FilePathMap.APMServiceEndpointsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMErrorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMErrorsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMErrorsReportFilePath(), FilePathMap.APMErrorsIndexFilePath(jobTarget));
                        //}
                        //if (File.Exists(FilePathMap.APMInformationPointsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.APMInformationPointsIndexFilePath(jobTarget)).Length > 0)
                        //{
                        //    FileIOHelper.AppendTwoCSVFiles(FilePathMap.APMInformationPointsReportFilePath(), FilePathMap.APMInformationPointsIndexFilePath(jobTarget));
                        //}

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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            loggerConsole.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            if (jobConfiguration.Output.HealthCheck == false)
            {
                loggerConsole.Trace("Skipping index of health check");
            }
            return (jobConfiguration.Output.HealthCheck == true);
        }

        #region Settings helper methods

        private Version getVersionSetting(Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary, string settingName, string valueIfNotThere)
        {
            if (healthCheckSettingsDictionary.ContainsKey(settingName) == true)
            {
                string versionString = healthCheckSettingsDictionary[settingName].Value;
                Version version = new Version();
                if (Version.TryParse(versionString, out version) == true)
                {
                    return version;
                }
                else
                {
                    return new Version(valueIfNotThere);
                }
            }
            else
            {
                return new Version(valueIfNotThere);
            }
        }

        private int getIntegerSetting(Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary, string settingName, int valueIfNotThere)
        {
            if (healthCheckSettingsDictionary.ContainsKey(settingName) == true)
            {
                string numberString = healthCheckSettingsDictionary[settingName].Value;
                int number = 0;
                if (Int32.TryParse(numberString, out number) == true)
                {
                    return number;
                }
                else
                {
                    return valueIfNotThere;
                }
            }
            else
            {
                return valueIfNotThere;
            }
        }

        private string getStringSetting(Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary, string settingName, string valueIfNotThere)
        {
            if (healthCheckSettingsDictionary.ContainsKey(settingName) == true)
            {
                return healthCheckSettingsDictionary[settingName].Value;
            }
            else
            {
                return valueIfNotThere;
            }
        }

        #endregion

        private HealthCheckRuleResult createHealthCheckRuleResult(
            JobTarget jobTarget, 
            string entityType, 
            string entityName, 
            long entityID,
            string ruleCategory,
            string ruleCode,
            string ruleName)
        {
            HealthCheckRuleResult healthCheckRuleResult = new HealthCheckRuleResult();
            healthCheckRuleResult.Controller = jobTarget.Controller;
            healthCheckRuleResult.Application = jobTarget.Application;
            healthCheckRuleResult.ApplicationID = jobTarget.ApplicationID;

            healthCheckRuleResult.EntityType = entityType;
            healthCheckRuleResult.EntityName = entityName;
            healthCheckRuleResult.EntityID = entityID;

            healthCheckRuleResult.Category = ruleCategory;
            healthCheckRuleResult.Code = ruleCode;
            healthCheckRuleResult.Name = ruleName;
            healthCheckRuleResult.Grade = 0;

            healthCheckRuleResult.EvaluationTime = DateTime.Now;

            return healthCheckRuleResult;
        }

        #region Controller

        /// <summary>
        /// Version of Controller should be reasonably latest
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="controllerSummariesList"></param>
        /// <returns></returns>
        private HealthCheckRuleResult evaluate_Controller_Version(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            List<ControllerSummary> controllerSummariesList)
        {
            string thisHealthRuleCode = "PLAT-001-PLATFORM-VERSION";
            loggerConsole.Info("Evaluating Controller Version ({0})", thisHealthRuleCode);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, "Platform", thisHealthRuleCode, "Controller Version");
            if (controllerSummariesList != null && controllerSummariesList.Count > 0)
            {
                ControllerSummary controllerSummary = controllerSummariesList[0];
                Version versionThisController = new Version(jobTarget.ControllerVersion);

                Version versionLatest = getVersionSetting(healthCheckSettingsDictionary, "LatestControllerVersion", "4.5.13");
                Version versionLatestFirstRelease = new Version(versionLatest.Major, versionLatest.Minor);
                Version versionLatestMinus1 = new Version(versionLatest.Major, versionLatest.Minor - 1);
                Version versionLatestMinus2 = new Version(versionLatest.Major, versionLatest.Minor - 2);
                Version versionLatestMinus3 = new Version(versionLatest.Major, versionLatest.Minor - 3);
                Version versionLatestMinus4 = new Version(versionLatest.Major, versionLatest.Minor - 4);
                Version versionLatestMinus5 = new Version(versionLatest.Major, versionLatest.Minor - 5);
                if (versionThisController >= versionLatest)
                {
                    healthCheckRuleResult.Grade = 5;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is greater or equal to latest '{1}'", jobTarget.ControllerVersion, versionLatest);
                }
                else if (versionThisController >= versionLatestFirstRelease)
                {
                    healthCheckRuleResult.Grade = 4;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is in '{1}' branch, old", jobTarget.ControllerVersion, versionLatestFirstRelease);
                }
                else if (versionThisController >= versionLatestMinus1)
                {
                    healthCheckRuleResult.Grade = 4;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is in '{1}' branch, really old", jobTarget.ControllerVersion, versionLatestMinus1);
                }
                else if (versionThisController >= versionLatestMinus2)
                {
                    healthCheckRuleResult.Grade = 3;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is in '{1}' branch, really, really old", jobTarget.ControllerVersion, versionLatestMinus2);
                }
                else if (versionThisController >= versionLatestMinus3)
                {
                    healthCheckRuleResult.Grade = 3;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is in '{1}' branch, really, really, really old", jobTarget.ControllerVersion, versionLatestMinus3);
                }
                else if (versionThisController >= versionLatestMinus4)
                {
                    healthCheckRuleResult.Grade = 2;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is in '{1}' branch, really, really, really, really old", jobTarget.ControllerVersion, versionLatestMinus4);
                }
                else if (versionThisController >= versionLatestMinus5)
                {
                    healthCheckRuleResult.Grade = 2;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is in '{1}' branch, really, really, really, really, really old", jobTarget.ControllerVersion, versionLatestMinus5);
                }
                else
                {
                    healthCheckRuleResult.Grade = 1;
                    healthCheckRuleResult.Description = String.Format("The Controller version '{0}' is < '{1}' branch. Ancient", jobTarget.ControllerVersion, versionLatestMinus5);
                }
            }
            else
            {
                healthCheckRuleResult.Grade = 1;
                healthCheckRuleResult.Description = "No information about Controller version available";
            }

            return healthCheckRuleResult;
        }

        /// <summary>
        /// We like SaaS controllers more than on premises
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <returns></returns>
        private HealthCheckRuleResult evaluate_Controller_SaaS_OnPrem(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary)
        {
            string thisHealthRuleCode = "PLAT-002-PLATFORM-SAAS";
            loggerConsole.Info("Evaluating Controller SaaS or OnPremises ({0})", thisHealthRuleCode);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, "Platform", thisHealthRuleCode, "Controller SaaS or OnPrem");
            if (jobTarget.Controller.ToLower().Contains("saas.appdynamics") == true)
            {
                healthCheckRuleResult.Grade = 5;
                healthCheckRuleResult.Description = "Controller is running in AppDynamics SaaS cloud";
            }
            else
            {
                healthCheckRuleResult.Grade = 3;
                healthCheckRuleResult.Description = "Controller is running in OnPremises configuration";
            }
            return healthCheckRuleResult;
        }

        #endregion

        #region APM Application Naming

        /// <summary>
        /// Length of the APM Application Name
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <returns></returns>
        private HealthCheckRuleResult evaluate_APMApplication_Name_Length(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration)
        {
            string thisHealthRuleCode = "APM-001-APP-NAME-LENGTH";
            loggerConsole.Info("Evaluating Application name length ({0})", thisHealthRuleCode);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Logical Model Naming", thisHealthRuleCode, "App Name Length");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application"));

            if (jobTarget.Application.Length > getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade2", 60))
            {
                healthCheckRuleResult.Grade = 1;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' is very, very, very long (>{1} characters)", jobTarget.Application, getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade2", 60));
            }
            else if (jobTarget.Application.Length > getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade3", 50))
            {
                healthCheckRuleResult.Grade = 2;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' is very, very long (>{1} characters)", jobTarget.Application, getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade3", 50));
            }
            else if (jobTarget.Application.Length > getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade4", 40))
            {
                healthCheckRuleResult.Grade = 3;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' is very long (>{1} characters)", jobTarget.Application, getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade2", 40));
            }
            else if (jobTarget.Application.Length > getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade5", 30))
            {
                healthCheckRuleResult.Grade = 4;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' is long (>{1} characters)", jobTarget.Application, getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade5", 30));
            }
            else
            {
                healthCheckRuleResult.Grade = 5;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' is within recommended length (<={1} characters)", jobTarget.Application, getIntegerSetting(healthCheckSettingsDictionary, "APMApplicationNameLengthGrade5", 30));
            }
            
            return healthCheckRuleResult;
        }

        /// <summary>
        /// APM Application name should contain environment designation
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <returns></returns>
        private HealthCheckRuleResult evaluate_APMApplication_Name_Environment(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration)
        {
            string thisHealthRuleCode = "APM-002-APP-NAME-INC-ENV-DESIGNATION";
            loggerConsole.Info(thisHealthRuleCode);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Logical Model Naming", thisHealthRuleCode, "App Name Includes Environment Designation");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application"));

            Regex regexQuery = new Regex(getStringSetting(healthCheckSettingsDictionary, "APMApplicationNameEnvironmentRegex", "(production|prod|qa|test|tst|nonprod|perf|performance|sit|clt|dev|uat|poc|pov|demo|stage|stg)"), RegexOptions.IgnoreCase);
            Match regexMatch = regexQuery.Match(jobTarget.Application);
            if (regexMatch.Success == true && regexMatch.Groups.Count == 2)
            {
                healthCheckRuleResult.Grade = 5;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' contains environment designation '{1}'", jobTarget.Application, regexMatch.Groups[1].Value);
            }
            else
            {
                healthCheckRuleResult.Grade = 3;
                healthCheckRuleResult.Description = String.Format("The Application Name '{0}' does not contains environment designation", jobTarget.Application);
            }

            return healthCheckRuleResult;
        }

        #endregion

        #region Application Agent Properties

        /// <summary>
        /// Built-in properties modified from their default values
        /// Any change is graded 3
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Properties_BuiltIn_Modified(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList)
        {
            string thisHealthRuleCode = "APM-003-APP-DEFAULT-PROPERTY-MODIFIED";
            loggerConsole.Info("Evaluating default Agent Properties modified ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Agent Property Modified");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "None of the built-in Agent Properties are modified from their default values";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesBuiltInList = agentPropertiesList.Where(p => AGENT_PROPERTIES_BUILTIN.Contains(p.PropertyName) == true && p.IsDefault == false && p.TierName.Length == 0).ToList();
                if (agentPropertiesBuiltInList != null && agentPropertiesBuiltInList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesBuiltInList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 3;

                        switch (agentProperty.PropertyType)
                        {
                            case "BOOLEAN":
                                healthCheckRuleResult1.Description = String.Format("Built-in Agent Property '{0}/{1} [{2}]' value is '{3}' and modified from its default value '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.BooleanDefaultValue);
                                break;

                            case "INTEGER":
                                healthCheckRuleResult1.Description = String.Format("Built-in Agent Property '{0}/{1} [{2}]' value is '{3}' and modified from its default value '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.IntegerValue, agentProperty.IntegerDefaultValue);
                                break;

                            case "STRING":
                                healthCheckRuleResult1.Description = String.Format("Built-in Agent Property '{0}/{1} [{2}]' value is '{3}' and modified from its default value '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.StringValue, agentProperty.StringDefaultValue);
                                break;

                            default:
                                break;
                        }

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        /// <summary>
        /// Non-built in properties added
        /// Any change is graded 3
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="jobConfiguration"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Properties_New_Added(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList)
        {
            string thisHealthRuleCode = "APM-004-APP-PROPERTY-ADDED";
            loggerConsole.Info("Evaluating non-default Agent Properties added ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Agent Property Added");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "No additional non-default Agent Properties are added to application configuration";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesNonBuiltIntList = agentPropertiesList.Where(p => AGENT_PROPERTIES_BUILTIN.Contains(p.PropertyName) == false && p.TierName.Length == 0).ToList();
                if (agentPropertiesNonBuiltIntList != null && agentPropertiesNonBuiltIntList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesNonBuiltIntList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 3;

                        switch (agentProperty.PropertyType)
                        {
                            case "BOOLEAN":
                                healthCheckRuleResult1.Description = String.Format("Non-default Agent Property '{0}/{1} [{2}]' value is '{3}' and its default value is '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.BooleanDefaultValue);
                                break;

                            case "INTEGER":
                                healthCheckRuleResult1.Description = String.Format("Non-default Agent Property '{0}/{1} [{2}]' value is '{3}' and its default value is '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.IntegerValue, agentProperty.IntegerDefaultValue);
                                break;

                            case "STRING":
                                healthCheckRuleResult1.Description = String.Format("Non-default Agent Property '{0}/{1} [{2}]' value is '{3}' and its default value is '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.StringValue, agentProperty.StringDefaultValue);
                                break;

                            default:
                                break;
                        }

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {            
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        /// <summary>
        /// Well-known properties - analytics-dynamic-service-enabled
        /// if it is not set at all , grade 5
        /// if it is set, grade 5
        /// if it is set for some but not others, grade 1
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="jobConfiguration"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Properties_WellKnown_analytics_dynamic_service_enabled(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList)
        {
            string thisHealthRuleCode = "APM-005-APP-SIGNIFICANT-PROPERTY-SET";
            loggerConsole.Info("Evaluating significat Agent Property analytics-dynamic-service-enabled ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Agent Property Set (analytics-dynamic-service-enabled)");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "BIQ/Analytics is not enabled via 'analytics-dynamic-service-enabled' Agent Property for any agent types";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesAnalyticsEnabledList = agentPropertiesList.Where(p => p.PropertyName == "analytics-dynamic-service-enabled" && p.BooleanValue == true && p.TierName.Length == 0).ToList();
                if (agentPropertiesAnalyticsEnabledList != null && agentPropertiesAnalyticsEnabledList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesAnalyticsEnabledList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 5;
                        healthCheckRuleResult1.Description = String.Format("BIQ/Analytics is enabled via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.AgentType);
                 
                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }

                List<AgentConfigurationProperty> agentPropertiesAnalyticsDisabledList = agentPropertiesList.Where(p => p.PropertyName == "analytics-dynamic-service-enabled" && p.BooleanValue == false && p.TierName.Length == 0).ToList();
                if (agentPropertiesAnalyticsDisabledList != null && agentPropertiesAnalyticsDisabledList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesAnalyticsDisabledList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 1;
                        healthCheckRuleResult1.Description = String.Format("BIQ/Analytics is disabled via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.AgentType);
                    
                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }
            
            if (healthCheckRuleResults.Count == 0)
            {
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        /// <summary>
        /// Well-known properties - find-entry-points
        /// If it is set at all, grade 2
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="jobConfiguration"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Properties_WellKnown_find_entry_points(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList)
        {
            string thisHealthRuleCode = "APM-005-APP-SIGNIFICANT-PROPERTY-SET";
            loggerConsole.Info("Evaluating significat Agent Property find-entry-points ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Agent Property Set (find-entry-points)");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "Business Transaction discovery stack logging is disabled via 'find-entry-points' Agent Property for all agent types";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesFindEntryPointsDisabledList = agentPropertiesList.Where(p => p.PropertyName == "find-entry-points" && p.BooleanValue == false && p.TierName.Length == 0).ToList();
                if (agentPropertiesFindEntryPointsDisabledList != null && agentPropertiesFindEntryPointsDisabledList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesFindEntryPointsDisabledList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 5;
                        healthCheckRuleResult1.Description = String.Format("Business Transaction discovery stack logging is disabled via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.AgentType);
                 
                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }

                List<AgentConfigurationProperty> agentPropertiesFindEntryPointsEnabledList = agentPropertiesList.Where(p => p.PropertyName == "find-entry-points" && p.BooleanValue == true && p.TierName.Length == 0).ToList();
                if (agentPropertiesFindEntryPointsEnabledList != null && agentPropertiesFindEntryPointsEnabledList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesFindEntryPointsEnabledList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 2;
                        healthCheckRuleResult1.Description = String.Format("Business Transaction discovery stack logging is enabled via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.AgentType);
                    
                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {                
                healthCheckRuleResults.Add(healthCheckRuleResult);

            }
            return healthCheckRuleResults;
        }

        /// <summary>
        /// Well-known properties - use-old-servlet-split-for-get-parm-value-rule
        /// You set it, you get graded lowest
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="jobConfiguration"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Properties_WellKnown_use_old_servlet_split_for_get_parm_value_rule(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList)
        {
            string thisHealthRuleCode = "APM-005-APP-SIGNIFICANT-PROPERTY-SET";
            loggerConsole.Info("Evaluating significat Agent Property use-old-servlet-split-for-get-parm-value-rule ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Agent Property Set (use-old-servlet-split-for-get-parm-value-rule)");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "Dangerous Servlet parameter parsing option is disabled via 'use-old-servlet-split-for-get-parm-value-rule' Agent Property for all agent types";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesEvilOldServletSplitEnabledList = agentPropertiesList.Where(p => p.PropertyName == "use-old-servlet-split-for-get-parm-value-rule" && p.BooleanValue == true && p.TierName.Length == 0).ToList();
                if (agentPropertiesEvilOldServletSplitEnabledList != null && agentPropertiesEvilOldServletSplitEnabledList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesEvilOldServletSplitEnabledList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 1;
                        healthCheckRuleResult1.Description = String.Format("Dangerous Servlet parameter parsing option is enabled via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.AgentType);

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        /// <summary>
        /// Well-known properties - max-business-transactions
        /// Set higher than standard - grade 2
        /// Set lower than standard, grade 3
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="jobConfiguration"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Properties_WellKnown_use_max_business_transactions(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList)
        {
            string thisHealthRuleCode = "APM-005-APP-SIGNIFICANT-PROPERTY-SET";
            loggerConsole.Info("Evaluating significat Agent Property max-business-transactions ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Agent Property Set (max-business-transactions)");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "Application_Properties"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "Maximum Business Transaction slot registration are at default value via 'max-business-transactions' Agent Property for all agent types";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesMaxBTGreaterThanDefault = agentPropertiesList.Where(p => p.PropertyName == "max-business-transactions" && p.IntegerValue > 50 && p.TierName.Length == 0).ToList();
                if (agentPropertiesMaxBTGreaterThanDefault != null && agentPropertiesMaxBTGreaterThanDefault.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesMaxBTGreaterThanDefault)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 2;
                        healthCheckRuleResult1.Description = String.Format("Higher than standard Business Transaction slot registration value is set via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.IntegerValue, agentProperty.AgentType);

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }

                List<AgentConfigurationProperty> agentPropertiesMaxBTLessThanDefault = agentPropertiesList.Where(p => p.PropertyName == "max-business-transactions" && p.IntegerValue < 50 && p.TierName.Length == 0).ToList();
                if (agentPropertiesMaxBTLessThanDefault != null && agentPropertiesMaxBTLessThanDefault.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesMaxBTLessThanDefault)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 3;
                        healthCheckRuleResult1.Description = String.Format("Lower than standard Business Transaction slot registration value is set via '{0}/{1} [{2}]' Agent Property set to '{3}' for agent type '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.IntegerValue, agentProperty.AgentType);

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        #endregion

        #region Tier Agent Property evaluations

        /// <summary>
        /// Tier override has properties?
        /// Any change is graded 3
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Tier_Properties_Overriden(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList,
            List<APMTier> tiersList)
        {
            string thisHealthRuleCode = "APM-006-APP-TIER-PROPERTIES-OVERRIDEN";
            loggerConsole.Info("Evaluating Tier Agent Properties override ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Tier Agent Property Override");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "TODO"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "None of the Application Tiers have Agent Properties override";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesTierOverridePropertiesList = agentPropertiesList.Where(p => p.TierName.Length > 0).ToList();
                if (agentPropertiesTierOverridePropertiesList != null && agentPropertiesTierOverridePropertiesList.Count > 0)
                {
                    var agentPropertiesTierOverridePropertiesListUniqueTiers = agentPropertiesTierOverridePropertiesList.GroupBy(p => p.TierName);
                    foreach (var uniqueTier in agentPropertiesTierOverridePropertiesListUniqueTiers)
                    {
                        List<AgentConfigurationProperty> agentPropertiesInTier = uniqueTier.ToList();
                        AgentConfigurationProperty agentProperty = agentPropertiesInTier[0];

                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();

                        healthCheckRuleResult1.EntityType = "APMTier";
                        healthCheckRuleResult1.EntityName = agentProperty.TierName;
                        if (tiersList != null)
                        {
                            APMTier tier = tiersList.Where(t => t.TierName == agentProperty.TierName).FirstOrDefault();
                            if (tier != null)
                            {
                                healthCheckRuleResult1.EntityID = tier.TierID;
                            }
                            else
                            {
                                healthCheckRuleResult1.EntityID = 0;
                            }
                        }
                        
                        healthCheckRuleResult1.Grade = 3;
                        healthCheckRuleResult1.Description = String.Format("Tier '{0} [{1}]' has Agent Property override flag on, overriding '{2}' properties", agentProperty.TierName, agentProperty.AgentType, agentPropertiesInTier.Count);

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        /// <summary>
        /// Tier override has things changed from defaults
        /// Any change is graded 3
        /// </summary>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="agentPropertiesList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_APMApplication_Tier_Properties_Overriden_BuiltIn_Modified(
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            JobConfiguration jobConfiguration,
            List<AgentConfigurationProperty> agentPropertiesList,
            List<APMTier> tiersList)
        {
            string thisHealthRuleCode = "APM-006-APP-TIER-DEFAULT-PROPERTY-MODIFIED";
            loggerConsole.Info("Evaluating default properties in Tier Agent Properties override ({0})", thisHealthRuleCode);

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "APMApp", jobTarget.Application, jobTarget.ApplicationID, "Agent Property Configuration", thisHealthRuleCode, "App Tier Agent Property Modified");
            healthCheckRuleResult.RuleLink = String.Format(@"=HYPERLINK(""{0}"", ""<Detail>"")", String.Format("{0}#{1}", FilePathMap.ApplicationSummaryWordReportFilePath(jobTarget, jobConfiguration.Input.TimeRange, false), "TODO"));
            healthCheckRuleResult.Grade = 5;
            healthCheckRuleResult.Description = "None of the Application Tiers with Agent Properties override on have modified built-in properties";

            if (agentPropertiesList != null && agentPropertiesList.Count > 0)
            {
                List<AgentConfigurationProperty> agentPropertiesBuiltInList = agentPropertiesList.Where(p => AGENT_PROPERTIES_BUILTIN.Contains(p.PropertyName) == true && p.IsDefault == false && p.TierName.Length > 0).ToList();
                if (agentPropertiesBuiltInList != null && agentPropertiesBuiltInList.Count > 0)
                {
                    foreach (AgentConfigurationProperty agentProperty in agentPropertiesBuiltInList)
                    {
                        HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                        healthCheckRuleResult1.Grade = 3;

                        healthCheckRuleResult1.EntityType = "APMTier";
                        healthCheckRuleResult1.EntityName = agentProperty.TierName;
                        if (tiersList != null)
                        {
                            APMTier tier = tiersList.Where(t => t.TierName == agentProperty.TierName).FirstOrDefault();
                            if (tier != null)
                            {
                                healthCheckRuleResult1.EntityID = tier.TierID;
                            }
                            else
                            {
                                healthCheckRuleResult1.EntityID = 0;
                            }
                        }

                        switch (agentProperty.PropertyType)
                        {
                            case "BOOLEAN":
                                healthCheckRuleResult1.Description = String.Format("Built-in Agent Property '{0}/{1} [{2}]' value is '{3}' and modified from its default value '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.BooleanValue, agentProperty.BooleanDefaultValue);
                                break;

                            case "INTEGER":
                                healthCheckRuleResult1.Description = String.Format("Built-in Agent Property '{0}/{1} [{2}]' value is '{3}' and modified from its default value '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.IntegerValue, agentProperty.IntegerDefaultValue);
                                break;

                            case "STRING":
                                healthCheckRuleResult1.Description = String.Format("Built-in Agent Property '{0}/{1} [{2}]' value is '{3}' and modified from its default value '{4}'", agentProperty.AgentType, agentProperty.PropertyName, agentProperty.PropertyType, agentProperty.StringValue, agentProperty.StringDefaultValue);
                                break;

                            default:
                                break;
                        }

                        healthCheckRuleResults.Add(healthCheckRuleResult1);
                    }
                }
            }

            if (healthCheckRuleResults.Count == 0)
            {
                healthCheckRuleResults.Add(healthCheckRuleResult);
            }

            return healthCheckRuleResults;
        }

        #endregion

    }
}
