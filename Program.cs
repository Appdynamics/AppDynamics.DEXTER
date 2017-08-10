using CommandLine;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AppDynamics.OfflineData
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {

                #region Set up main application logging

                DateTime dateTimeNow = DateTime.Now; // UtcNow;
                string logFolderName = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                if (PrepareJob.createFolder(logFolderName) == false) return;
                string logFileName = String.Format("{0}-{1}.log", LogHelper.OFFLINE_DATA_TRACE_SOURCE, dateTimeNow.ToString("yyyyMMddHHmm"));
                string logFilePath = Path.Combine(logFolderName, logFileName);

                // Create the trace
                if (LogHelper.Instance.TraceSources.ContainsKey(LogHelper.OFFLINE_DATA_TRACE_SOURCE) == false)
                {
                    TraceSource ts = new TraceSource(LogHelper.OFFLINE_DATA_TRACE_SOURCE);
                    TextWriterTraceListener twtl = new TextWriterTraceListener(logFilePath);
                    twtl.Name = logFileName;
                    //twtl.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId;
                    ts.Listeners.Add(twtl);
                    LogHelper.Instance.TraceSources.TryAdd(LogHelper.OFFLINE_DATA_TRACE_SOURCE, ts);
                }

                #endregion

                #region Parse input parameters

                LogHelper.Instance.EnsureCorrelationId();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Start,
                    EventId.APPLICATION_STARTED,
                    "Program.Main",
                    String.Format("Starting. Version='{0}', Parameters='{1}'", Assembly.GetEntryAssembly().GetName().Version, String.Join(" ", args)));

                Console.WriteLine("Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                Console.WriteLine("Log file='{0}'", logFilePath);

                // Parse parameters
                ProgramOptions programOptions = parseProgramOptions(args);
                if (programOptions == null) return;

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.JOBFILE_PARAMETERS,
                    "Program.Main",
                    String.Format("Parsed {0}", programOptions));

                #endregion

                #region Validate input parameters, output and job folders

                Console.WriteLine("Validating job file");

                // Validate job file exists
                if (PrepareJob.validateJobFileExists(programOptions) == false) return;

                Console.WriteLine("Validating output folder");

                // Validate output folder exists and create if necessary
                if (PrepareJob.validateOrCreateOutputFolder(programOptions) == false) return;

                // Set up the output job folder and job file path
                programOptions.JobName = Path.GetFileNameWithoutExtension(programOptions.InputJobFilePath);
                programOptions.OutputJobFolderPath = Path.Combine(programOptions.OutputFolderPath, programOptions.JobName);
                programOptions.OutputJobFilePath = Path.Combine(programOptions.OutputJobFolderPath, "jobparameters.json");

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Information,
                    EventId.JOBFILE_PARAMETERS,
                    "Program.Main",
                    String.Format("Now {0}", programOptions));

                Console.WriteLine(programOptions);

                Console.WriteLine("Validating job folder");

                // Create job folder if it doesn't exist
                // or
                // Clear out job folder if it already exists and restart of the job was requested
                if (PrepareJob.validateOrCreateJobOutputFolder(programOptions) == false) return;

                #endregion

                #region Process input job file to output job file

                Console.WriteLine("Processing input job file");

                // Validate job file for validity if the job is new
                // Expand list of targets from the input file 
                // Save validated job file to the output directory
                // Check if this job file was already already validated and exists in target folder
                if (Directory.Exists(programOptions.OutputJobFolderPath) == false || File.Exists(programOptions.OutputJobFilePath) == false)
                {
                    if (PrepareJob.validateAndExpandJobFileContents(programOptions) == false) return;
                }

                #endregion

                #region Setup of job file logging

                //logFolderName = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                //if (PrepareJob.createFolder(logFolderName) == false) return;
                //logFileName = String.Format("{0}-{1}-{2}.log", LogHelper.OFFLINE_DATA_TRACE_SOURCE, Path.GetFileNameWithoutExtension(programOptions.InputJobFilePath), dateTimeNow.ToString("yyyy-MM-dd"));
                //logFilePath = Path.Combine(logFolderName, logFileName);

                //// Create the trace
                //if (LogHelper.Instance.TraceSources.ContainsKey(LogHelper.OFFLINE_DATA_TRACE_SOURCE) == false)
                //{
                //    TraceSource ts = new TraceSource(LogHelper.OFFLINE_DATA_TRACE_SOURCE);
                //    TextWriterTraceListener twtl = new TextWriterTraceListener(logFilePath);
                //    twtl.Name = logFileName;
                //    twtl.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId;
                //    ts.Listeners.Add(twtl);
                //    LogHelper.Instance.TraceSources.TryAdd(LogHelper.OFFLINE_DATA_TRACE_SOURCE, ts);
                //}

                //LogHelper.Instance.NewCorrelationId();

                //LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                //    TraceEventType.Verbose,
                //    EventId.JOBFILE_PARAMETERS,
                //    "Program.Main",
                //    String.Format("Processing {0}", programOptions));

                #endregion

                #region Process output job file

                Console.WriteLine("Running job");

                // Go to work on the previously expanded job file
                ProcessJob.startOrContinueJob(programOptions);

                #endregion
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.EXCEPTION_GENERIC,
                    "Program.Main",
                    ex);
            }
            finally
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Stop,
                    EventId.APPLICATION_ENDED,
                    "Program.Main",
                    String.Format("Ending. Parameters='{0}'", String.Join(" ", args)));

                stopWatch.Stop();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Information,
                    EventId.FUNCTION_DURATION_EVENT,
                    "Program.Main",
                    String.Format("Application execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(String.Format("Application execution took {0:c} ({1} ms)", stopWatch.Elapsed, stopWatch.ElapsedMilliseconds));
                Console.ResetColor();

                LogHelper.Instance.FlushAndCloseAll(); 
            }
        }

        private static ProgramOptions parseProgramOptions(string[] args)
        {
            // Parse command line options
            ProgramOptions programOptions = new ProgramOptions();
            if (Parser.Default.ParseArguments(args, programOptions) == false)
            {

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.INVALID_PROGRAM_PARAMETERS,
                    "Program.Main",
                    String.Format("Could not parse command line arguments"));

                return null;
            }

            return programOptions;
        }
    }
}
