using AppDynamics.Dexter.DataObjects;
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
    public class ExtractAPMEntities : JobStepBase
    {
        private const int NODE_PROPERTIES_EXTRACT_NUMBER_OF_THREADS = 5;
        private const int BACKEND_PROPERTIES_EXTRACT_NUMBER_OF_THREADS = 5;
        private const int ENTITIES_EXTRACT_NUMBER_OF_NODES_TO_PROCESS_PER_THREAD = 10;
        private const int ENTITIES_EXTRACT_NUMBER_OF_BACKENDS_TO_PROCESS_PER_THREAD = 10;

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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_APM) == 0)
                {
                    return true;
                }

                // Process each target
                for (int i = 0; i < jobConfiguration.Target.Count; i++)
                {
                    Stopwatch stopWatchTarget = new Stopwatch();
                    stopWatchTarget.Start();

                    JobTarget jobTarget = jobConfiguration.Target[i];

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

                    StepTiming stepTimingTarget = new StepTiming();
                    stepTimingTarget.Controller = jobTarget.Controller;
                    stepTimingTarget.ApplicationName = jobTarget.Application;
                    stepTimingTarget.ApplicationID = jobTarget.ApplicationID;
                    stepTimingTarget.JobFileName = programOptions.OutputJobFilePath;
                    stepTimingTarget.StepName = jobConfiguration.Status.ToString();
                    stepTimingTarget.StepID = (int)jobConfiguration.Status;
                    stepTimingTarget.StartTime = DateTime.Now;

                    stepTimingTarget.NumEntities = 9;

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.HourlyTimeRanges[jobConfiguration.Input.HourlyTimeRanges.Count - 1].From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.HourlyTimeRanges[jobConfiguration.Input.HourlyTimeRanges.Count - 1].To);
                        long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);

                        #endregion

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            #region Application

                            loggerConsole.Info("Application Name");

                            string applicationJSON = controllerApi.GetAPMApplication(jobTarget.ApplicationID);
                            if (applicationJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationJSON, FilePathMap.APMApplicationDataFilePath(jobTarget));

                            #endregion

                            #region Tiers

                            loggerConsole.Info("List of Tiers");

                            string tiersJSON = controllerApi.GetAPMTiers(jobTarget.ApplicationID);
                            if (tiersJSON != String.Empty) FileIOHelper.SaveFileToPath(tiersJSON, FilePathMap.APMTiersDataFilePath(jobTarget));

                            #endregion

                            #region Nodes

                            loggerConsole.Info("List of Nodes");

                            string nodesJSON = controllerApi.GetAPMNodes(jobTarget.ApplicationID);
                            if (nodesJSON != String.Empty) FileIOHelper.SaveFileToPath(nodesJSON, FilePathMap.APMNodesDataFilePath(jobTarget));

                            #endregion

                            #region Backends

                            loggerConsole.Info("List of Backends");

                            string backendsJSON = controllerApi.GetAPMBackends(jobTarget.ApplicationID);
                            if (backendsJSON != String.Empty) FileIOHelper.SaveFileToPath(backendsJSON, FilePathMap.APMBackendsDataFilePath(jobTarget));

                            controllerApi.PrivateApiLogin();
                            backendsJSON = controllerApi.GetAPMBackendsAdditionalDetail(jobTarget.ApplicationID);
                            if (backendsJSON != String.Empty) FileIOHelper.SaveFileToPath(backendsJSON, FilePathMap.APMBackendsDetailDataFilePath(jobTarget));

                            List<AppDRESTBackend> backendsList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBackend>(FilePathMap.APMBackendsDataFilePath(jobTarget));
                            if (backendsList != null)
                            {
                                loggerConsole.Info("DBMon Mappings for Backends ({0} entities)", backendsList.Count);

                                int j = 0;

                                var listOfBackendsInHourChunks = backendsList.BreakListIntoChunks(ENTITIES_EXTRACT_NUMBER_OF_BACKENDS_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTBackend>, int>(
                                    listOfBackendsInHourChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = BACKEND_PROPERTIES_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (listOfBackendsInHourChunk, loop, subtotal) =>
                                    {
                                    // Set up controller access
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));
                                    // Login into private API
                                    controllerApiParallel.PrivateApiLogin();

                                        foreach (AppDRESTBackend backend in listOfBackendsInHourChunk)
                                        {
                                            if (File.Exists(FilePathMap.APMBackendToDBMonMappingDataFilePath(jobTarget, backend)) == false)
                                            {
                                                string backendToDBMonMappingJSON = controllerApi.GetAPMBackendToDBMonMapping(backend.id);
                                                if (backendToDBMonMappingJSON != String.Empty) FileIOHelper.SaveFileToPath(backendToDBMonMappingJSON, FilePathMap.APMBackendToDBMonMappingDataFilePath(jobTarget, backend));
                                            }
                                        }

                                        return listOfBackendsInHourChunk.Count;
                                    },
                                    (finalResult) =>
                                    {
                                        Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );

                                loggerConsole.Info("Completed {0} Backends", backendsList.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + backendsList.Count;
                            }

                            #endregion

                            #region Business Transactions

                            loggerConsole.Info("List of Business Transactions");

                            string businessTransactionsJSON = controllerApi.GetAPMBusinessTransactions(jobTarget.ApplicationID);
                            if (businessTransactionsJSON != String.Empty) FileIOHelper.SaveFileToPath(businessTransactionsJSON, FilePathMap.APMBusinessTransactionsDataFilePath(jobTarget));

                            #endregion

                            #region Service Endpoints

                            loggerConsole.Info("List of Service Endpoints");

                            string serviceEndPointsJSON = controllerApi.GetAPMServiceEndpoints(jobTarget.ApplicationID);
                            if (serviceEndPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(serviceEndPointsJSON, FilePathMap.APMServiceEndpointsDataFilePath(jobTarget));

                            controllerApi.PrivateApiLogin();
                            serviceEndPointsJSON = controllerApi.GetAPMServiceEndpointsAdditionalDetail(jobTarget.ApplicationID);
                            if (serviceEndPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(serviceEndPointsJSON, FilePathMap.APMServiceEndpointsDetailDataFilePath(jobTarget));

                            #endregion

                            #region Errors

                            loggerConsole.Info("List of Errors");

                            string errorsJSON = controllerApi.GetAPMErrors(jobTarget.ApplicationID);
                            if (errorsJSON != String.Empty) FileIOHelper.SaveFileToPath(errorsJSON, FilePathMap.APMErrorsDataFilePath(jobTarget));

                            #endregion

                            #region Information Points

                            loggerConsole.Info("List of Information Points");

                            string informationPointsJSON = controllerApi.GetAPMInformationPoints(jobTarget.ApplicationID);
                            if (informationPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(informationPointsJSON, FilePathMap.APMInformationPointsDataFilePath(jobTarget));

                            controllerApi.PrivateApiLogin();
                            string informationPointsDetailJSON = controllerApi.GetAPMInformationPointsAdditionalDetail(jobTarget.ApplicationID);
                            if (informationPointsDetailJSON != String.Empty) FileIOHelper.SaveFileToPath(informationPointsDetailJSON, FilePathMap.APMInformationPointsDetailDataFilePath(jobTarget));

                            #endregion

                            #region Node Properties

                            List<AppDRESTNode> nodesList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTNode>(FilePathMap.APMNodesDataFilePath(jobTarget));
                            if (nodesList != null)
                            {
                                loggerConsole.Info("Node Properties for Nodes ({0} entities)", nodesList.Count);

                                int j = 0;

                                var listOfNodesInHourChunks = nodesList.BreakListIntoChunks(ENTITIES_EXTRACT_NUMBER_OF_NODES_TO_PROCESS_PER_THREAD);

                                Parallel.ForEach<List<AppDRESTNode>, int>(
                                    listOfNodesInHourChunks,
                                    new ParallelOptions { MaxDegreeOfParallelism = NODE_PROPERTIES_EXTRACT_NUMBER_OF_THREADS },
                                    () => 0,
                                    (listOfNodesInHourChunk, loop, subtotal) =>
                                    {
                                    // Set up controller access
                                    ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword));

                                    // Login into private API
                                    controllerApiParallel.PrivateApiLogin();

                                        foreach (AppDRESTNode node in listOfNodesInHourChunk)
                                        {
                                            if (File.Exists(FilePathMap.APMNodeRuntimePropertiesDataFilePath(jobTarget, node)) == false)
                                            {
                                                string nodePropertiesJSON = controllerApi.GetAPMNodeProperties(node.id);
                                                if (nodePropertiesJSON != String.Empty) FileIOHelper.SaveFileToPath(nodePropertiesJSON, FilePathMap.APMNodeRuntimePropertiesDataFilePath(jobTarget, node));
                                            }
                                            if (File.Exists(FilePathMap.APMNodeMetadataDataFilePath(jobTarget, node)) == false)
                                            {
                                                string nodeMetadataJSON = controllerApi.GetAPMNodeMetadata(jobTarget.ApplicationID, node.id);
                                                if (nodeMetadataJSON != String.Empty) FileIOHelper.SaveFileToPath(nodeMetadataJSON, FilePathMap.APMNodeMetadataDataFilePath(jobTarget, node));
                                            }
                                        }

                                        return listOfNodesInHourChunk.Count;
                                    },
                                    (finalResult) =>
                                    {
                                        Interlocked.Add(ref j, finalResult);
                                        Console.Write("[{0}].", j);
                                    }
                                );

                                loggerConsole.Info("Completed {0} Nodes", nodesList.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + nodesList.Count;
                            }

                            #endregion

                            #region Backend to Tier Mappings

                            List<AppDRESTTier> tiersRESTList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTTier>(FilePathMap.APMTiersDataFilePath(jobTarget));
                            if (tiersRESTList != null)
                            {
                                loggerConsole.Info("Backend to Tier Mappings ({0} entities)", tiersRESTList.Count);

                                int j = 0;

                                foreach (AppDRESTTier tier in tiersRESTList)
                                {
                                    string backendMappingsJSON = controllerApi.GetAPMBackendToTierMapping(tier.id);
                                    if (backendMappingsJSON != String.Empty && backendMappingsJSON != "[ ]") FileIOHelper.SaveFileToPath(backendMappingsJSON, FilePathMap.APMBackendToTierMappingDataFilePath(jobTarget, tier));

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    j++;
                                }

                                loggerConsole.Info("Completed {0} Tiers", tiersRESTList.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + tiersRESTList.Count;
                            }

                            #endregion

                            #region Overflow Business Transactions

                            if (tiersRESTList != null)
                            {
                                loggerConsole.Info("Contents of Overflow Business Transaction in Tiers ({0} entities)", tiersRESTList.Count);

                                int j = 0;

                                foreach (AppDRESTTier tier in tiersRESTList)
                                {
                                    JArray droppedBTsArray = new JArray();
                                    JArray droppedBTsDebugModeArray = new JArray();

                                    bool noMoreBTs = false;
                                    long currentFetchedEventCount = 0;
                                    long endEventID = 0;
                                    while (noMoreBTs == false)
                                    {
                                        string batchOfBTsJSON = controllerApi.GetAPMBusinessTransactionsInOverflow(tier.id, currentFetchedEventCount, endEventID, fromTimeUnix, toTimeUnix, differenceInMinutes);

                                        if (batchOfBTsJSON != String.Empty)
                                        {
                                            JObject batchOfBTsContainer = JObject.Parse(batchOfBTsJSON);
                                            if (batchOfBTsContainer != null)
                                            {
                                                // Copy out both of the containers, not sure why there are multiple
                                                if (isTokenPropertyNull(batchOfBTsContainer, "droppedTransactionItemList") == false)
                                                {
                                                    foreach (JObject btObject in batchOfBTsContainer["droppedTransactionItemList"])
                                                    {
                                                        droppedBTsArray.Add(btObject);
                                                    }
                                                }
                                                if (isTokenPropertyNull(batchOfBTsContainer, "debugModeDroppedTransactionItemList") == false)
                                                {
                                                    foreach (JObject btObject in batchOfBTsContainer["debugModeDroppedTransactionItemList"])
                                                    {
                                                        droppedBTsDebugModeArray.Add(btObject);
                                                    }
                                                }

                                                currentFetchedEventCount = getLongValueFromJToken(batchOfBTsContainer, "eventSummariesCount");
                                                endEventID = getLongValueFromJToken(batchOfBTsContainer, "endEventId");

                                                if (currentFetchedEventCount == 0 || endEventID == 0)
                                                {
                                                    // Done getting batches
                                                    noMoreBTs = true;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            noMoreBTs = true;
                                        }
                                    }

                                    if (droppedBTsArray.Count > 0) FileIOHelper.SaveFileToPath(droppedBTsArray.ToString(), FilePathMap.APMTierOverflowBusinessTransactionRegularDataFilePath(jobTarget, tier));
                                    if (droppedBTsDebugModeArray.Count > 0) FileIOHelper.SaveFileToPath(droppedBTsDebugModeArray.ToString(), FilePathMap.APMTierOverflowBusinessTransactionDebugDataFilePath(jobTarget, tier));

                                    if (j % 10 == 0)
                                    {
                                        Console.Write("[{0}].", j);
                                    }
                                    j++;
                                }

                                loggerConsole.Info("Completed {0} Tiers", tiersRESTList.Count);
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

        public override bool ShouldExecute(JobConfiguration jobConfiguration)
        {
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
