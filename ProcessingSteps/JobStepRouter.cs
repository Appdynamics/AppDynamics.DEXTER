using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AppDynamics.Dexter.ProcessingSteps;

namespace AppDynamics.Dexter
{
    public class JobStepRouter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        private static List<JobStatus> jobSteps = new List<JobStatus>
            {
                // Get data
                JobStatus.ExtractControllerApplicationsAndEntities,
                JobStatus.ExtractControllerSIMApplicationsAndEntities,
                JobStatus.ExtractControllerAndApplicationConfiguration,
                JobStatus.ExtractApplicationAndEntityMetrics,
                JobStatus.ExtractApplicationAndEntityFlowmaps,
                JobStatus.ExtractEventsAndHealthRuleViolations,
                JobStatus.ExtractSnapshots,

                // Index data
                JobStatus.IndexControllerApplicationsAndEntities,
                JobStatus.IndexControllerSIMApplicationsAndEntities,
                JobStatus.IndexControllerAndApplicationConfiguration,
                JobStatus.IndexApplicationConfigurationComparison,
                JobStatus.IndexApplicationAndEntityMetrics,
                JobStatus.IndexApplicationAndEntityFlowmaps,
                JobStatus.IndexEventsAndHealthRuleViolations,
                JobStatus.IndexSnapshots,

                // Report data
                JobStatus.ReportControlerApplicationsAndEntities,
                JobStatus.ReportControlerSIMApplicationsAndEntities,
                JobStatus.ReportControllerAndApplicationConfiguration,
                JobStatus.ReportApplicationAndEntityMetrics,
                JobStatus.ReportApplicationAndEntityMetricGraphs,
                JobStatus.ReportEventsAndHealthRuleViolations,
                JobStatus.ReportSnapshots,
                JobStatus.ReportSnapshotsMethodCallLines,
                JobStatus.ReportIndividualApplicationAndEntityDetails,
                JobStatus.ReportFlameGraphs,

                // Done 
                JobStatus.Done,

                JobStatus.Error
            };

        private static LinkedList<JobStatus> jobStepsLinked = new LinkedList<JobStatus>(jobSteps);

        public static void ExecuteJobThroughSteps(ProgramOptions programOptions)
        {
            // Read job file from the location
            JobConfiguration jobConfiguration = FileIOHelper.ReadJobConfigurationFromFile(programOptions.OutputJobFilePath);
            if (jobConfiguration == null)
            {
                loggerConsole.Error("Unable to load job input file {0}", programOptions.InputJobFilePath);

                return;
            }

            #region Output diagnostic parameters to log

            loggerConsole.Info("Starting job from status {0}({0:d})", jobConfiguration.Status);
            logger.Info("Starting job from status {0}({0:d})", jobConfiguration.Status);
            logger.Info("Job input: TimeRange.From='{0:o}', TimeRange.To='{1:o}', Time ranges='{2}', Flowmaps='{3}', Metrics='{4}', Snapshots='{5}', Configuration='{6}', Events='{7}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To, jobConfiguration.Input.HourlyTimeRanges.Count, jobConfiguration.Input.Flowmaps, jobConfiguration.Input.Metrics, jobConfiguration.Input.Snapshots, jobConfiguration.Input.Configuration, jobConfiguration.Input.Events);
            if (jobConfiguration.Input.MetricsSelectionCriteria != null)
            {
                logger.Info("Job input: MetricsSelectionCriteria='{0}'", String.Join(",", jobConfiguration.Input.MetricsSelectionCriteria));
            }
            if (jobConfiguration.Input.SnapshotSelectionCriteria != null)
            {
                PropertyInfo[] pis = jobConfiguration.Input.SnapshotSelectionCriteria.TierType.GetType().GetProperties();
                StringBuilder sb = new StringBuilder(16 * pis.Length);
                foreach (PropertyInfo pi in pis)
                {
                    sb.AppendFormat("{0}={1}, ", pi.Name, pi.GetValue(jobConfiguration.Input.SnapshotSelectionCriteria.TierType));
                }
                logger.Info("Job input, SnapshotSelectionCriteria: Tiers='{0}', TierTypes='{1}'",
                    String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.Tiers),
                    sb.ToString());

                pis = jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType.GetType().GetProperties();
                sb = new StringBuilder(16 * pis.Length);
                foreach (PropertyInfo pi in pis)
                {
                    sb.AppendFormat("{0}={1}, ", pi.Name, pi.GetValue(jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType));
                }
                logger.Info("Job input, SnapshotSelectionCriteria: BusinessTransactions='{0}', BusinessTransactionType='{1}'",
                    String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactions),
                    sb.ToString());
                logger.Info("Job input, SnapshotSelectionCriteria: UserExperience.Normal='{0}', UserExperience.Slow='{1}', UserExperience.VerySlow='{2}', UserExperience.Stall='{3}', UserExperience.Error='{4}'",
                    jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Normal,
                    jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Slow,
                    jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.VerySlow,
                    jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Stall,
                    jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Error);
                logger.Info("Job input, SnapshotSelectionCriteria: SnapshotType.Full='{0}', SnapshotType.Partial='{1}', SnapshotType.None='{2}'",
                    jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.Full,
                    jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.Partial,
                    jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.None);
            }
            if (jobConfiguration.Input.ConfigurationComparisonReferenceCriteria != null)
            {
                logger.Info("Job input: ConfigurationComparisonReferenceCriteria.Controller='{0}', ConfigurationComparisonReferenceCriteria.Application='{1}'", jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Controller, jobConfiguration.Input.ConfigurationComparisonReferenceCriteria.Application);
            }

