using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;

namespace AppDynamics.Dexter
{
    public class PrepareJob
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        internal static bool validateJobFileExists(ProgramOptions programOptions)
        {
            // Get job file
            programOptions.InputJobFilePath = Path.GetFullPath(programOptions.InputJobFilePath);

            // Validate that job file exists
            if (File.Exists(programOptions.InputJobFilePath) == false)
            {
                logger.Error("Job file {0} does not exist", programOptions.InputJobFilePath);
                loggerConsole.Error("Job file {0} does not exist", programOptions.InputJobFilePath);

                return false;
            }
            else
            {
                return true;
            }
        }

        internal static bool validateOrCreateOutputFolder(ProgramOptions programOptions)
        {
            try
            {
                // If output folder isn't specified, assume output folder to be a child of local folder
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

                // Expand the output folder to valid path
                programOptions.OutputFolderPath = Path.GetFullPath(programOptions.OutputFolderPath);
            }
            catch (Exception ex)
            {
                // The output folder was messed up
                logger.Error("Invalid output folder {0}", programOptions.OutputFolderPath);
                logger.Error(ex);
                loggerConsole.Error("Invalid output folder {0}", programOptions.OutputFolderPath);

                return false;
            }

            if (FileIOHelper.createFolder(programOptions.OutputFolderPath) == false)
            {
                logger.Error("Unable to create output folder={0}", programOptions.OutputFolderPath);
                loggerConsole.Error("Unable to create output folder={0}", programOptions.OutputFolderPath);

                return false;
            }

            return true;
        }

        internal static bool validateOrCreateJobOutputFolder(ProgramOptions programOptions)
        {
            // Clear out the job output folder if requested and exists
            if (programOptions.RestartJobFromBeginning)
            {
                if (FileIOHelper.deleteFolder(programOptions.OutputJobFolderPath) == false)
                {
                    logger.Error("Unable to clear job folder {0}", programOptions.OutputJobFolderPath);
                    loggerConsole.Error("Unable to clear job folder {0}", programOptions.OutputJobFolderPath);

                    return false;
                }

                // Sleep after deleting to let the file system catch up
                Thread.Sleep(1000);
            }

            // Create it if it doesn't exist
            return (FileIOHelper.createFolder(programOptions.OutputJobFolderPath));
        }

        internal static bool validateAndExpandJobFileContents(ProgramOptions programOptions)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Load job configuration
            JobConfiguration jobConfiguration = FileIOHelper.readJobConfigurationFromFile(programOptions.InputJobFilePath);
            if (jobConfiguration == null)
            {
                loggerConsole.Error("Unable to load job input file {0}", programOptions.InputJobFilePath);

                return false;
            }

            #region Validate Input 

            if (jobConfiguration.Input == null)
            {
                logger.Error("Job File Problem: Input can not be empty");
                loggerConsole.Error("Job File Problem: Input can not be empty");

                return false;
            }

            // Validate input time range selection
            if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.From == null || jobConfiguration.Input.TimeRange.From == DateTime.MinValue)
            {
                logger.Error("Job File Problem: Input.TimeRange.From can not be empty");
                loggerConsole.Error("Job File Problem: Input.TimeRange.From can not be empty");

                return false;
            }
            else if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.To == null || jobConfiguration.Input.TimeRange.To == DateTime.MinValue)
            {
                logger.Error("Job File Problem: Input.TimeRange.To can not be empty");
                loggerConsole.Error("Job File Problem: Input.TimeRange.To can not be empty");

                return false;
            }
            else if (jobConfiguration.Input.TimeRange.From > jobConfiguration.Input.TimeRange.To)
            {
                logger.Error("Job File Problem: Input.TimeRange.From='{0:u}' can not be > Input.TimeRange.To='{1:u}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                loggerConsole.Error("Job File Problem: Input.TimeRange.From='{0:u}' can not be > Input.TimeRange.To='{1:u}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                return false;
            }
            else if (jobConfiguration.Input.TimeRange.From.ToLocalTime() > DateTime.Now)
            {
                logger.Error("Job File Problem: Input.TimeRange.From='{0:u}' can not be in the future", jobConfiguration.Input.TimeRange.From);
                loggerConsole.Error("Job File Problem: Input.TimeRange.From='{0:u}' can not be in the future", jobConfiguration.Input.TimeRange.From);

                return false;
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

            if (jobConfiguration.Input.SnapshotSelectionCriteria.Tiers == null )
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

            #endregion

            #region Validate Output

            if (jobConfiguration.Output == null)
            {
                logger.Error("Job File Problem: Output can not be empty");
                loggerConsole.Error("Job File Problem: Output can not be empty");

                return false;
            }

            #endregion

            #region Expand time ranges into hourly chunks

            jobConfiguration.Input.TimeRange.From = jobConfiguration.Input.TimeRange.From.ToUniversalTime();
            jobConfiguration.Input.TimeRange.To = jobConfiguration.Input.TimeRange.To.ToUniversalTime();

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

                return false;
            }

            #endregion

            #region Expand list of targets

            // Process each target and validate the controller authentication, as well as create multiple per-application entries if there is a regex match
            List<JobTarget> expandedJobTargets = new List<JobTarget>(jobConfiguration.Target.Count);
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

                if (isTargetValid == false)
                {
                    jobTarget.Status = JobTargetStatus.InvalidConfiguration;

                    expandedJobTargets.Add(jobTarget);

                    continue;
                }

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

                        jobTarget.Status = JobTargetStatus.NoController;

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

                    jobTarget.Status = JobTargetStatus.NoController;

                    jobTarget.UserPassword = AESEncryptionHelper.Encrypt(jobTarget.UserPassword);
                    expandedJobTargets.Add(jobTarget);

                    continue;
                }

                #endregion

                #region Expand list of Applications using regex, if present

                // Now we know we have access to Controller. Let's get Applications and expand them into multiple targets if there is a wildcard/regex
                string applicationsJSON = controllerApi.GetListOfApplications();
                JArray applicationsInTarget = JArray.Parse(applicationsJSON);

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

                    jobTarget.Status = JobTargetStatus.NoApplication;

                    expandedJobTargets.Add(jobTarget);

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
                    jobTargetExpanded.ApplicationID = (int)application["id"];

                    // Add status to each individual application
                    jobTargetExpanded.Status = JobTargetStatus.ConfigurationValid;

                    expandedJobTargets.Add(jobTargetExpanded);

                    logger.Info("Target [{0}] Controller {1} Application {2}=>{3}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application);
                    loggerConsole.Info("Target [{0}] Controller {1} Application {2}=>{3}", i + 1, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application);
                }

                #endregion
            }

            // Final check for fat-fingered passwords or no internet access
            if (expandedJobTargets.Count(t => t.Status == JobTargetStatus.ConfigurationValid) == 0)
            {
                logger.Error("Job File Problem: Expanded targets but not a single valid target to work on");
                loggerConsole.Error("Job File Problem: Expanded targets but not a single valid target to work on");

                return false;
            }

            // Sort them to be pretty
            expandedJobTargets = expandedJobTargets.OrderBy(o => o.Controller).ThenBy(o => o.Application).ToList();

            // Save expanded targets
            jobConfiguration.Target = expandedJobTargets;

            #endregion

            // Add status to the overall job
            jobConfiguration.Status = JobStatus.ExtractControllerApplicationsAndEntities;

            // Save the resulting JSON file to the job target folder
            if (FileIOHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
            {
                loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                return false;
            }

            return true;
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
