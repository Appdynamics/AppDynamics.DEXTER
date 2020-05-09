using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractDBEntities : JobStepBase
    {
        public override bool Execute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            StepTiming stepTimingFunction = new StepTiming();
            stepTimingFunction.JobFileName = programOptions.OutputJobFilePath;
            stepTimingFunction.StepName = jobConfiguration.Status.ToString();
            stepTimingFunction.StepID = (int)jobConfiguration.Status;
            stepTimingFunction.StartTime = DateTime.Now;
            stepTimingFunction.NumEntities = jobConfiguration.Target.Count;

            this.DisplayJobStepStartingStatus(jobConfiguration);

            FilePathMap = new FilePathMap(programOptions, jobConfiguration);

            try
            {
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_DB) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_DB);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_DB);

                    return true;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_DB) continue;

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    stepTimingTarget.NumEntities = 1;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Target step variables

                        Version version4_5 = new Version(4, 5);
                        Version version4_4 = new Version(4, 4);
                        Version version20_4 = new Version(20, 4);
                        Version versionThisController = new Version(jobTarget.ControllerVersion);

                        #endregion

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                        #endregion

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            controllerApi.PrivateApiLogin();

                            #region Collector definitions

                            // Only collect once per Controller since there is only one Database Application
                            if (File.Exists(FilePathMap.DBCollectorDefinitionsForEntitiesFilePath(jobTarget)) == false)
                            {
                                loggerConsole.Info("Collector Definitions");

                                string collectorDefinitionsJSON = controllerApi.GetDBCollectorsConfiguration("4.5");
                                if (collectorDefinitionsJSON == String.Empty) collectorDefinitionsJSON = controllerApi.GetDBCollectorsConfiguration("4.4");
                                if (collectorDefinitionsJSON != String.Empty) FileIOHelper.SaveFileToPath(collectorDefinitionsJSON, FilePathMap.DBCollectorDefinitionsForEntitiesFilePath(jobTarget));
                            }

                            #endregion

                            #region Collectors

                            // Only collect once per Controller since there is only one Database Application
                            if (File.Exists(FilePathMap.DBCollectorsCallsDataFilePath(jobTarget)) == false)
                            {
                                loggerConsole.Info("Collectors - Calls");

                                string collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls(fromTimeUnix, toTimeUnix, "4.5");
                                if (collectorsJSON == String.Empty) collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls(fromTimeUnix, toTimeUnix, "4.4");
                                if (collectorsJSON != String.Empty) FileIOHelper.SaveFileToPath(collectorsJSON, FilePathMap.DBCollectorsCallsDataFilePath(jobTarget));

                                loggerConsole.Info("Collectors - Time Taken");

                                collectorsJSON = String.Empty;
                                collectorsJSON = controllerApi.GetDBRegisteredCollectorsTimeSpent(fromTimeUnix, toTimeUnix, "4.5");
                                if (collectorsJSON == String.Empty) collectorsJSON = controllerApi.GetDBRegisteredCollectorsTimeSpent(fromTimeUnix, toTimeUnix, "4.4");
                                if (collectorsJSON != String.Empty) FileIOHelper.SaveFileToPath(collectorsJSON, FilePathMap.DBCollectorsTimeSpentDataFilePath(jobTarget));
                            }

                            #endregion
                        }

                        // Check the file existence to support resuming previously interrupted jobs
                        if (File.Exists(FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange)) == false)
                        {
                            ParallelOptions parallelOptions = new ParallelOptions();
                            if (programOptions.ProcessSequentially == true)
                            {
                                parallelOptions.MaxDegreeOfParallelism = 1;
                            }

                            Parallel.Invoke(parallelOptions,
                                () =>
                                {
                                    using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApiParallel.PrivateApiLogin();

                                        loggerConsole.Info("All Wait States");
                                        string allWaitStatesJSON = controllerApiParallel.GetDBAllWaitStates(jobTarget.DBCollectorID);
                                        if (allWaitStatesJSON != String.Empty) FileIOHelper.SaveFileToPath(allWaitStatesJSON, FilePathMap.DBAllWaitStatesDataFilePath(jobTarget));

                                        loggerConsole.Info("Current Wait States");
                                        if (versionThisController >= version20_4)
                                        {
                                            string currentWaitStatesJSON = controllerApiParallel.GetDCurrentWaitStates_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (currentWaitStatesJSON != String.Empty) FileIOHelper.SaveFileToPath(currentWaitStatesJSON, FilePathMap.DBCurrentWaitStatesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string currentWaitStatesJSON = controllerApiParallel.GetDCurrentWaitStates_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (currentWaitStatesJSON != String.Empty) FileIOHelper.SaveFileToPath(currentWaitStatesJSON, FilePathMap.DBCurrentWaitStatesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string currentWaitStatesJSON = controllerApiParallel.GetDCurrentWaitStates_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (currentWaitStatesJSON != String.Empty) FileIOHelper.SaveFileToPath(currentWaitStatesJSON, FilePathMap.DBCurrentWaitStatesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                    }
                                },
                                () =>
                                {
                                    using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApiParallel.PrivateApiLogin();

                                        loggerConsole.Info("Queries");
                                        if (versionThisController >= version20_4)
                                        {
                                            string queriesJSON = controllerApiParallel.GetDBQueries_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (queriesJSON != String.Empty) FileIOHelper.SaveFileToPath(queriesJSON, FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string queriesJSON = controllerApiParallel.GetDBQueries_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (queriesJSON != String.Empty) FileIOHelper.SaveFileToPath(queriesJSON, FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string queriesJSON = controllerApiParallel.GetDBQueries_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (queriesJSON != String.Empty) FileIOHelper.SaveFileToPath(queriesJSON, FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }

                                        loggerConsole.Info("Clients");
                                        if (versionThisController >= version20_4)
                                        {
                                            string clientsJSON = controllerApiParallel.GetDBClients_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (clientsJSON != String.Empty) FileIOHelper.SaveFileToPath(clientsJSON, FilePathMap.DBClientsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string clientsJSON = controllerApiParallel.GetDBClients_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (clientsJSON != String.Empty) FileIOHelper.SaveFileToPath(clientsJSON, FilePathMap.DBClientsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string clientsJSON = controllerApiParallel.GetDBClients_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (clientsJSON != String.Empty) FileIOHelper.SaveFileToPath(clientsJSON, FilePathMap.DBClientsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                    }
                                },
                                () =>
                                {
                                    using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApiParallel.PrivateApiLogin();

                                        loggerConsole.Info("Sessions");
                                        if (versionThisController >= version20_4)
                                        {
                                            string sessionsJSON = controllerApiParallel.GetDBSessions_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (sessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(sessionsJSON, FilePathMap.DBSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string sessionsJSON = controllerApiParallel.GetDBSessions_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (sessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(sessionsJSON, FilePathMap.DBSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string sessionsJSON = controllerApiParallel.GetDBSessions_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (sessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(sessionsJSON, FilePathMap.DBSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }

                                        loggerConsole.Info("Databases");
                                        if (versionThisController >= version20_4)
                                        {
                                            string databasesJSON = controllerApiParallel.GetDBDatabases_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (databasesJSON != String.Empty) FileIOHelper.SaveFileToPath(databasesJSON, FilePathMap.DBDatabasesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string databasesJSON = controllerApiParallel.GetDBDatabases_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (databasesJSON != String.Empty) FileIOHelper.SaveFileToPath(databasesJSON, FilePathMap.DBDatabasesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string databasesJSON = controllerApiParallel.GetDBDatabases_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (databasesJSON != String.Empty) FileIOHelper.SaveFileToPath(databasesJSON, FilePathMap.DBDatabasesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }

                                        loggerConsole.Info("Users");
                                        if (versionThisController >= version20_4)
                                        {
                                            string usersJSON = controllerApiParallel.GetDBUsers_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (usersJSON != String.Empty) FileIOHelper.SaveFileToPath(usersJSON, FilePathMap.DBUsersDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string usersJSON = controllerApiParallel.GetDBUsers_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (usersJSON != String.Empty) FileIOHelper.SaveFileToPath(usersJSON, FilePathMap.DBUsersDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string usersJSON = controllerApiParallel.GetDBUsers_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (usersJSON != String.Empty) FileIOHelper.SaveFileToPath(usersJSON, FilePathMap.DBUsersDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                    }
                                },
                                () =>
                                {
                                    using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApiParallel.PrivateApiLogin();

                                        loggerConsole.Info("Modules");
                                        if (versionThisController >= version20_4)
                                        {
                                            string modulesJSON = controllerApiParallel.GetDBModules_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (modulesJSON != String.Empty) FileIOHelper.SaveFileToPath(modulesJSON, FilePathMap.DBModulesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string modulesJSON = controllerApiParallel.GetDBModules_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (modulesJSON != String.Empty) FileIOHelper.SaveFileToPath(modulesJSON, FilePathMap.DBModulesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string modulesJSON = controllerApiParallel.GetDBModules_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (modulesJSON != String.Empty) FileIOHelper.SaveFileToPath(modulesJSON, FilePathMap.DBModulesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }

                                        loggerConsole.Info("Programs");
                                        if (versionThisController >= version20_4)
                                        {
                                            string programsJSON = controllerApiParallel.GetDBPrograms_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (programsJSON != String.Empty) FileIOHelper.SaveFileToPath(programsJSON, FilePathMap.DBProgramsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            string programsJSON = controllerApiParallel.GetDBPrograms_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (programsJSON != String.Empty) FileIOHelper.SaveFileToPath(programsJSON, FilePathMap.DBProgramsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            string programsJSON = controllerApiParallel.GetDBPrograms_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (programsJSON != String.Empty) FileIOHelper.SaveFileToPath(programsJSON, FilePathMap.DBProgramsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }

                                        loggerConsole.Info("Business Transactions");
                                        if (versionThisController >= version20_4)
                                        {
                                            string businessTransactionsJSON = controllerApiParallel.GetDBBusinessTransactions_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (businessTransactionsJSON != String.Empty) FileIOHelper.SaveFileToPath(businessTransactionsJSON, FilePathMap.DBBusinessTransactionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else
                                        {
                                            string businessTransactionsJSON = controllerApiParallel.GetDBBusinessTransactions_Pre_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix);
                                            if (businessTransactionsJSON != String.Empty) FileIOHelper.SaveFileToPath(businessTransactionsJSON, FilePathMap.DBBusinessTransactionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                    }
                                },
                                () =>
                                {
                                    using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApiParallel.PrivateApiLogin();
                                        
                                        loggerConsole.Info("All Blocked Sessions");
                                        string blockedSessionsJSON = String.Empty;
                                        if (versionThisController >= version20_4)
                                        {
                                            blockedSessionsJSON = controllerApiParallel.GetDBBlockingSessions_20_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (blockedSessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionsJSON, FilePathMap.DBBlockingSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_5)
                                        {
                                            blockedSessionsJSON = controllerApiParallel.GetDBBlockingSessions_4_5(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (blockedSessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionsJSON, FilePathMap.DBBlockingSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }
                                        else if (versionThisController >= version4_4)
                                        {
                                            blockedSessionsJSON = controllerApiParallel.GetDBBlockingSessions_4_4(jobTarget.DBCollectorID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (blockedSessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionsJSON, FilePathMap.DBBlockingSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                        }

                                        if (blockedSessionsJSON != String.Empty)
                                        {
                                            JArray blockedSessionsArray = JArray.Parse(blockedSessionsJSON);
                                            if (blockedSessionsArray != null && blockedSessionsArray.Count > 0)
                                            {
                                                loggerConsole.Info("Blocked Sessions Detail ({0} sessions)", blockedSessionsArray.Count);

                                                foreach (JToken blockedSessionToken in blockedSessionsArray)
                                                {
                                                    try
                                                    {
                                                        long blockingSessionID = (long)blockedSessionToken["blockingSessionId"];

                                                        if (versionThisController >= version20_4)
                                                        {
                                                            string blockedSessionJSON = controllerApiParallel.GetDBBlockingSession_20_4(jobTarget.DBCollectorID, blockingSessionID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                            if (blockedSessionJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionJSON, FilePathMap.DBBlockingSessionDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, blockingSessionID));
                                                        }
                                                        else if (versionThisController >= version4_5)
                                                        {
                                                            string blockedSessionJSON = controllerApiParallel.GetDBBlockingSession_4_5(jobTarget.DBCollectorID, blockingSessionID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                            if (blockedSessionJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionJSON, FilePathMap.DBBlockingSessionDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, blockingSessionID));
                                                        }
                                                        else if (versionThisController >= version4_4)
                                                        {
                                                            string blockedSessionJSON = controllerApiParallel.GetDBBlockingSession_4_4(jobTarget.DBCollectorID, blockingSessionID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                            if (blockedSessionJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionJSON, FilePathMap.DBBlockingSessionDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, blockingSessionID));
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                        }
                                    }
                                }
                            );
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);

                        return false;
                    }
                    finally
                    {
                        stopWatchTarget.Stop();

                        this.DisplayJobTargetEndedStatus(jobConfiguration, jobTarget, i + 1, stopWatchTarget);

                        stepTimingTarget.EndTime = DateTime.Now;
                        stepTimingTarget.Duration = stopWatchTarget.Elapsed;
                        stepTimingTarget.DurationMS = stopWatchTarget.ElapsedMilliseconds;

                        List<StepTiming> stepTimings = new List<StepTiming>(1);
                        stepTimings.Add(stepTimingTarget);
                        FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                loggerConsole.Error(ex);

                return false;
            }
            finally
            {
                stopWatch.Stop();

                this.DisplayJobStepEndedStatus(jobConfiguration, stopWatch);

                stepTimingFunction.EndTime = DateTime.Now;
                stepTimingFunction.Duration = stopWatch.Elapsed;
                stepTimingFunction.DurationMS = stopWatch.ElapsedMilliseconds;

                List<StepTiming> stepTimings = new List<StepTiming>(1);
                stepTimings.Add(stepTimingFunction);
                FileIOHelper.WriteListToCSVFile(stepTimings, new StepTimingReportMap(), FilePathMap.StepTimingReportFilePath(), true);
            }
        }

        public override bool ShouldExecute(ProgramOptions programOptions, JobConfiguration jobConfiguration)
        {
            logger.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            loggerConsole.Trace("LicensedReports.DetectedEntities={0}", programOptions.LicensedReports.DetectedEntities);
            if (programOptions.LicensedReports.DetectedEntities == false)
            {
                loggerConsole.Warn("Not licensed for detected entities");
                return false;
            }

            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping export of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
        }
    }
}
