using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractControllerApplicationsAndEntities : JobStepBase
    {
        private const int NODE_PROPERTIES_EXTRACT_NUMBER_OF_THREADS = 5;
        private const int ENTITIES_EXTRACT_NUMBER_OF_NODES_TO_PROCESS_PER_THREAD = 10;

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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_APM) continue;

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

                        #region Controller

                        if (File.Exists(FilePathMap.ControllerVersionDataFilePath(jobTarget)) != true)
                        {
                            loggerConsole.Info("Controller Version");

                            string controllerVersionXML = controllerApi.GetControllerVersion();
                            if (controllerVersionXML != String.Empty) FileIOHelper.SaveFileToPath(controllerVersionXML, FilePathMap.ControllerVersionDataFilePath(jobTarget));
                        }

                        #endregion

                        #region Applications

                        // Only do it once per controller, if processing multiple applications
                        if (File.Exists(FilePathMap.ApplicationsDataFilePath(jobTarget)) != true)
                        {
                            loggerConsole.Info("List of Applications");

                            string applicationsJSON = controllerApi.GetApplicationsAPM();
                            if (applicationsJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationsJSON, FilePathMap.ApplicationsDataFilePath(jobTarget));
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("This Application");

                        string applicationJSON = controllerApi.GetSingleApplicationAPM(jobTarget.ApplicationID);
                        if (applicationJSON != String.Empty) FileIOHelper.SaveFileToPath(applicationJSON, FilePathMap.ApplicationDataFilePath(jobTarget));

                        #endregion

                        #region Tiers

                        loggerConsole.Info("List of Tiers");

                        string tiersJSON = controllerApi.GetListOfTiers(jobTarget.ApplicationID);
                        if (tiersJSON != String.Empty) FileIOHelper.SaveFileToPath(tiersJSON, FilePathMap.TiersDataFilePath(jobTarget));

                        #endregion

                        #region Nodes

                        loggerConsole.Info("List of Nodes");

                        string nodesJSON = controllerApi.GetListOfNodes(jobTarget.ApplicationID);
                        if (nodesJSON != String.Empty) FileIOHelper.SaveFileToPath(nodesJSON, FilePathMap.NodesDataFilePath(jobTarget));

                        #endregion

                        #region Backends

                        loggerConsole.Info("List of Backends");

                        string backendsJSON = controllerApi.GetListOfBackends(jobTarget.ApplicationID);
                        if (backendsJSON != String.Empty) FileIOHelper.SaveFileToPath(backendsJSON, FilePathMap.BackendsDataFilePath(jobTarget));

                        controllerApi.PrivateApiLogin();
                        backendsJSON = controllerApi.GetListOfBackendsAdditionalDetail(jobTarget.ApplicationID);
                        if (backendsJSON != String.Empty) FileIOHelper.SaveFileToPath(backendsJSON, FilePathMap.BackendsDetailDataFilePath(jobTarget));

                        #endregion

                        #region Business Transactions

                        loggerConsole.Info("List of Business Transactions");

                        string businessTransactionsJSON = controllerApi.GetListOfBusinessTransactions(jobTarget.ApplicationID);
                        if (businessTransactionsJSON != String.Empty) FileIOHelper.SaveFileToPath(businessTransactionsJSON, FilePathMap.BusinessTransactionsDataFilePath(jobTarget));

                        #endregion

                        #region Service Endpoints

                        loggerConsole.Info("List of Service Endpoints");

                        string serviceEndPointsJSON = controllerApi.GetListOfServiceEndpoints(jobTarget.ApplicationID);
                        if (serviceEndPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(serviceEndPointsJSON, FilePathMap.ServiceEndpointsDataFilePath(jobTarget));

                        controllerApi.PrivateApiLogin();
                        serviceEndPointsJSON = controllerApi.GetListOfServiceEndpointsAdditionalDetail(jobTarget.ApplicationID);
                        if (serviceEndPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(serviceEndPointsJSON, FilePathMap.ServiceEndpointsDetailDataFilePath(jobTarget));

                        #endregion

                        #region Errors

                        loggerConsole.Info("List of Errors");

                        string errorsJSON = controllerApi.GetListOfErrors(jobTarget.ApplicationID);
                        if (errorsJSON != String.Empty) FileIOHelper.SaveFileToPath(errorsJSON, FilePathMap.ErrorsDataFilePath(jobTarget));

                        #endregion

                        #region Information Points

                        loggerConsole.Info("List of Information Points");

                        string informationPointsJSON = controllerApi.GetListOfInformationPoints(jobTarget.ApplicationID);
                        if (informationPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(informationPointsJSON, FilePathMap.InformationPointsDataFilePath(jobTarget));

                        controllerApi.PrivateApiLogin();
                        informationPointsJSON = controllerApi.GetListOfInformationPointsAdditionalDetail(jobTarget.ApplicationID);
                        if (informationPointsJSON != String.Empty) FileIOHelper.SaveFileToPath(informationPointsJSON, FilePathMap.InformationPointsDetailDataFilePath(jobTarget));

                        #endregion

                        #region Node Properties

                        List<AppDRESTNode> nodesList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTNode>(FilePathMap.NodesDataFilePath(jobTarget));
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
                                        if (File.Exists(FilePathMap.NodeRuntimePropertiesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, node)) == false)
                                        {
                                            string nodePropertiesJSON = controllerApi.GetNodeProperties(node.id);
                                            if (nodePropertiesJSON != String.Empty) FileIOHelper.SaveFileToPath(nodePropertiesJSON, FilePathMap.NodeRuntimePropertiesDataFilePath(jobTarget, jobConfiguration.Input.TimeRange, node));
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
