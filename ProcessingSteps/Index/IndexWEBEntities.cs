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
    public class IndexWEBEntities : JobStepIndexBase
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

                if (jobConfiguration.Target.Count(t => t.Type == APPLICATION_TYPE_WEB) == 0)
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

                    if (jobTarget.Type != null && jobTarget.Type.Length > 0 && jobTarget.Type != APPLICATION_TYPE_WEB) continue;

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

                        #region EUM Pages

                        List<WEBPage> webPagesList = null;
                        List<WEBPageToBusinessTransaction> webPageToBTsList = null;
                        List<WEBPageToWebPage> webPageToWebPagesList = null;

                        JObject webPagesContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBPagesDataFilePath(jobTarget));                        
                        if (isTokenPropertyNull(webPagesContainerObject, "data") == false)
                        {
                            JArray webPagesArray = (JArray)webPagesContainerObject["data"];

                            loggerConsole.Info("Index List of Web Pages ({0} entities)", webPagesArray.Count);

                            webPagesList = new List<WEBPage>(webPagesArray.Count);
                            webPageToBTsList = new List<WEBPageToBusinessTransaction>(webPagesArray.Count * 4);
                            webPageToWebPagesList = new List<WEBPageToWebPage>(webPagesArray.Count * 4);

                            foreach (JObject webPageObject in webPagesArray)
                            {
                                WEBPage webPage = new WEBPage();
                                webPage.Controller = jobTarget.Controller;
                                webPage.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_THIS_TIMERANGE);
                                webPage.ApplicationName = jobTarget.Application;
                                webPage.ApplicationID = jobTarget.ApplicationID;
                                webPage.ApplicationLink = String.Format(DEEPLINK_WEB_APPLICATION, webPage.Controller, webPage.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                                webPage.PageType = getStringValueFromJToken(webPageObject, "type");
                                webPage.PageName = getStringValueFromJToken(webPageObject, "name");
                                webPage.PageID = getLongValueFromJToken(webPageObject, "addId");
                                webPage.PageLink = String.Format(DEEPLINK_WEB_PAGE, webPage.Controller, webPage.ApplicationID, webPage.PageID, DEEPLINK_THIS_TIMERANGE);

                                string[] pageNameTokens = webPage.PageName.Split('/');
                                webPage.NumNameSegments = pageNameTokens.Length;
                                if (pageNameTokens.Length > 0)
                                {
                                    webPage.FirstSegment = pageNameTokens[0];
                                }

                                webPage.Duration = differenceInMinutes;
                                webPage.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
                                webPage.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
                                webPage.FromUtc = jobConfiguration.Input.TimeRange.From;
                                webPage.ToUtc = jobConfiguration.Input.TimeRange.To;

                                webPage.IsSynthetic = getBoolValueFromJToken(webPageObject, "synthetic");

                                // Now to metrics
                                webPage.MetricsIDs = new List<long>(16);

                                JObject webPageDetailsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBPagePerformanceDataFilePath(jobTarget, webPage.PageType, webPage.PageName, webPage.PageID, jobConfiguration.Input.TimeRange));
                                if (isTokenPropertyNull(webPageDetailsContainerObject, "rum") == false)
                                {
                                    JObject webPageDetailsObject = (JObject)webPageDetailsContainerObject["rum"];

                                    webPage.IsCorrelated = getBoolValueFromJToken(webPageDetailsObject, "hasServerAgentCorrelation");
                                    webPage.IsNavTime = getBoolValueFromJToken(webPageDetailsObject, "hasNavTimeData");
                                    webPage.IsCookie = getBoolValueFromJToken(webPageDetailsObject, "hasCookieData");

                                    // More metrics
                                    if (isTokenPropertyNull(webPageDetailsObject, "endUserResponseTime") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["endUserResponseTime"], "graphData") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["endUserResponseTime"]["graphData"], "data") == false)
                                    {
                                        webPage.TimeTotal = sumLongValuesInArray((JArray)webPageDetailsObject["endUserResponseTime"]["graphData"]["data"]);
                                    }

                                    webPage.ART = 0;
                                    if (isTokenPropertyNull(webPageDetailsObject, "endUserResponseTime") == false)
                                    {
                                        webPage.ART = getLongValueFromJToken(webPageDetailsObject["endUserResponseTime"], "value");
                                        if (webPage.ART < 0)
                                        {
                                            webPage.ART = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["endUserResponseTime"], "metricId"));
                                        }
                                    }
                                    webPage.ARTRange = getDurationRangeAsString(webPage.ART);

                                    if (isTokenPropertyNull(webPageDetailsObject, "requestsPerMinute") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["requestsPerMinute"], "graphData") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["requestsPerMinute"]["graphData"], "data") == false)
                                    {
                                        webPage.Calls = sumLongValuesInArray((JArray)webPageDetailsObject["requestsPerMinute"]["graphData"]["data"]);
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "requestsPerMinute") == false)
                                    {
                                        webPage.CPM = getLongValueFromJToken(webPageDetailsObject["requestsPerMinute"], "value");
                                        if (webPage.CPM < 0)
                                        {
                                            webPage.CPM = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["requestsPerMinute"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "domInteractiveTime") == false)
                                    {
                                        webPage.DOMReady = getLongValueFromJToken(webPageDetailsObject["domInteractiveTime"], "value");
                                        if (webPage.DOMReady < 0)
                                        {
                                            webPage.DOMReady = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["domInteractiveTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "firstByteTime") == false)
                                    {
                                        webPage.FirstByte = getLongValueFromJToken(webPageDetailsObject["firstByteTime"], "value");
                                        if (webPage.FirstByte < 0)
                                        {
                                            webPage.FirstByte = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["firstByteTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "serverConnectionTime") == false)
                                    {
                                        webPage.ServerConnection = getLongValueFromJToken(webPageDetailsObject["serverConnectionTime"], "value");
                                        if (webPage.ServerConnection < 0)
                                        {
                                            webPage.ServerConnection = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["serverConnectionTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "dnsTime") == false)
                                    {
                                        webPage.DNS = getLongValueFromJToken(webPageDetailsObject["dnsTime"], "value");
                                        if (webPage.DNS < 0)
                                        {
                                            webPage.DNS = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["dnsTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "tcpConnectionTime") == false)
                                    {
                                        webPage.TCP = getLongValueFromJToken(webPageDetailsObject["tcpConnectionTime"], "value");
                                        if (webPage.TCP < 0)
                                        {
                                            webPage.TCP = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["tcpConnectionTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "sslTlsTime") == false)
                                    {
                                        webPage.SSL = getLongValueFromJToken(webPageDetailsObject["sslTlsTime"], "value");
                                        if (webPage.SSL < 0)
                                        {
                                            webPage.SSL = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["sslTlsTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "serverTime") == false)
                                    {
                                        webPage.Server = getLongValueFromJToken(webPageDetailsObject["serverTime"], "value");
                                        if (webPage.Server < 0)
                                        {
                                            webPage.Server = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["serverTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "documentDownloadTime") == false)
                                    {
                                        webPage.HTMLDownload = getLongValueFromJToken(webPageDetailsObject["documentDownloadTime"], "value");
                                        if (webPage.HTMLDownload < 0)
                                        {
                                            webPage.HTMLDownload = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["documentDownloadTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "documentProcessingTime") == false)
                                    {
                                        webPage.DOMBuild = getLongValueFromJToken(webPageDetailsObject["documentProcessingTime"], "value");
                                        if (webPage.DOMBuild < 0)
                                        {
                                            webPage.DOMBuild = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["documentProcessingTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "pageRenderTime") == false)
                                    {
                                        webPage.ResourceFetch = getLongValueFromJToken(webPageDetailsObject["pageRenderTime"], "value");
                                        if (webPage.ResourceFetch < 0)
                                        {
                                            webPage.ResourceFetch = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["pageRenderTime"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "jsErrorsPerMinute") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["jsErrorsPerMinute"], "graphData") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["jsErrorsPerMinute"]["graphData"], "data") == false)
                                    {
                                        webPage.JSErrors = sumLongValuesInArray((JArray)webPageDetailsObject["jsErrorsPerMinute"]["graphData"]["data"]);
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "jsErrorsPerMinute") == false)
                                    {
                                        webPage.JSEPM = getLongValueFromJToken(webPageDetailsObject["jsErrorsPerMinute"], "value");
                                        if (webPage.JSEPM < 0)
                                        {
                                            webPage.JSEPM = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["jsErrorsPerMinute"], "metricId"));
                                        }
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "ajaxErrorsPerMinute") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["ajaxErrorsPerMinute"], "graphData") == false &&
                                        isTokenPropertyNull(webPageDetailsObject["ajaxErrorsPerMinute"]["graphData"], "data") == false)
                                    {
                                        webPage.AJAXErrors = sumLongValuesInArray((JArray)webPageDetailsObject["ajaxErrorsPerMinute"]["graphData"]["data"]);
                                    }

                                    if (isTokenPropertyNull(webPageDetailsObject, "ajaxErrorsPerMinute") == false)
                                    {
                                        webPage.AJAXEPM = getLongValueFromJToken(webPageDetailsObject["ajaxErrorsPerMinute"], "value");
                                        if (webPage.AJAXEPM < 0)
                                        {
                                            webPage.AJAXEPM = 0;
                                        }
                                        else
                                        {
                                            webPage.MetricsIDs.Add(getLongValueFromJToken(webPageDetailsObject["ajaxErrorsPerMinute"], "metricId"));
                                        }
                                    }
                                }

                                // Has Activity
                                if (webPage.ART == 0 && webPage.TimeTotal == 0 &&
                                    webPage.CPM == 0 && webPage.Calls == 0)
                                {
                                    webPage.HasActivity = false;
                                }
                                else
                                {
                                    webPage.HasActivity = true;
                                }

                                // Create metric link
                                if (webPage.MetricsIDs != null && webPage.MetricsIDs.Count > 0)
                                {
                                    StringBuilder sb = new StringBuilder(256);
                                    foreach (long metricID in webPage.MetricsIDs)
                                    {
                                        if (metricID > 0)
                                        {
                                            sb.Append(String.Format(DEEPLINK_METRIC_APPLICATION_TARGET_METRIC_ID, webPage.ApplicationID, metricID));
                                            sb.Append(",");
                                        }
                                    }
                                    sb.Remove(sb.Length - 1, 1);
                                    webPage.MetricLink = String.Format(DEEPLINK_METRIC, webPage.Controller, webPage.ApplicationID, sb.ToString(), DEEPLINK_THIS_TIMERANGE);
                                }

                                // Load Business Transactions
                                if (webPageDetailsContainerObject != null)
                                {
                                    if (isTokenPropertyNull(webPageDetailsContainerObject, "rum") == false)
                                    {
                                        JObject webPageDetailsObject = (JObject)webPageDetailsContainerObject["rum"];

                                        if (isTokenPropertyNull(webPageDetailsObject, "eumPageRelatedBusinessTransactionData") == false)
                                        {
                                            foreach (JToken btContainerToken in webPageDetailsObject["eumPageRelatedBusinessTransactionData"])
                                            {
                                                JObject btContainerObject = (JObject)btContainerToken.First();

                                                WEBPageToBusinessTransaction webPageToBT = new WEBPageToBusinessTransaction();

                                                webPageToBT.Controller = webPage.Controller;
                                                webPageToBT.ControllerLink = webPage.ControllerLink;
                                                webPageToBT.ApplicationName = webPage.ApplicationName;
                                                webPageToBT.ApplicationID = webPage.ApplicationID;
                                                webPageToBT.ApplicationLink = webPage.ApplicationLink;

                                                webPageToBT.PageName = webPage.PageName;
                                                webPageToBT.PageType = webPage.PageType;
                                                webPageToBT.PageID = webPage.PageID;

                                                webPageToBT.Duration = webPage.Duration;
                                                webPageToBT.From = webPage.From;
                                                webPageToBT.To = webPage.To;
                                                webPageToBT.FromUtc = webPage.FromUtc;
                                                webPageToBT.ToUtc = webPage.ToUtc;

                                                webPageToBT.BTID = getLongValueFromJToken(btContainerObject, "businessTransactionId");
                                                webPageToBT.BTName = getStringValueFromJToken(btContainerObject, "businessTransactionName");

                                                JobTarget jobTargetofAPM = jobConfiguration.Target.Where(j => j.ApplicationID == getLongValueFromJToken(btContainerObject, "applicationId") && j.Type == APPLICATION_TYPE_WEB).FirstOrDefault();
                                                List<APMBusinessTransaction> businessTransactionsList = null;
                                                if (jobTargetofAPM != null)
                                                {
                                                    businessTransactionsList = FileIOHelper.ReadListFromCSVFile<APMBusinessTransaction>(FilePathMap.APMBusinessTransactionsIndexFilePath(jobTargetofAPM), new APMBusinessTransactionReportMap());
                                                }

                                                if (businessTransactionsList != null && webPageToBT.BTID != 0)
                                                {
                                                    APMBusinessTransaction businessTransaction = businessTransactionsList.Where(b => b.BTID == webPageToBT.BTID).FirstOrDefault();
                                                    if (businessTransaction != null)
                                                    {
                                                        webPageToBT.BTType = businessTransaction.BTType;
                                                        webPageToBT.TierName = businessTransaction.TierName;
                                                        webPageToBT.TierID = businessTransaction.TierID;
                                                    }
                                                }

                                                if (isTokenPropertyNull(btContainerObject, "averageResponseTime") == false)
                                                {
                                                    webPageToBT.ART = getLongValueFromJToken(btContainerObject["averageResponseTime"], "value");
                                                    if (webPageToBT.ART < 0)
                                                    {
                                                        webPageToBT.ART = 0;
                                                    }
                                                    webPageToBT.ARTRange = getDurationRangeAsString(webPageToBT.ART);
                                                }

                                                if (isTokenPropertyNull(btContainerObject, "callsPerMinute") == false)
                                                {
                                                    webPageToBT.CPM = getLongValueFromJToken(btContainerObject["callsPerMinute"], "value");
                                                    if (webPageToBT.CPM < 0)
                                                    {
                                                        webPageToBT.CPM = 0;
                                                    }
                                                }

                                                if (isTokenPropertyNull(btContainerObject, "totalNumberOfCalls") == false)
                                                {
                                                    webPageToBT.Calls = getLongValueFromJToken(btContainerObject["totalNumberOfCalls"], "value");
                                                    if (webPageToBT.Calls < 0)
                                                    {
                                                        webPageToBT.Calls = 0;
                                                    }
                                                }

                                                // Has Activity
                                                if (webPageToBT.ART == 0 &&
                                                    webPageToBT.CPM == 0 && webPageToBT.Calls == 0)
                                                {
                                                    webPageToBT.HasActivity = false;
                                                }
                                                else
                                                {
                                                    webPageToBT.HasActivity = true;
                                                }

                                                webPage.NumBTs++;

                                                webPageToBTsList.Add(webPageToBT);
                                            }
                                        }
                                    }
                                }

                                // Load child requests
                                if (webPageDetailsContainerObject != null)
                                {
                                    if (isTokenPropertyNull(webPageDetailsContainerObject, "rum") == false)
                                    {
                                        JObject webPageDetailsObject = (JObject)webPageDetailsContainerObject["rum"];

                                        if (isTokenPropertyNull(webPageDetailsObject, "eumPageRequestData") == false)
                                        {
                                            foreach (JToken childWebPageContainerToken in webPageDetailsObject["eumPageRequestData"])
                                            {
                                                JObject childWebPageContainerObject = (JObject)childWebPageContainerToken.First();

                                                WEBPageToWebPage webPageToWebPage = new WEBPageToWebPage();

                                                webPageToWebPage.Controller = webPage.Controller;
                                                webPageToWebPage.ControllerLink = webPage.ControllerLink;
                                                webPageToWebPage.ApplicationName = webPage.ApplicationName;
                                                webPageToWebPage.ApplicationID = webPage.ApplicationID;
                                                webPageToWebPage.ApplicationLink = webPage.ApplicationLink;

                                                webPageToWebPage.PageName = webPage.PageName;
                                                webPageToWebPage.PageType = webPage.PageType;
                                                webPageToWebPage.PageID = webPage.PageID;

                                                webPageToWebPage.Duration = webPage.Duration;
                                                webPageToWebPage.From = webPage.From;
                                                webPageToWebPage.To = webPage.To;
                                                webPageToWebPage.FromUtc = webPage.FromUtc;
                                                webPageToWebPage.ToUtc = webPage.ToUtc;

                                                webPageToWebPage.ChildPageName = getStringValueFromJToken(childWebPageContainerObject, "addName");
                                                webPageToWebPage.ChildPageType = getStringValueFromJToken(childWebPageContainerObject, "addType");
                                                webPageToWebPage.ChildPageID = getLongValueFromJToken(childWebPageContainerObject, "addId");

                                                if (isTokenPropertyNull(childWebPageContainerObject, "averageResponseTime") == false)
                                                {
                                                    webPageToWebPage.ART = getLongValueFromJToken(childWebPageContainerObject["averageResponseTime"], "value");
                                                    if (webPageToWebPage.ART < 0)
                                                    {
                                                        webPageToWebPage.ART = 0;
                                                    }
                                                    webPageToWebPage.ARTRange = getDurationRangeAsString(webPageToWebPage.ART);
                                                }

                                                if (isTokenPropertyNull(childWebPageContainerObject, "requestsPerMinute") == false)
                                                {
                                                    webPageToWebPage.CPM = getLongValueFromJToken(childWebPageContainerObject["requestsPerMinute"], "value");
                                                    if (webPageToWebPage.CPM < 0)
                                                    {
                                                        webPageToWebPage.CPM = 0;
                                                    }
                                                }

                                                if (isTokenPropertyNull(childWebPageContainerObject, "totalNumberOfRequests") == false)
                                                {
                                                    webPageToWebPage.Calls = getLongValueFromJToken(childWebPageContainerObject["totalNumberOfRequests"], "value");
                                                    if (webPageToWebPage.Calls < 0)
                                                    {
                                                        webPageToWebPage.Calls = 0;
                                                    }
                                                }

                                                // Has Activity
                                                if (webPageToWebPage.ART == 0 &&
                                                    webPageToWebPage.CPM == 0 && webPageToWebPage.Calls == 0)
                                                {
                                                    webPageToWebPage.HasActivity = false;
                                                }
                                                else
                                                {
                                                    webPageToWebPage.HasActivity = true;
                                                }

                                                webPage.NumPages++;

                                                webPageToWebPagesList.Add(webPageToWebPage);
                                            }
                                        }
                                    }
                                }


                                webPagesList.Add(webPage);
                            }

                            // Sort them
                            webPagesList = webPagesList.OrderBy(o => o.PageType).ThenBy(o => o.PageName).ToList();
                            FileIOHelper.WriteListToCSVFile(webPagesList, new WEBPageReportMap(), FilePathMap.WEBPagesIndexFilePath(jobTarget));

                            webPageToBTsList = webPageToBTsList.OrderBy(o => o.PageName).ThenBy(o => o.TierName).ThenBy(o => o.BTType).ThenBy(o => o.BTName).ToList();
                            FileIOHelper.WriteListToCSVFile(webPageToBTsList, new WEBPageToBusinessTransactionReportMap(), FilePathMap.WEBPageBusinessTransactionsIndexFilePath(jobTarget));

                            webPageToWebPagesList = webPageToWebPagesList.OrderBy(o => o.PageName).ThenBy(o => o.ChildPageType).ThenBy(o => o.ChildPageName).ToList();
                            FileIOHelper.WriteListToCSVFile(webPageToWebPagesList, new WEBPageToWebPageReportMap(), FilePathMap.WEBPageResourcesIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + webPagesList.Count;
                        }

                        #endregion

                        #region Geo Locations

                        List<WEBGeoLocation> geoLocationsList = new List<WEBGeoLocation>(1024);

                        JObject geoRegionsContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBGeoLocationsDataFilePath(jobTarget, "all"));

                        if (geoRegionsContainerObject != null)
                        {
                            if (isTokenPropertyNull(geoRegionsContainerObject, "rumItems") == false)
                            {
                                JArray geoRegionsArray = (JArray)geoRegionsContainerObject["rumItems"];

                                loggerConsole.Info("Index List of Geo Locations - Real ({0} entities)", geoRegionsArray.Count);

                                foreach (JObject geoRegionObject in geoRegionsArray)
                                {
                                    WEBGeoLocation webGeoLocation = fillGeoLocation(geoRegionObject, jobTarget, jobConfiguration, differenceInMinutes, DEEPLINK_THIS_TIMERANGE);
                                    webGeoLocation.LocationType = "REAL";
                                    geoLocationsList.Add(webGeoLocation);

                                    if (isTokenPropertyNull(geoRegionObject, "eumRegionPerformanceSummaryData") == false)
                                    {
                                        string country = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"], "country");

                                        JObject geoRegionContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBGeoLocationsDataFilePath(jobTarget, country));

                                        if (geoRegionContainerObject != null)
                                        {
                                            if (isTokenPropertyNull(geoRegionContainerObject, "rumItems") == false)
                                            {
                                                JArray geoRegionArray = (JArray)geoRegionContainerObject["rumItems"];

                                                foreach (JObject geoRegionSubRegionObject in geoRegionArray)
                                                {
                                                    WEBGeoLocation webGeoLocationSubRegion = fillGeoLocation(geoRegionSubRegionObject, jobTarget, jobConfiguration, differenceInMinutes, DEEPLINK_THIS_TIMERANGE);
                                                    webGeoLocationSubRegion.LocationType = "REAL";
                                                    geoLocationsList.Add(webGeoLocationSubRegion);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (isTokenPropertyNull(geoRegionsContainerObject, "syntheticItems") == false)
                            {
                                JArray geoRegionsArray = (JArray)geoRegionsContainerObject["syntheticItems"];

                                loggerConsole.Info("Index List of Geo Locations - Synthetic ({0} entities)", geoRegionsArray.Count);

                                foreach (JObject geoRegionObject in geoRegionsArray)
                                {
                                    WEBGeoLocation webGeoLocation = fillGeoLocation(geoRegionObject, jobTarget, jobConfiguration, differenceInMinutes, DEEPLINK_THIS_TIMERANGE);
                                    webGeoLocation.LocationType = "SYNTHETIC";
                                    geoLocationsList.Add(webGeoLocation);

                                    if (isTokenPropertyNull(geoRegionObject, "eumRegionPerformanceSummaryData") == false)
                                    {
                                        string country = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"], "country");

                                        JObject geoRegionContainerObject = FileIOHelper.LoadJObjectFromFile(FilePathMap.WEBGeoLocationsDataFilePath(jobTarget, country));

                                        if (geoRegionContainerObject != null)
                                        {
                                            if (isTokenPropertyNull(geoRegionContainerObject, "syntheticItems") == false)
                                            {
                                                JArray geoRegionArray = (JArray)geoRegionContainerObject["syntheticItems"];

                                                foreach (JObject geoRegionSubRegionObject in geoRegionArray)
                                                {
                                                    WEBGeoLocation webGeoLocationSubRegion = fillGeoLocation(geoRegionSubRegionObject, jobTarget, jobConfiguration, differenceInMinutes, DEEPLINK_THIS_TIMERANGE);
                                                    webGeoLocationSubRegion.LocationType = "SYNTHETIC";
                                                    geoLocationsList.Add(webGeoLocationSubRegion);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Sort them
                            geoLocationsList = geoLocationsList.OrderBy(o => o.LocationType).ThenBy(o => o.Country).ThenBy(o => o.Region).ToList();
                            FileIOHelper.WriteListToCSVFile(geoLocationsList, new WEBGeoLocationReportMap(), FilePathMap.WEBGeoLocationsIndexFilePath(jobTarget));

                            stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + geoLocationsList.Count;
                        }

                        #endregion

                        #region Application

                        loggerConsole.Info("Index Application");

                        WEBApplication application = new WEBApplication();

                        application.Controller = jobTarget.Controller;
                        application.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_THIS_TIMERANGE);
                        application.ApplicationName = jobTarget.Application;
                        application.ApplicationID = jobTarget.ApplicationID;
                        application.ApplicationLink = String.Format(DEEPLINK_WEB_APPLICATION, application.Controller, application.ApplicationID, DEEPLINK_THIS_TIMERANGE);

                        if (webPagesList != null)
                        {
                            application.NumPages = webPagesList.Count(p => p.PageType == "BASE_PAGE");
                            application.NumAJAXRequests = webPagesList.Count(p => p.PageType == "AJAX_REQUEST");
                            application.NumVirtualPages = webPagesList.Count(p => p.PageType == "VIRTUAL_PAGE");
                            application.NumIFrames = webPagesList.Count(p => p.PageType == "IFRAME");

                            application.NumActivity = webPagesList.Count(p => p.HasActivity == true);
                            application.NumNoActivity = webPagesList.Count(p => p.HasActivity == false);
                        }

                        if (geoLocationsList != null)
                        {
                            application.NumRealGeoLocations = geoLocationsList.Count(g => g.LocationType == "REAL" && g.Region.Length == 0);
                            application.NumRealGeoLocationsRegion = geoLocationsList.Count(g => g.LocationType == "REAL" && g.Region.Length > 0);

                            application.NumSynthGeoLocations = geoLocationsList.Count(g => g.LocationType == "SYNTHETIC" && g.Region.Length == 0);
                            application.NumSynthGeoLocationsRegion = geoLocationsList.Count(g => g.LocationType == "SYNTHETIC" && g.Region.Length > 0);
                        }

                        List<WEBApplication> applicationsList = new List<WEBApplication>(1);
                        applicationsList.Add(application);

                        FileIOHelper.WriteListToCSVFile(applicationsList, new WEBApplicationReportMap(), FilePathMap.WEBApplicationsIndexFilePath(jobTarget));

                        stepTimingTarget.NumEntities = stepTimingTarget.NumEntities + 1;

                        #endregion

                        #region Combine All for Report CSV

                        // If it is the first one, clear out the combined folder
                        if (reportFolderCleaned == false)
                        {
                            FileIOHelper.DeleteFolder(FilePathMap.WEBEntitiesReportFolderPath());
                            Thread.Sleep(1000);
                            FileIOHelper.CreateFolder(FilePathMap.WEBEntitiesReportFolderPath());
                            reportFolderCleaned = true;
                        }

                        // Append all the individual report files into one
                        if (File.Exists(FilePathMap.WEBApplicationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBApplicationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBApplicationsReportFilePath(), FilePathMap.WEBApplicationsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.WEBPagesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBPagesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBPagesReportFilePath(), FilePathMap.WEBPagesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.WEBPageBusinessTransactionsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBPageBusinessTransactionsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBPageBusinessTransactionsReportFilePath(), FilePathMap.WEBPageBusinessTransactionsIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.WEBPageResourcesIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBPageResourcesIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBPageResourcesReportFilePath(), FilePathMap.WEBPageResourcesIndexFilePath(jobTarget));
                        }
                        if (File.Exists(FilePathMap.WEBGeoLocationsIndexFilePath(jobTarget)) == true && new FileInfo(FilePathMap.WEBGeoLocationsIndexFilePath(jobTarget)).Length > 0)
                        {
                            FileIOHelper.AppendTwoCSVFiles(FilePathMap.WEBGeoLocationsReportFilePath(), FilePathMap.WEBGeoLocationsIndexFilePath(jobTarget));
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

        private WEBGeoLocation fillGeoLocation(JObject geoRegionObject, JobTarget jobTarget, JobConfiguration jobConfiguration, int differenceInMinutes, string DEEPLINK_THIS_TIMERANGE)
        {
            WEBGeoLocation webGeoLocation = new WEBGeoLocation();

            webGeoLocation.Controller = jobTarget.Controller;
            webGeoLocation.ControllerLink = String.Format(DEEPLINK_CONTROLLER, jobTarget.Controller, DEEPLINK_THIS_TIMERANGE);
            webGeoLocation.ApplicationName = jobTarget.Application;
            webGeoLocation.ApplicationID = jobTarget.ApplicationID;
            webGeoLocation.ApplicationLink = String.Format(DEEPLINK_WEB_APPLICATION, webGeoLocation.Controller, webGeoLocation.ApplicationID, DEEPLINK_THIS_TIMERANGE);

            webGeoLocation.LocationName = getStringValueFromJToken(geoRegionObject, "locationName");

            if (isTokenPropertyNull(geoRegionObject, "eumRegionPerformanceSummaryData") == false)
            {
                webGeoLocation.Country = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"], "country");
                webGeoLocation.Region = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"], "region");
                webGeoLocation.City = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"], "city");

                if (isTokenPropertyNull(geoRegionObject["eumRegionPerformanceSummaryData"], "geoCode") == false)
                {
                    webGeoLocation.GeoCode = getStringValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"]["geoCode"], "key");
                    webGeoLocation.Latitude = getDoubleValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"]["geoCode"], "latitude");
                    if (webGeoLocation.Latitude < 0) webGeoLocation.Latitude = 0;
                    webGeoLocation.Longitude = getDoubleValueFromJToken(geoRegionObject["eumRegionPerformanceSummaryData"]["geoCode"], "longitude");
                    if (webGeoLocation.Longitude < 0) webGeoLocation.Longitude = 0;
                }
            }

            webGeoLocation.ART = getLongValueFromJToken(geoRegionObject, "endUserResponseTime");
            if (webGeoLocation.ART < 0) webGeoLocation.ART = 0;
            webGeoLocation.ARTRange = getDurationRangeAsString(webGeoLocation.ART);

            webGeoLocation.Calls = getLongValueFromJToken(geoRegionObject, "pageRequests");
            if (webGeoLocation.Calls < 0) webGeoLocation.Calls = 0;

            webGeoLocation.CPM = getLongValueFromJToken(geoRegionObject, "pageRequestsPerMinute");
            if (webGeoLocation.CPM < 0) webGeoLocation.CPM = 0;

            webGeoLocation.DOMReady = getLongValueFromJToken(geoRegionObject, "domReadyTime");
            if (webGeoLocation.DOMReady < 0) webGeoLocation.DOMReady = 0;

            webGeoLocation.FirstByte = getLongValueFromJToken(geoRegionObject, "firstByteTime");
            if (webGeoLocation.FirstByte < 0) webGeoLocation.FirstByte = 0;

            webGeoLocation.ServerConnection = getLongValueFromJToken(geoRegionObject, "serverConnectionTime");
            if (webGeoLocation.ServerConnection < 0) webGeoLocation.ServerConnection = 0;

            webGeoLocation.HTMLDownload = getLongValueFromJToken(geoRegionObject, "htmlDownloadTime");
            if (webGeoLocation.HTMLDownload < 0) webGeoLocation.HTMLDownload = 0;

            webGeoLocation.DOMBuild = getLongValueFromJToken(geoRegionObject, "domBuildingTime");
            if (webGeoLocation.DOMBuild < 0) webGeoLocation.DOMBuild = 0;

            webGeoLocation.ResourceFetch = getLongValueFromJToken(geoRegionObject, "resourceFetchTime");
            if (webGeoLocation.ResourceFetch < 0) webGeoLocation.ResourceFetch = 0;

            webGeoLocation.JSErrors = getLongValueFromJToken(geoRegionObject, "pageViewsWithJavascriptErrors");
            if (webGeoLocation.JSErrors < 0) webGeoLocation.JSErrors = 0;

            webGeoLocation.JSEPM = getLongValueFromJToken(geoRegionObject, "pageViewsWithJavascriptErrorsPerMinute");
            if (webGeoLocation.JSEPM < 0) webGeoLocation.JSEPM = 0;

            webGeoLocation.AJAXEPM = getLongValueFromJToken(geoRegionObject, "ajaxRequestErrorsPerMinute");
            if (webGeoLocation.AJAXEPM < 0) webGeoLocation.AJAXEPM = 0;

            webGeoLocation.Duration = differenceInMinutes;
            webGeoLocation.From = jobConfiguration.Input.TimeRange.From.ToLocalTime();
            webGeoLocation.To = jobConfiguration.Input.TimeRange.To.ToLocalTime();
            webGeoLocation.FromUtc = jobConfiguration.Input.TimeRange.From;
            webGeoLocation.ToUtc = jobConfiguration.Input.TimeRange.To;

            // Has Activity
            if (webGeoLocation.ART == 0 &&
                webGeoLocation.CPM == 0 &&
                webGeoLocation.Calls == 0)
            {
                webGeoLocation.HasActivity = false;
            }
            else
            {
                webGeoLocation.HasActivity = true;
            }

            return webGeoLocation;
        }
    }
}
