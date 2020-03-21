using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractLicenses : JobStepBase
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

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {

                            #region Prepare time range

                            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                            #endregion

                            // Increase timeout of the extraction requests to quite a bit more for wider time ranges
                            controllerApi.Timeout = 3;

                            #region License assignments and consumption

                            loggerConsole.Info("Account ID");

                            long accountID = -1;
                            string myAccountJSON = controllerApi.GetAccountsMyAccount();
                            if (myAccountJSON != String.Empty)
                            {
                                JObject myAccount = JObject.Parse(myAccountJSON);
                                if (myAccount != null)
                                {
                                    accountID = getLongValueFromJToken(myAccount, "id");
                                }
                            }

                            loggerConsole.Info("License Modules");

                            string licenseModulesJSON = controllerApi.GetLicenseModules(accountID);
                            if (licenseModulesJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseModulesJSON, FilePathMap.LicenseModulesDataFilePath(jobTarget));

                            if (licenseModulesJSON != String.Empty)
                            {
                                JObject licenseModulesContainer = JObject.Parse(licenseModulesJSON);
                                if (licenseModulesContainer != null &&
                                    isTokenPropertyNull(licenseModulesContainer, "modules") == false)
                                {
                                    JArray licenseModulesArray = (JArray)licenseModulesContainer["modules"];
                                    foreach (JObject licenseModuleObject in licenseModulesArray)
                                    {
                                        string licenseModuleName = getStringValueFromJToken(licenseModuleObject, "name");
                                        loggerConsole.Info("License Module - {0}", licenseModuleName);

                                        string licenseModulePropertiesJSON = controllerApi.GetLicenseModuleProperties(accountID, licenseModuleName);
                                        if (licenseModulePropertiesJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseModulePropertiesJSON, FilePathMap.LicenseModulePropertiesDataFilePath(jobTarget, licenseModuleName));

                                        string licenseModuleUsagesJSON = controllerApi.GetLicenseModuleUsages(accountID, licenseModuleName, jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                                        if (licenseModuleUsagesJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseModuleUsagesJSON, FilePathMap.LicenseModuleUsagesDataFilePath(jobTarget, licenseModuleName));
                                    }
                                }
                            }

                            #endregion

                            #region License Rules

                            loggerConsole.Info("License Rules");

                            string licenseRulesJSON = controllerApi.GetLicenseRules();
                            if (licenseRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseRulesJSON, FilePathMap.LicenseRulesDataFilePath(jobTarget));

                            if (licenseRulesJSON != String.Empty)
                            {
                                JArray licenseRulesArray = JArray.Parse(licenseRulesJSON);
                                if (licenseRulesArray != null)
                                {
                                    using (ControllerApi controllerApi1 = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApi1.PrivateApiLogin();

                                        foreach (JObject licenseRuleObject in licenseRulesArray)
                                        {
                                            string ruleID = getStringValueFromJToken(licenseRuleObject, "id");
                                            string ruleName = getStringValueFromJToken(licenseRuleObject, "name");

                                            loggerConsole.Info("License Rule Configuration - {0}", ruleName);

                                            string licenseRuleDetailsJSON = controllerApi.GetLicenseRuleConfiguration(ruleID);
                                            if (licenseRuleDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseRuleDetailsJSON, FilePathMap.LicenseRuleConfigurationDataFilePath(jobTarget, ruleName, ruleID));
                                        }

                                        foreach (JObject licenseRuleObject in licenseRulesArray)
                                        {
                                            string ruleID = getStringValueFromJToken(licenseRuleObject, "id");
                                            string ruleName = getStringValueFromJToken(licenseRuleObject, "name");

                                            loggerConsole.Info("License Rule Usage - {0}", ruleName);

                                            string licenseRuleUsageJSON = controllerApi1.GetLicenseRuleUsage(ruleID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (licenseRuleUsageJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseRuleUsageJSON, FilePathMap.LicenseRuleUsageDataFilePath(jobTarget, ruleName, ruleID));
                                        }
                                    }

                                }
                            }

                            controllerApi.PrivateApiLogin();

                            loggerConsole.Info("List of Applications Referenced by License Rules");

                            string applicationsListJSON = controllerApi.GetControllerApplicationsForLicenseRule();
                            if (applicationsListJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationsListJSON, FilePathMap.LicenseApplicationsDataFilePath(jobTarget));

                            loggerConsole.Info("List of Machines Referenced by License Rules");

                            string machinesListJSON = controllerApi.GetControllerSIMMachinesForLicenseRule();
                            if (machinesListJSON != String.Empty) FileIOHelper.SaveFileToPath(machinesListJSON, FilePathMap.LicenseSIMMachinesDataFilePath(jobTarget));

                            #endregion

                            #region Account Summary

                            loggerConsole.Info("Account Summary");

                            string accountJSON = controllerApi.GetAccount();
                            if (accountJSON != String.Empty) FileIOHelper.SaveFileToPath(accountJSON, FilePathMap.LicenseAccountDataFilePath(jobTarget));

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

                    i++;
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
            logger.Trace("LicensedReports.Licenses={0}", programOptions.LicensedReports.Licenses);
            loggerConsole.Trace("LicensedReports.Licenses={0}", programOptions.LicensedReports.Licenses);
            if (programOptions.LicensedReports.Licenses == false)
            {
                loggerConsole.Warn("Not licensed for licenses");
                return false;
            }

            logger.Trace("Input.Licenses={0}", jobConfiguration.Input.Licenses);
            loggerConsole.Trace("Input.Licenses={0}", jobConfiguration.Input.Licenses);
            if (jobConfiguration.Input.Licenses == false)
            {
                loggerConsole.Trace("Skipping export of licenses");
            }
            return (jobConfiguration.Input.Licenses == true);
        }
    }
}
