using NLog;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace AppDynamics.Dexter
{
    public class ControllerApi : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("AppDynamics.Dexter.Console");

        #region Private variables

        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;
        private CookieContainer _cookieContainer;

        #endregion

        #region Public properties

        public string ControllerUrl { get; set; }
        public string ControllerName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public int Timeout
        {
            get
            {
                return this._httpClient.Timeout.Minutes;
            }
            set
            {
                this._httpClient.Timeout = new TimeSpan(0, value, 0);
            }
        }

        #endregion

        #region Constructor, Destructor and overrides

        public ControllerApi(string controllerURL, string userName, string userPassword)
        {
            this.ControllerUrl = controllerURL;
            this.ControllerName = new Uri(this.ControllerUrl).Host;
            this.UserName = userName;
            this.Password = userPassword;

            this._cookieContainer = new CookieContainer();
            this._httpClientHandler = new HttpClientHandler();
            this._httpClientHandler.UseCookies = true;
            this._httpClientHandler.CookieContainer = this._cookieContainer;

            HttpClient httpClient = new HttpClient(this._httpClientHandler);
            // Default to 1 minute timeout. Can be adjusted as needed
            httpClient.Timeout = new TimeSpan(0, 1, 0);
            httpClient.BaseAddress = new Uri(this.ControllerUrl);
            if (this.UserName.ToUpper() == "BEARER")
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.Password);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format("{0}:{1}", this.UserName, this.Password))));
            }
            httpClient.DefaultRequestHeaders.Add("User-Agent", String.Format("AppDynamics DEXTER {0}", Assembly.GetEntryAssembly().GetName().Version));

            // If customer controller certificates are not in trusted store, let's not fail
            // Yes, that's not particularly secure, but it makes the tool work on the machines where the certificates in the controller are not trusted by the retrieving client
            this._httpClientHandler.ServerCertificateCustomValidationCallback =
            (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };

            // If customer controller is still leveraging old TLS or SSL3 protocols, enable that
