using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace AppDynamics.OfflineData
{
    public class ControllerApi
    {
        #region Private variables

        private HttpClient _httpClient;
        private CookieContainer _cookieContainer;
        private Cookie _cookieXSRF;

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

            //CookieContainer cookieContainer = new CookieContainer();
            this._cookieContainer = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = this._cookieContainer;

            HttpClient httpClient = new HttpClient(handler);
            httpClient.Timeout = new TimeSpan(0, 3, 0);
            httpClient.BaseAddress = new Uri(this.ControllerUrl);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", this.UserName, this.Password))));

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

        public string GetApplicationConfiguration(int applicationID)
        {
            return this.apiGET(String.Format("controller/ConfigObjectImportExportServlet?applicationId={0}", applicationID), "text/xml", false);
        }

        public string GetListOfApplications()
        {
            return this.apiGET("controller/rest/applications?output=JSON", "application/json", false);
        }

        public string GetSingleApplication(string applicationNameOrID)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}?output=JSON", applicationNameOrID), "application/json", false);
        }

        public string GetListOfTiers(string applicationNameOrID)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/tiers?output=JSON", applicationNameOrID), "application/json", false);
        }

        public string GetListOfNodes(string applicationNameOrID)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/nodes?output=JSON", applicationNameOrID), "application/json", false);
        }

        public string GetListOfBusinessTransactions(string applicationNameOrID)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/business-transactions?output=JSON", applicationNameOrID), "application/json", false);
        }

        public string GetListOfBackends(string applicationNameOrID)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/backends?output=JSON", applicationNameOrID), "application/json", false);
        }

        public string GetListOfServiceEndpoints(int applicationID)
        {
            return this.apiGET(String.Format("controller/restui/serviceEndpoint/list2/{0}/{0}/APPLICATION?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetListOfServiceEndpoints(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Service Endpoints|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetListOfErrors(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Errors|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        #endregion

        #region Flowmap retrieval

        public string GetFlowmapApplication(int applicationID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
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

        public string GetFlowmapTier(int tierID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
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

        public string GetFlowmapNode(int nodeID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
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

        public string GetFlowmapBusinessTransaction(int applicationID, int businessTransactionID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
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

        public string GetFlowmapBackend(int backendID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
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

        public string GetFlowmapSnapshot(int applicationID, int businessTransactionID, string requestGUID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
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

        public string GetListOfSnapshots(int applicationID, DateTime startTime, DateTime endTime, int durationBetweenTimes, int maxRows, long serverCursorId)
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
	}}{5}
}}";
            #region Full body
            //@"{\
            //    ""firstInChain"": true,
            //	""maxRows"": {4},
            //	""applicationIds"": [{0}],
            //	""businessTransactionIds"": [],
            //	""applicationComponentIds"": [],
            //	""applicationComponentNodeIds"": [],
            //	""errorIDs"": [],
            //	""errorOccured"": null,
            //	""userExperience"": [],
            //	""executionTimeInMilis"": null,
            //	""endToEndLatency"": null,
            //	""url"": null,
            //	""sessionId"": null,
            //	""userPrincipalId"": null,
            //	""dataCollectorFilter"": null,
            //	""archived"": null,
            //	""guids"": [],
            //	""diagnosticSnapshot"": null,
            //	""badRequest"": null,
            //	""deepDivePolicy"": [],
            //	""rangeSpecifier"": {
            //		""type"": ""BETWEEN_TIMES"",
            //		""startTime"": ""{1:o}"",
            //		""endTime"": ""{2:o}"",
            //		""durationInMinutes"": {3}
            //	}
            //}";
            #endregion

            string cursorJSONTemplate =
@",
	""serverCursor"": {{
		""rsdScrollId"": {0}
	}}
";

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
                serverCursorId > 0 ? String.Format(cursorJSONTemplate, serverCursorId) : String.Empty);

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
                this._httpClient.DefaultRequestHeaders.Accept.Clear();
                this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
                this._httpClient.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                if (useXSRFHeader == true)
                {
                    // Add CSRF cookie if available
                    Cookie cookieXSRF = this._cookieContainer.GetCookies(new Uri(this._httpClient.BaseAddress, "controller/auth"))["X-CSRF-TOKEN"];
                    if (cookieXSRF != null)
                    {
                        this._httpClient.DefaultRequestHeaders.Add("X-CSRF-TOKEN", cookieXSRF.Value);
                    }
                }

                HttpResponseMessage response = this._httpClient.GetAsync(restAPIUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Error,
                        EventId.CONTROLLER_REST_API_ERROR,
                        "ControllerApi.apiGET",
                        String.Format("{0}/{1} as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase));
                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.CONTROLLER_REST_API_ERROR,
                    "ControllerApi.apiGET",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.CONTROLLER_REST_API_ERROR,
                    "ControllerApi.apiGET",
                    String.Format("{0}/{1} as {2} threw {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, ex.Message, ex.Source));

                return String.Empty;
            }
            finally
            {
                stopWatch.Stop();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FUNCTION_DURATION_EVENT,
                    "ControllerApi.apiGET",
                    String.Format("{0}/{1} as {2} took {3:c} ({4} ms)", this.ControllerUrl, restAPIUrl, this.UserName, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds));
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
                this._httpClient.DefaultRequestHeaders.Accept.Clear();
                this._httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
                this._httpClient.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
                if (useXSRFHeader == true)
                {
                    // Add CSRF cookie if available
                    Cookie cookieXSRF = this._cookieContainer.GetCookies(new Uri(this._httpClient.BaseAddress, "controller/auth"))["X-CSRF-TOKEN"];
                    if (cookieXSRF != null)
                    {
                        this._httpClient.DefaultRequestHeaders.Add("X-CSRF-TOKEN", cookieXSRF.Value);
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
                    LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                        TraceEventType.Error,
                        EventId.CONTROLLER_REST_API_ERROR,
                        "ControllerApi.apiPOST",
                        String.Format("{0}/{1} with {5} as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase, requestBody));
                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.CONTROLLER_REST_API_ERROR,
                    "ControllerApi.apiPOST",
                    ex);

                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Error,
                    EventId.CONTROLLER_REST_API_ERROR,
                    "ControllerApi.apiPOST",
                    String.Format("{0}/{1} with {5} as {2} threw {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, ex.Message, ex.Source, requestBody));

                return String.Empty;
            }
            finally
            {
                stopWatch.Stop();
                LogHelper.Instance.Log(new string[] { LogHelper.OFFLINE_DATA_TRACE_SOURCE },
                    TraceEventType.Verbose,
                    EventId.FUNCTION_DURATION_EVENT,
                    "ControllerApi.apiPOST",
                    String.Format("{0}/{1} with {5} as {2} took {3:c} ({4} ms)", this.ControllerUrl, restAPIUrl, this.UserName, stopWatch.Elapsed.ToString("c"), stopWatch.ElapsedMilliseconds, requestBody));
            }
        }

        #endregion

    }
}
