using CommandLine;
using CommandLine.Text;
using System;

namespace AppDynamics.Dexter
{
    public class ProgramOptions
    {
        [Option('j', "job-file", Required = true, SetName ="etl", HelpText = "Input file defining the parameters of the ETL job to process.")]
        public string InputJobFilePath { get; set; }

        [Option('o', "output-folder", Required = false, HelpText = "Output folder where to store results of processing.")]
        public string OutputFolderPath { get; set; }

        [Option('c', "compare-states-file", Required = true, SetName = "compare",  HelpText = "Compare file defining the mappings of the state comparison to perform.")]
        public string CompareFilePath { get; set; }

        [Option('l', "left-from-state-folder", Required = true, SetName = "compare", HelpText = "Folder of the ETL job to use as a left side/reference/from comparison.")]
        public string FromJobFolderPath { get; set; }

        [Option('r', "right-to-state-folder", Required = true, SetName = "compare", HelpText = "Folder of the ETL job to use as a right side/target/to comparison.")]
        public string ToJobFolderPath{ get; set; }

        [Option('d', "delete-previous-job-output", Required = false, HelpText = "If true, delete any results of previous processing.")]
        public bool RestartJobFromBeginning { get; set; }

        [Option('s', "sequential-processing", Required = false, HelpText = "If true, process certain items during extraction and conversion sequentially.")]
        public bool ProcessSequentially { get; set; }

        [Option('v', "skip-version-check", Required = false, HelpText = "If true, skips the version check against GitHub repository.")]
        public bool SkipVersionCheck { get; set; }

        public string OutputJobFolderPath { get; set;}

        public string OutputJobFilePath { get; set; }

        public string ProgramLocationFolderPath { get; set; }

        public string JobName { get; set; }

        public override string ToString()
        {
            return String.Format("ProgramOptions:\r\nInputJobFilePath='{0}'\r\nRestartJobFromBeginning='{1}'\r\nOutputFolderPath='{2}'\r\nOutputJobFolderPath='{3}'\r\nOutputJobFilePath='{4}'\r\nProcessSequentially='{5}'\r\nProgramLocationFolderPath='{6}'", this.InputJobFilePath, this.RestartJobFromBeginning, this.OutputFolderPath, this.OutputJobFolderPath, this.OutputJobFilePath, this.ProcessSequentially, this.ProgramLocationFolderPath);
        }
    }
}
