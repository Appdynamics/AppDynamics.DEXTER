using CommandLine;
using System;

namespace AppDynamics.Dexter
{
    public class ProgramOptions
    {
        // ETL
        [Option('j', "job-file", Required = true, SetName = "etl", HelpText = "Input file defining the parameters of the ETL job to process.")]
        public string InputETLJobFilePath { get; set; }

        // Compare States
        //[Option('c', "compare-states-file", Required = true, SetName = "compare", HelpText = "Compare file defining the mappings of the state comparison to perform.")]
        public string InputCompareJobFilePath { get; set; }

        //[Option('l', "left-from-state-folder", Required = true, SetName = "compare", HelpText = "Folder of the ETL job to use as a left side/reference/from comparison.")]
        public string CompareReportReferenceFolderPath { get; set; }

        //[Option('r', "right-to-state-folder", Required = true, SetName = "compare", HelpText = "Folder of the ETL job to use as a right side/difference/to comparison.")]
        public string CompareReportDifferenceFolderPath { get; set; }

        // Common
        [Option('o', "output-folder", Required = false, HelpText = "Output folder where to store results of processing.")]
        public string OutputFolderPath { get; set; }

        [Option('d', "delete-previous-job-output", Required = false, HelpText = "If true, delete any results of previous processing.")]
        public bool DeletePreviousJobOutput { get; set; }

        [Option('s', "sequential-processing", Required = false, HelpText = "If true, process certain items during extraction and conversion sequentially.")]
        public bool ProcessSequentially { get; set; }

        // Version check
        [Option('v', "skip-version-check", Required = false, HelpText = "If true, skips the version check against GitHub repository.")]
        public bool SkipVersionCheck { get; set; }


        public string OutputJobFolderPath { get; set; }

        public string OutputJobFilePath { get; set; }

        public string ProgramLocationFolderPath { get; set; }

        public string JobName { get; set; }

        public JobOutput LicensedReports { get; set; }

        public override string ToString()
        {
            return String.Format(
@"ProgramOptions:
InputJobFilePath='{0}'
RestartJobFromBeginning='{1}'
OutputFolderPath='{2}'
OutputJobFolderPath='{3}'
OutputJobFilePath='{4}'
ProcessSequentially='{5}'
ProgramLocationFolderPath='{6}'
CompareFilePath='{7}'
ReferenceJobFolderPath='{8}'
DifferenceJobFolderPath='{9}'
SkipVersionCheck='{10}'
",
                this.InputETLJobFilePath, 
                this.DeletePreviousJobOutput, 
                this.OutputFolderPath, 
                this.OutputJobFolderPath, 
                this.OutputJobFilePath,
                this.ProcessSequentially, 
                this.ProgramLocationFolderPath,
                this.InputCompareJobFilePath, 
                this.CompareReportReferenceFolderPath, 
                this.CompareReportDifferenceFolderPath,
                this.SkipVersionCheck);
        }
    }
}
