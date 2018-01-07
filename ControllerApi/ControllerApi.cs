using NLog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;

namespace AppDynamics.Dexter
{
    public class ControllerApi
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region Private variables

        private HttpClient _httpClient;
        private CookieContainer _cookieContainer;

        private System.Net.Security.RemoteCertificateValidationCallback ignoreBadCertificates = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });

        #endregion

        #region Public properties

        public string ControllerUrl { get; set; }
        public string ControllerName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        #endregion

        #region Constructor and overrides

        public ControllerApi(string controllerURL, string userName, string userPassword)
        {
            this.ControllerUrl = controllerURL;
            this.ControllerName = new Uri(this.ControllerUrl).Host;
            this.UserName = userName;
            this.Password = userPassword;

            this._cookieContainer = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = this._cookieContainer;

            HttpClient httpClient = new HttpClient(handler);
            httpClient.Timeout = new TimeSpan(0, 3, 0);
            httpClient.BaseAddress = new Uri(this.ControllerUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", this.UserName, this.Password))));
            httpClient.DefaultRequestHeaders.Add("User-Agent", String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version));

            // If customer controller certificates are not in trusted store, let's not fail
            ServicePointManager.ServerCertificateValidationCallback += ignoreBadCertificates;

            // If customer controller is still leveraging old TLS or SSL3 protocols, enable that
#if (NETCOREAPP2_0)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
#else
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
#endif

            this._httpClient = httpClient;
        }

        public override String ToString()
        {
            return String.Format(
                "ControllerApi: ControllerUrl='{0}', UserName='{1}'",
                this.ControllerUrl,
                this.UserName);
        }

        #endregion

        #region Check for controller accessibility and login

        public bool IsControllerAccessible()
        {
            return (this.GetListOfApplications() != String.Empty);
        }

        public void PrivateApiLogin()
        {
            this.apiGET("controller/auth?action=login", "text/plain", false);
        }

        #endregion

        #region Metadata retrieval

        public string GetControllerConfiguration()
        {
            return this.apiGET("controller/rest/configuration?output=json", "application/json", false);
        }

        public string GetApplicationConfiguration(int applicationID)
        {
            return this.apiGET(String.Format("controller/ConfigObjectImportExportServlet?applicationId={0}", applicationID), "text/xml", false);
        }

        public string GetListOfApplications()
        {
            return this.apiGET("controller/rest/applications?output=JSON", "application/json", false);
        }

        public string GetSingleApplication(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}?output=JSON", applicationName), "application/json", false);
        }

        public string GetSingleApplication(int applicationID)
        {
            return this.GetSingleApplication(applicationID.ToString());
        }

        public string GetListOfTiers(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/tiers?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfTiers(int applicationID)
        {
            return this.GetListOfTiers(applicationID.ToString());
        }

        public string GetListOfNodes(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/nodes?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfNodes(int applicationID)
        {
            return this.GetListOfNodes(applicationID.ToString());
        }

        public string GetListOfBusinessTransactions(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/business-transactions?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfBusinessTransactions(int applicationID)
        {
            return this.GetListOfBusinessTransactions(applicationID.ToString());
        }

        public string GetListOfBackends(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/backends?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfBackends(int applicationID)
        {
            return this.GetListOfBackends(applicationID.ToString());
        }

        public string GetListOfBackendsAdditionalDetail(int applicationID)
        {
            return this.apiGET(String.Format("controller/restui/backendUiService/backendListViewData/{0}/false?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetListOfServiceEndpoints(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Service Endpoints|*|*|Calls per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfServiceEndpoints(int applicationID)
        {
            return this.GetListOfServiceEndpoints(applicationID.ToString());
        }

        public string GetListOfServiceEndpointsInTier(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Service Endpoints|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetListOfServiceEndpointsInTier(int applicationID, string tierName)
        {
            return this.GetListOfServiceEndpointsInTier(applicationID.ToString(), tierName);
        }

        public string GetListOfServiceEndpointsAdditionalDetail(int applicationID)
        {
            return this.apiGET(String.Format("controller/restui/serviceEndpoint/list2/{0}/{0}/APPLICATION?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetListOfErrors(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Errors|*|*|Errors per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfErrors(int applicationID)
        {
            return this.GetListOfErrors(applicationID.ToString());
        }

        public string GetListOfErrorsInTier(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Errors|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetListOfErrorsInTier(int applicationID, string tierName)
        {
            return this.GetListOfErrorsInTier(applicationID.ToString(), tierName);
        }

        public string GetListOfInformationPoints(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Information Points|*|Calls per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfInformationPoints(int applicationID)
        {
            return this.GetListOfInformationPoints(applicationID.ToString());
        }

        public string GetListOfInformationPointsAdditionalDetail(int applicationID)
        {
            return this.apiGET(String.Format("controller/restui/informationPointUiService/getAllInfoPointsListViewData/{0}?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        #endregion

        #region Flowmap retrieval

        public string GetFlowmapApplication(long applicationID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/applicationFlowMapUiService/application/{0}?time-range=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}&mapId=-1&baselineId=-1",
                    applicationID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetFlowmapTier(long tierID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/componentFlowMapUiService/component/{0}?time-range=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}&mapId=-1&baselineId=-1",
                    tierID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetFlowmapNode(long nodeID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/nodeFlowMapUiService/node/{0}?time-range=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}&mapId=-1&baselineId=-1",
                    nodeID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json", 
                true);
        }

        public string GetFlowmapBusinessTransaction(long applicationID, long businessTransactionID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/btFlowMapUiService/businessTransaction/{0}?applicationId={1}&time-range=Custom_Time_Range.BETWEEN_TIMES.{2}.{3}.{4}&mapId=-1&baselineId=-1",
                    businessTransactionID,
                    applicationID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetFlowmapBackend(long backendID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/backendFlowMapUiService/backend/{0}?time-range=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}&mapId=-1&baselineId=-1",
                    backendID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetFlowmapSnapshot(long applicationID, long businessTransactionID, string requestGUID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/snapshotFlowmap/distributedSnapshotFlow?applicationId={0}&businessTransactionId={1}&requestGUID={2}&eventType=&timeRange=Custom_Time_Range.BETWEEN_TIMES.{3}.{4}.{5}&mapId=-1",
                    applicationID,
                    businessTransactionID,
                    requestGUID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        #endregion

        #region Snapshot retrieval

        public string GetListOfSnapshotsFirstPage(long applicationID, DateTime startTime, DateTime endTime, int durationBetweenTimes, int maxRows)
        {
            // Full body of the snapshot re
            //@"{\
            //    ""firstInChain"": true,
            //    ""maxRows"": {4},
            //    ""applicationIds"": [{0}],
            //    ""businessTransactionIds"": [],
            //    ""applicationComponentIds"": [],
            //    ""applicationComponentNodeIds"": [],
            //    ""errorIDs"": [],
            //    ""errorOccured"": null,
            //    ""userExperience"": [],
            //    ""executionTimeInMilis"": null,
            //    ""endToEndLatency"": null,
            //    ""url"": null,
            //    ""sessionId"": null,
            //    ""userPrincipalId"": null,
            //    ""dataCollectorFilter"": null,
            //    ""archived"": null,
            //    ""guids"": [],
            //    ""diagnosticSnapshot"": null,
            //    ""badRequest"": null,
            //    ""deepDivePolicy"": [],
            //    ""rangeSpecifier"": {
            //        ""type"": ""BETWEEN_TIMES"",
            //        ""startTime"": ""{1:o}"",
            //        ""endTime"": ""{2:o}"",
            //        ""durationInMinutes"": {3}
            //    }
            //}";

            string requestJSONTemplate =
@"{{
    ""firstInChain"": true,
    ""maxRows"": {4},
    ""applicationIds"": [{0}],
    ""rangeSpecifier"": {{
        ""type"": ""BETWEEN_TIMES"",
        ""startTime"": ""{1:o}"",
        ""endTime"": ""{2:o}"",
        ""durationInMinutes"": {3}
    }}
}}";

            // The controller expects the data in one of the following forms for java.util.Date:
            // "yyyy-MM-dd'T'HH:mm:ss.SSSZ"
            // "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'"
            // "EEE, dd MMM yyyy HH:mm:ss zzz", 
            // "yyyy-MM-dd"
            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                startTime.ToUniversalTime(),
                endTime.ToUniversalTime(),
                durationBetweenTimes,
                maxRows);

            return this.apiPOST("controller/restui/snapshot/snapshotListDataWithFilterHandle", "application/json", requestBody, "application/json", true);
        }

        public string GetListOfSnapshotsNextPage_Type_rsdScrollId(long applicationID, DateTime startTime, DateTime endTime, int durationBetweenTimes, int maxRows, long serverCursorId)
        {
            string requestJSONTemplate =
@"{{
    ""firstInChain"": true,
    ""maxRows"": {4},
    ""applicationIds"": [{0}],
    ""rangeSpecifier"": {{
        ""type"": ""BETWEEN_TIMES"",
        ""startTime"": ""{1:o}"",
        ""endTime"": ""{2:o}"",
        ""durationInMinutes"": {3}
    }},
    ""serverCursor"": {{
        ""rsdScrollId"": {5}
    }}
}}";

            // The controller expects the data in one of the following forms for java.util.Date:
            // "yyyy-MM-dd'T'HH:mm:ss.SSSZ"
            // "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'"
            // "EEE, dd MMM yyyy HH:mm:ss zzz", 
            // "yyyy-MM-dd"
            string requestBody = String.Format(requestJSONTemplate, 
                applicationID, 
                startTime.ToUniversalTime(), 
                endTime.ToUniversalTime(),
                durationBetweenTimes,
                maxRows,
                serverCursorId);

            return this.apiPOST("controller/restui/snapshot/snapshotListDataWithFilterHandle", "application/json", requestBody, "application/json", true);
        }

        public string GetListOfSnapshotsNextPage_Type_scrollId(long applicationID, DateTime startTime, DateTime endTime, int durationBetweenTimes, int maxRows, long serverCursorId)
        {
            string requestJSONTemplate =
@"{{
    ""firstInChain"": true,
    ""maxRows"": {4},
    ""applicationIds"": [{0}],
    ""rangeSpecifier"": {{
        ""type"": ""BETWEEN_TIMES"",
        ""startTime"": ""{1:o}"",
        ""endTime"": ""{2:o}"",
        ""durationInMinutes"": {3}
    }},
    ""serverCursor"": {{
        ""scrollId"": {5}
    }}
}}";

            // The controller expects the data in one of the following forms for java.util.Date:
            // "yyyy-MM-dd'T'HH:mm:ss.SSSZ"
            // "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'"
            // "EEE, dd MMM yyyy HH:mm:ss zzz", 
            // "yyyy-MM-dd"
            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                startTime.ToUniversalTime(),
                endTime.ToUniversalTime(),
                durationBetweenTimes,
                maxRows,
                serverCursorId);

            return this.apiPOST("controller/restui/snapshot/snapshotListDataWithFilterHandle", "application/json", requestBody, "application/json", true);
        }

        public string GetListOfSnapshotsNextPage_Type_handle(long applicationID, DateTime startTime, DateTime endTime, int durationBetweenTimes, int maxRows, long serverCursorId)
        {
            // This was build for 4.2.3 and earlier. I can't figure out what endRequestId is supposed to be. So this will only ever retrieve maxRows rows max
            string requestJSONTemplate =
@"{{
    ""firstInChain"": true,
    ""maxRows"": {4},
    ""applicationIds"": [{0}],
    ""rangeSpecifier"": {{
        ""type"": ""BETWEEN_TIMES"",
        ""startTime"": ""{1:o}"",
        ""endTime"": ""{2:o}"",
        ""durationInMinutes"": {3}
    }},
    ""startingRequestId"": 1,
    ""endRequestId"": 1,
    ""handle"": {5}
}}";

            // The controller expects the data in one of the following forms for java.util.Date:
            // "yyyy-MM-dd'T'HH:mm:ss.SSSZ"
            // "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'"
            // "EEE, dd MMM yyyy HH:mm:ss zzz", 
            // "yyyy-MM-dd"
            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                startTime.ToUniversalTime(),
                endTime.ToUniversalTime(),
                durationBetweenTimes,
                maxRows,
                serverCursorId);

            return this.apiPOST("controller/restui/snapshot/snapshotListDataWithFilterHandle", "application/json", requestBody, "application/json", true);
        }

        public string GetSnapshotSegments(string requestGUID, DateTime startTime, DateTime endTime, int durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""guids"": [""{0}""],
    ""needExitCalls"": true,
    ""needProps"": true,
    ""rangeSpecifier"": {{
        ""type"": ""BETWEEN_TIMES"",
        ""startTime"": ""{1:o}"",
        ""endTime"": ""{2:o}"",
        ""durationInMinutes"": {3}
    }}
}}";
            string requestBody = String.Format(requestJSONTemplate,
                requestGUID,
                startTime.ToUniversalTime(),
                endTime.ToUniversalTime(),
                durationBetweenTimes);

            return this.apiPOST("controller/restui/snapshot/getFilteredRSDListData", "application/json", requestBody, "application/json", true);
        }

        public string GetSnapshotSegmentDetails(long requestSegmentID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, int durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/snapshot/getRequestSegmentById?rsdId={0}&timeRange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                    requestSegmentID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json", 
                true);
        }

        public string GetSnapshotSegmentErrors(long requestSegmentID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, int durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/snapshot/getErrorsForRsd/{0}?limit=30&timeRange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                    requestSegmentID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetSnapshotSegmentCallGraph(long requestSegmentID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, int durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/snapshot/getCallGraphRoot?rsdId={0}&timeRange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                    requestSegmentID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetProcessSnapshotCallGraph(string requestGUID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, int durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/processSnapshot/getProcessCallGraphRoot?processSnapshotGUID={0}&timeRange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                    requestGUID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        #endregion

        #region Metric retrieval

        public string GetMetricData(string applicationNameOrID, string metricPath, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, bool rollup)
        {
            return this.apiGET(
                String.Format("controller/rest/applications/{0}/metric-data?metric-path={1}&time-range-type=BETWEEN_TIMES&start-time={2}&end-time={3}&rollup={4}&output=JSON",
                    applicationNameOrID,
                    WebUtility.UrlEncode(metricPath),
                    startTimeInUnixEpochFormat,
                    endTimeInUnixEpochFormat,
                    rollup),
                "application/json",
                false);
        }

        public string GetMetricData(int applicationID, string metricPath, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, bool rollup)
        {
            return this.GetMetricData(applicationID.ToString(), metricPath, startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, rollup);
        }

        #endregion

        #region Event retrieval

        public string GetHealthRuleViolations(int applicationID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            return this.apiGET(
                String.Format("controller/rest/applications/{0}/problems/healthrule-violations?time-range-type=BETWEEN_TIMES&start-time={1}&end-time={2}&output=JSON",
                    applicationID,
                    startTimeInUnixEpochFormat,
                    endTimeInUnixEpochFormat),
                "application/json",
                true);
        }

        public string GetEvents(int applicationID, string eventType, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            
            return this.apiGET(
                String.Format("controller/rest/applications/{0}/events?event-types={1}&severities=INFO,WARN,ERROR&time-range-type=BETWEEN_TIMES&start-time={2}&end-time={3}&output=JSON",
                    applicationID,
                    eventType,
                    startTimeInUnixEpochFormat,
                    endTimeInUnixEpochFormat),
                "application/json",
                true);
        }

        #endregion

        #region Data retrieval 

        /// <summary>
        /// Invokes Controller API using GET method
        /// </summary>
        /// <param name="restAPIUrl">REST URL to retrieve with GET</param>
        /// <param name="acceptHeader">Desired Content Type of response</param>
        /// <returns>Raw results if successful, empty string otherwise</returns>
        private string apiGET(string restAPIUrl, string acceptHeader, bool useXSRFHeader)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                MediaTypeWithQualityHeaderValue accept = new MediaTypeWithQualityHeaderValue(acceptHeader);
                if (this._httpClient.DefaultRequestHeaders.Accept.Contains(accept) == false)
                {
                    this._httpClient.DefaultRequestHeaders.Accept.Add(accept);
                }
                //this._httpClient.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                if (useXSRFHeader == true)
                {
                    if (this._httpClient.DefaultRequestHeaders.Contains("X-CSRF-TOKEN") == false)
                    {
                        // Add CSRF cookie if available
                        Cookie cookieXSRF = this._cookieContainer.GetCookies(new Uri(this._httpClient.BaseAddress, "controller/auth"))["X-CSRF-TOKEN"];
                        if (cookieXSRF != null)
                        {
                            this._httpClient.DefaultRequestHeaders.Add("X-CSRF-TOKEN", cookieXSRF.Value);
                        }
                    }
                }

                HttpResponseMessage response = this._httpClient.GetAsync(restAPIUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    logger.Error("{0}/{1} GET as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase);

                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Error("{0}/{1} GET as {2} threw {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, ex.Message, ex.Source);
                logger.Error(ex);

                return String.Empty;
            }
            finally
            {
                stopWatch.Stop();
                logger.Trace("{0}/{1} GET as {2} took {3:c} ({4} ms)", this.ControllerUrl, restAPIUrl, this.UserName, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Invokes Controller API using POST method
        /// </summary>
        /// <param name="restAPIUrl">REST URL to retrieve with POST</param>
        /// <param name="acceptHeader">Desired Content Type of response</param>
        /// <param name="requestBody">Body of the message</param>
        /// <returns>Raw results if successful, empty string otherwise</returns>
        private string apiPOST(string restAPIUrl, string acceptHeader, string requestBody, string requestTypeHeader, bool useXSRFHeader)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                MediaTypeWithQualityHeaderValue accept = new MediaTypeWithQualityHeaderValue(acceptHeader);
                if (this._httpClient.DefaultRequestHeaders.Accept.Contains(accept) == false)
                {
                    this._httpClient.DefaultRequestHeaders.Accept.Add(accept);
                }
                //this._httpClient.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                if (useXSRFHeader == true)
                {
                    if (this._httpClient.DefaultRequestHeaders.Contains("X-CSRF-TOKEN") == false)
                    {
                        // Add CSRF cookie if available
                        Cookie cookieXSRF = this._cookieContainer.GetCookies(new Uri(this._httpClient.BaseAddress, "controller/auth"))["X-CSRF-TOKEN"];
                        if (cookieXSRF != null)
                        {
                            this._httpClient.DefaultRequestHeaders.Add("X-CSRF-TOKEN", cookieXSRF.Value);
                        }
                    }
                }
                StringContent content = new StringContent(requestBody);
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(requestTypeHeader);

                HttpResponseMessage response = this._httpClient.PostAsync(restAPIUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    logger.Error("{0}/{1} POST as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase);

                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Error("{0}/{1} POST as {2} threw {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, ex.Message, ex.Source);
                logger.Error(ex);

                return String.Empty;
            }
            finally
            {
                stopWatch.Stop();
                logger.Trace("{0}/{1} POST as {2} took {3:c} ({4} ms)", this.ControllerUrl, restAPIUrl, this.UserName, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds);
                logger.Trace("POST body {0}", requestBody);
            }
        }

        #endregion
    }
}
