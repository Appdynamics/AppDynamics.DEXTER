using AppDynamics.OfflineData.JobParameters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace AppDynamics.OfflineData
{
    public class PrepareJob
    {
        internal static bool validateJobFileExists(ProgramOptions programOptions)
        {
            // Get job file
            programOptions.InputJobFilePath = Path.GetFullPath(programOptions.InputJobFilePath);

            // Validate that job file exists
            if (File.Exists(programOptions.InputJobFilePath) == false)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROGRAM_PARAMETERS,
                    "PrepareJob.Main",
                    String.Format("Job file='{0}' does not exist", programOptions.InputJobFilePath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Job file {0} does not exist", programOptions.InputJobFilePath);
                Console.ResetColor();

                return false;
            }

            return true;
        }

        internal static bool validateOrCreateOutputFolder(ProgramOptions programOptions)
        {
            try
            {
                if (programOptions.OutputFolderPath == null || programOptions.OutputFolderPath.Length == 0)
                {
                    // Assume output folder to be a child of local folder
                    programOptions.OutputFolderPath = "out";
                }
                programOptions.OutputFolderPath = Path.GetFullPath(programOptions.OutputFolderPath);
            }
            catch (ArgumentException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_ARGUMENT,
                    "PrepareJob.validateOrCreateOutputFolder",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROGRAM_PARAMETERS,
                    "PrepareJob.validateOrCreateOutputFolder",
                    String.Format("Invalid output folder='{0}'", programOptions.OutputFolderPath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid output folder={0}", programOptions.OutputFolderPath);
                Console.ResetColor();

                return false;
            }

            if (createFolder(programOptions.OutputFolderPath) == false)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unable to create output folder={0}", programOptions.OutputFolderPath);
                Console.ResetColor();

                return false;
            }

            return true;
        }

        internal static bool validateOrCreateJobOutputFolder(ProgramOptions programOptions)
        {
            // Clear out the job output folder if requested and exists
            if (programOptions.RestartJobFromBeginning)
            {
                if (deleteFolder(programOptions.OutputJobFolderPath) == false)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to clear job folder={0} ", programOptions.OutputJobFolderPath);
                    Console.ResetColor();

                    return false;
                }

                // Sleep after deleting to let the file system catch up
                Thread.Sleep(1000);
            }

            // Create it if it doesn't exist
            return (createFolder(programOptions.OutputJobFolderPath));
        }

        internal static bool validateAndExpandJobFileContents(ProgramOptions programOptions)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // Load job configuration
            JobConfiguration jobConfiguration = JobConfigurationHelper.readJobConfigurationFromFile(programOptions.InputJobFilePath);
            if (jobConfiguration == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unable to load job input file={0}", programOptions.InputJobFilePath);
                Console.ResetColor();

                return false;
            }

            #region Validate Input 

            // Validate input time range selection

            if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.From == null || jobConfiguration.Input.TimeRange.From == DateTime.MinValue)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROPERTY_IN_JSON_JOB_FILE,
                    "PrepareJob.validateAndExpandJobFileContents",
                    String.Format("Input.TimeRange.From is empty in input file='{0}'", programOptions.InputJobFilePath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Input.TimeRange.From is empty", programOptions.InputJobFilePath);
                Console.ResetColor();

                return false;
            }
            else if (jobConfiguration.Input.TimeRange == null || jobConfiguration.Input.TimeRange.To == null || jobConfiguration.Input.TimeRange.To == DateTime.MinValue)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROPERTY_IN_JSON_JOB_FILE,
                    "PrepareJob.validateAndExpandJobFileContents",
                    String.Format("Input.TimeRange.To is empty in input file='{0}'", programOptions.InputJobFilePath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Input.TimeRange.To is empty", programOptions.InputJobFilePath);
                Console.ResetColor();

                return false;
            }
            else if (jobConfiguration.Input.TimeRange.From >= jobConfiguration.Input.TimeRange.To)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROPERTY_IN_JSON_JOB_FILE,
                    "PrepareJob.validateAndExpandJobFileContents",
                    String.Format("Input.TimeRange.From='{0:u}' can not be >= Input.TimeRange.To='{1:u}' in input file='{2}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To, programOptions.InputJobFilePath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Input.TimeRange.From='{0:u}' can not be >= Input.TimeRange.To='{1:u}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                Console.ResetColor();

                return false;
            }
            else if (jobConfiguration.Input.TimeRange.From > DateTime.Now)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROPERTY_IN_JSON_JOB_FILE,
                    "PrepareJob.validateAndExpandJobFileContents",
                    String.Format("Input.TimeRange.From='{0:u}' can not be in the future in input file='{1}'", jobConfiguration.Input.TimeRange.From, programOptions.InputJobFilePath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Input.TimeRange.From='{0:u}' can not be in the future", jobConfiguration.Input.TimeRange.From);
                Console.ResetColor();

                return false;
            }
            // Not sure about blocking this time, as long as it is > then from
            //else if (jobConfiguration.Input.TimeRange.To > DateTime.Now)
            //{
            //    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
            //        TraceEventType.Error,
            //        EventId.INVALID_PROPERTY_IN_JSON_JOB_FILE,
            //        "PrepareJob.validateAndExpandJobFileContents",
            //        String.Format("Input.TimeRange.To='{0:u}' can not be in the future in input file='{1}'", jobConfiguration.Input.TimeRange.To, programOptions.InputJobFilePath));

            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine("Input.TimeRange.To='{0:u}' can not be in the future", jobConfiguration.Input.TimeRange.To);
            //    Console.ResetColor();

            //    return false;
            //}

            #endregion

            #region Expand time ranges for metric retrieval

            // Change times to local time zone
            //jobConfiguration.Input.From = DateTime.SpecifyKind(jobConfiguration.Input.From, DateTimeKind.Local);
            //jobConfiguration.Input.To = DateTime.SpecifyKind(jobConfiguration.Input.To, DateTimeKind.Local);

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
            expandedTimeRange.To = new DateTime(
                expandedTimeRange.To.Year,
                expandedTimeRange.To.Month,
                expandedTimeRange.To.Day,
                expandedTimeRange.To.Hour,
                59,
                0,
                DateTimeKind.Utc);

            jobConfiguration.Input.ExpandedTimeRange = expandedTimeRange;

            // Prepare list of time ranges that goes from the Hour:00 of the From to the Hour:59 of the To
            jobConfiguration.Input.HourlyTimeRanges = new List<JobTimeRange>();

            DateTime intervalStartTime = expandedTimeRange.From;
            DateTime intervalEndTime = intervalStartTime.AddMinutes(59);
            while (intervalEndTime <= expandedTimeRange.To)
            {
                jobConfiguration.Input.HourlyTimeRanges.Add(new JobTimeRange { From = intervalStartTime, To = intervalEndTime });

                intervalStartTime = intervalStartTime.AddHours(1);
                intervalEndTime = intervalStartTime.AddMinutes(59);
            }

            #endregion

            #region Validate Output

            // Validate output object selection

            #endregion

            #region Validate list of targets

            // Validate list of targets
            if (jobConfiguration.Target == null || jobConfiguration.Target.Count == 0)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.NO_TARGETS_IN_JSON_JOB_FILE,
                    "PrepareJob.validateAndExpandJobFileContents",
                    String.Format("No targets to work on in job input file='{0}'", programOptions.InputJobFilePath));

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No targets to work on in job input file={0}", programOptions.InputJobFilePath);
                Console.ResetColor();

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
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.INVALID_TARGET_IN_JSON_JOB_FILE,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target item [{0}] does not contain necessary property='{1}'", i, "Controller"));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Target [{0}] property {1} is empty", i, "Controller");
                    Console.ResetColor();

                    isTargetValid = false;
                }
                if (jobTarget.UserName == null || jobTarget.UserName == string.Empty)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.INVALID_TARGET_IN_JSON_JOB_FILE,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target item [{0}] does not contain necessary property='{1}'", i, "UserName"));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Target [{0}] property {1} is empty", i, "UserName");
                    Console.ResetColor();

                    isTargetValid = false;
                }
                if (jobTarget.UserPassword == null || jobTarget.UserPassword == string.Empty)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.INVALID_TARGET_IN_JSON_JOB_FILE,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target item [{0}] does not contain necessary property='{1}'", i, "UserPassword"));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Target [{0}] property {1} is empty", i, "UserPassword");
                    Console.ResetColor();

                    isTargetValid = false;
                }
                if (jobTarget.Application == null || jobTarget.Application == string.Empty)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.INVALID_TARGET_IN_JSON_JOB_FILE,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target item [{0}] does not contain necessary property='{1}'", i, "Application"));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Target [{0}] property {1} is empty", i, "Application");
                    Console.ResetColor();

                    isTargetValid = false;
                }

                if (isTargetValid == false)
                {
                    jobTarget.Status = JobTargetStatus.InvalidConfiguration;

                    expandedJobTargets.Add(jobTarget);

                    continue;
                }

                #endregion

                #region Validate target Controller is accessible

                // If reached here, we have all the properties to go query for list of Applications
                ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, jobTarget.UserPassword);
                if (controllerApi.IsControllerAccessible() == false)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.CONTROLLER_NOT_ACCESSIBLE,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target [{0}/{1}] not accessible: '{2}'", i + 1, jobConfiguration.Target.Count, controllerApi));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Target [{0}/{1}] not accessible: {2}", i + 1, jobConfiguration.Target.Count, controllerApi);
                    Console.ResetColor();

                    jobTarget.Status = JobTargetStatus.NoController;

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
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Warning,
                        EventId.CONTROLLER_APPLICATION_DOES_NOT_EXIST,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target [{0}/{1}], controller '{2}' does not have '{3}'", i + 1, jobConfiguration.Target.Count, controllerApi, jobTarget.Application));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Target [{0}/{1}], controller {2} does not have {3}", i + 1, jobConfiguration.Target.Count, controllerApi, jobTarget.Application);
                    Console.ResetColor();

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
                    jobTargetExpanded.UserPassword = jobTarget.UserPassword;
                    jobTargetExpanded.Application = application["name"].ToString();
                    jobTargetExpanded.ApplicationID = (int)application["id"];

                    // Add status to each individual application

                    jobTargetExpanded.Status = JobTargetStatus.Extract;

                    expandedJobTargets.Add(jobTargetExpanded);

                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Verbose,
                        EventId.TARGET_APPLICATION_EXPANDED,
                        "PrepareJob.validateAndExpandJobFileContents",
                        String.Format("Target [{0}/{1}] with Controller='{2}' and Application='{3}' resulted in Application='{4}'", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application));

                    Console.WriteLine("Target [{0}/{1}], Controller={2}, Application={3}=>{4}", i + 1, jobConfiguration.Target.Count, jobTarget.Controller, jobTarget.Application, jobTargetExpanded.Application);
                }

                #endregion
            }

            // Save expanded targets
            jobConfiguration.Target = expandedJobTargets;

            #endregion

            #region Status for overall job

            // Add status to the overall job
            //jobConfiguration.Status = JobStepStatusConstants.OVERALL_STEP_1_EXTRACT_DATA;
            jobConfiguration.Status = JobStatus.Extract;

            #endregion

            // Save the resulting JSON file to the job target folder
            if (JobConfigurationHelper.writeJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unable to write job input file={0} ", programOptions.OutputJobFilePath);
                Console.ResetColor();

                return false;
            }

            return true;
        }

        internal static bool createFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Verbose,
                        EventId.FOLDER_CREATE,
                        "PrepareJob.createFolder",
                        String.Format("Creating folder='{0}'", folderPath));

                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (IOException ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_IO,
                    "PrepareJob.createFolder",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.FOLDER_CREATE_FAILED,
                    "PrepareJob.createFolder",
                    String.Format("Unable to create folder='{0}'", folderPath));

                return false;
            }
            return true;
        }

        internal static bool deleteFolder(string folderPath)
        {
            int tryNumber = 1;

            do
            {
                try
                {
                    if (Directory.Exists(folderPath))
                    {
                        LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                            TraceEventType.Verbose,
                            EventId.FOLDER_DELETE,
                            "PrepareJob.deleteFolder",
                            String.Format("Deleting folder='{0}', try#='{1}'", folderPath, tryNumber));

                        Directory.Delete(folderPath, true);
                    }
                    return true;
                }
                catch (IOException ex)
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Error,
                        EventId.EXCEPTION_IO,
                        "PrepareJob.deleteFolder",
                        ex);

                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Error,
                        EventId.FOLDER_DELETE_FAILED,
                        "PrepareJob.deleteFolder",
                        String.Format("Unable to delete folder='{0}'", folderPath));

                    if (ex.Message.StartsWith("The directory is not empty"))
                    {
                        tryNumber++;
                        Thread.Sleep(3000);
                    }
                    else
                    {
                        return false;
                    }
                }
            } while (tryNumber <= 3);

            return true;
        }
    }
}
