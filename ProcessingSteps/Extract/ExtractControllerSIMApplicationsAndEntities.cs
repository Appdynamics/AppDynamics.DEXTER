using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractControllerSIMApplicationsAndEntities : JobStepBase
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

                        #region Target step variables

                        // Set up controller access
                        ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                        #endregion

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

                            foreach (JToken sa in sasList)
                            {
                                string saJSON = controllerApi.GetSIMServiceAvailability((long)sa["id"]);
                                if (saJSON != String.Empty) FileIOHelper.SaveFileToPath(saJSON, FilePathMap.SIMServiceAvailabilityDataFilePath(jobTarget, sa["name"].ToString(), (long)sa["id"]));

                                string saEventsJSON = controllerApi.GetSIMServiceAvailabilityEvents((long)sa["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                if (saEventsJSON != String.Empty && saEventsJSON != "[]") FileIOHelper.SaveFileToPath(saEventsJSON, FilePathMap.SIMServiceAvailabilityEventsDataFilePath(jobTarget, sa["name"].ToString(), (long)sa["id"], jobConfiguration.Input.TimeRange));
                            }

                            loggerConsole.Info("Completed {0} Service Availabilities", sasList.Count);
                        }

                        #endregion

                        #region Machines

                        loggerConsole.Info("List of Machines");

                        string machinesJSON = controllerApi.GetSIMListOfMachines();
                        if (machinesJSON != String.Empty) FileIOHelper.SaveFileToPath(machinesJSON, FilePathMap.SIMMachinesDataFilePath(jobTarget));

                        JArray machinesList = FileIOHelper.LoadJArrayFromFile(FilePathMap.SIMMachinesDataFilePath(jobTarget));
                        if (machinesList != null)
                        {
                            loggerConsole.Info("Machine, Container Details and Processes ({0} entities)", machinesList.Count);

                            int j = 0;

                            var listOfMachinesChunks = machinesList.BreakListIntoChunks(ENTITIES_EXTRACT_NUMBER_OF_MACHINES_TO_PROCESS_PER_THREAD);

                            Parallel.ForEach<List<JToken>, int>(
                                listOfMachinesChunks,
                                new ParallelOptions { MaxDegreeOfParallelism = MACHINES_EXTRACT_NUMBER_OF_THREADS},
                                () => 0,
                                (listOfMachinesChunk, loop, subtotal) =>
                                {
                                    // Set up controller access
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                                    foreach (JToken machine in listOfMachinesChunk)
                                    {
                                        string machineJSON = controllerApi.GetSIMMachine((long)machine["id"]);
                                        if (machineJSON != String.Empty) FileIOHelper.SaveFileToPath(machineJSON, FilePathMap.SIMMachineDataFilePath(jobTarget, machine["name"].ToString(), (long)machine["id"]));

                                        string machineDockerJSON = controllerApi.GetSIMMachineDockerContainers((long)machine["id"]);
                                        if (machineDockerJSON != String.Empty && machineDockerJSON != "[]") FileIOHelper.SaveFileToPath(machineDockerJSON, FilePathMap.SIMMachineDockerContainersDataFilePath(jobTarget, machine["name"].ToString(), (long)machine["id"]));

                                        string machineProcessesJSON = controllerApi.GetSIMMachineProcesses((long)machine["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                        if (machineProcessesJSON != String.Empty && machineProcessesJSON != "[]") FileIOHelper.SaveFileToPath(machineProcessesJSON, FilePathMap.SIMMachineProcessesDataFilePath(jobTarget, machine["name"].ToString(), (long)machine["id"], jobConfiguration.Input.TimeRange));
                                    }

                                    return listOfMachinesChunk.Count;
                                },
                                (finalResult) =>
                                {
                                    Interlocked.Add(ref j, finalResult);
                                    Console.Write("[{0}].", j);
                                }
                            );

                            loggerConsole.Info("Completed {0} Machines", machinesList.Count);
                        }

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
