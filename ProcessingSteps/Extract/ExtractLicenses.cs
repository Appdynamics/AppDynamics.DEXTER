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
                if (this.ShouldExecute(jobConfiguration) == false)
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
                            controllerApi.PrivateApiLogin();

                            #region Summary information for licenses

                            loggerConsole.Info("List of Applications");

                            string applicationsListJSON = controllerApi.GetControllerApplicationsForLicenseRule();
                            if (applicationsListJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationsListJSON, FilePathMap.LicenseApplicationsDataFilePath(jobTarget));

                            loggerConsole.Info("List of Machines");

                            string machinesListJSON = controllerApi.GetControllerSIMMachinesForLicenseRule();
                            if (machinesListJSON != String.Empty) FileIOHelper.SaveFileToPath(machinesListJSON, FilePathMap.LicenseSIMMachinesDataFilePath(jobTarget));

                            loggerConsole.Info("Account Summary");

                            string accountJSON = controllerApi.GetAccount();
                            if (accountJSON != String.Empty) FileIOHelper.SaveFileToPath(accountJSON, FilePathMap.LicenseAccountDataFilePath(jobTarget));

                            loggerConsole.Info("License");

                            string licenseJSON = controllerApi.GetLicense(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (licenseJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseJSON, FilePathMap.LicenseDataFilePath(jobTarget));

                            loggerConsole.Info("Usage Summary - All Except EUM");

                            string accountUsageSummaryJSON = controllerApi.GetLicenseUsageAllExceptEUMSummary(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (accountUsageSummaryJSON != String.Empty) FileIOHelper.SaveFileToPath(accountUsageSummaryJSON, FilePathMap.LicenseUsageSummaryAllExceptEUMDataFilePath(jobTarget));

                            loggerConsole.Info("Usage Summary - EUM");

                            string accountUsageSummaryEUMJSON = controllerApi.GetLicenseUsageEUMSummary(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (accountUsageSummaryEUMJSON != String.Empty) FileIOHelper.SaveFileToPath(accountUsageSummaryEUMJSON, FilePathMap.LicenseUsageSummaryEUMDataFilePath(jobTarget));

                            #endregion

                            #region Global license usage details time series

                            loggerConsole.Info("Usage Details - APM (combined)");

                            string usageDetailsJSON = controllerApi.GetLicenseUsageAPM(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageAPMDataFilePath(jobTarget));

                            // Databases 
                            loggerConsole.Info("Usage Details - Database");

                            usageDetailsJSON = controllerApi.GetLicenseUsageDatabase(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageDatabaseVisibilityDataFilePath(jobTarget));

                            // Infrastructure Visibility
                            loggerConsole.Info("Usage Details - Machine Agent");

                            usageDetailsJSON = controllerApi.GetLicenseUsageMachineAgent(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageMachineAgentDataFilePath(jobTarget));

                            loggerConsole.Info("Usage Details - Server Visibility");

                            usageDetailsJSON = controllerApi.GetLicenseUsageSIM(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageSIMDataFilePath(jobTarget));

                            loggerConsole.Info("Usage Details - Service Availability");

                            usageDetailsJSON = controllerApi.GetLicenseUsageServiceAvailability(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageServiceAvailabilityDataFilePath(jobTarget));

                            loggerConsole.Info("Usage Details - Network Visibility");

                            usageDetailsJSON = controllerApi.GetLicenseUsageNetworkVisibility(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageNetworkVisibilityDataFilePath(jobTarget));

                            // Analytics
                            loggerConsole.Info("Usage Details - Transaction Analytics");

                            usageDetailsJSON = controllerApi.GetLicenseUsageNetworkTransactionAnalytics(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageTransactionAnalyticsDataFilePath(jobTarget));

                            loggerConsole.Info("Usage Details - Log Analytics");

                            usageDetailsJSON = controllerApi.GetLicenseUsageNetworkLogAnalytics(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (usageDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(usageDetailsJSON, FilePathMap.LicenseUsageLogAnalyticsDataFilePath(jobTarget));
                            
                            #endregion

                            #region License Rules

                            loggerConsole.Info("License Rules");

                            string licenseRulesJSON = controllerApi.GetLicenseRules(fromTimeUnix, toTimeUnix, differenceInMinutes);
                            if (licenseRulesJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseRulesJSON, FilePathMap.LicenseRulesDataFilePath(jobTarget));

                            if (licenseRulesJSON != String.Empty)
                            {
                                JArray licenseRulesArray = JArray.Parse(licenseRulesJSON);
                                if (licenseRulesArray != null)
                                {
                                    foreach (JObject licenseRuleObject in licenseRulesArray)
                                    {
                                        string ruleID = getStringValueFromJToken(licenseRuleObject, "id");
                                        string ruleName = getStringValueFromJToken(licenseRuleObject, "name");

                                        loggerConsole.Info("License Rule Configuration - {0}", ruleName);

                                        string licenseRuleDetailsJSON = controllerApi.GetLicenseRuleConfiguration(ruleID);
                                        if (licenseRuleDetailsJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseRuleDetailsJSON, FilePathMap.LicenseRuleConfigurationDataFilePath(jobTarget, ruleName, ruleID));

                                        loggerConsole.Info("License Rule Usage - {0}", ruleName);

                                        string licenseRuleUsageJSON = controllerApi.GetLicenseRuleUsage(ruleID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                        if (licenseRuleUsageJSON != String.Empty) FileIOHelper.SaveFileToPath(licenseRuleUsageJSON, FilePathMap.LicenseRuleUsageDataFilePath(jobTarget, ruleName, ruleID));
                                    }
                                }
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
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
