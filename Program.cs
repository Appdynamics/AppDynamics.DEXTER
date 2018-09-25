using AppDynamics.Dexter.ProcessingSteps;
using CommandLine;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace AppDynamics.Dexter
{
    public class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                logger.Trace("AppDynamics DEXTER Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                loggerConsole.Trace("AppDynamics DEXTER Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                logger.Trace("Starting at local {0:o}/UTC {1:o}, Version={2}, Parameters={3}", DateTime.Now, DateTime.UtcNow, Assembly.GetEntryAssembly().GetName().Version, String.Join(" ", args));
                logger.Trace("Timezone {0} {1} {2}", TimeZoneInfo.Local.DisplayName, TimeZoneInfo.Local.StandardName, TimeZoneInfo.Local.BaseUtcOffset);
                logger.Trace("Framework {0}", RuntimeInformation.FrameworkDescription);
                logger.Trace("OS Architecture {0}", RuntimeInformation.OSArchitecture);
                logger.Trace("OS {0}", RuntimeInformation.OSDescription);
                logger.Trace("Process Architecture {0}", RuntimeInformation.ProcessArchitecture);
                logger.Trace("Number of Processors {0}", Environment.ProcessorCount);

                #region Parse input parameters

                // Parse parameters
                ProgramOptions programOptions = new ProgramOptions();
                if (Parser.Default.ParseArguments(args, programOptions) == false)
                {
                    logger.Error("Could not parse command line arguments into ProgramOptions");
                    loggerConsole.Error("Could not parse command line arguments into ProgramOptions");

                    return;
                }

                #endregion

                #region Validate job file exists

                programOptions.InputJobFilePath = Path.GetFullPath(programOptions.InputJobFilePath);

                logger.Info("Checking input job file {0}", programOptions.InputJobFilePath);
                loggerConsole.Info("Checking input job file {0}", programOptions.InputJobFilePath);

                if (File.Exists(programOptions.InputJobFilePath) == false)
                {
                    logger.Error("Job file {0} does not exist", programOptions.InputJobFilePath);
                    loggerConsole.Error("Job file {0} does not exist", programOptions.InputJobFilePath);

                    return;
                }

                #endregion

                #region Create output folder

                // If output folder isn't specified, assume output folder to be:
                // Windows: at the root of C: on Windows
                // Mac/Linux: a child of %HOME% path
                if (programOptions.OutputFolderPath == null || programOptions.OutputFolderPath.Length == 0)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
                    {
                        programOptions.OutputFolderPath = @"C:\AppD.Dexter.Out";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == true || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
                    {
                        programOptions.OutputFolderPath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "AppD.Dexter.Out");
                    }
                }

                programOptions.OutputFolderPath = Path.GetFullPath(programOptions.OutputFolderPath);

                logger.Info("Creating output folder {0}", programOptions.OutputFolderPath);
                loggerConsole.Info("Checking output folder {0}", programOptions.OutputFolderPath);

                if (FileIOHelper.CreateFolder(programOptions.OutputFolderPath) == false)
                {
                    logger.Error("Unable to create output folder={0}", programOptions.OutputFolderPath);
                    loggerConsole.Error("Unable to create output folder={0}", programOptions.OutputFolderPath);

                    return;
                }

                #endregion

                #region Create job output folder

                // Set up the output job folder and job file path
                programOptions.JobName = Path.GetFileNameWithoutExtension(programOptions.InputJobFilePath);
                programOptions.OutputJobFolderPath = Path.Combine(programOptions.OutputFolderPath, programOptions.JobName);
                programOptions.OutputJobFilePath = Path.Combine(programOptions.OutputJobFolderPath, "jobparameters.json");
                programOptions.ProgramLocationFolderPath = AppDomain.CurrentDomain.BaseDirectory;

                // Create job folder if it doesn't exist
                // or
                // Clear out job folder if it already exists and restart of the job was requested
                if (programOptions.RestartJobFromBeginning)
                {
                    if (FileIOHelper.DeleteFolder(programOptions.OutputJobFolderPath) == false)
                    {
                        logger.Error("Unable to clear job folder {0}", programOptions.OutputJobFolderPath);
                        loggerConsole.Error("Unable to clear job folder {0}", programOptions.OutputJobFolderPath);

                        return;
                    }

                    // Sleep after deleting to let the file system catch up
                    Thread.Sleep(1000);
                }

                logger.Info("Creating job output folder {0}", programOptions.OutputJobFolderPath);
                loggerConsole.Info("Creating job output folder {0}", programOptions.OutputJobFolderPath);

                if (FileIOHelper.CreateFolder(programOptions.OutputJobFolderPath) == false)
                {
                    logger.Error("Unable to create job output folder={0}", programOptions.OutputJobFolderPath);
                    loggerConsole.Error("Unable to create job output folder={0}", programOptions.OutputJobFolderPath);

                    return;
                }

                #endregion

                #region Process input job file to output job file

                // Check if this job file was already already validated and exists in target folder
                loggerConsole.Info("Processing input job file to output job file");
                if (Directory.Exists(programOptions.OutputJobFolderPath) == false || File.Exists(programOptions.OutputJobFilePath) == false)
                {
                    // New job
                    // Validate job file for validity if the job is new
                    // Expand list of targets from the input file 
                    // Save validated job file to the output directory

                    // Load job configuration
                    JobConfiguration jobConfiguration = FileIOHelper.ReadJobConfigurationFromFile(programOptions.InputJobFilePath);
                    if (jobConfiguration == null)
                    {
                        loggerConsole.Error("Unable to load job input file {0}", programOptions.InputJobFilePath);

                        return;
                    }

                    #region Validate Input 

                    if (jobConfiguration.Input == null)
                    {
                        logger.Error("Job File Problem: Input can not be empty");
                        loggerConsole.Error("Job File Problem: Input can not be empty");

                        return;
                    }

                    // Validate input time range selection
                    if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.From == null || jobConfiguration.Input.TimeRange.From == DateTime.MinValue)
                    {
                        logger.Error("Job File Problem: Input.TimeRange.From can not be empty");
                        loggerConsole.Error("Job File Problem: Input.TimeRange.From can not be empty");

                        return;
                    }
                    else if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.To == null || jobConfiguration.Input.TimeRange.To == DateTime.MinValue)
                    {
                        logger.Error("Job File Problem: Input.TimeRange.To can not be empty");
                        loggerConsole.Error("Job File Problem: Input.TimeRange.To can not be empty");

                        return;
                    }

                    jobConfiguration.Input.TimeRange.From = jobConfiguration.Input.TimeRange.From.ToUniversalTime();
                    jobConfiguration.Input.TimeRange.To = jobConfiguration.Input.TimeRange.To.ToUniversalTime();

                    if (jobConfiguration.Input.TimeRange.From > jobConfiguration.Input.TimeRange.To)
                    {
                        logger.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be > Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                        loggerConsole.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be > Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                        return;
                    }
                    else if (jobConfiguration.Input.TimeRange.From > DateTime.UtcNow)
                    {
                        logger.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be in the future", jobConfiguration.Input.TimeRange.From);
                        loggerConsole.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be in the future", jobConfiguration.Input.TimeRange.From);

                        return;
                    }

                    // Validate Metrics selection
                    if (jobConfiguration.Input.MetricsSelectionCriteria == null)
                    {
                        jobConfiguration.Input.MetricsSelectionCriteria = new string[0];
                    }

                    // Validate Snapshot selection
                    if (jobConfiguration.Input.SnapshotSelectionCriteria == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria = new JobSnapshotSelectionCriteria();
                    }

                    if (jobConfiguration.Input.SnapshotSelectionCriteria.Tiers == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria.Tiers = new string[0];
                    }

                    if (jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactions == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactions = new string[0];
                    }

                    if (jobConfiguration.Input.SnapshotSelectionCriteria.TierType == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria.TierType = new JobTierType();
                        jobConfiguration.Input.SnapshotSelectionCriteria.TierType.All = true;
                    }

                    if (jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType = new JobBusinessTransactionType();
                        jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType.All = true;
                    }

                    if (jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience = new JobUserExperience();
                        jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Normal = true;
                        jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Slow = true;
                        jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.VerySlow = true;
                        jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Stall = true;
                        jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Error = true;
                    }

                    if (jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType == null)
                    {
                        jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType = new JobSnapshotType();
                        jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.Full = true;
                        jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.Partial = true;
                        jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.None = true;
                    }

                    // Validate Configuration Comparison selection
                    if (jobConfiguration.Input.ConfigurationComparisonReferenceCriteria == null)
                    {
                        jobConfiguration.Input.ConfigurationComparisonReferenceCriteria = new JobTarget();
                        jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller = JobStepBase.BLANK_APPLICATION_CONTROLLER;
                        jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Application = JobStepBase.BLANK_APPLICATION_APPLICATION;
                    }
                    else
                    {
                        jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller = jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller.TrimEnd('/');
                    }

                    #endregion

                    #region Validate Output

                    if (jobConfiguration.Output == null)
                    {
                        logger.Error("Job File Problem: Output can not be empty");
                        loggerConsole.Error("Job File Problem: Output can not be empty");

                        return;
                    }

                    #endregion

                    #region Expand time ranges into hourly chunks

                    // Prepare list of time ranges that goes from the Hour:00 of the From to the Hour:59 of the To
                    jobConfiguration.Input.HourlyTimeRanges = new List<JobTimeRange>();

                    DateTime intervalStartTime = jobConfiguration.Input.TimeRange.From;
                    DateTime intervalEndTime = new DateTime(
                        intervalStartTime.Year,
                        intervalStartTime.Month,
                        intervalStartTime.Day,
                        intervalStartTime.Hour,
                        0,
                        0,
                        DateTimeKind.Utc).AddHours(1);
                    do
                    {
                        TimeSpan timeSpan = intervalEndTime - jobConfiguration.Input.TimeRange.To;
                        if (timeSpan.TotalMinutes >= 0)
                        {
                            jobConfiguration.Input.HourlyTimeRanges.Add(new JobTimeRange { From = intervalStartTime, To = jobConfiguration.Input.TimeRange.To });
                            break;
                        }
                        else
                        {
                            jobConfiguration.Input.HourlyTimeRanges.Add(new JobTimeRange { From = intervalStartTime, To = intervalEndTime });
                        }

                        intervalStartTime = intervalEndTime;
                        intervalEndTime = intervalStartTime.AddHours(1);
                    }
                    while (true);

                    #endregion

                    #region Validate list of targets

                    // Validate list of targets
                    if (jobConfiguration.Target == null || jobConfiguration.Target.Count == 0)
                    {
                        logger.Error("Job File Problem: No targets to work on");
                        loggerConsole.Error("Job File Problem: No targets to work on");

                        return;
                    }

                    #endregion

                    #region Expand list of targets

                    List<JobTarget> expandedJobTargets = new List<JobTarget>(jobConfiguration.Target.Count);

                    // Process each target and validate the controller authentication, as well as create multiple per-application entries if there is a regex match
                    for (int i = 0; i < jobConfiguration.Target.Count; i++)
                    {
                        JobTarget jobTarget = jobConfiguration.Target[i];

                        jobTarget.ApplicationID = -1;

                        #region Validate target Controller properties against being empty

                        bool isTargetValid = true;
                        if (jobTarget.Controller == null || jobTarget.Controller == string.Empty)
                        {
                            logger.Warn("Target {0} property {1} is empty", i + 1, "Controller");
                            loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "Controller");

                            isTargetValid = false;
                        }
                        if (jobTarget.UserName == null || jobTarget.UserName == string.Empty)
                        {
                            logger.Warn("Target {0} property {1} is empty", i + 1, "UserName");
                            loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "UserName");

                            isTargetValid = false;
                        }
                        if (jobTarget.Application == null || jobTarget.Application == string.Empty)
                        {
                            logger.Warn("Target {0} property {1} is empty", i + 1, "Application");
                            loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "Application");

                            isTargetValid = false;
                        }

                        if (isTargetValid == false) continue;

                        #endregion

                        #region Get credential or prompt for it

                        if (jobTarget.UserPassword == null || jobTarget.UserPassword == string.Empty)
                        {
                            logger.Warn("Target {0} property {1} is empty", i + 1, "UserPassword");
                            loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "UserPassword");

                            loggerConsole.Warn("Enter Password for user {0} for {1}:", jobTarget.UserName, jobTarget.Controller);

                            String password = ReadPassword('*');
                            Console.WriteLine();
                            if (password.Length == 0)
                            {
                                logger.Warn("User specified empty password");
                                loggerConsole.Warn("Password can not be empty");

                                continue;
                            }
                            jobTarget.UserPassword = AESEncryptionHelper.Encrypt(password);
                        }

                        #endregion

                        #region Validate target Controller is accessible

                        // If reached here, we have all the properties to go query for list of Applications
                        //ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, jobTarget.UserPassword);
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                        if (controllerApi.IsControllerAccessible() == false)
                        {
                            logger.Warn("Target [{0}] Controller {1} not accessible", i + 1, controllerApi);
                            loggerConsole.Warn("Target [{0}] Controller {1} not accessible", i + 1, controllerApi);

                            continue;
                        }

                        #endregion

                        if (jobTarget.Type == null || jobTarget.Type == String.Empty)
                        {
                            jobTarget.Type = JobStepBase.APPLICATION_TYPE_APM;
                        }

                        switch (jobTarget.Type)
                        {
                            case JobStepBase.APPLICATION_TYPE_APM:

                                #region Expand list of APM Applications using regex, if present

                                // Now we know we have access to Controller. Let's get Applications and expand them into multiple targets if there is a wildcard/regex
                                string applicationsAPMJSON = controllerApi.GetApplicationsAPM();
                                if (applicationsAPMJSON != String.Empty && applicationsAPMJSON.Length > 0)
                                {
                                    JArray applicationsInTarget = JArray.Parse(applicationsAPMJSON);

                                    // Filter to the regex or exact match
                                    IEnumerable<JToken> applicationsMatchingCriteria = null;
                                    if (jobTarget.NameRegex == true)
                                    {
                                        if (jobTarget.Application == "*")
                                        {
                                            jobTarget.Application = ".*";
                                        }
                                        Regex regexApplication = new Regex(jobTarget.Application, RegexOptions.IgnoreCase);
                                        applicationsMatchingCriteria = applicationsInTarget.Where(
                                            app => regexApplication.Match(app["name"].ToString()).Success == true);
                                    }
                                    else
                                    {
                                        applicationsMatchingCriteria = applicationsInTarget.Where(
                                            app => String.Compare(app["name"].ToString(), jobTarget.Application, true) == 0);
                                    }

                                    if (applicationsMatchingCriteria.Count() == 0)
                                    {
                                        logger.Warn("Target [{0}] Controller {1} does not have Application {2}", i + 1, jobTarget.Controller, jobTarget.Application);
                                        loggerConsole.Warn("Target [{0}] Controller {1} does not have Application {2}", i + 1, jobTarget.Controller, jobTarget.Application);

                                        continue;
                                    }

                                    foreach (JObject application in applicationsMatchingCriteria)
                                    {
                                        // Create a copy of target application for each individual application
                                        JobTarget jobTargetExpanded = new JobTarget();
                                        jobTargetExpanded.Controller = jobTarget.Controller.TrimEnd('/');

                                        jobTargetExpanded.UserName = jobTarget.UserName;
                                        jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        jobTargetExpanded.Application = application["name"].ToString();
                                        jobTargetExpanded.ApplicationID = (long)application["id"];
                                        jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_APM;

                                        expandedJobTargets.Add(jobTargetExpanded);

                                        logger.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                        loggerConsole.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_WEB:

                                #region Expand list of EUM Applications using regex, if present

                                // Now we know we have access to Controller. Let's get Applications and expand them into multiple targets if there is a wildcard/regex
                                controllerApi.PrivateApiLogin();
                                string applicationsEUMJSON = controllerApi.GetApplicationsEUM();
                                if (applicationsEUMJSON.Length > 0)
                                {
                                    JArray applicationsInTarget = JArray.Parse(applicationsEUMJSON);

                                    IEnumerable<JToken> applicationsMatchingCriteria = null;
                                    if (jobTarget.NameRegex == true)
                                    {
                                        if (jobTarget.Application == "*")
                                        {
                                            jobTarget.Application = ".*";
                                        }
                                        Regex regexApplication = new Regex(jobTarget.Application, RegexOptions.IgnoreCase);
                                        applicationsMatchingCriteria = applicationsInTarget.Where(
                                            app => regexApplication.Match(app["name"].ToString()).Success == true);
                                    }
                                    else
                                    {
                                        applicationsMatchingCriteria = applicationsInTarget.Where(
                                            app => String.Compare(app["name"].ToString(), jobTarget.Application, true) == 0);
                                    }

                                    if (applicationsMatchingCriteria.Count() == 0)
                                    {
                                        logger.Warn("Target [{0}] Controller {1} does not have Application {2}", i + 1, jobTarget.Controller, jobTarget.Application);
                                        loggerConsole.Warn("Target [{0}] Controller {1} does not have Application {2}", i + 1, jobTarget.Controller, jobTarget.Application);

                                        continue;
                                    }

                                    foreach (JObject application in applicationsMatchingCriteria)
                                    {
                                        // Create a copy of target application for each individual application
                                        JobTarget jobTargetExpanded = new JobTarget();
                                        jobTargetExpanded.Controller = jobTarget.Controller.TrimEnd('/');

                                        jobTargetExpanded.UserName = jobTarget.UserName;
                                        jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        jobTargetExpanded.Application = application["name"].ToString();
                                        jobTargetExpanded.ApplicationID = (long)application["id"];
                                        jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_WEB;

                                        expandedJobTargets.Add(jobTargetExpanded);

                                        logger.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                        loggerConsole.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_MOBILE:

                                loggerConsole.Warn("Target [{0}] Application type '{1}' is not yet implemented", i + 1, JobStepBase.APPLICATION_TYPE_MOBILE);

                                break;

                            case JobStepBase.APPLICATION_TYPE_SIM:

                                #region Find SIM application

                                string applicationSIMJSON = controllerApi.GetSIMApplication();
                                if (applicationSIMJSON.Length > 0)
                                {
                                    JObject applicationSIMInTarget = JObject.Parse(applicationSIMJSON);

                                    if (applicationSIMInTarget["id"] != null && applicationSIMInTarget["name"].ToString() == "Server & Infrastructure Monitoring")
                                    {
                                        // Create a copy of target application for each individual application
                                        JobTarget jobTargetExpanded = new JobTarget();
                                        jobTargetExpanded.Controller = jobTarget.Controller.TrimEnd('/');

                                        jobTargetExpanded.UserName = jobTarget.UserName;
                                        jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        jobTargetExpanded.Application = applicationSIMInTarget["name"].ToString();
                                        jobTargetExpanded.ApplicationID = (long)applicationSIMInTarget["id"];
                                        jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_SIM;

                                        expandedJobTargets.Add(jobTargetExpanded);

                                        logger.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                        loggerConsole.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_DB:

                                #region Find Database application

                                // Now we know we have access to Controller. Let's get Database Collectors and expand them into multiple targets if there is a wildcard/regex
                                controllerApi.PrivateApiLogin();

                                string applicationsAllTypesJSON = controllerApi.GetAllApplicationsAllTypes();
                                if (applicationsAllTypesJSON.Length > 0)
                                {
                                    JObject applicationsAll = JObject.Parse(applicationsAllTypesJSON);

                                    if (applicationsAll["dbMonApplication"] != null && applicationsAll["dbMonApplication"]["name"].ToString() == "Database Monitoring")
                                    {
                                        // Get database collectors
                                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);

                                        string collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls45(fromTimeUnix, toTimeUnix);
                                        if (collectorsJSON == String.Empty) collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls44(fromTimeUnix, toTimeUnix);

                                        if (collectorsJSON != String.Empty && collectorsJSON.Length > 0)
                                        {
                                            JObject collectorsContainer = JObject.Parse(collectorsJSON);
                                            if (collectorsContainer != null && collectorsContainer["data"] != null)
                                            {
                                                JArray dbCollectorsInTarget = (JArray)collectorsContainer["data"];

                                                // Filter to the regex or exact match
                                                IEnumerable<JToken> dbCollectorsMatchingCriteria = null;
                                                if (jobTarget.NameRegex == true)
                                                {
                                                    if (jobTarget.Application == "*")
                                                    {
                                                        jobTarget.Application = ".*";
                                                    }
                                                    Regex regexApplication = new Regex(jobTarget.Application, RegexOptions.IgnoreCase);
                                                    dbCollectorsMatchingCriteria = dbCollectorsInTarget.Where(
                                                        dbColl => regexApplication.Match(dbColl["name"].ToString()).Success == true);
                                                }
                                                else
                                                {
                                                    dbCollectorsMatchingCriteria = dbCollectorsInTarget.Where(
                                                        dbColl => String.Compare(dbColl["name"].ToString(), jobTarget.Application, true) == 0);
                                                }

                                                if (dbCollectorsMatchingCriteria.Count() == 0)
                                                {
                                                    logger.Warn("Target [{0}] Controller {1} does not have DB Collector {2}", i + 1, jobTarget.Controller, jobTarget.Application);
                                                    loggerConsole.Warn("Target [{0}] Controller {1} does not have DB Collector {2}", i + 1, jobTarget.Controller, jobTarget.Application);

                                                    continue;
                                                }

                                                foreach (JObject dbCollector in dbCollectorsMatchingCriteria)
                                                {
                                                    // Create a copy of target application for each individual application
                                                    JobTarget jobTargetExpanded = new JobTarget();
                                                    jobTargetExpanded.Controller = jobTarget.Controller.TrimEnd('/');

                                                    jobTargetExpanded.UserName = jobTarget.UserName;
                                                    jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                                    jobTargetExpanded.Application = dbCollector["name"].ToString();
                                                    jobTargetExpanded.ApplicationID = (long)dbCollector["id"];
                                                    jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_DB;

                                                    expandedJobTargets.Add(jobTargetExpanded);

                                                    logger.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                                    loggerConsole.Info("Target [{0}] Controller {1} Application {2}=>{3} ({4})", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application, jobTargetExpanded.Type);
                                                }
                                            }
                                        }
                                    }
                                }

                                #endregion

                                break;

                            default:
                                logger.Warn("Target [{0}] Unknown application type '{1}'", i + 1, jobTarget.Type);
                                loggerConsole.Warn("Target [{0}] Unknown application type '{1}'", i + 1, jobTarget.Type);
                                break;
                        }
                    }

                    // Final check for fat-fingered passwords or no internet access
                    if (expandedJobTargets.Count() == 0)
                    {
                        logger.Error("Job File Problem: Expanded targets but not a single valid target to work on");
                        loggerConsole.Error("Job File Problem: Expanded targets but not a single valid target to work on");
                        loggerConsole.Error("This is most likely caused by either incorrect password or incorrectly specified user name.");
                        loggerConsole.Error("Only AppDynamics-internal authentication is supported in username@tenant format");
                        loggerConsole.Error("If you are using SAML or LDAP account, please change to AppDynamics-internal account");

                        return;
                    }

                    // Sort them to be pretty
                    expandedJobTargets = expandedJobTargets.OrderBy(o => o.Controller).ThenBy(o => o.Application).ToList();

                    // Save expanded targets
                    jobConfiguration.Target = expandedJobTargets;

                    #endregion

                    // Add status to the overall job
                    jobConfiguration.Status = JobStatus.ExtractControllerApplicationsAndEntities;

                    // Save the resulting JSON file to the job target folder
                    if (FileIOHelper.WriteJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                    {
                        loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                        return;
                    }
                }

                #endregion

                logger.Trace("Executing ProgramOptions:\r\n{0}", programOptions);
                loggerConsole.Trace("Executing ProgramOptions:\r\n{0}", programOptions);

                // Go to work on the expanded and validated job file
                JobStepRouter.ExecuteJobThroughSteps(programOptions);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);
            }
            finally
            {
                stopWatch.Stop();

                logger.Info("Application execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);
                loggerConsole.Trace("Application execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds);

                // Flush all the logs before shutting down
                LogManager.Flush();
            }
        }

        public static string ReadPassword(char mask)
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write(mask);
                }
            }

            return new string(pass.Reverse().ToArray());
        }
    }
}
