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
            // Yes, that's not particularly secure, but it makes the tool work on the machines where the certificates in the controller are not trusted by the retrieving client
            ServicePointManager.ServerCertificateValidationCallback += ignoreBadCertificates;

            // If customer controller is still leveraging old TLS or SSL3 protocols, enable that
#if (NETCOREAPP2_1)
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
            return (this.GetApplicationsAPM() != String.Empty);
        }

        public void PrivateApiLogin()
        {
            this.apiGET("controller/auth?action=login", "text/plain", false);
        }

        #endregion

        #region Controller configuration

        public string GetControllerVersion()
        {
            return this.apiGET("controller/rest/serverstatus", "text/xml", false);
        }

        public string GetControllerConfiguration()
        {
            return this.apiGET("controller/rest/configuration?output=json", "application/json", false);
        }

        #endregion

        #region All Applications metadata

        public string GetAllApplicationsAllTypes()
        {
            return this.apiGET("controller/restui/applicationManagerUiBean/getApplicationsAllTypes", "application/json", true);
        }

        #endregion

        #region APM Application configuration

        public string GetApplicationConfiguration(long applicationID)
        {
            return this.apiGET(String.Format("controller/ConfigObjectImportExportServlet?applicationId={0}", applicationID), "text/xml", false);
        }

        public string GetAccountsMyAccount()
        {
            return this.apiGET("api/accounts/myaccount", "application/vnd.appd.cntrl+json", false);
        }

        public string GetApplicationSEPConfiguration(long accountID, long applicationID)
        {
            return this.apiGET(String.Format("api/accounts/{0}/applications/{1}/sep", accountID, applicationID), "application/vnd.appd.cntrl+json", false);
        }

        public string GetDeveloperModeConfiguration(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/applicationManagerUiBean/getDevModeConfig/{0}", applicationID), "application/json", true);
        }

        #endregion

        #region APM metadata

        public string GetApplicationsAPM()
        {
            return this.apiGET("controller/rest/applications?output=JSON", "application/json", false);
        }

        public string GetSingleApplicationAPM(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}?output=JSON", applicationName), "application/json", false);
        }

        public string GetSingleApplicationAPM(long applicationID)
        {
            return this.GetSingleApplicationAPM(applicationID.ToString());
        }

        public string GetListOfTiers(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/tiers?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfTiers(long applicationID)
        {
            return this.GetListOfTiers(applicationID.ToString());
        }

        public string GetListOfNodes(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/nodes?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfNodes(long applicationID)
        {
            return this.GetListOfNodes(applicationID.ToString());
        }

        public string GetNodeProperties(long nodeID)
        {
            return this.apiGET(String.Format("controller/restui/nodeUiService/appAgentByNodeId/{0}", nodeID), "application/json", true);
        }

        public string GetNodeMetadata(long applicationID, long nodeID)
        {
            return this.apiGET(String.Format("controller/restui/components/getNodeViewData/{0}/{1}", applicationID, nodeID), "application/json", true);
        }

        public string GetListOfBusinessTransactions(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/business-transactions?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfBusinessTransactions(long applicationID)
        {
            return this.GetListOfBusinessTransactions(applicationID.ToString());
        }

        public string GetListOfBackends(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/backends?output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfBackends(long applicationID)
        {
            return this.GetListOfBackends(applicationID.ToString());
        }

        public string GetListOfBackendsAdditionalDetail(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/backendUiService/backendListViewData/{0}/false?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetBackendToDBMonMapping(long backendID)
        {
            return this.apiGET(String.Format("controller/restui/databases/backendMapping/getMappedDBServer?backendId={0}", backendID), "application/json", true);
        }

        public string GetBackendToTierMapping(long tierID)
        {
            return this.apiGET(String.Format("controller/restui/backendUiService/resolvedBackendsForTier/{0}", tierID), "application/json", true);
        }

        public string GetListOfServiceEndpoints(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Service Endpoints|*|*|Calls per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfServiceEndpoints(long applicationID)
        {
            return this.GetListOfServiceEndpoints(applicationID.ToString());
        }

        public string GetListOfServiceEndpointsInTier(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Service Endpoints|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetListOfServiceEndpointsInTier(long applicationID, string tierName)
        {
            return this.GetListOfServiceEndpointsInTier(applicationID.ToString(), tierName);
        }

        public string GetListOfServiceEndpointsAdditionalDetail(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/serviceEndpoint/list2/{0}/{0}/APPLICATION?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetListOfErrors(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Errors|*|*|Errors per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfErrors(long applicationID)
        {
            return this.GetListOfErrors(applicationID.ToString());
        }

        public string GetListOfErrorsInTier(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Errors|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetListOfErrorsInTier(long applicationID, string tierName)
        {
            return this.GetListOfErrorsInTier(applicationID.ToString(), tierName);
        }

        public string GetListOfInformationPoints(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Information Points|*|Calls per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetListOfInformationPoints(long applicationID)
        {
            return this.GetListOfInformationPoints(applicationID.ToString());
        }

        public string GetListOfInformationPointsAdditionalDetail(long applicationID)
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

        public string GetMetricData(long applicationID, string metricPath, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, bool rollup)
        {
            return this.GetMetricData(applicationID.ToString(), metricPath, startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, rollup);
        }

        #endregion

        #region Event retrieval

        public string GetHealthRuleViolations(long applicationID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            return this.apiGET(
                String.Format("controller/rest/applications/{0}/problems/healthrule-violations?time-range-type=BETWEEN_TIMES&start-time={1}&end-time={2}&output=JSON",
                    applicationID,
                    startTimeInUnixEpochFormat,
                    endTimeInUnixEpochFormat),
                "application/json",
                false);
        }

        public string GetEvents(long applicationID, string eventType, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            
            return this.apiGET(
                String.Format("controller/rest/applications/{0}/events?event-types={1}&severities=INFO,WARN,ERROR&time-range-type=BETWEEN_TIMES&start-time={2}&end-time={3}&output=JSON",
                    applicationID,
                    eventType,
                    startTimeInUnixEpochFormat,
                    endTimeInUnixEpochFormat),
                "application/json",
                false);
        }

        public string GetNotifications()
        {
            return this.apiGET("controller/restui/notificationUiService/notifications", "application/json", true);
        }

        public string GetAuditEvents(DateTime startTime, DateTime endTime)
        {
            return this.apiGET(String.Format("controller/ControllerAuditHistory?startTime={0:yyyy-MM-ddThh:mm:ss.fff}-0000&endTime={1:yyyy-MM-ddThh:mm:ss.fff}-0000", startTime, endTime), "application/json", false);
        }

        #endregion

        #region SIM metadata 

        public string GetSIMApplication()
        {
            return this.apiGET("controller/sim/v2/user/app", "application/json", false);
        }

        public string GetSIMListOfTiers()
        {
            return this.apiGET("controller/sim/v2/user/app/tiers", "application/json", false);
        }

        public string GetSIMListOfNodes()
        {
            return this.apiGET("controller/sim/v2/user/app/nodes", "application/json", false);
        }

        public string GetSIMListOfMachines()
        {
            return this.apiGET("controller/sim/v2/user/machines", "application/json", false);
        }

        public string GetSIMListOfGroups()
        {
            return this.apiGET("controller/sim/v2/user/groups", "application/json", false);
        }

        public string GetSIMMachine(long machineID)
        {
            return this.apiGET(String.Format("controller/sim/v2/user/machines/{0}", machineID), "application/json", false);
        }

        public string GetSIMMachineDockerContainers(long machineID)
        {
            return this.apiGET(String.Format("controller/sim/v2/user/machines/{0}/docker/containers", machineID), "application/json", false);
        }

        public string GetSIMListOfServiceAvailability()
        {
            return this.apiGET("controller/sim/v2/user/sam/targets/http", "application/json", false);
        }

        public string GetSIMServiceAvailability(long saID)
        {
            return this.apiGET(String.Format("controller/sim/v2/user/sam/targets/http/{0}", saID), "application/json", false);
        }

        #endregion

        #region SIM Data

        public string GetSIMMachineProcesses(long machineID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/sim/v2/user/machines/{0}/processes?timeRange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}&limit=1000&sortBy=CLASS", 
                machineID,
                endTimeInUnixEpochFormat,
                startTimeInUnixEpochFormat,
                durationBetweenTimes), 
            "application/json", 
            false);
        }

        public string GetSIMServiceAvailabilityEvents(long saID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/sim/v2/user/sam/target/{0}/events?timeRange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}&maxCount=5000",
                saID,
                endTimeInUnixEpochFormat,
                startTimeInUnixEpochFormat,
                durationBetweenTimes),
            "application/json",
            false);
        }

        #endregion

        #region EUM metadata

        public string GetApplicationsEUM()
        {
            return this.apiGET("controller/restui/eumApplications/getEumWebApplications", "application/json", true);
        }

        #endregion

        #region DB metadata

        public string GetDBCollectorsConfiguration()
        {
            return this.apiGET("controller/rest/databases/collectors", "application/json", false);
        }

        public string GetDBRegisteredCollectorsCalls45(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
    ""requestFilter"": {{}},
    ""resultColumns"": [""HEALTH"", ""QUERIES"", ""TIME_SPENT"", ""CPU""],
    ""offset"": 0,
    ""limit"": -1,
    ""searchFilters"": [],
    ""columnSorts"": [{{
        ""column"": ""QUERIES"",
        ""direction"": ""DESC""
        }}
    ],
    ""timeRangeStart"": {0},
    ""timeRangeEnd"": {1}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/databasesui/databases/list", "application/json", requestBody, "application/json", true);
        }

        public string GetDBRegisteredCollectorsCalls44(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
    ""requestFilter"": {{}},
    ""resultColumns"": [""HEALTH"", ""QUERIES"", ""TIME_SPENT"", ""CPU""],
    ""offset"": 0,
    ""limit"": -1,
    ""searchFilters"": [],
    ""columnSorts"": [{{
        ""column"": ""QUERIES"",
        ""direction"": ""DESC""
        }}
    ],
    ""timeRangeStart"": {0},
    ""timeRangeEnd"": {1}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/list", "application/json", requestBody, "application/json", true);
        }

        public string GetDBRegisteredCollectorsTimeSpent45(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
    ""requestFilter"": {{}},
    ""resultColumns"": [""HEALTH"", ""QUERIES"", ""TIME_SPENT"", ""CPU""],
    ""offset"": 0,
    ""limit"": -1,
    ""searchFilters"": [],
    ""columnSorts"": [{{
        ""column"": ""TIME_SPENT"",
        ""direction"": ""DESC""
        }}
    ],
    ""timeRangeStart"": {0},
    ""timeRangeEnd"": {1}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/databasesui/databases/list", "application/json", requestBody, "application/json", true);
        }

        public string GetDBRegisteredCollectorsTimeSpent44(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
    ""requestFilter"": {{}},
    ""resultColumns"": [""HEALTH"", ""QUERIES"", ""TIME_SPENT"", ""CPU""],
    ""offset"": 0,
    ""limit"": -1,
    ""searchFilters"": [],
    ""columnSorts"": [{{
        ""column"": ""TIME_SPENT"",
        ""direction"": ""DESC""
        }}
    ],
    ""timeRangeStart"": {0},
    ""timeRangeEnd"": {1}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/list", "application/json", requestBody, "application/json", true);
        }

        public string GetDBAllWaitStates(long dbCollectorID)
        {
            return this.apiGET(String.Format("controller/databasesui/waitStateFiltering/getAllWaitStatesForDBServer/{0}", dbCollectorID), "application/json", true);
        }

        #endregion

        #region DB data

        public string GetDCurrentWaitStates(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long differenceInMinutes)
        {
            return this.apiGET(String.Format("controller/restui/databases/reports/waitReportData?dbId={0}&isCluster=false&timeRange=Custom_Time_Range|BETWEEN_TIMES|{2}|{1}|{3}&topNum=20", dbCollectorID, startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, differenceInMinutes), "application/json", true);
        }

        public string GetDBQueries(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""cluster"": false,
	""serverId"": {0},
	""field"": ""query-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2},
	""waitStateIds"": [],
	""useTimeBasedCorrelation"": false
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBClients(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""field"": ""client-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/clientListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBSessions(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""field"": ""session-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBBlockingSessions(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/databases/getBlockingTreeData?dbId={0}&isCluster=false&timerange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                    dbCollectorID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetDBBlockingSession(long dbCollectorID, long sessionID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return this.apiGET(
                String.Format("controller/restui/databases/getBlockingTreeChildrenData?dbId={0}&isCluster=false&timerange=Custom_Time_Range.BETWEEN_TIMES.{2}.{3}.{4}&blockigSessionId={1}",
                    dbCollectorID,
                    sessionID,
                    endTimeInUnixEpochFormat,
                    startTimeInUnixEpochFormat,
                    durationBetweenTimes),
                "application/json",
                true);
        }

        public string GetDBDatabases(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""field"": ""schema-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBUsers(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""field"": ""db-user-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBModules(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""field"": ""module-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBPrograms(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""field"": ""program-id"",
	""size"": 5000,
	""filterBy"": ""time"",
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);
        }

        public string GetDBBusinessTransactions(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
	""serverId"": {0},
	""cluster"": false,
	""size"": 5000,
	""startTime"": {1},
	""endTime"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                dbCollectorID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/databasesui/snapshot/getBTListViewData", "application/json", requestBody, "application/json", true);
        }

        #endregion

        #region RBAC 

        public string GetUsers()
        {
            return this.apiGET("controller/api/rbac/v1/users", "application/json", false);
        }

        public string GetUsersExtended()
        {
            return this.apiGET("controller/restui/userAdministrationUiService/users", "application/json", true);
        }

        public string GetUser(long userID)
        {
            return this.apiGET(String.Format("controller/api/rbac/v1/users/{0}", userID), "application/json", false);
        }

        public string GetUserExtended(long userID)
        {
            return this.apiGET(String.Format("controller/restui/userAdministrationUiService/users/{0}", userID), "application/json", true);
        }


        public string GetGroups()
        {
            return this.apiGET("controller/api/rbac/v1/groups", "application/json", false);
        }

        public string GetGroupsExtended()
        {
            return this.apiGET("controller/restui/groupAdministrationUiService/groupSummaries", "application/json", true);
        }

        public string GetGroup(long groupID)
        {
            return this.apiGET(String.Format("controller/api/rbac/v1/groups/{0}", groupID), "application/json", false);
        }

        public string GetGroupExtended(long groupID)
        {
            return this.apiGET(String.Format("controller/restui/groupAdministrationUiService/group/{0}", groupID), "application/json", true);
        }

        public string GetUsersInGroup(long groupID)
        {
            return this.apiGET(String.Format("controller/restui/groupAdministrationUiService/groups/userids/{0}", groupID), "application/json", true);
        }


        public string GetRoles()
        {
            return this.apiGET("controller/api/rbac/v1/roles", "application/json", false);
        }

        public string GetRolesExtended()
        {
            return this.apiGET("controller/restui/accountRoleAdministrationUiService/accountRoleSummaries", "application/json", true);
        }

        public string GetRole(long roleID)
        {
            return this.apiGET(String.Format("controller/api/rbac/v1/roles/{0}", roleID), "application/json", false);
        }

        public string GetRoleExtended(long roleID)
        {
            return this.apiGET(String.Format("controller/restui/accountRoleAdministrationUiService/accountRoles/{0}", roleID), "application/json", true);
        }

        public string GetSecurityProviderType()
        {
            return this.apiGET("controller/restui/accountAdmin/getSecurityProviderType", "application/json", true);
        }

        public string GetRequireStrongPasswords()
        {
            return this.apiGET("controller/restui/accountAdmin/getRequireStrongPasswords", "application/json", true);
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
