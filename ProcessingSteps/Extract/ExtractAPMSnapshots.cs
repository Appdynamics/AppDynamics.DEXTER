using AppDynamics.Dexter.DataObjects;
using AppDynamics.Dexter.Extensions;
using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class ExtractAPMSnapshots : JobStepReportBase
    {
        private const int SNAPSHOTS_EXTRACT_NUMBER_OF_SNAPSHOTS_TO_PROCESS_PER_THREAD = 128;
        private const int SNAPSHOTS_EXTRACT_NUMBER_OF_THREADS_ONPREM = 8;
        private const int SNAPSHOTS_EXTRACT_NUMBER_OF_THREADS_SAAS = 12;
        private const int SNAPSHOTS_QUERY_PAGE_SIZE = 600;

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

                    try
                    {
                        this.DisplayJobTargetStartingStatus(jobConfiguration, jobTarget, i + 1);

                        // Set up controller access
                        using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                        {
                            controllerApi.PrivateApiLogin();

                            #region Get list of Snapshots in time ranges

                            loggerConsole.Info("Extract List of Snapshots ({0} time ranges)", jobConfiguration.Input.HourlyTimeRanges.Count);

                            // Get list of snapshots in each time range
                            int totalSnapshotsFound = 0;
                            foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                            {
                                logger.Info("Extract List of Snapshots from {0:o} to {1:o}", jobTimeRange.From, jobTimeRange.To);
                                loggerConsole.Info("Extract List of Snapshots from {0:G} to {1:G}", jobTimeRange.From.ToLocalTime(), jobTimeRange.To.ToLocalTime());

                                string snapshotsDataFilePath = FilePathMap.SnapshotsDataFilePath(jobTarget, jobTimeRange);

                                int differenceInMinutes = (int)(jobTimeRange.To - jobTimeRange.From).TotalMinutes;

                                if (File.Exists(snapshotsDataFilePath) == false)
                                {
                                    JArray snapshotsArray = new JArray();

                                    // Extract snapshot list
                                    long serverCursorId = 0;
                                    string serverCursorIdType = String.Empty;
                                    Hashtable requestIDs = new Hashtable(10000);

                                    do
                                    {
                                        string snapshotsJSON = String.Empty;
                                        if (serverCursorId == 0)
                                        {

                                            // Extract first page of snapshots
                                            snapshotsJSON = controllerApi.GetListOfSnapshotsFirstPage(jobTarget.ApplicationID, jobTimeRange.From, jobTimeRange.To, differenceInMinutes, SNAPSHOTS_QUERY_PAGE_SIZE);
                                        }
                                        else
                                        {
                                            // If there are more snapshots on the server, the server cursor would be non-0 
                                            switch (serverCursorIdType)
                                            {
                                                case "scrollId":
                                                    // Sometimes - >4.3.3? the value of scroll is in scrollId, not rsdScrollId
                                                    // "serverCursor" : {
                                                    //    "scrollId" : 1509543646696
                                                    //  }
                                                    snapshotsJSON = controllerApi.GetListOfSnapshotsNextPage_Type_scrollId(jobTarget.ApplicationID, jobTimeRange.From, jobTimeRange.To, differenceInMinutes, SNAPSHOTS_QUERY_PAGE_SIZE, serverCursorId);

                                                    break;

                                                case "rsdScrollId":
                                                    // "serverCursor" : {
                                                    //    "rsdScrollId" : 1509543646696
                                                    //  }
                                                    snapshotsJSON = controllerApi.GetListOfSnapshotsNextPage_Type_rsdScrollId(jobTarget.ApplicationID, jobTimeRange.From, jobTimeRange.To, differenceInMinutes, SNAPSHOTS_QUERY_PAGE_SIZE, serverCursorId);

                                                    break;

                                                case "fetchMoreDataHandle":
                                                    // Seen this on 4.2.3.0 Controller. Maybe that's how it used to be?
                                                    // "fetchMoreDataHandle":1509626881987
                                                    // Can't seem to make it load more than 600 items
                                                    snapshotsJSON = controllerApi.GetListOfSnapshotsNextPage_Type_handle(jobTarget.ApplicationID, jobTimeRange.From, jobTimeRange.To, differenceInMinutes, SNAPSHOTS_QUERY_PAGE_SIZE, serverCursorId);

                                                    break;

                                                default:
                                                    logger.Warn("Unknown type of serverCursorIdType={0}, not going to retrieve any snapshots", serverCursorIdType);

                                                    break;
                                            }
                                        }

                                        // Assume we have no more pages
                                        serverCursorId = 0;

                                        // Process retrieved snapshots and check if we actually have more pages
                                        if (snapshotsJSON != String.Empty)
                                        {
                                            Console.Write(".");

                                            // Load snapshots into array
                                            JObject snapshotscontainerObject = JObject.Parse(snapshotsJSON);
                                            JArray retrievedSnapshotsArray = (JArray)snapshotscontainerObject["requestSegmentDataListItems"];
                                            foreach (JObject snapshotObject in retrievedSnapshotsArray)
                                            {
                                                // Filter out duplicates
                                                if (requestIDs.ContainsKey(snapshotObject["requestGUID"].ToString()) == true)
                                                {
                                                    logger.Warn("Snapshot {0} is a duplicate, skipping", snapshotObject["requestGUID"]);
                                                    continue;
                                                }

                                                snapshotsArray.Add(snapshotObject);
                                                requestIDs.Add(snapshotObject["requestGUID"].ToString(), true);
                                            }

                                            // Check whether we have more snapshots and if yes, get continuation type and cursor ID
                                            JToken fetchMoreDataHandleToken = snapshotscontainerObject["fetchMoreDataHandle"];
                                            JToken serverCursorToken = snapshotscontainerObject["serverCursor"];
                                            if (serverCursorToken != null)
                                            {
                                                JToken scrollIdToken = serverCursorToken["scrollId"];
                                                JToken rsdScrollIdToken = serverCursorToken["rsdScrollId"];

                                                if (scrollIdToken != null)
                                                {
                                                    serverCursorIdType = "scrollId";
                                                    // Parse the cursor ID 
                                                    if (Int64.TryParse(scrollIdToken.ToString(), out serverCursorId) == false)
                                                    {
                                                        // Nope, not going to go forward
                                                        serverCursorId = 0;
                                                    }
                                                }
                                                else if (rsdScrollIdToken != null)
                                                {
                                                    serverCursorIdType = "rsdScrollId";
                                                    // Parse the cursor ID 
                                                    if (Int64.TryParse(rsdScrollIdToken.ToString(), out serverCursorId) == false)
                                                    {
                                                        // Nope, not going to go forward
                                                        serverCursorId = 0;
                                                    }
                                                }
                                            }
                                            else if (fetchMoreDataHandleToken != null)
                                            {
                                                serverCursorIdType = "fetchMoreDataHandle";
                                                // Parse the cursor ID 
                                                if (Int64.TryParse(fetchMoreDataHandleToken.ToString(), out serverCursorId) == false)
                                                {
                                                    // Nope, not going to go forward
                                                    serverCursorId = 0;
                                                }
                                            }
                                            else
                                            {
                                                logger.Warn("Snapshot list retrieval call unexpectedly did not have any evidence of continuation CursorId");
                                            }

                                            logger.Info("Retrieved snapshots from Controller {0}, Application {1}, From {2:o}, To {3:o}', number of snapshots {4}, continuation type {5}, continuation CursorId {6}", jobTarget.Controller, jobTarget.Application, jobTimeRange.From, jobTimeRange.To, retrievedSnapshotsArray.Count, serverCursorIdType, serverCursorId);

                                            // Move to next loop
                                            Console.Write("+{0}", snapshotsArray.Count);
                                        }
                                    }
                                    while (serverCursorId > 0);

                                    Console.WriteLine();

                                    FileIOHelper.WriteJArrayToFile(snapshotsArray, snapshotsDataFilePath);

                                    totalSnapshotsFound = totalSnapshotsFound + snapshotsArray.Count;

                                    logger.Info("{0} snapshots from {1:o} to {2:o}", snapshotsArray.Count, jobTimeRange.From, jobTimeRange.To);
                                    loggerConsole.Info("{0} snapshots from {1:G} to {2:G}", snapshotsArray.Count, jobTimeRange.From.ToLocalTime(), jobTimeRange.To.ToLocalTime());
                                }
                            }

                            logger.Info("{0} snapshots in all time ranges", totalSnapshotsFound);
                            loggerConsole.Info("{0} snapshots in all time ranges", totalSnapshotsFound);

                            #endregion
                        }

                        #region Get individual Snapshots

                        // Extract individual snapshots
                        loggerConsole.Info("Extract Individual Snapshots");

                        List<AppDRESTTier> tiersList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTTier>(FilePathMap.APMTiersDataFilePath(jobTarget));
                        List<AppDRESTBusinessTransaction> businessTransactionsList = FileIOHelper.LoadListOfObjectsFromFile<AppDRESTBusinessTransaction>(FilePathMap.APMBusinessTransactionsDataFilePath(jobTarget));

                        // Identify Node.JS tiers that will extact call graph using a different call
                        List<AppDRESTTier> tiersNodeJSList = null;
                        if (tiersList != null)
                        {
                            tiersNodeJSList = tiersList.Where(t => t.agentType == "NODEJS_APP_AGENT").ToList();
                        }

                        // Process each hour at a time
                        foreach (JobTimeRange jobTimeRange in jobConfiguration.Input.HourlyTimeRanges)
                        {
                            string snapshotsDataFilePath = FilePathMap.SnapshotsDataFilePath(jobTarget, jobTimeRange);

                            JArray snapshotsInHourArray = FileIOHelper.LoadJArrayFromFile(snapshotsDataFilePath);
                            if (snapshotsInHourArray != null && snapshotsInHourArray.Count > 0)
                            {
                                #region Filter Snapshots

                                logger.Info("Filter Snapshots {0:o} to {1:o} ({2} snapshots)", jobTimeRange.From, jobTimeRange.To, snapshotsInHourArray.Count);
                                loggerConsole.Info("Filter Snapshots {0:G} to {1:G} ({2} snapshots)", jobTimeRange.From.ToLocalTime(), jobTimeRange.To.ToLocalTime(), snapshotsInHourArray.Count);

                                // Filter the list of snapshots based on SnapshotSelectionCriteria
                                List<JToken> listOfSnapshotsInHourFiltered = new List<JToken>(snapshotsInHourArray.Count);
                                foreach (JToken snapshotToken in snapshotsInHourArray)
                                {
                                    logger.Trace("Considering filtering snapshot requestGUID={0}, firstInChain={1}, userExperience={2}, fullCallgraph={3}, delayedCallGraph={4}, applicationComponentName={5}, businessTransactionName={6}",
                                        snapshotToken["requestGUID"],
                                        snapshotToken["firstInChain"],
                                        snapshotToken["userExperience"],
                                        snapshotToken["fullCallgraph"],
                                        snapshotToken["delayedCallGraph"],
                                        snapshotToken["applicationComponentName"],
                                        snapshotToken["businessTransactionName"]);

                                    // Only grab first in chain snapshots
                                    if ((bool)snapshotToken["firstInChain"] == false) continue;

                                    // Filter user experience
                                    switch (snapshotToken["userExperience"].ToString())
                                    {
                                        case "NORMAL":
                                            if (jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Normal != true) continue;
                                            break;
                                        case "SLOW":
                                            if (jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Slow != true) continue;
                                            break;
                                        case "VERY_SLOW":
                                            if (jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.VerySlow != true) continue;
                                            break;
                                        case "STALL":
                                            if (jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Stall != true) continue;
                                            break;
                                        case "ERROR":
                                            if (jobConfiguration.Input.SnapshotSelectionCriteria.UserExperience.Error != true) continue;
                                            break;
                                        default:
                                            // Not sure what kind of beast it is
                                            continue;
                                    }

                                    // Filter call graph
                                    if ((bool)snapshotToken["fullCallgraph"] == true)
                                    {
                                        if (jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.Full != true) continue;
                                    }
                                    else if ((bool)snapshotToken["delayedCallGraph"] == true)
                                    {
                                        if (jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.Partial != true) continue;
                                    }
                                    else
                                    {
                                        if (jobConfiguration.Input.SnapshotSelectionCriteria.SnapshotType.None != true) continue;
                                    }

                                    // Filter Tier type
                                    if (jobConfiguration.Input.SnapshotSelectionCriteria.TierType.All != true)
                                    {
                                        if (tiersList != null)
                                        {
                                            AppDRESTTier tier = tiersList.Where(t => t.id == (long)snapshotToken["applicationComponentId"]).FirstOrDefault();
                                            if (tier != null)
                                            {
                                                PropertyInfo pi = jobConfiguration.Input.SnapshotSelectionCriteria.TierType.GetType().GetProperty(tier.agentType);
                                                if (pi != null)
                                                {
                                                    if ((bool)pi.GetValue(jobConfiguration.Input.SnapshotSelectionCriteria.TierType) == false) continue;
                                                }
                                            }
                                        }
                                    }

                                    // Filter BT type
                                    if (jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType.All != true)
                                    {
                                        if (businessTransactionsList != null)
                                        {
                                            AppDRESTBusinessTransaction businessTransaction = businessTransactionsList.Where(b => b.id == (long)snapshotToken["businessTransactionId"] && b.tierId == (long)snapshotToken["applicationComponentId"]).FirstOrDefault();
                                            if (businessTransaction != null)
                                            {
                                                PropertyInfo pi = jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType.GetType().GetProperty(businessTransaction.entryPointType);
                                                if (pi != null)
                                                {
                                                    if ((bool)pi.GetValue(jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactionType) == false) continue;
                                                }
                                            }
                                        }
                                    }

                                    // Filter Tier name
                                    bool tierNameMatch = false;
                                    if (jobConfiguration.Input.SnapshotSelectionCriteria.Tiers.Length == 0) tierNameMatch = true;
                                    foreach (string matchCriteria in jobConfiguration.Input.SnapshotSelectionCriteria.Tiers)
                                    {
                                        if (matchCriteria.Length > 0)
                                        {
                                            // Try straight up string compare first
                                            if (String.Compare(snapshotToken["applicationComponentName"].ToString(), matchCriteria, true) == 0)
                                            {
                                                tierNameMatch = true;
                                                break;
                                            }

                                            // Try regex compare second
                                            Regex regexQuery = new Regex(matchCriteria, RegexOptions.IgnoreCase);
                                            Match regexMatch = regexQuery.Match(snapshotToken["applicationComponentName"].ToString());
                                            if (regexMatch.Success == true && regexMatch.Index == 0)
                                            {
                                                tierNameMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (tierNameMatch == false) continue;

                                    // Filter BT name
                                    bool businessTransactionNameMatch = false;
                                    if (jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactions.Length == 0) businessTransactionNameMatch = true;
                                    foreach (string matchCriteria in jobConfiguration.Input.SnapshotSelectionCriteria.BusinessTransactions)
                                    {
                                        if (matchCriteria.Length > 0)
                                        {
                                            // Try straight up string compare first
                                            if (String.Compare(snapshotToken["businessTransactionName"].ToString(), matchCriteria, true) == 0)
                                            {
                                                businessTransactionNameMatch = true;
                                                break;
                                            }

                                            // Try regex compare second
                                            Regex regexQuery = new Regex(matchCriteria, RegexOptions.IgnoreCase);
                                            Match regexMatch = regexQuery.Match(snapshotToken["businessTransactionName"].ToString());
                                            if (regexMatch.Success == true && regexMatch.Index == 0)
                                            {
                                                businessTransactionNameMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (businessTransactionNameMatch == false) continue;

                                    // If we got here, then the snapshot passed the filter
                                    logger.Trace("Keeping snapshot requestGUID={0}, firstInChain={1}, userExperience={2}, fullCallgraph={3}, delayedCallGraph={4}, applicationComponentName={5}, businessTransactionName={6}",
                                        snapshotToken["requestGUID"],
                                        snapshotToken["firstInChain"],
                                        snapshotToken["userExperience"],
                                        snapshotToken["fullCallgraph"],
                                        snapshotToken["delayedCallGraph"],
                                        snapshotToken["applicationComponentName"],
                                        snapshotToken["businessTransactionName"]);

                                    listOfSnapshotsInHourFiltered.Add(snapshotToken);
                                }

                                logger.Info("Total Snapshots {0:o} to {1:o} is {2}, after filtered {3}", jobTimeRange.From, jobTimeRange.To, snapshotsInHourArray.Count, listOfSnapshotsInHourFiltered.Count);

                                #endregion

                                #region Extract Snapshots

                                // Now extract things
                                logger.Info("Extract Snapshots {0:o} to {1:o} ({2} snapshots)", jobTimeRange.From, jobTimeRange.To, listOfSnapshotsInHourFiltered.Count);
                                loggerConsole.Info("Extract Snapshots {0:G} to {1:G} ({2} snapshots)", jobTimeRange.From.ToLocalTime(), jobTimeRange.To.ToLocalTime(), listOfSnapshotsInHourFiltered.Count);

                                stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + listOfSnapshotsInHourFiltered.Count;

                                int numSnapshots = 0;

                                if (programOptions.ProcessSequentially == false)
                                {
                                    var listOfSnapshotsInHourChunks = listOfSnapshotsInHourFiltered.BreakListIntoChunks(SNAPSHOTS_EXTRACT_NUMBER_OF_SNAPSHOTS_TO_PROCESS_PER_THREAD);

                                    int numThreadsToUseForExtraction = SNAPSHOTS_EXTRACT_NUMBER_OF_THREADS_ONPREM;
                                    if (jobTarget.Controller.ToLower().Contains("saas.appdynamics.com") == true)
                                    {
                                        numThreadsToUseForExtraction = SNAPSHOTS_EXTRACT_NUMBER_OF_THREADS_SAAS;
                                    }

                                    Parallel.ForEach<List<JToken>, int>(
                                        listOfSnapshotsInHourChunks,
                                        new ParallelOptions { MaxDegreeOfParallelism = numThreadsToUseForExtraction },
                                        () => 0,
                                        (listOfSnapshotsInHourChunk, loop, subtotal) =>
                                        {
                                            // Set up controller access
                                            using (ControllerApi controllerApiParallel = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                            {
                                                // Login into private API
                                                controllerApiParallel.PrivateApiLogin();

                                                subtotal += extractSnapshots(jobConfiguration, jobTarget, controllerApiParallel, listOfSnapshotsInHourChunk, tiersNodeJSList, false);
                                                return subtotal;
                                            }
                                        },
                                        (finalResult) =>
                                        {
                                            Interlocked.Add(ref numSnapshots, finalResult);
                                            Console.Write("[{0}].", numSnapshots);
                                        }
                                    );
                                }
                                else
                                {
                                    using (ControllerApi controllerApi = new ControllerApi(jobTarget.Controller, jobTarget.UserName, AESEncryptionHelper.Decrypt(jobTarget.UserPassword)))
                                    {
                                        controllerApi.PrivateApiLogin();

                                        numSnapshots = extractSnapshots(jobConfiguration, jobTarget, controllerApi, listOfSnapshotsInHourFiltered, tiersNodeJSList, true);
                                    }
                                }

                                loggerConsole.Info("{0} snapshots extracted", numSnapshots);

                                #endregion
                            }
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
                loggerConsole.Trace("Skipping export of snapshots");
            }
            return (jobConfiguration.Input.Snapshots == true);
        }

        private int extractSnapshots(
            JobConfiguration jobConfiguration,
            JobTarget jobTarget,
            ControllerApi controllerApi,
            List<JToken> snapshotTokenList,
            List<AppDRESTTier> tiersNodeJSList,
            bool progressToConsole)
        {
            int j = 0;

            foreach (JToken snapshotToken in snapshotTokenList)
            {
                // Only do first in chain
                if ((bool)snapshotToken["firstInChain"] == true)
                {
                    #region Target step variables

                    DateTime snapshotTime = UnixTimeHelper.ConvertFromUnixTimestamp((long)snapshotToken["serverStartTime"]);

                    string snapshotDataFilePath = FilePathMap.SnapshotDataFilePath(
                        jobTarget,
                        snapshotToken["applicationComponentName"].ToString(), (long)snapshotToken["applicationComponentId"],
                        snapshotToken["businessTransactionName"].ToString(), (long)snapshotToken["businessTransactionId"],
                        snapshotTime,
                        snapshotToken["userExperience"].ToString(),
                        snapshotToken["requestGUID"].ToString());

                    // Must strip out the milliseconds, because the segment list retrieval doesn't seem to like them in the datetimes
                    DateTime snapshotTimeFrom = snapshotTime.AddMinutes(-30).AddMilliseconds(snapshotTime.Millisecond * -1);
                    DateTime snapshotTimeTo = snapshotTime.AddMinutes(30).AddMilliseconds(snapshotTime.Millisecond * -1);

                    long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(snapshotTimeFrom);
                    long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(snapshotTimeTo);
                    int differenceInMinutes = (int)(snapshotTimeTo - snapshotTimeFrom).TotalMinutes;

                    #endregion

                    if (File.Exists(snapshotDataFilePath) == false)
                    {
                        logger.Info("Retrieving snapshot for Application {0}, Tier {1}, Business Transaction {2}, RequestGUID {3}", jobTarget.Application, snapshotToken["applicationComponentName"], snapshotToken["businessTransactionName"], snapshotToken["requestGUID"]);

                        #region Get Snapshot Flowmap

                        // Get snapshot flow map
                        // Commenting this out until the time I decide to build visual representation of it, until then it is not needed
                        //string snapshotFlowmapDataFilePath = Path.Combine(snapshotFolderPath, EXTRACT_SNAPSHOT_FLOWMAP_FILE_NAME);

                        //if (File.Exists(snapshotFlowmapDataFilePath) == false)
                        //{
                        //    string snapshotFlowmapJson = controllerApi.GetFlowmapSnapshot(jobTarget.ApplicationID, (int)snapshot["businessTransactionId"], snapshot["requestGUID"].ToString(), fromTimeUnix, toTimeUnix, differenceInMinutes);
                        //    if (snapshotFlowmapJson != String.Empty) FileIOHelper.SaveFileToPath(snapshotFlowmapJson, snapshotFlowmapDataFilePath);
                        //}

                        #endregion

                        Dictionary<long, string> snapshotSegmentDetailsJsonList = new Dictionary<long, string>(5);

                        try
                        {
                            using (TextWriter tw = FileIOHelper.SaveFileToPathWithWriter(snapshotDataFilePath))
                            {
                                // Start root object
                                tw.WriteLine("{");

                                // Snapshot object
                                tw.Write("\"snapshot\" : {0},", snapshotToken.ToString(Newtonsoft.Json.Formatting.Indented));
                                tw.WriteLine();

                                #region Get List of Segments

                                // Get list of segments
                                string snapshotSegmentsJson = controllerApi.GetSnapshotSegments(snapshotToken["requestGUID"].ToString(), snapshotTimeFrom, snapshotTimeTo, differenceInMinutes);

                                // Segments object
                                if (snapshotSegmentsJson.Length > 0)
                                {
                                    tw.WriteLine("\"segments\" :");
                                    tw.Write(snapshotSegmentsJson);
                                    tw.WriteLine(",");
                                }
                                else
                                {
                                    tw.WriteLine("\"segments\" : [],");
                                }

                                #endregion

                                #region Get Details for Each Segment                        

                                if (snapshotSegmentsJson != String.Empty)
                                {
                                    JArray snapshotSegmentsList = JArray.Parse(snapshotSegmentsJson);
                                    if (snapshotSegmentsList != null)
                                    {
                                        int numSegments = snapshotSegmentsList.Count;

                                        // Get details for segment
                                        tw.WriteLine("\"segmentDetails\" :");
                                        tw.WriteLine("{");
                                        bool someObjectWrittenAlready = false;
                                        foreach (JToken snapshotSegment in snapshotSegmentsList)
                                        {
                                            string snapshotSegmentJson = controllerApi.GetSnapshotSegmentDetails((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                            if (snapshotSegmentJson != String.Empty)
                                            {
                                                snapshotSegmentDetailsJsonList.Add((long)snapshotSegment["id"], snapshotSegmentJson);

                                                if (someObjectWrittenAlready == true)
                                                {
                                                    tw.WriteLine(",");
                                                }
                                                tw.Write("\"{0}\" : {1}", (long)snapshotSegment["id"], snapshotSegmentJson);
                                                someObjectWrittenAlready = true;
                                            }
                                        }
                                        tw.WriteLine("},");

                                        // Get errors for segment
                                        tw.WriteLine("\"errors\" :");
                                        tw.WriteLine("{");
                                        someObjectWrittenAlready = false;
                                        foreach (JToken snapshotSegment in snapshotSegmentsList)
                                        {
                                            if ((bool)snapshotSegment["errorOccurred"] == true)
                                            {
                                                string snapshotSegmentErrorJson = controllerApi.GetSnapshotSegmentErrors((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                if (snapshotSegmentErrorJson != String.Empty)
                                                {
                                                    // "[ ]" == empty data. Don't create the file
                                                    if (snapshotSegmentErrorJson.Length > 3)
                                                    {
                                                        if (someObjectWrittenAlready == true)
                                                        {
                                                            tw.WriteLine(",");
                                                        }
                                                        tw.Write("\"{0}\" : {1}", (long)snapshotSegment["id"], snapshotSegmentErrorJson);
                                                        someObjectWrittenAlready = true;
                                                    }
                                                }
                                            }
                                        }
                                        tw.WriteLine("},");

                                        // Get call graphs for segment
                                        tw.WriteLine("\"callgraphs\" :");
                                        tw.WriteLine("{");
                                        someObjectWrittenAlready = false;
                                        foreach (JToken snapshotSegment in snapshotSegmentsList)
                                        {
                                            if (((bool)snapshotSegment["fullCallgraph"] == true || (bool)snapshotSegment["delayedCallGraph"] == true))
                                            {
                                                // If the tier is Node.JS, the call graphs come from Process Snapshot
                                                bool getProcessCallGraph = false;
                                                string processRequestGUID = String.Empty;
                                                if (tiersNodeJSList != null && tiersNodeJSList.Count > 0)
                                                {
                                                    // Is this a Node.JS tier?
                                                    if (tiersNodeJSList.Count(t => t.id == (long)snapshotSegment["applicationComponentId"]) > 0)
                                                    {
                                                        // Yes, it is

                                                        // Is there a process snapshot? Check Transaction Properties for its value
                                                        string snapshotSegmentDetailJson = snapshotSegmentDetailsJsonList[(long)snapshotSegment["id"]];
                                                        JObject snapshotSegmentDetail = JObject.Parse(snapshotSegmentDetailJson);
                                                        if (snapshotSegmentDetail != null)
                                                        {
                                                            if (snapshotSegmentDetail["transactionProperties"].HasValues == true)
                                                            {
                                                                foreach (JToken transactionPropertyToken in snapshotSegmentDetail["transactionProperties"])
                                                                {
                                                                    if (transactionPropertyToken["name"].ToString() == "Process Snapshot GUIDs")
                                                                    {
                                                                        getProcessCallGraph = true;
                                                                        processRequestGUID = transactionPropertyToken["value"].ToString();
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                // Ok, now either get call graph the usual way or process snapshot call graph
                                                string snapshotSegmentJson = String.Empty;
                                                if (getProcessCallGraph == true && processRequestGUID.Length > 0)
                                                {
                                                    snapshotSegmentJson = controllerApi.GetProcessSnapshotCallGraph(processRequestGUID, fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                }
                                                else
                                                {
                                                    snapshotSegmentJson = controllerApi.GetSnapshotSegmentCallGraph((long)snapshotSegment["id"], fromTimeUnix, toTimeUnix, differenceInMinutes);
                                                }
                                                if (snapshotSegmentJson != String.Empty)
                                                {
                                                    if (someObjectWrittenAlready == true)
                                                    {
                                                        tw.WriteLine(",");
                                                    }
                                                    tw.Write("\"{0}\" : {1}", (long)snapshotSegment["id"], snapshotSegmentJson);
                                                    someObjectWrittenAlready = true;
                                                }
                                            }
                                        }
                                        tw.WriteLine("}");
                                    }
                                }

                                #endregion

                                // End root object
                                tw.WriteLine("}");

                                logger.Trace("Wrote string length {0} to file {1}", ((System.IO.FileStream)((System.IO.StreamWriter)tw).BaseStream).Position, snapshotDataFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Warn("Unable to write to {0}", snapshotDataFilePath);
                            logger.Warn(ex);
                            loggerConsole.Warn(ex);
                        }

                    }
                }

                if (progressToConsole == true)
                {
                    j++;
                    if (j % 10 == 0)
                    {
                        Console.Write("[{0}].", j);
                    }
                }

            }

            return snapshotTokenList.Count;
        }

        private void addListOfObjectsAsIndividualObjectsToJSONString(StringBuilder sb, Dictionary<long, string> keyValuePairs)
        {
            int currentIndex = 0;
            int lastIndex = keyValuePairs.Count - 1;
            foreach (KeyValuePair<long, string> kvp in keyValuePairs)
            {
                sb.AppendFormat("\"{0}\" : {1}", kvp.Key, kvp.Value);
                if (currentIndex != lastIndex)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
                currentIndex++;
            }
        }

        private void addListOfObjectsAsArrayToJSONString(StringBuilder sb, Dictionary<long, string> keyValuePairs)
        {
            int currentIndex = 0;
            int lastIndex = keyValuePairs.Count - 1;
            foreach (KeyValuePair<long, string> kvp in keyValuePairs)
            {
                sb.Append(kvp.Value);
                if (currentIndex != lastIndex)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
                currentIndex++;
            }
        }
    }
}
