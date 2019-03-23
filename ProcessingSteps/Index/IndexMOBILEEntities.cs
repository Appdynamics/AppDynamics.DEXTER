using AppDynamics.Dexter.ReportObjectMaps;
using AppDynamics.Dexter.ReportObjects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AppDynamics.Dexter.ProcessingSteps
{
    public class IndexMOBILEEntities : JobStepIndexBase
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
                if (this.ShouldExecute(jobConfiguration) == false)
                {
                    return true;
                }

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_MOBILE) == 0)
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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_MOBILE) continue;

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

                        #region Prepare time range

                        long fromTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.From);
                        long toTimeUnix = UnixTimeHelper.ConvertToUnixTimestamp(jobConfiguration.Input.TimeRange.To);
                        int differenceInMinutes = (int)(jobConfiguration.Input.TimeRange.To - jobConfiguration.Input.TimeRange.From).Duration().TotalMinutes;
                        string DEEPLINK_THIS_TIMERANGE = String.Format(DEEPLINK_TIMERANGE_BETWEEN_TIMES, toTimeUnix, fromTimeUnix, differenceInMinutes);

                        #endregion

                        #region Network Requests

                        List<MOBILENetworkRequest> networkRequestsList = null;
                        List<MOBILENetworkRequestToBusinessTransaction> networkRequestToBTsList = null;

                        JObject networkRequestsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.MOBILENetworkRequestsDataFilePath(jobTarget));
                        if (isTokenPropertyNull(networkRequestsContainerObject, "data") == false)
                        {
                            JArray networkRequestsArray = (JArray)networkRequestsContainerObject["data"];

                            loggerConsole.Info("Index List of Network Requests ({0} entities)", networkRequestsArray.Count);

                            networkRequestsList = new List<MOBILENetworkRequest>(networkRequestsArray.Count);
                            networkRequestToBTsList = new List<MOBILENetworkRequestToBusinessTransaction>(networkRequestsArray.Count * 4);

                            foreach (JObject networkRequestObject in networkRequestsArray)
                            {
                                MOBILENetworkRequest networkRequest = new MOBILENetworkRequest();
                                networkRequest.Controller = jobTarget.Controller;
                                networkRequest.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_THIS_TIMERANGE);
                                networkRequest.ApplicationName = jobTarget.Application;
                                networkRequest.ApplicationID = jobTarget.ApplicationID;
                                networkRequest.ApplicationLink = String.Format(DEEPLINK_MOBILE_APPLICATION, networkRequest.Controller, jobTarget.ParentApplicationID, networkRequest.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                networkRequest.RequestName = getStringValueFromJToken(networkRequestObject, "name");
                                networkRequest.RequestNameInternal = getStringValueFromJToken(networkRequestObject, "internalName");
                                networkRequest.RequestID = getLongValueFromJToken(networkRequestObject, "addId");
                                networkRequest.RequestLink = String.Format(DEEPLINK_NETWORK_REQUEST, networkRequest.Controller, jobTarget.ParentApplicationID, networkRequest.ApplicationID, networkRequest.RequestID, DEEPLINK_THIS_TIMERANGE);

                                networkRequest.Platform = getStringValueFromJToken(networkRequestObject, "mobilePlatform");

                                networkRequest.IsCorrelated = getBoolValueFromJToken(networkRequestObject, "hasServerAgentCorrelation");

                                networkRequest.Duration = differenceInMinutes;
                                networkRequest.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                networkRequest.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                networkRequest.FromUtc = jobConfiguration.Input.TimeRange.From;
                                networkRequest.ToUtc = jobConfiguration.Input.TimeRange.To;

                                networkRequest.IsExcluded = getBoolValueFromJToken(networkRequestObject, "excluded");

                                // Now to metrics
                                networkRequest.MetricsIDs = new List<long>(16);

                                JObject networkRequestDetailsObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.MOBILENetworkRequestPerformanceDataFilePath(jobTarget, networkRequest.RequestName, networkRequest.RequestID, jobConfiguration.Input.TimeRange));
                                if (networkRequestDetailsObject != null)
                                {
                                    networkRequest.UserExperience = getStringValueFromJToken(networkRequestDetailsObject, "performanceState");

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "networkRequestTime") == false)
                                    {
                                        networkRequest.ART = getLongValueFromJToken(networkRequestDetailsObject["networkRequestTime"], "value");
                                        if (networkRequest.ART < 0)
                                        {
                                            networkRequest.ART = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["networkRequestTime"], "metricId"));
                                        }
                                        networkRequest.ARTRange = getDurationRangeAsString(networkRequest.ART);
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "networkRequestTime") == false &&
                                        isTokenPropertyNull(networkRequestDetailsObject["networkRequestTime"], "graphData") == false &&
                                        isTokenPropertyNull(networkRequestDetailsObject["networkRequestTime"]["graphData"], "data") == false)
                                    {
                                        networkRequest.TimeTotal = sumLongValuesInArray((JArray)networkRequestDetailsObject["networkRequestTime"]["graphData"]["data"]);
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "requestsPerMin") == false)
                                    {
                                        networkRequest.CPM = getLongValueFromJToken(networkRequestDetailsObject["requestsPerMin"], "value");
                                        if (networkRequest.CPM < 0)
                                        {
                                            networkRequest.CPM = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["requestsPerMin"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "totalNumOfRequests") == false)
                                    {
                                        networkRequest.Calls = getLongValueFromJToken(networkRequestDetailsObject["totalNumOfRequests"], "value");
                                        if (networkRequest.Calls < 0)
                                        {
                                            networkRequest.Calls = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["totalNumOfRequests"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "totalServerTime") == false)
                                    {
                                        networkRequest.Server = getLongValueFromJToken(networkRequestDetailsObject["totalServerTime"], "value");
                                        if (networkRequest.Server < 0)
                                        {
                                            networkRequest.Server = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["totalServerTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "totalNumHttpErrors") == false)
                                    {
                                        networkRequest.HttpErrors = getLongValueFromJToken(networkRequestDetailsObject["totalNumHttpErrors"], "value");
                                        if (networkRequest.HttpErrors < 0)
                                        {
                                            networkRequest.HttpErrors = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["totalNumHttpErrors"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "httpErrorsPerMin") == false)
                                    {
                                        networkRequest.HttpEPM = getLongValueFromJToken(networkRequestDetailsObject["httpErrorsPerMin"], "value");
                                        if (networkRequest.HttpEPM < 0)
                                        {
                                            networkRequest.HttpEPM = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["httpErrorsPerMin"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "totalNumNetworkErrors") == false)
                                    {
                                        networkRequest.NetworkErrors = getLongValueFromJToken(networkRequestDetailsObject["totalNumNetworkErrors"], "value");
                                        if (networkRequest.NetworkErrors < 0)
                                        {
                                            networkRequest.NetworkErrors = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["totalNumNetworkErrors"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(networkRequestDetailsObject, "networkErrorsPerMin") == false)
                                    {
                                        networkRequest.NetworkEPM = getLongValueFromJToken(networkRequestDetailsObject["networkErrorsPerMin"], "value");
                                        if (networkRequest.NetworkEPM < 0)
                                        {
                                            networkRequest.NetworkEPM = 0;
                                        }
                                        else
                                        {
                                            networkRequest.MetricsIDs.Add(getLongValueFromJToken(networkRequestDetailsObject["networkErrorsPerMin"], "metricId"));
                                        }
                                    }
                                }

                                // Has Activity
                                if (networkRequest.ART == 0 && networkRequest.TimeTotal == 0 &&
                                    networkRequest.CPM == 0 && networkRequest.Calls == 0)
                                {
                                    networkRequest.HasActivity = false;
                                }
                                else
                                {
                                    networkRequest.HasActivity = true;
                                }

                                // Create metric link
                                if (networkRequest.MetricsIDs != null && networkRequest.MetricsIDs.Count > 0)
                                {
                                    StringBuilder sb = new StringBuilder(256);
                                    foreach (long metricID in networkRequest.MetricsIDs)
                                    {
                                        if (metricID > 0)
                                        {
                                            sb.Append(String.Format(DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID, jobTarget.ParentApplicationID, metricID));
                                            sb.Append(",");
                                        }
                                    }
                                    sb.Remove(sb.Length - 1, 1);
                                    networkRequest.MetricLink = String.Format(DEEPLINK_METRIC, networkRequest.Controller, jobTarget.ParentApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                                }

                                // Load Business Transactions
                                if (networkRequestDetailsObject != null)
                                {
                                    if (isTokenPropertyNull(networkRequestDetailsObject, "eumPageRelatedBusinessTransactionData") == false)
                                    {
                                        foreach (JToken btContainerToken in networkRequestDetailsObject["eumPageRelatedBusinessTransactionData"])
                                        {
                                            JObject btContainerObject = (JObject)btContainerToken.First();

                                            MOBILENetworkRequestToBusinessTransaction networkRequestToBT = new MOBILENetworkRequestToBusinessTransaction();

                                            networkRequestToBT.Controller = networkRequest.Controller;
                                            networkRequestToBT.ControllerLink = networkRequest.ControllerLink;
                                            networkRequestToBT.ApplicationName = networkRequest.ApplicationName;
                                            networkRequestToBT.ApplicationID = networkRequest.ApplicationID;
                                            networkRequestToBT.ApplicationLink = networkRequest.ApplicationLink;

                                            networkRequestToBT.RequestName = networkRequest.RequestName;
                                            networkRequestToBT.RequestNameInternal = networkRequest.RequestNameInternal;
                                            networkRequestToBT.RequestID = networkRequest.RequestID;

                                            networkRequestToBT.Duration = networkRequest.Duration;
                                            networkRequestToBT.From = networkRequest.From;
                                            networkRequestToBT.To = networkRequest.To;
                                            networkRequestToBT.FromUtc = networkRequest.FromUtc;
                                            networkRequestToBT.ToUtc = networkRequest.ToUtc;

                                            networkRequestToBT.BTID = getLongValueFromJToken(btContainerObject, "businessTransactionId");
                                            networkRequestToBT.BTName = getStringValueFromJToken(btContainerObject, "businessTransactionName");

                                            JobTarget jobTargetofAPM = jobConfiguration.Target.Where(j => j.ApplicationID == getLongValueFromJToken(btContainerObject, "applicationId") && j.Type == APPLICATION_TYPE_WEB).FirstOrDefault();
                                            List<APMBusinessTransaction> businessTransactionsList = null;
                                            if (jobTargetofAPM != null)
                                            {
                                                businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTargetofAPM), new APMBusinessTransactionReportMap());
                                            }

                                            if (businessTransactionsList != null && networkRequestToBT.BTID != 0)
                                            {
                                                APMBusinessTransaction businessTransaction = businessTransactionsList.Where(b => b.BTID == networkRequestToBT.BTID).FirstOrDefault();
                                                if (businessTransaction != null)
                                                {
                                                    networkRequestToBT.BTType = businessTransaction.BTType;
                                                    networkRequestToBT.TierName = businessTransaction.TierName;
                                                    networkRequestToBT.TierID = businessTransaction.TierID;
                                                }
                                            }

                                            if (isTokenPropertyNull(btContainerObject, "averageResponseTime") == false)
                                            {
                                                networkRequestToBT.ART = getLongValueFromJToken(btContainerObject["averageResponseTime"], "value");
                                                if (networkRequestToBT.ART < 0)
                                                {
                                                    networkRequestToBT.ART = 0;
                                                }
                                                networkRequestToBT.ARTRange = getDurationRangeAsString(networkRequestToBT.ART);
                                            }

                                            if (isTokenPropertyNull(btContainerObject, "callsPerMinute") == false)
                                            {
                                                networkRequestToBT.CPM = getLongValueFromJToken(btContainerObject["callsPerMinute"], "value");
                                                if (networkRequestToBT.CPM < 0)
                                                {
                                                    networkRequestToBT.CPM = 0;
                                                }
                                            }

                                            if (isTokenPropertyNull(btContainerObject, "totalNumberOfCalls") == false)
                                            {
                                                networkRequestToBT.Calls = getLongValueFromJToken(btContainerObject["totalNumberOfCalls"], "value");
                                                if (networkRequestToBT.Calls < 0)
                                                {
                                                    networkRequestToBT.Calls = 0;
                                                }
                                            }

                                            // Has Activity
                                            if (networkRequestToBT.ART == 0 && 
                                                networkRequestToBT.CPM == 0 && networkRequestToBT.Calls == 0)
                                            {
                                                networkRequestToBT.HasActivity = false;
                                            }
                                            else
                                            {
                                                networkRequestToBT.HasActivity = true;
                                            }

                                            networkRequest.NumBTs++;

                                            networkRequestToBTsList.Add(networkRequestToBT);
                                        }
                                    }
                                }

                                networkRequestsList.Add(networkRequest);
                            }

                            // Sort them
                            networkRequestsList = networkRequestsList.OrderBy(o => o.RequestName).ToList();
                            FileIOHelper.WriteListToCSVFile(networkRequestsList, new MOBILENetworkRequestReportMap(), FilePathMap.MOBILENetworkRequestsIndexFilePath(jobTarget));

                            networkRequestToBTsList = networkRequestToBTsList.OrderBy(o => o.RequestName).ThenBy(o => o.TierName).ThenBy(o => o.BTType).ThenBy(o => o.BTName).ToList();
                            FileIOHelper.WriteListToCSVFile(networkRequestToBTsList, new MOBILENetworkRequestToBusinessTransactionReportMap(), FilePathMap.MOBILENetworkRequestsBusinessTransactionsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + networkRequestsList.Count;
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("Index Application");

                        MOBILEApplication application = new MOBILEApplication();

                        application.Controller = jobTarget.Controller;
                        application.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_THIS_TIMERANGE);
                        application.ApplicationName = jobTarget.Application;
                        application.ApplicationID = jobTarget.ApplicationID;
                        application.ApplicationLink = String.Format(DEEPLINK_MOBILE_APPLICATION, application.Controller, jobTarget.ParentApplicationID, application.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                        if (networkRequestsList != null)
                        {
                            application.NumNetworkRequests = networkRequestsList.Count;

                            application.NumActivity = networkRequestsList.Count(p => p.HasActivity == true);
                            application.NumNoActivity = networkRequestsList.Count(p => p.HasActivity == false);
                        }

                        List<MOBILEApplication> applicationsList = new List<MOBILEApplication>(1);
                        applicationsList.Add(application);

                        FileIOHelper.WriteListToCSVFile(applicationsList, new MOBILEApplicationReportMap(), FilePathMap.MOBILEApplicationsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.MOBILEEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.MOBILEEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.MOBILEApplicationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MOBILEApplicationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MOBILEApplicationsReportFilePath(), FilePathMap.MOBILEApplicationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.MOBILENetworkRequestsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MOBILENetworkRequestsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MOBILENetworkRequestsReportFilePath(), FilePathMap.MOBILENetworkRequestsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.MOBILENetworkRequestsBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.MOBILENetworkRequestsBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.MOBILENetworkRequestsBusinessTransactionsReportFilePath(), FilePathMap.MOBILENetworkRequestsBusinessTransactionsIndexFilePath(jobTarget));
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
            logger.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            loggerConsole.Trace("Input.DetectedEntities={0}", jobConfiguration.Input.DetectedEntities);
            if (jobConfiguration.Input.DetectedEntities == false)
            {
                loggerConsole.Trace("Skipping index of detected entities");
            }
            return (jobConfiguration.Input.DetectedEntities == true);
        }
    }
}
