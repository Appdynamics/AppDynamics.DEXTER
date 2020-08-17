using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractSIMEntities : JobStepBase
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
                if (this.ShouldExecute(programOptions, jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_SIM) == 0)
                {
                    logger.Warn("No {0} targets to process", APPLICATION_TYPE_SIM);
                    loggerConsole.Warn("No {0} targets to process", APPLICATION_TYPE_SIM);

                    return true;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_SIM) continue;

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

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            #region Prepare time range

                            long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                            long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                            long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                            #endregion

                            #region Tiers

                            loggerConsole.Info("List of Tiers");

                            string tiersJSON = controllerApi.GetSIMListOfTiers();
                            if (tiersJSON != String.Empty) FileIOHelper.SaveFileToPath(tiersJSON, FilePathMap.SIMTiersDataFilePath(jobTarget));

                            #endregion

                            #region Nodes

                            loggerConsole.Info("List of Nodes");

                            string nodesJSON = controllerApi.GetSIMListOfNodes();
                            if (nodesJSON != String.Empty) FileIOHelper.SaveFileToPath(nodesJSON, FilePathMap.SIMNodesDataFilePath(jobTarget));

                            #endregion

                            #region Groups

                            loggerConsole.Info("List of Groups");

                            string groupsJSON = controllerApi.GetSIMListOfGroups();
                            if (groupsJSON != String.Empty) FileIOHelper.SaveFileToPath(groupsJSON, FilePathMap.SIMGroupsDataFilePath(jobTarget));

                            #endregion

                            #region Service Availability

                            loggerConsole.Info("List of Service Availability");

                            string sasJSON = controllerApi.GetSIMListOfServiceAvailability();
                            if (sasJSON != String.Empty) FileIOHelper.SaveFileToPath(sasJSON, FilePathMap.SIMServiceAvailabilitiesDataFilePath(jobTarget));

                            JArray sasList = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMServiceAvailabilitiesDataFilePath(jobTarget));
                            if (sasList != null)
                            {
                                loggerConsole.Info("Service Availability Details ({0} entities)", sasList.Count);

                                foreach (JToken saToken in sasList)
                                {
                                    string saJSON = controllerApi.GetSIMServiceAvailability(getLongValueFromJToken(saToken, "id"));
                                    if (saJSON != String.Empty) FileIOHelper.SaveFileToPath(saJSON, FilePathMap.SIMServiceAvailabilityDataFilePath(jobTarget, getStringValueFromJToken(saToken, "name"), getLongValueFromJToken(saToken, "id")));

                                    string saEventsJSON = controllerApi.GetSIMServiceAvailabilityEvents(getLongValueFromJToken(saToken, "id"), fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (saEventsJSON != String.Empty && saEventsJSON != "[]") FileIOHelper.SaveFileToPath(saEventsJSON, FilePathMap.SIMServiceAvailabilityEventsDataFilePath(jobTarget, getStringValueFromJToken(saToken, "name"), getLongValueFromJToken(saToken, "id"), jobConfiguration.Input.TimeRange));
                                }

                                loggerConsole.Info("Completed {0} Service Availabilities", sasList.Count);
                            }

                            #endregion

                            #region Machines

                            loggerConsole.Info("List of Machines");

                            string machinesJSON = controllerApi.GetSIMListOfMachines();
                            if (machinesJSON != String.Empty) FileIOHelper.SaveFileToPath(machinesJSON, FilePathMap.SIMMachinesDataFilePath(jobTarget));

                             JArray machinesArray = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachinesDataFilePath(jobTarget));
                            if (machinesArray != null)
                            {
                                loggerConsole.Info("Machine, Container Details and Processes ({0} entities)", machinesArray.Count);

                                int j = 0;

                                var listOfMachinesChunks = machinesArray.BreakListIntoChunks(ENTITIES_EXTRACT_NUMBER_OF_MACHINES_TO_PROCESS_PER_THREAD);

                                ParallelOptions parallelOptions = new ParallelOptions();
                                if (programOptions.ProcessSequentially == true)
                                {
                                    parallelOptions.MaxDegreeOfParallelism = 1;
                                }
                                else
                                {
                                    parallelOptions.MaxDegreeOfParallelism = MACHINES_EXTRACT_NUMBER_OF_THREADS;
                                }

                                Parallel.ForEach<List<JToken>, int>(
                                    listOfMachinesChunks,
                                    parallelOptions,
                                    () => 0,
                                    (listOfMachinesChunk, loop, subtotal) =>
                                    {
                                        // Set up controller access
                                        using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                        {

                                            foreach (JToken machineToken in listOfMachinesChunk)
                                            {
                                                if (File.Exists(FilePathMap.SIMMachineDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id"))) == false)
                                                {
                                                    string machineJSON = controllerApi.GetSIMMachine(getLongValueFromJToken(machineToken, "id"));
                                                    if (machineJSON != String.Empty) FileIOHelper.SaveFileToPath(machineJSON, FilePathMap.SIMMachineDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id")));

                                                    string machineDockerJSON = controllerApi.GetSIMMachineDockerContainers(getLongValueFromJToken(machineToken, "id"));
                                                    if (machineDockerJSON != String.Empty && machineDockerJSON != "[]") FileIOHelper.SaveFileToPath(machineDockerJSON, FilePathMap.SIMMachineDockerContainersDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id")));

                                                    string machineProcessesJSON = controllerApi.GetSIMMachineProcesses((long)machineToken["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                    if (machineProcessesJSON != String.Empty && machineProcessesJSON != "[]") FileIOHelper.SaveFileToPath(machineProcessesJSON, FilePathMap.SIMMachineProcessesDataFilePath(jobTarget, getStringValueFromJToken(machineToken, "name"), getLongValueFromJToken(machineToken, "id"), jobConfiguration.Input.TimeRange));
                                                }
                                            }

                                            return listOfMachinesChunk.Count;
                                        }
                                    },
                                    (finalResult) =>
                                    {
                                        Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );

                                loggerConsole.Info("Completed {0} Machines", machinesArray.Count);
                            }

                            #endregion
                        }
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
