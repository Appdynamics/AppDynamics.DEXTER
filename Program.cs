using AppDynamics.Dexter.ProcessingSteps;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using System.Xml;

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
            logger.Trace("Executing:\r\n{0}", programOptions);
            loggerConsole.Trace("Executing:\r\n{0}", programOptions);

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

            // Choose type of program to run, ETL or Compare
            if (programOptions.InputETLJobFilePath != null && programOptions.InputETLJobFilePath.Length > 0)
            {
                loggerConsole.Info("Running ETL workload");

                RunProgramETL(programOptions);
            }
            else if (programOptions.RequestIDs != null && programOptions.ReportFolderPath.Length > 0)
            {
                loggerConsole.Info("Running Individual Snapshots workload");

                RunProgramIndividualSnapshots(programOptions);
            }
            else if (programOptions.InputCompareJobFilePath != null && programOptions.InputCompareJobFilePath.Length > 0)
            {
                loggerConsole.Info("Running Compare workload");

                RunProgramCompare(programOptions);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Compiler", "CS0168", Justification = "Hiding ArgumentOutOfRangeException and FormatException that may occur when dates are parsed")]
        public static void RunProgramETL(ProgramOptions programOptions)
        {
            #region Validate job file exists and load it

            programOptions.InputETLJobFilePath = Path.GetFullPath(programOptions.InputETLJobFilePath);

            logger.Info("Checking input ETL job file {0}", programOptions.InputETLJobFilePath);
            loggerConsole.Info("Checking input ETL job file {0}", programOptions.InputETLJobFilePath);

            if (File.Exists(programOptions.InputETLJobFilePath) == false)
            {
                logger.Error("ETL job file {0} does not exist", programOptions.InputETLJobFilePath);
                loggerConsole.Error("ETL job file {0} does not exist", programOptions.InputETLJobFilePath);

                return;
            }

            // Load job configuration
            JobConfiguration jobConfiguration = FileIOHelper.ReadJobConfigurationFromFile(programOptions.InputETLJobFilePath);
            if (jobConfiguration == null)
            {
                logger.Error("Unable to load job input file {0}", programOptions.InputETLJobFilePath);
                loggerConsole.Error("Unable to load job input file {0}", programOptions.InputETLJobFilePath);

                return;
            }

            if (jobConfiguration.Input == null)
            {
                logger.Error("Job File Problem: Input can not be empty");
                loggerConsole.Error("Job File Problem: Input can not be empty");

                return;
            }

            if (jobConfiguration.Output == null)
            {
                logger.Error("Job File Problem: Output can not be empty");
                loggerConsole.Error("Job File Problem: Output can not be empty");

                return;
            }

            if (jobConfiguration.Target == null || jobConfiguration.Target.Count == 0)
            {
                logger.Error("Job File Problem: No targets to work on");
                loggerConsole.Error("Job File Problem: No targets to work on");

                return;
            }

            #endregion

            #region Validate Input - Time Range

            DateTime dateTimeNowOriginal = DateTime.Now;
            DateTime dateTimeNowOriginalUtc = dateTimeNowOriginal.ToUniversalTime();

            // Validate input time range selection
            if (jobConfiguration.Input.TimeFrame != null)
            {
                #region Validate new style TimeFrame

                // Using Timespan https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings?view=netcore-3.1#the-constant-c-format-specifier
                //"TimeFrame": {
                //    "MarkDate": "2020-03-20",
                //    "MarkTime": "10:00:00",
                //    "Duration": "1:00:00"
                //}

                // Or using ISO 8601 time interval https://en.wikipedia.org/wiki/ISO_8601#Time_intervals
                // Going Forward in time from mark:
                //"TimeFrame": {
                //    "MarkDate": "2020-03-20",
                //    "MarkTime": "10:00:00",
                //    "Duration": "PT1H"
                //}
                // Going Backward in time from mark:
                //"TimeFrame": {
                //    "MarkDate": "2020-03-20",
                //    "MarkTime": "10:00:00",
                //    "Duration": "-PT1H"
                //}

                // The newer type of relative time range specifier in newer job files

                if (jobConfiguration.Input.TimeFrame.MarkDate == null || jobConfiguration.Input.TimeFrame.MarkDate.Length == 0)
                {
                    logger.Error("Job File Problem: Input.TimeFrame.MarkDate can not be empty");
                    loggerConsole.Error("Job File Problem: Input.TimeFrame.MarkDate can not be empty");

                    return;
                }
                if (jobConfiguration.Input.TimeFrame.MarkTime == null || jobConfiguration.Input.TimeFrame.MarkTime.Length == 0)
                {
                    logger.Error("Job File Problem: Input.TimeFrame.MarkTime can not be empty");
                    loggerConsole.Error("Job File Problem: Input.TimeFrame.MarkTime can not be empty");

                    return;
                }
                if (jobConfiguration.Input.TimeFrame.Duration == null || jobConfiguration.Input.TimeFrame.Duration.Length == 0)
                {
                    logger.Error("Job File Problem: Input.TimeFrame.Duration can not be empty");
                    loggerConsole.Error("Job File Problem: Input.TimeFrame.Duration can not be empty");

                    return;
                }

                // Parse out date of the point from which we're counting
                DateTime markDate = DateTime.MinValue;
                if (DateTime.TryParse(jobConfiguration.Input.TimeFrame.MarkDate, null, DateTimeStyles.AssumeLocal, out markDate) == true)
                {
                    //markDate = markDate.ToUniversalTime();
                }
                else
                {
                    logger.Warn("Job File Problem: Input.TimeFrame.MarkDate={0} is not a valid Date", jobConfiguration.Input.TimeFrame.MarkDate);

                    DateTimeKind kind = dateTimeNowOriginal.Kind;
                    string token = jobConfiguration.Input.TimeFrame.MarkDate.ToUpper();
                    if (jobConfiguration.Input.TimeFrame.MarkDate.ToUpper().EndsWith("_Z") == true)
                    {
                        kind = DateTimeKind.Utc;
                        token = token.Substring(0, token.Length - 2);
                    }

                    // Let's try one of the tokens
                    switch (token)
                    {
                        #region Day offsets

                        case "TODAY":
                            markDate = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            break;

                        case "YESTERDAY":
                            markDate = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind).AddDays(-1);

                            break;

                        case "DAY_BEFORE_YESTERDAY":
                            markDate = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind).AddDays(-2);

                            break;

                        case "SAME_DAY_LAST_WEEK":
                            markDate = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind).AddDays(-7);

                            break;

                        #endregion

                        #region Weekdays

                        case "MONDAY":
                            DateTime dateTime_Token_Monday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Monday.DayOfWeek != DayOfWeek.Monday)
                            {
                                dateTime_Token_Monday = dateTime_Token_Monday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Monday;

                            break;

                        case "TUESDAY":
                            DateTime dateTime_Token_Tuesday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Tuesday.DayOfWeek != DayOfWeek.Tuesday)
                            {
                                dateTime_Token_Tuesday = dateTime_Token_Tuesday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Tuesday;

                            break;

                        case "WEDNESDAY":
                            DateTime dateTime_Token_Wednesday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Wednesday.DayOfWeek != DayOfWeek.Wednesday)
                            {
                                dateTime_Token_Wednesday = dateTime_Token_Wednesday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Wednesday;

                            break;

                        case "THURSDAY":
                            DateTime dateTime_Token_Thursday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Thursday.DayOfWeek != DayOfWeek.Thursday)
                            {
                                dateTime_Token_Thursday = dateTime_Token_Thursday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Thursday;

                            break;

                        case "FRIDAY":
                            DateTime dateTime_Token_Friday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Friday.DayOfWeek != DayOfWeek.Friday)
                            {
                                dateTime_Token_Friday = dateTime_Token_Friday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Friday;

                            break;

                        case "SATURDAY":
                            DateTime dateTime_Token_Saturday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Saturday.DayOfWeek != DayOfWeek.Saturday)
                            {
                                dateTime_Token_Saturday = dateTime_Token_Saturday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Saturday;

                            break;

                        case "SUNDAY":
                            DateTime dateTime_Token_Sunday = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                0, 0, 0,
                                kind);

                            while (dateTime_Token_Sunday.DayOfWeek != DayOfWeek.Sunday)
                            {
                                dateTime_Token_Sunday = dateTime_Token_Sunday.AddDays(-1);
                            }
                            markDate = dateTime_Token_Sunday;

                            break;

                        #endregion

                        default:
                            // Day of Month?
                            if (token.StartsWith("DAY_OF_MONTH") == true)
                            {
                                string tokenWithoutZ = token.Replace("_Z", "");
                                string dayOfMonthToken = tokenWithoutZ.Substring("DAY_OF_MONTH_".Length);
                                int dayOfMonth = -1;
                                if (Int32.TryParse(dayOfMonthToken, out dayOfMonth) == false)
                                {
                                    logger.Error("Job File Problem: Input.TimeFrame.MarkDate={0} is not a valid day of month token", jobConfiguration.Input.TimeFrame.MarkDate);
                                }
                                else
                                {
                                    if (dayOfMonth < 1 || dayOfMonth > 31)
                                    {
                                        logger.Error("Job File Problem: Input.TimeFrame.MarkDate={0} is not a valid day of month token", jobConfiguration.Input.TimeFrame.MarkDate);
                                    }
                                    else
                                    {
                                        // Got a day of the month in range of 1..31
                                        // The months are all different in size. Let's find that day. If we're in the month with 30 days and we are on day 31, we'll go to 30
                                        DateTime dateTime_Token_Today = new DateTime(
                                            dateTimeNowOriginal.Year,
                                            dateTimeNowOriginal.Month,
                                            dateTimeNowOriginal.Day,
                                            0, 0, 0,
                                            kind);

                                        DateTime dateTime_Token_ThisDay_ThisMonth = DateTime.MinValue;
                                        bool parsedDayThisMonth = false;
                                        while (parsedDayThisMonth == false)
                                        {
                                            try
                                            {
                                                dateTime_Token_ThisDay_ThisMonth = new DateTime(
                                                    dateTimeNowOriginal.Year,
                                                    dateTimeNowOriginal.Month,
                                                    dayOfMonth,
                                                    0, 0, 0,
                                                    kind);

                                                parsedDayThisMonth = true;
                                            }
                                            catch (ArgumentOutOfRangeException ex)
                                            {
                                                // Date out of range
                                                dayOfMonth--;
                                            }
                                        }
                                        if (dateTime_Token_ThisDay_ThisMonth > dateTime_Token_Today)
                                        {
                                            // This day is in the future. Must go back
                                            markDate = dateTime_Token_ThisDay_ThisMonth.AddMonths(-1);
                                        }
                                        else
                                        {
                                            // This day of month is today or in the past. Take it
                                            markDate = dateTime_Token_ThisDay_ThisMonth;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                logger.Error("Job File Problem: Input.TimeFrame.MarkDate={0} is not a valid token", jobConfiguration.Input.TimeFrame.MarkDate);
                            }

                            break;
                    }
                }

                if (markDate == DateTime.MinValue)
                {
                    loggerConsole.Error("Job File Problem: Input.TimeFrame.MarkDate={0} is not a valid Date or recognized token", jobConfiguration.Input.TimeFrame.MarkDate);
                    return;
                }

                // Parse out time of the point from which we're counting
                DateTime markTime = DateTime.MinValue;
                if (DateTime.TryParse(jobConfiguration.Input.TimeFrame.MarkTime, null, DateTimeStyles.AssumeLocal, out markTime) == true)
                {
                    //markTime = markTime.ToUniversalTime();
                }
                else
                {
                    logger.Warn("Job File Problem: Input.TimeFrame.MarkTime={0} is not a valid Time", jobConfiguration.Input.TimeFrame.MarkTime);

                    // Let's try one of the tokens
                    switch (jobConfiguration.Input.TimeFrame.MarkTime.ToUpper())
                    {
                        case "NOW":
                            // Round the time out to the beginning of the minute
                            markTime = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                dateTimeNowOriginal.Hour,
                                dateTimeNowOriginal.Minute,
                                0,
                                dateTimeNowOriginal.Kind);

                            break;

                        case "NOW_Z":
                            // Round the time out to the beginning of the minute
                            markTime = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                dateTimeNowOriginal.ToUniversalTime().Hour,
                                dateTimeNowOriginal.ToUniversalTime().Minute,
                                0,
                                DateTimeKind.Utc);

                            break;

                        case "CURRENT_HOUR":
                            // Round the time out to the beginning of hour
                            markTime = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                dateTimeNowOriginal.Hour,
                                0,
                                0,
                                dateTimeNowOriginal.Kind);

                            break;

                        case "CURRENT_HOUR_Z":
                            // Round the time out to the beginning of hour
                            markTime = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                dateTimeNowOriginal.ToUniversalTime().Minute,
                                0,
                                0,
                                DateTimeKind.Utc);

                            break;

                        case "PREVIOUS_HOUR":
                            // Round the time out to the beginning of past 
                            markTime = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                dateTimeNowOriginal.Hour,
                                0,
                                0,
                                dateTimeNowOriginal.Kind).AddHours(-1);

                            break;

                        case "PREVIOUS_HOUR_Z":
                            // Round the time out to the beginning of past 
                            markTime = new DateTime(
                                dateTimeNowOriginal.Year,
                                dateTimeNowOriginal.Month,
                                dateTimeNowOriginal.Day,
                                dateTimeNowOriginal.ToUniversalTime().Hour,
                                0,
                                0,
                                DateTimeKind.Utc).AddHours(-1);

                            break;

                        default:
                            logger.Warn("Job File Problem: Input.TimeFrame.MarkTime={0} is not a valid token", jobConfiguration.Input.TimeFrame.MarkTime);

                            break;
                    }
                }

                if (markTime == DateTime.MinValue)
                {
                    loggerConsole.Error("Job File Problem: Input.TimeFrame.MarkTime={0} is not a valid Time or recognized token", jobConfiguration.Input.TimeFrame.MarkTime);
                    return;
                }

                // Parse out duration, first the .NET 
                TimeSpan duration = new TimeSpan(0, 0, 0);
                if (TimeSpan.TryParse(jobConfiguration.Input.TimeFrame.Duration, out duration) == false)
                {
                    // Not a Timespan
                    logger.Warn("Job File Problem: Input.TimeFrame.Duration={0} is not a valid TimeSpan", jobConfiguration.Input.TimeFrame.Duration);
                    loggerConsole.Warn("Job File Problem: Input.TimeFrame.Duration={0} is not a valid TimeSpan", jobConfiguration.Input.TimeFrame.Duration);

                    // Try the ISO 8601 time interval
                    try
                    {
                        duration = XmlConvert.ToTimeSpan(jobConfiguration.Input.TimeFrame.Duration);
                    }
                    catch (FormatException ex)
                    {
                        logger.Error("Job File Problem: Input.TimeFrame.Duration={0} is not a valid ISO 8601 Time interval", jobConfiguration.Input.TimeFrame.Duration);
                        loggerConsole.Error("Job File Problem: Input.TimeFrame.Duration={0} is not a valid ISO 8601 Time interval", jobConfiguration.Input.TimeFrame.Duration);

                        return;
                    }
                }
                if (Math.Abs(duration.TotalSeconds) < 60)
                {
                    logger.Error("Job File Problem: Input.TimeFrame.Duration={0} is shorter than 60 seconds", jobConfiguration.Input.TimeFrame.Duration);
                    loggerConsole.Error("Job File Problem: Input.TimeFrame.Duration={0} is is shorter than 60 seconds", jobConfiguration.Input.TimeFrame.Duration);

                    return;
                }

                // If we got here, we successfully parsed the date, time and duration (possibly negative)
                logger.Info("Parsed MarkDate '{0}' as '{1:o}', local '{2:o}'", jobConfiguration.Input.TimeFrame.MarkDate, markDate.ToUniversalTime(), markDate.ToLocalTime());
                logger.Info("Parsed MarkTime '{0}' as '{1:o}', local '{2:o}'", jobConfiguration.Input.TimeFrame.MarkTime, markTime.ToUniversalTime(), markTime.ToLocalTime());
                logger.Info("Parsed Duration '{0}' as '{1:c}'", jobConfiguration.Input.TimeFrame.Duration, duration);
                loggerConsole.Info("Parsed MarkDate '{0}' as '{1:o}', local '{2:o}'", jobConfiguration.Input.TimeFrame.MarkDate, markDate.ToUniversalTime(), markDate.ToLocalTime());
                loggerConsole.Info("Parsed MarkTime '{0}' as '{1:o}', local '{2:o}'", jobConfiguration.Input.TimeFrame.MarkTime, markTime.ToUniversalTime(), markTime.ToLocalTime());
                loggerConsole.Info("Parsed Duration '{0}' as '{1:c}'", jobConfiguration.Input.TimeFrame.Duration, duration);

                DateTime dateTimeMark = DateTime.MinValue;
                if ((markDate.Kind == DateTimeKind.Local && markTime.Kind == DateTimeKind.Local) || (markDate.Kind == DateTimeKind.Utc && markTime.Kind == DateTimeKind.Utc))
                {
                    dateTimeMark = new DateTime(
                        markDate.Year,
                        markDate.Month,
                        markDate.Day,
                        markTime.Hour,
                        markTime.Minute,
                        markTime.Second,
                        markDate.Kind);
                }
                else if (markDate.Kind == DateTimeKind.Local && markTime.Kind == DateTimeKind.Utc)
                {
                    dateTimeMark = new DateTime(
                        markDate.Year,
                        markDate.Month,
                        markDate.Day,
                        markTime.ToLocalTime().Hour,
                        markTime.ToLocalTime().Minute,
                        markTime.ToLocalTime().Second,
                        markDate.Kind);
                }
                else if (markDate.Kind == DateTimeKind.Utc && markTime.Kind == DateTimeKind.Local)
                {
                    dateTimeMark = new DateTime(
                        markDate.Year,
                        markDate.Month,
                        markDate.Day,
                        markTime.ToUniversalTime().Hour,
                        markTime.ToUniversalTime().Minute,
                        markTime.ToUniversalTime().Second,
                        markDate.Kind);
                }

                DateTime dateTimeMarkPlusDuration = dateTimeMark.Add(duration);

                logger.Info("DateTimeMark '{0:o}', local '{1:o}'", dateTimeMark.ToUniversalTime(), dateTimeMark.ToLocalTime());
                logger.Info("DateTimeMarkPlusDuration '{0:o}', local '{1:o}'", dateTimeMarkPlusDuration.ToUniversalTime(), dateTimeMarkPlusDuration.ToLocalTime());
                loggerConsole.Info("DateTimeMark '{0:o}', local '{1:o}'", dateTimeMark.ToUniversalTime(), dateTimeMark.ToLocalTime());
                loggerConsole.Info("DateTimeMarkPlusDuration '{0:o}', local '{1:o}'", dateTimeMarkPlusDuration.ToUniversalTime(), dateTimeMarkPlusDuration.ToLocalTime());

                jobConfiguration.Input.TimeRange = new JobTimeRange();
                if (dateTimeMark <= dateTimeMarkPlusDuration)
                {
                    jobConfiguration.Input.TimeRange.From = dateTimeMark;
                    jobConfiguration.Input.TimeRange.To = dateTimeMarkPlusDuration;
                }
                else
                {
                    jobConfiguration.Input.TimeRange.From = dateTimeMarkPlusDuration;
                    jobConfiguration.Input.TimeRange.To = dateTimeMark;
                }

                #endregion
            }
            else if (jobConfiguration.Input.TimeRange != null)
            {
                #region Validate old style TimeRange

                //"TimeRange": {
                //  "From": "2020-04-22T23:00:00",
                //  "To": "2020-04-23T00:00:00"
                //}
                // Or
                //"TimeRange": {
                //  "From": "2020-04-22T23:00:00Z",
                //  "To": "2020-04-23T00:00:00Z"
                //}

                // The older type of explicit time range specifier in older job files

                if (jobConfiguration.Input.TimeRange.From == null || jobConfiguration.Input.TimeRange.From == DateTime.MinValue)
                {
                    logger.Error("Job File Problem: Input.TimeRange.From can not be empty");
                    loggerConsole.Error("Job File Problem: Input.TimeRange.From can not be empty");

                    return;
                }
                else if (jobConfiguration.Input.TimeRange.To == null || jobConfiguration.Input.TimeRange.To == DateTime.MinValue)
                {
                    logger.Error("Job File Problem: Input.TimeRange.To can not be empty");
                    loggerConsole.Error("Job File Problem: Input.TimeRange.To can not be empty");

                    return;
                }

                #endregion
            }

            else
            {
                // Either TimeFrame or TimeRange must be specified. TimeRange is older style for explicit saying, TimeFrame is for the newer one
                logger.Error("Job File Problem: Input.TimeRange and Input.TimeFrame can not both be empty");
                loggerConsole.Error("Job File Problem: Input.TimeRange and Input.TimeFrame can not both be empty");

                return;
            }

            // Switch all times in TimeRange to UTC
            jobConfiguration.Input.TimeRange.From = jobConfiguration.Input.TimeRange.From.ToUniversalTime();
            jobConfiguration.Input.TimeRange.To = jobConfiguration.Input.TimeRange.To.ToUniversalTime();

            // Now measure the durations, make sure that From is before To
            if (jobConfiguration.Input.TimeRange.From > jobConfiguration.Input.TimeRange.To)
            {
                logger.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be > Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
                loggerConsole.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be > Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);

                return;
            }

            // Make sure we are not past the Now for the From
            if (jobConfiguration.Input.TimeRange.From > dateTimeNowOriginalUtc)
            {
                logger.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be in the future", jobConfiguration.Input.TimeRange.From);
                loggerConsole.Error("Job File Problem: Input.TimeRange.From='{0:o}' can not be in the future", jobConfiguration.Input.TimeRange.From);

                return;
            }

            // Make sure we are not past the Now for the To, if yes, set it to Now
            if (jobConfiguration.Input.TimeRange.To > dateTimeNowOriginalUtc)
            {
                logger.Warn("Job File Problem: Input.TimeRange.To='{0:o}' can not be in the future", jobConfiguration.Input.TimeRange.To);
                loggerConsole.Warn("Job File Problem: Input.TimeRange.To='{0:o}' can not be in the future, setting to Now", jobConfiguration.Input.TimeRange.To);

                jobConfiguration.Input.TimeRange.To = dateTimeNowOriginalUtc;
            }

            logger.Info("UTC Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
            loggerConsole.Info("UTC Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
            logger.Info("Local Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime());
            loggerConsole.Info("Local Input.TimeRange.From='{0:o}' to Input.TimeRange.To='{1:o}'", jobConfiguration.Input.TimeRange.From.ToLocalTime(), jobConfiguration.Input.TimeRange.To.ToLocalTime());

            #endregion

            #region Validate Input Everything else

            // Validate Metrics selection criteria
            if (jobConfiguration.Input.MetricsSelectionCriteria == null)
            {
                jobConfiguration.Input.MetricsSelectionCriteria = new JobMetricSelectionCriteria();
                jobConfiguration.Input.MetricsSelectionCriteria.MetricSets = new string[0];
            }

            // Validate Events selection criteria
            if (jobConfiguration.Input.EventsSelectionCriteria == null)
            {
                jobConfiguration.Input.EventsSelectionCriteria = new string[0];
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

            if (jobConfiguration.Input.SnapshotSelectionCriteria.TierTypes == null)
            {
                jobConfiguration.Input.SnapshotSelectionCriteria.TierTypes = new string[1];
                jobConfiguration.Input.SnapshotSelectionCriteria.TierTypes[0] = "All";
            }

            if (jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionTypes == null)
            {
                jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionTypes = new string[1];
                jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionTypes[0] = "All";
            }

            if (jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs == null)
            {
                jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs = new string[0];
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

            if (jobConfiguration.Input.EntityDashboardSelectionCriteria.TierTypes == null)
            {
                jobConfiguration.Input.EntityDashboardSelectionCriteria.TierTypes = new string[1];
                jobConfiguration.Input.EntityDashboardSelectionCriteria.TierTypes[0] = "All";
            }

            if (jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeTypes == null)
            {
                jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeTypes = new string[1];
                jobConfiguration.Input.EntityDashboardSelectionCriteria.NodeTypes[0] = "All";
            }

            if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionTypes == null)
            {
                jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionTypes = new string[1];
                jobConfiguration.Input.EntityDashboardSelectionCriteria.BusinessTransactionTypes[0] = "All";
            }

            if (jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendTypes == null)
            {
                jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendTypes = new string[1];
                jobConfiguration.Input.EntityDashboardSelectionCriteria.BackendTypes[0] = "All";
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

            #region Get job file name

            // It is either passed as parameter
            if (programOptions.JobName != null && programOptions.JobName.Length > 0)
            {
                // Using jobName passed in the parameter
            }
            else
            {
                // Job name is derived from the file name, start date and number of hourly chunks in that range
                programOptions.JobName = String.Format("{0}.{1:yyyyMMddHHmm}.{2}",
                Path.GetFileNameWithoutExtension(programOptions.InputETLJobFilePath),
                jobConfiguration.Input.TimeRange.From,
                jobConfiguration.Input.HourlyTimeRanges.Count);
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
            //programOptions.JobName = Path.GetFileNameWithoutExtension(programOptions.InputETLJobFilePath);
            programOptions.OutputJobFolderPath = Path.Combine(programOptions.OutputFolderPath, programOptions.JobName);
            programOptions.OutputJobFilePath = Path.Combine(programOptions.OutputJobFolderPath, "jobparameters.json");
            programOptions.ProgramLocationFolderPath = AppDomain.CurrentDomain.BaseDirectory;

            // Create job folder if it doesn't exist
            // or
            // Clear out job folder if it already exists and restart of the job was requested
            if (programOptions.DeletePreviousJobOutput)
            {
                if (FileIOHelper.DeleteFolder(programOptions.OutputJobFolderPath) == false)
                {
                    logger.Error("Unable to clear job folder {0}", programOptions.OutputJobFolderPath);
                    loggerConsole.Error("Unable to clear job folder {0}", programOptions.OutputJobFolderPath);

                    return;
                }

                // Sleep after deleting to let the file system catch up
                Thread.Sleep(2000);
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
                // Expand list of targets from the input file 
                // Save validated job file to the output directory

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

                                        if (applicationsInTarget != null && applicationsInTarget.Count > 0)
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

                // Also save a copy of the original file name
                string copyOfJobFileInOriginalName = Path.Combine(
                    programOptions.OutputJobFolderPath,
                    Path.GetFileName(programOptions.InputETLJobFilePath));
                // Remove the timeframe for later replay
                jobConfiguration.Input.TimeFrame = null;
                if (FileIOHelper.WriteJobConfigurationToFile(jobConfiguration, copyOfJobFileInOriginalName) == false)
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

            if (LoadAndValidateLicense(programOptions) == false) return;

            logger.Trace("Executing:\r\n{0}", programOptions);
            loggerConsole.Trace("Executing:\r\n{0}", programOptions);

            // Go to work on the expanded and validated job file
            JobStepRouter.ExecuteJobThroughSteps(programOptions);
        }

        public static void RunProgramIndividualSnapshots(ProgramOptions programOptions)
        {
            #region Validate report folder exits

            programOptions.ReportFolderPath = Path.GetFullPath(programOptions.ReportFolderPath);

            if (Directory.Exists(programOptions.ReportFolderPath) == false)
            {
                logger.Error("Report folder {0} does not exist", programOptions.ReportFolderPath);
                loggerConsole.Error("Report folder {0} does not exist", programOptions.ReportFolderPath);

                return;
            }

            if (File.Exists(Path.Combine(programOptions.ReportFolderPath, "SNAP", "snapshots.csv")) == false)
            {
                logger.Error("Report folder {0} does not contain Snapshot data", programOptions.ReportFolderPath);
                loggerConsole.Error("Report folder {0} does not contain Snapshot data", programOptions.ReportFolderPath);

                return;
            }

            #endregion

            #region Read existing or create new Job File 

            // Parse the request IDs into the right list
            string[] requestIDsTokens = programOptions.RequestIDs.Split(',');
            for (int i = 0; i < requestIDsTokens.Length; i++)
            {
                requestIDsTokens[i] = requestIDsTokens[i].Trim();
            }
            logger.Info("Parsed {0} to {1} items {2}", programOptions.RequestIDs, requestIDsTokens.Length, String.Join(",", requestIDsTokens));
            loggerConsole.Info("Parsed {0} to {1} items {2}", programOptions.RequestIDs, requestIDsTokens.Length, String.Join(",", requestIDsTokens));

            // Reverse engineer it from the path 
            // 
            // D:\AppD.Dexter.Out\Demo\demodev.all.202005081500.2\Report
            // ^^^^^^^^^^^^^^^^^^^^^^^^                                     Output folder path
            //                         ^^^^^^^^^^^^^^^^^^^^^^^^^^           Job name
            char pathSeparatorToken = '\\';
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true)
            {
                pathSeparatorToken = '\\';
            }
            // Mac/Linux: a child of %HOME% path
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == true || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) == true)
            {
                pathSeparatorToken = '/';
            }

            string[] pathTokens = programOptions.ReportFolderPath.Split(pathSeparatorToken);
            string[] pathTokensMinus1Folder = pathTokens.Take(pathTokens.Length - 1).ToArray();
            string[] pathTokensMinus2Folder = pathTokens.Take(pathTokens.Length - 2).ToArray();

            if (pathTokensMinus1Folder.Length == 0 || pathTokensMinus2Folder.Length == 0)
            { 
                loggerConsole.Error("{0} needs to be at least 2 layers deep in the folder hierachy. Yes this is silly but it is how it works here", programOptions.ReportFolderPath);
                return;
            }

            string reportFolderName = pathTokens[pathTokens.Length - 1];

            programOptions.OutputFolderPath = Path.Combine(pathTokensMinus2Folder);
            programOptions.JobName = pathTokens[pathTokens.Length - 2];
            programOptions.OutputJobFolderPath = Path.Combine(programOptions.OutputFolderPath, programOptions.JobName);
            programOptions.OutputJobFilePath = Path.Combine(programOptions.OutputJobFolderPath, "jobparameters.json");
            programOptions.ProgramLocationFolderPath = AppDomain.CurrentDomain.BaseDirectory;

            // Resume or create new job file
            if (File.Exists(programOptions.OutputJobFilePath) == true)
            {
                // Let's update it with the right parameters and fast forward the job to the right 

                // Read job file from the location
                JobConfiguration jobConfiguration = FileIOHelper.ReadJobConfigurationFromFile(programOptions.OutputJobFilePath);
                if (jobConfiguration == null)
                {
                    loggerConsole.Error("Unable to load job input file {0}", programOptions.InputETLJobFilePath);

                    return;
                }
                
                // Specify request IDs that we want
                jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs = requestIDsTokens;

                // Set up the report to output
                jobConfiguration.Output.IndividualSnapshots = true;

                // Fast forward the status
                jobConfiguration.Status = JobStatus.ReportAPMIndividualSnapshots;

                // Save the resulting JSON file to the job target folder
                if (FileIOHelper.WriteJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                    return;
                }
            }
            else
            {
                // We don't have the old jobparameters.json, so let's make a minimum one

                string defaultJobFilePath = Path.Combine(
                    programOptions.ProgramLocationFolderPath,
                    "DefaultJob.json");
                JobConfiguration jobConfiguration = FileIOHelper.ReadJobConfigurationFromFile(defaultJobFilePath);
                if (jobConfiguration == null)
                {
                    loggerConsole.Error("Unable to load job input file {0}", programOptions.InputETLJobFilePath);

                    return;
                }

                // These have to exist for application not to complain
                jobConfiguration.Input.TimeRange = new JobTimeRange();
                jobConfiguration.Input.HourlyTimeRanges = new List<JobTimeRange>();

                // Specify request IDs that we want
                jobConfiguration.Input.SnapshotSelectionCriteria.RequestIDs = requestIDsTokens;

                // Set up the report to output
                jobConfiguration.Output = new JobOutput();
                jobConfiguration.Output.IndividualSnapshots = true;

                // Fast forward the status
                jobConfiguration.Status = JobStatus.ReportAPMIndividualSnapshots;

                // Save the resulting JSON file to the job target folder
                if (FileIOHelper.WriteJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                    return;
                }
            }

            // Finally, if the folder is not Report, check 

            if (String.Compare(reportFolderName, "Report", true) != 0)
            {
                programOptions.IndividualSnapshotsNonDefaultReportFolderName = reportFolderName;
            }

            #endregion

            if (LoadAndValidateLicense(programOptions) == false) return;

            logger.Trace("Executing:\r\n{0}", programOptions);
            loggerConsole.Trace("Executing:\r\n{0}", programOptions);

            // Go to work on the expanded and validated job file
            JobStepRouter.ExecuteJobThroughSteps(programOptions);
        }

        public static void RunProgramCompare(ProgramOptions programOptions)
        {
            #region Validate job file exists

            programOptions.InputCompareJobFilePath = Path.GetFullPath(programOptions.InputCompareJobFilePath);

            logger.Info("Checking input Compare job file {0}", programOptions.InputCompareJobFilePath);
            loggerConsole.Info("Checking input Compare job file {0}", programOptions.InputCompareJobFilePath);

            if (File.Exists(programOptions.InputCompareJobFilePath) == false)
            {
                logger.Error("Compare job file {0} does not exist", programOptions.InputCompareJobFilePath);
                loggerConsole.Error("Compare job file {0} does not exist", programOptions.InputCompareJobFilePath);

                return;
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

            #region Create compare output folder

            // Set up the output job folder and job file path
            programOptions.JobName = Path.GetFileNameWithoutExtension(programOptions.InputCompareJobFilePath);
            programOptions.OutputJobFolderPath = Path.Combine(programOptions.OutputFolderPath, programOptions.JobName);
            programOptions.OutputJobFilePath = Path.Combine(programOptions.OutputJobFolderPath, "compareparameters.json");
            programOptions.ProgramLocationFolderPath = AppDomain.CurrentDomain.BaseDirectory;

            // Create job folder if it doesn't exist
            // or
            // Clear out job folder if it already exists and restart of the job was requested
            if (programOptions.DeletePreviousJobOutput)
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

            logger.Trace("Executing:\r\n{0}", programOptions);
            loggerConsole.Trace("Executing:\r\n{0}", programOptions);

            loggerConsole.Warn("RunProgramCompare is not implemented yet");

            // Go to work on the expanded and validated compare file
        }

        public static bool LoadAndValidateLicense(ProgramOptions programOptions)
        {
            string programLicensePath = Path.Combine(
                programOptions.ProgramLocationFolderPath,
                "LicensedFeatures.json");

            JObject licenseFile = FileIOHelper.LoadJObjectFromFile(programLicensePath);
            JObject licensedFeatures = (JObject)licenseFile["LicensedFeatures"];

            string dataSigned = licensedFeatures.ToString(Newtonsoft.Json.Formatting.None);
            var bytesSigned = Encoding.UTF8.GetBytes(dataSigned);

            string dataSignature = licenseFile["Signature"].ToString();
            byte[] bytesSignature = Convert.FromBase64String(dataSignature);

            string licenseCertificatePath = Path.Combine(
                programOptions.ProgramLocationFolderPath,
                "AppDynamics.DEXTER.public.cer");

            X509Certificate2 publicCert = new X509Certificate2(licenseCertificatePath);

            var rsaPublicKey = publicCert.GetRSAPublicKey();

            bool licenseValidationResult = rsaPublicKey.VerifyData(bytesSigned, bytesSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            logger.Info(
@"Validating license
{0}
with signature {1}
from {2} containing
{3} returned {4}",
                dataSigned, dataSignature, licenseCertificatePath, publicCert, licenseValidationResult);

            JobOutput licensedReports = new JobOutput();
            licensedReports.ApplicationSummary = true;
            licensedReports.Configuration = true;
            licensedReports.Dashboards = true;
            licensedReports.DetectedEntities = true;
            licensedReports.EntityDashboards = true;
            licensedReports.EntityDetails = true;
            licensedReports.EntityMetricGraphs = true;
            licensedReports.EntityMetrics = true;
            licensedReports.MetricsList = true;
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
                    licensedReports.MetricsList = JobStepBase.getBoolValueFromJToken(licensedFeatures, "MetricsList");
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

                    return false;
                }
            }
            else
            {
                logger.Warn("License validation signature check failed");
                loggerConsole.Warn("License validation signature check failed");

                return false;
            }

            programOptions.LicensedReports = licensedReports;

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