#if (NETCOREAPP3_0)
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

        public void Dispose()
        {
            this._httpClientHandler.Dispose();
            this._httpClient.Dispose();
        }

        #endregion

        #region Check for controller accessibility and login

        public bool IsControllerAccessible()
        {
            return (this.GetAPMApplications() != String.Empty);
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

        public string GetControllerSettings()
        {
            return this.apiGET("controller/rest/configuration?output=json", "application/json", false);
        }

        public string GetControllerHTTPTemplates()
        {
            return this.apiGET("controller/actiontemplate/httprequest", "application/json", false);
        }
        public string GetControllerHTTPTemplatesDetail()
        {
            return this.apiGET("controller/restui/httpaction/getHttpRequestActionPlanList", "application/json", true);
        }

        public string GetControllerEmailTemplates()
        {
            return this.apiGET("controller/actiontemplate/email", "application/json", false);
        }

        public string GetControllerEmailTemplatesDetail()
        {
            return this.apiGET("controller/restui/emailaction/getCustomEmailActionPlanList", "application/json", true);
        }

        public string GetAccountsMyAccount()
        {
            return this.apiGET("api/accounts/myaccount", "application/vnd.appd.cntrl+json", false);
        }

        #endregion

        #region Dashboards

        public string GetControllerDashboards()
        {
            return this.apiGET("controller/restui/dashboards/getAllDashboardsByType/false", "application/json", true);
        }

        public string GetControllerDashboard(long dashboardID)
        {
            return this.apiGET(String.Format("controller/CustomDashboardImportExportServlet?dashboardId={0}", dashboardID), "application/json", true);
        }

        #endregion

        #region All Applications metadata

        public string GetAllApplicationsAllTypes()
        {
            return this.apiGET("controller/restui/applicationManagerUiBean/getApplicationsAllTypes", "application/json", true);
        }

        public string GetAPMApplications()
        {
            return this.apiGET("controller/rest/applications?output=JSON", "application/json", false);
        }

        public string GetMOBILEApplications()
        {
            return this.apiGET("controller/restui/eumApplications/getAllMobileApplicationsData?time-range=last_1_hour.BEFORE_NOW.-1.-1.60", "application/json", true);
        }

        #endregion

        #region All Application configuration

        public string GetApplicationHealthRules(long applicationID)
        {
            return this.apiGET(String.Format("controller/healthrules/{0}", applicationID), "text/xml", false);
        }

        public string GetApplicationHealthRulesWithIDs(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/policy2/policies/{0}", applicationID), "application/json", true);
        }


        public string GetApplicationPolicies(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/policy/getPolicyListViewData/{0}", applicationID), "application/json", true);
        }

        public string GetApplicationActions(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/policy/getActionsListViewData/{0}", applicationID), "application/json", true);
        }

        #endregion

        #region APM Application configuration

        public string GetAPMConfigurationExportXML(long applicationID)
        {
            return this.apiGET(String.Format("controller/ConfigObjectImportExportServlet?applicationId={0}", applicationID), "text/xml", false);
        }

        public string GetAPMSEPConfiguration(long accountID, long applicationID)
        {
            return this.apiGET(String.Format("api/accounts/{0}/applications/{1}/sep", accountID, applicationID), "application/vnd.appd.cntrl+json", false);
        }

        public string GetAPMDeveloperModeConfiguration(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/applicationManagerUiBean/getDevModeConfig/{0}", applicationID), "application/json", true);
        }

        public string GetAPMConfigurationDetailsJSON(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/applicationManagerUiBean/applicationConfiguration/{0}", applicationID), "application/json", true);
        }

        #endregion

        #region APM metadata

        public string GetAPMApplication(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}?output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMApplication(long applicationID)
        {
            return this.GetAPMApplication(applicationID.ToString());
        }

        public string GetAPMTiers(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/tiers?output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMTiers(long applicationID)
        {
            return this.GetAPMTiers(applicationID.ToString());
        }

        public string GetAPMNodes(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/nodes?output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMNodes(long applicationID)
        {
            return this.GetAPMNodes(applicationID.ToString());
        }

        public string GetAPMNodeProperties(long nodeID)
        {
            return this.apiGET(String.Format("controller/restui/nodeUiService/appAgentByNodeId/{0}", nodeID), "application/json", true);
        }

        public string GetAPMNodeMetadata(long applicationID, long nodeID)
        {
            return this.apiGET(String.Format("controller/restui/components/getNodeViewData/{0}/{1}", applicationID, nodeID), "application/json", true);
        }

        public string GetAPMBusinessTransactions(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/business-transactions?output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMBusinessTransactions(long applicationID)
        {
            return this.GetAPMBusinessTransactions(applicationID.ToString());
        }

        public string GetAPMBusinessTransactionsInOverflow(long tierID, long currentEventCount, long endEventID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""componentId"": {0},
    ""timeRangeSpecifier"": {{
        ""type"": ""BETWEEN_TIMES"",
        ""startTime"": {3},
        ""endTime"": {4},
        ""durationInMinutes"": {5}
    }},
    ""endEventId"": {2},
    ""currentFetchedEventCount"": {1}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                tierID,
                currentEventCount,
                endEventID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST("controller/restui/overflowtraffic/event", "application/json", requestBody, "application/json", true);
        }

        public string GetAPMBackends(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/backends?output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMBackends(long applicationID)
        {
            return this.GetAPMBackends(applicationID.ToString());
        }

        public string GetAPMBackendsAdditionalDetail(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/backendUiService/backendListViewData/{0}/false?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetAPMBackendToDBMonMapping(long backendID)
        {
            return this.apiGET(String.Format("controller/databasesui/databases/backendMapping/getMappedDBServer?backendId={0}", backendID), "application/json", true);
        }

        public string GetAPMBackendToTierMapping(long tierID)
        {
            return this.apiGET(String.Format("controller/restui/backendUiService/resolvedBackendsForTier/{0}", tierID), "application/json", true);
        }

        public string GetAPMServiceEndpoints(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Service Endpoints|*|*|Calls per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMServiceEndpoints(long applicationID)
        {
            return this.GetAPMServiceEndpoints(applicationID.ToString());
        }

        public string GetAPMServiceEndpoints(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Service Endpoints|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetAPMServiceEndpoints(long applicationID, string tierName)
        {
            return this.GetAPMServiceEndpoints(applicationID.ToString(), tierName);
        }

        public string GetAPMServiceEndpointsAdditionalDetail(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/serviceEndpoint/list2/{0}/{0}/APPLICATION?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        public string GetAPMErrors(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Errors|*|*|Errors per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMErrors(long applicationID)
        {
            return this.GetAPMErrors(applicationID.ToString());
        }

        public string GetAPMErrors(string applicationName, string tierName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metrics?metric-path=Errors|{1}&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName, WebUtility.UrlEncode(tierName)), "application/json", false);
        }

        public string GetAPMErrors(long applicationID, string tierName)
        {
            return this.GetAPMErrors(applicationID.ToString(), tierName);
        }

        public string GetAPMInformationPoints(string applicationName)
        {
            return this.apiGET(String.Format("controller/rest/applications/{0}/metric-data?metric-path=Information Points|*|Calls per Minute&time-range-type=BEFORE_NOW&duration-in-mins=15&output=JSON", applicationName), "application/json", false);
        }

        public string GetAPMInformationPoints(long applicationID)
        {
            return this.GetAPMInformationPoints(applicationID.ToString());
        }

        public string GetAPMInformationPointsAdditionalDetail(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/informationPointUiService/getAllInfoPointsListViewData/{0}?time-range=last_15_minutes.BEFORE_NOW.-1.-1.15", applicationID), "application/json", true);
        }

        #endregion

        #region APM Flowmap

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

        #region APM Snapshots

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

        #region APM Metrics

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

        #region Events and Health Rule Violations

        public string GetApplicationHealthRuleViolations(long applicationID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            return this.apiGET(
                String.Format("controller/rest/applications/{0}/problems/healthrule-violations?time-range-type=BETWEEN_TIMES&start-time={1}&end-time={2}&output=JSON",
                    applicationID,
                    startTimeInUnixEpochFormat,
                    endTimeInUnixEpochFormat),
                "application/json",
                false);
        }

        public string GetApplicationEvents(long applicationID, string eventType, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
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

        public string GetControllerNotifications()
        {
            return this.apiGET("controller/restui/notificationUiService/notifications", "application/json", true);
        }

        public string GetControllerAuditEvents(DateTime startTime, DateTime endTime)
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

        #region SIM data

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

        #region WEB metadata

        public string GetWEBPages(long applicationID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
    ""requestFilter"": {{ ""applicationId"": {0}, ""fetchSyntheticData"": false }},
    ""resultColumns"": [ ""PAGE_TYPE"", ""PAGE_NAME"", ""TOTAL_REQUESTS"", ""END_USER_RESPONSE_TIME"" ],
    ""offset"": 0,
    ""limit"": -1,
    ""searchFilters"": [],
    ""columnSorts"": [ {{ ""column"": ""TOTAL_REQUESTS"", ""direction"": ""DESC"" }} ],
    ""timeRangeStart"": {1},
    ""timeRangeEnd"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/web/pagelist", "application/json", requestBody, "application/json", true);
        }

        public string GetWEBPagePerformance(long applicationID, long addID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long differenceInMinutes)
        {
            string requestJSONTemplate =
@"{{
    ""addId"": {1},
    ""applicationId"": {0},
    ""timeRangeString"": ""Custom_Time_Range|BETWEEN_TIMES|{3}|{2}|{4}"",
    ""maxDataPointsForMetricTrends"": 1440,
    ""fetchSyntheticData"": false
}}";

            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                addID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                differenceInMinutes);

            return this.apiPOST("controller/restui/pages/details", "application/json", requestBody, "application/json", true);
        }

        public string GetWEBGeoRegions(long applicationID, string country, string region, string city, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long differenceInMinutes)
        {
            string requestJSONTemplate =
@"{{
    ""applicationId"": {0},
    ""timeRangeString"": ""Custom_Time_Range.BETWEEN_TIMES.{5}.{4}.{6}"",
    ""country"": ""{1}"",
    ""state"": ""{2}"",
    ""city"": ""{3}"",
    ""zipCode"": """"
}}";

            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                country,
                region,
                city,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                differenceInMinutes);

            return this.apiPOST("controller/restui/geoDashboardUiService/getEUMWebGeoDashboardSubLocationsData", "application/json", requestBody, "application/json", true);
        }

        #endregion

        #region WEB configuration

        public string GetEUMorMOBILEApplicationKey(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/eumConfigurationUiService/getAppKey/{0}", applicationID), "application/json", true);
        }

        public string GetEUMApplicationInstrumentationOption(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/browserRUMConfig/getInstrumentationConfig/{0}", applicationID), "application/json", true);
        }

        public string GetEUMApplicationMonitoringState(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/eumConfigurationUiService/isEUMWebEnabled/{0}", applicationID), "application/json", true);
        }

        public string GetEUMConfigPagesAndFrames(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/browserRUMConfig/getPagesAndFramesConfig/{0}", applicationID), "application/json", true);
        }

        public string GetEUMConfigAjax(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/browserRUMConfig/getAJAXConfig/{0}", applicationID), "application/json", true);
        }

        public string GetEUMConfigVirtualPages(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/browserRUMConfig/getVirtualPagesConfig/{0}", applicationID), "application/json", true);
        }

        public string GetEUMConfigErrorDetection(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/browserRUMConfig/getErrorDetectionConfig/{0}", applicationID), "application/json", true);
        }

        public string GetEUMConfigSettings(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/browserRUMConfig/getSettingsConfig/{0}", applicationID), "application/json", true);
        }

        public string GetWEBSyntheticJobs_Before_4_5_13(long applicationID)
        {
            string requestJSONTemplate =
@"{{
    ""applicationId"": {0},
    ""timeRangeString"": ""last_1_hour.BEFORE_NOW.-1.-1.60""
}}";

            string requestBody = String.Format(requestJSONTemplate,
                applicationID);

            return this.apiPOST("controller/restui/synthetic/schedule/getJobList", "application/json", requestBody, "application/json", true);
        }

        public string GetWEBSyntheticJobs(long applicationID)
        {
            string requestBody = String.Empty;

            return this.apiPOST(String.Format("controller/restui/synthetic/schedule/getJobList/{0}", applicationID), "application/json", requestBody, "application/json", true);
        }

        #endregion

        #region MOBILE metadata

        public string GetMOBILENetworkRequests(long applicationID, long mobileApplicationId, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            string requestJSONTemplate =
@"{{
    ""requestFilter"": {{ ""applicationId"": {0}, ""mobileApplicationId"": {1} }},
    ""resultColumns"": [""NETWORK_REQUEST_NAME"", ""NETWORK_REQUEST_ORIGINAL_NAME"", ""TOTAL_REQUESTS"", ""NETWORK_REQUEST_TIME""],
    ""offset"": 0,
    ""limit"": -1,
    ""searchFilters"": [],
    ""columnSorts"": [ {{ ""column"": ""TOTAL_REQUESTS"", ""direction"": ""DESC"" }}	],
    ""timeRangeStart"": {2},
    ""timeRangeEnd"": {3}
}}
";
            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                mobileApplicationId,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat);

            return this.apiPOST("controller/restui/mobile/networkrequestlist", "application/json", requestBody, "application/json", true);
        }

        public string GetMOBILENetworkRequestPerformance(long applicationID, long mobileApplicationId, long addID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long duration)
        {
            string requestJSONTemplate =
@"{{
    ""addId"": {1},
    ""applicationId"": {0},
    ""timeRangeString"": ""Custom_Time_Range|BETWEEN_TIMES|{3}|{2}|{4}""
}}";

            string requestBody = String.Format(requestJSONTemplate,
                applicationID,
                addID,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                duration);

            return this.apiPOST("controller/restui/mobileRequests/requestData", "application/json", requestBody, "application/json", true);
        }

        #endregion

        #region MOBILE configuration

        public string GetMOBILEApplicationInstrumentationOption(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/eumConfigurationUiService/isEUMMobileEnabled/{0}", applicationID), "application/json", true);
        }

        public string GetMobileConfigNetworkRequests(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/mobileRUMConfig/networkRequestsConfig/{0}", applicationID), "application/json", true);
        }

        public string GetMOBILEConfigSettings(long applicationID)
        {
            return this.apiGET(String.Format("controller/restui/mobileRUMConfig/settingsConfig/{0}", applicationID), "application/json", true);
        }

        #endregion

        #region Analytics metadata

        public string GetBIQSearches()
        {
            return this.apiGET("controller/restui/analyticsSavedSearches/getAllAnalyticsSavedSearches", "application/json", true);
        }

        public string GetBIQMetrics()
        {
            return this.apiGET("controller/restui/analyticsMetric/getAnalyticsScheduledQueryReports", "application/json", true);
        }

        public string GetBIQBusinessJourneys(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat)
        {
            return this.apiGET(String.Format("controller/restui/analytics/biz_outcome/definitions/summary?token=&dashboardId=0&isWarRoom=false&startTime={0}&endTime={1}", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat), "application/json", true);
        }

        public string GetBIQExperienceLevels()
        {
            return this.apiGET("controller/restui/analytics/slm/performance-configs?active=true&configuration-name=&offset=1&limit=1000", "application/json", true);
        }

        public string GetBIQCustomSchemas()
        {
            return this.apiGET("controller/restui/analytics/schema", "application/json", true);
        }

        public string GetBIQSchemaFields(string schemaName)
        {
            return this.apiGET(String.Format("controller/restui/analytics/v1/store/metadata/getFieldDefinitions?eventType={0}&token=&dashboardId=0&isWarRoom=false", schemaName), "application/json", true);
        }

        #endregion

        #region DB metadata

        public string GetDBCollectorsConfiguration(string controllerVersion)
        {
            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiGET("controller/rest/databases/collectors", "application/json", false);

                case "4.5":
                    return this.apiGET("controller/databasesui/collectors", "application/json", false);

                default:
                    return String.Empty;
            }
        }

        public string GetDBRegisteredCollectorsCalls(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/list", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/list", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBRegisteredCollectorsTimeSpent(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/list", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/list", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBCustomMetrics(string controllerVersion)
        {
            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiGET("controller/restui/databases/customDBQueryMetrics/getAll", "application/json", true);

                case "4.5":
                    return this.apiGET("controller/databasesui/dbCustomQueryMetrics/getAll", "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBAllWaitStates(long dbCollectorID)
        {
            return this.apiGET(String.Format("controller/databasesui/waitStateFiltering/getAllWaitStatesForDBServer/{0}", dbCollectorID), "application/json", true);
        }

        #endregion

        #region DB data

        public string GetDCurrentWaitStates(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long differenceInMinutes, string controllerVersion)
        {
            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiGET(String.Format("controller/restui/databases/reports/waitReportData?dbId={0}&isCluster=false&timeRange=Custom_Time_Range|BETWEEN_TIMES|{2}|{1}|{3}&topNum=20", dbCollectorID, startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, differenceInMinutes), "application/json", true);

                case "4.5":
                    return this.apiGET(String.Format("controller/databasesui/databases/reports/waitReportData?dbId={0}&isCluster=false&timeRange=Custom_Time_Range|BETWEEN_TIMES|{2}|{1}|{3}&topNum=20", dbCollectorID, startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, differenceInMinutes), "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBQueries(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/queryListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBClients(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/clientListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/clientListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBSessions(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/queryListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBBlockingSessions(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes, string controllerVersion)
        {
            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiGET(
                        String.Format("controller/restui/databases/getBlockingTreeData?dbId={0}&isCluster=false&timerange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                            dbCollectorID,
                            endTimeInUnixEpochFormat,
                            startTimeInUnixEpochFormat,
                            durationBetweenTimes),
                        "application/json",
                        true);

                case "4.5":
                    return this.apiGET(
                        String.Format("controller/databasesui/databases/getBlockingTreeData?dbId={0}&isCluster=false&timerange=Custom_Time_Range.BETWEEN_TIMES.{1}.{2}.{3}",
                            dbCollectorID,
                            endTimeInUnixEpochFormat,
                            startTimeInUnixEpochFormat,
                            durationBetweenTimes),
                        "application/json",
                        true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBBlockingSession(long dbCollectorID, long sessionID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes, string controllerVersion)
        {
            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiGET(
                        String.Format("controller/restui/databases/getBlockingTreeChildrenData?dbId={0}&isCluster=false&timerange=Custom_Time_Range.BETWEEN_TIMES.{2}.{3}.{4}&blockigSessionId={1}",
                            dbCollectorID,
                            sessionID,
                            endTimeInUnixEpochFormat,
                            startTimeInUnixEpochFormat,
                            durationBetweenTimes),
                        "application/json",
                        true);

                case "4.5":
                    return this.apiGET(
                        String.Format("controller/databasesui/databases/getBlockingTreeChildrenData?dbId={0}&isCluster=false&timerange=Custom_Time_Range.BETWEEN_TIMES.{2}.{3}.{4}&blockigSessionId={1}",
                            dbCollectorID,
                            sessionID,
                            endTimeInUnixEpochFormat,
                            startTimeInUnixEpochFormat,
                            durationBetweenTimes),
                        "application/json",
                        true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBDatabases(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/queryListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBUsers(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/queryListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBModules(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/queryListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
        }

        public string GetDBPrograms(long dbCollectorID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, string controllerVersion)
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

            switch (controllerVersion)
            {
                case "4.4":
                    return this.apiPOST("controller/restui/databases/queryListData", "application/json", requestBody, "application/json", true);

                case "4.5":
                    return this.apiPOST("controller/databasesui/databases/queryListData", "application/json", requestBody, "application/json", true);

                default:
                    return String.Empty;
            }
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

        #region RBAC configuration

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

        #region License configuration

        public string GetAccount()
        {
            return this.apiGET("controller/restui/user/account", "application/json", true);
        }

        public string GetLicenseModules(long accountID)
        {
            return this.apiGET(String.Format("api/accounts/{0}/licensemodules", accountID), "application/vnd.appd.cntrl+json", false);
        }

        public string GetLicenseModuleProperties(long accountID, string licenseModule)
        {
            return this.apiGET(String.Format("api/accounts/{0}/licensemodules/{1}/properties", accountID, licenseModule), "application/vnd.appd.cntrl+json", false);
        }

        public string GetLicenseModuleUsages(long accountID, string licenseModule, DateTime from, DateTime to)
        {
            // I really want to use {0:o} but our Controller chokes on this:
            //     yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK
            //                                   ^^^^^^^^^

            return this.apiGET(String.Format("api/accounts/{0}/licensemodules/{1}/usages?startdate={2:yyyy-MM-ddTHH:mm:ssK}&enddate={3:yyyy-MM-ddTHH:mm:ssK}&showfiveminutesresolution=false", accountID, licenseModule, from, to), "application/vnd.appd.cntrl+json", false);
        }

        public string GetLicense(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""type"": ""BETWEEN_TIMES"",
    ""startTime"": {0},
    ""endTime"": {1},
    ""durationInMinutes"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST("controller/restui/licenseRule/getAllLicenseModuleProperties", "application/json", requestBody, "application/json", true);
        }

        public string GetLicenseUsageAllExceptEUMSummary(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""type"": ""BETWEEN_TIMES"",
    ""startTime"": {0},
    ""endTime"": {1},
    ""durationInMinutes"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST("controller/restui/licenseRule/getAccountUsageSummary", "application/json", requestBody, "application/json", true);
        }

        public string GetLicenseUsageEUMSummary(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""type"": ""BETWEEN_TIMES"",
    ""startTime"": {0},
    ""endTime"": {1},
    ""durationInMinutes"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST("controller/restui/licenseRule/getEumLicenseUsage", "application/json", requestBody, "application/json", true);
        }

        public string GetLicenseUsageAPM(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""type"": ""BETWEEN_TIMES"",
    ""startTime"": {0},
    ""endTime"": {1},
    ""durationInMinutes"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST("controller/restui/licenseRule/getUnifiedAccountUsageViewData", "application/json", requestBody, "application/json", true);
        }

        public string GetLicenseUsageDatabase(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("DATABASE", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageMachineAgent(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("MACHINE_AGENT", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageSIM(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("SIM_MACHINE_AGENT", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageServiceAvailability(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("SERVICE_AVAIL_MON", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageNetworkVisibility(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("NETVIZ", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageNetworkTransactionAnalytics(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("TRANSACTION_ANALYTICS", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageNetworkLogAnalytics(long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            return GetLicenseUsageOtherAgent("LOG_ANALYTICS", startTimeInUnixEpochFormat, endTimeInUnixEpochFormat, durationBetweenTimes);
        }

        public string GetLicenseUsageOtherAgent(string licenseType, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
            ""type"": ""BETWEEN_TIMES"",
            ""startTime"": {0},
            ""endTime"": {1},
            ""durationInMinutes"": {2},
            ""timeRange"": null,
            ""timeRangeAdjusted"": false
        }}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST(String.Format("controller/restui/licenseRule/getAccountUsageGraphData/{0}", licenseType), "application/json", requestBody, "application/json", true);
        }

        public string GetLicenseRules()
        {
            return this.apiGET("mds/v1/license/rules", "application/json", false);
        }

        public string GetLicenseRuleUsage(string ruleID, long startTimeInUnixEpochFormat, long endTimeInUnixEpochFormat, long durationBetweenTimes)
        {
            string requestJSONTemplate =
@"{{
    ""type"": ""BETWEEN_TIMES"",
    ""startTime"": {0},
    ""endTime"": {1},
    ""durationInMinutes"": {2}
}}";

            string requestBody = String.Format(requestJSONTemplate,
                startTimeInUnixEpochFormat,
                endTimeInUnixEpochFormat,
                durationBetweenTimes);

            return this.apiPOST(String.Format("controller/restui/licenseRule/getApmLicenseRuleDetailViewData/{0}", ruleID), "application/json", requestBody, "application/json", true);
        }

        public string GetLicenseRuleConfiguration(string ruleID)
        {
            return this.apiGET(String.Format("mds/v1/license/rules/{0}", ruleID), "application/json", false);
        }

        public string GetControllerApplicationsForLicenseRule()
        {
            return this.apiGET("controller/restui/licenseRule/getAllApplications", "application/json", true);
        }

        public string GetControllerSIMMachinesForLicenseRule()
        {
            return this.apiGET("controller/restui/licenseRule/getAllMachines", "application/json", true);
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
                    // For the times when the system throws 500 with some meaningful message
                    string resultString = response.Content.ReadAsStringAsync().Result;
                    if (resultString.Length > 0)
                    {
                        logger.Error("{0}/{1} GET as {2} returned {3} ({4}) with {5}", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase, resultString);
                    }
                    else
                    {
                        logger.Error("{0}/{1} GET as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase);
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        loggerConsole.Error("{0}/{1} GET as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase);
                    }

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
                    string resultString = response.Content.ReadAsStringAsync().Result;
                    if (resultString.Length > 0)
                    {
                        logger.Error("{0}/{1} POST as {2} returned {3} ({4}) with {5}", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase, resultString);
                    }
                    else
                    {
                        logger.Error("{0}/{1} POST as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase);
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        loggerConsole.Error("{0}/{1} POST as {2} returned {3} ({4})", this.ControllerUrl, restAPIUrl, this.UserName, (int)response.StatusCode, response.ReasonPhrase);
                    }

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