            logger.Info("Job output: DetectedEntities='{0}', EntityMetrics='{1}', EntityDetails='{2}', Snapshots='{3}', Configuration='{4}', Events='{5}'", jobConfiguration.Output.DetectedEntities, jobConfiguration.Output.EntityMetrics, jobConfiguration.Output.EntityDetails, jobConfiguration.Output.Snapshots, jobConfiguration.Output.Configuration, jobConfiguration.Output.Events);

            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
            {
                logger.Info("Expanded time ranges: From='{0:o}', To='{1:o}'", jobTimeRange.From, jobTimeRange.To);
            }

            #endregion

            // Run the step and move to next until things are done
            while (jobConfiguration.Status != JobStatus.Done && jobConfiguration.Status != JobStatus.Error)
            {
                loggerConsole.Info("Executing job step {0}({0:d})", jobConfiguration.Status);
                logger.Info("Executing job step {0}({0:d})", jobConfiguration.Status);

                JobStepBase jobStep = getJobStepFromFactory(jobConfiguration.Status);
                if (jobStep != null)
                {
                    if (jobStep.Execute(programOptions, jobConfiguration) == false)
                    {
                        jobConfiguration.Status = JobStatus.Error;
                    }
                }
                if (jobConfiguration.Status != JobStatus.Error)
                {
                    jobConfiguration.Status = jobStepsLinked.Find(jobConfiguration.Status).Next.Value;
                }

                // Save the resulting JSON file to the job target folder
                if (FileIOHelper.WriteJobConfigurationToFile(jobConfiguration, programOptions.OutputJobFilePath) == false)
                {
                    loggerConsole.Error("Unable to write job input file {0}", programOptions.OutputJobFilePath);

                    return;
                }
            }
        }

        private static JobStepBase getJobStepFromFactory(JobStatus jobStatus)
        {
            switch (jobStatus)
            {
                case JobStatus.ExtractControllerApplicationsAndEntities:
                    return new ExtractControllerApplicationsAndEntities();

                case JobStatus.ExtractControllerSIMApplicationsAndEntities:
                    return new ExtractControllerSIMApplicationsAndEntities();

                case JobStatus.ExtractControllerAndApplicationConfiguration:
                    return new ExtractControllerAndApplicationConfiguration();

                case JobStatus.ExtractApplicationAndEntityMetrics:
                    return new ExtractApplicationAndEntityMetrics();

                case JobStatus.ExtractApplicationAndEntityFlowmaps:
                    return new ExtractApplicationAndEntityFlowmaps();

                case JobStatus.ExtractSnapshots:
                    return new ExtractSnapshots();

                case JobStatus.ExtractEventsAndHealthRuleViolations:
                    return new ExtractEventsAndHealthRuleViolations();


                case JobStatus.IndexControllerApplicationsAndEntities:
                    return new IndexControllerApplicationsAndEntities();

                case JobStatus.IndexControllerSIMApplicationsAndEntities:
                    return new IndexControllerSIMApplicationsAndEntities();

                case JobStatus.IndexControllerAndApplicationConfiguration:
                    return new IndexControllerAndApplicationConfiguration();

                case JobStatus.IndexApplicationConfigurationComparison:
                    return new IndexApplicationConfigurationComparison();

                case JobStatus.IndexApplicationAndEntityMetrics:
                    return new IndexApplicationAndEntityMetrics();

                case JobStatus.IndexApplicationAndEntityFlowmaps:
                    return new IndexApplicationAndEntityFlowmaps();

                case JobStatus.IndexEventsAndHealthRuleViolations:
                    return new IndexEventsAndHealthRuleViolations();

                case JobStatus.IndexSnapshots:
                    return new IndexSnapshots();


                case JobStatus.ReportControlerApplicationsAndEntities:
                    return new ReportControlerApplicationsAndEntities();

                case JobStatus.ReportControlerSIMApplicationsAndEntities:
                    return new ReportControlerSIMApplicationsAndEntities();

                case JobStatus.ReportControllerAndApplicationConfiguration:
                    return new ReportControllerAndApplicationConfiguration();

                case JobStatus.ReportEventsAndHealthRuleViolations:
                    return new ReportEventsAndHealthRuleViolations();

                case JobStatus.ReportApplicationAndEntityMetrics:
                    return new ReportApplicationAndEntityMetrics();

                case JobStatus.ReportApplicationAndEntityMetricGraphs:
                    return new ReportApplicationAndEntityMetricGraphs();

                case JobStatus.ReportSnapshots:
                    return new ReportSnapshots();

                case JobStatus.ReportSnapshotsMethodCallLines:
                    return new ReportSnapshotsMethodCallLines();

                case JobStatus.ReportIndividualApplicationAndEntityDetails:
                    return new ReportIndividualApplicationAndEntityDetails();

                case JobStatus.ReportFlameGraphs:
                    return new ReportFlameGraphs();

                default:
                    break;
            }

            return null;
        }
    }
}
