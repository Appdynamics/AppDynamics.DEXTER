using AppDynamics.Dexter.ProcessingSteps;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
                loggerConsole.Info("AppDynamics DEXTER Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                Console.WriteLine();
                logger.Trace("Starting at local {0:o}/UTC {1:o}, Version={2}, Parameters={3}", DateTime.Now, DateTime.UtcNow, Assembly.GetEntryAssembly().GetName().Version, String.Join(" ", args));
                logger.Trace("Timezone {0} {1} {2}", TimeZoneInfo.Local.DisplayName, TimeZoneInfo.Local.StandardName, TimeZoneInfo.Local.BaseUtcOffset);
                logger.Trace("Culture {0}({1}), ShortDate={2}, ShortTime={3}, All={4}", CultureInfo.CurrentCulture.DisplayName, CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern, String.Join(";", CultureInfo.CurrentCulture.DateTimeFormat.GetAllDateTimePatterns()));
                logger.Trace("Framework {0}", RuntimeInformation.FrameworkDescription);
                logger.Trace("OS Architecture {0}", RuntimeInformation.OSArchitecture);
                logger.Trace("OS {0}", RuntimeInformation.OSDescription);
                logger.Trace("Process Architecture {0}", RuntimeInformation.ProcessArchitecture);
                logger.Trace("Number of Processors {0}", Environment.ProcessorCount);

                var parserResult = Parser.Default
                    .ParseArguments<ProgramOptions>(args)
                    .WithParsed((ProgramOptions programOptions) => { RunProgram(programOptions); })
                    .WithNotParsed((errs) =>
                    {
                        logger.Error("Could not parse command line arguments into ProgramOptions");
                        //loggerConsole.Error("Could not parse command line arguments into ProgramOptions");
                    });
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

        public static void RunProgram(ProgramOptions programOptions)
        {
            // Choose type of program to run, ETL or Compare

            if (programOptions.InputJobFilePath != null && programOptions.InputJobFilePath.Length > 0)
            {
                RunProgramETL(programOptions);
            }
            else if (programOptions.CompareFilePath != null && programOptions.CompareFilePath.Length > 0)
            {
                RunProgramCompare(programOptions);
            }
        }

        public static void RunProgramETL(ProgramOptions programOptions)
        {
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

            #region Check version and prompt someone 

            if (programOptions.SkipVersionCheck == false)
            {
                using (GithubApi githubApi = new GithubApi("https://api.github.com"))
                {
                    loggerConsole.Info("Version check against Github releases");

                    string listOfReleasesJSON = githubApi.GetReleases();
                    if (listOfReleasesJSON.Length > 0)
                    {
                        JArray releasesListArray = JArray.Parse(listOfReleasesJSON);
                        if (releasesListArray != null)
                        {
                            if (releasesListArray.Count > 0)
                            {
                                JObject releaseObject = (JObject)releasesListArray[0];

                                string latestReleaseVersionString = releaseObject["tag_name"].ToString();

                                if (latestReleaseVersionString != null && latestReleaseVersionString.Length > 0)
                                {
                                    try
                                    {
                                        Version latestReleaseVersion = new Version(latestReleaseVersionString);
                                        if (latestReleaseVersion > Assembly.GetEntryAssembly().GetName().Version)
                                        {
                                            // This version is older than what is listed on Github
                                            loggerConsole.Warn("Latest released version is {0}, and yours is {1}. Would you like to upgrade (y/n)?", latestReleaseVersion, Assembly.GetEntryAssembly().GetName().Version);

                                            ConsoleKeyInfo cki = Console.ReadKey();
                                            Console.WriteLine();
                                            if (cki.Key.ToString().ToLower() == "y")
                                            {
                                                loggerConsole.Info("Go to AppDynamics Extensions or to {0} to download new release", releaseObject["html_url"]);
                                                return;
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Error(ex);
                                        loggerConsole.Error(ex);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            #region Create output folder

            // If output folder isn't specified, get default output folder
            if (programOptions.OutputFolderPath == null || programOptions.OutputFolderPath.Length == 0)
            {
                // Windows: at the root of C: on Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
                {
                    programOptions.OutputFolderPath = @"C:\AppD.Dexter.Out";
                }
                // Mac/Linux: a child of %HOME% path
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == true || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
                {
                    programOptions.OutputFolderPath = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "AppD.Dexter.Out");
                }
            }

            programOptions.OutputFolderPath = Path.GetFullPath(programOptions.OutputFolderPath);

            logger.Info("Creating output folder {0}", programOptions.OutputFolderPath);
            loggerConsole.Info("Creating output folder {0}", programOptions.OutputFolderPath);

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
                logger.Info("UTC Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                loggerConsole.Info("UTC Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                logger.Info("Local Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime());
                loggerConsole.Info("Local Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime());

                // Validate Metrics selection
                if (jobConfiguration.Input.MetricsSelectionCriteria == null)
                {
                    jobConfiguration.Input.MetricsSelectionCriteria = new string[1];
                    jobConfiguration.Input.MetricsSelectionCriteria[0] = "TransactionApplication";
                }

                // Validate Events selection
                if (jobConfiguration.Input.EventsSelectionCriteria == null || jobConfiguration.Input.EventsSelectionCriteria.Length == 0)
                {
                    jobConfiguration.Input.EventsSelectionCriteria = new string[1];
                    jobConfiguration.Input.EventsSelectionCriteria[0] = "None";
                }

                // Validate Snapshot selection criteria
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

                // Validate Entity Dashboard Screenshot selection criteria
                if (jobConfiguration.Input.EntityDashboardSelectionCriteria == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria = new JobEntityDashboardSelectionCriteria();
                }

                if (jobConfiguration.Input.EntityDashboardSelectionCriteria.Tiers == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.Tiers = new string[0];
                }

                if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactions == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactions = new string[0];
                }

                if (jobConfiguration.Input.EntityDashboardSelectionCriteria.TierType == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.TierType = new JobTierType();
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.TierType.All = true;
                }

                if (jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeType == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeType = new JobTierType();
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeType.All = true;
                }

                if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionType == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionType = new JobBusinessTransactionType();
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionType.All = true;
                }

                if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendType == null)
                {
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendType = new JobBackendType();
                    jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendType.All = true;
                }

                // Validate Configuration Comparison selection
                if (jobConfiguration.Input.ConfigurationComparisonReferenceAPM == null ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller == null || jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller.Length == 0 ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application == null || jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application.Length == 0)
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM = new JobTarget();
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller = JobStepBase.BLANK_APPLICATION_CONTROLLER;
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Application = JobStepBase.BLANK_APPLICATION_APM;
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Type = JobStepBase.APPLICATION_TYPE_APM;
                }
                else
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller = jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Controller;
                    jobConfiguration.Input.ConfigurationComparisonReferenceAPM.Type = JobStepBase.APPLICATION_TYPE_APM;
                }

                if (jobConfiguration.Input.ConfigurationComparisonReferenceWEB == null ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller == null || jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller.Length == 0 ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Application == null || jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Application.Length == 0)
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB = new JobTarget();
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller = JobStepBase.BLANK_APPLICATION_CONTROLLER;
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Application = JobStepBase.BLANK_APPLICATION_WEB;
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Type = JobStepBase.APPLICATION_TYPE_WEB;
                }
                else
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller = jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Controller;
                    jobConfiguration.Input.ConfigurationComparisonReferenceWEB.Type = JobStepBase.APPLICATION_TYPE_WEB;
                }

                if (jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE == null ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller == null || jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller.Length == 0 ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Application == null || jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Application.Length == 0)
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE = new JobTarget();
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller = JobStepBase.BLANK_APPLICATION_CONTROLLER;
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Application = JobStepBase.BLANK_APPLICATION_MOBILE;
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Type = JobStepBase.APPLICATION_TYPE_MOBILE;
                }
                else
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller = jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Controller;
                    jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE.Type = JobStepBase.APPLICATION_TYPE_MOBILE;
                }

                if (jobConfiguration.Input.ConfigurationComparisonReferenceDB == null ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller == null || jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller.Length == 0 ||
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Application == null || jobConfiguration.Input.ConfigurationComparisonReferenceDB.Application.Length == 0)
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB = new JobTarget();
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller = JobStepBase.BLANK_APPLICATION_CONTROLLER;
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Application = JobStepBase.BLANK_APPLICATION_DB;
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Type = JobStepBase.APPLICATION_TYPE_DB;
                }
                else
                {
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller = jobConfiguration.Input.ConfigurationComparisonReferenceDB.Controller;
                    jobConfiguration.Input.ConfigurationComparisonReferenceDB.Type = JobStepBase.APPLICATION_TYPE_DB;
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

                    loggerConsole.Info("Target {0} {1}/{2} [{3}] as {4} with Regex={5}", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type, jobTarget.UserName, jobTarget.NameRegex);

                    jobTarget.ApplicationID = -1;

                    #region Validate target Controller properties against being empty

                    if (jobTarget.Controller == null || jobTarget.Controller == String.Empty)
                    {
                        logger.Warn("Target {0} property {1} is empty", i + 1, "Controller");
                        loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "Controller");

                        continue;
                    }
                    if (jobTarget.UserName == null || jobTarget.UserName == String.Empty)
                    {
                        logger.Warn("Target {0} property {1} is empty", i + 1, "UserName");
                        loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "UserName");

                        continue;
                    }
                    if (jobTarget.Application == null || jobTarget.Application == String.Empty)
                    {
                        logger.Warn("Target {0} property {1} is empty", i + 1, "Application");
                        loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "Application");

                        continue;
                    }

                    #endregion

                    #region Get credential or prompt for it

                    // Check for the @accountname to see if it is a valid account, unless it is using Token
                    if (jobTarget.UserName.ToUpper() != "BEARER")
                    {
                        if (jobTarget.UserName.Contains('@') == false)
                        {
                            logger.Warn("Target {0} property {1} does not supply account name (after @ sign).", i + 1, "UserName");
                            loggerConsole.Warn("Target {0} property {1} does not supply account name (after @ sign).", i + 1, "UserName");

                            string accountName = "customer1";
                            try
                            {
                                Uri controllerUri = new Uri(jobTarget.Controller);

                                if (controllerUri.Host.Contains("saas.appdynamics.com") == true)
                                {
                                    accountName = controllerUri.Host.Split('.')[0];
                                }
                            }
                            catch { }

                            loggerConsole.Warn("Your account name is most likely '{0}'", accountName);
                            loggerConsole.Warn("Your full username is most likely '{0}@{1}'", jobTarget.UserName, accountName);

                            loggerConsole.Warn("Would you like to try that instead of what you supplied (y/n)?");

                            ConsoleKeyInfo cki = Console.ReadKey();
                            Console.WriteLine();
                            if (cki.Key.ToString().ToLower() == "y")
                            {
                                jobTarget.UserName = String.Format("{0}@{1}", jobTarget.UserName, accountName);
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (jobTarget.UserPassword == null || jobTarget.UserPassword == String.Empty)
                    {
                        logger.Warn("Target {0} property {1} is empty", i + 1, "UserPassword");
                        loggerConsole.Warn("Target {0} property {1} is empty", i + 1, "UserPassword");

                        loggerConsole.Warn("Enter Password for user {0} for {1}:", jobTarget.UserName, jobTarget.Controller);

                        try
                        {
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
                        catch (InvalidOperationException ex)
                        {
                            logger.Error(ex);
                            loggerConsole.Warn("Unable to read password from console. Are you running Bash on Windows? If yes, please run in cmd or Powershell, or specify password in job file");
                            continue;
                        }
                    }

                    #endregion

                    using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                    {
                        #region Validate target Controller is accessible

                        // If reached here, we have all the properties to go query for list of Applications
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

                                #region Find APM applications

                                // Now we know we have access to Controller. Let's get Applications and expand them into multiple targets if there is a wildcard/regex
                                string applicationsAPMJSON = controllerApi.GetAPMApplications();
                                if (applicationsAPMJSON != String.Empty && applicationsAPMJSON.Length > 0)
                                {
                                    JArray applicationsInController = JArray.Parse(applicationsAPMJSON);

                                    // Filter to the regex or exact match
                                    IEnumerable<JToken> applicationsMatchingCriteria = null;
                                    if (jobTarget.NameRegex == true)
                                    {
                                        if (jobTarget.Application == "*")
                                        {
                                            jobTarget.Application = ".*";
                                        }
                                        Regex regexApplication = new Regex(jobTarget.Application, RegexOptions.IgnoreCase);
                                        applicationsMatchingCriteria = applicationsInController.Where(
                                            app => regexApplication.Match(JobStepBase.getStringValueFromJToken(app, "name")).Success == true);
                                    }
                                    else
                                    {
                                        applicationsMatchingCriteria = applicationsInController.Where(
                                            app => String.Compare(JobStepBase.getStringValueFromJToken(app, "name"), jobTarget.Application, true) == 0);
                                    }

                                    if (applicationsMatchingCriteria.Count() == 0)
                                    {
                                        logger.Warn("Target [{0}] Controller {1} does not have Application {2} [{3}]", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type);
                                        loggerConsole.Warn("Target [{0}] Controller {1} does not have Application {2} [{3}]", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type);

                                        continue;
                                    }

                                    foreach (JObject application in applicationsMatchingCriteria)
                                    {
                                        // Create a copy of target application for each individual application
                                        JobTarget jobTargetExpanded = new JobTarget();
                                        jobTargetExpanded.Controller = jobTarget.Controller;

                                        jobTargetExpanded.UserName = jobTarget.UserName;
                                        jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        jobTargetExpanded.Application = JobStepBase.getStringValueFromJToken(application, "name");
                                        jobTargetExpanded.ApplicationID = JobStepBase.getLongValueFromJToken(application, "id");
                                        jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_APM;

                                        expandedJobTargets.Add(jobTargetExpanded);

                                        logger.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                        loggerConsole.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_WEB:

                                #region Find EUM Web applications

                                // Now we know we have access to Controller. Let's get Applications and expand them into multiple targets if there is a wildcard/regex
                                controllerApi.PrivateApiLogin();

                                string applicationsAllTypesJSON = controllerApi.GetAllApplicationsAllTypes();
                                if (applicationsAllTypesJSON.Length > 0)
                                {
                                    JObject applicationsAll = JObject.Parse(applicationsAllTypesJSON);

                                    if (JobStepBase.isTokenPropertyNull(applicationsAll, "eumWebApplications") == false)
                                    {
                                        JArray applicationsInController = (JArray)applicationsAll["eumWebApplications"];

                                        IEnumerable<JToken> applicationsMatchingCriteria = null;
                                        if (jobTarget.NameRegex == true)
                                        {
                                            if (jobTarget.Application == "*")
                                            {
                                                jobTarget.Application = ".*";
                                            }
                                            Regex regexApplication = new Regex(jobTarget.Application, RegexOptions.IgnoreCase);
                                            applicationsMatchingCriteria = applicationsInController.Where(
                                                app => regexApplication.Match(JobStepBase.getStringValueFromJToken(app, "name")).Success == true);
                                        }
                                        else
                                        {
                                            applicationsMatchingCriteria = applicationsInController.Where(
                                                app => String.Compare(JobStepBase.getStringValueFromJToken(app, "name"), jobTarget.Application, true) == 0);
                                        }

                                        if (applicationsMatchingCriteria.Count() == 0)
                                        {
                                            logger.Warn("Target [{0}] Controller {1} does not have Application {2} [{3}]", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type);
                                            loggerConsole.Warn("Target [{0}] Controller {1} does not have Application {2} [{3}]", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type);

                                            continue;
                                        }

                                        foreach (JObject application in applicationsMatchingCriteria)
                                        {
                                            // Create a copy of target application for each individual application
                                            JobTarget jobTargetExpanded = new JobTarget();
                                            jobTargetExpanded.Controller = jobTarget.Controller;

                                            jobTargetExpanded.UserName = jobTarget.UserName;
                                            jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                            jobTargetExpanded.Application = JobStepBase.getStringValueFromJToken(application, "name");
                                            jobTargetExpanded.ApplicationID = JobStepBase.getLongValueFromJToken(application, "id");
                                            jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_WEB;

                                            expandedJobTargets.Add(jobTargetExpanded);

                                            logger.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                            loggerConsole.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                        }
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_MOBILE:

                                #region Find EUM Mobile applications

                                // Now we know we have access to Controller. Let's get Applications and expand them into multiple targets if there is a wildcard/regex
                                controllerApi.PrivateApiLogin();

                                string applicationsMobileJSON = controllerApi.GetMOBILEApplications();
                                if (applicationsMobileJSON.Length > 0)
                                {
                                    JArray applicationsMobile = JArray.Parse(applicationsMobileJSON);

                                    foreach (JObject applicationMobile in applicationsMobile)
                                    {
                                        JArray applicationsInTarget = (JArray)applicationMobile["children"];

                                        if (applicationsInTarget.Count > 0)
                                        {
                                            IEnumerable<JToken> applicationsMatchingCriteria = null;
                                            if (jobTarget.NameRegex == true)
                                            {
                                                if (jobTarget.Application == "*")
                                                {
                                                    jobTarget.Application = ".*";
                                                }
                                                Regex regexApplication = new Regex(jobTarget.Application, RegexOptions.IgnoreCase);
                                                applicationsMatchingCriteria = applicationsInTarget.Where(
                                                    app => regexApplication.Match(JobStepBase.getStringValueFromJToken(app, "name")).Success == true);
                                            }
                                            else
                                            {
                                                applicationsMatchingCriteria = applicationsInTarget.Where(
                                                    app => String.Compare(JobStepBase.getStringValueFromJToken(app, "name"), jobTarget.Application, true) == 0);
                                            }

                                            if (applicationsMatchingCriteria.Count() == 0)
                                            {
                                                logger.Warn("Target [{0}] Controller {1} does not have Application {2} [{3}]", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type);
                                                loggerConsole.Warn("Target [{0}] Controller {1} does not have Application {2} [{3}]", i + 1, jobTarget.Controller, jobTarget.Application, jobTarget.Type);

                                                continue;
                                            }

                                            foreach (JObject application in applicationsMatchingCriteria)
                                            {
                                                // Create a copy of target application for each individual application
                                                JobTarget jobTargetExpanded = new JobTarget();
                                                jobTargetExpanded.Controller = jobTarget.Controller;

                                                jobTargetExpanded.UserName = jobTarget.UserName;
                                                jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                                jobTargetExpanded.Application = JobStepBase.getStringValueFromJToken(application, "name");
                                                jobTargetExpanded.ApplicationID = JobStepBase.getLongValueFromJToken(application, "mobileAppId");
                                                jobTargetExpanded.ParentApplicationID = JobStepBase.getLongValueFromJToken(application, "applicationId");
                                                jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_MOBILE;

                                                expandedJobTargets.Add(jobTargetExpanded);

                                                logger.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                                loggerConsole.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                            }
                                        }
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_SIM:

                                #region Find SIM application

                                string applicationSIMJSON = controllerApi.GetSIMApplication();
                                if (applicationSIMJSON.Length > 0)
                                {
                                    JObject applicationSIMInController = JObject.Parse(applicationSIMJSON);

                                    if (JobStepBase.isTokenPropertyNull(applicationSIMInController, "id") == false &&
                                        JobStepBase.getStringValueFromJToken(applicationSIMInController, "name").Equals("Server & Infrastructure Monitoring", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        // Create a copy of target application for each individual application
                                        JobTarget jobTargetExpanded = new JobTarget();
                                        jobTargetExpanded.Controller = jobTarget.Controller;

                                        jobTargetExpanded.UserName = jobTarget.UserName;
                                        jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        jobTargetExpanded.Application = JobStepBase.getStringValueFromJToken(applicationSIMInController, "name");
                                        jobTargetExpanded.ApplicationID = JobStepBase.getLongValueFromJToken(applicationSIMInController, "id");
                                        jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_SIM;

                                        expandedJobTargets.Add(jobTargetExpanded);

                                        logger.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                        loggerConsole.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_DB:

                                #region Find Database application

                                // Now we know we have access to Controller. Let's get Database Collectors and expand them into multiple targets if there is a wildcard/regex
                                controllerApi.PrivateApiLogin();

                                applicationsAllTypesJSON = controllerApi.GetAllApplicationsAllTypes();
                                if (applicationsAllTypesJSON.Length > 0)
                                {
                                    JObject applicationsAll = JObject.Parse(applicationsAllTypesJSON);

                                    if (JobStepBase.isTokenNull(applicationsAll["dbMonApplication"]) == false &&
                                        JobStepBase.getStringValueFromJToken(applicationsAll["dbMonApplication"], "name").Equals("Database Monitoring", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        // Get database collectors
                                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);

                                        string collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls(fromTimeUnix, toTimeUnix, "4.5");
                                        if (collectorsJSON == String.Empty) collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls(fromTimeUnix, toTimeUnix, "4.4");

                                        if (collectorsJSON != String.Empty && collectorsJSON.Length > 0)
                                        {
                                            JObject collectorsContainer = JObject.Parse(collectorsJSON);
                                            if (JobStepBase.isTokenPropertyNull(collectorsContainer, "data") == false)
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
                                                        dbColl => regexApplication.Match(JobStepBase.getStringValueFromJToken(dbColl, "name")).Success == true);
                                                }
                                                else
                                                {
                                                    dbCollectorsMatchingCriteria = dbCollectorsInTarget.Where(
                                                        dbColl => String.Compare(JobStepBase.getStringValueFromJToken(dbColl, "name"), jobTarget.Application, true) == 0);
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
                                                    jobTargetExpanded.Controller = jobTarget.Controller;

                                                    jobTargetExpanded.UserName = jobTarget.UserName;
                                                    jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                                    jobTargetExpanded.Application = JobStepBase.getStringValueFromJToken(dbCollector, "name");
                                                    jobTargetExpanded.ApplicationID = JobStepBase.getLongValueFromJToken(applicationsAll["dbMonApplication"], "id");
                                                    jobTargetExpanded.DBCollectorID = JobStepBase.getLongValueFromJToken(dbCollector, "id");
                                                    jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_DB;

                                                    expandedJobTargets.Add(jobTargetExpanded);

                                                    logger.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                                    loggerConsole.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                                }
                                            }
                                        }
                                    }
                                }

                                #endregion

                                break;

                            case JobStepBase.APPLICATION_TYPE_BIQ:

                                #region Find Analytics application

                                // Now we know we have access to Controller. Let's get Database Collectors and expand them into multiple targets if there is a wildcard/regex
                                controllerApi.PrivateApiLogin();

                                applicationsAllTypesJSON = controllerApi.GetAllApplicationsAllTypes();
                                if (applicationsAllTypesJSON.Length > 0)
                                {
                                    JObject applicationsAll = JObject.Parse(applicationsAllTypesJSON);

                                    if (JobStepBase.isTokenNull(applicationsAll["analyticsApplication"]) == false &&
                                        JobStepBase.getStringValueFromJToken(applicationsAll["analyticsApplication"], "name").StartsWith("AppDynamics Analytics", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        JobTarget jobTargetExpanded = new JobTarget();
                                        jobTargetExpanded.Controller = jobTarget.Controller;

                                        jobTargetExpanded.UserName = jobTarget.UserName;
                                        jobTargetExpanded.UserPassword = AESEncryptionHelper.Encrypt(AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        jobTargetExpanded.Application = JobStepBase.getStringValueFromJToken(applicationsAll["analyticsApplication"], "name");
                                        jobTargetExpanded.ApplicationID = JobStepBase.getLongValueFromJToken(applicationsAll["analyticsApplication"], "id");
                                        jobTargetExpanded.Type = JobStepBase.APPLICATION_TYPE_BIQ;

                                        expandedJobTargets.Add(jobTargetExpanded);

                                        logger.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
                                        loggerConsole.Info("Target [{0}] {1}/{2} [{3}] => {4}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Type, jobTargetExpanded.Application);
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
                }

                // Final check for fat-fingered passwords or no internet access
                if (expandedJobTargets.Count() == 0)
                {
                    logger.Error("Job File Problem: Expanded targets but not a single valid target to work on");
                    loggerConsole.Error("Job File Problem: Expanded targets but not a single valid target to work on");
                    loggerConsole.Error("This is most likely caused by either incorrect password or incorrectly specified user name (see https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/Job-File#username-string).");
                    loggerConsole.Error("Only AppDynamics-internal authentication is supported in username@tenant format");
                    loggerConsole.Error("If you are using SAML or LDAP account, please change to AppDynamics-internal account");
                    loggerConsole.Error("If you have proxy, consult https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/Proxy-Settings to configure access");
                    loggerConsole.Error("Check Controller Log Files (https://github.com/Appdynamics/AppDynamics.DEXTER/wiki/Log-Files) for more detail");
                    loggerConsole.Warn("If you need support, please review https://github.com/Appdynamics/AppDynamics.DEXTER/wiki#getting-support and send the logs");

                    return;
                }

                // Sort them to be pretty
                expandedJobTargets = expandedJobTargets.OrderBy(o => o.Controller).ThenBy(o => o.Type).ThenBy(o => o.Application).ToList();

                // Save expanded targets
                jobConfiguration.Target = expandedJobTargets;

                #endregion

                // Add status to the overall job, setting it to the beginning
                jobConfiguration.Status = JobStatus.ExtractControllerVersionAndApplications;

                // Save the resulting JSON file to the job target folder
                if (FileIOHelper.WriteJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                    return;
                }
            }
            else
            {
                logger.Info("Resuming run from the job file {0}", programOptions.OutputJobFilePath);
                loggerConsole.Info("Resuming run from the job file {0}", programOptions.OutputJobFilePath);
            }

            #endregion

            #region Load and validate license

            string programLicensePath = Path.Combine(
                programOptions.ProgramLocationFolderPath,
                "LicensedFeatures.json");

            JObject licenseFile = FileIOHelper.LoadJObjectFromFile(programLicensePath);
            JObject licensedFeatures = (JObject)licenseFile["LicensedFeatures"];

            string dataSigned = licensedFeatures.ToString(Formatting.None);
            var bytesSigned = Encoding.UTF8.GetBytes(dataSigned);

            string dataSignature = licenseFile["Signature"].ToString();
            byte[] bytesSignature = Convert.FromBase64String(dataSignature);

            string licenseCertificatePath = Path.Combine(
                programOptions.ProgramLocationFolderPath,
                "AppDynamics.DEXTER.public.cer");

            X509Certificate2 publicCert = new X509Certificate2(licenseCertificatePath);

            var rsaPublicKey = publicCert.GetRSAPublicKey();

            bool licenseValidationResult = rsaPublicKey.VerifyData(bytesSigned, bytesSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            logger.Info("Validating license\n{0}\nwith signature {1}\nfrom {2} containing \n{3} returned {4}", dataSigned, dataSignature, licenseCertificatePath, publicCert, licenseValidationResult);

            JobOutput licensedReports = new JobOutput();
            licensedReports.ApplicationSummary = true;
            licensedReports.Configuration = true;
            licensedReports.Dashboards = true;
            licensedReports.DetectedEntities = true;
            licensedReports.EntityDashboards = true;
            licensedReports.EntityDetails = true;
            licensedReports.EntityMetricGraphs = true;
            licensedReports.EntityMetrics = true;
            licensedReports.Events = true;
            licensedReports.FlameGraphs = true;
            // Health check is not free
            licensedReports.HealthCheck = false;
            // Licenses are not free
            licensedReports.Licenses = false;
            licensedReports.Snapshots = true;
            licensedReports.UsersGroupsRolesPermissions = true;
            
            if (licenseValidationResult == true)
            {
                logger.Info("License validation signature check succeeded");
                loggerConsole.Info("License validation signature check succeeded");

                DateTime dateTimeLicenseExpiration = (DateTime)licensedFeatures["ExpirationDateTime"];
                if (dateTimeLicenseExpiration >= DateTime.Now)
                {
                    logger.Trace("License expires on {0:o}, valid", dateTimeLicenseExpiration);
                    loggerConsole.Info("License expires on {0:o}, valid", dateTimeLicenseExpiration);

                    licensedReports.ApplicationSummary = JobStepBase.getBoolValueFromJToken(licensedFeatures, "ApplicationSummary");
                    licensedReports.Configuration = JobStepBase.getBoolValueFromJToken(licensedFeatures, "Configuration");
                    licensedReports.Dashboards = JobStepBase.getBoolValueFromJToken(licensedFeatures, "Dashboards");
                    licensedReports.DetectedEntities = JobStepBase.getBoolValueFromJToken(licensedFeatures, "DetectedEntities");
                    licensedReports.EntityDashboards = JobStepBase.getBoolValueFromJToken(licensedFeatures, "EntityDashboards");
                    licensedReports.EntityDetails = JobStepBase.getBoolValueFromJToken(licensedFeatures, "EntityDetails");
                    licensedReports.EntityMetricGraphs = JobStepBase.getBoolValueFromJToken(licensedFeatures, "EntityMetricGraphs");
                    licensedReports.EntityMetrics = JobStepBase.getBoolValueFromJToken(licensedFeatures, "EntityMetrics");
                    licensedReports.Events = JobStepBase.getBoolValueFromJToken(licensedFeatures, "Events");
                    licensedReports.FlameGraphs = JobStepBase.getBoolValueFromJToken(licensedFeatures, "FlameGraphs");
                    licensedReports.HealthCheck = JobStepBase.getBoolValueFromJToken(licensedFeatures, "HealthCheck");
                    licensedReports.Licenses = JobStepBase.getBoolValueFromJToken(licensedFeatures, "Licenses");
                    licensedReports.Snapshots = JobStepBase.getBoolValueFromJToken(licensedFeatures, "Snapshots");
                    licensedReports.UsersGroupsRolesPermissions = JobStepBase.getBoolValueFromJToken(licensedFeatures, "UsersGroupsRolesPermissions");
                }
                else
                {
                    logger.Trace("License expires on {0:o}, expired", dateTimeLicenseExpiration);
                    loggerConsole.Info("License expires on {0:o}, expired", dateTimeLicenseExpiration);
                }
            }
            else
            {
                logger.Warn("License validation signature check failed");
                loggerConsole.Warn("License validation signature check failed");
            }

            programOptions.LicensedReports = licensedReports;

            #endregion

            logger.Trace("Executing:\r\n{0}", programOptions);
            loggerConsole.Trace("Executing:\r\n{0}", programOptions);

            // Go to work on the expanded and validated job file
            JobStepRouter.ExecuteJobThroughSteps(programOptions);
        }

        public static void RunProgramCompare(ProgramOptions programOptions)
        {
            logger.Trace("Executing:\r\n{0}", programOptions);
            loggerConsole.Trace("Executing:\r\n{0}", programOptions);

            loggerConsole.Warn("RunProgramCompare is not implemented yet");
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
