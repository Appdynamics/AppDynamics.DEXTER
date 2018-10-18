using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexSnapshots : JobStepIndexBase
    {
        private const int SNAPSHOTS_INDEX_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD = 100;

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

                bool reportFolderCleaned = false;

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

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        #region Load logical model

                        List<APMTier> tiersList = FileIOHelper.ReadListFromCSVFile<APMTier>(FilePathMap.TiersIndexFilePath(jobTarget), new APMTierReportMap());
                        List<APMNode> nodesList = FileIOHelper.ReadListFromCSVFile<APMNode>(FilePathMap.NodesIndexFilePath(jobTarget), new APMNodeReportMap());
                        List<Backend> backendsList = FileIOHelper.ReadListFromCSVFile<Backend>(FilePathMap.BackendsIndexFilePath(jobTarget), new BackendReportMap());
                        List<BusinessTransaction> businessTransactionsList = FileIOHelper.ReadListFromCSVFile<BusinessTransaction>(FilePathMap.BusinessTransactionsIndexFilePath(jobTarget), new BusinessTransactionReportMap());
                        List<ServiceEndpoint> serviceEndpointsList = FileIOHelper.ReadListFromCSVFile<ServiceEndpoint>(FilePathMap.ServiceEndpointsIndexFilePath(jobTarget), new ServiceEndpointReportMap());
                        List<Error> errorsList = FileIOHelper.ReadListFromCSVFile<Error>(FilePathMap.ErrorsIndexFilePath(jobTarget), new ErrorReportMap());
                        List<MethodInvocationDataCollector> methodInvocationDataCollectorsList = FileIOHelper.ReadListFromCSVFile<MethodInvocationDataCollector>(FilePathMap.MethodInvocationDataCollectorsIndexFilePath(jobTarget), new MethodInvocationDataCollectorReportMap());
                        Dictionary<long, APMTier> tiersDictionary = null;
                        if (tiersList != null)
                        {
                            tiersDictionary = tiersList.ToDictionary(e => e.EntityID, e => e.Clone());
                        }
                        else
                        {
                            tiersDictionary = new Dictionary<long, APMTier>();
                        }
                        Dictionary<long, APMNode> nodesDictionary = null;
                        if (nodesList != null)
                        {
                            nodesDictionary = nodesList.ToDictionary(e => e.EntityID, e => e.Clone());
                        }
                        else
                        {
                            nodesDictionary = new Dictionary<long, APMNode>();
                        }
                        Dictionary<long, Backend> backendsDictionary = null;
                        if (backendsList != null)
                        {
                            backendsDictionary = backendsList.ToDictionary(e => e.EntityID, e => e.Clone());
                        }
                        else
                        {
                            backendsDictionary = new Dictionary<long, Backend>();
                        }
                        Dictionary<long, BusinessTransaction> businessTransactionsDictionary = null;
                        if (businessTransactionsList != null)
                        {
                            businessTransactionsDictionary = businessTransactionsList.ToDictionary(e => e.EntityID, e => e.Clone());
                        }
                        else
                        {
                            businessTransactionsDictionary = new Dictionary<long, BusinessTransaction>();
                        }
                        Dictionary<long, ServiceEndpoint> serviceEndpointsDictionary = null;
                        if (serviceEndpointsList != null)
                        {
                            serviceEndpointsDictionary = serviceEndpointsList.Where(e => e.SEPID >= 0).ToDictionary(e => e.EntityID, e => e.Clone());
                        }
                        else
                        {
                            serviceEndpointsDictionary = new Dictionary<long, ServiceEndpoint>();
                        }
                        Dictionary<long, Error> errorsDictionary = null;
                        if (errorsList != null)
                        {
                            errorsDictionary = errorsList.Where(e => e.ErrorID >= 0).ToDictionary(e => e.EntityID, e => e.Clone());
                        }
                        else
                        {
                            errorsDictionary = new Dictionary<long, Error>();
                        }

                        // Load and bucketize the framework mappings
                        Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary = populateMethodCallMappingDictionary(FilePathMap.MethodCallLinesToFrameworkTypetMappingFilePath());

                        #endregion

                        #region Index Snapshots

                        loggerConsole.Info("Index Snapshots");

                        int totalNumberOfSnapshots = 0;
                        
                        // Process each hour at a time
                        foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                        {
                            JArray listOfSnapshotsInHour = FileIOHelper.LoadJArrayFromFile(FilePathMap.SnapshotsDataFilePath(jobTarget, jobTimeRange));

                            int j = 0;

                            if (listOfSnapshotsInHour != null && listOfSnapshotsInHour.Count > 0)
                            {
                                logger.Info("Index Snapshots {0:o} to {1:o} ({2} snapshots)", jobTimeRange.From, jobTimeRange.To, listOfSnapshotsInHour.Count);
                                loggerConsole.Info("Index Snapshots {0:G} to {1:G} ({2} snapshots)", jobTimeRange.From.ToLocalTime(), jobTimeRange.To.ToLocalTime(), listOfSnapshotsInHour.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + listOfSnapshotsInHour.Count;

                                // Group all snapshots in this time range by Business Transaction
                                var listOfSnapshotsInHourGroupedByBT = listOfSnapshotsInHour.GroupBy(s => (long)s["businessTransactionId"]);

                                // For each BT in this time range, process all snapshots in this BT
                                foreach (var listOfBTSnapshotsInHourGroup in listOfSnapshotsInHourGroupedByBT)
                                {
                                    List<JToken> listOfBTSnapshotsInHour = listOfBTSnapshotsInHourGroup.ToList();

                                    BusinessTransaction businessTransaction = null;
                                    if (businessTransactionsDictionary.TryGetValue(listOfBTSnapshotsInHourGroup.Key, out businessTransaction) == true)
                                    {
                                        Console.Write("{0}({1})({2} snapshots) starting. ", businessTransaction.BTName, businessTransaction.BTID, listOfBTSnapshotsInHour.Count);

                                        // Only process if it hasn't been done before. This is for restartability mid-way
                                        if (File.Exists(FilePathMap.SnapshotsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange)) == false)
                                        {
                                            IndexedSnapshotsResults indexedSnapshotsResults = null;

                                            if (programOptions.ProcessSequentially == false && listOfBTSnapshotsInHour.Count >= SNAPSHOTS_INDEX_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD)
                                            {
                                                // Partition list of BTs into chunks
                                                int chunkSize = SNAPSHOTS_INDEX_NUMBER_OF_ENTITIES_TO_PROCESS_PER_THREAD;
                                                var listOfSnapshotsInHourChunks = listOfBTSnapshotsInHour.BreakListIntoChunks(chunkSize);

                                                // Prepare thread safe storage to dump all those chunks
                                                ConcurrentBag<IndexedSnapshotsResults> indexedSnapshotsResultsBag = new ConcurrentBag<IndexedSnapshotsResults>();

                                                int k = 0;
                                                // Index them in parallel
                                                Parallel.ForEach<List<JToken>, IndexedSnapshotsResults>(
                                                    listOfSnapshotsInHourChunks,
                                                    () => new IndexedSnapshotsResults(chunkSize),
                                                    (listOfSnapshotsInHourChunk, loop, subtotal) =>
                                                    {
                                                        IndexedSnapshotsResults indexedSnapshotsResultsChunk = indexSnapshots(jobTarget, jobTimeRange, listOfSnapshotsInHourChunk, tiersDictionary, nodesDictionary, backendsDictionary, businessTransactionsDictionary, serviceEndpointsDictionary, errorsDictionary, methodInvocationDataCollectorsList, methodCallLineClassToFrameworkTypeMappingDictionary, false);
                                                        return indexedSnapshotsResultsChunk;
                                                    },
                                                    (finalResult) =>
                                                    {
                                                        indexedSnapshotsResultsBag.Add(finalResult);
                                                        Interlocked.Add(ref k, finalResult.Snapshots.Count);
                                                        Console.Write("[{0}].", k);
                                                    }
                                                );

                                                // Combine chunks of this single BT indexing produced by multiple threads into one object
                                                indexedSnapshotsResults = new IndexedSnapshotsResults(listOfBTSnapshotsInHour.Count);
                                                foreach (IndexedSnapshotsResults indexedSnapshotsResultsChunk in indexedSnapshotsResultsBag)
                                                {
                                                    if (indexedSnapshotsResultsChunk.Snapshots.Count == 0) continue;

                                                    indexedSnapshotsResults.Snapshots.AddRange(indexedSnapshotsResultsChunk.Snapshots);
                                                    indexedSnapshotsResults.Segments.AddRange(indexedSnapshotsResultsChunk.Segments);
                                                    indexedSnapshotsResults.ExitCalls.AddRange(indexedSnapshotsResultsChunk.ExitCalls);
                                                    indexedSnapshotsResults.ServiceEndpointCalls.AddRange(indexedSnapshotsResultsChunk.ServiceEndpointCalls);
                                                    indexedSnapshotsResults.DetectedErrors.AddRange(indexedSnapshotsResultsChunk.DetectedErrors);
                                                    indexedSnapshotsResults.BusinessData.AddRange(indexedSnapshotsResultsChunk.BusinessData);
                                                    indexedSnapshotsResults.MethodCallLines.AddRange(indexedSnapshotsResultsChunk.MethodCallLines);
                                                    indexedSnapshotsResults.MethodCallLineOccurrences.AddRange(indexedSnapshotsResultsChunk.MethodCallLineOccurrences);

                                                    // Fold the folded call stacks from chunks into the results
                                                    if (indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming.ContainsKey(businessTransaction.BTID) == false)
                                                    {
                                                        indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming[businessTransaction.BTID] = new Dictionary<string, FoldedStackLine>(50);
                                                    }
                                                    addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming[businessTransaction.BTID], indexedSnapshotsResultsChunk.FoldedCallStacksBusinessTransactionsNoTiming[businessTransaction.BTID].Values.ToList<FoldedStackLine>());
                                                    if (indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsWithTiming.ContainsKey(businessTransaction.BTID) == false)
                                                    {
                                                        indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsWithTiming[businessTransaction.BTID] = new Dictionary<string, FoldedStackLine>(50);
                                                    }
                                                    addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsWithTiming[businessTransaction.BTID], indexedSnapshotsResultsChunk.FoldedCallStacksBusinessTransactionsWithTiming[businessTransaction.BTID].Values.ToList<FoldedStackLine>());
                                                    foreach (long nodeID in indexedSnapshotsResultsChunk.FoldedCallStacksNodesNoTiming.Keys)
                                                    {
                                                        if (indexedSnapshotsResults.FoldedCallStacksNodesNoTiming.ContainsKey(nodeID) == false)
                                                        {
                                                            indexedSnapshotsResults.FoldedCallStacksNodesNoTiming[nodeID] = new Dictionary<string, FoldedStackLine>(50);
                                                        }
                                                        addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksNodesNoTiming[nodeID], indexedSnapshotsResultsChunk.FoldedCallStacksNodesNoTiming[nodeID].Values.ToList<FoldedStackLine>());
                                                    }
                                                    foreach (long nodeID in indexedSnapshotsResultsChunk.FoldedCallStacksNodesWithTiming.Keys)
                                                    {
                                                        if (indexedSnapshotsResults.FoldedCallStacksNodesWithTiming.ContainsKey(nodeID) == false)
                                                        {
                                                            indexedSnapshotsResults.FoldedCallStacksNodesWithTiming[nodeID] = new Dictionary<string, FoldedStackLine>(50);
                                                        }
                                                        addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksNodesWithTiming[nodeID], indexedSnapshotsResultsChunk.FoldedCallStacksNodesWithTiming[nodeID].Values.ToList<FoldedStackLine>());
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                indexedSnapshotsResults = indexSnapshots(jobTarget, jobTimeRange, listOfBTSnapshotsInHour, tiersDictionary, nodesDictionary, backendsDictionary, businessTransactionsDictionary, serviceEndpointsDictionary, errorsDictionary, methodInvocationDataCollectorsList, methodCallLineClassToFrameworkTypeMappingDictionary, true);
                                            }
                                            j += listOfBTSnapshotsInHour.Count;

                                            // Save results for this BT for all the Snapshots
                                            if (indexedSnapshotsResults != null && indexedSnapshotsResults.Snapshots.Count > 0)   
                                            {
                                                // Sort things prettily
                                                indexedSnapshotsResults.Snapshots = indexedSnapshotsResults.Snapshots.OrderBy(s => s.Occurred).ThenBy(s => s.UserExperience).ToList();
                                                indexedSnapshotsResults.Segments = indexedSnapshotsResults.Segments.OrderBy(s => s.RequestID).ThenByDescending(s => s.IsFirstInChain).ThenBy(s => s.Occurred).ThenBy(s => s.UserExperience).ToList();
                                                indexedSnapshotsResults.ExitCalls = indexedSnapshotsResults.ExitCalls.OrderBy(c => c.RequestID).ThenBy(c => c.SegmentID).ThenBy(c => c.ExitType).ToList();
                                                indexedSnapshotsResults.ServiceEndpointCalls = indexedSnapshotsResults.ServiceEndpointCalls.OrderBy(s => s.RequestID).ThenBy(s => s.SegmentID).ThenBy(s => s.SEPName).ToList();
                                                indexedSnapshotsResults.DetectedErrors = indexedSnapshotsResults.DetectedErrors.OrderBy(e => e.RequestID).ThenBy(e => e.SegmentID).ThenBy(e => e.ErrorName).ToList();
                                                indexedSnapshotsResults.BusinessData = indexedSnapshotsResults.BusinessData.OrderBy(b => b.RequestID).ThenBy(b => b.DataType).ThenBy(b => b.DataName).ToList();

                                                // Save Snapshot data for this hour
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.Snapshots, new SnapshotReportMap(), FilePathMap.SnapshotsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.Segments, new SegmentReportMap(), FilePathMap.SnapshotsSegmentsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.ExitCalls, new ExitCallReportMap(), FilePathMap.SnapshotsExitCallsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.ServiceEndpointCalls, new ServiceEndpointCallReportMap(), FilePathMap.SnapshotsServiceEndpointCallsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.DetectedErrors, new DetectedErrorReportMap(), FilePathMap.SnapshotsDetectedErrorsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.BusinessData, new BusinessDataReportMap(), FilePathMap.SnapshotsBusinessDataIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.MethodCallLines, new MethodCallLineReportMap(), FilePathMap.SnapshotsMethodCallLinesIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.MethodCallLineOccurrences, new MethodCallLineOccurrenceReportMap(), FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));

                                                // Save Snapshot call stacks for flame graphs for this hour
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming[businessTransaction.BTID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                FileIOHelper.WriteListToCSVFile(indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsWithTiming[businessTransaction.BTID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                foreach (long nodeID in indexedSnapshotsResults.FoldedCallStacksNodesNoTiming.Keys)
                                                {
                                                    APMNode nodeForFoldedStack = null;
                                                    if (nodesDictionary.TryGetValue(nodeID, out nodeForFoldedStack) == true)
                                                    {
                                                        Dictionary<string, FoldedStackLine> foldedCallStacksList = indexedSnapshotsResults.FoldedCallStacksNodesNoTiming[nodeID];

                                                        FileIOHelper.WriteListToCSVFile(foldedCallStacksList.Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksIndexBusinessTransactionNodeHourRangeFilePath(jobTarget, businessTransaction, nodeForFoldedStack, jobTimeRange));
                                                    }
                                                }
                                                foreach (long nodeID in indexedSnapshotsResults.FoldedCallStacksNodesWithTiming.Keys)
                                                {
                                                    APMNode nodeForFoldedStack = null;
                                                    if (nodesDictionary.TryGetValue(nodeID, out nodeForFoldedStack) == true)
                                                    {
                                                        Dictionary<string, FoldedStackLine> foldedCallStacksList = indexedSnapshotsResults.FoldedCallStacksNodesWithTiming[nodeID];

                                                        FileIOHelper.WriteListToCSVFile(foldedCallStacksList.Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionNodeHourRangeFilePath(jobTarget, businessTransaction, nodeForFoldedStack, jobTimeRange));
                                                    }
                                                }
                                            }
                                        }

                                        Console.WriteLine("Done [{0}/{1}].", j, listOfSnapshotsInHour.Count);
                                    }
                                }

                                loggerConsole.Info("{0} snapshots", j);
                                totalNumberOfSnapshots = totalNumberOfSnapshots + j;
                            }
                        }
                        loggerConsole.Info("{0} snapshots total in all hour ranges", totalNumberOfSnapshots);

                        #endregion

                        #region Combine Snapshots, Segments, Call Exits, Service Endpoints and Business Data for this Application

                        // Assemble snapshot files into summary file for entire application
                        loggerConsole.Info("Combine Snapshots for This Application");

                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsMethodCallLinesIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexFilePath(jobTarget));
                        FileIOHelper.DeleteFile(FilePathMap.ApplicationSnapshotsIndexFilePath(jobTarget));

                        List<APMApplication> applicationList = FileIOHelper.ReadListFromCSVFile<APMApplication>(FilePathMap.ApplicationIndexFilePath(jobTarget), new APMApplicationReportMap());
                        APMApplication applicationsRow = null;
                        if (applicationList != null && applicationList.Count > 0)
                        {
                            applicationsRow = applicationList[0];

                            applicationsRow.Duration = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                            applicationsRow.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                            applicationsRow.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                            applicationsRow.FromUtc = jobConfiguration.Input.TimeRange.From;
                            applicationsRow.ToUtc = jobConfiguration.Input.TimeRange.To;

                            Hashtable requestIDs = new Hashtable(totalNumberOfSnapshots);

                            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                            {
                                JArray listOfSnapshotsInHour = FileIOHelper.LoadJArrayFromFile(FilePathMap.SnapshotsDataFilePath(jobTarget, jobTimeRange));

                                if (listOfSnapshotsInHour != null && listOfSnapshotsInHour.Count > 0)
                                {
                                    logger.Info("Combine Snapshots {0:o} to {1:o} ({2} snapshots)", jobTimeRange.From, jobTimeRange.To, listOfSnapshotsInHour.Count);
                                    loggerConsole.Info("Combine Snapshots {0:G} to {1:G} ({2} snapshots)", jobTimeRange.From.ToLocalTime(), jobTimeRange.To.ToLocalTime(), listOfSnapshotsInHour.Count);

                                    // Count the snapshots for Application row report
                                    foreach (JToken snapshotToken in listOfSnapshotsInHour)
                                    {
                                        if (requestIDs.ContainsKey(snapshotToken["requestGUID"].ToString()) == true)
                                        {
                                            logger.Warn("Snapshot {0} is a duplicate, skipping", snapshotToken["requestGUID"]);
                                            continue;
                                        }
                                        requestIDs.Add(snapshotToken["requestGUID"].ToString(), true);

                                        applicationsRow.NumSnapshots++;
                                        switch (snapshotToken["userExperience"].ToString())
                                        {
                                            case "NORMAL":
                                                applicationsRow.NumSnapshotsNormal++;
                                                break;

                                            case "SLOW":
                                                applicationsRow.NumSnapshotsSlow++;
                                                break;

                                            case "VERY_SLOW":
                                                applicationsRow.NumSnapshotsVerySlow++;
                                                break;

                                            case "STALL":
                                                applicationsRow.NumSnapshotsStall++;
                                                break;

                                            case "ERROR":
                                                applicationsRow.NumSnapshotsError++;
                                                break;

                                            default:
                                                break;
                                        }
                                    }

                                    // Combine main snapshot data
                                    using (FileStream snapshotsIndexFileStream = File.Open(FilePathMap.SnapshotsIndexFilePath(jobTarget), FileMode.Append))
                                    {
                                        using (FileStream segmentsIndexFileStream = File.Open(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget), FileMode.Append))
                                        {
                                            using (FileStream callExitsIndexFileStream = File.Open(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget), FileMode.Append))
                                            {
                                                using (FileStream serviceEndpointCallsIndexFileStream = File.Open(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget), FileMode.Append))
                                                {
                                                    using (FileStream detectedErrorsIndexFileStream = File.Open(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget), FileMode.Append))
                                                    {
                                                        using (FileStream businessDataIndexFileStream = File.Open(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget), FileMode.Append))
                                                        {
                                                            using (FileStream methodCallLinesIndexFileStream = File.Open(FilePathMap.SnapshotsMethodCallLinesIndexFilePath(jobTarget), FileMode.Append))
                                                            {
                                                                using (FileStream methodCallLinesOccurrencesIndexFileStream = File.Open(FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexFilePath(jobTarget), FileMode.Append))
                                                                {
                                                                    foreach (BusinessTransaction businessTransaction in businessTransactionsList)
                                                                    {
                                                                        if (File.Exists(FilePathMap.SnapshotsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange)) == true)
                                                                        {
                                                                            Console.Write("{0}({1})+", businessTransaction.BTName, businessTransaction.BTID);

                                                                            FileIOHelper.AppendTwoCSVFiles(snapshotsIndexFileStream, FilePathMap.SnapshotsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(segmentsIndexFileStream, FilePathMap.SnapshotsSegmentsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(callExitsIndexFileStream, FilePathMap.SnapshotsExitCallsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(serviceEndpointCallsIndexFileStream, FilePathMap.SnapshotsServiceEndpointCallsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(detectedErrorsIndexFileStream, FilePathMap.SnapshotsDetectedErrorsIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(businessDataIndexFileStream, FilePathMap.SnapshotsBusinessDataIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(methodCallLinesIndexFileStream, FilePathMap.SnapshotsMethodCallLinesIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                            FileIOHelper.AppendTwoCSVFiles(methodCallLinesOccurrencesIndexFileStream, FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange));
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    Console.WriteLine("Done combining snapshots from hour ranges");
                                }
                            }

                            if (applicationsRow.NumSnapshots > 0) applicationsRow.HasActivity = true;

                            FileIOHelper.WriteListToCSVFile(applicationList, new ApplicationSnapshotReportMap(), FilePathMap.ApplicationSnapshotsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Combine All for Report CSV

                        loggerConsole.Info("Combine Snapshots for All Applications");

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.SnapshotsReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.SnapshotsReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual application files into one
                        if (File.Exists(FilePathMap.SnapshotsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsReportFilePath(), FilePathMap.SnapshotsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsSegmentsReportFilePath(), FilePathMap.SnapshotsSegmentsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsExitCallsReportFilePath(), FilePathMap.SnapshotsExitCallsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsServiceEndpointCallsReportFilePath(), FilePathMap.SnapshotsServiceEndpointCallsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsDetectedErrorsCallsReportFilePath(), FilePathMap.SnapshotsDetectedErrorsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsBusinessDataReportFilePath(), FilePathMap.SnapshotsBusinessDataIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsMethodCallLinesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsMethodCallLinesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsMethodCallLinesReportFilePath(), FilePathMap.SnapshotsMethodCallLinesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.SnapshotsMethodCallLinesOccurrencesReportFilePath(), FilePathMap.SnapshotsMethodCallLinesOccurrencesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.ApplicationSnapshotsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.ApplicationSnapshotsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.ApplicationSnapshotsReportFilePath(), FilePathMap.ApplicationSnapshotsIndexFilePath(jobTarget));
                        }

                        #endregion

                        #region Combine folded Flame Graphs and Flame Charts stacks from individual Snapshots

                        if (tiersList != null && nodesList != null && businessTransactionsList != null)
                        {
                            // Prepare summary containers
                            Dictionary<long, Dictionary<string, FoldedStackLine>> foldedCallStacksNodesList = new Dictionary<long, Dictionary<string, FoldedStackLine>>(nodesList.Count);
                            Dictionary<long, Dictionary<string, FoldedStackLine>> foldedCallStacksBusinessTransactionsList = new Dictionary<long, Dictionary<string, FoldedStackLine>>(businessTransactionsList.Count);
                            Dictionary<long, Dictionary<string, FoldedStackLine>> foldedCallStacksTiersList = new Dictionary<long, Dictionary<string, FoldedStackLine>>(tiersList.Count);
                            Dictionary<string, FoldedStackLine> foldedCallStacksApplication = new Dictionary<string, FoldedStackLine>(100);
                            Dictionary<long, Dictionary<string, FoldedStackLine>> foldedCallStacksWithTimeNodesList = new Dictionary<long, Dictionary<string, FoldedStackLine>>(nodesList.Count);
                            Dictionary<long, Dictionary<string, FoldedStackLine>> foldedCallStacksWithTimeBusinessTransactionsList = new Dictionary<long, Dictionary<string, FoldedStackLine>>(businessTransactionsList.Count);
                            Dictionary<long, Dictionary<string, FoldedStackLine>> foldedCallStacksWithTimeTiersList = new Dictionary<long, Dictionary<string, FoldedStackLine>>(tiersList.Count);
                            Dictionary<string, FoldedStackLine> foldedCallStacksWithTimeApplication = new Dictionary<string, FoldedStackLine>(100);

                            #region Build Node and BT rollups first from the list of prepared chunks by BT and BT\Node structure

                            loggerConsole.Info("Fold Stacks for Nodes and Business Transactions");

                            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                            {
                                // Go through BTs
                                foreach (BusinessTransaction businessTransaction in businessTransactionsList)
                                {
                                    // Flame graph
                                    if (foldedCallStacksBusinessTransactionsList.ContainsKey(businessTransaction.BTID) == false) foldedCallStacksBusinessTransactionsList[businessTransaction.BTID] = new Dictionary<string, FoldedStackLine>(50);
                                    if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange)) == true)
                                    {
                                        List<FoldedStackLine> foldedStackLines = FileIOHelper.ReadListFromCSVFile<FoldedStackLine>(FilePathMap.SnapshotsFoldedCallStacksIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange), new FoldedStackLineReportMap());

                                        addFoldedStacks(foldedCallStacksBusinessTransactionsList[businessTransaction.BTID], foldedStackLines);
                                    }

                                    // Flame chart
                                    if (foldedCallStacksWithTimeBusinessTransactionsList.ContainsKey(businessTransaction.BTID) == false) foldedCallStacksWithTimeBusinessTransactionsList[businessTransaction.BTID] = new Dictionary<string, FoldedStackLine>(50);
                                    if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange)) == true)
                                    {
                                        List<FoldedStackLine> foldedStackLines = FileIOHelper.ReadListFromCSVFile<FoldedStackLine>(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionHourRangeFilePath(jobTarget, businessTransaction, jobTimeRange), new FoldedStackLineReportMap());

                                        addFoldedStacks(foldedCallStacksWithTimeBusinessTransactionsList[businessTransaction.BTID], foldedStackLines);
                                    }

                                    // Nodes for this BT
                                    foreach (APMNode node in nodesList)
                                    {
                                        if (node.TierID == businessTransaction.TierID)
                                        {   
                                            // Flame graph
                                            if (foldedCallStacksNodesList.ContainsKey(node.NodeID) == false) foldedCallStacksNodesList[node.NodeID] = new Dictionary<string, FoldedStackLine>(50);
                                            if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksIndexBusinessTransactionNodeHourRangeFilePath(jobTarget, businessTransaction, node, jobTimeRange)) == true)
                                            {
                                                List<FoldedStackLine> foldedStackLines = FileIOHelper.ReadListFromCSVFile<FoldedStackLine>(FilePathMap.SnapshotsFoldedCallStacksIndexBusinessTransactionNodeHourRangeFilePath(jobTarget, businessTransaction, node, jobTimeRange), new FoldedStackLineReportMap());

                                                addFoldedStacks(foldedCallStacksNodesList[node.NodeID], foldedStackLines);
                                            }

                                            // Flame chart
                                            if (foldedCallStacksWithTimeNodesList.ContainsKey(node.NodeID) == false) foldedCallStacksWithTimeNodesList[node.NodeID] = new Dictionary<string, FoldedStackLine>(50);
                                            if (File.Exists(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionNodeHourRangeFilePath(jobTarget, businessTransaction, node, jobTimeRange)) == true)
                                            {
                                                List<FoldedStackLine> foldedStackLines = FileIOHelper.ReadListFromCSVFile<FoldedStackLine>(FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexBusinessTransactionNodeHourRangeFilePath(jobTarget, businessTransaction, node, jobTimeRange), new FoldedStackLineReportMap());

                                                addFoldedStacks(foldedCallStacksWithTimeNodesList[node.NodeID], foldedStackLines);
                                            }
                                        }
                                    }
                                }
                            }

                            #endregion

                            #region Build folded stack rollups for Tiers and Applications

                            loggerConsole.Info("Fold Stacks for Tiers");

                            foreach (APMTier tier in tiersList)
                            {
                                foreach (APMNode node in nodesList)
                                {
                                    if (node.TierID == tier.TierID)
                                    {
                                        // Flame graph
                                        if (foldedCallStacksTiersList.ContainsKey(tier.TierID) == false) foldedCallStacksTiersList[tier.TierID] = new Dictionary<string, FoldedStackLine>(25);
                                        if (foldedCallStacksNodesList.ContainsKey(node.NodeID) == true)
                                        {
                                            addFoldedStacks(foldedCallStacksTiersList[tier.TierID], foldedCallStacksNodesList[node.NodeID].Values.ToList<FoldedStackLine>());
                                        }

                                        // Flame chart
                                        if (foldedCallStacksWithTimeTiersList.ContainsKey(tier.TierID) == false) foldedCallStacksWithTimeTiersList[tier.TierID] = new Dictionary<string, FoldedStackLine>(25);
                                        if (foldedCallStacksWithTimeNodesList.ContainsKey(node.NodeID) == true)
                                        {
                                            addFoldedStacks(foldedCallStacksWithTimeTiersList[tier.TierID], foldedCallStacksWithTimeNodesList[node.NodeID].Values.ToList<FoldedStackLine>());
                                        }
                                    }
                                }
                            }

                            loggerConsole.Info("Fold Stacks for Application");

                            foreach (APMTier tier in tiersList)
                            {
                                // Flame graph
                                if (foldedCallStacksTiersList.ContainsKey(tier.TierID) == true)
                                {
                                    addFoldedStacks(foldedCallStacksApplication, foldedCallStacksTiersList[tier.TierID].Values.ToList<FoldedStackLine>());
                                }

                                // Flame chart
                                if (foldedCallStacksWithTimeTiersList.ContainsKey(tier.TierID) == true)
                                {
                                    addFoldedStacks(foldedCallStacksWithTimeApplication, foldedCallStacksWithTimeTiersList[tier.TierID].Values.ToList<FoldedStackLine>());
                                }
                            }

                            #endregion

                            #region Save folded stacks

                            loggerConsole.Info("Save Fold Stacks for Business Transactions");

                            foreach (BusinessTransaction businessTransaction in businessTransactionsList)
                            {
                                if (foldedCallStacksBusinessTransactionsList.ContainsKey(businessTransaction.BTID) == true && foldedCallStacksBusinessTransactionsList[businessTransaction.BTID].Count > 0)
                                {
                                    FileIOHelper.WriteListToCSVFile(foldedCallStacksBusinessTransactionsList[businessTransaction.BTID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, businessTransaction));
                                }
                                if (foldedCallStacksWithTimeBusinessTransactionsList.ContainsKey(businessTransaction.BTID) == true && foldedCallStacksWithTimeBusinessTransactionsList[businessTransaction.BTID].Count > 0)
                                {
                                    FileIOHelper.WriteListToCSVFile(foldedCallStacksWithTimeBusinessTransactionsList[businessTransaction.BTID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, businessTransaction));
                                }
                            }

                            loggerConsole.Info("Save Fold Stacks for Tiers ");

                            foreach (APMTier tier in tiersList)
                            {
                                if (foldedCallStacksTiersList.ContainsKey(tier.TierID) == true && foldedCallStacksTiersList[tier.TierID].Count > 0)
                                {
                                    FileIOHelper.WriteListToCSVFile(foldedCallStacksTiersList[tier.TierID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, tier));
                                }
                                if (foldedCallStacksWithTimeTiersList.ContainsKey(tier.TierID) == true && foldedCallStacksWithTimeTiersList[tier.TierID].Count > 0)
                                {
                                    FileIOHelper.WriteListToCSVFile(foldedCallStacksWithTimeTiersList[tier.TierID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, tier));
                                }
                            }

                            loggerConsole.Info("Save Fold Stacks for Nodes");

                            foreach (APMNode node in nodesList)
                            {
                                if (foldedCallStacksNodesList.ContainsKey(node.NodeID) == true && foldedCallStacksNodesList[node.NodeID].Count > 0)
                                {
                                    FileIOHelper.WriteListToCSVFile(foldedCallStacksNodesList[node.NodeID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksIndexEntityFilePath(jobTarget, node));
                                }
                                if (foldedCallStacksWithTimeNodesList.ContainsKey(node.NodeID) == true && foldedCallStacksWithTimeNodesList[node.NodeID].Count > 0)
                                {
                                    FileIOHelper.WriteListToCSVFile(foldedCallStacksWithTimeNodesList[node.NodeID].Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexEntityFilePath(jobTarget, node));
                                }
                            }

                            loggerConsole.Info("Save Fold Stacks for Applications");

                            FileIOHelper.WriteListToCSVFile(foldedCallStacksApplication.Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksIndexApplicationFilePath(jobTarget));
                            FileIOHelper.WriteListToCSVFile(foldedCallStacksWithTimeApplication.Values.ToList<FoldedStackLine>(), new FoldedStackLineReportMap(), FilePathMap.SnapshotsFoldedCallStacksWithTimeIndexApplicationFilePath(jobTarget));

                            #endregion
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
            logger.Trace("Input.Snapshots={0}", jobConfiguration.Input.Snapshots);
            loggerConsole.Trace("Input.Snapshots={0}", jobConfiguration.Input.Snapshots);
            if (jobConfiguration.Input.Snapshots == false)
            {
                loggerConsole.Trace("Skipping index of snapshots");
            }
            return (jobConfiguration.Input.Snapshots == true);
        }

        private IndexedSnapshotsResults indexSnapshots(
            JobTarget jobTarget,
            JobTimeRange jobTimeRange,
            List<JToken> snapshotsList,
            Dictionary<long, APMTier> tiersDictionary,
            Dictionary<long, APMNode> nodesDictionary,
            Dictionary<long, Backend> backendsDictionary,
            Dictionary<long, BusinessTransaction> businessTransactionsDictionary,
            Dictionary<long, ServiceEndpoint> serviceEndpointsDictionary,
            Dictionary<long, Error> errorsDictionary,
            List<MethodInvocationDataCollector> methodInvocationDataCollectorsList,
            Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary,            
            bool progressToConsole)
        {
            int j = 0;

            IndexedSnapshotsResults indexedSnapshotsResults = new IndexedSnapshotsResults(snapshotsList.Count);

            foreach (JToken snapshotToken in snapshotsList)
            {
                // Only do first in chain
                if ((bool)snapshotToken["firstInChain"] == false)
                {
                    continue;
                }

                DateTime snapshotTime = UnixTimeHelper.ConvertFromUnixTimestamp((long)snapshotToken["serverStartTime"]);

                string snapshotDataFilePath = FilePathMap.SnapshotDataFilePath(
                    jobTarget,
                    snapshotToken["applicationComponentName"].ToString(), (long)snapshotToken["applicationComponentId"],
                    snapshotToken["businessTransactionName"].ToString(), (long)snapshotToken["businessTransactionId"],
                    snapshotTime,
                    snapshotToken["userExperience"].ToString(),
                    snapshotToken["requestGUID"].ToString());

                logger.Info("Indexing snapshot for Application {0}, Tier {1}, Business Transaction {2}, RequestGUID {3}", jobTarget.Application, snapshotToken["applicationComponentName"], snapshotToken["businessTransactionName"], snapshotToken["requestGUID"]);

                JObject snapshotData = FileIOHelper.LoadJObjectFromFile(snapshotDataFilePath);
                if (snapshotData != null)
                {
                    #region Fill in Snapshot data

                    Snapshot snapshot = new Snapshot();
                    snapshot.Controller = jobTarget.Controller;
                    snapshot.ApplicationName = jobTarget.Application;
                    snapshot.ApplicationID = jobTarget.ApplicationID;
                    snapshot.TierID = (long)snapshotToken["applicationComponentId"];
                    snapshot.TierName = snapshotToken["applicationComponentName"].ToString();
                    if (tiersDictionary != null)
                    {
                        APMTier tier = null;
                        if (tiersDictionary.TryGetValue(snapshot.TierID, out tier) == true)
                        {
                            snapshot.TierType = tier.TierType;
                        }
                    }
                    snapshot.BTID = (long)snapshotToken["businessTransactionId"];
                    snapshot.BTName = snapshotToken["businessTransactionName"].ToString();
                    if (businessTransactionsDictionary != null)
                    {
                        BusinessTransaction businessTransaction = null;
                        if (businessTransactionsDictionary.TryGetValue(snapshot.BTID, out businessTransaction) == true)
                        {
                            snapshot.BTType = businessTransaction.BTType;
                        }
                    }
                    snapshot.NodeID = (long)snapshotToken["applicationComponentNodeId"];
                    snapshot.NodeName = snapshotToken["applicationComponentNodeName"].ToString();
                    if (nodesDictionary != null)
                    {
                        APMNode node = null;
                        if (nodesDictionary.TryGetValue(snapshot.NodeID, out node) == true)
                        {
                            snapshot.AgentType = node.AgentType;
                        }
                    }

                    snapshot.OccurredUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)snapshotToken["serverStartTime"]);
                    snapshot.Occurred = snapshot.OccurredUtc.ToLocalTime();

                    snapshot.RequestID = snapshotToken["requestGUID"].ToString();
                    snapshot.UserExperience = snapshotToken["userExperience"].ToString();
                    snapshot.Duration = (long)snapshotToken["timeTakenInMilliSecs"];
                    snapshot.DurationRange = getDurationRangeAsString(snapshot.Duration);
                    snapshot.DiagSessionID = snapshotToken["diagnosticSessionGUID"].ToString();
                    if (snapshotToken["url"] != null) { snapshot.URL = snapshotToken["url"].ToString(); }

                    snapshot.TakenSummary = snapshotToken["summary"].ToString();
                    if (snapshot.TakenSummary.Contains("Scheduled Snapshots:") == true)
                    {
                        snapshot.TakenReason = "Scheduled";
                    }
                    else if (snapshot.TakenSummary.Contains("[Manual Diagnostic Session]") == true)
                    {
                        snapshot.TakenReason = "Diagnostic Session";
                    }
                    else if (snapshot.TakenSummary.Contains("[Error]") == true)
                    {
                        snapshot.TakenReason = "Error";
                    }
                    else if (snapshot.TakenSummary.Contains("Request was slower than the Standard Deviation threshold") == true)
                    {
                        snapshot.TakenReason = "Slower than StDev";
                    }
                    else if (snapshot.TakenSummary.Contains("of requests were slow in the last minute starting") == true)
                    {
                        snapshot.TakenReason = "Slow Rate in Minute";
                    }
                    else if (snapshot.TakenSummary.Contains("of requests had errors in the last minute starting") == true)
                    {
                        snapshot.TakenReason = "Error Rate in Minute";
                    }
                    else
                    {
                        snapshot.TakenReason = "";
                    }

                    if ((bool)snapshotToken["fullCallgraph"] == true)
                    {
                        snapshot.CallGraphType = "FULL";
                    }
                    else if ((bool)snapshotToken["delayedCallGraph"] == true)
                    {
                        snapshot.CallGraphType = "PARTIAL";
                    }
                    else
                    {
                        snapshot.CallGraphType = "NONE";
                    }

                    snapshot.HasErrors = (bool)snapshotToken["errorOccurred"];
                    snapshot.IsArchived = (bool)snapshotToken["archived"];

                    #region Fill in the deeplinks for the snapshot

                    // Decide what kind of timerange
                    long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.From);
                    long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobTimeRange.To);
                    long differenceInMinutes = (toTimeUnix - fromTimeUnix) / (60000);
                    string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                    // The snapshot link requires to have the time range is -30 < Occurredtime < +30 minutes
                    long fromTimeUnixSnapshot = UnixTimeHelper.ConvertToUnixTimestamp(snapshot.OccurredUtc.AddMinutes(-30));
                    long toTimeUnixSnapshot = UnixTimeHelper.ConvertToUnixTimestamp(snapshot.OccurredUtc.AddMinutes(+30));
                    long differenceInMinutesSnapshot = (toTimeUnixSnapshot - fromTimeUnixSnapshot) / (60000);
                    string DEEPLINK_THIS_TIMERANGE_SNAPSHOT = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnixSnapshot, fromTimeUnixSnapshot, differenceInMinutesSnapshot);
                    snapshot.SnapshotLink = String.Format(DEEPLINK_SNAPSHOT_OVERVIEW, snapshot.Controller, snapshot.ApplicationID, snapshot.RequestID, DEEPLINK_THIS_TIMERANGE_SNAPSHOT);

                    if (snapshot.CallGraphType != "NONE")
                    {
                        snapshot.FlameGraphLink = String.Format(@"=HYPERLINK(""{0}"", ""<Flame>"")", FilePathMap.FlameGraphReportFilePath(snapshot, jobTarget, false));
                    }

                    #endregion

                    #endregion

                    #region Process segments

                    IndexedSnapshotsResults indexedSnapshotResults = new IndexedSnapshotsResults(1);

                    Dictionary<string, FoldedStackLine> foldedCallStacksList = null;
                    Dictionary<string, FoldedStackLine> foldedCallStacksWithTimeList = null;

                    JArray snapshotSegmentsList = (JArray)snapshotData["segments"];
                    if (snapshotSegmentsList != null && snapshotSegmentsList.Count > 0)
                    {
                        #region Prepare elements for storage of indexed data from segments

                        // Assume 25 distinct call stacks in each segment
                        foldedCallStacksList = new Dictionary<string, FoldedStackLine>(snapshotSegmentsList.Count * 25);
                        foldedCallStacksWithTimeList = new Dictionary<string, FoldedStackLine>(snapshotSegmentsList.Count * 25);

                        SortedDictionary<string, CallChainContainer> callChainsSnapshot = new SortedDictionary<string, CallChainContainer>();

                        #endregion

                        #region Process segments one by one

                        foreach (JToken snapshotSegmentToken in snapshotSegmentsList)
                        {
                            JObject snapshotSegmentDetail = (JObject)(snapshotData["segmentDetails"][snapshotSegmentToken["id"].ToString()]);

                            if (snapshotSegmentDetail != null)
                            {
                                #region Fill in Segment data

                                Segment segment = new Segment();

                                segment.Controller = snapshot.Controller;
                                segment.ApplicationName = snapshot.ApplicationName;
                                segment.ApplicationID = snapshot.ApplicationID;
                                segment.TierID = (long)snapshotSegmentToken["applicationComponentId"];
                                segment.TierName = snapshotSegmentToken["applicationComponentName"].ToString();
                                if (tiersDictionary != null)
                                {
                                    APMTier tier = null;
                                    if (tiersDictionary.TryGetValue(snapshot.TierID, out tier) == true)
                                    {
                                        segment.TierType = tier.TierType;
                                    }
                                }
                                segment.BTID = snapshot.BTID;
                                segment.BTName = snapshot.BTName;
                                segment.BTType = snapshot.BTType;
                                segment.NodeID = (long)snapshotSegmentToken["applicationComponentNodeId"];
                                segment.NodeName = snapshotSegmentToken["applicationComponentNodeName"].ToString();
                                if (nodesDictionary != null)
                                {
                                    APMNode node = null;
                                    if (nodesDictionary.TryGetValue(snapshot.NodeID, out node) == true)
                                    {
                                        segment.AgentType = node.AgentType;
                                    }
                                }

                                segment.OccurredUtc = UnixTimeHelper.ConvertFromUnixTimestamp((long)snapshotSegmentDetail["serverStartTime"]);
                                segment.Occurred = segment.OccurredUtc.ToLocalTime();

                                segment.RequestID = snapshotSegmentDetail["requestGUID"].ToString();
                                segment.SegmentID = (long)snapshotSegmentDetail["id"];
                                segment.UserExperience = snapshotSegmentDetail["userExperience"].ToString();
                                segment.SnapshotUserExperience = snapshot.UserExperience;
                                segment.Duration = (long)snapshotSegmentDetail["timeTakenInMilliSecs"];
                                segment.DurationRange = getDurationRangeAsString(segment.Duration);
                                // The value here is not in milliseconds, contrary to the name
                                segment.CPUDuration = Math.Round((double)snapshotSegmentDetail["cpuTimeTakenInMilliSecs"] / 1000000, 2);
                                segment.E2ELatency = (long)snapshotSegmentDetail["endToEndLatency"];
                                if (segment.E2ELatency == -1) { segment.E2ELatency = 0; }
                                if (snapshotSegmentDetail["totalWaitTime"] != null) segment.WaitDuration = (long)snapshotSegmentDetail["totalWaitTime"];
                                if (snapshotSegmentDetail["totalBlockTime"] != null) segment.BlockDuration = (long)snapshotSegmentDetail["totalBlockTime"];
                                segment.DiagSessionID = snapshotSegmentDetail["diagnosticSessionGUID"].ToString();
                                if (snapshotSegmentDetail["url"] != null) segment.URL = snapshotSegmentDetail["url"].ToString();
                                if (snapshotSegmentDetail["securityID"] != null) { segment.UserPrincipal = snapshotSegmentDetail["securityID"].ToString(); }
                                if (snapshotSegmentDetail["httpSessionID"] != null) { segment.HTTPSessionID = snapshotSegmentDetail["httpSessionID"].ToString(); }

                                segment.TakenSummary = snapshotSegmentDetail["summary"].ToString();
                                if (segment.TakenSummary.Contains("Scheduled Snapshots:") == true)
                                {
                                    segment.TakenReason = "Scheduled";
                                }
                                else if (segment.TakenSummary.Contains("[Manual Diagnostic Session]") == true)
                                {
                                    segment.TakenReason = "Diagnostic Session";
                                }
                                else if (segment.TakenSummary.Contains("[Error]") == true)
                                {
                                    segment.TakenReason = "Error";
                                }
                                else if (segment.TakenSummary.Contains("Request was slower than the Standard Deviation threshold") == true)
                                {
                                    segment.TakenReason = "Slower than StDev";
                                }
                                else if (segment.TakenSummary.Contains("of requests were slow in the last minute starting") == true)
                                {
                                    segment.TakenReason = "Slow Rate in Minute";
                                }
                                else if (segment.TakenSummary.Contains("of requests had errors in the last minute starting") == true)
                                {
                                    segment.TakenReason = "Error Rate in Minute";
                                }
                                else if (segment.TakenSummary.Contains("[Continuing]") == true)
                                {
                                    segment.TakenReason = "Continuing";
                                }
                                else
                                {
                                    segment.TakenReason = "";
                                }
                                segment.TakenPolicy = snapshotSegmentDetail["deepDivePolicy"].ToString();

                                segment.ThreadID = snapshotSegmentDetail["threadID"].ToString();
                                segment.ThreadName = snapshotSegmentDetail["threadName"].ToString();

                                segment.WarningThreshold = snapshotSegmentDetail["warningThreshold"].ToString();
                                segment.CriticalThreshold = snapshotSegmentDetail["criticalThreshold"].ToString();

                                if ((bool)snapshotSegmentToken["fullCallgraph"] == true)
                                {
                                    segment.CallGraphType = "FULL";
                                }
                                else if ((bool)snapshotSegmentToken["delayedCallGraph"] == true)
                                {
                                    segment.CallGraphType = "PARTIAL";
                                }
                                else
                                {
                                    segment.CallGraphType = "NONE";
                                }

                                segment.HasErrors = (bool)snapshotSegmentDetail["errorOccured"];
                                segment.IsArchived = (bool)snapshotSegmentDetail["archived"];
                                segment.IsAsync = (bool)snapshotSegmentDetail["async"];
                                segment.IsFirstInChain = (bool)snapshotSegmentDetail["firstInChain"];

                                // What is the relationship to the root segment
                                segment.FromSegmentID = 0;
                                if (segment.IsFirstInChain == false)
                                {
                                    if (snapshotSegmentDetail["snapshotExitSequence"] != null)
                                    {
                                        // Parent exit has snapshotSequenceCounter in exitCalls array
                                        // Child exit has snapshotExitSequence value that binds the child snapshot to the parent
                                        List<JToken> possibleParentSegments = snapshotSegmentsList.Where(s => s["exitCalls"].Count() > 0).ToList();
                                        foreach (JToken possibleParentSegment in possibleParentSegments)
                                        {
                                            List<JToken> possibleExits = possibleParentSegment["exitCalls"].Where(e => e["snapshotSequenceCounter"].ToString() == snapshotSegmentDetail["snapshotExitSequence"].ToString()).ToList();
                                            if (possibleExits.Count > 0)
                                            {
                                                segment.FromSegmentID = (long)possibleParentSegment["id"];
                                                break;
                                            }
                                        }
                                    }
                                    if (segment.FromSegmentID == 0)
                                    {
                                        // Some async snapshots can have no initiating parent
                                        // Do nothing
                                        // OR!
                                        // This can happen when the parent snapshot got an exception calling downstream tier, both producing snapshot, but parent snapshot doesn't have a call graph
                                        // But sometimes non-async ones have funny parenting
                                    }
                                }
                                segment.FromTierName = snapshotSegmentToken["callingComponent"].ToString();

                                #endregion

                                #region Fill in the deeplinks for the segment

                                // The snapshot link requires to have the time range is -30 < Occurredtime < +30 minutes
                                fromTimeUnixSnapshot = UnixTimeHelper.ConvertToUnixTimestamp(snapshot.OccurredUtc.AddMinutes(-30));
                                toTimeUnixSnapshot = UnixTimeHelper.ConvertToUnixTimestamp(snapshot.OccurredUtc.AddMinutes(+30));
                                differenceInMinutesSnapshot = (toTimeUnixSnapshot - fromTimeUnixSnapshot) / (60000);
                                DEEPLINK_THIS_TIMERANGE_SNAPSHOT = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnixSnapshot, fromTimeUnixSnapshot, differenceInMinutesSnapshot);
                                segment.SegmentLink = String.Format(DEEPLINK_SNAPSHOT_SEGMENT, segment.Controller, segment.ApplicationID, segment.RequestID, segment.SegmentID, DEEPLINK_THIS_TIMERANGE_SNAPSHOT);

                                #endregion

                                #region Get segment's call chain and make it pretty

                                // Convert call chain to something readable
                                // This is raw:
                                //Component:108|Exit Call:JMS|To:{[UNRESOLVED][115]}|Component:{[UNRESOLVED][115]}|Exit Call:JMS|To:115|Component:115
                                //^^^^^^^^^^^^^ ECommerce-Services
                                //              ^^^^^^^^^^^^^ JMS
                                //							^^^^^^^^^^^^^^^^^^^^^^ Active MQ-OrderQueue
                                //												   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^ JMS
                                //																				^^^^^^^^^^^^^^ 
                                //																							   ^^^^^^^ Order-Processing-Services
                                //																									   ^^^^^^^^^^^^ Order-Processing-Services
                                // This is what I want it to look like:
                                // ECommerce-Services->[JMS]->Active MQ-OrderQueue->[JMS]->Order-Processing-Services
                                // 
                                // This is raw:
                                //Component:108|Exit Call:WEB_SERVICE|To:111|Component:111
                                //^^^^^^^^^^^^^ ECommerce-Services
                                //              ^^^^^^^^^^^^^^^^^^^^^ WEB_SERVICE
                                //                                    ^^^^^^ Inventory-Services
                                //                                           ^^^^^^ Inventory-Services
                                // This is what I want it to look like:
                                // ECommerce-Services->[WEB_SERVICE]->Inventory-Services
                                string callChainForThisSegment = snapshotSegmentDetail["callChain"].ToString();
                                string[] callChainTokens = callChainForThisSegment.Split('|');
                                StringBuilder sbCallChain = new StringBuilder();
                                foreach (string callChainToken in callChainTokens)
                                {
                                    if (callChainToken.StartsWith("Component") == true)
                                    {
                                        long tierID = -1;
                                        if (long.TryParse(callChainToken.Substring(10), out tierID) == true)
                                        {
                                            APMTier tier = null;
                                            if (tiersDictionary.TryGetValue(tierID, out tier) == true)
                                            {
                                                sbCallChain.AppendFormat("({0})->", tier.TierName);
                                            }
                                        }
                                    }
                                    else if (callChainToken.StartsWith("Exit Call") == true)
                                    {
                                        sbCallChain.AppendFormat("[{0}]->", callChainToken.Substring(10));
                                    }
                                    else if (callChainToken.StartsWith("To:{[UNRESOLVED]") == true)
                                    {
                                        long backendID = -1;
                                        if (long.TryParse(callChainToken.Substring(17).TrimEnd(']', '}'), out backendID) == true)
                                        {
                                            Backend backend = null;
                                            if (backendsDictionary.TryGetValue(backendID, out backend) == true)
                                            {
                                                //sbCallChain.AppendFormat("<{0}><{1}>>->", backendRow.BackendName, backendRow.BackendType);
                                                sbCallChain.AppendFormat("<{0}>->", backend.BackendName);
                                            }
                                        }
                                    }
                                }
                                if (sbCallChain.Length > 2)
                                {
                                    sbCallChain.Remove(sbCallChain.Length - 2, 2);
                                }
                                callChainForThisSegment = sbCallChain.ToString();

                                #endregion

                                #region Process Exits in Segment

                                SortedDictionary<string, CallChainContainer> callChainsSegment = new SortedDictionary<string, CallChainContainer>();

                                List<ExitCall> exitCallsListInThisSegment = new List<ExitCall>();
                                foreach (JToken exitCallToken in snapshotSegmentDetail["snapshotExitCalls"])
                                {
                                    #region Parse the exit call into correct exit

                                    ExitCall exitCall = new ExitCall();

                                    exitCall.Controller = segment.Controller;
                                    exitCall.ApplicationName = segment.ApplicationName;
                                    exitCall.ApplicationID = segment.ApplicationID;
                                    exitCall.TierID = segment.TierID;
                                    exitCall.TierName = segment.TierName;
                                    exitCall.TierType = segment.TierType;
                                    exitCall.BTID = segment.BTID;
                                    exitCall.BTName = segment.BTName;
                                    exitCall.BTType = segment.BTType;
                                    exitCall.NodeID = segment.NodeID;
                                    exitCall.NodeName = segment.NodeName;
                                    exitCall.AgentType = segment.AgentType;

                                    exitCall.RequestID = segment.RequestID;
                                    exitCall.SegmentID = segment.SegmentID;

                                    exitCall.OccurredUtc = segment.OccurredUtc;
                                    exitCall.Occurred = segment.Occurred;

                                    exitCall.SegmentUserExperience = segment.UserExperience;
                                    exitCall.SnapshotUserExperience = snapshot.UserExperience;

                                    exitCall.ExitType = exitCallToken["exitPointName"].ToString();

                                    exitCall.SequenceNumber = exitCallToken["snapshotSequenceCounter"].ToString();

                                    exitCall.Duration = (long)exitCallToken["timeTakenInMillis"];
                                    exitCall.DurationRange = getDurationRangeAsString(exitCall.Duration);

                                    exitCall.IsAsync = ((bool)exitCallToken["exitPointCall"]["synchronous"] == false);

                                    // Create pretty call chain
                                    // Where are we going, Tier or Backend
                                    if (exitCallToken["toComponentId"].ToString().StartsWith("{[UNRESOLVED]") == true)
                                    {
                                        // Backend
                                        exitCall.ToEntityType = Backend.ENTITY_TYPE;
                                        JToken goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "backendId").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityID = (long)goingToProperty["value"]; ;
                                        }
                                        goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "to").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityName = goingToProperty["value"].ToString();
                                        }
                                        if (exitCall.IsAsync == false)
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms]-><{2}>", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                        else
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms async]-><{2}>", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                    }
                                    else if (exitCallToken["toComponentId"].ToString().StartsWith("App:") == true)
                                    {
                                        // Application in same controller
                                        exitCall.ToEntityType = APMApplication.ENTITY_TYPE;
                                        JToken goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "appId").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityID = (long)goingToProperty["value"]; ;
                                        }
                                        goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "to").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityName = goingToProperty["value"].ToString();
                                        }
                                        if (exitCall.IsAsync == false)
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms]->{{{2}}}", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                        else
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms async]->{{{2}}}", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                    }
                                    else if (exitCallToken["toComponentId"].ToString().StartsWith("ExApp:") == true)
                                    {
                                        // Application in Federated controller
                                        exitCall.ToEntityType = APMApplication.ENTITY_TYPE;
                                        JToken goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "appId").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityID = (long)goingToProperty["value"]; ;
                                        }
                                        goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "to").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityName = goingToProperty["value"].ToString();
                                        }
                                        if (exitCall.IsAsync == false)
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms]->{{{2}}}", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                        else
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms async]->{{{2}}}", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                    }
                                    else
                                    {
                                        // Tier
                                        exitCall.ToEntityType = APMTier.ENTITY_TYPE;
                                        try { exitCall.ToEntityID = (long)exitCallToken["toComponentId"]; } catch { }
                                        JToken goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "to").FirstOrDefault();
                                        if (goingToProperty != null)
                                        {
                                            exitCall.ToEntityName = goingToProperty["value"].ToString();
                                        }
                                        if (exitCall.IsAsync == false)
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms]->({2})", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                        else
                                        {
                                            exitCall.CallChain = String.Format("{0}->[{1}]:[{3} ms async]->({2})", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName, exitCall.Duration);
                                        }
                                    }

                                    // Add the exit call to the overall list for tracking
                                    string exitCCCKey = String.Format("{0}_{1}_{2}", callChainForThisSegment, exitCall.ExitType, exitCall.ToEntityName);
                                    CallChainContainer cccSeg = null;
                                    if (callChainsSegment.ContainsKey(exitCCCKey) == false)
                                    {
                                        cccSeg = new CallChainContainer { From = callChainForThisSegment, ExitType = exitCall.ExitType, ToEntityName = exitCall.ToEntityName, ToEntityType = exitCall.ToEntityType };
                                        callChainsSegment.Add(exitCCCKey, cccSeg);
                                    }
                                    else
                                    {
                                        cccSeg = callChainsSegment[exitCCCKey];
                                    }
                                    cccSeg.CallTimings.Add(new CallTiming { Async = exitCall.IsAsync, Duration = exitCall.Duration });

                                    CallChainContainer cccSnap = null;
                                    if (callChainsSnapshot.ContainsKey(exitCCCKey) == false)
                                    {
                                        cccSnap = new CallChainContainer { From = callChainForThisSegment, ExitType = exitCall.ExitType, ToEntityName = exitCall.ToEntityName, ToEntityType = exitCall.ToEntityType };
                                        callChainsSnapshot.Add(exitCCCKey, cccSnap);
                                    }
                                    else
                                    {
                                        cccSnap = callChainsSnapshot[exitCCCKey];
                                    }
                                    cccSnap.CallTimings.Add(new CallTiming { Async = exitCall.IsAsync, Duration = exitCall.Duration });

                                    exitCall.Detail = exitCallToken["detailString"].ToString().Trim();
                                    if (exitCall.Detail.Length == 0)
                                    {
                                        exitCall.Detail = exitCall.ToEntityName;
                                    }
                                    exitCall.ErrorDetail = exitCallToken["errorDetails"].ToString();
                                    if (exitCall.ErrorDetail == "\\N") { exitCall.ErrorDetail = String.Empty; }

                                    exitCall.Method = exitCallToken["callingMethod"].ToString();
                                    exitCall.Framework = getFrameworkFromClassOrFunctionName(exitCall.Method, methodCallLineClassToFrameworkTypeMappingDictionary);

                                    // Parse Properties
                                    exitCall.PropsAll = exitCallToken["propertiesAsString"].ToString();
                                    int i = 0;
                                    foreach (JToken customExitPropertyToken in exitCallToken["properties"])
                                    {
                                        exitCall.NumProps++;
                                        string propertyName = customExitPropertyToken["name"].ToString();
                                        string propertyValue = customExitPropertyToken["value"].ToString();
                                        switch (propertyName)
                                        {
                                            case "component":
                                            case "to":
                                            case "from":
                                            case "backendId":
                                                // Ignore those, already mapped elsewhere
                                                exitCall.NumProps--;
                                                break;
                                            case "Query Type":
                                                exitCall.PropQueryType = propertyValue;
                                                break;
                                            case "Statement Type":
                                                exitCall.PropStatementType = propertyValue;
                                                break;
                                            case "URL":
                                                exitCall.PropURL = propertyValue;
                                                break;
                                            case "Service":
                                                exitCall.PropServiceName = propertyValue;
                                                break;
                                            case "Operation":
                                                exitCall.PropOperationName = propertyValue;
                                                break;
                                            case "Name":
                                                exitCall.PropName = propertyValue;
                                                break;
                                            case "Asynchronous":
                                                exitCall.PropAsync = propertyValue;
                                                break;
                                            case "Continuation":
                                                exitCall.PropContinuation = propertyValue;
                                                break;
                                            default:
                                                i++;
                                                // Have 5 overflow buckets for those, hope it is enough
                                                if (i == 1)
                                                {
                                                    exitCall.Prop1Name = propertyName;
                                                    exitCall.Prop1Value = propertyValue;
                                                }
                                                else if (i == 2)
                                                {
                                                    exitCall.Prop2Name = propertyName;
                                                    exitCall.Prop2Value = propertyValue;
                                                }
                                                else if (i == 3)
                                                {
                                                    exitCall.Prop3Name = propertyName;
                                                    exitCall.Prop3Value = propertyValue;
                                                }
                                                else if (i == 4)
                                                {
                                                    exitCall.Prop4Name = propertyName;
                                                    exitCall.Prop4Value = propertyValue;
                                                }
                                                else if (i == 5)
                                                {
                                                    exitCall.Prop5Name = propertyName;
                                                    exitCall.Prop5Value = propertyValue;
                                                }
                                                break;
                                        }
                                    }

                                    exitCall.NumCalls = (int)exitCallToken["count"];
                                    exitCall.NumErrors = (int)exitCallToken["errorCount"];
                                    exitCall.HasErrors = exitCall.NumErrors != 0;

                                    // Calculate duration
                                    exitCall.AvgDuration = exitCall.Duration / exitCall.NumCalls;

                                    // Which Segment are we going to
                                    exitCall.ToSegmentID = 0;
                                    if (exitCallToken["snapshotSequenceCounter"] != null)
                                    {
                                        // Parent segment has snapshotSequenceCounter in exitCalls array
                                        // Child snapshot has snapshotExitSequence value that binds the child snapshot to the parent
                                        JToken childSegment = snapshotSegmentsList.Where(s => s["triggerCall"].HasValues == true && s["triggerCall"]["snapshotSequenceCounter"].ToString() == exitCallToken["snapshotSequenceCounter"].ToString()).FirstOrDefault();
                                        if (childSegment != null)
                                        {
                                            exitCall.ToSegmentID = (long)childSegment["id"];
                                        }
                                    }

                                    #endregion

                                    #region Parse SQL 

                                    // Only format for stuff that is actually SQL
                                    if (exitCall.ExitType == "ADODOTNET" ||
                                        exitCall.ExitType == "JDBC" ||
                                        exitCall.ExitType == "DB")
                                    {
                                        if (exitCall.Detail != null && exitCall.Detail.Length > 0 && exitCall.PropQueryType != null && exitCall.PropQueryType.Length > 0)
                                        {
                                            // Only look through the first few characters for the SQL selection
                                            int lengthToSeekThrough = 30;
                                            if (exitCall.Detail.Length < lengthToSeekThrough) lengthToSeekThrough = exitCall.Detail.Length;

                                            switch (exitCall.PropQueryType.ToLower())
                                            {
                                                case "stored procedure":
                                                    exitCall.SQLClauseType = "PROCCALL";
                                                    break;

                                                case "commit":
                                                    exitCall.SQLClauseType = "COMMIT";
                                                    break;

                                                case "db transaction rollback":
                                                    exitCall.SQLClauseType = "ROLLBACK";
                                                    break;

                                                case "datasource.getconnection":
                                                case "driver.connect":
                                                    exitCall.SQLClauseType = "CONNECTION";
                                                    break;

                                                case "insert":
                                                case "query":
                                                case "update":
                                                case "delete":
                                                case "batch update":
                                                default:
                                                    // Get SQL statement type
                                                    exitCall.SQLClauseType = getSQLClauseType(exitCall.Detail, lengthToSeekThrough);

                                                    // Check other clauses
                                                    exitCall.SQLWhere = doesSQLStatementContain(exitCall.Detail, @"\bWHERE\s");
                                                    exitCall.SQLGroupBy = doesSQLStatementContain(exitCall.Detail, @"\bGROUP BY\s");
                                                    exitCall.SQLOrderBy = doesSQLStatementContain(exitCall.Detail, @"\bORDER BY\s");
                                                    exitCall.SQLHaving = doesSQLStatementContain(exitCall.Detail, @"\bHAVING\s");
                                                    exitCall.SQLUnion = doesSQLStatementContain(exitCall.Detail, @"\bUNION\s");

                                                    // Get join type if present
                                                    exitCall.SQLJoinType = getSQLJoinType(exitCall.Detail);

                                                    break;
                                            }
                                        }
                                    }

                                    #endregion

                                    #region Parse HTTP

                                    if (exitCall.ExitType == "HTTP" ||
                                        exitCall.ExitType == "WCF" ||
                                        exitCall.ExitType == "WEB_SERVICE" ||
                                        exitCall.ExitType == "DOTNETRemoting")
                                    {
                                        if (exitCall.Detail != null && exitCall.Detail.Length > 0)
                                        {
                                            Uri uri = null;
                                            try
                                            {
                                                uri = new Uri(exitCall.Detail);
                                            }
                                            catch
                                            {
                                                // Not a URI, ignore it
                                            }

                                            if (uri != null)
                                            {
                                                exitCall.URLScheme = uri.Scheme;
                                                exitCall.URLHost = uri.Host;
                                                exitCall.URLPort = uri.Port;
                                                exitCall.URLPath = uri.LocalPath;
                                                exitCall.URLQuery = Uri.UnescapeDataString(uri.Query);
                                                exitCall.URLFragment = uri.Fragment;
                                                if (exitCall.URLQuery.Length > 0)
                                                {
                                                    exitCall.URLNumQueryParams = exitCall.URLQuery.Count(c => c == '=');
                                                }
                                                Regex regexGUID = new Regex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.IgnoreCase);
                                                exitCall.URLCleaned = regexGUID.Replace(String.Format("{0}://{1}{2}", uri.Scheme, uri.Authority, uri.LocalPath), "{guid-removed}");

                                            }
                                        }
                                    }

                                    #endregion

                                    #region Parse CUSTOM

                                    if (exitCall.ExitType == "CUSTOM")
                                    {
                                        // Look up whether this Exit type is something prettier than CUSTOM
                                        Backend backend = null;
                                        if (backendsDictionary.TryGetValue(exitCall.ToEntityID, out backend) == true)
                                        {
                                            exitCall.ExitType = backend.BackendType;
                                        }
                                    }

                                    #endregion 

                                    exitCallsListInThisSegment.Add(exitCall);
                                }

                                #endregion

                                #region Process Service Endpoints in Segment

                                List<ServiceEndpointCall> serviceEndpointCallsListInThisSegment = new List<ServiceEndpointCall>();
                                foreach (JToken serviceEndpointToken in snapshotSegmentDetail["serviceEndPointIds"])
                                {
                                    long serviceEndpointID = (long)((JValue)serviceEndpointToken).Value;

                                    #region Fill Service Endpoint stuff

                                    ServiceEndpointCall serviceEndpointCall = new ServiceEndpointCall();

                                    serviceEndpointCall.Controller = segment.Controller;
                                    serviceEndpointCall.ApplicationName = segment.ApplicationName;
                                    serviceEndpointCall.ApplicationID = segment.ApplicationID;
                                    serviceEndpointCall.TierID = segment.TierID;
                                    serviceEndpointCall.TierName = segment.TierName;
                                    serviceEndpointCall.TierType = segment.TierType;
                                    serviceEndpointCall.BTID = segment.BTID;
                                    serviceEndpointCall.BTName = segment.BTName;
                                    serviceEndpointCall.BTType = segment.BTType;
                                    serviceEndpointCall.NodeID = segment.NodeID;
                                    serviceEndpointCall.NodeName = segment.NodeName;
                                    serviceEndpointCall.AgentType = segment.AgentType;

                                    serviceEndpointCall.RequestID = segment.RequestID;
                                    serviceEndpointCall.SegmentID = segment.SegmentID;

                                    serviceEndpointCall.OccurredUtc = segment.OccurredUtc;
                                    serviceEndpointCall.Occurred = segment.Occurred;

                                    serviceEndpointCall.SegmentUserExperience = segment.UserExperience;
                                    serviceEndpointCall.SnapshotUserExperience = snapshot.UserExperience;

                                    if (serviceEndpointsDictionary != null)
                                    {
                                        ServiceEndpoint serviceEndpoint = null;
                                        if (serviceEndpointsDictionary.TryGetValue(serviceEndpointID, out serviceEndpoint) == true)
                                        {
                                            serviceEndpointCall.SEPID = serviceEndpoint.SEPID;
                                            serviceEndpointCall.SEPName = serviceEndpoint.SEPName;
                                            serviceEndpointCall.SEPType = serviceEndpoint.SEPType;
                                        }
                                    }

                                    #endregion

                                    serviceEndpointCallsListInThisSegment.Add(serviceEndpointCall);
                                }

                                #endregion

                                #region Process Errors in Segment

                                segment.NumErrors = snapshotSegmentDetail["errorIDs"].Count();
                                List<DetectedError> detectedErrorsListInThisSegment = new List<DetectedError>();
                                if (segment.NumErrors > 0)
                                {
                                    // First, populate the list of errors from the detected errors using error numbers
                                    List<DetectedError> detectedErrorsFromErrorIDs = new List<DetectedError>(segment.NumErrors);
                                    foreach (JToken errorToken in snapshotSegmentDetail["errorIDs"])
                                    {
                                        long errorID = (long)((JValue)errorToken).Value;

                                        Error error = null;
                                        if (errorsDictionary.TryGetValue(errorID, out error) == true)
                                        {
                                            DetectedError detectedError = new DetectedError();

                                            detectedError.Controller = segment.Controller;
                                            detectedError.ApplicationName = segment.ApplicationName;
                                            detectedError.ApplicationID = segment.ApplicationID;
                                            detectedError.TierID = segment.TierID;
                                            detectedError.TierName = segment.TierName;
                                            detectedError.TierType = segment.TierType;
                                            detectedError.BTID = segment.BTID;
                                            detectedError.BTName = segment.BTName;
                                            detectedError.BTType = segment.BTType;
                                            detectedError.NodeID = segment.NodeID;
                                            detectedError.NodeName = segment.NodeName;
                                            detectedError.AgentType = segment.AgentType;

                                            detectedError.RequestID = segment.RequestID;
                                            detectedError.SegmentID = segment.SegmentID;

                                            detectedError.OccurredUtc = segment.OccurredUtc;
                                            detectedError.Occurred = segment.Occurred;

                                            detectedError.SegmentUserExperience = segment.UserExperience;
                                            detectedError.SnapshotUserExperience = snapshot.UserExperience;

                                            detectedError.ErrorID = error.ErrorID;
                                            detectedError.ErrorName = error.ErrorName;
                                            detectedError.ErrorType = error.ErrorType;

                                            detectedError.ErrorIDMatchedToMessage = false;

                                            detectedError.ErrorCategory = "<unmatched>";
                                            detectedError.ErrorDetail = "<unmatched>";

                                            detectedErrorsFromErrorIDs.Add(detectedError);
                                        }
                                    }

                                    // Second, populate the list of the details of errors
                                    JArray snapshotSegmentErrorDetail = (JArray)(snapshotData["errors"][segment.SegmentID.ToString()]);
                                    if (snapshotSegmentErrorDetail != null)
                                    {
                                        detectedErrorsListInThisSegment = new List<DetectedError>(snapshotSegmentErrorDetail.Count);

                                        foreach (JToken errorToken in snapshotSegmentErrorDetail)
                                        {
                                            DetectedError detectedError = new DetectedError();

                                            detectedError.Controller = segment.Controller;
                                            detectedError.ApplicationName = segment.ApplicationName;
                                            detectedError.ApplicationID = segment.ApplicationID;
                                            detectedError.TierID = segment.TierID;
                                            detectedError.TierName = segment.TierName;
                                            detectedError.TierType = segment.TierType;
                                            detectedError.BTID = segment.BTID;
                                            detectedError.BTName = segment.BTName;
                                            detectedError.BTType = segment.BTType;
                                            detectedError.NodeID = segment.NodeID;
                                            detectedError.NodeName = segment.NodeName;
                                            detectedError.AgentType = segment.AgentType;

                                            detectedError.RequestID = segment.RequestID;
                                            detectedError.SegmentID = segment.SegmentID;

                                            detectedError.OccurredUtc = segment.OccurredUtc;
                                            detectedError.Occurred = segment.Occurred;

                                            detectedError.SegmentUserExperience = segment.UserExperience;
                                            detectedError.SnapshotUserExperience = snapshot.UserExperience;

                                            detectedError.ErrorID = -1;
                                            detectedError.ErrorName = "Unparsed";
                                            detectedError.ErrorType = "Unparsed";

                                            detectedError.ErrorCategory = errorToken["name"].ToString();
                                            detectedError.ErrorDetail = errorToken["value"].ToString().Replace("AD_STACK_TRACE:", "\n").Replace("__AD_CMSG__", "\n");

                                            detectedErrorsListInThisSegment.Add(detectedError);
                                        }
                                    }

                                    // Now reconcile them both
                                    #region Explanation of all of this nonsense to parse the errors

                                    // The IDs of the errors give us what errors Occurred
                                    // But the segment JSON does not include all the errors
                                    // The JSON in segment error detailsdoesn't include error number
                                    // However, we get multipe error instances for each of the errors
                                    // Here we have to do some serious gymnastics to match the detected error with what is in JSON
                                    // 
                                    // Segment may say:
                                    //"errorIDs" : [ 4532 ],
                                    //"errorDetails" : [ {
                                    //  "id" : 0,
                                    //  "version" : 0,
                                    //  "name" : "Internal Server Error : 500",
                                    //  "value" : "HTTP error code : 500"
                                    //}
                                    // Error detail says:
                                    //{
                                    //  "id" : 286452959,
                                    //  "version" : 0,
                                    //  "name" : "Internal Server Error : 500",
                                    //  "value" : "HTTP error code : 500"
                                    //}
                                    // -------------------------------
                                    // Sometimes segment has no details:
                                    //"errorIDs" : [ 66976 ],
                                    //"errorDetails" : [ ],
                                    // Where:
                                    // 66976        TRBException : COMException
                                    // But the details are there:
                                    //[ {
                                    //  "id" : 171771942,
                                    //  "version" : 0,
                                    //  "name" : "Corillian.Voyager.ExecutionServices.Client.TRBException:Corillian.Voyager.ExecutionServices.Client.TRBException",
                                    //  "value" : "Unknown Voyager Connectivity Error: C0000FA5__AD_CMSG__System.Runtime.InteropServices.COMException (0xC0000FA5): Execute: Session doesn't exist or has timed out in TP TP41-SVAKSA69901MXK\r\n   at Corillian.Platform.Router.VoyagerLoadBalancer.Execute(String sKey, String sRequest, String& sResponse)\r\n   at Corillian.Voyager.VoyagerInterface.Client.VlbConnector.Execute(String voyagerCommandString, String sessionId, String userId, String FI)AD_STACK_TRACE:Corillian.Voyager.ExecutionServices.Client.TRBException: at Corillian.Voyager.VoyagerInterface.Client.VlbConnector.Void HandleCOMException(System.Runtime.InteropServices.COMException)() at Corillian.Voyager.VoyagerInterface.Client.VlbConnector.System.String Execute(System.String, System.String, System.String, System.String)() at Corillian.Voyager.ExecutionServices.Client.VoyagerService.System.String Execute(Corillian.Voyager.Common.IRequest, System.String, System.String, System.String)() at Corillian.Voyager.ExecutionServices.Client.VoyagerService.System.String Execute(Corillian.Voyager.Common.IRequest)() at USB.Banking.Operations.BankingServiceProxy.USB.Banking.Messages.USBGetAccountsResponse GetAccounts(USB.Banking.Messages.USBGetAccountsRequest)() at Corillian.AppsUI.Web.Models.Accounts.AccountServiceProxy.USB.Banking.Messages.USBGetAccountsResponse Corillian.AppsUI.Web.Models.Accounts.IAccountServiceProxy.GetAllAccounts(Boolean, Boolean, Boolean, Boolean)() at Corillian.AppsUI.Web.Models.Accounts.AccountServiceProxy.USB.Banking.Messages.USBGetAccountsResponse Corillian.AppsUI.Web.Models.Accounts.IAccountServiceProxy.GetAllAccounts(Boolean)() at Castle.Proxies.Invocations.IAccountServiceProxy_GetAllAccounts.Void InvokeMethodOnTarget()() at Castle.DynamicProxy.AbstractInvocation.Void Proceed()() at USB.DigitalChannel.DigitalUI.Helpers.Logging.LoggingInterceptor.Void Intercept(Castle.DynamicProxy.IInvocation)() at Castle.DynamicProxy.AbstractInvocation.Void Proceed()() at Castle.Proxies.IAccountServiceProxyProxy.USB.Banking.Messages.USBGetAccountsResponse GetAllAccounts(Boolean)() at Corillian.AppsUI.Web.Models.PaymentCentral.PaymentCentralService.Corillian.AppsUI.Web.Models.PaymentCentral.AccountBalancesResponseContainer GetAccountBalances(Corillian.AppsUI.Web.Models.PaymentCentral.AccountBalancesRequest)() at Corillian.AppsUI.Web.Models.PaymentCentral.PaymentCentralService.Corillian.AppsUI.Web.Models.PaymentCentral.UserAndAccountsResponse GetUserAndAccounts(Corillian.AppsUI.Web.Models.PaymentCentral.AccountBalancesRequest)() at Castle.Proxies.Invocations.IPaymentCentralService_GetUserAndAccounts.Void InvokeMethodOnTarget()() at Castle.DynamicProxy.AbstractInvocation.Void Proceed()() at USB.DigitalChannel.DigitalUI.Helpers.Logging.LoggingInterceptor.Void Intercept(Castle.DynamicProxy.IInvocation)() at Castle.DynamicProxy.AbstractInvocation.Void Proceed()() at Castle.Proxies.IPaymentCentralServiceProxy.Corillian.AppsUI.Web.Models.PaymentCentral.UserAndAccountsResponse GetUserAndAccounts(Corillian.AppsUI.Web.Models.PaymentCentral.AccountBalancesRequest)() at Corillian.AppsUI.Web.AsyncGetUserAndAccounts.System.String GetUserAndAccounts()() at Corillian.AppsUI.Web.AsyncGetUserAndAccounts.System.String get_TaskResult()() at USB.DigitalChannel.CommonUI.Controllers.BaseController.Void GetAsyncData(USB.DigitalChannel.CommonUI.Models.shared.BaseModel)() at Corillian.AppsUI.Web.Controllers.BaseDashboardController.Void GetWebAsyncData(Corillian.AppsUI.Web.Models.Shared.DashboardBaseModel)() at Corillian.AppsUI.Web.Controllers.CustomerDashboardController.System.Web.Mvc.ActionResult Index()() at .System.Object lambda_method(System.Runtime.CompilerServices.ExecutionScope, System.Web.Mvc.ControllerBase, System.Object[])() at System.Web.Mvc.ReflectedActionDescriptor.System.Object Execute(System.Web.Mvc.ControllerContext, System.Collections.Generic.IDictionary`2[System.String,System.Object])() at System.Web.Mvc.ControllerActionInvoker.System.Web.Mvc.ActionResult InvokeActionMethod(System.Web.Mvc.ControllerContext, System.Web.Mvc.ActionDescriptor, System.Collections.Generic.IDictionary`2[System.String,System.Object])() at System.Web.Mvc.ControllerActionInvoker+<>c__DisplayClassd.System.Web.Mvc.ActionExecutedContext <InvokeActionMethodWithFilters>b__a()() at System.Web.Mvc.ControllerActionInvoker.System.Web.Mvc.ActionExecutedContext InvokeActionMethodFilter(System.Web.Mvc.IActionFilter, System.Web.Mvc.ActionExecutingContext, System.Func`1[System.Web.Mvc.ActionExecutedContext])() at System.Web.Mvc.ControllerActionInvoker.System.Web.Mvc.ActionExecutedContext InvokeActionMethodFilter(System.Web.Mvc.IActionFilter, System.Web.Mvc.ActionExecutingContext, System.Func`1[System.Web.Mvc.ActionExecutedContext])() at System.Web.Mvc.ControllerActionInvoker.System.Web.Mvc.ActionExecutedContext InvokeActionMethodFilter(System.Web.Mvc.IActionFilter, System.Web.Mvc.ActionExecutingContext, System.Func`1[System.Web.Mvc.ActionExecutedContext])() at System.Web.Mvc.ControllerActionInvoker.System.Web.Mvc.ActionExecutedContext InvokeActionMethodFilter(System.Web.Mvc.IActionFilter, System.Web.Mvc.ActionExecutingContext, System.Func`1[System.Web.Mvc.ActionExecutedContext])() at System.Web.Mvc.ControllerActionInvoker.System.Web.Mvc.ActionExecutedContext InvokeActionMethodWithFilters(System.Web.Mvc.ControllerContext, System.Collections.Generic.IList`1[System.Web.Mvc.IActionFilter], System.Web.Mvc.ActionDescriptor, System.Collections.Generic.IDictionary`2[System.String,System.Object])() at System.Web.Mvc.ControllerActionInvoker.Boolean InvokeAction(System.Web.Mvc.ControllerContext, System.String)() at System.Web.Mvc.Controller.Void ExecuteCore()() at System.Web.Mvc.ControllerBase.Execute() at System.Web.Mvc.MvcHandler+<>c__DisplayClass8.<BeginProcessRequest>b__4() at System.Web.Mvc.Async.AsyncResultWrapper+<>c__DisplayClass1.<MakeVoidDelegate>b__0() at System.Web.Mvc.Async.AsyncResultWrapper+<>c__DisplayClass8`1.<BeginSynchronous>b__7() at System.Web.Mvc.Async.AsyncResultWrapper+WrappedAsyncResult`1.End() at System.Web.Mvc.Async.AsyncResultWrapper.End() at System.Web.Mvc.Async.AsyncResultWrapper.End() at Microsoft.Web.Mvc.MvcDynamicSessionHandler.EndProcessRequest() at System.Web.HttpApplication+CallHandlerExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute() at System.Web.HttpApplication.ExecuteStep() at System.Web.HttpApplication+PipelineStepManager.ResumeSteps() at System.Web.HttpApplication.BeginProcessRequestNotification() at System.Web.HttpRuntime.ProcessRequestNotificationPrivate() at System.Web.Hosting.PipelineRuntime.ProcessRequestNotificationHelper() at System.Web.Hosting.PipelineRuntime.ProcessRequestNotification() at System.Web.Hosting.PipelineRuntime.ProcessRequestNotificationHelper() at System.Web.Hosting.PipelineRuntime.ProcessRequestNotification() Caused by: Corillian.Voyager.ExecutionServices.Client.TRBException  at Corillian.Platform.Router.VoyagerLoadBalancer.Void Execute(System.String, System.String, System.String ByRef)()  ... 17 more "
                                    //} ]
                                    // -------------------------------
                                    // Sometimes segment says:
                                    //"errorIDs" : [ 131789, 3002 ],
                                    //"errorDetails" : [ {
                                    //  "id" : 0,
                                    //  "version" : 0,
                                    //  "name" : "1. USB.OLBService.Handlers.TransactionUtilities",
                                    //  "value" : "USB.OLBService.Handlers.TransactionUtilities : Error occurred in MapHostTransactions: System.NullReferenceException: Object reference not set to an instance of an object.\r\n   at USB.OLBService.Handlers.TransactionUtilities.MapCheckCardHostResponseTransactions(GetOutStandingAuthRequest requestFromUI, List`1 transactions, USBAccount actualAcct)"
                                    //} ],
                                    // Where:
                                    // 131789   MessageQueueException
                                    // 3002     .NET Logger Error Messages
                                    // But the list of errors looks like that:
                                    //[ {
                                    //  "id" : 171889775,
                                    //  "version" : 0,
                                    //  "name" : "System.Messaging.MessageQueueException",
                                    //  "value" : "Insufficient resources to perform operation.AD_STACK_TRACE:System.Messaging.MessageQueueException: at System.Messaging.MessageQueue.SendInternal() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Audit.AuditTrxSender.Audit() at USB.DigitalServices.Audit.MessageReceiver.Process() at USB.OLBService.Handlers.Utilities.Audit() at USB.OLBService.Handlers.GetTransactionTypes.Execute() at USB.OLBService.Handlers.TransactionUtilities.MapHostResponseTransactions() at USB.OLBService.Handlers.TransactionUtilities.GetMonitoryListExecutor() at USB.OLBService.Handlers.TransactionUtilities.GetHostHistory() at USB.OLBService.Handlers.GetPagedTransactionsV2.Execute() at USB.DCISService.Accounts.V1.Handlers.GetAccountTransactionsV4.Execute() at Fiserv.AppService.Core.HandlerBase`1.Execute() at Fiserv.AppService.Core.ServiceProcessor.Process() at USB.DCIS.Server.DCISServiceServer.Execute() at .SyncInvokeExecute() at System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke() at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage4() at System.ServiceModel.Dispatcher.MessageRpc.Process() at System.ServiceModel.Dispatcher.ChannelHandler.DispatchAndReleasePump() at System.ServiceModel.Dispatcher.ChannelHandler.HandleRequest() at System.ServiceModel.Dispatcher.ChannelHandler.AsyncMessagePump() at System.ServiceModel.Diagnostics.Utility+AsyncThunk.UnhandledExceptionFrame() at System.ServiceModel.AsyncResult.Complete() at System.ServiceModel.Channels.InputQueue`1+AsyncQueueReader.Set() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueueChannel`1.EnqueueAndDispatch() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.HttpChannelListener.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpTransportManager.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.BeginRequest() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequest() at System.ServiceModel.PartialTrustHelpers.PartialTrustInvoke() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequestWithFlow() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke2() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.ProcessCallbacks() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.CompletionCallback() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+ScheduledOverlapped.IOCallback() at System.ServiceModel.Diagnostics.Utility+IOCompletionThunk.UnhandledExceptionFrame() at System.Threading._IOCompletionCallback.PerformIOCompletionCallback() "
                                    //}, {
                                    //  "id" : 171889775,
                                    //  "version" : 0,
                                    //  "name" : "System.Messaging.MessageQueueException",
                                    //  "value" : "Insufficient resources to perform operation.AD_STACK_TRACE:System.Messaging.MessageQueueException: at System.Messaging.MessageQueue.SendInternal() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Audit.AuditTrxSender.Audit() at USB.DigitalServices.Audit.MessageReceiver.Process() at USB.OLBService.Handlers.Utilities.Audit() at USB.OLBService.Handlers.GetTransactionTypes.Execute() at USB.OLBService.Handlers.TransactionUtilities.MapCheckCardHostResponseTransactions() at USB.OLBService.Handlers.TransactionUtilities.GetCheckCardAuthorizationsFromHost() at USB.OLBService.Handlers.GetPagedTransactionsV2.GetDebitCardAuthorizationTransactions() at USB.OLBService.Handlers.GetPagedTransactionsV2.Execute() at USB.DCISService.Accounts.V1.Handlers.GetAccountTransactionsV4.Execute() at Fiserv.AppService.Core.HandlerBase`1.Execute() at Fiserv.AppService.Core.ServiceProcessor.Process() at USB.DCIS.Server.DCISServiceServer.Execute() at .SyncInvokeExecute() at System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke() at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage4() at System.ServiceModel.Dispatcher.MessageRpc.Process() at System.ServiceModel.Dispatcher.ChannelHandler.DispatchAndReleasePump() at System.ServiceModel.Dispatcher.ChannelHandler.HandleRequest() at System.ServiceModel.Dispatcher.ChannelHandler.AsyncMessagePump() at System.ServiceModel.Diagnostics.Utility+AsyncThunk.UnhandledExceptionFrame() at System.ServiceModel.AsyncResult.Complete() at System.ServiceModel.Channels.InputQueue`1+AsyncQueueReader.Set() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueueChannel`1.EnqueueAndDispatch() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.HttpChannelListener.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpTransportManager.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.BeginRequest() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequest() at System.ServiceModel.PartialTrustHelpers.PartialTrustInvoke() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequestWithFlow() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke2() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.ProcessCallbacks() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.CompletionCallback() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+ScheduledOverlapped.IOCallback() at System.ServiceModel.Diagnostics.Utility+IOCompletionThunk.UnhandledExceptionFrame() at System.Threading._IOCompletionCallback.PerformIOCompletionCallback() "
                                    //}, {
                                    //  "id" : 171889775,
                                    //  "version" : 0,
                                    //  "name" : "System.Messaging.MessageQueueException",
                                    //  "value" : "Insufficient resources to perform operation.AD_STACK_TRACE:System.Messaging.MessageQueueException: at System.Messaging.MessageQueue.SendInternal() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Audit.AuditTrxSender.Audit() at USB.DigitalServices.Audit.MessageReceiver.Process() at USB.OLBService.Handlers.Utilities.Audit() at USB.OLBService.Handlers.GetPagedTransactionsV2.GetDebitCardAuthorizationTransactions() at USB.OLBService.Handlers.GetPagedTransactionsV2.Execute() at USB.DCISService.Accounts.V1.Handlers.GetAccountTransactionsV4.Execute() at Fiserv.AppService.Core.HandlerBase`1.Execute() at Fiserv.AppService.Core.ServiceProcessor.Process() at USB.DCIS.Server.DCISServiceServer.Execute() at .SyncInvokeExecute() at System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke() at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage4() at System.ServiceModel.Dispatcher.MessageRpc.Process() at System.ServiceModel.Dispatcher.ChannelHandler.DispatchAndReleasePump() at System.ServiceModel.Dispatcher.ChannelHandler.HandleRequest() at System.ServiceModel.Dispatcher.ChannelHandler.AsyncMessagePump() at System.ServiceModel.Diagnostics.Utility+AsyncThunk.UnhandledExceptionFrame() at System.ServiceModel.AsyncResult.Complete() at System.ServiceModel.Channels.InputQueue`1+AsyncQueueReader.Set() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueueChannel`1.EnqueueAndDispatch() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.HttpChannelListener.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpTransportManager.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.BeginRequest() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequest() at System.ServiceModel.PartialTrustHelpers.PartialTrustInvoke() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequestWithFlow() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke2() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.ProcessCallbacks() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.CompletionCallback() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+ScheduledOverlapped.IOCallback() at System.ServiceModel.Diagnostics.Utility+IOCompletionThunk.UnhandledExceptionFrame() at System.Threading._IOCompletionCallback.PerformIOCompletionCallback() "
                                    //}, {
                                    //  "id" : 171889775,
                                    //  "version" : 0,
                                    //  "name" : "System.Messaging.MessageQueueException",
                                    //  "value" : "Insufficient resources to perform operation.AD_STACK_TRACE:System.Messaging.MessageQueueException: at System.Messaging.MessageQueue.SendInternal() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Audit.AuditTrxSender.Audit() at USB.DigitalServices.Audit.MessageReceiver.Process() at USB.OLBService.Handlers.Utilities.Audit() at USB.OLBService.Handlers.GetPagedTransactionsV2.Execute() at USB.DCISService.Accounts.V1.Handlers.GetAccountTransactionsV4.Execute() at Fiserv.AppService.Core.HandlerBase`1.Execute() at Fiserv.AppService.Core.ServiceProcessor.Process() at USB.DCIS.Server.DCISServiceServer.Execute() at .SyncInvokeExecute() at System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke() at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage4() at System.ServiceModel.Dispatcher.MessageRpc.Process() at System.ServiceModel.Dispatcher.ChannelHandler.DispatchAndReleasePump() at System.ServiceModel.Dispatcher.ChannelHandler.HandleRequest() at System.ServiceModel.Dispatcher.ChannelHandler.AsyncMessagePump() at System.ServiceModel.Diagnostics.Utility+AsyncThunk.UnhandledExceptionFrame() at System.ServiceModel.AsyncResult.Complete() at System.ServiceModel.Channels.InputQueue`1+AsyncQueueReader.Set() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueueChannel`1.EnqueueAndDispatch() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.HttpChannelListener.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpTransportManager.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.BeginRequest() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequest() at System.ServiceModel.PartialTrustHelpers.PartialTrustInvoke() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequestWithFlow() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke2() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.ProcessCallbacks() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.CompletionCallback() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+ScheduledOverlapped.IOCallback() at System.ServiceModel.Diagnostics.Utility+IOCompletionThunk.UnhandledExceptionFrame() at System.Threading._IOCompletionCallback.PerformIOCompletionCallback() "
                                    //}, {
                                    //  "id" : 171889775,
                                    //  "version" : 0,
                                    //  "name" : "System.Messaging.MessageQueueException",
                                    //  "value" : "Insufficient resources to perform operation.AD_STACK_TRACE:System.Messaging.MessageQueueException: at System.Messaging.MessageQueue.SendInternal() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Messaging.Sender.Send() at Corillian.Platform.Audit.AuditTrxSender.Audit() at USB.DigitalServices.Audit.MessageReceiver.Process() at USB.DigitalServices.HandlerCore.ContextSafeHandler`1.Audit() at USB.DCISService.Accounts.V1.Handlers.GetAccountTransactionsV4.Execute() at Fiserv.AppService.Core.HandlerBase`1.Execute() at Fiserv.AppService.Core.ServiceProcessor.Process() at USB.DCIS.Server.DCISServiceServer.Execute() at .SyncInvokeExecute() at System.ServiceModel.Dispatcher.SyncMethodInvoker.Invoke() at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5() at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage4() at System.ServiceModel.Dispatcher.MessageRpc.Process() at System.ServiceModel.Dispatcher.ChannelHandler.DispatchAndReleasePump() at System.ServiceModel.Dispatcher.ChannelHandler.HandleRequest() at System.ServiceModel.Dispatcher.ChannelHandler.AsyncMessagePump() at System.ServiceModel.Diagnostics.Utility+AsyncThunk.UnhandledExceptionFrame() at System.ServiceModel.AsyncResult.Complete() at System.ServiceModel.Channels.InputQueue`1+AsyncQueueReader.Set() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueue`1.EnqueueAndDispatch() at System.ServiceModel.Channels.InputQueueChannel`1.EnqueueAndDispatch() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.SingletonChannelAcceptor`3.Enqueue() at System.ServiceModel.Channels.HttpChannelListener.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpTransportManager.HttpContextReceived() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.BeginRequest() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequest() at System.ServiceModel.PartialTrustHelpers.PartialTrustInvoke() at System.ServiceModel.Activation.HostedHttpRequestAsyncResult.OnBeginRequestWithFlow() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke2() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+WorkItem.Invoke() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.ProcessCallbacks() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper.CompletionCallback() at System.ServiceModel.Channels.IOThreadScheduler+CriticalHelper+ScheduledOverlapped.IOCallback() at System.ServiceModel.Diagnostics.Utility+IOCompletionThunk.UnhandledExceptionFrame() at System.Threading._IOCompletionCallback.PerformIOCompletionCallback() "
                                    //}, {
                                    //  "id" : 171889775,
                                    //  "version" : 0,
                                    //  "name" : "1. USB.OLBService.Handlers.TransactionUtilities",
                                    //  "value" : "USB.OLBService.Handlers.TransactionUtilities : Error occurred in MapHostTransactions: System.NullReferenceException: Object reference not set to an instance of an object.\r\n   at USB.OLBService.Handlers.TransactionUtilities.MapCheckCardHostResponseTransactions(GetOutStandingAuthRequest requestFromUI, List`1 transactions, USBAccount actualAcct)"
                                    //} ]

                                    #endregion

                                    foreach (DetectedError detectedError in detectedErrorsListInThisSegment)
                                    {
                                        // Try by exact message match
                                        DetectedError detectedErrorWithErrorID = detectedErrorsFromErrorIDs.Where(e => e.ErrorName == detectedError.ErrorCategory).FirstOrDefault();

                                        // Try starting with the message
                                        if (detectedErrorWithErrorID == null)
                                        {
                                            detectedErrorWithErrorID = detectedErrorsFromErrorIDs.Where(e => e.ErrorName.StartsWith(detectedError.ErrorCategory)).FirstOrDefault();
                                        }

                                        // Try containing the message
                                        if (detectedErrorWithErrorID == null)
                                        {
                                            detectedErrorWithErrorID = detectedErrorsFromErrorIDs.Where(e => e.ErrorName.Contains(detectedError.ErrorCategory)).FirstOrDefault();
                                        }

                                        // Try by partial name match second
                                        if (detectedErrorWithErrorID == null)
                                        {
                                            // Split by . and :
                                            // java.io.IOException 
                                            //      -> java, io, IOException
                                            //      Detected as IOException
                                            // Corillian.Voyager.ExecutionServices.Client.TRBException:Corillian.Voyager.ExecutionServices.Client.TRBException
                                            //      -> Corillian, Voyager, ExecutionServices, Client, TRBException, Corillian, Voyager, ExecutionServices, Client, TRBException
                                            //      Detected as TRBException
                                            string[] errorMessageTokens = detectedError.ErrorCategory.Split('.', ':');

                                            // Go backwards because exception type is at the end
                                            for (int i = errorMessageTokens.Length - 1; i >= 0; i--)
                                            {
                                                detectedErrorWithErrorID = detectedErrorsFromErrorIDs.Where(e => e.ErrorName.Contains(errorMessageTokens[i])).FirstOrDefault();
                                                if (detectedErrorWithErrorID != null)
                                                {
                                                    break;
                                                }
                                            }
                                        }

                                        // Did we find it?
                                        if (detectedErrorWithErrorID != null)
                                        {
                                            // Yay, we did, mark this error by ID off as matched and copy the values to the final 
                                            detectedErrorWithErrorID.ErrorIDMatchedToMessage = true;

                                            detectedError.ErrorID = detectedErrorWithErrorID.ErrorID;
                                            detectedError.ErrorName = detectedErrorWithErrorID.ErrorName;
                                            detectedError.ErrorType = detectedErrorWithErrorID.ErrorType;
                                        }
                                    }

                                    // At this point, we matched what we could.
                                    // A little cleanup - what if we have 1 by error ID and some messages without matched Error ID left? If yes, those obviously match
                                    List<DetectedError> detectedErrorsListInThisSegmentUnmatched = detectedErrorsListInThisSegment.Where(e => e.ErrorID == -1).ToList();
                                    if (detectedErrorsListInThisSegmentUnmatched.Count > 0)
                                    {
                                        List<DetectedError> detectedErrorsFromErrorIDsUnmatched = detectedErrorsFromErrorIDs.Where(e => e.ErrorIDMatchedToMessage == false).ToList();
                                        if (detectedErrorsFromErrorIDsUnmatched.Count == 1)
                                        {
                                            foreach (DetectedError detectedErrorThatWasUnmatched in detectedErrorsListInThisSegmentUnmatched)
                                            {
                                                detectedErrorThatWasUnmatched.ErrorIDMatchedToMessage = true;

                                                detectedErrorThatWasUnmatched.ErrorID = detectedErrorsFromErrorIDsUnmatched[0].ErrorID;
                                                detectedErrorThatWasUnmatched.ErrorName = detectedErrorsFromErrorIDsUnmatched[0].ErrorName;
                                                detectedErrorThatWasUnmatched.ErrorType = detectedErrorsFromErrorIDsUnmatched[0].ErrorType;
                                            }
                                        }
                                    }

                                    // Finally, let's parse stack trace away from the message
                                    foreach (DetectedError detectedError in detectedErrorsListInThisSegment)
                                    {
                                        int atIndex = detectedError.ErrorDetail.IndexOf(" at ");
                                        if (atIndex < 0) atIndex = detectedError.ErrorDetail.IndexOf("\tat ");
                                        if (atIndex < 0) atIndex = detectedError.ErrorDetail.IndexOf("\nat ");
                                        if (atIndex < 0) atIndex = detectedError.ErrorDetail.IndexOf("\rat ");

                                        if (atIndex > 0)
                                        {
                                            detectedError.ErrorMessage = detectedError.ErrorDetail.Substring(0, atIndex).TrimEnd();
                                            detectedError.ErrorStack = detectedError.ErrorDetail.Substring(atIndex).Trim();
                                        }
                                    }
                                }

                                #endregion

                                #region Process Data Collectors in Segment

                                List<BusinessData> businessDataListInThisSegment = new List<BusinessData>();

                                // Transaction properties
                                foreach (JToken transactionPropertyToken in snapshotSegmentDetail["transactionProperties"])
                                {
                                    BusinessData businessData = new BusinessData();

                                    businessData.Controller = segment.Controller;
                                    businessData.ApplicationName = segment.ApplicationName;
                                    businessData.ApplicationID = segment.ApplicationID;
                                    businessData.TierID = segment.TierID;
                                    businessData.TierName = segment.TierName;
                                    businessData.TierType = segment.TierType;
                                    businessData.BTID = segment.BTID;
                                    businessData.BTName = segment.BTName;
                                    businessData.BTType = segment.BTType;
                                    businessData.NodeID = segment.NodeID;
                                    businessData.NodeName = segment.NodeName;
                                    businessData.AgentType = segment.AgentType;

                                    businessData.RequestID = segment.RequestID;
                                    businessData.SegmentID = segment.SegmentID;

                                    businessData.OccurredUtc = segment.OccurredUtc;
                                    businessData.Occurred = segment.Occurred;

                                    businessData.SegmentUserExperience = segment.UserExperience;
                                    businessData.SnapshotUserExperience = snapshot.UserExperience;

                                    businessData.DataType = "Transaction";

                                    businessData.DataName = transactionPropertyToken["name"].ToString();
                                    businessData.DataValue = transactionPropertyToken["value"].ToString();

                                    businessDataListInThisSegment.Add(businessData);
                                }

                                // HTTP data collectors
                                foreach (JToken transactionPropertyToken in snapshotSegmentDetail["httpParameters"])
                                {
                                    BusinessData businessData = new BusinessData();

                                    businessData.Controller = segment.Controller;
                                    businessData.ApplicationName = segment.ApplicationName;
                                    businessData.ApplicationID = segment.ApplicationID;
                                    businessData.TierID = segment.TierID;
                                    businessData.TierName = segment.TierName;
                                    businessData.TierType = segment.TierType;
                                    businessData.BTID = segment.BTID;
                                    businessData.BTName = segment.BTName;
                                    businessData.BTType = segment.BTType;
                                    businessData.NodeID = segment.NodeID;
                                    businessData.NodeName = segment.NodeName;
                                    businessData.AgentType = segment.AgentType;

                                    businessData.RequestID = segment.RequestID;
                                    businessData.SegmentID = segment.SegmentID;

                                    businessData.OccurredUtc = segment.OccurredUtc;
                                    businessData.Occurred = segment.Occurred;

                                    businessData.SegmentUserExperience = segment.UserExperience;
                                    businessData.SnapshotUserExperience = snapshot.UserExperience;

                                    businessData.DataType = "HTTP";

                                    businessData.DataName = transactionPropertyToken["name"].ToString();
                                    businessData.DataValue = transactionPropertyToken["value"].ToString();

                                    businessDataListInThisSegment.Add(businessData);
                                }

                                // MIDCs 
                                foreach (JToken transactionPropertyToken in snapshotSegmentDetail["businessData"])
                                {
                                    BusinessData businessData = new BusinessData();

                                    businessData.Controller = segment.Controller;
                                    businessData.ApplicationName = segment.ApplicationName;
                                    businessData.ApplicationID = segment.ApplicationID;
                                    businessData.TierID = segment.TierID;
                                    businessData.TierName = segment.TierName;
                                    businessData.TierType = segment.TierType;
                                    businessData.BTID = segment.BTID;
                                    businessData.BTName = segment.BTName;
                                    businessData.BTType = segment.BTType;
                                    businessData.NodeID = segment.NodeID;
                                    businessData.NodeName = segment.NodeName;
                                    businessData.AgentType = segment.AgentType;

                                    businessData.RequestID = segment.RequestID;
                                    businessData.SegmentID = segment.SegmentID;

                                    businessData.OccurredUtc = segment.OccurredUtc;
                                    businessData.Occurred = segment.Occurred;

                                    businessData.SegmentUserExperience = segment.UserExperience;
                                    businessData.SnapshotUserExperience = snapshot.UserExperience;

                                    businessData.DataName = transactionPropertyToken["name"].ToString();
                                    businessData.DataValue = transactionPropertyToken["value"].ToString().Trim('[', ']');

                                    if (businessData.DataName.StartsWith("Exit ") == true)
                                    {
                                        // Exits from the call graphs
                                        businessData.DataType = "Exit";
                                    }
                                    else
                                    {
                                        // Most likely MIDC although not always
                                        businessData.DataType = "Code";
                                    }

                                    businessDataListInThisSegment.Add(businessData);
                                }

                                #endregion

                                #region Process Call Graphs in Segment

                                JArray snapshotSegmentCallGraphs = (JArray)(snapshotData["callgraphs"][segment.SegmentID.ToString()]);

                                // Can't use recursion for some of the snapshots because of StackOverflowException
                                // We run out of stack when we go 400+ deep into call stack, which apparently happens
                                //List<MethodCallLine> methodCallLinesInSegmentList = new List<MethodCallLine>(250);
                                //if (snapshotSegmentCallGraphs != null && snapshotSegmentCallGraphs.HasValues == true)
                                //{
                                //    int methodLineCallSequenceNumber = 0;
                                //    // Make a copy of this list because we are going to slowly strip it down and we don't want the parent list modified
                                //    List<ExitCall> exitCallsListInThisSegmentCopy = new List<ExitCall>(exitCallsListInThisSegment.Count);
                                //    exitCallsListInThisSegmentCopy.AddRange(exitCallsListInThisSegment);
                                //    convertCallGraphChildren_Recursion(snapshotSegmentCallGraphs[0], 0, ref methodLineCallSequenceNumber, methodCallLinesInSegmentList, serviceEndpointCallsListInThisSegment, exitCallsListInThisSegmentCopy);
                                //}

                                // Instead, let's unwrap it using stack-based algorithm

                                // Make a copy of this list because we are going to slowly strip it down and we don't want the parent list modified
                                List<MethodCallLine> methodCallLinesInSegmentList = new List<MethodCallLine>();
                                if (snapshotSegmentCallGraphs != null && snapshotSegmentCallGraphs.HasValues == true)
                                {
                                    List<ExitCall> exitCallsListInThisSegmentCopy = new List<ExitCall>(exitCallsListInThisSegment.Count);
                                    exitCallsListInThisSegmentCopy.AddRange(exitCallsListInThisSegment);

                                    // For vast majority of snapshots, there is only one element off the root that is an entry
                                    // However, for process snapshots (Node.JS tiers), there can be multiple of those
                                    methodCallLinesInSegmentList = convertCallGraphChildren_Stack(snapshotSegmentCallGraphs[0], serviceEndpointCallsListInThisSegment, exitCallsListInThisSegmentCopy);
                                }
                                if (methodCallLinesInSegmentList == null)
                                {
                                    methodCallLinesInSegmentList = new List<MethodCallLine>(0);
                                }

                                // Fill in common values and look up framework
                                long execTimeTotal = 0;
                                foreach (MethodCallLine methodCallLine in methodCallLinesInSegmentList)
                                {
                                    methodCallLine.Controller = snapshot.Controller;
                                    methodCallLine.ApplicationName = snapshot.ApplicationName;
                                    methodCallLine.ApplicationID = snapshot.ApplicationID;
                                    methodCallLine.TierID = segment.TierID;
                                    methodCallLine.TierName = segment.TierName;
                                    methodCallLine.TierType = segment.TierType;
                                    methodCallLine.BTID = segment.BTID;
                                    methodCallLine.BTName = segment.BTName;
                                    methodCallLine.BTType = segment.BTType;
                                    methodCallLine.NodeID = segment.NodeID;
                                    methodCallLine.NodeName = segment.NodeName;
                                    methodCallLine.AgentType = segment.AgentType;

                                    methodCallLine.RequestID = segment.RequestID;
                                    methodCallLine.SegmentID = segment.SegmentID;

                                    methodCallLine.SegmentUserExperience = segment.UserExperience;
                                    methodCallLine.SnapshotUserExperience = snapshot.UserExperience;

                                    // Index Method->Framework type
                                    methodCallLine.Framework = getFrameworkFromClassOrFunctionName(methodCallLine.Class, methodCallLineClassToFrameworkTypeMappingDictionary);

                                    // Calculate the duration range
                                    methodCallLine.ExecRange = getDurationRangeAsString(methodCallLine.Exec);

                                    // Calculate elapsed time
                                    methodCallLine.ExecToHere = execTimeTotal;
                                    execTimeTotal = execTimeTotal + methodCallLine.Exec;

                                    // Adjust time when it occurred to take execution time of the method into account
                                    methodCallLine.OccurredUtc = segment.OccurredUtc.AddMilliseconds(methodCallLine.ExecToHere);
                                    methodCallLine.Occurred = segment.Occurred.AddMilliseconds(methodCallLine.ExecToHere);
                                }

                                // Fill in data collectors
                                // Choose only the MIDC data collectors
                                List<BusinessData> businessDataCodeListInThisSegment = businessDataListInThisSegment.Where(b => b.DataType == "Code").ToList();
                                // Only fill in MIDCs if there are rules in places and values
                                if (businessDataCodeListInThisSegment.Count > 0 && methodInvocationDataCollectorsList != null && methodInvocationDataCollectorsList.Count > 0)
                                {
                                    var midcSettingGroups = methodInvocationDataCollectorsList.GroupBy(m => new { m.MatchClass, m.MatchMethod });

                                    foreach (var midcSettingGroup in midcSettingGroups)
                                    {
                                        List<MethodInvocationDataCollector> methodInvocationDataCollectors = midcSettingGroup.ToList();
                                        MethodInvocationDataCollector methodInvocationDataCollector = methodInvocationDataCollectors[0];
                                        // Find the methods matching this data collector setting
                                        List<MethodCallLine> methodCallLinesMatchingMIDC = methodCallLinesInSegmentList.Where(m => m.Class == methodInvocationDataCollector.MatchClass && m.Method == methodInvocationDataCollector.MatchMethod).ToList();
                                        if (methodCallLinesMatchingMIDC.Count > 0)
                                        {
                                            // Found some lines, let's enumerate data collectors that were actually collected and match them
                                            List<string> businessDataReferenceList = new List<string>(methodInvocationDataCollectors.Count);
                                            foreach (BusinessData businessData in businessDataCodeListInThisSegment)
                                            {
                                                if (methodInvocationDataCollectors.Count(m => m.DataGathererName == businessData.DataName) > 0)
                                                {
                                                    businessDataReferenceList.Add(String.Format("{0}={1} ({2})", businessData.DataName, businessData.DataValue, methodInvocationDataCollector.CollectorName));
                                                }
                                            }

                                            // Now that we have the list of those data collectors, put them into the method line
                                            foreach (MethodCallLine methodCallLine in methodCallLinesMatchingMIDC)
                                            {
                                                methodCallLine.NumMIDCs = businessDataReferenceList.Count;

                                                if (methodCallLine.NumMIDCs == 1)
                                                {
                                                    methodCallLine.MIDCs = businessDataReferenceList[0];
                                                }
                                                else
                                                {
                                                    if (businessDataReferenceList.Count > 0)
                                                    {
                                                        StringBuilder sb = new StringBuilder(64 * methodCallLine.NumMIDCs);
                                                        foreach (string businessDataReference in businessDataReferenceList)
                                                        {
                                                            sb.AppendFormat("{0};\n", businessDataReference);
                                                        }
                                                        sb.Remove(sb.Length - 1, 1);
                                                        methodCallLine.MIDCs = sb.ToString();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // Process all method call lines to generate Occurrences list, finding and counting all the unique values
                                Dictionary<string, MethodCallLine> methodCallLinesOccurrencesInSegmentDictionary = new Dictionary<string, MethodCallLine>(methodCallLinesInSegmentList.Count);
                                foreach (MethodCallLine methodCallLine in methodCallLinesInSegmentList)
                                {
                                    if (methodCallLinesOccurrencesInSegmentDictionary.ContainsKey(methodCallLine.FullName) == false)
                                    {
                                        // Add new
                                        MethodCallLine methodCallLineOccurrence = methodCallLine.Clone();
                                        methodCallLineOccurrence.NumCalls = 1;
                                        // Correct the time from the offset originally associated with this method call back to the segment's time
                                        methodCallLineOccurrence.OccurredUtc = segment.OccurredUtc;
                                        methodCallLineOccurrence.Occurred = segment.Occurred;
                                        methodCallLinesOccurrencesInSegmentDictionary.Add(methodCallLine.FullName, methodCallLineOccurrence);
                                    }
                                    else
                                    {
                                        // Adjust existing
                                        MethodCallLine methodCallLineOccurrence = methodCallLinesOccurrencesInSegmentDictionary[methodCallLine.FullName];

                                        methodCallLineOccurrence.NumCalls++;

                                        methodCallLineOccurrence.Exec = methodCallLineOccurrence.Exec + methodCallLine.Exec;
                                        methodCallLineOccurrence.Wait = methodCallLineOccurrence.Wait + methodCallLine.Wait;
                                        methodCallLineOccurrence.Block = methodCallLineOccurrence.Block + methodCallLine.Block;
                                        methodCallLineOccurrence.CPU = methodCallLineOccurrence.CPU + methodCallLine.CPU;

                                        methodCallLineOccurrence.NumExits = methodCallLineOccurrence.NumExits + methodCallLine.NumExits;
                                        methodCallLineOccurrence.NumSEPs = methodCallLineOccurrence.NumSEPs + methodCallLine.NumSEPs;
                                        methodCallLineOccurrence.NumMIDCs = methodCallLineOccurrence.NumMIDCs + methodCallLine.NumMIDCs;
                                        methodCallLineOccurrence.NumChildren = methodCallLineOccurrence.NumChildren + methodCallLine.NumChildren;
                                    }
                                }
                                List<MethodCallLine> methodCallLinesOccurrencesInSegmentList = new List<MethodCallLine>(methodCallLinesOccurrencesInSegmentDictionary.Count);
                                methodCallLinesOccurrencesInSegmentList = methodCallLinesOccurrencesInSegmentDictionary.Values.ToList();
                                methodCallLinesOccurrencesInSegmentList = methodCallLinesOccurrencesInSegmentList.OrderBy(m => m.FullName).ToList();
                                foreach (MethodCallLine methodCallLine in methodCallLinesOccurrencesInSegmentList)
                                {
                                    methodCallLine.ExecRange = getDurationRangeAsString(methodCallLine.Exec);
                                }

                                // Process all method call lines into folded call stacks for flame graphs
                                List<MethodCallLine> methodCallLinesLeaves = null;
                                if (methodCallLinesInSegmentList.Count > 0)
                                {
                                    // Find all leaves and go up from them there
                                    if (methodCallLinesInSegmentList.Count == 1)
                                    {
                                        methodCallLinesLeaves = methodCallLinesInSegmentList;
                                    }
                                    else
                                    {
                                        methodCallLinesLeaves = methodCallLinesInSegmentList.Where(m => m.ElementType == MethodCallLineElementType.Leaf).ToList();
                                    }

                                    foreach (MethodCallLine methodCallLineLeaf in methodCallLinesLeaves)
                                    {
                                        FoldedStackLine foldedStackLine = new FoldedStackLine(methodCallLineLeaf, false);
                                        if (foldedCallStacksList.ContainsKey(foldedStackLine.FoldedStack) == true)
                                        {
                                            foldedCallStacksList[foldedStackLine.FoldedStack].AddFoldedStackLine(foldedStackLine);
                                        }
                                        else
                                        {
                                            foldedCallStacksList.Add(foldedStackLine.FoldedStack, foldedStackLine);
                                        }

                                        FoldedStackLine foldedStackLineWithTime = new FoldedStackLine(methodCallLineLeaf, true);
                                        if (foldedCallStacksWithTimeList.ContainsKey(foldedStackLineWithTime.FoldedStack) == true)
                                        {
                                            foldedCallStacksWithTimeList[foldedStackLineWithTime.FoldedStack].AddFoldedStackLine(foldedStackLineWithTime);
                                        }
                                        else
                                        {
                                            foldedCallStacksWithTimeList.Add(foldedStackLineWithTime.FoldedStack, foldedStackLineWithTime);
                                        }
                                    }
                                }

                                #endregion

                                #region Update call chains and call types from exits into segment

                                SortedDictionary<string, int> exitTypesSegment = new SortedDictionary<string, int>();

                                StringBuilder sbCallChainsSegment = new StringBuilder(128 * callChainsSegment.Count);
                                foreach (var callChain in callChainsSegment)
                                {
                                    sbCallChainsSegment.AppendFormat("{0}\n", callChain.Value);
                                    if (exitTypesSegment.ContainsKey(callChain.Value.ExitType) == false)
                                    {
                                        exitTypesSegment.Add(callChain.Value.ExitType, 0);
                                    }
                                    exitTypesSegment[callChain.Value.ExitType] = exitTypesSegment[callChain.Value.ExitType] + callChain.Value.CallTimings.Count;
                                }
                                if (sbCallChainsSegment.Length > 1) { sbCallChainsSegment.Remove(sbCallChainsSegment.Length - 1, 1); }
                                segment.CallChains = sbCallChainsSegment.ToString();

                                StringBuilder sbExitTypesSegment = new StringBuilder(10 * exitTypesSegment.Count);
                                foreach (var exitType in exitTypesSegment)
                                {
                                    sbExitTypesSegment.AppendFormat("{0}={1}\n", exitType.Key, exitType.Value);
                                }

                                if (sbExitTypesSegment.Length > 1) { sbExitTypesSegment.Remove(sbExitTypesSegment.Length - 1, 1); }
                                segment.ExitTypes = sbExitTypesSegment.ToString();

                                #endregion

                                #region Update counts of calls and types of destinations for Segment

                                segment.NumCallsToTiers = exitCallsListInThisSegment.Where(e => e.ToEntityType == APMTier.ENTITY_TYPE).Sum(e => e.NumCalls);
                                segment.NumCallsToBackends = exitCallsListInThisSegment.Where(e => e.ToEntityType == Backend.ENTITY_TYPE).Sum(e => e.NumCalls);
                                segment.NumCallsToApplications = exitCallsListInThisSegment.Where(e => e.ToEntityType == APMApplication.ENTITY_TYPE).Sum(e => e.NumCalls);

                                segment.NumCalledTiers = exitCallsListInThisSegment.Where(e => e.ToEntityType == APMTier.ENTITY_TYPE).GroupBy(e => e.ToEntityName).Count();
                                segment.NumCalledBackends = exitCallsListInThisSegment.Where(e => e.ToEntityType == Backend.ENTITY_TYPE).GroupBy(e => e.ToEntityName).Count();
                                segment.NumCalledApplications = exitCallsListInThisSegment.Where(e => e.ToEntityType == APMApplication.ENTITY_TYPE).GroupBy(e => e.ToEntityName).Count();

                                segment.NumSEPs = serviceEndpointCallsListInThisSegment.Count();

                                segment.NumHTTPDCs = businessDataListInThisSegment.Where(d => d.DataType == "HTTP").Count();
                                segment.NumMIDCs = businessDataListInThisSegment.Where(d => d.DataType == "Code").Count();

                                #endregion

                                // Add the created entities to the container
                                indexedSnapshotResults.Segments.Add(segment);
                                indexedSnapshotResults.ExitCalls.AddRange(exitCallsListInThisSegment);
                                indexedSnapshotResults.ServiceEndpointCalls.AddRange(serviceEndpointCallsListInThisSegment);
                                indexedSnapshotResults.DetectedErrors.AddRange(detectedErrorsListInThisSegment);
                                indexedSnapshotResults.BusinessData.AddRange(businessDataListInThisSegment);
                                indexedSnapshotResults.MethodCallLines.AddRange(methodCallLinesInSegmentList);
                                indexedSnapshotResults.MethodCallLineOccurrences.AddRange(methodCallLinesOccurrencesInSegmentList);
                            }
                        }

                        #endregion

                        #region Calculate End to End Duration time

                        if (indexedSnapshotResults.Segments.Count == 0)
                        {
                            snapshot.EndToEndDuration = snapshot.Duration;
                        }
                        else if (indexedSnapshotResults.Segments.Count == 1)
                        {
                            snapshot.EndToEndDuration = indexedSnapshotResults.Segments[0].Duration;
                        }
                        else
                        {
                            List<Segment> segmentsListSorted = indexedSnapshotResults.Segments.OrderBy(s => s.Occurred).ToList();
                            Segment segmentFirst = segmentsListSorted[0];
                            Segment segmentLast = segmentsListSorted[segmentsListSorted.Count - 1];
                            snapshot.EndToEndDuration = Math.Abs((long)(segmentLast.Occurred - segmentFirst.Occurred).TotalMilliseconds) + segmentLast.Duration;
                            if (snapshot.EndToEndDuration < snapshot.Duration)
                            {
                                snapshot.EndToEndDuration = snapshot.Duration;
                            }

                            if (snapshot.EndToEndDuration != snapshot.Duration)
                            {
                                snapshot.IsEndToEndDurationDifferent = true;
                            }
                        }
                        snapshot.EndToEndDurationRange = getDurationRangeAsString(snapshot.EndToEndDuration);

                        #endregion

                        #region Build call timeline for the Segments

                        foreach (Segment segment in indexedSnapshotResults.Segments)
                        {
                            // Timeline looks like that:
                            //^ ---------------^---1------------^------2---------^---------3-----^-------------4 -^
                            //-
                            //-
                            //-
                            //-
                            //^ ---------------
                            //                -
                            //                -
                            //                ^ ---------------
                            //                                 -
                            //                                 -
                            //                                 -
                            //                                 ^ ---------------
                            //                                                  -
                            //                                                  -
                            //                                                  ^ ---------------
                            //                                                                  -
                            //                                                                  ^ ---------------
                            //                                                                                   -
                            //                                                                                   -
                            //                                                                                   -
                            //                                                                                   ^
                            //                                                                                   -
                            //                                                                                   -
                            // Where: 
                            //      - each dash - character is a certain duration, resolution is determined by the overall duration of the snapshot
                            //      - space character begins is offset between the time of Segment and time of Snapshot
                            //      - exits are signified by caret mark ^ character
                            //      - numbers are either 1 second or 10 second marks, depending on resolution
                            // To calculate
                            // 1) Evaluate interval range based on overall size of snapshot
                            // 2) For each interval, put dash - characters to signify exec time
                            // 3) For each of the exit calls in MethodCallLine, replace = character with caret mark ^ character
                            // 4) Go from beginning of Snapshot time to beginning of Segment, putting space character for each interval to signify time offset

                            // Assume duration of segment to be max 2 minutes. Of course, StringBuilder will adjust if it is longer
                            StringBuilder sbTimeline = new StringBuilder(120);

                            // Determine Timeline resolution
                            int timelineResolutionInMS = 0;
                            int timelineSignificantMarksAt = 0;

                            if (snapshot.EndToEndDuration <= 1000)
                            {
                                timelineResolutionInMS = 10;
                                timelineSignificantMarksAt = 100;
                            }
                            else if (snapshot.EndToEndDuration <= 5000)
                            {
                                timelineResolutionInMS = 50;
                                timelineSignificantMarksAt = 20;
                            }
                            else if (snapshot.EndToEndDuration <= 10000)
                            {
                                timelineResolutionInMS = 100;
                                timelineSignificantMarksAt = 10;
                            }
                            else if (snapshot.EndToEndDuration <= 25000)
                            {
                                timelineResolutionInMS = 250;
                                timelineSignificantMarksAt = 40;
                            }
                            else if (snapshot.EndToEndDuration <= 50000)
                            {
                                timelineResolutionInMS = 500;
                                timelineSignificantMarksAt = 20;
                            }
                            else
                            {
                                timelineResolutionInMS = 1000;
                                timelineSignificantMarksAt = 10;
                            }
                            segment.TimelineResolution = timelineResolutionInMS;

                            // Display this Segment's execution time as line of dashes -
                            int numIntervalsSegmentDuration = (int)Math.Round((double)(segment.Duration / timelineResolutionInMS), 0);
                            if (numIntervalsSegmentDuration == 0)
                            {
                                numIntervalsSegmentDuration = 1;
                            }
                            sbTimeline.Append(new String('-', numIntervalsSegmentDuration));

                            // Put significant area marks at 1 or 10 second intervals
                            int intervalCounter = 1;
                            for (int i = timelineSignificantMarksAt; i < sbTimeline.Length; i = i + timelineSignificantMarksAt)
                            {
                                // Calculate number of seconds
                                int secondsElapsed = (timelineResolutionInMS * timelineSignificantMarksAt) / 1000 * intervalCounter;
                                string secondsElapsedString = secondsElapsed.ToString();

                                // Insert number of seconds, aligned left
                                sbTimeline.Insert(i - 1, secondsElapsedString);

                                // Now remove the same number of characters
                                if (i - 1 + secondsElapsedString.Length > numIntervalsSegmentDuration)
                                {
                                    // We went past the end of the buffer
                                    sbTimeline.Remove(numIntervalsSegmentDuration, sbTimeline.Length - numIntervalsSegmentDuration);
                                }
                                else
                                {
                                    sbTimeline.Remove(i - 1 + secondsElapsedString.Length, secondsElapsedString.Length);
                                }

                                // Go to next counter
                                intervalCounter++;
                            }

                            // Represent the exits from MethodCallLines with caret ^ characters
                            List<MethodCallLine> methodCallLinesOccurrencesExits = indexedSnapshotResults.MethodCallLines.Where(m => m.SegmentID == segment.SegmentID && m.NumExits > 0).ToList();
                            foreach (MethodCallLine methodCallLineExit in methodCallLinesOccurrencesExits)
                            {
                                int numIntervalsOffsetFromSegmentStartToExit = (int)Math.Round((double)(methodCallLineExit.ExecToHere / timelineResolutionInMS), 0);
                                if (numIntervalsOffsetFromSegmentStartToExit > sbTimeline.Length - 1)
                                {
                                    numIntervalsOffsetFromSegmentStartToExit = sbTimeline.Length - 1;
                                }
                                sbTimeline[numIntervalsOffsetFromSegmentStartToExit] = '^';
                            }

                            // Add indent away from the beginning of Snapshot time to this Segment
                            int numIntervalsBetweenSnapshotStartandSegmentStart = (int)Math.Round((segment.Occurred - snapshot.Occurred).TotalMilliseconds / timelineResolutionInMS, 0);
                            if (numIntervalsBetweenSnapshotStartandSegmentStart > 0)
                            {
                                sbTimeline.Insert(0, new String(' ', numIntervalsBetweenSnapshotStartandSegmentStart));
                            }
                            else if (numIntervalsBetweenSnapshotStartandSegmentStart < 0)
                            {
                                // This segment happened earlier then Snapshot
                                // Happens sometimes when the clock on originating Tier is behind the clocks on the downstream tiers
                                // Indicate that this segment came from earlier using < character
                                for (int i = 0; i < Math.Abs(numIntervalsBetweenSnapshotStartandSegmentStart) - 1; i++)
                                {
                                    if (sbTimeline.Length > 0)
                                    {
                                        sbTimeline.Remove(0, 1);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                if (sbTimeline.Length > 0)
                                {
                                    sbTimeline[0] = '<';
                                }
                                else if (sbTimeline.Length == 0)
                                {
                                    sbTimeline.Append('-');
                                }
                            }

                            // Finally, render the timeline as a pretty string. Whew.
                            segment.Timeline = sbTimeline.ToString();
                        }

                        #endregion

                        #region Update call chains from segments into snapshot

                        SortedDictionary<string, int> exitTypesSnapshot = new SortedDictionary<string, int>();

                        StringBuilder sbCallChainsSnapshot = new StringBuilder(128 * callChainsSnapshot.Count);
                        foreach (var callChain in callChainsSnapshot)
                        {
                            sbCallChainsSnapshot.AppendFormat("{0};\n", callChain.Value);
                            if (exitTypesSnapshot.ContainsKey(callChain.Value.ExitType) == false)
                            {
                                exitTypesSnapshot.Add(callChain.Value.ExitType, 0);
                            }
                            exitTypesSnapshot[callChain.Value.ExitType] = exitTypesSnapshot[callChain.Value.ExitType] + callChain.Value.CallTimings.Count;
                        }
                        if (sbCallChainsSnapshot.Length > 1) { sbCallChainsSnapshot.Remove(sbCallChainsSnapshot.Length - 1, 1); }
                        snapshot.CallChains = sbCallChainsSnapshot.ToString();

                        StringBuilder sbExitTypesSnapshot = new StringBuilder(10 * exitTypesSnapshot.Count);
                        foreach (var exitType in exitTypesSnapshot)
                        {
                            sbExitTypesSnapshot.AppendFormat("{0}={1}\n", exitType.Key, exitType.Value);
                        }

                        if (sbExitTypesSnapshot.Length > 1) { sbExitTypesSnapshot.Remove(sbExitTypesSnapshot.Length - 1, 1); }
                        snapshot.ExitTypes = sbExitTypesSnapshot.ToString();

                        #endregion

                        #region Update various counts for Snapshot columns

                        snapshot.NumErrors = indexedSnapshotResults.Segments.Sum(s => s.NumErrors);

                        snapshot.NumSegments = indexedSnapshotResults.Segments.Count;
                        snapshot.NumCallGraphs = indexedSnapshotResults.Segments.Count(s => s.CallGraphType != "NONE");

                        snapshot.NumCallsToTiers = indexedSnapshotResults.Segments.Sum(s => s.NumCallsToTiers);
                        snapshot.NumCallsToBackends = indexedSnapshotResults.Segments.Sum(s => s.NumCallsToBackends);
                        snapshot.NumCallsToApplications = indexedSnapshotResults.Segments.Sum(s => s.NumCallsToApplications);

                        snapshot.NumCalledTiers = indexedSnapshotResults.Segments.Sum(s => s.NumCalledTiers);
                        snapshot.NumCalledBackends = indexedSnapshotResults.Segments.Sum(s => s.NumCalledBackends);
                        snapshot.NumCalledApplications = indexedSnapshotResults.Segments.Sum(s => s.NumCalledApplications);

                        snapshot.NumSEPs = indexedSnapshotResults.Segments.Sum(s => s.NumSEPs);

                        snapshot.NumHTTPDCs = indexedSnapshotResults.Segments.Sum(s => s.NumHTTPDCs);
                        snapshot.NumMIDCs = indexedSnapshotResults.Segments.Sum(s => s.NumMIDCs);

                        #endregion
                    }

                    indexedSnapshotResults.Snapshots.Add(snapshot);

                    #endregion

                    #region Save results

                    indexedSnapshotsResults.Snapshots.AddRange(indexedSnapshotResults.Snapshots);
                    indexedSnapshotsResults.Segments.AddRange(indexedSnapshotResults.Segments);
                    indexedSnapshotsResults.ExitCalls.AddRange(indexedSnapshotResults.ExitCalls);
                    indexedSnapshotsResults.ServiceEndpointCalls.AddRange(indexedSnapshotResults.ServiceEndpointCalls);
                    indexedSnapshotsResults.DetectedErrors.AddRange(indexedSnapshotResults.DetectedErrors);
                    indexedSnapshotsResults.BusinessData.AddRange(indexedSnapshotResults.BusinessData);
                    indexedSnapshotsResults.MethodCallLines.AddRange(indexedSnapshotResults.MethodCallLines);
                    indexedSnapshotsResults.MethodCallLineOccurrences.AddRange(indexedSnapshotResults.MethodCallLineOccurrences);

                    // Save the folded call stacks into the results
                    if (indexedSnapshotsResults.FoldedCallStacksNodesNoTiming.ContainsKey(snapshot.NodeID) == false)
                    {
                        indexedSnapshotsResults.FoldedCallStacksNodesNoTiming[snapshot.NodeID] = new Dictionary<string, FoldedStackLine>(50);
                        indexedSnapshotsResults.FoldedCallStacksNodesWithTiming[snapshot.NodeID] = new Dictionary<string, FoldedStackLine>(50);
                    }
                    if (indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming.ContainsKey(snapshot.BTID) == false)
                    {
                        indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming[snapshot.BTID] = new Dictionary<string, FoldedStackLine>(50);
                        indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsWithTiming[snapshot.BTID] = new Dictionary<string, FoldedStackLine>(50);
                    }

                    if (foldedCallStacksList != null)
                    {
                        addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksNodesNoTiming[snapshot.NodeID], foldedCallStacksList.Values.ToList<FoldedStackLine>());
                        addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsNoTiming[snapshot.BTID], foldedCallStacksList.Values.ToList<FoldedStackLine>());
                    }

                    if (foldedCallStacksWithTimeList != null)
                    {
                        addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksNodesWithTiming[snapshot.NodeID], foldedCallStacksWithTimeList.Values.ToList<FoldedStackLine>());
                        addFoldedStacks(indexedSnapshotsResults.FoldedCallStacksBusinessTransactionsWithTiming[snapshot.BTID], foldedCallStacksWithTimeList.Values.ToList<FoldedStackLine>());
                    }

                    #endregion
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 100 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }
            }

            return indexedSnapshotsResults;
        }

        private MethodCallLine convertCallGraphChildren_Recursion(
            JToken methodCallLineJSON,
            int currentDepth,
            ref int methodLineCallSequenceNumber,
            List<MethodCallLine> methodCallLinesList,
            List<ServiceEndpointCall> serviceEndpointCallsList,
            List<ExitCall> exitCallsList)
        {
            MethodCallLine methodCallLine = new MethodCallLine();

            methodCallLine.SequenceNumber = methodLineCallSequenceNumber;
            methodLineCallSequenceNumber++;

            // Populate current method call class, methods and types
            methodCallLine.Type = methodCallLineJSON["type"].ToString();
            methodCallLine.PrettyName = methodCallLineJSON["name"].ToString();
            methodCallLine.Class = methodCallLineJSON["className"].ToString();
            methodCallLine.Method = methodCallLineJSON["methodName"].ToString();
            methodCallLine.LineNumber = (int)methodCallLineJSON["lineNumber"];
            if (methodCallLine.LineNumber > 0)
            {
                methodCallLine.FullName = String.Format("{0}:{1}:{2}", methodCallLine.Class, methodCallLine.Method, methodCallLine.LineNumber);
            }
            else
            {
                methodCallLine.FullName = String.Format("{0}:{1}", methodCallLine.Class, methodCallLine.Method);
            }
            methodCallLine.FullNameIndent = String.Format("{0}{1}", new string(' ', currentDepth), methodCallLine.FullName);

            // Fill in Service Endpoints
            if (methodCallLineJSON["serviceEndPointIds"].HasValues == true && serviceEndpointCallsList.Count > 0)
            {
                methodCallLine.NumSEPs = methodCallLineJSON["serviceEndPointIds"].Count();

                List<string> serviceEndpointReferenceList = new List<string>(methodCallLine.NumSEPs);
                foreach (long sepID in methodCallLineJSON["serviceEndPointIds"])
                {
                    ServiceEndpointCall serviceEndpointCall = serviceEndpointCallsList.Where(s => s.SEPID == sepID).FirstOrDefault();
                    if (serviceEndpointCall != null)
                    {
                        serviceEndpointReferenceList.Add(String.Format("{0} ({1})", serviceEndpointCall.SEPName, serviceEndpointCall.SEPType));
                    }
                }

                if (methodCallLine.NumSEPs == 1)
                {
                    methodCallLine.SEPs = serviceEndpointReferenceList[0];
                }
                else
                {
                    StringBuilder sb = new StringBuilder(32 * methodCallLine.NumSEPs);
                    foreach (string serviceEndpointReference in serviceEndpointReferenceList)
                    {
                        sb.AppendFormat("{0};\n", serviceEndpointReference);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    methodCallLine.SEPs = sb.ToString();
                }
            }

            // Fill in Durations
            // We first assume that duration is equal to duration with children. Then when adding children, recalculate, subtracting child's duration
            methodCallLine.ExecTotal = (long)methodCallLineJSON["timeSpentInMilliSec"];
            methodCallLine.Exec = methodCallLine.ExecTotal;
            methodCallLine.WaitTotal = (long)methodCallLineJSON["waitTime"];
            methodCallLine.Wait = methodCallLine.WaitTotal;
            methodCallLine.BlockTotal = (long)methodCallLineJSON["blockTime"];
            methodCallLine.Block = methodCallLine.BlockTotal;
            methodCallLine.CPUTotal = (long)methodCallLineJSON["cpuTime"];
            methodCallLine.CPU = methodCallLine.CPUTotal;

            // Count children
            if (methodCallLineJSON["children"].HasValues == false)
            {
                methodCallLine.NumChildren = 0;
            }
            else
            {
                methodCallLine.NumChildren = methodCallLineJSON["children"].Count();
            }

            // Specify depth
            methodCallLine.Depth = currentDepth;

            // Determine type of this element in the call graph tree
            if (currentDepth == 0)
            {
                methodCallLine.ElementType = MethodCallLineElementType.Root;
            }
            else
            {
                if (methodCallLine.NumChildren == 0)
                {
                    methodCallLine.ElementType = MethodCallLineElementType.Leaf;
                }
                else if (methodCallLine.NumChildren == 1)
                {
                    methodCallLine.ElementType = MethodCallLineElementType.Stem;
                }
                else
                {
                    methodCallLine.ElementType = MethodCallLineElementType.Branch;
                }
            }

            // Fill in exits
            // Frequently, the exits in the list from Segments (passed via exitCallsList parameter to the function), are ordered
            // in the same sequence as the exits encountered during unrolling the call graph tree
            // However, that is not always the case
            // Exceptions appear to be 
            // a) .NET applications 
            // and 
            // b) the database connection acquisition backend calls that are grouped together into number of calls >1 and duration being the Sum(of all)
            // So the logic is to find the Exit by the ordinal location, if that doesn't work, find it by SequenceNumber, and if that doesn't work, by the detail string 
            // The ExitCall.SequenceNumber can look like that:
            // "snapshotSequenceCounter" : "1|6|5"
            // Most of the time the sequence number from segment matches just great to that in the exit call
            // But sometimes there can be an exit that is to the same query, and so its ExitCall.NumCalls will be > 1
            // For those, the UI displays the exit with total number of those, and the call graph has detail
            // Typically these are the calls to database pooling
            if (methodCallLineJSON["exitCalls"].HasValues == true && exitCallsList.Count > 0)
            {
                methodCallLine.NumExits = methodCallLineJSON["exitCalls"].Count();

                List<string> exitCallsReferenceList = new List<string>(methodCallLine.NumExits);

                foreach (JToken exitCallToken in methodCallLineJSON["exitCalls"])
                {
                    ExitCall exitCallForThisExit = null;

                    bool adjustCallDurationInCallChain = false;

                    // First, try by the ordinal value
                    if (exitCallsList.Count > 0)
                    {
                        exitCallForThisExit = exitCallsList[0];
                        if (exitCallForThisExit.SequenceNumber == exitCallToken["snapshotSequenceCounter"].ToString())
                        {
                            if (exitCallForThisExit.NumCalls > 1)
                            {
                                // Found it and it is used more than once
                                adjustCallDurationInCallChain = true;
                            }
                            else
                            {
                                // Found it and it is a singular one
                                exitCallsList.Remove(exitCallForThisExit);
                            }
                        }
                        else
                        {
                            // Not the right one
                            exitCallForThisExit = null;
                        }
                    }

                    // Second, try looking it up by the sequence number
                    if (exitCallForThisExit == null)
                    {
                        exitCallForThisExit = exitCallsList.Where(e => e.SequenceNumber == exitCallToken["snapshotSequenceCounter"].ToString()).FirstOrDefault();
                        if (exitCallForThisExit != null)
                        {
                            if (exitCallForThisExit.NumCalls > 1)
                            {
                                // Found it and it is used more than once
                                adjustCallDurationInCallChain = true;
                            }
                            else
                            {
                                // Found it and it is a singular one
                                exitCallsList.Remove(exitCallForThisExit);
                            }
                        }
                    }

                    // Third, try looking up up by the exact properties
                    if (exitCallForThisExit == null)
                    {
                        adjustCallDurationInCallChain = true;

                        // This must be one of those calls that has more then 1 call, and is grouped
                        // Make up the exit details using the values in the call graph information
                        exitCallForThisExit = exitCallsList.Where(
                            e => e.NumCalls > 1 &&
                            e.Detail == exitCallToken["detailString"].ToString() &&
                            e.PropsAll == exitCallToken["propertiesAsString"].ToString()).FirstOrDefault();
                    }

                    // Fourth, still don't have an exit from segment data
                    // Manually create an exit from the Call Graph value
                    if (exitCallForThisExit == null)
                    {
                        adjustCallDurationInCallChain = false;

                        exitCallForThisExit = new ExitCall();
                        exitCallForThisExit.Duration = (long)exitCallToken["timeTakenInMillis"];
                        exitCallForThisExit.IsAsync = ((bool)exitCallToken["exitPointCall"]["synchronous"] == false);
                        exitCallForThisExit.ExitType = exitCallToken["type"].ToString();
                        exitCallForThisExit.Detail = exitCallToken["detailString"].ToString();

                        JToken goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "to").FirstOrDefault();
                        if (goingToProperty != null)
                        {
                            exitCallForThisExit.ToEntityName = goingToProperty["value"].ToString();
                        }
                        goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "from").FirstOrDefault();
                        string callChainForThisSegment = "(Generated From Call Graph:Unknown)";
                        if (goingToProperty != null)
                        {
                            callChainForThisSegment = String.Format("(Generated From Call Graph:{0})", goingToProperty["value"].ToString());
                        }
                        if (exitCallForThisExit.IsAsync == false)
                        {
                            exitCallForThisExit.CallChain = String.Format("{0}->[{1}]:[{3} ms]-><{2}>", callChainForThisSegment, exitCallForThisExit.ExitType, exitCallForThisExit.ToEntityName, exitCallForThisExit.Duration);
                        }
                        else
                        {
                            exitCallForThisExit.CallChain = String.Format("{0}->[{1}]:[{3} ms async]-><{2}>", callChainForThisSegment, exitCallForThisExit.ExitType, exitCallForThisExit.ToEntityName, exitCallForThisExit.Duration);
                        }
                    }

                    // Finally, here we should have an exit from the segment data
                    string callChain = exitCallForThisExit.CallChain;
                    if (adjustCallDurationInCallChain == true)
                    {
                        // Call duration in the exit that has more then one call (typically database connection acquisition
                        // would have this call chain
                        // (ECommerce-Services)->[WEB_SERVICE]->(Inventory-Services)->[JDBC]:[20 ms]-><INVENTORY-MySQL DB-DB-5.7.13-0ubuntu0.16.04.2>
                        // Here we replace this                                               ^^, which is a sum of all the calls in the call graph
                        // with the value from the exit in the call graph

                        Regex regexDuration = new Regex(@"(.*\[)(\d*)( ms.*\].*)", RegexOptions.IgnoreCase);
                        callChain = regexDuration.Replace(callChain,
                            m => String.Format(
                                "{0}{1}{2}",
                                m.Groups[1].Value,
                                exitCallToken["timeTakenInMillis"],
                                m.Groups[3].Value));
                    }

                    // Prepare the rendered value
                    if (exitCallForThisExit.HasErrors == false)
                    {
                        if (exitCallForThisExit.ToSegmentID != 0)
                        {
                            exitCallsReferenceList.Add(String.Format("{0}->/{1}/ {2}", callChain, exitCallForThisExit.ToSegmentID, exitCallForThisExit.Detail));
                        }
                        else
                        {
                            exitCallsReferenceList.Add(String.Format("{0} {1}", callChain, exitCallForThisExit.Detail));
                        }
                    }
                    else
                    {
                        exitCallsReferenceList.Add(String.Format("{0} {1} Error {2}", callChain, exitCallForThisExit.Detail, exitCallForThisExit.ErrorDetail));
                        methodCallLine.HasErrors = true;
                    }
                }

                // Finally, render the value out of all the exits in here
                if (methodCallLine.NumExits == 1 && exitCallsReferenceList.Count > 0)
                {
                    methodCallLine.ExitCalls = exitCallsReferenceList[0];
                }
                else
                {
                    StringBuilder sb = new StringBuilder(32 * methodCallLine.NumExits);
                    foreach (string exitCallsReference in exitCallsReferenceList)
                    {
                        sb.AppendFormat("{0};\n", exitCallsReference);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    methodCallLine.ExitCalls = sb.ToString();
                }
            }

            // Add to total list
            methodCallLinesList.Add(methodCallLine);

            // Go through the children, recursively. Love recursion
            if (methodCallLine.NumChildren > 0)
            {
                List<MethodCallLine> methodCallLinesAllChildren = new List<MethodCallLine>(10);
                foreach (JToken childMethodCallLineJSON in (JArray)methodCallLineJSON["children"])
                {
                    currentDepth++;

                    MethodCallLine methodCallLineChild = convertCallGraphChildren_Recursion(childMethodCallLineJSON, currentDepth, ref methodLineCallSequenceNumber, methodCallLinesList, serviceEndpointCallsList, exitCallsList);

                    // Now that we measured child, subtract its duration from the current node
                    methodCallLine.Exec = methodCallLine.Exec - methodCallLineChild.ExecTotal;
                    methodCallLine.Wait = methodCallLine.Wait - methodCallLineChild.WaitTotal;
                    methodCallLine.Block = methodCallLine.Block - methodCallLineChild.BlockTotal;
                    methodCallLine.CPU = methodCallLine.CPU - methodCallLineChild.CPUTotal;

                    currentDepth--;
                }
            }

            // Calculate the duration range
            methodCallLine.ExecRange = getDurationRangeAsString(methodCallLine.Exec);

            return methodCallLine;
        }

        private List<MethodCallLine> convertCallGraphChildren_Stack(
            JToken methodCallLineJSONRoot,
            List<ServiceEndpointCall> serviceEndpointCallsList,
            List<ExitCall> exitCallsList)
        {
            List<MethodCallLine> methodCallLinesList = new List<MethodCallLine>(500);
            List<MethodCallLine> methodCallLinesLeafList = new List<MethodCallLine>(10);

            if (methodCallLineJSONRoot == null)
            {
                return methodCallLinesList;
            }

            // Assume depth of at least 100
            Stack<JToken> stackOfMethodCallLineJSONs = new Stack<JToken>(100);
            Stack<MethodCallLine> stackOfParentMethodCallLines = new Stack<MethodCallLine>(100);

            // Add the first one 
            stackOfMethodCallLineJSONs.Push(methodCallLineJSONRoot);

            int methodLineCallSequenceNumber = 0;

            // Let's scroll through, it is just like a binary tree, except with multiple children, meaning that it has left and right, and right again
            while (stackOfMethodCallLineJSONs.Count > 0)
            {
                JToken methodCallLineJSON = stackOfMethodCallLineJSONs.Pop();
                MethodCallLine methodCallLineParent = null;
                if (stackOfParentMethodCallLines.Count > 0)
                {
                    methodCallLineParent = stackOfParentMethodCallLines.Pop();
                }

                #region Populate MethodCallLine

                MethodCallLine methodCallLine = new MethodCallLine();

                methodCallLine.Parent = methodCallLineParent;

                methodCallLine.SequenceNumber = methodLineCallSequenceNumber;
                methodLineCallSequenceNumber++;

                // Populate current method call class, methods and types
                methodCallLine.Type = methodCallLineJSON["type"].ToString();
                methodCallLine.PrettyName = methodCallLineJSON["name"].ToString();
                methodCallLine.Class = methodCallLineJSON["className"].ToString();
                methodCallLine.Method = methodCallLineJSON["methodName"].ToString();
                methodCallLine.LineNumber = (int)methodCallLineJSON["lineNumber"];
                if (methodCallLine.Type == "JS")
                {
                    // Node.js call graphs should use pretty name
                    if (methodCallLine.LineNumber > 0)
                    {
                        methodCallLine.FullName = String.Format("{0}:{1}", methodCallLine.PrettyName, methodCallLine.LineNumber);
                    }
                    else
                    {
                        methodCallLine.FullName = methodCallLine.PrettyName;
                    }
                }
                else
                {
                    if (methodCallLine.LineNumber > 0)
                    {
                        methodCallLine.FullName = String.Format("{0}:{1}:{2}", methodCallLine.Class, methodCallLine.Method, methodCallLine.LineNumber);
                    }
                    else
                    {
                        methodCallLine.FullName = String.Format("{0}:{1}", methodCallLine.Class, methodCallLine.Method);
                    }
                }

                // Specify depth
                if (methodCallLine.Parent == null)
                {
                    methodCallLine.Depth = 0;
                }
                else
                {
                    methodCallLine.Depth = methodCallLine.Parent.Depth + 1;
                }

                methodCallLine.FullNameIndent = String.Format("{0}{1}", new string(' ', methodCallLine.Depth), methodCallLine.FullName);

                // Fill in Service Endpoints
                if (methodCallLineJSON["serviceEndPointIds"].HasValues == true && serviceEndpointCallsList.Count > 0)
                {
                    methodCallLine.NumSEPs = methodCallLineJSON["serviceEndPointIds"].Count();

                    List<string> serviceEndpointReferenceList = new List<string>(methodCallLine.NumSEPs);
                    foreach (long sepID in methodCallLineJSON["serviceEndPointIds"])
                    {
                        ServiceEndpointCall serviceEndpointCall = serviceEndpointCallsList.Where(s => s.SEPID == sepID).FirstOrDefault();
                        if (serviceEndpointCall != null)
                        {
                            serviceEndpointReferenceList.Add(String.Format("{0} ({1})", serviceEndpointCall.SEPName, serviceEndpointCall.SEPType));
                        }
                    }

                    if (serviceEndpointReferenceList.Count > 0)
                    {
                        if (methodCallLine.NumSEPs == 1)
                        {
                            methodCallLine.SEPs = serviceEndpointReferenceList[0];
                        }
                        else
                        {
                            StringBuilder sb = new StringBuilder(32 * methodCallLine.NumSEPs);
                            foreach (string serviceEndpointReference in serviceEndpointReferenceList)
                            {
                                sb.AppendFormat("{0};\n", serviceEndpointReference);
                            }
                            sb.Remove(sb.Length - 1, 1);
                            methodCallLine.SEPs = sb.ToString();
                        }
                    }
                }

                // Fill in Durations
                // We first assume that duration is equal to duration with children. Then when adding children, recalculate, subtracting child's duration
                methodCallLine.ExecTotal = (long)methodCallLineJSON["timeSpentInMilliSec"];
                methodCallLine.Exec = methodCallLine.ExecTotal;
                methodCallLine.WaitTotal = (long)methodCallLineJSON["waitTime"];
                methodCallLine.Wait = methodCallLine.WaitTotal;
                methodCallLine.BlockTotal = (long)methodCallLineJSON["blockTime"];
                methodCallLine.Block = methodCallLine.BlockTotal;
                methodCallLine.CPUTotal = (long)methodCallLineJSON["cpuTime"];
                methodCallLine.CPU = methodCallLine.CPUTotal;

                if (methodCallLineParent != null)
                {
                    methodCallLineParent.Exec = methodCallLineParent.Exec - methodCallLine.ExecTotal;
                    methodCallLineParent.Wait = methodCallLineParent.Wait - methodCallLine.WaitTotal;
                    methodCallLineParent.Block = methodCallLineParent.Block - methodCallLine.BlockTotal;
                    methodCallLineParent.CPU = methodCallLineParent.CPU - methodCallLine.CPUTotal;
                }

                // Count children
                if (methodCallLineJSON["children"].HasValues == false)
                {
                    methodCallLine.NumChildren = 0;
                }
                else
                {
                    methodCallLine.NumChildren = methodCallLineJSON["children"].Count();
                }

                // Determine type of this element in the call graph tree
                if (methodCallLine.Depth == 0)
                {
                    methodCallLine.ElementType = MethodCallLineElementType.Root;
                }
                else
                {
                    if (methodCallLine.NumChildren == 0)
                    {
                        methodCallLine.ElementType = MethodCallLineElementType.Leaf;
                        // Remember this as the bottom of call graph so we can walk from up here calculating durations
                        methodCallLinesLeafList.Add(methodCallLine);
                    }
                    else if (methodCallLine.NumChildren == 1)
                    {
                        methodCallLine.ElementType = MethodCallLineElementType.Stem;
                    }
                    else
                    {
                        methodCallLine.ElementType = MethodCallLineElementType.Branch;
                    }
                }

                // Fill in exits
                // Frequently, the exits in the list from Segments (passed via exitCallsList parameter to the function), are ordered
                // in the same sequence as the exits encountered during unrolling the call graph tree
                // However, that is not always the case
                // Exceptions appear to be 
                // a) .NET applications 
                // and 
                // b) the database connection acquisition backend calls that are grouped together into number of calls >1 and duration being the Sum(of all)
                // So the logic is to find the Exit by the ordinal location, if that doesn't work, find it by SequenceNumber, and if that doesn't work, by the detail string 
                // The ExitCall.SequenceNumber can look like that:
                // "snapshotSequenceCounter" : "1|6|5"
                // Most of the time the sequence number from segment matches just great to that in the exit call
                // But sometimes there can be an exit that is to the same query, and so its ExitCall.NumCalls will be > 1
                // For those, the UI displays the exit with total number of those, and the call graph has detail
                // Typically these are the calls to database pooling
                if (methodCallLineJSON["exitCalls"].HasValues == true && exitCallsList.Count > 0)
                {
                    methodCallLine.NumExits = methodCallLineJSON["exitCalls"].Count();

                    List<string> exitCallsReferenceList = new List<string>(methodCallLine.NumExits);

                    foreach (JToken exitCallToken in methodCallLineJSON["exitCalls"])
                    {
                        ExitCall exitCallForThisExit = null;

                        bool adjustCallDurationInCallChain = false;

                        // First, try by the ordinal value
                        if (exitCallsList.Count > 0)
                        {
                            exitCallForThisExit = exitCallsList[0];
                            if (exitCallForThisExit.SequenceNumber == exitCallToken["snapshotSequenceCounter"].ToString())
                            {
                                if (exitCallForThisExit.NumCalls > 1)
                                {
                                    // Found it and it is used more than once
                                    adjustCallDurationInCallChain = true;
                                }
                                else
                                {
                                    // Found it and it is a singular one
                                    exitCallsList.Remove(exitCallForThisExit);
                                }
                            }
                            else
                            {
                                // Not the right one
                                exitCallForThisExit = null;
                            }
                        }

                        // Second, try looking it up by the sequence number
                        if (exitCallForThisExit == null)
                        {
                            exitCallForThisExit = exitCallsList.Where(e => e.SequenceNumber == exitCallToken["snapshotSequenceCounter"].ToString()).FirstOrDefault();
                            if (exitCallForThisExit != null)
                            {
                                if (exitCallForThisExit.NumCalls > 1)
                                {
                                    // Found it and it is used more than once
                                    adjustCallDurationInCallChain = true;
                                }
                                else
                                {
                                    // Found it and it is a singular one
                                    exitCallsList.Remove(exitCallForThisExit);
                                }
                            }
                        }

                        // Third, try looking up up by the exact properties
                        if (exitCallForThisExit == null)
                        {
                            adjustCallDurationInCallChain = true;

                            // This must be one of those calls that has more then 1 call, and is grouped
                            // Make up the exit details using the values in the call graph information
                            exitCallForThisExit = exitCallsList.Where(
                                e => e.NumCalls > 1 &&
                                e.Detail == exitCallToken["detailString"].ToString() &&
                                e.PropsAll == exitCallToken["propertiesAsString"].ToString()).FirstOrDefault();
                        }

                        // Fourth, still don't have an exit from segment data
                        // Manually create an exit from the Call Graph value
                        if (exitCallForThisExit == null)
                        {
                            adjustCallDurationInCallChain = false;

                            exitCallForThisExit = new ExitCall();
                            exitCallForThisExit.Duration = (long)exitCallToken["timeTakenInMillis"];
                            exitCallForThisExit.IsAsync = ((bool)exitCallToken["exitPointCall"]["synchronous"] == false);
                            exitCallForThisExit.ExitType = exitCallToken["type"].ToString();
                            exitCallForThisExit.Detail = exitCallToken["detailString"].ToString();

                            JToken goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "to").FirstOrDefault();
                            if (goingToProperty != null)
                            {
                                exitCallForThisExit.ToEntityName = goingToProperty["value"].ToString();
                            }
                            goingToProperty = exitCallToken["properties"].Where(p => p["name"].ToString() == "from").FirstOrDefault();
                            string callChainForThisSegment = "(Generated From Call Graph:Unknown)";
                            if (goingToProperty != null)
                            {
                                callChainForThisSegment = String.Format("(Generated From Call Graph:{0})", goingToProperty["value"].ToString());
                            }
                            if (exitCallForThisExit.IsAsync == false)
                            {
                                exitCallForThisExit.CallChain = String.Format("{0}->[{1}]:[{3} ms]-><{2}>", callChainForThisSegment, exitCallForThisExit.ExitType, exitCallForThisExit.ToEntityName, exitCallForThisExit.Duration);
                            }
                            else
                            {
                                exitCallForThisExit.CallChain = String.Format("{0}->[{1}]:[{3} ms async]-><{2}>", callChainForThisSegment, exitCallForThisExit.ExitType, exitCallForThisExit.ToEntityName, exitCallForThisExit.Duration);
                            }
                        }

                        // Finally, here we should have an exit from the segment data
                        string callChain = exitCallForThisExit.CallChain;
                        if (adjustCallDurationInCallChain == true)
                        {
                            // Call duration in the exit that has more then one call (typically database connection acquisition
                            // would have this call chain
                            // (ECommerce-Services)->[WEB_SERVICE]->(Inventory-Services)->[JDBC]:[20 ms]-><INVENTORY-MySQL DB-DB-5.7.13-0ubuntu0.16.04.2>
                            // Here we replace this                                               ^^, which is a sum of all the calls in the call graph
                            // with the value from the exit in the call graph

                            Regex regexDuration = new Regex(@"(.*\[)(\d*)( ms.*\].*)", RegexOptions.IgnoreCase);
                            callChain = regexDuration.Replace(callChain,
                                m => String.Format(
                                    "{0}{1}{2}",
                                    m.Groups[1].Value,
                                    exitCallToken["timeTakenInMillis"],
                                    m.Groups[3].Value));
                        }

                        // Prepare the rendered value
                        if (exitCallForThisExit.HasErrors == false)
                        {
                            if (exitCallForThisExit.ToSegmentID != 0)
                            {
                                exitCallsReferenceList.Add(String.Format("{0}->/{1}/ {2}", callChain, exitCallForThisExit.ToSegmentID, exitCallForThisExit.Detail));
                            }
                            else
                            {
                                exitCallsReferenceList.Add(String.Format("{0} {1}", callChain, exitCallForThisExit.Detail));
                            }
                        }
                        else
                        {
                            exitCallsReferenceList.Add(String.Format("{0} {1} Error {2}", callChain, exitCallForThisExit.Detail, exitCallForThisExit.ErrorDetail));
                            methodCallLine.HasErrors = true;
                        }
                    }

                    // Finally, render the value out of all the exits in here
                    if (methodCallLine.NumExits == 1 && exitCallsReferenceList.Count > 0)
                    {
                        methodCallLine.ExitCalls = exitCallsReferenceList[0];
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder(32 * methodCallLine.NumExits);
                        foreach (string exitCallsReference in exitCallsReferenceList)
                        {
                            sb.AppendFormat("{0};\n", exitCallsReference);
                        }
                        sb.Remove(sb.Length - 1, 1);
                        methodCallLine.ExitCalls = sb.ToString();
                    }
                }

                #endregion

                // Add to total list
                methodCallLinesList.Add(methodCallLine);

                // Move to next sibling
                if (methodCallLineJSON.Next != null)
                {
                    JToken methodCallLineNextJSON = methodCallLineJSON.Next;
                    stackOfMethodCallLineJSONs.Push(methodCallLineNextJSON);
                    stackOfParentMethodCallLines.Push(methodCallLine.Parent);
                }

                // Move to next child, if exists, will take precedence of the sibling
                if (methodCallLine.NumChildren > 0)
                {
                    JToken methodCallLineNextJSON = methodCallLineJSON["children"];
                    if (methodCallLineNextJSON.HasValues == true)
                    {
                        methodCallLineNextJSON = methodCallLineNextJSON[0];
                        stackOfMethodCallLineJSONs.Push(methodCallLineNextJSON);
                        stackOfParentMethodCallLines.Push(methodCallLine);
                    }
                }
            }

            return methodCallLinesList;
        }

        private string getFrameworkFromClassOrFunctionName(string classOrFunctionName, Dictionary<string, List<MethodCallLineClassTypeMapping>> methodCallLineClassToFrameworkTypeMappingDictionary)
        {
            string frameworkName = String.Empty;

            if (classOrFunctionName.Length > 0)
            {
                // Find mapping
                string keyLetterOfMappingList = classOrFunctionName.Substring(0, 1).ToLower();
                if (methodCallLineClassToFrameworkTypeMappingDictionary.ContainsKey(keyLetterOfMappingList) == true)
                {
                    List<MethodCallLineClassTypeMapping> methodCallLineClassToFrameworkTypeMappingList = methodCallLineClassToFrameworkTypeMappingDictionary[keyLetterOfMappingList];
                    foreach (MethodCallLineClassTypeMapping mapping in methodCallLineClassToFrameworkTypeMappingList)
                    {
                        if (classOrFunctionName.StartsWith(mapping.ClassPrefix, StringComparison.InvariantCulture) == true)
                        {
                            frameworkName = String.Format("{0} ({1})", mapping.ClassPrefix, mapping.FrameworkType);
                            break;
                        }
                    }
                }

                // If we haven't found framework, get it out of the full class name
                // Grab first two elements instead of the entire class name
                // AutoFac.Control.Execution -> Autofac.Funky
                // com.tms.whatever -> com.tms
                // com.matrixone.jdl.rmi.bosMQLCommandImpl > com.matrixone
                // org.bread.with.butter -> org.bread
                if (frameworkName.Length == 0)
                {
                    string[] classNameTokens = classOrFunctionName.Split('.');
                    if (classNameTokens.Length == 0 || classNameTokens.Length == 1)
                    {
                        // No periods
                        frameworkName = classOrFunctionName;
                    }
                    else if (classNameTokens.Length >= 2)
                    {
                        frameworkName = String.Format("{0}.{1}", classNameTokens[0], classNameTokens[1]);
                    }
                }
            }

            return frameworkName;
        }

        internal void addFoldedStacks(Dictionary<string, FoldedStackLine> foldedStackLinesContainer, List<FoldedStackLine> foldedStackLinesToAdd)
        {
            if (foldedStackLinesContainer != null && foldedStackLinesToAdd != null)
            {
                foreach (FoldedStackLine foldedCallStack in foldedStackLinesToAdd)
                {
                    if (foldedStackLinesContainer.ContainsKey(foldedCallStack.FoldedStack) == true)
                    {
                        foldedStackLinesContainer[foldedCallStack.FoldedStack].AddFoldedStackLine(foldedCallStack);
                    }
                    else
                    {
                        FoldedStackLine foldedStackLineClone = foldedCallStack.Clone();
                        foldedStackLinesContainer.Add(foldedCallStack.FoldedStack, foldedStackLineClone);
                    }
                }
            }
        }
    }
}
