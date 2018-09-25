using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractControllerDBApplicationsAndEntities : JobStepBase
    {
        private const int MACHINES_EXTRACT_NUMBER_OF_THREADS = 10;
        private const int ENTITIES_EXTRACT_NUMBER_OF_MACHINES_TO_PROCESS_PER_THREAD = 10;

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
                if (this.ShouldExecute(jobConfiguration) == false)
                {
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

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        controllerApi.PrivateApiLogin();

                        #endregion

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                        #endregion

                        #region Collector definitions

                        // Only collect once per Controller since there is only one Database Application
                        if (File.Exists(FilePathMap.DBCollectorDefinitionsDataFilePath(jobTarget)) == false)
                        {
                            loggerConsole.Info("List of Collector Definitions");

                            string collectorDefinitionsJSON = controllerApi.GetDBCollectorsConfiguration();
                            if (collectorDefinitionsJSON != String.Empty) FileIOHelper.SaveFileToPath(collectorDefinitionsJSON, FilePathMap.DBCollectorDefinitionsDataFilePath(jobTarget));
                        }

                        #endregion

                        #region Collectors

                        // Only collect once per Controller since there is only one Database Application
                        if (File.Exists(FilePathMap.DBCollectorsCallsDataFilePath(jobTarget)) == false)
                        {
                            loggerConsole.Info("List of Collectors - Calls");

                            string collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls45(fromTimeUnix, toTimeUnix);
                            if (collectorsJSON == String.Empty) collectorsJSON = controllerApi.GetDBRegisteredCollectorsCalls44(fromTimeUnix, toTimeUnix);

                            if (collectorsJSON != String.Empty)
                            {
                                FileIOHelper.SaveFileToPath(collectorsJSON, FilePathMap.DBCollectorsCallsDataFilePath(jobTarget));
                            }

                            loggerConsole.Info("List of Collectors - Time Taken");

                            collectorsJSON = String.Empty;
                            collectorsJSON = controllerApi.GetDBRegisteredCollectorsTimeSpent45(fromTimeUnix, toTimeUnix);
                            if (collectorsJSON == String.Empty) collectorsJSON = controllerApi.GetDBRegisteredCollectorsTimeSpent44(fromTimeUnix, toTimeUnix);

                            if (collectorsJSON != String.Empty)
                            {
                                FileIOHelper.SaveFileToPath(collectorsJSON, FilePathMap.DBCollectorsTimeSpentDataFilePath(jobTarget));
                            }
                        }

                        #endregion

                        #region Everything else

                        // Check the file existence to support resuming previously interrupted jobs
                        if (File.Exists(FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange)) == false)
                        {
                            Parallel.Invoke(
                                () =>
                                {
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                    controllerApiParallel.PrivateApiLogin();

                                    loggerConsole.Info("All Wait States");
                                    string allWaitStatesJSON = controllerApiParallel.GetDBAllWaitStates(jobTarget.ApplicationID);
                                    if (allWaitStatesJSON != String.Empty) FileIOHelper.SaveFileToPath(allWaitStatesJSON, FilePathMap.DBAllWaitStatesDataFilePath(jobTarget));

                                    loggerConsole.Info("Current Wait States");
                                    string currentWaitStatesJSON = controllerApiParallel.GetDCurrentWaitStates(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (currentWaitStatesJSON != String.Empty) FileIOHelper.SaveFileToPath(currentWaitStatesJSON, FilePathMap.DBCurrentWaitStatesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                },
                                () =>
                                {
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                    controllerApiParallel.PrivateApiLogin();

                                    loggerConsole.Info("Queries");
                                    string queriesJSON = controllerApiParallel.GetDBQueries(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (queriesJSON != String.Empty) FileIOHelper.SaveFileToPath(queriesJSON, FilePathMap.DBQueriesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                                    loggerConsole.Info("Clients");
                                    string clientsJSON = controllerApiParallel.GetDBClients(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (clientsJSON != String.Empty) FileIOHelper.SaveFileToPath(clientsJSON, FilePathMap.DBClientsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                },
                                () =>
                                {
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                    controllerApiParallel.PrivateApiLogin();

                                    loggerConsole.Info("Sessions");
                                    string sessionsJSON = controllerApiParallel.GetDBSessions(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (sessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(sessionsJSON, FilePathMap.DBSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                                    loggerConsole.Info("Databases");
                                    string databasesJSON = controllerApiParallel.GetDBDatabases(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (databasesJSON != String.Empty) FileIOHelper.SaveFileToPath(databasesJSON, FilePathMap.DBDatabasesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                                    loggerConsole.Info("Users");
                                    string usersJSON = controllerApiParallel.GetDBUsers(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (usersJSON != String.Empty) FileIOHelper.SaveFileToPath(usersJSON, FilePathMap.DBUsersDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                },
                                () =>
                                {
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                    controllerApiParallel.PrivateApiLogin();

                                    loggerConsole.Info("Modules");
                                    string modulesJSON = controllerApiParallel.GetDBModules(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (modulesJSON != String.Empty) FileIOHelper.SaveFileToPath(modulesJSON, FilePathMap.DBModulesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                                    loggerConsole.Info("Programs");
                                    string programsJSON = controllerApiParallel.GetDBPrograms(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (programsJSON != String.Empty) FileIOHelper.SaveFileToPath(programsJSON, FilePathMap.DBProgramsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                                    loggerConsole.Info("Business Transactions");
                                    string businessTransactionsJSON = controllerApiParallel.GetDBBusinessTransactions(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix);
                                    if (businessTransactionsJSON != String.Empty) FileIOHelper.SaveFileToPath(businessTransactionsJSON, FilePathMap.DBBusinessTransactionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                },
                                () =>
                                {
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                    controllerApiParallel.PrivateApiLogin();

                                    loggerConsole.Info("All Blocked Sessions");
                                    string blockedSessionsJSON = controllerApiParallel.GetDBBlockingSessions(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (blockedSessionsJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionsJSON, FilePathMap.DBBlockingSessionsDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));

                                    if (blockedSessionsJSON != String.Empty)
                                    {
                                        JArray blockedSessionsList = JArray.Parse(blockedSessionsJSON);
                                        if (blockedSessionsList != null && blockedSessionsList.Count > 0)
                                        {
                                            loggerConsole.Info("Blocked Sessions Detail ({0} sessions)", blockedSessionsList.Count);

                                            foreach (JToken blockedSessionToken in blockedSessionsList)
                                            {
                                                try
                                                {
                                                    long blockingSessionID = (long)blockedSessionToken["blockingSessionId"];

                                                    string blockedSessionJSON = controllerApiParallel.GetDBBlockingSession(jobTarget.ApplicationID, blockingSessionID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                    if (blockedSessionJSON != String.Empty) FileIOHelper.SaveFileToPath(blockedSessionJSON, FilePathMap.DBBlockingSessionDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, blockingSessionID));
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                            );
                        };

                        #endregion

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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
            logger.Trace("{0} is always executed", jobConfiguration.Status);
            loggerConsole.Trace("{0} is always executed", jobConfiguration.Status);
            return true;
        }
    }
}
