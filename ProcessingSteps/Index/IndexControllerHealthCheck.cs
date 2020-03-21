using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexControllerHealthCheck : JobStepIndexBase
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

                bool reportFolderCleaned = false;

                // Process each Controller once
                int i = 0;
                var controllers = jobConfiguration.Target.GroupBy(t => t.Controller);
                foreach (var controllerGroup in controllers)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = controllerGroup.ToList()[0];

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

                        // This file will always be there
                        List<HealthCheckSettingMapping> healthCheckSettingsList = FileIOHelper.ReadListFromCSVFile<HealthCheckSettingMapping>(FilePathMap.HealthCheckSettingMappingFilePath(), new HealthCheckSettingMappingMap());
                        if (healthCheckSettingsList == null || healthCheckSettingsList.Count == 0)
                        {
                            loggerConsole.Warn("Health check settings file did not load. Exiting the health checks");

                            return false;
                        }
                        Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary = healthCheckSettingsList.ToDictionary(h => h.Name, h => h);

                        List<ControllerSummary> controllerSummariesList = FileIOHelper.ReadListFromCSVFile<ControllerSummary>(FilePathMap.ControllerSummaryIndexFilePath(jobTarget), new ControllerSummaryReportMap());
                        List<ControllerSetting> controllerSettingsList = FileIOHelper.ReadListFromCSVFile<ControllerSetting>(FilePathMap.ControllerSettingsIndexFilePath(jobTarget), new ControllerSettingReportMap());

                        #endregion

                        #region Controller Version and Properties

                        healthCheckRuleResults.Add(
                            evaluate_Controller_Version(
                                new HealthCheckRuleDescription("Platform", "PLAT-001-PLATFORM-VERSION", "Controller Version"),
                                jobTarget,
                                healthCheckSettingsDictionary,
                                controllerSummariesList));

                        healthCheckRuleResults.Add(
                            evaluate_Controller_SaaS_OnPrem(
                                new HealthCheckRuleDescription("Platform", "PLAT-002-PLATFORM-SAAS", "SaaS or OnPrem"),
                                jobTarget,
                                healthCheckSettingsDictionary));

                        healthCheckRuleResults.Add(
                            evaluate_Controller_Setting_Performance_Profile(
                                new HealthCheckRuleDescription("Platform", "PLAT-003-PLATFORM-PERFORMANCE-PROFILE", "Performance Profile"),
                                jobTarget,
                                healthCheckSettingsDictionary,
                                controllerSettingsList));

                        healthCheckRuleResults.AddRange(
                            evaluate_Controller_Setting_Buffer_Sizes(
                                new HealthCheckRuleDescription("Platform", "PLAT-004-PLATFORM-BUFFER-SIZES", "Buffer Size"),
                                jobTarget,
                                healthCheckSettingsDictionary,
                                controllerSettingsList));

                        healthCheckRuleResults.AddRange(
                            evaluate_Controller_Setting_ADD_Limits(
                                new HealthCheckRuleDescription("Platform", "PLAT-005-PLATFORM-ADD-LIMITS", "ADD Limit"),
                                jobTarget,
                                healthCheckSettingsDictionary,
                                controllerSettingsList));


                        // TODO Add things like ADD limits, etc

                        #endregion

                        // Remove any health rule results that weren't very good
                        healthCheckRuleResults.RemoveAll(h => h == null);

                        // Sort them
                        healthCheckRuleResults = healthCheckRuleResults.OrderBy(h => h.EntityType).ThenBy(h => h.EntityName).ThenBy(h => h.Category).ThenBy(h => h.Name).ToList();

                        // Set version to each of the health check rule results
                        string versionOfDEXTER = Assembly.GetEntryAssembly().GetName().Version.ToString();
                        foreach (HealthCheckRuleResult healthCheckRuleResult in healthCheckRuleResults)
                        {
                            healthCheckRuleResult.Version = versionOfDEXTER;
                        }

                        FileIOHelper.WriteListToCSVFile(healthCheckRuleResults, new HealthCheckRuleResultReportMap(), FilePathMap.ControllerHealthCheckRuleResultsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = healthCheckRuleResults.Count;

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ControllerHealthCheckReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ControllerHealthCheckReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.ControllerHealthCheckRuleResultsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ControllerHealthCheckRuleResultsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ControllerHealthCheckRuleResultsReportFilePath(), FilePathMap.ControllerHealthCheckRuleResultsIndexFilePath(jobTarget));
                        }

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

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.HealthCheck={0}", programOptions.LicensedReports.HealthCheck);
            loggerConsole.Trace("LicensedReports.HealthCheck={0}", programOptions.LicensedReports.HealthCheck);
            if (programOptions.LicensedReports.HealthCheck == false)
            {
                loggerConsole.Warn("Not licensed for health check");
                return false;
            }

            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            logger.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            logger.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.Configuration={0}", jobConfiguration.Input.Configuration);
            loggerConsole.Trace("Output.HealthCheck={0}", jobConfiguration.Output.HealthCheck);
            if (jobConfiguration.Input.DetectedEntities == false ||
                jobConfiguration.Input.Configuration == false || 
                jobConfiguration.Output.HealthCheck == false)
            {
                loggerConsole.Trace("Skipping index of health check");
            }
            return (jobConfiguration.Input.DetectedEntities == true &&
                jobConfiguration.Input.Metrics == true &&
                jobConfiguration.Input.Configuration == true &&
                jobConfiguration.Output.HealthCheck == true);
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
            HealthCheckRuleDescription hcrd,
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            List<ControllerSummary> controllerSummariesList)
        {
            logger.Trace("Evaluating {0}", hcrd);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, hcrd);
            healthCheckRuleResult.Application = "[ALL APPS]";

            if (controllerSummariesList != null && controllerSummariesList.Count > 0)
            {
                ControllerSummary controllerSummary = controllerSummariesList[0];
                Version versionThisController = new Version(jobTarget.ControllerVersion);

                healthCheckRuleResult.Description = String.Format("The Controller version '{0}'", jobTarget.ControllerVersion, getVersionSetting(healthCheckSettingsDictionary, "ControllerVersionGrade5", "4.5"));

                if (versionThisController >= getVersionSetting(healthCheckSettingsDictionary, "ControllerVersionGrade5", "4.5"))
                {
                    healthCheckRuleResult.Grade = 5;
                }
                else if (versionThisController >= getVersionSetting(healthCheckSettingsDictionary, "ControllerVersionGrade4", "4.4"))
                {
                    healthCheckRuleResult.Grade = 4;
                }
                else if (versionThisController >= getVersionSetting(healthCheckSettingsDictionary, "ControllerVersionGrade3", "4.3"))
                {
                    healthCheckRuleResult.Grade = 3;
                }
                else if (versionThisController >= getVersionSetting(healthCheckSettingsDictionary, "ControllerVersionGrade2", "4.2"))
                {
                    healthCheckRuleResult.Grade = 2;
                }
                else
                {
                    healthCheckRuleResult.Grade = 1;
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
            HealthCheckRuleDescription hcrd,
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary)
        {
            logger.Trace("Evaluating {0}", hcrd);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, hcrd);
            healthCheckRuleResult.Application = "[ALL APPS]";

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

        private HealthCheckRuleResult evaluate_Controller_Setting_Performance_Profile(
            HealthCheckRuleDescription hcrd,
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            List<ControllerSetting> controllerSettingsList)
        {
            logger.Trace("Evaluating {0}", hcrd);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, hcrd);
            healthCheckRuleResult.Application = "[ALL APPS]";

            if (controllerSettingsList != null)
            {
                ControllerSetting controllerSettingProfile = controllerSettingsList.Where(s => s.Name == "performance.profile").FirstOrDefault();
                if (controllerSettingProfile == null)
                {
                    healthCheckRuleResult.Grade = 1;
                    healthCheckRuleResult.Description = "Controller performance profile is unknown";
                }
                else
                {
                    healthCheckRuleResult.Description = String.Format("Controller performance profile is '{0}'", controllerSettingProfile.Value);

                    // Defined in Enterprise Console via various
                    // C:\AppDynamics\Platform\platform-admin\archives\controller\4.5.4.15417\playbooks\controller-<size>.groovy
                    switch (controllerSettingProfile.Value.ToLower())
                    {
                        case "internal":
                        case "dev":
                        case "demo":
                            healthCheckRuleResult.Grade = 1;
                            break;

                        case "small":
                            healthCheckRuleResult.Grade = 2;
                            break;

                        case "medium":
                            healthCheckRuleResult.Grade = 3;
                            break;

                        case "large":
                            healthCheckRuleResult.Grade = 4;
                            break;

                        case "extra-large":
                            healthCheckRuleResult.Grade = 5;
                            break;

                        default:
                            healthCheckRuleResult.Grade = 1;
                            break;
                    }
                }
            }

            return healthCheckRuleResult;
        }

        /// <summary>
        /// https://community.appdynamics.com/t5/Knowledge-Base/Why-am-I-receiving-the-error-quot-Controller-Metric-Data-Buffer/ta-p/14653
        /// https://community.appdynamics.com/t5/Knowledge-Base/Why-are-snapshots-missing-in-the-Controller/ta-p/19047
        /// </summary>
        /// <param name="hcrd"></param>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="controllerSettingsList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_Controller_Setting_Buffer_Sizes(
            HealthCheckRuleDescription hcrd,
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            List<ControllerSetting> controllerSettingsList)
        {
            logger.Trace("Evaluating {0}", hcrd);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, hcrd);
            healthCheckRuleResult.Application = "[ALL APPS]";

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            if (controllerSettingsList != null)
            {
                // Defined in Enterprise Console via various
                // C:\AppDynamics\Platform\platform-admin\archives\controller\4.5.4.15417\playbooks\controller-<size>.groovy
                ControllerSetting controllerSettingProfile = controllerSettingsList.Where(s => s.Name == "performance.profile").FirstOrDefault();
                if (controllerSettingProfile != null)
                {
                    List<ControllerSetting> controllerSettingsBuffers = controllerSettingsList.Where(s => s.Name.Contains(".buffer.size")).ToList();
                    if (controllerSettingsBuffers != null)
                    {
                        foreach (ControllerSetting controllerSetting in controllerSettingsBuffers)
                        {
                            // Dealing with only these:
                            // events.buffer.size
                            // metrics.buffer.size
                            // process.snapshots.buffer.size
                            // snapshots.buffer.size

                            string lookupSettingName = String.Format("ControllerSetting.{0}.{1}", controllerSettingProfile.Value, controllerSetting.Name);

                            int settingCurrentValue = -1;
                            Int32.TryParse(controllerSetting.Value, out settingCurrentValue);
                            
                            int settingDefaultValue = getIntegerSetting(healthCheckSettingsDictionary, lookupSettingName, -1);
                            
                            if (settingDefaultValue != -1 && settingCurrentValue != -1)
                            {
                                HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                                healthCheckRuleResult1.Name = String.Format("{0} ({1})", healthCheckRuleResult.Name, controllerSetting.Name);

                                if (settingCurrentValue == settingDefaultValue)
                                {
                                    healthCheckRuleResult1.Grade = 5;
                                    healthCheckRuleResult1.Description = String.Format("Controller performance profile is '{0}', setting '{1}'='{2}', at default value", controllerSettingProfile.Value, controllerSetting.Name, settingCurrentValue);
                                }
                                else if (settingCurrentValue < settingDefaultValue)
                                {
                                    healthCheckRuleResult1.Grade = 3;
                                    healthCheckRuleResult1.Description = String.Format("Controller performance profile is '{0}', setting '{1}'='{2}', (<) less than default value '{3}'", controllerSettingProfile.Value, controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                }
                                else if (settingCurrentValue > settingDefaultValue)
                                {
                                    healthCheckRuleResult1.Grade = 4;
                                    healthCheckRuleResult1.Description = String.Format("Controller performance profile is '{0}', setting '{1}'='{2}', (>) greater than default value '{3}'", controllerSettingProfile.Value, controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                }

                                healthCheckRuleResults.Add(healthCheckRuleResult1);
                            }
                        }
                    }

                }
            }

            return healthCheckRuleResults;
        }

        /// <summary>
        /// https://community.appdynamics.com/t5/Knowledge-Base/Controller-and-Agent-ADD-Limit-Notifications-Explanations/ta-p/23273#CONTROLLER_ASYNC_ADD_REG_LIMIT_REACHED
        /// </summary>
        /// <param name="hcrd"></param>
        /// <param name="jobTarget"></param>
        /// <param name="healthCheckSettingsDictionary"></param>
        /// <param name="controllerSettingsList"></param>
        /// <returns></returns>
        private List<HealthCheckRuleResult> evaluate_Controller_Setting_ADD_Limits(
            HealthCheckRuleDescription hcrd,
            JobTarget jobTarget,
            Dictionary<string, HealthCheckSettingMapping> healthCheckSettingsDictionary,
            List<ControllerSetting> controllerSettingsList)
        {
            logger.Trace("Evaluating {0}", hcrd);

            HealthCheckRuleResult healthCheckRuleResult = createHealthCheckRuleResult(jobTarget, "Controller", jobTarget.Controller, 0, hcrd);
            healthCheckRuleResult.Application = "[ALL APPS]";

            List<HealthCheckRuleResult> healthCheckRuleResults = new List<HealthCheckRuleResult>();

            if (controllerSettingsList != null)
            {
                // Defined in Enterprise Console via various
                // C:\AppDynamics\Platform\platform-admin\archives\controller\4.5.4.15417\playbooks\controller-<size>.groovy
                ControllerSetting controllerSettingProfile = controllerSettingsList.Where(s => s.Name == "performance.profile").FirstOrDefault();
                if (controllerSettingProfile != null)
                {
                    List<ControllerSetting> controllerSettingsLimits = controllerSettingsList.Where(s => s.Name.Contains(".registration.limit")).ToList();
                    if (controllerSettingsLimits != null)
                    {
                        foreach (ControllerSetting controllerSetting in controllerSettingsLimits)
                        {
                            string lookupSettingName = String.Format("ControllerSetting.{0}.{1}", controllerSettingProfile.Value, controllerSetting.Name);

                            int settingCurrentValue = -1;
                            int settingDefaultValue = -1;
                            int settingRecommendedValue = -1;
                            if (Int32.TryParse(controllerSetting.Value, out settingCurrentValue) == true)
                            {
                                HealthCheckRuleResult healthCheckRuleResult1 = healthCheckRuleResult.Clone();
                                healthCheckRuleResult1.Name = String.Format("{0} ({1})", healthCheckRuleResult.Name, controllerSetting.Name);
                                healthCheckRuleResult1.Grade = -1;

                                // All settings are taken 
                                // from C:\AppDynamics\Platform\platform-admin\archives\controller\4.5.4.15417\playbooks\controller-<size>.groovy in EC
                                // And from careful review of significant number of controllers
                                switch (controllerSetting.Name)
                                {
                                    case "application.custom.metric.registration.limit":
                                    case "application.metric.registration.limit":
                                    case "async.thread.tracking.registration.limit":
                                    case "metric.registration.limit":
                                    case "sep.ADD.registration.limit":
                                    case "stacktrace.ADD.registration.limit":
                                    case "error.registration.limit":
                                        // maxStacktracePerAccountLimit = 4000
                                        // maxSepPerAccountLimit = 4000
                                        // maxErrorsPerAccountLimit = 4000
                                        settingRecommendedValue = getIntegerSetting(healthCheckSettingsDictionary, lookupSettingName, -1);
                                        if (settingRecommendedValue != -1)
                                        {
                                            if (settingCurrentValue == settingRecommendedValue)
                                            {
                                                healthCheckRuleResult1.Grade = 5;
                                                healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', at recommended value", controllerSetting.Name, settingCurrentValue);
                                            }
                                            else if (settingCurrentValue < settingRecommendedValue)
                                            {
                                                healthCheckRuleResult1.Grade = 3;
                                                healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (<) less than recommended value '{2}'", controllerSetting.Name, settingCurrentValue, settingRecommendedValue);
                                            }
                                            else if (settingCurrentValue > settingRecommendedValue)
                                            {
                                                healthCheckRuleResult1.Grade = 5;
                                                healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (>) greater than recommended value '{2}'", controllerSetting.Name, settingCurrentValue, settingRecommendedValue);
                                            }
                                        }
                                        break;

                                    case "collections.ADD.registration.limit":
                                        // maxCollectionsPerAccountLimit = 4000
                                        settingDefaultValue = getIntegerSetting(healthCheckSettingsDictionary, lookupSettingName, 4000);
                                        if (settingCurrentValue == settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 3;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', at default value", controllerSetting.Name, settingCurrentValue);
                                        }
                                        else if (settingCurrentValue < settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 2;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (<) less than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        else if (settingCurrentValue > settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 5;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (>) greater than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        break;

                                    case "tracked.object.ADD.registration.limit":
                                        // maxTrackedObjectAccountLimit = 4000
                                        settingDefaultValue = getIntegerSetting(healthCheckSettingsDictionary, lookupSettingName, 4000);
                                        if (settingCurrentValue == settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 3;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', at default value", controllerSetting.Name, settingCurrentValue);
                                        }
                                        else if (settingCurrentValue < settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 2;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (<) less than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        else if (settingCurrentValue > settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 5;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (>) greater than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        break;

                                    case "memory.ADD.registration.limit":
                                        // maxMemoryPointsPerAccountLimit = 4000
                                        settingDefaultValue = getIntegerSetting(healthCheckSettingsDictionary, lookupSettingName, 4000);
                                        if (settingCurrentValue == settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 3;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', at default value", controllerSetting.Name, settingCurrentValue);
                                        }
                                        else if (settingCurrentValue < settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 2;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (<) less than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        else if (settingCurrentValue > settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 5;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (>) greater than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        break;

                                    case "backend.registration.limit":
                                        // maxBackendsPerAccountLimit = 100000
                                        settingDefaultValue = getIntegerSetting(healthCheckSettingsDictionary, lookupSettingName, 10000);
                                        if (settingCurrentValue == settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 3;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', at default value", controllerSetting.Name, settingCurrentValue);
                                        }
                                        else if (settingCurrentValue < settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 2;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (<) less than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        else if (settingCurrentValue > settingDefaultValue)
                                        {
                                            healthCheckRuleResult1.Grade = 5;
                                            healthCheckRuleResult1.Description = String.Format("Controller setting '{0}'='{1}', (>) greater than default value '{2}'", controllerSetting.Name, settingCurrentValue, settingDefaultValue);
                                        }
                                        break;

                                    case "controller.metric.registration.limit":
                                        // Don't know what this one means
                                    default:
                                        break;
                                }

                                if (healthCheckRuleResult1.Grade != -1)
                                {
                                    healthCheckRuleResults.Add(healthCheckRuleResult1);
                                }
                            }
                        }
                    }
                }
            }

            return healthCheckRuleResults;
        }
        #endregion
    }
}
