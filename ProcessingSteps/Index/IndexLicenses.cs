using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexLicenses : JobStepIndexBase
    {
        //private const string LICENSE_DISPLAY_NAME_APM_ANY = "APM Any";
        //private const string LICENSE_DISPLAY_NAME_JAVA = "Java";
        private const string LICENSE_DISPLAY_NAME_BROWSER_ANALYTICS = "BROWSER_BIQ";
        private const string LICENSE_DISPLAY_NAME_DATABASE = "DATABASE";
        //private const string LICENSE_DISPLAY_NAME_DOT_NET = ".NET";
        //private const string LICENSE_DISPLAY_NAME_GOLANG = "Golang SDK";
        private const string LICENSE_DISPLAY_NAME_LOG_ANALYTICS = "LOG_BIQ";
        //private const string LICENSE_DISPLAY_NAME_MACHINE_AGENT = "Machine";
        private const string LICENSE_DISPLAY_NAME_MOBILE_ANALYTICS = "MOBILE_BIQ";
        //private const string LICENSE_DISPLAY_NAME_NATIVE_SDK = "C/C++ SDK";
        //private const string LICENSE_DISPLAY_NAME_WEB_SERVER = "Web Server";
        //private const string LICENSE_DISPLAY_NAME_NETWORK_VIZ = "Network Visibility";
        //private const string LICENSE_DISPLAY_NAME_NODEJS = "Node.js";
        //private const string LICENSE_DISPLAY_NAME_PHP = "PHP";
        //private const string LICENSE_DISPLAY_NAME_PYTHON = "Python";
        //private const string LICENSE_DISPLAY_NAME_SERVICE_AVAILABILITY = "Service Availability";
        //private const string LICENSE_DISPLAY_NAME_SIM = "Server Visibility";
        private const string LICENSE_DISPLAY_NAME_TRANSACTION_ANALYTICS = "TRANSACTION_BIQ";
        //private const string LICENSE_DISPLAY_NAME_WMB = "IBM_IIB_WMB";
        private const string LICENSE_DISPLAY_NAME_BROWSER_RUM = "BROWSER_RUM";
        private const string LICENSE_DISPLAY_NAME_MOBILE_RUM = "MOBILE_RUM";
        private const string LICENSE_DISPLAY_NAME_SYNTHETIC_HOSTED = "SYNTHETIC_HOSTED";
        private const string LICENSE_DISPLAY_NAME_SYNTHETIC_PRIVATE = "SYNTHETIC_PRIVATE";
        private const string LICENSE_DISPLAY_NAME_IOT_ANALYTICS = "IOT_BIQ";

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

                    stepTimingTarget.NumEntities = 0;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        int differenceInMinutes = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        #endregion

                        #region Account Summary

                        loggerConsole.Info("Account Summary");

                        LicenseAccountSummary licenseAccountSummary = new LicenseAccountSummary();

                        JObject accountSummaryContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.LicenseAccountDataFilePath(jobTarget));
                        if (accountSummaryContainerObject != null && isTokenPropertyNull(accountSummaryContainerObject, "account") == false)
                        {
                            JObject accountSummaryLicenseObject = (JObject)accountSummaryContainerObject["account"];

                            licenseAccountSummary.Controller = jobTarget.Controller;

                            licenseAccountSummary.AccountID = getLongValueFromJToken(accountSummaryLicenseObject, "id");
                            licenseAccountSummary.AccountName = getStringValueFromJToken(accountSummaryLicenseObject, "name");
                            licenseAccountSummary.AccountNameGlobal = getStringValueFromJToken(accountSummaryLicenseObject, "globalAccountName");
                            licenseAccountSummary.AccountNameEUM = getStringValueFromJToken(accountSummaryLicenseObject, "eumAccountName");

                            licenseAccountSummary.AccessKey1 = getStringValueFromJToken(accountSummaryLicenseObject, "accessKey");
                            licenseAccountSummary.AccessKey2 = getStringValueFromJToken(accountSummaryLicenseObject, "key");
                            licenseAccountSummary.LicenseKeyEUM = getStringValueFromJToken(accountSummaryLicenseObject, "eumCloudLicenseKey");
                            licenseAccountSummary.ServiceKeyES = getStringValueFromJToken(accountSummaryLicenseObject, "eumEventServiceKey");

                            licenseAccountSummary.ExpirationDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(accountSummaryLicenseObject, "expirationDate"));

                            licenseAccountSummary.LicenseLink = String.Format(DEEPLINK_LICENSE, licenseAccountSummary.Controller, DEEPLINK_THIS_TIMERANGE);

                            List<LicenseAccountSummary> licenseAccountSummaryList = new List<LicenseAccountSummary>(1);
                            licenseAccountSummaryList.Add(licenseAccountSummary);
                            FileIOHelper.WriteListToCSVFile(licenseAccountSummaryList, new LicenseAccountSummaryReportMap(), FilePathMap.LicenseAccountIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + licenseAccountSummaryList.Count;

                        }

                        #endregion

                        #region Account License Entitlements

                        loggerConsole.Info("Account License");

                        List<License> licensesList = new List<License>(20);

                        JObject licensesContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.LicenseDataFilePath(jobTarget));
                        JObject licensesMoreDetailAllButEUMContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.LicenseUsageSummaryAllExceptEUMDataFilePath(jobTarget));
                        JObject licensesMoreDetailEUMContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.LicenseUsageSummaryEUMDataFilePath(jobTarget));
                        if (licensesContainerObject != null)
                        {
                            // Parse almost all except User Experience licenses
                            foreach (JToken licenseContainerToken in licensesContainerObject.Children())
                            {
                                License license = new License();

                                license.Controller = jobTarget.Controller;
                                license.AccountID = licenseAccountSummary.AccountID;
                                license.AccountName = licenseAccountSummary.AccountName;

                                // Duration
                                license.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                license.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                license.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                license.FromUtc = jobConfiguration.Input.TimeRange.From;
                                license.ToUtc = jobConfiguration.Input.TimeRange.To;
                                
                                license.AgentType = String.Empty;
                                JToken licenseMinConsumptionContainer = null;
                                switch (licenseContainerToken.Path)
                                {
                                    case "apmAgentLicenseProperties" :
                                        break;
                                    case "apmLicenseProperties" :
                                        license.AgentType = "APM";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "apm");
                                        break;
                                    case "appAgentLicenseProperties" :
                                        license.AgentType = "JAVA";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "java");
                                        break;
                                    case "browserAnalyticsLicenseProperties" :
                                        license.AgentType = LICENSE_DISPLAY_NAME_BROWSER_ANALYTICS;
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "browser-analytics");
                                        break;
                                    case "dbLicenseProperties" :
                                        license.AgentType = LICENSE_DISPLAY_NAME_DATABASE;
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "database");
                                        break;
                                    case "dotNetLicenseProperties" :
                                        license.AgentType = "DOT_NET";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "dot-net");
                                        break;
                                    case "golangSDKLicenseProperties" :
                                        license.AgentType = "GOLANG_SDK";
                                        // Not sure about the name of this container, not seen it in my development. Guessing
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "golang-sdk");
                                        break;
                                    case "logAnalyticsLicenseProperties" :
                                        license.AgentType = LICENSE_DISPLAY_NAME_LOG_ANALYTICS;
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "log-analytics");
                                        break;
                                    case "machineLicenseProperties" :
                                        license.AgentType = "MACHINE_AGENT";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "machine-agent");
                                        break;
                                    case "mobileAnalyticsLicenseProperties" :
                                        license.AgentType = LICENSE_DISPLAY_NAME_MOBILE_ANALYTICS;
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "mobile-analytics");
                                        break;
                                    case "nativeDynamicLicenseProperties" :
                                        break;
                                    case "nativeLicenseProperties" :
                                        break;
                                    case "nativeSDKLicenseProperties" :
                                        license.AgentType = "NATIVE_SDK";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "native-sdk");
                                        break;
                                    case "nativeWebServerLicenseProperties" :
                                        license.AgentType = "NATIVE_WEB_SERVER";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "native-web-server");
                                        break;
                                    case "netvizLicenseProperties" :
                                        license.AgentType = "NETVIZ";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "netviz");
                                        break;
                                    case "nodeJsLicenseProperties" :
                                        license.AgentType = "NODEJS";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "nodejs");
                                        break;
                                    case "phpLicenseProperties" :
                                        license.AgentType = "PHP";
                                        // Not sure about the name of this container, not seen it in my development. Guessing
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "php");
                                        break;
                                    case "pythonLicenseProperties" :
                                        license.AgentType = "PYTHON";
                                        // Not sure about the name of this container, not seen it in my development. Guessing
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "python");
                                        break;
                                    case "serverAvailMonLicenseProperties" :
                                        license.AgentType = "SERVICE_AVAIL_MON";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "service-avail-mon");
                                        break;
                                    case "simAgentLicenseProperties" :
                                        license.AgentType = "SIM_MACHINE_AGENT";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "sim-machine-agent");
                                        break;
                                    case "transactionAnalyticsLicenseProperties" :
                                        license.AgentType = LICENSE_DISPLAY_NAME_TRANSACTION_ANALYTICS;
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "transaction-analytics");
                                        break;
                                    case "wmbAgentLicenseProperties" :
                                        license.AgentType = "WMB_AGENT";
                                        licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "wmb-agent");
                                        break;
                                    default:
                                        // Unknown, let's just replace the value of the string
                                        logger.Warn("Unknown type of license {0} in license", licenseContainerToken.Path);
                                        license.AgentType = licenseContainerToken.Path.Replace("LicenseProperties", "");
                                        break;
                                }

                                // Parse licenses
                                if (isTokenNull(licenseContainerToken) == false &&
                                    licenseContainerToken.First.HasValues == true)
                                {
                                    JObject licenseContainerObject = (JObject)licenseContainerToken.First;
                                    license.Edition = getStringValueFromJToken(licenseContainerObject, "edition");
                                    license.Model = getStringValueFromJToken(licenseContainerObject, "licenseModel");

                                    long valueLong = 0;
                                    double valueDouble = 0;
                                    if (long.TryParse(getStringValueFromJToken(licenseContainerObject, "expirationDate"), out valueLong) == true)
                                    {
                                        license.ExpirationDate = UnixTimeHelper.ConvertFromUnixTimestamp(valueLong);
                                    }

                                    if (long.TryParse(getStringValueFromJToken(licenseContainerObject, "numOfProvisionedLicense"), out valueLong) == true)
                                    {
                                        license.Licenses = valueLong;
                                    }

                                    if (long.TryParse(getStringValueFromJToken(licenseContainerObject, "maxAllowedLicenses"), out valueLong) == true)
                                    {
                                        license.Units = valueLong;
                                    }

                                    if (long.TryParse(getStringValueFromJToken(licenseContainerObject, "peakUsage"), out valueLong) == true)
                                    {
                                        license.Peak = valueLong;
                                    }
                                    else if (double.TryParse(getStringValueFromJToken(licenseContainerObject, "peakUsage"), out valueDouble) == true)
                                    {
                                        // Transaction and Browser Analytics
                                        // Not sure what this is, but it shows as double
                                        if (license.AgentType == LICENSE_DISPLAY_NAME_TRANSACTION_ANALYTICS)
                                        {
                                            // Not sure what this is, but it shows as double
                                        }
                                        else if (license.AgentType == LICENSE_DISPLAY_NAME_BROWSER_ANALYTICS)
                                        {
                                        }
                                        license.Peak = valueDouble;
                                    }

                                    int valueInt = 0;
                                    if (int.TryParse(getStringValueFromJToken(licenseContainerObject, "dataRetentionPeriod"), out valueInt) == true)
                                    {
                                        license.Retention = valueInt;
                                    }

                                    license.Min = getLongValueFromJToken(licenseMinConsumptionContainer, "minUnitsUsed");

                                    licensesList.Add(license);
                                }
                            }

                            // Parse User Experience licenses
                            if (licensesMoreDetailEUMContainerObject != null)
                            {
                                #region Browser RUM

                                License license = new License();

                                license.Controller = jobTarget.Controller;
                                license.AccountID = licenseAccountSummary.AccountID;
                                license.AccountName = licenseAccountSummary.AccountName;

                                // Duration
                                license.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                license.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                license.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                license.FromUtc = jobConfiguration.Input.TimeRange.From;
                                license.ToUtc = jobConfiguration.Input.TimeRange.To;

                                license.Edition = getStringValueFromJToken(licensesMoreDetailEUMContainerObject, "licenseType");
                                license.Model = getBoolValueFromJToken(licensesMoreDetailEUMContainerObject, "licenseAllowsOverages") == true ? "Overage Allowed" : "No Overage";

                                license.AgentType = LICENSE_DISPLAY_NAME_BROWSER_RUM;
                                JToken licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "browser-rum");

                                license.StartDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "currentUsagePeriodStartMillis"));
                                license.ExpirationDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "currentUsagePeriodEndMillis"));

                                license.Licenses = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "licenseUnits");
                                license.Peak = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "consumedPageViews");
                                license.Units= getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "allocatedPageViews");
                                license.Min = getLongValueFromJToken(licenseMinConsumptionContainer, "minUnitsUsed");

                                licensesList.Add(license);

                                #endregion

                                #region Mobile RUM

                                license = new License();

                                license.Controller = jobTarget.Controller;
                                license.AccountID = licenseAccountSummary.AccountID;
                                license.AccountName = licenseAccountSummary.AccountName;

                                // Duration
                                license.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                license.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                license.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                license.FromUtc = jobConfiguration.Input.TimeRange.From;
                                license.ToUtc = jobConfiguration.Input.TimeRange.To;

                                license.Edition = getStringValueFromJToken(licensesMoreDetailEUMContainerObject, "mobileLicenseType");
                                license.Model = getBoolValueFromJToken(licensesMoreDetailEUMContainerObject, "mobileLicenseAllowsOverages") == true ? "Overage Allowed" : "No Overage";

                                license.AgentType = LICENSE_DISPLAY_NAME_MOBILE_RUM;
                                licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "mobile-rum");

                                license.StartDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "mobileCurrentUsagePeriodStartMillis"));
                                license.ExpirationDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "mobileCurrentUsagePeriodEndMillis"));

                                license.Licenses = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "mobileLicenseUnits");
                                license.Peak = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "consumedMobileAgents");
                                license.Units = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "allocatedMobileAgents");
                                license.Min = getLongValueFromJToken(licenseMinConsumptionContainer, "minUnitsUsed");

                                licensesList.Add(license);

                                #endregion

                                #region Synthetic Hosted

                                license = new License();

                                license.Controller = jobTarget.Controller;
                                license.AccountID = licenseAccountSummary.AccountID;
                                license.AccountName = licenseAccountSummary.AccountName;

                                // Duration
                                license.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                license.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                license.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                license.FromUtc = jobConfiguration.Input.TimeRange.From;
                                license.ToUtc = jobConfiguration.Input.TimeRange.To;

                                license.Edition = getStringValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticLicenseType");

                                license.AgentType = LICENSE_DISPLAY_NAME_SYNTHETIC_HOSTED;
                                licenseMinConsumptionContainer = getJTokenValueFromJToken(licensesMoreDetailAllButEUMContainerObject, "synthetic");

                                license.StartDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticCurrentUsagePeriodStartMillis"));
                                license.ExpirationDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticCurrentUsagePeriodEndMillis"));

                                license.Licenses = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticLicenseUnits");
                                license.Peak = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "consumedSyntheticMeasurementUnits");
                                license.Units = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "allocatedSyntheticMeasurementUnits");
                                license.Min = getLongValueFromJToken(licenseMinConsumptionContainer, "minUnitsUsed");

                                licensesList.Add(license);

                                #endregion

                                #region Synthetic Private

                                license = new License();

                                license.Controller = jobTarget.Controller;
                                license.AccountID = licenseAccountSummary.AccountID;
                                license.AccountName = licenseAccountSummary.AccountName;

                                // Duration
                                license.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                license.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                license.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                license.FromUtc = jobConfiguration.Input.TimeRange.From;
                                license.ToUtc = jobConfiguration.Input.TimeRange.To;

                                license.Edition = getStringValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticLicenseType");

                                license.AgentType = LICENSE_DISPLAY_NAME_SYNTHETIC_PRIVATE;

                                license.Licenses = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticPrivateAgentLicenseUnits");
                                license.Peak = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticPrivateAgentUnitsUtilized");
                                license.Units = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "syntheticPrivateAgentUnitsAllocated");

                                licensesList.Add(license);

                                #endregion

                                #region IoT

                                license = new License();

                                license.Controller = jobTarget.Controller;
                                license.AccountID = licenseAccountSummary.AccountID;
                                license.AccountName = licenseAccountSummary.AccountName;

                                // Duration
                                license.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                license.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                license.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                license.FromUtc = jobConfiguration.Input.TimeRange.From;
                                license.ToUtc = jobConfiguration.Input.TimeRange.To;

                                license.Edition = getStringValueFromJToken(licensesMoreDetailEUMContainerObject, "iotLicenseType");
                                license.Model = getBoolValueFromJToken(licensesMoreDetailEUMContainerObject, "iotLicenseAllowsOverages") == true ? "Overage Allowed" : "No Overage";

                                license.AgentType = LICENSE_DISPLAY_NAME_IOT_ANALYTICS;
                                license.StartDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "iotCurrentUsagePeriodStartMillis"));
                                license.ExpirationDate = UnixTimeHelper.ConvertFromUnixTimestamp(getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "iotCurrentUsagePeriodEndMillis"));

                                license.Licenses = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "iotLicenseUnits");
                                license.Peak = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "uniqueIotDevices");
                                license.Units = getLongValueFromJToken(licensesMoreDetailEUMContainerObject, "allocatedIotDevices");

                                licensesList.Add(license);

                                #endregion
                            }

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + licensesList.Count;

                            licensesList = licensesList.OrderBy(l => l.AgentType).ToList();
                            FileIOHelper.WriteListToCSVFile(licensesList, new LicenseReportMap(), FilePathMap.LicensesIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region License Rules

                        loggerConsole.Info("License Rules");

                        JArray licenseApplicationsReferenceArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseApplicationsDataFilePath(jobTarget));
                        JArray licenseSIMMachinesReferenceArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseSIMMachinesDataFilePath(jobTarget));

                        #region Preload the application and machine mapping

                        Dictionary<string, string> applicationsUsedByRules = null;
                        Dictionary<string, string> simMachinesUsedByRules = null;

                        if (licenseApplicationsReferenceArray == null)
                        {
                            applicationsUsedByRules = new Dictionary<string, string>(0);
                        }
                        else
                        {
                            applicationsUsedByRules = new Dictionary<string, string>(licenseApplicationsReferenceArray.Count);
                            foreach (JObject applicationObject in licenseApplicationsReferenceArray)
                            {
                                applicationsUsedByRules.Add(getStringValueFromJToken(applicationObject["objectReference"], "id"), getStringValueFromJToken(applicationObject, "name"));
                            }
                        }

                        if (licenseSIMMachinesReferenceArray == null)
                        {
                            simMachinesUsedByRules = new Dictionary<string, string>(0);
                        }
                        else
                        {
                            simMachinesUsedByRules = new Dictionary<string, string>(licenseSIMMachinesReferenceArray.Count);
                            foreach (JObject machineObject in licenseSIMMachinesReferenceArray)
                            {
                                simMachinesUsedByRules.Add(getStringValueFromJToken(machineObject["objectReference"], "id"), getStringValueFromJToken(machineObject, "name"));
                            }
                        }

                        List<ControllerApplication> controllerApplicationsList = FileIOHelper.ReadListFromCSVFile<ControllerApplication>(FilePathMap.ControllerApplicationsIndexFilePath(jobTarget), new ControllerApplicationReportMap());

                        #endregion

                        List<LicenseRule> licenseRulesList = new List<LicenseRule>(10);
                        List<LicenseRuleScope> licenseRuleScopesList = new List<LicenseRuleScope>(100);

                        JArray licenseRulesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseRulesDataFilePath(jobTarget));
                        if (licenseRulesArray != null)
                        {
                            foreach (JObject licenseRuleObject in licenseRulesArray)
                            {
                                string ruleID = getStringValueFromJToken(licenseRuleObject, "id");
                                string ruleName = getStringValueFromJToken(licenseRuleObject, "name");

                                JObject licenseRuleDetailsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.LicenseRuleConfigurationDataFilePath(jobTarget, ruleName, ruleID));
                                if (licenseRuleDetailsObject != null)
                                {
                                    LicenseRule licenseRuleTemplate = new LicenseRule();

                                    licenseRuleTemplate.Controller = jobTarget.Controller;
                                    licenseRuleTemplate.AccountID = licenseAccountSummary.AccountID;
                                    licenseRuleTemplate.AccountName = licenseAccountSummary.AccountName;

                                    // Duration
                                    licenseRuleTemplate.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                                    licenseRuleTemplate.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                    licenseRuleTemplate.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                    licenseRuleTemplate.FromUtc = jobConfiguration.Input.TimeRange.From;
                                    licenseRuleTemplate.ToUtc = jobConfiguration.Input.TimeRange.To;

                                    licenseRuleTemplate.RuleName = getStringValueFromJToken(licenseRuleObject, "name");
                                    licenseRuleTemplate.RuleID = getStringValueFromJToken(licenseRuleObject, "id");

                                    licenseRuleTemplate.AccessKey = getStringValueFromJToken(licenseRuleObject, "access_key");

                                    licenseRuleTemplate.RuleLicenses = getLongValueFromJToken(licenseRuleObject, "total_licenses");
                                    licenseRuleTemplate.RulePeak = getLongValueFromJToken(licenseRuleObject, "peak_usage");

                                    if (isTokenPropertyNull(licenseRuleDetailsObject, "constraints") == false)
                                    {
                                        JArray licenseRuleConstraintsArray = (JArray)licenseRuleDetailsObject["constraints"];
                                        foreach (JObject licenseConstraintObject in licenseRuleConstraintsArray)
                                        {
                                            LicenseRuleScope licenseRuleScope = new LicenseRuleScope();

                                            licenseRuleScope.Controller = licenseRuleTemplate.Controller;
                                            licenseRuleScope.AccountID = licenseRuleTemplate.AccountID;
                                            licenseRuleScope.AccountName = licenseRuleTemplate.AccountName;

                                            licenseRuleScope.RuleName = licenseRuleTemplate.RuleName;
                                            licenseRuleScope.RuleID = licenseRuleTemplate.RuleID;

                                            licenseRuleScope.ScopeSelector = getStringValueFromJToken(licenseConstraintObject, "constraint_type");

                                            string scopeType = getStringValueFromJToken(licenseConstraintObject, "entity_type_id");
                                            if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.ApplicationEntity")
                                            {
                                                licenseRuleScope.EntityType = "Application";
                                            }
                                            else if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.MachineEntity")
                                            {
                                                licenseRuleScope.EntityType = "Machine";
                                            }

                                            if (licenseRuleScope.ScopeSelector == "ALLOW_ALL")
                                            {
                                                licenseRuleScope.MatchType = "EQUALS";
                                                if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.ApplicationEntity")
                                                {
                                                    licenseRuleScope.EntityName = "[Any Application]";
                                                    licenseRuleTemplate.NumApplications++;
                                                }
                                                else if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.MachineEntity")
                                                {
                                                    licenseRuleScope.EntityName = "[Any Machine]";
                                                    licenseRuleTemplate.NumServers++;
                                                }
                                                licenseRuleScopesList.Add(licenseRuleScope);
                                            }
                                            else if (isTokenPropertyNull(licenseConstraintObject, "match_conditions") == false)
                                            {
                                                JArray licenseRuleMatcheConditionsArray = (JArray)licenseConstraintObject["match_conditions"];

                                                foreach (JObject licenseRuleMatchConditionObject in licenseRuleMatcheConditionsArray)
                                                {
                                                    LicenseRuleScope licenseRuleScopeMatchCondition = licenseRuleScope.Clone();

                                                    licenseRuleScopeMatchCondition.MatchType = getStringValueFromJToken(licenseRuleMatchConditionObject, "match_type");
                                                    if (licenseRuleScopeMatchCondition.MatchType == "EQUALS" &&
                                                        getStringValueFromJToken(licenseRuleMatchConditionObject, "attribute_type") == "ID")
                                                    {
                                                        string applicationID = getStringValueFromJToken(licenseRuleMatchConditionObject, "match_string");

                                                        if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.ApplicationEntity")
                                                        {
                                                            if (applicationsUsedByRules.ContainsKey(applicationID) == true &&
                                                                controllerApplicationsList != null)
                                                            {
                                                                ControllerApplication controllerApplication = controllerApplicationsList.Where(a => a.ApplicationName == applicationsUsedByRules[applicationID]).FirstOrDefault();
                                                                if (controllerApplication != null)
                                                                {
                                                                    licenseRuleScopeMatchCondition.EntityName = controllerApplication.ApplicationName;
                                                                    licenseRuleScopeMatchCondition.EntityType = controllerApplication.Type;
                                                                    licenseRuleScopeMatchCondition.EntityID = controllerApplication.ApplicationID;
                                                                }
                                                            }
                                                            licenseRuleTemplate.NumApplications++;
                                                        }
                                                        else if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.MachineEntity")
                                                        {
                                                            if (simMachinesUsedByRules.ContainsKey(applicationID) == true)
                                                            {
                                                                licenseRuleScopeMatchCondition.EntityName = simMachinesUsedByRules[applicationID];
                                                            }
                                                            licenseRuleTemplate.NumServers++;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        licenseRuleScopeMatchCondition.EntityName = getStringValueFromJToken(licenseRuleMatchConditionObject, "match_string");
                                                        licenseRuleScopeMatchCondition.EntityID = -1;
                                                        if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.ApplicationEntity")
                                                        {
                                                            licenseRuleTemplate.NumApplications++;
                                                        }
                                                        else if (scopeType == "com.appdynamics.modules.apm.topology.impl.persistenceapi.model.MachineEntity")
                                                        {
                                                            licenseRuleTemplate.NumServers++;
                                                        }
                                                    }

                                                    licenseRuleScopesList.Add(licenseRuleScopeMatchCondition);
                                                }
                                            }
                                        }
                                    }

                                    if (isTokenPropertyNull(licenseRuleDetailsObject, "entitlements") == false)
                                    {
                                        JArray licenseRuleEntitlementsArray = (JArray)licenseRuleDetailsObject["entitlements"];
                                        foreach (JObject licenseEntitlementObject in licenseRuleEntitlementsArray)
                                        {
                                            LicenseRule licenseRule = licenseRuleTemplate.Clone();

                                            licenseRule.Licenses = getLongValueFromJToken(licenseEntitlementObject, "number_of_licenses");

                                            licenseRule.AgentType = getStringValueFromJToken(licenseEntitlementObject, "license_module_type");

                                            licenseRulesList.Add(licenseRule);
                                        }
                                    }
                                }
                            }

                            licenseRulesList = licenseRulesList.OrderBy(l => l.RuleName).ThenBy(l => l.AgentType).ToList();
                            FileIOHelper.WriteListToCSVFile(licenseRulesList, new LicenseRuleReportMap(), FilePathMap.LicenseRulesIndexFilePath(jobTarget));

                            licenseRuleScopesList = licenseRuleScopesList.OrderBy(l => l.RuleName).ThenBy(l => l.ScopeSelector).ThenBy(l => l.EntityType).ThenBy(l => l.EntityName).ToList();
                            FileIOHelper.WriteListToCSVFile(licenseRuleScopesList, new LicenseRuleScopeReportMap(), FilePathMap.LicenseRuleScopesIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Global License consumption details

                        loggerConsole.Info("Global License Consumption");

                        // Typically there are 12 values per hour
                        // Assume there will be 16 different license types
                        List<LicenseValue> licenseValuesGlobalList = new List<LicenseValue>(jobConfiguration.Input.HourlyTimeRanges.Count * 12 * 16);

                        // Parse the APM results first
                        JArray licenseValuesAPMContainerArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageAPMDataFilePath(jobTarget));
                        if (licenseValuesAPMContainerArray != null)
                        {
                            foreach (JObject licenseValuesContainerObject in licenseValuesAPMContainerArray)
                            {
                                License thisLicense = licensesList.Where(l => l.AgentType == getStringValueFromJToken(licenseValuesContainerObject, "licenseModuleType")).FirstOrDefault();

                                // If we have the license looked up, look through the values
                                if (thisLicense != null && 
                                    isTokenPropertyNull(licenseValuesContainerObject, "graphPoints") == false)
                                {
                                    // Looks like [[ 1555484400000, 843 ], [ 1555488000000, 843 ]]
                                    JArray licenseValuesAPMArray = (JArray)licenseValuesContainerObject["graphPoints"];

                                    List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesAPMArray, thisLicense);
                                    if (licenseValuesList != null)
                                    {
                                        licenseValuesGlobalList.AddRange(licenseValuesList);
                                    }
                                }
                            }
                        }

                        // Parse individual graphs for other types of agents
                        JArray licenseValuesDBArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageDatabaseVisibilityDataFilePath(jobTarget));
                        if (licenseValuesDBArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == LICENSE_DISPLAY_NAME_DATABASE).FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesDBArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        JArray licenseValuesMAArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageMachineAgentDataFilePath(jobTarget));
                        if (licenseValuesMAArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == "MACHINE_AGENT").FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesMAArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        JArray licenseValuesSIMArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageSIMDataFilePath(jobTarget));
                        if (licenseValuesSIMArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == "SIM_MACHINE_AGENT").FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesSIMArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        JArray licenseValuesSAArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageServiceAvailabilityDataFilePath(jobTarget));
                        if (licenseValuesSAArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == "SERVICE_AVAIL_MON").FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesSAArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        JArray licenseValuesNetVizArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageNetworkVisibilityDataFilePath(jobTarget));
                        if (licenseValuesNetVizArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == "NETVIZ").FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesNetVizArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        JArray licenseValuesTXAnalyticsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageTransactionAnalyticsDataFilePath(jobTarget));
                        if (licenseValuesTXAnalyticsArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == LICENSE_DISPLAY_NAME_TRANSACTION_ANALYTICS).FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesTXAnalyticsArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        JArray licenseValuesLogAnalyticsArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.LicenseUsageLogAnalyticsDataFilePath(jobTarget));
                        if (licenseValuesLogAnalyticsArray != null)
                        {
                            License thisLicense = licensesList.Where(l => l.AgentType == LICENSE_DISPLAY_NAME_LOG_ANALYTICS).FirstOrDefault(); ;
                            if (thisLicense != null)
                            {
                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesLogAnalyticsArray, thisLicense);
                                if (licenseValuesList != null)
                                {
                                    licenseValuesGlobalList.AddRange(licenseValuesList);
                                }
                            }
                        }

                        FileIOHelper.WriteListToCSVFile(licenseValuesGlobalList, new LicenseValueReportMap(), FilePathMap.LicenseUsageAccountIndexFilePath(jobTarget));

                        #endregion

                        #region License Rule consumption details

                        loggerConsole.Info("Rules License Consumption");

                        // Typically there are 12 values per hour
                        // Assume there will be 16 different license types
                        List<LicenseValue> licenseValuesRulesList = new List<LicenseValue>(jobConfiguration.Input.HourlyTimeRanges.Count * 12 * 16 * licenseRulesList.Count);

                        foreach (LicenseRule licenseRule in licenseRulesList)
                        {
                            JObject licenseValuesForRuleContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.LicenseRuleUsageDataFilePath(jobTarget, licenseRule.RuleName, licenseRule.RuleID));

                            if (licenseValuesForRuleContainerObject != null)
                            {
                                if (isTokenPropertyNull(licenseValuesForRuleContainerObject, "apmStackGraphViewData") == false)
                                {
                                    // Parse the APM results first
                                    JArray licenseValuesForRuleAPMContainerArray = (JArray)licenseValuesForRuleContainerObject["apmStackGraphViewData"];
                                    if (licenseValuesForRuleAPMContainerArray != null)
                                    {
                                        foreach (JObject licenseValuesContainerObject in licenseValuesForRuleAPMContainerArray)
                                        {
                                            string licenseType = getStringValueFromJToken(licenseValuesContainerObject, "licenseModuleType");

                                            // If we have the license looked up, look through the values
                                            if (licenseType != String.Empty &&
                                                isTokenPropertyNull(licenseValuesContainerObject, "graphPoints") == false)
                                            {
                                                // Looks like [[ 1555484400000, 843 ], [ 1555488000000, 843 ]]
                                                JArray licenseValuesAPMArray = (JArray)licenseValuesContainerObject["graphPoints"];

                                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesAPMArray, licenseRule, licenseType);
                                                if (licenseValuesList != null)
                                                {
                                                    licenseValuesRulesList.AddRange(licenseValuesList);
                                                }
                                            }
                                        }
                                    }
                                }
                                if (isTokenPropertyNull(licenseValuesForRuleContainerObject, "nonApmModuleDetailViewData") == false)
                                {
                                    // Parse the non-APM results second
                                    JArray licenseValuesForRuleNonAPMContainerArray = (JArray)licenseValuesForRuleContainerObject["nonApmModuleDetailViewData"];
                                    if (licenseValuesForRuleNonAPMContainerArray != null)
                                    {
                                        foreach (JObject licenseValuesContainerObject in licenseValuesForRuleNonAPMContainerArray)
                                        {
                                            string licenseType = getStringValueFromJToken(licenseValuesContainerObject, "licenseModuleType");

                                            // If we have the license looked up, look through the values
                                            if (licenseType != String.Empty &&
                                                isTokenPropertyNull(licenseValuesContainerObject, "graphPoints") == false)
                                            {
                                                // Looks like [[ 1555484400000, 843 ], [ 1555488000000, 843 ]]
                                                JArray licenseValuesAPMArray = (JArray)licenseValuesContainerObject["graphPoints"];

                                                List<LicenseValue> licenseValuesList = parseLicenseValuesArray(licenseValuesAPMArray, licenseRule, licenseType);
                                                if (licenseValuesList != null)
                                                {
                                                    licenseValuesRulesList.AddRange(licenseValuesList);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        FileIOHelper.WriteListToCSVFile(licenseValuesRulesList, new LicenseValueReportMap(), FilePathMap.LicenseUsageRulesIndexFilePath(jobTarget));

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.ControllerLicensesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.ControllerLicensesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.LicenseAccountIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.LicenseAccountIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.LicenseAccountReportFilePath(), FilePathMap.LicenseAccountIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.LicensesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.LicensesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.LicensesReportFilePath(), FilePathMap.LicensesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.LicenseRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.LicenseRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.LicenseRulesReportFilePath(), FilePathMap.LicenseRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.LicenseUsageAccountIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.LicenseUsageAccountIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.LicenseUsageAccountReportFilePath(), FilePathMap.LicenseUsageAccountIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.LicenseUsageRulesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.LicenseUsageRulesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.LicenseUsageRulesReportFilePath(), FilePathMap.LicenseUsageRulesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.LicenseRuleScopesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.LicenseRuleScopesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.LicenseRuleScopesReportFilePath(), FilePathMap.LicenseRuleScopesIndexFilePath(jobTarget));
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
                loggerConsole.Trace("Skipping index of licenses");
            }
            return (jobConfiguration.Input.Licenses == true);
        }

        private static List<LicenseValue> parseLicenseValuesArray(JArray licenseValuesArray, License thisLicense)
        {
            List<LicenseValue> licenseValuesList = new List<LicenseValue>(licenseValuesArray.Count);

            foreach (JToken licenseValueToken in licenseValuesArray)
            {
                try
                {
                    JArray licenseValueArray = (JArray)licenseValueToken;

                    if (licenseValueArray.Count == 2)
                    {
                        LicenseValue licenseValue = new LicenseValue();

                        licenseValue.Controller = thisLicense.Controller;
                        licenseValue.AccountName = thisLicense.AccountName;
                        licenseValue.AccountID = thisLicense.AccountID;

                        licenseValue.RuleName = "Account";

                        licenseValue.AgentType = thisLicense.AgentType;

                        licenseValue.LicenseEventTimeUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)licenseValueArray[0]);
                        licenseValue.LicenseEventTime = licenseValue.LicenseEventTimeUtc.ToLocalTime();

                        if (isTokenNull(licenseValueArray[1]) == false)
                        {
                            licenseValue.Value = (long)licenseValueArray[1];
                        }

                        licenseValuesList.Add(licenseValue);
                    }
                }
                catch { }
            }

            return licenseValuesList;
        }

        private static List<LicenseValue> parseLicenseValuesArray(JArray licenseValuesArray, LicenseRule thisLicenseRule, string agentType)
        {
            List<LicenseValue> licenseValuesList = new List<LicenseValue>(licenseValuesArray.Count);

            foreach (JToken licenseValueToken in licenseValuesArray)
            {
                try
                {
                    JArray licenseValueArray = (JArray)licenseValueToken;

                    if (licenseValueArray.Count == 2)
                    {
                        LicenseValue licenseValue = new LicenseValue();

                        licenseValue.Controller = thisLicenseRule.Controller;
                        licenseValue.AccountName = thisLicenseRule.AccountName;
                        licenseValue.AccountID = thisLicenseRule.AccountID;

                        licenseValue.RuleName = thisLicenseRule.RuleName;

                        licenseValue.AgentType = agentType;

                        licenseValue.LicenseEventTimeUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)licenseValueArray[0]);
                        licenseValue.LicenseEventTime = licenseValue.LicenseEventTimeUtc.ToLocalTime();

                        if (isTokenNull(licenseValueArray[1]) == false)
                        {
                            licenseValue.Value = (long)licenseValueArray[1];
                        }

                        licenseValuesList.Add(licenseValue);
                    }
                }
                catch { }
            }

            return licenseValuesList;
        }

    }
}
