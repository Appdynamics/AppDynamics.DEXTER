using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractApplicationAndEntityFlowmaps : JobStepBase
    {
        private const int FLOWMAP_EXTRACT_NUMBER_OF_THREADS = 3;

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

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Target state check

                        if (jobTarget.Status != JobTargetStatus.ConfigurationValid)
                        {
                            loggerConsole.Trace("Target in invalid state {0}, skipping", jobTarget.Status);

                            continue;
                        }

                        #endregion

                        #region Target step variables

                        int numEntitiesTotal = 0;

                        #endregion

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                        #endregion

                        Parallel.Invoke(
                            () =>
                            {
                                #region Application

                                loggerConsole.Info("Extract Flowmap for Application");

                                ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                controllerApi.PrivateApiLogin();

                                if (File.Exists(FilePathMap.ApplicationFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange)) == false)
                                {
                                    string flowmapJson = controllerApi.GetFlowmapApplication(jobTarget.ApplicationID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                    if (flowmapJson != String.Empty) FileIOHelper.SaveFileToPath(flowmapJson, FilePathMap.ApplicationFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange));
                                }

                                loggerConsole.Info("Completed Application");

                                loggerConsole.Info("Extract Flowmap for Application in each minute in ({0} minutes)", differenceInMinutes);

                                int j = 0;

                                Parallel.For(0, 
                                    differenceInMinutes, 
                                    new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (minute, loop, subtotal) =>
                                    {
                                        ControllerApi controllerApiLocal = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                        controllerApiLocal.PrivateApiLogin();

                                        JobTimeRange thisMinuteJobTimeRange = new JobTimeRange();
                                        thisMinuteJobTimeRange.From = jobConfiguration.Input.TimeRange.From.AddMinutes(minute);
                                        thisMinuteJobTimeRange.To = jobConfiguration.Input.TimeRange.From.AddMinutes(minute + 1);

                                        long fromTimeUnixLocal = UnixTimeHelper.ConvertToUnixTimestamp(thisMinuteJobTimeRange.From);
                                        long toTimeUnixLocal = UnixTimeHelper.ConvertToUnixTimestamp(thisMinuteJobTimeRange.To);
                                        long differenceInMinutesLocal = 1;

                                        if (File.Exists(FilePathMap.ApplicationFlowmapDataFilePath(jobTarget, thisMinuteJobTimeRange)) == false)
                                        {
                                            string flowmapJson = controllerApiLocal.GetFlowmapApplication(jobTarget.ApplicationID, fromTimeUnixLocal, toTimeUnixLocal, differenceInMinutesLocal);
                                            if (flowmapJson != String.Empty) FileIOHelper.SaveFileToPath(flowmapJson, FilePathMap.ApplicationFlowmapDataFilePath(jobTarget, thisMinuteJobTimeRange));
                                        }
                                        return 1;
                                    },
                                    (finalResult) =>
                                    {
                                        Interlocked.Add(ref j, finalResult);
                                        if (j % 10 == 0)
                                        {
                                            Console.Write("[{0}].", j);
                                        }
                                    }
                                );

                                loggerConsole.Info("Completed Application {0} timeranges", differenceInMinutes);

                                Interlocked.Add(ref numEntitiesTotal, 1);

                                #endregion
                            },
                            () =>
                            {
                                #region Tiers

                                List<AppDRESTTier> tiersList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTTier>(FilePathMap.TiersDataFilePath(jobTarget));
                                if (tiersList != null)
                                {
                                    loggerConsole.Info("Extract Flowmaps for Tiers ({0} entities)", tiersList.Count);

                                    int j = 0;

                                    Parallel.ForEach(
                                        tiersList,
                                        new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                        () => 0,
                                        (tier, loop, subtotal) =>
                                        {
                                            ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                            controllerApi.PrivateApiLogin();

                                            if (File.Exists(FilePathMap.TierFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, tier)) == false)
                                            {
                                                string flowmapJson = controllerApi.GetFlowmapTier(tier.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                if (flowmapJson != String.Empty) FileIOHelper.SaveFileToPath(flowmapJson, FilePathMap.TierFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, tier));
                                            }
                                            return 1;
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref j, finalResult);
                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                        }
                                    );

                                    loggerConsole.Info("Completed {0} Tiers", tiersList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, tiersList.Count);
                                }
                                #endregion
                            },
                            () =>
                            {
                                #region Nodes

                                List<AppDRESTNode> nodesList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTNode>(FilePathMap.NodesDataFilePath(jobTarget));
                                if (nodesList != null)
                                {
                                    loggerConsole.Info("Extract Flowmaps for Nodes ({0} entities)", nodesList.Count);

                                    int j = 0;

                                    Parallel.ForEach(
                                        nodesList,
                                        new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                        () => 0,
                                        (node, loop, subtotal) =>
                                        {
                                            ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                            controllerApi.PrivateApiLogin();

                                            if (File.Exists(FilePathMap.NodeFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, node)) == false)
                                            {
                                                string flowmapJson = controllerApi.GetFlowmapNode(node.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                if (flowmapJson != String.Empty) FileIOHelper.SaveFileToPath(flowmapJson, FilePathMap.NodeFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, node));
                                            }
                                            return 1;
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref j, finalResult);
                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                        }
                                    );

                                    loggerConsole.Info("Completed {0} Nodes", nodesList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, nodesList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Backends

                                List<AppDRESTBackend> backendsList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBackend>(FilePathMap.BackendsDataFilePath(jobTarget));
                                if (backendsList != null)
                                {
                                    loggerConsole.Info("Extract Flowmaps for Backends ({0} entities)", backendsList.Count);

                                    int j = 0;

                                    Parallel.ForEach(
                                        backendsList,
                                        new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                        () => 0,
                                        (backend, loop, subtotal) =>
                                        {
                                            ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                            controllerApi.PrivateApiLogin();

                                            if (File.Exists(FilePathMap.BackendFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, backend)) == false)
                                            {
                                                string flowmapJson = controllerApi.GetFlowmapBackend(backend.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                if (flowmapJson != String.Empty) FileIOHelper.SaveFileToPath(flowmapJson, FilePathMap.BackendFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, backend));
                                            }
                                            return 1;
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref j, finalResult);
                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                        }
                                    );

                                    loggerConsole.Info("Completed {0} Backends", backendsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, backendsList.Count);
                                }

                                #endregion
                            },
                            () =>
                            {
                                #region Business Transactions

                                List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBusinessTransaction>(FilePathMap.BusinessTransactionsDataFilePath(jobTarget));
                                if (businessTransactionsList != null)
                                {
                                    loggerConsole.Info("Extract Flowmaps for Business Transactions ({0} entities)", businessTransactionsList.Count);

                                    int j = 0;

                                    Parallel.ForEach(
                                        businessTransactionsList,
                                        new ParallelOptions { MaxDegreeOfParallelism = FLOWMAP_EXTRACT_NUMBER_OF_THREADS },
                                        () => 0,
                                        (businessTransaction, loop, subtotal) =>
                                        {
                                            ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                            controllerApi.PrivateApiLogin();

                                            if (File.Exists(FilePathMap.BusinessTransactionFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, businessTransaction)) == false)
                                            {
                                                string flowmapJson = controllerApi.GetFlowmapBusinessTransaction(jobTarget.ApplicationID, businessTransaction.id, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                if (flowmapJson != String.Empty) FileIOHelper.SaveFileToPath(flowmapJson, FilePathMap.BusinessTransactionFlowmapDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, businessTransaction));
                                            }
                                            return 1;
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref j, finalResult);
                                            if (j % 10 == 0)
                                            {
                                                Console.Write("[{0}].", j);
                                            }
                                        }
                                    );

                                    loggerConsole.Info("Completed {0} Business Transactions", businessTransactionsList.Count);

                                    Interlocked.Add(ref numEntitiesTotal, businessTransactionsList.Count);
                                }

                                #endregion
                            }
                        );

                        stepTimingTarget.NumEntities = numEntitiesTotal;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex);
                        loggerConsole.Warn(ex);
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
            logger.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            loggerConsole.Trace("Input.Flowmaps={0}", jobConfiguration.Input.Flowmaps);
            if (jobConfiguration.Input.Flowmaps == false)
            {
                loggerConsole.Trace("Skipping export of entity flowmaps");
            }
            return (jobConfiguration.Input.Flowmaps == true);
        }
    }
}
