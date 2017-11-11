using CommandLine;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
                #region Parse input parameters

                logger.Trace("Starting at {0:o}, Version={1}, Parameters={2}", DateTime.Now, Assembly.GetEntryAssembly().GetName().Version, String.Join(" ", args));

                // Parse parameters
                ProgramOptions programOptions = parseProgramOptions(args);
                if (programOptions == null) return;

                #endregion

                #region Validate input parameters, output and job folders

                // Validate job file exists
                loggerConsole.Info("Validating input job file");
                if (PrepareJob.validateJobFileExists(programOptions) == false) return;

                // Validate output folder exists and create if necessary
                loggerConsole.Info("Validating output folder");
                if (PrepareJob.validateOrCreateOutputFolder(programOptions) == false) return;

                // Set up the output job folder and job file path
                programOptions.JobName = Path.GetFileNameWithoutExtension(programOptions.InputJobFilePath);
                programOptions.OutputJobFolderPath = Path.Combine(programOptions.OutputFolderPath, programOptions.JobName);
                programOptions.OutputJobFilePath = Path.Combine(programOptions.OutputJobFolderPath, "jobparameters.json");

                logger.Trace("Adjusted ProgramOptions are now:\r\n{0}", programOptions);
                loggerConsole.Trace("Adjusted ProgramOptions are now:\r\n{0}", programOptions);

                // Create job folder if it doesn't exist
                // or
                // Clear out job folder if it already exists and restart of the job was requested
                loggerConsole.Info("Validating job folder");
                if (PrepareJob.validateOrCreateJobOutputFolder(programOptions) == false) return;

                #endregion

                #region Process input job file to output job file

                // Validate job file for validity if the job is new
                // Expand list of targets from the input file 
                // Save validated job file to the output directory
                // Check if this job file was already already validated and exists in target folder
                loggerConsole.Info("Processing input job file to output job file");
                if (Directory.Exists(programOptions.OutputJobFolderPath) == false || File.Exists(programOptions.OutputJobFilePath) == false)
                {
                    if (PrepareJob.validateAndExpandJobFileContents(programOptions) == false) return;
                }

                #endregion

                #region Process output job file

                // Go to work on the expanded and validated job file
                loggerConsole.Info("Running job");
                ProcessJob.startOrContinueJob(programOptions);

                #endregion
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

                LogManager.Flush();

                //LogManager.Configuration.AllTargets
                //    .OfType<BufferingTargetWrapper>()
                //    .ToList()
                //    .ForEach(b => b.Flush(e =>
                //    {
                //        do nothing here
                //    }));
            }
        }

        private static ProgramOptions parseProgramOptions(string[] args)
        {
            // Parse command line options
            ProgramOptions programOptions = new ProgramOptions();
            if (Parser.Default.ParseArguments(args, programOptions) == false)
            {
                //logger.Error("Could not parse command line arguments into ProgramOptions");
                return null;
            }
            else
            {
                //logger.Trace("Parsed ProgramOptions\r\n{0}", programOptions);
                return programOptions;
            }
        }
    }
}
