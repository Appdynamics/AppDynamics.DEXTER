using AppDynamics.Dexter.ProcessingSteps;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AppDynamics.Dexter
{
    public class JobStepRouter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        private static List<JobStatus> jobSteps = new List<JobStatus>
            {
                // Extract data
                JobStatus.ExtractControllerVersionAndApplications,
                JobStatus.ExtractControllerConfiguration,
                JobStatus.ExtractControllerUsersGroupsRolesAndPermissions,

                JobStatus.ExtractDashboards,
                JobStatus.ExtractLicenses,

                JobStatus.ExtractControllerAuditEventsAndNotifications,
                JobStatus.ExtractApplicationEventsAndHealthRuleViolations,

                JobStatus.ExtractApplicationHealthRulesAlertsPolicies,
                JobStatus.ExtractAPMConfiguration,
                JobStatus.ExtractDBConfiguration,
                JobStatus.ExtractWEBConfiguration,
                JobStatus.ExtractMOBILEConfiguration,
                //JobStatus.ExtractBIQConfiguration,

                JobStatus.ExtractAPMEntities,
                JobStatus.ExtractSIMEntities,
                JobStatus.ExtractDBEntities,
                JobStatus.ExtractWEBEntities,
                JobStatus.ExtractMOBILEEntities,
                JobStatus.ExtractBIQEntities,

                JobStatus.ExtractAPMMetrics,
                JobStatus.ExtractAPMFlowmaps,
                JobStatus.ExtractAPMEntityDashboardScreenshots,
                JobStatus.ExtractAPMSnapshots,

                // Index data
                JobStatus.IndexControllerVersionAndApplications,
                JobStatus.IndexControllerConfiguration,
                JobStatus.IndexControllerUsersGroupsRolesAndPermissions,

                JobStatus.IndexDashboards,
                JobStatus.IndexLicenses,

                JobStatus.IndexControllerAuditEventsAndNotifications,
                JobStatus.IndexApplicationEventsAndHealthRuleViolations,

                JobStatus.IndexAPMEntities,
                JobStatus.IndexSIMEntities,
                JobStatus.IndexDBEntities,
                JobStatus.IndexWEBEntities ,
                JobStatus.IndexMOBILEEntities,
                JobStatus.IndexBIQEntities,

                JobStatus.IndexApplicationHealthRulesAlertsPolicies,
                JobStatus.IndexAPMConfiguration,
                JobStatus.IndexDBConfiguration,
                JobStatus.IndexWEBConfiguration,
                JobStatus.IndexMOBILEConfiguration,
                //JobStatus.IndexBIQConfiguration,

                JobStatus.IndexApplicationConfigurationDifferences,

                JobStatus.IndexAPMMetrics,
                JobStatus.IndexAPMFlowmaps,
                JobStatus.IndexAPMSnapshots,

                // Report data
                JobStatus.ReportControllerAndApplicationConfiguration,
                JobStatus.ReportControllerUsersGroupsRolesAndPermissions,

                JobStatus.ReportDashboards,
                JobStatus.ReportLicenses,

                JobStatus.ReportApplicationEventsAndHealthRuleViolations,

                JobStatus.ReportAPMEntities,
                JobStatus.ReportSIMEntities,
                JobStatus.ReportDBEntities,
                JobStatus.ReportWEBEntities,
                JobStatus.ReportMOBILEEntities,
                JobStatus.ReportBIQEntities,

                JobStatus.ReportAPMMetrics,
                JobStatus.ReportAPMMetricGraphs,

                JobStatus.ReportAPMSnapshots,
                JobStatus.ReportAPMSnapshotsMethodCallLines,
                JobStatus.ReportAPMFlameGraphs,

                JobStatus.ReportAPMEntityDetails,

                JobStatus.ReportAPMEntityDashboardScreenshots,

                JobStatus.ApplicationHealthCheckComparison,
                JobStatus.ReportApplicationHealthCheck,

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
            logger.Info("Job input: ConfigurationComparisonReferenceAPM='{0}'", jobConfiguration.Input.ConfigurationComparisonReferenceAPM);
            logger.Info("Job input: ConfigurationComparisonReferenceWEB='{0}'", jobConfiguration.Input.ConfigurationComparisonReferenceWEB);
            logger.Info("Job input: ConfigurationComparisonReferenceMOBILE='{0}'", jobConfiguration.Input.ConfigurationComparisonReferenceMOBILE);
            logger.Info("Job input: ConfigurationComparisonReferenceDB='{0}'", jobConfiguration.Input.ConfigurationComparisonReferenceDB);

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
                        loggerConsole.Warn("If you need support, please review https://github.com/Appdynamics/AppDynamics.DEXTER/wiki#getting-support and send the logs");

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

                // Do a forced GC Collection after all the work in a step. 
                // Probably unnecessary, but to counteract the ulimit handle consumption on non-Windows machines
                GC.Collect();
            }
        }

        private static JobStepBase getJobStepFromFactory(JobStatus jobStatus)
        {
            switch (jobStatus)
            {
                // Extract Data
                case JobStatus.ExtractControllerVersionAndApplications:
                    return new ExtractControllerVersionAndApplications();
                case JobStatus.ExtractControllerConfiguration:
                    return new ExtractControllerConfiguration();
                case JobStatus.ExtractControllerUsersGroupsRolesAndPermissions:
                    return new ExtractControllerUsersGroupsRolesAndPermissions();

                case JobStatus.ExtractDashboards:
                    return new ExtractDashboards();
                case JobStatus.ExtractLicenses:
                    return new ExtractLicenses();

                case JobStatus.ExtractControllerAuditEventsAndNotifications:
                    return new ExtractControllerAuditEventsAndNotifications();
                case JobStatus.ExtractApplicationEventsAndHealthRuleViolations:
                    return new ExtractApplicationEventsAndHealthRuleViolations();

                case JobStatus.ExtractApplicationHealthRulesAlertsPolicies:
                    return new ExtractApplicationHealthRulesAlertsPolicies();
                case JobStatus.ExtractAPMConfiguration:
                    return new ExtractAPMConfiguration();
                case JobStatus.ExtractDBConfiguration:
                    return new ExtractDBConfiguration();
                case JobStatus.ExtractWEBConfiguration:
                    return new ExtractWEBConfiguration();
                case JobStatus.ExtractMOBILEConfiguration:
                    return new ExtractMOBILEConfiguration();
                case JobStatus.ExtractBIQConfiguration:
                    return new ExtractBIQConfiguration();

                case JobStatus.ExtractAPMEntities:
                    return new ExtractAPMEntities();
                case JobStatus.ExtractSIMEntities:
                    return new ExtractSIMEntities();
                case JobStatus.ExtractDBEntities:
                    return new ExtractDBEntities();
                case JobStatus.ExtractWEBEntities:
                    return new ExtractWEBEntities();
                case JobStatus.ExtractMOBILEEntities:
                    return new ExtractMOBILEEntities();
                case JobStatus.ExtractBIQEntities:
                    return new ExtractBIQEntities();

                case JobStatus.ExtractAPMMetrics:
                    return new ExtractAPMMetrics();
                case JobStatus.ExtractAPMFlowmaps:
                    return new ExtractAPMFlowmaps();
                case JobStatus.ExtractAPMEntityDashboardScreenshots:
                    return new ExtractAPMEntityDashboardScreenshots();
                case JobStatus.ExtractAPMSnapshots:
                    return new ExtractAPMSnapshots();


                // Index data
                case JobStatus.IndexControllerVersionAndApplications:
                    return new IndexControllerVersionAndApplications();
                case JobStatus.IndexControllerConfiguration:
                    return new IndexControllerConfiguration();
                case JobStatus.IndexControllerUsersGroupsRolesAndPermissions:
                    return new IndexControllerUsersGroupsRolesAndPermissions();

                case JobStatus.IndexDashboards:
                    return new IndexDashboards();
                case JobStatus.IndexLicenses:
                    return new IndexLicenses();

                case JobStatus.IndexControllerAuditEventsAndNotifications:
                    return new IndexControllerAuditEventsAndNotifications();
                case JobStatus.IndexApplicationEventsAndHealthRuleViolations:
                    return new IndexApplicationEventsAndHealthRuleViolations();

                case JobStatus.IndexAPMEntities:
                    return new IndexAPMEntities();
                case JobStatus.IndexSIMEntities:
                    return new IndexSIMEntities();
                case JobStatus.IndexDBEntities:
                    return new IndexDBEntities();
                case JobStatus.IndexWEBEntities:
                    return new IndexWEBEntities();
                case JobStatus.IndexMOBILEEntities:
                    return new IndexMOBILEEntities();
                case JobStatus.IndexBIQEntities:
                    return new IndexBIQEntities();

                case JobStatus.IndexApplicationHealthRulesAlertsPolicies:
                    return new IndexApplicationHealthRulesAlertsPolicies();
                case JobStatus.IndexAPMConfiguration:
                    return new IndexAPMConfiguration();
                case JobStatus.IndexDBConfiguration:
                    return new IndexDBConfiguration();
                case JobStatus.IndexWEBConfiguration:
                    return new IndexWEBConfiguration();
                case JobStatus.IndexMOBILEConfiguration:
                    return new IndexMOBILEConfiguration();
                case JobStatus.IndexBIQConfiguration:
                    return new IndexBIQConfiguration();

                case JobStatus.IndexApplicationConfigurationDifferences:
                    return new IndexApplicationConfigurationDifferences();

                case JobStatus.IndexAPMMetrics:
                    return new IndexAPMMetrics();
                case JobStatus.IndexAPMFlowmaps:
                    return new IndexAPMFlowmaps();
                case JobStatus.IndexAPMSnapshots:
                    return new IndexAPMSnapshots();


                // Report data
                case JobStatus.ReportControllerAndApplicationConfiguration:
                    return new ReportControllerAndApplicationConfiguration();
                case JobStatus.ReportControllerUsersGroupsRolesAndPermissions:
                    return new ReportControllerUsersGroupsRolesAndPermissions();

                case JobStatus.ReportDashboards:
                    return new ReportDashboards();
                case JobStatus.ReportLicenses:
                    return new ReportLicenses();

                case JobStatus.ReportApplicationEventsAndHealthRuleViolations:
                    return new ReportApplicationEventsAndHealthRuleViolations();

                case JobStatus.ReportAPMEntities:
                    return new ReportAPMEntities();
                case JobStatus.ReportSIMEntities:
                    return new ReportSIMEntities();
                case JobStatus.ReportDBEntities:
                    return new ReportDBEntities();
                case JobStatus.ReportWEBEntities:
                    return new ReportWEBEntities();
                case JobStatus.ReportMOBILEEntities:
                    return new ReportMOBILEEntities();
                case JobStatus.ReportBIQEntities:
                    return new ReportBIQEntities();

                case JobStatus.ReportAPMMetrics:
                    return new ReportAPMMetrics();
                case JobStatus.ReportAPMMetricGraphs:
                    return new ReportAPMMetricGraphs();

                case JobStatus.ReportAPMSnapshots:
                    return new ReportAPMSnapshots();
                case JobStatus.ReportAPMSnapshotsMethodCallLines:
                    return new ReportAPMSnapshotsMethodCallLines();
                case JobStatus.ReportAPMFlameGraphs:
                    return new ReportAPMFlameGraphs();

                case JobStatus.ReportAPMEntityDetails:
                    return new ReportAPMEntityDetails();

                case JobStatus.ReportAPMEntityDashboardScreenshots:
                    return new ReportAPMEntityDashboardScreenshots();
                //case JobStatus.ApplicationHealthCheckComparison:
                  //  return new ApplicationHealthCheckComparison();

                case JobStatus.ReportApplicationHealthCheck:
                    return new ReportApplicationHealthCheck();


                default:
                    break;
            }

            return null;
        }
    }
}