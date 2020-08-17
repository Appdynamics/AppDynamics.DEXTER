using AppDynamics.Dexter.ProcessingSteps;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

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
                JobStatus.ExtractSIMMetrics,
                JobStatus.ExtractDBMetrics,
                JobStatus.ExtractWEBMetrics,
                JobStatus.ExtractMOBILEMetrics,
                JobStatus.ExtractBIQMetrics,

                JobStatus.ExtractAPMMetricsList,

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
                JobStatus.IndexSIMMetrics,
                JobStatus.IndexDBMetrics,
                JobStatus.IndexWEBMetrics,
                JobStatus.IndexMOBILEMetrics,
                JobStatus.IndexBIQMetrics,

                JobStatus.IndexAPMMetricsList,

                JobStatus.IndexAPMFlowmaps,
                JobStatus.IndexAPMSnapshots,

                JobStatus.IndexControllerHealthCheck,
                JobStatus.IndexAPMHealthCheck,

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

                JobStatus.ReportAPMFlowmaps,

                JobStatus.ReportAPMMetricsList,

                JobStatus.ReportAPMSnapshots,
                JobStatus.ReportAPMSnapshotsMethodCallLines,
                JobStatus.ReportAPMFlameGraphs,

                JobStatus.ReportAPMEntityDetails,

                JobStatus.ReportHealthCheck,
                JobStatus.ReportAPMApplicationSummary,

                JobStatus.ReportAPMEntityDashboardScreenshots,

                JobStatus.ReportAPMIndividualSnapshots,

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
                loggerConsole.Error("Unable to load job input file {0}", programOptions.InputETLJobFilePath);

                return;
            }

            #region Output diagnostic parameters to log

            loggerConsole.Info("Starting job from status {0}({0:d})", jobConfiguration.Status);
            logger.Info("Starting job from status {0}({0:d})", jobConfiguration.Status);
            logger.Info("Job input: TimeRange.From='{0:o}', TimeRange.To='{1:o}', Time ranges='{2}'", jobConfiguration.Input.TimeRange.From, jobConfiguration.Input.TimeRange.To);
            logger.Info("DetectedEntities='{0}'", jobConfiguration.Input.DetectedEntities);
            logger.Info("Flowmaps='{0}'", jobConfiguration.Input.Flowmaps);
            logger.Info("Metrics='{0}'", jobConfiguration.Input.Metrics);
            logger.Info("Snapshots='{0}'", jobConfiguration.Input.Snapshots);
            logger.Info("Events='{0}'", jobConfiguration.Input.Events);
            logger.Info("Licenses='{0}''", jobConfiguration.Input.Licenses);
            logger.Info("Configuration='{0}'", jobConfiguration.Input.Configuration);
            logger.Info("UsersGroupsRolesPermissions='{0}'", jobConfiguration.Input.UsersGroupsRolesPermissions);
            logger.Info("EntityDashboards='{0}'", jobConfiguration.Input.EntityDashboards);

            if (jobConfiguration.Input.MetricsSelectionCriteria != null)
            {
                logger.Info("Job input: MetricsSelectionCriteria='{0}'", String.Join(",", jobConfiguration.Input.MetricsSelectionCriteria));
            }
            if (jobConfiguration.Input.SnapshotSelectionCriteria != null)
            {
                logger.Info("Job input, SnapshotSelectionCriteria: Tiers='{0}', TierTypes='{1}'",
                    String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.Tiers),
                    String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.TierTypes));
                logger.Info("Job input, SnapshotSelectionCriteria: BusinessTransactions='{0}', BusinessTransactionType='{1}'",
                    String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactions),
                    String.Join(",", jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionTypes));
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

            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
            {
                logger.Info("Expanded time ranges: From='{0:o}', To='{1:o}'", jobTimeRange.From, jobTimeRange.To);
            }

            logger.Info("Job output:");
            logger.Info("DetectedEntities='{0}'", jobConfiguration.Output.DetectedEntities);
            logger.Info("Flowmaps='{0}'", jobConfiguration.Output.Flowmaps);
            logger.Info("EntityMetrics='{0}'", jobConfiguration.Output.EntityMetrics);
            logger.Info("EntityMetricsGraphs='{0}'", jobConfiguration.Output.EntityMetricGraphs);
            logger.Info("Snapshots='{0}'", jobConfiguration.Output.Snapshots);
            logger.Info("Events='{0}'", jobConfiguration.Output.Events);
            logger.Info("Licenses='{0}''", jobConfiguration.Output.Licenses);
            logger.Info("Configuration='{0}'", jobConfiguration.Output.Configuration);
            logger.Info("UsersGroupsRolesPermissions='{0}'", jobConfiguration.Output.UsersGroupsRolesPermissions);
            logger.Info("EntityDashboards='{0}'", jobConfiguration.Output.EntityDashboards);
            logger.Info("HealthCheck='{0}'", jobConfiguration.Output.HealthCheck);
            logger.Info("ApplicationSummary='{0}'", jobConfiguration.Output.ApplicationSummary);
            
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
                case JobStatus.ExtractSIMMetrics:
                    return new ExtractSIMMetrics();
                case JobStatus.ExtractDBMetrics:
                    return new ExtractDBMetrics();
                case JobStatus.ExtractWEBMetrics:
                    return new ExtractWEBMetrics();
                case JobStatus.ExtractMOBILEMetrics:
                    return new ExtractMOBILEMetrics();
                case JobStatus.ExtractBIQMetrics:
                    return new ExtractBIQMetrics();

                case JobStatus.ExtractAPMMetricsList:
                    return new ExtractAPMMetricsList();

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
                case JobStatus.IndexSIMMetrics:
                    return new IndexSIMMetrics();
                case JobStatus.IndexDBMetrics:
                    return new IndexDBMetrics();
                case JobStatus.IndexWEBMetrics:
                    return new IndexWEBMetrics();
                case JobStatus.IndexMOBILEMetrics:
                    return new IndexMOBILEMetrics();
                case JobStatus.IndexBIQMetrics:
                    return new IndexBIQMetrics();

                case JobStatus.IndexAPMMetricsList:
                    return new IndexAPMMetricsList();

                case JobStatus.IndexAPMFlowmaps:
                    return new IndexAPMFlowmaps();
                case JobStatus.IndexAPMSnapshots:
                    return new IndexAPMSnapshots();

                case JobStatus.IndexControllerHealthCheck:
                    return new IndexControllerHealthCheck();
                case JobStatus.IndexAPMHealthCheck:
                    return new IndexAPMHealthCheck();

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
                
                case JobStatus.ReportAPMFlowmaps:
                    return new ReportAPMFlowmaps();

                case JobStatus.ReportAPMMetricsList:
                    return new ReportAPMMetricsList();

                case JobStatus.ReportAPMSnapshots:
                    return new ReportAPMSnapshots();
                case JobStatus.ReportAPMSnapshotsMethodCallLines:
                    return new ReportAPMSnapshotsMethodCallLines();
                case JobStatus.ReportAPMFlameGraphs:
                    return new ReportAPMFlameGraphs();
                case JobStatus.ReportAPMIndividualSnapshots:
                    return new ReportAPMIndividualSnapshots();

                case JobStatus.ReportAPMEntityDetails:
                    return new ReportAPMEntityDetails();

                case JobStatus.ReportHealthCheck:
                    return new ReportHealthCheck();
                case JobStatus.ReportAPMApplicationSummary:
                    return new ReportAPMApplicationSummary();

                case JobStatus.ReportAPMEntityDashboardScreenshots:
                    return new ReportAPMEntityDashboardScreenshots();

                default:
                    break;
            }

            return null;
        }
    }
}