using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;

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
                    programOptions.OutputFolderPath = "out";
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

            // Validate input time range selection
            if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.From == null || jobConfiguration.Input.TimeRange.From == DateTime.MinValue)
            {
                logger.Error("Input.TimeRange.From can not be empty");
                loggerConsole.Error("Input.TimeRange.From can not be empty");

                return false;
            }
            else if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.To == null || jobConfiguration.Input.TimeRange.To == DateTime.MinValue)
            {
                logger.Error("Input.TimeRange.To can not be empty");
                loggerConsole.Error("Input.TimeRange.To can not be empty");

                return false;
            }
            else if (jobConfiguration.Input.TimeRange.From >= jobConfiguration.Input.TimeRange.To)
            {
                logger.Error("Input.TimeRange.From='{0:u}' can not be >= Input.TimeRange.To='{1:u}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                loggerConsole.Error("Input.TimeRange.From='{0:u}' can not be >= Input.TimeRange.To='{1:u}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                return false;
            }
            else if (jobConfiguration.Input.TimeRange.From > DateTime.Now)
            {
                logger.Error("Input.TimeRange.From='{0:u}' can not be in the future", jobConfiguration.Input.TimeRange.From);
                loggerConsole.Error("Input.TimeRange.From='{0:u}' can not be in the future", jobConfiguration.Input.TimeRange.From);

                return false;
            }

            #endregion

            #region Expand time ranges for metric retrieval

            // Expand the time ranges to the hour beginning and end
            JobTimeRange expandedTimeRange = new JobTimeRange();
            expandedTimeRange.From = jobConfiguration.Input.TimeRange.From.ToUniversalTime();
            expandedTimeRange.From = new DateTime(
                expandedTimeRange.From.Year,
                expandedTimeRange.From.Month,
                expandedTimeRange.From.Day,
                expandedTimeRange.From.Hour,
                0,
                0,
                DateTimeKind.Utc);

            expandedTimeRange.To = jobConfiguration.Input.TimeRange.To.ToUniversalTime();
            if (expandedTimeRange.To.Minute > 0 || expandedTimeRange.To.Second > 0)
            {
                expandedTimeRange.To = new DateTime(
                    expandedTimeRange.To.Year,
                    expandedTimeRange.To.Month,
                    expandedTimeRange.To.Day,
                    expandedTimeRange.To.Hour,
                    0,
                    0,
                    DateTimeKind.Utc).AddHours(1);
            }

            jobConfiguration.Input.ExpandedTimeRange = expandedTimeRange;

            // Prepare list of time ranges that goes from the Hour:00 of the From to the Hour:59 of the To
            jobConfiguration.Input.HourlyTimeRanges = new List<JobTimeRange>();

            DateTime intervalStartTime = expandedTimeRange.From;
            //DateTime intervalEndTime = intervalStartTime.AddMinutes(59);
            DateTime intervalEndTime = intervalStartTime.AddHours(1);
            while (intervalEndTime <= expandedTimeRange.To)
            {
                jobConfiguration.Input.HourlyTimeRanges.Add(new JobTimeRange { From = intervalStartTime, To = intervalEndTime });

                intervalStartTime = intervalStartTime.AddHours(1);
                //intervalEndTime = intervalStartTime.AddMinutes(59);
                intervalEndTime = intervalStartTime.AddHours(1);
            }

            #endregion

            #region Validate list of targets

            // Validate list of targets
            if (jobConfiguration.Target == null || jobConfiguration.Target.Count == 0)
            {
                logger.Error("No targets to work on in job input file {0}", programOptions.InputJobFilePath);
                loggerConsole.Error("No targets to work on in job input file {0}", programOptions.InputJobFilePath);

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

                    loggerConsole.Info("Enter Password for user {0}:", jobTarget.UserName);

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
